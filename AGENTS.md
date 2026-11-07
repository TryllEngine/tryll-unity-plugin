# Tryll Engine (Unity) — context for AI coding agents

**Tryll plugin version:** 0.10.1

You are looking at the Tryll Unity package. This file is a **map, not a manual** — it gives
you the mental model and the gotchas, then points at the full docs. Fetch a linked page
whenever you need detail; do not guess an API from its name.

- Full docs index for agents: **<https://docs.tryllengine.com/v0.10/llms.txt>**
- Every page below is also available as clean Markdown by swapping `.html` → `.md`.
- **If this file and the docs disagree, the docs win.** This file ships frozen inside the
  package; the site tracks the current release.

---

## What Tryll is

Tryll runs **small language models on the player's own machine**. A C++ TCP server
(`tryll_server.exe`, bundled in this package under `.Server/Default/`) hosts the models;
your C# code drives it over a binary FlatBuffers protocol. No cloud API, no API keys, no
per-token cost, works offline.

The practical consequences: models are **files that must exist on disk** before use, VRAM
and RAM are **your budget to manage**, and the models are **small** (1–8 B) — they need
tighter prompting and guardrails than a frontier cloud model.

## The mental model

A Tryll **agent is not a prompt.** It is a **workflow graph** of typed nodes, authored
once and re-walked on every user turn.

- A **graph** = a set of named **nodes** + a **start node**.
- Each node has **typed params** carrying both its config *and* its exit wiring. Exits are
  plain string fields on the params: `default_exit`, `then_exit`, `triggered_exit`, …
  An empty string means **END**. There is no separate routes/edges array.
- Nodes pass values through **slots** — a per-turn blackboard. A producing node writes a
  slot; `input` selects which slot a downstream node reads. `user_message` is the reserved
  implicit input slot. Mustache templates address slots as `{{slot.<name>}}`.
- `send` decides whether a slot's text goes to the client (`None` / `Whole` / `Streamed`);
  `history_role` decides whether it replays as dialogue history on later turns.

Read these two pages before writing any graph:

- <https://docs.tryllengine.com/v0.10/concepts/workflows-graphs-nodes.md>
- <https://docs.tryllengine.com/v0.10/concepts/slots-and-inter-node-value-passing.md>

## Things that differ from cloud LLM SDKs

These are the mistakes an agent makes by pattern-matching on OpenAI/Anthropic SDKs.

