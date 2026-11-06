English | [简体中文](GETTING_STARTED.zh-CN.md) · [README](README.md) · [Manual](MANUAL.md)

# Getting started with AdHoc

*Pipeline: **description → AdHocAgent → generator → received folder → deployment** - the first pass through every step*

This guide takes you from an empty folder to generated code in your own project: install the tools, get access to the generator,
get the template, compose your protocol, generate, and deploy. Each step links to the chapter that explains it in full.

![The steps of this guide that you run or do, numbered as in the text, drawn as a U. Top row, left to right: step 2, AdHocAgent followed by your volatile UUID stores it as PersonalVolatileUUID in AdHocAgent.toml beside the executable; step 4, AdHocAgent with no arguments, run in the folder of your description project, writes the template MyProtocolDescription.cs there, never over a file of that name; step 5, you compose your protocol by editing that file in your IDE - packs and their fields, hosts and their languages, connections, states and branches - and the IDE checks the C# as you type. An arrow runs down the right side to the bottom row, which reads right to left: step 6, AdHocAgent MyProtocolDescription.cs - the file with its folder or without - adds the numbers tables to the description, fills the received folder MyProtocolDescription with .description, InCS, InJAVA and InTS host folders, writes MyProtocolDescription.md and stops; a note under it says that a UUID is taken for one by its form and anything else is a file, a missing one reported. Step 7: after you add to the .md file a route to /D:/Projects/server/protocol/ and the no-entry mark for unused hosts, AdHocAgent MyProtocolDescription.md copies gen, lib and LICENSE-GRANT.md of the host into D:/Projects/server/protocol, a folder for generated code only.](docs/img/dev-02-first-run.svg)

## What you need for AdHoc

| What                          | Why                                                                                    | Where                                                                                                                                                              |
|:------------------------------|:---------------------------------------------------------------------------------------|:-------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| .NET 10                       | AdHocAgent runs on .NET 10; the SDK also builds your description project               | install the .NET 10 SDK                                                                                                                                            |
| a C# IDE                      | it checks the description as you type: syntax, types, references to AdHoc's vocabulary | [Rider](https://www.jetbrains.com/rider/), [Visual Studio Code](https://code.visualstudio.com/), [Visual Studio](https://visualstudio.microsoft.com/vs/community/) |
| AdHocAgent                    | the command-line utility that checks, uploads, receives and deploys                    | [releases](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases)                                                                                              |
| `Meta.cs` or `AdHocAgent.dll` | the AdHoc vocabulary - `Host`, `Connects<L, R>`, the attributes - for your IDE         | [`Meta.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs), or the `AdHocAgent.dll` of the release                                        |
| a personal volatile UUID               | admits AdHocAgent to the generator, which is a cloud service                           | the sign-up below                                                                                                                                                  |

## Step 1: install .NET and AdHocAgent

1. Install the .NET 10 SDK.
2. Get AdHocAgent, in whichever of three ways suits you: install it with one command, download a ready build, or build it yourself.

   **Install with one command.** Linux and macOS:

   ```shell
   curl -fsSL https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.sh | bash
   ```

   Windows, in PowerShell:

   ```powershell
   irm https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.ps1 | iex
   ```

   The script picks the build for your system and processor from the latest release, checks it against the release's
   `SHA256SUMS.txt` and puts the `AdHocAgent` command on your PATH. On Linux and macOS the agent goes to
   `~/.local/share/adhoc-agent` and the command to `~/.local/bin`; on Windows both go to `%LOCALAPPDATA%\Programs\AdHocAgent`.
   To update, run the same command again.

   AdHocAgent keeps its configuration file, `AdHocAgent.toml`, beside its executable; the file appears when your personal volatile UUID is
   stored. An update keeps the file, and with it your volatile UUID.

   | Variable | What it changes |
   |:--|:--|
   | `ADHOC_VERSION` | the release to install, for example `v1.2.3`; the latest by default |
   | `ADHOC_INSTALL_DIR` | the folder of the agent |
   | `ADHOC_BIN_DIR` | Linux and macOS: the folder of the `AdHocAgent` command |
   | `ADHOC_DOWNLOAD_URL` | a mirror that holds the release files |
   | `ADHOC_NO_MODIFY_PATH=1` | Windows: leave PATH as it is |

   **Download a ready build.** The [releases page](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases) holds the
   latest version as an archive for each system and processor: `AdHocAgent-<win|linux|osx>-<x64|arm64>.zip`, with `SHA256SUMS.txt`
   beside them. Unpack the archive into a folder of its own and run `AdHocAgent` from there.

   **Build it yourself.** The repository holds everything the build needs:

   ```shell
   git clone https://github.com/AdHoc-Protocol/AdHoc-protocol
   cd AdHoc-protocol
   dotnet publish AdHocAgent.csproj -c Release --use-current-runtime --self-contained false -o publish
   ```

   The agent is in the `publish` folder. The repository's workflow runs the same build for every system on each version tag, and
   that is what the releases page holds.
3. Install a C# IDE if you do not have one.

## Step 2: get your personal volatile UUID

The generator runs on a server, and AdHocAgent identifies you to it by a personal volatile UUID instead of a login and a password. A volatile UUID needs
no interactive sign-in, so AdHocAgent can run unattended in a build pipeline.

1. Sign in to GitHub.
2. Post a message in the [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up).
3. Within 2-3 minutes the post disappears and your GitHub email gets an invitation to your private repository in the
   AdHoc-Protocol organization. Accept it: the repository opens on your volatile UUID.
4. Run AdHocAgent once with the volatile UUID:
   
   ```shell
   AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f
   ```

AdHocAgent stores the UUID as `PersonalVolatileUUID` in `AdHocAgent.toml`. The UUID is *volatile*: the server may replace it during
a generation, and AdHocAgent then saves the new one in the same file. Keep that file. What to do when a volatile UUID is rejected is in
[AdHocAgent](MANUAL.md#adhocagent).

## Step 3: create the description project

A description is a C# source file, and your IDE checks it only when it belongs to a C# project that knows AdHoc's vocabulary. The
project is never run; it exists for the IDE.

1. Create a C# project in your IDE - a class library is enough.
2. Give it the vocabulary with your IDE's usual means - either way works:
	- add [`Meta.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs) to the project as an existing file (or as a link to
	  where you keep it);
	- or add a reference to the `AdHocAgent.dll` that came with AdHocAgent.

<details>
<summary>The same in the project file (for scripts and AI tools)</summary>

With `Meta.cs` linked from a folder beside the project:

```xml

<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <Compile Include="..\AdHoc\Meta.cs" Link="Meta.cs"/>
    </ItemGroup>
</Project>
```

With a reference to `AdHocAgent.dll`:

```xml

<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
    </PropertyGroup>
    <ItemGroup>
        <Reference Include="AdHocAgent">
            <HintPath>..\AdHocAgent\AdHocAgent.dll</HintPath>
        </Reference>
    </ItemGroup>
</Project>
```

</details>

The vocabulary uses generic attributes such as `[l____________<PACKS>]`, which need C# 11 or later; any project for .NET 7 or newer
has it.

> [!IMPORTANT]
> AdHocAgent has the vocabulary built in. Give it your description files only - never `Meta.cs`, and never a `.csproj` that
> compiles `Meta.cs`. AdHocAgent compiles every file it is given as part of the description, so `Meta.cs` would be checked as one
> and stop the run with an error about its names.

## Step 4: get the template

Run AdHocAgent with no arguments in the folder of your description project:

```shell
AdHocAgent
```

It prints its help, writes the template `MyProtocolDescription.cs` into the current folder and waits for Enter. An existing file of
that name is kept: the template is not written then. An SDK-style project includes every `.cs` file in its folder, so the IDE sees the new file at once.

The template is the canonical first description, and it passes AdHocAgent's checks as it is. Its code lines are these (the comments
are shortened here):

