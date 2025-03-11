using MasterNode.Models;
using MasterNode.Models.DTOs;
using Microsoft.VisualBasic;
using RestSharp;

namespace MasterNode.Services
{
    public class ApiServices
    {
        private readonly RestClient _client;

        public ApiServices()
        {
            _client = new RestClient("https://localhost:7167"); //since url is going to remain the same
        }

        public async Task<ApiResponse> SendCommand(MessageDto _dto)
        {
            var request = new RestRequest("/inform-task",Method.Post);
            request.AddJsonBody(_dto);
            return await _client.PostAsync<ApiResponse>(request)??new ApiResponse
            {
                Success = false,
                Message = "Unable to reach server"
            };
        }
    }
}
