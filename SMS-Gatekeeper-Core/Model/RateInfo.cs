using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMS_Gatekeeper_Core.Model
{
    public class RateInfo
    {
        public int Count { get; set; }
        public DateTime StartWindow { get; set; }
        public DateTime LastUsed { get; set; }
        public RateInfo() 
        {
            Count = 0;
            StartWindow = DateTime.UtcNow;
            LastUsed = DateTime.UtcNow;
        }
    }
}