```csharp
using org.unirail.Meta; // the AdHoc vocabulary

namespace com.my.company // required
{
    public interface MyProject // the project
    {
        enum Mood : byte { Calm, Happy, Angry } // an enum: 2 bits on the wire

        class Style // a Value Pack: 8 bits, kept as one integer
        {
            Mood                 mood;
            bool                 bold;
            bool                 italic;
            [MinMax(0, 15)] byte color;  // 4 bits
        }

        class Message // declared in the project: common to all hosts
        {
            public const int MAX_TEXT = 1000;

            string?                author; // optional
            [D(+MAX_TEXT)] string  text;   // at most MAX_TEXT characters
            Style                  style;  // a Value Pack as a field
            long                   time;
        }

        /// <see cref="InCS"/>   C#: concrete code
        /// <see cref="InJAVA"/> Java: concrete code
        /// <see cref="InTS"/>-  TypeScript: abstract code
        struct Server : Host // a host
        {
            public class Welcome
            {
                [D(100)] Message[,,] recent; // up to 100 messages
            }

            public class Refused
            {
                string reason;
            }

            public class Online // a Value Pack sent on its own
            {
                [MinMax(0, 10_000)] ushort people; // a declared range: 14 bits
                [MinMax(0, 10_000)] ushort typing;
            }

            public class Accepted { }

            public class History
            {
                [D(100)] Message[,,] older;
            }
        }

        /// <see cref="InTS"/>   TypeScript: concrete code
        /// <see cref="InCS"/>-  C#: abstract code
        /// <see cref="InJAVA"/> Java: concrete code
        struct Client : Host // a host
        {
            public class SignIn
            {
                [D(+32)] string name;
                string?         password;
            }

            public class SignOut{ } // an empty pack

            public class FileOffer
            {
                [D(+255)]                      string name;
                [MinMax(1, FileData.MAX_SIZE)] int    size;
            }

            public class FileData
            {
                public const int MAX_SIZE = 1_000_000;

                [D(MAX_SIZE)] Binary[,] bytes; // raw bytes
            }

            public class HistoryRequest
            {
                long before;
            }
        }

        // The connection: Client is the left host, Server the right host.
        interface Chat : Connects<Client, Server>
        {
            // Actor0: the states written right in the connection
            [L____________<Answer, Client.SignIn>]
            struct Start { }

            [ReceiveTimeout(10)]
            [____________R<Talk, Server.Welcome>]
            [____________R<Close, Server.Refused>]
            struct Answer { }

            [l____________<Message>]
            [____________r<(Message, Server.Online)>]
            [L____________<Close, Client.SignOut>]
            struct Talk { }

            interface Upload : Actor // a named actor
            {
                int Actor.MaxActiveInstances => 4;

                [L____________<Offered, Client.FileOffer>]
                struct Start { }

                [____________R<Sending, Server.Accepted>]
                [____________R<End, Server.Refused>]
                struct Offered { }

                [L____________<End, Client.FileData>]
                struct Sending { }
            }

            // an RPC method
            (L____________, Server.History) history(Client.HistoryRequest request);
        }
    }
}
```

