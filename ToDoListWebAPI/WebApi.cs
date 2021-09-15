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
        private readonly ILogger<WebApi> _logger;

        public WebApi(ITodoRepository todoRepository, ILogger<WebApi> logger)
        {
            _todoRepository = todoRepository ?? throw new ArgumentNullException(nameof(todoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        [Function("InitializeDb")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            _logger.LogInformation("Initialize CosmosDb with fake data.");
            _todoRepository.InitializeCosmosDbDataIfEmpty().Wait();

            var response = req.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}