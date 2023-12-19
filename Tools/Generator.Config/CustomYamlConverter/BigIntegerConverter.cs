using System.Numerics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.Utilities;

namespace GoPlay.Generators.Config.CustomYamlConverter;

public sealed class BigIntegerConverter : IYamlTypeConverter
{
    // Unfortunately the API does not provide those in the ReadYaml and WriteYaml
    // methods, so we are forced to set them after creation.
    public IValueSerializer ValueSerializer { get; set; }
    public IValueDeserializer ValueDeserializer { get; set; }
    
    public bool Accepts(Type type)
    {
        return type == typeof(BigInteger);
    }

    public object? ReadYaml(IParser parser, Type type)
    {
        var val = (string)ValueDeserializer.DeserializeValue(parser, typeof(string), new SerializerState(), ValueDeserializer);

        if (string.IsNullOrEmpty(val) || !BigInteger.TryParse(val, out var bi)) return BigInteger.Zero;
        return bi;
    }

    public void WriteYaml(IEmitter emitter, object? value, Type type)
    {
        var val = (BigInteger)value;
        ValueSerializer.SerializeValue(emitter, val.ToString(), typeof(string));
    }
}