# AdHoc protocol - AI reference (AI.md)

Machine-oriented version of [MANUAL.md](MANUAL.md): same facts, same chapter order, no narrative or images. For AI models that write
AdHoc descriptions, run AdHocAgent, deploy generated code or reason about generated code and the runtime. Humans: read MANUAL.md.

## How to read this file
- Do NOT read the whole file. Read this header (up to the line `END OF HEADER`), pick sections from TASK ROUTER or TOC, then read
  only their lines (`Read` with offset/limit = the range after `|`). The ranges are exact for this version of the file.
- If the ranges look off (file edited), locate a section by its §ID: `grep -n "^#\+ §7.6 " AI.md`.
- Find a term: `grep -n "KW:.*<term>" AI.md` (every heading is followed by a `KW:` line of exact identifiers and concepts), then read
  the section above that line. Plain `grep -n "<identifier>"` finds every mention.
- §IDs: `§N` = MANUAL chapter N, `§N.M` = its section, `§N.M.K` = subsection; same order and titles as MANUAL.md headings.
  `→§X.Y` = see that section. Subsections (`### §N.M.K`) are not in the TOC: they lie inside their section's range.
- Bullet tags (grep inside a section): `DECL:` how to declare · `GEN:` what the generator produces · `RT:` runtime behavior ·
  `WIRE:` bytes on the wire · `RULE:` constraint enforced by AdHocAgent (with its error) · `DEFAULT:` defaults, limits ·
  `PITFALL:` traps · `CLI:` AdHocAgent commands, env vars, exit codes, files · `NOTE:` other facts.
- Code in examples is valid as written; keep identifiers exactly. A description is C# compiled against `Meta.cs` (namespace `org.unirail.Meta`).

## Task router
| Task | Read |
|:--|:--|
| understand the system, terms | §1.2 model, §1.6 glossary |
| install, UUID, first project, first generation, first deploy | §2 |
| skeleton of a description: project interface, C# construct → AdHoc entity, struct roles, names, comments | §3.2, §3.4, §3.5, §3.6, §3.7 |
| declare a pack (where, nested, empty, docs) | §4 |
| choose a field type; optional; string; arrays; Map/Set; limits `[D]` | §5.1, §5.2, §5.7-§5.15 |
| 128-bit integers, UUID, exact decimals | §5.3, §5.4, §5.5 |
| pack/enum as field, Binary, TYPEDEF, alternatives, time types | §5.16-§5.20 |
| constants, `[ValueFor]`, enums, flags | §6 |
| wire size: `[MinMax]`, bit fields, varint `[A]`/`[V]`/`[X]`, Value Packs, pack layout | §7 |
| compose packs: XML directives, inheritance, `FieldsInjectInto`, `HeaderFor`, `Modify<Header>` | §8 |
| select a group of packs (`_<>`, `X<>`, `@`, filters, templates) | §9 |
| declare a host, its languages, `+`/`-` implementation (generated objects vs your structures) | §10 |
| connect two hosts, timeouts, serial-line `Framing` | §11 |
| conversation rules: actors, states, branches, routing attributes, dispatch | §12 |
| RPC, function groups, full actors, lifecycle, threads, `[Grouped]`, broadcast | §13 |
| survive reconnects `[Resumable]`, guaranteed delivery, UDP | §14 |
| large/endless data: `Stream`, `File`, `[S(N)]` | §15 |
| compression/encryption: `[Zstd]`, `[ChaCha20]`, custom stages, flows | §16 |
| store/relay payload without parsing it: trims | §17 |
| tunnels through relay hosts, `VirtuallyConnects` | §18 |
| many services/versions on one port | §19 |
| numbers tables at top of the file, pack ids | §20 |
| change a protocol safely, generations, `.cs~` impact report | §21 |
| import/extend another description, `Modify<>` | §22 |
| attribute catalog, custom attributes | §23 |
| AdHocAgent tasks (`.cs`, `.cs!`, `.cs~`, `.md`), toml, env vars, exit codes, CI | §24 |
| deployment instructions file, routes, steps, hooks, smart merge, injection points, backups | §25 |
| who may use a pack/actor/RPC: `[Role]` enum, `[Can]`, `[Role(…)]`, `[Modify<F>]`, rights matrix, `may_`/`Deny` | §26 |
| network: TCP/WebSocket/TLS/Noise, chooseChain, Cloudflare and direct, trust, Chain.Pack | §27 |

## TOC (§ID title | first-last line)
§1 How AdHoc works | 339-565
  §1.1 The pipeline: from a description to running code | 346-356
  §1.2 The model: from the project down to the bytes | 357-414
  §1.3 The runtime model: one buffer per direction | 415-493
  §1.4 What AdHoc generates for each host | 494-510
  §1.5 Target languages of AdHoc | 511-529
  §1.6 Glossary of AdHoc terms | 530-565
§2 Getting started | 566-820
  §2.1 What you need for AdHoc | 570-580
  §2.2 Step 1: install .NET and AdHocAgent | 581-593
  §2.3 Step 2: get your personal UUID | 594-605
  §2.4 Step 3: create the description project | 606-639
  §2.5 Step 4: get the template | 640-689
  §2.6 Step 5: compose your protocol | 690-741
  §2.7 Step 6: generate the first code | 742-780
  §2.8 What comes back from the generator | 781-800
  §2.9 Step 7: deploy the first code into your project | 801-816
  §2.10 Where to go next with AdHoc | 817-820
§3 The description file | 821-1076
  §3.1 A description is compiled C# | 827-833
  §3.2 The project interface | 834-897
  §3.3 Several projects in one description | 898-935
  §3.4 From C# constructs to AdHoc entities | 936-1012
  §3.5 The four roles of a struct | 1013-1030
  §3.6 Naming rules for description entities | 1031-1046
  §3.7 Comments in a description | 1047-1067
  §3.8 The numbers tables at the top of the file | 1068-1076
§4 Packs | 1077-1323
  §4.1 What a pack contains | 1083-1095
  §4.2 Where a pack may be declared | 1096-1123
  §4.3 Nested packs | 1124-1163
  §4.4 Which classes are packs | 1164-1183
  §4.5 Empty packs | 1184-1236
  §4.6 Documentation in the generated code | 1237-1323
§5 Fields and types | 1324-2103
  §5.1 The field types at a glance | 1329-1350
  §5.2 Numeric and other primitive types | 1351-1372
  §5.3 Int128 and UInt128 | 1373-1415
  §5.4 UUID | 1416-1443
  §5.5 Decimal | 1444-1494
  §5.6 longJS and ulongJS | 1495-1517
  §5.7 Optional fields | 1518-1549
  §5.8 Strings | 1550-1570
  §5.9 Arrays: constant, fixed and dynamic length | 1571-1613
  §5.10 The `[D]` attribute | 1614-1635
  §5.11 Collection limits: `_DefaultMaxLengthOf` | 1636-1653
  §5.12 Map and Set | 1654-1699
  §5.13 `[Key:]` and `[Val:]` attribute targets | 1700-1738
  §5.14 Multidimensional arrays | 1739-1767
  §5.15 Arrays of collections | 1768-1785
  §5.16 Packs and enums as field types | 1786-1838
  §5.17 Binary | 1839-1881
  §5.18 TYPEDEF | 1882-1922
  §5.19 Alternatives: exactly one of several | 1923-1945
  §5.20 Time types | 1946-2098
  §5.21 Stream and File fields | 2099-2103
§6 Constants and enums | 2104-2337
  §6.1 Declaring a constant: const and static | 2111-2153
  §6.2 Computed constants: [ValueFor] | 2154-2208
  §6.3 Constants containers | 2209-2238
  §6.4 Enums and flags enums | 2239-2291
  §6.5 Where constants and enums end up | 2292-2337
§7 How values become bytes | 2338-2844
  §7.1 Value layers: exT, inT, ioT | 2344-2362
  §7.2 Narrowing a primitive field with [MinMax] | 2363-2401
  §7.3 Bit fields: small values share the pack's bits | 2402-2434
  §7.4 Enums on the wire | 2435-2449
  §7.5 Absent values on the wire | 2450-2469
  §7.6 Varint: declaring where values cluster | 2470-2598
  §7.7 Range constants in the generated code | 2599-2626
  §7.8 Strings and lengths on the wire | 2627-2652
  §7.9 Value Packs | 2653-2793
  §7.10 The layout of a pack on the wire | 2794-2844
§8 Building packs from packs | 2845-3267
  §8.1 How a pack's field list is assembled | 2852-2868
  §8.2 Importing and removing fields with XML directives | 2869-2946
  §8.3 Inheriting fields: C# inheritance and `_<(...)>` | 2947-2987
  §8.4 The final field order of a composed pack | 2988-3013
  §8.5 Field injection: `FieldsInjectInto` | 3014-3045
  §8.6 Pack headers: `HeaderFor` | 3046-3220
  §8.7 Modifying a header: `Modify<Header>` | 3221-3259
  §8.8 Related: composition and headers | 3260-3267
§9 Pack Sets | 3268-3549
  §9.1 The parts of a Pack Set expression | 3285-3342
  §9.2 In-place Pack Sets: `_<>`, tuples and `X<>` | 3343-3352
  §9.3 Named Pack Sets | 3353-3370
  §9.4 Project, host and pack scopes, and `@` | 3371-3393
  §9.5 Filtering a set by name and by documentation | 3394-3448
  §9.6 Filter templates: `F<SCOPE>` | 3449-3488
  §9.7 Combining sets: union, intersection, difference | 3489-3532
  §9.8 Rules for Pack Sets at a glance | 3533-3545
  §9.9 Related: Pack Sets | 3546-3549
§10 Hosts, languages, generated code | 3550-3961
  §10.1 Declaring a host | 3556-3605
  §10.2 Language markers | 3606-3650
  §10.3 Implementation modifiers: who holds the data | 3651-3745
  §10.4 Choosing `+` or `-`: generated objects or your own structures | 3746-3758
  §10.5 Implementation management: host, pack and field rules | 3759-3904
  §10.6 Modifying an imported host | 3905-3947
  §10.7 A host as a named Pack Set | 3948-3951
  §10.8 What the generator produces for a host | 3952-3961
§11 Connections | 3962-4239
  §11.1 What a connection is | 3969-3973
  §11.2 Declaring a connection | 3974-4022
  §11.3 One connection per pair of hosts | 4023-4027
  §11.4 Connecting two hosts of one type | 4028-4067
  §11.5 Composing connections and SwapHosts | 4068-4131
  §11.6 The connection at runtime | 4132-4157
  §11.7 Connection timeouts: TransmitTimeout and ReceiveTimeout | 4158-4208
  §11.8 Serial links and fast recovery: Framing | 4209-4239
§12 Actors, states, branches | 4240-4825
  §12.1 Actors: independent conversations on one connection | 4247-4267
  §12.2 Actor0, the connection's own actor | 4268-4296
  §12.3 States: the phases of a conversation | 4297-4321
  §12.4 Branches and the routing attributes | 4322-4367
  §12.5 Choosing the packs of a branch | 4368-4483
  §12.6 Pack-id dispatch and uniqueness | 4484-4560
  §12.7 Chain or function group: the two shapes of an actor | 4561-4600
  §12.8 Multi-path transitions and loops | 4601-4632
  §12.9 Grafting a flow from another actor | 4633-4663
  §12.10 What the generator produces for actors and states | 4664-4703
  §12.11 Worked state machine examples | 4704-4760
  §12.12 A real state machine: AdHoc's Communication connection | 4761-4800
  §12.13 Actor, state and branch rules at a glance | 4801-4825
§13 Actor kinds and the actor at runtime | 4826-5362
  §13.1 Four kinds of actor | 4830-4844
  §13.2 Function-group actor | 4845-4901
  §13.3 RPC methods | 4902-5057
  §13.4 Full-featured actor | 5058-5115
  §13.5 Nesting actors | 5116-5145
  §13.6 Actor lifecycle | 5146-5212
  §13.7 Your code lives in the actor | 5213-5244
  §13.8 The two loops: threads and actor handlers | 5245-5253
  §13.9 Actors of many connections: [Grouped] | 5254-5287
  §13.10 One pack to many connections | 5288-5348
  §13.11 Parser checks for actor kinds | 5349-5362
§14 Sessions, guaranteed delivery, UDP | 5363-5793
  §14.1 What a session is | 5367-5378
  §14.2 Keeping a session across a lost connection | 5379-5480
  §14.3 What happens to a session when its connection is lost | 5481-5514
  §14.4 Identity and reclaim | 5515-5606
  §14.5 Guaranteed delivery | 5607-5702
  §14.6 UDP connections | 5703-5769
  §14.7 Session rules at a glance | 5770-5793
§15 Streams | 5794-6121
  §15.1 What streaming adds to the pull-based core | 5801-5809
  §15.2 The streaming model at a glance | 5810-5834
  §15.3 Raw conduits - `Stream` and `File` | 5835-5967
  §15.4 The size cap of a conduit - `[S(N)]` | 5968-5999
  §15.5 Named conduit packs | 6000-6048
  §15.6 Binary, File or Stream - choosing by the shape of the bytes | 6049-6066
  §15.7 Worked example - delivering an archive of any size | 6067-6090
  §15.8 Streaming typed packs | 6091-6108
  §15.9 Streaming rules at a glance | 6109-6121
§16 Transform chains | 6122-6485
  §16.1 Stages and dataflow order | 6129-6195
  §16.2 Where a chain may sit | 6196-6231
  §16.3 Composing a conduit pack's chain with the field's | 6232-6260
  §16.4 Roles and the rules of a chain | 6261-6289
  §16.5 Stage parameters - design-time or runtime-injected | 6290-6313
  §16.6 Flows - a chain declared once | 6314-6329
  §16.7 Built-in stages - Zstd and ChaCha20 | 6330-6358
  §16.8 Short packs go uncompressed | 6359-6374
  §16.9 Keys of a cipher | 6375-6405
  §16.10 Custom transform stages | 6406-6419
  §16.11 Chains on a connection | 6420-6470
  §16.12 Transform-chain rules at a glance | 6471-6485
§17 Trims | 6486-6907
  §17.1 Why a stored payload needs a trim | 6499-6507
  §17.2 Naming the leg: endpoints | 6508-6541
  §17.3 The three trims and who holds what | 6542-6565
  §17.4 Writing a trim on a field or on a pack | 6566-6605
  §17.5 What the generator produces on a cut leg | 6606-6641
  §17.6 Trimmed strings | 6642-6666
  §17.7 Cutting a chain at a chosen depth | 6667-6699
  §17.8 Store and replay with trims | 6700-6800
  §17.9 Trims on named Stream packs | 6801-6815
  §17.10 Trims inside flows | 6816-6833
  §17.11 Trims across imported projects | 6834-6861
  §17.12 What the agent reports about trims | 6862-6877
  §17.13 Trim pitfalls | 6878-6885
  §17.14 Trim rules at a glance | 6886-6907
§18 Virtual connections and relays | 6908-7274
  §18.1 Declaring a virtual connection | 6916-6981
  §18.2 Tunnel-only and structured virtual connections | 6982-7003
  §18.3 How the agent finds the route | 7004-7031
  §18.4 MaxTunnels and MaxStream_KiloBytes | 7032-7055
  §18.5 The tunnel on the wire | 7056-7071
  §18.6 What the generator produces for a tunnel | 7072-7200
  §18.7 Relay: the runtime primitive on a relay host | 7201-7219
  §18.8 Closing a tunnel | 7220-7231
  §18.9 Transform chains over a tunnel | 7232-7244
  §18.10 Choosing what the middle tier does | 7245-7260
  §18.11 Virtual connection rules at a glance | 7261-7274
§19 Multiplexing | 7275-7516
  §19.1 Declaring a multiplexer | 7281-7320
  §19.2 The first byte of a multiplexed socket | 7321-7338
  §19.3 Declared once, chosen at deployment | 7339-7350
  §19.4 Several kinds of peer, several generations behind one port | 7351-7375
  §19.5 The life of a multiplexed socket | 7376-7384
  §19.6 What the generator produces for a multiplexer | 7385-7417
  §19.7 Wiring a multiplexed port | 7418-7472
  §19.8 Relayed connections in a multiplexer | 7473-7496
  §19.9 What the parser checks for a multiplexer | 7497-7511
  §19.10 Multiplexers and tunnels | 7512-7516
§20 Numbers tables | 7517-7861
  §20.1 Identity in a table, position in a pack | 7522-7541
  §20.2 Where the numbers tables live | 7542-7573
  §20.3 The packs table | 7574-7632
  §20.4 The project table | 7633-7693
  §20.5 Branch marks and state marks | 7694-7706
  §20.6 Keeping the numbers right | 7707-7732
  §20.7 From packs to wire ids: the workflow | 7733-7852
  §20.8 Pitfalls of the numbers tables | 7853-7861
§21 Evolving a protocol | 7862-8095
  §21.1 How packs and peers are told apart on the wire | 7869-7877
  §21.2 What changes the wire and what does not | 7878-7909
  §21.3 A new generation is a new host | 7910-7965
  §21.4 The `.cs~` impact report | 7966-8095
§22 Reusing descriptions | 8096-8475
  §22.1 Extending a project | 8108-8264
  §22.2 Importing single declarations with `_<>` | 8265-8284
  §22.3 Modifying imported declarations: `Modify<>` | 8285-8463
  §22.4 Pitfalls of reuse | 8464-8475
§23 Attributes | 8476-8809
  §23.1 Where attributes attach | 8483-8503
  §23.2 Built-in attribute catalog | 8504-8612
  §23.3 Other Meta.cs declarations | 8613-8638
  §23.4 Custom attributes | 8639-8785
  §23.5 How the agent tells built-in from custom | 8786-8809
§24 AdHocAgent | 8810-9091
  §24.1 How AdHocAgent chooses the task | 8817-8832
  §24.2 The `.cs` task: generating code | 8833-8859
  §24.3 What a `.cs` run does, step by step | 8860-8918
  §24.4 Pitfalls of the `.cs` task | 8919-8927
  §24.5 The `.md` task: redeploying received code | 8928-8938
  §24.6 The `.cs~` task: which hosts a change reaches | 8939-8946
  §24.7 AdHocAgent without arguments | 8947-8950
  §24.8 AdHocAgent.toml | 8951-8968
  §24.9 UUID | 8969-8998
  §24.10 Open source | 8999-9018
  §24.11 Environment variables of AdHocAgent | 9019-9031
  §24.12 Exit codes of AdHocAgent | 9032-9054
  §24.13 Running AdHocAgent in a pipeline | 9055-9076
  §24.14 Descriptions from other schema languages | 9077-9080
  §24.15 Third-party components of AdHocAgent | 9081-9091
§25 Deployment and smart merge | 9092-9467
  §25.1 The deployment workflow | 9098-9104
  §25.2 The deployment instructions file | 9105-9168
  §25.3 Routes: where received files go | 9169-9228
  §25.4 Processing steps before deployment | 9229-9295
  §25.5 Lifecycle hooks | 9296-9312
  §25.6 One deployment run, step by step | 9313-9343
  §25.7 Smart merge | 9344-9348
  §25.8 Injection points | 9349-9421
  §25.9 Automated backups and restoration | 9422-9446
  §25.10 Pitfalls of deployment | 9447-9467
§26 Roles and rights | 9468-9651
  §26.1 Declaring a family of roles | 9472-9498
  §26.2 What a role may use: `[Can]` | 9499-9510
  §26.3 Rules written on packs, Pack Sets, actors and RPC methods: `[Role(…)]` | 9511-9521
  §26.4 Who carries the roles | 9522-9527
  §26.5 How the rules combine | 9528-9534
  §26.6 Roles in a description that extends another | 9535-9563
  §26.7 The rights matrix in the numbers tables | 9564-9579
  §26.8 What the generator produces | 9580-9602
  §26.9 Overriding a rule on a host | 9603-9613
  §26.10 At runtime | 9614-9618
  §26.11 Rules for roles at a glance | 9619-9643
  §26.12 Pitfalls of roles | 9644-9651
§27 Network configurations | 9652-9998
  §27.1 What a connection can run over | 9657-9669
  §27.2 Choosing a configuration | 9670-9687
  §27.3 The chain | 9688-9745
  §27.4 Choosing per connection, by address | 9746-9770
  §27.5 Through Cloudflare and directly | 9771-9904
  §27.6 Keeping connections through reconnects | 9905-9909
  §27.7 Multiplexing under a chain | 9910-9925
  §27.8 Which certificates a client trusts | 9926-9943
  §27.9 A pack sealed on its own | 9944-9955
  §27.10 Buffers and what a chain costs | 9956-9962
  §27.11 How long a handshake may take | 9963-9978
  §27.12 Failures | 9979-9986
  §27.13 Differences between runtimes | 9987-9998

END OF HEADER
# §1 How AdHoc works
KW: pipeline, model, pack, description, AdHocAgent, generator, received folder, deployment, runtime
SUMMARY: Pipeline description → AdHocAgent → generator → code per host → deployment → runtime; model project → host → connection → actor → state → branch → pack → field → bytes; runtime buffers, threads, generated vs user code.
- AdHoc = multi-language code generator for binary protocols. One C# source file describes data exchanged, programs, conversations. Generated per program and per language: code writing data as bytes, carrying bytes over connection under declared rules, reading them back.
- `pack` = unit of data written/read: structured object with typed fields. Not tied to network: same generated code writes/reads packs in a storage format of your own.
- NOTE: three-minute visual overview (system model, conversation rules, streaming architecture) in README.md.

## §1.1 The pipeline: from a description to running code
KW: description, Meta.cs, AdHocAgent, Roslyn, numbers tables, UUID, generator, !, received folder, InCS, InJAVA, InTS, smart merge, injection points
1. Description: plain C# file in a .NET project; IDE compiles/checks it like any C#. Uses AdHoc vocabulary (`Host`, `Connects<L, R>`, branch attributes, field attributes) declared in `Meta.cs`. →§3
2. CLI: AdHocAgent (on your machine) compiles description files with Roslyn, checks every AdHoc rule. First error stops run with message; nothing leaves machine until description passes. Keeps numbers tables at top of file up to date (identities of packs, hosts, connections, actors, states) → rewrites description file in place. →§24, →§20
3. Generator = cloud service; generation never runs locally. AdHocAgent signs in with personal UUID, uploads checked project. Server generates code of every host in each of its languages. `!` after description file name → also tests code, as far as server resources allow.
4. Received folder: generated files stream straight to disk into `<output folder>/<description name>/` (description file name without `.cs`): folder per language (`InCS`, `InJAVA`, `InTS`), in each a folder per host. →§2
5. Deployment + smart merge: AdHocAgent copies received files into your projects as a Markdown *deployment instructions file* directs. Generated files contain `injection points` = marked regions holding your code; `smart merge` carries your code into new version of file on every deployment. Everything a deployment overwrites/deletes is backed up first, with restore scripts. →§25
6. Runtime: each host = your program built from generated code + AdHoc runtime library + your code; hosts exchange packs over connections. →§1.3
- NOTE: rerun steps 2–5 after every description change. To see in advance which hosts a change reaches: AdHocAgent compares two description versions → impact report →§21.
- NOTE (diagram legend): yours = description, projects; AdHocAgent; AdHoc service = generator; generated = received code.

## §1.2 The model: from the project down to the bytes
KW: project, host, connection, actor, state, branch, pack, field, bytes, transmittable, wire id, Connects<L,R>, L____________, ____________R
- One description holds data structures (packs, fields) and whole network topology (hosts, connections). Layers grouped: topology (project, host, connection), conversation (actor, state, branch), data (pack, field, bytes).

| Layer | What | Declared as | → |
|:--|:--|:--|:--|
| Project | whole description: one system + protocol of all parts | top-level `interface` in a namespace | §3 |
| Host | one program, with languages its code is generated in | `struct Name : Host`, directly in project | §10 |
| Connection | link between left host and right host + everything said over it | `interface Name : Connects<Left, Right>`, directly in project | §11 |
| Actor | one independent conversation on a connection, own state machine; many may run at once | states written in connection body (connection's own actor), or `interface Name : Actor` | §12, §13 |
| State | one step of conversation: which packs each side may send now | empty `struct` carrying branch attributes | §12 |
| Branch | in a state: packs one side may send + state that sending leads to | attribute on state: `[L____________<Next, Packs>]`, `[____________r<Packs>]`, ... | §12 |
| Pack | unit sent/received | `class` in project, host or another pack | §4 |
| Field | typed value inside pack | instance field of class | §5 |
| Bytes | wire form: id, then fields, each at place fixed by kind + declaration order | generated | §7 |

- Ladder is not only nesting: packs = data, declared in project or host, outside any connection (host or project declares packs). Branch *refers to* (selects) packs.
- `transmittable` pack = pack some branch sends → gets `wire id` = number preceding it on wire. Pack no branch sends is never sent alone; can still be field type of another pack.
- Complete example with every layer:
```csharp
using org.unirail.Meta;
namespace com.my.company
{
    public interface Telemetry                         // project
    {
        /// <see cref="InCS"/>
        struct Sensor : Host                           // host
        {
            public class Reading                       // pack, declared in the Sensor host
            {
                [MinMax(-40, 125)] short celsius;      // field
                ulong                    time;         // field
            }
        }
        /// <see cref="InJAVA"/>
        /// <see cref="InTS"/>
        struct Collector : Host                        // host
        {
            public class Ack { }                       // pack without fields
        }
        interface Link : Connects<Sensor, Collector>   // connection: Sensor is the left host, Collector the right
        {
            [L____________<Waiting, Sensor.Reading>]   // branch: the left host sends Reading and moves to Waiting
            struct Idle { }                            // state: the first declared state is the initial one
            [____________R<Idle, Collector.Ack>]       // branch: the right host sends Ack and moves back to Idle
            struct Waiting { }
        }
    }
}
```
- DECL: host doc comment names its languages (Sensor → C#; Collector → Java + TypeScript).
- NOTE: each pack declared in host that sends it. `Ack` has no fields: empty pack carries information by arriving.
- DECL: first type argument of `Connects` = left host, second = right host.
- DECL: states written directly in connection body belong to connection's own actor; first declared state (`Idle`) = initial state.
- DECL: branch letter beside underscores names side: `L` left, `R` right. Capital letter → sending moves conversation to state named by first type argument; small letter → state stays. Both hosts get the one state machine: generated code of each side knows what it may send/receive in every state.
- WIRE: `Reading` = wire id + 2 fields. Field carries no number/tag: known by its place. Generator writes fields in groups by kind (fixed-width numbers, bit fields, varints, nullable and reference fields, `Stream`/`File` fields), declaration order kept inside each group → swapping two fields of one kind changes wire format →§7. `celsius` range -40..125 = 166 values → 1 byte.
- CLI: first AdHocAgent run writes numbers tables above `interface Telemetry` and gives `Reading`, `Ack` wire ids →§20.

## §1.3 The runtime model: one buffer per direction
KW: transport, TCP, WebSocket, UDP, send buffer, receive buffer, bufferSize, transmitter, receiver
- RT: host opens connections through a runtime-library transport: TCP, WebSocket or UDP. Each connection has 2 buffers (send, receive), size chosen by your code when creating transport. Every byte of every pack passes through them.
- RT: runtime never allocates a pack-sized buffer in either direction — except when you ask it to capture a received pack for broadcast →§13.10.
- RT: layers of one host: your code (calls `send`, writes code into injection points inside generated actor/state classes) → generated code (connection, actors, states, sending queue, transmitter, receiver) → runtime library (transport with one send + one receive buffer of `bufferSize` bytes, socket). Large pack goes on wire cut into buffer-sized chunks.

### §1.3.1 Sending: the transport pulls the bytes
KW: send, sending queue, transmitter, true, false, queue full, pull
- RT: `send` does not serialize. Puts pack into connection's sending queue and returns `true` when queued, `false` when connection closed or closing, or queue full.
- RT: bytes produced later on demand: when socket can take more, transport asks connection's `transmitter` to fill send buffer. Transmitter takes head pack of queue, writes pack id + fields straight into buffer until full. Transport sends what was written; next fill continues exactly where previous stopped, mid-field if needed.
- RT: while sending, runtime holds pack itself + transmitter's small state machine (where writing stopped). Pack never exists as one contiguous byte array.

### §1.3.2 Receiving: bytes are consumed where they land
KW: receiver, receive buffer, handler, parser state, backpressure
- RT: socket fills receive buffer; transport hands bytes to connection's `receiver`. Receiver reads pack id, picks generated handler of that pack; handler consumes bytes field by field directly from buffer. Value cut by end of buffer waits in receiver's small parser state, completed from next buffer.
- RT: receiver never waits for whole pack before decoding; receive buffer reused for next read as soon as receiver returns.
- RT: destination signals it cannot take more (e.g. full stream sink) → transport stops reading socket until it can. Unread data waits in network, not memory.

### §1.3.3 Memory on the serialization path
KW: memory, Stream, abstract pack, concrete, resumable_megabytes, journal, GC, ArrayPool, broadcast
- RT: resident memory of a connection on path pack↔socket = 1 send buffer + 1 receive buffer + small serializer/parser state, regardless of pack length. Also for `Stream` field of unbounded size (1 GB blob, endless live feed, video relayed through middle host) →§15.
- NOTE: pack can be larger than memory of either host if its bulk travels in `Stream` fields, or in abstract pack whose values your code produces/consumes piece by piece.
- Memory outside that path (yours to count):
  - concrete pack (generated as class) = object holding field values;
  - connection with guaranteed delivery keeps journal of sent packs until peer confirms, up to `resumable_megabytes` (DEFAULT 100 MB) →§14;
  - compression/cipher stages keep own contexts →§16;
  - received pack captured for broadcast held as one array until slowest subscriber has sent it →§13.10.
- RT: no pack-sized object allocated, socket buffers reused (C# TCP transport rents them from shared array pool) → GC has no large per-message allocations.

### §1.3.4 The socket buffer size
KW: bufferSize, 256, 1024, Network.TCP.Server, Network.TCP.WebSocket.Client, backlog
- DEFAULT: buffer size = argument of transport constructor; C# runtime default 1024 bytes. One value sets both buffers of every connection the transport opens.
- RULE: smallest buffer = 256 bytes. PITFALL: transports do not check it — do not pass less. WHY: nullable `Int128` collection writes null bits with the 8 values they describe, 1 + 8 × 16 = 129 bytes in one fill.
- RT: every pack, any size, passes through buffer in portions (send fills as much as fits, continues next fill; receive decodes what arrived, continues next read). No array holding whole serialized pack allocated before send or after receive.
- GEN: generated C# demo transports:
```csharp
// listening side: 512-byte send and receive buffers per accepted connection
var server = new Network.TCP.Server("Server name",
                                    CommunicationChannel.Connection.NewWebSocketConnection,
                                    Network.TCP.onFailurePrintConsole,
                                    512,                                // bufferSize
                                    100,                                // backlog of pending connections
                                    null,                               // default listening socket
                                    IPEndPoint.Parse("127.0.0.1:1234"));
// dialing side: 1024-byte buffers
var client = new Network.TCP.WebSocket.Client<CommunicationChannel.Connection>("My Client Name",
                                    connection => new CommunicationChannel.Connection(connection),
                                    Network.TCP.onFailurePrintConsole,
                                    1024);                              // bufferSize
```
- NOTE: larger buffer → more bytes per system call, fewer kernel calls; does not change what fits in a pack. Small (down to 128) suits low-memory or many-connection hosts; 1 GB pack still goes through. Usual range 1–8 KB. AdHoc's own services use 1024 bytes (AdHocAgent's connection to generator; listener of AdHoc's monitoring server).

### §1.3.5 Threads: the receive loop and the transmit loop
KW: receive loop, transmit loop, threads, locks, blocking handler, fork-join pool, timeouts
- RT: each direction of a connection = sequence of events. Receive side: arriving packs + receive timeouts. Transmit side: outgoing packs + transmit timeouts. Within one direction events handled one at a time, each to completion → handlers on same side never race; state they own needs no locks.
- RT: two directions may run on different threads simultaneously; state shared between a receive handler and a transmit handler = the one place to coordinate.
- PITFALL: handlers run on these loops; blocking handler stalls its whole direction. Loop threads shared by connections of a transport (Java runtime: all on one thread pool, common fork-join pool unless you pass your own) → blocked handler holds back other connections. Hand long work to another thread, send result as new pack. Full threading model →§13 (→§13.8).

### §1.3.6 Generated code and your code at runtime
KW: gen, lib, injection point, #region>, #endregion>, smart merge, OnReceivedEvent

| Part | Holds | Written by |
|:--|:--|:--|
| Generated code (`gen`) | packs + serializers/parsers, connection classes with transmitter/receiver, actors with states + pack dispatch, constants | generator; replaced by every generation |
| Runtime library (`lib`) | transports (TCP, WebSocket, UDP), buffers, collections, compression, ciphers | AdHoc; copied as is |
| Your code in injection points | inside generated files, marked regions: what received pack means to this actor, what entering a state starts, what a timeout does | you; carried over by smart merge |
| Your code around generated code | creating transports, connecting, sending packs, own data structures | you |

- GEN: injection point = region in generated file that belongs to you. From generated C# of AdHoc's own protocol (agent receives server's `Info`):
```csharp
case Agent.AdHocProtocol.Server_.Info.__id_:
    actor.OnNewState(conn, CLOSE_.ONE);     // generated: the state machine has moved on
#region> Info OnReceived Event
    // your code: what receiving Info means here
#endregion> ĀĄÿĀĄ.OnReceivedEvent
    // ... generated code continues
```
- RT: generated dispatch has already moved actor to next state before your region. Lines between `#region>` and `#endregion>` are yours; characters after `#endregion>` identify the region → how deployment finds your code in next file version. →§25 (→§25.8)

## §1.4 What AdHoc generates for each host
KW: gen/, lib/, LICENSE-GRANT.md, demo, demo.cs, demo.ts, demo.py, Project.csproj, package.json, tsconfig.json, concrete, abstract
- GEN: per host, a folder per declared language; e.g. `InCS/Sensor/`, `InJAVA/Collector/`, `InTS/Collector/` for Telemetry (§1.2).

| In host folder | What | Deployed |
|:--|:--|:--|
| `gen/` | generated protocol code of host | always, with host folder |
| `lib/` | AdHoc runtime library in host's language | always |
| `LICENSE-GRANT.md` | terms generated code + library come under | always |
| `demo` | host skeleton: methods you must provide, transport set up, packs sent — `demo.cs` (C#), `demo.ts` (TS), `demo/` folder (Java), `demo.py` (Python), `demo.dart` (Dart), `demo.rs` (Rust), `main.go` (Go), `demo.cpp` (C++), `demo.c` (C), `app/demo.swift` (Swift) | only when asked |
| project files | build host folder as own project: `Project.csproj`, `package.json`, `tsconfig.json` | only when asked |

- NOTE: demo shows how to use generated code: compiles, need not work to the end; call that cannot run in demo shown all the same. Virtual connection: demo of host that dials the carrier sends on the virtual connection through a tunnel of the carrier (one tunnel: member `connection.<V>`; Python `send_ViewerToServer( connection.ViewerToServer )`), in the handler that gets the carrier's connection once up; demo of relay host shows example route of region `Tunneling` (Python: route set with `route( handler )`).
- NOTE: meaning of "deployed" and how to ask for optional items →§25.
- GEN: per host+language a pack has one of 2 forms. `concrete` = class holding field values: create object, fill, send. `abstract` = interface your own type implements: generated code reads values from your object field by field while serializing, writes received values into your object while parsing; no intermediate object. Chosen per host, language, pack →§10.
- NOTE: AdHocAgent's generated C# uses both: `Login` pack (sent to server) = class; `Project` pack (carries parsed description model to generator) = interface implemented by agent's own model classes.

## §1.5 Target languages of AdHoc
KW: language marker, InCPP, InRS, InCS, InJAVA, InGO, InTS, InSwift, InC, InDart, InPy
- DECL: host names languages with language markers in its doc comment; code generated in every marked language.

| Marker | Language |
|:--|:--|
| `InCPP` | C++ |
| `InRS` | Rust |
| `InCS` | C# |
| `InJAVA` | Java |
| `InGO` | Go |
| `InTS` | TypeScript |
| `InSwift` | Swift |
| `InC` | C |
| `InDart` | Dart |
| `InPy` | Python |

- Marker syntax + implementation modifiers →§10 (→§10.2, §10.3).

## §1.6 Glossary of AdHoc terms
KW: glossary, terms, definitions

| Term | Meaning | → |
|:--|:--|:--|
| description | C# source file(s) declaring a project | §3 |
| project | top-level C# interface in a namespace, one per file; project of first file = root of description | §3 |
| host | one program: `struct Name : Host` | §10 |
| language marker | `<see cref="InCS"/>` etc. in host doc comment: language host's code is generated in | §10 |
| concrete / abstract | two pack forms: class with field values / interface your type implements | §10 |
| pack | unit of data sent, received or stored: `class` in description | §4 |
| field | typed value of pack, known on wire by place, not number: swapping two fields of one kind changes wire format | §5 |
| constants container | declaration only holding constants, never sent | §6 |
| Value Pack | pack whose fields fit in 64 bits, generated as single primitive value | §7 (§7.9) |
| Pack Set | set of packs built from packs, scopes, filters, exclusions: `_<...>`, `X<...>` | §9 |
| connection | typed link between two hosts: `interface Name : Connects<Left, Right>` | §11 |
| left host / right host | first / second host of connection; branch letters `L`/`l`, `R`/`r` name them | §11 |
| actor | one independent conversation on connection, own state machine | §12 |
| state | empty struct with branch attributes: step of conversation | §12 |
| branch | attribute on state: packs one side may send there + next state | §12 |
| transmittable pack / wire id | pack some branch sends / number preceding it on wire | §20 |
| numbers tables | two doc comments AdHocAgent keeps above project interface: identity numbers of entities | §20 |
| transmitter / receiver | generated objects writing outgoing bytes into send buffer / reading incoming bytes from receive buffer | §1.3 |
| socket buffer | one send + one receive buffer per connection; size your choice | §1.3 |
| session | conversation surviving loss of its socket, continuing on new one | §14 |
| `Stream`, `File` | field types carrying raw bytes of any length through socket buffers | §15 |
| transform chain | stages (compression, cipher, your own) bytes of pack, field or connection pass through | §16 |
| trim | attribute (`ToStream`, `FromStream`, `Stream<To, From>`) making one side of one connection hold payload as raw bytes, not typed object | §17 |
| virtual connection / relay | connection between hosts with no direct link, carried through relay hosts forwarding bytes unread | §18 |
| multiplexer | one listening port serving several connections, told apart by first byte of each socket = dialing host's number | §19 |
| AdHocAgent | command-line utility: checks, uploads, receives, deploys | §24 |
| received folder | `<output folder>/<description name>/` (file name without `.cs`): where generated files arrive | §24 |
| deployment instructions file | Markdown file routing received files into your projects | §25 |
| injection point | marked region in generated file where your code lives | §25 |
| smart merge | deployment step carrying your injection-point code into new generated files | §25 |

# §2 Getting started
KW: getting started, install, UUID, template, MyProtocolDescription.cs, generate, deploy, first run
SUMMARY: From empty folder to generated code in your project: install tools, get UUID, create description project, get template, compose protocol, generate, deploy. Follow steps literally.

## §2.1 What you need for AdHoc
KW: .NET 10, IDE, Rider, Visual Studio Code, Visual Studio, AdHocAgent, Meta.cs, AdHocAgent.dll, UUID

| What | Why | Where |
|:--|:--|:--|
| .NET 10 | AdHocAgent runs on .NET 10; SDK also builds description project | install .NET 10 SDK |
| C# IDE | checks description as you type: syntax, types, references to AdHoc vocabulary | [Rider](https://www.jetbrains.com/rider/), [Visual Studio Code](https://code.visualstudio.com/), [Visual Studio](https://visualstudio.microsoft.com/vs/community/) |
| AdHocAgent | CLI that checks, uploads, receives, deploys | [releases](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases) |
| `Meta.cs` or `AdHocAgent.dll` | AdHoc vocabulary (`Host`, `Connects<L, R>`, attributes) for IDE | [`Meta.cs`](https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs), or `AdHocAgent.dll` of release |
| personal UUID | admits AdHocAgent to generator (cloud service) | sign-up §2.3 |

## §2.2 Step 1: install .NET and AdHocAgent
KW: .NET 10 SDK, releases, install.sh, install.ps1, PATH, AdHocAgent.toml, built-in template
1. Install .NET 10 SDK.
2. Get AdHocAgent, three ways: one-command install, ready build from releases, own build from the repository.
   - Linux/macOS: `curl -fsSL https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.sh | bash` → agent in `~/.local/share/adhoc-agent`, command in `~/.local/bin`.
   - Windows (PowerShell): `irm https://raw.githubusercontent.com/AdHoc-Protocol/AdHoc-protocol/main/install.ps1 | iex` → `%LOCALAPPDATA%\Programs\AdHocAgent`, added to user PATH.
   - Script: build for OS + processor from latest release, verified against `SHA256SUMS.txt`; rerun = update; keeps `AdHocAgent.toml` (personal UUID).
   - Env: `ADHOC_VERSION` (tag, default latest), `ADHOC_INSTALL_DIR`, `ADHOC_BIN_DIR` (Linux/macOS), `ADHOC_DOWNLOAD_URL` (mirror), `ADHOC_NO_MODIFY_PATH=1` (Windows).
   - Ready build: `AdHocAgent-<win|linux|osx>-<x64|arm64>.zip` of latest version from https://github.com/AdHoc-Protocol/AdHoc-protocol/releases, unpack into a folder of its own.
   - Own build: `git clone https://github.com/AdHoc-Protocol/AdHoc-protocol`, then in it `dotnet publish AdHocAgent.csproj -c Release --use-current-runtime --self-contained false -o publish` → agent in `publish`. Workflow runs same build for every system on each version tag; its result = releases.
   - CLI: AdHocAgent keeps config file `AdHocAgent.toml` beside its executable; missing → first run writes it from built-in template.
3. Install a C# IDE if none.

## §2.3 Step 2: get your personal UUID
KW: UUID, PersonalVolatileUUID, AdHocAgent.toml, Sign-Up Discussion, GitHub, volatile
- NOTE: AdHocAgent identifies you to generator server by personal UUID instead of login/password; no interactive sign-in → AdHocAgent can run unattended in build pipeline.
1. Sign in to GitHub.
2. Post a message in Sign-Up Discussion: https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up
3. When processed: post disappears; your GitHub email gets invitation to your private repository in AdHoc-Protocol organization. Accept it: `UUID` issue there holds your UUID.
4. Run AdHocAgent once with the UUID:
```shell
AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f
```
- CLI: stored as `PersonalVolatileUUID` in `AdHocAgent.toml`. UUID is *volatile*: server may replace it during a generation; AdHocAgent then saves new one in same file. Keep that file. Rejected UUID →§24 (→§24.9.3).

## §2.4 Step 3: create the description project
KW: .csproj, Meta.cs, AdHocAgent.dll, net10.0, Compile Include, Link, HintPath, C# 11, generic attributes
- NOTE: IDE checks description only when it belongs to a C# project knowing AdHoc vocabulary. Project never run; exists for IDE.
1. Create a C# project in IDE — class library enough.
2. Give it the vocabulary by IDE's usual means, either:
   - add `Meta.cs` (https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/src/Meta.cs) as existing file (or as link to where you keep it);
   - or add reference to `AdHocAgent.dll` that came with AdHocAgent.
- Same in project file. `Meta.cs` linked from folder beside project:
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
- Reference to `AdHocAgent.dll`:
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
- RULE: vocabulary uses generic attributes (`[l____________<PACKS>]`) → needs C# 11+; any project for .NET 7 or newer has it.
- PITFALL: AdHocAgent has vocabulary built in. Give it description files only — never `Meta.cs`, never a `.csproj` compiling `Meta.cs`. AdHocAgent compiles every given file as part of description → `Meta.cs` checked as one → run stops with error about its names.

## §2.5 Step 4: get the template
KW: AdHocAgent no arguments, MyProtocolDescription.cs, template, init template, CommonPacket, PacketToClient, PacketToServer
- CLI: in folder of description project run:
```shell
AdHocAgent
```
- CLI: prints help, writes template `MyProtocolDescription.cs` into current folder, waits for Enter. PITFALL: existing file of that name overwritten.
- NOTE: SDK-style project includes every `.cs` in its folder → IDE sees new file at once.
- Template = canonical first description; passes AdHocAgent checks as is. Code lines (comments shortened):
```csharp
using org.unirail.Meta; // the AdHoc vocabulary

namespace com.my.company // required
{
    public interface MyProject // the project
    {
        class CommonPacket{ } // an empty pack declared in the project: common to all hosts

        /// <see cref="InTS"/>-  TypeScript: abstract code
        /// <see cref="InCS"/>   C#: concrete code
        /// <see cref="InJAVA"/> Java: concrete code
        struct Server : Host // a host
        {
            public class PacketToClient{ } // an empty pack of the Server host
        }

        /// <see cref="InTS"/>   TypeScript: concrete code
        /// <see cref="InCS"/>-  C#: abstract code
        /// <see cref="InJAVA"/> Java: concrete code
        struct Client : Host // a host
        {
            public class PacketToServer{ } // an empty pack of the Client host
        }

        // The connection: Client is the left host, Server the right host.
        interface Connection : Connects<Client, Server>{
            [l____________<(CommonPacket, Client.PacketToServer)>]
            [____________r<(CommonPacket, Server.PacketToClient)>]
            struct Start { }
        }
    }
}
```
- Smallest description with every part: 1 project, 1 pack shared by all hosts, 2 hosts, 1 connection, 1 state.
- DECL: `using org.unirail.Meta;` brings vocabulary. RULE: namespace required; AdHocAgent refuses project outside namespace. Project = top-level interface `MyProject` →§3.
- DECL: `CommonPacket` in project → any host may use it. `PacketToClient`, `PacketToServer` inside a host → belong to it. All three empty packs →§4.
- DECL: each host doc comment line names a language with marker; `-` right after marker → *abstract* code in that language; no mark → *concrete*. `Server`: abstract TS, concrete C#, concrete Java. `Client`: concrete TS, abstract C#, concrete Java. Concrete packs = classes holding values; abstract = interfaces your types implement →§10.
- DECL: `Connection` links `Client` (left) with `Server` (right) →§11.
- DECL: `Start` = only state. Branch `l____________` lists in a tuple packs left host (`Client`) may send; `____________r` packs right host (`Server`) may send. Small letters → sending does not change state →§12.

## §2.6 Step 5: compose your protocol
KW: compose, edit template, Question, Answer, MinMax, branch lists new packs
- Edit `MyProtocolDescription.cs` in IDE: rename project and namespace; declare packs + fields, hosts + languages, connections with states + branches. IDE checks C# as you type (syntax, types, vocabulary refs); AdHoc rules checked by AdHocAgent in next step.
- First edit: each host gets pack with fields; branches list new packs:
```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface MyProject
    {
        class CommonPacket{ }

        /// <see cref="InTS"/>-
        /// <see cref="InCS"/>
        /// <see cref="InJAVA"/>
        struct Server : Host
        {
            public class PacketToClient{ }

            public class Answer                 // a new pack of the Server host
            {
                string text;                    // a field
            }
        }

        /// <see cref="InTS"/>
        /// <see cref="InCS"/>-
        /// <see cref="InJAVA"/>
        struct Client : Host
        {
            public class PacketToServer{ }

            public class Question               // a new pack of the Client host
            {
                [MinMax(1, 100)] int topic;     // a field with a declared range
                string               text;
            }
        }

        interface Connection : Connects<Client, Server>{
            [l____________<(CommonPacket, Client.PacketToServer, Client.Question)>]
            [____________r<(CommonPacket, Server.PacketToClient, Server.Answer)>]
            struct Start { }
        }
    }
}
```
- PITFALL: pack no branch lists is never sent alone → new pack takes effect only when a branch names it.
- Where declarable things are explained: packs, fields, field types →§4, §5; constants, enums →§6; hosts + languages →§10; connections, actors, states, branches →§11, §12; sessions, streams, tunnels, shared ports →§14, §15, §18, §19.
- NOTE: rest of chapter works with any description; examples use template file names.

## §2.7 Step 6: generate the first code
KW: AdHocAgent MyProtocolDescription.cs, !, UUID by form, numbers tables, received folder, output folder, MyProtocolDescription.md
- CLI: run with description file, with its folder or without:
```shell
AdHocAgent MyProtocolDescription.cs
AdHocAgent .\MyProtocolDescription.cs        # the same, with the folder
```
- RULE: UUID told by its form (`100b9fd2-e593-485b-a2fe-9b9c82bc1e3f`); file name never has it; missing file reported with full path.
- CLI: full path works too. `!` after name (`AdHocAgent .\MyProtocolDescription.cs!`) → code also tested, as far as server resources allow.
- CLI: first argument = UUID only when it has UUID form (`100b9fd2-e593-485b-a2fe-9b9c82bc1e3f`); file name works with or without folder (`AdHocAgent MyProtocol.cs` = `AdHocAgent .\MyProtocol.cs`). Missing description file → "There is no description file …"; missing `.cs`/`.csproj` among further arguments → same, not taken for output folder.
- What the run does:
  1. Compiles + checks description. Error → stops with file, line, what to fix; nothing sent.
  2. Writes numbers tables into description file: identity numbers of packs, hosts, connections + short `/*…*/` marks after branches. Template file now begins (project's own number differs per project):
```csharp
namespace com.my.company // required
{
    /** packs
        <see cref='Client.PacketToServer'/>ā    1
        <see cref='CommonPacket'/>ÿ             0
        <see cref='Server.PacketToClient'/>Ā    2
    */
    /** project
        <see cref='MyProject'/>ǃƴǣźč

        hosts
        <see cref='Server'/>ÿ
        <see cref='Client'/>Ā

        connections
        <see cref='Connection'/>ÿ
    */
    public interface MyProject // the project
```
     Branches read `[l____________<(CommonPacket, Client.PacketToServer)>/*Ā*/]`. Decimal column = each pack's wire id. RULE: numbers = identities of entities: keep rewritten file under version control; never edit numbers or marks →§20.
  3. Uploads project, shows generator progress. Server generates; with `!` also tests.
  4. Generated files arrive in received folder `MyProtocolDescription` (named after description file), created in output folder = current folder unless another one named as last argument. PITFALL: AdHocAgent clears received folder at start of every generation → never edit files there.
  5. First run: no deployment instructions file yet → AdHocAgent writes `MyProtocolDescription.md` beside received folder, says so, stops.
- CLI: checking without upload, all other arguments/options →§24.

## §2.8 What comes back from the generator
KW: received folder, .description, InCS, InJAVA, InTS, gen, lib, demo, LICENSE-GRANT.md
- GEN: output folder for template, in outline:
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
- `gen` = host's generated code; `lib` = AdHoc runtime library; `demo` = host skeleton to start from →§1.4.

## §2.9 Step 7: deploy the first code into your project
KW: MyProtocolDescription.md, route, ⛔, AdHocAgent .\MyProtocolDescription.md, y, protocol folder, destination folder
- CLI: open `MyProtocolDescription.md`: lists language folders and host folders; explains itself in its quick start. To deploy a host write a `route` at end of its line = Markdown link whose target is a folder of your project; put `⛔` at end of line of every unused host:
```markdown
- 📁[InCS](/D:/Work/MyProtocolDescription/InCS)
	- 📁[Client](/D:/Work/MyProtocolDescription/InCS/Client) ⛔
	- 📁[Server](/D:/Work/MyProtocolDescription/InCS/Server) [](/D:/Projects/server/protocol/)
```
- CLI: run deployment on files already received:
```shell
AdHocAgent .\MyProtocolDescription.md
```
- CLI: AdHocAgent reports what it is about to do; when list of received files changed or destination holds files it would delete → asks for `y` before writing anything. Host's `gen`, `lib`, license file land in `D:\Projects\server\protocol\`. Add `protocol/gen` and `protocol/lib` to source folders of your build. Every later generation of this description deploys by same routes.
- PITFALL: destination folder belongs to generator. File in it that deployment does not write → backed up then deleted. Route each host to a folder holding generated code only (`protocol` folder beside sources), never project root →§25.3.4.
- NOTE: AdHoc's own services: each has `protocol` folder holding `gen`, `lib`, `LICENSE-GRANT.md`, nothing else. Deploying `demo`/project files, routes with filters, smart merge, backups →§25.

## §2.10 Where to go next with AdHoc
KW: next steps, chapters
- Description file (projects, C# constructs, naming) →§3. Data: →§4, §5, §6, §7. Hosts + languages →§10. Conversations →§11, §12. Every CLI task/option →§24.

# §3 The description file
KW: description, using org.unirail.Meta, namespace, project interface, Roslyn, numbers tables, naming
SUMMARY: The file itself: compilation, project declaration, several projects, C#-construct → entity mapping, struct roles, naming rules, comments, numbers tables.
- Description = plain C# source in .NET project; C# is description language. IDE + C# compiler check syntax and AdHoc type references; AdHocAgent then checks AdHoc rules.
- NOTE (anatomy): file = `using org.unirail.Meta` + required namespace; in namespace numbers tables (written by AdHocAgent) before project; project interface holds classes (packs, possibly nested), structs implementing `Host` (hosts with markers, possibly own packs), struct without interfaces + enum (constants), interface implementing `Connects` (connection with state structs carrying branch attributes, and actor interface with own state). Branches select packs outside connection (in project or host).

## §3.1 A description is compiled C#
KW: using org.unirail.Meta, Roslyn, compile error, warnings off, access modifiers, public, private
- DECL: every description file starts with `using org.unirail.Meta;` — imports `Host`, `Connects<L, R>`, branch attributes, language markers `InCS`, `InJAVA`, `InTS`, field attributes, built-in types.
- RULE: AdHocAgent recognizes these types by full names in namespace `org.unirail.Meta` → must come from there: `Meta.cs` or `AdHocAgent.dll` in IDE; AdHocAgent's own copy when it runs →§2.
- CLI: compiles all given description files together with Roslyn, warnings off. C# compile error stops run with compiler message before any AdHoc rule; IDE shows same errors. Then reads compiled declarations as AdHoc entities and checks rules of each.
- RULE: C# access modifiers carry no meaning; ignored wherever C# accepts them. `public ulong timestamp;`, `private float x;`, `ulong timestamp;` = same kind of field; `public class` = `class`. (C# accessibility still applies for compilation →§4.2.)

## §3.2 The project interface
KW: project, interface, namespace, file-scoped namespace, root namespace, package, exit code 45, declares no project
- DECL: `project` = C# `interface` at top level of a namespace. RULE: namespace required; becomes root namespace / package of generated code.
```csharp
using org.unirail.Meta;
namespace com.my.company
{
    public interface MyProject
    {
        // the description: packs, hosts, connections, ...
    }
}
```
- DECL: file-scoped namespace works same (saves brace level):
```csharp
using org.unirail.Meta;
namespace com.my.company;
public interface MyProject
{
    // the description: packs, hosts, connections, ...
}
```
- RULE: skeletons above valid C# but not generating. Project needs ≥1 connection, connection ≥1 state. Skeleton as is → "The description declares no connection, so there is nothing to generate." (exit code 45).
- Smallest complete project (1 pack, 2 hosts, connection with 1 state):
```csharp
using org.unirail.Meta;
namespace com.my.company
{
    public interface Endianness
    {
        class Sample
        {
            [MinMax(0, 12345)]  ulong field_u2;
            [MinMax(-12345, 0)] long  field_uq;
            float                     field_f;
            double                    field_d;
        }
        ///<see cref='InTS'/>
        ///<see cref='InJAVA'/>
        ///<see cref='InCS'/>
        struct Server : Host { }
        ///<see cref='InTS'/>
        ///<see cref='InJAVA'/>
        ///<see cref='InCS'/>
        struct Client : Host { }
        interface Channel : Connects<Client, Server>
        {
            [_____lr_____<Sample>]
            struct Start { }
        }
    }
}
```
- DECL: `_____lr_____` = branch of both sides at once: in `Start` either host may send `Sample`, state stays.
- PITFALL: pack named `Sample`, not `Endianness`: pack named like project is renamed in generated code →§3.6. Init template (§2.5) = another complete first project.

| Situation | Result |
|:--|:--|
| no top-level interface in files | stops: "… declares no project. A project is a top-level interface in a namespace … Run AdHocAgent without arguments in an empty folder to get a template to start from." |
| project outside namespace | stops: "A project is declared inside a namespace, and these are not: …", then "Put each of them into a namespace - the namespace of its generated code" |
| root project with no connection | stops: "The description declares no connection, so there is nothing to generate." (exit code 45) |

- GEN: doc comment on namespace not lost: put in front of project's own documentation; both go into generated code.

## §3.3 Several projects in one description
KW: root project, library project, interface MyProject : Shared, one file one project, several files, .csproj, Compile Include, exit code 45, declare several
- RULE: every top-level interface = project; one file = one project. Project of first file of command line = `root project` = one whose hosts get generated. Others = libraries root project can extend (`MyProject : Shared1, Shared2`) and take entities from.
```csharp
// MyProject.cs - the root project
using org.unirail.Meta;
namespace com.my.company
{
    public interface MyProject : Shared
    {
        /// <see cref="InCS"/>
        struct Sensor : Host { }
        /// <see cref="InJAVA"/>
        struct Collector : Host { }
        interface Link : Connects<Sensor, Collector>
        {
            [l____________<Shared.Reading>]
            struct Start { }
        }
    }
}
```
```csharp
// Shared.cs - a library project
using org.unirail.Meta;
namespace com.my.company
{
    public interface Shared
    {
        class Reading { float value; }
    }
}
```
- CLI: `AdHocAgent MyProject.cs Shared.cs`: after first argument AdHocAgent takes further `.cs` files, or a `.csproj` from which it reads files named in its `<Compile Include="…"/>` items - NOT files an SDK-style project compiles by default →§24.
- RULE: file declaring several projects → exit code 2: "One file declares one project, and these declare several: <file>: Core, Notes, App" "Give each project a file of its own and pass the root project's file first: …".
- PITFALL: order of files matters. `Shared.cs` first → `Shared` becomes root; no connection → exit code 45.
- Extending, importing selected entities, modifying →§22.

## §3.4 From C# constructs to AdHoc entities
KW: class, struct, interface, enum, Host, Connects, VirtuallyConnects, Multiplex, Actor, Modify, HeaderFor, FieldsInjectInto, TYPEDEF, _DefaultMaxLengthOf, Resumable, IfSendingFrom, _<>
- RULE: entity kind decided by C# kind, location, what it implements. Most entities extend an `org.unirail.Meta` type; rest told apart by place.

| C# construct | Where | Entity | → |
|:--|:--|:--|:--|
| `interface` at top level of namespace | file | project; `interface P : Other` extends project `Other` | §3, §22 |
| `class` | project, host, another pack | pack | §4 |
| `class` with no fields that no branch sends | anywhere a pack may be | constants set, not pack | §6 |
| `class` whose only field is named `TYPEDEF` | anywhere a pack may be | type alias: field type + its attributes, reusable | §5.18 |
| `class X : HeaderFor<…>` / `: FieldsInjectInto<…>` | project, host; header also in connection | pack header / field injector | §8 |
| `class X : Stream` / `: File` | anywhere a pack may be | named conduit: raw byte stream as field type | §15 |
| `class X : DateTimeDef` / `: TimeSpanDef` / `: Duration` | anywhere a pack may be | time type | §5.20 |
| `class X : Modify<P>` | project | modifier of pack or pack header | §8, §22 |
| `interface X : Modify<C>`, `C` connection | project | connection modifier: adds actors, RPC methods, Actor0 states; removes states; replaces attributes | §22.3.6 |
| `struct X : Host` | directly in project | host | §10 |
| `struct X : Modify<H>`, `H` host | directly in project | host modifier | §10 |
| `struct` with branch attributes | connection or actor body | state | §12 |
| `struct` implementing no interface | outside connection body | constants container | §6 |
| `enum` | outside connection body | enum (named constants); `[Flags]` for bit flags | §6 |
| `enum _DefaultMaxLengthOf` | project | project's default length limits, not enum | §5.11 |
| `interface X : Connects<L, R>` | directly in project | connection | §11 |
| `interface X : VirtuallyConnects<L, R, PATH>` | directly in project | virtual connection | §18 |
| `interface X : Multiplex<(…)>` | directly in project | multiplexer | §19 |
| `interface X : Actor` | connection body, or interface grouping actors in it | actor | §13 |
| `interface` with no AdHoc base | connection body | group of actors | §13 |
| `interface` with no AdHoc base | elsewhere in project | constants container | §6 |
| method returning tuple, e.g. `(L____________, Pong) Ping(PingReq r);` | connection body | RPC actor | §13 (§13.3) |
| `interface X : _<…>` | project, host, connection | Named Pack Set | §9 |
| generic `interface F<SCOPE>` (one type param), usually with filter attributes | project, host, connection | filter template | §9 |
| `interface X : IfSendingFrom<H, C>`; `interface X : _<…>` whose items are endpoints | project | endpoint (one host sending over one connection); endpoint set | §17 |
| `interface X : Resumable<HOST, PACKS>` | connection body | guaranteed delivery of `PACKS` sent by `HOST` | §14 |
| instance field | class | field | §5 |
| `const` or `static` field | class, struct or interface | constant | §6 |
| `/// <see cref="InCS"/>` and other markers | host's doc comment | host's languages + implementation settings | §10 |

- Several rows in one valid description:
```csharp
using org.unirail.Meta;
/// The order service of the shop.
namespace com.my.company
{
    public interface Shop                                  // project
    {
        struct Limits                                      // constants container
        {
            public const byte MAX_ITEMS = 100;
        }
        enum Payment { Card, Cash }                        // enum
        class Order                                        // pack
        {
            [MinMax(1, Limits.MAX_ITEMS)] byte items;      // field, limited by a constant
            Payment                            payment;    // field typed with the enum
        }
        /// <see cref="InJAVA"/>
        struct Store : Host                                // host
        {
            public class Receipt { ulong serial; }         // a pack of the Store host
        }
        /// <see cref="InTS"/>
        struct Browser : Host { }                          // host
        interface Session : Connects<Browser, Store>       // connection
        {
            [L____________<Paying, Order>]                 // branch
            struct Open { }                                // state
            [____________R<Open, Store.Receipt>]           // branch
            struct Paying { }                              // state
        }
    }
}
```

### §3.4.1 What a connection body may hold
KW: connection body, states, actors, RPC methods, Named Pack Sets, filter templates, HeaderFor, Resumable, exit code 22
- RULE: connection body = conversation, not data. May hold: states, actors + interfaces grouping them, RPC methods, Named Pack Sets and filter templates used by its branches, pack headers (`HeaderFor`), guaranteed-delivery declarations (`Resumable<…>`).
- RULE: packs, enums, constants containers declared outside it. A `class` not `HeaderFor`, an `enum`, or a `struct` without branch attributes inside connection body → stops: "'…' (…) is declared inside the connection …: a connection body holds its actors, states and RPC methods only" (exit code 22). →§11

## §3.5 The four roles of a struct
KW: struct, state, host, host modifier, constants container, implements interfaces, not a host, exit code 22, Modify<H>
- RULE: pack is always `class`; `struct` never a pack; exactly one of 4 roles:

| Role | Recognized by | Where | Constraint |
|:--|:--|:--|:--|
| state | carries ≥1 branch attribute | connection or actor body | must be empty: no fields, methods, nested types (exit code 22 otherwise) |
| host | implements `Host` | directly in project interface | - |
| host modifier | implements `Modify<H>`, `H` a host | directly in project interface | - |
| constants container | implements no interface | anywhere outside connection body | `const` and `static` fields only |

- RULE errors:
  - struct with branch attributes outside connection/actor body → "The State '…' (line …) must be declared inside a Connection/Actor body." (exit code 22);
  - struct without branch attributes inside connection body → "… is declared inside the connection …" (exit code 22);
  - struct implementing any other interface, or implementing `Host` other than directly in project → "The struct … (…) implements interfaces, so it is not a constants set, and it is not a host either. A host is declared directly in the project: `struct X : Host { }`; a modifier of a host: `struct X : Modify<OtherHost> { }`."
- PITFALL: host is always struct: `interface X : Host` declares no host; AdHocAgent ignores it without message.
- RULE: time types `DateTimeDef`, `TimeSpanDef`, `Duration` implemented by `class`; struct implementing them is rejected the same way ("The struct … implements interfaces, so it is not a constants set, and it is not a host either").

## §3.6 Naming rules for description entities
KW: naming, underscore, keyword, prohibited, CS0542, Cross-platform naming conflict, case-insensitive, rename, ENdianness, tYpe, namespacE
- RULE: names become names in every target language → must be valid in all.

| Rule | AdHocAgent does |
|:--|:--|
| name must not start or end with `_` (`key_`, `_id`); only exception `enum _DefaultMaxLengthOf` | stops: "A name cannot start or end with `_`: rename … (line …)." |
| name must not be keyword of C#, C++, Java, TypeScript, Rust or Go, nor `arguments`, `eval`, `undefined`; case-sensitive check | stops: "The name … (…) is a keyword in a language AdHoc generates code in. Rename it, for example to …." |
| member must not have name of enclosing class/struct | C# compiler stops run: error CS0542, "member names cannot be the same as their enclosing type" |
| packs declared side by side must differ by more than letter case (`Status`/`STATUS`) | stops: "Cross-platform naming conflict" — Java classes of the two would overwrite each other on case-insensitive file system |
| pack directly in project should not have project's name | warns; renames pack in generated code by capitalizing a letter (`Endianness` → `ENdianness`) |
| nested pack or enum should not have name of a type in enclosing scope | warns: inside its parent the name now means nested type → field there referring to outer type gets nested one |
| actors have reserved names (`Actor0`, `Connection`, `OnReceived`, ...) | →§12 |

- PITFALL: keyword list = union of six languages → ordinary words taken: `type`, `number`, `function`, `var`, `let`, `final`, `native`, `transient`, `declare`, `match`, `loop`, `fn`, `chan`, `select`. Change case of one letter to keep word: AdHoc's own protocol declares `Type tYpe;`, `string namespacE;`.

## §3.7 Comments in a description
KW: ///, /** */, //, documentation, trailing comment, namespace doc, language marker block, configuration
- GEN: comment before declaration (`///`, `/** */` or `//`) → documentation of generated class/field/constant (XML doc in C#, Javadoc in Java, JSDoc in TS).
- GEN: `//` at end of declaration line → comment at end of generated line.
- GEN: namespace doc comment → in front of project's documentation.
- RULE: doc comment block holding language marker (`<see cref="InCS"/>`) or numbers table = configuration, not documentation. Write text about host in a block of its own.
- Placement and per-language escaping →§4.6.

### §3.7.1 Doc-comment directives: `<see cref="…"/>` with `+` or `-`
KW: <see cref>, +, -, directive, composition, language marker, project doc comment, _<>
- RULE: in doc comment of pack or host, `<see cref="…"/>` = instruction, not reference; `+` or `-` right after `/>` modifies it.

| In doc comment of | `<see cref="…"/>` means | → |
|:--|:--|:--|
| pack | `+` right after `/>` → take fields of referenced pack or field into this pack; `-` → remove them; no mark → plain reference | §8 (§8.2) |
| host | language marker, up to two `+`/`-` after it: concrete/abstract code, hash+equality on/off; references after a marker = entities the setting applies to | §10 |
| connection | copy states of referenced connection (`SwapHosts{…}`: sides swapped) or one state into this connection, with or without `+`; followed by `-` → leave copied state out | §11 |

- RULE: `<see cref>` in pack's leading doc comment = field directive only with `+`/`-` right after `/>`; no mark → plain reference, documentation.
- PITFALL: project interface doc comment = third case: plain text only, no references. AdHocAgent takes a block with `<see cref>` right before project interface for an old-form numbers table and deletes it when rewriting file. To take single declarations of another project put `_<…>` in project's base list →§22 (§22.2).

## §3.8 The numbers tables at the top of the file
KW: numbers tables, /** packs, /** project, wire id, /*Ā*/, branch mark, tags, rename, version control
- CLI: first run writes 2 doc comments right before project interface, `/** packs` and `/** project`; keeps them up to date afterwards. Hold number for every entity that must keep identity across runs (project, hosts, connections, actors, states, packs) + each transmittable pack's wire id.
- NOTE: code below tables carries no numbers → declarations may be moved, reordered, renamed without changing wire.
- PITFALL: rename with IDE (renames reference in table line too); after rename by hand next run stops until you correct the line.
- NOTE: only marks left in code = short block comments after branch attributes, e.g. `/*Ā*/`, identifying the branch.
- RULE: tables belong to AdHocAgent; only part that is yours = text after a pack's numbers = its tags (branches can select packs by them). Never edit numbers or marks.
- WIRE: fields have no numbers: known by place = declaration order among fields of its kind → swapping two fields of one kind changes pack wire format →§7. Tables, tags, upkeep →§20.

# §4 Packs
KW: pack, class, instance field, wire id, transmittable, empty pack, nested pack, documentation
SUMMARY: The pack: contents, where declarable, nesting, which classes are packs, empty packs, documentation reaching generated code. Fields →§5; constants/enums in packs →§6; byte layout →§7.
- `pack` = unit of data moved between hosts; declared as C# `class`; instance fields = data that travels. Everything sent/received is a pack.
- GEN: for every host sending or receiving a pack: a type in each of host's languages + code writing pack into socket buffer and reading it back.

## §4.1 What a pack contains
KW: instance field, const, static, nested class, nested enum, field order

| Member | What | → |
|:--|:--|:--|
| instance field `int x;` | data; transmitted with every instance | §5 |
| `const` / `static` field | constant; generated as constant of pack's type; never transmitted | §6 |
| nested `class` | another pack or other class kind (§4.4), named through this one | §4.3 |
| nested `enum` | enum; usable as field type here or anywhere | §6 |

- NOTE: only instance fields are data. Pack whose only members are constants + nested declarations has no data → empty pack →§4.5.
- WIRE: field order is part of pack. Field carries no number/tag on wire. Fields travel in declared order, primitive fields included. Reordering fields of deployed pack changes it for every host sending/receiving it. Layout →§7 (→§7.10); final order of composed pack →§8 (→§8.4).

## §4.2 Where a pack may be declared
KW: common pack, host pack, nested pack, constants container, connection body, exit code 22, public, accessibility, __id_, BytesDst, BytesSrc

| Place | Meaning |
|:--|:--|
| directly in project interface | common pack: any host may send/receive |
| inside host (`struct Server : Host`) | pack belongs to that host; effect on host's generated code →§10 |
| inside another pack | nested pack →§4.3 |
| inside constants container (`struct`/`interface` with no base) | pack grouped under a name →§6 |
| inside connection (`interface X : Connects<…>`) | NOT allowed. Only classes allowed in connection body = pack headers (`HeaderFor` →§8.6). Any other class or enum → exit code 22 →§11 |

- Example = new-user project (template): `CommonPacket` common; `PacketToClient` belongs to `Server`; `PacketToServer` to `Client`; connection names which packs each side sends. Code as in §2.5 (Server `InTS` `-`, `InCS`, `InJAVA`; Client `InTS`, `InCS` `-`, `InJAVA`).
- RULE: description compiled as ordinary C# → C# accessibility applies. Project interface members public by default; `struct`/`class` members private by default → mark pack `public` when connection or another host refers to it (`Server.PacketToClient`). Naming rules (no member with own name, reserved words) →§3.6.
- GEN: pack with fields → class with wire id, property per field, runtime serializer/parser interfaces. Trimmed C#:
```csharp
public partial class TeamCoordination : IEquatable<TeamCoordination>,
                                         AdHoc.Connection.Receiver.BytesDst,     // parsed from the receive buffer
                                         AdHoc.Connection.Transmitter.BytesSrc   // serialized into the transmit buffer
{
    public int __id => __id_;
    public const int __id_ = 4;                     // the pack's id on the wire
    public uint team_id { get; set; } = (uint) 0;
    public string? objective { get; set; } = null;
    // ... other fields, equality, hashing, the serializer and the parser
}
```
- GEN: pack whose field data fits in 8 bytes → generated as value type, not class (Value Pack →§7.9). Concrete vs abstract per host + language →§10.

## §4.3 Nested packs
KW: nested pack, qualified name, PlayerAction.StateUpdate, partial class, file name, scope, embedding
- RULE: class declared inside pack = pack of its own. Nesting gives qualified name, nothing else. Nested pack has own fields and, when a branch selects it, own wire id. Never part of parent's data.
- NOTE: data nests only through a field: field whose type is a pack carries whole instance inside parent (one pack on wire, embedded bytes inside parent bytes) →§5 (§5.16). Declaration nesting → separate packs, each sent alone, each own id.
```csharp
using org.unirail.Meta;
namespace com.my.company
{
    public interface Game
    {
        /// <see cref="InCS"/>
        struct Server : Host
        {
            public class PlayerAction
            {
                byte action_type;
                int  action_id;
                Kind kind;                         // a nested enum used as a field type
                public enum Kind : byte { Move, Attack, Chat }
                public class StateUpdate           // a pack of its own, named PlayerAction.StateUpdate
                {
                    ushort state_id;
                    bool   is_active;
                }
            }
        }
        /// <see cref="InTS"/>
        struct Client : Host { }
        interface Channel : Connects<Client, Server>
        {
            [____________r<(Server.PlayerAction, Server.PlayerAction.StateUpdate)>]
            struct Start { }
        }
    }
}
```
- DECL: packs nest to any depth; enums nest in packs same way.
- GEN: nesting kept: C# `PlayerAction.StateUpdate` = partial class nested in `PlayerAction`. Each pack's code in own file named after pack's path, `_` between names: `ChatMessage` nested in `GameStateUpdate` nested in project-level pack `PlayerAction` → `PlayerAction_GameStateUpdate_ChatMessage.cs`.
- NOTE: pack also = scope: branch or Pack Set can select a pack with all packs declared inside it → nesting decides what such selection takes →§9 (→§9.4).

## §4.4 Which classes are packs
KW: transmittable, TYPEDEF, DateTimeDef, TimeSpanDef, Duration, FieldsInjectInto, HeaderFor, Modify, constants container, not a transmittable message type
- RULE: not every `class` is a pack on wire; agent decides by what class implements and holds. Only transmittable pack can be sent by branch; other kinds = compile-time constructs (some usable as field types, none sendable alone).

| A `class` that… | is a… | Branch can send | Field type | → |
|:--|:--|:--|:--|:--|
| has instance fields | pack | yes | yes | §4, §5 |
| has no instance fields, a branch sends it | empty pack | yes | yes, as `bool` | §4.5 |
| has no instance fields, no branch sends it | constants container | no | as `bool` only (§4.5.1) | §6 |
| has exactly one field, named `TYPEDEF` | type alias (TYPEDEF) | no | yes | §5.18 |
| implements `DateTimeDef`, `TimeSpanDef` or `Duration` | time type | no | yes | §5.20 |
| implements `FieldsInjectInto<…>` | field template | no | no | §8.5 |
| implements `HeaderFor<…>` | pack header | no | no | §8.6 |
| implements `Modify<…>` | modifier of existing entity | no | no | §22.3 |

- NOTE: named `Stream`/`File` packs (`class X : Stream { }`) = separate case →§15 (§15.5).
- RULE: branch listing non-transmittable type → stops: "Type '…' is a TYPEDEF (Type Alias) and not a transmittable message type … can only be used as field types within Pack declaration". Same check covers enums, constants sets, time types, field templates, headers, modifiers.
- RULE: scope selection (`@Project`, `@Host`, pack scope, Pack Set filter) collects only transmittable packs; skips rest silently →§9.
- RULE: transmittable pack gets wire id only when a branch actually selects it. Pack used only as field type travels inside parent, needs no id →§20.

## §4.5 Empty packs
KW: empty pack, no instance fields, Handler, ONE, __put_bytes, event, signal, constants container
- `empty pack` = transmittable pack with no instance fields; may still hold constants + nested declarations. Carries no data, only fact of arrival → natural signal for event or state transition.
```csharp
/// <see cref="InCS"/>
struct Server : Host
{
    /// Invites the client to the next state of the conversation.
    public class Invitation { }     // an empty pack: a branch of the connection sends it
}
```
- GEN: no instance members, no allocation per message. Receiving code = one static handler consuming no payload bytes. WIRE: only pack id on wire. C# receiving side:
```csharp
public static partial class Invitation
{
    public const int __id_ = 3;
    public class Handler : AdHoc.Connection.Receiver.BytesDst
    {
        public int __id => __id_;
        public static readonly Handler ONE = new Handler();     // the only instance
        bool AdHoc.Connection.Receiver.BytesDst.__put_bytes(AdHoc.Connection.Receiver __src) => true;   // nothing to read
    }
}
```
- RULE: class without instance fields becomes empty pack only when a branch sends it. Empty class no branch sends → automatically reclassified as constants container: no wire id, no generated pack →§6. To group nested declarations never transmitted, declare constants container on purpose.

### §4.5.1 An empty pack as a field type
KW: bool, Empty pack used as field datatype, WRN, Map key, Set<bool>, Set<Empty>, nullable bool
- RULE: field typed with empty pack can only carry present/absent → agent replaces type with `bool` (`true` = present), warns:
```text
WRN  Empty pack used as field datatype — replaced with `bool`
```
- GEN: replacement follows field shape:
```csharp
class Empty { }
class Degenerate
{
    Empty                empty;          // bool
    Empty?               empty_Q;        // nullable bool: null, false, true
    [D(3)] Empty[]       arr3_empty;     // an array of 3 bools
    Map<int, Empty>      map_int_empty;  // Map<int, bool>
}
```

| Use of empty pack | Result |
|:--|:--|
| field type, array element, `Map` value | replaced by `bool`, warning above |
| `Map` key | error: "The Map field … is keyed by …, a pack without fields: all its values are equal, so it cannot tell keys apart. Give the map another key type." |
| `Set` element | not supported: set of 2 possible values carries nothing; `Set<bool>` refused. PITFALL: agent does not catch it yet — turns `Set<Empty>` into `Set<bool>` with warning. Do not declare it |

- NOTE: class need not be sent anywhere: empty class no branch sends = constants container (constants generated as usual); field typed with it still `bool`. `struct`/`interface` constants container is not a field type at all →§6.
- RULE: pack headers, field templates, time types, named `Stream`/`File` packs have no own fields either but are not empty packs; `bool` replacement does not apply.

## §4.6 Documentation in the generated code
KW: documentation, javadoc, JSDoc, XML documentation, doc comment
- GEN: write once as C# comments; agent renders per target language: javadoc (Java), JSDoc (TS), XML documentation (C#), placed above generated declaration.

### §4.6.1 Which comments are documentation
KW: ///, /** */, //, trailing comment, language marker, numbers table, <summary>, <remarks>

| Comment in description | Becomes |
|:--|:--|
| `///` lines or `/** … */` block directly above declaration | documentation of generated declaration |
| plain `//` lines above declaration (after previous declaration) | documentation too, joined line by line |
| `//` at end of declaration's own line | comment at end of generated line |
| doc block holding language marker (`<see cref="InCS"/>` …) or numbers table | not documentation: configuration, not copied |

- Example source:
```csharp
/// <summary>A player's request to join a room.</summary>
/// <remarks>The server answers with <c>JoinReply</c>.</remarks>
public class JoinRoom
{
    /// The room to join; 0 means any free room.
    uint    room_id;
    string? password;   // optional, at most 255 characters
}
```
- GEN C# (marker comments of generated code omitted):
```csharp
/// <summary>
/// A player's request to join a room.
/// </summary>
/// <remarks>
/// The server answers with <c>JoinReply</c>.
/// </remarks>
public partial class JoinRoom : IEquatable<JoinRoom>, AdHoc.Connection.Receiver.BytesDst, AdHoc.Connection.Transmitter.BytesSrc
{
    /// <summary>
    /// The room to join; 0 means any free room.
    /// </summary>
    public uint room_id { get; set; } = (uint) 0;

    public string? password { get; set; } = null;  //optional, at most 255 characters
```
- GEN Java (javadoc of same text):
```java
/**
 * A player's request to join a room.
 * <p>
 * The server answers with <code>JoinReply</code>.
 */
```

### §4.6.2 Where the documentation of each entity goes
KW: OnReceived.Pack, OnSerialized.Pack, Pack.Nullable, Pack.field__, Value Pack, Connection, RPC group

| In description | In generated code |
|:--|:--|
| pack | its class. Value Pack: its type, the calls of the pack, `Pack.Nullable`. Also pack handlers in a state: `OnReceived.Pack`, `OnSerialized.Pack` |
| field | every declaration about the field: what reads it, writes it, asks for its value or makes its item; its property; `Pack.field__` with its constants |
| enum + members | enum and its constants |
| host, project | file of the host |
| connection, actor, state | classes `Connection`, the actor, the state |
| interface only grouping RPC methods | nowhere: no class of its own in generated code |

### §4.6.3 How the text is rendered
KW: escaping, @, \u, */, <summary>, <remarks>, <para>, <c>, <code>, backticks, <see cref>, KeepDoc, SkipDoc
- GEN escaping: text escaped per target language. `<` and `&` cannot break javadoc/XML doc; `*/` cannot end comment. Java: `@` and `\` written as entities → stray `@` not read as javadoc tag, `\u` not decoded by `javac`. Trailing `//` comment containing `\u` gets one more backslash before `u`.
- GEN structure: `<summary>`/`<remarks>` keep places: C# first section = summary, following = remarks; Java + TS sections → paragraphs. `<para>` → paragraph. `<c>`/`<code>` → code: `<c>` in C#, `<code>` in Java, backticks in TS.
- GEN references: `<see cref="UserTier"/>` inside text → name it points to, in code font; name kept exactly as written (each language names entity its own way).
- GEN directives: composition directive `<see cref="Pack.field"/>+` or `<see cref="Pack.field"/>-` leaves nothing in documentation, nor does rest of its line. RULE: mark must follow `/>` immediately.
- GEN filters: until project complete, text keeps marks read by `[KeepDoc]`/`[SkipDoc]` Pack Set filters; documentation rendered per language last →§9 (§9.5.2).

### §4.6.4 Pitfalls of documentation comments
KW: <see cref> in pack doc, <c>, host config block, // banner, #region, absorbed comments
- RULE: in pack's documentation `<see cref>` with `+`/`-` right after `/>` = composition directive (adds/removes fields, leaves nothing in docs); without mark = plain reference, rendered as name in code font →§8. `<see cref="UserTier"/>` = plain reference; `<see cref="UserTier"/>+` imports fields of `UserTier`; `<see cref="UserTier"/>-` removes them.
- PITFALL: sentence starting right after a reference with a dash (`<see cref="UserTier"/>- the tier`) = directive too (removes fields): put a space before the dash.
- PITFALL: keep host documentation apart from configuration: doc block containing a language marker = configuration as a whole, its prose dropped. Text about host in own doc block:
```csharp
/// Accepts player connections and runs the game rooms.
/**
    <see cref="InCS"/>
    <see cref="InJAVA"/>-
*/
struct Server : Host { }
```
- PITFALL: free-standing `//` notes absorbed: every `//` line between previous declaration and this one → this declaration's documentation, across blank lines too. Banner `// ==== Headers ====` above a pack ends up in pack's generated docs. Use `#region` names instead: region name is not a comment, never documentation.
- NOTE: editing documentation does not change protocol: changes generated comments only, nothing a host does. What changes wire →§21.

# §5 Fields and types
KW: field, field type, instance field, pack field, data type, attributes, Stream, File
SUMMARY: every field type: meaning, declaration, generated code, agent rejections. Encoding (ranges, bit-packing, varint) →§7; conduits `Stream`/`File` →§15.
- `field` = instance field of a pack class; C# type selects AdHoc data type; attributes narrow it (length, range, distribution). Fields = pack's data; travel in declaration order →§7.

## §5.1 The field types at a glance
KW: field types, int, double, bool, longJS, string, Map, Set, Binary, TYPEDEF, DateTime, Stream, File
| Kind of value | Declare as | → |
|:--|:--|:--|
| integer, float, `bool`, `char` | `int count;` `double ratio;` `bool on;` | §5.2 |
| 64-bit int TS holds as plain number | `longJS id;` `ulongJS size;` | §5.6 |
| 128-bit integer | `Int128 hash;` `UInt128 counter;` | §5.3 |
| UUID | `Guid id;` `Guid? parent;` | §5.4 |
| exact decimal number | `[Decimal(5, 2)] decimal rate;` | §5.5 |
| value that may be absent | `int? count;` (reference types always optional) | §5.7 |
| text | `string name;` `[D(+N)] string name;` | §5.8 |
| array | `T[]`, `T[,]`, `T[,,]` with `[D(N)]` | §5.9 |
| map, set | `Map<K, V>`, `Set<K>` with `[D(+N)]` | §5.12 |
| multidimensional array | `[D(-2, ~3)] T name;` | §5.14 |
| array of arrays/maps/sets | `T[][,,]`, `Map<K, V>[]` | §5.15 |
| nested pack, enum | `Point origin;` `Mode mode;` | §5.16 |
| raw bytes of known length | `[D(32)] Binary[] hash;` | §5.17 |
| named alias of a type | class with one field named `TYPEDEF` | §5.18 |
| one of several alternatives | pack of optional fields | §5.19 |
| moment, recent moment, duration | `DateTime`, `DateTimeDef`, `TimeSpanDef`, `Duration` | §5.20 |
| bytes streamed through a channel | `[S(N)] Stream`, `[S(N)] File` | §15 |

## §5.2 Numeric and other primitive types
KW: sbyte, byte, short, ushort, int, uint, long, ulong, Int128, UInt128, float, double, char, bool, bool?, decimal, [Decimal(p, s)], Decimal_p_s
- RULE: all C# numeric primitives are field types. `decimal` requires `[Decimal(precision, scale)]` →§5.5.
| Type | Range |
|:--|:--|
| `sbyte` | −128..127 |
| `byte` | 0..255 |
| `short` | −32,768..32,767 |
| `ushort` | 0..65,535 |
| `int` | −2,147,483,648..2,147,483,647 |
| `uint` | 0..4,294,967,295 |
| `long` | −9,223,372,036,854,775,808..9,223,372,036,854,775,807 |
| `ulong` | 0..18,446,744,073,709,551,615 |
| `Int128` | −2¹²⁷..2¹²⁷ − 1 |
| `UInt128` | 0..2¹²⁸ − 1 |
| `float` | ±1.5 × 10⁻⁴⁵..±3.4 × 10³⁸ |
| `double` | ±5.0 × 10⁻³²⁴..±1.7 × 10³⁰⁸ |
- `char` = 16-bit unsigned (UTF-16 code unit).
- WIRE: `bool` = 1 bit. `bool?` = 2 bits, 3 codes: 0 false, 1 true, 2 null.
- NOTE: declared type = starting point only. `[MinMax]` narrows range →§7.2; `[A]`, `[V]`, `[X]` choose varint for skewed distribution →§7.6. Both change bits per value, not API type.
- `Int128`, `UInt128` →§5.3; UUID (`Guid`) →§5.4; `decimal` →§5.5.

## §5.3 Int128 and UInt128
KW: Int128, UInt128, System.Int128, System.UInt128, i128, u128, 16 bytes, little-endian, two's complement, x(), x_(), x___, Int128List, Int128NullList, Int128Set, Int128<V>Map, <K>Int128Map, Int128ObjectMap, ObjectInt128Map, key_, value_, bigint, (int, int), AdHoc.Int128, OverflowError
- DECL: 128-bit integers of C# (.NET 7 and later), declared as `long` is:
```csharp
class Account{
    Int128   balance;
    UInt128  id;
    UInt128? parent;   // optional: its null is a bit of the pack, as with long?
    int      version;
}
```
- WIRE: always its 16 bytes, little-endian, low 8 bytes first; two's complement for `Int128`.
- RULE: `[MinMax]`, `[A]`, `[V]`, `[X]` on such a field → agent stops: no range, no varint for it.
- NOTE: everything else as with `long`: arrays of the three forms, optional items, `Set`, keys and values of `Map`, `[D]` dimensions, arrays of collections, TYPEDEF, pack whose only field is of these types. Each item = its 16 bytes.
- GEN field per language:

| Language | Field |
|:--|:--|
| C# | `System.Int128` / `System.UInt128` |
| Rust | `i128` / `u128` |
| Go | `AdHoc.Int128` / `AdHoc.UInt128` of the library: `struct{ Hi, Lo uint64 }` with arithmetic, comparison, text, bytes and `math/big` conversion |
| C++ | `org::unirail::AdHoc::Int128` / `UInt128` of the library: `lo` and `hi` halves, `==`, `<=>`, hash, text (`to_chars` / `from_chars`) |
| C | `adhoc·Int128` / `adhoc·UInt128` of the library: `lo` and `hi` halves, functions `·eq`, `·cmp`, `·hash`, `·to_chars` |
| Java | two `long`: `x()` low 64 bits, `x_()` high; `x( hi, lo )` sets both. Operations: class `AdHoc.Int128`, `Int128.p().set( x_(), x() )` |
| TypeScript | three `number`, no `bigint`: `x` bits 0–47, `x_` bits 48–95, `x___` bits 96–127. Operations: class `Int128`, `Int128.p.set( x, x_, x___ )` |
| Swift | `Int128` / `UInt128` of Swift standard library (arithmetic, comparison, text); lib adds the hash its collections use |
| Dart | two `int`: `x` low 64 bits, `x_` high (`int?` in optional field). Operations: class `AdHoc.Int128`, `AdHoc.Int128.p.set( x_, x )` |
| Python | `int`; value out of type's range → `OverflowError` |

- RT TS: optional field has no value while `x___` is `undefined`; Dart: while `x_` is `null`.
- GEN collections: the value keeps the language's form above, no object per value; where the language has no 128-bit primitive, the library has collections for it:

| Language | `T[]` / `T[,,]` | `Set`, `Map` |
|:--|:--|:--|
| C#, Rust, Go, C++, Swift | same collections as for `long` | same collections as for `long` |
| C | `adhoc·Int128` arrays, `adhoc·Vec_i128`, `Int128List`, `Int128NullList` | `Int128Set`, `Int128<V>Map`, `<K>Int128Map` and their `Null` forms |
| Java | `long[]` of double length: item `i` is `[2i]` low, `[2i + 1]` high; `Int128List`, `Int128NullList` | `Int128Set`, `Int128<V>Map`, `<K>Int128Map`, `Int128ObjectMap`, `ObjectInt128Map` and their `Null` forms; high half of a key or value read with `key_` / `value_` |
| TypeScript | `Array<bigint>`, with nulls `Array<bigint \| undefined>` | `Set<bigint>`, `Map<bigint, V>`, `Map<K, bigint>` |
| Dart | `List<(int, int)>`, with nulls `List<(int, int)?>`; record = `( hi, lo )` | `Set<(int, int)>`, `Map<(int, int), V>`, `Map<K, (int, int)>` |
| Python | `int` items: `Array[int]` (`[D(N)] T[]`, `T[,]`), `List[int]` (`T[,,]`, nullable items) of `org.unirail.collections` | `Set[int]`, `Map[int, V]`, `Map[K, int]` of `org.unirail.collections` |

- GEN TS: field of a pack whose only field is `Int128` / `UInt128` = `bigint` too; in a pack with other fields it stays three `number`. Dart: such a field = record `(int, int)` (`(int, int)?` optional), as in collections.

## §5.4 UUID
KW: UUID, Guid, System.Guid, RFC 9562, RFC bytes, v4, v7, 8-4-4-4-12, TryWriteBytes, bigEndian, uuid::Uuid, google/uuid, AdHoc.UUID, AdHoc::Uuid, adhoc·Uuid, AdHoc.Uuid, toUUID, Guid.CompareTo, uuid.UUID, helpers.uuid
- DECL: UUID (RFC 9562) = `System.Guid`:
```csharp
class Order{
    Guid             id;
    Guid?            parent;
    [D(+100)] Set<Guid> tags;
}
```
- WIRE: exactly a `UInt128` →§5.3: 16 bytes, little-endian of the 128-bit number whose big-endian bytes are the RFC bytes → first wire byte = RFC byte 15.
- RULE: `[MinMax]`, `[A]`, `[V]`, `[X]` on such a field → agent stops.
- NOTE: all other forms as with `UInt128`: arrays, optional items, `Set`, keys and values of `Map`, `[D]` dimensions, TYPEDEF, pack of one field.
- RT order (where a language orders UUIDs): unsigned 128-bit order of the RFC bytes (= order of v7 UUIDs by time), NOT C# `Guid.CompareTo`. Text: RFC form, 36 characters `8-4-4-4-12`, lowercase hex.

| Language | Field |
|:--|:--|
| C# | `System.Guid`; to and from RFC bytes: `TryWriteBytes( span, bigEndian: true )` / `new Guid( span, bigEndian: true )` |
| Rust | `u128` in RFC order (as `uuid::Uuid::from_u128` / `as_u128`); text, bytes, v4, v7 in library module `uuid` |
| Go | `AdHoc.UUID` = `[16]byte` in RFC order, as `uuid.UUID` of google/uuid; text, parsing, `Cmp`, v4, v7 in the library |
| C++ | `org::unirail::AdHoc::Uuid` of the library: `hi` (RFC bytes 0-7) and `lo`, `==`, `<=>`, hash, `to_chars` / `from_chars`, `bytes()` |
| C | `adhoc·Uuid` = `adhoc·UInt128` layout, `hi` = RFC bytes 0-7; `adhoc·Uuid·to_chars` / `from_chars`, `to_bytes` / `from_bytes`, `version` |
| Java | as `UInt128`: `x_()` RFC bytes 0-7, `x()` bytes 8-15; operations: class `AdHoc.Uuid`, `Uuid.p().set( x_(), x() )`, `toUUID()` |
| TypeScript | as `UInt128`: three `number` in a pack of several fields, `bigint` in collections and in a pack of one field; operations: class `Uuid` |
| Swift | `UInt128` in RFC order (as Rust); text (`to_chars` / `from_chars`, `Text`), bytes, `version`, v4, v7 in `org.unirail.AdHoc.helpers.uuid` |
| Dart | as `UInt128`: `x_` RFC bytes 0-7, `x` bytes 8-15 in a pack of several fields; record `( hi, lo )` in collections and in a pack of one field; operations: class `AdHoc.Uuid`, `AdHoc.Uuid.p.set( x_, x )` |
| Python | `uuid.UUID`; its `.int` = the wire `UInt128`, whose big-endian bytes are the RFC bytes |

## §5.5 Decimal
KW: decimal, [Decimal(precision, scale)], precision, scale, units, Decimal_<precision>_<scale>, Decimal_5_2, Money, PRECISION, SCALE, MIN, MAX, RoundingMode, UP, DOWN, CEILING, FLOOR, HALF_UP, HALF_DOWN, HALF_EVEN, UNNECESSARY, Rescale, OverflowException, ArithmeticException, RangeError, org.unirail.Decimal, helpers.decimal.Decimal, checked_add, extension type, OverflowError
- DECL: exact decimal = `decimal` with `[Decimal(precision, scale)]`: `precision` significant digits 1..38, `scale` of them after the point 0..`precision`. RULE: attribute required: `decimal` without it → agent stops.
```csharp
class Money { [Decimal(19, 4)] decimal TYPEDEF; }   // a decimal type of its own, under its name

class Invoice{
    [Decimal(5, 2)]  decimal  rate;      // -999.99 … 999.99
    [Decimal(5, 2)]  decimal? discount;
    Money                     total;
    [D(+50), Decimal(9, 2)]   Set<decimal> prices;
    [D(+50), Decimal(10, 2)][Val: Decimal(30, 8)] Map<decimal, decimal> rates;
}
```
- WIRE: integer of units of 10^-scale (19.99 at scale 2 = 1999). Range ±(10^precision − 1) units; it alone sets width: precision ≤2 → 1 byte, ≤4 → 2, ≤9 → 4, ≤18 → 8, 19..38 → 16 (as `Int128`).
- RULE: `[MinMax]` and varint `[A]`, `[V]`, `[X]` not allowed on a decimal; to narrow the range lower the precision.
- NOTE: all other forms as with integers: optional fields, arrays, `Set`, keys and values of `Map`, `[D]` dimensions, pack of one field.
- GEN: each decimal definition = a type of its own: TYPEDEF under its name (`Money`); one type per (precision, scale) of fields without alias, named `Decimal_<precision>_<scale>` (`Decimal_5_2`). Type holds the units; constants `PRECISION`, `SCALE`, `MIN`, `MAX` (in units), text, parsing.
- RT text: exactly `SCALE` digits after the point (`-0.05`, `12.30`; no point at scale 0). Parsing exact: zeros beyond scale accepted (`1.230` at scale 2 = 123 units); non-zero digit beyond scale or value out of range → error.
- RULE: value made from units explicitly (`new Money( 50000 )`, `Money( 50000 )`); `Money m = 5;` does not compile in C# and C++ → 5 units never taken for 5.0000 by mistake.

| Language | Type |
|:--|:--|
| C# | `readonly struct Money` with `Units` (`sbyte` … `long` by range, `Int128` above 18 digits), `ToString`, `TryFormat( Span<char> )`, `Parse` / `TryParse`; up to 28 digits: conversion to and from `System.Decimal` |
| Java | units field (`byte` … `long`, two `long` above 18 digits as `Int128`) marked TYPE_USE `@Money`; annotation carries constants and `Money.Units.str` / `chars` / `parse` over `AdHoc.Decimal` and `AdHoc.Int128` |
| TypeScript | `type Money = number` up to 15 digits, `bigint` wider; namespace `Money` with constants, `toString`, `toAscii`, `parse` over class `Decimal` |
| Rust | `#[repr(transparent)] pub struct Money(pub i64)` (`i8` … `i64` by range, `i128` above 18 digits) with `Display`, `FromStr`, `to_chars`, `from_chars` |
| Go | `type Money int64` (`AdHoc.Int128` above 18 digits) with `String`, `AppendText`, `ParseMoney`; usable as `map` key |
| C++ | `struct Money final` with `units`, `==`, `<=>`, hash, `to_chars` / `from_chars`, `ToString`, `Parse` |
| C | `typedef int64_t <prefix>Money` (`adhoc·Int128` above 18 digits) with constants and `·to_chars` / `·from_chars` |
| Swift | `public struct Money` in host's namespace with `units` (`Int8` … `Int64` by range, `Int128` above 18 digits), constants, `to_chars` / `from_chars`, `description`, `init?( String )`, `parse` (`throws( ParseError )`); conforms to `org.unirail.AdHoc.helpers.decimal.Decimal` |
| Dart | `extension type const Money( int v ) implements int` up to 18 digits, `extension type const Money( (int, int) v )` above (record `( hi, lo )`); field holds units; static constants, `toString_`, `toAscii`, `parse` (`parse_` over `AdHoc.Int128` above 18 digits) |
| Python | `class Money( org.unirail.Decimal )`, immutable, over `units` (Python `int`, checked against ±(10^precision − 1)); `PRECISION`, `SCALE`, `MIN`, `MAX`, exact `parse`, `str` with `SCALE` digits after point, `==`, hash, order within the type; pack keeps the units, getter makes the object |

- GEN collections: item = decimal type where collections are generic (C#, Rust, Go, C++, Swift, Python, TypeScript `Set` and `Map`); in Java, C, Dart and TypeScript typed arrays item = integer of units (Dart: record `( hi, lo )` above 18 digits). Test and interlanguage dump compare units.
- RT arithmetic: exact, same in every language. Add, subtract, negate, absolute value: in one scale. Multiply and divide by another decimal (taken in its own scale, result in receiver's scale), by an integer or by 10^n, change of scale, conversion to integer and from `double`: explicit rounding mode = the 8 modes of `java.math.RoundingMode`: `UP`, `DOWN`, `CEILING`, `FLOOR`, `HALF_UP`, `HALF_DOWN`, `HALF_EVEN`, `UNNECESSARY` (error when rounding is needed). Intermediate value exact (128 bits for 64-bit units, 256 for 128-bit). RULE: overflow or result outside type's ±(10^precision − 1) → error, never silent wrap.
- GEN: pack of one decimal field has accessor of the decimal type in C#, Go, C++, Rust, Swift (pack keeps units in `value`; field `v` → `v()` / `set_v( … )` give and take the decimal), Python.

| Language | Arithmetic |
|:--|:--|
| C# | operators `+ - unary-` with range check, `Multiply` / `Divide( other, AdHoc.RoundingMode )`, `Rescale`, `ToLong`, `FromDouble`; errors `OverflowException`, `ArithmeticException` |
| Java | `Money.Units.add` / `multiply( a, b, RoundingMode )` / `divide` … with range check, over `AdHoc.Decimal` (`…128` methods above 18 digits) |
| TypeScript | functions of type's namespace: `Money.add`, `multiply( a, b, mode )`, `divide`, `rescale`, `toInteger`; `RangeError` on overflow |
| Rust | trait `Decimal`: `checked_add`, `checked_mul( o, RoundingMode )`, `checked_div`, `rescale`, `convert` → `Option`; `+ -` panic on overflow |
| Go | methods `Add`, `Sub`, `Mul( o, mode )`, `Div( o, mode )`, `Rescale`, `ToLong` → `( T, error )` |
| C++ | operators `+ - += -=`, `mul` / `div( o, RoundingMode )`, `rescale`, `to_long`; `std::overflow_error`, `std::domain_error` |
| C | functions `…Money·add( a, b, &out )`, `·mul( a, b, mode, &out )`, `·div`, `·rescale` → `bool` |
| Swift | protocol `org.unirail.AdHoc.helpers.decimal.Decimal`: `checked_add`, `checked_sub`, `checked_neg`, `checked_mul( o, mode )`, `checked_div( o, mode )`, `checked_mul_int`, `checked_div_int`, `checked_mul_pow10`, `checked_div_pow10`, `rescale`, `convert`, `to_i64`, `from_f64` → `nil` on overflow; operators `+ - <`, unary `-`; `+ -` trap on overflow; modes `.Up` … `.HalfEven`, `.Unnecessary` |
| Dart | static functions of the type over units: `Money.add`, `subtract`, `multiply( a, b, mode )`, `divide`, `multiplyDecimal`, `rescale`, `toInteger`, `fromNumber` …; above 18 digits `add_`, `multiply_` … over `AdHoc.Int128`; `AdHoc.RoundingMode`; `RangeError` |
| Python | operators `+ - unary- abs` (`OverflowError`), `multiply` / `divide( other, mode )` (`other` = decimal of any type, or `int` of units with `scale =`), `scale_by_power_of_10`, `rescale`, `to_int`, `from_float`, `from_decimal` / `to_decimal` (`decimal.Decimal`); `org.unirail.RoundingMode` required wherever result may be rounded; `UNNECESSARY` that must round → `ArithmeticError` |

## §5.6 longJS and ulongJS
KW: longJS, ulongJS, BigInt, 2^53, TypeScript number, MinMax, TYPEDEF, org.unirail.Meta
- TS `number` holds ints exactly only in −(2⁵³−1)..2⁵³−1; 64-bit field outside → `BigInt` in TS (slower). Use `longJS`/`ulongJS` instead of `long`/`ulong` when TS host sends/receives 64-bit field whose values fit safe range.
- DECL: both declared in `org.unirail.Meta` as TYPEDEFs (→§5.18) over `long` with range:
```csharp
public class longJS {  [MinMax(-0x10000000000000, 0xFFFFFFFFFFFFE)] long TYPEDEF; }   // −2⁵² … 2⁵² − 2
public class ulongJS { [MinMax(0, 0x1FFFFFFFFFFFFE)]                long TYPEDEF; }   // 0 … 2⁵³ − 2
```
- RULE: each range = 2⁵³ − 1 values + 1 code for null → code `value - MIN`, null included, fits 53 bits → `longJS?`, `ulongJS?`, arrays, `Map`, `Set` of them are `number` too. Own range keeps this while its codes with null stay within 2⁵³: `[A(0, 0x1FFFFFFFFFFFFE)] long?` → `number`; `[A(0, 0x1FFFFFFFFFFFFF)] long?` → `BigInt`.
- RULE: the guarantee is about the stored value: TS host keeps inT (→§7.1) of a `longJS`/`ulongJS` field in a `number`, never `BigInt`. exT follows: no offset → getter/setter take and give that same `number`; stored with an offset → they give stored + offset, which may leave the safe range and be `BigInt`. Plain `long`/`ulong` with range beyond safe → stored as `BigInt`.
- WIRE: being ranges, narrowed on wire like any `[MinMax]` field →§7.2.
- DECL: usable wherever a type may stand:
```csharp
class Ledger
{
    longJS                   balance;
    ulongJS?                 last_tx;             // optional
    [D(10)] longJS[]         recent_balances;     // exactly 10 items
    [D(10)] ulongJS?[,,]     sizes;               // up to 10 nullable items
    Map<longJS, ulongJS?>    totals;
}
```

## §5.7 Optional fields
KW: optional, ?, nullable, _hasValue, _to_null, NULL, null items, null key
- `optional` field = may hold no value.
- DECL: value type optional by trailing `?` (`int?`, `byte?`, `MyEnum?`). Reference types (string, pack, array, Map, Set) standing directly in pack = always optional, no `?` needed (`?` allowed on pack field, changes nothing). `?` on item type inside collection → items optional.
```csharp
class Packet
{
    string    user;            // optional: reference types always are
    string[]  tags;            // the field is optional; its items are not
    string?[] emails;          // the field and its items are optional
    uint?     optional_field;
}
public class QuitRoomResponse { MID? mid; [X] int? result; [X] int? rid; }   // optional enum, optional varints
public class RoomChangeResponse { MID? mid; RoomInfo? roomInfo; }            // ? on pack field changes nothing
```
| Declaration | field may be absent | items may be null |
|:--|:--|:--|
| `int count;` | no | - |
| `int? count;` | yes | - |
| `string name;`, `Point origin;` | yes | - |
| `int[] ids;`, `Set<int> ids;` | yes | no |
| `int?[] ids;`, `Set<int?> ids;` | yes | yes |
| `Map<string?, int?> scores;` | yes | one null key, and null values |
- GEN: reference-typed field → `null` when absent. Optional primitive keeps plain type in API + 2 extra members. C#, `[X] long? event_id;` in pack `PlayerEvent`:
```csharp
public long event_id { get; set; } = PlayerEvent.event_id__.NULL;            // the value
public bool event_id_hasValue => event_id != PlayerEvent.event_id__.NULL;    // is there a value?
public void event_id_to_null() => event_id = PlayerEvent.event_id__.NULL;    // make it absent
```
- GEN: `event_id__.NULL` = code of value's type reserved by field for "absent"; test with `_hasValue`, not by comparing values.
- WIRE: absent field costs 1 bit, or one spare code of a bit field →§7.5, §7.3.

## §5.8 Strings
KW: string, [D(+N)], STR_LEN_MAX, 255 chars, UTF-16 code units, _DefaultMaxLengthOf.Strings, TYPEDEF, trim
- `string` = immutable char sequence.
- DEFAULT: max 255 chars. `[D(+N)]` = limit of one field; `_DefaultMaxLengthOf.Strings` = project default →§5.11.
```csharp
class Packet
{
    string             string_field_with_max_255_chars;
    [D(+6)]    string  string_field_with_max_6_chars;
    [D(+7000)] string  string_field_with_max_7000_chars;
}
```
- RULE: limit counts UTF-16 code units (unit of C#, Java, JS strings); char outside BMP (e.g. emoji) counts 2.
- DECL: string type used in many fields → declare once as TYPEDEF (→§5.18): `class max_6_chars_string { [D(+6)] string TYPEDEF; }` then `max_6_chars_string string_field_with_max_6_chars;`.
- GEN: nullable string property + limit constant. C#:
```csharp
public string? objective { get; set; } = null;
public partial struct objective__ { public const int STR_LEN_MAX = 100; }
```
- NOTE: encoding →§7.8. String one side must receive as raw UTF-8 bytes (store/relay unparsed) = trimmed field →§17 (§17.6).

## §5.9 Arrays: constant, fixed and dynamic length
KW: T[], T[,], T[,,], constant length, fixed at creation, dynamic list, [D(N)], ARRAY_LEN, ARRAY_LEN_MAX, _new, _len, NullablePrimitiveList, BitsList
- DECL: brackets choose form; commas are not formatting, they decide form.
| Decl | Form | Length of a value | Length on wire |
|:--|:--|:--|:--|
| `T[]` | constant length | exactly declared length | never sent |
| `T[,]` | fixed at creation | chosen when field created, ≤ declared; one length for the field | sent once |
| `T[,,]` | dynamic (list) | each value own length, ≤ declared | sent with every value |
- DEFAULT: `[D(N)]` sets declared length; without it 255 (`_DefaultMaxLengthOf.Arrays`). String items keep own limit, `+N` in same attribute.
```csharp
class Dim_forms
{
    [D(10)] int[]     arr10_ints;     // exactly 10 ints; no length on the wire
    [D(10)] int[,]    fix10_ints;     // up to 10, fixed when the field is created
    [D(10)] int[,,]   list10_ints;    // up to 10, every value its own length
    [D(10)] int?[]    arr10_ints_Q;   // 10 optional ints
    string?[]               strs;           // 255 strings of at most 255 chars
    [D(100, +10)] string?[] arr100_strs10;  // 100 strings of at most 10 chars each
}
```
- PITFALL: length = item count for every collection, but written differently: `[D(300)] int[] reply;` = exactly 300 ints; `[D(+300)] Set<int> reply;` = up to 300 distinct ints.
- GEN (C#):
| Form | Field type | Create | Length | Assigning other length |
|:--|:--|:--|:--|:--|
| `T[]` | `int[]` | `arr10_ints_new()` | `arr10_ints_len` = `arr10_ints__.ARRAY_LEN` | cut or zero-padded to `ARRAY_LEN` |
| `T[,]` | `int[]` | `fix10_ints_new(int size)` | `fix10_ints_len` | cut to `fix10_ints__.ARRAY_LEN_MAX` |
| `T[,,]` | `List<int>` | `list10_ints_new(int size)` | `list10_ints_len()` | cut to `list10_ints__.ARRAY_LEN_MAX` |
- GEN: nullable items → runtime-lib collections, e.g. C# `NullablePrimitiveList<int>.RW`; items narrowed by `[MinMax]` packed into bits → `BitsList<byte>.RW`. Assignment goes through field setter (`fix10_ints(int[] src)`), which fits value to declared length per last column.

### §5.9.1 `[,]` or `[,,]`: whose length is it
KW: [,], [,,], field length, per-value length, Map value array, Set item array, multimap
- `[,]` length belongs to the field: chosen once; every value of field has exactly that length. `[,,]` length belongs to each value, travels with it.
- On plain pack field they look alike (one array, one length). Differ inside a collection: Map key/value, Set item, element of another array.
- WIRE: `T[]` = items, no length; `T[,]` inside collection = one length written once for whole field, then values of equal length; `T[,,]` inside collection = each value preceded by own length.
| Declaration | Lengths |
|:--|:--|
| `Map<string, string[,,]> tags` | every key has list of own length |
| `Map<string, string[,]> tags` | every list in map has the same length, chosen when map created |
| `string[][,,] rows` | constant number of rows, each row own length |
| `string[][,] rows` | constant number of rows, all same length |
- Outer/inner bracket order →§5.15.
- PITFALL: `[,]` inside collection only for values truly sharing one length (matrix row, fixed-size hash). Value of other length NOT rejected: generated code fits it to field's length. Multimap, tag list, anything of differing lengths = `[,,]`.

## §5.10 The `[D]` attribute
KW: [D], D(N), D(+N), D(-N), D(~N), dimensions, length, does not have an array declaration, exit code 2
- DECL: `[D(...)]` declares every length/dimension of a field. Args = numbers, `const` names or `[ValueFor]` constants (→§6.1, §6.2). Prefix says what each number describes:
| Arg | Meaning | Valid on |
|:--|:--|:--|
| `N` | array length: exactly N for `[]`, ≤N for `[,]`/`[,,]`. On array of collections (→§5.15): inner array length when item (or Map/Set key) is an array, else outer array length | field with `[]`, `[,]` or `[,,]` |
| `+N` | intrinsic length: ≤N chars of `string`, ≤N items of `Set`/`Map` | `string`, `Set`, `Map`, also as array items |
| `-N` | dimension of constant length N | multidimensional array →§5.14 |
| `~N` | dimension ≤N, fixed when field created | multidimensional array →§5.14 |
- RULE checks:
| Declaration | Result |
|:--|:--|
| `[D()]` no args | error, exit code 2 |
| `N` on field without `[]`/`[,]`/`[,,]` | error: "… does not have an array declaration such as '[]', '[,]', or '[,,]'" |
| `+N` on non-`string`/`Set`/`Map` | error |
| one `-N` or `~N` on an array | accepted as outer array length; prefix ignored, brackets decide form. `[D(-3)] float[,]` = fixed-at-creation array ≤3 |
| one `-N`/`~N` on `Map`/`Set` without brackets | error |
| two or more `-N`/`~N` | multidimensional array |
| `-N`/`~N` inside `[Key: D(...)]`/`[Val: D(...)]` | error: keys and values cannot be multidimensional |
| `[D]` on field of a pack header | error: header has fixed size →§8.6 |
- PITFALL: on plain array write length as bare number, let brackets choose form; single `-N`/`~N` looks like a form but does not choose one.

## §5.11 Collection limits: `_DefaultMaxLengthOf`
KW: _DefaultMaxLengthOf, Arrays, Maps, Sets, Strings, Uncompressed, 255, 1024, default limit
- DEFAULT: every array/map/set/string without explicit length limited to 255 items (chars for string).
- DECL: change project-wide by enum named `_DefaultMaxLengthOf` with members to change:
```csharp
enum _DefaultMaxLengthOf { Arrays = 255, Maps = 255, Sets = 255, Strings = 255, }
```
| Member | Default | Applies to |
|:--|:--|:--|
| `Arrays` | 255 | arrays without `N` |
| `Maps` | 255 | maps without `+N` |
| `Sets` | 255 | sets without `+N` |
| `Strings` | 255 | strings without `+N` |
| `Uncompressed` | 1024 | pack that can never exceed this many bytes loses compression stage of its chain; 0 keeps compression on every pack →§16 (§16.8) |
- RULE: omitted member keeps default. Member with other name ignored silently (no message).
- RULE: imported project may declare own `_DefaultMaxLengthOf`; imported settings apply first, root project last → root wins →§22.
- NOTE: configures agent only; not in generated code.

## §5.12 Map and Set
KW: Map<K,V>, Set<K>, [D(+N)], Dictionary, HashSet, TYPE_LEN_MAX, _new, null key, Set<bool>, not a type AdHoc knows, exit code 23
- DECL: `Map<K, V>`, `Set<K>` from `org.unirail.Meta`. DEFAULT ≤255 items; `[D(+N)]` = limit of one field.
```csharp
using org.unirail.Meta;
class Inventory
{
    [D(+20)] Set<uint>          max_20_uints_set;
    [D(+20)] Map<Point, uint>   map_of_max_20_items;
}
```
- DECL: keys/items/values may be primitives, strings, enums, packs, `DateTime`, `DateTimeDef` types, arrays. `?` on key type → one null key allowed; `?` on value/item type → null values.
```csharp
class Collections
{
    Set<byte?>           bytes_or_null;     // items may be null
    Set<Point3>          points;            // a pack as the item
    Set<int[]>           arrays;            // an array as the item
    Map<string?, int>    by_name;           // one null key allowed
    Map<Point2, Point3>  pack_to_pack;
    Map<Mode?, Mode?>    enum_to_enum;      // enums, nullable on both sides
    Map<DateTime, int>   by_time;
    Map<int, bool>       flags;             // bool as the VALUE is fine
}
```
- NOTE: array as key/value usually `[,,]` (each own length); `[,]` gives every key (or value) the same length →§5.9.1.
- RULE refused keys:
| Declaration | Result |
|:--|:--|
| `Set<bool>`, `Set<bool?>` | error: "The field `…` (…) is a Set of `bool`: it can hold only true and false. Use two bool fields, or a [Flags] enum." |
| `Map<bool, V>` | error: "The field `…` (…) is a Map keyed by `bool`: it can hold two entries only. Use two fields, one for each key." |
| `Map` key = empty pack | error; empty pack carries only presence →§4.5 |
| `Set` item = empty pack | not supported (like `Set<bool>`); agent does not stop: replaces item type with `bool` + warns. Do not declare |
| `Map`/`Set` inside `Map`/`Set` | not supported: "The field … (…) has the type …, which is not a type AdHoc knows" (exit code 23). Use a pack as value |
- GEN (C#): map = `Dictionary<K, V>`, set = `HashSet<K>`; creation method, add method, limits as constants:
```csharp
public Dictionary<string, int>? _event_data;
public Dictionary<string, int>? event_data_new(int _items) { ... }
public bool event_data(string key, int value) { ... }   // true when the key is new
public interface event_data__
{
    public const int TYPE_LEN_MAX = 50;                    // [D(+50)]
    public struct Key { public const int STR_LEN_MAX = 100; }
}
```

## §5.13 `[Key:]` and `[Val:]` attribute targets
KW: [Key:], [Val:], attribute target, Key D(+N), Val D(N), X, V, Map value, Set item, exit code 2
- DECL: attribute on `Map`/`Set` field applies to collection itself. C# attribute target: `[Key: ...]` = key of `Map` or item of `Set`; `[Val: ...]` = value of `Map`.
```csharp
[Key: D(+30)]               // each string key: at most 30 characters
[Val: D(100), X]            // each value: a list of at most 100 ints, ZigZag varint
Map<string, int[,,]> MAP;

[D(+70)]                    // the set: at most 70 items
[Key: D(30)]                // each item: a list of at most 30 doubles
Set<double[,,]> SET;
```
| Rule | Otherwise |
|:--|:--|
| `[Val:]` only on `Map` field | error, exit code 2 |
| `[Key:]` only on `Map`/`Set` field | error, exit code 2 |
| `[D]` in a target uses `N` and `+N` only | error: keys and values cannot be multidimensional |
| `[Key: D(+N)]` only on string key/item | error: `[Key: D(+30)] Set<double[,,]>` refused; list key takes `[Key: D(30)]` |
| `[Val: D(+N)]` accepted only when the KEY is a string too | error on `Map<int, string>`: agent checks key type, not value type. Limit string values with TYPEDEF |
- DECL: varint attributes go to elements same way (→§7.6):
```csharp
class Counters
{
    [Val: X]          Map<uint, int>           deltas;      // ZigZag varint values
    [Key: V] [Val: V] Map<uint, uint>          both;        // varint keys and values
    [Key: X]          Set<int>                 ids;         // varint items
    [D(-3, ~3, +200)] [Val: D(100), X] Map<Str30, int[,]>[] grids;   // 3 × (≤3) maps of up to 200 items; each value
                                                                       // an array of up to 100 varint ints, one length per map
}                                                                      // (Str30 is a TYPEDEF, §5.18)
```
- PITFALL: write `[Val: ...]` list after field's other attribute lists: agent applies every attribute list following a `[Val: ...]` list to the value as well.
- PITFALL: to limit string VALUES use string TYPEDEF value (`Map<int, max_100_chars_string>`), not `[Val: D(+N)]`.
- DECL: complex key/value types used in many fields → TYPEDEFs:
```csharp
class string_max_30_chars   { [D(+30)] string TYPEDEF; }
class list_of_max_100_ints  { [D(100), X] int[,,] TYPEDEF; }
class Lookup { Map<string_max_30_chars, list_of_max_100_ints>[,,] MAP; }   // a list of such maps
```

## §5.14 Multidimensional arrays
KW: multidimensional, D(-N), D(~N), dimensions, 32 dimensions, cells, grid
- `multidimensional array` = field with ≥2 dimensions, each constant length or fixed at creation.
- DECL: ≥2 prefixed `[D]` args, one per dimension; field needs no brackets. `-N` = constant length N; `~N` = ≤N, fixed when field created.
```csharp
class Pack
{
    [D(-2, -3, -4)] int     ints;                         // 2 × 3 × 4 ints
    [D(-2, ~3, ~4)] Point   points;                       // 2 × (≤3) × (≤4) points
    [D(-2, -3, -4)] string  strings_with_max_255_chars;   // 2 × 3 × 4 strings
    [D(-5, -12, +40)] string? labels;                     // 5 × 12 nullable strings of at most 40 chars
}
```
- PITFALL: `[D(2, 3)]` is NOT 2 × 3: unprefixed numbers are array lengths and need brackets.
- RULE: max 32 dimensions per field.
- GEN: agent passes dimensions to generator in declaration order, each with kind: constant (`-N`) or fixed at creation (`~N`). `-N` dim always exactly N cells; `~N` dim gets length ≤N when code creates field, keeps it. Each cell reached with one index per dimension. Member names: copy from generated code.
- DECL cells that are collections (brackets make each cell a collection):
  - one bracket pair → each cell is that array: `[D(-7, -34)] string?[] blocks;` = 7 × 34 array, cells = arrays of 255 nullable strings.
  - two bracket pairs → dimensions replace outer pair, cell = inner array: `[D(-5, ~12, 6)] string[,][] grid;` = 5 × (≤12), cells = arrays of exactly 6 strings.
  - Map/Set with brackets → multidimensional array of maps/sets; dimensions replace brackets.
```csharp
class Grids
{
    [D(-3, ~3)]       Map<int[], byte?>[]  maps;          // 3 × (≤3) maps, each of at most 255 items
    [D(+100, -3, ~3)] Map<int, string>[]   maps_100;      // 3 × (≤3) maps, each of at most 100 items
    [D(-3, ~69, +10)] [Key: D(+100)] Set<string?>[] sets; // 3 × (≤69) sets of at most 10 strings of at most 100 chars
}
```

## §5.15 Arrays of collections
KW: array of collections, jagged, string[][,,], outer array, inner array, element type, ?[], nullability levels
- `array of collections` = array whose items are arrays, maps or sets; declared with second bracket pair. AdHoc reads type exactly as C#:
  1. Without `?` between pairs, LEFT pair = outer array. `string[][,,]` = array (`[]`) of lists (`[,,]`) of strings (C# jagged rule).
  2. `?` between pairs closes element type. `string?[]?[,,]`: element = nullable array `string?[]?`; pair after `?` = outer: list (`[,,]`) of nullable constant-length arrays.
- DECL `[D]` args: bare `N` = element (inner) array length; single `-N`/`~N` = outer array length (form from brackets); `+N` = length of strings or of maps/sets. E.g. `[D(-100, 20, +50)] string?[][,,] a;` = outer exactly 100, each inner list ≤20, strings ≤50 chars, strings nullable.
| Declaration | Outer | Elements |
|:--|:--|:--|
| `[D(-100)] string?[][,,] a;` | `[]`: exactly 100 | `[,,]`: lists of ≤255 nullable strings |
| `[D(+50, -100)] string?[,,][,] b;` | `[,,]`: ≤100 | `[,]`: arrays of one length chosen at creation; strings ≤50 chars |
| `[D(~11, 12, +13)] string?[,][] c;` | `[,]`: ≤11, fixed at creation | `[]`: arrays of exactly 12 nullable strings ≤13 chars |
| `string?[]?[,,] d;` | `[,,]`: ≤255 | `[]?`: nullable arrays of exactly 255 nullable strings |
| `Map<int[,,], int[,,]>[,] e;` | `[,]`: ≤255, fixed at creation | maps |
| `[D(3)] Set<Item>[] f;` | `[]`: exactly 3 | sets of ≤255 `Item` packs |
| `Map<int[,,]?, byte[,]>?[]? g;` | `[]`: exactly 255 | nullable maps; keys nullable int lists, values byte arrays of one length |
- NOTE: nullability independent at every level: field (always optional), inner collection (`Set<int>?[]`, `T[]?[,,]`), items (`T?`), map keys/values (`Map<K?, V?>`).
- GEN: outer array and inner collections keep their forms (for `b`: outer `[,,]` ≤100 elements, each element array with one length chosen at field creation, strings ≤50 chars). Limits of every level = constants of field as for plain array (`ARRAY_LEN_MAX`, `STR_LEN_MAX`, ...). Copy member names from generated code.

## §5.16 Packs and enums as field types
KW: pack field, enum field, nested pack, recursive pack, nested_max, cycle, empty pack as bool, reserved word
- Pack-typed field carries whole instance inside parent; enum-typed field carries one of its constants. Pack fields always optional; enum field optional with `?`.
```csharp
public interface MyProject3
{
    /// <see cref="InJAVA"/>
    struct Server : Host { public class QuitRoomResponse { MID? mid; [X] int? result; } }   // optional enum
    public class RoomInfo { [X] long id; [X] int? rank; }
    enum RoomType { CLASSICS = 1, ARENA = 2, }
    enum MID { ServerRegisterReq = 1001, ServerRegisterRes = 1002, ServerListReq = 1003, ServerListRes = 1004, }
    /// <see cref="InTS"/>
    struct Client : Host
    {
        public class RoomChangeResponse
        {
            MID?      mid;
            RoomInfo? roomInfo;     // a pack declared at project level
            public class EnterRoomRequest { MID? mid; RoomType tYpe; [X] int rank; }   // `type` is a reserved word in target languages
        }
    }
    // @MyProject3: every pack of the project, incl. those inside hosts and other packs (→§9)
    interface MainConnection : Connects<Server, Client>{ [_____lr_____<@MyProject3>] struct Start { } }
}
```
- DECL recursive packs: pack may refer to itself directly or via other packs; tree = pack with collection of itself:
```csharp
class Folder
{
    string                    name;
    [D(65535)] Folder[,,]     folders;   // subfolders: the pack refers to itself
    [D(65535)] FileEntry[,,]  files;
}
class FileEntry { string name; ulong size; [D(32)] Binary[] sha256; }
```
- RULE: cycles through several packs allowed (`A` has `B`, `B` has `A`). Agent detects every cycle, records per recursive pack nesting bound derived from cycle (`nested_max`) → generator. Non-recursive pack: nesting depth fixed by description.
- NOTE: enum used by one pack may be declared inside that pack →§4.3.
- RULE enum as field type: needs ≥2 constants; enum whose constants all have one value → `bool`; `struct`/`interface` constants container is not a field type →§6.3, §6.4.2.
- RULE: class without instance fields as field type → `bool`, whether a branch sends it (empty pack) or not (constants container) →§4.5.1.
- Types combine freely:
```csharp
class max_100_chars_string { [D(+100)] string TYPEDEF; }
class TeamCoordination
{
    public uint                        team_id;
    [D(-3)]   float[,]                 waypoints;   // up to 3, fixed when created
    max_100_chars_string               objective;
    [D(+10)]  Set<max_100_chars_string> members;    // up to 10 names
    [D(100)]  Binary[,]                raw_data;    // up to 100 bytes
    [D(+50)]  Map<max_100_chars_string, int> scores;
}
```

## §5.17 Binary
KW: Binary, Binary[], raw bytes, byte[], ByteList.RW, Uint8List.RW, Uint8Array, Vec<u8>, []uint8, std::vector, adhoc·Vec_u8, InlineArray, Uint8List_RW, abstract pack window, _data_new, _data_done, _data_len, Data_Get, Data_Set
- `Binary` (`org.unirail.Meta`) = raw byte; array of `Binary` = bytes carried uninterpreted. Exists so bytes get natural type per language (signed `byte` Java, unsigned `byte` C#).
| Lang | `[D(N)] Binary[]` | `Binary[,]` | `Binary[,,]` |
|:--|:--|:--|:--|
| C# | `byte[]` | `byte[]` | `List<byte>` |
| Java | `byte[]` (signed) | `byte[]` (signed) | `ByteList.RW` |
| TS | `Uint8Array` | `Uint8Array` | `Uint8List.RW` |
| Rust | `[u8; N]` | `Vec<u8>` | `Vec<u8>` |
| Go | `[]uint8` | `[]uint8` | `[]uint8` |
| C++ | `std::array< std::uint8_t, N >` | `std::vector< std::uint8_t >` | `std::vector< std::uint8_t >` |
| C | `uint8_t [N]` | `adhoc·Vec_u8` | `adhoc·collections·UByteList` |
| Swift | `InlineArray< N, UInt8 >` | `[UInt8]` | `[UInt8]` |
| Dart | `Uint8List` | `Uint8List` | `Uint8List_RW` |
| Python | `Array[int]`, set from `bytes`, `bytearray`, `memoryview` or any buffer | as `[D(N)] Binary[]` | `List[int]` |
- GEN: absent array: Rust `Option<…>`, C++ `std::optional< … >`, Swift optional (`nil`), C `bool` flag `<field>_some`, Python `None`, others `null` (Go `nil`). Python `Array` / `List` (`org.unirail.collections`) = views of the field, read in place.
- NOTE: fits bytes whose length is part of the format (hash, signature, fixed-size tag). WIRE: constant-length array sends no length; value = array.
```csharp
class Result
{
    [D(32)] Binary[] sha256;   // exactly 32 bytes, no length prefix
    [D(4)]  Binary[] tag;      // exactly 4 bytes
}
```
- NOTE: bytes whose length is property of value (blob, serialized object, file) → usually `File`/`Stream` field (moves bytes socket buffer ↔ channel, no array held). Choosing Binary/File/Stream →§15.6.
- RT/GEN: `Binary` field of abstract pack (host implements pack itself, →§10.3.2): no array to fill; C#, Java, TS, Go, C++, Dart: pack gets window of socket buffer = bytes of field this buffer portion holds (receiving) or has room for (sending), starting at element `item`. Code takes/fills window as it likes (array, other buffer, file). Called once per portion; afterwards codec moves past window regardless. Field `data`:
| Lang | Receiving | Sending |
|:--|:--|:--|
| C# | `void _data(Base_.Receiver ctx, Base_.Receiver.Slot __slot, int item, ReadOnlySpan<byte> src)` | `void _data(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item, Span<byte> dst)` |
| Java | `void _data(Base_.Receiver ctx, Base_.Receiver.Slot __slot, int item, java.nio.ByteBuffer src)` | `void _data(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item, java.nio.ByteBuffer dst)` |
| TS | `_dataˎ(ctx: Base_.Receiver, __slot: Base_.Receiver.Slot, item: number, src: Uint8Array): void` | `_data̍(ctx: Base_.Transmitter, __slot: Base_.Transmitter.Slot, item: number, dst: Uint8Array): void` |
| Go | `Data_Set(ctx *Receiver_.Receiver, __slot *Receiver_.Slot, item int, src []byte)` | `Data_Get(ctx *Transmitter_.Transmitter, __slot *Transmitter_.Slot, item int, dst []byte)` |
| C++ | `void _data(rx::Rx& _src, std::int32_t item, org::unirail::AdHoc::Bytes src)` | `void _data(tx::Tx& _dst, std::int32_t item, org::unirail::AdHoc::MutableBytes dst) const` |
| Rust | no window: `fn _data_new(&mut self, len: usize) -> &mut [u8]` = storage codec copies bytes into | no window: `fn _data(&self) -> Option<&[u8]>` = bytes codec copies out |
| C | no window: `gen·__·Pack·data_new(self, n)` = storage codec copies bytes into | no window: codec copies out of storage the field's accessors give |
| Dart | `void $data$g(Base_.Receiver ctx, Base_.Receiver_Slot __slot, int item, Uint8List src)` | `void $data$v(Base_.Transmitter ctx, Base_.Transmitter_Slot __slot, int item, Uint8List dst)` |
| Swift | no window: `mutating func _data_mut(_ body: (inout [UInt8]?) …)` lends storage codec copies bytes into | no window: `func _data(_ body: (borrowing [UInt8]?) …)` lends bytes codec copies out |
| Python | abstract pack implemented in C part of host, as C | as C |
- PITFALL: TS names end in modifier chars (U+02CE receiving, U+030D sending); copy from generated code.
- GEN: receiving: `_data_new(ctx, slot)` (Go `Data_new`) called before first window to allocate field; length travels (`[,]`, `[,,]`) → also `items_count`. C#, Go add `_data_done(ctx, slot, recent, total)` (`Data_done`) for `[,,]`, called when last item in. Sending: `_data()` (Go `Data()`) tells whether field has value; `_data_len` (`Data_len()`) = number of items to send.
- RT: Rust, Swift, C: abstract pack gives codec field's storage; codec copies whole buffer blocks into/out of it, as for generated pack.
- GEN: same window applies to any plain-field array of one-byte elements that travel as stored (`sbyte`, `byte`, one-byte enum). Such arrays as `Map`/`Set` keys/values or in multidimensional array keep per-element methods.

## §5.18 TYPEDEF
KW: TYPEDEF, alias, type alias, varint on TYPEDEF, both declare a collection length, a Map or a Set cannot hold another one, exit code 2
- `TYPEDEF` = name for a field type; alias, not new type: field typed with it = exactly field of aliased type with all constraints. Use when several fields share constrained type (change in one place).
- DECL: class with single field named `TYPEDEF`; class name = alias of field's type.
```csharp
class max_6_chars_string    { [D(+6)]     string TYPEDEF; }
class max_7000_chars_string { [D(+7_000)] string TYPEDEF; }
class Packet
{
    max_6_chars_string                     string_field_with_max_6_chars;
    max_7000_chars_string                  string_field_with_max_7000_chars;
    [D(100)] max_7000_chars_string[]       array_of_100_strings_with_max_7000_chars;
}
```
- DECL: usable anywhere a type stands: field, array item (brackets stay on field: `max_7000_chars_string[]`), map key/value, set item. Doc comment on TYPEDEF documents alias.
```csharp
/// Overrides the default string limit (255 chars) for long texts.
class max_65_000_chars { [D(+65_000)] string TYPEDEF; }
/// A string key of at most 30 characters.
class Str30 { [D(+30)] string? TYPEDEF; }
/// A variable-length list of up to 100 varint ints.
class List100_intsX { [D(100), X] int[,,] TYPEDEF; }
class Lookup { [D(-3, ~3, +200)] Map<Str30, List100_intsX>[] tables; }
```
- NOTE what travels with alias: type, nullability, every constraint resolved on `TYPEDEF` field: `[D]` lengths/dimensions, `[S]`, `[MinMax]`, varint `[A]`, `[V]`, `[X]`.
- RULE: varint attributes must be on TYPEDEF itself (`class my_varint_int_type { [X] int TYPEDEF; }` → distribution travels to every user). Varint attr on using field (`[X] my_varint_int_type v;`) → agent stops, exit code 2.
- NOTE: `longJS`, `ulongJS` are TYPEDEFs of this kind →§5.6.
| Declaration | Result |
|:--|:--|
| TYPEDEF and field both declare map/set length, outer array, element array, or string limit | error: "… and the TYPEDEF … it uses both declare a collection length, an array or a string cap. Declare it in one of them." |
| TYPEDEF declaring `Map`/`Set` used as `Map` key or `Set` item | error: "… This nested Map/Set declaration is not supported" |
| TYPEDEF used as `Map` value that declares `Map`/`Set`, map/set length, outer array or dimensions | not supported; agent does not catch it yet; do not declare |
| TYPEDEF listed in a branch | error: not a transmittable pack, only a field type →§4 |
- NOTE: TYPEDEF has no line in numbers tables →§20.
- PITFALL: TYPEDEF does NOT carry transform chain or trim. Chain (→§16) or trim (→§17) on `TYPEDEF` field does not reach using fields; no warning:
```csharp
class CutPayload  { [ToStream<FromProducer>] Payload TYPEDEF; }   // the trim goes nowhere
class ZstdPayload { [Zstd]                   Payload TYPEDEF; }   // the chain goes nowhere
```
  Fix: write chain/trim on using field, or put them in a flow (flows travel, →§16.6). To give framing + chain one name, use named `Stream` pack instead of TYPEDEF: field typed with it inherits its chain and trim →§15.5, §16.3.

## §5.19 Alternatives: exactly one of several
KW: union, alternatives, one of, optional fields, Object, no union type, polymorphism
- NOTE: no union type; every union shape = packs + optional fields:
| Data is… | Declare |
|:--|:--|
| exactly one of several alternatives | pack with one optional field per alternative; receiver tests which is set |
| one or more alternatives at once | same pack, unchanged |
| alternatives sharing fields | base pack + inheritance or mixins: shared fields once, each alternative own pack →§8 |
| whole message one of several kinds | several packs; pack id tells apart, branch lists which packs a side may send →§12. Most event/response types belong here, not in a field |
| choice per collection element | pack per element with alternatives as optional fields: `Item[] items` with `class Item { A? a; B? b; }` |
```csharp
class Value { string? text; ulong? amount; Response? response; }   // exactly one of three
```
- RULE: field cannot be named `number`, `type` or other target-language keyword →§3.6.
- WIRE: unset alternative costs 1 bit →§7.5.
- PITFALL: "exactly one set" not guaranteed by AdHoc; filling code's job: state rule in pack doc, enforce where pack built, read alternatives in fixed order on receiver.
- NOTE: no `Object` type either:
| You know… | Declare |
|:--|:--|
| set of possible types | pack of optional alternatives |
| serialized bytes, fixed size | `[D(N)] Binary[]` |
| serialized bytes, variable size | `[S(N)] File` field →§15 |

## §5.20 Time types
KW: DateTime, DateTimeDef, TimeSpanDef, Duration, time, milliseconds, precision, class not struct, implements interfaces, not a host
- Four time types, plain → compact; all travel as count of ms or of steps of declared precision.
| Type | Value | Anchor | Size | Spare capacity |
|:--|:--|:--|:--|:--|
| `DateTime` | moment | Unix epoch | 6 bytes | - |
| `DateTimeDef` | moment within declared range | fixed `min` | fewest whole bytes for range at precision | extends `max`, or refines `precision` with `@precision` |
| `TimeSpanDef` | recent moment in window ending now | receiver's now | fewest whole bytes for window + 1-minute margin | refines `precision` |
| `Duration` | elapsed, 0..declared max | zero | 1–7 bytes | raises `max` |
- RULE: declare `DateTimeDef`, `TimeSpanDef`, `Duration` types as `class`, never `struct`: struct implementing interface other than `Host` (or `Modify` of host) → "The struct … implements interfaces, so it is not a constants set, and it is not a host either".
- PITFALL: declare their properties `public`; non-public property does not implement interface member → default applies.
- RULE: time type = field type only; listing in a branch = error.

### §5.20.1 `DateTime`
KW: DateTime, DateTime?, Unix epoch, 6 bytes, @AdHoc.DateTime, MinMax dates
- RT: ms since Unix epoch, range 1970-01-01..9999-12-31; dates before 1970 not representable.
- WIRE: 6 bytes; `DateTime?` reserves top value of range as null.
```csharp
class Pack { DateTime createdAt; DateTime? deletedAt; }
```
- GEN: C# `DateTime`; Java `long` of ms marked `@AdHoc.DateTime`.
- DECL: narrow plain `DateTime` to date range with `[MinMax]` constructors taking dates →§7.2.

### §5.20.2 `DateTimeDef`: a moment within a declared range
KW: DateTimeDef, min, max, precision, @precision, range expansion, enhanced precision, MIN_DATE
- NOTE: for moments anchored in history (birth dates, registration, audit). `value = (time − min) / precision`.
- DECL: class implementing `org.unirail.Meta.DateTimeDef`, overriding needed members:
| Member | Meaning | Default |
|:--|:--|:--|
| `min` | anchor: earliest moment | `DateTime.MinValue` (0001-01-01) |
| `max` | latest moment needed | `DateTime.MaxValue` (9999-12-31) |
| `precision` | one step | `TimeSpan.FromMinutes(1)` |
```csharp
class Seconds50Years : DateTimeDef
{
    public DateTime min       => new(2000, 1, 1);
    public DateTime max       => new(2050, 1, 1);
    public TimeSpan precision => TimeSpan.FromSeconds(1);
}
class UserProfile { Seconds50Years joinedAt; }
```
- WIRE sizing: steps between `min` and `max` → bits → round up to whole bytes. 50 years in seconds ≈ 1.58·10⁹ steps = 31 bits → 4 bytes; spare capacity used one of two ways:
  - Range expansion (default): precision as declared; `max` moves forward until bytes full. `Seconds50Years`: `max` → 2136-02-07 06:28:15 (2³²−1 s after `min`). Expansion stops at `DateTime.MaxValue`.
  - Enhanced precision: `@` on property name or type: `public TimeSpan @precision => ...` or `public @TimeSpan precision => ...`. Range stays near requested; precision halved per spare bit, down to 1 ms floor (1 µs for a microsecond type); `max` = `min` + full byte capacity at new step (up to ~2× requested range, more when 1 ms floor stops refinement). `max` stops at `DateTime.MaxValue` here too.
```csharp
class Decade : DateTimeDef
{
    public DateTime min        => new(2020, 1, 1);
    public DateTime max        => new(2030, 1, 1);
    public TimeSpan @precision => TimeSpan.FromMinutes(1);   // 23 bits needed, 24 allocated
}
```
- `Decade`: 23 bits → 3 bytes; with `@precision` step → 30 s, `max` ≈ Dec 2035. Without `@`: step 1 min, `max` ≈ Nov 2051.
- PITFALL: declare `max`. Omitted → end of year 9999: from 2020 at 1-min ≈ 4.2·10⁹ steps = 4 bytes. With `max => new(2050, 1, 1)` → 3 bytes, expansion carries `max` to late 2051. More: 50 years at 1-day = 15 bits → 2 bytes; 1 day at 1 ms = 27 bits → 4 bytes.
- NOTE: only changes byte count; same on every host and both directions; both ends work with ordinary date-time value.
| Declaration | Result |
|:--|:--|
| `max` earlier than `min` | error |
| `precision` zero or negative | error: "precision must be a positive time span" |
- GEN: generated type carries final `min`, `max` (ms since Unix epoch, after expansion) and `precision` (ms).
- RULE: `min` may be any moment from year 1, before 1970 too: `DateTimeDef` always linear from `min`; only `TimeSpanDef` is a cyclic window. Default `min` (year 1) costs bits → declare the `min` you need.
- RULE: `precision` of whole days = calendar date: year/month/day taken in UTC, no local zone → midnight never becomes previous day.
- RULE: `precision` of whole ms → millisecond type; any other step (`TimeSpan.FromTicks(10)` = 1 µs) → microsecond type: `min`, `max`, `precision` in µs; range ≤ 2⁵³ − 2 steps (~285 years at 1 µs) so TS keeps a number. Step finer than 1 µs → error: keep such a value in a plain `long`.
- PITFALL: values outside `min`…`max` not rejected: encoder wraps modulo range; check range in your code.

### §5.20.3 `TimeSpanDef`: a recent moment
KW: TimeSpanDef, interval, precision, cycle, margin, 1 minute, EarliestValidDate, getEarliestValidMillis, EarliestValidMillis, earliest_valid_date
- NOTE: for always-recent values where age matters more than date (telemetry, sensor ring buffers, session activity). Value = position within cycle; receiver places it on own timeline in one-cycle window ending 1 minute after its current time. Only recent window costs bytes.
| Member | Meaning | Default |
|:--|:--|:--|
| `interval` | window of history needed | `TimeSpan.FromDays(1)` |
| `precision` | step needed at least | `TimeSpan.FromSeconds(1)` |
```csharp
class SecondsADay : TimeSpanDef
{
    public TimeSpan interval  => TimeSpan.FromDays(1);
    public TimeSpan precision => TimeSpan.FromSeconds(1);
}
class Sample { SecondsADay taken; }
```
- WIRE sizing, 3 steps:
  1. Bytes: smallest count (≤8) holding `interval / precision` steps. `precision` not whole ms → microsecond type (counted in µs below); step finer than 1 µs, zero/negative `interval` or `precision` = error.
  2. Margin: reserve 1 minute capacity beyond `interval`; if spare capacity < 1 minute → add a byte.
  3. Precision: spread `interval` + 1 min over full capacity: new precision = `(interval + 1 minute) / capacity`, rounded up to whole ms (≥1 ms; µs for a microsecond type). Cycle = `capacity × precision`.
- Example: last 27 h at 1 s = 97,200 steps → 3 bytes (16,777,216 steps); minute fits. Precision = ⌈97,260,000 ms / 16,777,216⌉ = 6 ms; cycle = 16,777,216 × 6 ms = 100,663,296 ms ≈ 27.96 h. `SecondsADay` ends the same: 3 bytes, 6 ms, cycle ≈ 27.96 h. Runtime does all in integer arithmetic.
- RT: value encoded as cycle position without cycle number. Receiver decodes into window (now + 1 min − cycle, now + 1 min]: takes cycle holding "now + 1 min"; if value would land after that point → previous cycle. Packet sent just before rollover, received after → still right cycle while age < cycle − 1 min. 1-minute margin at newer end absorbs value slightly ahead of receiver clock (sender clock ahead); without it such value decodes a whole cycle into the past.
- RULE (usage): work only with moments between now and earliest valid moment = now − (cycle − 1 minute). Older values ambiguous: decode into later cycle.
| Lang | Earliest valid moment |
|:--|:--|
| C# | `SecondsADay.EarliestValidDate` (`DateTime`) |
| Java | `SecondsADay.DT.getEarliestValidMillis()` (ms); `getEarliestValidDateTime()` (encoded value) |
| TS | `SecondsADay.DT.EarliestValidMillis` (ms); `EarliestValidDateTime` (encoded value) |
| Rust | `SecondsADay::earliest_valid_date()` (`std::time::SystemTime`) |
| Go | `SecondsADay_EarliestValidDate()` (`time.Time`) |
| C++ | `SecondsADay::EarliestValidDate()` (`SecondsADay::DateTime` = `std::chrono::sys_time` of type's unit) |
| C | `gen·__·SecondsADay·EarliestValidDate()` (ms since Unix epoch, `int64_t`; µs type: `EarliestValidDate_micros()`) |
| Swift | `SecondsADay.earliest_valid_date()` (`std.SystemTime` of lib) |
| Dart | `SecondsADay.DT.EarliestValidMillis` (ms); `EarliestValidDateTime` (encoded value) |
| Python | no member: field setter takes `datetime` from earliest valid moment to one cycle after it; outside → `ValueError` |

### §5.20.4 `Duration`: how long something took
KW: Duration, max, precision, elapsed, steps, 2^53-2, max must be a positive step count
- NOTE: elapsed time (latency, run time, timeouts, heartbeat periods). No calendar anchor (unlike `DateTimeDef`), never rolls over (unlike `TimeSpanDef`) → no margin. `value = elapsed / precision`, in `[0, max]`.
| Member | Meaning | Default |
|:--|:--|:--|
| `max` | largest duration, as step count | largest allowed: 2⁵³ − 2 steps |
| `precision` | one step: whole ms, or µs when not whole ms | `TimeSpan.FromSeconds(1)` |
- WIRE sizing: fewest whole bytes, 1–7, holding `max` steps; then `max` raised to largest value those bytes hold. Precision never changes. `max` needing ≥7 bytes → set to 2⁵³ − 2 (exact integer in every language; plain `number` in TS).
```csharp
class RequestLatency : Duration
{
    public long     max       => 30_000;                          // 15 bits: 2 bytes; max becomes 65,535
    public TimeSpan precision => TimeSpan.FromMilliseconds(19);   // 65,535 × 19 ms ≈ 20.75 minutes
}
class Duration3days : Duration
{
    public long     max       => 99;                              // 1 byte; max becomes 255 steps
    public TimeSpan precision => TimeSpan.FromDays(3);            // 255 × 3 days = 765 days
}
class OneSecond : Duration { }                                    // the defaults: 7 bytes, 1-second steps
class ApiCall { RequestLatency latency; OneSecond uptime; }
```
- Example: up to 1 h at 1 s = 3,600 steps = 12 bits → 2 bytes; `max` raised to 65,535 steps ≈ 18.2 h at no cost.
| Declaration | Result |
|:--|:--|
| `max` zero or negative | error: "max must be a positive step count" |
| `precision` zero or negative | error: "precision must be a positive time span" |
| `max` above 2⁵³ − 2 | clamped to 2⁵³ − 2 |
| `precision` with ms fraction | microsecond type: step in µs |
| `precision` finer than 1 µs | error: keep such a value in a plain `long` |

### §5.20.5 What the generator produces for time types
KW: readonly struct, MIN_DATE, MAX_DATE, PRECISION, MAX, DT codec, branded number, Value.Millis, Value.Seconds, annotation, SystemTime, time.Time, sys_time, _value, std.SystemTime, extension type, datetime, timedelta
- GEN: each `DateTimeDef`, `TimeSpanDef`, `Duration` → own type in every language except Python; fields use it.
| Lang | Generated type |
|:--|:--|
| C# | `readonly struct` wrapping raw value; implicit conversions to/from raw value and `DateTime` (`DateTimeDef`, `TimeSpanDef`) or `TimeSpan` (`Duration`). Constants `MIN_DATE`, `MAX_DATE`, `PRECISION`; `Duration`: `PRECISION`, `MAX` |
| Java | annotation on primitive field (`@SecondsADay int taken`). `DateTimeDef`/`TimeSpanDef`: annotation holds codec `DT` + `MIN_DATE`, `MAX_DATE`, `PRECISION`; `Duration`: `PRECISION`, `MAX`, helpers `Value.Millis(...)`, `Value.Seconds(...)` |
| TS | branded `number` type + same-name namespace holding codec `DT` + `MIN_DATE`, `MAX_DATE`, `PRECISION`; `Duration`: `PRECISION` (ms), `MAX`, helper `Value(...)` (step count → ms) |
| Rust | `#[repr(transparent)] pub struct SecondsADay { pub _value: u32 }` (`Copy`, `Ord`, `Hash`): `new(raw)`, `of(…)`, `value()`, `From` both ways with raw value and `std::time::SystemTime` (`DateTimeDef`, `TimeSpanDef`) or `std::time::Duration` (`Duration`); `+`/`-` `std::time::Duration`. `PRECISION`, `min()`, `max()`; `Duration`: `PRECISION`, `MAX` |
| Go | named integer `type SecondsADay uint32`; `SecondsADay_of(…)` / method `Value()` ↔ `time.Time` (`DateTimeDef`, `TimeSpanDef`) or `time.Duration` (`Duration`). `SecondsADay_MIN_DATE`, `_MAX_DATE`, `_PRECISION`; `Duration`: `_PRECISION`, `_MAX` |
| C++ | `struct SecondsADay final` over `_value`; explicit ctors from raw value and `DateTime` (`std::chrono::sys_time` of ms, µs for µs type) or `std::chrono::milliseconds`/`microseconds` (`Duration`); `Value()`, `==`, `<=>`, hash. `MIN_DATE`, `MAX_DATE`, `PRECISION`; `Duration`: `PRECISION`, `MAX` |
| C | `typedef struct { uint32_t _value; } gen·__·SecondsADay` + functions `·new(raw)`, `·of(millis)`, `·value(&d)` (ms since Unix epoch, or of `Duration`), `·eq`, `·hash`, `·add`, `·sub` (date: also `·cmp`). Macros `·MIN_DATE`, `·MAX_DATE`, `·PRECISION`; `Duration`: `·PrecisionMillis`, `·MAX` |
| Swift | `public struct SecondsADay` over `_value` (`Hashable`, `Comparable`): `init( raw )`, `of(…)`, `value()` ↔ `std.SystemTime` of lib (`DateTimeDef`, `TimeSpanDef`) or Swift `Duration` (`Duration`); `inc()`, `dec()`, `+`/`-` `Duration`. `PRECISION` (a `Duration`), `min()`, `max()`, `earliest_valid_date()`; `Duration`: `PRECISION`, `MAX` |
| Dart | `extension type const SecondsADay( int v ) implements int` (raw value); statics: codec `DT`, `MIN_DATE`, `MAX_DATE` (Dart `DateTime`), `PRECISION`, `MAX`; `Duration`: `PRECISION`, `MAX`, helper `Value(...)` (step count → ms) |
| Python | no type: field reads/writes `datetime` (`DateTimeDef`, `TimeSpanDef`; returned in UTC, naive value taken as UTC) or `timedelta` (`Duration`), checked against range of the type: outside → `ValueError` (negative `timedelta` too), past `MAX` of `Duration` → `OverflowError` |
```csharp
// C#: a field of a DateTimeDef type reads and writes as a DateTime
profile.joinedAt = DateTime.UtcNow;
DateTime when    = profile.joinedAt;
```
- DECL: time types may be optional (`RequestLatency?`) and array items (`[D(10)] RequestLatency[,]`). `DateTimeDef` type may also be Set item or Map key (`Map<Seconds50Years, int?>`).

## §5.21 Stream and File fields
KW: Stream, File, [S(N)], conduit, abstract pack, channel
- DECL: `[S(N)] Stream` (chunked, may be cut short), `[S(N)] File` (one length, then bytes): move bytes socket buffer ↔ channel of your code without holding payload in memory.
- RULE: conduit field makes its pack abstract; cannot sit inside a collection. Details →§15.

# §6 Constants and enums
KW: const, static, enum, constants container, never sent, literal
SUMMARY: constants (`const`/`static`, `[ValueFor]`), constants containers, enums/flags enums, numbering, agent checks, and which hosts' generated code receives them.
- `constant` = named value written by generator into generated code as literal. `enum` = named set of integer constants of one type.
- RT: neither ever travels: each host needing a constant has own compiled copy; peer never reads it from connection. Enum-typed field travels as small code, not as constant →§7.4.
- Purposes: same numbers/strings/bit masks/lookup tables for every host, declared once; parametrize field attributes (`const` as `[D(...)]` length or `[MinMax(...)]` bound); enums give fields closed vocabulary encoded in fewest bits.

## §6.1 Declaring a constant: const and static
KW: const, static, constant types, static array, 1000 characters, Constant field cannot have type, invariant culture, initializer
- DECL: `const` or `static` field of supported type, in body of any entity: project interface, host, pack, constants container, connection, multiplexer. Belongs to that entity; becomes member of its generated type.
| Type | Notes |
|:--|:--|
| `bool`, `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char`, `float`, `double` | any value incl. `NaN`, `±Infinity`, `-0.0`, `ulong` above `long.MaxValue` |
| `string` | ≤1000 chars |
| `Binary`, `longJS`, `ulongJS` | AdHoc types of §5 |
| array of any of above | `static` only (C# has no `const` array); ≤255 elements; string element ≤1000 chars; `null` element refused |
- RULE: other type → agent stops: "Constant field '...' cannot have ... type. Supported: bool, byte/sbyte, short/ushort, int/uint, long/ulong, char, float, double, string, binary, longJS, ulongJS."
| | `const` | `static` |
|:--|:--|:--|
| computed by | C# compiler | agent at generation time (runs C# initializer) |
| initializer | C# compile-time expression; no method calls | any C# expression: `Math.Sin(23)`, `string.Join(...)`, `nameof(...)`, other statics |
| usable as attribute argument | yes | no (C# rule) |
| arrays | no | yes |
- RULE: agent evaluates `static` fields once, in text order: static reading another static must come after it. Invariant culture: `$"{1.5}"` = `"1.5"` on every machine. Every constant must have an initializer.
```csharp
class Settings
{
    const  int    MAX_RETRY = 7;               // compile-time: may serve as an attribute argument
    static double HALF_TURN = Math.PI / 2;     // computed by the agent at generation time
    [D(MAX_RETRY)] int[,] attempts;            // the const parametrizes a field
}
```
- GEN: generated code holds computed values, not expressions. `static` scalar → plain constant in target language; `static` array → read-only array literal.
- Example (AdHoc's own protocol: send small numbers, keep texts at home): `Progress` = 23-bit Value Pack (→§7.9); `what`, `lang`, `level` = indexes into string tables:
```csharp
public class Progress {
    [MinMax(0, 15)]  byte what;    // the index of its text in TEXT
    [MinMax(0, 31)]  byte lang;    // the index of its name in LANG
    [MinMax(1, 4)]   byte pass;
    [MinMax(1, 4)]   byte passes;
    [MinMax(0, 100)] byte percent;
    [MinMax(0, 4)]   byte level;   // the index of its text in LEVEL
    static string[] TEXT  = { "accepted", "generating code", "compiling", "running tests", "done", "delivering the result" };
    static string[] LANG  = { "C++", "Rust", "C#", "Java", "Go", "TS", "Swift", "C", "Dart", "Python" };
    static string[] LEVEL = { "none", "delivered code compiled", "compiled", "all stages compiled", "full" };
}
```
- GEN (C#): tables next to pack, `public static readonly string[] LANG = new string[] { "C++", "Rust", ... };` (same for `LEVEL`, `TEXT`); only the 23 bits of the pack travel.
- GEN: constants also appear undeclared: every applied custom attribute → constant of its entity; some built-in attributes (`TransmitTimeout`, `Resumable`, ...) emitted as constants of their state, actor or connection →§23 (§23.2.9, §23.4.4).

## §6.2 Computed constants: [ValueFor]
KW: [ValueFor], proxy field, placeholder, computed const, attribute argument, target type
- DECL: `const` holds only compiler-computable values; attribute arg must be `const`. To put computed value into attribute (or a constant): compute in `static` proxy, mark proxy `[ValueFor(TARGET)]`. At generation agent copies proxy's value AND type into `const` `TARGET`. In description `const` keeps placeholder value (all C# sees).
```csharp
class Login
{
    [ValueFor(NAME_CAP)] static int name_cap = "anonymous".Length * 4;          // 36
    [ValueFor(MAX_IDS)]  static int max_ids  = (int)Math.Pow(2, 5);             // 32
    [ValueFor(TEMP_LO)]  static int temp_lo  = -(int)Math.Round(Math.PI * 100); // -314
    [ValueFor(TEMP_HI)]  static int temp_hi  = 1000 + 1;                        // 1001
    const int NAME_CAP = 1;                    // placeholders: C# sees only these
    const int MAX_IDS  = 1;
    const int TEMP_LO  = 0;
    const int TEMP_HI  = 1;
    const int PLAIN    = 16;                   // a plain const: C# folds it itself
    [D(+NAME_CAP)]             string?   nick;        // up to 36 characters
    [D(MAX_IDS)]               int[]?    ids;         // 32 items
    [D(Login.MAX_IDS)]         ushort[]? codes;       // named through its pack: also fine
    [D(+(PLAIN * 2))]          string?   note;        // an expression of plain consts: C# computes it
    [MinMax(TEMP_LO, TEMP_HI)] int       temperature; // -314 … 1001
}
```
- NOTE: `[ValueFor]` arg only names target; its value never used. Overloads for `long`, `ulong`, `double`, `bool`, `string` → target may be `const` of any type converting to one of them without cast (every primitive, `string`).
- GEN: target takes proxy's type: `[ValueFor(VF_WIDER)] static long vf_wider = int.MaxValue + 1L;` → `const int VF_WIDER` is `long` in generated code.
- RULE where usable: C# compiles every use with placeholder; agent replaces placeholder only where it reads the constant by name, as a whole, in a field attribute:
| Accepted use | Example |
|:--|:--|
| `[D]` length, also with `+`, `-`, `~` | `[D(MAX)]`, `[D(+MAX)]`, `[D(-MAX)]`, `[D(~MAX)]` |
| conduit cap | `[S(MAX)]` |
| bounds of range or varint | `[MinMax(LO, HI)]`, `[A(LO, ...)]`, `[V(...)]`, `[X(...)]` |
| any of these via pack name | `[D(Login.MAX_IDS)]` |
- RULE: every other use refused (placeholder would be used silently): expression around constant (`[D(MAX_IDS * 2)]`, `-MAX_IDS`), another `const`/`static` initialized from it, enum member, any other attribute. Agent prints line + 3 fixes: use constant by name in one of above attributes; compute needed value in another static with own `[ValueFor]`; read the static proxy instead of constant where C# computes.
| Rule | Agent |
|:--|:--|
| one proxy per target | second `[ValueFor]` naming same `const` = error |
| scope | works in project, hosts, packs, constants containers |
| exotic values | `NaN`, `±Infinity`, `-0.0`, `double.Epsilon`, `ulong.MaxValue - 5`, strings with quotes, backslashes, `\u` sequences, control chars, CR/LF, surrogate pairs, U+2028/U+2029 or `${...}` written so C#, Java, TS each reproduce exact value |
- Host-scope example, from enum member and enum names:
```csharp
/// <see cref="InCS"/>
struct Server : Host
{
    public enum MAV_BATTERY_FUNCTION : byte
    {
        MAV_BATTERY_FUNCTION_UNKNOWN, MAV_BATTERY_FUNCTION_ALL, MAV_BATTERY_FUNCTION_PROPULSION,
        MAV_BATTERY_FUNCTION_AVIONICS, MAV_BATTERY_TYPE_PAYLOAD,
    }
    [ValueFor(HOST_BATTERY)] static byte host_battery = (byte)(MAV_BATTERY_FUNCTION.MAV_BATTERY_FUNCTION_ALL + 2); // 3
    const byte HOST_BATTERY = 0;
    [ValueFor(HOST_NAME)] static string host_name = nameof(MAV_BATTERY_FUNCTION) + "::" + MAV_BATTERY_FUNCTION.MAV_BATTERY_TYPE_PAYLOAD;
    const string HOST_NAME = "";
}
```
- PITFALL: `static` initializer runs as C# → sees C# enum numbering. Enum mixing explicit and implicit values: AdHoc numbers implicit ones differently (→§6.4.1) → computed constant no longer matches generated enum. Give every member a value, or none.

## §6.3 Constants containers
KW: constants container, struct, interface, empty class, namespace, never transmitted, implements interfaces, not a host
- `constants container` = type holding constants and nested declarations, never transmitted. Gives constants place and name (units, limits, masks); nested declarations form hierarchy repeated in generated code.
| Declaration | Container because |
|:--|:--|
| `struct Name { ... }` implementing no interface | interface-less struct = container wherever declared |
| nested `interface Name { ... }` whose first base is not an AdHoc interface | such interfaces also serve as namespaces for actor hierarchies →§13 |
| `class Name { ... }` with no instance fields, referenced by no branch | agent reclassifies such empty pack as container automatically |
- NOTE: `struct` form = explicit choice, can never become transmittable pack. Empty `class` stays pack (singleton signal) as soon as a branch sends it →§4.5.
```csharp
struct SI_Unit                       // constants container
{
    struct time     { const string s = "s"; const string ms = "ms"; const string us = "us"; const string Hz = "Hz"; } // nested: SI_Unit.time
    struct distance { const string km = "km"; const string m = "m"; const string m_s = "m/s"; }
    struct derived                   // [ValueFor] works inside a container the same way
    {
        [ValueFor(km_in_m)]        static long   src_km_in_m = 1000L * 1;
        [ValueFor(speed_of_light)] static double src_c       = 299_792_458.0;
        [ValueFor(label)]          static string src_label   = string.Concat("k", "m", "/", "s"); // "km/s"
        const long   km_in_m        = 0;
        const double speed_of_light = 0;
        const string label          = "";
    }
}
```
- RULE: `struct`/`interface` container is not a field type (no value to send); for closed value set use enum →§6.4. Empty `class` as field type → `bool` even when it is a container →§4.5.1.
- RULE: `struct` and `class` forms cannot be declared inside a connection body (classes, enums, structs that are not states refused there). Interface inside connection = namespace for actors.
- RULE: struct implementing an interface and not a host (or host modifier) = not a container but error ("The struct ... implements interfaces, so it is not a constants set, and it is not a host either") →§3.
- PITFALL: declare no instance fields in container; agent does not reject them, but meaningless (never sent).

## §6.4 Enums and flags enums
KW: enum, [Flags], FlagsAttribute, flags enum, standard enum, bit set, MID
- DECL: C# `enum` = named integer constants of one type. Declarable in project, host, pack, constants container; NOT in connection body. As field type → closed value set →§5.16.
```csharp
[Flags]                              // a bit set: members combine with |
enum GIMBAL_DEVICE_FLAGS
{
    GIMBAL_DEVICE_FLAGS_RETRACT    = 1,
    GIMBAL_DEVICE_FLAGS_NEUTRAL    = 2,
    GIMBAL_DEVICE_FLAGS_ROLL_LOCK  = 4,
    GIMBAL_DEVICE_FLAGS_PITCH_LOCK = 8,
    GIMBAL_DEVICE_FLAGS_YAW_LOCK   = 16,
}
enum MID                             // explicit message codes
{
    ServerRegisterReq = 1001, ServerRegisterRes = 1002, ServerListReq = 1003, ServerListRes = 1004,
    ChangeRoleServerReq = 1005, ChangeRoleServerRes = 1006, ServerEventReq = 1007, ServerEventRes = 1008,
}
```
- `standard enum` = field holds exactly one member. `flags enum` (`[Flags]`, `System.FlagsAttribute`) = field holds any combination; each member one or more bits.
- NOTE: agent reads `[Flags]` as enum kind; it does not become custom metadata.
- WIRE: coding per kind (offset of contiguous enum, index of sparse one, mask of flags enum) →§7.4. `MID` above (8 contiguous codes from 1001) travels in 3 bits.

### §6.4.1 Numbering of enum members
KW: enum numbering, implicit values, explicit values, gaps, power of two, differs from C#
- RULE: explicit values optional. Members without value get:
| Enum | Members without value get |
|:--|:--|
| `[Flags]` | lowest power of two no explicit value uses, in declaration order |
| standard, no explicit values | 0, 1, 2, … in declaration order |
| standard, some explicit values | first gaps between explicit values, smallest up; then values below smallest explicit, counting down toward 0; then values above largest |
- PITFALL: mixed case differs from C#: `enum E { A = 5, B, C }` C# → `B = 6`, `C = 7`; AdHoc → `B = 4`, `C = 3`. Generated enums carry AdHoc values; C# code in description (`static` initializer like `(int)E.B`) uses C# values. Give every member a value, or none.

### §6.4.2 What the agent checks and does with an enum
KW: one value carries no information, sent as bool, aliases, underlying type, all powers of two, mark it [Flags], : uint, bit 31, SlimEnum, @interface
| Situation | Result |
|:--|:--|
| fewer than two members | error: "The enum … (…) has 1 member(s): one value carries no information. Add members, or delete the enum." |
| ≥2 members all sharing one value, used as field type | warning "All members of the enum … (…) have one value, so a field of its type carries no information: such fields are sent as bool."; such fields → `bool` |
| standard enum members sharing a value (aliases) | one code travels under name of first member with that value; agent logs it |
| no explicit underlying type | narrowest integer type holding all values, not C# default `int` |
| explicit underlying type (`enum E : byte`) | kept as declared |
| ≥3 non-zero values, all powers of two, none negative, no `[Flags]` | info: "The values of the enum E (…) are all powers of two: if they are combined, mark it [Flags]." |
- PITFALL: explicit underlying type still needed where C# needs it: flags enum using bit 31 must be `: uint` (`2147483648` does not fit C# default `int`).
- GEN: enum repeated in each language with every value written out. C# = `enum` with explicit values:
```csharp
public enum Type : byte
{
    Actor = 6, Connection = 5, Constant = 4, Field = 3, Host = 1, Pack = 2, Project = 0, State = 7,
}
```
- GEN: Java enum = type-use `@interface` holding values as primitive constants and tagging primitive variables that carry them; SlimEnum plugin turns tags into typed completion →§7.4.

## §6.5 Where constants and enums end up
KW: distribution, routes, _<T>, project interface, host scope, DataType, static members, namespace shell
- RULE: constant declared inside pack belongs to pack: every host whose generated code has the pack has the constant. Container or enum distributed separately, via 6 routes (additive):
| # | Route | Reaches |
|:--|:--|:--|
| 1 | declared inside host body, any depth (also nested in pack there) | that host |
| 2 | declared in project outside every host body (directly or nested in pack there) | every host of project |
| 3 | listed in `_<T>` on project interface | every host of project |
| 4 | type of field (or Map value) of pack host sends/receives, also behind `TYPEDEF` | that host |
| 5 | referenced by constant initializer of pack in host's scope →§6.5.1 | that host |
| 6 | listed in `_<T>` on host (`struct Server : Host, _<Codes>`); a pack there → warning, left out | that host |
- RULE: routes 1–3 apply only to containers holding ≥1 constant, and to enums; container only nesting other declarations (namespace shell) not distributed itself.
- GEN: whatever route, constants emitted as static members of host's generated code; never serialized.
- NOTE: host exchanging none of the packs gets only project-wide ones; connection carries fields only, no constant travels.
- DECL: route 3 pulls container/enum declared where other routes don't reach (inside host body, or another project). AdHoc's own protocol gives every host `DataType` enum declared deep in a pack:
```csharp
public interface AdHocProtocol :
    _<
        AdHocProtocol.Agent.Project.Host.Pack.Field.DataType // propagate the DataType enum to all hosts
    >
{ /* ... */ }
```
- RULE: only project interface distributes this way: list in project's `_<...>`, not a host's. Importing from other projects, removing import with `X<>`, modifying imported enums/constants with `Modify<>` →§22 (§22.2, §22.3.4).

### §6.5.1 Cross-pack constant references
KW: route 5, constant initializer, cross-container reference, Mask, compile, scope
- RULE: constant computed from constants of another container → every host having first pack must also have container (else generated code won't compile). Route 5: agent walks initializer of every constant of every pack in host's scope, follows each identifier resolving to constant of a different container or enum, adds that container to host.
```csharp
/// <see cref="InCS"/>
struct Monitor : Host
{
    public class Event
    {
        uint id;                                   // a composite event id, built from Mask and Action
        public struct Mask   { public const uint REMOTE  = 1u << 31; public const uint THIS  = 0; }
        public struct Action { public const uint CONNECT = 1;        public const uint CLOSE = 2; }
        static uint REMOTE_CONNECT = Mask.REMOTE | Action.CONNECT;
        static uint THIS_CONNECT   = Mask.THIS   | Action.CONNECT;
    }
}
```
- `Mask`, `Action` in `Monitor` body → route 1 gives them to `Monitor`. Host receiving `Event` gets them by route 5 (initializers of `REMOTE_CONNECT`, `THIS_CONNECT` name them). Host neither sending nor receiving `Event` gets neither. GEN: `Event` carries computed `REMOTE_CONNECT = 2147483649U`, `THIS_CONNECT = 1U`; generated containers carry `REMOTE`, `CONNECT`, etc.
- RULE scope: pack in host's scope when host sends or receives it; pack with a language configuration on the host (→§10.5) counts too.
- RULE driven by references: follows identifiers, not structure: referenced container may be sibling, nested, anywhere; expression any (bitwise, arithmetic, nested, member-access chain).
- RULE no duplicates: referenced pack host already sends/receives not added to constants list again (already in code as pack).

# §7 How values become bytes
KW: wire format, little-endian, exT, inT, ioT, bit field, varint, Value Pack, field order, serialization
SUMMARY: path of a value from declared type to wire bytes: 3 value layers, `[MinMax]` narrowing, bit fields, enums, nulls, varint `[A]`/`[V]`/`[X]`, range constants, strings/lengths, Value Packs, field groups and order.
- WIRE: multi-byte values little-endian everywhere.
- NOTE: nothing of this chapter visible to user code; generated API takes/returns declared values.

## §7.1 Value layers: exT, inT, ioT
KW: exT, inT, ioT, exTernal type, inTernal type, IO wire type, offset, bit-packed, typed array, Uint8Array
- Every primitive field value has 3 representations; generator converts between them:

| Layer | Name | What |
|:--|:--|:--|
| `exT` | exTernal type | what user code sees: getter return type, setter param; language-type granularity |
| `inT` | inTernal type | storage in generated object; language-type granularity |
| `ioT` | IO wire type | travel form: run of bytes/bits, no language granularity |

- Example: values 40,000,000,000 … 40,000,000,093 → exT must be `long`, but 94 values; stored and sent as `value - 40,000,000,000` → 7 bits.
- RT: `inT ↔ ioT` runs on every send/receive; real saving here. Values 1,000,000 … 1,080,000 (80,000 values): `int` in memory either way (no 3-byte integer in any language), travel 3 bytes as `value - 1,000,000`, receiver adds back. Span needing 3, 5, 6 or 7 bytes → exactly that many bytes on wire.
- RT: `exT ↔ inT` runs when code touches the field; saves memory only in collections of primitives (item width set by collection, not language type). Storage chosen by span of declared range:
  - 1,000,000 … 1,000,004 = 5 values, 3 bits → bit-packed, 3 bits/item instead of 4 bytes, item stored `value - 1,000,000`, accessors add offset back.
  - `[MinMax(-5, 5)] int[]` = 11 values, 4 bits/item, stored `value + 5`.
  - wider span → typed array of narrowest element: `[MinMax(400_000_000, 400_000_193)] int[]` → 1 byte/item.
- NOTE: TypeScript applies shift only in collections: single primitive field, primitive `Map`/`Set` entry, primitive item of plain `Array` = `number`, not shifted; shifted only in typed arrays (`Uint8Array` … `Uint32Array`), collections built on them, and bit-packed storage.
- GEN: generated API of primitive field speaks `exT` only: getters, setters, per-field constants `MIN`, `MAX`, `NULL`, `ZERO` →§7.7 use declared values.

## §7.2 Narrowing a primitive field with [MinMax]
KW: [MinMax], MinMax, span, offset, min, bit field, range, float, double, longJS, ulongJS, single value
- DECL: `[MinMax(min, max)]` = inclusive range of numeric primitive field (on collection: of its primitive items) → stored and sent in fewest bits/bytes holding the range.
```csharp
[MinMax(400_000_000, 400_000_193)] int ranged_field;  // 194 values: one byte, offset 400 000 000
[MinMax(-5, 5)]                    int trim;          // 11 values: 4 bits
[MinMax(1_000, 1_010)]             int level;         // 11 values: 4 bits as well
[MinMax(1_000, 1_009)]             int? setting;      // 10 values + null = 11 codes: 4 bits
```
- WIRE: stored value = offset from `min` (`value - min`) → only span decides width, not magnitude. Where raw value already fits same width, generator may store it unshifted (same width, same accessor behavior).

| Span (`max - min`, +1 when nullable) | Storage |
|:--|:--|
| below 128 | bit field: fewest bits holding largest code, packed with pack's other bit fields →§7.3 |
| up to 255 | 1 byte |
| larger | memory: narrowest unsigned 16/32/64-bit integer; wire: exactly as many bytes as span needs |

- NOTE: range may straddle zero and fit neither signed nor unsigned type of its width: `[MinMax(-100, 150)] int` = 251 values → 1 byte.
- WIRE: bit field holds codes `0 … N-1` for N values; nullable → one more code = "no value". `[MinMax(0, 7)] byte` → 3 bits; `[MinMax(-11, 75)] short` (87 values) → 7 bits; `[MinMax(1, 8)] int?` (8 values + null) → 4 bits, null code 8. Codes never reach user code: accessors convert.
- GEN: exT chosen from range: getter/setter use narrowest integer type holding `min … max`; may differ from declared type in either direction:
```csharp
[MinMax(-1128, 873)] byte altitude_offset;  // the range outgrows byte: the generated field is a short
[MinMax(20, 120)]    int  temperature;      // the range fits a byte: the generated field is a byte
[MinMax(40_000_000_000, 40_000_000_093)] long ranged_long; // stays a long for your code, 7 bits inside
```
- RULE: single-value range refused (`[MinMax(5, 5)]`): "... declares a range of a single value ... Widen the range, or declare the value as a constant instead of a field."
- RULE: one range attribute per field: at most one of `[MinMax]`, `[A]`, `[V]`, `[X]`, also across separate attribute lists. Error: "The field … (…) has more than one of [MinMax], [A], [V], [X]: a field takes one of them. A varint attribute takes the range as its arguments: [A(min, max)]." Attribute classes declare `AllowMultiple = true` → C# compiles a second one; agent refuses.
- RULE: numbers only: refused on `bool`, `string`, `DateTime`, pack-typed fields. On collection applies to items; `[Key: MinMax(...)]` / `[Val: MinMax(...)]` target Map key / value.
- DECL: bound may be a `[ValueFor]` constant named as a whole →§6.2.
- DECL: constructors `MinMax(long, long)`, `MinMax(ulong, ulong)`, `MinMax(double, double)` → integer and floating ranges. Class also declares date constructors and allows method target; agent gives range meaning only on numeric fields, refuses `[MinMax]` on `DateTime`.
- RULE: date constructors on numeric field refused: "[MinMax(…)] on the field … takes two bounds, the min and the max". Bound dates with `DateTimeDef` →§5.20.2.
- GEN: `float`/`double` field: range recorded, published as `MIN`/`MAX`; field generated as `float` when both bounds within `float` range, else `double`:
```csharp
[D(10), MinMax(-0.7, 0.7)] float[]   gains;    // a float, range -0.7 … 0.7
[MinMax(-1.5, 1.5)]        double    weight;   // the bounds fit a float: the field becomes a float
```
- PITFALL: `double` with `[MinMax]` fitting `float` range becomes `float` → 24 bits mantissa instead of 53. Leave `[MinMax]` off a `double` whose precision matters.
- NOTE: `longJS`, `ulongJS` = `[MinMax]` TYPEDEFs over `long`; narrow like any range →§5.6.

## §7.3 Bit fields: small values share the pack's bits
KW: bit field, bool, bool?, enum, [MinMax], 7 bits, 128 codes, group 2, bit storage
- WIRE: in every pack, primitive field whose codes fit 7 bits (values + 1 "none" code if nullable; ≤128 codes) = `bit field`. No own byte: bits of all bit fields of pack packed together, lowest bits first, declaration order, rounded up to whole bytes once per pack. = group 2 of pack layout →§7.10.

| Field | Codes | Bits |
|:--|:--|:--|
| `bool` | 2 | 1 |
| `bool?` | 3: false, true, none | 2 |
| enum with 3 values | 3 | 2 |
| same enum, nullable | 4 | 2 |
| `[MinMax(0, 7)] byte` | 8 | 3 |
| `[MinMax(1, 8)] int?` | 9 | 4 |
| `[MinMax(-20, 20)] sbyte` | 41 | 6 |
| `[MinMax(0, 100)] byte` | 101 | 7 |
| `[MinMax(0, 127)] byte` | 128 | 7 |
| `[MinMax(0, 127)] byte?` | 129 | not bit field: 1 byte |

```csharp
public class Status
{
    long                    at;         // 8 bytes, fixed: not a bit field
    bool                    ignition;   // 1 bit
    bool?                   door_open;  // 2 bits
    Gear                    gear;       // 2 bits - enum Gear { Park, Drive, Reverse }
    Gear?                   wanted;     // 2 bits
    [MinMax(0, 100)] byte   fuel_pct;   // 7 bits
    [MinMax(-20, 20)] sbyte tilt;       // 6 bits
    [D(+32)] string         driver;     // a reference field: a null bit in group 4
}
```
- WIRE: six bit fields = 20 bits → `Status` writes 3 bytes bit storage after 8 bytes of `at`. Code = `value - min`: `bool?` stores 0 false, 1 true, 2 none; accessors convert both ways (code sees `bool?`, `Gear`, `byte`, never codes).
- WIRE: varint fields' length bits join same storage when pack has varints →§7.6.1. Pack whose fields all fit 64 bits → single integer, Value Pack →§7.9.

## §7.4 Enums on the wire
KW: enum, [Flags], code, value - min, switch, index, sparse enum, bit mask, null code
- WIRE: enum field travels as code, never declared constant. Agent picks code from enum values:

| Enum | Code on wire | Example |
|:--|:--|:--|
| standard, values contiguous/close | `value - min` (expression) | `enum Shifted { P = 10, Q = 11, R = 12 }`: `value - 10`, 2 bits |
| standard, values sparse | index among distinct values (switch) | `enum Sparse { LOW = -7, MID = 0, HIGH = 300 }`: 0, 1, 2 in 2 bits instead of 9 |
| `[Flags]` | bit mask from lowest flag to OR of all flags; shifted down when lowest flag not bit 0 | `[Flags] enum AccessHigh { B4 = 16, B5 = 32, B6 = 64 }`: `value >> 4`, 3 bits |

- RT: switch costs lookup each way → kept only where it makes field narrower (with or without null code); else subtraction.
- NOTE: `enum MID { ServerRegisterReq = 1001, … ServerEventRes = 1008 }` contiguous → `value - 1001`, 3 bits. `enum Collide { A = -1, B = 3, C = 7 }` → index 0, 1, 2, null code 3 (= value of `B`); accessors keep codes/values apart. One-of enum starting at 0 (`enum Mode { SPLIT = 0, MERGE = 1 }`) → code = value.
- WIRE: flags enum as wide as its mask: 8 flags `1 … 128` → 1 byte; 16 flags → 2 bytes; nullable 16-flag field → 17 bits.
- NOTE: numbering, underlying types, aliases, `bool` replacement →§6.4.

## §7.5 Absent values on the wire
KW: nullable, null bit, null code, NULL, spare code, absent, null bits byte, putNullKey, set1, Add(null)
- Nullable field = declared `?` or reference type (string, pack-typed, collection) →§5.7. When holding no value:

| Field | How "no value" is sent |
|:--|:--|
| bit field (span < 128): `bool?`, small `[MinMax]` range, small enum | one more code in field's bits: `bool?` 2 bits (0 false, 1 true, 2 null); `[MinMax(1_000, 1_009)] int?` 11 codes in 4 bits |
| any other nullable: wider numbers, strings, packs, collections | one bit in pack's null bits (1 = present); absent field sends nothing else |

- WIRE: null bits written in bytes of eight, each byte just before the fields it covers →§7.10.
- GEN: in memory generated code may still represent null of narrow-range field by spare code; published as field's `NULL` constant (`[MinMax(0, 254)] byte?` → `NULL = 0xFF`). Nullable `DateTime` reserves its top value →§5.20.1.
- RT: items of nullable collection: if null fits item's own type (`bool?`, enum, `[MinMax(1_000, 1_009)] int?`) → null = one more code, collection read/written as codes. If not — exactly at widths 8+1, 16+1, 32+1, 64+1 bits — widening every item would double collection, so items keep natural type and nulls kept separately, 1 bit/item, own API (`set1(i, null)`, `putNullKey`, `Add(null)`).
- PITFALL: only sentinel left there: bulk setter filling such list from plain array of wider type; in that array `NULL` = value outside data range.
- WIRE: inside Value Pack (every field = slot of bits) same question decides whether null = spare code of slot or one more bit:
```csharp
[MinMax(2_000_000, 2_000_510)] int? null_fits_9bit;       // 511 values + null = 512 codes: null fits the 9 bits
[MinMax(2_000_000, 2_000_511)] int? null_needs_10th_bit;  // 512 values fill 9 bits: null takes a 10th bit
[MinMax(2_000_000, 2_000_512)] int? null_is_spare_code;   // 513 values need 10 bits: null is a spare code in them
```

## §7.6 Varint: declaring where values cluster
KW: varint, [A], [V], [X], ZigZag, distribution, point of concentration, continuation bit, length bits

### §7.6.1 How a varint is written
KW: varint, 7-bit groups, continuation bit, high bit, length bits, bit fields, group 3
- WIRE: `varint` = number in 7-bit groups, lowest group first; each byte 7 bits payload + high bit "another byte follows". Leading all-zero groups not sent → cost follows magnitude of number sent, not declared type width: `long` carrying `3` = 1 byte, carrying `3,000,000,000` = 5 bytes.

| Bytes | Number sent (distance from base) |
|:--|:--|
| 1 | 0 … 127 |
| 2 | 128 … 16,383 |
| 3 | 16,384 … 2,097,151 |
| 4 | 2,097,152 … 268,435,455 |
| 5 | 268,435,456 … 34,359,738,367 |

- NOTE: compression = property of distribution, not type. Uniform over whole range → saves nothing, adds continuation bits. Clustered values save only if generator subtracts cluster point. Only description knows where (temperature delta small, monotonic counter unbounded, remaining lease just under ceiling) → distribution must be declared; agent checks declaration.
- WIRE: varint form inside bit fields: when pack also has bit fields, its non-nullable varint fields (group 3 →§7.10) use second form sharing those bytes: number of payload bytes goes among bit fields as 1–4 `length bits`; payload follows as that many whole bytes, little-endian. 16-bit field: 1 length bit (0: 1 byte follows, 1: 2 bytes) + 1 or 2 bytes. Cost still follows magnitude; length bits replace continuation bits.
- WIRE: pack without bit fields, and varint fields of a Value Pack → 7-bit groups.

### §7.6.2 The varint attributes [A], [V] and [X]
KW: [A], [V], [X], minMostProbableValue, maxMostProbableValue, amplitude, zero, ZigZag, hard limit
- DECL: three attributes declare distribution of integer primitive field (collection: integer items); each names point of concentration that generator subtracts:

| Attribute | Values cluster near | Sent on wire |
|:--|:--|:--|
| `[A(minMostProbableValue = 0, max = 0)]` | minimum, rare excursions up | `value - minMostProbableValue` |
| `[V(maxMostProbableValue = 0, min = 0)]` | maximum, rare excursions down | `maxMostProbableValue - value` |
| `[X(amplitude = 0, zero = 0)]` | center, excursions both ways | `ZigZag(value - zero)` |

- RULE: declared range = hard limit, not hint: value outside cannot be transmitted. Attribute adds where mass sits, i.e. which end is subtracted.
- DECL: `[A]`/`[V]`: point of concentration first, far bound second. `[X]` reversed: `[X(amplitude, zero)]` bound first (center defaults 0, most often omitted). `amplitude` = `ulong`, `zero` = `long`; other args `long`.
- NOTE: what goes on wire = distance from declared line; dense band costs 1 byte, sparse fringe more → line must sit where values cluster.
```csharp
[A]               uint? bytes_received; // 0 … uint.MaxValue - the far bound is derived from the type
[A(1_000, 9_000)] short chunk_len;      // 1 000 … 9 000 - 1 000 is the floor, not a hint
[V]            short? charge_deficit; // -32 768 … 0 - the far bound is derived from the type
[V(30_000, 0)] int    lease_seconds;  // 0 … 30 000 - normally close to the full lease
[X]        short? temperature_delta; // -32 767 … 32 767 around 0 - the amplitude is derived from the type
[X(1_000)] int    cursor_shift;      // -1 000 … 1 000 - small shifts either way cost one byte
```
- PITFALL: `[X]` amplitude = maximum deviation, not typical: range exactly `zero ± amplitude`. Sizing to typical swing = most common misuse. Arg order: `[X(50, 20_000)]` = ±50 around 20,000, not ±20,000 around 50.
- WIRE: ZigZag interleaves signs (`0, -1, 1, -2, 2, …` → `0, 1, 2, 3, 4, …`) so small deviation of either sign stays small (plain subtraction would make half negative; negatives have no leading zero groups).
- WIRE: AdHoc varint never carries sign: subtracting base always non-negative, `[X]` covers two-sided case → small negative never costs full 10 bytes of sign-extended 64-bit varint.

| Your values | Declare | On wire |
|:--|:--|:--|
| spread evenly over range | `[MinMax(min, max)]`, no varint | fixed bits/bytes |
| mostly near floor, sometimes much higher | `[A(floor, max)]` | `value - floor` |
| mostly near ceiling, sometimes much lower | `[V(ceiling, min)]` | `ceiling - value` |
| mostly near center, either way | `[X(maximum deviation, center)]` | `ZigZag(value - center)` |
| span fits one byte | `[MinMax]`; varint rejected →§7.6.5 | bit field or 1 byte |

### §7.6.3 Derived bounds and named arguments
KW: derived bound, far bound, named argument, max:, min:, zero:, amplitude, short.MaxValue, exTernal type recomputed
- DEFAULT: far bound omitted (2nd arg of `[A]`/`[V]`, 1st of `[X]`) → derived from declared type relative to point of concentration (range shifted away from type's own range):

| Declared type | `[A]` derives `max` | `[V]` derives `min` | `[X]` derives range |
|:--|:--|:--|:--|
| `short` | `short.MaxValue + min` | `short.MinValue + max` | `zero ± short.MaxValue` |
| `ushort`, `char` | `ushort.MaxValue + min` | `-ushort.MaxValue + max` | `zero ± ushort.MaxValue` |
| `int` | `int.MaxValue + min` | `int.MinValue + max` | `zero ± int.MaxValue` |
| `uint` | `uint.MaxValue + min` | `-uint.MaxValue + max` | `zero ± uint.MaxValue` |
| `long` | `long.MaxValue + min` | `long.MinValue + max` | `zero ± long.MaxValue` |
| `ulong` | `ulong.MaxValue + min` | `long.MinValue + max` | `zero ± long.MaxValue` |

- RULE: derived only when argument omitted; explicit `0` is a bound (`[V(30_000, 0)] int` = 0 … 30,000). Two args used as written; far bound on wrong side → swapped.
- NOTE: declared type = width available, not final range. `[A(1000)] short q;` = 1,000 … 33,767 (shift used up negative half). Values also below concentration point → pick attribute matching direction: `[A(-500, 1000)]` concentrates at -500; at 1,000 both ways → `[X(amplitude, zero: 1_000)]`; downward only → `[V(1_000, min)]`.
- RULE: derived range must fit 64 bits: `[A(n)] ulong` with n > 0, `[V(n)] long`/`ulong` with n < 0 exceed → stop: "The field … gets the range …, which no 64-bit integer holds." → name both bounds.
- GEN: exT recomputed from resulting range, either direction: `[X] uint u;` spans ±`uint.MaxValue` → signed 64-bit; `[X(1_000)] int i;` → -1,000 … 1,000 → 16 bits. Bare `[X]` on signed type keeps type: ±`short.MaxValue` = -32,767 … 32,767 fits `short`; range symmetric so `short.MinValue` not in it; on nullable field that spare code holds "no value". Bare `[X]` on `ulong` → field becomes signed `long`; values above `long.MaxValue` not transmittable.
- DECL: omit non-last arg via C# named args: `minMostProbableValue`/`max`, `maxMostProbableValue`/`min`, `amplitude`/`zero`:
```csharp
[A(max: 5_000)]  int   queue_depth; //       0 …  5 000 - the point of concentration stays at 0
[V(min: -5_000)] int   headroom;    //  -5 000 …      0
[X(zero: 1_000)] short level;       // -31 767 … 33 767 - the amplitude is derived: 1 000 ± short.MaxValue
```
- GEN: last line: amplitude from declared type (`short.MaxValue`), 33,767 no longer fits `short` → generated field `int`. Center + wider amplitude: `[X(ushort.MaxValue + 7, 4)] int?` = `4 ± 65,542`, stays `int`.

### §7.6.4 When varint loses
KW: varint overhead, base trap, uniform distribution, hash, Unix time, monotonic id, 268,435,455
- WIRE: 7-bit groups spend 1 bit in 8 on continuation → number needing full type width comes out longer than fixed field. (Figures for 7-bit form; bit-field form pays length bits instead.) Vs fixed 4-byte: varint wins while distance < 2,097,152, breaks even up to 268,435,455, loses beyond.
- PITFALL: base left at zero (`[A]` without args) on values never small → loses every packet:

| Field | Fixed | Varint |
|:--|:--|:--|
| uniform `uint` (hash, checksum, half UUID) | 4 | 5 for most values (+25 %) |
| uniform `ulong` | 8 | 9 or 10; ~half take 10 |
| Unix time seconds (~1.7 billion) in 32-bit field | 4 | 5 always |
| latitude magnitude ×10⁷ (55.75° = 557,500,000) | 4 | 5 for every latitude > 26.8° from equator |
| monotonic id past 268,435,455 | 4 | 5 from then on forever |

- PITFALL: `[A] uint request_id;` on counter past 268,435,455 pays 5th byte every packet.
- NOTE: fat tail not the problem: typical small (1 byte) vs outlier (5 bytes) → varint falls behind fixed 32-bit only when outliers > 3/4 of traffic. Loss cause = base in wrong place so no value ever small. Cluster around 400,000,000 spread 200 = 8 bits info; base 0 → 5 bytes every packet; with `[MinMax]` →§7.2 1 byte.
- RULE: four conditions, all declared not guessed:
  1. base sits where mass is;
  2. direction matches tail: up `[A]`, down `[V]`, both `[X]` (`[A]` on values hugging top puts every value a full span from base);
  3. typical distance after subtraction fits 1–3 bytes (4 = wash, 5 = loss);
  4. span exceeds 1 byte (else bit packing wins); agent enforces this one →§7.6.5.

### §7.6.5 The one-byte rule
KW: one-byte rule, span, inT, amplitude 127, rejected, [MinMax] instead
- RULE: span of transmitted number (`max - min` for `[A]`/`[V]`, `2 × amplitude` for `[X]`; = inT layer, not declared type) fits 1 byte → agent rejects: "... declares a value span that fits in a single byte, so varint has no leading zero bytes to skip ... Use `[MinMax(min, max)]` for a range this narrow". For `[X]`: amplitude ≤ 127.
```csharp
[A(1_000, 1_100)] int offset;          // hard range 1 000 … 1 100, span 100 - rejected
[X(50, 20_000)]   int sensor_reading;  // hard range 19 950 … 20 050, span 100 - rejected
```
- Two mistakes, opposite fixes:
  - range really narrow → offset + bit packing: `[MinMax(1_000, 1_100)] int offset; // 101 values: 7 bits on the wire`
  - PITFALL: typical deviation written where maximum belongs: `[X(50, 20_000)]` forbids 20,100; `[MinMax]` would make wall permanent. Declare real bound, keep center: `[X(2_000, 20_000)] int sensor_reading; // 18 000 … 22 000: typical readings 1 byte, rare swings 2`

### §7.6.6 Restrictions on varint attributes
KW: varint restrictions, integer types, header fields, [Key:], [Val:], TYPEDEF, ValueFor, collections

| Rule | Detail |
|:--|:--|
| integers wider than 1 byte only | `short`, `ushort`, `char`, `int`, `uint`, `long`, `ulong`. Refused on `byte`, `sbyte`, `bool` (inT spans 1 byte); on `float`, `double`, `string`, `Binary`, `DateTime`, `Stream`/`File` (needs integer) |
| one range attribute per field | `[A]`, `[V]`, `[X]`, `[MinMax]` mutually exclusive: `[MinMax]` = uniform, no compression; varint attrs = skewed, own bounds |
| not on header fields | header has fixed wire size →§8.6.7 |
| collections | applies to items; `[Key: …]`/`[Val: …]` target Map/Set key or Map value: `[Val: X] Map<uint, int> deltas;` ZigZags values; `[A, D(5)] uint[]`, `[X, D(5)] int?[,]` |
| `TYPEDEF` | declare attribute inside typedef, not on using field; alias carries distribution to every user |
| computed bounds | bound may be `[ValueFor]` constant named as a whole →§6.2 |

```csharp
[A]                  uint?  bytes_received;  // skewed low,  varint
[V]                  short? charge_deficit;  // skewed high, varint
[X]                  short? temperature;     // two-sided,   varint
[MinMax(-11, 75)]    short  celsius;         // uniform in range: no varint, a 7-bit field
[MinMax(-1128, 873)] byte   altitude_offset; // the range outgrows byte: the generated field is a short
```

## §7.7 Range constants in the generated code
KW: MIN, MAX, NULL, ZERO, field__, <field>__, Val.MIN, uuid_hi__, constants
- GEN: range published as constants in helper type `<field>__` (field name + 2 underscores); Map: one set for key, one for value.

| Constant | Meaning |
|:--|:--|
| `MIN`, `MAX` | inclusive range: declared, derived, or full type range |
| `NULL` | nullable fields whose "no value" is spare code: that code written as value |
| `ZERO` | varint fields: point of concentration, value that travels as 0: `min` for `[A]`, `max` for `[V]`, `zero` for `[X]` |

```csharp
[A(1_000)]      short chunk_len; // MIN = 1 000   MAX = 33 767   ZERO =  1 000
[V(30_000, 0)]  int   lease;     // MIN =     0   MAX = 30 000   ZERO = 30 000
[X(1_000, 50)]  int   drift;     // MIN =  -950   MAX =  1 050   ZERO =     50
[MinMax(-5, 5)] int   trim;      // MIN =    -5   MAX =      5
[MinMax(1, 7)]  byte? bits;      // MIN =     1   MAX =      7   NULL = 8
```
- GEN: all = exT values (what setter takes/getter returns); shifted/ZigZag/bit-packed forms never show. Bare `[V] uint` → `MIN = -4,294,967,295`, `MAX = 0` (not stored 0 … 4,294,967,295).
- NOTE: use to validate input before setter (outside `MIN … MAX` not transmittable) or size a slider, without copying numbers into hosts.
- GEN: every numeric primitive field has `MIN`/`MAX` in C#, Java, TS, Rust, Go, C++, C, full-type-range fields included. C# `Pack.field__.MIN`; Java one level deeper `Pack.field__.Val.MIN`; TS `Pack.field__.MIN`; Rust module in pack file `field__::MIN`; Go `Pack_Field___MIN`; C++ `Pack::field_::MIN`; C macro `gen·__·Pack·field·MIN`. C# for plain `ulong` (AdHoc `Login`):
```csharp
public partial struct uuid_hi__
{
    public const ulong MIN = 0x0;
    public const ulong MAX = 0xFFFF_FFFF_FFFF_FFFFUL;
}
```

## §7.8 Strings and lengths on the wire
KW: string, UTF-16, code unit, varint, UTF-8, [D(+N)], length field, _DefaultMaxLengthOf, collection length
- WIRE: `string` = varint count of UTF-16 code units, then each code unit as varint 1–3 bytes. No UTF-8 conversion inside a pack: sender writes native string, receiver rebuilds, no decode step.

| Characters | AdHoc bytes | UTF-8 bytes |
|:--|:--|:--|
| U+0000 … U+007F (ASCII) | 1 | 1 (same bytes) |
| U+0080 … U+07FF (Latin supplements, Greek, Cyrillic, Hebrew, Arabic) | 2 | 2 |
| U+0800 … U+3FFF (Devanagari, Thai, kana, …) | 2 | 3 |
| U+4000 … U+FFFF (CJK ideographs, Hangul, …) | 3 | 3 |
| above U+FFFF (emoji, non-BMP) | 6 (2 code units × 3) | 4 |

- NOTE: pack is framed (receiver reads exactly pack bytes; lost byte invalidates frame) → strings need no self-synchronizing encoding.
- RULE: `[D(+N)]` limit counts UTF-16 code units; emoji counts 2 →§5.10.
- NOTE: receiver needing raw UTF-8 bytes (store, relay, write file) → trim sends field as UTF-8 on that leg →§17.6.
- WIRE: collection carries length before items, except constant-length array (`T[]` with `[D]`): length never travels →§5.9. Length of array/list = fixed-width field sized by declared max:

| Declared max length | Length field |
|:--|:--|
| ≤ 255 | 1 byte |
| ≤ 65,535 | 2 bytes |
| ≤ 16,777,215 | 3 bytes |

- WIRE: empty collection → zero length; absent → only its null bit.
- DEFAULT: max of field without `[D]` from `_DefaultMaxLengthOf` (255 unless changed) →§5.11.

## §7.9 Value Packs
KW: Value Pack, 64 bits, single integer, no allocation, 53 bits, PositionPack, StatusFlags, carrier
- `Value Pack` = pack whose fields fit together in 64 bits; generator collapses it into one integer: no object, no heap allocation, no indirection. C#, Java, Rust, Go, C++, C always; TypeScript while pack fits 53 bits. No marker: generator recognizes it.
```csharp
class PositionPack      // 48 bits: a Value Pack
{
    float x;            // 32 bits
    byte  layer;        //  8 bits
    byte  flags;        //  8 bits
}
class StatusFlags       // 6 bits: a Value Pack
{
    [MinMax(0, 7)] byte priority;  // 3 bits
    bool                is_leader; // 1 bit
    [MinMax(0, 3)] byte mode;      // 2 bits
}
```

### §7.9.1 What makes a pack a Value Pack
KW: Value Pack criteria, field widths, nullable, 65 bits, Point2, Point3, estimate
- RULE: every field one of these, widths sum ≤ 64 bits:

| Field kind | Width |
|:--|:--|
| `bool` | 1 bit |
| integers, `float`, `double` | type width, or fewer with `[MinMax]` |
| `char` | 16 bits |
| enum | width of its code |
| `DateTime` | 64 bits |
| another Value Pack | its own width |

- RULE: nullable field needs room for null: spare code in its bits if any, else 1 more bit.
- RULE: strings, arrays, other collections, `Binary`, `Stream`/`File`, self-containing packs never make a Value Pack.
- NOTE: agent's own estimate = each field at declared width + 1 bit per nullable; generator decides finally with narrowed widths → pack brought under 65 bits only by `[MinMax]` is a Value Pack too.
```csharp
class Point2 { float x; float y; }           // 64 bits: a Value Pack; as an array item it is one number
class Point3 { float x; float y; float z; }  // 96 bits: an ordinary pack; as an item it is a nested object
class Over_64bit_by_one { float? f32; int i32; } // 33 + 32 = 65 bits: not a Value Pack
```
- NOTE: values too wide for Value Pack and sent alone (3 `float` coords) can travel without object as header fields of empty pack →§8.6.6.
- NOTE: Value Pack usable as field, array item, Map key/value, Set item. Field injected into Value Pack →§8.5 joins its bits; stays Value Pack while it fits.

### §7.9.2 How a Value Pack is laid out
KW: slot, carrier integer, code, widest first, Progress, pack id, varint after fixed bits
- WIRE: in carrier integer each field = fixed slot of bits holding code (`value - min`). Slots ordered by width, widest first; same width → declaration order.
- NOTE: AdHoc `Progress` = 23 bits in 32-bit unsigned carrier: `percent` bits 0–6, `lang` 7–11, `what` 12–15, `level` 16–18, `pass` 19–20, `passes` 21–22 (`pass`, `passes` stored as value − 1), bits 23–31 unused. Wire: pack id byte + 3 bytes. C# partial struct with `uint Value` and properties; Java `int` tagged `@Progress` with static get/set groups; TS `number` with merged namespace; all same bytes.
- WIRE: Value Pack sent alone = pack id + fixed bits rounded up to whole bytes (`Progress`: 3 bytes). Varint fields of Value Pack travel after fixed bits, each own 7-bit-group varint: `StatusFlags` + injected `[X] short event_code` → 6 fixed bits in 1 byte, then `event_code` as ZigZag varint.
- WIRE: nullable field in Value Pack uses spare code of slot where one exists: 3-bit enum with 5 values is null when slot holds 5.

### §7.9.3 Smart flattening
KW: smart flattening, single-field pack, wrapper, nullable chain, Set<float?>
- GEN: pack of single field that is Value Pack disappears into using field; field takes inner value directly.
```csharp
class Temperature   { float celsius; }
class SensorReading { Temperature measurement; }   // generated as: float measurement
class Pressure       { float kilopascals; }
class PressureSensor { Pressure? reading; }        // generated as: float? reading - the null survives
class Voltage      { float volts; }
class PowerLevel   { Voltage? level; }
class DeviceStatus { PowerLevel? power; }          // a chain flattens too: power is a nullable float
```
- RULE: nullability survives: any nullable link → flattened field nullable. Each nullable link keeps own null code in field's bits → on wire "no power level" ≠ "power level without voltage" for `DeviceStatus.power`.
- GEN: with `class FloatWrapper { float v; }`, `class FloatWrapperNullable { float? v; }`: `Set<FloatWrapper?>`, `Set<FloatWrapperNullable>`, `Set<FloatWrapperNullable?>` all → `Set<float?>`.

### §7.9.4 The generated Value Pack in each language
KW: partial struct, Value, IEquatable, repr(transparent), Progress::Nullable, Progress_Nullable, Ulong, to_u64, @interface, type-use annotation, carrier primitive, namespace, SlimEnum, get, set, hasValue, to_null, New, jso, Pack.Nullable, bigint, extension type, Pack_get, Pack_Nullable, copy.replace

| Target | Representation | Allocation |
|:--|:--|:--|
| C# | mutable `partial struct` over one unsigned integer `Value`, property per field | none: value type |
| Java | type-use `@interface` tagging carrier primitive; static accessors in nested interfaces | none: only primitives |
| TypeScript | `type Pack = number` merged with `namespace Pack { ... }` of accessor functions | none while ≤ 53 bits |
| Rust | `#[repr(transparent)] pub struct Pack { pub Value: u32 }` (`Copy`), getter `field()` + setter `set_field(v)` per field | none: `Copy` value |
| Go | `type Pack struct { Value uint32 }`, methods `Field()`, `SetField(v)` per field | none: value type |
| C++ | `struct Pack final` over `Value`, overloaded `field()` / `field(v)` per field (`constexpr`) | none: value type |
| C | `struct gen·__·Pack { uint32_t Value; }`, inline functions `gen·__·Pack·field(&p)`, `gen·__·Pack·set_field(&p, v)` | none: value type |
| Swift | `public struct Pack { public var value: UInt32 }` (`Hashable`, `BitwiseCopyable`), getter `field()` + setter `set_field(v)` per field; `Pack.Nullable` for pack absent as a whole | none: value type |
| Dart | `extension type const Pack.raw( int _v ) implements int`; getter per field, `with_field(v)` returns changed pack, constructor `Pack( field: … )`; `Pack_Nullable` for pack absent as a whole | none: an `int` |
| Python | immutable object over the C value: read-only property per field; `copy.replace( v, field = … )` makes another | a Python object (C core keeps the integer) |

- WIRE: all of them write identical bytes.
- GEN: C#: struct wraps unsigned integer `Value` wide enough (`uint Value` for 23-bit `Progress`, `ulong Value` for 64-bit). Constructor from carrier (`new Progress(bits)`), implicit conversions to/from carrier, implements `IEquatable<T>`, `==`/`!=` also work on `Progress?`. Field = property:
```csharp
var progress = new Progress();
progress.what    = 2;                 // "compiling"
progress.percent = 40;
progress.pass    = 1;
uint bits = progress;                 // implicit conversion to the carrier
string text = Progress.TEXT[progress.what];
```
- GEN: Java: no user value types → no instance ever created. Pack = annotation on primitive holding it (`@Progress int`); carrier = smallest Java primitive that fits: `boolean`, `byte`, `char`, `int`, `long`. Unsigned 8- and 16-bit fields map to `char`. Field access = static method in nested interface (group) of annotation (annotation cannot hold static method): groups `get`, `set`, `hasValue`, `to_null`, `New`, `jso`.
- NOTE: SlimEnum IntelliJ plugin (https://github.com/cheblin/SlimEnum, https://plugins.jetbrains.com/plugin/10316-slimenum) reads these `@interface` tags (Value Packs and enums) → IDE treats plain `long` as typed enum/flag set: context-aware completion, switch-case narrowing, OR-combined flag awareness, invalid value detection.
- GEN: TypeScript: pack is a number, `type Pack = number`; merged `namespace Pack` holds groups `get`, `set`, `hasValue`, `to_null`, `New`, `jso`, one function per field per group doing bit arithmetic. Pack wider than 53 bits → class instance. "No value" for whole pack = out-of-range sentinel as literal type → TS narrows automatically.
- GEN: Rust, Go, C++, C: struct of one unsigned integer `Value` (as C#), copied by value. Field getter/setter: Rust `percent()`/`set_percent(v)`; Go `Percent()`/`SetPercent(v)`; C++ `percent()`/`percent(v)`; C `gen·__·Progress·percent(&p)`/`gen·__·Progress·set_percent(&p, v)`. Nullable field adds `percent_hasValue()`, `percent_to_null()` (Go `Percent_hasValue()`, `Percent_to_null()`). Pack ↔ wire code: Rust `From` with carrier and `u64`; Go `p.Ulong()`, `Progress_Ulong(code)`; C++ `explicit` conversions; C `·to_u64`/`·from_u64`. Pack absent as a whole: `Progress::Nullable` (Go `Progress_Nullable`, C `gen·__·Progress·Nullable`) = same number with one reserved "no value" code; `hasValue()`, `to_null()` (Go `HasValue()`, `To_null()`).
- GEN: Swift: struct of one unsigned integer `value` (lowercase), copied by value; `field()` / `set_field(v)` per field; `init(from: UInt64)` from wire code. Absent as a whole: `Pack.Nullable`: `Pack.Nullable.of( pack )`, `Value()`, `hasValue()`, `to_null()`, `optional` (= `Pack?`).
- GEN: Dart: pack = `int`: `extension type const Pack.raw( int _v ) implements int`; fields read as C# struct properties: `pack.percent`; pack immutable → `pack = pack.with_percent( 40 )`; make `Progress( what: 2, percent: 40, pass: 1 )`, start `Pack.EMPTY_PACK`; nullable field adds `percent_hasValue`, `percent_to_null()`; `toJSON()` JSON text. One-field pack whose field holds Dart `null` = `typedef Pack = Pack_?`: members in `extension Pack$ on Pack`, made by `Pack_( field: … )`. Pack absent as a whole (C# `Pack.Nullable`, no nested types in Dart) = `Pack_Nullable`: `Pack_Nullable( pack )`, `maybe.hasValue`, `maybe.Value`, `Pack_Nullable.NULL`.
- GEN: Python: immutable object over the C value: read-only property per field; fields = constructor keywords and `__match_args__`; `copy.replace( pack, field = value )` makes changed pack.
- GEN: TS one-field pack with range (e.g. `[MinMax(400_000_000, 400_000_193)] int`) kept in TS collections and sent as its code `value - MIN` (1 byte here). `Pack.Nullable.get( code )` → value; `Pack.Nullable.set( value )` → code. Range near end of `long` → value is `bigint` (exceeds 53 bits), code stays `number`.
- GEN: in abstract (`-`) pack →§10.3.2 user-implemented accessors give/take such field as code (`number`, what travels): no shift, no per-item `bigint`; `Pack.Nullable.get`/`set` convert where value needed. C#/Java: those accessors give/take pack itself (value type, shift free).

### §7.9.5 Working with a Value Pack in Java and TypeScript
KW: Pack.get.field, Pack.set.field, Pack.hasValue, Pack.to_null, Pack.New.of, EMPTY_PACK, Pack.jso.n, New.str, JsonWriter, JSON_EXT
- GEN: call shape same in both: pack, group, field:

| What | Call |
|:--|:--|
| read field | `Pack.get.field( pack )` |
| write field | `pack = Pack.set.field( pack, value )` |
| field holds value? | `Pack.hasValue.field( pack )` |
| make field hold none | `pack = Pack.to_null.field( pack )` |
| make pack | `Pack.New.of( field, field, ... )` |
| empty pack start (Java) | `Pack.EMPTY_PACK` |
| pack as JSON | `Pack.jso.n( pack )`, also `Pack.New.str( pack )` |

```java
@PositionPack long pack = PositionPack.New.of( 1.5f, (char) 3, (char) 4 ); // x, layer, flags: the order of the description
pack = PositionPack.set.x( pack, 2.5f );
float x = PositionPack.get.x( pack );
System.out.println( PositionPack.jso.n( pack ) );
```
```typescript
let pack = PositionPack.New.of( 1.5, 3, 4 );
pack     = PositionPack.set.x( pack, 2.5 );
const x  = PositionPack.get.x( pack );
console.log( PositionPack.jso.n( pack ) );
```
- PITFALL: pack first, value second, setter returns pack; nothing changes in place: `Pack.set.field( pack, value )` without `pack =` is lost. Exception: TS pack > 53 bits = object; setter mutates and returns same object → same code line works both ways.
- RULE: `Pack.New.of` takes fields in declaration order, not bit order (`Progress.percent` at bit 0 is 5th arg). Null field: afterwards `Pack.to_null.field( pack )`.
- GEN: `hasValue`, `to_null` exist only when pack has nullable field.
- GEN: one-field pack has same groups (code unchanged when 2nd field added); its setter takes value alone: `Pack.set.field( value )`.
- GEN: JSON: Java text from runtime `JsonWriter`. TS `JSON.stringify`; `bigint` written as digits + `n` (as `AdHoc.JSON_EXT()` writes), whether or not `JSON_EXT()` called.
- GEN: doc comment of every Value Pack lists these calls with pack/field names, ready to copy: making, reading, writing, JSON, whole-pack absent form.
- NOTE: former accessors `Pack.field__.Val.get( pack )` (Java), `Pack.field__.get( pack )` (TS) no longer generated; `Pack.field__` keeps only constants `MIN`, `MAX`, `NULL`.

### §7.9.6 A Value Pack that may be absent
KW: nullable Value Pack, Pack.Nullable, NULL, hasValue, to_null, @Pack.Nullable, Pack.Nullable.value, sentinel
- GEN: field typed nullable Value Pack → generator picks out-of-range carrier code, publishes as `NULL`:
  - C#: nested struct `Pack.Nullable`: holds carrier (`value`), converts to pack (`Value`), has `hasValue`, `to_null()`, constant `NULL`.
  - Java: annotation `@Pack.Nullable` marks primitive that may hold no pack; group `Pack.Nullable.value` has `set( pack )`, `to_null()`, `hasValue( maybe )`, `get( maybe )`; constant `Pack.Nullable.NULL`. Nullable form may need wider primitive: 32-bit pack in `int` → `long` when nullable, `NULL = 0x1_0000_0000L`.
  - TypeScript: literal-type sentinel →§7.9.4.

## §7.10 The layout of a pack on the wire
KW: wire layout, pack id, field groups, fixed-width, bit fields, varint, null bits, conduits, no tags, position, Reading
- WIRE: pack = pack id →§20.3.2, header fields if any →§8.6.4, then fields back to back. Pack as field of another pack written in place, no pack id. No length before pack (receiver knows from generated code). Field has no number/tag; recognized only by position.
- WIRE: field-order algorithm — fields written in groups; inside each group description order:

| Group | Fields | Written as |
|:--|:--|:--|
| 1. fixed-width primitives | non-nullable numbers and `char`s that are not bit fields | their bytes, little-endian |
| 2. bit fields | `bool`s, small `[MinMax]` ranges, small enums, their nullable forms | packed together, lowest bits first, rounded up to whole bytes |
| 3. varint fields | `[A]`, `[V]`, `[X]` fields | one varint each; when pack has bit fields, length bits continue bit-field bytes, payload bytes follow →§7.6.1 |
| 4. nullable and reference fields | strings, pack-typed fields, collections, nullable fields whose null is not a code | byte of null bits before every 8 of them, then each present field |
| 5. conduits | `Stream`, `File` fields | after everything else |

- WIRE: groups 1–3 hold single primitive fields: numbers, booleans, chars, enums, Value Packs used as fields.
- WIRE: pack of primitive fields only → fixed-width fields written widest first; same width → declaration order.
- WIRE: composed pack → own fields first, inherited and injected after, order per →§8.4.
- Worked example:
```csharp
class Reading
{
    string?                 label;     // group 4: null bit, then varint count + characters
    ushort                  sensor;    // group 1: 2 bytes
    [MinMax(-11, 75)] short celsius;   // group 2: 87 values, 7 bits
    bool                    alarm;     // group 2: 1 bit
    [X(2_000, 20_000)] int  pressure;  // group 3: ZigZag(value - 20 000), a length bit among the bit fields, then its bytes
    uint?                   sequence;  // group 4: null bit, then 4 bytes
    [D(16)] ushort[,]       samples;   // group 4: null bit, then a 1-byte length and 2 bytes per item
}
```
- WIRE: `label = "ok"`, `sensor = 513`, `celsius = 20`, `alarm = true`, `pressure = 20 001`, `sequence` absent, `samples = [1, 2]` → bytes after pack id:

| Bytes | Field | Why |
|:--|:--|:--|
| `01 02` | `sensor` | 513 = 0x0201, little-endian |
| `9F` | `celsius`, `alarm` | celsius code 20 − (−11) = 31 in bits 0–6, alarm bit 7 |
| `00` | `pressure` length | bit fields continue into 2nd byte: length bit 0 → 1 payload byte |
| `02` | `pressure` payload | 20 001 − 20 000 = 1, ZigZag(1) = 2 |
| `05` | null bits | label present (bit 0), sequence absent (bit 1), samples present (bit 2) |
| `02 6F 6B` | `label` | 2 code units, then `o`, `k` |
| `02 01 00 02 00` | `samples` | length 2 in 1 byte (max 16), then two `ushort`s |

- WIRE: 14 bytes; `sequence` costs only its null bit. 8 bits of `celsius`+`alarm` fill first bit-field byte → `pressure` length bit opens second; with 6-bit `celsius` all three would share one byte.

### §7.10.1 Why reordering fields changes the protocol
KW: field order, reorder, protocol change, wire compatibility, Login, uuid_hi, uuid_lo, regenerate
- RULE: fields not numbered → order is part of wire format, like types. Generator moves field only into its group (and, in pack of primitives only, among widths); never sorts by name; same group + same width → description order, primitives included.
- PITFALL: two fields of one group swapping places = different pack on wire. Swap `ulong uuid_hi;` / `ulong uuid_lo;` in AdHoc `Login` → hosts read each other's halves. Every sending/receiving host must be regenerated and deployed together.
- NOTE: field moved past fields of another group changes nothing (primitive moved past string stays in its group, same place).
- NOTE: moving whole declarations (packs, hosts, connections) changes nothing: identified by numbers, not position →§20. Sorting pack fields by name or grouping by meaning = protocol change.
- NOTE: which edits change wire, new protocol generations, `.cs~` report naming every reached host →§21, §21.4.

# §8 Building packs from packs
KW: composition, payload fields, header fields, FieldsInjectInto, HeaderFor, _<>, X<>, see cref, inheritance
SUMMARY: how a pack takes fields from other packs (inheritance, `_<(...)>`, XML `<see cref>` directives, `FieldsInjectInto`), resulting field order; pack headers `HeaderFor` and `Modify<Header>`.
- Two kinds of attached fields:
  - `payload fields`: part of pack; inherited (C# inheritance, `_<(...)>`), imported/removed by XML directives (`<see cref="..."/>+`, `-`), injected into pack set by `FieldsInjectInto`. In generated pack class, travel in payload like body fields.
  - `header fields`: protocol metadata declared with `HeaderFor`; not in pack class; connection sends them between pack id and payload, only when pack sent on its own.

## §8.1 How a pack's field list is assembled
KW: field list, four steps, name occupation, shared fields, native fields, injection, XML directives, base list, cycle
- RULE: Agent builds every pack's field list in 4 steps, fixed sequence.
- RULE: name occupation: name in list = taken; later step bringing same name skips it, whatever its type. Only exception: field injection replaces existing field.
- RULE: shared fields, not copies: imported/inherited field = same definition as in source pack; source is only definition place. Changing type/attributes/doc in source changes every pack that took it. IDE rename refactoring also renames `cref` pointing to it.

| Step | Source | Field whose name already in list |
|:--|:--|:--|
| 1 | native fields (class body) | - (come first) |
| 2 | field injection: every `FieldsInjectInto` whose target set contains pack | injected field replaces it |
| 3 | XML directives in pack's doc comment, top to bottom: `+` adds, `-` removes | skipped when added |
| 4 | base list, left to right: C# base class, then `_<...>` items; `X<...>` removes | skipped |

- NOTE: rule = first field with a name wins, except injection which replaces. Generator works from resulting list; on wire grouped by kind, order holds inside groups →§7.10.
- NOTE: `Modify<Pack>` in another project can change list afterwards (adapting reused pack) →§22.3.
- RULE: composition references may form cycle (A imports field of B, B inherits A): Agent detects, resolves in second pass; not an error.

## §8.2 Importing and removing fields with XML directives
KW: XML directive, <see cref/>, +, -, doc comment, import field, cref rename, Update_score
- DECL: `XML directive` = `<see cref="..."/>` in doc comment directly above pack, mark right after `/>`: `+` → adds, `-` → removes. No mark, or space before it → plain reference (documentation), changes no field; generated docs read mark same way. Run in step 3: after native/injected, before base list.

| Directive | Effect |
|:--|:--|
| `/// <see cref="Pack.field"/>+` | adds field unless name taken |
| `/// <see cref="Pack"/>+` | adds every field and constant of `Pack`, each subject to name occupation |
| `/// <see cref="Pack.field"/>-` | removes field if list holds it at this moment |
| `/// <see cref="Pack"/>-` | removes every field of `Pack` list holds at this moment |

- RULE: top to bottom; first occurrence of name wins. Text after mark on same line ignored: `/// <see cref="Player.id"/>+ // the key` OK.
- Typical: small update pack carrying few fields of big one:
```csharp
class Player
{
    /// <summary>Unique ID</summary>
    public int id;
    /// <summary>Current game score</summary>
    public int score;
    /// <see cref="Player.id"/>+
    /// <see cref="Player.score"/>+
    class Update_score { }
}
```
- GEN: `Update_score` → `{ int id; int score; }`; fields bring their documentation. After IDE rename `score` → `experience` and type change to `long` (with `Update_score` untouched; IDE renamed `cref` to `Player.experience`) → `{ int id; long experience; }`.
- PITFALL: mark goes immediately after `/>`: `<see cref="Common.id"/> -` (space) = plain reference, removes nothing. Directive naming host or connection stops run: "The pack … (…) takes …, which is neither a pack, nor a field, nor a constant: a pack takes or removes only those.". How directives/plain refs look in generated docs →§4.6.

### §8.2.1 `-` removes a field that is already in the list
KW: -, remove field, identity, order of directives, X<>, replace imported field, Ticket, LoginPacket
- RULE: `-` is not a name block: removes that very field definition if list holds it when directive runs. Consequences:
  - put `-` after the `+` that brought field; `-` above it removes nothing, then `+` adds.
  - `-` cannot keep out base-list field (arrive step 4, after all directives). Drop inherited field with `X<...>` in base list, or import selectively with `+` instead of inheriting.
  - removes only named field, by identity; same-name field from different pack stays.
- NOTE: remove then import same name from other pack = replace; new field goes to end of list:
```csharp
class Common
{
    public int    id;
    public int    version;
    public string debug_tag;
}
class WideId
{
    public long id;
}
/// <see cref="Common"/>+
/// <see cref="Common.id"/>-
/// <see cref="WideId.id"/>+
class Ticket { }               // { int version; string debug_tag; long id; }
```
- Common patterns (`Common` above):
```csharp
class SessionInfo
{
    public string token;
    public long   expires;
}
// A native field wins: Common.version is skipped, although its type differs.
/// <see cref="Common"/>+
class Release
{
    public string version;     // { string version; int id; string debug_tag; }
}
// Import a pack, drop one of its fields, import another pack.
/// <see cref="Common"/>+
/// <see cref="Common.debug_tag"/>-
/// <see cref="SessionInfo"/>+
class LoginPacket { }          // { int id; int version; string token; long expires; }
// Change the type of an imported field: declare it natively.
/// <see cref="Common"/>+
class BigIdPacket
{
    public long id;            // { long id; int version; string debug_tag; }
}
```
- NOTE: in `BigIdPacket` a `<see cref="Common.id"/>-` changes nothing: native `long id` occupies name, `Common.id` never enters list.

## §8.3 Inheriting fields: C# inheritance and `_<(...)>`
KW: inheritance, base class, _<(...)>, X<...>, leftmost wins, shared fields, Transfer, TransferLite
- DECL: inheritance brings all fields of another pack in step 4; shortest way to give many packs same field group. C# base class = single-inheritance form:
```csharp
class Root                     // shared fields; no branch sends Root itself
{
    long id;
    long hash;
    long order;
}
/// <see cref='InCS'/>
struct BackendServer : Host
{
    public class ReplyInts : Root
    {
        [D(300)] int[] reply;
    }
}
```
- RULE: `ReplyInts` list = `{ reply, id, hash, order }`: native first, base after (step 4).
- NOTE: AdHoc protocol: `class Constants : Entity { [MinMax(0, 0xFFFF - 1)] ushort? parent; [D(65_000)] int[,] constants; }` with `class Entity { string name; max_65_000_chars doc; string inline_doc; }` → order `parent, constants, name, doc, inline_doc`.
- DECL: several sources → Meta interface `_<...>` with tuple of packs; C# base class (if any) first:
```csharp
class Tracing  { long trace_id; long span_id; }
class Audit    { long user_id;  long stamp; }
class Envelope { long stamp;    int  hops; }
class Transfer : Tracing, _<(Audit, Envelope)>
{
    long amount;
}
// { amount, trace_id, span_id, user_id, stamp, hops }  - stamp is Audit's: the leftmost source wins
```
- RULE: base list left to right; same name from several sources → leftmost wins (`Envelope.stamp` skipped).
- DECL: `X<...>` in base list removes every field of named pack the list holds at that point; works across levels (fields shared, not copied):
```csharp
class TransferLite : Transfer, X<Tracing> { }      // { amount, user_id, stamp, hops }
```
  `trace_id`, `span_id` inherited via `Transfer` are the very fields of `Tracing` → removed.
- RULE: native field with inherited name replaces inherited one. AdHoc protocol: `class View : XY { int x; int y; int w; int h; ... }` redeclares base coordinates.
- GEN: derived pack's generated type holds all composed fields; code uses them like native. C# base class travels with derived pack: included in generated code of every host sending/receiving derived pack; it is ordinary pack, a branch may send it alone. Packs in `_<...>` are only field sources, not pulled in.

## §8.4 The final field order of a composed pack
KW: field order, composed pack, injected order, injector order, wire format, Order example
- RULE: final order:
  1. native fields, minus those an injector replaced;
  2. injected fields, in injector declaration order; several injectors in their declaration order;
  3. fields added by XML directives, top to bottom;
  4. base-list fields, left to right.
- RULE: field replaced by injector or header modifier moves to end of its group.
```csharp
class Tracing { long trace_id; }
class Common  { int id; int version; }
class Stamp : FieldsInjectInto<Order>
{
    long stamp;
}
/// <see cref="Common"/>+
class Order : Tracing
{
    int    version;
    string note;
}
// Order: { version, note, stamp, id, trace_id }
```
- NOTE: `version`, `note` native; `stamp` injected; `Common` adds `id`, skips taken `version`; `Tracing` adds `trace_id`. If `Stamp` also declared `int version;` after `long stamp;` → injected `version` replaces native, lands among injected: `{ note, stamp, version, id, trace_id }`.
- PITFALL: generator works from this list; on wire groups by kind, order holds inside each group →§7.10. Moving a directive, reordering base list or adding injector changes protocol.

## §8.5 Field injection: `FieldsInjectInto`
KW: FieldsInjectInto, field injector, template, PackSet, appended, event_code, target set, @MyProject
- DECL: `field injector` = class implementing `FieldsInjectInto<PackSet>`; template: its fields added to every data pack in `PackSet`; injector itself not a pack. For fields a pack family must carry in payload (request id, event code).
```csharp
class CommonFields : FieldsInjectInto<_<(MyProject, X<Point2d>)>>
{
    string name;
    int    length;
}
class Point2d { float X; float Y; }
class Point3d { float X; float Y; float Z; }
```
- RULE: target = every data pack of `MyProject` outside hosts, minus `Point2d`. `Point2d` stays `{ X, Y }`; `Point3d` → `{ X, Y, Z, name, length }`: injected fields appended after native.
- DECL: `PackSet` = Pack Set expression →§9: pack, Named Pack Set, project/host/pack scope with or without `@`, `_<...>`, `X<...>`, filter template, or tuple of these. Tuple works without `_<...>` wrapper.
```csharp
class CommonPayloadFields : FieldsInjectInto<Headers>
{
    [X] short event_code;
}
```
- RULE: injected fields = ordinary payload fields: every field type and attribute allowed (`[X]` here). Header fields much stricter →§8.6.7.
- RULE: targets = every data pack in set, including packs used only as field types and Value Packs (live project: Value Pack `StatusFlags` gets `event_code`). Never targets: enums, constants sets, typedefs, `DateTimeDef`, `TimeSpanDef`, `Duration` definitions, headers, header modifiers, other injectors.
- RULE: name clash: target already has field with injected name → injector's definition (type, attributes, doc) replaces it; field takes place among injected fields, after remaining native. Injection = step 2, before XML directives and base list → injected name wins over those.

| Rule | What happens |
|:--|:--|
| injector is not a pack | no wire id, no generated class; naming as branch payload stops run: "Type '...' is a FieldsInjectInto (Template) and not a transmittable message type." |
| injector's own fields | only fields declared in its body injected; its base list and `<see cref>` refs in doc comment edit its target set, not its fields |

- PITFALL: injector changes wire format of every target, incl. nested sub-packs and Value Packs no branch sends directly.
- PITFALL: project scope without `@` holds packs outside hosts only: `FieldsInjectInto<MyProject>` misses packs declared in hosts; use `FieldsInjectInto<@MyProject>` →§9.4.

## §8.6 Pack headers: `HeaderFor`
KW: pack header, HeaderFor, metadata, routing, sequence number, session id, connection, header fields
- `pack header` = group of fields with protocol metadata (routing, sequence numbers, session id, timestamp) traveling before pack payload, not part of pack. Declared once for set of packs. Connection sends fields after pack id, before payload; code can read them before payload parsed.
- GEN: headers sit on connection, not pack: generated pack class has no header fields; same pack object can be sent with different header values.

### §8.6.1 Declaring a header and where its declaration sits
KW: HeaderFor<PackSet>, header scope, project scope, host scope, connection scope, direction
- DECL: class implementing `HeaderFor<PackSet>`; its fields = header fields. `PackSet` = Pack Set expression →§9: one pack, tuple, recursive `@` scope, Named Pack Set, filter template, `X<...>` exclusions.
- RULE: declaration place sets scope (on which transmissions it travels):

| Declared | Header travels on its target packs |
|:--|:--|
| directly in project | both directions, every connection carrying pack |
| inside host `H` | only on way to `H` (H receiving) |
| inside connection | both directions, only that connection |

- RULE: connection body accepts no class but `HeaderFor` header →§11.2.1.
- NOTE: host-scope header = what host expects in front of packs it receives: header declared in `NodeA` travels on packs `NodeB` sends to `NodeA`, not packs `NodeA` sends.

### §8.6.2 Several headers on one pack
KW: stacked headers, no override, ProjectScopeHeader, RecursiveHeader, NodeAHeader, ChannelScopeHeader, @PlayerAction
- RULE: headers never override each other: every header whose scope matches transmission and whose target set contains pack travels, each as own field group. Project + host + connection headers on one pack all travel.
- Example (reduced from live test project):
```csharp
using org.unirail.Meta;
namespace com.company.demo {
public interface Headers {
    class max_100_chars_string { [D(+100)] string TYPEDEF; }
    // Project scope: every pack of the project outside hosts, except ServerStatus.
    class ProjectScopeHeader : HeaderFor<(Headers, X<ServerStatus>)> { public ulong timestamp; public uint session_id; }
    // Project scope: the packs nested in PlayerAction (not PlayerAction itself), except ChatMessage.
    class RecursiveHeader : HeaderFor<(@PlayerAction, X<PlayerAction.GameStateUpdate.ChatMessage>)> { public uint transaction_id; }
    class PrecedenceTestPacket { max_100_chars_string test_data; }
    class TeamCoordination     { public uint team_id; }
    class PlayerEvent          { public byte event_type; }
    class ServerStatus         { public uint uptime; }
    class PlayerAction { public byte action_type;
        public class GameStateUpdate { public ushort state_id;
            public class ChatMessage { public ulong timestamp;
                public class AuthRequest { [D(32)] byte[] auth_token; } } } }
    /// <see cref = 'InCS'/>
    struct NodeA : Host {
        // Host scope: on these packs when they are sent to NodeA.
        class NodeAHeader : HeaderFor<(PlayerEvent, PrecedenceTestPacket)> { public uint host_specific_id; [MinMax(0, 31)] byte flags; /* 5 bits */ } }
    /// <see cref = 'InJAVA'/>
    struct NodeB : Host {
        // Host scope: on PlayerEvent when it is sent to NodeB.
        class NodeBHeader : HeaderFor<PlayerEvent> { public uint sequence_id; bool is_urgent; } }
    interface CommunicationChannel : Connects<NodeA, NodeB> {
        // Connection scope: both directions, on this connection only.
        class ChannelScopeHeader : HeaderFor<(TeamCoordination, PrecedenceTestPacket)> { public uint sequence_num; public ushort priority; bool flag_bit; }
        interface Actor2 : Actor {
            [_____lr_____<@Headers>]       // either side may send every pack of the project
            struct Start { } } }
} }
```
- GEN: resulting headers per pack (both hosts):

| Pack | NodeA → NodeB | NodeB → NodeA |
|:--|:--|:--|
| `PrecedenceTestPacket` | ProjectScopeHeader, ChannelScopeHeader | ProjectScopeHeader, NodeAHeader, ChannelScopeHeader |
| `PlayerEvent` | ProjectScopeHeader, NodeBHeader | ProjectScopeHeader, NodeAHeader |
| `TeamCoordination` | ProjectScopeHeader, ChannelScopeHeader | ProjectScopeHeader, ChannelScopeHeader |
| `PlayerAction` | ProjectScopeHeader | ProjectScopeHeader |
| `PlayerAction.GameStateUpdate` | ProjectScopeHeader, RecursiveHeader | ProjectScopeHeader, RecursiveHeader |
| `...GameStateUpdate.ChatMessage` | ProjectScopeHeader | ProjectScopeHeader |
| `...ChatMessage.AuthRequest` | ProjectScopeHeader, RecursiveHeader | ProjectScopeHeader, RecursiveHeader |
| `ServerStatus` | none | none |

- NOTE: `@PlayerAction` takes packs nested in it but not container → `RecursiveHeader` skips `PlayerAction`; `X<ChatMessage>` removes only that pack, not nested `AuthRequest`. `NodeAHeader` only toward `NodeA`, `NodeBHeader` only toward `NodeB` (on `PlayerEvent`, not `PrecedenceTestPacket`). `PrecedenceTestPacket` to `NodeA`: 3 headers stack (id, ProjectScopeHeader, NodeAHeader, ChannelScopeHeader, payload); to `NodeB`: id, ProjectScopeHeader, ChannelScopeHeader, payload.

### §8.6.3 What the generator produces for headers
KW: Connection.Transmitter.Header, Connection.Receiver.Header, getXxxHeader, send, OnSerializing, OnSerialized, OnReceiving, OnReceived, view
- GEN: pack classes contain no header fields; headers appear in connection:
  - `Connection.Transmitter.Header` and `Connection.Receiver.Header`: one nested interface per header that can travel in that direction; getter + setter method per field; `getXxxHeader()` returns `null` when header not on current pack. Trailing comment of header field carried to its accessors.
  - `send` method takes values of every header the pack carries in that direction as params, header by header, before connection argument.
  - receive handler gets one header object per header on pack, after the pack.
  - every actor has header hooks: `OnSerializing` / `OnSerialized` (sending), `OnReceiving` / `OnReceived` (receiving). `OnReceiving` runs when pack id + header bits arrived, before payload parsed → inspect routing/session fields first →§13.
- C# side of `NodeA` for example:
```csharp
// Connection.Receiver.Header (shape)
public interface Header
{
    interface ChannelScopeHeader
    {
        uint   sequence_num();  void sequence_num(uint src);
        ushort priority();      void priority(ushort src);
        bool   flag_bit();      void flag_bit(bool src);
    }
    ChannelScopeHeader getChannelScopeHeader();   // null if the current pack has no ChannelScopeHeader
    // ... ProjectScopeHeader, NodeAHeader, RecursiveHeader the same way
}
// Sending to NodeB: ProjectScopeHeader + ChannelScopeHeader
Actor2.Start.send(packet,
                  timestamp, sessionId,             // ProjectScopeHeader
                  sequenceNum, priority, flagBit,   // ChannelScopeHeader
                  conn);
// Receiving from NodeB: three headers
Actor2.Start.OnReceiveD.PrecedenceTestPacket.handlers += (pack, project, nodeA, channel, conn, actor) =>
{
    ulong ts    = project.timestamp();
    byte  flags = nodeA.flags();
    uint  seq   = channel.sequence_num();
};
```
- PITFALL: receiving header objects = views over connection's reusable receive buffer: read values inside handler, do not keep references (next pack overwrites).

### §8.6.4 Headers on the wire
KW: header bits, bit-packed, actor address, rounded to bytes, sub-pack no header, 182 bits
- WIRE: standalone pack = pack id, then actor address (on connections addressing several actors), then header bits, then payload.
- WIRE: header fields of all headers on pack bit-packed one after another, group after group, in order generator fixes for connection = order `send` lists them. `bool` 1 bit, `[MinMax(0, 31)]` field 5 bits, other primitives full width; total rounded up to whole bytes. Header not on pack takes no space.
- WIRE: example `PrecedenceTestPacket` received by `NodeA`: ProjectScopeHeader 96 (timestamp 64, session_id 32) + NodeAHeader 37 (host_specific_id 32, flags 5) + ChannelScopeHeader 49 (sequence_num 32, priority 16, flag_bit 1) = 182 header bits + 2 padding bits = 23 bytes; available in `OnReceiving` before payload parsed.
- RULE: pack used as field of another pack (sub-pack) carries no header: headers belong to transmission, not pack. Pack both sent alone and used as field has headers only when sent alone.

### §8.6.5 Which packs carry headers
KW: sent packs, wire id, Value Pack header, empty pack header, Stream, File, Resumable, [UDP], field-less header
- RULE: header attaches only to packs some branch sends (packs with wire id →§20.3.2). Target no branch sends → nothing; header with no such target omitted from generated code.
- RULE: attaches to every kind of sent pack: Value Packs, empty packs (header = all that follows pack id), `Stream`/`File` conduit packs (header after pack id, before framing →§15).
```csharp
class Flags                        // a Value Pack
{
    [MinMax(0, 15)] byte kind;
    bool                urgent;
}
class Nop { }                      // an empty pack
// inside a connection that sends Flags and Nop
class Route : HeaderFor<(Flags, Nop)>
{
    [MinMax(0, 7)] byte hop;       // 3 bits
}
```
- NOTE: same live project combines this with project header excluding one pack (`HeaderFor<(ResumableTest, X<Telemetry>)>`), host header, header on another connection; matching ones stack.
- NOTE: guaranteed delivery uses same mechanism: `Resumable<HOST, PACKS>` and `[UDP]` produce field-less headers the Agent makes, bound like any header →§14.

### §8.6.6 An empty pack with a header: values without an object
KW: empty pack, header carries values, no object, XYZ, Position, 96 bits, Value Pack alternative
- NOTE: pack > 64 bits not Value Pack →§7.9 (3 `float` = 12-byte pack; concrete pack = object per message). Sent alone → empty pack with header carries values with no object on either side:
```csharp
class Position { }                              // an empty pack: only its id travels
interface Link : Connects<Station, Drone>
{
    class XYZ : HeaderFor<Position>             // the coordinates ride in the header
    {
        public float x;
        public float y;
        public float z;
    }
    [____________r<Position>]
    struct Live { }
}
```
- GEN: sending: generated `send` of `Position` takes `x`, `y`, `z` as params, header by header; values stored in slot of connection's sending queue (as any header); empty pack has no object to make.
- RT: receiving: handler gets `XYZ` header as view over receive buffer, reads `x()`, `y()`, `z()`; read in handler, next pack overwrites view.
- WIRE: id + 96 header bits = 12 bytes = cost of pack `{ float x, y, z; }`.
- RULE: only what header rules allow →§8.6.7: single non-nullable primitives, optionally `[MinMax]`. Strings, arrays, nested packs → ordinary pack, or abstract pack reading/writing own structures →§10.3.2.

### §8.6.7 Rules for header fields and names
KW: header field rules, non-nullable, primitive, fixed size, name_1, rename, exit code 22, Header (Packet Metadata)
- RULE: receiver must read headers without knowing payload → every header field fixed size.

| Rule | Agent's response when broken |
|:--|:--|
| header field = single non-nullable primitive: `bool`, `byte`, `sbyte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `float`, `double` | "The header field \`F\` (…) of \`Path\` has a type a header cannot carry. A header field is one primitive value: not nullable, not a string, not an array or a collection." |
| no string, `Binary`, pack, array, `Map`, `Set` field | same message |
| no `[D]`, `[A]`, `[V]`, `[X]` | same message |
| no nullable field (`int?`) | same message; later check "A header field cannot be nullable (`T?`). Make these fields non-nullable:" lists any nullable header field that got past it |
| `[MinMax(min, max)]` allowed, narrows field to bits its range needs | - |
| header names unique; header field name unique across all headers of project and differs from own header's name | warning per clash names a free name (`name_1`, `name_2`, …), then run stops: "The names of headers and header fields above are taken. Rename them in the description - the names above are free - and run again." |
| inside connection body `HeaderFor` only class allowed | "The class '...' (...) is declared inside the connection ...: inside a connection a class is a header only..." (exit code 22) |
| header is not payload | naming it in branch stops run: "... is a Header (Packet Metadata) and not a transmittable message type." |

- NOTE: name rule because generated connection exposes fields of all its headers on one object (two headers with `timestamp` would clash). Header names never clash with payload field names (header outside pack).
- PITFALL: unattended run: nobody answers `yes` → name header fields uniquely yourself.
- RULE: like injector, header carries only fields declared in its body; its base list and `<see cref>` refs in doc comment edit its target set.

## §8.7 Modifying a header: `Modify<Header>`
KW: Modify<Header>, header modifier, add fields, override fields, target set, _<>, X<>, two-argument Modify
- DECL: `header modifier` changes existing header (typically imported) without rewriting: class implementing `Modify<Header>` with exactly one type argument, outside any connection body. Does three things:
  1. adds fields: every modifier field added to header;
  2. overrides fields: modifier field with header field's name replaces it; replacing field moves to end of header → changes header bit layout;
  3. edits target set: packs/Pack Sets in `_<...>` in modifier's base list added to targets; `X<...>` removes.
```csharp
class SessionHeader : HeaderFor<Point2d>
{
    int plain_id;
    int session;
}
class SessionHeaderModifier : Modify<SessionHeader>, _<Point3d>, X<Point2d>
{
    long routing_hash;     // added
    uint session;          // replaces int session
}
// SessionHeader: targets { Point3d }, fields { plain_id, routing_hash, session (uint) }
```
- RULE: pack in base list goes in `_<...>`: C# requires base class first, so pack after `Modify<...>` must be wrapped. Live streaming test project:
```csharp
class StreamSequenceHeader : HeaderFor<(NamedStream, NamedStreamZstd, NamedFile, UniversalStreamPack)>
{
    public uint sequence_num;
    public long timestamp_ns;
}
class StreamSequenceHeaderModifier : Modify<StreamSequenceHeader>, _<SmallNamedFile>, X<NamedFile>
{
    public short routing_priority;
    public uint  sequence_num;
}
// targets: NamedStream, NamedStreamZstd, UniversalStreamPack, SmallNamedFile
// fields:  timestamp_ns, routing_priority, sequence_num (the modifier's)
```
- RT: Agent merges modifier into header before binding headers to packs → modified header travels wherever header travels.
- RULE: modifier fields obey header field rules →§8.6.7; broken rule → same message naming a "header modifier field".
- NOTE: `Modify<>` for packs, hosts, connections, other entities →§22.3.
- PITFALL: only one-argument form is header modifier. Meta also declares two-argument `Modify<Target, Modifications>`; class `Modify<SomeHeader, (PackA, PackB)>` is not merged into `SomeHeader`: Agent ignores 2nd arg, does not add class's fields to header; like every modifier, class is in no scope (`@MyProject`), never sent. Modifier changes header for all its targets; to add packs put them in `_<...>` in modifier's base list.

## §8.8 Related: composition and headers
KW: related, packs, pack sets, connections, actor hooks, reuse, Modify
- →§4: nested packs, empty packs, directives in generated docs.
- →§7: how generator groups fields on wire.
- →§9: every expression `FieldsInjectInto<>`, `HeaderFor<>`, `_<>` accept.
- →§11, →§13: where header accessors and hooks live.
- →§22: `Modify<>`, adapting reused packs.

# §9 Pack Sets
KW: Pack Set, _<>, X<>, @, Named Pack Set, filter template, KeepName, SkipName, KeepDoc, SkipDoc, PackSet collection
SUMMARY: `Pack Set` = expression naming a group of packs; one expression language + one resolver for all consumers. Scopes, `@`, in-place sets, named sets, regex filters, filter templates, union/intersection/difference, errors.
- Model level: project → host → connection → actor → state → branch → pack → field; Pack Set selects packs for levels above pack.
- Consumers (all same syntax/resolver):

| Consumer | Described in |
|:--|:--|
| `PACKS` of a branch on a state | →§12 |
| argument and replies of RPC method | →§13 |
| `HeaderFor<PackSet>`, `FieldsInjectInto<PackSet>` | →§8 |
| `Resumable<HOST, PACKS>`, `[UDP<EXCLUDE_PACKS_SET>]` | →§14 |
| host configuration groups | →§10 |

- NOTE: resolved by Agent while reading description; leaves nothing in generated code, only resulting pack list of each consumer.
- NOTE: evaluation: elements of e.g. `_<(ReportTraffic, CriticalAlerts<@Server>, X<ActiveOnly<@Order>>)>` taken left to right; `X<>` removes only what came before it; in a template call the argument (tuple merged first = union) passes template's base list, then filter stages KeepName, KeepDoc, SkipName, SkipDoc; survivors return to outer set.

## §9.1 The parts of a Pack Set expression
KW: Pack Set parts, _<TYPES>, X<TYPES>, tuple, scope, data packs only, DateTimeDef, TimeSpanDef, Duration
- Example project used in §9 (9 packs: `CpuStats`, `V1Payload`, `Order`, `Order.Line`, `Order.Cancel`, `Server.Credentials`, `Server.Report`, `Server.Report.Subscribe`, `Server.Report.Unsubscribe`; host `Client` empty):
```csharp
using org.unirail.Meta;
namespace com.my.company
{
    public interface Shop
    {
        /// 📈 Server performance metrics.
        class CpuStats { float load; }
        /// ⛔ Legacy payload.
        class V1Payload { int value; }
        class Order
        {
            long id;
            public class Line   { int sku; int qty; }
            public class Cancel { long order_id; }
        }
        /// <see cref = 'InCS'/>
        struct Server : Host
        {
            /// 🔒 User credentials.
            public class Credentials { string login; string password; }
            public class Report
            {
                string title;
                public class Subscribe   { int period_s; }
                public class Unsubscribe { int reason; }
            }
        }
        /// <see cref = 'InTS'/>
        struct Client : Host { }
        interface Link : Connects<Server, Client>
        {
            [_____lr_____<@Shop>]
            struct Start { }
        }
    }
}
```

| Part | Written as | Contributes |
|:--|:--|:--|
| pack | `Order` | that pack |
| recursive pack scope | `@Order` | every pack nested in `Order`, any depth, not `Order` |
| host scope | `Server`, `@Server` | packs declared directly in host; with `@` every pack inside it |
| project scope | `Shop`, `@Shop` | packs outside hosts; with `@` every pack of project |
| Named Pack Set | `Reports`, `@Reports` | its packs; with `@` packs nested in its packs |
| in-place set | `_<...>` | what inner expression contributes |
| exclusion | `X<...>` | removes what inner expression contributes |
| filter template | `ActiveOnly<@Shop>` | packs of argument passing its filters |
| tuple | `(A, B, X<C>)` | its elements, left to right |

- DECL: Meta declares `_<TYPES>` and `X<TYPES>` with one type parameter each; C# has no variadic generics → several types as tuple `_<(A, B, C)>`; single type no parentheses `_<A>`.
- NOTE: same `_<>` wrapper also composes pack fields (→§8) and groups endpoints (→§17); meaning depends on position.
- RULE: only data packs selected. Enums, constants sets, typedefs, headers, field injectors, `DateTimeDef`, `TimeSpanDef`, `Duration` skipped by every scope. Named directly in branch `PACKS` → run stops (→§12).

## §9.2 In-place Pack Sets: `_<>`, tuples and `X<>`
KW: _<>, X<>, tuple, left to right, exclusion order, X<@Pack>
- DECL: `_<...>` makes set on the spot; `X<...>` takes packs out:
```csharp
interface OrderTraffic : _<(Order, @Order, X<Order.Cancel>)> { }    // Order, Order.Line
```
- RULE: elements apply left to right: Agent walks interface base list left to right and, inside each base, every tuple's elements left to right as written. `X<...>` removes only what was added before it.
- PITFALL: `_<(X<Order.Cancel>, @Order)>` → exclusion first removes nothing, `@Order` then adds `Order.Cancel`. Put exclusions last.
- RULE: `X<Pack>` removes that pack only; nested packs stay. Remove them too: `X<@Pack>`, or both: `X<(Order, @Order)>`.

## §9.3 Named Pack Sets
KW: Named Pack Set, first base _<...>, Unresolved symbol, PackSet collection, see cref, second pass
- DECL: `Named Pack Set` = nested interface whose FIRST base is `_<...>`; its name stands for the set wherever Pack Set accepted. More bases may follow (`X<...>` among them), applied left to right like tuple elements.
```csharp
interface Reports    : _<@Server.Report> { }               // Server.Report.Subscribe, Server.Report.Unsubscribe
interface Everything : _<@Shop>, X<V1Payload> { }          // all packs of Shop but V1Payload
interface Readable   : _<(Everything, X<Server.Credentials>)> { }
// live test project: links' packs defined once, second set derived
interface Everything : _<(Tick, Flags, Nop, Note, Bulk, Order, Telemetry, Blob, Frame, FrameZstd, Thumb, Capture, Attachment, Album, Archive, Vault)> { }
interface Guaranteed : _<(Everything, X<Telemetry>)> { }
```
- RULE: declare anywhere in project, incl. inside the connection using it.
- RULE: may name a set declared later in file (Agent builds that one first). Two sets naming each other do not stop run; cycle resolved in second pass.
- RULE: interface whose first base is `X<...>` = not a Pack Set; start with `_<...>`.
- RULE: `_<...>` whose elements are `IfSendingFrom<...>` endpoints = Endpoint Set, not Pack Set (→§17).
- RULE: unresolvable element stops run: "Unresolved symbol '...' (line N) in PackSet collection."
- RULE: doc comment above Named Pack Set reads references as pack's does: `<see cref="X"/>+` adds pack to set, `-` removes, no mark = documentation. List packs in base list anyway.

## §9.4 Project, host and pack scopes, and `@`
KW: scope, @, recursive, shallow, verbatim identifier, @Shop, Host keyword, Unexpected item, PACKS argument
- `scope` = project, host or pack used as set of packs inside it. Without `@` shallow; with `@` recursive. On pack, `@` takes contents, leaves out container.

| Expression | Selects in `Shop` |
|:--|:--|
| `Order` | `Order` |
| `@Order` | `Order.Line`, `Order.Cancel` |
| `Server` | `Server.Credentials`, `Server.Report` |
| `@Server` | `Server.Credentials`, `Server.Report`, `Server.Report.Subscribe`, `Server.Report.Unsubscribe` |
| `Shop` | `CpuStats`, `V1Payload`, `Order`, `Order.Line`, `Order.Cancel` |
| `@Shop` | all nine packs |

- RULE: Pack: without `@` = pack itself; with `@` = every nested pack, any depth, not the pack. Both: `(Order, @Order)`.
- RULE: Host: without `@` = packs declared directly in host; with `@` = every pack inside host, any depth.
- RULE: Project: without `@` = every pack not inside a host, at ANY nesting depth (`Order.Line` included); with `@` = every pack of project incl. those inside hosts. Project's own packs only: imported project = scope of its own.
- RULE: Named Pack Set: without `@` = its packs; with `@` (in Pack Set, header, injector, host configuration) = packs nested in each of its packs, those packs themselves excluded. In branch `PACKS`, `@` on Named Pack Set means something else: its packs fed to branch's filter arguments (→§12).
- DECL: `@` = C# verbatim-identifier prefix; C# ignores it (`@Shop` compiles as `Shop`); Agent reads source text: expression whose first token starts with `@` = recursive. On dotted name prefix goes before first segment, applies to whole reference: `@Server.Report` = recursive scope of `Server.Report`.
- RULE: connection is not a scope: `@Link` in Named Pack Set/header/injector → "Unresolved symbol '...' (line N) in PackSet collection."; in branch `PACKS` → "Unexpected item ... in the ... PACKS argument".
- PITFALL: `Project`, `Host` are not keywords. Under `using org.unirail.Meta;` `Host` resolves to Meta's `Host` interface (no entity) → fails same way. Name own project/hosts.
- PITFALL: project without `@` leaves out every pack declared inside a host: `[_____lr_____<Shop>]` carries `CpuStats`, `V1Payload`, `Order` packs, not `Server.Credentials` or report packs. Write `@Shop` when hosts' packs belong.
- NOTE: in a branch, Agent warns when project or host written without `@` holds no packs of its own.

## §9.5 Filtering a set by name and by documentation
KW: KeepName, SkipName, KeepDoc, SkipDoc, regex, .NET regex, OR, AND, case-sensitive, unanchored
| Attribute | Effect | Matched against |
|:--|:--|:--|
| `[KeepName(regex)]` | keeps only matching packs | pack's full path |
| `[SkipName(regex)]` | removes matching packs | pack's full path |
| `[KeepDoc(regex)]` | keeps only matching packs | pack's doc pool |
| `[SkipDoc(regex)]` | removes matching packs | pack's doc pool |

- DECL: put on Named Pack Set or filter template (→§9.6); each may appear several times. On Named Pack Set: filter every pack collected through `_<...>`/`X<...>`, directly listed packs included. On filter template: filter everything argument and base list produce. Branch attributes take same four filters as constructor arguments, but there touch only packs of `@` scopes (→§12).
- RULE: combine: same kind → OR (two `[KeepDoc]` keep pack matching either); different kinds → AND. Pack survives when: no `[KeepName]` or matches one, AND no `[KeepDoc]` or matches one, AND matches no `[SkipName]`, AND matches no `[SkipDoc]`. Both `[KeepName]`+`[KeepDoc]` → must match one of each.
- RT: stage order KeepName, KeepDoc, SkipName, SkipDoc; each only narrows → result independent of order.
- RULE: pattern = .NET regex compiled without `IgnoreCase` or any option: case-sensitive, space = literal space, matches anywhere unless anchored `^`/`$`. Branch filter arguments compiled same way (case, spaces, anchoring and first three pitfalls of §9.5.2 apply).

### §9.5.1 The name a filter sees: the full path
KW: full path, KeepName, SkipName, anchor, ^, $, (^|\.), generation, V1
- RULE: `KeepName`/`SkipName` match pack's `full path` = names of enclosing entities below root project joined with dots; no namespace, no root project name. In `Shop`: `CpuStats`, `Order.Line`, `Server.Report.Subscribe`. Pack of imported project starts with that project's name. `^com\.` never matches.
- PITFALL: unanchored → bare name matches longer names; anchor it; top-level pack of root project has no dot before name.

| Pattern | Matches | Does not match |
|:--|:--|:--|
| `"Line"` | `Order.Line`, `Order.LineV2`, `Order.Outline` | - (too broad) |
| `"\.Line$"` | `Order.Line` | `Order.LineV2`, `Order.Outline`, top-level `Line` |
| `"(^\|\.)Line$"` | `Order.Line`, top-level `Line` | `Order.LineV2`, `Order.Outline` |
| `"\.Subscribe$\|\.Unsubscribe$"` | `Server.Report.Subscribe`, `Server.Report.Unsubscribe` | `Server.Subscriber`, `Server.Unsubscribed` |

- NOTE: name filters select one protocol generation among several side by side (e.g. everything under container `V1`); generation shape →§19.
```csharp
[KeepName(@"(^|\.)V1\.")]                     // packs inside any container named V1, top level included
interface Generation1<SCOPE> { }
[SkipName(@"(^|\.)Test$")]                    // drop packs named Test
interface NoTests<SCOPE> { }
[KeepName(@"\.Subscribe$|\.Unsubscribe$")]    // Server.Report.Subscribe, Server.Report.Unsubscribe
interface SubsUnsubs<SCOPE> { }
```

### §9.5.2 The doc pool: comments and tags
KW: doc pool, tags, packs table, KeepDoc, SkipDoc, emoji, U+FE0F, variation selector, empty regex, invalid regex
- RULE: `KeepDoc`/`SkipDoc` match `unified doc pool` = one text of: (1) comment lines directly above pack's class: `///` doc and plain `//` lines, without `<see cref>` directive lines; `/* ... */` block comment NOT included; (2) pack's `tags` = text after the numbers on its line in packs table (→§20). Packs tables of EVERY loaded project count → tags written in own table for an imported pack are searched too.
- NOTE: sources not distinguished: packs-table line `<see cref='CpuStats'/>ÿ   0 📈 metrics` adds `📈 metrics` to `CpuStats` pool exactly as `/// 📈 metrics`. Tags may be emojis or keywords; branches select by them same as by comments.
```csharp
[KeepDoc(@"📈")]
interface MetricsOnly<SCOPE> { }      // from @Shop: CpuStats
[SkipDoc(@"⛔|🙈")]
interface NoLegacy<SCOPE> { }         // from @Shop: everything but V1Payload
[KeepDoc(@"📈")]
[SkipDoc(@"⛔")]
interface ActiveMetrics<SCOPE> { }    // from @Shop: CpuStats
```
- NOTE: routing by tags alone: packs carry direction tag `🖥️👉`/`🖥️👈` in packs table; Named Pack Set `[KeepDoc(@"🖥️👉|🖥️👈")] interface ... : _<@SomeHost>, X<...> { }` gathers both; each branch keeps one tag.
- PITFALL: `"base"` also matches `database`, anywhere in pool. Make patterns specific or use tags that cannot occur in prose.
- PITFALL: spaces literal: `"common | 🔑"` = alternatives `common␠` and `␠🔑`; fail when pool ends with `common` or no space before `🔑`. Write `"common|🔑"`.
- PITFALL: many emojis carry invisible variation selector U+FE0F (`👁️`, `🛡️`, `🖥️`); pattern must contain exactly text's code points → copy emoji from comment/tag.
- RULE: empty pattern stops run ("[KeepName] has an empty or null regex pattern."); invalid one too ("... contains an invalid regex: ..."); both reported at the declaration.

## §9.6 Filter templates: `F<SCOPE>`
KW: filter template, F<SCOPE>, SCOPE, generic interface, CS0305, one type parameter, LayoutFilter, base list
- DECL: `filter template` = generic nested interface with exactly one type parameter; defines how to filter; use site supplies what (type argument = any Pack Set expression, tuple included).
```csharp
[SkipDoc(@"⛔|Deprecated")]
interface ActiveOnly<SCOPE> { }
[KeepName(@"(^|\.)Order\.")]
interface OnlyOrders<SCOPE> { }
// state of interface Link : Connects<Server, Client>, in place of Start
[l____________<ActiveOnly<@Shop>>]         // server may send every active pack of project
struct ActiveState { }
```
- RT: `ActiveOnly<@Shop>` evaluates `@Shop`, keeps what passes filters. Branch syntax →§12.
- NOTE: AdHoc's own protocol declares template inside the connection using it; packs meant for it carry `///🔠` line:
```csharp
interface SaveLayout : Connects<Agent, LayoutFile>
{
    [KeepDoc("🔠")] interface LayoutFilter<SCOPE> { }
    [_____lr_____<LayoutFilter<@AdHocProtocol>>]
    struct Start { }
}
```
- DECL: template with base list: `SCOPE` stands for argument:
```csharp
interface WithoutLegacy<SCOPE> : _<SCOPE>, X<V1Payload> { }     // argument minus V1Payload
```
- RT: Agent walks base list substituting argument for `SCOPE`, then applies template's filters (if any). Template with base list MUST contain `_<SCOPE>` or argument never enters set. Template without base list simply evaluates argument.
- RULE: every generic nested interface = filter template, exactly one type parameter; else "Filter template '...' (line N) declares K generic parameters, but a filter template must have exactly ONE generic parameter — the scope placeholder." Pass several scopes as tuple at use site.
- RULE: template always used with argument: bare `ActiveOnly` = C# error CS0305; Agent stops on compile errors: "The description does not compile as C#: ... error CS0305: Using the generic type '...' requires 1 type arguments". Agent's own check: "Filter template '...' cannot be used without a scope argument."
- RULE: template is not an entity: never in numbers tables or generated code.
- NOTE: template vs Named Pack Set: template when same filter applies to different scopes; when one filtered subset is a concept of its own → Named Pack Set with filter attributes on interface, filtering what its `_<...>`/`X<...>` collected:
```csharp
[KeepName(@"\.Report\.")]
interface ReportTraffic : _<@Shop>, X<Server.Report.Unsubscribe> { }    // Server.Report.Subscribe
// state of interface Link : Connects<Server, Client>, in place of Start
[l____________<ReportTraffic>]
struct Reporting { }
```
- NOTE: third place for filters = branch attribute constructor arguments (→§12).

## §9.7 Combining sets: union, intersection, difference
KW: union, intersection, difference, tuple, nested template, X<template>, PublicView, AdminView
- RT: union: tuple passed to template merged first, then filtered:
```csharp
[KeepDoc(@"👁️Public")]
interface PublicView<SCOPE> { }
interface PublicFeed : _<PublicView<(@Order, @Server.Report)>> { }
```
- RT: intersection: filtered set as argument of another template narrows step by step; read inside out:
```csharp
interface ActiveOrders : _<OnlyOrders<ActiveOnly<@Shop>>> { }
// 1. every pack of Shop, 2. without ⛔ / Deprecated, 3. only packs inside Order
```
  `PublicView<AdminView<@Shop>>` keeps only packs satisfying both.
- RT: difference: `X<...>` around template removes packs surviving template.
- Combined with Named Pack Sets and plain packs; order still matters (`X<...>` removes what first two added):
```csharp
[KeepDoc(@"🚨")]
interface CriticalAlerts<SCOPE> { }
interface AdminDashboard : _<(ReportTraffic,                  // a Named Pack Set
                              CriticalAlerts<@Server>,        // plus every 🚨 pack of Server host
                              X<ActiveOnly<@Order>>)> { }     // minus active packs nested in Order
```
- Example one set per role: packs marked `👁️Public` or `🛡️Admin` in comments/tags:
```csharp
// PublicView as above
[KeepDoc(@"🛡️Admin")]
interface AdminView<SCOPE> { }
/// <see cref = 'InTS'/>
struct GuestApp : Host { }
/// <see cref = 'InTS'/>
struct AdminApp : Host { }
interface GuestLink : Connects<Server, GuestApp>
{
    [l____________<PublicView<@Shop>>]                          // Public packs only
    struct Start { }
}
interface AdminLink : Connects<Server, AdminApp>
{
    [l____________<(PublicView<@Shop>, AdminView<@Shop>)>]      // union: Public or Admin
    struct Start { }
}
```

## §9.8 Rules for Pack Sets at a glance
KW: Unresolved symbol, Unexpected item, Filter template, CS0305, Endpoint Set, second pass
| Situation | Result |
|:--|:--|
| element resolving to nothing known (typo, connection, bare `Host`) | "Unresolved symbol '...' (line N) in PackSet collection."; in branch `PACKS`: "Unexpected item ... in the ... PACKS argument" |
| generic nested interface with >1 type parameter | "Filter template '...' declares K generic parameters ..." |
| filter template without argument | C# error CS0305, reported by Agent before it reads description |
| empty or invalid filter pattern | run stops at declaration |
| interface whose first base is not `_<...>` | not a Named Pack Set |
| `_<...>` of endpoints | Endpoint Set →§17 |
| Pack Sets naming each other | resolved in second pass |
| `X<...>` before element that adds the pack | removes nothing |

## §9.9 Related: Pack Sets
KW: related, PACKS, headers, injectors, host configuration, tags
- →§12 `PACKS` of branch, branch filter arguments, `@` on Named Pack Set in branch; →§8 headers/injectors targeting Pack Sets; →§10 host name as set of its packs in host configuration (→§10.7); →§20 where tags live.

# §10 Hosts, languages, generated code
KW: host, Host, language marker, implementation modifier, concrete, abstract, ++, --, gen, lib
SUMMARY: declaring hosts (`struct X : Host`), language markers, implementation modifiers (concrete/abstract, equality/hash) at host/pack/field level with exact precedence and comment-reading algorithm, `Modify<Host>`, host as Pack Set, generated output per host and language.
- Model: project → host → connection → actor → state → branch → pack → field. Pipeline: description → AdHocAgent → generator → code per host and language → deployment → runtime.
- `host` = endpoint of communication: program sending/receiving packs; unit generator works in: every host gets own code in every language named for it; per pack: generated code or your structures hold data.

## §10.1 Declaring a host
KW: struct, Host, org.unirail.Meta.Host, uid, implements interfaces, names no language to generate it in, exit code 45
- DECL: host = C# `struct` implementing marker interface `org.unirail.Meta.Host`, declared DIRECTLY in project interface; language markers in its doc comment:
```csharp
using org.unirail.Meta;
namespace com.example.shop
{
    public interface Shop
    {
        class Order
        {
            long              id;
            [D(+64)] string   customer;
        }
        /// <summary>The order service.</summary>
        /**
        <see cref="InCS"/>
        <see cref="InJAVA"/>--
        */
        struct Server : Host
        {
            public class Receipt { long id; long total_cents; }
            public class OutOfStock { long id; }
        }
        /// <see cref="InTS"/>
        struct Client : Host { }
        interface Session : Connects<Client, Server>
        {
            [l____________<Order>]
            [____________r<Server>]
            struct Open { }
        }
    }
}
```
- GEN: `Server` → C# and Java; `Client` → TypeScript (AdHocAgent's project template mixes languages same way, →§2).
- NOTE: packs in host body (`Server.Receipt`) = ordinary packs (→§4). Nesting does not limit who sends/receives (any connection may carry `Server.Receipt`), but puts host name in pack's path and makes pack part of host's own Pack Set (→§10.7; branch `[____________r<Server>]` uses it).
- NOTE: constants container or enum declared in host body = itself a route into that host's generated code (→§6).
- NOTE: host = type of node, not single process: any number of running programs may be instances of one host. Each host gets number `uid` in project's numbers table (→§20).

| Situation | Result |
|:--|:--|
| `struct X : Host` directly in project interface | host |
| `struct X : Host` elsewhere (inside pack, host, nested interface) | error: "The struct ... implements interfaces, so it is not a constants set, and it is not a host either. A host is declared directly in the project" |
| `interface X : Host` | not a host; ignored WITHOUT message; always declare as `struct` |
| host with no language marker | error (exit code 45): "The host ... (...) names no language to generate it in." |
| two hosts same name after import | error →§22 |

- NOTE: which enums/constant sets a host carries (packs named in its language configuration count too) →§6. Classification of every `struct` →§3.

## §10.2 Language markers
KW: InCPP, InRS, InCS, InJAVA, InGO, InTS, InSwift, InC, InDart, InPy, All, see cref, UDP
- DECL: `language marker` = `<see cref="..."/>` to a language interface of `Meta.cs`, in host doc comment. Does two things: turns language on for host, sets implementation modifier (→§10.3; e.g. `--` after `InJAVA`). No marker → no code: host gets code only in languages its comment names.

| Marker | Language | Marker | Language |
|:--|:--|:--|:--|
| `InCPP` | C++ | `InTS` | TypeScript |
| `InRS` | Rust | `InSwift` | Swift |
| `InCS` | C# | `InC` | C |
| `InJAVA` | Java | `InDart` | Dart |
| `InGO` | Go | `InPy` | Python |

- GEN: code generated in each. Exception: the TypeScript runtime has no UDP transport; Dart has it on `dart:io` only, not in a web build (→§14).
- PITFALL: host generated in TypeScript cannot sit on `[UDP]` connection (→§14).
- DECL: several languages, one marker per line; free text after marker ignored:
```csharp
///<see cref = 'InTS'/>   will be implemented in TypeScript
///<see cref = 'InCS'/>   will be implemented in C#
///<see cref = 'InJAVA'/> will be implemented in JAVA
// ... same for InCPP, InRS, InGO, InSwift, InC, InDart, InPy (all ten markers allowed together)
struct FreeClient : Host{ }
```
- PITFALL: `Meta.cs` interface `All` is NOT a language marker; in host comment treated as ordinary reference → ignored with warning or rejected. Name each language.
- DECL: both `///` lines and `/** ... */` block work. Doc-comment block containing language marker = configuration, not documentation: kept out of generated code comments, prose included.
- PITFALL: consecutive `///` lines = one block → `/// <summary>` directly above `/// <see cref="InCS"/>` dropped with it. Keep description in own `/// <summary>` block, configuration in `/** ... */` block. AdHoc's own protocol:
```csharp
/// <summary>
/// Defines the Server host. ... (host description, kept in generated code)
/// </summary>
/**
<see cref = 'InJAVA'/> The following packs of the `Server` host are fully implemented and generated in JAVA.
<see cref = 'Result'/>
<see cref = 'Progress'/>
<see cref = 'Agent.Login'/>
<see cref = 'Agent.Version'/>
<see cref = 'Server.Invitation'/>
<see cref = 'Server.InvitationUpdate'/>
<see cref = 'Agent.Project.Connection.Actor.State.Branch'/>
<see cref = 'InJAVA'/>-- The remaining packs are generated in JAVA as abstract (without implementation).
*/
struct Server : Host{
    // ... the packs of the Server host
}
```

## §10.3 Implementation modifiers: who holds the data
KW: implementation modifier, ++, +-, -+, --, concrete, abstract, equality, hash, host default
- DECL: `implementation modifier` = 1 or 2 chars right after marker. 1st: who holds pack data; 2nd: equality+hash generated or not.

| Modifier | Example | Implementation | Equality and hash |
|:--:|:--|:--|:--|
| `++` | `<see cref='InCS'/>++` | concrete: generated pack holds data | generated |
| `+-` | `<see cref='InCS'/>+-` | concrete | not generated |
| `-+` | `<see cref='InCS'/>-+` | abstract: your code holds data | generated |
| `--` | `<see cref='InCS'/>--` | abstract | not generated |

- RULE: modifier = the one or two `+`/`-` chars right after marker (whitespace between marker and them allowed). Anything else on line (trailing `//` comment, sentence) not part of it.
- DEFAULT: marker without modifier = `++`. Single char sets implementation, equality/hash stays `+`: `<see cref='InCS'/>-` = `-+`.
- RULE: modifier of LAST marker of a language = host default for that language; overridable per pack and per field by references under a marker above it (→§10.5).
- RT: concrete: receive buffer → generated parser → generated pack object → your code reads; send: your code fills generated object → serializer copies into send buffer. Abstract: parser calls setters on your object field by field as bytes arrive; serializer calls getters field by field; no generated pack object either direction.

### §10.3.1 Concrete (`+`): the generated pack holds the data
KW: concrete, +, stored field, send, random access
- GEN: complete class, stored field per pack field.
- RT: receiving: parser reads whole pack from socket buffer into object; every field decoded before your code sees it; random access to all. Sending: your code fills object, hands to generated `send` method.
- NOTE: use `+` when packs ARE your data model (ordinary objects passed and stored).

### §10.3.2 Abstract (`-`): your code holds the data
KW: abstract, -, setter, getter, _field, set__field, partial interface, pub trait, Pack_Fields, _Allocator, Allocator_DEFAULT, _allocator, new_, The producer of ... is not assigned, windows, BytesSrc, Fields, Impl, $field, PackImpl
- GEN: abstract type your class implements, one accessor per field: where host RECEIVES pack → setter per field (parser calls it per field as decoded); where host SENDS → getter per field (serializer calls it per field as written).
- RT: no generated object allocated, no copy either side: socket buffer ↔ your structures directly. → `-` serves packs larger than memory you want to spend, and pass-through hosts.

| Language | Concrete pack | Abstract pack |
|:--|:--|:--|
| C# | `partial class`, property per field | `partial interface`: `_field { get; }` to send, `_field { set; }` to receive |
| Java | `class`, field per pack field | `interface`, accessor method per field |
| TypeScript | `class` | `abstract class` with `protected abstract get _field()` / `set _field(...)` |
| Rust | `pub struct`, `pub` field per pack field | `pub trait`: `fn _field(&self) -> T` to send, `fn set__field(&mut self, value: T)` to receive; collections via `_field_new(...)`, `_field_mut()` |
| Go | `struct`, exported field per pack field | `interface Pack_Fields`: `Field()` to send, `SetField(v)` to receive; wrapped by `type Pack struct{ Pack_Fields }` (`NewPack(fields)`) |
| C++ | `struct Pack final`, member per field | `struct Pack` with pure virtual `_field() const` to send, `_field(value)` to receive; your class implements it |
| C | `struct gen·__·Pack`, member per field (+ `_some` flag for optional) | your type `gen·<Host>·_allocator·<Pack>` with accessor functions `gen·__·Pack·_field(self)`, `gen·__·Pack·set__field(self, value)` the codec calls |
| Swift | `public struct Pack`, `public var` per field | `protocol Pack.Fields`: `_field()` to send, `set__field( v )` to receive; collection or nested pack via closures over its storage `_field { … }`, `_field_mut { … }`; default `Pack.Impl`, named by `_allocator` of host file |
| Dart | `class`, field per pack field | `abstract class` with `T get $field` to send, `set $field( T src )` to receive; your class extends it (demo `PackImpl`) |
| Python | class, property per field, over the C value | none of its own: pack abstract in C part of host, implemented there as C; Python sends/receives only concrete packs; nested abstract pack = opaque object |

- GEN: real case AdHocAgent: on its host `Agent` project-describing packs are abstract in C#:
```csharp
struct Agent : Host
{
    public class Project
    {
        public class Host
        {
            byte uid;
            uint default_impl_hash_equal;
            // ... more fields
        }
    }
}
// generated (abridged):
public partial interface Host : AdHoc.Connection.Transmitter.BytesSrc
{
    public byte _uid { get; }
    public uint _default_impl_hash_equal { get; }
    // ... serializer, a default interface method, pulls fields through these getters
}
// agent's model class implements it; serializer reads agent's structures directly:
public class HostImpl : Entity, Project.Host
{
    public byte _uid => (byte)uid;
    public uint _default_impl_hash_equal { get; set; }
    // ...
}
```
- GEN: received abstract pack needs object: generated host file has `_Allocator` with one factory per received pack, named `new_` + pack path with `_` for dots (`new_Collector_Telemetry` for `Collector.Telemetry`; imported project's pack starts with project name: `new_AdHocProtocol_Server_Info`).
- RT: concrete pack default factory makes new generated object; abstract pack default factory throws "The producer of ... is not assigned". Assign own factory before first such pack arrives. Java, host `Collector` (§10.5.2):
```java
Collector._Allocator.DEFAULT.new_Collector_Telemetry = receiver -> mySink;
```
- GEN: C# and TypeScript have same `_Allocator`, same factory names (C#: `_Allocator.DEFAULT.new_... = receiver => ...;`). Go: `Allocator_DEFAULT`, factory `New_<Pack>` per received pack. Rust, C++, C, Swift: same `_Allocator` of host with `DEFAULT`, per pack fields `new_<Pack>` and `free_<Pack>`, assign the two together: Rust `_Allocator::DEFAULT.new_P.set( |scope| NonNull )` / `.free_P.set( |pack| … )`; C++ `_Allocator::DEFAULT.new_P = []( const void* scope ) -> P* { … }`, `free_P`; C `gen·<Host>·_Allocator·DEFAULT.new_P`, `.free_P`; Swift `H._Allocator.DEFAULT.new_P = { scope in UnsafeMutablePointer }`, `free_P`. `new_P` gives reference to the place: pack or type starting with it, heap, arena, same place each time, initialized; scope = connection. Default: `arena_P` field of `_Allocator`: place of an ended pack serves next one with its memory, a lock a pack; a hint: own arena (per thread, per batch) likely better. Called once, after `OnReceiving`, before first body byte; refused pack not made (roles → connection reset §26; actor state → read on into library's place, application not asked). After `OnReceived` pack is yours: keep = take in region `OnReceived` (Rust `take()` of slot, C++ `take( )` of `Place`, Swift `take_P()`, C pointer out of `as`, set `NULL`); not taken → `free_P` right after `OnReceived`; lost in middle → `free_P` (Resumable session goes on with it). Nested pack / collection item by value inside received pack, no factory. Value and empty packs: no place. File / stream pack: `new_P` = producer of sink, field of `_Allocator`. Abstract pack type still named at compile time: Rust `_allocator` module (`pub type Telemetry = …TelemetryImpl;`), C `gen/<Host>/_allocator/<Pack>.h`, Swift alias in `_allocator`; C++ class derived from pack. Per-connection state, receive/transmit loops calling your code →§13.
- RT: arrays in abstract pack: one-byte elements (`Binary`, `sbyte`, `byte`, one-byte enum) travelling as stored → C#, Java, TS, Go, C++, Dart: windows of receive buffer itself, one call per buffer portion, `item` argument = where in array portion starts. Rust, Swift, C: abstract pack gives codec array storage (`_field_new`; Swift `_field_mut`), codec copies whole buffer blocks into it. Every other array of abstract pack → element by element via per-element method: wider elements (also those travelling exactly as stored), elements stored wider than they travel (3 bytes on wire, `int` in memory), bit-packed items (codec reads/writes runs of whole bytes, hands items one by one). Concrete pack copies arrays of one-byte elements and of wider elements travelling as stored by whole buffer blocks.
- Live test host whose packs are all abstract:
```csharp
/**
        <see cref = 'InTS'/>--
        <see cref = 'InJAVA'/>--
        <see cref = 'InCS'/>--
        <see cref = 'InRS'/>--
        <see cref = 'InGO'/>--
        */
struct BlockEcho : Host{ }
```
- NOTE: byte-window API of single `Binary` field →§5.
- RULE: pack with `Stream` or `File` field cannot be used as generated on any host whatever modifier says (only your code supplies conduit source/sink): configured concrete → other fields stay concrete, pack becomes abstract class whose conduit methods you implement; configured abstract → interface as usual (→§15).

### §10.3.3 Equality and hash (second position)
KW: equality, hash, IEquatable, Equals, GetHashCode, equals, hashCode, hash(hash, pack), second position
- GEN: `+` in 2nd position → equality and hash methods (packs usable as keys in hash collections): C# `IEquatable<T>.Equals` + `GetHashCode`; Java `equals` + `hashCode`; TypeScript static `equals(a, b)` + `hash(hash, pack)`. In abstract pack: declared for your implementation.
- GEN: `-` → none emitted: less code, no hashing work for packs never in such collection.

## §10.4 Choosing `+` or `-`: generated objects or your own structures
KW: choosing, concrete vs abstract, existing model, whole host, mixed, Broker, per host
- RULE of thumb: ask where data lives in the host, not message size.
- Packs = host's data (new app whose model IS the protocol) → `+`.
- Host has own model (existing system getting network layer: DB node with own query/result structures, message broker with own request objects and log, client library with own records) → `-` on WHOLE host:
```csharp
/// <see cref='InJAVA'/>--
struct Broker : Host { }
```
  Every pack = interface over system's objects; data flows field by field in/out; nothing converted twice, no second model to sync. With `+` every message exists twice, one copy per message each direction. Listing packs one by one says same at more length and goes stale as packs are added.
- Mixed (model mostly protocol, few packs too large to hold at once or mapping onto existing structures) → `+` host default, only those packs/fields abstract (→§10.5; packs too large to materialize →§15).
- NOTE: choice per host: same pack abstract on host binding it to existing system, concrete on new client without own model; wire bytes identical either way.

## §10.5 Implementation management: host, pack and field rules
KW: implementation management, host, language, entity, field reference, Modify<Host>, field injection
- RULE: implementation kind is not a pack property; decided separately per (host, language, entity), entity = pack or single field. A rule pins exactly one combination; same pack on another host/language and all other packs keep own settings.
- DECL: configured ONLY in host's doc comment (`struct ... : Host` or `struct ... : Modify<Host>`). Never tag pack/field itself; open language scope and reference target:
```csharp
/**
<see cref='InCS'/>-                 // a C# marker: ABSTRACT implementation...
<see cref='Telemetry'/>             // ...for a whole pack (prefix @ to take its nested packs instead)
<see cref='Frame.samples'/>         // ...or for a single field
*/
```
- NOTE: field reference written like pack reference; agent distinguishes by what `cref` resolves to.
- PITFALL: `<see cref='Pack.field'/>+` or `-` in a PACK's comment = different feature, field injection (→§8). Implementation configuration read only from hosts.

### §10.5.1 Implementation levels and their precedence
KW: precedence, field level, pack level, Pack Set level, host default, most specific
- RULE: within one (host, language), pack/field setting comes from most specific level naming it:

| Level | Declared by | Applies to |
|:--|:--|:--|
| Field | marker followed by field reference `<see cref='Pack.field'/>` | that field only (implementation only) |
| Pack / Pack Set | marker followed by type reference: pack, Pack Set, host, project, with or without `@` | packs in that scope |
| Host default | LAST marker of the language, with references or without | every pack not named above |

- RULE: field rule beats pack rule: concrete pack with one field abstract → only that field abstract, rest stored. For field only 1st modifier char counts (`+` concrete, `-` abstract); equality/hash belong to whole pack.

### §10.5.2 How the agent reads a host comment
KW: three rules, marker with references, marker without references, default = last marker, default goes last, exceptions first, Collector, Router, Panel, @Target
- RULE (all of it):
  1. Marker WITH references under it gives its setting to them and to every reference below, until next marker of ITS language. Nothing closes a marker; markers of other languages between do not stop it.
  2. Marker WITHOUT references gives nothing to references below; it only proposes default of its language.
  3. Default of a language = its LAST marker in the comment (with or without references) → every pack no reference gives a setting in that language.
- DECL: reference names pack (itself), Pack Set (its packs), host/project (their packs; `@` → nested too), field. Field starts from setting of its pack, takes implementation character only. Language with no marker anywhere (host's comment or a modifier's) → not generated for host.
- One language, most hosts one line:
```csharp
/// <see cref='InJAVA'/>--       // generate Java for this host; every pack abstract, no equality and hash
struct Node : Host { }
```
- RULE: some packs differ → exceptions FIRST, default LAST:
```csharp
/**
<see cref='InJAVA'/>--          // the exceptions...
<see cref='RowsResult'/>
<see cref='Row'/>
<see cref='Cell'/>
<see cref='InJAVA'/>+-          // ...and the default: the last Java marker
*/
struct Driver : Host { }
```
  → `RowsResult`, `Row`, `Cell` abstract in Java; every other pack `+-`.
- PITFALL: default written first is no default: `InJAVA+-` then `InJAVA--` + `RowsResult` → every pack `--`; agent warns both lines change nothing.
- NOTE: AdHoc's own `Server` lists seven concrete Java packs, ends with `<see cref = 'InJAVA'/>--`; `Agent` lists twelve concrete C# packs, ends with `<see cref = 'InCS'/>--`.
- One pack abstract on one host:
```csharp
/**
    <see cref='InJAVA'/>-
    <see cref='Telemetry'/>

    <see cref='InJAVA'/>
*/
struct Collector : Host
{
    public class Telemetry { long sensorId; int sequence; float value; }
    public class Heartbeat { long timestamp; }
}
```
  → abstract `Telemetry` parsed field by field into handler, never allocated whole; `Heartbeat` (default `++`) ordinary stored object.
- One field abstract in concrete pack:
```csharp
/**
    <see cref='InCS'/>-
    <see cref='Frame.samples'/>

    <see cref='InCS'/>
*/
struct Router : Host
{
    public class Frame { long timestamp; int channel; [D(1_000_000)] int[,,] samples; }
}
```
  → `Frame` ordinary object; `samples` abstract: items handed to your code as they arrive, never held in `Frame`. Unbounded/abortable payload → `Stream` field (→§15).
- `@Target` (not a field) → every transmittable pack in scope of `Target` (pack, host, project, Pack Set) recursively; container itself excluded.
```csharp
class Dashboard { public class Cpu { float load; } public class Memory { long used; } }
/**
<see cref="InTS"/>--
<see cref="@Dashboard"/>
<see cref="InTS"/>+-
*/
struct Panel : Host { }
```
  → `Cpu`, `Memory` `--` on `Panel`; every other pack `+-`. What reference without `@` takes →§9.

### §10.5.3 Implementation rules in a host with several languages
KW: several languages, block per language, default stops passing, one setting in two languages, Driver, ObserverPacks
- RULE: three rules apply to every language at once. Simplest layout: block per language — its exceptions, then its default:
```csharp
/**
<see cref='InCS'/>-         // C#: the exceptions
<see cref='Payload'/>
<see cref='InCS'/>+-        // C#: the default - and, having no references, it passes nothing on
<see cref='InJAVA'/>--      // Java: the exceptions
<see cref='RowsResult'/>
<see cref='Row'/>
<see cref='InJAVA'/>+-      // Java: the default
*/
struct Driver : Host { }
```
| Pack | C# | Java | Why |
|:--|:--|:--|:--|
| `Payload` | `-+` | `+-` | C# line 1; Java named only below → Java default |
| `RowsResult`, `Row` | `+-` | `--` | Java line 4; C# marker above (line 3) has no references, passes nothing → C# default |
| every other pack | `+-` | `+-` | defaults: last C# marker (line 3), last Java marker (line 7) |
- RULE: marker with references keeps giving its setting to references below, in blocks of other languages too → one set of packs gets a setting in two languages:
```csharp
/**
<see cref='InTS'/>+-
<see cref='ObserverPacks'/>    // TS +-                 Java: the default
<see cref='InJAVA'/>+-
<see cref='SystemInfo'/>       // TS +- (line 1 goes on)  Java +-
<see cref='InJAVA'/>--
<see cref='Signup'/>           // TS +-                 Java --
<see cref='InTS'/>--
*/
```
  → every pack no reference names `--` in both (last TS marker, last Java marker).
- RULE: stop a marker passing its setting further → marker of its language without references (`<see cref='InTS'/>` before Java block → `SystemInfo`, `Signup` get TS default).

### §10.5.4 Host configuration: targets, warnings and errors
KW: configuration target, exit code 45, language section lists, does not declare, neither sends nor receives, comes before any language marker, changes nothing, set twice, modifier changes nothing
| Situation | Result |
|:--|:--|
| reference to pack, Pack Set, host, project or field after marker | configuration target |
| reference to enum, constants container, constant, or never-transmitted pack (header, `TYPEDEF`, ...) after marker | ignored without message (no implementation kind) |
| reference to other entity after marker (connection, actor, state, `All`, ...) | error (exit code 45): "The host ... (...) configures `...` in a language section. A language section lists packs, Pack Sets, hosts, projects and fields only." |
| unresolvable reference | error: "`...` (...) refers to `...`, which the description does not declare." |
| reference before first marker | warning: "The host ...: <see cref='...'/> (...) comes before any language marker such as <see cref='InCS'/>, so it configures no language and is ignored." |
| configured pack host neither sends nor receives | warning "The host ... (...) configures packs it neither sends nor receives - a typo? The settings have no effect and are dropped:" + pack list; run continues |
| reference whose packs get that setting anyway (default or another reference) | warning "… <see cref='...'/> (...) under <see cref='InJAVA'/>+- changes nothing - what it names gets that setting anyway: the default, or another line."; run continues |
| marker that changes nothing: no references, later marker of its language gives default | warning "… <see cref='InJAVA'/>+- (...) changes nothing - a later marker of its language gives the default, and no reference under it takes this one."; run continues |
| pack two references of one comment set differently | warning "… Order is set twice in one comment - by … (…) and by … (…). The later one holds: InJAVA -- gives way to InJAVA +-. Keep one of them."; run continues |
| host modifier whose configuration changes nothing at all | warning "… the language configuration of … (…) changes nothing - every pack and field of the host gets what it gets without it."; run continues |
- RT: line checked in its OWN comment: comment read without it; every pack/field gets same → line changes nothing. Of two equal lines the later named. Modifier restating host's setting not reported line by line (overriding host = its job); modifier changing nothing at all is.
- PITFALL: broad scope `<see cref='@MyProject'/>` names every project pack → every pack host does not use listed as redundant. Narrow scope or use filtered Pack Set (→§9).
- PITFALL: in `/** */` block, marker with nothing after it on its line takes modifier from START of next line; if that line starts with `+`/`-` (list bullet) that char becomes modifier. Keep prose lines from starting with `+`/`-`, or always write modifier.

## §10.6 Modifying an imported host
KW: Modify<Host>, imported host, ServerTuning, comment read on its own, laid over host, reference stronger than default, X<Host>
- DECL: `struct` directly in your project whose FIRST base is `Modify<ImportedHost>`; configuration lines in its comment as usual:
```csharp
// Deploy.cs
public interface Deploy : Platform
{
    /**
    <see cref='InJAVA'/>
    <see cref='Platform.Order'/>

    <see cref='InJAVA'/>--
    */
    struct ServerTuning : Modify<Platform.Server> { }
}
// Platform.cs
public interface Platform
{
    class Order   { long id; }
    class Receipt { long id; long total; }
    /**
    <see cref='InCS'/>--
    <see cref='InJAVA'/>+-
    */
    struct Server : Host { }
    /// <see cref='InTS'/>
    struct Client : Host { }
    interface Session : Connects<Client, Server>
    {
        [l____________<Order>]
        [____________r<Receipt>]
        struct Open { }
    }
}
```
- RT: each comment (host's, every modifier's) read ON ITS OWN by the three rules from its own first line; modifier inherits nothing from host's comment. Then laid over host, a language at a time:
  - language modifier names → modifier's default; language it does not name → host's (`ServerTuning` names no C# → C# stays `--`);
  - pack modifier's references name → modifier's setting in languages those references give;
  - pack host's references name and modifier's do not → host's setting kept (reference stronger than default);
  - modifier's languages added to host's (can turn new language on); modifier generates nothing of its own.
- RT: result above: `Server` `--` in both languages (C# own comment, Java last marker of `ServerTuning`), except `Order` `++` in Java. Without `<see cref='InJAVA'/>--` last Java marker of `ServerTuning` = `<see cref='InJAVA'/>` → every other pack of `Server` `++` in Java.
- NOTE: other kinds of `Modify<>`, removing imported host with `X<Host>` →§22.

## §10.7 A host as a named Pack Set
KW: host as Pack Set, Server, @Server, directly declared, transmittable
- RULE: host name = Pack Set of every transmittable pack declared DIRECTLY in host body. `[____________r<Server>]` (§10.1) lets server send `Server.Receipt`, `Server.OutOfStock`; pack added to `Server` later joins branch by itself. With `@` (`<see cref='@Server'/>`, `[____________r<@Server>]`) → packs nested at every depth. Same reading in host configuration comment.

## §10.8 What the generator produces for a host
KW: gen, lib, demo, org.unirail, org.unirail.collections, namespace clash, package.json, tsconfig.json, prestart, tsx, typecheck, undefined
- GEN: per host and per named language a separate source tree: generated code (`gen`), runtime library (`lib`), demo, project files of that language. Layout and delivery →§1, →§25.
- GEN: `gen` code takes project's namespace; `lib` always in `org.unirail` (and `org.unirail.collections`) whatever your namespace.
- PITFALL: two independently generated projects in one process (one JVM, one .NET process, one JS bundle) → two copies of `org.unirail` library types, clash. Keep one project's generated code per process, or give one project's `lib` own namespace.
- GEN: TypeScript project has `package.json` and `tsconfig.json`; tsconfig beyond `strict`: `noImplicitOverride`, `noImplicitReturns`, `exactOptionalPropertyTypes`, `noPropertyAccessFromIndexSignature`, `noUncheckedSideEffectImports`, `isolatedModules`; unreachable code and unused labels = errors.
- CLI: `npm start` runs `prestart` script `tsc --noEmit -p .` first; demo starts only when types hold. Demo runs via `tsx` (executes TS without type check) → starting with `tsx` or `node --import tsx` directly skips check. `typecheck` script runs same check alone.
- PITFALL: generated TS code and runtime lib spell "no value" as `undefined`, never `null`; passing `null` does not compile.
- NOTE: related →§4, →§9, →§11, →§15, →§22, →§20.

# §11 Connections
KW: connection, Connects<L, R>, VirtuallyConnects<L, R, PATH>, left host, right host, Session, Connection, External, Internal
SUMMARY: declaring `Connects<L,R>`, one connection per host pair, self-connection `Connects<X,X>`, composition/`SwapHosts`, runtime `Connection` (External/Internal, events, ending), connection timeouts, serial-link `Framing`.
- Model: project → host → **connection** → actor → state → branch → pack → field.
- `connection` = static definition of link between two hosts; typed pipe for all they say to each other. Every message, state transition, RPC call declared inside one. Contents (actors, states, branches) →§12.
- NOTE (diagram): `Connects<Client, Server>`: L = first type arg, R = second. Body lanes: Actor0 (states in body), named actor, RPC actor; packs both directions. Runtime: transport (External) ↔ generated `Connection` (Internal: transmitter, receiver, actors); events → `OnExternalEvent` → actors (1), then `Connection.OnEvent.handlers` (2) →§11.6.

## §11.1 What a connection is
KW: protocol flow, state machine, socket, port, Multiplexing
- Body = `protocol flow`: order packs may travel + valid answers to each; built from actors/states/branches (→§12) = one state machine per actor tracking which packs may be sent/received now.
- NOTE: says nothing about sockets; own port vs port shared with host's other connections decided at deployment →§19.

## §11.2 Declaring a connection
KW: Connects<L, R>, org.unirail.Meta.Connects, Host, struct, left, right
- DECL: C# interface, FIRST base `org.unirail.Meta.Connects<L, R>`, directly in project interface. First type arg = **left** host, second = **right**; both hosts (`struct` implementing `Host`). Branches say whether left or right sends.
```csharp
using org.unirail.Meta;
namespace com.example.shop
{
  public interface Shop
  {
    class Order { long id; }
    /// <see cref="InCS"/>
    struct Server : Host { public class Receipt { long id; long total_cents; } }
    /// <see cref="InTS"/>
    struct Client : Host { }
    interface Session : Connects<Client, Server>
    {
      [l____________<Order>] [____________r<Server.Receipt>] struct Open { }
    }
  }
}
```
- `Client` (left) sends `Order`; `Server` (right) sends `Receipt`.
- NOTE: host joins any number of connections; connection may carry pack declared anywhere in project, incl. inside another host. Live example (one host, several single-state connections, no transitions); `TrialClient.TrialClientPack` (declared in host `TrialClient`) also travels on `FrontendServer`–`FullFeaturedClient`:
```csharp
interface TrialCommunicationActor : Connects<FrontendServer, TrialClient>{
  [l____________<(Point3, Root, TrialClient.TrialClientPack)>]  // FrontendServer → TrialClient
  [____________r<(Point3, TrialClient.TrialClientPack)>]        // TrialClient → FrontendServer
  struct Start { }
}
interface CommunicationActor : Connects<FrontendServer, FullFeaturedClient>{
  [l____________<(Point3, Root, TrialClient.TrialClientPack, FullFeaturedClient.Login, FullFeaturedClient.FullFeaturedClientPack)>]
  [____________r<(Point3, TrialClient.TrialClientPack, FullFeaturedClient.FullFeaturedClientPack)>]
  struct Start { }
}
```

### §11.2.1 Rules for declaring a connection
KW: declare it directly in the project, has no state, exit code 45, exit code 22, is declared inside the connection, Resumable, HeaderFor
| Rule | Otherwise |
|:--|:--|
| declared directly in project interface | error "The connection ... (...) is declared inside another declaration: declare it directly in the project." |
| `Connects<L, R>` (or `VirtuallyConnects<L, R, PATH>`) is FIRST base | not taken as connection; its states reported as declared outside a connection |
| `L`, `R` are hosts | C# compiler rejects type args |
| non-virtual connection has ≥1 actor with ≥1 state | error "The connection ... (...) has no state: declare at least one in its body." →§12.1 |
| root project has ≥1 connection | error (exit code 45) "The description declares no connection, so there is nothing to generate." |
| two hosts joined by ≤1 connection | error →§11.3 |
- DECL: body may contain: states (empty `struct`s with branch attributes); actors, RPC methods (→§12, →§13); header packs = classes implementing `HeaderFor<...>` (→§8); `Resumable<HOST, PACKS>` interfaces — allowed ONLY here (→§14); interface-based modifiers of states and actors.
- RULE: anything else (ordinary pack, `class X : Modify<SomeHeader>`, any enum, non-state struct) → exit code 22 "... is declared inside the connection ...: a connection body holds its actors, states and RPC methods only." Declare packs outside, reference from branches.

## §11.3 One connection per pair of hosts
KW: one connection per pair, joined by more than one connection, VirtuallyConnects, MaxTunnels, host order
- RULE: two hosts joined by **at most one** connection, physical `Connects<>` or virtual `VirtuallyConnects<>` (→§18), either order; `Connects<A, B>` = `Connects<B, A>` pair. Else error "The hosts A and B are joined by more than one connection: ... A pair of hosts may be joined by at most one connection — physical or virtual, in either host order."
- NOTE: one is always enough: carries both directions; any number of actors converse concurrently; virtual connection already carries up to `MaxTunnels` concurrent tunnels over its path → no parallel tunnel declarations needed (→§18).

## §11.4 Connecting two hosts of one type
KW: Connects<X, X>, self-connection, dialed, accepted, actor-instance id space, VirtuallyConnects<Peer, Peer, Relay>, IfSendingFrom, one meaning per pack
- `host` = TYPE of node, not one process. Same-type nodes talking (broker replication, cluster gossip, mesh) → that type on both sides:
```csharp
/// <see cref='InJAVA'/>
struct Broker : Host
{
  public class Append { long offset; [D(+4096)] string record; }
  public class Ack    { long offset; }
}
[ReceiveTimeout(30)]
interface InterBroker : Connects<Broker, Broker>
{
  [l____________<Broker.Append>] [____________r<Broker.Ack>] struct Replicating { }
}
```
- GEN: type's code generated once, holds **both ends**: every pack both sent and received; every state machine runs left and right branches; every RPC both called and answered.
- RT: host that **dialed** = **left**, host that **accepted** = **right**. Left-sent pack → sent by caller, received by acceptor, and vice versa → state machines, RPC roles, header directions keep meaning. Example: dialing broker sends `Append`, acceptor answers `Ack`.
- RT: only runtime knowledge = own half of actor-instance id space: dialer left half, acceptor right half → instances created at both ends of one socket at same moment never share id. Decided once at socket connect; nothing added per pack.
- RULE: `Connects<X, X>` ≠ connection of a program to itself: joins two **different instances** of host type `X` (two processes, usually two machines, same generated code) over a socket between them.
- RULE: `Connects<X, X>` = the one connection of pair `(X, X)`.
- PITFALL: don't invent second host type (`BrokerPeer`) for calling side → two generated modules for one program, same generated class names → can't load side by side.
- NOTE: virtual same-type: `VirtuallyConnects<Peer, Peer, Relay>` = peers reachable only via relay; `Peer → Relay → Peer` over `PeerToRelay` twice; side that opened tunnel = left (→§18).
- PITFALL: both hosts open one tunnel at same time → both hear `THIS_CONNECT`, both left half of actor-instance ids, two different instances share id. Fix: `Tunnel.side( tunnel_id, carrier )` set at startup, rule on what both ends know (own address, `tunnel_id` = address of other end), different answer at each end (→§18.6.2).
- NOTE: trim endpoint `IfSendingFrom<Host, Connection>` on same-type connection names both legs (→§17). On multiplexed port opened with uid of shared host's type (→§19).
- RULE: **one meaning per pack in a state**: each end receives what either side sends, identifies pack by id only, not sender → pack may not be transition when left sends and stay when right sends in one state. Agent refuses state, names pack. Fix: separate packs (request that stays, pack that ends state).
```csharp
interface Mesh : Connects<Node, Node>
{
  interface Call : Actor
  {
    int Actor.MaxActiveInstances => 1;
    [L____________<Talking, Node.Ring>] struct Start { }
    [____________r<Node.HangupRequest>]   // the right side asks
    [L____________<End, Node.Hangup>]     // the left side ends; [____________r<Node.Hangup>] here would be refused
    struct Talking { }
  }
}
```

## §11.5 Composing connections and SwapHosts
KW: SwapHosts<Base>, <see cref='SwapHosts{Base}'/>, base connection, X<Base.State>, Modify<Connection>, Modify<TargetConnection, L, R>, _<Connection>
- DECL: connection may take states of another (usually of imported project →§22) instead of repeating them:
```csharp
// Office.cs
using org.unirail.Meta;
namespace com.example.office
{
  public interface Office : Chat
  {
    class Command { [D(+100)] string text; }
    /// <see cref='InCS'/>
    struct Guest : Host { }
    /// <see cref='InJAVA'/>
    struct Bot : Host { }
    // Guest plays Member's part: it is the left host, as Member is in Talk.
    interface GuestTalk : Connects<Guest, Chat.Hub>, Chat.Talk
    {
      [l____________<Command>] struct Commands { }
    }
    // Here the hub is the LEFT host: SwapHosts turns Talk around, so Hub still answers and Bot plays Member.
    /// <see cref='SwapHosts{Chat.Talk}'/>
    interface BotTalk : Connects<Chat.Hub, Bot>
    {
      [____________r<Command>] struct Commands { }
    }
  }
}
```
```csharp
// Chat.cs
using org.unirail.Meta;
namespace com.example.office
{
  public interface Chat
  {
    class Say  { [D(+500)] string text; }
    class Seen { long message_id; }
    /// <see cref='InTS'/>
    struct Member : Host { }
    /// <see cref='InJAVA'/>
    struct Hub : Host { }
    interface Talk : Connects<Member, Hub>
    {
      [l____________<Say>] [____________r<Seen>] struct Chatting { }
    }
  }
}
```
- `GuestTalk` = copy of `Chatting` + own `Commands`: `Guest` sends `Say`, `Command`; `Hub` sends `Seen`. `BotTalk` = same, sides swapped: `Hub` (left) sends `Seen`; `Bot` (right) sends `Say`, `Command`.
| Form | Where | Effect |
|:--|:--|:--|
| `interface C : Connects<L, R>, Base` | base list after `Connects<>` | states of `Base`'s own actor (declared directly in its body) copied into `C`'s own actor |
| `SwapHosts<Base>` | `<see cref='SwapHosts{Base}'/>` in connection doc comment | same copy, left/right branches of every copied state swapped |
| one state of another connection | `<see cref='Base.State'/>` in doc comment | that state alone copied |
| `X<Base.State>` | base list, after the base it removes from | that copied state left out |
| `<see cref='Base.State'/>-` | connection doc comment | copied state left out too (doc comment read after base list) |
- RULE: `Connects<>` stays first base; composed-from bases follow. Written after them → run stops: "The connection … has `Connects<…>` after other bases: it goes first".
- RULE: only base's own-actor states copied; its named actors and RPC methods NOT — declare them in composed connection.
- RULE: composed connection needs no own state: copied states are its states. Base may be declared anywhere (this project, before/after, or extended one).
- RULE: copied + own states = one actor → one valid state machine: all independent, or one chain (→§12.7).
- RULE: one-connection-per-pair still applies to composed connection.
- RULE: composition makes another connection between another host pair. To change an existing connection itself (add actors, RPC methods, Actor0 states, remove states, replace attributes, give it other hosts) → connection modifier `Modify<Connection>` / `Modify<TargetConnection, L, R>` →§22.3.6. Adding whole imported connection with `_<Connection>` on project →§22.

## §11.6 The connection at runtime
KW: Connection, session, External(ext), External(null), OnExternalEvent, OnEvent.handlers, OnOpen, OnClose, THIS_CONNECT, REMOTE_CLOSE_ABRUPTLY, IsClose, Close(), Abort(), parked
- GEN: per connection, each host gets generated `Connection` class (all languages alike) in namespace named after connection: `com.example.shop.Session.Connection`.
- `session` = one live `Connection` instance = one conversation between two running hosts; network connection = only its transport; with `[Resumable]` (→§14) session outlives lost socket.
| Half | Provided by | What |
|:--|:--|:--|
| External | transport: TCP/WebSocket connection, tunnel endpoint | moves bytes; `IsOpen`, `Close()`, `Abort()`, two timeouts |
| Internal | generated `Connection` | gives bytes to send (transmitter), takes received bytes (receiver); owns actors; hears events |
- GEN: parameterless constructor. `External(ext)` seats on transport, returns previous transport; `External(null)` unseats (multiplexer's shared host → back to its pool). Per-language wiring of servers/clients/multiplexed ports →§19.
- RT: transport reports via `OnExternalEvent(connection, event)`; generated code gives event to actors FIRST, then your handlers: connect puts actors in start state, so pack a handler sends moves them on from there. C#:
```csharp
com.example.shop.Session.Connection.OnEvent.handlers += (conn, e) =>
    org.unirail.Network.TCP.onEventPrintConsole(conn.ext_connection, e);
```
- RT: `OnOpen()`/`OnClose()` exactly once per network connection session sits on, always alternating: `OnOpen` when usable (WebSocket: after upgrade), `OnClose` when session leaves it, whatever ended it. Non-resumable: once per session. Resumable: `OnClose` when socket lost and parked, `OnOpen` again on continuing socket; close **event** reaches actors only once, at session end (→§14). Both hooks have injection points in generated class; own per-connection state + cleanup →§13.
| Event | Meaning |
|:--|:--|
| `THIS_CONNECT` / `REMOTE_CONNECT` | this side connected / peer connected to this side |
| `THIS_CLOSE_GRACEFUL` / `REMOTE_CLOSE_GRACEFUL` | this side / peer closed in order, after queue drained |
| `THIS_CLOSE_ABRUPTLY` / `REMOTE_CLOSE_ABRUPTLY` | this side / peer broke off (abort, reset, socket error) |
| `TRANSMIT_TIMEOUT` / `RECEIVE_TIMEOUT` | connection timeout ran out →§11.7 |
| `WEBSOCKET_...` | same + WebSocket-specific (handshake failure, ping, pong) |
- GEN: events = flags; helpers `IsClose()`, `IsRemote()`, `IsGraceful()`, `IsAbrupt()`, `IsTimeout()`.
- RT: ending: Graceful close = `Close()` on transport or FSM reaching `Close`: no new packs accepted, queue drains, link closes. Abort = `Abort()`: hard stop, buffered data may be lost. Loss = socket error, vanished peer, timeout your code answered with 0 (default →§11.7): with `[Resumable(seconds)]` on current state or connection → session **parked**, waits for peer; else ends with connection. Graceful close by your code and abort ALWAYS end session (→§14).
- NOTE: `End` ends one actor pair, connection stays open; `Close` closes connection (→§12.4.3). Virtual connection ends with same events and `OnClose()` (→§18). Every connection a listening host accepts can be encrypted; transform chain on connection wraps all it carries (→§16).

## §11.7 Connection timeouts: TransmitTimeout and ReceiveTimeout
KW: [TransmitTimeout(seconds)], [ReceiveTimeout(seconds)], link, stage, OnReceiveTimeout, OnTransmitTimeout, int.MaxValue, OnTimeout Receiver, OnTimeout Transmitter, [Deadline], [Idle], 60 seconds, Pulse_
- DECL: three limits, **seconds**: `[ReceiveTimeout]` on the **connection** = **the link** (how long peer may be silent on socket); `[ReceiveTimeout]` on a **state** = **the stage** (how long this side waits in state for a pack of peer); `[TransmitTimeout]` on a **state** = how long peer may take to accept what this side wrote.
- NOTE: the two `[ReceiveTimeout]` are different: link → peer gone (process died, network fell, NAT forgot); stage → peer there but does not do what protocol expects in state. Connection carries one link limit per session + at any moment the stage limit of state entered last.
```csharp
[ReceiveTimeout(30)]                                        // the link: a peer silent on the socket for 30 s is gone
interface Chat : Connects<Client, Server> { /* ... */ }
```
- Stage, AdHoc `Communication` excerpt:
```csharp
/// <summary>STAGE 1: The initial state. The Agent (Left host) must send its `Version`,
/// which transitions the state machine to the `VersionMatching` state.</summary>
[TransmitTimeout(12)]                                       // Sets a 12-second timeout for this state.
[L____________ /*ÿ*/<VersionMatching, Agent.Version>/*ÿ*/] /*ÿ*/ // Master L→R: send Version, transition to VersionMatching.
struct Start /*ÿ*/{ }
// ...
/// <summary>STAGE 6: The Server processes the project and sends a final response from the `Info_Result` pack set, then exits.
/// While it works it says what it is doing - `Progress`, any number of times, the state stays.</summary>
[ReceiveTimeout(120)]
[____________R /*ÿ*/<Close, Info_Result>/*ÿ*/] /*ÿ*/
[____________r<Server.Progress>/*Ā*/]
struct Project /*Ą*/{ }
```
- Example: `Project`: Agent waits ≤120 s for result; each `Progress` from Server restarts wait.
| | link: `[ReceiveTimeout]` on connection | stage: `[ReceiveTimeout]` on state | `[TransmitTimeout]` on state |
|:--|:--|:--|:--|
| Set | once per session as it opens, on a side receiving anything on connection | on state entry, side receiving packs in it | on state entry, side sending packs in it |
| Absent | 60 s | no limit | 60 s |
| Restarted by | any arriving byte, `Pulse_` included | a pack of peer, any actor, one the actor drops included; NOT service packs (`Pulse_`, `Resumable`'s) | peer taking written bytes |
| Runs out | `OnReceiveTimeout()` | `OnReceiveTimeout()` | `OnTransmitTimeout()` |
- Example: `Start`: Agent sends → 12 s transmit; Server receives → no stage limit, its link limit still holds.
- PITFALL: stage set on state entry → state of an actor whose states never change (chain / function group, →§12.7) has no stage limit: never entered.
- RT: on expiry transport asks `OnReceiveTimeout()` / `OnTransmitTimeout()` how much longer: generated default 0 → closed as loss (resumable session parked). Your code may return ms to wait, or `int.MaxValue` = no limit this time. No own heartbeat needed for link: `Pulse_` (→§11.7.1). Current state hears timeout at `OnTimeout Receiver` / `OnTimeout Transmitter` injection points (→§13).
- RULE: stage + transmit limits on connection's own states (Actor0). On another actor's state agent WARNS (value still connection-wide, set by whichever instance entered last). Per-instance limit, connection and other actors keep running → `[Deadline(seconds)]`, `[Idle(seconds)]` (→§13). Link limit goes on connection itself.
| | `[ReceiveTimeout]` on connection | `[TransmitTimeout]`/`[ReceiveTimeout]` on state | `[Deadline]`/`[Idle]` |
|:--|:--|:--|:--|
| Scope | whole connection | whole connection | one actor instance |
| Set by | session opening | entering state (last wins) | instance entering state / its packs |
| On expiry | connection closes (or parked as loss) | connection closes (or parked as loss) | instance ends; connection lives |
| Where | the connection | states of connection's own actor | any actor's states (`[Idle]` also on actor) |

### §11.7.1 Pulse_: the line that is not silent
KW: Pulse_, service pack, keepalive, heartbeat, idle link, accepting side, dialing side, tick, 15 seconds
- GEN: service pack `Pulse_` added to every project with a TCP or WebSocket connection, wire id 255; never declared or handled by your code.
- RT: side that **accepted** says `Pulse_` after saying nothing for 1/3 of link's `[ReceiveTimeout]`. Side that **dialed** answers each unless it said something in last half of that third. Dialing side receiving nothing on connection says it by itself. Browser client keeps no timer for it: only answers.
- WIRE: pack id alone (1 byte with 1-byte id). Line carrying packs carries no `Pulse_`; idle line ≈ two per third of link's `[ReceiveTimeout]`.
- RT: not before session starts (WebSocket upgrade), not before `Resumable` sides agree, not in graceful close; never journalled; never handed to handlers.
- RT: dead peer noticed within link's `[ReceiveTimeout]`. PITFALL: most runtimes check silence once per loop tick, 5 s default → keep link ≥15 s.
- NOTE: not on `[UDP]` (link kept alive in datagram header, →§14.6) nor virtual connection (lives on carrier's socket).
- Related: →§10, →§12, →§14, →§18, →§19, →§22.

## §11.8 Serial links and fast recovery: Framing
KW: Framing, Connection.Transmitter.Framing, Connection.Receiver.Framing, 0xFF, 0x7F, CRC-16, CRC_ERROR, BYTES_DISTORTION, FFFF_ERROR, error_handler, switch_to, UART, RS-485
- NOTE: TCP/WebSocket/tunnels deliver bytes intact, in order. UART, RS-485, radio modem = bare byte stream (bytes lost, flipped, added). AdHoc pack has no length, no marker → one bad byte desyncs every later pack. Fix = **framing layer**, class pair between generated connection and stream:
| Class | Side | Does |
|:--|:--|:--|
| `Connection.Transmitter.Framing` | send | wraps every pack: mark `0xFF`, pack bytes with `0x7F`/`0xFF` escaped, CRC-16 |
| `Connection.Receiver.Framing` | receive | finds frames by mark, unescapes, checks CRC, passes pack on |
- NOTE: runtimes of C#, Java (`org.unirail.AdHoc.Connection.Receiver.Framing`), Rust (`connection::receiver::framing::Framing`, `connection::transmitter::framing::Framing`), Go (`Receiver.NewFraming(…)`, `Transmitter.NewFraming(…)`), C++ (`org::unirail::AdHoc::Connection::receiver::Framing`, `…::transmitter::Framing`), C (`adhoc·connection·receiver·framing·Framing`, `adhoc·connection·transmitter·framing·Framing`), Swift (`org.unirail.AdHoc.Connection.Receiver.Framing`, `org.unirail.AdHoc.Connection.Transmitter.Framing`), Dart (`Connection_Receiver_Framing`, `Connection_Transmitter_Framing`). Python: no class of its own; C part of a Python host has the C ones. TypeScript: none, by design (TS host runs over TCP/WebSocket).
```
0xFF  [ the pack's bytes, 0x7F and 0xFF escaped ]  [ CRC-16 of the pack, 2 bytes, escaped ]
```
- WIRE: one frame per pack; `0xFF` on line only as frame start. In frame, byte with low 7 bits all ones (`0x7F`, `0xFF`) written as `0x7F`, its top bit moves into next byte → escape costs **one bit**; all-such frame grows ≤1 byte in 8. Frame adds mark + 2 CRC bytes.
- RT: receive: unescaped in place in receive buffer, bytes passed to generated receiver as they come → decoded field by field. Pack **dispatched** (`OnReceived` handlers) only after CRC matches; bad CRC → dropped.
- RT: recovery: damaged byte (`0xFF` where impossible, frame ending before pack complete) → frame dropped, skip to next `0xFF`; next intact frame normal → fault costs damaged pack only. Lossy link: pair with guaranteed delivery for must-arrive packs (→§14).
- RT: errors → static `Connection.Receiver.error_handler` (prints to console unless replaced):
| Error | Meaning |
|:--|:--|
| `CRC_ERROR` | whole frame, CRC mismatch: pack dropped |
| `BYTES_DISTORTION` | impossible byte, or new frame before pack complete: frame dropped, seek next mark |
| `FFFF_ERROR` | two frame marks in a row |
- RT: send: `Transmitter.Framing.Read(buffer, offset, length)` fills buffer with framed bytes; returns count, `0` = no room for frame's next portion, `-1` = no pack waiting. Encodes in place, no second buffer; any-size pack goes out in portions.
```csharp
var rx = new AdHoc.Connection.Receiver.Framing(connection.receiver);        // in front of the generated receiver
var tx = new AdHoc.Connection.Transmitter.Framing(connection.transmitter);  // behind the generated transmitter
int n = port.Read(inBuffer, 0, inBuffer.Length);    // port's read loop: bytes as they come
if (0 < n) rx.Write(inBuffer, 0, n);
int m = tx.Read(outBuffer, 0, outBuffer.Length);    // port's write loop: frames as the port can take them
if (0 < m) port.Write(outBuffer, 0, m);
```
- NOTE: `connection` = generated connection created as host normally does; packs queued with `send` as always; framing only changes line format. `switch_to(...)` moves framing layer to another receiver/transmitter and resets it.

# §12 Actors, states, branches
KW: actor, state, branch, FSM, Actor, routing attribute, L____________, ____________R, l____________, ____________r, _____lr_____
SUMMARY: actors (incl. implicit Actor0), states, branches (routing attributes), branch pack selection (@ scopes, filters, X<>), pack-id dispatch/uniqueness, chain vs function group, grafting, generated code, worked examples, rules table. Actor kinds/runtime →§13.
- Model: project → host → connection → **actor → state → branch** → pack → field.
- `actor` = one independent conversation on connection with own FSM. `state` = phase of it. `branch` = attribute on state naming packs one side may send there, and whether sending moves to another state.
- RT: both hosts get same FSM; each knows at every moment which packs it may send/receive; generated code refuses anything else.

## §12.1 Actors: independent conversations on one connection
KW: Actor, org.unirail.Meta.Actor, MaxActiveInstances, UNLIMITED, RPC method, reserved identifier, exit code 123, declared inside another actor, grouping interface
- `actor` = unit of stateful behavior; owns FSM; current state decides allowed packs. Actors on one connection independent, each tracks own state (file transfer in one, chat in another, one socket, no waiting). RT: with several actors every pack carries its actor and, for actor with instances, the instance.
- DECL: interface extending `org.unirail.Meta.Actor` inside connection; body = states. `MaxActiveInstances` decides creation, addressing, limits of instances (→§13).
```csharp
interface Link : Connects<Client, Server>
{
  // an upload: the Client sends a blob, the Server acknowledges it, the conversation ends
  interface Upload : Actor
  {
    int Actor.MaxActiveInstances => UNLIMITED;
    [L____________<Waiting, Blob>] struct Start { }
    [____________R<End, BlobAck>] struct Waiting { }
  }
}
```
- RULE: placement: directly in connection body; or in plain interface nested in connection (= namespace for actor group; GEN: matching namespace); NOT inside another actor → "The actor X (...) is declared inside another actor. Declare it in the connection body." stops agent.
- DECL: RPC method in connection body or grouping interface, e.g. `(L____________, Pong) ping(Ping req);` = actor of its own: own class, own actor id, sibling of other actors, NOT part of Actor0. Return-type marker `L____________` / `____________R` = calling side; no marker → either side calls → two actors. Shorthand →§13.
- RULE: actor name → class name. Reserved, stop agent with "Error: 'X' (line N) is a reserved identifier and cannot be assigned." (exit code 123): `Actor0`, `id`, `Id`, `OnSerializing`, `OnSerialized`, `OnReceiving`, `OnReceived`, `OnEvent`, `BroadcastActor`, `BroadcastActorInstance`, `Actors`, `Actor`, `Connection`, `Virtual`.
- RULE: physical connection needs ≥1 actor with ≥1 state, else "The connection X (...) has no state: declare at least one in its body - an empty struct with branch attributes." Virtual connection (→§18) may have no actors; declared actors look exactly the same, packs travel through tunnel.

## §12.2 Actor0, the connection's own actor
KW: Actor0, implicit actor, Actor0.Acquire(conn), Actor0.one, actor id 0, no epoch, End, handshake, keep-alive
- `Actor0` = implicit actor; never declared: states written directly in connection body = its states. For whole-connection concerns: handshake, login, keep-alive, connection-wide notifications. Reserved name.
- RULE: exists only if body declares ≥1 state. Body with only named actors / RPC methods / grouping interfaces → no Actor0 (agent drops empty placeholder; GEN: nothing named `Actor0`).
```csharp
interface Link : Connects<Client, Server>
{
  //--- Actor0: the states written right in the connection body
  [TransmitTimeout(5)] [L____________<Greeting, Hello>] struct Start { }
  [ReceiveTimeout(10)] [____________R<Ready, Welcome>] [____________R<Close, Refused>] struct Greeting { }
  // the working state: no way out, Actor0 stays here until the connection ends
  [ReceiveTimeout(30)] [_____lr_____<Beat>] struct Ready { }
  //--- a named actor: a sibling of Actor0, not part of it
  interface Notify : Actor
  {
    [____________r<(Note, Alert)>] struct Messages { }
    [l____________<Flag>] struct Flags { }
  }
  //--- an RPC method: an actor of its own, also a sibling of Actor0
  (L____________, Pong) ping(Ping req);
}
```
- RT: one for connection's whole life, whatever its states; no `MaxActiveInstances`; actor id `0`. GEN: with transitions each connection holds own Actor0 instance, `Actor0.Acquire(conn)` returns it.
- RT: `End` doesn't free it: named actor at `End` released; Actor0's release = back to initial state.
- RT: no epoch (→§12.4.2): single instance, no pack from older one.
- GEN: all Actor0 states stay-only → function group (→§12.7): no `Acquire(conn)`; one shared `Actor0.one` serves every connection.
- RULE: `[TransmitTimeout]`/`[ReceiveTimeout]` (stage) of a state belong on Actor0's states; on named actor's state → warning (→§11.7). Link `[ReceiveTimeout]` goes on the connection. `[Deadline]`, `[Idle]` →§13.
- NOTE: Actor0's state does not gate named actors: a named actor or RPC method is a sibling, its packs are dispatched by actor id whatever state Actor0 is in. To keep actors, RPC methods and packs closed until the login (in whatever form it is done) → put them under a role rule (→§26): a side starts with no roles, everything under a rule is refused until your code sets the roles after the login (→§26.10).

## §12.3 States: the phases of a conversation
KW: state, empty struct, State.O, initial state, must be declared inside a Connection/Actor body, must be empty, FSM Integrity Error, Duplicate state names, exit code 22
- `state` = phase of actor's conversation; defines packs each side may send in it.
- DECL: **empty `struct`** with ≥1 branch attribute, in actor body (connection body for Actor0); everything in branch attributes.
```csharp
[L____________<Running, JobStart>] // the branch: the Left host sends JobStart, the actor moves to Running
struct Start { }                    // the state: an empty struct
```
| Rule | Otherwise |
|:--|:--|
| `struct` = state iff ≥1 branch attribute | - |
| declared inside connection or actor | "The State 'X' (line N) must be declared inside a Connection/Actor body." (exit 22) |
| empty: no fields, methods, nested types | "The state struct 'X' (line N) must be empty - states cannot contain fields, methods, or nested types..." (exit 22) |
| in connection/actor every `struct` is a state; struct without branch attributes (e.g. constants container) not allowed there | "'X' (...) is declared inside the connection ...: a connection body holds its actors, states and RPC methods only..." (exit 22) |
| names unique per actor, grafted included | "[!] FSM Integrity Error: Duplicate state names detected in Actor 'X'" + state map (→§12.9) |
- RULE: **initial state = first declared** (in connection body for Actor0); name clearly (`Start`, `Handshake`). GEN: also `State.O`. Actor with any transition: all other states reachable from initial (→§12.7).
- RT: FSM driven only by events: pack sent, pack received, timeout. Generator writes all transition code; your code attaches handlers to generated states (→§12.10). Handlers on one side called one at a time (→§13).
- RULE: agent numbers states per actor (like packs, branches); numbers live in description, never edit (→§20).
| On a state | Meaning | → |
|:--|:--|:--|
| `[TransmitTimeout(s)]`, `[ReceiveTimeout(s)]` | connection-wide timeouts, set on entry: transmit limit, stage | §11.7 |
| `[Deadline(s)]`, `[Idle(s)]` | how long one instance may stay / stay silent in state | §13 |
| `[Resumable(seconds)]` | reconnecting peer resumes session in this state | §14 |
| custom attributes | kept as constants of generated state | §23 |

## §12.4 Branches and the routing attributes
KW: branch, transitional branch, stay branch, routing attribute, L____________<Target, PACKS>, l____________<PACKS>, ____________R<Target, PACKS>, ____________r<PACKS>, _____lr_____<PACKS>, AttributeUsage, an interface that extends the branch marker
- `branch` = which packs one side may send in a state. `transitional branch` → FSM moves to target once one of its packs sent. `stay branch` (non-transitional) → FSM stays.
- DECL: branches = routing attributes on state struct, any number. All five declared in `Meta.cs` with `AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)` → C# refuses them elsewhere.
- RULE: interface derived from RPC markers `L____________` / `____________R` → "... is an interface that extends the branch marker .... A state is an empty struct that carries branch attributes".
| With `PACKS` | Without `PACKS` | Sender | Kind | Effect |
|:--|:--|:--|:--|:--|
| `[L____________<Target, PACKS>(...)]` | `[L____________<Target>(...)]` | Left | transitional | Left sends one → FSM to `Target` |
| `[l____________<PACKS>(...)]` | `[l____________(...)]` | Left | stay | Left sends, stays |
| `[____________R<Target, PACKS>(...)]` | `[____________R<Target>(...)]` | Right | transitional | Right sends one → FSM to `Target` |
| `[____________r<PACKS>(...)]` | `[____________r(...)]` | Right | stay | Right sends, stays |
| `[_____lr_____<PACKS>(...)]` | `[_____lr_____(...)]` | either | stay | either sends, stays |
- DECL: `(...)` = four optional ctor params `(KeepDoc, SkipDoc, KeepName, SkipName)`, same on all ten attribute classes; `PACKS` = pack set expression. Both →§12.5.

### §12.4.1 Reading a routing attribute's name
KW: letter position, upper case, lower case, Left host, Right host, lr
- RULE: letter POSITION = sender: left of underscores = Left host (first of `Connects<L, R>`), right = Right host, `lr` in middle = either.
- RULE: letter CASE: upper → transitional, sender leads state, moves FSM to `Target`; lower → stay.
- RULE: no transitional form for both sides; transition belongs to one side. `_____lr_____` = stay branch for Left + identical stay branch for Right → every per-side rule applies to it on both sides.

### §12.4.2 The side that leads a state
KW: leads, Main side, Master authority, Follower, BOTH Left and Right Master authority, epoch, 4-bit, Epoch mismatch
- `lead`: side with transitional branches in a state leads it = *Main* side ("Master authority" in agent messages), may move FSM out. Other side = **Follower**, only stay branches. Stay-only state: nobody leads, sides equal.
- RULE: one leader per state; both `L____________` and `____________R` on one state →
```text
State X line:N contains BOTH Left (L____________) and Right (____________R) Master authority branches.
A state can only assign Master authority to one side.
```
- NOTE: all transitional branches of a state belong to one host; leadership may change state to state (baton pass →§12.11). Roles from branches, not host names: `Server` leads only where a `____________R` is written.
- RT: both hosts run FSM replicas; only leader changes state → no racing transitions. Remaining race: Follower sends stay pack just before learning leader moved on → **epoch**: every instance of full actor (has `MaxActiveInstances`) keeps 4-bit epoch; every transition increments it, wraps after 16; every sent pack carries current epoch; receiver drops pack whose epoch ≠ its instance's. Injection point `Epoch mismatch` for your reaction. → late pack of previous state dropped, not taken as new state's.
- RT: pack for unknown instance opens new instance only if epoch = 0; other epoch = instance gone → dropped.
- RT: Actor0 and function-group actors have no epoch (single instance / never change state).

### §12.4.3 Transition targets: a state, End or Close
KW: TARGET_STATE, where TARGET_STATE : struct, org.unirail.Meta.End, org.unirail.Meta.Close, CS0453, not a known state, grafting
- DECL: first generic arg of `L____________` / `____________R` = `TARGET_STATE`, `where TARGET_STATE : struct`.
| Target | Meaning |
|:--|:--|
| state of this actor | FSM moves there |
| state of another actor / another connection | that state + all reachable from it copied into this actor (→§12.9) |
| `org.unirail.Meta.End` | conversation ends: actor instance pair released, connection stays open for other actors. Actor0: back to initial state |
| `org.unirail.Meta.Close` | graceful connection end: queued packs sent, then connection closes with all actors |
- RULE: other struct → "State X (line N): transitional branch attribute targets `T`, which is not a known state, `Close`, `End`, or `X<...>`. Fix the target and rerun." Message names `X<...>`, but `X<>` is interface → C# refuses it as `TARGET_STATE` (CS0453, `struct` constraint).
- RULE: `Close`/`End` inside `PACKS` ignored (targets, not packs).
- RULE: own state may be named `End`/`Close`: name resolves to own state (C# and agent alike); built-in then = `org.unirail.Meta.Close`.

## §12.5 Choosing the packs of a branch
KW: PACKS, pack set expression, KeepDoc, SkipDoc, KeepName, SkipName, _<>, X<>, Named Pack Set, filter template, @ scope
- Branch pack set = optional `PACKS` generic arg + optional four ctor filters `KeepDoc`, `SkipDoc`, `KeepName`, `SkipName`. Pack Set constructs (`_<>`, `X<>`, Named Pack Sets, scopes, filter templates) →§9.
- RT pipeline: `PACKS` tuple read left to right. Non-`@` elements (pack, host/project = own packs only, Named Pack Set, filter-template result, in-place `_<...>`) → branch list in tuple order; `X<...>` removes from list so far. `@` elements (`@Project`, `@Host`, `@Pack`, `@NamedPackSet`) → separate buffer → KeepDoc, SkipDoc, KeepName, SkipName → survivors appended at end. No `PACKS` + ≥1 filter → all transmittable project packs enter buffer. `X<...>` never removes `@`-buffer packs.

### §12.5.1 Direct packs and @ scopes
KW: @, direct inclusion, scope, transmittable, Unexpected item X in the S PACKS argument, written without @
- RULE: no `@` = direct inclusion: added unconditionally, filters never touch.
- RULE: `@` = scope: all transmittable packs inside, any depth → buffer → only buffer filtered → survivors added.
- RULE: `PACKS` with `@` scope → filters work ONLY on that scope, never widen to project.
- RULE: direct + filtered merged, no duplicates.
| Element | Without `@` | With `@` |
|:--|:--|:--|
| pack | pack itself | packs nested in it, recursively, NOT itself |
| host | packs declared directly in host | every pack in host, any depth |
| project | project packs not inside any host | every transmittable project pack |
| Named Pack Set | members, direct | members, fed to filters |
| filter template `F<@Scope>` | result, filtered by template's own attributes, direct | - |
| `_<...>`, tuple `(A, B, ...)` | elements, each by own rule | - |
| `X<...>` | removes packs (→§12.5.4) | - |
| connection | error "Unexpected item X (line:N) in the S PACKS argument" | same |
- Exact rules per element →§9. For branches:
  - `[_____lr_____<MyProject>]` = only packs outside project's hosts; `[_____lr_____<@MyProject>]` = every transmittable project pack.
  - RULE (warning): project/host without `@` with no own packs while hosts/nested packs have some → "The project `X` (line N) is written without `@`, so it adds only the packs declared right in it - none here. Its nested scopes (hosts, packs ) hold N transmittable pack (s): to send them, write `@X`."
  - connection is not a scope; for "all it may carry" use project, host, Named Pack Set.
  - RULE: **no `PACKS`** → filters run over all transmittable packs of YOUR project, any depth, hosts included — only if ≥1 filter given. Neither `PACKS` nor filter → selects nothing. Imported project's packs: `<@ImportedProject>` or list them.

### §12.5.2 The branch filters KeepDoc, SkipDoc, KeepName, SkipName
KW: KeepDoc, SkipDoc, KeepName, SkipName, unified doc pool, regex, .NET regular expression, named argument, not a valid regex, case-sensitive
- DECL: all `string`, default `""` = no filter; each = .NET regex.
| Param | Matches | Effect |
|:--|:--|:--|
| `KeepDoc` | **unified doc pool**: class-level `///` doc comment + tags (text after numbers on pack's line in packs table →§20), joined by space | keep matching |
| `SkipDoc` | doc pool | remove matching |
| `KeepName` | **path inside project**: dot-joined names (`Telemetry.Gps`, `Server.V2.Query`); no namespace, no project name | keep matching |
| `SkipName` | same path | remove matching |
- RULE: fixed order KeepDoc → SkipDoc → KeepName → SkipName, each on previous result → two Keeps narrow (must match both), Skips remove from survivors.
- DECL: positional (`""` placeholders) or C# named args:
```csharp
[l____________<@MyProject>(KeepName: @"(^|\.)V2\.")]   // named: skip straight to KeepName
[l____________<@MyProject>("", "", @"(^|\.)V2\.")]     // positional: empty KeepDoc and SkipDoc
```
- RULE: substring match anywhere: `"status"` matches `"final_status"` → word boundaries (`@"\bstatus\b"`) or distinct pattern. No `^`/`$` reliance in doc patterns (comment + tags = one text).
- PITFALL: `|` = alternation, spaces literal: `"📈 | server"` = `"📈 "` or `" server"`; write `"📈|server"`.
- RULE: case-sensitive (no `IgnoreCase` or other option).
- RULE: no leading dot: top-level pack = `Telemetry.Gps`, not `.Telemetry.Gps`; `@"(^|\.)Telemetry\."` matches scope at any level, `@"\.Telemetry\."` misses top level.
- RULE: invalid pattern → "Branch attribute at line N: KeepDoc pattern `...` is not a valid regex - ...", then "Fix the regex and rerun."
- NOTE: branch filters refine only `@` scopes (or whole project without `PACKS`). Filter attributes on Named Pack Set / filter template differ: refine every member of that set (→§9).

### §12.5.3 Branch pack collection examples
KW: examples, KeepDoc, SkipName, filter ignored, filter template, Tagged<SCOPE>
- Project `MyProject` (each branch below on a state of its own actor → no competition):
```csharp
public interface MyProject
{
  /// 📈 cpu load
  class CpuLoad { float value; }
  /// 📈 server memory
  class MemUsage { long bytes; }
  /// ⛔ 📈 legacy load
  class OldLoad { float value; }
  class Telemetry { public class Gps { double lat; double lon; } public class Speed { float kmh; } }
  class V2 { public class Query { int id; } public class QueryTest { int id; } }
  class StringMessage { string text; }
  class WarningEvent
  {
    /// critical
    public class DiskFull { int disk; }
    /// critical deprecated
    public class OldDiskFull { int disk; }
    public class Minor { int code; }
  }
  class ErrorEvent
  {
    /// critical
    public class Crash { string place; }
  }
  /// handshake
  class ClientHello { int version; }
  class ClientVersion { int version; }
  // ... hosts and the connection
}
```
| Branch | Resolved | Why |
|:--|:--|:--|
| `[l____________<(CpuLoad, MemUsage)>]` | `CpuLoad`, `MemUsage` | direct only |
| `[l____________("📈")]` | `CpuLoad`, `MemUsage`, `OldLoad` | no `PACKS`: KeepDoc over project |
| `[l____________("📈\|server", "⛔")]` | `CpuLoad`, `MemUsage` | KeepDoc, SkipDoc drops `OldLoad` |
| `[l____________(KeepName: @"(^\|\.)Telemetry\.")]` | `Telemetry.Gps`, `Telemetry.Speed` | KeepName over project |
| `[l____________<(StringMessage, @WarningEvent, @ErrorEvent)>("critical", "deprecated")]` | `StringMessage`, `WarningEvent.DiskFull`, `ErrorEvent.Crash` | `StringMessage` direct; filters see only the two scopes' nested packs: `Minor` not critical, `OldDiskFull` deprecated |
| `[l____________<@MyProject>(KeepName: @"(^\|\.)V2\.")]` | `V2.Query`, `V2.QueryTest` | KeepName over project scope |
| `[l____________<@MyProject>("📈", SkipName: @"Old")]` | `CpuLoad`, `MemUsage` | KeepDoc, then SkipName |
| `[L____________<Next>("handshake")]` | `ClientHello` | transitional, filter over project |
| `[L____________<Next, (ClientHello, ClientVersion)>]` | `ClientHello`, `ClientVersion` | transitional, direct |
| `[l____________<(CpuLoad, MemUsage)>("⛔")]` | `CpuLoad`, `MemUsage` | **filter ignored**: no `@` scope in `PACKS` |
- PITFALL: filter + `PACKS` without `@` scope → filter has nothing to work on, agent silent. Combine listed + filtered: `[l____________<(CpuLoad, @Telemetry)>(KeepName: "Gps")]`.
- DECL: filter template = reusable filtered scope; keeps packs whose doc pool holds `🔠`, either side sends, single-state connection:
```csharp
[KeepDoc("🔠")] interface Tagged<SCOPE> { }
interface Saving : Connects<Editor, Store>
{
  [_____lr_____<Tagged<@MyProject>>] struct Start { }
}
```

### §12.5.4 Removing packs with X<> inside a branch
KW: X<...>, subtract, tuple order, All<SCOPE>, @ packs not removed
- RULE: `X<...>` in `PACKS` subtracts unconditionally (no filters); removes only direct packs added by elements **before it in tuple**.
| `PACKS` | Result |
|:--|:--|
| `<(Server, X<Server.Ack>)>` | packs declared in `Server`, minus `Ack` |
| `<(X<Server.Ack>, Server)>` | all `Server` packs: subtraction came first, removed nothing |
| `<(NamedSet, X<B>)>` | set members minus `B` |
| `<(All<@Server>, X<(Server.Ack, Server.Nack)>)>` with `interface All<SCOPE> { }` | every pack in `Server` any depth, minus `Ack`, `Nack`: filter-template result is direct |
- RULE: `X<...>` takes packs out of `@` scopes of same `PACKS` too, wherever it stands in tuple (`@` packs filtered and added after whole tuple read, `X<...>` packs kept out): `<(@Server, X<Server.Ack>)>` = `<(X<Server.Ack>, @Server)>` = every pack inside `Server` but `Ack`.

## §12.6 Pack-id dispatch and uniqueness
KW: pack id, dispatch, discriminator, active scope, FSM AMBIGUITY, same packet ID claimed by multiple handlers, NO_COLOR, branch merge, does not have any packs to transmit
- RT: receiver tells packs apart only by pack id; inside actor, id alone picks branch → handler + next state. So one branch may list alternatives: four overloads of C# method `LogEvent(...)` → one state, branch of four packs, each own id + own handler:
```csharp
class NoArg { } // an empty pack for "no argument"
interface LogEventActor : Actor
{
  [l____________<(NoArg, StringMessage, WarningEvent, ErrorEvent)>] struct LogEvent { }
}
```
- NOTE: message of several kinds = several packs; pack id = discriminator; branch lists which a side may send (→§5).
- RULE: **pack claimed at most once per side in active scope** (else two handlers / next states for one wire pack). Active scope by shape (→§12.7):
| Shape | Scope checked | Pack may appear |
|:--|:--|:--|
| chain (has transitions) | each state alone, per side | once per state per side; may reappear in another state |
| function group (stay only) | all states together, per side | once per actor per side |
- RULE: per side = separate dispatch tables: pack may be in a Right branch and a Left branch. Two stay claims still conflict: two identical `[l____________<B>]` on one state = error; `_____lr_____<(A, B)>` + `l____________<B>` on one state = error (`lr` also Left). Report (colors off with `NO_COLOR`; decoration rules shortened here):
```text
━━━━━━━━━━
  FSM AMBIGUITY  ·  same packet ID claimed by multiple handlers
━━━━━━━━━━
  actor:  Link.Job
  side:   Left host
  scope:  active scope of state 'Processing'
  ▌ packet  Outcome   (com.demo.Shop.Outcome)
  ├─ state 'Processing'  ·  Stay  ·  line 32
  └─ state 'Processing'  ·  Transitional → End  ·  line 32
  ...
[ERR] FSM Ambiguity detected — see report above.
```
- NOTE: report lists each conflicting pack and claim: state, Stay or Transitional + target, file:line, or "grafted from" for grafted state, and the `X<...>` that takes conflicting packs out of the other branches.
- RULE: uniqueness checked BEFORE merge. Then one side's branches sharing a target merge into one branch (union); two stay branches of one side on one state merge too (both target state itself) → splitting a pack list over lines OK, listing a pack twice NOT.
- RULE: all branches of actor resolve to empty → "The actor X (line N) does not have any packs to transmit: every branch of its states resolves to an empty list of packs..." Usual cause: project/host without `@` (warning →§12.5.1 names it).

### §12.6.1 When one payload must mean two things
KW: inheritance, FinalStatusPayload, new pack id, word boundaries, \bstatus\b
- Same data, different outcomes in one state (update stays, final update ends): don't list pack twice; derive second pack by inheritance (→§8) = same fields, own id:
```csharp
class StatusPayload { string message; }
class FinalStatusPayload : StatusPayload { } // same fields, new pack id
interface Job : Actor
{
  int Actor.MaxActiveInstances => 8;
  [L____________<Processing, JobStart>] struct Start { }
  [l____________<StatusPayload>]           // the state stays
  [L____________<End, FinalStatusPayload>] // the state moves on
  struct Processing { }
}
```
- Or by tags, with mutually exclusive patterns. Tags `status` / `final_status`: plain `"status"` also matches `final_status` → FSM ambiguity. Word boundaries:
```csharp
[l____________(@"\bstatus\b")] [L____________<End>(@"\bfinal_status\b")] struct Processing { }
```

### §12.6.2 Broad inclusions next to explicit branches
KW: broad inclusion, X<>, subtract explicit packs, per side, function group scope
- RULE: broad inclusion (whole host, project, recursive scope) easily re-claims packs another branch of same scope claims on same side. Declare explicit branches first; subtract their packs from every broad inclusion, per side using one: `l____________` Left, `____________r` Right, both halves of `_____lr_____`. Function group: scope = whole actor.
```csharp
struct DeviceB : Host
{
  public class Ack { }
  public class Nack { int code; }
  public class Reading { int value; }
  public class Health { byte level; }
}
interface Bus : Connects<DeviceA, DeviceB>
{
  interface Replies : Actor
  {
    [____________r<(DeviceB.Ack, DeviceB.Nack)>] struct Acks { }
    // everything else DeviceB declares: Reading, Health
    [____________r<(DeviceB, X<(DeviceB.Ack, DeviceB.Nack)>)>] struct Others { }
  }
}
```
- `DeviceB` without `@` = direct → `X<>` after it works; with `@DeviceB` subtraction would not apply (→§12.5.4).

## §12.7 Chain or function group: the two shapes of an actor
KW: chain, function group, Stateful Actor, Singleton Actor, illegally mixes linked and isolated states, reachable, one, MaxActiveInstances, Call → Reply → End
- Exactly one shape, decided from branches:
  - **Chain**: ≥1 transitional branch; one connected FSM. RULE: **every state reachable from initial (first declared) by transitions**. Stay-only state reached by transition = regular final working state: no way out, actor stays till connection ends (like `Ready` →§12.2); named actor instance there still endable by `[Deadline]`/`[Idle]` (→§13).
  - **Function group**: no transitional branch; FSM never moves; all states always active = named groups of fire-and-forget operations; nobody leads; no per-conversation instance: GEN: one shared object `one` serves every connection.
- RULE: **mixing = error**: stay-only state no transition reaches can't sit beside a chain:
```csharp
interface Work : Actor
{
  int Actor.MaxActiveInstances => 4;
  [L____________<Running, JobStart>] struct Start { }
  [l____________<JobChunk>] [L____________<End, JobDone>] struct Running { }
  [____________r<Note>] // rejected: no transition leads here
  struct Overlay { }
}
```
```text
[FSM Integrity Error] Actor 'Link.Work' (line: 21) illegally mixes linked and isolated states.
  Linked chain states : 'Start', 'Running'  ( reachable from the initial state 'Start' )
  Isolated states     : 'Overlay'  ( no transition leads to them )
An Actor's FSM must be strictly one of the following:
  • Stateful Actor  — A single, connected state machine. ...
  • Singleton Actor — A collection of completely independent states using ONLY non-transitional branches ...
Resolution: Extract the isolated states into their own separate Actor.
```
- Fix: isolated states → own function-group actor beside chain:
```csharp
interface WorkNotes : Actor { [____________r<Note>] struct Overlay { } }
```
- PITFALL: order: chain's first declared state stay-only → initial state with no way out → rest unreachable → same error lists them isolated.
| | Chain | Function group |
|:--|:--|:--|
| transitional branches | ≥1 | none |
| active states | current only | all |
| dispatch table / pack-id uniqueness | current state, per side | all states together, per side |
| who leads | side with transitional branches in current state | nobody |
| instances | created and ended (`MaxActiveInstances`) | none, one shared object; `MaxActiveInstances` ignored with warning |
| name in agent messages | "Stateful Actor" | "Singleton Actor" |
- RULE: named chain actor must declare `MaxActiveInstances`, except actor shaped exactly as RPC call (Call → Reply → End). Function group needs none. →§13.

## §12.8 Multi-path transitions and loops
KW: multiple transitional branches, outcome by pack id, loop, refresh cycle, login cycle
- DECL: several outcomes = several transitional branches with own targets, all of leading side; receiver learns outcome from pack id:
```csharp
interface Vault : Actor
{
  int Actor.MaxActiveInstances => 4;
  [L____________<Evaluating, AccessRequest>] struct Start { }
  [____________R<VaultOpen, (AccessGranted, LimitedAccessGranted)>] // success, in two flavours
  [____________R<Close, AccessDenied>]                               // failure: the connection closes
  struct Evaluating { }
  [_____lr_____<VaultData>]        // both sides exchange data, the state stays
  [L____________<End, VaultLeave>] // the Client leaves, the conversation ends
  struct VaultOpen { }
}
```
- DECL: targets may point back → loops. Two exits from first state, pass-through state, refresh cycle:
```csharp
interface Viewing : Connects<Editor, Viewer>
{
  [L____________<LayoutSent, Layout>] // the layout first...
  [L____________<Operate, Document>]  // ...or the document right away
  struct Start { }
  [L____________<Operate, Document>] struct LayoutSent { } // passed through: the document follows the layout
  [____________r<ShowCode>]          // the Viewer's commands, the state stays
  [____________R<Refresh, UpToDate>] // the Viewer asks for fresh data
  struct Operate { }
  [L____________<Operate, (Document, UpToDate)>] struct Refresh { } // fresh data, or "nothing changed"
}
```
- Login cycle, same shape: state `One`: both sides send plain packs, Right logs peer in via `[____________R<Live, Login>]`; `Live`: same plain packs, `[____________R<One, Logout>]` leads back.

## §12.9 Grafting a flow from another actor
KW: graft, external state, copy, CommonFlows, Original, Grafted from, State name collision detected, reusable FSM block
- DECL: target may be state of another actor, even another connection → agent **grafts**: (1) walks state graph from initial state; (2) branch targeting state outside actor → copy state with branches, redirect branch to copy; (3) walk continues through copy → all reachable from it copied; stops at `End`, `Close`. → reusable block (teardown, error flow) written once, grafted anywhere:
```csharp
interface Link : Connects<Client, Server>
{
  // a teardown to graft into other actors
  interface CommonFlows : Actor
  {
    [L____________<Closing, Goodbye>] struct Leaving { }
    [____________R<End, GoodbyeAck>] struct Closing { }
  }
  interface Job : Actor
  {
    int Actor.MaxActiveInstances => 8;
    [L____________<Running, JobStart>] struct Start { }
    [l____________<JobChunk>]
    [____________r<JobDone>]
    [L____________<CommonFlows.Leaving, JobCancel>] // Leaving and Closing are copied into Job
    struct Running { }
  }
}
```
- After grafting `Job` has `Start`, `Running`, `Leaving`, `Closing`.
- DECL: any C# name resolving to state: relative `CommonFlows.Leaving` or fully qualified `com.demo.actors.Demo.Shared.CommonFlows.Leaving` (other connection).
- RULE: each external state copied once per actor, however many branches target it.
- GEN: copy keeps original's number; not renumbered in target.
- PITFALL: branch letters keep position: in copy `L` = first host of connection grafted into → block from connection with hosts in other order changes sides.
- NOTE: source block = full actor (`CommonFlows` is also actor of `Link`, own class, checked like any). Call → Reply → End shape → no `MaxActiveInstances`; longer block needs one.
- RULE: names unique: grafted state named like local one stops agent; report = full state map, each `(Original)` or `(Grafted from X, original line: N)`, collisions flagged `[!]`, ends "State name collision detected. Reusable FSM blocks must be grafted into Actors where their state names do not conflict with existing ones." Rename in target actor or block.

## §12.10 What the generator produces for actors and states
KW: State.<Name>.ONE, State.<Name>.uid, State.O, Transmitter, send_<Pack>, OnReceiveD, OnReceived, OnSerializeD, OnSerialized, OnActivate, CLOSE_, END_, OnNewState, DISCARDED_BYSTATE, Receiving unexpected id, REJECTED, INVALID_ID, Unknown pack id, OnReceiving Error code
- GEN: per actor a class (`Actor0`, `Job`, ...) under namespace of connection + grouping interfaces. Its `State` holds nested class per state (grafted copies included) + built-in `CLOSE_`, `END_`:
| Member | What |
|:--|:--|
| `State.<Name>.ONE` | state's single instance (states hold no data) |
| `State.<Name>.uid` | state number; grafted copy has original's → two states of one actor may share it |
| `State.O` | initial state's instance |
| `State.<Name>.Transmitter`, via `State.<Name>.ONE.transmitter` | one send method per pack this host may send in state: `send(pack, conn)` (pack with fields), `send_<Pack>(conn)` (empty pack); Java value pack `send_<Pack>(value, conn)`. Named actor: every send method takes actor instance as LAST param |
| `State.<Name>.OnReceiveD.<Pack>.handlers` (C#) / `State.<Name>.OnReceived.<Pack>.handlers` (Java) | called when pack received in state |
| `State.<Name>.OnSerializeD.<Pack>.handlers` (C#) / `State.<Name>.OnSerialized.<Pack>.handlers` (Java) | called when pack sent from state |
| `OnActivate` | injection point on state entry; also sets state's `[TransmitTimeout]` and stage (`[ReceiveTimeout]`) on connection |
| `CLOSE_` | on entry starts last transmission: queued packs sent, connection closes |
| `END_` | on entry releases actor instance (Actor0: back to initial state) |
- GEN: handler classes named by pack path with underscores (`AdHocProtocol_Server_Invitation`). Handler params: pack (none for empty pack), connection, actor instance, transmitter of state actor is in AFTER this pack (transitional → target's; stay → current). No transmitter after pack ending in `End`/`Close`, nor when host may send nothing in next state → handler answers only with packs next state allows.
- GEN (Dart): no nested types → states = top-level classes (`Room_State_Start`, `Room_State_Start_Transmitter`); actor class gives C# names as static members: `Link.Room.State.O.transmitter.send_RoomJoin( pack, conn, actor )`, `Room.State.Start.ONE`, `Room.State.Start.OnReceiveD.RoomJoin.handlers.subscribe( f )`; function group `Notify.Messages.send_Note( note, conn )`; RPC replier `ping.OnReceiveD.Ping.handlers`; `Connection.OnEvent.handlers`. Connection file re-exports its actors: one import of `Link/Connection.dart` as `Link` reaches `Link.Room`. Only type names stay flat (`Room_State_Inside_Transmitter`).
- RT: transitional pack sent or received → (1) state changes (`OnNewState`), full actor also increments epoch; (2) your handlers; (3) new state's `OnActivate`. Both hosts make same move on same pack → replicas in step, no extra message.
- RT: state guards packs: before send current state checks id; disallowed → refused, reported to transmitter's error handler as `DISCARDED_BYSTATE` ("Sending unexpected id ... at State ..."). Receive: unexpected id → receiver's error handler ("Receiving unexpected id ... at State ...").
- RT (receive, C#): id this host receives on the connection, but the current state of its actor does not take from the peer → injection point `OnReceiving Error code`, receiver error handler `ERROR` "Receiving unexpected id:N at State:S", then `REJECTED`; pack dropped, no handler runs, connection stays open. Id this host never receives on the connection (e.g. a pack only this side's branches send) → `INVALID_ID` "Unknown pack id:N", connection aborted.
- Agent side of AdHoc `Communication` (→§12.12), C#:
```csharp
// Server.Invitation (an empty pack) in VersionMatching moves Actor0 to Login: the handler gets Login's transmitter
Actor0.State.VersionMatching.OnReceiveD.AdHocProtocol_Server_Invitation.handlers += (conn, actor, transmitter) =>
{
    PersonalVolatileUUID(out var hi, out var lo); // a helper of the Agent, not generated
    transmitter.send(new AdHocProtocol.Agent_.Login { uuid_hi = hi, uuid_lo = lo }, conn);
};
// Server.Progress is a stay branch of Project: the state does not change. The Agent sends nothing in Project,
// so the handler gets no transmitter
Actor0.State.Project.OnReceiveD.AdHocProtocol_Server_Progress.handlers += (pack, conn, actor) => TaskProgress.show(pack);
// the first pack is sent from the initial state
Actor0.State.O.transmitter.send(new AdHocProtocol.Agent_.Version { uid = (ushort)VER }, connection);
```
- Server side, Java, answers login from `LoginResponse`'s transmitter:
```java
Actor0.State.Login.OnReceived.AdHocProtocol_Agent_Login.handlers.add(
        (pack, conn, actor, transmitter) -> transmitter.send_AdHocProtocol_Server_Invitation(conn)); // State.LoginResponse.Transmitter
```
- NOTE: instance creation, limits, pooling, handler threads →§13.

## §12.11 Worked state machine examples
KW: baton pass, SecureHandshake, TelemetryStream, CollaborativeEdit, GameProject, MaxActiveInstances spelling, @GameProject
- DECL: full actors → declare `MaxActiveInstances` (→§13); both spellings accepted: `int Actor.MaxActiveInstances => N;`, `int MaxActiveInstances => N;`.
- Baton pass, leadership alternates:
```csharp
interface SecureHandshake : Actor
{
  int Actor.MaxActiveInstances => 1;
  [L____________<AwaitingChallenge, ClientHello>] struct Initializing { }
  [____________R<Verifying, (AuthChallenge, UpgradeRequest)>] struct AwaitingChallenge { }
  [L____________<Finalizing, ChallengeResponse>] struct Verifying { }
  [____________R<End, (AccessOk, AccessDenied)>] struct Finalizing { }
}
```
- One side streams (Client = Follower, sends freely), other governs (Server alone moves state — because written so, not because it is server):
```csharp
interface TelemetryStream : Actor
{
  int Actor.MaxActiveInstances => 4;
  [l____________<(SensorData, GpsCoords)>] // the Client streams, the state stays
  [____________R<Paused, PauseCmd>]        // the Server pauses the stream
  [____________R<Close, Terminate>]        // the Server ends the whole connection
  struct Active { }
  [____________R<Active, Resume>] struct Paused { }
}
```
- Shared work, one finalizer (both edit, only Client finalizes, Server decides):
```csharp
interface CollaborativeEdit : Actor
{
  int Actor.MaxActiveInstances => 5;
  [_____lr_____<(TextInsert, TextDelete, CursorMove)>] // both sides edit
  [L____________<Reviewing, FinalizeDoc>]              // only the Client finalizes
  struct Editing { }
  [____________R<Editing, (Approved, NeedsChanges)>] struct Reviewing { }
}
```
- Whole project in one line: `@` on project → both sides send every transmittable project pack; state never changes = one-state function group:
```csharp
public interface GameProject
{
  class UserPoint    { float X; float Y; float Z; }
  class PlayerAction { int ActionId; }
  class ServerUpdate { int Health; }
  // ... more packs, in the project, in hosts, nested in packs
  /// <see cref="InCS"/>
  struct PlayerClient : Host { }
  /// <see cref="InJAVA"/>
  struct GameServer : Host { }
  interface GameplayCommunication : Connects<PlayerClient, GameServer>
  {
    interface MainActor : Actor { [_____lr_____<@GameProject>] struct Playing { } }
  }
}
```
- `@GameProject` = packs anywhere in project; `<GameProject>` = only packs outside hosts.

## §12.12 A real state machine: AdHoc's Communication connection
KW: Communication, AdhocProtocol.cs, Agent, Server, Info_Result, Version, Invitation, InvitationUpdate, Progress, Result
- AdHocAgent ↔ generator server via `Communication` of AdHoc's description `AdhocProtocol.cs`; all Actor0 (states in connection body). Shown without the `/*…*/` number marks agent writes into live file:
```csharp
interface Communication : Connects<Agent, Server>
{
  // a Named Pack Set: the two final answers of the Server
  interface Info_Result : _<(Server.Info, Server.Result)> { }
  [TransmitTimeout(12)]
  [L____________<VersionMatching, Agent.Version>] // the Agent opens with its version
  struct Start { }
  [TransmitTimeout(1)]
  [____________R<Login, Server.Invitation>] // version accepted
  [____________R<Close, Server.Info>]       // version refused, with the reason
  struct VersionMatching { }
  [L____________<LoginResponse, Agent.Login>] struct Login { }
  [TransmitTimeout(12)]
  [____________R<TodoJobRequest, (Server.Invitation, Server.InvitationUpdate)>] // accepted, maybe with a new UUID
  [____________R<Close, Server.Info>]                                          // refused
  struct LoginResponse { }
  [TransmitTimeout(2)]
  [L____________<Project, Agent.Project>] // the job: the description to generate from
  struct TodoJobRequest { }
  [ReceiveTimeout(120)]
  [____________R<Close, Info_Result>] // the result, or why there is none
  [____________r<Server.Progress>]    // what the Server is doing, any number of times
  struct Project { }
}
```
| State | Leads | Packs | Next |
|:--|:--|:--|:--|
| `Start` | Agent | `Agent.Version` | `VersionMatching` |
| `VersionMatching` | Server | `Server.Invitation` / `Server.Info` | `Login` / `Close` |
| `Login` | Agent | `Agent.Login` | `LoginResponse` |
| `LoginResponse` | Server | `Server.Invitation` or `Server.InvitationUpdate` / `Server.Info` | `TodoJobRequest` / `Close` |
| `TodoJobRequest` | Agent | `Agent.Project` | `Project` |
| `Project` | Server | `Server.Progress` (stay, repeated) / `Server.Info` or `Server.Result` | `Project` / `Close` |
- Shows: leadership alternates per state; `VersionMatching`, `LoginResponse` have two exits; Named Pack Set `Info_Result` as `PACKS`; `Project` = stay branch (`Progress`) + terminal transition; connection-wide timeouts on Actor0 states (`Login` has none).
- Agent handlers →§12.10; what Agent does around exchange →§24.

## §12.13 Actor, state and branch rules at a glance
KW: rules, checklist, error, warning, silently ignored, exit code 22, exit code 123
| Rule | Checked |
|:--|:--|
| physical connection has ≥1 actor with ≥1 state | error |
| actor not inside another actor; name not reserved | error (exit 123 for reserved) |
| state = empty struct, ≥1 branch attribute, in connection/actor; every struct there is a state | error (exit 22) |
| initial state = first declared | - |
| Actor0 exists only if connection body declares states | - |
| actor = chain (all reachable from first) or function group (stay only), never both | error |
| named chain actor declares `MaxActiveInstances` unless Call → Reply → End (→§13) | error |
| routing attributes on structs only; no interface derives from `L____________` / `____________R` | C# error / error |
| state names unique per actor, grafted included | error + state map |
| transitional branches of one side only per state | error |
| target = state (this actor or grafted), `End`, `Close` | error |
| pack once per side per state (chain) / per actor (function group); Stay/Stay duplicates count | error: FSM AMBIGUITY |
| `PACKS` = packs, hosts, projects, Named Pack Sets, filter templates, `_<>`, `X<>`; no connection | error |
| filter patterns valid regex | error |
| actor has ≥1 pack to send | error |
| project/host without `@` adding nothing while nested scopes hold packs | warning |
| filter beside `PACKS` without `@` scope | **silently ignored** |
| `X<...>` before packs it should remove, or aimed at `@` packs | **silently removes nothing** |
| `Close`/`End` inside `PACKS` | ignored |
- NOTE: routing attributes take no part in attribute replacement. Imported connection gets actors and Actor0 states from a connection modifier; an existing state is not modified: its branches stay as its project wrote them (→§22.3.6). Full attribute catalog incl. routing →§23.

# §13 Actor kinds and the actor at runtime
KW: actor kinds, Actor0, function-group actor, RPC actor, stateful actor, MaxActiveInstances, Acquire, Release, lifecycle, injection points, [Grouped], Broadcaster
SUMMARY: actor kinds a description produces, instance counts, RPC methods → actors, `MaxActiveInstances` concurrency modes, instance lifecycle/timers, where user code lives, threading, cross-connection groups, one pack to many connections. Actor/state/branch syntax, epoch →§12.

## §13.1 Four kinds of actor
KW: Actor0, function-group actor, RPC actor, stateful actor, identity, address, epoch 0, Acquire, pool
- Every actor belongs to one connection. Kind depends only on state-machine shape (chain linked by transitional branches vs states without transitions →§12) and `MaxActiveInstances`.

| Kind | From | Instances | Identity (address on wire) | `MaxActiveInstances` | Ends |
|:--|:--|:--|:--|:--|:--|
| Actor0 | states declared directly in connection body | one per connection, each side | fixed | always 1 | with connection |
| Function-group | actor whose states have only non-transitional branches | none per conversation: one shared object per host, outside connection, serving every connection | fixed: actor's type | ignored, warning | never |
| RPC | RPC method, or actor shaped exactly `Call → Reply → End` declaring no `MaxActiveInstances` | caller: one registered instance per call; replier: one pooled context per request | dynamic: caller instance's address | unlimited, implied | when reply has gone |
| Stateful | states form chain of transitional branches | one pair (instance per host) per conversation, up to limit | dynamic: type + instance | required | at `End`/`Close`, by `[Deadline]`/`[Idle]`, or with connection |
- Multicast (stateful) = instance pairs + one fixed group address fanning a cast to all members →§13.4.2.
- RT: identity decides cost. Type-bound (function group): nothing created/tracked per conversation; every declared pack sendable whenever connection open, from either host the branch allows. Callback-bound (RPC): request carries caller instance's address; replier answers there, registers nothing. Type+instance-bound (stateful): both instances share one dynamic address; packs travel only between these two; both run replica of same state machine = single source of truth; epoch guards stale packs →§12.
- RT: opening side takes instance via generated `Acquire`; first pack carries new address + epoch 0; peer finds no instance under address → takes one from actor's pool, registers it; later packs go there. Pack for address peer no longer knows with epoch ≠ 0 = conversation already ended there → dropped.
- NOTE: choose: related one-way notifications/status updates driving no workflow → function group; one request/one answer → RPC method; multistep exchange (handshake, file transfer, collaborative edit) → stateful. Actor0 = connection's own machine →§12; Actor0 whose states are all fire-and-forget is itself a function-group actor.

## §13.2 Function-group actor
KW: function-group actor, Singleton Actor, fire-and-forget, l____________, ____________r, _____lr_____, non-transitional
- `function-group actor` = actor whose states use only non-transitional branches (`l____________`, `____________r`, `_____lr_____`). Never changes state → each state = named group of one-way (fire-and-forget) functions, always available on both hosts; no side leads a state, both equal.
- NOTE: agent messages say "Singleton Actor"; "single-instance" means `MaxActiveInstances => 1`, different thing.

### §13.2.1 Declaring a function-group actor
KW: NoArg, overloads, pack id, tag, KeepDoc, SkipDoc, KeepName, SkipName
- DECL: C# overload group (`void LogEvent(); void LogEvent(StringMessage msg); ...`; in connection body a void method = error) → one state whose branch lists every argument pack; receiver tells overloads apart by pack id →§12. Several groups = several states of one actor:
```csharp
// struct Client : Host (InCS), struct Server : Host (InJAVA)
class NoArg         { }
class StringMessage { string text; }
class WarningEvent  { int code; string text; }
class ErrorEvent    { int code; string text; }
class StatusPayload { byte level; }
class PartialStatus { byte level; string part; }
interface ClientServer : Connects<Client, Server>
{
    interface MyFunctions : Actor
    {
        [l____________<(NoArg, StringMessage, WarningEvent, ErrorEvent)>]   // Client calls LogEvent, no arg or one of three packs
        struct LogEvent { }
        [l____________<(StatusPayload, PartialStatus)>]                      // full or partial status
        struct UpdateStatus { }
    }
}
```
- RULE: no void argument; every branch carries concrete pack. No-argument function → one empty pack per project `class NoArg { }`, listed with state's packs; if state's packs selected by doc tags, tag `NoArg` too in packs table →§20.
- DECL: select by tag: packs tagged `log_events` (in `///` or packs table) → `[l____________("log_events")]`. Optional `(KeepDoc, SkipDoc, KeepName, SkipName)` and other set forms →§9.
```csharp
interface Notify : Actor        // Server pushes notes/alerts, Client sends flags
{
    [____________r<(Note, Alert)>]
    struct Messages { }
    [l____________<Flag>]
    struct Flags { }
}
```

### §13.2.2 What the generator produces for a function-group actor
KW: one, send, OnReceiveD, handlers, Release, epoch 0
- GEN: one static shared object (`one` in C#) used by every connection of host; id = actor's type, epoch always 0, `Release` does nothing. Per state: static `send` overload per pack this host sends + handler list per received pack. C#, Client:
```csharp
Notify.Flags.send(flag, conn);   // any time: nothing to acquire/release
Notify.Messages.OnReceiveD.Note.handlers += (note, conn, actor) => Show(note);
```

### §13.2.3 Rules for function-group actors
KW: MaxActiveInstances ignored, warning, fire-and-forget, unique pack ids
| Rule | Agent does |
|:--|:--|
| No transitional branch in actor | function group, host-wide global object; no `MaxActiveInstances` needed |
| `MaxActiveInstances` declared | ignored, warning *"contains only fire-and-forget functions but assigned MaxActiveInstances value. MaxActiveInstances is ignored"*; multicast `+N` cleared. Actor0: no warning |
| Pack sent by one side in two of its states | error: all states active at once → pack ids unique per side across actor →§12 |
| Non-transitional states mixed with chain | error: chain or function group →§12 |
- PITFALL: `int MaxActiveInstances => UNLIMITED;` on it compiles, only yields the warning; remove.

## §13.3 RPC methods
KW: RPC method, Call, Reply, End, L____________, ____________R, return tuple, marker interfaces
- DECL: `RPC method` = C# method in connection body or plain grouping interface inside it; structured virtual connection too →§18. Agent → actor with machine `Call → Reply → End`. Return type = tuple(direction marker, reply packs); single parameter = request:
```csharp
(L____________, UserProfile) GetUser(UserId id);   // Left calls with UserId, Right replies UserProfile
```
- NOTE: in return tuple `L____________`/`____________R` = marker interfaces of `org.unirail.Meta`, distinct from same-named branch attributes; used only there.

### §13.3.1 Unidirectional request-response (RPC)
KW: marker, caller, replier, Pack Set request, alternatives, X<…>, filter template
- Marker in return tuple names host that SENDS REQUEST; other host replies. `L____________` → Left calls, Right replies; `____________R` → Right calls, Left replies; none → either →§13.3.3.
```csharp
// Client (InCS, InTS), Server (InJAVA, InCS)
class NoArg { }  class Ping { long stamp; }  class Pong { long stamp; }  class Status { byte level; }
class FileName { string name; }  class FileId { long id; }  class FileData { Binary[] data; }  class NotFound { string what; }
class Query { int id; }  class Found { int id; string value; }  class Nope { int id; }
interface Link : Connects<Client, Server>
{
    (L____________, Pong) ping(Ping req);                                    // Client asks
    (____________R, Status) status(NoArg _);                                 // Server asks, no argument
    (L____________, FileData, NotFound) FetchFile((FileName, FileId) query); // exactly one request and one reply pack sent
    (Found, Nope) lookup(Query q);                                           // either side: actors lookupL, lookupR
}
```
- GEN: `ping` ≡
```csharp
interface ping : Actor          // no MaxActiveInstances: this exact shape is made unlimited
{
    [L____________<Reply, Ping>]   // caller leads Call
    struct Call { }
    [____________R<End, Pong>]      // replier leads Reply
    struct Reply { }
}
```
| Synthesized | Value |
|:--|:--|
| Actor name | method name; `<name>L`/`<name>R` without marker |
| State `Call`, number 0 | one transitional branch led by caller; packs = request; → `Reply` |
| State `Reply`, number 1 | one transitional branch led by replier; packs = reply; → `End` |
| `MaxActiveInstances` | unlimited |
| Placement | actor of enclosing connection/grouping interface, next to Actor0, not inside it |
| Persistent number | agent writes `/*…*/` comment after method name →§20 |
- DECL: request and reply = Pack Sets: parameter type and each return element after marker accept all a branch's `PACKS` accepts (pack, named Pack Set, `Project`/`Host`/`Pack` scope ± `@`, `_<…>`, `X<…>`, filter template, tuple) →§9. Several request variants = tuple parameter; several reply variants = several tuple elements; elements are ALTERNATIVES (one pack sent), not value-tuple fields.
- RULE: return elements form ONE set → `X<…>` in one element removes packs named by others too. Packs/sets may be declared anywhere in project; resolved after all packs read.
```csharp
interface CPURequest : _<(ForPeriod, ForRange)> { }   // named Pack Set
interface TelemetryLink : Connects<Dashboard, Server>
{
    (L____________, CpuList)                         getCPU(CPURequest req);
    (L____________, _<@Replies>, X<Replies.Legacy>)  ask(RecentOnly<@Requests> req);  // scope minus one pack; filter template RecentOnly<SCOPE>
}
```

### §13.3.2 What the generator produces for an RPC method
KW: Acquire, acquire, ping.State.Call.ONE.transmitter.send, OnReceiveD, OnSerializeD, ctx.send, ctx.Release, replier context, not registered
- GEN/RT caller: full actor class. `Acquire(conn)`: instance from pool, registered on connection, in `Call`; request via `Call` transmitter → `Reply`; reply arrives, handlers run, transition to `End` returns instance to pool. Instance = callback context: reply checked against state, reaches asking logic.
- GEN/RT replier: small context class. Per request connection takes context from pool, stamps caller's address + epoch; NOT registered (no limit, not tracked). Handler gets `(request, connection, context)`; answer through context now or later from another handler; return it via `Release(connection)` once reply out.
- RT: warm pool → no actor allocation per call on either side.
```csharp
// Client - caller. Once at start-up:
ping.State.Reply.OnReceiveD.Pong.handlers += (pong, conn, actor) => Show(pong);
// Each call: never null - RPC actor has no limit.
var actor = ping.Acquire(conn);
ping.State.Call.ONE.transmitter.send(request, conn, actor);   // Call -> Reply; reply moves it to End and back to pool
// Server - replier: answer, give context back once Pong serialized.
ping.OnReceiveD.Ping.handlers   += (req, conn, ctx) => ctx.send(MakePong(req), conn);
ping.OnSerializeD.Pong.handlers += (_, conn, ctx) => ctx.Release(conn);
```
- GEN: handler classes named after pack; nested pack → path joined by `_` (`OnReceiveD.Gateway_Download` for `Download` of host `Gateway`). Java: same structure, `OnReceived.<Pack>.handlers.add(...)`; TypeScript: `acquire(conn)`.
- PITFALL: generated code never returns replier context to pool; your code does, in reply's `OnSerializeD` handler.

### §13.3.3 Bidirectional request-response
KW: bidirectional, <name>L, <name>R, no marker, bare return type
- DECL: no marker → TWO actors `<name>L` (Left calls), `<name>R` (Right calls), each own `Call`/`Reply`; simultaneous calls never collide.
```csharp
(Found, Nope) lookup(Query q);              // lookupL, lookupR
PlayerPosition Ping(PlayerPosition echo);   // bare return type: also bidirectional, one reply kind
```
- NOTE: two numbers, one line of numbers table →§20.

### §13.3.4 Cascading RPC (multi-hop relay)
KW: cascade, relay, multi-hop, Gateway, independent RPC, getSessionFiles
- Host without direct connection to data host → call cascaded via middle host (`Dashboard <-> Gateway <-> Server`). Each hop = independent RPC, own actors and lifetime; middle receives next hop's reply, sends it as own reply. Same signature on both connections:
```csharp
// Server InJAVA, Dashboard InTS
///<see cref = 'InCS'/>
struct Gateway : Host
{
    public class Download { long session_id; }
    public class Upload   { Binary[] data; }
}
interface ServerToGateway : Connects<Server, Gateway>
{
    (____________R, Gateway.Upload) getSessionFiles(Gateway.Download req);   // Gateway asks Server
}
interface GatewayToDashboard : Connects<Gateway, Dashboard>
{
    (____________R, Gateway.Upload) getSessionFiles(Gateway.Download req);   // Dashboard asks Gateway
}
```
- RT: 1) Dashboard calls: caller instance on Dashboard, replier context on Gateway. 2) Gateway's request handler starts NEW `getSessionFiles` to Server (second caller instance on Gateway, context on Server); keeps Dashboard-side context + its connection in a field of Server-side caller instance. 3) Server replies `Upload`, releases context. 4) Gateway's Server-side instance reaches `End` → pool; its reply handler sends files via kept Dashboard-side context, which then returns to pool. 5) Dashboard's instance reaches `End`; code has files.
- NOTE: Gateway holds two objects during relay; you write relay in its two handlers; creation, addressing, cleanup per hop generated.

### §13.3.5 Parser rules for RPC methods
KW: RPC parser errors, void, NoArg, one parameter, l____________ in return tuple, no reply pack
| Declaration | Result |
|:--|:--|
| In connection body / grouping interface inside it | RPC actor |
| Outside connection (host, project interface) | ignored, nothing generated |
| Inside an `Actor` | error |
| `void` return | error: fire-and-forget needs function-group actor →§13.2 |
| `l____________`/`____________r` in return tuple | error: only `L____________`/`____________R` |
| Return type only marker (`L____________ Get(Req r);`) or tuple with marker and no reply pack | error: ≥1 reply pack, in a tuple |
| No parameter | error: `class NoArg { }`, `Name(NoArg _)` |
| >1 parameter | error: one tuple parameter `Name((A, B) req)` |
| Request or reply resolves to no transmittable pack (filter keeps nothing, `X<…>` removes all) | error |
```csharp
// valid (live description)
(L____________, TeamCoordination)          RequestTeamCoordination(PlayerPosition query);
(____________R, ServerStatus)              GetServerStatus(NoArg _);
PlayerPosition                             Ping(PlayerPosition echo);
(L____________, ServerStatus, PlayerEvent) ComplexQuery((PlayerAction, TeamCoordination) query);
(PlayerPosition, PlayerAction)             Ping2(PlayerPosition echo);
```

### §13.3.6 When the RPC shorthand is not enough
KW: limit concurrent calls, hand-written Call Reply, multicast off, LongJob, MaxActiveInstances
- RULE: shorthand always unlimited; to bound, write in full:
```csharp
interface FetchFile : Actor
{
    int Actor.MaxActiveInstances => 14;
    [L____________<Reply, (FileName, FileId)>]
    struct Call { }
    [____________R<End, (FileData, NotFound)>]
    struct Reply { }
}
```
- RULE: hand-written actor of same shape (exactly two states, one branch each, sides alternating, first → second state, second → `End` or `Close`) = RPC actor: no `MaxActiveInstances` needed, unlimited.
- RULE: `+N` on `Call → Reply → End` shape → warning, multicast off (RPC lives one call).
- RULE: any other machine (job accepted, progress, result) = stateful, must declare `MaxActiveInstances`:
```csharp
interface LongJob : Actor
{
    int Actor.MaxActiveInstances => UNLIMITED;
    [L____________<Reply, JobRequest>]
    struct Call { }
    [____________R<Running, JobAccepted>]
    [____________R<End, JobRejected>]
    struct Reply { }
    [____________r<JobProgress>]      // Server reports, state stays
    [____________R<End, JobResult>]   // Server ends job
    struct Running { }
}
```

## §13.4 Full-featured actor
KW: org.unirail.Meta.Actor, MaxActiveInstances, Actor.MaxActiveInstances, must explicitly define, => 1
- DECL: interface in connection scope extending `org.unirail.Meta.Actor`, states written out →§12. Concurrency = `MaxActiveInstances`; agent takes property whose name ends in `MaxActiveInstances`, reads constant: `int MaxActiveInstances => 8;` or `int Actor.MaxActiveInstances => 8;`.

| Actor | Without `MaxActiveInstances` | With |
|:--|:--|:--|
| Actor0 | 1 | always 1 |
| Function-group | nothing needed | ignored, warning |
| RPC method | unlimited | impossible: write actor in full |
| Hand-written `Call → Reply → End`/`Close` | unlimited | as written |
| Other stateful | error *"must explicitly define 'MaxActiveInstances': how many of its conversations may run at once on one connection"* | as written |
- PITFALL: `Meta.cs` `Actor` declares property `=> 1`; agent ignores it as default, reads only actor's own declaration.

### §13.4.1 Concurrency modes
KW: single-instance, swarm, unlimited swarm, multicast, UNLIMITED, +N, unary plus, resource-exhaustion
| Mode | Declaration | Identity | Behavior |
|:--|:--|:--|:--|
| Single-instance | `=> 1;` | one per connection: stable destination | while one conversation open, next `Acquire` refused |
| Swarm | `=> 8;` (any N) | dynamic address per instance | ≤N concurrent per connection; checked |
| Unlimited swarm | `=> UNLIMITED;` | dynamic per instance | no limit, no check |
| Multicast | `=> +4;` (unary plus) | dynamic per instance + one fixed group address | ≤N; pack cast to group reaches all |
```csharp
// Client InCS, Server InJAVA. Packs: Pause{} Resume{} JobStart{int job; string what;} JobChunk{int job; Binary[] data;}
// JobCancel{int job;} JobDone{int job; bool ok;} Blob{Binary[] data;} BlobAck{int size;} RoomJoin{string who;}
// RoomSaid{string who; string text;} RoomLeave{string who;}
interface Link : Connects<Client, Server>
{
    interface Control : Actor { int Actor.MaxActiveInstances => 1;      // one control conversation per connection
        [L____________<Paused, Pause>]    struct Running { }
        [L____________<Running, Resume>]  struct Paused { } }
    interface Job : Actor { int Actor.MaxActiveInstances => 8;          // up to 8 jobs, a pair per job
        [L____________<Running, JobStart>] struct Start { }
        [Deadline(30)]                                                   // each job's own: eight jobs, eight deadlines
        [l____________<JobChunk>] [____________r<JobDone>] [L____________<End, JobCancel>]
        struct Running { } }
    [Idle(15)]                                                           // upload whose peer quiet 15 s ends itself
    interface Upload : Actor { int Actor.MaxActiveInstances => UNLIMITED;
        [L____________<Waiting, Blob>]    struct Start { }
        [____________R<End, BlobAck>]     struct Waiting { } }
    interface Room : Actor { int Actor.MaxActiveInstances => +4;        // up to 4 members, one address for all
        [L____________<Inside, RoomJoin>] struct Start { }
        [____________r<RoomSaid>] [L____________<End, RoomLeave>]
        struct Inside { } }
}
```
- GEN: value → generated property `MaxActiveInstances`, known both sides. `UNLIMITED` = constant 0 of `Meta.Actor`. RPC actors show largest `int` in generated code (never reached).
- PITFALL: unlimited swarm saves limit-check work but lets peer open unbounded instances: only private, controlled, trusted environments (agent's error text warns of resource-exhaustion attacks).
- RULE: `+1` → warning: one-member multicast = unicast.

### §13.4.2 The group address
KW: multicast, Multicast(connection), multicastFactory, cast, join, Group injection point
- RT: multicast fixed address = group with address on wire; same kind as `[Grouped]` group →§13.9; one per connection, kept among connection's actors.
- GEN: `Room.Multicast( connection )` returns connection's group, created on first call by actor's `multicastFactory`.
- RT: instance joins group when it opens (by this side's `Acquire` or by peer), leaves at end in `Release`.
- GEN/RT: `cast( pack, connection )` on state's transmitter sends pack ONCE to group address; peer's group hands it to each member, whatever their state/epoch.
- RT: one group at a time; instance that `join`s another group leaves the address, casts stop reaching it.
- GEN: shared member data → injection point `Group` of actor's group class; own group: extend class, set `multicastFactory`.

## §13.5 Nesting actors
KW: grouping interface, namespace, package, nested actors, doc comment lost
- DECL: actors and RPC methods may sit in plain grouping interfaces in connection scope; GEN: each = namespace (C#) / package (Java) level.
```csharp
// Client InCS, Server InJAVA. Packs: ForPeriod{long since; long until;} CpuList{float[] load;} MemList{long[] used;} Subscribe{int what;}
// Sample{int what; long value;} Unsubscribe{int what;} Restart{string reason;} Done{} Failed{string why;}
interface Link : Connects<Client, Server>
{
    interface Telemetry
    {
        (L____________, CpuList) getCpu(ForPeriod req);
        (L____________, MemList) getMemory(ForPeriod req);
        [Idle(120)]   // subscriber that stopped renewing is let go
        interface Feed : Actor
        {
            int Actor.MaxActiveInstances => 16;
            [L____________<Live, Subscribe>]
            struct Start { }
            [____________r<Sample>]
            [l____________<Subscribe>]
            [L____________<End, Unsubscribe>]
            struct Live { }
        }
    }
    interface Admin { (L____________, Done, Failed) restart(Restart req); }
}
```
- GEN: C# `…Link.Telemetry.getCpu`, `…Link.Telemetry.Feed`, `…Link.Admin.restart`. Grouping interface generates no class → its doc comment does not reach generated code →§4.
- RULE: actor or RPC method inside an actor = error.

## §13.6 Actor lifecycle
KW: lifecycle, Acquire, Release, End, Close, OnActivate, OnDeadline, OnClose, park, pool
- RT: instance from pool opened by this side's `Acquire` (null at limit) or by peer's first pack (peer over limit → connection aborted) → start state → states, with `OnActivate` and per-pack `OnReceived`/`OnSerialized`. Exits: `End` (`END OnActivate`); `Close` (`CLOSE OnActivate`, drain queue, close connection); `Deadline`/`Idle` (`OnDeadline`, default releases); lost connection (socket error, vanished peer, timeout) → session parked if Resumable applies (actor hears nothing, keeps state); otherwise, or when wait runs out, close event runs state's `OnClose`, instance ends. All exits except parking meet in `Release`.

### §13.6.1 Opening an actor instance and its limit
KW: MaxActiveInstances limit, Acquire null, undefined, connection aborted, UNLIMITED, Actor0 Acquire
- RT: both sides know the limit, each checks where instances are registered.

| Event | Result |
|:--|:--|
| This side's `Acquire` would exceed limit | that instance refused: `Acquire` returns `null` (`undefined` in TS), nothing sent; connection, other instances go on |
| Peer opens one more than limit | peer broken/hostile: connection aborted |
| `UNLIMITED` | no count, no check |
| RPC actor | never refused: caller limit = largest `int`; replier context not registered |
| Actor0 | `Acquire(conn)` returns connection's own Actor0, lives as long as connection |

### §13.6.2 How an actor instance ends
KW: End, Close, Deadline, Idle, ReceiveTimeout, TransmitTimeout, Resumable, tunnel, Release
| Cause | Scope | Details |
|:--|:--|:--|
| → `End` | pair | pair deleted; connection stays for other actors →§12 |
| → `Close` | connection | transmit queue drained, connection closes →§12 |
| `[Deadline]`/`[Idle]` ran out | instance | `OnDeadline`; default ends pair on this side →§13.6.3 |
| `[ReceiveTimeout]`/`[TransmitTimeout]` ran out | connection | connection timeouts: closes with all actors →§11 |
| Connection lost/broken | connection | `[Resumable(seconds)]` on current state or connection → session parked; else instance ends with connection →§14 |
| Virtual connection's tunnel closes | tunnel | its instances end →§18 |
- RT: every end → `Release`: leave group if any, removed from connection's actor table, reset to start state, back to pool.

### §13.6.3 Per-instance timers: [Deadline] and [Idle]
KW: [Deadline(seconds)], [Idle(seconds)], lease, OnDeadline, non-leading side, received packs only, timer cost
- DECL: `[Deadline(seconds)]` on STATE: max stay of one instance in it; set on entry, dropped on leave; per instance (8 jobs = 8 deadlines); connection timeouts untouched.
- DECL: `[Idle(seconds)]` on ACTOR or STATE = lease: instance ends when none of ITS packs came from peer that long. Actor: every state; state: while in it.
- RULE: `[Idle]` holds on side NOT leading the state (no transitional branch there) — it cannot end pair itself, would keep vanished peer's instance forever. Leading side ends pair at will → not applied. State nobody leads → both sides. `Upload` (§13.4.1): Server leads `Waiting` → `[Idle(15)]` holds on Client: no `BlobAck` in 15 s → Client ends instance.
- RT: only RECEIVED packs push deadline later; sends don't (pushing events to gone subscriber doesn't keep it alive; renewing one lives).
```csharp
interface ViewerToServer : VirtuallyConnects<Viewer, Server, Hub>   // tunnel via Hub →§18
{
    [Idle(120)]   // Viewer renews lease with WatchSub
    [Grouped]
    interface Watch : Actor
    {
        int Actor.MaxActiveInstances => 4;
        [L____________<Live, WatchSub>]
        struct Start { }
        [____________r<WatchEvent>]          // Server's events do not renew
        [l____________<WatchSub>]            // Viewer's renewals do
        [L____________<End, WatchUnsub>]
        struct Live { }
    }
}
```
- RT: expiry → `OnDeadline` = injection point; generated default releases instance on this side; for Actor0 (pair = connection) closes connection. Peer ends own instance by own deadline or with connection.
- RT cost: one field per instance, two with `[Idle]`. Connection keeps only nearest of its instances' deadlines in transport's existing deadline list: no timer object, nothing per instance in loop. Push later (each pack an `[Idle]` instance receives) = add + compare; list touched only when deadline moves earlier; on firing only that connection's instances examined.
- RT: Java fires on connection's own loop. C# fires in connection maintenance with I/O paused → never during one of instance's own packs (C# WebSocket client transport: one timer per connection). TypeScript: one timer per connection. Virtual-connection instance keeps deadlines on carrier connection →§18.
- GEN: `[Deadline]`, `[Idle]`, `[Grouped]` also emitted as constants of state/actor →§23.

| Attribute | On | Limits | Ends |
|:--|:--|:--|:--|
| `[ReceiveTimeout(s)]` | connection | link: peer silent on socket | whole connection |
| `[ReceiveTimeout(s)]`, `[TransmitTimeout(s)]` | state (Actor0's states) | stage: no pack of peer in state / stalled sending | whole connection |
| `[Deadline(s)]` | state | one instance's stay in state | that instance |
| `[Idle(s)]` | actor or state | one instance without pack from peer, non-leading side | that instance |

### §13.6.4 Overriding the default lifecycle actions
KW: Check limit, END OnActivate, CLOSE OnActivate, Actor OnDeadline, injection point
- GEN: each default sits next to an injection point: refuse at limit (`Check limit`), release at `END` (`END OnActivate`), close at `CLOSE` (`CLOSE OnActivate`); deadline default sits INSIDE `Actor OnDeadline` → replaceable with own error handling/recovery. Everything beyond explicit limits and terminal states = your code's responsibility.

## §13.7 Your code lives in the actor
KW: injection points, Actor, Import, OnReceiving, OnReceived, OnSerializing, OnSerialized, OnActivate, OnClose, OnNewState, Epoch mismatch, Check limit, Connection Release
- NOTE: generated actor OWNS its part of conversation: your refs/counters/buffers/flags → its fields; behavior → injection points (survive regeneration →§25).

| Injection point | Runs | For |
|:--|:--|:--|
| `Actor` (class body) | - | instance fields/methods; actor's pool declared here |
| `Import` | - | `using`/`import` lines |
| `Actor OnReceiving`, `Actor OnReceived`, `Actor OnSerializing`, `Actor OnSerialized` | every pack of actor, before its state sees it, on that direction's loop | checks/bookkeeping for all actor's packs |
| `<Pack> OnReceived Event`, `<Pack> OnSerialized Event` | pack of this state arrived/went out | what pack means for instance |
| `OnReceiving code`, `OnReceived code`, `OnSerializing code`, `OnSerialized code` | every pack of this state, before per-pack event | checks for one state's packs |
| `OnReceiving Error code` | pack state doesn't take arrived, just before error report | diagnostics |
| `OnActivate code` | instance entered state | what entering starts |
| `OnClose`, `OnTimeout Receiver`, `OnTimeout Transmitter` | close event arrived = session ended (parked session withholds it until wait runs out) / connection timeout fired in this state | what instance gives back/reports |
| `Actor OnNewState` | before every state change, incl. reset to start | cross-state bookkeeping |
| `Epoch mismatch` | stale pack for instance dropped | diagnostics |
| `Actor OnConnected`, `Actor OnEvent` | connection connected / any connection event | per-instance reaction |
| `Actor OnDeadline` | `[Deadline]`/`[Idle]` ran out | own action instead of default release |
| `END OnActivate` | pair deleted, just before return to pool | hand-over before reuse |
| `CLOSE OnActivate` | actor moved to `Close`, before drain and close | last report |
| `Group` (`[Grouped]`, multicast) | - | what group members share |
| `Connection code` | - | whole-connection state |
| `Check limit` (connection) | limited actor's instance about to be counted against `MaxActiveInstances`, under connection lock; for own `Acquire` and peer-opened | own admission rule |
| `Connection Release` | connection object returns to pool; generated where pooled, e.g. multiplexed port →§19 | clear connection fields |
- PITFALL: handler args are the context (actor + connection). Side table `Map<actor, state>` duplicates instance, costs lookup + lock per pack, may outlive instance; runtime already tracks actors and notifies each of close/timeout.

### §13.7.1 Pooled instances: reset and hand over
KW: pool reuse, reset fields, END OnActivate, Connection Release, hand over
- RT: actor instances, replier contexts, pooled connection objects reused.
- RULE: reset what you add: right after `Acquire`, or in `END OnActivate`/`Connection Release`. Peer-side instance taken by connection, not your code → `END OnActivate` covers both sides. Allocate buffers once per instance, not per conversation.
- RULE: hand over before `END`: afterwards instance serves another conversation. Longer-lived result (verdict, file) handed in `END OnActivate` to object outliving conversation (task, session), which drops its actor reference there.

## §13.8 The two loops: threads and actor handlers
KW: receive loop, transmit loop, single-threaded, run to completion, lock, epoch, do not block
- RT: one single-threaded event loop per direction. Receive: inbound packs, receive timeouts, `OnReceiving`/`OnReceived`. Transmit: outbound packs, transmit timeouts, `OnSerializing`/`OnSerialized`.
- RT run to completion: one event at a time per direction, dispatched to actors' current states; next only after return. Actors on a connection advanced in turn by same thread → same-side handlers never race, no locks for owned state.
- RT: the two loops = two threads, may run concurrently. Field touched by both receive and transmit handlers = the one place inside connection to coordinate; one-side fields need no lock.
- RT: current state published under connection lock together with epoch → transition seen whole on other side; value read at an instant (e.g. parked-session wait at loss) = consistent snapshot of actors' states.
- RT: outside code (other connection's loop, workers) = one well-defined hand-off, not shared mutable state. Send from any thread only enqueues; send from transmit-side handler queued by transmitting thread itself.
- PITFALL: never block in handler: stalls whole direction; in C#/Java one transport loop may serve many connections → all wait. Offload long work, send result back as next event.

## §13.9 Actors of many connections: [Grouped]
KW: [Grouped], Connection.Group, Watch.Group, join, leave, Join, Leave, FastReentrantSpinLock, Lock, head, size, Connection.Groups
- DECL: `[Grouped]` on actor interface → host gathers instances from all connections/tunnels into groups without side table (feed subscribers, list watchers). Example: `Watch` §13.6.3.
- GEN: own group class `Watch.Group` extends runtime `Connection.Group`; shared data/methods → injection point `Group` (survives regeneration). Custom: extend `Watch.Group`, pass object to `join`; members see it typed as `group`.
- RT: members linked through instances (actor extends runtime member class with `prev`, `next`, `group`). Join → head; leave → unlink in place; O(1), no walk, no allocation, no GC. `group` taken only while empty, by CAS under lock of joined group → at most one group regardless of threads.
- RT: group = own lock, runtime `FastReentrantSpinLock` (C# field `Lock`): few instructions uncontended, short spin contended; reentrant → member may leave mid-walk of own group. TypeScript single-threaded, walks without lock.

| Instance member | Meaning |
|:--|:--|
| `connection` | its connection, set on join |
| `join( connection, group )` | joins, leaves previous; `false` = already in it, or concurrent join won |
| `leave()` | leaves group |
| `group` | typed `Watch.Group` or subclass |
| `prev`, `next` | neighbor members |

| Group member | Meaning |
|:--|:--|
| `head`, `size` | first member, count |
| `prev`, `next`, `list` | neighbor groups in its list, the list |
| `join( groups )`, `leave()` | join/leave a list of groups |
- GEN: C# spells `Join`/`Leave`.
- RT: `Release` removes member on any end (state machine, `[Deadline]`/`[Idle]`, connection/tunnel close) → group never holds gone instance; no sweep/lease code.
```java
static final Watch.Group WATCHERS = new Watch.Group();
Watch.State.Start.OnReceived.WatchSub.handlers.add( ( pack, conn, actor, transmitter ) -> actor.join( conn, WATCHERS ) );
// event for all - from anywhere, or from a member: group is its own lock
WATCHERS.acquire();
try { for( Watch w = WATCHERS.head; w != null; w = w.next ) Watch.State.Live.ONE.transmitter.send( event, w.connection, w ); }
finally { WATCHERS.release(); }
```
- GEN: C# walk: `WATCHERS.Lock.Acquire()` / `WATCHERS.Lock.Release()`.
- RT: `Connection.Groups` lists groups likewise (telemetry feeds, chat rooms): `group.join( groups )` O(1), one list at a time; walk `for( g = groups.head; g != null; g = g.next )` under list's own lock. Extend `Groups` with shared data.
- RT: walk holds lock only while queuing (send only enqueues).

## §13.10 One pack to many connections
KW: fan-out, broadcast, no copy per recipient, BytesCopy, Broadcaster, reference count
| | Fan-out of typed pack | Broadcast of received pack |
|:--|:--|:--|
| Shared | one pack object | received bytes, captured once |
| Serialized | once per connection, from object | never again: each connection copies capture |
| Kept alive by | your code | reference count; capture back to pool after last send |
| Use | events host makes | relay storing what it receives and passing on |
- Neither copies pack per recipient.

### §13.10.1 Fan-out of a typed pack: one object, many queues
KW: send reference, sending queue, OnSerialized, shared object, Stream, File, Value Pack
- RT: `send` enqueues a REFERENCE, no serialization →§1.3.1. Same object on N connections = in N queues; each transmitter writes it into own send buffer when socket has room, own cursor; read N times, never copied. §13.9 walk is exactly this.
- PITFALL: no count kept for typed pack: don't change/reuse/pool it until every connection reached its `OnSerialized`; simplest: new object per event.
- PITFALL: abstract pack whose getters read stateful source (DB cursor) not shareable: first connection consumes it.
- PITFALL: pack with `Stream`/`File` field not shareable: source read once.
- NOTE: Value Pack →§7.9 has no object; value copied into each queue slot → loop of sends, free.

### §13.10.2 Broadcast of a received pack: the bytes, captured once
KW: Connection.Receiver.BytesCopy, BytesCopy.InMemory, Broadcastable, Broadcaster<DST>, Subscribe, Unsubscribe, PrepareBroadcastIfAnySubscriber, Broadcast, Broadcasting, sending_put, InitPool, Connection.Transmitter.BytesCopy
- RT: relay host (monitoring backend, market-data hub, log broker) captures incoming pack's bytes while decoding, same pass, hands one capture to all subscribers. C# and Java:

| Class | Role |
|:--|:--|
| `Connection.Receiver.BytesCopy` | capture of received pack body; `BytesCopy.InMemory` = pooled impl |
| `Connection.Transmitter.Broadcastable` | what a broadcaster sends; `BytesCopy` is one |
| `Connection.Transmitter.Broadcaster<DST>` | one feed's subscribers: `Subscribe`, `Unsubscribe`, `PrepareBroadcastIfAnySubscriber`, `Broadcast` |
| `Connection.Transmitter.Broadcaster` (generated per connection) | broadcaster of that connection's transmitters; unsubscribes connection on close |
- NOTE: Go: same names (`Receiver.BytesCopy` + `BytesCopy_InMemory`, `Transmitter.Broadcastable`, `Broadcaster[DST]`). Rust, C++, C: capture into `Capture` (Rust `connection::receiver::bytes_copy::Capture`, C `adhoc·connection·receiver·bytes_copy·Capture`), fan out with `Broadcaster` of `Subscriber`s (`connection::transmitter::broadcaster`).
```csharp
// 1. Feed subscribers, on the connection to observers (AdHoc's monitoring backend).
public class Feed : ToObservers.Connection.Transmitter.Broadcaster
{
    public static readonly Feed ONE = new();
    // one capture queued on one subscriber as a pack the actor RealTimeData sends
    protected override bool Broadcasting(AdHoc.Connection.Transmitter.Broadcastable src, ToObservers.Connection.Transmitter dst)
        => dst.sending_put(RealTimeData.one, src, -1, 0);
}
// observers' Subscribe/Unsubscribe OnReceived: Feed.ONE.Subscribe(conn.transmitter); Feed.ONE.Unsubscribe(conn.transmitter);
// 2. Arm capture in the pack's factory, when a pack starts to arrive from a source.
_Allocator.DEFAULT.new_Monitoring_Sessions_Authorizer_CheckAttempt = receiver =>
{
    Feed.ONE.PrepareBroadcastIfAnySubscriber(receiver, CheckAttempt.__id_);   // captures only while someone is subscribed
    return pool.Acquire();                                                     // your object: setters fill a DB command
};
// 3. Pack arrived: store, then broadcast.
public void __OnReceived_ServerToMonitoring__Actor0__One(ToServer.Connection conn, ToServer.Actor0 actor, ToServer.Actor0.State.One.Transmitter transmitter_)
{
    try     { insert.ExecuteNonQuery(); }
    catch( Exception e ) { Console.WriteLine(e.Message); }  // never let an exception escape into the receive loop
    finally { Feed.ONE.Broadcast(conn.receiver); pool.Release(this); }   // Broadcast also clears the capture slot
}
```
- RT: 1) receiver reads pack id, calls factory; `PrepareBroadcastIfAnySubscriber` attaches pooled `BytesCopy.InMemory` ONLY if feed has ≥1 subscriber (else nothing captured/spent). 2) generated decoder consumes fields (here into setters writing DB params); each consumed receive-buffer slice also copied into capture, starting after transport headers: one pass. 3) your `OnReceived` stores, calls `Broadcast(receiver)`. 4) `Broadcast` takes capture off receiver, takes one reference per subscriber in single atomic step, queues on each via your `Broadcasting`; each transmitter copies bytes into own send buffer, own cursor, releases reference after last byte; after last subscriber capture returns to pool.
- RT cost: one body copy per broadcast, one contiguous array held until slowest accepting subscriber sent it (only place runtime keeps pack contiguous →§1.3.3). `BytesCopy.InMemory.InitPool(5)` at start-up → pool of 32; array grows by doubling, keeps largest size.
- RT: never blocks: subscriber with full sending queue refuses capture, misses event, stays subscribed; nobody waits. Fan-out leg best-effort; make leg INTO host guaranteed if loss matters →§14.
- RT: store then forward: slow store delays broadcast and next receives on source connection, never subscribers' transmitters.
- PITFALL: always call `Broadcast` (in `finally`): it clears receiver's capture slot; skipped → capture lost to pool.
- RULE: same pack, same body encoding on both legs (bytes forwarded as-is; per-leg headers fine). Captured packs bypass sender's state checks and `OnSerialized`: description must allow pack in sending state. Don't capture packs with `Stream` fields or transform chains; Value Pack → loop of sends.
- NOTE: transmit-side tap `Connection.Transmitter.BytesCopy` copies bytes a transmitter writes.

## §13.11 Parser checks for actor kinds
KW: parser checks, actor in actor, MaxActiveInstances missing, +1 warning, ReceiveTimeout warning
| Check | Result |
|:--|:--|
| Actor in actor; RPC method in actor | error |
| Stateful actor without `MaxActiveInstances`, not `Call → Reply → End` shape | error |
| Linked + isolated states in one actor | error →§12 |
| `MaxActiveInstances` on function group | warning; ignored, multicast cleared |
| `+1` | warning: unicast |
| `+N` on `Call → Reply → End` | warning; multicast off |
| `[ReceiveTimeout]`/`[TransmitTimeout]` on non-Actor0 state | warning: whole-connection timeouts →§11 |
| RPC methods | →§13.3.5 |
- Related: §12 states/epoch; §11 connection events/timeouts; §14 parked sessions; §18 tunnels; §25 injection points.

# §14 Sessions, guaranteed delivery, UDP
KW: session, [Resumable(seconds)], Resumable<HOST, PACKS>, [UDP(seconds)], parked, journal, reclaim, identify, resume
SUMMARY: session = live instance of a connection; how it outlives its socket (parking, identity, reclaim, resume), guaranteed once-in-order delivery via sender journal, `[UDP]` transport on same machinery.

## §14.1 What a session is
KW: session, Internal, External, close event, [Resumable], Resumable<HOST, PACKS>, [UDP], [UDP<EXCLUDE_PACKS_SET>]
- `session` = `Internal` half (generated connection object) + actors + their states + what your code attached (→§11); transport (TCP socket, WebSocket, UDP connection) = `External` half, only carries it.
- DEFAULT: both die together: socket lost → session ends, actors get close event; reconnecting peer starts new session in first state.

| Declaration | Where | Gives (each builds on previous) |
|:--|:--|:--|
| `[Resumable(seconds)]` | Connection or state | lost socket PARKS session ≤ `seconds`; peer returning in time and proving identity continues it: same actors, same states |
| `Resumable<HOST, PACKS>` | interface in Connection body | guaranteed delivery: `PACKS` sent by `HOST` arrive once, in order, across lost socket; sender journals, replays what receiver missed |
| `[UDP(seconds)]` | Connection | over UDP; every pack guaranteed both ways (as if each host declared `Resumable<HOST, PACKS>` over all it sends); `[UDP<EXCLUDE_PACKS_SET>(seconds)]` leaves named packs unguarded |
- RT example: actor in `Working`, packs 1–5 on socket 1, lost with 4, 5 in flight; parked; on socket 2 peer logs in, `reclaim(session_id)`; same actor continues in same state; 4, 5 replayed from journal before 6, 7.

## §14.2 Keeping a session across a lost connection
KW: [Resumable(seconds)], parked, wait
- `[Resumable(seconds)]` = seconds a session survives socket loss; while waiting it is `parked`: actors and states, no socket.

### §14.2.1 Declaring the wait on a connection and on its states
KW: [Resumable], [Resumable(0)], no-argument form, override, Connection value, too short
- DECL: `[Resumable(seconds)]` or `[Resumable]`; on Connection (interface) or state (struct).
- RULE: Connection → wait for all states. State → wait while actor in it; OVERRIDES Connection value up or down; `[Resumable(0)]` = no parking there; `[Resumable]` = keep Connection value. No attribute → Connection value. DEFAULT `0` = session ends with socket.
- RULE: no-arg form belongs on states; give Connection seconds. Connection declaring a guaranteed leg with bare `[Resumable]` → agent stops "[Resumable (-1)] on the connection C ... is too short ...".
```csharp
// Client, Server: InCS + InJAVA
struct Client : Host{
    public class Login{ public long resume_token; /* 0 on first login, else session id Server gave last time */ public ulong account; }
    public class Upload{ public ulong offset; [D(65_536)] Binary[,] data; /* up to 64 KiB */ }
    public class Logout{ }
}
struct Server : Host{
    public class Welcome{ public long session_id; /* Client keeps it for next Login */ }
    public class Denied{ }
    public class Progress{ public ulong offset; }
}
[Resumable(30 * 60)]                              // default for every state: half an hour
interface Communication : Connects<Client, Server>{
    [Resumable(0)]                                // nothing to keep before login
    [L____________<Authorizing, Client.Login>]
    struct Start{ }
    [Resumable(0)]
    [____________R<Working, Server.Welcome>]
    [____________R<Close, Server.Denied>]
    struct Authorizing{ }
    [Resumable(60 * 60 * 24)]                     // working session waits a day
    [l____________<Client.Upload>]
    [____________r<(Server.Progress, Server.Welcome)>] // Welcome here too: after a reclaim, this state answers the new login
    [L____________<Start, Client.Logout>]         // after logout the wait is Start's again: 0
    struct Working{ }
}
```
- Opposite default: no Connection attribute, few states kept. Swarm excerpt (`CommonFlows.Leaving` = teardown flow grafted from elsewhere →§12):
```csharp
interface Job : Actor{
    int Actor.MaxActiveInstances => 8;
    [TransmitTimeout(5)]
    [L____________<Running, JobStart>]
    struct Start{ }
    [ReceiveTimeout(20)]
    [Resumable(5 * 60)]                               // connection has no [Resumable]: only a running job kept, 5 min
    [Deadline(30)]
    [l____________<JobChunk>]
    [____________r<JobDone>]
    [L____________<CommonFlows.Leaving, JobCancel>]
    struct Running{ }
}
```
- NOTE: pick one default, mark exceptions (Connection value + `[Resumable(0)]` on worthless states, or none + `[Resumable(seconds)]` on few).

### §14.2.2 Which value applies: the state or the Connection
KW: wait resolution, state overrides, larger of two, unacknowledged journal, imported connection
| Declared | Wait in the state |
|:--|:--|
| `[Resumable(m)]` on Connection only | `m` |
| `[Resumable(s)]` on state | `s`, larger or smaller than Connection's |
| `[Resumable(0)]` on state | `0`: loss ends session |
| `[Resumable]` on state | Connection value (`0` if none) |
| nothing | `0` |
- RULE: while a `Resumable<HOST, PACKS>` journal (→§14.5) on connection holds unacknowledged packs, wait = LARGER of state and Connection value; only then, not in general.
- RULE: written in project declaring the Connection; importing project may give Connection another value by connection modifier (its attributes replace Connection's whole attribute set →§22.3.6). Imported state unmodifiable → keeps author's waits.

### §14.2.3 What the generator produces for `[Resumable]`
KW: resume_, ResumableSeconds, resumable_seconds, OnActivate, injection region ResumableSeconds, IsTimeout, -1
- GEN: agent passes constant `resume_` on Connection/state; no-arg form = `-1` ("use Connection's seconds") →§23. Generated: one connection field written by each state on becoming current + one method runtime asks at loss. C#, `Communication`:
```csharp
public partial class Connection : AdHoc.Connection.Internal
{
    public const uint resume_ = 1800U;            // [Resumable(30 * 60)] on the Connection
    internal volatile int ResumableSeconds = 1800; //current state's parked-session seconds; each state's OnActivate overwrites it
    public int resumable_seconds(AdHoc.Connection.External connection, Network.TCP.ExternalConnection.Event Event)
    {
#region> ResumableSeconds
#endregion> ÿÿ.ResumableSeconds
        return ResumableSeconds;
    }
    …
}
// state Working of the connection's actor:
public void OnActivate(Connection conn, Actor0 actor, State prevState, …)
{
#region> OnActivate code
#endregion> ÿÿĀ.OnActivate
    …
    conn.ResumableSeconds = 86400;                // [Resumable(60 * 60 * 24)] of this state
}
```
- GEN: `resumable_seconds(connection, event)` gets same connection/event transport passes to `OnExternalEvent`. Region `ResumableSeconds` (survives regeneration →§25): vary wait by cause or return `0` to refuse parking:
```csharp
#region> ResumableSeconds
        if (logged_out) return 0;                 // a field of yours: nothing to keep after a logout
        if (Event.IsTimeout()) return 5 * 60;     // silent peer 5 minutes, broken socket the state's value
#endregion> ÿÿ.ResumableSeconds
```
- GEN: no `[Resumable]` anywhere → no method; runtime default returns `0`; never parked.
- PITFALL: one wait per connection, overwritten by every state becoming current (connection's own actor and all other actors alike). Several instances (swarm) → wait at loss = what state entered LAST wrote. Policy over all instances (park while any job runs) → compute in `ResumableSeconds` region.

## §14.3 What happens to a session when its connection is lost
KW: loss, park, close event withheld, journal, in-flight packs, REMOTE_CLOSE_ABRUPTLY
- RT: at loss: not parked → ends now (close event to actors, `OnClose` runs, journal dropped, slot freed). Parked → back in time (accepting side: login + `reclaim`; dialing side: reconnect of same client) → live on socket 2 with `OnOpen` and `resume(connection)`, or `resume(null)` if sender lost journal; time out → ended. Graceful Close/Abort by own code → never parked.

### §14.3.1 Loss versus intent
KW: lost, broken, socket error, timeout answered 0, OnReceiveTimeout, OnTransmitTimeout, undispatchable pack id, lost(), graceful close, Abort
- RT: `lost or broken` = end not from own code's intent: socket error; timeout (`OnReceiveTimeout`/`OnTransmitTimeout` answered `0` →§11); peer closing (gracefully or not); receiver meeting undispatchable pack id (stream out of step → dropped like broken link); code calling `lost()` on connection's `External`.
- RT: graceful close or Abort by own code = intent → session ends at once regardless of seconds.

### §14.3.2 Parking a session
KW: park, identity 0, resumable_seconds, close event withheld, OnClose, OnOpen
- RT: 1) peer never identified (identity `0` →§14.4) → ends now, seconds not asked. 2) else `resumable_seconds(connection, event)`; `0` → ends now, close event to connection and actors. 3) other → PARKED until now + seconds; actors, states, app state untouched.
- RT parked: actors hear nothing; close event WITHHELD (session not known finished). Generated connection's `OnClose()` DOES run (its connection is over); `OnOpen()` runs again on new connection. `OnOpen`/`OnClose` bracket each connection ridden; close EVENT once per session.

### §14.3.3 What a parked session holds
KW: slot, parked census, census, censusText, receiver closed, transmitter kept, journal on disk, buffers to pool
- RT holds: its slot (not counted among socket-holding connections a server checks on accept; host census counts it `parked`, C#: `census`, `censusText()` on TCP host); generated connection with actors, states, attached objects (receiver closed at park; transmitter closed too unless side journals — journaling side keeps transmitter with sending queue and pack in production as loss left it); with guaranteed delivery the journal + receiver's position, journal in disk files while parked.
- RT: no socket, no transport buffers (back to pools), nor transform-chain stages of closed receiver/transmitter →§16.
- PITFALL: parked session not free; resources return only on peer return or timeout. Seconds = what reconnecting peer needs; `[Resumable(0)]` on states with nothing worth keeping.
- NOTE: rebuildable state needs no parking: describe it as packs, store with same generated serialization (file/DB bytes = wire bytes, serialization not tied to socket), rebuild on return (minute or month later). Park only the unrebuildable: half-delivered guaranteed stream, live transaction.

### §14.3.4 Packs in flight at the moment of loss
KW: half-received pack, half-sent pack, sending queue discarded, sending while parked, Stream, File, conduit, onFailure
- RT receiving: half-received guaranteed pack kept unfinished with session (position counts its bytes), continues from cut byte after reconnect; other half-received packs discarded.
- RT sending guaranteed: half-sent pack is in journal; continues mid-pack from position receiver names.
- RT sending others: non-guaranteed pack being sent dropped with socket. Side without journal: queued packs discarded too, reported to transmitter's error handler. Journaling side: queue survives park.
- RT sending while parked: journaling side may keep sending guaranteed packs; sending thread journals at once; leaves after reconnect (up to cap). Non-guaranteed sent while parked dropped.
- RT conduits: guaranteed `Stream`/`File` pack continues from cut byte: receiver keeps pack + sink until reconnect; sender replays from journal (bytes as first sent) → source NOT re-read. Non-guaranteed conduit lost: receiver discards partial, stream ends torn down →§15.
- RT sender lost journal: bytes after cut gone → receiver drops unfinished guaranteed pack, reports to error handler (`onFailure`: guaranteed pack with its id cut by loss of connection, dropped), continues from position sender names →§14.5.4.

### §14.3.5 When a parked session ends
KW: expiry, REMOTE_CLOSE_ABRUPTLY, journal deleted, slot returns
- RT: peer back in time naming session → continues →§14.4. Time out first → ended: withheld close event `REMOTE_CLOSE_ABRUPTLY` to connection and actors (actor exits →§13.6), codecs closed, journal deleted, slot to pool; later peer starts new session.

## §14.4 Identity and reclaim
KW: identify, reclaim, id, session(), parked, lost(), External, ext_connection, session id
- RT: identity = ordinary FSM handshake: fresh socket anonymous; every connection (first or reconnect) starts machine at beginning, runs your login. Identity (token, account, device) defined by your protocol. After recognizing peer, handler calls `External` (`conn.ext_connection`):

| `External` member | Does |
|:--|:--|
| `long identify(int id)` | sets identity, raises `resume`; claims no parked session; returns opaque session id. Repeat with same identity → no second `resume` |
| `long reclaim(long session_id)` | parked session under id exists → MOVES onto this connection, fresh session discarded. No match (too late, never parked, id names this connection) → exactly `identify` of id's identity. Returns new session id |
| `long reclaim(long session_id, int id)` | `reclaim` of token from peer. Token no secret: slot + identity, guessable. `id` = identity login of this connection proved (key, UUID, whatever trusted); session moves only when token names it. Other token → connection keeps own session, identified as `id`. One-argument form: token server keeps itself. Rust, C, Go, Dart: `reclaim_id` (`ReclaimId`); TS: `External.reclaim( connection, session_id, id )` |
| `int id` | identity; `0` = none |
| `long session()` | session id; `-1` = none |
| `int parked` | seconds parked session still waits; `0` = time up, not reaped yet; `-1` = no parked session |
| `void lost()` | end as loss: identified session parks |
- WIRE: session id = slot in high 32 bits + identity in low 32 (slot 2, identity 42 → `2 << 32 | 42`). Opaque to code; peer persists it, returns at next login.

### §14.4.1 Continuing on the accepting side: reclaim
KW: reclaim, swap slots, OnClose, OnOpen, encryption key, resume, new session id, Internal, Welcome in Working
- RT: returning peer on NEW socket → new slot, fresh session in first state, fresh login runs. `reclaim(session_id)` with parked session present: sessions swap slots (parked → new socket; fresh → freed slot, slot returns to pool); fresh hears `OnClose()`, arriving hears `OnOpen()`; new connection's encryption key added to arriving session →§16; arriving session identified here, `resume` raised on it; returns new session id for new slot → send to peer (old one dead).
- Example: slot 2 = parked S (id 42, `Working`, journal); slot 4 = fresh F in `Authorizing`; Login token `2 << 32 | 42` → after `reclaim` slot 4 holds S still in `Working`, slot 2 holds F → free pool; returns `4 << 32 | 42`.
```csharp
// Server, in handler of Client.Login; conn = generated connection the pack arrived on
var ext = conn.ext_connection;
long session_id = pack.resume_token != 0
                      ? ext.reclaim(pack.resume_token, account_id) // session parked under this token and this PROVED account moves onto this socket; check pack.account first
                      : ext.identify(account_id);      // first login: identity only (a non-zero int of yours)
var session = (Connection)ext.Internal;                 // after a reclaim: the parked session - no longer conn
// send session_id back in Server.Welcome through session
```
- PITFALL: after successful `reclaim`, `conn` = fresh session that just LEFT the socket; session on socket = transport's `Internal`.
- RT: `reclaim` = single join point where login FSM hands socket to parked work FSM; peer mid-`Working` re-logs in like first-timer, continues in `Working` server-side. No separate protocol: don't add "where was I" packets or own replay.
- RULE: after `reclaim`, parked session (in its parked state) sends what handler sends next → packs completing peer's login must be sendable in states a session can be parked in. Hence `Server.Welcome` in `Working` (§14.2.1): Client waits in `Authorizing`, reclaimed session answers from `Working`.

### §14.4.2 Continuing on the dialing side
KW: dialing side, identify, ConnectAsync, Network.TCP.Client<INT>, reconnect, connect event, Actor0 reset
- RT: session belongs to reused connection object; parks only if identified → dialing side calls `identify` on own connection (identity of its choice, e.g. assigned by accepting side).
- RT: reconnecting same client (C#: `ConnectAsync` on same `Network.TCP.Client<INT>`) continues parked session if in time; else stale session ended first (close event), connect starts new one. Failed reconnect → back to waiting for remaining time.
- RT: identity survives with object; runtime raises `resume` itself when new socket up, unless connect handler identifies with different identity.
- PITFALL: connect event resets connection's own actor to first state (→§13): login reruns, pre-loss actor state gone. Runtime restores what it owns (parked session object, journal, receiver position) → guaranteed stream replays exactly; FSM state reached via NON-guaranteed pack not replayed, login restart doesn't restore it (bridge →§14.4.4).
- NOTE: accepting side: login FSM and work FSM = different objects on different slots; dialing side: one object carries both.

### §14.4.3 When the client dials again by itself
KW: redial, backoff, 1.6, 120 s, ±20 %, 20 s connect, stalled, carried almost nothing, 1 KiB in 10 s, Confirm_ owed, socket taken as lost
- RT: client whose parked session's journal holds guaranteed bytes peer has not confirmed dials again by itself (only dialing side can; peer would wait in vain): first 1 s after loss, each next wait ×1.6, max 120 s, each ±20 % (gRPC connection backoff); a dial may take ≥20 s. Stops when parked session's time runs out or a connect takes session on.
- RT: live socket taken as lost (session parks; such client dials again) when:
| What runtime sees | Where | Why new socket helps |
|:--|:--|:--|
| link's `[ReceiveTimeout]` ran out (`Pulse_` keeps live peer audible, →§11.7.1) | both sides | peer or path gone |
| peer confirmed nothing for 10 s though a 64 KiB journal step went out since its last `Confirm_` | both sides | tunnel/proxy keeps socket open, other end gone, swallows writes |
| link carried <1 KiB both ways in 10 s **while session needed it** | client | path speed fell almost to nothing: degraded path, collapsed congestion window |
- RT: speed judged only while session needs link: socket does not take what this side writes, OR peer owes a confirmation (64 KiB step out since its last), OR a guaranteed pack half received. PITFALL avoided: few small guaranteed packs stay unconfirmed long after arrival (peer confirms once per 64 KiB) → do not count. Moved = bytes socket carried + what peer confirmed; slow link that moves lives.

### §14.4.4 resume: the runtime tells your code that the session continues
KW: resume(connection), resume(null), service packs, control.Continue, Resumable region, journal online
- RT: handshake internal, via generator-added service packs (→§14.5.2), never seen/sent by code. Reaches code only as generated `resume(connection)`, raised once per connection on BOTH ends when peer identified:
  - journaling side: go-ahead; journal handed to new connection; guaranteed packs flow (replay first, then new).
  - receiving side: cue to name consumed position; returns `true` when position sent.
  - `resume(null)` to receiver: sender couldn't continue (journal gone); position jumped past missing bytes, none replayed. Receiver with no position yet (first connection of session) answered same: position set to journal start, hears `resume(null)`, nothing to restore.
```csharp
// generated receiving side, C#
public bool resume(AdHoc.Connection.External connection)
{
    //Runs once, on this `connection`, the moment
    //the peer is identified (identify()/reclaim()) and this session continues on it.
#region> Resumable
#endregion> ÿÿ.Resumable
    if (connection == null)
        return false;
    ext_connection = connection;
    control.Continue(); //the peer is asked to go on from the position of this receiver
    return true;
}
```
- GEN: journal-only side: seats connection, puts journal online, returns `false`. Your code → `Resumable` region.

### §14.4.5 Restoring the state of a receiving side
KW: snapshot FSM state, restore on resume, resume(null) resync, timing of resume
- RULE: where readiness depends on FSM state (typically dialing side = usual receiver), gate restore on `resume`, not own reconnect bookkeeping: 1) unplanned loss → snapshot FSM state, let connection fall back to first state for login. 2) restore ONLY on `resume(connection)` with non-null connection (sole signal of continuing identified session). 3) no `resume` (never identified / not resumable) → clean slate. 4) `resume(null)` → discard snapshot, resync; crashed sender without journal identifies like first-timer, so identity ≠ data survived.
- RT timing: accepting side: inside login handler at `identify`/`reclaim` (after login). Dialing side with kept identity: right after socket connects, before login reruns. `resume` is public → code may raise it itself when ready.
- RT: receiver position survives loss only while parked; unparked session restarts without position.
- PITFALL: position must reach session holding the journal: on accepting side the reclaimed one, not fresh login session. Who says what when = your login design.
- NOTE: app-state snapshot under runtime callbacks, not own resume protocol on wire.

### §14.4.6 Pitfalls of session identity
KW: identity 0, session id not secret, newest session id, identify or reclaim, A Resumable pack before the peer was identified
- PITFALL: identity `0` = not identified → never parked; use non-zero.
- PITFALL: session id not secret (slot + identity); `reclaim` gives parked session to whoever presents it. Authenticate first; check id was issued to that peer.
- PITFALL: keep newest session id; every `reclaim` returns new one.
- PITFALL: token from peer → `reclaim(token, id)` with identity login proved; session moves only when token names it. `identify` of same identity before it harmless: moved session hears `resume`.
- PITFALL: journaling side identifies before sending: guaranteed send throws "A Resumable pack before the peer was identified" while unidentified; journal stage online only in `resume(connection)`.
- PITFALL: receiving side must reach `resume`: sender holds guaranteed packs until position named →§14.5.2.
- PITFALL: non-guaranteed transitions not replayed on dialing side →§14.4.2.

## §14.5 Guaranteed delivery
KW: Resumable<HOST, PACKS>, guaranteed leg, once in order, journal
- Continuation keeps session, doesn't deliver pre-loss packs; `Resumable<HOST, PACKS>` in Connection body does: `PACKS` sent there by `HOST` arrive ONCE, IN ORDER, across lost socket. One declaration = `guaranteed leg` (one sending host, one connection).

### §14.5.1 Declaring a guaranteed leg
KW: Resumable<HOST, PACKS>, resumable_megabytes, default 100, 2048, filter template, VirtuallyConnects
- DECL: interface extending `Resumable<HOST, PACKS>`:
  - `HOST` = Left or Right host of enclosing Connection.
  - `PACKS` = Pack Set or tuple of packs/sets/scopes/exclusions, same as `HeaderFor<>` targets →§9, §8; filter template over host scope with exclusions OK: `Resumable<Server, (ToMonitoring<@Monitoring>, X<@Monitoring.VolatileInfo>)>` (`ToMonitoring<>` = filter template of your description).
  - `int resumable_megabytes => N;` journal cap MB. DEFAULT `100`, min `1`, max `2048` (session positions must stay within signed 32-bit distance). Agent checks only min; larger → runtime throws when connection creates journal.
```csharp
/// Everything a host may send on any of the links, the snapshot Telemetry included.
interface Everything : _<(Tick, Flags, Nop, Note, Bulk, Order, Telemetry, Blob, Frame, FrameZstd, Thumb, Capture, Attachment, Album, Archive, Vault)>{ }
/// The packs worth journalling: everything but the snapshot.
interface Guaranteed : _<(Everything, X<Telemetry>)>{ }
[Resumable(30 * 60)]
interface CtoA : Connects<C, A>{                          // both directions guaranteed, different sets/sizes
    [l____________<(Everything, C.HelloFromC, Relayed)>] // C → A
    [____________r<(Everything, A.HelloFromA)>]          // A → C
    [____________R<Live, Login>]                         // A logs the peer in
    struct One{ }
    [l____________<(Everything, C.HelloFromC, Relayed)>]
    [____________r<(Everything, A.HelloFromA)>]
    [____________R<One, Logout>]
    struct Live{ }
    interface FromC : Resumable<C, (Guaranteed, C.HelloFromC, Relayed)>{ int resumable_megabytes => 100; }
    interface FromA : Resumable<A, (Tick, Note, Order, A.HelloFromA, Capture, Thumb)>{ int resumable_megabytes => 20; }
}
[Resumable(10 * 60)]
interface BtoC : Connects<B, C>{                          // one direction: one leg; C's packs plain
    [l____________<(Everything, B.HelloFromB, Relayed)>]
    [____________r<(Everything, C.HelloFromC)>]
    struct One{ }
    interface FromB : Resumable<B, (Guaranteed, B.HelloFromB, Relayed, X<Blob>)>{ int resumable_megabytes => 50; }   // large pack out
}
```
- NOTE: snapshot `Telemetry` excluded on purpose: re-sent in full after reconnect; replaying stale samples only fills journal.
- DECL: legal inside `VirtuallyConnects` body too →§18.

### §14.5.2 How the journal works
KW: journal, marked pack, 512 KiB, 64 KiB, Confirm_, Continue_, Fresh_, Ok_, Lost_, position, service packs, Resumable.dir, 4 MB files
- RT send: generated code marks each guaranteed pack when queued; journal (stage between generated transmitter and socket) copies bytes of marked packs as they go out, AFTER transform chain; others pass uncopied.
- RT cap: at `resumable_megabytes` sender takes no further pack from queue (guaranteed or not: one queue keeps order) until acknowledged; only service packs go. Live socket: same, sooner, at 512 KiB unacknowledged out; in-flight bytes at loss resent on next socket.
- RT ack: receiver's `position` = bytes of guaranteed packs taken, unfinished included. Each crossed 64 KiB step (checked once per receive call) → `Confirm_`; sender frees journalled bytes below it.
- RT start/continue: receiver names position in `resume`; sender replays from there, then new packs. Until position arrives (first connection and after reconnect) guaranteed packs only journalled, not sent; non-guaranteed flow (login runs).
- PITFALL: receiver must reach `resume` (→§14.4.4), else journal fills; at cap nothing leaves queue, login packs included.
- RT storage: newest bytes in memory buffer (starts small, grows while peer keeps up); backlog → 4 MB disk files under per-process dir below system temp (C#: static `AdHoc.Connection.Transmitter.Resumable.dir`); parked → on disk. Files fully below acked position deleted on each ack; all acked → nothing held. Failed file op ends session. Journal positions: base, acked, sent, next.
- RT: code sees each guaranteed pack once, in order; handshake/positions/journal invisible.
- GEN: over stream (TCP, WebSocket, tunnel) sides talk in `service packs` = generated, undeclared. Project guaranteeing on a stream connection → generator adds five, numbered right after description's packs, with generated serialization/parsing/dispatch; runtime holds only handshake logic.

| Pack | Field | Sent by | Meaning |
|:--|:--|:--|:--|
| `Confirm_` | - | receiver | another 64 KiB step taken: free bytes up to it |
| `Continue_` | position | receiver, once per connection | go on from this position |
| `Fresh_` | - | receiver, once per connection | no position: session new here |
| `Ok_` | - | sender | journal holds named position; bytes after this pack continue from it |
| `Lost_` | position | sender | journal lacks named position (or not answered yet this session); stream continues from this one |
- WIRE: `Confirm_`, `Fresh_`, `Ok_` = pack id alone (1 byte with 1-byte id); `Continue_`, `Lost_` add 3–5-byte position; guaranteed packs carry no extra bytes.
- WIRE: position sent modulo width set by project's largest `resumable_megabytes`: 3 bytes ≤7; 4 bytes ≤1024 (default 100 included); 5 bytes above. Reader takes value nearest own position.
- RT new socket: nothing sent before agreement. Dialing side sends only login. Accepting side, after identifying peer, names own position (`Continue_`/`Fresh_`), waits. Dialing side answers with its position + `Ok_`/`Lost_` for heard one, replays its journal; accepting side answers `Ok_`/`Lost_`, replays own.
- RT: receiver waiting mid-guaranteed-pack reads exactly two service packs (peer's position, answer to own); then after `Ok_` continues from stop byte, after `Lost_` drops it. Sender crash: `Lost_(new position)` → receiver jumps, hears `resume(null)`.
- WIRE: UDP: datagram header carries same info →§14.6.1.

### §14.5.3 What the guarantee costs
KW: cost, journal copy every send, Confirm_ 0.002 %, no service packs, per-pack transform chain
- Journal copy of every guaranteed pack on every send, not only after loss.
- One `Confirm_` per 64 KiB (1 byte with 1-byte id, ≈0.002 %); new socket adds one position each way + `Ok_`/`Lost_`.
- Pack carries nothing for guarantee; connection without `Resumable` carries nothing; project without guaranteed packs on stream connection has none of these five (no pack numbers taken); the one service pack of every project with a TCP/WebSocket connection = `Pulse_` (→§11.7.1).
- Journal holds all unacked bytes, ≤ `resumable_megabytes` per sending host per connection, as long as session lives (parked or not). Guarantee only must-not-lose packs.
- Guaranteeing connection applies transform chain PER PACK, not stream; replayed pack decrypted with key of connection it was written on; pack whose key receiver lost dropped whole →§16.

### §14.5.4 When the sender has lost its journal
KW: Lost_, sender crash, empty journal, resume(null), gap, resync
- RT: guarantee lasts as long as sender's session + journal (parked with session; gone when process dies). Receiver names position sender lacks (typically crash, empty journal) → `Lost_` + position sender can continue from. Same answer from journal not yet answered in its session → earlier session's position never taken for this one. Receiver position jumps past gone bytes (no eternal wait); mid-way guaranteed pack dropped, reported to error handler; code hears `resume(null)` → gap, resync →§14.4.4.
- RT: packs outside `PACKS` continue with session but lost with socket. Session outliving its seconds starts over; actors got close event at expiry.

### §14.5.5 Rules for `Resumable<HOST, PACKS>`
KW: Resumable errors, exit code 77, resumable_megabytes, one per host, dropped from the set
| Rule | Otherwise |
|:--|:--|
| Only in Connection body (`Connects`/`VirtuallyConnects`) | stops: "... is a Resumable, and a Resumable can be declared only inside a Connection body. Move it into the Connection whose leg it guards." |
| `HOST` one of Connection's hosts | stops (exit 77): "Resumable X ... names H as HOST, but C connects L and R. HOST has to be one of them." |
| Connection has `[Resumable(seconds)]` ≥ 1 | stops: "The connection C ... declares a Resumable but carries no [Resumable(seconds)]: say how long a session outlives its socket, at least one second." / "[Resumable(0)] on the connection C ... is too short ..." |
| `resumable_megabytes` ≥ 1 | stops: "X ... sets resumable_megabytes to N; the journal needs at least one megabyte." |
| `resumable_megabytes` ≤ 2048 | unchecked by agent; runtime throws creating journal |
| One per host per Connection (one per direction allowed) | stops (77): "A and B both make H resumable on C. One Resumable per host per connection: merge their pack sets into one declaration." |
| Only packs `HOST` transmits on Connection count | warns, drops: "Resumable X ... lists P, but H does not transmit it on C; the pack is dropped from the set." |
| ≥1 pack left | stops (77): "Resumable X ... is left with no pack: H transmits none of the packs it lists on C." |
| Not in `[UDP]` Connection | stops →§14.6.2 |

### §14.5.6 What the generator produces for a guaranteed leg
KW: header pack, FromB_, FromC_, mb, Host, Conn, HeaderFor, service packs numbering
- GEN: declaration → header pack `<Name>_` (`FromB_`, `FromC_`), NO fields, constants `mb` (journal size), `Host`, `Conn` (sending host and connection numbers); bound like any header (→§8): every leg pack gets artificial header field of that type; zero wire bytes; marks pack guaranteed; invisible to application.
- GEN: sender's connection owns journal (`resumable_megabytes`), routes guaranteed sends via marked path of sending queue, puts journal online in `resume`. Receiver's connection counts guaranteed bytes, reports position itself, names it in `resume`.
- GEN: five service packs (`Confirm_`, `Fresh_`, `Continue_`, `Ok_`, `Lost_`) generated once any stream connection guarantees packs, wire ids 250..254; both sides get serialization/parsing/dispatch + second generated receiver instance reading them while a guaranteed pack waits unfinished. `Pulse_`, when project has it, = 255. RULE: 250..255 same in every project, never given to anything else → new pack never moves them; numbers tables (→§20) don't keep them. Table entry already holding such id → gets new id with warning.

## §14.6 UDP connections
KW: [UDP(seconds)], UDP transport, datagram
- DECL: `[UDP(seconds)]` on Connection → carried over UDP.

### §14.6.1 Every pack guaranteed, both ways
KW: [UDP], [UDP<EXCLUDE_PACKS_SET>], 100 MB journal, Connection_Host_, udp_, datagram header 9 bytes, Network.UDP.Host, idle_timeout, keepalive, mtu, key, window, congestion
- RT: datagrams lost/duplicated/reordered while connection stays up → `[UDP]` guarantees EVERY pack both ways: agent declares `Resumable<HOST, PACKS>` per host over all it sends (two hosts of one type → one host type, one declaration). After each lost datagram: receiver sees gap from datagram numbers, asks to continue, sender replays from journal. Once, in order, across lost connection too.
```csharp
class Chat{ [D(+200)] string text; }
struct Client : Host{   // InCS, InJAVA
    public class Input{ public uint tick; public byte buttons; }
    public class PlayerPosition{ public float x; public float y; }
}
struct Server : Host{   // InCS, InJAVA
    public class Score{ public uint player; public int points; }
    public class Telemetry{ public float load; public uint players; }
}
/// Snapshots: the next one replaces the last, so a lost one is not worth a replay.
interface Volatile : _<(Client.PlayerPosition, Server.Telemetry)>{ }
[UDP<Volatile>(30 * 60)]                          // over UDP; session outlives socket 30 min
interface GameLink : Connects<Client, Server>{    // no Resumable<HOST, PACKS> here: every pack is guaranteed already
    [l____________<(Client.Input, Client.PlayerPosition, Chat)>]
    [____________r<(Server.Score, Server.Telemetry, Chat)>]
    struct Play{ }
}
```
- DECL: no exclusions → `[UDP(30 * 60)]`.
- RULE: `seconds` = Connection's `[Resumable(seconds)]`, ≥1. `[UDP]` without seconds takes `[Resumable(seconds)]`; one required; both → must agree.
- RT: every non-excluded pack journalled, `resumable_megabytes` = 100 per sending host, unchangeable.
- RULE: every guarded pack is guaranteed → §14.4 rules apply: identify before sending, receiver must reach `resume`. Login packs guarded unless excluded: side sending guarded login packs identifies its connection as soon as it opens.
- WIRE: journal signaling in datagram header; UDP sends no service packs.
- GEN: marks = header packs `Connection_Host_` (`GameLink_Client_`, `GameLink_Server_`), no fields, no bytes; agent numbers them above project's topmost pack number; not in numbers tables →§20.
- GEN: boolean constant `udp_` among connection constants → generator wires UDP runtime instead of TCP.
- RT: transform chain (`[Zstd]`, `[ChaCha20]`, ...) on `[UDP]` applied PER PACK (lost datagram can't break compressor/cipher for rest); same for any Connection with `Resumable<HOST, PACKS>`, TCP too →§16.
- WIRE: all runtimes' UDP transports share one design. Datagram header 9 bytes: 1 flags, 4 connection id, 1 series, 3 datagram number. Flags: stream data follows?; acks, continue position, lost-position answer.
- RT: no FIN: silence → `OnReceiveTimeout`; answered `0` → identified session parks as over TCP.
- DEFAULT: UDP host settings (C# `Network.UDP.Host`):

| Setting | Default | Meaning |
|:--|:--|:--|
| `idle_timeout` | 30 000 ms | nothing received this long → asks `OnReceiveTimeout`; initial receive timeout |
| `keepalive` | 15 000 ms | ping after this long without sending: keeps NAT binding and peer's idle timer |
| `mtu` | 1200 bytes | largest datagram payload |
| `key` | none | server's static X25519 secret key; set → every connection encrypted (handshake, then authenticated cipher per datagram ending in 16-byte tag); client pins matching public key →§16 |
| `window` | 256 KiB | unacked journal bytes out per connection until congestion control has a say |
| `congestion` | on | congestion control with pacing; off → as fast as window allows: LAN/dedicated link only |

### §14.6.2 Rules for UDP connections
KW: [UDP] errors, exit code 77, TypeScript, Dart, Multiplex, InTS
| Rule | Otherwise |
|:--|:--|
| `[UDP]` on Connection only, not Actor/state (C# compiles it on struct; agent refuses) | stops: "[UDP] on X ...: the transport is chosen for a whole connection. Move it to C." |
| No `Resumable<HOST, PACKS>` inside | stops (77): "The connection C ... is [UDP]: every pack on it is guaranteed already, both ways. Remove the Resumable X ...; to leave packs out, list them in [UDP<EXCLUDE_PACKS_SET>]." |
| Wait via `[UDP(seconds)]` or `[Resumable(seconds)]`, ≥1, equal if both | stops: "... says [UDP(a)] and [Resumable(b)]: two waits for one session. Keep one of them." / "... is [UDP] without seconds: say how long a session outlives its socket - [UDP(seconds)], at least one second." / "[UDP(0)] on the connection C ... is too short ..." |
| No host generated in TypeScript (browser has no UDP socket): the only runtime without UDP; Dart has it on `dart:io` ( a web build does not compile ) | stops: "The connection C ... is [UDP], and its host H is generated in TypeScript: the TypeScript runtime has no UDP transport ... Take [UDP] off the connection, or InTS off the host." |
| Not listed in `Multiplex<...>` (multiplexed port = TCP; UDP connection has own port →§19) | stops: "The connection C ... is [UDP], and the multiplexer M ... lists it ... Take it out of the multiplexer." |

### §14.6.3 Leaving packs unguarded on UDP
KW: [UDP<EXCLUDE_PACKS_SET>(seconds)], unguarded, no journal, no replay, nothing to exclude
- DECL: `[UDP<EXCLUDE_PACKS_SET>(seconds)]`: listed packs (telemetry, positions, snapshots) travel both ways, any sender, UNGUARDED: no journal copy, no replay. `GameLink`: `Volatile` → `PlayerPosition`, `Telemetry`.
- DECL: `EXCLUDE_PACKS_SET` same form as `PACKS`.
- RT: unguarded pack lost with datagram = lost for good, unnoticed; guarded packs of same datagram replayed, unguarded not (nor after lost connection).
- RT: host whose packs are all excluded keeps no journal.
- RULE: listed pack no host sends → no effect, warning: "[UDP<…>] on C ... lists P to exclude, but no host sends it on this connection: nothing to exclude."
- RULE: `[UDP<EXCLUDE_PACKS_SET>]` without seconds takes Connection's `[Resumable(seconds)]`.
- NOTE: price of UDP guarantee = journal copy per pack. TCP: choose packs that must not be lost; UDP: choose ones that may be.

## §14.7 Session rules at a glance
KW: summary, session rules, Resumable, identify, reclaim, resume, UDP, expiry, cost
| Rule | Detail |
|:--|:--|
| Session | generated connection + actors + states + attachments; socket only carries; no `[Resumable]` → end together →§14.1 |
| Lost/broken | socket error, timeout answered `0`, peer close, undispatchable pack id, `lost()`; parked if identified and `resumable_seconds` > 0 →§14.3 |
| Graceful close, `Abort` | own code: end at once |
| `[Resumable(seconds)]` | Connection = all states; state overrides; `(0)` off; bare (state only) = Connection value; unacked guaranteed leg → larger of two; default `0`; on imported Connection via connection modifier (replaces its attribute set) →§14.2, →§22.3.6 |
| Generated wait | one field per connection, written by every state becoming current (any actor); `resumable_seconds(connection, event)` returns it after `ResumableSeconds` region |
| Parked | keeps slot, connection, actors, states, objects, journal; drops socket, buffers; actors hear nothing; `OnClose` runs; close event withheld |
| Identity | ordinary login, then `identify(id)` (non-zero `int`) or `reclaim(session_id)`; both return session id →§14.4 |
| Dialing side | parks only if identified; reconnect same client continues; connect restarts own actor's FSM; kept identity → `resume` when socket up |
| Join point | after `reclaim` parked session answers; login-completing packs sendable in parkable states; reach it via `Internal` |
| `resume(connection)`/`resume(null)` | once per connection, both ends, on identification: journal go-ahead / receiver names position; `null` = journal gone, jumped, resync |
| Half-sent / half-received | guaranteed (incl. `Stream`/`File`) continue from cut byte, source not re-read; lost journal → dropped, reported; others dropped/discarded |
| `Resumable<HOST, PACKS>` | once-in-order; Connection body only; one per host per Connection; `resumable_megabytes` default 100, 1..2048; needs `[Resumable(seconds)]` ≥ 1 →§14.5 |
| Guaranteed send | throws while unidentified; leaves only after receiver's position (first connection too); at cap queue stops for all packs |
| `identify`/`reclaim` | one per login; `identify` then `reclaim` same identity → no `resume` |
| `[UDP(seconds)]` | all packs both hosts guaranteed, 100 MB journal per sending host; no `Resumable<>` inside; not TS ( Dart: `dart:io` only ); not in `Multiplex<>` →§14.6 |
| `[UDP<EXCLUDE_PACKS_SET>(seconds)]` | set's packs unguarded |
| Expiry | close event `REMOTE_CLOSE_ABRUPTLY`; journal deleted; later peer → new session |
| Cost | parked: slot, objects, journal until return/timeout; guaranteed pack: journal copy per send, one `Confirm_` per 64 KiB |
| Alternative | session state as stored packs, rebuild on return; park only unrebuildable |

# §15 Streams
KW: Stream, File, stream, conduit, raw conduit, named conduit pack, [S(N)], chunked, framing, ABORT
SUMMARY: Field values or whole packs whose bytes cross the connection while produced (not assembled first). Raw conduits `Stream` / `File`, their framing and `[S(N)]` cap, named conduit packs, choosing `Binary` / `File` / `Stream`, streaming typed packs. Compression/encryption →§16; what a middle tier keeps →§17.
- Model: project → host → connection → actor → state → branch → pack → field → wire bytes; runtime: your source → socket buffer → wire → socket buffer → your sink.
- NOTE: "Stream" names three things: here = field type `Stream` (and a pack inheriting it). Trim attribute `[Stream<To, From>]` is unrelated →§17.
- `raw conduit` = field declared with bare type `Stream` or `File`. `named conduit pack` = class inheriting `Stream` or `File`. `conduit` = either.

## §15.1 What streaming adds to the pull-based core
KW: pull-based, socket buffer, transmitter, receiver, terminator, relay, interruptible
- RT: runtime pull-based (→§1): transmitter asks generated code for bytes only when socket buffer has room; receiver hands bytes to generated code as they arrive. Pack never rendered into byte array before send, never assembled from one after receive.
- RT: each direction of a connection = one reusable socket buffer + small parser state, whatever payload size: `Stream` field carrying 1 GB costs same buffer as 64-byte status pack.
- Chunked `Stream` framing adds:
  - total size not needed up front: sender emits chunks, closes with terminator.
  - end visible from framing alone: middle tier forwards/stores/replays chunked stream without decoding a byte (chunk lengths show end). Generated relay of virtual connection built on this →§18.
  - stream may stop early or never stop: source decides end (after known amount, part-way, or hours of live feed with no natural total).

## §15.2 The streaming model at a glance
KW: layers, pack fields, concrete +, abstract -, transform chain, trim, Connects, VirtuallyConnects, relay
- Layers, top to bottom, all over same pull-based core:
  - Pack fields: generated concrete (`+`) or abstract event-driven (`-`), per host, language, pack, field →§10, →§15.8.
  - `Stream` / `File` fields: raw byte conduits between your source/sink and socket buffer, capped by `[S(N)]` (this chapter).
  - Transform chains (`[Zstd]`, `[ChaCha20]`, own stages) wrap serialized bytes of field, pack or whole connection →§16.
  - Trims (`[ToStream<E>]`, `[FromStream<E>]`) cut a chain at one depth for one endpoint so that side holds raw bytes →§17.
  - Connections: chain on physical `Connects<>` wraps everything one hop carries; `VirtuallyConnects<>` + generated relay carry whole conversation through relay hosts that never decode it →§11, →§18.
  - Bottom: single reusable socket buffer per direction.
- Choose by payload:

| Payload | Use | → |
|:--|:--|:--|
| bytes, length known when sent: DB cell, serialized value, token, BLOB on disk | `File` field or named `File` pack | §15.3 |
| exactly 16 bytes (UUID, MD5, IPv6) or 1–8 bytes with meaning | pack of two `ulong` halves; primitive or Value Pack | §15.6 |
| unbounded, live or interruptible raw bytes: encoder feed, capture | `Stream` field or named `Stream` pack | §15.3 |
| typed pack too large to materialize on receiver | abstract (`-`) implementation for that host/language/pack/field | §15.8 |
| typed pack a middle tier must store/replay/forward without parsing | `[ToStream<E>]` / `[FromStream<E>]` / `[Stream<To, From>]` trim | §17 |
| large text a middle tier stores/serves as is: log, document | same trim on `string` field (UTF-8 on cut leg) | §17 |
| any of above, compressed and/or encrypted | transform chain `[Zstd]`, `[ChaCha20]`, custom stages | §16 |
| everything one connection carries, compressed/encrypted | same chain declared on connection | §16 |
| whole conversation crossing one or more relay hosts | `VirtuallyConnects<L, R, PATH>` + generated relay | §18 |

- NOTE: layers compose: `Stream` field can carry a chain; chain can be cut for one endpoint by trim; physical connection can carry chain over whole link; tunnel is itself a chunked stream carrying chains end to end.

## §15.3 Raw conduits - `Stream` and `File`
KW: Stream, File, org.unirail.Meta, source, sink, [S(N)], framing
- DECL: field of type `Stream` or `File` from `org.unirail.Meta`. Use when bytes do not come from a pack: file handle, DB BLOB, encoder output, another socket.
- RT: generated code never holds the value: out → runtime reads from your source straight into socket buffer; in → writes from socket buffer straight into your sink. Never loaded into array of full size, either side.
```csharp
class Capture {
    public ulong sequence;
    [S(200_000)] Stream feed;       // chunked: sender may end feed any time
}
class Attachment {
    public ulong sequence;
    [D(+100)] string name;
    [S(300_000)] File content;      // one length prefix: bytes piped from file into socket
}
```
- RULE: both need size cap `[S(N)]` →§15.4. Difference = framing (how receiver learns where bytes end).

### §15.3.1 `Stream` framing - chunked and interruptible
KW: chunk, two-byte length, little-endian, 0x0000, 0xFFFF, ABORT, 0xFFFE, 65534, terminator, interruptible, broken pack
- WIRE: sequence of chunks, each with 2-byte little-endian length in front, ended by length 0:
```
[len][data] [len][data] … [len][data] [0x0000]
```

| Length value | Meaning |
|:--|:--|
| `0x0001`–`0xFFFE` | chunk of that many bytes follows; max chunk `0xFFFE` = 65 534 bytes |
| `0x0000` | stream ended normally |
| `0xFFFF` (`ABORT`) | stream ended as void: what receiver got after its last complete pack is to be discarded; nothing more of stream will come |

- RT: transmitter writes at most one chunk per pass over socket buffer, sized to room left → chunk ≈ socket buffer length in practice; `0xFFFE` = format ceiling, not typical size.
- WIRE: chunk never empty (zero length = terminator). Cost: 2 bytes per chunk + 2-byte terminator.
- RT: `Stream` interruptible: ends whenever source says, after any number of bytes; receiver sees ordinary end, keeps everything sent.
- RT: `ABORT` is different: written by relay when source of a packet it forwards disappears part-way, and by tunnel endpoint to end its tunnel →§18. Inside a pack, field stream ending with `ABORT` leaves pack incomplete → receiver reports broken pack.

### §15.3.2 `File` framing - one length up front
KW: File, varint, total length, Length, SeekableByteChannel, size(), not interruptible
- WIRE: one varint-encoded total length, then exactly that many bytes: `[varint total][data …]`.
- WIRE: prefix width follows actual total sent, not `[S(N)]` cap: 100-byte file under 1 GB cap → 1-byte prefix.
- RT: source must know length before first byte. C#: runtime takes it from seekable `System.IO.Stream` (`Length`); Java: generated method returns `SeekableByteChannel` (`size()`). Source with unknown length (non-seekable) or longer than cap fails on sending side before anything sent.
- RT: receiver checks total against cap as soon as prefix arrives, before reading any payload byte, then reads exactly that many bytes. Total 0 legal: one `0x00` byte, no body.
- NOTE: whoever forwards a `File` without decoding must read the prefix to know where it ends.
- RULE: `File` not interruptible: once length on wire, receiver waits for exactly that many bytes; source must deliver exactly declared length, no more, no fewer.

### §15.3.3 Chunked or length-prefixed - comparing the framings
KW: chunked, length-prefixed, trimmed payload, trimmed string, Binary, interruptible
| Payload | Wire format | Total known up front? | Interruptible? |
|:--|:--|:--|:--|
| `Stream` | `[len₁][data₁] … [0x0000]` | no | yes |
| `File` | `[varint total][data]` | yes, required | no |
| trimmed pack payload | chunked, as `Stream` | no | no |
| trimmed `string` payload | `[varint total][UTF-8]`, as `File` | yes | no |
- NOTE: chunked: both ends produce/consume incrementally without agreeing on total; any party (receiver, store, relay) finds end from framing alone; price 2 bytes/chunk. `File` single prefix denser when total cheap to know (file on disk, buffer in memory).
- NOTE: trim borrows chunked framing for boundary visibility, but crosses one complete serialized value → not interruptible (truncated = torn object for decoder) →§17.
- NOTE: fixed-size content (hashes, signatures) → `Binary` array, no length prefix at all →§15.6.

### §15.3.4 What the generator produces for a conduit field
KW: abstract partial class, partial interface, protected abstract, BytesDst, BytesSrc, _hasValue, System.IO.Stream, ReadableByteChannel, WritableByteChannel, SeekableByteChannel, demo, presence bit
- GEN: pack with conduit field cannot be used as generated on any host, whatever implementation modifier (→§10): only your code supplies source/sink. Pack configured concrete keeps other fields concrete, becomes abstract class (C# `abstract partial class`, Java `abstract class`), conduit methods `protected abstract`. Pack configured abstract = interface (C# `partial interface`, Java `interface`) as any abstract pack. Generated `demo` shows methods to implement.
- GEN: per field up to 3 methods: source runtime reads when sending; sink it writes into when receiving; "has value to send". Send-only host gets 1st + 3rd; receive-only host gets 2nd.
- GEN: C# source and sink = `System.IO.Stream`. Java `Stream` field: `ReadableByteChannel` source, `WritableByteChannel` sink; Java `File` field: `SeekableByteChannel` for both.
- WIRE: conduit field always carries presence bit. "has value" `false` → field not sent; receiver never asks for sink. Hence `?` on conduit (`Stream? feed;`) changes nothing.
- RULE: conduit cannot be inside a collection: not array element, `Map` key/value, `Set` element (whole-field wire format with own framing).
- GEN: C# for `FileEntry` of AdHoc's own protocol, `bytes` = `[S(0x5_000_000)] Stream` (abridged):
```csharp
public partial interface FileEntry : AdHoc.Connection.Receiver.BytesDst, AdHoc.Connection.Transmitter.BytesSrc
{
    public string? _path { get; set; }
    System.IO.Stream _bytes_(AdHoc.Connection.Receiver scope);    // sink a received conduit is written into
    System.IO.Stream _bytes_(AdHoc.Connection.Transmitter scope); // source a sent conduit is read from
    bool __bytes_hasValue(AdHoc.Connection.Transmitter scope);    // false: field not sent
    // ... generated serialization and deserialization
}
```
- NOTE: AdHocAgent's implementation (every project file streamed from disk on upload, to disk on download):
```csharp
class Entry(Files owner) : AdHocProtocol.FileEntry {
    public string? _path { get; set; }
    public string src = "";    // send only: file bytes are read from
    public Stream _bytes_(AdHoc.Connection.Transmitter scope) => File.OpenRead(src);
    public bool __bytes_hasValue(AdHoc.Connection.Transmitter scope) => File.Exists(src);
    public Stream _bytes_(AdHoc.Connection.Receiver scope) {
        var dst = owner.resolve(_path!);
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        return new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None);
    }
}
```

### §15.3.5 A conduit at runtime - sources, sinks and the end of a stream
KW: Read, Write, Connection.Transmitter.BytesSrc, Connection.Receiver.BytesDst, OnStreamComplete, FileStream, transmit loop, journal, guaranteed
- RT: runtime asks for source/sink when field's turn comes in the pack, then moves bytes in blocks: `Read` from source into free part of socket buffer; `Write` from received part of socket buffer into sink.
- RT: inside runtime a conduit = `Connection.Transmitter.BytesSrc` (send) / `Connection.Receiver.BytesDst` (receive) — same two contracts every generated pack implements; also how relay consumes a stream and re-emits it →§18.
- RT: `Read` / `Write` run on connection's transmit and receive loops. C# `System.IO.Stream` source: `Read` returning 0 = end of stream.
- PITFALL: source must answer `Read` promptly; source that waits for data holds transmit loop of whole connection.
- RT: `Stream` conduit ends → runtime closes a `FileStream` you returned; for any other source/sink only drops reference.
- RT: to decide yourself, call `scope.OnStreamComplete(hook)` on scope argument right before returning the stream. Hook receives transmitter or receiver + flag: `true` = ended with terminator, `false` = torn down (e.g. delete partly received file). `scope.OnStreamComplete()` without argument → runtime leaves stream alone (a `FileStream` you keep using).
- RT: `File` conduit closes its source and sink once committed length is through.
- RT: connection breaks with conduit under way, carrying pack guaranteed → receiving side keeps pack and sink; if session continues, pack goes on from byte where socket cut it, replayed from journal (bytes as first sent) — source not asked again. Not-guaranteed pack lost with socket; receiving side discards its part →§14.

### §15.3.6 Pausing an endless stream for urgent packs
KW: segment, endless feed, urgent pack, send, one pack at a time, video, journal, unguaranteed
- RT: connection's transmitter writes one pack at a time: once pack carrying `Stream` started, everything queued behind waits until stream ends.
- PITFALL: endless feed (video, audio, capture) must not be one endless pack. Send as sequence of `Stream` packs = segments:
  1. feed = named `Stream` pack sent again and again; `[S(N)]` caps one segment; feed as whole has no end.
  2. urgent pack: queue it with `send`, let current segment end: next `Read` of segment's source returns 0 → runtime writes 2-byte terminator, receiver keeps every byte of segment.
  3. transmitter takes next queued pack (urgent one), out within ≈ one socket buffer of bytes after source ended segment.
  4. next segment carries feed on.
```csharp
/// <see cref="InCS"/>
struct Drone : Host {
    [S(64_000_000)] public class VideoSegment : Stream { }  // one segment of endless feed, at most 64 MB
    public class Alert {                                     // urgent: goes out between two segments
        byte code;
        [MinMax(0, 100)] byte battery;
    }
}
/// <see cref="InTS"/>
struct Operator : Host {
    public class Command { [MinMax(-100, 100)] sbyte pitch; [MinMax(-100, 100)] sbyte roll; }
    public class Land { }                                    // urgent command
}
interface Link : Connects<Operator, Drone> {
    [l____________<(Operator.Command, Operator.Land)>]
    [____________r<(Drone.VideoSegment, Drone.Alert)>]
    struct Flying { }
}
```
- RT: other direction never waits: receive and transmit on separate loops → `Land` operator→drone not queued behind drone's video; only same-direction packs wait for segment end.
- NOTE: segment cannot be paused and continued as same pack — it ends, feed continues in next. Cut at boundary decoder can resume from (frame, group of frames), or end segments at such boundaries anyway so urgent pack never waits more than one.
- PITFALL: never let `Read` wait for encoder (holds transmit loop of whole connection); when encoder has nothing ready, end segment, send next when ready.
- PITFALL: keep feed out of journal: guaranteed pack copied into sender's journal; guarantee alerts and commands (→§14), leave segments unguaranteed.

## §15.4 The size cap of a conduit - `[S(N)]`
KW: [S(N)], SAttribute, maxLength, InvalidOperationException, RuntimeException, Error, TYPEDEF, cap, "Stream would exceed max size"
- DECL: `[S(N)]` = largest conduit in bytes the receiving side accepts. Mandatory on every conduit (generated code cannot check conduit bytes; cap isolates endpoint or middle tier from peer sending more than agreed). In `Meta.cs`: `SAttribute(long maxLength)`, applicable to field or class inheriting `Stream`/`File`.
```csharp
[S(100_000)] File raw;                 // bare File field: [S] required
[S(100_000), ChaCha20] Stream secret;  // chain on top does not replace the cap
```
- RT: receiving `Stream`: runtime counts bytes written into sink; when count would pass `N` → reception fails.
- RT: receiving `File`: declared total checked as soon as prefix arrives; total > `N` fails before any payload byte read.
- RT: sending: `Stream` source yielding > `N` bytes, or `File` source longer than `N` → fails on sender.
- RT: failure = exception: C# `InvalidOperationException`, Java `RuntimeException`, TypeScript `Error` (e.g. "Stream would exceed max size of 100000 bytes.").

| Rule | Detail |
|:--|:--|
| Required on every bare `Stream`/`File` field | directly, through a TYPEDEF carrying it (→§5), or by typing field with named conduit pack (brings own cap) |
| Required on every named conduit pack | `[S(N)] class Frame : Stream { }` |
| One argument, integer constant | compile-time constant expression; named constant works (→§6) |
| `N` > 8 | payload fitting 8 bytes ≤ one 64-bit primitive: use primitive or `Binary` array |
| Never on field typed with named conduit pack | pack already declares cap; agent reports duplicate, names pack's cap |
| Never on non-conduit field | not on primitive, pack-typed or `string` field, incl. one made directional by trim: trimmed field wraps bounded payload (pack, or string capped by `[D(+N)]`) = own limit |
| Trim does not lift it from conduit | bare `Stream` field carrying trim is still raw conduit, still needs `[S(N)]` |
| Independent of transform stages | cap counts conduit's own bytes; stages on top change nothing |

- DECL: cap via TYPEDEF:
```csharp
class Feed { [S(1_000_000)] Stream TYPEDEF; }   // alias carries the cap
class Probe {
    Feed raw;                                   // bare Stream capped at 1 000 000 bytes
}
```
- PITFALL: TYPEDEF carries cap but not transform chain →§5.

## §15.5 Named conduit packs
KW: named conduit pack, class : Stream, class : File, [S(N)], ContentType, constants, pack id, headers, t_stream, t_file
- DECL: class inheriting `Stream` or `File` = reusable framing type carrying cap and optionally transform chain under one name.
```csharp
[S(150_000)]       class Frame     : Stream { }   // chunked, interruptible, at most 150 000 bytes
[S(150_000), Zstd] class FrameZstd : Stream { }   // same, compressed
[S(64_000)]        class Thumb     : File   { }   // one length prefix, at most 64 000 bytes
[S(100_000_000)]
class NamedStream : Stream {
    public const string ContentType = "application/octet-stream";   // metadata of generated pack, never on wire
    public const int    ChunkHintKB = 64;
}
class Album {
    public ulong sequence;
    Thumb     cover;     // File conduit capped at 64 000 bytes
    Frame     preview;   // Stream conduit capped at 150 000 bytes
    FrameZstd packed;    // same, with pack's [Zstd] chain
}
```
- RULE: `[S(N)]` mandatory, exactly one integer constant argument > 8 (same rules as field).
- RULE: body has no instance fields (error: could never reach wire). Constants and static fields allowed → constants of generated pack (`NamedStream.ContentType`), never payload.
- RULE: inherit `Stream` or `File`, not both (incompatible wire formats).
- RULE: named `File` pack takes no transform chain and no trim →§16.3.
- WIRE: listed on a channel → transmittable pack with pack id like any other: sent standalone as pack id, bound headers (→§8), then framing and bytes.
- NOTE: in description metadata agent sends to generator, conduit pack kind encoded in sign of its cap: negative = `Stream`, positive = `File`, zero = ordinary pack.

### §15.5.1 A named conduit pack as a field type
KW: field type, bare form, TYPEDEF pass, t_stream, t_file, headers stripped, not collectable, ?
- RULE: field typed with named conduit pack parsed exactly like bare form:
```csharp
[S(1_110_000)] class Report : File { }
class Shipment {
    Report report;                  // parsed as:  [S(1_110_000)] File report;
}
```
- NOTE: agent rewrites such field into raw conduit right after TYPEDEF pass (so TYPEDEF aliasing a conduit pack is rewritten too), before conduit checks. Pack contributes only framing kind, cap and (for `Stream` pack) chain:

| On such a field | Result |
|:--|:--|
| field type in generated metadata | `Stream` or `File` (`t_stream` / `t_file`), not the pack: no sub-pack reference |
| max length of field | pack's `[S(N)]` cap, bytes |
| wire bytes | framing + bytes only: no pack id; headers bound to pack stripped (headers ride standalone packs, not fields) |
| transform chain and trim | inherited from `Stream` pack, composed with field's own →§16.3, →§17; `File` pack passes nothing |
| `[S(N)]` on field | rejected: cap belongs to pack |
| element of array/`Map`/`Set` | rejected like bare `Stream`/`File`; wrap conduit in ordinary pack and collect that |
| `Report? report;` | `?` ignored, as on bare `File?`/`Stream?` |

- GEN: named conduit pack used only as field type and never listed on a channel drops out of generated code entirely (like bare type). List it on a channel to also send standalone with pack id and headers.

## §15.6 Binary, File or Stream - choosing by the shape of the bytes
KW: Binary, Binary[], Binary[,,], [D(N)], File, Stream, Value Pack, ulong halves, UUID, hash, Map key
- RULE of thumb: where bytes live (ByteBuffer, DB row, file) does not decide type; shape of bytes and code path do. `Binary` array (→§5) = length is part of format (hash, signature, fixed-size tag). Length = property of value (BLOB, serialized object, token, DB cell) → `File` or `Stream` conduit.

| Bytes are … | Declare | Why |
|:--|:--|:--|
| 1–8 bytes with meaning: port, counter, small id, IPv4 + port | primitive or Value Pack (→§7) | one primitive: no array, loop, object |
| exactly 16 bytes: UUID, MD5, IPv6 | pack of two `ulong` halves | 16 bytes on wire, two 64-bit reads each side |
| fixed length part of format: 32-byte hash, 4-byte tag | `[D(N)] Binary[]` | no length prefix: value is the array |
| variable length known when sent: cell, serialized value, token, BLOB | `[S(N)] File` | one varint length, bytes move in blocks between socket buffer and your channel |
| length unknown until end, or transfer may be cut short: encoder feed, capture | `[S(N)] Stream` | chunked, interruptible |
| must be `Map` key, `Set` element or array element | `Binary[]` or `Binary[,,]` | conduit cannot be collected; keep such keys small |

- NOTE: serialized object of type description does not model → `[S(N)] File`. When set of possible types known, pack of optional alternatives describes it better →§5.
- RT: `Binary` array = value: held whole, receiver allocates array. Bytes not moved one at a time: concrete pack copies each portion socket buffer holds in one block; abstract pack is handed portion as window of buffer (→§5). Conduit = channel: bytes move between socket buffer and what implementation owns (`ByteBuffer`, file, DB page), value never held whole.
- PITFALL: long `Binary[,,]` almost always a `File` in disguise.
- PITFALL: conduit costs: on every host pack needs your code for source/sink (generated abstract class or interface); field cannot be in collection. Fixed-size `Binary[]` pays neither.

## §15.7 Worked example - delivering an archive of any size
KW: SrcZip, FileEntry, FileEntry.List, Result, StreamFlowAttribute, archive, Zstd, upload
- NOTE: AdHoc's protocol delivers project files (uploaded description sources, received generated code) as archive of any size: one list pack of entries, each = path + one conduit.
```csharp
/// The on-the-wire format of a file archive, and the single place it is defined.
[Zstd]
public class SrcZip : StreamFlowAttribute { }
public class FileEntry {
    [D(+4096)] string path;              // '/'-separated path inside archive
    [S(0x5_000_000)] Stream bytes;       // raw file content, up to 80 MiB, piped between disk and socket
    public class List {
        [D(0xFFFF)] FileEntry[,] files;  // up to 65 535 entries
    }
}
public class Result {
    string task;
    [SrcZip] FileEntry.List result;      // generated code, compressed on wire, typed at both ends
}
```
- RT: `Result` with thousands of files streams through one socket buffer: each entry read from disk in blocks as buffer has room; `SrcZip` (`[Zstd]` →§16) compresses on the fly through own fixed-size staging buffer; no archive-sized buffer anywhere (server, agent, between).
- NOTE: agent implements `FileEntry.List` and `FileEntry` abstractly: one re-armed `FileEntry` instance serves every entry (entries written/read strictly one after another; each received entry has streamed itself to disk by the time list hands it over).
- NOTE: chain sits on fields not on `List`: several packs carry the list over different legs with different needs (upload leg also cuts chain so server keeps archive compressed →§17). When one chain fits every use, write it on the list pack (`[Zstd] public class List { … }`) → compressed wherever sent.
- NOTE: when every file length known before send, `[S(N)] File content;` does same job with one length prefix per file instead of chunks.

## §15.8 Streaming typed packs
KW: abstract, -, event-driven, InJAVA, Server, Project, trim, typed pack
- Two independent mechanisms when payload is a typed pack:
  - abstract (`-`) implementation (for receiver that cannot afford the object): receiving side of any pack or any single field generated event-driven: parser calls your code per value as it comes off wire; serializer asks your code per value as buffer has room; full object never allocated. Chosen per host, language, pack, field in host's doc comment →§10 (also how one field of concrete pack is made abstract).
  - trim (for middle tier that cannot afford the schema): store/broker keeps typed payload as opaque bytes →§17.
- NOTE: AdHoc code-generation server receives every user's `Project` pack (arrays up to 65 535 packs and fields) through packs generated abstract for its Java code (abridged; real list names seven concrete packs):
```csharp
/**
<see cref = 'InJAVA'/> The following packs of the `Server` host are fully implemented and generated in JAVA.
<see cref = 'Result'/>
<see cref = 'Progress'/>
<see cref = 'InJAVA'/>-- The remaining packs are generated in JAVA as abstract (without implementation).
*/
struct Server : Host { /* packs */ }
```
- → server parses project of any size without building it in memory.
- NOTE: payload with no structure → `Stream` field simpler than abstract field: hands bytes, not sequence of values.

## §15.9 Streaming rules at a glance
KW: [S(N)], chunked framing, File framing, interruptibility, named conduit packs, presence bit, journal
| Rule | Short form | → |
|:--|:--|:--|
| `[S(N)]` | required on bare conduits (direct/TYPEDEF/named pack) and named conduit packs; `N > 8`; never on field typed with named conduit pack or non-conduit field; independent of chains; enforced on receive and send | §15.4 |
| Chunked framing | 2-byte LE length, 1–65 534 bytes/chunk; `0x0000` end; `0xFFFF` `ABORT` = void | §15.3.1 |
| `File` framing | one varint total (width = actual total); length known up front; checked vs cap before payload | §15.3.2 |
| Interruptibility | `Stream` yes; `File` no; trimmed payload no | §15.3.3 |
| Named conduit packs | no instance fields; `Stream` xor `File`; as field type = bare conduit; `Stream` pack's chain/trim inherited, pack's entries nearer wire | §15.5, §15.5.1 |
| Generated code | pack unusable as generated on every host; source, sink, "has value"; presence bit, `?` ignored | §15.3.4 |
| Lost/broken connection | guaranteed pack resumes from journal at cut byte, source not re-read; other pack lost, receiver discards part | §15.3.5 |
| Chains, trims | | §16.12, §17.14 |

# §16 Transform chains
KW: transform chain, stage, StreamStageAttribute, [Zstd], [ChaCha20], leaf, wire, flow, chunked framing
SUMMARY: Ordered byte-to-byte stages (compression, encryption, custom) applied to serialized bytes of a field, pack or whole connection before wire, inverse after arrival. Order, targets, composition with conduit packs, agent rules, parameters, flows, built-ins, cipher keys, custom stages, connection chains. Cutting a chain per endpoint →§17.
- Model: project → host → connection → actor → state → branch → pack → field → wire bytes; runtime: serialized bytes → stages → chunked framing → socket buffer.
- DECL: chain written as attributes; target keeps its own type: chain wraps serialized bytes (`leaf`), does not replace them.
- WIRE: anything carrying a stage travels framed as chunked stream →§15.3.1.

## §16.1 Stages and dataflow order
KW: stage, StreamStageAttribute, dataflow order, leaf, wire, compress then encrypt, [Zstd, ChaCha20], inverse
- `stage` = one link of a chain: attribute class deriving from `StreamStageAttribute`. Built in: `[Zstd]` (compression), `[ChaCha20]` (encryption); custom →§16.10. Applicable to field, pack, connection.
- RULE: stages in dataflow order: left = leaf, right = wire.
```csharp
[Zstd, ChaCha20] Order secured;   // leaf → Zstd → ChaCha20 → wire: compress, then encrypt
```
- RT: transmit: left-most stage takes serialized bytes, right-most hands output to chunked framing. Receive: list walked backwards, right-most inverse first. Stages run on sending side, inverses on receiving side → both hold same `Order`.
- RULE: cipher to the right of compressor (ciphertext does not compress).
- NOTE (diagram): receive side = de-framing → ChaCha20 decrypt → Zstd decompress → typed pack. Field typed with named Stream pack: field's stages nearer leaf, pack's inherited stages nearer wire.
- DECL: chain on each target kind (live test description):
```csharp
[Zstd]                                       // a pack: compressed on every transmission
class Archive {
    public ulong         sequence;
    [D(+200_000)] string text;
    [D(65_536)]   byte[] data;
}
class Vault {
    public ulong sequence;
    [Zstd(6), ChaCha20] Order  secured;      // pack-typed field: compress at level 6, then encrypt
    [Zstd, D(+100_000)] string notes;        // single string: its bytes are the leaf
    [S(500_000), Zstd]  Stream packed_feed;  // bare Stream: conduit's bytes are the leaf
}
[Zstd(6)]                                    // a connection: everything A and B exchange
interface AtoB : Connects<A, B> { /* actors and states */ }
```
- DECL: AdHoc's protocol: chain written once as flow (→§16.6), applied at each archive field:
```csharp
[Zstd]
public class SrcZip : StreamFlowAttribute { }
public class Result {
    string task;
    [SrcZip] FileEntry.List result;          // still a FileEntry.List at both ends
}
```

### §16.1.1 What the generator produces for a chain
KW: Chain, Chunked, Pool<Chain>, chain(int _id, ...), datatype, case, design-time parameter, 64 KiB, pooled, parked session
- GEN: field keeps its type (`Result.result` = `FileEntry.List?` property); serialization hands value to chain by number: `__src.datatype(2, result_new_item(__src), 4)`.
- GEN: every connection's generated `Transmitter` and `Receiver` own a `Chain` class building stages for each chain the connection carries: one `case` per distinct chain, design-time parameters baked in (abridged C#):
```csharp
public class Chain : Chunked
{
    // Injection point: the pool keeps up to 2^5 = 32 spare transmit chains of transform stages.
    public static readonly Pool<Chain> pool = new Pool<Chain>(5, () => new Chain());
    readonly Zstd.Transmitter.Stage Zstd = new Zstd.Transmitter.Stage();
    public AdHoc.Connection.Transmitter.Chunked chain(int _id, Transmitter.BytesSrc _src, Transmitter.BytesSrc datatype)
    {
        switch (_id)
        {
            case 0:                          // [Zstd]
            {
                int level = 3;               // design-time parameter, baked in
                Zstd.Init(_src, level);
                return Init(Zstd, datatype);
            }
            // ... one case per distinct chain of this connection
        }
        return Init(_src, datatype);         // no stage: chunked framing alone
    }
}
```
- RT: chain objects pooled, re-armed per stream: stages allocated once, not per message.
- RT: stage memory: `Zstd` stage (sending or receiving) and receiving `ChaCha20` stage each stage up to 64 KiB plaintext; `Zstd` stage holds a zstd codec context.
- RT: connection's transmitter and receiver take chains from pool when first needed, keep them (re-armed per stream) until closed. Parked session keeps no chains for receiver (closed when session parks). Transmitter closed same way unless side journals: journaling side keeps transmitter, chains included, as the loss left it →§14.

## §16.2 Where a chain may sit
KW: target, pack, named Stream pack, Stream field, string, Connects, VirtuallyConnects, Modify<>, File, Value Pack, enum, collection, TYPEDEF
| Target | Chain? |
|:--|:--|
| ordinary pack | yes: compressed/encrypted on every transmission, standalone or as field value |
| named `Stream` pack | yes: field typed with it inherits chain, composes with own →§16.3 |
| field typed with ordinary pack | yes |
| bare `Stream` field, or field typed with named `Stream` pack | yes; with named pack, field's chain composed with pack's |
| single `string` field, plain or trimmed | yes: plain string's own encoding = leaf; trimmed string's UTF-8 form = leaf on cut leg →§17.6 |
| any connection, physical `Connects<>` or virtual `VirtuallyConnects<>` | yes →§16.11 |
| `Modify<>` modifier of a pack or of a connection | yes: replaces chain of its target →§22 |
| `File` field / `File` pack, incl. field typed with named `File` pack | no — stage and trim alike |
| collection field of packs or strings: array, `Map`, `Set` | yes, a stage: whole collection = one stream through chain, items inside without own chain; no trim |
| `string` or pack inside collection: item, `Map` key/value, `Set` element | only via chain of collection field; item has no chain of its own |
| primitive, enum or Value Pack field | no — written in place: no framing for stage, no boundary for cut |
| collection of primitives, enums or Value Packs | no — put collection in pack, chain on pack |
| TYPEDEF | dropped silently — write chain on field, hoist into flow, or use named `Stream` pack →§5 |

- RULE: chain where it may not sit stops build:
```
A stream transform chain (or a trim) on field '…' is allowed only on a pack-typed field, a bare Stream field,
a string field, or a collection of packs or strings - not on a File / primitive / value-pack field or a
collection of them. Group the data inside a pack and put the chain on the pack.
```
- RT: chain on collection field compresses/encrypts collection as whole, not per item: one stream → compressor sees every item, finds repeats between them; one framing for field instead of one per item. Pack holding field = leaf of that stream: writes field into chain from where field begins, reads back same way; no pack made for it. Field limits stay collection's: `[D(…)]` bounds items, not stream.
```csharp
class Job {
    [Zstd, D(0xFFFF)] SrcFile[,] files;       // all files of job in one Zstd stream
    [Zstd, D(+60)] Map<string, Entry> index;  // keys and values in one stream
}
```
- RULE: items inside stream have no chain of their own; chain declared on items' pack is replaced by field's (as for single field typed with that pack). Collection field with no own chain sends each item through chain of item's pack, if it has one. Trim refused on collection field (trim hands one value over).
- RULE: field typed with Value Pack or enum gets its own (diagnostic) message (value written in place by every language). To compress/encrypt small values, group with more data in ordinary pack, chain on that pack.
- NOTE: trim on field whose pack would fit Value Pack but for its null bit is legal, earns warning that streaming costs more than sending →§17.12.
- RULE: chain on ordinary pack and chain on field typed with it are NOT merged. Pack's chain = default for that pack wherever sent; chain on field typed with pack = that field's chain instead. Only conduit pack merges chain into field (field is the pack's framing) →§16.3. Ordinary pack does hand legs of its trim to fields typed with it →§17.4.

## §16.3 Composing a conduit pack's chain with the field's
KW: named Stream pack, composition, inherited, field entries nearer leaf, pack entries nearer wire, File pack, Base64
- RULE: named `Stream` pack chain + field's chain = one chain: field's entries nearer leaf, pack's nearer wire (pack says how type reaches wire; field adds transform inside).
```csharp
public class Base64Attribute : StreamStageAttribute { }   // custom stage without role
[S(1_600_000), Zstd] class TelemetryFrame : Stream { }
class Telemetry {
    [Base64] TelemetryFrame frame;   // leaf → Base64 → Zstd → wire
}
```
- RULE: every chain rule checked on composed result; conflict may involve stage absent from field's declaration; diagnostic names origin:
```csharp
class Telemetry {
    [ChaCha20] TelemetryFrame frame;
    // ERROR: The stream chain on …Telemetry.frame encrypts before it compresses: the cipher 'ChaCha20' stands to the
    //        left of the compressor 'Zstd' (inherited from the conduit pack `…TelemetryFrame`).
}
```
- RULE: inherited entries always land right of field's own. Order composition cannot produce → write in one place: move stage onto pack, or take it off pack and write whole chain on field.
- NOTE: flow applied to pack expands there, inherited whole. Trim among pack's entries comes down to field at position pack gave it →§17.9.
- RULE: `File` pack passes nothing down (may carry nothing): single length-prefixed conduit, one committed total, no per-chunk framing → stage has nothing to ride on, length-changing stage has no length field to grow, cut has no chunk boundary. Stage or trim on `File` pack stops build:
```csharp
[S(1_110_000), Zstd] class Report : File { }
// ERROR: A transform chain on File pack '…Report' is not allowed - a File is a single length-prefixed conduit with
//        no room for a byte-transform stage, nor a boundary at which a trim could hand bytes over. Inherit `Stream`
//        (chunked), or apply the chain to an ordinary or Stream pack.
```
- RULE: field typed with named `File` pack = always plain conduit pack declares; chain on that field rejected too. To transform or cut a conduit, inherit `Stream`.

## §16.4 Roles and the rules of a chain
KW: role, StreamCompressionStageAttribute, StreamCipherStageAttribute, ZstdAttribute, ChaCha20Attribute, encrypts before it compresses, duplicate stage, level, Information
- `role` = base class stage derives from; lets agent recognize compressor/cipher and check combination:
```
StreamStageAttribute                     any byte transform, no role
├─ StreamCompressionStageAttribute       compression role  →  ZstdAttribute      [Zstd]
└─ StreamCipherStageAttribute            cipher role       →  ChaCha20Attribute  [ChaCha20]
```
- NOTE: new algorithms (`Lz4` compressor, `AesCtr` cipher) derive from role base, checked exactly like built-ins. Trim is not a stage: no role, does not count toward limits →§17.7.
- RULE: agent checks every chain after flows expanded and inherited entries composed:

| Rule | Agent does |
|:--|:--|
| at most one compressor and one cipher per chain | error naming both stages and where each came from |
| compressor left of cipher | error "encrypts before it compresses" (compressing ciphertext gains nothing) |
| stages without role (e.g. `Base64`) | allowed anywhere |
| each stage at most once per chain | error: remove duplicate |
| two different stage types same name (`a.Foo`, `b.Foo`) | error: rename one (name = stage's handle in generated chain) |
| chain with trim: no stage named `ToStream` or `FromStream` | error: names taken by trim |
| parameter names unique across whole chain | error: parameters of all stages share one flat scope |
| one parameter name, one type, across a stage's constructors | error: rename one |
| `Zstd` level | error outside 1–22; warning above 12 |
| trim in connection's chain | error →§17.4 |

- CLI: agent reports every chain it builds at Information level, dataflow order, parameters and roles (first place to look):
```
stream chain on …Vault.secured (line 12): leaf -> Zstd(level=6)[compression] -> ChaCha20(key=runtime,iv=runtime)[cipher] -> wire  =>  container #3
```

## §16.5 Stage parameters - design-time or runtime-injected
KW: design-time, runtime-injected, constructor, Binary, Binary[,], key, iv, nonce, level, nullable
- DECL: stage constructors declare parameters; agent reads them, never runs them. Use AdHoc types: `Binary`, not `byte` (`byte` signed in Java, unsigned in C#, absent in TypeScript).
- Sorted by whether parameter gets a value, not by type:

| Parameter | Declare | Value from |
|:--|:--|:--|
| design-time | constructor default (`int level = 3`) or value where applied (`[Zstd(6)]`) | baked into description, shared by every use of that chain |
| runtime-injected | declared, never given value (e.g. `Binary[,] key`) | your code at runtime via generated code |

- GEN: any declared parameter left without value → runtime hook, whatever type; may declare with plain non-nullable type and not assign; generator makes it nullable.
- RULE: agent reads union of all constructors. Buffer-shaped runtime parameter (`Binary[,]` key/nonce) cannot be attribute argument → put in own constructor overload: applied form stays short, overload declares shape.
- GEN: every declared parameter reaches both sides: each = constant of generated stage on sender and receiver; stage implementation decides use. `Zstd` `level` matters only to encoder (zstd stream self-describing); decoder ignores it.
```csharp
public class Lz4Attribute : StreamCompressionStageAttribute {
    public Lz4Attribute(int level = 1) { }                    // design-time: 1, or [Lz4(3)]
}
public class AesCtrAttribute : StreamCipherStageAttribute {
    public AesCtrAttribute() { }                              // form you apply: [AesCtr]
    public AesCtrAttribute(Binary[,] key, Binary[,] iv) { }   // declares runtime-injected key and iv
}
```
- PITFALL: keys and nonces are secrets: runtime-injected shapes, never values in description.

## §16.6 Flows - a chain declared once
KW: flow, StreamFlowAttribute, named chain, expands in place, SrcZip, persisted format, identical chains
- `flow` = named chain: class deriving from `StreamFlowAttribute`, its attributes = stages in dataflow order. Applied by name as one attribute; target keeps type.
```csharp
[Zstd(6), ChaCha20]
public class CipherAndFastCompress : StreamFlowAttribute { }
class Orders {
    [CipherAndFastCompress] Order payload;   // still an Order: leaf → Zstd(6) → ChaCha20 → wire
}
```
- RULE: flow expands in place wherever applied: field, pack, connection, or named `Stream` pack (field typed with it inherits entries whole). Chain rules checked on expanded result.
- PITFALL: flow lists stages, not other flows: agent does not expand flow listed inside another flow → its stages missing from chain. List stages themselves.
- NOTE: flow may contain a trim → pins every use to trim's endpoints. Only when flow = one store-and-replay pipeline; for general chain write trim where applied →§17.10.
- PITFALL: flow names a format. `SrcZip` documented as "the on-the-wire format of a file archive, and the single place it is defined": change it → every archive stored earlier stops decoding at once. While stored data must stay readable, declare new flow beside old.
- GEN: identical chains generated once: same stages, same parameter values, same kind of target → one generated chain (one `case` of `Chain`). Chain written inline more than once → Information-level advice to name it as flow.

## §16.7 Built-in stages - Zstd and ChaCha20
KW: Zstd, ZstdAttribute, level, ZstdSharp.Port, ChaCha20, ChaCha20Attribute, RFC 8439, keystream XOR, 256-bit key, 96-bit nonce
- NOTE: both fully implemented in runtime library; no codec to write. Cipher still needs key →§16.9.

| Stage | Role | Declaration | Does |
|:--|:--|:--|:--|
| `[Zstd]`, `[Zstd(level)]` | compression | `ZstdAttribute(int level = 3)`, on field, class or interface, once | Zstandard; `level` 1–22, default 3 (zstd's own); encoder-only; never over `File`. C# stage uses ZstdSharp.Port package |
| `[ChaCha20]` | cipher | `ChaCha20Attribute()` and `ChaCha20Attribute(Binary[,] key, Binary[,] iv)`, any target, once; 2nd constructor declares key and nonce shape | ChaCha20 (RFC 8439): 256-bit key, 96-bit nonce, 20 rounds; keystream XOR — byte-incremental, resumable across chunks and socket fills, no padding/block alignment, same operation both ways. Implemented in runtime of each target language |

- NOTE: stream cipher fits chunked stage: keystream XOR → any chunk size, no block alignment, resumable, same op both directions.

### §16.7.1 What Zstd and ChaCha20 guarantee
KW: lossless, confidentiality, no integrity, no authentication, MAC, Poly1305, nonce reuse, tampering
- RT: `[Zstd]` lossless: receiver gets exactly bytes sender's leaf produced; zstd stream carries what decoder needs → receiving side needs no configuration.
- PITFALL: `[ChaCha20]` = confidentiality only, no integrity, no authentication. Modified ciphertext decrypts to different bytes, stage reports nothing: tampering undetected. For integrity: own stage authenticating bytes (e.g. MAC over stream), or encrypted transport (→§16.9): its ChaCha20-Poly1305 records authenticated, but only per physical connection. End-to-end `[ChaCha20]` through relay keeps relay from reading payload, not from altering it undetected.
- RULE: a (key, nonce) pair must never encrypt two streams (nonce reuse under one key leaks XOR of two plaintexts). Per-pack form takes fresh nonce per pack by itself; for chain on connection's stream, injected key and nonce must be fresh per stream.

### §16.7.2 When to compress, and at which level
KW: compress, redundant, opaque, Zstd level, wire levels, archival, warns above 12
- NOTE: compress where bytes known redundant: text, metadata, repeated identifiers, log. Not on pack with opaque payload: blob, key, UUID, counter, already-compressed document.
- NOTE: chain costs on every message crossing it: codec context, frame header, flush, two codec calls each side; saving data-dependent; opaque bytes give nothing. Opaque-payload protocol → chain on connection: deployment adds it when it knows data; one frame per connection, not per message.
- Pick level for a wire, not archive:

| Levels | Use |
|:--|:--|
| 1–6 | wire levels; 1–3 run at hundreds of MB/s, already take a page of rows to ≈ 1/5 size; `[Zstd]` alone = 3 |
| 7–12 | few percent more ratio for several times CPU |
| 13–22 | archival, files at rest: level 20 ≈ 10 MB/s; 256 KiB reply behind it cost sending thread 25 ms in measured benchmark; agent warns above 12 |

## §16.8 Short packs go uncompressed
KW: _DefaultMaxLengthOf.Uncompressed, 1024, short pack, bound, compression taken off, warning
- RULE: agent removes compression stage from chain of pack that cannot be long, with warning naming pack and bound. Other stages stay: `[Zstd, ChaCha20]` → `[ChaCha20]`. Both sides get same chain (pack has one).
```
The pack …Ping (line 40) is 23 bytes at most: the compression Zstd is taken off it, the rest of its chain stays.
```
- NOTE: bound = longest serialized pack, counted generously: every field at declared width with room for null bit and length; string at 3 bytes per character; collection at declared or default max length (→§5); nested pack at its own bound.
- RULE: pack with no bound keeps compression: one that is or contains `Stream`/`File`; one with field having own chain or cut by trim; one containing itself. Chain containing trim keeps compression too (what cut hands over defined by stages around it).
- DEFAULT: pack ≤ 1024 bytes goes uncompressed. Change with `_DefaultMaxLengthOf.Uncompressed`; `0` compresses every pack:
```csharp
enum _DefaultMaxLengthOf {
    Uncompressed = 4096,   // packs that cannot be longer than 4096 bytes go uncompressed
}
```
- RULE: applies to packs, incl. packs taking chain of connection that guarantees packs (→§16.11.1). Chain on a field, and chain a connection runs over its stream: left as written.

## §16.9 Keys of a cipher
KW: key, nonce, session keys, 12 bytes, key id, sequence, Noise NK, X25519, ChaCha20-Poly1305, server.key, ConnectAsync, connect, connect_with_key, encryption_key, keypair, Keyring.Seat, Keyring.seat, SeatKeys
| Chain on | Key and nonce |
|:--|:--|
| pack or field, or connection that guarantees packs (chain applied per pack) | keys of the session: stage writes 12 bytes in clear in front of each pack = 4-byte key id + 8-byte sequence; pack's nonce = key's base nonce XOR sequence → every pack decrypts on its own |
| stream of a connection | runtime-injected by your code at both endpoints, once per stream |

- RT: session gets keys from handshake of its transport → connection with encrypted packs wants encrypted transport. Listening host with static X25519 key pair encrypts every connection it accepts — TCP or WebSocket, `ws://` or `wss://` alike: Noise NK handshake, then ChaCha20-Poly1305 on every record or datagram. Client pins server's public key on connect. No server key → transport not encrypted, no cost. Resulting key added with an id to session's keyring on both sides. TLS, other Noise patterns, per-connection choice →§27.
- PITFALL: generated samples of such connection use key pair made for them: replace with own, keep secret out of code.

| Runtime | Server | Client | Key pair |
|:--|:--|:--|:--|
| C# | `server.key = secret;` before listening | last argument of `ConnectAsync` | `X25519.KeyPair(secret, pub)` |
| Java | `server.key = secret;` before listening | last argument of `connect` | `X25519.keyPair(secret, pub)` |
| TypeScript | - | last argument of `connect` | `Crypto.X25519.keyPair(secret, pub)` |
| Rust | `.encryption_key(secret)` on server builder, before `build()` | `connect_with_key(server, timeout, Some(&key), …)`, `connect_encrypted(…)` or `Client::encrypted(…)` | `x25519::keypair()` → `(secret, public)` |
| Go | `server.Key = secret[:]` before listening | last argument of `Connect` | `X25519.KeyPair()` → `(secret, pub)` |
| C++ | `.encryption_key(secret)` on server builder | `connect_with_key(server, timeout, key, hook, on_connected)` or `Client::encrypted(…)` | `org::unirail::X25519::keypair()` → `{secret, public}` |
| Swift | `.encryption_key(secret)` on server builder | `connect_with_key(server, timeout, key, hook, on_connected)`, `connect_encrypted(…)` or `WebSocket.Client( encrypted: … )` | `org.unirail.X25519.keypair()` → `(secret, public)` |
| Dart | `server.key = secret;` before listening | last argument of `connect` | `X25519.keyPair(secret, pub)` |
| Python | `TCP.Server( …, key = secret )` | `WebSocket.Client.connect( url, timeout_ms, on_connected, server_key = public )` | `org.unirail.X25519.keypair()` → `( secret, public )`; `public_key( secret )`, `KEY` (32) |
| C | `adhoc·network·TCP·server·Builder·encryption_key(…)` | `adhoc·network·TCP·client·Client·connect_with_key(…)`, `adhoc·network·TCP·client·connect_encrypted(…)` | `adhoc·x25519·keypair(secret, public_key, seed, seed_n)` from OS-random `seed` |

- RT: every connection of a session adds its key to session; pack names key it was encrypted with. New packs use newest key; pack replayed from journal after reconnect decrypted with key of connection it was written on. Keys stay until session ends. Pack whose key receiver lacks (receiver lost session) dropped whole; Resumable (→§14) counts it lost anyway.
- RT: without encrypted transport session has no key → pack cipher refuses: sending throws "A pack chain with ChaCha20 on a connection without keys: encrypt the connection (a host key, a pinned server key), or add a key to its Transmitter's and Receiver's keys."
- RT: give session a key yourself, same on both sides, id ≠ 0:
```csharp
AdHoc.Connection.Keyring.Seat(connection, new AdHoc.Connection.Keyring.Key(id, key32, nonce12));   // C#
```
- NOTE: Java and TypeScript: `Keyring.seat(…)`; Go: `Connection.SeatKeys(session, &key)` with `Connection.Keyring_Key{Id, Key, Iv}`. Rust, C++, C: add key to keyring of transmitter and of receiver: Rust `keys.add(Key::new(id, key32, nonce12))`; C++ `keys->add(Keyring::Key(id, key32, nonce12))`; C `adhoc·Keyring·add(keys, adhoc·Key·new(id, key32, nonce12))`.

## §16.10 Custom transform stages
KW: custom stage, StreamCompressionStageAttribute, StreamCipherStageAttribute, StreamStageAttribute, Lz4, AesCtr, Base64, injection points, smart merge
- DECL: attribute class deriving from role base (`StreamCompressionStageAttribute` / `StreamCipherStageAttribute`) or `StreamStageAttribute` directly (no role). Constructors declare parameters in AdHoc types; apply like built-in. `Lz4Attribute` (custom compressor, design-time `level = 1`) and `AesCtrAttribute` (custom cipher, runtime key and iv) declared as in §16.5; plus:
```csharp
public class Base64Attribute : StreamStageAttribute { }        // no role: anywhere in chain
class Secured {
    [Lz4(3)]         Order p;   // design-time: level = 3
    [AesCtr]         Order q;   // key and iv supplied at runtime
    [Lz4(3), AesCtr] Order r;   // compress, then encrypt
}
```
- GEN: one file per stage type, named after attribute (`Lz4.cs`, `Lz4.java`, `Lz4.ts`): pass-through with injection points for encode/decode transform (hand-written or library call). Shared by every chain using stage; regeneration preserves your code →§25.
- NOTE: custom stage = custom attribute class, but forms a chain and changes wire bytes (other custom attributes do not). Stage, flow and trim attributes never emitted as generated constants →§23.

## §16.11 Chains on a connection
KW: connection chain, transport-level, Connects, VirtuallyConnects, hop, end to end, relay, File pack, Base64
- RT: chain on connection = transport-level: wraps everything connection carries, every pack, every stream, both directions. Same attribute list, written on connection interface. Reach follows from endpoints:

| Declared on | Scope | Runs at |
|:--|:--|:--|
| physical `Connects<L, R>` | the hop: that one link | `L` and `R` (directly linked hosts); each hop of multi-hop route needs own chain |
| virtual `VirtuallyConnects<L, R, PATH>` | end to end: whole tunnel | `L` and `R` only; every relay on `PATH` forwards transformed bytes untouched |

```csharp
[Base64, Zstd(6), ChaCha20]   // Base64Attribute : StreamStageAttribute (§16.10)
interface DirectProducerToConsumerChannel : Connects<XProducer, Consumer> {
    [_____lr_____<(SmallNamedFile, EmptyPayloadStream, Pack1, NestedFramingPack)>]
    struct Direct { }
}
```
- RT: every byte `XProducer`/`Consumer` exchange over link → custom `Base64`, compressed, encrypted, restored on arrival; packs, actors, state machine unaware.
- NOTE: stream connection chain carries `File` pack (`SmallNamedFile`) without objection: wraps transport, not packs one by one.
- RT: hop chain ends at each hop: bytes plaintext again inside host at other end (gateway in middle sees plaintext). Right for securing one link (device radio leg, LAN segment); wrong for keeping middle tier out of payload → use chain on virtual connection (endpoints = two hosts that matter). Both kinds stack →§18.
- NOTE: chain applies to connection's transport, not contents → structured connection with actors/packs takes one exactly as body-less tunnel-only virtual connection:
```csharp
[Zstd(6), ChaCha20] interface DeviceToCloud : VirtuallyConnects<Device, Cloud, Broker> { }   // sealed end to end through Broker
```
- RULE: ordinary rules hold: left = leaf, right = wire; ≤ 1 compressor, ≤ 1 cipher; connection chain may not contain trim. Cipher key/nonce on stream injected at runtime at both endpoints →§16.9.

### §16.11.1 Per-pack chains on connections that guarantee packs
KW: [UDP], Resumable<HOST, PACKS>, per-pack chain, journal, datagram, reconnect, GameLink
- RT: connection that guarantees packs (`[UDP]` connection, any connection with `Resumable<HOST, PACKS>` →§14) applies its chain to each pack, not stream. Reason: stage keeps state across packs; state does not survive lost datagram, replay from journal (holds pack as sent, compressed and encrypted) or reconnect. Agent gives connection's chain to every pack it carries as if written on pack, incl. packs left unguarded; logs which packs took it.
```csharp
[UDP(30 * 60), Zstd(3), ChaCha20]            // every pack of GameLink: compressed, then encrypted, on its own
interface GameLink : Connects<Client, Server> { /* actors and states */ }
```
- RULE: pack with own chain keeps it; agent warns when that chain lacks a stage of connection's, names missing stages.
- RULE: connection's chain becomes pack's and travels with it. Pack also on a connection not applying chain per pack → error: write chain on pack itself, or give each connection own packs. Pack carried by two per-pack connections with different chains → error (pack has one chain).
- RULE: `File` pack takes no chain → such connection cannot carry one: make it `Stream` pack, or move chain from connection onto packs.
- RULE: compression taken off surely short packs →§16.8.
- RT: cipher uses session keys, 12-byte clear prefix per pack →§16.9.

### §16.11.2 Chaining a pack or a connection you do not own
KW: Modify<>, modifier, imported pack, CompressedLog, attribute set replaced, connection modifier, SecureLink
- DECL: imported/unedited pack takes chain via modifier:
```csharp
[Zstd] class CompressedLog : Modify<ImportedLogEntry> { }
```
- RT: pack carries chain wherever sent. Agent reports substitution against target with removed and installed attribute sets:
```
The attributes of …ImportedLogEntry are replaced by the modifier …CompressedLog (line 31): [] -> [Zstd]
```
- PITFALL: modifier's attributes replace whole attribute set of target → write on same modifier every other attribute target must keep. Replaced chain is part of wire format; data stored under old chain stays bound to it →§22.
- DECL: imported connection takes chain the same way: `[Zstd, ChaCha20] interface SecureLink : Modify<ImportedConnection> { }` → its whole attribute set replaced (chain, `[Resumable]`, timeouts) →§22.3.6.

## §16.12 Transform-chain rules at a glance
KW: direction, targets, roles, composition, parameters, flows, short packs, keys, connection scale
| Rule | Short form | → |
|:--|:--|:--|
| Direction | left = leaf, right = wire; cipher left of compressor rejected | §16.1 |
| Targets | ordinary pack, named `Stream` pack, pack-typed field, bare `Stream` field, `string` field, collection of packs/strings (one stream), any connection, `Modify<>` of pack; never `File`, primitive/enum/Value Pack or collection of them; TYPEDEF drops chain | §16.2 |
| Roles | ≤ 1 compressor, ≤ 1 cipher; role-less anywhere; each stage once; unique stage names; unique parameter names | §16.4 |
| Composition | named `Stream` pack: field's stages nearer leaf, pack's nearer wire; ordinary pack's chain not merged | §16.3 |
| Parameters | with value = design-time; without = runtime-injected, any type; all reach both sides | §16.5 |
| Flows | one attribute, expands in place, stages not flows; identical chains generated once | §16.6 |
| Short packs | bound ≤ `_DefaultMaxLengthOf.Uncompressed` (default 1024; 0 = off) → compression removed, warning | §16.8 |
| Keys | per-pack: session keys + 12-byte clear prefix; connection stream: runtime-injected; `[ChaCha20]` no authentication | §16.9, §16.7.1 |
| Connection scale | `Connects<>` = hop; `VirtuallyConnects<>` = end to end; `[UDP]` / `Resumable` = per pack | §16.11 |
| Trims | | §17.14 |

# §17 Trims
KW: trim, ToStream, FromStream, Stream<To, From>, IfSendingFrom, leg, endpoint, cut, raw bytes, store, replay
SUMMARY: Trim attribute makes one side of one leg hold a payload as raw bytes instead of a typed object, so a host storing/forwarding/replaying it needs no parsing, no re-serialization, no schema. Endpoints, who holds what, generated code, strings, cut depth in chains, store-and-replay, flows, imports, diagnostics.
- Model: project → host → connection leg (sending host over one connection) → pack → field → transform chain → cut → wire bytes.
- `trim` = attribute `[ToStream<E>]`, `[FromStream<E>]` or `[Stream<To, From>]`.

| Term | Meaning |
|:--|:--|
| `leg` | one direction of one connection: sending host + connection it sends over |
| `endpoint` | declaration naming a leg: `IfSendingFrom<Host, Connection>` |
| `trim` | the attribute (`ToStream`, `FromStream`, `Stream<To, From>`) |
| `cut` | position of trim in payload's transform chain; on named leg, unwrapping stops (or starts) there |

## §17.1 Why a stored payload needs a trim
KW: pack length, parser, broker, cache, archive, symmetric chain, framing
- WIRE: serialized pack = pack id + fields back to back (→§7); no length header: only parser walking every field finds end → middle host (broker, cache, archive) cannot keep/forward/skip unparsed pack.
- RT: transform chain symmetric: receiver decrypts/decompresses in full. For host that keeps payload: wasted decrypt/decompress/re-encode per message; would need keys and schema it has no use for.
- Trim fixes both on legs where needed:
  - frames payload so end found without parsing: chunked `[len][data]…[0]` (→§15.3.1), or length prefix for string;
  - on legs it names, hands payload over as opaque bytes at chain depth you choose.
- Everywhere else field stays ordinary field, typed value both ends.

## §17.2 Naming the leg: endpoints
KW: IfSendingFrom, endpoint, endpoint set, _<…>, tuple, Pack Set, Connects<Node, Node>, Node_via_Mesh, Node_via_Mesh_R, not an Endpoint, participant
- DECL: endpoint = source host over a connection:
```csharp
// IfSendingFrom<fromHost, viaConnection>
interface FromCamera   : IfSendingFrom<Camera, CameraToRecorder> { }   // named endpoint
interface FromRecorder : IfSendingFrom<Recorder, RecorderToViewer> { }
```
- RULE: `fromHost` = host (`struct : Host`); `viaConnection` = `Connects<L, R>` or `VirtuallyConnects<L, R, PATH>`. Cut may fire on tunnel leg as on physical; tunnel relays never see payload either way →§18.
- DECL: `endpoint set` = interface derived from `_<…>` whose elements are endpoints (bare `IfSendingFrom<…>`, named endpoint, another endpoint set), alone or in tuple. `_<…>` with pack elements = Pack Set (→§9); agent tells apart by elements.
- DECL: four spellings of trim argument, all resolve to same list of legs:

| Spelling | Example |
|:--|:--|
| inline endpoint | `[ToStream<IfSendingFrom<Camera, CameraToRecorder>>]` |
| named endpoint | `[ToStream<FromCamera>]` with `interface FromCamera : IfSendingFrom<Camera, CameraToRecorder> { }` |
| named endpoint set | `[ToStream<AnySource>]` with `interface AnySource : _<(FromCamera, FromRecorder)> { }` or `interface CameraOnly : _<IfSendingFrom<Camera, CameraToRecorder>> { }` |
| tuple at point of use | `[ToStream<(FromCamera, FromRecorder)>]` |

- RULE: trim attributes leave type argument unconstrained (C# tuple could not satisfy constraint) → agent checks; other forms stop run:
```
The ToStream trim on … names '…', which is not an Endpoint. Expected `IfSendingFrom<Host, Connection>`, an interface declaring one,
a named Endpoint Set, or a tuple of those — e.g. `(FromProducer, FromBackup)`.
```
- RULE: endpoint names one leg, never a channel. `IfSendingFrom<Camera, CameraToRecorder>` = Camera → Recorder only; reverse leg Recorder → Camera keeps typed form. Trimmed field usually asymmetric on a host: Recorder takes field as raw bytes, sends same pack back to Camera typed.
- RULE: two hosts of one type: `interface Mesh : Connects<Node, Node>`: `Node` sends on both legs, as caller (Left, dialed) and acceptor (Right) →§11. `IfSendingFrom<Node, Mesh>` names both legs; recorded as two endpoints `Node_via_Mesh` (Left), `Node_via_Mesh_R` (Right). No spelling names only one.
- RULE: endpoint must be able to fire: host must be party of its connection. Non-party element skipped with warning; no usable endpoint left → run stops (cut that can never happen = error, not silent no-op):
```
WARN   The ToStream trim on … (line …) names IfSendingFrom<Viewer, CameraToRecorder>, but Viewer is not a participant of
       CameraToRecorder; that endpoint is skipped.
ERROR  The ToStream trim on … (line …) resolves to no usable endpoint, so the cut could never happen. Name an endpoint whose host
       actually participates in its connection.
```

## §17.3 The three trims and who holds what
KW: ToStream, FromStream, Stream<To, From>, AttributeUsage, sender holds, receiver holds, raw bytes, same wire, not interruptible
| Trim | When named endpoint sends… |
|:--|:--|
| `[ToStream<E>]` | sender serializes typed value; receiver stops unwrapping at cut, takes raw bytes |
| `[FromStream<E>]` | sender supplies raw bytes at cut (from storage, another socket); receiver rehydrates typed value |
| `[Stream<To, From>]` | `ToStream` on legs of `To` + `FromStream` on legs of `From`, at one position of chain |

- DECL: all three `[AttributeUsage(Field | Class, AllowMultiple = false)]`: trim written on field or pack.
- With `E = IfSendingFrom<HostA, ConnectionAB>`:

| Declaration | Leg `E`: sender holds | Leg `E`: receiver holds | Every other leg |
|:--|:--|:--|:--|
| `MyPack p;` | `MyPack` | `MyPack` | `MyPack` both ends |
| `[ToStream<E>] MyPack p;` | `MyPack` | raw bytes | `MyPack` both ends |
| `[FromStream<E>] MyPack p;` | raw bytes | `MyPack` | `MyPack` both ends |
| `[Stream<E, E2>] MyPack p;` | `MyPack` (leg `E`), raw bytes (leg `E2`) | raw bytes (leg `E`), `MyPack` (leg `E2`) | `MyPack` both ends |

- WIRE: wire same on every leg: for pack payload trim = per-leg code-generation decision, not wire format; bytes of `[ToStream<E>] MyPack p;` identical on named leg and every other. Changes which side materializes object → which side needs schema.
- GEN: payload's type given to host only where it materializes it; raw-bytes side: field brings none of payload type's code (host still gets type if something else it handles uses it).
- WIRE: exception: trimmed `string` changes encoding on cut leg →§17.6.
- RULE: trimmed payload not interruptible (one complete serialized value; truncated = torn object).
- RULE: trim orthogonal to chain: stages on same declaration run on every leg; trim only decides where unwrapping stops on named legs →§17.7.

## §17.4 Writing a trim on a field or on a pack
KW: field trim, pack-level trim, LogEntry, leg decides, targets, connection, flow, TYPEDEF
- DECL: on field → applies to that field:
```csharp
public class RecordSnapshot {
    [ToStream<FromCamera>] public Snapshot snapshot;   // still a Snapshot on every leg FromCamera does not name
}
```
- DECL: on pack → cut travels with type: every field typed with it, pack sent standalone, this project and every importing project. Use when payload never sent standalone and no single field can carry cut (many fields typed with it, or fields of packs you do not own):
```csharp
[Zstd, ToStream<FromRouter>]        // FromRouter : IfSendingFrom<Router, RouterToLogger>
public class LogEntry {
    long   at;
    string note;
}
public class ToLogger   { string tag; LogEntry entry; }   // listed on RouterToLogger
public class ToConsumer { string tag; LogEntry entry; }   // listed on RouterToConsumer
```
- RULE: leg decides, not type. Agent records pack-level cut on every field typed with pack, asks each field one leg at a time. `ToLogger.entry` crosses Router → Logger as raw, still compressed bytes; `ToConsumer.entry` typed `LogEntry` both ends of both legs of `RouterToConsumer` (compressed/decompressed as usual) although same host sends same payload: cut names logger leg, not Router.
- NOTE: one type over several legs with different needs → leave type clean, trim each carrying field. AdHoc's `FileEntry.List` carries no chain and no trim; each carrying pack states at its field what leg needs.
- RULE: trim accepted on same fields/packs as stage (→§16.2), both entries of same list; only difference: connection may carry chain, never trim.

| Target | Trim allowed? |
|:--|:--|
| field typed with pack | yes |
| bare `Stream` field, field typed with named `Stream` pack | yes →§17.9 |
| single `string` field | yes →§17.6 |
| any pack except `File` pack | yes |
| `File` field or `File` pack | no — one committed length prefix, no chunk boundary to hand bytes over; `File` payload raw bytes both ends anyway |
| primitive, enum or Value-pack field | no — written in place by every language: no framing for stage, no boundary for cut |
| array, list, `Map`, `Set`, string inside one | no — group data in pack, trim pack |
| connection, physical or virtual | no |
| TYPEDEF | silently dropped →§17.13 |

- RULE: never on connection: connection chain wraps everything link carries; "endpoint holds raw bytes instead of object" = what a relay already is. Trim on connection interface does not compile (attributes declared for fields and classes only). Flow containing trim applied to connection → agent stops run:
```
The chain on connection '…' carries a trim. A trim cuts the chain of one field or one pack; a connection chain wraps everything the link
carries, where an endpoint holding raw bytes is simply a relay. Move the trim onto the pack (or field) whose payload the endpoint should keep opaque.
```

## §17.5 What the generator produces on a cut leg
KW: scope, Transmitter, Receiver, source, sink, bytes_, _bytes_hasValue, BytesTime, WritableByteChannel, ReadableByteChannel, System.IO.Stream, abstract
- GEN: typed side of cut leg generated as any field. Raw-bytes side gets what `Stream` conduit field gets (→§15.3.4): method named after field, takes connection's `Transmitter` or `Receiver` (`scope`), returns byte source or sink. Sink fed as bytes arrive, source read as socket drains → neither side holds whole payload.
- GEN: host sending at `FromStream` endpoint → pack becomes abstract, you supply bytes. Monitoring backend `[FromStream<TelemetrySenders>] public CPU bytes;` in `BytesTime`, C# for sender:
```csharp
public abstract partial class BytesTime : AdHoc.Connection.Transmitter.BytesSrc
{
    public Monitoring.SecondsADay time { get; set; }
    protected abstract System.IO.Stream bytes_(AdHoc.Connection.Transmitter scope);   // stored bytes of one CPU sample
    protected abstract bool _bytes_hasValue(AdHoc.Connection.Transmitter scope);       // false: field is null
    …
}
```
- GEN: host receiving at `ToStream` endpoint → you supply sink. AdHoc `Project.source` (`[SrcZip, ToStream<IfSendingFrom<Agent, Communication>>] FileEntry.List source;`), Java for Server (packs as interfaces):
```java
public interface AdHocProtocol_Agent_Project extends AdHoc.Connection.Receiver.BytesDst, … {
    // where the compressed archive goes: the Server stores it and never parses it
    java.nio.channels.WritableByteChannel _source_(AdHoc.Connection.Receiver scope);
    …
}
```
- GEN: Java source = `ReadableByteChannel`, sink = `WritableByteChannel`; C# both `System.IO.Stream`.

### §17.5.1 One wire form per host and direction
KW: one wire form, serializer, parser, conflict, TWO different wire forms, carrier pack, endpoint set
- RULE: pack has one serializer and one parser per host, shared by every connection of that host; trim names a leg (finer). OK while host takes trimmed field in same form on every leg it takes it over. Same host, same direction, cut on one connection and plain on another → only one form generatable, other leg would silently read/write wrong bytes → agent refuses, draws every conflict leg by leg:
```
The protocol description is refused: 1 trimmed field of 1 pack would need TWO different wire forms on the same host ( 1 conflict ).
```
- CLI: report ends with three fixes per pack, using your packs and legs:
  1. send pack only where trimmed — remove from sets listing it on plain legs;
  2. give each leg own carrier pack — one with trim for trimmed legs, copy without for others;
  3. trim field on every leg host takes it over — widen endpoint set.
- RULE: non-string payload: only raw-bytes side can conflict (receiver of `ToStream`, sender of `FromStream`). `string` changes form on both ends of cut leg → any cut counts for sender and receiver alike.
- NOTE: store-and-replay pair whose endpoints name different sending hosts does not conflict by itself (store = receiver on one leg, sender on other).

## §17.6 Trimmed strings
KW: trimmed string, UTF-8, File framing, totalLen, [D(+N)], _DefaultMaxLengthOf.Strings, 255, invalid UTF-8, null, Zstd
- WIRE: framing: string whole in memory → length known up front → on cut leg travels in `File` framing: varint total length, then bytes. Raw side learns total before first payload byte, can refuse oversized at once. Not interruptible.
- WIRE: encoding: inside packs string = varint-encoded characters (both ends generated code). On cut leg = UTF-8, both ends of that leg (raw side hands it to non-AdHoc code: store persists as `.txt`; generated source text written to disk as it streams in).

| Declaration | Leg `E`: sender holds | wire on that leg | receiver holds | Every other leg |
|:--|:--|:--|:--|:--|
| `[ToStream<E>] string s;` | `string` | `[totalLen][UTF-8]` | raw UTF-8, total known upfront | ordinary string |
| `[FromStream<E>] string s;` | raw UTF-8 | `[totalLen][UTF-8]` | decoded `string` | ordinary string |
| `[Stream<E, E2>] string s;` | per leg, as above | `[totalLen][UTF-8]` | per leg, as above | ordinary string |

```csharp
public class RecordSubtitles {   // Camera → Recorder: store archives UTF-8 text it never decodes
    [D(+100_000), ToStream<IfSendingFrom<Camera, CameraToRecorder>>] public string subtitles;
}
public class ReplayLog {         // Recorder → Viewer: streamed from disk, received as string
    [D(+5_000_000), FromStream<IfSendingFrom<Recorder, RecorderToViewer>>] public string log;
}
```
- NOTE: AdHoc code-generation service sends each generated source file to test hosts as `[D(+5_000_000), ToStream<…>] string` → receiving host writes UTF-8 bytes to disk as they arrive.
- RULE: string's ordinary length cap governs every leg: 255 characters default, `[D(+N)]` per field, `_DefaultMaxLengthOf.Strings` per project (→§5). `[S(N)]` does not apply.
- RT: receiver of `FromStream` string validates bytes: invalid UTF-8 (overlong forms, encoded surrogates, code points past U+10FFFF) raises exception, as `[S(N)]` violation does.
- RT: `null` string not transmitted, any leg.
- WIRE: chains stack on trimmed string: `[ToStream<E>, Zstd] string s;` compresses UTF-8 bytes on cut leg; chain output length unknown upfront → chain's chunked framing replaces `File` framing there. Every other leg runs same Zstd chain with `string` both ends.

## §17.7 Cutting a chain at a chosen depth
KW: cut position, marker, depth, position 0, leaf, right of marker runs, left skipped, ChaCha20⁻¹
- DECL: trim among stages = marker picking cut depth; list in dataflow order (leaf left, wire right):
```csharp
[Zstd, ToStream<FromProducer>, ChaCha20] public class Event { long at; string topic; }
```
- RT: on named leg, everything right of marker (towards wire) still runs; everything left (remaining stages + pack's own serialization) skipped; payload crosses boundary as opaque bytes:
  - `[ToStream<E>]`: when `E` sends, receiver runs stages right of marker in reverse (here `ChaCha20⁻¹`) and stops; holds compressed bytes.
  - `[FromStream<E>]`: when `E` sends, sender injects bytes at marker; only stages right of it run.
  - `[Stream<To, From>]`: both, same depth.
- RT: chain independent of cut: every leg marker does not name runs full chain, typed both ends. Moving marker changes how much of chain cut host runs, never what other legs do.
- RULE: position 0 = leaf: marker before every stage cuts at payload itself: named-leg receiver runs whole inverse chain, stops right before materializing, holds plain serialized pack. Stage list may be empty: `[ToStream<E>] Payload p;` = cut at position 0 of no chain (plain serialized payload as opaque bytes) = everyday form.
- RULE: trim is not a transform: no role, no parameters, nothing generated for it; outside one-compressor/one-cipher limit and compressor-before-cipher rule; may stand anywhere (between stages, either end).

### §17.7.1 The depth decides what the store must run
KW: depth, store, compressed blob, wire bytes, ChaCha20, session key, MyCipher, persisted format, FromStream injection, SrcZip
- Pack chained `[Zstd, ChaCha20]`, and compressor-only chain:

| Declaration | Store keeps | Store runs | Store must hold |
|:--|:--|:--|:--|
| `[Stream<To,From>, Zstd, ChaCha20]` | plain serialized pack | whole inverse chain | key and every stage |
| `[Zstd, Stream<To,From>, ChaCha20]` | compressed blob | `ChaCha20⁻¹` only | key |
| `[Zstd, ToStream<E>]` | exactly wire bytes, still compressed | nothing | nothing |

- NOTE: no row needs payload's schema. Row 1 = plain cut (→§17.3) next to a chain.
- NOTE: row 2 = log broker: producer compresses once; broker stores and serves compressed batch, never recompresses — also on fan-out (N consumers = one compression, not N). Small disk without compressor or schema in broker.
- NOTE: row 3 = AdHoc upload: agent sends `[SrcZip, ToStream<IfSendingFrom<Agent, Communication>>] FileEntry.List source;` (`SrcZip` = flow `[Zstd]`). Agent compresses; Server's receiver runs nothing, stores archive as it came off wire. Back: `[SrcZip] FileEntry.List result;` no trim → agent decompresses, holds typed file entries.
- NOTE: why not tunnel or connection chain for a store: connection chain ends at store (store unwraps everything link carries); tunnel (→§18) passes through relay, leaves nothing to keep. Only trim lets a host that is endpoint of conversation keep payload in chosen form.
- PITFALL (WARNING): keep built-in `[ChaCha20]` right of a cut whose bytes are kept and replayed later. On pack/field, built-in cipher encrypts with newest session key of connection pack is sent on, writes key id ahead of ciphertext; receiver whose session lacks key drops pack whole. Bytes stored with `ChaCha20` still on were sealed for producer's connection; consumer on another connection cannot open them. Pack-level `ChaCha20` also needs connection with keys (encrypted connection, or keys added to transmitter and receiver →§16.9).
- PITFALL: built-in cipher cannot give a store ciphertext it holds no key for: cut right of `ChaCha20` (store keeps encrypted bytes as they came) stores bytes sealed for producer's connection → consumer on another connection drops them. Store keeping payloads it cannot read needs own cipher stage (→§16.10) whose key producer and consumer share as runtime-injected parameter, outside session keys; then cut at wire end `[Zstd, MyCipher, Stream<To, From>]` keeps exactly wire bytes, runs nothing, holds no key.
- PITFALL: bytes injected at `FromStream` must be exactly what skipped stages would have produced; generator cannot check; mismatch = decode failure at far end.
- PITFALL: trimmed bytes pinned to configuration left of cut: stages, order, design-time parameters. Change level, add stage, reorder → everything stored earlier silently stops decoding (nothing on wire announces difference). Treat left-of-cut chain as persisted format: name once in flow (like `SrcZip`); when old archives must stay readable, declare new flow beside old →§21.

## §17.8 Store and replay with trims
KW: store, replay, Stream<To, From>, one depth, different depths, ToStream and FromStream
- RT: store receives payload on one leg, replays on another. `[Stream<To, From>]` declares both cuts with one marker at one depth → received bytes byte-identical to replayed: `store(bytes)`, `replay(bytes)`, no decode/re-encode/transcoding on hot path.
- RULE: two directions at different depths refused (store would hold one form, owe another, must run stages in between). Two separate markers always = two positions, even side by side:
```csharp
[S(65_536), ToStream<FromProducer>, FromStream<FromBroker>, Zstd] Stream feed;
// ERROR: The stream chain on … puts its ToStream and FromStream trims at different depths. A store that receives one form and must
//        send another has to transform between them — which re-adds the very stages the cut removed. Put both at one depth, written as
//        a single `Stream<To, From>` marker.
```

### §17.8.1 Worked example - a schema-blind recorder
KW: Camera, Recorder, Viewer, Snapshot, RecordSnapshot, ReplaySnapshot, ToStream, FromStream, schema-blind
```csharp
using org.unirail.Meta;
namespace com.example.recording
{
    public interface Recording
    {
        /// <see cref="InCS"/>
        struct Camera : Host { }
        /// <see cref="InCS"/>
        struct Recorder : Host { }
        /// <see cref="InJAVA"/>
        struct Viewer : Host { }
        public class Snapshot
        {
            long                      taken;
            [D(65_000)] Binary[,]     jpeg;
        }
        // Camera → Recorder: typed at source, raw bytes at store
        public class RecordSnapshot
        {
            [ToStream<IfSendingFrom<Camera, CameraToRecorder>>] public Snapshot snapshot;
        }
        // Recorder → Viewer: replayed from storage, typed again at viewer
        public class ReplaySnapshot
        {
            DateTime recordedAt;
            [FromStream<IfSendingFrom<Recorder, RecorderToViewer>>] public Snapshot snapshot;
        }
        interface CameraToRecorder : Connects<Camera, Recorder>
        {
            [l____________<RecordSnapshot>]
            struct Start { }
        }
        interface RecorderToViewer : Connects<Recorder, Viewer>
        {
            [l____________<ReplaySnapshot>]
            struct Start { }
        }
    }
}
```
- RT: Camera serializes typed `Snapshot` once. Recorder receives framed bytes into sink its code supplies, persists without parsing, later replays from its source without re-serializing. Viewer rehydrates typed object. Store never depends on schema → `Snapshot` can gain fields without Recorder touched or rebuilt.
- NOTE: AdHoc monitoring backend: keeps CPU, process, network, disk telemetry samples as serialized bytes, serves with `[FromStream<…>]`. Memory and file-store samples are Value packs → travel in place (trim on Value-pack field is an error).
- NOTE: here store keeps plain serialized pack; to keep payload compressed, move cut deeper (→§17.8.2).

### §17.8.2 Worked example - a schema-blind log broker
KW: Producer, Broker, Consumer, Event, FromProducer, FromBroker, Stream<FromProducer, FromBroker>, session key
```csharp
interface FromProducer : IfSendingFrom<Producer, ProducerToBroker> { }
interface FromBroker   : IfSendingFrom<Broker,   BrokerToConsumer> { }
// stored compressed: broker decrypts the link, keeps the Zstd blob, never sees an Event
[Zstd, Stream<FromProducer, FromBroker>, ChaCha20]
public class Event
{
    long                   at;
    string                 topic;
    [D(65_000)] Binary[,]  body;
}
interface ProducerToBroker : Connects<Producer, Broker>
{
    [l____________<Event>]
    struct Start { }
}
interface BrokerToConsumer : Connects<Broker, Consumer>
{
    [l____________<Event>]
    struct Start { }
}
```
- NOTE: broker = endpoint on both legs, never a relay.
- RT: Producer → Broker: producer serializes, compresses, encrypts for this connection; broker decrypts, appends compressed bytes to log.
- RT: Broker → Consumer: broker hands back same bytes, encrypted for this connection; consumer decrypts, decompresses, rehydrates typed `Event`.
- RT: any other leg: full chain, typed `Event` both ends.
- RULE: `ChaCha20` right of cut → each leg encrypts with own connection's session key; both connections need keys (→§17.7.1 warning).
- NOTE: marker to front `[Stream<FromProducer, FromBroker>, Zstd, ChaCha20]` → recorder case: broker keeps plain serialized packs, runs whole inverse chain.
- NOTE: two endpoints name different sending hosts (Producer, Broker) → one pack cut on both legs without any host needing two wire forms →§17.5.1.

### §17.8.3 A host that passes bytes straight on
KW: Stream<FromBroker, FromBroker>, same leg, forward, bytes in bytes out, Upload, ToArchive
- RULE: both halves of `[Stream<To, From>]` may name same leg: on it sender supplies bytes, receiver takes bytes, neither materializes payload. Shape for host receiving payload as bytes and forwarding still as bytes over another connection to a store:
```csharp
interface FromProducer : IfSendingFrom<Producer, ProducerToBroker> { }
interface FromBroker   : IfSendingFrom<Broker,   BrokerToArchive> { }
public class Upload    { [Zstd, ToStream<FromProducer>]           Payload payload; }   // broker takes compressed bytes
public class ToArchive { [Zstd, Stream<FromBroker, FromBroker>]   Payload payload; }   // and hands them on: bytes in, bytes out
```
- NOTE: both cuts right of `Zstd` → bytes received exactly = bytes forwarded. AdHoc code-generation service hands archive an agent uploads to a test host this way, socket buffer to socket buffer while agent still sending.

## §17.9 Trims on named Stream packs
KW: named Stream pack, inherited trim, position, Frame, MyStage, no-op, directional
- RULE: named `Stream` pack (→§15.5) may carry chain and trim; field typed with it may add stages; compose into one chain (field's entries nearer leaf, pack's nearer wire →§16.3). Pack's trim comes to field keeping position pack gave it (pack's depth = field's cut depth). On conduit both ends hold raw bytes anyway → cut decides how much chain receiving side still runs:

| Pack declaration | `Frame frame;` | `[MyStage] Frame frame;` | Cut host |
|:--|:--|:--|:--|
| `[S(16_000_000), ToStream<FromRouter>, Zstd] class Frame : Stream { }` | `leaf → cut → Zstd → wire` | `leaf → MyStage → cut → Zstd → wire` | runs `Zstd⁻¹`, stops, keeps bytes as field's stages left them |
| `[S(16_000_000), Zstd, ToStream<FromRouter>] class Frame : Stream { }` | `leaf → Zstd → cut → wire` | `leaf → MyStage → Zstd → cut → wire` | runs nothing, keeps compressed wire bytes |

- `MyStage` = any custom stage →§16.10.
- PITFALL: cut at pack's leaf = no-op on plain conduit: row 1 without field stages → host holds same raw bytes as without trim (runs whole inverse chain either way). On conduit trim earns place only where it leaves stages un-run (row 2).
- RULE: field stage never runs beyond inherited cut: field's entries go left of whole inherited block → always on skipped side. Stage that must run past cut belongs on pack, right of marker.
- RULE: being cut makes field directional on named legs, as if marker written on field.
- RULE: `File` pack passes nothing down: neither stage nor trim. To cut/transform conduit, inherit `Stream`.

## §17.10 Trims inside flows
KW: flow, StreamFlowAttribute, StoreCompressed, one trim, pins, cuts the receiving side twice
- RULE: flow (→§16.6) may contain one trim; expands in place keeping position flow gave it (= writing entries by hand):
```csharp
[Zstd(6), ToStream<FromProducer>, ChaCha20]
public class StoreCompressed : StreamFlowAttribute { }
[StoreCompressed] Payload payload;
//  →  leaf → Zstd(6) → |cut ToStream(Producer@ProducerToBroker)| → ChaCha20 → wire
```
- PITFALL: flow of stages = neutral local transform applicable anywhere. Flow with trim carries routing decision: every application cut on flow's endpoints, every field/pack using it directional on those legs. Right when flow is pipeline of one store-and-replay path; as general chain it pins unrelated payloads. Prefer trims at point of application, flows = stages.
- RULE: chain rules checked after expansion on flattened list → flow's trim collides with one written next to it:
```csharp
[StoreCompressed, ToStream<FromProducer>] Payload payload;
// ERROR: The stream chain on '…payload' cuts the receiving side twice. At most one ToStream trim per chain — one payload cannot be
//        handed over at two depths at once.
```
- RULE: same expansion on named `Stream` pack: field typed with it inherits flow's entries whole, trim and position included.

## §17.11 Trims across imported projects
KW: import, composed project, IfSendingFrom, RecordingWithArchive, Modify<TargetPack>, extension, connection modifier
- RULE: trim part of its declaration → importing project (→§22) brings it with pack/field; nothing redeclared. Import can change whether named leg still exists.
- RULE: both halves of `IfSendingFrom<host, connection>` resolve against composed project (root description + everything it extends) → trim may name imported host over connection the extension declares:
```csharp
// Recording.cs - project of the recorder example (§17.8.1)
// RecordingWithArchive.cs
public interface RecordingWithArchive : Recording
{
    /// <see cref="InJAVA"/>
    struct Archive : Host { }                                    // new host
    interface CameraToArchive : Connects<Camera, Archive>       // Camera is imported
    {
        [l____________<ArchiveSnapshot>]
        struct Start { }
    }
    public class ArchiveSnapshot
    {
        [ToStream<IfSendingFrom<Camera, CameraToArchive>>] public Snapshot snapshot;
    }
}
```
- NOTE: AdHoc monitoring backend built this way: extension of AdHoc's protocol whose trims name imported `Server` host over virtual connection the extension declares, via named endpoint set.
- RULE: cut that can never fire = error → imported pack whose trim names connection importing project leaves out must be dropped from composition (not merely ignored).
- DECL: modifier re-declares cut of pack you do not own: `Modify<TargetPack>` with attributes replaces target's whole attribute set, chain and trims together → extension states depth and legs in one place. Intended way to cut imported pack on leg its author could not know. Which modifier wins when several modify one target →§22.
- PITFALL: after replacement, store still injects bytes kept un-parsed and un-recompressed → replacing chain restates format stores already hold: keep stages left of cut as they were (mismatch not announced on wire).
- NOTE: connection modifier (`Modify<Connection>`) with attributes replaces attribute set of connection (its chain), not trims of its packs →§22.3.6.

## §17.12 What the agent reports about trims
KW: cut field, Host@Connection, stream chain, |cut ToStream(...)|, ~idx, Value pack warning, StreamTrimAttribute, Custom trims are not supported
- CLI: one line per cut field naming legs as `Host@Connection`:
```
cut field com.example.recording.Recording.RecordSnapshot.snapshot: ToStream[Camera@CameraToRecorder] FromStream[]
```
- CLI: one line per chain, declaration order, cut where written:
```
stream chain on …payload (line 42): leaf -> Zstd(level=6)[compression] -> |cut ToStream(Producer@ProducerToBroker)| -> wire  =>  container #3
```
- NOTE: these lines = first place a cut firing on wrong leg becomes visible.
- NOTE: internally each endpoint recorded as index of its connection: index itself when sender is connection's Left host, bitwise-inverted (`~idx`) when Right.
- CLI: warning: trimmed field whose payload small enough to be Value pack but for its null bits (framing costs more than sending): `The trimmed field '…' (line: …) hands over '…', which is small enough to be a Value pack — streaming it costs more than transmitting it.`
- RULE: class derived from `StreamTrimAttribute` refused (custom trims not supported): `'…' derives from StreamTrimAttribute but is not one of the recognized trim markers (ToStream<E>, FromStream<E>, Stream<To, From>) … Custom trims are not supported.`
- RULE: two chain rules involving trims (→§16.4, →§16.8): chain containing trim → no stage named `ToStream`/`FromStream`; chain containing trim keeps compression even on short pack.

## §17.13 Trim pitfalls
KW: TYPEDEF, stages left of cut, FromStream injection, ChaCha20, flow pins
- PITFALL: TYPEDEF drops trim silently: propagates type and caps, neither trim nor chain → field becomes ordinary nested pack. Reuse cut via flow or named `Stream` pack, never alias →§5.
- PITFALL: changing stages left of a cut breaks stored data silently →§17.7.1.
- PITFALL: bytes injected at `FromStream` not checked; mismatch with skipped stages = decode failure at far end.
- PITFALL: built-in `ChaCha20` left of replayed cut seals stored bytes for one connection.
- PITFALL: flow with trim pins every use to its endpoints.

## §17.14 Trim rules at a glance
KW: forms, endpoint argument, legs, resolution, position, count, role, targets, wire, strings, hosts, flows, TYPEDEF, import, persisted format
| Rule | Short form | → |
|:--|:--|:--|
| Forms | `[ToStream<E>]`, `[FromStream<E>]`, `[Stream<To, From>]` on field or pack; no custom trims | §17.3, §17.12 |
| Endpoint argument | `IfSendingFrom<Host, Connection>`, named endpoint, named endpoint set, tuple; else error | §17.2 |
| Legs | one leg per endpoint; reverse and other legs typed; same-type hosts → both legs | §17.2 |
| Resolution | composed project; non-party element skipped (warning); no usable endpoint = error | §17.2, §17.11 |
| Position | right of marker runs, left skipped on named leg; empty list = cut at leaf; full chain elsewhere | §17.7 |
| Count | ≤ 1 `ToStream`, ≤ 1 `FromStream` per chain after flow expansion; same depth → one `Stream<To, From>` | §17.8, §17.10 |
| Role | none: no parameters, nothing generated, outside compressor/cipher rules | §17.7 |
| Targets | pack-typed, `Stream`, single `string` field, any non-`File` pack; never `File`, primitive, enum, Value-pack, collection; never connection (modifier or not) | §17.4 |
| Pack-level trim | every field typed with pack + pack standalone; each field cut only on named legs | §17.4 |
| Wire | same bytes every leg for pack payload; trimmed `string` = UTF-8 in `File` framing on cut leg | §17.3, §17.6 |
| Strings | own cap (`[D(+N)]`, default 255); no `[S(N)]`; invalid UTF-8 → exception; `null` not sent | §17.6 |
| Interruptibility | trimmed transfer not interruptible | §17.3 |
| Hosts | one wire form per host and direction; conflict = error | §17.5.1 |
| Flows | ≤ 1 trim; expands in place; pins every application | §17.10 |
| TYPEDEF | drops trim and chain silently | §17.13 |
| Import | trim travels with declaration; `Modify<Pack>` with attributes replaces chain and trims | §17.11 |
| Persisted format | stored bytes valid only for exact stages, order, design-time params left of cut | §17.7.1 |

# §18 Virtual connections and relays
KW: VirtuallyConnects, virtual connection, tunnel, Relay, relay host, PATH, carrier, tunnel_id, ABORT
SUMMARY: `VirtuallyConnects<L,R,PATH>` = connection between hosts with no direct link; bytes ride a chunked tunnel over physical legs through relay hosts. Generated Relay forwards without decoding. Route finding, MaxTunnels, wire frame, generated relay/endpoint code, closing, chains, choosing a middle-tier role.
- `virtual connection` = joins two hosts sharing no direct link; they behave as directly connected (same packs, actors, state machines as any connection →§11) while bytes physically cross one or more `relay hosts`. `tunnel` = bytes of one virtual connection between its two hosts. Generated `Relay` on each relay host forwards tunnel without decoding.
- NOTE: typical uses: browser dashboard ↔ backend reachable only via gateway; device behind NAT via rendezvous broker; sensor whose bytes must pass aggregation tier.
- NOTE: vs naive bridge (middle host deserializes + reserializes each pack: two full passes, heap object as large as message, middle host rebuilt on every pack change): relay forwards tunnel bytes without decoding, buffering or knowing structure; moves payload of any size at constant memory; not rebuilt when packs inside change.
- NOTE: builds on chunked framing of Streams →§15 (`[len][data]…[0]`, 2-byte chunk length, `0xFFFF` = `ABORT` ends stream as void, max `0xFFFE` bytes/chunk), Transform chains →§16, Connections →§11 (`External`/`Internal`, connection events, close kinds).

## §18.1 Declaring a virtual connection
KW: VirtuallyConnects<L, R, PATH>, Connects<L, R>, PATH, MaxTunnels, MaxStream_KiloBytes, IfSendingFrom, Resumable
- DECL: like a connection, `VirtuallyConnects` instead of `Connects`. Meta.cs:
```csharp
public interface VirtuallyConnects<L, R, PATH> : Connects<L, R>
    where L : struct, Host
    where R : struct, Host
{
    int  MaxTunnels          => 256;            // how many tunnels may be multiplexed over the path at once
    uint MaxStream_KiloBytes => uint.MaxValue;  // the largest stream one tunnel may carry, in KiB
}
```
- `L`, `R` = two logical endpoints (behave as directly connected). May be one type: two hosts of that type, each sends and receives every pack, as physical `Connects<A, A>` →§11.4.
- `PATH` = relay host(s) bytes physically pass: one host (single hop) or C# tuple `(H1, H2, …)`. No C# constraint on `PATH`; agent checks it →§18.3.
- RULE: extends `Connects<L,R>` → accepted wherever a connection is: first base of a connection interface; named by trim endpoint `IfSendingFrom<Host, Connection>` →§17; counts in "pair of hosts joined by at most one connection" →§11.
- NOTE: actor, states, deadlines, group work exactly as on physical connection →§13. Body may declare `Resumable<HOST, PACKS>` for guaranteed delivery →§14.
- Example: viewer subscribes to server events, reachable only via hub:
```csharp
///<see cref = 'InJAVA'/>
///<see cref = 'InCS'/>
///<see cref = 'InTS'/>
struct Server : Host { }
/// The relay between a Viewer and the Server.
///<see cref = 'InJAVA'/>
///<see cref = 'InCS'/>
///<see cref = 'InTS'/>
struct Hub : Host { }
///<see cref = 'InJAVA'/>
///<see cref = 'InCS'/>
///<see cref = 'InTS'/>
struct Viewer : Host { }
class Beat       { long stamp; }
class WatchSub   { int what; }
class WatchEvent { int what; long value; }
class WatchUnsub { int what; }
interface HubToServer : Connects<Hub, Server>      // physical leg
{
    [_____lr_____<Beat>]
    struct One { }
}
interface ViewerToHub : Connects<Viewer, Hub>      // physical leg
{
    [_____lr_____<Beat>]
    struct One { }
}
// a connection that rides a tunnel through the Hub: the Viewer subscribes on the Server
interface ViewerToServer : VirtuallyConnects<Viewer, Server, Hub>
{
    [Idle(120)] // a lease: the Viewer renews it with WatchSub; a Viewer gone without a word is let go
    [Grouped]   // the Server gathers the watches of all Viewers, of all its Hubs, to send one event to all of them
    interface Watch : Actor
    {
        int Actor.MaxActiveInstances => 4;
        [ReceiveTimeout(60)]
        [L____________<Live, WatchSub>]
        struct Start { }
        [____________r<WatchEvent>]
        [l____________<WatchSub>]
        [L____________<End, WatchUnsub>]
        struct Live { }
    }
}
```
- NOTE: every byte of `ViewerToServer` rides legs `ViewerToHub` + `HubToServer`, relayed by `Hub`.
- GEN: relay host gets relay field per virtual connection on receiver of each physical leg + Provider class whose `Tunneling` region maps `tunnel_id` → outbound connection. Endpoints get carrier connection with tunnels table, rx/tx demux, send function, `Tunnel` and `Tunnel end` regions; virtual connection's Connection class built on `Tunnel.EndPoint` (actors, packs, chain) →§18.6.

## §18.2 Tunnel-only and structured virtual connections
KW: empty body, tunnel only, structured, raw bytes, Relay, foreign protocol
| Body | Generated on `L`,`R` | On each `PATH` host | Travels in tunnel |
|:--|:--|:--|:--|
| empty `{ }` | tunnel endpoint API: raw opaque bytes | `Relay` | raw bytes, structure unknown to relay and generated code |
| actors, branches, packs (as in `Connects`) | full connection: packs, actors, state machines | `Relay` | serialized packs of that connection |
- DECL: empty body = tunnel only; only transport generated. Use when `L`,`R` have own framing or carry foreign protocol:
```csharp
/// <see cref="InCS"/>
struct Device : Host { }
/// <see cref="InJAVA"/>
struct Broker : Host { }
/// <see cref="InJAVA"/>
struct Cloud : Host { }
class Ping { long stamp; }
interface DeviceToBroker : Connects<Device, Broker> { [_____lr_____<Ping>] struct One { } }
interface BrokerToCloud  : Connects<Broker, Cloud>  { [_____lr_____<Ping>] struct One { } }
interface DeviceTunnel : VirtuallyConnects<Device, Cloud, Broker> { }   // a raw byte pipe Device ↔ Cloud
```
- RULE: "connection needs ≥1 actor with ≥1 state" applies to physical connections only → empty body valid.
- NOTE: non-empty body = ordinary end-to-end connection riding tunnel; endpoints speak full protocol; relays carry opaque bytes, never parse (`ViewerToServer` →§18.1).

## §18.3 How the agent finds the route
KW: PATH, route, physical legs, cannot be routed, empty PATH, visits the host twice, ambiguous, Modify<Host>, routing loop
- RULE: `PATH` lists intermediate hosts in any order. Agent rebuilds strict route `L → … → R` over graph of physical `Connects<>`: the one chain starting at `L`, ending at `R`, visiting every listed host exactly once. `VirtuallyConnects<A, D, (C, B)>` = `VirtuallyConnects<A, D, (B, C)>` when `A → B → C → D` is the only physical chain.
- Two-hop example, structured body, single tunnel:
```csharp
// physical legs: Connects<XProducer, ZRouter>, Connects<ZRouter, ZAuditor>, Connects<ZAuditor, WSink>
[Zstd(6), ChaCha20]
interface Producer__Sink_Structured : VirtuallyConnects<XProducer, WSink, (ZRouter, ZAuditor)>
{
    int VirtuallyConnects<XProducer, WSink, (ZRouter, ZAuditor)>.MaxTunnels          => 1;       // no tunnel_id on the wire
    uint VirtuallyConnects<XProducer, WSink, (ZRouter, ZAuditor)>.MaxStream_KiloBytes => 64000;
    [_____lr_____<(EndpointFormsPack, DateTimeOptimizationPack)>]
    struct Exchange { }
}
```
- NOTE: `(ZAuditor, ZRouter)` = same tunnel. Each of `ZRouter`, `ZAuditor` gets a Relay, chained; neither knows the connection inside.
- RULE: route uses physical legs only: virtual connection cannot ride another virtual connection. Host not in `PATH` not routed through, even if physical leg passes it. `PATH` entry that is host modifier (`Modify<Host>`) stands for the host it modifies.
- RULE: run stops on failure:
| Rule | Agent message |
|:--|:--|
| every hop `L → PATH₁ → … → R` is real physical `Connects<>` | `The virtual connection … cannot be routed` + physical legs that exist among listed hosts |
| ≥1 intermediate host | `… has an empty PATH. At least one intermediate host is required.` |
| every `PATH` element is a declared host | `… lists … in PATH, but that is not a host. PATH names the relay hosts the tunnelled bytes physically pass through: every element has to be a struct : Host declared in this project.` |
| `L`, `R`, every `PATH` host appear once; `L = R` only as two hosts of one type at the two ends | `The virtual connection … visits the host … twice` (repeated host = routing loop competing for same `tunnel_id` demux state on that host). Not a repeat: `Peer → Relay → Peer` routed over `PeerToRelay` twice |
| route unique | `The virtual connection … is ambiguous` + two routes; adjust `PATH` or topology |
| no other connection (physical or virtual) joins `L`,`R` in either order | `The hosts … and … are joined by more than one connection: …` →§11 |
- RULE: parallel conversations between same two hosts → one virtual connection, raise `MaxTunnels`; not a second declaration, not repeating a host in `PATH`.

## §18.4 MaxTunnels and MaxStream_KiloBytes
KW: MaxTunnels, MaxStream_KiloBytes, tunnel_id width, compile-time constant, must be a positive value
| Property | Default | Meaning |
|:--|:--|:--|
| `MaxTunnels` | `256` | max independent tunnels multiplexed over path at once. Sets width of `tunnel_id` routing key: `1` → no `tunnel_id` (one tunnel); larger → little-endian key of 1–4 bytes, as many as `MaxTunnels - 1` needs (1 byte for 256, 2 bytes for 65 536). ≥ 1 |
| `MaxStream_KiloBytes` | `uint.MaxValue` (unbounded) | largest stream one tunnel may carry, KiB; tunnel counterpart of `[S(N)]` on stream field |
- DECL: override in body, short form or explicit interface implementation:
```csharp
// one of the two, not both: a pair of hosts may be joined by one connection only
interface DeviceLink : VirtuallyConnects<Device, Cloud, Broker>
{
    int  MaxTunnels          => 1;       // a single dedicated tunnel: no tunnel_id on the wire
    uint MaxStream_KiloBytes => 4_096;   // each tunneled stream up to 4 MiB
}
interface DeviceLinks : VirtuallyConnects<Device, Cloud, Broker>
{
    int  VirtuallyConnects<Device, Cloud, Broker>.MaxTunnels          => 512;
    uint VirtuallyConnects<Device, Cloud, Broker>.MaxStream_KiloBytes => 100_000;
}
```
- PITFALL: agent reads property by name, takes value only if compile-time constant; non-constant expression skipped silently, default stays.
- RULE: `MaxTunnels` < 1 stops run: `The virtual connection … (…) sets MaxTunnels to …: it has to be 1 or more.`
- NOTE: `MaxTunnels => 1` → virtual connection = member of its carrier `connection.<V>`, no tunnel id, no factory →§18.6.2.

## §18.5 The tunnel on the wire
KW: tunnel frame, burst, pack-id, tunnel_id, chunk, 0xFFFE, 0x0000, ABORT, 0xFFFF, wire id
- WIRE: no frame per pack. Virtual connection with packs to send enqueues itself once on transmitter of physical connection; on its turn serializes its packs as one `burst` = one tunnel frame; frame ends when virtual connection has nothing more in flight. Burst ends only between two packs. Next packs → new frame; frames of other tunnels and ordinary packs of physical connection interleave between frames.
- WIRE: frame:
```
[pack-id][tunnel_id?]   [len₁][data₁]  [len₂][data₂] … [0]
```
- WIRE: `pack-id` = wire id of virtual connection: persistent id from same pool as transmittable packs, recorded after its name in `connections` section of numbers tables (e.g. `<see cref='Server__MonitoringObserver'/>ā 124`) →§20.4.4. Tunnel frame told from pack by id like any pack. Id collision with a pack → renumbered with warning; hand-rename (not IDE) of virtual connection stops run until name corrected →§20.6.
- WIRE: `tunnel_id` = fixed-width little-endian field right after pack id, before chunked body; width from `MaxTunnels` (0–4 bytes); absent with `MaxTunnels => 1`.
- WIRE: body chunked: 2-byte length before each chunk, ≤ `0xFFFE` bytes/chunk, `0x0000` at end.
- WIRE: frame that ends tunnel ends with `ABORT` (`0xFFFF`) instead of `[0]`, after last chunks of a burst or alone:
```
[pack-id][tunnel_id?]   [len₁][data₁] … [ABORT]          or just          [pack-id][tunnel_id?][ABORT]
```
- WIRE: relay re-framing →§18.7: inbound cut into regions at socket-read ends; source chunk headers = holes; outbound = fresh pack id + tunnel_id, then each region as own chunk skipping holes, +2 bytes/region.

## §18.6 What the generator produces for a tunnel
KW: Relay, PATH host, endpoint code, wire id, numbers tables
- GEN: one `VirtuallyConnects` → relay on every `PATH` host; endpoint code on `L` and `R`; agent gives virtual connection its wire id in numbers tables.

### §18.6.1 On a relay host: the Relay and its router
KW: Relay, relay124, Provider_..._External, Connection.Tunnel.Relay.OutboundSrc, outbound, Tunneling, sending_put, noCustomCodeAlarm, NotImplementedException, Gone, OnClose, route_<V>·set_hook, adhoc·panic, Relay.<Tunnel>.route, route( None ), Provide the logic
- GEN: on receiver of each physical leg a tunnel can arrive on: one `Relay` per relayed virtual connection, in field named after virtual connection's wire id (`relay124`), + router class named after virtual connection. Receiver hands each tunnel frame (by pack id) to relay instead of deserializer. C# for `Monitoring` (relay host of AdHoc monitoring backend), wire id 124, abridged:
```csharp
class Provider_Server__MonitoringObserver_External : Connection.Tunnel.Relay.OutboundSrc
{
    public Connection.External? outbound(Connection.Tunnel.Relay dst, int tunnel_id)
    {
        Connection.External? ret =
        //Provide logic returning the `External` part of the 'ServerToMonitoring' that serves tunnel 'Server__MonitoringObserver' inside #region,
        //or null when no live connection serves it - the relay then drops the packet.
        //Use org.unirail.Network.TCP.GetConnection() if necessary.
        //IMPORTANT: End your code with line 'if(false)' to disarm the trap.
#region> Tunneling
            ServerToMonitoring.Connection.one.ext_connection;        // your code: the connection that serves tunnel_id
        if( false )
#endregion> ÿā.Tunneling
            noCustomCodeAlarm();
        return ret?.Internal is org.unirail.ServerToMonitoring.Connection __internal && __internal.transmitter.sending_put(dst) ? ret : null;
    }
    private Connection.External? noCustomCodeAlarm() => throw new NotImplementedException("Provide logic returning the `External` part of the 'ServerToMonitoring' that serves tunnel 'Server__MonitoringObserver' (or null) inside #region");
    public static readonly Provider_Server__MonitoringObserver_External one = new();
}
internal Connection.Tunnel.Relay? relay124 = new Connection.Tunnel.Relay();
// in the receiver's dispatch by pack id:
case 124:
    return relay124.Init(_id, 1, 1, connection.ext_connection, Provider_Server__MonitoringObserver_External.one);
```
- RT: contract of `outbound`: your code in region `Tunneling` returns `External` of connection serving `tunnel_id` (opposite leg) or `null` when none live (closed, closing, never connected). May rewrite `dst.tunnel_id` to re-key outbound stream.
- RT: generated tail enqueues relay on that connection's transmitter (`sending_put`). On `null` or refused enqueue (queue full, connection closing) → relay consumes packet body and drops it; inbound connection keeps running.
- PITFALL: `null` / refused enqueue = designed outcome, not an error: do not "fix" a `null` with a fallback connection.
- NOTE: `tunnel_id` = routing key of your application, not a connection id →§18.7.
- PITFALL: unfilled region throws the `NotImplementedException`; end your code with `if( false )` to disarm (regions →§25).
- RT: router stateless: one instance `one` serves all relays of that direction.
- GEN other languages: same contract, same region `Tunneling`; unfilled region → language's trap: Java `RuntimeException`, TS `Error`, Dart `StateError`, Go and Rust `panic`, C++ `std::logic_error`, Swift `org.unirail.AdHoc.panic`, C `adhoc·panic`; text "Provide … logic returning … inside … region".
- GEN C: route of tunnel `V` relayed from connection `C` = function `gen·<C>·route_<V>`, region `Tunneling` at its top. Program driving the C code from another language sets route instead: `gen·<C>·route_<V>·set_hook( hook )`; `hook( tunnel_id )` → connection on far leg serving the tunnel (new reference, released by route) or `NULL` (relay drops pack). Empty region + no hook → `adhoc·panic` (as `noCustomCodeAlarm` elsewhere; by design).
- GEN Python: `<Carrier>.Relay.<Tunnel>.route( handler )`; `<Carrier>` = module of connection the tunnel comes from; `handler( tunnel_id )` → `<FarCarrier>.Connection` serving the tunnel, or `None`; `route( None )` removes handler. RT: handler runs on receive thread of the C core. `None` → pack dropped, connection goes on (designed outcome). Exception of handler, or return of another type → reported (`sys.unraisablehook`), pack dropped. No handler → text "Provide the logic … `<Carrier>.Relay.<Tunnel>.route( handler )`" to `onFailure` (`org.unirail.Network.on_failure`), pack dropped, process does not stop.
```python
served_by_ViewerToHub: dict[ int, object ] = { }   # tunnel_id -> the ViewerToHub connection that serves it, filled as they connect
HubToServer.Relay.ViewerToServer.route( lambda tunnel_id: served_by_ViewerToHub.get( tunnel_id ) )   # None: the relay drops the pack
```
- RT: physical connection's `OnClose` calls `Gone()` on each of its relays → far ends of fed tunnels are told →§18.8. Multi-hop `PATH` → Relay on every relay host, chained.

### §18.6.2 On an endpoint host: the carrier
KW: carrier, tunnels_<VC>, rx_<VC>, tx_<VC>, send_<VC>, close_tunnels_<VC>, Tunnel.of, Tunnel.ended, tunneling_via, Tunnel.EndPoint, Tunnel end, current_by_tunnel_id, THIS_CLOSE_ABRUPTLY, has no carrier, REJECTED, on_failure, MaxTunnels => 1, one tunnel, connection.<V>, viewer_to_server, enable_shared_from_this, REMOTE_CONNECT
- `carrier` = physical connection the tunnel frames arrive on, at an endpoint. Carrier keeps virtual connections of its tunnels; no other tunnel table needed.
- GEN (Java, C#): added to carrier (here `ServerToMonitoring` carrying `Server__MonitoringObserver`; Java, abridged):
```java
// the virtual connections this carrier carries, by tunnel id: made on a tunnel's first frame, closed with this carrier
public final IntObjectMap.RW<Server__MonitoringObserver.Connection> tunnels_Server__MonitoringObserver = …;
public final Server__MonitoringObserver.Connection.Tunnel.Rx rx_Server__MonitoringObserver = …; // inbound demux of this carrier
public final Server__MonitoringObserver.Connection.Tunnel.Tx tx_Server__MonitoringObserver = …; // packs the replies onto this carrier
public static boolean send_Server__MonitoringObserver(Server__MonitoringObserver.Connection.Tunnel src, int tunnel_id, Connection conn) { … }
public void close_tunnels_Server__MonitoringObserver() { … }  // THIS_CLOSE_ABRUPTLY + OnClose for each, then forget them
```
- GEN: name suffix = virtual connection's name. `rx_<VirtualConnection>` = one demux per carrier (never shared by two carriers). `tx_<VirtualConnection>` shared by tunnels of one carrier (carrier transmitter writes one pack at a time).
- GEN: virtual connection's class: `<VirtualConnection>.Connection.Tunnel` extends `AdHoc.Connection.Tunnel.EndPoint`; holds `tunnel_id` and carrier (`tunneling_via`); static `Tunnel.of(tunnel_id, carrier)` = virtual connection of that tunnel on that carrier, created on first use, kept in carrier's table; `Tunnel.ended(tunnel_id, carrier, whole)` = closes it after far end ended tunnel.
- RT: on virtual connection far-end identity = tunnel id: endpoint's `id` is its `tunnel_id`.
- GEN: `side( tunnel_id, carrier ) -> remote`, only `VirtuallyConnects<X, X, …>` with `MaxTunnels` > 1, on `Tunnel` type: `false` = left, `true` = right. RT: set → answer replaces `remote` when new tunnel made, by own open or by first frame (host left by rule stays left when first frame beats own open); called once per tunnel at creation, no work on pack path; not set → connect event chooses (`THIS_CONNECT` left, `REMOTE_CONNECT` right). Rule must answer differently at two ends, from own address and `tunnel_id`:
```java
Connection.Tunnel.side = ( tunnel_id, carrier ) -> my_address > tunnel_id; // Java, C#: lesser address = left
```
| Language | Set |
|:--|:--|
| C#, Java | static field `Connection.Tunnel.side`; `null` = not set |
| TypeScript | static field `Tunnel.side`; `undefined` = not set |
| Dart | static field `Connection_Tunnel.side`; `null` = not set |
| Go | `Connection_Tunnel_side( fn )`; `nil` = not set; `carrier` = connection's `Tunnels_<VirtualConnection>` |
| Rust | `Tunnel::side( f )`: once, before first connection; second call changes nothing |
| Swift | `Tunnel.side( f )`; `nil` = not set |
| C++ | `Tunnel::side( fn )`; empty = not set |
| C | `gen·<VirtualConnection>·Tunnel·side( fn, ctx )`, before first connection; `fn( tunnel_id, carrier, ctx )`; `NULL` = not set |
| Python | `Tunnel.side( fn )`; `None` = not set; exception of `fn` reported, tunnel takes left side |
- GEN one tunnel (`MaxTunnels => 1`): no tunnel id, no `Tunnel.of` / `Tunnel.open`, no table: virtual connection = member of its carrier, named after the virtual connection:
| Language | Member of the carrier |
|:--|:--|
| C# | property `connection.ViewerToServer` |
| Java | method `connection.ViewerToServer()` |
| Go | method `connection.ViewerToServer()` |
| TypeScript | getter `connection.ViewerToServer` |
| Dart | getter `connection.ViewerToServer` |
| Rust | `connection.viewer_to_server()` (`self: &Arc<Self>`) → `Arc<Connection>` |
| Swift | property `connection.viewer_to_server` |
| C++ | `carrier->ViewerToServer( )` → `std::shared_ptr< ViewerToServer::Connection >`; carrier of a one-tunnel connection derives from `std::enable_shared_from_this`, must be made with `std::make_shared` |
| C | `gen·<Carrier>·ViewerToServer( carrier )` = new reference, released with `gen·ViewerToServer·Connection·release` |
| Python | attribute `connection.ViewerToServer` |
```csharp
var tunneled = connection.ViewerToServer;                     // the one tunnel of ViewerToServer on this carrier
var actor    = ViewerToServer.Watch.Acquire( tunneled );
if( actor != null ) ViewerToServer.Watch.State.O.transmitter.send( new Viewer.WatchSub(), tunneled, actor );
```
- RULE: member name = virtual connection's name, snake_case in Rust and Swift; taken on carrier (own member or actor of carrier) → `_` appended until free.
- RT one tunnel, every language: made at first use of member (`THIS_CONNECT`) or first frame from far end (`REMOTE_CONNECT`); later uses → same one; tunnel ends → carrier forgets it, next use makes new one; carrier's close closes tunnel (`THIS_CLOSE_ABRUPTLY`, then `OnClose`), after reconnect a new tunnel works; on far end frames go to same virtual connection `carrier.<V>` gives there; end of tunnel from other side closes it with `REMOTE_CLOSE_GRACEFUL` / `REMOTE_CLOSE_ABRUPTLY`.
- RT: send on virtual connection without carrier (made on its own, not as tunnel of a carrier; take it from carrier: `carrier.<V>` one tunnel, `Tunnel.of` / `Tunnel.open` many), every language: transmitter reports to its error handler (`onFailure`) error `REJECTED`, text `virtual connection X has no carrier` (X = virtual connection's name); pack not sent; `send` returns `false`; nothing thrown. Handler: `AdHoc.Connection.Transmitter.error_handler` (C#, Java, TS), `AdHoc.Connection_Transmitter.error_handler` (Dart), `Transmitter_.Error_handler` (Go); transmit error hook: Rust `connection::transmitter::on_error::tx`, C++ `tx::error`, Swift `org.unirail.AdHoc.Connection.Transmitter.OnError.tx`, C `adhoc·connection·transmitter·on_error·tx`. Python: one handler of the process `org.unirail.Network.on_failure( handler )`, called `handler( source, error )`; no handler → text printed on console (`org.unirail.Network.onFailurePrintConsole`).
- GEN: carrier demux has two regions with generated defaults:
```java
public void accept(int tunnel_id)          // a frame of tunnel `tunnel_id` arrived
{
    Tunnel current_by_tunnel_id = null;
//#region > Tunnel
    current_by_tunnel_id = Tunnel.of(tunnel_id, carrier);
//#endregion > ÿā.Tunnel
    …
}
public void ended(int tunnel_id, boolean whole)   // the far end ended tunnel `tunnel_id`
{
//#region > Tunnel end
    Tunnel.ended(tunnel_id, carrier, whole);
//#endregion > ÿā.Tunnel.end
}
```
- RULE: replace default in `Tunnel` when your code resolves tunnel ids; resolver leaving `current_by_tunnel_id` null fails: `A custom resolver that maps tunnel_id to a Tunnel instance current_by_tunnel_id must be provided.`
- RT: carrier's close ends its tunnels: its `OnClose` calls `close_tunnels_<VirtualConnection>()` → each virtual connection hears `THIS_CLOSE_ABRUPTLY`, then `OnClose()`; actors released as on any connection.
- RT: deadlines work through tunnel: actor instance of virtual connection keeps `[Deadline]`/`[Idle]` on carrier, whose loop times them →§13.
- RT: replies go back the way frames came (virtual connection knows its carrier).
- RT: when frames on a carrier start coming from other sources (carrier took another socket) → call `close_tunnels_<VirtualConnection>()` yourself on carrier's loop; earlier sources' virtual connections closed and forgotten; next frame opens new tunnel.
- RT: on endpoint virtual connection addressed by `tunnel_id` when `MaxTunnels > 1`; `MaxTunnels => 1` → member `carrier.<V>`.

### §18.6.3 TypeScript tunnel endpoints
KW: TypeScript, Dart, Tunnel extends AdHoc.Connection.Tunnel.EndPoint, Tunnel.of, Connection_Tunnel.of, WeakMap, Expando, connected( remote ), send_<VC>, Tunnel end, closed(event)
- GEN (TS): tunnel endpoint class (`Tunnel extends AdHoc.Connection.Tunnel.EndPoint`), `send_<VirtualConnection>` on carrier, same `Tunnel` and `Tunnel end` regions, `Tunnel.of( tunnel_id, carrier, remote = false )` = virtual connection of tunnel `tunnel_id` on carrier, made on first use, kept for the carrier by `tunnel_id` in a `WeakMap` until tunnel ends (`closed`); new one hears `connected( remote )` (`remote` = first frame from far end, `REMOTE_CONNECT`; else `THIS_CONNECT`). Carrier: no table field of its own, does not close its tunnels. Regions without default code.
- DECL (TS): your code in `Tunnel` sets `current_by_tunnel_id` to endpoint of a `tunnel_id` (tunnel far end opened: make endpoint, seat virtual connection, `connected( true )` = what `Tunnel.of( tunnel_id, carrier, true )` does); your code in `Tunnel end` closes it with its `closed(event)`.
- GEN (Dart): same: `Connection.Tunnel.of( tunnel_id, carrier, [remote = false] )` (class `Connection_Tunnel`), kept in `Expando` by carrier, same regions.
- NOTE: all of §18.6.3 = `MaxTunnels > 1`; one tunnel = getter `connection.<V>` of carrier in TS and Dart too →§18.6.2.

## §18.7 Relay: the runtime primitive on a relay host
KW: AdHoc.Connection.Tunnel.Relay, org.unirail.AdHoc.Connection.Tunnel.Relay, region, holes, OnSerialized, Gone, backpressure, constant memory, tunnel_id re-key
- RT: `Relay` = `AdHoc.Connection.Tunnel.Relay` (C#), `org.unirail.AdHoc.Connection.Tunnel.Relay` (Java). Re-emits tunnel frame from inbound leg on outbound leg without decoding or assembling body; constant state whatever tunnel size.
- RT: per frame: 1) read pack id and `tunnel_id` (if any); 2) ask router (your `Tunneling` code) which outbound connection serves `tunnel_id`; enqueued on that connection's transmitter; 3) write outbound prefix `[pack-id][tunnel_id?]` afresh with `tunnel_id` router left in `dst.tunnel_id` (possibly different → re-address on far side); 4) move body region by region until source's `[0]` or `ABORT` ends frame.
- WIRE: relay frames body itself. Region = what one socket read delivered; usually ends inside a source chunk. Source chunk headers and terminator not passed through: relay notes their positions in each region ("holes"); transmit side writes each region as own chunk `[len][payload]` stepping over holes. Cost 2 bytes/region; payload copied exactly once. Reason: after every region relay stands on chunk boundary → when tunnel source vanishes, relay ends that one frame with `ABORT`, peer resets that one virtual connection, instead of whole shared physical connection closed over a half-sent chunk.
- RT: region holding frame's end is copied; others taken over whole on raw TCP (transport hands receive buffer to relay). Transport decoding frames in its buffer (WebSocket) cannot hand buffer → every region copied.
- RT: one `Relay` instance forwards one frame at a time; after outbound pack serialized, `OnSerialized` releases enqueue gate and wakes inbound receive for next frame.
- RT: failures:
| What fails | What happens |
|:--|:--|
| no target, or target refused enqueue | frame consumed and dropped whole; frame ends only between packs → peer misses whole packs only |
| target connection dies while holding frame | receive drops rest of that frame |
| source dies mid-frame | transmit finishes held region, writes `ABORT`; if nothing of frame reached wire yet, frame vanishes |
| source connection closes | its `OnClose` calls `Gone()`: frame `[pack-id][tunnel_id][ABORT]` queued for every tunnel it fed, after what it queued before |
- RT: `tunnel_id` with `MaxTunnels > 1` lets one physical connection carry many independent tunnels, each routed to own peer. Key = pure application address (not connection id, not registry slot); mapping keys → connections entirely up to your router (one key/connection, block of keys/connection, any lookup filled as peers connect).
- NOTE: AdHoc monitoring backend: `interface Tester__MonitoringObserver : VirtuallyConnects<MonitoringObserver, Tester, (Monitoring, Server)>` sets `int MaxTunnels => 65536;` → 2-byte `tunnel_id`. Dashboard composes key: high byte = id `Monitoring` assigned it; low byte = tester slot. `Monitoring` forwards inbound frames unchanged, finds dashboard of a reply by high byte; `Server` finds tester by low byte, passes key on unchanged. `Server` is relay for this tunnel and simultaneously endpoint of its own `Server__MonitoringObserver` → generator gives it a Relay for one, endpoint code for the other.
- RT: properties: constant memory (at most one region between inbound and outbound socket buffer; 1 GB and 64-byte tunnel same resident memory); end-to-end backpressure (slow outbound → relay stops taking inbound regions → kernel buffer fills → TCP backpressure upstream to producer; no user-space queue, no unbounded growth); interruptible/open-ended (end mid-flight with `ABORT`, or run without end); schema-decoupled (depends only on framing; packs change without touching relay host).
- PITFALL: while relay waits, its inbound connection receives nothing else: other tunnels and packs it carries wait too.

## §18.8 Closing a tunnel
KW: ABORT, close(), Close(), abort(), Abort(), ext_connection, THIS_CLOSE_GRACEFUL, REMOTE_CLOSE_GRACEFUL, REMOTE_CLOSE_ABRUPTLY, THIS_CLOSE_ABRUPTLY, Tunnel end, whole
- WIRE: tunnel ends with frame ending in `ABORT`. No pack id reserved; tunnel's own protocol not needed → relay can say it as easily as endpoint.
| Who ends | How | Far end hears |
|:--|:--|:--|
| endpoint | `close()` on virtual connection's `ext_connection` (`Close()` C#). Packs queued by then go first; last burst ends with `ABORT`; when out, this side hears `THIS_CLOSE_GRACEFUL` and `OnClose()`. `abort()` (`Abort()`) does the same (tunnel has nothing to cut short) | `REMOTE_CLOSE_GRACEFUL`, then `OnClose()` |
| relay that lost tunnel source | frame being forwarded (if any) ends with `ABORT`; when source connection closes, relay sends `[pack-id][tunnel_id][ABORT]` for every other tunnel that connection fed | `REMOTE_CLOSE_ABRUPTLY` when a pack was cut, else `REMOTE_CLOSE_GRACEFUL` |
| endpoint's carrier closes | `close_tunnels_<VirtualConnection>()` in carrier's `OnClose` | (this side) `THIS_CLOSE_ABRUPTLY`, then `OnClose()`, per virtual connection |
- RT: far end: carrier region `Tunnel end` receives `(tunnel_id, whole)`; `whole` false when a pack was cut (cut pack already dropped). Default closes virtual connection with `REMOTE_CLOSE_GRACEFUL` or `REMOTE_CLOSE_ABRUPTLY`, runs `OnClose()`, carrier forgets it. Later frame with same `tunnel_id` starts new tunnel.
- RT: physical link dies while endpoint's burst queued or in flight → virtual connection's pack in flight abandoned (peer discarded partial part; serialization cannot rewind) → next burst starts at pack boundary.
- RT: virtual connection ends like physical: actors released on `OnClose`, `[Grouped]` instances leave group →§13. No lease/sweep needed to guess peer gone.

## §18.9 Transform chains over a tunnel
KW: [Zstd], [ChaCha20], transform chain, end-to-end, MAC, integrity, per-hop chain, trim, IfSendingFrom
- DECL: tunnel = chunked stream → carries transform chain →§16; put stages on `VirtuallyConnects` interface. Chunked tunnel = implicit root; stages wrap bytes through it.
```csharp
[Zstd(6), ChaCha20]
interface Producer__Auditor_Chained : VirtuallyConnects<XProducer, ZAuditor, ZRouter> { }
```
- RULE: chain applies to tunnel transport → same on tunnel-only and structured. All chain rules unchanged: dataflow order (`[Zstd, ChaCha20]` compresses then encrypts); ≤1 compressor, ≤1 cipher; key and nonce supplied at runtime at endpoints →§16.
- RT: chain end-to-end: sending endpoint runs stages, receiving endpoint inverse; every `PATH` host forwards already compressed/encrypted chunks, decodes none; Relay has no stage code. Compression once at source, survives every hop; relays forward smaller form.
- PITFALL: relay cannot read tunnel chained with `[ChaCha20]`, but confidentiality only: changed ciphertext not detected, decrypts to garbage without error. Integrity → add own MAC stage →§16.
- NOTE: chains on physical legs encrypt each wire but relay host holds keys of both legs and sees plaintext between. Chain on virtual connection keeps payload sealed through relay. Per-hop chains protect links; tunnel chain protects conversation; they stack (tunnel encrypted end-to-end while each leg separately compressed for own medium).
- RULE: trim →§17 never stands in a connection chain, virtual included (chain on link wraps all link carries). Trimmed field may still be cut on tunnel leg: its endpoint `IfSendingFrom<XProducer, Producer__Sink_Structured>` names virtual connection. Tunnel chain and cut = independent layers; relays see neither.

## §18.10 Choosing what the middle tier does
KW: Relay, trim, Smart middle, cascading RPC, mirror, Receiver.BytesDst, Transmitter.BytesSrc, fan-out, multiplexer
| Middle host must… | Use | Where |
|:--|:--|:--|
| route bytes between connections, never read them | virtual connection: generated Relay | §18 |
| keep payload and replay later without schema | trim: host = endpoint holding raw bytes | →§17 |
| inspect, transform, filter, fan one stream out to many | Smart middle | below |
| answer a call by calling further | cascading RPC: one actor pair per hop, middle re-issues call | →§13 |
- NOTE: Relay right only when middle host content-blind; never materializes bytes → cannot measure, transcode, filter, duplicate.
- `Smart middle` = does not forward; consumes stream as ordinary consumer (`Receiver.BytesDst` handler →§15), same pass keeps in-memory `mirror`, re-emits to N live subscribers without re-reading persistent store. Not generated; build from stream API:
  1. capture costs nothing idle: append inbound chunk to mirror only while ≥1 subscriber; none → skip capture, plain consumer;
  2. one mirror buffer: single shared buffer; one copy feeds all N, not N relays;
  3. slicer per subscriber: `Transmitter.BytesSrc` hands each subscriber successive mirror slices as its socket drains; slow subscriber never stalls others; middle never buffers more than window in flight.
- NOTE: Smart middle when middle owns schema and must act on content (measure, transcode, filter, replicate one source to many); Relay for pure routing hop, constant cost, no schema.
- RULE: multiplexer →§19 shares one listening port among physical connections; cannot list virtual connection (tunnel never arrives on own port). Reverse exists: multiplexer may carry a relayed physical connection (shared host not party) in tunnel over physical carrier; takes wire id from packs' pool like virtual connection →§19.8.

## §18.11 Virtual connection rules at a glance
KW: VirtuallyConnects, PATH, MaxTunnels, MaxStream_KiloBytes, Relay, Tunneling, carrier, Tunnel end, wire id
| Rule | Detail |
|:--|:--|
| Declaration | `interface X : VirtuallyConnects<L, R, PATH>`; valid wherever `Connects<L, R>` is →§18.1 |
| Endpoints, `PATH`, route, host pairs | each host once (`L`,`R` may be one type); `PATH` ≥1 declared host, any order; route over physical legs, exists, unique; one connection per pair, parallel → `MaxTunnels` →§18.3 |
| Body | empty = tunnel only; non-empty = structured →§18.2 |
| `MaxTunnels` / `MaxStream_KiloBytes` | 256 (≥1, 1 = no `tunnel_id`, else 1–4 B LE) / unbounded KiB; compile-time constants →§18.4 |
| Wire | `[wire id of VC][tunnel_id?][len][data]…[0]`, `ABORT` ends →§18.5 |
| Relay host / endpoint host | `Relay` + `Tunneling` region →§18.6.1; carrier members, `Tunnel`/`Tunnel end` →§18.6.2 |
| One tunnel | `MaxTunnels => 1` → member `carrier.<V>` (snake_case Rust, Swift; `_` while name taken): no id, no `of`/`open`, no table; made on first use or first frame, forgotten when tunnel ends, closed with carrier →§18.6.2 |
| Chains | end-to-end on `VirtuallyConnects`, no trim →§18.9 |
| Numbers tables / multiplexing | wire id from pack pool →§20.4.4; not listable in multiplexer →§19.10 |

# §19 Multiplexing
KW: Multiplex<CONNECTIONS>, multiplexer, uid, first byte, shared host, mux, pool, apply, DEFAULT
SUMMARY: `Multiplex<(A, B, …)>` = one listening port serving several connections of a host; each socket opens with one byte = dialing host's uid. Declaration, byte rules, deployment shapes, generations, socket life, generated code, wiring in Java/C#/TS, relayed connections, parser checks.
- `multiplexer` = one port on which a host serves several of its connections. Every socket on port opens with one byte = `uid` of dialing host; from then socket belongs to connection between dialing and listening host (its packs, actors, states as declared). Next socket may belong to another connection.
- NOTE: connection says nothing about sockets; own port vs shared = deployment decision; multiplexer offers the shared option. Concerns listening side only: dialing host stays ordinary client, only adds the byte. Without multiplexer: one port per connection (listening socket must know in advance which generated connection reads bytes).

## §19.1 Declaring a multiplexer
KW: Multiplex<CONNECTIONS>, tuple, shared host, constants, attributes not read
- DECL: interface extending `Multiplex<CONNECTIONS>`, `CONNECTIONS` = tuple of ≥2 connections:
```csharp
struct Vehicle       : Host{ }   // firmware 3.x
struct VehicleLegacy : Host{ }   // firmware 2.x, still in the field
struct Dashboard     : Host{ }   // operator console
struct Service       : Host{ }   // diagnostic tool
struct Fleet         : Host{ }   // the backend
interface Telemetry       : Connects<Vehicle,       Fleet>{ /* states and branches */ }
interface TelemetryLegacy : Connects<VehicleLegacy, Fleet>{ /* states and branches */ }
interface Console         : Connects<Dashboard,     Fleet>{ /* states and branches */ }
interface Diagnostics     : Connects<Service,       Fleet>{ /* states and branches */ }
interface FleetPort : Multiplex<(Telemetry, TelemetryLegacy, Console, Diagnostics)>{ }
```
- NOTE: `Fleet` = host shared by all four → owns port.
- DECL: body normally empty; may hold constants (carried to generator with multiplexer →§23.4.4); attributes on a multiplexer not read.
- Smallest test-bench shape; third connection kept out on purpose:
```csharp
// struct Hub, Alpha, Beta, Gamma : Host{ ... } - each declares its own ping or pong packs
interface AlphaHub : Connects<Alpha, Hub>{
    [l____________<Alpha.AlphaPing>]
    [____________r<Hub.AlphaPong>]
    struct Start{ }
}
interface BetaHub : Connects<Beta, Hub>{
    [l____________<Beta.BetaPing>]
    [____________r<Hub.BetaPong>]
    struct Start{ }
}
interface GammaHub : Connects<Gamma, Hub>{
    [l____________<Gamma.GammaPing>]
    [____________r<Hub.GammaPong>]
    struct Start{ }
}
/// <summary>One port of Hub for Alpha and Beta.</summary>
interface HubPort : Multiplex<(AlphaHub, BetaHub)>{ }
```
- NOTE: `Alpha`, `Beta` dial same port of `Hub`; `Gamma` dials own port, its wire unchanged by `HubPort`. Behind a multiplexed port: one TCP/WebSocket server instance + multiplexer reading first byte and handing socket to matching pooled Connection.

## §19.2 The first byte of a multiplexed socket
KW: uid, first byte, hosts section, 0..255, 256 hosts, stable uid, unique on a port, Connects<Broker, Broker>, encrypted handshake
- WIRE: every socket on multiplexed port opens with one byte = `uid` of dialing host = its number in `hosts` section of numbers tables →§20.4.3.
- RULE: one byte 0..255; agent errors on host with larger uid → project + extended projects ≤ 256 hosts; multiplexer stands for ≤ 256 connections.
- RULE: stable: hosts numbered across whole project incl. extended projects' hosts, which keep their numbers. Adding a host never renumbers existing → field device keeps sending byte it was built with. Exception: extended project gains hosts whose numbers your hosts had → your hosts renumbered with warning, their first byte changes = wire change for peers dialing a multiplexer →§20.6.
- RULE: unique on a port: two hosts dialing same multiplexer with same uid = error ("Delete the lines of every host but one in the hosts table, and the next run gives them free uids").
- NOTE: why one byte names a connection: shared host party of every listed connection; pair of hosts joined by ≤1 connection →§11; parser lets a host dial a given multiplexer for one connection only → far end of each listed connection is different host → dialing uid names exactly one connection.
- NOTE: connection of two hosts of one type →§11.4: dialing host is of shared host's type → its byte = that type's uid. Broker port serving clients and brokers:
```csharp
struct Client : Host{ }
struct Broker : Host{ }
interface ClientBroker : Connects<Client, Broker>{ /* ... */ }
interface InterBroker  : Connects<Broker, Broker>{ /* ... */ }
interface BrokerPort : Multiplex<(ClientBroker, InterBroker)>{ }   // byte Client.uid -> ClientBroker, byte Broker.uid -> InterBroker
```
- WIRE: after byte, socket = ordinary link of named connection. Byte paid once per socket, never per pack. Nothing on wire after first byte differs; nothing in connection declaration refers to multiplexer: changes deployment, not what connections are.
- WIRE: with encrypted transport (handshake →§16) byte = first byte of encrypted stream after handshake; never sent in clear.

## §19.3 Declared once, chosen at deployment
KW: deployment shape, port per connection, mixed, mux, direct port, .cs~
- RT: multiplexer declares which connections may share a port; forces nothing. Generated code of every listed connection works on own port and on multiplexed port; each side chooses at setup: listening host hands multiplexer to its server, or serves a connection on own port as without declaration; dialing host is told whether port is multiplexed; if so, its connection sends byte before anything else.
| Shape | Cost / when right |
|:--|:--|
| port per connection | listener, port, firewall rule, certificate per connection; right when a connection must be reached, secured or restarted on its own |
| one multiplexed port | one listener, one address to publish, one certificate, one NAT hole |
| mixed | public connections share port; `Diagnostics` keeps own port reachable only from service LAN |
- RULE: host may own several multiplexers (one per port). Connection belongs to ≤1 multiplexer (arrives on exactly one port).
- PITFALL: both sides must agree. Byte sent to a direct port → read as start of first pack; multiplexed port dialed without byte → first byte of first pack read as uid. Same as dialing wrong port.
- NOTE: `.cs~` impact report →§21.4 shows multiplexer edit as reaching port's host only; dialing hosts open sockets with own uid whatever else port serves.

## §19.4 Several kinds of peer, several generations behind one port
KW: generations, VehicleLegacy, TelemetryLegacy, retirement, uid reuse, Named Pack Set, KeepName, SkipName
- NOTE: unrelated peer kinds (vehicles, consoles, diagnostic tool) = hosts with own connections behind one port.
- NOTE: new firmware generation = new host with own connection, not a version field in a pack →§21. `Vehicle`, `VehicleLegacy` differ in uid → both dial same port, each lands on own connection: `TelemetryLegacy` keeps packs and state machine deployed firmware was built against, nothing edits it; `Telemetry` changes freely; handler never asks which firmware (each generation own generated code; first byte settled it before first pack).
- RULE: retirement = deletion of host + connection + their numbers-table lines; take connection out of tuple (`FleetPort` keeps 3); if one connection would remain, keep retired one in tuple →§21.3.
- PITFALL: retire only when none of its devices dials any more. Deleting `VehicleLegacy` frees its uid; next new host may get it →§20.6. Legacy device still in field keeps sending old byte → if new host dials same port, device lands on new host's connection.
- DECL: packs of two generations side by side in one scope: Named Pack Set →§9 filtered with `[KeepName]`/`[SkipName]` picks one generation's packs. Pattern matched against pack's path below project, without namespace → anchor on name segment: `(^|\.)V2\.` keeps packs inside any container named `V2`.

### §19.4.1 Server generations behind one port
KW: FleetV2, FleetV2Port, composed multiplexer, IntFunction, apply(uid), null for unknown uid
- NOTE: backend generation `FleetV2` = new host (copy of `Fleet` + additions/corrections); new peers `VehicleV2`, `DashboardV2` = new hosts. One shared host per multiplexer → `FleetV2` gets own multiplexer:
```csharp
interface FleetPort   : Multiplex<(Telemetry,   Console)>{ }     // Fleet   - generation 1, frozen
interface FleetV2Port : Multiplex<(TelemetryV2, ConsoleV2)>{ }   // FleetV2 - generation 2
```
- RT: both in one process behind one port: server takes two multiplexers composed into one function (Java); asks `FleetPort` first, `FleetV2Port` second:
```java
IntFunction< AdHoc.Connection.Internal > both = uid -> {
    AdHoc.Connection.Internal in = Fleet.FleetPort.DEFAULT.apply( uid );
    return in != null ? in : FleetV2.FleetV2Port.DEFAULT.apply( uid );
};
```
- RT: generated multiplexer returns `null` for uid it does not serve; uid names exactly one host in project (incl. extended projects) → composition never guesses as long as no host dials both multiplexers (holds: every gen-2 peer is new host).
- PITFALL: parser checks each multiplexer alone; keeping dialing hosts of composed multiplexers apart = your job. Retire gen 1 → drop `Fleet.FleetPort` from composition.

## §19.5 The life of a multiplexed socket
KW: socket lifecycle, Telemetry.id, Vehicle.uid, apply, pool, OnOpen, onFailure, External(null), Connection Release, transport slot
1. Caller connects: TCP, or TCP + WebSocket upgrade.
2. RT: caller's connection sends one byte `Telemetry.id` = `Vehicle.uid`, on every new socket incl. reconnects, before anything else. Data already waiting → byte goes in same send.
3. RT: listening side calls `FleetPort.apply(Vehicle.uid)` → takes `Telemetry` connection object from pool, seats it on socket; connection hears connect event and `OnOpen()` as on own port. Unknown byte → socket closed, failure "A multiplexed port has no connection for the host uid N" reported to server's `onFailure`.
4. Packs flow both ways; nothing added.
5. RT: socket gone → transport slot keeps connection object. Next socket on slot naming same connection → takes object back directly. Naming another → kept object released with `External(null)`, runs `Connection Release` region, returns to pool.
- RT: direct port skips 2 and 3 (no byte; connection = one port's factory built).

## §19.6 What the generator produces for a multiplexer
KW: id, id(), Connection.pool, External(null), Connection pool size: a power of two, Connection Release, FleetPort, new_Telemetry, apply(uid), DEFAULT
| Where | What |
|:--|:--|
| every listed connection, both sides | `id` (`id()` Java) = uid of host on far side from shared one; `Telemetry.id` = `Vehicle.uid`; same number both sides (caller sends, multiplexer reads) |
| shared host, per listed connection | `Connection.pool`: connection objects waiting for caller; `External(null)` returns one |
| shared host, per listed connection | injection point `Connection pool size: a power of two`: what you write is added to exponent; default `2` → up to 4 idle objects |
| shared host, per listed connection | injection point `Connection Release`, run as object returns to pool |
| shared host | class named after declaration, `Fleet.FleetPort`: one factory field per connection (`new_Telemetry`, `new_Console`, …), `apply(uid)`, ready instance `DEFAULT` |
- GEN (Java, uids from `hosts` section):
```java
class FleetPort implements java.util.function.IntFunction< AdHoc.Connection.Internal > {
    public java.util.function.Supplier< AdHoc.Connection.Internal > new_Telemetry       = Telemetry.Connection.pool::acquire;
    public java.util.function.Supplier< AdHoc.Connection.Internal > new_TelemetryLegacy = TelemetryLegacy.Connection.pool::acquire;
    public java.util.function.Supplier< AdHoc.Connection.Internal > new_Console         = Console.Connection.pool::acquire;
    public java.util.function.Supplier< AdHoc.Connection.Internal > new_Diagnostics     = Diagnostics.Connection.pool::acquire;
    @Override
    public AdHoc.Connection.Internal apply( int uid ) {
        switch( uid ) {
            case 1: return new_Telemetry.get();       //Vehicle.uid
            case 2: return new_TelemetryLegacy.get(); //VehicleLegacy.uid
            case 3: return new_Console.get();         //Dashboard.uid
            case 4: return new_Diagnostics.get();     //Service.uid
            default: return null;
        }
    }
    public static final FleetPort DEFAULT = new FleetPort();
}
```
- RT: pool bounded, never blocks: empty → new connection object; full → released object dropped. Size = idle objects kept for next callers, not limit on concurrent callers. 32 idle instead of 4 → write `+ 3` in `Connection pool size: a power of two` (2² × 2³ = 2⁵).
- NOTE: `FleetPort` = plain function byte → connection object; server only calls it. Assign own factory to any `new_...`, compose multiplexers (§19.4.1), or write function from scratch.
- PITFALL: pooled object serves caller after caller. Generated code resets its own state on every close; state in your own fields must be cleared by your code in `Connection Release` AND on connection close, since socket naming same connection on same transport slot gets object back without `Connection Release` (§19.5 step 5).

## §19.7 Wiring a multiplexed port
KW: Network.TCP.Server, Network.TCP.Engine, Network.TCP.WebSocket, FleetPort.DEFAULT, mux, Network.TCP.Client, Network.WebSocketClient, External(ext), ConnectAsync
- RT: listening host passes multiplexer to server as function first byte → connection; transport factory then makes only bare transport. Java, multiplexed public port + direct port for `Diagnostics`:
```java
var engine = new Network.TCP.Engine( "Fleet", Runtime.getRuntime().availableProcessors(), 1024 );
var pub = new Network.TCP.Server( "Fleet",
                                  Network.TCP.WebSocket::new,            // the bare transport ...
                                  Fleet.FleetPort.DEFAULT,               // ... and the first byte picks the connection
                                  Network.TCP.onFailurePrintConsole,
                                  engine,
                                  new InetSocketAddress( 443 ) );
var lan = new Network.TCP.Server( "Fleet diagnostics",                   // a direct port: one connection, no byte
                                  host -> {
                                      var ext = new Network.TCP.WebSocket( host );
                                      new Diagnostics.Connection().External( ext );
                                      return ext;
                                  },
                                  new InetSocketAddress( 8443 ) );
```
- C#:
```csharp
var pub = new Network.TCP.Server( "Fleet",
                                  host => new Network.TCP.WebSocket( host ),
                                  Fleet.FleetPort.DEFAULT.apply,
                                  Network.TCP.onFailurePrintConsole,
                                  1024, 0, null,
                                  new IPEndPoint( IPAddress.Any, 443 ) );
```
- RT: dialing host passes `mux` (Java, C#, TS):
```java
var vehicle = new Network.TCP.WebSocket.Client< Telemetry.Connection >( "vehicle", ext -> new Telemetry.Connection(),
                                                                        Network.TCP.onFailurePrintConsole,
                                                                        new Network.TCP.Engine( "vehicle", 1, 1024 ),
                                                                        true );   // mux
```
```csharp
var vehicle = new Network.TCP.WebSocket.Client< Telemetry.Connection >( "vehicle", ext => new Telemetry.Connection(),
                                                                        Network.TCP.onFailurePrintConsole, 1024, mux: true );
```
```typescript
const dashboard = new Network.WebSocketClient( "dashboard", ext => new Console.Connection(),
                                               Network.Host.onFailurePrintConsole, 1024, true );   // mux
```
- RT: raw-TCP clients (`Network.TCP.Client`) take same `mux` argument. AdHocAgent itself dials AdHoc server's multiplexed port over WebSocket or raw TCP depending on address:
```csharp
connection = await new Network.TCP.Client< Connection >( "tcp_client", ( ext ) => new Connection(), Network.TCP.onFailurePrintConsole, 1024, mux: true )
                       .ConnectAsync( new IPEndPoint( ipAddress, uri.Port ), TimeSpan.FromSeconds( 10 ) );
```
- GEN: generated connection has parameterless constructor; server or client seats it on socket with `External(ext)`.
| Side | Java | C# | TypeScript |
|:--|:-:|:-:|:--|
| listening, multiplexed | ✓ | ✓ | - |
| dialing a multiplexed port | ✓ | ✓ | ✓ byte leads first WebSocket frame |
- NOTE: TS host has no server → always dialing side.

## §19.8 Relayed connections in a multiplexer
KW: relayed connection, carrier, shared host not a party, tunnel, wire id, EdgePort
- `relayed` connection = listed in multiplexer but shared host not a party. Shared host accepts socket of the connection's dialing host, carries its bytes in tunnel over physical connection joining shared host and connection's other host = `carrier`. Neither host of relayed connection needs direct link to other.
```csharp
struct Browser : Host{ }
struct Admin   : Host{ }
struct Mobile  : Host{ }
struct Edge    : Host{ }   // owns the public port
struct Backend : Host{ }   // reachable only from Edge
interface Console     : Connects<Browser, Edge>{ /* ... */ }
interface AdminLink   : Connects<Admin,   Edge>{ /* ... */ }
interface EdgeBackend : Connects<Edge,    Backend>{ /* ... */ }   // the carrier
interface Query       : Connects<Mobile,  Backend>{ /* ... */ }   // Edge is not a party of it
interface EdgePort : Multiplex<(Console, AdminLink, Query)>{ }
```
- RULE: parser reading of `EdgePort`: 1) shared host = party of ≥2 listed connections and of more than any other host (`Edge`: `Console`, `AdminLink`) → owns port; 2) listed connection shared host is party of → served directly; 3) listed connection it is not party of (`Query`) needs exactly one carrier: physical connection (not virtual, not modifier) between shared host and one of its two hosts (`EdgeBackend`: `Edge`–`Backend`); other host (`Mobile`) dials port → `Query`'s byte = `Mobile.uid`; 4) carrier need not be listed in multiplexer.
- RULE: rule-3 errors:
| Situation | Message (abridged) |
|:--|:--|
| neither host of relayed connection has physical connection with shared host | "... can relay it only in a tunnel over a physical connection with one of its hosts, and there is none. Declare one, or take `Query` out of the multiplexer." |
| both hosts have one | "... both its hosts have a physical connection with `Edge` ... It is not clear which of them dials the port. Take `Query` out of the multiplexer." |
- WIRE: relayed connection takes wire id from packs' pool = id of its tunnel on carrier, as virtual connection →§18.5; agent writes it after connection's number in numbers tables →§20.4.4.
- RULE: connection relayed by ≤1 multiplexer.

## §19.9 What the parser checks for a multiplexer
KW: Multiplex, tuple, Keep one of them, [UDP], shared host, uid 0..255, namespace, imported connections
| Check | Rule |
|:--|:--|
| tuple of ≥2 connections | `Multiplex<(A, B)>`; single connection has nothing to share with |
| connections only | each element declared with `Connects<L, R>`; host, pack, `Modify<>` modifier, `VirtuallyConnects<…>` rejected |
| each connection once | no connection twice in one multiplexer |
| one shared host | party of ≥2 connections and of more than any other host; side (left/right) irrelevant; owns port |
| relayed connections | not-party connection needs exactly one carrier →§19.8 |
| one connection per dialing host | host dials given multiplexer for ≤1 connection |
| uids fit and differ | every host uid 0..255; no two hosts dialing one multiplexer share uid |
| one multiplexer per connection | connection in ≤1 multiplexer; two multiplexers over same set reported as such ("Keep one of them") |
| no `[UDP]` connection | multiplexed port = TCP port; UDP connection →§14 has own port |
| placed like a connection | declared directly in project scope; name shares connections' namespace → multiplexer and connection may not share name, after import too →§22; may list connections imported from other projects |

## §19.10 Multiplexers and tunnels
KW: VirtuallyConnects, multiplexer, relayed, tunnel
- RULE: `VirtuallyConnects` link rides tunnel through relay hosts, never arrives on own port → parser rejects it in tuple. Relayed connections (§19.8) = opposite: physical `Connects` whose dialing side arrives on port and shared host puts into tunnel.
- NOTE: related: →§11 (one connection per host pair, two hosts of one type), →§20 (uids, wire ids), →§21 (generations, `.cs~`), →§18 (carriers, tunnels), →§14.

# §20 Numbers tables
KW: numbers tables, /** packs, /** project, base256, wire id, uid, marks, tags, persistent ids
SUMMARY: AdHocAgent numbers every entity (project, host, connection, actor, state, branch, pack) and keeps the numbers in two doc comments at top of the description (`packs`, `project`) plus marks for branches and `Actor0` states. Layout, base256 encoding, wire ids, tags, imported slots, host uids, maintenance rules, errors, workflow, pitfalls.
- Entity must stay same entity across runs despite rename, move, reorder → each gets a `number`; all kept in `numbers tables` = two doc comments at top of description file. AdHocAgent writes/maintains them; declarations below carry no numbers.

## §20.1 Identity in a table, position in a pack
KW: entity number, identity, positional fields, field order, Actor0, TYPEDEF, custom code regions
- RULE: entities (project, hosts, connections, actors, states, branches, packs) told apart by number: assigned once, independent of name and place. IDE rename, moving host to file end, sorting connections → nothing changes on wire nor in what generated code keys by number (e.g. custom code regions →§25).
- RULE: fields not numbered; told by place: travel in declaration order, primitive fields too. Reordering fields of a pack = another pack on wire. Layout →§7; deployed-protocol consequences →§21.
- NOTE: number keys: pack wire id, host uid byte, custom code regions. Example: `PositionV2 { float lat; float lon; }` → wire `id, lat, lon`; swapped → `id, lon, lat` = another pack.
| Entity | Numbered | Where kept | Numbered within |
|:--|:--|:--|:--|
| project | yes | first line of project table | - (number = moment first numbered) |
| host | yes | `hosts` section | project and every project it extends; number = host's `uid` byte |
| connection (physical, virtual, multiplexer-relayed) | yes | `connections` section; virtual/relayed also have wire id there | project |
| named actor, RPC method | yes | `actors` section; method called both ways has two numbers | project |
| `Actor0` (connection's own actor) | fixed, never written | - | - |
| state of named actor | yes | `states` section | its actor |
| state of `Actor0` (declared directly in connection) | yes | `states` section, as named actors' states | its actor |
| branch | yes | mark inside routing attribute: `[l____________<Login>/*Ā*/]` | its state |
| pack, enum, constant set (and pack-like: `Modify<>` modifier, named transform flow) | yes | packs table, with wire id when transmitted | project |
| `TYPEDEF` class | no | - | - |
| field | no, positional | - | - |
| parser-created states (`Call`, `Reply` of RPC method, `End`, `Close`) | fixed, never written | - | - |

## §20.2 Where the numbers tables live
KW: /** packs, /** project, hosts, connections, actors, states, imported, numbers tables are updated, read-only, not written into its numbers tables
- DECL: two doc comments right before project's interface, each opening with word naming it. Top of numbered file (shortened):
```csharp
using org.unirail.Meta;
namespace com.my.company2
{
    /** packs
        <see cref='Point3'/>Ā                                       1 common | geo | 📍
        <see cref='Root'/>ÿ                                         0 common | base
    */
    /** project
        <see cref='MyProject'/>ǍŦƈŗč

        hosts
        <see cref='FrontendServer'/>ÿ

        connections
        <see cref='TrialConnection'/>ÿ
    */
    public interface MyProject{
        // the declarations - the complete description is at the end of this chapter
    }
}
```
- RULE: tables belong to AdHocAgent: written on first run, lines kept sorted and aligned, rewritten whenever a number changes. Only yours: text after a pack's numbers = tags (§20.3.3).
- CLI: `.cs` task maintains them: compares tables it would write with file; rewrites only if different (unchanged description keeps timestamp). Logs `The numbers tables of <file> are updated` when writing. `.cs~` impact report →§21.4 reads tables, never writes.
- CLI: tables of every extended project →§22 (directly or transitively) maintained same way; their files rewritten when their numbers change.
- CLI: read-only description not updated: warning ("... is read-only: new numbers and wire ids are not written into its numbers tables."), continues with numbers it has. Run that died can leave file read-only →§24.
- RULE: only project's own interface has tables. Doc comment = table when leading comment of top-level interface (project) and first word is `packs`, `imported`, `project`, `hosts`, `connections`, `actors` or `states`.
- PITFALL: do not start your own doc comment of the project interface with one of these lowercase words. `/** ... */` with `<see cref/>` lines inside project body = ordinary doc comment of following declaration, not a table.

## §20.3 The packs table
KW: /** packs, sorted alphabetically, enums, constant sets, Modify<>, TYPEDEF not listed
- RULE: `/** packs` lists every pack of project sorted alphabetically by path relative to project (`BackendServer.ReplyInts`). Includes packs that never travel: enums, constant sets, empty packs, `Modify<>` modifiers. `TYPEDEF` classes not listed (alias has no identity).
```csharp
    /** packs
        <see cref='BackendServer.ReplyInts'/>ă                      7 backend | 📊
        <see cref='Point3'/>Ā                                       1 common | geo | 📍
        <see cref='Root'/>ÿ                                         0 common | base
    */
```
- RULE: after `/>` each line holds up to three things: number (§20.3.1), wire id (§20.3.2), tags (§20.3.3). Cref renamed by IDE with the pack.

### §20.3.1 The number of a pack
KW: base256, U+00FF, ÿ, Ā, ā, ă, ĊĀ, least significant byte first
- RULE: characters right after `/>` = pack's number (identity); never changes while pack exists.
- WIRE: `base256` encoding: each character = one byte of number + U+00FF, least significant byte first. `ÿ`=0, `Ā`=1, `ā`=2, `ă`=4, `ĊĀ` = 11 + 1×256 = 267. Same encoding for every number in tables and marks.

### §20.3.2 The wire id of a pack
KW: wire id, directly transmittable, PACKS, KeepName, SkipName, KeepDoc, SkipDoc, Pack Set, lowest free wire id, AdhocProtocol.cs
- RULE: decimal number in own column (`7`) = pack's `wire id`: value preceding pack on wire. Only directly transmittable pack has one: sent by some branch, via `PACKS` generic, `KeepName`/`SkipName`/`KeepDoc`/`SkipDoc` filter, or Pack Set (→§9, →§12). Pack used only as field type travels inside field, no wire id.
- Example, `AdhocProtocol.cs` (shortened):
```csharp
	/** packs
		<see cref='Agent.Login'/>Ď                                    5
		<see cref='Agent.Project'/>Ć                                  8
		<see cref='Agent.Project.Connection'/>ċ
		<see cref='Agent.Project.Host.Langs'/>Ę
	*/
```
- NOTE: branches of `Communication` send `Agent.Login`, `Agent.Project` → wire ids. `Agent.Project.Connection` only in field of `Agent.Project`; `Agent.Project.Host.Langs` = enum → listed without wire id.
- WIRE: number = identity, any size. Wire id kept compact: generated receiver reads every pack id of a connection in same fixed number of bytes (enough for largest id) → small ids keep field short.
- RULE: new transmittable pack takes lowest free wire id. In project extending others, new ids start above largest id of imported projects →§22. Virtual and relayed connections draw from same pool.
- RULE: no branch sends pack any more → loses wire id, keeps line, number, tags. Sent again → lowest free wire id, not necessarily old one. Changed wire id = protocol change.

### §20.3.3 Tags of a pack
KW: tags, KeepDoc, SkipDoc, <, leading number, comment filler
- RULE: everything after numbers = tags (any words, symbols, emojis). Organize packs; branches select by them: `KeepDoc`/`SkipDoc` search tags together with doc comments; tags of every project's packs table participate →§9. AdHocAgent keeps tags on rewrite, except:
| You write | Kept | Why |
|:--|:--|:--|
| text after numbers to end of line | same text | tag text |
| `<` somewhere | only part before `<` | `<` starts XML element of doc comment |
| leading/trailing spaces, `/`, `*` | text without them | comment filler |
| several spaces in a row | one space | words re-joined |
| tag starting with number (`2024 sensors`) | `sensors` | number after pack's number read as wire id |
- RULE: write tags as words not starting with digit, no `<`: `sensors | 2024`, `client | auth | 🔑`.

### §20.3.4 Imported packs sent under an id of this project
KW: imported packs, sent under an id of this project, wire id alone, full name
- RULE: extending project usually sends imported pack under wire id its own project gave it. Sent under another id (typically pack its own project never sends → no id there) → packs table gets third section:
```csharp
    /** packs
        <see cref='Sample'/>ÿ        3

        imported packs, sent under an id of this project
        <see cref='com.lib.Library.Trace'/>4
    */
```
- RULE: line names imported pack by full name, holds wire id alone in decimal, optional tags. Pack's number stays in its own project's table. When this happens →§22.

## §20.4 The project table
KW: /** project, hosts, connections, actors, states, section order, ActorsDemo
- RULE: `/** project`: first line = project; then sections `hosts`, `connections`, `actors`, `states`, each name on own line after blank line. Empty section omitted. Hosts, connections, actors in order of numbers; states in order of paths.
- Example `ActorsDemo.cs` (default actor, function-group actor `Notify`, one-way RPC `ping`, two-way RPC `lookup`, stateful actors, virtual connection `ViewerToServer`), shortened:
```csharp
    /** project
        <see cref='ActorsDemo'/>ŵǢǣźč

        hosts
        <see cref='Client'/>ÿ
        <see cref='Server'/>Ā
        <see cref='Hub'/>ā
        <see cref='Viewer'/>Ă

        connections
        <see cref='Link'/>ÿ
        <see cref='HubToServer'/>Ā
        <see cref='ViewerToHub'/>ā
        <see cref='ViewerToServer'/>Ă 26

        actors
        <see cref='Link.Notify'/>Ā
        <see cref='Link.ping'/>ā
        <see cref='Link.lookup'/>Ă ă
        <see cref='Link.Job'/>ą
        <see cref='ViewerToServer.Watch'/>Ĉ

        states
        <see cref='Link.Job.Running'/>Ā
        <see cref='Link.Job.Start'/>ÿ
        <see cref='Link.Notify.Flags'/>Ā
        <see cref='Link.Notify.Messages'/>ÿ
        <see cref='ViewerToServer.Watch.Live'/>Ā
        <see cref='ViewerToServer.Watch.Start'/>ÿ
    */
```

### §20.4.1 The project's own number
KW: project number, born, milliseconds since 2024-10-14 08:42:55.710 UTC, copy, have one number
- RULE: number on project line = moment project first numbered: milliseconds since 2024-10-14 08:42:55.710 UTC. Distinguishes project from every other, incl. extended. Printed with date on numbering new project (`"MyProject" is a new project: its number is ..., born 2026-10-04 08:44:30.167`); messages name projects by that date ("born ...").
- RULE: never copy project line into another project. Two projects with one number refused: "The projects A, B have one number ... one is a copy of the other. Delete the number of the copy - its project line in the numbers table - and the next run gives it a new one."
- RULE: project line cannot be deleted while other lines remain (new number = different project for importers): "The numbers tables of X have no line for the project itself ... restore the line".

### §20.4.2 Slots of the imported projects
KW: slot, imported project, zig-zag, extends, custom code regions keys
- WIRE: extending project →§22 writes one more number after its own per imported project (direct or transitive), space-separated: `<see cref='MyProject'/>ƍǃǤźč ū`. Each = difference own number − imported number, zig-zag encoded (nearby number stays short).
- RULE: position = import's `slot`; slot (not imported project's number) identifies import in generated code (e.g. keys of custom code regions of imported entities). Import keeps its slot. Import disappears + another appears → newcomer takes vacant slot, AdHocAgent warns; if newcomer is old import under new number, its identity was lost — find out why.

### §20.4.3 Hosts: the number is the uid byte
KW: uid, hosts, multiplexer, 256 hosts, numbered across extended projects
- RULE: host number = `uid` on wire: byte peer sends first when dialing multiplexer →§19.2.
- RULE: hosts numbered across project and every extended project; imported hosts keep numbers; own hosts continue after largest. AdHoc's own protocol has hosts 0–3 → extending project starts at `ă` (4). `uid` belongs to exactly one host of composed project.
- RULE: uid one byte: host number > 255 = error → ≤ 256 hosts. Two hosts dialing one multiplexer with same uid = error: "Delete the lines of every host but one in the hosts table, and the next run gives them free uids."
- RULE: adding hosts never renumbers existing (field device keeps its byte), except when extended project grows into numbers your hosts had →§20.6.

### §20.4.4 Connections, actors and states in the project table
KW: connections, wire id, virtual connection, relayed, actors, RPC two numbers, Actor0, states per actor
- RULE: connections numbered per project. Virtual connection →§18 also has wire id in decimal after number (`<see cref='ViewerToServer'/>Ă 26`), same pool as packs' wire ids = id leading every frame of its tunnel. Multiplexer-relayed connection takes one too, as its tunnel id on carrier →§19.8.
- RULE: actors numbered per project. Named actors and RPC methods have a line; `Actor0` fixed number, no line. RPC method called both ways = two actors (one per call direction); one line with both numbers (`<see cref='Link.lookup'/>Ă ă`). Wrong count of numbers = error.
- RULE: states numbered per actor, `Actor0`'s too: each actor restarts at `ÿ`. `Actor0` state named via connection `<see cref='Link.Start'/>ÿ`; named actor's via actor `<see cref='Link.Session.Open'/>ÿ`.

## §20.5 Branch marks and state marks
KW: marks, /*ÿ*/, /*Ā*/, branch number, Actor0 state, _____lr_____, routing attribute
- RULE: branch numbered by mark inside routing attribute, at end of attribute body after `>` and `)`: `[____________r<(PackA, PackB)>/*ÿ*/]`, `[l____________("📍|trial")/*Ā*/]`. Branches numbered per state; in new state right-side branches numbered first → state with one branch per side: `ÿ` on r-branch, `Ā` on l-branch. `_____lr_____` carries one mark (right half derived).
- RULE: mark after `Actor0` state name (`struct Start /*ÿ*/{ }`) only in descriptions written before its number moved to `states` section; agent reads it, moves number there, mark goes.
```csharp
        interface TrialConnection : Connects<FrontendServer, TrialClient>{
            [l____________("📍|base|trial")/*Ā*/]
            [____________r("📍|trial")/*ÿ*/]
            struct Start{ }
        }
```
- PITFALL: marks = identities; never edit, move or copy. Copied mark → two branches one identity; edited → branch becomes another branch. Copying a state/branch to make new one → delete mark in copy; next run numbers it.

## §20.6 Keeping the numbers right
KW: lowest unused number, rename, hand rename, delete line, renumbered, wire id collision, exit code 66, migration, ~number
| What happens | AdHocAgent does |
|:--|:--|
| new entity | takes lowest unused number of its kind from 0 (own hosts: above largest imported host). Existing never move. Number freed by deleted entity reused by next new entity of that kind → whatever keyed by old number (custom code regions) can meet new entity |
| rename in IDE | IDE renames `cref` in table; number goes with entity |
| rename by hand, or delete declaration | line refers to missing name → run stops: "... refers to `X`, which is not there. Renamed by hand? Rename it in the line too, or its number - its identity - is lost. Deleted? Delete the line." Delete line with declaration; branch/state marks go with declarations |
| you delete a line | entity gets new number, as new. Project's own line cannot be deleted while other lines remain |
| extended project adds hosts taking your hosts' numbers | imported project decides: your hosts renumbered as new, warning. Old number not kept anywhere |
| two packs or virtual connections share wire id in one file (copied line, merge) | warning; keeps one; others get new ids |
| this file gives wire id an imported project gives too (usually that project changed since) | warning once; number in this file stands; imported pack or virtual connection moves to free id, written in `imported` section; check hosts it talks to: their id = the one its project gives |
| two entities of one kind share number in one project (copied mark) | run stops (exit code 66): "This may have been accidentally copied. Please delete the duplicate assignment." |
- PITFALL: hosts renumbered because extended project grew → your code in custom code regions of old host number does not follow. Next deployment shows it as left without region, asks before dropping; move it into regions of new number →§25.
- RULE: table errors that stop run:
| Error | Meaning |
|:--|:--|
| "The numbers table line ... has no number after `/>`." | line lost its number |
| "The numbers table lists X ... more than once." | two lines for one entity |
| "... lists X, which is not numbered in a table. Remove the line." | line for something with no table line (`TYPEDEF`, `Actor0`) |
| "... lists X of the project Y: a project numbers only its own entities." | line for entity of imported project |
| "... gives X n number(s), but it has m to number." | wrong count, typically RPC method line |
| "... gives X no number. Only a pack or a virtual connection of an imported project is listed by its id alone." | line with wire id, no number, for entity of this project |
| "X has the mark /\*...\*/ and the number ... in the numbers table ... Keep one of them." | mark from older description disagrees with table |
| "The project X has n numbers after its name ... keep the one it was born with and delete the others." | several project marks |
- NOTE: older forms migrated automatically: marks earlier AdHocAgent wrote after name of pack, host, connection, named actor, state of named actor → moved into tables; marks after RPC method name (`(L____________, Pong) ping /*Ā*/(Ping req);`, two marks for method called both ways) → its line in `actors`. Old "dashboard" comment before interface, and tables written before interface's closing brace → replaced by two tables; `~number` words (former numbers) dropped.

## §20.7 From packs to wire ids: the workflow
KW: discovery, tagging, spreading, finalization, KeepDoc, tag routing, complete numbered description
- NOTE: packs table = project inventory. Cycle: 1) discovery: write packs; next run lists all; unsent pack has no wire id. 2) tagging: tags after numbers (`client | auth | 🔑`). 3) spreading: declare branches; name packs in `PACKS` generic (`[l____________<(PackA, PackB)>]`), select by tags/doc comments (`[l____________("📈")]`), or both (→§12, →§9). 4) finalization: next run gives every transmittable pack a wire id locally, before upload; generator server then generates.
- Complete description (9 tagged packs, 5 hosts, 4 connections, tag-only branches); AdHocAgent run leaves file unchanged (every number/wire id = computed one):
```csharp
using org.unirail.Meta;
namespace com.my.company2
{
    /** packs
        <see cref='BackendServer.ReplyInts'/>ă                      7 backend | 📊
        <see cref='BackendServer.ReplySet'/>Ą                       8 backend | 📊
        <see cref='FrontendServer.PackB'/>Ă                         6 frontend | 📦
        <see cref='FrontendServer.QueryDatabase'/>ā                 5 frontend | query | 🔍
        <see cref='FullFeaturedClient.FullFeaturedClientPack'/>Ć    4 client | full | 📝
        <see cref='FullFeaturedClient.Login'/>ą                     3 client | auth | 🔑
        <see cref='Point3'/>Ā                                       1 common | geo | 📍
        <see cref='Root'/>ÿ                                         0 common | base
        <see cref='TrialClient.TrialClientPack'/>ć                  2 client | trial | 📝
    */
    /** project
        <see cref='MyProject'/>ǍŦƈŗč

        hosts
        <see cref='FrontendServer'/>ÿ
        <see cref='BackendServer'/>Ā
        <see cref='FullFeaturedClient'/>ā
        <see cref='TrialClient'/>Ă
        <see cref='FreeClient'/>ă

        connections
        <see cref='TrialConnection'/>ÿ
        <see cref='MainConnection'/>Ā
        <see cref='TheConnection'/>ā
        <see cref='BackendConnection'/>Ă

        states
        <see cref='BackendConnection.Start'/>ÿ
        <see cref='MainConnection.Start'/>ÿ
        <see cref='TheConnection.Start'/>ÿ
        <see cref='TrialConnection.Start'/>ÿ
    */
    public interface MyProject{
        public class Root{ // Base pack: four packs inherit its fields
            long id;
            long hash;
            long order;
        }
        class max_1_000_chars_string{ // Non-transmittable typedef
            [D(+1_000)] string? TYPEDEF;
        }
        class Point3{
            private float          x;
            private float          y;
            private float          z;
            max_1_000_chars_string label;
        }
        ///<see cref = 'InJAVA'/>
        struct FrontendServer : Host{
            public class QueryDatabase : Root{
                private string? question;
            }
            public  class PackB{ }
        }
        ///<see cref = 'InCS'/>
        struct BackendServer : Host{
            public class ReplyInts : Root{
                [D(300)] int[] reply;
            }
            public class ReplySet : Root{
                [D(+300)] Set<int> reply;
            }
        }
        ///<see cref = 'InTS'/>
        struct FullFeaturedClient : Host{
            public class Login : Root{
                private string? login;
                private string? password;
            }
            public class FullFeaturedClientPack{
                max_1_000_chars_string query;
            }
        }
        ///<see cref = 'InCS'/>
        struct TrialClient : Host{
            public class TrialClientPack{
                max_1_000_chars_string query;
            }
        }
        ///<see cref = 'InTS'/>
        struct FreeClient : Host{ }
        interface TrialConnection : Connects<FrontendServer, TrialClient>{
            [l____________("📍|base|trial")/*Ā*/]
            [____________r("📍|trial")/*ÿ*/]
            struct Start{ }
        }
        interface MainConnection : Connects<FrontendServer, FullFeaturedClient>{
            [l____________("📍|base|trial|🔑|full")/*Ā*/]
            [____________r("📍|trial|full")/*ÿ*/]
            struct Start{ }
        }
        interface TheConnection : Connects<FrontendServer, FreeClient>{
            [l____________("📍|base")/*Ā*/]
            [____________r("📍")/*ÿ*/]
            struct Start{ }
        }
        interface BackendConnection : Connects<FrontendServer, BackendServer>{
            [l____________("🔍|📍|📦")/*Ā*/]
            [____________r("📊")/*ÿ*/]
            struct Start{ }
        }
    }
}
```
- RULE: routing in tags: each branch string = `KeepDoc` regex, `|` = or. No `PACKS` generic → branch searches project's transmittable packs. `BackendConnection`: front end sends `🔍|📍|📦` matches (`QueryDatabase`, `Point3`, `PackB`); back end answers `📊` (`ReplyInts`, `ReplySet`). `Root` sent too: `base` selects it in three connections.
- PITFALL: regex matches substring: `base` would also match a pack whose doc comment says "database" (none here) →§9.
- NOTE: wire ids 0–8 cover nine packs (every pack sent by some branch). Pack used only through a field — here `TYPEDEF` `max_1_000_chars_string` — has no line at all.
- NOTE: `Root` = base pack, four packs inherit its fields →§8; also sent alone.
- NOTE: no `actors` section: every state belongs to its connection's `Actor0`; `states` section numbers them (each connection's from `ÿ`), branches carry marks.
- NOTE: `[D(300)] int[]` = array of exactly 300 items; `[D(+300)] Set<int>` = set of up to 300 →§5.

## §20.8 Pitfalls of the numbers tables
KW: edit numbers, hand rename, delete lines, retire, uid reuse, tags, copied marks
- PITFALL: do not edit numbers; only tags yours. Hand-changed number = another identity: wire ids, uids, custom code regions follow number, not name.
- PITFALL: rename in IDE, not by hand: hand rename stops next run until table line renamed too.
- PITFALL: do not delete lines to "clean up": entity becomes new (new number, new host uid, custom code regions no longer match).
- PITFALL: retiring entity frees its number; next new entity of that kind may take it. For host: new host may get uid a retired device generation still sends →§21.3.
- PITFALL: tags: no `<`, no leading number →§20.3.3.
- PITFALL: copied marks: copying state/branch with mark → two entities one identity; delete mark in copy.

# §21 Evolving a protocol
KW: protocol evolution, generation, new host, frozen generation, wire change, .cs~, impact report, adhoc-impact
SUMMARY: No version fields or field tags: bytes fixed by description (wire id + fields in declaration order); deployed generation that cannot update is never edited, new generation = new host + connection. What edits change the wire; `.cs~` impact report (CLI, previous-version lookup, comparison, verdicts, page, JSON, CI guard, pitfalls).
- RULE: what travels fixed by description: pack on wire = wire id + fields in declaration order; bytes do not say which edit produced them. Edit changing bytes = different protocol → hosts on both ends regenerated and deployed together.
- RULE: deployed generation that cannot be updated is not edited. New peer generation = new host with own connection, declared next to old in same description; old stays exactly as was.
- CLI: `.cs~` task tells, before generation, which hosts an edit reaches and why →§21.4.

## §21.1 How packs and peers are told apart on the wire
KW: wire id, position, uid, names do not travel
| What | On the wire | Defined in |
|:--|:--|:--|
| which pack follows | wire id, fixed number of bytes per connection | →§20 |
| which field is which | position: declaration order, in sequences, no per-field tag | →§7 |
| which peer dialed multiplexed port | dialing host's uid, one byte, once per socket | →§19 |
- RULE: everything else (names of packs, fields, hosts, states, comments, declaration order in file) stays in description and generated code. Identities in numbers tables → rename/move keeps number and wire id.

## §21.2 What changes the wire and what does not
KW: wire change, another pack, other ids, other flow, rename, reorder, comment, KeepDoc, custom attribute, reaches
| Edit | On the wire | `.cs~` says it reaches |
|:--|:--|:--|
| add or remove field (any position) | another pack | every host sending/receiving the pack, and every host handling a pack built of it |
| change field type, range, length limits, varint encoding, nullability, or transform chain on field | another pack | same hosts |
| change custom attribute of pack or field | no change (becomes constant of generated code) | same hosts |
| two fields of one sequence swap | another pack | same hosts; page notes "In N packs fields changed places" |
| move primitive field past string, collection, pack-typed or nullable field | no change (neither sequence changes) | nobody |
| change transform chain of pack | other bytes for that pack | same hosts →§16 |
| branch starts/stops sending pack (gains/loses wire id) | other ids; ids of other packs and service packs stay; peer that is never sent new pack still understands sender, unless greatest id passes 255: all ids of project become 2 bytes | hosts of connections that send it |
| change enum or constant set | other values | every host handling a pack built of it, and hosts carrying it as constants |
| change connection, its actors or states: branches, transitions, timeouts, packs of a branch | other flow (what may be sent when) | both hosts of connection |
| change host declaration: languages, implementation settings | no change | that host |
| change multiplexer | no change after first byte | host its port is on |
| rename pack, host, connection, actor, state with IDE (number stays) | no change (names do not travel) | hosts of entity (generated names change) |
| rename field | no change when type and position stay | hosts of pack; report shows one field removed, one added (fields unnumbered) |
| reorder declarations in file (packs, hosts, connections) | no change | nobody |
| edit comment | no change | nobody; edited comments listed apart |
| edit comment or tag a branch selects packs by (`KeepDoc`/`SkipDoc` →§9) | other ids and flow when branch now selects other packs | hosts of connection whose state changed; comment still listed apart |
- NOTE: "reaches" = host's generated code made of that entity, must be regenerated; not always a wire change (rename reaches hosts though old/new peers still understand each other). When wire changed: every reached host regenerated and deployed together with its peers.

### §21.2.1 The order of fields is part of the wire format
KW: field order, two sequences, primitive fields, swap, lat lon, tidying
- WIRE: field carries no number, no tag; told by place. Generator splits pack fields into two sequences: (1) strings, pack-typed fields, collections, nullable fields; (2) single primitive fields (numbers, booleans, enums, Value packs); writes each in description order (exact layout →§7).
- PITFALL: primitive fields no exception: swap two fields of one sequence → every sender/receiver reads other's value. Swap `float lat; float lon;` in deployed pack → ends exchange latitude for longitude.
- RULE: two fields of one sequence swapped = another pack on wire; all senders/receivers regenerated and deployed together.
- RULE: primitive field moved past a string changes neither sequence → nothing.
- RULE: new field not safe at any position: pack is different pack once it has it. AdHoc does not evolve pack in place; generation that must keep old layout keeps old pack →§21.3.
- PITFALL: tidying (sorting fields by name, grouping by meaning) = protocol change, not cosmetic. Editing a comment = opposite: changes documentation of generated code, nothing a host does.
- RULE: impact report treats every order change inside a sequence as pack change; does not try to prove a reorder harmless.

## §21.3 A new generation is a new host
KW: generation, VehicleLegacy, TelemetryLegacy, PositionV2, frozen, retirement, FleetPort, uid reuse
- DECL: second peer generation = second host + second connection (not version field). Firmware 2.x and 3.x vehicles = two hosts in one description, each own connection to backend and own packs:
```csharp
using org.unirail.Meta;
namespace com.fleet
{
    public interface FleetProtocol
    {
        class PositionV2   // what firmware 2.x was built against
        {
            float lat;
            float lon;
        }
        class Position
        {
            double lat;
            double lon;
            float  speed;
        }
        class Alarm
        {
            string text;
        }
        ///<see cref = 'InJAVA'/>
        struct Fleet : Host { }          // the backend
        ///<see cref = 'InCS'/>
        struct VehicleLegacy : Host { }  // firmware 2.x, still in the field
        ///<see cref = 'InCS'/>
        struct Vehicle : Host { }        // firmware 3.x
        interface TelemetryLegacy : Connects<VehicleLegacy, Fleet>
        {
            [l____________<PositionV2>]
            struct Start { }
        }
        interface Telemetry : Connects<Vehicle, Fleet>
        {
            [l____________<(Position, Alarm)>]
            struct Start { }
        }
        interface FleetPort : Multiplex<(Telemetry, TelemetryLegacy)> { }
    }
}
```
- NOTE: `VehicleLegacy` (uid 1) and `Vehicle` (uid 2) differ in uid → both dial same port of `Fleet`; `FleetPort` hands each socket to its generation's connection →§19. Without multiplexer each connection has own port. `.cs~` must not list `VehicleLegacy`.
- NOTE: old dialect frozen, new free: `TelemetryLegacy` keeps packs and flow deployed firmware built against; `Telemetry` may drop a pack, rename state, change field type without regard for field devices.
- NOTE: no version branching in handlers: each generation own generated code and state machine; connection (or first byte on multiplexed port) settled it before first pack.
- RULE: retirement = deletion: when last old vehicle updated, delete `VehicleLegacy`, `TelemetryLegacy`, `PositionV2` with their numbers-table lines (line left behind stops next run); take `TelemetryLegacy` out of multiplexer tuple. Limit: multiplexer lists ≥2 connections; one-element tuple does not compile. `FleetPort` would keep `Telemetry` alone, and multiplexer cannot be dropped either: `Vehicle` devices open sockets with uid byte, port without multiplexer reads it as pack start →§19. On such port keep retired generation (host, connection, packs, frozen) in tuple until another connection joins port.
- RULE: frozen generation stays compatible because nothing edits it ("nothing" literally): no field tags protect pack; adding field, widening type, reordering fields = wire change. Two rules: 1) everything frozen host sends/receives frozen with it: connection, states, packs, every pack/enum/constant set those packs are built of. Pack both generations send belongs to frozen generation; new generation needing it changed gets own pack (`Position` next to `PositionV2`). 2) check every edit before it goes anywhere: `AdHocAgent .\FleetProtocol.cs~` lists reached hosts; frozen host among them → edit touched frozen generation →§21.4.7.
- RULE: host uids stable (adding hosts never renumbers) →§20. Deleting host and its line frees uid; next new host takes lowest free uid, possibly that one. If retired-generation devices may still dial, keep retired host's declaration (host without connections keeps its number) until no device can send its uid.
- NOTE: backend generations same way: new server host next to old, each own multiplexer, both behind one port →§19.4.1.

### §21.3.1 When separate generations fit
KW: generations, known population, open ecosystem
- NOTE: every served generation = generated code you build and deploy side by side. Fits known population (shipped devices, listable peers, retirement date you control). Does not fit open ecosystem with mostly unknown peers and unbounded generation count (declaration per generation grows without end).

## §21.4 The `.cs~` impact report
KW: .cs~, impact report, changes.html, which hosts have to be updated, no upload, no UUID, adhoc-impact
- CLI: `.cs~` task compares description with previous version, writes one HTML page answering: which hosts must be updated, and why. Nothing uploaded, no UUID needed, neither version written; numbers tables read, never rewritten. Page carries same answer as JSON for scripts (§21.4.6).
- NOTE: uses same parser as `.cs` task, locally. Previous = file, folder, or `.description` folder of newest differing deployment backup. Page has four tiles: update, new, removed, not reached.

### §21.4.1 Running the impact report
KW: AdHocAgent, ~ suffix, previous version argument, output folder, <stem>.changes.html, console verdict lines, browser
- CLI:
```text
AdHocAgent .\FleetProtocol.cs~
AdHocAgent .\FleetProtocol.cs~ .\v1\FleetProtocol.cs~
AdHocAgent .\FleetProtocol.cs~ D:\Deploy\FleetProtocol_7~
AdHocAgent .\MyProject.cs~ .\Library.cs .\reports
```
- CLI: first argument = current description + `~`. Other arguments = those of `.cs` task (imported `.cs`/`.csproj` files; a folder = output folder) + optional previous version = first other argument ending with `~`. General argument rules →§24.
- CLI: page → `<output folder>/<description stem>.changes.html` (working directory when no output folder). Console lists hosts by verdict (non-empty lines only) and file name:
```text
  have to be updated (3): Fleet, Vehicle, VehicleLegacy

[16:55:18 INF] The report is in ./FleetProtocol.changes.html
```
- CLI: four possible lines: `new`, `have to be updated`, `removed`, `not reached`. Page then opens in default browser, unless output redirected (scripted run reads file).

### §21.4.2 How the previous version is found
KW: previous version, .description folder, backup folder, <stem>_<n>, newest differing backup, exit code 1
| Previous given as | AdHocAgent takes |
|:--|:--|
| file `...\FleetProtocol.cs~` | that file. Imports: per current import, same-named file beside previous file = its previous version; none → import taken as unchanged |
| folder `...\v1~` | file of description's name in folder, imports as for file; if absent, the file in its `.description` subfolder (deployment backup folder works as is). Folder must hold one of the two, else run stops |
| `.description` folder (directly or in backup) | description + as imports every other `.cs` file in that folder (saved description complete in itself) |
| nothing | description saved by last deployment (below) |
- CLI: every deployment copies description and files compiled with it into `.description` folder of its backup →§25. Without `~` argument: AdHocAgent looks for backup folders `<description stem>_<n>` in output folder and working directory holding `.description/<description file name>`, newest by creation time. Newest backup right after deployment = files on disk → takes newest backup whose `.description` differs from current description + imports; newest one when none differs. Page header says origin: "named on the command line" or "saved by the deployment backed up in ...".
- CLI: no backup holds `.description` (e.g. backups by earlier AdHocAgent) → run stops (exit code 1), lists places searched + hint: `AdHocAgent "<current>~" "<previous>~"`.

### §21.4.3 What the report compares
KW: parser, model, hosts, packs, connections, multiplexers, renames via numbers tables, token comparison, comments separate
- RULE: both versions read by `.cs` parser locally; version that does not parse stops report as it would stop generation. Each reduced to what generator is given:
  - hosts: languages, implementation settings, constants;
  - packs: wire id, transform chain, every field with type, limits, encoding, attributes, two field sequences in order, constants; enums and constant sets with members;
  - connections: two hosts, actors (`MaxActiveInstances`, multicast), states (what each side sends there and where it goes), tunnel id and route of virtual connection, multiplexer connection is on, chain;
  - multiplexers: host whose port they are, connections listed.
- RULE: references kept by name, never index; renames recognized via numbers tables: entity keeping number under another name = renamed, not removed+added. Besides model, text of every declaration compared token by token (spaces, line breaks, comments ignored) → edit unexplained by model still shows. Comments compared separately, never make a host one to update.

### §21.4.4 What makes a host one to update
KW: reached, verdict, update, new, removed, not_reached, now handled, no longer handled, another role, Resumable, trim
- RULE: host to update when anything its generated code is made of differs:
| Reached through | When |
|:--|:--|
| host itself | declaration differs: languages, implementation settings, constants |
| project | project declaration differs (base list, project-level constants) → every host |
| connection it is party of | connection, one of its actors or states differs |
| pack it sends/receives | pack differs: sent directly in branch, carried inside another pack, or kept as raw bytes by a trim →§17 |
| type its packs are built of | enum, constant set or pack used as field type differs, any depth |
| guaranteed delivery on its connection | `Resumable` declaration differs →§14 |
| virtual connection it relays | host on tunnel route and virtual connection differs |
| multiplexer on its port | multiplexer differs. Host dialing port not reached (opens socket with own uid whatever port serves) |
| its part in unchanged entity | branch, Pack Set or field of another pack now brings entity to host (`now handled`), no longer (`no longer handled`), or host now serializes what it only parsed (`another role`) |
- RULE: comment reaches nobody. Non-entity declarations (Pack Sets, filter templates, modifiers, type aliases) reach hosts via entities built with them: edited Pack Set → state whose branch gained a pack; edited alias → field whose limit moved.
| Verdict | Console line | Meaning |
|:--|:--|:--|
| `update` | have to be updated | existing host reached by ≥1 reason |
| `new` | new | current adds host; reasons list only entities changed along with it |
| `removed` | removed | current no longer has host |
| `not_reached` | not reached | no reason reaches host |

### §21.4.5 The report page
KW: changes.html, dark theme, tiles, The two versions describe the same protocol, matrix, reason card, vscode://file, Comments edited
- NOTE: dark theme; opens with answer; rest is detail, folded:
  1. header: project, previous and current file with dates and previous origin; tally: hosts to update (with new, removed), not reached, entities changed, comments edited. Nothing a host is made of differs → "The two versions describe the same protocol".
  2. Hosts to update: tile per reached host (update first, then new, then removed): languages, verdict, reason count, first reaching entities; note in how many packs fields changed places.
  3. Not reached: tile per unreached host.
  4. How it is decided (folded): rules of §21.4.4.
  5. The details (folded): *What changed, and whom it reaches* = matrix changed entities (badged new/removed/renamed/changed) × reached hosts; *section per reached host*, reasons grouped "The host itself", "Connection ... · with ...", "Constants and types". Reason card: entity kind, name (old → new for rename), host's use ("sends to Fleet over Telemetry", "receives from VehicleLegacy over TelemetryLegacy"), differing properties (value old → new, set − / +, order old order → new order), declaration quoted before/after line by line with changed part marked; entity's own text unchanged → card quotes declaration change comes from (Pack Set, type alias, template). *Changed, and reaching no host*: entities differing but no host sends/receives/built from. *Other edited declarations*: Pack Sets, templates, modifiers, type aliases. *Comments edited* (folded): each with differing words marked; no host update needed.
- NOTE: every name and line number = `vscode://file/<path>:<line>:<column>` link into current description (opens VS Code). Removed entity → link to nearest enclosing declaration that still exists.
- NOTE: example edit of §21.3 description (`lat`/`lon` swapped in `PositionV2`, `ushort heading` added to `Position`, comment added to `Alarm`) → `Fleet`, `Vehicle`, `VehicleLegacy` to update; note fields changed places in one pack; card of `PositionV2` "order of the primitive fields: lat, lon → lon, lat"; `Alarm` comment under *Comments edited*. Frozen `VehicleLegacy` listed → edit must be undone.

### §21.4.6 The report for scripts: the `adhoc-impact` JSON
KW: adhoc-impact, application/json, project, previous, current, update, new, removed, not_reached, hosts, reasons, kind, what, jq, ConvertFrom-Json
- CLI: same answer in page as one line of JSON in hidden element:
```html
<script type = "application/json"
        id = "adhoc-impact">
	{"project":"FleetProtocol","previous":"…\\v1\\FleetProtocol.cs","current":"…\\FleetProtocol.cs","update":["Fleet","Vehicle","VehicleLegacy"],"new":[],"removed":[],"not_reached":[],"hosts":[{"name":"Fleet","languages":["InJAVA"],"verdict":"update","reasons":[{"kind":"Pack","name":"PositionV2","what":"changed"},{"kind":"Pack","name":"Position","what":"changed"}]},…]}
</script>
```
| Key | Holds |
|:--|:--|
| `project` | project name |
| `previous`, `current` | paths of the two compared description files |
| `update` | names of hosts that must be updated |
| `new` | hosts current adds |
| `removed` | hosts current no longer has |
| `not_reached` | hosts changes do not reach |
| `hosts` | every host: `name`, `languages` (e.g. `"InCS"`), `verdict` (`update`, `new`, `removed`, `not_reached`), `reasons` |
| `hosts[].reasons[]` | `kind` (e.g. `Host`, `Pack`, `Enum`, `Constants`, `Connection`, `Virtual connection`, `Actor`, `State`, `Multiplexer`, `Project`), `name` (entity path, e.g. `Telemetry.Start`), `what`: `changed`, `new`, `removed`, `renamed`, or for unchanged entity `now handled`, `no longer handled`, `another role` |
- CLI: element on a line of its own; serializer writes `<`, `>`, `&` inside JSON as `\u` escapes → nothing ends element early. Extract without HTML parser:
```shell
grep -o '<script type="application/json" id="adhoc-impact">.*</script>' FleetProtocol.changes.html | sed 's/<[^>]*>//g' | jq -r '.update[]'
```
```powershell
((Select-String -Path FleetProtocol.changes.html -Pattern '(?<=id="adhoc-impact">).*(?=</script>)').Matches[0].Value | ConvertFrom-Json).update
```

### §21.4.7 Guarding a frozen generation in a pipeline
KW: CI, pipeline, frozen host, jq -e, update + removed, redirected output, impact.json
- CLI: run report on every description change; fail build when frozen host reached (in `update` or `removed`):
```shell
AdHocAgent ./FleetProtocol.cs~ > impact.log    # redirected: no browser
grep -o '<script type="application/json" id="adhoc-impact">.*</script>' FleetProtocol.changes.html \
  | sed 's/<[^>]*>//g' > impact.json
jq -e '(.update + .removed) | index("VehicleLegacy") | not' impact.json \
  || { echo "the frozen generation VehicleLegacy is reached"; exit 1; }
```
```powershell
AdHocAgent .\FleetProtocol.cs~ > impact.log
$impact = (Select-String -Path FleetProtocol.changes.html -Pattern '(?<=id="adhoc-impact">).*(?=</script>)').Matches[0].Value | ConvertFrom-Json
if ($impact.update + $impact.removed -contains 'VehicleLegacy')
{
    throw 'the frozen generation VehicleLegacy is reached'
}
```
- CLI: without `~` argument pipeline needs deployment backups with `.description` folders; otherwise keep last released description in repository and name it as previous version.

### §21.4.8 Pitfalls of the impact report
KW: imports, renames, numbers tables, reached vs incompatible, older backups
- PITFALL: pass the imports: report parses both versions; description extending other projects needs their files as for `.cs` task →§22.
- PITFALL: renames rely on numbers tables: IDE-renamed entity keeps number → renamed. Entity whose table line was deleted got new number → removed and added. Fields have no numbers: renamed field always one removed + one added.
- PITFALL: reached ≠ incompatible: rename reaches hosts whose wire unchanged; changed pack reaches hosts whose wire changed. Report says which to regenerate; whether old and new builds may meet on a connection → table §21.2.
- PITFALL: backups from older AdHocAgent have no `.description`, cannot serve as previous; name previous file instead.

# §22 Reusing descriptions
KW: extend, import, _<>, X<>, Modify<>, Library, composition, reuse, imported project
SUMMARY: Model: project ← projects it extends → host → connection → actor → state → branch → pack → field. Three mechanisms: extend whole project, import single declarations by `_<>`, edit imported declarations in place by `Modify<>` (original files untouched).

| Mechanism | Declared as | Effect |
|:--|:--|:--|
| extending a project | `interface MyProject : Library { }` | brings every host, connection, multiplexer, pack, enum, constant set of `Library` and of projects it extends |
| importing single declarations | `_<...>` in project's base list | adds one connection, or broadcasts enum / constant set to every host |
| modifying imported declaration | `class M : Modify<Target> { }` | edits pack fields; removes enum / constant-set members; replaces pack attributes; adds languages and settings to host; adds actors and states to connection |

- NOTE: own connection may be built from an imported connection's states, as from own →§11.

## §22.1 Extending a project
KW: interface MyProject : Library, extends, imported sources, .csproj, duplicate names, root project

### §22.1.1 Declaring an extension
KW: interface : com.lib.Library, Compile Include, could not be found, duplicate names, see cref full name, name hiding
- DECL: extending project = C# interface extending other project interfaces:
```csharp
using org.unirail.Meta;
namespace com.my
{
    public interface MyProject : com.lib.Library
    {
        class Sample { long value; }
        ///<see cref = 'InJAVA'/>
        struct Metrics : Host { }
        interface ServerToMetrics : Connects<Server, Metrics>
        {
            [l____________<(Sample, Stamp)>]
            struct Start { }
        }
    }
}
```
- CLI: imported sources compiled with description: pass their files as extra args — `.cs` files or `.csproj` (its `<Compile Include>` items collected): `AdHocAgent .\MyProject.cs .\Library.cs`. Missing source stops run: "... is not declared in the description files given to AdHocAgent. If an imported project declares it in a file of its own, pass that file too: AdHocAgent Main.cs Imported.cs" →§24.
- RULE: everything comes in: hosts, connections, multiplexers, packs, enums, constant sets of extended project and of projects it extends in turn. Own connections may join own hosts with imported ones (`Connects<Server, Metrics>`, `Server` from `Library`); own branches may send imported packs (`Stamp`).
- RULE: names unique across composition: two hosts, two connections, or connection + multiplexer with one name after import stop run: "The following hosts have duplicate names after importing '...' ... This is not allowed. Please rename or remove the duplicates to resolve the conflict."
- NOTE: packs not compared by name: `com.my.MyProject.Stamp` and `com.lib.Library.Stamp` may coexist; inside `MyProject` simple `Stamp` = own pack (C# name hiding); name imported one by full name.
- RULE: one connection per pair of hosts holds across composition: own connection may not join two hosts an imported connection already joins →§11.
- RULE: root = description file you run; its first top-level interface = project code is generated for; others imported →§3.
- PITFALL: inside extending interface C# finds imported types by simple name wherever a type is expected (`Connects<Server, Metrics>`, `Modify<Stamp>`), but `<see cref="..."/>` in doc comment does NOT look through extended interfaces: use full name `<see cref="com.lib.Library.Stamp.zone"/>`.
- CLI: AdHocAgent maintains numbers tables →§20 of project run and of every project it extends (directly or transitively) → run of extending project may rewrite imported project's file when its numbers change. File of a project NOT extended (passed only so a reference compiles) not maintained.

### §22.1.2 Numbers across the imported projects
KW: uid, wire id, numbers tables, imported packs sent under an id of this project, renumbered, project line, slot
- NOTE: imported project keeps own numbers (does not know who extends it); extending project numbers own entities around them. Library as AdHocAgent numbers it:
```csharp
using org.unirail.Meta;
namespace com.lib
{
    /** packs
        <see cref='Client.Hello'/>Ă    0
        <see cref='Level'/>ă
        <see cref='Limits'/>Ą
        <see cref='Server.Info'/>ā     2
        <see cref='Stamp'/>ÿ           1
        <see cref='Trace'/>Ā
    */
    /** project
        <see cref='Library'/>Ťǅǥźč

        hosts
        <see cref='Server'/>ÿ
        <see cref='Client'/>Ā

        connections
        <see cref='Link'/>ÿ
    */
    public interface Library
    {
        enum Level { Low, High }
        struct Limits { const int MaxHops = 16; const int MaxZone = 24; }
        class Stamp { long time; int zone; }
        [Zstd]
        class Trace { [D(+4_000)] string origin; int hops; }
        ///<see cref = 'InCS'/>
        struct Server : Host
        {
            public class Info { string text; Level level; Trace trace; }
        }
        ///<see cref = 'InCS'/>
        struct Client : Host
        {
            public class Hello { string name; }
        }
        interface Link : Connects<Client, Server>
        {
            [L____________<Ready, Client.Hello>/*ÿ*/]
            struct Start { }
            [_____lr_____<Stamp>/*Ā*/]
            [____________r<Server.Info>/*ÿ*/]
            struct Ready { }
        }
    }
}
```
- GEN: tables written for the §22.1.1 extension:
```csharp
namespace com.my
{
    /** packs
        <see cref='Sample'/>ÿ    3
    */
    /** project
        <see cref='MyProject'/>Ƌǅǥźč ō

        hosts
        <see cref='Metrics'/>ā

        connections
        <see cref='ServerToMetrics'/>ÿ
    */
    public interface MyProject : com.lib.Library
```

| What | Rule |
|:--|:--|
| host numbers (uids) | numbered across whole composition: imported hosts keep theirs, own hosts continue after largest (`Metrics` = `ā`, 2) → uid names exactly one host of composed project |
| wire ids of imported packs | keep id their own project gave (`Stamp` still 1 when `ServerToMetrics` sends it) |
| wire ids of own packs | start above largest imported id (`Sample` → 3) |
| clash of wire ids | file with no numbers keeps imported ids, takes free ones. Ids written in your file stand, for imported packs too; imported one clashing with them moves, with warning, into `imported` section |
| imported pack sent under own id | when its own project gives it no id (never sends it), own project gives one; packs table lists it in section `imported packs, sent under an id of this project`. If `ServerToMetrics` also sent `Trace` (library only carries it inside `Info`) → id 4, line `<see cref='com.lib.Library.Trace'/>4` |
| connections, actors, states, packs | numbered per project: own numbering restarts at `ÿ` |
| project line | own project's number + one slot per imported project →§20 |

- NOTE (diagram): imported hosts keep uids (Server 0, Client 1); imported packs keep ids 0–2 (Hello 0, Stamp 1, Info 2; Trace none); own Sample 3; Trace sent by MyProject would get 4; project line of MyProject = own number + one slot pointing at Library.
- PITFALL: library owner later adds hosts taking numbers own hosts already have → library wins: own hosts renumbered (warning); code in their custom code regions must be moved →§20.

### §22.1.3 Example: a backend extension of AdHoc's own protocol
KW: AdHocProtocol, AdhocProtocol.cs, AdHocProtocolWithBackend, Metrics, Authorizer, backend, internal protocol
- NOTE: `AdhocProtocol.cs` (https://github.com/AdHoc-Protocol/AdHoc-protocol/blob/main/AdhocProtocol.cs) describes public connections of agent and generator server. Server side often needs internal protocol (metrics, authorization, workload distribution). Two ways:
  1. separate description — best when external and internal share no packs; two separately generated projects in one process need distinct namespaces for runtime libraries →§10.
  2. extension of `AdHocProtocol` — when both must meet in one `Server` host: extension's connections join imported `Server` with new hosts; one generated `Server` serves both.
```csharp
using org.unirail.Meta;
namespace org.unirail
{
    public interface AdHocProtocolWithBackend : AdHocProtocol
    {
        ///<see cref = "InCS"/>
        struct Metrics : Host { }
        class MetricsData { public string UserName; public long LoginTime; public long LogoutTime; public int LoginAttempts;
                            public int FailedLoginAttempts; public string LastAccessedPage; public bool IsSessionActive; }
        enum Role { Admin, User, Guest, Moderator }
        class AuthorisationRequest { public string UserName; public string Password; public string IPAddress; public string TwoFactorCode; }
        ///<see cref = "InJAVA"/>
        struct Authorizer : Host
        {
            public class AuthorisationConfirmed { public Role Role; public string UserName; public long LastLogin; public string ConfirmationToken; }
            public class AuthorisationRejected { public string UserName; public string Reason; public int FailedAttempts; }
        }
        interface ConnectionToMetrics : Connects<Server, Metrics>
        {
            [l____________<MetricsData>]
            struct Start { }
        }
        interface ConnectionToAuthorizer : Connects<Server, Authorizer>
        {
            [l____________<AuthorisationRequest>]
            [____________r<(Authorizer.AuthorisationConfirmed, Authorizer.AuthorisationRejected)>]
            struct Start { }
        }
    }
}
```
- NOTE: adds hosts `Metrics` (C#) and `Authorizer` (Java), connections from imported `Server`: `ConnectionToMetrics` (server reports), `ConnectionToAuthorizer` (server asks, authorizer answers with one of two packs).
- CLI: `AdHocAgent .\AdHocProtocolWithBackend.cs .\AdhocProtocol.cs`. Hosts numbered from 4 on, after the four hosts of `AdHocProtocol`.

## §22.2 Importing single declarations with `_<>`
KW: _<>, _<(A)>, tuple, broadcast, DataType, X<>, imports the host, the host is left out
- DECL: project base list may carry `_<...>` items beside extended projects; each names one declaration; several → C# tuple. One type written without parentheses: `_<(A)>` = one-element tuple, C# does not compile it.
```csharp
	public interface AdHocProtocol :
        _<
            AdHocProtocol.Agent.Project.Host.Pack.Field.DataType //propagate DataType constants set to all hosts
        >{
```
- RULE: items processed left to right. Effect by what `_<T>` names:

| `_<T>` names | Effect |
|:--|:--|
| enum or constant set | broadcast to EVERY host of project, even when `T` declared inside one host. (Project-level enums/constant sets reach every host anyway; `_<>` matters for one declared inside a host, like `DataType`.) →§6 |
| connection | added to project. Its two hosts must be hosts of the project — own or of an extended project (host comes into a project only with project declaring it). Connection whose host comes from a non-extended project stops run: "The connection … joins the host … of the project …, which … does not extend" |
| host | ignored, warning "The project ... imports the host ... (...): a project imports enums, constants sets, connections and projects, so the host is left out. A host takes part through the connections that name it." |
| pack | ignored, similar message. Pack of another project needs no import: name it in a branch of own state → sent |

- RULE: project-level `X<...>` takes out what project or extended one brings, after merge: `interface App : Lib, X<Lib.Base>` drops connection `Lib.Base`; `X<Lib.Watch>` drops host + every connection joining it; `X<Lib.Codes>` drops enum/constant set from broadcast. Own host by full name too (`X<App.Spare>`): C# resolves base list outside interface body. `_<OtherProject>` takes nothing (warning): extend it instead.

## §22.3 Modifying imported declarations: `Modify<>`
KW: Modify<Target>, org.unirail.Meta.Modify, modifier, Modify<Header>, Modify<Connection>, Modify<Connection, L, R>, impact report
- `modifier` = own declaration implementing `org.unirail.Meta.Modify<Target>`; changes `Target` in place as if written in Target's own declaration, without touching its file.
- RULE: modifier is not a protocol message: name the target, never the modifier, in a branch. Modifier gets a line in packs table (like every pack), no wire id.

| Target | Modifier | Can change |
|:--|:--|:--|
| pack | `class M : Modify<Pack> { }` | fields: add, remove, replace; attributes: replace whole set |
| pack header | `class M : Modify<Header> { }` | its fields and packs it is put into →§8 |
| enum / constant set | `class M : Modify<Set> { }` | remove members |
| host | `struct M : Modify<Host> { }`, directly in project | add languages and implementation settings →§10 |
| connection | `interface M : Modify<Connection> { … }`, `Modify<Connection, L, R>` | add actors, RPC methods, Actor0 states; remove states; attributes: replace whole set; hosts →§22.3.6 |

- RULE: modifiers change declaration for whole composition; every host built with it sees change. Field/attribute edits change wire for every host sending/receiving pack → run impact report (→§21) before deploy.
- RULE: a state is not modified: state modifier (`struct M : Modify<State>`) stops run (exit 22). Add a state to the connection instead →§22.3.6.

### §22.3.1 Modify<> for packs: fields
KW: see cref +, see cref -, _<Some.Pack>, X<Some.Pack>, add field, remove field, replace field, field order, wire order
- DECL: pack modifier = class; its doc comment and base list say what to add/remove:

| Write on modifier | Effect on target |
|:--|:--|
| `/// <see cref="Some.Pack.field"/>+` (or no sign) | adds that field |
| `/// <see cref="Some.Pack.field"/>-` | removes that field. To remove target's own field name it through target: `<see cref="Target.field"/>-` |
| `/// <see cref="Some.Pack"/>+`, or `_<Some.Pack>` in base list | adds all fields of `Some.Pack` |
| `/// <see cref="Some.Pack"/>-`, or `X<Some.Pack>` in base list | removes fields target has from `Some.Pack` — inherited or added earlier →§8 |

- NOTE: `+`/`-` after `<see cref/>` = doc-comment markers, not pack tags.
- WIRE: added fields go to end of target's fields, after its own; field order = wire order. Field whose name target already has REPLACES target's field in its place. Fields accumulate across every layer (each project in import chain may add).
```csharp
using org.unirail.Meta;
namespace com.my
{
    public interface MyProject : com.lib.Library
    {
        class Sample { long value; }
        class WideZone { long zone; }
        /// <see cref="com.lib.Library.Trace.origin"/>+
        class StampMore : Modify<Stamp>, _<Sample> { }   // adds origin, then value
        /// <see cref="WideZone.zone"/>+
        class StampWide : Modify<Stamp> { }              // zone becomes long, in its place
        /// <see cref="com.lib.Library.Trace.hops"/>-
        class TraceCut : Modify<Trace> { }               // Trace loses hops
        // Metrics, ServerToMetrics as in §22.1.1
    }
}
```
- WIRE: result: library `Stamp { long time; int zone; }` travels as `time`, `zone` (now `long`), `origin`, `value` — for every host of composition, incl. library's own `Client`, `Server`; `Trace` travels without `hops`.
- RULE: several modifiers may edit fields of one target. Fields declared in body of pack modifier added too, before its references.

### §22.3.2 Modify<> for packs: replacing the attributes
KW: attribute replacement, fields merge attributes replace, replaceable attribute, [Zstd(9)], topmost layer wins, composition order, exit code 2, stream chain log
- RULE: transform chain (→§16) and trim (→§17) are attributes on their declaration; base author fixed them before your project, could not name your connections → modifier restates them. Fields merge, attributes replace:

| Modifier declares | Target's attributes become |
|:--|:--|
| no replaceable attribute | unchanged |
| one or more replaceable attributes | exactly the modifier's: whole set replaced, never merged |
| `[ClearAttributes]` | empty |

```csharp
        // in MyProject : com.lib.Library   (library declares Trace [Zstd])
        [Zstd(9)]
        class TraceChain : Modify<Trace> { }
```
- NOTE: whole set replaced because attribute list ordered and position-bearing: chain means what it means only as a whole → redeclaring layer states all of it (every stage, parameter, trim); keeps effective declaration in one place. Attributes MOVE: modifier keeps none afterwards.
- CLI: every replacement logged with removed and installed set; second line = resulting chain:
```text
INF  The attributes of "com.lib.Library.Trace" are replaced by the modifier "com.my.MyProject.TraceChain" (line 22): ["Zstd"] -> ["Zstd(9)"]
INF  stream chain on "com.lib.Library.Trace" (line 40): "leaf -> Zstd(level=9)[compression] -> wire"
```
- NOTE: base project quietly changing compression level shows as log difference, not bytes that stop decoding. Impact report of this edit reaches `Client` and `Server` (hosts handling `Info`, which carries `Trace`).
- NOTE: works same on own pack; live `Stream.cs` re-cuts one:
```csharp
        [ToStream<LoggingEndpoints>, Zstd]
        class TrimByModify : Modify<ModifiedPayload>{ }
```
  log `[""] -> ["ToStream<LoggingEndpoints>, Zstd"]` (target declared no attributes); chain and cut land on `ModifiedPayload`, modifier keeps neither.
- NOTE: re-cutting imported pack = how extension forwards stored payload over connection base never knew: store keeps received bytes, injects later un-parsed and un-recompressed; far end runs inverse stages and rebuilds typed pack. Replacement as `Stream<To, From>` pair → both cuts at one depth (store-and-replay needs it) →§17.
- RULE: replaceable attributes = transform stages and flows, trims (`ToStream`, `FromStream`, `Stream<To, From>`), `[ClearAttributes]`, and every attribute not internal to AdHoc: `[TransmitTimeout]`, `[ReceiveTimeout]`, `[Deadline]`, `[Idle]`, `[Grouped]`, `[Resumable]`, own custom attributes →§23. Other `org.unirail.Meta` attributes (`[D]`, `[S]`, `[MinMax]` …) and `System` attributes (`[Flags]`) do not trigger replacement on their own. Branch attributes (`L____________`, `l____________`, `____________R`, `____________r`, `_____lr_____`) never take part (they spell transitions = state's body) — replacement neither counts nor drops them.
- RULE: topmost layer wins: when several projects modify one target, modifier of project that imports the others (directly/transitively) decides; others overruled. Two replacements whose projects do not import one another (side-by-side projects, or two modifiers in one project) → run stops, exit code 2:
```text
ERROR  Both 'A.ReChain' (project A) and 'B.ReChain' (project B) replace the attributes of Payload,
       and neither project imports the other, so which one wins would be decided by composition order.
       Keep one of them, or move the replacement into a project that imports both.
```
- NOTE (diagram): Library `Trace [Zstd]` ← `Middle : Library` modifier adds field + `[Zstd(3)]` ← `MyProject : Middle` modifier `[Zstd(9)]`: fields of all layers accumulate; only topmost set `[Zstd(9)]` reaches final Trace.
- PITFALL: replacement restates a format something may already have stored. Everything pinned left of a cut (stages, order, design-time parameters) must still match what wrote those bytes →§17. Nothing on wire announces mismatch; reader just fails. Treat replaced chain as published format; keep layers in step.

### §22.3.3 Clearing the attributes of an imported pack: `[ClearAttributes]`
KW: [ClearAttributes], empty attribute set, TracePlain, carries [ClearAttributes] together with other attributes
- RULE: modifier with no attributes = "leave target's attributes alone" (needed for field-only modifiers) → stripping needs a marker:
```csharp
[ClearAttributes]
class TracePlain : Modify<Trace> { }   // Trace: no chain, no trim
```
- CLI: log `["Zstd"] -> [""]`.
- RULE: removes what target's own declaration carries: chain, trims, replaceable metadata. Does not touch anything a field declares (replace a field by bringing in field of same name). Marker has no meaning of its own, never reaches target; only makes attribute list non-empty while denoted set is empty.
- RULE: written next to another replaceable attribute → refused: "The modifier '...' carries `[ClearAttributes]` together with other attributes ..." (exit code 2). Next to non-replaceable attributes → accepted.

### §22.3.4 Modify<> for enums and constant sets
KW: Modify<Limits>, remove members, enum modifier, constant set modifier
- DECL: modifier of enum / constant set = class; can remove members:
```csharp
/// <see cref="com.lib.Library.Limits.MaxZone"/>-
class LimitsEdit : Modify<Limits> { }
```
- RULE: `struct` or `enum` cannot be modifier. Adding members through modifier not processed: declare an additional constant set. Enums/constant sets root project contains reach hosts per →§6.

### §22.3.5 Modify<> for hosts
KW: Modify<Server>, host modifier, add language, InTS, languages InCS → InCS, InTS
- DECL: host modifier = `struct` directly in project, first base `Modify<ImportedHost>`. Languages in its doc comment added to target's; its configuration appended to target's:
```csharp
/// <see cref = 'InTS'/>
struct ServerTS : Modify<Server> { }   // Server: generated in C# and now also in TypeScript
```
- CLI: impact report shows `languages InCS → InCS, InTS` on host. Host configuration →§10.

### §22.3.6 Modify<> for connections
KW: connection modifier, Modify<Connection>, Modify<Connection, L, R>, behavior module, add actor, add RPC method, add state, remove state, layers, slot, two actors named, gets two actors named
- DECL: connection modifier = interface, directly in project; body declared as a connection body:
```csharp
// App.cs - the root: core + two behavior modules
public interface App : Mail, Call { }
// Core.cs
public interface Core
{
    /// <see cref="InTS"/>
    struct Participant : Host { public class Card { uint version; } }
    interface Link : Connects<Participant, Participant>
    {
        [_____lr_____<Participant.Card>] struct Cards { }
    }
}
// Mail.cs
public interface Mail : Core
{
    class Deposit { uint size; }  class Stored { uint id; }  class Letter { uint id; }  class Note { uint id; }

    /// <see cref='Core.Link.Cards'/>-              // takes a state away
    [ReceiveTimeout(90)]                             // replaces the attribute set of Link
    interface LinkMail : Modify<Core.Link>
    {
        (L____________, Stored) deposit(Deposit d);  // RPC method → actor of Link
        interface Inbox : Actor                       // named actor → actor of Link
        {
            int Actor.MaxActiveInstances => 1;
            [L____________<Open, Letter>] struct Start { }
            [____________r<Letter>]       struct Open { }
        }
        [_____lr_____<Note>] struct Notes { }        // state → state of Link's Actor0
    }
}
// Call.cs
public interface Call : Core
{
    class Ring { uint caller; }
    interface LinkCall : Modify<Core.Link> { [_____lr_____<Ring>] struct Rings { } }
}
```

| In modifier | Effect on connection |
|:--|:--|
| named actor, RPC method in body | actor of connection |
| state in body | state of connection's Actor0 (one valid state machine with existing ones →§12.7) |
| `/// <see cref='Conn.State'/>-`, `X<Conn.State>` in base list | state taken away |
| `/// <see cref='Base.State'/>`, base connection in base list | states copied in, as composition does →§11.5 |
| any attribute (chain, `[Resumable]`, `[ReceiveTimeout]`…) | replaces whole attribute set of connection →§22.3.2 |
| `Modify<Conn, L, R>` | `L`, `R` take places of its hosts |

- RULE: layers: what a project adds is numbered in its own numbers tables (`actors`, `states`, branch marks); tables and wire ids of a project below never change with what is built on it - each project writes them as it sees connections: its own layer and those below.
- GEN: actor/state a project adds to another project's connection: number given to generator carries adding project's slot → custom-code regions of actors two modules add to one connection never share a key. WIRE: actor address by its place among connection's actors, unchanged.
- RULE: two projects adding one actor name or one state name to one connection → error "The connection … gets two actors named …: … rename one of them." (generated code names them by name).
- RULE: modifiers in sibling projects (`App : Mail, Call`) may both add to one connection; attribute replacement by both → error (→§22.3.2).
- RULE: `Modify<Conn, L, R>` = hosts replaced + body/attributes as `Modify<Conn>`; connection moves for whole composition: project seeing connection but not extending project declaring new host → error "The connection … joins the host … of the project …, which … does not extend". Use states between other hosts AS WELL → compose own connection →§11.5.
- RULE: modifier interface declared directly in project (else "is declared inside another declaration: declare it directly in the project").
- CLI: run root with all files: `AdHocAgent App.cs Mail.cs Call.cs Core.cs` → `Link` carries actor of RPC `deposit`, actor `Inbox`, Actor0 states `Notes`, `Rings`; `Cards` gone; attribute set `[ReceiveTimeout(90)]`.
- NOTE: an existing state is not modified (its branches stay); a host built only with the lower project does not know actors added above it.

## §22.4 Pitfalls of reuse
KW: pitfalls, X<> no filter, does not declare, body fields merged, appended fields, renumber hosts
- RULE: imports come whole, then project-level `X<>` filters: connection, host with its connections, enum or constant set.
- PITFALL: name imported declarations by full name in doc comments; `<see cref/>` does not find types through extended interfaces; unresolved one stops run with "... refers to `...`, which the description does not declare".
- RULE: modifier body fields and constants merged into target (before its refs and base list): new name appended; name target already has → replaces that field in place (`class RM : Modify<Reading> { string v; }` → `Reading.v` string).
- PITFALL: added fields appended after target's own → wire change for every host of the pack.
- PITFALL: one replacement per target per layer: two attribute replacements of one target in one project, or in two projects not importing each other, stop run.
- PITFALL: library's file can be rewritten (its numbers tables maintained by every run of extending project); keep it writable and under version control.
- PITFALL: connection to host of non-extended project stops run; extend that project to get the host.
- PITFALL: do not name a modifier in a branch; name its target.
- PITFALL: growing library can renumber your hosts →§20.

# §23 Attributes
KW: attribute, built-in, custom, Meta.cs, org.unirail.Meta, constants, interpreted, emitted
SUMMARY: Reference: description → attributes (built-in = interpreted by agent; custom = carried as constants) → generated code. Catalog of all built-in attributes, other `Meta.cs` declarations, full rules of custom attributes.
- `attribute` = C# attribute on entity of description: project, host, pack, field, connection, actor, state.
- `built-in attributes` = declared in `Meta.cs`, namespace `org.unirail.Meta`; agent and generator interpret them: change field encoding, add transform chain, set timeout, make session outlive socket, declare state-machine branch, filter pack set, strip attributes in modifier. Most change generated code; only some change wire bytes.
- `custom attributes` = classes you declare; not interpreted; every applied one → constants of entity it is written on, read by your code at runtime. Never changes wire.

## §23.1 Where attributes attach
KW: entity, attach, project, host, pack, field, static field, connection, multiplexer, actor, state, Pack Set, modifier, [Flags]

| Entity | Built-in attributes it takes | Custom attributes |
|:--|:--|:--|
| Project (project interface) | - | yes, constants of project |
| Host (`struct X : Host`) | - | yes, constants of host |
| Pack (class) | transform stages and flows, trims, `[S]` on `Stream`/`File` pack | yes, constants of pack |
| Field of pack | `[MinMax]`, `[A]`, `[V]`, `[X]`, `[D]`, `[S]`, `[Key:]`/`[Val:]` targets, transform stages and flows, trims | yes, constants of field |
| `static` field | `[ValueFor]` | - |
| Connection (`interface X : Connects<L, R>`) | `[ReceiveTimeout]`, `[Resumable]`, `[UDP]`, `[UDP<…>]`, transform stages and flows | yes, constants of connection |
| Multiplexer (`interface X : Multiplex<(…)>`) | - | not read; write constants in its body |
| Actor (`interface X : Actor`, or RPC method) | `[Idle]`, `[Grouped]` | yes, constants of actor |
| State (empty struct in connection or actor) | five branch attributes, `[TransmitTimeout]`, `[ReceiveTimeout]`, `[Deadline]`, `[Idle]`, `[Resumable]` | yes, constants of state |
| Named Pack Set, filter template | `[KeepName]`, `[KeepDoc]`, `[SkipName]`, `[SkipDoc]` | - |
| `Modify<>` modifier | `[ClearAttributes]`; chains, trims, timing attributes on modifier replace target's attribute set | yes, they replace target's set |

- RULE: branch = not an entity; it is a branch attribute on a state → metadata for it goes on the state. Branch attributes never take part in modifier attribute replacement →§22.3.2.
- RULE: enum takes one attribute: `[Flags]` from `System` →§23.2.8. Custom attributes on enum or constants container not read: give metadata as constants in its body.
- NOTE (diagram): multiplexer beside connection rung takes constants only.

## §23.2 Built-in attribute catalog
KW: catalog, Meta.cs, goes on, signature, constructor
- Every attribute `Meta.cs` declares for descriptions, grouped by purpose; signature = constructor you write; "goes on" = entity parser accepts it on.

### §23.2.1 Built-in field encoding attributes
KW: [MinMax], [A], [V], [X], [D], [Key:], [Val:], [S], [ValueFor], varint, zig-zag, pack header

| Attribute | Goes on | Does | → |
|:--|:--|:--|:--|
| `[MinMax(min, max)]` | numeric primitive field (or items) | bounds value; stored as `value - min` in fewest bits/bytes span needs; integer (`long`, `ulong`) and floating-point (`double`) forms | §7 |
| `[A(minMostProbableValue, max)]` | integer primitive field (or items) | varint for values concentrated near minimum; omitted bound derived from field type | §7 |
| `[V(maxMostProbableValue, min)]` | integer primitive field (or items) | varint for values concentrated near maximum | §7 |
| `[X(amplitude, zero)]` | integer primitive field (or items) | zig-zag varint for values spread around `zero` | §7 |
| `[D(dims)]` | array, string, `Map`, `Set` field | lengths/dimensions: `N` array length, `+N` length of string/map/set, `-N` constant dimension, `~N` fixed dimension | §5 |
| `[Key: …]`, `[Val: …]` | `Key:` on `Map`/`Set` field, `Val:` on `Map` field | attribute targets: field encoding attributes after them apply to key / value, e.g. `[Key: D(+30)]`, `[Val: X]` | §5 |
| `[S(maxLength)]` | bare `Stream`/`File` field; pack extending `Stream` or `File` | size cap of raw conduit in bytes; must be > 8 | §15 |
| `[ValueFor(CONST)]` | `static` field | copies static field's computed value into named `const` at generation time; overloads for `long`, `ulong`, `double`, `bool`, `string` constants | §6 |

- RULE: at most one of `[MinMax]`, `[A]`, `[V]`, `[X]` per field. `[A]`, `[V]`, `[X]`, `[D]` refused on fields of pack header; `[MinMax]` allowed there →§8.

### §23.2.2 Built-in transform-chain and trim attributes
KW: [Zstd], [ChaCha20], StreamStageAttribute, StreamCompressionStageAttribute, StreamCipherStageAttribute, StreamFlowAttribute, [ToStream<E>], [FromStream<E>], [Stream<To, From>], StreamTrimAttribute

| Attribute | Goes on | Does | → |
|:--|:--|:--|:--|
| `[Zstd]`, `[Zstd(level)]` | field, pack, connection | Zstandard compression stage; `level` 1..22, default 3, used by encoder only | §16 |
| `[ChaCha20]`, `[ChaCha20(key, iv)]` | field, pack, connection | ChaCha20 cipher stage; `(Binary[,] key, Binary[,] iv)` form declares shape of runtime-injected secrets | §16 |
| class derived from `StreamStageAttribute`, `StreamCompressionStageAttribute` or `StreamCipherStageAttribute` | field, pack, connection | own stage; base class gives role: plain transform, the one compressor, the one cipher of a chain | §16 |
| class derived from `StreamFlowAttribute` | field, pack, connection | named reusable chain: stages written on the class, applied by one attribute | §16 |
| `[ToStream<E>]` | field, pack | trim: receiver at endpoint `E` stops unwrapping here, takes raw bytes | §17 |
| `[FromStream<E>]` | field, pack | trim: sender at endpoint `E` supplies raw bytes here | §17 |
| `[Stream<To, From>]` | field, pack | both trims at one depth: store-and-replay pair | §17 |

- RULE: chain stages written in dataflow order, left (payload) → right (wire). Trims derive from `StreamTrimAttribute`; deriving own trim from it = error.

### §23.2.3 Built-in connection and session attributes
KW: [Resumable(seconds)], [Resumable], [Resumable(0)], [UDP], [UDP(seconds)], [UDP<EXCLUDE_PACKS_SET>], session, guaranteed

| Attribute | Goes on | Does | → |
|:--|:--|:--|:--|
| `[Resumable(seconds)]` | connection, state | how long session outlives socket, parked waiting for peer; on state overrides connection's value; `[Resumable(0)]` turns it off there | §14 |
| `[Resumable]` | state | hold session in this state for connection's seconds | §14 |
| `[UDP(seconds)]`, `[UDP]` | connection | carried over UDP, every pack guaranteed both ways; `seconds` as in `[Resumable(seconds)]` | §14 |
| `[UDP<EXCLUDE_PACKS_SET>(seconds)]`, `[UDP<EXCLUDE_PACKS_SET>]` | connection | as `[UDP]`, but packs of the set travel unguarded | §14 |

### §23.2.4 Built-in actor and state timing attributes
KW: [TransmitTimeout], [ReceiveTimeout], [Deadline], OnDeadline, [Idle], lease, [Grouped], group

| Attribute | Goes on | Does | → |
|:--|:--|:--|:--|
| `[TransmitTimeout(seconds)]` | state | transmit timeout of whole connection, written onto connection when an instance enters state | §11 |
| `[ReceiveTimeout(seconds)]` | connection | link: how long peer may be silent on socket; 60 s when absent, kept alive by `Pulse_` | §11.7 |
| `[ReceiveTimeout(seconds)]` | state | stage: how long this side waits in state for a pack of peer; set when instance enters state, no limit when absent | §11.7 |
| `[Deadline(seconds)]` | state | how long one actor instance may stay in state; then `OnDeadline` fires, by default pair ends | §13 |
| `[Idle(seconds)]` | actor, state | lease: instance ends when none of its packs came from peer for that long | §13 |
| `[Grouped]` | actor | instances can join a group; one send reaches all | §13 |

### §23.2.5 Built-in branch attributes
KW: l____________, L____________, ____________r, ____________R, _____lr_____, TARGET_STATE, PACKS, End, Close, KeepDoc, SkipDoc, KeepName, SkipName

| Attribute | Goes on | Does | → |
|:--|:--|:--|:--|
| `[l____________]`, `[l____________<PACKS>]` | state | packs left host may send; machine stays in state | §12 |
| `[L____________<TARGET_STATE>]`, `[L____________<TARGET_STATE, PACKS>]` | state | packs left host sends, then machine moves to `TARGET_STATE` (`End`, `Close` included) | §12 |
| `[____________r]`, `[____________r<PACKS>]` | state | packs right host may send; machine stays | §12 |
| `[____________R<TARGET_STATE>]`, `[____________R<TARGET_STATE, PACKS>]` | state | packs right host sends, then machine moves to `TARGET_STATE` | §12 |
| `[_____lr_____]`, `[_____lr_____<PACKS>]` | state | packs either host may send; machine stays | §12 |

- RULE: every branch attribute may repeat on a state; takes four optional regexp arguments `(KeepDoc, SkipDoc, KeepName, SkipName)` filtering packs it selects (matching rules →§9).

### §23.2.6 Built-in Pack Set filter attributes
KW: [KeepName], [SkipName], [KeepDoc], [SkipDoc], regexp, Named Pack Set, filter template, name path

| Attribute | Goes on | Does |
|:--|:--|:--|
| `[KeepName(regexp)]` | Named Pack Set, filter template | keeps only packs whose name path matches |
| `[SkipName(regexp)]` | Named Pack Set, filter template | removes packs whose name path matches |
| `[KeepDoc(regexp)]` | Named Pack Set, filter template | keeps only packs whose documentation and tags match |
| `[SkipDoc(regexp)]` | Named Pack Set, filter template | removes packs whose documentation and tags match |

- RULE: each may repeat. Matched name = pack's path below project, without namespace. Empty or invalid pattern = error. →§9.

### §23.2.7 Built-in modifier attributes
KW: [ClearAttributes], Modify<>

| Attribute | Goes on | Does |
|:--|:--|:--|
| `[ClearAttributes]` | `Modify<>` modifier | replaces target's attribute set with empty one; refused together with any other attribute on same modifier (→§22.3.3: accepted beside non-replaceable ones) |

### §23.2.8 Attributes from System
KW: [Flags], System.FlagsAttribute, [AttributeUsage], bit flags

| Attribute | Goes on | Does |
|:--|:--|:--|
| `[Flags]` | enum | enum = set of bit flags (`System.FlagsAttribute`) →§6 |
| `[AttributeUsage]` | own attribute class | where custom attribute may be placed, whether it may repeat; never emitted →§23.4.1 |

### §23.2.9 Built-in timing attributes as generated constants
KW: resume_, -1, ReceiveTimeout constant, Grouped constant, Idle constant, java.time.Duration, emitted
- GEN: built-ins interpreted, not emitted — except timing attributes: reach generator as constants of state, actor or connection they are on, named after attribute: `[TransmitTimeout]`, `[ReceiveTimeout]`, `[Deadline]`, `[Idle]`, `[Grouped]`, `[Resumable]`.
- GEN: `[Resumable]` → constant `resume_`. On state: seconds, or `-1` for argument-less form (keeps connection's value). On connection: connection's own `resume_` with connection's seconds; `[UDP(seconds)]` connection without `[Resumable]` gets same constant from its `[UDP]` seconds. Generated session code built from these constants.
- GEN: Java, state with `[ReceiveTimeout(60)]` and `[Grouped]`, `[Idle(60)]` actor:
```java
public static final java.time.Duration ReceiveTimeout = java.time.Duration.ofSeconds(60);   // in the state's class
public static final boolean Grouped = true;                                                 // in the actor's class
public static final long    Idle    = 60;
```
- NOTE: your code may read them as any constant. Same names reserved for custom attributes →§23.5.1.

## §23.3 Other Meta.cs declarations
KW: Host, InCS, InJAVA, InTS, Connects<L, R>, SwapHosts, VirtuallyConnects, Multiplex, Actor, MaxActiveInstances, End, Close, Resumable<HOST, PACKS>, IfSendingFrom, HeaderFor, FieldsInjectInto, Binary, longJS

| Declaration | What it is | → |
|:--|:--|:--|
| `Host` | struct extending it = host | §10 |
| `InCPP`, `InRS`, `InCS`, `InJAVA`, `InGO`, `InTS`, `InSwift`, `InC`, `InDart`, `InPy` | language markers for host, referenced in `<see cref>` doc comments | §10 |
| `Connects<L, R>` | connection between two hosts | §11 |
| `SwapHosts<Connection>` | connection with left/right hosts of another swapped; written only as `<see cref='SwapHosts{Base}'/>` in connection's doc comment | §11 |
| `VirtuallyConnects<L, R, PATH>` with `MaxTunnels`, `MaxStream_KiloBytes` | tunnel between two hosts across relay hosts | §18 |
| `Multiplex<CONNECTIONS>` | one port of a host for several of its connections | §19 |
| `Actor` with `MaxActiveInstances`, `UNLIMITED` | actor and how many instances it may have | §12, §13 |
| `L____________`, `____________R` (interfaces) | markers in return tuple of RPC method: which side calls | §13 |
| `End`, `Close` | terminal target states: end actor pair / close connection | §12 |
| `Resumable<HOST, PACKS>` with `resumable_megabytes` | guaranteed delivery of packs a host sends over connection | §14 |
| `IfSendingFrom<fromHost, viaConnection>` | endpoint: host sending over connection; argument of a trim | §17 |
| `FieldsInjectInto<PackSet>`, `HeaderFor<PackSet>` | fields injected into packs; pack header | §8 |
| `_<TYPES>`, `X<TYPES>` | pack set, exclusion | §9 |
| `Modify<Target>`, `Modify<TargetConnection, L, R>` | modifier of imported pack, pack header, enum, constant set, host or connection; 3-argument form gives connection other hosts | §22 |
| `Stream`, `File` | raw conduit types | §15 |
| `Binary` | byte type; generated code handles signedness per language | §5 |
| `longJS`, `ulongJS` | 64-bit integers kept within range JavaScript `number` holds exactly | §5 |
| `Set<K>`, `Map<K, V>` | collection field types | §5 |
| `DateTimeDef`, `Duration`, `TimeSpanDef` | time-value encodings | §5 |
| `___IConnects`, `___IfSendingFrom` | internal base interfaces; do not use in description | - |

## §23.4 Custom attributes
KW: custom attribute, metadata, constants, routing tags, UI labels, ownership, retention, validation hints
- `custom attribute` = own metadata: declare as ordinary C# attribute class, apply to entities, generator turns each applied attribute into constants of that entity. Uses: routing tags, UI labels, ownership, retention periods, validation hints read by runtime. Wire format identical with or without them.

### §23.4.1 Declaring a custom attribute
KW: System.Attribute, XxxAttribute, [AttributeUsage], AttributeTargets, AllowMultiple, positional, named argument, property, params, typeof, Constant field cannot have type
- DECL: class deriving from `System.Attribute`, directly or through another attribute class; never a pack, no scope (`@MyProject`) takes it in. Declared inside project interface (typically together at its end). Live example (used on a state and an actor):
```csharp
/// <summary>A custom attribute: the generator keeps it as a constant of what it is applied to.</summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Interface)]
class LeaseAttribute : Attribute
{
    public LeaseAttribute(int seconds) { }
}
```
- RULE: standard C# conventions, generator relies on them:
  - class `XxxAttribute` applied as `[Xxx]`; generated constants named without suffix.
  - constructor parameters = positional args; their names = names of generated constants when attribute has several args.
  - public properties = named args (`Name = value`); only properties you set are emitted.
  - `[AttributeUsage(AttributeTargets....)]` restricts placement; `AllowMultiple = true` lets it repeat on one entity. Without `[AttributeUsage]` allowed on any entity, once. `[AttributeUsage]` never emitted.

| Technique | Declaration | Applied as | Generated constants |
|:--|:--|:--|:--|
| one parameter | `class TitleAttribute : Attribute { public TitleAttribute(string text) { } }` | `[Title("Shop protocol")]` | `Title = "Shop protocol"` on entity |
| several parameters | `class OwnerAttribute : Attribute { public OwnerAttribute(string name, string email) { } }` | `[Owner("Platform team", "platform@example.com")]` | container `Owner` holding `name`, `email` |
| overloaded constructors | `DefaultAttribute(long value)`, `DefaultAttribute(double value)`, `DefaultAttribute(string value)`, `DefaultAttribute(bool value)` | `[Default(0)]`, `[Default("View")]`, `[Default(false)]` | `Default`, typed by overload compiler picked |
| properties (named args) | `class ValidateAttribute : Attribute { public string Regex { get; set; } = ""; public long MaxLength { get; set; } = -1; }` | `[Validate(Regex = @"^[a-z]+$")]` | `Regex = "^[a-z]+$"` on entity |
| positional and named | `class AuditAttribute : Attribute { public AuditAttribute(string journal) { } public int Retention { get; set; } = 7; }` | `[Audit("orders", Retention = 30)]` | container `Audit` holding `journal`, `Retention` |
| marker (no params) | `class ReadOnlyAttribute : Attribute { }` | `[ReadOnly]` | `ReadOnly = true` on entity |
| repeatable | `[AttributeUsage(AttributeTargets.All, AllowMultiple = true)] class TagAttribute : Attribute { public TagAttribute(string name, string note) { } }` | two `[Tag(...)]` lines on one entity | container `Tag` with `_0`, `_1`, each holding `name`, `note` |

- RULE: optional constructor parameter left out at use site → its default value: `[Tag("x")]` with `TagAttribute(string name, int level = 3)` → `name = "x"`, `level = 3`. Arguments may be written by name (`[Pair(b: 2, a: 1)]`); `params` list = one array constant.
- RULE: arguments the constructor takes (defaults included) + properties set decide inline constant (one value) vs container (two or more). Property left out → not emitted.
- RULE: argument becomes constant → type must be constant-able: `bool`, integer type, `char`, `float`, `double`, `string`, or array of one of them. Enum value → its number (`AttributeTargets.Class` → `4`); `typeof(X)` → full name of `X` as string (`typeof(int)` → `"System.Int32"`).

### §23.4.2 Applying custom attributes
KW: apply, project, host, pack, field, connection, actor, RPC method, state, TYPEDEF, stack, Modify<>
- DECL: custom attribute may go on project, host, pack, field, connection, actor, RPC method, state. Positional values → constructor params; `Name = value` → property. Stack freely with each other and with built-ins (`[D(...)]`). Every placement:
```csharp
using System;
using org.unirail.Meta;
namespace com.example.shop
{
    [Title("Shop protocol")]                                   // PROJECT: one argument
    [Owner("Platform team", "platform@example.com")]           // PROJECT: two arguments
    public interface ShopProtocol
    {
        ///<see cref = 'InJAVA'/>
        [Region("eu-west")]                                    // HOST
        struct Backend : Host
        {
            [Audit("orders", Retention = 30)]                  // PACK: positional and named arguments
            public class Order
            {
                [ReadOnly] long id;                            // FIELD: marker
                [D(+64)] [Pattern(@"^[A-Z0-9-]+$")] string sku; // FIELD: built-in [D] stacked with a custom attribute
            }
            [Tag("billing", "issued once per order")]
            [Tag("audit", "kept for seven years")]             // PACK: repeated, needs AllowMultiple = true
            public class Invoice { long order_id; }
            public class Price { long cents; }
        }
        ///<see cref = 'InTS'/>
        struct Client : Host
        {
            public class PriceQuery { long order_id; }
        }
        interface Orders : Connects<Client, Backend>
        {
            [Lease(30)]                                        // STATE
            [_____lr_____<(Backend.Order, Backend.Invoice)>]
            struct Ready { }
            [Tag("rpc", "price lookup")]                       // RPC METHOD: constants of the actor made from it
            (L____________, Backend.Price) price(Client.PriceQuery query);
        }
        class TitleAttribute : Attribute { public TitleAttribute(string text) { } }
        class OwnerAttribute : Attribute { public OwnerAttribute(string name, string email) { } }
        class RegionAttribute : Attribute { public RegionAttribute(string name) { } }
        class AuditAttribute : Attribute { public AuditAttribute(string journal) { } public int Retention { get; set; } = 7; }
        class ReadOnlyAttribute : Attribute { }
        class PatternAttribute : Attribute { public PatternAttribute(string regex) { } }
        [AttributeUsage(AttributeTargets.All, AllowMultiple = true)]
        class TagAttribute : Attribute { public TagAttribute(string name, string note) { } }
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Interface)]
        class LeaseAttribute : Attribute { public LeaseAttribute(int seconds) { } }
    }
}
```
- RULE: actor takes custom attributes on its interface (`[Lease(120)] interface Job : Actor { ... }`). Attribute on RPC method → constant of actor generator makes from that method.
- RULE: field typed with a TYPEDEF also carries custom attributes written on TYPEDEF's own field (TYPEDEF →§5).
- RULE: `Modify<>` modifier carrying custom attribute, transform stage/flow, trim, timing attribute or `[ClearAttributes]` replaces target's whole attribute set, custom attributes included; branch attributes and field encoding attributes take no part →§22.3.2.

### §23.4.3 What the generator makes of a custom attribute
KW: constant, container, _0, _1, array constant, 255 elements, 1000 characters, CriticalPacket, public const, static final
- GEN: agent groups an entity's custom attributes by class, emits each group:

| Written on entity | Generated on entity |
|:--|:--|
| one attribute, one positional argument | constant named after attribute holding argument: `[Lease(30)]` → `Lease = 30` |
| one attribute, one named argument | constant named after property: `[Validate(Regex = "…")]` → `Regex = "…"` |
| one marker attribute | `bool` constant named after attribute, `true`: `[ReadOnly]` → `ReadOnly = true` |
| one attribute, two or more arguments | container named after attribute, one constant per argument, named after constructor parameter or property |
| same attribute ≥2 times (`AllowMultiple`) | container named after attribute, sub-containers `_0`, `_1`, … in written order, each holding that occurrence's args |
| array argument | array constant (≤ 255 elements; strings ≤ 1000 characters; no `null` element) |
| enum or `typeof(X)` argument | enum value's number; full name of the type, as string |

- GEN: pack `PlayerPosition` with `[CriticalPacket(true)]` (one `bool` param) → in generated pack class:
```csharp
public const bool CriticalPacket = true;                // C#
```
```java
public static final boolean CriticalPacket = true;      // Java
```
```typescript
const CriticalPacket = true;                            // TypeScript
```
- RULE: argument values must be compile-time constants (as any C# attribute); agent takes them as C# compiler evaluated them.
- RULE: custom attribute never interpreted for wire format. Only user-declared attribute class that changes bytes = class derived from `StreamStageAttribute` or `StreamFlowAttribute`: transform stage/flow, forms chain, produces no constants →§16.

### §23.4.4 Custom metadata as constants
KW: const, static, constant form, attribute form, multiplexer constants, state empty, [ValueFor]
- NOTE: on entity whose body may hold constants, attribute may be skipped and constant written directly; both generate same constant:
```csharp
[CriticalPacket(true)]
class PlayerPosition
{
    public float x;
}
```
```csharp
class PlayerPosition
{
    const bool CriticalPacket = true;
    public float x;
}
```

| Entity | Attribute form | Constant form |
|:--|:--:|:--|
| project, host, pack, connection | ✓ | ✓ (`const` or `static` in body) |
| multiplexer | - | ✓, only form |
| state | ✓, only form | - (state struct must stay empty) |
| field | ✓, only form | - (field has no body) |
| actor, RPC method | ✓ | - |

- RULE: computed value cannot be attribute argument: declare it as constant of entity and compute with `static` field marked `[ValueFor]` →§6.

## §23.5 How the agent tells built-in from custom
KW: classification, StreamStageAttribute, StreamFlowAttribute, StreamTrimAttribute, use-site name, Attribute suffix, org.unirail.Meta, System skipped, [Flags]
- RULE: agent classifies every attribute in order:
  1. transform stages, flows, trims — recognized by base class (`StreamStageAttribute` and its two role bases, `StreamFlowAttribute`, `StreamTrimAttribute`), built-in or own; form chains, never constants.
  2. field encoding attributes — by name written at use site (with `Attribute` appended if omitted): `S`, `D`, `MinMax`, `A`, `V`, `X` on a field, whatever namespace class comes from. `[Validate]` and `[ValidateAttribute]` = same attribute; `[X]` resolves to `XAttribute`. `[ValueFor]` recognized by name `ValueFor` on `static` field; `[S]` on pack by class name `SAttribute`.
  3. other built-ins — as classes of namespace `org.unirail.Meta`: branch attributes, Pack Set filters, `[UDP]`, `[ClearAttributes]`, timing attributes.
  4. everything else = custom → constants; except attributes of namespace `System` (e.g. `[AttributeUsage]`) and `[Flags]`, which are skipped. Of built-ins only timing attributes also become constants (`[Resumable]` as `resume_`).
- NOTE (diagram): custom constants inline for one argument, container for several, `_0`/`_1` sub-containers for repeated attribute.

### §23.5.1 Reserved attribute names
KW: reserved, S, D, MinMax, A, V, X, ValueFor, SAttribute, ResumableAttribute, resume_, TransmitTimeout, Zstd, ChaCha20, UDP
- RULE: do not give custom attribute a built-in's name. Names where name alone decides (agent takes yours for built-in):
  - on a field, `[S]`, `[D]`, `[MinMax]`, `[A]`, `[V]`, `[X]` (with or without `Attribute` suffix) read as built-in with built-in's argument rules, whichever class it resolves to;
  - on `static` field, `[ValueFor]` read as built-in; on `Stream`/`File` pack any class named `SAttribute` is;
  - class named `ResumableAttribute` → `resume_` constant session code is built from;
  - custom `TransmitTimeout`, `ReceiveTimeout`, `Deadline`, `Idle`, `Grouped` → constant with same name as built-in's.
- RULE: other built-in names (`Zstd`, `ChaCha20`, `ToStream`, `FromStream`, `Stream`, `UDP`, `KeepName`, `KeepDoc`, `SkipName`, `SkipDoc`, `ClearAttributes`, five branch names): same-named class of another namespace stays custom, but reader cannot tell apart. Treat whole list as reserved.

### §23.5.2 Pitfalls of custom attributes
KW: derive directly from Attribute, SkipName Attribute$, <@MyProject>, X<>, MaxLength, const in state
- PITFALL: metadata ≠ wire config: `[MaxLength(64)]` only produces constant; to bound string on wire use `[D(+64)]`.
- PITFALL: states stay empty: state metadata only as attribute; `const` inside state struct = error (state may contain nothing).
- NOTE: related →§6 (constants, `[ValueFor]`), §22 (attribute replacement, `[ClearAttributes]`), §16 (custom stages).

# §24 AdHocAgent
KW: AdHocAgent, CLI, received folder, generator server, UUID, Communication, AdhocProtocol.cs, Agent host, .NET
SUMMARY: Pipeline: description → AdHocAgent → generator server → generated code → smart merge and deployment → runtime. `AdHocAgent` = .NET command-line utility (your machine or build pipeline): task dispatch, `.cs`/`.cs!`/`.md`/`.cs~` tasks, `AdHocAgent.toml`, UUID, open source mode, env vars, exit codes, pipelines.
- NOTE: five jobs: (1) checks description locally, uploads to generator server (`.cs` task →§24.2); (2) receives generated code into received folder →§24.3.4; (3) deploys it into your projects, keeping code you wrote inside generated files →§25; (4) reports which hosts a description change reaches (`.cs~` →§24.6); (5) stores personal UUID and every new UUID server issues →§24.9.
- NOTE: access by personal UUID, not login/password; run needs nobody at keyboard → can be a delivery pipeline step →§24.13.
- NOTE: AdHocAgent is itself an AdHoc application: talks to server over `Communication` connection of `AdhocProtocol.cs`; generated code of its `Agent` host in `protocol/gen` of its repository, runtime library in `protocol/lib`. →§12 uses that connection as worked state machine.

## §24.1 How AdHocAgent chooses the task
KW: first argument, dispatch, .cs, .cs!, .cs~, .md, UUID, PersonalVolatileUUID, Unsupported file type, case-sensitive
- CLI: first argument decides; tests in fixed order, first match wins:

| # | Test on first argument | Task | Needs UUID |
|:--|:--|:--|:--|
| 1 | no arguments | print help, write description template →§24.7 | no |
| 2 | ends with `.cs~` | write impact report →§24.6 | no |
| 3 | has the form of a UUID | store it as your UUID →§24.9 | no |
| 4 | ends with `.md` | repeat deployment of code already received →§24.5 | no |
| 5 | - | from here on `AdHocAgent.toml` must have `PersonalVolatileUUID` key; without it exit (code 1) with sign-up instructions | - |
| 6 | ends with `.cs` or `.cs!` | generate code; with `!` also test it, as far as resources allow →§24.2 | yes |
| 7 | anything else | refused: "Unsupported file type ...", with link to converters; exit code 2 | - |

- RULE: extension tests case-sensitive: `.cs`, `.cs!`, `.cs~`, `.md` in lower case.

## §24.2 The `.cs` task: generating code
KW: .cs, .cs!, description file, imported .cs, .csproj, Compile Include, output folder, GitHub URL, tags, received folder, compiled twice
```text
AdHocAgent <description>.cs[!] [imported .cs files] [.csproj files] [output folder] [GitHub URL tag tag ...]
```
```text
AdHocAgent .\MyProtocol.cs
AdHocAgent .\MyProtocol.cs!
AdHocAgent .\MyProtocol.cs D:\Generated
AdHocAgent D:\Protocols\Extended.cs D:\Protocols\Base.cs D:\Generated
AdHocAgent D:\Protocols\Extended.cs D:\Protocols\Descriptions.csproj D:\Generated
```
- CLI: first arg = description file (holds root project →§3). `!` after name → code also tested as far as server resources allow →§24.3.3.
- CLI: later arguments classified one by one, wherever they stand:

| Argument after description | Taken as |
|:--|:--|
| first one looking like GitHub URL (`github.com/...`, with/without `https://` and `www.`) | start of open source mode: URL, every argument after it = tag →§24.10 |
| existing `.csproj` file | every `<Compile Include="...">` item, resolved against `.csproj` folder, joins compilation |
| existing `.cs` file | joins compilation |
| any other existing file | ignored, no message |
| anything not an existing file | output folder: existing used, missing created. Of several such args each is created; last one = output folder |

- NOTE: extra `.cs` files = sources of imported projects an extending project needs →§22; compiled together with description file. DEFAULT: no output folder arg → output folder = working directory.
- PITFALL: `.csproj` contributes only `<Compile Include="...">` items written in it, as full paths. If it also lists the description file, description file excluded from extras only if first argument names it by same full path, same letter case: `AdHocAgent .\MyProtocol.cs .\Descriptions.csproj` with `MyProtocol.cs` in `.csproj` → compiled twice, fails with duplicate definitions. Pass description file by full path, or keep it out of that `.csproj`.
- CLI: generated code arrives in received folder `<output folder>/<description file name without .cs>` (`AdHocAgent .\MyProtocol.cs D:\Generated` → `D:\Generated\MyProtocol`). First run also writes deployment instructions file beside it and stops; every later run deploys right after code arrives →§25.1.

## §24.3 What a `.cs` run does, step by step
KW: steps, local validation, ADHOC_PARSE_ONLY, ADHOC_DUMP_BRANCHES, read-only, .description, Result, connect
- RT: everything up to upload happens locally; description with error never reaches server.
  1. UUID key: `AdHocAgent.toml` must have `PersonalVolatileUUID` key, else exit (code 1) with sign-up instructions. Value read only later, at login.
  2. Compile and validate: all description files compiled together as one C# library, compiler warnings off, references = assemblies AdHocAgent runs on (.NET runtime and AdHocAgent, which carries `org.unirail.Meta` types). Compile error → prints diagnostics, exit (code 1, "Please fix the problem and rerun"). Then parser checks model; every description error stops here, exit codes →§24.12. Step also updates numbers tables in description file →§20.
  3. Optional stops: `ADHOC_DUMP_BRANCHES=1` → writes `branches.dump.txt`; `ADHOC_PARSE_ONLY=1` → stops here, exit 0 →§24.11.
  4. Published copy: open source mode checks GitHub repository and its `adhoc` folder; any mismatch exits (code 1), nothing sent →§24.10.
  5. Received folder cleared: `<output folder>/<description stem>` deleted and prepared before connection opens (generated files stream to disk as they arrive).
  6. Connect: endpoints of `server` key tried in order →§24.3.1.
  7. Version, login, upload, result →§24.3.2. When `Project` pack goes out, description file set read-only; when `Result` arrives: flag cleared, server's `info` text printed, description files copied into `.description` in received folder, deployment starts.
- NOTE (diagram): failures branch to exit 1, a parser exit code, or connection-lost message with exit code 0.

### §24.3.1 Generator server endpoints
KW: server key, ws:, wss:, WebSocket, TCP, port 80, DNS, localhost, 10 seconds, There are no servers available
- CLI: `server` key of `AdHocAgent.toml` = array of endpoints tried in order until one connects:

| Endpoint | How AdHocAgent connects |
|:--|:--|
| starts with `ws:` or `wss:` | WebSocket client to that URL |
| anything else | TCP to `host[:port]` (DEFAULT port 80); host name looked up by DNS, one address taken at random; `localhost` = loopback |

- RT: each attempt has 10 seconds to connect. Refused/unreachable endpoint, timeout, failed DNS lookup, malformed URL → next endpoint; none left → exit (code 1) "There are no servers available." Once connected, later failure is not a reason to try another endpoint.

### §24.3.2 The exchange with the generator server
KW: Communication, Agent.Version, Server.Invitation, Server.Info, Agent.Login, InvitationUpdate, Agent.Project, Server.Progress, Server.Result, SrcZip, Connection closed before the result was complete
- RT: exchange = `Communication` connection of `AdhocProtocol.cs`; AdHocAgent = `Agent` host, generator = `Server` host:

| State | AdHocAgent sends | Server answers | AdHocAgent does with answer |
|:--|:--|:--|:--|
| `Start` | `Agent.Version`: low byte of UI culture id, standard UTC offset of local time zone in 15-minute units, protocol version `1` | - | - |
| `VersionMatching` | - | `Server.Invitation`, or `Server.Info` when versions mismatch | `Info`: prints it, exit (code 1) "Resolve the issue and try again." |
| `Login` | `Agent.Login`: UUID as two 64-bit halves | - | - |
| `LoginResponse` | - | `Server.Invitation`; `Server.InvitationUpdate` with new UUID; or `Server.Info` when UUID rejected | `InvitationUpdate`: stores new UUID, then uploads; `Info`: prints it, with recovery hint →§24.9 |
| `TodoJobRequest` | `Agent.Project`: description files, serialized model, in open source mode URL and tags | - | sets description file read-only, starts progress display |
| `Project` | - | any number of `Server.Progress`, then `Server.Result` or `Server.Info` | shows progress; `Result`: receives files and deploys; `Info`: prints it |

- WIRE: description files and generated code travel compressed: file lists of `Agent.Project` and `Server.Result` go through `SrcZip` flow of AdHoc's own description (a Zstd chain →§16).
- RT: connection closing before `Result` complete (remote close, abrupt close, receive timeout, WebSocket protocol error) ends run with "Connection closed before the result was complete ... Nothing was deployed; whatever arrived is in <folder>". Server closes connection after an `Info` → rejected login and failed task end the same way (exit 0 →§24.12).

### §24.3.3 Progress and server-side testing of generated code
KW: Server.Progress, progress display, testing level, none, delivered code compiled, compiled, all stages compiled, full, .cs!, The task is accepted
- RT: on terminal AdHocAgent redraws block in place: task line (testing level, elapsed time, percent) + one line per language (pass, activity, percent):
```text
MyProtocol · testing level: full · 00:41 · 29%
  Java   pass 2 of 4   running tests     ███████░░░░░░░░░  43%
  C#     pass 2 of 4   compiling         █████░░░░░░░░░░░  31%
  TS     pass 1 of 4   compiling         ██░░░░░░░░░░░░░░  12%
```
- RT: redirected output (pipeline log) → plain lines, only those that stay true: task accepted (with testing level), a language pass done, result on its way. Number with no known text (newer server) shown as number.
- NOTE: without suffix server only generates; with `!` (`AdHocAgent .\MyProtocol.cs!`) also tests, as far as resources allow. Testing level ∈ `none`, `delivered code compiled`, `compiled`, `all stages compiled`, `full` (least → most); shown in first progress line (or "The task is accepted" log line). Service of generator, not replacement for your project's tests.

### §24.3.4 The received folder
KW: received folder, InCS, InJAVA, InTS, host folder, .description, Common1.cs, .., dot names, exit code 2
- NOTE: received folder = `<output folder>/<description file name without .cs>`. First level: folder per language (`InCS`, `InJAVA`, `InTS`), each holding folder per host generated in that language →§10. Host folder contents and deployment →§25.2.4.
- RULE: files stream straight to disk. Received path landing outside received folder (absolute, or climbing out with `..`) refused, exit (code 2).
- RT: when `Result` complete, description file and every file compiled with it copied into `.description` in received folder. Description files travel under bare file names; counter before extension separates two files of one name (`Common.cs`, `Common1.cs`); `.description` uses same names. Every deployment copies this folder into its backup, where `.cs~` finds description deployed code came from →§24.6.
- RULE: name starting with dot in root of received folder (e.g. `.description`) = AdHocAgent's own record: never listed in instructions file, processed or deployed.
- NOTE: received folder kept after deployment → change routes and redeploy without regenerating (`.md` →§24.5).

## §24.4 Pitfalls of the `.cs` task
KW: read-only, attrib -r, chmod u+w, not written into its numbers tables, deleted folders, stray folder, .csproj, wildcard, exit code 0
- RT: description read-only while server works on it: flag set when `Project` goes out, cleared when run ends however it ends (result, error, lost connection, Ctrl+C). Only a killed process leaves it set → next run skips numbers-table update with warning "... is read-only: new numbers and wire ids are not written into its numbers tables."; clear by hand (`attrib -r`, `chmod u+w`, editor).
- PITFALL: two folders in output folder deleted: received folder `<output folder>/<description stem>` before connection opens, and `<output folder>/<project interface name>` when upload starts. Use scratch folder as output folder, never one whose sub-folders are yours.
- PITFALL: mistyped path becomes output folder (non-existing file arg → output folder, created) → stray folder + import missing from compilation → compile error or "The source code for '...' could not be found" →§22.1.1.
- PITFALL: `.csproj` brings every `<Compile Include>` item and only those: linked templates and unrelated sources join compilation (and, in open source mode, set of files that must be published). Files an SDK-style project compiles without listing not added. `.csproj` whose elements carry XML namespace (pre-SDK format) adds nothing, no message. `<Compile>` without `Include` (`Remove`, `Update`) or `Include` with wildcard → run stops with exception. Give descriptions own `.csproj` listing each file, or pass `.cs` files one by one.
- PITFALL: description file also in `.csproj` compiled twice unless passed by full path `.csproj` resolves it to →§24.2.
- PITFALL: failed generation ends with exit code 0 →§24.12.

## §24.5 The `.md` task: redeploying received code
KW: .md, redeploy, instructions file, Cannot find source folder, redeploy process canceled, working directory
```text
AdHocAgent .\MyProtocol.md
AdHocAgent "D:\Generated\MyProtocol.md"
```
- CLI: argument = deployment instructions file. Repeats only deployment for files already received: nothing uploaded, no UUID needed. Use after changing routes, or to debug a deployment.
- CLI: received folder = instructions file path without `.md` (`D:\Generated\MyProtocol.md` → folder `MyProtocol`); looked for beside instructions file first, then in working directory; neither → exit (code 1) "Cannot find source folder ... redeploy process canceled".
- PITFALL: run from folder holding received folder: commands in instructions file naming `/InCS/...` paths resolve against `<working directory>/<received folder name>` →§25.5.
- NOTE: deployment →§25.

## §24.6 The `.cs~` task: which hosts a change reaches
KW: .cs~, impact report, .changes.html, previous version, ~, backup folder, JSON
```text
AdHocAgent .\MyProtocol.cs~ [<previous version>~] [imported .cs / .csproj files] [output folder]
```
- CLI: compares description with previous version, writes `<output folder>/<description stem>.changes.html`: hosts to update and why. Nothing uploaded, no UUID, neither version written to.
- CLI: previous version = first other argument ending with `~`: description file, folder holding one, or deployment backup folder; none → description saved by last deployment. Page opens in default browser unless output redirected. Finding previous version, what makes a host one to update, JSON for scripts →§21.

## §24.7 AdHocAgent without arguments
KW: no arguments, help, MyProtocolDescription.cs, template, Enter
- CLI: prints help, writes description template `MyProtocolDescription.cs` in working directory (existing file of that name overwritten), waits for Enter. Template = starting point of →§2.

## §24.8 AdHocAgent.toml
KW: AdHocAgent.toml, server, PersonalVolatileUUID, PreviousVolatileUUID, show_code_exe, show_code_args, template, may be outdated, not a valid UUID
- CLI: settings read from `AdHocAgent.toml`, looked for ONLY in folder of AdHocAgent executable. Missing → written there from built-in template, warning "its content may be outdated", run continues with it.

| Key | Holds | Written by |
|:--|:--|:--|
| `server` | generator server endpoints, array tried in order →§24.3.1 | you; template holds public endpoint |
| `PersonalVolatileUUID` | personal UUID, sent at login | `AdHocAgent <uuid>`, and every UUID rotation |
| `PreviousVolatileUUID` | UUID last change replaced | AdHocAgent, when storing new UUID |

```toml
server = ["wss://wss.unirail.org"]
# server = [ "wss://wss.unirail.org", "tcp.unirail.org" ]
PersonalVolatileUUID = "100b9fd2-e593-485b-a2fe-9b9c82bc1e3f"
```
- NOTE: no other keys read for generation and deployment. Template also holds `show_code_exe`, `show_code_args` (auxiliary tool settings, no part in either). Source formatters not configured here: they are processing steps of instructions file →§25.4.
- PITFALL: template's `PersonalVolatileUUID = ""` passes key test of `.cs` run → run connects to server first, then stops with "'PersonalVolatileUUID' in ... is not a valid UUID: ''. Re-apply your UUID: AdHocAgent <uuid>". Apply UUID before first upload.

## §24.9 UUID
KW: volatile UUID, 128-bit, personal, rotation, login replacement
- `volatile UUID` = personal 128-bit identifier, replaced by server from time to time; stands in for login + password → run needs nobody to sign in.

### §24.9.1 Getting your first UUID
KW: sign up, GitHub, Sign-Up Discussion, private repository, UUID issue, invitation
- NOTE: 1) sign in to GitHub; 2) post in Sign-Up Discussion https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up. When processed, post disappears.
- RT: first time → email invitation to your PRIVATE repository in AdHoc-Protocol organization; accept it, `UUID` issue holds your UUID. No email → check GitHub notification settings, spam folder.
- RT: later → same issue rewritten with new UUID, previous one stops working. Access suspended → issue says why and until when instead.
- RT: post stays → request queued or server unavailable; nothing to do, processed automatically.
- PITFALL: one UUID = one computer; it changes from time to time, cannot keep working on two. New computer or reinstall → post again; previous computer loses access.

### §24.9.2 Applying a UUID
KW: AdHocAgent <uuid>, GUID forms, hyphenated, AdHocAgent.toml.tmp, PreviousVolatileUUID, Your personal UUID is saved in
```text
AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f
```
- CLI: first arg with form of a UUID = UUID to store. Any GUID form accepted (with/without hyphens, braces or not); stored in `AdHocAgent.toml` hyphenated. Equal to stored value → file untouched. Otherwise replaced value kept as `PreviousVolatileUUID`; file written to `AdHocAgent.toml.tmp` and moved over original (interrupted write cannot truncate).
- RT: run ends with "Your personal UUID is saved in …" + full path of file. Anything without UUID form = file name → mistyped UUID not stored: run stops, exit code 2, "Unsupported file type: …".

### §24.9.3 UUID rotation and recovery
KW: InvitationUpdate, rotation, PreviousVolatileUUID, rejected UUID, re-apply, sign up again
- RT: at login server may answer `InvitationUpdate` with new UUID; AdHocAgent stores it as `AdHocAgent <uuid>` does (replaced one → `PreviousVolatileUUID`), then uploads task.
- PITFALL: replaced UUID cannot be used again → keep `AdHocAgent.toml`: only place holding current UUID; other copies of old value go stale.
- RT: server rejects UUID → AdHocAgent prints server's message and, when file has `PreviousVolatileUUID`, the command re-applying it:
```text
AdHocAgent <PreviousVolatileUUID>
```
- NOTE: server keeps previous UUID valid until new one confirmed → re-applying it gets new UUID issued without new sign-up. Sign up again only when that fails.

## §24.10 Open source
KW: open source, catalog, 500 stars, adhoc folder, GitHub URL, tags, GITHUB_TOKEN, 403, 429, 404, Project.github, Project.tags, BOM, CRLF
- NOTE: code generation is free. Project may be listed in catalog of projects using AdHoc; needs: public GitHub repository with ≥ 500 stars; folder named `adhoc` in it holding copy of EVERY file the task uploads under same names (description file, every imported `.cs`, every file a `.csproj` arg brings); project's topical tags.
- CLI: pass folder URL after file arguments, then tags, one per argument:
```text
AdHocAgent .\MyProtocol.cs https://github.com/owner/repo/tree/main/path/to/adhoc networking telemetry iot
AdHocAgent .\MyProtocol.cs .\Shared.cs D:\Generated https://github.com/owner/adhoc networking
```
- RULE: URL ends list of paths: every argument after it = tag → imports and output folder go before it. URL forms:

| URL | Points to |
|:--|:--|
| `https://github.com/owner/repo/tree/<branch>/<path>/adhoc` | folder `adhoc` at `<path>` on branch `<branch>`; `<path>` may be empty (`.../tree/main/adhoc`) |
| `https://github.com/owner/adhoc` | repository itself, named `adhoc`, on default branch |

- RULE: `https://` and `www.` optional; `.git` suffix of repo name dropped; last segment must be exactly `adhoc`. Other URL (no `/tree/<branch>/` and not repo root, or folder not named `adhoc`) refused (code 1). Tag: starts with letter, continues with letters, digits, `-`, `_`, ASCII; ≥ 1 tag required.
- RULE: after local validation, before upload, AdHocAgent checks: no two uploaded files share a name ignoring case (one GitHub folder holds one file of a name); repo public with ≥ 500 stars; folder (named branch or default) holds a file of each uploaded file's name — only files directly in it count, other files allowed; each copy equals uploaded file after removing UTF-8 BOM and turning CRLF and CR into LF (git rewrites both on checkout). Any mismatch refuses task with message what to fix, nothing sent (code 1). Server repeats check → published copy must stay identical to what you generate from.
- RT: checks via GitHub API; anonymous limit 60 requests/hour per address; refused request (403 or 429) says so. `GITHUB_TOKEN` lifts limit. 404 = repo or folder missing, or repo private.
- WIRE: URL and tags travel in `Project.github` and `Project.tags` fields of `AdhocProtocol.cs` (≤ 1 000 characters each; tags joined by single spaces). Run without URL leaves both empty.

## §24.11 Environment variables of AdHocAgent
KW: ADHOC_PARSE_ONLY, ADHOC_DUMP_BRANCHES, branches.dump.txt, ADHOC_VERBOSE, ADHOC_TRACE, NO_COLOR, GITHUB_TOKEN

| Variable | Effect |
|:--|:--|
| `ADHOC_PARSE_ONLY=1` | `.cs` task compiles and validates description, stops with exit 0; nothing uploaded. `PersonalVolatileUUID` key must exist, value unused |
| `ADHOC_DUMP_BRANCHES=1` | `.cs` task writes `branches.dump.txt` beside description: every connection, actor, state, branch with packs and ids, packs each side of a connection sends and parses, pack headers, implementation settings of every host |
| `ADHOC_VERBOSE=1` | also prints what AdHocAgent worked out: transform chain of each stream, legs each trimmed field is cut on, route of each virtual connection, rights of each role, config file read. Without it: only what needs attention |
| `ADHOC_TRACE=1` | every message ends with the line of AdHocAgent source code that wrote it; for reports about AdHocAgent itself |
| `NO_COLOR` (any value) | no ANSI colors in AdHocAgent output nor in error output of tools it runs. Colors also off when output redirected |
| `FORCE_COLOR` (any value) | colors even when output redirected: CI log that renders ANSI |
| `GITHUB_TOKEN` | open source mode: token for GitHub API checks, lifts anonymous limit 60 requests/hour |

## §24.12 Exit codes of AdHocAgent
KW: exit code, 0, 1, 2, 22, 23, 45, 56, 66, 77, 88, 123, -1, 255, Press ENTER to exit, message format, path:line, clickable, ADHOC_VERBOSE, FORCE_COLOR
- CLI: message = time, level (`ERR` red, `WRN` yellow, `INF` cyan), text. Place named by full path + line: `D:\Projects\Shop\Shop.cs:42`; C# compiler errors: `D:\Projects\Shop\Shop.cs(42,17)`. VS Code, JetBrains IDEs, Windows Terminal open file at that place on click (Ctrl+click in some). This file writes a place as `(line N)`.
- CLI: a run prints only what needs attention (errors, warnings, what it wrote); `ADHOC_VERBOSE=1` adds what AdHocAgent worked out. Colors off with `NO_COLOR` or output to file/pipe; `FORCE_COLOR` turns them on there.
- CLI: on purposeful stop: logs reason (as error unless code 0), prints "Press ENTER to exit", waits for a line of input, exits with code. Runs ending without that prompt, with code 0: applying UUID, `ADHOC_PARSE_ONLY`, written impact report, first instructions file.

| Code | When |
|:--|:--|
| `0` | task finished: deployment completed, UUID applied, report written, `ADHOC_PARSE_ONLY` stop, first instructions file generated, `n` answered to "Deploy now?" |
| `1` | default error: configuration, compile error in description, open source check, no reachable server, no route in instructions file, missing received folder, failing C# processing step, many description errors |
| `2` | unsupported file type; received path outside received folder; many description errors, e.g. file declaring several projects →§3.3, two attribute replacements in projects not importing each other →§22.3.2 |
| `22` | state, constants container or other declaration in wrong place of connection body →§3, §6 |
| `23` | field of unsupported type |
| `45` | connections and hosts: root project without connection, host without language marker, configuration target of wrong kind →§10 |
| `56` | constants: constant of unsupported type →§6 |
| `66` | numbers: two entities of one kind with one number in one project, usually copied mark →§20 |
| `77` | trims and `Resumable` endpoints →§17, §14 |
| `88` | `Stream` and `File` packs and fields →§15 |
| `123` | reserved identifier assigned |
| `-1` (255 on Linux and macOS) | you canceled: at orphaned code prompt, or `N` after failed processing step or hook; orphaned code with redirected input; external command could not be started; few description errors — value range with minimum = maximum, `[MinMax]` on type taking none, `[D]` argument not fitting its field (array length on non-array field, `+` length on field not string/Map/Set) |

- PITFALL: treat numbers as diagnostics, not interface: script can rely only on zero vs non-zero; zero does not always mean code was generated and deployed.

## §24.13 Running AdHocAgent in a pipeline
KW: pipeline, CI, persist AdHocAgent.toml, ADHOC_PARSE_ONLY, The code is deployed, NO_COLOR, GITHUB_TOKEN, echo y, time limit
- RULE: persist `AdHocAgent.toml`: server rotates UUID, replaced UUID not reusable → restore file before each run, save after, so each run starts with UUID previous run received.
- CLI: validate without generating: `ADHOC_PARSE_ONLY=1` checks description in a pull request without upload.
- CLI: exit code tells it: run that got no code (refused login, connection ended before result, failed deployment) → non-zero. Completed deployment logs "✔ The code is deployed".
- NOTE: redirected output: no colors, progress as plain lines; `NO_COLOR` switches colors off on terminal too.
- NOTE: open source mode needs `GITHUB_TOKEN` when pipeline shares address with other GitHub API users.
- CLI: answer prompts through input: pipe answer (`echo y | AdHocAgent .\MyProtocol.cs`) or redirect input from empty source to answer `n`.
- PITFALL: orphaned region always needs a person: with redirected input deployment stops before anything is written (code -1); resolve in interactive run.

### §24.13.1 Prompts of AdHocAgent with redirected input
KW: prompt, redirected input, Deploy now?, Remove it and go on? (y/N), Go on with the deployment?, Press ENTER to exit

| Prompt | When | With redirected input |
|:--|:--|:--|
| "Deploy now? (y = deploy now / n = update the list and stop, deploy later)" | received files no longer match instructions file, or orphaned files in destinations →§25.6 | reads line; line starting with `y` deploys; anything else (end of input included) refreshes list, exits code 0 |
| "Remove it and go on? (y/N)" | region with your code gone from new code →§25.8.3 | not asked: deployment stops before any destination written, "The deployment is stopped: your code above would be removed, and with the input redirected there is nobody to agree." (code -1) |
| "Go on with the deployment? Enter N to stop, anything else to go on:" | processing step or hook failed: exit code other than 0 | not asked: warning "The deployment goes on: with the input redirected there is nobody to ask.", deployment goes on |
| "Press ENTER to exit" | stop on purpose →§24.12 | not shown: run exits at once |

- NOTE: prompt appears only when its condition arises: deployment whose instructions file matches received files and finds no orphaned files deploys without asking.

## §24.14 Descriptions from other schema languages
KW: converters, other schema, union, optional fields
- NOTE: AdHocAgent reads only AdHoc descriptions. Standalone converters (one repository each) turn existing schema into AdHoc description to start from — e.g. union → pack of optional fields, one per alternative (→§5); review output: https://github.com/AdHoc-Protocol#converters-to-adhoc-protocol.

## §24.15 Third-party components of AdHocAgent
KW: .NET 10, Roslyn, Microsoft.CodeAnalysis.CSharp, Serilog, Tommy, ZstdSharp.Port
- NOTE: targets .NET 10; packages:

| Package | Version | Used for |
|:--|:--|:--|
| Microsoft.CodeAnalysis.CSharp and Microsoft.CodeAnalysis.Common (Roslyn) | 5.9.0 | compiling and reading descriptions; compiling C# processing steps of deployment |
| Serilog.Sinks.Console | 6.1.1 | console log |
| Tommy | 3.1.2 | reading and writing `AdHocAgent.toml` |
| ZstdSharp.Port | 0.8.8 | Zstd stage of runtime library, compresses files sent to / received from server |

# §25 Deployment and smart merge
KW: deployment, instructions file, routes, smart merge, injection points, backup, restore, orphan, .md
SUMMARY: Last step of `.cs` run: AdHocAgent copies received code into your project folders per deployment instructions file (Markdown, generated once, edited by you); smart merge carries your code inside generated files into new versions; every overwritten/deleted file backed up with restore scripts.
- RT: deployment runs right after every `.cs` run once instructions file has routes; also alone, for code already received, with `.md` task →§24.5.
- NOTE (diagram, on-disk layout): output folder `D:/Generated` holds received folder `MyProtocol` (`.description`, language and host folders), instructions file `MyProtocol.md`, previous list `MyProtocol.1.md`, backup folder `MyProtocol_1` (restore scripts, `.description`, mirrored files). Route on `InCS/Server` line copies `gen`, `lib`, license into `D:/Projects/server/protocol` (generator territory); project's own `Server.csproj` and `src` beside it untouched; `demo.cs` and `Project.csproj` stay behind unless list has their lines.

## §25.1 The deployment workflow
KW: first run, routes, ⛔, AdHocAgent .\MyProtocol.md, workflow
1. First run: `.cs` run finds no instructions file → writes one beside received folder, logs warning saying how to rerun, exit code 0. Nothing deployed.
2. Write routes: at end of line of each host folder you use, add route to a folder that will hold generated code only; put `⛔` at end of line of each host not used.
3. Deploy: run `AdHocAgent .\MyProtocol.md` from folder holding instructions file → deploys code already received, no regeneration. Rerun as often as routes change.
4. Repeat: every later `.cs` run deploys right after result arrives. Routes, notes in instructions file and your code inside generated files carry over between generations.

## §25.2 The deployment instructions file
KW: instructions file, <received folder name>.md, MyProtocol.md, rename, pairing
- RULE: named after received folder: `<received folder name>.md` (`MyProtocol.cs` → received folder `MyProtocol` → `MyProtocol.md`). Name pairs file with received folder: do not rename; move both together.

### §25.2.1 Where the instructions file is looked for
KW: lookup, output folder, working directory, .md task

| Task | Where AdHocAgent looks |
|:--|:--|
| `.cs` | beside received folder (= output folder), then working directory. Neither → new one generated beside received folder |
| `.md` | file you name. Received folder = its path without `.md`, looked for beside it, then in working directory |

- RULE: folder of description file not searched unless it is output folder or working directory.

### §25.2.2 What a generated instructions file contains
KW: quick start, ```text fences, full-width ［, generated sections
- GEN: self-explaining; short quick start, then: 1) received files — what host folder holds, tree of language and host folders; 2) how to write a route — rules and safe project layout; 3) running a deployment — command line, what happens; 4) your code inside generated files — injection points and generated blocks; 5) processing received files before deployment — formatting steps, ready to run.
- PITFALL: its explanations use ` ```text ` fences, write example hook with full-width bracket `［`, start no example line with `- ` + icon — because real step block, hook link or tree line is read wherever it stands, inside a fence too →§25.4. Keep that in own notes.

### §25.2.3 The tree of received files
KW: tree, line, icon, 📁, ＃, ☕, 🌀, identity, language folder, note, ⛔, rebuilt, <stem>.<n>.md, angle brackets
- DECL: tree = Markdown list of language folders of received folder and host folders in them (lines routes are written on). `D:\Generated\MyProtocol` with routes:
```markdown
- 📁[InCS](/D:/Generated/MyProtocol/InCS)
	- 📁[Server](/D:/Generated/MyProtocol/InCS/Server) [](/D:/Projects/server/protocol/)
- 📁[InJAVA](/D:/Generated/MyProtocol/InJAVA)
	- 📁[Device](/D:/Generated/MyProtocol/InJAVA/Device) ⛔ not used yet
- 📁[InTS](/D:/Generated/MyProtocol/InTS)
	- 📁[Browser](/D:/Generated/MyProtocol/InTS/Browser) [](/D:/Projects/web/src/protocol/)
```
- RULE: line = `- <icon>[<name>](<path>)`, recognized anywhere in file; link = path of folder/file in received folder. AdHocAgent writes two spaces indentation per level, forward slashes, `/` before drive letter, path with spaces in angle brackets: `(</D:/My Out/MyProtocol/InCS>)`.
- RULE: icon by extension: `📁` folder, `＃` `.cs`, `☕` `.java`, `🌀` `.ts`, `📜` `.js`, `🌐` `.html`, `🎨` `.css`, `{}` `.json`, `🧩` `.cpp`, `🧾` `.h`, `⚙️` `.rs`, `🐹` `.go`, `🟪` `.kt`, `🐦` `.swift`, `🔧` `.c`, `🎯` `.dart`, `🐍` `.py`, `📄` anything else. Line with another icon is not a tree line.
- RULE: identity of line = part of link from language folder on (`InCS/Server`); folders before it may change (so received folder + instructions file can move together). Indentation cosmetic: nesting comes from paths, not list nesting. Language folder = first folder of link named `InCPP`, `InRS`, `InCS`, `InJAVA`, `InGO`, `InTS`, `InSwift`, `InC`, `InDart` or `InPy` → no folder above it may have such a name (neither on output folder path nor received folder, named after description file): such lines never match what arrived.
- RULE: note = any text between line's link and its routes (`not used yet`); kept when list rebuilt. Note cannot contain `[`: line ends there; text after it, routes included, not read and lost on rebuild.
- PITFALL: routes may go on next lines: line breaks and blank lines do not end a line's routes → link at line start right below tree (e.g. hook) read as route of last line. Keep other text between tree and such a link.
- PITFALL: list rewritten as whole: text between two list lines lost on rebuild; write as note on a line, or outside list.
- RT: when received files no longer match list (host added, host gone, lines naming nothing that arrived) → list rebuilt from what arrived. Each line still matching keeps routes, `⛔`, note; everything around list (explanations, processing steps, your text) left as is. Previous file first copied beside it as `<stem>.<n>.md` (`MyProtocol.1.md`, `MyProtocol.2.md`, …). File with no list at all gets new list on top, existing text kept below.

### §25.2.4 What a host folder holds
KW: gen, __, lib, LICENSE.md, NOTICE, LICENSE-GRANT.md, demo, demo.cs, demo.ts, Project.csproj, package.json, tsconfig.json, sources, optional, REMOVED

| Inside host folder | What it is | Deployed |
|:--|:--|:--|
| `gen` | generated protocol code; its `__` folders hold internal helper code | always, with host folder, in its place |
| `lib` | AdHoc runtime library, with `LICENSE.md` and `NOTICE` | always, with host folder, in its place |
| `.md` files, e.g. `LICENSE-GRANT.md` | terms code comes under | always, with host folder, in its place |
| `demo` — folder (Java) or file (`demo.cs`, `demo.ts`, `demo.py`, `demo.dart`, `demo.rs`, `main.go`, `demo.cpp`, `demo.c`, `app/demo.swift`) | host skeleton: every method you provide, with injection points | only when list has its line |
| project files — `Project.csproj`, `package.json`, `tsconfig.json`, in Java host folder `sources` (list of source files) | what builds host folder as own project | only when list has their lines |

- RULE: precisely: every sub-folder of host folder except `demo` always deployed with it; `demo` folder and every file directly in host folder except `.md` files optional.
- RULE: code your project runs on is fixed: `gen`, `lib`, licenses have no lines; nothing written there leaves part out or moves it. Line written for `gen`, `lib` or anything inside not kept: dropped from list, reported REMOVED when it carries route, `⛔` or note.
- DECL: optional item deployed by adding its line under its host's line:
```markdown
  - 📁[Server](/D:/Generated/MyProtocol/InCS/Server) [](/D:/Projects/server/protocol/)
	- ＃[demo.cs](/D:/Generated/MyProtocol/InCS/Server/demo.cs)
	- 📄[Project.csproj](/D:/Generated/MyProtocol/InCS/Server/Project.csproj) [](/D:/Scratch/server-skeleton/)
```
- RT: nothing after link → item goes where host goes, in its place (`demo.cs` → `D:\Projects\server\protocol`).
- RT: own route → goes there AS WELL (host route still applies; routes add up →§25.3.2): `Project.csproj` → `D:\Scratch\server-skeleton` and `D:\Projects\server\protocol`.
- RT: line deleted → item not deployed. Every deployment report names optional items left behind: `⛔ optional: deployed when the list has its line`.
- RULE: bare `⛔` on demo/project-file line = no line; line removed. `⛔` + note → line stays as record of why.
- RULE: line for a file inside `demo` folder may be written by hand, same way; stays while it carries route, `⛔` or note.
- RT: instructions file of earlier AdHocAgent (listed every file, deployed all not `⛔`) brought to this form on next deployment, nothing deployed differently: demo/project file line without `⛔` keeps line and target; bare `⛔` on such line leaves with line; `⛔` + note stays; line of deeper file carrying nothing leaves without question.

## §25.3 Routes: where received files go
KW: route, [<filter>](<destination>), filter, destination, regex
- DECL: `route` = Markdown link at end of line after line's own link: `[<filter>](<destination>)`. `filter` = optional regexp; empty → every file. `destination` = path on your file system.
```markdown
  - 📁[Server](/D:/Generated/MyProtocol/InCS/Server) the code of the host [](/D:/Projects/server/protocol/)
```

### §25.3.1 Destination paths and the trailing separator
KW: /D:/dir/, D:\dir\, angle brackets, relative path, ), Program Files (x86), trailing slash, rename file
- DECL: Windows path as `/D:/dir/` (leading `/` is Markdown's) or `D:\dir\`; spaces → angle brackets `</D:/My Projects/dir/>`. Write absolute: relative resolved against working directory.
- RULE: destination cannot contain `)` (ends link) → `C:\Program Files (x86)\...` cannot be a destination.
- RULE: trailing `/` or `\` matters only on line of a file:

| Line | Destination | Where files land |
|:--|:--|:--|
| folder `Server` | `/D:/Projects/server/protocol/` | `gen\X.cs` → `D:\Projects\server\protocol\gen\X.cs`: folder's CONTENTS go into destination; folder `Server` itself not created |
| folder `Server` | `/D:/Projects/server/protocol` | same: on folder line destination is always the folder receiving contents (may rename it) |
| file `demo.cs` | `/D:/Projects/server/demo/` | into that folder: `D:\Projects\server\demo\demo.cs` |
| file `demo.cs` | `/D:/Projects/server/demo/ServerSkeleton.cs` | exactly that path: copied under that name |

- RT: route on file line added to routes inherited from its folders: both file rows above also deploy `demo.cs` wherever host's route sends it.

### §25.3.2 Route inheritance and filters
KW: inheritance, routes add up, filter, absolute path, case-sensitive, ], ( | ), several routes
- RT: route on folder inherited by everything inside; each file keeps path relative to folder carrying route (single route on host folder deploys whole `gen` and `lib`). Route on inner line added to inherited ones, never replaces. Several routes on one line all apply → one file to several places.
- RULE: filter limits folder route to files whose path matches. Matched path = file's ABSOLUTE path in received folder as file system writes it (`\` between folders on Windows); case-sensitive. On file line filter has no effect. Filter cannot contain `]` (ends it) → write alternatives as `( | )`, not character class:
```markdown
  - 📁[Browser](/D:/Generated/MyProtocol/InTS/Browser) ✅ all to src, images also to
    assets [\. (jpg|png|gif)$](/D:/Projects/web/assets/images/) [](/D:/Projects/web/src/protocol/)
	- 🌀[demo.ts](/D:/Generated/MyProtocol/InTS/Browser/demo.ts)
```
- RT: every `Browser` file → `src/protocol`, keeping relative path; image matches both routes → both places. `demo.ts` line carries nothing → deployed with host: `D:\Projects\web\src\protocol\demo.ts`.

### §25.3.3 Skipping files and folders
KW: ⛔, []( ), skip, exclude host
- RULE: `⛔` on line, or empty route `[]()`, keeps item out of deployment — folder with everything inside, its own and inherited routes included:
```markdown
  - 📁[Browser](/D:/Generated/MyProtocol/InTS/Browser) [](/D:/Projects/web/src/protocol/)
	- 🌀[demo.ts](/D:/Generated/MyProtocol/InTS/Browser/demo.ts) ⛔ we wrote our own entry point
- 📁[InJAVA](/D:/Generated/MyProtocol/InJAVA) [](/D:/Projects/all-java/)
	- 📁[Device](/D:/Generated/MyProtocol/InJAVA/Device) ⛔
```
- RT: route on `InJAVA` deploys every Java host into `D:\Projects\all-java\<host>`; `⛔` on `Device` keeps that host out → quick start asks `⛔` on every unused host.

### §25.3.4 A destination folder belongs to the generator
KW: generator territory, orphan, deleted, backed up, .git, .idea, .vs, ORPHANED, protocol folder, safe layout, gen/gen
- PITFALL: every folder a deployment writes into = generator territory. File already there that this deployment does not write = `orphan`: backed up, then DELETED. Holds for folder of every destination file and every folder below. Only folders whose names start with dot (`.git`, `.idea`, `.vs`) never looked into; folders deletion empties are removed.
- RULE: destination must hold generated code only — never project root, never `src`, never folder with your files. Includes folder of a single routed file: routing `demo.cs` into your `src` makes all of `src` generator territory. Orphans listed under ORPHANED before "Deploy now?"; read before answering.
- NOTE: safe layout = dedicated `protocol` folder beside your code:
```text
MyProject/
├─ MyProject.csproj   yours, outside the territory
├─ src/               your code
└─ protocol/          route the host folder here: generated content only
   ├─ gen/            rewritten by every deployment; stale files in it are pruned
   └─ lib/            the runtime library
```
- RULE: route host folder to `.../MyProject/protocol/`, not `.../MyProject/` or `.../MyProject/src/`; add `protocol/gen`, `protocol/lib`, `src` to build source folders. AdHocAgent's own repository uses this layout (generated in `protocol/gen`, `protocol/lib`, project file at root).
- PITFALL: route host folder itself, not its `gen`: host folder holds `gen` and `lib` → route to `.../protocol/gen/` writes `.../protocol/gen/gen/` and `.../protocol/gen/lib/`.

## §25.4 Processing steps before deployment
KW: processing step, ```regexp, ```shell, ```csharp, FILE_PATH, formatter, in place, /InCS/, last argument
- DECL: `processing step` runs tool on received files before anything copied (mostly formatter). Step = two fenced blocks one right after other, only blank space between: ` ```regexp ` block with one-line regexp selecting entries by absolute path (as route filters), then ` ```shell ` or ` ```csharp ` block.
````markdown
```regexp
\.(java|cs|cpp|h)$
```

```shell
clang-format -i -style="{ColumnLimit: 120, BreakBeforeBraces: Allman}" FILE_PATH
```
````
- RT: steps run in file order, shell and C# interleaved.
- RT: step works IN PLACE in received folder: changes received files; changed file is what deploys. Runs on every received entry whose full path matches (library files and files no route deploys included) except `.md` files (never processed). Shell step also runs on matching folders (filter `.*` hands folders to command too); C# step on files only.
- RT: `FILE_PATH` replaced with full path of selected entry, quoted when it contains spaces.
- RULE: argument starting with `/InCS/`, `/InJAVA/`, `/InTS/` or another language folder, not naming an existing file/folder = path inside received folder. Write such argument LAST: one followed by another argument is joined with it into one broken argument.
- PITFALL: ` ```regexp ` block followed by ` ```shell `/` ```csharp ` block = step wherever it stands. Write explanations/examples in instructions file in ` ```text ` blocks.

### §25.4.1 Shell processing steps
KW: shell step, one command per block, .exe, .cmd, .sh, .bash, error output, Go on with the deployment?, code -1
- RULE: one command per shell block; long command may continue on lines starting with space or tab. Runs once per selected entry, working directory = received folder. Lines of one block joined into single command line → two commands in one block run as one command (second's words as arguments); second command → second step.
- RT: executable started as written; fails → tries name with `.exe` and `.cmd` on Windows, `.sh` and `.bash` elsewhere. None starts → deployment stops (code -1). Delete steps whose tool not installed.
- DECL: quote executable path with spaces: `"C:\Program Files\LLVM\bin\clang-format.exe" -i FILE_PATH`.
- RT: command ends with exit code 0 → done; its error output shown as warning, deployment goes on. Other exit code → failure: AdHocAgent shows command line and output, asks "Go on with the deployment? Enter N to stop, anything else to go on:". `N` stops deployment (code -1); anything else goes on.

### §25.4.2 C# processing steps
KW: csharp step, class Program, Main(string[] args), args[0], references, quoted reference line, compile error code 1
- DECL: ` ```csharp ` block holds class `Program`, outside any namespace, with `public static void Main(string[] args)`. Compiled once per step as console app; `Main` called per selected file with file's full path in `args[0]`.
- RULE: compilation references .NET core library, mscorlib, System, System.Core, System.Runtime, System.Console, System.Collections, System.Linq, System.Text.RegularExpressions. Each quoted line at top of block (consecutive) adds reference: assemblies loaded in AdHocAgent having a type whose full name starts with quoted text.
- RT: compile error → prints compiler messages, stops deployment (code 1); so does exception thrown by `Main`.
````markdown
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
    public static void Main(string[] args)
    {
        var content = File.ReadAllText(args[0], Encoding.UTF8);
        var updated = Regex.Replace(content, @"^\s+(?=//#region|#region)", "", RegexOptions.Multiline);
        File.WriteAllText(args[0], updated, new UTF8Encoding(false));
    }
}
```
````

### §25.4.3 The default steps of a generated instructions file
KW: default steps, clang-format, Artistic Style, astyle, prettier, rustfmt, gofmt, region markers, PATH

| Files | Step |
|:--|:--|
| `\.(java\|cs\|cpp\|h)$` | clang-format (https://releases.llvm.org/download.html) `-i`, Allman style, 4-space indentation, no column limit; Artistic Style (https://sourceforge.net/projects/astyle/files/) named as alternative |
| `\.ts$` | prettier `--write` (https://prettier.io/docs/en/install.html; `npm install -g prettier`) |
| `\.rs$` | `rustfmt` |
| `\.go$` | `gofmt -w` |
| `.*` | C# step removing indentation before region markers (`#region`, `#endregion`, `//#region`, `//#endregion`, `//region` forms) |

- PITFALL: tools called by name → must be on `PATH`. Delete steps not needed; step whose tool missing stops deployment.

## §25.5 Lifecycle hooks
KW: [before deployment], [after deployment], hook, full-width ［, Cannot start, code -1, working directory
- DECL: `lifecycle hook` runs one command once per deployment, before or after:
```markdown
[before deployment]("C:\Program Files\dotnet\dotnet.exe" format "/InCS/Server")
[after deployment](/path/to/logging_script.sh --status=success)
```
- RULE: Markdown link whose text is `before deployment` or `after deployment`, target = command. Found anywhere in file; every hook found runs. Command = everything between `(` and LAST `)` of line → one hook per line.
- RULE: command = executable + its arguments, if any: `[after deployment](/path/script.sh)` runs as it is. Quote executable path with spaces. Executable lookup and error-output handling as shell steps →§25.4.1; executable that cannot start → deployment stops (code -1) with "Cannot start ...".
- RT: working directory = received folder; arguments starting `/InCS/`, `/InJAVA/`, `/InTS/` … = paths inside it; write such argument last. Redeploying with `.md` task: run from folder holding received folder (paths resolved against `<working directory>/<received folder name>`).
- DECL: written with full-width `［` instead of `[` → inert text (form generated file uses for its example).

| Hook | Runs |
|:--|:--|
| `[before deployment]` | after list brought up to date, before processing steps |
| `[after deployment]` | last: after backup, deletion of orphans and copy into destinations → sees deployed files (build, test run fit) |

## §25.6 One deployment run, step by step
KW: scan, match, plan, orphans, NEW, REMOVED, ORPHANED, UNCHANGED, Deploy now?, No received folder has a destination, merge, backup, The code is deployed
1. Scan received folder; names starting with dot in its root skipped.
2. Match each instructions-file line to received folder/file by link part from language folder on. Line whose folder/file is gone: REMOVED when it carries route, `⛔` or note; leaves quietly when it carries nothing. Received host folder without line = NEW.
3. Plan destination of every file from routes; collect orphans = files in destination folders the plan does not write.
4. Ask: with NEW or REMOVED lines or orphans, report before anything written: each NEW file with path inherited route would write it to (or "no rule matches it - it will NOT be deployed anywhere"), each REMOVED line with what it carried, each ORPHANED file, number of UNCHANGED lines, where list copy and backup go. When list has ≥ 1 route asks "Deploy now? (y = deploy now / n = update the list and stop, deploy later)" — single key, or a line when input redirected. `n` updates list, deploys and deletes nothing, exit code 0.
5. Refresh list when changed; previous file first copied to `<stem>.<n>.md`.
6. No route anywhere → exit (code 1) "No received folder has a destination in ... yet".
7. `[before deployment]` hooks run.
8. Processing steps run on received folder.
9. Merge: each planned file prepared in temporary file: library files (`lib`, `__` folders) and `.md` files as verbatim copies; every other file merged with file at destination →§25.7. Orphaned code prompt appears here.
10. Report every received file and what happened →§25.6.1.
11. Back up: backup folder `<received folder name>_<n>` created beside instructions file; `.description` copied into it; every destination file about to be overwritten copied into it; every orphan copied into it and deleted; restore scripts written; folders deletion emptied removed →§25.9.
12. Write temporary files into destinations.
13. `[after deployment]` hooks run: they see deployed files. Logs "✔ The code is deployed" with backup folder; exit code 0.
- RT: destinations untouched up to step 11. Run stopped before it (`n` at prompt, failing step, refused orphaned region) writes nothing into them; only instructions file may already be refreshed, received files processed, `[before deployment]` hooks run. Input redirected → nobody asked about orphaned code: run stops (code -1), nothing of yours removed.

### §25.6.1 The deployment report
KW: report, 👉, ✅, ✅⚠️, ✎ N edited block(s) kept, //todo 🔴, ⛔, optional
- CLI: step 10 prints tree of received files, each with what happened — one line per destination:

| Mark | Meaning |
|:--|:--|
| `👉 <destination>` | new file, or verbatim copy (`lib`, `__` folders, `.md`) |
| `✅ <destination>` | merged with file that was there |
| `✅⚠️ <destination>` | merged after you confirmed dropping orphaned code |
| `✎ N edited block(s) kept` | appended to merged file: N generated blocks hold your edits and were kept |
| `, the generator has changed M: //todo 🔴` | appended after that: M of those blocks got update note |
| `⛔` | not deployed: skipped, or no route reaches it |
| `⛔ optional: deployed when the list has its line` | demo or project file without a line |

## §25.7 Smart merge
KW: smart merge, template, region by region, injection point, generated block
- `smart merge` = keeps code you write inside generated files across deployments: generated file not copied over destination file; new version taken as template, your old file's content carried in region by region. Enabled by injection points (where your code lives) →§25.8 and generated blocks (generator's code inside them) →§25.8.1.
- NOTE (diagram): untouched generated block → replaced by new version; edited block → kept with pencil mark in tag + todo note with new version; deleted block → comes back after your last block; block generator dropped holding your code → stays commented out under todo note; own lines between blocks pass unchanged; region whose id is gone → orphaned code prompt.

## §25.8 Injection points
KW: injection point, UID, #region>, #endregion>, //#region >, //#endregion >, region kind, Actor.OnNewState, numbers, copied region
- `injection point` = region of generated file whose content belongs to you; identified by unique id (`UID`) on closing line. C#: `#region>` / `#endregion>`; Java, TypeScript: `//#region >` / `//#endregion >`. Spaces around `>` irrelevant.
```csharp
#region> Actor

    //🌭<ƍǷŞŘ
    public static global::org.unirail.collections.Pool<Actor0> pool = new(4, () => new Actor0());
    //🌭/>
#endregion> Āÿÿ.Actor
```
```typescript
public OnNewState(conn: Connection, state: ComplexQuery.State): void {
//#region > Actor OnNewState
//#endregion > ÿÿÿ.Actor.OnNewState
	this.state = state;
	this.epoch++;
}
```
- RULE: UID = text after `#endregion>`. Name after `#region>` is for you; ignored. Entity region UID = numbers of entities it belongs to (characters from `ÿ` up, one per byte, as in numbers tables) + region kind (`Actor`, `Actor.OnNewState`, `OnReceivedEvent`, …). Names not in it → renaming pack/host/connection keeps your code in place; number change does not →§20.
- RULE: form looked for chosen by destination file extension: `//#region` in `.java` and `.ts`, `#region` in every other file. `#region Name` without `>` (e.g. grouping regions of a demo) = ordinary region, not injection point.
- RT: merge: content of region in your file → region with same UID in new file. Region empty in your file carries nothing; region your file lacks comes from generator alone.
- NOTE: located in files of `gen` and demo: code of every pack (`<Pack> Code`, its receive and serialize events), of connections and actors (list →§13), of custom transform stages →§16.
- PITFALL: not in library: library files, files of `__` folders, `.md` files copied verbatim, never merged. Runtime library has same-shaped regions (`#region> TCP code` in `lib/Network.cs`) but code written there overwritten by next deployment; old file only in backup folder.
- RULE: never edit, move or copy `#endregion>` line or its UID: region whose UID no longer matches is gone for merge; your code in it goes through orphan check →§25.8.3.
- PITFALL: two regions with one UID in a file lose code silently: only content of last carried over (what copied region does).
- PITFALL: region found by UID only: new entity taking over number of deleted one, region in same file → code written for old entity appears in new entity's region.

### §25.8.1 Generated blocks inside injection points
KW: generated block, //🌭<, //🌭/>, fingerprint, CRC-32, ✎, edited block, reorder, delete block
- `generated block` = code between `//🌭<` line and `//🌭/>` line inside injection point: generator's code in a place that is yours (actor pool above, call notifying handlers of received pack).
- GEN: received files have bare opening tag `//🌭<`. Writing into your project AdHocAgent adds four characters after `<`: `fingerprint` of generator's version of block = CRC-32 computed without whitespace and without generator's own `/*...*/` marks, one character per byte from `ÿ` up. Block holding your edit: `✎` follows fingerprint. Symbol after `//` (`🌭`) names block within its region.

| What you do with block | Next deployment |
|:--|:--|
| leave as is | replaces with generator's current version |
| reformat (indentation, line breaks) | same: whitespace is not an edit |
| edit — change, comment out | keeps as you left it, `✎` in tag; adds update note when generator's version changed |
| write code before/after/around | leaves your code as is |
| delete | brings generator's version back, after last block you kept |
| reorder blocks | keeps your order; new generator block goes after your last block |

- RULE: leave tags alone: fingerprint and `✎` tell next deployment what is yours. Block written by earlier AdHocAgent (tag without fingerprint) counts as yours only when switched off — commented out, or `//` before its tag; otherwise generator's version replaces it.

### §25.8.2 Update notes of smart merge
KW: update note, //todo 🔴, The generator has changed this block, The generator has dropped this block, new fingerprint
- RT: generator changes block holding your edit → your version kept, generator's new version put under it as `//todo 🔴` lines; tag takes new fingerprint → note written once:
```csharp
#region> Actor
    // sized for our peak load
    //🌭<ŀƼǅŭ✎
    public static global::org.unirail.collections.Pool<Actor0> pool = new(64, () => new Actor0());
    //🌭/>
//todo 🔴 The generator has changed this block. Its version now:
//todo 🔴     public static global::org.unirail.collections.Pool<Actor0> pool = new(8, () => new Actor0());
#endregion> Āÿÿ.Actor
```
  (four chars after `<` stand for fingerprint AdHocAgent computed.) Read note, adjust code, delete note. Note left is removed by next deployment.
- RT: generator drops block holding your edit → your code stays in place, commented out, under note:
```csharp
//todo 🔴 The generator has dropped this block. What it held here:
    // public static global::org.unirail.collections.Pool<Actor0> pool = new(64, () => new Actor0());
```
- RT: dropped block without your edit disappears without note. File report: `✎ 1 edited block(s) kept, the generator has changed 1: //todo 🔴`.

### §25.8.3 Orphaned code protection
KW: orphaned region, Remove it and go on? (y/N), The deployment is stopped, ✅⚠️, code -1, renumbered host
- RT: injection point can vanish (actor/pack/state removed, entity renumbered, UID edited). For each region of your file missing from new file, AdHocAgent strips generated blocks you did not edit and looks at rest:
  - nothing (empty or only generator's code) → dropped silently.
  - your code (edited blocks included) → deployment pauses: prints each orphaned region with UID, notes old file will be backed up, asks "Remove it and go on? (y/N)". `Y` → merges file without that code, marks it `✅⚠️` in report; old file in backup folder. Any other key → stops whole deployment with "The deployment is stopped: nothing is changed." (code -1); nothing yet written to destinations, backed up or deleted.
- RULE: question read as single key press. Redirected input → nobody asked: deployment stops at once, writes nothing →§24.13.1.
- NOTE: before confirming, copy code to keep into its new region — for renumbered host, regions of its new number.

## §25.9 Automated backups and restoration
KW: backup folder, <received folder name>_<n>, MyProtocol_1, .description, restore.bat, restore.ps1, restore.sh, Delete added files?
- RT: every deployment that goes ahead backs up first. `backup folder` = `<received folder name>_<n>` beside instructions file, numbered one past highest existing: `MyProtocol_1`, `MyProtocol_2`, …:

| In backup folder | What it is |
|:--|:--|
| `.description/` | description deployed code was generated from (description file + every file compiled with it, as uploaded). §21 compares your description with it; restore scripts never write it back |
| overwritten files | every destination file deployment replaced, under its path without drive: `D:\Projects\server\protocol\gen\X.cs` → `Projects\server\protocol\gen\X.cs`; two on one name → ` (1)`, ` (2)` before extension |
| orphans | every orphaned file, saved same way before deletion |
| `restore.bat`, `restore.ps1`, `restore.sh` | scripts undoing deployment |

- NOTE: beside instructions file also copies `<stem>.<n>.md`, one per list rebuild.
- RT: restore scripts first change to own folder (work from anywhere); recreate missing folders, copy every saved file back (overwritten files and deleted orphans), list files deployment added and ask `Delete added files? [A]ll / [N]one / [S]elect per-file (default N)`. Exact pre-deployment state only with `A`, or selecting every file with `S`.
- PITFALL: backup folders accumulate, never deleted by AdHocAgent. Delete old ones yourself; keep newest (impact report looks for previous description there).

### §25.9.1 How to restore a deployment
KW: restore, restore.bat, restore.ps1, restore.sh, chmod +x
1. Open newest backup folder, e.g. `MyProtocol_5`.
2. Run script: Windows — double-click `restore.bat` or run `restore.ps1` with PowerShell; Linux, macOS, WSL:
```shell
chmod +x restore.sh
./restore.sh
```
3. Answer question about added files.

## §25.10 Pitfalls of deployment
KW: pitfalls, project root, gen/gen, notes [, ), language folder name, fences, two commands, missing tool, hook argument, orphaned region pipeline, copied injection point, restore N

| Pitfall | What happens | What to do |
|:--|:--|:--|
| route to project root, to `src`, or file routed into folder of yours | every file there the deployment does not write backed up and deleted | route to dedicated folder →§25.3.4 |
| host folder routed to `.../protocol/gen/` | `gen/gen/` and `gen/lib/` | route host folder to `.../protocol/` |
| route on demo/project-file line meant to send it elsewhere | also goes where its host goes | expect both copies; routes add up |
| note containing `[` | routes after it not read; line cut there on rebuild | keep `[` out of notes |
| text between two list lines | lost on rebuild | write as note, or outside list |
| destination containing `)` | route misread | folder without `)` |
| folder above language folders named like one (`InCS`, `InJAVA`, `InTS`, `InPy` …) | no line ever matches; every run reports NEW and REMOVED | rename that folder, or the description file |
| example step or hook written with real fences/brackets | it runs | ` ```text ` blocks and full-width `［` |
| two commands in one ` ```shell ` block | run as one command line | one step per command |
| processing step whose tool not installed | deployment stops | delete step, or install tool on `PATH` |
| `/InCS/...` argument followed by another argument | passed as one broken argument | write it last |
| `.md` task run from another folder | `/InCS/...` paths in commands point to wrong folder | run from folder holding received folder |
| orphaned region in pipeline | deployment stops at question; `.cs` run does not exit | resolve in interactive run; time-limit pipeline step |
| copied injection point | only last copy's content survives | never copy region lines |
| restore answered with default `N` | files deployment added stay | answer `A` for exact return |

# §26 Roles and rights
KW: [Role], [Can], [Modify<F>], roles, family of roles, rights, Deny, may_, roles_, rights matrix, /** roles, ⨯, ✎
SUMMARY: Family of roles = `[Role] enum`; member = role (one bit) or named set; rights from both sides (`[Can<…>]` on roles, `[Role(…)]` on packs/Pack Sets/actors/RPC); agent folds all into one mask per pack/actor → one AND with constant before serialize/parse. Rules = defaults, overridable per host in injection points. Modifiers in extending descriptions, rights matrix in numbers tables.
- NOTE: states say *when* pack may go (→§12), roles say *who*: one host kind run by people with different rights, rights change while connection lives.
## §26.1 Declaring a family of roles
KW: [Role] enum, Viewer, Editor = Viewer, +, -, |, named set, seniority, 64 roles
```csharp
[Role] enum Staff
{
    [Can<@Catalog>]                     Viewer,
    [Can<(SetPrice, Catalog.Item)>]     Editor  = Viewer,
    [Can<Finance>]                      Auditor = Viewer - Editor,
    [Can<(Maintenance, Locked<@Shop>)>] Admin   = Editor + Auditor,
    [Can("👑")]                         Owner   = Admin,
                                        Writers = Editor | Admin,
}
```
- DECL: member value expression read as text; its C# value meaningless. Agent gives each role own bit in declaration order, writes bits into generated enum.

| Member | Meaning |
|:--|:--|
| `Viewer` | role, own bit |
| `Editor = Viewer` | senior to `Viewer`: may use all `Viewer` may |
| `Admin = Editor + Auditor` | senior to both |
| `Auditor = Viewer - Editor` | senior to `Viewer`; never granted with `Editor` |
| `Writers = Editor \| Admin` | named set (any of); no bit; value = mask of its roles |

- RULE: seniority transitive; circle → error. Expression joining only with `|` = named set; `|` mixed with `+`/`-` → error.
- RULE: senior to / never together only with roles of own family; never together with own junior/senior → error. `[Can]` on named set → error.
- DEFAULT: max 64 roles per family (named sets not counted); more → error, split family.
- NOTE: family = ordinary flags enum besides: usable as field type (e.g. `Welcome` pack telling peer its roles).
## §26.2 What a role may use: `[Can]`
KW: [Can<PACKS>], [Can("regex")], X<>, <see cref='Link.rpc'/>+, -, RPC method rights

| On member | Effect |
|:--|:--|
| `[Can<PACKS>]` | role may use packs of Pack Set expression (pack, Pack Set, scope with/without `@`, filter template, tuple →§9); actor interface in it = actor |
| `[Can<X<…>>]` | takes away rights role would have otherwise, inherited included |
| `[Can("regex")]` | every pack whose doc matches (tag e.g. `👑`, read as `[KeepDoc]` reads it) |
| `/// <see cref='Link.wipe'/>+` | RPC method `wipe` (not a type → named in doc comment) |
| `/// <see cref='Link.wipe'/>-` | takes RPC method away |

- RULE: attributes of member apply in order: `[Can<X<@Shop>>, Can<(Item, Price)>]` → exactly those two packs.
## §26.3 Rules written on packs, Pack Sets, actors and RPC methods: `[Role(…)]`
KW: [Role(Staff.Editor)], [Role(A | B)], two families, Pack Set rule, actor rule, RPC rule
```csharp
[Role(Staff.Editor)]                class SetPrice { int sku; long cents; }
[Role(Staff.Admin | Staff.Auditor)] interface Finance : _<@Catalog.Cost> { }
[Role(Staff.Admin)]                 interface Maintenance : Actor { … }
[Role(Staff.Admin)]                 (L____________, Done) wipe(Wipe req);
[Role(Staff.Admin, Tier.Gold)]      class Margin { … }   // roles of two families
```
- DECL: role named → it and all its seniors may use it. Join with `|`; `+`/`-` → error. On named Pack Set → rule to each of its packs.
- RULE: `[Role]` on connection → error: roles belong to host.
## §26.4 Who carries the roles
KW: carrier host, <see cref='Staff'/>, full name, host modifier, Connects<Node, Node>, L, R
- DECL: family declared inside host = roles of that host. Declared elsewhere → named in doc comment of each carrying host, like language: `/// <see cref='Staff'/>` on `struct Desk : Host`.
- DECL: project extending another names imported family by full name: `/// <see cref='com.lib.Library.Staff'/>`; host modifier adds family to imported host same way →§10.6.
- RT: on connection, side whose host carries family = side whose rights checked: what it sends and what it is sent. Both hosts carry it (`Connects<Node, Node>`) → both sides checked, each by own roles.
- RULE: family no host carries → warning, checks nothing.
## §26.5 How the rules combine
KW: union, open pack, no one, actor AND pack, several families
- RULE: what pack rules name from both sides (`[Can]` of roles + `[Role(…)]` on pack) add up: any of them may use it.
- DEFAULT: pack/actor no rule names = open to all; no check generated.
- RULE: pack given to a role then taken from every role → stays under rule: no one may use it. Pack only taken away (`X<…>` of pack no role given) → stays open.
- RULE: pack sent inside actor under rule needs both actor rule and pack rule. Pack restricted by two families on one connection needs every family's rule.
- RT: family rules apply on every connection where a host carries it: packs travelling there either way + actors of that connection.
## §26.6 Roles in a description that extends another
KW: [Modify<Staff>] enum, Modify<Pack>, bare [Role], layers, topmost wins, sibling conflict, exit code 2
```csharp
/// <see cref='com.lib.Library.Staff.Owner'/>-
[Modify<Staff>] enum StaffEdit
{
                                                   Editor,
    [Can<Moderation>]                              Moderator = Editor,
    /// <see cref='com.lib.Library.Link.wipe'/>-
    [Can<X<SetPrice>>]                             Admin,
}
[Role(Staff.Auditor)] class MarginRule : Modify<Margin> { }
[Role]                class ItemFree   : Modify<Catalog.Item> { }
```
- DECL: enum cannot extend `Modify<>` → modifier is attribute `[Modify<Family>]`.

| In modifier | Effect |
|:--|:--|
| member named as target role, no `= …`/`[Can]` | refers to it, changes nothing |
| member of new name | new role of target, own bit |
| `[Can<…>]` on member | adds rights; `X<…>` in it takes away, inherited too |
| value expression on member of target's name | replaces what role is senior to |
| `<see cref='Staff.Owner'/>-` on modifier | removes role |
| `[Role(…)] class M : Modify<Pack>` | whole rule of pack: all roles + pack said before replaced; bare `[Role]` opens it |

- GEN: modifier enum generates nothing.
- NOTE: `[Role]` on pack modifier replaces only pack rule; not a replaceable attribute: target transform chain stays; modifier replacing chain leaves rule →§22.3.2.
- RULE: layers apply lowest up: extending project edits after base → topmost layer wins. Rules on packs first, role lists after.
- RULE: two modifiers editing one role, or giving rule of one pack, in projects neither importing the other → exit 2: "Both '…' and '…' edit the role … of …, and neither project imports the other, so which one wins would be decided by composition order." Two rules of one pack in one project → error too.
## §26.7 The rights matrix in the numbers tables
KW: /** roles, ●, ↑, ✎, ⨯, matrix, never read
```
/** roles Staff                            Viewer  Editor  Auditor  Admin  Owner
    <see cref='Ban'/>                                ●                ●      ↑
    <see cref='Catalog.Item'/>               ●       ●        ↑       ↑      ↑
    <see cref='Margin'/>                                      ●       ↑      ↑
    <see cref='SetPrice'/>                           ●                ↑      ↑
    <see cref='StaffLink.Maintenance'/>                               ●      ↑
    <see cref='StaffLink.wipe'/>                                      ●      ↑
    Editor ⨯ Auditor
*/
```
- GEN: per family, under packs table of numbers tables →§20. `●` description names role for it; `↑` by seniority; empty row = no one; `A ⨯ B` never together; `✎` after cell = modifier edited that right (row from pack modifier → every cell).
- NOTE: pack rows before actor rows; RPC method called by either side = one row.
- RULE: matrix rewritten every run, never read. Only project the run is for writes it (rights after all layers); imported project's file keeps own matrix as is.
## §26.8 What the generator produces
KW: roles_<Family>, roles_<Family>L, roles_<Family>R, may_<Pack>, may_actor_<Actor>, May_, Deny, <name> Role, set_roles_, _lo, _hi
- GEN: per connection with side whose host carries family, each language of host:
  - `roles_<Family>` field of generated connection; both sides carry → `roles_<Family>L`, `roles_<Family>R`. Family with never-together pairs → written through setter checking pairs.
  - `may_<Pack>()` per pack under rule travelling on connection (nested pack by path: `may_Catalog_Item()`); `may_actor_<Actor>()` per actor/RPC method under rule (pack and actor of one name never clash): one AND of field with constant, only asks. Each has injection point with the check: `<Pack> Role`, `actor <Actor> Role`.
  - `Deny(what)` in injection point `Deny`: reports refusal to transport failure handler (no transport yet → receive error handler), connection stays, returns false.
  - `Refuse(what)`: violation on receipt: `Deny`, connection reset, receive stops.
- GEN: each rule checked once: receive = at pack id, before pack is made (`Refuse`: nothing made or read); send = when pack about to be queued, `send` and `cast` of each state (`Deny`, `send` → false, connection stays); broadcast = when about to be queued for a subscriber (`Deny`, subscriber does not get it, stays subscribed); actor = first in `OnReceiving` (cast: in `OnReceiving` of its group), and `send` methods of its states. Relayed frame not looked into: no rule of relay host applies, packs checked at tunnel ends. Pack in field of another pack not checked by own rule. Java, C#, Go, TS, Dart: pack made at id, so actor rule (read after headers) refuses pack already made; dropped there.
- GEN: nothing for connection no family reaches, nor for pack no rule names. WIRE: unchanged; rights never sent; family-typed field carries roles like any flags enum.

| Language | Roles of side | `Deny` reports |
|:--|:--|:--|
| C# | field of enum type; property with pairs check | `ext_connection.failure( UnauthorizedAccessException )`, `Abort()` |
| Java | `@<Family> long`; with pairs `roles_X()` / `roles_X( long )` | `failure( SecurityException )`, `abort()` |
| TypeScript, Dart | number; family with role on bit ≥32 → halves `roles_X_lo` (0..31), `roles_X_hi` (32..63), set together by `set_roles_X( lo, hi )` when pairs; enums `<Family>_lo`/`<Family>_hi` give each role's half | TS `fault( Error )`, Dart `fault( StateError )`; `abort()` |
| Rust | `AtomicU64`, `Relaxed`; `roles_X()` / `set_roles_X()` | `failure( io::Error PermissionDenied )`, `abort()` |
| C++ | `mutable std::atomic< std::uint64_t >`, relaxed; `roles_X()` / `set_roles_X()` | `failure( std::runtime_error )`, `abort()` |
| C | `adhoc·atomic·u64`; `may_` inline in header | `External·failure` with `adhoc·network·Error` kind `ERROR_KIND_PERMISSION_DENIED`: what, id, roles on stack, text formatted on request; `abort` |
| Go | `roles_X uint64`; `Roles_X()` / `SetRoles_X()`; `May_<Pack>()`, `May_actor_<Actor>()` | `Failure( error )`, `Abort()` |
| Swift | `Atomic<UInt64>`, relaxed; `roles_X()` / `set_roles_X()` | `failure( std.io.Error( .PermissionDenied ) )`, `abort()` |
| Python | property `roles_X` of connection, written through C setter | as C |

- RT: Rust, C++, Swift connection shared between threads → roles atomic (send from any thread + receive loop read while app writes). Relaxed load = plain load: nothing else depends on value; thread sees own write at once.
## §26.9 Overriding a rule on a host
KW: injection point, <name> Role, Deny, generated block, smart merge, //todo
- NOTE: description rules = defaults. Check of each pack/actor + reaction to refusal = generated blocks in injection points of connection →§25.8.1: edit in place (another condition, other reaction) → smart merge keeps edit; description changes that rule → new version beside it marked `//todo 🔴`. Delete block → generated one back.
```csharp
#region > SetPrice Role
    //🌭<…✎
    return (roles_Staff & (Desk.Staff)0x1a) != 0 && !night_shift;
    //🌭/>
#endregion > ….Role
```
- PITFALL: check runs at pack id: pack fields unknown, only connection (its roles, your fields, time). Content rule ("only its own order") → handler of received pack.
## §26.10 At runtime
KW: no roles, login, set roles, thread, exclusive, refusal, journal
- RT: side starts with no roles → everything under a rule refused until your code sets them. Set where peer known (login pack handler), on both sides: checking side + carrier itself (then refuses own wrong send before it goes).
- RT: roles set from another thread reach receive loop by its next pack; set in receive-loop handler for exact order.
- RT: every refusal → transport failure handler. Received against roles → connection reset at once. Send against roles → no send, `send` false (same as full queue: ask `may_<Pack>()` to tell apart), connection stays. Never-together roles → not set, connection stays. Pack queued before rights taken goes out; pack replayed from session journal checked on receipt like any other. Roles saved with session: restored session goes on with them.
## §26.11 Rules for roles at a glance
KW: errors, exit code 2, warning
- CLI: every error below → exit code 2.

| Situation | Response |
|:--|:--|
| >64 roles in family | error: split family |
| roles senior to each other in circle; named set containing itself | error |
| `\|` mixed with `+`/`-` in member | error |
| role senior to / never with role of another family | error |
| role never together with own junior/senior | error |
| name in expression not a role (removed by modifier?) | error |
| `[Can]` on named set | error |
| `<see cref='…'/>` with sign other than `+`/`-` in role doc | error |
| `+`/`-` in `[Role(…)]` | error |
| `[Role]` without roles on pack that is not modifier | error |
| `[Role(…)] class M : Modify<X>`, `X` not a pack | error |
| `[Role]` on connection | error |
| `[Modify<X>] enum`, `X` not `[Role]` enum | error |
| `<see cref='…'/>` on roles modifier other than `-` of target role | error |
| two edits of one role / two rules of one pack, projects neither importing other | error "Both '…' and '…' edit the role …" |
| two rules of one pack in one project | error "… Keep one of them." |
| `[Can<…>]` naming no pack | warning |
| family no host carries | warning: rules check nothing |

## §26.12 Pitfalls of roles
KW: authentication, host split, full name, TypeScript, Dart, modifier enum
- PITFALL: roles ≠ authentication: who peer is = your login; roles = what your code gives it after login.
- PITFALL: forbidden pack's code still in host (check refuses at run time). Client must not hold pack code → separate host →§10.
- PITFALL: `<see cref>` to family of imported project needs full name: doc comment does not look through interfaces project extends.
- PITFALL: TS/Dart: family with role on bit ≥32 → two halves; role value in plain enum >32 bits, cut by JS bitwise ops → take halves from `<Family>_lo`/`<Family>_hi`.
- PITFALL: modifier enum generates nothing → name target's roles (`Staff.Editor`) everywhere else.

# §27 Network configurations
KW: Chain, tls, tlsServer, noiseNK, noiseIK, noiseXX, noiseKK, onPeer, peerKey, chooseChain, wss://, Cloudflare, Chain.Pack, trust
SUMMARY: what a connection runs over (transport, layers under it, who decides): none of it is in the description; same generated code runs over any of these; choice is made in user code when a host listens or dials.
SUMMARY: all languages have same capabilities and same bytes on the wire (client in one language connects to server in any other); platform-forced differences →§27.13.

## §27.1 What a connection can run over
KW: TCP, WebSocket, TLS 1.3, Noise, NK, IK, XX, KK, UDP, Multiplex, layers, crypto library
| Layer | Gives | Costs | When absent |
|:--|:--|:--|:--|
| TCP | reliable byte stream | the socket | - |
| WebSocket | passes HTTP proxies, CDNs, firewalls; only socket a browser has | upgrade; frame header per send | plain TCP |
| TLS 1.3 | server certificate any client trusts; port 443 looks like the web; ends at a terminator | a certificate; a handshake; one state per connection | no certificate needed, no TLS library touched |
| Noise (NK, IK, XX, KK) | end-to-end encryption pinned by key, no CA; session keys for pack ciphers; passes through a TLS terminator sealed | a handshake; one state per connection | transport not encrypted, no cost; pack ciphers (→§16.9) have no key |
| UDP | datagrams, own delivery (→§14.6); a key gives Noise NK per datagram | - | - |
| Multiplex | one port, many connections: first byte names the connection (→§19) | one byte per socket | one connection type per port |
- RT: layer order from the wire: `TCP → TLS → WebSocket frames → Noise → packs`; any may be left out. All together worked in one call per buffer, no buffer of their own: TLS, frames and Noise open in place in the connection's receive buffer; on transmit in the buffer the packs were written to.
- RT: connection with neither TLS nor Noise never touches the crypto library; non-encrypting host does not need it installed.

## §27.2 Choosing a configuration
KW: scenario, wss://, Noise NK, IK, KK, XX, onPeer, Resumable, UDP, Chain.Pack
| Scenario | Configuration | Why |
|:--|:--|:--|
| hosts inside one trusted network, fastest | TCP, no key | nothing to pay |
| own client + own server over internet | TCP + server key (Noise NK) | encrypted, authenticated by pinned key; no certificates to renew |
| browser client | `wss://` + server key | browser has only WebSocket and its own TLS; key seals past any terminator |
| clients behind proxies/firewalls passing only web | `wss://` on port 443 | looks like the web |
| someone else's client/server speaking TLS (a site, standard WebSocket client) | TLS alone (`wss://` or TLS over TCP) | other side has no Noise |
| CDN or terminator in the middle, or both ways in at once | →§27.5 | TLS decided per connection by its address |
| server must know client by key | Noise IK (client knows server) or KK (both know each other), `onPeer` | client proves its key in handshake |
| two sides never met, decision on proved key | Noise XX, `onPeer` | each proves key, your code decides |
| one public port for many services/generations | Multiplex under any of the above (→§27.7, §19) | |
| socket lost, session must not be | `[Resumable]` under any of the above | client dials again, new handshake, session goes on |
| fresh samples matter more than old ones, or NAT and loss | UDP + key (→§14.6) | |
| middle hosts must not read | virtual connection with a chain, over any of the above (→§18) | |
| one document for one reader, stored or sent through anything | `Chain.Pack` (→§27.9) | |

## §27.3 The chain
KW: Chain, tls, tlsServer, noiseNK, noiseNKServer, noiseIK, noiseIKServer, noiseXX, noiseKK, onPeer, peerKey, handshake
- `Chain` = what the bytes of a connection go through. Made once, shared by all connections using it: certificate context is one; per-connection state comes from a pool only when that connection needs it.
| Member (Java names) | Side | Meaning |
|:--|:--|:--|
| `tls(host, alpn)` | client | TLS 1.3 under everything; `host` = what server certificate must carry |
| `tlsServer(certificateChainPem, keyPem)` | server | TLS 1.3 under everything |
| `noiseNK(serverPublic)` | client | knows server; same as key argument of `connect` |
| `noiseNKServer(secret)` | server | same as host's `key` |
| `noiseIK(serverPublic, mySecret)` | client | knows server, proves own key in first message |
| `noiseIKServer(secret)` | server | learns client's key from first message |
| `noiseXX(mySecret)` | both | neither knows other beforehand; each proves key; three messages |
| `noiseKK(mySecret, theirPublic)` | both | both know each other beforehand |
| `onPeer(key -> bool)` | both | hears proved key of other side; `false` closes connection. Not called where pattern gives no key: server of NK |
- RT: after handshake proved key is on the connection (`peerKey`). Chain chosen before connecting; pattern must match on both sides; wrong key or wrong pattern fails handshake and closes connection.
- NOTE: names above are Java's; each language spells them its own way →§27.3.1.

### §27.3.1 Dialing and listening, language by language
KW: Network.TCP.WebSocket.Client, WebSocketClient, connect, connectTls, connect_chain, ConnectChain, Builder, encryption_key, peer_key, PeerKey
| Runtime | WebSocket client | TCP client (plain, TLS, chain) | Server | Chain, peer key |
|:--|:--|:--|:--|:--|
| Java | `new Network.TCP.WebSocket.Client<>(…)`; `connect(URI.create("wss://h/"), timeout, serverKey, cb)`, `connect(chain, uri, timeout, cb)` | `new Network.TCP.Client<>(…)`; `connect(address, timeout, serverKey, cb)`, `connectTls(address, "h", alpn, timeout, serverKey, cb)`, `connect(chain, address, timeout, cb)` | `host.key`, `host.chain`, `host.chooseChain`, `host.bind(…)` | `new Chain().tls(…).noiseIK(…).onPeer(…)`; `external.noise.peerKey` |
| C# | `new Network.TCP.WebSocketClient<T>(…)`; `Connect(chain, new Uri("wss://h/"), timeout, cb, serverKey)` (`chain` may be `null`) | `new Network.TCP.Client<T>(…)`; `Connect(endPoint, timeout, cb, serverKey)`, `ConnectTls(endPoint, "h", alpn, timeout, cb, serverKey)`, `Connect(chain, endPoint, timeout, cb)` | `host.key`, `host.chain`, `host.chooseChain`, `host.bind(…)` | `new Chain().Tls(…).NoiseIK(…).OnPeer(…)`; `external.noise.peerKey` |
| Rust | `web_socket::Client::with_engine("c", &engine, seat)`; `connect("wss://h/", t, cb)`, `connect_with_key("wss://h/", t, Some(&key), None, cb)`, `connect_chain(chain, "wss://h/", t, cb)` | `client::Client::with_engine(…)`; `connect(addr, t, cb)`, `connect_with_key(addr, t, Some(&key), None, cb)`, `connect_tls(addr, "h", alpn, t, key, cb)`, `connect_chain(chain, addr, t, cb)` | `server::Builder::new("s", addr).on_connect(seat).encryption_key(secret)`, `.chain(…)`, `.choose_chain(…)`, `.web_socket()`, `.build()` | `Chain::default().tls("h", alpn).noise_ik(&key, &mine).on_peer(…)`; `connection.noise().and_then(\|n\| n.peer_key())` |
| C++ | `tcp::web_socket::Client::with_engine("c", engine, seat)`; `connect("wss://h/", t, cb)`, `connect_with_key("wss://h/", t, std::cref(key), std::nullopt, cb)`, `connect(chain, "wss://h/", t, cb)` | `tcp::Client::with_engine(…)`; `connect(addr, t, cb)`, `connect_with_key(addr, t, std::cref(key), std::nullopt, cb)`, `connect_tls(addr, "h", alpn, t, key, cb)`, `connect(chain, addr, t, cb)` | `tcp::server::Builder("s", addr).on_connect(seat).encryption_key(secret)`, `.chain(…)`, `.choose_chain(…)`, `.web_socket()`, `.build()` | `Chain().tls("h", alpn).noise_ik(key, mine).on_peer(…)`; `state->noise()->peer_key()` |
| C | `…·web_socket·client·Client·new("c", 1, size, seat)`; `…·connect(&ws, "wss://h/", n, ms, cb)`, `…·connect_with_key(&ws, url, n, ms, key32, NULL, cb)`, `…·connect_with_chain(&ws, url, n, ms, chain, NULL, cb)` | `adhoc·network·TCP·client·Client·new(…)`; `…·connect(&t, addr, ms, cb)`, `…·connect_with_key(&t, addr, ms, key32, NULL, cb)`, `…·connect_tls(&t, addr, "h", alpn, ms, key32, NULL, cb)`, `…·connect_with_chain(&t, addr, ms, chain, NULL, cb)` | `adhoc·network·TCP·server·Builder·new(…)`; `…·encryption_key`, `…·chain`, `…·choose_chain`, `…·web_socket` | `adhoc·network·Chain·new()`; `…·tls`, `…·noise_ik(c, key, mine)`, `…·on_peer(c, fn, ctx)`; `adhoc·network·noise_adapter·NoiseHandle·peer_key(&handle)` |
| Swift | `T.WebSocket.Client("c", engine, seat)`; `connect("wss://h/", ms, cb)`, `connect_with_key("wss://h/", ms, key, nil, cb)`, `connect_chain(chain, "wss://h/", ms, cb)` | `T.Client("c", engine, seat)`; `connect(addr, ms, cb)`, `connect_with_key(addr, ms, key, nil, cb)`, `connect_tls(addr, "h", alpn, ms, key, cb)`, `connect_chain(chain, addr, ms, cb)` | `T.Server.Builder("s", addr).on_connect(seat)`; `.encryption_key(secret)`, `.chain(…)`, `.choose_chain { … }`, `.web_socket()`, `.build()` | `C().tls("h", alpn).noise_ik(key, mine).on_peer { … }`; `state.noise()?.peer_key()` |
| Go | `Network.NewWebSocketClient(name, newInternal, onFailure, size, mux)`; `Connect("wss://h/", d, serverKey)`, `ConnectChain(chain, "wss://h/", d)` | `Network.NewTCPClient(…)`; `Connect(addr, d, serverKey)`, `ConnectTls(addr, "h", alpn, d, serverKey)`, `ConnectChain(chain, addr, d)` | `Network.NewTCPServer(…)`; `s.Key`, `s.Chain`, `s.ChooseChain` before `s.Bind(…)` | `new(Network.Chain).Tls("h", alpn).NoiseIK(key, mine).OnPeer(…)`; `conn.PeerKey()` |
| Dart | `Network.TCP_WebSocket_Client<…>(…, NetworkIO.webSocketConnector(), o)`; `connect("wss://h/", ms, serverKey, chain)` | `Network.TCP_Client<…>(…, NetworkIO.tcpConnector(), o)`; `connect("h:7000", ms, serverKey, chain)` with `Chain().tls("h", alpn)` | `Network.TCP_Server<…>(…)`; `.key`, `.chain`, `.chooseChain`, `NetworkIO.listenTcp(…)`, `NetworkIO.listenWebSocket(…)` | `Chain().tls("h", alpn).noiseIK(key, mine).onPeer(…)`; `client.connection.peerKey` |
| Python | `TCP.WebSocket.Client("c", size, Channel.Connection)`; `connect("wss://h/", ms, done, server_key = key, chain = chain)` | `TCP.Client("c", size, Channel.Connection)`; `connect("ip:port", ms, done, server_key = key, chain = chain)` | `TCP.Server("s", address, Channel.Connection, web_socket = True, key = secret, chain = c, choose_chain = fn)` | `TCP.Chain().tls("h", alpn).noise_ik(key, mine).on_peer(fn)`; `connection.peer_key` |
| TypeScript, Node | `new Network.WebSocketClient( ext => new Channel( ), { bufferSize } )`; `connect( "wss://h/", ms, serverKey )`, `connect( "wss://h/", ms, chain )` | `new Network.TcpClient( … )`; `connect( "h:9000", ms, serverKey )`, `connect( "h:9000", ms, new Chain( ).tls( "h", alpn ) )` | `new Network.TcpServer( ( ext, id ) => …, { bufferSize } )`, `new Network.WebSocketServer( … )`; `s.key`, `s.chain`, `s.chooseChain`, `s.bind( port, address )` | `new Chain( ).tls( "h", alpn ).noiseIK( key, mine ).onPeer( … )`; `client.connection.peerKey` |
| TypeScript, browser | `new Network.WebSocketClient( name, newInternal, onFailure, size, mux )`; `connect( "wss://h/", ms, serverKey )`, `connect( "wss://h/", ms, chain )` | - | - | `new Network.Chain( ).noiseIK( key, mine ).onPeer( … )`; `client.connection.peerKey` |
| TypeScript, Cloudflare Worker | `TCP.WebSocket.Client` | - | `createWorker( c => new Channel( ), { pathname, serverKey, chain, chooseChain } )` | `new Chain( ).noiseXX( secret )` |
- RT: `wss://` in a WebSocket address gives TLS by itself: port 443 unless named, host name of the address for the certificate, ALPN `http/1.1`. Server key or chain given with it goes inside the TLS. Plain `ws://` = only way to a WebSocket without TLS.
```java
// Java
var client = new Network.TCP.WebSocket.Client<>( "app", ext -> new Channel.Connection( ), 1 << 16 );
client.connect( URI.create( "wss://example.com/" ), 5000, serverPublic, ( connection, error ) -> { } ); // TLS, then Noise NK
var server = new Network.TCP.Server( "edge", host -> {
    var ext = new Network.TCP.WebSocket( host );
    new Channel.Connection( ).External( ext );
    return ext;
}, Network.TCP.onFailurePrintConsole, 1 << 16 );
server.key = secret;                                                    // Noise NK
server.chain = new Chain( ).tlsServer( certificateChainPem, keyPem );   // TLS under it
server.bind( new InetSocketAddress( 443 ) );
```
```csharp
// C#
var client = new Network.TCP.WebSocketClient<Channel.Connection>( "app", ext => new Channel.Connection( ), 1 << 16 );
client.Connect( null, new Uri( "wss://example.com/" ), 5000, ( connection, error ) => { }, serverPublic ); // TLS, then Noise NK
var server = new Network.TCP.Server( "edge", Channel.Connection.NewWebSocketConnection, Network.TCP.onFailurePrintConsole, 1 << 16, 100, null );
server.key   = secret;                                                  // Noise NK
server.chain = new Chain( ).TlsServer( certificateChainPem, keyPem );   // TLS under it
server.bind( 100, null, new IPEndPoint( IPAddress.Any, 443 ) );
```
- NOTE: C# has two WebSocket clients: `Network.TCP.WebSocketClient<T>` (above) goes through the chain, one native call per buffer; `Network.TCP.WebSocket.Client<T>` stays on the platform's `ClientWebSocket` and its `ConnectAsync`.

## §27.4 Choosing per connection, by address
KW: chooseChain, choose_chain, ChooseChain, null, address, pool, decided once
- RT: server decides on each incoming connection by where it comes from, before a byte is read. `chooseChain` takes the peer's address, returns that connection's chain; `null` = none. Replaces `chain`; `key` stays, so a connection that got `null` still runs Noise NK when host has a key.
| Function returned | Connection gets |
|:--|:--|
| `null`, host has no key | nothing: socket used as it is |
| `null`, host has a key | Noise NK from the pool |
| chain with TLS | TLS from the pool, certificate context shared by all |
| chain with Noise | that pattern from the pool |
- RT: cost paid only by the connection that needs it: state comes from a pool at open, returns at close.
- PITFALL: decided once, at connect. TLS 1.3 has no way to turn off, and a buffer already read could hold open bytes and start of TLS mixed. To change layers, open a new connection.
- NOTE: address = what the socket reports; each language hands it in its own type:
| Runtime | Function |
|:--|:--|
| Java | `host.chooseChain = address -> …` with `SocketAddress` |
| C# | `host.chooseChain = address => …` with `EndPoint?` |
| Rust | `.choose_chain( move \|peer: SocketAddr\| … )` returning `Option<Arc<Chain>>` |
| C++ | `.choose_chain( []( nw::mio::Address from ) -> std::shared_ptr<const Chain> { … } )` |
| C | `…·Builder·choose_chain( &b, pick, ctx )`, `adhoc·network·Chain *pick( void *ctx, adhoc·network·SockAddr peer )` |
| Swift | `.choose_chain { peer in … }` |
| Go | `s.ChooseChain = func( a net.Addr ) *Network.Chain { … }` |
| Dart | `server.chooseChain = ( address ) => …` with `String` |
| Python | `choose_chain = lambda address: …` with `"ip:port"` |
| TypeScript | `s.chooseChain = address => …`, `undefined` for none (Node, Worker) |

## §27.5 Through Cloudflare and directly
KW: Cloudflare, Flexible, Origin Rule, cloudflared, Spectrum, CF-Connecting-IP, Authenticated Origin Pulls, Pulse_, orange cloud, createWorker, terminator
- One server, one port, two ways in. Directly: client connects to server's own address over `wss://`; server does TLS with its own certificate. Through Cloudflare: client connects to Cloudflare over `wss://`; Cloudflare ends TLS and comes to the server without it; server must not expect TLS from it.
- RT: server tells the two apart by socket address: `chooseChain` returns no chain for Cloudflare's address, the TLS chain for everything else. Noise runs inside on both ways, so Cloudflare carries what it cannot read (same Noise bytes client → server on both ways).
- Scheme: client `wss://cf.example.com` → Cloudflare (TLS: Cloudflare certificate) → server, no TLS; client `wss://direct.example.com:8443` → server (TLS: server certificate); one port, `chooseChain` by address.

- NOTE: Cloudflare settings (DNS, Flexible, Origin Rule, WebSocket, Spectrum):
| What | Setting |
|:--|:--|
| DNS | two names: `cf.example.com` proxied (orange cloud), `direct.example.com` DNS only (grey cloud). Proxied name resolves to Cloudflare, so a direct client needs a name of its own |
| SSL/TLS mode | Flexible: Cloudflare comes to the server over plain HTTP. Applies to HTTPS on port 443; on other HTTPS ports Cloudflare falls back to Full, which brings TLS to the server |
| Port of the server | Origin Rule sets destination port to the port the server listens on; without it Flexible comes to port 80 |
| WebSocket | on by default on all plans |
| Certificate | at the edge, Cloudflare's. For the direct name, your own, PEM: client checks it against `direct.example.com`. Cloudflare Origin CA certificate only if client trusts that root itself: only Cloudflare's proxy trusts it. Let's Encrypt with DNS-01 through Cloudflare API needs no open port |
| Only WebSocket | proxied ports take HTTP and WebSocket. Raw TCP through Cloudflare needs Spectrum: a client on the Cloudflare way must be a WebSocket client |

- DECL: on the server. Cloudflare's address list is published at <https://www.cloudflare.com/ips/>; belongs in your code or configuration: changes rarely; who is let in by which way is yours to decide.
```java
// Java
final Chain tls = new Chain( ).tlsServer( certificateChainPem, keyPem );      // one context for all connections
final var server = new Network.TCP.Server( "edge", host -> {
    var ext = new Network.TCP.WebSocket( host );
    new Channel.Connection( ).External( ext );
    return ext;
}, onFailure, 1 << 16 );
server.key         = secret;                                                  // Noise NK on both ways
server.chooseChain = address -> cloudflare( address ) ? null : tls;           // null - socket as it is, and the key
server.bind( new InetSocketAddress( 8443 ) );
```
```csharp
// C#
var tls    = new Chain( ).TlsServer( certificateChainPem, keyPem );
var server = new Network.TCP.Server( "edge", Channel.Connection.NewWebSocketConnection, onFailure, 1 << 16, 100, null );
server.key         = secret;
server.chooseChain = address => Cloudflare( address ) ? null : tls;
server.bind( 100, null, new IPEndPoint( IPAddress.Any, 8443 ) );
```
```rust
// Rust
let tls = Arc::new( Chain::default( ).tls_server( &cert_pem, &key_pem ) );
server::Builder::new( "edge", addr ).on_connect( seat ).web_socket( ).encryption_key( secret )
    .choose_chain( move |peer: SocketAddr| ( !cloudflare( peer.ip( ) ) ).then( || tls.clone( ) ) ).build( )?;
```
```cpp
// C++
auto tls = std::make_shared<const Chain>( Chain( ).tls_server( cert_pem, key_pem ) );
auto s = tcp::server::Builder( "edge", address ).engine( engine ).on_connect( seat ).web_socket( ).encryption_key( secret )
           .choose_chain( [ tls ]( nw::mio::Address from ) -> std::shared_ptr<const Chain> { return cloudflare( from ) ? nullptr : tls; } ).build( );
```
```c
// C
static adhoc·network·Chain *pick( void *tls, adhoc·network·SockAddr peer ) { return cloudflare( peer ) ? NULL : tls; }
adhoc·network·Chain *tls = adhoc·network·Chain·new( );
adhoc·network·Chain·tls_server( tls, cert_pem, cert_n, key_pem, key_n );
adhoc·network·TCP·server·Builder b = adhoc·network·TCP·server·Builder·new( "edge", 1, addr );
adhoc·network·TCP·server·Builder·on_connect( &b, seat );
adhoc·network·TCP·server·Builder·web_socket( &b );
adhoc·network·TCP·server·Builder·encryption_key( &b, secret );
adhoc·network·TCP·server·Builder·choose_chain( &b, pick, tls );
```
```swift
// Swift
let tls = try C( ).tls_server( certPem, keyPem )
let b = T.Server.Builder( "edge", addr ).engine( engine ).on_connect( seat )
b.web_socket( )
b.encryption_key( secret )
b.choose_chain { peer in cloudflare( peer ) ? nil : tls }
let server = try b.build( )
```
```go
// Go
tls := new( Network.Chain ).TlsServer( certPem, keyPem )
s, _ := Network.NewTCPServer( "edge", func( h *Network.TCPBase ) *Network.TCPExternalConnection {
	w := Network.NewWebSocket( h )
	w.Init( newInternal( ) )
	return w.TCPExternalConnection
}, nil, nil, 1<<16, 0 )
s.Key = secret
s.ChooseChain = func( a net.Addr ) *Network.Chain {
	if cloudflare( a ) { return nil }
	return tls
}
s.Bind( &net.TCPAddr{ Port: 8443 } )
```
```dart
// Dart
final tls = Chain( ).tlsServer( certPem, keyPem );
final s = Network.TCP_Server<Channel>( ( e, id ) => make( ), o, ( h ) => Network.TCP_WebSocket( h ) );
s.key = secret;
s.chooseChain = ( address ) => cloudflare( address ) ? null : tls;
await NetworkIO.listenTcp( s, io.InternetAddress.anyIPv4, 8443, webSocket: true );
```
```python
# Python
tls = TCP.Chain( ).tls_server( cert_pem, key_pem )
server = TCP.Server( "edge", "0.0.0.0:8443", Channel.Connection, web_socket = True, key = secret,
                     choose_chain = lambda address: None if cloudflare( address ) else tls )
```
```typescript
// TypeScript on Node
const tls = new Chain( ).tlsServer( certPem, keyPem );
const s = new Network.WebSocketServer( ( ext, id ) => new Channel( ), { bufferSize: 65536 } );
s.key = secret;
s.chooseChain = address => cloudflare( address ) ? undefined : tls;
await s.bind( 8443, "0.0.0.0" );
```

- DECL: on the client: client names the way by its address; rest is the same.
```java
client.connect( URI.create( "wss://cf.example.com/" ), 5000, serverPublic, onConnected );          // through Cloudflare
client.connect( URI.create( "wss://direct.example.com:8443/" ), 5000, serverPublic, onConnected ); // directly
```
```typescript
// a browser: TLS is the browser's, Noise runs in the page
client.connect( "wss://cf.example.com/", 5000, serverPublic );
```
- RT: client falling back from one way to the other dials the other address with the same object: a parked `[Resumable]` session goes on over the new socket, whichever way it came.

- NOTE: what holds on the Cloudflare way, and what does not (`CF-Connecting-IP`, Authenticated Origin Pulls, idle, `Pulse_`):
- Keep Noise: between Cloudflare and server there is no TLS, and Cloudflare itself sees everything TLS carried. With a server key it sees sealed bytes only; a pinned client cannot be answered by anyone without the secret.
- PITFALL: an address in the Cloudflare list does not prove the connection is yours. Cloudflare addresses serve every Cloudflare customer: anyone can point a name of their own at your server's IP and arrive without TLS from a listed address. Give that way no more trust than the direct one: such visitor still faces Noise, and with IK or KK has no key to prove. Cloudflare's Authenticated Origin Pulls do not work under Flexible.
- PITFALL: address the server sees is Cloudflare's, not the client's; a per-address limit counts Cloudflare, not visitors. Visitor's address travels in the `CF-Connecting-IP` header of the upgrade; runtime reads the upgrade for its key only and does not hand that header to your code.
- RT: Cloudflare closes idle sockets and restarts its own servers, which ends a WebSocket. On an idle line the connection's `[ReceiveTimeout]` (→§11.7) keeps `Pulse_` going at a third of it. Cloudflare publishes no one figure for idle WebSocket; reports speak of 100 seconds: keep it at 60 seconds or less. A restart looks like any lost socket: `[Resumable]` connection dials again and goes on.
- NOTE: line to Cloudflare can be slow; if so, keep the direct way for hosts of your own and leave Cloudflare to the browsers.

- RT: the same through a tunnel: with `cloudflared` the server has no public port for Cloudflare at all; what arrives comes from the tunnel's local address. Decide by that:
```java
server.chooseChain = address -> ( (InetSocketAddress)address ).getAddress( ).isLoopbackAddress( ) ? null : tls;
```
- NOTE: tunnel configuration: service is `http://localhost:<port>`. No Cloudflare address list, no Origin Rule; no stranger can arrive this way unless a process runs on the server itself.
- NOTE: Cloudflare Worker as the server needs none of this: platform ends TLS; `createWorker` takes `serverKey` or `chain` for Noise inside.
- NOTE: other terminators (nginx, HAProxy, cloud load balancer) work the same way: their address gets `null`, everything else gets TLS.
- NOTE: if Cloudflare must carry TLS to the server (Full or Full strict): return the TLS chain for all: set `host.chain`, drop `chooseChain`.

## §27.6 Keeping connections through reconnects
KW: reconnect, Resumable, handshake, chain kept, keyring
- RT: same client connects again with same chain and same key: every socket has a new TLS and Noise handshake. Client keeps its chain for the next connect and for its own dials until another is given.
- RT: under a chain nothing else changes: a `[Resumable]` session (→§14.2) parks when socket is lost and goes on over the new one; the new handshake adds its key to the session.

## §27.7 Multiplexing under a chain
KW: mux, multiplex, first byte, Internal.id, internals, dialing, factory, REMOTE_CONNECT, WEBSOCKET_REMOTE_CONNECT
- RT: a multiplexed port (→§19) works under any chain: the byte naming the connection is first thing inside the layers, in the first WebSocket frame, sealed with the rest. Server chooses chain by address first, connection by the byte after.
| Runtime | Client | Server |
|:--|:--|:--|
| Java | `new Network.TCP.Client<>( name, newInternal, onFailure, engine, true )`, same on `WebSocket.Client`; byte = `Internal.id()` | `host.internals = id -> …` |
| C# | `new Network.TCP.Client<T>( name, newInternal, onFailure, bufferSize, true )`, `WebSocketClient<T>( …, mux: true )`; byte = `Internal.id` | `host.internals = id => …` |
| Rust | `mux::dialing( id, seat )` | `mux::factory( pick, Event::REMOTE_CONNECT )` (`WEBSOCKET_REMOTE_CONNECT`) |
| C++ | `nw::mux::dialing( id, streams )` | `nw::mux::factory( pick, tcp::Event::REMOTE_CONNECT )` (`WEBSOCKET_REMOTE_CONNECT`) |
| C | `adhoc·network·mux·dialing( id, seat )` | `adhoc·network·mux·factory( pick, …Event·REMOTE_CONNECT )` |
| Swift | `org.unirail.Network.mux.dialing( id, seat )` | `org.unirail.Network.mux.factory( pick, .REMOTE_CONNECT )` |
| Go | `NewTCPClient( …, true )`, `NewWebSocketClient( …, true )`; byte = `Id()` | `internals` of `NewTCPServer`: `func( id int ) Connection.Internal` |
| Dart | `Options( mux: true )`; byte = `Internal.id` | `Options( mux: true )`, `newInternal( ext, id )` |
| Python | `TCP.Client( …, mux = id )`, `TCP.WebSocket.Client( …, mux = id )` | `TCP.Server( …, internals = { id: Channel.Connection } )` |
| TypeScript | `new Network.WebSocketClient( ext => …, { mux: true } )` (Node), last argument of the browser's constructor; byte = `Internal.id` | `new Network.TcpServer( ( ext, id ) => …, { mux: true } )`, `new Network.WebSocketServer( … )` |

## §27.8 Which certificates a client trusts
KW: trust, set_trust, ChainTrust, ChainTrustFunc, roots, pin, DER, X509Certificate2, system roots, Windows
- DEFAULT: client takes the system roots and checks host name. For a chain signed by own authority, or a pinned certificate, give a function of trust: it replaces the roots; host name still checked. Wrong certificate fails the connect.
| Runtime | Function |
|:--|:--|
| Java | `Chain.trust = trustManager`; JDK roots by default |
| C# | `Chain.trust = certificates => …`; `X509Certificate2[]`, leaf first |
| Rust | `chain::set_trust( Some( Arc::new( \|certs: &[&[u8]]\| … ) ) )`; DER, leaf first |
| C++ | `Chain::trust( []( std::span<const AdHoc::Bytes> shown ) { … } )`; DER, leaf first |
| C | `adhoc·network·Chain·trust( chain, fn, ctx )`, `…·trust_of_process( fn, ctx )` |
| Swift | `C.trust = { der in … }` |
| Go | `Network.ChainTrust = pool`, `Network.ChainTrustFunc = func( certs []*x509.Certificate ) bool` |
| Dart | `Chain.trust = ( chain ) => …`; DER, leaf first |
| Python | `TCP.Chain( ).tls( "h" ).trust( lambda der_list: … )` |
| TypeScript | Node: `new Chain( ).tls( "h" ).trust( ( der, host ) => … )`, DER, leaf first. Browser and Worker: platform's, `trust` refuses |
- RULE: set it before the first connection.
- PITFALL: Rust, C, C++, Swift, Dart read system roots on Windows only so far: elsewhere a client without a trust function refuses every server.

## §27.9 A pack sealed on its own
KW: Chain.Pack, pair, signedBy, signerOf, seal, open, HEADER, size, Crypto.ed25519KeyPair, Ed25519, X25519
- `Chain.Pack` = seals one buffer for one reader and signs it for any reader, no connection: for a message stored, or travelling through hosts not to read it. Same on the wire in all languages.
```java
final var out = new Chain.Pack( ).pair( mySecret, theirPublic, "envelope" ).signedBy( signingSecret );
final int total = out.seal( buf, 0, length );         // text lies from Chain.Pack.HEADER; out.size( length ) says how much room it takes
final var in = new Chain.Pack( ).pair( theirSecret, myPublic, "envelope" ).signerOf( signingPublic );
final int n = in.open( buf, 0, total );               // negative - forged
```
- `pair` seals (X25519, then ChaCha20-Poly1305); `signedBy`/`signerOf` sign/check (Ed25519); either may be left out. Signing key pair from `Crypto.ed25519KeyPair`.
- NOTE: not yet wired to attributes in a description: called from your code.

## §27.10 Buffers and what a chain costs
KW: buffer size, 256 bytes, 3.7 KB, in place, relay, one write, receive speed
- DEFAULT: buffer size from 256 bytes, any record size; a pack longer than the buffer goes in pieces.
- RT: nothing is moved on receive: records and frames opened where they lie, application handed the pieces in turn, so a long buffer costs several calls and no copying. Only bytes moved: a Noise record straddling the edge of a frame or a TLS record.
- RT: relay under a chain copies what it forwards; it takes the receive buffer away only when no layer opens records in it.
- NOTE: cost per connection with TLS and Noise: about 3.7 KB native state, returned to the pool at close. TLS and frames over Noise take about half of the receive speed of plain Noise. Each send = one write of one buffer, so a larger buffer brings speed.

## §27.11 How long a handshake may take
KW: handshakeTimeout, handshake_timeout, HandshakeTimeout, silent peer, not TLS
- DEFAULT: server limit on TLS + Noise + WebSocket upgrade of accepted connection: 30 s; zero or less = none. Set before listening. No handshake (plain TCP, no key, no chain) → no limit, no cost.
- RT: expires → `onFailure`, close. After handshakes connection's `[ReceiveTimeout]` applies →§11. Bytes that cannot be TLS (plain HTTP on TLS port) refused at once whatever the limit.
| Runtime | Limit, ms unless said |
|:--|:--|
| Java, C#, Dart | `server.handshakeTimeout = 10_000;` |
| Rust | `.handshake_timeout( 10_000 )` on `server::Builder` |
| C++ | `.handshake_timeout( 10'000 )` on `tcp::server::Builder` |
| C | `adhoc·network·TCP·server·Builder·handshake_timeout( &b, 10000 )` |
| Swift | `.handshake_timeout( 10_000 )` on `T.Server.Builder` |
| Go | `s.HandshakeTimeout = 10 * time.Second` before `Bind` |
| Python | `TCP.Server( …, handshake_timeout = 10000 )` |
| TypeScript | Node: `server.handshakeTimeout = 10_000;`. Worker: platform's |
- NOTE: slow lines need more: TLS handshake through bad line can take tens of seconds.

## §27.12 Failures
KW: onFailure, close_notify, KeyUpdate, forged record, handshake failure, onPeer refused
- RT: forged or broken TLS record, forged Noise record, wrong WebSocket frame close the connection; stream ends at the first one, `onFailure` hears it.
- RT: failed certificate, wrong key, refused `onPeer` fail the handshake: the connect reports the error.
- RT: before closing, TLS sends `close_notify`: other side tells clean end from a cut.
- RT: long-lived TLS connection renews keys by KeyUpdate by itself, both ways. Go and TypeScript on Node answer a KeyUpdate but do not start one: their TLS has no call for it.
- NOTE: a proxy that splits or merges WebSocket frames, or inserts control frames between pieces, changes nothing.

## §27.13 Differences between runtimes
KW: adhoc-crypto, BoringSSL, dart:ffi, crypto/tls, node:tls, WebAssembly, InTS/wasm, maxPendingBytes, browser, Worker, AdHoc.Crypto, lib/native, nuget.config, ADHOC_CRYPTO_NATIVE
| Runtime | How built |
|:--|:--|
| Java, C#, Rust, C, C++, Swift | everything through native library `adhoc-crypto` on BoringSSL: ciphers, Noise, TLS, frames, one call a buffer |
| Python | a layer over C |
| Dart | on `dart:io` through `dart:ffi`, as the above. In a browser: TLS is the platform's, Noise and the pack on its own Dart ciphers. UDP and stage ciphers in Dart |
| Go | `crypto/tls`, `crypto/ecdh`, `x/crypto`, measured to run at same speed without cgo. One copy more per record. Answers KeyUpdate but does not start it. Write timeout under TLS is final |
| TypeScript | TLS is the platform's: `node:tls` on Node, with `Network.node.ts` for TCP clients and servers; nothing of the kind in a browser, where `tls()` and `trust()` refuse. Noise and the pack on a WebAssembly module of its own (`InTS/wasm`), compiled only when a key or a chain asks |
- PITFALL: browser has no TCP and no server; its receive cannot refuse bytes: they pile up to `maxPendingBytes`. TLS records, `close_notify`, KeyUpdate and certificates are the browser's own concern. Cloudflare Worker serves WebSocket only and dials WebSocket only.
- Native library comes from the latest release of `AdHoc-Protocol/adhoc-crypto`; generated build files take it at the first build. C#: `Project.csproj` downloads `AdHoc.Crypto.nupkg` into `obj/adhoc-crypto`; `dotnet build`/`publish` lay the library out; own project: see head of `Chain.cs`. Java: `pom.xml`, `build.gradle`, `build.sbt` take `adhoc-crypto.jar`. C, C++, Python: `lib/adhoc_crypto.cmake` puts the file next to the program or module `_adhoc`. Rust: `build.rs` with `curl`. Dart on `dart:io`: `hook/build.dart` downloads the file or takes it from `lib/native/<rid>`, SDK bundles it. Swift: SwiftPM cannot fetch; `Package.swift` names the address, download `adhoc-crypto-<rid>.<ext>` and put it next to the program as `libadhoc-crypto-0.1.<ext>`. `ADHOC_CRYPTO=off` fetches nothing, `ADHOC_CRYPTO_URL` fetches from another place. `ADHOC_CRYPTO_NATIVE` (full file name) comes first where a language has it. No chain and no key: library not loaded; failed download in C, C++, Python, Rust only warns.
- Revocation of a certificate is not checked by default (the check goes to the network during the handshake, a certificate with no answer fails). `checkRevocation` of `Chain` turns it on before the first connection: `check_revocation` in C++ and Python, `CHECK_REVOCATION` in Rust, `adhoc·crypto·check_revocation` in C. C#, Java: every system; C, C++, Python, Rust, Swift, Dart: roots of Windows only. Go, TypeScript: no switch, their TLS does not check it; use a trust function.
