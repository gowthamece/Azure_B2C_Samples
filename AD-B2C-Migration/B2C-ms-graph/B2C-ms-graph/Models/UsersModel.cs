using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace B2C_ms_graph.Models
{
    public class UsersModel
    {
        [JsonPropertyName("users")]
        public UserModel[] Users { get; set; }

        public static UsersModel Parse(string JSON)
        {
            return JsonSerializer.Deserialize<UsersModel>(JSON);
        }
    }
}
