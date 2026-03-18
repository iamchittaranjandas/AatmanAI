# Aatman AI 🧠

**Aatman AI** is a privacy-first, on-device AI assistant for Android and iOS. All inference runs locally on your mobile processor using lightweight quantized LLM models — no cloud, no servers, no data collection.

Built with **.NET MAUI** and powered by **llama.cpp** for native performance.

---

## 🌟 What is Aatman AI?

Aatman AI is a mobile chat application that brings powerful AI capabilities directly to your phone:

- **100% Private**: All processing happens on your device. Your conversations never leave your phone.
- **Offline-First**: Works without internet after model download. Perfect for travel, privacy, or unreliable networks.
- **Multilingual**: Supports 10+ languages including English, Hindi, Spanish, French, German, Japanese, Chinese, Arabic, Portuguese, and Russian.
- **Customizable**: Use custom system prompts to shape AI behavior (Coder, Tutor, Creative Writer, etc.)
- **Document RAG**: Upload PDFs/text files to your Vault and the AI will reference them in responses.
- **Model Marketplace**: Download and switch between multiple GGUF-quantized models (SmolLM2, Qwen, Gemma, Phi, LLaMA, BitNet).

---

## 📱 Features

### Core Features
- **Chat Interface**: Clean, modern UI with streaming token generation
- **Multi-Model Support**: Download and switch between models on-the-fly
- **Custom Prompts**: Create folders of system prompts for different use cases
- **Language Selector**: Force AI responses in your preferred language
- **Vault (RAG)**: Upload documents and the AI will use them as context
- **Network Audit**: Track all network requests for transparency
- **Storage Manager**: Monitor model downloads and device storage

### Technical Features
- **On-Device Inference**: llama.cpp native binaries (ARM64 optimized)
- **GGUF Quantization**: Q4/Q5/Q8 models for mobile efficiency
- **SQLite Persistence**: Local database for chats, models, settings
- **Download Manager**: Resumable HTTP downloads with progress tracking
- **Device Benchmarking**: Automatic RAM/storage/GPU capability detection
- **Encrypted Storage**: Local data stored in encrypted form

---

## 🏗️ Architecture

### Technology Stack

| Layer | Technology |
|-------|-----------|
| **Framework** | .NET 9 MAUI (Multi-platform App UI) |
| **UI Pattern** | MVVM (Model-View-ViewModel) |
| **Navigation** | Shell-based routing |
| **Database** | SQLite (sqlite-net-pcl) |
| **Inference** | llama.cpp (P/Invoke to native .so libs) |
| **HTTP** | HttpClient with resumable downloads |
| **DI Container** | Microsoft.Extensions.DependencyInjection |

### Project Structure

```
AatmanAI/
├── src/AatmanAI/
│   ├── Core/                    # Constants, enums, utilities
│   ├── Data/
│   │   ├── Database/            # AppDatabase, migrations
│   │   └── Models/              # Entity models (Conversation, ChatMessage, etc.)
│   ├── Services/
│   │   ├── Interfaces/          # Service contracts
│   │   ├── Native/              # P/Invoke wrappers for llama.cpp
│   │   ├── ChatService.cs       # Conversation & message management
│   │   ├── InferenceService.cs  # LLM inference engine
│   │   ├── ModelService.cs      # Model manifest & lifecycle
│   │   ├── DownloadService.cs   # HTTP download manager
│   │   ├── DeviceService.cs     # Device capability detection
│   │   ├── VaultService.cs      # Document RAG (keyword search)
│   │   ├── CustomPromptService.cs
│   │   ├── NetworkAuditService.cs
│   │   └── [Auth/Voice/Translation]Service.cs (stubs)
│   ├── ViewModels/              # MVVM ViewModels
│   ├── Views/                   # XAML pages
│   ├── Platforms/
│   │   └── Android/libs/arm64-v8a/  # llama.cpp .so binaries
│   ├── Resources/
│   │   └── Raw/model_manifest.json  # Model catalog
│   ├── AppShell.xaml            # Shell navigation routes
│   └── MauiProgram.cs           # DI registration
└── README.md
```

### Service Layer

