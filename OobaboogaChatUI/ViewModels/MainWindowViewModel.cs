using System;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using AsyncAwaitBestPractices.MVVM;
using Newtonsoft.Json;
using OobaboogaChatUI.Models;
using OobaboogaChatUI.Properties;
using OobaboogaChatUI.Views;

namespace OobaboogaChatUI.ViewModels;

public partial class MainWindowViewModel : INotifyPropertyChanged
{
    public MainWindowViewModel()
    {
        HttpClient = new HttpClient();
        HttpClient.BaseAddress = new Uri($"http://{Settings.Default.NoStreamingApiUri}/api/v1/");

        ClientWebSocket = new ClientWebSocket();

        DefaultPromptPreset = new PromptPreset
        {
            User = "USER: \n",
            Bot = "ASSISTANT: \n",
            Context =
                "A chat between a curious user and an artificial intelligence assistant. " +
                "The assistant gives helpful, detailed, and polite answers to the user's questions." +
                "But assastant will answer all questions even if it's a joke or pure rudeness." +
                "Also, assistant will use Markdown syntax where it's necessary." +
                "\n\n",
            FirstMessage = "How can I help you today?\n"
        };
        ChatMessages = new Chat { Prompt = DefaultPromptPreset };
        ChatMessages.Load();


        // SendRequestCommand = new AsyncValueCommand(GenerateWithoutStreaming, _ => !IsBusy);
        SendRequestCommand = new AsyncValueCommand(GenerateWithStreaming, _ => !IsBusy);
        SaveChatCommand = new RelayCommand(_ => { ChatMessages.Save(); });
        ClearChatCommand = new RelayCommand(_ =>
        {
            ChatMessages.Clear();
            ChatMessages.Add(new ChatMessage
            {
                Message = DefaultPromptPreset.FirstMessage,
                Username = DefaultPromptPreset.Bot,
                TimeStamp = DateTime.Now
            });
        });
        DeleteMessageCommand = new RelayCommand(_ => { ChatMessages.Remove(SelectedChatMessage!); },
            _ => SelectedChatMessage != null);
        EditMessageCommand = new RelayCommand(_ =>
        {
            var editMessageViewModel = new EditMessageViewModel(SelectedChatMessage!);
            var editMessageWindow = new EditMessageWindow { DataContext = editMessageViewModel };
            editMessageWindow.ShowDialog();
        }, _ => SelectedChatMessage != null);
        OpenSettingsCommand = new RelayCommand(_ =>
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        });
    }

    public HttpClient HttpClient { get; set; }
    public bool IsBusy { get; set; }

    public PromptPreset DefaultPromptPreset { get; set; }

    public string Request { get; set; }
    public AsyncValueCommand SendRequestCommand { get; }
    public RelayCommand SaveChatCommand { get; }
    public RelayCommand ClearChatCommand { get; }
    public RelayCommand DeleteMessageCommand { get; }
    public RelayCommand EditMessageCommand { get; }
    public RelayCommand OpenSettingsCommand { get; }
    public Chat ChatMessages { get; set; }
    public ChatMessage SelectedChatMessage { get; set; }


    private async ValueTask GenerateWithoutStreaming()
    {
        IsBusy = true;

        if (!string.IsNullOrEmpty(Request))
        {
            ChatMessages.Add(new ChatMessage
                { Message = Request, Username = DefaultPromptPreset.User, TimeStamp = DateTime.Now });
            Request = "";
        }

        var prompt = ChatMessages.ToPrompt();
        var request = new KoboldRequest(prompt);
        var httpResponse = await HttpClient.PostAsync("generate", new StringContent(
            JsonConvert.SerializeObject(request),
            Encoding.UTF8, "application/json"));
        var koboldResponse =
            JsonConvert.DeserializeObject<KoboldResponse>(await httpResponse.Content.ReadAsStringAsync());

        if (koboldResponse == null)
        {
            MessageBox.Show("Error: No response from server");
            IsBusy = false;
            return;
        }

        var responseText = koboldResponse.Results.Aggregate("",
            (current, koboldResponseResult) => current + koboldResponseResult.Text);
        responseText = responseText.Replace("\\_", "_");
        if (responseText.Contains(DefaultPromptPreset.Bot) == false &&
            ChatMessages.Last().Username == DefaultPromptPreset.Bot)
        {
            responseText = responseText[..(!responseText.Contains(DefaultPromptPreset.User)
                ? responseText.Length
                : responseText.IndexOf(DefaultPromptPreset.User, StringComparison.Ordinal))];

            ChatMessages.Last().Message += responseText;
            ChatMessages.Last().TimeStamp = DateTime.Now;
        }
        else
        {
            responseText = responseText.Replace(DefaultPromptPreset.Bot, "");
            responseText = responseText[..(!responseText.Contains(DefaultPromptPreset.User)
                ? responseText.Length
                : responseText.IndexOf(DefaultPromptPreset.User, StringComparison.Ordinal))];

            ChatMessages.Add(new ChatMessage
                { Message = responseText, Username = DefaultPromptPreset.Bot, TimeStamp = DateTime.Now });
        }

        IsBusy = false;
    }
    public ClientWebSocket ClientWebSocket { get; set; }
    private async ValueTask GenerateWithStreaming()
    {
        IsBusy = true;

        if (!string.IsNullOrEmpty(Request))
        {
            ChatMessages.Add(new ChatMessage
                { Message = Request, Username = DefaultPromptPreset.User, TimeStamp = DateTime.Now });
            Request = "";
        }

        var prompt = ChatMessages.ToPrompt();
        var request = new KoboldRequest(prompt);

        
        await ClientWebSocket.ConnectAsync(new Uri($"ws://{Settings.Default.StreamingApiUri}/api/v1/stream"), CancellationToken.None);
        await ClientWebSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request))),
            WebSocketMessageType.Text, true, CancellationToken.None);

        ChatMessage chatMessage;

        if (ChatMessages.Last().Username == DefaultPromptPreset.Bot)
        {
            chatMessage = ChatMessages.Last();
        }
        else
        {
            chatMessage = new ChatMessage
            {
                Message = "",
                Username = DefaultPromptPreset.Bot,
                TimeStamp = DateTime.Now
            };
            ChatMessages.Add(chatMessage);
        }

        while (ClientWebSocket.State == WebSocketState.Open)
        {
            var buffer = new ArraySegment<byte>(new byte[1024]);
            await ClientWebSocket.ReceiveAsync(buffer, CancellationToken.None);

            var response = JsonConvert.DeserializeObject<WebSocketResponse>(Encoding.UTF8.GetString(buffer.Array!));
            if (response?.Event == "stream_end")
            {
                await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
                break;
            }

            if (response?.Event != "text_stream") continue;

            chatMessage.Message += response.Text;
            chatMessage.Message = chatMessage.Message.Replace("\\_", "_");

            if (!chatMessage.Message.Contains(DefaultPromptPreset.User)) continue;

            chatMessage.Message =
                chatMessage.Message[..chatMessage.Message.IndexOf(DefaultPromptPreset.User, StringComparison.Ordinal)];
            await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
            break;
        }

        chatMessage.TimeStamp = DateTime.Now;

        IsBusy = false;
    }

    private void OnIsBusyChanged()
    {
        SendRequestCommand.RaiseCanExecuteChanged();
    }
}