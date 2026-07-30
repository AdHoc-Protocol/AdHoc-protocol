# AdHoc: Multi-Language Binary Protocol Code Generator

Performance should never be an afterthought.

![image](https://user-images.githubusercontent.com/29354319/204679188-d5b0bdc7-4e47-4f32-87bb-2bfaf9d09d78.png)

When system components need to communicate efficiently across languages and platforms, manually coding serialization and deserialization becomes a
real burden - slow to write, prone to subtle bugs, and increasingly painful as the system grows, new languages are added, or data structures change.

**Domain-Specific Languages (DSLs) for protocol description** solve this by letting you declare your data structures, message types, and communication
protocols once, then automatically generate consistent, high-performance implementation code for any supported language. The benefits are concrete:

* **Reduced development time:** No manual serialization logic.
* **Fewer bugs:** Generated code is consistent across platforms, eliminating a whole class of human error.
* **Uniform implementation:** Data handling and protocol adherence are identical everywhere.
* **Easier maintenance:** Protocol changes go in one place and propagate automatically.

Many established frameworks use this approach:

- [**Swagger/OpenAPI**](https://swagger.io/docs/specification/data-models/): RESTful API definition with documentation and code generation.
- [**Protocol Buffers**](https://developers.google.com/protocol-buffers/docs/overview): Compact binary serialization with schema evolution.
- [**Cap'n Proto**](https://capnproto.org/language.html): High-performance, zero-copy serialization with RPC.
- [**FlatBuffers**](http://google.github.io/flatbuffers/flatbuffers_guide_writing_schema.html): Memory-efficient zero-copy serialization.
- [**ZCM**](https://github.com/ZeroCM/zcm/blob/master/docs/tutorial.md): Real-time, low-latency messaging for structured data.
- [**MAVLink**](https://github.com/mavlink/mavlink): Lightweight messaging for drones and robotics.
- [**Thrift**](https://thrift.apache.org/docs/idl): Cross-language serialization and RPC.
- [**Apache Avro**](https://avro.apache.org/docs/1.8.2/idl.html): Schema-based serialization for big data.

After evaluating these options, particularly for scenarios demanding maximum binary efficiency and application-specific protocol control, we developed
**AdHoc Protocol** - a next-generation code generator built for those demands.

AdHoc currently supports **C#, Java, and TypeScript**, with C++, Rust, and Go planned. It handles translation between binary data streams and
structured objects ("packs"), making high-performance cross-language communication straightforward.

## Why Choose AdHoc?

AdHoc is built for **data-oriented applications** that need high performance and efficient handling of structured binary data, whether for network
communication or custom storage formats. Unlike frameworks that require buffering entire messages in memory, **AdHoc uses a streaming architecture.**
Data is processed in small, reusable chunks, dramatically reducing memory usage and enabling efficient handling of messages of any size - including
messages larger than available RAM.

### 1. Best Fit: Data-Intensive Applications

AdHoc is well-suited for systems where data volume, speed, and efficiency matter:

- **Financial Trading:** Real-time, high-frequency market data with minimal latency.
- **CRM:** Large datasets of customer interactions and transactions.
- **ERP:** High-volume, real-time data updates in logistics, inventory, and operations.
- **Scientific & Industrial Data Acquisition:** Massive volumes of binary sensor data for factory automation and engineering analysis.
- **Game Servers:** Low-latency, real-time communication for multiplayer games.
- **IoT Systems:** High-throughput sensor data streams.
- **Real-Time Analytics:** Streaming data where processing speed is critical.
- **Streaming Media:** High-quality audio/video delivery with low latency.
- **Telecommunications:** High-volume call routing, message delivery, and network state.
- **Autonomous Vehicles:** Rapid processing of sensor and communication data.
- **Microservices:** Efficient data exchange in distributed systems.
- **Custom File Storage:** Application-specific binary formats optimized for retrieval and storage.

### 2. Performance Benefits

- **No whole-message buffer - in either direction.** AdHoc's defining property is that the runtime **never** allocates a buffer sized to the entire
  pack. Serialization is pull-based: bytes flow through one reusable socket buffer (user-chosen size, minimum 256 bytes - pick larger to amortize
  kernel syscalls; the Monitoring server uses 1024), and the producer holds only its own small state machine. Parsing is the inverse - bytes are
  consumed slot-by-slot directly off the wire.
  
  This is the opposite of how most binary protocols work. Protocol Buffers, FlatBuffers, Thrift, MessagePack and friends serialize the whole pack
  into one contiguous byte array before any of it goes on the wire (`byte[] data = pack.toByteArray(); socket.write(data);`) and parse the inverse
  way - read the full message into a single buffer before any field is accessible. For a 64-byte status update this is fine. For a 1 GB telemetry
  blob, an arbitrarily-long live encoder feed, or a video stream relayed through a proxy, it's a non-starter - the sender would need a gigabyte of
  RAM just to *prepare* the message before the first byte hits the socket.
  
  AdHoc has no such moment. A pack of any size - a `Stream` field of unbounded size - costs the same constant socket-buffer size of resident
  memory on each side, independent of the pack's logical length.
- **Lower GC pressure:** By avoiding large single-object allocations and reusing small buffers, AdHoc reduces garbage collector workload, leading to
  lower latency, fewer pauses, and more predictable throughput.
- **Efficient serialization/deserialization:** The streaming model transforms data on-the-fly, reducing end-to-end latency.

### 3. When to Consider Other Solutions

- **Text-based or content-oriented applications:** For blogs, CMS, or document storage where human-readable formats are acceptable, JSON or XML are
  simpler and sufficient.
- **Simple or low-performance use cases:** If data volume, speed, and resource efficiency are not priorities, standard formats are easier to implement
  and maintain.

## AdHoc Protocol Key Concepts

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a15016a6-ac05-4d66-8798-4a7188bf24c5)

The **AdHoc** generator provides:

- C# as the protocol description language - familiar and well-tooled.
- Entities that can import (inherit) or subtract (remove) properties of others.
- Projects composable from other projects, or able to selectively import specific components such as **connections**, constants, or individual packs.
- **Connections** constructable from other connections or their components (states, branches).
- Packs that can import or subtract individual fields or all fields of other packs.
- A [`custom code injection point`](#injection-points) for safely integrating custom code with generated code.
- Built-in visualization through the **AdHoc Observer**, which renders interactive diagrams of network topology, pack field layouts, and data flow
  state machines.
- Bitfield support.
- Nullable primitive data types.
- Automatic `long` primitive representation for packs whose field data fits within 8 bytes, reducing GC overhead.
- Support for strings, maps, sets, and arrays.
- Multidimensional arrays with constant, fixed, or variable dimensions.
- Nested packs and enums.
- Standard and flags-style enums.
- Fields typed as enums or pack references.
- Constants at both host and packet levels.
- Circular reference handling and multiple inheritance. Reused entities can be modified for new projects.
- Compression via [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding.
- Fully functional generated code ready for network infrastructure.
- **Built-in streaming parser:** Processes all incoming data in a single reusable socket buffer (user-chosen size, minimum 256 bytes). Buffer
  allocation for the entire object is never required.
- **Inventory-First Dashboard:** A centralized, top-of-file inventory of all packets
  with [reactive ID management](#c-id-management-system-managed--reactive), user-driven semantic tagging, and tag-based routing.

The **AdHoc Code Generator** is a [**SaaS**](https://en.wikipedia.org/wiki/Software_as_a_service) platform providing cloud-based code generation.

First, you'll need a personal [UUID](#uuid). A UUID - rather than a login and password - lets you automate code generation and embed the **AdHocAgent
** utility into your delivery pipeline.

To get started:

1. Install .NET.
2. Install a **C# IDE** such as **[Intellij Rider](https://www.jetbrains.com/rider/)**, **[Visual Studio Code](https://code.visualstudio.com/)**, or *
   *[Visual Studio](https://visualstudio.microsoft.com/vs/community/)**.

---

1. Install [7-Zip](https://www.7-zip.org/download.html) for PPMd compression of source files. Download [**version 24.07 or higher
   **](https://youtu.be/i5L9xEk_adw) for your platform:
	
	- **[Windows](https://www.7-zip.org/a/7zr.exe)**  
	  Add `C:\Program Files\7-Zip` to the system `PATH` and verify `7z` works in the console.
	
	- **[Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)**
	  ```shell
	  apk add p7zip
	  ```
	
	- **[macOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)**
	  ```
	  brew install p7zip
	  ```

2. Download the [AdHoc protocol metadata attributes Meta.cs file](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/master/src/Meta.cs), or add a
   dependency on `AdHocAgent.dll` to your protocol project.

![image](https://github.com/user-attachments/assets/76298dca-1f8c-4b88-855b-080ead6ad0d7)

1. Add a reference to `Meta` in your AdHoc protocol description project.  
   ![image](https://github.com/user-attachments/assets/c91a05fe-3eff-4106-880f-e17f0e6b12de)
2. Compose your protocol description project.
3. Use the **[AdHocAgent](https://github.com/cheblin/AdHocAgent)** utility to upload your project to the server and retrieve the generated code.

# AdHocAgent Utility

AdHocAgent is a command-line utility that handles:

1. Uploading your task
2. Downloading generated results
3. Deploying your project
4. Visualizing your project structure as a diagram
5. Uploading `.proto` files to convert to AdHoc protocol description format
6. Storing and updating your user `UUID`

The first argument is the path to the task file. The file extension determines the task type.

---

## `.cs`

Upload the `protocol description file` to generate source code.
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/7d5181a3-3642-4027-9c3d-aed3ad4b1f5d)

 </details>

## `.cs?`

Launches the **AdHoc Observer**, a web-based tool for visualizing, analyzing, and documenting your protocol definitions. It connects via WebSocket to
receive live protocol data and renders it as a series of interconnected diagrams.

```cmd
    AdHocAgent.exe MyProtocol.cs?
```

The Observer lets you:

* **Visualize high-level architecture:** See all hosts, the packs they handle, and the **connections** linking them in a clear, interactive graph.
* **Drill into data flow logic:** Right-click a **connection** to open a detailed pop-up view of its state machine, including all states and branching
  logic.
* **Inspect data structures:** Left-click a pack to view its fields, data types, and nested structures.
* **Annotate and document:** Double-click the background to create, edit, and save rich-text notes ("stickers") directly on the diagrams.
* **Navigate:** Use a searchable, collapsible tree view in the sidebar to find and focus on any host, pack, or **connection**.
* **Persist your workspace:** All layout customizations (node positions, pan, zoom) and annotations are automatically saved.

> **[See the full Observer User Guide](./Observer.md) for a detailed explanation of all features.**

![image](https://user-images.githubusercontent.com/29354319/232010215-ea6f4b1e-2251-4c3a-956d-017d222ab1e3.png)

![image](https://github.com/user-attachments/assets/565a76c2-58f3-4570-9ca8-c6bad41f4f43)

> [!NOTE]    
> To enable navigation from the Observer to your source code, specify the path to your local C# IDE in the `AdHocAgent.toml` configuration file.

### Saving Your Workspace

The Observer automatically saves your workspace, including diagram layouts and annotations, into a dedicated folder.

* **Location:** The current working folder of AdHocAgent.
* **Manual save:** Open the sidebar and select **"Save Diagram"**.
* **Recovery:** If you accidentally close the browser without saving, the Observer creates a `current_working_folder/unsaved` folder. Move those files
  to the `current_working_folder` to recover your work.

![image](https://github.com/user-attachments/assets/d2482a1b-5058-4903-920e-ef5dbf252ef6)

## `.proto` or path to a folder

Converts a file or directory of [Protocol Buffers](https://developers.google.com/protocol-buffers) files to the AdHoc `protocol description` format.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>
```cmd
    AdHocAgent.exe MyProtocol.proto
```

![image](https://user-images.githubusercontent.com/29354319/232012276-03d497a7-b80c-4315-9547-ad8dd120f077.png)
 </details>

> [!NOTE]  
> Additional arguments can be paths to directories containing supplemental imported `.proto` files, such as
> [`well_known`](https://github.com/protocolbuffers/protobuf/tree/main/src/google/protobuf) files.
> Multiple directories are supported - for example, when imports are spread across several roots:
> ```cmd
>     AdHocAgent.exe influxdb  influxdb  google.proto
> ```
> If the last argument does not end with `.proto`, it is treated as the output destination directory rather than an import search path.

The result of `.proto` file conversion is only a starting point for migrating to AdHoc - it cannot be used as-is. Review it with the full capabilities
of AdHoc protocol in mind.

## `.json` or `.yaml`

Treats the input file as a Swagger/OpenAPI specification. An optional second argument specifies the output AdHoc protocol description `.cs` file path.
If omitted, the `.cs` file is written next to the input file. Do not expect a perfect result from this conversion; it is a starting point for
transitioning from an OpenAPI spec to AdHoc.

## `.md`

The provided path is a `deployment instruction file` for the embedded [Continuous Deployment](https://en.wikipedia.org/wiki/Continuous_deployment)
system. AdHocAgent will only repeat the deployment process for source files already received from the server. Useful for debugging deployments.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/6109d22b-d4f9-43dc-8e9b-976d38d63b32)
 </details>

> [!NOTE]  
> In addition to command-line arguments, AdHocAgent requires a configuration file:

- **`AdHocAgent.toml`:** Contains settings including:
	- The URL of the code-generating server.
	- The path to the local C# IDE binary, enabling the utility to open the IDE at a specific file and line.
	- The path to the [7-Zip](https://www.7-zip.org/download.html) binary.
		- [Windows](https://www.7-zip.org/a/7zr.exe) | [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz) | [macOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)
	- Paths to source code formatter binaries:
		- [clang-format](https://releases.llvm.org/download.html)
		- [prettier](https://prettier.io/docs/en/install.html) - install globally: `npm install -g prettier`
		- [astyle](https://sourceforge.net/projects/astyle/files/)

AdHocAgent searches for `AdHocAgent.toml` in its own directory. If not found, it generates a template to fill in.

## UUID

To get your first `volatile` personal [UUID](https://en.wikipedia.org/wiki/Universally_unique_identifier):

1. Sign in to **GitHub**.
2. Post a message in the [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up).

Once your request is processed (when the post disappears), a bot creates a new **private** project for
you [here](https://github.com/orgs/AdHoc-Protocol/projects), tracking your code generation history and surfacing any issues with resolution details.

The project will contain a task with your `UUID`:  
![image](https://github.com/user-attachments/assets/b1789e7e-3ca3-4442-839b-aca172babf4e)

Copy the `UUID` and run AdHocAgent once:

```shell
AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f
```

The utility saves the `volatile UUID` in `AdHocAgent.toml`.

> [!NOTE]  
> The UUID may be automatically renewed during new code generation requests and cannot be reused. Keep your `AdHocAgent.toml` file - it stores the
> updated UUID. If your UUID is rejected, repeat the sign-up process to get a new one.

> [!NOTE]  
> When run without arguments, AdHocAgent displays help and generates a `protocol description file` template.

## Continuous Deployment (CD) System

The embedded CD system automates deploying generated source code into your target projects. It uses a **Deployment Instructions File** (a Markdown
file) to control exactly how and where files are copied. Its key feature is **Smart Merge**, which preserves custom code inside designated injection
points, preventing your work from being overwritten during updates.

### The Deployment Workflow

1. **First run:** Run AdHocAgent. If it finds no deployment instructions file, it generates one (e.g., `AdHocProtocol.md`) and stops.
2. **Configure:** Open the `.md` file. It contains a complete tree of all source files. Add destination paths for your project folders.
3. **Redeploy:** Run AdHocAgent again. It reads your instructions and deploys the files, intelligently merging your custom code.
4. **Repeat:** After future code generation, re-run the utility. Your deployment configuration and custom code are preserved.

### The Deployment Instructions File

* **Naming:** Must match the protocol description filename with an `.md` extension (e.g., `AdHocProtocol.cs` → `AdHocProtocol.md`).
* **Location:** AdHocAgent searches first in the protocol file's directory, then the working directory.

#### Structure: The File Tree

The file contains a Markdown list representing the source directory structure:

```markdown
- 📁[InCS](/path/to/source/InCS)
	- 📁[Agent](/path/to/source/InCS/Agent)
		- ＃[Agent.cs](/path/to/source/InCS/Agent/gen/Agent.cs)
		- ＃[Channel.cs](/path/to/source/InCS/Agent/gen/Channel.cs)
```

#### Configuring Deployment Targets

Specify where files go by appending Markdown links to the end of a line: `[<regex_filter>](<destination_path>)`.

* The `regex_filter` is optional. If omitted (`[](/path)`), the rule applies to all files in scope.
* The `destination_path` is the target location on your filesystem.

##### Target Path Behavior

Behavior is determined by whether the destination path ends with `/` or `\`.

**1. Copy contents into a folder (path ends with `/` or `\`):**
Copies the *contents* of the source folder into the destination. The source folder itself is not created.

- **Folder:** `- 📁[Agent](...) [](/path/to/project/src/)`
	* Files inside `Agent` are copied directly into `/path/to/project/src/`.
- **File:** `- 🌀[demo.ts](...) [](/path/to/project/components/)`
	* The file is copied into the destination folder.

**2. Copy and rename (path does NOT end with `/` or `\`):**
Copies the source item with the exact name and location specified.

- **Folder:** `- 📁[Agent](...) [](/path/to/project/RenamedAgent)`
	* The `Agent` folder and its contents are copied to `RenamedAgent`.
- **File:** `- 🌀[demo.ts](...) [](/path/to/NewName.ts)`
	* The file is copied and renamed.

##### Inheritance and Filtering

* **Inheritance:** Rules on a parent folder are inherited by all its children.
* **Filtering:** Provide a regular expression to apply a rule only to matching files.

**Example:**
> [!TIP]
> Switch from Markdown preview to source to view detailed formatting.

```markdown
- 📁[Observer](/path/to/source/InTS/Observer)  ✅ All files go to 'src', images go to 'assets'.
  [\.(jpg|png|gif)$](/project/assets/images/)
  [](/project/src/)
	
	- 🌀[demo.ts](/path/to/source/InTS/Observer/demo.ts)  // Inherits rule → /project/src/demo.ts
	- 📁[gen](/path/to/source/InTS/Observer/gen)          // All files inside also inherit
```

##### Skipping Files and Folders

Add `⛔` to a line or use an empty target `[]()` to exclude from deployment:

```markdown
- 📁[Observer](/path/to/source/InTS/Observer) [](/path/to/project/src/)
	- 🌀[demo.ts](/path/to/source/InTS/Observer/demo.ts) ⛔ // Skipped
	- 📁[gen](/path/to/source/InTS/Observer/gen) []()        // Entire folder skipped
```

#### Advanced Processing: Execution Instructions

Run scripts or tools on source files *before* they are deployed - for formatting, linting, or other transformations. Instructions are defined in code
blocks and executed in order.

##### File Path Placeholder & Root Path

* Use the `FILE_PATH` placeholder - it is replaced with the actual file path at runtime.
* Paths starting with `/InCS/`, `/InJAVA/`, etc., are treated as relative to the source files root directory.

##### Shell Execution

```regexp
<regex_to_select_files>
```

```shell
<executable_path> <command_line_arguments_with_FILE_PATH>
```

**Example: Formatting C++, C#, and Java files with `clang-format`.**

```regexp
\.(java|cs|cpp|h)$
```

```shell
clang-format -i -style="{ColumnLimit: 120, BreakBeforeBraces: Allman}" FILE_PATH
```

##### C# Code Execution

Execute an inline C# script for more complex transformations. The file path is passed as `args[0]` to `Main`.

* **Reference assemblies:** Add assembly references in quotes at the top of the script if needed (e.g., for `System.Linq`).

**Example: Removing leading whitespace from region directives.**

```regexp
.*
```

```csharp
"System.Text.RegularExpressions"

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    public static void Main(string[] args)
    {
        var filePath = args[0];
        var pattern = @"^\s+(?=//#region|#region)";
        var content = File.ReadAllText(filePath, Encoding.UTF8);
        var updatedContent = Regex.Replace(content, pattern, "", RegexOptions.Multiline);
        File.WriteAllText(filePath, updatedContent, new UTF8Encoding(false));
    }
}
```

### Preserving Custom Code: Smart Merge

Smart Merge ensures that custom logic you add to generated files is not lost on the next deployment. It acts as an intelligent intermediary between
your hand-written code and the code generator.

#### Injection Points

An **injection point** is a marked region in a generated file where you can safely add your own code. Each is identified by a **Unique ID (UID)**.

- **C#**:
  ```csharp
  #region > receiving
  // Your custom code goes here
  #endregion > ǺÿÿČ.Project.Connection receiving  // <-- DO NOT EDIT THIS UID
  ```
- **Java/TypeScript**:
  ```typescript
  //#region > receiving
  // Your custom code goes here
  //#endregion > ǺÿÿČ.Project.Connection receiving // <-- DO NOT EDIT THIS UID
  ```

> [!CAUTION]
> **Never edit, move, or duplicate the `endregion` line or its UID.** The UID is how the system locates your safe zone. Changing it causes your custom
> code to be permanently lost.

#### Generated Blocks

Inside an injection point, you may find pre-written code snippets wrapped in special tags (`//❗<` and `//❗/>`). These are **Generated Blocks**.

```csharp
#region > receiving
// Your custom code can go here.

//❗<
    // This is a generated block. Enable or disable it as needed.
//❗/>

// Your custom code can also go here.
#endregion > ǺÿÿČ.Project.Connection receiving
```

**Working with Generated Blocks:**

* ✅ **Enable/Disable:** Comment out **the entire block (including tags)** to disable; uncomment to enable.
* ✅ **Reorder:** Move an entire block (tags and all) within its injection point.
* ❌ **Do not edit** the code *inside* a generated block - changes are discarded on the next deployment.
* ❌ **Do not modify** the tags: block markers (e.g., `//❗<`).

#### Smart Update Notifications

When a new version of the protocol is deployed, the system merges your custom code with the new file. It adds `//todo 🔴` comments to flag important
generator changes for your review:

* **New active code:** A new, active generated block was added to your injection point.
  ```csharp
  //todo 🔴 New active generated code was added by the generator. Please review as it may affect your custom logic.
  //✅<
      callNewFunction();
  //✅/>
  ```
* **Removed code you used:** A generated block you had explicitly enabled was removed by the generator in this update. To prevent silent failures in
  your logic, it is commented out with a warning rather than permanently deleted. You free to review and remove it.
  ```csharp
  //todo 🔴 The following code block was removed by the code generator. Please review.
  //   callObsoleteFunction();
  ```

💡 **Best Practice for Notifications:**
It is highly recommended to investigate all `//todo 🔴` warnings immediately to understand how the protocol update affects your custom logic. Once you
have reviewed the changes and adjusted your code, **manually delete the warning lines and the obsolete commented-out blocks** to keep your codebase
clean.

**Auto-Cleanup:** If you forget to remove them, don't worry. During the *next* code refresh cycle, the AdHoc Agent will automatically and silently
clean up any lingering `//todo 🔴` warning lines. Furthermore, any generated blocks that are both disabled (commented out) and no longer exist in the
new protocol will simply disappear.

#### Orphaned Code Protection

Sometimes, sweeping changes to a protocol mean an entire **Injection Point (UID)** is removed by the generator.

If this happens, the AdHoc Agent performs a smart "Orphan Check":

1. **Content Analysis:** It strips away all generated blocks from the missing region and checks if any hand-written custom code remains.
2. **Silent Cleanup:** If the region was empty or only contained generated code, it is safely discarded.
3. **Active Intervention:** If you wrote actual custom code in that region, the deployment is immediately paused. The console will display your "
   orphaned" code and require your explicit confirmation (`y/N`) before proceeding, ensuring your work is never deleted without your knowledge.

#### Automatic Backups & Recovery

Even if you mistakenly confirm the deletion of orphaned code, or if a deployment behaves unexpectedly, your work is safe.

Before any existing file is modified, the AdHoc Agent automatically copies the original files into a versioned backup folder (e.g., `backup_name_1`).
Inside this folder, you will find ready-to-use restore scripts (`restore.bat`, `restore.ps1`, `restore.sh`) that instantly revert your project to its
exact pre-deployment state.

### Lifecycle Hooks

Run executables at the very beginning or end of the entire deployment process:

```markdown
[before deployment]("C:\Program Files\dotnet\dotnet.exe" format "/InCS/MyProject")
[after deployment](/path/to/logging_script.sh --status=success)
```

### Automated Backups and Restoration

Before overwriting any files, the system creates a backup automatically.

#### How It Works

1. **Creates a backup directory** named sequentially (e.g., `AdHocProtocol_1`, `AdHocProtocol_2`) in the same location as the deployment instructions
   file.
2. **Copies existing files** that are about to be replaced, with generic names (`original_1.cs`, `original_2.java`) to prevent conflicts.
3. **Generates restore scripts** inside the backup directory:
	* `restore.bat` (Windows Command Prompt)
	* `restore.ps1` (Windows PowerShell)
	* `restore.sh` (Linux, macOS, WSL)

> [!IMPORTANT]
> Only files being **overwritten** are backed up. New files deployed to empty locations have no original to back up.

#### How to Restore

1. Open the latest backup folder (e.g., `AdHocProtocol_5`).
2. Run the script for your OS:
	* **Windows:** Double-click `restore.bat` or right-click `restore.ps1` → "Run with PowerShell".
	* **Linux/macOS:**
	  ```shell
	  chmod +x restore.sh
	  ./restore.sh
	  ```

Running the script copies every backed-up file back to its original project location, reverting the deployment entirely.

# Overview

The simplest `protocol description file` looks like this:

```csharp
using org.unirail.Meta; // Required for AdHoc protocol generation

namespace com.my.company // Required
{
    public interface MyProject // Declares an AdHoc protocol description project
    {
        /**
            <see cref = 'CommonPacket' /> common
            <see cref = 'Server.PacketToClient' /> server | 🖥️
            <see cref = 'Client.PacketToServer' /> client | 🔑
        */

        class CommonPacket{ } // A common empty packet used across different hosts

        /// <see cref="InTS"/>-   // Generates an abstract TypeScript version
        /// <see cref="InCS"/>    // Generates the concrete C# implementation
        /// <see cref="InJAVA"/>  // Generates the concrete Java implementation
        struct Server : Host
        {
            public class PacketToClient{ }
        }

        /// <see cref="InTS"/>    // Generates the concrete TypeScript implementation
        /// <see cref="InCS"/>-   // Generates an abstract C# version
        /// <see cref="InJAVA"/>  // Generates the concrete Java implementation
        struct Client : Host
        {
            public class PacketToServer{ }
        }

        interface Connection : Connects<Client, Server>{

            [l____________<@Connection>("common | 🔑")]
            [____________r<@Connection>("common | 🖥️")]
            struct Start { }
        }
    }
}
```

To visualize your protocol structure, run the **AdHoc Observer** by appending `?` to your protocol file path:
`AdHocAgent.exe /dir/minimal_descr_file.cs?`.

<details>
  <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/acc420a1-b2bf-4579-9ee6-5336ad155d4f)
</details>

To upload a file and get generated source code: `AdHocAgent.exe /dir/minimal_descr_file.cs`

# Protocol Description File Format

> [!IMPORTANT]  
> **The `protocol description file` follows a specific naming convention:**
>
> - Names must not start or end with an underscore `_`.
> - C# prohibits a class from having a field or nested class with the same name as the class itself - a `Pack` cannot have a field or nested pack
    sharing its name.
> - Names must not match keywords in any language the code generator supports. **AdHocAgent** checks for these conflicts before uploading.

## Project

The `protocol description file` is a plain C# source file within a .NET project, using C# as
a [DSL](https://en.wikipedia.org/wiki/Domain-specific_language).

To create one:

- Create a C# project.
- Add a reference to the [AdHoc protocol metadata attributes](https://github.com/cheblin/AdHoc-protocol/tree/master/src/org/unirail/AdHoc).
- Create a new C# source file.
- Declare the protocol description project as a C# `interface` within your company's namespace.

```csharp
using org.unirail.Meta; // Required

namespace com.my.company // Required
{
    public interface MyProject
    {
        // Add your protocol description here
    }
}
```

> [!Note]
> C# 10 introduced file-scoped namespaces, eliminating curly braces and reducing indentation:

```csharp
using org.unirail.Meta;

namespace com.my.company;

public interface MyProject
{
    // Add your protocol description here
}
```

AdHoc protocol descriptions cover both the data structures (packets and fields) and the complete network topology: hosts, connections, and their
logical interconnections.

---

## Packs Inventory

The documentation block at the top of the protocol file is the primary **User Interface** for the protocol - the user's **workspace** for organizing,
categorizing, and routing packets.

### The Core Vision: Top-Down Control

The goal is to give the user a single central place to see all available packets, categorize them semantically, and "spread" them across connections
and states using those categories (tags), direct pack references, or both.

### A. Automatic Discovery & Alphabetization

On every run, the system scans the project for all potentially transmittable classes/structs (excluding enums, headers, and meta-types). It
automatically maintains an alphabetical list (by full path) of these types at the top of the file.

**Initial state (first run) - no IDs yet, just a clean menu of available "ingredients":**

```csharp
/**
    <see cref = 'Agent.Login' />
    <see cref = 'Server.Invitation' />
    <see cref = 'Server.Result' />
*/
```

### B. Semantic User Tagging

Users "label" packets by adding text or emojis **after the `/>`** on each line. These tags are the user's workspace - the system **never deletes or
modifies** them.

```csharp
/**
    <see cref = 'Agent.Login' /> server | 🔑
    <see cref = 'Server.Invitation' /> server to client | 🖥️👉📈
    <see cref = 'Server.Result' /> metrics | 📈
*/
```

### C. ID Management (System-Managed & Reactive)

The `id = 'N'` attribute is **reactive**:

- **Assigned:** If a packet is detected as "directly transmittable" in any connection branch, the system assigns and maintains its unique ID.
- **Removed:** If a packet is no longer used by any branch, the system **removes the ID** but **keeps the line** and the user's tags.
- **Result:** Users see a clean list where "active" packets have IDs and "inactive" packets do not.

```csharp
/**
    <see cref = 'Agent.Login'      id = '5' /> server | 🔑
    <see cref = 'Server.Invitation' id = '3' /> server to client | 🖥️👉📈
    <see cref = 'Server.Result' /> metrics | 📈  ← ID removed because the branch using 📈 was deleted
*/
```

### D. The User Workflow

1. **Discovery:** User writes packet types; system lists them in the Dashboard (no IDs).
2. **Tagging:** User categorizes packets in the Dashboard using emojis/tags after `/>`.
3. **Spreading:** User declares branches on empty state structs - packs can be included directly via the `PACKS` generic (e.g.,
   `[l____________<(PackA, PackB)>]`), filtered via KeepDoc/SkipDoc/KeepName/SkipName (e.g., `[l____________("📈")]`), or both combined.
4. **Finalization:** System assigns IDs to all active packets in the Dashboard and generates the protocol "Glue" code.

---

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Example protocol description file:</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company2
{
    /**
        <see cref = 'BackendServer.ReplyInts'                   id = '7' /> backend | 📊
        <see cref = 'BackendServer.ReplySet'                    id = '8' /> backend | 📊
        <see cref = 'FrontendServer.PackB'                      id = '6' /> frontend | 📦
        <see cref = 'FrontendServer.QueryDatabase'              id = '5' /> frontend | query | 🔍
        <see cref = 'FullFeaturedClient.FullFeaturedClientPack' id = '4' /> client | full | 📝
        <see cref = 'FullFeaturedClient.Login'                  id = '3' /> client | auth | 🔑
        <see cref = 'Point3'                                    id = '0' /> common | geo | 📍
        <see cref = 'Root'                                      id = '1' /> common | base
        <see cref = 'TrialClient.TrialClientPack'               id = '2' /> client | trial | 📝
    */
    public interface MyProject{

        public class Root/*Ā*/{ // Non-transmittable base entity
            long id;
            long hash;
            long order;
        }

        class max_1_000_chars_string{ // Non-transmittable typedef
            [D(+1_000)] string? TYPEDEF;
        }

        class Point3/*ÿ*/{
            private float          x;
            private float          y;
            private float          z;
            max_1_000_chars_string label;
        }

        ///<see cref = 'InJAVA'/>
        struct FrontendServer/*ā*/ : Host{
            public class QueryDatabase/*Ą*/ : Root{
                private string? question;
            }

            public  class PackB/*ą*/{ }
        }

        ///<see cref = 'InCS'/>
        struct BackendServer/*ÿ*/ : Host{
            public class ReplyInts/*Ć*/ : Root{
                [D(300)] int[] reply;
            }

            public class ReplySet/*ć*/ : Root{
                [D(+300)] Set<int> reply;
            }
        }

        ///<see cref = 'InTS'/>
        struct FullFeaturedClient/*Ă*/ : Host{
            public class Login/*Ă*/ : Root{
                private string? login;
                private string? password;
            }

            public class FullFeaturedClientPack/*ă*/{
                max_1_000_chars_string query;
            }
        }

        ///<see cref = 'InCS'/>
        struct TrialClient/*ă*/ : Host{
            public class TrialClientPack/*ā*/{
                max_1_000_chars_string query;
            }
        }

        ///<see cref = 'InTS'/>
        struct FreeClient/*Ā*/ : Host{ }

        interface TrialConnection/*ÿ*/ : Connects<FrontendServer, TrialClient>{

            [l____________<@TrialConnection>("📍 | base | trial")]
            [____________r<@TrialConnection>("📍 | trial")]
            struct Start/*ÿ*/ { }
        }

        interface MainConnection/*Ā*/ : Connects<FrontendServer, FullFeaturedClient>{

            [l____________<@MainConnection>("📍 | base | trial | 🔑 | full")]
            [____________r<@MainConnection>("📍 | trial | full")]
            struct Start/*Ā*/ { }
        }

        interface TheConnection/*ā*/ : Connects<FrontendServer, FreeClient>{

            [l____________<@TheConnection>("📍 | base")]
            [____________r<@TheConnection>("📍")]
            struct Start/*ā*/ { }
        }

        interface BackendConnection/*Ă*/ : Connects<FrontendServer, BackendServer>{

            [l____________<@BackendConnection>("🔍 | 📍 | 📦")]
            [____________r<@BackendConnection>("📊")]
            struct Start/*Ă*/ { }
        }
    }
}
```

</details>
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Viewed in the AdHocAgent observer:</u></b></summary>  

![image](https://github.com/user-attachments/assets/6408d113-730b-4823-82c5-74159a65c5cb)

Selecting a specific connection shows the packets involved and their destinations.

![image](https://github.com/user-attachments/assets/04a3ae72-665b-4579-9c04-6304fdf7b991)
![image](https://github.com/user-attachments/assets/895b9268-1a06-467f-8337-7d4b14d7f87f)
</details>

After processing with AdHocAgent, the tool assigns packet ID numbers in the Dashboard for identification and tracking. IDs are assigned to packets
that are determined to be directly transmittable - whether they are matched by KeepDoc/SkipDoc/KeepName/SkipName filters, listed explicitly as types
in the `<PACKS>` generic parameter of a branch, or included via a Pack Set.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/51163c18-3b49-4f4f-adea-c3450c0fe01c)

> [!NOTE]  
> A project can function as a [set of packs](#project-host-or-pack-scopes).

### Extending Other Projects

To import all components from another project, extend it as a C# interface:

```csharp
interface MyProject : OtherProjects, MoreProjects
{
}
```

> [!NOTE]  
> The order of extended interfaces determines priority for name or pack ID conflicts - earlier ones take precedence.

For example, the [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs) description defines public,
external connections. Backend infrastructure on the **Server** side often requires an internal protocol for tasks like:

- Distributing workloads across nodes
- Transmitting and aggregating metrics
- Managing internal database records
- Authentication and authorization

**Options:**

1. **Create a separate `Backend` protocol description** - best when the external and internal protocols don't share packet instances.

2. **Extend the existing `AdHocProtocol` description** - use when you want both protocols integrated within a single `Server` host:
   
   ```csharp
   using org.unirail.Meta;

   namespace org.unirail
   {
       public interface AdHocProtocolWithBackend : AdHocProtocol
       {
           // Backend-specific protocol details
       }
   }
   ```

An example backend extension:

```csharp
using org.unirail.Meta;

namespace org.unirail {
    public interface AdHocProtocolWithBackend : AdHocProtocol {

        ///<see cref="InCS"/>
        struct Metrics : Host { }

        class MetricsData{
            public string UserName;
            public long LoginTime;
            public long LogoutTime;
            public long SessionDuration;
            public int LoginAttempts;
            public int FailedLoginAttempts;
            public int SuccessfulLoginAttempts;
            public string LastAccessedPage;
            public int PagesViewed;
            public string BrowserInfo;
            public string OperatingSystem;
            public bool IsSessionActive;
        }

        enum Role {
            Admin,
            User,
            Guest,
            SuperAdmin,
            Moderator
        }

        public class AuthorisationRequest  {
            public string UserName;
            public string Password;
            public string Email;
            public string IPAddress;
            public bool RememberMe;
            public string TwoFactorCode;
        }

        ///<see cref="InJAVA"/>
        struct Authorizer : Host {
            public class AuthorisationConfirmed {
                public Role Role;
                public string UserName;
                public string Email;
                public bool IsAuthenticated;
                public bool IsEmailConfirmed;
                public bool IsTwoFactorEnabled;
                public long LastLogin;
                public string ConfirmationToken;
                public long ConfirmationExpiry;
            }

            public class AuthorisationRejected {
                public string UserName;
                public string Reason;
                public long RejectionTime;
                public int FailedAttempts;
                public string IPAddress;
                public string ErrorCode;
            }
        }

        interface ConnectionToMetrics : Connects<Server, Metrics> {

            [l____________<@ConnectionToMetrics>("metrics")]
            struct One { }
        }

        interface ConnectionToAuthorizer : Connects<Server, Authorizer> {

            [l____________<@ConnectionToAuthorizer>("auth_request")]
            [____________r<@ConnectionToAuthorizer>("auth_confirmed | auth_rejected")]
            struct Start { }
        }
    }
}
```

This example introduces two new hosts (`Metrics` in C#, `Authorizer` in Java), several packs, and two connections:

* `ConnectionToMetrics` - links `Server` and `Metrics`.
* `ConnectionToAuthorizer` - links `Server` and `Authorizer`, with a request/reply pattern.

> [!IMPORTANT]
> When working with multiple protocols, you cannot combine their generated protocol-processing code in the same VM instance due to `lib` **org.unirail
** namespace clashes. Assign each project's `lib` to a distinct namespace to resolve this.

### Selective Entity Import

AdHoc provides two methods for fine-tuning imported entities: **XML documentation tags** and **generic interfaces**.

#### By XML Documentation Tags

* **Exclude (`-`)**: Prevents an entity from being imported.
* **Include (`+`)**: Imports *only* the specified entities.

To import only specific connections, enums, or constant sets:

```csharp
	/// <see cref="SomeProject.Pack"/>+
	/// <see cref="FromProjects.Connection"/>+
	interface MyProject : OtherProjects, MoreProjects
	{
	}
```

> [!NOTE]  
> Note the **plus** character after the attribute. You cannot import `State` this way.

To exclude specific entities:

```csharp
/// <see cref="MoreProjects.UnnecessaryPack"/>-
/// <see cref="OtherProjects.UnnecessaryConnection"/>-
/// <see cref="OtherProjects.UnnecessaryConnection.State"/>-
interface MyProject : OtherProjects, MoreProjects
{
}
```

> [!NOTE]  
> The **minus** character after the attribute excludes the entity.

#### By Generic Interfaces (`_<TYPES>` and `X<TYPES>`)

Use `_<TYPES>` to explicitly **add** entities and `X<TYPES>` to **remove** them from the project scope. For multiple types, use C# tuple syntax:
`_<(TYPE_A, TYPE_B)>`.

```csharp
public interface AdHocProtocol :
    OtherProject,
    _<
        (AdHocProtocol.Agent.Project.Host.Pack.Field.DataType)
    >,
    X<
        OtherProject.LegacyConnection
    >
{
}
```

**Supported operations:**

1. **`_<T>` (Add)**
	* **Connections:** Adds the connection to the project.
	* **Enums / Constant Sets:**
		* **Project level** (`interface Project : _<Enum>`): Included in **every** host in the project.
		* **Host level** (`struct Host : _<Enum>`): Included in that **specific** host regardless of field references.
	* **Hosts:** Restricted - hosts must be referenced as endpoints within a **Connection**.
	* **Packs:** Restricted - packs must be referenced within a **Branch** of a **State**.

2. **`X<T>` (Remove)**
	* **Connections:** Removes the connection.
	* **Enums / Constant Sets:** Removes from project scope.
	* **Hosts:** Removes the host *and* any Connection referencing it.
	* **Packs:** Removes the pack from the project and from every State where its tags are matched.

3. **`_<(TYPE_A, TYPE_B, ...)>`:** Use C# tuple syntax for multiple types.

> [!NOTE]  
> To import a **host**, reference it as an endpoint within a **connection**. To import a **pack**, reference it within a branch of a state.

[Learn how to modify imported packs](#modifying-imported-packs).  
[Learn how to modify imported connections](#modifying-imported-connections).

---

## Hosts

**Hosts** are the active participants in network communication, responsible for sending and receiving data packets across logical **Channels**
established over a **Connection**. A host is defined as a C# `struct` within a project's `interface` and must implement the `org.unirail.Meta.Host`
marker interface.

The AdHoc compiler generates host code only for the programming languages you explicitly specify, using XML documentation comments (`/// <see.../>`)
that define the target language and the desired implementation style.

### Implementation Modifiers

When specifying a target language, append a two-character modifier (e.g., `++`, `+-`) to control the generated code's behavior.

#### First Position: Parsing Strategy (`+` or `-`)

* `+` - **Full Object Deserialization (Concrete Implementation)**
	* The streaming parser reads the entire message and constructs a complete, in-memory object. All data is deserialized before your code accesses
	  it.
	* Best for most application and business logic - simple, stateful objects that can be passed to methods or stored.

* `-` - **Streaming Event-Based Parsing (Abstract Interface)**
	* Activates an event-driven parsing model. The generator creates an abstract base class you must implement. As the parser reads data from the
	  stream, it immediately calls methods on your implementation for each field encountered. **The full object is never allocated on the heap.**
	* Best for high-throughput, low-latency scenarios - network routers, data loggers, or services that must process messages larger than available
	  RAM.

> [!NOTE]
> The modifier set here is only the **host default**. It can be overridden per pack and even per field - the full
> (host, language, entity) resolution model lives in [Implementation Management](#implementation-management-1).

#### Second Position: Hash Support (`+` or `-`)

* `+` - Generates `Equals()` and `GetHashCode()` implementations (or signatures in abstract mode). Use when storing packet objects in hash-based
  collections.
* `-` - Skips `Equals()` and `GetHashCode()`. Reduces generated code and avoids minor overhead when hash-based storage is not needed.

#### Modifier Summary Table

| Modifier | Example                | **Parsing Strategy**          | **Hash Support** |
|:--------:|:-----------------------|:------------------------------|:-----------------|
|   `++`   | `<see cref='InCS'/>++` | Full Object Deserialization   | Enabled          |
|   `+-`   | `<see cref='InCS'/>+-` | Full Object Deserialization   | Disabled         |
|   `-+`   | `<see cref='InCS'/>-+` | Streaming Event-Based Parsing | Enabled          |
|   `--`   | `<see cref='InCS'/>--` | Streaming Event-Based Parsing | Disabled         |

> **Default: `++`** - If a language tag has no modifier (e.g., `<see cref='InCS'/>`), it defaults to `++`.

---

### The Configuration Scoping System

* **No configuration, no code.** If a host has no `<see.../>` tag for a given language, no code is generated in that language.
* **Top-down and persistent.** The generator reads `<see.../>` tags top to bottom. When it encounters a language marker, that rule becomes the *
  *active rule** for that language and applies to all following entities - until another rule for the same language appears.
* **Grouped application.** When specific packs or [Pack Sets](#pack-set) are listed immediately after a language marker, that rule is **confined** to
  that group only. The previously active rule resumes afterward.

#### Recursive Scoping with the `@` Prefix

The `@` prefix acts as an **inline recursive Pack Set**. When the generator encounters `<see cref='@Target'/>` and `Target` is not a field, it *
*recursively includes all transmittable packets** found within the scope of `Target` (a Project, Host, or nested namespace/interface).

> **Note:** The container itself is excluded. Using `@` targets its children, not the container pack.

**Example:**

```csharp
/// <see cref="InTS"/>--
/// <see cref="@RootWithNestedPacks"/>
/// <see cref="InTS"/>+-
struct MonitoringObserver : Host {
    public int Channels => 256;
}
```

#### Detailed Example

```csharp
public interface MyProject
{
    interface BackendPacksThatImplementedOnServer :
        _<
           ( @Monitoring.Network,
            @Monitoring.Authorizer,
            @Monitoring.Processing)
        >{ }

    /**
    <see cref='InCS'/>+-                        // RULE 1: Default for C#, all packs in Server

    <see cref='InJAVA'/>                        // RULE 2: Confined group for Java (defaults to ++)
    <see cref='BackendPacksThatImplementedOnServer'/>
    <see cref='ToAgent.Result'/>
    <see cref='Agent.ToServer.Proto'/>
    <see cref='Agent.ToServer.Login'/>

    <see cref='InJAVA'/>--                      // RULE 3: New Java default for remaining packs
    */
    struct Server : Host { }
}
```

How the generator interprets this:

1. **Rule 1 (`InCS+-`):** `+-` applies to **every** pack in `Server` - no further C# rules override it.
2. **Rule 2 (`InJAVA`):** Defaults to `++`, but is **confined** to the four listed entities. All other Java packs are unaffected.
3. **Rule 3 (`InJAVA--`):** `--` applies to **all remaining** packs in `Server` not covered by Rule 2.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0cfa47f2-8b2e-4e49-9c7d-0fd908dbd7ce)

</details>

> [!TIP]
> These same scoping rules resolve down to an **individual field**: a language marker followed by a `<see cref='Pack.field'/>` reference confines an
> implementation strategy (concrete `+` / abstract `-`) to that one field, while the rest of the pack keeps the host default.
> See [Implementation Management](#implementation-management-1).

---

### Advanced Host Concepts

#### Modifying Imported Hosts

To alter code generation configuration for a host defined in another project, create a `struct` implementing `Modify<T>` where `T` is the imported
host, then apply configuration rules as usual:

```csharp
/**
// For the imported 'Server' host:
// 1. Start a confined group rule for Java (++).
<see cref='InJAVA'/>
// 2. Apply this rule only to 'Pack'.
<see cref='Pack'/>
// 3. Set the new Java default for all other packs to be abstract interfaces (--).
<see cref='InJAVA'/>--
*/
struct ModifyServer : Modify<Server> { }
```

#### Host as a Named Pack Set

A `Host` definition also implicitly acts as a named [Pack Set](#pack-set), allowing you to reference all packets defined directly within that host's
scope by its name.

---

### Pack Set

A Pack Set groups related packet types under a single unit, simplifying rule application and improving reusability. Pack Sets are the primary
mechanism for defining the target group of packets for a protocol rule or operation (such as branches on states).

#### In-Place Pack Sets

The `org.unirail.Meta._<>` interface creates an ad-hoc Pack Set for flexible, inline grouping. You can group multiple items using tuple syntax
`(...)`. Conversely, use `org.unirail.Meta.X<>` to explicitly exclude specific entities from a Pack Set.

```csharp
interface Info_Result :
    _<
        (
            Server.Info,
            Server.Result,
            X<Server.Result.Deprecated> // Exclude a specific packet
        )
    > {}
```

#### Project, Host, or Pack Scopes

Instead of listing individual packets manually, a `Project`, `Host`, or `Pack` can be used as a source scope to automatically include transmittable
packets defined within them.

```csharp
interface ServerData :
    _<
        (
            Server.Info,
            Project,  // All transmittable packets directly in Project scope
            Host      // All transmittable packets directly in Host scope
        )
    > {}
```

#### Recursive Scopes (`@`)

To include all transmittable packets recursively (including all nested structures inside a container), prefix the reference with `@`.

> **Important:** The `@` prefix extracts all the contents, but excludes the container itself from the set.

```csharp
interface AllTelemetry :
    _<
        (
            @Project,           // Recursively includes all transmittable packets in Project
            @Host.ExternalLib,  // Recursively includes packets from a specific host library
            X< @ToDelete >      // Recursively exclude everything inside the ToDelete container
        )
    > {}
```

---

#### Filtering Rules

You can refine any scope using `[Keep...]` or `[Skip...]` attributes from `org.unirail.Meta`. These filter a scope using regular expressions matched
against either the full pack type name or the documentation comment.

* **Keep attributes (Additive OR):** If any `[Keep...]` attributes are present, a packet is retained only if it matches at least one pattern. If no
  `[Keep...]` attributes exist, all packets in the scope are candidates.
* **Skip attributes (Subtractive OR):** A packet is immediately removed if it matches any provided pattern.

##### By Name Filtering (`[KeepName]` & `[SkipName]`)

Filters packets based on their full type names (namespace + name). The regex is matched against the entire qualified path (e.g.,
`com.my.company.Monitoring.VolatileInfo.DiskIO.BytesTime.Subscribe`). This is excellent for protocol versioning or strict namespace targeting.

> [!IMPORTANT]
> Because the match runs against the **full type name**, a bare name like `"MyPack"` will also match `MyPackExtended`, `NotMyPack`, or
`Some.MyPackage.Data`. To target an exact pack name, anchor it with `\.` (preceding dot) and `$` (end of string):
>
> | Pattern                          | Matches                                 | Does NOT match                   |
> |:---------------------------------|:----------------------------------------|:---------------------------------|
> | `"MyPack"`                       | `X.MyPack`, `X.MyPackV2`, `X.NotMyPack` | - (too broad)                    |
> | `"\.MyPack$"`                    | `X.MyPack`                              | `X.MyPackV2`, `X.NotMyPack`      |
> | `"\.Subscribe$\|\.Unsubscribe$"` | `X.Y.Subscribe`, `X.Y.Unsubscribe`      | `X.Subscriber`, `X.Unsubscribed` |

```csharp
[KeepName(@"\.V1\.")]
// Keeps only packets containing ".V1." in their path

[SkipName(@"\.Test$")]
// Removes packets whose name ends with exactly ".Test"

// Filter for Subscribe/Unsubscribe packs at the end of the name
[KeepName(@"\.Subscribe$|\.Unsubscribe$")]
interface SubsUnsubs<SCOPE>{}
// Matches: Monitoring.VolatileInfo.DiskIO.BytesTime.Subscribe
// Matches: Monitoring.VolatileInfo.DiskIO.BytesTime.Unsubscribe
// Skips:   Monitoring.VolatileInfo.DiskIO.Subscriber
```

##### By Documentation Filtering (`[KeepDoc]` & `[SkipDoc]`)

Filters packets based on their documentation text. The generator scans a unified pool per pack that includes both `///` comments on the class
definition and the text after `/>` on the pack's line in the Dashboard - there is no distinction between the two. This enables powerful visual tagging
using emojis or short keywords in either location.

```csharp
/// 🔒 User credentials.
class Credentials { ... }

/// 📈 Server performance metrics.
class CpuStats { ... }

/// ⛔ Legacy payload.
class V1Payload { ... }

// Define filtered Pack Sets using these attributes:

[KeepDoc(@"📈")]
interface MetricsOnly<SCOPE> {}         // Keeps CpuStats

[SkipDoc(@"⛔|🙈")]
interface NoLegacy<SCOPE> {}            // Skips V1Payload

[KeepDoc(@"📈")]
[SkipDoc(@"⛔")]
interface ActiveMetrics<SCOPE> {}       // Keeps CpuStats, skips V1Payload

// Use in a branch:
[l____________< ActiveMetrics<@Project> >]
struct Dashboard { }
```

---

##### Two Filtering Paradigms

AdHoc offers two ways to apply these filters: **Filter Templates** (separated generic scopes) for maximum reusability, and **Named Pack Sets** (bound
inline scopes) for specific, pre-packaged aliases.

###### 1. Filter Templates (Generic Scopes)

By utilizing C#'s generic type syntax (`<SCOPE>`), you can separate the filtering logic from the data source. A Filter Template defines *how* to
filter, and you provide *what* to filter by passing a scope on the fly.

This functional style is perfect for enforcing global standards across a massive project.

```csharp
// 1. Define reusable filtering logic once
[SkipDoc(@"⛔|Deprecated")]
interface ActiveOnly<SCOPE> {}

[KeepName(@"\.Telemetry\.")]
interface OnlyTelemetry<SCOPE> {}

// 2. Apply filters to specific scopes dynamically via branches
interface GuestConnection : Connects<Server, Client> {
    interface StreamingActor : Actor {
        // Applies the ActiveOnly filter to the entire Project recursively.
        // The PACKS generic carries the full filtered scope - no tags needed.
        [l____________< ActiveOnly<@Project> >]
        struct ActiveState { }
    }
}
```

###### 2. Named Pack Sets (Bound Scopes)

When a specific filtered subset represents a distinct, reusable protocol concept that doesn't need to be applied generically to other scopes, you can
combine the filters and the scope into a single declaration using inheritance.

The attributes apply directly to the base interfaces (`_<...>` and `X<...>`).

```csharp
// The filter and scope are bound together. 
// Just use the name 'ImplementOnObserver' later.
[KeepName(@"\.Sessions\.")]
interface ImplementOnObserver : _<@Monitoring>, X<All_Lists_on_Observer_are_virtual> { }

[l____________< ImplementOnObserver >]
struct ObserverState { }
```

---

##### Advanced Composition & Use Cases

Because filters, scopes, and sets are all evaluated as interfaces under the hood, they can be mixed and combined in highly expressive ways using
native C# syntax.

###### Multi-Scope Union

You can pass multiple distinct scopes into a filter using tuple syntax. The generator merges them first, then applies the filter to the combined set.

```csharp
[KeepDoc(@"👁️Public")]
interface PublicView<SCOPE> {}

// Take packets from both local Project and Monitoring, then apply the filter
[l____________< PublicView< (@Project, @Monitoring.Session) > >]
struct Feed { }
```

###### Filter Composition (Intersection / AND)

A filtered set can be used as the `<SCOPE>` for another filter, creating an elegant functional pipeline. Because each filter sequentially narrows down
the set, nesting them creates a logical intersection (AND). It reads from the inside out:

```csharp
// Example 1: Pipeline filtering
// 1. Take everything in @Project
// 2. Remove anything with ⛔ (ActiveOnly)
// 3. Keep only packets with ".Telemetry." in their name (OnlyTelemetry)
[l____________< OnlyTelemetry< ActiveOnly<@Project> > >]
struct TelemetryStream { }

// Example 2: Composing Role Views
// 1. Take all packets recursively in Monitoring.Sessions
// 2. Keep only packets matching the AdminView rules
// 3. From those, keep only packets that ALSO match the PublicView rules
// Result: Strictly packets that satisfy BOTH Admin and Public requirements.
[l____________< PublicView< AdminView<@Monitoring.Sessions> > >]
struct SharedSessionStream { }
```

###### Set Arithmetic & Mixed Paradigms

Combine Generic Filters, Named Pack Sets, In-Place Sets (`_<...>`), and Exclusions (`X<...>`) to achieve exact protocol definitions.

```csharp
[KeepDoc(@"🚨")]
interface CriticalAlerts<SCOPE> {}

// A state that combines generic templates and bound pack sets - all via PACKS generic
[l____________<
    _<
        (
            ImplementOnObserver,                   // The Bound Named Pack Set defined earlier
            CriticalAlerts<@Host.ExternalLib>,     // PLUS all 🚨 packets from external host
            X< ActiveOnly<@Project.Legacy> >       // EXCEPT active packets from the legacy folder
        )
    >
>]
struct AdminDashboardState { }
```

###### Use Case: Role-Based Access Control (RBAC)

Filter templates make it effortless to define exactly what packets different client permission levels are allowed to see, all driven by emojis or tags
in pack documentation.

```csharp
[KeepDoc(@"👁️Public")] interface PublicView<SCOPE> {}
[KeepDoc(@"🛡️Admin")]  interface AdminView<SCOPE> {}

// Guest connection only gets Public packets - filter template via PACKS generic
[l____________< PublicView<@Project> >]
struct GuestStream { }

// Admin gets both Public and Admin packets using a Union (OR)
[l____________< (PublicView<@Project>, AdminView<@Project>) >]
struct AdminStream { }
```

## Empty Packs, Constants, Enums

### Empty Packs

A **transmittable** (referenced via a branch) C# class-based pack with no instance fields - only [constants](#constants) or nested pack
declarations. Implemented as singletons, it is the most efficient way to signal simple events or states over a connection.

> [!NOTE]  
> If an empty pack's sole purpose is to define hierarchy structure and should not be transmitted, switch to a C#
> struct-based [Constants Container](#constants-container), which is non-transmittable.

### Constants Container

A **non-transmittable** C# struct-based pack that may contain [constants](#constants) or nested pack declarations. Instance fields are not allowed.
Used to define hierarchy structure and deliver metadata to generated code. Can be declared anywhere within your project.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using System;
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject2
    {
        [Flags]
        enum GIMBAL_DEVICE_FLAGS
        {
            GIMBAL_DEVICE_FLAGS_RETRACT    = 1,
            GIMBAL_DEVICE_FLAGS_NEUTRAL    = 2,
            GIMBAL_DEVICE_FLAGS_ROLL_LOCK  = 4,
            GIMBAL_DEVICE_FLAGS_PITCH_LOCK = 8,
            GIMBAL_DEVICE_FLAGS_YAW_LOCK   = 16,
        }

        ///<see cref = 'InJAVA'/>
        struct Server : Host
        {
            public enum MAV_BATTERY_FUNCTION : byte
            {
                MAV_BATTERY_FUNCTION_UNKNOWN,
                MAV_BATTERY_FUNCTION_ALL,
                MAV_BATTERY_FUNCTION_PROPULSION,
                MAV_BATTERY_FUNCTION_AVIONICS,
                MAV_BATTERY_TYPE_PAYLOAD,
            }

            class ServerPack { }
        }

        ///<see cref = 'InTS'/>
        struct Client : Host
        {
            class Login
            {
                string user;
                string password;
                [D(DST_CONST_FIELD)] Binary[,]  hash;

                static int      USE_ANY_FUNCTION = (int)Math.Sin(34) * 4 + 2;
                static string[] STRINGS          = { "", "\0", "ere::22r" + "K\nK\n\"KK", STR };

                [ValueFor(DST_CONST_FIELD)]
                private static int SRC_STATIC_FIELD = 45 * (int)Server.MAV_BATTERY_FUNCTION.MAV_BATTERY_FUNCTION_ALL + 45 >> 2 + USE_ANY_FUNCTION;
                const string STR = "KKKK";

                const int DST_CONST_FIELD = 0;
            }
        }

        struct SI_Unit
        {
            struct time
            {
                const string s   = "s";
                const string ds  = "ds";
                const string cs  = "cs";
                const string ms  = "ms";
                const string us  = "us";
                const string Hz  = "Hz";
                const string MHz = "MHz";
            }

            struct distance
            {
                const string km    = "km";
                const string dam   = "dam";
                const string m     = "m";
                const string m_s   = "m/s";
                const string m_s_s = "m/s/s";
                const string m_s_5 = "m/s*5";
                const string dm    = "dm";
                const string dm_s  = "dm/s";
                const string cm    = "cm";
                const string cm_2  = "cm^2";
                const string cm_s  = "cm/s";
                const string mm    = "mm";
                const string mm_s  = "mm/s";
                const string mm_h  = "mm/h";
            }
        }

        interface MainConnection : Connects<Server, Client> { }
    }
}
```

</details>

#### Distribution Over Hosts

A Constants Container ends up in a host's generated code when **any** of the following routes places it there - the rules are additive:

1. **Declared inside that host's body.** A constants container nested inside `struct SomeHost : Host { … }` is automatically included in `SomeHost`'s
   const/enum scope and only there.

2. **Declared at project level** - i.e. directly under the project interface, not inside any host struct - **broadcast to every host** in the project:
   
    ```csharp
    public interface MyProject {
        class GlobalLimits {                      // project-scope, outside any host
            public const int MaxPayload = 65_000;
        }

        struct Client : Host { /* gets GlobalLimits */ }
        struct Server : Host { /* gets GlobalLimits */ }
    }
    ```

3. **Imported via `_<T>`**. This is the explicit routing override and may be placed at either project or host level:
	
	* **Project level** - `interface MyProject : _<EnumOrConst> { … }` → `EnumOrConst` is copied into **every** host in the project. Use this to pull
	  a constants container from elsewhere (a nested host scope, an imported project, etc.) and make it globally available.
	* **Host level** - `struct SomeHost : Host, _<EnumOrConst> { … }` → `EnumOrConst` is copied into **that specific host** only. Use this when only
	  one host needs the import.

In all three cases the code generator emits the constants as local, host-scoped static values; constants are never serialized on the wire (see the
note below under **Enums**).

---

### Enums

Enums organize sets of constants of the same primitive type:

- Use `[Flags]` to indicate a bit field or set of flags.
- Manual value assignment is optional. Fields without explicit values are automatically assigned integers; with `[Flags]`, each field gets a unique
  power of two.

> [!NOTE]
> Enums and all constants are replicated on every host and are not transmitted. They serve as local copies available within each host's scope.

#### Distribution Over Hosts

An Enum or Constants Container is included in a generated Host only if one of the following is true:

1. **Declared in host scope.** The enum / constants container is declared inside that Host's body.
2. **Referenced by a field type.** It is referenced as the type of a field in a pack transmitted, received, or implemented by that Host.
3. **Referenced by a constant expression** - described below.

You can always override the default placement with `_<T>` at project or host level (same as Constants Containers above).

#### Cross-Pack Constant References

When a pack in a host's scope (transmitted, received, or implemented) defines a constant whose **initializer expression references
constants from *another* constants / enum pack**, that other pack is automatically added to the host's const/enum scope - so the
generated code can resolve the identifier at compile time.

**Example.** A `Sessions.Event` pack declares composite event IDs by OR-ing flags and actions that live in two sibling constants
packs - `Event.Mask` and `Event.Action`:

```csharp
public class Event {
    // …regular fields…
    public static class Mask   { public const uint REMOTE = 1u << 31; /* … */ }
    public static class Action { public const uint CONNECT = 1;        /* … */ }

    // The composite constants reference BOTH nested containers:
    static uint REMOTE_CONNECT = Mask.REMOTE | Action.CONNECT;
    static uint THIS_CONNECT   = Mask.THIS   | Action.CONNECT;
    // …
}
```

Every host that sends, receives, or implements `Event` must be able to resolve `Mask.REMOTE`, `Action.CONNECT`, etc.
The generator therefore walks each constant's initializer, follows every identifier that resolves to a constant field in
a *different* constants / enum pack, and pulls that owning pack into the host's scope alongside `Event`.

Which hosts get `Event.Mask` and `Event.Action` pulled in:

- **Monitoring** - transmits `Event` → pulled in.
- **MonitoringObserver** - receives `Event` → pulled in.
- **Server** - implements `Event` via `Modify<Server>` → pulled in.
- Any host where `Event` is not in scope → not pulled in.

> [!NOTE]
> **Exception - pure constants only.** The cross-reference rule adds a referenced pack *only if the host does not already transmit
> or receive that pack*. Packs already flowing through the normal pipeline are not duplicated into the constants/enums list.

The rule is reference-driven, not structural. It works regardless of where the referenced pack is declared (sibling, nested,
elsewhere in the tree), and it handles arbitrary expressions - bitwise, arithmetic, nested, chained member access.

### Modifying Enums and Constants

Enums and constants can be modified like a [simple pack](#modifying-imported-packs), but the modifier is discarded after the modification is applied.

---

## Packs

Packs are the smallest units of transmittable information, defined as C# `class` declarations. They can be nested and placed anywhere within a
project's scope.

Instance **fields** represent the data transmitted. A pack may also contain [constants](#constants) or nested pack declarations.

> [!NOTE]  
> A pack can act as a [set of packs](#project-host-or-pack-scopes) - keep this in mind when organizing the pack hierarchy.

### Implementation Management

A pack's **implementation kind** is decided per **(host, language, pack)** - see [Implementation Management](#implementation-management-1) in the
Fields chapter for the full model, declaration rules, and resolution precedence. In short: `+` generates a concrete, fully materialized object; `-`
generates an abstract base
class whose fields are delivered to your implementation as they stream off the wire, with the whole object never allocated. By default a pack follows
its host's [implementation modifier](#modifier-summary-table) for the language being generated; an explicit rule pins exactly one
**(host, language, pack)** combination.

Make one pack abstract on a host while its siblings keep the host default:

```csharp
/**
    <see cref='InJAVA'/>                // Java default for this host: ++ (concrete, materialized objects)

    <see cref='InJAVA'/>-               // confined scope: ABSTRACT implementation...
    <see cref='Telemetry'/>             // ...applied to this pack only
*/
struct Collector : Host {

    public class Telemetry {            // ABSTRACT in Java on this host - parsed field-by-field via your
        long  sensorId;                 //   handler; the whole Telemetry object is never allocated
        int   sequence;
        float value;
    }

    public class Heartbeat {            // concrete in Java (host default) - an ordinary stored object
        long timestamp;
    }
}
```

The same `Telemetry` pack delivered to (or generated for) a *different* host is unaffected by this rule - it follows that host's own configuration. To
override the implementation of a single field within a pack, see [Implementation Management](#implementation-management-1).

### Inheritance

AdHoc Packs use a **hybrid composition model** for constructing complex data structures by mixing fields from various sources (Mixins) or inheriting
them (OOP).

**The core principle: name occupation.** The generator builds the final field list by checking sources in strict order. Once a field name is "
occupied," any subsequent attempt to add a field with the same name is skipped.

**Resolution hierarchy:**

1. **Native fields** (highest priority) - fields written explicitly in the class body always win.
2. **XML documentation includes** (`<see .../>+`) - processed top-to-bottom; first occurrence wins.
3. **Inheritance** (`base` / `_<...>`) - base class fields are added last; already-occupied names are skipped.

---

**XML-Driven Composition (Mixins)**

Use XML documentation to inject or remove fields before the generator resolves inheritance.

| Operator | Action  | Description                                                                        |
|:--------:|:--------|:-----------------------------------------------------------------------------------|
| **`+`**  | Include | Imports fields from the target if the name is not yet taken.                       |
| **`-`**  | Exclude | Pre-emptively blocks a field name, preventing it from being imported or inherited. |

Because fields are imported via **symbolic XML references**, the pack creates a **live link** to the original definition - not a static copy.

**Single Source of Truth (SSOT):** The source class is the only place definitions exist. Update Packs are projections of that model. Rename a field in
the source via IDE refactoring and the XML tag updates automatically. Change a field's type and all referencing Packs adopt the new type.

**Example**

```csharp
class Player {
    /// <summary>Unique ID</summary>
    public int id;
    
    /// <summary>Current game score</summary>
    public int score;

    // 📉 The Update Pack: Defines a packet { int id; int score; }
    /// <see cref="Player.id"/>+
    /// <see cref="Player.score"/>+
    class Update_score { }
}
```

After renaming `score` → `experience` and changing its type to `long`:

```csharp
class Player {
    public int id;
    
    // ✏️ CHANGE: Renamed 'score' -> 'experience' and changed type 'int' -> 'long'
    public long experience;

    // ✅ MIRACLE: This pack is ALREADY fixed.
    // The IDE automatically updated the XML reference during the rename.
    // The Generator automatically pulls the new 'long' type.
    
    /// <see cref="Player.id"/>+
    /// <see cref="Player.experience"/>+   // Updated automatically by IDE
    class Update_score { }
}
```

The `Update_score` pack is now generated as `{ int id; long experience; }` with no manual intervention.

---

**Overriding Fields**

To override an inherited field, use the **"Exclude-then-Add"** pattern:

```csharp
/// <see cref="Header"/>+
/// <see cref="Header.id"/>-       // Block 'id' from being imported
/// <see cref="ExtendedHeader.id"/>+ // Import 'id' from new source
class MyPacket { ... }
```

---

**C# Inheritance Support**

Single inheritance:

```csharp
class MyPack : BasePack { ... }
```

Multiple inheritance via `_<TYPES>`:

```csharp
class MyPack : Base, _<(BaseA, BaseB, BaseC)> { ... }
```

If multiple base classes define the same field name, the leftmost source wins.

---

**Comprehensive example:**

```csharp
using org.unirail.Meta;

namespace com.my.project
{
    class CommonHeader
    {
        public int id;
        public int version;
        public string debug_tag;
    }

    class SessionInfo
    {
        public string token;
        public long expires;
    }

    // CASE 1: NATIVE PRIORITY
    /// <see cref="CommonHeader"/>+
    struct CustomHeader
    {
        // Native 'version' takes priority; CommonHeader.version is skipped.
        public string version;
    }

    // CASE 2: FILTERING
    /// <see cref="CommonHeader"/>+
    /// <see cref="CommonHeader.debug_tag"/>-
    /// <see cref="SessionInfo"/>+
    struct LoginPacket { }

    // CASE 3: EXPLICIT OVERRIDE
    /// <see cref="CommonHeader"/>+
    /// <see cref="CommonHeader.id"/>-  // Block 'int id' from CommonHeader
    struct BigIdPacket
    {
        public long id; // Native 'long id' fills the slot
    }
}
```

### Field Injection

The `FieldsInjectInto` interface defines a "template" class whose fields are automatically injected into the payload of other packets. The template
class itself is not preserved as a packet.

`FieldsInjectInto< PackSet >` injects fields only into transmittable packets within the specified [`PackSet`](#pack-set).

**Example:**

```csharp
class CommonFields : FieldsInjectInto< _<(MyProject, X<Point2d>)> > {
    string name;
    int length;
}

class Point2d {
    float X;
    float Y;
}

class Point3d {
    float X;
    float Y;
    float Z;
}
```

**Result:**

```csharp
// Point2d: unchanged (excluded)
// Point3d: injected fields prepended
class Point3d {
    string name;
    int length;
    float X;
    float Y;
    float Z;
}
```

> [!NOTE]  
> If a target packet already has a field with the same name as an injected field, the injector's definition (type, attributes, documentation) takes
> precedence.

---

#### Modifying Imported Field Injection

Target a specific `TargetFieldInjection` using `org.unirail.Meta.Modify<TargetFieldInjection>` and specify a **PackSet** to add or remove fields:

```csharp
class FieldInjectionModifier : Modify<TargetFieldInjection>, _<(AddPack, X<RemovePack>)>  {
    string name;
    int length;
}
```

---

### Pack Headers

A **packet header** contains protocol-level metadata that is strictly segregated from the application payload. These fields handle essential network
tasks such as routing, stream sequence management, actor identification, and session control.

Under the hood, the AdHoc compiler treats headers as injected, artificial fields (e.g., `_header0`) that are prepended to the packet during
transmission but do not pollute your application-level data models.

#### Key Characteristics & Strict Constraints

The protocol parser enforces highly rigid rules for header definitions to ensure fast, deterministic parsing at the network edge:

- **Transmission Order**
  Headers are transmitted and parsed **after** the pack identifier but **before** the main payload. They are immediately accessible to network event
  handlers before the full payload is deserialized.
- **Standalone Packets Only**
  Headers are attached **only** to explicitly transmittable packets (root packets picked up by branches).
  *If a packet is embedded as a field inside another packet (a Sub-packet), its header is completely stripped and ignored.*
- **Strict Data Type Limitations**
  Because headers must be parsed blindly at the transport layer, they are heavily restricted:
	- Must use **single, primitive, non-nullable** types (`bool`, `int`, `long`, `double`, `float`, `short`, `byte`, `ulong`, etc.).
	- **NO** Nullables (`int?`).
	- **NO** Arrays, Maps, Sets, or Strings (`string`, `byte[]`).
	- **NO** VarInt Compression. You cannot use `[V]`, `[A]`, or `[X]` attributes on header fields. They must have fixed wire sizes.
- **Automatic Conflict Resolution**
  If a header field shares a name with a payload field (or a field in another header), the compiler will automatically rename it (appending `_1`,
  `_2`, etc.) and issue a compilation warning to prevent data loss.

#### Contextual Scope & Declaration

Headers are defined by creating a class that implements `HeaderFor<PackSet>`. Where you declare this class dictates its scope. In fact, `HeaderFor` is
the **only** pack type allowed to be declared directly inside a `Connection` interface.

1. **Project Scope (Global):** Declared at the root level. Applies to the targeted packs everywhere in the project.
2. **Host Scope:** Declared inside a `Host` struct. Applies to the targeted packs only when transmitted or received by that specific Host.
3. **Connection Scope:** Declared inside a `Connects<A, B>` interface. Applies to the targeted packs only when transmitted over this specific
   connection.

**Example Usage:**

```csharp
// 1. Project-scope header: Applies globally to Point2d
class SessionHeader : HeaderFor<Point2d> {
    int plain_id;
    int session;
}

// Packet payload definitions
class Point2d { float X; float Y; }
class Point3d { float X; float Y; float Z; }

// 2. Host-specific scope
struct NodeB : Host {
    // Overlays a WorldHeader onto both Point3d and Point2d, 
    // but ONLY when NodeB is involved.
    class WorldHeader : HeaderFor<(Point3d, Point2d)> {
        int world_id;
        long timestamp;
    }
}

// 3. Connection-specific scope
interface CommunicationConnection : Connects<NodeA, NodeB> {
    // NOTE: Normal packs cannot be declared here. Only Headers and Modifiers!
    class CoordinationHeader : HeaderFor<TeamCoordination> {
        uint sequence_num;
        ushort priority;
    }
    
    // ... Actors and States ...
}
```

**Resulting On-the-Wire Structure**

**`Point3d` Packet (Sent from `NodeB`)**:
Applies the *Host-Specific* header.

```text
[-- HEADER --]
  pack_id     (Implicit)
  channel_id  (Conditional: if MultiChannelHost is used)
  world_id    (Host-Specific: from WorldHeader)
  timestamp   (Host-Specific: from WorldHeader)
[-- PAYLOAD --]
  X, Y, Z
```

**`TeamCoordination` Packet (if send via `CommunicationConnection`)**:
Applies the *Connection-Specific* header.

```text
[-- HEADER --]
  pack_id        (Implicit)
  channel_id     (Conditional: if MultiChannelHost is used)
  sequence_num   (Connection-Specific: from CoordinationHeader)
  priority       (Connection-Specific: from CoordinationHeader)
[-- PAYLOAD --]
  ... (TeamCoordination fields)
```

The protocol allows you to mutate imported or existing headers without rewriting them. By implementing `Modify<TargetHeader>`, you can inject new
fields into an existing header, overwrite existing fields, or change which packets the header applies to.

**Modifier Capabilities:**

1. **Field Injection/Override:** Any field declared in the modifier will be injected into the target header. If a field with the same name already
   exists in the target header, the modifier **overwrites** it.
2. **Target Pack Expansion:** By inheriting from packs or pack sets, you add them to the header's target list.
3. **Target Pack Reduction:** By inheriting from `X<PackToSkip>`, you explicitly remove a packet from the header's target list.

**Modifier Example:**

```csharp
// We want to modify the globally defined 'SessionHeader'
// 1. We add 'Point3d' to the packets that get this header.
// 2. We remove 'Point2d' from getting this header (using the X<> exclusion modifier).
class SessionHeaderModifier : Modify<SessionHeader>, Point3d, X<Point2d> {
    
    // This field is injected into SessionHeader
    long routing_hash; 
    
    // If SessionHeader already had a 'session' field, this overrides it
    uint session; 
}
```

---

### Value Pack

A **Value Pack** packs multiple fields into a single primitive type of up to 8 bytes.

**Key features:**

- **Zero heap allocation:** Stored as value types, avoiding GC overhead.
- **Compact memory layout:** Fields packed into eight bytes or fewer.
- **Type safety:** Full compile-time validation. Fields must be primitive numeric types or other Value Packs.
- **Automatic implementation:** The generator produces optimized packing/unpacking code.

```csharp
// 6 bytes total, packed into an 8-byte primitive (long)
class PositionPack {
    float x;     // 4 bytes
    byte layer;  // 1 byte
    byte flags;  // 1 byte
}
```

#### Smart Flattening

Nested single-field Value Packs are automatically flattened:

```csharp
class Temperature { float celsius; }
class SensorReading { Temperature measurement; }
// Result: class SensorReading { float measurement; }
```

Nullability is preserved through the chain:

```csharp
class Pressure { float kilopascals; }
class PressureSensor { Pressure? reading; }
// Result: class PressureSensor { float? reading; }
```

Deep chains are also flattened:

```csharp
class Voltage { float volts; }
class PowerLevel { Voltage? level; }
class DeviceStatus { PowerLevel? power; }
// Result: class DeviceStatus { float? power; }
```

The following fields all result in the same underlying type - `Set<float?>`:

```csharp
Set<FloatWrapper?>          set_a;
Set<FloatWrapperNullable>   set_b;
Set<FloatWrapperNullable?>  set_c;
```

#### Generated Representation per Language

A Value Pack is a logical record collapsed into a single primitive (≤ 8 bytes). The generator preserves that zero-allocation, zero-indirection model
in every target - the syntax differs, the runtime cost does not.

**C# - `readonly struct`**
A value type wrapping the primitive, with field accessors exposed as properties/methods. Stack-allocated, no GC, pass-by-value - a direct language
match.

**Java - `SlimStruct` + [SlimEnum](https://plugins.jetbrains.com/plugin/10316-slimenum)**
Java has no user-defined value types, so the generator emits a class whose **instances are never created**. Every field access is a `static` method
that bit-slices a `long`/`int` parameter - the "struct" is just a namespace over a primitive.

The authoring ergonomics come from the **[SlimEnum IntelliJ plugin](https://github.com/cheblin/SlimEnum)**:

- Generated parameters and fields are tagged with `@interface` constant sets.
- The IDE then treats a plain `long` as if it were a typed enum or flag set - context-aware completion, switch-case narrowing, OR-combination
  awareness for flags, and invalid-value detection.
- Without it, users would face opaque `long`s and memorized bit layouts. With it, pack-manipulating Java reads like working with real enums - the same
  authoring feel as C# enums, at zero runtime cost.

SlimEnum is what closes the gap between Java and languages with native value types.

**TypeScript - `type` alias + merged namespace**
No value types exist, so the generator uses TS declaration merging:

- `type Pack = number` - the pack *is* a number.
- `namespace Pack { ... }` - holds per-field `get` / `set` / `hasValue` / `to_null` helpers that do the bit math.
- Whole-pack nullability uses an out-of-range sentinel encoded as a literal type, so TS narrows it automatically.

| Target     | Representation                                       | Zero alloc | IDE ergonomics via  |
|:-----------|:-----------------------------------------------------|:----------:|:--------------------|
| C#         | `readonly struct`                                    |   native   | language            |
| Java       | static methods over a primitive + `@interface` tags  |    yes     | **SlimEnum plugin** |
| TypeScript | `number` + merged namespace + literal-type sentinels |   native   | TS literal types    |

All three produce identical bytes on the wire.

### Modifying Imported Packs

Create a new pack implementing `org.unirail.Meta.Modify<TargetPack>` to merge fields into the target. Use XML comments to add or remove specific
fields:

```csharp
/// <see cref="Agent.Proto.proto"/>+   // Add field to target
/// <see cref="Agent.Login.uid"/>-     // Remove field from target
class Pack : Modify<TargetPack> {
    public string UserName;
    public long LoginTime;
}
```

> [!NOTE]  
> A modifier pack can function as a normal pack.

---

# Connections

A **Connection** is the static definition of a remoting link - the typed pipe through which all protocol logic flows between two hosts. Every
message, every state transition, every RPC call is declared inside a Connection.

Connections are declared as C# interfaces that extend `org.unirail.Meta.Connects< HostLeft, HostRight >`:

```csharp
using org.unirail.Meta;

namespace com.company {
    public interface MyProject {
        interface Communication : Connects<Client, Server> { }
    }
}
```

> [!IMPORTANT]
> **[Data is represented on the wire in little-endian format.](https://news.ycombinator.com/item?id=25611514)**

---

The body of a Connection interface is where you define its **protocol flow**: the logical sequence of messages, the ordering of packets, and the
valid response patterns. You do this by declaring [`Actors`](#actors), [`States`](#states), and [`Branches`](#branches-routing-attributes).

Together, these constructs define a **Finite State Machine (FSM)** for each participating actor. The FSM tracks which `State` the communication is
currently in, which in turn determines which messages are valid to send or receive at that moment.

---

## Importing and Composing Connections

Connections can be based on other connections. Use standard C# interface inheritance, and use `SwapHosts<Connection>` to reverse the host roles
of imported content:

```csharp
interface CommunicationConnection : Connects<Server, Client>,
                                    SomeOtherConnection,
                                    SwapHosts<TheConnection> { }
```

---

## Actors

An **Actor** is the unit of concurrent, stateful behavior inside a Connection. Each Actor owns an independent FSM - an isolated logical thread of
conversation between two hosts.

1. **Exactly One Linked Chain:** An Actor may contain only **one** sequence of states connected by transitional branches (`L____________` or
   `____________R`).
   This represents the Actor's "Main Thread" or synchronized FSM.
2. **Unlimited Isolated States:** An Actor may contain **any number** of isolated states. These states use **non-transitional** branches (
   `l____________`,   `____________r`, or `_____lr_____`) that **do not change the Actor's state**. Isolated states use the Actor scope as
   a **logical grouping unit**.
   Related fire-and-forget notifications or status updates that don't drive a workflow are grouped into the same Actor to keep the API organized.

### The Default Actor (Actor0)

Every connection interface contains one implicit, connection-wide actor known as **Actor0**. This is the "Primary Pipe." You do not need to declare
it; it
is always there to handle connection logic, discovery, or fire-and-forget notifications.

The connection body itself acts as the declaration scope for Actor0. How Actor0 is populated depends on how you use the interface body:

| Declaration Style    | Resulting Structure                          |
|:---------------------|:---------------------------------------------|
| ** `struct` States** | Actor0 uses these states as its primary FSM. |
| ** RPC Methods**     | These methods are Actors nested into Actor0. |

If `Actor0` FSM contains multiply states, this actor restricted to only one instance.   
Actors declared in the Connection body create own hierarchy and never nested into Actor0.

---

### Singleton Actor

| Property      | Description                                                                                                                                                                                                                                                           |
|:--------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Identity**  | Bound to the **Type** - fixed, predefined address                                                                                                                                                                                                                     |
| **Lifecycle** | Permanent; always available on both hosts                                                                                                                                                                                                                             |
| **FSM**       | Isolated, non-linked states; uses **non-transitional branches** exclusively (`l____________`, `____________r`, or `_____lr_____`). Because there are no transitions, neither host acts as "Main" - **both sides are equal**. Each state acts as a logical aggregator. |
| **Memory**    | Zero - no instance is ever created                                                                                                                                                                                                                                    |

Singleton actors are the workhorse of **fire-and-forget** messaging. Because they have no linked state transitions, no session needs to be tracked,
and no actor instance is ever allocated. A group of related fire-and-forget functions is simply a host-wide global singleton with multiple independent
non-transitional branches.

---

### RPC Actor - Transient / Asymmetric

| Property      | Description                                                          |
|:--------------|:---------------------------------------------------------------------|
| **Identity**  | Bound to a **Dynamic Address** (the callback address)                |
| **Lifecycle** | Created on call; destroyed on reply                                  |
| **Memory**    | Initiator-only: **only the caller** allocates a small context object |

All C# method-based declarations and simple request-response flow Actors. The design and implementation is intentionally asymmetric:

- **The Initiator (Stateful):** Spawns a local actor instance to hold the callback context. It waits in the `Return` state to ensure the returning
  packet is valid and directed to the right logic.
- **The Replier (Stateless):** Receives the request, processes it, and sends the reply to the dynamic address. **The replier never allocates an actor
  instance.**

---

### Stateful Actor - Synchronized / 1-to-1

A Stateful Actor owns a **single chain of linked states** - at least one state in the chain must contain a **transitional branch** (`L____________` or
`____________R`) that advances the FSM to another user-defined state.

| Property            | Description                                                     |
|:--------------------|:----------------------------------------------------------------|
| **Identity**        | Bound to **Type + Instance** - unique dynamic address per pair  |
| **Lifecycle**       | Managed via `End` or `Close` terminal states                    |
| **Synchronization** | A shared "Epoch" (logical clock) increments on every transition |

When the initiator creates an actor instance, a peer instance is automatically created on the other host - both sides allocate a real, stateful
object. The two instances are permanently bound by sharing the same dynamic address, so every message travels directly between these exact
counterparts. Both run identical replicas of the shared FSM - the single source of truth.

Within the state chain, the presence of a transitional branch assigns the **Main** role to that host, granting it sole authority to advance the state,
while the opposite host acts as the **Follower** (limited to executing non-transitional branches within that state). This asymmetric authority
prevents race conditions; stale messages carrying an outdated Epoch are silently dropped.

Use Stateful Actors for complex, multistep workflows: handshakes, file streaming, collaborative editing.

---

### Declaring Actors

#### Fire-and-Forget

A fire-and-forget operation is a one-way notification: one host sends a packet, no response is expected, and the FSM remains in its current state.
These are declared with states using **non-transitional branches**:

- `[l____________<PACKS>(...)]` or `[l____________(...)]` - Left host initiates (send)
- `[____________r<PACKS>(...)]` or `[____________r(...)]` - Right host initiates
- `[_____lr_____<PACKS>(...)]` or `[_____lr_____(...)]]` - Either side may send

Where `(...)` stands for the optional constructor parameters `(KeepDoc, SkipDoc, KeepName, SkipName)`. Packs can be specified directly in the `PACKS`
generic (without `@`), filtered via KeepDoc/SkipDoc/KeepName/SkipName over scopes (with `@`), or both.

Each isolated state acts as a named function group: a logical aggregator of related fire-and-forget operations.

**Example: Left initiates, overloads**

*Traditional C# equivalent:*

```csharp
void LogEvent();
void LogEvent(StringMessage msg);
void LogEvent(WarningEvent ev);
void LogEvent(ErrorEvent ev);
```

The overloads are collapsed into a single FSM state using tag-based routing.

*AdHoc FSM declaration:*

```csharp
interface ClientServerConnection : Connects<Client, Server>{
    interface LogEventActor : Actor {
        // Direct packs - all overloads listed explicitly
        [l____________<(NoArg, StringMessage, WarningEvent, ErrorEvent)>]
        struct LogEvent { }//state  is groupping functions
    }
}
```

> [!NOTE]
> Alternatively, if these packs are documented (e.g., with `log_events` in their `///` comments or Dashboard line), you can use the KeepDoc form:
> `[l____________("log_events")]`

**No-Argument Overloads**

The packet system requires every branch to carry a concrete type - there is no native "void argument." To declare a function that takes no argument,
create a reusable empty sentinel class **once** per project:

```csharp
class NoArg { }
```

Tag it in the Dashboard alongside the other packets for that state.

**Example: Grouping related fire-and-forget functions**

Related fire-and-forget functions can be grouped as multiple states under a single actor. This is the preferred compact form - each state is a named,
always-available function group.

*AdHoc FSM declaration:*

```csharp
interface ClientServerConnection : Connects<Client, Server>{
    interface MyFunctions : Actor {
        [l____________<(StringMessage, WarningEvent, ErrorEvent)>]
        struct LogEvent { }

        [l____________<(StatusPayload, OR_PartialStatus)>]
        struct UpdateStatus { }
    }
}
```

Because there are no transitions, the actor is automatically a host-wide global singleton. No `MaxActiveInstances` declaration is needed.

---

#### Unidirectional Request-Response (RPC)

One host initiates a call and expects a result from the other. The direction marker placed **inside the return tuple** names the host that **sends the
request**. The opposite host always sends the reply.

> [!NOTE]
> Unlike fire-and-forget, a request-response actor requires two states (`Call` → `Return`) and must transition to `End` to destroy the actor after
> completion.

**Example: Simple call, single argument, single return type**

*Traditional C# equivalent:*

```csharp
UserProfile GetUser(UserId id);
```

*AdHoc FSM declaration (Left initiates):*

```csharp
interface ClientServerConnection : Connects<Client, Server> {
        (L____________, UserProfile) GetUser(UserId id);
}
```

**Example: Multiple argument overloads, multiple return types including errors**

*Traditional C# equivalent:*

```csharp
FileData FetchFile(FileName name);
OR_NotFound FetchFile(FileId id);
```

*AdHoc FSM declaration:*

```csharp
interface ClientServerConnection : Connects<Client, Server> {
    (L____________, FileData, OR_NotFound) FetchFile((FileName, FileId) query);
}
```

in this shorthand form, implicitly `int MaxActiveInstances => UNLIMITED;`.
use [Full-Featured form](#full-featured-actor) to declare MaxActiveInstances explicitly.

```csharp
interface FetchFile: Actor{
    int MaxActiveInstances => 14;

    [L____________<Return, (FileName, FileId)>]
    struct Call { }

    [____________R<End, (FileData, OR_NotFound)>]
    struct Return { }
}
```

RPC-like actors do not support multicasting due to their short-lived nature.

---

#### Bidirectional Request-Response

When **either host** may independently initiate the same interaction, omit the direction marker from the return tuple entirely. The code generator
produces **two distinct states** - one for each direction - so both sides can initiate without race conditions.

**Example: Either host can look up a user**

*Traditional C# equivalent:*

```csharp
// Both sides can call this
(UserInfo, OR_NotFound) LookupUser(UserId id);
```

*AdHoc FSM declaration (produces two symmetric actors):*

```csharp
interface ClientServerConnection : Connects<Client, Server> {
    (UserInfo, OR_NotFound) LookupUser(UserId id);
}    
```

---

#### Cascading RPC (Multi-Hop Relay)

When a host has no direct connection to the target, RPC calls can be **cascaded** through intermediate hosts. Each hop is an independent actor with
its own lifecycle - the middle host receives the reply, then relays it forward.

**Example: Browser downloads session files from Server via Monitoring**

`MonitoringObserver` (a browser) has no direct connection to `Server`. It can only reach `Server` through `Monitoring`:

```
MonitoringObserver ←→ Monitoring ←→ Server
     (browser)         (relay)      (data source)
```

The same RPC signature is declared on both connections:

```csharp
struct Server : Host { }
struct Monitoring : Host {
    public class Download { long session_id; }
    public class Upload   { Binary[,,] data; }
}
struct MonitoringObserver : Host { }

interface ServerToMonitoring : Connects<Server, Monitoring> {
    (____________R, Monitoring.Upload) getSessionFiles(Monitoring.Download req);
}

interface MonitoringToObserver : Connects<Monitoring, MonitoringObserver> {
    (____________R, Monitoring.Upload) getSessionFiles(Monitoring.Download req);
}
```

**The fully async cascade:**

1. **MonitoringObserver** (browser) calls `getSessionFiles` → a transient RPC actor is created on MonitoringObserver and Monitoring.
2. **Monitoring** receives the request. In its handler code, it initiates a **new** `getSessionFiles` call toward Server → a second, independent RPC
   actor is created on Monitoring and Server.
3. **Server** processes the request, sends `Upload` reply, its actor is destroyed.
4. **Monitoring** receives the reply from Server, its Server-side actor is destroyed. Monitoring then passes the data as a reply to
   MonitoringObserver, its Observer-side actor is destroyed.
5. **MonitoringObserver** receives the final reply, its actor is destroyed. User code gets the session files.

Each hop is a self-contained actor pair. Monitoring holds two actors simultaneously for the duration of the relay - one facing the Observer, one
facing the Server. The developer writes the relay logic in Monitoring's `getSessionFiles` handler; the protocol infrastructure handles actor creation,
addressing, and cleanup at every hop.

---

#### Full-Featured Actor

For complex workflows requiring **multiple states**, declare a full Actor explicitly. Actors are C# interfaces inside the Connection scope that extend
`org.unirail.Meta.Actor`. Their concurrency mode is configured by inheriting from `Actor` and setting `MaxActiveInstances`.

##### Concurrency Modes

| Mode                   | Declaration                            | Identity                                | Behavior                                                      |
|:-----------------------|:---------------------------------------|:----------------------------------------|:--------------------------------------------------------------|
| **Singleton**          | `int MaxActiveInstances => 1`          | Fixed, predefined address               | One per connection; stable destination                        |
| **Swarm**              | `int MaxActiveInstances => 14;`        | Dynamic per-instance address            | Up to N concurrent instances                                  |
| **Unlimited Swarm**    | `int MaxActiveInstances => UNLIMITED;` | Dynamic per-instance address            | No limit checks - ideal for short-lived RPC actors            |
| **Multicast (PubSub)** | `int MaxActiveInstances => +22;`       | Dynamic instances + fixed group address | Sending to the group address fans out to all active instances |

RPC-like actors do not support multicasting due to their short-lived nature.
You have to set the `MaxActiveInstances` explicitly.

**Example:**

```csharp
using org.unirail.Meta;

namespace com.company {
    public interface MyProject {
        interface Communication : Connects<Client, Server> {

            // Per-connection singleton (fixed address, one instance).
            interface MainControllerActor : Actor { int MaxActiveInstances => 1; }

            // Swarm of up to 14 instances (dynamic addresses).
            interface CPUMetricsActor : Actor { int MaxActiveInstances => 14; }

            // Unlimited swarm - zero limit-check overhead.
            interface BackgroundTaskActor : Actor { int MaxActiveInstances => UNLIMITED; }

            // Swarm + PubSub - one shared fixed address multicasts to all active instances.
            interface ChatRoomMemberActor : Actor { int MaxActiveInstances => +22; }
        }
    }
}
```

---

### Nesting Actors

In large systems, nest actor interfaces inside the Connection scope to group related actors logically. The code generator respects the hierarchy and
organizes the generated API accordingly.

**Example: Smart Factory Protocol**

```csharp
using org.unirail.Meta;

interface FactoryLink : Connects<Agent, Server> {

    interface Infrastructure {

        // Singleton health monitor
        interface HealthMonitor : Actor {

            [l____________<(BatteryLevel, Temperature, CpuLoad)>]
            [____________R<DiagnosticMode, RequestSelfTest>]
            struct Active { }

            [l____________<TestProgress>]
            [____________R<Active, TestResult>]
            struct DiagnosticMode { }
        }
    }

    interface Production {

        // Multi-instance task runner
        interface TaskRunner : Actor {
            int MaxActiveInstances => 14;

            [L____________<Assignment, RequestJob>]
            struct Idle { }

            [____________R<Executing, (JobManifest, ToolingSpecs)>]
            [____________R<Idle, WaitCommand>]
            struct Assignment { }

            [l____________<Telemetry>]
            [L____________<Idle, JobComplete>]
            [____________R<Stopped, EmergencyStop>]
            struct Executing { }

            [L____________<Idle, ManualOverride>]
            struct Stopped { }
        }

        // Multi-instance asset sync
        interface AssetSync : Actor {
            int MaxActiveInstances => 14;

            [L____________<UpdateCheck, CheckUpdates>]
            struct Start { }

            [____________R<Downloading, NewFirmware>]
            [____________R<End, UpToDate>]
            struct UpdateCheck { }

            [l____________<ChunkAck>]
            [____________r<FileChunk>]
            [L____________<End, DownloadComplete>]
            struct Downloading { }
        }
    }
}
```

---

### Actor Lifecycle

The runtime engine automatically enforces the following rules:

- **Allocation Limits:** Exceeding `MaxActiveInstances` **closes the network connection**. Standard swarms configured with `UNLIMITED` and all RPC
  Shorthand Actors bypass these checks completely.
- **Timeouts:** If a `[ReceiveTimeout]` or `[TransmitTimeout]` is reached, the **network connection is closed** by default to prevent hangs.
- **`End` State:** Transitions to `org.unirail.Meta.End` **delete the actor pair**, freeing instances while keeping the physical connection open for
  other actors.
- **`Close` State:** Transitions to `org.unirail.Meta.Close` trigger a **graceful connection shutdown** - the transmission queue is fully drained
  before the physical link closes.
- **Customization:** All default lifecycle actions can be overridden in generated code for custom error-handling or recovery strategies.
- **User Responsibility:** Everything outside explicit limits and terminal states is the **developer's responsibility** to manage.

---

## States

States represent the distinct processing phases in an Actor's lifecycle. They define which messages are valid and which logic should execute.

The actor's **initial state** is the **topmost declared state with at least one transitional branch** (`L____________` or `____________R`) - the first
station the actor *can be in* and *can leave*. Name it clearly (e.g., `Start` or `Handshake`).

A state declared with **only non-transitional branches** (`l____________`, `____________r`, `_____lr_____`) is **not a station** the actor occupies -
it
is a *global overlay* that is always active alongside whichever linked state is current (see the **"Always Active" rule** below). For that reason,
**a stateless state cannot serve as the initial state**: the actor never enters or leaves it. Place stateless overlays anywhere in the Actor - their
position has no effect on the entry point.

States are declared as C# **empty `struct`s** inside the Actor interface, with branches controlling their behavior.

> [!IMPORTANT]
> **States are empty.** A `struct` used as a State does not contain fields or methods. If the parser detects any members inside the struct, it
> triggers a compilation error. All communication logic is expressed exclusively via branches (routing attributes) on the struct.

> [!NOTE]
> The state machine is purely event-driven (packet transmission and timeouts). AdHoc generates all state-transition code from your dataflow
> description. You only need to integrate the generated code and add your custom business logic.

The code generator collects all states, resolves links via branch targets, and traverses the state graph starting from the initial state. A
compilation error is raised if the generator detects duplicate state names or multiple independent state chains.

Branch targets are not restricted to the local Actor or Connection. You can reference a state defined in an entirely different Actor and Connection.
In such cases, the parser performs a graph traversal and **copies the referenced state**-including all subsequent links and branches-directly into
the current Actor's flow. This mechanism allows you to build modular, reusable FSM blocks (e.g., standard error-handling or teardown sequences) that
can
be seamlessly grafted across multiple Actors.

---

### Built-in Terminal States

**`org.unirail.Meta.End`**
Deallocates the current actor instance. The linked actor pair is deleted, but the underlying physical connection remains open for other actors.

**`org.unirail.Meta.Close`**
Gracefully terminates the physical connection. The transmission queue is fully drained before the link is shut down.

---

### State Attributes

Limit how long an actor may wait in a state using built-in timeout attributes (values in seconds):

- `[ReceiveTimeout(seconds)]` - maximum time to wait for an incoming message
- `[TransmitTimeout(seconds)]` - maximum time to send an outgoing message

You may also add **any custom attributes** to states or actors. The code generator preserves them and makes them available as constants or
static fields in the generated code. Use this to attach routing tags, UI labels, or any application-specific metadata directly to your protocol FSM.

---

## Branches (Routing Attributes)

On each **State**, **branches** define the data flow and assign host roles. Branches are strictly either **transitional** or **non-transitional**, and
are declared using routing attributes on the state struct.

By declaring a transitional branch for a specific host, you assign that host the **Main** role, granting it sole authority to advance the FSM. The
opposite host acts as a **Follower**, limited to executing non-transitional branches. If a state contains only non-transitional branches, neither host
holds the Main role, and **both sides are equal**.

| Attribute (with PACKS)                | Attribute (no PACKS)           | Host Side | Type                 | Effect                                                   |
|:--------------------------------------|:-------------------------------|:----------|:---------------------|:---------------------------------------------------------|
| `[L____________<Target, PACKS>(...)]` | `[L____________<Target>(...)]` | **Left**  | **Transitional**     | Left (as **Main**) sends packs, FSM moves to `Target`.   |
| `[l____________<PACKS>(...)]`         | `[l____________(...)]`         | **Left**  | **Non-transitional** | Left sends packs; FSM **stays** in current state.        |
| `[____________R<Target, PACKS>(...)]` | `[____________R<Target>(...)]` | **Right** | **Transitional**     | Right (as **Main**) sends packs, FSM moves to `Target`.  |
| `[____________r<PACKS>(...)]`         | `[____________r(...)]`         | **Right** | **Non-transitional** | Right sends packs; FSM **stays** in current state.       |
| `[_____lr_____<PACKS>(...)]`          | `[_____lr_____(...)]`          | **Both**  | **Non-transitional** | Either side sends packs; FSM **stays** in current state. |

Where `(...)` stands for the optional constructor parameters: `(KeepDoc, SkipDoc, KeepName, SkipName)`.

The constructor takes four optional named parameters: `string KeepDoc = ""`, `string SkipDoc = ""`, `string KeepName = ""`, and
`string SkipName = ""`. The `Doc` filters are regex patterns applied to each pack's documentation text (a unified pool of `///` class comments and
Dashboard line text). The `Name` filters are regex patterns applied to each pack's full type name (namespace + name). `Keep` retains only matching
packs; `Skip` removes matching packs. All use `|` as OR separator.

Since all parameters are optional with defaults, you can use C# named parameter syntax to set only the ones you need, or pass empty strings
positionally:

```csharp
// Named - skip straight to KeepName
[l____________<@Project>(KeepName: @"\.V2\.")]

// Positional - empty strings for KeepDoc and SkipDoc
[l____________<@Project>("", "", @"\.V2\.")]
```

**How pack collection works:**

The `PACKS` generic parameter contains type expressions where the `@` prefix controls how each type is interpreted:

- **Without `@`** - the type is a **direct pack inclusion**. It is added to the branch unconditionally.
- **With `@` prefix** - the type declares a **SCOPE**. The KeepDoc/SkipDoc/KeepName/SkipName filters are applied **exclusively** over this scope to
  select packs. When `@` scopes are present, the filters are confined to those scopes only - they do not additionally scan all packs.

Both direct packs and filter-selected packs are **merged** into the final packet set for the branch.

**Without PACKS generic** - the KeepDoc/SkipDoc/KeepName/SkipName filters apply over the scope of **all packs** in the project.

**Examples:**

```csharp
// 1. Direct packs only - no filtering, packs listed explicitly
[l____________<(Monitoring.Sessions.ForPeriod, Monitoring.Sessions.ForRange)>]
struct SessionMetrics { }

// 2. KeepDoc filter over ALL packs (no PACKS generic)
[l____________("📈")]
struct SimpleMetrics { }

// 3. KeepDoc + SkipDoc over ALL packs
[l____________("📈 | server", "⛔")]
struct FilteredMetrics { }

// 4. KeepName filter over ALL packs - by type name regex
[l____________(KeepName: @"\.Telemetry\.")]
struct TelemetryOnly { }

// 5. Mixed - direct packs + scoped filtering with KeepDoc/SkipDoc
//    StringMessage is included directly (no @)
//    @WarningEvent and @ErrorEvent are SCOPEs - filters applied over their contents
[l____________<(StringMessage, @WarningEvent, @ErrorEvent)>("critical", "deprecated")]
struct MixedBranch { }

// 6. Scoped filtering with KeepName - filter by type name within @Project
[l____________<@Project>(KeepName: @"\.V2\.")]
struct V2Only { }

// 7. Combined KeepDoc + SkipName on a scope
[l____________<@Project>("📈", SkipName: @"Test")]
struct MetricsNoTests { }

// 8. Transitional, KeepDoc over all packs - advances to NextState
[L____________<NextState>("handshake")]
struct Start { }

// 9. Transitional with direct packs - advances to NextState
[L____________<NextState, (ClientHello, ClientVersion)>]
struct StartWithPacks { }
```

### RPC Markers (Shorthand Call/Return)

For simple interactions, method-signature-style interfaces synthesize a 2-state FSM:

- `public interface L____________ { }` : Call from Left, **Return from Right**.
- `public interface ____________R { }` : Call from Right, **Return from Left**.

> [!IMPORTANT]
> **Packet ID Uniqueness & The "Always Active" Rule**
>
> In an Actor, the outcome of a transmission is recognized and differentiated *solely* by the packet ID on the wire. Because of this, **a specific
> packet type transmission can only be declared once for a specific host within the active FSM context.**
>
> **Non-transitional (isolated) states are Always Active.**
> States declared with only `l____________`, `____________r`, or `_____lr_____` never change the Actor's state. They are not stations the actor
> occupies - they are *global overlays* layered on top of whichever linked state is currently active. The actor never "enters" or "leaves" them; their
> handlers are simply available all the time.
>
> The dispatcher's **active scope** while the actor is in any linked state X is therefore:
>
> > **Active scope = X's own branches ∪ every isolated state's branches** (per host side)
>
> The same packet ID cannot appear twice within this combined scope on the same host side - even if both occurrences are non-transitional (Stay)
> branches. Two handlers would fire for the same wire packet, and the dispatcher has no way to choose between them.
>
> - **Singleton Actor** (only non-transitional states; no linked chain): every state is always active simultaneously, so **every packet across every
    state must be strictly unique** for each host side.
> - **Stateful Actor** (one linked chain plus zero or more isolated states): for each linked state X, the union of X's branches and every isolated
    state's branches forms one flat dispatch table per host side; duplicates between them are an error.
>
> **Need the same data to trigger different outcomes?**
> If you need the same payload information to trigger different effects (e.g., a normal update that stays in state vs. a final update that triggers a
> transition), do not declare the same packet twice. Instead, create a new packet using [AdHoc Protocol packet inheritance](#inheritance) to generate
> a new Pack with a unique Packet ID carrying identical fields:
>
> ```csharp
> // Original data packet
> class StatusPayload { string Message; }
>
> // New packet ID, exact same payload structure
> class FinalStatusPayload : StatusPayload { }
>
> // Option A: Direct pack references (no filtering needed)
> [l____________<StatusPayload>]                          // ID 1: Stay in state
> [L____________<DoneState, FinalStatusPayload>]          // ID 2: Transition
> struct ProcessingState { }
>
> // Option B: KeepDoc filtering (using documentation text from comments or Dashboard line)
> // In Dashboard:
> // <see cref = 'StatusPayload' />      status
> // <see cref = 'FinalStatusPayload' /> final_status
> [l____________("status")]                    // ID 1: Stay in state
> [L____________<DoneState>("final_status")]   // ID 2: Transition
> struct ProcessingState { }
> ```

> [!IMPORTANT]
> **Trap: Broad inclusions in isolated states overlapping with the linked chain**
>
> A common pattern is to declare a global "background" overlay that lets one host send any pack of a host or scope as a fire-and-forget:
>
> ```csharp
> interface ChannelH : Connects<DeviceA, DeviceB> {
>     // 1. Linked chain - explicit per-state Stays on the right side
>     [L____________<Configure, EmptyPack>]
>     [____________r<EmptyPack2>]                              // Stay
>     struct Handshake { }
>
>     [L____________<Active, (SerialControl, UseVarPack)>]
>     [____________r<(SerialControl, EmptyPack)>]              // Stay
>     struct Configure { }
>
>     [L____________<Close, TestBoolAndEmpty>]
>     [____________r<Person>]                                  // Stay
>     struct Maintenance { }
>
>     // 2. Global overlay - wants to allow ANY HTest pack from the right host as fire-and-forget
>     //    BAD: pulls EmptyPack, EmptyPack2, SerialControl, Person back in - collides with the chain.
>     [____________r<(DeviceB, @HTest)>]
>     struct GlobalAck { }
> }
> ```
>
> Because `GlobalAck` is always active, while the actor is in `Handshake` the right-side dispatch table is `{EmptyPack2}` ∪ `{every HTest pack}` - and
> `EmptyPack2` lands in both. The compiler rejects this with an FSM ambiguity error.
>
> Note that both occurrences may be non-transitional (Stay). That does **not** make the conflict harmless: the isolated state is always active
> *alongside* the linked state, so both Stay handlers would receive the same packet. The same kind of conflict is reported when an actor has no
> linked chain at all (Singleton) - every non-transitional state is simultaneously active, so a packet declared in two of them collides for the same
> reason.
>
> **Resolution: subtract the chain-owned packs from the broad inclusion with `X<>`:**
>
> ```csharp
> // GOOD - overlay explicitly excludes everything the chain has claimed on this host side.
> [____________r<(DeviceB, @HTest, X<(EmptyPack, EmptyPack2, SerialControl, Person)>)>]
> struct GlobalAck { }
> ```
>
> **Mental model:** declare the explicit transitions and per-state Stays in the chain first; a global overlay using a broad inclusion
> (`<Host>`, `<@Project>`, `<@SomeScope>`) must `X<>`-exclude every pack the chain owns on that host side. Apply this independently to **each** host
> side that uses a broad inclusion - `l____________` (Left), `____________r` (Right), and both halves of `_____lr_____`. Per-side filtering matters:
> it is fine for `EmptyPack` to be claimed by the chain on the Right and simultaneously be part of the Left overlay, because the two sides have
> separate dispatch tables.

---

### Multi-Path Transitions

For states with multiple possible outcomes (e.g., Success/Failure), use multiple transitional branches on the same state, each targeting a different
state:

**The "Decision" pattern:**

```csharp
// Using direct pack references:
[____________R<VaultOpen, (AccessGranted, LimitAccessGranted)>]   // Path 1: Success
[____________R<Close, AccessDenied>]                               // Path 2: Failure
struct Evaluating { }

// Or using KeepDoc filtering:
[____________R<VaultOpen>("access_granted | limited_access")]   // Path 1: Success
[____________R<Close>("access_denied")]                          // Path 2: Failure
struct Evaluating { }

// Or combined - direct packs plus KeepDoc-filtered packs merged:
[____________R<VaultOpen, (AccessGranted, LimitAccessGranted)>("extra_success_packs")]
[____________R<Close, AccessDenied>]
struct Evaluating { }
```

---

### Cross-Actor State Grafting

When defining a transitional branch, the target `STATE` does not have to be local to the current Actor or Connection. You can reference a state
defined
in a completely different Actor. When this happens, the parser **performs a graph traversal and copies the referenced state** - along with all its
subsequently linked states and branches - directly into the current Actor's flow. This lets you create modular, reusable FSM blocks (e.g., standard
error-handling or teardown sequences) that can be grafted across multiple actors.

> [!WARNING]
> **State Name Collisions during Grafting:** Every state within a single Actor must have a unique name. If a grafted state collides with an existing
> one, the parser halts with a **FSM Integrity Error** and prints a full state map identifying local vs. grafted states and their source locations.
> Resolve conflicts by renaming states in either the target Actor or the source block.

---

### Branch Examples

**Example 1: The "Baton Pass" - Swapping authority**

The "Main" role passes back and forth, ensuring only one side is in control at any given moment.

```csharp
interface SecureHandshake : Actor {

    // Direct pack references - clean and explicit
    [L____________<AwaitingChallenge, ClientHello>]
    struct Initializing { }

    [____________R<Verifying, (AuthChallenge, UpgradeRequest)>]
    struct AwaitingChallenge { }

    [L____________<Finalizing, ChallengeResponse>]
    struct Verifying { }

    [____________R<End, (Welcome, AccessDenied)>]
    struct Finalizing { }
}
```

---

**Example 2: Server-Governed Data Stream**

The Server controls state transitions. The Agent pumps data freely as a follower.

```csharp
interface TelemetryStream : Actor {

    // Mixed: direct packs for the follower stream, tags for transitions
    [l____________<(SensorData, GPSCoords)>]               // Agent streams data (direct packs)
    [____________R<Paused, PauseCmd>]                       // Server pauses (direct pack)
    [____________R<Close, Terminate>]                       // Server kills connection (direct pack)
    struct Active { }

    [____________R<Active, Resume>]
    struct Paused { }
}
```

The Server governs state transitions not because it is a "server" but because the developer designed the `Active` state that way.

---

**Example 3: Shared Authority with a Designated Finalizer**

Both sides collaborate freely, but only Left can finalize.

```csharp
interface CollaborativeEdit : Actor {
    int MaxActiveInstances => 5;

    [_____lr_____<(TextInsert, TextDelete, CursorMove)>]   // Both sides can edit (direct packs)
    [L____________<Reviewing, FinalizeDoc>]                  // Only Left can finalize
    struct Editing { }

    [____________R<Editing, (Approved, NeedsChanges)>]
    struct Reviewing { }
}
```

---

**Example 4: Cross-Actor State Grafting - Reusable Teardown**

A standard teardown sequence is defined once in `CommonFlows` and grafted into `DataSync`.

```csharp
interface CommonFlows : Actor {

    [L____________<Closed, Goodbye>]
    struct GracefulDisconnect { }

    [____________R<End, AckDisconnect>]
    struct Closed { }
}

interface DataSync : Actor {
    int MaxActiveInstances => 14;

    [_____lr_____<DataChunk>]
    // Parser copies CommonFlows.GracefulDisconnect and Closed into this FSM.
    [L____________<CommonFlows.GracefulDisconnect, SyncComplete>]
    struct Syncing { }
}
```

---

**Example 5: Entire-Project Pack Set - One-Line Full-Duplex Channel**

Reference an entire project scope as a pack set with `@Project`. Combined with `_____lr_____`, this creates a fully-duplex connection state where
both sides can freely exchange **every packet in the project** - all in a single line.

```csharp
interface GameProject {

    class UserPoint  { float X; float Y; float Z; }
    class PlayerAction { int ActionId; }
    class ServerUpdate { int Health; }
    // ... hundreds more packs ...

    struct PlayerClient : Host { }
    struct GameServer   : Host { }

    public interface GameplayCommunication : Connects<PlayerClient, GameServer> {
        interface MainActor : Actor {
            // Full-duplex channel: both sides can send any packet in GameProject.
            [_____lr_____<@GameProject>]
            struct PlayingState { }
        }
    }
}
```

- **`@GameProject`** - collects all packs declared anywhere inside the `GameProject` scope (passed as `PACKS` generic).
- **`_____lr_____`** - both `PlayerClient` (Left) and `GameServer` (Right) can send any of those packets without breaking state.
- **No tags needed** - the `PACKS` generic alone provides the full set.

---

## Real-World State Machine Diagram

The following diagram illustrates the FSM from
[`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/acfc582c971914a4a86f3458d4b85a141a787d3c/AdHocProtocol.cs#L443):

```mermaid
stateDiagram-v2
    [*] --> Start

    Start --> VersionMatching : Agent.Version

    VersionMatching --> Login : Server.Invitation
    VersionMatching --> Close : Server.Info

    Login --> LoginResponse : Agent.Login

    LoginResponse --> TodoJobRequest : Server.Invitation\nServer.InvitationUpdate
    LoginResponse --> Close : Server.Info

    TodoJobRequest --> Project : Agent.Project
    TodoJobRequest --> Proto : Agent.Proto

    Project --> Close : Server.Info\nServer.Result
    Proto --> Close : Server.Info\nServer.Result

    Close --> [*]
```

> [!WARNING]
> Short block comments such as `/*įĂ*/` contain auto-generated unique identifiers. **Never edit or duplicate them.**

---

## Modifying Imported Connections

You can customize an imported Connection and all its components without touching the original definition.

### Modification Syntax

- Replicate the target's structure with your own naming, and extend `org.unirail.Meta.Modify<TargetEntity>`.
- To **delete** entities entirely, reference them with `/// <see cref="Delete.Connection"/>-`.
- Within branches: use `X<Entity>` to delete a packet from the matched set, reference new tags to add packets, and explicitly reference the
  target State to modify its transitions.

> [!NOTE]
> Modified branches are identified by their transition target State.

### Example: Remove Specific Packets from a Branch

```csharp
[L____________<Update_to_state, @Connection>("login"), X<Agent.Login>, X<Agent.Signup>]
struct UpdateLogin : Modify<Login> { }
```

### Complete Modification Example

```csharp
interface UpdateCommunication : Modify<AdHocProtocol.Communication> {

    // Remove Server.Info from this branch's packet set.
    [____________R<@UpdateCommunication>("info_result"), X<Server.Info>]
    struct Change_Info_Result : Modify<AdHocProtocol.Communication.Info_Result> { }

    // Add a transmit timeout and introduce a new target state.
    [TransmitTimeout(30)]
    [____________R<NewState, @UpdateCommunication>("start_packs"), X<AdHocProtocol.Communication.VersionMatching>]
    struct Updated_Start : Modify<AdHocProtocol.Communication.Start> { }

    // Swap out Server.Invitation for a custom Authorizer packet.
    [____________r<@UpdateCommunication>("version_matching"), X<Server.Invitation>]
    struct UpdatedVersionMatching : Modify<AdHocProtocol.Communication.VersionMatching> { }

    // A new state introduced by the modification above.
    [l____________<@UpdateCommunication>("sending")]
    struct NewState { }
}
```

## Virtual Connections

Two hosts often have to talk but share no direct link - a browser-side **Observer** and a backend **Server** reachable
only through a **Monitoring** relay; a device behind NAT reachable only via a rendezvous broker; a sensor whose bytes
must pass through an aggregation tier.

The naive way to bridge them is to make the middle tier **re-handle every message**: deserialize the inbound pack into an
object and re-serialize it onto the outbound connection. That is costly on every axis - two full passes over the data, a
heap object proportional to the message size, and a hard **schema dependency** on a pack the relay doesn't even own, so
the relay must be rebuilt whenever that pack changes. For a 1 GB tunnel, an open-ended live feed, or a video stream
through a proxy it is a non-starter - the relay would have to buffer and understand data it only ever needed to *pass
along*.

**`VirtuallyConnects` takes the middle tier out of that burden entirely.** You declare the two endpoints as if they were
wired together; their bytes physically travel across one or more **relay hosts** that forward them **without decoding,
buffering, or even knowing their structure**. The relay moves a payload of *any* size between two connections at constant
memory cost - the most extreme application of AdHoc's [streaming](#streams) model - and stays completely decoupled from
the protocol riding through it.

> [!NOTE]
> This section builds on streaming machinery defined later in this document: chunked `[len][data]…[0]` framing and interruptibility
> ([Streams](#streams)), compression/encryption stages ([Transform chains](#transform-chains---stages-roles-and-flows)), and the `[S(N)]` resource
> cap ([Size cap](#size-cap---sn)). Skim those first if the terms are unfamiliar.

### Declaring a tunnel - `VirtuallyConnects<L, R, PATH>`

A virtual connection is declared like an ordinary connection, but with `VirtuallyConnects` in place of `Connects`:

```csharp
public interface VirtuallyConnects<L, R, PATH> : Connects<L, R>
    where L : struct, Host
    where R : struct, Host
{
    int  MaxTunnels          => 256;           // concurrent tunnels multiplexed over the path
    uint MaxStream_KiloBytes => uint.MaxValue;  // size cap per tunneled stream (KiB)
}
```

* **`L`, `R`** - the two logical endpoints (the hosts that behave as if directly connected).
* **`PATH`** - the relay host the bytes physically traverse: a single host for one hop, or a C# tuple `(H1, H2, …)` for
  a multi-hop chain.

The example from AdHoc's own protocol description:

```csharp
interface ServerToMonitoring             : Connects<Server, Monitoring>            { … }  // physical leg
interface MonitoringToMonitoringObserver : Connects<Monitoring, MonitoringObserver>{ … }  // physical leg

interface Server__MonitoringObserver     : VirtuallyConnects<Server, MonitoringObserver, Monitoring> { }
```

```
             ServerToMonitoring                     MonitoringToMonitoringObserver
 Server ───────[ Connects ]───────▶   Monitoring   ───────[ Connects ]───────▶   MonitoringObserver
    └──────────── Server__MonitoringObserver : VirtuallyConnects<Server, MonitoringObserver, Monitoring> ────────────┘
```

`Server` and `MonitoringObserver` now share a connection, even though every byte actually rides the two physical
`Connects<>` legs and is relayed by `Monitoring` in the middle.

### Transform chains over a tunnel - compress and encrypt

A tunnel **is** a chunked stream, so it carries the same [transform chains](#transform-chains---stages-roles-and-flows)
as any other stream: a virtual connection can be **compressed**, **encrypted**, or both. Decorate the
`VirtuallyConnects` interface with the very same stages (`[Zstd]`, `[ChaCha20]`, or any custom stage) and the whole
tunnel rides through that chain - the chunked tunnel is the implicit root, the stages wrap the bytes that travel it.

```csharp
[ChaCha20, Zstd(6)] interface Server__MonitoringObserver : VirtuallyConnects<Server, MonitoringObserver, Monitoring> { }
```

The attributes apply to the tunnel transport itself, so this works on an **empty-body (`{ }`) tunnel-only** connection
just as it does on a **structured** one - in both cases the stages wrap the bytes the endpoints exchange.

All the streaming rules apply unchanged - **left = wire, right = app/leaf**, so `[ChaCha20, Zstd]` is
`L → Zstd → ChaCha20 → wire` = **compress-then-encrypt** (ciphertext doesn't compress); at most **one compressor and one
cipher** per chain; key and nonce are [runtime-injected](#parameters--design-time-vs-runtime-injected) at the endpoints.

**The chain is end-to-end - the relay never sees inside it.** Stages run at `L`, the inverse stages run at `R`; every
`PATH` host only ever forwards the already-compressed, already-encrypted chunks. The [`Relay`](#relay) decodes
**none** of it - it can't, by design - so this turns schema-decoupling into genuine **end-to-end confidentiality**: an
untrusted or merely curious middle tier relays the bytes at constant memory while remaining unable to read or tamper
with the payload. Compression likewise happens once at the source and survives every hop, so the relay forwards the
smaller, compressed form.

### Tunnel-only vs. structured virtual connection

What the generator emits depends on whether the `VirtuallyConnects` interface **has a body**:

* **Empty body (`{ }`) - a tunnel only.** The generator emits just the transport: a relay on each `PATH` host and the
  tunnel endpoint API on `L` and `R`. The endpoints exchange **raw opaque bytes** through the pipe; nothing - not the
  relay, not the generated code - knows anything about the payload's structure. Use this when `L` and `R` agree on their
  own framing (or carry a foreign protocol) and only need AdHoc to move the bytes. The `Server__MonitoringObserver { }`
  above is exactly this.

* **Non-empty body - a structured connection layered on the tunnel.** When the interface declares Actors, branches, and
  packs (exactly as a normal `Connects` connection would), the generator emits the tunnel **and** the full
  structured-protocol code for that connection on `L` and `R` - the packs, actors, and state machine - whose serialized
  bytes are carried *through* the generated tunnel instead of over a direct physical link. The `PATH` relay still
  forwards opaque bytes; it never parses the structured protocol riding inside.

In both cases the tunnel is the **transport substrate**. A non-empty virtual connection is simply an ordinary end-to-end
connection that *rides* that substrate: the two endpoints speak the full protocol, the relays transport it blindly.

| Body      | Generated on `L`, `R`              | Generated on `PATH`    | Payload on the wire        |
|:----------|:-----------------------------------|:-----------------------|:---------------------------|
| `{ }`     | tunnel endpoint API (opaque bytes) | relay (`Tunnel.Relay`) | raw bytes                  |
| non-empty | full connection (packs/actors/FSM) | relay (`Tunnel.Relay`) | structured packs, tunneled |

### The path must be a real, unambiguous chain

`PATH` names the intermediate host(s) **in any order**. The generator reconstructs the strict ordered route
`L → … → R` by walking the graph of physical `Connects<>` connections, and enforces at compile time:

* **The chain must exist** - every hop (`L→PATH₁`, …, `PATHₙ→R`) must be backed by a real physical `Connects<>`. A gap
  is an error.
* **At least one intermediate** - an empty `PATH` is rejected (that would just be a physical connection).
* **Each host once** - `L`, `R`, and every `PATH` host must be distinct.
* **`L ≠ R`** - the two endpoints must differ.
* **Unambiguous** - if the listed hosts admit more than one physical route from `L` to `R`, the generator refuses and
  asks you to disambiguate, so the relay path is always deterministic.

Because order is irrelevant, `VirtuallyConnects<A, D, (C, B)>` and `VirtuallyConnects<A, D, (B, C)>` are identical as
long as `A→B→C→D` is the only physical chain through those hosts.

### The two knobs

| Property              | Default         | Meaning                                                                                                                                                                                                                                  |
|:----------------------|:----------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `MaxTunnels`          | `256`           | How many independent tunnels may be multiplexed over the path at once. Sets the on-wire width of the **`tunnel_id`** routing key: `1` ⇒ no `tunnel_id` (a single one-to-one tunnel); larger ⇒ a 1–4-byte little-endian key. Must be ≥ 1. |
| `MaxStream_KiloBytes` | `uint.MaxValue` | Maximum size of one tunneled stream, in KiB - the relay rejects anything larger, the same resource guard as [`[S(N)]`](#size-cap---sn) on a stream field.                                                                                |

Override either by re-declaring it in the interface body:

```csharp
interface DeviceLink : VirtuallyConnects<Device, Cloud, Broker>
{
    int  MaxTunnels          => 1;      // a single dedicated tunnel - no tunnel_id on the wire
    uint MaxStream_KiloBytes => 4_096;  // cap each tunneled stream at 4 MiB
}
```

### What the generator produces

From one `VirtuallyConnects` declaration the generator wires all three roles:

* **On each relay host in `PATH`** - a pooled **[`Relay`](#relay)** plus a routing
  [custom code injection point](#injection-points). The relay receives the tunnel on its inbound physical leg
  and re-emits it on the outbound leg without decoding it; your code in the injection point maps each `tunnel_id` to the
  destination connection. A multi-hop `PATH` gets one `Relay` per relay host, chained.
* **On the endpoints `L` and `R`** - either the tunnel endpoint API (empty body) or the full structured connection
  (non-empty body), addressed by `tunnel_id` when `MaxTunnels > 1`.
* **In the Dashboard** - the virtual connection takes a persistent **`id`** from the same pool as transmittable packs
  (always active when included), so it appears as a first-class, taggable entity at the top of the protocol file.

### Middle-tier patterns

A host sitting between a producer and its consumers plays one of two roles, and the two need opposite machinery. Decide which one you have before
reaching for either:

#### Pattern A — pump: the schema-blind Relay

The middle tier only **moves** bytes between two connections - it never reads, transforms, or duplicates them. Use the generated
[`Relay`](#relay): constant memory regardless of payload size, natural TCP backpressure, and zero dependency on the payload's schema. This is what
`VirtuallyConnects` places on every `PATH` host automatically. Detailed in the next section.

#### Pattern B — pump + in-memory mirror for fan-out

The middle tier **owns the schema and must act on the content** - measure it, transcode it, filter it, or replicate one inbound stream to N live
subscribers. It consumes the stream as an ordinary stream consumer and, in the same pass, keeps an in-memory mirror of the in-flight window so a
transmit-side slicer can feed each subscriber independently - one shared copy, not N relays, and a slow subscriber never stalls the others. Worked in
detail in [Smart middle](#smart-middle---when-not-to-use-relay) below.

### Relay

The **`Relay`** (`org.unirail.AdHoc.Connection.Tunnel.Relay`) is the runtime primitive the generator places on every relay host to make a virtual
connection real -
the mechanism behind the constant-memory, schema-blind relay described above. It takes a chunked tunnel arriving on the
inbound leg and re-emits it on the outbound leg **without decoding, buffering, or even looking at the body**: bytes flow
from the inbound socket buffer to the outbound socket buffer one fill at a time, and the relay holds only constant state
regardless of tunnel size.

**On the wire** a tunnel frame is:

```
[pack-id][tunnel_id?]   [len₁][data₁]  [len₂][data₂] … [0]
```

The inbound receiver, instead of handing the pack to a deserializer, hands it to a pooled `Relay`, which:

1. reads the `pack-id` and the optional `tunnel_id`;
2. asks the router (your injection-point code) which outbound connection this tunnel belongs to, and enqueues itself on
   that connection's transmitter;
3. relays the chunked body straight through - received into the socket buffer and re-emitted onto the outbound, chunk
   after chunk until the `[0]` terminator, never assembled in full;
4. re-writes the outbound `[pack-id][tunnel_id?]` prefix so the downstream sees a well-formed frame - optionally with a
   *different* `tunnel_id` (the relay may re-key the stream to re-address it on the far side).

**`tunnel_id` - multiplexing and demux.** When `MaxTunnels > 1`, the key lets **one** physical connection carry **many**
logically independent tunnels, each routed to its own peer. The key is purely an application address - *not* a connection
or registry Id; how keys map to connections is entirely up to the router. Common schemes: one key per connection, a block
of keys per connection, or any custom lookup populated as peers connect.

**Properties.**

* **Constant memory** - one socket buffer per side; a 1 GB tunnel and a 64-byte tunnel cost the same resident memory.
* **End-to-end backpressure** - if the outbound is slow the relay stops accepting inbound chunks; the kernel buffer fills
  and TCP backpressure flows upstream to the original producer - no user-space queue, no unbounded growth.
* **Interruptible / indefinite** - inheriting chunked framing, a relayed tunnel can be aborted mid-flight or run
  indefinitely.
* **Schema-decoupled** - the relay depends only on the framing, never on the pack's fields, so the pack's schema can
  evolve without touching the middle tier.

### Smart middle - when not to use Relay

The `Relay` is the right tool only when the relay is **content-blind** - it routes opaque bytes and never needs to
read, transform, or duplicate them. The moment the middle tier must **inspect, transform, or fan one stream out to many
consumers**, the opaque relay is the wrong tool: by design it never materializes the bytes it carries. That is the
**Smart middle** - [Pattern B](#pattern-b--pump--in-memory-mirror-for-fan-out) above, fully worked.

A Smart middle does *not* forward; it **consumes** the stream as an ordinary stream consumer (a `Receiver.BytesDst` handler) and, in
the same pass, keeps an in-memory **mirror** of the bytes so it can re-emit them to N live subscribers through a
`Transmitter.BytesSrc` slicer - without re-reading any persistent store:

1. **Zero-cost-when-idle capture** - each inbound chunk is appended to the mirror only while at least one subscriber is
   attached; with none, capture is skipped and the middle behaves like a plain pump.
2. **The mirror buffer** - a single shared buffer holds the bytes currently being fanned out; one copy feeds all N
   consumers, not N independent relays.
3. **The slicer** - a `Transmitter.BytesSrc` hands each subscriber successive slices of the mirror as its socket drains, so
   a slow subscriber never stalls the others and the middle never buffers more than the in-flight window.

Use the Smart middle when the relay owns the schema and must act on the content - measure, transcode, filter, or
replicate one source to many sinks. Use the [`Relay`](#relay) when it must not: a pure routing hop that moves
bytes between connections at constant cost, decoupled from the pack's schema. The Monitoring server's `getSessionFiles` /
`VolatileInfoHandler` handlers are the canonical Smart-middle example.

---

# Attributes

Attributes communicate metadata to the code generator for optimized implementation. They can be applied to **Hosts**, **Packs**, **Fields**,
**Connections**, **Actors**, **States**.

## Built-in

Built-in attributes are **interpreted by the generator** - each one changes how a field is encoded on the wire or laid out in memory. They are a
fixed,
reserved set (the generator matches them by name); everything else you write is treated as [custom metadata](#custom). Most apply to **fields**:

| Attribute                                                  | Purpose                                                                                                                                   | Documented in                                                  |
|:-----------------------------------------------------------|:------------------------------------------------------------------------------------------------------------------------------------------|:---------------------------------------------------------------|
| `[MinMax(min, max)]`                                       | Constrain a numeric field to a range so the generator picks the smallest storage (down to bit-packing)                                    | this section                                                   |
| `[A(min, max)]` / `[V(min, max)]` / `[X(amplitude, zero)]` | Varint compression tuned to the value distribution (rare-large / rare-small / bidirectional ZigZag)                                       | [Varint Type](#varint-type)                                    |
| `[D(...)]`                                                 | Dimensions and maximum lengths for arrays, strings, maps, and sets (`+` = element/collection length, `-` = constant dim, `~` = fixed dim) | [Collection Type](#collection-type)                            |
| `[S(N)]`                                                   | Maximum size cap for a raw `Stream` / `File` conduit                                                                                      | [Streams](#size-cap---sn)                                      |
| `[ValueFor(const)]`                                        | Copy a `static` field's computed value/type into a `const` at generation time                                                             | [Constants](#constants)                                        |
| Stream stage / flow attributes                             | Declare a byte-transform chain (compression, cipher, custom stages) on a field                                                            | [Transform chains](#transform-chains---stages-roles-and-flows) |

> [!NOTE]
> Because these names are reserved, do **not** name a [custom attribute](#custom) `S`, `D`, `MinMax`, `A`, `V`, `X`, or `ValueFor` - the generator
> would interpret it as the built-in instead of carrying it through as metadata.

Built-in attributes targeting a collection's **generic parameters** use a C# attribute target: `[Key: ...]` applies to a Map/Set key,
`[Val: ...]` to a Map value. For example, `[Key: D(+30)]` caps the key length while `[Val: D(100), X]` bounds and varint-compresses the value.

A worked example. Consider a field with values in the range **400,000,000** to **400,000,193**. Storing these as `int` wastes space. Using a constant
offset of 400,000,000, the entire range fits in one byte. The `MinMax` attribute handles this automatically:

```csharp
[MinMax(400_000_000, 400_000_193)] int ranged_field;
```

The code generator determines the most efficient storage type and generates getter/setter methods to handle the offset.

For ranges under 127, the generator can further optimize by packing fields into bit storage:

```csharp
[MinMax(1, 8)] int car_doors; // Range 1–8 requires only 3 bits
```

## Custom

Custom attributes are your own metadata: you **declare** them as ordinary C# attribute classes, **apply** them to protocol entities, and the generator
transforms each applied attribute into a **hierarchy of constants** (or static fields) in the generated code, where your runtime can read them.

- For **fields**, attributes are the primary method for specifying metadata.
- For other entities (projects, hosts, packs, connections, actors, states, branches), metadata can use attributes **or** constants directly - the two
  are equivalent (see [the constant-container form](#custom) below).

### Declaring a custom attribute

A custom attribute is a plain C# class deriving from `System.Attribute`. Follow the standard C# conventions - the generator relies on them:

```csharp
// Named 'XxxAttribute' → applied as [Xxx]. The suffix is dropped at the use site.
public class ServerAttribute : Attribute {
    public ServerAttribute(string Url, string Description) { }   // constructor params → POSITIONAL metadata
}
```

The shape of the declaration controls how the attribute may be written and what constants it produces:

| Declaration technique                             | Purpose                                               | Example declaration                                                                                                                                      | Applied as                                                               |
|:--------------------------------------------------|:------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------|:-------------------------------------------------------------------------|
| **Constructor parameters**                        | Positional metadata, in order                         | `ContactAttribute(string Name, string Email, string Url)`                                                                                                | `[Contact("Grafana Labs", "hello@grafana.com", "https://grafana.com/")]` |
| **Overloaded constructors**                       | Accept several value types for one slot               | `DefaultAttribute(double v); DefaultAttribute(long v); DefaultAttribute(string v); DefaultAttribute(bool v)`                                             | `[Default(0)]`, `[Default("View")]`, `[Default(false)]`                  |
| **Public properties (with defaults)**             | Named / optional arguments                            | `class ValidateAttribute : Attribute { public string Regex {get;set;} = ""; public long MaxLength {get;set;} = -1; }`                                    | `[Validate(Regex = @"^[a-z]+$")]`                                        |
| **No parameters (marker)**                        | A pure on/off flag                                    | `ReadOnlyAttribute() { }`                                                                                                                                | `[ReadOnly]`                                                             |
| **`[AttributeUsage(..., AllowMultiple = true)]`** | Apply the same attribute more than once on one entity | `[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] class TagAttribute : Attribute { public TagAttribute(string Name, string Description){} }` | two `[Tag(...)]` lines on one method                                     |

> [!TIP]
> `[AttributeUsage(AttributeTargets....)]` restricts where an attribute may be placed and, with `AllowMultiple = true`, lets it repeat on a single
> entity (used for `Tag`, `GlobalSecurity`, `SecurityRequirement`, `TagDefinition` and similar list-valued metadata). Omit it to allow the attribute
> anywhere, once.

Attribute declarations live in the protocol description alongside everything else - typically collected at the end of the project `interface`.

### Applying custom attributes

A custom attribute can be attached to **any** protocol entity. Positional values map to constructor parameters; `Name = value` maps to a property.
Custom attributes freely stack with each other and with [built-in attributes](#built-in) such as `[D(...)]`.

```csharp
// PROJECT (the protocol interface itself):
[Title("Grafana HTTP API.")]
[Version("0.0.1")]
[Description("The Grafana backend exposes an HTTP API ...")]
[Contact("Grafana Labs", "hello@grafana.com", "https://grafana.com/")]
public interface openapi3 {

    // PACK (class) — marker + repeated (AllowMultiple) attributes:
    [ApiKeyAuth("Authorization", "Header")]
    public class api_key { }

    [GlobalSecurity("basic", "")]
    [GlobalSecurity("Authorization", "")]   // applied twice — needs AllowMultiple = true
    public class global { }

    public class EmbeddedContactPoint {
        [ReadOnly] string? provenance;      // FIELD — marker attribute

        [D(+40)]                            // FIELD — built-in (dimension) stacked with...
        [Validate(Regex = @"^[a-zA-Z0-9\-\_]+$")]   // ...a custom attribute using a NAMED argument
        string? uid;
    }
}

// HOST (struct : Host):
[Server("/api", "")]
struct Server0 : Host { }

// ACTOR / RPC METHOD (a branch inside a connection):
interface ClientServerConnection : Connects<Client, Server0> {
    [Tag("enterprise", "These are only available in Grafana Enterprise")]
    (L____________,
        components.schemas.SearchResult,
        StandardErrors) searchResult(NoArg _);
}
```

The same rules reach **states** and **branches** as well - see [State Attributes](#state-attributes)
and [Branches (Routing Attributes)](#branches-routing-attributes).

### Equivalent constant-container form

Because every applied attribute becomes a constant, you can bypass the attribute syntax entirely and declare the same metadata as a `const` inside a
companion container. This is the manual equivalent of the `[Description(...)]` example above:

```csharp
[AttributeUsage(AttributeTargets.Struct)]
public class DescriptionAttribute : Attribute {
    public DescriptionAttribute(string description) { }
}

interface Communication : Connects<Agent, Server> {
    [Description("The state either responds with the result if successful, or an error message on failure.")]
    [____________r<@Communication>("info | result")]
    struct State { }
}
```

is equivalent to:

```csharp
struct State_Meta {
    const string Description = "The state either responds with the result if successful, or an error message on failure.";
}
```

### How the generator distinguishes built-in from custom

Understanding the matching rule prevents surprises:

1. **Name normalization.** An attribute is matched by its class name with the `Attribute` suffix appended if you omitted it - `[Validate]` and
   `[ValidateAttribute]` are the same attribute, and `[X]` resolves to `XAttribute`.
2. **Built-in names are reserved.** If the normalized name is one of the [built-in](#built-in) set (`S`, `D`, `MinMax`, `A`, `V`, `X`, `ValueFor`, the
   stream-stage/flow attributes, the routing attributes, or the `KeepName`/`KeepDoc`/`SkipName`/`SkipDoc` filters), the generator **interprets** it -
   it changes wire encoding, layout, or scoping.
3. **Everything else is custom.** Any other attribute is **not interpreted for the wire format**. The generator carries it through and materializes it
   as constants/static fields attached to the entity, so your runtime can read the metadata. This is why a custom attribute never affects how bytes
   are
   serialized - it is pure, passenger metadata.

> [!TIP]
> A custom attribute's argument values must be **compile-time constants** (like any C# attribute argument). To attach a *computed* value, route it
> through a `static` proxy field with [`[ValueFor]`](#constants).

# Fields

## Implementation Management

An entity's **implementation kind** - concrete or abstract - is not a global property of that entity. It is decided independently for each
**(host, language, entity)** combination: every host generates its own code for a pack or field, and does so separately for each target language.

* `+` **concrete** - a fully materialized object: fields are parsed and stored, then handed to your code with full random access.
* `-` **abstract** - the generator emits an abstract base class; as the parser reads the data off the wire it invokes methods on your implementation,
  and the whole object is never allocated.

By default a pack or field takes its host's [implementation modifier](#modifier-summary-table) for the language being generated. Adding an explicit
rule **pins exactly one (host, language, entity) combination**: on *this* host, in *this* language, only *this* pack or field is generated with the
chosen kind. Nothing else moves - the same entity on another host, the same entity in another language, and every other entity keep their own
defaults. A single pack can therefore be a concrete object on one host and an abstract stream consumer on another.

**Where it is declared**

Implementation is configured **only from a host's doc-comment** (a `struct … : Host`, or a `struct … : Modify<Host>`), using the scoping rules
described in [The Configuration Scoping System](#the-configuration-scoping-system). You never tag the pack's or field's own comment - instead you open
a language scope and *reference* the target by name or path:

```csharp
<see cref='InCS'/>-                 // open a confined C# scope: ABSTRACT implementation
<see cref='Pack'/>                  // ...applied to a whole pack (prefix @ to include its nested packs)
<see cref='Pack.field'/>            // ...or to a single field
```

A field reference is written exactly like a pack reference; the generator distinguishes them automatically from the C# symbol the `cref` resolves to.

> [!NOTE]
> A `<see cref='Pack.field'/>+` / `-` placed on a **pack** is a different feature - [selective field import / injection](#field-injection) - not
> implementation configuration. Implementation configuration is read only from hosts.

**Resolution Precedence**

Within a single **(host, language)**, implementation resolves at three levels. For any pack or field, the **most specific** level that names it wins:

| Level               | Declared by                                                            | Applies to                                        |
|:--------------------|:-----------------------------------------------------------------------|:--------------------------------------------------|
| **Host default**    | a language marker with **no** following targets                        | every pack/field in the host not overridden below |
| **Pack / Pack Set** | a language marker + a **type** reference (`<see cref='Pack'/>`)        | the packs in that scope                           |
| **Field**           | a language marker + a **field** reference (`<see cref='Pack.field'/>`) | that single field                                 |

A language marker that **has** targets is *confined* - it applies only to its listed targets and does **not** change the running default. A marker
with **no** targets *becomes* the new default for every entity that follows it.

> [!NOTE]
> A field rule is more specific than a pack rule: if a pack is configured concrete but one of its fields is explicitly abstract, only that field is
> abstract - the rest of the pack stays concrete.

Carve out a single field as abstract inside an otherwise-concrete pack:

```csharp
/**
    <see cref='InCS'/>                  // C# default for this host: ++ (concrete, materialized objects)

    <see cref='InCS'/>-                 // confined scope: ABSTRACT implementation...
    <see cref='Frame.samples'/>         // ...applied to this one field only
*/
struct Router : Host {

    public class Frame {
        long            timestamp;      // concrete - stored, random-access
        int             channel;        // concrete - stored, random-access
        [D(+1_000_000)] int[] samples;  // ABSTRACT - delivered to your handler as it streams in,
                                        //            never materialized into the Frame object
    }
}
```

For C#, `Frame` is generated as an ordinary object - `timestamp` and `channel` are stored fields with full random access - but `samples` is generated
abstract, so a multi-million-element array never has to be held in memory at once.

> [!NOTE]
> For a **field** target, the operative modifier is the **first** character - the implementation strategy (`+` concrete / `-` abstract). The second
> character (hash/equals) is a property of the whole pack object and has no field-level effect.

> [!NOTE]
> Valid configuration targets are **Packs, Pack Sets, Hosts, Projects, or Fields**. Referencing any other kind of entity after a language marker is
> rejected at build time.

For unbounded or interruptible payloads, prefer a dedicated [`Stream`](#streams) field; field-level abstract targeting is the general mechanism for
making *any* field event-driven rather than stored.

## Numeric Types

AdHoc supports all C# numeric primitives except `decimal`:

| Type     | Range                                                   |
|:---------|:--------------------------------------------------------|
| `sbyte`  | −128 to 127                                             |
| `byte`   | 0 to 255                                                |
| `short`  | −32,768 to 32,767                                       |
| `ushort` | 0 to 65,535                                             |
| `int`    | −2,147,483,648 to 2,147,483,647                         |
| `uint`   | 0 to 4,294,967,295                                      |
| `long`   | −9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| `ulong`  | 0 to 18,446,744,073,709,551,615                         |
| `float`  | ±1.5 × 10⁻⁴⁵ to ±3.4 × 10³⁸                             |
| `double` | ±5.0 × 10⁻³²⁴ to ±1.7 × 10³⁰⁸                           |

### longJS

TypeScript's `number` type can only safely represent integers in the range **−2⁵³ + 1 to 2⁵³ − 1** (
see [SAFE_INTEGER](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number/SAFE_INTEGER)). Values outside this range
require [BigInt](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt), which is less efficient.

If your field's values fall within the safe integer range, prefer `longJS` or `ulongJS` over `long` or `ulong` when communicating with TypeScript
hosts.

## Constants

Fields declared as `const` or `static` using primitive types, strings, or arrays of these types.

- **`static` fields:** Can be assigned a value or a calculated expression using any available C# static functions.
- **`const` fields:** Can be used as attribute parameters. Must use C# compile-time expressions and cannot call static functions.

To work around the limitations of `const` fields, use the `[ValueFor(const_constant)]` attribute on a proxy `static` field. The generator assigns the
value and type from the `static` field to the corresponding `const` at code generation time.

```csharp
[ValueFor(ConstantField)] static double value_for = Math.Sin(23);

const double ConstantField = 0; // Result: ConstantField = Math.Sin(23)
```

## Optional Fields

Optional (nullable) fields are declared with a trailing `?` (e.g., `int?`, `byte?`, `string?`). They are allocated in memory but transmit only a
single bit when empty, optimizing transmission size. Reference types (embedded packs, strings, collections) that are not inside a collection are
always treated as optional.

```csharp
class Packet
{
    string user;          // Reference types are always optional
    string[] tags;        // The collection field is optional; items are not
    string?[] emails;     // The collection field and items are both optional
    uint? optional_field; // Optional uint
}
```

For optional fields with primitive types, AdHoc attempts to encode the empty value efficiently by default. Override this by declaring the field as
non-nullable and specifying a "treat as empty" value via a custom attribute:

```csharp
[AttributeUsage(AttributeTargets.Field)]
public class IgnoreZoomIfEqualAttribute : Attribute
{
    public IgnoreZoomIfEqualAttribute(float value) { }
}

[IgnoreZoomIfEqual(1.1f)]
float zoom;
```

## Value Layers

The AdHoc generator uses a 3-layer approach for field values:

| Layer | Description                                                                                                     |
|:------|:----------------------------------------------------------------------------------------------------------------|
| exT   | **External type.** The representation required for external consumers (matches language data type granularity). |
| inT   | **Internal type.** The representation optimized for storage (matches language data type granularity).           |
| ioT   | **IO wire type.** The network transmission format - transmitted as a byte stream with no language granularity.  

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/180a331d-3d55-4878-8dfe-794ceb9297f3)

For a field with values from 1,000,000 to 1,080,000, shifting at the `exT ↔ inT` layer yields no memory savings in C# or Java due to fixed type
quantization. However, subtracting 1,000,000 before transmission (`ioT`) reduces the data to 3 bytes - restored on receipt by adding 1,000,000 back.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0b8f90cc-aafc-4923-8c90-1fed53775bb3)

Data transformation at `exT ↔ inT` is often redundant; the meaningful optimization happens at `inT ↔ ioT`.

Note that when a field's data type is an enclosed array (such as keys in a `Map` or `Set`), repacking data into different array types during
transitions can be costly and impractical.

## Varint Type

For numeric fields with randomly distributed values spanning the full type range, compression is typically inefficient. However, when values cluster
within a narrower range, [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding becomes highly effective - it skips
leading zero bytes and restores them on the receiving end.

Three patterns are worth recognizing:

|                                                     Pattern                                                     | Description                                                                                    |
|:---------------------------------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------|
| ![image](https://user-images.githubusercontent.com/29354319/155324344-311c6e30-fda5-4d38-b2c7-b946aca3bcf8.png) | For rare fluctuations toward larger values relative to a probable `min`, use `[A(min, max)]`.  |
| ![image](https://user-images.githubusercontent.com/29354319/155324459-585969ac-d7ef-4bdc-b314-cc537301aa1d.png) | For fluctuations in both directions relative to a probable `zero`, use `[X(amplitude, zero)]`. |
| ![image](https://user-images.githubusercontent.com/29354319/155325170-e4ebe07d-cc45-4ffa-9b24-21d10c3a3f18.png) | For rare fluctuations toward smaller values relative to a probable `max`, use `[V(min, max)]`. |

```csharp
[A]          uint?  field1;  // Optional; compressible values from 0 to uint.MaxValue.
[MinMax(-1128, 873)] byte field2; // Required; fixed range without compression.
[X]          short? field3;  // Optional; compressed using the ZigZag algorithm.
[A(1000)]    short  field4;  // Required; compressed values from -1,000 to 65,535.
[V]          short? field5;  // Optional; compressed values from -65,535 to 0.
[MinMax(-11, 75)] short field6;  // Required; uniform distribution within range.
```

## Collection Type

Collections (`arrays`, `maps`, `sets`) can store primitives, strings, and user-defined types (packs). All collection fields are optional by nature.

By default, all collections (including `string`) are limited to 255 items. Override global limits with a `_DefaultMaxLengthOf` enum:

```csharp
enum _DefaultMaxLengthOf {
    Arrays  = 255,
    Maps    = 255,
    Sets    = 255,
    Strings = 255,
}
```

Types omitted retain the default limit.

**The `[D]` Attribute: `N` vs `+N`**

The `[D]` attribute controls length limits and appears in two forms:

* **`[D(N)]`** - applies to **array fields**. `N` is the element count: the number of items in the array, list, or collection.
* **`[D(+N)]`** - applies to **non-array entities that have their own intrinsic length**, such as `string`, `Set`, and `Map`. The `+` prefix
  distinguishes the item-count limit from an array dimension. For example, `[D(+6)]` on a `string` limits it to 6 characters, while `[D(6)]` on a
  `string[]` limits the array to 6 string elements.

This distinction matters when combining the two: `[D(+50, -100)] string[,,][]` sets a string length limit of 50 (`+50`) and a constant outer dimension
of 100 (`-100`).

### Flat Array/List

Flat arrays are declared with square brackets `[]`. Three behaviors are supported:

| Declaration | Description                                                                                 |
|:------------|:--------------------------------------------------------------------------------------------|
| `[]`        | **Immutable:** Array length is constant and unchangeable.                                   |
| `[,]`       | **Fixed-at-Init:** Length is set during initialization and remains fixed (like a `string`). |
| `[,,]`      | **Dynamic:** Length varies up to a maximum (like a `List<T>`).                              |

Use `[D(N)]` to set specific field limits:

```cs
using org.unirail.Meta;

class Pack {
    string[] array_of_255_string_with_max_256_chars;
    [D(47)] Point[,] array_fixed_max_47_points;
    [D(47)] Point[,,] list_max_47_points;
}
```

### String

A `string` is an immutable array of characters, limited to 255 characters by default. Use `[D(+N)]` to impose a specific limit:

```csharp
class Packet {
    string                   string_field_with_max_255_chars;
    [D(+6)] string           opt_string_field_with_max_6_chars;
    [D(+7000)] string        string_field_with_max_7000_chars;
}
```

For frequently used string formats, use `TYPEDEF`:

```csharp
class max_6_chars_string {
    [D(+6)] string TYPEDEF;
}

class Packet {
    string             string_field_with_max_255_chars;
    max_6_chars_string string_field_with_max_6_chars;
}
```

> [!NOTE]  
> **AdHoc uses `Varint` encoding for string transmission instead of UTF-8.**
> native string → [encode] → varint bytes on wire → [decode] → native string
> <details>
> <summary><b>Why</b></summary>
>
> **Varint Encoding for Optimal Text Transmission in Framed Protocols**
>
> **Aligning Encoding with Protocol Guarantees**
>
> While UTF-8 is the standard for text in files and documents, its design is misaligned with the guarantees of a modern, framed network protocol. The
> features that make UTF-8 robust for unstructured data become redundant overhead within the structured channel of a TCP message stream.
>
> **1. Framing Makes Self-Synchronization Redundant**
>
> UTF-8's self-synchronizing byte pattern is designed for parsing corrupted or truncated streams. In a framed TCP connection, we don't operate on an
> undifferentiated byte stream - we read a length header, read exactly that many bytes, and repeat.
>
> In this model, UTF-8's self-synchronization solves a problem that no longer exists. If a byte is lost, the frame's length won't match and the entire
> frame is invalidated at the frame level. Attempting to resynchronize mid-message is an anti-pattern.
>
> **2. Better Space Efficiency for Modern Text**
>
> Varint encoding is more space-efficient than UTF-8 for characters beyond the Basic Multilingual Plane (emoji, historic scripts, specialized
> symbols).
>
> For the "Face with Tears of Joy" emoji (😂), U+1F602:
>
> * **UTF-8:** 4 bytes (`0xF0 0x9F 0x98 0x82`)
> * **Varint:** 3 bytes - a **25% reduction** per character.
>
> For purely ASCII text, the encodings are identical in size. Varint is better optimized for the full Unicode spectrum.
>
> **3. Simpler Implementation**
>
> Varint encoding/decoding is a simple loop of bitwise shifts and continuation-bit checks - typically under 10 lines per direction. A fully compliant
> UTF-8 decoder requires a complex state machine to handle multibyte sequences, overlong encoding attacks, invalid sequences, and surrogate pair
> rejection - running to hundreds or thousands of lines to be spec-correct. Varint has far fewer edge cases and is typically faster.
>
> **Conclusion**
>
> UTF-8 was designed to bring order to unstructured text streams. Within a framed binary protocol, that problem is already solved by the protocol's
> structure. By leveraging protocol-level framing guarantees, **Varint encoding is the more efficient choice for text transmission.**
> Every major binary protocol encodes string *content* as UTF-8, using varint only for the **length prefix**:
> SAD table
> | Protocol | String length | String content |
> |------------------|---------------|----------------|
> | Protocol Buffers | varint | UTF-8 |
> | MessagePack | varint | UTF-8 |
> | Cap'n Proto | varint | UTF-8 |
> | FlatBuffers | uint32 | UTF-8 |
> | Avro | varint | UTF-8 |
> | CBOR | varint | UTF-8 |
> | Thrift | int32 | UTF-8 |
> </details>

### Map/Set

`Map` and `Set` types are declared in the `org.unirail.Meta` namespace, limited to 255 items by default. Use `[D(+N)]` to impose per-field size
limits:

```csharp
using org.unirail.Meta;

[D(+20)] Set<uint>          max_20_uints_set;
[D(+20)] Map<Point, uint>   map_of_max_20_items;
```

To apply attributes specifically to `Key` or `Val` generics:

```csharp
[Key: D(+30)]           // Limit string key length
[Val: D(100), X]        // Limit integer list length
Map<string, int[,,]> MAP;

[D(+70)]                // Limit set to 70 items
[Key: D(+30)]           // Limit double list key length
Set<double[,,]> SET;
```

For complex types used in many fields, use [`TYPEDEF`](#typedef):

```csharp
class string_max_30_chars {
   [D(+30)] string TYPEDEF;
}

class list_of_max_100_ints {
    [D(100), X] int[,,] TYPEDEF;
}

Map<string_max_30_chars, list_of_max_100_ints>[,,] MAP;
```

### Multidimensional Array

A `multidimensional array` extends a flat array with additional dimensions, each with constant or fixed length, defined via `[D(-N, ~N)]`:

|    | Description                                                         |
|---:|:--------------------------------------------------------------------|
| -N | Length of a constant-length dimension.                              |
| ~N | Maximum length of a fixed-length dimension (set at initialization). |

> [!CAUTION]
> Note the prepended characters `-` and `~`.

```cs
using org.unirail.Meta;

class Pack {
    [D(-2, -3, -4)] int      ints;
    [D(-2, ~3, ~4)] Point   points;
    [D(-2, -3, -4)] string  strings_with_max_255_chars;
}
```

Commas inside array brackets for formatting purposes are ignored.

### Flat Array of Collection

To define a flat array of collections, use additional array brackets:

```csharp
class Packet {
    [D(-100)]          string []   [,,]    list_of_100_arrays_of_255_strings_with_max_255_chars;
    [D(+50, -100)]     string [,,] [,]     array_of_max_100_lists_of_max_255_strings_with_max_50_chars;
    [D(+50, 20, ~100)] string [,]  []      array_of_max_100_arrays_of_max_20_strings_with_max_50_chars;
}
```

### Multidimensional Array of Collection

```csharp
class Packet {
    [D(+100, -3, ~3)] string?                []    mult_dim_array_of_strings_with_max_100_chars;
    [D(+100, -3, ~3)] Map<int[,,]?, byte[,]>?[]?   mult_dim_arrays_of_map_of_max_100_items;
    [D(~3, -3)]       Map<int[], byte?>      []    mult_dim_arrays_of_max_255_maps;
}
```

## Object Type

There is no distinct `Object` type. Use the [`Binary`](#binary-type) array type instead, pre-transforming your object to binary before storage and
converting back on retrieval.

For better efficiency when only a limited set of types is expected, define a specific optional field per type:

```csharp
// Less efficient:
Binary[,] myFieldOfObjects;

// Better:
string? myFieldIfString;
ulong? myFieldIfUlong;
Response? myFieldIfResponse;
```

> [!NOTE]  
> An empty (null) field allocates **just a single bit** in the transmitting packet bytes.

## Binary Type

Use the `Binary` type from `org.unirail.Meta` to declare a raw binary array. It maps to `byte` (signed) in **Java**, `byte` (unsigned) in **C#**, and
`ArrayBuffer` in **TypeScript**.

```csharp
using org.unirail.Meta;

class Result
{
    [D(650_000)] Binary[,,] result; // Binary list, max 650,000 bytes
    [D(100)]     Binary[]   hash;   // Binary array, constant length 100 bytes
}
```

**Usage guidance:**

* **In-memory data:** Use `Binary` when data is already in RAM (a cryptographic hash, a generated thumbnail, an active memory buffer).
* **External sources (disk/database):** Use `Stream` or `File` types instead - they support **Direct Transfer**, piping bytes from the external source
  directly to the socket buffer without loading into managed memory. This reduces memory pressure, GC overhead, and redundant memory copies.
  Stream-based fields require an explicit size limit via `[S(N)]` - see [Size cap](#size-cap---sn).

| If the data is...    | Use...                             | Benefit                                                                    |
|:---------------------|:-----------------------------------|:---------------------------------------------------------------------------|
| **Already in RAM**   | `Binary`                           | Simple access to raw bytes as a native array.                              |
| **On disk / in DB**  | [`File`](#file-field-datatype)     | Optimized for known-size BLOBs; direct source-to-socket transfer.          |
| **Continuous/large** | [`Stream`](#stream-field-datatype) | Interruptible, chunked transfer; opaque forwarding without loading to RAM. |

## TYPEDEF

`Typedef` establishes an alias for a data type rather than creating a new type. When multiple fields share the same complex type, declare a `TYPEDEF`
to simplify future changes.

In AdHoc, `TYPEDEF` is a C# class containing a **single** field named `TYPEDEF`. The class name becomes an alias for that field's type.

```csharp
class max_6_chars_string {
    [D(+6)] string TYPEDEF;
}

class max_7000_chars_string {
    [D(+7_000)] string TYPEDEF;
}

class Packet {
    max_6_chars_string    string_field_with_max_6_chars;
    max_7000_chars_string string_field_with_max_7000_chars;
    [D(100)] max_7000_chars_string array_of_100_strings_with_max_7000_chars;
}
```

## Pack/Enum Type

Both `enums` and `packs` can serve as field data types. Packs can be nested and may contain self-referential fields, enabling complex interconnected
data structures.

> [!NOTE]  Nesting Depth (`_nested_max`)  
> AdHoc calculates the maximum nesting depth of every `Pack` at compile time. The runtime enforces the `_nested_max` limit even when rehydrating from
> a `FromStream`, preventing resource exhaustion attacks and enabling pre-calculated memory requirements.

Empty packs (no fields) or enums with fewer than two fields used as data types are represented as `boolean`.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company{
    /**
        <see cref = 'Client.RoomChangeResponse'                  id = '2' /> room | 🏠
        <see cref = 'Client.RoomChangeResponse.EnterRoomRequest'  id = '3' /> room | enter | 🏠
        <see cref = 'RoomInfo'                                    id = '1' /> room | info | 🏠
        <see cref = 'Server.QuitRoomResponse'                     id = '0' /> room | quit | 🏠
    */
    public interface MyProject3{
        ///<see cref = 'InJAVA'/>
        struct Server : Host{
            public class QuitRoomResponse{
                MID?     mid;
                [X] int? result;
                [X] int? rid;
            }
        }

        public class RoomInfo{
            [X] long  id;
            [X] int?  rank;
            [X] int?  tYpe;
            [X] long? cumulativeGold;
            [X] long? state;
        }

        enum RoomType{ CLASSICS = 1, ARENA = 2, }

        enum MID{
            ServerRegisterReq   = 1001,
            ServerRegisterRes   = 1002,
            ServerListReq       = 1003,
            ServerListRes       = 1004,
            ChangeRoleServerReq = 1005,
            ChangeRoleServerRes = 1006,
            ServerEventReq      = 1007,
            ServerEventRes      = 1008,
        }

        ///<see cref = 'InTS'/>
        struct Client : Host{
            public class RoomChangeResponse{
                MID?      mid;
                RoomInfo? roomInfo;

                public class EnterRoomRequest{
                    MID?     mid;
                    RoomType tYpe;
                    [X] int  rank;
                }
            }
        }

        interface MainConnection : Connects<Server, Client>{ }
    }
}
```

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/4c485e72-fea2-4886-b1aa-28444657fe71)
</details>

---

## Streams

AdHoc's serialization is **pull-based**: the runtime never holds a buffer larger than the single reusable socket buffer (user-chosen, min 256 B,
typically 1–8 KB), regardless of pack size. A stream-typed field - or a streamed pack - rides this directly: an arbitrarily large payload that **never
materializes as one contiguous buffer**; resident cost is one socket buffer plus small parser state. This enables what most binary protocols can't:

* **Unknown size at send time** - emit chunks until `[0]`; never buffer the whole payload just to learn its length (length-prefixed formats must).
* **Opaque relay** - a middle tier forwards a stream without decoding it; the chunked terminator is visible without parsing the body
  (see [`Relay`](#relay)).
* **Interruptible / indefinite** - abort mid-flight with a zero-length chunk; or run for hours with no natural "total length."

### The streaming model at a glance

Every streaming feature in AdHoc is one layer over the same pull-based core. From the application down to the socket:

```
your data
 ├─ pack fields ················ concrete (+) object or abstract (-) event-driven parse,
 │                               chosen per (host, language, pack, field)      → Implementation Management
 ├─ Stream / File fields ······· raw byte conduits piped source-to-socket,
 │                               capped by [S(N)]                              → Raw conduits
 ├─ transform chains ··········· [Cipher, Compressor] attribute stages wrap
 │                               the serialized bytes (the leaf)               → Transform chains
 ├─ ToStream / FromStream ······ chunked framing switched on per Endpoint,
 │                               so middle tiers see boundaries                → Contextual Scoping
 └─ VirtuallyConnects / Relay ·· whole connections piped through relay hosts
                                 that never decode them                        → Virtual Connections
socket buffer (user-chosen, min 256 B) — the only buffer, in either direction
```

Which mechanism fits which payload:

| The payload is...                                                    | Use...                                                              | Documented in                                                                 |
|:---------------------------------------------------------------------|:--------------------------------------------------------------------|:------------------------------------------------------------------------------|
| A bounded blob whose size is known cheaply (disk BLOB, thumbnail)    | [`File`](#file-field-datatype) field or named `File` pack           | [Raw conduits](#raw-conduits---stream-and-file)                               |
| Unbounded, live, or interruptible raw bytes (encoder feed, capture)  | [`Stream`](#stream-field-datatype) field or named `Stream` pack     | [Raw conduits](#raw-conduits---stream-and-file)                               |
| A typed pack too large to materialize on the receiving side          | abstract (`-`) implementation for that (host, language, pack/field) | [Implementation Management](#implementation-management-1)                     |
| A typed pack a middle tier must store/replay/forward without parsing | `ToStream` / `FromStream` / `Stream<To,From,T>`                     | [Contextual Scoping](#contextual-scoping---tostream-fromstream-streamtofromt) |
| Any of the above, compressed and/or encrypted                        | a transform chain (`[Zstd]`, `[ChaCha20]`, custom stages)           | [Transform chains](#transform-chains---stages-roles-and-flows)                |
| An entire conversation crossing one or more relay hosts              | `VirtuallyConnects<L, R, PATH>` + the generated `Relay`             | [Virtual Connections](#virtual-connections)                                   |

The layers compose freely: a `Stream` field can carry a chain; a `ToStream` root can carry a chain on its streamed path only; a tunnel is itself a
chunked stream and carries chains end-to-end.

---

### Raw conduits - `Stream` and `File`

Two field datatypes carry raw, untyped bytes - the source isn't a `Pack` (a file handle, socket, any byte producer), consumed as raw bytes via
`BytesDst`.

#### `Stream` field datatype

A pure, untyped binary conduit, chunked (`[length][data]…[0]`) and **interruptible** - the sender can emit the terminating `[0]` at any time.

#### `File` field datatype

Known size: a single length prefix (`[totalLength][data]`) - densest for disk BLOBs/buffers. **Not** interruptible; the receiver waits for exactly the
committed length.

#### Why chunked framing, not a single length prefix

| Type                                            | Wire format              | Knows total upfront? | Interruptible? |
|:------------------------------------------------|:-------------------------|:---------------------|:---------------|
| [`Stream`](#stream-field-datatype)              | `[len₁][data₁]…[0]`      | No                   | Yes            |
| [`File`](#file-field-datatype)                  | `[totalLen][data]`       | Yes (required)       | No             |
| `ToStream` / `FromStream` / `Stream<To,From,T>` | chunked `Stream` framing | No                   | No             |

Chunked framing costs 2 bytes/chunk (≤65 535 B payload each); in return both ends produce/consume incrementally with no agreed total, and either side
spots end-of-stream from the framing alone - the property the [`Relay`](#relay) depends on. `ToStream`/`FromStream` borrow the chunked framing for
that boundary visibility, but their payload is one complete serialized pack - delivering a truncated pack would hand the receiver a torn object, so
unlike a raw `Stream` they are not interruptible. `File`'s single prefix is denser when the total
is known cheaply (disk BLOBs, buffers). For fixed-size content (hashes, signatures) use [`Binary`](#binary-type) collections - no length prefix at
all.

#### Size cap - `[S(N)]`

`[S(N)]` applies to **fields of `Stream`/`File` type only** - the raw, unbounded conduits. It caps the bytes the receive side accepts, raising an
`IOException` past `N` - resource isolation when middle tiers forward un-auditable bytes. It is required whenever a field's datatype is
`Stream`/`File`, **independent of any transform stages layered on top**. A transform chain on a **pack-typed** field is bounded by that field's own
type and carries no `[S(N)]`. `N` must be **greater than 8**.

```csharp
[S(100_000)] File raw;                 // File field → [S] required
[S(100_000), ChaCha20] Stream secret;  // Stream field + cipher → [S] required by the datatype
```

---

### Named `Stream` / `File` packs

A class may inherit `Stream` or `File` to become a **named** conduit - a reusable type that carries its framing metadata on the wire:

```csharp
[S(100_000), Zstd] class TelemetryFrame : Stream { } // chunked, interruptible — max 100 000 bytes
[S(  4_096)]       class Thumbnail      : File   { } // single length-prefix    — max     4 096 bytes
```

**Rules for named Stream/File packs:**

* `[S(N)]` is **mandatory** — same rationale as the field datatype form.
* The pack body must have **no instance fields**. Constants and static fields are fine — they ride along as pack metadata, not as wire payload.
* A class may inherit `Stream` **or** `File`, not both — they describe incompatible wire formats.
* These packs **cannot be used as a field type** — use the `File`/`Stream` datatype directly instead. `File`-based packs **cannot carry transform
  chains** (no length-extending compression over a fixed-length prefix).

---

### Streams at runtime - the generated API

A schema-side `Stream`/`File` field never becomes a `byte[]` in your code. The generator emits **channel hooks**: your implementation hands the
runtime a standard byte channel, and the runtime moves the bytes through the socket buffer itself - pull-based on transmit, push-based on receive.

Given this schema:

```csharp
public class VideoUpload {
    long session_id;
    [S(1_000_000_000)] Stream frames;   // unbounded, interruptible conduit - capped at ~1 GB
}
```

the generated Java interface asks your implementation for a channel per side (`Stream` fields use `ReadableByteChannel`/`WritableByteChannel`;
`File` fields use `SeekableByteChannel` on both sides):

```java
public class VideoUploadImpl implements Consumer.VideoUpload {
    long session_id;

    // ---- Transmit side: the runtime PULLS. As the socket drains, the transmitter
    // reads the next chunk from this channel into the socket buffer and frames it.
    // The file is never loaded into memory.
    @Override public ReadableByteChannel __frames(AdHoc.Connection.Transmitter scope) {
        try { return FileChannel.open(Path.of("/videos/" + session_id + ".raw"), StandardOpenOption.READ); }
        catch (IOException e) { throw new UncheckedIOException(e); }
    }
    // Whether there is stream data to send at all (an absent stream is legal).
    @Override public boolean __frames_hasValue(AdHoc.Connection.Transmitter scope) {
        return Files.exists(Path.of("/videos/" + session_id + ".raw"));
    }

    // ---- Receive side: the runtime PUSHES. Each chunk is written into this channel
    // as it comes off the wire; the stream is complete when the [0] terminator lands.
    @Override public WritableByteChannel __frames(AdHoc.Connection.Receiver scope) {
        try { return FileChannel.open(Path.of("/incoming/" + session_id + ".raw"),
                                      StandardOpenOption.WRITE, StandardOpenOption.CREATE); }
        catch (IOException e) { throw new UncheckedIOException(e); }
    }
}
```

Sending is the same call as for any pack - the generated actor's sender streams the conduit through the socket buffer:

```java
Consumer.VideoUpload pack = new VideoUploadImpl();
Actor0.Upload.send(pack, connection);          // sender is generated per actor/branch
```

[Named `Stream`/`File` packs](#named-stream--file-packs) skip the pack object entirely - the generator emits a **direct sender** that takes the
channel itself:

```java
Actor0.Upload.send_TelemetryFrame(FileChannel.open(path, StandardOpenOption.READ), connection);
```

C# and TypeScript mirror the same shape with their platforms' channel/stream abstractions. In all three languages the contract is identical: you
provide a byte source or sink, the runtime owns the chunking, framing, transform chains, and the socket buffer.

---

### Transform chains - stages, roles, and flows

A stream can pass its bytes through an ordered **chain of transforms** - compression, encryption, or any byte-to-byte stage you define - before the
wire, and the inverse on arrival. The chain is pure **attributes**, and **the target keeps its own type**: the chain wraps the serialized bytes (the
*leaf*), it doesn't replace them. It declares, in the schema, the runtime stage chain (`Stream → Cipher → Compress → leaf`) - see
[Custom stages](#custom-stages) for how a stage is implemented.

**A stage is one link** - a reusable byte transform deriving from `StreamStageAttribute`. It can be applied to a **field** or to a whole **pack**:

```csharp
[ChaCha20]          MyPack payload;        // field: pack-typed — encrypt
[Zstd(6)]           Stream raw;            // field: bare Stream — compress, level 6
[ChaCha20, Zstd(6)] MyPack secure;         // field: compress, then encrypt

[Zstd]     class Telemetry { … }           // ordinary pack: transparently compressed on every transmission
[ChaCha20] class Frame : Stream { … }      // Stream pack: encrypted
```

`payload` stays a `MyPack`; its bytes are the leaf. The chunked-stream root is **implicit** - anything carrying a stage is framed as a chunked stream.

#### Where a chain may sit

| Target                                                         | Chain?                                                                                                                            |
|:---------------------------------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------|
| bare `Stream` field, or pack-typed field                       | ✅                                                                                                                                 |
| ordinary pack (compress/encrypt on transmit), or `Stream` pack | ✅                                                                                                                                 |
| `File` field or `File` pack                                    | ❌ - single length-prefix has no per-chunk framing for a stage to ride on, and a length-changing stage has no length field to grow |
| primitive / value-pack / typedef field                         | ❌ - group the data in a pack and put the chain there                                                                              |

#### Roles

Concrete algorithms derive from a role base so the generator can group them:

```
StreamStageAttribute
├─ StreamCompressionStageAttribute → ZstdAttribute      [Zstd]
└─ StreamCipherStageAttribute      → ChaCha20Attribute  [ChaCha20]
```

The role enables recognition (compressed/encrypted) and **at most one** compressor + one cipher per chain. (Only a `File` rejects a chain - **any**
stage, compressor *or* cipher - on either a field or a pack; every other target carries one.) New algorithms (`Lz4`, `AesCtr`) slot in unchanged.

#### Direction — left = wire, right = app/leaf

On transmit the right-most stage runs first; on receive the left-most. So `[ChaCha20, Zstd]` is
`pack → Zstd → ChaCha20 → wire` = **compress-then-encrypt** (the correct order - ciphertext doesn't compress). Put the cipher left of the compressor.

#### Flows — reuse

Declare a chain once as a `StreamFlowAttribute`, apply by one attribute; the field keeps its type:

```csharp
[ChaCha20, Zstd(6)]
public class CipherAndFastCompress : StreamFlowAttribute { }

[CipherAndFastCompress] MyPack payload;   // still a MyPack
```

A flow lists stages only (root implicit), never other flows.

#### Parameters — design-time vs runtime-injected

A stage's constructor(s) *declare* the params it needs. Use **AdHoc types** . It's the **value, not the type**, that sorts a param:

| Param kind       | How you declare it                                                      | Where its value comes from                           |
|:-----------------|:------------------------------------------------------------------------|:-----------------------------------------------------|
| Design-time      | a ctor default (`int level = 20`) or a value when applied (`[Zstd(6)]`) | baked into the description - shared by every use     |
| Runtime-injected | declared but **never given a value** (e.g. `Binary[,] key`)             | you supply it at runtime through the generated stage |

Any declared param you don't give a value becomes a runtime hook - **whatever its type** (you may use a plain non-nullable type and just leave it
unassigned; you needn't make it nullable, the generator does that for you). Buffer-shaped runtime params (`Binary[,]` keys/nonces) can't be
attribute arguments, so put them in their own constructor overload - the generator reads every constructor's params. Each declared param is emitted
on **both** sides; the impl decides usage (zstd `level` is encoder-only - the decoder ignores it).

#### Built-in stages

Shipped fully implemented; you don't write the codec (a cipher still needs its key/nonce injected at runtime):

| Stage                      | Role        | Notes                                                                                                                                         |
|:---------------------------|:------------|:----------------------------------------------------------------------------------------------------------------------------------------------|
| `[Zstd]` / `[Zstd(level)]` | compression | Zstandard; `level` 1–20 (default 20), encoder-only; not over `File`.                                                                          |
| `[ChaCha20]`               | cipher      | Stream cipher (keystream XOR): byte-incremental, resumable, no padding, encrypt == decrypt; key+nonce runtime-injected; native to C#/Java/TS. |

A *stream* cipher fits a chunked stage precisely because it's keystream-XOR - any chunk size, no block alignment, resumable, same op both ways. A
block mode (CBC/GCM) would fight it.

#### Custom stages

To add your own byte-stream transform, declare a new attribute deriving from a role base
(`StreamCompressionStageAttribute` / `StreamCipherStageAttribute`) or from `StreamStageAttribute` directly, give its constructor the parameters the
stage needs (in [AdHoc types](#binary-type)), and apply it like a built-in. The generator emits **one** file per stage type, named after the
attribute (`MyStage.cs` / `.java` / `.ts`) - a pass-through, with [injection points](#injection-points) where you drop the encode/decode
transform (hand-rolled or a library call). The file is shared by every chain that uses the stage, and regeneration preserves your code.

```csharp
public class Lz4Attribute : StreamCompressionStageAttribute    // custom compressor - one design-time param
{
    public Lz4Attribute(int level = 1) { }
}

public class AesCtrAttribute : StreamCipherStageAttribute      // custom cipher - runtime key/iv
{
    public AesCtrAttribute() { }                               // the form you apply:  [AesCtr]
    public AesCtrAttribute(Binary[,] key, Binary[,] iv) { }    // declares the runtime-injected params
}

[Lz4(3)]         MyPack p;   // design-time: level = 3
[AesCtr]         MyPack q;   // key + iv supplied at runtime
[AesCtr, Lz4(3)] MyPack r;   // a chain: compress, then encrypt
```

---

### Contextual Scoping - `ToStream`, `FromStream`, `Stream<To,From,T>`

**Why this exists - packs have no length prefix.** An AdHoc pack is a pack-id plus its fields back-to-back, with **no overall length header** - only a
parser walking every field knows where it ends. That density is why AdHoc beats length-prefixed formats on the wire, but it means a middle tier
**can't relay, store, or skip a pack without parsing it**. `ToStream` / `FromStream` / `Stream<To,From,T>` *opt a pack into* chunked `[len][data]…[0]`
framing so anyone in the middle can recognize boundaries (count chunks to `[0]`), extract/persist/replay the bytes, rehydrate (`FromStream`), or
forward them to a downstream consumer ([`Relay`](#relay)) - all without decoding.

The same field can need framing on one path and not another. **Contextual Scoping** makes a field's behavior depend on the **Endpoint** it travels.

#### Defining Endpoints

An Endpoint is a *source* (who sends) over a *pipe* (which connection); group them into **Endpoint Sets**:

```csharp
// IfSendingFrom<fromHost, viaConnection>
public interface FromProducer : IfSendingFrom<Producer, ProducerToRouterConnection> { }
public interface ExternalTraffic : _<(FromProducer, FromRouter)> { }   // a set
```

#### Channel Asymmetry

`ToStream`/`FromStream` break sender/receiver symmetry on the named Endpoint:

* **`ToStream<E, T>`** - originating at `E`, the **sender** serializes `T` as a pack; the **receiver** gets **raw bytes** (opaque sink).
* **`FromStream<E, T>`** - originating at `E`, the **sender** emits **raw bytes** (e.g. a file handle); the **receiver** rehydrates `T`.

The proxy literally can't depend on the pack's schema - on its side the field is just bytes. **`Stream<To, From, Pack>`** combines both: `ToStream`
on the `To` path, `FromStream` on the `From` path, a normal nested pack elsewhere.

Assume **Endpoint E** is `IfSendingFrom<HostA, ConnectionAB>`:

| Field Declaration          | Sender (at E)             | On-the-Wire     | Receiver (from E)         | Other Routes |
|:---------------------------|:--------------------------|:----------------|:--------------------------|:-------------|
| `MyPack p;`                | `MyPack` object           | Standard AdHoc  | `MyPack` object           | Same         |
| `ToStream<E, MyPack> p;`   | `MyPack` object           | Raw Bytes       | Raw Bytes (`ExtBytesDst`) | Normal Pack  |
| `FromStream<E, MyPack> p;` | Raw Bytes (`ExtBytesSrc`) | Standard AdHoc  | `MyPack` object           | Normal Pack  |
| `Stream<E, E2, MyPack> p;` | Depends on path           | Depends on path | Depends on path           | Normal Pack  |

These are **conditional chunked roots** - [transform stages](#transform-chains---stages-roles-and-flows) may stack on them, applying only on the
streamed path (the plain-pack fallback carries no chain).

---

## DateTime

AdHoc provides three strategies for handling time, from standard convenience to highly optimized compression. All time values are normalized to *
*milliseconds** for transmission.

### 1. Standard `DateTime`

For general-purpose fields where bandwidth is not critical.

* **Cost:** Fixed **8 bytes** (64 bits).

```csharp
class Pack
{
    DateTime createdAt;
}
```

---

### 2. Absolute Time (`DateTimeDef`)

Use `org.unirail.Meta.DateTimeDef` for long-term records anchored to a fixed point in history - birth dates, registration timestamps, audit logs.

* **Mechanism:** `Value = (ActualTime - MinAnchor) / Precision`
* **Alignment:** Bit-level - AdHoc allocates the exact bits needed to cover the range.

```csharp
public interface DateTimeDef
{
    DateTime min       { get; } // Anchor point. Default: 0
    DateTime max       { get; } // End of range. Default: DateTimeOffset.MaxValue.LocalDateTime
    TimeSpan precision { get; } // Step size. Default: TimeSpan.FromMinutes(1)
}
```

```csharp
struct RegistrationDate : DateTimeDef
{
    public DateTime min => new DateTime(2020, 1, 1);
    public TimeSpan precision => TimeSpan.FromMinutes(1);
    // 'max' is calculated automatically by AdHoc based on available bits
}

class UserProfile
{
    RegistrationDate joinedAt;
}
```

#### Optimization: Managing Spare Bits

AdHoc calculates the bits required to cover the defined range (e.g., 13 bits). Since data is stored in bytes, there are typically spare bits (e.g., 3
spare bits in a 2-byte container). Two options for how spare capacity is used:

**1. Range Expansion (default):** Precision stays fixed; spare bits extend `max` further into the future. You get a longer lifespan than requested.

**2. Enhanced Precision (prefix with `@`):** Precision floats finer (down to 1ms minimum); `max` adjusts just enough to make the range divisible by
the new precision. You keep the time window close to what you requested but with higher fidelity.

```csharp
public @TimeSpan precision => TimeSpan.FromSeconds(1);
```

---

### 3. Relative History (`TimeSpanDef`)

Use `org.unirail.Meta.TimeSpanDef` for cyclic data where "age" matters more than specific date. Defines a **History Window** relative to "now." Suited
for real-time telemetry, sensor ring buffers, and session activity tracking.

```csharp
namespace org.unirail.Meta
{
    public interface TimeSpanDef
    {
        TimeSpan interval  { get; } // History window. Default: TimeSpan.FromDays(1)
        TimeSpan precision { get; } // Step size. Default: TimeSpan.FromSeconds(1)
    }
}
```

#### The "Boundary Crossing" Problem

In a cyclic system, network latency can cause a packet to arrive after the cycle boundary has rolled over. For example, a 24-hour cycle:

* **Sender at 23:59:59:** Sends `time: 0` (start of current cycle).
* **Receiver at 00:00:01 (2 seconds later):** The cycle has rolled over. Receiver interprets `time: 0` as the start of the *new* cycle - a **24-hour
  time jump error**.

#### The Solution: 1-Minute Protection Gap

AdHoc reserves an extra **1 minute** of capacity beyond the requested `interval`:

* Physical capacity = `Interval` + `1 Minute`.
* When the packet arrives after rollover, the decoder can mathematically determine the offset belongs to the previous cycle.

> **Usage constraint:** Always operate on dates within the range of `now()` and `EarliestValidDate` (a calculated property exposed by AdHoc).

---

**AdHoc Optimization Strategy for `TimeSpanDef`** (3-step calculation):

**Step 1: Byte allocation.** Find the smallest byte count to hold the requested interval ticks.

**Step 2: Ensure the gap fits.** If spare capacity is less than 1 minute, add a byte.

**Step 3: Refine precision.** Use the spare byte capacity to improve precision:

* New precision = `(Interval + 1 Minute) / Container Capacity`
* The interval floats slightly to align with the new precision.

**Example - CPU Monitor, last 27 hours with 1s precision:**

* 27 hours = 97,200 ticks. Requires 3 bytes (2²⁴ = 16,777,216).
* Spare space far exceeds 60 ticks - gap fits.
* Total coverage: 97,260,000 ms ÷ 16,777,216 steps = ~6 ms precision.
* Result: 3 bytes, upgraded from 1s to ~6ms precision automatically. All runtime calculations use integer arithmetic.

---

### 4. Elapsed Time (`Duration`)

Use `org.unirail.Meta.Duration` for fields that measure **how long something took** - a non-negative elapsed duration from zero up to a known maximum.
Suited for request latency, task runtimes, timeout intervals, and heartbeat periods.

* **No calendar anchor** - unlike `DateTimeDef`, it carries no fixed origin point in history.
* **Non-cyclic** - unlike `TimeSpanDef`, it is linear and never rolls over, so no boundary-crossing protection is needed.
* **Mechanism:** `Value = ElapsedTime / Precision`, encoded as a step count in `[0, max]`.
* **Sizing:** Byte-level - AdHoc allocates the smallest whole-byte container (1 to 7 bytes) required for your requested `max` step count. **Note:**
  AdHoc automatically scales the actual operational `max` up to completely fill the allocated bytes.

```csharp
public interface Duration
{
    long     max       { get; } // Upper bound in steps of precision.
                                // AdHoc strictly caps this at JS MAX_SAFE_INTEGER: (1L << 53) - 1.
                                // This guarantees lossless cross-platform compatibility using plain
                                // Numbers, avoiding the performance overhead of BigInt entirely.
                                // Any provided value above this limit is automatically clamped.
                                // Default: (1L << 53) - 1 

    TimeSpan precision { get; } // Granularity of one step. Evaluated as total milliseconds.
                                // Values are clamped to a minimum of 1 ms, and an upper
                                // limit of JS MAX_SAFE_INTEGER: (1L << 53) - 1.
                                // Default: TimeSpan.FromSeconds(1)
}
```

**Example:**

```csharp
struct RequestLatency : Duration
{
    public long     max       => 30_000;                    // Requires 2 bytes (UInt16). 
                                                            // AdHoc expands the actual max to 65,535.
    
    public TimeSpan precision => TimeSpan.FromMilliseconds(19); // 19 ms per step.
                                                                // Expanded limits allow up to ~20 minutes 
                                                                // of duration (65,535 steps * 19 ms).
}

class ApiCall
{
    RequestLatency latency;
}
```

#### Spare Bits: `max` Expansion

After byte allocation, unused bit capacity is turned into a **larger representable range**: AdHoc silently increases `max` so that every bit in the
allocated bytes is used. Precision is never altered - the step size you declared stays fixed, and you simply get headroom beyond what you explicitly
requested.

> **Example - task runtime, up to 3 600 steps of 1 s (1 hour):**
> * 3 600 steps requires ⌈log₂ 3 601⌉ = 12 bits → **2 bytes** (65 536 capacity).
> * Spare capacity: 65 536 − 3 601 = **61 935 steps**.
> * Effective `max` promoted to **65 535 steps** → ~18.2 hours at 1 s precision.
> * Wire cost: still 2 bytes.

### Summary Table

| Feature           | `DateTimeDef` (Absolute)             | `TimeSpanDef` (Relative) | `Duration` (Elapsed) |
|:------------------|:-------------------------------------|:-------------------------|:---------------------|
| **Concept**       | Linear timeline                      | Cyclic ring buffer       | Linear elapsed time  |
| **Anchor**        | Fixed date (`min`)                   | Floating (`now`)         | Zero (no anchor)     |
| **Sizing**        | Bit-level                            | Byte-level               | Byte-level           |
| **Safety**        | Clamps to range                      | 1-minute protection gap  | None needed          |
| **Spare space**   | Extends `max` or refines `precision` | Refines `precision`      | Extends `max`        |
| **Latency error** | Immune                               | Protected by gap         | Immune               |

---

## Meta

[Downloads](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases)

* Ask questions and share ideas.
* Engage with other community members.

[AdHoc Agent and general forum](https://github.com/AdHoc-Protocol/AdHoc-protocol/discussions)  
[TypeScript generator forum](https://github.com/AdHoc-Protocol/InTS/discussions)  
[Java generator forum](https://github.com/AdHoc-Protocol/InJAVA/discussions)  
[C# generator forum](https://github.com/AdHoc-Protocol/InCS/discussions)  
C++ generator forum  
RUST generator forum  
Swift generator forum  
GO generator forum

# Third-Party Dependencies

1. **[Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/4.12.0-1.final)**  
   .NET Compiler Platform ("Roslyn") support for the C# language.

2. **[Microsoft.OpenApi.Readers](https://www.nuget.org/packages/Microsoft.OpenApi.Readers/2.0.0-preview2)**  
   Reading OpenAPI (Swagger) documents.

3. **[Cytoscape](https://js.cytoscape.org/)**  
   JavaScript library for visualizing networks and graphs, used in the Observer.