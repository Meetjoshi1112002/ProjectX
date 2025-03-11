using MongoDB.Driver;
using OnBoarding.Data;

namespace OnBoarding.Services
{
    public class MongoDbTestService
    {
        private readonly IMongoDatabase _database;

        public MongoDbTestService(AppDbContext dbContext)
        {
            _database = dbContext.Database; 

        }

        public bool IsDatabaseConnected()
        {
            try
            {
                _database.ListCollectionNames(); // ping db
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
    