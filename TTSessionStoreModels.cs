using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TTCustomSessionStore
{
    public class TTCleanInfo {
        public int Count { get; set; }
        public DateTime LastestCleanTime { get; set; }

        public TTCleanInfo() {
            Count = 0;
            LastestCleanTime = DateTime.UtcNow;
        }
    }
}
