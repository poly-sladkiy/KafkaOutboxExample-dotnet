// See https://aka.ms/new-console-template for more information

using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Publisher.Core.Domain.Interfaces;

namespace Publisher.ConsoleApp;

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

            // Initialize database
            InitializeDatabase(configuration.GetConnectionString("DefaultConnection")!);

            // Get publishing service
            var publishingService = serviceProvider.GetRequiredService<IPublishingService>();

            // Start outbox processor
            var cancellationTokenSource = new CancellationTokenSource();
            var processorTask = StartOutboxProcessorAsync(publishingService, cancellationTokenSource.Token);

            try
            {
                Console.WriteLine("Publisher started. Enter 'exit' to quit.");
                Console.WriteLine("Format to publish: {topic} {key} {json_payload}");
                Console.WriteLine("Example: users user-123 {\"name\":\"John Doe\",\"email\":\"john@example.com\"}");

                while (true)
                {
                    var input = Console.ReadLine();
                    if (input?.ToLower() == "exit")
                        break;

                    try
                    {
                        var parts = input.Split(' ', 3);
                        if (parts.Length < 3)
                        {
                            Console.WriteLine("Invalid format. Use: {topic} {key} {json_payload}");
                            continue;
                        }

                        var topic = parts[0];
                        var key = parts[1];
                        var jsonPayload = parts[2];
                        var payload = JsonSerializer.Deserialize<object>(jsonPayload);

                        await publishingService.EnqueueMessageAsync(topic, key, payload);
                        Console.WriteLine("Message enqueued successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                    }
                }
            }
            finally
            {
                // Cancel the token and wait for the task to complete
                cancellationTokenSource.Cancel();
                try
                {
                    // Add a timeout to avoid hanging if the task doesn't respond to cancellation
                    await Task.WhenAny(processorTask, Task.Delay(5000));
                    
                    // Check if the task completed or timed out
                    if (!processorTask.IsCompleted)
                    {
                        Console.WriteLine("Outbox processor did not exit gracefully. Forcing exit.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during shutdown: {ex.Message}");
                }
                
                // Dispose the cancellation token source
                cancellationTokenSource.Dispose();
            }
        }

        static void InitializeDatabase(string connectionString)
        {
            using var connection = new Npgsql.NpgsqlConnection(connectionString);
            connection.Open();

            var createTableCommand = @"
                CREATE TABLE IF NOT EXISTS outbox_messages (
                    id UUID PRIMARY KEY,
                    topic VARCHAR(255) NOT NULL,
                    key VARCHAR(255) NOT NULL,
                    payload TEXT NOT NULL,
                    created_at TIMESTAMP NOT NULL,
                    processed_at TIMESTAMP NULL,
                    error TEXT NULL,
                    retry_count INT NOT NULL DEFAULT 0
                );
                
                CREATE INDEX IF NOT EXISTS ix_outbox_messages_processed_at 
                ON outbox_messages (processed_at) 
                WHERE processed_at IS NULL;";

            using var command = new Npgsql.NpgsqlCommand(createTableCommand, connection);
            command.ExecuteNonQuery();
            
            Console.WriteLine("Database initialized successfully.");
        }

        static async Task StartOutboxProcessorAsync(IPublishingService publishingService, CancellationToken cancellationToken)
        {
            try
            {
                Console.WriteLine("Outbox processor started.");
                
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await publishingService.ProcessOutboxAsync();
                        
                        // Use a shorter delay and check for cancellation
                        await Task.Delay(1000, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        // Exit the loop when cancellation is requested
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in outbox processor: {ex.Message}");
                        
                        // Add a small delay to avoid tight loop in case of persistent errors
                        try
                        {
                            await Task.Delay(1000, cancellationToken);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // This is expected when cancellation is requested
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error in outbox processor: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Outbox processor stopped.");
            }
        }

    }
