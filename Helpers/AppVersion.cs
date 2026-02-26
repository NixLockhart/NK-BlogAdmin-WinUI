using System.Reflection;

namespace Blog_Manager.Helpers
{
    /// <summary>
    /// 应用版本号提供者
    /// 版本号从 git tag 自动提取，构建时由 MSBuild Target 注入到 InformationalVersion
    /// 工作流：git tag v2.0.5-alpha → dotnet build → AppVersion.Current = "v2.0.5-alpha"
    /// </summary>
    public static class AppVersion
    {
        public static string Current { get; } =
            Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion ?? "unknown";
    }
}
