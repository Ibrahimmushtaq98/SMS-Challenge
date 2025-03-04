using Microsoft.Extensions.Logging;
using SMS_Gatekeeper_Core.Interface;
using SMS_Gatekeeper_Core.Services;
using Moq;

namespace SMS_Gatekeeper_Test
{
    public class RateLimiterServiceTests
    {
        private readonly IRateLimiterService _rateLimiter;
        private readonly Mock<ILogger<RateLimiterService>> _loggerMock;

        public RateLimiterServiceTests()
        {
            _loggerMock = new Mock<ILogger<RateLimiterService>>();
            _rateLimiter = new RateLimiterService(
                _loggerMock.Object,
                maxMessagesPerNumberPerSecond: 3,       
                maxMessagesPerAccountPerSecond: 5,      
                inactivityTimeout: TimeSpan.FromSeconds(2),
                cleanupInterval: TimeSpan.FromSeconds(1)
            );
        }


        [Fact]
        // This is to test it at normal case where the limit does not get exceeded
        // Should assert true
        public async Task CanSendMessage_WithinPhoneLimit_ReturnsTrue()
        {
            string phoneNumber = "+1234567890";
            bool result = await _rateLimiter.CanSendMessageAsync(phoneNumber);
            Assert.True(result);
        }

        [Fact]
        // This is to test when the messages exceeds the phone limit
        // Should assert false
        public async Task CanSendMessage_ExceedsPhoneLimit_ReturnsFalse()
        {
            string phoneNumber = "+1234567890";

            // Loop used to hit the limit
            for (int i = 0; i < 3; i++)
            {
                await _rateLimiter.CanSendMessageAsync(phoneNumber);
            }

            // Part of the 4th call
            bool result = await _rateLimiter.CanSendMessageAsync(phoneNumber);

            Assert.False(result);
        }

        [Fact]
        // This is to test when the messages exceeds the account limit
        // Should assert false
        public async Task CanSendMessage_ExceedsAccountLimit_ReturnsFalse()
        {
            string[] phoneNumbers = { "+111", "+222", "+333" };

            // Loop used to hit the limit
            for (int i = 0; i < 5; i++)
            {
                await _rateLimiter.CanSendMessageAsync(phoneNumbers[i % 3]);
            }

            // Part of the 6th call
            bool result = await _rateLimiter.CanSendMessageAsync("+444");

            Assert.False(result);
        }

        [Fact]
        // This is to test when the messages exceeds the account limit, when the
        // phonenumber gets reseted to be sent again
        // Should assert false, then true
        public async Task CanSendMessage_AfterWindowReset_ReturnsTrue()
        {
            string phoneNumber = "+1234567890";

            // Loop used to hit the limit
            for (int i = 0; i < 3; i++)
            {
                await _rateLimiter.CanSendMessageAsync(phoneNumber);
            }

            // This should come back as blocked
            bool blocked = await _rateLimiter.CanSendMessageAsync(phoneNumber);
            await Task.Delay(1000);

            // After the 1s wait, this should become true
            bool afterReset = await _rateLimiter.CanSendMessageAsync(phoneNumber);

            Assert.False(blocked);
            Assert.True(afterReset);
        }

        [Fact]
        // This is to test when the messages exceeds the account limit, if the cleaning interval
        // takes care of the unused resources
        // Should assert true
        public async Task CleanupInactiveNumbers_RemovesOldEntries()
        {
            string phoneNumber = "+1234567890";
            await _rateLimiter.CanSendMessageAsync(phoneNumber);

            await Task.Delay(3000);

            bool result = await _rateLimiter.CanSendMessageAsync(phoneNumber);
            Assert.True(result);
        }
    }
}