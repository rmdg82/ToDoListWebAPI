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
        Task<bool> InitializeCosmosDbDataIfEmpty();

        Task<IEnumerable<Todo>> GetByQueryAsync(string sqlQuery);

        Task<IEnumerable<Todo>> GetByQueryAsync(bool getOnlyUncompleted = false);

        Task<Todo> GetByIdAsync(string todoId);

        Task ToggleCompletionAsync(string todosId);

        Task AddAsync(Todo todo);

        Task UpdateAsync(string todoId, Todo todoUpdated);

        Task DeleteAsync(string todoId);
    }
}