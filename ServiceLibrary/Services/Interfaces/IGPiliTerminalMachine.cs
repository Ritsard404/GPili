using ServiceLibrary.Models;

namespace ServiceLibrary.Services.Interfaces
{
    public interface IGPiliTerminalMachine
    {
        Task<(bool IsValid, string Message)> ValidateTerminalExpiration();
        Task<PosTerminalInfo?> GetTerminalInfo();
        Task<bool> IsTrainMode();
        Task<bool> ChangeMode(string managerEmail);
    }
}
