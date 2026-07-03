using AspiraHub.Repositories;

namespace AspiraHub.Services
{
    public class UniqueKeyService
    {
        private readonly IUserRepository _repo;
        private readonly Random _rng = new Random();
        private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        public UniqueKeyService(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<string> GenerateAsync()
        {
            string key;
            int attempts = 0;

            do
            {
                key = $"ASP-{RandomSegment()}-{RandomSegment()}";
                attempts++;

                // ✅ Safety net — infinite loop na ho
                if (attempts > 20)
                    throw new Exception("Unique key generation failed after 20 attempts.");
            }
            while (await _repo.UniqueKeyExistsAsync(key));

            return key;
        }

        private string RandomSegment()
        {
            return new string(Enumerable.Range(0, 4)
                .Select(_ => Chars[_rng.Next(Chars.Length)])
                .ToArray());
        }
    }
}