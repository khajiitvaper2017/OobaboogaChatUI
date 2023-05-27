using System.ComponentModel;
using OobaboogaChatUI.Models;

namespace OobaboogaChatUI.ViewModels;

public partial class EditMessageViewModel : INotifyPropertyChanged
{
    public EditMessageViewModel()
    {
        SaveMessageCommand = new RelayCommand(_ =>
        {
            OriginalMessage.Message = Message.Message;
            OriginalMessage.Username = Message.Username;
        }, _ => OriginalMessage.Message != Message.Message ||
                OriginalMessage.Username != Message.Username);
    }

    public EditMessageViewModel(ChatMessage message) : this()
    {
        Message = new ChatMessage(message);
        OriginalMessage = message;
    }

    public ChatMessage Message { get; set; } = new();
    public ChatMessage OriginalMessage { get; set; } = new();
    public RelayCommand SaveMessageCommand { get; set; }
}