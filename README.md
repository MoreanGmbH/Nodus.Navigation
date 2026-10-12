# Nodus.Navigation

ECS navigation history for tracked Friflo entities: snapshots, Back/Forward, GoTo, state URLs and grouped navigation requests.

## Installation

```powershell
git submodule add https://github.com/MoreanGmbH/Nodus.Navigation.git Packages/com.nodus.navigation
```

Requires Nodus.ECS and Friflo.Engine.ECS with Unity integration.

## Setup

Add `ECSStore`, `ECSSystemSet` and `Navigation` to the prefab. Add `HistorySystem` to the **Update** group, after any action systems. It initializes history through Friflo `OnAddStore`. `Navigation` registers the store under `Navigation` and the package's component keys.

## Use

```csharp
Navigation.Back();
Navigation.Forward();
Navigation.GoTo(0);
Navigation.CreateRequest("Show feedback").AddAction(new MyAction());
```

To make a reusable Back button, create an ECS Entities Definition targeting store `Navigation`, containing an entity with `Nodus.Navigation.Request` and `Action=Back`. Assign it to an `EntitiesButton` in UI Builder.

`HistoryAvailability` on the cursor entity exposes `Index`, `Count`, `CanGoBack`, and `CanGoForward`. Consumers can bind UI visibility to that ECS state.

Track desired state with `Nodus.Navigation.Tracked` and commit after modifying it. Back/Forward restore snapshots of tracked entities. JSON and URL methods are available as `EntityStore` extensions.

## Tests

Run **EditMode** tests. Add `"testables": ["com.nodus.navigation"]` to `Packages/manifest.json` if tests are not listed.
