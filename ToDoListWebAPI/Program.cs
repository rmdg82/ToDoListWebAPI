using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using ToDoListWebAPI.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos;
using System;
using System.Reflection;

namespace ToDoListWebAPI
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(service =>
                {
                    service.AddSingleton<ITodoRepository>(InitializeCosmosClientInstanceAsync().GetAwaiter().GetResult());
                    service.Configure<JsonSerializerOptions>(options =>
                    {
                        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.Converters.Add(new JsonStringEnumConverter());
                    });
                    service.AddAutoMapper(Assembly.GetExecutingAssembly());
                })
                .Build();

            host.Run();
        }

        private static async Task<CosmosTodoRepository> InitializeCosmosClientInstanceAsync()
        {
            string connectionString = Environment.GetEnvironmentVariable("CosmosConnectionString");
            string databaseId = Environment.GetEnvironmentVariable("DatabaseId");
            string containerId = Environment.GetEnvironmentVariable("ContainerId");
            CosmosClientOptions clientOptions = new()
            {
                SerializerOptions = new CosmosSerializationOptions()
                {
                    PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
                }
            };

            var client = new CosmosClient(connectionString, clientOptions);

            var database = await client.CreateDatabaseIfNotExistsAsync(databaseId);
            await database.Database.CreateContainerIfNotExistsAsync(containerId, "/id");
            var cosmosDbService = new CosmosTodoRepository(client, databaseId, containerId);

            return cosmosDbService;
        }
    }
}