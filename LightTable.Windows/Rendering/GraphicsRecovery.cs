using Microsoft.Graphics.Canvas;

namespace LightTable.Windows.Rendering;

static class GraphicsRecovery
{
    // IsDeviceLost verifies the actual device reason. RaiseDeviceLost is not a
    // fault-injection API and must not be called on a healthy D3D device.
    public static bool Handle(CanvasDevice? device,Exception error,Action invalidate,Action<string>? log=null)
    {
        if(device==null||!device.IsDeviceLost(error.HResult))return false;
        invalidate();log?.Invoke($"GPU设备失效，正在重建资源：0x{error.HResult:X8}");device.RaiseDeviceLost();return true;
    }
}
