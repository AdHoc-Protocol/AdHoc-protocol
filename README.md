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
Its design prioritizes fast data throughput with minimal resource consumption, allowing you to serve more users or process more data on the same
hardware.

### 1. Best Fit: Data-Intensive Applications

AdHoc is particularly well-suited for systems where data volume, speed, and efficiency are paramount:

- **Financial Trading:** Managing real-time, high-frequency market data with minimal latency.
- **Customer Relationship Management (CRM):** Processing large datasets of customer interactions and transactions efficiently.
- **Enterprise Resource Planning (ERP):** Handling high-volume, real-time data updates in logistics, inventory, and operations.
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

- **Reduced Memory and CPU Usage:** Optimized binary formats require less memory and processing power for serialization/deserialization, enabling
  higher concurrency and resource efficiency.
- **Faster Serialization/Deserialization:** Minimized processing time for data transformation translates directly to lower latency.
- **Improved Network Efficiency:** Smaller binary packets reduce bandwidth usage and transfer times, enhancing overall throughput.

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
- Projects can be composed of other projects or selectively import specific components, such as channels, constants, or individual packs.
- Channels can be constructed from channels or their components, such as stages or branches.
- Packs can import or subtract individual fields or all fields of other packs.
- Provides a [`custom code injection point`](#custom-code-injection-point), where custom code can safely be integrated with the generated code.
- Provides built-in visualization tools through the **AdHoc Observer**, which can render interactive diagrams of network topology, pack field layouts, and data flow state machines.
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
- The generated code reuses buffers, starting from a minimum length of 127 bytes, with a preference for 256 bytes or larger. Buffer allocation for the
  entire packet is not required.

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

This command launches the **AdHoc Observer**, a powerful, web-based tool for visualizing, analyzing, and documenting your protocol definitions. It connects via WebSocket to receive live protocol data and renders it as a series of interconnected diagrams.

Example:

```cmd
    AdHocAgent.exe MyProtocol.cs?
```
The Observer is an integrated development environment for your protocol, allowing you to:
*   **Visualize High-Level Architecture:** See all hosts, the packs they handle, and the communication channels linking them in a clear, interactive graph.
*   **Drill into Data Flow Logic:** Right-click a channel to open a detailed pop-up view of its state machine, including all stages and branching logic.
*   **Inspect Data Structures:** Left-click a pack to instantly view its fields, data types, and nested structures in a dedicated diagram.
*   **Annotate and Document:** Double-click the background to create, edit, and save rich-text "stickers" (notes) directly on the diagrams.
*   **Navigate with Ease:** Use a searchable, collapsible tree view in the sidebar to quickly find and focus on any host, pack, or channel.
*   **Persist Your Workspace:** All layout customizations (node positions, pan, zoom) and annotations are automatically saved.

> **[See the full Observer User Guide](./Observer.md) for a detailed explanation of all features.**

![image](https://user-images.githubusercontent.com/29354319/232010215-ea6f4b1e-2251-4c3a-956d-017d222ab1e3.png)

![image](https://github.com/user-attachments/assets/565a76c2-58f3-4570-9ca8-c6bad41f4f43)

> [!NOTE]    
> To enable navigation from the Observer to your source code, specify the path to your local C# IDE in the `AdHocAgent.toml` configuration file.

### Saving Your Workspace (Layouts and Annotations)

The Observer automatically saves your workspace, including diagram layouts and annotations (stickers), into a dedicated folder.

*   **Location:** The data is saved in the current working folder of AdhocAgent.
*   **Manual Save:** To save the current state of your diagram, open sidebar and select **"Save Diagram"**.
*   **Recovery:** If you accidentally close the browser without saving, the Observer creates an `current_working_folder/unsaved`. You can move these filese to current_working_folder to recover your work.

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
`well_known`](https://github.com/protocolbuffers/protobuf/tree/main/src/google/protobuf)
> files and others.

The result of the .proto files transformation is only a starting point for your transition to the AdHoc protocol and cannot be used as is.
Reconsider it in the context of the greater opportunities provided by the AdHoc protocol.

## `.json` or `.yaml` Input

Specify that your input file is a Swagger/OpenAPI specification in `.json` or `.yaml` format. An optional second argument can be the
path to the output AdHoc protocol description `.cs` file.
If the second argument is skipped, the AdHocAgent utility will output the `.cs` file next to the provided OpenAPI file.
Do not expect a perfect result from the transformation, but this is a good starting point for the transition from OpenAPI specification
to the AdHoc protocol description.

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
	- The path to the local C# IDE binary. This allows the utility to open the IDE directly to specific source files at a specified line
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

Grub the `UUID` and once run the **AdHocAgent** utility.

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
		- ＃[Context.cs](/path/to/source/InCS/Agent/gen/Context.cs)
```

#### Configuring Deployment Targets

You specify where files go by adding extra Markdown links to the end of a line. The syntax is `[<regex_filter>](<destination_path>)`.

* The `regex_filter` is optional. If omitted (`[](/path)`), the rule applies to all files within that scope.
* The `destination_path` is the target location on your file system.

##### Target Path Behavior

**1. Copying into a Folder (Path ends with `/` or `\`):**
The source item is copied *inside* the destination folder, keeping its original name.

- **Folder:** `- 📁[Observer](...) [](/path/to/project/src/)`
	* The `Observer` folder will be copied to `/path/to/project/src/Observer`.
- **File:** `- 🌀[demo.ts](...) [](/path/to/project/components/)`
	* The `demo.ts` file will be copied to `/path/to/project/components/demo.ts`.

**2. Copying and Renaming (Path does NOT end with `/` or `\`):**
The source item is copied *as* the destination path.

- **Folder:** `- 📁[Observer](...) [](/path/to/project/NewObserverName)`
	* The `Observer` folder will be copied and renamed to `NewObserverName`.
- **File:** `- 🌀[demo.ts](...) [](/path/to/NewName.ts)`
	* The `demo.ts` file will be copied and renamed to `NewName.ts`.

##### Inheritance and Filtering

* **Inheritance:** Rules applied to a parent folder are automatically inherited by all its children.
* **Filtering:** You can provide a regular expression in the brackets to apply a rule only to matching files within a folder's hierarchy.

**Example:**
> [!TIP]
> Switch from Markdown preview to Markdown source to view detailed formatting.

```markdown

- 📁[Observer](/path/to/source/InTS/Observer)  ✅ Deploys all files to folder1, but images go to an assets folder.
  [\.(jpg|png|gif)$](/project/assets/images/)
  [](/project/src/folder1/)
	
	- 🌀[demo.ts](/path/to/source/InTS/Observer/demo.ts)  // Inherits /project/src/folder1/
	- 📁[gen](/path/to/source/InTS/Observer/gen)          // All files inside also inherit
```

##### Skipping Files and Folders

To exclude a file or folder from deployment, add `⛔` to the line or use an empty target `[]()`.

```markdown
- 📁[Observer](/path/to/source/InTS/Observer) [](/path/to/project/)
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
  #endregion > ǺÿÿČ.Project.Channel receiving  // <-- DO NOT EDIT THIS UID
  ```
- **Java/TypeScript**:
  ```typescript
  //#region > receiving
  // Your custom code goes here
  //#endregion > ǺÿÿČ.Project.Channel receiving // <-- DO NOT EDIT THIS UID
  ```

> [!CAUTION]
> **Never edit, move, or duplicate the `endregion` line or its Unique ID.** The UID is how the system finds your safe zone to preserve your code.
Changing it will cause your custom code to be permanently lost.

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
#endregion > ǺÿÿČ.Project.Channel receiving
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
original" to back up.

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

        // Defines a communication channel for exchanging data between the client and server
        interface Channel : ChannelFor<Client, Server>{
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
   Therefore, a `Pack` cannot have a field or nested pack with the same name as the pack.
>- Names should not match any keywords defined by the programming languages that the code generator supports. *
   *AdHocAgent** will check for and warn about such conflicts before uploading.

## Project

As a [`DSL`](https://en.wikipedia.org/wiki/Domain-specific_language) to describe **AdHoc
protocol** the C# language was chosen.
The `protocol description file` is essentially a plain C# source code file within a .NET project.

To create a `protocol description file`, follow these steps:

- Start by creating a C# project.
- Add a reference to
  the [AdHoc protocol metadata attributes.](https://github.com/cheblin/AdHoc-protocol/tree/master/src/org/unirail/AdHoc).
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
> In C# 10, file-scoped namespaces were introduced to simplify namespace declarations by eliminating the need for curly braces and reducing
> indentation levels.

This enhancement allows you to declare your namespace in a more concise manner:

```csharp
using org.unirail.Meta; // Importing AdHoc protocol attributes. This is required.

namespace com.my.company; // Your company's namespace. This is required.

public interface MyProject // Declare the AdHoc protocol description project as "MyProject."
{
    // Add your protocol description here
}

```

The **AdHoc protocol** not only defines the data for passing information, which includes packets and fields, but it also
incorporates features to describe the complete
network topology. This entails information about hosts, channels, and their interconnections.

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


        //FrontendServer, also known as the application server or server-side, handles core business logic,
        //manages data storage, and orchestrates various backend operations. This will be generated in JAVA.
        ///<see cref = 'InJAVA'/>
        struct FrontendServer/*ā*/ : Host{
            // Define packets that Server can create and send
            public class QueryDatabase/*Ą*/ : Root{
                private string? question; // The query string to be executed on the database
            }

            public  class PackB/*ą*/{ } // A specific packet type B, purpose to be defined based on system requirements

        }

        // BackendServer acts as the interface or entry point for clients (e.g., web browsers, mobile apps) to interact with the system.
        // It manages client requests, handles user authentication, and communicates with the FrontendServer. This will be generated in C#.
        ///<see cref = 'InCS'/>
        struct BackendServer/*ÿ*/ : Host{
            public class ReplyInts/*Ć*/ : Root{
                [D(300)] int[] reply; //Array containing a maximum of 300 integers as a response
            }

            public class ReplySet/*ć*/ : Root{
                [D(+300)] Set<int> reply; //Set containing a maximum of 300 unique integers as a response
            }
        }

        ///<see cref = 'InTS'/> // Indicates that this client will be implemented in TypeScript
        struct FullFeaturedClient/*Ă*/ : Host{
            public class Login/*Ă*/ : Root{
                private string? login;    // User's login identifier
                private string? password; // User's password for authentication
            }

            public class FullFeaturedClientPack/*ă*/{
                max_1_000_chars_string query; // A query string from the full-featured client, limited to 1000 characters
            }
        }

        ///<see cref = 'InCS'/> // Indicates that this client will be implemented in C#
        struct TrialClient/*ă*/ : Host{
            public class TrialClientPack/*ā*/{
                max_1_000_chars_string query; // A query string from the trial client, limited to 1000 characters
            }
        }

        ///<see cref = 'InTS'/> // Indicates that this client will be implemented in TypeScript
        struct FreeClient/*Ā*/ : Host{ } // Represents a free client with limited functionality

        // Define communication channels between hosts

        interface TrialCommunicationChannel/*ÿ*/ : ChannelFor<FrontendServer, TrialClient>{
            interface Start/*ÿ*/ : L,
                              _</*ÿ*/ // Defines packets that can be sent from FrontendServer to TrialClient
                                  Point3,
                                  Root,
                                  TrialClient.TrialClientPack
                              >,
                              R,
                              _</*Ā*/ // Defines packets that can be sent from TrialClient to FrontendServer
                                  Point3,
                                  TrialClient.TrialClientPack
                              >{ }
        }

        interface CommunicationChannel/*Ā*/ : ChannelFor<FrontendServer, FullFeaturedClient>{
            interface Start/*Ā*/ : L,
                              _</*ÿ*/ // Defines packets that can be sent from FrontendServer to FullFeaturedClient
                                  Point3,
                                  Root,
                                  TrialClient.TrialClientPack,
                                  FullFeaturedClient.Login,
                                  FullFeaturedClient.FullFeaturedClientPack
                              >,
                              R,
                              _</*Ā*/ // Defines packets that can be sent from FullFeaturedClient to FrontendServer
                                  Point3,
                                  TrialClient.TrialClientPack,
                                  FullFeaturedClient.FullFeaturedClientPack
                              >{ }
        }

        interface TheChannel/*ā*/ : ChannelFor<FrontendServer, FreeClient>{
            interface Start/*ā*/ : L,
                              _</*ÿ*/ // Defines packets that can be sent from FrontendServer to FreeClient
                                  Point3,
                                  Root
                              >,
                              R,
                              _</*Ā*/ // Defines packets that can be sent from FreeClient to FrontendServer
                                  Point3
                              >{ }
        }

        interface BackendCommunication/*Ă*/ : ChannelFor<FrontendServer, BackendServer>{
            interface Start/*Ă*/ : L,
                              _</*ÿ*/ // List of packets that can be sent from FrontendServer to BackendServer
                                  FrontendServer.QueryDatabase,
                                  Point3,
                                  FrontendServer.PackB
                              >,
                              R,
                              _</*Ā*/ // List of packets that can be sent from BackendServer to FrontendServer
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

By selecting a specific channel in the AdHocAgent utility viewer, you can view detailed information about the packets
involved and their destinations.
This feature allows you to track the specific path taken by the packets within the network.

![image](https://github.com/user-attachments/assets/04a3ae72-665b-4579-9c04-6304fdf7b991)
![image](https://github.com/user-attachments/assets/895b9268-1a06-467f-8337-7d4b14d7f87f)
</details>

Please note that after processing the file with AdHocAgent, it assigns packet ID numbers to the packets.
These numbers help in identifying and tracking the packets within the system.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/51163c18-3b49-4f4f-adea-c3450c0fe01c)


> [!NOTE]  
> A project can function as a [set of packs](#projecthost-as-a-named-pack-set). Keep this in mind when organizing packet hierarchy.

### Extend other Project

You can create a protocol description project by importing enums, constant sets, channels, or other projects.

To import all components, extend the desired source projects as C# interfaces in your project's interface:

```csharp
interface MyProject : OtherProjects, MoreProjects
{
}
```

> [!NOTE]  
> The order of the extended interfaces determines priority in cases of full name or pack ID conflicts. Projects listed first take precedence.

To exclude specific imported entities, reference them in the project's XML documentation using the [
`<see cref="entity"/>-`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute) attribute:

```csharp
/// <see cref="MoreProjects.UnnecessaryPack"/>-
/// <see cref="OtherProjects.UnnecessaryChannel"/>-
/// <see cref="OtherProjects.UnnecessaryChannel.Stage"/>-
interface MyProject : OtherProjects, MoreProjects
{
}
```

> [!NOTE]  
> Note the **minus** character after the attribute to exclude the entity.

To import only specific enums, constant sets, channels, list them in the project's XML documentation using the `<see cref="entity"/>+` attribute:

```csharp
	/// <see cref="SomeProject.Pack"/>+
	/// <see cref="FromProjects.Channel"/>+
	interface MyProject : OtherProjects, MoreProjects
	{
	}
```

> [!NOTE]  
> Note the **plus** character after the attribute to import the entity.  
> You cannot import `Stages` this way.

> [!NOTE]  
> To import a **host** from another project, reference the host as the endpoint within the project's channels.  
> To import a **pack** from another project, reference the pack in a branch of a stage within the project's channels.

[Learn how to modify imported packs](#modify-imported-packs).  
[Learn how to modify imported channels](#modify-imported-channels).

For example, the protocol description in [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs) defines
public, external communications. However, on the **Server** side, the backend infrastructure requires an internal communication protocol to handle
tasks such as:

- Distributing workloads
- Sending and receiving metrics
- Managing database records
- Implementing authentication and authorization

**Options for Protocol Extension:**

1. **Create a Separate `Backend` Protocol Description**  
   This option is viable if you do not plan to pass packets through both protocols.

2. **Extend the Existing `AdHocProtocol` Description**  
   This approach is suitable if you want to combine both protocols in a single `Server` host.
   The example below demonstrates how to extend the `AdHocProtocol` to include backend-specific protocol details.
   
   ```csharp
   using org.unirail.Meta;

   namespace org.unirail
   {
       public interface AdHocProtocolWithBackend : AdHocProtocol 
       {
           // Backend-specific protocol details can be added here
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

        interface ChannelToMetrics : ChannelFor<Server, Metrics> {
            interface One : L,
                            _<
                                MetricsData,
                                One
                            > { }
        }

        interface ChannelToAuthorizer : ChannelFor<Server, Authorizer> {
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

In this example, the `AdHocProtocolWithBackend` protocol description import all entities from `AdHocProtocol` and introduces several components:

- **Hosts**:
	- Two new hosts have been defined: `Metrics`, implemented in **C#**, and `Authorizer`, implemented in **Java**.

- **Packs**:
	- The `AuthorisationRequest` and `MetricsData` pack is created to be sent from the `Server` via `ChannelToAuthorizer`.
	- Two packs, `AuthorisationConfirmed` and `AuthorisationRejected`, are defined within the `Authorizer`. One of these will be sent as a reply to
	  the `AuthorisationRequest` from the `Server`.

- **Channels**:
	- `ChannelToMetrics` connects the `Server` and `Metrics`.
	- `ChannelToAuthorizer` connects the `Server` and `Authorizer`.

> [!IMPORTANT]
> If your solution requires working with multiple protocols, you cannot easily combine their generated protocol-processing code within the same VM
> instance
> due to `lib` **org.unirail** namespace clashes. To resolve this, assign each project’s `lib` to a distinct namespace.

## Hosts

In the AdHoc protocol, "hosts" refer to entities that actively participate in the exchange of information.
These hosts are represented as C# `structs` within a project's `interface` and implement the `org.unirail.Meta.Host` marker interface.

To specify the programming language and options for generating the host's source code, use the XML [
`<see cref="entity">`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute)
tag in the code documentation of the host declaration.

The built-in marker interfaces such as `InCS`, `InJAVA`, `InTS` and others allow you to declare language configuration scopes.

`Packs` and `Fields` type entities referenced within a particular language scope will inherit the configuration specified by that scope.
The latest language configuration scope becomes the default for the subsequent `Packs` and `Fields` entities within the host.

```csharp
using org.unirail.Meta;

namespace com.my.company // Your company namespace. Required!
{
    public interface MyProject
    {
        /**
        <see cref='InCS'/>+-
        <see cref='InJAVA'/>
        <see cref='ToAgent.Result'/>
        <see cref='Agent.ToServer.Proto'/>
        <see cref='Agent.ToServer.Login'/>
        <see cref='InJAVA'/>--
        */
        struct Server : Host
        {

        }
    }
}
```

AdHocAgent utility could be read the `Server` configuration in this manner..
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0cfa47f2-8b2e-4e49-9c7d-0fd908dbd7ce)

</details>


> [!NOTE]  
> A host can serve as a [set of packs](#projecthost-as-a-named-pack-set). Keep this in mind when organizing the host's internal packet hierarchy.

### The Multi Context Host

In a simple application, you might have one continuous interaction (a single "state machine") per connection. However, complex applications often need
to manage multiple, independent tasks or sessions over that same single connection.

For example, on a game server, a single player connection might need to handle:

* In-game chat (one context)
* A live trade with another player (a second context)
* A background quest update (a third context)

Trying to manage these distinct tasks within a single state machine is complex and error-prone. The **Multicontext Host** is AdHoc's solution to this
challenge.

**Core Concepts**

1. **Context as an Independent Task:** A "Context" is an isolated state machine with its own state and data. It represents a single, logical task.
2. **State Isolation:** Each Context's state is completely separate from others, even though they share the same network connection.
3. **Shared Packet Types:** All Contexts on a Host use the same defined set of request and response packets.
4. **Automatic Routing:** The AdHoc runtime automatically tags outgoing packets with a Context ID and routes incoming packets to the correct Context
   instance. You don't have to manage this manually.

**Defining a Multicontext Host**

To enable this feature, you implement the `org.unirail.MultiContextHost` interface instead of the basic `org.unirail.Host` interface. The compiler
will then
require you to define the `Contexts` property, which specifies the maximum number of concurrent contexts that can run on this Host per connection.

```csharp
using org.unirail.Meta;

struct Server : MultiContextHost
{
    // This server can handle up to 10 independent contexts (tasks) on a single connection.
    public int Contexts => 10;
}
```

> **Warning:** Be mindful that each active Context consumes memory. Choose a number that balances your application's needs with its resource budget.

**Benefits of the Multicontext Approach**

* **Modularity:** Build your application as a set of clean, decoupled logical units.
* **State Safety:** Prevent state conflicts between different tasks, making your code safer and easier to debug.
* **Efficiency:** Manage many concurrent operations over a single, efficient network connection instead of multiple connections.
* **Simplicity:** Your code within each Context only needs to care about its own state and logic, making it dramatically simpler.

In short, the Multicontext feature allows you to build sophisticated, modular, and scalable network applications by layering multiple independent
interactions over a single physical connection.

### Modify Imported Hosts

You can adjust the language implementation configuration of imported `hosts`. For example:

```csharp
/**
<see cref='InJAVA'/>
<see cref='Pack'/>
<see cref='InJAVA'/>-- 
*/
struct ModifyServer : Modify<Server> { }
```

In this example, we add information to the imported host `Server`, imply that `Pack` should be fully implemented in Java.

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

To define a **Named Pack Set**, use C# interface construct. For example:

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

**Named packet sets** can be declared anywhere within your project and may contain references to individual pack, other **Named packet sets**,
projects, or hosts.Once you have defined a **Named Pack Set**, you can reference it in your code wherever the set of packets is needed. For example:

### Project, Host, or Pack as a Named Pack Set

You can treat a **Project**, **Host**, or **Pack** as a **Named Pack Set** to automatically include all transmittable packets defined directly within
their scope. This approach simplifies organizing and managing large packet sets hierarchically.

```csharp
interface Info_Result:
    _<
    	Server.Info,
    	Server.Result
        Project,  //  includes all transmittable packets directly within the Project's scope.
        Host      // includes all transmittable packets directly within the Host's scope.    
    >{}
```                  

To include **all transmittable packets recursively** (including those in nested structures) within a **Project**, **Host**, or **Pack**, prefix the
reference with the `@` symbol. For example:

```csharp
interface Info_Result:
    _<
    	Server.Info,
    	Server.Result
        @Project, // includes all transmittable packets recursively within the Project.
        Host,      // includes all transmittable packets directly within the Host's scope. 
		X<
		    Packs, //excludes the specified packets
		    Need, 
		    @ToDelete 
		>
    >{}
```    

## Empty packs, Constants, Enums

### Empty Packs

A **transmittable** (referenced(registered) in a channel) C# class-based pack that contains no instance fields, but various types
of [constants](#constants) or nested declarations of other packs.
Implemented as singletons, it offers the most efficient way to signal simple events or states via a channel.

> [!NOTE]  
> When constructing a pack hierarchy, you may encounter an `Empty pack` that is unexpectedly transmittable over the network. This is undesirable if
> the pack’s sole purpose is to define the hierarchy structure.
> To prevent transmission, switch to use a C# struct-based [Constant Container](#constant-container) that remains non-transmittable while still
> fulfilling its hierarchy organizational role.

### Constant Container

A **non-transmittable** C# struct-based pack that may contain various types of [constants](#constants) or nested declarations of other packs.
Declaring instance fields is not allowed.
**Constant Container** is primarily used to define the hierarchy structure and deliver metadata to generated code. It can be declared anywhere within
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

        interface CommunicationChannel : ChannelFor<Server, Client> { }
    }
}
```

</details>


----

### Enums

Enums are used to organize sets of constants of the same primitive type:

- Use the `[Flags]` attribute to indicate that an enum can be treated as a bit field or a set of flags.
- Manual assignment of values is not required, except when an explicit value is needed. Enum fields without explicit initialization
  are automatically assigned integer values, with the `[Flags]` attribute ensuring that each field is assigned a unique bit.


- [!NOTE]
- `Enums` and all constants are replicated on every host and are not transmitted during communication.
- They serve as local copies of the constant values and are available for reference and use within the respective host's scope.

### Modify Enums and Constants

Enums and constants can be modified like a [simple pack](#modify-imported-packs), **but the modifier is discarded after the modification is applied.**

## Packs

Packs are the smallest units of transmittable information, defined using a C# `class`. Pack declarations can be nested and placed anywhere within a
project’s scope.

The instance **fields** in a pack's class represent the data it transmits. A pack may also contain various types of [constants](#constants) or nested
declarations of other packs.
> [!NOTE]  
> A pack can be used as a [set of packs](#projecthost-as-a-named-pack-set). Keep this in mind when organizing the pack hierarchy.

To include or inherit:

- **All fields** from other packs: Use C# class inheritance (To inherit fields from multiple packs, use the `org.unirail.Meta._<>` wrapper.)   
  or use the `<see cref='Path.To.Pack'/>+` line in the target pack’s XML documentation.
- **Specific fields** from other packs: Use the `<see cref='Path.To.Pack.field'/>+` line in the target pack’s XML documentation.

> [!NOTE]
> Inherited fields cannot override existing or previously inherited fields with same name.

To remove:

- **All fields** with the same names as another `Pack`: Use the `<see cref="Full.Path.To.Source.Pack"/>-` line in the target pack’s XML documentation.
- **Specific fields** with the same names as referenced: Use the `<see cref="Full.Path.To.OtherPack.RemoveField"/>-` line in the target pack’s XML
  documentation.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company2 {
    public interface MyProject {
        public class AnyPacksFields {
            long   received_time;
            long   global_uid;
            string client_hashcode;
        }

        ///<see cref = 'InJAVA'/>
        struct Server : Host {
            ///<see cref="AnyPacksFields"/> Embeds all fields from AnyPacksFields.
            ///<see cref="AnyPacksFields.client_hashcode"/> but excludes the `client_hashcode` field from AnyPacksFields.
            class ServerPack { } // Packets that the Server can create and send through this port
        }


        ///<see cref = 'InTS'/>
        struct Client : Host {
            class Login : _<AnyPacksFields> // Embeds all fields from AnyPacksFields
            {
                string user;
                string password;
            }
        }

        interface CommunicationChannel : ChannelFor<Server, Client> { }
    }
}
```

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/c3cd9f94-7c6f-486e-a60b-c156b5342d5f)

</details>

### Field Injection

The `FieldsInjectInto` Interface allows you to define a "template" class containing fields that are automatically injected into the **payload** (the
main
data body) of other packets. This is ideal for adding common fields without repetitive code. The template class itself
is not preserved as a packet; only its fields are distributed.

`FieldsInjectInto< PackSet >`inject fields only into transmittable packets defined within the specified [`PackSet`](#projecthost-as-a-named-pack-set)

**Example:**

Let's define a set of common fields and apply them to all packets in `MyProject` except for `Point2d`.

```csharp
// Rule: Add 'name' and 'length' to all packets in MyProject, but not to Point2d.
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

The source generator processes these rules and effectively transforms the code:

* `Point2d` is unchanged because it was in the `X<Point2d>`.
* `Point3d` is modified to inject the fields from `CommonFields`.

```csharp
// Final structure of Point3d after generation
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
> If a target packet already contains a field with the same name as a field being injected, the target's original field is removed and replaced by the
> one from the injector. The injector's field definition (including its type, attributes, and documentation) takes precedence.
---

#### Modify Imported Field Injection

The `Modify Imported Field Injection` feature extends [Modify Imported Packs](#modify-imported-packs) but targets a specific `TargetFieldInjection`.
Use the `org.unirail.Meta.Modify<TargetFieldInjection>` modifier and specify a **PackSet** to add or remove fields from the target injector.

```csharp

class FieldInjectionModifier : Modify<TargetFieldInjection>, _<AddPack, X<RemovePack>>  {
    string name; //add field in the imported injector
    int length;
}
```

### Headers

A **packet header** contains protocol-level metadata fields separate from the **application payload**. These fields manage critical network tasks such
as routing, identification, and session management.

**Key Characteristics of Headers**

- **Transmission Order**: Headers are always sent and received *before* the payload and are directly accessible in network event handlers.
- **Data Types**: Header fields must use primitive, **non-nullable❗**types (e.g., `bool`, `int`, `long`).
- **Scope**: Headers are only present in **Standalone Packets**, not in **Sub-Packets**.
	- **Standalone Packet**: A packet sent or received directly over the network, always including a header.
	- **Sub-Packet**: When a packet definition is used only as the type for a field in another packet, it acts as a "Sub-Packet." In this role, it
	  contributes only its payload to the parent packet's data. It does not have headers.

#### Adding Header Fields

Header fields can be added to standalone packets in three ways:

1. **Implicit (Automatic)**: Every standalone packet includes a `packet_id` field for identification.
2. **Conditional (Feature-Based)**: If the [MultiContextHost](#the-multi-context-host) is used, a `context_id` field is added to the headers of all
   host
   packets.
3. **Explicit (User-Defined)**: You have the flexibility to define your own custom header fields. This is achieved by creating a "Header" class that
   implements the `HeaderFor< PackSet >` interface from the `org.unirail.Meta` namespace, where [PackSet](#pack-set) is the specific set of one or
   more packet types that you want the custom header to target.

#### Header Scope

The scope of an explicit header depends on where the `Header` is declared:

1. **Channel-Specific Scope (Highest Precedence)**:
	- Applies only to packets sent through a specific channel.
	- **How**: Declare `HeaderFor<PackSet>` within a `Channel` interface.

2. **Host-Specific Scope**:
	- Applies only to packets sent from or received by a specific host.
	- **How**: Declare `HeaderFor<PackSet>` within a `Host` definition.

3. **Project Scope (Lowest Precedence)**:
	- Applies globally to all specified packets across the project, unless overridden by a more specific scope.
	- **How**: Declare `HeaderFor<PackSet>` at the project’s top level.

**Precedence Rule**: Channel-Specific **▷** Host-Specific **▷** Project Scope.

**Example**

The following example assigns different header fields to `Point2d` and `Point3d` packets:

```csharp
// Project-scope header for Point2d packets
class HeaderFor2dEntities : HeaderFor<Point2d> {
    int plain_id;
    int session;
}

/**
 * See also: InCPP, InTS, InJAVA, InCS
 */
struct NodeB : Host {
    // Host-specific header for Point3d packets
    class HeaderFor3dEntities : HeaderFor<Point3d> {
        int world_id;
        int session;
    }
}

// Packet definitions
class Point2d {
    float X;
    float Y;
}

class Point3d {
    float X;
    float Y;
    float Z;
}

// Communication channel definition
interface CommunicationChannel : ChannelFor<NodeA, NodeB> {
    // Channel-specific header for TeamCoordination packets
    class CommunicationChannelHeader : HeaderFor<TeamCoordination> {
        uint sequence_num;
        ushort priority;
    }
}
```

**Resulting On-the-Wire Packet Structure**

The final packet structure combines implicit, conditional, and explicit headers.

**`Point3d` Packet (Host-Specific Header)**:
When sent via *any* channel:

```
[-- HEADER --]
  packet_id   (Implicit)
  context_id  (Conditional: if MultiContextHost is used)
  world_id    (Host-Specific: from HeaderFor3dEntities)
  session     (Host-Specific: from HeaderFor3dEntities)
[-- PAYLOAD --]
  X
  Y
  Z
```

**`TeamCoordination` Packet (Channel-Specific Header)**:

- **When sent via `CommunicationChannel`**:

```
[-- HEADER --]
  packet_id      (Implicit)
  context_id     (Conditional: if MultiContextHost is used)
  sequence_num   (Channel-Specific: from CommunicationChannelHeader)
  priority       (Channel-Specific: from CommunicationChannelHeader)
[-- PAYLOAD --]
  ... (TeamCoordination fields)
```

- **When sent via any other channel**:

```
[-- HEADER --]
  packet_id   (Implicit)
  context_id  (Conditional: if MultiContextHost is used)
[-- PAYLOAD --]
  ... (TeamCoordination fields)
```

#### Modify Imported Header

The `Modify Imported Header` feature extends [Modify Imported Packs](#modify-imported-packs) but targets a specific `TargetHeader`.
Use the `org.unirail.Meta.Modify<TargetHeader>` modifier and specify a **PackSet** to add or remove fields from the target injector.

```csharp

class HeaderModifier : Modify<TargetHeader>, _<AddPack, X<RemovePack>>  {
    string name; //add field in the imported header
    int length;
}
```

### Value Pack

**Value Pack** is a highly efficient data structure that packs multiple fields into a single **up to 8-byte** primitive type.
This design offers significant performance and memory advantages while maintaining type safety and ease of use.

Key Features

- **Zero Heap Allocation**: Data is stored in value types, eliminating garbage collection overhead.
- **Compact Memory Layout**: Multiple fields are efficiently packed into eight bytes or fewer.
- **Type Safety**: Full compile-time validation and type-checking. Fields must be primitive numeric types or other Value Packs.
- **Automatic Implementation**: All fields are always implemented. The code generator automatically produces optimized packing and unpacking methods.

Memory Layout

The code generator performs the following steps:

1. Analyzes field layouts to calculate the required space.
2. Choose the appropriate primitive type for storage.
3. Generates optimal packing/unpacking code to ensure efficiency.

```csharp
// Example: 6 bytes total, packed into an 8-byte primitive (long)
class PositionPack {
    float x;     // 4 bytes
    byte layer;  // 1 byte
    byte flags;  // 1 byte 
}
```

In this example:

- The `PositionPack` requires **6 bytes**, which doesn't fit in a 4-byte primitive like `int`, so it uses an **8-byte primitive** like `long`.

#### Smart Flattening

AdHoc code generator automatically flattens nested **single-field** `Value Packs` to eliminate unnecessary wrapping, enhancing compactness.

**Basic Flattening**

```csharp
// Before flattening
class Temperature {
    float celsius;
}

class SensorReading {
    Temperature measurement;
}

// After flattening
class SensorReading {
    float measurement;  // Directly stores the temperature value
}
```

This flattening occurs because `SensorReading` is essentially just a copy of `Temperature` and does not add any extra data.

**Nullability Preservation**

Flattening preserves nullability throughout the pack chain:

```csharp
// Before flattening
class Pressure {
    float kilopascals;
}

class PressureSensor {
    Pressure? reading;  // Nullable Pressure
}

// After flattening
class PressureSensor {
    float? reading;  // Nullability is preserved
}
```

**Deep Flattening**

Value Pack can flatten arbitrarily deep chains of **single-field** types:

```csharp
// Before flattening
class Voltage { float volts; }
class PowerLevel { Voltage? level; }
class DeviceStatus { PowerLevel? power; }

// After flattening
class DeviceStatus {
    float? power;  // Fully flattened with preserved nullability
}
```

another example

```csharp
class FloadWraper{ float field;}
class FloadWraperNullable{ float? field;}

// The following fields have the same type: Set<float?>.
Set<FloadWraper?>          set_of_nullable_float;                
Set<FloadWraperNullable>   set_of_nullable_float; 
Set<FloadWraperNullable?>  set_of_nullable_float; 
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

This approach allows you to add, remove and replace fields from an imported pack.



> [!NOTE]  
> A modifier pack can function as a normal pack.

## Channels

In the **AdHoc protocol**, channels serve as communication pathways connecting hosts. They are declared using a C# `interface` and, like hosts,
are defined directly within the project’s body. Channels must extend the built-in `org.unirail.Meta.ChannelFor<HostA, HostB>` interface and specify
the two hosts they connect as generic parameters.

Example:

```csharp
using org.unirail.Meta;

namespace com.company{
    public interface MyProject{
        ....
        ...
        interface Communication : ChannelFor<Client, Server>{ }
    }
}
```

![image](https://github.com/user-attachments/assets/dd47301d-4f2b-4648-ab1b-8f00f40ce271)

To clearly define which packets are sent through the channel, their order, and the responses, the channel's `interface`
body should include [`stages`](#stages) and [`branches`](#branches). These elements specify the data flow logic between the connected hosts.

A channel can import content from other channels by extending them. To swap the content hosts being imported,
wrap the importing channel with built-in `org.unirail.Meta.SwapHosts<Channel>`.

For example:

```csharp
interface CommunicationChannel : ChannelFor<Server, Client>, SomeCommunicationChannel, SwapHosts<TheChannel> { }
```

Implementation:

The AdHoc protocol implementation features channels designed to connect the EXTernal network with the INTernal host. Each channel comprises processing
layers,
each containing both an **EXT**ernal and **INT**ernal side. The abbreviations INT and EXT are consistently employed throughout the generated code to
denote
internal and external aspects.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/234749384-73a1ce13-59c1-4730-89a7-0a182e6012eb.png)

</details>

> [!IMPORTANT]  
> **[The little-endian format is used for data representation on the wire.](https://news.ycombinator.com/item?id=25611514)**

### Stages

Stages represent distinct processing states within a channel's lifecycle.
The implementation follows established [state machine patterns](https://en.wikipedia.org/wiki/Finite-state_machine) similar to

- [**Spring Statemachine**](https://spring.io/projects/spring-statemachine): A robust state machine library for Java with hierarchical states and
  transition guards.
- [**xstate**](https://github.com/statelyai/xstate): A JavaScript/TypeScript library for creating and interpreting statecharts, featuring visual
  tools.
- [**squirrel-foundation**](https://github.com/hekailiang/squirrel): A lightweight Java library for building hierarchical and concurrent state
  machines.
- [**StatefulJ**](https://www.statefulj.io/): A Java-based state machine framework tailored for RESTful services.
- [**stateless4j**](https://github.com/stateless4j): A minimalist state machine library for Java with a clean API.
- [**zustand**](https://github.com/pmndrs/zustand): A lightweight state management library for React with minimal boilerplate.
- [**Jotai**](https://jotai.org/): A modern and scalable state management library for React focused on atomic state pieces.
- [**Easy Peasy**](https://easy-peasy.vercel.app/): A simple and intuitive React state management library built on Redux.

providing a structured approach to state management.


> [!NOTE]   
> In the AdHoc protocol, the state machine is driven by packet transmission and timeout events.
> It is designed to outline the overall system architecture rather than to provide exhaustive details about every aspect.  
> The code generated from your dataflow description based on stages and branches.
> Developers are encouraged to adapt and integrate this code into their implementations, making decisions and responding to deviations from the
> standard flow.
> For a practical usage example, you can search for `Communication.Stages` in the
> [ChannelToServer.cs](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/ChannelToServer.cs) file of the AdHoc Protocol GitHub
> repository.

Let's examine the practical use of the communication flow in the
[`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/acfc582c971914a4a86f3458d4b85a141a787d3c/AdHocProtocol.cs#L443) protocol
description file.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

<img width="1120" height="2392" alt="image" src="https://github.com/user-attachments/assets/3b059e62-6fb3-482a-b6d3-1ba56ef8af56" />

The diagram on the **right** illustrates the communication channels lifecycle, which is declared in the code .  
To view the communication flow diagram in the **Observer**, run the **AdHocAgent** utility from the command line:
   ```cmd
      AdHocAgent.exe /path/to/AdHocProtocol.cs?
   ```

Once the diagram opens, right-click on a channel's link. Resize the opened channels window to display all channels.

### Top Diagram: Communication: Agent ↔ Server

This diagram illustrates the stateful communication protocol between an `Agent` and a `Server`. The flow progresses through distinct stages, from
initial connection and authentication to task processing.

**1. Start & Version Matching**

* **Initial State:** The process begins with the `Agent` in a `Start` state.
* **Agent's Action:** The `Agent` initiates communication by sending its `Version` information to the `Server`, which is in a `VersionMatching` state.
* **Server's Action:** The `Server` validates the `Agent`'s version.
	* **If Versions Mismatch:** The `Server` sends an `Info` packet containing an error description. The red stop symbol (⛔) indicates this is a
	  terminal action, and the `Server` closes the connection.
	* **If Versions Match:** The `Server` sends an `Invitation` packet, signaling that the `Agent` can proceed to the authentication stage.

**2. Login & Authentication**

* **State Transition:** Upon receiving the `Invitation`, the `Agent` transitions to the `Login` state, while the `Server` moves to the `LoginResponce`
  state to await credentials.
* **Agent's Action:** From the `Login` state, the `Agent` sends either a `Login` packet (for existing users) or a `Signup` packet (for new users).
* **Server's Action:** The `Server` processes the request. A failed attempt results in an `Info` packet. A successful authentication is confirmed with
  another `Invitation` packet.
* **Agent State Transition:** After a successful login, the `Agent` moves to the `TodoJobRequest` state, ready to submit tasks.

**3. Data Request & Response (Task Processing)**

* **Agent's Action:** From the `TodoJobRequest` state, the `Agent` can send one of two types of tasks to the `Server`:
	1. **Project Task:** The `Agent` sends a packet containing either a new `Project` or a `RequestResult` for a project that was sent earlier. The
	   `Server` then transitions to its `Project` processing state.
	2. **Proto Task:** The `Agent` sends a `Proto` packet (likely structured data like a protocol buffer). The `Server` then transitions to its
	   `Proto` processing state.
* **Server's Action & Response:** The `Server` processes the received task (`Project` or `Proto`). Afterward, it sends a single, final response and
  terminates the connection.
	* On **success**, the `Server` sends a `Result` packet with the task's outcome.
	* On **failure**, the `Server` sends an `Info` packet with an error description.
	* The red stop symbol (⛔) beside both `Result` and `Info` indicates that the connection is closed after this final packet is sent.

---

### Bottom Diagram: ObserverCommunication: Agent ↔ Observer

This diagram illustrates a separate, persistent communication channel between the `Agent` and an `Observer`.

**1. Start & Initial Synchronization**

* **Initial State:** The `Agent` begins in a `Start` state for this channel.
* **Agent's Action:** The `Agent` initiates the session by pushing its initial state to the `Observer`. This includes a `Layout` packet (defining UI
  structure)
  and a `Project` packet (containing initial project data).
* **Observer's Action:** The `Observer` receives this data to render its initial view.

**2. Observer-driven Operations**

* **State Transition:** After initialization, the system enters a continuous interactive loop where the `Observer` drives the actions.
* **Observer's Action:** The `Observer` in the `Operate` stage can send to the `Agent` packs:
	* `Up_to_date`: A request to check if the `Observer`'s data is current.
	* `Show_Code`: A request for the `Agent` to provide and show source code in the IDE.
	* `Layout`: A command to same modified diagram layout.
* **Agent's Action & Response:** The `Agent` in the `RefreshProject` stage can reply:
	* With updated `Project` if the command required it (`Up_to_date`).
	* Or an `Up_to_date` status confirmation if no update occurs.
* This cycle allows the `Observer` to stay synchronized with the `Agent` and request operations as needed.

---

</details>



Each stage is declared within the channel scope using a C# `interface`, where the `interface` name becomes the stage name.
The topmost stage represents the initial state.  
The code generator collects the stages of a channel by initiating a traversal from the top stage.
Any stages that are not reachable from the top will be disregarded.

A stage's `interface` extends the built-in interfaces `org.unirail.Meta.L`, `org.unirail.Meta.R`, or `org.unirail.Meta.LR`.  
Here, `L` and `R` represent the left and right hosts of the channel to which the stage belongs, respectively, while `LR` denotes both hosts.

The declaration of branches begins right after denote host side.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/1cd6ad55-7e0e-4167-9d4a-fef279b4fa11)

It is possible for only one side to be able to send packets.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/f1cdc9e3-9e14-4781-af7b-ce46b3dc5234)

> [!WARNING]   
> A short `block comment` with some symbols `/*įĂ*/` represents auto-sets unique identifiers.
> These identifiers are used to identify entities. Therefore, you can relocate or rename entities, but the
> identifier will remain unchanged.
> It is important to never edit or clone this identifier.

#### Branches

After referencing a host side (`L`, `R`, or `LR`), `sending` packets are organized into multiple `branches`. A `branch` consists of the specified [
`PackSet`](#projecthost-as-a-named-pack-set) of
`sending` packets
and may optionally include a reference to the target `stage`, which the host will transition to, after sending any packet from the list.

- If the target `stage` is a reference to the built-in `org.unirail.Meta.Exit`, the receiving host will terminate the connection after receiving any
  packet from the branch.
- If a branch does not explicitly reference a target `stage`, it implicitly references its own stage — the stage to which the branch belongs.
  This implies that the current stage is permanent.

For example, this ( the branch implicitly references to the self stage `Login`):

```csharp
interface Login /*ā*/ : L,
                        _< /*Ă*/
                            Agent.Name,
                            Agent.Signup
                        >
            { }
```

is tha sames as this  ( the branch explicitly references to the self stage `Login`) :

```csharp
interface Login /*ā*/ : L,
                        _< /*Ă*/
                            Agent.Name,
                            Agent.Signup,
                            Login
                        >
            { }
```

- If a channel's hosts have the same branches layout, use `LR` to avoid duplicate declarations.

Use `LR`

```csharp
interface Start  : LR,
                    _< 
                        LayoutFile.UID,
                        GoToStage
                    >
                    { };
```

instead of:

```csharp
interface Start  : L,
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

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/8637f064-75e7-4ab0-8c66-c7625a7aa813)

#### Timeout

The `ReceiveTimeout` and `TransmitTimeout` is the built-in attribute on a stage that sets the maximum duration it can remain active in seconds.
If this attribute is not specified, the stage can persist indefinitely.

### Modify imported channels and their internal components.

You can modify the configuration of imported `channels` and their internal components, including `stages`, `named packs sets`, and `branches`.

- To modify `channels` or `stages`, replicate the original layout of the targets with custom names and,
  extend the built-in `org.unirail.Meta.Modify<TargetEntity>` or `org.unirail.Meta.Modify<TargetChannel, HostA, HostB>` if you also want to modify
  channel's hosts.
- To delete imported `channel` or `stage`, reference it using an XML comment `/// <see cref="Delete.Channel"/>-` in the project declaration.

> [!NOTE]  
> Modified target branches are identified by their corresponding transition `Stage`.

- To delete an entity from a `branch`, replicate the original entity reference and wrap it in the `org.unirail.Meta.X<>` interface.

```csharp
interface UpdateLogin : Modify<Login>, 
                          L, 
                        _<
                            X<Agent.Login, Agent.Signup>,
                            X<Login>,
                            Update_to_stage 
                        >
{ }
```

In this example, `Agent.Login`, `Agent.Signup`, and `Login` will be removed from the `Login` stage branch, and the target stage will be set to
`Update_to_stage`.

- To add a new entity to a `branch`, reference the new entity as you would when declare branches.

> [!NOTE]  
> If a branch you want to modify does not explicitly reference a target `Stage` (imply a self-referencing, permanent stage), you must reference it in
> modifier explicitly
> to modify the branch's target `Stage`..

For example:

```csharp
interface Login /*ā*/ : L,
                        _< /*Ă*/
                            Agent.Login,
                            Agent.Signup
                        >
            { }
```

In this case, the branch implicitly circular references the transition to the `Login` stage. To modify the target stage, explicitly reference the
`Login` stage, as shown below:

```csharp
interface UpdateLogin : Modify<Login>, 
                          L, 
                        _<
                            X<Login>, 
                            Update_to_stage
                        >
            { }
```

This code modifies the branch's target stage from `Login` to `Update_to_stage` by making the reference explicit.

For example, suppose you import all entities from [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs)
but need to modify the inherited `Communication` channel:

```csharp
interface Communication : ChannelFor<Agent, Server> { ... }
```

To modify the `Communication` channel, follow this approach:

```csharp
interface UpdateCommunication : Modify<AdHocProtocol.Communication> {
    interface Change_Info_Result : Modify<AdHocProtocol.Communication.Info_Result>,
                                   _<
                                      X<Server.Info>
                                   > { }

    [TransmitTimeout(30)]
    interface Updated_Start : Modify<AdHocProtocol.Communication.Start>,
                              L,
                              _<
	                              X<AdHocProtocol.Communication.VersionMatching>, // delete goto stage VersionMatching
	                              NewStage // set new goto stage
                              > { }

    interface UpdatedVersionMatching : Modify<AdHocProtocol.Communication.VersionMatching>,
                                       R,
                                       _<
	                                       X<Server.Invitation>,
	                                       Authorizer 
                                       > { }

    interface NewStage : L,
                         _<
                         	Sending_Pack
                         > { }
}
```

In this example, the following changes are made to the `Communication` channel:

- The `Server.Info` pack is removed from the original `named packs set` `Info_Result` via  `Change_Info_Result`.
- The `VersionMatching` transition stage is replaced by a new `NewStage`.
- In the `VersionMatching` stage, the `Invitation` pack is removed, and a new `Authorizer` pack is added.
- Set new `TransmitTimeout` on the `Start` stage

# Fields

## Numeric type

AdHoc protocol description supports the entire range of numeric primitive types available in C# (excluding `decimal`)

| Type     | Range                                                    |
|----------|----------------------------------------------------------|
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

### Example: Using the `[ValueFor(ConstantField)]` Attribute

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
Attributes can be applied to the following elements of the protocol **Hosts, Packs, Fields, Channels,** and **Stages**

### Built-in Attributes

The AdHoc protocol description includes a suite of built-in attributes within the `org.unirail.Meta` namespace.
These attributes enable you to convey essential metadata directly to the code generator, ensuring efficient and accurate implementation.

Let's consider a field with values in the range of **400 000 000** to **400 000 193**, storing them as an `int` would waste memory.
Instead, we can optimize memory usage by using a constant offset of 400 000 000.
This enables representing the entire range using just one byte.

When setting a value, the constant offset is subtracted from the input value.
Conversely, when retrieving the value, the constant is added back to return the original value.

This approach is applied seamlessly using built in the `MinMax` attribute:

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
constants,
offering a straightforward and consistent approach to defining metadata.

- For **fields**, attributes are the sole method for specifying metadata.
- For other entities, metadata can be defined either through attributes or directly using constants.
  For more details, see [Constants](#constants).

Example: Specifying a Description Attribute

To set a Description for a channel stage, you can define a reusable `Description` attribute:

```csharp
[AttributeUsage(AttributeTargets.Interface)]
public class DescriptionAttribute : Attribute {
    public DescriptionAttribute(string description) { }
}

interface Communication : ChannelFor<Agent, Server> {
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
interface Communication : ChannelFor<Agent, Server> {
    interface Stage : 
        _<
            Server.Info,
            Server.Result
        > {
        const string Description = "The stage either responds with the result if successful or provides an error message with relevant information in case of failure.";
    }
}
```

Both approaches provide similar functionality, but the second approach offers more control over the layout of constants.

## Optional Fields

`Optional(nullable) Fields` are identified by type declarations ending with a `?` (e.g., `int?`, `byte?`, `string?`, etc.).
They are allocated in memory but transmit only a bit, if empty, optimizing transmission size.
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

For **optional fields**, with primitive types, the AdHoc generator attempts to encode the empty value efficiently by default.
This behavior can be overridden by declaring the field as **required(not nullable)** and applying a custom attribute to specify a value that
should be treated specially.

```csharp
        [AttributeUsage(AttributeTargets.Field)]
        public class IgnoreZoomIfEqualAttribute : Attribute
        {
            public IgnoreZoomIfEqualAttribute(float value) { }
        }
        
        [IgnoreZoomIfEqual(1.1f)]
        float zoom;
```

The `[IgnoreZoomIfEqual]` attribute with the value `1.1f` will be embedded directly into the field's generated code.

## Value layers

The AdHoc generator uses a 3-layered approach for representing field values.

| layer | Description                                                                                                                                         |
|-------|-----------------------------------------------------------------------------------------------------------------------------------------------------|
| exT   | External datatype. The representation required for external consumers.<br> Information Quanta: Matches the granularity of the language's data types |
| inT   | Internal datatype. The representation optimized for storage.<br> Information Quanta: Matches the granularity of the language's data types           |
| ioT   | IO wire datatype. The network transmission format. Information Quanta: None; transmitted as a byte stream.                                          |

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/180a331d-3d55-4878-8dfe-794ceb9297f3)

However, when dealing with a field containing values ranging from 1 000 000 to 1 080 000, applying shifting on exT <==> inT transition will not result
in memory savings in C#/Java.
This limitation primarily stems from the type quantization inherent to the language.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0b8f90cc-aafc-4923-8c90-1fed53775bb3)

Nevertheless, prior to transmitting data over the network (ioT), a simple optimization can be implemented by subtracting a constant
value of 1 000 000. This action effectively reduces the data to a mere 3 bytes.
Upon reception, reading these 3 bytes and subsequently adding 1 000 000 allows for the retrieval of the originally sent value.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a28e5b20-5c49-4b18-be98-e9bfb6387290)

This example illustrates that data transformation on exT <==> inT can be redundant and only meaningful during the inT <==> ioT transition.

This is a simple and effective technique, but it's not applicable in every scenario. When a field's data type is an `enclosed` array, repacking data
into
different array types during exT <==> inT transitions can be costly and entirely impractical, especially when dealing with keys in a Map or Set
(such as Map<int[], string> or Set<long[]>).

## Varint type

When a numeric field contains randomly distributed values spanning the entire numeric type range, it can be depicted as follows:

![image](https://user-images.githubusercontent.com/29354319/70127303-bdf40900-16b5-11ea-94c9-c0dcd045500f.png)

Efforts to compress this data type would be inefficient and wasteful.

However, if the numeric field exhibits a specific dispersion or gradient pattern within its value range, as illustrated in the following image:

![image](https://user-images.githubusercontent.com/29354319/70128574-0a404880-16b8-11ea-8a4d-efa8a7358dc1.png)

Compressing this type of data could be advantageous in reducing the amount of data transmitted. In such cases,
the code generator can use the
[Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding
[algorithm](https://en.wikipedia.org/wiki/Variable-length_quantity)  for encoding single value field data.
For encoding fields with value collections, the code generator can use the `Group Varint Encoding` technique

This compression algorithms skips the transmission of higher bytes if they are zeros, reducing the amount of data
transmitted, and restore the skipped bytes on the receiving end.

This graph illustrates the relationship between the number of bytes being sent and the transferred value.

![image](https://user-images.githubusercontent.com/29354319/70126207-84ba9980-16b3-11ea-9900-48251b545eef.png)

The graph shows tha `Varint Encoding`  reduces the number of transmitted bytes for smaller values.

Useful to recognize three particular dispersion or gradient patterns within value range:

|                                                     pattern                                                     | description                                                                                                                                                  |
|:---------------------------------------------------------------------------------------------------------------:|:-------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ![image](https://user-images.githubusercontent.com/29354319/155324344-311c6e30-fda5-4d38-b2c7-b946aca3bcf8.png) | For rare fluctuations that are possible only in the direction of bigger values relative to the most probable `min` value, use the `[A(min, max)]` attribute. |
| ![image](https://user-images.githubusercontent.com/29354319/155324459-585969ac-d7ef-4bdc-b314-cc537301aa1d.png) | For rare fluctuations that are possible in both directions relative to the most probable `zero` value, use the `[X(amplitude, zero)]` attribute.             |
| ![image](https://user-images.githubusercontent.com/29354319/155325170-e4ebe07d-cc45-4ffa-9b24-21d10c3a3f18.png) | For fluctuations that are possible only in the direction of smaller values relative to the most probable `max` value, use the `[V(min, max)]` attribute.     |

```csharp
    [A]          uint?  field1;  // Optional field that can store values from 0 to uint.MaxValue. Data is compressible.
    [MinMax(-1128, 873)]    byte  field2;   // Required field without compression, accepting values from -1128 to -873.
    [X]          short? field3;   // Optional field taking values from -32,768 to 32,767. Compressed using the ZigZag algorithm.
    [A(1000)]    short field4;   // Required field taking values from -1,000 to 65,535. Compressed during transmission.
    [V]          short? field5;   // Optional field taking values from -65,535 to 0. Compressed during transmission.
    [MinMax(-11, 75)] short field6;   // Required field with uniformly distributed values within the specified range.
```

## Collection type

Collections, such as `arrays`, `maps`, and `sets`, have the ability to store a variety of data types, including `primitives`, `strings`, and even
`user-defined types`(packs).
The fields of Collection type are `optional`.

Controlling the length of collections is crucial, especially in network applications. This control is vital in preventing overflow, which is one of
the tactics
used in Distributed Denial of Service (DDoS) attacks.
By default, all collections, including `string`, have a maximum capacity of 255 items. To adjust this limit, you can explicitly define an `enum` named
`_DefaultCollectionsMaxLength` with the following fields:

```csharp
    enum _DefaultMaxLengthOf{
        Arrays  = 255,
        Maps    = 255,
        Sets    = 255
        Strings = 255,
    }
```

Types omitted in the `_DefaultMaxLengthOf` enum retain the default limit.

### Flat array

Flat arrays are declared using square brackets `[]`, and the `AhHoc` supports three types of flat arrays:

| Declaration | Description                                                                             |
|-------------|-----------------------------------------------------------------------------------------|
| `[]`        | This means that the length of the array is constant and cannot be changed.              |
| `[,]`       | The array's length is set upon initialization and remains fixed, similar to a `string`. |
| `[,,]`      | The array's length can vary up to a maximum, similar to a `List<T>`.                    |

To customize the size limit for a specific field with an `array` type, you can use the `[D(N)]` attribute,
where `N` represents the new limit.

For example:

```cs
using org.unirail.Meta;

class Pack{
    string[] array_of_255_string_with_max_256_chars; //An array of strings with a constant default length.  
    [D(47)]  Point[,] array_fixed_max_47_points; //An array with a fixed length set at field initialization can contain up to 47 points.  
    [D(47)]  Point[,,] list_max_47_points; //An array with a variable length can have up to 47 points. 
}
```

### String

A `string` is essentially an immutable array of characters.
By default, the `string` type has a maximum length of 255 characters, unless redefined in the [`_DefaultMaxLengthOf.Strings`](#collection-type).
Apply the `[D(+N)]` attribute if you need to impose size limitations on a specific field with `string`,
the `N` represents the new limit.

```csharp
class Packet{
    string                   string_field_with_max_255_chars;
    [D(+6)] string           opt_string_field_with_max_6_chars;
    [D(+7000)] string        string_field_with_max_7000_chars;
}
```

If you have a specific `string` format that is used in multiple places,
it may be more convenient to declare and use the AdHoc [`TYPEDEF`](#typedef) construction:

```csharp
class max_6_chars_string{         // AdHoc typedef
    [D(+6)] string TYPEDEF;
}

class max_7000_chars_string{      // AdHoc typedef
    [D(+7_000)] string TYPEDEF;
}

class Packet{ //                         using typedef
    string                             string_field_with_max_255_chars;
    max_6_chars_string                 string_field_with_max_6_chars;      
    max_7000_chars_string              string_field_with_max_7000_chars;   
}
```

> [!NOTE]
> **When transmitting strings, the `Varint` algorithm is used instead of `UTF-8`.**

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
    [D(-2, -3, -4)] int    []  ints; 
    [D(-2, ~3, ~4)] Point  [] points; 
    [D(-2, -3, -4)] string [] strings_with_max_255_chars; 
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

## longJS and ulongJS types

If you are planning to generate a host in `TypeScript`, you must be aware of the limitations
of the `TypeScript (JavaScript)` `number` type.  
The `number` type can safely represent integers only within the range of **-2^53 + 1 to 2^53 - 1** (
see [SAFE_INTEGER](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number/SAFE_INTEGER)).

If a field's value exceeds this range, the [BigInt](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt)
type will be used, which is less efficient than the `number` primitive.

Therefore, if your field's data is within the safe integer range, using `longJS` or `ulongJS` for communication with the host generated in TypeScript
is preferable to using `long` or `ulong`.

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

## Pack/Enum Type

Both `enums` and `packs` can serve as data types for a field.

- `Enums` are used to represent a set of named constant values of the same type.
- `Packs` are data structures designed to contain multiple fields with diverse data types.

By utilizing `enums` and `packs` as field data types, you can effectively organize and manage diverse data types in your code.

Within packs, you can nest types and even include self-referential fields within the data type definition.
This flexibility allows you to construct complex data structures with interconnected components.

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

        interface CommunicationChannel : ChannelFor<Server, Client>{ }
    }
}
```

result

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/4c485e72-fea2-4886-b1aa-28444657fe71)
</details>

# Meta

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