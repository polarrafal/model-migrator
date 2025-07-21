namespace ModelMigrator.Services;

public abstract record ModelBase
{
    public abstract string ModelVersion { get; }
}
