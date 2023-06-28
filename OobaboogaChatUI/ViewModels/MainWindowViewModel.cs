using System;
using System.ComponentModel;
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
    public static MediaPlayer MediaPlayer = new();

    public MainWindowViewModel()
    {
        Request = "";
        var defaultPromptPreset = new PromptPreset(
            "USER: \n",
            new Character
            (
                "ASSISTANT: \n",
                "A chat between a curious user and an artificial intelligence assistant. " +
                "The assistant gives helpful, detailed, and polite answers to the user's questions." +
                "But assistant will answer all questions even if it's a joke or pure rudeness." +
                "Also, assistant will use Markdown syntax where it's necessary." +
                "\n\n",
                "How can I help you today?\n"
            )
        );

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

        SendRequestCommand = new AsyncValueCommand<string>(str =>
        {
            Request = "";
            return OobaboogaClient.Generate(str);
        }, _ => !OobaboogaClient.IsBusy);

        ImpersonateRequestCommand = new AsyncValueCommand<string>(_ => OobaboogaClient.Impersonate(this), _ => !OobaboogaClient.IsBusy);

        SaveChatCommand = new RelayCommand(_ => { OobaboogaClient.ChatMessages.Save(); });
        ClearChatCommand = new RelayCommand(_ =>
        {
            OobaboogaClient.ChatMessages.Clear();
            OobaboogaClient.ChatMessages.Add(new ChatMessage
            {
                Message = defaultPromptPreset.Greeting,
                Username = defaultPromptPreset.Name,
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
            if (window.DialogResult == true) OobaboogaClient.LoadChat(selectViewModel?.SelectedItem!);
        });

        SelectCharacterCommand = new RelayCommand(_ =>
        {
            var list = OobaboogaClient.CharacterList.Select(c => c.Name).ToList();
            var window = new SelectWindow();
            var selectViewModel = window.DataContext as SelectViewModel;
            selectViewModel?.UseCollection(list);
            window.ShowDialog();
            if (window.DialogResult == true)
                OobaboogaClient.SelectCharacter(
                    OobaboogaClient.CharacterList.First(c => c.Name == selectViewModel?.SelectedItem!));
        });

        OobaboogaClient.IsBusyChanged += (_, _) => OnIsBusyChanged();
        Chat.LastMessageChanged += ChatMessagesOnCollectionChanged;
    }

    public OobaboogaClient OobaboogaClient { get; set; }
    public string Request { get; set; }
    public AsyncValueCommand<string> SendRequestCommand { get; }
    public AsyncValueCommand<string> ImpersonateRequestCommand { get; }
    public RelayCommand SaveChatCommand { get; }
    public RelayCommand ClearChatCommand { get; }
    public RelayCommand LoadHistoryCommand { get; }
    public RelayCommand SelectCharacterCommand { get; }
    public RelayCommand DeleteMessageCommand { get; }
    public RelayCommand EditMessageCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }

    private async void ChatMessagesOnCollectionChanged(string previousText)
    {
        if (!Settings.Default.UseBalabolkaTTS) return;

        var lastMessage = OobaboogaClient.ChatMessages.LastOrDefault();

        if (lastMessage == null) return;
        if (lastMessage.Username == OobaboogaClient.PromptPreset.Username) return;
        if (string.IsNullOrWhiteSpace(lastMessage.Message)) return;
        var text = lastMessage.Message;

        if (!string.IsNullOrWhiteSpace(previousText))
        {
            var lastDotIndex = previousText.LastIndexOf('.');
            if (lastDotIndex != -1)
            {
                var result = previousText.Substring(lastDotIndex + 1);
                var index = text.IndexOf(result);
                if (index != -1) text = text.Substring(index);
            }
        }

        text = text.Replace('*', ' ');

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

    private void OnIsBusyChanged()
    {
        SendRequestCommand.RaiseCanExecuteChanged();
        ImpersonateRequestCommand.RaiseCanExecuteChanged();
    }
}