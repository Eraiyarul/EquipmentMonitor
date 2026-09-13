using System.Text;
using System.Text.Json;
using EquipmentMonitor.Data;
using EquipmentMonitor.Models;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;

namespace EquipmentMonitor.Services;

/// <summary>
/// Single hosted service that:
///   1. Starts an in-process MQTT broker (no external broker needed)
///   2. Subscribes to equipment/readings and pipes messages into ReadingIngestionService
///   3. Publishes simulated weather sensor readings every 15 seconds
/// </summary>
public class MqttBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<MqttBackgroundService> logger) : BackgroundService
{
    private const string Topic = "equipment/readings";
    private const int BrokerPort = 1883;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // ── 1. Start in-process broker ───────────────────────────────────────
        var factory = new MqttFactory();

        var serverOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(BrokerPort)
            .Build();

        var broker = factory.CreateMqttServer(serverOptions);
        await broker.StartAsync();
        logger.LogInformation("MQTT broker started on port {Port}", BrokerPort);

        await Task.Delay(300, ct); // brief pause so broker is fully ready

        // ── 2. Subscriber ────────────────────────────────────────────────────
        var subscriber = factory.CreateMqttClient();
        subscriber.ApplicationMessageReceivedAsync += OnMessageReceivedAsync;

        await subscriber.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", BrokerPort)
                .WithClientId("em-subscriber")
                .Build(), ct);

        await subscriber.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(Topic)
                .Build(), ct);

        logger.LogInformation("MQTT subscriber ready on topic '{Topic}'", Topic);

        // ── 3. Simulator ─────────────────────────────────────────────────────
        var publisher = factory.CreateMqttClient();
        await publisher.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithTcpServer("127.0.0.1", BrokerPort)
                .WithClientId("em-simulator")
                .Build(), ct);

        logger.LogInformation("MQTT simulator started — publishing every 15 s");

        var rng = new Random();

        while (!ct.IsCancellationRequested)
        {
            var equipmentIds = await GetActiveEquipmentIdsAsync();

            foreach (var equipId in equipmentIds)
            {
                var metrics = BuildMetrics(rng);
                foreach (var m in metrics)
                {
                    var payload = JsonSerializer.Serialize(new
                    {
                        EquipmentId = equipId,
                        m.MetricType,
                        m.Value,
                        m.Unit,
                        Timestamp = DateTime.UtcNow
                    });

                    await publisher.PublishAsync(
                        new MqttApplicationMessageBuilder()
                            .WithTopic(Topic)
                            .WithPayload(Encoding.UTF8.GetBytes(payload))
                            .Build(), ct);

                    await Task.Delay(80, ct); // small gap between messages
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(15), ct);
        }

        await subscriber.DisconnectAsync();
        await publisher.DisconnectAsync();
        await broker.StopAsync();
    }

    private async Task OnMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            using var scope = scopeFactory.CreateScope();
            var ingestion = scope.ServiceProvider.GetRequiredService<ReadingIngestionService>();
            await ingestion.IngestAsync(json);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing MQTT message");
        }
    }

    private async Task<List<int>> GetActiveEquipmentIdsAsync()
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Equipments
            .Where(e => e.Status == EquipmentStatus.Active)
            .Select(e => e.Id)
            .ToListAsync();
    }

    // Occasionally generates out-of-range values to trigger alerts (~10% chance)
    private static IEnumerable<(string MetricType, double Value, string Unit)> BuildMetrics(Random rng)
    {
        double spike = rng.NextDouble() < 0.1 ? 1 : 0; // 10% chance of spike

        yield return ("Temperature",
            Math.Round(18 + rng.NextDouble() * 15 + spike * 30, 1), "°C");

        yield return ("Humidity",
            Math.Round(35 + rng.NextDouble() * 35 + spike * 45, 1), "%");

        yield return ("Pressure",
            Math.Round(985 + rng.NextDouble() * 40 + spike * 80, 1), "hPa");
    }
}
