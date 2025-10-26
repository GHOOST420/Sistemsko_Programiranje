using System;

namespace WeatherRxServer.DTOs
{
    public class RequestLog
    {
        public DateTime Timestamp { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public long Duration { get; set; }
    }
}