| Service | Responsibility |
|---------|---------------|
| **ChatService** | Manages conversations, messages, system prompt building (RAG + user profile + custom prompts) |
| **InferenceService** | Loads GGUF models, generates tokens via llama.cpp, manages context window |
| **ModelService** | Fetches model manifest, tracks downloaded models, sets active model |
| **DownloadService** | HTTP downloads with progress tracking, speed/ETA calculation, cancellation |
| **DeviceService** | Benchmarks RAM/storage/GPU, checks model compatibility |
| **VaultService** | Indexes uploaded documents, performs keyword search for RAG |
| **CustomPromptService** | CRUD for prompt folders and prompts |
| **NetworkAuditService** | Logs all HTTP requests for transparency |

---

## 🔄 Application Lifecycle

### 1. App Startup Flow

```
┌─────────────┐
│ App Launch  │
└──────┬──────┘
       │
       ▼
┌─────────────────────┐
│ MauiProgram.cs      │  ← Register all services & ViewModels in DI container
└──────┬──────────────┘
       │
       ▼
┌─────────────────────┐
│ App.xaml.cs         │  ← Initialize AppDatabase, set MainPage = AppShell
└──────┬──────────────┘
       │
       ▼
┌─────────────────────┐
│ AppShell.xaml       │  ← Navigate to initial route: "splash"
└──────┬──────────────┘
       │
       ▼
┌─────────────────────┐
│ SplashPage          │  ← Show logo, check first launch flag
└──────┬──────────────┘
       │
       ├─ First Launch? ──YES──▶ Navigate to "firstlaunch" (onboarding)
       │
       └─ Returning User? ──NO──▶ Navigate to "main/home" (conversation list)
```

### 2. First Launch Flow (Onboarding)

```
┌──────────────────┐
│ FirstLaunchPage  │  ← Welcome screen
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ ProfileSetupPage │  ← User enters name, life goal, interests
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ ProfileLicensePage│ ← Show Apache 2.0 license, privacy policy
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ MarketplacePage  │  ← Download first model (required to use app)
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│ HomePage         │  ← Main conversation list
└──────────────────┘
```

### 3. Chat Lifecycle

```
┌──────────────┐
│ HomePage     │  ← User taps "New Chat" or selects existing conversation
└──────┬───────┘
       │
       ▼
┌──────────────────────────────────┐
│ ChatViewModel.LoadConversationAsync │
└──────┬───────────────────────────┘
       │
       ├─ Load conversation metadata
       ├─ Load all messages from DB
       └─ Populate Messages collection
       │
       ▼
┌──────────────┐
│ ChatPage     │  ← Display messages, input box, header pills
└──────┬───────┘
       │
       │ User types message, taps Send
       ▼
┌──────────────────────────────────┐
│ ChatViewModel.SendAsync          │
└──────┬───────────────────────────┘
       │
       ├─ Save user message to DB
       ├─ Add placeholder assistant message to UI
       │
       ▼
┌──────────────────────────────────┐
│ ChatService.SendMessageAsync     │
└──────┬───────────────────────────┘
       │
       ├─ Build system prompt:
       │   ├─ Custom prompt (if selected)
       │   ├─ Language instruction (if non-English)
       │   ├─ Vault RAG context (search user's documents)
       │   └─ User profile (name, goals, interests)
       │
       ├─ Load conversation history
       │
       ▼
┌──────────────────────────────────┐
│ InferenceService.GenerateAsync   │
└──────┬───────────────────────────┘
       │
       ├─ Check if model is loaded
       │   └─ If not: LoadModelAsync (llama.cpp)
       │
       ├─ Trim history to fit context window
       ├─ Call llama_generate_stream (P/Invoke)
       │
       ▼
┌──────────────────────────────────┐
│ Token Streaming Loop             │
└──────┬───────────────────────────┘
       │
       │ For each token:
       ├─ Yield token to ChatService
       ├─ ChatService yields to ChatViewModel
       ├─ ChatViewModel appends to assistant message
       └─ UI updates in real-time
       │
       ▼
┌──────────────────────────────────┐
│ Generation Complete              │
└──────┬───────────────────────────┘
       │
       ├─ Save assistant message to DB
       ├─ Update conversation metadata (message count, timestamp)
       └─ IsGenerating = false
```

### 4. Model Download Lifecycle

