// ReSharper disable MoveLocalFunctionAfterJumpStatement

using System.Text.Json;
using FluentAssertions;
using ModelMigrator.Services;
using ModelMigrator.Test.Helpers;
using OneOf;
using Xunit.Abstractions;

namespace ModelMigrator.Test.Features.ComplexMigration;

public class SeparateExecutionPaths(ITestOutputHelper testOutputHelper)
{
    public record ExampleModelA : ModelBase, IModelVersioned<ExampleModelA>
    {
        public static string Mv { get => nameof(ExampleModelA); }
        public override string ModelVersion { get; } = Mv;

        public required int Value { get; init; }
    }

    public record ExampleModelB : ModelBase, IModelVersioned<ExampleModelB>
    {
        public static string Mv { get => nameof(ExampleModelB); }
        public override string ModelVersion { get; } = Mv;

        public required string Value { get; init; }
    }

    private static ExampleModelA GetExampleModelA => new()
    {
        Value = 10
    };

    private static ExampleModelB GetExampleModelB => new()
    {
        Value = "10"
    };

    public static IEnumerable<object[]> Combinations =>
        from o2 in new ModelBase[] { GetExampleModelA, GetExampleModelB }
        select new object[] { o2 };

    [Theory]
    [MemberData(nameof(Combinations))]
    public async Task WhenAddingNewProperty_SaveStoresProperVersion_AndLoadIsFullyCompatible(
        OneOf<ExampleModelA, ExampleModelB> exampleModel)
    {
        // Arrange
        var inMemoryDb = new InMemoryDb();

        var migrator = new RedisModelManager.DoubleArg<ExampleModelA, ExampleModelB>
        {
            Mappings = new RedisModelManager.DoubleArg<ExampleModelA, ExampleModelB>.Map
            {
                Mapping1To2Func = a => new ExampleModelB
                {
                    Value = a.Value.ToString()
                },
                Mapping2To1Func = b => new ExampleModelA
                {
                    Value = int.Parse(b.Value)
                }
            }
        };

        var jsonMigrator = migrator.CreateJsonParser(JsonSerializerOptions.Default);

        // Act
        await jsonMigrator.Save(
            exampleModel,
            inMemoryDb.Save
        );

        var result = await jsonMigrator
            .Load(() => Task.FromResult(inMemoryDb.Value!));

        // Assert
        result.Switch(
            exampleModelA =>
            {
                exampleModelA.Should().BeEquivalentTo(GetExampleModelA);
                exampleModelA.Should().BeEquivalentTo(migrator.Mappings.Mapping2To1Func(GetExampleModelB));
            },
            exampleModelB =>
            {
                exampleModelB.Should().BeEquivalentTo(GetExampleModelB);
                exampleModelB.Should().BeEquivalentTo(migrator.Mappings.Mapping1To2Func(GetExampleModelA));
            },
            error =>
            {
                testOutputHelper.WriteLine(error.ToString());
            });
    }
}
