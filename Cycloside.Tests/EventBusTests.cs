using System;
using System.Threading.Tasks;
using Xunit;
using Cycloside.Core;

namespace Cycloside.Tests
{
    public class EventBusTests
    {
        private readonly EventBus _eventBus;

        public EventBusTests()
        {
            _eventBus = new EventBus();
        }

        [Fact]
        public async Task Subscribe_And_Publish_Should_Work_Correctly()
        {
            // Arrange
            var receivedMessage = string.Empty;
            var receivedPayload = 0;

            _eventBus.Subscribe("test/topic", message =>
            {
                receivedMessage = message.Topic;
                if (message.Payload.ValueKind == System.Text.Json.JsonValueKind.Number)
                {
                    receivedPayload = message.Payload.GetInt32();
                }
            });

            // Act
            _eventBus.Publish("test/topic", 42);

            // Wait a bit for async processing
            await Task.Delay(10);

            // Assert
            Assert.Equal("test/topic", receivedMessage);
            Assert.Equal(42, receivedPayload);
        }

        [Fact]
        public async Task Wildcard_Subscription_Should_Work()
        {
            // Arrange
            var receivedCount = 0;

            _eventBus.Subscribe("test/*", message =>
            {
                receivedCount++;
            });

            // Act
            _eventBus.Publish("test/topic1", "data1");
            _eventBus.Publish("test/topic2", "data2");
            _eventBus.Publish("other/topic", "data3"); // Should not match

            await Task.Delay(10);

            // Assert
            Assert.Equal(2, receivedCount);
        }

        [Fact]
        public async Task Multiple_Subscribers_Should_All_Receive_Message()
        {
            // Arrange
            var receivedCount1 = 0;
            var receivedCount2 = 0;

            _eventBus.Subscribe("test/topic", _ => receivedCount1++);
            _eventBus.Subscribe("test/topic", _ => receivedCount2++);

            // Act
            _eventBus.Publish("test/topic", "test");

            await Task.Delay(10);

            // Assert
            Assert.Equal(1, receivedCount1);
            Assert.Equal(1, receivedCount2);
        }

        [Fact]
        public void Unsubscribe_Should_Stop_Receiving_Messages()
        {
            // Arrange
            var receivedCount = 0;
            var subscription = _eventBus.Subscribe("test/topic", _ => receivedCount++);

            // Act
            _eventBus.Publish("test/topic", "before unsubscribe");
            subscription.Dispose();
            _eventBus.Publish("test/topic", "after unsubscribe");

            // Assert
            Assert.Equal(1, receivedCount);
        }
    }
}
