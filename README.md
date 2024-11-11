# *Attention!!!*

Performance can rarely be an afterthought

![image](https://user-images.githubusercontent.com/29354319/204679188-d5b0bdc7-4e47-4f32-87bb-2bfaf9d09d78.png)

When your solution components need to communicate across different programming languages, manually coding data serialization becomes a
challenging task. It's slow, error-prone, and grows more complicated as you add languages and platforms, or when you need to modify
existing solutions with new data structures or message types.

This is where Domain-Specific Languages (DSLs) for protocol description come in. These declarative languages allow you to define your data structures and communication protocols once, then automatically generate implementation code for any supported language. This approach offers several key benefits:

* Reduced development time
* Fewer bugs and compatibility issues
* Consistent implementation across platforms
* Easier maintenance and updates

Many established frameworks already utilize this approach, including:

- [Protocol Buffers](https://developers.google.com/protocol-buffers/docs/overview)
- [Cap’n Proto](https://capnproto.org/language.html)
- [FlatBuffers](http://google.github.io/flatbuffers/flatbuffers_guide_writing_schema.html)
- [ZCM](https://github.com/ZeroCM/zcm/blob/master/docs/tutorial.md)
- [MAVLink](https://github.com/mavlink/mavlink)
- [Thrift](https://thrift.apache.org/docs/idl)
- [Apache Avro](https://avro.apache.org/docs/1.8.2/idl.html)

However, after careful evaluation of existing solutions, we identified opportunities for improvement.
This led to the development of AdHoc protocol — a next-generation code generator designed to overcome common limitations.AdHoc currently supports C#, Java, and TypeScript, with
plans to expand to C++, Rust, and Go. It seamlessly handles the translation between binary data streams and structured packages in your application,
making cross-language communication effortless.

The AdHoc code generator is crafted for **data-oriented applications** requiring high performance, with efficient handling of structured binary data across network
communication and custom storage formats. Its design is ideal for applications that demand fast data throughput with minimal resource consumption, allowing more users to be
served on the same hardware. Here’s where it excels:

1. **Best Fit: Data-Intensive Applications**  
   This solution is especially suited for:
	- **Financial Trading**: Real-time handling of high-frequency data packets with low latency.
	- **Customer Relationship Management (CRM)**: Processing substantial datasets for customer and transactional data.
	- **Enterprise Resource Planning (ERP)**: Supporting high-volume updates in logistics, inventory, and other real-time processes.
	- **Game Servers**: Managing real-time communication and state synchronization between players in multiplayer online games with minimal latency.
	- **IoT Systems**: Handling large-scale sensor data streams with high throughput and low latency.
	- **Real-Time Analytics Platforms**: Processing large amounts of data in real time, such as monitoring and analyzing system logs or sensor data, where low latency and
	  high throughput are essential.
	- **Streaming Media Services**: Delivering high-quality, low-latency audio or video streams, ensuring smooth, uninterrupted delivery to users.
	- **Telecommunications Systems**: Managing communication data for high-volume call routing, message delivery, and network state monitoring in real time.
	- **Autonomous Vehicles**: Handling data from various sensors and communication systems, requiring fast and efficient transmission of data packets between components
	  for decision-making.
   
   Applications in these areas require **optimized binary protocols** that can manage complex data transactions while minimizing memory and CPU usage. The efficient data
2. handling enables these applications to **serve more users on the same hardware**, enhancing scalability and cost-efficiency.

2. **Network Communication and Custom Storage**  
   The generated code can be employed for high-performance network communication between applications or microservices, ensuring fast, reliable data exchange with
3. reduced computational overhead. Additionally, it supports **custom binary file storage formats** that are compact and efficient, further reducing memory usage and
4. improving retrieval speed.

3. **Performance Benefits**  
   By leveraging binary protocols, this solution offers:
	- **Reduced Memory and CPU Usage**: Optimized data formats mean lower resource consumption, allowing a single server to handle a higher number of simultaneous users.
	- **Faster Serialization/Deserialization**: Less processing time for data transformations.
	- **Improved Network Efficiency**: Smaller data packets translate to reduced transfer times and enhanced throughput.

4. **When to Consider Other Solutions**  
   Applications that are primarily **text-based or content-oriented**, such as blogs, content management systems, or document storage, may not gain significant benefits
5. from binary protocols. In these cases, standard formats like JSON or XML often meet requirements effectively without the complexity of a binary format.
   
   Additionally, for **simple or low-performance use cases**, where data volume and speed are not critical factors, traditional data formats may be more straightforward to
6. implement and maintain.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a15016a6-ac05-4d66-8798-4a7188bf24c5)

The **AdHoc** generator offers a comprehensive set of features:

- Support for bitfields.
- Handling of standard and nullable primitive data types.
- If all the field data of a pack fits within 8 bytes, it will be represented as a `long` primitive, thereby
  reducing garbage collection overhead.
- Support for data types like strings, maps, sets, and arrays.
- Allows multidimensional arrays with constant/fixed/variable dimensions as field types.
- Supports nested packs and enums.
- Handles both standard and flags-like enums.
- Fields can use enum and reference to a pack data types.
- Defines constants at both host and packet levels.
- Each entity can import(inherit) or subtract(remove) properties of others, allowing for flexible composition.
  The system handles pack circular references and supports multiple inheritance.
  Additionally, reused entities can be modified to meet the specific requirements of the new project.
- Projects can be composed of other projects or selectively import specific parts, such as channels, constants or substruct a pack.
- Channels can be constructed from channels or their segments, such as stages or branches.
- Packs can import or subtract individual fields or all fields of other packs.
- Implements compression using the [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding algorithm.
- Generates code for a ready-to-use network infrastructure.
- The generated code reuses buffers, starting from a minimum length of 127 bytes, with a preference for 256 bytes or larger. Buffer allocation for the entire packet is not required.
- Provides a [`custom code injection point`](#custom-code-injection-point), where custom code can safely be integrated with the generated code.
- The system has built-in facilities that enable it to display diagrams of the network infrastructure topology, the
  layout of the pack's field, and the states of the data flow state machine.

The code generated by the AdHoc generator can be used to handle network communication
between applications or microservices and to create custom file storage formats for
application data.

The **AdHoc Code Generator** is a [**SaaS**](https://en.wikipedia.org/wiki/Software_as_a_service) platform that provides cloud-based code generation services.

First, you'll need a personal [UUID](#uuid).
Why UUID?
The use of a UUID, rather than a login and password, allows users to automate code generation and embed the **AdHocAgent** utility into their code delivery process.

To start using the AdHoc code generator, follow these steps:

1. Install .NET.
2. Install a **C# IDE** such as **[Intellij Rider](https://www.jetbrains.com/rider/)**,
   **[Visual Studio Code](https://code.visualstudio.com/)**, or *
   *[Visual Studio](https://visualstudio.microsoft.com/vs/community/)**.

---

3. Install [7-Zip Compression](https://www.7-zip.org/download.html), a utility for optimal PPMd compression of source files. Download the appropriate version for your platform:
	
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
4. Download the source code of the [AdHoc protocol metadata attributes Meta.cs file](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/master/src/Meta.cs).  
   Alternatively, add a dependency on the AdHocAgent `.dll` to your protocol project.
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
4. View your project structure as a diagram
5. Upload `.proto` files to convert into AdHoc protocol description format
6. Retain and update user `UUID`

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

Render the `protocol description file` in a browser-based viewer.
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

Example:

```cmd
    AdHocAgent.exe MyProtocol.cs?
```

![image](https://user-images.githubusercontent.com/29354319/232010215-ea6f4b1e-2251-4c3a-956d-017d222ab1e3.png)

![image](https://github.com/user-attachments/assets/565a76c2-58f3-4570-9ca8-c6bad41f4f43)
 </details>

> [!NOTE]    
> To enable navigation from the viewer to the source code, specify the path to your local C# IDE in the `AdHocAgent.toml` configuration file.

The remaining arguments are:

- Paths to source files `.cs` and/or `.csproj` project files referenced in the `protocol description file`.
- The path to a temporary folder where files received from the server will be stored.

The Observer will search for and save the `.layout` file in the current working directory with the same name as the specified protocol description file.
For example, if the provided file path is   
`my_protocol_description.cs`  the corresponding layout file will be   
`my_protocol_description.layout`.

To save the current layout, right-click on an empty space within the diagram:

![image](https://github.com/user-attachments/assets/d2482a1b-5058-4903-920e-ef5dbf252ef6)

Then, click `Save layout`.

If you modify the layout and close the browser without saving, a `my_protocol_description.unsaved_layout` file will be created, containing the layout before closing.
You can rename this file to `my_protocol_description.layout` to use it, if you accidentally closed without saving.

## `.proto` or path to a directory

Indicates that the task is converting files in the [Protocol Buffers](https://developers.google.com/protocol-buffers) format to format of the AdHoc `protocol description`.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>
Example

```cmd
    AdHocAgent.exe MyProtocol.proto
```

![image](https://user-images.githubusercontent.com/29354319/232012276-03d497a7-b80c-4315-9547-ad8dd120f077.png)
 </details> 

> [!NOTE]  
> The second argument can be a path to a directory containing additional imported `.proto` files, such as [`well_known`](https://github.com/protocolbuffers/protobuf/tree/main/src/google/protobuf)
> files and others.

The result of the .proto files transformation is only a starting point for your transition to the AdHoc protocol and cannot be used as is.
Reconsider it in the context of the greater opportunities provided by the AdHoc protocol.

## `.md`

The provided path is the `deployment instruction file` for the embedded [Continuous Deployment](https://en.wikipedia.org/wiki/Continuous_deployment) system.
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
	- The path to the [7-Zip](https://www.7-zip.org/download.html) binary. `AdHocAgent` utilizes its PPMd compression capability.
		- Download links:  
		  [Windows](https://www.7-zip.org/a/7zr.exe)  
		  [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)  
		  [MacOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)
	- The path to your preferred source code formatter binaries, including:
		- Download links  
		  [clang-format](https://releases.llvm.org/download.html)  
		  [prettier](https://prettier.io/docs/en/install.html)  
		  [astyle](https://sourceforge.net/projects/astyle/files/)

The `AdHocAgent` utility will search for the `AdHocAgent.toml` file in its directory.
If the file is not found, the utility will generate a template that you can update with the required information.

## UUID

To get your first `volatile` [UUID](https://en.wikipedia.org/wiki/Universally_unique_identifier) or to recover one, follow these steps:

1. Sign in to your GitHub account.
2. Go to the [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up) and post a message.

After your request is processed (when the post disappears), a bot will automatically create a new **private** project for you [here](https://github.com/orgs/AdHoc-Protocol/projects).
This project will track your code generation history and provide helpful messages with details about any issues and their resolutions.

In the project, you will find a task with your UUID:   
![image](https://github.com/user-attachments/assets/b1789e7e-3ca3-4442-839b-aca172babf4e)

Copy the UUID and once run the **AdHocAgent** utility.

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

## Continuous deployment (CD)

The embedded [Continuous deployment](https://en.wikipedia.org/wiki/Continuous_deployment) system relies on a `deployment instructions file` to deploy the received source code into the target project folders.  
A typical layout for received files might resemble the following:

- 📁[InCS](/C:/Received/AdHocProtocol/InCS)
	- 📁[Agent](file:/C:/Received/AdHocProtocol/InCS/Agent)
		- 📁[gen](/C:/Received/AdHocProtocol/InCS/Agent/gen)
			- ＃[Agent.cs](D:/AdHocTMP/AdHocProtocol/InCS/Agent/gen/Agent.cs)
			- ＃[Context.cs](/C:/Received/AdHocProtocol/InCS/Agent/gen/Context.cs)
		- 📁[lib](/C:/Received/AdHocProtocol/InCS/Agent/lib)
			- ＃[AdHoc.cs](/C:/Received/AdHocProtocol/InCS/Agent/lib/AdHoc.cs)
			- 📁[collections](/C:/Received/AdHocProtocol/InCS/Agent/lib/collections)
				- ＃[BitList.cs](/C:/Received/AdHocProtocol/InCS/Agent/lib/collections/BitList.cs)
				- ＃[RingBuffer.cs](/C:/Received/AdHocProtocol/InCS/Agent/lib/collections/RingBuffer.cs)
			- ＃[Network.cs](/C:/Received/AdHocProtocol/InCS/Agent/lib/Network.cs)
		- 📄[Project.csproj](/C:/Received/AdHocProtocol/InCS/Agent/Project.csproj)
- 📁[InJAVA](/C:/Received/AdHocProtocol/InJAVA)
	- 📁[Server](/C:/Received/AdHocProtocol/InJAVA/Server)
		- 📁[collections](/C:/Received/AdHocProtocol/InJAVA/Server/collections)
			- 📁[org](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org)
				- 📁[unirail](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org/unirail)
					- 📁[collections](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org/unirail/collections)
						- ☕[Array.java](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org/unirail/collections/Array.java)
						- ☕[BitList.java](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org/unirail/collections/BitList.java)
						- ☕[BitsList.java](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org/unirail/collections/BitsList.java)
					- ☕[JsonWriter.java](/C:/Received/AdHocProtocol/InJAVA/Server/collections/org/unirail/JsonWriter.java)
		- 📁[demo](/C:/Received/AdHocProtocol/InJAVA/Server/demo)
			- 📁[org](/C:/Received/AdHocProtocol/InJAVA/Server/demo/org)
				- 📁[unirail](/C:/Received/AdHocProtocol/InJAVA/Server/demo/org/unirail)
					- ☕[ServerImpl.java](/C:/Received/AdHocProtocol/InJAVA/Server/demo/org/unirail/ServerImpl.java)
		- 📁[gen](/C:/Received/AdHocProtocol/InJAVA/Server/gen)
			- 📁[org](/C:/Received/AdHocProtocol/InJAVA/Server/gen/org)
				- 📁[unirail](/C:/Received/AdHocProtocol/InJAVA/Server/gen/org/unirail)
					- ☕[Context.java](/C:/Received/AdHocProtocol/InJAVA/Server/gen/org/unirail/Context.java)
					- ☕[Server.java](/C:/Received/AdHocProtocol/InJAVA/Server/gen/org/unirail/Server.java)
		- 📁[lib](/C:/Received/AdHocProtocol/InJAVA/Server/lib)
			- 📁[org](/C:/Received/AdHocProtocol/InJAVA/Server/lib/org)
				- 📁[unirail](/C:/Received/AdHocProtocol/InJAVA/Server/lib/org/unirail)
					- ☕[AdHoc.java](/C:/Received/AdHocProtocol/InJAVA/Server/lib/org/unirail/AdHoc.java)
					- ☕[Network.java](/C:/Received/AdHocProtocol/InJAVA/Server/lib/org/unirail/Network.java)
- 📁[InTS](/C:/Received/AdHocProtocol/InTS)
	- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer)
		- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts)
		- 📁[gen](/C:/Received/AdHocProtocol/InTS/Observer/gen)
			- 🌀[Context.ts](/C:/Received/AdHocProtocol/InTS/Observer/gen/Context.ts)
			- 🌀[Observer.ts](/C:/Received/AdHocProtocol/InTS/Observer/gen/Observer.ts)
		- 📁[lib](/C:/Received/AdHocProtocol/InTS/Observer/lib)
			- 🌀[AdHoc.ts](/C:/Received/AdHocProtocol/InTS/Observer/lib/AdHoc.ts)
			- 📁[collections](/C:/Received/AdHocProtocol/InTS/Observer/lib/collections)
				- 🌀[BigInt64List.ts](/C:/Received/AdHocProtocol/InTS/Observer/lib/collections/BigInt64List.ts)
				- 🌀[Uint8List.ts](/C:/Received/AdHocProtocol/InTS/Observer/lib/collections/Uint8List.ts)
				- 🌀[Uint8NullList.ts](/C:/Received/AdHocProtocol/InTS/Observer/lib/collections/Uint8NullList.ts)
			- 🌀[Network.ts](/C:/Received/AdHocProtocol/InTS/Observer/lib/Network.ts)
		- {}[package.json](/C:/Received/AdHocProtocol/InTS/Observer/package.json)
		- {}[tsconfig.json](/C:/Received/AdHocProtocol/InTS/Observer/tsconfig.json)

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting

This tree view has been taken from autogenerated deployment instructions file.

This `deployment instructions file` should be named using the `protocol description file name` followed by `.md`  
For example, if the protocol description file is named `AdHocProtocol.cs`, the instruction file should be named `AdHocProtocol.md`.

The AdHocAgent utility will search for this instruction file in the following locations:

- The folder containing the `protocol description file`.
- The `Working directory`.

If the utility cannot find the `deployment instruction file`, it will generate a suitable one.  
In that case, you will need to edit the file and provide the correct `deployment instructions`.

### Adding Destination Paths

`Destination Paths` specify the target locations for received files or folders.
You can add `target path(s)` at the end of any folder or file line using the following syntax:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer) [\.(jpg|png|gif)$](/path/to/folder) [](/path/to/folder2)
	- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts) [](/path/to/folder3)
	- 📁[gen](/C:/Received/AdHocProtocol/InTS/Observer/gen) [\.cpp$](/path/to/folder3)

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting.

The deployment process will process [custom code injection point](#custom-code-injection-point) and copy according instructions with matched selectors of a file's parent
folders and instructions on their own line.

#### Target File Path Link

**Copying to a folder:**  
If the link ends with '/', the received item will be copied into the specified path.

For a folder:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer) [](/path/to/parent_folder/)
  
  `/C:/Received/AdHocProtocol/InTS/Observer` will be copied inside `/path/to/parent_folder` as `/path/to/parent_folder/Observer`.

For a file:

- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts) [](/path/to/parent_folder/)
  
  `/C:/Received/AdHocProtocol/InTS/Observer/demo.ts` will be copied inside `/path/to/parent_folder` as `/path/to/parent_folder/demo.ts`.

**Copying with a new name:**  
If the link doesn't end with '/', the item will be copied with the specified name.

For a folder:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer) [](/path/to/NewName)
  `/C:/Received/AdHocProtocol/InTS/Observer` will be copied as `/path/to/NewName`.

For a file:

- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts) [](/path/to/NewName.ts)
  
  `/C:/Received/AdHocProtocol/InTS/Observer/demo.ts` will be copied as `/path/to/NewName.ts`.

If no `destination` is specified for files/subfolders, they inherit the parent folder's destination:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer) [\.cpp$](/path/to/folder) [](/path/to/folder2)
	- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts)
	- 📁[gen](/C:/Received/AdHocProtocol/InTS/Observer/gen)

Use an empty target link `[]()` or `⛔` to skip a file/folder:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer) [](/path/to/folder)
	- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts) ⛔
	- 📁[gen](/C:/Received/AdHocProtocol/InTS/Observer/gen) []()

Specify a [ regular expression](#regular-expression-patterns-for-file-path-matching) on folder lines to select multiple files:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer) [](/path/to/folder) [\.(jpg|png|gif)$](/path/to/folder)
	- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts)
	- 📁[gen](/C:/Received/AdHocProtocol/InTS/Observer/gen)

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting.

You can add notes or comments (without line breaks) on any line:

- 📁[Observer](/C:/Received/AdHocProtocol/InTS/Observer)  ✅ copy full tree structure. [](/path/to/folder) Filtered [\.(jpg|png|gif)$](/path/to/folder)
	- 🌀[demo.ts](/C:/Received/AdHocProtocol/InTS/Observer/demo.ts)
	- 📁[gen](/C:/Received/AdHocProtocol/InTS/Observer/gen)

#### Regular Expression Patterns for File Path Matching

Examples of valid regex patterns commonly used for file matching:

1. **File Extensions**
	- `\.cpp$`: Matches files ending with .cpp
	- `\.(c|h)$`: Matches files ending with either .c or .h

2. **Specific Naming Patterns**
	- `^test[^/]\.txt$`: Matches files like test1.txt, testA.txt, but not test/1.txt
	- `^log_[0-9]\.txt$`: Matches log_0.txt through log_9.txt

3. **Directory Structures**
	- `^src/.*\.js$`: Matches .js files in the src directory or its subdirectories
	- `^docs/[^/]*\.md$`: Matches .md files directly in the docs directory

4. **Exclusions**
	- `^.*(?<!\.o)$`: Matches files not ending with .o
	- `^.*(?<!\.txt)$`: Matches files not ending with .txt

5. **Complex Patterns**
	- `^.*test_.*\.py$`: Matches Python files with "test_" anywhere in the filename
	- `^data_\d{4}\.csv$`: Matches data files with a 4-digit number, like data_0001.csv

6. **Multiple Criteria**
	- `\.(jpg|png|gif)$`: Matches files ending with .jpg, .png, or .gif
	- `^project/(src|test)/.*\.js$`: Matches .js files in either src or test directories

Key regex components:

- `^` asserts the start of the string.
- `$` asserts the end of the string.
- `.` matches any character except newline.
- `.*` matches any number of characters.
- `[^/]` matches any character except a forward slash.
- `\d` matches a digit.
- `{4}` specifies exactly four occurrences of the previous pattern.

### Deploying Execution Instructions

The `Execution Instructions` feature allows you to run code on received source files before they are deployed to their destinations.
This is useful for tasks such as formatting, linting, or performing other operations on the files.
A `Deployment Instructions` file can include multiple `Execution Instructions` as needed.

`Execution Instructions` start with `regex_matching` - regular expressions (regex) used to match file paths and determine which files a particular instruction applies to. These patterns provide a powerful and flexible way to select files based on their names or paths.

`Execution Instructions` are executed in the order they appear in the instruction file.

#### Root Path

If a path starts with one of the following prefixes:

- `/InCPP/`
- `/InCS/`
- `/InGO/`
- `/InJAVA/`
- `/InRS/`
- `/InTS/`

it indicates that the path is relative to the root of the folder containing the received files.

#### File Path Placeholder

The `FILE_PATH` placeholder in an `Execution Instruction` will be replaced with the actual file path during execution.

#### Types of Execution Instructions

##### Shell Execution Instruction

Execute an application via the command line using the following structure:

   ```regexp
   regex_matching
   ```

   ```shell
executable_path command_line_parameters
   ```

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting

- `regex_matching`: Specifies which files the instruction applies to.
- `executable_path`: Path to the executable. Must be at the start of the line. Enclose `executable_path` in quotes if it contains spaces.
- `command_line_parameters`: Parameters to pass to the executable. Must include the `FILE_PATH` placeholder.
	- Notes:
		- You can split `command_line_parameters` across multiple lines, but each line must be indented.
		- Multiple shell executions `executable_path command_line_parameters` can be specified for the same `regex_matching` selector.

**Examples:**

Format Java, C#, C++, and header files using clang-format:

   ```regexp
   \.(java|cs|cpp|h|)$
   ```

   ```shell  
clang-format -i -style="{ColumnLimit: 0,IndentWidth: 4, 
                        TabWidth: 4, 
                        UseTab: Never, 
                        BreakBeforeBraces: Allman, 
                        IndentCaseLabels: true, 
                        SpacesInLineCommentPrefix: {Minimum: 0, Maximum: 0}}" FILE_PATH 
   ```

To format C# files with `dotnet format` use the command in format [before and after deployment execution](#before-and-after-deployment-execution)

```shell
[before deployment](dotnet format "/InCS/Host_in_C#" )
```

To format TypeScript files using prettier:

   ```regexp
   \.ts$
   ```

   ```shell  
prettier --write FILE_PATH --tab-width 4 --bracket-spacing false --print-width  999
   ```

Run custom lint on JavaScript files:

   ```regexp
   \.js$
   ```

   ```shell
c:\scripts\custom_lint.exe --fix FILE_PATH
   ```

Multiple executions for C++ files:

   ```regexp
   \.cpp$
   ```

   ```shell
c:\clang-format.exe -i FILE_PATH
"d:\Path with whitespaces must be enclosed in quotes\apps.exe" FILE_PATH
   ```

##### C# Code Execution Instruction

Execute a C# code snippet. The structure is:

```regexp
\.cpp$
```

```csharp
"System.Linq.Enumerable"
"System.Text.RegularExpressions"

using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;

public class Program
{
    // Define the regex pattern
    static string pattern = @"^\s+(?=//#region|//#endregion|#region|#endregion|//region|//endregion|// region|// endregion)";
    
    public static void Main(string[] args)
    {
        // Read the file content with UTF-8 encoding
        var content = File.ReadAllText( args[0], System.Text.Encoding.UTF8);

        // Perform the replacement
        var updatedContent = Regex.Replace(content, pattern,"", RegexOptions.Multiline);

        // Write the updated content back to the file with UTF-8 encoding
        //According to the Unicode standard, the BOM for UTF-8 files is not recommended !!!
        File.WriteAllText( args[0], updatedContent,  new System.Text.UTF8Encoding()); 
    }
}
```

This code removes whitespace before region directives in a C++ file.

**Attention**:

- actual file path will be passed as `arg[0]` of `Program.Main`.
- Add required assembly references for missing namespaces.   
  For example, if you encounter errors like `The type or namespace name 'Linq' does not exist in the namespace 'System'`,
  add the required assembly at the top of your code.   
  Each assembly should be on a new line, enclosed in quotes, such as `"System.Linq.Enumerable"`.

```csharp
"System.Linq.Enumerable"
"Other.Assembly.Full.Name"
"Other.Assembly.Full.Name1"

...
rest of your code    
...
```

### Custom code injection point

Custom code `injection points` are areas within your generated codebase where you can safely insert custom code that will be preserved during generated file updates.
This feature allows you to seamlessly integrate your custom logic with generated code.

#### How It Works

1. During file deployment, the system scans the content of files being overwritten in the target location.
2. It extracts custom code within `injection points` areas and transfer to corresponding areas in the newly received file.
3. The files being overwritten are backed up to ensure data integrity.

#### Injection Point Format

The format of `injection points` varies depending on the programming language. They are denoted by special comments that mark the beginning and end of the `injection point`.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/7c7dec09-86b0-4b7a-8c63-8f7df2a035ee)

Here are examples of the special injection points:

Java/TypeScript:

```javascript
//#region > before Project.Channel receiving
//#endregion > ÿ.Receiver.receiving.Project.Channel
```

C#:

```csharp
#region > before Project.Channel receiving
#endregion > ÿ.Receiver.receiving.Info
```

Some `injection points`may contain generated code, which is marked by an empty inline comment at the end:

```csharp
//#region > before Project.Channel receiving
return allocator.new_Project_Channel.get();//
// You can add your custom code here
//#endregion > ÿ.Receiver.receiving.Project.Channel
```

This indicates that the code line is generated and can be deleted if it does not meet your requirements.

#### Important Considerations

Unique Identifiers

- Each `injection point` has a unique identifier, represented by short text (`scope uid` and `injection point uid`).
- These identifiers allow you to freely move or edit the `injection point` comment without losing changes after an update.

> [!CAUTION]  
> Never edit or duplicate the unique identifiers. Doing so may result in loss of custom code during updates.

Best Practices

1. Only add custom code within clearly marked `injection point` areas.
2. Do not modify the `injection point` comments themselves.
3. Keep your custom code modular and well-documented for easier maintenance.

### Before and After Deployment Execution

In the `deployment instructions file`, you can specify the paths to executable files
that will run before and after the `Continuous Deployment` process. These executables can be configured as follows:

```markdown
[before deployment]("C:\Program Files\dotnet\dotnet.exe" format "C:\My Deployment\folder\MyProject\InCS\My Host" )
[before deployment](dotnet format "C:\My Deployment\folder\MyProject\InCS\My Host2" )

[after deployment](/path/to/executable_after1.exe)
[after deployment](/path/to/executable_after2.exe)

[before deployment](/path/to/executable_before2.exe)
```

# Overview

The simplest form of a `protocol description file` can be [represented as follows](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/Templates/ProtocolDescription.cs):

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

To view the structure of a `protocol description file`, use the AdHocAgent utility by specifying the file path followed by a question mark.
For example: `AdHocAgent.exe /dir/minimal_descr_file.cs?`. Running this command will prompt the utility to display the schema of the `protocol description file`.

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

To exclude specific imported entities, reference them in the project's XML documentation using the [`<see cref="entity"/>-`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute) attribute:

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

For example, the protocol description in [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs) defines public, external communications. However, on the **Server** side, the backend infrastructure requires an internal communication protocol to handle tasks such as:

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
	- Two packs, `AuthorisationConfirmed` and `AuthorisationRejected`, are defined within the `Authorizer`. One of these will be sent as a reply to the `AuthorisationRequest` from the `Server`.

- **Channels**:
	- `ChannelToMetrics` connects the `Server` and `Metrics`.
	- `ChannelToAuthorizer` connects the `Server` and `Authorizer`.

> [!IMPORTANT]
> If your solution requires working with multiple protocols, you cannot easily combine their generated protocol-processing code within the same VM instance
> due to `lib` **org.unirail** namespace clashes. To resolve this, assign each project’s `lib` to a distinct namespace.

## Hosts

In the AdHoc protocol, "hosts" refer to entities that actively participate in the exchange of information.
These hosts are represented as C# `structs` within a project's `interface` and implement the `org.unirail.Meta.Host` marker interface.

To specify the programming language and options for generating the host's source code, use the XML [`<see cref="entity">`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute)
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

## Packs

Packs are the smallest units of transmittable information, defined using a C# `class`. Pack declarations can be nested and placed anywhere within a project’s scope.

> [!NOTE]  
> A pack can function as a [set of packs](#projecthost-as-a-named-pack-set). Keep this in mind when organizing the pack's internal hierarchy of packs.

The `fields` in a pack's class represent the data it transmits.

Constant and static fields define constants within a pack's scope. A pack can be empty; in this case, its transmission is the only information it conveys.

To include or inherit **all fields** from other packs, add the `<see cref='Full.Path.To.Pack_or_field'/>+` line in the target pack’s XML documentation,
or use C# class inheritance. To inherit fields from multiple packs, use the `org.unirail.Meta._<>` wrapper.

Individual fields can be _inherited_ or _embedded_ by adding the `<see cref="Full.Path.To.OtherPack.AddField"/>+` comment in the target pack's XML documentation.

Inherited fields cannot override existing or previously inherited fields with same name.

To remove specific inherited fields, use the `<see cref="Full.Path.To.OtherPack.RemoveField"/>-` XML comment on pack.  
The `<see cref="Full.Path.To.Source.Pack"/>-` comment can be used to simultaneously remove all fields that share the same names as the fields in the referenced `Pack`.

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

### Empty packs

Empty packets, which have no fields, are implemented as singletons. They serve as the most efficient means of signaling
something simple.

### Value Pack

Value packs are compact data structures that can fit within **8 bytes**. They possess unique properties:

- Do not allocate memory on the heap
- Store data directly in primitive types
- Benefit from specialized code generation methods for efficient packing and unpacking of field data

### Container Pack

Container packs are non-transmittable structures designed to organize other packs into logical hierarchies:

- Declared using a C# `struct`
- Dedicated to structuring and grouping related packs
- Can contain constants declared with `const` or `static` fields

### Modify Imported Packs

To modify the layout of imported packs, create a new pack and merge its fields into the `TargetPack` by implementing the built-in `org.unirail.Meta.Modify<TargetPack>`.

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

## Enums and Constants

### Enums

Enums are used to organize sets of constants of the same primitive type:

- Use the `[Flags]` attribute to indicate an enum can be treated as a bit field or set of flags
- Enum fields without explicit initialization are automatically assigned integer values
- In a `[Flags]` enum, assigned values represent bit flags

### Constants

Constants with different primitive types or strings (including their arrays) can be declared as `const` or `static` fields
in any related pack, including `Container Packs`.

- `static` fields:
	- Can be assigned a value or the result of a static expression
	- Can utilize any available C# static functions

- `const` fields:
	- Can be used as `attribute` parameters
	- Must have a value that is the result of a C# compile-time expression
	- Cannot use C# static functions to calculate values.

To overcome this `const` fields limitations the `AdHoc protocol description` syntax introduces the `[ValueFor(const_constant)]` attribute:

- Applied to a dummy `static` field
- During code generation, the generator assigns the value and type from the `static` field to a corresponding `const` constant

This approach combines the flexibility of `static` fields with the compile-time benefits of `const` constants.

Example: Using [ValueFor(ConstantField)] Attribute

Here's an example demonstrating the use of the `[ValueFor(ConstantField)]` attribute:

```csharp
[ValueFor(ConstantField)] static double value_for = Math.Sin(23);

const double ConstantField = 0; // Result: ConstantField = Math.Sin(23)
```

In this example:

- `value_for` is assigned the value of `Math.Sin(23)`
- This value is then copied to the `ConstantField` constant
- Due to the `[ValueFor(ConstantField)]` attribute, `ConstantField` will have the calculated value of `Math.Sin(23)` at compile-time

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

The constants defined in the root description file are propagated to all hosts.

`Enums` and all constants are replicated on every host and are not transmitted during communication.
They serve as local copies of the constant values and are available for reference and use within the respective host's scope.

### Modify Enums and Constants

Enums and constants can be modified like a [simple pack](#modify-imported-packs), **but the modifier is discarded after the modification is applied.**

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

The AdHoc protocol implementation features channels designed to connect the EXTernal network with the INTernal host. Each channel comprises processing layers,
each containing both an **EXT**ernal and **INT**ernal side. The abbreviations INT and EXT are consistently employed throughout the generated code to denote
internal and external aspects.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/234749384-73a1ce13-59c1-4730-89a7-0a182e6012eb.png)

</details>

> [!IMPORTANT]  
> **[The little-endian format is used for data representation on the wire.](https://news.ycombinator.com/item?id=25611514)**

### Stages

Stages within a channel represent the channel's distinct processing states.

Each stage is declared within the channel scope using a C# `interface` construction, where the `interface` name becomes the stage name.
The topmost stage, known as the "**init**" stage, represents the initial state.
To collect the stages of a channel, initiate a traversal from the **init** stage. Any stages that are not reachable from **init** will be disregarded.

A stage extends the built-in interfaces `org.unirail.Meta.L`, `org.unirail.Meta.R` or `org.unirail.Meta.LR`. Here, `L` and `R` represent the left and right hosts, respectively,
while `LR` denotes both hosts in the channel declaration to which the stage belongs.

The declaration of branches begins immediately after denote host side.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/1cd6ad55-7e0e-4167-9d4a-fef279b4fa11)

It is possible for only one side to have the capability to send packets.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/f1cdc9e3-9e14-4781-af7b-ce46b3dc5234)

> [!WARNING]   
> A short `block comment` with some symbols `/*įĂ*/` represents auto-sets
> unique
> identifiers.
> These identifiers are used to identify entities. Therefore, you can relocate or rename entities, but the
> identifier will remain unchanged.
> It is important to never edit or clone this identifier.

#### Branches

After referencing a host side (`L`, `R`, or `LR`), `sending` packets are organized into multiple `branches`. A `branch` consists of a list of `sending` packets
and may optionally include a reference to the target `stage`, which the host will transition to after sending any packet from the list.

- If the target `stage` is a reference to the built-in `org.unirail.Meta.Exit`, the receiving host will terminate the connection after receiving any packet from the branch.
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

#### Named Pack Sets

When you identify a recurring pattern in a set of packets, you can create a **named set of transmittable packets** and reference that set by name.   
![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/8637f064-75e7-4ab0-8c66-c7625a7aa813)  
Prefixing a reference to a pack with **`@`** includes all transmittable packs declared within the pack body.

```csharp
interface Start : LR,
                  _< 
                      @Pack1,
                      Pack12
                  >
                  { };
```

**Named packet sets** can be declared anywhere within your project and may contain references to individual pack, other named packet sets, projects, or hosts.

##### Project/Host as a Named Pack Set

A project or host can function as a **named pack set** and be used accordingly. When referenced directly, it implies a collection of transmittable packs declared within the body.

```csharp
interface Start : LR,
                  _< 
                      Project,
                      Host,
                  >
                  { };
```

Prefixing a reference with **`@`** adds all transmittable packs recursively, including packs throughout the hierarchical structure.

```csharp
interface Start : LR,
                  _< 
                      @Project,
                      @Host
                  >
                  { };
```

This approach simplifies referencing and enhances the reusability of packet sets throughout your protocol description.

#### Timeout

The `Timeout` is the built-in attribute on a stage that sets the maximum duration it can remain active in seconds.
If this attribute is not specified, the stage can persist indefinitely.

Let's examine the practical use of the communication flow in the
[`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/acfc582c971914a4a86f3458d4b85a141a787d3c/AdHocProtocol.cs#L443) protocol description file.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/63eb6d6f-fa33-4f7a-852a-724531db5726)

</details>

To view the communication flow diagram in the **Observer**, follow these steps:

Run the **AdHocAgent** Utility using the following command:

   ```cmd
      AdHocAgent.exe /path/to/AdHocProtocol.cs
   ```

Once the diagram opens, right-click on a channel link. Resize the opened channels window to display all channels.


> [!NOTE]
> The code generated for each stage is provided as a reference. It does not impose any constraints on the package flow.
> Developers have full flexibility to adapt and integrate this code into their implementations according to their specific needs and requirements.
> For a practical usage example, you can search for `Communication.Stages` in the
> [ChannelToServer.cs](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/ChannelToServer.cs) file of the AdHoc Protocol GitHub repository.

### Modify imported channels and their internal components.

You can modify the configuration of imported `channels` and their internal components, including `stages`, `named packs sets`, and `branches`.

- To modify `channels` or `stages`, replicate the original layout of the targets with custom names and,
  extend the built-in `org.unirail.Meta.Modify<TargetEntity>` or `org.unirail.Meta.Modify<TargetChannel, HostA, HostB>` if you also want to modify channel's hosts.
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

In this example, `Agent.Login`, `Agent.Signup`, and `Login` will be removed from the `Login` stage branch, and the target stage will be set to `Update_to_stage`.

- To add a new entity to a `branch`, reference the new entity as you would when declare branches.

> [!NOTE]  
> If a branch you want to modify does not explicitly reference a target `Stage` (imply a self-referencing, permanent stage), you must reference it in modifier explicitly
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

In this case, the branch implicitly circular references the transition to the `Login` stage. To modify the target stage, explicitly reference the `Login` stage, as shown below:

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

For example, suppose you import all entities from [`AdHocProtocol.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdHocProtocol.cs) but need to modify the inherited `Communication` channel:

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

    [Timeout(30)]
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
- Set new Timeout on the `Start` stage

# Fields

## `Optional` and `Required` fields

A pack's field can be `optional` or `required`.

* `required` fields are always allocated and transmitted.
* `optional` fields, on the other hand, only allocate a few bits if they have not data.

In the AdHoc protocol description, fields with type declarations ending with a `?` (such as `int?`, `byte?`, `string?`, etc.) are considered 'optional'.

```csharp
class Packet
{
    string user; //non-primitive data types are always optional
    uint? optional_field; //optional uint field
}     
```

> [!NOTE]
> All fields with reference types (embedded packs, strings, collections) declared in the pack root are always `optional`.

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

As the protocol creator, you possess the most profound understanding of your data and its unique requisites.

When dealing with a field whose values fall within the range of 400 000 000 to 400 000 093, it's common to use an 'int' data type.
However, it becomes evident that for efficient storage of this field, only one byte and a constant value of 400 000 000 are necessary.
This constant should be subtracted during the setting process and added during retrieval.

The AdHoc protocol description includes attributes that enable you to provide this knowledge to the code generator, allowing it to generate optimized code.

In this scenario, the `MinMax` attribute can be applied to the field declaration.

```csharp
     [MinMax(400_000_000, 400_000_093)] int ranged_field;     
```

The code generator will then select the appropriate field type (`byte`) and generate helper getter and setter functions accordingly.

For fields with value **ranges** smaller than 127, the code generator employs internal `bits storage` to conserve memory.

Example:

```csharp
     [MinMax(1, 8)] int car_doors;     
```

In this case, the code generator can allocate 3 bits in the bits storage for the `car_doors` field, based on the
specified range of 1 to 8.

The AdHoc generator utilizes a 3-layered approach for representing field values.

| layer | Description                                                                                                                                     |
|-------|-------------------------------------------------------------------------------------------------------------------------------------------------|
| exT   | External type. The representation required for external consumers.<br> Information Quanta: Matches the granularity of the language's data types |
| inT   | Internal type. The representation optimized for storage.<br> Information Quanta: Matches the granularity of the language's data types           |
| ioT   | IO wire type. The network transmission format. Information Quanta: None; transmitted as a byte stream.                                          |

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/180a331d-3d55-4878-8dfe-794ceb9297f3)

However, when dealing with a field containing values ranging from 1 000 000 to 1 080 000, applying shifting on exT <==> inT transition will not result in memory savings in C#/Java.
This limitation primarily stems from the type quantization inherent to the language.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0b8f90cc-aafc-4923-8c90-1fed53775bb3)

Nevertheless, prior to transmitting data over the network (ioT), a simple optimization can be implemented by subtracting a constant
value of 1 000 000. This action effectively reduces the data to a mere 3 bytes.
Upon reception, reading these 3 bytes and subsequently adding 1 000 000 allows for the retrieval of the originally sent value.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a28e5b20-5c49-4b18-be98-e9bfb6387290)

This example illustrates that data transformation on exT <==> inT can be redundant and only meaningful during the inT <==> ioT transition.

This is a simple and effective technique, but it's not applicable in every scenario. When a field's data type is an `enclosed` array, repacking data into
different array types during exT <==> inT transitions can be costly and entirely impractical, especially when dealing with keys in a Map or Set
(such as Map<int[], string> or Set<long[]>).

## Varint type

When a numeric field contains randomly distributed values spanning the entire numeric type range, it can be depicted as follows:

![image](https://user-images.githubusercontent.com/29354319/70127303-bdf40900-16b5-11ea-94c9-c0dcd045500f.png)

Efforts to compress this data type would be inefficient and wasteful.

However, if the numeric field exhibits a specific dispersion or gradient pattern within its value range, as illustrated in the following image:

![image](https://user-images.githubusercontent.com/29354319/70128574-0a404880-16b8-11ea-8a4d-efa8a7358dc1.png)

Compressing this type of data could be advantageous in reducing the amount of data transmitted. In such cases,
the code generator can utilize the
[Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding
[algorithm](https://en.wikipedia.org/wiki/Variable-length_quantity)  for encoding single value field data.
For encoding fields with value collections, the code generator can utilize the `Group Varint Encoding` technique

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

Collections, such as `arrays`, `maps`, and `sets`, have the ability to store a variety of data types, including `primitives`, `strings`, and even `user-defined types`(packs).
The fields of Collection type are `optional`.

Controlling the length of collections is crucial, especially in network applications. This control is vital in preventing overflow, which is one of the tactics
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
it may be more convenient to declare and utilize the AdHoc [`TYPEDEF`](#typedef) construction:

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
By default, they are limited to holding a maximum of 255 items unless redefined in the [`_DefaultMaxLengthOf.Sets` / `_DefaultMaxLengthOf.Maps`](#collection-type).
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

- `Enums` are utilized to represent a set of named constant values of the same type.
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