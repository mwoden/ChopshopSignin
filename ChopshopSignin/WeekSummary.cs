using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    sealed internal class WeekSummary
    {
        public int Week { get; private set; }
        public string FullName { get; private set; }
        public IEnumerable<string> SignInTimes { get; private set; }

        public WeekSummary(int weekNumber, string name, IEnumerable<string> times)
        {
            Week = weekNumber;
            FullName = name;
            SignInTimes = times;
        }
    }
}
