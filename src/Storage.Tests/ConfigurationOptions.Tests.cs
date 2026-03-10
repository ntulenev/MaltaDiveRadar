using FluentAssertions;

using Storage.Configuration;

namespace Storage.Tests;

[Trait("Category", "Unit")]
public sealed class ConfigurationOptionsTests
{
    [Fact(DisplayName = "DiveSiteCatalogOptions initializes Sites with an empty collection")]
    public void DiveSiteCatalogOptionsInitializesSitesWithEmptyCollection()
    {
        // Arrange
        // Act
        var options = new DiveSiteCatalogOptions();

        // Assert
        options.Sites.Should().BeEmpty();
    }

    [Fact(DisplayName = "DiveSiteOptions defaults IsActive to true")]
    public void DiveSiteOptionsDefaultsIsActiveToTrue()
    {
        // Arrange
        // Act
        var options = new DiveSiteOptions();

        // Assert
        options.IsActive.Should().BeTrue();
    }

    [Fact(DisplayName = "OpenMeteoProviderOptions enables provider by default with priority 1")]
    public void OpenMeteoProviderOptionsDefaultsToEnabledWithPriorityOne()
    {
        // Arrange
        // Act
        var options = new OpenMeteoProviderOptions();

        // Assert
        options.Enabled.Should().BeTrue();
        options.Priority.Should().Be(1);
    }

    [Fact(DisplayName = "OpenWeatherProviderOptions defaults to disabled with empty API key")]
    public void OpenWeatherProviderOptionsDefaultsToDisabledWithEmptyApiKey()
    {
        // Arrange
        // Act
        var options = new OpenWeatherProviderOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.ApiKey.Should().BeEmpty();
    }

    [Fact(DisplayName = "WeatherApiProviderOptions defaults to disabled with empty API key")]
    public void WeatherApiProviderOptionsDefaultsToDisabledWithEmptyApiKey()
    {
        // Arrange
        // Act
        var options = new WeatherApiProviderOptions();

        // Assert
        options.Enabled.Should().BeFalse();
        options.ApiKey.Should().BeEmpty();
    }

    [Fact(DisplayName = "WeatherProviderPoolOptions creates nested provider option instances")]
    public void WeatherProviderPoolOptionsCreatesNestedProviderOptionInstances()
    {
        // Arrange
        // Act
        var options = new WeatherProviderPoolOptions();

        // Assert
        options.OpenMeteo.Should().NotBeNull();
        options.WeatherApi.Should().NotBeNull();
        options.OpenWeather.Should().NotBeNull();
    }

    [Fact(DisplayName = "WeatherRefreshOptions.HasAnyEnabledProvider returns false when all providers are disabled")]
    public void WeatherRefreshOptionsHasAnyEnabledProviderReturnsFalseWhenAllProvidersDisabled()
    {
        // Arrange
        var options = new WeatherRefreshOptions
        {
            Providers = new WeatherProviderPoolOptions
            {
                OpenMeteo = new OpenMeteoProviderOptions
                {
                    Enabled = false,
                },
                WeatherApi = new WeatherApiProviderOptions
                {
                    Enabled = false,
                },
                OpenWeather = new OpenWeatherProviderOptions
                {
                    Enabled = false,
                },
            },
        };

        // Act
        var result = options.HasAnyEnabledProvider();

        // Assert
        result.Should().BeFalse();
    }
}


