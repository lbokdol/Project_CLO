using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

using Project_CLO.Services;
using Project_CLO.Common;

namespace Project_CLO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ApiController : Controller
    {
        private StatisticsService _statisticsService;

        public ApiController(StatisticsService statisticsService) 
        {
            _statisticsService = statisticsService;
        }

        [HttpGet("call")]
        public async Task<IActionResult> GetAPICallCount([Required] string apiName, [Required] MethodType methodType)
        {
            if (string.IsNullOrWhiteSpace(apiName))
                return BadRequest("Api Name should not be empty");

            var apiInformation = await _statisticsService.GetAPIInformation(apiName, methodType);
            _statisticsService.UpsertApiInformation(Request.Path, Enum.Parse<MethodType>(Request.Method));

            return Ok(apiInformation);
        }
    }
}
