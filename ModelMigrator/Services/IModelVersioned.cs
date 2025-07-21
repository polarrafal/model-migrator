namespace ModelMigrator.Services;

public interface IModelVersioned<T>
    where T : IModelVersioned<T>
{
    static abstract string Mv { get; }
}
