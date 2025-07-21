// ReSharper disable MoveLocalFunctionAfterJumpStatement
// ReSharper disable UnusedMember.Local

using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using ModelMigrator.Services;
using ModelMigrator.Test.Helpers;
using OneOf;
using Xunit.Abstractions;

namespace ModelMigrator.Test.Features.SimpleMigration;

public class ChangeTypeOrRename(ITestOutputHelper testOutputHelper)
{
    public enum EnumA
    {
        A, B, C
    }

    public enum EnumB
    {
        X, Y, Z
    }

    public record ExampleModelA : ModelBase, IModelVersioned<ExampleModelA>
    {
        public static string Mv { get => nameof(ExampleModelA); }
        public override string ModelVersion { get; } = Mv;

        public required EnumA Enum { get; init; }
    }

    public record ExampleModelB : ModelBase, IModelVersioned<ExampleModelB>
    {
        public static string Mv { get => nameof(ExampleModelB); }
        public override string ModelVersion { get; } = Mv;

        public required EnumB Enum { get; init; }
    }

    private static ExampleModelA GetExampleModelA => new()
    {
        Enum = EnumA.A
    };

    private static ExampleModelB GetExampleModelB => new()
    {
        Enum = EnumB.X
    };

    public static IEnumerable<object[]> Combinations =>
        from o1 in new ModelBase[] { GetExampleModelA, GetExampleModelB }
        select new object[] { o1 };

    [Theory]
    [MemberData(nameof(Combinations))]
    public async Task WhenAddingNewProperty_SaveStoresProperVersion_AndLoadIsFullyCompatible(
        OneOf<ExampleModelA, ExampleModelB> exampleModel)
    {
        // Arrange
        var inMemoryDb = new InMemoryDb();

        var jsonMigrator = new RedisModelManager.DoubleArg<ExampleModelA, ExampleModelB>()
            .CreateJsonParser(new JsonSerializerOptions
            {
                Converters = { new JsonStringEnumConverter() }
            });

        // Act
        await jsonMigrator.Save(
            exampleModel,
            inMemoryDb.Save
        );

        var result = await jsonMigrator
            .Load(() => Task.FromResult(inMemoryDb.Value!));

        // Assert
        ExampleModelA? resultModelA = null;
        ExampleModelB? resultModelB = null;

        result.Switch(
            exampleModelA =>
            {
                resultModelA = exampleModelA;
            },
            exampleModelB =>
            {
                resultModelB = exampleModelB;
            },
            error =>
            {
                testOutputHelper.WriteLine(error.ToString());
            });

        exampleModel.Switch(
            exampleModelA =>
            {
                resultModelB.Should().BeNull();
                resultModelA.Should().NotBeNull();
                resultModelA!.Enum.Should().Be(exampleModelA.Enum);
            },
            exampleModelB =>
            {
                resultModelA.Should().BeNull();
                resultModelB.Should().NotBeNull();
                resultModelB!.Enum.Should().Be(exampleModelB.Enum);
            });
    }
}
