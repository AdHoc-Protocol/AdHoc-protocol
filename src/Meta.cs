// Copyright 2025 Chikirev Sirguy, Unirail Group
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// For inquiries, please contact: al8v5C6HU4UtqE9@gmail.com
// GitHub Repository: https://github.com/AdHoc-Protocol

using System;

namespace org.unirail{
    namespace Meta{
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Method, AllowMultiple = true)]
        public class MinMaxAttribute : Attribute{
            /// <summary>
            /// Defines a range with minimum and maximum values (inclusive) for unsigned long types.
            /// </summary>
            /// <param name="Min">The minimum value (inclusive).</param>
            /// <param name="Max">The maximum value (inclusive).</param>
            public MinMaxAttribute(ulong Min, ulong Max) { }

            /// <summary>
            /// Defines a range with minimum and maximum values (inclusive) for signed long types.
            /// </summary>
            /// <param name="Min">The minimum value (inclusive).</param>
            /// <param name="Max">The maximum value (inclusive).</param>
            public MinMaxAttribute(long Min, long Max) { }

            /// <summary>
            /// Defines a range with minimum and maximum values (inclusive) for double types.
            /// </summary>
            /// <param name="Min">The minimum value (inclusive).</param>
            /// <param name="Max">The maximum value (inclusive).</param>
            public MinMaxAttribute(double Min, double Max) { }

            /// <summary>
            /// Defines a range with minimum and maximum dates (inclusive) specified as year, month, and day.
            /// </summary>
            /// <param name="minYear">The minimum year (inclusive).</param>
            /// <param name="minMonth">The minimum month (inclusive).</param>
            /// <param name="minDay">The minimum day (inclusive).</param>
            /// <param name="maxYear">The maximum year (inclusive).</param>
            /// <param name="maxMonth">The maximum month (inclusive).</param>
            /// <param name="maxDay">The maximum day (inclusive).</param>
            /// <remarks>
            /// The range is interpreted in milliseconds since the Unix epoch. For example,
            /// specifying minYear=1970, minMonth=1, minDay=1 and maxYear=2020, maxMonth=12, maxDay=31
            /// sets the range from January 1, 1970, to December 31, 2020.
            /// </remarks>
            public MinMaxAttribute(int minYear, int minMonth, int minDay, int maxYear, int maxMonth, int maxDay) { }

            /// <summary>
            /// Defines a range with minimum and maximum dates (inclusive) specified as year, month, and day, with an explicit time unit.
            /// </summary>
            /// <param name="minYear">The minimum year (inclusive).</param>
            /// <param name="minMonth">The minimum month (inclusive).</param>
            /// <param name="minDay">The minimum day (inclusive).</param>
            /// <param name="maxYear">The maximum year (inclusive).</param>
            /// <param name="maxMonth">The maximum month (inclusive).</param>
            /// <param name="maxDay">The maximum day (inclusive).</param>
            /// <param name="time_unit">The time unit applied to the range (e.g., milliseconds, seconds, etc.).</param>
            /// <remarks>
            /// The generated API will operate with time values represented in the specified <paramref name="time_unit"/>
            /// since the Unix epoch. For instance, using TimeSpan.FromSeconds(1) sets the range in seconds.
            /// </remarks>
            public MinMaxAttribute(int minYear, int minMonth, int minDay, int maxYear, int maxMonth, int maxDay, TimeSpan time_unit) { }
        }


        /// <summary>
        /// Specifies that a numeric field's values are expected to be predominantly concentrated near a specific minimum value,
        /// with deviations becoming less likely as values approach the maximum.
        /// </summary>
        /// <remarks>
        /// - The point of highest concentration is defined by the <c>minMostProbableValue</c> parameter, defaulting to 0 if not specified.
        /// - The maximum possible value is given by the <c>max</c> parameter. If omitted, it’s calculated based on the field's data type
        ///   (e.g., for a short field, it’s <c>short.MaxValue + minMostProbableValue</c>), optimizing varint compression for values near the minimum.
        ///
        /// **Conceptual Maximums by Type:**
        /// | Field Type    | Conceptual Maximum            |
        /// |---------------|-------------------------------|
        /// | short         | short.MaxValue + min          |
        /// | ushort / char | ushort.MaxValue + min         |
        /// | int           | int.MaxValue + min            |
        /// | uint          | uint.MaxValue + min           |
        /// | long          | long.MaxValue + min           |
        /// | ulong         | long.MaxValue + min (conceptual) |
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
        public class AAttribute : Attribute{
            /// <param name="minMostProbableValue">The minimum value that is most probable (defaults to 0 if not specified).</param>
            /// <param name="max">
            /// The conceptual upper bound for the distribution. If 0 (default), it’s calculated based on the field type
            /// and <c>minMostProbableValue</c>. If non-zero, this value is used directly.
            /// </param>
            public AAttribute(long minMostProbableValue = 0, long max = 0) { }
        }


        /// <summary>
        /// Specifies that a numeric field's values are expected to be predominantly concentrated near a specific maximum value,
        /// with deviations becoming less likely as values approach the minimum.
        /// </summary>
        /// <remarks>
        /// - The point of highest concentration is defined by the <c>maxMostProbableValue</c> parameter, defaulting to 0 if not specified.
        /// - The minimum possible value is given by the <c>min</c> parameter. If omitted, it’s calculated based on the field's data type.
        ///
        /// **Conceptual Minimums by Type:**
        /// | Field Type    | Conceptual Minimum           |
        /// |---------------|----------------------------|
        /// | short         | short.MinValue + max       |
        /// | ushort / char | -ushort.MaxValue + max     |
        /// | int           | int.MinValue + max         |
        /// | uint          | -uint.MaxValue + max       |
        /// | long / ulong  | long.MinValue + max        |
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
        public class VAttribute : Attribute{
            /// <param name="maxMostProbableValue">The value around which the distribution is most concentrated (defaults to 0 if not specified).</param>
            /// <param name="min">
            /// The conceptual lower bound. If 0 (default), it’s calculated based on the field type and <c>maxMostProbableValue</c>.
            /// If non-zero, this value is used directly.
            /// </param>
            public VAttribute(long maxMostProbableValue = 0, long min = 0) { }
        }


        /// <summary>
        /// Specifies that a numeric field's values are centered around a specific zero point, with deviations less likely further from the center.
        /// </summary>
        /// <remarks>
        /// - The central point is given by the <c>zero</c> property, defaulting to 0 if not specified.
        /// - The <c>amplitude</c> property defines the spread. If omitted, the range is based on the field’s type.
        ///
        /// **Default Ranges by Type (Amplitude=0):**
        /// | Field Type    | Minimum               | Maximum               |
        /// |---------------|-----------------------|-----------------------|
        /// | short         | Zero - short.MaxValue | Zero + short.MaxValue |
        /// | ushort / char | Zero - ushort.MaxValue| Zero + ushort.MaxValue|
        /// | int           | Zero - int.MaxValue   | Zero + int.MaxValue   |
        /// | uint          | Zero - uint.MaxValue  | Zero + uint.MaxValue  |
        /// | long / ulong  | Zero - long.MaxValue  | Zero + long.MaxValue  |
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
        public class XAttribute : Attribute{
            /// <param name="amplitude">
            /// The maximum deviation from <c>zero</c>. If 0 (default), the range is based on the field type’s maximum value relative to <c>zero</c>.
            /// If non-zero, the range is [<c>zero - amplitude</c>, <c>zero + amplitude</c>]. Note that <c>amplitude</c> is unsigned (ulong).
            /// </param>
            /// <param name="zero">The central value around which data is distributed (defaults to 0 if not specified).</param>
            public XAttribute(ulong amplitude = 0, long zero = 0) { }
        }

        /// <summary>
        /// Marks a field as multidimensional with specified dimensions, affecting how data is serialized as multi-dimensional arrays.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
        public class DAttribute : Attribute{
            /// <param name="dims">The dimensions of the multidimensional array.</param>
            public DAttribute(params int[] dims) { }
        }

        /// <summary>
        /// Sets the maximum stream length in bytes. May be applied to a stream-based field, or
        /// to a class declaration that inherits <see cref="Stream"/> or <see cref="File"/>.
        /// </summary>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = true)]
        public class SAttribute : Attribute{
            /// <param name="maxLength">The maximum stream length in bytes.</param>
            public SAttribute(long maxLength) { }
        }


        public interface ___IfSendingFrom{ }

        /// <summary>
        /// A meta-type representing a specific communication Endpoint: a <c>Host</c> sending data over a particular <c>Connection</c>.
        /// </summary>
        /// <typeparam name="Connection">The communication connection link. Must extend a base connection interface like `org.unirail.Meta._`.</typeparam>
        /// <typeparam name="fromHost">The sending host on the specified connection.</typeparam>
        public interface IfSendingFrom<fromHost, viaConnection> : ___IfSendingFrom where viaConnection : ___IConnects where fromHost : Meta.Host{ }

        /// <summary>
        /// Marks the direct, high-performance passthrough of an arbitrary binary stream.
        /// The content is not assumed to be a structured <c>Pack</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Used when the source of the data is already a stream (e.g., a file or another network socket) and the receiver
        /// should also consume it as a raw stream via the <c>Connection.Receiver.BytesDst</c> interface.
        /// The on-the-wire format is a self-terminating chunked stream (<c>[length][data]...[0]</c>), suited for feeds
        /// of unknown or continuous length. The framing is interruptible — the sender can emit the terminator at any time
        /// to yield bandwidth to higher-priority traffic — and lets opaque relays (e.g. the <c>Forwarder</c>) shuttle
        /// bytes between connections without parsing the contents.
        /// Ideal for proxying data or streaming large files with minimal latency and memory overhead.
        /// </para>
        /// <para>
        /// Two usage forms:
        /// <list type="bullet">
        /// <item><description><b>As a field marker:</b> <c>[S(N)] Stream raw_bytes;</c> — a one-off stream field.</description></item>
        /// <item><description><b>As a named pack:</b> <c>[S(N)] class MyStream : Stream {}</c> — a reusable, named Stream
        /// type. The pack body must have no instance fields (constants and static fields are fine); the pack carries only its framing
        /// metadata and gets a transmittable id like any other pack. The generator records the kind on the wire as
        /// <c>stream_max &lt; 0</c> (negative N).</description></item>
        /// </list>
        /// </para>
        /// <para>Both forms require an explicit maximum size via the <see cref="SAttribute"/> attribute.</para>
        /// </remarks>
        public interface Stream{ }

        /// <summary>
        /// Marks the efficient transfer of a binary payload with a known total size.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unlike <see cref="Stream"/>, which uses a self-terminating chunked format
        /// (<c>[length][data]...[0]</c>) suited for feeds of unknown or continuous length,
        /// <c>File</c> uses a single length-prefix (<c>[total_length][data]</c>). This makes
        /// it the most efficient wire format for disk-based BLOBs or in-memory buffers whose
        /// size is known upfront. The trade-off is that <c>File</c> is <b>not</b> interruptible:
        /// once transmission begins, it cannot be terminated early to yield bandwidth to
        /// higher-priority traffic, and an opaque relay cannot forward it without knowing the total.
        /// </para>
        /// <para>
        /// Two usage forms:
        /// <list type="bullet">
        /// <item><description><b>As a field marker:</b> <c>[S(N)] File blob;</c> — a one-off file field.</description></item>
        /// <item><description><b>As a named pack:</b> <c>[S(N)] class MyFile : File {}</c> — a reusable, named File type.
        /// The pack body must have no instance fields (constants and static fields are fine); the pack carries only its framing metadata
        /// and gets a transmittable id like any other pack. The generator records the kind on the wire as
        /// <c>stream_max &gt; 0</c> (positive N).</description></item>
        /// </list>
        /// </para>
        /// <para>Both forms require an explicit maximum size via the <see cref="SAttribute"/> attribute.</para>
        /// </remarks>
        public interface File{ }

        /// <summary>
        /// Base type for a single <b>stage</b> (one link) in a stream-transform chain: a reusable byte transform —
        /// compression, encryption, … — inserted between the chunked-stream framing and a field's own serialized bytes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The chain.</b> A field <b>keeps its own type</b>; its serialized bytes are the chain's <i>leaf</i>. A
        /// chunked-stream root (<c>[length][data]…[0]</c>) frames the chain — <b>implicit</b> for an ordinary field, or
        /// the explicit <see cref="Stream"/>/<see cref="File"/> field type. The list reads in <b>dataflow order</b>, the
        /// way the bytes actually travel: <b>left = app/leaf end</b> → <b>right = wire end</b>. On <b>transmit</b> the
        /// left-most stage runs first (it wraps the leaf) and the right-most hands its output to the wire; on
        /// <b>receive</b> the same list is walked backwards. So <c>[Zstd, ChaCha20] MyPack p;</c> is
        /// <c>pack → Zstd → ChaCha20 → wire</c> — compress <i>then</i> encrypt, the correct order (ciphertext does not
        /// compress). Place a cipher to the <b>right</b> of a compressor.
        /// </para>
        /// <para>
        /// <b>Applying.</b> Inline as an ordered list on a field — <c>[Zstd(6), ChaCha20] MyPack p;</c> — or, for reuse,
        /// grouped into a named <see cref="StreamFlowAttribute"/> applied by one attribute. Either way the field's type is
        /// unchanged. On a <b>structured</b> field (pack, array, primitive) the type bounds the chain — no size cap. On a
        /// <see cref="Stream"/>/<see cref="File"/> field the stages layer on the raw conduit, which carries its own
        /// <see cref="SAttribute"/> (<c>[S(N)]</c>) cap by virtue of the datatype, independent of the stages.
        /// </para>
        /// <para>
        /// <b>Code generation.</b> Each stage type generates <b>one</b> file named after the attribute
        /// (e.g. <c>ChaCha20.cs</c> / <c>ChaCha20.java</c> / <c>ChaCha20.ts</c>): an identity pass-through <c>IStream</c>
        /// with custom-code injection points. The user fills the injection points (hand-rolled or a library call), and
        /// regeneration preserves that code. One implementation is generated per stage type and shared by every chain
        /// that uses it.
        /// </para>
        /// <para>
        /// <b>Parameters.</b> The constructor signature <i>declares the stage's parameter contract</i> — it is read by
        /// the generator, never executed. Declare parameters in <b>AdHoc types</b> (use <see cref="Binary"/>, not
        /// <c>byte</c> — <c>byte</c> is signed in Java, unsigned in C#, and absent in TypeScript). The parameter's type
        /// decides where its value lives:
        /// <list type="bullet">
        ///   <item><description><b>Constant-expressible</b> (e.g. <c>int level</c>) → may be supplied in the protocol
        ///   description (<c>[Zstd(6)]</c>) → shared, compile-time configuration.</description></item>
        ///   <item><description><b>Non-constant</b> (e.g. <c>Binary[,] key</c>) → cannot be an attribute literal → a
        ///   <b>runtime-injected</b> value (e.g. a secret key); the generator scaffolds the hook.</description></item>
        /// </list>
        /// Generated code carries every declared parameter to <b>both</b> the sender and the receiver; the stage
        /// implementation decides whether and how to use each one.
        /// </para>
        /// </remarks>
        public class StreamStageAttribute : Attribute{ }

        /// <summary>
        /// Role base for a <b>compression</b> stage (e.g. <see cref="ZstdAttribute"/>). Grouping every compressor under
        /// one type lets the generator recognize them (<c>is StreamCompressionStageAttribute</c>), allow <b>at most
        /// one</b> compressor per chain, and reject a compressor placed over a <see cref="File"/> root.
        /// </summary>
        /// <remarks>
        /// A compression stage <b>changes the byte length</b>, so it cannot sit over a <see cref="File"/> root —
        /// <c>File</c> commits a known total length up front (<c>[total_length][data]</c>), which compression would
        /// invalidate. Compression is <b>symmetric</b>: a compressing sender is always matched by a decompressing
        /// receiver on every path the data travels.
        /// </remarks>
        public class StreamCompressionStageAttribute : StreamStageAttribute{ }

        /// <summary>
        /// Role base for an <b>encryption</b> stage (e.g. <see cref="ChaCha20Attribute"/>). Grouping every cipher under
        /// one type lets the generator recognize them and allow <b>at most one</b> cipher per chain.
        /// </summary>
        /// <remarks>
        /// Ciphers are <b>symmetric</b>: the receiver applies the inverse (decrypt). Keys and nonces are <b>secrets</b> —
        /// declare them as a runtime-injected parameter contract (a <see cref="Binary"/> shape), never as literal values
        /// in the protocol description. For <i>compress-then-encrypt</i>, place the cipher toward the <b>wire end</b>
        /// (to the right of the compressor) so transmitted data is compressed first, then encrypted — this is the only
        /// accepted arrangement of the pair: a cipher written to the left of a compressor is <b>rejected</b>, since
        /// encrypting first leaves the compressor nothing to shrink.
        /// </remarks>
        public class StreamCipherStageAttribute : StreamStageAttribute{ }

        /// <summary>
        /// A named, reusable <b>stream-transform chain</b>: an ordered list of <see cref="StreamStageAttribute"/> stages
        /// declared once and applied to a field by a single attribute, while the field keeps its own type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The chunked-stream root is <b>implicit</b> — list the transform stages in dataflow order
        /// <b>left = app/leaf end</b> → <b>right = wire end</b> (see <see cref="StreamStageAttribute"/> for direction).
        /// A flow never lists other flows.
        /// </para>
        /// <para>
        /// A <see cref="StreamTrimAttribute"/> may sit in the list too — it expands in place, keeping its position —
        /// but that pins the flow to the endpoints it names: every application is cut there. Fine when the flow IS one
        /// store-and-replay pipeline, wrong for a general-purpose chain; prefer writing trims at the point of
        /// application.
        /// </para>
        /// <para>Declare once, apply by name — the field keeps its type:</para>
        /// <code>
        /// [Zstd(6), ChaCha20]
        /// public class FastCompressAndCipher : StreamFlowAttribute { }
        ///
        /// [FastCompressAndCipher] MyPack payload;   // payload is still a MyPack
        /// </code>
        /// </remarks>
        public class StreamFlowAttribute : Attribute{ }

        /// <summary>
        /// Base type for a <b>trim</b> — a <i>cut point</i> at which the pipeline stops for one named
        /// <see cref="IfSendingFrom{fromHost, viaConnection}"/> endpoint, so that the host there holds <b>raw bytes at
        /// that depth</b> instead of a typed object. This is how a payload is handed to a host that must store, forward
        /// or replay it without ever knowing its type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Not a transform.</b> A trim occupies a <i>position</i> in the ordered attribute list next to the
        /// <see cref="StreamStageAttribute"/> stages, but transforms nothing: it has no role, no parameters, and no
        /// implementation to generate. It does not count towards the one-compressor / one-cipher limit, and it does not
        /// take part in the compressor-before-cipher ordering rule — a trim may sit anywhere between two stages. It
        /// needs no stages at all: a marker on its own is a complete declaration (see <b>Position 0</b> below).
        /// </para>
        /// <para>
        /// <b>What it does.</b> A chain is ordinarily symmetric: whatever the sender wraps, the receiver unwraps in
        /// full, and both ends hold the same object. A trim breaks that symmetry <i>on one leg</i>. Everything to the
        /// <b>right</b> of the marker (towards the wire) still runs; everything to its <b>left</b> — the remaining
        /// stages <i>and the pack's own serialization</i> — is skipped, and the payload crosses that boundary as an
        /// opaque byte stream. Given
        /// <code>
        /// [Base64, Zstd, ToStream&lt;FromSource&gt;, ChaCha20] class Telemetry { … }
        /// </code>
        /// a host receiving from <c>FromSource</c> sees
        /// <c>wire → ChaCha20⁻¹ → bytes</c> — still Base64'd and Zstd-compressed, storable as-is, never parsed —
        /// while every other leg runs the whole chain and delivers a <c>Telemetry</c>.
        /// </para>
        /// <para>
        /// <b>The wire is unchanged.</b> A trim is a per-endpoint code-generation decision, never a wire-format one:
        /// the transmitted bytes are identical whether or not a chain carries one. What changes is which side
        /// materializes an object — and therefore which side needs the payload's <b>schema</b> at all. That is the
        /// point of it: an archive, relay, or foreign consumer sitting at a trim can store and forward the payload
        /// while remaining free of the type it carries, so the type may gain fields without that host being rebuilt.
        /// </para>
        /// <para>
        /// <b>Position 0 — the leaf end.</b> A marker written before any stage — <c>[ToStream&lt;E&gt;, Zstd] MyPack p;</c>
        /// — cuts at the payload itself: on that leg the receiver runs the whole inverse chain and stops right before
        /// materializing the object, holding the plain serialized pack. A bare <c>[ToStream&lt;E&gt;] MyPack p;</c>, with
        /// no stage at all, is the degenerate case of the same thing: the payload simply crosses as opaque bytes.
        /// Every other position moves the cut further towards the wire; the chain itself is unaffected by where the
        /// marker sits and runs in full on every leg the marker does not name.
        /// </para>
        /// <para>
        /// <b>Where it is used — the depth is a dial.</b> The natural user of a trim is a host whose job is to
        /// <i>keep</i> the payload rather than read it: a log broker, an object store, an edge cache, a WORM audit
        /// archive, a cross-datacenter mirror. Without one such a host must decrypt, decompress and then re-encode
        /// every message — paying for work whose result it discards, and holding keys it has no business holding. The
        /// position of the marker decides <b>how much of the pipeline that host must implement</b>. For a pack chained
        /// <c>[Zstd, ChaCha20]</c>:
        /// <list type="table">
        ///   <listheader><description>declaration → what the store keeps / runs / must hold</description></listheader>
        ///   <item><description><c>[Stream&lt;To,From&gt;, Zstd, ChaCha20]</c> → the plain serialized pack; the whole
        ///   inverse chain; the key and every stage implementation.</description></item>
        ///   <item><description><c>[Zstd, Stream&lt;To,From&gt;, ChaCha20]</c> → the <b>compressed</b> blob;
        ///   <c>ChaCha20⁻¹</c> only; just the key. This is what a log broker actually does: the producer compresses
        ///   once, the store serves that blob unchanged, and fan-out to N consumers costs one compression, not
        ///   N.</description></item>
        ///   <item><description><c>[Zstd, ChaCha20, Stream&lt;To,From&gt;]</c> → exactly the <b>wire bytes</b>;
        ///   <b>nothing</b>; nothing at all. Blind custody: the operator stores ciphertext it holds no key for, while
        ///   producer and consumer share it. Neither a per-hop chain nor a tunnel can express this — a hop chain
        ///   terminates <i>at</i> the store in plaintext, a tunnel passes <i>through</i> it leaving nothing
        ///   behind.</description></item>
        /// </list>
        /// No row needs the payload's schema; the rows differ only in how much of the chain the store must implement.
        /// </para>
        /// <para>
        /// <b>Same depth means no transcoding.</b> When the two cuts of a store-and-replay pair sit at one position —
        /// which is what <see cref="StreamAttribute{ToStream_IfSendingFrom, FromStream_IfSendingFrom}"/> expresses —
        /// the bytes arriving are byte-identical to the bytes replayed: <c>store(bytes)</c> / <c>replay(bytes)</c>,
        /// nothing decoded, nothing re-encoded on the hot path. Giving the two directions <i>different</i> depths would
        /// force the store to convert between the forms, re-adding to it exactly what the deeper cut removed.
        /// </para>
        /// <para>
        /// <b>Compatibility of trimmed bytes.</b> Bytes taken (or supplied) at a cut are only meaningful against the
        /// exact configuration standing to its left — the stages, their order, and their design-time parameters.
        /// Changing a level, adding a stage, or reordering silently invalidates anything stored earlier, because
        /// nothing on the wire announces the difference. Treat the left-of-cut configuration as a persisted format.
        /// </para>
        /// <para>
        /// <b>Naming the legs.</b> Every trim's endpoint parameter is intentionally <b>UNCONSTRAINED</b>: besides a
        /// single Endpoint (<see cref="IfSendingFrom{fromHost, viaConnection}"/>) or a named Endpoint Set it also
        /// accepts a <b>TUPLE</b> — <c>[ToStream&lt;(FromA, FromB)&gt;]</c> — which names several sending legs at once.
        /// A tuple is a ValueTuple struct, so no <see cref="___IfSendingFrom"/> / <c>class</c> constraint could admit
        /// it. Whatever is written there is resolved, and diagnosed, by the parser (<c>ProjectImpl.resolve_endpoints</c>).
        /// </para>
        /// <para>
        /// Applicable to a <b>field</b> or a <b>pack</b>. Deliberately not to a connection: a chain on a
        /// <see cref="Connects{L, R}"/> or <see cref="VirtuallyConnects{L, R, PATH}"/> wraps everything that link
        /// carries, where "the endpoint holds bytes instead of an object" is simply what a relay already is.
        /// A <see cref="StreamFlowAttribute"/> may include a trim — it expands in place like any other entry — but that
        /// pins the flow to one endpoint set; prefer writing trims at the point of application.
        /// </para>
        /// </remarks>
        public class StreamTrimAttribute : Attribute{ }

        /// <summary>
        /// <b>Trim on the receiving side.</b> When the sender is an endpoint in <typeparamref name="IfSendingFrom"/>,
        /// the receiver stops unwrapping at this point in the chain and takes <b>raw bytes</b>; the sender still holds
        /// (and serializes) the typed value.
        /// </summary>
        /// <typeparam name="IfSendingFrom">
        /// An Endpoint or Endpoint Set — see <see cref="IfSendingFrom{fromHost, viaConnection}"/>. An Endpoint names
        /// <i>one leg</i> (a sender over a connection), never the whole channel: the opposite leg of the same channel
        /// is not covered and keeps the ordinary, fully-unwrapped form.
        /// </typeparam>
        /// <remarks>
        /// <code>
        /// [Base64, Zstd, ToStream&lt;FromSource&gt;, ChaCha20] class Telemetry { … }
        /// //  sender  : Telemetry → Base64 → Zstd → ChaCha20 → wire
        /// //  receiver:                              wire → ChaCha20⁻¹ → bytes   (Base64'd + compressed, stored as-is)
        /// //  any other leg: the full chain, a typed Telemetry on both ends
        /// </code>
        /// The receiving host feeds the generated opaque sink and never materializes the pack, so it needs none of its
        /// schema. Because the payload is one complete serialized value, the transfer is <b>not interruptible</b> — a
        /// truncated one would leave a torn object for whoever decodes it later.
        /// <para>With no stage to cut, the marker stands alone and the plain serialized payload crosses as bytes:</para>
        /// <code>
        /// [ToStream&lt;FromSource&gt;] public Telemetry telemetry;   // sender: typed → wire; receiver at FromSource: raw bytes
        /// [ToStream&lt;FromSource&gt;] public string    log;         // a `string` payload rides as UTF-8, `[totalLen][UTF-8]`
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
        public class ToStreamAttribute<IfSendingFrom> : StreamTrimAttribute{ // unconstrained: the argument may also be a TUPLE of endpoints
        }

        /// <summary>
        /// Guaranteed delivery: the packs of <typeparamref name="PACKS"/> that <typeparamref name="HOST"/> sends over the
        /// Connection this interface is declared in survive the loss of the socket. The sender journals the packs it has
        /// sent, up to <see cref="resumable_megabytes"/>, and after the peer reconnects and proves its identity replays from
        /// the position the peer names. Packs outside <typeparamref name="PACKS"/> travel as before.
        /// How long a session outlives its socket is set by <see cref="ResumableAttribute"/> on the Connection, which a
        /// Connection with a Resumable declaration has to carry.
        /// </summary>
        /// <remarks>
        /// Legal only inside a Connection body, and <typeparamref name="HOST"/> has to be one of the two hosts that Connection
        /// connects; anything else stops the agent. A Connection may carry one declaration per direction.
        /// <para>
        /// The declaration becomes a header named <c>&lt;Name&gt;_</c> with one field, <c>int pos</c> — the pack's position in the
        /// journalled stream — applied to every pack of the set that <typeparamref name="HOST"/> really transmits there. The
        /// receiver reports the position it has consumed; the sender continues from it, or names the position it can continue
        /// from when the journal no longer holds the one asked for. All of that is generated; the application only sees the
        /// packs arrive once, in order.
        /// </para>
        /// <code>
        /// [Resumable(30)]
        /// interface ServerToMonitoring : Connects&lt;Server, Monitoring&gt;{
        ///     …
        ///     interface Guaranteed : Resumable&lt;Server, (ToMonitoring&lt;@Monitoring&gt;, X&lt;@Monitoring.VolatileInfo&gt;)&gt;{
        ///         int resumable_megabytes => 100;
        ///     }
        /// }
        /// </code>
        /// </remarks>
        /// <typeparam name="HOST">The sending host — the Left or the Right host of the enclosing Connection.</typeparam>
        /// <typeparam name="PACKS">A Pack Set, or a tuple of packs, Pack Sets, scopes and exclusions, as in <see cref="HeaderFor{PackSet}"/>.</typeparam>
        public interface Resumable<HOST, PACKS> where HOST : Host{
            /// <summary>How much, in megabytes, the sender keeps journalled for a replay; at the cap the guaranteed packs wait until the receiver acknowledges. At least 1.</summary>
            public int resumable_megabytes => 100;
        }

        /// <summary>
        /// <b>Trim on the sending side.</b> When the sender is an endpoint in <typeparamref name="IfSendingFrom"/>, it
        /// injects <b>raw bytes</b> at this point in the chain — replayed from storage, forwarded from elsewhere — and
        /// only the stages to the right of the marker run before the wire; the receiver rehydrates the typed value as
        /// usual.
        /// </summary>
        /// <typeparam name="IfSendingFrom">
        /// An Endpoint or Endpoint Set — see <see cref="IfSendingFrom{fromHost, viaConnection}"/>. It names the leg on
        /// which the sender supplies bytes rather than an object.
        /// </typeparam>
        /// <remarks>
        /// <code>
        /// [Base64, Zstd, FromStream&lt;FromArchive&gt;, ChaCha20] class Telemetry { … }
        /// //  sender  : bytes → ChaCha20 → wire                     (the archive re-sends what it kept)
        /// //  receiver:           wire → ChaCha20⁻¹ → Zstd⁻¹ → Base64⁻¹ → Telemetry
        /// </code>
        /// The bytes handed in must already be exactly what the skipped left-of-cut stages would have produced — the
        /// generator cannot check this, and a mismatch surfaces as a decode failure at the far end. See the
        /// compatibility note on <see cref="StreamTrimAttribute"/>.
        /// <para>With no stage to cut, the marker stands alone and the sender supplies the plain serialized payload:</para>
        /// <code>
        /// [FromStream&lt;FromArchive&gt;] public Telemetry telemetry;   // sender at FromArchive: raw bytes; receiver: typed
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
        public class FromStreamAttribute<IfSendingFrom> : StreamTrimAttribute{ // unconstrained: the argument may also be a TUPLE of endpoints
        }

        /// <summary>
        /// <b>Both trims at one depth</b> — <see cref="ToStreamAttribute{IfSendingFrom}"/> for
        /// <typeparamref name="ToStream_IfSendingFrom"/> and <see cref="FromStreamAttribute{IfSendingFrom}"/> for
        /// <typeparamref name="FromStream_IfSendingFrom"/>, written as one marker.
        /// </summary>
        /// <typeparam name="ToStream_IfSendingFrom">The Endpoint (or Set) whose <b>receiver</b> takes raw bytes here.</typeparam>
        /// <typeparam name="FromStream_IfSendingFrom">The Endpoint (or Set) whose <b>sender</b> supplies raw bytes here.</typeparam>
        /// <remarks>
        /// This is the <b>store-and-replay</b> pair, and the reason the combined form exists: because both cuts sit at
        /// the same position, what the store receives on the inbound leg is byte-compatible with what it hands back on
        /// the outbound one — it keeps the payload in the transmitted, still-compressed form and never learns the type.
        /// <code>
        /// [Base64, Zstd, Stream&lt;FromSource, FromArchive&gt;, ChaCha20] class Telemetry { … }
        /// //  Source  → Archive : Telemetry → Base64 → Zstd → ChaCha20 → wire → ChaCha20⁻¹ → bytes   (kept)
        /// //  Archive → Viewer  : bytes → ChaCha20 → wire → ChaCha20⁻¹ → Zstd⁻¹ → Base64⁻¹ → Telemetry
        /// //  every other leg   : the full chain, typed on both ends
        /// </code>
        /// <para>With no stage to cut, the marker stands alone — the store keeps the plain serialized payload:</para>
        /// <code>
        /// [Stream&lt;FromSource, FromArchive&gt;] public Telemetry telemetry;
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class, AllowMultiple = false)]
        public class StreamAttribute<ToStream_IfSendingFrom, FromStream_IfSendingFrom> : StreamTrimAttribute{ // unconstrained: each argument may also be a TUPLE of endpoints
        }

        /// <summary>
        /// <b>Zstandard</b> compression stage — transparently compresses a field's (or pack's) serialized bytes on the
        /// way out and restores them on arrival, invisibly to the code on both ends.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use inline — <c>[Zstd] Snapshot snap;</c> (level 3, zstd's own default) or <c>[Zstd(9)] Telemetry t;</c> (stronger, slower) —
        /// or as one stage of a <see cref="StreamFlowAttribute"/>. Applies to a pack, a pack-typed field, or a
        /// <see cref="Stream"/> field/named pack. It <b>cannot</b> sit over a <see cref="File"/> root (compression breaks
        /// File's known-length contract); to compress small or primitive values, group them in a pack and compress the
        /// pack.
        /// </para>
        /// <para>
        /// <para>
        /// Put it where the bytes are <b>known to be redundant</b> - text, metadata, repeated identifiers - and not on a pack
        /// whose payload is opaque application data (a blob, a key, a UUID, something already compressed): the chain is paid for
        /// on every message that crosses it, while the saving is the data's to give. For a protocol of opaque payloads, a chain on
        /// the <b>Connection</b> costs one frame per connection instead of one per message, and the deployment - which knows the
        /// data - decides whether to add it.
        /// </para>
        /// <para>
        /// <b>Symmetric</b> and part of the agreed wire format. The <c>level</c> is <b>encoder-only</b> — zstd streams
        /// are self-describing, so the decoder ignores it; both generated sides receive it, but only the sender's
        /// implementation need apply it.
        /// </para>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
        public class ZstdAttribute : StreamCompressionStageAttribute{
            /// <param name="level">
            /// Compression level, from <b>1</b> (fastest, weakest) to <b>22</b> (strongest, slowest). Omit for <b>3</b>, zstd's
            /// own default and the level for a wire: it compresses at hundreds of MB/s and already takes a page of rows to a
            /// fifth of its bytes. Levels 1–6 are wire levels; 7–12 buy a few percent of ratio for several times the CPU;
            /// 13 and above are archival settings for files at rest — level 20 compresses at about 10 MB/s, and a 256 KiB
            /// reply behind it costs the sending thread 25 ms (measured in the CQL-over-AdHoc benchmark). Higher = stronger
            /// and slower; lower = faster with a weaker ratio.
            /// </param>
            public ZstdAttribute(int level = 3) { }
        }

        /// <summary>
        /// <b>ChaCha20</b> stream-cipher stage — encrypts a field's serialized bytes on the way out and decrypts them on
        /// arrival. A stream cipher (keystream XOR) is the natural fit for a chunked stage: byte-incremental, resumable,
        /// no padding or block alignment, and the same operation in both directions.
        /// </summary>
        /// <remarks>
        /// The key and nonce are <b>secrets, injected at runtime</b> — never stored in the protocol description. The
        /// parameterless form leaves the whole key/nonce setup to the generated stub's injection points; the
        /// <c>(key, iv)</c> form additionally <i>declares their shape</i> (a <see cref="Binary"/> contract, lowered to
        /// the right per-language byte type by the generator) so the generated signatures expect them. See
        /// <see cref="StreamStageAttribute"/> for how stage parameters are declared and generated.
        /// </remarks>
        public class ChaCha20Attribute : StreamCipherStageAttribute{
            public ChaCha20Attribute() { }

            /// <param name="key">Secret key — runtime-injected. Declared as a <see cref="Binary"/> shape, not a value.</param>
            /// <param name="iv">Nonce / initialization vector — runtime-injected. Declared as a <see cref="Binary"/> shape.</param>
            public ChaCha20Attribute(Binary[,] key, Binary[,] iv) { }
        }


        /// <summary>
        /// Interface for transmitting binary data. In Java, maps to signed bytes (-128 to 127); in C#, maps to unsigned bytes (0 to 255).
        /// This difference is handled internally for cross-language compatibility.
        /// </summary>
        public interface Binary{ }

        /// <summary>
        /// Represents a signed long type for JavaScript, using the `number` primitive within the safe integer range
        /// (-2^53 + 1 to 2^53 - 1). Exceeding this range uses `BigInt`, which is less efficient.
        /// </summary>
        /// <remarks>
        /// See: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number/SAFE_INTEGER
        /// and https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt for performance implications.
        /// </remarks>
        public class longJS{
            [MinMax(-0x1FFFFFFFFFFFFF, 0x1FFFFFFFFFFFFF)]
            long TYPEDEF;
        }

        /// <summary>
        /// Represents an unsigned long type for JavaScript, using the `number` primitive within the safe integer range
        /// (0 to 2^53 - 1). Exceeding this range uses `BigInt`, which is less efficient.
        /// </summary>
        /// <remarks>
        /// See: https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Number/SAFE_INTEGER
        /// and https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/BigInt for performance implications.
        /// </remarks>
        public class ulongJS{
            [MinMax(0, 0x1FFFFFFFFFFFFF)]
            long TYPEDEF;
        }

        /// <summary>
        /// Defines a set of unique elements of type K. Default maximum length is 255.
        /// </summary>
        /// <typeparam name="K">The type of elements in the set.</typeparam>
        public interface Set<K>{ }

        /// <summary>
        /// Defines a map with key type K and value type V. Default maximum length is 255.
        /// </summary>
        /// <typeparam name="K">The type of keys in the map.</typeparam>
        /// <typeparam name="V">The type of values in the map.</typeparam>
        public interface Map<K, V>{ }

        // Marker interfaces for target-specific code generation.
        // These allow definitions to be included or excluded for a particular target platform.
