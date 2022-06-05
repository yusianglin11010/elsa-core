using System.Text.Json;
using System.Text.Json.Serialization;
using Elsa.Expressions.Helpers;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Serialization.Converters;

/// <summary>
/// Serializes <see cref="Type"/> objects to a simple alias representing said type.
/// </summary>
public class VariableConverter : JsonConverter<Variable>
{
    private readonly IWellKnownTypeRegistry _wellKnownTypeRegistry;

    public VariableConverter(IWellKnownTypeRegistry wellKnownTypeRegistry)
    {
        _wellKnownTypeRegistry = wellKnownTypeRegistry;
    }

    public override Variable? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var model = JsonSerializer.Deserialize<VariableModel>(ref reader, options)!;
        var variable = Map(model);

        return variable;
    }

    public override void Write(Utf8JsonWriter writer, Variable value, JsonSerializerOptions options)
    {
        var model = Map(value);
        JsonSerializer.Serialize(writer, model, options);
    }
    
    public Variable? Map(VariableModel source)
    {
        if (!_wellKnownTypeRegistry.TryGetTypeOrDefault(source.TypeName, out var type))
            return null;

        var variableGenericType = typeof(Variable<>).MakeGenericType(type);
        var variable = (Variable)Activator.CreateInstance(variableGenericType)!;

        variable.Name = source.Name;
        variable.Value = source.Value.ConvertTo(type);

        return variable;
    }

    public IEnumerable<Variable> Map(IEnumerable<VariableModel>? source) =>
        source?
            .Select(Map)
            .Where(x => x != null)
            .Select(x => x!)
        ?? Enumerable.Empty<Variable>();

    public VariableModel Map(Variable source)
    {
        var variableType = source.GetType();
        var value = source.Value;
        var valueType = source.Value?.GetType() ?? (variableType.IsConstructedGenericType ? variableType.GetGenericArguments().FirstOrDefault() ?? typeof(object) : typeof(object));
        var valueTypeAlias = _wellKnownTypeRegistry.GetAliasOrDefault(valueType);

        var serializedValue = value.Format();

        return new VariableModel(source.Name, valueTypeAlias, serializedValue);
    }

    public IEnumerable<VariableModel> Map(IEnumerable<Variable>? source) => source?.Select(Map) ?? Enumerable.Empty<VariableModel>();

    public class VariableModel
    {
        [JsonConstructor]
        public VariableModel()
        {
        }

        public VariableModel(string name, string typeName, string? value)
        {
            Name = name;
            TypeName = typeName;
            Value = value;
        }

        public string Name { get; set; } = default!;
        public string TypeName { get; set; } = default!;
        public string? Value { get; set; }
    }
}