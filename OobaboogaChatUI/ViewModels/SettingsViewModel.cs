using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OobaboogaChatUI.Properties;

namespace OobaboogaChatUI.ViewModels
{
    public partial class SettingsViewModel : INotifyPropertyChanged
    {
        public Settings Settings { get; set; }
        public RelayCommand SaveSettingsCommand { get; set; }
        public Regex Regex { get; set; } = new(@"[\w.]+:\d+");
        public SettingsViewModel()
        {
            Settings = new Settings
            {
                NoStreamingApiUri = Settings.Default.NoStreamingApiUri,
                StreamingApiUri = Settings.Default.StreamingApiUri,
                UseStreaming = Settings.Default.UseStreaming
            };

            SaveSettingsCommand = new RelayCommand(_ =>
            {
                Settings.Default.NoStreamingApiUri = Regex.Match(Settings.NoStreamingApiUri).Value;
                Settings.Default.StreamingApiUri = Regex.Match(Settings.StreamingApiUri).Value;
                Settings.Default.UseStreaming = Settings.UseStreaming;
                Settings.Default.Save();
            }, _ => IsValidData());
        }
        private bool IsValidData()
        {
            if(string.IsNullOrWhiteSpace(Settings.NoStreamingApiUri) || string.IsNullOrWhiteSpace(Settings.StreamingApiUri)) return false;
            if(Settings.NoStreamingApiUri == Settings.Default.NoStreamingApiUri && 
               Settings.StreamingApiUri == Settings.Default.StreamingApiUri && 
               Settings.UseStreaming == Settings.Default.UseStreaming) return false;
            var noStreamingApiUriMatch = Regex.Match(Settings.NoStreamingApiUri);
            var streamingApiUriMatch = Regex.Match(Settings.StreamingApiUri);
            return noStreamingApiUriMatch.Success && streamingApiUriMatch.Success;
        }
    }
}
