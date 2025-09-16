using Xunit;
using DesktopMemo.Core.Services;
using DesktopMemo.Core.Interfaces;

namespace DesktopMemo.Tests.Core.Services
{
    public class SettingsServiceTests
    {
        private readonly SettingsService _settingsService;

        public SettingsServiceTests()
        {
            _settingsService = new SettingsService();
        }

        [Fact]
        public void GetSetting_WithExistingKey_ShouldReturnValue()
        {
            // Arrange
            var key = "TestKey";
            var value = "TestValue";
            _settingsService.SetSetting(key, value);

            // Act
            var result = _settingsService.GetSetting<string>(key, "DefaultValue");

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void GetSetting_WithNonExistingKey_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "NonExistingKey";
            var defaultValue = "DefaultValue";

            // Act
            var result = _settingsService.GetSetting<string>(key, defaultValue);

            // Assert
            Assert.Equal(defaultValue, result);
        }

        [Fact]
        public void SetSetting_ShouldTriggerSettingChangedEvent()
        {
            // Arrange
            SettingChangedEventArgs? eventArgs = null;
            _settingsService.SettingChanged += (sender, args) => eventArgs = args;

            var key = "TestKey";
            var value = "TestValue";

            // Act
            _settingsService.SetSetting(key, value);

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(key, eventArgs.Key);
            Assert.Equal(value, eventArgs.Value);
        }

        [Theory]
        [InlineData("StringValue", "StringValue")]
        [InlineData(42, 42)]
        [InlineData(3.14, 3.14)]
        [InlineData(true, true)]
        public void GetSetting_WithDifferentTypes_ShouldReturnCorrectValue<T>(T value, T expected)
        {
            // Arrange
            var key = "TypedKey";
            _settingsService.SetSetting(key, value);

            // Act
            var result = _settingsService.GetSetting<T>(key, default(T)!);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public void GetSetting_WithWrongType_ShouldReturnDefaultValue()
        {
            // Arrange
            var key = "StringKey";
            _settingsService.SetSetting(key, "StringValue");

            // Act
            var result = _settingsService.GetSetting<int>(key, 42);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public async Task SaveSettingsAsync_ShouldPersistSettings()
        {
            // Arrange
            var key = "PersistentKey";
            var value = "PersistentValue";
            _settingsService.SetSetting(key, value);

            // Act
            await _settingsService.SaveSettingsAsync();

            // Create new instance to test persistence
            var newSettingsService = new SettingsService();
            await newSettingsService.LoadSettingsAsync();

            // Assert
            var result = newSettingsService.GetSetting<string>(key, "DefaultValue");
            Assert.Equal(value, result);
        }

        [Fact]
        public async Task LoadSettingsAsync_WithNoFile_ShouldInitializeDefaults()
        {
            // Act
            await _settingsService.LoadSettingsAsync();

            // Assert default values
            Assert.True(_settingsService.GetSetting("ShowExitPrompt", false));
            Assert.True(_settingsService.GetSetting("ShowDeletePrompt", false));
            Assert.Equal(0.1, _settingsService.GetSetting("BackgroundOpacity", 0.0));
            Assert.Equal("Desktop", _settingsService.GetSetting("TopmostMode", ""));
        }
    }
}