using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LightTable.Windows;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if(args.Length==4&&args[0]=="--compile-shader")
        {
            try{ShaderCompiler.Build(args[1],args[2],args[3]);}
            catch(Exception ex){File.WriteAllText(args[3]+".error.txt",ex.ToString());Environment.ExitCode=1;}
            return;
        }
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(parameters =>
        {
            SynchronizationContext.SetSynchronizationContext(new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread()));
            _ = new LightTableApplication();
        });
    }
}

sealed partial class LightTableApplication : Application
{
    private Window? window;
    public LightTableApplication()
    {
        InitializeComponent();
        UnhandledException += (_, e) =>
        {
            var root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"LightTable");var arguments=Environment.GetCommandLineArgs();var index=Array.IndexOf(arguments,"--data-root");
            try{if(index>=0&&index+1<arguments.Length)root=Path.GetFullPath(arguments[index+1]);}catch{}
            try{Directory.CreateDirectory(Path.Combine(root,"logs"));File.AppendAllText(Path.Combine(root,"logs","crash.log"),$"{DateTimeOffset.Now:O} {e.Exception}\nManaged event stack: {Environment.StackTrace}\n");}catch{}
        };
    }
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        window = new MainWindow();
        var arguments=Environment.GetCommandLineArgs();
        if(arguments.Contains("--integration-fixtures")||arguments.Contains("--benchmark-photos")||arguments.Contains("--optical-cache-probe")){window.AppWindow.Show(false);window.AppWindow.MoveInZOrderAtBottom();}
        else window.Activate();
    }
}
