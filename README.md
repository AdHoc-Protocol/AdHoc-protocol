# *Attention!!!*

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
- The generated code can reuse and operate with buffers of lengths starting from 64 bytes and larger. There is no need
  to allocate a buffer for the entire packet.
- Network byte order (big-endian)
- The system has built-in facilities that enable it to display diagrams of the network infrastructure topology, the
  layout of the pack’s field, and the states of the data flow state machine.

> <span style = "font-size:20px">❗️</span> The generated code from AdHoc generator can be used for network communication
> between applications or microservices,  
> <span style = "font-size:20px">❗️</span> **as well as for creating custom file storage formats for your application
data.**

The code generator is available as a [**SaaS**](https://en.wikipedia.org/wiki/Software_as_a_service) (Software as a
Service) and can be accessed online.

To use the AdHoc code generator, follow these steps:

-[x] Install .NET on your operating system.
-[x] Install a **C# IDE** such as *
 *[Intellij Rider](https://www.jetbrains.com/rider/),  [Visual Studio Code](https://code.visualstudio.com/),
 or [Visual Studio](https://visualstudio.microsoft.com/vs/community/)** ).
-[x] Install [7zip compression](https://www.7-zip.org/download.html) The 7zip compression utility is required for best
 compression when working with text file formats.  
 download it here:  
 [Windows](https://www.7-zip.org/a/7zr.exe)  
 [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)  
 [MacOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)
-[x] Download the source code of
 the [AdHoс protocol metadata attributes](https://github.com/cheblin/AdHoc-protocol/tree/master/xyz/unirail/AdHoc).
 Alternatively, you can use the version embedded in the AdHocAgent binary.
-[x] Add a reference to the Meta in your AdHoc protocol description project.
-[x] Use the **[AdHocAgent](https://github.com/cheblin/AdHocAgent)** utility to upload your protocol description file to
 the server and obtain the generated code for deployment.

-[x] You can either download a [prebuilt version of AdHocAgent](https://github.com/cheblin/AdHocAgent/tree/master/bin)
 or
 download the source code and build it yourself.

# AdHocAgent utility

The AdHocAgent utility is a command-line tool that facilitates uploading your project, downloading the generated result,
and deploying it.

It accepts the following input:

The first argument is the path to the task file, which can optionally include a parameter at the end to specify the type
of task:

- `.cs`  - Upload the protocol description file to the server to generate the source code.
- `.cs!` - Upload the protocol description file to generate the source code and test it.

The remaining arguments are as follows:

- paths to source files `.cs` and/or `.csproj` project file used in the mentioned protocol description file
- The path to a temporary folder where files received from the server will be stored.
- The last argument, if provided, is the path to your binary file, which will be executed after the embedded deployment
  process.
  (⚠️ Please note that the temporary folder will serve as the working directory for this executable file).

> For the embedded deployment system to function correctly, an instruction file is required to deploy the received
> source code.
> The instruction file should be named using the protocol description file name followed by `Deployment.md`
> For example, if the protocol description file is named `MyProtocol_file_name`, the instruction file should be
> named `MyProtocol_file_nameDeployment.md`.
>
> The AdHocAgent utility will search for this instruction file in the following locations:
>- The folder containing the protocol description file.
>- The `Working directory`
>
>If the utility cannot find the file in these locations, it will extract a template of the instruction file next to the
> protocol description file.
> In that case, you will need to edit the file and provide the correct deployment instructions.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/7d5181a3-3642-4027-9c3d-aed3ad4b1f5d)

 </details>

If the first argument's path ends with certain parameters, the utility behaves as follows:

- `.cs?` - This parameter instructs the utility to render the structure of the provided protocol description file in a
  browser-based viewer.
  To navigate from the viewer to the protocol description source code, you need to provide a path to a locally installed
  C# IDE
  in the `AdHocAgent.toml`configuration file.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

Example:

```cmd
    AdHocAgent.exe MyProtocol.cs?
```

![image](https://user-images.githubusercontent.com/29354319/232010215-ea6f4b1e-2251-4c3a-956d-017d222ab1e3.png)

 </details>

- `.proto` - This parameter indicates that the provided input is
  in [Protocol Buffers](https://developers.google.com/protocol-buffers) format.
  The utility will transmit the content of the file or directory (with names ending in `.proto`) to the server for
  conversion into the Adhoc protocol description format.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>
Example

```cmd
    AdHocAgent.exe MyProtocol.proto
```

![image](https://user-images.githubusercontent.com/29354319/232012276-03d497a7-b80c-4315-9547-ad8dd120f077.png)
 </details> 

- `.md` - This parameter indicates that the provided file is a deployment instruction file. As a result, AdHocAgent will
  repeat only the deployment process
  for the already received source files in the working directory. This is particularly useful for debugging deployment.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/6109d22b-d4f9-43dc-8e9b-976d38d63b32)
 </details> 

During the initial code generation, the deployment file is automatically generated. It contains instructions for copying
the received source files from the server
to the target local project folders. The file consists of two parts separated by a horizontal rule (***): the first part
is a list declaration of **named** target locations,
and the second part is a list of copying instructions for the declared locations.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/b7066585-e221-4f6e-b370-8041d6078e46)
> <span style = "font-size:20px">❗️</span>It is safe to add or delete any additional descriptive content.  
> If your project doesn't require certain delivered files, simply delete the corresponding copy instruction lines.  
> In a standard .md file editor/viewer, you can click on hyperlinks to open the target path.



While copying files to the destination locations, the system scans for any custom changes made by the user.
Any custom code inserted at specific `insertion points` is moved to the new file obtained from the server and saved at
the destination location.
The previously used file is backed up to ensure data integrity.
The insertion points are formatted differently depending on the programming language.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/7c7dec09-86b0-4b7a-8c63-8f7df2a035ee)

> <span style = "font-size:20px">❗️</span> A short text `scope uid`  and `insertion point uid` represents autogenerated
> unique identifiers.
> These identifiers are utilized to identify entities. Therefore, you can relocate or rename entities, but the
> identifier will remain unchanged.
> It is important to never edit or clone this identifier.

Here are examples of the special insertion points:

In Java:

```javascript
//region > before Project.Channel receiving
//endregion > ÿ.Receiver.receiving.Project.Channel
```

In C#:

```csharp
#region > before Project.Channel receiving
#endregion > ÿ.Receiver.receiving.Info
```

The generated insertion points may contain generated code. In such cases, this code is marked with an empty inline
comment at the end.

```javascript
//region > before Project.Channel receiving
return allocator.new_Project_Channel.get();//
//endregion > ÿ.Receiver.receiving.Project.Channel
```

Indeed, you have the flexibility to mix your custom code with the generated code at the insertion point scope or make
modifications as per your requirements.
You can choose to retain, modify, or delete the generated code within those insertion points.

This level of customization allows you to tailor the code to your specific needs and incorporate any additional
functionality or logic as necessary.
Feel free to modify the code in those insertion points according to your preferences and project requirements.




> <span style = "font-size:20px">❗️</span> In addition to the command-line arguments, the AdHocAgent utility needs:

- An `AdHocAgent.toml` - This file contains configuration settings for the AdHocAgent utility such as:
    - The URL of the code-generating server.
    - Path to the binary of local C# IDE: This path is used to enable the utility to interact with the local C#
      Integrated Development Environment (IDE).
      The utility may need to launch the IDE or open specific files in it, for example, to navigate to a particular code
      line related to a generated code snippet.
    - And path to the binary of [7zip compression](https://www.7-zip.org/download.html) The 7zip compression utility is
      required for best compression when working with text file formats.
      It used by the AdHocAgent utility to compress or decompress files efficiently.  
      download:  
      [Windows](https://www.7-zip.org/a/7zr.exe)  
      [Linux](https://www.7-zip.org/a/7z2201-linux-x86.tar.xz)  
      [MacOS](https://www.7-zip.org/a/7z2107-mac.tar.xz)

AdHocAgent utility will search for the `AdHocAgent.toml` file next to itself.
If it cannot find the file in this location, it will generate a template that you can update with the actual
information.

# Overview

The minimal protocol description file could be represented as follows:

```csharp
using xyz.unirail.Meta; // Importing AdHoc protocol attributes is mandatory

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

        interface Channel : Communication_Channel_Of<Client, Server>{ } // The communication channel between the Client and the Server.
    }
}
```

If you wish to view the structure of a protocol description file, you can
utilize the AdHocAgent utility by providing the path to the file followed by a
question mark. For example: `AdHocAgent.exe /dir/minimal_descr_file.cs?`.
Running this command will prompt the utility to display the corresponding scheme
of the protocol description file.

<details>
  <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/acc420a1-b2bf-4579-9ee6-5336ad155d4f)
</details>
To upload a file and obtain the generated source code, you can utilize the AdHocAgent utility by providing the path to it, for example: `AdHocAgent.exe /dir/minimal_descr_file.cs`. 
This command will upload the file and initiate the process of generating the source code based on the contents of the specified file.

# Protocol description file format

> **The protocol description file follows a specific naming convention:**
>
>- Names should not start or end with an underscore `_`.
>- Names should not match any keywords defined by the programming languages that the code generator supports. *
   *AdHocAgent** will check for and warn about such conflicts before uploading.

## Project

As a [`DSL`](https://en.wikipedia.org/wiki/Domain-specific_language) to describe **AdHoc
protocol** the C# language was chosen.
The protocol description file is essentially a plain C# source code file within a .NET project.
To create a protocol description file, follow these steps:

As a DSL (Domain-Specific Language) to describe the AdHoc protocol, the C# language was chosen. The protocol description
file is essentially a plain C# source
code file within a .NET project.
By using this approach, you can leverage the C# language and its features to define and describe the AdHoc protocol in a
structured and readable manner.

To create a protocol description file, follow these steps:

- Start by creating a C# project.
- Add a reference to
  the [AdHoc protocol metadata attributes.](https://github.com/cheblin/AdHoc-protocol/tree/master/src/org/unirail/AdHoc).
- Create a new C# source file within the project.
- Declare the protocol description project using a C# 'interface' within your company's namespace.

```csharp
using xyz.unirail.Meta; // Importing AdHoc protocol attributes. This is required.

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

For instance, let's consider the following protocol description file:

```csharp

using xyz.unirail.Meta; // Importing AdHoc protocol attributes is mandatory

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

        interface TrialCommunicationChannel : Communication_Channel_Of<Server, TrialClient> { }

        interface CommunicationChannel : Communication_Channel_Of<Server, FullFeaturedClient> { }

        interface TheChannel : Communication_Channel_Of<Server, FreeClient> { }
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
using xyz.unirail.Meta;

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
using xyz.unirail.Meta;

namespace xyz.unirail
{
    public interface AdhocProtocol : _<AdhocProtocol.Agent.Project.Host.Port.Pack.Field.DataType>//propagate DataType constants set to all hosts
    {
    }
}
```

## Hosts

In the AdHoc protocol, hosts are represented as C# `struct` within the scope of a project's `interface`.
These hosts actively participate in the exchange of information and are required to implement
the `xyz.unirail.Meta.Host` marker interface.

To specify the language and relevant options for the host's source code generation, XML tags can be used in the code
documentation of the host declaration.
The  [<see cref="member">](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute)
tag is used for this purpose

The built-in marker interfaces `InCS`, `InJAVA`, `InTS` etc. allow for the declaration of language configuration scopes.

Items, such as `packs` or `fields` referenced within a particular language configuration scope will inherit the
configuration specified by that scope.
The latest language configuration scope becomes the default for the subsequent items within the host.

Based on the provided code snippet, the language configuration for the 'Server' host appears to be as follows:

```csharp
using xyz.unirail.Meta;

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

AdHocAgent utility could be read in this manner..
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/0cfa47f2-8b2e-4e49-9c7d-0fd908dbd7ce)

</details>

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
using xyz.unirail.Meta;

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

        interface CommunicationChannel : Communication_Channel_Of<Server, Client> { }
    }
}
```

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/c3cd9f94-7c6f-486e-a60b-c156b5342d5f)

</details>

## Empty packs

Empty packets, which have no fields, are implemented as singletons. They serve as the most efficient means of signaling
something simple.

## Value pack

Packs containing information that can fit within 8 bytes are referred to as `Value` packs. These packs have special
characteristics
as they do not allocate memory on the heap, and their data is stored directly in primitive types. The code generator
offers methods
that facilitate the packing and unpacking of field data for these packs.

## Enums and constants

To propagate constants across hosts with the same integral type, use enums. They ensure type safety and code clarity
when sharing constant values.

The `[Flags]` attribute on enum is indicates that
an [the enumeration can be treated as a bit field or a set of flags.](https://learn.microsoft.com/en-us/dotnet/api/system.flagsattribute)

Enum fields without explicit initialization are automatically assigned integer values. When an enum is marked with
the `[Flags]` attribute,
the assigned values represent **bit flags**,

If your constants have a non-primitive data type, you can represent them using static or const fields.
These fields can be placed either within a pack that they are logically related to or within a `constants set` declared
using a C# `struct` construction.
This allows for better organization and encapsulation of the constants, making them easily accessible and manageable
within your codebase.

`Enums` and `constant sets` are replicated on every host and are not transmitted during communication. They serve as
local copies of the constant values and
are available for reference and use within the respective host's scope. These copies ensure that each host has access to
the necessary constant values without
the need for transferring them between hosts during communication.

Constants declared with `static` fields can be assigned values as either a
number or the result of a static expression. You have the flexibility to use any
available C\# functions to calculate the values. These values are determined
during code generation, ensuring that they are resolved at compile-time and
remain constant throughout the execution of the program.

Constants declared with `const` fields can be used as `attributes parameters`. They must have a value that is the result
of a compile-time expression.
Standard static C# functions cannot be used to calculate the value of a `const` field due to a limitation in the C#
compiler.

To work around this limitation, the AdHoc protocol description syntax introduces the `[ProxyFor(const_constant)]`
attribute which is applied to a 'static' field
During code generation, the generator assigns the value and type from the `static` field to a corresponding `const`
constant. This allows the desired value to be used
as an attribute parameter while preserving the benefits of compile-time constants.

Here's an example utilizing the `[ProxyFor(ConstantField)]` attribute:

```csharp
[ProxyFor(ConstantField)] static double static_proxy_field = Math.Sin(23);

const double ConstantField = 0; // Result: ConstantField = Math.Sin(23)
```

In this example, the `static_proxy_field` is assigned the value of
`Math.Sin(23)`, which is then proxied to the `ConstantField` constant. The
**ConstantField** will have the calculated value of **Math.Sin(23)** at
compile-time due to the **[ProxyFor(ConstantField)]** attribute.


<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using System;
using xyz.unirail.Meta;

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
        struct Client : Host
        {
            class Login
            {
                string user;
                string password;

                //======= static fields === constatns related to Login pack
                static int      USE_ANY_FUNCTION = (int)Math.Sin(34) * 4 + 2;
                static string[] STRINGS          = { "", "\0", "ere::22r" + "K\nK\n\"KK", STR };

                [ProxyFor(DST_CONST_FIELD)] //SRC_STATIC_FIELD pushes the value and type to DST_CONST_FIELD
                private static int SRC_STATIC_FIELD = 45 * (int)Server.MAV_BATTERY_FUNCTION.MAV_BATTERY_FUNCTION_ALL + 45 >> 2 + USE_ANY_FUNCTION;
                const string STR = "KKKK";
                
                const int DST_CONST_FIELD = 0;
            }
        }
        
        struct SI_Unit //constants set
        {
            struct time //constants set
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

        interface CommunicationChannel : Communication_Channel_Of<Server, Client> { }
    }
}
```

</details>

The constants defined in the root description file are propagated to all hosts.

## Channels

Channels in the **AdHoc protocol** is a communication pathway and serve as the means to connect hosts. They are declared
using a C# `interface` and, similar to hosts, reside directly within the project scope.
The Channel's interface extends the `xyz.unirail.Meta.Communication_Channel_Of` interface and specifies the two hosts
that are being connected through its generic parameter.

Here's an example:

```csharp
        interface TrialCommunicationChannel : Communication_Channel_Of<Server, TrialClient> { }
        interface CommunicationChannel : Communication_Channel_Of<Server, FullFeaturedClient> { }
        interface TheChannel : Communication_Channel_Of<Server, FreeClient> { }
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

## Stages

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
A stage have to extend the built-in `xyz.unirail.Meta.L` and/or `xyz.unirail.Meta.R` interfaces.
The `L` and `R` are used to denote the left and right hosts, respectively, in the channel declaration, the stage belongs
to.
Immediately following the reference to the `L/R`side, the declaration of the side branches is initiated.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/1cd6ad55-7e0e-4167-9d4a-fef279b4fa11)

It is possible for only one side to have the capability to send packets.

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/f1cdc9e3-9e14-4781-af7b-ce46b3dc5234)

> <span style = "font-size:30px">⚠️</span> A short `block comment` with some symbols `/*įĂ*/` represents auto-sets
> unique
> identifiers.
> These identifiers are utilized to identify entities. Therefore, you can relocate or rename entities, but the
> identifier will remain unchanged.
> It is important to never edit or clone this identifier.

## Branches

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

## Named packs set

If you recognize a pattern or repetition in a set of packets, you have the option to create a `named set of packets` and
refer to them by their assigned name.
This allows for easier referencing and reusability of packet sets within your code.  
![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/8637f064-75e7-4ab0-8c66-c7625a7aa813)

`Named packet sets` can be declared anywhere in your `project` and may contain references to individual `packets` as
well as other `named packet sets`.

## Timeout

The `Timeout` attribute on a stage sets the maximum it duration. If the attribute is not specified, the stage can
remain indefinitely

Let's take a look at the **snippet** of the communication flow part in the protocol description file used by AdHocAgent
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

## `Optional` and `mandatory` fields

A pack's field can be `optional` or `mandatory`.

* `mandatory` fields are always allocated and transmitted, even if they have not been modified or filled with data.
* `optional` fields, on the other hand, only allocate a few bits if they have not been modified.

In the AdHoc protocol description, primitive data types that end with a `?` (such as `int?`, `byte?`,
etc.) are considered 'optional', as all non-primitive data types such as `string` and `array`.

```csharp
class Packet
{
    string user; //non-primitive data types are always optional
    uint? optional_field; //optional uint field
}     
```

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

As the protocol creator, you have the best understanding of your data and its specific requirements. The AdHoc protocol
description provides attributes that allow you to share this knowledge with the code generator, enabling optimized code
generation.

Adding the `MinMax` attribute to a field declaration is recommended when you know the data falls within a specific
range.
It provides explicit information to the code generator, enabling it to optimize data type selection and generate
range-bound information for efficient value handling.

For value **ranges** less than 127, the code generator utilizes internal bits storage to save memory.

Example:

```csharp
     [MinMax(1, 8)] int car_doors;     
```

In this case, the code generator can allocate 3 bits in the bits storage for the `car_doors` field, based on the
specified range of 1 to 8.

If a field's value falls within the range of `200 005` to `200 078`,

```csharp
     [MinMax(200_005, 200_078)] int field;     
```

The code generator will allocate the necessary storage for values within the specified
range: `73`(`200 078` - `200 005` = `73`).
Additionally, it will generate `getter` and `setter` methods that adding or subtracting a constant value of `200,005`
to or from the field value.

## Varint type

If a numeric field contains randomly distributed values that span the entire range of the numeric type, it can be
visualized as follows:

![image](https://user-images.githubusercontent.com/29354319/70127303-bdf40900-16b5-11ea-94c9-c0dcd045500f.png)

Compressing this data type would be inefficient and wasteful in terms of computing resources.  
However, if the numeric field exhibits a particular dispersion or gradient pattern within its value range, as shown in
the following image:

![image](https://user-images.githubusercontent.com/29354319/70128574-0a404880-16b8-11ea-8a4d-efa8a7358dc1.png)

compressing the data type might still be beneficial to minimize the amount of data transmission. In such cases,
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
    [MinMax(-1128, 873)]    byte  field2;   // Mandatory field without compression, accepting values from -1128 to -873.
    [X]          short? field3;   // Optional field taking values from -32,768 to 32,767. Compressed using the ZigZag algorithm.
    [A(1000)]    short field4;   // Mandatory field taking values from -1,000 to 65,535. Compressed during transmission.
    [V]          short? field5;   // Optional field taking values from -65,535 to 0. Compressed during transmission.
    [MinMax(-11, 75)] short field6;   // Mandatory field with uniformly distributed values within the specified range.
```

## Array type

The array can store and return various data types, such as primitives, `strings`, `maps`, `sets`, and other `packs`.
Fields with the `Array type` are always 'optional'.

When the type of the field in a collection such as `Map`, `Set`, `String`, or  `array`, it
becomes crucial to have control over the individual length. Especially in network applications.
By default, the maximum length of collections is restricted to 255 elements.

This restriction for collections, like `String`, `Set`, and `Map`, can be adjusted using the rightmost
argument of the `[Dims(+N)]` attribute.
> Note the `+` character. `N` is the maximum length of the entity.

```csharp
class Packet{
    string                          string_with_max_255_chars;   
    [Dims(+100)] string             string_with_max_100_chars;   
    
    Map<int,string>                 map_with_max_255_items;   
    [Dims(+1_000)] Map<int,string>  map_with_max_1_000_items;
}
```

A flat array of items is declare in a same way as C# array with the `[Dims]` attribute. The rightmost argument of
the `[Dims]` attribute is the array params

| +N | N is the maximum length an array with a variable size  (which is  a List).             |
|---:|----------------------------------------------------------------------------------------|
| -N | N is the maximum length of the array, with a fixed length set at field initialization. |
|  N | N is the length of the constant length array                                           |

```cs
using xyz.unirail.Meta;

class Pack{
    [Dims(4)]  Point[] field2; //constant length array of 4 Ponts.  
    [Dims(+4)] short[] field1; //variable length array of maximum 4 shorts - List 
}
```

### Multidimensional array

In AdHoc, a field also can have a multidimensional array type with constant or fixed dimensions.
The same `[Dims(N, -N, N)]`  attribute is used to declare a field as a multidimensional array.

|    |                                                                                              |
|---:|:---------------------------------------------------------------------------------------------|
|  N | N Represents the length of the constant length dimension.                                    |
| -N | N Indicates the maximum length of a fixed length dimension, set during field initialization. |

The generated API allows you to access both individual items by index and arrays of items.

```cs
using xyz.unirail.Meta;

class Pack {
    [Dims(2,  3,  4)] int    ints; 
    [Dims(2, -3, -4)] Point  points; 
    [Dims(2,  3,  4)] string strings_with_max_255_chars; 
}
```

> The lengths of all **fixed dimensions** in the multidimensional arrays are set during **field initialization**.

### Multidimensional array of arrays

```csharp
class Packet{
    
    [Dims(100)]  string            array_of_100_strings_with_max_255_chars;   
    [Dims(+100)] string            string_with_max_100_chars;   
    [Dims(+100)] string []         list_of_max_100_strings_with_max_255_chars;
       
    [Dims(100, +100)] string       array_of_100_strings_with_max_100_chars;   
    [Dims(100, +100)] string[]     array_of_100_lists_of_max_100_strings_with_max_255_chars;   
    
    [Dims(+100, +1024)] string[]   list_of_upto_100_strings_with_max_1024_chars;   
    [Dims(+100)] string []         list_of_upto_100_strings_with_max_255_chars;   
    [Dims(-100)] string            array_of_100_strings_with_max_255_chars;   
    
    Map<int,string>                       map_with_max_255_items;   
    [Dims(+100,-10)] Map<int,string>[]    array_fixed_upto_10_maps_with_max_100_items;
       
    [Dims(10, 10, 5, -100)] int []       multidimensional_array_of_arrays_of_100_ints;   
}
```

## String type

A `string` is, in fact, an immutable array of characters. Fields with the `String` type are always 'optional'.

```csharp
string  string_field;
```

By default, the `string` type in AdHoc protocol declares the `string` with a maximum length of 255 chars.

To declare a field with a different maximum character length restrictions, use the `[Dims]` attribute.

```csharp
class Packet{
    string                      string_field_with_max_255_chars;
    [Dims(+6)] string           string_field_with_max_6_chars;
    [Dims(+7000)] string        string_field_with_max_7000_chars;
    
    [Dims(100)]  string         array_of_100_strings_with_max_255_chars;   
    [Dims(100, +7_000)]         array_of_100_strings_with_max_7000_chars;   
}
```

> Note the use of the `+` sign before the maximum length value.


If you have certain `string` type restrictions that are used in multiple places, it's beneficial to declare and utilize
the AdHoc `typedef` construction:

```csharp
class max_6_chars_string{         // AdHoc typedef
    [Dims(+6)] string typedef;
}

class max_7000_chars_string{      // AdHoc typedef
    [Dims(+7_000)] string typedef;
}

class Packet{ //                         using typedef
    string                  string_field_with_max_255_chars;
    max_6_chars_string      string_field_with_max_6_chars;      
    max_7000_chars_string   string_field_with_max_7000_chars;   
    [Dims(100)] max_7000_chars_string   array_of_100_strings_with_max_7000_chars;   
}
```

> **When transmitting strings, they are encoded using the `Varint` algorithm rather than `UTF-8`.**
>

## Map/Set type

The description of fields with `Map`/`Set` datatype is straightforward.

By default, both `Map` and `Set` restricted to hold up to 255 items. However, you can adjust this restriction by using
the `[Dims]` attribute.

The `[MapValueParams]` attribute serves as a delimiter between a Map type "Key attributes" and "Value attributes." Any
attributes following the `MapValueParams` are applied to the Map's `value` type. This allows for more specific
customization of the
`value` type in the `Map`.

```csharp
using xyz.unirail.Meta;

[Dims(+20)]Set<uint>          max_20_uints_set; //The set is limited to a maximum of 20 items.
[Dims(+20)]Set<uint>[]        array_of_max_20_sets_of_max_255_uints; 
[Dims(+20,+20)]Set<uint>[]    array_of_max_20_sets_of_max_20_uints;
 
Set<float?>               max_255_floats_set;
Set<City>                 max_255_Cities_set;
[MinMax(4, 45)] Set<int>  max_255_ints_set_with_MinMax_attributes;

Map<string, byte?>                               max_255_items_string_2_byte_map;
[V, Dims(+100) ]            Map<uint?, ulong?>   max_100_items_only_key_attribute_map; // no more then 100 items
[MapValueParams, Dims(-57)] Map<uint, string>    max_255_items_map_with_value_max_57_chars;
[V, MapValueParams, V]      Map<uint?, ulong?>   max_255_items_map_with_key_value_attibutes;
```

> Note the use of the `+` sign before the maximum length value.

## Binary type

To declare the type as a raw binary array, you can use the `Binary` type from the `xyz.unirail.Meta` namespace. This
type will be recognized by the code generator, and it will handle the representation of the binary array appropriately
in
different target languages, such as using `byte` (signed) in **Java**, `byte` (unsigned) in **C#**, and `ArrayBuffer` in
**TypeScrip**, etc.

```csharp
using xyz.unirail.Meta;

class Result
{
    string                 task;
    [Dims(+650_000)] Binary[] result;// binary array with variable,  length max 65000 bytes
    [Dims(100)] Binary[] hash;       // binary array with constant length 100 bytes
}

```

## typedef

`Typedef` is used to create an alias (additional name) for a data type, not to create a new type.
When you need multiple fields to share the same type, you can declare and use `typedef`.
This approach allows you to modify the data type of all related fields at once easily.  
AdHoc `typedef` is declared with a C# class construction that contains declaration of only one field named `typedef`.
The **name** of the class becomes an alias for the type of its `typedef` field.

Example:
To modify the default 255-character restriction for the `string` type, you have to use `[Dims]` attribute. If
multiple fields have the same restriction, utilize `typedef` to share this meta information across all of them.

```csharp
class Packet{
    string                  string_field_with_max_255_chars;
    [Dims(+6)] string       string_field_with_max_6_chars;
    [Dims(+7_000)] string   string_field_with_max_7000_chars;
}
```

But if you use these string types in many places, consider declaring the typedef:

```csharp
class max_6_chars_string{         // AdHoc typedef
    [Dims(+6)] string typedef;
}

class max_7000_chars_string{      // AdHoc typedef
    [Dims(+7_000)] string typedef;
}

class Packet{ //                         using typedef
    string                  string_field_with_max_255_chars;
    max_6_chars_string      string_field_with_max_6_chars;      
    max_7000_chars_string   string_field_with_max_7000_chars;   
    [Dims(100)] max_7000_chars_string   field_array_of_100_strings_with_max_7000_chars;   
}
```

## Pack/enum type

Both `enums` and `packs` can be used as data types for a field.
`Enums` are used to represent a set of named constant values, while `packs` are user-defined data structures that can
hold
multiple fields with various data types. By using `enums` and `packs` as field data types, you can organize and manage
different types of data effectively in your code.

In packs, you can have nested data types and self-referential fields within the fields' data type.
This allows for building complex data structures with interconnected components.

However, empty `packs` (with no fields) or `enums` with fewer than two fields are not suitable as field data types. In
such
cases, when only a single binary value is needed, it's recommended to use a `boolean` type instead. This ensures that
the
data type remains meaningful and avoids unnecessary complexity when dealing with single binary values.


<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using xyz.unirail.Meta;

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

        interface CommunicationChannel : Communication_Channel_Of<Server, Client>{ }
    }
}
```

result

![image](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/4c485e72-fea2-4886-b1aa-28444657fe71)
</details>
