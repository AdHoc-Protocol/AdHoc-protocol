//What the bytes of a connection go through: chosen here, run in the native library, adhoc-crypto, in one call a buffer each way.
//The same library, the same wire and the same layout of its memory as the Java runtime's Chain.java: see native/README.md in InJAVA.
//
//  new Chain().Tls("host", "http/1.1").NoiseIK(serverKey, myKey).OnPeer(key => Known(key))
//
//The frames of a WebSocket join in by themselves: a WebSocket connection has them. On the way out each layer only writes down what is
//to be done to this buffer, the last one sends it all into the native library; on the way in that call opens TLS, cuts the frames and
//opens Noise, and the data lie where the socket put them, in runs: each goes up on its own, nothing moves that does not have to.
//
//A connection with no chain, and no key, never touches the library, nor anything here.
//
//The library: libadhoc-crypto-<version>.<dll|so|dylib>, in the package AdHoc.Crypto of the release:
//  https://github.com/AdHoc-Protocol/adhoc-crypto/releases/latest/download/AdHoc.Crypto.nupkg
//The Project.csproj and nuget.config the generator writes take it from there into obj/adhoc-crypto at the first restore. In a project of your
//own put the file into a folder, in the nuget.config:
//  <packageSources><add key="adhoc" value="that folder" /></packageSources>
//and in the csproj:
//  <PackageReference Include="AdHoc.Crypto" Version="0.1.*" />
//A build and a publish lay it out by themselves: in runtimes/<rid>/native, or next to the program with `dotnet publish -r <rid>`.
//It is found through ADHOC_CRYPTO_NATIVE, the full name of the file, next to the program, in what the runtime knows from the package.
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace org.unirail;

///<summary>The native library: BoringSSL and the code around it. Plain C calls, on memory the caller holds still.</summary>
public static unsafe class Crypto
{
    const string LIB = "adhoc-crypto", VERSION = "0.1";

    ///<summary>What this code expects of the native library.</summary>
    const int ABI = 9;

    static Crypto()
    {
        try
        {
            NativeLibrary.SetDllImportResolver(typeof(Crypto).Assembly, Resolve);
        }
        catch (InvalidOperationException)
        {
        } //the program has its own resolver: it knows where the library is
        if (adhoc_abi() != ABI)
            throw new DllNotFoundException("The native library adhoc-crypto does not match this lib: take the one of its release.");
    }

    ///<summary>The library is loaded: anything the library is needed for says here what is missing.</summary>
    internal static void Load() {}

    static IntPtr Resolve(string name, Assembly assembly, DllImportSearchPath? search)
    {
        if (name != LIB)
            return IntPtr.Zero;
        var ext = OperatingSystem.IsWindows() ? "dll" : OperatingSystem.IsMacOS() ? "dylib"
                                                                                  : "so";
        var file = $"libadhoc-crypto-{VERSION}.{ext}";
        foreach (var path in new[] {
                     Environment.GetEnvironmentVariable("ADHOC_CRYPTO_NATIVE"),
                     Path.Combine(AppContext.BaseDirectory, file),
                 })
            if (!string.IsNullOrEmpty(path) && File.Exists(path) && NativeLibrary.TryLoad(path, out var handle))
                return handle;
        if (NativeLibrary.TryLoad(file, assembly, search, out var known)) //the folder of this platform in the package, from <program>.deps.json
            return known;
        throw new DllNotFoundException($"Encryption needs the native library {file}, of this platform: {RuntimeInformation.RuntimeIdentifier}, from the package AdHoc.Crypto. See the head of Chain.cs.");
    }

