using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;

namespace TestData.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly DataContext _context;

        public TestController(DataContext context)
        {
            _context = context;
        }

        [HttpGet("tables")]
        public async Task<IActionResult> GetTables()
        {
            try
            {
                // Get all table names from the database
                var tables = await _context.Database.SqlQueryRaw<string>(
                    "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' AND name NOT LIKE '__EF%'")
                    .ToListAsync();

                return Ok(new
                {
                    Message = "Database connection successful",
                    Tables = tables,
                    DatabasePath = _context.Database.GetConnectionString()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Database access failed",
                    Message = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _context.Product.ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Failed to fetch products",
                    Message = ex.Message
                });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var products = await _context.User.ToListAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Error = "Failed to fetch products",
                    Message = ex.Message
                });
            }
        }
    }
} 