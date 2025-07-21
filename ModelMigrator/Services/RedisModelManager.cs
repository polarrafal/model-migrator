using System.Text.Json;
using System.Text.Json.Nodes;
using ModelMigrator.Models;
using OneOf;

namespace ModelMigrator.Services;

public static class RedisModelManager
{
    public class DoubleArg<Tda1, Tda2>
        where Tda1 : ModelBase, IModelVersioned<Tda1>
        where Tda2 : ModelBase, IModelVersioned<Tda2>
    {
        public Map? Mappings { get; init; }

        public JsonParser CreateJsonParser(JsonSerializerOptions jsonSerializerOptions) => new(jsonSerializerOptions);

        public record Map
        {
            public required Func<Tda1, Tda2> Mapping1To2Func { get; init; }
            public required Func<Tda2, Tda1> Mapping2To1Func { get; init; }
        }

        public class JsonParser(JsonSerializerOptions jsonSerializerOptions)
        {
            public async Task Save(
                OneOf<Tda1, Tda2> obj,
                Func<string, Task> storeFunc)
            {
                Task? task = null;
                obj.Switch(
                    tda1 => task = storeFunc(JsonSerializer.Serialize(tda1, jsonSerializerOptions)),
                    tda2 => task = storeFunc(JsonSerializer.Serialize(tda2, jsonSerializerOptions)));

                if (task != null)
                {
                    await task;
                }
            }

            public async Task<OneOf<Tda1, Tda2, MigratorError>> Load(
                Func<Task<string>> jsonProvider)
            {
                try
                {
                    var json = await jsonProvider();
                    var version = FindVersion(json);

                    if (TryDeserializeToVersion<Tda1>(version, json, jsonSerializerOptions, out var t1))
                    {
                        return t1!;
                    }

                    if (TryDeserializeToVersion<Tda2>(version, json, jsonSerializerOptions, out var t2))
                    {
                        return t2!;
                    }

                    return new MigratorError(ErrorReason.MissingVersion);
                }
                catch
                {
                    return new MigratorError(ErrorReason.ParsingIssue);
                }
            }

            private static string FindVersion(string json)
            {
                var node = JsonNode.Parse(json)?.AsObject();

                if (node is null ||
                    !node.TryGetPropertyValue(nameof(ModelBase.ModelVersion), out var versionNode))
                {
                    throw new InvalidOperationException($"Missing or malformed '{nameof(ModelBase.ModelVersion)}'");
                }

                var version = versionNode?.GetValue<string>()
                              ?? throw new InvalidOperationException("Version must be a string");

                return version;
            }

            private static bool TryDeserializeToVersion<T>(
                string version,
                string json,
                JsonSerializerOptions jsonSerializerOptions,
                out T? result)
                where T : ModelBase, IModelVersioned<T>
            {
                if (!version.Equals(T.Mv))
                {
                    result = null;
                    return false;
                }

                result = JsonSerializer.Deserialize<T>(json, jsonSerializerOptions);
                return true;
            }
        }
    }
}