#region Language Markers
        /// <summary>
        /// Marker interface to indicate a definition is for C++ code generation.
        /// </summary>
        public interface InCPP{ }

        /// <summary>
        /// Marker interface to indicate a definition is for Rust code generation.
        /// </summary>
        public interface InRS{ }

        /// <summary>
        /// Marker interface to indicate a definition is for C# code generation.
        /// </summary>
        public interface InCS{ }

        /// <summary>
        /// Marker interface to indicate a definition is for Java code generation.
        /// </summary>
        public interface InJAVA{ }

        /// <summary>
        /// Marker interface to indicate a definition is for Go code generation.
        /// </summary>
        public interface InGO{ }

        /// <summary>
        /// Marker interface to indicate a definition is for TypeScript code generation.
        /// </summary>
        public interface InTS{ }

        /// <summary>
        /// A marker to indicate that a definition applies to all target languages.
        /// </summary>
        public interface All{ }
#endregion


        /// <summary>
        /// Specifies that fields in this definition should be added to all transmittable data packs within the <typeparamref name="PackSet"/>.
        /// </summary>
        /// <typeparam name="PackSet">A type that defines the PackSet of packs to be modified.</typeparam>
        public interface FieldsInjectInto<PackSet>{ }


        /// <summary>
        /// Declares this data pack as a header for all transmittable data packs within the <typeparamref name="PackSet"/>.
        /// </summary>
        /// <typeparam name="PackSet">A type that defines the PackSet of packs this header applies to.</typeparam>
        public interface HeaderFor<PackSet>{ }

        /// <summary>
        /// Acts as a <b>Whitelist</b> filter for a Named PackSet.
        /// <para>
        /// <b>Retains ONLY</b> the packets within the set whose <b>fully qualified type name</b>
        /// (Namespace.ClassName) matches the provided Regular Expression.
        /// </para>
        /// <para>All other packets are removed from the set.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
        public class KeepNameAttribute : Attribute{
            public KeepNameAttribute(string regexp) { }
        }

        /// <summary>
        /// Acts as a <b>Whitelist</b> filter for a Named PackSet.
        /// <para>
        /// <b>Retains ONLY</b> the packets within the set whose <b>documentation comments</b>
        /// match the provided Regular Expression.
        /// </para>
        /// <para>Useful for filtering by visual tags (e.g., emojis 🔒, ⚡) or specific keywords in docs.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
        public class KeepDocAttribute : Attribute{
            public KeepDocAttribute(string regexp) { }
        }

        /// <summary>
        /// Acts as a <b>Blacklist</b> filter for a Named PackSet.
        /// <para>
        /// <b>Removes</b> any packets from the set whose <b>fully qualified type name</b>
        /// (Namespace.ClassName) matches the provided Regular Expression.
        /// </para>
        /// <para>Useful for excluding specific namespaces (e.g., "Debug", "Test") from a production set.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
        public class SkipNameAttribute : Attribute{
            public SkipNameAttribute(string regexp) { }
        }

        /// <summary>
        /// Acts as a <b>Blacklist</b> filter for a Named PackSet.
        /// <para>
        /// <b>Removes</b> any packets from the set whose <b>documentation comments</b>
        /// match the provided Regular Expression.
        /// </para>
        /// <para>Useful for excluding deprecated items (e.g., matching "⛔") or internal notes.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true)]
        public class SkipDocAttribute : Attribute{
            public SkipDocAttribute(string regexp) { }
        }

        /// <summary>
        /// A marker interface to identify a struct as a communication endpoint (a "Host").
        /// </summary>
        public interface Host{ }


        /// <summary>
        /// Defines a strategy for compressing **Absolute Time** (Linear History).
        /// <br/>Values are encoded as linear offsets (in milliseconds) from a fixed <see cref="min"/> anchor.
        /// </summary>
        /// <remarks>
        /// AdHoc calculates the bits required to cover the range (<see cref="max"/> - <see cref="min"/>) at the requested <see cref="precision"/>,
        /// then rounds up to the next full <b>Byte</b> (8, 16, 24... bits).
        /// <para>
        /// <b>Optimization Strategies (Usage of spare bits):</b>
        /// <list type="bullet">
        /// <item>
        ///     <term>1. Range Expansion (Default)</term>
        ///     <description>
        ///     Keeps <see cref="precision"/> exactly as defined.
        ///     Uses spare bits to extend the <see cref="max"/> date significantly into the future.
        ///     <br/><i>Use this for long-term data validity.</i>
        ///     </description>
        /// </item>
        /// <item>
        ///     <term>2. Enhanced Precision (Prefix <c>@</c>)</term>
        ///     <description>
        ///     Keeps the <see cref="max"/> range close to what was requested.
        ///     Uses spare bits to subdivide the <see cref="precision"/> (down to a limit of 1ms).
        ///     <br/><i>Syntax: <c>public @TimeSpan precision => ...</c></i>
        ///     <br/><i>Use this for high-fidelity timestamping.</i>
        ///     </description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        public interface DateTimeDef{
            /// <summary>
            /// The fixed anchor point (Epoch) for the timeline.
            /// <br/>All encoded values are offsets relative to this date.
            /// </summary>
            DateTime min => DateTimeOffset.MinValue.LocalDateTime;

            /// <summary>
            /// The requested upper bound of the timeline.
            /// <br/><b>Note:</b> This value is dynamic. AdHoc will increase this value to utilize the remaining capacity
            /// of the allocated bytes, ensuring no bit is wasted.
            /// </summary>
            DateTime max => DateTimeOffset.MaxValue.LocalDateTime;

            /// <summary>
            /// The requested minimum time step (granularity).
            /// <br/>Prefix with <c>@</c> (e.g., <c>@TimeSpan</c>) to instruct AdHoc to automatically improve
            /// this precision using spare bit capacity (Strategy 2).
            /// </summary>
            TimeSpan precision => TimeSpan.FromMinutes(1);
        }

        /// <summary>
        /// Defines the encoding parameters for a fixed, bounded elapsed-time field.
        /// <para>
        /// Use <c>Duration</c> when a field represents "how long something took" — a non-negative
        /// duration measured from zero up to a known upper bound (e.g. request latency, task runtime,
        /// timeout intervals). Unlike <see cref="DateTimeDef"/>, it carries no calendar anchor;
        /// unlike <see cref="TimeSpanDef"/>, it is linear and non-cyclic, so no rollover protection
        /// is needed.
        /// </para>
        /// <para>
        /// AdHoc encodes the value as an integer step count in the range <c>[0, max]</c> and allocates
        /// the minimum number of whole bytes required to cover that range (up to 7 bytes). Because byte
        /// boundaries rarely align perfectly with the requested <c>max</c>, the container almost always
        /// has spare capacity. AdHoc uses those spare bits to silently promote <c>max</c> to the largest
        /// value the container can hold, keeping <see cref="precision"/> exactly as declared. You receive
        /// a wider representable range at no extra wire cost.
        /// </para>
        /// <para>
        /// Example — task runtime declared as up to 3 600 steps of 1 s (1 hour):
        /// <c>max = 3 600</c> requires 12 bits, so AdHoc allocates 2 bytes (65 536 slots).
        /// The spare 61 935 slots promote the effective ceiling to 65 535 steps (~18.2 hours),
        /// still at 1 s precision and still transmitted in 2 bytes.
        /// </para>
        /// </summary>
        public interface Duration{
            /// <summary>
            /// The upper bound of the representable range, expressed as a count of <see cref="precision"/>
            /// steps from zero.
            /// <para>
            /// AdHoc allocates the minimum number of whole bytes whose capacity covers <c>[0, max]</c>,
            /// then promotes this value to the largest step count that fits in those bytes — giving you
            /// a wider range than requested at no extra wire cost, with <see cref="precision"/> unchanged.
            /// </para>
            /// <para>
            /// Choose <c>max</c> as the worst-case duration you need to represent divided by
            /// <see cref="precision"/>. For example, a 30-second ceiling at 1 ms precision →
            /// <c>max = 30_000</c>.
            /// </para>
            /// <para>
            /// AdHoc strictly caps this value at JavaScript's <c>MAX_SAFE_INTEGER</c> (<c>(1L &lt;&lt; 53) - 1</c>).
            /// This guarantees lossless cross-platform compatibility using highly performant plain <c>Number</c> types,
            /// entirely avoiding the overhead of <c>BigInt</c>. Any requested value above this limit is automatically clamped.
            /// </para>
            /// Default: <c>(1L &lt;&lt; 53) - 1</c>.
            /// </summary>
            long max => (1L << 53) - 1;

            /// <summary>
            /// The granularity of a single encoded step.
            /// <para>
            /// The transmitted integer is multiplied by this value to recover the original duration.
            /// Choosing a coarser precision (e.g. <see cref="TimeSpan.FromMilliseconds(10)"/> instead of
            /// 1 ms) reduces the required <c>max</c> step count and can shrink the wire size by a full byte.
            /// </para>
            /// <para>
            /// Evaluated internally as total milliseconds. The resulting value is clamped to a minimum
            /// of 1 ms, and an upper limit of <c>(1L &lt;&lt; 53) - 1</c> to maintain cross-platform safety.
            /// </para>
            /// <para>
            /// Unlike <see cref="DateTimeDef"/> (which can trade precision for a longer range via the
            /// <c>@</c> prefix) and <see cref="TimeSpanDef"/> (which refines precision from spare bytes),
            /// <c>Duration</c> never adjusts precision automatically — spare capacity always extends
            /// <see cref="max"/> instead.
            /// </para>
            /// Default: <see cref="TimeSpan.FromSeconds(1)"/>.
            /// </summary>
            TimeSpan precision => TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// Defines a strategy for compressing **Relative History** (Cyclic Time / "Last N Hours").
        /// <br/>Values are encoded as offsets within a cyclic ring buffer anchored to "Now".
        /// </summary>
        /// <remarks>
        /// <b>Container Sizing:</b>
        /// <br/>AdHoc allocates the smallest number of **Bytes** (1, 2, 3...) required to hold
        /// the <see cref="interval"/> ticks.
        /// <para>
        /// <b>Safety Mechanism (The 1-Minute Gap):</b>
        /// <br/>To prevent errors where network latency causes a packet to cross a cycle boundary,
        /// AdHoc internally adds a <b>1 Minute Protection Gap</b> to your requested <see cref="interval"/>.
        /// <br/>If the spare space in the chosen byte container is less than 1 minute, the container size is increased by 1 Byte.
        /// </para>
        /// <para>
        /// <b>Automatic Optimization:</b>
        /// <br/>Any spare capacity in the Byte container is <b>always</b> applied to improve the <see cref="precision"/>.
        /// <br/><i>Example: A request for 27 Hours @ 1s precision fits in 3 Bytes. AdHoc utilizes the massive spare space in the 3rd byte
        /// to automatically upgrade precision to ~6ms.</i>
        /// </para>
        /// </remarks>
        public interface TimeSpanDef{
            /// <summary>
            /// The duration of the history window (e.g., "The last 24 hours").
            /// <br/>Acts as the modulo wrapper for the cyclic buffer.
            /// </summary>
            TimeSpan interval => TimeSpan.FromDays(1);

            /// <summary>
            /// The minimum requested step size.
            /// <br/><b>Note:</b> AdHoc will likely generate a much finer precision (smaller step)
            /// by utilizing the spare capacity of the allocated Byte container.
            /// </summary>
            TimeSpan precision => TimeSpan.FromSeconds(1);
        }

        public interface ___IConnects{ } //do not use it

        public interface Offline<L, R> : ___IConnects
            where L : struct, Host
            where R : struct, Host{ }

        /// <summary>
        /// Defines a physical or logical Connection between two endpoints, <typeparamref name="L"/> and <typeparamref name="R"/>.
        /// </summary>
        /// <typeparam name="L">The first host (often considered the "left" or initiator).</typeparam>
        /// <typeparam name="R">The second host (often considered the "right" or responder).</typeparam>
        public interface Connects<L, R> : ___IConnects
            where L : struct, Host
            where R : struct, Host{ }


        /// <summary>
        /// Declares a <b>tunnel</b>: a connection between <typeparamref name="L"/> and <typeparamref name="R"/> that have
        /// no direct link, carried instead across one or more <b>relay hosts</b> named by <typeparamref name="PATH"/>.
        /// The endpoints behave as if directly connected; their bytes physically traverse the relays, which forward them
        /// <b>without decoding, buffering, or even knowing their structure</b> — moving a payload of any size at constant
        /// memory cost and staying fully decoupled from the protocol riding through.
        /// <para>
        /// <b>Empty body (<c>{ }</c>)</b> generates the tunnel only: a raw opaque-byte pipe (a relay on each
        /// <typeparamref name="PATH"/> host and the tunnel endpoint API on <typeparamref name="L"/>/<typeparamref name="R"/>).
        /// <b>A non-empty body</b> (Actors, branches, packs — declared exactly as on a <see cref="Connects{L,R}"/>) also
        /// generates the full structured connection on the endpoints, whose serialized bytes are carried <i>through</i>
        /// the tunnel; the relays still forward opaque bytes and never parse it.
        /// </para>
        /// <para>
        /// <typeparamref name="PATH"/> is one host (single hop) or a <c>ValueTuple</c> of hosts <c>(H1, H2, …)</c>
        /// (multi-hop), listed in <b>any order</b>. The generator reconstructs the strict route
        /// <c>L → … → R</c> by walking the physical <see cref="Connects{L,R}"/> graph and rejects a declaration whose
        /// hosts do not form a single, unambiguous physical chain (every PATH element names a host, every hop must be a
        /// real <see cref="Connects{L,R}"/>, each host appears once, <typeparamref name="L"/> ≠ <typeparamref name="R"/>, and the route must be unique).
        /// </para>
        /// </summary>
        /// <typeparam name="L">First tunnel endpoint host (the "left").</typeparam>
        /// <typeparam name="R">Second tunnel endpoint host (the "right"); must differ from <typeparamref name="L"/>.</typeparam>
        /// <typeparam name="PATH">The relay host, or a tuple of relay hosts, the bytes physically pass through.</typeparam>
        public interface VirtuallyConnects<L, R, PATH> : Connects<L, R> where L : struct, Host
                                                                        where R : struct, Host{
            /// <summary>
            /// How many independent tunnels may be multiplexed over <typeparamref name="PATH"/> at once. Sets the
            /// on-wire width of the per-pack <c>tunnel_id</c> routing key: <c>1</c> ⇒ no <c>tunnel_id</c> (a single
            /// one-to-one tunnel); larger ⇒ a 1–4-byte little-endian key the relay uses to demux. Must be ≥ 1.
            /// Override by re-declaring this property in the connection interface body. Default: 256.
            /// </summary>
            int MaxTunnels => 256;

            /// <summary>
            /// Maximum size of a single tunneled stream, in KiB. The relay rejects anything larger — the same resource
            /// guard as <c>[S(N)]</c> on a stream field. Override by re-declaring this property. Default: unbounded.
            /// </summary>
            uint MaxStream_KiloBytes => uint.MaxValue;
        }

        /// <summary>
        /// Declares a <b>multiplexer</b>: one port on which a host serves several of its connections.
        /// <para>
        /// Using a multiplexer is the caller's choice, made <b>once</b>, when it sets up the connection in the
        /// generated code: connect directly, or through a multiplexer — and if so, through which one.
        /// </para>
        /// <para>
        /// A multiplexer changes how connections are <b>deployed</b>, not what they are. Every listed connection keeps
        /// its own packs, actors and states exactly as declared on its <see cref="Connects{L,R}"/>; only the port they
        /// arrive on is shared.
        /// </para>
        /// <para>
        /// <typeparamref name="CONNECTIONS"/> is a <c>ValueTuple</c> of connections <c>(C1, C2, …)</c>. The parser
        /// enforces:
        /// <list type="bullet">
        ///   <item>at least two connections — a single connection has nothing to share a port with;</item>
        ///   <item>every element is a <see cref="Connects{L,R}"/> connection — hosts, packs and
        ///         <see cref="VirtuallyConnects{L,R,PATH}"/> are rejected: a virtual connection rides a tunnel through
        ///         relay hosts and never arrives on a port of its own;</item>
        ///   <item>no connection is listed twice;</item>
        ///   <item>all connections share <b>exactly one</b> host — the one that owns the port; whether it is the
        ///         <c>L</c> or the <c>R</c> side of each connection does not matter;</item>
        ///   <item>a connection belongs to at most one multiplexer, so two multiplexers over the same set of
        ///         connections are rejected too.</item>
        /// </list>
        /// A host may own several multiplexers — one per port.
        /// </para>
        /// <para>
        /// A multiplexer follows the placement rules of a connection: it is declared directly in the project scope,
        /// its name shares the connections' namespace, and it may list connections imported from other projects.
        /// </para>
        /// </summary>
        /// <typeparam name="CONNECTIONS">A tuple of two or more connections sharing exactly one host.</typeparam>
        public interface Multiplex<CONNECTIONS>{ }

        // 1. Define the attribute[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
        public class FromAttribute<Host> : Attribute where Host : struct, Meta.Host{ }

        /// <summary>
        /// The base metadata interface for Actor definitions.
        /// This interface is used by the code generator to determine the lifecycle,
        /// addressing model, and distribution patterns of the actor.
        /// </summary>
        /// <remarks>
        /// The behavior of the actor is governed by the value assigned to <see cref="MaxActiveInstances"/>.
        /// <para>
        /// <b>Supported Patterns:</b>
        /// <list type="bullet">
        /// <item>
        /// <term><b>Singleton</b> (Default)</term>
        /// <description>
        /// Set to <c>1</c>. The actor has a fixed, well-known identity.
        /// Use this for global managers or stateful services that should only have one instance.
        /// </description>
        /// </item>
        /// <item>
        /// <term><b>Swarm</b> (Direct Addressing)</term>
        /// <description>
        /// Set to a literal integer <c>N</c> (e.g., <c>11</c>).
        /// Multiple instances are allowed, each with a unique, dynamic identity.
        /// Messages must be routed to a specific instance ID. Limit checks are enforced.
        /// </description>
        /// </item>
        /// <item>
        /// <term><b>Unlimited Swarm</b> (Unbound Direct Addressing)</term>
        /// <description>
        /// Set to <see cref="UNLIMITED"/>.
        /// Functions identically to a standard Swarm, but completely disables runtime limit-checking overhead.
        /// Recommended for private, controlled, or trusted environments where resource exhaustion is not a risk.
        /// </description>
        /// </item>
        /// <item>
        /// <term><b>Multicast Swarm</b> (Fan-out)</term>
        /// <description>
        /// Set using the <b>unary plus operator</b> (e.g., <c>+11</c>).
        /// All instances share a single, well-known address.
        /// A message sent to that address is automatically broadcast to all active instances.
        /// </description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        public interface Actor{
            /// <summary>
            /// Constant used to disable allocation limit checks for multi-instance actors, saving runtime overhead.
            /// </summary>
            const int UNLIMITED = 0;

            /// <summary>
            /// Defines the maximum number of concurrent instances and the addressing mode.
            /// </summary>
            /// <value>
            /// Use <c>1</c> for a Singleton.
            /// Use a standard integer (e.g., <c>10</c>) for individual addressing with a hard limit.
            /// Use <see cref="UNLIMITED"/> for unbounded instances without limit-check overhead.
            /// Use the plus prefix (e.g., <c>+10</c>) to enable Multicasting/Pub-Sub behavior.
            /// </value>
            int MaxActiveInstances => 1;
        }

        /// <summary>
        /// Sets the timeout in seconds for transmitting data on a connection.
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct)]
        public class TransmitTimeoutAttribute : Attribute{
            public TransmitTimeoutAttribute(uint seconds) { }
        }

        /// <summary>
        /// Sets the timeout in seconds for receiving data on a connection.
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct)]
        public class ReceiveTimeoutAttribute : Attribute{
            public ReceiveTimeoutAttribute(uint seconds) { }
        }

        /// <summary>
        /// How long, in minutes, a session outlives the socket that carried it. When the socket is lost — by an error, a
        /// timeout or the peer closing it — and the peer has identified itself, the connection is parked for that long
        /// with its actors, states and application state intact; a peer that reconnects within the time and names its
        /// session continues it. Zero, the default, ends the session with the socket. A graceful <c>Close</c> or an
        /// <c>Abort</c> by the application always ends it.
        /// <para>
        /// On a <b>Connection</b>: the value for all of its states. Mandatory, at least 1, on a Connection that declares a
        /// <see cref="Resumable{HOST, PACKS}"/>; may also be added by a <see cref="Modify{Target}"/> to an imported Connection.
        /// On a <b>State</b>: the value while an actor is in that state; it OVERRIDES the Connection default (<c>[Resumable(0)]</c> turns
        /// holding off in that state, <c>[Resumable]</c> with no argument keeps the Connection value). A state without the attribute
        /// inherits the Connection value. Pick one default and mark the exceptions: no Connection attribute + a few resumable states,
        /// or a Connection value + <c>[Resumable(0)]</c> on the states that do not need it.
        /// <para>
        /// Guaranteed delivery is never dropped by a short state value: while the journal of a <see cref="Resumable{HOST, PACKS}"/> leg still
        /// holds unacknowledged packs, the wait is the <b>larger</b> of the state value and the Connection value - the max applies only in that
        /// competition with the Resumable packs, not between a state and its Connection.
        /// </para>
        /// </para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Interface)]
        public class ResumableAttribute : Attribute{
            public ResumableAttribute(uint minutes) { }
            public ResumableAttribute() { } // no argument on a State: hold, using the Connection's minutes
        }

        /// <summary>
        /// The Connection is carried over UDP. A datagram can be lost on its own, while the connection stays up, so every pack on a
        /// UDP Connection is guaranteed, both ways: it is as if each of its two hosts declared a <see cref="Resumable{HOST, PACKS}"/>
        /// over every pack it sends there. A <see cref="Resumable{HOST, PACKS}"/> declared inside it is an error.
        /// <para>
        /// <c>minutes</c> is the Connection's <see cref="ResumableAttribute"/>: how long a session outlives its socket, at least one
        /// minute. Without it the Connection has to carry <c>[Resumable(minutes)]</c>; given both, they have to agree.
        /// </para>
        /// <para>
        /// A stage chain on the Connection is applied to each pack it carries, not to the stream - as on any Connection that
        /// guarantees packs. The runtimes with a UDP transport are C# and Java: a host generated in TypeScript cannot be on a
        /// UDP Connection, and a UDP Connection cannot be listed in a <see cref="Multiplex{CONNECTIONS}"/>.
        /// </para>
        /// On a Connection only - not on an Actor or a State: the transport is chosen for the whole Connection.
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Interface)]
        public class UDPAttribute : Attribute{
            public UDPAttribute(uint minutes) { }
            public UDPAttribute() { } // the minutes come from [Resumable(minutes)] on the Connection
        }

        /// <summary>
        /// <see cref="UDPAttribute"/> with packs left unguarded: the packs of <c>EXCLUDE_PACKS_SET</c> travel on the Connection as
        /// before, both ways, but with no position header, no journal and no replay - a lost one is lost for good, unnoticed. For
        /// packs the next one replaces anyway: telemetry, positions, snapshots. <c>EXCLUDE_PACKS_SET</c> is a Pack Set, or a tuple
        /// of packs, Pack Sets, scopes and exclusions, as the <c>PACKS</c> of a <see cref="Resumable{HOST, PACKS}"/>.
        /// </summary>
        [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Interface)]
        public class UDPAttribute<EXCLUDE_PACKS_SET> : Attribute{
            public UDPAttribute(uint minutes) { }
            public UDPAttribute() { } // the minutes come from [Resumable(minutes)] on the Connection
        }

