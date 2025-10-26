namespace WeatherRxServer.Models
{
    public class DailyWeather
    {
        public string[] Time { get; set; }
        public double[] TempMax { get; set; }
        public double[] TempMin { get; set; }
        public double[] UvIndexMax { get; set; }
    }
}