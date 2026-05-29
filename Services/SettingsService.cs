using System;
using System.IO;
using System.Text.Json;
using HabitatRural.Models;

namespace HabitatRural.Services;

public class SettingsService
{
    private static readonly Lazy<SettingsService> _lazy = new(() => new SettingsService());
    public static SettingsService Instance => _lazy.Value;
    private static readonly JsonSerializerOptions _opts = new() { WriteIndented = true };
    public string SettingsFolder { get; }
    public string SettingsFile   { get; }
    private AppSettings _current = new();
    public AppSettings Current => _current;

    private SettingsService()
    {
        SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HabitatRural");
        Directory.CreateDirectory(SettingsFolder);
        SettingsFile = Path.Combine(SettingsFolder, "settings.json");
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var s = JsonSerializer.Deserialize<AppSettings>(json);
                if (s != null) _current = s;
            }
        }
        catch { _current = new AppSettings(); }
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_current, _opts);
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }

    public void Replace(AppSettings s)
    {
        _current = s;
        Save();
    }
}
