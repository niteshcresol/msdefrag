using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MSDefragLib
{
    public class LogMessage
    {
        public LogMessage(Int16 level, String msg)
        {
            logLevel = level;
            message = msg;
        }


        private Int16 logLevel;
        public Int16 LogLevel { set { logLevel = value; } get { return logLevel; } }

        private String message;
        public String Message { set { message = value; } get { return message; } }
    }
}
