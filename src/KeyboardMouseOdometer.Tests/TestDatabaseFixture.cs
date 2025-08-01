using KeyboardMouseOdometer.Core.Services;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Moq;

namespace KeyboardMouseOdometer.Tests;

/// <summary>
/// Base class for database tests that properly handles SQLite file cleanup
/// </summary>
public abstract class TestDatabaseFixture : IDisposable
{
    protected readonly Mock<ILogger<DatabaseService>> MockLogger;
    protected readonly DatabaseService DatabaseService;
    protected readonly string TestDatabasePath;

    protected TestDatabaseFixture(bool useFileDatabase = false)
    {
        MockLogger = new Mock<ILogger<DatabaseService>>();
        
        if (useFileDatabase)
        {
            TestDatabasePath = Path.GetTempFileName();
            DatabaseService = new DatabaseService(MockLogger.Object, TestDatabasePath);
        }
        else
        {
            // Use in-memory database to avoid file locking issues
            TestDatabasePath = ":memory:";
            DatabaseService = new DatabaseService(MockLogger.Object, TestDatabasePath);
        }
    }

    protected async Task InitializeDatabaseAsync()
    {
        await DatabaseService.InitializeAsync();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Close the database service first
                DatabaseService?.Dispose();
                
                // Clear all connection pools to ensure no lingering connections
                SqliteConnection.ClearAllPools();
                
                // Force garbage collection to run finalizers
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                
                // Small delay to let Windows release file handles
                Thread.Sleep(100);
                
                // Clean up all SQLite-related files
                CleanupDatabaseFiles();
            }
            catch (Exception ex)
            {
                // Don't fail tests due to cleanup issues
                System.Diagnostics.Debug.WriteLine($"Database cleanup warning: {ex.Message}");
            }
        }
    }

    private void CleanupDatabaseFiles()
    {
        // Skip cleanup for in-memory databases
        if (TestDatabasePath == ":memory:")
            return;
            
        var filesToCleanup = new[]
        {
            TestDatabasePath,           // Main database file
            TestDatabasePath + "-wal",  // Write-Ahead Log file
            TestDatabasePath + "-shm",  // Shared memory file
            TestDatabasePath + "-journal" // Rollback journal (older SQLite versions)
        };

        foreach (var file in filesToCleanup)
        {
            TryDeleteFile(file);
        }
    }

    private static void TryDeleteFile(string filePath)
    {
        if (!File.Exists(filePath))
            return;

        const int maxRetries = 3;
        const int delayMs = 50;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                File.Delete(filePath);
                return; // Success
            }
            catch (IOException) when (attempt < maxRetries - 1)
            {
                // Wait before retrying
                Thread.Sleep(delayMs);
            }
            catch (UnauthorizedAccessException) when (attempt < maxRetries - 1)
            {
                // Wait before retrying
                Thread.Sleep(delayMs);
            }
            catch
            {
                // On final attempt or other exceptions, try to rename the file
                // so it doesn't interfere with subsequent tests
                try
                {
                    var tempName = filePath + ".delete_" + DateTime.Now.Ticks;
                    File.Move(filePath, tempName);
                }
                catch
                {
                    // Give up gracefully - the file will be cleaned up eventually
                    System.Diagnostics.Debug.WriteLine($"Could not delete test file: {filePath}");
                }
                break;
            }
        }
    }
}