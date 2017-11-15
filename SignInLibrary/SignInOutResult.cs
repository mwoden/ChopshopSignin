using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignInLibrary
{
    public sealed class SignInOutResult
    {
        public bool OperationSucceeded { get; }
        public string Status { get; }

        public SignInOutResult(bool success, string status)
        {
            OperationSucceeded = success;
            Status = status;
        }

    }
}
