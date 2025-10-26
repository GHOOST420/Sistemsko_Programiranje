using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using WeatherRxServer.Models;

namespace WeatherRxServer.Services
{
    public class StatisticsService
    {
        private readonly ConcurrentBag<WeatherData> _weatherDataCollection;
        private readonly object _lock = new object();
        private int _totalRequests;
        private readonly DateTime _serverStartTime;

        public StatisticsService()
        {
            _weatherDataCollection = new ConcurrentBag<WeatherData>();
            _totalRequests = 0;
            _serverStartTime = DateTime.Now;
        }

        public void AddWeatherData(WeatherData data)
        {
            lock (_lock)
            {
                _weatherDataCollection.Add(data);
                _totalRequests++;
            }
        }

        public string GetSummary()
        {
            lock (_lock)
            {
                var uptime = DateTime.Now - _serverStartTime;
                return $"Ukupno zahteva: {_totalRequests} | Gradova pretraženo: {_weatherDataCollection.Count} | Uptime: {uptime:hh\\:mm\\:ss}";
            }
        }

        public string GetDetailedSummary()
        {
            lock (_lock)
            {
                if (!_weatherDataCollection.Any())
                    return "Nema dostupnih podataka. Prvo napravite neki zahtev.";

                var sb = new StringBuilder();
                var uptime = DateTime.Now - _serverStartTime;

                sb.AppendLine($"Server pokrenut: {_serverStartTime:dd.MM.yyyy HH:mm:ss}");
                sb.AppendLine($"Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s");
                sb.AppendLine($"Ukupno vremenskih zahteva: {_totalRequests}");
                sb.AppendLine($"Broj različitih gradova: {_weatherDataCollection.Count}");
                sb.AppendLine();

                var allData = _weatherDataCollection.ToList();

                var globalAvgTemp = Observable.Return(allData)
                    .SelectMany(data => data)
                    .Select(d => d.AvgTemp)
                    .Average()
                    .Wait();

                var globalMinTemp = Observable.Return(allData)
                    .SelectMany(data => data)
                    .Select(d => d.MinTemp)
                    .Min()
                    .Wait();

                var globalMaxTemp = Observable.Return(allData)
                    .SelectMany(data => data)
                    .Select(d => d.MaxTemp)
                    .Max()
                    .Wait();

                var globalAvgUv = Observable.Return(allData)
                    .SelectMany(data => data)
                    .Select(d => d.AvgUvIndex)
                    .Average()
                    .Wait();

                sb.AppendLine("=== GLOBALNA STATISTIKA ===");
                sb.AppendLine($"Prosečna temperatura (svi gradovi): {globalAvgTemp:F2}°C");
                sb.AppendLine($"Najniža temperatura zabeležena: {globalMinTemp:F2}°C");
                sb.AppendLine($"Najviša temperatura zabeležena: {globalMaxTemp:F2}°C");
                sb.AppendLine($"Prosečan UV indeks: {globalAvgUv:F2}");
                sb.AppendLine();

                sb.AppendLine("=== PRETRAŽIVANI GRADOVI ===");
                foreach (var data in allData.TakeLast(10))
                {
                    sb.AppendLine($"\n{data.City}:");
                    sb.AppendLine($"  - Koordinate: {data.Latitude:F2}°N, {data.Longitude:F2}°E");
                    sb.AppendLine($"  - Prosečna temp: {data.AvgTemp:F1}°C");
                    sb.AppendLine($"  - Raspon: {data.MinTemp:F1}°C do {data.MaxTemp:F1}°C");
                    sb.AppendLine($"  - UV indeks: {data.AvgUvIndex:F1}");
                    sb.AppendLine($"  - Preuzeto: {data.FetchedAt:dd.MM.yyyy HH:mm:ss}");
                }

                return sb.ToString();
            }
        }
    }
}