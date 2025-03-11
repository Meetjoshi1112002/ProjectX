using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

public class Otp
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; } // Nullable _id to let MongoDB handle it

    [BsonRequired]
    public string Email { get; set; }

    [BsonRequired]
    public string OtpCode { get; set; }

    [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
    public DateTime OtpExpiry { get; set; }
}
