using System.Text.Json;
using Jellyfin.Plugin.JuxHomepage.IO;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.JuxHomepage.Configuration;

/// <summary>
/// Reads and writes per-user <see cref="UserConfiguration"/> as JSON files on disk.
/// Files are stored under <c>PluginConfigurationsPath/Jellyfin.Plugin.JuxHomepage/users/{userId}.json</c>.
/// All file access is protected by a <see cref="ReaderWriterLockSlim"/> to allow concurrent reads
/// while serializing writes.
/// </summary>
public sealed class UserConfigurationStore : IUserConfigurationStore, IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _usersDir;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<UserConfigurationStore> _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConfigurationStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">Provides the plugin configurations directory path.</param>
    /// <param name="fileSystem">File system abstraction, for testability.</param>
    /// <param name="logger">Logger.</param>
    public UserConfigurationStore(
        IApplicationPaths applicationPaths,
        IFileSystem fileSystem,
        ILogger<UserConfigurationStore> logger)
    {
        _fileSystem = fileSystem;
        _logger = logger;
        _usersDir = Path.Combine(
            applicationPaths.PluginConfigurationsPath,
            "Jellyfin.Plugin.JuxHomepage",
            "users");

        _fileSystem.CreateDirectory(_usersDir);
    }

    /// <summary>
    /// Reads the user configuration for the given user.
    /// </summary>
    /// <param name="userId">The Jellyfin user identifier.</param>
    /// <returns>The user configuration, or null if none has been saved yet.</returns>
    public UserConfiguration? GetUserConfiguration(Guid userId)
    {
        var path = GetPath(userId);

        _lock.EnterReadLock();
        try
        {
            if (!_fileSystem.FileExists(path))
            {
                return null;
            }

            var json = _fileSystem.ReadAllText(path);
            return JsonSerializer.Deserialize<UserConfiguration>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read user configuration for {UserId}.", userId);
            return null;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    /// <summary>
    /// Persists the given user configuration to disk.
    /// </summary>
    /// <param name="config">The configuration to save.</param>
    public void SaveUserConfiguration(UserConfiguration config)
    {
        var path = GetPath(config.UserId);
        var tmpPath = path + ".tmp";

        _lock.EnterWriteLock();
        try
        {
            var json = JsonSerializer.Serialize(config, SerializerOptions);

            // Write to a temp file first, then rename into place. File.Move with overwrite is
            // atomic on the same volume (rename() on Linux, MoveFileEx/MOVEFILE_REPLACE_EXISTING on
            // Windows), so a crash or restart mid-write can never leave a partially-written config
            // file on disk -- readers always see either the old file or the fully-written new one.
            _fileSystem.WriteAllText(tmpPath, json);
            _fileSystem.Move(tmpPath, path, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user configuration for {UserId}.", config.UserId);
            TryDeleteStrayTempFile(tmpPath);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (!_disposed)
        {
            _lock.Dispose();
            _disposed = true;
        }
    }

    private string GetPath(Guid userId) =>
        Path.Combine(_usersDir, $"{userId}.json");

    private void TryDeleteStrayTempFile(string tmpPath)
    {
        try
        {
            if (_fileSystem.FileExists(tmpPath))
            {
                _fileSystem.Delete(tmpPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to clean up stray temp file {TmpPath}.", tmpPath);
        }
    }
}
