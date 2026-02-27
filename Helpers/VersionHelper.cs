using System;

namespace Blog_Manager.Helpers
{
    public static class VersionHelper
    {
        /// <summary>
        /// 解析版本号字符串 "v2.0.5" / "v2.0.5-alpha" → (Major, Minor, Patch, PreRelease)
        /// </summary>
        public static (int Major, int Minor, int Patch, string? Pre) Parse(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return (0, 0, 0, null);

            var v = version.TrimStart('v', 'V');
            string? pre = null;

            var dashIndex = v.IndexOf('-');
            if (dashIndex >= 0)
            {
                pre = v.Substring(dashIndex + 1);
                v = v.Substring(0, dashIndex);
            }

            var parts = v.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out var ma) ? ma : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out var mi) ? mi : 0;
            int patch = parts.Length > 2 && int.TryParse(parts[2], out var pa) ? pa : 0;

            return (major, minor, patch, pre);
        }

        /// <summary>
        /// 比较两个版本号: a > b → 正数, a == b → 0, a &lt; b → 负数
        /// 有 pre-release 后缀的视为更旧 (v2.0.5-alpha &lt; v2.0.5)
        /// </summary>
        public static int Compare(string a, string b)
        {
            var pa = Parse(a);
            var pb = Parse(b);

            var cmp = pa.Major.CompareTo(pb.Major);
            if (cmp != 0) return cmp;

            cmp = pa.Minor.CompareTo(pb.Minor);
            if (cmp != 0) return cmp;

            cmp = pa.Patch.CompareTo(pb.Patch);
            if (cmp != 0) return cmp;

            // 都没有 pre-release → 相等
            if (pa.Pre == null && pb.Pre == null) return 0;
            // 有 pre-release 的比没有的旧
            if (pa.Pre != null && pb.Pre == null) return -1;
            if (pa.Pre == null && pb.Pre != null) return 1;
            // 都有 pre-release → 字符串比较
            return string.Compare(pa.Pre, pb.Pre, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 版本 a 是否比 b 更新
        /// </summary>
        public static bool IsNewer(string a, string b) => Compare(a, b) > 0;
    }
}
