using MasterNode.Models.POCOs;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.Contracts;

namespace MasterNode.Models.DTOs
{
    public class DTO02
    {
        public class ResposneLimitDto
        {
            [Required]
            public ResponseLimitType LimitType { get; set; }
            [Required]
            public AccessType AccessType { get; set; }


        }
        public class UserDto
        {
            [Required]
            public string UserId { get; set; }
            [Required]
            public string Email { get; set; }
        }
    }
}