    [DllImport(LIB)]
    static extern int adhoc_abi();
    [DllImport(LIB)]
    internal static extern int adhoc_aead_size();
    [DllImport(LIB)]
    internal static extern int adhoc_aead_init(int kind, void *ctx, byte *key, int key_len);
    [DllImport(LIB)]
    internal static extern int adhoc_seal(long ctx, long n, long ad, int ad_len, long text, int len);
    [DllImport(LIB)]
    internal static extern int adhoc_open(long ctx, long n, long ad, int ad_len, long text, int len);
    [DllImport(LIB)]
    internal static extern int adhoc_seal_nonce(long ctx, byte *nonce, byte *ad, int ad_len, byte *text, int len);
    [DllImport(LIB)]
    internal static extern int adhoc_open_nonce(long ctx, byte *nonce, byte *ad, int ad_len, byte *text, int len);
    [DllImport(LIB)]
    internal static extern long adhoc_seal_records(long ctx, long n, long address, int bytes);
    [DllImport(LIB)]
    internal static extern long adhoc_open_records(long ctx, long n, long address, int have, int max);
    [DllImport(LIB)]
    internal static extern int adhoc_stream_size();
    [DllImport(LIB)]
    internal static extern void adhoc_stream_xor(long state, long address, int len);
    [DllImport(LIB)]
    internal static extern int adhoc_x25519(byte *shared, byte *secret, byte *peer);
    [DllImport(LIB)]
    internal static extern void adhoc_x25519_public(byte *pub, byte *secret);
    [DllImport(LIB)]
    internal static extern void adhoc_x25519_keypair(byte *secret, byte *pub);
    [DllImport(LIB)]
    internal static extern int adhoc_noise_size();
    [DllImport(LIB)]
    internal static extern int adhoc_noise_tx();
    [DllImport(LIB)]
    internal static extern int adhoc_noise_rx();
    [DllImport(LIB)]
    internal static extern void adhoc_noise_init(long state, int pattern, int initiator, byte *prologue, int prologue_len, byte *static_secret, byte *static_public, byte *peer, byte *ephemeral);
    [DllImport(LIB)]
    internal static extern int adhoc_noise_write(long state, byte *dst, int cap, byte *payload, int len);
    [DllImport(LIB)]
    internal static extern int adhoc_noise_read(long state, byte *msg, int len, byte *payload, int room);
    [DllImport(LIB)]
    internal static extern long adhoc_tls_context();
    [DllImport(LIB)]
    internal static extern long adhoc_tls_server_context(byte *chain, int chain_len, byte *key, int key_len);
    [DllImport(LIB)]
    internal static extern long adhoc_tls_server(long ctx);
    [DllImport(LIB)]
    internal static extern long adhoc_tls_client(long ctx, byte *host, byte *alpn, int alpn_len, delegate *unmanaged<void *, int, byte **, int *, int> verify, void *verify_ctx);
    [DllImport(LIB)]
    internal static extern long adhoc_tls_handshake(long conn, long input, int in_len, long output, int out_cap);
    [DllImport(LIB)]
    internal static extern int adhoc_tls_control(long conn, long record);
    [DllImport(LIB)]
    internal static extern void adhoc_tls_close_notify(long conn);
    [DllImport(LIB)]
    internal static extern void adhoc_tls_tx_limit(long conn, long records);
    [DllImport(LIB)]
    internal static extern byte *adhoc_tls_error(long conn);
    [DllImport(LIB)]
    internal static extern void adhoc_tls_free(long conn);
    [DllImport(LIB)]
    internal static extern int adhoc_chain_size();
    [DllImport(LIB)]
    static extern void adhoc_ed25519_keypair(byte *secret, byte *pub, byte *seed);
    [DllImport(LIB)]
    static extern int adhoc_ed25519_sign(byte *signature, byte *secret, byte *message, long len);
    [DllImport(LIB)]
    static extern int adhoc_ed25519_verify(byte *signature, byte *pub, byte *message, long len);
    [DllImport(LIB)]
    internal static extern int adhoc_pair_key(byte *key, byte *iv, int *id, byte *secret, byte *their, byte *label, int label_len);
    [DllImport(LIB)]
    internal static extern int adhoc_pack_seal(long ctx, long n, long secret, long address, int header, int len);
    [DllImport(LIB)]
    internal static extern int adhoc_pack_open(long ctx, long n, long pub, long address, int header, int total);

    ///<summary>A signing pair of Ed25519: 64 bytes of secret, 32 of public. <paramref name="seed"/>: 32 bytes it comes from; empty - random.</summary>
    public static void Ed25519KeyPair(Span<byte> secret, Span<byte> pub, ReadOnlySpan<byte> seed = default)
    {
        if (secret.Length < 64 || pub.Length < 32 || !seed.IsEmpty && seed.Length < 32)
            throw new ArgumentException("Ed25519: a secret is 64 bytes, a public key and a seed 32.");
        fixed(byte *s = secret, p = pub, d = seed)
            adhoc_ed25519_keypair(s, p, seed.IsEmpty ? null : d);
    }

    ///<summary>The signature of <paramref name="message"/>, 64 bytes.</summary>
    public static bool Ed25519Sign(Span<byte> signature, ReadOnlySpan<byte> secret, ReadOnlySpan<byte> message)
    {
        if (signature.Length < 64 || secret.Length < 64)
            throw new ArgumentException("Ed25519: a signature and a secret are 64 bytes.");
        fixed(byte *o = signature, s = secret, m = message) return adhoc_ed25519_sign(o, s, m, message.Length) != 0;
    }

    public static bool Ed25519Verify(ReadOnlySpan<byte> signature, ReadOnlySpan<byte> pub, ReadOnlySpan<byte> message)
    {
        if (signature.Length < 64 || pub.Length < 32)
            return false;
        fixed(byte *o = signature, p = pub, m = message) return adhoc_ed25519_verify(o, p, m, message.Length) != 0;
    }
    [DllImport(LIB)]
    internal static extern int adhoc_chain_tx(long chain);
    [DllImport(LIB)]
    internal static extern long adhoc_chain_rx(long chain, byte *@base, int at, int limit);
}

