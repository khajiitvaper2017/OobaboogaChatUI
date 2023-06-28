using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using OobaboogaChatUI.Properties;

namespace OobaboogaChatUI.Models;

public partial class OobaboogaClient : INotifyPropertyChanged
{
    public OobaboogaClient(ClientWebSocket clientWebSocket, PromptPreset promptPreset)
    {
        IsStreaming = true;

        ClientWebSocket = clientWebSocket;
        PromptPreset = promptPreset;
        ChatMessages = new Chat { Prompt = PromptPreset };
    }

    public OobaboogaClient(HttpClient httpClient, PromptPreset promptPreset)
    {
        IsStreaming = false;

        HttpClient = httpClient;
        PromptPreset = promptPreset;
        ChatMessages = new Chat { Prompt = PromptPreset };
    }

    public HttpClient HttpClient { get; set; }
    public ClientWebSocket ClientWebSocket { get; set; }
    public PromptPreset PromptPreset { get; set; }
    public Chat ChatMessages { get; set; }
    public ChatMessage? SelectedChatMessage { get; set; }
    public bool IsStreaming { get; set; }
    public bool IsBusy { get; set; }

    public event EventHandler? IsBusyChanged;

    private void OnIsBusyChanged()
    {
        IsBusyChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UseStreaming(bool useStreaming, HttpClient? httpClient = null, ClientWebSocket? clientWebSocket = null)
    {
        IsStreaming = useStreaming;

        HttpClient = httpClient ?? new HttpClient
            { BaseAddress = new Uri($"http://{Settings.Default.NoStreamingApiUri}/api/v1/") };
        ClientWebSocket = clientWebSocket ?? new ClientWebSocket();
    }

    public void LoadChat(string? fileName = null)
    {
        ChatMessages.Load(fileName);
    }

    public async ValueTask Generate(string? request)
    {
        if (IsStreaming)
            await GenerateWithStreaming(request);
        else
            await GenerateWithoutStreaming(request);
    }

    public async ValueTask GenerateWithoutStreaming(string? request)
    {
        IsBusy = true;

        if (!string.IsNullOrEmpty(request))
            ChatMessages.Add(new ChatMessage
                { Message = request, Username = PromptPreset.User, TimeStamp = DateTime.Now });

        var prompt = ChatMessages.ToPrompt();
        var koboldRequest = new KoboldRequest(prompt);
        Debug.WriteLine(JsonConvert.SerializeObject(koboldRequest));
        var httpResponse = await HttpClient.PostAsync("generate", new StringContent(
            JsonConvert.SerializeObject(koboldRequest),
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
        if (responseText.Contains(PromptPreset.Bot) == false &&
            ChatMessages.Last().Username == PromptPreset.Bot)
        {
            responseText = responseText[..(!responseText.Contains(PromptPreset.User)
                ? responseText.Length
                : responseText.IndexOf(PromptPreset.User, StringComparison.Ordinal))];

            var previous = ChatMessages.Last().Message;
            ChatMessages.Last().Message += responseText;
            ChatMessages.Last().TimeStamp = DateTime.Now;
            ChatMessages.NotifyLastMessageChanged(previous);
        }
        else
        {
            responseText = responseText.Replace(PromptPreset.Bot, "");
            responseText = responseText[..(!responseText.Contains(PromptPreset.User)
                ? responseText.Length
                : responseText.IndexOf(PromptPreset.User, StringComparison.Ordinal))];

            ChatMessages.Add(new ChatMessage
                { Message = responseText, Username = PromptPreset.Bot, TimeStamp = DateTime.Now });
            ChatMessages.NotifyLastMessageChanged();
        }

        IsBusy = false;
    }

    private async ValueTask GenerateWithStreaming(string? request)
    {
        IsBusy = true;

        if (!string.IsNullOrEmpty(request))
        {
            ChatMessages.Add(new ChatMessage
                { Message = request, Username = PromptPreset.User, TimeStamp = DateTime.Now });
            request = "";
        }

        var prompt = ChatMessages.ToPrompt();
        var koboldRequest = new KoboldRequest(prompt);


        await ClientWebSocket.ConnectAsync(new Uri($"ws://{Settings.Default.StreamingApiUri}/api/v1/stream"),
            CancellationToken.None);
        await ClientWebSocket.SendAsync(
            new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(koboldRequest))),
            WebSocketMessageType.Text, true, CancellationToken.None);

        ChatMessage chatMessage;

        if (ChatMessages.Last().Username == PromptPreset.Bot)
        {
            chatMessage = ChatMessages.Last();
        }
        else
        {
            chatMessage = new ChatMessage
            {
                Message = "",
                Username = PromptPreset.Bot,
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

            if (!chatMessage.Message.Contains(PromptPreset.User)) continue;

            chatMessage.Message =
                chatMessage.Message[..chatMessage.Message.IndexOf(PromptPreset.User, StringComparison.Ordinal)];
            await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
            break;
        }

        ChatMessages.NotifyLastMessageChanged();
        chatMessage.TimeStamp = DateTime.Now;

        IsBusy = false;
    }
}