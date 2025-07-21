// ReSharper disable MoveLocalFunctionAfterJumpStatement
// ReSharper disable UnusedAutoPropertyAccessor.Local

using FluentAssertions;
using ModelMigrator.Services;
using ModelMigrator.Test.Helpers;
using OneOf;
using System.Text.Json;
using Xunit.Abstractions;

namespace ModelMigrator.Test.Features.SimpleMigration;

public class AddNewProperty(ITestOutputHelper testOutputHelper)
{
    public record ExampleModelA : ModelBase, IModelVersioned<ExampleModelA>
    {
        public static string Mv { get => nameof(ExampleModelA); }
        public override string ModelVersion { get; } = Mv;

        public required int IntValue { get; init; }
    }

    public record ExampleModelB : ModelBase, IModelVersioned<ExampleModelB>
    {
        public static string Mv { get => nameof(ExampleModelB); }
        public override string ModelVersion { get; } = Mv;

        public required int IntValue { get; init; }
        public required string StringValue { get; init; }
    }

    private static ExampleModelA GetExampleModelA => new()
    {
        IntValue = 10
    };

    private static ExampleModelB GetExampleModelB => new()
    {
        IntValue = 10,
        StringValue = "some string"
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
            .CreateJsonParser(JsonSerializerOptions.Default);

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
                resultModelA!.IntValue.Should().Be(exampleModelA.IntValue);
            },
            exampleModelB =>
            {
                resultModelA.Should().BeNull();
                resultModelB.Should().NotBeNull();
                resultModelB!.IntValue.Should().Be(exampleModelB.IntValue);
            });
    }
}