#region FSM Branch Attributes
        // ============================================================================================
        // Branch attributes declare the packs a state can send in one direction, plus the optional
        // FSM transition target. They are always placed on empty state structs (no fields, no methods).
        //
        // Naming convention — the letter position shows direction, its case shows authority:
        //
        //     L / l  ← packs sent from the Left host              (first of Connects<L, R>)
        //     R / r  ← packs sent from the Right host             (second of Connects<L, R>)
        //     lr     ← packs either host may send (peer / bidi)
        //     UPPER  ← Master: this side drives the transition (goto TARGET_STATE)
        //     lower  ← Follower: non-transitional, the FSM stays in this state
        //
        // Each kind has TWO overloads:
        //     * With the `PACKS` generic — pack collection built from type arguments (+ optional filters).
        //     * Without `PACKS`          — the four filters scan ALL transmittable packs in the project.
        //
        // Constructor parameters (identical across all kinds, all optional, all regex):
        //     string KeepDoc  = ""  — keep  only packs whose unified doc pool matches.
        //     string SkipDoc  = ""  — drop  packs whose unified doc pool matches.
        //     string KeepName = ""  — keep  only packs whose full type name matches.
        //     string SkipName = ""  — drop  packs whose full type name matches.
        //
        // Evaluation order is sequential: KeepDoc → SkipDoc → KeepName → SkipName.
        // Multiple Keeps narrow (intersection); Skips remove from whatever survived.
        //
        // The UNIFIED DOC POOL of a pack is the concatenation of:
        //     1. its class-level `///` XML doc comments, and
        //     2. its tags - the user text after the numbers on its line in the packs table at the top of the file.
        // Both sources are treated as one searchable body.
        //
        // The FULL TYPE NAME is the namespace-qualified type path (e.g. `com.my.company.Server.V2.Query`).
        //
        // `@` PREFIX in the PACKS generic (critical):
        //     * `T`   — direct inclusion; the four filters DO NOT touch it.
        //     * `@T`  — scope; enumerate all transmittable packs inside T, THEN apply the four filters.
        // If any `@` scope appears in PACKS, the filters are confined to those scopes —
        // they never widen to the whole project.
        // ============================================================================================

        /// <summary>
        /// Non-transitional Left-host Follower branch (L → R, FSM stays in this state) without a PACKS
        /// generic. The four filters scan every transmittable pack in the project.
        /// </summary>
        /// <remarks>
        /// Use when the packs you want are identified purely by their tags in the packs table or by name.
        /// <para><b>Examples</b></para>
        /// <code>
        /// // Every pack tagged "📈" in the packs table / class doc.
        /// [l____________("📈")]
        /// struct MetricsState { }
        ///
        /// // Keep-then-skip on doc: packs tagged "metrics" but NOT also tagged "legacy".
        /// [l____________("metrics", "legacy")]
        /// struct ActiveMetricsState { }
        ///
        /// // By-name filter (named arg syntax lets you skip KeepDoc/SkipDoc).
        /// [l____________(KeepName: @"\.Telemetry\.")]
        /// struct TelemetryState { }
        ///
        /// // Combined: doc tag "📈" AND type name excluding "Test".
        /// [l____________("📈", SkipName: @"Test")]
        /// struct MetricsNoTests { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class l____________Attribute : Attribute{
            /// <param name="KeepDoc">Regex; only packs whose unified doc pool matches this pattern are kept. Empty = no keep-filter.</param>
            /// <param name="SkipDoc">Regex; packs matching this pattern are removed after KeepDoc. Empty = no skip-filter.</param>
            /// <param name="KeepName">Regex; only packs whose full type name matches this pattern are kept. Empty = no keep-filter.</param>
            /// <param name="SkipName">Regex; packs whose full type name matches this pattern are removed after KeepName. Empty = no skip-filter.</param>
            public l____________Attribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// Non-transitional Left-host Follower branch (L → R, FSM stays in this state) whose PACKS
        /// generic selects the packs. Types without <c>@</c> are direct inclusions (filters bypass them);
        /// types with <c>@</c> are scopes — their enumerated packs are run through the four filters.
        /// </summary>
        /// <typeparam name="PACKS">
        /// A single type, a tuple, a Named Pack Set, a scope (<c>@Project</c>, <c>@Host</c>, <c>@Pack</c>),
        /// a filter template (<c>ActiveOnly&lt;@Project&gt;</c>), or an exclusion (<c>X&lt;LegacyPack&gt;</c>).
        /// Mix freely inside a tuple.
        /// </typeparam>
        /// <remarks>
        /// <para><b>Examples</b></para>
        /// <code>
        /// // Direct packs only — no filtering, exact set.
        /// [l____________&lt;(PackA, PackB, PackC)&gt;]
        /// struct DirectState { }
        ///
        /// // Scope with all four filters — they apply only inside @Project.
        /// [l____________&lt;@Project&gt;("📈", "⛔", @"\.V2\.", @"Test")]
        /// struct ProjectV2MetricsState { }
        ///
        /// // Mixed — DirectPack is always included; filters apply only to @Monitoring and @Logging.
        /// [l____________&lt;(DirectPack, @Monitoring, @Logging)&gt;("critical", "deprecated")]
        /// struct MixedState { }
        ///
        /// // KeepName scoped to a specific host (named-arg syntax).
        /// [l____________&lt;@Server&gt;(KeepName: @"\.V2\.")]
        /// struct ServerV2State { }
        ///
        /// // Named Pack Set used as scope (`@`) — branch-level filters applied to its contents.
        /// [l____________&lt;@ImplementOnObserver&gt;("sessions", SkipName: @"Virtual")]
        /// struct FilteredObserverState { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class l____________Attribute<PACKS> : Attribute{
            /// <param name="KeepDoc">Regex applied to `@`-scoped packs inside <typeparamref name="PACKS"/>. Direct packs (no `@`) bypass.</param>
            /// <param name="SkipDoc">Regex applied after KeepDoc, on `@`-scoped packs only.</param>
            /// <param name="KeepName">Regex applied after SkipDoc, matched against the pack's full type name.</param>
            /// <param name="SkipName">Regex applied after KeepName, matched against the pack's full type name.</param>
            public l____________Attribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }


        /// <summary>
        /// Transitional Left-host Master branch (L → R) whose PACKS generic selects the packs and whose
        /// <typeparamref name="TARGET_STATE"/> generic is the state the FSM advances to after one of the
        /// selected packs is sent.
        /// </summary>
        /// <typeparam name="TARGET_STATE">The state struct the FSM transitions to. Use <c>Close</c> or <c>End</c> for terminal transitions.</typeparam>
        /// <typeparam name="PACKS">Same expression language as <see cref="l____________Attribute{PACKS}"/>.</typeparam>
        /// <remarks>
        /// <para><b>Examples</b></para>
        /// <code>
        /// // Agent sends one of two concrete packs, then advances to `LoginResponse`.
        /// [L____________&lt;LoginResponse, (Agent.Login, Agent.ResumeToken)&gt;]
        /// struct Login { }
        ///
        /// // Agent sends any project-scoped pack tagged "handshake", then advances to `Next`.
        /// [L____________&lt;Next, @Project&gt;("handshake")]
        /// struct Start { }
        ///
        /// // Scoped KeepName: only @Server types matching `\.V2\.` drive the transition.
        /// [L____________&lt;Next, @Server&gt;(KeepName: @"\.V2\.")]
        /// struct V2Handshake { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class L____________Attribute<TARGET_STATE, PACKS> : Attribute where TARGET_STATE : struct{
            /// <param name="KeepDoc">Regex applied to `@`-scoped packs inside <typeparamref name="PACKS"/>.</param>
            /// <param name="SkipDoc">Regex applied after KeepDoc.</param>
            /// <param name="KeepName">Regex applied to the pack's full type name.</param>
            /// <param name="SkipName">Regex applied after KeepName.</param>
            public L____________Attribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// Transitional Left-host Master branch (L → R) without a PACKS generic — the four filters scan
        /// every transmittable pack in the project.
        /// </summary>
        /// <typeparam name="TARGET_STATE">The state struct the FSM transitions to. Use <c>Close</c> or <c>End</c> for terminal transitions.</typeparam>
        /// <remarks>
        /// <para><b>Example</b></para>
        /// <code>
        /// // Any project-wide pack tagged "bye" sends and closes the connection.
        /// [L____________&lt;Close&gt;("bye")]
        /// struct GoodByeState { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class L____________Attribute<TARGET_STATE> : Attribute where TARGET_STATE : struct{
            /// <param name="KeepDoc">Regex; only project packs whose unified doc pool matches are kept.</param>
            /// <param name="SkipDoc">Regex; packs matching this are removed after KeepDoc.</param>
            /// <param name="KeepName">Regex; only packs whose full type name matches are kept.</param>
            /// <param name="SkipName">Regex; packs whose full type name matches are removed after KeepName.</param>
            public L____________Attribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// RPC marker interface — used in the return-tuple of RPC shorthand methods to indicate
        /// "the Left host initiates the call, the Right host returns". The code generator synthesizes
        /// a two-state FSM (Call → Return → End) from the method signature.
        /// </summary>
        /// <example>
        /// <code>
        /// // L calls GetUser(UserId), R returns UserProfile.
        /// (L____________, UserProfile) GetUser(UserId id);
        /// </code>
        /// </example>
        public interface L____________{ }


        /// <summary>
        /// Non-transitional Right-host Follower branch (R → L, FSM stays in this state) whose PACKS
        /// generic selects the packs. See <see cref="l____________Attribute{PACKS}"/> for the full
        /// expression grammar — direction is the only difference.
        /// </summary>
        /// <typeparam name="PACKS">A single type, tuple, Pack Set, scope (<c>@X</c>), filter template, or <c>X&lt;...&gt;</c> exclusion.</typeparam>
        /// <remarks>
        /// <para><b>Example</b></para>
        /// <code>
        /// // Observer (R) sends back any volatile-subscription control pack whose type name ends in Subscribe/Unsubscribe.
        /// [____________r&lt;@Monitoring.VolatileInfo&gt;(KeepName: @"\.(Subscribe|Unsubscribe)$")]
        /// struct VolatileControl { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class ____________rAttribute<PACKS> : Attribute{
            /// <param name="KeepDoc">Regex applied to `@`-scoped packs inside <typeparamref name="PACKS"/>.</param>
            /// <param name="SkipDoc">Regex applied after KeepDoc.</param>
            /// <param name="KeepName">Regex applied to the pack's full type name.</param>
            /// <param name="SkipName">Regex applied after KeepName.</param>
            public ____________rAttribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// Non-transitional Right-host Follower branch (R → L) without a PACKS generic — the four filters
        /// scan every transmittable pack in the project.
        /// </summary>
        /// <remarks>
        /// <para><b>Example</b></para>
        /// <code>
        /// // Right host echoes back any pack tagged "ack" project-wide.
        /// [____________r("ack")]
        /// struct Acks { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class ____________rAttribute : Attribute{
            /// <param name="KeepDoc">Regex; only project packs whose unified doc pool matches are kept.</param>
            /// <param name="SkipDoc">Regex; packs matching this are removed after KeepDoc.</param>
            /// <param name="KeepName">Regex; only packs whose full type name matches are kept.</param>
            /// <param name="SkipName">Regex; packs whose full type name matches are removed after KeepName.</param>
            public ____________rAttribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// Transitional Right-host Master branch (R → L) whose PACKS generic selects the packs and whose
        /// <typeparamref name="TARGET_STATE"/> is the state the FSM advances to.
        /// </summary>
        /// <typeparam name="TARGET_STATE">The state struct to transition to. Use <c>Close</c> or <c>End</c> for terminal transitions.</typeparam>
        /// <typeparam name="PACKS">See <see cref="l____________Attribute{PACKS}"/> for the expression grammar.</typeparam>
        /// <remarks>
        /// <para><b>Examples</b></source>
        /// <code>
        /// // Server sends Invitation or InvitationUpdate, moves to TodoJobRequest.
        /// [____________R&lt;TodoJobRequest, (Server.Invitation, Server.InvitationUpdate)&gt;]
        /// struct LoginResponseSuccess { }
        ///
        /// // Server sends any error-tagged pack, closes the connection.
        /// [____________R&lt;Close, @Server&gt;("error")]
        /// struct Failure { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class ____________RAttribute<TARGET_STATE, PACKS> : Attribute where TARGET_STATE : struct{
            /// <param name="KeepDoc">Regex applied to `@`-scoped packs inside <typeparamref name="PACKS"/>.</param>
            /// <param name="SkipDoc">Regex applied after KeepDoc.</param>
            /// <param name="KeepName">Regex applied to the pack's full type name.</param>
            /// <param name="SkipName">Regex applied after KeepName.</param>
            public ____________RAttribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// Transitional Right-host Master branch (R → L) without a PACKS generic — the four filters scan
        /// every transmittable pack in the project.
        /// </summary>
        /// <typeparam name="TARGET_STATE">The state struct to transition to.</typeparam>
        /// <remarks>
        /// <para><b>Example</b></para>
        /// <code>
        /// // Right host sends any pack tagged "bye" project-wide, then closes.
        /// [____________R&lt;Close&gt;("bye")]
        /// struct Disconnect { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class ____________RAttribute<TARGET_STATE> : Attribute where TARGET_STATE : struct{
            /// <param name="KeepDoc">Regex; only project packs whose unified doc pool matches are kept.</param>
            /// <param name="SkipDoc">Regex; packs matching this are removed after KeepDoc.</param>
            /// <param name="KeepName">Regex; only packs whose full type name matches are kept.</param>
            /// <param name="SkipName">Regex; packs whose full type name matches are removed after KeepName.</param>
            public ____________RAttribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// RPC marker interface — used in the return-tuple of RPC shorthand methods to indicate
        /// "the Right host initiates the call, the Left host returns". The code generator synthesizes
        /// a two-state FSM (Call → Return → End) from the method signature.
        /// </summary>
        /// <example>
        /// <code>
        /// // R asks for metrics; L returns a CPU list.
        /// (____________R, CPU.BytesTime.List) GetCPU(CPU.BytesTime.List.ForPeriod req);
        /// </code>
        /// </example>
        public interface ____________R{ }


        /// <summary>
        /// Non-transitional Peer branch (L ↔ R, both hosts can send, FSM stays in this state) whose
        /// PACKS generic selects the packs. Used for symmetric bidirectional exchanges.
        /// </summary>
        /// <typeparam name="PACKS">See <see cref="l____________Attribute{PACKS}"/> for the expression grammar.</typeparam>
        /// <remarks>
        /// <para><b>Example</b></para>
        /// <code>
        /// // Either host may send any LayoutFile-related pack in this state.
        /// [_____lr_____&lt;@LayoutFile&gt;]
        /// struct LayoutExchange { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class _____lr_____Attribute<PACKS> : Attribute{
            /// <param name="KeepDoc">Regex applied to `@`-scoped packs inside <typeparamref name="PACKS"/>.</param>
            /// <param name="SkipDoc">Regex applied after KeepDoc.</param>
            /// <param name="KeepName">Regex applied to the pack's full type name.</param>
            /// <param name="SkipName">Regex applied after KeepName.</param>
            public _____lr_____Attribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }

        /// <summary>
        /// Non-transitional Peer branch (L ↔ R) without a PACKS generic — the four filters scan every
        /// transmittable pack in the project.
        /// </summary>
        /// <remarks>
        /// <para><b>Example</b></para>
        /// <code>
        /// // Any project-wide pack tagged "heartbeat" travels in either direction.
        /// [_____lr_____("heartbeat")]
        /// struct Heartbeat { }
        /// </code>
        /// </remarks>
        [AttributeUsage(AttributeTargets.Struct, AllowMultiple = true)]
        public class _____lr_____Attribute : Attribute{
            /// <param name="KeepDoc">Regex; only project packs whose unified doc pool matches are kept.</param>
            /// <param name="SkipDoc">Regex; packs matching this are removed after KeepDoc.</param>
            /// <param name="KeepName">Regex; only packs whose full type name matches are kept.</param>
            /// <param name="SkipName">Regex; packs whose full type name matches are removed after KeepName.</param>
            public _____lr_____Attribute(string KeepDoc = "", string SkipDoc = "", string KeepName = "", string SkipName = "") { }
        }
