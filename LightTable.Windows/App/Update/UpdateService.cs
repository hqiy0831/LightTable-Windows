using System.Net;
using System.Security.Cryptography;
using LightTable.Core;

namespace LightTable.Windows;

sealed class UpdateService(DocumentStore storage)
{
    static readonly HttpClient http=CreateClient();
    static readonly SemaphoreSlim checkLock=new(1);
    static bool launchChecked;
    static HttpClient CreateClient()
    {
        var client=new HttpClient(new HttpClientHandler{AutomaticDecompression=DecompressionMethods.All}){Timeout=TimeSpan.FromSeconds(30)};
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LightTable-Windows/0.1.72.1");return client;
    }
    public async Task<WindowsRelease?> CheckAsync(bool manual,bool periodic=false)
    {
        await checkLock.WaitAsync();
        try
        {
            if(!manual&&!periodic&&launchChecked)return null;
            if(!manual&&!periodic)launchChecked=true;
            var json=await http.GetStringAsync("https://api.github.com/repos/fuyuan9972-svg/LightTable/releases?per_page=100");
            return ReleasePolicy.Find(json,new Version(0,1,72,1));
        }
        finally{checkLock.Release();}
    }
    public async Task<string> DownloadAsync(WindowsRelease release,IProgress<double>? progress=null)
    {
        if(!ReleasePolicy.Allowed(release.Installer)||release.Version<=new Version(0,1,72,1))throw new InvalidDataException("更新地址或版本无效");
        var file=Path.Combine(storage.Root,"updates",$"LightTable-{release.Version}-Windows-Setup-x64.exe");var temp=file+".partial";
        try
        {
            using var response=await http.GetAsync(release.Installer,HttpCompletionOption.ResponseHeadersRead);response.EnsureSuccessStatusCode();
            if(response.RequestMessage?.RequestUri?.Scheme!="https")throw new InvalidDataException("更新下载必须使用 HTTPS");
            var total=response.Content.Headers.ContentLength;
            if(total>2L*1024*1024*1024)throw new InvalidDataException("更新包异常过大");
            await using(var source=await response.Content.ReadAsStreamAsync())await using(var output=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None,65536,true))
            {
                var buffer=new byte[65536];long done=0;int n;
                while((n=await source.ReadAsync(buffer))>0){done+=n;if(done>2L*1024*1024*1024)throw new InvalidDataException("更新包超过大小限制");await output.WriteAsync(buffer.AsMemory(0,n));if(total>0)progress?.Report((double)done/total.Value);}
                if(total is {} expected&&done!=expected)throw new InvalidDataException("下载不完整");await output.FlushAsync();
            }
            if(release.Checksums!=null)
            {
                var text=await http.GetStringAsync(release.Checksums);var name=Path.GetFileName(release.Installer.AbsolutePath);
                var line=text.Split('\n').FirstOrDefault(l=>l.TrimEnd().EndsWith(name,StringComparison.Ordinal));
                var expected=line?.Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                await using var stream=File.OpenRead(temp);var actual=Convert.ToHexString(await SHA256.HashDataAsync(stream));
                if(expected==null||!actual.Equals(expected,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("更新包 SHA256 校验失败");
            }
            File.Move(temp,file,true);return file;
        }
        finally{if(File.Exists(temp))File.Delete(temp);}
    }
}
