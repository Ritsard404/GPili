using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Services;
using ServiceLibrary.Services.Interfaces;

namespace TestData.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController(DataContext _context, IPrinterService _printer, IReport report) : ControllerBase
    {

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

        [HttpGet()]
        public async Task<IActionResult> Reprint(long inv)
        {
            try
            {
                await _printer.ReprintInvoice(inv);
                return Ok();
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

        [HttpGet("[action]")]
        public async Task<IActionResult> InvoiceDocuments()
        {
            try
            {
                var result = await report.InvoiceDocuments(DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1));

                return Ok(result);
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