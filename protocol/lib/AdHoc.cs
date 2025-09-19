//Copyright 2025 Chikirev Sirguy, Unirail Group
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
//For inquiries, please contact: al8v5C6HU4UtqE9@gmail.com
//GitHub Repository: https://github.com/AdHoc-Protocol

using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using org.unirail.collections;

namespace org.unirail
{
    public class AdHoc
    {
        ///<summary>
        ///Defines an interface for a source of bytes, typically used for reading data sequentially.
        ///</summary>
        public interface BytesSrc
        {
            ///<summary>
            ///Reads a sequence of bytes from the current source and advances the position within the source by the number of bytes read.
            ///</summary>
            ///<param name="dst">The byte array to which data is read.</param>
            ///<param name="dst_byte">The zero-based byte offset in <paramref name="dst"/> at which to begin storing the data read from the current source.</param>
            ///<param name="dst_bytes">The maximum number of bytes to be read from the current source.</param>
            ///<returns>
            ///A positive integer indicating the total number of bytes read into the buffer <paramref name="dst"/>.
            ///Returns 0 if <paramref name="dst_bytes"/> is 0, or if there is currently no data available but the source is still open and might provide data later (e.g., not enough space in <paramref name="dst"/> to fit a minimal unit of data).
            ///Returns -1 if no more data is available from the source (end of stream).
            ///</returns>
            int Read(byte[] dst, int dst_byte, int dst_bytes);

            ///<summary>
            ///Subscribes an action to be invoked when new bytes become available from this source.
            ///This is typically used to signal a consumer that it can attempt to read data again.
            ///</summary>
            ///<param name="subscriber">The action to invoke. The `BytesSrc` instance itself is passed as an argument to the action, allowing the subscriber to identify the source.</param>
            ///<returns>The previously subscribed action, or null if no action was previously subscribed.</returns>
            Action<BytesSrc>? subscribeOnNewBytesToTransmitArrive(Action<BytesSrc>? subscriber);

            ///<summary>
            ///Gets a value indicating whether the byte source is currently open and available for reading.
            ///</summary>
            ///<returns>True if the source is open; otherwise, false.</returns>
            bool isOpen();

            ///<summary>
            ///Closes the byte source and releases any associated resources.
            ///</summary>
            void Close();
        }

        ///<summary>
        ///Defines an interface for a destination of bytes, typically used for writing data sequentially.
        ///</summary>
        public interface BytesDst
        {
            ///<summary>
            ///Writes a sequence of bytes to the current destination and advances the current position within this destination by the number of bytes written.
            ///ATTENTION! The data in the provided buffer <paramref name="src"/> may be modified after this call due to buffer reuse by the caller.
            ///</summary>
            ///<param name="src">The byte array containing the data to write. </param>
            ///<param name="src_byte">The zero-based byte offset in <paramref name="src"/> from which to begin copying bytes to the current destination.</param>
            ///<param name="src_bytes">The number of bytes to write to the current destination.</param>
            ///<returns>The total number of bytes successfully written to the destination. This can be less than <paramref name="src_bytes"/> if the destination cannot accept all bytes (e.g., buffer full).</returns>
            int Write(byte[] src, int src_byte, int src_bytes);

            ///<summary>
            ///Checks if the byte destination is open.
            ///</summary>
            ///<returns>True if open, false otherwise.</returns>
            bool isOpen();

            ///<summary>
            ///Closes the byte destination.
            ///</summary>
            void Close();
        }

        public interface Channel
        {
            ///<summary>
            ///Represents a single, stateful step or phase in a data processing pipeline for a communication channel.
            ///<para>
            ///Each stage can inspect, modify, or react to data as it is transmitted or received. Implementations
            ///define the logic for various events in the channel's lifecycle, such as activation, transmission,
            ///reception, and timeouts.
            ///</para>
            ///</summary>
            ///<typeparam name="CTX">The type of the context object, holding stateful data for the pipeline instance.</typeparam>
            ///<typeparam name="SND">The type of the packet headers used on sending.</typeparam>
            ///<typeparam name="RCV">The type of the packet headers used on receiving.</typeparam>
            public interface Stage<CTX, SND, RCV>
                where SND : class
                where RCV : class
            {
                ///<summary>
                ///A lifecycle callback invoked when the stage becomes active in the pipeline.
                ///This is the ideal place for initialization, resource allocation, or setting up initial state.
                ///</summary>
                ///<param name="context">The context object for this pipeline instance.</param>
                ///<param name="prevStage">The preceding stage in the pipeline, or <c>null</c> if this is the first stage.</param>
                ///<param name="sendHeaders">The packet headers that initiated this activation, if driven by a transmission. Can be null.</param>
                ///<param name="sendPack">The outgoing packet. Can be null if the pipeline is not initiated by a transmission.</param>
                ///<param name="receiveHeaders">The packet headers that initiated this activation, if driven by a receivePacks. Can be null.</param>
                ///<param name="receivePack">The incoming packet. Can be null if the pipeline does not handle reception.</param>
                void OnActivate(CTX context, Stage<CTX, SND, RCV> prevStage, SND? sendHeaders = null, Channel.Transmitter.BytesSrc? sendPack = null, RCV? receiveHeaders = null, Channel.Receiver.BytesDst? receivePack = null);

                ///<summary>
                ///Handles a failure event within the pipeline. This is called when an error, timeout,
                ///or connection drop occurs, allowing the stage to perform cleanup.
                ///</summary>
                ///<param name="context">The shared context object for this pipeline instance.</param>
                ///<param name="reason">The type of failure that occurred.</param>
                ///<param name="description">A human-readable description of the failure.</param>
                ///<param name="sendHeaders">The headers of the packet being sent at the time of failure, if any.</param>
                ///<param name="sendPack">The packet being sent at the time of failure, if any.</param>
                ///<param name="receiveHeaders">The headers of the packet being received at the time of failure, if any.</param>
                ///<param name="receivePack">The packet being received at the time of failure, if any.</param>
                void OnFailure(CTX context, FailureReason reason, string? description = null, SND? sendHeaders = null, Channel.Transmitter.BytesSrc? sendPack = null, RCV? receiveHeaders = null, Channel.Receiver.BytesDst? receivePack = null);

                ///<summary>
                ///Enumerates the reasons for a pipeline failure or connection termination.
                ///</summary>
                public enum FailureReason
                {
                    ///<summary>
                    ///The connection was terminated by the local application or pipeline logic.
                    ///This is typically an intentional or controlled shutdown.
                    ///</summary>
                    LocalDisconnect,

                    ///<summary>
                    ///The connection was terminated by the remote peer.
                    ///</summary>
                    RemoteDisconnect,

                    ///<summary>
                    ///An operation did not complete within its expected time frame.
                    ///</summary>
                    Timeout,

                    ///<summary>
                    ///The data received from the remote peer violates the expected communication protocol.
                    ///Examples include a malformed packet or an unexpected message type.
                    ///</summary>
                    ProtocolError,

                    ///<summary>
                    ///An unexpected or unhandled error occurred within the pipeline's logic,
                    ///such as a critical exception or serialization failure.
                    ///</summary>
                    InternalError
                }

                ///<summary>
                ///A hook invoked immediately before the serialization process begins for an outgoing packet.
                ///This method provides the final opportunity to inspect, modify, or validate the packet and its headers
                ///before they are converted into a byte stream for transmission.
                ///</summary>
                ///<param name="context">The pipeline context.</param>
                ///<param name="headers">The headers for the outgoing packet.</param>
                ///<param name="pack">The packet to be serialized and sent.</param>
                ///<returns>An error message <c>string</c> to abort the sending process, or <c>null</c> to allow it to proceed.</returns>
                string? OnSerializing(CTX context, SND? headers, Channel.Transmitter.BytesSrc pack);

                ///<summary>
                ///A callback invoked after a packet has been fully serialized into the outgoing byte stream.
                ///<para>
                ///This event confirms the completion of the C# object-to-byte serialization step. Due to the streaming
                ///nature of network I/O, parts or even all of the packet's bytes may have already been sent to the peer
                ///by the time this callback is invoked.
                ///</para>
                ///</summary>
                ///<param name="context">The pipeline context.</param>
                ///<param name="headers">The headers of the packet that was just serialized.</param>
                ///<param name="pack">The packet that was just serialized.</param>
                void OnSerialized(CTX context, SND? headers, Channel.Transmitter.BytesSrc pack);

                ///<summary>
                ///Processes an incoming packet header before its body is received.
                ///This allows the stage to inspect the header and decide whether to accept or reject the packet.
                ///</summary>
                ///<param name="context">The pipeline context.</param>
                ///<param name="headers">The headers of the incoming packet.</param>
                ///<param name="pack">The incoming packet, which is initially empty.</param>
                ///<returns>An error message <c>string</c> to reject the packet, or <c>null</c> to accept it and proceed with receiving the body.</returns>
                string? OnReceiving(CTX context, RCV? headers, Channel.Receiver.BytesDst pack);

                ///<summary>
                ///Handles a fully received packet, including its header and body.
                ///This is the final step in the reception process where the complete data is available for processing.
                ///</summary>
                ///<param name="context">The pipeline context.</param>
                ///<param name="headers">The headers of the fully received packet.</param>
                ///<param name="pack">The received packet.</param>
                void OnReceived(CTX context, RCV? headers, Channel.Receiver.BytesDst pack);

                ///<summary>
                ///Provides a human-readable name for the stage, primarily for logging and debugging purposes.
                ///</summary>
                ///<returns>The name of the stage, defaulting to the simple class name.</returns>
                string name() => GetType().Name;
            }

            ///<summary>
            ///Defines a contract for an external communication channel (e.g., a network socket, serial port)
            ///that acts as an adapter between the external I/O and the internal byte stream handling system.
            ///</summary>
            ///<remarks>
            ///This interface orchestrates bidirectional communication by bridging an external resource
            ///with an internal data producer (<see cref="BytesSrc"/>) and consumer (<see cref="BytesDst"/>).
            ///
            ///<h3>Data Flow:</h3>
            ///<list type="bullet">
            ///</list>
            ///
            ///<h3>Required Setup Lifecycle:</h3>
            ///An instance of <see cref="External"/> must be configured in a specific order:
            ///</remarks>
            public interface External
            {
                ///<summary>
                ///Gets or sets the receive timeout in milliseconds.
                ///A negative value in the getter indicates that a graceful close is in progress.
                ///Setting a negative value initiates a graceful close and sets the timeout to its absolute value.
                ///</summary>
                int ReceiveTimeout { get; set; }

                ///<summary>
                ///Gets or sets the send timeout in milliseconds.
                ///A negative value in the getter indicates that a graceful close is in progress.
                ///Setting a negative value initiates a graceful close and sets the timeout to its absolute value.
                ///</summary>
                int TransmitTimeout { get; set; }

                ///<summary>
                ///Gracefully closes the communication channel and disposes of all associated resources (e.g., sockets, file handles).
                ///The object is considered unusable after this method is called. This is a terminal operation.
                ///</summary>
                void CloseAndDispose();

                ///<summary>
                ///Gracefully closes the communication channel.
                ///This may involve flushing send buffers and completing pending operations.
                ///Unlike <see cref="CloseAndDispose"/>, this may not release all underlying system resources,
                ///potentially allowing the channel to be reconfigured and reused.
                ///</summary>
                void Close();

                ///Aborts the connection and cancels any pending IO operations.
                void Abort();

                Internal Internal { get; set; }
            }

            public interface Internal
            {
                ///<summary>
                ///Gets or sets the internal destination for data received from the external source.
                ///This property must be set before communication begins.
                ///</summary>
                BytesDst? BytesDst { get; }

                ///<summary>
                ///Gets or sets the internal source of data to be transmitted to the external destination.
                ///Setting this property is the final step in configuration and triggers the channel to become active.
                ///</summary>
                ///<remarks>
                ///The <see cref="External"/> implementation is responsible for efficiently pulling data from this source.
                ///To do so, it <b>must</b> subscribe to notifications using <see cref="BytesSrc.subscribeOnNewBytesToTransmitArrive"/>.
                ///When notified, it should call the source's <see cref="BytesSrc.Read"/> method to get the available data and transmit it.
                ///</remarks>
                BytesSrc? BytesSrc { get; }

                ///<summary>
                ///Gets or sets a callback action to be invoked when significant channel events occur,
                ///such as connection, disconnection, or errors.
                ///</summary>
                ///<param name="IExternalChannel">The channel instance raising the event.</param>
                ///<param name="int">An event code identifying the type of event. Implementations
                ///should provide a public set of constants for these codes.</param>
                void OnExternalEvent(External channel, int Event);

                class Impl : Internal
                {
                    public Impl(Action<External, int> onEvent, BytesDst? bytesDst, BytesSrc? bytesSrc)
                    {
                        OnEvent = onEvent;
                        BytesDst = bytesDst;
                        BytesSrc = bytesSrc;
                    }

                    public BytesDst? BytesDst { get; set; }
                    public BytesSrc? BytesSrc { get; set; }
                    public void OnExternalEvent(External channel, int Event) => OnEvent?.Invoke(channel, Event);
                    public Action<External, int> OnEvent;
                }
            }

            ///<summary>
            ///Abstract base class for data receivers. It manages the state for deserializing
            ///structured data from a byte stream. It extends `Base.Receiver` and implements `BytesDst`.
            ///</summary>
            public abstract class Receiver : Base.Receiver, BytesDst
            {
                ///<summary>
                ///Internal interface used by `Receiver` to interact with message-specific deserialization logic.
                ///</summary>
                public interface BytesDst
                {
                    ///<summary>
                    ///Internal method called by the `Receiver` to deserialize data for a specific message type.
                    ///Implementers should read from the `Receiver`'s buffer and update its state.
                    ///</summary>
                    ///<param name="src">The `Receiver` instance providing the byte stream and deserialization context.</param>
                    ///<returns>True if the message part was successfully processed from available bytes; false if more bytes are needed or processing is otherwise incomplete.</returns>
                    bool __put_bytes(Receiver src);

                    ///<summary>
                    ///Gets the unique identifier for this message type or data structure.
                    ///</summary>
                    int __id { get; }
                }

                ///<summary>
                ///Defines an interface for handling events generated by a `Receiver`.
                ///</summary>
                public interface EventsHandler
                {
                    ///<summary>
                    ///Called when enough bytes have been received to identify the incoming packet type,
                    ///but before the entire packet has been deserialized.
                    ///</summary>
                    ///<param name="src">The `Receiver` instance that identified the packet.</param>
                    ///<param name="dst">The `BytesDst` instance corresponding to the identified packet type.</param>
                    void OnReceiving(Receiver src, BytesDst dst) { }

                    ///<summary>
                    ///Called when a complete packet has been successfully received and deserialized.
                    ///The packet is now ready for application-level processing.
                    ///</summary>
                    ///<param name="src">The `Receiver` instance that received the packet.</param>
                    ///<param name="dst">The `BytesDst` instance representing the fully deserialized packet.</param>
                    void OnReceived(Receiver src, BytesDst dst) { }
                }

                ///<summary>
                ///The current event handler for this receiver.
                ///</summary>
                public EventsHandler handler;

                ///<summary>
                ///Atomically exchanges the current event handler with a new one.
                ///</summary>
                ///<param name="handler">The new event handler to set.</param>
                ///<returns>The event handler that was previously set.</returns>
                public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

                private readonly byte id_bytes;

                ///<summary>
                ///Initializes a new instance of the <see cref="Receiver"/> class.
                ///</summary>
                ///<param name="handler">The event handler for receiver events.</param>
                ///<param name="id_bytes">The number of bytes used at the beginning of a packet to identify its type (packet ID).</param>
                public Receiver(EventsHandler handler, int id_bytes)
                {
                    this.handler = handler;
                    bytes_left = bytes_max = this.id_bytes = (byte)id_bytes;
                    slot_ref = new(new Slot(this, null));
                }

                ///<summary>
                ///The default error handler for `Receiver` related errors.
                ///</summary>
                public static OnErrorHandler error_handler = OnErrorHandler.DEFAULT;

                ///<summary>
                ///Enumerates possible error types that can occur during reception.
                ///</summary>
                public enum OnError
                {
                    ///<summary>Indicates a sequence of 0xFF 0xFF was detected, which is invalid outside of specific framing contexts or suggests a synchronization issue.</summary>
                    FFFF_ERROR = 0,

                    ///<summary>Indicates a CRC checksum mismatch, suggesting data corruption.</summary>
                    CRC_ERROR = 1,

                    ///<summary>Indicates that the received byte sequence does not conform to the expected framing or encoding rules, suggesting data corruption or desynchronization.</summary>
                    BYTES_DISTORTION = 3,

                    ///<summary>Indicates a buffer overflow, typically when a declared length exceeds buffer capacity or a predefined maximum.</summary>
                    OVERFLOW = 4,

                    ///<summary>Indicates that an invalid or unrecognized packet ID was received.</summary>
                    INVALID_ID = 5,

                    ///Error code: An receiving packet is rejected by dataflow.
                    REJECTED = 6,

                    ///Error code indicating a timeout during packet transmission or reception.
                    TIMEOUT = 7,

                    ///Generic error code for unspecified or unexpected errors during packet processing.
                    ERROR = 8
                }

                ///<summary>
                ///Defines an interface for handling errors that occur in a `Receiver`.
                ///</summary>
                public interface OnErrorHandler
                {
                    ///<summary>
                    ///A default `OnErrorHandler` implementation that logs errors to the console.
                    ///</summary>
                    static OnErrorHandler DEFAULT = new ToConsole();

