using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace ServiceLibrary.Services.Repositories
{
    public class GPiliTerminalMachineRepository(DataContext _dataContext, IAuth _auth) : IGPiliTerminalMachine
    {
        public async Task<bool> ChangeMode(string managerEmail)
        {
            var manager = await _auth.IsManagerValid(managerEmail);

            if (!manager.isSuccess)
                return false;

            var posTerminalInfo = await _dataContext.PosTerminalInfo.FirstOrDefaultAsync();
            if (posTerminalInfo == null)
            {
                throw new InvalidOperationException("POS Terminal Info not found");
            }

            posTerminalInfo.IsTrainMode = !posTerminalInfo.IsTrainMode;

            _dataContext.AuditLog.Add(new AuditLog
            {
                Manager = manager.manager,
                Action = "Change POS Mode",
                Changes = $"Changed to {(posTerminalInfo.IsTrainMode ? "Training" : "Live")} Mode"
            });

            await _dataContext.SaveChangesAsync();
            return posTerminalInfo.IsTrainMode;
        }

        public async Task<PosTerminalInfo?> GetTerminalInfo()
        {
            return await _dataContext.PosTerminalInfo
                .AsNoTracking()
                .SingleOrDefaultAsync();
        }

        private async Task<bool> IsTerminalExpired()
        {
            var terminalInfo = await GetTerminalInfo();
            if (terminalInfo == null)
            {
                return true;
            }

            return DateTime.Now > terminalInfo.ValidUntil;
        }

        private async Task<bool> IsTerminalExpiringSoon()
        {
            var terminalInfo = await GetTerminalInfo();
            if (terminalInfo == null)
            {
                return false;
            }

            var oneWeekFromNow = DateTime.Now.AddDays(7);
            return DateTime.Now <= terminalInfo.ValidUntil && terminalInfo.ValidUntil <= oneWeekFromNow;
        }

        public async Task<bool> IsTrainMode()
        {
            return await _dataContext.PosTerminalInfo.Select(t => t.IsTrainMode).FirstOrDefaultAsync();
        }

        public async Task<(bool IsValid, string Message)> ValidateTerminalExpiration()
        {
            var terminalInfo = await GetTerminalInfo();
            if (terminalInfo == null)
            {
                return (false, "POS terminal is not configured.");
            }

            if (await IsTerminalExpired())
            {
                return (false, "POS terminal has expired. Please contact your administrator.");
            }

            if (await IsTerminalExpiringSoon())
            {
                var remainingDays = (terminalInfo.ValidUntil - DateTime.Now).TotalDays;
                var daysLeft = Math.Ceiling(remainingDays);

                return (true, $"Warning: POS terminal will expire in {daysLeft} day{(daysLeft == 1 ? "" : "s")}. Please contact your administrator.");

            }

            return (true, string.Empty);
        }

        public async Task<(bool IsSuccess, string Message)> SetPosTerminalInfo(PosTerminalInfo posTerminalInfo)
        {
            if (posTerminalInfo is null)
                return (false, "POS terminal info cannot be null.");

            var existing = await _dataContext.PosTerminalInfo.AsTracking().SingleOrDefaultAsync();

            if (existing is null)
                await _dataContext.PosTerminalInfo.AddAsync(posTerminalInfo);
            else
                _dataContext.Entry(existing).CurrentValues.SetValues(posTerminalInfo);

            await _dataContext.SaveChangesAsync();
            return (true, "POS terminal info has been saved successfully.");
        }
    }
}
