using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Repositories.Monitoring
{
    public static class MenuBadges
    {
        public const string Kyc = "KYC";
        public const string WithdrawRequest = "WithdrawRequest";
        public const string FailedTransaction = "FailedTransaction";
        public const string VoiceCallRequest = "VoiceCallRequest";
    }

    public interface IMenuBadge
    {
        string Id { get; }
        string Value { get; }
    }

    public interface IMenuBadgesRepository
    {
        Task SaveBadgeAsync(string id, string value);
        Task RemoveBadgeAsync(string id);
        Task<IEnumerable<IMenuBadge>> GetBadesAsync();
    }
}
