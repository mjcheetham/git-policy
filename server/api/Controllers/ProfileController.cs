using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Mjcheetham.Git.Policy.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ILogger<ProfileController> _logger;
        private readonly IPolicyStore _db;

        public ProfileController(ILogger<ProfileController> logger, IPolicyStore db)
        {
            _logger = logger;
            _db = db;
        }

        [HttpGet]
        public ActionResult<Profile> Get()
        {
            // TODO: select profile based on client identity
            return new Profile
            {
                Policies = _db.GetPolicies().Select(x => x.Id).ToArray()
            };
        }
    }
}
