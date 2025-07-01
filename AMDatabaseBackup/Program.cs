using System.IO.Compression;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace AMDatabaseBackup;

public class BackupConfig
{
    public string BackupDirectory { get; set; } = string.Empty;
    public Dictionary<string, string> Databases { get; set; } = new();
}

internal static class Program
{
    private static void Main(string[] args)
    {
        var config = LoadConfig("appsettings.json");

        foreach (var (databaseName, connectionString) in config.Databases)
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupFilePath = Path.Combine(config.BackupDirectory, $"{databaseName}_{timestamp}.bak");
            var backupSql = $"""

                                                 BACKUP DATABASE [{databaseName}]
                                                 TO DISK = N'{backupFilePath}'
                                                 WITH FORMAT, INIT, NAME = N'{databaseName}-Full Backup';
                                             
                             """;

            using var connection = new SqlConnection(connectionString);
            using var command = new SqlCommand(backupSql, connection);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();

                if (File.Exists(backupFilePath))
                {
                    var zipPath = Path.ChangeExtension(backupFilePath, ".zip");

                    using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                    {
                        zip.CreateEntryFromFile(backupFilePath, Path.GetFileName(backupFilePath));
                    }

                    File.Delete(backupFilePath);
                    Console.WriteLine($"ackup completed and zipped: {zipPath}");
                }
                else
                {
                    Console.WriteLine($"Backup completed but file not found: {backupFilePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Backup failed for {databaseName}: {ex.Message}");
            }
        }
    }

    private static BackupConfig LoadConfig(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Configuration file not found.", path);

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<BackupConfig>(json)
               ?? throw new Exception("Invalid config file format.");
    }
}