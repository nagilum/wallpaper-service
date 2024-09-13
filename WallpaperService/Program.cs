namespace WallpaperService;

public static class Program
{
    /// <summary>
    /// Init all the things...
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    private static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddWindowsService();
        builder.Services.AddHostedService<Worker>();

        var host = builder.Build();
        
        host.Run();
    }
}