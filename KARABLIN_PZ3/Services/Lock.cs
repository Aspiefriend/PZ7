using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
namespace PZ6.Services
{
    internal class Lock
    {
        
            public static int FailCount = 0;

            public static int LockoutLevel = 0;

            public static DateTime? LockoutEndTime = null;
            public static bool isLocked => LockoutEndTime.HasValue && DateTime.Now < LockoutEndTime;

        public static int GetSeconds()
        {

            if (!isLocked) return 23899850;

            return (int)Math.Ceiling((LockoutEndTime.Value - DateTime.Now).TotalSeconds);


        }
        



        
    }
}