public sealed unsafe class Chain
{
    ///<summary>Decides whether the chain of certificates a server has shown, the leaf first, is to be trusted; the host name is checked apart. null - the roots of the system.</summary>
    public static volatile Func<X509Certificate2[], bool>? trust;

    ///<summary>With the roots of the system: whether a certificate is also checked for being revoked. Off by default, as in SslStream: the check goes to the network during the handshake, and a certificate with no answer about it fails.</summary>
    public static volatile bool checkRevocation;

    static long context;

    static long Context()
    {
        lock (typeof(Chain))
        {
            if (context == 0)
                context = Crypto.adhoc_tls_context();
            return context;
        }
    }

    [UnmanagedCallersOnly]
    static int Verify(void *ctx, int count, byte **der, int *len)
    {
        try
        {
            var chain = new X509Certificate2[count];
            for (var i = 0; i < count; i++)
                chain[i] = X509CertificateLoader.LoadCertificate(new ReadOnlySpan<byte>(der[i], len[i]));
            var given = trust;
            if (given != null)
                return given(chain) ? 1 : 0;
            using var check = new X509Chain();
            check.ChainPolicy.RevocationMode = checkRevocation ? X509RevocationMode.Online : X509RevocationMode.NoCheck;
            for (var i = 1; i < count; i++)
                check.ChainPolicy.ExtraStore.Add(chain[i]);
            return check.Build(chain[0]) ? 1 : 0;
        }
        catch
        {
            return 0; //nothing may escape into native code
        }
    }

    internal string? tlsHost, tlsAlpn;
    internal long tlsServer;
    internal int pattern = -1;
    internal byte[]? staticSecret, peer;
    internal Predicate<byte[]>? onPeer;

    ///<summary>For tests: the keys of TLS change every so many records.</summary>
    internal long txLimit;
#region the choice
    ///<summary>TLS 1.3 under everything. <paramref name="host"/>: what the server's certificate has to carry. <paramref name="alpn"/>: the protocol to ask for; null - none.</summary>
    public Chain Tls(string host, string? alpn)
    {
        tlsHost = host;
        tlsAlpn = alpn;
        return this;
    }

    ///<summary>TLS 1.3 under everything, the server's end: its certificate chain and key, PEM. One such chain for all its connections.</summary>
    public Chain TlsServer(byte[] certificateChain, byte[] key)
    {
        fixed(byte *c = certificateChain, k = key)
            tlsServer = Crypto.adhoc_tls_server_context(c, certificateChain.Length, k, key.Length);
        if (tlsServer == 0)
            throw new ArgumentException("The certificate chain and the key of TLS: they are not PEM, or do not match.");
        return this;
    }

    ///<summary>Noise NK, the client's end: it knows the server. The server's end: <see cref="NoiseNKServer"/>, or the host's <c>key</c>.</summary>
    public Chain NoiseNK(byte[] serverPublic) => noise(org.unirail.NoiseNK.PATTERN_NK, null, serverPublic);

    public Chain NoiseNKServer(byte[] secret) => noise(org.unirail.NoiseNK.PATTERN_NK, secret, null);

    ///<summary>Noise IK, the client's end: it knows the server, and proves its own key in the first message.</summary>
    public Chain NoiseIK(byte[] serverPublic, byte[] mySecret) => noise(org.unirail.NoiseNK.PATTERN_IK, mySecret, serverPublic);

    public Chain NoiseIKServer(byte[] secret) => noise(org.unirail.NoiseNK.PATTERN_IK, secret, null);

    ///<summary>Noise XX: neither end knows the other beforehand, each proves its key; <see cref="OnPeer"/> decides.</summary>
    public Chain NoiseXX(byte[] mySecret) => noise(org.unirail.NoiseNK.PATTERN_XX, mySecret, null);

    ///<summary>Noise KK: both know each other beforehand.</summary>
    public Chain NoiseKK(byte[] mySecret, byte[] theirPublic) => noise(org.unirail.NoiseNK.PATTERN_KK, mySecret, theirPublic);

    Chain noise(int pattern, byte[]? secret, byte[]? peer)
    {
        this.pattern = pattern;
        staticSecret = secret;
        this.peer = peer;
        return this;
    }

    ///<summary>Hears the key the other side has proved; false - the connection ends. For the patterns that bring it.</summary>
    public Chain OnPeer(Predicate<byte[]> onPeer)
    {
        this.onPeer = onPeer;
        return this;
    }
#endregion

    ///<summary>
    ///A pack on its own, what the session's transport has nothing to do with: signed by its author ( E7 ), sealed under the key of the pair
    ///( E6 ), any of the two, in one call. On the wire: [ id 4 ][ number 8 ], in clear and authenticated, then the text, the signature
    ///of 64 bytes, the tag of 16. What is chosen must be the same on both ends. The same wire as the Java runtime's Chain.Pack.
    ///</summary>
    public sealed class Pack
    {
        public const int HEADER = 12, SIGNATURE = 64, TAG = 16;

