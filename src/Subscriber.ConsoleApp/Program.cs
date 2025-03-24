using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subscriber.Core.Application.Interfaces;

namespace Subscriber.ConsoleApp;

class Program
{
    static async Task Main(string[] args)
    {
        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        // Configure services
        var services = new ServiceCollection();
        services.ConfigureServices(configuration);

        // Create service provider
        var serviceProvider = services.BuildServiceProvider();

        // Get message handling service
        var messageHandlingService = serviceProvider.GetRequiredService<IMessageHandlingService>();

        // Set up cancellation token source
        using var cancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Start the service
            Console.WriteLine("Subscriber starting...");
                
            // Subscribe to default topics from config
            var defaultTopics = configuration.GetSection("Kafka:DefaultTopics").Get<string[]>() ?? Array.Empty<string>();
            foreach (var topic in defaultTopics)
            {
                messageHandlingService.Subscribe(topic);
            }
                
            await messageHandlingService.StartAsync(cancellationTokenSource.Token);
            Console.WriteLine("Subscriber started. Listening for messages...");
            Console.WriteLine("Commands: 'subscribe {topic}' to add a topic, 'exit' to quit");

            while (true)
            {
                var input = Console.ReadLine();
                if (input?.ToLower() == "exit")
                    break;

                if (input?.StartsWith("subscribe ", StringComparison.OrdinalIgnoreCase) == true)
                {
                    var topic = input.Substring("subscribe ".Length).Trim();
                    if (!string.IsNullOrEmpty(topic))
                    {
                        messageHandlingService.Subscribe(topic);
                    }
                }
            }
        }
        finally
        {
            // Stop the service
            await messageHandlingService.StopAsync();
            Console.WriteLine("Subscriber stopped.");
        }
    }
}