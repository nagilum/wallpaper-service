# WallpaperService

Simple Windows service to change wallpapers on multiple monitors on an interval.

## Settings

Configuration is done throught the `appsettings.json` file in the executable path.

```json
{
    "interval": 3600,
    "paths": ["C:\\Wallpapers"],
    "recursive": true,
    "pattern": "*.jpeg",
    "shuffle": true
}
```

### Interval

Interval, in seconds. Defaults to `1800` seconds (30 minutes).

### Paths

Paths is a list of folders to get files from. At least one folder path is required.

### Recursive

Whether to scan for files recursively in each folder path given. Defaults to `false`.

### Pattern

Pattern of files to match. Defaults to `*`.

### Shuffle

Whether to shuffle the files list. Defaults to `false`.

## Credit

Uses the [IDesktopWallpaperWrapper](https://github.com/9eck0/IDesktopWallpaper-dotNet/) behind the scenes.