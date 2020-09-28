using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using NuGet.Versioning;
using ReferenceTrace.MSProject;
using ReferenceTrace.NuSpec;

namespace ReferenceTrace
{
    static class Extensions
    {
        public static XmlSerializer ProjectXmlSerializer = new XmlSerializer(typeof(Project));
        public static XmlSerializer PackageXmlSerializer = new XmlSerializer(typeof(Package));

        public static IEnumerable<T> RemoveNulls<T>(this IEnumerable<T> self) where T : class
        {
            return self.Where(x => x != null).Select(y => y);
        }

        public static IEnumerable<T> Complement<T>(this IEnumerable<T> self, [NotNull] IEnumerable<T> other)
        {
            return self.Where(x => !other.Contains(x));
        }
        public static IEnumerable<(T1, T2)> CartesianProduct<T1, T2>(this IEnumerable<T1> self, [NotNull] IEnumerable<T2> other)
        {
            var otherList = other.ToList();
            foreach (var t1 in self)
            foreach (var t2 in otherList)
                yield return (t1, t2);
        }


        public static void AddRange<T>(this HashSet<T> self, IEnumerable<T> other)
        {
            foreach (var element in other)
                self.Add(element);
        }

        public static NuGetVersion ToNugetVersion(this string self)
        {
            if (NuGetVersion.TryParse(self, out var version)) return version;
            
            return new NuGetVersion(0,0,0);
        }

        public static VersionRange ToVersionRange(this string self)
        {
            var range = VersionRange.Parse(self);
            return range;
        }
    }
}
