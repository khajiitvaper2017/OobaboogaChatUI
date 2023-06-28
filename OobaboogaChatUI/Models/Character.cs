using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace OobaboogaChatUI.Models;

public class Character
{
    [JsonConstructor]
    public Character(string name, string context, string greeting)
    {
        Name = name;
        Context = context;
        Greeting = greeting;
    }

    public Character(Character character)
    {
        Name = character.Name;
        Context = character.Context;
        Greeting = character.Greeting;
    }
    [JsonProperty (PropertyName = "name")]
    public string Name { get; set; }
    [JsonProperty (PropertyName = "context")]
    public string Context { get; set; }
    [JsonProperty (PropertyName = "greeting")]
    public string Greeting { get; set; }
}