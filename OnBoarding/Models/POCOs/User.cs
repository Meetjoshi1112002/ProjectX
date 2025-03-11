using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace OnBoarding.Models.POCOs
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRequired]
        [BsonElement("name")]
        public string Name { get; set; }

        [BsonRequired]
        [BsonElement("email")]
        public string Email { get; set; }

        [BsonRequired]
        [BsonElement("password")]
        public string Password { get; set; }

        [BsonRequired]
        [BsonElement("company")]
        public string Company { get; set; }

        [BsonRequired]
        [BsonElement("role")]
        public Role Role { get; set; }

        public User()
        {
            Role = Role.User;
        }
    }

    public enum Role
    {
        Admin = 1,
        User
    }
}
