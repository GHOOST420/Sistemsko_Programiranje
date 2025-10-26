using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WeatherRxServer.DTOs;
using WeatherRxServer.Services;

namespace WeatherRxServer.Server
{
    public class RequestHandler
    {
        private readonly WeatherService _weatherService;
        private readonly StatisticsService _statisticsService;
        private readonly IObserver<RequestLog> _logger;

        public RequestHandler(
            WeatherService weatherService,
            StatisticsService statisticsService,
            IObserver<RequestLog> logger)
        {
            _weatherService = weatherService;
            _statisticsService = statisticsService;
            _logger = logger;
        }

        public async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var sw = Stopwatch.StartNew();
            var request = context.Request;
            var response = context.Response;

            var log = new RequestLog
            {
                Timestamp = DateTime.Now,
                Method = request.HttpMethod,
                Path = request.Url.AbsolutePath
            };

            try
            {
                await HandleApiRequestAsync(request, response);

                log.StatusCode = response.StatusCode;
                log.Success = response.StatusCode < 400;
            }
            catch (Exception ex)
            {
                log.Success = false;
                log.ErrorMessage = ex.Message;
                log.StatusCode = 500;

                var errorResponse = new { error = ex.Message };
                var errorJson = JsonConvert.SerializeObject(errorResponse);
                var errorBytes = Encoding.UTF8.GetBytes(errorJson);

                response.StatusCode = 500;
                response.ContentType = "application/json";
                response.ContentLength64 = errorBytes.Length;
                await response.OutputStream.WriteAsync(errorBytes, 0, errorBytes.Length);
            }
            finally
            {
                response.OutputStream.Close();
                sw.Stop();
                log.Duration = sw.ElapsedMilliseconds;
                _logger.OnNext(log);
            }
        }

        private async Task HandleApiRequestAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            if (request.Url.AbsolutePath == "/api/weather" && request.HttpMethod == "GET")
            {
                await HandleWeatherApiAsync(request, response);
            }
            else if (request.Url.AbsolutePath == "/api/stats" && request.HttpMethod == "GET")
            {
                HandleStatsApi(response);
            }
            else
            {
                response.StatusCode = 404;
                var errorResponse = new { error = "API endpoint nije pronađen" };
                var json = JsonConvert.SerializeObject(errorResponse);
                var bytes = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        private async Task HandleWeatherApiAsync(HttpListenerRequest request, HttpListenerResponse response)
        {
            var city = request.QueryString["city"];
            var daysStr = request.QueryString["days"] ?? "7";

            if (string.IsNullOrEmpty(city))
            {
                response.StatusCode = 400;
                var errorResponse = new { error = "Parametar 'city' je obavezan" };
                var json = JsonConvert.SerializeObject(errorResponse);
                var bytes = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                return;
            }

            int days = int.TryParse(daysStr, out int d) ? d : 7;
            var weatherData = await _weatherService.GetWeatherAsync(city, days);

            if (weatherData != null)
            {
                _statisticsService.AddWeatherData(weatherData);
                response.StatusCode = 200;

                var json = JsonConvert.SerializeObject(weatherData, new JsonSerializerSettings
                {
                    ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver()
                });
                var bytes = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            else
            {
                response.StatusCode = 500;
                var errorResponse = new { error = "Greška pri preuzimanju vremenskih podataka" };
                var json = JsonConvert.SerializeObject(errorResponse);
                var bytes = Encoding.UTF8.GetBytes(json);

                response.ContentType = "application/json";
                response.ContentLength64 = bytes.Length;
                await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
        }

        private void HandleStatsApi(HttpListenerResponse response)
        {
            response.StatusCode = 200;

            var statsResponse = new { summary = _statisticsService.GetDetailedSummary() };
            var json = JsonConvert.SerializeObject(statsResponse);
            var bytes = Encoding.UTF8.GetBytes(json);

            response.ContentType = "application/json";
            response.ContentLength64 = bytes.Length;
            response.OutputStream.Write(bytes, 0, bytes.Length);
        }
    }
}