                    ///<summary>
                    ///A concrete `OnErrorHandler` that writes error information to the console.
                    ///</summary>
                    class ToConsole : OnErrorHandler
                    {
                        ///<summary>
                        ///Handles a `Receiver` error by printing details to the console.
                        ///</summary>
                        ///<param name="src">The `AdHoc.BytesDst` (often a `Receiver.Framing` or `Receiver` itself) where the error occurred.</param>
                        ///<param name="error">The type of error.</param>
                        ///<param name="ex">An optional exception associated with the error.</param>
                        public void error(AdHoc.BytesDst src, OnError error, Exception? ex = null)
                        {
                            switch (error)
                            {
                                case OnError.FFFF_ERROR:
                                    Console.WriteLine("FFFF_ERROR at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                                case OnError.CRC_ERROR:
                                    Console.WriteLine("CRC_ERROR at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                                case OnError.BYTES_DISTORTION:
                                    Console.WriteLine("BYTES_DISTORTION at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                                case OnError.OVERFLOW:
                                    Console.WriteLine("OVERFLOW at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                                case OnError.INVALID_ID:
                                    Console.WriteLine("INVALID_ID at " + src + (ex == null ? "" : Environment.StackTrace));
                                    return;
                                case OnError.REJECTED:
                                    Console.WriteLine("REJECTED at " + src + (ex == null ? "" : Environment.StackTrace));
                                    return;
                            }
                        }
                    }

                    ///<summary>
                    ///Handles a `Receiver` error.
                    ///</summary>
                    ///<param name="src">The `AdHoc.BytesDst` (often a `Receiver.Framing` or `Receiver` itself) where the error occurred.</param>
                    ///<param name="error">The type of error.</param>
                    ///<param name="ex">An optional exception associated with the error.</param>
                    void error(AdHoc.BytesDst src, OnError error, Exception? ex = null);
                }

                ///<summary>
                ///Implements a framing decoder that handles a byte-oriented protocol with 0xFF as a frame start marker,
                ///0x7F-based escaping for 0x7F and 0xFF bytes within the payload, and a 2-byte CRC.
                ///Frame format: 0xFF (start) + encoded_payload + 2_byte_CRC.
                ///Special characters in payload:
                ///- 0xFF is encoded.
                ///- 0x7F is encoded.
                ///It decodes incoming byte streams and passes complete, validated frames to an `upper_layer` Receiver.
                ///</summary>
                public class Framing : AdHoc.BytesDst, EventsHandler
                {
                    public bool isOpen() => upper_layer.isOpen();

                    ///<summary>
                    ///The `Receiver` instance that will process the decoded frames from this framing layer.
                    ///</summary>
                    public Receiver upper_layer; //Provides external interface for decoded receiving data

                    ///<summary>
                    ///Handles events for decoded frames, typically forwarding them to the original handler of the `upper_layer` before it was hooked by this framing instance.
                    ///</summary>
                    public EventsHandler handler; //Handles decoded frame events for upper layer

                    ///<summary>
                    ///Atomically exchanges the current event handler with a new one.
                    ///</summary>
                    ///<param name="handler">The new event handler to set.</param>
                    ///<returns>The event handler that was previously set.</returns>
                    public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

                    ///<summary>
                    ///Initializes a new instance of the <see cref="Framing"/> class.
                    ///</summary>
                    ///<param name="upper_layer">The `Receiver` instance that will process the decoded frames.</param>
                    public Framing(Receiver upper_layer) => switch_to(upper_layer);

                    ///<summary>
                    ///Switches the framing layer to operate on a new `upper_layer` receiver.
                    ///This also resets the internal state of the framing decoder and hooks its event handling into the new `upper_layer`.
                    ///</summary>
                    ///<param name="upper_layer">The new `Receiver` instance to process decoded frames.</param>
                    public void switch_to(Receiver upper_layer)
                    {
                        reset();

                        if (this.upper_layer != null)
                        {
                            this.upper_layer.Reset();
                            this.upper_layer.exchange(handler); //off hook
                        }

                        handler = (this.upper_layer = upper_layer).exchange(this);
                    }

                    ///<summary>
                    ///Resets the framing decoder's state and reports an error.
                    ///</summary>
                    ///<param name="error">The type of error to report.</param>
                    private void error_reset(OnError error)
                    {
                        error_handler.error(this, error);
                        reset();
                    }

                    public void Close()
                    {
                        reset();
                        upper_layer.Close();
                    }

                    ///<summary>
                    ///Resets the internal state of the framing decoder, including CRC calculations, bit accumulators, and state machine.
                    ///This is called during initialization, when switching receivers, or upon encountering an unrecoverable error.
                    ///</summary>
                    private void reset()
                    {
                        bits = 0;
                        shift = 0;
                        crc0 = 0;
                        crc1 = 0;
                        crc2 = 0;
                        crc3 = 0;
                        dst_byte = 0;
                        raw = 0;
                        waiting_for_dispatching_pack = null;

                        if (!FF) //this packet received, but next packet start FF mark does not deected, so switch to SEEK_FF mode
                            state = State.SEEK_FF;
                    }

                    /**
                     * Writes raw bytes to the framing decoder for processing.
                     * Decoded bytes are passed to the `upper_layer` Receiver.
                     * ATTENTION! The data in the provided buffer "src" may change due to buffer reuse.
                     */
                    public int Write(byte[] src, int src_byte, int src_bytes)
                    {
                        if (src_bytes < 1)
                            return 0;
                        var limit = src_byte + src_bytes;
                        dst_byte = 0;
                        switch (state)
                        {
                            case State.SEEK_FF: //bytes distortion was detected, skip bytes until FF sync mark
                                while (src_byte < limit)
                                    if (src[src_byte++] == 0xFF)
                                    {
                                        state = State.NORMAL;
                                        if (FF)
                                            error_handler.error(this, OnError.FFFF_ERROR);
                                        FF = true;
                                        if (src_byte < limit)
                                            goto write;
                                        return src_bytes;
                                    }
                                    else
                                        FF = false;

                                return src_bytes;
                            case State.Ox7F:
                                if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                                {
                                    error_reset(OnError.BYTES_DISTORTION);
                                    goto write;
                                }

                                bits |= ((raw & 1) << 7 | 0x7F) << shift;
                                put(src, 0);
                                write(src, 1, State.NORMAL);
                                goto case State.Ox7F_;
                            case State.Ox7F_:
                                while (raw == 0x7F)
                                {
                                    if (src_byte == limit)
                                    {
                                        write(src, dst_byte, State.Ox7F_);
                                        return src_bytes;
                                    }

                                    if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                                    {
                                        error_reset(OnError.BYTES_DISTORTION);
                                        goto write;
                                    }

                                    bits |= (raw << 6 | 0x3F) << shift;
                                    if ((shift += 7) < 8)
                                        continue;
                                    shift -= 8;
                                    put(src, dst_byte++);
                                }

                                bits |= (raw >> 1) << shift;
                                if ((shift += 7) < 8)
                                    break;
                                shift -= 8;
                                if (src_byte == dst_byte)
                                {
                                    write(src, dst_byte, State.NORMAL);
                                    dst_byte = 0;
                                }

                                put(src, dst_byte++);
                                state = State.NORMAL;
                                break;
                        }

                    write:
                        while (src_byte < limit)
                        {
                            if ((raw = src[src_byte++]) == 0x7F)
                            {
                                FF = false;
                                if (src_byte == limit)
                                {
                                    write(src, dst_byte, State.Ox7F);
                                    return src_bytes;
                                }

                                if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                                {
                                    error_reset(OnError.BYTES_DISTORTION);
                                    goto write;
                                }

                                bits |= ((raw & 1) << 7 | 0x7F) << shift;

                                put(src, dst_byte++);

                                while (raw == 0x7F)
                                {
                                    if (src_byte == limit)
                                    {
                                        write(src, dst_byte, State.Ox7F_);
                                        return src_bytes;
                                    }

                                    if (FF = (raw = src[src_byte++]) == 0xFF) //FF here is an error
                                    {
                                        error_reset(OnError.BYTES_DISTORTION);
                                        goto write;
                                    }

                                    bits |= ((raw & 1) << 6 | 0x3F) << shift;
                                    if ((shift += 7) < 8)
                                        continue;
                                    shift -= 8;

                                    put(src, dst_byte++);
                                }

                                bits |= (raw >> 1) << shift;
                                if ((shift += 7) < 8)
                                    continue;
                                shift -= 8;
                            }
                            else if (raw == 0xFF) //mark
                            {
                                if (FF)
                                {
                                    error_handler.error(this, OnError.FFFF_ERROR);
                                    continue;
                                }

                                FF = true;
                                //Check if in SEEK_FF state (possible reset after error)
                                if (state == State.SEEK_FF) //can happen after any call of  put(src, dec_position++) that can  call >>> checkCrcThenDispatch >>> reset() so cleanup
                                {
                                    reset();
                                    state = State.NORMAL;
                                }
                                else
                                    write(src, dst_byte, State.NORMAL);

                                continue;
                            }
                            else
                                bits |= raw << shift;

                            FF = false;
                            put(src, dst_byte++);
                        }

                        write(src, dst_byte, State.NORMAL);
                        return src_bytes;
                    }

                    ///<summary>
                    ///Processes a single decoded byte from the bit accumulator, updates the CRC calculation,
                    ///and stores the decoded byte in the destination buffer.
                    ///</summary>
                    ///<param name="dst">The destination buffer where the decoded byte will be written (often the input buffer being reused).</param>
                    ///<param name="dst_index">The index in `dst` where the decoded byte should be written.</param>
                    private void put(byte[] dst, int dst_index)
                    {
                        crc3 = crc2; //shift crc history
                        crc2 = crc1;
                        crc1 = crc0;

                        crc0 = crc16((byte)bits, crc1);
                        dst[dst_index] = (byte)bits;

                        bits >>= 8;
                    }

                    public void OnReceiving(Receiver src, BytesDst dst) => handler?.OnReceiving(src, dst);

                    public void OnReceived(Receiver src, BytesDst pack)
                    {
                        pack_crc = 0;
                        pack_crc_byte = CRC_LEN_BYTES - 1;
                        waiting_for_dispatching_pack = pack;
                        dispatch_on_0 = false;

                        while (0 < src.remaining && waiting_for_dispatching_pack != null)
                            getting_crc(src.get_byte());
                    }

                    ///<summary>
                    ///Writes a block of decoded bytes to the `upper_layer` Receiver.
                    ///Manages the state transition based on whether all bytes were written or if more are expected.
                    ///Also handles potential errors if a new frame marker (0xFF) is encountered prematurely.
                    ///</summary>
                    ///<param name="src">The buffer containing the decoded bytes.</param>
                    ///<param name="limit">The number of decoded bytes in `src` to be written.</param>
                    ///<param name="state_if_ok">The state to transition to if the write operation proceeds normally.</param>
                    private void write(byte[] src, int limit, State state_if_ok)
                    {
                        state = state_if_ok;
                        if (limit == 0)
                            return; //no decoded bytes
                        var BYTE = 0;
                        while (waiting_for_dispatching_pack != null)
                        {
                            getting_crc(src[BYTE++]);
                            if (BYTE == limit)
                                return;
                        }

                        upper_layer.Write(src, BYTE, limit - BYTE);

                        if (upper_layer.mode == OK || !FF)
                            return;
                        error_reset(OnError.BYTES_DISTORTION); //not enough bytes to complete the current packet but already next pack frame detected. error
                    }

                    private BytesDst? waiting_for_dispatching_pack;

                    private bool dispatch_on_0;

                    ///<summary>
                    ///Processes incoming bytes that are part of the CRC checksum.
                    ///Once all CRC bytes are received, it verifies the CRC.
                    ///If valid, it dispatches the packet via `handler.OnReceived`.
                    ///If invalid, it reports a CRC error and resets.
                    ///Supports a two-stage CRC check for certain distortion scenarios.
                    ///</summary>
                    ///<param name="crc_byte">The next byte of the CRC value being received from the stream.</param>
                    private void getting_crc(int crc_byte)
                    {
                        if (dispatch_on_0)
                        {
                            if (crc_byte == 0)
                                handler?.OnReceived(upper_layer, waiting_for_dispatching_pack!); //dispatching
                            else
                                error_handler.error(this, OnError.CRC_ERROR); //bad CRC
                            reset();
                            return;
                        }

                        pack_crc |= (ushort)(crc_byte << pack_crc_byte * 8);

                        pack_crc_byte--;
                        if (-1 < pack_crc_byte)
                            return; //need more

                        if (crc2 == pack_crc)
                            handler?.OnReceived(upper_layer, waiting_for_dispatching_pack!); //dispatching
                        else if (crc16((byte)(pack_crc >> 8), crc3) == crc2)
                        {
                            dispatch_on_0 = true;
                            return;
                        }
                        else
                            error_handler.error(this, OnError.CRC_ERROR); //bad CRC

                        reset();
                    }

                    private int bits;                    //Bit buffer for decoding 7F-escaped sequences.
                    private int shift;                   //Current bit shift within the `bits` accumulator.
                    private ushort pack_crc;             //CRC value read from the incoming packet.
                    private ushort crc0;                 //Current calculated CRC of the payload.
                    private ushort crc1;                 //Previous CRC value (crc0 one step ago).
                    private ushort crc2;                 //CRC value before crc1 (crc0 two steps ago), used for final CRC check.
                    private ushort crc3;                 //CRC value before crc2, used for alternative CRC check.
                    private int pack_crc_byte;           //Counter for bytes received for `pack_crc`.
                    private int raw;                     //Last raw byte read from input stream.
                    private int dst_byte;                //Index for writing decoded bytes if temporarily buffered.
                    private bool FF;                     //Flag indicating if the last byte processed was 0xFF (frame marker).
                    private State state = State.SEEK_FF; //Current state of the framing decoder.

                    ///<summary>
                    ///Defines the states for the framing decoder's state machine.
                    ///</summary>
                    private enum State
                    {
                        ///<summary>Normal byte processing, expecting payload or 0x7F escape, or 0xFF frame marker.</summary>
                        NORMAL = 0,

                        ///<summary>A 0x7F byte was received; expecting the byte that follows it in the escape sequence.</summary>
                        Ox7F = 1,

                        ///<summary>Processing subsequent bytes in a multi-byte 0x7F escape sequence.</summary>
                        Ox7F_ = 2,

                        ///<summary>An error occurred or end of frame; seeking the next 0xFF frame start marker.</summary>
                        SEEK_FF = 3
                    }
                }
                #region Slot
                ///<summary>
                ///Internal class representing a slot in the receiver's processing chain, typically for nested messages.
                ///It extends `Base.Receiver.Slot` and holds message-specific state.
                ///</summary>
                internal class Slot : Base.Receiver.Slot
                {
                    ///<summary>
                    ///The `BytesDst` instance responsible for deserializing the current message or part of a message associated with this slot.
                    ///</summary>
                    public BytesDst dst;

                    public BytesDst dst_(BytesDst dst)
                    {
                        state = 0;
                        return this.dst = dst;
                    }

                    ///<summary>
                    ///A bitmask indicating which fields of a message are null. Used by some deserialization schemes.
                    ///</summary>
                    public int fields_nulls;

                    ///<summary>
                    ///Gets the `BytesDst` instance from the `next` slot in the chain, cast to the specified type `DST`.
                    ///</summary>
                    ///<typeparam name="DST">The expected type of the `BytesDst` in the next slot.</typeparam>
                    ///<returns>The `BytesDst` from the next slot, cast to `DST`.</returns>
                    public DST get_bytes<DST>() => (DST)next.dst;

                    internal Slot next;
                    internal readonly Slot? prev;

                    ///<summary>
                    ///Initializes a new instance of the <see cref="Slot"/> class.
                    ///</summary>
                    ///<param name="dst">The `Receiver` associated with this slot chain.</param>
                    ///<param name="prev">The previous slot in the chain, or null if this is the root slot.</param>
                    public Slot(Receiver dst, Slot? prev) : base(dst)
                    {
                        this.prev = prev;
                        if (prev != null)
                            prev.next = this;
                    }
                }

                internal Slot? slot;
                internal WeakReference<Slot> slot_ref;
                #endregion

                ///<summary>
                ///Reads a byte from the buffer and stores it as `fields_nulls` in the current slot.
                ///This byte is typically a bitmask indicating null fields for a message.
                ///</summary>
                ///<param name="this_case">The state to set for retry if not enough bytes are available.</param>
                ///<returns>True if the byte was read successfully; false if a retry is needed due to insufficient data.</returns>
                public bool get_fields_nulls(uint this_case)
                {
                    if (byte_ < byte_max)
                    {
                        slot!.fields_nulls = buffer![byte_++];
                        return true;
                    }

                    slot!.state = this_case;
                    mode = RETRY;
                    return false;
                }

                ///<summary>
                ///Checks if a specific field is marked as null based on the `fields_nulls` bitmask in the current slot.
                ///</summary>
                ///<param name="field">The bit representing the field to check (e.g., a power of 2).</param>
                ///<returns>True if the field's corresponding bit is NOT set in `fields_nulls` (meaning it is null, by convention where 0 means null); false otherwise.</returns>
                public bool is_null(int field) => (slot!.fields_nulls & field) == 0;

                ///<summary>
                ///Reads a byte. If the byte is 0 (null indicator), returns false.
                ///Otherwise, updates `u8_` by ORing the byte value shifted by `shift`, sets `u8` to this new `u8_` value, and returns true.
                ///</summary>
                ///<param name="shift">The number of bits to left-shift the byte value before ORing with `u8_`.</param>
                ///<returns>False if the read byte is 0; true otherwise.</returns>
                public bool byte_nulls(byte shift)
                {
                    u4 = get_byte();
                    if (u4 == 0)
                        return false;
                    u8 = u8_ |= (ulong)u4 << shift;
                    return true;
                }

                ///<summary>
                ///Reads a byte. If the byte is 0 (null indicator), returns false.
                ///Otherwise, `null_value` is ORed into `u8_`, `u8` is updated to this new `u8_` value, and the method returns true.
                ///This is used when a non-zero byte indicates presence, and `null_value` itself carries the presence flag for `u8_`.
                ///</summary>
                ///<param name="null_value">The value to OR into `u8_` if the read byte is not 0.</param>
                ///<returns>False if the read byte is 0; true otherwise.</returns>
                public bool byte_nulls(ulong null_value)
                {
                    u4 = get_byte();
                    if (u4 == 0)
                        return false;

                    u8 = u8_ |= null_value;
                    return true;
                }

                ///<summary>
                ///Reads a byte. If the byte is 0 (null indicator), returns false.
                ///Otherwise, if the byte is 0xFF, `null_value` is ORed into `u8_`.
                ///If the byte is neither 0 nor 0xFF, the byte value (shifted by `shift`) is ORed into `u8_`.
                ///In either non-zero case, `u8` is updated to the new `u8_` value, and the method returns true.
                ///</summary>
                ///<param name="shift">The number of bits to left-shift the byte value if it's not 0 or 0xFF.</param>
                ///<param name="null_value">The value to OR into `u8_` if the read byte is 0xFF.</param>
                ///<returns>False if the read byte is 0; true otherwise.</returns>
                public bool byte_nulls(byte shift, ulong null_value)
                {
                    u4 = get_byte();
                    if (u4 == 0)
                        return false;

                    u8 = u8_ |= u4 == 0xFF ? null_value : (ulong)u4 << shift;
                    return true;
                }

                ///<summary>
                ///Checks if the receiver is currently idle, meaning it's not in the middle of processing a packet.
                ///</summary>
                ///<returns>True if the receiver is idle (current slot is null); false otherwise.</returns>
                public bool idle() => slot == null;

                ///<summary>
                ///Attempts to read up to 4 bytes into `u4`. If fewer than `bytes_left` bytes are available,
                ///it reads what's available, updates `u4` partially, and decrements `bytes_left`.
                ///</summary>
                ///<returns>True if more bytes are still needed to complete the 4-byte read (i.e., `bytes_left` was not fully satisfied); false if 4 bytes (or the original `bytes_left` requirement) have been read into `u4`.</returns>
                bool not_get4()
                {
                    if (remaining < bytes_left)
                    {
                        var r = remaining;
                        u4 |= get4<uint>(r) << (bytes_max - bytes_left) * 8;
                        bytes_left -= r;
                        return true;
                    }

                    u4 |= get4<uint>(bytes_left) << (bytes_max - bytes_left) * 8;
                    return false;
                }

                ///<summary>
                ///Abstract method to obtain a `BytesDst` handler suitable for deserializing a packet of the given `id`.
                ///This method is called after a packet ID has been read from the stream.
                ///</summary>
                ///<param name="id">The packet identifier.</param>
                ///<returns>A `BytesDst` instance capable of deserializing the identified packet type.</returns>
                ///<exception cref="Exception">Typically thrown if the `id` is unknown or invalid.</exception>
                protected abstract BytesDst _OnReceiving(int id); //throws Exception if wrong id

                protected abstract void _OnReceived(BytesDst received);

                public bool isOpen() => slot != null;

                public virtual void Close() => Reset();

                ///<summary>
                ///Resets the receiver's internal state, including clearing any active slots, buffers,
                ///and resetting processing modes. This prepares the receiver for a new connection or packet.
                ///</summary>
                protected void Reset()
                {
                    if (slot == null)
                        return;

                    for (var s = slot; s != null; s = s.next)
                        s.dst = null;
                    slot = null;

                    if (chs != null)
                    {
                        ArrayPool<char>.Shared.Return(chs);
                        chs = null;
                    }

                    buffer = null;
                    mode = OK;
                    bytes_left = bytes_max = id_bytes;
                    u4 = 0;
                    //dont set   u8 = 0; preserve probably a value pack data for framing layer.
                    //dont set   str = null; preserve probably a value pack data for framing layer.
                }

                /**
                 * Writes raw bytes from `src` to this receiver for processing.
                 * This method drives the deserialization process, identifying packet types,
                 * and invoking message-specific deserialization logic via `BytesDst` handlers.
                 * ATTENTION! The data in the provided buffer `src` may be modified after this call due to buffer reuse.
                 */
                public int Write(byte[] src, int src_byte, int src_bytes)
                {
                    if (src_bytes < 1)
                        return 0;
                    for (buffer = src, byte_max = (byte_ = src_byte) + src_bytes; byte_ < byte_max;)
                    {
                        if (slot?.dst == null)
                            try
                            {
                                if (not_get4())
                                    goto exit;                   //read id
                                var dst = _OnReceiving((int)u4); //throws Exception if wrong id
                                if (slot == null && !slot_ref.TryGetTarget(out slot))
                                    slot_ref = new WeakReference<Slot>(slot = new Slot(this, null));

                                slot.dst = dst;
                                bytes_left = bytes_max = id_bytes;
                                u4 = 0;
                                u8 = 0;
                                u8_ = 0;
                                slot.state = 0;
                                handler?.OnReceiving(this, dst);
                                if (slot == null)
                                    return -1; //receiving event handler has reset this
                            }
                            catch (Exception ex)
                            {
                                Reset();
                                error_handler.error(this, OnError.INVALID_ID, ex);
                                break;
                            }
                        else //internal write
                            switch (mode)
                            {
                                case INT1:
                                    if (not_get4())
                                        goto exit;
                                    u8 = (ulong)(sbyte)u4;
                                    break;
                                case INT2:
                                    if (not_get4())
                                        goto exit;
                                    u8 = (ulong)(short)u4;
                                    break;
                                case INT4:
                                    if (not_get4())
                                        goto exit;
                                    u8 = (ulong)(int)u4;
                                    break;
                                case VAL4:
                                    if (not_get4())
                                        goto exit;
                                    break;
                                case VAL8:

                                    if (remaining < bytes_left)
                                    {
                                        var r = remaining;
                                        u8 |= get8<ulong>(r) << (bytes_max - bytes_left) * 8;
                                        bytes_left -= r;
                                        goto exit;
                                    }

                                    u8 |= get8<ulong>(bytes_left) << (bytes_max - bytes_left) * 8;
                                    break;
                                case LEN0:
                                    if (not_get4())
                                        goto exit;
                                    slot.check_len0((int)u4);
                                    break;
                                case LEN1:
                                    if (not_get4())
                                        goto exit;
                                    slot.check_len1((int)u4);
                                    break;
                                case LEN2:
                                    if (not_get4())
                                        goto exit;
                                    slot.check_len2((int)u4);
                                    break;
                                case VARINT:
                                    if (varint())
                                        break;
                                    goto exit;

                                case STR:

                                    if (!varint())
                                        goto exit;

                                    if (u8_ == ulong.MaxValue)
                                        if (check_length_and_getting_string())
                                            break;
                                        else
                                            goto exit;

                                    chs![u4++] = (char)u8;
                                    if (getting_string())
                                        break;
                                    goto exit;
                            }

                        mode = OK;

                        for (; ; )
                            if (!slot.dst!.__put_bytes(this))
                                goto exit; //data over
                            else
                            {
                                if (slot.prev == null)
                                    break;
                                slot = slot.prev;
                            }

                        _OnReceived(slot.dst);
                        handler?.OnReceived(this, slot.dst); //dispatching

                        u4 = 0;
                        bytes_left = bytes_max = id_bytes;
                        if (slot == null)
                            return -1;   //received event handler has reset this
                        slot.dst = null; //preparing to read next packet data
                    }

                    if (slot != null && slot.dst == null)
                        Reset();

                    exit:
                    buffer = null;

                    return byte_ - src_byte;
                }

                ///<summary>
                ///Gets the `BytesDst` instance for the current slot and invokes its `__put_bytes` method
                ///to continue deserialization of the current message.
                ///</summary>
                ///<typeparam name="DST">The expected type of the `BytesDst` handler.</typeparam>
                ///<param name="dst">The `BytesDst` instance, typically pre-allocated for the message type.</param>
                ///<returns>The `dst` instance after `__put_bytes` has been called.</returns>
                public DST get_bytes<DST>(DST dst)
                    where DST : BytesDst
                {
                    slot!.state = 0;
                    dst.__put_bytes(this);
                    return dst;
                }

                ///<summary>
                ///Attempts to get the `BytesDst` handler for a nested message.
                ///If successful (enough data for the nested message part), it processes it and returns the handler.
                ///If not enough data, sets the receiver to retry at `next_case` and returns null.
                ///</summary>
                ///<typeparam name="DST">The type of the `BytesDst` handler for the nested message.</typeparam>
                ///<param name="dst">The `BytesDst` instance for the nested message.</param>
                ///<param name="next_case">The state to set for retry if processing the nested message requires more data.</param>
                ///<returns>The `dst` instance if processing was successful (or initiated successfully); null if a retry is needed.</returns>
                public DST? try_get_bytes<DST>(DST dst, uint next_case)
                    where DST : class?, BytesDst
                {
                    var s = slot!;

                    (slot = s.next ?? (s.next = new Slot(this, s))).dst = dst;
                    slot!.state = 0;
                    u8_ = 0;
                    if (dst.__put_bytes(this))
                    {
                        slot = s;
                        return dst;
                    }

                    s.state = next_case;

                    return null;
                }

                public int get_bytes(byte[] dst, int dst_byte, int dst_bytes, uint retry_case)
                {
                    if (remaining < dst_bytes)
                    {
                        dst_bytes = remaining;
                        retry_at(retry_case);
                    }

                    Array.Copy(buffer!, byte_, dst, dst_byte, dst_bytes);

                    byte_ += dst_bytes;
                    return dst_bytes;
                }

                ///<summary>
                ///Sets the receiver's current processing mode to `RETRY` and records `the_case` as the state
                ///to resume from when more data is available.
                ///</summary>
                ///<param name="the_case">The specific state or step in the deserialization logic to resume at.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void retry_at(uint the_case)
                {
                    slot!.state = the_case;
                    mode = RETRY;
                }

                ///<summary>
                ///Checks if there are any bytes remaining in the current buffer segment (`byte_ < byte_max`).
                ///If not, sets the receiver to retry mode at `next_case`.
                ///</summary>
                ///<param name="next_case">The state to set for retry if no bytes are remaining.</param>
                ///<returns>True if bytes are remaining; false if no bytes are remaining and retry mode is set.</returns>
                public bool has_bytes(uint next_case)
                {
                    if (byte_ < byte_max)
                        return true;
                    mode = RETRY;
                    slot!.state = next_case;
                    return false;
                }

                ///<summary>
                ///Checks if at least 1 byte is available in the buffer. If not, sets up a retry to get 1 byte
                ///(as part of a 4-byte read attempt for efficiency) and transitions to `get_case`.
                ///</summary>
                ///<param name="get_case">The state to transition to if a retry is needed.</param>
                ///<returns>True if at least 1 byte is immediately available or if the retry mechanism is successfully initiated; effectively, true if processing can continue or will be retried for this byte.</returns>
                public bool has_1bytes(uint get_case) => 0 < byte_max - byte_ || retry_get4(1, get_case);

                ///<summary>
                ///Gets the boolean value stored in the temporary `u4` buffer (1 for true, 0 for false).
                ///Assumes `u4` was populated by a previous read operation (e.g., `try_get4`).
                ///</summary>
                ///<returns>The boolean value from `u4`.</returns>
                public bool get_bool_() => u4 == 1;

                ///<summary>
                ///Reads a single byte from the buffer and interprets it as a boolean (1 for true, 0 for false).
                ///Advances the buffer position.
                ///</summary>
                ///<returns>The boolean value.</returns>
                public bool get_bool() => buffer![byte_++] == 1;

                ///<summary>
                ///Gets the signed byte value stored in the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The signed byte value from `u4`.</returns>
                public sbyte get_sbyte_() => (sbyte)u4;

                ///<summary>
                ///Reads a single signed byte from the buffer. Advances the buffer position.
                ///</summary>
                ///<returns>The signed byte value.</returns>
                public sbyte get_sbyte() => (sbyte)buffer![byte_++];

                ///<summary>
                ///Gets the unsigned byte value stored in the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The unsigned byte value from `u4`.</returns>
                public byte get_byte_() => (byte)u4;

                ///<summary>
                ///Reads a single unsigned byte from the buffer. Advances the buffer position.
                ///</summary>
                ///<returns>The unsigned byte value.</returns>
                public byte get_byte() => buffer![byte_++];

                ///<summary>
                ///Checks if at least 2 bytes are available in the buffer. If not, sets up a retry to get 2 bytes
                ///(as part of a 4-byte read attempt) and transitions to `get_case`.
                ///</summary>
                ///<param name="get_case">The state to transition to if a retry is needed.</param>
                ///<returns>True if at least 2 bytes are immediately available or if the retry is initiated.</returns>
                public bool has_2bytes(uint get_case) => 1 < byte_max - byte_ || retry_get4(2, get_case);

                ///<summary>
                ///Gets the short (Int16) value stored in the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The short value from `u4`.</returns>
                public short get_short_() => (short)u4;

                ///<summary>
                ///Reads a 2-byte short (Int16) value from the buffer using appropriate endianness.
                ///Advances the buffer position by 2 bytes.
                ///</summary>
                ///<returns>The short value.</returns>
                public short get_short()
                {
                    var ret = Endianness.OK.Int16(buffer!, byte_);
                    byte_ += 2;
                    return ret;
                }

                ///<summary>
                ///Gets the unsigned short (UInt16) value stored in the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The unsigned short value from `u4`.</returns>
                public ushort get_ushort_() => (ushort)u4;

                ///<summary>
                ///Reads a 2-byte unsigned short (UInt16) value from the buffer using appropriate endianness.
                ///Advances the buffer position by 2 bytes.
                ///</summary>
                ///<returns>The unsigned short value.</returns>
                public ushort get_ushort()
                {
                    var ret = Endianness.OK.UInt16(buffer!, byte_);
                    byte_ += 2;
                    return ret;
                }

                ///<summary>
                ///Checks if at least 4 bytes are available in the buffer. If not, sets up a retry to get 4 bytes
                ///and transitions to `get_case`.
                ///</summary>
                ///<param name="get_case">The state to transition to if a retry is needed.</param>
                ///<returns>True if at least 4 bytes are immediately available or if the retry is initiated.</returns>
                public bool has_4bytes(uint get_case) => 3 < byte_max - byte_ || retry_get4(4, get_case);

                ///<summary>
                ///Gets the integer (Int32) value stored in the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The integer value from `u4`.</returns>
                public int get_int_() => (int)u4;

                ///<summary>
                ///Reads a 4-byte integer (Int32) value from the buffer using appropriate endianness.
                ///Advances the buffer position by 4 bytes.
                ///</summary>
                ///<returns>The integer value.</returns>
                public int get_int()
                {
                    var ret = Endianness.OK.Int32(buffer!, byte_);
                    byte_ += 4;
                    return ret;
                }

                ///<summary>
                ///Gets the unsigned integer (UInt32) value stored in the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The unsigned integer value from `u4`.</returns>
                public uint get_uint_() => u4;

                ///<summary>
                ///Reads a 4-byte unsigned integer (UInt32) value from the buffer using appropriate endianness.
                ///Advances the buffer position by 4 bytes.
                ///</summary>
                ///<returns>The unsigned integer value.</returns>
                public uint get_uint()
                {
                    var ret = Endianness.OK.UInt32(buffer!, byte_);
                    byte_ += 4;
                    return ret;
                }

                ///<summary>
                ///Checks if at least 8 bytes are available in the buffer. If not, sets up a retry to get 8 bytes
                ///and transitions to `get_case`.
                ///</summary>
                ///<param name="get_case">The state to transition to if a retry is needed.</param>
                ///<returns>True if at least 8 bytes are immediately available or if the retry is initiated.</returns>
                public bool has_8bytes(uint get_case) => 7 < byte_max - byte_ || retry_get8(8, get_case);

                ///<summary>
                ///Gets the long (Int64) value stored in the temporary `u8` buffer.
                ///Assumes `u8` was populated by a previous read operation.
                ///</summary>
                ///<returns>The long value from `u8`.</returns>
                public long get_long_() => (long)u8;

                ///<summary>
                ///Reads an 8-byte long (Int64) value from the buffer using appropriate endianness.
                ///Advances the buffer position by 8 bytes.
                ///</summary>
                ///<returns>The long value.</returns>
                public long get_long()
                {
                    var ret = Endianness.OK.Int64(buffer!, byte_);
                    byte_ += 8;
                    return ret;
                }

                ///<summary>
                ///Gets the unsigned long (UInt64) value stored in the temporary `u8` buffer.
                ///Assumes `u8` was populated by a previous read operation.
                ///</summary>
                ///<returns>The unsigned long value from `u8`.</returns>
                public ulong get_ulong_() => u8;

                ///<summary>
                ///Reads an 8-byte unsigned long (UInt64) value from the buffer using appropriate endianness.
                ///Advances the buffer position by 8 bytes.
                ///</summary>
                ///<returns>The unsigned long value.</returns>
                public ulong get_ulong()
                {
                    var ret = Endianness.OK.UInt64(buffer!, byte_);
                    byte_ += 8;
                    return ret;
                }

                ///<summary>
                ///Reads an 8-byte double-precision floating-point value from the buffer.
                ///The bytes are interpreted as UInt64 (respecting endianness) and then converted to double.
                ///Advances the buffer position by 8 bytes.
                ///</summary>
                ///<returns>The double value.</returns>
                public double get_double()
                {
                    var ret = BitConverter.UInt64BitsToDouble(Endianness.OK.UInt64(buffer!, byte_));
                    byte_ += 8;
                    return ret;
                }

                ///<summary>
                ///Gets the double value by reinterpreting the bits of the temporary `u8` buffer.
                ///Assumes `u8` was populated by a previous read operation.
                ///</summary>
                ///<returns>The double value from `u8`.</returns>
                public double get_double_() => BitConverter.UInt64BitsToDouble(u8);

                ///<summary>
                ///Reads a 4-byte single-precision floating-point value from the buffer.
                ///The bytes are interpreted as UInt32 (respecting endianness) and then converted to float.
                ///Advances the buffer position by 4 bytes.
                ///</summary>
                ///<returns>The float value.</returns>
                public float get_float()
                {
                    var ret = BitConverter.UInt32BitsToSingle(Endianness.OK.UInt32(buffer!, byte_));
                    byte_ += 4;
                    return ret;
                }

                ///<summary>
                ///Gets the float value by reinterpreting the bits of the temporary `u4` buffer.
                ///Assumes `u4` was populated by a previous read operation.
                ///</summary>
                ///<returns>The float value from `u4`.</returns>
                public float get_float_() => BitConverter.UInt32BitsToSingle(u4);
                #region get_into_u8
                ///<summary>
                ///Attempts to read a signed byte from the buffer and store its sign-extended value in `u8`.
                ///If not enough data, sets up a retry for 1 byte (read as INT1 type) at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the signed byte was read and stored successfully; false if a retry is needed.</returns>
                public bool get_sbyte_u8(uint get_case)
                {
                    if (0 < byte_max - byte_)
                    {
                        u8 = buffer![byte_++];
                        return true;
                    }

                    retry_get4(1, get_case);
                    mode = INT1;
                    return false;
                }

                ///<summary>
                ///Attempts to read an unsigned byte from the buffer and store its value in `u8`.
                ///If not enough data, sets up a retry for 1 byte at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the byte was read and stored successfully; false if a retry is needed.</returns>
                public bool get_byte_u8(uint get_case)
                {
                    if (byte_max - byte_ == 0)
                        return retry_get8(1, get_case);
                    u8 = buffer![byte_++];
                    return true;
                }

                ///<summary>
                ///Attempts to read a short (Int16) from the buffer and store its sign-extended value in `u8`.
                ///If not enough data, sets up a retry for 2 bytes (read as INT2 type) at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the short was read and stored successfully; false if a retry is needed.</returns>
                public bool get_short_u8(uint get_case)
                {
                    if (1 < byte_max - byte_)
                    {
                        u8 = (ulong)Endianness.OK.Int16(buffer!, byte_);
                        byte_ += 2;
                        return true;
                    }

                    retry_get4(2, get_case);
                    mode = INT2;
                    return false;
                }

                ///<summary>
                ///Attempts to read an unsigned short (UInt16) from the buffer and store its value in `u8`.
                ///If not enough data, sets up a retry for 2 bytes at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the unsigned short was read and stored successfully; false if a retry is needed.</returns>
                public bool get_ushort_u8(uint get_case)
                {
                    if (byte_max - byte_ < 2)
                        return retry_get8(2, get_case);
                    u8 = Endianness.OK.UInt16(buffer!, byte_);
                    byte_ += 2;
                    return true;
                }

                ///<summary>
                ///Attempts to read an integer (Int32) from the buffer and store its sign-extended value in `u8`.
                ///If not enough data, sets up a retry for 4 bytes (read as INT4 type) at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the integer was read and stored successfully; false if a retry is needed.</returns>
                public bool get_int_u8(uint get_case)
                {
                    if (3 < byte_max - byte_)
                    {
                        u8 = (ulong)Endianness.OK.Int32(buffer!, byte_);
                        byte_ += 4;
                        return true;
                    }

                    retry_get4(4, get_case);
                    mode = INT4;
                    return false;
                }

                ///<summary>
                ///Attempts to read an unsigned integer (UInt32) from the buffer and store its value in `u8`.
                ///If not enough data, sets up a retry for 4 bytes at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the unsigned integer was read and stored successfully; false if a retry is needed.</returns>
                public bool get_uint_u8(uint get_case)
                {
                    if (byte_max - byte_ < 4)
                        return retry_get8(4, get_case);
                    u8 = Endianness.OK.UInt32(buffer!, byte_);
                    byte_ += 4;
                    return true;
                }

                ///<summary>
                ///Attempts to read a long (Int64) from the buffer and store its value in `u8`.
                ///If not enough data, sets up a retry for 8 bytes at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the long was read and stored successfully; false if a retry is needed.</returns>
                public bool get_long_u8(uint get_case)
                {
                    if (byte_max - byte_ < 8)
                        return retry_get8(8, get_case);
                    u8 = (ulong)Endianness.OK.Int64(buffer!, byte_);
                    byte_ += 8;
                    return true;
                }

                ///<summary>
                ///Attempts to read an unsigned long (UInt64) from the buffer and store its value in `u8`.
                ///If not enough data, sets up a retry for 8 bytes at `get_case`.
                ///</summary>
                ///<param name="get_case">The state to retry at if reading fails.</param>
                ///<returns>True if the unsigned long was read and stored successfully; false if a retry is needed.</returns>
                public bool get_ulong_u8(uint get_case)
                {
                    if (byte_max - byte_ < 8)
                        return retry_get8(8, get_case);
                    u8 = Endianness.OK.UInt64(buffer!, byte_);
                    byte_ += 8;
                    return true;
                }
                #endregion
                #region 8
                ///<summary>
                ///Attempts to read a specified number of bytes (up to 8) from the buffer into `u8`.
                ///If fewer than `bytes` are available, sets up a retry for the required number of bytes at `get8_case`.
                ///</summary>
                ///<param name="bytes">The number of bytes to read (1 to 8).</param>
                ///<param name="get8_case">The state to retry at if reading fails.</param>
                ///<returns>True if the bytes were read successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool try_get8(int bytes, uint get8_case)
                {
                    if (remaining < bytes)
                        return retry_get8(bytes, get8_case);
                    u8 = get8<ulong>(bytes);
                    return true;
                }

                ///<summary>
                ///Sets up a retry operation to read a specified number of bytes (up to 8) into `u8`.
                ///It reads any currently available bytes, stores them partially in `u8`,
                ///and sets the mode to `VAL8` and state to `get8_case` for continuation.
                ///</summary>
                ///<param name="bytes">The total number of bytes that need to be read (1 to 8).</param>
                ///<param name="get8_case">The state to resume at when more data is available.</param>
                ///<returns>Always false, indicating that a retry is required.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool retry_get8(int bytes, uint get8_case)
                {
                    bytes_left = (bytes_max = bytes) - remaining;
                    u8 = get8<ulong>(remaining);
                    slot!.state = get8_case;
                    mode = VAL8;
                    return false;
                }

                ///<summary>
                ///Reads a specified number of bytes (up to 8) from the buffer and returns them as type `T`.
                ///The bytes are combined into a `ulong` respecting endianness, then reinterpreted as `T`.
                ///Advances the buffer position by `bytes`.
                ///</summary>
                ///<typeparam name="T">The target type (must be compatible with an 8-byte representation if `bytes` is large).</typeparam>
                ///<param name="bytes">The number of bytes to read (1 to 8).</param>
                ///<returns>The value of type `T` read from the buffer.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public T get8<T>(int bytes)
                {
                    ulong u8 = 0;
                    byte_ += bytes;
                    switch (bytes)
                    {
                        case 8:
                            u8 = Endianness.OK.UInt64(buffer!, byte_ - 8);
                            break;
                        case 7:
                            u8 = Endianness.OK.UInt32(buffer!, byte_ - 7);
                            u8 |= (ulong)Endianness.OK.UInt16(buffer!, byte_ - 3) << 32;
                            u8 |= (ulong)buffer![byte_ - 1] << 48;
                            break;
                        case 6:
                            u8 = Endianness.OK.UInt32(buffer!, byte_ - 6);
                            u8 |= (ulong)Endianness.OK.UInt16(buffer!, byte_ - 2) << 32;
                            break;
                        case 5:
                            u8 = Endianness.OK.UInt32(buffer!, byte_ - 5);
                            u8 |= (ulong)buffer![byte_ - 1] << 32;
                            break;
                        case 4:
                            u8 = Endianness.OK.UInt32(buffer!, byte_ - 4);
                            break;
                        case 3:
                            u8 = Endianness.OK.UInt16(buffer!, byte_ - 3);
                            u8 |= (ulong)buffer![byte_ - 1] << 16;
                            break;
                        case 2:
                            u8 = Endianness.OK.UInt16(buffer!, byte_ - 2);
                            break;
                        case 1:
                            u8 = buffer![byte_ - 1];
                            break;
                    }

                    return Unsafe.As<ulong, T>(ref u8);
                }
                #endregion
                #region 4
                ///<summary>
                ///Attempts to read the number of bytes currently specified by `bytes_left` (assumed to be up to 4) from the buffer into `u4`.
                ///If insufficient bytes are available, sets up a retry at `next_case`.
                ///</summary>
                ///<param name="next_case">The state to retry at if reading fails.</param>
                ///<returns>True if the bytes were read successfully; false if a retry is needed.</returns>
                public bool try_get4(uint next_case) => try_get4(bytes_left, next_case);

                ///<summary>
                ///Attempts to read a specified number of bytes (up to 4) from the buffer into `u4`.
                ///If fewer than `bytes` are available, sets up a retry for the required number of bytes at `next_case`.
                ///</summary>
                ///<param name="bytes">The number of bytes to read (1 to 4).</param>
                ///<param name="next_case">The state to retry at if reading fails.</param>
                ///<returns>True if the bytes were read successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool try_get4(int bytes, uint next_case)
                {
                    if (remaining < bytes)
                        return retry_get4(bytes, next_case);
                    u4 = get4<uint>(bytes);
                    return true;
                }

                ///<summary>
                ///Sets up a retry operation to read a specified number of bytes (up to 4) into `u4`.
                ///It reads any currently available bytes, stores them partially in `u4`,
                ///and sets the mode to `VAL4` and state to `get4_case` for continuation.
                ///</summary>
                ///<param name="bytes">The total number of bytes that need to be read (1 to 4).</param>
                ///<param name="get4_case">The state to resume at when more data is available.</param>
                ///<returns>Always false, indicating that a retry is required.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool retry_get4(int bytes, uint get4_case)
                {
                    bytes_left = (bytes_max = bytes) - remaining;
                    u4 = get4<uint>(remaining);
                    slot!.state = get4_case;
                    mode = VAL4;
                    return false;
                }

                ///<summary>
                ///Gets the value currently stored in the `u4` buffer, reinterpreted as type `T`.
                ///This is an unsafe cast and assumes `T` is compatible with a 4-byte representation.
                ///</summary>
                ///<typeparam name="T">The type to reinterpret the `u4` buffer as.</typeparam>
                ///<returns>The value from `u4` cast to type `T`.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public T get4<T>() => Unsafe.As<uint, T>(ref u4);

                ///<summary>
                ///Reads a specified number of bytes (up to 4) from the buffer and returns them as type `T`.
                ///The bytes are combined into a `uint` respecting endianness, then reinterpreted as `T`.
                ///Advances the buffer position by `bytes`.
                ///</summary>
                ///<typeparam name="T">The target type (must be compatible with a 4-byte representation if `bytes` is large).</typeparam>
                ///<param name="bytes">The number of bytes to read (1 to 4).</param>
                ///<returns>The value of type `T` read from the buffer.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public T get4<T>(int bytes)
                {
                    uint u4 = 0;
                    byte_ += bytes;
                    switch (bytes)
                    {
                        case 4:
                            u4 = Endianness.OK.UInt32(buffer!, byte_ - 4);
                            break;
                        case 3:
                            u4 = Endianness.OK.UInt16(buffer!, byte_ - 3);
                            u4 |= (uint)buffer![byte_ - 1] << 16;
                            break;
                        case 2:
                            u4 = Endianness.OK.UInt16(buffer!, byte_ - 2);
                            break;
                        case 1:
                            u4 = buffer![byte_ - 1];
                            break;
                    }

                    return Unsafe.As<uint, T>(ref u4);
                }
                #endregion
                #region bits
                ///<summary>
                ///Initializes the state for bitwise reading operations. Resets `bits` accumulator and `bit` pointer.
                ///</summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void init_bits()
                {
                    bits = 0;
                    bit = 8;
                }

                ///<summary>
                ///Gets the value currently stored in `u4` (populated by `try_get_bits`), reinterpreted as type `T`.
                ///</summary>
                ///<typeparam name="T">The type to reinterpret `u4` as.</typeparam>
                ///<returns>The bits value from `u4` cast to type `T`.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public T get_bits<T>() => Unsafe.As<uint, T>(ref u4);

                ///<summary>
                ///Reads a specified number of bits from the buffer, potentially consuming one or more bytes.
                ///Bits are read from the internal `bits` accumulator; if more bits are needed, a byte is read from `buffer`.
                ///</summary>
                ///<typeparam name="T">The type to return the bits as (e.g., int, byte). The value is stored in the lower bits of `T`.</typeparam>
                ///<param name="len_bits">The number of bits to read (must be less than or equal to the bit-width of `T` and typically small, e.g., 1-8).</param>
                ///<returns>The bits read from the buffer, cast to type `T`.</returns>
                public T get_bits<T>(int len_bits)
                {
                    uint ret;
                    if (bit + len_bits < 9)
                    {
                        ret = bits >> bit & 0xFFU >> 8 - len_bits;
                        bit += len_bits;
                    }
                    else
                    {
                        ret = (bits >> bit | (bits = buffer![byte_++]) << 8 - bit) & 0xFFU >> 8 - len_bits;
                        bit = bit + len_bits - 8;
                    }

                    return Unsafe.As<uint, T>(ref ret);
                }

                ///<summary>
                ///Attempts to read a specified number of bits from the buffer into `u4`.
                ///If not enough data (bytes) is available in the buffer to satisfy the bit read,
                ///sets up a retry at `this_case`.
                ///</summary>
                ///<param name="len_bits">The number of bits to read.</param>
                ///<param name="this_case">The state to retry at if reading fails due to insufficient underlying bytes.</param>
                ///<returns>True if the bits were read successfully into `u4`; false if a retry is needed.</returns>
                public bool try_get_bits(int len_bits, uint this_case)
                {
                    if (bit + len_bits < 9)
                    {
                        u4 = bits >> bit & 0xFFU >> 8 - len_bits;
                        bit += len_bits;
                    }
                    else if (byte_ < byte_max)
                    {
                        u4 = (bits >> bit | (bits = buffer![byte_++]) << (8 - bit)) & 0xFFU >> 8 - len_bits;
                        bit = bit + len_bits - 8;
                    }
                    else //not enough data
                    {
                        retry_at(this_case);
                        return false;
                    }

                    return true;
                }
                #endregion
                #region varint
                ///<summary>
                ///Attempts to read `bytes_left` (assumed to be up to 8) from the buffer into `u8` for varint processing.
                ///If insufficient bytes, sets up a retry. This is a specialized version of `try_get8` for varint contexts.
                ///</summary>
                ///<param name="next_case">The state to retry at if reading fails.</param>
                ///<returns>True if bytes were read successfully; false if a retry is needed.</returns>
                public bool try_get8(uint next_case) => try_get8(bytes_left, next_case);

                ///<summary>
                ///Tries to read 1 bit for varint length information (0 for 1 byte, 1 for 2 bytes).
                ///Sets `bytes_left` and `bytes_max` according to this length.
                ///</summary>
                ///<param name="bits">Number of bits for length info (should be 1 for this method).</param>
                ///<param name="this_case">The state to retry at if reading bits fails.</param>
                ///<returns>True if length bit was read and `bytes_left` set; false if retry is needed.</returns>
                public bool try_get_varint_bits1(int bits, uint this_case)
                {
                    if (!try_get_bits(bits, this_case))
                        return false;
                    bytes_left = bytes_max = get_bits<int>() + 1;
                    return true;
                }

                ///<summary>
                ///Tries to read a specified number of bits for varint length information.
                ///Sets `bytes_left` and `bytes_max` according to this length.
                ///</summary>
                ///<param name="bits">Number of bits for length info.</param>
                ///<param name="this_case">The state to retry at if reading bits fails.</param>
                ///<returns>True if length bits were read and `bytes_left` set; false if retry is needed.</returns>
                public bool try_get_varint_bits(int bits, uint this_case)
                {
                    if (!try_get_bits(bits, this_case))
                        return false;
                    bytes_left = bytes_max = get_bits<int>();
                    return true;
                }

                ///<summary>
                ///Attempts to read a varint-encoded unsigned long from the buffer into `u8`.
                ///If not enough data, sets mode to `VARINT` and state to `next_case` for retry.
                ///</summary>
                ///<param name="next_case">The state to retry at if reading fails.</param>
                ///<returns>True if the varint was read successfully; false if a retry is needed.</returns>
                public bool try_get_varint(uint next_case)
                {
                    u8 = 0;
                    bytes_left = 0;

                    if (varint())
                        return true;

                    slot!.state = next_case;
                    mode = VARINT;
                    return false;
                }

                ///<summary>
                ///Reads a varint-encoded unsigned long from the `buffer` and accumulates it into `u8`.
                ///Updates `byte_` (buffer position) and `bytes_left` (varint shift accumulator).
                ///</summary>
                ///<returns>True if a complete varint value has been read; false if more bytes are needed from the buffer.</returns>
                private bool varint()
                {
                    for (ulong b; byte_ < byte_max; u8 |= (b & 0x7FUL) << bytes_left, bytes_left += 7)
                        if ((b = buffer![byte_++]) < 0x80)
                        {
                            u8 |= b << bytes_left;
                            return true;
                        }

                    return false;
                }

                ///<summary>
                ///Decodes a ZigZag-encoded unsigned long back to a signed long.
                ///ZigZag encoding maps signed integers to unsigned integers so that numbers with a small absolute value
                ///(i.e., close to zero, positive or negative) have a small varint encoded value.
                ///</summary>
                ///<param name="src">The ZigZag-encoded unsigned long value.</param>
                ///<returns>The decoded signed long value.</returns>
                public static long zig_zag(ulong src) => -(long)(src & 1) ^ (long)(src >> 1);
                #endregion
                #region dims
                private int[] dims = Array.Empty<int>(); //temporary buffer for the receiving string and more

                ///<summary>
                ///Initializes or resizes the internal `dims` array used for storing dimensions of multi-dimensional data.
                ///Also resets `u8` to 1 (often used as a running product of dimensions).
                ///</summary>
                ///<param name="size">The required size of the dimensions array.</param>
                public void init_dims(int size)
                {
                    u8 = 1;
                    if (size <= dims.Length)
                        return;
                    ArrayPool<int>.Shared.Return(dims);
                    dims = ArrayPool<int>.Shared.Rent(size);
                }

                ///<summary>
                ///Gets a dimension value from the internal `dims` array at the specified index.
                ///</summary>
                ///<param name="index">The index of the dimension to retrieve.</param>
                ///<returns>The dimension value stored at `dims[index]`.</returns>
                public int dim(int index) => dims[index];

                ///<summary>
                ///Reads a 4-byte integer from the buffer, validates it against `max`, stores it in `dims[index]`,
                ///and multiplies it into `u8` (which typically accumulates the total number of elements).
                ///Reports an overflow error if `dim > max`.
                ///</summary>
                ///<param name="max">The maximum allowed value for this dimension.</param>
                ///<param name="index">The index in the `dims` array where this dimension should be stored.</param>
                public void dim(int max, int index)
                {
                    var dim = get4<int>();
                    if (max < dim)
                        error_handler.error(this, OnError.OVERFLOW, new ArgumentOutOfRangeException("In dim(int max, int index){} max < dim : " + max + " < " + dim));

                    u8 *= (ulong)dim;
                    dims[index] = dim;
                }

                ///<summary>
                ///Reads a 4-byte integer from the buffer representing a length, and validates it against `max`.
                ///Reports an overflow error if `len > max`. If an overflow occurs, `u8` (often total elements) is set to 0.
                ///</summary>
                ///<param name="max">The maximum allowed value for this length.</param>
                ///<returns>The read length if it's within the `max` limit; otherwise, 0 if an overflow occurred.</returns>
                public int length(long max)
                {
                    var len = get4<int>();
                    if (len <= max)
                        return len;

                    error_handler.error(this, OnError.OVERFLOW, new ArgumentOutOfRangeException("In length(long max){} max < len : " + max + " < " + len));
                    u8 = 0;
                    return 0;
                }
                #endregion
                #region string
                ///<summary>
                ///Gets the string value currently stored in the `str` field and then clears `str` to null.
                ///Assumes `str` was populated by a previous string deserialization operation.
                ///</summary>
                ///<returns>The string value from the `str` field, or null if `str` was already null.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public string? get_string()
                {
                    var ret = str;
                    str = null;
                    return ret;
                }

                ///<summary>
                ///Attempts to read a varint-encoded string from the buffer. The string consists of a varint-encoded length
                ///followed by varint-encoded characters.
                ///If not enough data, sets mode to `STR` and state to `get_string_case` for retry.
                ///</summary>
                ///<param name="max_chars">The maximum number of characters allowed for the string. An overflow error is reported if the decoded length exceeds this.</param>
                ///<param name="get_string_case">The state to retry at if reading fails.</param>
                ///<returns>True if the string was read successfully and stored in `str`; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool try_get_string(uint max_chars, int get_string_case)
                {
                    u4 = max_chars;
                    u8_ = ulong.MaxValue; //indicate state before string length received
                    u8 = 0;               //varint receiving string char holde
                    bytes_left = 0;       //varint pointer

                    if (varint() && //getting string length into u8
                        check_length_and_getting_string())
                        return true;

                    slot!.state = (uint)get_string_case;
                    mode = STR; //lack of received bytes, switch to reading lines internally
                    return false;
                }

                private char[]? chs;

                ///<summary>
                ///Checks if the decoded string length (`u8`) is within the `u4` (max_chars) limit.
                ///Allocates or reuses `chs` character buffer. Then proceeds to read characters via `getting_string()`.
                ///</summary>
                ///<returns>True if length check passes and `getting_string()` returns true; false otherwise (e.g., overflow or incomplete character read).</returns>
                private bool check_length_and_getting_string()
                {
                    if (u4 < u8)
                        error_handler.error(this, OnError.OVERFLOW, new ArgumentOutOfRangeException("In check_length_and_getting_string(){} u4 < u8 : " + u4 + " < " + u8));
                    if (chs == null)
                        chs = ArrayPool<char>.Shared.Rent((int)u8);
                    else if (chs.Length < (int)u8)
                    {
                        ArrayPool<char>.Shared.Return(chs);
                        chs = ArrayPool<char>.Shared.Rent((int)u8);
                    }

                    u8_ = u8; //store string length into u8_
                    u4 = 0;   //index receiving char

                    return getting_string();
                }

                ///<summary>
                ///Reads varint-encoded characters from the buffer and populates the `chs` array up to the length stored in `u8_`.
                ///Updates `u4` as the character index. If all characters are read, constructs `str`.
                ///</summary>
                ///<returns>True if the complete string has been read and `str` is set; false if more bytes are needed for characters.</returns>
                private bool getting_string()
                {
                    while (u4 < u8_)
                    {
                        u8 = 0;
                        bytes_left = 0;
                        if (varint())
                            chs![u4++] = (char)u8;
                        else
                            return false;
                    }

                    str = new string(chs!, 0, (int)u4);
                    return true;
                }
                #endregion

                public override string ToString()
                {
                    if (slot == null)
                        return "";

                    var s = slot;
                    while (s.prev != null)
                        s = s.prev;
                    var str = "";
                    var offset = "";
                    for (; s != slot; s = s.next, offset += "\t")
                        str += $"{offset}{s.dst.GetType()}\t{s.state}\n";

                    str += $"{offset}{slot.dst.GetType()}\t{s.state}\n";

                    return str;
                }
            }

            ///<summary>
            ///Abstract base class for data transmitters. It manages the state for serializing
            ///structured data to a byte stream. It extends `Base.Transmitter` and implements `BytesSrc`
            ///(to provide the serialized bytes).
            ///</summary>
            public abstract class Transmitter : Base.Transmitter, BytesSrc
            {
                ///<summary>
                ///Internal interface used by `Transmitter` to interact with message-specific serialization logic.
                ///</summary>
                public interface BytesSrc
                {
                    ///<summary>
                    ///Internal method called by the `Transmitter` to serialize data for a specific message type.
                    ///Implementers should write to the `Transmitter`'s buffer using its `put_` methods.
                    ///</summary>
                    ///<param name="dst">The `Transmitter` instance providing the byte stream and serialization context.</param>
                    ///<returns>True if the message part was successfully serialized with available buffer space; false if more buffer space is needed or processing is otherwise incomplete.</returns>
                    bool __get_bytes(Transmitter dst);

                    ///<summary>
                    ///Gets the unique identifier for this message type or data structure.
                    ///</summary>
                    int __id { get; }
                }

                ///<summary>
                ///Enumerates possible error types that can occur during reception.
                ///</summary>
                public enum OnError
                {
                    ///<summary> Error code: Transmitting packet is rejected by dataflow. </summary>
                    REJECTED = 0,

                    ///<summary>Indicates a buffer overflow, typically when a declared length exceeds buffer capacity or a predefined maximum.</summary>
                    OVERFLOW = 1,

                    ///Error code indicating a timeout during packet transmission or reception.
                    TIMEOUT = 2,

                    ///Generic error code for unspecified or unexpected errors during packet processing.
                    ERROR = 3
                }

                ///<summary>
                ///The default error handler for `BytesSrc` related errors (currently only `OnError.OVERFLOW`).
                ///</summary>
                public static OnErrorHandler error_handler = OnErrorHandler.DEFAULT;

                ///<summary>
                ///Defines an interface for handling errors that occur in a `BytesSrc` context.
                ///</summary>
                public interface OnErrorHandler
                {
                    ///<summary>
                    ///A default `OnErrorHandler` implementation that logs errors to the console.
                    ///</summary>
                    static OnErrorHandler DEFAULT = new ToConsole();

                    ///<summary>
                    ///A concrete `OnErrorHandler` that writes error information to the console.
                    ///</summary>
                    class ToConsole : OnErrorHandler
                    {
                        ///<summary>
                        ///Handles a `BytesSrc` error by printing details to the console.
                        ///</summary>
                        ///<param name="src">The `AdHoc.BytesSrc` where the error occurred.</param>
                        ///<param name="error">The type of error .</param>
                        ///<param name="ex">An optional exception associated with the error.</param>
                        public void error(AdHoc.BytesSrc src, OnError error, Exception? ex = null)
                        {
                            switch (error)
                            {
                                case OnError.OVERFLOW:
                                    Console.WriteLine("OVERFLOW at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                                case OnError.REJECTED:
                                    Console.WriteLine("REJECTED at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                                default:
                                    Console.WriteLine("Error at " + src + (ex == null ? "" : ex + Environment.StackTrace));
                                    return;
                            }
                        }
                    }

                    ///<summary>
                    ///Handles a `BytesSrc` error.
                    ///</summary>
                    ///<param name="src">The `AdHoc.BytesSrc` where the error occurred.</param>
                    ///<param name="error">The type of error.</param>
                    ///<param name="ex">An optional exception associated with the error.</param>
                    void error(AdHoc.BytesSrc src, OnError error, Exception? ex = null);
                }

                ///<summary>
                ///Defines an interface for handling events generated by a `Transmitter`.
                ///</summary>
                public interface EventsHandler
                {
                    ///<summary>
                    ///Called just before a packet's serialization process begins via its `BytesSrc` handler.
                    ///</summary>
                    ///<param name="dst">The `Transmitter` instance that is about to send the packet.</param>
                    ///<param name="src">The `BytesSrc` instance representing the packet to be serialized.</param>
                    void OnSerializing(Transmitter dst, BytesSrc src) { }

                    ///<summary>
                    ///Called after a complete packet has been successfully serialized and (notionally) sent
                    ///from the internal perspective of the `Transmitter`'s processing loop.
                    ///The actual transmission to an external sink depends on the `Transmitter.Read` method being called.
                    ///</summary>
                    ///<param name="dst">The `Transmitter` instance that has completed sending the packet.</param>
                    ///<param name="src">The `BytesSrc` instance representing the packet that was sent.</param>
                    void OnSerialized(Transmitter dst, BytesSrc src) { }
                }

                ///<summary>
                ///The current event handler for this transmitter.
                ///</summary>
                protected EventsHandler? handler;

                ///<summary>
                ///Atomically exchanges the current event handler with a new one.
                ///</summary>
                ///<param name="handler">The new event handler to set.</param>
                ///<returns>The event handler that was previously set.</returns>
                public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

                ///<summary>
                ///Initializes a new instance of the <see cref="org.unirail.AdHoc.Transmitter.Transmitter"/> class.
                ///</summary>
                ///<param name="handler">The event handler for transmitter events.</param>
                ///<param name="power_of_2_sending_queue_size">The size of the internal sending queues, expressed as a power of 2 (e.g., 5 for a size of 2^5=32). Default is 5.</param>
                public Transmitter(EventsHandler handler, int power_of_2_sending_queue_size = 5) : base(power_of_2_sending_queue_size)
                {
                    this.handler = handler;
                    slot_ref = new WeakReference<Slot>(new Slot(this, null));
                }

                private Action<AdHoc.BytesSrc>? subscriber;

                public Action<AdHoc.BytesSrc>? subscribeOnNewBytesToTransmitArrive(Action<AdHoc.BytesSrc>? subscriber)
                {
                    var tmp = this.subscriber;
                    this.subscriber = subscriber;
                    notify_subscribers();
                    return tmp;
                }

                protected void notify_subscribers()
                {
                    if (subscriber != null && IsIdle())
                        subscriber!.Invoke(this);
                } //Notify subscriber if pending bytes exist
                #region sending
                /** Acquires the sending lock using a spin-wait. */
                protected void sending_lock_acquire()
                {
                    while (Interlocked.CompareExchange(ref Lock, 1, 0) != 0)
                        Thread.SpinWait(10);
                }

                protected void sending_lock_release() { Lock = 0; }

                //do not forget to  set u8 = sending_out.value;
                protected abstract BytesSrc? _OnSerializing();
                protected abstract void _OnSerialized(BytesSrc bytes);

                public abstract bool IsIdle();

                private volatile int Lock;
                #endregion
                #region value_pack transfer
                ///<summary>
                ///Sets `u8` to `src`, then calls `put_bytes(handler, next_case)` to serialize a nested message.
                ///This is used when `src` itself is the value to be serialized by `handler`.
                ///</summary>
                ///<param name="src">The `ulong` value to be effectively passed to or used by the `handler`.</param>
                ///<param name="handler">The `BytesSrc` handler for the nested message part.</param>
                ///<param name="next_case">The state to retry at if serialization by `handler` requires more buffer space.</param>
                ///<returns>True if `handler` completed serialization successfully; false if a retry is needed.</returns>
                public bool put_bytes(ulong src, BytesSrc handler, uint next_case)
                {
                    u8 = src;
                    return put_bytes(handler, next_case);
                }

                ///<summary>
                ///Sets `u8` to `src`, then calls `put_bytes(handler)` to serialize a nested message.
                ///This is used when `src` itself is the value to be serialized by `handler`, assuming no retry is needed.
                ///</summary>
                ///<param name="src">The `ulong` value to be effectively passed to or used by the `handler`.</param>
                ///<param name="handler">The `BytesSrc` handler for the nested message part.</param>
                public void put_bytes(ulong src, BytesSrc handler)
                {
                    u8 = src;
                    put_bytes(handler);
                }

                ///<summary>
                ///Invokes the `__get_bytes` method on the provided `BytesSrc` instance `src`
                ///to serialize its data using the current transmitter context.
                ///Assumes `src` is part of the current serialization slot, skipping packet ID writing.
                ///</summary>
                ///<param name="src">The `BytesSrc` instance to serialize.</param>
                public void put_bytes(BytesSrc src)
                {
                    slot!.state = 1; //skip write id
                    src.__get_bytes(this);
                }

                ///<summary>
                ///Attempts to serialize a nested message represented by `src`.
                ///Sets up a new slot for `src`, then invokes its `__get_bytes` method.
                ///If `src` cannot complete serialization due to insufficient buffer space,
                ///the transmitter's state is set to `next_case` for retry.
                ///</summary>
                ///<param name="src">The `BytesSrc` handler for the nested message part.</param>
                ///<param name="next_case">The state to retry at if serialization by `src` requires more buffer space.</param>
                ///<returns>True if `src` completed serialization successfully; false if a retry is needed.</returns>
                public bool put_bytes(BytesSrc src, uint next_case)
                {
                    var s = slot;

                    (slot = s.next ?? (s.next = new Slot(this, s))).src = src;
                    slot.state = 1; //skip write id

                    if (src.__get_bytes(this))
                    {
                        slot = s;
                        return true;
                    }

                    s.state = next_case;
                    return false;
                }
                #endregion

                ///<summary>
                ///Implements a framing encoder that handles a byte-oriented protocol with 0xFF as a frame start marker,
                ///0x7F-based escaping for 0x7F and 0xFF bytes within the payload, and a 2-byte CRC.
                ///Frame structure: 0xFF (start marker) + encoded_payload + 2_byte_CRC.
                ///It takes raw packet data from an `upper_layer` Transmitter, encodes it, and provides the framed data via its `Read` method.
                ///</summary>
                public class Framing : AdHoc.BytesSrc, EventsHandler
                {
                    public bool isOpen() => upper_layer.isOpen(); //Check if the upper layer is open

                    ///<summary>
                    ///The `Transmitter` instance from which raw, unframed packet data is read.
                    ///</summary>
                    public Transmitter upper_layer; //Handles transmission of encoded frames

                    ///<summary>
                    ///Handles events for the encoding process, typically forwarding them from the original handler of the `upper_layer` before it was hooked by this framing instance.
                    ///</summary>
                    public EventsHandler handler; //Encoding process event handler

                    ///<summary>
                    ///Atomically exchanges the current event handler with a new one.
                    ///</summary>
                    ///<param name="handler">New event handler</param>
                    ///<returns>Previous event handler</returns>
                    public EventsHandler exchange(EventsHandler handler) => Interlocked.Exchange(ref this.handler, handler);

                    ///<summary>
                    ///Initializes a new instance of the <see cref="Framing"/> class.
                    ///</summary>
                    ///<param name="upper_layer">The `Transmitter` instance that will provide the raw packets to be framed.</param>
                    public Framing(Transmitter upper_layer) => switch_to(upper_layer);

                    ///<summary>
                    ///Switches the framing layer to operate on a new `upper_layer` transmitter.
                    ///This also resets the internal state of the framing encoder and hooks its event handling into the new `upper_layer`.
                    ///</summary>
                    ///<param name="upper_layer">New `Transmitter` instance to provide raw packets.</param>
                    public void switch_to(Transmitter upper_layer)
                    {
                        bits = 0;
                        shift = 0;
                        crc = 0;
                        if (this.upper_layer != null)
                        {
                            this.upper_layer.Reset();
                            this.upper_layer.exchange(handler);
                        }

                        handler = (this.upper_layer = upper_layer).exchange(this);
                    }

                    private int enc_position; //Current position for writing encoded bytes
                    private int raw_position; //Current position for reading raw input bytes

                    ///<summary>
                    ///Estimates the required space in the output buffer (`dst` in `Read` method) to hold
                    ///a segment of raw data after encoding, including frame markers and CRC.
                    ///This is used to determine how much raw data can be safely read from the `upper_layer`.
                    ///</summary>
                    ///<param name="limit">The total available size of the current output buffer segment.</param>
                    ///<returns>True if sufficient space is estimated to be available in the output buffer for reading more raw data and encoding it; false otherwise.</returns>
                    private bool allocate_raw_bytes_space(int limit)
                    {
                        //Divide free space.
                        raw_position = enc_position +
                                       1 +                          //Space for 0xFF frame start marker
                                       (limit - enc_position) / 8 + //Worst case byte expansion for payload (approx)
                                       CRC_LEN_BYTES + 2;           //CRC plus possible expansion (max 2 extra bytes for 0x7F encoding of CRC)

                        return raw_position < limit;
                    }

                    public void Close()
                    {
                        Reset();
                        upper_layer.Close();
                    }

                    ///<summary>
                    ///Resets the internal state of the framing encoder and the `upper_layer` transmitter.
                    ///This prepares it for a new sequence of packets or a new connection.
                    ///</summary>
                    protected void Reset()
                    {
                        upper_layer.Reset();
                        bits = 0;
                        shift = 0;
                        crc = 0;
                    }

                    ///<summary>
                    ///Reads raw packet data from the `upper_layer`, encodes it using the framing protocol
                    ///(including 0x7F escaping and CRC calculation), and writes the resulting framed bytes into `dst`.
                    ///</summary>
                    ///<param name="dst">The destination buffer for the encoded, framed data.</param>
                    ///<param name="dst_byte">The starting offset in `dst` to write to.</param>
                    ///<param name="dst_bytes">The maximum number of bytes to write to `dst`.</param>
                    ///<returns>
                    ///The number of bytes written to `dst`.
                    ///Returns 0 if `dst` has insufficient space.
                    ///Returns -1 if the `upper_layer` has no more raw packets to provide.
                    ///</returns>
                    public int Read(byte[] dst, int dst_byte, int dst_bytes)
                    {
                        enc_position = dst_byte;
                        var limit = dst_byte + dst_bytes;

                        while (allocate_raw_bytes_space(limit))
                        {
                            var fix = raw_position;
                            var len = upper_layer.Read(dst, raw_position, limit - raw_position);

                            if (len < 1)
                                return dst_byte < enc_position ? enc_position - dst_byte : len;

                            for (var max = fix + len; raw_position < max;)
                                enc_position = encode(dst[raw_position++], dst, enc_position);
                        }

                        return dst_byte < enc_position ? enc_position - dst_byte : 0;
                    }

                    public void OnSerializing(Transmitter dst, BytesSrc src)
                    {
                        handler?.OnSerializing(dst, src);
                        dst.buffer![enc_position++] = 0xFF; //write starting frame byte
                    }

                    ///<summary>
                    ///Handles the `OnSerialized` event from the `upper_layer` transmitter.
                    ///This signifies that a complete raw packet has been provided by the `upper_layer`.
                    ///This method encodes any remaining bytes of that packet, then encodes and appends the CRC checksum.
                    ///It finalizes any bit-level encoding for the frame and resets its state for the next packet.
                    ///</summary>
                    ///<param name="dst">The `upper_layer` transmitter instance.</param>
                    ///<param name="src">The `BytesSrc` instance of the packet that was just completed by `upper_layer`.</param>
                    public void OnSerialized(Transmitter dst, BytesSrc src)
                    {
                        while (raw_position < dst.byte_)
                            enc_position = encode(dst.buffer![raw_position++], dst.buffer, enc_position);

                        //the packet sending completed write crc
                        int fix = crc; //crc will continue counting on  encode() calling , so fix it
                        enc_position = encode(fix & 0xFF, dst.buffer!, encode(fix >> 8 & 0xFF, dst.buffer!, enc_position));
                        if (0 < shift)
                        {
                            dst.buffer![enc_position++] = (byte)bits;
                            if (bits == 0x7F)
                                dst.buffer![enc_position++] = 0;
                        }

                        if (allocate_raw_bytes_space(dst.byte_max))
                            dst.byte_ = raw_position;
                        else
                            dst.byte_max = raw_position = dst.byte_; //no more space. prevent continue

                        bits = 0;
                        shift = 0;
                        crc = 0;
                        handler?.OnSerialized(dst, src);
                    }

                    ///<summary>
                    ///Encodes a single source byte into the destination buffer, applying 0x7F-based escaping
                    ///and updating the running CRC. Manages a bit accumulator for byte alignment.
                    ///</summary>
                    ///<param name="src">The source byte to encode.</param>
                    ///<param name="dst">The destination buffer for encoded bytes.</param>
                    ///<param name="dst_byte">The current write position in `dst`.</param>
                    ///<returns>The updated write position in `dst` after encoding the byte.</returns>
                    private int encode(int src, byte[] dst, int dst_byte)
                    {
                        crc = crc16((byte)src, crc);
                        var v = (bits |= src << shift) & 0xFF;
                        if ((v & 0x7F) == 0x7F)
                        {
                            dst[dst_byte++] = 0x7F;
                            bits >>= 7;
                            if (shift < 7)
                                shift++;
                            else //a full byte in enc_bits
                            {
                                if ((bits & 0x7F) == 0x7F)
                                {
                                    dst[dst_byte++] = 0x7F;
                                    bits >>= 7;
                                    shift = 1;
                                    return dst_byte;
                                }

                                dst[dst_byte++] = (byte)bits;
                                shift = 0;
                                bits = 0;
                            }

                            return dst_byte;
                        }

                        dst[dst_byte++] = (byte)v;
                        bits >>= 8;
                        return dst_byte;
                    }

                    public Action<AdHoc.BytesSrc>? subscribeOnNewBytesToTransmitArrive(Action<AdHoc.BytesSrc>? subscriber) => upper_layer.subscribeOnNewBytesToTransmitArrive(subscriber);

                    //Internal state variables
                    private int bits;   //Accumulator for bits during 0x7F-based encoding.
                    private int shift;  //Current bit shift within the `bits` accumulator.
                    private ushort crc; //Running CRC16 checksum for the current frame's payload.
                }
                #region Slot
                ///<summary>
                ///Internal class representing a slot in the transmitter's processing chain, typically for nested messages.
                ///It extends `Base.Transmitter.Slot` and holds message-specific state.
                ///</summary>
                internal sealed class Slot : Base.Transmitter.Slot
                {
                    ///<summary>
                    ///The `BytesSrc` instance responsible for serializing the current message or part of a message associated with this slot.
                    ///</summary>
                    internal BytesSrc src;

                    public BytesSrc src_(BytesSrc src)
                    {
                        state = 1;
                        return this.src = src;
                    }

                    ///<summary>
                    ///A bitmask indicating which fields of a message are null. Used by some serialization schemes.
                    ///</summary>
                    internal int fields_nulls;

                    internal Slot? next;
                    internal readonly Slot? prev;

                    ///<summary>
                    ///Initializes a new instance of the <see cref="Slot"/> class.
                    ///</summary>
                    ///<param name="src">The `Transmitter` associated with this slot chain.</param>
                    ///<param name="prev">The previous slot in the chain, or null if this is the root slot.</param>
                    public Slot(Transmitter src, Slot? prev) : base(src)
                    {
                        this.prev = prev;
                        if (prev != null)
                            prev.next = this;
                    }
                }

                internal WeakReference<Slot> slot_ref;
                internal Slot? slot;
                #endregion

                ///<summary>
                ///Initializes the `fields_nulls` bitmask in the current slot with a starting bit.
                ///Also allocates 1 byte in the buffer for storing this bitmask later via `flush_fields_nulls`.
                ///</summary>
                ///<param name="field0_bit">The initial bit value for the nulls bitmask (e.g., representing the first field).</param>
                ///<param name="this_case">The state to retry at if buffer allocation fails.</param>
                ///<returns>True if allocation is successful and bitmask initialized; false if a retry is needed.</returns>
                public bool init_fields_nulls(int field0_bit, uint this_case)
                {
                    if (!Allocate(1, this_case))
                        return false;
                    slot!.fields_nulls = field0_bit;
                    return true;
                }

                ///<summary>
                ///Sets a specific bit in the `fields_nulls` bitmask of the current slot.
                ///This is used to mark a field as non-null (by convention, a set bit means non-null).
                ///</summary>
                ///<param name="field">The bit corresponding to the field to mark (e.g., a power of 2).</param>
                public void set_fields_nulls(int field) { slot!.fields_nulls |= field; }

                ///<summary>
                ///Writes the `fields_nulls` bitmask from the current slot as a single byte into the buffer.
                ///Assumes space was previously allocated by `init_fields_nulls`.
                ///</summary>
                public void flush_fields_nulls() { put((byte)slot!.fields_nulls); }

                ///<summary>
                ///Checks if a specific field is marked as null based on the `fields_nulls` bitmask in the current slot.
                ///</summary>
                ///<param name="field">The bit representing the field to check (e.g., a power of 2).</param>
                ///<returns>True if the field's corresponding bit is NOT set in `fields_nulls` (meaning it is null, by convention where 0 means null); false otherwise.</returns>
                public bool is_null(int field) => (slot!.fields_nulls & field) == 0;

                public bool isOpen() => slot != null;

                public virtual void Close() => Reset();

                ///<summary>
                ///Resets the transmitter's internal state, including clearing any active slots, sending queues, buffers,
                ///and resetting processing modes. This prepares the transmitter for a new connection or sequence of packets.
                ///</summary>
                protected virtual void Reset()
                {
                    if (slot == null)
                        return;

                    for (var s = slot; s != null; s = s.next)
                        s.src = null;
                    slot = null;
                    buffer = null;
                    mode = OK;
                    u4 = 0;
                    bytes_left = 0; //requires correct bitwise sending
                }

                //if dst == null - clean / reset state (Note: dst is never null in this implementation's usage)
                //
                //if 0 < return - bytes read (i.e., bytes written to dst)
                //if return == 0 - not enough space in provided buffer dst available
                //if return == -1 -  no more packets left
                ///<summary>
                ///Reads data from the internal sending queue, processes it through the current transmitter slot's
                ///serialization logic (if any is active or a new one is started), and writes the serialized output bytes
                ///into the provided `dst` buffer. Invokes `OnSerializing` and `OnSerialized` events as packets are processed.
                ///This method is the core of the `BytesSrc` implementation for `Transmitter`.
                ///</summary>
                ///<param name="dst">The destination byte array where serialized data will be written.</param>
                ///<param name="dst_byte">The starting offset in `dst`.</param>
                ///<param name="dst_bytes">The maximum number of bytes to write into `dst`.</param>
                ///<returns>
                ///The number of bytes written to `dst`.
                ///Returns 0 if `dst` has insufficient space to write even a part of the next packet segment.
                ///Returns -1 if there are no more packets in the sending queue to process.
                ///</returns>
                public int Read(byte[] dst, int dst_byte, int dst_bytes)
                {
                    if (dst_bytes < 1)
                        return 0;

                    for (buffer = dst, byte_max = (byte_ = dst_byte) + dst_bytes; byte_ < byte_max;)
                    {
                        if (slot?.src == null)
                        {
                            u4 = 0; //used by value packs
                            u8 = 0;

                            var src = _OnSerializing(); //Get next BytesSrc from sending queue

                            if (src == null)
                            {
                                Reset();
                                goto exit;
                            }

                            if (slot == null && !slot_ref.TryGetTarget(out slot))
                                slot_ref = new WeakReference<Slot>(slot = new Slot(this, null));

                            slot.src = src;
                            slot.state = 0; //write id request
                            bytes_left = 0;
                            handler?.OnSerializing(this, src);
                            if (slot == null)
                                return -1; //sending event handler has reset this
                        }
                        else
                            switch (mode) //the packet transmission was interrupted, recall where we stopped
                            {
                                case STR:
                                    if (!varint())
                                        goto exit;

                                    if (u4 == uint.MaxValue)
                                        u4 = 0;

                                    while (u4 < str!.Length)
                                        if (!varint(str[(int)u4++]))
                                            goto exit;

                                    str = null;
                                    break;
                                case VAL4:
                                    if (byte_max - byte_ < bytes_left)
                                        goto exit;
                                    put_val(u4, bytes_left);
                                    break;
                                case VAL8:
                                    if (byte_max - byte_ < bytes_left)
                                        goto exit;
                                    put_val(u8, bytes_left);
                                    break;
                                case BITS_BYTES:
                                    if (byte_max - byte_ < bits_transaction_bytes_)
                                        goto exit;     //space for one full transaction
                                    bits_byte = byte_; //preserve space for bits info
                                    byte_++;
                                    put_val(u8, bytes_left);
                                    break;
                                case VARINT:
                                    if (varint())
                                        break;
                                    goto exit;
                                case BITS:
                                    if (byte_max - byte_ < bits_transaction_bytes_)
                                        goto exit;     //space for one full transaction
                                    bits_byte = byte_; //preserve space for bits info
                                    byte_++;
                                    break;
                            }

                        mode = OK; //Restore mode to OK after resuming transmission

                        //Get bytes from byte source and put into buffer
                        for (; ; )
                            if (!slot!.src!.__get_bytes(this))
                                goto exit;
                            else
                            {
                                if (slot.prev == null)
                                    break;
                                slot = slot.prev;
                            }

                        _OnSerialized(slot.src);
                        handler?.OnSerialized(this, slot.src);
                        if (slot == null)
                            return -1;   //sent event handler has reset this
                        slot.src = null; //sing of the request next packet
                    }

                    if (slot != null && slot.src == null)
                        slot = null;

                    exit:
                    buffer = null;
                    return dst_byte < byte_ ? byte_ - dst_byte : -1; //no more packets left
                }

                ///<summary>
                ///Checks if a specified number of bytes can be allocated in the current output buffer.
                ///If not, sets the transmitter to retry mode at `this_case`.
                ///</summary>
                ///<param name="bytes">The number of bytes required in the buffer.</param>
                ///<param name="this_case">The state to set for retry if allocation fails due to insufficient space.</param>
                ///<returns>True if `bytes` are available in the remaining buffer; false if not and retry mode is set.</returns>
                public bool Allocate(uint bytes, uint this_case)
                {
                    slot!.state = this_case;
                    if (bytes <= remaining)
                        return true;
                    mode = RETRY;
                    return false;
                }

                ///<summary>
                ///Puts a boolean value into the buffer, encoded as a single bit (1 for true, 0 for false),
                ///using the bitwise serialization mechanism.
                ///</summary>
                ///<param name="src">The boolean value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(bool src) => put_bits(src ? 1 : 0, 1);

                ///<summary>
                ///Puts a nullable boolean value into the buffer, encoded as two bits:
                ///00 for null, 10 for false, 11 for true.
                ///Uses the bitwise serialization mechanism.
                ///</summary>
                ///<param name="src">The nullable boolean value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(bool? src) => put_bits(src.HasValue ? src.Value ? 3 : //11 binary
                                                                          2
                                                                    : //10 binary
                                                           0,
                                                       2); //00 binary
                #region bits
                private int bits_byte = -1;
                private uint bits_transaction_bytes_;

                ///<summary>
                ///Initializes a bitwise transaction, ensuring `transaction_bytes` are available.
                ///This version is typically called to *continue* a bitwise operation after a retry.
                ///</summary>
                ///<param name="transaction_bytes">Minimum number of bytes that should be available in the buffer for this transaction segment.</param>
                ///<param name="this_case">The state to retry at if `transaction_bytes` are not available.</param>
                ///<returns>True if sufficient bytes are available; false if a retry is needed (mode set to BITS).</returns>
                public bool init_bits_(uint transaction_bytes, uint this_case)
                {
                    if ((bits_transaction_bytes_ = transaction_bytes) <= byte_max - byte_)
                        return true;
                    slot!.state = this_case;
                    byte_ = bits_byte; //trim byte at bits_byte index
                    mode = BITS;
                    return false;
                }

                ///<summary>
                ///Initializes a bitwise transaction. Allocates one byte in the buffer to start accumulating bits
                ///and ensures at least `transaction_bytes` are available for the whole operation.
                ///Resets `bits` accumulator and `bit` pointer.
                ///</summary>
                ///<param name="transaction_bytes">Minimum number of bytes that should be available in the buffer for this transaction. This includes the initial byte allocated here.</param>
                ///<param name="this_case">The state to retry at if `transaction_bytes` are not available.</param>
                ///<returns>True if sufficient bytes are available and bitwise context is initialized; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool init_bits(uint transaction_bytes, uint this_case)
                {
                    if (byte_max - byte_ < (bits_transaction_bytes_ = transaction_bytes))
                    {
                        slot.state = this_case;
                        mode = RETRY;
                        return false;
                    }

                    bits = 0;
                    bit = 0;
                    bits_byte = byte_++; //Allocate space for the first byte of bits
                    return true;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put_bits(bool src) => put_bits(src ? 1 : 0, 1);

                ///<summary>
                ///Puts a specified number of bits from `src` into the bit accumulator (`bits`).
                ///If the accumulator fills a byte, that byte is written to `buffer[bits_byte]`,
                ///`bits_byte` is advanced, and the accumulator is updated.
                ///</summary>
                ///<param name="src">The integer containing the bits to put (in its lower `len_bits`).</param>
                ///<param name="len_bits">The number of bits from `src` to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put_bits(int src, int len_bits)
                {
                    bits |= (uint)src << bit;
                    if ((bit += len_bits) < 9)
                        return; //yes 9! not 8!  to avoid allocating the next byte after the current one is full. it might be redundant
                    buffer![bits_byte] = (byte)bits;
                    bits >>= 8;
                    bit -= 8;
                    bits_byte = byte_++;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put_bits(bool src, uint continue_at_case) => put_bits(src ? 1 : 0, 1, continue_at_case);

                ///<summary>
                ///Puts a specified number of bits from `src` into the bit accumulator, with retry capability.
                ///If writing a filled byte requires more buffer space than `bits_transaction_bytes_` allows from current `byte_`,
                ///it sets up a retry.
                ///</summary>
                ///<param name="src">The integer containing the bits to put.</param>
                ///<param name="len_bits">The number of bits from `src` to put.</param>
                ///<param name="continue_at_case">The state to retry at if buffer space runs out.</param>
                ///<returns>True if bits were put successfully (or partially, with more buffer available); false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put_bits(int src, int len_bits, uint continue_at_case)
                {
                    bits |= (uint)src << bit;
                    if ((bit += len_bits) < 9)
                        return true; //yes 9! not 8!  to avoid allocating the next byte after the current one is full. it might be redundant
                    buffer![bits_byte] = (byte)bits;
                    bits >>= 8;
                    bit -= 8;
                    if (byte_max - byte_ < bits_transaction_bytes_)
                    {
                        slot!.state = continue_at_case;
                        return false;
                    }

                    bits_byte = byte_++;
                    return true;
                }

                ///<summary>
                ///Finalizes the current bitwise transaction. If any bits are remaining in the `bits` accumulator
                ///(i.e., `0 ᐸ bit`), they are written to `buffer[bits_byte]`. If no bits were written to the
                ///current `bits_byte` (i.e. `bit == 0`), `byte_` is trimmed back to `bits_byte`, effectively
                ///de-allocating the unused byte.
                ///</summary>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void end_bits()
                {
                    if (0 < bit)
                        buffer![bits_byte] = (byte)bits;
                    else
                        byte_ = bits_byte; //trim byte at bits_byte index. allocated, but not used
                }

                ///<summary>
                ///Puts a nulls bitmask using `put_bits`. If `put_bits` requires a retry,
                ///this method sets the mode to `BITS` for continuation.
                ///</summary>
                ///<param name="nulls">The nulls bitmask value.</param>
                ///<param name="nulls_bits">The number of bits in the `nulls` bitmask.</param>
                ///<param name="continue_at_case">The state to retry at if putting bits fails.</param>
                ///<returns>True if the nulls bitmask was put successfully; false if a retry is needed.</returns>
                public bool put_nulls(int nulls, int nulls_bits, uint continue_at_case)
                {
                    if (put_bits(nulls, nulls_bits, continue_at_case))
                        return true;

                    mode = BITS;
                    return false;
                }

                ///<summary>
                ///Sets up a retry for a bitwise operation that was interrupted.
                ///Resets `byte_` to `bits_byte` (start of current bit-holding byte) and sets mode to `BITS`
                ///and state to `continue_at_case`.
                ///</summary>
                ///<param name="continue_at_case">The state to resume the bitwise operation at.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void continue_bits_at(uint continue_at_case)
                {
                    slot!.state = continue_at_case;
                    byte_ = bits_byte;
                    mode = BITS;
                }
                #endregion

                public bool put_bits_bytes(int info, int info_bits, double value, int value_bytes, uint continue_at_case) => put_bits_bytes(info, info_bits, Unsafe.As<double, ulong>(ref value), value_bytes, continue_at_case);
                public bool put_bits_bytes(int info, int info_bits, float value, int value_bytes, uint continue_at_case) => put_bits_bytes(info, info_bits, Unsafe.As<float, uint>(ref value), value_bytes, continue_at_case);

                ///<summary>
                ///Puts a sequence of `info_bits` followed by `value_bytes` representing `value`.
                ///This is a common pattern for length-prefixed or type-prefixed fields.
                ///If `put_bits` for `info` requires retry, sets mode to `BITS_BYTES` and stores `value` and `value_bytes`
                ///in `u8` and `bytes_left` respectively for continuation.
                ///</summary>
                ///<param name="info">The integer containing the information bits (e.g., length or type).</param>
                ///<param name="info_bits">The number of bits to take from `info`.</param>
                ///<param name="value">The `ulong` value to write after the info bits.</param>
                ///<param name="value_bytes">The number of bytes to use for serializing `value`.</param>
                ///<param name="continue_at_case">The state to retry at if operation fails.</param>
                ///<returns>True if both info bits and value bytes were put successfully; false if a retry is needed.</returns>
                public bool put_bits_bytes(int info, int info_bits, ulong value, int value_bytes, uint continue_at_case)
                {
                    if (put_bits(info, info_bits, continue_at_case))
                    {
                        put_val(value, value_bytes);
                        return true;
                    }

                    u8 = value;
                    bytes_left = value_bytes;
                    mode = BITS_BYTES;
                    return false;
                }
                #region varint
                private static int bytes1(ulong src) => src < 1 << 8 ? 1 : 2;

                ///<summary>
                ///Puts a varint-like value `src` using 1 bit for length (0 for 1 byte, 1 for 2 bytes)
                ///followed by the value itself (1 or 2 bytes).
                ///Uses `put_bits_bytes` for combined bit and byte writing.
                ///</summary>
                ///<param name="src">The ulong value to encode (expected to fit in 1 or 2 bytes).</param>
                ///<param name="continue_at_case">The state to retry at if writing fails.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint21(ulong src, uint continue_at_case)
                {
                    var bytes = bytes1(src);
                    return put_bits_bytes(bytes - 1, 1, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` (1 or 2 bytes) prefixed by `nulls_bits` and 1 bit for length.
                ///The length bit is 0 for 1 byte payload, 1 for 2 bytes payload.
                ///</summary>
                ///<param name="src">The ulong value to encode.</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<param name="nulls">The nulls bitmask.</param>
                ///<param name="nulls_bits">Number of bits in the nulls bitmask.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint21(ulong src, uint continue_at_case, int nulls, int nulls_bits)
                {
                    var bytes = bytes1(src);
                    return put_bits_bytes(bytes - 1 << nulls_bits | nulls, nulls_bits + 1, src, bytes, continue_at_case);
                }

                private static int bytes2(ulong src) => src < 1 << 8 ? 1
                                                        : src < 1 << 16 ? 2
                                                                        : 3;

                ///<summary>
                ///Puts a varint-like value `src` using 2 bits for length (1 for 1 byte, 2 for 2 bytes, 3 for 3 bytes)
                ///followed by the value itself (1, 2, or 3 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode (expected to fit in 1-3 bytes).</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint32(ulong src, uint continue_at_case)
                {
                    var bytes = bytes2(src);
                    return put_bits_bytes(bytes, 2, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` (1-3 bytes) prefixed by `nulls_bits` and 2 bits for length.
                ///Length bits are actual byte count (1, 2, or 3).
                ///</summary>
                ///<param name="src">The ulong value to encode.</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<param name="nulls">The nulls bitmask.</param>
                ///<param name="nulls_bits">Number of bits in the nulls bitmask.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint32(ulong src, uint continue_at_case, int nulls, int nulls_bits)
                {
                    var bytes = bytes2(src);
                    return put_bits_bytes(bytes << nulls_bits | nulls, nulls_bits + 2, src, bytes, continue_at_case);
                }

                private static int bytes3(ulong src) => src < 1L << 16 ? src < 1L << 8 ? 1 : 2
                                                        : src < 1L << 24 ? 3
                                                                         : 4;

                ///<summary>
                ///Puts a varint-like value `src` using 2 bits for length (0 for 1 byte, ... , 3 for 4 bytes)
                ///followed by the value itself (1 to 4 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode (expected to fit in 1-4 bytes).</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint42(ulong src, uint continue_at_case)
                {
                    var bytes = bytes3(src);
                    return put_bits_bytes(bytes - 1, 2, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` (1-4 bytes) prefixed by `nulls_bits` and 2 bits for length.
                ///Length bits are 0-indexed (0 for 1 byte payload, ..., 3 for 4 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode.</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<param name="nulls">The nulls bitmask.</param>
                ///<param name="nulls_bits">Number of bits in the nulls bitmask.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint42(ulong src, uint continue_at_case, int nulls, int nulls_bits)
                {
                    var bytes = bytes3(src);
                    return put_bits_bytes(bytes - 1 << nulls_bits | nulls, nulls_bits + 2, src, bytes, continue_at_case);
                }

                private static int bytes4(ulong src) => src < 1 << 24 ? src < 1 << 16 ? src < 1 << 8 ? 1 : 2 : 3
                                                        : src < 1L << 32 ? 4
                                                        : src < 1L << 40 ? 5
                                                        : src < 1L << 48 ? 6
                                                                         : 7;

                ///<summary>
                ///Puts a varint-like value `src` using 3 bits for length (actual byte count 1-7)
                ///followed by the value itself (1 to 7 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode (expected to fit in 1-7 bytes).</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint73(ulong src, uint continue_at_case)
                {
                    var bytes = bytes4(src);
                    return put_bits_bytes(bytes, 3, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` (1-7 bytes) prefixed by `nulls_bits` and 3 bits for length.
                ///Length bits are actual byte count (1-7).
                ///</summary>
                ///<param name="src">The ulong value to encode.</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<param name="nulls">The nulls bitmask.</param>
                ///<param name="nulls_bits">Number of bits in the nulls bitmask.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint73(ulong src, uint continue_at_case, int nulls, int nulls_bits)
                {
                    var bytes = bytes4(src);
                    return put_bits_bytes(bytes << nulls_bits | nulls, nulls_bits + 3, src, bytes, continue_at_case);
                }

                private static int bytes5(ulong src) => src < 1L << 32 ? src < 1 << 16 ? src < 1 << 8 ? 1 : 2
                                                                           : src < 1 << 24 ? 3
                                                                                           : 4
                                                        : src < 1L << 48 ? src < 1L << 40 ? 5 : 6
                                                        : src < 1L << 56 ? 7
                                                                         : 8;

                ///<summary>
                ///Puts a varint-like value `src` using 3 bits for length (0 for 1 byte, ..., 7 for 8 bytes)
                ///followed by the value itself (1 to 8 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode (fits in 1-8 bytes).</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint83(ulong src, uint continue_at_case)
                {
                    var bytes = bytes5(src);
                    return put_bits_bytes(bytes - 1, 3, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` (1-8 bytes) prefixed by `nulls_bits` and 3 bits for length.
                ///Length bits are 0-indexed (0 for 1 byte payload, ..., 7 for 8 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode.</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<param name="nulls">The nulls bitmask.</param>
                ///<param name="nulls_bits">Number of bits in the nulls bitmask.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint83(ulong src, uint continue_at_case, int nulls, int nulls_bits)
                {
                    var bytes = bytes5(src);
                    return put_bits_bytes(bytes - 1 << nulls_bits | nulls, nulls_bits + 3, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` using 4 bits for length (actual byte count 1-8, values 9-15 unused or reserved)
                ///followed by the value itself (1 to 8 bytes).
                ///</summary>
                ///<param name="src">The ulong value to encode (fits in 1-8 bytes).</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint84(ulong src, uint continue_at_case)
                {
                    var bytes = bytes5(src);
                    return put_bits_bytes(bytes, 4, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a varint-like value `src` (1-8 bytes) prefixed by `nulls_bits` and 4 bits for length.
                ///Length bits are actual byte count (1-8).
                ///</summary>
                ///<param name="src">The ulong value to encode.</param>
                ///<param name="continue_at_case">The state to retry at.</param>
                ///<param name="nulls">The nulls bitmask.</param>
                ///<param name="nulls_bits">Number of bits in the nulls bitmask.</param>
                ///<returns>True if successful; false if retry is needed.</returns>
                public bool put_varint84(ulong src, uint continue_at_case, int nulls, int nulls_bits)
                {
                    var bytes = bytes5(src);
                    return put_bits_bytes(bytes << nulls_bits | nulls, nulls_bits + 4, src, bytes, continue_at_case);
                }

                ///<summary>
                ///Puts a standard varint-encoded `ulong` value into the buffer.
                ///If not enough buffer space, sets mode to `VARINT` and state to `next_case` for retry,
                ///storing `src` in `u8_` for continuation.
                ///</summary>
                ///<param name="src">The ulong value to varint-encode and put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the varint was put successfully; false if a retry is needed.</returns>
                public bool put_varint(ulong src, uint next_case)
                {
                    if (varint(src))
                        return true;

                    slot!.state = next_case;
                    mode = VARINT;
                    return false;
                }

                ///<summary>
                ///Puts the varint-encoded value currently stored in `u8_` into the buffer.
                ///This is used for retrying a `put_varint` operation.
                ///</summary>
                ///<returns>True if the varint was put successfully; false if more buffer space is needed.</returns>
                private bool varint() => varint(u8_);

                ///<summary>
                ///Writes a `ulong` value into the buffer using standard varint encoding.
                ///Each byte written has its MSB set if more bytes follow; the last byte has MSB clear.
                ///Only 7 bits per byte are used for data.
                ///</summary>
                ///<param name="src">The `ulong` value to encode and write.</param>
                ///<returns>True if the entire varint value was written to the buffer; false if the buffer ran out of space, in which case `u8_` stores the remaining part of `src` to be encoded.</returns>
                private bool varint(ulong src)
                {
                    for (; byte_ < byte_max; buffer![byte_++] = (byte)(0x80 | src), src >>= 7)
                        if (src < 0x80)
                        {
                            buffer![byte_++] = (byte)src;
                            return true;
                        }

                    u8_ = src;
                    return false;
                }

                ///<summary>
                ///Encodes a signed long value using ZigZag encoding.
                ///This maps signed integers to unsigned integers so that numbers with a small absolute value
                ///(i.e., close to zero, positive or negative) have a small varint encoded value.
                ///</summary>
                ///<param name="src">The signed long value to encode.</param>
                ///<param name="right">The number of bits to right-shift `src` in the XOR operation (typically 63 for Int64).</param>
                ///<returns>The ZigZag-encoded unsigned long value.</returns>
                public static ulong zig_zag(long src, int right) => (ulong)(src << 1 ^ src >> right);
                #endregion

                ///<summary>
                ///Puts a `ulong` value `src` into the buffer using a fixed number of `bytes`.
                ///If not enough buffer space, sets up a retry.
                ///</summary>
                ///<param name="src">The `ulong` value to put.</param>
                ///<param name="bytes">The number of bytes to use for `src` (1 to 8).</param>
                ///<param name="next_field_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put_val(ulong src, int bytes, uint next_field_case)
                {
                    if (remaining < bytes)
                    {
                        put(src, bytes, next_field_case);
                        return false;
                    }

                    put_val(src, bytes);
                    return true;
                }

                ///<summary>
                ///Puts a `ulong` value `src` into the buffer using a fixed number of `bytes`,
                ///respecting endianness. Advances buffer position.
                ///Assumes sufficient space is available.
                ///</summary>
                ///<param name="src">The `ulong` value to put.</param>
                ///<param name="bytes">The number of bytes to use for `src` (1 to 8).</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put_val(ulong src, int bytes)
                {
                    byte_ += bytes;
                    switch (bytes)
                    {
                        case 8:
                            Endianness.OK.UInt64(src, buffer!, byte_ - 8);
                            return;
                        case 7:
                            Endianness.OK.UInt32((uint)src, buffer!, byte_ - 7);
                            Endianness.OK.UInt16((ushort)(src >> 32), buffer!, byte_ - 3);
                            buffer![byte_ - 1] = (byte)(src >> 48);
                            return;
                        case 6:
                            Endianness.OK.UInt32((uint)src, buffer!, byte_ - 6);
                            Endianness.OK.UInt16((ushort)(src >> 32), buffer!, byte_ - 2);
                            return;
                        case 5:
                            Endianness.OK.UInt32((uint)src, buffer!, byte_ - 5);
                            buffer![byte_ - 1] = (byte)(src >> 32);
                            return;
                        case 4:
                            Endianness.OK.UInt32((uint)src, buffer!, byte_ - 4);
                            return;
                        case 3:
                            Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 3);
                            buffer![byte_ - 1] = (byte)(src >> 16);
                            return;
                        case 2:
                            Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 2);
                            return;
                        case 1:
                            buffer![byte_ - 1] = (byte)src;
                            return;
                    }
                }

                ///<summary>
                ///Puts a `uint` value `src` into the buffer using a fixed number of `bytes`.
                ///If not enough buffer space, sets up a retry.
                ///</summary>
                ///<param name="src">The `uint` value to put.</param>
                ///<param name="bytes">The number of bytes to use for `src` (1 to 4).</param>
                ///<param name="next_field_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put_val(uint src, int bytes, uint next_field_case)
                {
                    if (remaining < bytes)
                    {
                        put(src, bytes, next_field_case);
                        return false;
                    }

                    put_val(src, bytes);
                    return true;
                }

                ///<summary>
                ///Puts a `uint` value `src` into the buffer using a fixed number of `bytes`,
                ///respecting endianness. Advances buffer position.
                ///Assumes sufficient space is available.
                ///</summary>
                ///<param name="src">The `uint` value to put.</param>
                ///<param name="bytes">The number of bytes to use for `src` (1 to 4).</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put_val(uint src, int bytes)
                {
                    byte_ += bytes;
                    switch (bytes)
                    {
                        case 4:
                            Endianness.OK.UInt32(src, buffer!, byte_ - 4);
                            return;
                        case 3:
                            Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 3);
                            buffer![byte_ - 1] = (byte)(src >> 16);
                            return;
                        case 2:
                            Endianness.OK.UInt16((ushort)src, buffer!, byte_ - 2);
                            return;
                        case 1:
                            buffer![byte_ - 1] = (byte)src;
                            return;
                    }
                }

                ///<summary>
                ///Puts a string into the buffer using varint encoding for its length and for each character.
                ///If not enough buffer space, sets mode to `STR` and state to `next_case` for retry,
                ///storing `src` in the `str` field for continuation.
                ///</summary>
                ///<param name="src">The string to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the string was put successfully; false if a retry is needed.</returns>
                public bool put(string src, uint next_case)
                {
                    u4 = uint.MaxValue; //indicate state before string length send
                    if (!varint((ulong)src.Length))
                        goto exit;
                    u4 = 0; //indicate state after string length sent

                    while (u4 < src.Length)
                        if (!varint(src[(int)u4++]))
                            goto exit;
                    return true;

                exit:
                    slot!.state = next_case;
                    str = src; //switch to sending internally
                    mode = STR;
                    return false;
                }

                ///<summary>
                ///Sets up a retry operation for putting a `uint` value.
                ///Stores `src`, `bytes`, and `next_case` in `u4`, `bytes_left`, and `slot.state` respectively.
                ///Sets mode to `VAL4`.
                ///</summary>
                ///<param name="src">The `uint` value that needs to be written.</param>
                ///<param name="bytes">The number of bytes `src` should occupy (1 to 4).</param>
                ///<param name="next_case">The state to resume at when more buffer space is available.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                private void put(uint src, int bytes, uint next_case)
                {
                    slot!.state = next_case;
                    bytes_left = bytes;
                    u4 = src;
                    mode = VAL4;
                }

                ///<summary>
                ///Sets up a retry operation for putting a `ulong` value.
                ///Stores `src`, `bytes`, and `next_case` in `u8`, `bytes_left`, and `slot.state` respectively.
                ///Sets mode to `VAL8`.
                ///</summary>
                ///<param name="src">The `ulong` value that needs to be written.</param>
                ///<param name="bytes">The number of bytes `src` should occupy (1 to 8).</param>
                ///<param name="next_case">The state to resume at when more buffer space is available.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                private void put(ulong src, int bytes, uint next_case)
                {
                    slot!.state = next_case;
                    bytes_left = bytes;
                    u8 = src;
                    mode = VAL8;
                }

                ///<summary>
                ///Sets the transmitter's current processing mode to `RETRY` and records `the_case` as the state
                ///to resume from when more buffer space is available (or operation can be retried).
                ///</summary>
                ///<param name="the_case">The specific state or step in the serialization logic to resume at.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void retry_at(uint the_case)
                {
                    slot!.state = the_case;
                    mode = RETRY;
                }

                ///<summary>
                ///Calculates the minimum number of bytes required to represent an integer value.
                ///0 requires 0 bytes (by some conventions, not universally).
                ///1-255 requires 1 byte.
                ///256-65535 requires 2 bytes.
                ///65536-16777215 requires 3 bytes.
                ///Larger values up to Int32.MaxValue require 4 bytes.
                ///</summary>
                ///<param name="value">The integer value.</param>
                ///<returns>The number of bytes required (0-4).</returns>
                public int bytes4value(int value) => value < 0xFFFF ? value < 0xFF ? value == 0 ? 0 : 1 : 2
                                                     : value < 0xFFFFFF ? 3
                                                                        : 4;

                ///<summary>
                ///Puts a boolean value into the buffer as a single byte (1 for true, 0 for false).
                ///If not enough buffer space, sets up a retry.
                ///</summary>
                ///<param name="src">The boolean value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the byte was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(bool src, uint next_case) => put(src ? (byte)1 : (byte)0, next_case);

                ///<summary>
                ///Puts a signed byte value into the buffer. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The signed byte value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(sbyte src) => buffer![byte_++] = (byte)src;

                ///<summary>
                ///Puts a nullable signed byte value into the buffer. Assumes the value is not null and sufficient space is available.
                ///</summary>
                ///<param name="src">The nullable signed byte value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(sbyte? src) => buffer![byte_++] = (byte)src!.Value;

                ///<summary>
                ///Puts a signed byte value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The signed byte value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the byte was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(sbyte src, uint next_case) => put((byte)src, next_case);

                ///<summary>
                ///Puts a nullable signed byte value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable signed byte value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the byte was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(sbyte? src, uint next_case) => put((byte)src!.Value, next_case);

                ///<summary>
                ///Puts an unsigned byte value into the buffer. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The unsigned byte value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(byte src) => buffer![byte_++] = src;

                ///<summary>
                ///Puts a nullable unsigned byte value into the buffer. Assumes the value is not null and sufficient space is available.
                ///</summary>
                ///<param name="src">The nullable unsigned byte value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(byte? src) => buffer![byte_++] = src!.Value;

                ///<summary>
                ///Puts a nullable unsigned byte value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable unsigned byte value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the byte was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(byte? src, uint next_case) => put(src!.Value, next_case);

                ///<summary>
                ///Puts an unsigned byte value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The unsigned byte value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the byte was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(byte src, uint next_case)
                {
                    if (byte_ < byte_max)
                    {
                        put(src);
                        return true;
                    }

                    put(src, 1, next_case);
                    return false;
                }

                public int put(byte[] src, int src_byte, int src_bytes, uint retry_case)
                {
                    if (remaining < src_byte)
                    {
                        src_bytes = remaining;
                        retry_at(retry_case);
                    }

                    Array.Copy(src, src_byte, buffer, byte_, src_bytes);
                    byte_ += src_bytes;
                    return src_bytes;
                }

                ///<summary>
                ///Puts a nullable short (Int16) value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable short value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(short? src, uint next_case) => put((ushort)src!.Value, next_case);

                ///<summary>
                ///Puts a short (Int16) value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The short value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(short src, uint next_case) => put((ushort)src, next_case);

                ///<summary>
                ///Puts a nullable unsigned short (UInt16) value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable unsigned short value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(ushort? src, uint next_case) => put(src!.Value, next_case);

                ///<summary>
                ///Puts an unsigned short (UInt16) value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The unsigned short value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(ushort src, uint next_case)
                {
                    if (remaining < 2)
                    {
                        put(src, 2, next_case);
                        return false;
                    }

                    put(src);
                    return true;
                }

                ///<summary>
                ///Puts a short (Int16) value into the buffer. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The short value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(short src) => put((ushort)src);

                ///<summary>
                ///Puts a nullable short (Int16) value into the buffer. Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable short value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(short? src) => put((ushort)src!.Value);

                ///<summary>
                ///Puts a nullable unsigned short (UInt16) value into the buffer. Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable unsigned short value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(ushort? src) => put(src!.Value);

                ///<summary>
                ///Puts an unsigned short (UInt16) value into the buffer using appropriate endianness. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The unsigned short value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(ushort src)
                {
                    Endianness.OK.UInt16(src, buffer!, byte_);
                    byte_ += 2;
                }

                ///<summary>
                ///Puts an integer (Int32) value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The integer value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(int src, uint next_case) => put((uint)src, next_case);

                ///<summary>
                ///Puts a nullable integer (Int32) value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable integer value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(int? src, uint next_case) => put((uint)src!.Value, next_case);

                ///<summary>
                ///Puts a float value into the buffer (as its UInt32 bit representation). If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The float value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(float src, uint next_case) => put(Unsafe.As<float, uint>(ref src), next_case);

                ///<summary>
                ///Puts a nullable float value into the buffer (as its UInt32 bit representation). Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable float value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(float? src, uint next_case)
                {
                    var f = src!.Value;
                    return put(Unsafe.As<float, uint>(ref f), next_case);
                }

                ///<summary>
                ///Puts a nullable unsigned integer (UInt32) value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable unsigned integer value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(uint? src, uint next_case) => put(src!.Value, next_case);

                ///<summary>
                ///Puts an unsigned integer (UInt32) value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The unsigned integer value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(uint src, uint next_case)
                {
                    if (remaining < 4)
                    {
                        put(src, 4, next_case);
                        return false;
                    }

                    put(src);
                    return true;
                }

                ///<summary>
                ///Puts an integer (Int32) value into the buffer. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The integer value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(int src) => put((uint)src);

                ///<summary>
                ///Puts a nullable integer (Int32) value into the buffer. Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable integer value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(int? src) => put((uint)src!.Value);

                ///<summary>
                ///Puts a nullable float value into the buffer (as its UInt32 bit representation). Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable float value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(float? src)
                {
                    var f = src!.Value;
                    put(Unsafe.As<float, uint>(ref f));
                }

                ///<summary>
                ///Puts a float value into the buffer (as its UInt32 bit representation). Assumes sufficient space.
                ///</summary>
                ///<param name="src">The float value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(float src) => put(Unsafe.As<float, uint>(ref src));

                ///<summary>
                ///Puts a nullable unsigned integer (UInt32) value into the buffer. Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable unsigned integer value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(uint? src) => put(src!.Value);

                ///<summary>
                ///Puts an unsigned integer (UInt32) value into the buffer using appropriate endianness. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The unsigned integer value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(uint src)
                {
                    Endianness.OK.UInt32(src, buffer!, byte_);
                    byte_ += 4;
                }

                ///<summary>
                ///Puts a long (Int64) value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The long value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(long src, uint next_case) => put((ulong)src, next_case);

                ///<summary>
                ///Puts a nullable long (Int64) value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable long value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(long? src, uint next_case) => put((ulong)src!.Value, next_case);

                ///<summary>
                ///Puts a double value into the buffer (as its UInt64 bit representation). If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The double value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(double src, uint next_case) => put(Unsafe.As<double, ulong>(ref src), next_case);

                ///<summary>
                ///Puts a nullable double value into the buffer (as its UInt64 bit representation). Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable double value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(double? src, uint next_case)
                {
                    var d = src!.Value;
                    return put(Unsafe.As<double, ulong>(ref d), next_case);
                }

                ///<summary>
                ///Puts a nullable unsigned long (UInt64) value into the buffer. Assumes value is not null. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The nullable unsigned long value to put. Must have a value.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(ulong? src, uint next_case) => put(src!.Value, next_case);

                ///<summary>
                ///Puts an unsigned long (UInt64) value into the buffer. If not enough space, sets up a retry.
                ///</summary>
                ///<param name="src">The unsigned long value to put.</param>
                ///<param name="next_case">The state to retry at if writing fails.</param>
                ///<returns>True if the value was put successfully; false if a retry is needed.</returns>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public bool put(ulong src, uint next_case)
                {
                    if (remaining < 8)
                    {
                        put(src, 8, next_case);
                        return false;
                    }

                    put(src);
                    return true;
                }

                ///<summary>
                ///Puts a double value into the buffer (as its UInt64 bit representation). Assumes sufficient space.
                ///</summary>
                ///<param name="src">The double value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(double src) => put(Unsafe.As<double, ulong>(ref src));

                ///<summary>
                ///Puts a nullable double value into the buffer (as its UInt64 bit representation). Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable double value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(double? src)
                {
                    var d = src!.Value;
                    put(Unsafe.As<double, ulong>(ref d));
                }

                ///<summary>
                ///Puts a long (Int64) value into the buffer. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The long value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(long src) => put((ulong)src);

                ///<summary>
                ///Puts a nullable long (Int64) value into the buffer. Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable long value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(long? src) => put((ulong)src!.Value);

                ///<summary>
                ///Puts a nullable unsigned long (UInt64) value into the buffer. Assumes value is not null and sufficient space.
                ///</summary>
                ///<param name="src">The nullable unsigned long value to put. Must have a value.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(ulong? src) => put(src!.Value);

                ///<summary>
                ///Puts an unsigned long (UInt64) value into the buffer using appropriate endianness. Assumes sufficient space.
                ///</summary>
                ///<param name="src">The unsigned long value to put.</param>
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void put(ulong src)
                {
                    Endianness.OK.UInt64(src, buffer, byte_);
                    byte_ += 8;
                }

                public override string ToString()
                {
                    if (slot == null)
                        return "";
                    var s = slot;
                    while (s.prev != null)
                        s = s.prev;
                    var str = "";
                    var offset = "";
                    for (; s != slot; s = s.next, offset += "\t")
                        str += $"{offset}{s.src.GetType()}\t{s.state}\n";

                    str += $"{offset}{slot.src.GetType()}\t{s.state}\n";

                    return str;
                }
            }
        }

        ///<summary>
        ///Internal constants representing different data processing modes or states.
        ///These are used to manage continuations and data type expectations during serialization/deserialization.
        ///</summary>
        internal const uint OK = int.MaxValue, //Indicates successful completion or normal state.
            STR = OK - 100,                    //Mode for processing string data.
            RETRY = STR + 1,                   //Mode indicating a retry is needed, often due to insufficient data.
            VAL4 = RETRY + 1,                  //Mode for processing a 4-byte value.
            VAL8 = VAL4 + 1,                   //Mode for processing an 8-byte value.
            INT1 = VAL8 + 1,                   //Mode for processing a 1-byte signed integer.
            INT2 = INT1 + 1,                   //Mode for processing a 2-byte signed integer.
            INT4 = INT2 + 1,                   //Mode for processing a 4-byte signed integer.
            LEN0 = INT4 + 1,                   //Mode for processing a length field (specific usage).
            LEN1 = LEN0 + 1,                   //Mode for processing a length field (specific usage).
            LEN2 = LEN1 + 1,                   //Mode for processing a length field (specific usage).
            BITS = LEN2 + 1,                   //Mode for bitwise processing.
            BITS_BYTES = BITS + 1,             //Mode for combined bitwise and byte-level processing.
            VARINT = BITS_BYTES + 1;           //Mode for processing varint encoded data.

        ///<summary>
        ///Current bit position within the `bits` field for bitwise read/write operations. Typically ranges from 0 to 7.
        ///</summary>
        protected int bit;

        ///<summary>
        ///Temporary string buffer, often used during string deserialization.
        ///</summary>
        public string? str;

        ///<summary>
        ///Buffer for accumulating bits during bitwise read/write operations.
        ///</summary>
        public uint bits;

        ///<summary>
        ///The primary byte buffer used for serialization or deserialization operations.
        ///</summary>
        public byte[]? buffer;

        ///<summary>
        ///The current byte offset within the `buffer` for read/write operations.
        ///</summary>
        public int byte_;

        ///<summary>
        ///The exclusive upper limit for byte access in the `buffer` (i.e., `buffer[byte_max - 1]` is the last accessible byte).
        ///</summary>
        public int byte_max;

        ///<summary>
        ///The current processing mode, indicating the type of data or operation being handled. Uses constants like `OK`, `STR`, `RETRY`.
        ///</summary>
        public uint mode;

        ///<summary>
        ///Temporary buffer for a 4-byte unsigned integer value, used during partial reads or for small value processing.
        ///</summary>
        internal uint u4;

        ///<summary>
        ///Temporary buffer for an 8-byte unsigned integer value, used during partial reads or for general value processing.
        ///</summary>
        public ulong u8;

        ///<summary>
        ///Secondary temporary buffer for an 8-byte unsigned integer value, often used in conjunction with `u8` (e.g., varint operations, storing original values).
        ///</summary>
        public ulong u8_;

        ///<summary>
        ///Gets the value currently stored in the `u8` buffer, reinterpreted as type `T`.
        ///This is an unsafe cast and assumes `T` is compatible with an 8-byte representation.
        ///</summary>
        ///<typeparam name="T">The type to reinterpret the `u8` buffer as.</typeparam>
        ///<returns>The value from `u8` cast to type `T`.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
        public T get8<T>() => Unsafe.As<ulong, T>(ref u8);

        private int bytes_left;
        private int bytes_max;

        ///<summary>
        ///Gets the number of bytes remaining in the `buffer` from the current `byte_` position to `byte_max`.
        ///</summary>
        public int remaining => byte_max - byte_;

        ///<summary>
        ///Resizes an array to a new length. If the array is expanded, new elements are filled with `fill_value`.
        ///If the array is shrunk, elements are truncated. If lengths are equal, the original array is returned.
        ///</summary>
        ///<typeparam name="T">The type of elements in the array.</typeparam>
        ///<param name="src">The source array to resize.</param>
        ///<param name="new_length">The desired new length of the array.</param>
        ///<param name="fill_value">The value to use for newly added elements if the array is expanded.</param>
        ///<returns>The resized array. This may be the original array instance if `new_length` matches the original length, or a new array instance if `Array.Resize` reallocates.</returns>
        public static T[] Resize<T>(T[] src, int new_length, T fill_value)
        {
            var len = src.Length;
            if (len == new_length)
                return src;

            Array.Resize(ref src, new_length);
            if (len < new_length)
                Array.Fill(src, fill_value, len, new_length - len);
            return src;
        }

        ///<summary>
        ///Resizes a `List<T>` to a new length. If the list is expanded, new elements are added with `fillValue`.
        ///If the list is shrunk, elements are removed from the end.
        ///</summary>
        ///<typeparam name="T">The type of elements in the list.</typeparam>
        ///<param name="list">The list to resize.</param>
        ///<param name="newLength">The desired new length of the list.</param>
        ///<param name="fillValue">The value to use for newly added elements if the list is expanded.</param>
        ///<returns>The original list instance, resized.</returns>
        public static List<T> Resize<T>(List<T> list, int newLength, T fillValue)
        {
            if (list.Count < newLength)
                while (list.Count < newLength)
                    list.Add(fillValue);
            else
                list.RemoveRange(newLength, list.Count - newLength);

            return list;
        }

        ///<summary>
        ///Resizes the specified `IList<T>` to the new length.
        ///If `newLength` is greater than the current count, the list is expanded and new elements are filled with `fillValue`.
        ///If `newLength` is less than the current count, the list is truncated.
        ///This method handles `T[]` and `List<T>` efficiently, and provides a generic fallback for other `IList<T>` implementations.
        ///</summary>
        ///<typeparam name="T">The type of elements in the list.</typeparam>
        ///<param name="list">The list to resize.</param>
        ///<param name="newLength">The desired length of the list.</param>
        ///<param name="fillValue">The value to use for filling the list if it is expanded.</param>
        ///<returns>The resized list. This may be a new instance if `list` was an array and resizing occurred, or the original instance for other `IList<T>` types.</returns>
        public static IList<T> Resize<T>(IList<T> list, int newLength, T fillValue)
        {
            switch (list)
            {
                case T[] array:
                    return Resize(array, newLength, fillValue);

                case List<T> list_:
                    return Resize(list_, newLength, fillValue);

                default:
                    //If the list is neither an array nor a List<T>, handle the resizing manually
                    if (list.Count < newLength)
                        while (list.Count < newLength)
                            list.Add(fillValue);
                    else
                        while (list.Count > newLength)
                            list.RemoveAt(list.Count - 1);

                    return list;
            }
        }

        ///<summary>
        ///Converts an `IList<T>` to a `List<T>`, taking at most `max` elements from the source.
        ///</summary>
        ///<typeparam name="T">The type of elements in the list.</typeparam>
        ///<param name="src">The source `IList<T>`.</param>
        ///<param name="max">The maximum number of elements to include in the new list.</param>
        ///<returns>A new `List<T>` containing elements from `src`, truncated or copied entirely based on `max`.</returns>
        public static List<T> toList<T>(IList<T> src, int max)
        {
            var ret = new List<T>(max);

            if (max < src.Count)
                for (var i = 0; i < max; i++)
                    ret.Add(src[i]);
            else
                ret.AddRange(src);

            return ret;
        }

        ///<summary>
        ///Creates a new array of a specified size. If `fill` is not the default value for type `T`,
        ///the array is filled with `fill`. Otherwise, it's returned with default-initialized elements
        ///(which is the standard behavior for `new T[size]`).
        ///</summary>
        ///<typeparam name="T">The type of elements in the array.</typeparam>
        ///<param name="fill">The value to fill the array with if it's not the default for `T`.</param>
        ///<param name="size">The size of the array to create.</param>
        ///<returns>A new array of the specified size, potentially filled with `fill`.</returns>
        public static T[] sizeArray<T>(T fill, int size)
        {
            var ret = new T[size];
            //Only fill the array if fill is not the default value for the type (e.g., 0 for integers, null for reference types)
            if (EqualityComparer<T>.Default.Equals(fill, default))
                return ret;

            //Fill the array with the specified fill value
            Array.Fill(ret, fill);
            return ret;
        }

        ///<summary>
        ///Creates a new `List<T>` of a specified size, filling it with the given `fill` value.
        ///</summary>
        ///<typeparam name="T">The type of elements in the list.</typeparam>
        ///<param name="fill">The value to fill the list with.</param>
        ///<param name="size">The desired number of elements in the list.</param>
        ///<returns>A new `List<T>` of size `size`, with all elements initialized to `fill`.</returns>
        public static List<T> sizeList<T>(T fill, int size)
        {
            var ret = new List<T>(size);

            while (-1 < --size)
                ret.Add(fill);
            return ret;
        }
        #region CRC
        ///<summary>
        ///Length of the CRC checksum in bytes. Currently 2 bytes for CRC16.
        ///</summary>
        private const int CRC_LEN_BYTES = 2; //CRC len in bytes

        ///<summary>
        ///Precomputed lookup table for CRC16 calculation (based on a 4-bit nibble).
        ///</summary>
        private static readonly ushort[] tab = { 0, 4129, 8258, 12387, 16516, 20645, 24774, 28903, 33032, 37161, 41290, 45419, 49548, 53677, 57806, 61935 };

        ///<summary>
        ///Calculates CRC16 for a given byte, updating an existing CRC value.
        ///This implementation is equivalent to the CRC-16/XMODEM variant.
        ///(Reference: Similar to Redis crc16: https://github.com/redis/redis/blob/unstable/src/crc16.c)
        ///For input "123456789", the output is 0x31C3 (decimal 12739).
        ///</summary>
        ///<param name="src">The input byte to include in the CRC calculation.</param>
        ///<param name="crc">The current CRC value (intermediate or initial).</param>
        ///<returns>The updated CRC value after processing the input byte.</returns>
        //!!!! Https://github.com/redis/redis/blob/95b1979c321eb6353f75df892ab8be68cf8f9a77/src/crc16.c
        //Output for "123456789"     : 31C3 (12739)
        private static ushort crc16(byte src, ushort crc)
        {
            crc = (ushort)(tab[(crc >> 12 ^ src >> 4) & 0x0F] ^ crc << 4);
            return (ushort)(tab[(crc >> 12 ^ src & 0x0F) & 0x0F] ^ crc << 4);
        }
        #endregion

        ///<summary>
        ///Implements equality comparison and hash code generation for `IList<T>` instances
        ///based on their element-wise content and sequence.
        ///</summary>
        ///<typeparam name="T">The type of elements in the list.</typeparam>
        public class ArrayEqualHash<T> : IEqualityComparer<IList<T>>
        {
            public bool Equals(IList<T>? x, IList<T>? y) => (x == null || y == null) ? x == y : x.Count == y.Count && x.SequenceEqual(y);

            public int GetHashCode(IList<T> list) => list.Aggregate(17, (current, item) => HashCode.Combine(current, item));
        }

        private static readonly ConcurrentDictionary<object, object> pool = new();

        ///<summary>
        ///Gets a singleton instance of `ArrayEqualHash<T>` for the specified type `T`.
        ///This comparer can be used for `IList<T>` instances where equality depends on element sequence and values.
        ///</summary>
        ///<typeparam name="T">The type of elements in the lists to be compared.</typeparam>
        ///<returns>An `IEqualityComparer<IList<T>>` instance.</returns>
        public static IEqualityComparer<IList<T>> getArrayEqualHash<T>()
        {
            var t = typeof(T);
            if (pool.TryGetValue(t, out var value))
                return (IEqualityComparer<IList<T>>)value;
            var ret = new ArrayEqualHash<T>();
            pool[t] = ret;
            return ret;
        }

        ///<summary>
        ///Interface for abstracting endianness-specific read and write operations on byte arrays.
        ///</summary>
        interface Endianness
        {
            ///<summary>
            ///Reads a 16-bit signed integer (short) from a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The source byte array.</param>
            ///<param name="index">The zero-based starting index in `src` to read from.</param>
            ///<returns>The 16-bit signed integer.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public short Int16(byte[] src, int index);

            ///<summary>
            ///Reads a 16-bit unsigned integer (ushort) from a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The source byte array.</param>
            ///<param name="index">The zero-based starting index in `src` to read from.</param>
            ///<returns>The 16-bit unsigned integer.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public ushort UInt16(byte[] src, int index);

            ///<summary>
            ///Reads a 32-bit signed integer (int) from a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The source byte array.</param>
            ///<param name="index">The zero-based starting index in `src` to read from.</param>
            ///<returns>The 32-bit signed integer.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public int Int32(byte[] src, int index);

            ///<summary>
            ///Reads a 32-bit unsigned integer (uint) from a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The source byte array.</param>
            ///<param name="index">The zero-based starting index in `src` to read from.</param>
            ///<returns>The 32-bit unsigned integer.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public uint UInt32(byte[] src, int index);

            ///<summary>
            ///Reads a 64-bit signed integer (long) from a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The source byte array.</param>
            ///<param name="index">The zero-based starting index in `src` to read from.</param>
            ///<returns>The 64-bit signed integer.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public long Int64(byte[] src, int index);

            ///<summary>
            ///Reads a 64-bit unsigned integer (ulong) from a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The source byte array.</param>
            ///<param name="index">The zero-based starting index in `src` to read from.</param>
            ///<returns>The 64-bit unsigned integer.</returns>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public ulong UInt64(byte[] src, int index);

            ///<summary>
            ///Writes a 16-bit signed integer (short) to a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The 16-bit signed integer to write.</param>
            ///<param name="dst">The destination byte array.</param>
            ///<param name="index">The zero-based starting index in `dst` to write to.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void Int16(short src, byte[] dst, int index);

            ///<summary>
            ///Writes a 16-bit unsigned integer (ushort) to a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The 16-bit unsigned integer to write.</param>
            ///<param name="dst">The destination byte array.</param>
            ///<param name="index">The zero-based starting index in `dst` to write to.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void UInt16(ushort src, byte[] dst, int index);

            ///<summary>
            ///Writes a 32-bit signed integer (int) to a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The 32-bit signed integer to write.</param>
            ///<param name="dst">The destination byte array.</param>
            ///<param name="index">The zero-based starting index in `dst` to write to.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void Int32(int src, byte[] dst, int index);

            ///<summary>
            ///Writes a 32-bit unsigned integer (uint) to a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The 32-bit unsigned integer to write.</param>
            ///<param name="dst">The destination byte array.</param>
            ///<param name="index">The zero-based starting index in `dst` to write to.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void UInt32(uint src, byte[] dst, int index);

            ///<summary>
            ///Writes a 64-bit signed integer (long) to a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The 64-bit signed integer to write.</param>
            ///<param name="dst">The destination byte array.</param>
            ///<param name="index">The zero-based starting index in `dst` to write to.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void Int64(long src, byte[] dst, int index);

            ///<summary>
            ///Writes a 64-bit unsigned integer (ulong) to a byte array at a given index, respecting the specific endianness.
            ///</summary>
            ///<param name="src">The 64-bit unsigned integer to write.</param>
            ///<param name="dst">The destination byte array.</param>
            ///<param name="index">The zero-based starting index in `dst` to write to.</param>
            [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
            public void UInt64(ulong src, byte[] dst, int index);

            ///<summary>
            ///Little-endian implementation of the `Endianness` interface.
            ///</summary>
            private class LE : Endianness
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public short Int16(byte[] src, int index) => Unsafe.ReadUnaligned<short>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ushort UInt16(byte[] src, int index) => Unsafe.ReadUnaligned<ushort>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public int Int32(byte[] src, int index) => Unsafe.ReadUnaligned<int>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public uint UInt32(byte[] src, int index) => Unsafe.ReadUnaligned<uint>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public long Int64(byte[] src, int index) => Unsafe.ReadUnaligned<long>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ulong UInt64(byte[] src, int index) => Unsafe.ReadUnaligned<ulong>(ref src[index]);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int16(short src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt16(ushort src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int32(int src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt32(uint src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int64(long src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), src);

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt64(ulong src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), src);
            }

            ///<summary>
            ///Big-endian implementation of the `Endianness` interface.
            ///</summary>
            private class BE : Endianness
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public short Int16(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<short>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ushort UInt16(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public int Int32(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<int>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public uint UInt32(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public long Int64(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<long>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public ulong UInt64(byte[] src, int index) => BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(ref src[index]));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int16(short src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt16(ushort src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 2)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int32(int src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt32(uint src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 4)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void Int64(long src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), BinaryPrimitives.ReverseEndianness(src));

                [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
                public void UInt64(ulong src, byte[] dst, int index) => Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(new Span<byte>(dst, index, 8)), BinaryPrimitives.ReverseEndianness(src));
            }

            ///<summary>
            ///Provides an `Endianness` implementation appropriate for the current system's architecture (Little or Big Endian).
            ///This ensures that multi-byte numerical values are read from and written to byte arrays correctly.
            ///</summary>
            public static readonly Endianness OK = BitConverter.IsLittleEndian ? new LE() : new BE();
        }

        ///<summary>
        ///Represents a boolean value that can also be null. Internally stored as a byte:
        ///0 for false, 1 for true, and 2 (see `NULL` constant) for null.
        ///</summary>
        public struct NullableBool : IEquatable<NullableBool>
        {
            ///<summary>
            ///Initializes a new instance of the <see cref="NullableBool"/> struct to the null state.
            ///</summary>
            public NullableBool() { }

            ///<summary>
            ///Initializes a new instance of the <see cref="NullableBool"/> struct with a specified boolean value.
            ///</summary>
            ///<param name="value">The boolean value.</param>
            public NullableBool(bool value) => Value = value;

            ///<summary>
            ///Initializes a new instance of the <see cref="NullableBool"/> struct with a raw byte value
            ///representing its state (0 for false, 1 for true, 2 for null).
            ///</summary>
            ///<param name="value">The byte value (0, 1, or 2).</param>
            public NullableBool(byte value) => this.value = value;

            ///<summary>
            ///The internal byte representation of the NullableBool state.
            ///0 = false, 1 = true, 2 (NULL) = null.
            ///</summary>
            public byte value = NULL;

            ///<summary>
            ///Gets or sets the boolean value of this <see cref="NullableBool"/>.
            ///Throws an exception if getting the value when it is null.
            ///Setting a value makes `hasValue` true.
            ///</summary>
            public bool Value
            {
                get => value == 1;
                set => this.value = (byte)(value ? 1 : 0);
            }

            ///<summary>
            ///Indicates whether the NullableBool has a value (not null).
            ///</summary>
            public bool hasValue => value != NULL;

            ///<summary>
            ///Sets the NullableBool to null state.
            ///</summary>
            public void to_null() => value = NULL;

            public static bool operator ==(NullableBool? a, NullableBool? b) => a.HasValue == b.HasValue && (!a.HasValue || a!.Value.value == b!.Value.value);
            public static bool operator !=(NullableBool? a, NullableBool? b) => a.HasValue != b.HasValue || (a.HasValue && a!.Value.value != b!.Value.value);

            public static bool operator ==(NullableBool a, NullableBool b) => a.value == b.value;
            public static bool operator !=(NullableBool a, NullableBool b) => a.value != b.value;

            public static bool operator ==(NullableBool a, bool b) => a.value != NULL && a.value == (byte)(b ? 1 : 0);

            public static bool operator !=(NullableBool a, bool b) => a.value == NULL || a.value != (byte)(b ? 1 : 0);

            public static bool operator ==(bool a, NullableBool b) => b.value != NULL && b.value == (byte)(a ? 1 : 0);

            public static bool operator !=(bool a, NullableBool b) => b.value == NULL || b.value != (byte)(a ? 1 : 0);

            public override bool Equals(object? other) => other is NullableBool p && p.value == value;
            public bool Equals(NullableBool other) => value == other.value;
            public override int GetHashCode() => value.GetHashCode();

            public static explicit operator bool(NullableBool a) => a.Value;
            public static implicit operator NullableBool(bool a) => new NullableBool(a);

            public static implicit operator NullableBool(bool? a) => a == null ? NULL : a.Value;

            public static explicit operator byte(NullableBool a) => a.value;
            public static implicit operator NullableBool(byte a) => new NullableBool(a);

            ///<summary>
            ///Constant value representing a null state for the `NullableBool` struct.
            ///</summary>
            public const byte NULL = 2;
        }

        //Decoding table for base64
        private static readonly byte[] char2byte = new byte[256];

        ///<summary>
        ///Static constructor for the `AdHoc` class. This initializes a lookup table for base64 decoding.
        ///This table allows quick lookup of the numerical value of each base64 character.
        ///</summary>
        static AdHoc()
        {
            for (int i = 'A'; i <= 'Z'; i++)
                char2byte[i] = (byte)(i - 'A');
            for (int i = 'a'; i <= 'z'; i++)
                char2byte[i] = (byte)(i - 'a' + 26);
            for (int i = '0'; i <= '9'; i++)
                char2byte[i] = (byte)(i - '0' + 52);
            char2byte['+'] = 62;
            char2byte['/'] = 63;
        }

        ///<summary>
        ///Decodes a base64 encoded byte array in-place, modifying the original array.
        ///</summary>
        ///<param name="bytes">The byte array containing the base64 encoded data.</param>
        ///<param name="src_index">The index in `bytes` where the base64 encoded data starts.</param>
        ///<param name="dst_index">The index in `bytes` where the decoded data will be written.</param>
        ///<param name="len">The number of base64 characters to decode.</param>
        ///<returns>The number of bytes written to the decoded data.</returns>
        public static int base64decode(byte[] bytes, int src_index, int dst_index, int len)
        {
            var max = src_index + len;

            while (bytes[max - 1] == '=')
            {
                max--;
            }

            var new_len = max - src_index;
            for (var i = new_len >> 2; 0 < i; i--) //Process full 4-character blocks
            {
                var b = char2byte[bytes[src_index++]] << 18 |
                        char2byte[bytes[src_index++]] << 12 |
                        char2byte[bytes[src_index++]] << 6 |
                        char2byte[bytes[src_index++]];

                bytes[dst_index++] = (byte)(b >> 16);
                bytes[dst_index++] = (byte)(b >> 8);
                bytes[dst_index++] = (byte)b;
            }

            switch (new_len & 3)
            {
                case 3:
                    //If there are 3 characters remaining, decode them into 2 bytes
                    var b = char2byte[bytes[src_index++]] << 12 |
                            char2byte[bytes[src_index++]] << 6 |
                            char2byte[bytes[src_index]];
                    bytes[dst_index++] = (byte)(b >> 10); //Extract first byte
                    bytes[dst_index++] = (byte)(b >> 2);  //Extract second byte
                    break;
                case 2:
                    //If there are 2 characters remaining, decode them into 1 byte
                    bytes[dst_index++] = (byte)((char2byte[bytes[src_index++]] << 6 | char2byte[bytes[src_index]]) >> 4);
                    break;
            }

            return dst_index;
        }

        ///<summary>
        ///Retrieves a set of TXT records from DNS for a given key, returning the values as a byte array.
        ///</summary>
        ///<param name="key">The DNS key to query.</param>
        ///<returns>An array of `Memory<byte>` representing the TXT record values associated with the key, or null if the lookup fails.</returns>
        //Using DNS as readonly key-value storage https://datatracker.ietf.org/doc/html/rfc1035
        public static Memory<byte>[] value(string key)
        {
            byte[] Create_DNS_TXT_Record_Request(string domain)
            {
                var id = (ushort)new Random().Next(65536); //Generate a random query ID

                var request = new byte[12 + domain.Length + 2 + 4]; //Initialize the request packet

                //Set DNS header fields
                request[0] = (byte)(id >> 8);
                request[1] = (byte)(id & 0xFF);
                request[2] = 0x01; //QR=0, OPCODE=0, AA=0, TC=0, RD=1
                request[5] = 0x01; //QDCOUNT=1

                //Add the domain name to the question section
                var index = 12;
                var p = index++;

                foreach (var ch in domain)
                    if (ch == '.')
                    {
                        request[p] = (byte)(index - p - 1);
                        p = index++;
                    }
                    else
                        request[index++] = (byte)ch;

                request[p] = (byte)(index - p - 1); //Set the length for the last label

                index += 2;              //Terminate domain name, set question type (TXT) and class (IN)
                request[index++] = 0x10; //QTYPE = TXT
                request[++index] = 0x01; //QCLASS = IN

                return request;
            }

            static Memory<byte>[] Parse_DNS_TXT_Record_Response(byte[] response)
            {
                var questionCount = (response[4] << 8) | response[5]; //Extract question and answer counts from the header
                var answerCount = (response[6] << 8) | response[7];

                var index = 12;

                for (var i = 0; i < questionCount; i++, index += 5) //Skip the question section
                    while (response[index] != 0)
                        index += response[index] + 1;

                var dst_index = 0;
                var dst_index_ = 0;
                var records = new Memory<byte>[answerCount];
                for (var i = 0; i < answerCount; i++) //Parse each answer
                {
                    index += 2; //Skip NAME field
                                //TYPE            two octets containing one of the RR TYPE codes.
                    var TYPE = (ushort)((response[index] << 8) | response[index + 1]);
                    //CLASS           two octets containing one of the RR CLASS codes.
                    //
                    //TTL             a 32 bit signed integer that specifies the time interval
                    //                that the resource record may be cached before the source
                    //                of the information should again be consulted.  Zero
                    //                values are interpreted to mean that the RR can only be
                    //                used for the transaction in progress, and should not be
                    //                cached.  For example, SOA records are always distributed
                    //                with a zero TTL to prohibit caching.  Zero values can
                    //                also be used for extremely volatile data.
                    index += 8;                                                          //Skip all above
                    var RDLENGTH = (ushort)(response[index] << 8 | response[index + 1]); //an unsigned 16 bit integer that specifies the length in  octets of the RDATA field.
                    index += 2;
                    //TXT-DATA        One or more <character-string>s. where <character-string> is a single length octet followed by that number of characters
                    //!!! attention records in reply may follow in arbitrary order

                    if (TYPE == 16) //TXT record
                        for (var max = index + RDLENGTH; index < max;)
                        {
                            var len = response[index++];
                            Array.Copy(response, index, response, dst_index, len);
                            dst_index += len;
                            index += len;
                        }

                    records[i] = new Memory<byte>(response, dst_index_, dst_index - dst_index_);
                    dst_index_ = dst_index;
                }

                return records;
            }

            var ep = new IPEndPoint(IPAddress.Any, 0);

            using (var udpClient = new UdpClient()) foreach (var os_dns in NetworkInterface.GetAllNetworkInterfaces()
                                                                 .Where(n => n.OperationalStatus == OperationalStatus.Up)
                                                                 .SelectMany(n => n.GetIPProperties().DnsAddresses)
                                                                 .ToArray())
                    try
                    {
                        var request = Create_DNS_TXT_Record_Request(key);

                        udpClient.Send(request, request.Length, os_dns.ToString(), 53);

                        var response = udpClient.Receive(ref ep);

                        return Parse_DNS_TXT_Record_Response(response);
                    }
                    catch (Exception e)
                    {
                    }

            return null;
        }

        ///<summary>
        ///Calculates the number of bytes required to varint encode a span of characters.
        ///Varint encoding uses variable numbers of bytes to represent integers, optimizing for small numbers.
        ///Each character may require 1, 2, or 3 bytes.
        ///</summary>
        ///<param name="src">The ReadOnlySpan of characters to encode.</param>
        ///<returns>The estimated number of bytes needed to varint-encode the characters in `src`.</returns>
        public static int varint_bytes(ReadOnlySpan<char> src)
        {
            var bytes = 0;
            foreach (var ch in src)
                bytes += ch < 0x80 ? 1 : ch < 0x4_000 ? 2
                                                      : 3;
            return bytes;
        }

        ///<summary>
        ///Counts the number of characters represented by a varint-encoded byte span.
        /// Varint encoding represents each character via 1 to 3 bytes.
        ///</summary>
        ///<param name="src">The span of bytes in varint encoding.</param>
        ///<returns>The number of characters encoded by the `src` bytes.</returns>
        public static int varint_chars(ReadOnlySpan<byte> src)
        {
            var chars = 0;
            foreach (var b in src)
                if (b < 0x80)
                    chars++;
            return chars;
        }

        ///<summary>
        ///Encodes a portion of a string into a byte array using varint encoding.
        ///Each character is represented by one, two, or three bytes.
        ///This function is used in the `Transmitter`'s implementation.
        ///</summary>
        ///<param name="src">The source string to encode.</param>
        ///<param name="dst">The destination byte array.</param>
        ///<returns>
        ///A 64-bit unsigned integer. This combines two pieces of information:
        ///- High 32 bits: The index in the source string (`src`) of the next character *not* yet processed.  If all characters
        ///were encoded, this will equal `src.Length`.
        ///- Low 32 bits: The number of bytes written to the `dst` byte array.
        ///</returns>
        public static ulong varint(ReadOnlySpan<char> src, Span<byte> dst)
        {
            var src_from = 0;
            var dst_from = 0;

            //Iterate through the source string, starting from the specified index
            for (int dst_max = dst.Length, src_max = src.Length, ch; src_from < src_max; src_from++)
                if ((ch = src[src_from]) < 0x80) //Most frequent case: ASCII characters (0-127) These characters are encoded as a single byte
                {
                    //Check if there's enough space in the destination array for 1 byte
                    if (dst_from == dst_max)
                        break;

                    //Encode the character in 1 byte (no special encoding needed)
                    dst[dst_from++] = (byte)ch;
                }
                else if (ch < 0x4_000)
                {
                    //Check if there's enough space in the destination array for 2 bytes
                    if (dst_max - dst_from < 2)
                        break;

                    //Encode the character in 2 bytes using varint encoding
                    dst[dst_from++] = (byte)(0x80 | ch); //First byte: Set the MSB and use 7 LSBs of ch
                    dst[dst_from++] = (byte)(ch >> 7);   //Second byte: Use the remaining 7 bits of ch
                }
                else //Less frequent case
                {
                    //Check if there's enough space in the destination array for 3 bytes
                    if (dst_max - dst_from < 3)
                        break;

                    //Encode the character in 3 bytes using varint encoding
                    dst[dst_from++] = (byte)(0x80 | ch);      //First byte: Set the MSB and use 7 LSBs of ch
                    dst[dst_from++] = (byte)(0x80 | ch >> 7); //Second byte: Set the MSB and use next 7 bits of ch
                    dst[dst_from++] = (byte)(ch >> 14);       //Third byte: Use the remaining 2 bits of ch
                }

            //Return the result: high 32 bits contain the next character index to process,
            //low 32 bits contain the number of bytes written to the destination array
            return (ulong)(uint)src_from << 32 | (uint)dst_from;
        }

        ///<summary>
        ///Decodes a sequence of varint-encoded bytes into a string, appending the result to the provided StringBuilder.
        ///This is the complimentary of the `varint(ReadOnlySpan<char> src, Span<byte> dst)` method.
        ///This function is used in the `Receiver`'s implementation.
        ///</summary>
        ///<param name="src">The input byte array containing varint-encoded data.</param>
        ///<param name="ret">An integer, used to carry over state from a previous call to this function
        ///(for multi-byte characters that were only partially decoded in a previous call).
        ///- Low 16 bits: Contains the partial character being decoded (if incomplete).
        ///- High 8 bits: The number of bits processed for the current partial character (from previous call).
        ///</param>
        ///<param name="dst">The StringBuilder to which the decoded characters are appended.</param>
        ///<returns>
        ///An integer, to be used as `ret` in a subsequent call if decoding isn't finished.
        ///- Low 16 bits: If a character is only partially decoded (e.g. 2 bytes), these bits contain
        ///the partially decoded character.
        ///- High 8 bits: Number of bits processed for a partial character.
        ///</returns>
        public static int varint(ReadOnlySpan<byte> src, int ret, StringBuilder dst)
        {
            var src_from = 0;
            var dst_to = src.Length;
            //Extract the partial character and shift from the ret parameter
            var ch = ret & 0xFFFF;     //Low 16 bits: partial character value
            var s = (byte)(ret >> 16); //High 8 bits: number of bits already processed
            int b;
            while (src_from < dst_to)
                if ((b = src[src_from++]) < 0x80) //If the high bit is not set, this is the last byte of the character
                {
                    //Combine the partial character with the current byte and append to StringBuilder
                    dst.Append((char)(b << s | ch));
                    s = 0;  //Reset the shift
                    ch = 0; //Reset the partial character
                }
                else //If the high bit is set, this is not the last byte of the character
                {
                    //Add the 7 bits of this byte to the partial character
                    ch |= (b & 0x7F) << s;
                    s += 7; //Increase the shift by 7 bits
                }

            //Return the current state (partial character and shift) for potential continuation
            return s << 16 | ch;
        }

        ///<summary>
        ///Creates a Boyer-Moore pattern table to optimize case-sensitive string searches.
        ///This table is used by the `boyer_moore_ASCII_Case_sensitive` and `boyer_moore_ASCII_Case_insensitive` methods.
        ///</summary>
        ///<param name="src">The search pattern string.</param>
        ///<returns>An array of uints representing the pattern table.</returns>
        public static uint[] boyer_moore_pattern(string src)
        {
            var ret = new uint[src.Length];
            for (var i = src.Length; -1 < --i;)
                if (ret[i] == 0)
                    for (int ii = i, ch = src[i], p = i << 8 | ch; -1 < ii; ii--)
                        if (src[ii] == ch)
                            ret[ii] = (uint)p;
            return ret;
        }

        ///<summary>
        ///Performs a Boyer-Moore search for a case-sensitive match of a pattern in a byte array.
        ///</summary>
        ///<param name="bytes">The byte array to search within.</param>
        ///<param name="pattern">The Boyer-Moore pattern table generated for the search pattern.</param>
        ///<returns>The index of the last byte of the first match of the pattern in the byte array, or -1 if not found.</returns>
        //Case-sensitive
        public static int boyer_moore_ASCII_Case_sensitive(byte[] bytes, uint[] pattern) //return pattern's last byte position in the `bytes`
        {
            for (int len = pattern.Length, i = len - 1, max = bytes.Length - len + 1; i < max;)
            {
                for (var j = len; -1 < --j;)
                {
                    var p = pattern[j];

                    if ((byte)p == bytes[i + j])
                        continue; //Compare characters

                    //Use the last occurrence to determine how far to skip
                    var last = p >>> 8; //Extract last occurrence position
                    i += (int)Math.Max(1, j - last);
                    goto next;
                }

                return i; //return found pattern's last byte position in the `bytes`
            next:;
            }

            return -1; //Pattern not found
        }

        ///<summary>
        ///Performs a Boyer-Moore search for a case-insensitive match of a pattern in a byte array.
        ///</summary>
        ///<param name="bytes">The byte array to search within.</param>
        ///<param name="pattern">The Boyer-Moore pattern table generated for the search pattern.</param>
        ///<returns>The index of the last byte of the first match of the pattern in the byte array, or -1 if not found.</returns>
        //Case-insensitive
        public static int boyer_moore_ASCII_Case_insensitive(byte[] bytes, uint[] pattern) //return pattern's last byte position in the `bytes`
        {
            for (int len = pattern.Length, i = len - 1, max = bytes.Length - len + 1; i < max;)
            {
                for (var j = len; -1 < --j;)
                {
                    var p = pattern[j];

                    switch ((sbyte)p - bytes[i + j])
                    {
                        case 0:
                            continue;
                        case 32:
                            if ('a' <= p)
                                continue;
                            break;
                        case -32:
                            if ('A' <= p)
                                continue;
                            break;
                    }

                    //Use the last occurrence to determine how far to skip
                    var last = p >>> 8; //Extract last occurrence position
                    i += (int)Math.Max(1, j - last);
                    goto next;
                }

                return i; //return found pattern's last byte position in the `bytes`
            next:;
            }

            return -1; //Pattern not found
        }

        ///<summary>
        ///Packs a specified number of bits from a long value into a byte array at a given bit offset.
        ///</summary>
        ///<param name="src">The source long value containing the bits to pack.</param>
        ///<param name="dst">The destination byte array where bits will be packed.</param>
        ///<param name="dstBit">The starting bit position in the destination array.</param>
        ///<param name="dstBits">The number of bits to pack from the source.</param>
        public static void Pack(ulong src, byte[] dst, int dstBit, int dstBits)
        {
            var i = dstBit >> 3;
            dstBit &= 7;

            var done = Math.Min(dstBits, 8 - dstBit);
            var mask = (1UL << done) - 1;
            dst[i] = (byte)(dst[i] & ~(mask << dstBit) | (src & mask) << dstBit);
            src >>= done;
            dstBits -= done;
            i++;

            for (; 7 < dstBits; dstBits -= 8, src >>= 8, i++)
                dst[i] = (byte)src;

            if (dstBits == 0)
                return;

            mask = (1UL << dstBits) - 1;
            dst[i] = (byte)(dst[i] & ~mask | src & mask);
        }

        ///<summary>
        ///Unpacks a specified number of bits from a byte array starting at a given bit offset into a long value.
        ///</summary>
        ///<param name="src">The source byte array containing the bits to unpack.</param>
        ///<param name="srcBit">The starting bit position in the source array.</param>
        ///<param name="srcBits">The number of bits to unpack.</param>
        ///<returns>The unpacked bits as a long value.</returns>
        public static ulong Unpack(byte[] src, int srcBit, int srcBits)
        {
            var i = srcBit >> 3;
            srcBit &= 7;

            var done = Math.Min(srcBits, 8 - srcBit);
            var result = (ulong)(src[i] >> srcBit) & (1UL << done) - 1;

            srcBits -= done;
            i++;

            for (; 7 < srcBits; done += 8, srcBits -= 8, i++)
                result |= (ulong)src[i] << done;

            return srcBits == 0 ? result : result | (src[i] & ((1UL << srcBits) - 1)) << done;
        }
    }
}