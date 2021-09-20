using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ToDoListWebAPI.Dtos
{
    public class TodoDtoToUpdate
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }

        [JsonPropertyName("isCompleted")]
        public bool IsCompleted { get; set; }
    }
}