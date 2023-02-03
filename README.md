
# *Achtung!!!*  
![image](https://user-images.githubusercontent.com/29354319/204679188-d5b0bdc7-4e47-4f32-87bb-2bfaf9d09d78.png)

Writing data serialization and deserialization code manually in different programming languages
can be very time-consuming and prone to errors, 
especially when working with heterogeneous devices. A more efficient solution is to use a Domain-Specific Language (DSL) that formally
describes the protocol
and then generates source code based on this description for various target platforms and programming languages.
required programming languages.


This approach can be seen in Protocol Buffers, Cap'n Proto and others which are available at the links provided. 
[Protocol Buffers ](https://developers.google.com/protocol-buffers/docs/overview)  
[Cap’n Proto ](https://capnproto.org/language.html)  
[FlatBuffers ](http://google.github.io/flatbuffers/flatbuffers_guide_writing_schema.html)  
[ZCM ](https://github.com/ZeroCM/zcm/blob/master/docs/tutorial.md)  
[MAVLink ](https://github.com/mavlink/mavlink)  
[Thrift](https://thrift.apache.org/docs/idl)

I have studied several approaches to handling binary protocols and decided to create my own system, 
called AdHoc protocol,to address the weaknesses I found in these approaches.


AdHoc is a code generator that supports multiple programming languages, including C#, Java, and Typescript, with 
plans to add support for C++, Rust, and GO in the future.
The AdHoc server generates code based on a protocol description file,
You must implement the received packet handlers and packet-producing logic, fill packs with data and send them
to the recipient.

**AdHoc** generator features:

- bitfields,
- ordinary and nullable primitive datatypes
- strings, Maps, Set datatypes
- multidimensional fields with predefined and variable dimensions
- nested packs, enums
- ordinary and flags-like enums
- fields with enum and other pack datatypes
- host and packet level constants
- pack's fields inheritance
- host's communication interfaces inheritance
- importing packs and communication interfaces from other AdHoc protocol descriptions
- [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding) compression.
- generate ready-to-use network infrastructure

The code generator is currently available as a SaaS (Software as a Service)[**SaaS**](https://en.wikipedia.org/wiki/Software_as_a_service). 
To use it, you will need to:

- install **.NET**.
- install a **C#** IDE such as (**[Intellij IDEA](https://www.jetbrains.com/rider/) / [VSCode](https://code.visualstudio.com/) / [Visual Studio](https://visualstudio.microsoft.com/vs/community/)** )
- download the source code of the [AdHoс protocol metadata attributes](https://github.com/cheblin/AdHoc-protocol/tree/master/org/unirail/AdHoc).
 - or use the version embedded in the AdHocAgent binary.  
 Add a reference to the metafile with attributes in your **AdHoc protocol** description project.
- Use the **[AdHocAgent](https://github.com/cheblin/AdHocAgent)** utility to upload your protocol description file to the server and receive the generated code. You can either download a [prebuilt version](https://github.com/cheblin/AdHocAgent/tree/master/bin)  or download the source.

# AdHocAgent utility

The AdHocAgent utility is a command-line tool that processes the following input:

The first argument is the path to the task file, which may include a parameter at the end to specify the task type:
  - `.cs`  - upload the protocol description file to the server to generate the source code
  - `.cs!` - upload the protocol description file to generate the source code and test the source code  
  
>The remaining arguments are:
> - paths to source files `.cs` and  project files `.csproj`that are imported and used in the root protocol description file
> - The path to a temporary folder to store files received from the server.
> - a path to a binary file, that will be executed after deployment (⚠️ the temporary folder will be the working directory for this executable file).
>
> 
>In this case ⚠️, the utility will also need an instruction file for deploying the source code received from the server.  
>The file should be named with the protocol description file name followed by  `Deployment.md` e.g. - `MyProtocol_file_nameDeployment.md`  
>
>AdHocAgent utility will search for this file in the following locations:
>- The description file folder
>- The `Working directory`  
>
>If the utility cannot find the file in these locations, it will extract a template of the file in the description file folder, which you can then edit with the correct deployment instructions.

Otherwise if the first argument path ends with the parameters:
  - `.cs?` - this parameter will instruct utility to show the information of the provided protocol description file in the viewer.
  - `.proto` - it means the provided `.proto` file is in [Protocol Buffers](https://developers.google.com/protocol-buffers) format. The utility will send it to the server to convert into Adhoc protocol description format.
If there is a path to a folder among the arguments the utility will use this folder as the intermediate result output folder. Otherwise -  the utility will use the current working directory.

⚠️In addition to the command-line arguments, the AdHocAgent utility needs the following:
- An `AdHocAgent.toml` - the file that contains
  - The URL of the code-generating server.   
   
  - And paths to the local IDE
  - [7zip compression](https://www.7-zip.org/download.html) utility(used for best compression)

AdHocAgent utility will search for the `AdHocAgent.toml` file next to itself.
If it cannot find the file in this location, it will generate a template that you can update with your configuration information. Thus it is only necessary to update the information in this file according to your configuration.

# Overview

The minimal protocol description file may look like this:

```csharp
using org.unirail.Meta; //        importing AdHoc protocol attributes. Required!

namespace com.my.company // You company namespace. Required!
{
 	public interface MyProject { // MyProject  ddeclare AdHoc protocol description project 

        ///<see cref = 'InTS'/>
        struct Server : Host // generate code for this host in TypeScript
        {
            public interface ToClient
            {
                class PacketToClient { } // empty packet to send to client
            }
        }

        ///<see cref = 'InJAVA'/> 
        struct Client : Host // generate code for this host in JAVA
        {
            public interface ToServer
            {
                class PacketToServer { } // empty packet to send to server
            }
        }

        interface Channel : Communication_Channel_Of<Client.ToServer, Server.ToClient> { } //communication channel
    }
}
```
If you want to view the structure of a protocol description file, you can use the AdHocAgent utility with the path to the file followed by a question mark e.g. `\AdHocAgent.exe /dir/minimal_descr_file.cs?` . The utility will display the following scheme

<details>
  <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/194577494-11945417-1531-42a3-b426-f1c7e50d9ea2.png)
</details>
To upload a file and generate the source code, you can simply pass the path to the AdHocAgent utility `AdHocAgent.exe /dir/minimal_descr_file.cs`. This will require a deployment instructions file `minimal_descr_fileDeployment.md`


# Protocol description file format

>### The protocol description file follows a specific naming convention:
>
>- Names should not start or end with an underscore `_`.
>- Names should not match any keywords defined by the programming languages that the code generator supports. **AdHocAgent** will check for and warn about such conflicts before uploading.
>
>- The generator will suggest names that are as close to the original as possible.  
   >  Generated suggestions are always public and accessible.  But, if the identifier starts with an underscore `_`, it means that the value is stored in an internal format
>- for visual separation and better IntelliSense support in IDEs, methods generated for serializer internal needs will start with the 'ˉ' Unicode symbol




## Project

We choose the C# language as [`DSL`](https://en.wikipedia.org/wiki/Domain-specific_language) to describe **AdHoc protocol**.
Generally, the protocol description file is a plain C# source code in a .Net project.
To create a protocol description file, you can start by creating a **C#** project and adding a reference to the [AdHoc protocol metadata attributes.](https://github.com/cheblin/AdHoc-protocol/tree/master/src/org/unirail/AdHoc),
Then, create a new C# source file and declare the protocol description using C# `interface` enclosed in your company's namespace.

```csharp
using org.unirail.Meta;//        importing AdHoc protocol attributes. Required!

namespace com.my.company// You company namespace. Required!
{
    public interface MyProject { // MyProject  ddeclare AdHoc protocol description project 
        
    } 
}
```

The **AdHoc protocol** not only defines the data format, including packs and fields, but it also includes features for describing the full network topology. This includes details on hosts, channels, and the relationships between them. 

For example, consider the following protocol description file:

```csharp

namespace com.my.company
{
	public interface MyProject { 
        ///<see cref = 'InJAVA'/>
        struct Server: Host{
            public interface SendToTrialClient {
                class ServerPack{} // << packets that Server can create and send throught this port
            }

            public interface SendToClient{
                class Login{}
                class PackB{}
            }
        }
        
        ///<see cref = 'InTS'/>
        struct FullFeaturedClient: Host {
            public interface ToServer {
                class FullFeaturedClientPack{} // <<  packets that FullFeaturedClient can create and send throught this port
            }    
        }

        ///<see cref = 'InCS'/>
        struct TrialClient: Host {
            public interface ToServer {
                class TrialClientPack{}
            }    
        }

        ///<see cref = 'InTS'/>
        struct FreeClient: Host {
            public interface ToServer{
 		    
            }    
        }

        interface TrialCommunicationChannel : Communication_Channel_Of<Server.SendToTrialClient, TrialClient.ToServer> { }
        interface CommunicationChannel : Communication_Channel_Of<Server.SendToClient, FullFeaturedClient.ToServer> { }
        interface TheChannel : Communication_Channel_Of<Server.SendToClient, FreeClient.ToServer> { }
    } 
}
```
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>and if you observe it with AdHocAgent utility viewer you may see the following</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/194736174-420beea6-9a54-49af-bd34-5378bca7c898.png)

If you select a channel you may see what packets are involved exactly and where do they go

![image](https://user-images.githubusercontent.com/29354319/194736235-a35b2625-c358-483e-81b5-009ddd20e1c9.png)
</details>

## Hosts

In the AdHoc protocol, hosts are represented as C# `struct` within a project `interface` scope. These hosts participate in the exchange of information and must implement the org.unirail.Meta.Host marker interface.   
Host should implements 
Through XML tags  [<see cref="member">](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#cref-attribute)
in the host declaration's code documentation, it is possible to specify the language in which the host's source code will be generated, as well as any relevant options.
The built-in marker interfaces `InCS`, `InJAVA`, `InTS` etc. allow for the declaration of language configuration scopes.

Items (such as packs or fields) mentioned within the rolling scope will be given the configuration of that scope.
The most recent language configuration scope becomes the default for the rest of the items within the host.

In the following example, the language configuration for the 'Server' host in this file is:
```csharp
using org.unirail.Meta;

namespace com.my.company// You company namespace. Required!
{
    public interface MyProject { 
        /** 
        <see cref = 'InCS'/>+-
        <see cref = 'InJAVA'/>++
            <see cref = 'ToAgent.Result'/> 
            <see cref = 'Agent.ToServer.Proto'/>
            <see cref = 'Agent.ToServer.Login'/>  
        <see cref = 'InJAVA'/>--
        */
        struct Server: Host {
		
	    }
    } 
}
```
AdHocAgent utility could be read the in this way..
<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

![image](https://user-images.githubusercontent.com/29354319/194581459-7769bbe3-6004-4f9a-bccd-5579d97a762f.png)


</details>


## Ports

Each host has ports. They are declared with C# `interface` construction.

```csharp
using org.unirail.Meta;

namespace org.unirail
{
    public interface AdhocProtocol : _<AdhocProtocol.Agent.Project.Host.Port.Pack.Field.DataType>//propagate DataType constants set to all hosts
    {
        /** 
        <see cref = 'InJAVA'/>
        <see cref = 'ToAgent.Result'/> 
        <see cref = 'Agent.ToServer.Proto'/>
        <see cref = 'Agent.ToServer.Login'/>  
        <see cref = 'InJAVA'/>--
        */
        struct Server : Host
        {
            /**
            * public port ToAgent
            */
            public interface ToAgent : _<Upload> // Server's port
            {
             
            }
        }
    }
}
```


Hosts can connect to each other through communication channels, known as ports. 
Packs that are added to or declared within a host's port can be **create and send** to another host by the host.
A pack or packs declared in a port can also be added to other ports through C# inheritance.

```csharp
            ///<see cref="Full.Path.To.ThePack"/>           <<< add single pack `ThePack` as is
            ///<see cref="Full.Path.To.TheOtherPort"/>      <<< add all packs from `Full.Path.To.TheOtherPort` port as is
            ///<see cref="Full.Path.To.SkipPack"/>-         <<< delete single `SkipPack` pack
            interface SomePort: _<Full.Path.To.SomePack>,   //<<< add single pack `SomePack` by inheritance (user _<> interface wrapper from org.unirail.Meta)
                                Full.Path.To.TheOtherPort   //<<< add all packs from `Full.Path.To.TheOtherPort` port as is
            {
                class Login{}
                class PackB{}
            }
```

## Channels

The **AdHoc protocol** uses channels to connect the ports of hosts.  
Channels are declared using a C# `interface` and, like hosts, are located directly within the project scope.
The channel's `interface` "extends" the `org.unirail.Meta.Communication_Channel_Of`  interface and its parameter list soecifies the two ports being connected.
For example:
```csharp
        interface TrialCommunicationChannel : Communication_Channel_Of<Server.SendToTrialClient, TrialClient.ToServer> { }
        interface CommunicationChannel : Communication_Channel_Of<Server.SendToClient, FullFeaturedClient.ToServer> { }
        interface TheChannel : Communication_Channel_Of<Server.SendToClient, FreeClient.ToServer> { }
```


## Packs

Packs are the minimal **transmittable** information unit declared by `class` C# construction.
Pack declarations can be nested and declared anywhere inside a host scope.
Pack's none constant fields - are a list of information this pack transmits. Constant fields produce pack scope constants.
A packet can be empty(without any fields). In this case, the carried information is the fact of packet transmission.

It is possible to add/'inherit' **all fields** from other packs.
With comment `<see cref="Full.Path.To.SourcePack"/>` on destination packet or with C# "inheritance" from other packet wrapped into `_<>` interface wrapper.

Particular fields can be inherited/embedded from another packet with `<see cref="Full.Path.To.OtherPack.Field"/>` comment on destination packet.

Adding fields cannot override the existing fields (with the same name).

Comments `<see cref="Full.Path.To.OtherPack.Field"/>-` or `<see cref="Full.Path.To.Source.Pack"/>-` allow deleting individual or all imported fields, respectively.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject
    {
        public class AnyPacksFields
        {
            long   received_time;
            long   global_uid;
            string client_hashcode;
        }

        ///<see cref = 'InJAVA'/>
        struct Server : Host
        {
            public interface SendToClient
            {
                ///<see cref="AnyPacksFields"/> embed all fields from AnyPacksFields
                ///<see cref="AnyPacksFields.client_hashcode"/>- exclude AnyPacksFields.client_hashcode
                class ServerPack { } // << packets that Server can create and send throught this port
            }
        }


        ///<see cref = 'InTS'/>
        struct Client : Host
        {
            public interface SendToServer
            {
                class Login : _<AnyPacksFields> // embed all fields from AnyPacksFields
                {
                    string user;
                    string password;
                }
            }
        }

        interface CommunicationChannel : Communication_Channel_Of<Server.SendToClient, Client.SendToServer> { }
    }
}
```
![image](https://user-images.githubusercontent.com/29354319/194756050-d2a39c5a-4c3a-452c-b17d-3337497e5587.png)

</details>

When process this description file, generator embed in the source the packets assigned id information.

![image](https://user-images.githubusercontent.com/29354319/194849405-e65f0ef9-482d-43ec-8e13-f78a137caec2.png)

## Empty packs

Empty packets have no fields. Implemented as singleton. Used as most efficient way to signal about something.


## Value pack

Packages with information that fits into 8 bytes are special - `Value` packs.
These packets do not allocate on the heap, their data is stored in primitive types.
Code generator provides methods to pack and unpack the pack's fields data.

Packs with one primitive type field are `Value` by default.

## Enums and constants

If you need to propagate on hosts some constants, and they have the same integral type: use enums

`[Flags]` attributes Indicates that an [enumeration can be treated as a bit field; that is, a set of flags.](https://learn.microsoft.com/en-us/dotnet/api/system.flagsattribute)

None-initialized enum fields are automatically assigned integer values. If enum has `[Flags]` attribute, generated value,
 is a **bit flags** like respectively


If your constants have non primitive datatype, use static/const fields optionally:
-  Of the pack, they are logically related with.
-  Or the `constants set` declared with struct C# construction.

enums and  `constants set` are copied on every host and never transfered.

Value of constants declared with `static` fields may be assigned as a number or as a result of a static expression.
You can use any available C# functions. Values are calculated during the code generating time.

The constants declared with `const` fields can be used as `attributes parameters`. It has a value, but its value should be the result of a compile-time expression.
Due to language restrictions the standard static C# functions cannot be used to calculate the value.
To overcomes this limitation, AdHoc protocol description syntax use `///<see cref="Pack.static_field_with_real_value"/>` construction.
At code generation time system "copy" value and type from `static` based constant  to `const` based constant.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject
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

            public interface SendToClient
            {
                class ServerPack { }
            }
        }

        ///<see cref = 'InTS'/>
        struct Client : Host
        {
            public interface SendToServer
            {
                class Login
                {
                    string user;
                    string password;

                    //======= static fields === constatns related to Login pack
                    static int      USE_ANY_FUNCTION = (int)Math.Sin(34) * 4 + 2;
                    static string[] STRINGS          = { "", "\0", "ere::22r" + "K\nK\n\"KK", STR };

                    static int PRIMITIV_CONST = 45 * (int)Server.MAV_BATTERY_FUNCTION.MAV_BATTERY_FUNCTION_ALL + 45 >> 2 + (USE_ANY_FUNCTION < 2
                    const string STR = "KKKK";
                    
                    ///<see cref="PRIMITIV_CONST"/> CONST_FROM_STATIC getting value and type from PRIMITIV_CONST
                    const int CONST_FROM_STATIC = 0;
                }
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

        interface CommunicationChannel : Communication_Channel_Of<Server.SendToClient, Client.SendToServer> { }
    }
}
```

</details>

The root description file's constants are propagating to all hosts.


## Importing other descriptions

C# `using` statement in the description file header, let use entities declared in other protocol description files.

```csharp
using com.company.ProtocolProject;
using com.other_company.OtherProtocolProject;
```

Imported, none-root file's constants are propagating only if explicitly mentioned in  C# inheritance construction of the root project.
Inheritance from other projects - propagates all **constants** that they have.
To propagate individual  items  - inherit from other projects in particular use `enum`/`constants set`.

```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject : Project_const_packs, Particular_const_pack {
        
    } 
}
```

This propagates all **constants** from `Project_const_packs` and `Particular_const_pack` to all hosts in `MyProject` project.

You can wrap `constants sets` to use them in C# where only interface allowed. The real AdhocProtocol description file example
```csharp
using org.unirail.Meta;

namespace org.unirail
{
    public interface AdhocProtocol : _<AdhocProtocol.Agent.Project.Host.Port.Pack.Field.DataType>//propagate DataType constants set to all hosts
    {
    }
}
```
![image](https://user-images.githubusercontent.com/29354319/194483836-0f6b542b-15ba-474b-b2ea-bc7f68b622e4.png)


# Fields

## `Optional` and `required` fields

A pack's field can be `optional` or `required`.

* `required` fields are always allocated and transmitted, even if hey were not modified and filled with data.
* `optional` fields, if were not modiied, allocate just a few bits


According to AdHoc protocol description rule, primitive data type ended with `?` (`int?`, `byte?`...), and non-primitive data types (`string`, `array`...) are `optional`.
```csharp
class Packet
{
    string user; //non-primitive data types are always optional
    uint? optional_field; //optional primitive field
}     
```


## Numeric fields

All range of C# numeric primitive types are available

| Type   | Range                                                    |
|--------|----------------------------------------------------------|
| sbyte  | \-128 to 127                                             |
| byte   | 0 to 255                                                 |
| short  | \-32,768 to 32,767                                       |
| ushort | 0 to 65,535                                              |
| int    | \-2,147,483,648 to 2,147,483,647                         |
| uint   | 0 to 4,294,967,295                                       |
| long   | \-9,223,372,036,854,775,808 to 9,223,372,036,854,775,807 |
| ulong  | 0 to 18,446,744,073,709,551,615                          |
| float  | ±1.5 x 10−45 to ±3.4 x 1038                              |
| double | ±5.0 × 10−324 to ±1.7 × 10308                            |

As a creator  of a protocol, you know the scope of data in advance better than any code generator. **AdHoc** description provides attributes
that let you share your knowledge with the generator and helps to generate optimal code.

If your field's data fits in a particular range, it is better to declare it with the MinMax attribute. Code generator will try to find the best 
datatype and generate range bounds information to control passing values.

If value **range** is less than 127, the code generator will store the value in internal bits storage.

```csharp
     [MinMax(1, 8)] int car_doors;     
```

for `car_doors` field code generator could allocate 3 bits in the bits storage.

If some field value is in range `200_005` to `200_078`,

```csharp
     [MinMax(200_005, 200_078)] int field;     
```
The code generator generates code, that store field in the bits storage and provide getter/setter to add/subtract `200 005` constant.

## Varint

If the numeric field has random values uniformly distributed in the whole numeric type range as noise, it may look like the following visual representation.

![image](https://user-images.githubusercontent.com/29354319/70127303-bdf40900-16b5-11ea-94c9-c0dcd045500f.png)

Any compression of this data type is a waste of computing resources.  
But, if the numeric fields have some particular dispersion/gradient pattern in its value scope as it presented in following image:

![image](https://user-images.githubusercontent.com/29354319/70128574-0a404880-16b8-11ea-8a4d-efa8a7358dc1.png)

Then it is possible to use this knowledge to minimize the amount of data transmission.  
In this case code generator can use [Base 128 Varint](https://developers.google.com/protocol-buffers/docs/encoding)
compression
[algorithm](https://en.wikipedia.org/wiki/Variable-length_quantity) on single fields and `Group Varint Encoding` on
array or list. 
That would allow to reduces the sending data amount optimizing the resource load.
This is achieved by skipping transmission of the higher bytes if they are zeros, and then restore them on the receiving.

This graph shows the dependence of sending bytes on transferred value.

![image](https://user-images.githubusercontent.com/29354319/70126207-84ba9980-16b3-11ea-9900-48251b545eef.png)

It is becoming clear that `Varint Encoding`, for smaller value produce less bytes to transmit.

Let highlight three basic types of numeric value changing patterns.

|                                                     pattern                                                     | description                                                                                                                                                               |
|:---------------------------------------------------------------------------------------------------------------:|:--------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| ![image](https://user-images.githubusercontent.com/29354319/155324344-311c6e30-fda5-4d38-b2c7-b946aca3bcf8.png) | Rare fluctuations are possible only in the direction of bigger<br> values relative to most probable `min` value. <br>Use `[ A(min, max) ]`  attribute for this field type |
| ![image](https://user-images.githubusercontent.com/29354319/155324459-585969ac-d7ef-4bdc-b314-cc537301aa1d.png) | Rare fluctuations are possible in both directions relative <br> to most probable `zero` value .<br> Use `[ X(amlitude, zero) ]`  attribute for this field type            |
| ![image](https://user-images.githubusercontent.com/29354319/155325170-e4ebe07d-cc45-4ffa-9b24-21d10c3a3f18.png) | Fluctuations are possible only in the direction of smaller<br> values relative to most probable `max` value.<br> Use `[ V(min, max) ]` attribute for this field type      |


```csharp
    [A]          uint?  field1;  //optional field, the data is compressable, the field can store values in the range from 0 to uint.MaxValue. 
    [MinMax(-1128, 873)]    byte  field2;   //required field, (without compression), the field accept values in range -1128 to -873                                                          
    [X]          short? field3;   //optional field takes values in the range  from -32 768 to 32 767. will be compressed with ZigZag algorithm.                                       
    [A(1000)]     short field4;   //required field takes a value between – 1 000 to 65 535 will be compressed on sending.                                                           
    [V]          short? field5;   //optional field takes a value between  -65 535  to 0  will be compressed on sending.                                                                 
    [MinMax(-11 , 75)] short field6;   //required field with uniformly distributed values in the specified range.     
                                                                                
```


## String fields

Strings in **AdHoc protocol** on all languages are encoded in UTF-8 byte array. All string fields are `optional`

```csharp
string  string_field;
```

## Multidimensional fields

Field can hold a multidimensional array of primitives, strings, maps, sets and packs.
Dimensions can be constant and variable length.

`[Dims]` attribute is used to declare multidimensional field: `Dims(N, -N, N)]`

| N  |N is length of constant dimension         |
|----| :-------------------------------------------|
| -N | N is maximum length of variable dimension  |

## Multidimensional array of arrays

Multidimensional array also can hold other arrays. In this case the last dimension(argument) of `Dims` attribute denote contained array's parameter.

<table>
  <tbody>
    <tr>
      <td>~N</td>
      <td>All contained arrays have the same variable length, N is it maximum, Exact length is determined at field creation
and cannot be changed.</td>
    </tr>
    <tr>
      <td>~~N</td>
      <td>Each contained array has its own length, N is a maximum length. Can be changed individually </td>
    </tr>
    <tr>
      <td>+N</td>
      <td>All arrays, if created, are have the same constant length - N.</td>
    </tr>
  </tbody>
</table>
The variable dimensions length are set at field initialization.

```cs
        class Pack {
            [Dims(2 , 3 , 4)] short field1; // multidimensional field of primitive with constant dimensions 

            [Dims(2, -3, -4)] Point field2; //field with multidimensional array of  Ponts. dimension 1 can be in rande 1..3. dimension 2 - in range 1..4 
            
            [Dims(2, -3, +4)] Pack array_field; //field with multidimensional array of constant length 4, arrays of Packs 
            
            [Dims(~17)] Pack var_len_array; //field with var length array of Packs, with max length 17 

            [Dims(2, -3, ~~4)] string array_field1; //multidimensional array of arays of strings. each array has its own length
        }
```
## Fields with binary array datatype

With special attribute `org.unirail.Binary` possible to declare a field with binary data type. Content of this type transfer directly.
This field type implementation depends on target platform. On JAVA it is signed `byte` on C# unsigned 'byte'


## Fields with Map/Set datatype

description is simple and straightforward

```csharp
            Set<uint>                 uint_set;
            Set<float?>               uint_set;
            Set<City>                 set_of_City_types;
            [MinMax(4, 45)] Set<uint> set_with_MinMax_attributes;

            Map<string, byte?>                            string_2_byte;
            [V]                        Map<uint?, ulong?> only_key_attribute_map;
            [MapValueParams, Dims(-5)] Map<uint, uint>    only_value_attribute_map;
            [V, MapValueParams, V]     Map<uint?, ulong?> map_with_key_value_attibutes;
```
`MapValueParams` attribute is `Key`-`Value` attributes delimiter. Attributes after this delimiter apply to `value` type.


## Fields with other pack/enum as datatype

Enums and pack can be a field's data type.

<details>
 <summary><span style = "font-size:30px">👉</span><b><u>Click to see</u></b></summary>

```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject
    {
        /**
         Flags for gimbal device (lower level) operation.
    */
        ///<see cref = 'InJAVA'/>
        struct Server : Host
        {
            public interface SendToClient
            {
                public class QuitRoomResponse
                {
                    MID?     mid;
                    [X] int? result;
                    [X] int? rid;
                }
            }
        }

        public class RoomInfo
        {
            [X] long  id;
            [X] int?  rank;
            [X] int?  tYpe;
            [X] long? cumulativeGold;
            [X] long? state;
        }

        enum RoomType { CLASSICS = 1, ARENA = 2, }

        enum MID
        {
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
        struct Client : Host
        {
            public interface SendToServer
            {
                public class RoomChangeResponse
                {
                    MID?      mid;
                    RoomInfo? roomInfo;

                    public class EnterRoomRequest
                    {
                        MID?     mid;
                        RoomType tYpe;
                        [X] int  rank;
                    }
                }
            }
        }

        interface CommunicationChannel : Communication_Channel_Of<Server.SendToClient, Client.SendToServer> { }
    }
}
```
result

![image](https://user-images.githubusercontent.com/29354319/194811082-59f47c44-f580-4a85-97ff-e41895006206.png)
</details>

Packs through their fields data type can be nested and self-referenced.
Empty packs or enums with less then two fields cannot be used as field datatype. Use boolean type instead of

