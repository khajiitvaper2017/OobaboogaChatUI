using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;

namespace OobaboogaChatUI.Models;

public class Chat : ObservableCollection<ChatMessage>
{
    public delegate void MessageAddedEventHandler(string previousText);

    public static event MessageAddedEventHandler LastMessageChanged;

    public static void NotifyLastMessageChanged(string previousText = "")
    {
        LastMessageChanged.Invoke(previousText);
    }

    private PromptPreset _prompt;
    public string ChatHistoryPath { get; set; } = Path.Combine(Environment.CurrentDirectory, "ChatHistory");
    public Chat()
    {
    }

    public Chat(PromptPreset prompt)
    {
        if (prompt == null) return;
        Prompt = prompt;
    }
    public PromptPreset Prompt
    {
        get => _prompt;
        set
        {
            _prompt = value;
            if (Items.Count == 0)
                Add(new ChatMessage
                {
                    Message = value.Greeting,
                    Username = value.Name,
                    TimeStamp = DateTime.Now
                });
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Prompt)));
        }
    }

    public string ToPrompt()
    {
        var promptString = "";
        if (Prompt != null) promptString = Prompt.Context;

        var result = promptString + ToHistory();

        if (Prompt != null && Items.Last().Username == Prompt.Username) result += Environment.NewLine + Prompt.Name;
        return result;
    }

    public string ToImpersonatePrompt(string request)
    {
        var promptString = "";
        if (Prompt != null) promptString = Prompt.Context;

        var result = promptString + ToHistory();

        result += Environment.NewLine + Prompt.Username + request;
        return result;
    }

    public string ToHistory()
    {
        return Items.Aggregate("", (current, item) => current + Environment.NewLine + item.Username + item.Message);
    }

    public void Save()
    {
        if (!Directory.Exists(ChatHistoryPath))
            Directory.CreateDirectory(ChatHistoryPath);
        var path = Path.Combine(ChatHistoryPath, $"{DateTime.Now.ToFileTime()}history.json");

        var json = JsonSerializer.Serialize(this);

        File.WriteAllText(path, json);
    }

    public void Load(string? fileName = null)
    {
        Directory.SetCurrentDirectory(ChatHistoryPath);
        if (fileName == null)
        {
            if (!Directory.Exists(ChatHistoryPath))
                return;

            var files = Directory.GetFiles(ChatHistoryPath);
            fileName = files.MaxBy(x => x);

            if (!File.Exists(fileName))
                return;
        }
        else if (!File.Exists(fileName))
        {
            return;
        }

        var json = File.ReadAllText(fileName);
        var chat = JsonSerializer.Deserialize<Chat>(json);

        Items.Clear();
        foreach (var chatMessage in chat) Add(chatMessage);
        Prompt.Name = this.First().Username; //TODO: Prompt isn't saved when loading chat 
        Directory.SetCurrentDirectory(Environment.CurrentDirectory);
    }

    public List<string> GetAvailableHistory()
    {
        var path = Path.Combine(Environment.CurrentDirectory, "ChatHistory");
        if (!Directory.Exists(path))
            return new List<string>();

        List<string> filesWithoutDirectory = Directory.GetFiles(path).Select(Path.GetFileName).ToList();
        return filesWithoutDirectory;
    }
}