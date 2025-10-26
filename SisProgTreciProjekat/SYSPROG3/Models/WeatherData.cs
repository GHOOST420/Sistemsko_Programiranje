using System;

namespace WeatherRxServer.Models
{
    public class WeatherData
    {
        public string City { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DailyWeather Daily { get; set; }
        public double AvgTemp { get; set; }
        public double MinTemp { get; set; }
        public double MaxTemp { get; set; }
        public double AvgUvIndex { get; set; }
        public DateTime FetchedAt { get; set; }
    }
}