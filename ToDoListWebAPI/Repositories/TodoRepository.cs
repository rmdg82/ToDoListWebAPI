using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoListWebAPI.Model;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace ToDoListWebAPI.Repositories
{
    public class TodoRepository : ITodoRepository
    {
        private readonly CosmosClient _cosmosClient;
        private readonly ILogger<TodoRepository> _logger;
        private Database _database;
        private Container _container;
        private readonly string _partitionKey = "/id";

        public TodoRepository(ILogger<TodoRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");

            CosmosClientOptions clientOptions = new()
            {
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            };
            _cosmosClient = new CosmosClient(cosmosConnectionString, clientOptions);
            CreateDatabaseAsync(_cosmosClient).Wait();
            CreateContainerAsync(_cosmosClient).Wait();
        }

        private async Task CreateDatabaseAsync(CosmosClient cosmosClient)
        {
            var databaseId = Environment.GetEnvironmentVariable("DatabaseId");
            var databaseResult = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

            if (databaseResult.StatusCode.Equals(HttpStatusCode.Created))
            {
                _logger.LogInformation($"Created database with id {databaseResult.Database.Id}");
            }
            _database = databaseResult.Database;
        }

        private async Task CreateContainerAsync(CosmosClient cosmosClient)
        {
            var containerId = Environment.GetEnvironmentVariable("ContainerId");
            var containerResult = await cosmosClient.GetDatabase(_database.Id)
                .CreateContainerIfNotExistsAsync(containerId, _partitionKey);

            if (containerResult.StatusCode.Equals(HttpStatusCode.Created))
            {
                _logger.LogInformation($"Created container with id {containerResult.Container.Id}");
            }
            _container = containerResult.Container;
        }

        public Task AddTodo(Todo todo)
        {
            throw new NotImplementedException();
        }

        public Task DeleteTodo(string todoId)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<Todo>> GetTodos(bool getOnlyUncompleted = false)
        {
            if (getOnlyUncompleted)
            {
                return await ExecuteQuery("SELECT * FROM c WHERE c.isCompleted = false");
            }
            else
            {
                return await ExecuteQuery("SELECT * FROM c");
            }
        }

        public async Task InitializeCosmosDbDataIfEmpty()
        {
            var todosToAdd = new List<Todo>
            {
                new Todo
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Wash the dishes",
                    IsCompleted = false
                },
                new Todo
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Clean the house",
                    IsCompleted = true
                },
                new Todo
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = "Mow the meadow",
                    IsCompleted = false
                }
            };

            List<Todo> todos = await ExecuteQuery("SELECT * FROM c");

            if (!todos.Any())
            {
                foreach (var todo in todosToAdd)
                {
                    try
                    {
                        ItemResponse<Todo> itemResponse = await _container.CreateItemAsync(todo);
                    }
                    catch (CosmosException ex) when (ex.StatusCode.Equals(HttpStatusCode.Conflict))
                    {
                        _logger.LogError($"Item with id {todo.Id} already exists in the database.");
                    }
                }
            }
        }

        public async Task ToggleCompletion(string todosId)
        {
            ItemResponse<Todo> response = await _container.ReadItemAsync<Todo>(id: todosId, partitionKey: new PartitionKey(_partitionKey));
            if (response.StatusCode.Equals(HttpStatusCode.NotFound))
            {
                _logger.LogError($"Not found element with id [{todosId}].");
                throw new KeyNotFoundException($"Not found element with id [{todosId}].");
            }

            if (!response.StatusCode.Equals(HttpStatusCode.OK))
            {
                _logger.LogError($"Error: {response.StatusCode} - {response.Diagnostics}");
                throw new Exception("Somenthig went wrong.");
            }

            Todo todo = response;
            todo.IsCompleted = !todo.IsCompleted;

            await _container.ReplaceItemAsync(item: todo, id: todosId, partitionKey: new PartitionKey(_partitionKey));
        }

        public Task UpdateTodo(string todoId, Todo todoUpdated)
        {
            throw new NotImplementedException();
        }

        private async Task<List<Todo>> ExecuteQuery(string sqlQuery)
        {
            var todos = new List<Todo>();
            QueryDefinition queryDefinition = new(sqlQuery);
            _logger.LogInformation($"Executing query: [{sqlQuery}]");

            using (FeedIterator<Todo> feedIterator = _container.GetItemQueryIterator<Todo>(queryDefinition))
            {
                while (feedIterator.HasMoreResults)
                {
                    FeedResponse<Todo> response = await feedIterator.ReadNextAsync();
                    todos.AddRange(response);
                }
            }

            return todos;
        }

        public Task<Todo> GetById(string todoId)
        {
            throw new NotImplementedException();
        }
    }
}