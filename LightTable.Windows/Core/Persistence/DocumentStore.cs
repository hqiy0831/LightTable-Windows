using System.Text.Json;

namespace LightTable.Core;

public sealed class DocumentStore
{
    public string Root { get; }
    string activeFile="table.json";
    readonly object logGate=new();
    public string DocumentPath=>Path.Combine(Root,"layouts",activeFile);
    public static readonly JsonSerializerOptions JsonOptions=new(){WriteIndented=true,PropertyNameCaseInsensitive=true};
    public DocumentStore(string? root=null)
    {
        Root=Path.GetFullPath(root??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LightTable"));
        foreach(var d in new[]{"layouts","config","cache/previews","cache/thumbnails","logs","updates"})Directory.CreateDirectory(Path.Combine(Root,d));
    }
    public TableDocument Load(out string? warning)
    {
        warning=null;
        var primary=Path.Combine(Root,"layouts","table.json");
        foreach(var path in new[]{primary,primary+".bak",Path.Combine(Root,"layouts","table.recovered.json")})
        {
            if(!File.Exists(path))continue;
            try
            {
                if(new FileInfo(path).Length>64L*1024*1024)throw new InvalidDataException("布局超过64MiB读取限制");
                var doc=JsonSerializer.Deserialize<TableDocument>(File.ReadAllText(path),JsonOptions)??throw new InvalidDataException("空布局");
                if(doc.SchemaVersion>TableDocument.CurrentSchema)
                {
                    activeFile="table.recovered.json";warning="布局来自更新版本，请升级软件后打开。当前会话另存为 table.recovered.json，不覆盖新版本布局。";return new();
                }
                doc.Sanitize();if(path.EndsWith(".bak"))warning="布局损坏，已从上次备份恢复。";
                if(path.EndsWith("table.recovered.json")){activeFile="table.recovered.json";warning="原布局无法读取，已恢复独立的安全会话。";}
                return doc;
            }
            catch(Exception ex)when(ex is IOException or JsonException or InvalidDataException or UnauthorizedAccessException){warning=$"读取布局失败：{ex.Message}。原文件和备份仍保留。";}
        }
        if(File.Exists(primary)||File.Exists(primary+".bak"))
        {
            activeFile="table.recovered.json";
            warning+=" 新会话将另存为 table.recovered.json，不覆盖损坏原件。";
        }
        return new();
    }
    public void Save(TableDocument doc)
    {
        doc.SaveActiveCollection();
        var bytes=JsonSerializer.SerializeToUtf8Bytes(doc,JsonOptions);
        _=JsonSerializer.Deserialize<TableDocument>(bytes,JsonOptions)??throw new InvalidDataException("布局序列化失败");
        var temp=DocumentPath+"."+Guid.NewGuid().ToString("N")+".tmp";
        try
        {
            using(var f=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough)){f.Write(bytes);f.Flush(true);}
            if(File.Exists(DocumentPath))File.Replace(temp,DocumentPath,DocumentPath+".bak",true);else File.Move(temp,DocumentPath);
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
    public string Import(TableDocument document,ReadOnlySpan<byte> originalMacBytes)
    {
        var archive=Path.Combine(Root,"layouts","imports",DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(archive);
        using(var stream=new FileStream(Path.Combine(archive,"macOS-layout-v1.json"),FileMode.CreateNew,FileAccess.Write,FileShare.None,4096,FileOptions.WriteThrough))
        {stream.Write(originalMacBytes);stream.Flush(true);}
        if(File.Exists(DocumentPath))File.Copy(DocumentPath,Path.Combine(archive,"windows-before-import.json"),false);
        Save(document);return archive;
    }
    public void Log(string text){lock(logGate)File.AppendAllText(Path.Combine(Root,"logs","app.log"),$"{DateTimeOffset.Now:O} {text}\n");}
}
