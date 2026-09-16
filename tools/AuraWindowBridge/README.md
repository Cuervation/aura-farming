# AuraWindowBridge

Deterministic .NET 8 client for the `AuraWindowBridge.v1` Unity Named Pipe. It has no UI Automation, no screen coordinates, and no Unity business logic.

```powershell
dotnet run --project .\AuraWindowBridge.csproj -- ping
dotnet run --project .\AuraWindowBridge.csproj -- getHierarchy timeoutMs=15000
dotnet run --project .\AuraWindowBridge.csproj -- getComponents objectPath=Canvas componentType=UnityEngine.Canvas
```

All responses are one JSON object with `success`, `command`, `error`, and `data`. Mutations are explicit commands; `setComponentProperty` requires an existing `objectPath`, `componentType`, `propertyPath`, and compatible `valueJson`.
