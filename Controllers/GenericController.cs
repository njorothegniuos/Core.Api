using Core.Api.Model;
using Core.Api.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace Core.Api.Controllers
{
    [Route("v{version:apiVersion}/generic"), SwaggerOrder("A")]
    public class GenericController : ControllerBase
    {
        private readonly ILogger<GenericController> _logger;
        private readonly IConfiguration _configuration;
        public GenericController(ILogger<GenericController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        #region nonce
        /// <summary>
        ///  Retrieve nonce
        /// </summary>
        /// <returns></returns>
        [HttpGet, Route("retrieve")]
        [Produces(MediaTypeNames.Application.Json), Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ResponseObject<string>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> RetrieveNonce()
        {
            Utility _utility = new Utility();

            var nonce = _utility.GetRequest(_configuration["API_EndPoint"]);

            if(!string.IsNullOrEmpty(nonce))
                return Ok(nonce);
            else
                return Ok("Unable to retrieve value!");
        }

        #endregion
    }
}
