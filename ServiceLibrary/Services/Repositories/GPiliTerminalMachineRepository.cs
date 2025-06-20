using Microsoft.EntityFrameworkCore;
using ServiceLibrary.Data;
using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace ServiceLibrary.Services.Repositories
{
    public class GPiliTerminalMachineRepository(DataContext _dataContext) : IGPiliTerminalMachine
    {
        public Task<bool> ChangeMode(string managerEmail)
        {
            throw new NotImplementedException();
        }

        public Task<PosTerminalInfo?> GetTerminalInfo()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsTerminalExpired()
        {
            throw new NotImplementedException();
        }

        public Task<bool> IsTerminalExpiringSoon()
        {
            throw new NotImplementedException();
        }

        public async Task<bool> IsTrainMode()
        {
            return await _dataContext.PosTerminalInfo.Select(t => t.IsTrainMode).FirstOrDefaultAsync();
        }

        public Task<(bool IsValid, string Message)> ValidateTerminalExpiration()
        {
            throw new NotImplementedException();
        }
    }
}
