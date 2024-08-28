using org.unirail.Meta;

// Importing AdHoc protocol attributes. Required!
namespace com.my.company // Your company namespace. Required!
{
    public interface MyProject // Declare AdHoc protocol description project
    {
        /// <see cref="InTS"/>
        /// <see cref="InCS"/>
        /// <see cref="InJAVA"/>
        struct Server : Host // Generate code for this host in TypeScript
        {
            class PacketToClient{ } // Empty packet to send to client
        }

        /// <see cref="InTS"/>
        /// <see cref="InCS"/>
        /// <see cref="InJAVA"/>
        struct Client : Host // Generate code for this host in JAVA
        {
            class PacketToServer{ } // Empty packet to send to server
        }

        interface Channel : ChannelFor<Client, Server>{ } // Communication Channel
    }
}