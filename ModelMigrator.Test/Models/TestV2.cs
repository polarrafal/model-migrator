using ModelMigrator.Services;

namespace ModelMigrator.Test.Models;

public record TestV2 : ModelBase, IModelVersioned<TestV2>
{
    public static string Mv { get => nameof(TestV2); }
    public override string ModelVersion { get; } = Mv;

    public required string Id { get; init; }
    public string? Text { get; init; }
}
