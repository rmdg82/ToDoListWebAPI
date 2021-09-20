using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ToDoListWebAPI.Model;
using ToDoListWebAPI.Repositories;
using System.Web;
using System.Linq;

namespace ToDoListWebAPI
{
    public class WebApi
    {
        private readonly ITodoRepository _todoRepository;
        private readonly ILogger<WebApi> _logger;

        public WebApi(ITodoRepository todoRepository, ILogger<WebApi> logger)
        {
            _todoRepository = todoRepository ?? throw new ArgumentNullException(nameof(todoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("InitializeDb")]
        public HttpResponseData InitializeDb([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Initialize CosmosDb with fake data.");
            _todoRepository.InitializeCosmosDbDataIfEmpty().Wait();

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        [Function("GetAllTodos")]
        public async Task<HttpResponseData> GetTodos([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
            var getOnlyUncompleted = queryParams["onlyUncompleted"];
            _logger.LogInformation($"New request for GetAllTodos with querystring [{queryParams}].");

            var todos = new List<Todo>();
            try
            {
                bool onlyUncompleted = Convert.ToBoolean(getOnlyUncompleted);

                if (onlyUncompleted)
                {
                    todos = (await _todoRepository.GetTodos(onlyUncompleted)).ToList();
                }
                else
                {
                    todos = (await _todoRepository.GetTodos()).ToList();
                }
            }
            catch (FormatException ex)
            {
                todos = (await _todoRepository.GetTodos()).ToList();
                _logger.LogInformation(ex.Message);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todos);

            return response;
        }

        [Function("ToggleCompletion")]
        public async Task<HttpResponseData> ToggleCompletion([HttpTrigger(AuthorizationLevel.Function, "post", Route = "ToggleCompletion/{todoId}")] HttpRequestData req,
            FunctionContext executionContext, string todoId)
        {
            HttpResponseData response;
            try
            {
                _logger.LogInformation($"Toggle completion for todo with id [{todoId}]");
                await _todoRepository.ToggleCompletion(todoId);
                response = req.CreateResponse(HttpStatusCode.OK);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                response = req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                response = req.CreateResponse(HttpStatusCode.BadRequest);
                response.WriteString(ex.Message);
            }

            return response;
        }
    }
}