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

                var index = Random.Shared.Next(0, files.Count);

                logger.LogInformation(
                    "Setting {path} as wallpaper for monitor #{index} - {monitorId}",
                    files[index],
                    monitorIndex,
                    id);

                _engine.SetWallpaper(id, files[index]);

                files.RemoveAt(index);
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
            logger.LogInformation(
                "Getting {searchPattern} from {path} {recursively}",
                _searchPattern,
                path,
                _searchOption is SearchOption.AllDirectories ? "recursively" : "not recursively");
            
            try
            {
                files.AddRange(
                    Directory.GetFiles(
                        path,
                        _searchPattern,
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
        var n = list.Count;

        while (n > 1)
        {
            n--;

            var k = Random.Shared.Next(n + 1);
            var value = list[k];

            list[k] = list[n];
            list[n] = value;
        }
    }
}