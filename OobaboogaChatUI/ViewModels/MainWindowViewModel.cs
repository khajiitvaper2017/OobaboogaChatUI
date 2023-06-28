using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using AsyncAwaitBestPractices.MVVM;
using OobaboogaChatUI.Models;
using OobaboogaChatUI.Properties;
using OobaboogaChatUI.Views;

namespace OobaboogaChatUI.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
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

        SendRequestCommand = new AsyncValueCommand<string>(OobaboogaClient.Generate, _ => !OobaboogaClient.IsBusy);
        
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