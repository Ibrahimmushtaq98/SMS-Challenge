
using SMS_Gatekeeper_Core.Interface;
using SMS_Gatekeeper_Core.Services;
using SMS_Gatekeeper_Core.Keys;

namespace SMS_Gatekeeper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddLogging(logging =>
            {
                logging.AddConsole();
            });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var maxMessagesPerNumber = builder.Configuration.GetValue<int>(Keys.MaxPerNumberKeys);
            var maxMessagesPerAccount = builder.Configuration.GetValue<int>(Keys.MaxPerAccountKeys);
            var inactivityTimeout = builder.Configuration.GetValue<int>(Keys.InactivityTimeoutKey);
            var cleanupInterval = builder.Configuration.GetValue<int>(Keys.CleanupIntervalKey);

            builder.Services.AddSingleton<IRateLimiterService, RateLimiterService>(serviceProvider=>
            {
                var logger = serviceProvider.GetRequiredService<ILogger<RateLimiterService>>();
                return new RateLimiterService(logger,
                                                maxMessagesPerNumber, 
                                                maxMessagesPerAccount, 
                                                TimeSpan.FromSeconds(inactivityTimeout),
                                                TimeSpan.FromSeconds(cleanupInterval)
                                                );
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
