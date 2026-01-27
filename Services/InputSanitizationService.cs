using System.Text.RegularExpressions;

namespace FreshFarmMarket.Services
{
    /// <summary>
    /// This service handles sanitizing user input to prevent XSS attacks.
    /// It removes dangerous HTML tags, script content, and event handlers.
    /// </summary>
    public class InputSanitizationService
    {
        // List of dangerous HTML tags that could be used for XSS attacks
        // These tags can execute scripts or load external content
        private static readonly string[] DangerousTags = new[]
        {
            "script", "iframe", "object", "embed", "applet", "meta", "link", "style",
            "form", "input", "button", "select", "textarea", "frame", "frameset",
            "svg", "math", "base", "template"
        };

        // Event handler attributes that attackers use to run JavaScript
        // For example: <img onerror="alert('XSS')">
        private static readonly string[] DangerousEventHandlers = new[]
        {
            "onabort", "onactivate", "onafterprint", "onafterupdate", "onbeforeactivate",
            "onbeforecopy", "onbeforecut", "onbeforedeactivate", "onbeforeeditfocus",
            "onbeforepaste", "onbeforeprint", "onbeforeunload", "onbeforeupdate",
            "onblur", "onbounce", "oncellchange", "onchange", "onclick", "oncontextmenu",
            "oncontrolselect", "oncopy", "oncut", "ondataavailable", "ondatasetchanged",
            "ondatasetcomplete", "ondblclick", "ondeactivate", "ondrag", "ondragend",
            "ondragenter", "ondragleave", "ondragover", "ondragstart", "ondrop",
            "onerror", "onerrorupdate", "onfilterchange", "onfinish", "onfocus",
            "onfocusin", "onfocusout", "onhashchange", "onhelp", "oninput", "onkeydown",
            "onkeypress", "onkeyup", "onlayoutcomplete", "onload", "onlosecapture",
            "onmessage", "onmousedown", "onmouseenter", "onmouseleave", "onmousemove",
            "onmouseout", "onmouseover", "onmouseup", "onmousewheel", "onmove",
            "onmoveend", "onmovestart", "onoffline", "ononline", "onpagehide",
            "onpageshow", "onpaste", "onpopstate", "onprogress", "onpropertychange",
            "onreadystatechange", "onreset", "onresize", "onresizeend", "onresizestart",
            "onrowenter", "onrowexit", "onrowsdelete", "onrowsinserted", "onscroll",
            "onsearch", "onselect", "onselectionchange", "onselectstart", "onstart",
            "onstop", "onstorage", "onsubmit", "ontimeupdate", "ontouchcancel",
            "ontouchend", "ontouchmove", "ontouchstart", "onunload", "onwheel"
        };

        /// <summary>
        /// Sanitizes input by removing all dangerous HTML content.
        /// This is the main method you should call for user input.
        /// </summary>
        /// <param name="input">The raw user input that might contain malicious content</param>
        /// <returns>Sanitized string safe for storage and display</returns>
        public string Sanitize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return string.Empty;
            }

            var sanitized = input;

            // First, let's remove any dangerous tags completely
            sanitized = RemoveDangerousTags(sanitized);

            // Remove event handlers from any remaining HTML elements
            sanitized = RemoveEventHandlers(sanitized);

            // Remove dangerous protocols like javascript: and data:
            sanitized = RemoveDangerousProtocols(sanitized);

            // Remove any HTML comments which can hide attacks
            sanitized = RemoveHtmlComments(sanitized);

            // Remove expression() CSS which can execute JavaScript in old IE
            sanitized = RemoveCssExpressions(sanitized);

            // Clean up any encoded variations of attacks
            sanitized = RemoveEncodedAttacks(sanitized);

