using Microsoft.AspNetCore.Mvc;
using ServiceLibrary.Services.Interfaces;

namespace TestData.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuth _auth;

        public AuthController(IAuth auth)
        {
            _auth = auth;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LogIn([FromBody] LogInRequest request)
        {
            var result = await _auth.LogIn(request.ManagerEmail, request.CashierEmail);
            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> LogOut([FromBody] LogOutRequest request)
        {
            var result = await _auth.LogOut(request.ManagerEmail, request.CashierEmail, request.Cash);
            return Ok(result);
        }

        [HttpGet("pending-order")]
        public async Task<IActionResult> HasPendingOrder()
        {
            var result = await _auth.HasPendingOrder();
            return Ok(result);
        }

        [HttpGet("manager-valid")]
        public async Task<IActionResult> IsManagerValid([FromQuery] string managerEmail)
        {
            var result = await _auth.IsManagerValid(managerEmail);
            return Ok(result);
        }

        [HttpGet("cashier-valid")]
        public async Task<IActionResult> IsCashierValid([FromQuery] string cashierEmail)
        {
            var result = await _auth.IsCashierValid(cashierEmail);
            return Ok(result);
        }

        [HttpPost("set-cash-in-drawer")]
        public async Task<IActionResult> SetCashInDrawer([FromBody] SetCashInDrawerRequest request)
        {
            var result = await _auth.SetCashInDrawer(request.CashierEmail, request.Cash);
            return Ok(result);
        }

        [HttpPost("cash-withdraw-drawer")]
        public async Task<IActionResult> CashWithdrawDrawer([FromBody] CashWithdrawDrawerRequest request)
        {
            var result = await _auth.CashWithdrawDrawer(request.CashierEmail, request.ManagerEmail, request.Cash);
            return Ok(result);
        }

        [HttpGet("is-cashed-drawer")]
        public async Task<IActionResult> IsCashedDrawer([FromQuery] string cashierEmail)
        {
            var result = await _auth.IsCashedDrawer(cashierEmail);
            return Ok(result);
        }

        [HttpGet("cashiers")]
        public async Task<IActionResult> GetCashiers()
        {
            var result = await _auth.GetCashiers();
            return Ok(result);
        }
    }

    // Request DTOs for POST endpoints
    public record LogInRequest(string ManagerEmail, string CashierEmail);
    public record LogOutRequest(string ManagerEmail, string CashierEmail, decimal Cash);
    public record SetCashInDrawerRequest(string CashierEmail, decimal Cash);
    public record CashWithdrawDrawerRequest(string CashierEmail, string ManagerEmail, decimal Cash);
}