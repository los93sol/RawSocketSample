using System;

namespace ClientLibrary
{
    public class ClientOptions
    {
        public string ServerHostname { get; set; }
        public int ServerPort { get; set; }
        public string Message { get; set; }
        public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(5);
    }
}