using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Mjcheetham.Git.Policy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolicyController : ControllerBase
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IPolicyStore _db;

        public PolicyController(ILogger<ProfileController> logger, IPolicyStore db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet]
        [Route("{id}")]
        public ActionResult<Policy> Get(string id)
        {
            Policy? policy = _db.GetPolicy(id);
            if (policy is null)
            {
                return NotFound();
            }

            return policy;
        }
    }
}
