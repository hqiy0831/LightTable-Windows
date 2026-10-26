using System.Security.Cryptography;
using System.Text.Json;

namespace LightTable.Core;

public sealed record MacPathMap(string SourceRoot,string TargetRoot);
public sealed record MacImportResult(TableDocument Document,IReadOnlyList<string> MissingPaths,int LegacyAliases);

// Reads the upstream JSONEncoder Saved shape without ever rewriting the source.
// Relocation is explicit and prefix-based. Same-named photos are not guessed.
public static class MacSavedImporter
{
    const int MaximumBytes=64*1024*1024;
    public static IReadOnlyList<string> SourceDirectories(ReadOnlyMemory<byte> bytes)
    {
        using var json=Parse(bytes);return Urls(json.RootElement).Select(MacPath).Select(p=>p[..p.LastIndexOf('/')]).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
    }
    public static MacImportResult Import(ReadOnlyMemory<byte> bytes,IReadOnlyList<MacPathMap> mappings)
    {
        using var json=Parse(bytes);var root=json.RootElement;var urls=Urls(root).Distinct(StringComparer.Ordinal).ToArray();
        var rules=mappings.Select(m=>new MacPathMap(NormalizeMac(m.SourceRoot),Path.GetFullPath(m.TargetRoot))).OrderByDescending(m=>m.SourceRoot.Length).ToArray();
        if(rules.Length==0)throw new InvalidDataException("请指定原 Mac 文件夹与 Windows 文件夹的对应关系。");
        foreach(var rule in rules)if(!TableDocument.IsLocal(rule.TargetRoot))throw new InvalidDataException("目标必须是 Windows 的绝对文件夹路径。");
        var paths=new Dictionary<string,string>(StringComparer.Ordinal);foreach(var url in urls)paths[url]=Relocate(MacPath(url),rules);
        foreach(var group in paths.GroupBy(p=>p.Value,StringComparer.OrdinalIgnoreCase))
            if(group.Select(p=>MacPath(p.Key)).Distinct(StringComparer.Ordinal).Skip(1).Any())throw new InvalidDataException($"两个不同 Mac 文件会映射为同一 Windows 文件：{group.Key}。请调整文件夹映射。");
        var aliases=LegacyAliases(urls,paths);string Resolve(string url)=>paths[aliases.GetValueOrDefault(url,url)];
        var document=new TableDocument{SourceLabel=root.GetProperty("label").GetString()??"你的照片"};
        document.WorldWidth=Number(root,"worldWidth",1180);document.WorldHeight=Number(root,"worldHeight",550);document.FitScale=Number(root,"fitScale",1);
        if(root.TryGetProperty("viewport",out var viewport)&&viewport.ValueKind==JsonValueKind.Object)
            document.Viewport=new(){Scale=Number(viewport,"scale",1),OffsetX=Number(viewport,"offsetX",0),OffsetY=Number(viewport,"offsetY",0)};
        foreach(var url in urls)document.Photos.Add(new(){Path=Resolve(url)});
        foreach(var entry in root.GetProperty("layout").GetProperty("slides").EnumerateArray())
        {
            var slide=new Slide{Id=entry.GetProperty("id").GetGuid(),Path=Resolve(entry.GetProperty("url").GetString()!),X=Number(entry,"x",.5),Y=Number(entry,"y",.5),Width=Number(entry,"width",.38),Angle=Number(entry,"angle",0)};
            document.Slides.Add(slide);
        }
        // The original migration keeps the uppermost copy's immutable identity.
        if(aliases.Count>0)document.Slides=document.Slides.AsEnumerable().Reverse().DistinctBy(s=>s.Path,StringComparer.OrdinalIgnoreCase).Reverse().ToList();
        if(root.TryGetProperty("albums",out var albums)&&albums.ValueKind==JsonValueKind.Array)
        foreach(var entry in albums.EnumerateArray())
        {
            var page=new AlbumPage{Id=entry.GetProperty("id").GetGuid(),Name=entry.GetProperty("name").GetString()??"相册页"};
            if(entry.TryGetProperty("position",out var position)&&position.ValueKind!=JsonValueKind.Null)
            {
                // Foundation encodes CGPoint as [x,y]; keyed values are accepted
                // for exported fixtures produced by other migration utilities.
                page.Position=position.ValueKind==JsonValueKind.Array&&position.GetArrayLength()==2?new(position[0].GetDouble(),position[1].GetDouble()):
                    position.ValueKind==JsonValueKind.Object?new(Number(position,"x",0),Number(position,"y",0)):throw new InvalidDataException("相册位置格式无效。");
            }
            foreach(var pocket in entry.GetProperty("pockets").EnumerateArray())
            {
                var format=pocket.GetProperty("format").GetString() switch{"135"=>FilmFormat.Film135,"120"=>FilmFormat.Film120,_=>throw new InvalidDataException("未知胶片袋规格。")};
                var p=new AlbumPocket{Id=pocket.GetProperty("id").GetGuid(),Format=format,Column=pocket.GetProperty("column").GetInt32(),Row=pocket.GetProperty("row").GetInt32()};
                if(pocket.TryGetProperty("url",out var url)&&url.ValueKind==JsonValueKind.String)p.Path=Resolve(url.GetString()!);page.Pockets.Add(p);
            }
            document.Albums.Add(page);
        }
        if(root.TryGetProperty("tableOrder",out var order)&&order.ValueKind==JsonValueKind.Array)document.TableOrder=order.EnumerateArray().Select(p=>p.GetGuid()).ToList();
        if(root.TryGetProperty("trays",out var trays)&&trays.ValueKind==JsonValueKind.Array&&trays.GetArrayLength()>0)
        {
            var ids=new HashSet<Guid>();
            foreach(var tray in trays.EnumerateArray())
            {
                var id=tray.GetProperty("id").GetGuid();if(!ids.Add(id)){id=Guid.NewGuid();ids.Add(id);}
                var name=tray.GetProperty("name").GetString()??"未命名栏目";
                document.Collections.Add(new(){Id=id,Name=name,SourceLabel=name,Paths=tray.GetProperty("urls").EnumerateArray().Select(u=>Resolve(u.GetString()!)).Distinct(StringComparer.OrdinalIgnoreCase).ToList()});
            }
            document.ActiveCollectionId=root.TryGetProperty("activeTrayID",out var active)&&active.ValueKind==JsonValueKind.String&&active.TryGetGuid(out var selected)?selected:document.Collections[0].Id;
            // Upstream uses a shared stage; Windows partitions it by membership
            // to preserve the user's explicitly requested independent workspaces.
            document.SchemaVersion=2;
        }
        document.Sanitize();
        return new(document,document.Photos.Select(p=>p.Path).Where(p=>!File.Exists(p)).ToArray(),aliases.Count);
    }
    static JsonDocument Parse(ReadOnlyMemory<byte> bytes)
    {
        if(bytes.Length>MaximumBytes)throw new InvalidDataException("布局超过64MiB读取限制。");
        var json=JsonDocument.Parse(bytes,new(){MaxDepth=64});
        try
        {
            var root=json.RootElement;
            if(root.ValueKind!=JsonValueKind.Object||!root.TryGetProperty("urls",out var urls)||urls.ValueKind!=JsonValueKind.Array||
                !root.TryGetProperty("layout",out var layout)||layout.ValueKind!=JsonValueKind.Object||!layout.TryGetProperty("slides",out var slides)||slides.ValueKind!=JsonValueKind.Array||
                !root.TryGetProperty("label",out var label)||label.ValueKind!=JsonValueKind.String)throw new InvalidDataException("这不是原版 LightTable 的 Saved 布局。");
            return json;
        }
        catch{json.Dispose();throw;}
    }
    static IEnumerable<string> Urls(JsonElement root)
    {
        foreach(var url in root.GetProperty("urls").EnumerateArray())yield return url.GetString()??throw new InvalidDataException("照片URL为空。");
        if(root.TryGetProperty("trays",out var trays)&&trays.ValueKind==JsonValueKind.Array)
        foreach(var tray in trays.EnumerateArray())foreach(var url in tray.GetProperty("urls").EnumerateArray())yield return url.GetString()??throw new InvalidDataException("栏目照片URL为空。");
        foreach(var slide in root.GetProperty("layout").GetProperty("slides").EnumerateArray())yield return slide.GetProperty("url").GetString()??throw new InvalidDataException("散片URL为空。");
        if(root.TryGetProperty("albums",out var albums)&&albums.ValueKind==JsonValueKind.Array)
        foreach(var page in albums.EnumerateArray())foreach(var pocket in page.GetProperty("pockets").EnumerateArray())
            if(pocket.TryGetProperty("url",out var url)&&url.ValueKind==JsonValueKind.String)yield return url.GetString()!;
    }
    static double Number(JsonElement value,string key,double fallback)=>value.TryGetProperty(key,out var number)&&number.ValueKind==JsonValueKind.Number&&number.TryGetDouble(out var n)?Geometry.Finite(n,fallback):fallback;
    static string MacPath(string text)
    {
        if(!Uri.TryCreate(text,UriKind.Absolute,out var uri)||uri.Scheme!="file"||uri.Host.Length!=0&&uri.Host!="localhost"||uri.Query.Length!=0||uri.Fragment.Length!=0)
            throw new InvalidDataException($"布局包含非本地照片URL：{text}");
        return NormalizeMac(Uri.UnescapeDataString(uri.AbsolutePath));
    }
    static string NormalizeMac(string path)
    {
        if(!path.StartsWith('/')||path.Contains('\\')||path.Contains('\0'))throw new InvalidDataException("Mac 来源必须为以 / 开始的绝对路径。");
        var parts=new List<string>();foreach(var part in path.Split('/',StringSplitOptions.RemoveEmptyEntries))
        {if(part==".")continue;if(part==".."){if(parts.Count==0)throw new InvalidDataException("Mac 路径越出根目录。");parts.RemoveAt(parts.Count-1);}else parts.Add(part);}
        return "/"+string.Join('/',parts);
    }
    static string Relocate(string source,MacPathMap[] rules)
    {
        var rule=rules.FirstOrDefault(m=>m.SourceRoot=="/"||source.StartsWith(m.SourceRoot+"/",StringComparison.Ordinal));
        if(rule==null)throw new InvalidDataException($"尚未为这个原文件夹指定 Windows 位置：{source[..source.LastIndexOf('/')]}");
        var relative=source[(rule.SourceRoot=="/"?1:rule.SourceRoot.Length+1)..];var parts=relative.Split('/');
        foreach(var part in parts)
        {
            var stem=part.Split('.')[0];
            if(part.Length==0||part is "." or ".."||part.IndexOfAny(Path.GetInvalidFileNameChars())>=0||part.EndsWith('.')||part.EndsWith(' ')||
                new[]{"CON","PRN","AUX","NUL","COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9","LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"}.Contains(stem,StringComparer.OrdinalIgnoreCase))
                throw new InvalidDataException($"文件名无法安全映射到 Windows：{part}");
        }
        var target=Path.GetFullPath(Path.Combine(rule.TargetRoot,Path.Combine(parts)));var prefix=Path.EndsInDirectorySeparator(rule.TargetRoot)?rule.TargetRoot:rule.TargetRoot+Path.DirectorySeparatorChar;
        if(!target.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("映射路径越出所选文件夹。");return target;
    }
    static Dictionary<string,string> LegacyAliases(string[] urls,Dictionary<string,string> paths)
    {
        static bool Legacy(string url)
        {
            var parts=MacPath(url).Split('/');if(parts.Length<4)return false;var name=parts[^2];const string prefix="com.apple.SwiftUI.Drag-";
            return parts[^3]=="Caches"&&parts[^4]=="Library"&&name.StartsWith(prefix,StringComparison.Ordinal)&&Guid.TryParse(name[prefix.Length..],out _);
        }
        var original=urls.Where(u=>!Legacy(u)).GroupBy(u=>MacPath(u).Split('/')[^1]).ToDictionary(g=>g.Key,g=>g.ToArray(),StringComparer.Ordinal);
        var result=new Dictionary<string,string>(StringComparer.Ordinal);var hashes=new Dictionary<string,byte[]>(StringComparer.OrdinalIgnoreCase);
        byte[] Hash(string path){if(hashes.TryGetValue(path,out var hash))return hash;using var stream=File.OpenRead(path);return hashes[path]=SHA256.HashData(stream);}
        foreach(var url in urls.Where(Legacy))
        {
            var cached=paths[url];if(!File.Exists(cached)||!original.TryGetValue(MacPath(url).Split('/')[^1],out var candidates))continue;
            foreach(var candidate in candidates)
            {
                var target=paths[candidate];if(!File.Exists(target))continue;
                try{if(new FileInfo(cached).Length==new FileInfo(target).Length&&Hash(cached).SequenceEqual(Hash(target))){result[url]=candidate;break;}}
                catch(Exception ex)when(ex is IOException or UnauthorizedAccessException){/* Preserve unverified references. */}
            }
        }
        return result;
    }
}
