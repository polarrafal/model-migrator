namespace ModelMigrator.Test.Helpers;

public class InMemoryDb
{
    public string? Value { get; private set; }

    public Task Save(string value) => Task.FromResult(Value = value);
}