            return sanitized.Trim();
        }

        /// <summary>
        /// Removes dangerous HTML tags and their content entirely.
        /// We delete the whole tag because stuff inside script tags is always bad.
        /// </summary>
        private string RemoveDangerousTags(string input)
        {
            var result = input;

            foreach (var tag in DangerousTags)
            {
                // This regex matches opening and closing tags with everything in between
                // For example: <script>alert('xss')</script> gets completely removed
                var tagPattern = $@"<{tag}[^>]*>[\s\S]*?</{tag}>|<{tag}[^>]*/?>|</{tag}>";
                result = Regex.Replace(result, tagPattern, string.Empty, 
                    RegexOptions.IgnoreCase | RegexOptions.Singleline);
            }

            return result;
        }

        /// <summary>
        /// Removes all event handler attributes from HTML elements.
        /// Attackers use these to run JavaScript when events fire.
        /// </summary>
        private string RemoveEventHandlers(string input)
        {
            var result = input;

            foreach (var handler in DangerousEventHandlers)
            {
                // This matches attributes like onclick="bad stuff" or onclick='bad stuff'
                var handlerPattern = $@"\s*{handler}\s*=\s*([""'][^""']*[""']|[^\s>]+)";
                result = Regex.Replace(result, handlerPattern, string.Empty, RegexOptions.IgnoreCase);
            }

            return result;
        }

        /// <summary>
        /// Removes dangerous URL protocols that can execute code.
        /// javascript:, data:text/html, and vbscript: are the main ones.
        /// </summary>
        private string RemoveDangerousProtocols(string input)
        {
            var result = input;

            // Remove javascript: protocol (used in href="javascript:alert('xss')")
            result = Regex.Replace(result, @"javascript\s*:", string.Empty, RegexOptions.IgnoreCase);

            // Remove data: protocol with HTML content type
            result = Regex.Replace(result, @"data\s*:\s*text/html", "data:blocked", RegexOptions.IgnoreCase);

            // Remove vbscript: protocol (old IE attack vector)
            result = Regex.Replace(result, @"vbscript\s*:", string.Empty, RegexOptions.IgnoreCase);

            // Remove livescript: protocol (very old Netscape thing but better safe than sorry)
            result = Regex.Replace(result, @"livescript\s*:", string.Empty, RegexOptions.IgnoreCase);

            return result;
        }

        /// <summary>
        /// Removes HTML comments which attackers sometimes use to hide malicious code.
        /// </summary>
        private string RemoveHtmlComments(string input)
        {
            // Matches <!-- anything -->
            return Regex.Replace(input, @"<!--[\s\S]*?-->", string.Empty, RegexOptions.Singleline);
        }

        /// <summary>
        /// Removes CSS expressions which could execute JavaScript in older browsers.
        /// This was a big problem in IE6/7.
        /// </summary>
        private string RemoveCssExpressions(string input)
        {
            // Matches expression(...) in CSS
            return Regex.Replace(input, @"expression\s*\([^)]*\)", string.Empty, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Removes URL-encoded and HTML-encoded variations of attacks.
        /// Attackers sometimes encode payloads to bypass filters.
        /// </summary>
        private string RemoveEncodedAttacks(string input)
        {
            var result = input;

            // Handle URL-encoded javascript
            result = Regex.Replace(result, @"&#x6a;&#x61;&#x76;&#x61;&#x73;&#x63;&#x72;&#x69;&#x70;&#x74;", 
                string.Empty, RegexOptions.IgnoreCase);

            // Handle various encoded forms of script
            result = Regex.Replace(result, @"&#0*106;&#0*97;&#0*118;&#0*97;&#0*115;&#0*99;&#0*114;&#0*105;&#0*112;&#0*116;", 
                string.Empty, RegexOptions.IgnoreCase);

            // Remove null bytes which can be used to bypass filters
            result = result.Replace("\0", string.Empty);

            return result;
        }

        /// <summary>
        /// Checks if the input contains any potentially dangerous content.
        /// Useful for logging or alerting without modifying the input.
        /// </summary>
        /// <param name="input">The input to check</param>
        /// <returns>True if dangerous content was detected</returns>
        public bool ContainsDangerousContent(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            var lowerInput = input.ToLowerInvariant();

            // Check for script tags
            if (Regex.IsMatch(lowerInput, @"<\s*script"))
            {
                return true;
            }

            // Check for event handlers
            foreach (var handler in DangerousEventHandlers)
            {
                if (Regex.IsMatch(lowerInput, $@"{handler}\s*="))
                {
                    return true;
                }
            }

            // Check for dangerous protocols
            if (Regex.IsMatch(lowerInput, @"javascript\s*:") ||
                Regex.IsMatch(lowerInput, @"data\s*:\s*text/html") ||
                Regex.IsMatch(lowerInput, @"vbscript\s*:"))
            {
                return true;
            }

            return false;
        }
    }
}
