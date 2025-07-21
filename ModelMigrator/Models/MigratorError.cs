namespace ModelMigrator.Models;

public enum ErrorReason
{
    MissingVersion,
    ParsingIssue
}

public record MigratorError(ErrorReason ErrorReason);
