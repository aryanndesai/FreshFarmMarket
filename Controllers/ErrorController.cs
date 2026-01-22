using Microsoft.AspNetCore.Mvc;

namespace FreshFarmMarket.Controllers
{
    public class ErrorController : Controller
    {
        [Route("Error/{statusCode}")]
        public IActionResult HttpStatusCodeHandler(int statusCode)
        {
            switch (statusCode)
            {
                case 404:
                    ViewBag.ErrorMessage = "Sorry, the page you requested could not be found.";
                    ViewBag.ErrorCode = "404";
                    ViewBag.ErrorTitle = "Page Not Found";
                    break;
                case 403:
                    ViewBag.ErrorMessage = "Sorry, you don't have permission to access this page.";
                    ViewBag.ErrorCode = "403";
                    ViewBag.ErrorTitle = "Access Forbidden";
                    break;
                case 500:
                    ViewBag.ErrorMessage = "Sorry, something went wrong on our end. Please try again later.";
                    ViewBag.ErrorCode = "500";
                    ViewBag.ErrorTitle = "Internal Server Error";
                    break;
                default:
                    ViewBag.ErrorMessage = "An error occurred while processing your request.";
                    ViewBag.ErrorCode = statusCode.ToString();
                    ViewBag.ErrorTitle = "Error";
                    break;
            }

            return View("CustomError");
        }
    }
}