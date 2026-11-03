# Welcome to AdHoc

This private repository is yours. Below is your personal UUID and everything you need to generate your first code.

> [!IMPORTANT]
> **Your personal UUID**
>
> ```
> {{UUID}}
> ```
>
> It admits AdHocAgent to the generator instead of a login and a password. Keep it to yourself.

## 1. Install .NET and AdHocAgent

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), then open a terminal and install AdHocAgent, the command-line utility:

**Linux and macOS**

```shell
curl -fsSL https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.sh | bash
```

**Windows, in PowerShell**

```powershell
irm https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.ps1 | iex
```

Open a new terminal afterwards, so that it finds the `AdHocAgent` command.

## 2. Give AdHocAgent your UUID

Run this command once, in the terminal:

```shell
AdHocAgent {{UUID}}
```

AdHocAgent stores the UUID as `PersonalVolatileUUID` in `AdHocAgent.toml`, beside its executable.

> [!WARNING]
> Keep `AdHocAgent.toml`. The server may replace your UUID during a generation, and AdHocAgent saves the new one in that file, so the UUID shown above
> may stop working after your first generation. Updating AdHocAgent keeps the file.

## 3. Create the description project

A description of your protocol is a C# source file. The project only lets your IDE check it; it is never run.

1. Create a C# class library in your IDE: [Rider](https://www.jetbrains.com/rider/), [Visual Studio Code](https://code.visualstudio.com/) or [Visual Studio](https://visualstudio.microsoft.com/vs/community/).
2. Add [`Meta.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs) to the project as an existing file. It holds the AdHoc vocabulary for your IDE.

## 4. Get the template

Run this in the folder of your description project:

```shell
AdHocAgent
```

It prints its help, writes the template `MyProtocolDescription.cs` into the folder and waits for Enter. Edit that file to describe your own protocol: the template passes every check as it is, so you can also go on with it unchanged.

## 5. Generate the code

```shell
AdHocAgent MyProtocolDescription.cs
```

The generated code arrives in the folder `MyProtocolDescription`, with one folder per language and host, and AdHocAgent writes `MyProtocolDescription.md` beside it. Keep your description file under version control: AdHocAgent writes the identity numbers of your packs into it.

## 6. Put the code into your project

Open `MyProtocolDescription.md`. At the end of the line of each host you use, write the folder it goes to; at the end of the line of each host you do not use, put `⛔`:

```markdown
- 📁[InCS](/D:/Work/MyProtocolDescription/InCS)
	- 📁[Client](/D:/Work/MyProtocolDescription/InCS/Client) ⛔
	- 📁[Server](/D:/Work/MyProtocolDescription/InCS/Server) [](/D:/Projects/server/protocol/)
```

Then run:

```shell
AdHocAgent MyProtocolDescription.md
```

Add `protocol/gen` and `protocol/lib` to the source folders of your build. From now on, every generation of this description goes to the same folders.

> [!WARNING]
> A destination folder belongs to the generator: a file in it that the deployment does not write is backed up and then deleted. Use a folder for generated code only, never a project root.

## If something goes wrong

| What happens | What to do |
|:--|:--|
| `AdHocAgent` is not found | Open a new terminal. |
| You lost `AdHocAgent.toml`, or your UUID is rejected | [Post in the Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up) again. A new UUID appears on this page, and the previous one stops working. |
| You need anything else | [The manual](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/MANUAL.md#adhocagent) |
