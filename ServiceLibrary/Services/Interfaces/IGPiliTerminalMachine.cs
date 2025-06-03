using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IGPiliTerminalMachine
    {
        Task<(bool IsValid, string Message)> ValidateTerminalExpiration();
        Task<bool> IsTerminalExpired();
        Task<bool> IsTerminalExpiringSoon();
        Task<PosTerminalInfo?> GetTerminalInfo();
        Task<bool> IsTrainMode();
        Task<bool> ChangeMode(string managerEmail);
    }
}
