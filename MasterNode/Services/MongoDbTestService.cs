using MongoDB.Driver;

namespace MasterNode.Services
{
    public class MongoDbTestService
    {
        private readonly IMongoDatabase _database;

        public MongoDbTestService(IMongoDatabase database)
        {
            _database = database;
        }

        public bool IsDatabaseConnected()
        {
            try
            {
                _database.ListCollectionNames(); // Ping MongoDB
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
