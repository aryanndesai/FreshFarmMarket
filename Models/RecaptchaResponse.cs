namespace FreshFarmMarket.Models
{
    public class RecaptchaResponse
    {
        public bool success { get; set; }
        public float score { get; set; }
        public string action { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
    }
}
