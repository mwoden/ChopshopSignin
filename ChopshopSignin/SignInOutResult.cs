using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChopshopSignin
{
    sealed internal class SignInOutResult
    {
        public bool OperationSucceeded { get; private set; }
        public string Status { get; private set; }

        public SignInOutResult(bool success, string status)
        {
            OperationSucceeded = success;
            Status = status;
        }
    }
}
