using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HostInfoController : ControllerBase
    {
        [HttpGet("hostName")]
        public Task<string> GetHostNameAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Environment.MachineName);
    }
}
