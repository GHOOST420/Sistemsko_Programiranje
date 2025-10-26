using System;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using WeatherRxServer.Models;
using WeatherRxServer.DTOs;

namespace WeatherRxServer.Services
{
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private const string GEOCODING_API = "https://geocoding-api.open-meteo.com/v1/search";
        private const string WEATHER_API = "https://api.open-meteo.com/v1/forecast";

        public WeatherService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<WeatherData> GetWeatherAsync(string city, int days = 7)
        {
            try
            {
                var coordinates = await GetCoordinatesAsync(city);
                if (coordinates == null)
                {
                    Console.WriteLine($"Grad '{city}' nije pronađen");
                    return null;
                }

                var weatherData = await GetWeatherDataAsync(coordinates.Latitude, coordinates.Longitude, days, city);
                return weatherData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri preuzimanju podataka: {ex.Message}");
                return null;
            }
        }

        private async Task<GeocodingResponse?> GetCoordinatesAsync(string city)
        {
            var url = $"{GEOCODING_API}?name={Uri.EscapeDataString(city)}&count=1&language=en&format=json";

            try
            {
                var response = await _httpClient.GetStringAsync(url);
                var json = JObject.Parse(response);

                if (json["results"] != null && json["results"].Any())
                {
                    var result = json["results"][0];
                    return new GeocodingResponse
                    {
                        Latitude = result["latitude"].Value<double>(),
                        Longitude = result["longitude"].Value<double>()
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri geokodiranju: {ex.Message}");
            }

            return null;
        }

        private async Task<WeatherData> GetWeatherDataAsync(double lat, double lon, int days, string city)
        {
            var url = $"{WEATHER_API}?latitude={lat}&longitude={lon}" +
                      $"&daily=temperature_2m_max,temperature_2m_min,uv_index_max" +
                      $"&forecast_days={Math.Min(days, 16)}" +
                      $"&timezone=auto";

            try
            {
                var weatherData = await Observable
                    .FromAsync(() => _httpClient.GetStringAsync(url))
                    .Timeout(TimeSpan.FromSeconds(15))
                    .ObserveOn(TaskPoolScheduler.Default)
                    .Select(response => ParseWeatherData(response, lat, lon, city))
                    .FirstAsync();

                return weatherData;
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Zahtev prema API-ju je istekao (timeout)");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri preuzimanju vremenskih podataka: {ex.Message}");
                return null;
            }
        }

        private WeatherData ParseWeatherData(string jsonResponse, double lat, double lon, string city)
        {
            var json = JObject.Parse(jsonResponse);
            var daily = json["daily"];

            var times = daily["time"].Select(t => t.Value<string>()).ToArray();
            var tempMax = daily["temperature_2m_max"].Select(t => t.Value<double>()).ToArray();
            var tempMin = daily["temperature_2m_min"].Select(t => t.Value<double>()).ToArray();
            var uvIndex = daily["uv_index_max"].Select(u => u.Value<double>()).ToArray();

            var avgTemp = Observable.Return(tempMax.Concat(tempMin))
                .SelectMany(temps => temps)
                .Average()
                .Wait();

            var minTemp = Observable.Return(tempMin)
                .SelectMany(temps => temps)
                .Min()
                .Wait();

            var maxTemp = Observable.Return(tempMax)
                .SelectMany(temps => temps)
                .Max()
                .Wait();

            var avgUv = Observable.Return(uvIndex)
                .SelectMany(uv => uv)
                .Average()
                .Wait();

            return new WeatherData
            {
                City = city,
                Latitude = lat,
                Longitude = lon,
                Daily = new DailyWeather
                {
                    Time = times,
                    TempMax = tempMax,
                    TempMin = tempMin,
                    UvIndexMax = uvIndex
                },
                AvgTemp = avgTemp,
                MinTemp = minTemp,
                MaxTemp = maxTemp,
                AvgUvIndex = avgUv,
                FetchedAt = DateTime.Now
            };
        }
    }
}