using MongoDB.Driver;
using PuretalkApplication.Data;
using PuretalkApplication.Models;

namespace PuretalkApplication.Repositorys;

public class ApplicationRepository
{
    public DBContext _context { get; set; }
    public ApplicationRepository(DBContext dbContext)
        => _context = dbContext;

    public async Task<object?> GetObject(DBContext _DbMongo, string Id)
    {
        var collection = _DbMongo.GetDatabase().GetCollection<UserRegister>("UserRegister");

        // Create a filter to find the document by Id
        var filter = Builders<UserRegister>.Filter.Eq(u => u.id, Id);

        // Find the document matching the filter
        var wallet = collection.Find(filter).FirstOrDefault();

        return wallet as UserRegister;
    }

    public async Task<object?> InsetObject(DBContext _DbMongo, UserRegister _object, CancellationToken cancellationToken)
    {
        var collection = _DbMongo.GetDatabase().GetCollection<UserRegister>("UserRegister");
        // Insert the user object into the collection
        collection.InsertOne(_object);
        return _object;
    }

    public async Task<object> UpdateObject(object _object, CancellationToken cancellationToken)
    {
        return null;
    }

    public async Task RemoveObject(object _object, CancellationToken cancellationToken)
    {
    }

    public async Task<bool> CreateAvatar(string objId)
    {
        Console.WriteLine($"{nameof(CreateAvatar)} >> {DateTime.Now}");
        //string url = $"http://localhost:5250/newAccount/{objId}";
        string url = $"http://46.202.148.94:5250/newAccount/{objId}";
        using (HttpClientHandler handler = new HttpClientHandler())
        {
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; };
            using (HttpClient client = new HttpClient(handler))
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();
                    return responseBody == "200";
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error No Data: {ex.Message}");
                }
            }
        }
        return false;
    }
}