using AuthNet.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthNet.Data
{
    public class AuthContext: IdentityDbContext
    {
        public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

        public AuthContext(DbContextOptions<AuthContext> options): base(options)
        {

        }
    }
}
