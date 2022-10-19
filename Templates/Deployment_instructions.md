# This file is a deployment instruction template and self-manual at the same time.

This file has a markdown format, used markdown format for its own purpose, and contains `received source files` regex-replace-based, modification instructions, and deployment commands. 
Its name should be the same, as the protocol description file, with `Deployment.md` at the end `My_Protocol_description_fileDeployment.md`.
AdhocAgent utility will scan the deployment instructions file next into the temporary, received files `destination directory`, and then, next to the `protocol description file`.
If the file does not exist, this template will be extracted into the `destination folder`.

This is a kind of simple continuous integration (CI) and continuous delivery (CD) system.

# Adhoc deployment engine recognizes the following sections in this file:
## regex-replace-based modification commands... 
They are organized in section.   
Each section has header - a regex string to select target files by paths, and body with regex instruction on selected files. 

![image](https://user-images.githubusercontent.com/29354319/169692722-e239ef4f-9a96-415a-a9f2-f81b8c923490.png)

### regexp\/[^x-zQ-Z]\/on_files_path\/.+?dFile\.cpp

```c++
^(【#include <iostream>
int main()】)(?:【{
std::cout << "Hello World!";
return 0;
}】)➤$1{}
```

### regexp\/[^x-zQ-Z]\/on_files_path\/.+?\.java

```C#
^(【public static void main(String[] args)】)(?:【{
System.out.println( "Hello World!");
}】)➤$1{}
```

Each modification command consists of two parts, separated with unicode `➤` symbol:

```c#
regex-select-source_code ➤ regex_replace
```

text in 【】 brackets will be regex-escaped before use.

## ...and deployment section:

To execute previously declared regex commands and deploy result files instructions. Instructions are organized in a tables.

| src                                                            | dst                                                                                                   |
|----------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| [Generated InRS HostA](  Project_name/InRS/Host_A/  )          | [Project in InRS](    ../Path/To/Project_InRS)                                                        |
| [Generated InCS HostB](  Project_name/InCS/Host_B/  )          | [Project in InCS](    ../Path/To/Project_InCS)                                                        |
| [Generated InTS HostA](  Project_name/InTS/Host_A  ) "*.ts"    | [Project in InTS](    ../Path/To/Project_InTS)                                                        |
| [Generated InCPP HostA]( Project_name/InCPP/Host_A/ )          | [Project in InCPP](   ../Path/To/Project_InCPP)                                                       |
| [Generated InCPP HostB]( Project_name/InCPP/Host_B )           | [Project in InCPP](   ../Path/To/Project_InCPP_B)                                                     |
| [Generated InJAVA HostB](Project_name/InJAVA/Host_B/) "*.java" | [Project 1 in InJAVA](../Path/To/Project_InJAVA)<br>[Project 2 in InJAVA](../Path/To/Project2_InJAVA) |

For reference: file / folder link syntax examples:

[A relative link](../../some/dir/filename.ext)  
[Link to file in another dir on same drive](/another/dir/filename.ext)  
[Link to file in another dir on a different drive](/D:/dir/filename.ext)  
[If you have spaces in the filename](</C:/Program Files (x86)>)

Generated files from AdHoc server are receiving into folder, with name same as project, inside agent destination folder. 
Paths in the `src` column must be referred from the receiving folder(with project name). After folder path, may go additional files filter: [wildcard specifier](https://docs.microsoft.com/en-us/dotnet/api/System.IO.Directory.GetFiles?view=netcore-3.1) `*.*`,`*.java`...  in quarters  

>  a combination of literal and wildcard characters, but it doesn't support regular
>  expressions. The following wildcard specifiers are permitted.
>  
>  | **Wildcard specifier** | **Matches**                               |
>  |------------------------|-------------------------------------------|
>  | \* (asterisk)          | Zero or more characters in that position. |
>  | ? (question mark)      | Exactly one character in that position.   |
>  
>  Characters other than the wildcard are literal characters. For example, the
>  searchPattern string "\*t" searches for all names in path ending with the letter
>  "t". The searchPattern string "s\*" searches for all names in path beginning
>  with the letter "s".


The `dst` column may contains several paths.  
On each entity in `src` column, AdHoc deployer execute respected regex source modification commands and result copy to the paths in `dst` column.  
Without deployment section files will be modified in place.

