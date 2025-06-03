using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;

namespace ServiceLibrary.Services.Repositories
{
    public class GPiliTerminalMachineRepository : IGPiliTerminalMachine
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

        public Task<bool> IsTrainMode()
        {
            throw new NotImplementedException();
        }

        public Task<(bool IsValid, string Message)> ValidateTerminalExpiration()
        {
            throw new NotImplementedException();
        }
    }
}
