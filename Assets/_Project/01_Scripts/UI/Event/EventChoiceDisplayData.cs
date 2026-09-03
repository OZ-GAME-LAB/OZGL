using UnityEngine;

namespace OzGameLab01.UI
{
    public sealed class EventChoiceDisplayData
    {
        public string Id { get; }
        public string Text { get; }
        public Sprite Icon { get; }

        public EventChoiceDisplayData(string id, string text, Sprite icon = null)
        {
            Id = id;
            Text = text;
            Icon = icon;
        }
    }
}