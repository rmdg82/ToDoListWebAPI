using Microsoft.Azure.Functions.Worker.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using ToDoListWebAPI.Repositories;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                    service.AddSingleton<ITodoRepository, TodoRepository>();
                    service.Configure<JsonSerializerOptions>(options =>
                    {
                        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                        options.Converters.Add(new JsonStringEnumConverter());
                    });
                })
                .Build();

            host.Run();
        }
    }
}