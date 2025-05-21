using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayerApp
{
    public class Playlist
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int CategoryId { get; set; }  // Внешний ключ
        public List<string> Tracks { get; set; } = new List<string>();

        [JsonIgnore]  // Игнорируем при сериализации в JSON
        public Category Category { get; set; }
    }
}
