using NUnit.Framework;
using UnityEngine;
using OzGameLab01.Events;

namespace OzGameLab01.Tests.EditMode
{
    public sealed class EventDBTests
    {
        private EventDB _database;

        [SetUp]
        public void SetUp()
        {
            _database = ScriptableObject.CreateInstance<EventDB>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_database);
        }

        [Test]
        public void EmptyDatabase_ReturnsNullInsteadOfThrowing()
        {
            Assert.That(_database.GetRandomEvent(), Is.Null);
            Assert.That(_database.GetRandomTypeEvent(EventChoiceCategory.Quiz), Is.Null);
            Assert.That(_database.GetEventById("missing"), Is.Null);
        }

        [Test]
        public void SetDictionary_CanBeCalledRepeatedlyWithoutDuplicateKeyFailure()
        {
            EventSO battle = CreateEvent("battle");
            EventSO quiz = CreateEvent("quiz");
            _database.EventList_Battle.Add(battle);
            _database.EventList_Quiz.Add(quiz);

            Assert.That(_database.SetDictionary(), Is.True);
            Assert.That(_database.SetDictionary(), Is.True);
            Assert.That(_database.GetEventById("battle"), Is.SameAs(battle));
            Assert.That(_database.GetEventById("quiz"), Is.SameAs(quiz));
        }

        [Test]
        public void SetDictionary_IgnoresNullAndBlankIds()
        {
            EventSO noId = CreateEvent(string.Empty);
            EventSO valid = CreateEvent("valid");
            _database.EventList_Event.Add(null);
            _database.EventList_Event.Add(noId);
            _database.EventList_Event.Add(valid);

            Assert.That(() => _database.SetDictionary(), Throws.Nothing);
            Assert.That(_database.GetEventById("valid"), Is.SameAs(valid));
            Assert.That(_database.GetEventById(string.Empty), Is.Null);
        }

        private EventSO CreateEvent(string id)
        {
            EventSO eventSO = ScriptableObject.CreateInstance<EventSO>();
            eventSO.id = id;
            return eventSO;
        }
    }
}
