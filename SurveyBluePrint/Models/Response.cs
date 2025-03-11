namespace SurveyBluePrint.Models
{

    /// <summary>
    /// Standard API response format for consistency
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
