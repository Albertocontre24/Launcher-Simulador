using System;

namespace Launcher.Core.Utils
{
    public static class SemVerComparer
    {
        public static bool IsRemoteNewer(string localVersion, string remoteVersion)
        {
            return Compare(remoteVersion, localVersion) > 0;
        }

        public static int Compare(string v1, string v2)
        {
            var ver1 = new VersionInfo(v1);
            var ver2 = new VersionInfo(v2);
            return ver1.CompareTo(ver2);
        }

        private class VersionInfo : IComparable<VersionInfo>
        {
            public int Major { get; }
            public int Minor { get; }
            public int Patch { get; }
            public string PreRelease { get; }

            public VersionInfo(string version)
            {
                var parts = version.Split('-', 2);
                var nums = parts[0].Split('.');

                Major = int.Parse(nums[0]);
                Minor = nums.Length > 1 ? int.Parse(nums[1]) : 0;
                Patch = nums.Length > 2 ? int.Parse(nums[2]) : 0;
                PreRelease = parts.Length > 1 ? parts[1] : null;
            }

            public int CompareTo(VersionInfo other)
            {
                if (Major != other.Major)
                    return Major.CompareTo(other.Major);
                if (Minor != other.Minor)
                    return Minor.CompareTo(other.Minor);
                if (Patch != other.Patch)
                    return Patch.CompareTo(other.Patch);

                // Pre-release (null = estable → mayor)
                if (PreRelease == null && other.PreRelease != null) return 1;
                if (PreRelease != null && other.PreRelease == null) return -1;
                if (PreRelease == null && other.PreRelease == null) return 0;

                return string.Compare(PreRelease, other.PreRelease, StringComparison.Ordinal);
            }
        }
    }
}
