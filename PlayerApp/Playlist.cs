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
        public List<string> Tracks { get; set; } = new List<string>();
    }
}
