using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SMS_Gatekeeper_Core.Interface
{
    public interface IRateLimiterService
    {
        Task<bool> CanSendMessageAsync(string phoneNumber);
    }
}
