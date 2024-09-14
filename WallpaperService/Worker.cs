using IDesktopWallpaperWrapper;

namespace WallpaperService;

public class Worker(
    IConfiguration configuration,
    ILogger<Worker> logger) : BackgroundService
{
    /// <summary>
    /// Wallpaper engine.
    /// </summary>
    private readonly DesktopWallpaper _engine = new();
    
    /// <summary>
    /// Interval between each wallpaper change round, in seconds.
    /// </summary>
    private readonly int _interval = configuration
        .GetValue<int?>("Interval") ?? 30 * 60;

    /// <summary>
    /// All paths to search for files in.
    /// </summary>
    private readonly string[]? _paths =
        configuration
            .GetSection("Paths")
            .Get<string[]>()
        ?? throw new Exception("Missing app setting for 'paths'.");

    /// <summary>
    /// Whether to search recursively.
    /// </summary>
    private readonly SearchOption _searchOption = configuration
        .GetValue<bool?>("Recursive") is true
        ? SearchOption.AllDirectories
        : SearchOption.TopDirectoryOnly;
    
    /// <summary>
    /// Search pattern for matching filenames.
    /// </summary>
    private readonly string _searchPattern = configuration
        .GetValue<string?>("Pattern") ?? "*";

    /// <summary>
    /// Whether to shuffle the list of files.
    /// </summary>
    private readonly bool? _shuffle = configuration
        .GetValue<bool?>("Shuffle");

    /// <summary>
    /// Change wallpapers of each monitor every n seconds.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(_interval);
        var monitorIds = _engine.GetAllMonitorIDs();
        var files = this.GetFiles();

        while (!cancellationToken.IsCancellationRequested)
        {
            if (files.Count is 0)
            {
                files = this.GetFiles();
            }

            var monitorIndex = -1;

            foreach (var id in monitorIds)
            {
                monitorIndex++;
                
                if (files.Count is 0)
                {
                    break;
                }

                var file = files[0];
                
                logger.LogInformation(
                    "Setting {file} as wallpaper for monitor #{index} - {monitorId}",
                    file,
                    monitorIndex,
                    id);

                try
                {
                    _engine.SetWallpaper(id, file);
                    files.RemoveAt(0);
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error while setting {file} as wallpaper.",
                        file);
                }
            }
            
            logger.LogInformation(
                "Waiting {delay} to set new wallpapers.",
                delay);
            
            await Task.Delay(delay, cancellationToken);
        }
    }

    /// <summary>
    /// Get files from all paths.
    /// </summary>
    /// <returns>List of files.</returns>
    private List<string> GetFiles()
    {
        var files = new List<string>();

        foreach (var path in _paths!)
        {
            var patterns = _searchPattern.Split(';');

            foreach (var pattern in patterns)
            {
                logger.LogInformation(
                    "Getting {searchPattern} from {path} {recursively}",
                    pattern,
                    path,
                    _searchOption is SearchOption.AllDirectories ? "recursively" : "not recursively");
            
                try
                {
                    files.AddRange(
                        Directory.GetFiles(
                            path,
                            pattern,
                            _searchOption));
                }
                catch (Exception ex)
                {
                    logger.LogError(
                        ex,
                        "Error while getting files from {path}",
                        path);
                }
            }
        }
        
        logger.LogInformation(
            "Found {count} file(s).",
            files.Count);

        if (_shuffle is not true)
        {
            return files;
        }

        logger.LogInformation("Randomizing files list.");
        this.Shuffle(ref files);

        return files;
    }

    /// <summary>
    /// Shuffle list.
    /// </summary>
    /// <param name="list">List to shuffle.</param>
    private void Shuffle(ref List<string> list)
    {
        var rng = new Random((int)DateTime.Now.Ticks);
        var n = list.Count;

        while (n > 1)
        {
            n--;

            var k = rng.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}