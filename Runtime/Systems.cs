using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Systems;

namespace Nodus.Navigation {
    public sealed class HistorySystem : QuerySystem<Request> {
        protected override void OnAddStore(EntityStore store) => store.InitializeHistory();

        protected override void OnUpdate() {
            if (Query.Count == 0)
                return;
            var store = Query.Store;

            foreach (var entity in Query.Entities.ToEntityList()) {
                if (entity.IsNull || !entity.AreActionsHandled())
                    continue;
                var request = entity.GetComponent<Request>();

                switch (request.Action) {
                    case ActionType.Commit:
                        store.Commit(request.Label);
                        break;
                    case ActionType.Back:
                        store.Back();
                        break;
                    case ActionType.Forward:
                        store.Forward();
                        break;
                    case ActionType.GoTo:
                        store.GoTo(request.Index);
                        break;
                    case ActionType.Load:
                        store.RestoreJson(request.Json);
                        store.Commit(request.Label);
                        break;
                }

                entity.DeleteRequest();
            }
        }
    }
}
