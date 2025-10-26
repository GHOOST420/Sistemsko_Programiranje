using System;
using System.Threading.Tasks;
using WeatherRxServer.Server;

namespace WeatherRxServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== Weather Rx Server ===\n");

            var server = new HttpServer("http://localhost:8080/");
            await server.StartAsync();
        }
    }
}