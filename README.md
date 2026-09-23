# Modular GUI — AAS-Driven Mixed Reality Digital Twin

A Unity / MRTK3 mixed reality application that generates its entire in-scene GUI at runtime from an **Asset Administration Shell (AAS)** server, then overlays it onto a 3D model of the Fischertechnik **Learning Factory 4.0** teaching line. Instead of hand-placing labels and dialog boxes in the Unity Editor, the app asks an [Eclipse BaSyx](https://www.eclipse.org/basyx/) AAS server "what elements exist and where do they go," builds them on the fly, and then keeps them live with sensor data streamed over OPC UA via MQTT.

It was built as a HoloLens 2 style mixed reality experience for demonstrating a digital twin of a modular production line, where the GUI itself is part of the "digital twin" data model rather than being baked into the app.

## Why this exists

Most MR/AR dashboards hardcode their UI: every label, panel, and position is placed by hand in the editor and re-baked whenever the physical line changes. This project flips that around — the layout, text, and data bindings of every on-model dialog live as AAS submodel elements on a BaSyx server. Change the submodel, and the next app launch reflects it without touching Unity. The 3D model itself only needs to expose named "line parts," and the GUI submodel references those parts to anchor itself.

## Key features

- **Runtime GUI generation from AAS data** — `AASFetcher` queries a `GUI_Submodel` on the AAS server and instantiates one dialog prefab per submodel element, populating header/body text, font size, position, rotation, and scale straight from the returned JSON.
- **Model-relative anchoring** — dialogs can reference a `LinkedPart` in a separate `LineParts_Submodel` (loaded by `AASLinePartsLoader`) so they're positioned relative to the physical part they describe instead of using fixed world coordinates.
- **Live sensor data via OPC UA over MQTT** — any submodel element with a `NodeId` is registered for subscription; incoming values are pushed onto a thread-safe queue and applied to the matching `TextMeshProUGUI` label on the next `Update()` tick.
- **Simulated data mode** — `SimulatedDataUpdater` / `SimulatedVariableNode` can drive the same text fields with fake fluctuating values, so the GUI can be demoed without a live OPC UA/MQTT backend.
- **Interactive model controls** — `ModelManager` handles showing/hiding line parts, rotating and scaling the whole model, and toggling per-part dialogs; `TogglePartController` wires individual MRTK toggle switches to specific parts.
- **Hand menu & guided walkthrough** — `HandMenuManager` and `DialogNavigator` provide a paged, hand-menu-driven onboarding/explanation flow that can hide the model and switch to an interactive "explore mode" on its last page.
- **External configuration** — server address, submodel IDs, and the MQTT broker URL are all read from a single `config.json` at runtime (via `ConfigLoader`), so the same build can point at different AAS servers without recompiling.

## How it works

```
config.json ──▶ ConfigLoader
                    │
                    ▼
        AASLinePartsLoader ──GET──▶ BaSyx: LineParts_Submodel
                    │                (idShort → PosX/Y/Z)
                    ▼
             AASFetcher ──GET──▶ BaSyx: GUI_Submodel
                    │             (dialogs: text, font size,
                    │              position/rotation/scale,
                    │              optional NodeId, optional
                    │              LinkedPart reference)
                    ▼
      Instantiate CanvasDialog prefabs,
      anchor to model / LineParts position
                    │
                    ▼
        NodeIds with data ──▶ OpcUaSubscriber (MQTT)
                    │
                    ▼
       Live values pushed into the
       matching TextMeshProUGUI labels
```

1. `ConfigLoader` reads `config.json` and exposes the BaSyx server URL, the two submodel IDs, and the MQTT broker URL as static fields.
2. `AASLinePartsLoader` fetches the `LineParts_Submodel` and recursively walks its `SubmodelElementCollection`/`Entity` structure to build a lookup of part name → world position.
3. `AASFetcher` fetches the `GUI_Submodel`, and for every element instantiates a dialog prefab, sets its text/size, resolves its position (either fixed, or relative to a linked line part), and — if the element declares a `NodeId` — registers that node for live updates.
4. Once all dialogs are placed, `AASFetcher` opens an OPC UA (MQTT-transported) subscription for every registered `NodeId` and applies incoming values to the corresponding label each frame via a thread-safe queue.
5. `ModelManager`, `HandMenuManager`, `DialogNavigator`, and `TogglePartController` provide the interactive layer on top: rotating/scaling the model, toggling individual parts and their dialogs, and a guided multi-page introduction.

## Repository contents

```
Modular_GUI/
├── GUI_Scripts/
│   ├── AASFetcher.cs             # Fetches GUI_Submodel, instantiates & positions dialogs, drives OPC UA subscription
│   ├── AASLinePartsLoader.cs     # Fetches LineParts_Submodel, builds part-name → position lookup
│   ├── AppManager.cs             # Simple application-quit helper
│   ├── ConfigLoader.cs           # Loads config.json (server URL, submodel IDs, MQTT broker)
│   ├── DialogNavigator.cs        # Paged header/content walkthrough, drives ModelManager state
│   ├── HandMenuManager.cs        # MRTK hand-menu bindings (model/dialog toggle, page nav)
│   ├── ModelManager.cs           # Show/hide parts, rotate & scale model, toggle switches menu
│   ├── SimpleJSON.cs             # Bunny83's SimpleJSON parser (MIT licensed, see file header)
│   ├── SimulatedDataUpdater.cs   # Randomized value updates for demoing without live OPC UA data
│   ├── SimulatedVariableNode.cs  # POCO describing a simulated OPC UA variable node
│   └── TogglePartController.cs  # Binds an MRTK toggle switch to a part + its dialog
├── AAS_GUI_Submodel.json         # Example/reference GUI_Submodel export (dialog definitions)
├── AAS_GUI_Template.json         # Example AAS shell referencing the GUI and LineParts submodels
├── config.json                   # Runtime configuration (server URL, submodel IDs, MQTT broker)
├── SampleScene.unity              # Unity scene wiring the model, MRTK rig, and GUI prefabs together
└── Gesamtmodell_5_18_09 (1).fbx   # 3D model of the Fischertechnik Learning Factory 4.0 line
```

This repository holds the application-specific scripts, scene, 3D model, and sample AAS data — it is not a full exported Unity project (no `Assets/`, `ProjectSettings/`, or `Packages/` folders). See [Getting started](#getting-started) for how to bring it into a Unity project.

## Requirements

- **Unity** with the **Mixed Reality Toolkit 3 (MRTK3)** packages installed (`MixedReality.Toolkit.Core`, `MixedReality.Toolkit.UX`, `MixedReality.Toolkit.SpatialManipulation`), plus **TextMeshPro**.
- A target device/runtime for MR (e.g. HoloLens 2) or the Unity Editor with MRTK3's in-editor input simulation for testing without a headset.
- An [Eclipse BaSyx](https://www.eclipse.org/basyx/) AAS environment (or compatible AAS server) hosting a `GUI_Submodel` and a `LineParts_Submodel`, reachable over HTTP.
- An MQTT broker plus an OPC UA ↔ MQTT publisher bridge (`UnityOpcUaPublisher` namespace) if you want live sensor data rather than the simulated mode.

## Getting started

1. Create (or open) a Unity project with MRTK3 installed, and copy the contents of `GUI_Scripts/`, `SampleScene.unity`, and the `.fbx` model into its `Assets/` folder.
2. Set up an AAS environment (e.g. BaSyx) and import submodels shaped like `AAS_GUI_Template.json` / `AAS_GUI_Submodel.json` — these two files double as a reference/template for the expected data model. Note the (Base64-encoded) submodel IDs.
3. Edit `config.json` with your environment's details:

   ```json
   {
     "serverBaseUrl": "http://<basyx-host>:8081",
     "GUISubmodelId": "<base64 id of the GUI submodel>",
     "LinePartsSubmodelId": "<base64 id of the LineParts submodel>",
     "mqttBrokerUrl": "mqtt://<broker-host>:1883"
   }
   ```

   In the Editor this is read from `Application.streamingAssetsPath`, so place a copy under `Assets/StreamingAssets/config.json`; on-device builds read it from `Application.persistentDataPath`, so it needs to be deployed there at runtime.
4. Open `SampleScene.unity`, wire up the `canvasDialogPrefab` and part/dialog references on `AASFetcher`, `ModelManager`, and `TogglePartController` to match your scene hierarchy, and press Play (or deploy to your headset).
5. If no live OPC UA/MQTT feed is available yet, enable `SimulatedDataUpdater` on a GameObject in the scene to see fluctuating demo values instead.

## Data model reference

- **`GUI_Submodel`** (`AAS_GUI_Submodel.json`) — one `SubmodelElementCollection` per dialog, each carrying `HeaderText`, `MainText`, `FontSize`, optional `Position`/`Rotation`/`Scale` groups, an optional `NodeId` (for live data binding), and an optional `LinkedPart` reference element (for anchoring to a named line part instead of a fixed position).
- **`LineParts_Submodel`** — a (possibly nested) structure of `SubmodelElementCollection`/`Entity` nodes, each exposing `PosX`/`PosY`/`PosZ` properties that `AASLinePartsLoader` resolves into world positions keyed by `idShort`.
- **`AAS_GUI_Template.json`** — a full Asset Administration Shell referencing both submodels, useful as a starting template when standing up a new AAS environment for this app.

## Notes & limitations

This project originated as a demonstration/prototype for a mixed reality digital twin of a teaching production line, and the code reflects that:

- The simulated-variable creation path in `AASFetcher` is explicitly commented as being for presentation purposes only and would need to be replaced with real OPC UA node discovery in a production setting.
- Server address, credentials, and broker details are read from plain JSON with no transport security — treat `config.json` as environment-specific and do not commit real deployment credentials to version control.
- Some source comments are in Slovak; contributions with English comments/documentation are welcome.

## License

This project is licensed under the [MIT License](LICENSE) — see the `LICENSE` file for the full text.

`GUI_Scripts/SimpleJSON.cs` is a third-party file (© 2012–2022 Markus Göbel / Bunny83) distributed under its own MIT license, included in the file header.
