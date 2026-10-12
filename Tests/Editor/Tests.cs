using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;
using NUnit.Framework;
using Nodus.ECS;

namespace Nodus.Navigation.Tests {
    [ComponentKey("Nodus.Navigation.Tests.Panel")]
    public struct Panel : IComponent { public string Id; }
    public struct RuntimeData : IComponent { public int Value; }

    public class Tests {
        private EntityStore store;
        private SystemRoot systems;

        [SetUp]
        public void Setup() {
            store = new EntityStore();
            store.InitializeHistory();
            systems = new SystemRoot(store) { new HistorySystem() };
        }

        private void Update() { systems.Update(default); }
        private int PanelCount() {
            int count = 0;
            foreach (var entity in store.Query<Tracked, Panel>().Entities) count++;
            return count;
        }

        [Test]
        public void AvailabilityTracksCursorAndBranching() {
            var availability = store.GetHistory().GetComponent<HistoryAvailability>();
            Assert.IsFalse(availability.CanGoBack);
            Assert.IsFalse(availability.CanGoForward);
            store.CreateEntity(new Tracked(), new Panel { Id = "one" });
            store.Navigate(ActionType.Commit);
            Update();
            availability = store.GetHistory().GetComponent<HistoryAvailability>();
            Assert.IsTrue(availability.CanGoBack);
            Assert.IsFalse(availability.CanGoForward);
            store.Navigate(ActionType.Back);
            Update();
            availability = store.GetHistory().GetComponent<HistoryAvailability>();
            Assert.IsFalse(availability.CanGoBack);
            Assert.IsTrue(availability.CanGoForward);
        }

        [Test]
        public void OpenCloseBackForward() {
            var panel = store.CreateEntity(new Tracked(), new Panel { Id = "feedback" });
            store.Navigate(ActionType.Commit);
            Update();
            Assert.AreEqual(1, PanelCount());
            panel.DeleteEntity();
            store.Navigate(ActionType.Commit);
            Update();
            Assert.AreEqual(0, PanelCount());
            store.Navigate(ActionType.Back);
            Update();
            Assert.AreEqual(1, PanelCount());
            store.Navigate(ActionType.Forward);
            Update();
            Assert.AreEqual(0, PanelCount());
        }

        [Test]
        public void GoToAnySnapshotWithMultiplePanels() {
            store.CreateEntity(new Tracked(), new Panel { Id = "feedback" });
            store.CreateEntity(new Tracked(), new Panel { Id = "pdf" });
            store.Navigate(ActionType.Commit);
            Update();
            Assert.AreEqual(2, PanelCount());
            store.Navigate(ActionType.GoTo, index: 0);
            Update();
            Assert.AreEqual(0, PanelCount());
            store.Navigate(ActionType.GoTo, index: 1);
            Update();
            Assert.AreEqual(2, PanelCount());
        }

        [Test]
        public void SkipsNoOpAndPreservesRuntimeEntities() {
            var runtime = store.CreateEntity(new RuntimeData { Value = 12 });
            store.Navigate(ActionType.Commit);
            Update();
            Assert.AreEqual(1, store.GetHistory().ChildCount);
            store.CreateEntity(new Tracked(), new Panel { Id = "feedback" });
            store.Navigate(ActionType.Commit);
            Update();
            store.Navigate(ActionType.Back);
            Update();
            Assert.IsFalse(runtime.IsNull);
            Assert.AreEqual(12, runtime.GetComponent<RuntimeData>().Value);
        }

        [Test]
        public void UrlRoundTrip() {
            store.CreateEntity(new Tracked(), new Panel { Id = "feedback" });
            string url = store.ToUrl("https://nodus.example/view");
            store.Navigate(ActionType.Load, json: "[]");
            Update();
            Assert.AreEqual(0, PanelCount());
            store.NavigateUrl(url);
            Update();
            Assert.AreEqual(1, PanelCount());
        }

        [Test]
        public void GroupWaitsForAllActions() {
            var request = store.CreateEntity(new Request { Action = ActionType.Commit });
            request.AddAction(new Panel { Id = "first" }).AddAction(new Panel { Id = "second" });
            var a = request.ChildEntities[0];
            var b = request.ChildEntities[1];
            store.CreateEntity(new Tracked(), new Panel { Id = "visible" });

            Update();
            Assert.IsFalse(request.IsNull);
            Assert.AreEqual(1, store.GetHistory().ChildCount);

            a.GetComponent<CommandStatus>().Handled = true;
            Update();
            Assert.IsFalse(request.IsNull);

            b.GetComponent<CommandStatus>().Handled = true;
            Update();
            Assert.IsTrue(request.IsNull);
            Assert.IsTrue(a.IsNull);
            Assert.IsTrue(b.IsNull);
            Assert.AreEqual(2, store.GetHistory().ChildCount);
            Assert.AreEqual(1, PanelCount());
        }

        [Test]
        public void FluentRequestCreation() {
            var request = store.CreateEntity(new Request { Action = ActionType.Commit });
            Assert.AreEqual(request, request.AddAction(new Panel { Id = "feedback" }));
            Assert.AreEqual(1, request.ChildCount);
            Assert.AreEqual("feedback", request.ChildEntities[0].GetComponent<Panel>().Id);
        }

        [Test]
        public void CommitGroupWithNoChildren() {
            var request = store.CreateEntity(new Request { Action = ActionType.Commit });
            store.CreateEntity(new Tracked(), new Panel { Id = "test" });
            Update();
            Assert.IsTrue(request.IsNull);
            Assert.AreEqual(2, store.GetHistory().ChildCount);
        }

        [Test]
        public void BranchAfterBack() {
            store.CreateEntity(new Tracked(), new Panel { Id = "one" });
            store.Navigate(ActionType.Commit);
            Update();
            store.CreateEntity(new Tracked(), new Panel { Id = "two" });
            store.Navigate(ActionType.Commit);
            Update();
            Assert.AreEqual(3, store.GetHistory().ChildCount);
            store.Navigate(ActionType.Back);
            Update();
            store.CreateEntity(new Tracked(), new Panel { Id = "three" });
            store.Navigate(ActionType.Commit);
            Update();
            Assert.AreEqual(3, store.GetHistory().ChildCount);
            Assert.AreEqual(2, store.GetHistory().GetComponent<Cursor>().Index);
        }
    }
}
