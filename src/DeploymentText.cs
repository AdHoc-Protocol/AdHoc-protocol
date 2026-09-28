namespace org.unirail{
    // The text of a freshly generated deployment instructions file (AdHocAgent.Deployment.deploy): everything a user
    // needs to route the received files without opening the manual.
    //
    // Three things the parser does (AdHocAgent.Deployment.process) shape how it is written:
    //  - a line of the tree is recognised ANYWHERE by "- <icon>[name](path)", so no example starts with "- " + an icon;
    //  - "[before deployment](...)" / "[after deployment](...)" run wherever they appear, so the example uses a
    //    full-width bracket "［";
    //  - a ```regexp block followed by a ```shell or ```csharp block is a step that runs, so the explanations use
    //    ```text blocks, and only the real steps use those fences.
    //
    // Placeholders: {received} - the received folder's name, {md_name} - this file's name, {md_path} - its full path.
    static class DeploymentText{
        public const string HEADER = """
# Deployment instructions: {received}

AdHocAgent copies the code it received - the folder `{received}` next to this file - into your projects, following the
routes you write at the ends of the lines of the tree below. It keeps the code you wrote inside generated files
(section 4) and backs up everything it overwrites or deletes (section 3).

⚠️ Do not rename this file: its name, `{md_name}`, pairs it with the received folder `{received}` beside it.
Move the two together.

## Quick start

1. In the tree (section 1), find the folder of each host you use: the line of `📁[HostName]`, under `📁[InJAVA]`,
   `📁[InCS]`, `📁[InTS]`, ...
2. At the end of that line, after its link, add a route to a folder that will hold generated code only:
   `[](/D:/MyProject/protocol/)`. Section 2 has the rules.
3. Put `⛔` at the end of every line you do not want deployed: the whole folder of a host you do not use, a host's
   `demo` if you do not need it.
4. Run the command of section 3. The report lists every file and where it went.

## 1. The received files

Every file and folder that arrived is a line. A route at the end of a line applies to that file, or to everything
inside that folder. Not listed, because nothing is routed or merged on them: the runtime library (`lib`, `__`) and
Markdown files (licenses, notes). They travel with the folder they are in - so route the host folder, not only its
`gen`.

What a host folder holds:

| Inside a host folder | What it is                                                                                     | Route it              |
|:---------------------|:-----------------------------------------------------------------------------------------------|:----------------------|
| `gen`                | the generated protocol code, rewritten by every deployment                                     | with the host folder  |
| `lib`                | the AdHoc runtime library                                                                      | with the host folder  |
| `demo`               | a skeleton of the host: every method you have to provide, with injection points (section 4)    | with the host folder, or `⛔` if not needed |
| `sources`            | the list of the generated files, as named on the generation server                             | with the host folder  |

""";

        public const string FOOTER = """

---

## 2. How to write a route

A route is a Markdown link written at the end of a line, after the line's own link:

```text
📁[Broker](/D:/Out/InJAVA/Broker) [](/D:/Projects/kafka/broker/protocol/)
                                  ^^ the filter (empty: every file)
                                    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^ the destination
```

| At the end of a line                          | Meaning                                                                                                     |
|:----------------------------------------------|:------------------------------------------------------------------------------------------------------------|
| `[](/D:/dir/)` - ends with `/` or `\`         | into that folder. On a folder line: its **contents** go into `dir` (the folder itself is not recreated). On a file line: the file goes into `dir` |
| `[](/D:/dir/Name.java)` - no `/` at the end   | exactly that path: the file is copied under that name, the folder is copied as that folder                  |
| `[\.java$](/D:/dir/)`                         | a filter: a regular expression matched against the full path of each received file; the route applies only to the files it matches |
| several routes on one line                    | each one applies; one file can go to several places                                                         |
| a route on a folder                           | inherited by everything inside it, each file keeping its path relative to that folder. A route on a line inside it is added to the inherited one, it does not replace it |
| `⛔`, or an empty `[]()`                       | skip this file - or this folder and everything inside it, routes inherited or its own                       |
| any other text after the link                 | your note: kept when the list is refreshed                                                                  |

Paths: write a Windows path as `/D:/dir/` (the leading `/` is Markdown's) - `D:\dir\` works as well; a path with
spaces goes in angle brackets: `</D:/My Projects/dir/>`.

### ⚠️ A destination folder belongs to the generator

Any file already sitting in a destination folder that this deployment does not write is a leftover: it is listed in
the report, backed up, and **deleted** - in that folder and in every folder below it. So route to folders that hold
generated code and nothing else, never to a project root. Folders whose names start with a dot (`.git`, `.idea`) are
never touched.

The layout that keeps your code safe - a dedicated `protocol` folder beside it:

```text
MyProject/
├─ build file, README, ...   yours
├─ src/                      your code
└─ protocol/                 <- route the host folder here: gen/, lib/, ... - generated only
```

Then add `protocol/gen`, `protocol/lib` and `src` to the source folders of your build. A host routed this way, its
`demo` left out:

```text
📁[Server](/D:/Out/InJAVA/Server) ✅ the whole host [](/D:/Projects/server/protocol/)
  📁[demo](/D:/Out/InJAVA/Server/demo) ⛔
```

What you write between a line's link and its route (`✅ the whole host` above) is a note for you.

---

## 3. Running a deployment

```text
AdHocAgent "{md_path}"
```

What happens:

1. The tree is compared with what actually arrived. Files that are new, and lines whose file is gone, are listed -
   with the path each new file would be written to - and you choose between deploying now and stopping to edit the
   routes first. Lines that still match a received file keep their routes and notes.
2. The steps of section 5 run on the received files.
3. The files are written to their destinations, your code inside them merged in (section 4).
4. The report shows every file with its destination; `⛔` marks a file that was not deployed.
5. Every file that is overwritten or deleted is first copied into a numbered backup folder next to this file
   (`{received}_1`, `{received}_2`, ...), together with `restore.bat`, `restore.ps1` and `restore.sh`, which put
   everything back.

The received folder stays as it is: change the routes and deploy again as often as you like.

---

## 4. Your code inside generated files

Generated files contain injection points: regions whose content survives every regeneration.

```text
//#region > Connection receiving
    your code
//#endregion > ǺÿÿČ.Project.Connection receiving     <- never edit, move or copy this line: its id finds your code
```

(C# uses `#region` / `#endregion`.) Inside a region, the blocks between `//❗<` and `//❗/>` are generated: comment a
whole block out, markers included, to switch it off; uncomment it to switch it on; move it around - but do not edit
inside it, that is rewritten. After an update the agent marks what needs a look with `//todo 🔴`. If a region you
wrote code in is gone from the new code, the agent shows that code and asks before dropping it.

---

## 5. Processing the received files before deployment

Optional steps that run on the received files before anything is copied - formatting, mostly. A step is two fenced
blocks, one right after the other: a `regexp` block that selects files by their full path, then a `shell` or a
`csharp` block. Steps run in the order they appear; Markdown files are never processed.

- `shell`: one command per line - a line that starts with a space or a tab continues the one above. `FILE_PATH` is
  replaced with the path of each selected file.
- `csharp`: a class `Program` with `public static void Main(string[] args)`; `args[0]` is the file. Lines in quotes at
  the top, such as `"System.Text.RegularExpressions"`, add assembly references.
- A path that starts with `/InJAVA/`, `/InCS/`, `/InTS/`, `/InCPP/`, `/InRS/` or `/InGO/` is a path inside the
  received folder.
- To run a command once, before or after the whole deployment, write a link whose text is `before deployment` or
  `after deployment` and whose target is the command, for example `［before deployment](dotnet format "/InCS/MyHost")`.
  Written with `［`, a full-width bracket, as here, it does nothing; with an ordinary `[` it runs. Such links are
  found anywhere in this file.
- To switch a step off, delete it. The steps below call clang-format, prettier, rustfmt and gofmt: delete the ones whose
  tool is not installed, or the deployment stops at them.

### Java, C#, C++: clang-format

```regexp
\.(java|cs|cpp|h)$
```

```shell
clang-format -i -style="{ColumnLimit: 0, IndentWidth: 4, TabWidth: 4, UseTab: Never, BreakBeforeBraces: Allman, IndentCaseLabels: true, AllowShortBlocksOnASingleLine: false, SpacesInLineCommentPrefix: {Minimum: 0, Maximum: 0}}" FILE_PATH
```

Another formatter for the same files: [Artistic Style](https://sourceforge.net/projects/astyle/files/astyle/), for
example `astyle --style=allman --indent-switches --indent-cases --indent-namespaces --pad-oper --pad-comma --suffix=none --quiet FILE_PATH`.

### TypeScript: prettier (install it with `npm install -g prettier`)

```regexp
\.ts$
```

```shell
prettier --write FILE_PATH --tab-width 4 --bracket-spacing false --print-width 999
```

### Rust: rustfmt

```regexp
\.rs$
```

```shell
rustfmt FILE_PATH
```

### Go: gofmt

```regexp
\.go$
```

```shell
gofmt -w FILE_PATH
```

### Every file: no indentation before region markers

```regexp
.*
```

```csharp
"System.Text.RegularExpressions"

using System.IO;
using System.Text;
using System.Text.RegularExpressions;

public class Program
{
    static readonly string pattern = @"^\s+(?=//#region|//#endregion|#region|#endregion|//region|//endregion|// region|// endregion)";

    public static void Main(string[] args)
    {
        var content = File.ReadAllText(args[0], Encoding.UTF8);
        // UTF-8 without a byte order mark: the Unicode standard does not recommend one for UTF-8 files
        File.WriteAllText(args[0], Regex.Replace(content, pattern, "", RegexOptions.Multiline), new UTF8Encoding(false));
    }
}
```

---

## Markdown link syntax, for reference

```text
[a relative path](../../some/dir/filename.ext)
[a path on the same drive](/another/dir/filename.ext)
[a path on another drive](/D:/dir/filename.ext)
[a path with spaces](</C:/Program Files (x86)/dir/>)
```

""";
    }
}
