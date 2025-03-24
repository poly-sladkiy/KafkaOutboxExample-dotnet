using System.Text.Json;
using Confluent.Kafka;
using Subscriber.Core.Domain.Entities;
using Subscriber.Core.Domain.Interfaces;

namespace Subscriber.Infrastructure;

public class MessageProcessor : IMessageProcessor
{
	public async Task ProcessAsync(Message message)
	{
		Console.WriteLine($"Processing message from topic: {message.Topic}");
		Console.WriteLine($"Key: {message.Key}");
            
		try
		{
			// Pretty-print the JSON payload
			var jsonObject = JsonSerializer.Deserialize<JsonElement>(message.Payload);
			var formattedJson = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
                
			Console.WriteLine($"Payload: \n{formattedJson}");
			Console.WriteLine($"Received at: {message.ReceivedAt}");
			Console.WriteLine(new string('-', 50));
                
			// Simulate processing time
			await Task.Delay(500);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error processing message: {ex.Message}");
		}
	}
}
