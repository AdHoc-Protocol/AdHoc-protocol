using org.unirail.Meta; //  importing AdHoc protocol attributes. Required!

namespace com.my.company // You company namespace. Required!
{
    public interface MyProject { // MyProject  ddeclare AdHoc protocol description project 

        ///<see cref = 'InTS'/>
        struct Server : Host // generate code for this host in TypeScript
        {
            public interface ToClient
            {
                class PacketToClient { } // empty packet to send to client
            }
        }

        ///<see cref = 'InJAVA'/> 
        struct Client : Host // generate code for this host in JAVA
        {
            public interface ToServer
            {
                class PacketToServer { } // empty packet to send to server
            }
        }

        interface Channel : Communication_Channel_Of<Client.ToServer, Server.ToClient> { } //communication channel
    }
}