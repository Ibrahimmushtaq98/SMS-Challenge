using SMS_Gatekeeper_Core.Interface;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System;
using SMS_Gatekeeper_Core.Model;
using Microsoft.Extensions.Logging;

namespace SMS_Gatekeeper_Core.Services
{
    public class RateLimiterService: IRateLimiterService
    {
        private readonly int _maxMessagesPerNumberPerSecond;
        private readonly int _maxMessagesPerAccountPerSecond;
        private TimeSpan _inactivityTimeout;
        private TimeSpan _cleanupInterval;

        private readonly ILogger<RateLimiterService> _logger;

        // Using In Memory storage. But over here you can use other memory storage like Redis
        // https://learn.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=net-9.0
        // Reasoning of using Concurrent Dictionary is mainly populate the in memory structure with threads
        private readonly ConcurrentDictionary<string, RateInfo> _phoneNumberRates;
        private readonly RateInfo _accountRate;

        public RateLimiterService(ILogger<RateLimiterService> logger,int maxMessagesPerNumberPerSecond, int maxMessagesPerAccountPerSecond, TimeSpan inactivityTimeout, TimeSpan cleanupInterval)
        {
            _logger = logger;
            _maxMessagesPerNumberPerSecond = maxMessagesPerNumberPerSecond;
            _maxMessagesPerAccountPerSecond = maxMessagesPerAccountPerSecond;
            _inactivityTimeout = inactivityTimeout;
            _cleanupInterval = cleanupInterval;
            _phoneNumberRates = new ConcurrentDictionary<string, RateInfo>();
            _accountRate = new RateInfo();

            _logger.LogInformation("RateLimiter initialized with PhoneNumberLimit={PhoneLimit}, AccountLimit={AccountLimit}",
                _maxMessagesPerNumberPerSecond, _maxMessagesPerAccountPerSecond);

            // Start a background task to cleanup resources
            // In the future add cancellation token as this will run indefinitely
            Task.Run(CleanupInactiveNumbersAsync);
        }

        /// <summary>
        /// Checks if the given phonenumber can be sent with the rate limited options.
        /// </summary>
        /// <param name="phoneNumber">The phoneNumber in string to determine if it can be sent or not</param>
        /// <returns>Returns a Task boolean</returns>
        public Task<bool> CanSendMessageAsync(string phoneNumber)
        {
            if (string.IsNullOrEmpty(phoneNumber))
            {
                _logger.LogWarning("Received null or empty phone number");

                return Task.FromResult(false);
            }
                

            var phoneRate = _phoneNumberRates.GetOrAdd(phoneNumber, _ => new RateInfo());
            DateTime now = DateTime.UtcNow;

            lock (phoneRate)
            {
                lock (_accountRate)
                {
                    // Reset phone number window if expired
                    if (now >= phoneRate.StartWindow.AddSeconds(1))
                    {
                        _logger.LogDebug("Resetting phone number {PhoneNumber} count", phoneNumber);

                        phoneRate.Count = 0;
                        phoneRate.StartWindow = now;
                    }

                    // Reset account window if expired
                    if (now >= _accountRate.StartWindow.AddSeconds(1))
                    {
                        _logger.LogDebug("Resetting account-wide count");

                        _accountRate.Count = 0;
                        _accountRate.StartWindow = now;
                    }

                    // Check both limits
                    bool phoneNumberAllowed = phoneRate.Count < _maxMessagesPerNumberPerSecond;
                    bool accountAllowed = _accountRate.Count < _maxMessagesPerAccountPerSecond;

                    if (phoneNumberAllowed && accountAllowed)
                    {
                        phoneRate.Count++;
                        _accountRate.Count++;
                        phoneRate.LastUsed = now;

                        _logger.LogInformation("Message allowed for {PhoneNumber}. PhoneCount={PhoneCount}, AccountCount={AccountCount}",
                            phoneNumber, phoneRate.Count, _accountRate.Count);

                        return Task.FromResult(true);
                    }
                    _logger.LogInformation("Message blocked for {PhoneNumber}. PhoneCount={PhoneCount}/{PhoneLimit}, AccountCount={AccountCount}/{AccountLimit}",
                        phoneNumber, phoneRate.Count, _maxMessagesPerNumberPerSecond, _accountRate.Count, _maxMessagesPerAccountPerSecond);
                    return Task.FromResult(false);
                }
            }
        }

        /// <summary>
        /// Cleans up any inactive number saved in memory.
        /// </summary>
        private async Task CleanupInactiveNumbersAsync()
        {
            while (true)
            {
                await Task.Delay(_cleanupInterval);
                DateTime now = DateTime.UtcNow;

                var inactiveNumbers = _phoneNumberRates
                    .Where(kvp => now - kvp.Value.LastUsed > _inactivityTimeout)
                    .Select(kvp => kvp.Key)
                    .ToList();

                if (inactiveNumbers.Any())
                {
                    _logger.LogInformation("Cleaning up {Count} inactive phone numbers", inactiveNumbers.Count);

                    foreach (var number in inactiveNumbers)
                    {
                        _logger.LogDebug("Removed inactive phone number: {PhoneNumber}", number);

                        _phoneNumberRates.TryRemove(number, out _);
                    }
                }
            }
        }
    }
}