        static readonly int aead = Crypto.adhoc_aead_size();

        readonly byte *state = (byte *)NativeMemory.AlignedAlloc((nuint)(128 + aead), 16);
        long ctx, secret, pub;
        long number = (long)ChaCha20.Core.NextSeq();
        int id;

        ~Pack()
        {
            NativeMemory.Clear(state, (nuint)(128 + aead));
            NativeMemory.AlignedFree(state);
        }

        ///<summary>Sealed under the key of this pair, for what it is used: both ends get the same one, and the key stays inside.</summary>
        public Pack Pair(ReadOnlySpan<byte> mySecret, ReadOnlySpan<byte> theirPublic, string purpose)
        {
            if (mySecret.Length < 32 || theirPublic.Length < 32)
                throw new ArgumentException("X25519 keys are 32 bytes");
            var label = Encoding.UTF8.GetBytes(purpose);
            byte *key = stackalloc byte[32], iv = stackalloc byte[12];
            int i;
            fixed(byte *s = mySecret, t = theirPublic, l = label) if (Crypto.adhoc_pair_key(key, iv, &i, s, t, l, label.Length) == 0) throw new ArgumentException("A key of a low order.");
            ctx = (long)state + 128;
            var ok = Crypto.adhoc_aead_init(0, (void *)ctx, key, 32);
            new Span<byte>(key, 32).Clear();
            if (ok == 0)
                throw new InvalidOperationException("aead");
            id = i;
            return this;
        }

        ///<summary>Signed by this key: 64 bytes of secret, see <see cref="Crypto.Ed25519KeyPair"/>.</summary>
        public Pack SignedBy(ReadOnlySpan<byte> signingSecret)
        {
            signingSecret.Slice(0, 64).CopyTo(new Span<byte>(state, 64));
            secret = (long)state;
            return this;
        }

        ///<summary>Must be signed by the owner of this key.</summary>
        public Pack SignerOf(ReadOnlySpan<byte> signingPublic)
        {
            signingPublic.Slice(0, 32).CopyTo(new Span<byte>(state + 64, 32));
            pub = (long)state + 64;
            return this;
        }

        ///<summary>What a pack of <paramref name="len"/> bytes of text takes.</summary>
        public int Size(int len) => HEADER + len + (secret != 0 ? SIGNATURE : 0) + (ctx != 0 ? TAG : 0);

        ///<summary>The text at <c>buf[from + HEADER]</c> of <paramref name="len"/> bytes, the buffer with room for <see cref="Size"/>: sealed in place. The whole length.</summary>
        public int Seal(byte[] buf, int from, int len)
        {
            if (buf.Length - Size(len) < from)
                throw new IndexOutOfRangeException();
            var n = Interlocked.Increment(ref number) - 1;
            System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buf.AsSpan(from), id);
            System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(buf.AsSpan(from + 4), n);
            int r;
            fixed(byte *b = buf)
                r = Crypto.adhoc_pack_seal(ctx, n, secret, (long)b + from, HEADER, len);
            if (r < 0)
                throw new InvalidOperationException("seal");
            return r;
        }

