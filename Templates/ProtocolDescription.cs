// A small chat, ready to generate: run `AdHocAgent MyProtocolDescription.cs`, then change it into your own protocol.
// To see it drawn - hosts, packs, the states of every conversation - run `AdHocAgent MyProtocolDescription.cs?` and open http://localhost:4321/
// A Client signs in; the Server welcomes it with the recent messages, or refuses; then they talk until the Client signs out.
// Beside the chat, on the same connection, the Client uploads files, several at once, and asks for older messages.

using org.unirail.Meta; // the AdHoc vocabulary: Host, Connects, the attributes

namespace com.my.company // required
{
    public interface MyProject // the project: one description of what every host says to every other
    {
        enum Mood : byte { Calm, Happy, Angry } // a field of this type takes 2 bits on the wire, not a byte

        // A Value Pack: its fields fit in 64 bits together, so the generated code keeps it as one integer - no object, no allocation.
        // Nothing marks it: the generator sees it by its fields.
        class Style
        {
            Mood                 mood;   // 2 bits
            bool                 bold;   // 1 bit
            bool                 italic; // 1 bit
            [MinMax(0, 15)] byte color;  // 4 bits: the declared range, not the type, decides the width
        }

        // Declared in the project, not in a host: both sides send it, and each gets the same code.
        class Message
        {
            public const int MAX_TEXT = 1000; // constants reach the generated code too

            string?                author; // ? - optional: the Server fills it in, the Client leaves it out
            [D(+MAX_TEXT)] string  text;   // at most MAX_TEXT characters
            Style                  style;  // a Value Pack as a field: 8 bits, read and written like any other field
            long                   time;
        }

        // InRS, InGO, InCPP, InC, InSwift, InDart, InPy are added the same way, and they all talk to each other.
        /// <see cref="InCS"/>   C#: packs are classes that hold their values
        /// <see cref="InJAVA"/> Java: the same
        /// <see cref="InTS"/>-  TypeScript: the - asks for abstract packs, interfaces that your own types implement
        struct Server : Host
        {
            public class Welcome
            {
                [D(100)] Message[,,] recent; // up to 100 messages: packs nest in packs
            }

            public class Refused
            {
                string reason;
            }

            public class Online // a Value Pack sent on its own: the pack id and 4 bytes, nothing more
            {
                [MinMax(0, 10_000)] ushort people; // the declared range decides how few bits the value takes: 14 here
                [MinMax(0, 10_000)] ushort typing;
            }

            public class Accepted { } // the upload may go on

            public class History
            {
                [D(100)] Message[,,] older;
            }
        }

        /// <see cref="InTS"/>   TypeScript: packs are classes
        /// <see cref="InCS"/>-  C#: abstract
        /// <see cref="InJAVA"/> Java: packs are classes
        struct Client : Host
        {
            public class SignIn
            {
                [D(+32)] string name;
                string?         password;
            }

            public class SignOut{ } // an empty pack: its arrival says it all, and it costs next to nothing

            public class FileOffer
            {
                [D(+255)]                     string name;
                [MinMax(1, FileData.MAX_SIZE)] int    size;
            }

            public class FileData
            {
                public const int MAX_SIZE = 1_000_000;

                [D(MAX_SIZE)] Binary[,] bytes; // raw bytes, at most MAX_SIZE of them
            }

            public class HistoryRequest
            {
                long before; // the messages older than this time
            }
        }

        // The rules of the conversation: what each host may send in each state.
        // In a branch, l is the left host (Client), r the right host (Server).
        // A capital letter moves the conversation to the state named first; a small one stays in the state.
        // The side with the capital letter leads the state: only it moves the conversation on.
        interface Chat : Connects<Client, Server>
        {
            // The states written right here belong to Actor0, the connection's own conversation; the first one is where it starts.
            [L____________<Answer, Client.SignIn>] // the Client speaks first
            struct Start { }

            [ReceiveTimeout(10)]                   // the Client waits at most 10 seconds for the answer, then the connection closes
            [____________R<Talk, Server.Welcome>]  // accepted
            [____________R<Close, Server.Refused>] // refused, with the reason; the connection closes
            struct Answer { }

            [l____________<Message>]                  // any number of messages, in both directions
            [____________r<(Message, Server.Online)>]
            [L____________<Close, Client.SignOut>]
            struct Talk { }

            // A named actor: a conversation of its own, with its own states, beside Actor0.
            // Named actors and RPC methods do not wait for the sign-in: roles keep them closed until it, see the manual.
            interface Upload : Actor
            {
                int Actor.MaxActiveInstances => 4; // up to 4 uploads at once on one connection

                [L____________<Offered, Client.FileOffer>]    // the Client offers a file
                struct Start { }

                [____________R<Sending, Server.Accepted>]     // now the Server leads: it takes the file...
                [____________R<End, Server.Refused>]          // ...or refuses it; End ends this upload only, the chat goes on
                struct Offered { }

                [L____________<End, Client.FileData>]         // the Client leads again: the bytes, and the upload is over
                struct Sending { }
            }

            // An RPC method: one request, one reply. It is an actor too, generated as Call -> Reply -> End.
            (L____________, Server.History) history(Client.HistoryRequest request); // L: the Client asks, the Server replies
        }
    }
}