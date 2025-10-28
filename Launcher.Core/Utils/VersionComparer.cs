using System;
using System.Collections.Generic;

namespace Launcher.Core.Utils
{
    public interface IVersionComparer
    {
        int Compare(string versionA, string versionB);
        bool IsNewerThan(string versionA, string versionB);
    }

    public sealed class VersionComparer : IVersionComparer
    {
        public int Compare(string versionA, string versionB)
        {
            if (versionA == versionB)
                return 0;

            var vA = ParseSemVer(versionA);
            var vB = ParseSemVer(versionB);

            // Comparar partes principales
            for (int i = 0; i < 3; i++)
            {
                int diff = vA.Numbers[i].CompareTo(vB.Numbers[i]);
                if (diff != 0)
                    return diff;
            }

            // Comparar prerelease (alpha, beta, rc)
            if (vA.Prerelease == vB.Prerelease)
                return 0;

            if (string.IsNullOrEmpty(vA.Prerelease))
                return 1; // vA es estable, vB es pre-release
            if (string.IsNullOrEmpty(vB.Prerelease))
                return -1; // vA es pre-release, vB es estable

            // Orden típico: alpha < beta < rc
            var order = new List<string> { "alpha", "beta", "rc" };
            int aIndex = order.IndexOf(vA.Prerelease);
            int bIndex = order.IndexOf(vB.Prerelease);
            return aIndex.CompareTo(bIndex);
        }

        public bool IsNewerThan(string versionA, string versionB)
            => Compare(versionA, versionB) > 0;

        private (int[] Numbers, string Prerelease) ParseSemVer(string input)
        {
            string[] parts = input.Split('-', 2);
            string[] nums = parts[0].Split('.');
            int[] numbers = new int[3];
            for (int i = 0; i < Math.Min(nums.Length, 3); i++)
                int.TryParse(nums[i], out numbers[i]);

            string prerelease = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;
            return (numbers, prerelease);
        }
    }
}
