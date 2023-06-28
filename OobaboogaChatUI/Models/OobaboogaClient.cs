using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.TextFormatting;
using Newtonsoft.Json;
using OobaboogaChatUI.Properties;
using OobaboogaChatUI.ViewModels;

namespace OobaboogaChatUI.Models;

public partial class OobaboogaClient : INotifyPropertyChanged
{
    private OobaboogaClient(PromptPreset promptPreset)
    {
        PromptPreset = promptPreset;
        ChatMessages = new Chat { Prompt = PromptPreset };
        CharacterList = new List<Character>();
        var charactersPath = Path.Combine(Environment.CurrentDirectory, "Characters");

        var characterFiles = Directory.GetFiles(charactersPath);

        foreach (var characterFile in characterFiles)
        {
            var json = File.ReadAllText(characterFile);
            Character character = JsonConvert.DeserializeObject<Character>(json);
            CharacterList.Add(character);
        }
    }

    public void SelectCharacter(Character character)
    {
        PromptPreset = new PromptPreset(PromptPreset.Username, character);

        ChatMessages = new Chat { Prompt = PromptPreset };
    }
    public OobaboogaClient(ClientWebSocket clientWebSocket, PromptPreset promptPreset) : this(promptPreset)
    {
        IsStreaming = true;
        ClientWebSocket = clientWebSocket;
    }

    public OobaboogaClient(HttpClient httpClient, PromptPreset promptPreset) : this(promptPreset)
    {
        IsStreaming = false;
        HttpClient = httpClient;
    }

    public HttpClient HttpClient { get; set; }
    public ClientWebSocket ClientWebSocket { get; set; }
    public PromptPreset PromptPreset { get; set; }
    public Chat ChatMessages { get; set; }
    public ChatMessage? SelectedChatMessage { get; set; }
    public bool IsStreaming { get; set; }
    public bool IsBusy { get; set; }
    public List<Character> CharacterList { get; set; }

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

    public async ValueTask Impersonate(MainWindowViewModel mwvm)
    {
        PromptPreset = ChatMessages.Prompt;
        IsBusy = true;

        var prompt = ChatMessages.ToImpersonatePrompt(mwvm.Request);
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

        responseText = responseText[..(!responseText.Contains(PromptPreset.Name)
            ? responseText.Length
            : responseText.IndexOf(PromptPreset.Name, StringComparison.Ordinal))];

        mwvm.Request += responseText.TrimEnd();
        IsBusy = false;
        
    }
    public async ValueTask Generate(string? request)
    {
        PromptPreset = ChatMessages.Prompt;
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
                { Message = request, Username = PromptPreset.Username, TimeStamp = DateTime.Now });

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
        if (responseText.Contains(PromptPreset.Name) == false &&
            ChatMessages.Last().Username == PromptPreset.Name)
        {
            responseText = responseText[..(!responseText.Contains(PromptPreset.Username)
                ? responseText.Length
                : responseText.IndexOf(PromptPreset.Username, StringComparison.Ordinal))];

            var previous = ChatMessages.Last().Message;
            ChatMessages.Last().Message += responseText;
            ChatMessages.Last().TimeStamp = DateTime.Now;
            Chat.NotifyLastMessageChanged(previous);
        }
        else
        {
            responseText = responseText.Replace(PromptPreset.Name, "");
            responseText = responseText[..(!responseText.Contains(PromptPreset.Username)
                ? responseText.Length
                : responseText.IndexOf(PromptPreset.Username, StringComparison.Ordinal))];

            ChatMessages.Add(new ChatMessage
                { Message = responseText, Username = PromptPreset.Name, TimeStamp = DateTime.Now });
            Chat.NotifyLastMessageChanged();
        }

        IsBusy = false;
    }

    private async ValueTask GenerateWithStreaming(string? request)
    {
        IsBusy = true;

        if (!string.IsNullOrEmpty(request))
        {
            ChatMessages.Add(new ChatMessage
                { Message = request, Username = PromptPreset.Username, TimeStamp = DateTime.Now });
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

        if (ChatMessages.Last().Username == PromptPreset.Name)
        {
            chatMessage = ChatMessages.Last();
        }
        else
        {
            chatMessage = new ChatMessage
            {
                Message = "",
                Username = PromptPreset.Name,
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

            if (!chatMessage.Message.Contains(PromptPreset.Username)) continue;

            chatMessage.Message =
                chatMessage.Message[..chatMessage.Message.IndexOf(PromptPreset.Username, StringComparison.Ordinal)];
            await ClientWebSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "OK", CancellationToken.None);
            break;
        }

        Chat.NotifyLastMessageChanged();
        chatMessage.TimeStamp = DateTime.Now;

        IsBusy = false;
    }
}