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

        public TodoRepository(ILogger<TodoRepository> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            var cosmosConnectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");

            //_cosmosClient = new CosmosClient(cosmosConnectionString);
            CosmosClientOptions clientOptions = new CosmosClientOptions()
            {
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            };
            _cosmosClient = new CosmosClient(cosmosConnectionString, clientOptions);
            CreateDatabaseAsync(_cosmosClient).Wait();
            CreateContainerAsync(_cosmosClient).Wait();
            InitializeCosmosDbDataIfEmpty().Wait();
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
                .CreateContainerIfNotExistsAsync(containerId, "/id");

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

        public Task DeleteTodo(Guid todoId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Todo>> GetTodos()
        {
            throw new NotImplementedException();
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

            var sqlQuery = "SELECT * FROM c";

            QueryDefinition queryDefinition = new(sqlQuery);
            FeedIterator<Todo> feedIterator = _container.GetItemQueryIterator<Todo>(queryDefinition);
            var todos = new List<Todo>();
            while (feedIterator.HasMoreResults)
            {
                var feedResponse = await feedIterator.ReadNextAsync();
                foreach (Todo item in feedResponse)
                {
                    todos.Add(item);
                }
            }

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

        public Task ToggleCompletion(Guid todosId)
        {
            throw new NotImplementedException();
        }

        public Task UpdateTodo(Guid todoId, Todo todoUpdated)
        {
            throw new NotImplementedException();
        }
    }
}