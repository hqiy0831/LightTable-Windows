using System.Runtime.InteropServices.WindowsRuntime;
using LightTable.Windows.Imaging;
using global::Windows.Graphics.Imaging;
using global::Windows.Foundation;

namespace LightTable.Windows;

static class ImagingChecks
{
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    public static async Task<string> OrientationAndTransparency(ImageStore images,string root)
    {
        var pixels=new byte[4*3*4];for(int i=0;i<12;i++){pixels[i*4]=(byte)(i*15);pixels[i*4+1]=(byte)(i*9);pixels[i*4+2]=(byte)(240-i*14);pixels[i*4+3]=255;}
        for(ushort orientation=1;orientation<=8;orientation++)
        {
            var path=Path.Combine(root,$"orientation-{orientation}.tiff");
            using(var file=File.Create(path))using(var stream=file.AsRandomAccessStream())
            {
                var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.TiffEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,4,3,96,96,pixels);
                await encoder.BitmapProperties.SetPropertiesAsync(new BitmapPropertySet{{"System.Photo.Orientation",new BitmapTypedValue(orientation,PropertyType.UInt16)}});await encoder.FlushAsync();
            }
            var decoded=await images.GetAsync(path,256);var swap=orientation>=5;var w=swap?3:4;var h=swap?4:3;
            Require(decoded.Width==w&&decoded.Height==h,$"EXIF {orientation} 尺寸错误");Require(await images.ReadAspectAsync(path)==(double)w/h,$"EXIF {orientation} 几何方向错误");
            for(int y=0;y<h;y++)for(int x=0;x<w;x++)
            {
                var (sx,sy)=orientation switch{1=>(x,y),2=>(3-x,y),3=>(3-x,2-y),4=>(x,2-y),5=>(y,x),6=>(y,2-x),7=>(3-y,2-x),_=>(3-y,x)};
                for(int c=0;c<4;c++)Require(Math.Abs(decoded.Pixels[(y*w+x)*4+c]-pixels[(sy*4+sx)*4+c])<=1,$"EXIF {orientation} 镜像或旋转像素错误 @ {x},{y},{c}");
            }
        }
        var alpha=new byte[16*12*4];for(int i=0;i<16*12;i++)if(i%16>=8){alpha[i*4]=20;alpha[i*4+1]=15;alpha[i*4+2]=70;alpha[i*4+3]=128;}
        var transparent=Path.Combine(root,"transparent.png");
        using(var file=File.Create(transparent))using(var stream=file.AsRandomAccessStream())
        {var encoder=await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId,stream);encoder.SetPixelData(BitmapPixelFormat.Bgra8,BitmapAlphaMode.Premultiplied,16,12,96,96,alpha);await encoder.FlushAsync();}
        var result=await images.GetAsync(transparent,256);Require(result.Pixels.Zip(alpha).All(p=>Math.Abs(p.First-p.Second)<=1),"PNG透明像素或预乘alpha错误");
        return "TIFF EXIF 1–8逐像素旋转/镜像与方向几何全部通过；PNG 0/128 alpha预乘通道差≤1/255。ICC色域尚需专门fixture。";
    }
    public static async Task<string> RecursiveAndLongPaths(ImageStore images,string root,string[] originals)
    {
        var scan=Path.Combine(root,"scan-fixtures");Directory.CreateDirectory(scan);
        void Copy(string name,int index){var path=Path.Combine(scan,name);Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.Copy(originals[index],path);}
        Copy("照片10.jpg",0);Copy("子目录/照片2.jpg",1);Copy("照片3.jpg",2);Copy(".hidden/ignored.jpg",0);Copy("隐藏属性/ignored.jpg",0);Copy("Ignored.app/inside.jpg",0);
        File.SetAttributes(Path.Combine(scan,"隐藏属性"),FileAttributes.Directory|FileAttributes.Hidden);
        await File.WriteAllTextAsync(Path.Combine(scan,"bad.jpg"),"corrupt image");
        var imported=await new PhotoImporter(images).ImportAsync([scan],[]);
        Require(imported.Photos.Select(p=>Path.GetFileName(p.Path)).SequenceEqual(new[]{"照片2.jpg","照片3.jpg","照片10.jpg"}),"递归排序或隐藏/包目录规则不匹配");Require(imported.Errors.Count==1,"单片损坏未独立报告");
        var longDir=Path.Combine(root,"长路径 Unicode");while(Path.Combine(longDir,"照片.png").Length<=300)longDir=Path.Combine(longDir,"更长的子目录0123456789");Directory.CreateDirectory(longDir);
        var longPath=Path.Combine(longDir,"照片.png");File.Copy(Path.Combine(root,"codec.png"),longPath);var decoded=await images.GetAsync(longPath,256);
        Require(decoded.Width==16&&decoded.Height==12,"超过260字符路径WIC读取失败");
        var locked=Path.Combine(root,"locked.jpg");File.Copy(originals[0],locked);using var lockFile=new FileStream(locked,FileMode.Open,FileAccess.ReadWrite,FileShare.None);
        var lockedImport=await new PhotoImporter(images).ImportAsync([locked],[]);Require(lockedImport.Photos.Count==0&&lockedImport.Errors.Count==1,"占用照片未友好隔离");
        return $"递归子目录/自然编号排序正确，点隐藏/Hidden属性/已知Mac包目录跳过；损坏和独占文件各1项错误；实际WIC读取{longPath.Length}字符中文路径。未遍历目录reparse。";
    }
}