1. **Models are never auto-downloaded.** Acquire a model first — the editor **Model
   Manager** window, or `TryllClient.Instance.RequestDownloadModel(name)` at runtime.
   Creating an agent whose graph references an absent model fails fast with
   `GraphCompilationFailed`. This applies to the voice profile's TTS model too. In the editor (Client Dev),
   downloads land under the project **TryllStorage/models/** folder (outside `Assets/`),
   not inside the package.
2. **A session is mandatory and one-shot.** `Connect()` only establishes the connection;
   a session must then be created (`CreateSession(...)`) before anything else, or requests
   are rejected with `SessionNotReady`. A second attempt fails with `SessionAlreadyExists`.
   `StartSession()` does both steps (connect, then `CreateSession` from the settings
   asset). **Auto Start Session** on a scene `TryllClient` (off by default) calls it from
   `Awake`. **Auto Launch Server** only starts the server process; it does not connect.
   Unity defaults to a manual session; Unreal differs (its **Auto Create Session** is on
   by default), so do not port this step to Unreal code as-is.
3. **There is no `stream: bool`.** Use `send: SendAnswer` and `history_role` (see above).
4. **Never hand-translate node type numbers.** The client `TryllNodeType` enum and the
   wire `NodeParams` union discriminator are **two independent numbering schemes** and they
   diverge for newer node types. Always use the named enum value or the builder method.
5. **Some params are structural** — fixed for the agent's lifetime. Changing one at runtime
   is rejected with `ParamNotMutable`. Rebuild the agent instead.
6. **Storage is usually a relative file path**, not a wire call. Node params like
   `string_storage` / `embedded_string_storage` accept a path resolved against the process
   storage root (`TryllRuntimeSettings.StorageDataFolder`, default `TryllStorage/` outside
   `Assets/`; players use `StreamingAssets/Tryll/`). Prefer this over creating storages
   over the wire. There is no per-session storage override on CreateSession.
7. **Turns are cancellable and pausable, not fire-and-forget.** A turn can pause between
   nodes (tool calls, `Pause` node) and **something must resume it**. A pause holds the
   turn open: the agent stays busy and every later `SendMessage` fails with `AgentBusy`
   (3004) until `Resume`, `Cancel`, or the pause's timeout ends it.
   So if a workflow contains a `Pause` node or a pausing `ToolCall` disposition, the game
   must either subscribe to `TryllClient.Paused` (or the component's `Paused`) and call
   `Resume`, or `RegisterTool` a handler for every tool in the batch (which auto-resumes).
   A pause can time out. `Pause.pause_timeout_ms` `0` (the default) waits forever. With a
   timeout, `timeout_exit` continues the graph — use it so the character says *something*
   instead of going quiet; an empty `timeout_exit` ends the turn with `PauseTimedOut`.
   `ToolCall.pause_timeout_ms` `0` inherits the server's
   `workflow.tool_call_pause_timeout_ms`, which is **0 (no deadline)** by default and
   in the released server. A source checkout's `data/server-config.json` sets `2000`
   so contributors notice a missing `Resume`. There is no timeout exit on `ToolCall`: an unresumed tool-call
   pause ends the turn with `PauseTimedOut`. `TryllAgent.PausedNode` reports the parked
   node, and the package logs a warning when a pause arrives that nothing can resume.

## Unity API map

| You want to… | Use |
|---|---|
| Connect / own the session | `TryllClient` — singleton `MonoBehaviour`, `TryllClient.Instance` |
| Run an agent on a GameObject | `TryllAgentComponent` |
| Author a graph as an asset | `TryllWorkflowAsset` (**Assets → Create → Tryll → Workflow Asset**) |
| Build a graph in code | `TryllGraphBuilder` |
| Configure host/port/auto-launch/llama.cpp GPU API/storage root | `TryllRuntimeSettings` (**Project Settings → Tryll Client**) |
| Resolve Localized prompt Text IDs | `TryllRuntimeSettings.TextProvider` / `TryllClient.TextProvider` (`ITryllTextProvider`); outcome of a language change per agent: `TryllClient.LocalizedTextApplied` / `TryllClient.LocalizedTextFailed` |
| Give an agent a voice (required when a node speaks) | `TryllVoiceProfile` asset (**Assets → Create → Tryll → Voice Profile**) on `TryllAgentComponent.Voice`; runtime `SetVoiceAsync` / `SetDeliveryModeAsync` |
| Play streamed TTS audio | `TryllSpeaker` |
| Move a face's mouth with the TTS voice | `TryllLipSync` (next to `TryllSpeaker`; the voice profile needs `EmitVisemes`) |
| Capture mic → speech-to-text | `TryllVoiceInputComponent` |
| Read/write per-agent Variables | `agent.Variables` on `TryllAgent` |

Key members — see the reference pages for exact signatures:

- `TryllClient.Instance` — `StartSession()`, `Connect()`, `CreateSession()`,
  `Disconnect()`, `IsSessionReady`,
  `RequestDownloadModel()`, `RequestLoadModel()`, `RequestListModelsAsync()`,
  `RequestGetMemoryConsumptionAsync()`;
  events `ConnectionChanged`, `CreateSessionComplete`, `DownloadProgress`, `Error`.
- `TryllAgentComponent` — inspector fields `WorkflowAsset`,
  `AutoCreateOnConnect`, `EnableDiagnostics`, `MaintainDialogueHistory`; methods
  `CreateAgent()`, `SendMessage(string)`, `Cancel()`, `Resume()`, `ChangeParams()`,
  `RegisterTool()`, `AppendInteractions()`, `DestroyAgent()`; `UnityEvent`s
  `OnAnswerText`, `OnTurnComplete`, `OnAgentCreated`, `OnError`, `OnParamChanged`,
  `OnResumed`; C# event `Paused`.
- Node params are per-node classes: `TryllGenerateParams`, `TryllRetrieveParams`,
  `TryllBranchParams`, … all deriving from `TryllNodeParamsBase`.
- Builder methods are `Add<NodeType>` — e.g. `AddGenerate(name, params)`,
  `AddRegexGuardrail(name, params)` — plus `SetStartNode()`, `SetDefaultModelName()`,
  `Build()`. **Note:** Unity uses `AddGenerate`; the Unreal plugin uses
  `AddGenerateNode`. Do not copy names across engines.

Shape of a minimal integration:

1. Set **Project Settings → Tryll Client** (enable **Auto Launch Server** for local
   dev; **Llama.cpp backend** defaults to Vulkan — set Auto to prefer CUDA on NVIDIA).
   Keep **Storage Data Folder** at `TryllStorage` (outside `Assets/`). Game code can
   call `TryllRuntimeSettings.ResolveStorageRootForRuntime()` for absolute paths.
2. Add `TryllAgentComponent` to a GameObject; assign a `TryllWorkflowAsset`; enable
   **Auto Create On Connect**.
3. Subscribe `OnAnswerText` and `OnTurnComplete`.
4. `TryllClient.Instance.StartSession()` on startup (connects and creates the session),
   or tick **Auto Start Session** on a scene `TryllClient`.
5. `SendMessage("…")` to drive a turn.

In the Editor, the server starts only after the person accepts the Tryll Runtime Licence in
**Window → Tryll → Welcome**. Until then, a start fails with "Accept the Tryll Runtime Licence in
the Tryll Welcome window first." Batch mode and players are not gated.

Full walkthrough: <https://docs.tryllengine.com/v0.10/getting-started/first-inference-unity.md>

## Where to look for what

| Question | Page |
|---|---|
| What does this term mean? | [glossary](https://docs.tryllengine.com/v0.10/reference/glossary.md) |
| Which node do I need? | [nodes index](https://docs.tryllengine.com/v0.10/reference/nodes/index.md) |
| What params does node X take? | `reference/nodes/<node>.md` |
| Every field on agent creation | [agent-parameters](https://docs.tryllengine.com/v0.10/reference/agent-parameters.md) |
| What does this error code mean? | [error-codes](https://docs.tryllengine.com/v0.10/reference/error-codes.md) |
| Simplest working graph | [build a chat agent](https://docs.tryllengine.com/v0.10/how-to/build-chat-agent-with-graph.md) |
| Answer from my own data (RAG) | [create a RAG assistant](https://docs.tryllengine.com/v0.10/how-to/create-simple-rag-assistant.md) |
| Give an NPC a voice | [add voice output](https://docs.tryllengine.com/v0.10/how-to/add-voice-to-an-agent.md) |
| Lip-sync a speaking character | [add lip sync](https://docs.tryllengine.com/v0.10/how-to/add-lip-sync-to-a-character.md) |
| Let the model call game code | [tool calls](https://docs.tryllengine.com/v0.10/how-to/define-and-handle-tool-calls.md) |
| Test tools in Chat / Dialog Lab | [test tool calls in the editor](https://docs.tryllengine.com/v0.10/how-to/test-tool-calls-in-editor.md) |
| Which model for tool calls | [compare tool-call models](https://docs.tryllengine.com/v0.10/how-to/compare-tool-call-models.md) |
| Shape a tool schema / prompt | [design a tool-call prompt](https://docs.tryllengine.com/v0.10/how-to/design-a-tool-call-prompt.md) |
| Feed game state into prompts | [drive prompts from game state](https://docs.tryllengine.com/v0.10/how-to/drive-prompts-and-retrieval-from-game-state.md) |
| Localize a prompt as a Text ID | [localize prompts](https://docs.tryllengine.com/v0.10/how-to/localize-prompts.md) |
| Live server RAM / VRAM | [inspect server memory](https://docs.tryllengine.com/v0.10/how-to/inspect-server-memory.md) |
| Force JSON / structured output | [constrain with a grammar](https://docs.tryllengine.com/v0.10/how-to/constrain-output-with-a-grammar.md) |
| RAM / VRAM cost | [estimate memory footprint](https://docs.tryllengine.com/v0.10/how-to/estimate-memory-footprint.md) |
| Ship storage files in a build | [ship storage folders](https://docs.tryllengine.com/v0.10/how-to/ship-storage-folders-for-builds.md) |
| Unity API details | [Unity client API](https://docs.tryllengine.com/v0.10/reference/client-api/unity/index.md) |

## Do not

- **Do not edit anything under `Runtime/Generated/`.** It is generated from the wire
  schema and will be overwritten. Node params and builder methods live there.
- **Do not edit `.Server/`.** That is the bundled server binary and its data.
- Do not put the storage root under `Assets/` — use `TryllStorage/` at the project
  root. Client Dev model downloads and derived files write there; Unity must not
  track them. Player builds stage that folder to `StreamingAssets/Tryll/` and
  launch the staged Client Prod server with `--mode client-prod`.
- Do not invent node types, param names, or exit names. Check the node reference page.
- Do not assume a model name is valid. List the catalog or check the Model Manager.
