using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using WeatherRxServer.DTOs;
using WeatherRxServer.Services;

namespace WeatherRxServer.Logging
{
    public class RequestLogger
    {
        private readonly Subject<RequestLog> _requestLogSubject;
        private readonly StatisticsService _statisticsService;

        public RequestLogger(StatisticsService statisticsService)
        {
            _requestLogSubject = new Subject<RequestLog>();
            _statisticsService = statisticsService;
            SetupLogging();
        }

        public IObserver<RequestLog> LogObserver => _requestLogSubject;

        private void SetupLogging()
        {
            _requestLogSubject
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(log =>
                {
                    var color = log.Success ? ConsoleColor.Green : ConsoleColor.Red;
                    Console.ForegroundColor = color;
                    Console.WriteLine($"[{log.Timestamp:HH:mm:ss}] {log.Method} {log.Path}");
                    Console.ResetColor();
                    Console.WriteLine($"  Status: {log.StatusCode} | Duration: {log.Duration}ms");
                    if (!string.IsNullOrEmpty(log.ErrorMessage))
                        Console.WriteLine($"  Error: {log.ErrorMessage}");
                    Console.WriteLine();
                });

            Observable.Interval(TimeSpan.FromSeconds(30))
                .ObserveOn(TaskPoolScheduler.Default)
                .Subscribe(_ =>
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("\n=== STATISTIKA ===");
                    Console.WriteLine(_statisticsService.GetSummary());
                    Console.WriteLine("==================\n");
                    Console.ResetColor();
                });
        }
    }
}