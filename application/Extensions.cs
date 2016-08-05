using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace application
{
    public static class Extensions
    {
        public static string GetLastErrorMessage(this Exception exception)
        {
            if (exception == null) return "";

            string message = exception.Message;
            Exception inner = exception.InnerException;
            while (!string.IsNullOrEmpty(inner?.Message))
            {
                message = inner.Message;
                inner = inner.InnerException;
            }

            return message;
        }
    }
}
