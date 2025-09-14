using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO.Compression;

namespace WebUI.Pages.Security;

[Authorize]
public class RestoreModel : PageModel
{
    [BindProperty]
    public IFormFile? File { get; set; }

    public string? ResultMessage { get; set; }

    private static readonly HashSet<string> Whitelist = new(StringComparer.OrdinalIgnoreCase)
    {
        "accounts.db",
        "accounts.db-shm",
        "accounts.db-wal",
        Path.Combine("Data", "reminder_records.db"),
        "appsettings.json",
        "appsettings.Development.json"
    };

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (File == null || File.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "请选择ZIP文件");
            return Page();
        }

        try
        {
            var baseDir = AppContext.BaseDirectory;
            using var stream = File.OpenReadStream();
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);

            int restored = 0, skipped = 0;
            foreach (var entry in archive.Entries)
            {
                var entryPath = entry.FullName.Replace('\\', '/');
                if (entryPath.EndsWith('/')) { continue; }

                // normalize to OS path
                var normalized = entryPath.Replace('/', Path.DirectorySeparatorChar);
                if (!Whitelist.Contains(normalized) && !Whitelist.Contains(Path.GetFileName(normalized)))
                {
                    skipped++;
                    continue;
                }

                var destinationPath = Path.Combine(baseDir, normalized);
                var destinationDir = Path.GetDirectoryName(destinationPath);
                if (!string.IsNullOrEmpty(destinationDir)) Directory.CreateDirectory(destinationDir);

                using var entryStream = entry.Open();
                using var fs = System.IO.File.Create(destinationPath);
                await entryStream.CopyToAsync(fs);
                restored++;
            }

            ResultMessage = $"还原完成：成功 {restored} 项，跳过 {skipped} 项。建议重启应用。";
            return Page();
        }
        catch (Exception ex)
        {
            ResultMessage = $"还原失败：{ex.Message}";
            return Page();
        }
    }
}


