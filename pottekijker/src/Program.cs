using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Smeuj.Pottekijker;

Console.Title = "Pottekijker";

Config config = Config.FromFile("config.yml");

IServiceProvider services = new ServiceCollection()
    .AddSingleton<DiscordSocketClient>(new DiscordSocketClient(
        new DiscordSocketConfig {
            GatewayIntents = GatewayIntents.GuildMessages
        })
    )
    .BuildServiceProvider();

var client = services.GetRequiredService<DiscordSocketClient>();

client.Log += async (LogMessage msg) => {
    Console.WriteLine(msg.Message);
    await Task.CompletedTask;
};

client.Ready += async () => {
    var channel = await client.GetChannelAsync(config.Channel) as ITextChannel;
    var batches = channel!.GetMessagesAsync();
    await foreach (var messages in batches) {
        foreach (var message in messages) {
            Console.WriteLine(message.Content);
            var reactions = message.Reactions;
            if (reactions is not null) {
                foreach (var reaction in reactions) {
                    var reaction_type  = reaction.Key.Name;
                    var reaction_count = reaction.Value.ReactionCount;
                    Console.WriteLine($"{reaction_type}: {reaction_count}");
                }
            }
        }
    }
};

await client.LoginAsync(TokenType.Bot, config.Token);
await client.StartAsync();

await Task.Delay(-1);
