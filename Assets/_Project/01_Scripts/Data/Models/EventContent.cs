using System.Collections.Generic;
using System.Linq;
using OzGameLab01.Events;

namespace OzGameLab01.Data
{
    /// <summary>Detached event definition/instance; reward text edits never mutate the authoring asset.</summary>
    public sealed class EventContent
    {
        public string id;
        public string eventTitle;
        public string eventDialog;
        public EventCategory eventCategory;
        public EventChoiceCategory pool;
        public List<EventChoice> choices;

        public EventContent Copy() => new EventContent
        {
            id = id, eventTitle = eventTitle, eventDialog = eventDialog, eventCategory = eventCategory,
            pool = pool, choices = choices?.Select(choice => choice?.Copy()).ToList()
        };
        public static EventContent FromAsset(EventSO asset, EventChoiceCategory pool)
            => asset == null ? null : new EventContent
            {
                id = asset.id, eventTitle = asset.eventTitle, eventDialog = asset.eventDialog,
                eventCategory = asset.eventCategory, pool = pool,
                choices = asset.choices?.Select(choice => choice?.Copy()).ToList()
            };
    }
}
