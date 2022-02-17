using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Smeuj.Pottekijker;

public class Config {
    public string Token   { get; set; } = string.Empty;
    public ulong  Channel { get; set; } = 0;

    public static Config FromFile(string path) {
        using var reader = new StreamReader(path);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(HyphenatedNamingConvention.Instance)
            .Build();
        var config = deserializer.Deserialize<Config>(reader);
        return config;
    }
}
