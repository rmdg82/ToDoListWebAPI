using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToDoListWebAPI.Model;

namespace ToDoListWebAPI.Repositories
{
    public interface ITodoRepository
    {
        Task InitializeCosmosDbDataIfEmpty();

        Task<IEnumerable<Todo>> GetTodos(bool getOnlyUncompleted = false);

        Task<Todo> GetById(string todoId);

        Task ToggleCompletion(string todosId);

        Task AddTodo(Todo todo);

        Task UpdateTodo(string todoId, Todo todoUpdated);

        Task DeleteTodo(string todoId);
    }
}