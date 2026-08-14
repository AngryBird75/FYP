using System.Data;

namespace AspiraHub.Data
{
    // The rest of this codebase already talks to the DB with snake_case
    // POCOs (see program.education_level, user.user_id in
    // ApiOnboardingController) instead of EF Core's PascalCase convention —
    // that's the Dapper style. This factory follows the same pattern for
    // the three new tables this package adds. If your project already has
    // an IDbConnectionFactory (or an EF Core DbContext) wired up, delete
    // this file and SqlConnectionFactory.cs and point the repositories in
    // this package at whatever you already use instead.
    public interface IDbConnectionFactory
    {
        IDbConnection CreateConnection();
    }
}
