# TUnit Unit and Integration Testing

- The application uses TUnit for testing
- Important: TUnit tests are always `async Task`
  - `Asserts` must be `await`ed
- Test directory: `src/tests/Tasklet.Tests`
- Test setup can be done in the constructor; an instance is created per test, but `[Before(Test)]` and `[After(Test)]` hooks can also be used
- Command: `dotnet run --project src/tests/Tasklet.Tests --output detailed --disable-logo`
  - `--log-level` with Debug or Trace as needed
  - `--list-tests` to quickly explore the test cases available
  - `--treenode-filter` with a filter format: `/<assembly>/<namespace>/<class_name>/<test_name>`
    - Valid treenode filter queries including `*`, `[Category!=Performance]`, `/*/*/(Class1)|(Class2)/*`

## Philosophy for Writing Tests

- High signal over pure coverage; avoid simple write-then-read tests except in proof-of-concept scenarios
- Tests focus on invariant behavior and lock them in to prevent regressions
- Use shared setup in the constructor when possible to reduce wordiness; refactor common setup out

## Basic Test

<basic_tunit_test>

```cs
public class CalculatorTests
{
    [Test]
    public async Task Add_WithTwoNumbers_ReturnsSum()
    {
        // Arrange
        var calculator = new Calculator();

        // Act
        var result = calculator.Add(2, 3);

        // Assert
        await Assert.That(result).IsEqualTo(5);
    }
}
```

</basic_tunit_test>

<basic_test_with_arguments>

```cs
public class PathNormalizationTests
{
    [Test]
    [Arguments("/Path/With/Caps.md", "/path/with/caps.md")]
    [Arguments("/path/with/a space.md", "/path/with/a-space.md")]
    public async Task Path_Normalization_Produces_Valid_Pahts(
        string inputPath,
        string expectedNormalizedPath
    )
    {
        var normalizedPath = KbDocument.NormalizePath(inputPath);

        await Assert.That(normalizedPath).IsEqualTo(expectedNormalizedPath);
    }
}
```

</basic_test_with_arguments>

## Common Assertions

<tunit_common_assertions>

```cs
[Test]
public async Task Collection_Contains_Item()
{
    var numbers = new[] { 1, 2, 3, 4, 5 };
    await Assert.That(numbers).Contains(3);
}

[Test]
public async Task Collections_Are_Equivalent()
{
    var actual = new[] { 1, 2, 3, 4, 5 };
    var expected = new[] { 5, 4, 3, 2, 1 };
    await Assert.That(actual).IsEquivalentTo(expected);
}

[Test]
public async Task Code_Throws_Exception()
{
    await Assert.That(() => int.Parse("not a number"))
        .Throws<FormatException>();
}
```

</tunit_common_assertions>

## Good Pracices to Follow

- Use descriptive names for tests like `This_Thing_Does_This_And_Has_This_State()`
- Make related tests easy to filter for
- Use comments to describe how to filter and run the test suite and individual tests
