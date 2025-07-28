using System;
using System.IO;

namespace DocSpy.Source
{
    internal class UriHelper
    {
        public static void MatchUrl(string url, Action<TRoot> action)
        {
            foreach (var root in Config.Instance.Roots)
            {
                if (Uri.UnescapeDataString(url).StartsWith($"{Server.RootUrl}/{root.Name}/", StringComparison.OrdinalIgnoreCase))
                {
                    action(root);
                    return;
                }
            }
        }

        public static string GetRelativeSourcePathFromUrl(string url, TRoot root)
        {
            var BaseUrl = $"{Server.RootUrl}/{root.Name}/";
            if (!url.StartsWith(BaseUrl, StringComparison.OrdinalIgnoreCase))
            {
                return string.Empty;
            }
            var relativePath = Uri.UnescapeDataString(url)[BaseUrl.Length..].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
            if (Path.GetExtension(relativePath).StartsWith(".htm", StringComparison.OrdinalIgnoreCase))
            {
                return Path.ChangeExtension(relativePath, null);
            }
            return Path.Combine(relativePath, "index");

        }


    }
}
