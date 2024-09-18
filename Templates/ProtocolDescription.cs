using org.unirail.Meta; // Importing attributes required for AdHoc protocol generation

namespace com.my.company // The namespace for your company's project. Required!
{
    public interface MyProject // Declares an AdHoc protocol description project
    {
        class CommonPacket{ } // Represents a common empty packet used across different hosts

        /// <see cref="InTS"/>-   // Generates an abstract version of the corresponding TypeScript code
        /// <see cref="InCS"/>    // Generates the concrete implementation in C#
        /// <see cref="InJAVA"/>  // Generates the concrete implementation in Java
        struct Server : Host // Defines the server-side host and generates platform-specific code
        {
            class PacketToClient{ } // Represents an empty packet to be sent from the server to the client
        }

        /// <see cref="InTS"/>    // Generates the concrete implementation in TypeScript
        /// <see cref="InCS"/>-   // Generates an abstract version of the corresponding C# code
        /// <see cref="InJAVA"/>  // Generates the concrete implementation in Java
        struct Client : Host // Defines the client-side host and generates platform-specific code
        {
            class PacketToServer{ } // Represents an empty packet to be sent from the client to the server
        }

        // Defines a communication channel for exchanging data between the client and server
        interface Channel : ChannelFor<Client, Server>{ }
    }
}