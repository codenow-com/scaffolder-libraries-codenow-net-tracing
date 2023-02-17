using System.Collections.Generic;

namespace CodeNow.Tracing
{
    public class RequestHeaderInjection
    {
        public Dictionary<string, string> RequestHeaders { get; set; }
        public ActivityInfo ActivityInfo { get; set; }
    }
}