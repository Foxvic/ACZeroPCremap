using ACZeroPCremap.Core.Localization;
using System.Security.Cryptography;

namespace ACZeroPCremap.Core;

public sealed class ExternalChangeException() : IOException(Text.Get("Error.ExternalChange"));

public sealed class ConfigSession
{
    private byte[] baseline;
    private readonly TimeProvider clock;
    public string FilePath { get; }
    public IniDocument Document { get; private set; }
    public bool IsDirty => !baseline.AsSpan().SequenceEqual(Document.ToBytes());

    private ConfigSession(string path, byte[] bytes, TimeProvider clock)
    {
        FilePath = Path.GetFullPath(path);
        baseline = bytes;
        this.clock = clock;
        Document = IniDocument.Parse(bytes);
    }

    public static ConfigSession Open(string path, TimeProvider? clock = null) => new(path, File.ReadAllBytes(path), clock ?? TimeProvider.System);
    public void DiscardChanges() => Document = IniDocument.Parse(baseline);

    public string? Save() => IsDirty ? Commit(Document.ToBytes()) : null;

    public string Restore(string backupPath)
    {
        if (IsDirty) throw new InvalidOperationException(Text.Get("Error.DirtyRestore"));
        if (Path.GetFullPath(backupPath).Equals(FilePath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(Text.Get("Error.SelectBackup"));
        var bytes = File.ReadAllBytes(backupPath);
        var candidate = IniDocument.Parse(bytes);
        if (candidate.SectionCount("KeyboardUI") == 0 && candidate.SectionCount("KeyboardFlight") == 0 && !candidate.HasDeviceSections)
            throw new InvalidDataException(Text.Get("Error.InvalidBackup"));
        return Commit(bytes);
    }

    private string Commit(byte[] bytes)
    {
        _ = IniDocument.Parse(bytes);
        // Deny other writers while checking and replacing, but allow atomic rename/delete.
        using var source = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
        using var memory = new MemoryStream();
        source.CopyTo(memory);
        var current = memory.ToArray();
        if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(current), SHA256.HashData(baseline)))
            throw new ExternalChangeException();

        var backup = CreateBackup(current);
        var temporary = Path.Combine(Path.GetDirectoryName(FilePath)!, $".{Path.GetFileName(FilePath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using (var output = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                output.Write(bytes);
                output.Flush(flushToDisk: true);
            }
            var verified = File.ReadAllBytes(temporary);
            if (!bytes.AsSpan().SequenceEqual(verified)) throw new IOException(Text.Get("Error.VerifyWrite"));
            _ = IniDocument.Parse(verified);
            // Check the current path as well: an external editor may have replaced it by rename.
            if (!File.ReadAllBytes(FilePath).AsSpan().SequenceEqual(baseline)) throw new ExternalChangeException();
            File.Replace(temporary, FilePath, null);
            baseline = bytes;
            Document = IniDocument.Parse(bytes);
            return backup;
        }
        finally
        {
            try { if (File.Exists(temporary)) File.Delete(temporary); }
            catch (IOException) { /* Never mask the actual save error with cleanup errors. */ }
            catch (UnauthorizedAccessException) { }
        }
    }

    private string CreateBackup(byte[] bytes)
    {
        var stem = FilePath + "." + clock.GetLocalNow().ToString("yyyyMMdd-HHmmss-fff");
        for (var i = 0; ; i++)
        {
            var path = stem + (i == 0 ? "" : $"-{i}") + ".bak";
            FileStream output;
            try { output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None); }
            catch (IOException) when (File.Exists(path)) { continue; }
            using (output) { output.Write(bytes); output.Flush(flushToDisk: true); }
            return path;
        }
    }
}
