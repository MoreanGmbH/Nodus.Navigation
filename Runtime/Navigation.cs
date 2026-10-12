using Friflo.Engine.ECS;
using Friflo.Engine.Unity;
using Nodus.ECS;
using UnityEngine;
#if UNITY_EDITOR
using FrifloECS.Unity.EntityVisualize;
#endif

namespace Nodus.Navigation {
    [RequireComponent(typeof(ECSStore))]
    public sealed class Navigation : MonoBehaviour {
        public static EntityStore Store;
        [SerializeField] string storeId = "Navigation";

        void Awake() {
            Store = GetComponent<ECSStore>().EntityStore;
            EcsRegistry.RegisterStore(storeId, Store);
            EcsRegistry.RegisterComponent<Request>();
            EcsRegistry.RegisterComponent<RequestItem>();
            EcsRegistry.RegisterComponent<CommandStatus>();
            EcsRegistry.RegisterComponent<Tracked>();
            EcsRegistry.RegisterComponent<Cursor>();
            EcsRegistry.RegisterComponent<Snapshot>();
            EcsRegistry.RegisterComponent<HistoryAvailability>();
        }

        void OnDestroy() {
            EcsRegistry.UnregisterStore(storeId, GetComponent<ECSStore>().EntityStore);
            if (Store == GetComponent<ECSStore>().EntityStore)
                Store = null;
        }

        public static Entity CreateRequest(string label = null)
            => Store.CreateEntity(new Request { Action = ActionType.Commit, Label = label });

        public static Entity CreateRequest<T>(T action, string label = null) where T : struct, IComponent
            => CreateRequest(label).AddAction(action);

        public static Entity Back() => Store.CreateEntity(new Request { Action = ActionType.Back });
        public static Entity Forward() => Store.CreateEntity(new Request { Action = ActionType.Forward });
        public static Entity GoTo(int index) => Store.CreateEntity(new Request { Action = ActionType.GoTo, Index = index });
        public static Entity Load(string json, string label = null) => Store.CreateEntity(new Request { Action = ActionType.Load, Json = json, Label = label });

#if UNITY_EDITOR
        [SerializeField] bool visualize = true;
        void Start() {
            if (visualize)
                EntityVisualizer.Register(name, Store);
        }
#endif
    }
}