It is a small chat. A `Client` signs in; the `Server` welcomes it with the recent messages, or refuses; then they talk until the
`Client` signs out. Beside the chat, on the same connection, the `Client` uploads files, several at once, and asks for older
messages. The template's comment lines tell how to see all of it drawn: `AdHocAgent MyProtocolDescription.cs?` starts the
[Observer](OBSERVER.md) at `http://localhost:4321/`.

- `using org.unirail.Meta;` brings in the vocabulary. The namespace is required: AdHocAgent refuses a project outside a namespace.
  The project is the top-level interface `MyProject`. See [The description file](MANUAL.md#the-description-file).
- `Message`, `Style` and the enum `Mood` are declared in the project, so any host may use them. The other packs are declared inside a host
  and belong to it. `SignOut` and `Accepted` are empty packs: they carry no fields. `FileOffer` takes the limit of its `size` from
  the constant `FileData.MAX_SIZE`. See [Packs](MANUAL.md#packs), [Fields and types](MANUAL.md#fields-and-types) and
  [Constants and enums](MANUAL.md#constants-and-enums).
- `Style` is a Value Pack: its fields fit in 64 bits together - 8 bits here - so the generated code keeps it as one integer, with no
  object behind it. Nothing marks it; the generator sees it by its fields. `Online` is one as well and travels alone: the pack id
  and 4 bytes. See [Value Packs](MANUAL.md#value-packs).
- `Server` and `Client` are hosts. Each doc comment line names a language with a marker; a `-` right after the marker asks for *abstract* code in that
  language, no mark for *concrete* code. So `Server` gets abstract TypeScript, concrete C# and concrete Java;
  `Client` gets concrete TypeScript, abstract C# and concrete Java. Concrete packs are classes that hold their values; abstract packs
  are interfaces your own types implement. See [Hosts, languages, generated code](MANUAL.md#hosts-languages-generated-code).
- `Chat` links `Client`, its left host, with `Server`, its right host. See [Connections](MANUAL.md#connections).
- `Start`, `Answer` and `Talk` are the states of the conversation. In a branch, `l` is the left host and `r` the right one. A capital
  letter moves the conversation to the state named first: `SignIn` leads from `Start` to `Answer`, and `Close` ends the connection.
  A small letter stays in the state: in `Talk`, the two sides send `Message` any number of times. The side with the capital letter
  leads the state: only it moves the conversation on. `[ReceiveTimeout(10)]` on `Answer`: the `Client` waits at most 10 seconds
  for the answer, then the connection closes. See [Actors, states, branches](MANUAL.md#actors-states-branches) and [Connections](MANUAL.md#connections).
- These three states belong to `Actor0`, the connection's own conversation; the first one declared is where it starts. `Upload` is
  a named actor: a conversation of its own beside `Actor0`, up to 4 of them at once on one connection. In `Start` the `Client` leads,
  in `Offered` the `Server`, in `Sending` the `Client` again. `End` ends this one upload, while `Close` ends the whole connection.
  `history` is an RPC method: one request, one reply, generated as an actor too, `Call → Reply → End`. Named actors and RPC methods
  do not wait for the sign-in in `Actor0`; roles keep them closed until it. See
  [Actor kinds and the actor at runtime](MANUAL.md#actor-kinds-and-the-actor-at-runtime) and [Roles and rights](MANUAL.md#roles-and-rights).

## Step 5: compose your protocol

The template is a starting point. Compose your own protocol by editing `MyProtocolDescription.cs` in your IDE: rename the project
and the namespace, declare the packs your programs exchange and their fields, the hosts and the languages each one needs, and the
connections with their states and branches. The IDE checks the C# as you type - syntax, types, references to AdHoc's vocabulary;
AdHoc's own rules are checked by AdHocAgent in the next step.

A first edit of the template: the `Client` gets a new pack, and the branch of `Talk` lists it.

```csharp
struct Client : Host
{
    // SignIn and SignOut as they are

    public class Typing                 // a new pack of the Client host
    {
        [MinMax(1, 100)] int room;      // a field with a declared range
    }
}

// and in the state Talk of Chat:
[l____________<(Message, Client.Typing)>]
```

A pack that no branch lists is never sent on its own, so a new pack takes effect only when a branch names it. What you can declare,
and where it is explained:

| You declare                              | Chapter                                                                                                                                                                                        |
|:-----------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| packs, their fields and field types      | [Packs](MANUAL.md#packs), [Fields and types](MANUAL.md#fields-and-types)                                                                                                                                         |
| constants and enums                      | [Constants and enums](MANUAL.md#constants-and-enums)                                                                                                                                                    |
| hosts and their languages                | [Hosts, languages, generated code](MANUAL.md#hosts-languages-generated-code)                                                                                                                            |
| connections, actors, states, branches    | [Connections](MANUAL.md#connections), [Actors, states, branches](MANUAL.md#actors-states-branches)                                                                                                               |
| sessions, streams, tunnels, shared ports | [Sessions, guaranteed delivery, UDP](MANUAL.md#sessions-guaranteed-delivery-udp), [Streams](MANUAL.md#streams), [Virtual connections and relays](MANUAL.md#virtual-connections-and-relays), [Multiplexing](MANUAL.md#multiplexing)   |

The rest of this guide works with any description; its examples show the template's file names.

## Step 6: generate the first code

Run AdHocAgent with the description file, with its folder or without:

```shell
AdHocAgent MyProtocolDescription.cs
AdHocAgent .\MyProtocolDescription.cs        # the same, with the folder
```

A full path works as well. With `!` after the name - `AdHocAgent .\MyProtocolDescription.cs!` - the code is also tested, as far as
the server's resources allow.

AdHocAgent tells a volatile UUID by its form - `100b9fd2-e593-485b-a2fe-9b9c82bc1e3f` - and a file name never has it; a file that is not there
is reported with its full path.

What the run does:

1. AdHocAgent compiles the description and checks it. An error stops the run here with the file, the line and what to fix; nothing
   has been sent.
2. It writes the numbers tables into your description file: the identity numbers of the packs, hosts, connections, actors and states, and short
   `/*…*/` marks after the branches. For the template, your file now begins like this (the project's own number differs for every project):
   
   ```csharp
   namespace com.my.company // required
   {
       /** packs
           <see cref='Client.FileData'/>ĉ          8
           <see cref='Client.FileOffer'/>Ĉ         6
           <see cref='Client.HistoryRequest'/>Ċ    9
           <see cref='Client.SignIn'/>Ć            0
           <see cref='Client.SignOut'/>ć           4
           <see cref='Message'/>Ā                  3
           <see cref='Mood'/>ċ
           <see cref='Server.Accepted'/>Ą          7
           <see cref='Server.History'/>ą           10
           <see cref='Server.Online'/>ă            5
           <see cref='Server.Refused'/>Ă           2
           <see cref='Server.Welcome'/>ā           1
           <see cref='Style'/>ÿ
       */
       /** project
           <see cref='MyProject'/>ĞĽƌĔĎ

           hosts
           <see cref='Server'/>ÿ
           <see cref='Client'/>Ā

           connections
           <see cref='Chat'/>ÿ

           actors
           <see cref='Chat.Upload'/>Ā
           <see cref='Chat.history'/>ā

           states
           <see cref='Chat.Answer'/>Ā
           <see cref='Chat.Start'/>ÿ
           <see cref='Chat.Talk'/>ā
           <see cref='Chat.Upload.Offered'/>Ā
           <see cref='Chat.Upload.Sending'/>ā
           <see cref='Chat.Upload.Start'/>ÿ
       */
       public interface MyProject // the project
   ```
   
   and the branches read `[l____________<Message>/*Ā*/]`. The decimal column is each pack's wire
   id; `Mood` and `Style` have none, as they never travel on their own. These numbers are the identities of your entities: keep the rewritten file under version control, and never edit the numbers
   or the marks. See [Numbers tables](MANUAL.md#numbers-tables).
3. It uploads the project and shows the generator's progress. The server generates the code; with `!` after the name it also tests
   it, as far as its resources allow.
4. The generated files arrive in the **received folder** `MyProtocolDescription`, named after the description file and created in
   the output folder - the current folder unless you name another one as the last argument. AdHocAgent clears the received folder
   at the start of every generation, so never edit files there.
5. On the first run there is no deployment instructions file yet. AdHocAgent writes `MyProtocolDescription.md` beside the received
   folder, says so, and stops.

To check a description without uploading it, and for every other argument and option, see [AdHocAgent](MANUAL.md#adhocagent).

## What comes back from the generator

For the template, the output folder now holds, in outline:

```text
MyProtocolDescription.cs           your description, with its numbers tables
MyProtocolDescription.md           the deployment instructions, written on the first run
MyProtocolDescription/             the received folder
├─ .description/                   a copy of the description files of this generation
├─ InCS/
│  ├─ Client/                      gen/, lib/, LICENSE-GRANT.md, demo.cs, Project.csproj
│  └─ Server/                      the same for the Server host
├─ InJAVA/
│  ├─ Client/                      gen/, lib/, LICENSE-GRANT.md, demo/
│  └─ Server/
└─ InTS/
   ├─ Client/                      gen/, lib/, LICENSE-GRANT.md, demo.ts, package.json, tsconfig.json
   └─ Server/
```

`gen` is the generated code of the host, `lib` the AdHoc runtime library, `demo` a skeleton of the host you can start from. What each
part is for is in [What AdHoc generates for each host](MANUAL.md#what-adhoc-generates-for-each-host).

## Step 7: deploy the first code into your project

Open `MyProtocolDescription.md`. It lists the language folders and the host folders in them, and it explains itself in its quick
start. To deploy a host, write a **route** at the end of its line - a Markdown link whose target is a folder of your project - and
put `⛔` at the end of the line of every host you do not use:

```markdown
- 📁[InCS](/D:/Work/MyProtocolDescription/InCS)
	- 📁[Client](/D:/Work/MyProtocolDescription/InCS/Client) ⛔
	- 📁[Server](/D:/Work/MyProtocolDescription/InCS/Server) [](/D:/Projects/server/protocol/)
```

Then run the deployment on the files already received:

```shell
AdHocAgent .\MyProtocolDescription.md
```

AdHocAgent reports what it is about to do; when the list of received files has changed or a destination holds files it would
delete, it asks for a `y` before it writes anything. The host's `gen`, `lib` and license file land in `D:\Projects\server\protocol\`.
Add `protocol/gen` and `protocol/lib` to the source folders of your build. From now on, every generation of this description
deploys by the same routes.

> [!WARNING]
> A destination folder belongs to the generator. A file in it that the deployment does not write is backed up and then deleted. Route
> each host to a folder that holds generated code only - a `protocol` folder beside your sources - never to a project root.

AdHoc's own services are laid out this way: each has a `protocol` folder that holds `gen`, `lib` and `LICENSE-GRANT.md`, and nothing
else. To deploy a `demo` or the project files as well, routes with filters, smart merge and backups, see
[Deployment and smart merge](MANUAL.md#deployment-and-smart-merge).

## Where to go next with AdHoc

- The description file itself - projects, the C# constructs, naming rules: [The description file](MANUAL.md#the-description-file).
- Data - packs, fields and their types, constants, the bytes on the wire: [Packs](MANUAL.md#packs), [Fields and types](MANUAL.md#fields-and-types),
  [Constants and enums](MANUAL.md#constants-and-enums), [How values become bytes](MANUAL.md#how-values-become-bytes).
- Hosts and their languages: [Hosts, languages, generated code](MANUAL.md#hosts-languages-generated-code).
- Conversations - connections, actors, states, branches: [Connections](MANUAL.md#connections),
  [Actors, states, branches](MANUAL.md#actors-states-branches).
- Every task and option of the command-line utility: [AdHocAgent](MANUAL.md#adhocagent).
