using System;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Concurrency;
using System.Threading.Tasks;
using WeatherRxServer.Services;
using WeatherRxServer.Logging;
using System.Reactive;

namespace WeatherRxServer.Server
{
    public class HttpServer
    {
        private readonly HttpListener _listener;
        private readonly WeatherService _weatherService;
        private readonly StatisticsService _statisticsService;
        private readonly RequestLogger _requestLogger;
        private readonly RequestHandler _requestHandler;
        private readonly Subject<HttpListenerContext> _requestSubject;

        public HttpServer(string prefix)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);

            _weatherService = new WeatherService();
            _statisticsService = new StatisticsService();
            _requestLogger = new RequestLogger(_statisticsService);
            _requestHandler = new RequestHandler(_weatherService, _statisticsService, _requestLogger.LogObserver);
            _requestSubject = new Subject<HttpListenerContext>();

            SetupRequestHandling();
        }

        private void SetupRequestHandling()
        {
            _requestSubject
                .ObserveOn(NewThreadScheduler.Default)
                .SelectMany(context => Observable.FromAsync(() => _requestHandler.ProcessRequestAsync(context)))
                .Subscribe();
        }

        public async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine($"Server pokrenut na: {_listener.Prefixes.ToArray()[0]}");
            Console.WriteLine("\nPritisnite Ctrl+C za zaustavljanje...\n");

            Observable.Repeat(Unit.Default)
                .SelectMany(_ => Observable.FromAsync(_listener.GetContextAsync))
                .Subscribe(
                    context =>
                    {
                        AddCorsHeaders(context);

                        if (context.Request.HttpMethod == "OPTIONS")
                        {
                            context.Response.StatusCode = 200;
                            context.Response.Close();
                            return;
                        }

                        _requestSubject.OnNext(context);
                    },
                    ex => Console.WriteLine($"Error: {ex.Message}")
                );

            await Task.Delay(-1); 
        }

        private void AddCorsHeaders(HttpListenerContext context)
        {
            context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            context.Response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
        }
    }
}
