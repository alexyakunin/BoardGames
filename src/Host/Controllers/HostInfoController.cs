using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace BoardGames.Host.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class HostInfoController : ControllerBase
    {
        [HttpGet]
        public Task<string> GetHostName(CancellationToken cancellationToken = default)
            => Task.FromResult(Environment.MachineName);
    }
}
