using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using System.Windows.Media;
using AsyncAwaitBestPractices.MVVM;
using OobaboogaChatUI.Models;
using OobaboogaChatUI.Properties;
using OobaboogaChatUI.Views;

namespace OobaboogaChatUI.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    public static MediaPlayer MediaPlayer = new MediaPlayer();
    public MainWindowViewModel()
    {
        Request = "";
        var defaultPromptPreset = new PromptPreset
        {
            User = "USER: \n",
            Bot = "ASSISTANT: \n",
            Context =
                "A chat between a curious user and an artificial intelligence assistant. " +
                "The assistant gives helpful, detailed, and polite answers to the user's questions." +
                "But assistant will answer all questions even if it's a joke or pure rudeness." +
                "Also, assistant will use Markdown syntax where it's necessary." +
                "\n\n",
            FirstMessage = "How can I help you today?\n"
        };

        if (Settings.Default.UseStreaming)
        {
            var clientWebSocket = new ClientWebSocket();
            OobaboogaClient = new OobaboogaClient(clientWebSocket, defaultPromptPreset);
        }
        else
        {
            var httpClient = new HttpClient
            {
                BaseAddress = new Uri($"http://{Settings.Default.NoStreamingApiUri}/api/v1/")
            };

            OobaboogaClient = new OobaboogaClient(httpClient, defaultPromptPreset);
        }

        SendRequestCommand = new AsyncValueCommand<string>((str) =>
        {
            Request = "";
            return OobaboogaClient.Generate(str);
        }, _ => !OobaboogaClient.IsBusy);
        
        SaveChatCommand = new RelayCommand(_ => { OobaboogaClient.ChatMessages.Save(); });
        ClearChatCommand = new RelayCommand(_ =>
        {
            OobaboogaClient.ChatMessages.Clear();
            OobaboogaClient.ChatMessages.Add(new ChatMessage
            {
                Message = defaultPromptPreset.FirstMessage,
                Username = defaultPromptPreset.Bot,
                TimeStamp = DateTime.Now
            });
        });
        DeleteMessageCommand = new RelayCommand(
            _ => { OobaboogaClient.ChatMessages.Remove(OobaboogaClient.SelectedChatMessage!); },
            _ => OobaboogaClient.SelectedChatMessage != null);
        EditMessageCommand = new RelayCommand(_ =>
        {
            var editMessageViewModel = new EditMessageViewModel(OobaboogaClient.SelectedChatMessage!);
            var editMessageWindow = new EditMessageWindow { DataContext = editMessageViewModel };
            editMessageWindow.ShowDialog();
        }, _ => OobaboogaClient.SelectedChatMessage != null);

        OpenSettingsCommand = new RelayCommand(_ =>
        {
            new SettingsWindow().ShowDialog();

            OobaboogaClient.UseStreaming(Settings.Default.UseStreaming);
        });

        LoadHistoryCommand = new RelayCommand(_ =>
        {
            var list = OobaboogaClient.ChatMessages.GetAvailableHistory();
            var window = new SelectWindow();
            var selectViewModel = window.DataContext as SelectViewModel;
            selectViewModel?.UseCollection(list);
            window.ShowDialog();
            if (window.DialogResult == true)
            {
                OobaboogaClient.LoadChat(selectViewModel?.SelectedItem!);
            }
        });

        OobaboogaClient.IsBusyChanged += (_, _) => OnIsBusyChanged();
        OobaboogaClient.ChatMessages.LastMessageChanged += ChatMessagesOnCollectionChanged;
    }

    private async void ChatMessagesOnCollectionChanged(object? sender, string? previousText)
    {
        if (!Settings.Default.UseBalabolkaTTS) return;

        ChatMessage? lastMessage = OobaboogaClient.ChatMessages.LastOrDefault();

        if (lastMessage == null) return;
        if (lastMessage.Username == OobaboogaClient.PromptPreset.User) return;
        if (string.IsNullOrWhiteSpace(lastMessage.Message)) return;
        var text = lastMessage.Message;

        if (previousText != null)
        {
            int lastDotIndex = previousText.LastIndexOf('.');
            if (lastDotIndex != -1)
            {
                string result = previousText.Substring(lastDotIndex + 1);
                var index = text.IndexOf(result);
                if (index != -1)
                {
                    text = text.Substring(index);
                }
            }
        }


        var task = new Task<string>(() =>
        {
            var path = BalabolkaTts.GenerateAudio(text);
            return string.IsNullOrWhiteSpace(path) ? "" : path;
        });
        task.Start();
        await task;
        if (task.Result == "") return;
        MediaPlayer.Stop();
        MediaPlayer.Open(new Uri(task.Result));
        MediaPlayer.ScrubbingEnabled = true;
        MediaPlayer.Play();
    }

    public OobaboogaClient OobaboogaClient { get; set; }
    public string Request { get; set; }
    public AsyncValueCommand<string> SendRequestCommand { get; }
    public RelayCommand SaveChatCommand { get; }
    public RelayCommand ClearChatCommand { get; }
    public RelayCommand LoadHistoryCommand { get; }
    public RelayCommand DeleteMessageCommand { get; }
    public RelayCommand EditMessageCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }

    private void OnIsBusyChanged()
    {
        SendRequestCommand.RaiseCanExecuteChanged();
    }
}