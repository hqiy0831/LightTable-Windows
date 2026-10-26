using System.Text.Json;
using System.Text.RegularExpressions;

namespace LightTable.Core;

public sealed record WindowsRelease(Version Version,string Tag,string Notes,Uri Page,Uri Installer,Uri? Checksums);
public static partial class ReleasePolicy
{
    public static WindowsRelease? Find(string json,Version current)
    {
        using var doc=JsonDocument.Parse(json);
        var releases=doc.RootElement.ValueKind==JsonValueKind.Array?doc.RootElement.EnumerateArray().ToArray():[doc.RootElement];
        var candidates=new List<WindowsRelease>();
        foreach(var r in releases)
        {
            if(r.GetProperty("draft").GetBoolean()||r.GetProperty("prerelease").GetBoolean())continue;
            var tag=r.GetProperty("tag_name").GetString()??"";
            if(!Version.TryParse(tag.TrimStart('v','V'),out var version)||version<=current)continue;
            Uri? installer=null,hashes=null;
            foreach(var a in r.GetProperty("assets").EnumerateArray())
            {
                var name=a.GetProperty("name").GetString()??"";var url=a.GetProperty("browser_download_url").GetString();
                if(!Uri.TryCreate(url,UriKind.Absolute,out var uri)||!Allowed(uri))continue;
                if(InstallerName().IsMatch(name))installer=uri;
                if(name.Equals("SHA256SUMS-Windows.txt",StringComparison.OrdinalIgnoreCase))hashes=uri;
            }
            if(installer==null)continue;
            if(!Uri.TryCreate(r.GetProperty("html_url").GetString(),UriKind.Absolute,out var page)||!Allowed(page))continue;
            candidates.Add(new(version,tag,r.TryGetProperty("body",out var body)?body.GetString()??"":"",page,installer,hashes));
        }
        return candidates.MaxBy(r=>r.Version);
    }
    public static bool Allowed(Uri uri)=>uri.Scheme==Uri.UriSchemeHttps && uri.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase)&&uri.AbsolutePath.StartsWith("/fuyuan9972-svg/LightTable/",StringComparison.OrdinalIgnoreCase)&&string.IsNullOrEmpty(uri.UserInfo);
    [GeneratedRegex("^LightTable-(?:v[0-9.]+-)?Windows-Setup-x64\\.exe$",RegexOptions.IgnoreCase)]
    private static partial Regex InstallerName();
}
