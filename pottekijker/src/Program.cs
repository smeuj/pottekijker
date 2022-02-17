using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Smeuj.Pottekijker;
using LiteDB;

const string DatabasePath = "voorraadkast.db";

Console.Title = "Pottekijker";

if (args.Length == 1 && args[0] is "show") {
    using var db = new LiteDatabase(DatabasePath);
    var smeuj = db.GetCollection<Smeu>("smeuj");
    var smeuigste = smeuj.Query().OrderByDescending(smeu => smeu.Pindakaas)
        .Where(smeu => smeu.Pindakaas != 0).Limit(10).ToList();
    var smerigste = smeuj.Query().OrderByDescending(smeu => smeu.Levertraan)
        .Where(smeu => smeu.Levertraan != 0).Limit(10).ToList();

    Console.WriteLine("======== De smeuïgste smeuj ========");
    foreach (var smeuigst in smeuigste)
        Console.WriteLine($"{smeuigst}\n");
    Console.WriteLine("======== De smerigste smeer ========");
    foreach (var smerigst in smerigste)
        Console.WriteLine($"{smerigst}\n");
    return;
}

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
    Console.WriteLine("Started");

    if (File.Exists(DatabasePath))
        File.Delete(DatabasePath);

    var channel = await client.GetChannelAsync(config.Channel) as ITextChannel;
    var batches = channel!.GetMessagesAsync();
    
    using var db = new LiteDatabase(DatabasePath);
    var smeuj = db.GetCollection<Smeu>("smeuj");

    await foreach (var messages in batches)
        foreach (var message in messages)
            StorePotten(message, smeuj);

    smeuj.EnsureIndex(smeu => smeu.Pindakaas);
    smeuj.EnsureIndex(smeu => smeu.Levertraan);

    Console.WriteLine("Done");
};

await client.LoginAsync(TokenType.Bot, config.Token);
await client.StartAsync();

await Task.Delay(-1);

void StorePotten(IMessage message, ILiteCollection<Smeu> smeuj) {
    var reactions = message.Reactions;
    if (reactions is not null) {
        var count = (string name) => reactions
            .Where(reaction => reaction.Key.Name == name)
            .Select(reaction => reaction.Value.ReactionCount)
            .SingleOrDefault();
        smeuj.Insert(new Smeu {
            Content    = message.Content,
            Author     = message.Author.Username,
            Pindakaas  = count("smeuig"),
            Levertraan = count("blegh")
        });
    }
}

class Smeu {
    public string Content    { get; set; } = "";
    public string Author     { get; set; } = "";
    public int    Pindakaas  { get; set; }
    public int    Levertraan { get; set; }

    public override string ToString()
        => $"\"{Content}\"\n- {Author}\n{Pindakaas} pindakaas, {Levertraan} levertraan.";
}
