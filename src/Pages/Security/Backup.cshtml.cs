using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.Sqlite;
using System.IO.Compression;

namespace WebUI.Pages.Security;

[Authorize]
public class BackupModel : PageModel
{
    public string? Message { get; set; }

    public IActionResult OnGet()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var webUiRoot = Path.GetFullPath(Path.Combine(baseDir));

            var files = new List<(string Path, string EntryName)>();
            var memFiles = new List<(byte[] Data, string EntryName)>();

            void AddIfExists(string path, string entryName)
            {
                if (System.IO.File.Exists(path))
                {
                    files.Add((path, entryName));
                }
            }

            // Safely copy SQLite databases to temp using backup API to avoid file locks
            void AddDb(string absPath, string entryName)
            {
                if (!System.IO.File.Exists(absPath)) return;
                try
                {
                    var tempPath = Path.Combine(Path.GetTempPath(), $"backup_{Guid.NewGuid():N}.db");
                    using (var source = new SqliteConnection($"Data Source={absPath}"))
                    using (var dest = new SqliteConnection($"Data Source={tempPath}"))
                    {
                        source.Open();
                        dest.Open();
                        source.BackupDatabase(dest);
                    }
                    // Read the temp file fully into memory then delete the temp file immediately to avoid file lock issues
                    byte[] data;
                    using (var fs = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (var msDb = new MemoryStream())
                    {
                        fs.CopyTo(msDb);
                        data = msDb.ToArray();
                    }
                    try { System.IO.File.Delete(tempPath); } catch { }
                    memFiles.Add((data, entryName));
                }
                catch
                {
                    // Fallback: skip if cannot backup; user will be informed if nothing to backup
                }
            }

            AddDb(Path.Combine(webUiRoot, "accounts.db"), "accounts.db");
            AddDb(Path.Combine(webUiRoot, "identity.db"), "identity.db");
            AddDb(Path.Combine(webUiRoot, "Data", "reminder_records.db"), Path.Combine("Data", "reminder_records.db"));

            // appsettings files (masking sensitive info would be a separate enhancement)
            AddIfExists(Path.Combine(webUiRoot, "appsettings.json"), "appsettings.json");
            AddIfExists(Path.Combine(webUiRoot, "appsettings.Development.json"), "appsettings.Development.json");

            if (files.Count == 0 && memFiles.Count == 0)
            {
                Message = "未找到可备份的数据文件。";
                return Page();
            }

            using var ms = new MemoryStream();
            using (var archive = new ZipArchive(ms, ZipArchiveMode.Create, true))
            {
                // Add in-memory database backups
                foreach (var m in memFiles)
                {
                    var entry = archive.CreateEntry(m.EntryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    entryStream.Write(m.Data, 0, m.Data.Length);
                }

                // Add disk files (configs, etc.)
                foreach (var f in files)
                {
                    var entry = archive.CreateEntry(f.EntryName, CompressionLevel.Optimal);
                    using var entryStream = entry.Open();
                    using var fs = new FileStream(f.Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    fs.CopyTo(entryStream);
                }
            }

            var zipBytes = ms.ToArray();
            var fileName = $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip";
            return File(zipBytes, "application/zip", fileName);
        }
        catch (Exception ex)
        {
            Message = $"备份失败: {ex.Message}";
            return Page();
        }
    }
}