```
┌──────────────────┐
│ MarketplacePage  │  ← User taps "Download" on a model card
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ MarketplaceViewModel.DownloadModelAsync │
└────────┬─────────────────────────────┘
         │
         ├─ Check device compatibility (RAM, storage, GPU)
         │   └─ If incompatible: Show alert, abort
         │
         ▼
┌──────────────────────────────────────┐
│ DownloadService.StartDownloadAsync   │
└────────┬─────────────────────────────┘
         │
         ├─ Create DownloadTask observable
         ├─ Start HTTP GET with Range header (resumable)
         │
         ▼
┌──────────────────────────────────────┐
│ Download Progress Loop               │
└────────┬─────────────────────────────┘
         │
         │ For each chunk:
         ├─ Write to file stream
         ├─ Update DownloadTask.Progress (0.0 - 1.0)
         ├─ Calculate speed (MB/s) and ETA
         ├─ Fire DownloadProgressChanged event
         │
         ▼
┌──────────────────────────────────────┐
│ MarketplaceViewModel subscribes      │
└────────┬─────────────────────────────┘
         │
         ├─ Find matching MarketplaceModelItem
         ├─ Update DownloadProgress, SpeedText, EtaText
         └─ UI shows progress bar in real-time
         │
         ▼
┌──────────────────────────────────────┐
│ Download Complete                    │
└────────┬─────────────────────────────┘
         │
         ├─ Fire DownloadCompleted event
         ├─ ModelService saves to DB (DownloadedModel table)
         ├─ MarketplaceModelItem.IsDownloaded = true
         └─ UI shows "Use This Model" button
```

### 5. Model Switching Lifecycle

```
┌──────────────────┐
│ ChatPage         │  ← User taps model pill (green dot)
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ ChatViewModel.SelectModelAsync       │
└────────┬─────────────────────────────┘
         │
         ├─ Show action sheet with downloaded models
         ├─ User selects a model
         │
         ▼
┌──────────────────────────────────────┐
│ ModelService.SetActiveModelAsync     │
└────────┬─────────────────────────────┘
         │
         ├─ Update DB: mark new model as active
         │
         ▼
┌──────────────────────────────────────┐
│ InferenceService.UnloadModelAsync    │
└────────┬─────────────────────────────┘
         │
         ├─ Call llama_free (P/Invoke)
         ├─ Release memory
         ├─ State = Idle
         │
         ▼
┌──────────────────────────────────────┐
│ InferenceService.LoadModelAsync      │
└────────┬─────────────────────────────┘
         │
         ├─ State = Loading
         ├─ Call llama_load_model (P/Invoke)
         ├─ Allocate context buffer
         ├─ State = Loaded
         │
         ▼
┌──────────────────────────────────────┐
│ ChatViewModel updates UI             │
└────────┬─────────────────────────────┘
         │
         ├─ SelectedModelName = "0.5B" (or model size)
         ├─ ModelStatus = "0.5B"
         └─ Pill label updates
```

### 6. Vault (RAG) Lifecycle

```
┌──────────────────┐
│ VaultPage        │  ← User taps "Add Document"
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ VaultViewModel.AddDocumentAsync      │
└────────┬─────────────────────────────┘
         │
         ├─ File picker (PDF, TXT, MD)
         │
         ▼
┌──────────────────────────────────────┐
│ VaultService.AddDocumentAsync        │
└────────┬─────────────────────────────┘
         │
         ├─ Extract text (PDF → Markdig, TXT → raw)
         ├─ Split into chunks (512 chars, 50 char overlap)
         ├─ Save VaultDocument + VaultChunk to DB
         ├─ Build keyword index (lowercase, split on whitespace)
         │
         ▼
┌──────────────────────────────────────┐
│ Document Indexed                     │
└──────────────────────────────────────┘

Later, during chat:

┌──────────────────────────────────────┐
│ ChatService.BuildSystemPromptAsync   │
└────────┬─────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────┐
│ VaultService.SearchAsync(userMessage)│
└────────┬─────────────────────────────┘
         │
         ├─ Tokenize query (lowercase, split)
         ├─ Search chunks by keyword overlap
         ├─ Score by match count
         ├─ Return top 5 chunks
         │
         ▼
┌──────────────────────────────────────┐
│ VaultService.BuildContext(results)   │
└────────┬─────────────────────────────┘
         │
         ├─ Format as: "RELEVANT DOCUMENTS:\n[chunk1]\n[chunk2]..."
         │
         ▼
┌──────────────────────────────────────┐
│ Inject into system prompt            │
└────────┬─────────────────────────────┘
         │
         └─ AI uses vault context in response
```

