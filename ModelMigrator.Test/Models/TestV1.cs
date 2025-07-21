using ModelMigrator.Services;

namespace ModelMigrator.Test.Models;

public record TestV1 : ModelBase, IModelVersioned<TestV1>
{
    public static string Mv { get => nameof(TestV1); }
    public override string ModelVersion { get; } = Mv;

    public required Guid Id { get; init; }
    public string? Text { get; init; }
}
