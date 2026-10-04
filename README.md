# AdHoc

**Describe a whole system in one C# file. Get the network code of every program in it, each in its own language.**

AdHoc is a code generator for binary protocols that describes systems, not only messages. In one C# description you declare the
data your programs exchange, the programs themselves and their languages, the connections between them, the conversation on each
connection as a state machine, and the delivery rules: what survives a dropped socket, what goes over UDP, what is compressed or
encrypted, and what a relay or a store may carry without reading it. AdHocAgent checks the description on your machine. The cloud
generator then writes the code of every program: C#, Java and TypeScript today, with C++, Rust and Go planned. AdHocAgent deploys
that code into your projects and carries your own code over into every new version.

![A fleet platform built from one AdHoc description. On the left, three peers dial one multiplexed port, :443, of a Gateway written in C#: a Vehicle on firmware 3.x and a VehicleLegacy on firmware 2.x, both in Java, and a Service diagnostic laptop in C#. One port serves three connections - Telemetry, TelemetryLegacy and Diagnostics - and two firmware generations. The Gateway talks to a Fleet backend in Java over a [Resumable(60)] connection: sessions outlive their sockets, and commands, declared as a guaranteed leg, arrive once and in order. Fleet runs an actor per vehicle with a state machine, RPC and deadlines, and is replicated through Connects<Fleet, Fleet>. A field Sensor in C# talks to Fleet over [UDP<Samples>(10)]: every pack is guaranteed except the samples. An Operator in a browser, written in TypeScript, reaches Fleet through an Edge host whose Relay forwards sealed bytes only: VirtuallyConnects<Operator, Fleet, Edge> with [Zstd, ChaCha20] end to end. On the right, an Archive in Java keeps events compressed and encrypted through a trimmed chain: no schema, no key. The caption reads: one C# file, 9 host types, 3 languages, 10 connections; every line here is generated code; framing, state machines, reconnects, replay, port sharing, relaying and encryption - none of it written by hand.](docs/img/tldr-system.svg)

Every line on this picture is a generated link, and every label on it is a line or two of the description. Here is what the
description holds, what the generator makes of it, and what the code does at runtime:

| Describe | Generate | Run |
|:--|:--|:--|
| One C# file in your IDE: packs and fields, hosts and languages, connections, states and branches, sessions, streams, chains, relays, ports | AdHocAgent checks every rule locally and uploads only a description that passes. The generator returns the code of every host in each of its languages and tests it first; how far depends on the server's load. Generation is free. | Packs travel as bytes whose layout both ends know. Two fixed buffers per connection carry them, whatever the size, and both ends run the same state machine. |

**On this page:** [What AdHoc solves](#what-adhoc-solves) · [How AdHoc is organized](#how-adhoc-is-organized) ·
[Why a binary protocol, not HTTP](#why-a-binary-protocol-not-http) · [Packs that cost almost nothing](#packs-that-cost-almost-nothing) · [Virtual connections](#virtual-connections-through-hosts-that-cannot-read) · [Why generate code from a description](#why-generate-code-from-a-description) ·
[Ready solutions](#ready-solutions) · [Capabilities](#capabilities) · [When AdHoc is not the right tool](#when-adhoc-is-not-the-right-tool) ·
[Getting started](#getting-started) · [Learn more](#learn-more)

**The full manual:** [MANUAL.md](MANUAL.md). It covers how a description is written, what the generator produces from it and how the
generated code behaves at runtime.

## What AdHoc solves

*The code a networked system needs besides its own logic, and the declarations that replace it*

Most of the code in a networked system is not business logic. It moves data. There are serializers and parsers in every language, a
dispatch switch on every connection, and checks that a message may arrive now. There are reconnect and replay, retransmission on lossy
links, routers in front of ports, version branches, proxies that parse and re-encode, and chunking for payloads too big for memory.
All of it is written by hand, separately on each side and in each language, and kept in step by discipline alone. In AdHoc you
declare what you need, and the generator writes that code for every side and every language.

![Hand-written plumbing against AdHoc declarations. On the left, under the title "Written by hand - per message, per side, per language", a tall grey stack of eleven blocks with two offset copies behind it, three copies in all, tabbed C#, Java and TypeScript. Its blocks, top to bottom: an encoder and a decoder for every message; a dispatch switch and "may this arrive now?" flags; correlation ids and a map of pending calls; reconnect, session restore and a resend queue; sequence numbers, acks and a retransmit window; chunking and temp files for big payloads; compression and cipher wrappers; a store that decrypts, decodes and re-encodes; a proxy that parses and holds the keys; a router in front, or a port per service; a version field with if (version >= 3) branches. A note under the stack: times both sides of every connection, kept in step by hand. On the right, an orange card titled "Declared once - one line in the description" holds one line per block, each block joined to its line by an arrow: class Order { [MinMax(1, 100)] byte quantity; }; [L____________<Shopping, Login>] struct Start { }; (L____________, Pong) ping(Ping req); [Resumable(30)] plus Resumable<Client, Order>; [UDP<Samples>(10)]; [S(200_000)] Stream feed; [Zstd, ChaCha20]; [ToStream<FromCamera>] Snapshot snapshot; VirtuallyConnects<Operator, Fleet, Edge>; Multiplex<(Telemetry, TelemetryV2, Console)>; struct VehicleV2 : Host, a new generation. An arrow from the card to a yellow band at the bottom: generated for every host, in each of its languages - C#, Java, TypeScript - the same bytes on the wire; your code says what a pack means and what a state starts, in injection points kept by smart merge.](docs/img/readme-hand-vs-declared.svg)

### The code you no longer write

The labels on the [picture at the top](#adhoc) come from these rows.

| What the system needs | Written by hand | Declared in AdHoc | More |
|:--|:--|:--|:--|
| **Messages as bytes, in every language** | an encoder and a decoder per message and language, kept in step by tests | `class Order { long item; [MinMax(1, 100)] byte quantity; }` - the code of both ends, in every language of each host; `quantity` travels in 7 bits | [Designed bytes](#designed-bytes-encodings-that-follow-your-declarations) |
| **Dispatch and the order of messages** | a switch on a type field; flags checked in every handler, on both sides | `[L____________<Shopping, Login>] struct Start { }` - one state machine, generated for both ends; a pack the state does not allow is refused | [Conversations](#conversations-connections-actors-states-rpc) |
| **Requests and replies** | correlation ids, a map of pending calls, cleanup | `(L____________, Pong) ping(Ping req);` - the reply is checked against the state and reaches the code that asked | [Conversations](#conversations-connections-actors-states-rpc) |
| **A dropped connection** | reconnect, session restore, acknowledgements, a resend queue, deduplication | `[Resumable(30)]` parks a lost session for up to 30 minutes; `Resumable<Client, Client.Order>` makes the orders arrive once and in order | [Sessions](#sessions-that-outlive-sockets-guaranteed-delivery-udp) |
| **Lossy radio links** | sequence numbers, acknowledgements, a retransmit window | `[UDP<Samples>(10)]` - over UDP, every pack guaranteed except the samples | [Sessions](#sessions-that-outlive-sockets-guaranteed-delivery-udp) |
| **Big and live payloads** | whole bodies in memory, temp files, hand-made chunking | `[S(200_000)] Stream feed;` - up to the cap you declare (here 200 000 bytes), from your source to your sink through the connection's two fixed buffers, never held whole | [Streams](#streams-any-size-through-fixed-buffers) |
| **Compression and encryption** | wrapper code at every call site | `[Zstd, ChaCha20]` on a pack, on a field that holds a pack, a string or a `Stream`, or on a whole connection | [Transform chains](#transform-chains-compression-and-encryption-as-attributes) |
| **Stores and archives** | decrypt, decode, store, re-encode on the way out - so the store needs the schema and the keys | `[ToStream<FromCamera>] Snapshot snapshot;` - the store takes raw bytes it never parses; `[FromStream<…>]` sends them on, and the far end gets the `Snapshot` | [Trims](#trims-stores-that-keep-bytes-they-never-parse) |
| **Gateways and proxies** | a proxy that parses and re-serializes, holds the keys and is rebuilt on every change | `VirtuallyConnects<Operator, Fleet, Edge>` - a generated relay on `Edge` forwards the bytes without decoding them | [Virtual connections](#virtual-connections-and-relays-middle-tiers-that-cannot-read) |
| **Many services on one port** | a router in front, or a port per service | `Multiplex<(Telemetry, TelemetryV2, Console)>` - the first byte of a socket picks the connection | [Multiplexing](#multiplexing-one-port-many-peers-many-generations) |
| **Versions in the field** | a version field and `if (version >= 3)` in every handler | a new generation is a new host with its own connection; the old one stays frozen; both share one port | [Evolution](#evolution-a-new-generation-is-a-new-host) |
| **A cluster's own protocol** | a second, internal protocol for node-to-node traffic | `Connects<Fleet, Fleet>` - a host connected to itself, beside its clients | [Ready solution R16](#r16-an-internal-cluster-protocol-and-a-server-mesh-on-the-same-port) |
| **Your code after regeneration** | copy and paste into every new version; guessing which programs an edit breaks | injection points and smart merge keep your code, with backups; `AdHocAgent .\MyProtocol.cs~` names the hosts an edit reaches | [Deployment](#deployment-routes-smart-merge-backups) · [Impact report](#impact-report-which-hosts-an-edit-reaches) |

What stays yours is what only you can write: what a received pack means, what entering a state starts, what a timeout does, and your
own data structures. AdHoc can even serialize straight from those structures, with no copy into generated objects
([Hosts](#hosts-generated-objects-or-your-own-structures)).

A pack is not tied to the network, either. The same generated code that sends packs over a connection can write and read them in
a storage format of your own.

### Where AdHoc pays off

![The same declarations, three worlds: three columns of everyday problems, each tagged with the number of its ready solution. Devices and the field, C# or Java on the device side: every byte costs airtime (R13); no event lost on a dropout (R4); sure commands, fresh samples (R5); generations, no version field (R2); OTA updates of any size (R10); moves only when armed (R14); video that yields to alerts (R19); serial links that resync (R22). Backends and clusters, C# or Java servers: one public port, many services (R3); cluster protocol, same port (R16); store bytes it cannot read (R1); compress once, fan out (R11); audit without the payload (R17); each role sees its own (R12); ingest into your structures (R15); one pack, many recipients (R20); store and broadcast at once (R21). Apps and browsers, TypeScript, C# and Java clients: live dashboards, push both ways (R7); jobs with progress, cancel and deadline (R8); subscriptions with leases (R9); rooms, presence, turn-taking (R18); private hosts via a gateway (R6); orders survive a flaky network (R4); video, commands never wait (R19); one tick to every player (R20). Under the columns, an orange bar - one description, one C# file - feeds a yellow bar - generated code in C#, Java and TypeScript, one protocol, the same bytes - which feeds all three columns. A footnote: device-side hosts are C# or Java programs today - gateways, industrial PCs, vehicle computers, laptops; a TypeScript host always dials.](docs/img/readme-domains.svg)

The same few declarations solve problems on devices, in backends and in apps. Each problem below has a [ready solution](#ready-solutions)
with its declaration, its runtime behaviour and its limits:

- **Devices and the field.** [Telemetry in a handful of bytes](#r13-every-byte-costs-airtime-telemetry-in-a-handful-of-bytes) ·
  [no event lost on a cellular dropout](#r4-a-client-that-never-loses-an-order-or-an-event) ·
  [UDP that is sure where it matters](#r5-reliable-where-it-matters-fresh-where-it-does-not) ·
  [firmware generations without version fields](#r2-versioning-without-version-fields) ·
  [OTA updates of any size](#r10-files-media-and-ota-updates-of-any-size-resumable) ·
  [no command out of order](#r14-protocol-enforced-workflows-no-command-out-of-order). The device-side host is a C# or Java program
  today: a gateway, an industrial PC, a vehicle computer, a service laptop.
- **Backends and clusters.** [One public port](#r3-one-public-port-for-many-services) ·
  [a cluster mesh on the same port](#r16-an-internal-cluster-protocol-and-a-server-mesh-on-the-same-port) ·
  [object storage that keeps bytes it cannot read](#r1-object-storage-that-keeps-bytes-it-cannot-read) ·
  [a log broker that compresses once](#r11-a-log-broker-that-compresses-once-and-fans-out) ·
  [a payload-blind audit tier](#r17-a-metadata-routed-payload-blind-middle-tier-audit-and-tracing) ·
  [least-knowledge hosts](#r12-least-knowledge-hosts-every-role-sees-only-its-own-messages) ·
  [ingest into your own structures](#r15-ingest-straight-into-your-own-data-structures).
- **Apps and browsers.** [Live dashboards](#r7-live-dashboards-and-consoles-in-the-browser) ·
  [long-running jobs](#r8-long-running-jobs-with-progress-cancel-and-deadlines) ·
  [fan-out subscriptions](#r9-fan-out-subscriptions-with-leases) ·
  [chat and collaboration](#r18-chat-and-collaboration-rooms-presence-turn-taking) ·
  [a gateway that never needs a schema update](#r6-a-gateway-that-never-needs-a-schema-update).

AdHoc is built for data-heavy systems whose peers you deploy and can list. Typical fields are trading and market data, game servers,
telemetry and the IoT, industrial and scientific data acquisition, real-time analytics, streaming media, telecom, back-office systems
and services that talk to each other. AdHoc's own protocol, the link between AdHocAgent and the generator, is itself an AdHoc
description: [`AdhocProtocol.cs`](AdhocProtocol.cs). Where AdHoc is the wrong choice is said plainly in
[When AdHoc is not the right tool](#when-adhoc-is-not-the-right-tool).

## How AdHoc is organized

*Pipeline: **description → AdHocAgent → generator → code per host → deployment → runtime** · Model: **project → host → connection →
actor → state → branch → pack → field → bytes***

Three pictures explain AdHoc as a whole. The pipeline turns one file into code, the model is what that file is built from, and the
runtime is what the generated code runs on. After them comes a complete description, line by line.

### The pipeline: one file in, code for every host out

![The AdHoc pipeline as a U: your machine on the left, the AdHoc cloud on the right. The top row runs towards the server: 1, the description file MyProtocol.cs, plain C# in a .NET project, checked by your IDE; 2, AdHocAgent, which compiles it with Roslyn, checks the AdHoc rules, stops on the first error and writes the numbers tables back into the file; an arrow labeled "upload: description + UUID" crosses to the cloud with the note "nothing leaves your machine until the description passes". 3, the generator, a cloud service, makes code for every host in each of its languages - C#, Java, TypeScript - and tests it before returning it. The bottom row runs back: 4, the received folder, a folder per language and per host with gen, lib and demo; 5, deployment, driven by MyProtocol.md, routes the files to your folders, keeps your code through smart merge and makes backups with restore scripts; 6, your projects, each built from gen, lib and your code. A dashed arrow from your projects back to the description says: description changed? run steps 2-5 again. Below, at runtime, a Server in C# and a Client in TypeScript exchange packs as bytes over a connection. Colours mark the owner: orange yours, grey AdHocAgent, purple the AdHoc service, yellow generated.](docs/img/dev-01-pipeline.svg)

| Step | Where | What happens |
|:--|:--|:--|
| **1. Description** | your IDE | Plain C# in a .NET project, written with AdHoc's vocabulary from [`Meta.cs`](src/Meta.cs): `Host`, `Connects<L, R>`, branch and field attributes. Your IDE compiles and checks it as you type. |
| **2. AdHocAgent** | your machine | Compiles the description with Roslyn and checks it against every AdHoc rule. The first error stops the run, and nothing leaves your machine until the description passes. AdHocAgent also writes the **numbers tables** back into the file: the identities of packs, hosts, connections, actors and states. |
| **3. Generator** | the AdHoc cloud | AdHocAgent signs in with your personal UUID and uploads the description. The generator writes the code of every host in each of its languages and tests it before returning it, as far as its load allows. |
| **4. Received folder** | your machine | A folder per language (`InCS`, `InJAVA`, `InTS`) with a folder per host inside: `gen/` (the protocol code), `lib/` (the runtime library), `LICENSE-GRANT.md`. |
| **5. Deployment** | your projects | A Markdown instructions file routes the received files into your projects. **Smart merge** carries your code in **injection points** over into the new files. Everything a deployment overwrites or deletes is backed up first, with restore scripts. |
| **6. Runtime** | your programs | Each host is your program, built from the generated code, the runtime library and your code. |

Whenever the description changes, steps 2 to 5 run again. Before that, `AdHocAgent .\MyProtocol.cs~` tells you which hosts the change
reaches ([Impact report](#impact-report-which-hosts-an-edit-reaches)).

→ MANUAL: [The pipeline](MANUAL.md#the-pipeline-from-a-description-to-running-code) · [AdHocAgent](MANUAL.md#adhocagent) ·
[What AdHoc generates for each host](MANUAL.md#what-adhoc-generates-for-each-host) ·
[Deployment and smart merge](MANUAL.md#deployment-and-smart-merge)

### The model: nine layers, from the project down to the bytes

![The AdHoc model as a ladder of nine layers, from top to bottom, each with the C# construct that declares it: project - interface Telemetry, the whole system; host - struct Sensor : Host, one program and its languages; connection - interface Link : Connects<Sensor, Collector>, the link between two hosts; actor - interface Name : Actor, one conversation, many run at once; state - struct Idle { }, a step: who may send what now; branch - [L____________<Waiting, Sensor.Reading>], the packs one side may send and the next state; pack - class Reading, the unit that travels; field - [MinMax(-40, 125)] short celsius, a typed value known by its place; bytes - a strip of a wire id followed by fields. Brackets group the rungs into topology (who exists, who talks), conversation (what may be sent when) and data (what is sent). Two dashed purple arrows end at the pack rung: one from the branch rung, labeled "selects packs", and one from the host rung, labeled "declares packs (or the project does)". A note: a branch does not contain packs; it selects packs declared in the project or in a host, outside any connection.](docs/img/dev-01-model-layers.svg)

| | Layer | Declared as | What it is |
|:--|:--|:--|:--|
| **Topology** | Project | a top-level `interface` in a namespace | the whole system, and the protocol of all its parts |
| | Host | `struct Server : Host`, with its languages in the doc comment | one program of the system |
| | Connection | `interface Link : Connects<Client, Server>` | the link between two hosts, and everything said over it |
| **Conversation** | Actor | the states of a connection body, or `interface Job : Actor` | one independent conversation; many run at once on one connection |
| | State | an empty `struct` | one step of a conversation: which packs each side may send now |
| | Branch | an attribute on a state: `[L____________<Next, Packs>]`, `[____________r<Packs>]` | the packs one side may send there, and the state that sending leads to |
| **Data** | Pack | a `class` | the unit that is sent, received or stored |
| | Field | an instance field: `[MinMax(-40, 125)] short celsius;` | a typed value inside a pack |
| | Bytes | generated | the pack's wire id, then its fields |

The ladder is more than a nesting:

- **Packs are data.** You declare them in the project or in a host, outside any connection, and a branch only refers to them. A pack
  that some branch sends gets a **wire id**, the number that precedes it on the wire. A pack that no branch sends can still be the
  type of a field.
- **Fields travel by place, names never travel.** A field carries no tag: it is known by its position, so changing the order of
  fields of one kind changes the format. Identities live in the numbers tables, so renaming a pack or a host with the IDE changes
  nothing on the wire.

→ MANUAL: [The model](MANUAL.md#the-model-from-the-project-down-to-the-bytes) ·
[From C# constructs to AdHoc entities](MANUAL.md#from-c-constructs-to-adhoc-entities) ·
[What changes the wire and what does not](MANUAL.md#what-changes-the-wire-and-what-does-not) ·
[Glossary](MANUAL.md#glossary-of-adhoc-terms)

### The runtime: generated code on two buffers per connection

![One host at runtime, in three layers. At the top, your code calls send(pack) and owns your handlers. In the middle, the generated code: a sending queue of packs, a transmitter, a receiver that maps the id to a pack handler and consumes fields in place, and the connection with its actor in its current state, holding #region slots for your code. At the bottom, the runtime library: the transport - TCP, WebSocket or UDP - with one send buffer and one receive buffer of bufferSize bytes, and the socket. The transmit loop runs from the socket asking for bytes, through the transmitter writing the id and fields of the head pack into the send buffer, back to the socket; the receive loop runs from the socket through the receive buffer and the receiver to the actor and your handlers. To the peer host, one pack leaves as buffer-sized chunks: chunk 1, chunk 2, chunk 3 - never one byte array. A legend says each loop handles one event at a time, to completion, the two loops may run on different threads, and a handler that blocks stalls its loop. The caption: resident memory on this path is 2 buffers per connection plus small serializer and parser state, whatever the pack size; extra are concrete pack objects, the guaranteed-delivery journal and codec contexts.](docs/img/dev-01-runtime.svg)

- **Sending is pulled by the socket.** `send(pack)` only queues the pack. Whenever the socket can take bytes, the generated transmitter
  writes the pack's id and fields straight into the send buffer. The next fill resumes where the last one stopped, in the middle of a
  field if need be. A pack never exists as one byte array.
- **Receiving starts with the first bytes.** The receiver reads the pack id, picks that pack's generated handler and consumes the
  fields where they land in the receive buffer. A value cut by the end of the buffer waits in a small parser state.
- **Memory does not grow with the message.** A connection holds one send buffer and one receive buffer, sized by your code (1024 bytes
  by default in C#), plus small state. A 64-byte status and a gigabyte `Stream` pass through the same two buffers. Pack objects, the
  journal of guaranteed delivery and the contexts of compression and cipher stages are counted apart.
- **Two loops, one event at a time.** Each direction handles its events, packs and timeouts, one at a time, each to completion.
  Handlers of one direction never race and need no locks; state shared by the two directions is the one place to coordinate.

A host's program has four parts. The generated code is replaced by every generation, and the runtime library is copied as it is.
Your code in injection points is carried over by smart merge, and your code around the generated code stays yours
([What you write and what is generated](#what-you-write-and-what-is-generated)). How this plays against whole text bodies is in
[Read in place, constant memory](#read-in-place-constant-memory); timing and threading are in
[The runtime engine](#the-runtime-engine-bytes-pulled-through-two-buffers).

→ MANUAL: [The runtime model: one buffer per direction](MANUAL.md#the-runtime-model-one-buffer-per-direction) ·
[Generated code and your code at runtime](MANUAL.md#generated-code-and-your-code-at-runtime)

### What a description looks like

A complete description that passes every check of AdHocAgent: a shop with a client generated in TypeScript and C#, a server in Java,
a login, orders that survive a lost socket, and an RPC.

```csharp
using org.unirail.Meta;

namespace com.my.company
{
    public interface Shop                                  // the project: one system
    {
        /// <see cref="InTS"/>
        /// <see cref="InCS"/>
        struct Client : Host                               // a program, generated in TypeScript and in C#
        {
            public class Login { string token; long session; } // session: 0 at first, then the one from Welcome

            public class Order
            {
                long                  item;
                [MinMax(1, 100)] byte quantity;            // 100 values: 7 bits on the wire
                string                note;                // optional, as every string is
            }
        }

        /// <see cref="InJAVA"/>
        struct Server : Host                               // a program, generated in Java
        {
            public class Welcome  { long session; }        // the id to present after a lost socket
            public class Accepted { long item; }
        }

        class Ping { long stamp; }
        class Pong { long stamp; }

        [Resumable(30)]                                    // a lost client has 30 minutes to come back
        interface Link : Connects<Client, Server>          // the connection: Client is left, Server right
        {
            [L____________<Shopping, Client.Login>]        // Start: the client logs in; both sides move to Shopping
            struct Start { }

            [l____________<Client.Order>]                  // Shopping: orders, any number; the state stays
            [____________r<(Server.Welcome, Server.Accepted)>] // the server welcomes and confirms
            struct Shopping { }

            (L____________, Pong) ping(Ping req);          // RPC: the client asks, the server answers

            interface Orders : Resumable<Client, Client.Order> { } // orders arrive once and in order, across lost sockets
        }
    }
}
```

![What the Shop description becomes. On the left, the description, condensed - braces, doc comments and the numbers tables left out - as an orange code card titled "Shop.cs - the whole system", with seven numbered markers: 1 struct Client : Host, InTS and InCS; 2 struct Server : Host, InJAVA; 3 [MinMax(1, 100)] byte quantity in class Order; 4 [Resumable(30)] on interface Link : Connects<Client, Server>; 5 the branch attribute of the state Start, [L____________<Shopping, Client.Login>]; 6 the RPC line (L____________, Pong) ping(Ping req); 7 interface Orders : Resumable<Client, Client.Order>. On the right, what is generated and what happens at runtime. Top: three yellow host cards - Client in TypeScript, Client in C#, Server in Java - each with the packs, the Link connection, its state machine and the ping actor. Middle: one state machine, generated into both hosts: from Start, Login sent by the client leads to Shopping; in Shopping, a loop for Order from the client and a loop for Welcome and Accepted from the server keep the state; anything else is refused before it is sent. Beside it, ping: an actor per call, from a pool, going Call, Ping, Return, Pong, End; the reply reaches the caller. Below: an Order without a note on the wire as a strip - the id, item in 8 bytes, quantity in one byte with 7 bits used, one byte of null bits - 10 bytes after the id, with no names and no tags; an absent note costs 1 bit. Bottom: a timeline. On socket 1 the orders O1 and O2 go through and O3 is lost in flight; the session is parked for at most 30 minutes with its actors, states, your objects and the journal; on socket 2 the client sends Login with its session id, the server calls reclaim, the conversation is in Shopping again, and O3 is replayed once. The server's code sees O1, O2, O3 - once each, in order.](docs/img/readme-shop-walkthrough.svg)

- **Project and hosts.** `Shop` is the project. Its namespace becomes the root namespace, or package, of the generated code. `Client` and
  `Server` are hosts, the programs of the system. Their doc comments name their languages: the client is generated in TypeScript and
  C#, the server in Java.
- **Packs.** Every class here is a pack. Each is declared in the host that sends it, except `Ping` and `Pong`: declared in the
  project, they are common packs that any host may use. `[MinMax(1, 100)]` gives `quantity` 100 possible values, so it travels
  in 7 bits. Like every string, `note` may be absent, and then it costs one null bit. An `Order` without a note is 10 bytes after its pack id: 8 for `item`, one
  for the bits of `quantity` and one of null bits, with no field names or tags.
- **Connection and branches.** `Link` joins `Client`, the left host, and `Server`, the right one. In a branch name, `L` or `l` means
  the left host sends and `R` or `r` the right. A capital letter moves the conversation to the state named first; a small letter keeps
  the state.
- **States.** `Start`, the first declared state, is where the conversation begins. There only the client may send, and only `Login`,
  which moves both sides to `Shopping`. In `Shopping` the client sends orders, and the server sends `Welcome` and `Accepted`, as often
  as needed. Both hosts get this one state machine, and a pack the current state does not allow is refused before it is sent.
- **RPC.** `ping` is a request and its reply in one line. AdHocAgent turns it into an actor of its own, with the states `Call` and
  `Return`. Each call is an instance from a pool, so many calls can be in flight beside the shop conversation, and each `Pong` reaches
  the code that asked.
- **Session.** With `[Resumable(30)]`, a lost socket does not end the session. An identified session is parked for up to 30 minutes
  with its actors, its states and the objects your code attached. The client reconnects and logs in again, presenting the session id
  that `Welcome` gave it. The server's `Login` handler calls `reclaim`, and the conversation goes on in `Shopping`.
- **Guaranteed orders.** `Resumable<Client, Client.Order>` makes the client keep every `Order` in a journal until the server confirms
  it. After the reclaim, the orders lost in flight are sent again. The server's code sees each order once, in order.
- **Numbers tables.** The first run of AdHocAgent writes two tables above `interface Shop`. One numbers the packs and gives the six
  that branches send their wire ids; the other numbers the project, its hosts, its connection and its actors. The listing above leaves
  out both tables and the short marks the agent adds to the branch attributes.

`AdHocAgent .\Shop.cs` brings back three host folders, `InTS/Client`, `InCS/Client` and `InJAVA/Server`, each with `gen/`, `lib/` and
`LICENSE-GRANT.md`. Every part of this file has a section under [Capabilities](#capabilities). The first run, step by step, is in
[Getting started](#getting-started).

→ MANUAL: [The description file](MANUAL.md#the-description-file) · [Packs](MANUAL.md#packs) ·
[Narrowing a field with MinMax](MANUAL.md#narrowing-a-primitive-field-with-minmax) ·
[Branches and the routing attributes](MANUAL.md#branches-and-the-routing-attributes) · [RPC methods](MANUAL.md#rpc-methods) ·
[Identity and reclaim](MANUAL.md#identity-and-reclaim) · [Guaranteed delivery](MANUAL.md#guaranteed-delivery) ·
[Numbers tables](MANUAL.md#numbers-tables)

## Why a binary protocol, not HTTP

*Two paradigms compared: request/response with text bodies, and AdHoc's binary packs on long-lived connections. Every AdHoc number
below is worked out from the rules in [MANUAL.md](MANUAL.md).*

Request/response with a text body is easy to read and quick to start with. But every message pays for it: field names travel in every
message, a parser touches every byte several times, the body is held whole in memory, the server may only answer, and a dropped
connection leaves requests in doubt. AdHoc goes the other way. Packs are binary, and both ends already know their layout. A connection
carries whole conversations. And the code that makes this as easy as the text way is generated for you.

| | Request/response, text body | AdHoc |
|:--|:--|:--|
| A field on the wire | its name, then its value spelled out as text | its value only, in the bits its declared range needs |
| Reading a message | buffer the body, tokenize, match names, convert digits, build objects | each field is consumed from the receive buffer as it lands |
| Memory for a message | grows with the message | two fixed buffers per connection, whatever the size |
| Who may speak | the client asks, the server answers | either side, in several conversations at once |
| Rules of the exchange | checked again by hand in every handler | one generated state machine, the same on both ends |
| A dropped connection | requests of unknown fate, retries, duplicates | the session is parked and goes on; packs you choose arrive once, in order |
| Large or endless data | a whole body; binary content re-encoded as text | a `Stream` field in chunks, through the same two buffers |
| Routing and versions | host, path and version on every request | on a shared port, one byte per socket; none on a port of its own |

### Fewer bytes on the wire

A pack on the wire is its id followed by its fields, back to back. There are no names, no tags and no length: a field is known by its
position, and its declared range sets its width. Take MANUAL's worked example, the `Reading` pack:

```csharp
class Reading
{
    string?                 label;
    ushort                  sensor;
    [MinMax(-11, 75)] short celsius;   // 87 values: 7 bits
    bool                    alarm;     // 1 bit
    [X(2_000, 20_000)] int  pressure;  // a varint measured from 20 000
    uint?                   sequence;  // absent: one null bit
    [D(16)] ushort[,]       samples;   // at most 16 items: a 1-byte length
}
```

Set `label = "ok"`, `sensor = 513`, `celsius = 20`, `alarm = true`, `pressure = 20 001`, `samples = [1, 2]` and leave `sequence` out.
The pack is then **15 bytes**, with a one-byte pack id. The same data as a text body,
`{"label":"ok","sensor":513,"celsius":20,"alarm":true,"pressure":20001,"samples":[1,2]}`, is **86 bytes**, and 56 of them spell out
the field names with their quotes and colons. Send it as a request and you add a minimal request line and headers (`POST /r HTTP/1.1`,
`Host: h`, `Content-Length: 86`: 49 bytes) and a minimal reply, which every request gets (`HTTP/1.1 204 No Content`: 27 bytes):
**162 bytes**, more than ten times as many.

![One Reading sent two ways, drawn to scale. Top: a request/response exchange of 162 bytes - a minimal 49-byte request line and headers, an 86-byte text body in which 56 bytes are the field names with their quotes and colons, and a minimal 27-byte reply. Bottom: the same Reading as an AdHoc pack of 15 bytes, enlarged byte by byte: the pack id, sensor in 2 bytes, celsius and alarm filling one bit-field byte, a second bit-field byte holding the length bit of pressure, the one payload byte of pressure, a byte of null bits, the label "ok" in 3 bytes and samples in 5 bytes. AdHoc sends no reply unless the protocol declares one.](docs/img/readme-bytes-vs-text.svg)

The rules behind those 15 bytes:

- **The range of a primitive field sets its width, not its type.** `[MinMax]` stores `value - min`. A span below 128 becomes a bit
  field of a few bits, packed together with the pack's other bit fields. A span up to 255 takes one byte. A wider span takes exactly as
  many bytes as it needs: 80 000 values take 3 bytes.
- **A varint is measured from where its values cluster.** `[A]`, `[V]` and `[X]` name a floor, a ceiling or a center, and only the
  distance from that point travels. `pressure = 20 001` costs one payload byte and a length bit; the text `20001` costs five bytes.
- **Values keep their natural width.** A `DateTime` is 6 bytes; the text `2026-10-04T20:57:49.014Z` is 24. A `DateTimeDef` covering 50
  years at one-second precision is 4 bytes. A SHA-256 in `[D(32)] Binary[]` is exactly 32 bytes; as hex text it is 64. An absent value
  costs one null bit, or a spare code inside a bit field.
- **A small pack becomes one integer.** A pack whose fields fit in 64 bits is a Value Pack. It is generated as a single primitive with no
  object behind it (in TypeScript while it fits 53 bits), and it writes the same bytes in C#, Java and TypeScript.

→ MANUAL: [The layout of a pack on the wire](MANUAL.md#the-layout-of-a-pack-on-the-wire) ·
[Narrowing a field with MinMax](MANUAL.md#narrowing-a-primitive-field-with-minmax) ·
[Varint](MANUAL.md#varint-declaring-where-values-cluster) · [Time types](MANUAL.md#time-types) · [Binary](MANUAL.md#binary) ·
[Absent values](MANUAL.md#absent-values-on-the-wire) · [Value Packs](MANUAL.md#value-packs)

### Read in place, constant memory

A text body is usually read in passes. The receiver waits for the whole body, tokenizes it, matches the keys by name, converts the digits,
unescapes the strings, builds a generic tree and copies it into typed objects. Every pass touches every byte again, and most of them
allocate memory.

An AdHoc receiver makes one pass. It reads the pack id and picks the generated handler, and the handler takes each field straight from the
receive buffer, at a place fixed by the description. A value cut off by the end of the buffer waits in a small parser state, so decoding
never waits for the whole pack. A string travels as its UTF-16 code units, each a small varint, so it is never converted to UTF-8 and
back on the way.

Sending is the mirror image. `send` only queues the pack. The transmitter writes the pack into the send buffer whenever the socket can
take bytes, and if the buffer fills up, it continues later from the middle of a field. No byte array for the whole serialized pack is
ever made, before sending or after receiving: both directions work in portions of the socket buffer, which may be as small as
**128 bytes** - a 1 GB pack goes through a 128-byte buffer just as a 10-byte one does. With an abstract pack there is not even a
generated object: the generated code reads each field from your own structure when it sends, and writes it there when it receives.

![Two receive paths, left to right. The text path: socket, the whole body buffered, tokenizer, key matching by name, digit conversion and unescaping, a generic tree, a typed object, validation - with a red dot on every stage that allocates or copies the bytes again. The AdHoc path: socket, one reused receive buffer, the pack id choosing the generated handler, each field consumed at its declared place, then your object or your own setters - a value cut off by the buffer end waits in a small parser state.](docs/img/readme-parse-paths.svg)

So memory follows the connection, not the message. A connection has one send buffer and one receive buffer, of a size your code chooses
(1024 bytes by default in C#). The runtime never allocates a buffer the size of a pack, in either direction. A 1 GB `Stream` field costs
the same two buffers as the 15-byte `Reading`. 10 000 connections with 1 KB buffers hold about 20 MB on the serialization path. A pack can
even be larger than the memory of either host, if its bulk travels in `Stream` fields or in an abstract pack that your code fills piece by
piece. And when a sink cannot take more, the transport stops reading the socket: unread data waits in the network, not in memory.

![Memory held for one message against message size, both on log scales from 1 KB to 1 GB. A whole text body is a rising diagonal: memory grows with the message. AdHoc is a flat line at two buffers per connection. A side panel: 10 000 connections with 1 KB buffers hold about 20 MB; a 1 GB Stream field uses the same two buffers; a full sink stops the reading, so the data waits in the network. Pack objects, delivery journals and codec contexts are counted apart.](docs/img/readme-constant-memory.svg)

Some memory lies outside that path, and MANUAL lists it: the objects of concrete packs, the journals of guaranteed delivery, and
the contexts of compression and cipher stages.

→ MANUAL: [The runtime model: one buffer per direction](MANUAL.md#the-runtime-model-one-buffer-per-direction) ·
[Receiving](MANUAL.md#receiving-bytes-are-consumed-where-they-land) · [Sending](MANUAL.md#sending-the-transport-pulls-the-bytes) ·
[Memory on the serialization path](MANUAL.md#memory-on-the-serialization-path) ·
[Strings on the wire](MANUAL.md#strings-and-lengths-on-the-wire) ·
[Abstract packs](MANUAL.md#abstract---your-code-holds-the-data)

### Both sides speak, many conversations at once

In request/response only the client speaks first. Server events need polling, requests held open, or a second channel with its own
protocol and login. Parallel operations become parallel requests, which your code then matches up by hand.

On an AdHoc connection, each branch names the side that sends: `l`/`L` is the left host, `r`/`R` the right one, `_____lr_____` either of
them. A function-group actor gives each side one-way functions that are available at any time, with no actor instance to create or
track:

```csharp
interface Notify : Actor
{
    [____________r<(Note, Alert)>]   // the server pushes, whenever it likes
    struct Messages { }

    [l____________<Flag>]            // the device sends, whenever it likes
    struct Flags { }
}
```

Several actors on one connection are independent conversations: a file transfer can run in one actor while a chat runs in another, on
the same socket. Request/response is still there where it fits. `(L____________, Pong) ping(Ping req);` is an RPC method, and with a warm
pool neither side allocates an actor object per call. The server may be the side that asks.

![Two timelines. Left, request/response: the client polls the server again and again; a poll finds nothing new, then an event happens on the server and waits, marked with an hourglass, until the next poll brings it back - the alternative is a second channel with its own protocol and login. Right, one AdHoc connection between a device and a server: packs flow both ways, interleaved and colored by conversation - a login and a Reading in Actor0, Note and Alert pushed by the server and a Flag sent by the device in the Notify function group, Job, Progress and Done of a stateful job, and a ping call with its Pong reply - on one socket with one login and no polling.](docs/img/readme-push-vs-request.svg)

→ MANUAL: [Branches and the routing attributes](MANUAL.md#branches-and-the-routing-attributes) ·
[Function-group actor](MANUAL.md#function-group-actor) ·
[Actors: independent conversations](MANUAL.md#actors-independent-conversations-on-one-connection) ·
[RPC methods](MANUAL.md#rpc-methods) · [Four kinds of actor](MANUAL.md#four-kinds-of-actor)

### A conversation with rules, not stateless calls

A stateless call carries its own context, so every handler checks again by hand. Is this client logged in? Is this step allowed now? Is
this a late copy of an earlier step?

In AdHoc the rules are part of the description. States are empty structs, and branches say which packs each side may send and which state
that leads to. Both hosts get the same generated state machine, and both move on the same pack with no extra message:

```csharp
interface Work : Actor
{
    int Actor.MaxActiveInstances => 4;   // up to four jobs at once on one connection

    [____________R<Running, Job>]        // the server starts a job
    struct Start { }

    [l____________<Progress>]            // the device reports progress, any number of times
    [L____________<End, Done>]           // ... and ends the job
    struct Running { }
}
```

Before a pack is sent, the current state checks it. A pack the state does not allow is refused as `DISCARDED_BYSTATE`, and an
unexpected id on the receiving side goes to the receiver's error handler. Only the side that leads a state can move it on. In an actor
with instances, a 4-bit epoch on every pack drops late packs from an earlier state. A handler is given the transmitter of the next
state, so it can answer only with packs that state allows. `[Deadline]` and `[Idle]` end a stalled instance without touching the
connection.

![Left, stateless calls: three requests - start, progress, done - each running the same hand-written steps in orange around one grey act: load the session, check the login, check the phase, act, save. Right, one AdHoc actor: the device and the server each hold a replica of the same generated state machine, Start, Running, End; both move from Start to Running on the one Job pack, stay in Running on Progress, and move to End, which releases the pair, on Done. Below, two red crosses: a Done sent while the actor is still in Start is refused before it leaves, as DISCARDED_BYSTATE; a late pack from an earlier state carries an old epoch and is dropped by the receiver.](docs/img/readme-conversation-vs-calls.svg)

→ MANUAL: [States](MANUAL.md#states-the-phases-of-a-conversation) ·
[The side that leads a state](MANUAL.md#the-side-that-leads-a-state) ·
[What the generator produces for actors and states](MANUAL.md#what-the-generator-produces-for-actors-and-states) ·
[Deadline and Idle](MANUAL.md#per-instance-timers-deadline-and-idle)

### Sessions and delivery that survive a lost socket

When a request/response connection drops, some requests are left in doubt: were they received, were they applied? Every retry risks a
duplicate, so services add idempotency keys, deduplication tables and a fresh login on every reconnect.

`[Resumable(minutes)]` separates a session from its socket. If the socket of an identified peer is lost rather than closed by your
code, the session is parked: its actors, their states and everything your code attached to them stay, with no socket and no
buffers. The peer comes back, logs in as usual, reclaims the session and goes on in the state it was in. `Resumable<HOST, PACKS>` adds
guaranteed delivery for the packs you name: they arrive once, in order, even across a lost socket. On a `[UDP(minutes)]` connection
every pack is guaranteed, both ways, and you list only the packs that may be lost.

```csharp
[Resumable(30)]                                                     // a lost session waits 30 minutes for its peer
interface Link : Connects<Device, Server>
{
    // ... states and actors ...
    interface FromDevice : Resumable<Device, (Reading, Done)> { }   // these arrive once, in order
}
```

![A session above two sockets. Socket 1 is lost while packs 4 and 5 are in flight; the session, with its actor in state Working, is parked for up to the declared minutes; on socket 2 the peer logs in and reclaims it, the actor goes on in the same state, and packs 4 and 5 are replayed from the sender's journal before 6 and 7 - your code sees each pack once, in order.](docs/img/tldr-session.svg)

The guarantee has a stated price. The sender copies each guaranteed pack into a journal: the newest bytes stay in memory, the rest go to 4 MB
files, up to `resumable_megabytes` (100 MB by default). The receiver confirms with one control record for every 4 KiB of guaranteed packs:
the pack id with all bits set plus 8 bytes, so 9 bytes with a one-byte id, about 0.2 %. The pack itself carries nothing for the
guarantee. In request/response, the same 4 KiB of 15-byte readings would be 273 requests, each waiting for its own reply.

![The cost of confirming 4 KiB of guaranteed 15-byte packs, as two bars on one scale of 1 px per 10 bytes. Request/response: 273 replies of at least 27 bytes each, 7 371 bytes in all. AdHoc: one REPORT control record of 9 bytes, about 0.2 percent of the data. The packs themselves carry no bytes for the guarantee.](docs/img/readme-ack-cost.svg)

→ MANUAL: [What a session is](MANUAL.md#what-a-session-is) · [Parking a session](MANUAL.md#parking-a-session) ·
[Identity and reclaim](MANUAL.md#identity-and-reclaim) · [Guaranteed delivery](MANUAL.md#guaranteed-delivery) ·
[What the guarantee costs](MANUAL.md#what-the-guarantee-costs) · [UDP connections](MANUAL.md#udp-connections)

### Streams instead of whole bodies

A request/response body is one whole. Binary content inside a text body has to be re-encoded, and base64 makes 1 GiB about 1.33 GiB. A
live feed with no end does not fit into a request at all.

A `Stream` field travels as chunks: `[len][data] … [0x0000]`. No total is needed up front, and the stream may end whenever its source
ends. Its end can be seen from the framing alone, so a relay can forward it and a store can keep it without decoding anything. The
runtime reads your source straight into the socket buffer and writes into your sink. The framing costs 2 bytes per chunk, about 0.2 %
with 1 KB buffers. `[S(N)]` caps the stream, and the cap is enforced on both sides.

```csharp
class Upload
{
    [D(+255)]          string name;
    [S(1_000_000_000)] Stream bytes;   // up to 1 GB, through the same two socket buffers
}
```

![Left: a 1 GiB file as a text body - the bar grows by a third when base64 re-encodes it, and the whole body sits in one memory box, or spills into a temporary file. Right: the same file as an AdHoc Stream field - your source feeds one fixed-size socket buffer, and a train of chunks, each a 2-byte length and about one buffer of data, goes on to your sink and ends with the 0x0000 terminator; a size-cap gate, S(N), stands before the sink, and more than N bytes fails, checked on both sides.](docs/img/readme-stream-vs-body.svg)

→ MANUAL: [Stream framing](MANUAL.md#stream-framing---chunked-and-interruptible) ·
[The size cap](MANUAL.md#the-size-cap-of-a-conduit---sn) ·
[Worked example: an archive of any size](MANUAL.md#worked-example---delivering-an-archive-of-any-size)

### The checks a server would write by hand

A text endpoint accepts any characters. Its handlers check the kind of message, whether fields are present and of the right type, string
and list lengths, the body size, whether the call is allowed now, how many operations one client runs at once, and timeouts. The client
repeats half of that.

In AdHoc the declarations become checks, and they run before your handler sees the pack:

| Check | Comes from |
|:--|:--|
| which message this is | the pack id; an id the receiver cannot dispatch drops the channel as out of step |
| fields present, of the right type | the layout itself: positions, types and null bits are fixed by the description |
| string and collection lengths | `[D(+N)]` and `[D(N)]`, 255 by default; the C# runtime refuses an oversized string before it allocates it |
| size of a raw stream | `[S(N)]`, on both sides |
| allowed now, not stale | the state guard (`DISCARDED_BYSTATE` when sending) and the epoch |
| concurrency | `MaxActiveInstances`: a peer that opens one instance too many is treated as hostile, and the connection is aborted |
| a stalled peer or step | `[ReceiveTimeout]`, `[TransmitTimeout]`, `[Deadline]`, `[Idle]` |
| value ranges | published as `MIN` / `MAX` constants; checking input before a setter is still your code |

![A simplified receiving pipeline of generated gates, left to right, between the arriving bytes and your handler: is the pack id known; is the pack allowed in the current state; is the actor instance within MaxActiveInstances and on the current epoch; are strings, collections and streams within their declared limits. Each gate has a red exit below it - channel dropped as out of step; reported to the error handler; connection aborted, or a stale pack dropped; refused as an overflow. A timer strip underneath: ReceiveTimeout, TransmitTimeout, Deadline, Idle. Your handler, at the end, gets a typed pack and the transmitter of the next state.](docs/img/readme-checks.svg)

→ MANUAL: [Pack-id dispatch](MANUAL.md#pack-id-dispatch-and-uniqueness) · [Strings](MANUAL.md#strings) ·
[Collection limits](MANUAL.md#collection-limits-_defaultmaxlengthof) ·
[Opening an actor instance and its limit](MANUAL.md#opening-an-actor-instance-and-its-limit) ·
[Connection timeouts](MANUAL.md#connection-timeouts-transmittimeout-and-receivetimeout) ·
[Range constants](MANUAL.md#range-constants-in-the-generated-code)

### Routing paid once per socket

Request/response routes every single request by host and path. Versions go into a path segment or a header, and a handler that serves
two versions branches on it.

An AdHoc multiplexer serves several connections of one host on one port. Every socket opens with one byte, the uid of the host that
dials. That byte is paid once per socket, never per pack, and from then on the socket is an ordinary link of its connection. A new
generation of a peer is a new host with its own connection. The old generation stays frozen, both are served behind one port, and no
handler ever asks which version it is talking to.

```csharp
interface Port : Multiplex<(Link, LinkV1)> { }   // Device and DeviceV1 dial the same port
```

![Top, request/response: a row of requests, each opening with the same grey routing block - method, path with a version segment, host - before its body. Bottom, AdHoc: a multiplexed port reads one uid byte per socket and hands the socket to its connection - Device.uid to Link, DeviceV1.uid to the frozen LinkV1 - after which the socket carries packs only.](docs/img/readme-route-once.svg)

→ MANUAL: [The first byte of a multiplexed socket](MANUAL.md#the-first-byte-of-a-multiplexed-socket) ·
[Several generations behind one port](MANUAL.md#several-kinds-of-peer-several-generations-behind-one-port) ·
[A new generation is a new host](MANUAL.md#a-new-generation-is-a-new-host)

### Middle tiers that carry what they cannot read

In request/response, a proxy in the middle usually ends the encryption and reads everything that passes. A store that keeps messages
parses them, or keeps them as text and parses them again on every read.

In AdHoc:

- **The transport is encrypted by one key.** A listening host given a static X25519 key encrypts every connection it accepts: a Noise NK
  handshake, then ChaCha20-Poly1305 on every record. Without a key, the transport costs nothing extra.
- **Compression and encryption of the content are declarations.** `[Zstd]`, `[ChaCha20]` and your own stages are attributes on a field,
  a pack or a connection. A pack that can never exceed 1024 bytes loses its compression stage by itself.
- **A relay forwards sealed bytes.** On a virtual connection the chain runs end to end, and the generated relay forwards the bytes with no
  schema, no key and constant memory.
- **A store keeps what it cannot read.** A trim lets a store keep a payload as raw bytes - even compressed and encrypted - while the far
  side still receives the typed object.
- **The costs are stated.** A per-pack cipher adds 12 clear bytes per pack, and `[ChaCha20]` alone gives confidentiality, not integrity.

→ MANUAL: [Keys of a cipher](MANUAL.md#keys-of-a-cipher) · [Built-in stages](MANUAL.md#built-in-stages---zstd-and-chacha20) ·
[What Zstd and ChaCha20 guarantee](MANUAL.md#what-zstd-and-chacha20-guarantee) ·
[Virtual connections and relays](MANUAL.md#virtual-connections-and-relays) · [Trims](MANUAL.md#trims)

### Where text and request/response still win

A binary, positional protocol has real costs:

- **The bytes do not describe themselves.** On the wire, data is identified only by a pack id, field positions and, on a multiplexed
  port, one uid byte. A captured packet cannot be read without the description.
- **A field cannot be added in place.** Adding, removing or reordering a field makes another pack, and both ends are regenerated and
  deployed together. Peers you cannot update stay on a frozen generation instead. That fits peers you can list, not an open ecosystem
  with any number of generations.
- **A varint needs a well-placed base.** A varint whose base is badly placed can cost more than a fixed-width field.

For public APIs used by clients you will never meet, for traffic that people inspect with generic tools, and for rare small calls, text
request/response is the simpler choice. More in [When AdHoc is not the right tool](#when-adhoc-is-not-the-right-tool).

→ MANUAL: [How packs and peers are told apart](MANUAL.md#how-packs-and-peers-are-told-apart-on-the-wire) ·
[The order of fields is part of the wire format](MANUAL.md#the-order-of-fields-is-part-of-the-wire-format) ·
[When separate generations fit](MANUAL.md#when-separate-generations-fit) · [When varint loses](MANUAL.md#when-varint-loses)

## Packs that cost almost nothing

*AdHoc's own ways to make a pack cheap: a message that is only its id, values that share bits, a pack that is a number, and a pack with no object at all*

A pack is usually an object: allocated, filled, serialized, parsed into another object on the other side. AdHoc removes that cost
where the data allows it - and the generator decides most of it for you.

| What | What it is | On the wire | In memory |
|:--|:--|:--|:--|
| [Empty pack](#empty-packs-a-message-that-is-only-its-id) | a pack without fields that a branch sends | only its pack id | no allocation per message |
| [Bit fields](#bit-fields-small-values-share-the-packs-bits) | every field whose codes fit in 7 bits: `bool`, `bool?`, small enums and ranges | 1 to 7 bits each, packed together | inside the pack, read as ordinary values |
| [Value Pack](#value-packs-a-pack-that-is-a-number) | a pack whose fields fit in 64 bits | its id and its bits, rounded up to whole bytes | one integer: no object, no pointer |
| [Pack without implementation](#packs-without-implementation-straight-into-your-own-structures) | an interface over your own objects | the same bytes as any pack | no generated object: bytes go straight into your structures |

### Empty packs: a message that is only its id

An event, a command, an acknowledgement, a state transition - "it happened" is often the whole message. An **empty pack** is a class
without fields that a branch sends:

```csharp
/// <see cref="InCS"/>
struct Server : Host
{
    public class Shipped { }        // an empty pack: the event itself
}

interface Link : Connects<Client, Server>
{
    [L____________<Waiting, Client.Order>]
    struct Start { }

    [____________R<Start, Server.Shipped>]      // the empty pack moves the conversation on
    struct Waiting { }
}
```

![Three panels. You declare public class Shipped { } with no fields, and the branch [____________R<Start, Server.Shipped>] on the state Waiting - a transition with no data. On the wire an ordinary pack is an id followed by its fields, while an empty pack is only the id: one byte while the connection's ids fit in it. The generated receiver is Shipped.Handler.ONE, the only instance, with nothing to read; the pack moves the state machine from Waiting to Start, with no allocation per message. Below: a million Shipped packs cost a million ids on the wire and no objects; as a field type an empty pack becomes a bool, present or absent; an empty class that no branch sends is a container for constants, not a pack.](docs/img/readme-empty-pack.svg)

- **Only the id travels.** The receiver reads every pack id of a connection in the same fixed number of bytes - one byte while the
  connection's largest id fits in it - and an empty pack adds nothing after it.
- **Nothing is allocated.** The receiving side is one static handler, the only instance there is, and it consumes no payload bytes.
  A million `Shipped` packs cost a million ids on the wire and no objects in memory.
- **It drives the state machine.** An empty pack in a branch is a transition with no data - `Waiting` → `Start` above - checked by the
  generated code on both ends like any other pack.
- **As a field type it becomes `bool`.** A field typed with an empty pack can only be present or absent, so the agent replaces it with
  `bool` (and warns). An empty class that no branch sends is not a pack at all: it becomes a container for constants and nested
  declarations.

→ MANUAL: [Empty packs](MANUAL.md#empty-packs) · [An empty pack as a field type](MANUAL.md#an-empty-pack-as-a-field-type)

### Bit fields: small values share the pack's bits

In **any** pack, a field whose values - together with "none", if it is optional - fit in 7 bits does not get a byte of its own. It goes
into the pack's **bit storage**: the bits of all such fields are packed together, lowest bits first, and rounded up to whole bytes once
for the whole pack.

```csharp
enum Gear { Park, Drive, Reverse }

public class Status
{
    long                    at;         // 8 bytes, fixed
    bool                    ignition;   // 1 bit
    bool?                   door_open;  // 2 bits: false, true, none
    Gear                    gear;       // 2 bits: 3 values
    Gear?                   wanted;     // 2 bits: 3 values + none
    [MinMax(0, 100)] byte   fuel_pct;   // 7 bits: 101 values
    [MinMax(-20, 20)] sbyte tilt;       // 6 bits: 41 values
    [D(+32)] string         driver;     // a string: not a bit field
}
```

![Top: the pack Status on the wire - its id, the 8 bytes of the long field at, 3 bytes of bit storage, a byte of null bits and the string driver. The bit storage is zoomed below as 24 bits in three bytes, bit 0 first and in declaration order: ignition, 1 bit (bool); door_open, 2 bits (bool?: false, true, none); gear, 2 bits (Gear); wanted, 2 bits (Gear?); fuel_pct, 7 bits (0 to 100), crossing into byte 1; tilt, 6 bits (-20 to 20), crossing into byte 2; and 4 unused bits. Each field holds its code, value minus min, and "none" is one more code inside the field. Bottom: six small fields at a byte each would take 6 bytes; in bit storage they take 3 bytes, 20 bits used.](docs/img/readme-bit-storage.svg)

- **What becomes a bit field.** `bool` (1 bit); `bool?` (2 bits: false, true, none); an enum whose codes - plus one for "none" when
  it is optional - number at most 128; a number whose `[MinMax]` span - plus one for "none" - stays below 128. The field takes the fewest bits that hold its largest code.
- **"None" costs a code, not a byte.** An optional bit field keeps its absence as one more code inside its own bits: `Gear?` with three
  values still takes 2 bits.
- **The pack decides the bytes, not the field.** Six small fields above take 20 bits - 3 bytes on the wire for all of them together.
- **Your code sees ordinary types.** The getters and setters take `bool`, `bool?`, `Gear` and `byte`; the bits stay inside the generated
  code and on the wire.

→ MANUAL: [Bit fields](MANUAL.md#bit-fields-small-values-share-the-packs-bits) ·
[Narrowing a primitive field with MinMax](MANUAL.md#narrowing-a-primitive-field-with-minmax) ·
[Absent values on the wire](MANUAL.md#absent-values-on-the-wire) ·
[The layout of a pack on the wire](MANUAL.md#the-layout-of-a-pack-on-the-wire)

### Value Packs: a pack that is a number

A pack whose fields fit together in 64 bits is a **Value Pack**. The generator collapses it into a single integer - there is no object,
no heap allocation and no indirection, in C# and Java always and in TypeScript while it fits 53 bits. There is no marker to write: the
generator recognizes it.

```csharp
class Position                      // 48 bits: a Value Pack
{
    float x;
    byte  layer;
    byte  flags;
}

class Status                        // 6 bits: a Value Pack
{
    [MinMax(0, 7)] byte priority;   // 3 bits
    bool                is_leader;  // 1 bit
    [MinMax(0, 3)] byte mode;       // 2 bits
}

public class Order
{
    ulong              id;
    Status             status;      // 6 bits inside the order
    Position           at;
    [D(16)] Position[] route;       // up to 16 Value Packs: 16 numbers, no objects
}
```

![Left: an ordinary pack Point3 with three floats, 96 bits - an array of them holds a reference per item, each pointing to a separate object with a header and the fields x, y and z: allocation, a header and a pointer to chase per item. Right: the Value Pack Position with a float x, a byte layer and a byte flags, 48 bits - an array of them is an array of 64-bit numbers, x, layer and flags in fixed slots with 16 bits unused: one integer per item, no object, no pointer, copied by value. Bottom: the same Value Pack in three languages, with the same bytes on the wire - the pack id and 6 bytes: in C# a partial struct Position over a ulong Value with a property per field; in Java @Position long pos, no object ever, with static get, set and New; in TypeScript type Position = number, a class only above 53 bits.](docs/img/readme-value-pack.svg)

- **What counts.** `bool` is 1 bit; integers, `float` and `double` take their width, or fewer with `[MinMax]`; `char` 16 bits; an enum
  the width of its code; `DateTime` 64 bits; another Value Pack its own width. A pack brought under 65 bits only by its `[MinMax]`
  ranges is a Value Pack as well. Strings, collections, `Binary` and streams never are.
- **Fixed slots.** Each field holds its code in a fixed slot of bits, the widest first. Sent on its own, a Value Pack is its id and its
  bits rounded up to whole bytes: AdHoc's own 23-bit `Progress` pack travels as an id and 3 bytes.
- **The same pack in three languages, the same bytes.** C#: a `partial struct` over one unsigned integer, with a property per field.
  Java: no object is ever created - the pack is an annotation on a primitive, `@Position long`, with static accessors. TypeScript: the
  pack *is* a `number` (a class only above 53 bits). Java and TypeScript are written the same way:

  ```java
  @Position long pos = Position.New.of( 1.5f, (char) 3, (char) 4 );   // fields in declaration order
  pos = Position.set.x( pos, 2.5f );                                   // a value: the setter returns the new pack
  float x = Position.get.x( pos );
  ```

- **Everywhere a type goes.** A Value Pack can be a field, an array item, a Map key or value and a Set item: an array of them is an
  array of numbers. A nullable Value Pack gets an out-of-range `NULL` code.
- **Smart flattening.** A pack of a single field that is a Value Pack disappears into the field that uses it:
  `class Temperature { float celsius; }` used as `Temperature measurement;` is generated as `float measurement` - nullability survives.

→ MANUAL: [Value Packs](MANUAL.md#value-packs) · [How a Value Pack is laid out](MANUAL.md#how-a-value-pack-is-laid-out) ·
[Smart flattening](MANUAL.md#smart-flattening) ·
[The generated Value Pack in C#, Java and TypeScript](MANUAL.md#the-generated-value-pack-in-c-java-and-typescript) ·
[Working with a Value Pack in Java and TypeScript](MANUAL.md#working-with-a-value-pack-in-java-and-typescript)

### Packs without implementation: straight into your own structures

Every pack can be ordered **with** an implementation or **without** one - per host, per language, per pack, even per field. With an
implementation (`+`, the default) the generator emits a class that holds the data. Without one (`-`) it emits only an interface, and
your own objects implement it:

- receiving, the parser calls a **setter** of yours for each field as it decodes it from the socket buffer;
- sending, the serializer calls a **getter** of yours for each field as it writes it into the socket buffer.

No generated object is allocated and no copy of the pack exists on either side: the data goes from the socket buffer straight into
your structures - your records, columns, ring buffers, collections - and from them straight back into the socket buffer.

![Two rows, each showing both directions of one pack. Top row, concrete (+): the receive buffer feeds the generated parser, which fills a generated pack object that your code then reads; for sending, your code fills a generated pack object and the serializer copies it into the send buffer. Bottom row, abstract (-): the parser calls setters on your own object field by field as the bytes arrive; for sending, the serializer calls getters on your object field by field; no generated pack object exists in either direction.](docs/img/dev-10-impl-modes.svg)

```csharp
/// <see cref='InJAVA'/>--          // Java: every pack of this host without implementation
struct Broker : Host { }
```

```java
Broker._Allocator.DEFAULT.new_Producer_Append = receiver -> myLog.nextSlot();   // an incoming Producer.Append lands in your object
```

- **Arrays arrive in windows.** An array of one-byte elements (`Binary`, `byte`, a one-byte enum) is handed to your code as windows of
  the receive buffer itself, so your code copies a whole block into its ring buffer, column or file at once; arrays of wider elements
  that travel as they are stored come in windows of whole elements, and bit-packed items in whole groups of eight.
- **One model, not two.** A database node, a broker or a client library keeps its own structures and gets a network layer without a
  second model to keep in sync, and without one conversion per message in each direction.
- **Per host, not per protocol.** The same pack can be abstract on the host that binds it to an existing system and concrete on a new
  client: the bytes on the wire are the same either way.

→ MANUAL: [Implementation modifiers](MANUAL.md#implementation-modifiers-who-holds-the-data) ·
[Choosing + or -](MANUAL.md#choosing--or---generated-objects-or-your-own-structures) ·
[Implementation management](MANUAL.md#implementation-management-host-pack-and-field-rules) ·
recipe [R15. Ingest straight into your own data structures](#r15-ingest-straight-into-your-own-data-structures)

## Virtual connections: through hosts that cannot read

*Two hosts talk as if wired together, while their bytes ride through relays that neither parse them nor hold their keys*

A browser reaches a server only through a gateway; a technician reaches a vehicle only through the backend; a device behind NAT is
reachable only through a broker. Usually the middle host decodes every message and encodes it again - so it needs every schema, every
key, memory for the largest message, and a rebuild with every protocol change. In AdHoc you declare the two ends and the hosts in
between, and the middle hosts get a generated **Relay** that forwards the bytes without reading them:

```csharp
interface ViewerToEdge : Connects<Viewer, Edge> { [_____lr_____<Beat>] struct One { } }   // physical legs
interface EdgeToHub    : Connects<Edge, Hub>    { [_____lr_____<Beat>] struct One { } }
interface HubToServer  : Connects<Hub, Server>  { [_____lr_____<Beat>] struct One { } }

[Zstd, ChaCha20]                                                        // end to end: the relays carry ciphertext
interface ViewerToServer : VirtuallyConnects<Viewer, Server, (Edge, Hub)>
{
    [L____________<Live, WatchSub>]
    struct Start { }

    [____________r<WatchEvent>]
    [L____________<Start, WatchUnsub>]
    struct Live { }
}
```

The agent finds the route itself and says so: `The virtual connection "ViewerToServer" is routed "Viewer → Edge → Hub → Server" over
"ViewerToEdge + EdgeToHub + HubToServer"`.

![Four hosts in a row: Viewer, a browser in TypeScript; Edge, a public gateway in Java; Hub, an internal router in Java; Server, private, in Java. Physical legs: WebSocket from Viewer to Edge, TCP from Edge to Hub and from Hub to Server. Edge and Hub each run a generated Relay. A dashed purple arc above joins Viewer and Server: ViewerToServer, one connection end to end, with the same actors, state machines, RPC and sessions as on a direct link; the two endpoints hold the actors, states and packs and run the Zstd and ChaCha20 chain. Below the legs, every physical leg carries sealed chunks of many tunnels, each led by a tunnel id - one byte for MaxTunnels 256 - and followed by bytes the relay never decodes. Three facts: the relay holds no schema and no keys, one region of memory for a payload of any size; a protocol change rebuilds the two endpoints while the relays stay as they are; a chain on the connection runs at its two ends only - ChaCha20 conceals, a MAC adds integrity.](docs/img/readme-vc-overview.svg)

- **A connection like any other, end to end.** `Viewer` and `Server` get the same packs, actors, state machines, RPC, deadlines,
  groups and guaranteed delivery as on a direct connection. The middle hosts take no part in the conversation.
- **Relays that cannot read.** Each host on the `PATH` gets a generated `Relay` that moves the tunnel's chunks from one connection to
  the other without decoding them: no schema of the packs inside, no keys, no message-sized buffer - a payload of any size crosses at
  constant memory, and a slow far end slows the near end instead of filling the middle.
- **A protocol change rebuilds the endpoints only.** The relays know nothing of the packs that ride through them, so they are not
  regenerated or redeployed when those packs change.
- **End-to-end compression and encryption.** A chain on the virtual connection runs at its two ends: `[Zstd, ChaCha20]` above means the
  relays carry compressed ciphertext. `ChaCha20` conceals but does not authenticate - add a MAC stage of your own for integrity.
- **Many tunnels on one carrier.** The bytes of one virtual connection between its two hosts form a **tunnel**; many tunnels share the
  physical legs. `MaxTunnels` sets how many may be open at once and with it the width of the tunnel id on the wire: 1 - no id at all,
  256 (the default) - one byte, 65 536 - two bytes.
- **Several hops, any order.** `PATH` is one host or a tuple of them; the agent rebuilds the one physical chain from `L` to `R` through
  them and stops the run if the chain does not exist, is ambiguous or visits a host twice.
- **Or just a pipe.** A `VirtuallyConnects` with an empty body is a raw byte tunnel for a protocol of your own.

Where it pays off: [R6. A gateway that never needs a schema update](#r6-a-gateway-that-never-needs-a-schema-update) ·
[R3. One public port for many services](#r3-one-public-port-for-many-services) ·
[R17. A payload-blind middle tier](#r17-a-metadata-routed-payload-blind-middle-tier-audit-and-tracing).

→ MANUAL: [Virtual connections and relays](MANUAL.md#virtual-connections-and-relays) ·
[Declaring a virtual connection](MANUAL.md#declaring-a-virtual-connection) ·
[How the agent finds the route](MANUAL.md#how-the-agent-finds-the-route) ·
[MaxTunnels and MaxStream_KiloBytes](MANUAL.md#maxtunnels-and-maxstream_kilobytes) ·
[Relay: the runtime primitive on a relay host](MANUAL.md#relay-the-runtime-primitive-on-a-relay-host) ·
[Transform chains over a tunnel](MANUAL.md#transform-chains-over-a-tunnel)

## Why generate code from a description

*AdHoc, too, generates code from a description written in a domain-specific language (DSL). The well-known benefits of that approach
apply here - and because an AdHoc description covers the whole system, not only its messages, they reach further.*

Written by hand, a protocol is written many times over. There is an encoder and a decoder for every message in every language, a
dispatch switch for every connection, a state machine on each side, plus reconnection and retransmission - and all of it must be kept in
step. With a description, you declare all of this once and a generator writes the code. Four benefits are classic for code generated from
a DSL description. Here is what each one means in AdHoc:

| Classic benefit | In AdHoc |
|:--|:--|
| Less hand-written code | No serializers, parsers, dispatch, state machines or session handling written by hand. Your code goes into marked injection points. |
| Fewer bugs | The description is compiled and checked against every AdHoc rule before anything leaves your machine, and both ends of a connection are generated from the same declaration. |
| The same implementation everywhere | One description gives code for every host, in each of its languages (C#, Java and TypeScript today), and they all write the same bytes. |
| One place to change | You edit the description, see which hosts the edit reaches, regenerate and deploy. Your own code is carried over. |

![One description, from left to right. An orange card, MyProtocol.cs - one C# file declaring packs and fields, hosts and languages, connections, actors, states and branches, sessions and guaranteed packs, streams, chains and trims, virtual connections and ports. A grey AdHocAgent box: compiled by Roslyn and checked against every AdHoc rule on your machine, with the numbers tables written into the file; the first error stops the run and nothing is uploaded. A purple generator box in the cloud: code for every host in each of its languages, tested before it is returned. Three yellow host cards - Device in C#, Server in Java, Browser in TypeScript - each with the same Reading pack, the same MIN, MAX and STR_LEN_MAX constants, the same state machine and the identical 15-byte strip. A dashed path at the bottom leads from the description back to the hosts for every later change: edit MyProtocol.cs, AdHocAgent .cs~ names the hosts the edit reaches, regenerate, deploy with smart merge keeping your code.](docs/img/readme-one-description.svg)

### What you write and what is generated

The generated code holds the packs with their serializers and parsers, the connection classes with their transmitter and receiver, the
actors with their states and the dispatch of packs, and the constants. The runtime library holds the transports (TCP, WebSocket, UDP),
the buffers, the collections, compression and ciphers. Your code lives in two places. Inside generated files, in injection points, it
says what a received pack means, what entering a state starts and what a timeout does. Around the generated code, it creates transports,
connects, sends packs and owns your data structures. An injection point from the generated C# code of AdHoc's own protocol:

```csharp
case Agent.AdHocProtocol.Server_.Info.__id_:
    actor.OnNewState(conn, CLOSE_.ONE);     // generated: the state machine has moved on
#region> Info OnReceived Event
    // your code: what receiving Info means here
#endregion> ĀĄÿĀĄ.OnReceivedEvent
```

You may not even need generated objects. For each host, language, pack and field you choose concrete or abstract. A concrete pack is a
class that holds the data. An abstract pack is an interface that your own structures implement, so the generated code reads from and
writes into your objects directly. The bytes on the wire are the same either way.

![One host program as a stacked block. A thin orange slice on top: your code around the generated code - creating transports, connecting, sending, your data structures. A large yellow block, the generated code: packs with serializers and parsers, the connection with transmitter and receiver, actors with states and dispatch, state guards, epochs and timeouts, sessions and guaranteed delivery, constants such as MIN, MAX and STR_LEN_MAX; small orange notches on three of these parts are the injection points, where your code says what a pack means, what a state starts and what a timeout does. A grey block at the bottom: the runtime library with TCP, WebSocket and UDP transports, buffers, collections, Zstd and ChaCha20. To the right, in orange outlines, what is otherwise written by hand: an encoder and a decoder per message and language, a dispatch switch per connection, a state machine on each side kept in step, reconnect, replay and acknowledgments, length and size checks, constants copied into every host - and a yellow arrow from that list into the generated block: all of this becomes declarations.](docs/img/readme-who-writes-what.svg)

→ MANUAL: [Generated code and your code at runtime](MANUAL.md#generated-code-and-your-code-at-runtime) ·
[What AdHoc generates for each host](MANUAL.md#what-adhoc-generates-for-each-host) · [Injection points](MANUAL.md#injection-points) ·
[Generated objects or your own structures](MANUAL.md#choosing--or---generated-objects-or-your-own-structures)

### Errors stopped before any code exists

A description passes three gates before generated code reaches your project:

1. **The C# compiler.** A description is ordinary C#, so your IDE shows compile errors as you type, and AdHocAgent compiles it with
   Roslyn.
2. **The AdHoc rules**, checked by AdHocAgent on your machine. The first error stops the run, and nothing leaves your machine until the
   description passes. Some of the rules: only one side may lead a state; a pack may be claimed only once per state and side; a range of a
   single value is refused; a varint whose span fits in one byte is refused, with a hint to use `[MinMax]`; a guaranteed leg needs a
   connection that keeps its session.
3. **Tests on the generator server.** The server tests the code it generates before returning it. How much testing a task gets depends
   on the server's load, and the result says how far it went, so keep your own tests too.

Both ends of every connection are generated from one declaration. A field typed one way on one host and another way on its peer cannot
be written.

![A horizontal track from the description to the generated code, through three gates. Gate 1, the C# compiler (Roslyn), the same errors your IDE shows as you type - for example, a host name misspelled as Colector in a Connects declaration is an unknown type. Gate 2, the AdHoc rules, checked locally by AdHocAgent, with red examples stopped there: a state where both sides lead, a pack claimed twice in one state, MinMax(5, 5) as a range of a single value, A(1_000, 1_100) as a varint whose span fits one byte. A dashed boundary after gate 2: nothing leaves your machine until the description passes. Gate 3, on the generator server: the generated code is tested before it is returned, as far as the server's load allows, and the result says how far. At the end, generated code for every host.](docs/img/readme-gates.svg)

→ MANUAL: [The pipeline](MANUAL.md#the-pipeline-from-a-description-to-running-code) ·
[A description is compiled C#](MANUAL.md#a-description-is-compiled-c) ·
[Actor, state and branch rules](MANUAL.md#actor-state-and-branch-rules-at-a-glance) · [The one-byte rule](MANUAL.md#the-one-byte-rule) ·
[Server-side testing](MANUAL.md#progress-and-server-side-testing-of-generated-code)

### One description, every language, the same bytes

A host names its languages in its doc comment, and one description gives each host its code in each of them:

```csharp
/// <see cref="InCS"/>
struct Device : Host { }    // generated in C#

/// <see cref="InJAVA"/>
struct Server : Host { }    // generated in Java

/// <see cref="InTS"/>
struct Browser : Host { }   // generated in TypeScript
```

A C# device, a Java backend and a TypeScript browser client therefore speak the same protocol, not three implementations of it. Ranges and
limits become the same generated constants everywhere (`MIN`, `MAX`, `NULL`, `ZERO`, `STR_LEN_MAX`), Value Packs write identical bytes in
all three languages, and your doc comments become the documentation of the generated code. Markers for C++, Rust and Go are accepted
already; their generators are not there yet.

→ MANUAL: [Target languages](MANUAL.md#target-languages-of-adhoc) ·
[Range constants](MANUAL.md#range-constants-in-the-generated-code) ·
[The generated Value Pack in C#, Java and TypeScript](MANUAL.md#the-generated-value-pack-in-c-java-and-typescript) ·
[Documentation in the generated code](MANUAL.md#documentation-in-the-generated-code)

### One place to change, with a report of its reach

By hand, one change of the protocol is a change in every program that speaks it - the device firmware, the backend, the browser
client - plus a search to make sure none was missed. With a description it is one edit. Before anything is generated,
`AdHocAgent MyProtocol.cs~` compares the description with its previous version, by default the one your last deployment saved. It
names every host the edit reaches, and why, on a page for you and as JSON for a pipeline, which can then refuse an edit that touches a
frozen generation.

The same steps as the first time then regenerate the code and deploy it into your projects. Smart merge carries your code in the
injection points over into the new files, and everything a deployment overwrites is backed up first, with restore scripts.

Renaming or moving a pack, host, connection, actor or state changes nothing on the wire, because identities live in the numbers tables,
not in names. Adding, removing or reordering fields does change the wire, and the report says which hosts that reaches.

![One protocol change, two ways. Left, without a generator: the change becomes three hand edits in three repositories - a C# device, a Java backend, a TypeScript client - one of them missed and marked in red, and an older device generation broken by the edit. Right, with AdHoc: one edit of MyProtocol.cs; AdHocAgent .cs~ lists the hosts it reaches - Device and Server to update, Browser and the frozen DeviceV1 not reached; one regeneration gives the code of every reached host in each of its languages; deployment brings it into those projects, where smart merge keeps your code and a backup is made first.](docs/img/readme-one-place-to-change.svg)

→ MANUAL: [The .cs~ impact report](MANUAL.md#the-cs-impact-report) ·
[Guarding a frozen generation in a pipeline](MANUAL.md#guarding-a-frozen-generation-in-a-pipeline) ·
[What changes the wire and what does not](MANUAL.md#what-changes-the-wire-and-what-does-not) · [Smart merge](MANUAL.md#smart-merge) ·
[Automated backups](MANUAL.md#automated-backups-and-restoration) ·
[Identity in a table, position in a pack](MANUAL.md#identity-in-a-table-position-in-a-pack)

### The description is C# and describes the whole system

A description is a C# file in a .NET project, so the tools you already use work on it: the IDE compiles it, navigates it and renames
across it, and an IDE rename keeps the wire id, because the number stays in the numbers table. A separate schema language would need tools
of its own.

And the description holds more than messages: packs and fields, hosts and their languages, connections, actors, states and branches,
sessions and guaranteed legs, streams, transform chains, trims, virtual connections and multiplexers. Who talks to whom, in which order,
and what happens when a socket drops are declarations, not prose in a design document. The design is the generator's input, so it cannot
go out of date. AdHoc's own agent and generator server talk over a protocol described and generated this way.
See [How AdHoc is organized](#how-adhoc-is-organized).

Code generation is free. It runs as a cloud service that you reach with a personal UUID. Checking a description
(`ADHOC_PARSE_ONLY=1`) and the `.cs~` impact report run locally, with nothing uploaded.

→ MANUAL: [A description is compiled C#](MANUAL.md#a-description-is-compiled-c) ·
[The model: from the project down to the bytes](MANUAL.md#the-model-from-the-project-down-to-the-bytes) ·
[UUID](MANUAL.md#uuid) · [Environment variables of AdHocAgent](MANUAL.md#environment-variables-of-adhocagent)

## Ready solutions

Real problems need several capabilities at once. Each recipe below takes a problem that teams solve by hand again and again - on
devices, in backends, in apps - and solves it with a few declarations: the problem, what is usually built instead, the AdHoc
declaration, what then happens at runtime, and the honest limits. The map shows where each recipe sits in one system; the table
lists the capabilities each one combines.

![One system drawn in three zones - field and internet, edge, private network - with orange recipe tags R1 to R22 pinned to the hosts and links they concern. Vehicles of two firmware generations, 3.x and a frozen 2.x, and a phone app dial port 443 of an Edge host, each socket opening with the uid byte of the dialing host; inside the Edge a multiplexer, whose first byte is the dialing host's uid, holds the connections Telemetry, TelemetryLegacy and Shop (R2, R3, R4, R10, R13, R18). A browser console reaches the backend through a relay hub, and a dashed purple line beside the physical links marks its virtual connection (R6, R7, R14). The same console, the operator's, also dials a drone right below it, a C# host with live video: on that one link the video goes up and the commands go down (R19). Sensors reach the backend through a gateway that only relays and then the hub: a virtual connection through two relays (R6, R13). Right above that gateway, a field controller in C# or Java hangs on a serial line to it, RS-485 drawn as a pair of wires, framed so that the link recovers after a damaged byte (R22). A robot or game client talks UDP to a game server that has its own port (R5). In the private network, backend nodes joined to each other by Connects<Node, Node> (R16) run jobs, subscriptions, role-limited conversations, workflows and chat rooms, and send one pack to many subscribers without copying it (R8, R9, R12, R14, R18, R20), next to an object store that keeps bytes it cannot read (R1), a log broker fanning out to consumers (R11), an ingest host that writes into your own structures (R15) and an audit tier that reads headers, not payloads (R17). The game server's link runs on into the private network as well, to a monitoring host that stores what it receives and broadcasts it to three observers in the same pass (R21). A legend explains solid lines as TCP or WebSocket connections, dotted lines as UDP, dashed purple lines as virtual connections through a relay, a double line as a serial line, UART or RS-485, orange tags as recipes and a yellow square as the uid byte of the dialing host.](docs/img/readme-recipes-map.svg)

| Recipe | Combines | Where it shows up |
|:--|:--|:--|
| [R1. Object storage that keeps bytes it cannot read](#r1-object-storage-that-keeps-bytes-it-cannot-read) | the same chunked streams, a trim per field: the whole object or a part of it kept as is, compressed (`Zstd`), or compressed and encrypted (a cipher of your own); flows | black-box recorders, archives, message and attachment stores, recorded media |
| [R2. Versioning without version fields](#r2-versioning-without-version-fields) | generations as hosts, multiplexing, the `.cs~` impact report in CI | firmware in the field, app versions, server generations |
| [R3. One public port for many services](#r3-one-public-port-for-many-services) | multiplexing, relayed connections, the encrypted transport | edge servers, web and mobile clients, admin tools |
| [R4. A client that never loses an order or an event](#r4-a-client-that-never-loses-an-order-or-an-event) | sessions (`[Resumable]`), guaranteed delivery, identity and reclaim | phones, cellular trackers, field-service apps |
| [R5. Reliable where it matters, fresh where it does not](#r5-reliable-where-it-matters-fresh-where-it-does-not) | `[UDP<…>]`, guaranteed delivery, per-pack chains, Value Packs | multiplayer games, robots and drones on radio links |
| [R6. A gateway that never needs a schema update](#r6-a-gateway-that-never-needs-a-schema-update) | virtual connections, the generated Relay, end-to-end chains | IoT gateways, NAT brokers, browsers and tools reaching private hosts |
| [R7. Live dashboards and consoles in the browser](#r7-live-dashboards-and-consoles-in-the-browser) | function-group actors, RPC in both directions, stateful actors, TypeScript over WebSocket | operator consoles, admin UIs, monitoring |
| [R8. Long-running jobs with progress, cancel and deadlines](#r8-long-running-jobs-with-progress-cancel-and-deadlines) | stateful actors, `MaxActiveInstances`, states, `[Deadline]` | exports, builds, batch processing |
| [R9. Fan-out subscriptions with leases](#r9-fan-out-subscriptions-with-leases) | `[Grouped]` actors, `[Idle]` leases, groups across connections and tunnels | market data, fleet-wide config and alerts |
| [R10. Files, media and OTA updates of any size, resumable](#r10-files-media-and-ota-updates-of-any-size-resumable) | `Stream` and `File` fields, `[S(N)]` caps, one buffer per direction, guaranteed delivery | attachments, firmware updates, archives |
| [R11. A log broker that compresses once and fans out](#r11-a-log-broker-that-compresses-once-and-fans-out) | a trim at a chosen depth of a chain, `[Zstd]`, `[ChaCha20]` | event logs, analytics pipelines |
| [R12. Least-knowledge hosts](#r12-least-knowledge-hosts-every-role-sees-only-its-own-messages) | Pack Sets with name and tag filters, per-state branches, trims | multi-role systems, partner integrations |
| [R13. Every byte costs airtime](#r13-every-byte-costs-airtime-telemetry-in-a-handful-of-bytes) | `[MinMax]`, varint, time types, Value Packs, custom attributes | telemetry over cellular, satellite, low-power radio |
| [R14. Protocol-enforced workflows](#r14-protocol-enforced-workflows-no-command-out-of-order) | states and branches, the leading side, `[Deadline]`, `[Idle]` | robots, valves, operator consoles |
| [R15. Ingest straight into your own data structures](#r15-ingest-straight-into-your-own-data-structures) | abstract (`-`) packs, one buffer per direction, compact encodings | data acquisition, small gateways with big payloads |
| [R16. An internal cluster protocol and a server mesh on the same port](#r16-an-internal-cluster-protocol-and-a-server-mesh-on-the-same-port) | `Connects<Node, Node>`, multiplexing, connection timeouts | brokers, chat and game server meshes |
| [R17. A metadata-routed, payload-blind middle tier](#r17-a-metadata-routed-payload-blind-middle-tier-audit-and-tracing) | pack headers (`HeaderFor`), trims, time types | audit trails, tracing |
| [R18. Chat and collaboration](#r18-chat-and-collaboration-rooms-presence-turn-taking) | `[Grouped]` room actors, `[Idle]` presence, turn-taking states | chat, collaborative editing, bots |
| [R19. Live video and audio that stop for an urgent command](#r19-live-video-and-audio-that-stop-for-an-urgent-command) | named `Stream` packs as segments, interruptible framing, the one-pack-at-a-time transmitter, independent directions | drones and robots streaming video, intercoms, field cameras |
| [R20. One pack, many recipients, no copies](#r20-one-pack-many-recipients-no-copies) | `send` queues a reference; every connection serializes the same object with its own cursor | events for all subscribers, settings for a fleet, game ticks |
| [R21. Receive, store and broadcast in one pass](#r21-receive-store-and-broadcast-in-one-pass) | a pack without implementation, `Receiver.BytesCopy`, `Transmitter.Broadcaster`, a guaranteed leg in | monitoring backends, market-data hubs, chat servers that persist |
| [R22. Serial links that recover after a damaged byte](#r22-serial-links-that-recover-after-a-damaged-byte) | `Receiver.Framing` and `Transmitter.Framing`: a frame per pack, a CRC-16, resynchronization at the next mark | UART and RS-485 links, radio modems |

### R1. Object storage that keeps bytes it cannot read

*Where: a vehicle's trip logs in an archive · chat messages and attachments in a media store · telemetry samples in a monitoring
backend · recorded video · any object kept in a database column.*

**Problem.** A store between producers and consumers keeps objects for later. Either it understands every object type - and is
rebuilt with every schema change, and holds the keys because it must decrypt - or it gets blobs serialized, compressed and encrypted by
hand, outside the protocol.

**Built by hand.** An envelope with a length, a type and a version; compression and application-level encryption with their own key
distribution; a store that parses, or one whose blobs nobody can check; then the reverse of all of it on the consumer side, in every
language.

**AdHoc: the store keeps streams it does not read.** These are the same streams as everywhere in AdHoc. A **trim** on a field tells
the generated code of one leg to stop at a chosen point and hand the payload over as raw bytes, framed like any `Stream`:
`[len][data] … [0]`. The store's code pours those bytes into a sink of yours as they arrive and later pulls them from a source of
yours as its socket drains - a blob of any size, at constant memory, exactly as video moves in
[R19](#r19-live-video-and-audio-that-stop-for-an-urgent-command) and files in
[R10](#r10-files-media-and-ota-updates-of-any-size-resumable).

![Three hosts in a row: Producer, Store drawn as a cylinder, Consumer. Producer and Consumer each hold a typed Document card and the key of the custom AesCtr stage; the Store holds a stack of byte bricks indexed by object_id and three crossed-out chips: no schema, no key, runs no stage. Two wire lanes carry the same hatched, sealed chunks in chunked framing - Put, from Producer to Store, and Found, from Store to Consumer - and an equals sign between two hatched swatches above the store says: byte-identical, what is stored is what is replayed. Beside the store: sink, bytes in as they arrive, the payload never held whole; source, bytes out as the socket drains, no decode and no re-encode. Under each host its chain: the Producer runs its whole chain, Document, Zstd, AesCtr, wire; the Store's chain is greyed up to the cut that stands right before the wire, because on its legs it runs nothing; the Consumer runs the inverse chain from the wire back to a typed Document. Footer: Document gains a field and the store is not rebuilt; stored bytes stay bound to the stages left of the cut.](docs/img/readme-object-store.svg)

Two choices describe every stored object:

| | kept **as is** | kept **compressed** | kept **compressed and encrypted** |
|:--|:--|:--|:--|
| **the whole object is one blob** | `[Stream<In, Out>] Body doc;` | `[Zstd, Stream<In, Out>] Body doc;` | `[Zstd, AesCtr, Stream<In, Out>] Body doc;` |
| **part of the object is a blob** - the store reads the other fields | the same trims on some fields of the object | the same | the same |

Each trimmed field carries its own chain, so one object can mix all three forms - and the store still reads its ordinary fields,
indexes and filters by them:

```csharp
public class AesCtrAttribute : StreamCipherStageAttribute {    // a cipher stage of your own
    public AesCtrAttribute() { }
    public AesCtrAttribute(Binary[,] key, Binary[,] iv) { }     // injected at runtime by the producer and the consumer
}

interface FromProducer : IfSendingFrom<Producer, ProducerToStore> { }   // the leg into the store
interface FromStore    : IfSendingFrom<Store,    StoreToConsumer> { }   // the leg out of it

public class Body      { [D(+4096)] string title; [D(1_000_000)] Binary[,] text; }
public class Thumbnail { [D(200_000)] Binary[,] png; }
public class Secret    { [D(+256)] string iban; [D(+256)] string passport; }

// the whole object kept as one blob, in each of the three forms
public class PutPlain      { ulong object_id; [Stream<FromProducer, FromStore>]               Body doc; }
public class PutCompressed { ulong object_id; [Zstd, Stream<FromProducer, FromStore>]         Body doc; }
public class PutSealed     { ulong object_id; [Zstd, AesCtr, Stream<FromProducer, FromStore>] Body doc; }

// an object kept partly as fields the store reads, partly as blobs
public class Record
{
    ulong           object_id;                                           // the store reads these fields:
    long            created;                                             // it indexes and filters by them
    [D(+64)] string owner;
    [Stream<FromProducer, FromStore>]               Thumbnail thumbnail; // a blob kept as is
    [Zstd, Stream<FromProducer, FromStore>]         Body      body;      // a compressed blob
    [Zstd, AesCtr, Stream<FromProducer, FromStore>] Secret    secret;    // a compressed and encrypted blob
}
```

The agent prints each chain as it understood it - for `Record.secret`:
`leaf -> Zstd -> AesCtr -> |cut ToStream(Producer@ProducerToStore) FromStream(Store@StoreToConsumer)| -> wire`.

![What an AdHoc store keeps, as a grid. Three columns: as is ([Stream<In, Out>]), compressed ([Zstd, Stream<In, Out>]) and compressed and encrypted ([Zstd, AesCtr, Stream<In, Out>]). Row one, the whole object is one blob: a typed object_id beside the doc blob - wide and striped as is, narrower and yellow when compressed, as narrow, hatched and locked when sealed. Row two, part of the object is a blob: one Record spans the columns, with typed object_id, created and owner that the store reads, indexes and filters by, then a thumbnail kept as is, a body kept compressed and a secret kept compressed and encrypted - one object, three forms. Under the columns, who can decode a stored blob: any host with the schema; a host with the schema and the decompressor; only the producer and the consumer, who hold the key - the store cannot read it. A band at the bottom: the same streams - every blob crosses as chunks, len data len data and a zero terminator, into your sink and out of your source, any size at constant memory, received equal to replayed byte for byte; in every column the store runs no stage and holds no key.](docs/img/readme-object-store-forms.svg)

At runtime:

- The producer serializes typed objects; each trimmed field runs its own chain - nothing, `Zstd`, or `Zstd` and then your cipher.
- The cut stands at the wire end of every chain, so the store runs no stage in any of the three forms and holds no key. It takes each
  blob as the bytes that came off the wire and later replays exactly those bytes: what it receives is byte-identical to what it sends.
- The consumer runs the inverse chains and holds exactly the `Record` that was sent: `Thumbnail` as it was, `Body` decompressed,
  `Secret` decrypted and decompressed.
- The store gets none of the blobs' code: `Body`, `Thumbnail` and `Secret` gain fields, and the store is neither touched nor rebuilt.
  It needs only the fields it reads.
- AdHoc itself works this way: its server stores the description archive an agent uploads exactly as it came off the wire, and its
  monitoring backend keeps telemetry samples as bytes and serves them with `[FromStream<…>]`.

**The forms compared.**

| The store keeps | Size at rest | Who can decode a stored blob | What the store runs and holds |
|:--|:--|:--|:--|
| as is | the serialized object | any host that has its schema | nothing |
| compressed | smaller: compressed once, by the producer | a host with the schema and the decompressor | nothing |
| compressed and encrypted | the compressed size, sealed | only the producer and the consumer - they hold the key | nothing, no key |

The cut is also a dial: written in the middle of a chain, it lets a store keep compressed bytes while the link itself is encrypted -
`[Zstd, Stream<In, Out>, ChaCha20]`: encrypted on the wire with the connection's keys, kept compressed at rest. The store then runs the
inverse of the stages right of the cut. No position needs the schema of the blob.

**Why it works.**

- The store depends only on the fields it reads, never on the blobs' schema.
- No decode, no re-encode and no recompression on the store's path; a blob never sits whole in the store's memory.
- With your cipher, the store cannot read what it keeps; only the producer and the consumer hold the key.
- Whole object or part of it, as is, compressed or sealed: a declaration per field, not a different store.

**Limits.** A trim stands on a field typed with a pack, on a `Stream` field or a named `Stream` pack, or on a single `string` - not
on a primitive or a collection: group such data in a pack and trim the pack. Stored bytes are pinned to the stages left of the cut and
to the payload's layout: name the stored chain once in a [flow](MANUAL.md#flows---a-chain-declared-once), keep it and the archived pack
frozen, and declare new ones beside them for a new format. A keystream cipher conceals but does not authenticate - for integrity add a
stage of your own that authenticates the bytes (a MAC). Keep the built-in `[ChaCha20]` right of a replayed cut: it seals bytes with one
connection's session key, which is why a store that must not read its payloads uses a cipher of your own.

**MANUAL:** [Trims](MANUAL.md#trims) · [Why a stored payload needs a trim](MANUAL.md#why-a-stored-payload-needs-a-trim) ·
[The three trims and who holds what](MANUAL.md#the-three-trims-and-who-holds-what) ·
[Writing a trim on a field or on a pack](MANUAL.md#writing-a-trim-on-a-field-or-on-a-pack) ·
[Cutting a chain at a chosen depth](MANUAL.md#cutting-a-chain-at-a-chosen-depth) ·
[The depth decides what the store must run](MANUAL.md#the-depth-decides-what-the-store-must-run) ·
[Store and replay with trims](MANUAL.md#store-and-replay-with-trims) · [Trims on named Stream packs](MANUAL.md#trims-on-named-stream-packs) ·
[Stream framing](MANUAL.md#stream-framing---chunked-and-interruptible) · [Custom transform stages](MANUAL.md#custom-transform-stages) ·
[Flows](MANUAL.md#flows---a-chain-declared-once)

### R2. Versioning without version fields

*Where: firmware generations in the field · app versions users never update · a new backend generation next to the old one.*

**Problem.** Old firmware and old apps stay in use for years. Every message carries a version, every handler branches on it, nobody
dares to remove a field, and a new server version means a new port and a migration window.

**Built by hand.** A version field in every message and `switch (version)` in every handler; fields kept "optional for
compatibility" forever; versioned ports or paths with balancer rules; compatibility test matrices.

**AdHoc.** A generation that is deployed and cannot be updated is **not edited**. The new generation is a **new host** with its own
connection, declared next to the old one, and a multiplexer puts both behind one port:

```csharp
class PositionV2 { float lat; float lon; }                // what firmware 2.x was built against
class Position   { double lat; double lon; float speed; }

/// <see cref="InJAVA"/>
struct Fleet : Host { }                                    // the backend
/// <see cref="InCS"/>
struct VehicleLegacy : Host { }                            // firmware 2.x, still in the field
/// <see cref="InCS"/>
struct Vehicle : Host { }                                  // firmware 3.x

interface TelemetryLegacy : Connects<VehicleLegacy, Fleet> { [l____________<PositionV2>] struct Start { } }   // frozen
interface Telemetry       : Connects<Vehicle, Fleet>       { [l____________<Position>]   struct Start { } }   // free to change

interface FleetPort : Multiplex<(Telemetry, TelemetryLegacy)> { }   // both generations, one port
```

At runtime:

- Every socket on the port opens with one byte: the uid of the dialing host. The generated `FleetPort.apply(uid)` hands the socket
  to the connection of that generation before the first pack arrives; after that byte nothing is added to any pack.
- Old devices keep the packs and the state machine they were built against; new devices get whatever `Telemetry` becomes. A handler
  never asks which version it talks to.
- CI keeps the old generation frozen. `AdHocAgent ./FleetProtocol.cs~` lists the hosts an edit reaches and puts the answer in the
  report page as `adhoc-impact` JSON; extracted to `impact.json`, it fails the build in one line when the frozen host is among them:

  ```shell
  jq -e '(.update + .removed) | index("VehicleLegacy") | not' impact.json || exit 1
  ```

- Servers get generations the same way: `FleetV2` with a multiplexer of its own, and both generated multiplexers composed into one
  function behind the same port.

![A timeline in three phases separated by dashed lines. Phase 1: the 2.x devices, host Vehicle, dial port 443 of Fleet, where the multiplexer FleetPort.apply(uid), drawn as a rotary switch, hands them to the connection Telemetry; Console shares the port, so the port is multiplexed from day one and every device opens its socket with the first byte, the dialing host's uid. Phase 2: the 2.x devices, now VehicleLegacy - renamed in the IDE, same uid - carry a padlock - frozen, never edited - next to free 3.x Vehicle devices; both dial the same port, and the uid byte routes each socket to TelemetryLegacy with PositionV2 or to Telemetry with Position inside Fleet; below, a CI gate runs AdHocAgent FleetProtocol.cs~, reads the adhoc-impact JSON and fails when VehicleLegacy is in update or removed. Phase 3: 3.x Vehicle becomes the frozen generation, 4.x devices, the new host VehicleV4, arrive on the new connection TelemetryV4, the 2.x column fades with a trash-can glyph - retired: delete host, connection and packs - TelemetryLegacy is struck out of the port, and CI now guards Vehicle. A ribbon across the bottom: no version field in any pack, no switch on a version in any handler, one byte per socket and never per pack.](docs/img/readme-generations-timeline.svg)

**Why it works.**

- No version field on the wire and no version branch in code: the first byte settled the question, once per socket.
- The old dialect is frozen and the new one is free to drop packs, rename states and change types.
- Retiring a generation means deleting its host, connection and packs, with their lines in the numbers tables.
- A machine-checked guarantee, on every change, that no edit touched the frozen generation.
- Every generation of clients and servers shares one listener, one certificate and one NAT hole.

**Limits.** This fits a known population - devices you shipped, a retirement date you control - not an open ecosystem with no bound on
generations. "Frozen" is literal: packs have no field tags, so a field added anywhere, a widened type or a reordered field makes
another pack. A deleted host frees its uid for the next new host, so keep a retired host declared while its devices may still dial.
A multiplexer lists at least two connections: a retirement that would leave one keeps the retired connection in the tuple.

**MANUAL:** [Evolving a protocol](MANUAL.md#evolving-a-protocol) ·
[A new generation is a new host](MANUAL.md#a-new-generation-is-a-new-host) ·
[What changes the wire and what does not](MANUAL.md#what-changes-the-wire-and-what-does-not) ·
[When separate generations fit](MANUAL.md#when-separate-generations-fit) ·
[Several generations behind one port](MANUAL.md#several-kinds-of-peer-several-generations-behind-one-port) ·
[Server generations behind one port](MANUAL.md#server-generations-behind-one-port) ·
[Guarding a frozen generation in a pipeline](MANUAL.md#guarding-a-frozen-generation-in-a-pipeline)

### R3. One public port for many services

*Where: an edge server facing browsers, mobile apps and admin tools · a backend that must never be on the internet.*

**Problem.** Every service wants its own port, firewall rule and certificate. Mobile clients must reach a backend that should not be
exposed, so a proxy in front decodes and re-encodes everything.

**Built by hand.** A port per service, or a proxy that inspects every request to route it; a gateway that terminates the client's
protocol and re-issues the calls; certificates and firewall rules per endpoint.

**AdHoc.** List the connections that share the port in a multiplexer - including one the edge host is not a party of:

```csharp
// language markers of the hosts omitted
struct Browser : Host { }  struct Admin : Host { }  struct Mobile : Host { }
struct Edge    : Host { }   // owns the public port
struct Backend : Host { }   // reachable only from Edge

interface Console     : Connects<Browser, Edge>    { /* states and branches */ }
interface AdminLink   : Connects<Admin,   Edge>    { /* ... */ }
interface EdgeBackend : Connects<Edge,    Backend> { /* ... */ }   // the carrier
interface Query       : Connects<Mobile,  Backend> { /* ... */ }   // Edge is not a party of it

interface EdgePort : Multiplex<(Console, AdminLink, Query)> { }
```

At runtime:

- The first byte of each socket names the dialing host. Browser and Admin land on connections that Edge serves itself.
- Mobile dials the same port, but `Query` joins Mobile and Backend. Edge carries its bytes in a tunnel over `EdgeBackend` without
  decoding them: Backend is never exposed, and neither Mobile nor Backend needs a link to the other.
- With a static X25519 key on Edge, every accepted connection is encrypted - a Noise NK handshake, then ChaCha20-Poly1305 records -
  and the uid byte travels inside the encrypted stream, never in clear.
- One port or a port per connection is chosen at deployment: the same generated code serves both, and a mixed shape keeps, for
  example, the admin connection on a port only the LAN reaches.

![Two panels. Left, dimmed, a port per service: Browser and Admin dial two ports of Edge and Mobile dials a port of Backend - three ports, three certificate badges and three firewall tags - and Backend is exposed to the internet under a red warning triangle. Right, one port: Browser, Admin and Mobile each dial the single port 443 of Edge, each arrow tipped with a small yellow square for its uid byte, Browser.uid, Admin.uid, Mobile.uid; a lock on the port and a note - X25519 key on Edge: a Noise NK handshake, then ChaCha20-Poly1305 records on every connection - say that the uid byte travels encrypted. Inside Edge the multiplexer EdgePort.apply(uid), drawn as a rotary switch, hands Browser and Admin to the connections Console and AdminLink, which Edge serves itself, while Mobile's Query continues as a dashed purple tunnel along the physical leg EdgeBackend to Backend, which sits behind a wall labelled private network, not exposed; Edge does not decode Query. Footer: one listener, one address, one certificate, one NAT hole; shared or separate ports are chosen at deployment with the same generated code.](docs/img/readme-edge-port.svg)

**Why it works.**

- One listener, one address to publish, one certificate to renew, one hole to punch through a NAT.
- The backend stays private, and the edge relays `Query` instead of re-implementing it.
- Peers that share nothing but the address they dial keep their own connections, packs and state machines.
- Nothing is added per pack: the byte is paid once per socket.

**Limits.** A relayed connection needs exactly one carrier; both failure cases are parser errors with named fixes. A multiplexed port
is TCP or WebSocket: no `[UDP]` connection and no virtual connection in the list. Both sides must agree that the port is multiplexed.
A pooled connection object serves caller after caller, so clear your own fields in its `Connection Release` region and on close.

**MANUAL:** [Multiplexing](MANUAL.md#multiplexing) · [Declaring a multiplexer](MANUAL.md#declaring-a-multiplexer) ·
[The first byte of a multiplexed socket](MANUAL.md#the-first-byte-of-a-multiplexed-socket) ·
[Declared once, chosen at deployment](MANUAL.md#declared-once-chosen-at-deployment) ·
[Relayed connections in a multiplexer](MANUAL.md#relayed-connections-in-a-multiplexer) ·
[Wiring a multiplexed port](MANUAL.md#wiring-a-multiplexed-port) · [Keys of a cipher](MANUAL.md#keys-of-a-cipher)

### R4. A client that never loses an order or an event

*Where: a phone that pays in a lift · a tracker on a cellular link that drops every few minutes · a field-service app in a basement.*

**Problem.** The socket dies while an order or an alarm is half-sent. A retry may duplicate it, no retry loses it, and after the
reconnect both sides log in again and rebuild where they were.

**Built by hand.** An outbox, sequence numbers and acknowledgements, a retry loop, idempotency keys and a server-side de-duplication
table, a reconnect state machine with a "where was I" handshake.

**AdHoc.** Declare how long a session outlives its socket, and which packs each side sends with a guarantee:

```csharp
[Resumable(30)]
interface Shop : Connects<App, Backend> {
    [Resumable(0)] [L____________<Authorizing, App.Login>] struct Start { }       // nothing to keep before the login
    [Resumable(0)] [____________R<Working, Backend.Welcome>]
                   [____________R<Close, Backend.Denied>]  struct Authorizing { }

    [Resumable(60 * 24)]                                     // a working session waits a day for its phone
    [l____________<(App.Order, App.Location)>]               // Location: a snapshot, the next one replaces it
    [____________r<(Backend.Receipt, Backend.Welcome)>]      // Welcome too: a reclaimed session answers the new login
    struct Working { }

    interface FromApp     : Resumable<App, App.Order>         { int resumable_megabytes => 10; }   // once, in order
    interface FromBackend : Resumable<Backend, Backend.Receipt> { int resumable_megabytes => 4; }
}
```

On the backend, resumption is one call in the ordinary login handler (C#):

```csharp
var ext = conn.ext_connection;
long session_id = pack.resume_token != 0 ? ext.reclaim(pack.resume_token)   // a parked session moves onto this socket
                                         : ext.identify(account_id);         // a first login
// answer with Welcome(session_id) through ext.Internal: after a reclaim, that is the parked session
```

At runtime:

1. At a loss, an identified session is **parked**: its actors, their states, your objects and the journal stay; the socket and the
   buffers go back to their pools.
2. Offline, the app may keep sending orders: each one is journalled at once (up to the 10 MB cap) and leaves after the reconnect. A
   `Location` sent meanwhile is dropped.
3. On the new socket the ordinary login runs. On the backend, `reclaim` moves the parked session onto it, still in `Working`, and
   `resume` fires on both ends.
4. The receiver names its position and the sender replays from there: every order arrives **once, in order**; a half-sent one goes
   again from its first byte.

The cost: a journal copy of each guaranteed pack on every send, and one control record (pack id + 8 bytes) per 4 KiB of guaranteed
packs received. The packs themselves carry nothing extra, and packs outside the guaranteed set are not copied.

![A timeline with three lanes - App on top, the wire in the middle, Backend at the bottom - and three bands: socket 1, no network, socket 2. On socket 1 Order 41 arrives with a tick and Receipt 41 comes back, a Location snapshot passes, and Order 42 is cut by a red mark where the socket dies: lost. In the gap, under the App lane, the journal holds 42 and 43 - 43 sent offline is journalled - while 41 is freed once acknowledged and a Location sent offline is dropped. In the gap the Backend lane shows a parked session box: actor still in Working, your objects, its journal, no socket and no buffers, waiting up to Resumable(60 * 24) minutes. On socket 2: Login with the token, reclaim on the Backend - the parked session moves onto socket 2 - resume on both ends, a CONTINUE record sent back by the Backend, then Orders 42 and 43 replayed from the journal and ticked once, in order. Footer: you declare Resumable and Resumable<App, App.Order> and write one reclaim call; not covered: a killed sender process, which leads to resume(null) and a resync.](docs/img/readme-never-lose.svg)

**Why it works.**

- Two kinds of declaration and one call replace an outbox, a retransmit protocol, a de-duplicator and a reconnect state machine.
- Resumption is not a second protocol: the same login plus one `reclaim` call, and the server-side conversation continues in the very
  objects it was in.
- Reliability is chosen per pack: snapshots stay out of the journal.
- At the journal's cap the sending queue stops - backpressure, not unbounded growth.

**Limits.** The guarantee lasts as long as the sender's session and journal. A killed process comes back without them: the receiver
hears `resume(null)` and resyncs. State that must survive that is described as packs and stored - in a file or a database row a pack
is the same bytes as on the wire - and rebuilt from them on start. Each side identifies its connection before it sends a guaranteed
pack - the app too. The session id is not a secret: authenticate the peer before `reclaim`.

**MANUAL:** [Sessions, guaranteed delivery, UDP](MANUAL.md#sessions-guaranteed-delivery-udp) ·
[Parking a session](MANUAL.md#parking-a-session) · [Packs in flight at the moment of loss](MANUAL.md#packs-in-flight-at-the-moment-of-loss) ·
[Continuing on the accepting side: reclaim](MANUAL.md#continuing-on-the-accepting-side-reclaim) ·
[Guaranteed delivery](MANUAL.md#guaranteed-delivery) · [How the journal works](MANUAL.md#how-the-journal-works) ·
[What the guarantee costs](MANUAL.md#what-the-guarantee-costs) ·
[When the sender has lost its journal](MANUAL.md#when-the-sender-has-lost-its-journal) ·
[Pitfalls of session identity](MANUAL.md#pitfalls-of-session-identity)

### R5. Reliable where it matters, fresh where it does not

*Where: multiplayer games · robots, drones and farm machines on radio or mobile links with real packet loss.*

**Problem.** Inputs, commands and chat must arrive once and in order; positions and battery readings must be fresh, not reliable. TCP
stalls on every loss to resend data that is already stale, plain UDP loses commands, and the link crosses NAT and must be encrypted.

**Built by hand.** A reliability layer over datagrams - sequence numbers, acknowledgements, resend windows, reorder buffers,
duplicate filters - plus a second channel for samples, NAT keepalives, congestion pacing, a key handshake and a reconnect path.

**AdHoc.** Over UDP every pack is guaranteed by default; you name the ones that may be lost:

```csharp
interface Volatile : _<(Client.PlayerPosition, Server.Telemetry)> { }   // snapshots: the next one replaces the last

[UDP<Volatile>(30)]                               // over UDP; a parked session waits up to 30 minutes
interface GameLink : Connects<Client, Server> {   // no Resumable<...> here: everything else is guaranteed already
    [l____________<(Client.Input, Client.PlayerPosition, Chat)>]
    [____________r<(Server.Score, Server.Telemetry, Chat)>]
    struct Play { }
}
```

At runtime:

- The agent declares a guaranteed leg for each host over everything it sends except `Volatile`. A lost datagram shows as a gap in the
  datagram numbers; the receiver asks to continue, and the sender replays the guaranteed packs from its journal. A lost position is
  simply gone - the next one is already on its way.
- Each datagram starts with a 9-byte header - flags, connection id, series, datagram number - which also carries the journals'
  acknowledgements.
- The runtime keeps the NAT binding warm (a ping after 15 s without sending), paces with congestion control and sends datagram
  payloads of up to 1200 bytes. With a static X25519 key on the server, every datagram is encrypted and authenticated (a 16-byte tag).
  A chain written on the connection (`[Zstd]`, `[ChaCha20]`) is applied to each pack, so a lost datagram cannot break the compressor
  or the cipher for what follows.
- A peer that falls silent parks its identified session exactly as over TCP, so a player who drops out can come back to the same
  session.
- `PlayerPosition` - two floats, 64 bits - is a Value Pack: one 64-bit number in C# and Java, no object per sample.

![Client on the left, Server on the right, and between them a row of six datagrams on a dotted UDP line, each starting with a short header segment that carries its number, labelled once: 9-byte header - flags, connection id, series, number. Inside the datagrams, solid orange Input and Chat packs, numbered 1 to 5 and marked with a small journal glyph, are guaranteed, and dashed grey Position packs are not. The third datagram, with Input 3, Chat 4 and a Position, is struck by a red mark: lost; the fourth, with only a Position, shows the gap in the numbers, and the receiver asks to continue. Row one, guaranteed, what Server receives: Input 3 and Chat 4 reappear in the fifth datagram, tagged replayed from the journal, and the packs arrive once, in order: 1 2 3 4 5. Row two, unguarded: the lost Position is crossed out and the next Position arrives - the next one replaces it, no replay. Bottom left in orange, UDP<Volatile>(30), with the caption on TCP you name the packs that must not be lost; on UDP, the ones that may be, and a note: Position is PlayerPosition { float x; float y; } - 64 bits, one number: a Value Pack. A settings card: keepalive 15 s for NAT, idle 30 s, mtu 1200 bytes, window 256 KiB, congestion control on, X25519 key - every datagram encrypted and authenticated with a 16-byte tag.](docs/img/readme-udp-selective.svg)

**Why it works.** Reliability is a per-pack declaration, by name: *on TCP you choose the packs that must not be lost; on UDP you choose
the ones that may be.* One socket, one state machine and one set of handlers carry both kinds. Loss recovery, NAT keepalive,
congestion control and encryption come with the runtime, tuned by its settings - not a layer you maintain.

**Limits.** C# and Java only: a browser cannot open a UDP socket, so a browser player is a separate host on a WebSocket connection. A
`[UDP]` connection has a port of its own and cannot join a multiplexer. Its journals are fixed at 100 MB per sending host. Identify
the connection before sending guaranteed packs.

**MANUAL:** [UDP connections](MANUAL.md#udp-connections) · [Every pack guaranteed, both ways](MANUAL.md#every-pack-guaranteed-both-ways) ·
[Leaving packs unguarded on UDP](MANUAL.md#leaving-packs-unguarded-on-udp) · [Rules for UDP connections](MANUAL.md#rules-for-udp-connections) ·
[Per-pack chains on connections that guarantee packs](MANUAL.md#per-pack-chains-on-connections-that-guarantee-packs) ·
[Value Packs](MANUAL.md#value-packs)

### R6. A gateway that never needs a schema update

*Where: sensors behind a gateway · devices behind NAT that reach the cloud through a rendezvous broker · browser dashboards that reach
private services through a hub · a technician's tool that reaches one vehicle through the backend the vehicle dialed.*

**Problem.** The middle host decodes every message and re-encodes it: two passes and a heap object per message, plaintext in the
middle, and a rebuild and rollout of every gateway whenever a message gains a field.

**Built by hand.** A bridge with its own copy of every message type; a home-made scheme to multiplex many devices over one uplink;
encryption that ends at the gateway; queues without end-to-end backpressure.

**AdHoc.** Declare a **virtual connection** between the two hosts that matter; the gateway only sits on its path:

```csharp
interface SensorToGateway : Connects<Sensor, Gateway> { [_____lr_____<Ping>] struct One { } }   // the physical legs
interface GatewayToCloud  : Connects<Gateway, Cloud>  { [_____lr_____<Ping>] struct One { } }

[Zstd(6), ChaCha20]                                   // end to end: compressed once, sealed past the gateway
interface SensorToCloud : VirtuallyConnects<Sensor, Cloud, Gateway> {
    int VirtuallyConnects<Sensor, Cloud, Gateway>.MaxTunnels => 65536;   // many sensors, one uplink: a 2-byte tunnel_id
    [l____________<(Sensor.Reading, Sensor.Alarm)>]
    [____________r<Cloud.Config>]
    struct Run { }
}
```

At runtime:

- Sensor and Cloud get the full connection - packs, actor, state machine - built on a tunnel. The gateway gets a generated **Relay**
  and a router whose `Tunneling` region (your code) maps a `tunnel_id` to the outbound connection.
- The relay reads the pack id and the `tunnel_id`, writes a fresh prefix and moves the body through region by region: no decoding, no
  assembling, no schema. A 1 GB tunnel and a 64-byte one cost it the same memory.
- When the uplink is slow, the relay stops reading, and TCP backpressure reaches the original sender: no queue grows in the gateway.
- A sensor that vanishes mid-frame ends only its own tunnel, with `ABORT`; the uplink and the other tunnels go on. Actors, deadlines,
  `[Grouped]` and guaranteed delivery work through a tunnel as on a direct link.
- The chain runs at the two endpoints only: the gateway forwards compressed, encrypted chunks and holds no key. A chain on the radio
  leg may stack on top - per-hop chains protect the links, a tunnel chain protects the conversation.

![Four sensors on the left, tunnels 1 to 4, a Gateway in the middle, the Cloud on the right. Short blue radio legs run from the sensors to the gateway, each inside a thin sleeve, with key glyphs at both ends of one, labelled optional hop chain per radio leg: protects the link only. One thick blue uplink runs from the gateway to the Cloud and carries the tunnel frames of all sensors interleaved, labelled frame = [id][tid][len][data]…[0], with the tunnel_id of each frame marked in purple and the data hatched as sealed. Above, a nested bundle of dashed purple lines runs from every sensor over the gateway straight down into the Cloud, labelled SensorToCloud: VirtuallyConnects<Sensor, Cloud, Gateway> with [Zstd(6), ChaCha20] end to end, a sealed envelope riding the bundle and a note: sealed chunks, the gateway holds no key. Inside the gateway only a funnel - Relay: reads pack id and tunnel_id, forwards regions - a small router table, Tunneling (your code): tunnel_id to the uplink, and three chips: no schema, no key, one region in memory. An orange callout: Sensor.Reading gains a field, the gateway is not rebuilt. The radio leg of sensor 3 is cut - gone - and its frame on the uplink ends with a red ABORT in place of the terminator: only tunnel 3 ends; the uplink and the other tunnels go on. At the bottom a dim orange backpressure arrow runs from the uplink back under the gateway to a sensor: uplink slow, relay stops reading, TCP window closes, the sender slows - end-to-end backpressure, no queue grows in the gateway.](docs/img/readme-gateway-relay.svg)

**Why it works.**

- The gateway is schema-blind: sensor packs change without touching or redeploying it.
- Constant memory and end-to-end backpressure replace growing queues.
- The payload stays confidential through the hop: the relay has no stage code and sees no plaintext.
- One declaration produces the gateway code and the endpoint code, and the agent checks the route against the physical legs.

A relay is right only when the middle host must not read what it carries. Pick by what the middle host has to do:

| The middle host must… | Use | Recipe |
|:--|:--|:--|
| route bytes between connections and never read them | a virtual connection and its generated Relay | this one |
| be the one public port in front of a hidden host | a relayed connection in a multiplexer | [R3](#r3-one-public-port-for-many-services) |
| keep a payload and replay it later, without its schema | a trim | [R1](#r1-object-storage-that-keeps-bytes-it-cannot-read), [R11](#r11-a-log-broker-that-compresses-once-and-fans-out) |
| route by metadata and leave the payload opaque | pack headers and a trim | [R17](#r17-a-metadata-routed-payload-blind-middle-tier-audit-and-tracing) |
| answer a call by calling further on | a cascading RPC | [MANUAL](MANUAL.md#cascading-rpc-multi-hop-relay) |
| inspect, transform or fan out the content | a Smart middle you build from the stream API (AdHoc generates none) | [MANUAL](MANUAL.md#choosing-what-the-middle-tier-does) |

**Limits.** `[ChaCha20]` conceals but does not authenticate: a relay cannot read the tunnel but could alter it undetected, so add a MAC
stage for integrity. The key of a chain on a tunnel is injected by your code at both endpoints, fresh for every stream. While a relay
waits for a slow outbound, its inbound connection receives nothing else. A virtual connection rides physical legs only and is never
listed in a multiplexer.

**MANUAL:** [Virtual connections and relays](MANUAL.md#virtual-connections-and-relays) ·
[Declaring a virtual connection](MANUAL.md#declaring-a-virtual-connection) ·
[Tunnel-only and structured virtual connections](MANUAL.md#tunnel-only-and-structured-virtual-connections) ·
[MaxTunnels](MANUAL.md#maxtunnels-and-maxstream_kilobytes) ·
[Relay: the runtime primitive](MANUAL.md#relay-the-runtime-primitive-on-a-relay-host) · [Closing a tunnel](MANUAL.md#closing-a-tunnel) ·
[Transform chains over a tunnel](MANUAL.md#transform-chains-over-a-tunnel) ·
[Choosing what the middle tier does](MANUAL.md#choosing-what-the-middle-tier-does)

### R7. Live dashboards and consoles in the browser

*Web apps · operator consoles · fleet and monitoring dashboards*

**Problem.** A browser dashboard needs live data pushed to it, typed calls to the server, sometimes a question from the server back
to the browser, and subscriptions that die with the tab - all over one connection, in TypeScript.

**By hand.** A query endpoint plus a separate push channel, a text envelope with a type string and a request id, a map of pending
requests with timeouts, a subscriber table with a sweeper, TypeScript interfaces copied from server classes, 64-bit ids sent as strings.

![One browser dashboard written in TypeScript and one Java server joined by a single WebSocket, with three lanes for three conversation shapes; orange marks packs the browser sends, blue packs the server sends, yellow what is generated. Notify, a function group: the server pushes Note and Alert, the browser sends Flag, and one shared object on each host serves them - nothing to acquire, track or release. getCpu, an RPC: the browser's caller instance moves Call, Return, End as ForPeriod goes out and CpuList comes back, while the server answers through a pooled context that is not registered; status is the same shape mirrored, the server asking and the browser answering. Feed, up to sixteen stateful subscriptions under Idle 120: each instance pair has its own address; Subscribe opens one, the server pushes Sample packs, a renewal from the browser keeps it alive, and on the server an hourglass ends an instance the browser stops renewing - the server's own Samples do not count. The socket dispatches by pack id and actor address; a padlock says the server key seals every record (Noise NK, ChaCha20-Poly1305) and the browser pins that key. A chip reads: no type strings, no correlation ids, no switch, no sweeper.](docs/img/readme-recipe-dashboard.svg)

**The AdHoc answer.** One connection, three actor shapes, each at its own cost (`Dashboard` is an `InTS` host, `Server` an `InJAVA` one):

```csharp
class Sample { int metric; longJS at; float value; }   // longJS: a 64-bit field kept within ±(2⁵³ − 1), exact in a TypeScript number

interface Console : Connects<Dashboard, Server>
{
    interface Notify : Actor                            // function group: fire-and-forget, both ways
    {
        [____________r<(Note, Alert)>] struct Messages { }
        [l____________<Flag>]          struct Flags    { }
    }

    (L____________, CpuList) getCpu(ForPeriod req);     // RPC: the browser asks the server
    (____________R, Status)  status(NoArg _);           // RPC: the server asks the browser

    [Idle(120)]                                         // a tab that stops renewing is let go
    interface Feed : Actor                              // stateful: up to 16 live subscriptions per tab
    {
        int Actor.MaxActiveInstances => 16;

        [L____________<Live, Subscribe>]
        struct Start { }

        [____________r<Sample>]                         // pushed by the server
        [l____________<Subscribe>]                      // renews the lease
        [L____________<End, Unsubscribe>]
        struct Live { }
    }
}
```

**At runtime**

- Everything travels on one WebSocket that the browser dials. The receiver dispatches by pack id and actor address - never by a type
  string.
- `Notify` is one shared object per host: the browser calls `Notify.Flags.send_Flag(flag, conn)` at any time, and nothing is acquired,
  tracked or released.
- `getCpu`: the caller's instance holds the callback context, so the reply reaches the code that asked; the server answers through a pooled,
  unregistered context. With a warm pool, neither side allocates an actor object for a call.
- `Feed`: each subscription is an instance pair with its own address. Only packs from the browser renew `[Idle(120)]` - the server's own
  `Sample` pushes do not - so the subscriptions of a frozen tab end by themselves. A closed tab closes the connection and every instance
  with it.
- Give the server a static X25519 key and every connection it accepts is encrypted (a Noise NK handshake, then ChaCha20-Poly1305 per
  record, `ws://` or `wss://` alike). The TypeScript client pins that key as the last argument of `connect`.

**Why it is good.** One description is the contract between TypeScript and Java - types, push, calls in both directions and the
subscription lifecycle - generated identically on both ends and changed in one place. Each shape pays only for what it needs: a function
group creates and tracks nothing, an RPC needs no request-id map and its replier registers nothing, a subscription is one pooled instance
pair. `longJS` keeps ids up to 2⁵³ − 1 exact in a plain TypeScript `number` and narrows them on the wire like any `[MinMax]` field.

**Limits.** A TypeScript host only dials; it has no server. The RPC shorthand is unlimited - write the actor out with `MaxActiveInstances`
to bound it. Handlers run on the connection's loops: a slow one stalls its direction, so hand long work to another thread.

→ MANUAL: [Four kinds of actor](MANUAL.md#four-kinds-of-actor) · [Function-group actor](MANUAL.md#function-group-actor) ·
[RPC methods](MANUAL.md#rpc-methods) · [\[Deadline\] and \[Idle\]](MANUAL.md#per-instance-timers-deadline-and-idle) ·
[longJS](MANUAL.md#longjs-and-ulongjs) · [Keys of a cipher](MANUAL.md#keys-of-a-cipher) ·
[The two loops](MANUAL.md#the-two-loops-threads-and-actor-handlers)

### R8. Long-running jobs with progress, cancel and deadlines

*Exports, renders and reports in apps · batch imports on a backend · test routines on a field controller*

**Problem.** A job takes minutes. The user wants progress, a cancel button and several jobs at once; the server must cap the jobs per
client, end the stuck ones, and never let a late message bring a finished job back to life.

**By hand.** A start endpoint that returns a job id, a polling endpoint or a push channel for progress, a job table with a status column,
a cancel endpoint, a timer per job, per-client counters, and every race handled case by case.

![Left: the Export state machine that both hosts run, as four boxes, Call, Return, Running and End, with arrows coloured by sender - ExportRequest from the client, ExportAccepted or ExportRejected from the server, ExportProgress from the server and ExportCancel from the client as loops that stay in Running, and ExportResult or ExportCancelled from the server to End; an hourglass in Running marks Deadline 600 seconds, and a caption says the server leads Running, the client may only ask. Right: one connection holding three job instance pairs under a counter of 3 of 8 (MaxActiveInstances), each pair with its own address and state - two in Running with their own hourglasses at different fill levels, one still in Return; a ninth Acquire ends in a grey box: null, refused, nothing sent; a peer that opens a ninth has its connection aborted. Bottom: the race the epoch closes - the client's ExportCancel with epoch 2 crosses the server's ExportResult; the server instance is already released, so the cancel arrives for an unknown instance with an epoch other than 0 and is dropped, while the client receives the result and the job is done.](docs/img/readme-recipe-long-jobs.svg)

**The AdHoc answer.** The job is a stateful actor: a small state machine that both hosts run.

```csharp
interface Export : Actor
{
    int Actor.MaxActiveInstances => 8;                    // eight jobs per client at once

    [L____________<Return, ExportRequest>]
    struct Call { }

    [____________R<Running, ExportAccepted>]
    [____________R<End, ExportRejected>]
    struct Return { }

    [Deadline(600)]                                       // each job's own ten minutes
    [____________r<ExportProgress>]                       // the server reports, the state stays
    [l____________<ExportCancel>]                         // the client asks to stop
    [____________R<End, (ExportResult, ExportCancelled)>]
    struct Running { }
}
```

**At runtime**

- `Export.Acquire(conn)` (`acquire` in TypeScript) opens a job. Each job is an instance pair with its own address, so eight jobs run side
  by side on one socket.
  A ninth `Acquire` returns `null` (`undefined` in TypeScript) and sends nothing; a peer that opens a ninth is treated as hostile and its
  connection is aborted. Your own admission rule - a quota per account - goes into the `Check limit` injection point.
- Only the server leads `Running`: the client can only ask (`ExportCancel` is a stay pack), and the server decides with `ExportCancelled`
  or `ExportResult`. The generated code refuses to send a pack the current state does not allow.
- Every transition increments the instance's 4-bit epoch, and every pack carries it. A cancel that crosses the result arrives for a server
  instance that is already gone, with an epoch other than 0, so it is dropped - the client simply receives the result.
- `[Deadline(600)]` costs one field per instance; the connection keeps only its nearest deadline, in the transport's existing list of
  deadlines - no timer object per job. When it runs out, `OnDeadline` releases that one job; the connection and the other jobs go on.
- In `END OnActivate` you hand the result to an object that outlives the conversation, and the instance returns to its pool for the next job.

**Why it is good.** The job lifecycle is one declared state machine, identical on both hosts and enforced by both. The classic races are
closed by construction: one leader per state, and the epoch drops stale packs. A job costs one pooled instance and one deadline field -
no job table, no polling, no sweep.

**Limits.** A cancel in a server-led state is a request the server answers, and the description says so. `[Deadline]` and `[Idle]` end
one instance; `[ReceiveTimeout]` and `[TransmitTimeout]` end the whole connection.

→ MANUAL: [When the RPC shorthand is not enough](MANUAL.md#when-the-rpc-shorthand-is-not-enough) ·
[The side that leads a state](MANUAL.md#the-side-that-leads-a-state) ·
[Opening an instance and its limit](MANUAL.md#opening-an-actor-instance-and-its-limit) ·
[How an instance ends](MANUAL.md#how-an-actor-instance-ends) ·
[\[Deadline\] and \[Idle\]](MANUAL.md#per-instance-timers-deadline-and-idle) ·
[Your code lives in the actor](MANUAL.md#your-code-lives-in-the-actor)

### R9. Fan-out subscriptions with leases

*Market data to traders · a new config or an alert to every vehicle of a model · chat rooms · watchers of a live list*

**Problem.** One event must reach every subscriber of a feed, whichever connection - or relay - it came through, while subscribers come,
go, and vanish without unsubscribing.

**By hand.** Subscriber maps keyed by connection id under a lock, lease timestamps and a sweeper thread, cleanup hooks on every close
path, and a separate path for subscribers that arrive through a gateway.

![The backend's memory holds a list of groups, Connection.Groups, with three groups - model X region Y, model X region Z, floor 3. Each group is a chain of subscription instances linked through their own prev and next fields, with a lock at the head: the group is its own lock. A ConfigUpdate enters the first group and walks its chain; from every member one pack goes into that vehicle's send queue, so a slow vehicle holds up nobody. Six vehicles on the right are joined to the backend by their own links. Three members leave by themselves: V4 slides out of the first group after its Unsubscribe ends the instance, V3 fades out of the second when Idle 300 runs out because it stopped renewing, and V6 drops out of the third when a lightning bolt breaks its connection. Footer: no subscriber map, no sweeper, join and leave in O(1) with no allocation, Idle counts only what the vehicle sends.](docs/img/readme-recipe-fanout.svg)

**The AdHoc answer.** A subscription is a stateful actor marked `[Grouped]`, with `[Idle]` as its lease:

```csharp
interface Telemetry : Connects<Vehicle, Backend>
{
    [Idle(300)]                                   // a vehicle that stops renewing is let go
    [Grouped]                                     // the Backend gathers instances of all connections into groups
    interface Subscription : Actor
    {
        int Actor.MaxActiveInstances => 4;

        [L____________<Live, Vehicle.Subscribe>] // "model X, region Y"
        struct Start { }

        [____________r<(Backend.ConfigUpdate, Backend.RegionAlert)>]
        [l____________<Vehicle.Subscribe>]        // renewals push the lease
        [L____________<End, Vehicle.Unsubscribe>]
        struct Live { }
    }
}
```

Your code on the backend (Java) - join on subscribe, walk on push:

```java
// a subscription arrives: the instance joins the group of its model and region (groupFor is yours)
Subscription.State.Start.OnReceived.Vehicle_Subscribe.handlers.add(
        (pack, conn, actor, transmitter) -> actor.join(conn, groupFor(pack)));

// a push to one group: walk it under its own lock - each send only queues
g.acquire();
try { for (Subscription s = g.head; s != null; s = s.next) Subscription.State.Live.ONE.transmitter.send(update, s.connection, s); }
finally { g.release(); }
```

**At runtime**

- The instances are the list: each carries `prev`, `next` and `group`, so joining and leaving are O(1) and allocate nothing. A group is its
  own reentrant spin lock; TypeScript, single-threaded, walks without one.
- The walk holds the lock only while it queues: a send only puts a pack in that connection's queue (a full queue refuses it with `false`),
  so a slow vehicle never holds up the rest.
- An instance leaves its group by itself in its `Release` - after `Unsubscribe`, when `[Idle]` or a `[Deadline]` runs out, or when its
  connection or tunnel closes. A group never holds a subscriber that is gone.
- `[Idle]` counts only packs received from the vehicle: pushing updates to a vanished subscriber does not keep it alive.
- `Connection.Groups` keeps lists of groups - instruments, models, regions, rooms - the same way.
- Subscribers behind relay hosts work the same: declare the actor on a virtual connection, and the server gathers the instances that
  arrive through all its relays into its groups; a tunnel that closes takes its members out.
- Inside one connection, a multicast actor (`MaxActiveInstances => +4`) adds a group address on the wire: `cast(pack, connection)` sends
  the pack once, and the peer hands it to each member.

**Why it is good.** No subscriber table, no sweeper, no lease bookkeeping: membership ends with the conversation, on every kind of end.
What you keep per subscriber lives in the instance, which every handler already receives. For market data, the ticks themselves shrink
with `[MinMax]`, varints and `TimeSpanDef` for recent moments ([R13](#r13-every-byte-costs-airtime-telemetry-in-a-handful-of-bytes)), and
orders ride a guaranteed leg on the same connection ([R4](#r4-a-client-that-never-loses-an-order-or-an-event)).

**Limits.** A group is an in-memory list of one host process: a backend spread over several processes routes between them itself. An
`UNLIMITED` swarm leaves the number of instances a peer may open unbounded - use it only between trusted hosts.

→ MANUAL: [Actors of many connections: \[Grouped\]](MANUAL.md#actors-of-many-connections-grouped) ·
[\[Deadline\] and \[Idle\]](MANUAL.md#per-instance-timers-deadline-and-idle) · [The group address](MANUAL.md#the-group-address) ·
[How an instance ends](MANUAL.md#how-an-actor-instance-ends) · [Concurrency modes](MANUAL.md#concurrency-modes)

### R10. Files, media and OTA updates of any size, resumable

*Firmware for vehicles and field controllers · video attachments in a chat or field-service app · archives between services*

**Problem.** Hundreds of megabytes must cross a link that drops, into a host that cannot hold them in memory. A transfer must not restart
from zero after every drop, must not hang forever, and must be checked before it is used.

**By hand.** Multipart or ranged-download endpoints, a chunk-and-offset resume protocol, temp files on every hop, a checksum step,
per-transfer timeouts, and size checks after the fact.

![Top band: the backend reads a 100 MB image from disk in blocks into one small socket buffer; a train of FirmwareChunk packs, each an offset and up to 64 KiB of data, crosses the wire; on the vehicle each chunk's data is handed over as a window of the receive buffer and written straight to flash, with no 64 KiB array. A flat memory gauge on each host: one buffer per direction, whatever the size. Middle band: the Update state machine - Start, Offered, Downloading, End - with FirmwareOffer (a 32-byte sha256 and a 64-byte signature) from the backend, Accept with from_offset or Decline from the vehicle, FirmwareChunk and Progress as loops on Downloading, Installed or Rejected to End, an hourglass with a six-hour Deadline on Downloading and a badge MaxActiveInstances 1, one update at a time. Bottom band: a progress bar, one cell per chunk. At a lightning bolt the socket is lost and the backend parks the session; after reconnect, login and reclaim the vehicle names its position, and the two cells it had not taken whole are outlined as replayed from the journal, once and in order. At a power symbol the vehicle reboots and its session is gone; an orange marker, Accept with from_offset, shows a new session continuing from the persisted offset.](docs/img/readme-recipe-big-transfers.svg)

**The AdHoc answer.** Pick the tool by the shape of the transfer:

| The transfer | Declare | What you get |
|:--|:--|:--|
| a file or an archive whose length is known | an `[S(N)] File` field | one varint length, then bytes piped from your source to the socket and from the socket into your sink; a total above `N` is refused before any payload byte is read |
| a live feed, or a length unknown up front | an `[S(N)] Stream` field | chunks `[len][data] … [0x0000]`, each length two bytes; the source may stop early or never; `[S(N)]` is enforced on both sides |
| hundreds of MB over a link that drops | chunk packs in a stateful actor + `Resumable<Sender, Chunk>` | a lost socket parks the session; after the reconnect the journal replays exactly what the receiver did not take whole |
| a reboot, or an outage longer than the session | an offset in a pack, persisted by the receiver | a new session continues from that offset |

AdHoc moves its own projects this way: every description upload and every generated-code download is one list of
`(path, [S(0x5_000_000)] Stream)` entries, compressed on the fly, with no archive-sized buffer anywhere. An OTA update over a cellular
link combines the last two rows:

```csharp
/**
<see cref = 'InJAVA'/>-                                  // on the vehicle, FirmwareChunk is abstract...
<see cref = 'FirmwareChunk'/>
<see cref = 'InJAVA'/>                                   // ...and every other pack concrete
*/
struct Vehicle : Host { }

class FirmwareChunk { uint offset; [D(65_536)] Binary[,] data; }   // up to 64 KiB per chunk

[Resumable(60 * 24)]                                     // a parked session waits a day
interface Telemetry : Connects<Vehicle, Fleet>
{
    /* ... your login states ... */

    interface Update : Actor
    {
        int Actor.MaxActiveInstances => 1;               // one update at a time per vehicle

        [____________R<Offered, FirmwareOffer>]          // size, a [D(32)] Binary[] sha256, a [D(64)] Binary[] signature
        struct Start { }

        [L____________<Downloading, Accept>]             // Accept { uint from_offset; }
        [L____________<End, Decline>]
        struct Offered { }

        [Deadline(6 * 3600)]                             // six hours at most
        [____________r<FirmwareChunk>]
        [l____________<Progress>]
        [L____________<End, (Installed, Rejected)>]
        struct Downloading { }
    }

    interface FromFleet : Resumable<Fleet, FirmwareChunk> { int resumable_megabytes => 128; }
}
```

**At runtime**

- Memory stays flat. A `File` or `Stream` conduit moves bytes in blocks between your source or sink and the one socket buffer per
  direction. In the OTA, `FirmwareChunk` is abstract on the vehicle, so each chunk's data arrives as windows of the receive buffer that
  your code writes straight to flash - no 64 KiB array.
- Short dropouts: the backend parks the session with `Update` in `Downloading`. Once the vehicle is back and your login reclaims the
  session, the vehicle names the position it has taken whole, and the backend replays its journal from there - every chunk once, in
  order. A chunk cut half-way goes again from its first byte.
- Reboots and long outages: the journal lasts only as long as the parked session - a day here - and a rebooted vehicle has lost its side
  of it. The vehicle persists its offset and answers `Accept { from_offset }` in a new session; the backend seeks its image file there.
  Without `FromFleet`, this offset alone is a journal-free variant.
- `sha256` and `signature` are fixed-size `Binary` arrays with no length prefix; your code checks them before it answers `Installed`.
  `[Deadline]` ends a stuck download, and `MaxActiveInstances => 1` refuses a second update.

**Why it is good.** Resume is declared, not built: a chunk pack plus a guaranteed leg for dropouts, one offset field for reboots. The
update flow - offer, accept or decline, download with progress, install or reject - is one state machine that both sides run. A 1 GB
image costs the same socket buffers as a 64-byte status pack, and the one extra cost is written in the description: up to 128 MB of
unacknowledged journal per vehicle connection on the backend (in memory, then in 4 MB files on disk), which the offset-only variant avoids.

**Limits.** A `Stream` or `File` under way at a loss is not continued in the middle (a guaranteed one is replayed from its first byte), so
links that drop use chunk packs. A conduit source runs on the transmit loop and must answer `Read` promptly. The dialing side restores
its own state when `resume` fires.

→ MANUAL: [Raw conduits](MANUAL.md#raw-conduits---stream-and-file) · [The size cap \[S(N)\]](MANUAL.md#the-size-cap-of-a-conduit---sn) ·
[Delivering an archive of any size](MANUAL.md#worked-example---delivering-an-archive-of-any-size) ·
[Binary, File or Stream](MANUAL.md#binary-file-or-stream---choosing-by-the-shape-of-the-bytes) ·
[Guaranteed delivery](MANUAL.md#guaranteed-delivery) · [Packs in flight at a loss](MANUAL.md#packs-in-flight-at-the-moment-of-loss) ·
[Abstract packs](MANUAL.md#abstract---your-code-holds-the-data) ·
[Restoring a receiving side](MANUAL.md#restoring-the-state-of-a-receiving-side)

### R11. A log broker that compresses once and fans out

*Event and audit logs · telemetry history · device trip logs*

**Problem.** Producers send events; a broker writes them to disk and serves them to many consumers. Either the broker decompresses and
recompresses every batch for every consumer, or compression happens outside the protocol and the typed messages are lost - and the broker
is coupled to every event schema.

**By hand.** Batch envelopes with a compression flag, a broker that decodes records to append them and encodes them again for each
consumer, and a schema registry the broker consults.

![A producer holds a typed Event, compresses it once with Zstd and encrypts it with ChaCha20 for its link. The broker runs only the inverse cipher with that link's key and appends the compressed blob to an append-only log of bricks marked z, labeled: Zstd blobs, never an Event - no schema, no compressor, no Event code. On the way out the broker seals the same bytes with each consumer's own link key; four consumers decrypt, decompress and get a typed Event. A ledger below reads: compress 1 at the producer, recompress 0 in the broker, decompress N at the consumers. A ruler shows the chain leaf, Zstd, the cut marker, ChaCha20, wire: the broker runs only what stands right of the cut, and left of the cut is the stored form, a Zstd blob.](docs/img/readme-recipe-log-broker.svg)

**The AdHoc answer.** A trim placed inside a transform chain - `Zstd` left of the cut, `ChaCha20` right of it. This is MANUAL's worked
example:

```csharp
interface FromProducer : IfSendingFrom<Producer, ProducerToBroker> { }
interface FromBroker   : IfSendingFrom<Broker,   BrokerToConsumer> { }

// stored compressed: the broker decrypts the link, keeps the Zstd blob, and never sees an Event
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

**At runtime**

- Producer → Broker: the producer serializes and compresses the `Event` once, then encrypts it for this connection. The broker runs only
  `ChaCha20⁻¹` and appends the compressed bytes to its log - `store(bytes)`.
- Broker → Consumer: the broker hands back the same bytes - `replay(bytes)` - encrypted with that consumer's connection key. The consumer
  decrypts, decompresses and gets a typed `Event`.
- On the broker, `Event` is bytes in and bytes out: it never builds an `Event` and never re-encodes one, so the generator gives it none of
  the `Event` type's code.
- Every other leg runs the full chain, with a typed `Event` on both ends.

**Why it is good.** N consumers cost one compression, not N. The disk stays small with no compressor and no schema in the broker, and
events gain fields without the broker being touched or rebuilt. Each link is encrypted with its own session key while the log stays
compact. Retention and replay are just the bytes on disk, served as they are. Live consumers can be kept as a `[Grouped]` actor with an
`[Idle]` lease ([R9](#r9-fan-out-subscriptions-with-leases)).

**Limits.** Both links need an encrypted transport: the pack-level `ChaCha20` takes each connection's session keys. The broker holds
those keys and sees the compressed plaintext. When it must not, use a cipher stage of your own whose key only producers and consumers
share, and cut at the wire end - the keyless store of [R1](#r1-object-storage-that-keeps-bytes-it-cannot-read). The chain left of the cut
(`Zstd` and its level) is the persisted format of the log: name it once in a flow, and declare a new flow beside it instead of editing it.

→ MANUAL: [A schema-blind log broker](MANUAL.md#worked-example---a-schema-blind-log-broker) ·
[The depth decides what the store must run](MANUAL.md#the-depth-decides-what-the-store-must-run) ·
[The three trims](MANUAL.md#the-three-trims-and-who-holds-what) · [Keys of a cipher](MANUAL.md#keys-of-a-cipher) ·
[Trim pitfalls](MANUAL.md#trim-pitfalls)

### R12. Least-knowledge hosts: every role sees only its own messages

*Trial and paid client tiers · staff consoles · partner and analytics services · device classes with different rights*

**Problem.** Every service and every client links the same message library. A guest client technically knows how to build staff
messages, an analytics service carries the credential types, and "who may send what, and when" is checked by hand in the handlers.

**By hand.** A shared message library deployed everywhere, authorization middleware that inspects message types per role, order checks
in handlers ("not before the login"), DTO subsets per client tier, and ad-hoc limits against peers that open too much or send too much.

![A matrix: rows are the hosts Guest in TypeScript, Staff in C# and Shop in Java; columns are the packs Item, Price, Margin tagged with a padlock, Browse, Login, Welcome and Denied, SetPrice. Filled cells mean the pack is generated in that host, empty cells mean it is not in its code: Guest has only Item, Price and Browse; Staff has everything except Browse; Shop has all of them. A funnel labeled SkipDoc padlock keeps Margin off the guest link and crosses out the Guest Margin cell. On the left, the staff link's states: Start takes only Login from Staff, Authorizing answers Welcome or Denied from Shop, Denied leads to Close and Welcome to Working, where Staff sends SetPrice and Shop the catalogue; a crossed-out SetPrice in Start is refused before it leaves, reported as DISCARDED_BYSTATE. On the right, four guard rails: a pack the state does not allow is refused and reported; an unknown pack id drops the channel; one instance over MaxActiveInstances aborts the connection; a conduit above its S(N) cap raises an exception.](docs/img/readme-recipe-least-knowledge.svg)

**The AdHoc answer.** Each connection lists, state by state, the packs each side may send; tags and filter templates select them by role:

```csharp
public class Catalog
{
    public class Item  { int sku; string title; }
    public class Price { int sku; long cents; }

    /// 🔒 staff only
    public class Margin { int sku; int percent; }
}

[SkipDoc(@"🔒")]
interface NoStaff<SCOPE> { }                    // a policy written once, applied anywhere

interface GuestLink : Connects<Shop, Guest>
{
    [l____________<NoStaff<@Catalog>>]          // Item and Price: Margin is not in Guest's code
    [____________r<Guest.Browse>]
    struct Start { }
}

interface StaffLink : Connects<Staff, Shop>
{
    [L____________<Authorizing, Staff.Login>]   // before the login: Login, nothing else
    struct Start { }

    [____________R<Working, Welcome>]
    [____________R<Close, Denied>]
    struct Authorizing { }

    [l____________<Staff.SetPrice>]
    [____________r<@Catalog>]                   // staff see the whole catalogue, Margin included
    struct Working { }
}
```

| Host | Its generated code holds | Not in its code |
|:--|:--|:--|
| `Guest` (TypeScript) | `Item`, `Price`, `Browse` | `Margin`, `Login`, `SetPrice`, `Welcome`, `Denied` |
| `Staff` (C#) | `Login`, `Welcome`, `Denied`, `SetPrice`, `Item`, `Price`, `Margin` | `Browse` |
| `Shop` (Java) | all eight packs | - |

**At runtime**

- A host's code is made of what it takes part in: its connections, the packs it sends or receives, and the types those packs are built of.
  A pack a role may not send or parse is simply absent.
- Before a pack is sent, the current state checks its id: `SetPrice` before the login is refused and reported (`DISCARDED_BYSTATE`), and an
  unexpected id that arrives goes to the receiver's error handler.
- A pack id the receiver cannot dispatch at all drops the channel as broken; a peer that opens one actor instance more than
  `MaxActiveInstances` has its connection aborted; a conduit above its `[S(N)]` fails with an exception.
- A host that only keeps a payload behind a trim gets none of the payload type's code ([R1](#r1-object-storage-that-keeps-bytes-it-cannot-read)).

**Why it is good.** Least privilege by construction: what a host may not send or parse is not in its generated code. Protocol order is
enforced by both sides' generated state machines, not by handler code. Tags keep a large description manageable: a new pack in `Catalog`
tagged 🔒 stays off the guest link by itself.

**Limits.** This is not authentication: who the peer is remains your login, plus the transport's server-key pinning. A pack two tiers share
is in both tiers' code - split it when they must differ. Filter patterns are case-sensitive and unanchored regular expressions, and an
emoji tag must match code point for code point.

→ MANUAL: [Declaring a connection](MANUAL.md#declaring-a-connection) · [Pack Sets](MANUAL.md#pack-sets) ·
[Filtering by name and documentation](MANUAL.md#filtering-a-set-by-name-and-by-documentation) ·
[Filter templates](MANUAL.md#filter-templates-f) ·
[What the generator produces for actors and states](MANUAL.md#what-the-generator-produces-for-actors-and-states) ·
[What makes a host one to update](MANUAL.md#what-makes-a-host-one-to-update) · [Loss versus intent](MANUAL.md#loss-versus-intent)

### R13. Every byte costs airtime: telemetry in a handful of bytes

*Where it fits: trackers and vehicles on cellular or satellite links, low-power radio, mobile apps on metered data - any stream of
samples that never stops.*

![Declare what you know, the generator picks the bits: the eight fields of a telemetry Sample in three columns - the declaration, what it says about the values, and what each costs on the wire. A TimeSpanDef window of the last 28 hours or so costs 3 bytes; a MinMax(-40, 125) temperature 1 byte, sent as value + 40; a MinMax(0, 100) percentage 7 bits; a four-value enum 2 bits; a bool 1 bit - these bit fields are packed together and rounded up to whole bytes; an X(1_000) varint, mostly small and of either sign, a length bit plus one byte within ±127; a V(30_000, 0) varint, mostly near its ceiling, a length bit plus one byte near 30 000 and two bytes when far - the length bits join the bit-field bytes; an absent nullable ushort one null bit. A ribbon below shows the exTernal values your code sees, the inTernal codes the generated object stores and the ioT bits and bytes on the wire; callouts note that the custom Unit attribute and the range become constants in C#, Java and TypeScript and never travel, and that MANUAL's worked Reading pack is 14 bytes after its pack id](docs/img/readme-airtime-bits.svg)

**The problem.** Thousands of devices send a sample every second, and every byte costs airtime, battery and money. Text repeats field
names and spells numbers out; hand-packed layouts drift between firmware and backend, and nobody remembers a raw value's offset or scale.
**Built by hand:** bit packing per message and language, "value × 10 in a short" conventions in a wiki, timestamp tricks.

**With AdHoc** you declare what you know about each value - its range, where it clusters, how recent it is - and the generator picks the
wire form. Your code keeps working with the physical value (the exTernal layer); the codes stay inside the generated code and on the wire.

```csharp
class SecondsADay : TimeSpanDef                       // a recent moment: 3 bytes, 6 ms steps
{
    public TimeSpan interval  => TimeSpan.FromDays(1);
    public TimeSpan precision => TimeSpan.FromSeconds(1);
}
enum Gear { P, R, N, D }                              // 4 values: a 2-bit code
class UnitAttribute : Attribute { public UnitAttribute(string symbol) { } }   // your own metadata

class Sample
{
    SecondsADay                          taken;       // 3 bytes instead of 6 for a DateTime
    [Unit("degC"), MinMax(-40, 125)] short coolant;   // 166 values: 1 byte, sent as value + 40
    [MinMax(0, 100)]                 byte  fuel_pct;  // 101 values: a 7-bit field
    Gear                                   gear;      // 2 bits
    bool                                   ignition;  // 1 bit
    [X(1_000)]                       int   accel;     // ZigZag around 0: within ±127, a length bit + 1 byte
    [V(30_000, 0)]                   int   lease;     // usually near 30 000: sent as 30 000 - value
    ushort?                                dtc;       // absent: one null bit, nothing else
}
```

- **Ranges.** `[MinMax]` stores `value - min`, so only the span decides the width: a span below 128 is a bit field shared with the
  pack's other bit fields, up to 255 one byte, beyond that exactly the bytes the span needs - 3, 5, 6 or 7 included.
- **Distributions.** `[A]`, `[V]`, `[X]` name where values cluster - a floor, a ceiling, a center - and the generator subtracts that
  point, so a small number travels. An AdHoc varint carries no sign: a small negative change never costs 10 bytes.
- **Time and small packs.** `DateTime` is 6 bytes; `TimeSpanDef` pays only for a window of recent moments; `DateTimeDef` for a declared
  range. A pack whose fields fit in 64 bits is a Value Pack - one integer, no object (TypeScript: up to 53 bits).
- **No names, no tags.** A pack is its id and its fields by position: MANUAL's worked `Reading` pack - sensor id, ranged temperature,
  alarm bit, pressure varint, optional sequence, label, two samples - is 14 bytes after its pack id.
- **Units as constants.** `[Unit("degC")]` becomes a constant of the field in every language and never touches the wire; with the
  generated `MIN` / `MAX`, a TypeScript dashboard sizes its gauge without a copied number.

**Why it is good.** The bit layout is generated identically for every host and language, so firmware and dashboard cannot disagree on
an offset or a scale. Domain knowledge is written once and becomes the encoding - checked by the agent, which refuses a one-value
range, two range attributes on one field, or a varint whose span fits one byte.

**Limits.** A declared range is a hard limit: a value outside it cannot be sent. Field order is part of the wire format. `TimeSpanDef`
is for recent moments only. A `double` whose `[MinMax]` fits `float` becomes a `float`.

→ MANUAL: [How values become bytes](MANUAL.md#how-values-become-bytes) ·
[Narrowing with MinMax](MANUAL.md#narrowing-a-primitive-field-with-minmax) ·
[The varint attributes](MANUAL.md#the-varint-attributes-a-v-and-x) ·
[TimeSpanDef](MANUAL.md#timespandef-a-recent-moment) · [Value Packs](MANUAL.md#value-packs) ·
[The layout of a pack on the wire](MANUAL.md#the-layout-of-a-pack-on-the-wire) ·
[Range constants](MANUAL.md#range-constants-in-the-generated-code) · [Custom attributes](MANUAL.md#custom-attributes)

### R14. Protocol-enforced workflows: no command out of order

*Where it fits: a robot arm, an AGV or a crane that moves only when armed; operator consoles for device control, payments back offices
and deployments - anywhere a dangerous command must follow a sequence.*

![The Motion state machine of Control between Console and Robot: Safe, led by the Console, goes to Arming on Console.Arm; Arming, led by the Robot and limited by Deadline(3), goes to Armed on Robot.ArmedAck or ends on Robot.Refused; Armed, led by the Console, stays on Jog and Heartbeat from the Console and on Pose from the Robot, ends on Console.Disarm, and its Idle(1) lease is held by the Robot. Lower left: the Console's generated transmitters, one send method per pack the state allows - Arm in Safe, none in Arming, Jog, Heartbeat and Disarm in Armed - and a dashed card showing that Safe has no method for Jog. Lower middle: a Jog sent anyway in Safe is refused before it leaves as DISCARDED_BYSTATE; a second Motion opened by the peer aborts the connection, a second local Acquire returns null; a late pack from a previous state is dropped by the epoch. Lower right: the Robot's timeline in Armed - Jog and Heartbeat ticks, then one second of silence, and OnDeadline fires so your code makes a safe stop while the connection lives on; only received packs count](docs/img/readme-guarded-workflow.svg)

**The problem.** Motion is allowed only after an explicit arming step; a jog that arrives while disarmed must be refused, the robot must
stop when the console goes silent (a dead-man's handle), and a second motion session must never open on the link. **Built by hand:**
a state machine on each side kept in sync by review, guards in every handler, heartbeat timers, lock tables.

**With AdHoc** the conversation is a state machine in the description, and its timing is attributes on its states:

```csharp
interface Control : Connects<Console, Robot>
{
    interface Motion : Actor
    {
        int Actor.MaxActiveInstances => 1;                  // one motion session per link

        [L____________<Arming, Console.Arm>]                 // only an Arm leaves Safe
        struct Safe { }

        [Deadline(3)]                                        // the robot answers within 3 s
        [____________R<Armed, Robot.ArmedAck>]
        [____________R<End, Robot.Refused>]
        struct Arming { }

        [Idle(1)]                                            // dead-man lease: held by the Robot
        [l____________<(Console.Jog, Console.Heartbeat)>]
        [____________r<Robot.Pose>]
        [L____________<End, Console.Disarm>]
        struct Armed { }
    }

    (L____________, Robot.Status) status(Console.StatusQuery q);   // a one-shot read next to the workflow
}
```

- **Both hosts run the same machine.** Each state has a generated transmitter with a send method only for the packs this host may send
  there: on the console, `Safe`'s transmitter has a send for `Arm` and none for `Jog`.
- **Each state guards its packs.** A `Jog` sent anyway is refused before it leaves (`DISCARDED_BYSTATE`); an unexpected id on arrival
  goes to the receiver's error handler. Only the side that leads a state moves it on, and a 4-bit epoch drops a late pack.
- **Timers per instance.** `[Deadline(3)]` limits one instance's stay in `Arming`. `[Idle(1)]` holds on the robot, which does not lead
  `Armed`: a second without `Jog` or `Heartbeat` fires `OnDeadline`, where your code stops the axes. The connection lives on.
- **One session per link is a number.** With `MaxActiveInstances => 1`, a second local `Acquire` returns `null`, and a peer that opens
  one instance more is treated as hostile: the connection is aborted. The limit is per connection; a rule across consoles is your code
  in the `Check limit` injection point.

**Why it is good.** The workflow is a reviewed artifact in the description, enforced on both sides; both replicas move on the same pack
with no sync message. A timer costs one field per instance (two with `[Idle]`) and no timer object. A flow change is a protocol
change, and the `.cs~` impact report names exactly the hosts it reaches.

**Limits.** Timers are in seconds, and `[Idle]` holds on the side that does not lead the state - decide who leads. Actors on one
connection are independent: "only after login" is a chain in the connection's own actor or your rule in the `Check limit` injection
point. A protocol layer complements a certified hardware safety chain; it does not replace one.

→ MANUAL: [States](MANUAL.md#states-the-phases-of-a-conversation) ·
[The side that leads a state](MANUAL.md#the-side-that-leads-a-state) ·
[What the generator produces for actors and states](MANUAL.md#what-the-generator-produces-for-actors-and-states) ·
[Deadline and Idle](MANUAL.md#per-instance-timers-deadline-and-idle) ·
[Opening an instance and its limit](MANUAL.md#opening-an-actor-instance-and-its-limit) ·
[Your code lives in the actor](MANUAL.md#your-code-lives-in-the-actor) ·
[A real state machine](MANUAL.md#a-real-state-machine-adhocs-communication-connection)

### R15. Ingest straight into your own data structures

*Where it fits: storage engines, metrics stores and brokers with their own rows, columns and logs; test benches filling ring buffers;
gateways with little RAM moving camera clips and crash dumps for hundreds of connections.*

![Two rows. Top, dim: the pattern to avoid - a socket buffer goes through a parser into a message object on the heap (copy 1), then through a mapper into your structures (copy 2), next to a memory bar the size of the whole message. Below, an abstract host: a Block streams through a 1 KB receive buffer - one fill holds the sequence and window 1 of the samples, a dashed next fill brings window 2 - and the generated parser calls your setters, so the data lands straight in your ring buffer and your columns, while a crossed-out generated Block object is never created. Right: Capture.raw, a Stream drawn as length-prefixed chunks ending in a zero length, flowing into your file sink, its length unknown at the start and its size cap checked on both sides. Bottom strip: a 1 KB send buffer and a 1 KB receive buffer per connection plus a small parser state, whatever the pack size; a full sink stops socket reads, so unread data waits in the network; 64 states of 3 bits take 24 bytes, not 64](docs/img/readme-ingest-direct.svg)

**The problem.** The network layer parses each message into an object, then your code copies it into the structures it really uses:
two copies per message and two models to keep in sync. Big payloads are built whole in memory, and a small box runs out of RAM.
**Built by hand:** message classes plus mappers, chunking schemes ("part 17 of 312"), a "max message size" tuned per deployment.

**With AdHoc** you mark the host abstract - `-` after its language marker (`--` also drops the equality and hash methods) - and every
pack becomes an interface over your own objects:

```csharp
/// <see cref='InJAVA'/>--                       // abstract: your code holds the data
struct Engine : Host { }

class Block
{
    ulong                        sequence;
    [D(4096)] short[]            samples;        // exactly 4096 samples: no length on the wire
    [D(64), MinMax(0, 7)] byte[] channel_state;  // 8 values: 3 bits per item, bit-packed
}

class Capture                                    // open-ended: the length is not known when it starts
{
    ulong                    sequence;
    [S(500_000_000)] Stream  raw;                // capped on both sides
}
```

- **Field by field into your objects.** The parser calls your setters as it decodes the receive buffer, the serializer your getters as
  it fills the send buffer. No generated object, no copy; a factory per received pack in the generated `_Allocator` picks your object.
- **Arrays come as windows** of the receive buffer: one-byte elements as the buffer itself, `samples` in whole elements, the 3-bit
  states in whole groups of eight. Your code copies each window straight into its ring buffer, column or file.
- **Open-ended data is a Stream.** `Capture.raw` starts before its length is known; the runtime reads your source into the free part of
  the send buffer and writes what arrives into your sink.
- **Memory is two buffers per connection** (C# default 1024 bytes; 1-8 KB is usual) plus a small parser state, whatever the pack size.
  A full sink stops socket reads: unread data waits in the network, not in memory.

**Why it is good.** One model, nothing converted twice. The same pack can be abstract on the engine and concrete on a new client, with
the same bytes on the wire. Memory sizing becomes "connections × two buffers": no buffer sized for the largest message, no chunking to
write. And a pack in a file or a database column is the same bytes as on the wire.

**Limits.** Handlers and stream sources run on the connection's loops: a source answers `Read` promptly, heavy work goes to another
thread. Assign the factory of every abstract received pack before the first one arrives - the default throws.

→ MANUAL: [Abstract: your code holds the data](MANUAL.md#abstract---your-code-holds-the-data) ·
[Choosing + or -](MANUAL.md#choosing--or---generated-objects-or-your-own-structures) ·
[One buffer per direction](MANUAL.md#the-runtime-model-one-buffer-per-direction) ·
[Memory on the serialization path](MANUAL.md#memory-on-the-serialization-path) ·
[Arrays](MANUAL.md#arrays-constant-fixed-and-dynamic-length) ·
[Sources, sinks and the end of a stream](MANUAL.md#a-conduit-at-runtime---sources-sinks-and-the-end-of-a-stream) ·
[The size cap of a conduit](MANUAL.md#the-size-cap-of-a-conduit---sn)

### R16. An internal cluster protocol and a server mesh on the same port

*Where it fits: brokers replicating to brokers, cluster nodes gossiping, chat and game servers scaled out to several nodes.*

![Three processes A, B and C in a triangle, each the same generated module of host Node, joined pairwise by InterNode sockets; at each socket the dialing end is marked L, dialed, and the accepting end R, accepted - A dialed B and C dialed A and B, so A is Left on one socket and Right on another. On the A-B socket Append flows from A to B and Ack flows back, and the actor-instance id space is split into A's half and B's half, once, when the socket connects, with nothing added per pack. On A's outer side its one port, NodePort, reads the first byte: a TypeScript Client arrives with Client.uid and lands on ClientNode, another Node arrives with Node.uid and lands on InterNode. At C a RoomEvent from a client fans out to C's local room group, and your code forwards it over InterNode to A and B, which hold room groups of their own](docs/img/readme-cluster-mesh.svg)

**The problem.** Nodes of one program must talk to each other. The peer protocol is written twice - for the node that dials and the
node that accepts - and runs on a second port; conversations opened at both ends of one socket collide on their ids. **Built by
hand:** invented "primary/replica" roles, a second listener with its own firewall rule and certificate.

**With AdHoc** you connect the host to itself and put clients and peers behind one port:

```csharp
/// <see cref='InJAVA'/>
struct Node : Host
{
    public class Append { long offset; [D(+4096)] string record; }
    public class Ack    { long offset; }
}

interface InterNode : Connects<Node, Node>                // one host type talks to itself
{
    [ReceiveTimeout(30)]                                   // a silent peer is noticed
    [l____________<Node.Append>]                           // the node that dialed replicates
    [____________r<Node.Ack>]                              // the node that accepted acknowledges
    struct Replicating { }
}

interface ClientNode : Connects<Client, Node> { /* the client protocol */ }
interface NodePort   : Multiplex<(ClientNode, InterNode)> { }   // byte Client.uid -> ClientNode, byte Node.uid -> InterNode
```

- **One module, both ends.** One host is generated; it sends and receives every pack of `InterNode`. The node that dialed is the left
  side, the one that accepted the right side, so states, RPC roles and header directions keep their meaning.
- **No id collisions.** When the socket connects, each end takes its own half of the actor-instance id space; nothing is added per pack.
- **One port.** A socket on the port opens with one byte, the dialing host's uid: `Client.uid` picks `ClientNode`, `Node`'s own uid picks
  `InterNode`. Paid once per socket, never per pack.
- **Liveness and parallel work.** Your `OnReceiveTimeout` may send a heartbeat and keep waiting instead of closing. A swarm actor
  (`MaxActiveInstances => N`) carries up to N concurrent transfers per peer link, each with its own `[Deadline]`.

**Why it is good.** The peer protocol is written once and generated once, with roles taken from who dialed. Clients and peers share one
listener and one certificate, each on its own typed connection. A chat node fans a room event out to its local members
([R18](#r18-chat-and-collaboration-rooms-presence-turn-taking)) and forwards it to the other nodes over `InterNode`.

**Limits.** Only a physical `Connects<>` joins a host to itself. Do not invent a second host type to play the caller: two generated
modules for one process share class names. Groups live in one process, so forwarding between nodes is your code.

→ MANUAL: [Connecting a host to itself](MANUAL.md#connecting-a-host-to-itself) ·
[The first byte of a multiplexed socket](MANUAL.md#the-first-byte-of-a-multiplexed-socket) ·
[Connection timeouts](MANUAL.md#connection-timeouts-transmittimeout-and-receivetimeout) ·
[Concurrency modes](MANUAL.md#concurrency-modes) · [Grouped actors](MANUAL.md#actors-of-many-connections-grouped)

### R17. A metadata-routed, payload-blind middle tier: audit and tracing

*Where it fits: audit trails and evidence stores, tracing, per-tenant routing and archiving - any tier that files a message by its
metadata and must keep it exactly as it was sent.*

![One AuditRecord on the wire as a strip: the pack id, the AuditMeta header bit-packed as trace_id 64, tenant 32 and severity 3 bits - 99 bits in 13 bytes - the 4-byte timestamp, a null-bits byte, and the payment as hatched opaque bytes in length-prefixed chunks ending in a zero length; a dashed line after the header marks where OnReceiving runs, before the payload is parsed. Below, three hosts: the Service holds a typed Payment card and sends AuditRecord; the Auditor, with no Payment type and no parser, lets your code pick a shelf by tenant and drops the unchanged hatched bytes into it; the same bytes travel as Evidence, FromStream, to the Investigator, whose generated parser turns them back into a typed Payment card; a bracket joins the two cards: the same bytes, the same Payment, byte-faithful evidence](docs/img/readme-audit-headers.svg)

**The problem.** Every message must reach the audit log with its trace id and tenant. The audit service parses each message to find the
tenant and re-serializes it for storage - and what it stores is no longer byte for byte what was sent. **Built by hand:** a text
envelope parsed per message, trace context copied into every message type, an auditor rebuilt with every schema change.

**With AdHoc** three capabilities combine - a pack header for the metadata, a trim that lets the auditor keep the payload as raw bytes,
and a compact `DateTimeDef`. Each part is a MANUAL pattern; this recipe joins them.

```csharp
class Seconds50Years : DateTimeDef                         // any second of 2000-2050 in 4 bytes
{
    public DateTime min       => new(2000, 1, 1);
    public DateTime max       => new(2050, 1, 1);
    public TimeSpan precision => TimeSpan.FromSeconds(1);
}
interface FromService : IfSendingFrom<Service, ServiceToAuditor> { }
interface FromAuditor : IfSendingFrom<Auditor, AuditorToInvestigator> { }

public class AuditRecord { Seconds50Years at; [ToStream<FromService>]   public Payment payment; }   // the auditor keeps bytes
public class Evidence    {                    [FromStream<FromAuditor>] public Payment payment; }   // replayed, typed again

interface ServiceToAuditor : Connects<Service, Auditor>
{
    class AuditMeta : HeaderFor<AuditRecord>                // metadata, read before the payload
    {
        public ulong        trace_id;
        public uint         tenant;
        [MinMax(0, 7)] byte severity;                       // 3 bits
    }

    [l____________<AuditRecord>]
    struct Start { }
}
```

- **Metadata outside the pack.** Header fields live on the connection: `send` takes their values before the connection argument, so
  the packs carry no trace fields and one pack object can go out with different metadata.
- **Read first.** The wire carries the pack id, the header bits (64 + 32 + 3 = 99 bits, rounded up to 13 bytes), then the payload. The
  auditor's `OnReceiving` hook runs before the payload is parsed: your code reads the tenant and the trace id there.
- **Kept as bytes.** When the payment's turn comes, your code returns a sink - that tenant's file or row - and the runtime feeds it the
  bytes as they arrive. Chunk framing marks the end without parsing.
- **Replayed as bytes.** For `Evidence` the auditor supplies the stored bytes, and the investigator rehydrates a typed `Payment`: exactly
  what the service sent.

**Why it is good.** Tracing and tenancy ride on every audited pack without touching the pack types, and header bits cost exactly their
width. The audit tier is schema-blind and byte-faithful: no re-encoding can alter evidence. Its generated code holds no `Payment` type,
so `Payment` can gain fields without the auditor being touched or rebuilt (the bytes already stored keep their old layout).

**Limits.** Header fields are single, non-nullable, fixed-size primitives (`[MinMax]` allowed), named uniquely across all headers;
received header objects are views over the receive buffer - copy what you need. The auditor must take `AuditRecord.payment` raw on
every leg it receives it over (one wire form per host and direction). Stored bytes decode only with the layout they were written in:
freeze `Payment` while its archive must stay readable.

→ MANUAL: [Pack headers](MANUAL.md#pack-headers-headerfor) ·
[What the generator produces for headers](MANUAL.md#what-the-generator-produces-for-headers) ·
[Headers on the wire](MANUAL.md#headers-on-the-wire) · [Rules for header fields](MANUAL.md#rules-for-header-fields-and-names) ·
[The three trims](MANUAL.md#the-three-trims-and-who-holds-what) · [Store and replay](MANUAL.md#store-and-replay-with-trims) ·
[One wire form per host and direction](MANUAL.md#one-wire-form-per-host-and-direction) ·
[DateTimeDef](MANUAL.md#datetimedef-a-moment-within-a-declared-range) · [Trim pitfalls](MANUAL.md#trim-pitfalls)

### R18. Chat and collaboration: rooms, presence, turn-taking

*Where it fits: chat rooms and support desks, collaborative editors with a review step, multiplayer lobbies - any app where people join
shared spaces and take turns.*

![Left, the Hub: Connection.Groups lists the rooms #general and #design, each a chain of member instances linked by prev and next, with the group's own lock at its head; one Said at the head of #general fans out into one queued copy per member, because a send only queues and a slow member sets nobody's pace. Member M3's instances are drawn leaving both rooms toward a note: an instance leaves its room by itself when it ends - through Leave in its state machine, through Idle(60) after 60 s without Say, Typing or Here, or when its session ends after the parked wait that follows a lost socket - Release, no ghosts, no sweeper. Right: the Edit state machine - Editing, led by the member, stays on TextInsert, TextDelete and CursorMove from both sides and goes to Reviewing on FinalizeDoc; Reviewing, led by the hub, returns on Approved or NeedsChanges; a crossed-out TextInsert in Reviewing is refused before it leaves with DISCARDED_BYSTATE. Bottom strip: Say is a guaranteed leg, delivered once and in order across a lost socket; Typing is not journalled; with Resumable(10) an identified member whose socket drops is parked and keeps its rooms](docs/img/readme-chat-rooms.svg)

**The problem.** Rooms, presence, typing indicators, edits from both sides, then a review step where nobody may edit. After a disconnect
users stay online as ghosts, and messages are lost or duplicated on reconnect. **Built by hand:** membership sets with cleanup on
disconnect, presence sweepers, acknowledgements and de-duplication, "is it under review?" checks in every handler.

**With AdHoc** rooms are a grouped actor with a lease, turn-taking is a state machine, and messages are a guaranteed leg:

```csharp
[Resumable(10)]                                            // a dropped member is parked for 10 minutes, not removed
interface Talk : Connects<Member, Hub>
{
    // the login (states of the connection itself, whose handler calls identify or reclaim) is left out
    [Idle(60)]                                             // presence: a member silent for 60 s is let go
    [Grouped]                                              // the hub gathers members of all connections into rooms
    interface Room : Actor
    {
        int Actor.MaxActiveInstances => 8;                 // up to 8 rooms open per member

        [L____________<Inside, Member.Join>]
        struct Start { }

        [l____________<(Member.Say, Member.Typing, Member.Here)>]   // the member's packs renew the lease
        [____________r<(Hub.Said, Hub.Presence)>]                   // the hub's pushes do not
        [L____________<End, Member.Leave>]
        struct Inside { }
    }

    interface Edit : Actor                                 // turn-taking
    {
        int Actor.MaxActiveInstances => 5;

        [_____lr_____<(TextInsert, TextDelete, CursorMove)>]   // both sides edit
        [L____________<Reviewing, Member.FinalizeDoc>]         // only the member finalizes
        struct Editing { }

        [____________R<Editing, (Hub.Approved, Hub.NeedsChanges)>]   // only the hub decides
        struct Reviewing { }
    }

    interface FromMember : Resumable<Member, Member.Say> { }       // messages arrive once, in order; Typing is not journalled
}
```

- **Rooms across connections.** The `Join` handler puts the instance into its room's group (O(1), no allocation); `Connection.Groups`
  lists the rooms. A walk over a room queues one `Said` per member - a send only queues, so a slow member sets nobody's pace.
- **Presence is a lease.** `[Idle(60)]` holds on the hub, because the member leads `Inside`. Only the member's own packs renew it, so the
  hub's pushes never keep a vanished member alive.
- **No ghosts.** An instance leaves its group by itself whenever it ends - `Leave`, `[Idle]`, its connection closing. An identified
  member whose socket drops is parked and keeps its rooms; one that is not back within 10 minutes ends and leaves every room.
- **Turns are enforced.** In `Reviewing` the generated code refuses a `TextInsert` before it leaves (`DISCARDED_BYSTATE`). Once the
  login has identified the member, every `Say` arrives once and in order, across lost sockets too; typing indicators skip the journal.

**Why it is good.** Membership and presence are lifecycle facts the runtime already knows: no membership table next to the actors, no
sweeper, no cleanup on disconnect. Review rules live in the description and both sides enforce them. Reliability is chosen per pack.

**Limits.** Groups live in one process - a chat spread over several nodes forwards between them
([R16](#r16-an-internal-cluster-protocol-and-a-server-mesh-on-the-same-port)). A transition always belongs to one side: `_____lr_____`
gives both sides stay packs only. `SwapHosts` copies only a connection's own states, not its named actors: a bot that plays the member's
part declares `Room` and `Edit` in its own connection.

→ MANUAL: [Worked state machine examples](MANUAL.md#worked-state-machine-examples) ·
[Grouped actors](MANUAL.md#actors-of-many-connections-grouped) ·
[Deadline and Idle](MANUAL.md#per-instance-timers-deadline-and-idle) ·
[How an actor instance ends](MANUAL.md#how-an-actor-instance-ends) ·
[Keeping a session across a lost connection](MANUAL.md#keeping-a-session-across-a-lost-connection) ·
[Declaring a guaranteed leg](MANUAL.md#declaring-a-guaranteed-leg) ·
[Composing connections and SwapHosts](MANUAL.md#composing-connections-and-swaphosts)

### R19. Live video and audio that stop for an urgent command

*Where it fits: drones and robots streaming video to an operator · intercoms and field cameras · any endless media feed that shares
one link with commands and alerts.*

![A timeline of one connection, time running left to right, between a Drone (C#) on the left and an Operator (TypeScript) on the right. Upper lane, Drone to Operator, one pack at a time: VideoSegment packs drawn as runs of chunk cells, each closed by a zero terminator. During the second segment send(Alert) is queued; the next Read returns 0, so the segment ends early with an orange terminator, and about one socket buffer later the orange Alert goes out; then a new VideoSegment carries the feed on. Lower lane, Operator to Drone, on its own loop: Command packs and an urgent orange Land cross while a segment is still going the other way. Notes: a segment is a named Stream pack with [S(64_000_000)], chunked, its length unknown up front, ending at any byte; the feed has no end, segment follows segment, cut where the decoder can resume (a frame or a group of frames); memory stays two socket buffers per connection however long the flight.](docs/img/readme-media-pause.svg)

**The problem.** A drone streams video to its operator over one link. The same link must carry the drone's alerts - battery low, an
obstacle - the moment they happen, and the operator's commands - land, return home - at once. An endless stream blocks everything behind
it, so teams open a second channel for control and keep the two in step. **Built by hand:** a framing layer that cuts the video into
chunks, a priority scheme that slips control messages between them, reassembly on the other side - or a separate control socket with
its own reconnect logic.

**With AdHoc** the feed is a sequence of `Stream` segments, and a segment ends whenever your source says so:

```csharp
/// <see cref="InCS"/>
struct Drone : Host
{
    [S(64_000_000)] public class VideoSegment : Stream { }  // one segment of the endless feed, at most 64 MB

    public class Alert                                       // urgent: goes out between two segments
    {
        byte code;
        [MinMax(0, 100)] byte battery;
    }
}

/// <see cref="InTS"/>
struct Operator : Host
{
    public class Command { [MinMax(-100, 100)] sbyte pitch; [MinMax(-100, 100)] sbyte roll; }
    public class Land { }                                    // urgent command
}

interface Link : Connects<Operator, Drone>
{
    [l____________<(Operator.Command, Operator.Land)>]
    [____________r<(Drone.VideoSegment, Drone.Alert)>]
    struct Flying { }
}
```

- **An endless feed in bounded segments.** `VideoSegment` is a named `Stream` pack: chunked, its length unknown up front, at most
  64 MB per segment. Segment follows segment for as long as the flight lasts, each moving through the same socket buffer - no segment
  is ever held whole in memory.
- **Stop at any byte.** A `Stream` ends whenever its source says so: the next `Read` returns 0, the runtime writes the two-byte
  terminator, and the receiver keeps every byte of the segment. The transmitter writes one pack at a time, so the pack queued behind
  the segment - the `Alert` - goes out next, within about one socket buffer of bytes. The next `VideoSegment` carries the feed on.
- **Commands never wait for video.** Receiving and transmitting run on separate loops: `Land`, going from the operator to the drone,
  travels the other way and is never queued behind a segment.
- **The receiver sees whole things.** Each segment pours into a sink of yours - a decoder, a recorder - and closes with an ordinary end
  of stream; each `Alert` arrives as a typed pack between two segments, in the order it was sent.

**Why it is good.** Video, alerts and commands share one connection, one session and one state machine: no second socket, no
priority layer, no reassembly code. However long the flight, the connection holds two socket buffers.

**Limits.** A segment is not paused and continued as the same pack: it ends, and the feed continues in the next segment - so cut
segments where your decoder can take up again, at a frame or a group of frames. A source must not wait for the encoder: a `Read` that
waits holds the whole sending direction; when nothing is ready, end the segment. Guarantee the alerts and commands, not the video: a
guaranteed pack is copied into the sender's journal.

→ MANUAL: [Pausing an endless stream for urgent packs](MANUAL.md#pausing-an-endless-stream-for-urgent-packs) ·
[Stream framing](MANUAL.md#stream-framing---chunked-and-interruptible) ·
[A conduit at runtime](MANUAL.md#a-conduit-at-runtime---sources-sinks-and-the-end-of-a-stream) ·
[Named conduit packs](MANUAL.md#named-conduit-packs) ·
[Threads: the receive loop and the transmit loop](MANUAL.md#threads-the-receive-loop-and-the-transmit-loop)

### R20. One pack, many recipients, no copies

*Where it fits: an event for every subscriber · a setting for every device of a model · a game tick for every player · any message
your host makes once and sends to many.*

![Your code fills one Tick object once and sends it on every subscriber connection. Each of four connections holds only a reference to that one object in a slot of its sending queue - dashed yellow lines lead back to the object; its transmitter writes the object into its own send buffer with its own cursor, each at a different point, and on to Viewer 1 to Viewer 4. The object is never copied. Notes: send puts a reference in a queue and serializes nothing yet; keep the object unchanged until every connection has reached OnSerialized; a Value Pack is a number, so each queue slot gets its own copy.](docs/img/readme-fanout-one-object.svg)

**The problem.** The same message goes to hundreds of peers. Serializing it once into a byte array and copying that array into every
socket costs memory per recipient; serializing it per recipient by hand duplicates work and code. **Built by hand:** a pre-encoded
buffer with a reference count, or a copy per peer.

**With AdHoc** you make one pack object and send it on every connection:

```java
Tick tick = new Tick();                                    // one object, filled once
for( Watch w = WATCHERS.head; w != null; w = w.next )      // a [Grouped] actor gathers the subscribers of all connections
    Watch.State.Live.ONE.transmitter.send( tick, w.connection, w );
```

- **Queued by reference.** `send` puts a reference to the pack into each connection's queue and returns; nothing is serialized or
  copied at that moment.
- **Read N times, copied never.** When a socket has room, its transmitter writes the pack straight from the object into its own send
  buffer with its own cursor. One object serves every connection, however many there are.
- **Slow peers slow nobody.** Each connection drains its own queue at its own pace; a walk over the subscribers only queues.

**Why it is good.** Memory per recipient is one queue slot, not a copy of the message; the code is a loop of `send`.

**Limits.** The object is shared until the last connection has written it: do not change or reuse it before every connection has
reached the pack's `OnSerialized` event - make a new object per event. A pack whose getters read a database cursor, or a pack with a
`Stream` field, is read once and cannot be shared. A Value Pack is a number: each queue gets its own copy of it, which costs nothing.

→ MANUAL: [Fan-out of a typed pack](MANUAL.md#fan-out-of-a-typed-pack-one-object-many-queues) ·
[Sending: the transport pulls the bytes](MANUAL.md#sending-the-transport-pulls-the-bytes) ·
[Actors of many connections: Grouped](MANUAL.md#actors-of-many-connections-grouped)

### R21. Receive, store and broadcast in one pass

*Where it fits: a monitoring backend that records events and shows them live · a market-data hub · a chat server that persists and
delivers · any relay host that keeps what it receives and passes it on.*

![A Source sends packs on a guaranteed leg into the Monitoring host. In one pass over the receive buffer the decoder calls your setters, which put the values straight into the database command parameters - the insert runs in OnReceived - and the consumed bytes are captured once into a pooled BytesCopy.InMemory that holds the pack body, with three references. After the insert, Broadcast(receiver) queues the one capture on every subscriber; each subscriber's transmitter copies it with its own cursor to Observer 1, 2 and 3, while Observer 4, whose queue is full, misses this event, stays subscribed, and nobody waits. No subscriber - no capture; the pack is never encoded again. A timeline below: pack arrives, decode and capture, OnReceived insert, Broadcast, each subscriber sends, the last one returns the capture to the pool. Store, then forward.](docs/img/readme-store-broadcast.svg)

**The problem.** A host receives events from sources, writes each into its database and pushes it to live subscribers. Done naively it
decodes the event into an object, copies the values into the database command, then encodes the object again - once per subscriber.
**Built by hand:** a decoder, a mapping layer to the database, an encoder, and a fan-out queue with its own memory management.

**With AdHoc** the bytes of an incoming pack are captured while it is decoded, and that one capture is what the subscribers get. This is
how AdHoc's own monitoring backend works (C#):

```csharp
_Allocator.DEFAULT.new_Monitoring_Sessions_Authorizer_CheckAttempt = receiver =>
{
    Feed.ONE.PrepareBroadcastIfAnySubscriber(receiver, CheckAttempt.__id_);   // capture only while someone is subscribed
    return pool.Acquire();                                                     // an abstract pack: its setters fill the DB command
};

// when the pack has arrived
try     { insert.ExecuteNonQuery(); }                     // the values are already in the command's parameters
catch( Exception e ) { Console.WriteLine(e.Message); }
finally { Feed.ONE.Broadcast(conn.receiver); }            // one capture, queued on every subscriber
```

- **One pass over the bytes.** The pack is [without implementation](#packs-without-implementation-straight-into-your-own-structures):
  the decoder calls setters that write straight into the database command's parameters, and every slice of the receive buffer it
  consumes is also copied into the capture. No object, no mapping layer, no second decode.
- **Never encoded again.** `Broadcast` hands the captured bytes to every subscriber's queue; each transmitter copies them into its own
  send buffer with its own cursor. A reference count returns the capture to its pool after the last send.
- **Nothing spent when nobody listens.** With no subscriber, no capture is made.
- **A slow subscriber misses, nobody waits.** A broadcast never blocks: a subscriber whose queue is full skips that event and stays
  subscribed; the source and the other subscribers go on. Make the leg *into* the host a guaranteed leg, and the store never misses
  anything.

**Why it is good.** Decode once into the store, encode never: the cost per subscriber is one copy of bytes into a socket buffer.

**Limits.** Subscribers get a pack after it has fully arrived and been stored - store, then forward. The capture holds the whole body of
the pack in one pooled array until the slowest subscriber has sent it. The subscribers' connection must carry the same pack (its own
headers are fine). Call `Broadcast` in `finally`: it also clears the capture slot. Packs with `Stream` fields or transform chains are not
captured; a Value Pack is fanned out with a loop of sends.

→ MANUAL: [Broadcast of a received pack](MANUAL.md#broadcast-of-a-received-pack-the-bytes-captured-once) ·
[One pack to many connections](MANUAL.md#one-pack-to-many-connections) ·
[Implementation modifiers](MANUAL.md#implementation-modifiers-who-holds-the-data) ·
[Declaring a guaranteed leg](MANUAL.md#declaring-a-guaranteed-leg)

### R22. Serial links that recover after a damaged byte

*Where it fits: UART and RS-485 links to controllers and sensors · radio modems · any byte stream with no framing of its own.*

![A serial line that recovers after a damaged byte. Top: one frame per pack - the mark FF, the pack id and fields as on any connection, an escaped 7F where a byte 0x7F or 0xFF occurred, the escape costing one bit because the top bit moves on, and a 2-byte CRC-16. Middle: on the line, frames Status, Command, Status and Alarm, each starting with FF; a lightning bolt damages a byte of Command - CRC_ERROR, dropped - and the receiver skips to the next FF, so the next Status arrives intact, CRC ok, dispatched. Bottom: receiving runs UART bytes, Receiver.Framing, the generated receiver, a CRC gate and OnReceived - unescaped in place, decoded field by field as the bytes come, handlers run only when the CRC matches; sending runs send(pack), the generated transmitter, Transmitter.Framing and UART bytes, with the frame mark, the escaping and the CRC written in place in the same buffer. C# and Java; the description, the actors and the packs are the same as on TCP.](docs/img/readme-serial-framing.svg)

**The problem.** A serial line delivers bytes, not messages. Noise flips a byte, a buffer overrun drops one, and a protocol without
markers loses its place: everything after the fault is garbage until someone resets the link. **Built by hand:** a start byte and byte
stuffing, a CRC, a resynchronization state machine, and a decoder that must not act on a message whose CRC has not arrived yet.

**With AdHoc** two classes of the runtime sit between the generated connection and the port:

```csharp
var rx = new AdHoc.Connection.Receiver.Framing(connection.receiver);        // in front of the generated receiver
var tx = new AdHoc.Connection.Transmitter.Framing(connection.transmitter);  // behind the generated transmitter

int n = port.Read(inBuffer, 0, inBuffer.Length);  if (0 < n) rx.Write(inBuffer, 0, n);
int m = tx.Read(outBuffer, 0, outBuffer.Length);  if (0 < m) port.Write(outBuffer, 0, m);
```

- **One frame per pack.** The mark `0xFF`, the pack's bytes, a CRC-16. A byte `0x7F` or `0xFF` inside is written as `0x7F` and its top
  bit moves into the next byte: an escape costs one bit, and a frame grows by at most one byte in eight.
- **Decoded as it arrives, dispatched only when intact.** The pack is decoded field by field as its bytes come, but its `OnReceived`
  handlers run only after the CRC matches. A broken pack never reaches your code.
- **Recovery at the next mark.** A damaged byte costs the damaged pack: the receiver drops the frame, skips to the next `0xFF` and takes
  the next pack normally. Errors - `CRC_ERROR`, `BYTES_DISTORTION` - go to a handler you can replace.
- **Nothing else changes.** The description, the actors, the states and the packs are the ones the host uses over TCP; only the bytes on
  the line look different. Framing works in place, in the same buffers.

**Why it is good.** A serial link gets the same protocol, the same generated code and the same state machine as a TCP link, and framing,
CRC and resynchronization are a few lines of wiring instead of a project.

**Limits.** C# and Java only: TypeScript hosts run where TCP or WebSocket carry the bytes. The CRC detects damage and does not repair it:
a dropped pack is gone unless it is on a guaranteed leg. Every pack pays its frame - a mark and two CRC bytes.

→ MANUAL: [Serial links and fast recovery: Framing](MANUAL.md#serial-links-and-fast-recovery-framing) ·
[The connection at runtime](MANUAL.md#the-connection-at-runtime) ·
[Declaring a guaranteed leg](MANUAL.md#declaring-a-guaranteed-leg)

## Capabilities

Each capability below is a few declarations in the description, or a property of the runtime that the generated code runs on. Every
section says what the capability is, what it gives you and where it acts, and links to the chapter of [MANUAL.md](MANUAL.md) that
explains it in full. The [ready solutions](#ready-solutions) above show them working together.

![Where each capability acts: a ladder of rungs - pipeline, project, host, connection, actor-state-branch, pack, field, wire bytes, runtime - with numbered capability chips beside the rung they act on. Pipeline: 1 One description, 18 Impact report, 20 AdHocAgent, 21 Deployment, 22 AdHoc Observer. Project: 16 Identity, 19 Reuse, 5 Composing and Pack Sets. Host: 6 Hosts, 17 Evolution, 15 Multiplexing. Connection: 8 Sessions and UDP, 14 Virtual connections, 12 Transform chains. Actor-state-branch: 7 Conversations. Pack: 3 Data, 13 Trims. Field: 4 Designed bytes, 11 Streams. Wire bytes: 10 Serial links - a frame and a CRC per pack, also at the connection - beside the note that every declaration above ends here - a pack id, then fields known by position. Runtime, two buffers per connection: 2 Runtime engine, 9 Fan-out and broadcast - one pack, many connections. Colours group them: describe and generate, data and bytes, conversation and transfer, runtime.](docs/img/readme-capability-map.svg)

| # | Capability | What it gives you |
|:--|:--|:--|
| 1 | [One description](#one-description-the-whole-system-in-c) | the whole system in one C# file; the code of every host, in each of its languages |
| 2 | [The runtime engine](#the-runtime-engine-bytes-pulled-through-two-buffers) | two fixed buffers per connection; bytes pulled by the socket; no locks within a direction |
| 3 | [Data](#data-packs-types-constants-your-own-metadata) | types for real data, each with a declared maximum; constants and metadata that never travel |
| 4 | [Designed bytes](#designed-bytes-encodings-that-follow-your-declarations) | ranges, distributions and enums become the fewest bits; a small pack becomes one integer |
| 5 | [Composing packs and Pack Sets](#composing-packs-and-pack-sets-define-once-select-by-rule) | a field defined once, wherever it is used; one selection language for every rule |
| 6 | [Hosts](#hosts-generated-objects-or-your-own-structures) | generated objects or your own structures, per host, language, pack and field |
| 7 | [Conversations](#conversations-connections-actors-states-rpc) | the same state machine on both ends; RPC in one line; many conversations on one socket |
| 8 | [Sessions, guaranteed delivery, UDP](#sessions-that-outlive-sockets-guaranteed-delivery-udp) | a session that outlives its socket; chosen packs once and in order; UDP with guarantees |
| 9 | [Fan-out and broadcast](#fan-out-and-broadcast-one-pack-many-connections) | one pack to many connections with no copy per peer; a received pack passed on without encoding it again |
| 10 | [Serial links](#serial-links-framing-that-recovers-after-a-damaged-byte) | UART, RS-485 and radio links: a frame per pack, a CRC, recovery at the next mark |
| 11 | [Streams](#streams-any-size-through-fixed-buffers) | payloads of any size, even endless ones, through the same two buffers |
| 12 | [Transform chains](#transform-chains-compression-and-encryption-as-attributes) | compression and encryption as attributes: on a field, a pack, a link or end to end |
| 13 | [Trims](#trims-stores-that-keep-bytes-they-never-parse) | stores that keep and replay bytes they never parse |
| 14 | [Virtual connections and relays](#virtual-connections-and-relays-middle-tiers-that-cannot-read) | hosts that talk as if wired, through relays that cannot read |
| 15 | [Multiplexing](#multiplexing-one-port-many-peers-many-generations) | many kinds of peer and many generations behind one port, one byte per socket |
| 16 | [Identity](#identity-numbers-instead-of-field-tags) | rename and move freely: entities are known by numbers, not by names |
| 17 | [Evolution](#evolution-a-new-generation-is-a-new-host) | a new generation is a new host; the old one stays frozen |
| 18 | [Impact report](#impact-report-which-hosts-an-edit-reaches) | which hosts an edit reaches, before anything is generated |
| 19 | [Reuse](#reuse-extend-import-modify) | build on another description without forking it |
| 20 | [AdHocAgent](#adhocagent-one-command-line-tool-for-every-task) | one command-line tool: local checks, generation, pipelines |
| 21 | [Deployment](#deployment-routes-smart-merge-backups) | generated files routed into your projects; your code kept; everything backed up |
| 22 | [AdHoc Observer](#adhoc-observer-the-description-as-live-diagrams) | the description as live, clickable diagrams |

### One description: the whole system in C#

*Where it sits: the pipeline - **description** → AdHocAgent → generator → **code per host and language** → deployment*

One C# file holds the data, the programs, their conversations and the rules of transfer, and AdHoc generates the network code of every
program from it. The pipeline is drawn in [How AdHoc is organized](#how-adhoc-is-organized), and the case for generated code is made in
[Why generate code from a description](#why-generate-code-from-a-description). This section is the vocabulary, and what comes back.

![One model for the whole conversation: your C# protocol description holds four cards - Data (packs, fields, types and limits), Participants (hosts and languages, connections), Behaviour (actors, RPC, states and branches), Transfer rules (sessions, streams, tunnels, shared ports). Through AdHocAgent and the cloud generator it becomes generated protocol code for a Device in Java, a Backend in C# and a Browser in TypeScript; your application adds authentication, business logic and routing.](docs/img/tldr-overview.svg)

**C# you already know, read as AdHoc entities:**

| You write | AdHoc reads it as |
|:--|:--|
| `interface Shop` at the top level of a namespace | the project; the namespace becomes the namespace or package of the generated code |
| `struct Server : Host` with `/// <see cref="InJAVA"/>` | a host - one program of the system - and its languages |
| `class Order { long id; }` | a pack and its fields; attributes on a field set its limits and its encoding |
| `const`, `static`, `enum`, a `struct` that implements nothing | constants, enums, constants containers: compiled into the hosts, never sent |
| `interface Link : Connects<Client, Server>` | a connection; with `VirtuallyConnects<L, R, PATH>`, one carried through relay hosts |
| `interface Job : Actor`; an empty `struct` with `[L____________<Next, Packs>]` | an actor; its states and their branches |
| `(L____________, Pong) ping(Ping req);` in a connection body | an RPC actor, in one line |
| `interface Reports : _<@Server.Report> { }` | a named Pack Set |
| `interface Edge : Multiplex<(Link, AdminLink)> { }` | one port for several connections |

- **Checked, generated, tested.** Your IDE and the C# compiler check the file as you type. AdHocAgent checks every AdHoc rule on your
  machine and uploads only a description that passes. The cloud generator tests the code before it returns it - how much depends on
  its load at that moment - and the result says how far the testing went. Code generation is free.
- **A folder per host and language**: `gen/` with the protocol code, replaced on every generation; `lib/` with the runtime library;
  `LICENSE-GRANT.md`; a demo and project files when you ask for them. For MANUAL's `Telemetry` example - a `Sensor` in C#, a `Collector`
  in Java and TypeScript:

  ```text
  > AdHocAgent .\Telemetry.cs
  <output folder>/Telemetry/InCS/Sensor/        gen/  lib/  LICENSE-GRANT.md
  <output folder>/Telemetry/InJAVA/Collector/   gen/  lib/  LICENSE-GRANT.md
  <output folder>/Telemetry/InTS/Collector/     gen/  lib/  LICENSE-GRANT.md
  ```

- **Several projects in one description.** The first project AdHocAgent meets is the root, the one whose hosts are generated; the others
  are libraries it extends and takes entities from. A description may span several files, or the files a `.csproj` names.

**Why it is good.** The system has one source of truth, written in a language your tools already understand. Who talks to whom, about
what and under which rules is the generator's input, so it cannot drift away from the code.

→ MANUAL: [The pipeline](MANUAL.md#the-pipeline-from-a-description-to-running-code) ·
[A description is compiled C#](MANUAL.md#a-description-is-compiled-c) ·
[From C# constructs to AdHoc entities](MANUAL.md#from-c-constructs-to-adhoc-entities) ·
[Several projects in one description](MANUAL.md#several-projects-in-one-description) ·
[What AdHoc generates for each host](MANUAL.md#what-adhoc-generates-for-each-host) ·
[Target languages](MANUAL.md#target-languages-of-adhoc) · [Open source](MANUAL.md#open-source)

### The runtime engine: bytes pulled through two buffers

*Where it sits: runtime - your code → generated connection → **one send buffer, one receive buffer** → socket*

The generated code runs on a small runtime library with TCP, WebSocket and UDP transports (UDP in C# and Java). Each connection owns two
buffers of a size your code chooses, and every byte of every pack passes through them. How that keeps memory flat and reading
single-pass is shown in [Read in place, constant memory](#read-in-place-constant-memory). This is the engine's timing and threading.

![Top: a timeline with lanes for your code, the sending queue, the transmitter, a 1024-byte send buffer and the socket. send(A) and send(B) return at once and only queue the packs; each time the socket has room, the transmitter refills the same buffer - fill 1 stops in the middle of a field of A, fill 2 finishes A and starts B, fill 3 finishes B - and each fill leaves as one chunk: pack A never existed as one byte array. Bottom: the receive loop and the transmit loop of a connection, each handling packs and timeouts one at a time to completion, with the state both loops touch as the one place to coordinate; a full sink pauses socket reads.](docs/img/readme-pull-and-loops.svg)

- **The socket pulls.** `send(pack)` queues the pack and returns at once. Whenever the socket can take bytes, the transmitter writes the
  pack's id and fields into the send buffer until the buffer is full; the next fill resumes exactly where it stopped, in the middle of a
  field if need be. A 64-byte status pack and a gigabyte `Stream` field take the same path, and neither ever exists as one byte array.
- **One knob.** The buffer size is an argument of the transport's constructor - 1024 bytes by default in C#, a few kilobytes as a rule,
  and at least **128 bytes**: whatever the size, every pack passes through it in portions.
  It sets how many bytes go per system call, not how large a pack may be.
- **Two loops, no locks.** Each direction runs its events - packs and timeouts - one at a time, each to completion. Handlers on one side
  never race, so the state they own needs no locks; state shared by a receive handler and a transmit handler is the one place you
  coordinate.
- **Back-pressure built in.** A destination that cannot take more - a full stream sink - stops the reading of the socket. Unread data
  waits in the network, not in memory.

```csharp
// both buffers of every connection this transport opens: 1024 bytes
var client = new Network.TCP.WebSocket.Client<CommunicationChannel.Connection>("My Client Name",
                 connection => new CommunicationChannel.Connection(connection),
                 Network.TCP.onFailurePrintConsole,
                 1024);   // bufferSize - not a limit on the size of a pack
```

**Why it is good.** Capacity is planned by the number of connections, not by the largest message. The garbage collector sees no
per-message arrays - the C# TCP transport even rents its buffers from the shared pool. And a handler is plain sequential code.

**Limits.** A handler that blocks stalls its direction, and on loop threads shared by a transport's connections it holds back the
others too: hand long work to another thread and send the result as a new pack. Memory outside the two buffers is yours to count: the
objects of concrete packs, the journal of guaranteed delivery (100 MB at most by default), the contexts of compression and cipher stages.

→ MANUAL: [The runtime model: one buffer per direction](MANUAL.md#the-runtime-model-one-buffer-per-direction) ·
[Sending](MANUAL.md#sending-the-transport-pulls-the-bytes) · [Receiving](MANUAL.md#receiving-bytes-are-consumed-where-they-land) ·
[Memory on the serialization path](MANUAL.md#memory-on-the-serialization-path) · [The socket buffer size](MANUAL.md#the-socket-buffer-size) ·
[Threads](MANUAL.md#threads-the-receive-loop-and-the-transmit-loop)

### Data: packs, types, constants, your own metadata

*Where it sits: project → host → connection → actor → state → branch → **pack → field**; constants: compiled into the hosts, never sent*

A **pack** is a C# `class`, and its instance fields are the data that travels. The field types cover what real systems exchange, every
size has a declared maximum, and the things every host must agree on but never needs to send - limits, lookup tables, your own
metadata - are compiled into the code instead of being exchanged.

![What a field costs on the wire: sixteen cards, each a kind of value, its declaration and its cost. A bool, 1 bit; a MinMax(-40, 125) short, 1 byte; an absent uint?, 1 bit; a three-value enum, 2 bits; a string, a varint count and 1 to 3 bytes per UTF-16 code unit, at most 64 code units as declared; D(32) Binary[], exactly 32 bytes with no length; a list of at most 100 packs, a 1-byte length and the items; a set (or a map) of at most 16 items, a count and the items; a nested pack, in place with no id; one of several alternatives, 1 bit per unset one; an empty event pack, only its id; DateTime, 6 bytes; a DateTimeDef for 2000-2050 at one second, 4 bytes; a Duration of at most 30 000 steps of 19 ms, 2 bytes; a Stream, chunked through constant memory, its cap enforced on both sides; a constant and a custom attribute, 0 bytes - compiled into each host. Orange badges mark declared maximums.](docs/img/readme-type-palette.svg)

```csharp
enum Payment { Card, Cash, Voucher }                          // travels as a 2-bit code

class RetentionAttribute : Attribute { public RetentionAttribute(int days) { } }   // your own metadata

struct Limits { public const byte MAX_ITEMS = 100; }          // a constants container: compiled in, never sent

class Item { uint sku; ushort qty; }                          // a pack used as a field type

[Retention(30)]                                               // becomes the constant Retention = 30 of Purchase
class Purchase
{
    [D(Limits.MAX_ITEMS)] Item[,,] items;                     // a list of at most 100 items
    Payment                        payment;
    [D(+64)] string                note;                      // at most 64 characters
    [D(32)] Binary[]               signature;                 // exactly 32 raw bytes: no length on the wire
    [D(+16)] Map<string, string>   tags;                      // at most 16 entries
    DateTime                       placed;                    // 6 bytes
    ulong?                         voucher_id;                // optional: one bit when absent

    static string[] PAYMENT_TEXT = { "card", "cash", "voucher" };   // a lookup table compiled into each host
}

class Shipped { }                                             // an empty pack a branch sends: an event, only its id travels
```

- **Packs and events.** A class declared inside another is a pack of its own - nesting only names it; a field typed with a pack carries
  that pack inside its parent. An empty pack - a class without fields that a branch sends - is an event: only its id travels, and one
  static handler receives it, with no allocation.
- **Types for real data.** Every C# numeric type except `decimal`, `bool` (1 bit) and `char`; optional values; strings; arrays of
  constant, fixed or dynamic length; `Map` and `Set`; multidimensional arrays; raw `Binary`; packs and enums as field types; named type
  aliases (`TYPEDEF`); four time types; `longJS` and `ulongJS` for 64-bit values that a TypeScript `number` holds exactly.
- **One of several, without a union type.** A pack of optional fields, where an unset alternative costs one bit - or, for whole
  messages, several packs told apart by their pack id.
- **Every size has a maximum.** An array, map or set holds at most 255 items, a string 255 characters, unless its `[D(...)]` says
  otherwise; `_DefaultMaxLengthOf` changes the defaults for a whole project. The limits become constants in every language.
- **Constants never travel.** `const` and `static` values are written into the generated code of the hosts as literals. A `static`
  initializer may be any C# expression: AdHocAgent runs it at generation time, so lookup tables cost nothing on the wire.
- **Your own metadata, for free.** Declare an attribute class and put it on a project, host, pack, field, connection, actor, RPC method
  or state: every language gets it as constants of that entity - routing tags, UI labels, retention periods. The bytes on the wire are
  the same with or without it.

**Why it is good.** Shapes, limits and vocabulary are declared once and are the same in every language. A receiver knows the maximum of
every string and collection from its own generated code, and what both sides only need to *know* is never put on the wire.

→ MANUAL: [Packs](MANUAL.md#packs) · [Empty packs](MANUAL.md#empty-packs) ·
[The field types at a glance](MANUAL.md#the-field-types-at-a-glance) ·
[Alternatives: exactly one of several](MANUAL.md#alternatives-exactly-one-of-several) · [Time types](MANUAL.md#time-types) ·
[Collection limits](MANUAL.md#collection-limits-_defaultmaxlengthof) · [Constants and enums](MANUAL.md#constants-and-enums) ·
[Where constants and enums end up](MANUAL.md#where-constants-and-enums-end-up) · [Custom attributes](MANUAL.md#custom-attributes)

### Designed bytes: encodings that follow your declarations

*Where it sits: … → pack → **field → wire bytes**; your code sees only the values you declared*

The wire form of a field follows from its declaration, not from its C# type. [Fewer bytes on the wire](#fewer-bytes-on-the-wire) shows
the result on a whole pack, and [R13](#r13-every-byte-costs-airtime-telemetry-in-a-handful-of-bytes) on a telemetry sample. This is the
toolbox behind them.

![A primitive field whose values span 40 000 000 000 to 40 000 000 093 in its three layers: a long of 8 bytes at exT, the type your code sees; 0 to 93 at inT, the storage, and at ioT, the wire. The exT to inT step runs when your code touches the field and only shrinks storage; the inT to ioT step runs on every send and receive.](docs/img/value-layers-transform.svg)

- **Three layers per primitive type field** (and per primitive item of a collection). exTernal (`exT`): the type your code sees.
  inTernal (`inT`): how the generated code stores the value. IO wire type (`ioT`): how it travels, with no language granularity.
  Getters, setters and constants speak `exT` only.
- **The range picks the width - and the API type.** `[MinMax]` stores `value - min`, and only the span `max - min` decides the width:
  below 128 a bit field packed with the others, up to 255 one byte, beyond that exactly the bytes the span needs on the wire - 3, 5, 6
  or 7 included, although no language has such an integer. The getter takes the narrowest type that holds the range, wider or
  narrower than the type you wrote.
- **Varint with a base and a direction.** `[A]` for values near a floor, `[V]` near a ceiling, `[X]` around a centre (ZigZag). The cost
  follows the distance from that point, and AdHocAgent refuses a varint whose span fits in one byte, pointing you to `[MinMax]`.
- **Codes, not constants.** An enum travels as a code: `value - min` for close values, the index among its values when that is
  narrower, the mask shifted down for a flags enum. A null is a spare code of the field's bits, or one null bit.
- **Small values share bits.** In any pack, every field whose codes fit in 7 bits - `bool` (1 bit), `bool?` (2), small enums and
  ranges - goes into the pack's bit storage, packed together and rounded up to whole bytes once per pack; see
  [Bit fields](#bit-fields-small-values-share-the-packs-bits).
- **Value Packs.** A pack whose fields fit in 64 bits is generated as one integer: a struct over one unsigned integer in C#, an
  annotated primitive in Java, a `number` in TypeScript (up to 53 bits). No object, no heap allocation, no indirection - and the same
  bytes from all three.
- **Ranges reach your code.** `MIN` and `MAX` constants on every numeric field - plus `NULL` and `ZERO` where they apply - let you check
  input or size a slider without copying a number into any host.

```csharp
enum Sparse { LOW = -7, MID = 0, HIGH = 300 }                 // sent as an index 0, 1, 2: 2 bits, not 9

class Point2 { float x; float y; }                            // 64 bits: a Value Pack

class Calibration
{
    [MinMax(-1128, 873)]           byte     altitude_offset;  // the range outgrows byte: your code gets a short
    [MinMax(20, 120)]              int      temperature;      // the range fits a byte: your code gets a byte
    [MinMax(1_000_000, 1_080_000)] int      serial;           // an int in memory, exactly 3 bytes on the wire
    [A]                            uint?    bytes_received;   // varint: values hug zero
    Sparse                                  level;            // a 2-bit index
    [D(16)]                        Point2[] outline;          // 16 Value Packs: 16 numbers, no objects
}
```

![How an enum becomes a code, in three rows: the contiguous enum MID, 1001 to 1008, travels as value minus 1001, so 1004 is the 3 bits 011; the sparse enum LOW = -7, MID = 0, HIGH = 300 travels as the index 0, 1 or 2 in 2 bits instead of 9; the flags enum B4 = 16, B5 = 32, B6 = 64 travels as its mask shifted down by 4, so B4 or B6 is the 3 bits 101. Hatched bars show the width each would take otherwise: 32 bits as an int, 9 bits as an offset from -7, 7 bits unshifted. The generated accessors convert both ways, so your code sees only the declared values.](docs/img/readme-enum-codes.svg)

![The 23-bit Progress Value Pack of AdHoc's own protocol: six MinMax fields in fixed bit slots of one 32-bit carrier, widest first; on the wire the pack id and 3 bytes; and three lanes - a C# struct over a uint, a Java int tagged @Progress, a TypeScript number - pointing at the same bytes. One integer, no object.](docs/img/dev-07-value-pack-bits.svg)

**Why it is good.** The description holds what only it can know - the range of a value, where its values cluster, how small a
structure really is - and the generator turns that knowledge into bits, identically in every language, checking it on the way.

**Limits.** Fields are known by position, so their order is part of the format (see [Identity](#identity-numbers-instead-of-field-tags)).
A declared range is a hard limit: a value outside it cannot be sent. A `double` whose `[MinMax]` fits `float` becomes a `float`.

→ MANUAL: [How values become bytes](MANUAL.md#how-values-become-bytes) · [Value layers](MANUAL.md#value-layers-ext-int-iot) ·
[Narrowing with MinMax](MANUAL.md#narrowing-a-primitive-field-with-minmax) ·
[The varint attributes](MANUAL.md#the-varint-attributes-a-v-and-x) · [The one-byte rule](MANUAL.md#the-one-byte-rule) ·
[Enums on the wire](MANUAL.md#enums-on-the-wire) · [Absent values](MANUAL.md#absent-values-on-the-wire) ·
[Range constants](MANUAL.md#range-constants-in-the-generated-code) · [Value Packs](MANUAL.md#value-packs) ·
[The layout of a pack on the wire](MANUAL.md#the-layout-of-a-pack-on-the-wire)

### Composing packs and Pack Sets: define once, select by rule

*Where it sits: **project** → host → connection → actor → state → **branch → pack → field**; resolved by AdHocAgent before any code is
generated*

Two tools keep a large protocol small. **Composition** builds packs from packs, so a field is defined in one place however many packs
carry it. **Pack Sets** select packs by name, scope or tag, so a rule like "every active pack of the orders" is written once and every
place that needs it says so by name.

![How AdHocAgent assembles a pack's field list in four steps: 1 the native fields version and note; 2 field injection appends stamp; 3 XML directives in the doc comment add id and skip a second version, because the first field with a name wins; 4 the base list - the C# base class, then the _<(A, B)> items, X<C> removing - adds trace_id. Modify<Pack> from another project can change the list later. The final list, coloured by source: version, note, stamp, id, trace_id.](docs/img/dev-08-field-composition.svg)

- **Shared fields, not copies.** Take fields with C# inheritance or `_<(...)>`, and import or drop single fields with
  `/// <see cref="Pack.field"/>+` and `-`. An imported field *is* the source's field: change it there, and every pack that took it
  follows; rename it with your IDE, and the references follow too.
- **Field injection.** `FieldsInjectInto<PackSet>` appends fields to a whole family of packs - a request id, an event code.
- **Headers.** `HeaderFor<PackSet>` declares metadata that travels between the pack id and the payload but is not part of the pack
  class; your code reads it before the payload is parsed. [R17](#r17-a-metadata-routed-payload-blind-middle-tier-audit-and-tracing)
  builds an audit tier on it.

```csharp
class Tracing { ulong trace_id; }                             // a field defined once ...

class Order : Tracing                                         // ... inherited here
{
    long id;

    public class Line { int sku; int qty; }

    /// <see cref="Order.id"/>+
    public class Cancel { [D(+200)] string reason; }          // imports Order.id: the same field, not a copy

    /// ⛔ Legacy
    public class LineV1 { int sku; }
}

class Stamp : FieldsInjectInto<(Order, @Order)>               // appended to Order and every pack inside it
{
    uint request_id;
}

[SkipDoc(@"⛔")] interface ActiveOnly<SCOPE> { }              // a filter template: a policy written once

interface Link : Connects<Client, Server>
{
    [_____lr_____<ActiveOnly<(Order, @Order)>>]               // Order, Line, Cancel - LineV1 is left out
    struct Start { }
}
```

![One selection language, seven consumers. Left: the parts of a Pack Set expression - a pack, a scope with @ for everything nested, _<...> and X<...>, a named set, a filter template matching full paths (KeepName, SkipName) or comments and tags (KeepDoc, SkipDoc), combined as union, intersection and difference. Middle: AdHocAgent resolves ActiveOnly<(Order, @Order)> while it reads the description into the list Order, Order.Line, Order.Cancel - LineV1, tagged legacy, is filtered out. Right: the same kind of expression feeds a branch, an RPC method, HeaderFor, FieldsInjectInto, Resumable<HOST, PACKS>, the UDP exclusion list and a host's implementation groups. The generated code holds only the resulting lists.](docs/img/readme-packset-hub.svg)

- **One selection language for every rule.** A pack; a scope with or without `@` (with it, everything nested); `_<...>` and `X<...>`;
  named Pack Sets; and filter templates `F<SCOPE>` that filter by regular expressions over a pack's full path (`KeepName`, `SkipName`)
  or over its comments and its tags in the packs table (`KeepDoc`, `SkipDoc`). Sets combine as union, intersection and difference.
- **The same expression everywhere.** Branches, RPC methods, headers, injectors, guaranteed delivery, UDP exclusions and host
  configuration all take it. [R12](#r12-least-knowledge-hosts-every-role-sees-only-its-own-messages) uses it for role-based views.
- **Nothing left at runtime.** AdHocAgent resolves every set while it reads the description; the generated code holds only the
  resulting lists of packs.

**Why it is good.** A field lives in one place, and a policy lives in one place. A pack added later and tagged the same way joins every
branch, header and injector that selects by that tag, with no list to update - and none of this flexibility costs anything at runtime.

**Limits.** An injector changes the wire format of every pack it reaches. Filter patterns are case-sensitive, unanchored regular
expressions, and an emoji tag must match code point for code point.

→ MANUAL: [Building packs from packs](MANUAL.md#building-packs-from-packs) ·
[How a pack's field list is assembled](MANUAL.md#how-a-packs-field-list-is-assembled) ·
[Field injection](MANUAL.md#field-injection-fieldsinjectinto) · [Pack headers](MANUAL.md#pack-headers-headerfor) ·
[Pack Sets](MANUAL.md#pack-sets) · [Scopes and @](MANUAL.md#project-host-and-pack-scopes-and-) ·
[Filtering by name and documentation](MANUAL.md#filtering-a-set-by-name-and-by-documentation) ·
[Filter templates](MANUAL.md#filter-templates-f) ·
[Combining sets](MANUAL.md#combining-sets-union-intersection-difference)

### Hosts: generated objects or your own structures

*Where it sits: project → **host** → connection → … → **pack → field**; pipeline: generator → **code per host and language***

A **host** is one program of the system: `struct Server : Host`, with its languages named in its doc comment. It is a *type* of node,
so any number of running programs can be instances of one host. For every pack a host handles you choose who holds the data: the
generated code, or your own structures.

![Who holds the data, as a matrix over the pack Frame - columns Frame, timestamp, channel and samples. Broker in Java, marked --, sends it: every cell purple, read from the broker's own objects. Router in C# receives it: a yellow generated object stores Frame, timestamp and channel, while samples is purple, handed to your code as it arrives. Dashboard in TypeScript receives it: every cell yellow, a plain generated object. Under the matrix one wire strip of Frame - pack id, timestamp 8 bytes, channel 4 bytes, null bits, samples as a 3-byte length and 4 bytes per item - with a bracket over all three rows pointing to it: the same bytes for every row. A field rule beats a pack rule, a pack rule beats the host default.](docs/img/readme-impl-matrix.svg)

- **Concrete (`+`).** The generated class holds the values: fill it and send it; receive it with every field decoded. Right when the
  packs *are* your data model.
- **Abstract (`-`).** The generated code is an interface that your own type implements. The parser calls a setter per field as the
  bytes arrive, the serializer a getter per field as it writes. No generated object, no copy, no second model to keep in step - and
  packs larger than the memory you want to spend on them. Arrays of one-byte items arrive as windows of the receive buffer itself.
- **Per host, language, pack - or single field.** The same pack can be abstract on a broker that keeps it in its own log and concrete on
  a new dashboard; the bytes are the same either way. One huge field of an otherwise ordinary pack can be handed to your code as it
  arrives while the rest stays stored.
- **Equality and hash on request.** A second `+` or `-` decides whether they are generated.

```csharp
class Frame
{
    long                   timestamp;
    int                    channel;
    [D(1_000_000)] int[,,] samples;          // up to a million items
}

/// <see cref='InJAVA'/>--          // the broker: every pack is an interface over its own objects
struct Broker : Host { }

/**
    <see cref='InCS'/>
    <see cref='InCS'/>-
    <see cref='Frame.samples'/>
*/
struct Router : Host { }            // C#: Frame is an ordinary object, but samples are handed over as they arrive

/// <see cref='InTS'/>              // a new dashboard: plain generated objects
struct Dashboard : Host { }

interface ToRouter    : Connects<Broker, Router>    { [l____________<Frame>] struct Start { } }
interface ToDashboard : Connects<Broker, Dashboard> { [l____________<Frame>] struct Start { } }
```

![Concrete (+), top row: the receive buffer feeds the generated parser, which fills a generated pack object that your code reads; for sending, your code fills a generated object that the serializer copies into the send buffer. Abstract (-), bottom row: the parser calls a setter per field straight into your own structures as the bytes arrive, and the serializer calls a getter per field on your object - no generated object either way. The same bytes on the wire in both modes.](docs/img/dev-10-impl-modes.svg)

AdHoc uses both itself. In AdHocAgent's generated C# code the `Login` pack it sends is a class, while the `Project` pack that carries
your parsed description to the generator is an interface that the agent's own model classes implement.

**Why it is good.** A binary protocol can be added to a system that already has its own model - a database node, a broker, a client
library - without a DTO layer and a copy of every message, while new programs get ordinary objects. The rule is one question: where
does the data live in this host? [R15](#r15-ingest-straight-into-your-own-data-structures) shows the abstract side at full speed.

**Limits.** A received abstract pack lands in an object your code supplies: assign its factory in the generated `_Allocator` before the
first such pack arrives, or the default factory throws.

→ MANUAL: [Declaring a host](MANUAL.md#declaring-a-host) · [Language markers](MANUAL.md#language-markers) ·
[Implementation modifiers](MANUAL.md#implementation-modifiers-who-holds-the-data) ·
[Abstract: your code holds the data](MANUAL.md#abstract---your-code-holds-the-data) ·
[Choosing + or -](MANUAL.md#choosing--or---generated-objects-or-your-own-structures) ·
[Host, pack and field rules](MANUAL.md#implementation-management-host-pack-and-field-rules) ·
[What the generator produces for a host](MANUAL.md#what-the-generator-produces-for-a-host)

### Conversations: connections, actors, states, RPC

*Where it sits: project → host → **connection → actor → state → branch** → pack → field*

A **connection** is the typed pipe between two hosts. Inside it, an **actor** is one conversation with a state machine of its own,
a **state** is an empty `struct`, and the state's **branch** attributes say which side may send which packs there and which state
each pack leads to. Both hosts are generated from the same machine, so they agree on the order of the conversation by
construction: a pack the current state does not allow is refused before it leaves, and an unexpected one is reported on arrival.

![One connection between a Client and a Server carrying four conversations, one lane each: Actor0 going Start, Authorizing, Ready on Login and Welcome, with a Beat loop; Notify, a function group through which the Server pushes Note and Alert at any time; ping, an RPC going Call, Return, End on Ping and Pong; and Job, a swarm of up to 8 instance pairs, three shown, each with its own Deadline clock. Below, one wire strip where the packs of all four interleave, colored by actor. Caption: every pack carries its actor and, for an actor with instances, its instance; no conversation waits for another.](docs/img/readme-conversations.svg)

```csharp
interface Link : Connects<Client, Server>   // the hosts and their packs are declared elsewhere in the project
{
    // Actor0, the connection's own machine: a login, then a working state
    [L____________<Authorizing, Client.Login>]  // L: the Left host sends Login, and the state moves on
    struct Start { }

    [____________R<Ready, Server.Welcome>]      // R: the Right host answers
    [____________R<Close, Server.Denied>]
    struct Authorizing { }

    [_____lr_____<Beat>]                        // lr: either side; lower case: the state stays
    struct Ready { }

    // a function group: the Server pushes at any time; no instance, nothing to track
    interface Notify : Actor
    {
        [____________r<(Server.Note, Server.Alert)>]
        struct Messages { }
    }

    // RPC in one line: an actor Call -> Return -> End, with no correlation ids of your own
    (L____________, Pong) ping(Ping req);

    // a swarm: up to 8 jobs at once on this connection, each with its own clock
    interface Job : Actor
    {
        int Actor.MaxActiveInstances => 8;

        [L____________<Running, Client.JobStart>]
        struct Start { }

        [Deadline(30)]
        [____________r<Server.JobProgress>]
        [____________R<End, Server.JobDone>]
        struct Running { }
    }
}
```

| You declare | What the generated code does on both hosts |
|:--|:--|
| branches on states | sends and accepts only the packs the current state allows; a handler receives the transmitter of the next state, so it can answer only with packs that state permits |
| transitional branches of one side | that side leads the state and is the only one that can move it, so transitions never race; a 4-bit epoch on the packs of an actor with instances drops a late pack from the previous state |
| several actors | independent conversations on one socket: a file transfer in one actor, a chat in another |
| an RPC method | the caller's pooled instance holds the callback context, the replier gets a pooled, unregistered context: with a warm pool no actor object is allocated per call. Without a direction marker either side may call, in two actors that never collide |
| `MaxActiveInstances` | `1`, `N`, `UNLIMITED` or multicast `+N`; at the limit `Acquire` returns `null`, and a peer that opens one instance too many has its connection aborted |
| `[Deadline]`, `[Idle]` | end one stuck instance while the connection and its other actors live on; `[Idle]` is a lease that only packs received from the peer renew |
| `[Grouped]` | gathers the instances of all connections into groups your code walks to fan an event out; join and leave are O(1), and an instance leaves by itself when it ends |
| `[TransmitTimeout]`, `[ReceiveTimeout]` | connection-wide stall limits, written by each state as it is entered |

Each direction of a connection is one event loop: the handlers of one side run one at a time and need no locks for the state they
own. Request/response is still here, as one actor shape among several: a job that is accepted, reports progress and then
delivers is a three-state actor, and a call relayed through a middle host is one RPC per hop.

![What exists on each host per actor kind, Client on the left, Server on the right. Actor0: one pair for the whole connection. Function-group actor: one shared static object per host, Notify.one, serving every connection; no instance per conversation. RPC actor: a registered ping instance waiting in Return on the caller, a pooled unregistered context on the replier; Ping carries the caller's address, Pong goes back to it. Stateful swarm, MaxActiveInstances => 8: pairs Job #1 to #3, each with its own address. Multicast, +4: Room pairs plus one group address that a cast reaches in one pack.](docs/img/dev-13-actor-kinds.svg)

![The Upload actor, Start, Sending, Review, End, running on both hosts, drawn as a sequence between a Client and a Server lifeline. 1: Begin moves both replicas to Sending, epoch 1. 2: a Chunk stay pack, the state stays. 3: the Server, which leads Sending, sends Done and moves to Review, epoch 2, while a Chunk stamped epoch 1 crosses it and is dropped on arrival as stale. 4: in Review the Client's code tries to send a Chunk, and the generated code refuses it before it leaves, as DISCARDED_BYSTATE. Footer: only the leading side moves a state; both ends run one generated machine.](docs/img/readme-state-guard.svg)

Calling and answering the RPC above, in C#:

```csharp
// Client, the caller: what the answer means here (once), then a call
ping.State.Return.OnReceiveD.Pong.handlers += (pong, conn, actor) => Show(pong);
var actor = ping.Acquire(conn);
ping.State.Call.ONE.transmitter.send(request, conn, actor);

// Server, the replier: answer every Ping, and give the context back once the Pong is out
ping.OnReceiveD.Ping.handlers   += (req, conn, ctx) => ctx.send(MakePong(req), conn);
ping.OnSerializeD.Pong.handlers += (_, conn, ctx) => ctx.Release(conn);
```

**Why it is good.** In request/response, the order of calls, who may answer, the valid replies, correlation, concurrency limits and
timeouts live in documentation and in checks written by hand on both ends. Here they are declarations that generated code enforces
on both ends: notifications cost nothing to track, RPC needs no correlation code of yours, swarms are bounded, and a peer that
opens too many conversations is cut off.

**Limits.** `UNLIMITED` skips the limit check and lets a peer open as many instances as it likes - keep it for trusted networks. A
slow handler stalls its whole direction - in C# and Java, with every connection served by the same transport loop: hand long work to
another thread and send the result back as the next event.

**In the manual:** [Connections](MANUAL.md#connections) · [Actors, states, branches](MANUAL.md#actors-states-branches) ·
[The side that leads a state](MANUAL.md#the-side-that-leads-a-state) · [Four kinds of actor](MANUAL.md#four-kinds-of-actor) ·
[RPC methods](MANUAL.md#rpc-methods) · [Concurrency modes](MANUAL.md#concurrency-modes) ·
[Per-instance timers](MANUAL.md#per-instance-timers-deadline-and-idle) · [`[Grouped]`](MANUAL.md#actors-of-many-connections-grouped) ·
[The two loops](MANUAL.md#the-two-loops-threads-and-actor-handlers) ·
[A real state machine](MANUAL.md#a-real-state-machine-adhocs-communication-connection)

### Sessions that outlive sockets, guaranteed delivery, UDP

*Where it sits: project → host → **connection → session** → actor → state; at runtime: what outlives a socket, and which packs survive its loss*

At runtime the live instance of a connection is a **session**: the generated connection object, its actors, their states and
everything your code attached to them. The socket only carries it. By default the two end together; three declarations decouple
them, each building on the one before:

| Declaration | What it gives |
|:--|:--|
| `[Resumable(minutes)]` on a connection or a state | a lost socket **parks** the session; a peer that comes back in time and proves who it is continues it - the same actors in the same states |
| `Resumable<HOST, PACKS>` in a connection body | the packs `HOST` sends there arrive **once, in order**, also across lost sockets |
| `[UDP(minutes)]` on a connection | the connection runs over UDP, with **every** pack guaranteed both ways; `[UDP<EXCLUDE_PACKS_SET>(minutes)]` leaves the packs you name unguarded |

![A session above two sockets: on socket 1 the actor is in state Working and packs 1 to 5 are sent; the socket is lost while packs 4 and 5 are in flight; the session waits parked for up to [Resumable(minutes)]; on a new socket 2 the peer logs in and calls reclaim(session_id); the same actor continues in the same state, and packs 4 and 5 are replayed from the sender's journal before packs 6 and 7. Footer: on TCP you guarantee the packs that must not be lost; on UDP every pack is guaranteed and you exclude the ones that may be lost.](docs/img/tldr-session.svg)

```csharp
[Resumable(30)]                                        // a lost socket parks the session for up to 30 minutes
interface Orders : Connects<Client, Server>
{
    [Resumable(0)]                                     // nothing worth keeping before the login
    [L____________<Authorizing, Client.Login>]
    struct Start { }

    [Resumable(0)]
    [____________R<Working, Server.Welcome>]
    [____________R<Close, Server.Denied>]
    struct Authorizing { }

    [l____________<(Client.Order, Client.Cancel)>]
    [____________r<(Server.Receipt, Server.Welcome)>]  // Welcome: a reclaimed session answers the new login from here
    struct Working { }

    // what the Client sends here arrives once, in order, across lost sockets
    interface FromClient : Resumable<Client, (Client.Order, Client.Cancel)>
    {
        int resumable_megabytes => 20;
    }
}
```

**Identity is your own login.** Every fresh socket runs your ordinary login states. Once your handler has recognized the peer, it
makes one call, and the runtime does the rest:

```csharp
// Server, in your handler of Client.Login
var ext = conn.ext_connection;
long session_id = pack.resume_token != 0
                      ? ext.reclaim(pack.resume_token)   // a session parked under this id moves onto this socket
                      : ext.identify(account_id);        // a first login: a non-zero identity of yours
// answer with Welcome { session_id } from the session now on this socket, ext.Internal
```

The dialing side calls `identify` on its own connection too: only an identified connection parks, and only an identified sender
may send a guaranteed pack. The runtime then raises `resume(connection)` on both ends: the receiver names the position it has
consumed, and the sender replays its journal from there before new packs. A sender whose process died has lost its journal: it
answers `LOST`, and the receiver hears `resume(null)` - a gap, time to resync instead of waiting forever. There are no "where was I"
packs to design.

![The price of Resumable<Client, (Order, Cancel)>. On the wire from Client to Server, guaranteed packs Order and Cancel, marked G, travel between ordinary packs, with a callout: plus 0 bytes in the pack. Below the Client, the journal keeps a copy of each guaranteed pack as it goes out, in memory and then in 4 MB files, capped by resumable_megabytes (default 100); ordinary packs bypass it. Back from the Server, one REPORT(position) control record, the pack id plus 8 bytes, per 4 KiB of guaranteed packs frees the journal below that position. After a reconnect, CONTINUE(position) starts the replay.](docs/img/readme-guarantee-cost.svg)

A parked session keeps its slot, its actors and states, your objects and, on disk, its journal; it gives its socket and transport
buffers back to the pools. A graceful close or an abort by your own code ends the session at once - only a loss parks it. State
that can be rebuilt need not be parked at all: describe it as packs and store it, because a pack in a file or a database column is
the same bytes as a pack on the wire.

**UDP: reliable where it matters, fresh where it does not.** On TCP you choose the packs that must not be lost; on UDP you choose
the ones that may be:

```csharp
interface Volatile : _<(Client.Position, Server.Telemetry)> { }   // the next one replaces the last

[UDP<Volatile>(30)]              // over UDP; every pack guaranteed both ways, except Volatile
interface GameLink : Connects<Client, Server>
{
    [l____________<(Client.Input, Client.Position)>]
    [____________r<(Server.Score, Server.Telemetry)>]
    struct Play { }
}
```

![UDP with every pack guaranteed, as a sequence from a Client to a Server. Datagram 41 carries Input 101 and a Position; datagram 42, with Input 102 and a Position, is lost; datagram 43 carries Input 103. The receiver sees the gap from the datagram numbers and asks to continue from its position; the sender replays Input 102 and 103 from its journal. The lost Position is in the Volatile set: never journalled, not replayed, replaced by the next one. The receiver's code sees Input 101, 102, 103 once, in order. A strip shows the 9-byte datagram header: flags, connection id, series, datagram number.](docs/img/readme-udp-gap-replay.svg)

A UDP server with a static X25519 key encrypts every connection (a 16-byte tag per datagram), and congestion control with pacing is
on by default. UDP connections exist in the C# and Java runtimes - a browser cannot open a UDP socket.

**Why it is good.** Reconnect logic, sequence numbers, acknowledgments, replay buffers and resume handshakes are among the most
error-prone code in networked software. Here they are declarations with documented costs: a dropped Wi-Fi resumes the same actor in
the same state, an order arrives once and in order, and on UDP only the packs you exclude may be lost - and they cost no journal copy.

**Limits.** The session id is a slot and an identity, not a secret: authenticate the peer before `reclaim`. A parked session holds
its objects and journal until the peer returns or the time runs out. Every guaranteed pack is copied into the journal on every
send - guarantee what must not be lost, not everything.

**In the manual:** [Sessions, guaranteed delivery, UDP](MANUAL.md#sessions-guaranteed-delivery-udp) ·
[Parking a session](MANUAL.md#parking-a-session) · [Identity and reclaim](MANUAL.md#identity-and-reclaim) ·
[resume](MANUAL.md#resume-the-runtime-tells-your-code-that-the-session-continues) ·
[How the journal works](MANUAL.md#how-the-journal-works) · [What the guarantee costs](MANUAL.md#what-the-guarantee-costs) ·
[UDP connections](MANUAL.md#udp-connections) · [Leaving packs unguarded on UDP](MANUAL.md#leaving-packs-unguarded-on-udp)

### Fan-out and broadcast: one pack, many connections

*Where it acts: the runtime - **sending queues and transmitters** of many connections at once*

Sending the same pack to hundreds of peers costs neither a copy per peer nor a second encoding:

- **One object, many queues.** `send` puts a reference to the pack into the queue of each connection; each transmitter writes it from
  the object into its own buffer, with its own cursor. Keep the object unchanged until every connection has reached `OnSerialized`.
- **One capture, many subscribers.** A host that passes on what it receives captures the bytes of an incoming pack while it decodes it
  (`Connection.Receiver.BytesCopy`), stores the decoded values, and hands the one capture to every subscriber
  (`Connection.Transmitter.Broadcaster`): decoded once, never encoded again. The capture is reference-counted and pooled, nothing is
  captured while nobody is subscribed, and a subscriber whose queue is full misses that event while the others go on.

→ Recipes: [R20. One pack, many recipients, no copies](#r20-one-pack-many-recipients-no-copies) ·
[R21. Receive, store and broadcast in one pass](#r21-receive-store-and-broadcast-in-one-pass) ·
MANUAL: [One pack to many connections](MANUAL.md#one-pack-to-many-connections)

### Serial links: framing that recovers after a damaged byte

*Where it acts: between the **generated connection** and a bare byte stream - a UART, an RS-485 bus, a radio modem*

`Connection.Receiver.Framing` and `Connection.Transmitter.Framing` wrap every pack in a frame - the mark `0xFF`, the pack's bytes with
`0x7F` and `0xFF` escaped at the cost of one bit, a CRC-16 - so a link without framing of its own carries the same protocol as TCP. A pack
is decoded as it arrives and dispatched only when its CRC matches; after a damaged byte the receiver skips to the next mark, and the
fault costs that one pack. Both layers work in place, in the connection's own buffers. C# and Java; TypeScript hosts run on TCP or
WebSocket.

→ Recipe: [R22. Serial links that recover after a damaged byte](#r22-serial-links-that-recover-after-a-damaged-byte) ·
MANUAL: [Serial links and fast recovery: Framing](MANUAL.md#serial-links-and-fast-recovery-framing)

### Streams: any size through fixed buffers

*Where it sits: pack → **field** → wire bytes; at runtime: your source → **socket buffer** → wire → **socket buffer** → your sink*

The runtime never renders a pack into a byte array: the transmitter asks for bytes when the socket buffer has room, and the
receiver consumes them where they land. A `Stream` or `File` field adds a **conduit** to that core: the runtime reads your source
straight into the send buffer and writes received bytes straight into your sink. A gigabyte costs the same buffer as a 64-byte
status pack, and the only size limit is the cap you declare.

![Byte layouts of the conduit framings. Stream: 2-byte little-endian lengths, each followed by up to 65 534 data bytes, closed by 0x0000 or voided by 0xFFFF (ABORT); total not known up front, interruptible. File: one varint total, then exactly that many bytes, checked against S(N) before the first data byte; total required, not interruptible. A trimmed pack is chunked like Stream but not interruptible; a trimmed string uses the File layout with UTF-8 bytes.](docs/img/dev-15-conduit-framing.svg)

- **`Stream`** needs no total up front, may stop at any moment or never stop (a live feed), and costs 2 bytes per chunk.
- **`File`** sends one length up front - denser when the size is known - and the receiver refuses an oversized one before the first
  payload byte.
- **`[S(N)]`** is mandatory on every conduit and enforced on both sides.
- **The end of a chunked stream is visible from the framing alone**, so a relay can forward it and a store can keep it without
  decoding a byte - the ground that virtual connections and trims stand on.
- **An endless feed yields to urgent packs.** A `Stream` ends whenever its source says so, so a live feed sent as a sequence of
  segments lets a command or an alert through between two of them - see
  [R19](#r19-live-video-and-audio-that-stop-for-an-urgent-command).
- **A typed pack too large to hold** streams too: generated abstract on a host, its parser hands your code one value after another
  and the object is never allocated. AdHoc's own server parses every uploaded project this way.

AdHoc's own protocol delivers the files of a project - the sources the agent uploads, the code it receives - as an archive of any
size:

```csharp
[Zstd]
public class SrcZip : StreamFlowAttribute { }      // the on-the-wire format of an archive, defined once

public class FileEntry
{
    [D(+4096)]       string path;                 // '/'-separated path inside the archive
    [S(0x5_000_000)] Stream bytes;                // up to 80 MiB, piped between disk and socket

    public class List
    {
        [D(0xFFFF)] FileEntry[,] files;           // up to 65 535 entries
    }
}

public class Result
{
    string task;
    [SrcZip] FileEntry.List result;               // compressed on the wire, typed at both ends
}
```

Your code supplies a source, a sink and a "has value" answer - up to three generated methods per conduit field. AdHocAgent's own
implementation returns `File.OpenRead(src)` when it sends an entry and a new `FileStream` when it receives one, so every file
streams from disk on upload and to disk on download.

![An archive of any size moving from disk to disk at constant memory, as when AdHocAgent receives generated code. Sender: each file read from disk in blocks, entry after entry, compressed on the fly by the Zstd stage of the SrcZip flow with its fixed staging buffer, into a fixed send buffer, onto the wire as length-prefixed chunks closed by 0x0000. Receiver: a fixed receive buffer, inverse Zstd, and one re-armed FileEntry, a path and a Stream, whose sink, a FileStream, writes each entry to disk. Between the two sides a chart: the memory each side holds - buffers and codec contexts - stays flat while the archive size rises without limit. Caption: FileEntry is a path plus an [S(0x5_000_000)] Stream, up to 80 MiB per entry and 65 535 entries, the cap checked on both sides.](docs/img/readme-archive-stream.svg)

![Layer stack of AdHoc streaming, from your data down to the socket: pack fields, concrete or abstract per host, language, pack and field; Stream and File conduits capped by S(N); transform chains wrapping the serialized bytes of a field, a pack or a connection; trims that cut a chain at one depth for one endpoint; connections, where a chain on Connects wraps one hop and VirtuallyConnects with a relay carries a conversation end to end; and at the bottom one reusable socket buffer per direction, fixed in size whatever the payload.](docs/img/dev-15-streaming-layers.svg)

Every transfer feature in the sections below is one layer over this core, so they compose: a conduit carries a chain, a trim cuts a
chain for one endpoint, and a tunnel is itself a chunked stream.

**Why it is good.** No temp files, no multipart scheme, no chunk protocol of your own and no upload limit beyond the declared cap:
a gigabyte or an endless feed goes through the same buffers as a status pack, and the cap protects every receiver.

**Limits.** A pack with a conduit field is never usable as generated - on every host your code supplies the source and the sink -
and a conduit cannot sit in a collection. A source whose `Read` waits for data holds the transmit loop of the whole connection.

**In the manual:** [Streams](MANUAL.md#streams) · [The streaming model at a glance](MANUAL.md#the-streaming-model-at-a-glance) ·
[Stream framing](MANUAL.md#stream-framing---chunked-and-interruptible) · [File framing](MANUAL.md#file-framing---one-length-up-front) ·
[The size cap `[S(N)]`](MANUAL.md#the-size-cap-of-a-conduit---sn) ·
[Binary, File or Stream](MANUAL.md#binary-file-or-stream---choosing-by-the-shape-of-the-bytes) ·
[Delivering an archive of any size](MANUAL.md#worked-example---delivering-an-archive-of-any-size) ·
[Streaming typed packs](MANUAL.md#streaming-typed-packs)

### Transform chains: compression and encryption as attributes

*Where it sits: **field, pack or connection** → serialized bytes → **stages** → chunked framing → socket buffer*

A chain is a list of byte-to-byte stages written as attributes, in dataflow order: left is the data, right is the wire.
`[Zstd, ChaCha20]` compresses, then encrypts on the sending side, and the receiving side runs the inverse stages in reverse. The
target keeps its type - the chain wraps its serialized bytes, it does not replace them.

![One attribute line, [Zstd(6), ChaCha20] Order secured;, explained: Zstd(6) is the compression, nearer the data; ChaCha20 the cipher, nearer the wire. Transmit row: the typed Order, serialized, through Zstd(6), then ChaCha20, then chunked framing, onto the wire. Receive row, right to left: de-framing, inverse ChaCha20, inverse Zstd, parsing, the typed Order again. Badges C#, Java, TypeScript: the same chain generated for each, no codec code to write. Footer: Zstd is lossless; ChaCha20 hides the bytes but does not detect tampering; a chain on a pack that cannot exceed 1024 bytes loses its Zstd stage automatically, a chain on a field stays as written.](docs/img/readme-chain-line.svg)

```csharp
[Zstd(6), ChaCha20]
public class SealedFast : StreamFlowAttribute { }   // a flow: a named chain, applied as one attribute

public class MacAttribute : StreamStageAttribute { } // a stage of your own: its encode and decode go in one generated file

class Vault
{
    ulong sequence;
    [Zstd(6), ChaCha20] Order  secured;     // compress, then encrypt; still an Order at both ends
    [Zstd, D(+100_000)] string notes;       // one string field
    [S(500_000), Zstd]  Stream feed;        // a raw stream
}

class Orders
{
    [SealedFast] Order payload;             // the same chain, by name
    [Zstd, Mac]  Order audited;             // a custom stage after the compressor
}

[Zstd(3)]                                    // a connection: everything this hop carries
interface AtoB : Connects<A, B> { /* states and branches */ }
```

![Four places a chain can wrap bytes. On a field or a pack: the stages wrap one value inside the stream from Host A to Host B. On each physical Connects: Device to Gateway and Gateway to Cloud each run their own chain, so the Gateway holds both keys and sees plaintext. On VirtuallyConnects<Device, Cloud, Broker>: the chain runs at the two endpoints only, and the Broker relay forwards sealed chunks with no schema and no key. On a [UDP] or Resumable connection: the chain is applied to each pack, with 12 clear bytes of key id and sequence in front.](docs/img/dev-16-chain-scope.svg)

| Stage | What it guarantees, what it costs |
|:--|:--|
| `[Zstd]`, `[Zstd(level)]` | lossless; level 1 to 22, default 3; taken off automatically, with a warning, on packs that cannot exceed 1024 bytes (`_DefaultMaxLengthOf.Uncompressed`) |
| `[ChaCha20]` | confidentiality only - a modified ciphertext is not detected. On a pack or a field it uses the session's keys and writes 12 clear bytes per pack (key id and sequence), so every pack decrypts on its own |
| a stage of your own | any byte transform, with a compressor role, a cipher role or none - for example a MAC for integrity; the generator emits one file per stage, and your code in it survives regeneration |

**An encrypted transport from one key.** A listening host with a static X25519 key pair encrypts every connection it accepts, TCP or
WebSocket: a Noise NK handshake, then authenticated ChaCha20-Poly1305 on every record; the client pins the server's public key.
Without a server key the transport stays plain, at no cost. Every connection of a session adds its key to the session, so a pack
replayed from the journal after a reconnect still decrypts.

The agent checks every chain before anything is generated: at most one compressor and one cipher, the compressor left of the
cipher, and no chain on a `File`, a primitive, an enum, a Value Pack or a collection field.

**Why it is good.** Compression and encryption decisions live in the description, visible in code review and applied identically in
C#, Java and TypeScript. One server key gives an authenticated, encrypted transport, and a chain on a virtual connection keeps
relays from reading what they carry.

**Limits.** A chain on a physical connection ends at each hop: a gateway in the middle sees plaintext - for a middle tier that must
not read, chain the virtual connection. Compress where bytes are known to be redundant; opaque payloads gain nothing. A key and
nonce injected for a stream chain must never encrypt two streams.

**In the manual:** [Transform chains](MANUAL.md#transform-chains) · [Stages and dataflow order](MANUAL.md#stages-and-dataflow-order) ·
[Where a chain may sit](MANUAL.md#where-a-chain-may-sit) · [Built-in stages](MANUAL.md#built-in-stages---zstd-and-chacha20) ·
[What Zstd and ChaCha20 guarantee](MANUAL.md#what-zstd-and-chacha20-guarantee) · [Keys of a cipher](MANUAL.md#keys-of-a-cipher) ·
[Flows](MANUAL.md#flows---a-chain-declared-once) · [Custom stages](MANUAL.md#custom-transform-stages) ·
[Short packs go uncompressed](MANUAL.md#short-packs-go-uncompressed) · [Chains on a connection](MANUAL.md#chains-on-a-connection)

### Trims: stores that keep bytes they never parse

*Where it sits: connection **leg** (a sending host over one connection) → pack → **field → chain → cut** → wire bytes*

A serialized pack has no length header, and a chain is symmetric: a host in the middle can neither find where a pack ends without
parsing it nor avoid running the whole inverse chain. A **trim** solves both on the legs you name: it frames the payload so that its
end is found without parsing, and hands it over as raw bytes.

| Trim | On the leg `E` names |
|:--|:--|
| `[ToStream<E>]` | the sender serializes a typed value; the receiver takes raw bytes |
| `[FromStream<E>]` | the sender supplies raw bytes (from disk, from a database); the receiver gets a typed value |
| `[Stream<To, From>]` | both, at one depth: what a store receives is byte-identical to what it replays |

![A trim names one leg, a sending host over one connection. On the leg IfSendingFrom<Camera, CameraToRecorder>, marked ToStream, the Camera holds a typed Snapshot and the Recorder raw bytes. On the leg IfSendingFrom<Recorder, RecorderToViewer>, marked FromStream, the Recorder holds raw bytes and the Viewer a typed Snapshot. The reverse legs, not named, carry typed Snapshots at both ends. The same bytes travel on every leg; the Recorder stores and replays bytes it never parses. A trimmed string travels as UTF-8 on its cut leg.](docs/img/dev-17-trim-legs.svg)

```csharp
// Camera -> Recorder: typed at the source, raw bytes at the store
public class RecordSnapshot
{
    [ToStream<IfSendingFrom<Camera, CameraToRecorder>>] public Snapshot snapshot;
}

// Recorder -> Viewer: replayed from storage, typed again at the viewer
public class ReplaySnapshot
{
    long recorded_at;
    [FromStream<IfSendingFrom<Recorder, RecorderToViewer>>] public Snapshot snapshot;
}
```

The Recorder needs no `Snapshot` code: `Snapshot` can gain fields and the Recorder is neither touched nor rebuilt. The wire is the
same on every leg - a trim is a code-generation decision, not a wire format - and every leg it does not name keeps a typed value at
both ends.

**The depth of the cut decides what the store runs.** Written among the stages of a chain, the trim marks where unwrapping stops:

```csharp
interface FromProducer : IfSendingFrom<Producer, ProducerToBroker> { }
interface FromBroker   : IfSendingFrom<Broker,   BrokerToConsumer> { }

// the broker keeps the compressed blob: it decrypts its links, never decompresses, never sees an Event
[Zstd, Stream<FromProducer, FromBroker>, ChaCha20]
public class Event { long at; [D(+100)] string topic; [D(65_000)] Binary[,] body; }
```

![Where the trim marker stands decides what the store runs, for three chains read leaf to wire. Cut at the leaf, [Stream<To,From>, Zstd, ChaCha20]: the store keeps the plain serialized pack, runs inverse ChaCha20 and inverse Zstd, and holds the key and both stages. Cut between the stages, [Zstd, Stream<To,From>, ChaCha20]: it keeps the compressed blob, runs inverse ChaCha20 only and holds the key. Cut at the wire, [Zstd, ToStream<E>], AdHoc's own Project.source: it keeps the wire bytes, runs nothing and holds nothing. Footer: every other leg runs the whole chain; keep the built-in ChaCha20 right of a cut whose bytes are replayed.](docs/img/dev-17-trim-depth.svg)

The producer compresses once, the broker stores and serves compressed bytes, and N consumers cost one compression. No row of the
picture needs the payload's schema. A trimmed `string` travels as UTF-8 on its cut leg, so a store writes text and logs straight to
disk. The recipes: [object storage](#r1-object-storage-that-keeps-bytes-it-cannot-read) and
[a log broker](#r11-a-log-broker-that-compresses-once-and-fans-out).

**Why it is good.** The object leaves the producer typed, lands in the store as plain bytes - still compressed if you cut after the
compressor - leaves the store as the same bytes, and the consumer receives exactly the object that was sent. The store never
parses, never re-encodes, needs no schema and is not rebuilt when the payload changes.

**Limits.** The stages left of the cut become a stored format: change them and data stored earlier stops decoding - name them in a
flow, and add a new flow beside the old one. The built-in `[ChaCha20]` seals bytes for one connection, so a store that must keep
what it cannot read needs a cipher stage of your own whose key the producer and the consumer share as a runtime-injected parameter;
then `[Zstd, MyCipher, Stream<To, From>]` keeps the exact wire bytes, runs nothing and holds no key. Bytes injected at a
`FromStream` are not checked.

**In the manual:** [Trims](MANUAL.md#trims) · [Why a stored payload needs a trim](MANUAL.md#why-a-stored-payload-needs-a-trim) ·
[The three trims](MANUAL.md#the-three-trims-and-who-holds-what) · [Trimmed strings](MANUAL.md#trimmed-strings) ·
[The depth decides what the store must run](MANUAL.md#the-depth-decides-what-the-store-must-run) ·
[A schema-blind recorder](MANUAL.md#worked-example---a-schema-blind-recorder) ·
[A schema-blind log broker](MANUAL.md#worked-example---a-schema-blind-log-broker) · [Trim pitfalls](MANUAL.md#trim-pitfalls)

### Virtual connections and relays: middle tiers that cannot read

*Where it sits: project → host → **connection, virtual, carried by the relay hosts on a PATH** → actor → state → pack → bytes in a chunked tunnel*

`VirtuallyConnects<L, R, PATH>` joins two hosts that share no direct link. They behave as directly connected - packs, actors, state
machines, RPC - while their bytes ride physical connections through the relay hosts on `PATH`. Every relay host gets a generated
**Relay** that forwards the tunnel without decoding it, assembling it or knowing its structure.

![A Viewer in the browser, in TypeScript, and a Server in Java joined by a dashed arc, VirtuallyConnects<Viewer, Server, Hub>: talk as if wired - packs, actors, state machines, RPC. The physical legs ViewerToHub and HubToServer run through the Hub, whose generated Relay holds no schema, no keys and one region of memory, the same for 64 bytes or 1 GB. Sealed chunks travel both legs; [Zstd(6), ChaCha20] runs at the Viewer, the inverse at the Server. Under the Hub: a frame is wire id, tunnel_id, chunks; up to 256 tunnels per link. Footer: change the packs and the Hub is not rebuilt; backpressure runs end to end; ABORT ends one tunnel, not the link; ChaCha20 keeps the relay from reading, not from altering.](docs/img/readme-virtual-connection.svg)

```csharp
// Viewer and Server talk as if wired; every byte rides ViewerToHub and HubToServer through Hub
[Zstd(6), ChaCha20]                          // end to end: Hub forwards sealed chunks
interface ViewerToServer : VirtuallyConnects<Viewer, Server, Hub>
{
    [Idle(120)]   // a lease: the Viewer renews it with WatchSub
    [Grouped]     // the Server gathers the watches of all Viewers to send one event to all of them
    interface Watch : Actor
    {
        int Actor.MaxActiveInstances => 4;

        [L____________<Live, WatchSub>]
        struct Start { }

        [____________r<WatchEvent>]
        [l____________<WatchSub>]
        [L____________<End, WatchUnsub>]
        struct Live { }
    }
}

// an empty body: a raw byte pipe for a protocol of the two endpoints' own
interface DeviceTunnel : VirtuallyConnects<Device, Server, Hub> { }
```

- **Constant memory.** A relay holds at most one region between two socket buffers: a 1 GB tunnel and a 64-byte one cost the same.
- **No schema.** It depends only on the framing: change the packs and the relay host is not rebuilt.
- **End-to-end backpressure.** A slow outbound side stops inbound reads, and TCP pushes back to the original producer; there is no
  user-space queue.
- **Sealed.** A chain on the virtual connection runs at the two endpoints only; the relay has no stage code and no keys.
- **Many tunnels on one link.** Up to `MaxTunnels` (default 256, a 1-byte `tunnel_id`); `ABORT` ends one tunnel, not the shared
  link. Your code in one generated region maps a `tunnel_id` to its outbound connection.
- **Routes found for you.** `PATH` may list several relays in any order; the agent finds the one physical route, or stops and says
  why.

![A decision card for the host in the middle, four rows. Relay: route bytes and never read them; holds no schema, no keys, one region of memory; VirtuallyConnects<L, R, PATH> with a generated Relay. Trim: keep a payload and replay it later; holds bytes at the depth you choose, no schema; ToStream, FromStream, Stream<To, From>. Smart middle: inspect, transform, filter or fan one stream out; holds the schema, one mirror buffer, a slicer per subscriber; built by you from the stream API. Cascading RPC: answer a call by calling further on; a replier context and a caller instance per call; one RPC method declared on both connections.](docs/img/readme-middle-roles.svg)

**Why it is good.** The middle tier stops being a liability: it holds no schema, so payload changes never redeploy it; it holds no
keys for an end-to-end chain, so it cannot read the conversation; and it runs at constant memory for any payload, endless feeds
included. A browser reaches a private backend, and a device behind NAT a cloud service, through a gateway that never parses.

**Limits.** `[ChaCha20]` keeps a relay from reading, not from altering: add a MAC stage of your own for integrity. While a relay
waits on a slow outbound connection, its inbound connection receives nothing else. A relay cannot keep, inspect, filter or fan out
what it carries: to keep and replay, make the host in the middle an endpoint with a trim; to inspect or fan out, build a smart middle.

**In the manual:** [Virtual connections and relays](MANUAL.md#virtual-connections-and-relays) ·
[Declaring a virtual connection](MANUAL.md#declaring-a-virtual-connection) ·
[Tunnel-only and structured](MANUAL.md#tunnel-only-and-structured-virtual-connections) ·
[How the agent finds the route](MANUAL.md#how-the-agent-finds-the-route) · [The tunnel on the wire](MANUAL.md#the-tunnel-on-the-wire) ·
[Relay](MANUAL.md#relay-the-runtime-primitive-on-a-relay-host) · [Transform chains over a tunnel](MANUAL.md#transform-chains-over-a-tunnel) ·
[Choosing what the middle tier does](MANUAL.md#choosing-what-the-middle-tier-does)

### Multiplexing: one port, many peers, many generations

*Where it sits: deployment - listening host → **one port for several connections** → the first byte names the dialing host → an ordinary connection*

`Multiplex<(C1, C2, ...)>` lets a host serve several of its connections on one port. Every socket opens with one byte - the uid of
the host that dials - and from then on it is an ordinary link of that connection. The byte is paid once per socket, never per pack;
over an encrypted transport it is the first byte after the handshake, never sent in clear.

![Four peer hosts dial one host, Fleet, through a single port 443: Vehicle on firmware 3.x and VehicleLegacy on firmware 2.x - two generations, two hosts, two connections - Dashboard, an operator console, and Service, a diagnostic tool. Behind the port, one TCP/WebSocket server hands every socket to FleetPort, which reads the first byte, the dialing host's uid, and seats the socket on a pooled Telemetry, TelemetryLegacy, Console or Diagnostics connection.](docs/img/multiplex-one-port.svg)

```csharp
struct Vehicle       : Host { }   // firmware 3.x
struct VehicleLegacy : Host { }   // firmware 2.x, still in the field
struct Dashboard     : Host { }   // operator console
struct Service       : Host { }   // diagnostic tool
struct Fleet         : Host { }   // the backend

interface Telemetry       : Connects<Vehicle,       Fleet> { /* states and branches */ }
interface TelemetryLegacy : Connects<VehicleLegacy, Fleet> { /* states and branches */ }
interface Console         : Connects<Dashboard,     Fleet> { /* states and branches */ }
interface Diagnostics     : Connects<Service,       Fleet> { /* states and branches */ }

interface FleetPort : Multiplex<(Telemetry, TelemetryLegacy, Console, Diagnostics)> { }
```

The listening server takes the generated `FleetPort` as the function from the first byte to a connection; a dialing client sets its
`mux` flag. Nothing in the connections refers to the multiplexer: it changes how they are deployed, not what they are.

- **One address, one certificate, one firewall or NAT rule** for every kind of peer.
- **Versioning without version fields.** A new firmware or client generation is a new host with its own connection. The old one
  stays frozen and keeps working, the new one changes freely, both dial the same port, and a handler never asks which version it
  talks to - the first byte settled that. Retirement is a deletion. A new server generation works the same way: two multiplexers
  composed in one process, behind one port.
- **Declared once, chosen at deployment.** The same generated code serves a port of its own or a shared one.
- **Private backends behind a public edge.** A multiplexer may list a connection its host is not a party of: the edge accepts the
  socket and carries that connection in a tunnel to a backend that is never exposed.

![One host, Fleet, deployed three ways: a port and a server instance per connection, with no multiplexer used; one multiplexed port 443 where FleetPort's first byte picks Telemetry or Console; and a mix, where the public Vehicle and Dashboard share port 443 while Service keeps port 8443 for Diagnostics, reachable from the service LAN only.](docs/img/multiplex-shapes.svg)

![The relayed case of a multiplexer: Browser and Admin dial port 443 of Edge and land on Console and AdminLink, which Edge serves itself; Mobile dials the same port with the byte Mobile.uid for Query, a connection between Mobile and Backend, and Edge carries Query's bytes in a tunnel over EdgeBackend, its one physical connection to the Backend, which is reachable only from Edge.](docs/img/dev-19-mux-relayed.svg)

**Why it is good.** Version handling moves out of every message and every handler into one byte per socket and one declaration: old
devices keep speaking the dialect they were built with, new ones change freely, and port sharing is an operations decision, not a
code change.

**Limits.** Both sides must agree whether a port is multiplexed. A uid is one byte, so a project with the projects it extends holds
at most 256 hosts. Retire a generation only when none of its devices dials any more, because its uid is freed for the next new
host. A multiplexed port is TCP or WebSocket - a UDP connection keeps a port of its own - and a TypeScript host only dials.

**In the manual:** [Multiplexing](MANUAL.md#multiplexing) · [The first byte](MANUAL.md#the-first-byte-of-a-multiplexed-socket) ·
[Declared once, chosen at deployment](MANUAL.md#declared-once-chosen-at-deployment) ·
[Several generations behind one port](MANUAL.md#several-kinds-of-peer-several-generations-behind-one-port) ·
[Server generations](MANUAL.md#server-generations-behind-one-port) · [Wiring a multiplexed port](MANUAL.md#wiring-a-multiplexed-port) ·
[Relayed connections](MANUAL.md#relayed-connections-in-a-multiplexer) · [A new generation is a new host](MANUAL.md#a-new-generation-is-a-new-host)

### Identity: numbers instead of field tags

Every entity of a description - project, host, connection, actor, state, branch, pack - gets a **number** the first time AdHocAgent
meets it. AdHocAgent keeps all the numbers in two doc comments at the top of your file, the **numbers tables**; the declarations below
them carry none. Fields get no number at all: a field is known by its place in the pack.

![Two columns. The left column, titled "numbered - identity", lists project, host, connection, actor, state, branch and pack; each carries a small number badge, a note says that renaming, moving or reordering these declarations changes nothing, and arrows lead from the badges to what is keyed by the number: the uid byte of a host, the custom code regions of your code in generated files, the wire id of a pack. The right column, titled "positional - order", shows the pack PositionV2 with the fields float lat and float lon and an arrow to a strip of wire bytes in the same order - id, lat, lon; with the two fields swapped the strip reads id, lon, lat, marked as another pack on the wire.](docs/img/dev-20-identity-vs-order.svg)

- **Refactor freely.** Rename a pack in your IDE, move a host to the end of the file, sort the connections: the number stays, and with
  it the pack's wire id, the host's uid byte and the regions of generated files that hold your code.
- **Nothing to hand-manage.** No field tags to assign, reserve or retire. AdHocAgent writes the tables, keeps them sorted and rewrites
  them when a number changes. The one part that is yours: tags after a pack's numbers - words or emojis that branches can select packs by.
- **Small wire ids.** The wire id is separate from the identity: a new transmittable pack takes the lowest free id, and the receiver
  reads every pack id of a connection in the same fixed number of bytes, enough for the largest one.
- **Order is the wire.** The other side of the coin: swap two fields of one kind - two numbers, say - and the pack is another pack on
  the wire. See the next section.

After the first run, the top of the `FleetProtocol` description of the next section reads like this (base256 characters after `/>`:
`ÿ` = 0, `Ā` = 1, `ā` = 2; the decimal column is the wire id; the project's own number differs for every project):

```csharp
namespace com.fleet
{
    /** packs
        <see cref='Alarm'/>ā         2
        <see cref='Position'/>Ā      1
        <see cref='PositionV2'/>ÿ    0
    */
    /** project
        <see cref='FleetProtocol'/>ěƱǊŻč

        hosts
        <see cref='Fleet'/>ÿ
        <see cref='VehicleLegacy'/>Ā
        <see cref='Vehicle'/>ā

        connections
        <see cref='TelemetryLegacy'/>ÿ
        <see cref='Telemetry'/>Ā
    */
    public interface FleetProtocol
```

Keep the rewritten file under version control, rename with the IDE, and never edit a number by hand.

→ MANUAL: [Numbers tables](MANUAL.md#numbers-tables) ·
[Identity in a table, position in a pack](MANUAL.md#identity-in-a-table-position-in-a-pack) ·
[The wire id of a pack](MANUAL.md#the-wire-id-of-a-pack) · [Tags of a pack](MANUAL.md#tags-of-a-pack) ·
[Pitfalls of the numbers tables](MANUAL.md#pitfalls-of-the-numbers-tables)

### Evolution: a new generation is a new host

A pack on the wire is its wire id followed by its fields in declaration order; nothing in the bytes says which edit of the description
produced them. So AdHoc does not evolve a pack in place. A generation of a peer that is deployed and cannot be updated is **frozen**,
and the next generation is a **new host with its own connection**, declared beside the old one. A multiplexer puts both on one port:
the first byte of each socket - the dialing host's uid - hands it to the connection of its own generation.

![Two columns inside one description. On the left, the frozen generation: host VehicleLegacy with uid 1, connection TelemetryLegacy and pack PositionV2, drawn with a lock and the note "deployed, never edited - an edit here reaches devices in the field". On the right, the free generation: host Vehicle with uid 2, connection Telemetry and packs Position and Alarm, with the note "free to change: add fields, change types, add states". Both connections end at the backend host Fleet, where one port with the multiplexer FleetPort reads the first byte of each socket and hands it to TelemetryLegacy or Telemetry. A dashed box around the frozen column is labeled "AdHocAgent .cs~ must not list VehicleLegacy".](docs/img/dev-21-generations.svg)

```csharp
using org.unirail.Meta;

namespace com.fleet
{
    public interface FleetProtocol
    {
        class PositionV2 { float lat; float lon; }                 // what firmware 2.x was built against
        class Position   { double lat; double lon; float speed; }  // firmware 3.x, free to change
        class Alarm      { string text; }

        ///<see cref = 'InJAVA'/>
        struct Fleet : Host { }          // the backend

        ///<see cref = 'InCS'/>
        struct VehicleLegacy : Host { }  // firmware 2.x: frozen, never edited

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

        interface FleetPort : Multiplex<(Telemetry, TelemetryLegacy)> { }  // both generations on one port
    }
}
```

- **No version fields, no version branches.** Each generation has its own generated code and its own state machine. A handler never
  asks which firmware it talks to: the connection - or the first byte on a shared port - settled that before the first pack arrived.
- **The old dialect is frozen, the new one is free.** `Telemetry` may drop a pack, rename a state or change a field's type with no
  thought for what is still in the field.
- **Retirement is a deletion** of the old host, connection and packs, with their lines in the numbers tables
  ([one caveat](MANUAL.md#a-new-generation-is-a-new-host) applies to a port that would be left with one connection).
- **Honest scope.** Every generation you still serve is code you build and deploy. That fits a population you know - devices you
  shipped, peers you can list - not an open ecosystem with no bound on its generations.

Which edits make a new protocol, and which do not:

![Which edits change the wire, in two columns under the line "a pack on the wire = wire id + fields in declaration order; entities have numbers, fields have places". Left, in green, "the wire stays the same - old and new peers still understand each other": rename a pack, host, connection, actor or state in the IDE; rename a field whose type and place are kept; reorder declarations in the file; edit a comment, which reaches no host; change a custom attribute, which becomes a constant in the code; add a language to a host; move a primitive field past a string, which leaves both sequences unchanged. Right, in orange, "the wire changes - regenerate and deploy both ends together": add or remove a field at any position, change a field's type, range or encoding, or swap two fields of one sequence such as lat and lon - each makes another pack; change the transform chain of a pack - other bytes; a branch starts or stops sending a pack - other wire ids; change an enum or a constant set - other values; change a connection's states or branches - other flow. A blue strip at the bottom: AdHocAgent .\FleetProtocol.cs~ lists every host an edit reaches, before anything is generated, and beside a padlock: a frozen generation must not be in the list.](docs/img/readme-edit-impact.svg)

The recipe that builds on this - several client and device generations behind one public port - is
[R2. Versioning without version fields](#r2-versioning-without-version-fields).

→ MANUAL: [Evolving a protocol](MANUAL.md#evolving-a-protocol) ·
[What changes the wire and what does not](MANUAL.md#what-changes-the-wire-and-what-does-not) ·
[A new generation is a new host](MANUAL.md#a-new-generation-is-a-new-host) ·
[When separate generations fit](MANUAL.md#when-separate-generations-fit) ·
[Several kinds of peer, several generations behind one port](MANUAL.md#several-kinds-of-peer-several-generations-behind-one-port)

### Impact report: which hosts an edit reaches

`AdHocAgent .\FleetProtocol.cs~` compares the description with its previous version - by default the newest one a deployment
saved in its backup that differs from the current file - and writes one HTML page that answers one question: **which hosts have
to be updated, and why**. It runs on your machine with the same parser as code generation: nothing is uploaded, no UUID is needed,
and neither version is written to.

![Flow from left to right. Two description boxes - "current: FleetProtocol.cs plus its imports" and "previous: a file, a folder or the .description folder of the newest differing deployment backup" - both enter one box "AdHocAgent .cs~: the .cs task's parser, local, nothing uploaded, neither file is written, no UUID needed". From it two outputs: a console box listing "have to be updated (3): Fleet, Vehicle, VehicleLegacy", and a page box "FleetProtocol.changes.html" with four tiles labeled update, new, removed, not reached and folded sections. A hidden strip at the bottom of the page box, labeled "script id adhoc-impact: one line of JSON", leads by an arrow to a box "CI script: jq / ConvertFrom-Json - fail when a frozen host is in update or removed".](docs/img/dev-21-impact-report.svg)

- **A verdict per host** - update, new, removed, not reached - and behind it a card per reason: what changed, how the host uses it
  ("sends to Fleet over Telemetry"), the declaration before and after with the changed part marked, and a link to the line.
- **Renames are recognized** through the numbers tables and reported as renamed, not as removed and added. Edited comments are listed
  apart and reach no host.
- **Built for CI.** The same answer sits in the page as one line of JSON, so a pipeline can fail the build when a frozen generation is
  touched:

```shell
AdHocAgent ./FleetProtocol.cs~ > impact.log    # redirected: no browser
grep -o '<script type="application/json" id="adhoc-impact">.*</script>' FleetProtocol.changes.html \
  | sed 's/<[^>]*>//g' > impact.json
jq -e '(.update + .removed) | index("VehicleLegacy") | not' impact.json \
  || { echo "the frozen generation VehicleLegacy is reached"; exit 1; }
```

Swap `lat` and `lon` in `PositionV2` and add a field to `Position`: the page lists `Fleet`, `Vehicle` and `VehicleLegacy`, notes that
fields changed places in one pack - and the frozen host in the list says the edit must be undone, before a line of code is generated.

→ MANUAL: [The .cs~ impact report](MANUAL.md#the-cs-impact-report) ·
[What makes a host one to update](MANUAL.md#what-makes-a-host-one-to-update) ·
[The report for scripts: the adhoc-impact JSON](MANUAL.md#the-report-for-scripts-the-adhoc-impact-json) ·
[Guarding a frozen generation in a pipeline](MANUAL.md#guarding-a-frozen-generation-in-a-pipeline)

### Reuse: extend, import, modify

A description can stand on another one - a shared library of packs, a vendor's public protocol, AdHoc's own protocol - without copying
it and without editing its declarations:

| Mechanism | Declared as | What it does |
|:--|:--|:--|
| **extend** a project | `interface MyProject : Library` | takes in every host, connection, multiplexer, pack, enum and constant set of `Library` and of the projects it extends |
| **import** one declaration | `_<...>` in the project's base list | adds one connection, or broadcasts an enum or a constant set to every host |
| **modify** an imported declaration | `class M : Modify<Target> { }` | edits a pack's fields, replaces its attributes, removes members of an enum or a constant set, adds languages to a host |

![A library project and a project that extends it. At the bottom, interface Library in Library.cs, someone else's description whose declarations are not edited: hosts Server uid 0 and Client uid 1, connection Link, packs Hello id 0, Stamp id 1, Info id 2 and Trace marked Zstd. A thick blue arrow up - "extends: everything comes in", hosts, connections, packs, enums, with their numbers and wire ids - to interface MyProject : Library in MyProject.cs, drawn as yours: a new host Metrics with uid 2, a new pack Sample with id 3, a new connection ServerToMetrics from Server to Metrics, and two modifiers, TraceCut : Modify<Trace> with hops removed and TraceChain : Modify<Trace> with Zstd(9). Dashed arrows labeled "Modify<> edits in place" lead from the two modifiers to the right, the composed project as generated: hosts Server, Client and Metrics; connections Link and ServerToMetrics; packs Hello 0, Stamp 1, Info 2 and Sample 3, the new ones in green; Trace with the field origin only, hops struck out, compressed with Zstd(9) for every host - Library's own Client and Server too. Below it: fields merge, attributes replace, the topmost layer wins; AdHocAgent logs every replacement, from Zstd to Zstd(9). Along the bottom, three badges: ": Library" extends a whole project, "_<X>" imports one connection or broadcasts an enum or constant set, "Modify<T>" edits an imported pack, enum, constant set or host.](docs/img/readme-reuse.svg)

```csharp
public interface MyProject : com.lib.Library               // everything of Library comes in
{
    class Sample { long value; }

    ///<see cref = 'InJAVA'/>
    struct Metrics : Host { }                              // a host of your own

    interface ServerToMetrics : Connects<Server, Metrics>  // joins Library's Server
    {
        [l____________<(Sample, Stamp)>]                   // sends an imported pack
        struct Start { }
    }

    /// <see cref="com.lib.Library.Trace.hops"/>-
    class TraceCut : Modify<Trace> { }                     // Trace loses hops - for every host

    [Zstd(9)]
    class TraceChain : Modify<Trace> { }                   // and is compressed harder
}
```

- **Identities survive the composition.** Imported hosts and packs keep their numbers and wire ids; your hosts' uids and your packs'
  wire ids continue after the largest imported ones (`Metrics` is uid 2, `Sample` takes wire id 3).
- **Layers, not forks.** Fields merge, attributes replace, and the topmost layer wins. Two layers that do not import each other and
  both replace one pack's attributes are refused: composition order never decides silently. AdHocAgent logs every replacement,
  `["Zstd"] -> ["Zstd(9)"]`.
- **Proven on itself.** The description of AdHoc's own generator service extends AdHoc's public protocol,
  [`AdhocProtocol.cs`](AdhocProtocol.cs), with internal hosts - monitoring, testing, formatting, updating - and one generated `Server`
  serves both protocols.
- **Know the edges.** Imports are not filtered (everything comes in); connections and states of an imported project cannot be
  modified; and AdHocAgent keeps the imported project's numbers tables up to date, so keep its file writable and under version control.

→ MANUAL: [Reusing descriptions](MANUAL.md#reusing-descriptions) · [Extending a project](MANUAL.md#extending-a-project) ·
[Numbers across the imported projects](MANUAL.md#numbers-across-the-imported-projects) ·
[Importing single declarations with `_<>`](MANUAL.md#importing-single-declarations-with-_) ·
[Modifying imported declarations: `Modify<>`](MANUAL.md#modifying-imported-declarations-modify) ·
[Example: a backend extension of AdHoc's own protocol](MANUAL.md#example-a-backend-extension-of-adhocs-own-protocol)

### AdHocAgent: one command-line tool for every task

AdHocAgent is the .NET utility through which you use AdHoc - on your machine or as a step of a build pipeline. Its first argument
picks the task:

| Command | Task | Leaves your machine |
|:--|:--|:--|
| `AdHocAgent` | print the help and write the template `MyProtocolDescription.cs` | - |
| `AdHocAgent <your-uuid>` | store your personal UUID in `AdHocAgent.toml` | - |
| `AdHocAgent .\MyProtocol.cs` | check the description, upload it, receive the generated code, deploy it | **the description**, after it passed the local check |
| `AdHocAgent .\MyProtocol.cs~` | the [impact report](#impact-report-which-hosts-an-edit-reaches) | - |
| `AdHocAgent .\MyProtocol.md` | deploy the code already received again, after you changed the routes | - |
| `AdHocAgent .\MyProtocol.cs?` | the [AdHoc Observer](#adhoc-observer-the-description-as-live-diagrams) | - |

![AdHocAgent's first argument picks the task. Six command lines in a terminal column on the left, each with an arrow to its result: AdHocAgent with no arguments prints the help and writes the template MyProtocolDescription.cs; AdHocAgent followed by your UUID stores PersonalVolatileUUID in AdHocAgent.toml; AdHocAgent .\MyProtocol.cs, drawn in blue, checks the description locally and uploads it, and the code goes to the received folder and is deployed; AdHocAgent .\MyProtocol.cs~ writes the impact report MyProtocol.changes.html plus JSON for scripts; AdHocAgent .\MyProtocol.md redeploys the received folder after you edited the routes; AdHocAgent .\MyProtocol.cs? starts the AdHoc Observer, interactive diagrams at localhost:4321. Every result except the .cs one carries a green "local" badge. A dashed boundary separates "your machine" from "network"; the only lines that cross it join the .cs result to a yellow box, the generator, a cloud service that tests the code: the description goes out, the generated code comes back - only the .cs task crosses this line, after the local check. A yellow warning strip at the bottom: AdHocAgent MyProtocol.cs without a directory part is taken as a UUID and nothing is generated - write .\ or ./ before the name. A small note: ADHOC_PARSE_ONLY=1 in CI checks a pull request without uploading.](docs/img/readme-agent-tasks.svg)

> [!WARNING]
> Give the task file **with a directory part**: `.\MyProtocol.cs`, `./MyProtocol.cs` or a full path. A first argument with neither `\`
> nor `/` is taken as a UUID - `AdHocAgent MyProtocol.cs` generates nothing.

- **Checked before it leaves.** The description is compiled and checked by AdHocAgent's parser first; an error stops the run with the
  file, the line and what to fix. A description with an error never reaches the server.
- **No login, no password.** Access is a personal **volatile UUID** from a free sign-up on GitHub. The server may replace it during a
  generation; AdHocAgent stores the new one by itself. Code generation is free.
- **Tested on the server.** The generator tests the code before it returns it - from none to the full set of checks, depending on its
  load - and says how far this result was tested. It is a service, not a replacement for your own tests.
- **Made for pipelines.** `ADHOC_PARSE_ONLY=1` validates a pull request without uploading; redirected output prints plain progress
  lines; `NO_COLOR` switches colors off. Persist `AdHocAgent.toml` between runs - it holds the current UUID - and read the log, not only
  the exit code.
- **Open source projects** can be listed in the catalog of projects that use AdHoc: a public GitHub repository with at least 500 stars,
  an `adhoc` folder that holds a copy of every uploaded file, and topical tags passed on the command line.
- **An AdHoc application itself.** AdHocAgent talks to the generator over the `Communication` connection of AdHoc's own description,
  `AdhocProtocol.cs` - with a real state machine of versions, login and upload.

Have a schema in another language? Standalone [converters to AdHoc protocol](https://github.com/AdHoc-Protocol#converters-to-adhoc-protocol)
turn it into an AdHoc description to start from.

→ MANUAL: [AdHocAgent](MANUAL.md#adhocagent) · [How AdHocAgent chooses the task](MANUAL.md#how-adhocagent-chooses-the-task) ·
[What a .cs run does, step by step](MANUAL.md#what-a-cs-run-does-step-by-step) ·
[Progress and server-side testing](MANUAL.md#progress-and-server-side-testing-of-generated-code) · [UUID](MANUAL.md#uuid) ·
[Running AdHocAgent in a pipeline](MANUAL.md#running-adhocagent-in-a-pipeline) · [Open source](MANUAL.md#open-source) ·
[Descriptions from other schema languages](MANUAL.md#descriptions-from-other-schema-languages)

### Deployment: routes, smart merge, backups

Generated code is not a one-shot scaffold. After every generation AdHocAgent deploys the received code into your projects by a
Markdown **instructions file** that it writes once and you edit: a route at the end of each host's line, `⛔` on the hosts you do not
use.

```markdown
- 📁[InCS](/D:/Generated/MyProtocol/InCS)
  - 📁[Server](/D:/Generated/MyProtocol/InCS/Server) [](/D:/Projects/server/protocol/)
- 📁[InJAVA](/D:/Generated/MyProtocol/InJAVA)
  - 📁[Device](/D:/Generated/MyProtocol/InJAVA/Device) ⛔ not used yet
- 📁[InTS](/D:/Generated/MyProtocol/InTS)
  - 📁[Browser](/D:/Generated/MyProtocol/InTS/Browser) [](/D:/Projects/web/src/protocol/)
```

![The regeneration loop, clockwise in five numbered stations: 1, edit MyProtocol.cs, your description; 2, AdHocAgent .\MyProtocol.cs~ tells locally which hosts the edit reaches; 3, AdHocAgent .\MyProtocol.cs checks, uploads, and the generator generates and tests; 4, deployment: routes from MyProtocol.md, processing steps such as formatters, smart merge into your files, backup and restore scripts; 5, build and run your project - gen, lib and your code - and back to editing. In the middle of the loop, one injection point of a generated file - #region> Actor, your code, #endregion> followed by the entity's number Āÿÿ and .Actor - labeled "keyed by the entity's number: survives regeneration and renames; an edited generated block is kept, a todo note if the generator changed it", with a dashed line to the smart merge step. A red callout under the deployment station: a region with your code is gone from the new file - the deployment stops before writing and asks Proceed with the merge? (y/N). A purple callout joined to the backup step: the backup folder MyProtocol_5 with restore.bat, restore.ps1, restore.sh and .description, the source description. A legend of the colours: yours, local check, generator, deployment, backup, stop prompt.](docs/img/readme-regenerate-loop.svg)

- **Smart merge.** A generated file is not copied over yours. The new version is the template, and your code in its **injection
  points** - regions keyed by entity numbers - is carried over region by region. A generated block you edited is kept; when the
  generator changes that block, a todo note under it shows the new version.
- **No silent loss.** When a region that holds your code is gone from the new file - its actor was removed, say - the deployment stops
  and asks before anything is written.
- **Every deployment is reversible.** The files it overwrites or deletes are backed up first, together with the description the code
  was generated from and `restore.bat`, `restore.ps1` and `restore.sh`.
- **Your toolchain in the loop.** Processing steps - a formatter selected by a regular expression over file paths - and
  before/after hooks are written into the same Markdown file.

<details>
<summary>How smart merge treats each block of an injection point</summary>

![One injection point across a deployment, in three columns - the received version, your file before, your file after: an untouched generated block is replaced by the generator's new version; a block you edited is kept with a pencil mark in its tag and a todo note carrying the generator's new version; a block you deleted comes back after your last block; a block the generator dropped that holds your code stays commented out under a todo note; your own lines between blocks pass unchanged; below, a region whose id is gone from the received file leads to the orphaned code prompt Proceed with the merge? (y/N).](docs/img/dev-25-smart-merge.svg)

</details>

> [!WARNING]
> A destination folder belongs to the generator: a file in it that the deployment does not write is backed up and deleted. Route each
> host to a folder that holds generated code only - a `protocol` folder beside your sources - never to a project root.

→ MANUAL: [Deployment and smart merge](MANUAL.md#deployment-and-smart-merge) ·
[Routes: where received files go](MANUAL.md#routes-where-received-files-go) · [Smart merge](MANUAL.md#smart-merge) ·
[Injection points](MANUAL.md#injection-points) · [Orphaned code protection](MANUAL.md#orphaned-code-protection) ·
[Processing steps before deployment](MANUAL.md#processing-steps-before-deployment) ·
[Automated backups and restoration](MANUAL.md#automated-backups-and-restoration)

### AdHoc Observer: the description as live diagrams

A big protocol is hard to hold in your head: who talks to whom, what each message carries, how each conversation flows. The **AdHoc
Observer** draws your description as interactive diagrams that follow your edits and lead back to the source line. It is an auxiliary
tool of AdHocAgent and runs entirely on your machine: nothing is uploaded and no UUID is needed.

```shell
AdHocAgent .\MyProtocol.cs?                       # Windows; ./MyProtocol.cs? elsewhere
AdHocAgent .\MyProtocol.cs? .\Library.cs .\work   # with imported files, and an output folder for the layout
```

AdHocAgent then waits for a browser at `http://localhost:4321/`; open that address.

![The AdHoc Observer at work. On the left, the description file MyProtocol.cs with its imports, and the command AdHocAgent .\MyProtocol.cs? with three green badges: local, nothing uploaded, no UUID; below them a note: the browser receives the parsed model, the same Project pack the generator gets. A blue double arrow labeled "WebSocket - AdHoc packs" joins the command to a browser window at localhost:4321; an orange arrow from the file to the browser reads "save → redraw", and a blue arrow back to the file reads "open in your IDE". In the window: a sidebar with a search field, a collapsible tree of hosts, packs and connections, and a Save Diagram button; a canvas with the hosts Vehicle and VehicleLegacy, generated in C#, and Fleet, generated in Java, as boxes listing their packs, joined by the connections Telemetry and TelemetryLegacy; a pop-up "right-click a connection: its state machine" showing the state Start with a loop labeled "l: Position, Alarm"; a pop-up "left-click a pack: its fields" listing Position: double lat, double lon, float speed; and a yellow sticker, "double-click the background". Three callouts below: save the file and the diagram follows - the page asks, AdHocAgent re-reads the files, parse errors are shown in the page; jump to the source - your IDE opens at the line and character, set by show_code_exe and show_code_args; a folder icon - the output folder keeps the layout and the stickers, keyed by entity numbers, so renames keep the layout.](docs/img/readme-observer.svg)

| In the browser | What you get |
|:--|:--|
| the graph | all hosts, the packs each one handles, and the connections between them |
| right-click a connection | its state machine in a pop-up: states and branches |
| left-click a pack | its fields, their types and nested structures |
| the sidebar | a searchable, collapsible tree of hosts, packs and connections |
| double-click the background | a sticker: a rich-text note on the diagram |
| **Save Diagram** in the sidebar | node positions, pan and zoom saved to `layout` in the output folder |

- **Live.** Save the description and the diagram follows: the page asks AdHocAgent whether anything changed, and AdHocAgent re-reads
  the files and sends the new model. A description that does not parse shows its error in the page.
- **One click to the source.** With `show_code_exe` and `show_code_args` in `AdHocAgent.toml`, the Observer opens your IDE at the
  declaration - placeholders `<path to file>`, `<line number>`, `<char number>`:

  ```toml
  show_code_exe = "X:/VSCode/Code.exe"
  show_code_args = "--goto <path to file>:<line number>:<char number>"
  ```
- **A layout that survives refactoring.** Positions are keyed by the same numbers the protocol uses, so renaming or moving a
  declaration keeps its place on the diagram. Stickers are saved as HTML files next to the layout; a browser closed without saving
  leaves its work in `unsaved/` of the output folder - move it up one folder to recover it.
- **AdHoc on itself.** The Observer is a host of AdHoc's own description: it receives the parsed model - the same `Project` pack the
  generator gets - over the `ObserverCommunication` connection, and the layout file is AdHoc packs written through the `SaveLayout`
  connection: packs used as a file format.

Like a `.cs` run, an Observer run keeps the numbers tables of the description up to date.

<details>
<summary>A screenshot of the AdHoc Observer</summary>

![Screenshot of the AdHoc Observer in a browser at localhost:4321 showing a minimal description: the hosts Server, generated in TypeScript, and Client, generated in Java, drawn as boxes joined by their connection, named Channel; each host lists the packs PacketToClient, PacketToServer and SharedPack with arrows that mark whether the host sends the pack, receives it or both, annotated "transmittable", "receivable" and "transmittable and receivable"; a pane on the right lists the packs.](https://github.com/AdHoc-Protocol/AdHoc-protocol/assets/29354319/acc420a1-b2bf-4579-9ee6-5336ad155d4f)

</details>

## When AdHoc is not the right tool

AdHoc is built for systems whose peers you know: you deploy them, you can list them, and you care what every byte and every buffer
costs. Outside that, a plain text request/response design is often the better choice - and knowing it early saves you time.

![Where AdHoc fits, as two cards side by side. Left card, "AdHoc fits", with a check mark: peers you deploy and can list - services, devices, apps, consoles; high message rates - bytes and CPU count; large or live payloads - files, media, streams; conversations with phases - pushes from both sides; middle tiers that must not read - route or store without the schema; one protocol, several languages - C#, Java, TypeScript. Right card, "text request/response fits better", with a page icon: open public APIs - clients you will never meet; peers that evolve independently, forever - no bound on their generations; traffic read by eye, or parsed by generic intermediaries; rare, small calls - bytes do not matter; a language AdHoc does not generate yet - C++, Rust, Go: markers only. A strip below both: AdHoc carries generations as separate hosts - right for a population you know, wrong for an ecosystem without bounds.](docs/img/readme-fit.svg)

### Where AdHoc does not fit

- **Open ecosystems.** AdHoc does not evolve a pack in place: a field added at any position makes another pack, and a frozen generation
  is carried as a separate host. That works for devices you shipped and clients you control; it does not work for a public API whose
  clients you will never meet and whose generations have no bound.
- **Traffic read by eye.** A pack on the wire is its id and its fields by position - no names, no tags, no length in front of it. A
  captured packet means nothing without the description, and a generic proxy cannot inspect it.
- **Rare, small calls.** When a system makes a few small calls a minute, the gains in bytes, memory and parsing are too small to repay a
  description, a generator and generated code.

### Limits of AdHoc today

| Limit | What it means for you |
|:--|:--|
| **Three generated languages** | code is generated for C#, Java and TypeScript. The markers for C++, Rust and Go are accepted, but no generator exists yet: firmware or a service written in one of them gets no generated code today. |
| **TypeScript only dials** | a TypeScript host - a browser dashboard, say - has no server side and no UDP transport: it always opens the connection. Listening ports and UDP are C# and Java. |
| **The generator is a cloud service** | generating code needs a network connection and a (free) personal UUID. Checking a description (`ADHOC_PARSE_ONLY=1`), the impact report and redeployment run locally. |
| **Field order is the wire format** | reordering two fields of one kind, or adding a field, changes the protocol: both ends of the pack are regenerated and deployed together. |
| **`[ChaCha20]` conceals, it does not authenticate** | tampering with a ciphertext goes undetected. The encrypted transport authenticates every record, but per physical connection: an end-to-end cipher through a relay keeps the relay from reading, not from altering. Add a stage of your own when you need integrity. |
| **Guaranteed delivery has a cost** | the sender keeps a copy of every guaranteed pack until the receiver confirms it (in memory, then on disk, capped at 100 MB by default), and the guarantee lasts as long as the sender's session: a sender that crashed with its journal lost makes the receiver resynchronize. |
| **Handlers must not block** | your handlers run on the receive and transmit loops; one that blocks stalls its whole direction, and the other connections that share its threads. Hand long work to another thread. |
| **Relays never read** | a generated relay forwards bytes it cannot read; a middle host that must measure, filter or transcode content consumes the stream with your code - AdHoc generates no such host. |
| **Server-side tests vary** | how far the generator tests a result depends on its load at that moment; the result says how far. Keep the tests of your own project. |

The trade-offs of binary packs against text payloads are weighed in [Why a binary protocol, not HTTP](#why-a-binary-protocol-not-http).

→ MANUAL: [When separate generations fit](MANUAL.md#when-separate-generations-fit) ·
[Target languages of AdHoc](MANUAL.md#target-languages-of-adhoc) · [UDP connections](MANUAL.md#udp-connections) ·
[The order of fields is part of the wire format](MANUAL.md#the-order-of-fields-is-part-of-the-wire-format) ·
[Built-in stages - Zstd and ChaCha20](MANUAL.md#built-in-stages---zstd-and-chacha20) ·
[Guaranteed delivery](MANUAL.md#guaranteed-delivery) ·
[Threads: the receive loop and the transmit loop](MANUAL.md#threads-the-receive-loop-and-the-transmit-loop) ·
[Choosing what the middle tier does](MANUAL.md#choosing-what-the-middle-tier-does)

## Getting started

From an empty folder to generated code in your own project, in five moves. Each one links to the step of the manual that explains it
in full; the picture numbers the steps as the manual does.

![The first run, drawn as a U of numbered steps. Top row, left to right: step 2, AdHocAgent followed by your UUID stores it as PersonalVolatileUUID in AdHocAgent.toml beside the executable; step 4, AdHocAgent with no arguments, run in the folder of your description project, writes the template MyProtocolDescription.cs there; step 5, you compose your protocol by editing that file in your IDE - packs and their fields, hosts and their languages, connections, states and branches - and the IDE checks the C# as you type. An arrow runs down the right side to the bottom row, which reads right to left: step 6, AdHocAgent .\MyProtocolDescription.cs adds the numbers tables to the description, fills the received folder MyProtocolDescription with .description, InCS, InJAVA and InTS host folders, writes MyProtocolDescription.md and stops; a warning says that a bare file name without a directory part is taken for a UUID and nothing is generated. Step 7: after you add a route to D:/Projects/server/protocol/ and the no-entry mark for unused hosts, AdHocAgent .\MyProtocolDescription.md copies gen, lib and LICENSE-GRANT.md of the host into that folder, a folder for generated code only.](docs/img/dev-02-first-run.svg)

1. **Install.** The .NET 10 SDK, a C# IDE, and AdHocAgent from the
   [releases page](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases), in a folder of its own.
   → [Step 1](MANUAL.md#step-1-install-net-and-adhocagent)
2. **Get your personal UUID - code generation is free.** Post a message in the
   [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up); a bot creates a private project for you
   with a task that holds your UUID. Store it once:
   `AdHocAgent 100b9fd2-e593-485b-a2fe-9b9c82bc1e3f`. → [Step 2](MANUAL.md#step-2-get-your-personal-uuid)
3. **Create the description project and get the template.** A C# class library that knows AdHoc's vocabulary - add
   [`Meta.cs`](src/Meta.cs) to it or reference `AdHocAgent.dll` - so your IDE checks the description as you type. In its folder run
   `AdHocAgent` with no arguments: it writes the template `MyProtocolDescription.cs` (overwriting a file of that name), which passes
   every check as it is.
   → [Step 3](MANUAL.md#step-3-create-the-description-project), [Step 4](MANUAL.md#step-4-get-the-template)
4. **Compose your protocol.** Packs and their fields, hosts and their languages, connections with their states and branches. The IDE
   checks the C# as you type; AdHoc's own rules are checked by AdHocAgent in the next move.
   → [Step 5](MANUAL.md#step-5-compose-your-protocol)
5. **Generate and deploy.** `AdHocAgent .\MyProtocolDescription.cs` checks the description, adds its numbers tables, uploads it and
   receives the code of every host; the first run also writes the instructions file `MyProtocolDescription.md` and stops. Add a route
   to each host you use and `⛔` to the others, then run `AdHocAgent .\MyProtocolDescription.md`. From then on, every generation
   deploys by the same routes. → [Step 6](MANUAL.md#step-6-generate-the-first-code),
   [Step 7](MANUAL.md#step-7-deploy-the-first-code-into-your-project)

> [!IMPORTANT]
> - Pass the description with a directory part - `.\` on Windows, `./` elsewhere. `AdHocAgent MyProtocolDescription.cs` is taken as a
>   UUID and generates nothing.
> - Give AdHocAgent your description files only, never `Meta.cs`: the vocabulary is built into AdHocAgent.
> - Route each host to a folder that holds generated code only, such as a `protocol` folder beside your sources.

## Learn more

### MANUAL.md, part by part

[MANUAL.md](MANUAL.md) is the complete developer manual: how a description is written, what the generator produces from it, and how
the generated code behaves at runtime. It follows the same model and pipeline as this page.

![A map of MANUAL.md. Across the top, the pipeline as six boxes left to right - description, AdHocAgent, generator, code per host, deployment, runtime - each with the numbers of the chapters that explain it: description 3 and 23, AdHocAgent 24, generator 1 and 24, code per host 10, deployment 25, runtime 1 and 13. Below, the model as a stack of bands from the project down to the bytes, each with its chapters: project - 3 description file, 20 numbers tables, 21 evolving, 22 reuse; host - 10 hosts, languages; connection - 11 connections, 14 sessions and UDP, 18 virtual connections, 19 multiplexing; actor, state, branch - 12 actors, states, branches, 13 actor kinds; pack - 4 packs, 8 building packs, 9 Pack Sets, 17 trims; field - 5 fields and types, 6 constants and enums; bytes - 7 values to bytes, 15 streams, 16 transform chains. On the right, a panel "start here": 1 How AdHoc works and 2 Getting started, then the part you need; to look up: 23 Attributes - the catalog, 1 Glossary of AdHoc terms, 24 every AdHocAgent option.](docs/img/readme-manual-map.svg)

| Part | Chapters | Read it for |
|:--|:--|:--|
| **I. Orientation** | [How AdHoc works](MANUAL.md#how-adhoc-works) · [Getting started](MANUAL.md#getting-started) · [The description file](MANUAL.md#the-description-file) | the pipeline, the model, the runtime with one buffer per direction, the first run, a [glossary](MANUAL.md#glossary-of-adhoc-terms) |
| **II. Data** | [Packs](MANUAL.md#packs) · [Fields and types](MANUAL.md#fields-and-types) · [Constants and enums](MANUAL.md#constants-and-enums) · [How values become bytes](MANUAL.md#how-values-become-bytes) | what a pack can carry, and the exact bytes on the wire |
| **III. Composing data** | [Building packs from packs](MANUAL.md#building-packs-from-packs) · [Pack Sets](MANUAL.md#pack-sets) | shared fields, headers, selecting packs by name, tag and scope |
| **IV. Hosts** | [Hosts, languages, generated code](MANUAL.md#hosts-languages-generated-code) | languages, and generated objects or your own structures |
| **V. Conversations** | [Connections](MANUAL.md#connections) · [Actors, states, branches](MANUAL.md#actors-states-branches) · [Actor kinds and the actor at runtime](MANUAL.md#actor-kinds-and-the-actor-at-runtime) | state machines, RPC, timers, groups, threads |
| **VI. Delivery and transport** | [Sessions, guaranteed delivery, UDP](MANUAL.md#sessions-guaranteed-delivery-udp) · [Streams](MANUAL.md#streams) · [Transform chains](MANUAL.md#transform-chains) · [Trims](MANUAL.md#trims) · [Virtual connections and relays](MANUAL.md#virtual-connections-and-relays) · [Multiplexing](MANUAL.md#multiplexing) | delivery across lost sockets, payloads of any size, compression and encryption, stores, relays, shared ports |
| **VII. Identity, evolution, reuse** | [Numbers tables](MANUAL.md#numbers-tables) · [Evolving a protocol](MANUAL.md#evolving-a-protocol) · [Reusing descriptions](MANUAL.md#reusing-descriptions) | identities, generations, the impact report, extending projects |
| **VIII. Reference and tooling** | [Attributes](MANUAL.md#attributes) · [AdHocAgent](MANUAL.md#adhocagent) · [Deployment and smart merge](MANUAL.md#deployment-and-smart-merge) | every attribute, every command and option, deployment |

### Code, releases and forums

- [`AdhocProtocol.cs`](AdhocProtocol.cs) - AdHoc's own protocol, described in AdHoc: the connection of AdHocAgent to the generator
  and AdHocAgent's other connections. A real, complete description to read.
- [`src/Meta.cs`](src/Meta.cs) - the vocabulary of a description: every interface and attribute you may use.
- [Releases](https://github.com/AdHoc-Protocol/AdHoc-protocol/releases) - AdHocAgent downloads.
- [Sign-Up Discussion](https://github.com/orgs/AdHoc-Protocol/discussions/categories/sign-up) - your personal UUID.
- Forums for questions and ideas: [AdHocAgent and general](https://github.com/AdHoc-Protocol/AdHoc-protocol/discussions) ·
  [TypeScript generator](https://github.com/AdHoc-Protocol/InTS/discussions) · [Java generator](https://github.com/AdHoc-Protocol/InJAVA/discussions) ·
  [C# generator](https://github.com/AdHoc-Protocol/InCS/discussions)
