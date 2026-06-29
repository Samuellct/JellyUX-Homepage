using System.Text.Json;
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
    private readonly ILogger<UserConfigurationStore> _logger;
    private readonly ReaderWriterLockSlim _lock = new();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserConfigurationStore"/> class.
    /// </summary>
    /// <param name="applicationPaths">Provides the plugin configurations directory path.</param>
    /// <param name="logger">Logger.</param>
    public UserConfigurationStore(IApplicationPaths applicationPaths, ILogger<UserConfigurationStore> logger)
    {
        _logger = logger;
        _usersDir = Path.Combine(
            applicationPaths.PluginConfigurationsPath,
            "Jellyfin.Plugin.JuxHomepage",
            "users");

        Directory.CreateDirectory(_usersDir);
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
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
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

        _lock.EnterWriteLock();
        try
        {
            var json = JsonSerializer.Serialize(config, SerializerOptions);
            File.WriteAllText(path, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save user configuration for {UserId}.", config.UserId);
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
}
