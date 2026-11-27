using System.Text.RegularExpressions;
using Manufacture.Models.Domain;
using Manufacture.Services;
using Xunit;

namespace Manufacture.Tests
{
    /// <summary>
    /// Guards against the SQL seeds drifting away from the code-defined RBAC
    /// defaults. Every seed file must contain exactly the same 60 role/module
    /// cells, keyed on role ID.
    /// </summary>
    public class RbacSeedScriptTests
    {
        private static readonly Regex MatrixRow = new(
            @"\(\s*N'(?<module>[^']+)'\s*,\s*(?<role>\d+)\s*,\s*N'(?<level>[^']+)'\s*\)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        [Theory]
        [InlineData("seed-rbac.sql")]
        [InlineData("seed-database.sql")]
        [InlineData("manufacture-live-schema.sql")]
        public void SqlSeedMatchesRbacDefaults(string fileName)
        {
            var path = Path.Combine(FindRepositoryRoot(), "scripts", fileName);
            Assert.True(File.Exists(path), $"Seed script not found: {path}");

            var actual = MatrixRow.Matches(File.ReadAllText(path))
                .Select(match => new
                {
                    Module = match.Groups["module"].Value,
                    RoleId = int.Parse(match.Groups["role"].Value),
                    Level = match.Groups["level"].Value
                })
                .Select(row => $"{row.Module}|{row.RoleId}|{row.Level}")
                .ToList();

            var expected = RbacDefaults.Matrix
                .Select(cell => $"{cell.ModuleKey}|{(int)cell.Role}|{cell.LevelName}")
                .ToList();

            Assert.Equal(60, actual.Count);
            Assert.Equal(actual.Count, actual.Distinct(StringComparer.OrdinalIgnoreCase).Count());
            Assert.Equal(
                expected.OrderBy(value => value, StringComparer.OrdinalIgnoreCase),
                actual.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
        }

        [Theory]
        [InlineData("seed-rbac.sql")]
        [InlineData("seed-database.sql")]
        [InlineData("manufacture-live-schema.sql")]
        public void SqlSeedUsesContractRoleIds(string fileName)
        {
            var path = Path.Combine(FindRepositoryRoot(), "scripts", fileName);
            var roleIds = MatrixRow.Matches(File.ReadAllText(path))
                .Select(match => int.Parse(match.Groups["role"].Value))
                .Distinct()
                .OrderBy(id => id)
                .ToList();

            var contractIds = Enum.GetValues<UserRole>()
                .Select(role => (int)role)
                .OrderBy(id => id)
                .ToList();

            Assert.Equal(contractIds, roleIds);
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Manufacture.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate the repository root (Manufacture.slnx).");
        }
    }
}
