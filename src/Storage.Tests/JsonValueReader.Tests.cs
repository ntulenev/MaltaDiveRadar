using System.Text.Json;

using FluentAssertions;

using Storage.Providers;

namespace Storage.Tests;

[Trait("Category", "Unit")]
public sealed class JsonValueReaderTests
{
    [Fact(DisplayName = "TryReadDouble reads numeric properties")]
    public void TryReadDoubleReadsNumericProperties()
    {
        // Arrange
        using var document = JsonDocument.Parse("""{"value": 12.5}""");
        var root = document.RootElement;

        // Act
        var value = JsonValueReader.TryReadDouble(root, "value");

        // Assert
        value.Should().Be(12.5D);
    }

    [Fact(DisplayName = "TryReadInt reads string integer properties")]
    public void TryReadIntReadsStringIntegerProperties()
    {
        // Arrange
        using var document = JsonDocument.Parse("""{"value": "180"}""");
        var root = document.RootElement;

        // Act
        var value = JsonValueReader.TryReadInt(root, "value");

        // Assert
        value.Should().Be(180);
    }

    [Fact(DisplayName = "TryReadDateTimeOffset parses and normalizes UTC timestamps")]
    public void TryReadDateTimeOffsetParsesAndNormalizesUtcTimestamps()
    {
        // Arrange
        using var document = JsonDocument.Parse("""{"time": "2026-03-10T15:00:00+02:00"}""");
        var root = document.RootElement;

        // Act
        var value = JsonValueReader.TryReadDateTimeOffset(root, "time");

        // Assert
        value.Should().Be(DateTimeOffset.Parse("2026-03-10T13:00:00+00:00"));
    }

    [Fact(DisplayName = "TryReadUnixTime parses unix seconds")]
    public void TryReadUnixTimeParsesUnixSeconds()
    {
        // Arrange
        using var document = JsonDocument.Parse("""{"dt": 1710000000}""");
        var root = document.RootElement;

        // Act
        var value = JsonValueReader.TryReadUnixTime(root, "dt");

        // Assert
        value.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1710000000));
    }

    [Fact(DisplayName = "TryReadArrayDoubleAt returns null for out-of-range index")]
    public void TryReadArrayDoubleAtReturnsNullForOutOfRangeIndex()
    {
        // Arrange
        using var document = JsonDocument.Parse("""{"values": [1.1, 2.2]}""");
        var values = document.RootElement.GetProperty("values");

        // Act
        var value = JsonValueReader.TryReadArrayDoubleAt(values, 4);

        // Assert
        value.Should().BeNull();
    }
}


