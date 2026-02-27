# AdHoc: Multi-Language Binary Protocol Code Generator

Performance should never be an afterthought.

![image](https://user-images.githubusercontent.com/29354319/204679188-d5b0bdc7-4e47-4f32-87bb-2bfaf9d09d78.png)

When your solution components need to communicate efficiently across different programming languages and platforms, manually coding data serialization
and deserialization becomes a
significant challenge. This process is inherently slow, prone to errors, and rapidly grows more complicated as your system scales, new languages are
added, or existing data structures
require modification.

This is precisely where **Domain-Specific Languages (DSLs) for protocol description** excel. By using a declarative language to define your data
structures, message types, and communication
protocols once, you can automatically generate consistent, high-performance implementation code for any supported language. This approach offers
substantial benefits:

* **Reduced Development Time:** Eliminate tedious manual coding of serialization logic.
* **Fewer Bugs and Compatibility Issues:** Generated code is inherently more consistent and less prone to human error across different platforms.
* **Consistent Implementation:** Ensure uniform data handling and protocol adherence throughout your distributed system.
* **Easier Maintenance and Updates:** Protocol changes can be made in one place (the description file) and propagated automatically to all language
  implementations.

Many established frameworks leverage this powerful paradigm, including:

- [**Swagger/OpenAPI**](https://swagger.io/docs/specification/data-models/): Defines RESTful APIs, enabling documentation and code generation.
- [**Protocol Buffers**](https://developers.google.com/protocol-buffers/docs/overview): A compact binary serialization format with schema evolution.
- [**Cap’n Proto**](https://capnproto.org/language.html): High-performance, zero-copy serialization with RPC capabilities.
- [**FlatBuffers**](http://google.github.io/flatbuffers/flatbuffers_guide_writing_schema.html): Memory-efficient serialization for zero-copy access.
- [**ZCM**](https://github.com/ZeroCM/zcm/blob/master/docs/tutorial.md): Real-time, low-latency messaging for structured data.
- [**MAVLink**](https://github.com/mavlink/mavlink): Lightweight messaging for drones and robotics, focused on efficiency.
- [**Thrift**](https://thrift.apache.org/docs/idl): A cross-language serialization and RPC framework.
- [**Apache Avro**](https://avro.apache.org/docs/1.8.2/idl.html): Schema-based serialization for big data, supporting dynamic typing.

However, through careful evaluation, we identified opportunities to enhance existing solutions, particularly for scenarios demanding the utmost in
binary data efficiency and application-specific
protocol control. This led to the development of **AdHoc Protocol** — a next-generation code generator designed to meet these demands.

AdHoc currently supports **C#, Java, and TypeScript**, with planned expansion to C++, Rust, and Go. It seamlessly handles the translation between
binary data streams and structured objects ("packs")
in your application, making high-performance cross-language communication effortless.

## Why Choose AdHoc?

The AdHoc code generator is specifically crafted for **data-oriented applications** that require high performance and efficient handling of structured
binary data, whether for network communication or custom storage formats.
Its design prioritizes fast data throughput with minimal resource consumption. Unlike traditional frameworks that may require buffering entire
messages in memory, **AdHoc is built on a streaming architecture.** This allows your application to process data in small, manageable chunks,
dramatically reducing memory usage and enabling the efficient handling of messages of any size—even those larger than available RAM. This allows you
to serve more users or process more data on the same hardware.

### 1. Best Fit: Data-Intensive Applications

AdHoc is particularly well-suited for systems where data volume, speed, and efficiency are paramount:

- **Financial Trading:** Managing real-time, high-frequency market data with minimal latency.
- **Customer Relationship Management (CRM):** Processing large datasets of customer interactions and transactions efficiently.
- **Enterprise Resource Planning (ERP):** Handling high-volume, real-time data updates in logistics, inventory, and operations.
- **Scientific & Industrial Data Acquisition:** Collecting, exchanging, and monitoring massive volumes of binary sensor data for factory automation,
  engineering analysis, and scientific research.
- **Game Servers:** Facilitating low-latency, real-time communication and state synchronization for multiplayer online games.
- **IoT Systems:** Processing extensive sensor data streams with high throughput and low latency demands.
- **Real-Time Analytics Platforms:** Analyzing vast amounts of streaming data (logs, sensor data) where speed is critical.
- **Streaming Media Services:** Delivering high-quality audio/video streams with low latency.
- **Telecommunications Systems:** Managing high-volume call routing, message delivery, and network state monitoring.
- **Autonomous Vehicles:** Processing data from numerous sensors and communication systems rapidly for decision-making.
- **Network Communication for Applications and Microservices:** Ensuring efficient, seamless data exchange in distributed systems.
- **Custom File Storage Formats:** Creating application-specific binary file formats optimized for data retrieval and storage efficiency.

### 2. Performance Benefits

Leveraging structured binary protocols generated by AdHoc provides significant performance advantages:

- **Drastically Reduced Memory Usage:** AdHoc's **core streaming parser** processes network data in small, reusable buffers (at least 256 bytes). This
  eliminates the need to allocate memory for the entire message at once, preventing memory spikes and allowing you to handle very large payloads with
  a minimal memory footprint.
- **Lower GC Pressure and Higher Concurrency:** By avoiding large, single-object allocations and reusing small buffers, AdHoc significantly reduces
  the workload on the garbage collector. This leads to lower latency, fewer pauses, and higher, more predictable throughput.
- **Efficient Serialization/Deserialization:** The streaming model means that data transformation happens on-the-fly, translating directly into lower
  end-to-end latency.

### 3. When to Consider Other Solutions

While powerful, AdHoc Protocol may not be the optimal choice for all scenarios:

- **Primarily Text-Based or Content-Oriented Applications:** For systems like blogs, content management, or document storage where human-readable
  formats are acceptable or beneficial, standard formats like JSON or XML are often sufficient and simpler to implement.
- **Simple or Low-Performance Use Cases:** If data volume, speed, and resource efficiency are not critical requirements, traditional data formats
  might be more straightforward to implement and maintain.

## AdHoc Protocol Key Concepts

AdHoc Protocol provides a rich set of features and building blocks to define sophisticated binary protocols and application communication flows.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a15016a6-ac05-4d66-8798-4a7188bf24c5)

The **AdHoc** generator offers a comprehensive set of features:

- Use C# to describe the protocol data structure in a familiar and intuitive way.
- Each entity can import(inherit) or subtract(remove) properties of others, allowing for flexible composition.
- Projects can be composed of other projects or selectively import specific components, such as **connections**, constants, or individual packs.
- **Connections** can be constructed from connections or their components, such as stages or branches.
- Packs can import or subtract individual fields or all fields of other packs.
- Provides a [`custom code injection point`](#custom-code-injection-point), where custom code can safely be integrated with the generated code.
- Provides built-in visualization tools through the **AdHoc Observer**, which can render interactive diagrams of network topology, pack field layouts,
  and data flow state machines.
- Support for bitfields.
- Handling of nullable primitive data types.
- If all the field data of a pack fits within 8 bytes, it will be represented as a `long` primitive, thereby
  reducing garbage collection overhead.
- Support for data types like strings, maps, sets, and arrays.
- Allows multidimensional arrays with constant/fixed/variable dimensions as field types.
- Supports nested packs and enums.
- Handles both standard and flags-like enums.
- Fields can use enum and reference to a pack data types.
- Defines constants at both host and packet levels.
- The system handles pack circular references and supports multiple inheritance.
  Additionally, reused entities can be modified to meet the specific requirements of the new project.
- Implements compression using the [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding algorithm.
- Generates fully functional code ready for use in network infrastructure.
- **Built-in Streaming Parser:** Processes all incoming data in small, reusable buffers (e.g., 256 bytes). This powerful architecture means **buffer
  allocation for the entire packet is never required**, enabling efficient handling of large messages and minimizing memory consumption.

The **AdHoc Code Generator** is a [**SaaS**](https://en.wikipedia.org/wiki/Software_as_a_service) platform that provides cloud-based code generation
services.

First, you'll need a personal [UUID](#uuid).
Why UUID?
The use of a UUID, rather than a login and password, allows users to automate code generation and embed the **AdHocAgent** utility into their code
delivery process.

To start using the AdHoc code generator, follow these steps:

1. Install .NET.
2. Install a **C# IDE** such as **[Intellij Rider](https://www.jetbrains.com/rider/)**,
   **[Visual Studio Code](https://code.visualstudio.com/)**, or **[Visual Studio](https://visualstudio.microsoft.com/vs/community/)**.

---

3. Install [7-Zip Compression](https://www.7-zip.org/download.html), a utility for optimal PPMd compression of source files. Download the [**24.07
   version or higher !**](https://youtu.be/i5L9xEk_adw) for your platform:
	
	- **[Windows](https://www.7-zip.org/a/7zr.exe)**  
	  Add `C:\Program Files\7-Zip` to the system `PATH`, and ensure the `7z` command works in the console.
	
	- **[Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)**
	  
	  ```shell
	  apk add p7zip
	  ```
	
	- **[macOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)**
	    ```
		brew install p7zip
		```
4. Download the source code of
   the [AdHoc protocol metadata attributes Meta.cs file](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/master/src/Meta.cs).  
   Alternatively, add a dependency on the `AdHocAgent.dll` to your protocol project.

![image](https://github.com/user-attachments/assets/76298dca-1f8c-4b88-855b-080ead6ad0d7)

5. Add a reference to the `Meta` in your AdHoc protocol description project.  
   ![image](https://github.com/user-attachments/assets/c91a05fe-3eff-4106-880f-e17f0e6b12de)
6. Compose your protocol description project.
7. Use the **[AdHocAgent](https://github.com/cheblin/AdHocAgent)** utility to upload your project to
   the server and obtain the generated code for deployment.

# AdHocAgent Utility

AdHocAgent is a command-line utility designed to streamline your project workflow. It facilitates:

1. Uploading your task
2. Downloading generated results
3. Deploying your project
4. Visualizing your project structure as a diagram
5. Uploading `.proto` files to convert into AdHoc protocol description format
6. Retaining and updating your user `UUID`

It accepts the following input:

The first argument is the path to the file with a task.

The file extension and path determines the task type:

---

## `.cs`

Upload the `protocol description file` to generate source code.
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/7d5181a3-3642-4027-9c3d-aed3ad4b1f5d)

 </details>

## `.cs?`

This command launches the **AdHoc Observer**, a powerful, web-based tool for visualizing, analyzing, and documenting your protocol definitions. It
connects via WebSocket to receive live protocol data and renders it as a series of interconnected diagrams.

Example:

```cmd
    AdHocAgent.exe MyProtocol.cs?
```

The Observer is an integrated development environment for your protocol, allowing you to:

* **Visualize High-Level Architecture:** See all hosts, the packs they handle, and the communication **connections** linking them in a clear,
  interactive graph.
* **Drill into Data Flow Logic:** Right-click a **connection** to open a detailed pop-up view of its state machine, including all stages and branching
  logic.
* **Inspect Data Structures:** Left-click a pack to instantly view its fields, data types, and nested structures in a dedicated diagram.
* **Annotate and Document:** Double-click the background to create, edit, and save rich-text "stickers" (notes) directly on the diagrams.
* **Navigate with Ease:** Use a searchable, collapsible tree view in the sidebar to quickly find and focus on any host, pack, or **connection**.
* **Persist Your Workspace:** All layout customizations (node positions, pan, zoom) and annotations are automatically saved.

> **[See the full Observer User Guide](./Observer.md) for a detailed explanation of all features.**

![image](https://user-images.githubusercontent.com/29354319/232010215-ea6f4b1e-2251-4c3a-956d-017d222ab1e3.png)

![image](https://github.com/user-attachments/assets/565a76c2-58f3-4570-9ca8-c6bad41f4f43)

> [!NOTE]    
> To enable navigation from the Observer to your source code, specify the path to your local C# IDE in the `AdHocAgent.toml` configuration file.

### Saving Your Workspace (Layouts and Annotations)

The Observer automatically saves your workspace, including diagram layouts and annotations (stickers), into a dedicated folder.

* **Location:** The data is saved in the current working folder of AdHocAgent.
* **Manual Save:** To save the current state of your diagram, open the sidebar and select **"Save Diagram"**.
* **Recovery:** If you accidentally close the browser without saving, the Observer creates an `current_working_folder/unsaved` folder. You can move
  these files to the `current_working_folder` to recover your work.

![image](https://github.com/user-attachments/assets/d2482a1b-5058-4903-920e-ef5dbf252ef6)

## `.proto` or path to a folder

Indicates that the task is converting a file or a directory of files in the [Protocol Buffers](https://developers.google.com/protocol-buffers) format
to the AdHoc `protocol description` format.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>
Example

```cmd
    AdHocAgent.exe MyProtocol.proto
```

![image](https://user-images.githubusercontent.com/29354319/232012276-03d497a7-b80c-4315-9547-ad8dd120f077.png)
 </details> 

> [!NOTE]  
> The second argument can be a path to a directory containing additional imported `.proto` files, such as [
`well_known`](https://github.com/protocolbuffers/protobuf/tree/main/src/google/protobuf) files and others.

The result of the .proto files transformation is only a starting point for your transition to the AdHoc protocol and cannot be used as is. Reconsider
it in the context of the greater opportunities provided by the AdHoc protocol.

## `.json` or `.yaml` Input

Specify that your input file is a Swagger/OpenAPI specification in `.json` or `.yaml` format. An optional second argument can be the path to the
output AdHoc protocol description `.cs` file. If the second argument is skipped, the AdHocAgent utility will output the `.cs` file next to the
provided OpenAPI file. Do not expect a perfect result from the transformation, but this is a good starting point for the transition from OpenAPI
specification to the AdHoc protocol description.

## `.md`

The provided path is the `deployment instruction file` for the embedded [Continuous Deployment](https://en.wikipedia.org/wiki/Continuous_deployment)
system.
`AdHocAgent` will only repeat the deployment process for source files that have already been received from the server.
This feature is particularly useful for debugging deployments.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/6109d22b-d4f9-43dc-8e9b-976d38d63b32)
 </details> 

> [!NOTE]  
> In addition to command-line arguments, the `AdHocAgent` utility requires the following configuration file:

- **`AdHocAgent.toml`:** This file includes essential settings for the `AdHocAgent` utility, such as:
	- The URL of the code-generating server.
	- The path to the local C# IDE binary. This allows the utility to open the IDE directly to specific source files at a specified line.
	- The path to the [7-Zip](https://www.7-zip.org/download.html) binary. `AdHocAgent` leverages 7-Zip's PPMd compression capability.
		- Download links:  
		  [Windows](https://www.7-zip.org/a/7zr.exe)  
		  [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)  
		  [MacOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)
	- The path to your preferred source code formatter binaries, including:
		- Download links  
		  [clang-format](https://releases.llvm.org/download.html)  
		  [prettier](https://prettier.io/docs/en/install.html)  Install `prettier` globally `npm install -g prettier` to ensure it is available in the
		  console as `prettier`.
		  [astyle](https://sourceforge.net/projects/astyle/files/)

The `AdHocAgent` utility will search for the `AdHocAgent.toml` file in its directory.
If the file is not found, the utility will generate a template that you can update with the required information.

## UUID

To restore or get your first `volatile` personal Authentication [UUID](https://en.wikipedia.org/wiki/Universally_unique_identifier), follow these
steps:

1. Sign in to your **GitHub** account.
2. Go to the [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up) and post a message.

After your request is processed (when the post disappears), a bot will automatically create a new **private** project for
you [here](https://github.com/orgs/AdHoc-Protocol/projects).
This project will track your code generation history and provide helpful messages with details about any issues and their resolutions.

In the project, you will find a task with your `UUID`:   
![image](https://github.com/user-attachments/assets/b1789e7e-3ca3-4442-839b-aca172babf4e)

Grab the `UUID` and run the **AdHocAgent** utility once.

```shell
AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f
```

The utility will save the `volatile UUID` in the `AdHocAgent.toml` configuration file.

> [!NOTE]  
> The utility may automatically renew your UUID during new code generation requests, so you cannot reuse it.
> To ensure consistency, retain and reuse your `AdHocAgent.toml` file where the updated UUID will be stored.
> If your UUID is rejected, you must manually repeat the process to acquire a new one.


> [!NOTE]  
> When run without arguments, the AdHocAgent utility displays the command-line help and generates a `protocol description file` template.

## Continuous Deployment (CD) System

The embedded Continuous Deployment system automates the process of deploying generated source code into your target projects. It uses a special
Markdown file, the **Deployment Instructions File**, to control exactly how and where files are copied.

Its most powerful feature is a **"Smart Merge"** capability, which preserves custom code you've written inside designated "injection points,"
preventing your work from being overwritten during updates.

### The Deployment Workflow

Here’s the typical workflow for using the deployment system:

1. **First Run & Generation:** Run the AdHocAgent utility. If it doesn't find a deployment instructions file, it will **automatically generate one for
   you** (e.g., `AdHocProtocol.md`). The process will then stop.
2. **Configure:** Open the newly generated `.md` file. This file contains a complete tree of all the source files. Edit this file to add the
   destination paths for your project folders.
3. **Redeploy:** Run the AdHocAgent utility again. This time, it will read your configured instructions and deploy the files, intelligently merging
   your custom code.
4. **Repeat:** After future code generation, simply re-run the utility to deploy the updated files. Your deployment configuration and custom code will
   be preserved.

### The Deployment Instructions File

This file is the brain of the deployment process.

* **Naming:** It must be named after the protocol description file, but with an `.md` extension. (e.g., `AdHocProtocol.cs` -> `AdHocProtocol.md`).
* **Location:** The utility searches for this file first in the directory of the protocol file, and then in the working directory.

#### Structure: The File Tree

The file contains a Markdown list representing the source directory structure. Each file and folder can be configured for deployment.

```markdown
- 📁[InCS](/path/to/source/InCS)
	- 📁[Agent](/path/to/source/InCS/Agent)
		- ＃[Agent.cs](/path/to/source/InCS/Agent/gen/Agent.cs)
		- ＃[Channel.cs](/path/to/source/InCS/Agent/gen/Channel.cs)
```

#### Configuring Deployment Targets

You specify where files go by adding extra Markdown links to the end of a line. The syntax is `[<regex_filter>](<destination_path>)`.

* The `regex_filter` is optional. If omitted (`[](/path)`), the rule applies to all files within that scope.
* The `destination_path` is the target location on your file system.

##### Target Path Behavior

The behavior of the copy operation is determined by whether the destination path ends with a path separator (`/` or `\`).

**1. Copy Contents Into a Folder (Path ends with `/` or `\`):**
This rule copies the **contents** of the source folder directly into the destination folder. The source folder itself is not created in the
destination. This is the most common rule for deploying a module's files into an existing project structure.

- **Folder:** `- 📁[Agent](...) [](/path/to/project/src/)`
	* **Result:** The files and folders *inside* `Agent` are copied directly into `/path/to/project/src/`.
	* e.g., `.../source/Agent/MyFile.cs` → `/path/to/project/src/MyFile.cs`
- **File:** `- 🌀[demo.ts](...) [](/path/to/project/components/)`
	* **Result:** The file is copied into the destination folder.
	* e.g., `.../source/demo.ts` → `/path/to/project/components/demo.ts`

**2. Copy and Rename Folder/File (Path does NOT end with `/` or `\`):**
This rule copies the source item and gives it the exact name and location specified in the destination path. This is useful for renaming a folder or
file during deployment.

- **Folder:** `- 📁[Agent](...) [](/path/to/project/RenamedAgent)`
	* **Result:** The `Agent` folder and its entire contents are copied to a new folder named `RenamedAgent`.
	* e.g., `.../source/Agent/MyFile.cs` → `/path/to/project/RenamedAgent/MyFile.cs`
- **File:** `- 🌀[demo.ts](...) [](/path/to/NewName.ts)`
	* **Result:** The file is copied and renamed.
	* e.g., `.../source/demo.ts` → `/path/to/NewName.ts`

##### Inheritance and Filtering

* **Inheritance:** Rules applied to a parent folder are automatically inherited by all its children.
* **Filtering:** You can provide a regular expression in the brackets to apply a rule only to matching files within a folder's hierarchy.

**Example:**
> [!TIP]
> Switch from Markdown preview to Markdown source to view detailed formatting.

```markdown
- 📁[Observer](/path/to/source/InTS/Observer)  ✅ Deploys all files into 'src', but images go to an 'assets' folder.
  [\.(jpg|png|gif)$](/project/assets/images/)
  [](/project/src/)
	
	- 🌀[demo.ts](/path/to/source/InTS/Observer/demo.ts)  // Inherits rule, deploys to /project/src/demo.ts
	- 📁[gen](/path/to/source/InTS/Observer/gen)          // All files inside also inherit
```

##### Skipping Files and Folders

To exclude a file or folder from deployment, add `⛔` to the line or use an empty target `[]()`.

```markdown
- 📁[Observer](/path/to/source/InTS/Observer) [](/path/to/project/src/)
	- 🌀[demo.ts](/path/to/source/InTS/Observer/demo.ts) ⛔ // This file will be skipped
	- 📁[gen](/path/to/source/InTS/Observer/gen) []()            // This entire folder will be skipped
```

#### Advanced Processing: Execution Instructions

You can run scripts or tools on source files *before* they are deployed. This is perfect for code formatting, linting, or other transformations.
Instructions are defined in code blocks and are executed in the order they appear.

##### File Path Placeholder & Root Path

* Use the `FILE_PATH` placeholder in your commands; it will be replaced with the actual path of the file being processed.
* Paths starting with `/InCS/`, `/InJAVA/`, etc., are treated as relative to the root of the source files directory.

##### Shell Execution

Execute any command-line tool.

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

Execute an in-line C# script for more complex transformations. The file path is passed as `args[0]` to `Main`.

* **Reference Assemblies:** If your script needs namespaces not available by default (like `System.Linq`), add assembly references in quotes at the
  top of the script.

**Example: Removing leading whitespace from region directives.**

```regexp
.*
```

```csharp
"System.Text.RegularExpressions" // Reference the assembly for Regex

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
        // Write back without a Byte Order Mark (BOM), which is recommended
        File.WriteAllText(filePath, updatedContent, new UTF8Encoding(false));
    }
}
```

### Preserving Custom Code: The Smart Merge Feature

This is the system's core safety feature. It ensures you can add custom logic to generated files without losing it on the next deployment.

#### Injection Points (Your Safe Zone)

An **injection point** is a special, identifiable region in a generated file where you can safely add your own code. They are marked with
language-specific region comments that include a **Unique ID (UID)**.

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
> **Never edit, move, or duplicate the `endregion` line or its Unique ID.** The UID is how the system finds your safe zone to preserve your code.
> Changing it will cause your custom code to be permanently lost.

#### Generated Blocks (Suggestions from the Generator)

Inside an injection point, you may find pre-written code snippets wrapped in special comment tags (e.g., `//❗<` and `//❗/>`). These are **Generated
Blocks**.

```csharp
#region > receiving
// Your custom code can go here.

//❗<
    // This is a generated block. You can enable or disable it.
//❗/>

// Your custom code can also go here.
#endregion > ǺÿÿČ.Project.Connection receiving
```

**How to Work with Generated Blocks:**

* ✅ **DO:** **Enable/Disable a block.** To disable it, comment out the entire block, including the start/end tags. To enable it, uncomment the entire
  block.
* ✅ **DO:** **Reorder blocks.** You can move an entire block (tags and all) within its injection point.
* ❌ **DO NOT:** **Edit the code *inside* a generated block.** Your changes will be discarded on the next deployment.
* ❌ **DO NOT:** **Modify the block markers** (e.g., `//❗<`).

#### Smart Update Notifications

The system helps you review important changes by automatically adding `//todo 🔴` comments.

* **New Active Code:** If an update adds a new, *active* generated block, you'll get a warning. This is critical because new code could change
  behavior.
  ```csharp
  //todo 🔴 New active generated code was added by the generator. Please review...
  //✅<
      callNewFunction();
  //✅/>
  ```
* **Removed Code You Used:** If a generated block that you had *enabled* is removed in an update, it won't be deleted. Instead, it will be commented
  out with a warning, preserving your logic for you to review.
  ```csharp
  //todo 🔴 The following code block was removed by the code generator. Please review.
  // //❗<
  //    callObsoleteFunction();
  // //❗/>
  ```

### Lifecycle Hooks: Before and After Deployment

You can run executables at the very beginning or very end of the entire deployment process.

```markdown
[before deployment]("C:\Program Files\dotnet\dotnet.exe" format "/InCS/MyProject")
[after deployment](/path/to/logging_script.sh --status=success)
```

### Safety Features: Automated Backups and Restoration

To protect your project from unintended consequences of a deployment, the system includes a robust, automatic backup and restore mechanism. This
provides a critical safety net, allowing you to instantly revert changes if needed.

#### How it Works

Before the deployment process overwrites any files in your target project directories, it performs the following steps:

1. **Creates a Backup Directory:** A new, unique backup directory is created in the same location as your deployment instructions file. The
   directories are named sequentially (e.g., `AdHocProtocol_1`, `AdHocProtocol_2`, etc.), making it easy to find the most recent backup.
2. **Copies Existing Files:** For every single file that is about to be updated or replaced, its current version is copied into this new backup
   directory. The files are given generic names (like `original_1.cs`, `original_2.java`) to prevent conflicts.
3. **Generates Restore Scripts:** Inside the backup directory, the system generates three scripts:
	* `restore.bat` (for Windows Command Prompt)
	* `restore.ps1` (for Windows PowerShell)
	* `restore.sh` (for Linux, macOS, or WSL)

> [!IMPORTANT]
> Only files that are being **overwritten** are backed up. If a new file is deployed to a location where no file previously existed, there is no "
> original" to back up.

#### How to Restore Your Files

If a deployment introduces a bug or an unwanted change, you can immediately roll back to the previous state.

1. **Navigate to the Backup Directory:** Open the latest backup folder (e.g., `AdHocProtocol_5`).
2. **Choose and Run the Correct Script for Your OS:**
	* **Windows:** Double-click `restore.bat` or right-click `restore.ps1` and select "Run with PowerShell".
	* **Linux/macOS:** Open a terminal in the backup directory and run the following commands:
	  ```shell
	  # Make the script executable (only need to do this once)
	  chmod +x restore.sh

	  # Run the script
	  ./restore.sh
	  ```

Running the script will copy every backed-up file from the backup folder back to its original project location, effectively undoing the deployment and
restoring your project to its exact pre-deployment state.

# Overview

The simplest form of a `protocol description file` can
be [represented as follows](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/Templates/ProtocolDescription.cs):

```csharp
using org.unirail.Meta; // Importing attributes required for AdHoc protocol generation

namespace com.my.company // The namespace for your company's project. Required!
{
    public interface MyProject // Declares an AdHoc protocol description project
    {
        class CommonPacket{ } // Represents a common empty packet used across different hosts

        /// <see cref="InTS"/>-   // Generates an abstract version of the corresponding TypeScript code
        /// <see cref="InCS"/>    // Generates the concrete implementation in C#
        /// <see cref="InJAVA"/>  // Generates the concrete implementation in Java
        struct Server : Host // Defines the server-side host and generates platform-specific code
        {
            public class PacketToClient{ } // Represents an empty packet to be sent from the server to the client
        }

        /// <see cref="InTS"/>    // Generates the concrete implementation in TypeScript
        /// <see cref="InCS"/>-   // Generates an abstract version of the corresponding C# code
        /// <see cref="InJAVA"/>  // Generates the concrete implementation in Java
        struct Client : Host // Defines the client-side host and generates platform-specific code
        {
            public class PacketToServer{ } // Represents an empty packet to be sent from the client to the server
        }

        // Defines a communication connection for exchanging data between the client and server
        interface Connection : Connects<Client, Server>{
            interface Start :
                L,
                _<
                    CommonPacket,
                    Client.PacketToServer
                >,
                R,
                _<
                    CommonPacket,
                    Server.PacketToClient
                >{ }
        }
    }
}
```

To visualize the structure of your protocol, launch the **AdHoc Observer** by appending a question mark to your protocol file path.
For example: `AdHocAgent.exe /dir/minimal_descr_file.cs?`. This command opens an interactive diagram of your protocol's architecture.

<details>
  <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/acc420a1-b2bf-4579-9ee6-5336ad155d4f)
</details>
To upload a file and get the generated source code, you can use the AdHocAgent utility by providing the path to it, for example, `AdHocAgent.exe /dir/minimal_descr_file.cs`. 
This command will upload the file and initiate the process of generating the source code based on the contents of the specified file.

# Protocol description file format

> [!IMPORTANT]  
> **The `protocol description file` follows a specific naming convention:**
>
>- Names should not start or end with an underscore `_`.
>- C# prohibits a class from having a field or nested class with the same name as the class itself.  
   > Therefore, a `Pack` cannot have a field or nested pack with the same name as the pack.
>- Names should not match any keywords defined by the programming languages that the code generator supports.
   > **AdHocAgent** will check for and warn about such conflicts before uploading.

## Project

As a [`DSL`](https://en.wikipedia.org/wiki/Domain-specific_language) to describe an **AdHoc protocol**, the C# language was chosen.
The `protocol description file` is essentially a plain C# source code file within a .NET project.

To create a `protocol description file`, follow these steps:

- Start by creating a C# project.
- Add a reference to the [AdHoc protocol metadata attributes.](https://github.com/cheblin/AdHoc-protocol/tree/master/src/org/unirail/AdHoc)
- Create a new C# source file within the project.
- Declare the protocol description project using a C# 'interface' within your company's namespace.

```csharp
using org.unirail.Meta; // Importing AdHoc protocol attributes. This is required.

namespace com.my.company // Your company's namespace. This is required.
{
    public interface MyProject // Declare the AdHoc protocol description project as "MyProject."
    {
        // Add your protocol description here
    }
}
```

> [!Note]
> In C# 10, file-scoped namespaces were introduced to simplify declarations by eliminating curly braces and reducing indentation levels.

This enhancement allows you to declare your namespace in a more concise manner:

```csharp
using org.unirail.Meta; // Importing AdHoc protocol attributes. This is required.

namespace com.my.company; // Your company's namespace. This is required.

public interface MyProject // Declare the AdHoc protocol description project as "MyProject."
{
    // Add your protocol description here
}

```

The **AdHoc protocol** not only defines the data for passing information, which includes packets and fields, but it also incorporates features to
describe the complete network topology. This entails information about hosts, connections, and their logical interconnections.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>For instance, let's consider the following `protocol description file`:</u></b></summary>

```csharp
using org.unirail.Meta; // Importing AdHoc protocol attributes is mandatory

namespace com.my.company2 // Your company namespace. Required!
{
    /**
		<see cref = 'BackendServer.ReplyInts'                      id = '7'/> // Represents the reply containing an array of integers from the BackendServer
		<see cref = 'BackendServer.ReplySet'                       id = '8'/> // Represents the reply containing a set of integers from the BackendServer
		<see cref = 'FrontendServer.PackB'                         id = '6'/> // Represents a specific packet type B sent by the FrontendServer
		<see cref = 'FrontendServer.QueryDatabase'                 id = '5'/> // Represents a database query sent by the FrontendServer
		<see cref = 'FullFeaturedClient.FullFeaturedClientPack'    id = '4'/> // Represents a data pack specific to the FullFeaturedClient
		<see cref = 'FullFeaturedClient.Login'                     id = '3'/> // Represents the login information for the FullFeaturedClient
		<see cref = 'Point3'                                       id = '0'/> // Represents a 3D point in space
		<see cref = 'Root'                                         id = '1'/> // Represents the base class for all transmittable packets
		<see cref = 'TrialClient.TrialClientPack'                  id = '2'/> // Represents a data pack specific to the TrialClient
	*/
	public interface MyProject{ //Your Project name - defines the structure and communication protocols for the entire system

        public class Root/*Ā*/{ // A non-transmittable base entity for all packets, providing common fields
            long id;       // Unique identifier for the connection
            long hash;     // Hash value for data integrity verification
            long order;    // Sequence number to maintain packet order
        }

        class max_1_000_chars_string{ // A non-transmittable typedef for strings with a maximum length of 1000 characters
            [D(+1_000)] string? TYPEDEF; // The actual string value, constrained to 1000 characters
        }

        class Point3/*ÿ*/{ // Represents a 3D point in space, potentially transmittable
            private float          x; // X-coordinate
            private float          y; // Y-coordinate
            private float          z; // Z-coordinate
            max_1_000_chars_string label; // Descriptive label for the point
        }


        //FrontendServer handles core business logic, manages data storage, and orchestrates backend operations.
        ///<see cref = 'InJAVA'/>
        struct FrontendServer/*ā*/ : Host{
            // Define packets that Server can create and send
            public class QueryDatabase/*Ą*/ : Root{
                private string? question; // The query string to be executed on the database
            }

            public  class PackB/*ą*/{ } // A specific packet type B
        }

        // BackendServer acts as the interface or entry point for clients.
        ///<see cref = 'InCS'/>
        struct BackendServer/*ÿ*/ : Host{
            public class ReplyInts/*Ć*/ : Root{
                [D(300)] int[] reply; //Array containing a maximum of 300 integers as a response
            }

            public class ReplySet/*ć*/ : Root{
                [D(+300)] Set<int> reply; //Set containing a maximum of 300 unique integers as a response
            }
        }

        ///<see cref = 'InTS'/> 
        struct FullFeaturedClient/*Ă*/ : Host{
            public class Login/*Ă*/ : Root{
                private string? login;    // User's login identifier
                private string? password; // User's password for authentication
            }

            public class FullFeaturedClientPack/*ă*/{
                max_1_000_chars_string query; // A query string limited to 1000 characters
            }
        }

        ///<see cref = 'InCS'/> 
        struct TrialClient/*ă*/ : Host{
            public class TrialClientPack/*ā*/{
                max_1_000_chars_string query; // A query string from the trial client
            }
        }

        ///<see cref = 'InTS'/>
        struct FreeClient/*Ā*/ : Host{ } // Represents a free client with limited functionality

        // Define communication connections between hosts

        interface TrialConnection/*ÿ*/ : Connects<FrontendServer, TrialClient>{
            interface Start/*ÿ*/ : L,
                              _</*ÿ*/ // Defines packets from FrontendServer to TrialClient
                                  Point3,
                                  Root,
                                  TrialClient.TrialClientPack
                              >,
                              R,
                              _</*Ā*/ // Defines packets from TrialClient to FrontendServer
                                  Point3,
                                  TrialClient.TrialClientPack
                              >{ }
        }

        interface MainConnection/*Ā*/ : Connects<FrontendServer, FullFeaturedClient>{
            interface Start/*Ā*/ : L,
                              _</*ÿ*/ // Defines packets from FrontendServer to FullFeaturedClient
                                  Point3,
                                  Root,
                                  TrialClient.TrialClientPack,
                                  FullFeaturedClient.Login,
                                  FullFeaturedClient.FullFeaturedClientPack
                              >,
                              R,
                              _</*Ā*/ // Defines packets from FullFeaturedClient to FrontendServer
                                  Point3,
                                  TrialClient.TrialClientPack,
                                  FullFeaturedClient.FullFeaturedClientPack
                              >{ }
        }

        interface TheConnection/*ā*/ : Connects<FrontendServer, FreeClient>{
            interface Start/*ā*/ : L,
                              _</*ÿ*/ // Defines packets from FrontendServer to FreeClient
                                  Point3,
                                  Root
                              >,
                              R,
                              _</*Ā*/ // Defines packets from FreeClient to FrontendServer
                                  Point3
                              >{ }
        }

        interface BackendConnection/*Ă*/ : Connects<FrontendServer, BackendServer>{
            interface Start/*Ă*/ : L,
                              _</*ÿ*/ // List of packets from FrontendServer to BackendServer
                                  FrontendServer.QueryDatabase,
                                  Point3,
                                  FrontendServer.PackB
                              >,
                              R,
                              _</*Ā*/ // List of packets from BackendServer to FrontendServer
                                  BackendServer.ReplyInts,
                                  BackendServer.ReplySet
                              >{ }
        }
    }
}
```

</details>
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>and if you observe it with AdHocAgent utility viewer you may see the following</u></b></summary>  

![image](https://github.com/user-attachments/assets/6408d113-730b-4823-82c5-74159a65c5cb)

By selecting a specific connection in the AdHocAgent utility viewer, you can view detailed information about the packets involved and their
destinations. This allows you to track the specific path taken by packets within the network.

![image](https://github.com/user-attachments/assets/04a3ae72-665b-4579-9c04-6304fdf7b991)
![image](https://github.com/user-attachments/assets/895b9268-1a06-467f-8337-7d4b14d7f87f)
</details>

Please note that after processing the file with AdHocAgent, it assigns packet ID numbers to the packets for identification and tracking.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/51163c18-3b49-4f4f-adea-c3450c0fe01c)


> [!NOTE]  
> A project can function as a [set of packs](#projecthost-as-a-named-pack-set).

### Extend other Project

You can create a protocol description project by importing enums, constant sets, connections, or other projects.

To import all components, extend the desired source projects as C# interfaces in your project's interface:

```csharp
interface MyProject : OtherProjects, MoreProjects
{
}
```

> [!NOTE]  
> The order of the extended interfaces determines priority in cases of name or pack ID conflicts. Projects listed first take precedence.

For example, the protocol description in [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs) defines
public, external connections. However, the backend infrastructure on the **Server** side often requires an internal communication protocol to handle
infrastructure-level tasks such as:

- Distributing workloads across nodes
- Transmitting and aggregating metrics
- Managing internal database records
- Implementing specialized authentication and authorization

**Options for Protocol Extension:**

1. **Create a Separate `Backend` Protocol Description**  
   This is the best choice if you do not need to pass the same packet instances across both the external and internal protocols.

2. **Extend the Existing `AdHocProtocol` Description**  
   This approach is ideal if you want to integrate both protocols within a single `Server` host.
   The example below demonstrates how to extend `AdHocProtocol` to incorporate these backend-specific details.
   
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

The `Backend` protocol description may look like this:

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
            interface One : L,
                            _<
                                MetricsData,
                                One
                            > { }
        }

        interface ConnectionToAuthorizer : Connects<Server, Authorizer> {
            interface Start : L,
                              _<
                                  AuthorisationRequest,
                                  Start
                              >,
                              R,
                              _<
                                  Authorizer.AuthorisationConfirmed,
                                  Authorizer.AuthorisationRejected,
                                  Start
                              > { }
        }
    }
}
```

In this example, the `AdHocProtocolWithBackend` protocol description imports all entities from `AdHocProtocol` and introduces several components:

* **Hosts**:
	* Two new hosts have been defined: `Metrics`, implemented in **C#**, and `Authorizer`, implemented in **Java**.
* **Packs**:
	* The `AuthorisationRequest` and `MetricsData` packs are created to be sent from the `Server` via the `ConnectionToAuthorizer`.
	* Two packs, `AuthorisationConfirmed` and `AuthorisationRejected`, are defined within the `Authorizer`. One of these will be sent as a reply to
	  the `AuthorisationRequest` from the `Server`.
* **Connections**:
	* `ConnectionToMetrics` represents the link between the `Server` and `Metrics`.
	* `ConnectionToAuthorizer` represents the link between the `Server` and `Authorizer`.

> [!IMPORTANT]
> If your solution requires working with multiple protocols, you cannot easily combine their generated protocol-processing code within the same VM
> instance due to `lib` **org.unirail** namespace clashes. To resolve this, assign each project’s `lib` to a distinct namespace.


### Selective Entities Import

AdHoc Protocol provides two methods to fine-tune the imported entities: **XML Documentation Tags** and **Generic Interfaces**.

#### By XML Documentation Tags

Use this method for simple inclusion or exclusion of specific entities directly in comments.

* **Exclude (`-`)**: Prevents an entity from being imported.
* **Include (`+`)**: Imports *only* the specified entities (Filtering).

To import only specific enums, constant sets, or connections, list them using the `<see cref="entity"/>+` attribute:

```csharp
	/// <see cref="SomeProject.Pack"/>+
	/// <see cref="FromProjects.Connection"/>+
	interface MyProject : OtherProjects, MoreProjects
	{
	}
```

> [!NOTE]  
> Note the **plus** character after the attribute to import the entity.  
> You cannot import `Stages` this way.


To exclude specific imported entities, reference them in the project's XML documentation using the [
`<see cref="entity"/>-`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute) attribute:

```csharp
/// <see cref="MoreProjects.UnnecessaryPack"/>-
/// <see cref="OtherProjects.UnnecessaryConnection"/>-
/// <see cref="OtherProjects.UnnecessaryConnection.Stage"/>-
interface MyProject : OtherProjects, MoreProjects
{
}
```

> [!NOTE]  
> The **minus** character after the attribute excludes the entity.

#### By Generic Interfaces (`_<>` and `X<>`)

Use this method for structural modification directly within the code syntax. This approach allows you to explicitly **Add (`_<T>`)** or *
*Remove (`X<T>`)** entities from the project scope.

```csharp
public interface AdHocProtocol : 
    OtherProject, 
    // Add specific components
    _<
        AdHocProtocol.Agent.Project.Host.Pack.Field.DataType
    >,
    // Remove specific components
    X<
        OtherProject.LegacyConnection
    >
{
}
```

**Supported Operations for Generic Interfaces:**

1. **`_<T>` (Add)**
	* **Connections**: Fully supported. Adds the connection to the project.
	* **Enums / Constant Sets**: Fully supported.
		* **Standard Behavior:** By default, an Enum or Constants Pack is included in a Host *only* if it is declared within that Host's body or if it
		  is referenced by a field in a Pack transmitted by that Host.
		* **Project Level (`interface Project : _<Enum>`):** Mandates the inclusion of the Enum/Constant Set in **every** Host defined in the project,
		  ensuring global availability.
		* **Host Level (`struct Host : _<Enum>`):** Mandates the inclusion of the Enum/Constant Set in that **specific** Host, regardless of whether
		  it is referenced by any fields.
	* **Hosts**: **Restricted**. You cannot add a Host directly to a project this way. Hosts must be referenced as endpoints within a **Connection**.
	* **Packs**: **Restricted**. You cannot add a standard Pack directly to a project this way. Packs must be referenced within a **Branch** of a *
	  *Stage**.

2. **`X<T>` (Remove)**
	* **Connections**: Fully supported. Removes the connection from the project.
	* **Enums / Constant Sets**: Fully supported. Removes the set from the project scope.
	* **Hosts**: **Supported (Cascading)**. Removes the host *and* automatically removes any Connection that references this host.
	* **Packs**: **Supported (Cascading)**. Removes the pack from the project *and* automatically removes it from every Stage Branch where it is used.

> [!NOTE]  
> To import a **host** from another project, reference that host as an endpoint within your project's **connections**.  
> To import a **pack** from another project, reference the pack within a branch of a stage inside your project's **connections**.

[Learn how to modify imported packs](#modify-imported-packs).  
[Learn how to modify imported connections](#modify-imported-connections).

***

## Hosts

In the AdHoc protocol, **Hosts** are the active participants in network communication, responsible for sending and receiving data packets across
logical **Channels** established over a **Connection**. A host is defined as a C# `struct` within a project's `interface` and must implement the
`org.unirail.Meta.Host` marker interface.

The AdHoc compiler generates the necessary code for a host only for the programming languages you explicitly specify. You control this code generation
using XML documentation comments (`/// <see.../>`) that define the target language and the desired implementation style for the data packets the host
will handle.

### Implementation Modifiers: Controlling Your Code Generation

When you specify a target language, you append a two-character modifier (e.g., `++`, `+-`) to control the generated code's behavior. This allows you
to choose the optimal data processing model for your application, from convenient object-oriented models to high-performance, low-memory streaming
interfaces.

The modifier consists of two independent parts:

#### First Position: Parsing Strategy (`+` or `-`)

This character determines how the host processes incoming data streams within a **Channel**.

* `+` : **Full Object Deserialization (Concrete Implementation)**
	* **How it Works:** The AdHoc streaming parser reads the entire message from the connection and constructs a complete, in-memory object on the
	  heap. All data is deserialized into the object's fields before your code can access it.
	* **Best For:** Most application and business logic. This model is the most convenient for developers, as it provides a simple, stateful object to
	  work with, pass to other methods, or store for later use.

* `-` : **Streaming Event-Based Parsing (Abstract Interface)**
	* **How it Works:** This activates a high-performance, event-driven parsing model. The generator creates an abstract base class (or interface)
	  that you must implement. As the parser reads data from the channel stream, it immediately calls methods on your implementation for each field it
	  encounters. **The full object is never allocated on the heap.**
	* **Best For:** High-throughput, low-latency applications like network routers, data loggers, or any service that must process messages larger
	  than available RAM. This model offers the lowest possible memory footprint and reduces garbage collection pressure.

#### Second Position: Hash Support (`+` or `-`)

This character controls whether methods required for using packs in hash-based collections (e.g., `HashSet`, `Dictionary`, `HashMap`) are generated.

* `+` : **Enabled**
	* **What it Does:** Generates `Equals()` and `GetHashCode()` method implementations (or signatures in abstract mode).
	* **Best For:** Scenarios where you need to store packet objects in a hash-based data structure or use them as dictionary keys.

* `-` : **Disabled**
	* **What it Does:** Skips the generation of `Equals()` and `GetHashCode()`.
	* **Why Choose This:** If you know you will not be storing packets in hash-based collections, disabling this feature reduces the total amount of
	  generated code and avoids the minor performance overhead of these methods.

---

#### Modifier Summary Table

| Modifier | Example                | **Parsing Strategy**                | **Hash Support** |
|:--------:|:-----------------------|:------------------------------------|:-----------------|
|   `++`   | `<see cref='InCS'/>++` | `+` (Full Object Deserialization)   | `+` (Enabled)    |
|   `+-`   | `<see cref='InCS'/>+-` | `+` (Full Object Deserialization)   | `-` (Disabled)   |
|   `-+`   | `<see cref='InCS'/>-+` | `-` (Streaming Event-Based Parsing) | `+` (Enabled)    |
|   `--`   | `<see cref='InCS'/>--` | `-` (Streaming Event-Based Parsing) | `-` (Disabled)   |

> **Default Behavior: `++`**
> If a language is specified without a modifier (e.g., `<see cref='InCS'/>`), it defaults to `++`, providing the most convenient object-oriented model
> out of the box.

---

### The Configuration Scoping System: Applying Rules Precisely

The generator applies these configuration rules based on a powerful, top-down scoping system. Understanding these principles is key to mastering code
generation.

* **Principle 1: No Configuration, No Code.** If a host definition does not contain a `<see.../>` tag for a specific language, no code for that host
  will be generated in that language.
* **Principle 2: Top-Down and Persistent.** The generator reads the `<see.../>` tags sequentially from top to bottom. When it encounters a language
  marker (e.g., `<see cref='InJAVA'/>--`), that rule becomes the **active rule** for that language and applies to all entities that follow it. This
  rule remains active until another rule for the *same language* is encountered.
* **Principle 3: Grouped Application.** When you list specific pack or [Pack Sets](#pack-set) immediately after a language marker, that rule is *
  *confined** and applies *only to that specific group of entities*. The previously active rule for that language is temporarily paused and then
  resumes for any entities that follow the group.

#### Recursive Scoping with the `@` Prefix

To streamline configuration, you can use the `@` prefix directly within a host's documentation tags. This acts as an **inline recursive Pack Set**.

When the generator encounters `<see cref='@Target'/>` and `Target` is not a field, it treats `@Target` as an inlined **Pack Set** and **recursively
includes all transmittable packets** found within the scope of `Target` (which can be a Project, Host, or a nested namespace/interface).

This allows you to apply specific modifiers to entire branches of your data hierarchy without manually defining a separate **Named Pack Set**.

**Example:**

```csharp
/// <see cref="InTS"/>--
/// // Apply '--' rule recursively to everything inside RootWithNestedPacks
/// <see cref="@RootWithNestedPacks"/> 
/// 
/// // Revert back to '+-' for any subsequent packs in this host
/// <see cref="InTS"/>+-
struct MonitoringObserver : Host {
}
```

#### Detailed Example Walkthrough

Let's analyze a complex configuration to see these principles in action.

```csharp
public interface MyProject
{
    // A named set of backend-related packs
    interface BackendPacksThatImplementedOnServer : 
        _<
            @Monitoring.Network,
            @Monitoring.Authorizer,
            @Monitoring.Processing
        >{ }

    /**
    // RULE 1: Set the default for C# for ALL packs in Server.
    <see cref='InCS'/>+-                        		

    // RULE 2: Start a confined group for Java. This rule applies ONLY to the next 4 items.
    <see cref='InJAVA'/> // (Defaults to ++)                       		
    <see cref='BackendPacksThatImplementedOnServer'/>		
    <see cref='ToAgent.Result'/>                		
    <see cref='Agent.ToServer.Proto'/>          		
    <see cref='Agent.ToServer.Login'/>          		

    // RULE 3: Set a NEW default for Java for ALL REMAINING packs in Server.
    <see cref='InJAVA'/>--                      		
    */
    struct Server : Host 
    { 
        // ... many other pack definitions inside Server ...
    }
}
```

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0cfa47f2-8b2e-4e49-9c7d-0fd908dbd7ce)

</details>

Here is how the generator interprets these rules:

1. **Rule 1 (`InCS+-`):** The active rule for the C# language is set to `+-` (Full Object, No Hash Support). This rule will apply to **every pack**
   defined within the `Server` host, as no other C# rules follow it.

2. **Rule 2 (`InJAVA`):** A new rule for the Java language is set to the default of `++` (Full Object, with Hash Support). Because it is followed by a
   list of specific entities, this rule is **confined** to that group. It will be applied *only* to the packs within
   `BackendPacksThatImplementedOnServer` and the three other listed packs (`ToAgent.Result`, etc.).

3. **Rule 3 (`InJAVA--`):** A new active rule for the Java language is set to `--` (Streaming, No Hash Support). This rule applies to **all other
   packs** within the `Server` host that were not part of the group defined in Rule 2.

This system gives you granular control to optimize for performance where needed (using streaming for high-volume data packs) while retaining
convenience elsewhere (using full objects for command-and-control packs).

---

#### Modifying Imported Hosts

You can alter the code generation configuration for hosts defined in other projects you import. Create a new `struct` that implements `Modify<T>`,
where `T` is the imported host you wish to change. Then, apply configuration rules as you would for a normal host.

This is useful for adapting a pre-built library to use a different parsing strategy (e.g., switching a library's packs from concrete classes to
abstract interfaces for performance).

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

A `Host` definition also implicitly acts as a named [Pack Set](#pack-set). This allows you to reference all packets defined directly within that
host's scope by simply using the host's name, which is useful for organizing and applying rules to large groups of related packets.

## Pack Set

A Pack Set is a powerful feature for grouping related packet types under a single, manageable unit. This simplifies rule application, improves code
organization, and enhances reusability.
Pack Sets are the primary mechanism for defining a Scope—the target group of packets for a rule or operation.

### In-Place Pack Sets

The `org.unirail.Meta._<> ` interface is a special utility that creates an **Ad-Hoc Pack Set**, allowing flexible grouping of packet types.
To exclude specific entities from a `PackSet`, use the `org.unirail.Meta.X<>` utility interface.

### Named Pack Sets

**Named Pack Sets** simplify the management of frequently used or recurring packets by grouping them under a single, reusable name. This improves code
readability and reduces complexity when referencing multiple packets in your project.

To define a **Named Pack Set**, use the C# interface construct. For example:

```csharp
interface Info_Result:
    _<
    	Server.Info,
    	Server.Result
    >{}
```

In this example:

- `Info_Result` is the name of the set.
- `Server.Info` and `Server.Result` are the packets that are grouped together.

**Named packet sets** can be declared anywhere within your project and may contain references to individual packs, other **Named packet sets**,
projects, or hosts. Once you have defined a **Named Pack Set**, you can reference it in your code wherever the set of packets is needed.

#### Filtering

To further refine a **Named Pack Set**, you can apply the `[Keep...]` or `[Skip...]` attributes from the `org.unirail.Meta` namespace. This allows you
to sieve the contents of a set using Regular Expressions based on the **full pack's type name** or **doc comment**.

You can **stack multiple attributes** of the same type. The logic is applied as follows:

1. **Keep Attributes (Additive OR):** If **any** `[Keep...]` attributes are present, the packet is retained **only if** it matches **at least one** of
   the patterns. If no `[Keep...]` attributes are present, all packets are initially candidates for inclusion.
2. **Skip Attributes (Subtractive OR):** The packet is removed if it matches **any** of the provided `[Skip...]` patterns.

This allows for complex filtering logic (e.g., "Keep items from Module A OR Module B, but Remove anything marked Internal OR Deprecated").

#### 1. Name Filtering (`[KeepName]` & `[SkipName]`)

These attributes filter packets based on their **full type names** (namespace + name).

* **`[KeepName]`**: Retains packets that match **any** of the provided patterns.
* **`[SkipName]`**: Removes packets that match **any** of the provided patterns.

```csharp
// Example: Select packets from  'Account' OR 'Billing' namespaces, 
// but exclude 'Test' packets and 'Draft' packets.

[KeepName(@"\.Account\.")]  // Match #1
[KeepName(@"\.Billing\.")]  // Match #2 (OR logic)
[SkipName(@"Test")]         // Remove #1
[SkipName(@"Draft")]        // Remove #2 (OR logic)
interface FinancePackets : _< @Project > {}
```

#### 2. Documentation Filtering (`[KeepDoc]` & `[SkipDoc]`)

These attributes filter packets based on their **documentation comments**. This is a powerful way to organize packets using visual tags, emojis, or
specific keywords.

* **`[KeepDoc]`**: Retains packets where the documentation contains **any** of the provided patterns.
* **`[SkipDoc]`**: Removes packets where the documentation contains **any** of the provided patterns.

**Visual Tagging Example:**
Users can include specific symbols (e.g., 📈, 🔒, ⛔) in their documentation to categorize packets, then use stacked attributes to aggregate them.

```csharp
// --- Packet Definitions ---

/// 🔒 User credentials.
interface Credentials : ... {}

/// 📈 Server performance metrics.
interface CpuStats : ... {}

/// 📈 Network throughput.
interface NetStats : ... {}

/// ⛔ Legacy payload.
interface V1Payload : ... {}


// --- Named Pack Sets ---

// 1. Dashboard Set: Collects all Metrics (📈) AND Security (🔒) items.
[KeepDoc(@"📈")] 
[KeepDoc(@"🔒")]
interface DashboardFeed : _< @Project > {}

// 2. Clean Export: Everything except Deprecated (⛔) or Internal (🙈) items.
[SkipDoc(@"⛔")]
[SkipDoc(@"🙈")]
interface PublicApi : _< @Project > {}

// 3. Specific Logic: 
// Keep items tagged with "👉" followed by "📈" (Source pointing to Graph)
// OR items tagged with "👉" followed by "👀" (Source pointing to View)
[KeepDoc(@"👉📈")]
[KeepDoc(@"👉👀")]
interface DataFlowVisualization : _< @Project > {}
```

**Note:** The filters scan the raw text of the documentation. Since modern source files are typically UTF-8, symbols and emojis are fully supported in
the regex.

### Project, Host, or Pack as a Named Pack Set

You can treat a **Project**, **Host**, or **Pack** as a **Named Pack Set** to automatically include all transmittable packets defined directly within
their scope. This approach simplifies organizing and managing large packet sets hierarchically.

```csharp
interface Info_Result:
    _<
        Server.Info,
        Server.Result,
        Project,  // Includes all transmittable packets directly within the Project's scope.
        Host      // Includes all transmittable packets directly within the Host's scope.    
    >{}
```                  

To include **all transmittable packets recursively** (including those in nested structures) within a **Project**, **Host**, or **Pack**, prefix the
reference with the `@` symbol.

> **Crucially:**  
> When using the recursive @ prefix, the container itself is excluded from the set. This prevents accidental inclusion of parent "wrapper"
> packets when you only intend to target their children.

```csharp
interface Info_Result:
    _<
        Server.Info,
        Server.Result,
        @Project,  // Includes all transmittable packets recursively within the Project. Exclude the Project itself
        Host,      // Includes all transmittable packets directly within the Host's scope. 
        X<
            Packs, // Excludes the specified packets
            Need, 
            @ToDelete 
        >
    >{}
```

## Empty packs, Constants, Enums

### Empty Packs

A **transmittable** (referenced(registered) in a connection) C# class-based pack that contains no instance fields, but various types
of [constants](#constants) or nested declarations of other packs.
Implemented as singletons, it offers the most efficient way to signal simple events or states via a connection.

> [!NOTE]  
> When constructing a pack hierarchy, you may encounter an `Empty pack` that is unexpectedly transmittable over the network. This is undesirable if
> the pack’s sole purpose is to define the hierarchy structure.
> To prevent transmission, switch to use a C# struct-based [Constants Container](#constant-container) that remains non-transmittable while still
> fulfilling its hierarchy organizational role.

### Constants Container

A **non-transmittable** C# struct-based pack that may contain various types of [constants](#constants) or nested declarations of other packs.
Declaring instance fields is not allowed.
**Constants Container** is primarily used to define the hierarchy structure and deliver metadata to generated code. It can be declared anywhere within
your project.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using System;
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject2
    {
        /**
         Flags for gimbal device (lower level) operation.
        */
        [Flags] //Flags type enum
        enum GIMBAL_DEVICE_FLAGS
        {
            GIMBAL_DEVICE_FLAGS_RETRACT    = 1, //explicitly assigned values
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
                MAV_BATTERY_FUNCTION_UNKNOWN, //implicitly autoassigned values
                MAV_BATTERY_FUNCTION_ALL,
                MAV_BATTERY_FUNCTION_PROPULSION,
                MAV_BATTERY_FUNCTION_AVIONICS,
                MAV_BATTERY_TYPE_PAYLOAD,
            }

            class ServerPack { }
        }

        ///<see cref = 'InTS'/>
        struct Client : Host //host
        {
            class Login
            {
                string user;
                string password;
                [D(DST_CONST_FIELD)] Binary[,]  hash;// Using calculated `const` field in the attribute

                //======= static fields === constatns related to Login pack
                static int      USE_ANY_FUNCTION = (int)Math.Sin(34) * 4 + 2;
                static string[] STRINGS          = { "", "\0", "ere::22r" + "K\nK\n\"KK", STR };

                [ValueFor(DST_CONST_FIELD)] //attribute SRC_STATIC_FIELD pushes the value and type to DST_CONST_FIELD field 
                private static int SRC_STATIC_FIELD = 45 * (int)Server.MAV_BATTERY_FUNCTION.MAV_BATTERY_FUNCTION_ALL + 45 >> 2 + USE_ANY_FUNCTION;
                const string STR = "KKKK";
                
                const int DST_CONST_FIELD = 0;
            }
        }
        
        struct SI_Unit //Container Pack as constants set
        {
            struct time //Container Pack as constants set
            {
                const string s   = "s";   // seconds
                const string ds  = "ds";  // deciseconds
                const string cs  = "cs";  // centiseconds
                const string ms  = "ms";  // milliseconds
                const string us  = "us";  // microseconds
                const string Hz  = "Hz";  // Herz
                const string MHz = "MHz"; // Mega-Herz
            }

            struct distance //constants set
            {
                const string km    = "km";    // kilometres
                const string dam   = "dam";   // decametres
                const string m     = "m";     // metres
                const string m_s   = "m/s";   // metres per second
                const string m_s_s = "m/s/s"; // metres per second per second
                const string m_s_5 = "m/s*5"; // metres per second * 5 required from dagar for HIGH_LATENCY2 message
                const string dm    = "dm";    // decimetres
                const string dm_s  = "dm/s";  // decimetres per second
                const string cm    = "cm";    // centimetres
                const string cm_2  = "cm^2";  // centimetres squared (typically used in variance)
                const string cm_s  = "cm/s";  // centimetres per second
                const string mm    = "mm";    // millimetres
                const string mm_s  = "mm/s";  // millimetres per second
                const string mm_h  = "mm/h";  // millimetres per hour
            }
        }

        interface MainConnection : Connects<Server, Client> { }
    }
}
```

</details>

#### Distribution Over Hosts

By default, a Constants Container is included in the Host where it is declared.
However, you can override this default behavior using the `_<T>` syntax to force inclusion:

* **Project Level (`interface Project : _<EnumOrConst>`)**  
  Mandates the inclusion of the Constants Container in **every** Host defined in the project, ensuring global availability.
* **Host Level (`struct Host : _<EnumOrConst>`)**  
  Mandates the inclusion of the Constants Container in that **specific** Host.

----

### Enums

Enums are used to organize sets of constants of the same primitive type:

- Use the `[Flags]` attribute to indicate that an enum can be treated as a bit field or a set of flags.
- Manual assignment of values is not required, except when an explicit value is needed. Enum fields without explicit initialization are automatically
  assigned integer values, with the `[Flags]` attribute ensuring that each field is assigned a unique bit power of two.

> [!NOTE]
> `Enums` and all constants are replicated on every host and are not transmitted during communication. They serve as local copies of the constant
> values, available for reference within the respective host's scope.

#### Distribution Over Hosts

By default, an Enum is included in a generated Host *only* if:

1. It is declared within that Host's body (scope).
2. It is referenced by a field in a Pack transmitted by that Host.

You can override this default behavior using the `_<T>` syntax to force inclusion:

* **Project Level (`interface Project : _<EnumOrConst>`)**  
  Mandates the inclusion of the Enum in **every** Host defined in the project, ensuring global availability.
* **Host Level (`struct Host : _<EnumOrConst>`)**  
  Mandates the inclusion of the Enum in that **specific** Host, regardless of whether it is referenced by any fields.
*

### Modify Enums and Constants

Enums and constants can be modified like a [simple pack](#modify-imported-packs), **but the modifier is discarded after the modification is applied.**

---

## Packs

Packs are the smallest units of transmittable information, defined using a C# `class`. Pack declarations can be nested and placed anywhere within a
project’s scope.

The instance **fields** in a pack's class represent the data it transmits. A pack may also contain various types of [constants](#constants) or nested
declarations of other packs.

> [!NOTE]  
> A pack can be used as a [set of packs](#projecthost-as-a-named-pack-set). Keep this in mind when organizing the pack hierarchy.

### Inheritance

AdHoc Packs utilize a **hybrid composition model**. This allows you to construct complex data structures by mixing fields from various sources (
Mixins) or inheriting them (OOP).

The Core Principle: Name Occupation
The generator builds the final field list by checking sources in a strict order. **Once a field name is "occupied" (defined), any subsequent attempt
to add a field with the same name is skipped.**

Resolution Hierarchy
The order of precedence for defining a field is:

1. **Native Fields** (Highest Priority)
	* Fields explicitly written in the C# class body always win. They cannot be overwritten by XML or Inheritance.
2. **XML Documentation Includes** (`<see .../>+`)
	* Fields imported via standard XML tags.
	* Processed top-to-bottom. If two XML tags import the same field name, the first one wins.
3. **Inheritance** (`base` / `_<...>`)
	* Fields from base classes are added last.
	* If a field name already exists (from Native or XML sources), the inherited field is ignored.

---

1. XML-Driven Composition (Mixins)
   Use XML documentation to inject or remove fields *before* the generator resolves inheritance.

Syntax Reference

| Operator | Action      | Description                                                                                               |
|:--------:|:------------|:----------------------------------------------------------------------------------------------------------|
| **`+`**  | **Include** | Imports fields from the target if the name is not yet taken.                                              |
| **`-`**  | **Exclude** | **Pre-emptively blocks** a field name. Used to prevent a specific field from being imported or inherited. |

> **Note:** Exclusions (`-`) run first logic-wise. They mark a name as "blocked" so that subsequent layers (Inheritance) cannot add it.

This is the "Miracle" of AdHoc Packs. Because fields are imported via **symbolic XML references** (`<see cref="..."/>`), the Pack does not copy static
text—it creates a **live link** to the original definition.

**Single Source of Truth (SSOT)**
Your data model (the source class) is the only place definitions exist. All Update Packs are just "projections" of that model.

* **Refactoring Safe:** If you rename a field in the source (e.g., via IDE refactoring), the XML tag updates automatically. The generated Pack
  immediately reflects the new name.
* **Type Evolution:** If you change a field's type (e.g., `int` to `long`), all Packs referencing that field automatically adopt the new type.
* **Attribute Sync:** Validation attributes (e.g., `[D(+1024)]`) are inherited. You never have to update logic in multiple places.

#### Example: The "Miracle" in Action

Consider a `Player` class and a packet responsible for updating the player's score.

**Step 1: The Initial Definition**

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

**Step 2: The Evolution (Refactoring)**
Later, you realize `score` needs to be renamed to `experience` and requires a `long` (64-bit) to prevent overflow.

You simply change the **Player** class. You **do not** touch the Pack.

```csharp
class Player {
    public int id;
    
    // ✏️ CHANGE: Renamed 'score' -> 'experience' and changed type 'int' -> 'long'
    public long experience; 

    // ✅ MIRACLE: This pack is ALREADY fixed.
    // The IDE automatically updated the XML reference during the rename.
    // The Generator automatically pulls the new 'long' type.
    
    /// <see cref="Player.id"/>+
    /// <see cref="Player.experience"/>+   <-- Updated automatically by IDE
    class Update_score { } 
}
```

**Result:** The `Update_score` pack is now generated as `{ int id; long experience; }`. Zero desynchronization, zero manual boilerplate maintenance.















---

2. Overriding & Conflict Resolution

Because of the "First-Occupied" rule, you cannot simply add a new field to overwrite an old one. You must explicitly **vacate** the name first using
the Exclusion operator.

How to Override a Field
If you import a Pack but want to change the type or source of one specific field, use the **"Exclude-then-Add"** pattern:

1. **Exclude (`-`)** the field from the source Pack.
2. **Add (`+`)** the field from the new source (or define it natively).

```csharp
/// <remarks>
/// Scenario: We want everything from 'Header', 
/// BUT we want 'id' to come from 'ExtendedHeader' instead.
/// </remarks>
/// 
/// 1. Import Header, but BLOCK 'id' so the name remains available.
/// <see cref="Header"/>+
/// <see cref="Header.id"/>-
/// 
/// 2. Import 'id' from the new source.
/// <see cref="ExtendedHeader.id"/>+
class MyPacket { ... }
```

---

3. C# Inheritance Support

Single Inheritance
Standard C# syntax. Base class fields are added strictly as a fallback.

```csharp
// 'MyPack' will have all fields from 'BasePack'
// EXCEPT those that are already defined in 'MyPack' or imported via XML.
class MyPack : BasePack { ... }
```

Multiple Inheritance
AdHoc supports multiple inheritance via the `_<...>` helper.

```csharp
// Inherits from BaseA, BaseB, and BaseC.
class MyPack : Base, _<BaseA, BaseB, BaseC> { ... }
```

**Conflict Logic:** If `BaseA` and `BaseB` both have a field named `timestamp`, `BaseA` (left-most) wins. `BaseB.timestamp` is skipped because the
name is already occupied.

---

Comprehensive Example

```csharp
using org.unirail.Meta;

namespace com.my.project
{
    // Source A
    class CommonHeader 
    { 
        public int id; 
        public int version; 
        public string debug_tag; 
    }

    // Source B
    class SessionInfo 
    { 
        public string token; 
        public long expires; 
    }

    // ---------------------------------------------------------
    // CASE 1: NATIVE PRIORITY
    // ---------------------------------------------------------
    /// <see cref="CommonHeader"/>+
    struct CustomHeader
    {
        // This NATIVE field takes priority. 
        // The generator skips 'CommonHeader.version' because 'version' exists here.
        public string version; 
    }

    // ---------------------------------------------------------
    // CASE 2: FILTERING (Allow-list / Block-list)
    // ---------------------------------------------------------
    /// <remarks>
    /// 1. Import CommonHeader.
    /// 2. Remove 'debug_tag' (it will not exist in the final struct).
    /// 3. Add SessionInfo fields.
    /// </remarks>
    /// <see cref="CommonHeader"/>+
    /// <see cref="CommonHeader.debug_tag"/>-
    /// <see cref="SessionInfo"/>+
    struct LoginPacket { }


    // ---------------------------------------------------------
    // CASE 3: EXPLICIT OVERRIDE
    // ---------------------------------------------------------
    /// <remarks>
    /// We want CommonHeader, but we want 'id' to be a 'long' (Native).
    /// </remarks>
    /// <see cref="CommonHeader"/>+
    /// <see cref="CommonHeader.id"/>-  <-- CRITICAL: Prevent import of 'int id'
    struct BigIdPacket 
    {
        public long id; // Native field fills the now-empty 'id' slot.
    }
}
```

### Field Injection

The `FieldsInjectInto` interface allows you to define a "template" class containing fields that are automatically injected into the **payload** (the
main data body) of other packets. This is ideal for managing cross-cutting concerns without repetitive code. The template class itself is not
preserved as a packet; only its fields are distributed.

`FieldsInjectInto< PackSet >` injects fields only into transmittable packets defined within the specified [
`PackSet`](#projecthost-as-a-named-pack-set).

**Example:**

Define common fields and apply them to all packets in `MyProject` except `Point2d`.

```csharp
// Rule: Add 'name' and 'length' to all packets in MyProject, but exclude Point2d.
class CommonFields : FieldsInjectInto< _<MyProject, X<Point2d>> > {
    string name;
    int length;
}

// Packet Definitions
class Point2d {
    float X;
    float Y;
}

class Point3d { // Assumed to be in MyProject scope
    float X;
    float Y;
    float Z;
}
```

**Resulting Generated Structures:**

The source generator transforms the logic as follows:

* `Point2d` remains unchanged because it was excluded via `X<Point2d>`.
* `Point3d` is modified to include the injected fields.

```csharp
class Point3d {
    // --- Injected Fields ---
    string name;
    int length;
    // --- Original Fields ---
    float X;
    float Y;
    float Z;
}
```

> [!NOTE]  
> If a target packet already contains a field with the same name as an injected field, the target's original field is replaced. The injector's
> definition (type, attributes, and documentation) takes precedence.

---

#### Modify Imported Field Injection

The `Modify Imported Field Injection` feature targets a specific `TargetFieldInjection`. Use the `org.unirail.Meta.Modify<TargetFieldInjection>`
modifier and specify a **PackSet** to add or remove fields from the target injector.

```csharp
class FieldInjectionModifier : Modify<TargetFieldInjection>, _<AddPack, X<RemovePack>>  {
    string name; // Adds field to the imported injector
    int length;
}
```

---

### Headers

A **packet header** contains protocol-level metadata separate from the **application payload**. These fields manage network tasks such as routing,
stream identification, and session management.

**Key Characteristics of Headers**

- **Transmission Order**: Headers are sent and received *before* the payload and are directly accessible in network event handlers.
- **Data Types**: Header fields must use primitive, **non-nullable❗** types (e.g., `bool`, `int`, `long`).
- **Scope**: Headers are only present in **Standalone Packets**, not in **Sub-Packets**.
	- **Standalone Packet**: A packet sent directly over a connection, always including a header.
	- **Sub-Packet**: A packet used as a field type within another packet. It contributes only its payload to the parent; it does not have its own
	  headers.

#### Adding Header Fields

Header fields are added to standalone packets in three ways:

> 1. **Implicit (Automatic)**: Every standalone packet includes a `packet_id` for identification.
> 2. **Conditional (Feature-Based)**: Additional fields are included based on specific features. For example, **Multi-Channel** connections add
     channel identifiers for message routing, and **RPC** calls add unique identifiers to link callers with repliers.
> 3. **Explicit (User-Defined)**: Custom header fields can be defined by creating a `Header` class that implements the `HeaderFor<PackSet>` interface.

#### Header Scope

The scope of an explicit header depends on its declaration context:

1. **Connection-Specific Scope (Highest Precedence)**:
	- Applies only to packets sent through a specific connection.
	- **How**: Declare `HeaderFor<PackSet>` within a `Connection` interface (formerly `Channel`).

2. **Host-Specific Scope**:
	- Applies only to packets sent from or received by a specific host.
	- **How**: Declare `HeaderFor<PackSet>` within a `Host` definition.

3. **Project Scope (Lowest Precedence)**:
	- Applies globally to specified packets across the project, unless overridden.
	- **How**: Declare `HeaderFor<PackSet>` at the project’s top level.

**Precedence Rule**: Connection-Specific **▷** Host-Specific **▷** Project Scope.


#### Modify Imported Header

The `Modify Imported Header` feature targets a specific `TargetHeader`. Use the `org.unirail.Meta.Modify<TargetHeader>` modifier and specify a *
*PackSet** to add or remove fields.

```csharp
class HeaderModifier : Modify<TargetHeader>, _<AddPack, X<RemovePack>>  {
    string name; // Adds field to the imported header
    int length;
}
```

---

### Value Pack

A **Value Pack** is a high-performance data structure that packs multiple fields into a single primitive type of **up to 8 bytes**.

**Key Features**

- **Zero Heap Allocation**: Stored as value types, avoiding garbage collection overhead.
- **Compact Memory Layout**: Efficiently packs data into eight bytes or fewer.
- **Type Safety**: Full compile-time validation. Fields must be primitive numeric types or other Value Packs.
- **Automatic Implementation**: The generator produces optimized packing/unpacking code automatically.

**Memory Layout**
The generator calculates the required bit-space and selects the smallest appropriate primitive type (e.g., `int`, `long`) for storage.

```csharp
// Example: 6 bytes total, packed into an 8-byte primitive (long)
class PositionPack {
    float x;     // 4 bytes
    byte layer;  // 1 byte
    byte flags;  // 1 byte 
}
```

#### Smart Flattening

The generator automatically flattens nested **single-field** `Value Packs` to eliminate unnecessary wrapping.

**Basic Flattening**

```csharp
class Temperature { float celsius; }
class SensorReading { Temperature measurement; }

// Resulting structure:
class SensorReading { float measurement; }
```

**Nullability Preservation**
Flattening preserves nullability throughout the chain:

```csharp
class Pressure { float kilopascals; }
class PressureSensor { Pressure? reading; }

// Resulting structure:
class PressureSensor { float? reading; }
```

**Deep Flattening**
Value Packs can flatten deep chains of single-field types:

```csharp
class Voltage { float volts; }
class PowerLevel { Voltage? level; }
class DeviceStatus { PowerLevel? power; }

// Resulting structure:
class DeviceStatus { float? power; }
```

**Example Comparison**

```csharp
class FloatWrapper { float field; }
class FloatWrapperNullable { float? field; }

// The following fields result in the same underlying type: Set<float?>.
Set<FloatWrapper?>          set_a;                
Set<FloatWrapperNullable>   set_b; 
Set<FloatWrapperNullable?>  set_c; 
```

### Modify Imported Packs

To modify the layout of imported packs, create a new pack and merge its fields into the `TargetPack` by implementing the built-in
`org.unirail.Meta.Modify<TargetPack>`.

To remove specific fields from the `TargetPack`, use the `<see cref="Full.Path.To.OtherPack.RemoveField"/>-` XML comment on the pack.

For example:

```csharp
/// <see cref="Agent.Proto.proto"/>+   // Add field to target
/// <see cref="Agent.Login.uid"/>-     // Remove field from target
class Pack : Modify<TargetPack> { 
    public string UserName;
    public long LoginTime;
}
```

This approach allows you to add, remove, and replace fields from an imported pack.

> [!NOTE]  
> A modifier pack can function as a normal pack.

## Connections

In the **AdHoc protocol**, a connection establishes a communication link between two hosts. Connections are declared as C# interfaces within your
project and must extend the built-in `org.unirail.Meta.Connects<HostA, HostB>` interface, which specifies the two hosts being linked.

> [!TIP]
> **Actor Analogy: The Remoting Association**
> Think of a **Connection** as the static definition of an **Actor System Association** or a Remoting Link. It is the physical pipe through which all
> actors (Channels) on one node communicate with actors on a remote node.

**Example:**

```csharp
using org.unirail.Meta;

namespace com.company {
    public interface MyProject {
        // Defines a connection between Client and Server
        interface Communication : Connects<Client, Server> { }
    }
}
```

![image](https://github.com/user-attachments/assets/dd47301d-4f2b-4648-ab1b-8f00f40ce271)

**Connection Architecture**

The AdHoc protocol implements a layered architecture where connections bridge external networks with internal hosts. Each connection comprises
multiple processing layers, with each layer containing both an **EXT**ernal and **INT**ernal side.

<details>
 <summary><span style="font-size:30px">👉</span><b><u>Click to see architecture diagram</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/234749384-73a1ce13-59c1-4730-89a7-0a182e6012eb.png)

</details>

At the code level, this is implemented through the `org.unirail.AdHoc.Connection.External` and `org.unirail.AdHoc.Connection.Internal` interfaces.

> [!IMPORTANT]  
> **[Data is represented on the wire using little-endian format.](https://news.ycombinator.com/item?id=25611514)**

**Defining Protocol Flow**

To define the logical message flows within a connection—including packet ordering and response patterns—populate the connection interface body with [
`stages`](#stages) and [`branches`](#branches), or define straightforward request-response pairs using [
`RPC Declarations`](#rpc-declarations). These elements define your protocol's context and data flow logic.

> [!NOTE]
> **Actor Analogy: Finite State Machines (FSM)**
> defining `stages` and `branches` is analogous to defining an **Actor FSM**. Just as an Actor switches behaviors (e.g., `Become(WaitingForAuth)`),
> the Connection tracks which `stage` the communication is currently in to validate incoming messages.

**Importing and Composing Connections:**

A connection can import content from other connections by extending them. To reverse the host roles of imported content, wrap the inherited connection
with `org.unirail.Meta.SwapHosts<Connection>`:

```csharp
interface CommunicationConnection : Connects<Server, Client>, 
                                    SomeOtherConnection, 
                                    SwapHosts<TheConnection> { }
```

### Channels

A **channel** is a virtual communication pathway within a physical connection. Every connection contains at least one channel: the **default channel
**, which uses the connection's body scope to declare its entities. Additional named channels can be created, each operating independently within its
parent connection.

Named channels (beyond the default) are declared within the connection scope as C# interfaces that implement `org.unirail.Meta.Channel`.

> [!TIP]
> **Actor Analogy: Actor Classes vs. Actor Refs**
> * **The Channel Interface** is the **Actor Class** (the blueprint/type definition).
> * **The Channel Instance** is the running **Actor Reference** (the actual worker in memory).
>
> A single Connection acts as a supervisor that manages multiple types of Actors (Channels) running over the same network socket.

The instance capacity for **all** channels (both the default and named channels) is controlled by a single property in the `Connects` interface:

```csharp
public interface Connects<L, R> : _
    where L : struct, Host
    where R : struct, Host {
    int MaxChannelInstances => 1;
}
```

- `MaxChannelInstances`: Defines the maximum number of instances allowed for **each** channel type defined in the connection. This setting applies to
  all connection channels, default and named.

#### Addressing and Broadcasting (Multi-Entity Rule)

When a connection contains **multiple channels** or is configured for **multiple instances**, the protocol utilizes a power-of-2 memory alignment to
create a dedicated **Broadcast ID**.

**The Alignment Rule:**
The total address space ($N$) is aligned to the **next power of 2** based on the count of items (Channels or Instances).

**The Broadcast ID:**
The **last index** of this aligned space ($N - 1$) is reserved as a special **Broadcast ID**.

* Sending a packet to this ID broadcasts the message to **all** active participants in that scope.
* **Valid Unicast IDs** are restricted to the range `0` to `N - 2`.

> [!NOTE]  
> **Single Entity Exception**
> This logic does **not** apply if configured only one channel or only one instance.
> **Actor Analogy: The Broadcast Router**
> The **Broadcast ID** functions exactly like an **Akka Broadcast Group Router**.
> * Sending to **ID 0...N-2** is like sending a message to a specific child Actor (Unicast).
> * Sending to **ID N-1** triggers the Router to forward the message to the entire pool of Actors (Broadcast).
>
> Unlike software routers, AdHoc bakes this routing logic directly into the binary addressing scheme for zero-overhead multiplexing.

**Example Scenarios:**

1. **Multi-Instance Case (`MaxChannelInstances => 7`)**
	* **Alignment:** 7 aligns to **8**.
	* **Address Space:** `0` to `7`.
	* **Valid Instance IDs:** `0, 1, 2, 3, 4, 5, 6` (Exactly 7 instances).
	* **Broadcast ID:** `7`. (Sending here reaches all instances).

2. **Multi-Channel Case (Default + 2 Named Channels = 3 Total)**
	* **Alignment:** 3 aligns to **4**.
	* **Address Space:** `0` to `3`.
	* **Valid Channel IDs:** `0, 1, 2` (Mapped to the 3 defined channels).
	* **Broadcast ID:** `3`. (Sending here reaches all channels).

3. **Overflow Case (`MaxChannelInstances => 9`)**
	* **Alignment:** 9 aligns to **16**.
	* **Valid Instance IDs:** `0` to `14` (Allows up to 15 instances).
	* **Broadcast ID:** `15`.

**Code Example:**

```csharp
using org.unirail.Meta;

namespace com.company {
    public interface MyProject {
        // Defines a connection between the Client and the Server
        interface Communication : Connects<Client, Server> {
            
            // Configure ALL channels (Default + Named) to allow up to 7 instances each
            int Connects<Client, Server>.MaxChannelInstances => 7;
            
            // Declare additional named channel
            // This channel also supports up to 7 instances, defined by the connection setting
            interface CPUMetricsChannel : Channel {
            }
        }
    }
}
```

Each channel instance maintains its own stage state.

> [!TIP]
> **Actor Analogy: State Isolation**
> "Each channel instance maintains its own stage state" effectively means **Share Nothing Architecture**.
> Just like two Actors of the same type do not share variables, `CPUMetricsChannel[0]` and `CPUMetricsChannel[1]` track their protocol state
> completely independently.

### Stages

Stages represent distinct processing states within a channel's lifecycle. They define the current context—what messages are expected and what logic
should execute. The implementation follows established [state machine patterns](https://en.wikipedia.org/wiki/Finite-state_machine) similar to
frameworks like:

- [**Spring Statemachine**](https://spring.io/projects/spring-statemachine): Hierarchical states with transition guards
- [**xstate**](https://github.com/statelyai/xstate): Statecharts with visual tooling
- [**squirrel-foundation**](https://github.com/hekailiang/squirrel): Lightweight hierarchical state machines
- [**StatefulJ**](https://www.statefulj.io/): State machines for RESTful services

> [!NOTE]  
> In the AdHoc protocol, the state machine is event-driven by packet transmission and timeouts. The protocol defines the overall system architecture
> rather than prescribing every implementation detail.
>
> The AdHoc server generates code from your dataflow description. Developers integrate this code into their implementations, adding custom logic and
> handling edge cases as needed.
>
> For usage examples, search for `Communication.Stages` in
> the [ConnectsToServer.cs](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/ChannelToServer.cs) file.

**Practical Example: Communication Lifecycle**

Let's examine the communication channel from [
`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/acfc582c971914a4a86f3458d4b85a141a787d3c/AdHocProtocol.cs#L443):

<details>
 <summary><span style="font-size:30px">👉</span><b><u>Click to view communication flow diagram</u></b></summary>

<img width="1120" height="2392" alt="Communication flow" src="https://github.com/user-attachments/assets/3b059e62-6fb3-482a-b6d3-1ba56ef8af56" />

The diagram on the **right** illustrates the connection lifecycle as declared in code. To view this in the **Observer** tool, run:

```cmd
AdHocAgent.exe /path/to/AdHocProtocol.cs?
```

Right-click on a connection link to open the connections window and resize to see all available connections.

**Top Diagram: Agent ↔ Server Communication**

This illustrates stateful communication between an `Agent` and `Server`, progressing from connection establishment through task processing.

**1. Initialization & Version Validation**

* **Agent starts** in the `Start` state and sends its `Version` to the `Server`
* **Server validates** the version while in the `VersionMatching` state:
	* **Mismatch:** Server sends an `Info` packet with error details, then terminates (⛔)
	* **Match:** Server sends an `Invitation`, advancing to authentication

**2. Authentication**

* **Agent transitions** to `Login` state; Server enters `LoginResponse` state
* **Agent sends** either `Login` (existing user) or `Signup` (new user)
* **Server processes** credentials:
	* **Failure:** Sends `Info` packet with error
	* **Success:** Sends `Invitation`, advancing Agent to task submission

**3. Task Processing**

* **From `TodoJobRequest`**, Agent can submit two task types:
	1. **Project Task:** Sends `Project` or `RequestResult`; Server enters `Project` processing
	2. **Proto Task:** Sends `Proto` data; Server enters `Proto` processing
* **Server responds** with a final packet, then terminates:
	* **Success:** Sends `Result` with outcome (⛔)
	* **Failure:** Sends `Info` with error description (⛔)

**Bottom Diagram: Agent ↔ Observer Communication**

This shows a persistent connection for monitoring and control.

**1. Initialization**

* **Agent** sends initial state: `Layout` (UI structure) and `Project` (data)
* **Observer** renders the initial view

**2. Interactive Loop**

* **Observer** (in `Operate` stage) can send:
	* `Up_max_date`: Check for data updates
	* `Show_Code`: Request code display in IDE
	* `Layout`: Save modified diagram layout
* **Agent** (in `RefreshProject` stage) responds:
	* Updated `Project` if needed
	* `Up_max_date` confirmation if no update required

This creates a continuous synchronization loop.

</details>

---

#### Declaring Stages

Stages are declared within the connection or channel scope as C# interfaces. The interface name becomes the stage name, and the topmost stage
represents the initial state.

The code generator traverses from the top stage; unreachable stages are ignored.

A stage interface must extend one of:

- `org.unirail.Meta.L` (left host)
- `org.unirail.Meta.R` (right host)
- `org.unirail.Meta.LR` (both hosts)

Branch declarations follow immediately after the host designation.

![Host designation example](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/1cd6ad55-7e0e-4167-9d4a-fef279b4fa11)

Stages can be unidirectional (only one side sends):

![Unidirectional example](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/f1cdc9e3-9e14-4781-af7b-ce46b3dc5234)

> [!WARNING]
> Short block comments like `/*įĂ*/` contain auto-generated unique identifiers. These identify entities uniquely across renames and relocations. *
*Never edit or clone these identifiers.**

#### Stage Timeouts

Use built-in attributes to define maximum stage duration (in seconds):

- `[ReceiveTimeout(seconds)]`
- `[TransmitTimeout(seconds)]`

Without these attributes, stages persist indefinitely.

---

### Branches

After declaring a host side (`L`, `R`, or `LR`), outgoing packets are organized into **branches**. Each branch contains:

1. A [`PackSet`](#projecthost-as-a-named-pack-set) of packets that can be sent
2. Optionally, a target `stage` to transition to after sending

**Transition rules:**

- **No target specified:** Implicitly transitions to itself (persistent stage)
- **`org.unirail.Meta.Exit` target:** Receiving host terminates the connection
- **Named stage target:** Transitions to that stage

#### Implicit vs Explicit Self-Reference

These two declarations are equivalent:

**Implicit (recommended):**

```csharp
interface Login /*ā*/ : L,
                        _< /*Ă*/
                            Agent.Name,
                            Agent.Signup
                        >
{ }
```

**Explicit:**

```csharp
interface Login /*ā*/ : L,
                        _< /*Ă*/
                            Agent.Name,
                            Agent.Signup,
                            Login  // explicit self-reference
                        >
{ }
```

---

#### Symmetric Branches with `LR`

When both hosts share identical branch structure, use `LR` to avoid duplication:

**Recommended:**

```csharp
interface Start : LR,
                  _< 
                      LayoutFile.UID,
                      GoToStage
                  >
{ };
```

**Instead of:**

```csharp
interface Start : L,
                  _< 
                      LayoutFile.UID,
                      GoToStage
                  >,
                  R,
                  _< 
                      LayoutFile.UID,
                      GoToStage
                  >
{ };
```

![Symmetric example](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/8637f064-75e7-4ab0-8c66-c7625a7aa813)

---

### RPC

While [`stages`](#stages) and [`branches`](#branches) provide a powerful way to define complex, stateful asynchronous flows (like FSMs), you often
need a simpler approach for standard **Request-Response** interactions.

To support this, the AdHoc protocol allows you to declare Remote Procedure Calls (RPC) directly within your connection interfaces.

> [!WARNING]  
> **RPC is a Primitive**
> While convenient, it is important to understand that RPC is fundamentally a **primitive** mechanism. It lacks the built-in state machine
> capabilities of `stages` and `branches`. It cannot natively enforce complex multi-step workflows, contextual packet validation, or strict protocol
> lifecycle phases. Use RPC strictly for straightforward, stateless request-reply interactions, and rely on `stages` for advanced, stateful protocol
> flows.

#### Defining

An RPC method is defined by declaring a method signature that takes a request packet and returns a reply packet.

Because an RPC must be executed by a specific endpoint, you use the generic **`[HostedBy<THost>]`** attribute to specify **where the function is
located**. The code generator uses this to understand which host acts as the server (the side that implements and executes the function) and which
acts as the client (the side that calls it).

If the attribute is omitted, the RPC method is treated as **bidirectional**, meaning both hosts implement the handler, and either host can initiate
the call.

#### Nesting

As your protocol grows, placing all RPC methods at the root of the connection interface can become cluttered. To organize your API, you can group
related RPC methods by declaring nested interfaces. The code generator will treat these nested interfaces as logical service groupings (similar to
namespaces or controller classes), keeping your generated code clean and highly organized.

**Example:**

```csharp
using org.unirail.Meta;

        public interface MyProject {
        
            // Defines a connection between Monitoring and MonitoringObserver
            interface MonitoringToMonitoringObserver : Connects<Monitoring, MonitoringObserver> {
            
                int Connects<Monitoring, MonitoringObserver>.MaxChannelInstances => 16;
            
                // 1. Unidirectional RPC (Root level): 
                // The 'MonitoringObserver' hosts this method (acts as the server).
                // Therefore, only the 'Monitoring' host is permitted to call it.
                [HostedBy<MonitoringObserver>]
                HelloReply SayHelloToObserver(HelloRequest pack);

                // Grouping related functions into a logical "Service"
                interface AccountingService {
                
                    // 2. Unidirectional RPC (Nested): 
                    // The 'Monitoring' host implements and runs this method.
                    // Only the 'MonitoringObserver' can initiate this call.[HostedBy<Monitoring>]
                    HelloReply SayHelloToMonitoring(HelloRequest pack);
                }
            
                // Another logical grouping
                interface SecretBranch{
                    Passsword SayPassword(Request pack);
                    
                    interface GreetingDepartment{

                        // 3. Bidirectional RPC (Nested): 
                        // No attribute is specified. 
                        // Both sides host this function, meaning both can initiate the call to the other.
                        HelloReply SayHelloBidirectional(HelloRequest pack);
                    }
                }
            }
        }
```

> [!TIP]
> **Code Generator Metadata**
> Developers **will never implement or invoke these interface methods directly**. These method signatures act purely as a declarative schema (DSL).
> The code generator parses the `[HostedBy<THost>]` metadata and interface hierarchy to automatically scaffold the underlying request/response packet
> routing, grouped handler stubs, and awaitable tasks on the correct sides of the connection.

### Modifying Imported Connections

You can customize imported connections and their components (stages, channels, branches) without modifying the original definitions.

#### Modification Syntax

**For connections or stages:**

- Replicate the target's structure with custom naming
- Extend `org.unirail.Meta.Modify<TargetEntity>` or `org.unirail.Meta.Modify<TargetConnection, HostA, HostB>`

**To delete entities:**

- Reference with XML comment: `/// <see cref="Delete.Connection"/>-`

**Within branches:**

- **Delete:** Wrap entities in `org.unirail.Meta.X<>`: `X<Agent.Login>`
- **Add:** Reference new entities normally
- **Modify transitions:** Explicitly reference the target stage (even if self-referencing)

> [!NOTE]  
> Modified branches are identified by their transition target stage.

#### Example: Removing Entities from a Branch

```csharp
interface UpdateLogin : Modify<Login>, 
                        L, 
                        _
                            X<Agent.Login>,    // Remove packet
                            X<Agent.Signup>,   // Remove packet
                            X<Login>,          // Remove self-transition
                            Update_to_stage    // Set new target
                        >
{ }
```

#### Example: Changing Implicit Self-Reference

Original stage (implicitly self-referencing):

```csharp
interface Login : L,
                  _
                      Agent.Login,
                      Agent.Signup
                  >
{ }
```

To redirect the transition, explicitly reference `Login`:

```csharp
interface UpdateLogin : Modify<Login>, 
                        L, 
                        _
                            X<Login>,          // Remove implicit self-transition
                            Update_to_stage    // Set new target
                        >
{ }
```

---

#### Complete Example: Overriding an Inherited Connection

Suppose you import [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs) and need to modify the
`Communication` connection:

```csharp
interface UpdateCommunication : Modify<AdHocProtocol.Communication> {
    
    // Remove packet from a named pack set
    interface Change_Info_Result : Modify<AdHocProtocol.Communication.Info_Result>,
                                   _
                                       X<Server.Info>  // Remove this packet
                                   > { }

    // Add timeout and change stage transition
    [TransmitTimeout(30)]
    interface Updated_Start : Modify<AdHocProtocol.Communication.Start>,
                              L,
                              _
                                  X<AdHocProtocol.Communication.VersionMatching>,  // Remove transition
                                  NewStage                                         // Set new target
                              > { }

    // Replace packet in existing stage
    interface UpdatedVersionMatching : Modify<AdHocProtocol.Communication.VersionMatching>,
                                       R,
                                       _
                                           X<Server.Invitation>,  // Remove
                                           Authorizer             // Add
                                       > { }

    // Define new stage
    interface NewStage : L,
                         _
                             Sending_Pack
                         > { }
}
```

# Fields

## Numeric Types

The AdHoc protocol supports all C# numeric primitives (excluding `decimal`).

| Type     | Range                                                    |
|:---------|:---------------------------------------------------------|
| `sbyte`  | \-128 to 127                                             |
| `byte`   | 0 to 255                                                 |
| `short`  | \-32,768 to 32,767                                       |
| `ushort` | 0 to 65,535                                              |
| `int`    | \-2,147,483,648 to 2,147,483,647                         |
| `uint`   | 0 to 4,294,967,295                                       |
| `long`   | \-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| `ulong`  | 0 to 18,446,744,073,709,551,615                          |
| `float`  | ±1.5 x 10−45 to ±3.4 x 1038                              |
| `double` | ±5.0 × 10−324 to ±1.7 × 10308                            |

### longJS

If you are planning to generate a host in `TypeScript`, you must be aware of the limitations
of the `TypeScript (JavaScript)` `number` type.  
The `number` type can safely represent integers only within the range of **-2^53 + 1 to 2^53 - 1** (
see [SAFE_INTEGER](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number/SAFE_INTEGER)).

If a field's value exceeds this range, the [BigInt](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt)
type will be used, which is less efficient than the `number` primitive.

Therefore, if your field's data is within the safe integer range, using `longJS` or `ulongJS` for communication with the host generated in TypeScript
is preferable to using `long` or `ulong`.

## Constants

Fields declared as `const` or `static` that use primitive types, strings, or arrays of these types.

- **`static` fields**:
  Can be assigned a value or the result of a calculated expression. Any available C# static functions can be used to calculate their values.

- **`const` fields**:
	- Can be used as `attribute` parameters.
	- Must have a value resulting from a C# compile-time expression and cannot use C# static functions to calculate their values.

To overcome the limitations of `const` fields, the `AdHoc protocol description` syntax introduces the `[ValueFor(const_constant)]` attribute.

- Applied to a proxy `static` field.
- During code generation, the generator assigns the value and type from this `static` field to the corresponding `const` constant.

This approach combines the flexibility of `static` fields with the compile-time benefits of `const` constants.

**Example: Using the `[ValueFor(ConstantField)]` Attribute**

Here's an example demonstrating the use of the `[ValueFor(ConstantField)]` attribute:

```csharp
[ValueFor(ConstantField)] static double value_for = Math.Sin(23);

const double ConstantField = 0; // Result: ConstantField = Math.Sin(23)
```

In this example:

- `value_for` is assigned the value of `Math.Sin(23)`.
- This value is then copied to the `ConstantField` constant.
- Due to the `[ValueFor(ConstantField)]` attribute, `ConstantField` will have the calculated value of `Math.Sin(23)`.

## Attributes

As the protocol creator, you possess the most comprehensive understanding of your data and its specific requirements.
The **AdHoc protocol description** empowers you with tools to encode metadata effectively, facilitating optimized code generation.
Attributes can be applied to the following protocol elements: **Hosts, Packs, Fields, Connections,** and **Stages**.

### Built-in Attributes

The AdHoc protocol description includes a suite of built-in attributes within the `org.unirail.Meta` namespace.
These attributes enable you to convey essential metadata directly to the code generator, ensuring efficient and accurate implementation.

Let's consider a field with values in the range of **400,000,000** to **400,000,193**. Storing them as an `int` would waste memory.
Instead, we can optimize memory usage by using a constant offset of 400,000,000.
This enables representing the entire range using just one byte.

When setting a value, the constant offset is subtracted from the input value.
Conversely, when retrieving the value, the constant is added back to return the original value.

This approach is applied seamlessly using the built-in `MinMax` attribute:

```csharp
[MinMax(400_000_000, 400_000_193)] int ranged_field;
```

When the `MinMax` attribute is used, the **code generator**:

1. Automatically determines the most efficient storage type (e.g., `byte` for this range).
2. Generates **getter** and **setter** methods to handle the offset during access operations.

If the `MinMax` arguments specify a range of less than 127, the code generator can further optimize memory usage by packing
multiple small-range fields into a pack's **bit storage** bytes. For example:

```csharp
[MinMax(1, 8)] int car_doors;
```

In this case:

- The range **1 to 8** requires only **3 bits**.
- The code generator:
	- Allocates three bits for `car_doors` in the pack's **bit storage**.
	- Generates optimized **bit manipulation logic** for accessing and modifying the field value.

### Custom Attributes

In addition to built-in attributes, you can define **custom attributes**. Custom attributes are transformed by the generator into a hierarchy of
constants, offering a straightforward and consistent approach to defining metadata.

- For **fields**, attributes are the primary method for specifying metadata.
- For other entities, metadata can be defined either through attributes or directly using constants.
  For more details, see [Constants](#constants).

Example: Specifying a Description Attribute

To set a Description for a connection stage, you can define a reusable `Description` attribute:

```csharp
[AttributeUsage(AttributeTargets.Interface)]
public class DescriptionAttribute : Attribute {
    public DescriptionAttribute(string description) { }
}

interface Communication : Connects<Agent, Server> {
    [Description("The stage either responds with the result if successful or provides an error message with relevant information in case of failure.")]
    interface Stage : 
        _<
            Server.Info,
            Server.Result
        > { }
}
```

Alternatively, you can use a constant:

```csharp
interface Communication : Connects<Agent, Server> {
    interface Stage : 
        _<
            Server.Info,
            Server.Result
        > {
        const string Description = "The stage either responds with the result if successful or provides an error message with relevant information in case of failure.";
    }
}
```

Both approaches provide similar functionality, but the second approach offers more granular control over the layout of constants.

## Optional Fields

`Optional (nullable) Fields` are identified by type declarations ending with a `?` (e.g., `int?`, `byte?`, `string?`, etc.).
They are allocated in memory but transmit only a single bit when empty, significantly optimizing transmission size.
Fields with reference types (e.g., embedded packs, strings, and collections) that are not part of a collection are always treated as optional.

```csharp
class Packet
{
    string user;          // Single referenced data types are always optional
    string[] tags;        // The field with a collection is optional, but collection items with referenced data types are not
    string?[] emails;     // The field with a collection is optional, as well as the collection items
    uint? optional_field; // Optional uint field (nullable)
}
```

For **optional fields** with primitive types, the AdHoc generator attempts to encode the empty value efficiently by default.
This behavior can be overridden by declaring the field as **required (not nullable)** and applying a custom attribute to specify a value that should
be treated as the "empty" or "ignored" state.

```csharp
        [AttributeUsage(AttributeTargets.Field)]
        public class IgnoreZoomIfEqualAttribute : Attribute
        {
            public IgnoreZoomIfEqualAttribute(float value) { }
        }
        
        [IgnoreZoomIfEqual(1.1f)]
        float zoom;
```

The `[IgnoreZoomIfEqual]` attribute with the value `1.1f` will be embedded directly into the field's generated logic.

## Value layers

The AdHoc generator uses a 3-layered approach for representing field values.

| Layer | Description                                                                                                         |
|:------|:--------------------------------------------------------------------------------------------------------------------|
| exT   | **External datatype**. The representation required for external consumers (matches language data type granularity). |
| inT   | **Internal datatype**. The representation optimized for storage (matches language data type granularity).           |
| ioT   | **IO wire datatype**. The network transmission format. Transmitted as a byte stream (no language granularity).      |

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/180a331d-3d55-4878-8dfe-794ceb9297f3)

When dealing with a field containing values ranging from 1,000,000 to 1,080,000, applying shifting on the `exT <==> inT` transition will not result in
memory savings in C# or Java. This limitation stems from the fixed type quantization inherent to those languages.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0b8f90cc-aafc-4923-8c90-1fed53775bb3)

Nevertheless, prior to transmitting data over the network (`ioT`), a simple optimization can be implemented by subtracting a constant value of
1,000,000. This action effectively reduces the data to a mere 3 bytes. Upon reception, reading these 3 bytes and subsequently adding 1,000,000 allows
for the retrieval of the original value.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a28e5b20-5c49-4b18-be98-e9bfb6387290)

This illustrates that data transformation on `exT <==> inT` can be redundant, becoming truly meaningful only during the `inT <==> ioT` transition.

While effective, this technique isn't universal. When a field's data type is an `enclosed` array, repacking data into different array types during
transitions can be costly and impractical, especially when dealing with keys in a `Map` or `Set` (e.g., `Map<int[], string>`).

## Varint type

When a numeric field contains randomly distributed values spanning the entire numeric type range, compression is typically inefficient:

![image](https://user-images.githubusercontent.com/29354319/70127303-bdf40900-16b5-11ea-94c9-c0dcd045500f.png)

However, if the numeric field exhibits a specific dispersion or gradient pattern within its range, as shown below:

![image](https://user-images.githubusercontent.com/29354319/70128574-0a404880-16b8-11ea-8a4d-efa8a7358dc1.png)

Compression becomes highly advantageous. In such cases, the code generator
employs [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding (
a [variable-length quantity](https://en.wikipedia.org/wiki/Variable-length_quantity) algorithm) for single-value fields. For collections, the
generator can utilize `Group Varint Encoding`.

This algorithm skips the transmission of leading zero bytes, restoring them on the receiving end. The following graph illustrates the space savings
for smaller values:

![image](https://user-images.githubusercontent.com/29354319/70126207-84ba9980-16b3-11ea-9900-48251b545eef.png)

It is useful to recognize three particular dispersion patterns:

|                                                     Pattern                                                     | Description                                                                                    |
|:---------------------------------------------------------------------------------------------------------------:|:-----------------------------------------------------------------------------------------------|
| ![image](https://user-images.githubusercontent.com/29354319/155324344-311c6e30-fda5-4d38-b2c7-b946aca3bcf8.png) | For rare fluctuations toward larger values relative to a probable `min`, use `[A(min, max)]`.  |
| ![image](https://user-images.githubusercontent.com/29354319/155324459-585969ac-d7ef-4bdc-b314-cc537301aa1d.png) | For fluctuations in both directions relative to a probable `zero`, use `[X(amplitude, zero)]`. |
| ![image](https://user-images.githubusercontent.com/29354319/155325170-e4ebe07d-cc45-4ffa-9b24-21d10c3a3f18.png) | For rare fluctuations toward smaller values relative to a probable `max`, use `[V(min, max)]`. |

```csharp
    [A]          uint?  field1;  // Optional; compressible values from 0 to uint.MaxValue.
    [MinMax(-1128, 873)] byte field2; // Required; fixed range without compression.
    [X]          short? field3;   // Optional; compressed using the ZigZag algorithm.
    [A(1000)]    short  field4;   // Required; compressed values from -1,000 to 65,535.
    [V]          short? field5;   // Optional; compressed values from -65,535 to 0.
    [MinMax(-11, 75)] short field6;   // Required; uniform distribution within range.
```

## Collection type

Collections such as `arrays`, `maps`, and `sets` can store primitives, strings, and `user-defined types` (packs). All collection fields are `optional`
by nature.

Controlling collection length is vital for preventing memory overflow and mitigating Distributed Denial of Service (DDoS) attacks. By default, all
collections (including `string`) are limited to 255 items. You can adjust these global limits by defining an `enum` named `_DefaultMaxLengthOf`:

```csharp
    enum _DefaultMaxLengthOf {
        Arrays  = 255,
        Maps    = 255,
        Sets    = 255,
        Strings = 255,
    }
```

Types omitted in the `_DefaultMaxLengthOf` enum retain the default limit.

### Flat array/list

Flat arrays are declared using square brackets `[]`. AdHoc supports three distinct array behaviors:

| Declaration | Description                                                                                 |
|:------------|:--------------------------------------------------------------------------------------------|
| `[]`        | **Immutable**: The array length is constant and unchangeable.                               |
| `[,]`       | **Fixed-at-Init**: Length is set during initialization and remains fixed (like a `string`). |
| `[,,]`      | **Dynamic**: Length varies up to a maximum limit (like a `List<T>`).                        |

To customize specific field limits, use the `[D(N)]` attribute:

```cs
using org.unirail.Meta;

class Pack {
    string[] array_of_255_string_with_max_256_chars; // Default constant length.
    [D(47)] Point[,] array_fixed_max_47_points;     // Fixed-at-init up to 47.
    [D(47)] Point[,,] list_max_47_points;           // Variable length up to 47.
}
```

### String

A `string` is an immutable array of characters. By default, strings are limited to 255 characters. Use the `[D(+N)]` attribute to impose a specific
limit on a field:

```csharp
class Packet {
    string                   string_field_with_max_255_chars;
    [D(+6)] string           opt_string_field_with_max_6_chars;
    [D(+7000)] string        string_field_with_max_7000_chars;
}
```

For frequently used formats, utilize the `TYPEDEF` construction:

```csharp
class max_6_chars_string {         
    [D(+6)] string TYPEDEF;
}

class Packet { 
    string                string_field_with_max_255_chars;
    max_6_chars_string    string_field_with_max_6_chars;      
}
```

> [!NOTE]  
> **When transmitting strings, the `Varint` algorithm is used instead of `UTF-8`.**
> <details>
> <summary><b>why</b></summary>
>
> **Varint Encoding for Optimal Text Transmission in Framed Protocols**
>
> **The Premise: Aligning Encoding with Protocol Guarantees**
>
> While UTF-8 is the undisputed standard for text "at rest" in files and documents, its design principles are fundamentally misaligned with the
> guarantees offered by a modern, framed network protocol. The features that make UTF-8 a robust solution for defensive engineering with unstructured
> data become redundant and inefficient overhead within the structured **channel** of a TCP message stream.
>
> When designing a performant binary protocol, we must leverage the strengths of our transport layer. By relying on protocol-level framing, we can
> liberate our text encoding from legacy constraints and choose a more optimal representation: the variable-length integer, or `varint`.
>
> **1. Protocol Framing Makes Self-Synchronization Redundant**
>
> The definitive feature of UTF-8 is its self-synchronizing byte pattern, which allows a parser to find character boundaries even in a corrupted or
> truncated stream. This is critical for robustly handling raw text files.
>
> However, in a well-designed TCP **connection**, we do not operate on an undifferentiated stream of bytes. We use **framing**. Typically, each
> message is prefixed with its length. The logic is simple:
>
> 1. Read the length header (e.g., 4 bytes).
> 2. Read exactly that many bytes to get the complete message payload.
> 3. Repeat.
>
> The stream is no longer a boundless sea of bytes; it is a sequence of discrete, verifiable blocks.
>
> In this model, UTF-8’s self-synchronization becomes a solution to a problem that no longer exists. If a byte is lost due to a network error, the
> frame's length will not match, and the receiver's checksum will fail. The **entire frame** is invalidated and discarded. The protocol recovers at
> the
*frame level*, not the character level. Attempting to resynchronize mid-message is an anti-pattern; the integrity of the entire message is already
> lost.
>
> **2. Superior Space-Efficiency for Modern Text**
>
> A standard `varint` encoding is demonstrably more space-efficient than UTF-8 for a growing and important subset of Unicode: characters beyond the
> Basic Multilingual Plane (BMP). This includes most emoji, historic scripts, and specialized symbols.
>
> Let's compare the encoding for the "Face with Tears of Joy" emoji (😂), code point `U+1F602`:
>
> * **UTF-8 Encoding:** Requires **4 bytes** (`0xF0 0x9F 0x98 0x82`).
> * **Varint Encoding:** Requires only **3 bytes** (e.g., `0x82 0xBC 0x07`).
>
> This represents a **25% size reduction** per character. In protocols that transmit user-generated content, chats, or social media feeds, this
> efficiency is a significant advantage. It translates directly to lower bandwidth consumption, reduced server costs, and improved latency, especially
> on mobile networks. While for purely Latin text the encodings are identical in size, `varint` is better optimized for the full spectrum of modern
> Unicode.
>
> **3. Simplicity of Implementation**
>
> The logic for encoding and decoding a `varint` is lightweight and straightforward. It consists of a simple loop of bitwise shifts and checks for the
> continuation bit. In contrast, a fully compliant UTF-8 decoder requires a more complex state machine to handle multi-byte sequences and to validate
> against security vulnerabilities like non-canonical, overlong encodings.
>
> A `varint` implementation is smaller, has fewer edge cases, and is often faster to execute. This reduces the potential for bugs and simplifies the
> protocol’s **ConnectionRuntime**.
>
> **Conclusion: The Right Tool for the Job**
>
> UTF-8 was designed to bring order to the chaos of unstructured text files and simple byte streams. It paid a small price in efficiency for an
> immense gain in real-world robustness and backward compatibility.
>
> Within a framed binary protocol, that chaos is already tamed. The protocol's structure provides the integrity and synchronization that UTF-8 builds
> into its own byte patterns. By shedding these redundant features, we can choose an encoding that is simpler, faster, and more compact for modern
> communication.
>
> For the design of a new binary protocol where performance and efficiency are primary goals, the choice is clear. By leveraging the guarantees of
> protocol-level framing, **`varint` encoding for text is the superior engineering decision.**
> </details>

### Map/Set

The `Map` and `Set` types are declared in the `org.unirail.Meta` namespace.
By default, they are limited to holding a maximum of 255 items unless redefined in the [`_DefaultMaxLengthOf.Sets` /
`_DefaultMaxLengthOf.Maps`](#collection-type).
Apply the `[D(+N)]` attribute if you need to impose size limitations on a specific field with `Map` and `Set`,
the `N` represents the new limit.

```csharp
using org.unirail.Meta;

[D(+20)]Set<uint>          max_20_uints_set; //The set is limited to a maximum of 20 items.
[D(+20)]Map<Point, uint>   map_of_max_20_items; 
```

To apply type attributes specifically to the `Key` or `Value` generics, define a separate section of
attributes indicating the target as `Key:` for the key or `Val:` for the value generic type.
Example:

```csharp
        [Key: D(+30)]            // Limit the length of the Key with string type
        [Val: D(100), X]         // Limit the length of the Value with list of integers.
        Map<string, int[,,]> MAP;

        [D(+70)]                 // Limit the set's length to a maximum of 20 items. 	
        [Key: D(+30)]            // Limit the length of the list of doubles used as keys.
        Set<double[,,]> SET;
```

If the declaration becomes overly complex and is used in many fields, consider utilizing [`TYPEDEF`](#typedef) for decomposition.

```csharp
        class string_max_30_chars{
           [D(+30)] string TYPEDEF;
        }

        class list_of_max_100_ints{
            [D(100), X]  int[,,] TYPEDEF;
        }
       
        Map< string_max_30_chars, list_of_max_100_ints >[,,] MAP;
```

### Multidimensional array

A `multidimensional array` extends the concept of a `flat array` by adding dimensions, each of which can have either a constant or fixed length.
These new dimensions are defined using `[D(-N, ~N)]` attribute.

|    | Description                                                                                   |
|---:|:----------------------------------------------------------------------------------------------|
| -N | defines the length of the constant-length dimension.                                          |
| ~N | defines the maximum length of a fixed-length dimension, which is set at field initialization. |

> [!CAUTION]
> Note the prepended characters '-' and '~'.

```cs
using org.unirail.Meta;

class Pack {
    [D(-2, -3, -4)] int      ints; 
    [D(-2, ~3, ~4)] Point   points; 
    [D(-2, -3, -4)] string  strings_with_max_255_chars; 
}
```

In a multidimensional array, formatting in the form of commas inside array square brackets is ignored.

### Flat array of collection

To define a "flat array" of collections, use additional array brackets with the same format as [`flat array`](#flat-array)
For setting size limitations, use a single dimension `[D(-N)]` or `[D(~N)]` attribute.
> [!CAUTION]
> Note the prepended characters.

```csharp
class Packet{
    
    [D(-100)]          string []   [,,]    list_of_100_arrays_of_255_strings_with_max_255_chars;   
    [D(+50, -100)]     string [,,] [,]     array_of_max_100_lists_of_max_255_strings_with_max_50_chars;   
    [D(+50, 20, ~100)] string [,]  []      array_of_max_100_arrays_of_max_20_strings_with_max_50_chars;   
}
```

### Multidimensional array of collection

The declaration of a multidimensional array of collections is similar to that of a multidimensional array,
but with the addition of empty square brackets.

```csharp
class Packet{
    
    [D(+100, -3, ~3)] string?                []    mult_dim__array__of_strings_with_max_100_chars;
    [D(+100, -3, ~3)] Map<int[,,]?, byte[,]>?[]?   mult_dim_arrays_of_map_of_max_100_items;
    [D(~3, -3)]       Map<int[], byte?>      []    mult_dim_arrays_of_max_255_maps;  
}
```

## Object type

There is no distinct `Object` type available. If you want your field to have an `Object` type, use the [`Binary`](#binary-type) array type instead.
You should pre-transform your object into a binary array before storing it in the field and convert it back from binary upon retrieval.

For better efficiency, especially if you expect only a limited number of types, consider creating an optional field for each object type. For example:

```csharp
Binary[,] myFieldOfObjects;// Not ideal
```

Alternatively, you can define specific fields for each expected type:

```csharp
string? myFieldIfString;
ulong? myFieldIfUlong;
Response? myFieldIfResponse;
```

> [!NOTE]  
> If a field is empty(null), it allocates **just a bit** in the transmitting packet bytes

## Binary type

To declare the type as a raw binary array, you can use the `Binary` type from the `org.unirail.Meta` namespace.  
This type will be represented as a binary array appropriate for the target languages:
`byte` (signed) in **Java**, `byte` (unsigned) in **C#**, and `ArrayBuffer` in **TypeScript**, etc.

```csharp
using org.unirail.Meta;

class Result
{
    [D(650_000)] Binary[,,] result;// binary list with max length 65000 bytes
    [D(100)]     Binary[]   hash; // binary array with constant length 100 bytes
}
```

**Usage Guidance: Memory vs. I/O Efficiency**

While the `Binary` type is the most straightforward way to handle raw data, its performance characteristics depend on the data's location:

* **In-Memory Data:** Use `Binary` when the data is already residing in RAM (e.g., a small cryptographic hash, a generated thumbnail, or an active
  memory buffer). It provides direct access to the underlying language-native array (`byte[]` or `ArrayBuffer`).
* **External Sources (Disk/Database):** If the data is stored in a file or a database, consider using the **`Stream`** or **`File`** types instead.

**Why use Stream/File for I/O?**
Unlike the `Binary` type, which requires the entire payload to be loaded into a managed memory array before serialization, `Stream` and `File` types
support **Direct Transfer**. They allow the AdHoc protocol to pipe bytes directly from the external source (like a file stream or database blob) to
the **Socket Buffer**. This minimizes memory pressure, reduces garbage collection overhead, and avoids redundant memory-to-memory copies.

| If the data is...      | Use...               | Benefit                                                                                  |
|:-----------------------|:---------------------|:-----------------------------------------------------------------------------------------|
| **Already in RAM**     | `Binary`             | Simplest access to raw bytes as a native array.                                          |
| **On Disk / In DB**    | [`File`](#File)      | Optimized for known-size BLOBs; direct source-to-socket transfer.                        |
| **Continuous / Large** | [`Stream`](#Streams) | Interruptible, chunked transfer; allows "opaque forwarding" without loading into memory. |

***

**Example**

```csharp
using org.unirail.Meta;

class Result
{
    // Ideal for small, in-memory identifiers or fixed signatures
    [D(100)] Binary[] hash; 

    // If 'result' is a large blob coming from a DB, 
    // consider File or Stream for better I/O performance:
    [S(650_000)] File result_blob; 
}
```

## TYPEDEF

`Typedef` is employed to establish an alias for a data type, rather than creating a new type.
When multiple fields require the same (complex) type, consider declaring and using `TYPEDEF`.
This simplifies the process of modifying the data type for all related fields simultaneously.

In AdHoc, `TYPEDEF` is declared with a C# class construction containing the declaration of a **single** field named `TYPEDEF`.
The **name** of the class becomes an alias for the type of its `TYPEDEF` field.

For example, to adjust the default 255-character restriction for the `string` type, you would use the `[D]` attribute.

```csharp
class Packet{
    [D(+6)] string       string_field_with_max_6_chars;
    [D(+7_000)] string   string_field_with_max_7000_chars;
}
```

If multiple fields have the same type restriction, follow these...

```csharp
class max_6_chars_string{         // AdHoc typedef
    [D(+6)] string TYPEDEF;
}

class max_7000_chars_string{      // AdHoc typedef
    [D(+7_000)] string TYPEDEF;
}

class Packet{ //                         using typedef
    max_6_chars_string      string_field_with_max_6_chars;      
    max_7000_chars_string   string_field_with_max_7000_chars;   
    [D(100)] max_7000_chars_string   field_array_of_100_strings_with_max_7000_chars;   
}
```

## Pack/Enum type

Both `enums` and `packs` can serve as data types for a field.

- `Enums` are used to represent a set of named constant values of the same type.
- `Packs` are data structures designed to contain multiple fields with diverse data types.

By utilizing `enums` and `packs` as field data types, you can effectively organize and manage diverse data types in your code.

Within packs, you can nest types and even include self-referential fields within the data type definition.
This flexibility allows you to construct complex data structures with interconnected components.

> [!NOTE]  Nesting Depth (`_nested_max`)  
> To ensure protocol robustness, AdHoc calculates the maximum nesting depth of every `Pack` at compile time. Even when a `Pack` is rehydrated from a
> `FromStream`, the runtime enforces the `_nested_max` limit. This prevents resource exhaustion attacks (like "Deeply Nested Object" exploits) and
> allows the system to pre-calculate memory requirements.

`Empty packs` (those with no fields) or `enums` containing fewer than two fields used as data types will be represented as `boolean`.


<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company{
    /**
		<see cref = 'Client.RoomChangeResponse'                     id = '2'/>
		<see cref = 'Client.RoomChangeResponse.EnterRoomRequest'    id = '3'/>
		<see cref = 'RoomInfo'                                      id = '1'/>
		<see cref = 'Server.QuitRoomResponse'                       id = '0'/>
	*/
	public interface MyProject3{
        /**
         Flags for gimbal device (lower level) operation.
        */
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

result

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/4c485e72-fea2-4886-b1aa-28444657fe71)
</details>


------------

## Streams

In high-performance architectures—such as message routers, binary object stores, or drone telemetry proxy needs to transmit data without
understanding/inspecting its contents. AdHoc Protocol handles these scenarios via **Contextual Scoping**: the behavior of a field changes dynamically
based on
the communication path (the **Endpoint**) it travels.

This allows for "opaque forwarding" and "lazy deserialization," ensuring that services only parse the data they absolutely need for their specific
role.

---

**Channel Asymmetry**

Standard communication is symmetrical: both the Sender and Receiver require the full "infrastructure" (generated serializers, sub-type definitions,
and enum mappings) of a `Pack` to encode and decode it.

Stream modifier (`ToStream` and `FromStream`) break this symmetry by leveraging the concept of a **Typed Endpoint** versus an **Opaque Pipe**:

* **`ToStream<Endpoint, T>`**: The **Sender** requires the infrastructure for `T`.
	* The Sender treats the field as a structured `Pack` and serializes it.
	* The Receiver (if path to it matches the `Endpoint` scope) treats the field as **raw bytes**. It does not need the code for `T`. It acts as an
	* **Opaque Sink** (e.g., a database saving a BLOB or a file).
* **`FromStream<Endpoint, T>`**: The **Receiver** requires the infrastructure for `T`.
	* The Sender (if path to it matches the `Endpoint` scope) treats the field as **raw bytes**. It does not need the code or infrastructure for `T`.
	  It acts
	  as an **Opaque Source** (e.g., a disk reading bytes directly into the connection).
	* The Receiver rehydrates these bytes back into a fully structured `Pack` `T`.

This asymmetry allows middle-tier infrastructure (proxies, routers, stores) to remain lean and decoupled from the internal evolution of the Packs they
transport.

---

* **Using Patterns**

1. [x] **The Binary Object Store**

A common use for the Stream is a storage service that archives Packs and serves them back to clients. The store itself never needs the
generated code for the Packs it holds; it simply manages the byte flow.

```csharp
// Define the Store as the endpoint for raw byte handling
interface StoreEndpoint : IfSendingFrom<ChannelToStore, BinaryObjectStore> {}

class StoreRequest {
    public long object_id;
    
    // Client sends structured Pack -> Store receives raw bytes
    [S(1024 * 1024)]
    public ToStream<StoreEndpoint, UserProfile> data;
}

class StoreResponse {
    public long object_id;
    
    // Store sends raw bytes from disk -> Client receives structured Pack
    [S(1024 * 1024)]
    public FromStream<StoreEndpoint, UserProfile> data;
}
```

1. [x] **Robotics & Drones: Interleaving and Interrupts**

The **`Stream<To, From, Pack>`** (Universal Stream) is designed for real-time systems like drones, where high-bandwidth raw data (video/audio) must be
interleaved with low-latency structured control Packs over the same **Connection**.

* **Interleaving:** A drone can send a continuous video feed via the `Stream`. Because the stream is framed, the receiver can distinguish
  between a chunk of raw video and a structured `StatusUpdate` Pack.
* **Interruptibility:** The generic `Stream` type is the only **interruptible** flow. A sender can instantly terminate a low-priority video stream
  by sending a terminal zero-length chunk to prioritize an urgent structured `Command` Pack (e.g., "Emergency Land"). Once the high-priority data is
  sent, a new stream can begin.

---

**Comparison of Contextual Field Behaviors**

Assume **Endpoint E** is the designated route where the host acts as the "Opaque Pipe."

| Field Declaration          | Sender (at E)                 | On-the-Wire Format | Receiver (from E)             | Behavior on Other Routes |
|:---------------------------|:------------------------------|:-------------------|:------------------------------|:-------------------------|
| `MyPack p;`                | `MyPack` object               | Standard AdHoc     | `MyPack` object               | Same                     |
| `ToStream<E, MyPack> p;`   | `MyPack` object               | **Raw Bytes**      | **Raw Bytes** (`ExtBytesDst`) | Normal Pack              |
| `FromStream<E, MyPack> p;` | **Raw Bytes** (`ExtBytesSrc`) | **Standard AdHoc** | `MyPack` object               | Normal Pack              |

---

### Size Limits `[S(N)]`

Because streams can be large, AdHoc requires all stream-based fields (`ToStream`, `FromStream`, `Stream`, and `File`) to be annotated with an explicit
maximum total size in bytes using the **`[S(N)]`** attribute.

### File

While the `Stream` type uses chunking (`[length][data]...[0]`) for unknown or continuous feeds, the **`File`** type is an optimization for data with a
known size. It uses a single length prefix (`[total_length][data]`), making it the most efficient way to transfer disk-based BLOBs or memory buffers.
Unlike `Stream`, the `File` type is **not** interruptible.

## DateTime

In C#, the standard `DateTime` type is the default choice for working with dates and times. It offers an enormous range and high precision, but at a
fixed cost of **8 bytes (64 bits)** per value.

For network-sensitive applications, transmitting 64 bits for every timestamp is often inefficient. AdHoc provides three strategies to handle time,
ranging from standard convenience to highly optimized, context-aware compression.

**Key Definition:** AdHoc normalizes all time values to **Milliseconds** for transmission.

### 1. Standard `DateTime`

For general-purpose fields where bandwidth is not a critical concern, you can use the standard `DateTime` type directly.

* **Cost:** Fixed **8 bytes** (64 bits).
* **Behavior:** Transmits the full C# date range and precision.

```csharp
class Pack
{
    // Standard declaration. Uses 8 bytes on the wire.
    DateTime createdAt; 
}
```

---

### 2. Absolute Time (`DateTimeDef`)

Use `org.unirail.Meta.DateTimeDef` for long-term records anchored to a fixed point in history. This strategy is ideal for birth dates, registration
timestamps, or audit logs.

* **Mechanism:** `Value = (ActualTime - MinAnchor) / Precision`
* **Alignment:** **Bit-level**. AdHoc allocates the exact number of bits required to cover the range.

To use this, define a `struct` implementing `DateTimeDef`, then use that struct as the field type.

```csharp
public interface DateTimeDef
{
    DateTime min       { get; } // Anchor (e.g., 2020-01-01) by default  = DateTime.MinValue
    DateTime max       { get; } // End of range by default  = DateTime.MaxValue
    TimeSpan precision { get; } // Step size by default  = TimeSpan.FromMinutes(1)
}
```

```csharp
// 1. Define the configuration
struct RegistrationDate : DateTimeDef 
{
    // The fixed anchor point (e.g., system launch date)
    public DateTime min => new DateTime(2020, 1, 1); 
    // The resolution required (e.g., 1 minute)
    public TimeSpan precision => TimeSpan.FromMinutes(1);
    // 'max' is calculated automatically by AdHoc based on available bits
}

// 2. Use the struct as the field type
class UserProfile 
{
    // This field will use significantly fewer than 8 bytes
    RegistrationDate joinedAt; 
}
```

#### Optimization Strategy: Managing Spare Bits

AdHoc calculates the bits required to cover the range between `min` and `max` (e.g., 13 bits). Since data is stored in bytes, there is often "spare
capacity" (e.g., 3 spare bits in a 2-byte container).

You decide **how** it floats:

#### 1. Range Expansion (Default)

In this mode, your **precision is fixed**. AdHoc applies the spare bits to the **Tick Count**.

* **Precision:** Stays exactly as defined.
* **Max:** Floats significantly into the future.
* **Result:** You get a much longer lifespan (more cycles) for your data than you requested.

#### 2. Enhanced Precision (Prefix with `@`)

In this mode, your **precision floats**. AdHoc applies the spare bits to **Subdivide the Time Step**.

```csharp
public @TimeSpan precision => TimeSpan.FromSeconds(1);
```

* **Precision:** Becomes finer (smaller) than requested, down to a hard limit of **1 millisecond**.
* **Max:** Floats slightly. It adjusts just enough to ensure the total range is perfectly divisible by the new, finer precision.
* **Result:** You keep the time window close to what you requested, but gain higher fidelity.

---

### 3. Relative History (`TimeSpanDef`)

Use `org.unirail.Meta.TimeSpanDef` for cyclic data where "age" matters more than the specific date. This defines a **History Window** relative to "
Now." Ideally suited for real-time telemetry, sensor ring buffers, or logical message flows.

* **Examples:** Real-time telemetry, Sensor data, Ring buffers, Session activity tracking, Cache expiration windows.

```csharp
namespace org.unirail.Meta
{
    public interface TimeSpanDef
    {
        TimeSpan interval  { get; } // The History Window (e.g., "Last 27 Hours") by default  = TimeSpan.FromDays(1)
        TimeSpan precision { get; } // Step size by default  = TimeSpan.FromSeconds(1)
    }
}
```  

#### The "Boundary Crossing" Problem

In a cyclic system, a critical error occurs when network latency causes a packet to cross the cycle boundary (e.g., Midnight).

**The Scenario:**

* **Cycle Capacity:** 24 Hours.
* **Sender State:** Currently at **23:59:59** (End of **Cycle A**).
* **Payload:** The client reports an event that occurred at the **Start of Cycle A**.
  ```csharp
  CPULoad {
	 time: 0;       // Primitive 0 implies: "The very first tick of the current cycle"
	 CPULoad: 0.5;  // 50% Load
  }
  ```
* **Transmission:** Latency is **2 seconds**.
* **Receiver State:** Receives packet at **00:00:01**. The world has crossed into **Cycle B**.

**The Error:**

1. Receiver sees `time: 0`.
2. Receiver applies `0` to its current context (**Cycle B**).
3. **Result:** Decoder logs the event at the **Start of Cycle B** (Today, 00:00:00).
4. **Damage:** A **24-hour time jump error**. The event moved from "Yesterday" to "Now."

#### The Solution: 1 Minute Protection Gap

AdHoc eliminates this ambiguity by enforcing a **Safety Margin**.

1. **The Rule:** AdHoc internally reserves an extra **1 Minute** of capacity beyond the requested `interval`.
2. **The Logic:**
	* Physical Capacity = `Interval` + `1 Minute`.
	* When the packet arrives at `00:00:01` (Cycle B), the decoder checks the offset.
	* Because of the extended capacity, the decoder can mathematically determine that `0` belongs to the **Previous Cycle**, not the current one.

> **Usage constraints:**
> AdHoc exposes a calculated property named `EarliestValidDate`. Users must strictly operate on dates within the range of `now()` and
`EarliestValidDate`.
---

**AdHoc Code Generator Optimization Strategy**

For `TimeSpanDef`, AdHoc performs a 3-step calculation based on **Byte Boundaries** (1, 2, 3, 4... Bytes):

**Step 1: Byte Allocation**
AdHoc calculates the ticks needed for `Requested Interval`. It finds the smallest number of **Bytes** ($B$) required to hold that value.

* Container Capacity = $2^{8 \times B}$ (e.g., $2^8, 2^{16}, 2^{24}\dots$).

**Step 2: Ensure Gap Security**
It checks the **Spare Capacity** (Container Capacity - Requested Interval).

* If Spare Capacity < **1 Minute**: AdHoc adds **1 Byte** to the container size ($B+1$).
* This guarantees there is always room for the Protection Gap.

**Step 3: Refine Precision**
AdHoc utilizes the massive spare capacity of the Byte Container to improve data quality.

1. **Total Time:** `Requested Interval` + `1 Minute Gap`.
2. **New Precision:** `Total Time (ms)` / `Container Capacity`.
3. **Float Interval:** It adjusts the `interval` slightly to align with the new, high-fidelity precision.

---

#### Example: Robust CPU Monitor

We want to store CPU events for the **Last 27 Hours** with **1s** precision.

```csharp
public class CPUHistory : TimeSpanDef
{
    public TimeSpan interval  => TimeSpan.FromHours(27);
    public TimeSpan precision => TimeSpan.FromSeconds(1);
}
```

**Step 1: Container Sizing**

* **Request:** 27 Hours = 97,200 ticks.
* **Byte Check:**
	* 1 Byte ($2^8 = 256$): Too small.
	* 2 Bytes ($2^{16} = 65,536$): Too small.
	* 3 Bytes ($2^{24} = 16,777,216$): **Fits.**
* **Allocated:** 3 Bytes.

**Step 2: Gap Check**

* Container (16.7M) - Request (97.2K) = Massive Spare Space.
* Is Space > 60 ticks (1 min)? **Yes.**

**Step 3: Optimization**
We have a container that can hold ~16.7 million distinct values, but we only asked for 97,200 values. AdHoc uses this massive surplus to improve
resolution.

* **Total Time to Cover:** 27 Hours + 1 Minute Gap = **97,260,000 ms**.
* **Container Steps:** **16,777,216**.
* **Calculation:** $97,260,000 / 16,777,216 = 5.797...$
* **Round Up (Ceiling):** **6 ms**.

**Result:**
The user stores 3 Bytes. They requested **1s** precision, but AdHoc automatically upgraded it to **~6ms** precision because the 3rd byte provided
ample spare room.

* **Runtime Math:** The code generator passes `6` to the constructor.
* **Zero Floats:** All runtime calculations use clean integer multiplication/division (`tick * 6`)

---

### Summary Table

| Feature           | `DateTimeDef` (Absolute)             | `TimeSpanDef` (Relative)       |
|:------------------|:-------------------------------------|:-------------------------------|
| **Concept**       | Linear Timeline                      | Cyclic Ring Buffer             |
| **Anchor**        | Fixed Date (`min`)                   | Floating (`Now`)               |
| **Sizing**        | Fits in **Bits**                     | Fits in **Bytes** (1, 2, 3...) |
| **Safety**        | Clamps to range                      | **1 Min Protection Gap**       |
| **Spare Space**   | Extends `max` OR Refines `precision` | **Refines `precision`**        |
| **Latency Error** | Immune                               | Protected by Gap               |

## Meta

[downloads](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases)

* Ask questions you’re wondering about.
* Share ideas.💡
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

**This project respectfully acknowledges the invaluable contributions of the following third-party dependencies:**

1. **[Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp/4.12.0-1.final)**
	- This library provides essential .NET Compiler Platform ("Roslyn") support for the C# language, greatly enhancing our project's development
	  capabilities.

2. **[Microsoft.OpenApi.Readers](https://www.nuget.org/packages/Microsoft.OpenApi.Readers/2.0.0-preview2)**
	- This library delivers robust functionality for reading OpenAPI (Swagger) documents, significantly enriching our project's API documentation and
	  integration processes.

4. **[Cytoscape](https://js.cytoscape.org/)**
	- This JavaScript library is a powerful tool for visualizing complex networks and graphs, enabling advanced data representation and interaction
	  within our project.