#endregion


        /// <summary>
        /// A special terminal target state that deallocates the current Actors instance.
        /// This effectively deletes the linked actors,
        /// while leaving the underlying physical connection open for other actors.
        /// </summary>
        public struct End{ }

        /// <summary>
        /// A special terminal target state that gracefully terminates the physical connection.
        /// This ensures the transmission queue is fully drained before closing the pipe.
        /// Once all pending data is sent, the communication link between hosts is safely shut down.
        /// </summary>
        public struct Close{ }

        public class Empty{ }

        /// <summary>
        /// An attribute that assigns a compile-time constant value to the annotated field.
        /// </summary>
        /// <example>
        /// Applying `[ValueFor(42L)]` copies the compile-time value 42 to a constant field.
        /// </example>
        [AttributeUsage(AttributeTargets.Field)]
        // Attribute for a static field whose value is computed at compile time and then copied to the specified constant field.
        public class ValueForAttribute : Attribute{
            public ValueForAttribute(long   to_const) { }
            public ValueForAttribute(double to_const) { }
        }


#region Metaprogramming Instructions
        /// <summary>
        /// A metaprogramming instruction to modify the definition of a <typeparamref name="Target"/> type.
        /// <para>
        /// <b>Fields merge, attributes replace.</b> What the modifier declares as fields is merged into the target,
        /// cumulatively across every layer that modifies it. Attributes behave the other way round: a modifier that
        /// declares <b>any</b> attribute replaces the target's <b>whole</b> attribute set with its own, and a modifier
        /// that declares none leaves the target's attributes untouched. Write
        /// <see cref="ClearAttributesAttribute"/> to replace the set with an empty one.
        /// </para>
        /// <para>
        /// Replacement is the only way to re-declare the attributes of a type you do not own - a pack imported from
        /// another project, whose author cannot know which connections a later extension will add. The whole set is
        /// replaced rather than merged because an attribute list is ordered and position-bearing: a
        /// <see cref="StreamStageAttribute">transform chain</see> means what it means only as a whole, so the layer
        /// that re-declares it states all of it.
        /// </para>
        /// <para>
        /// <b>Branch attributes are not part of that set.</b> <c>L____________</c>, <c>l____________</c>,
        /// <c>____________R</c>, <c>____________r</c> and <c>_____lr_____</c> spell a state's transitions, which is
        /// its body rather than its tuning; they are merged like fields and a replacement never drops them.
        /// </para>
        /// <para>
        /// <b>The topmost layer wins.</b> When several projects modify one target's attributes, the one whose project
        /// imports the others - directly or transitively - decides. Two modifiers in projects that do not import one
        /// another leave the outcome to composition order and are refused.
        /// </para>
        /// </summary>
        /// <typeparam name="Target">The type to be modified.</typeparam>
        public interface Modify<Target>{ }

        public interface Modify<Target, Modifications>{ }

        /// <summary>
        /// A metaprogramming instruction to modify a specific <typeparamref name="TargetConnection"/> by redefining its hosts.
        /// </summary>
        /// <typeparam name="TargetConnection">The connection definition to modify.</typeparam>
        /// <typeparam name="L">The new "left" host.</typeparam>
        /// <typeparam name="R">The new "right" host.</typeparam>
        public interface Modify<TargetConnection, L, R>{ }

        /// <summary>
        /// On a <see cref="Modify{Target}"/> modifier: replace the target's attribute set with an <b>empty</b> one.
        /// <para>
        /// A modifier declaring no attributes at all means "leave the target's attributes alone", which is what a
        /// modifier that only merges fields must mean. So stripping them needs something to write, and this is it:
        /// the marker makes the attribute list syntactically non-empty while the set it denotes is empty. It carries
        /// no meaning of its own and never reaches the target.
        /// </para>
        /// <para>
        /// It removes what the target's own declaration carries - its transform chain, its trims, its
        /// <c>[S(N)]</c>, its metadata. It does not touch the branch attributes of a state, which spell transitions
        /// rather than tuning, nor anything a <b>field</b> declares: a field is replaced by re-declaring the field.
        /// </para>
        /// <code>
        /// // FileEntry.List reaches the wire compressed and cut for its own leg. Here it must not.
        /// [ClearAttributes] class PlainList : Modify&lt;FileEntry.List&gt; { }
        /// </code>
        /// <para>
        /// Meaningless, and refused, together with any other attribute on the same modifier: the two halves would
        /// say opposite things about one set.
        /// </para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
        public class ClearAttributesAttribute : Attribute{ }

        /// <summary>
        /// A metaprogramming instruction to swap the Left (L) and Right (R) roles in a connection definition.
        /// </summary>
        /// <typeparam name="Connection">The connection whose hosts will be swapped.</typeparam>
        public interface SwapHosts<Connection>{ }
#endregion

        // The following `X` interfaces are metaprogramming instructions used to exclude one or more types
        // from a definition. This pattern simulates variadic generics to allow removing a variable
        // number of types in a single declaration.
        /// <typeparam name="TYPES">A type or tuple of types.</typeparam>
        public interface X<TYPES>{ }


        // The following `_` interfaces are universal wrappers for composition. They are used to group
        // multiple types (including classes, structs, enums, and other interfaces) into a single logical
        // unit for the metaprogramming engine. This pattern simulates variadic generics, allowing a
        // variable number of types to be composed together.
        /// <typeparam name="TYPES">A type or tuple of types.</typeparam>
        public interface _<TYPES>{ }
    }
}