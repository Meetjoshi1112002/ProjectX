namespace MasterNode.Models
{
    /// <summary>
    /// Standard API response format for consistency
    /// </summary>
    public class ApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public dynamic Data { get; set; }
    }
}