        ///<summary>The pack of <paramref name="total"/> bytes at <c>buf[from]</c>, opened in place. The length of the text at <c>from + HEADER</c>; -1 - forged, or not of its signer.</summary>
        public int Open(byte[] buf, int from, int total)
        {
            if (total < HEADER || buf.Length - total < from)
                return -1;
            var n = System.Buffers.Binary.BinaryPrimitives.ReadInt64LittleEndian(buf.AsSpan(from + 4));
            fixed(byte *b = buf) return Crypto.adhoc_pack_open(ctx, n, pub, (long)b + from, HEADER, total);
        }
    }

    const int TLS_HEADER = 5, TLS_OVERHEAD = TLS_HEADER + 1 + 16, TLS_MAX_DATA = 16384, TLS_MAX_RECORD = TLS_HEADER + 16384 + 256;

    ///<summary>The room a buffer has on top of what was asked of it: the headers of the chain, in its place, so the window stays.</summary>
    internal const int HEADERS = TLS_OVERHEAD + 18 + 14 + 2;

    ///<summary>A whole record of TLS holding a whole record of Noise, and what may have begun behind it.</summary>
    internal const int RECORD_BUFFER = 2 + 16384 + 16 + TLS_MAX_RECORD;

    //Offsets in the memory of the chain: the Chain of adhoc_crypto.c.
    const int TLS = 0, RX_CTX = 8, RX_N = 16, FLAGS = 24, WS_STATE = 28, WS_LEFT = 32, WS_MASK = 36, WS_MASK_POS = 40, NOISE_BYTES = 52, NOISE_COUNT = 56, TLS_FLAGS = 60, OPS = 64, PIECES = 68, OP = 72, OP_SIZE = 32, NOISE_END = 328, PIECE = 332;
    const int F_WS = 1, F_WS_CLIENT = 2, F_NOISE = 4, F_IN_PIECES = 8;
    const int OP_NOISE = 0, OP_MASK = 1, OP_TLS = 2;
    const int WS_OTHER = 2;
    const int OFFSET = 0x1F_FFFF;

    static readonly int size = Crypto.adhoc_chain_size();

    ///<summary>The chain on one connection. Taken from the pool for a connection that has a chain; a connection with none takes nothing.</summary>
    internal sealed class Link
    {
        static readonly ConcurrentBag<Link> pool = new();

        ///<summary>How many have ever been made: what a connection with a chain really costs.</summary>
        internal static long made;

        internal static Link Take(Network.TCP.ExternalConnection connection, Chain? chain)
        {
            if (!pool.TryTake(out var l))
            {
                Interlocked.Increment(ref made);
                l = new Link();
            }
            l.attach(connection, chain);
            return l;
        }

        readonly byte *mem;
        readonly long memAt;

        Link()
        {
            mem = (byte *)NativeMemory.AlignedAlloc((nuint)size, 16);
            memAt = (long)mem;
            NativeMemory.Clear(mem, (nuint)size);
        }

        ~Link() => NativeMemory.AlignedFree(mem);

        Network.TCP.ExternalConnection connection = null!;
        Network.TCP.WebSocket? ws;
        long tls; //the connection of TLS in the native library
        volatile bool handshaking;
        volatile bool needsControl;
        bool closed;

        //What the TLS handshake has to send: gone when it ends. The two directions of a connection run on threads of their own: the handshake is theirs in common, under this.
        byte[]? flight;
        int flightAt, flightEnd;
        bool started;

        int held; //the bytes of a TLS record left at the start of the buffer, with no plaintext before it

        //What waits for the next read lies where it came, in the same buffer, past the plaintext that goes up first; the next read moves it to the front.
        //A record of Noise cut by the end of the plaintext: no call of its own for it. A TLS record not all here.
        int carryAt, carryLen, tailAt, tailLen, carried, restored;

        //A record is authenticated whole, so all of it has to lie somewhere, and it is the peer who says how long it is. A buffer that holds it does.
        //For a longer one, a whole-record buffer of the pool: taken when such a record starts, given back when it is taken up.
        byte[]? big;

        int piece, pieces, piecesEnd, piecesLimit;

        internal long txCalls, rxCalls, txBuffers, rxBuffers, rxBytes, txBytes, rxCarried;

        void attach(Network.TCP.ExternalConnection connection, Chain? chain)
        {
            this.connection = connection;
            ws = connection as Network.TCP.WebSocket;
            closed = false;
            if (chain != null && chain.tlsServer != 0)
                tls = Crypto.adhoc_tls_server(chain.tlsServer);
            else if (chain?.tlsHost != null)
            {
                var alpn = chain.tlsAlpn == null ? null : Encoding.ASCII.GetBytes(chain.tlsAlpn);
                fixed(byte *h = Encoding.UTF8.GetBytes(chain.tlsHost + "\0"), a = alpn)
                    tls = Crypto.adhoc_tls_client(Context(), h, a, alpn?.Length ?? 0, &Verify, null);
            }
            if (tls != 0)
            {
                if (chain!.txLimit != 0)
                    Crypto.adhoc_tls_tx_limit(tls, chain.txLimit);
                handshaking = true;
                flight = ArrayPool<byte>.Shared.Rent(RECORD_BUFFER);
                flightAt = flightEnd = 0;
                started = false;
            }
            if (connection.noise != null)
                connection.noise.link = this;
        }

        internal bool Ready => !handshaking;

        ///<summary>Something waits in the buffer for the next read.</summary>
        internal bool Keeps => carryLen != 0 || tailLen != 0;

        void fit(byte[] dst, int length)
        {
            if (big == null && dst.Length < length)
                big = ArrayPool<byte>.Shared.Rent(RECORD_BUFFER);
        }

        static int tlsLength(byte[] dst, int at, int have) => have < TLS_HEADER ? 0 : TLS_HEADER + (dst[at + 3] << 8 | dst[at + 4]);

        void op(int kind, long address, int len, long a, long b)
        {
            var n = *(int *)(mem + OPS);
            var at = mem + OP + n * OP_SIZE;
            *(int *)at = kind;
            *(int *)(at + 4) = len;
            *(long *)(at + 8) = address;
            *(long *)(at + 16) = a;
            *(long *)(at + 24) = b;
            *(int *)(mem + OPS) = n + 1;
        }
#region what the layers say on the way out : the places are offsets in the buffer until the buffer is held still, for the one call
        internal void Noise(long ctx, long n, int at, int bytes) => op(OP_NOISE, at, bytes, ctx, n);

        internal void Mask(int from, int to, uint key) => op(OP_MASK, from, to - from, key, 0);
#endregion

        int run(byte[] buf)
        {
            txCalls++;
            fixed(byte *b = buf)
            {
                for (int i = 0, n = *(int *)(mem + OPS); i < n; i++)
                    *(long *)(mem + OP + i * OP_SIZE + 8) += (long)b;
                var r = Crypto.adhoc_chain_tx(memAt);
                if (r < 0)
                    throw new IOException("seal");
                return r;
            }
        }

        ///<summary>Fills <paramref name="buf"/>, whatever is to go next: the handshake, a record of the connection's own, the data through the chain.</summary>
        internal bool Transmit(byte[] buf)
        {
            if (closed)
                return false;
            *(int *)(mem + OPS) = 0;
            if (tls == 0)
            {
                connection.txAt = 0;
                connection.txEnd = buf.Length;
                var filled = connection.TransmitOfLayers(buf);
                if (*(int *)(mem + OPS) != 0)
                    run(buf);
                if (filled)
                {
                    txBuffers++;
                    txBytes += connection.Count;
                }
                return filled;
            }
            lock (this)
            {
                if (handshaking && !started)
                    handshake(Array.Empty<byte>(), 0, out _);
                if (flightAt < flightEnd)
                { //in as many buffers as it takes
                    var n = Math.Min(flightEnd - flightAt, buf.Length);
                    System.Buffer.BlockCopy(flight!, flightAt, buf, 0, n);
                    flightAt += n;
                    connection.SetBuffer(0, n);
                    return true;
                }
                if (handshaking)
                    return false;
                if (flight != null)
                {
                    ArrayPool<byte>.Shared.Return(flight);
                    flight = null;
                }
            }
            if (needsControl)
            {
                needsControl = false; //first: what the other direction asks from now on comes after this
                int n;
                fixed(byte *b = buf)
                    n = Crypto.adhoc_tls_control(tls, (long)b);
                if (n < 0)
                    throw new IOException(error());
                if (0 < n)
                {
                    connection.SetBuffer(0, n);
                    return true;
                }
            }
            connection.txAt = TLS_HEADER;
            connection.txEnd = TLS_HEADER + Math.Min(buf.Length - TLS_OVERHEAD, TLS_MAX_DATA);
            if (!connection.TransmitOfLayers(buf))
                return false;
            //The framing left its bytes at Offset, a slack of its own before them: the TLS header goes there.
            var at = connection.Offset;
            var len = connection.Count;
            if (len == 0)
                return false;
            op(OP_TLS, at - TLS_HEADER, len, 0, 0);
            var record = run(buf);
            needsControl = (*(int *)(mem + TLS_FLAGS) & 2) != 0;
            connection.SetBuffer(at - TLS_HEADER, record);
            txBuffers++;
            txBytes += record;
            return true;
        }

        string error() => tls == 0 ? "no connection" : Marshal.PtrToStringUTF8((IntPtr)Crypto.adhoc_tls_error(tls)) ?? "unknown";

        //Under the lock: the two directions share it. Whole records are taken, a partial one stays, and what follows the handshake stays too.
        void handshake(byte[] input, int limit, out int consumed)
        {
            started = true;
            if (flightAt == flightEnd)
                flightAt = flightEnd = 0;
            else if (0 < flightAt)
            {
                System.Buffer.BlockCopy(flight!, flightAt, flight!, 0, flightEnd - flightAt);
                flightEnd -= flightAt;
                flightAt = 0;
            }
            long r;
            fixed(byte *i = input.Length == 0 ? new byte[1] : input, o = flight)
                r = Crypto.adhoc_tls_handshake(tls, (long)i, limit, (long)o + flightEnd, flight!.Length - flightEnd);
            consumed = (int)(r >> 24) & 0xFF_FFFF;
            flightEnd += (int)r & 0xFF_FFFF;
            var status = (int)(r >> 48);
            if (status == 0)
            {
                handshaking = false;
                *(long *)(mem + TLS) = tls;
            }
            else if (status != 1)
            {
                //what it has made may be the alert: it goes, it is the last thing to go
                var alert = connection.Ext;
                if (alert != null && flightAt < flightEnd)
                    try
                    {
                        alert.Send(flight!, flightAt, flightEnd - flightAt, SocketFlags.None);
                    }
                    catch
                    {
                    }
                throw new IOException("handshake: " + error());
            }
        }

        ///<summary>Before a read: what was held back goes in front of it; what is to be read goes to the buffer it fits.</summary>
        internal void Receiving(SocketAsyncEventArgs mate)
        {
            var dst = mate.Buffer!;
            if (big != null && !ReferenceEquals(dst, big))
            { //from now on the reads go to the big one: what waits goes with them
                if (carryLen != 0)
                    System.Buffer.BlockCopy(dst, carryAt, big, 0, carryLen);
                if (tailLen != 0)
                    System.Buffer.BlockCopy(dst, tailAt, big, carryLen, tailLen);
                carryAt = 0;
                tailAt = carryLen;
                if (held != 0)
                    System.Buffer.BlockCopy(dst, 0, big, 0, held);
                ArrayPool<byte>.Shared.Return(dst);
                mate.SetBuffer(big, held, big.Length - held);
                dst = big;
            }
            else if (big != null && !Keeps && held == 0)
            { //it has done its work
                var bigger = big;
                big = null;
                connection.host.buffers(mate);
                ArrayPool<byte>.Shared.Return(bigger);
                dst = mate.Buffer!;
                mate.SetBuffer(0, dst.Length);
            }
            if (!Keeps)
            {
                var noise = connection.noise;
                if (restored == 0 && mate.Offset == 0 && noise != null && (ws == null || ws.chainState() != WS_OTHER))
                { //what the slow way holds of a record goes in front of this read: the chain opens it and all behind it in one call, and the record buffer goes back
                    carried = noise.GiveBack(dst);
                    restored = carried;
                    if (carried != 0)
                        mate.SetBuffer(carried, dst.Length - carried);
                }
                return;
            }
            //To the front, what comes first first: nothing above it is needed any more.
            if (carryLen != 0)
                System.Buffer.BlockCopy(dst, carryAt, dst, 0, carryLen);
            if (tailLen != 0)
                System.Buffer.BlockCopy(dst, tailAt, dst, carryLen, tailLen);
            carried = carryLen;
            restored = carryLen + tailLen;
            carryLen = tailLen = 0;
            mate.SetBuffer(restored, dst.Length - restored);
        }

        ///<summary>The read has brought bytes to [Offset, limit). Hands the data on, or waits for more.</summary>
        internal void Received(SocketAsyncEventArgs mate)
        {
            var dst = mate.Buffer!;
            var limit = mate.Offset + mate.BytesTransferred;
            var at = mate.Offset - restored;
            restored = 0;
            if (held != 0)
            { //what lies at the start of the buffer is the part of a record held from the last read, not plaintext
                at = 0;
                held = 0;
            }
            if (handshaking)
            {
                int consumed;
                lock (this)
                    handshake(dst, limit, out consumed);
                connection.WakeTransmit(); //its answer, or what is to go next
                var left = limit - consumed;
                if (handshaking)
                { //a part of a record, or none: it stays and the next read appends
                    if (0 < consumed && 0 < left)
                        System.Buffer.BlockCopy(dst, consumed, dst, 0, left);
                    held = left;
                    fit(dst, tlsLength(dst, 0, left));
                    mate.SetBuffer(left, dst.Length - left);
                    return;
                }
                connection.SessionStartedOfTransport();
                if (left == 0)
                {
                    mate.SetBuffer(0, dst.Length);
                    return;
                }
                System.Buffer.BlockCopy(dst, consumed, dst, 0, left);
                mate.SetBuffer(0, dst.Length);
                open(mate, dst, 0, left);
                return;
            }
            open(mate, dst, at, limit);
        }

        void open(SocketAsyncEventArgs mate, byte[] dst, int at, int limit)
        {
            //what the layers above know, where the native code reads it
            var flags = 0;
            var state = ws?.chainState() ?? 0;
            if (ws != null)
            {
                flags = ws.client ? F_WS | F_WS_CLIENT : F_WS;
                *(int *)(mem + WS_STATE) = at != 0 || state == WS_OTHER ? WS_OTHER : state;
                *(int *)(mem + WS_LEFT) = ws.chainLeft;
                *(uint *)(mem + WS_MASK) = ws.chainMask;
                *(int *)(mem + WS_MASK_POS) = ws.chainMaskPos;
            }
            var noise = connection.noise;
            //Behind the frames only when no decoded payload waits; with no frames, right from the start.
            var inPieces = at == 0 && (ws == null || state != WS_OTHER);
            var aligned = noise != null && noise.Aligned && inPieces;
            if (aligned)
            {
                *(long *)(mem + RX_CTX) = noise!.RxContext;
                *(long *)(mem + RX_N) = noise.RxNumber;
                flags |= F_NOISE;
            }
            //In one piece for what reads the buffer from its very start: what the framing left, the upgrade, the Java decoder.
            if (inPieces)
                flags |= F_IN_PIECES;
            *(int *)(mem + FLAGS) = flags;

            rxCalls++;
            rxBuffers++;
            rxBytes += limit - at;
            var from = carried;
            rxCarried += from;
            carried = 0;
            long r;
            fixed(byte *b = dst)
                r = Crypto.adhoc_chain_rx(memAt, b, at + from, limit);
            if (r < 0)
            {
                needsControl = true;
                throw new IOException(error());
            }
            var consumed = (int)(r >> 42);
            int rawEnd = (int)(r >> 21) & OFFSET, wsEnd = (int)r & OFFSET;
            if (tls != 0)
            {
                var f = *(int *)(mem + TLS_FLAGS);
                needsControl = (f & 2) != 0;
                if (needsControl)
                    connection.WakeTransmit();
            }
            //Only with nothing behind it for the Java decoder: that would have to come first.
            var noiseEnd = aligned ? *(int *)(mem + NOISE_END) : -1;
            var whole = noiseEnd < 0 ? wsEnd : noiseEnd;
            if (aligned && wsEnd == rawEnd && whole < wsEnd)
            {
                carryAt = whole;
                carryLen = wsEnd - whole;
                //Under TLS a whole TLS record may come behind it; without, what is left of its own.
                fit(dst, tls != 0 ? RECORD_BUFFER : 2 + 16 + (carryLen < 2 ? 0 : dst[whole] | dst[whole + 1] << 8));
                wsEnd = rawEnd = whole;
            }
            pieces = *(int *)(mem + PIECES);
            piece = 0;
            piecesEnd = wsEnd;
            piecesLimit = rawEnd;
            var firstEnd = pieces < 2 ? wsEnd : *(int *)(mem + PIECE + 4);
            if (ws != null && inPieces)
                ws.chainDecoded(*(int *)(mem + WS_STATE), *(int *)(mem + WS_LEFT), *(uint *)(mem + WS_MASK), *(int *)(mem + WS_MASK_POS));
            var rest = limit - consumed;
            fit(dst, tlsLength(dst, consumed, rest));
            var start = *(int *)(mem + PIECE);
            var nothing = pieces < 2 ? rawEnd - start : 1; //the first run, and whether there is anything at all
            if (nothing == 0 && carryLen == 0)
            { //no plaintext at all: what is left stays in the buffer, the next read appends
                if (0 < consumed && 0 < rest)
                    System.Buffer.BlockCopy(dst, consumed, dst, 0, rest);
                held = rest;
                mate.SetBuffer(rest, dst.Length - rest);
                return;
            }
            if (0 < rest)
            { //plaintext goes up first: the piece of the next record stays behind it, untouched, and the buffer stays
                tailAt = consumed;
                tailLen = rest;
            }
            if (aligned)
                noise!.OpenedAhead(*(int *)(mem + NOISE_BYTES), *(int *)(mem + NOISE_COUNT));
            mate.SetBuffer(0, dst.Length);
            if (nothing == 0)
                return; //all of it waits for the next read
            if (inPieces)
                connection.ReceivedOfChain(start, pieces < 2 ? rawEnd : firstEnd, firstEnd);
            else
                connection.ReceivedAsItIs(dst, rawEnd);
        }

        ///<summary>The run in hand is taken up: the next one. false - no more.</summary>
        internal bool NextPiece(out int at, out int limit, out int end)
        {
            while (++piece < pieces)
            {
                var last = piece + 1 == pieces;
                at = *(int *)(mem + PIECE + piece * 8);
                end = last ? piecesEnd : *(int *)(mem + PIECE + piece * 8 + 4);
                limit = last ? piecesLimit : end;
                if (at != limit)
                    return true;
            }
            at = limit = end = 0;
            return false;
        }

        ///<summary>The FIN is about to go: close_notify, when the socket takes it at once.</summary>
        internal void Closing(Socket? socket)
        {
            if (tls == 0 || handshaking || closed || socket == null)
                return;
            try
            {
                Span<byte> alert = stackalloc byte[64];
                int n;
                Crypto.adhoc_tls_close_notify(tls);
                fixed(byte *b = alert)
                    n = Crypto.adhoc_tls_control(tls, (long)b);
                if (0 < n)
                    socket.Send(alert.Slice(0, n));
            }
            catch
            {
            }
        }

        ///<summary>The connection is gone, and nothing runs on it: all of this goes back.</summary>
        internal void Close(SocketAsyncEventArgs mate)
        {
            if (flight != null)
                ArrayPool<byte>.Shared.Return(flight);
            flight = null;
            if (big != null)
            {
                if (!ReferenceEquals(mate.Buffer, big))
                    ArrayPool<byte>.Shared.Return(big); //else it goes back as the buffer of the connection
                big = null;
            }
            closed = true;
            if (tls != 0)
                Crypto.adhoc_tls_free(tls);
            tls = 0;
            //clean, for the next connection: it may have no TLS
            NativeMemory.Clear(mem, (nuint)size);
            handshaking = needsControl = started = false;
            flightAt = flightEnd = held = carryAt = carryLen = tailAt = tailLen = carried = restored = piece = pieces = piecesEnd = piecesLimit = 0;
            txCalls = rxCalls = txBuffers = rxBuffers = rxBytes = txBytes = rxCarried = 0;
            connection = null!;
            ws = null;
            pool.Add(this);
        }
    }
}
