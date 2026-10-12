using Friflo.Engine.ECS;

namespace Nodus.Navigation {
    [ComponentKey("Nodus.Navigation.Tracked")]
    public struct Tracked : IComponent { }

    [ComponentKey("Nodus.Navigation.Cursor")]
    public struct Cursor : IComponent {
        public int Index;
    }

    [ComponentKey("Nodus.Navigation.HistoryAvailability")]
    public struct HistoryAvailability : IComponent {
        public int Index;
        public int Count;
        public bool CanGoBack;
        public bool CanGoForward;
    }

    [ComponentKey("Nodus.Navigation.Snapshot")]
    public struct Snapshot : IComponent {
        public string Json;
        public string Label;
    }

    public enum ActionType { Commit, Back, Forward, GoTo, Load }

    [ComponentKey("Nodus.Navigation.Request")]
    public struct Request : IComponent {
        public ActionType Action;
        public int Index;
        public string Json;
        public string Label;
    }

    [ComponentKey("Nodus.Navigation.RequestItem")]
    public struct RequestItem : IComponent {
        public bool Handled;
    }
}
