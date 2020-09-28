using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DatingApp.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace DatingApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WeatherForecastController : ControllerBase
    {
        private readonly DataContext _context;
        public WeatherForecastController(DataContext context)
        {
            _context = context;
        }
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "ChillyRRR", "Cool", "Mild", "Warmw", "BalmyTest", "Hot", "Sweltering", "Scorching"
        };

        // private readonly ILogger<WeatherForecastController> _logger;

        // public WeatherForecastController(ILogger<WeatherForecastController> logger)
        // {
        //     _logger = logger;
        // }

        [HttpGet]
        public IActionResult GetValues()
        {
            var values=_context.Values.ToList();
            return Ok(values);
        }
        [HttpGet("{id}")]
        public IActionResult GetValue(int id)
        {
            var value=_context.Values.Where(x=>x.Id==id).FirstOrDefault();
            return Ok(value);
        }
    }
}
