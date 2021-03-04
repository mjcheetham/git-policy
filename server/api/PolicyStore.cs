using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Mjcheetham.Git.Policy.Api
{
    public interface IPolicyStore
    {
        IEnumerable<Policy> GetPolicies();

        Policy? GetPolicy(string id);
    }

    public class SqlitePolicyStore : IPolicyStore
    {
        private readonly string _connectionString;

        public SqlitePolicyStore(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("policy");
        }

        public IEnumerable<Policy> GetPolicies()
        {
            var context = new GitPolicyContext(_connectionString);
            return context.Policies.Select(x => x.ToPolicy());
        }

        public Policy? GetPolicy(string id)
        {
            var context = new GitPolicyContext(_connectionString);
            var policy = context.Policies.FirstOrDefault(x => x.Id == id);
            return policy?.ToPolicy();
        }
    }

#pragma warning disable 8618
    public sealed class GitPolicyContext : DbContext
    {
        private readonly string _connectionString;

        public GitPolicyContext(string connectionString)
        {
            _connectionString = connectionString;
            Database.EnsureCreated();
        }

        public DbSet<Policy> Policies { get; set; }
        public DbSet<GitConfiguration> Configuration { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options) =>
            options.UseLazyLoadingProxies()
                .UseSqlite(_connectionString);

        protected override void OnModelCreating(ModelBuilder m)
        {
            m.Entity<Policy>().HasMany(x => x.Configuration)
                .WithOne(x => x.Policy)
                .HasForeignKey(x => x.PolicyId)
                .OnDelete(DeleteBehavior.Cascade);
        }

        public class Policy
        {
            [Key]
            public string Id { get; set; }
            public string Description { get; set; }
            public string MinimumVersion { get; set; }
            public string MaximumVersion { get; set; }

            public virtual List<GitConfiguration> Configuration { get; } = new();

            public Mjcheetham.Git.Policy.Policy ToPolicy() =>
                new(Id, Description)
                {
                    MinimumVersion = MinimumVersion,
                    MaximumVersion = MaximumVersion,
                    Configuration = Configuration.Select(x => x.ToGitConfiguration()).ToList()
                };
        }

        public class GitConfiguration
        {
            [Key]
            public int Id { get; set; }
            public string PolicyId { get; set; }
            public virtual Policy Policy { get; set; }
            public string Key { get; set; }
            public string Value { get; set; }

            public Mjcheetham.Git.Policy.GitConfiguration ToGitConfiguration() => new(Key!, Value);
        }
#pragma warning restore 8618
    }
}
