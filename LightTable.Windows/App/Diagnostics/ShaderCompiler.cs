using System.Runtime.InteropServices;
using System.Text;

namespace LightTable.Windows;

// Build-time utility; packaged builds consume precompiled shader bytecode.
static class ShaderCompiler
{
    [DllImport("d3dcompiler_47.dll",CallingConvention=CallingConvention.StdCall,CharSet=CharSet.Ansi)]
    static extern int D3DCompile(byte[] source,nuint length,string? name,IntPtr defines,IntPtr include,string? entry,string target,uint flags,uint effectFlags,out IntPtr code,out IntPtr errors);
    [DllImport("d3dcompiler_47.dll",CallingConvention=CallingConvention.StdCall)]
    static extern int D3DSetBlobPart(byte[] source,nuint length,int part,uint flags,byte[] data,nuint dataLength,out IntPtr shader);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate IntPtr BlobPointer(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]delegate nuint BlobSize(IntPtr self);
    static byte[] Bytes(IntPtr blob)
    {
        if(blob==IntPtr.Zero)return [];
        var table=Marshal.ReadIntPtr(blob);
        var pointer=Marshal.GetDelegateForFunctionPointer<BlobPointer>(Marshal.ReadIntPtr(table,3*IntPtr.Size))(blob);
        var length=Marshal.GetDelegateForFunctionPointer<BlobSize>(Marshal.ReadIntPtr(table,4*IntPtr.Size))(blob);
        var bytes=new byte[checked((int)length)];Marshal.Copy(pointer,bytes,0,bytes.Length);return bytes;
    }
    static byte[] Compile(string source,string profile,string? entry)
    {
        var text=Encoding.UTF8.GetBytes(source);var hr=D3DCompile(text,(nuint)text.Length,"LightTable shader",IntPtr.Zero,IntPtr.Zero,entry,profile,1u<<15,0,out var code,out var errors);
        try{if(hr<0)throw new InvalidOperationException($"HLSL {profile}: {Encoding.UTF8.GetString(Bytes(errors))}");return Bytes(code);}
        finally{if(code!=IntPtr.Zero)Marshal.Release(code);if(errors!=IntPtr.Zero)Marshal.Release(errors);}
    }
    public static void Build(string hlslPath,string helperPath,string output)
    {
        var helper=File.ReadAllText(helperPath);var source=File.ReadAllText(hlslPath).Replace("#include \"d2d1effecthelpers.hlsli\"",helper,StringComparison.Ordinal);
        var library=Compile("#define D2D_FUNCTION\n#define D2D_ENTRY main\n"+source,"lib_4_0",null);
        var shader=Compile("#define D2D_FULL_SHADER\n#define D2D_ENTRY main\n"+source,"ps_4_0","main");
        Marshal.ThrowExceptionForHR(D3DSetBlobPart(shader,(nuint)shader.Length,10,0,library,(nuint)library.Length,out var combined));
        try{File.WriteAllBytes(output,Bytes(combined));}finally{Marshal.Release(combined);}
    }
}