---

## 🚀 Getting Started

### Prerequisites

- **.NET 9 SDK** (or later)
- **Visual Studio 2022** or **JetBrains Rider** (with MAUI workload)
- **Android SDK** (API 24+) or **Xcode** (iOS 15.0+)
- **Physical device** recommended (emulators may not support native inference)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/yourusername/AatmanAI.git
cd AatmanAI

# Restore dependencies
dotnet restore src/AatmanAI/AatmanAI.csproj

# Build for Android
dotnet build src/AatmanAI/AatmanAI.csproj -f net9.0-android -c Debug

# Deploy to connected Android device
dotnet build src/AatmanAI/AatmanAI.csproj -f net9.0-android -c Debug -t:Run -p:AdbTarget="-s <DEVICE_ID>"

# Build for iOS (requires macOS)
dotnet build src/AatmanAI/AatmanAI.csproj -f net9.0-ios -c Debug
```

### First Launch

1. **Onboarding**: Enter your name, life goal, and interests (optional but improves personalization)
2. **Download a Model**: Visit the Marketplace and download a small model (e.g., SmolLM2-135M-Instruct)
3. **Start Chatting**: Tap "New Chat" and send your first message

---

## 📦 Model Manifest

Models are defined in `Resources/Raw/model_manifest.json` and fetched from the remote manifest at:
```
https://raw.githubusercontent.com/aatmanai/models/main/manifest.json
```

Each model entry includes:
- **id**: Unique identifier
- **name**: Display name
- **parameters**: Size (e.g., "0.5B", "1B")
- **quantization**: GGUF quant type (Q4_K_M, Q5_K_S, etc.)
- **size_mb**: File size in MB
- **download_url**: Direct GGUF download link
- **tier**: "free" or "premium"
- **min_ram_gb**: Minimum RAM required
- **context_length**: Max tokens (e.g., 2048, 4096)
- **capabilities**: ["chat", "code", "multilingual", etc.]

---

## 🔒 Privacy & Security

- **No Cloud**: All inference runs on-device. No API calls to external servers.
- **No Telemetry**: No analytics, no tracking, no data collection.
- **Encrypted Storage**: SQLite database is encrypted at rest.
- **Network Audit**: Every HTTP request is logged and visible in Settings → Network Audit.
- **Open Source**: Apache 2.0 license — audit the code yourself.

---

## 🛠️ Development

### Adding a New Service

1. Define interface in `Services/Interfaces/IYourService.cs`
2. Implement in `Services/YourService.cs`
3. Register in `MauiProgram.cs`:
   ```csharp
   builder.Services.AddSingleton<IYourService, YourService>();
   ```

### Adding a New Page

1. Create XAML in `Views/YourPage.xaml`
2. Create ViewModel in `ViewModels/YourViewModel.cs`
3. Register both in `MauiProgram.cs`
4. Add route in `AppShell.xaml` or navigate via:
   ```csharp
   await Shell.Current.GoToAsync("//yourroute");
   ```

### Database Migrations

Modify `AppDatabase.cs` and increment version:
```csharp
private const int DatabaseVersion = 2; // Increment this

protected override async Task MigrateAsync(int oldVersion, int newVersion)
{
    if (oldVersion < 2)
    {
        // Add migration SQL here
    }
}
```

---

## 📄 License

**Apache License 2.0**

Copyright © 2024 Aatman AI (sritcreations.com)

See [LICENSE](LICENSE) for full text.

---

## 🙏 Acknowledgments

- **llama.cpp**: Georgi Gerganov and contributors
- **GGUF Models**: Hugging Face community (SmolLM2, Qwen, Gemma, Phi, etc.)
- **.NET MAUI**: Microsoft
- **CommunityToolkit.Mvvm**: .NET Foundation

---

## 📧 Contact

- **Website**: [sritcreations.com](https://sritcreations.com)
- **Origin**: Made in India 🇮🇳 for the world 🌍

---

**Aatman AI** — Your AI, Your Device, Your Privacy.
