using System;

namespace PackageDependencyAnalysis.Model
{
    public class ReleaseVersion : IComparable
    {
        public Version Version { get; }
        public string Release { get; set; }
        public bool IsPrerelease => !string.IsNullOrEmpty(Release);

        public ReleaseVersion(Version version)
        {
            Version = version ?? throw new ArgumentNullException(nameof(version));
        }

        public int CompareTo(object obj)
        {
            if (obj is ReleaseVersion r)
            {
                var diff = Version.CompareTo(r.Version);
                return diff == 0 ? string.Compare(Release, r.Release, StringComparison.InvariantCulture) : diff;
            }
            else
            {
                return 1;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            var other = (ReleaseVersion)obj;
            return Version.Equals(other.Version) && Release == other.Release;
        }

        public override int GetHashCode()
        {
            return Version.GetHashCode();
        }

        public static implicit operator Version(ReleaseVersion r) => r.Version;
        public static implicit operator ReleaseVersion(Version v) => new ReleaseVersion(v);

        public override string ToString() => string.IsNullOrEmpty(Release) ? Version.ToString() : $"{Version}-{Release}";
    }
}
