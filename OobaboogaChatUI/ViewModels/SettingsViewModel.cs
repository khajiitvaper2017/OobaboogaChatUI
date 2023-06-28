using System.ComponentModel;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using OobaboogaChatUI.Properties;

namespace OobaboogaChatUI.ViewModels;

public partial class SettingsViewModel : INotifyPropertyChanged
{
    public SettingsViewModel()
    {
        Settings = new Settings
        {
            NoStreamingApiUri = Settings.Default.NoStreamingApiUri,
            StreamingApiUri = Settings.Default.StreamingApiUri,
            UseStreaming = Settings.Default.UseStreaming,
            UseBalabolkaTTS = Settings.Default.UseBalabolkaTTS,
            BalabolkaExecutable = Settings.Default.BalabolkaExecutable,
        };

        SaveSettingsCommand = new RelayCommand(_ =>
        {
            Settings.Default.NoStreamingApiUri = Regex.Match(Settings.NoStreamingApiUri).Value;
            Settings.Default.StreamingApiUri = Regex.Match(Settings.StreamingApiUri).Value;

            Settings.Default.UseStreaming = Settings.UseStreaming;
            Settings.Default.UseBalabolkaTTS = Settings.UseBalabolkaTTS;

            Settings.Default.BalabolkaExecutable = Settings.BalabolkaExecutable;

            Settings.Default.Save();
        }, _ => IsValidData());

        SelectExecutableCommand = new RelayCommand(_ =>
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                DefaultExt = ".exe",
                Filter = "Executables (.exe)|*.exe"
            };
            openFileDialog.ShowDialog();
            Settings.BalabolkaExecutable = openFileDialog.FileName;
        });
    }

    public Settings Settings { get; set; }
    public RelayCommand SaveSettingsCommand { get; set; }
    public RelayCommand SelectExecutableCommand { get; set; }
    public Regex Regex { get; set; } = new(@"[\w.]+:\d+");

    private bool IsValidData()
    {
        if (string.IsNullOrWhiteSpace(Settings.NoStreamingApiUri) ||
            string.IsNullOrWhiteSpace(Settings.StreamingApiUri) ||
            (Settings.UseBalabolkaTTS && string.IsNullOrWhiteSpace(Settings.BalabolkaExecutable))
            ) return false;
        if (Settings.NoStreamingApiUri == Settings.Default.NoStreamingApiUri &&
            Settings.StreamingApiUri == Settings.Default.StreamingApiUri &&
            Settings.UseStreaming == Settings.Default.UseStreaming &&
            Settings.UseBalabolkaTTS == Settings.Default.UseBalabolkaTTS && 
            Settings.BalabolkaExecutable == Settings.Default.BalabolkaExecutable
            ) return false;
        var noStreamingApiUriMatch = Regex.Match(Settings.NoStreamingApiUri);
        var streamingApiUriMatch = Regex.Match(Settings.StreamingApiUri);
        return noStreamingApiUriMatch.Success && streamingApiUriMatch.Success;
    }
}