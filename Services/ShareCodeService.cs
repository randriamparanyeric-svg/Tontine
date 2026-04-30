namespace Tontine.Services
{
    public interface IShareCodeService
    {
        string GenerateShareCode();
    }

    public class ShareCodeService : IShareCodeService
    {
        public string GenerateShareCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var code = new string(Enumerable.Range(0, 8)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
            return code;
        }
    }
}