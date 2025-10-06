using System;
using System.Threading.Tasks;
using Xunit;
using Cycloside.Core;

namespace Cycloside.Tests
{
    public class IntegrationTests
    {
        [Fact]
        public async Task EventBus_Integration_Should_Work()
        {
            // Arrange
            var eventBus = new EventBus();
            var receivedMessage = string.Empty;
            var receivedData = string.Empty;

            eventBus.Subscribe("integration/test", message =>
            {
                receivedMessage = message.Topic;
                receivedData = message.Payload.GetString();
            });

            // Act
            eventBus.Publish("integration/test", "Integration test data");

            // Wait for async processing
            await Task.Delay(10);

            // Assert
            Assert.Equal("integration/test", receivedMessage);
            Assert.Equal("Integration test data", receivedData);
        }

        [Fact]
        public async Task JsonConfig_Should_Save_And_Load()
        {
            // Arrange
            var testPath = "test-config.json";
            var testData = new { TestValue = "test", Number = 42 };

            // Act - Save
            JsonConfig.Save(testPath, testData);

            // Act - Load
            var loadedData = JsonConfig.LoadOrDefault(testPath, new { TestValue = "", Number = 0 });

            // Cleanup
            if (System.IO.File.Exists(testPath))
            {
                System.IO.File.Delete(testPath);
            }

            // Assert
            Assert.Equal("test", loadedData.TestValue);
            Assert.Equal(42, loadedData.Number);
        }

        [Fact]
        public void Basic_Platform_Components_Should_Be_Accessible()
        {
            // This test just verifies that basic platform components exist
            // and can be instantiated (testing the Core project)

            // Test EventBus
            var eventBus = new EventBus();
            Assert.NotNull(eventBus);

            // Test JsonConfig
            var testData = new { TestValue = "test", Number = 42 };
            JsonConfig.Save("Cycloside.Tests/test-config.json", testData);

            var loadedData = JsonConfig.LoadOrDefault("Cycloside.Tests/test-config.json", new { TestValue = "", Number = 0 });
            Assert.Equal("test", loadedData.TestValue);

            // Cleanup
            if (System.IO.File.Exists("Cycloside.Tests/test-config.json"))
            {
                System.IO.File.Delete("Cycloside.Tests/test-config.json");
            }
        }
    }
}
