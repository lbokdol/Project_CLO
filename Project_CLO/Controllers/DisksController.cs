using Microsoft.AspNetCore.Mvc;

using System.ComponentModel.DataAnnotations;

using Project_CLO.Common;
using Project_CLO.Services;

namespace Project_CLO.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DisksController : Controller
    {
        private DiskService _diskService;
        private StatisticsService _statisticsService;

        public DisksController(DiskService diskService, StatisticsService statisticsService) 
        {
            _diskService = diskService;
            _statisticsService = statisticsService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDiskList()
        {
            var diskList = _diskService.GetDiskList();
            _statisticsService.UpsertApiInformation(Request.Path, Enum.Parse<MethodType>(Request.Method));

            return Ok(diskList);
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> GetDirectoryStructures(int uid, [Required] int depth)
        {
            if (depth <= 0)
                return BadRequest($"Depth must be greater than zero");

            if (uid < 0)
                return BadRequest($"Uid must be greater than or equal to zero");

            var rootDirectory = await _diskService.GetDirectoryStructure(uid, depth);
            _statisticsService.UpsertApiInformation(Request.Path, Enum.Parse<MethodType>(Request.Method));

            return Ok(rootDirectory);
        }

        [HttpPost("{uid}")]
        public async Task<IActionResult> CreateContent(int uid, [FromBody] RequestContentInformation requestBody)
        {
            if (uid < 0)
                return BadRequest($"Uid must be greater than or equal to zero");

            if (string.IsNullOrWhiteSpace(requestBody.Path))
                return BadRequest($"Path should not be empty");

            var contentInformation = await _diskService.CreateContent(uid, requestBody);
            _statisticsService.UpsertApiInformation(Request.Path, Enum.Parse<MethodType>(Request.Method));

            return Ok(contentInformation);
        }
    }
}
