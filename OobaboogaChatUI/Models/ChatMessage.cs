using System;
using System.ComponentModel;

namespace OobaboogaChatUI.Models;

public partial class ChatMessage : INotifyPropertyChanged
{
    public ChatMessage(ChatMessage message)
    {
        Message = message.Message;
        Username = message.Username;
        TimeStamp = message.TimeStamp;
    }

    public ChatMessage()
    {
        TimeStamp = DateTime.Now;
    }

    public string Message { get; set; } = "";
    public string Username { get; set; } = "";
    public DateTime TimeStamp { get; set; }
}