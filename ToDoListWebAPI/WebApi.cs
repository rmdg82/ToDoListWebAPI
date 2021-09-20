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
using System.IO;
using ToDoListWebAPI.Dtos;
using AutoMapper;
using Microsoft.Azure.Cosmos;

namespace ToDoListWebAPI
{
    public class WebApi
    {
        private readonly ITodoRepository _todoRepository;
        private readonly ILogger<WebApi> _logger;
        private readonly IMapper _mapper;

        public WebApi(ITodoRepository todoRepository, ILogger<WebApi> logger, IMapper mapper)
        {
            _todoRepository = todoRepository ?? throw new ArgumentNullException(nameof(todoRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        [Function("InitializeDb")]
        public async Task<HttpResponseData> InitializeDb([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("Initialize CosmosDb with fake data.");
            var result = await _todoRepository.InitializeCosmosDbDataIfEmpty();

            if (result)
            {
                return req.CreateResponse(HttpStatusCode.Created);
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [Function("GetAllTodos")]
        public async Task<HttpResponseData> GetTodos([HttpTrigger(AuthorizationLevel.Function, "get", Route = "todos")] HttpRequestData req)
        {
            var queryParams = HttpUtility.ParseQueryString(req.Url.Query);
            var getOnlyUncompleted = queryParams["onlyUncompleted"];
            _logger.LogInformation($"New request for {nameof(GetTodos)} with querystring [{queryParams}].");

            var todos = new List<Todo>();
            try
            {
                bool onlyUncompleted = Convert.ToBoolean(getOnlyUncompleted);

                if (onlyUncompleted)
                {
                    todos = (await _todoRepository.GetByQueryAsync(getOnlyUncompleted: true)).ToList();
                }
                else
                {
                    todos = (await _todoRepository.GetByQueryAsync()).ToList();
                }
            }
            catch (FormatException ex)
            {
                todos = (await _todoRepository.GetByQueryAsync()).ToList();
                _logger.LogInformation(ex.Message);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(todos);

            return response;
        }

        [Function("GetTodosById")]
        public async Task<HttpResponseData> GetTodoById([HttpTrigger(AuthorizationLevel.Function, "get", Route = "todos/{todoId}")] HttpRequestData req, string todoId)
        {
            _logger.LogInformation($"New request for {nameof(GetTodoById)} with id [{todoId}].");

            var todo = await _todoRepository.GetByIdAsync(todoId);

            if (todo is null)
            {
                _logger.LogError($"Not found todo for {nameof(GetTodoById)} with id [{todoId}].");
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(_mapper.Map<TodoDto>(todo));

            return response;
        }

        [Function("AddTodo")]
        public async Task<HttpResponseData> AddTodo([HttpTrigger(AuthorizationLevel.Function, "post", Route = "todos")] HttpRequestData req)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"New request for {nameof(AddTodo)} with body [{requestBody}].");
            var todoToAddDto = JsonSerializer.Deserialize<TodoDtoToAdd>(requestBody);
            var todoToAdd = _mapper.Map<Todo>(todoToAddDto);
            todoToAdd.Id = Guid.NewGuid().ToString();

            try
            {
                await _todoRepository.AddAsync(todoToAdd);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var response = req.CreateResponse();
            await response.WriteAsJsonAsync(_mapper.Map<TodoDto>(todoToAdd));
            response.Headers.Add("Location", $"{req.Url.OriginalString}/{todoToAdd.Id}");
            response.StatusCode = HttpStatusCode.Created;

            return response;
        }

        [Function("ToggleCompletion")]
        public async Task<HttpResponseData> ToggleCompletion([HttpTrigger(AuthorizationLevel.Function, "post", Route = "todos/{todoId}/toggle")] HttpRequestData req, string todoId)
        {
            HttpResponseData response;
            try
            {
                _logger.LogInformation($"Toggle completion for todo with id [{todoId}]");
                await _todoRepository.ToggleCompletionAsync(todoId);
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

        [Function("DeleteTodo")]
        public async Task<HttpResponseData> DeleteTodo([HttpTrigger(AuthorizationLevel.Function, "delete", Route = "todos/{todoId}")] HttpRequestData req, string todoId)
        {
            _logger.LogInformation($"New request for {nameof(DeleteTodo)} with id [{todoId}].");

            try
            {
                await _todoRepository.DeleteAsync(todoId);
            }
            catch (CosmosException ex) when (ex.StatusCode.Equals(HttpStatusCode.NotFound))
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }

        [Function("UpdateTodo")]
        public async Task<HttpResponseData> UpdateTodo([HttpTrigger(AuthorizationLevel.Function, "put", Route = "todos/{todoId}")] HttpRequestData req, string todoId)
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"New request for {nameof(UpdateTodo)} with id [{todoId}] and body [{requestBody}].");

            var todoToUpdateDto = JsonSerializer.Deserialize<TodoDtoToUpdate>(requestBody);
            var todoToAdd = _mapper.Map<Todo>(todoToUpdateDto);
            todoToAdd.Id = todoId;

            try
            {
                await _todoRepository.UpdateAsync(todoId, todoToAdd);
            }
            catch (CosmosException ex) when (ex.StatusCode.Equals(HttpStatusCode.NotFound))
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (CosmosException ex) when (ex.StatusCode.Equals(HttpStatusCode.BadRequest))
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
            catch(KeyNotFoundException ex)
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.NotFound);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Catched exception: [{ex.Message}]");
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            return req.CreateResponse(HttpStatusCode.NoContent);
        }
    }
}