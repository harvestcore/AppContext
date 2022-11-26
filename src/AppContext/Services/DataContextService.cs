using System.Linq.Expressions;
using System.Reflection;

using AppContext.Interfaces;
using AppContext.Services.Interfaces;

using MongoDB.Driver;

namespace AppContext.Services;

public class DataContextService : IDataContextService
{
    /// <summary>
    /// The MongoDB client.
    /// </summary>
    private readonly MongoClient _mongoClient;

    /// <summary>
    /// The database.
    /// </summary>
    private readonly IMongoDatabase _database;

    /// <summary>
    /// The static property name that contains the collection name.
    /// </summary>
    private readonly string CollectionNamePropertyName = "CollectionName";

    /// <summary>
    /// Default constructor.
    /// </summary>
    /// <param name="configurationService">The configuration service where to extract the required connection parameters.</param>
    public DataContextService(IConfigurationService configurationService)
    {
        string? connectionString = configurationService.Get<string>("MongoDBConnectionString");
        string? databaseName = configurationService.Get<string>("MongoDBDatabaseName");

        _mongoClient = new MongoClient(connectionString) ?? throw new Exception("Could not get the MongoDB client.");
        _database = _mongoClient?.GetDatabase(databaseName) ?? throw new Exception("Could not get the database.");
    }

    /// <summary>
    /// Constructor with parameters.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="databaseName">The database name.</param>
    public DataContextService(string connectionString, string databaseName)
    {
        _mongoClient = new MongoClient(connectionString) ?? throw new Exception("Could not get the MongoDB client.");
        _database = _mongoClient?.GetDatabase(databaseName) ?? throw new Exception("Could not get the database.");
    }

    /// <inheritdoc/>
    public IMongoCollection<TValue> GetCollection<TValue>() where TValue : IBaseItem
    {
        // Get the static property that contains the collection name.
        PropertyInfo? property = typeof(TValue).GetProperty(CollectionNamePropertyName);

        if (property != null)
        {
            // Cast the object to string.
            string? collectionName = property.GetValue(null) as string;

            if (!string.IsNullOrEmpty(collectionName))
            {
                // Return the collection.
                return _database.GetCollection<TValue>(collectionName);
            }
        }

        throw new Exception("Missing collection.");
    }

    /// <inheritdoc/>
    public async Task<List<TValue>> Filter<TValue>(Expression<Func<TValue, bool>> filter) where TValue : IBaseItem
    {
        // List of elements to be returned.
        List<TValue> output = new();

        // Get all the items.
        IAsyncCursor<TValue> items = await GetCollection<TValue>().FindAsync(filter);
        if (items != null)
        {
            // Convert the items to a list.
            output = items.ToList();
        }

        // Return the items.
        return output;
    }

    /// <inheritdoc/>
    public async Task<List<TValue>> GetAll<TValue>() where TValue : IBaseItem
    {
        // List of elements to be returned.
        List<TValue> output = new();

        // Get all the items.
        IAsyncCursor<TValue> items = await GetCollection<TValue>().FindAsync(_ => true);
        if (items != null)
        {
            // Convert the items to a list.
            output = items.ToList();
        }

        // Return the items.
        return output;
    }

    /// <inheritdoc/>
    public async Task<TValue?> Get<TValue>(Guid guid) where TValue : IBaseItem
    {
        // Get all the items.
        FilterDefinition<TValue> filter = Builders<TValue>.Filter.Eq("_id", guid.ToString());
        IAsyncCursor<TValue> items = await GetCollection<TValue>().FindAsync(filter);
        if (items != null)
        {
            // Convert the items to a list.
            return items.FirstOrDefault();
        }

        // Return the items.
        return default;
    }

    /// <inheritdoc/>
    public async Task<bool> Insert<TValue>(TValue item) where TValue : IBaseItem
    {
        if (item == null)
        {
            return false;
        }

        await GetCollection<TValue>().InsertOneAsync(item);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> Update<TValue>(Guid guid, TValue item) where TValue : IBaseItem
    {
        if (item == null)
        {
            return false;
        }

        FilterDefinition<TValue> filter = Builders<TValue>.Filter.Eq("_id", guid.ToString());
        ReplaceOneResult result = await GetCollection<TValue>().ReplaceOneAsync(filter, item);
        return result.ModifiedCount == 1;
    }

    /// <inheritdoc/>
    public async Task<bool> Delete<TValue>(Guid guid) where TValue : IBaseItem
    {
        FilterDefinition<TValue> filter = Builders<TValue>.Filter.Eq("_id", guid.ToString());
        DeleteResult result = await GetCollection<TValue>().DeleteOneAsync(filter);
        return result.DeletedCount == 1;
    }
}
