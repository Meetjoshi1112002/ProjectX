namespace OnBoarding.Models
{
    public class Response
    {
        public dynamic Data { get; set; }

        public bool IsError { get; set; }

        public string Message { get; set; }
    }
}
