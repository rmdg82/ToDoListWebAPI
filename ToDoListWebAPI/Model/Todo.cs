using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ToDoListWebAPI.Model
{
    public class Todo
    {
        public string Id { get; set; }

        public string Text { get; set; }

        public bool IsCompleted { get; set; }
    }
}