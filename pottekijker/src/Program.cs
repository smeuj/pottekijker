using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Smeuj.Pottekijker;
using LiteDB;
using System.Linq.Expressions;

const string DatabasePath = "voorraadkast.db";

Console.Title = "Pottekijker";

if (args.Length >= 1 && args[0] is "show") {
    int limit = 10;
    if (args.Length >= 2)
        int.TryParse(args[1], out limit);
    using var db = new LiteDatabase(DatabasePath);
    var smeuj = db.GetCollection<Smeu>("smeuj");
    
    Expression<Func<Smeu, int>> score =
        (Smeu smeu) => smeu.Pindakaas - smeu.Levertraan;
    var smeuigste = smeuj.Query()
        .OrderByDescending(score)
        .Where(smeu => smeu.Pindakaas != 0).Limit(limit).ToList();
    var smerigste = smeuj.Query()
        .OrderBy(score)
        .Where(smeu => smeu.Levertraan != 0).Limit(limit).ToList();

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
    var batches = channel!.GetMessagesAsync(limit: int.MaxValue);
    
    using var db = new LiteDatabase(DatabasePath);
    var smeuj = db.GetCollection<Smeu>("smeuj");

    await foreach (var messages in batches) {
        foreach (var message in messages)
            StorePotten(message, smeuj);
        Console.WriteLine("Batch finished.");
    }        

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
