using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Publisher.Core.Domain.Application.Services;
using Publisher.Core.Domain.Interfaces;
using Publisher.Infrastructure.Messaging;
using Publisher.Infrastructure.Persistence;
using Publisher.Infrastructure.Persistence.Repositories;

namespace Publisher.ConsoleApp;

public static class DependencyInjection
{
	public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
	{
		// Database
		var connectionString = configuration.GetConnectionString("DefaultConnection");
		services.AddSingleton(new DbConnectionFactory(connectionString));

		// Kafka
		var kafkaBootstrapServers = configuration["Kafka:BootstrapServers"]!;
		services.AddSingleton<IMessagePublisher>(provider => new KafkaMessagePublisher(kafkaBootstrapServers));

		// Repositories
		services.AddScoped<IOutboxRepository, OutboxRepository>();

		// Services
		services.AddScoped<IPublishingService, PublishingService>();

		return services;
	}
}
