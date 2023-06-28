namespace OobaboogaChatUI.Models;

public class PromptPreset : Character
{
    public PromptPreset(string username, Character character) : base(character)
    {
        Username = username;
    }

    //TODO: Remove this class
    public string Username { get; set; }
}