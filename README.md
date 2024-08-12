# *Attention!!!*

Performance can rarely be an afterthought

![image](https://user-images.githubusercontent.com/29354319/204679188-d5b0bdc7-4e47-4f32-87bb-2bfaf9d09d78.png)

Manually writing code for data serialization and deserialization across different programming languages can be a
time-consuming and error-prone process, especially when working with heterogeneous hosts. To address these challenges, a
more efficient approach is to use a Domain-Specific Language (DSL) that formally describes the protocol in a declarative
style. Based on the protocol description, a code generator can produce source code for various target platforms and
programming languages. This reduces the risk of errors, resulting in faster development and improved compatibility
across different devices and languages.

This approach is used by frameworks such as:

[Protocol Buffers ](https://developers.google.com/protocol-buffers/docs/overview)  
[Cap’n Proto ](https://capnproto.org/language.html)  
[FlatBuffers ](http://google.github.io/flatbuffers/flatbuffers_guide_writing_schema.html)  
[ZCM ](https://github.com/ZeroCM/zcm/blob/master/docs/tutorial.md)  
[MAVLink ](https://github.com/mavlink/mavlink)  
[Thrift](https://thrift.apache.org/docs/idl)
[Apache Avro](https://avro.apache.org/docs/1.8.2/idl.html)

After evaluating available options, a decision was made to develop a code generator that addresses the identified
shortcomings.
This led to the creation of AdHoc protocol, a versatile code generator that supports different programming languages,
including C#, Java, and TypeScript. Future plans include support for additional languages such as C++, Rust, and Go.  
The generated code for protocol handling translates an incoming binary stream into a stream of packages for your program
and vice versa.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/a15016a6-ac05-4d66-8798-4a7188bf24c5)

**AdHoc** generator offers a range of features:

- Support for bitfields
- Handling of ordinary and nullable primitive data types
- Efficiently handle packs with fields that fit within 8 bytes by using the ‘long’ primitive data type to reduce the
  load on garbage collection
- Support for data types such as strings, maps, sets, and arrays.
- A field in a package may have a type of multidimensional array with constant and fixed dimensions.
- Nesting of packs and enums
- Support for ordinary and flags-like enums
- Fields that can have enum and other pack data types
- Constants at the host and packet level
- Inheritance of fields within packs
- Inheritance of communication interfaces at the host level
- In an AdHoc protocol description project, entities can be inherited from other descriptions.
- Compression using the [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) encoding
  algorithm
- Generating code for a ready-to-use network infrastructure.
- The generated code algorithm can reuse and operate with buffers starting from a minimum length of 127 bytes.
  Ideally, using buffers of 256 bytes or larger is preferable. There is no need
  to allocate a buffer for the entire packet.
- The [`custom code injection point`](#custom-code-injection-point) is the area where you can safely insert your code and have the flexibility to mix your custom code with the generated code.
- The system has built-in facilities that enable it to display diagrams of the network infrastructure topology, the
  layout of the pack’s field, and the states of the data flow state machine.

> [!IMPORTANT]  
> The generated code from AdHoc generator can be used for network communication
> between applications or microservices,  
> **as well as for creating custom file storage formats for your application
data.  
> [The little-endian format is used](https://news.ycombinator.com/item?id=25611514)**

The AdHoc code generator is a Software as a Service ([**SaaS**](https://en.wikipedia.org/wiki/Software_as_a_service)) and
provides its services online.

To start using the AdHoc code generator, follow these steps:

1. Install .NET.
2. Install a **C# IDE** such as **[Intellij Rider](https://www.jetbrains.com/rider/)**,
   **[Visual Studio Code](https://code.visualstudio.com/)**, or *
   *[Visual Studio](https://visualstudio.microsoft.com/vs/community/)**.
3. Install [7zip compression](https://www.7-zip.org/download.html). This utility is required for the best compression
   when working with text file formats.   
   You can download it for   
   [Windows](https://www.7-zip.org/a/7zr.exe)  
   [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)  
   [MacOS](https://www.7-zip.org/a/7z2107-mac.tar.xz).
4. Download the source code of
   the [AdHoс protocol metadata attributes](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/master/src/Meta.cs).
   Alternatively, you can use the version embedded in the AdHocAgent binary.
5. Add a reference to the Meta in your AdHoc protocol description project.
6. Use the **[AdHocAgent](https://github.com/cheblin/AdHocAgent)** utility to upload your `protocol description file` to
   the server and obtain the generated code for deployment.

# AdHocAgent Utility

AdHocAgent is a command-line utility designed to streamline your project workflow. It facilitates:

1. Uploading your task
2. Downloading generated results
3. Deploying your project

It accepts the following input:

The first argument is the path to the file with the task for `AdHocAgent Utility`.

The file extension determines the task:

---

## `.cs`

Upload the `protocol description file` to generate source code.
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/7d5181a3-3642-4027-9c3d-aed3ad4b1f5d)

 </details>

## `.cs!`

Upload the `protocol description file` to generate source code and test it.

## `.cs?`

Render the `protocol description file` in a browser-based viewer.
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

Example:

```cmd
    AdHocAgent.exe MyProtocol.cs?
```

![image](https://user-images.githubusercontent.com/29354319/232010215-ea6f4b1e-2251-4c3a-956d-017d222ab1e3.png)

 </details>

> [!NOTE]    
> To navigate from the viewer to the source code, specify your local C# IDE path in `AdHocAgent.toml`.

The remaining arguments are:

- Paths to source files `.cs` and/or `.csproj` project files referenced in the `protocol description file`.
- The path to a temporary folder where files received from the server will be stored.

## `.proto` or directory path

The path to a directory indicates that the task is converting files in the [Protocol Buffers](https://developers.google.com/protocol-buffers) format to format of the AdHoc `protocol description`.

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
> In addition to the command-line arguments, the `AdHocAgent` utility requires:

- `AdHocAgent.toml`: This file contains configuration settings for the `AdHocAgent` utility, including:
  > - The URL of the code-generating server.
  > - The path to the binary of the local C# IDE. This path enables the utility to interact with the local C# Integrated Development Environment (IDE), such as launching the IDE or opening specific files. For example, it allows navigation to a particular code line related to a generated code snippet.
  > - The path to the binary of [7-Zip](https://www.7-zip.org/download.html). The 7-Zip compression utility is used for optimal compression when working with text file formats. `AdHocAgent` uses it to compress or decompress files efficiently.  
      >   Download:
      >   [Windows](https://www.7-zip.org/a/7zr.exe)  
      >   [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)  
      >   [MacOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)

The `AdHocAgent` utility will search for the `AdHocAgent.toml` file in its directory.
If the file is not found, the utility will generate a template that you can update with the required information.

## Continuous deployment

The embedded [Continuous deployment](https://en.wikipedia.org/wiki/Continuous_deployment) system relies on a `deployment instructions file` to deploy the received source code into the target project folders.  
A typical layout for received files might resemble the following:

- 📁[InCS](/C:/Received/AdhocProtocol/InCS)
	- 📁[Agent](file:/C:/Received/AdhocProtocol/InCS/Agent)
		- 📁[gen](/C:/Received/AdhocProtocol/InCS/Agent/gen)
			- ＃[Agent.cs](D:/AdHocTMP/AdhocProtocol/InCS/Agent/gen/Agent.cs)
			- ＃[Context.cs](/C:/Received/AdhocProtocol/InCS/Agent/gen/Context.cs)
		- 📁[lib](/C:/Received/AdhocProtocol/InCS/Agent/lib)
			- ＃[AdHoc.cs](/C:/Received/AdhocProtocol/InCS/Agent/lib/AdHoc.cs)
			- 📁[collections](/C:/Received/AdhocProtocol/InCS/Agent/lib/collections)
				- ＃[BitList.cs](/C:/Received/AdhocProtocol/InCS/Agent/lib/collections/BitList.cs)
				- ＃[RingBuffer.cs](/C:/Received/AdhocProtocol/InCS/Agent/lib/collections/RingBuffer.cs)
			- ＃[Network.cs](/C:/Received/AdhocProtocol/InCS/Agent/lib/Network.cs)
		- 📄[Project.csproj](/C:/Received/AdhocProtocol/InCS/Agent/Project.csproj)
- 📁[InJAVA](/C:/Received/AdhocProtocol/InJAVA)
	- 📁[Server](/C:/Received/AdhocProtocol/InJAVA/Server)
		- 📁[collections](/C:/Received/AdhocProtocol/InJAVA/Server/collections)
			- 📁[org](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org)
				- 📁[unirail](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org/unirail)
					- 📁[collections](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org/unirail/collections)
						- ☕[Array.java](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org/unirail/collections/Array.java)
						- ☕[BitList.java](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org/unirail/collections/BitList.java)
						- ☕[BitsList.java](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org/unirail/collections/BitsList.java)
					- ☕[JsonWriter.java](/C:/Received/AdhocProtocol/InJAVA/Server/collections/org/unirail/JsonWriter.java)
		- 📁[demo](/C:/Received/AdhocProtocol/InJAVA/Server/demo)
			- 📁[org](/C:/Received/AdhocProtocol/InJAVA/Server/demo/org)
				- 📁[unirail](/C:/Received/AdhocProtocol/InJAVA/Server/demo/org/unirail)
					- ☕[ServerImpl.java](/C:/Received/AdhocProtocol/InJAVA/Server/demo/org/unirail/ServerImpl.java)
		- 📁[gen](/C:/Received/AdhocProtocol/InJAVA/Server/gen)
			- 📁[org](/C:/Received/AdhocProtocol/InJAVA/Server/gen/org)
				- 📁[unirail](/C:/Received/AdhocProtocol/InJAVA/Server/gen/org/unirail)
					- ☕[Context.java](/C:/Received/AdhocProtocol/InJAVA/Server/gen/org/unirail/Context.java)
					- ☕[Server.java](/C:/Received/AdhocProtocol/InJAVA/Server/gen/org/unirail/Server.java)
		- 📁[lib](/C:/Received/AdhocProtocol/InJAVA/Server/lib)
			- 📁[org](/C:/Received/AdhocProtocol/InJAVA/Server/lib/org)
				- 📁[unirail](/C:/Received/AdhocProtocol/InJAVA/Server/lib/org/unirail)
					- ☕[AdHoc.java](/C:/Received/AdhocProtocol/InJAVA/Server/lib/org/unirail/AdHoc.java)
					- ☕[Network.java](/C:/Received/AdhocProtocol/InJAVA/Server/lib/org/unirail/Network.java)
- 📁[InTS](/C:/Received/AdhocProtocol/InTS)
	- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer)
		- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts)
		- 📁[gen](/C:/Received/AdhocProtocol/InTS/Observer/gen)
			- 🌀[Context.ts](/C:/Received/AdhocProtocol/InTS/Observer/gen/Context.ts)
			- 🌀[Observer.ts](/C:/Received/AdhocProtocol/InTS/Observer/gen/Observer.ts)
		- 📁[lib](/C:/Received/AdhocProtocol/InTS/Observer/lib)
			- 🌀[AdHoc.ts](/C:/Received/AdhocProtocol/InTS/Observer/lib/AdHoc.ts)
			- 📁[collections](/C:/Received/AdhocProtocol/InTS/Observer/lib/collections)
				- 🌀[BigInt64List.ts](/C:/Received/AdhocProtocol/InTS/Observer/lib/collections/BigInt64List.ts)
				- 🌀[Uint8List.ts](/C:/Received/AdhocProtocol/InTS/Observer/lib/collections/Uint8List.ts)
				- 🌀[Uint8NullList.ts](/C:/Received/AdhocProtocol/InTS/Observer/lib/collections/Uint8NullList.ts)
			- 🌀[Network.ts](/C:/Received/AdhocProtocol/InTS/Observer/lib/Network.ts)
		- {}[package.json](/C:/Received/AdhocProtocol/InTS/Observer/package.json)
		- {}[tsconfig.json](/C:/Received/AdhocProtocol/InTS/Observer/tsconfig.json)

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting

This tree view has been taken from autogenerated deployment instructions file.

This `deployment instructions file` should be named using the `protocol description file name` followed by `Deployment.md`  
For example, if the protocol description file is named `AdhocProtocol.cs`, the instruction file should be named `AdhocProtocolDeployment.md`.

The AdHocAgent utility will search for this instruction file in the following locations:

- The folder containing the `protocol description file`.
- The `Working directory`.

If the utility cannot find the `deployment instruction file`, it will generate a suitable one.  
In that case, you will need to edit the file and provide the correct `deployment instructions`.

### Adding Destination Paths

`Destination Paths` specify the target locations for received files or folders.
You can add `target path(s)` at the end of any folder or file line using the following syntax:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer) [\.(jpg|png|gif)$](/path/to/folder) [](/path/to/folder2)
	- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts) [](/path/to/folder3)
	- 📁[gen](/C:/Received/AdhocProtocol/InTS/Observer/gen) [\.cpp$](/path/to/folder3)

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting.

The deployment process will process [custom code injection point](#custom-code-injection-point) and copy according instructions with matched selectors of a file's parent
folders and instructions on their own line.

#### Target File Path Link

**Copying to a folder:**  
If the link ends with '/', the received item will be copied into the specified path.

For a folder:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer) [](/path/to/parent_folder/)
  
  `/C:/Received/AdhocProtocol/InTS/Observer` will be copied inside `/path/to/parent_folder` as `/path/to/parent_folder/Observer`.

For a file:

- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts) [](/path/to/parent_folder/)
  
  `/C:/Received/AdhocProtocol/InTS/Observer/demo.ts` will be copied inside `/path/to/parent_folder` as `/path/to/parent_folder/demo.ts`.

**Copying with a new name:**  
If the link doesn't end with '/', the item will be copied with the specified name.

For a folder:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer) [](/path/to/NewName)
  `/C:/Received/AdhocProtocol/InTS/Observer` will be copied as `/path/to/NewName`.

For a file:

- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts) [](/path/to/NewName.ts)
  
  `/C:/Received/AdhocProtocol/InTS/Observer/demo.ts` will be copied as `/path/to/NewName.ts`.

If no `destination` is specified for files/subfolders, they inherit the parent folder's destination:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer) [\.cpp$](/path/to/folder) [](/path/to/folder2)
	- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts)
	- 📁[gen](/C:/Received/AdhocProtocol/InTS/Observer/gen)

Use an empty target link `[]()` or `⛔` to skip a file/folder:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer) [](/path/to/folder)
	- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts) ⛔
	- 📁[gen](/C:/Received/AdhocProtocol/InTS/Observer/gen) []()

Specify a [ regular expression](#regular-expression-patterns-for-file-path-matching) on folder lines to select multiple files:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer) [](/path/to/folder) [\.(jpg|png|gif)$](/path/to/folder)
	- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts)
	- 📁[gen](/C:/Received/AdhocProtocol/InTS/Observer/gen)

> [!TIP]  
> Switch from Markdown preview to Markdown source to view detailed formatting.

You can add notes or comments (without line breaks) on any line:

- 📁[Observer](/C:/Received/AdhocProtocol/InTS/Observer)  ✅ copy full tree structure. [](/path/to/folder) Filtered [\.(jpg|png|gif)$](/path/to/folder)
	- 🌀[demo.ts](/C:/Received/AdhocProtocol/InTS/Observer/demo.ts)
	- 📁[gen](/C:/Received/AdhocProtocol/InTS/Observer/gen)

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

Format TypeScript files using prettier:

   ```regexp
   \.ts$
   ```

   ```shell  
prettier --write FILE_PATH --print-width  999
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

Some `injection points` may contain generated code. This code is marked with an empty inline comment at the end:

```javascript
//#region > before Project.Channel receiving
return allocator.new_Project_Channel.get();//
// You can add your custom code here
//#endregion > ÿ.Receiver.receiving.Project.Channel
```

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
[before deployment](/path/to/executable_before1.exe)

[after deployment](/path/to/executable_after1.exe)
[after deployment](/path/to/executable_after2.exe)

[before deployment](/path/to/executable_before2.exe)
```

# Overview

The minimal `protocol description file` could be represented as follows:

```csharp
using org.unirail.Meta; // Importing AdHoc protocol attributes is mandatory

namespace com.my.company // Your company namespace. Required!
{
    public interface MyProject // Declare the AdHoc protocol description project as "MyProject."
    {
        class SharedPack{ } // An empty packet that is transmitted and received by both the Client and Server hosts.

        ///<see cref='InTS'/>
        struct Server : Host // Request to generate code for the Server host using TypeScript.
        {
            class PacketToClient{ } // An empty packet to send to the client.
        }

        ///<see cref='InJAVA'/> 
        struct Client : Host // Request to generate the code for the Client host using Java.
        {
            class PacketToServer{ } // An empty packet to send to the server.
        }

        interface Channel : ChannelFor<Client, Server>{ } // The communication channel between the Client and the Server.
    }
}
```

If you wish to view the structure of a `protocol description file`, you can
utilize the AdHocAgent utility by providing the path to the file followed by a
question mark. For example: `AdHocAgent.exe /dir/minimal_descr_file.cs?`.
Running this command will prompt the utility to display the corresponding scheme
of the `protocol description file`.

<details>
  <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/acc420a1-b2bf-4579-9ee6-5336ad155d4f)
</details>
To upload a file and obtain the generated source code, you can utilize the AdHocAgent utility by providing the path to it, for example: `AdHocAgent.exe /dir/minimal_descr_file.cs`. 
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
network topology. This entails providing information about hosts, channels, and their interconnections.

For instance, let's consider the following `protocol description file`:

```csharp

using org.unirail.Meta; // Importing AdHoc protocol attributes is mandatory

namespace com.my.company2 // Your company namespace. Required!
{
    /**
        <see cref = 'FullFeaturedClient.FullFeaturedClientPack'    id = '4'/>
        <see cref = 'Server.Login'                                 id = '1'/>
        <see cref = 'Server.PackB'                                 id = '2'/>
        <see cref = 'Server.ServerPack'                            id = '0'/>
        <see cref = 'TrialClient.TrialClientPack'                  id = '3'/>
    */
    public interface MyProject
    {
        ///<see cref = 'InJAVA'/>
        struct Server : Host
        {
            // Define packets that Server can create and send
            class ServerPack { }
            class Login { }
            class PackB { }
        }

        ///<see cref = 'InTS'/>
        struct FullFeaturedClient : Host
        {
            class FullFeaturedClientPack { }// Define packets that FullFeaturedClient can create and send
        }

        ///<see cref = 'InCS'/>
        struct TrialClient : Host
        {
            class TrialClientPack { }
        }

        ///<see cref = 'InTS'/>
        struct FreeClient : Host { }

        // Define communication channels between hosts

        interface TrialCommunicationChannel : ChannelFor<Server, TrialClient> { }

        interface CommunicationChannel : ChannelFor<Server, FullFeaturedClient> { }

        interface TheChannel : ChannelFor<Server, FreeClient> { }
    }
}
```

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>and if you observe it with AdHocAgent utility viewer you may see the following</u></b></summary>  

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/02d2dbc8-cfaa-4b07-8d89-58caac560e1a)

By selecting a specific channel in the AdHocAgent utility viewer, you can view detailed information about the packets
involved and their destinations.
This feature allows you to track the specific path taken by the packets within the network.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0a3e372d-1eee-4ba4-bbdd-8eb34a0fcc40)
</details>

Please note that after processing the file with AdHocAgent, it assigns packet ID numbers to the packets.
These numbers help in identifying and tracking the packets within the system.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/51163c18-3b49-4f4f-adea-c3450c0fe01c)

## Importing other descriptions

The C# `using` [directive](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/using-directive)
at the top of the description file enables the usage of entities declared in other protocol description files.

```csharp
using com.company.ProtocolProject;
using com.other_company.OtherProtocolProject;
```

Constants from imported non-root projects are propagated to the root project only if they are explicitly specified in
the project's inheritance or
referenced using
the [<see cref="entity">](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute)
attribute
in the XML documentation of the root project.  
Direct inheritance from other projects automatically propagates all **constants** they have.
Individual `enum` or `constants set` items can be selectively propagated if the root project inherits them from other
projects.

This ensures that the desired constants are included and available for use in the root project,
providing explicit control over which constants are propagated.

```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject : Project_const_packs, Particular_const_pack {
        
    } 
}
```

This code propagates all **constants** from `Project_const_packs` and `Particular_const_pack` to all hosts in
the `MyProject` project.

Imported items can be wrapped with the `_<>` interface to allow their usage in C# code where only interfaces are
allowed.

```csharp
using org.unirail.Meta;

namespace org.unirail
{
    public interface AdhocProtocol : _<AdhocProtocol.Agent.Project.Host.Port.Pack.Field.DataType>//propagate DataType constants set to all hosts
    {
    }
}
```

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

> [!IMPORTANT]  
> All packs explicitly referenced in the documentation of the host declaration will be included in the host, even if they are not transmittable or receivable.
> This is useful for generating utility packs.

## Packs

Packs serve as the smallest unit of transmittable information and are defined
using a C# `class` construction. Pack declarations can be nested and
positioned anywhere within a project's scope.
The instance `fields` of a pack's class represent the information that the pack
carries and transmits.  
Additionally, constant and static fields within the pack generate constants within its scope.  
It is also possible for a pack to be empty, signifying that the transmitted
information is solely indicative of the packet's transmission.

It is possible to include or inherit **all fields** from other packs by
utilizing the `<see cref='Full.Path.To.Pack_or_field'/>` comment in the
destination pack or through C# `inheritance` from the other pack wrapped in the
`_<>` interface wrapper.

Individual fields can also be `inherited` or `embedded` from another pack by using
the `<see cref="Full.Path.To.OtherPack.Field"/>` comment in the destination pack.

Field cannot be overridden if they have the same name as an existing field.

To remove specific imported fields, you can use the `<see cref="Full.Path.To.OtherPack.Field"/>-`  comment,
The `<see cref="Full.Path.To.Source.Pack"/>-` comment can be utilized to delete all fields with the same name as the
fields in the referenced pack simultaneously.
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
            ///<see cref="AnyPacksFields.client_hashcode"/> but excludes the client_hashcode field from AnyPacksFields.
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

## Empty packs

Empty packets, which have no fields, are implemented as singletons. They serve as the most efficient means of signaling
something simple.

## Value Pack

Value packs are compact data structures that can fit within **8 bytes**. They possess unique properties:

- Do not allocate memory on the heap
- Store data directly in primitive types
- Benefit from specialized code generation methods for efficient packing and unpacking of field data

## Container Pack

Container packs are non-transmittable structures designed to organize other packs into logical hierarchies:

- Declared using a C# `struct`
- Dedicated to structuring and grouping related packs
- Can contain constants declared with `const` or `static` fields

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

## Channels

Channels in the **AdHoc protocol** is a communication pathway and serve as the means to connect hosts. They are declared
using a C# `interface` and, similar to hosts, reside directly within the project scope.
The Channel's interface extends the `org.unirail.Meta.ChannelFor` interface and specifies the two hosts
that are being connected through its generic parameter.

Here's an example:

```csharp
        interface TrialCommunicationChannel : ChannelFor<Server, TrialClient> { }
        interface CommunicationChannel : ChannelFor<Server, FullFeaturedClient> { }
        interface TheChannel : ChannelFor<Server, FreeClient> { }
```

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/64b0caac-f850-4375-a035-f70eef6dc07d)

Implementation:

In the implementation of the **AdHoc protocol**, channels are specifically designed to connect the **EXT**ernal network
with the **INT**ernal host.
A channel is composed of processing layers, and each layer has both an **EXT**ernal and **INT**ernal interface.
The abbreviations INT and EXT are used consistently throughout the generated code to denote the internal and external
aspects of the system.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/234749384-73a1ce13-59c1-4730-89a7-0a182e6012eb.png)

</details>

A channel with an empty body indicates that hosts connected through the channel
can **send** packets declared within their own body to their counterparts.
Furthermore, packets declared outside any hosts' scope the
project scope are shared among all hosts. This means that any host can **transmit** these
packets as if they were declared within their own body.

Typically, the body of a channel's `interface` contains declarations of `stages` and `branches` that define the dataflow
logic between the connected hosts.

### Stages

The stages defined within a channel represent different processing states in your code that communicate with each other
on the ends of the channel.
The code generated for the stage does not impose any restrictions; it is solely used as reference information in various
parts of your code where necessary.
It is entirely up to the developer to decide how to utilize and incorporate this code in their implementation as per
their requirements.

Each stage is declared inside channel scope using the C# `interface` construction, where the interface name becomes the
name of the stage.  
The top stage, also known as the "**init**" stage, represents the initial state or starting point in the series of
stages defined within the channel.
A stage have to extend the built-in `org.unirail.Meta.L` and/or `org.unirail.Meta.R` interfaces.
The `L` and `R` are used to denote the left and right hosts, respectively, in the channel declaration, the stage belongs
to.
Immediately following the reference to the `L/R`side, the declaration of the side branches is initiated.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/1cd6ad55-7e0e-4167-9d4a-fef279b4fa11)

It is possible for only one side to have the capability to send packets.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/f1cdc9e3-9e14-4781-af7b-ce46b3dc5234)

> [!WARNING]   
> A short `block comment` with some symbols `/*įĂ*/` represents auto-sets
> unique
> identifiers.
> These identifiers are utilized to identify entities. Therefore, you can relocate or rename entities, but the
> identifier will remain unchanged.
> It is important to never edit or clone this identifier.

#### Branches

Within each side of the stage `L/R` the sending packets are organized into multiple `branches`.
A `branch` consists of a set of packets and a reference to the goto `stage` to which it will switch after sending any of
the packets in the `branch`.

- If the destination stage is a reference to the built-in `Stage.Exit`, the receiver side will drop the connection after
  receiving the packet in the branch.
- If the destination stage is a reference to the built-in `Stage.None`, sending a packet from the branch does not change
  the state.

If you recognize a pattern or repetition in a set of packets, you have the option to create a named set of packets and
refer to them by their assigned name.
This allows for easier referencing and reusability of packet sets within your code.

#### Named packs set

If you recognize a pattern or repetition in a set of packets, you have the option to create a `named set of packets` and
refer to them by their assigned name.
This allows for easier referencing and reusability of packet sets within your code.  
![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/8637f064-75e7-4ab0-8c66-c7625a7aa813)

`Named packet sets` can be declared anywhere in your `project` and may contain references to individual `packets` as
well as other `named packet sets`.

#### Timeout

The `Timeout` attribute on a stage sets the maximum it duration. If the attribute is not specified, the stage can
remain indefinitely

Let's take a look at the **snippet** of the communication flow part in the `protocol description file` used by AdHocAgent
Utility.
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/63eb6d6f-fa33-4f7a-852a-724531db5726)

</details>

To view code diagram in Observer, run the AdHocAgent Utility with the following command line:

```cmd
AdHocAgent.exe /path/to/AdhocProtocol.cs?
```

In the opened diagram, simply right-click on a channel link, and resize opened channels window to view all channels

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
it may be more convenient to declare and utilize the AdHoc [`typedef`](#typedef) construction:

```csharp
class max_6_chars_string{         // AdHoc typedef
    [D(+6)] string typedef;
}

class max_7000_chars_string{      // AdHoc typedef
    [D(+7_000)] string typedef;
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

If the declaration becomes overly complex and is used in many fields, consider utilizing [`typedef`](#typedef) for decomposition.

```csharp
        class string_max_30_chars{
           [D(+30)] string typedef;
        }

        class list_of_max_100_ints{
            [D(100), X]  int[,,] typedef;
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

## typedef

`Typedef` is employed to establish an alias for a data type, rather than creating a new type.
When multiple fields require the same (complex) type, consider declaring and using `typedef`.
This simplifies the process of modifying the data type for all related fields simultaneously.

In AdHoc, `typedef` is declared with a C# class construction containing the declaration of a **single** field named `typedef`.
The **name** of the class becomes an alias for the type of its `typedef` field.

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
    [D(+6)] string typedef;
}

class max_7000_chars_string{      // AdHoc typedef
    [D(+7_000)] string typedef;
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
