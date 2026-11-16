[English](GETTING_STARTED.md) | 简体中文 · [README](README.zh-CN.md) · [手册](MANUAL.zh-CN.md)

<a id="getting-started-with-adhoc"></a>
# AdHoc 快速入门

*流程：**描述 → AdHocAgent → 生成器 → 接收文件夹 → 部署**——每一步的第一遍*

本指南带你从一个空文件夹走到自己项目中的生成代码：安装工具、获得生成器的访问权、拿到模板、编写你的协议、
生成、部署。每一步都链接到完整讲解它的章节。

![本指南中由你运行或完成的各步骤，编号与正文一致，画成一个 U 形。顶行从左到右：第 2 步，AdHocAgent 后跟你的易失 UUID，把它作为 PersonalVolatileUUID 存入可执行文件旁的 AdHocAgent.toml；第 4 步，不带参数运行在你的描述项目文件夹中的 AdHocAgent，在那里写出模板 MyProtocolDescription.cs，绝不覆盖同名文件；第 5 步，你在 IDE 中编辑该文件来编写你的协议——pack 及其字段、主机及其语言、连接、状态和分支——IDE 在你输入时检查 C#。一条箭头沿右侧下行到底行，底行从右往左读：第 6 步，AdHocAgent MyProtocolDescription.cs——文件带不带文件夹均可——把编号表加入描述，用 .description、InCS、InJAVA 和 InTS 主机文件夹填充接收文件夹 MyProtocolDescription，写出 MyProtocolDescription.md 并停止；它下方的注释说明，按格式识别 UUID，其余都是文件，不存在的文件会被报告。第 7 步：你在 .md 文件中加入一条到 /D:/Projects/server/protocol/ 的路由和未使用主机的禁入标记后，AdHocAgent MyProtocolDescription.md 把该主机的 gen、lib 和 LICENSE-GRANT.md 复制进 D:/Projects/server/protocol，一个只放生成代码的文件夹。](docs/img/dev-02-first-run.zh-CN.svg)

<a id="what-you-need-for-adhoc"></a>
## 使用 AdHoc 需要什么

| 什么                          | 为什么                                                                                | 哪里                                                                                                                                                                 |
|:------------------------------|:--------------------------------------------------------------------------------------|:---------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| .NET 10                       | AdHocAgent 运行在 .NET 10 上；SDK 也用于构建你的描述项目                                | 安装 .NET 10 SDK                                                                                                                                                     |
| 一个 C# IDE                   | 它在你输入时检查描述：语法、类型、对 AdHoc 词汇的引用                                    | [Rider](https://www.jetbrains.com/rider/)、[Visual Studio Code](https://code.visualstudio.com/)、[Visual Studio](https://visualstudio.microsoft.com/vs/community/)   |
| AdHocAgent                    | 检查、上传、接收和部署的命令行工具                                                       | [releases](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases)                                                                                                |
| `Meta.cs` 或 `AdHocAgent.dll` | AdHoc 词汇——`Host`、`Connects<L, R>`、各特性——供你的 IDE 使用                           | [`Meta.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs)，或发布版中的 `AdHocAgent.dll`                                                     |
| 一个个人易失 UUID                 | 准许 AdHocAgent 使用生成器——一个云服务                                                   | 下方的注册                                                                                                                                                           |

<a id="step-1-install-net-and-adhocagent"></a>
## 步骤 1：安装 .NET 和 AdHocAgent

1. 安装 .NET 10 SDK。
2. 获取 AdHocAgent，三种方式任选其一：用一条命令安装、下载现成的构建，或自己构建。

   **用一条命令安装。** Linux 和 macOS：

   ```shell
   curl -fsSL https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.sh | bash
   ```

   Windows，在 PowerShell 中：

   ```powershell
   irm https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.ps1 | iex
   ```

   脚本从最新发布中挑选适合你的系统和处理器的构建，用发布的 `SHA256SUMS.txt` 校验它，并把 `AdHocAgent` 命令放到你的
   PATH 上。在 Linux 和 macOS 上，代理放在 `~/.local/share/adhoc-agent`，命令放在 `~/.local/bin`；在 Windows 上两者都放在
   `%LOCALAPPDATA%\Programs\AdHocAgent`。要更新，再次运行同一条命令。

   AdHocAgent 把自己的配置文件 `AdHocAgent.toml` 放在可执行文件旁边；这个文件在存储你的个人易失 UUID 时出现。更新会保留
   这个文件，连同其中你的易失 UUID。

   | 变量 | 它改变什么 |
   |:--|:--|
   | `ADHOC_VERSION` | 要安装的发布，例如 `v1.2.3`；默认为最新 |
   | `ADHOC_INSTALL_DIR` | 代理的文件夹 |
   | `ADHOC_BIN_DIR` | Linux 和 macOS：`AdHocAgent` 命令的文件夹 |
   | `ADHOC_DOWNLOAD_URL` | 存放发布文件的镜像 |
   | `ADHOC_NO_MODIFY_PATH=1` | Windows：保持 PATH 不变 |

   **下载现成的构建。** [发布页面](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases)上有最新版本，每种系统和处理器一个
   压缩包：`AdHocAgent-<win|linux|osx>-<x64|arm64>.zip`，旁边是 `SHA256SUMS.txt`。把压缩包解压到一个独立的文件夹，然后从那里运行
   `AdHocAgent`。

   **自己构建。** 仓库里有构建所需的一切：

   ```shell
   git clone https://github.com/AdHoc-Protocol/AdHoc-protocol
   cd AdHoc-protocol
   dotnet publish AdHocAgent.csproj -c Release --use-current-runtime --self-contained false -o publish
   ```

   代理在 `publish` 文件夹中。仓库的工作流在每个版本标签上为所有系统运行同样的构建，发布页面上的就是它的结果。
3. 如果你还没有 C# IDE，安装一个。

<a id="step-2-get-your-personal-volatile-uuid"></a>
## 步骤 2：获取你的个人易失 UUID

生成器运行在服务器上，AdHocAgent 用个人易失 UUID 而不是登录名和密码向它标识你。易失 UUID 不需要交互式登录，因此
AdHocAgent 可以在构建流水线中无人值守地运行。

1. 登录 GitHub。
2. 在 [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up) 中发布一条消息。
3. 2-3 分钟内，该帖子会消失，你的 GitHub 邮箱会收到一封邀请，邀请你加入 AdHoc-Protocol 组织中属于你的私有仓库。
   接受邀请：仓库打开时显示的就是你的易失 UUID。
4. 带上易失 UUID 运行一次 AdHocAgent：
   
   ```shell
   AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f
   ```

AdHocAgent 把 UUID 作为 `PersonalVolatileUUID` 存入 `AdHocAgent.toml`。这个 UUID 是*易失的*：服务器可能在一次
生成期间替换它，AdHocAgent 随后会把新值保存到同一个文件中。请保管好该文件。易失 UUID 被拒绝时该怎么办，见
[AdHocAgent](MANUAL.zh-CN.md#adhocagent)。

<a id="step-3-create-the-description-project"></a>
## 步骤 3：创建描述项目

描述是一个 C# 源文件，只有当它属于一个认识 AdHoc 词汇表的 C# 项目时，IDE 才会检查它。这个项目永远不会
被运行；它是为 IDE 而存在的。

1. 在你的 IDE 中创建一个 C# 项目——类库即可。
2. 用 IDE 的常规方式把词汇表交给它——两种方式都可以：
	- 把 [`Meta.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs) 作为现有文件加入项目（或作为指向
	  你存放位置的链接）；
	- 或者添加对 AdHocAgent 附带的 `AdHocAgent.dll` 的引用。

<details>
<summary>项目文件中的等价写法（供脚本和 AI 工具使用）</summary>

从项目旁边的文件夹链接 `Meta.cs`：

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

通过引用 `AdHocAgent.dll`：

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

词汇表使用了泛型特性，如 `[l____________<PACKS>]`，这需要 C# 11 或更高版本；任何面向 .NET 7 或更新版本的项目
都具备。

> [!IMPORTANT]
> AdHocAgent 内置了词汇表。只把你的描述文件交给它——永远不要给 `Meta.cs`，也不要给会编译 `Meta.cs` 的
> `.csproj`。AdHocAgent 会把它收到的每个文件都当作描述的一部分来编译，因此 `Meta.cs` 会被当作描述检查，
> 并因其中的名字报错而中止运行。

<a id="step-4-get-the-template"></a>
## 步骤 4：获取模板

在你的描述项目的文件夹中不带参数运行 AdHocAgent：

```shell
AdHocAgent
```

它会打印帮助，把模板 `MyProtocolDescription.cs` 写入当前文件夹，然后等待回车。同名的现有文件会被
保留：此时不写出模板。SDK 风格的项目会包含文件夹中的每个 `.cs` 文件，因此 IDE 会立即看到这个新文件。

模板是规范的第一份描述，它原样就能通过 AdHocAgent 的检查。它的代码行如下（此处的注释有所缩短）：

```csharp
using org.unirail.Meta; // AdHoc 词汇表

namespace com.my.company // 必需
{
    public interface MyProject // 项目
    {
        enum Mood : byte { Calm, Happy, Angry } // 枚举:线路上占 2 位

        class Style // 一个 Value Pack:8 位,作为一个整数保存
        {
            Mood                 mood;
            bool                 bold;
            bool                 italic;
            [MinMax(0, 15)] byte color;  // 4 位
        }

        class Message // 在项目中声明:所有主机共用
        {
            public const int MAX_TEXT = 1000;

            string?                author; // 可选
            [D(+MAX_TEXT)] string  text;   // 最多 MAX_TEXT 个字符
            Style                  style;  // Value Pack 作为字段
            long                   time;
        }

        /// <see cref="InCS"/>   C#: concrete code
        /// <see cref="InJAVA"/> Java: concrete code
        /// <see cref="InTS"/>-  TypeScript: abstract code
        struct Server : Host // 一台主机
        {
            public class Welcome
            {
                [D(100)] Message[,,] recent; // 最多 100 条消息
            }

            public class Refused
            {
                string reason;
            }

            public class Online // 单独发送的 Value Pack
            {
                [MinMax(0, 10_000)] ushort people; // 声明的范围:14 位
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
        struct Client : Host // 一台主机
        {
            public class SignIn
            {
                [D(+32)] string name;
                string?         password;
            }

            public class SignOut{ } // 一个空 pack

            public class FileOffer
            {
                [D(+255)]                      string name;
                [MinMax(1, FileData.MAX_SIZE)] int    size;
            }

            public class FileData
            {
                public const int MAX_SIZE = 1_000_000;

                [D(MAX_SIZE)] Binary[,] bytes; // 原始字节
            }

            public class HistoryRequest
            {
                long before;
            }
        }

        // 连接:Client 是左侧主机,Server 是右侧主机。
        interface Chat : Connects<Client, Server>
        {
            // Actor0:直接写在连接中的状态
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

            interface Upload : Actor // 一个具名 Actor
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

            // 一个 RPC 方法
            (L____________, Server.History) history(Client.HistoryRequest request);
        }
    }
}
```

它是一个小型聊天。`Client` 登录；`Server` 用最近的消息欢迎它，或者拒绝它；然后双方交谈，直到 `Client` 退出。
在聊天之外，`Client` 还在同一个连接上上传文件（可以同时上传多个），并请求更早的消息。模板的注释行说明了如何
看到这一切的图示：`AdHocAgent MyProtocolDescription.cs?` 会在 `http://localhost:4321/` 启动
[AdHoc Observer](OBSERVER.md)。

- `using org.unirail.Meta;` 引入词汇表。命名空间是必需的：AdHocAgent 拒绝命名空间之外的项目。
  项目是顶层接口 `MyProject`。见[描述文件](MANUAL.zh-CN.md#the-description-file)。
- `Message`、`Style` 和枚举 `Mood` 声明在项目中，因此任何主机都可以使用它们。其余 pack 声明在主机内部，属于该主机。
  `SignOut` 和 `Accepted` 是空 pack：它们不带字段。`FileOffer` 的 `size` 上限取自常量 `FileData.MAX_SIZE`。
  见 [Packs](MANUAL.zh-CN.md#packs)、[字段和类型](MANUAL.zh-CN.md#fields-and-types) 和[常量和枚举](MANUAL.zh-CN.md#constants-and-enums)。
- `Style` 是 Value Pack（值包）：它的字段合起来不超过 64 位——这里是 8 位——因此生成的代码把它保存为一个整数，
  背后没有对象。它不需要任何标记；生成器根据它的字段识别它。`Online` 也是 Value Pack，并且单独发送：pack ID
  加 4 个字节。见 [Value Packs](MANUAL.zh-CN.md#value-packs)。
- `Server` 和 `Client` 是主机。每行文档注释用一个标记命名一种语言；紧跟标记之后的 `-` 请求该语言的*抽象*代码，无标记则是*具体*代码。因此 `Server` 得到抽象 TypeScript、具体 C# 和具体 Java；
  `Client` 得到具体 TypeScript、抽象 C# 和具体 Java。具体 pack 是持有自身值的类；抽象 pack
  是由你自己的类型实现的接口。见[主机、语言、生成的代码](MANUAL.zh-CN.md#hosts-languages-generated-code)。
- `Chat` 把 `Client`（它的左侧主机）与 `Server`（它的右侧主机）连接起来。见[连接](MANUAL.zh-CN.md#connections)。
- `Start`、`Answer` 和 `Talk` 是对话的状态。分支中 `l` 是左侧主机，`r` 是右侧主机。大写字母把对话转到首先列出的
  状态：`SignIn` 从 `Start` 转到 `Answer`，`Close` 结束连接。小写字母停留在该状态：在 `Talk` 中，双方可以
  任意多次发送 `Message`。带大写字母的一方主导该状态：只有它能让对话继续前进。`Answer` 上的 `[ReceiveTimeout(10)]`：
  `Client` 最多等待 10 秒的答复，之后连接关闭。见 [Actor、状态、分支](MANUAL.zh-CN.md#actors-states-branches) 和[连接](MANUAL.zh-CN.md#connections)。
- 这三个状态属于 `Actor0`，即连接自己的对话；最先声明的状态就是对话开始的地方。`Upload` 是一个具名 Actor：
  在 `Actor0` 之外的一段独立对话，一个连接上最多同时进行 4 个。在 `Start` 中由 `Client` 主导，在 `Offered` 中由
  `Server` 主导，在 `Sending` 中又由 `Client` 主导。`End` 只结束这一次上传，而 `Close` 结束整个连接。
  `history` 是一个 RPC 方法：一个请求，一个答复，同样生成为一个 Actor，`Call → Reply → End`。具名 Actor 和
  RPC 方法不会等待 `Actor0` 中的登录；在登录之前由角色把它们关闭。见
  [Actor 的种类与运行时的 Actor](MANUAL.zh-CN.md#actor-kinds-and-the-actor-at-runtime) 和[角色与权限](MANUAL.zh-CN.md#roles-and-rights)。

<a id="step-5-compose-your-protocol"></a>
## 步骤 5：编写你的协议

模板是一个起点。在 IDE 中编辑 `MyProtocolDescription.cs` 来编写你自己的协议：重命名项目
和命名空间，声明你的程序之间交换的 pack 及其字段、主机及各自需要的语言，以及
带有状态和分支的连接。IDE 会在你输入时检查 C#——语法、类型、对 AdHoc 词汇表的引用；
AdHoc 自己的规则由 AdHocAgent 在下一步检查。

对模板的第一次编辑：`Client` 得到一个新 pack，`Talk` 的分支列出它。

```csharp
struct Client : Host
{
    // SignIn 和 SignOut 保持原样

    public class Typing                 // Client 主机的一个新 pack
    {
        [MinMax(1, 100)] int room;      // 带声明范围的字段
    }
}

// 以及在 Chat 的状态 Talk 中:
[l____________<(Message, Client.Typing)>]
```

没有被任何分支列出的 pack 永远不会被单独发送，因此新 pack 只有在分支点名时才生效。你可以声明什么，
以及在哪里有说明：

| 你声明的内容                             | 章节                                                                                                                                                                                           |
|:-----------------------------------------|:-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| pack、字段和字段类型                     | [Packs](MANUAL.zh-CN.md#packs)、[字段和类型](MANUAL.zh-CN.md#fields-and-types)                                                                                                                                                |
| 常量和枚举                               | [常量和枚举](MANUAL.zh-CN.md#constants-and-enums)                                                                                                                                                             |
| 主机及其语言                             | [主机、语言、生成的代码](MANUAL.zh-CN.md#hosts-languages-generated-code)                                                                                                                                       |
| 连接、Actor、状态、分支                  | [连接](MANUAL.zh-CN.md#connections)、[Actor、状态、分支](MANUAL.zh-CN.md#actors-states-branches)                                                                                                                              |
| 会话、流、隧道、共享端口                 | [会话、保证送达、UDP](MANUAL.zh-CN.md#sessions-guaranteed-delivery-udp)、[流](MANUAL.zh-CN.md#streams)、[虚拟连接和中继](MANUAL.zh-CN.md#virtual-connections-and-relays)、[多路复用](MANUAL.zh-CN.md#multiplexing)                                           |

本指南其余部分适用于任何描述；示例中使用的是模板的文件名。

<a id="step-6-generate-the-first-code"></a>
## 步骤 6：生成第一份代码

带着描述文件运行 AdHocAgent，文件带不带文件夹均可：

```shell
AdHocAgent MyProtocolDescription.cs
AdHocAgent .\MyProtocolDescription.cs        # 同上，带文件夹
```

完整路径也可以。名字后加 `!`——`AdHocAgent .\MyProtocolDescription.cs!`——代码还会在服务器资源允许的范围内经过测试。

AdHocAgent 按格式识别易失 UUID——`100b9fd2-e593-485b-a2fe-9b9c82bc1e3f`——而文件名永远不会是这种格式；不存在的文件会连同它的
完整路径被报告。

这次运行做了什么：

1. AdHocAgent 编译描述并检查它。有错误时，运行到此停止，给出文件、行号和要修复的内容；此时还没有
   发送任何东西。
2. 它把编号表写入你的描述文件：pack、主机、连接、Actor 和状态的标识编号，以及分支之后的简短
   `/*…*/` 标记。对于模板，你的文件现在开头是这样的（项目自己的编号因项目而异）：
   
   ```csharp
   namespace com.my.company // 必需
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
       public interface MyProject // 项目
   ```
   
   分支则变成 `[l____________<Message>/*Ā*/]`。十进制那一列是每个 pack 的线路
   ID；`Mood` 和 `Style` 没有线路 ID，因为它们从不单独发送。这些编号是你的实体的身份：把改写后的文件纳入版本控制，并且永远不要编辑这些编号
   或标记。见[编号表](MANUAL.zh-CN.md#numbers-tables)。
3. 它上传项目并显示生成器的进度。服务器生成代码；名字后加 `!` 时，还会在资源允许的范围内测试它。
4. 生成的文件到达**接收文件夹** `MyProtocolDescription`——以描述文件命名，在
   输出文件夹中创建；除非你以最后一个参数指定另一个文件夹，否则输出文件夹就是当前文件夹。AdHocAgent 在每次生成
   开始时清空接收文件夹，因此永远不要在那里编辑文件。
5. 第一次运行时还没有部署指令文件。AdHocAgent 在接收文件夹
   旁边写入 `MyProtocolDescription.md`，说明这一点，然后停止。

要只检查描述而不上传，以及所有其他参数和选项，见 [AdHocAgent](MANUAL.zh-CN.md#adhocagent)。

<a id="what-comes-back-from-the-generator"></a>
## 生成器返回了什么

对于模板，输出文件夹现在大致包含：

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

`gen` 是主机生成的代码，`lib` 是 AdHoc 运行时库，`demo` 是你可以从它起步的主机骨架。每个
部分的用途见[AdHoc 为每个主机生成什么](MANUAL.zh-CN.md#what-adhoc-generates-for-each-host)。

<a id="step-7-deploy-the-first-code-into-your-project"></a>
## 步骤 7：把第一份代码部署到你的项目中

打开 `MyProtocolDescription.md`。它列出了语言文件夹及其中的主机文件夹，并在它的快速
入门中解释自己。要部署一个主机，在它那一行的末尾写一条**路由**——一个目标为你项目中某个文件夹的 Markdown 链接——并
在你不使用的每个主机的行尾放上 `⛔`：

```markdown
- 📁[InCS](/D:/Work/MyProtocolDescription/InCS)
	- 📁[Client](/D:/Work/MyProtocolDescription/InCS/Client) ⛔
	- 📁[Server](/D:/Work/MyProtocolDescription/InCS/Server) [](/D:/Projects/server/protocol/)
```

然后在已接收的文件上运行部署：

```shell
AdHocAgent .\MyProtocolDescription.md
```

AdHocAgent 会报告它将要做什么；当接收文件清单有变化，或某个目标中有它要
删除的文件时，它会在写入任何内容之前询问 `y`。主机的 `gen`、`lib` 和许可证文件落在 `D:\Projects\server\protocol\`。
把 `protocol/gen` 和 `protocol/lib` 加入你构建的源文件夹。从此，这个描述的每次生成
都按同样的路由部署。

> [!WARNING]
> 目标文件夹属于生成器。其中不由部署写入的文件会先被备份，然后被删除。把
> 每个主机路由到一个只存放生成代码的文件夹——你的源码旁边的 `protocol` 文件夹——永远不要路由到项目根目录。

AdHoc 自己的服务就是这样布局的：每个服务都有一个 `protocol` 文件夹，其中只有 `gen`、`lib` 和 `LICENSE-GRANT.md`，没有
别的。要同时部署 `demo` 或项目文件，以及带过滤器的路由、智能合并和备份，见
[部署与智能合并](MANUAL.zh-CN.md#deployment-and-smart-merge)。

<a id="where-to-go-next-with-adhoc"></a>
## 接下来去哪里

- 描述文件本身——项目、C# 构造、命名规则：[描述文件](MANUAL.zh-CN.md#the-description-file)。
- 数据——pack、字段及其类型、常量、线上的字节：[Packs](MANUAL.zh-CN.md#packs)、[字段和类型](MANUAL.zh-CN.md#fields-and-types)、
  [常量和枚举](MANUAL.zh-CN.md#constants-and-enums)、[值如何变成字节](MANUAL.zh-CN.md#how-values-become-bytes)。
- 主机及其语言：[主机、语言、生成的代码](MANUAL.zh-CN.md#hosts-languages-generated-code)。
- 对话——连接、Actor、状态、分支：[连接](MANUAL.zh-CN.md#connections)、
  [Actor、状态、分支](MANUAL.zh-CN.md#actors-states-branches)。
- 命令行工具的每个任务和选项：[AdHocAgent](MANUAL.zh-CN.md#adhocagent)。
