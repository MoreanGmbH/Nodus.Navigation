using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Friflo.Engine.ECS;
using Friflo.Engine.ECS.Serialize;
using Nodus.ECS;

namespace Nodus.Navigation {
    public static class Extensions {
        public static Entity AddAction<T>(this Entity request, T action) where T : struct, IComponent {
            if (!request.HasComponent<Request>() || request.GetComponent<Request>().Action != ActionType.Commit)
                throw new InvalidOperationException("Actions can only be added to a Commit request.");
            var child = request.Store.CreateEntity(action, new CommandStatus());
            request.AddChild(child);
            return request;
        }

        internal static bool AreActionsHandled(this Entity request) {
            foreach (var child in request.ChildEntities) {
                if (child.TryGetComponent<CommandStatus>(out var state)) {
                    if (!state.Handled)
                        return false;
                } else if (!child.TryGetComponent<RequestItem>(out var legacy) || !legacy.Handled) {
                    return false;
                }
            }
            return true;
        }

        internal static void DeleteRequest(this Entity request) {
            while (request.ChildCount > 0) request.ChildEntities[request.ChildCount - 1].DeleteRequest();
            request.DeleteEntity();
        }

        private static void UpdateAvailability(this Entity history) {
            int index = history.GetComponent<Cursor>().Index;
            history.AddComponent(new HistoryAvailability {
                Index = index,
                Count = history.ChildCount,
                CanGoBack = index > 0,
                CanGoForward = index + 1 < history.ChildCount
            });
        }

        public static Entity InitializeHistory(this EntityStore store) {
            foreach (var entity in store.Query<Cursor>().Entities) return entity;
            var history = store.CreateEntity(new Cursor { Index = 0 });
            history.AddChild(store.SaveSnapshot("Initial"));
            history.UpdateAvailability();
            return history;
        }

        public static Entity GetHistory(this EntityStore store) {
            foreach (var entity in store.Query<Cursor>().Entities) return entity;
            return store.InitializeHistory();
        }

        public static void Navigate(this EntityStore store, ActionType action, int index = 0, string json = null, string label = null) {
            store.CreateEntity(new Request { Action = action, Index = index, Json = json, Label = label });
        }

        public static void Commit(this EntityStore store, string label = null) {
            var history = store.GetHistory();
            int index = history.GetComponent<Cursor>().Index;
            string json = store.ToJson();
            if (json == history.ChildEntities[index].GetComponent<Snapshot>().Json) return;

            while (history.ChildCount > index + 1) history.ChildEntities[history.ChildCount - 1].DeleteEntity();
            history.AddChild(store.SaveSnapshot(label, json));
            history.AddComponent(new Cursor { Index = history.ChildCount - 1 });
            history.UpdateAvailability();
        }

        public static bool Back(this EntityStore store) {
            var history = store.GetHistory();
            return store.GoTo(history.GetComponent<Cursor>().Index - 1);
        }

        public static bool Forward(this EntityStore store) {
            var history = store.GetHistory();
            return store.GoTo(history.GetComponent<Cursor>().Index + 1);
        }

        public static bool GoTo(this EntityStore store, int index) {
            var history = store.GetHistory();
            if (index < 0 || index >= history.ChildCount || index == history.GetComponent<Cursor>().Index) return false;
            var snapshot = history.ChildEntities[index];
            var previous = new List<Entity>();
            foreach (var entity in store.Query<Tracked>().Entities) previous.Add(entity);
            foreach (var entity in previous) entity.DeleteEntity();
            foreach (var saved in snapshot.ChildEntities) {
                var restored = saved.CloneEntity();
                restored.AddComponent(new Tracked());
            }
            history.AddComponent(new Cursor { Index = index });
            history.UpdateAvailability();
            return true;
        }

        public static string ToJson(this EntityStore store) {
            var converter = new EntityConverter();
            var values = new List<string>();
            foreach (var entity in store.Query<Tracked>().Entities) {
                var value = converter.EntityToDataEntity(entity, null, false);
                value.pid = 0;
                value.children = null;
                values.Add(value.DebugJSON);
            }
            values.Sort(StringComparer.Ordinal);
            return "[" + string.Join(",", values) + "]";
        }

        public static void RestoreJson(this EntityStore store, string json) {
            if (string.IsNullOrEmpty(json)) throw new ArgumentException("Invalid navigation JSON.", nameof(json));
            var parsed = new List<DataEntity>();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using (var stream = new MemoryStream(bytes, 0, bytes.Length, false, true)) {
                var result = new EntitySerializer().ReadEntities(parsed, stream);
                if (result.error != null) throw new FormatException(result.error);
            }

            var staged = new List<Entity>();
            var converter = new EntityConverter();
            try {
                foreach (var value in parsed) {
                    var entity = store.CreateEntity(default(Tags));
                    staged.Add(entity);
                    value.pid = entity.Pid;
                    value.children = null;
                    converter.DataEntityToEntity(value, store, out string error);
                    if (error != null) throw new FormatException(error);
                    if (!entity.HasComponent<Tracked>()) throw new FormatException("Navigation entity is missing Tracked.");
                    entity.RemoveComponent<Tracked>();
                }
            } catch {
                foreach (var entity in staged) if (!entity.IsNull) entity.DeleteEntity();
                throw;
            }

            var previous = new List<Entity>();
            foreach (var entity in store.Query<Tracked>().Entities) previous.Add(entity);
            foreach (var entity in previous) entity.DeleteEntity();
            foreach (var entity in staged) entity.AddComponent(new Tracked());
        }

        public static string ToUrl(this EntityStore store, string baseUrl) {
            return baseUrl + (baseUrl.Contains("?") ? "&" : "?") + "nav=" + Uri.EscapeDataString(store.ToJson());
        }

        public static void NavigateUrl(this EntityStore store, string url) {
            int start = url.IndexOf('?');
            if (start < 0) throw new FormatException("URL has no navigation state.");
            int fragment = url.IndexOf('#', start);
            string query = url.Substring(start + 1, (fragment < 0 ? url.Length : fragment) - start - 1);
            foreach (string parameter in query.Split('&')) {
                int separator = parameter.IndexOf('=');
                if (separator < 0 || parameter.Substring(0, separator) != "nav") continue;
                store.Navigate(ActionType.Load, json: Uri.UnescapeDataString(parameter.Substring(separator + 1)));
                return;
            }
            throw new FormatException("URL has no nav parameter.");
        }

        private static Entity SaveSnapshot(this EntityStore store, string label, string json = null) {
            var snapshot = store.CreateEntity(new Snapshot { Label = label, Json = json ?? store.ToJson() });
            var source = new List<Entity>();
            foreach (var entity in store.Query<Tracked>().Entities) source.Add(entity);
            foreach (var entity in source) {
                var clone = entity.CloneEntity();
                clone.RemoveComponent<Tracked>();
                snapshot.AddChild(clone);
            }
            return snapshot;
        }
    }
}
