using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using SMS_Gatekeeper.Model;
using SMS_Gatekeeper_Core.Interface;
using System.Threading.RateLimiting;

namespace SMS_Gatekeeper.Controllers
{
    [ApiController]
    [Route("api/sms")]

    public class SMSRatelimiterController : Controller
    {
        private readonly IRateLimiterService _rateLimiter;
        public SMSRatelimiterController(IRateLimiterService rateLimiter)
        {
            _rateLimiter = rateLimiter;
        }

        [HttpPost("can-send")]
        public async Task<IActionResult> CheckIfCanSendSMS([FromBody] SMSRequest request)
        {
            bool canSend = await _rateLimiter.CanSendMessageAsync(request.PhoneNumber);
            return Ok(new { CanSend = canSend });
        }
    }
}
