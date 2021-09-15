using System;
using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ToDoListWebAPI.Repositories;

namespace ToDoListWebAPI
{
    public class WebApi
    {
        private readonly ITodoRepository _todoRepository;
        //private readonly ILogger<WebApi> _logger;

        public WebApi(ITodoRepository todoRepository)
        {
            _todoRepository = todoRepository ?? throw new ArgumentNullException(nameof(todoRepository));
            //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("GetAll")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _todoRepository.InitializeCosmosDbDataIfEmpty().Wait();

            var logger = executionContext.GetLogger("GetAll");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            //_logger.LogInformation("Loggin using injected logger.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString("Welcome to Azure Functions!");

            return response;
        }
    }
}