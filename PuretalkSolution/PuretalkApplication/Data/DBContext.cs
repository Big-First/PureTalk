using MongoDB.Driver;

namespace PuretalkApplication.Data
{
    public class DBContext
    {
        public DBContext() { }
        IMongoClient client;
        string databaseName;
        IMongoDatabase _database;


        public DBContext(string connectionString, string _databaseName)
        {
            client = new MongoClient(connectionString);
            databaseName = _databaseName;
            GetOrCreateDatabase();
        }

        public IMongoDatabase GetDatabase()
        => _database;

        public void GetOrCreateDatabase()
        {
            _database = client.GetDatabase(databaseName);
            var collectionList = _database.ListCollectionNames().ToList();
            if (collectionList.Count <= 0) _database.CreateCollection(databaseName);
        }
    }
}
