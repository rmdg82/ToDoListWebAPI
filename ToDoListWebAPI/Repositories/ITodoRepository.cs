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

        Task<IEnumerable<Todo>> GetTodos();

        Task ToggleCompletion(Guid todosId);

        Task AddTodo(Todo todo);

        Task UpdateTodo(Guid todoId, Todo todoUpdated);

        Task DeleteTodo(Guid todoId);
    }
}