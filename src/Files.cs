using System;
using System.Collections.Generic;
using System.IO;

using org.unirail.Agent;

namespace org.unirail;

/// <summary>
/// The one <see cref="AdHocProtocol.FileEntry.List"/> implementation, serving both directions of the single
/// connection the Agent ever has open — hence the <see cref="ONE"/> singleton.
/// <para>
/// The two directions carry different shapes, so they keep disjoint state and are armed separately:
/// </para>
/// <list type="bullet">
/// <item><b><see cref="Send"/></b> — <c>Agent.Project.source</c>: a FLAT list. The description files are
/// scattered absolute paths with no common root (<c>provided_path</c> plus whatever the referenced
/// <c>.csproj</c>s pulled in), so each is published under its bare file name, with a counter inserted before
/// the extension to break collisions: <c>Foo.cs</c>, <c>Foo1.cs</c>, <c>Foo2.cs</c>.</item>
/// <item><b><see cref="Receive"/></b> — <c>Server.Result.result</c>: a NESTED tree of generated code
/// (<c>InCS/…</c>, <c>InJAVA/…</c>), which <see cref="AdHocAgent.Deployment.deploy"/> walks with
/// <c>Path.GetRelativePath</c>. Entries keep their relative path and land under an armed destination root.</item>
/// </list>
/// </summary>
public class Files : AdHocProtocol.FileEntry.List{

    public static readonly Files ONE = new();

#region send
    readonly List<(string name, string path)> send = [];

    /// <summary>
    /// Arms the SEND direction over an explicit set of files. They need not share a root — each entry is
    /// published under its bare file name, de-duplicated with a trailing counter.
    /// </summary>
    public Files Send(IEnumerable<string> files)
    {
        send.Clear();
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach( var path in files )
        {
            var name = Path.GetFileName(path);

            if( !taken.Add(name) ) // name already claimed — insert a counter BEFORE the extension so the
            {                      // Server still sees a valid `.cs`, and keep climbing past literal `Foo1.cs`
                var stem = Path.GetFileNameWithoutExtension(name);
                var ext  = Path.GetExtension(name);
                var n    = 0;
                do name = stem + ++n + ext;
                while( !taken.Add(name) );
            }

            send.Add((name, path));
        }

        return this;
    }

    public object? _files() => send.Count == 0 ?
                                   null :
                                   this;

    public int _files_len => send.Count;

    // Indexed, not a forward-only walk: the entry is re-armed per item, and an index lookup cannot drift
    // out of step with the count the way a second enumeration of a live directory could.
    public AdHocProtocol.FileEntry _files(Base_.Transmitter ctx, Base_.Transmitter.Slot __slot, int item)
    {
        (file._path, file.src) = send[item];
        return file;
    }
#endregion

#region receive
    string root = "";

    /// <summary>Arms the RECEIVE direction: every incoming entry lands at <paramref name="root"/>/<c>path</c>.</summary>
    public Files Receive(string root)
    {
        this.root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        return this;
    }

    public void _files_new(Base_.Receiver ctx, Base_.Receiver.Slot __slot, int items_count) { }

    // Nothing to collect: each entry has already streamed itself to disk by the time this is called.
    public void _files(Base_.Receiver ctx, Base_.Receiver.Slot __slot, int item, AdHocProtocol.FileEntry src) { }
#endregion

    /// <summary>The single re-armed entry carrier. Entries serialize and deserialize strictly one at a time,
    /// in index order, each to completion — so one instance is enough in both directions.</summary>
    public AdHocProtocol.FileEntry entry => file;

    readonly Entry file;

    public Files() => file = new Entry(this);

    class Entry(Files owner) : AdHocProtocol.FileEntry{
        /// Send: the published (bare, de-duplicated) name. Receive: the '/'-separated path under the root.
        public string? _path { get; set; }

        /// Send only: absolute path of the file the bytes are read from.
        public string src = "";

        // A fresh handle each time — the framework Disposes what these return once the payload is done.
        public Stream _bytes_(AdHoc.Connection.Transmitter scope) => File.OpenRead(src);

        public bool __bytes_hasValue(AdHoc.Connection.Transmitter scope) => File.Exists(src);

        public Stream _bytes_(AdHoc.Connection.Receiver scope)
        {
            var dst = owner.resolve(_path!);
            Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
            return new FileStream(dst, FileMode.Create, FileAccess.Write, FileShare.None);
        }
    }

    /// <summary>
    /// Resolves a received entry path against the armed root. Confining it is now OUR job — the extraction
    /// used to run inside 7-Zip — so a path that escapes the root (rooted, or climbing out through `..`)
    /// is refused rather than written.
    /// </summary>
    string resolve(string path)
    {
        if( root.Length == 0 ) AdHocAgent.exit("Destination folder for received files is not armed.", 2);

        var dst = Path.GetFullPath(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar)));

        if( dst.Length <= root.Length || !dst.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) )
            AdHocAgent.exit($"Received file path `{path}` resolves outside the destination folder {root}.", 2);

        return dst;
    }
}
