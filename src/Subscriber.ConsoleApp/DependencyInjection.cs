using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Subscriber.Core.Application.Interfaces;
using Subscriber.Core.Application.Messaging;
using Subscriber.Core.Application.Services;
using Subscriber.Core.Domain.Interfaces;
using Subscriber.Infrastructure;
using Subscriber.Infrastructure.Messaging;

namespace Subscriber.ConsoleApp;

public static class DependencyInjection
{
	public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
	{
		// Kafka
		var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"]!;
		var kafkaGroupId = configuration["Kafka:GroupId"]!;
            
		services.AddSingleton<IKafkaConsumerFactory>(provider => 
			new KafkaConsumerFactory(kafkaBootstrapServers, kafkaGroupId));

		// Services
		services.AddSingleton<IMessageProcessor, MessageProcessor>();
		services.AddSingleton<IMessageHandlingService, MessageHandlingService>();

		return services;
	}
}
