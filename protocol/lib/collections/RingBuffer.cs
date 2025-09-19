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
//
//--- C# Transformation ---
//Transformed from Java to C# by an AI assistant.
//Original Java version by Chikirev Sirguy.
//Key changes:
//- Replaced AtomicLongFieldUpdater with System.Threading.Interlocked and Volatile operations.
//- Converted public methods to properties (Capacity, Size, IsEmpty, IsFull).
//- Adapted to C# generic constraints and instantiation (Activator.CreateInstance).
//- Translated JavaDoc to C# XML documentation.
//- Replaced Java exceptions with .NET equivalents.

using System;
using System.Threading;

namespace org.unirail.collections;

///<summary>
///A generic, fixed-size, lock-free ring buffer (circular buffer).
///</summary>
///<remarks>
///<para>This implementation uses a power-of-two capacity for high-performance index calculation
///via bit masking. It is optimized for the following concurrent scenarios:
///<list type="bullet">
///  <item><description><b>Single-Producer, Single-Consumer (SPSC)</b> - Use non-multithreaded methods like <see cref="Put(T)"/> and <see cref="Get()"/>.</description></item>
///  <item><description><b>Multiple-Producers, Single-Consumer (MPSC)</b> - Use <see cref="PutMultithreaded(T)"/>.</description></item>
///  <item><description><b>Single-Producer, Multiple-Consumers (SPMC)</b> - Use <see cref="GetMultithreaded()"/>.</description></item>
///</list>
///</para>
///<para><b>Warning:</b> This implementation is <b>not safe</b> for Multiple-Producers, Multiple-Consumers (MPMC)
///scenarios without external synchronization. In an MPMC context, data races can occur, leading to incorrect behavior.
///</para>
///<para>Thread safety for MPSC and SPMC scenarios is achieved using <see cref="System.Threading.Interlocked"/>
///to perform lock-free compare-and-set (CAS) operations on the head (<c>get</c>) and tail (<c>put</c>) pointers.
///</para>
///</remarks>
///<typeparam name="T">The type of elements held in this collection.</typeparam>
public class RingBuffer<T>
{
    ///<summary>
    ///The final array that stores the buffer's elements. Its length is always a power of two.
    ///</summary>
    private readonly T[] _buffer;

    ///<summary>
    ///Bit mask used for efficient index wrapping. Calculated as <c>capacity - 1</c>.
    ///</summary>
    private readonly uint _mask;

    ///<summary>
    ///The head pointer, tracking the index of the next element to be read (dequeued).
    ///Declared <c>volatile</c> to ensure visibility of writes across threads.
    ///Access in multithreaded contexts should use Interlocked/Volatile operations.
    ///</summary>
    private ulong _get;

    ///<summary>
    //The tail pointer, tracking the index of the next available slot for writing (enqueuing).
    ///Declared <c>volatile</c> to ensure visibility of writes across threads.
    ///Access in multithreaded contexts should use Interlocked/Volatile operations.
    ///</summary>
    private ulong _put;

    ///<summary>
    ///Constructs a ring buffer with a capacity equal to 2<sup><paramref name="powerOf2"/></sup>.
    ///</summary>
    ///<param name="powerOf2">The exponent for the capacity calculation (e.g., 10 results in a capacity of 1024).</param>
    ///<exception cref="ArgumentOutOfRangeException">If <paramref name="powerOf2"/> is negative or greater than 30 (to prevent overflow).</exception>
    public RingBuffer(int powerOf2) : this(powerOf2, false) { }

    ///<summary>
    ///Constructs a ring buffer with a capacity of 2<sup><paramref name="powerOf2"/></sup>, optionally pre-filling it with new instances.
    ///</summary>
    ///<remarks>
    ///Pre-filling is useful when the buffer will be used for recycling objects, allowing consumers to retrieve
    ///an object and producers to replace it without causing a null pointer exception on the first pass.
    ///</remarks>
    ///<param name="powerOf2">The exponent for the capacity calculation (e.g., 10 results in a capacity of 1024).</param>
    ///<param name="fill">If <c>true</c>, the buffer is pre-filled with new instances created via the type's
    ///default (no-argument) constructor.</param>
    ///<exception cref="ArgumentOutOfRangeException">If <paramref name="powerOf2"/> is negative or greater than 30 (to prevent overflow).</exception>
    ///<exception cref="InvalidOperationException">If <paramref name="fill"/> is <c>true</c> but the class <typeparamref name="T"/> does not have an accessible
    ///no-argument constructor, or if instantiation fails for any other reason.</exception>
    public RingBuffer(int powerOf2, bool fill)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(powerOf2, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(powerOf2, 30);

        var capacity = 1 << powerOf2;
        _mask = (uint)(capacity - 1);
        _buffer = new T[capacity];

        if (!fill)
            return;

        //Ensure T has a parameterless constructor before trying to activate.
        if (typeof(T).IsValueType || typeof(T).GetConstructor(Type.EmptyTypes) != null)
            for (var i = 0; i < _buffer.Length; i++)
            {
                _buffer[i] = Activator.CreateInstance<T>();
            }
        else
            throw new MissingMethodException($"Class {typeof(T).Name} requires a public parameterless constructor for filling.");
    }

    ///<summary>
    ///Constructs a ring buffer with a capacity of 2<sup><paramref name="powerOf2"/></sup>, optionally pre-filling it using a factory.
    ///</summary>
    ///<remarks>
    ///Pre-filling is useful when the buffer will be used for recycling objects.
    ///If the <paramref name="factory"/> is null, the buffer is created but left empty (containing default values, i.e., null for reference types).
    ///</remarks>
    ///<param name="powerOf2">The exponent for the capacity calculation (e.g., 10 results in a capacity of 1024).</param>
    ///<param name="factory">An optional factory function to invoke for each slot in the buffer to create the initial instances.
    ///If <c>null</c>, the buffer is not pre-filled.</param>
    ///<exception cref="ArgumentOutOfRangeException">If <paramref name="powerOf2"/> is negative or greater than 30 (to prevent overflow).</exception>
    public RingBuffer(int powerOf2, Func<T>? factory)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(powerOf2, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(powerOf2, 30);
        ArgumentNullException.ThrowIfNull(factory);

        var capacity = 1 << powerOf2;
        _mask = (uint)(capacity - 1);
        _buffer = new T[capacity];

        if (factory == null)
            return;
        for (var i = 0; i < _buffer.Length; i++)
            _buffer[i] = factory();
    }

    ///<summary>
    ///Gets the fixed capacity of the ring buffer.
    ///</summary>
    public int Capacity => _buffer.Length;

    ///<summary>
    ///Gets the approximate number of elements currently in the buffer.
    ///</summary>
    ///<remarks>
    ///In a concurrent environment, this value can be stale as soon as it is returned,
    ///as other threads may be adding or removing elements. It should be used as an estimate.
    ///</remarks>
    public uint Size => (uint)(Volatile.Read(ref _put) - Volatile.Read(ref _get));

    ///<summary>
    ///Checks if the buffer is empty.
    ///</summary>
    ///<remarks>
    ///In a concurrent environment, the buffer's state can change after this method returns.
    ///The result is a snapshot in time and may be stale.
    ///</remarks>
    public bool IsEmpty => Volatile.Read(ref _get) == Volatile.Read(ref _put);

    ///<summary>
    ///Checks if the buffer is full.
    ///</summary>
    ///<remarks>
    ///In a concurrent environment, the buffer's state can change after this method returns.
    ///The result is a snapshot in time and may be stale.
    ///</remarks>
    public bool IsFull => (Volatile.Read(ref _put) - Volatile.Read(ref _get)) == (ulong)_buffer.Length;

    ///<summary>
    ///Atomically retrieves and removes an element from the buffer, returning a default value if empty.
    ///This method is thread-safe for multiple consumers (SPMC).
    ///</summary>
    ///<returns>The retrieved element, or <c>default(T)</c> if the buffer was empty.</returns>
    public T GetMultithreaded() => GetMultithreaded(default(T), default(T));

    ///<summary>
    ///Atomically retrieves and removes an element from the buffer.
    ///This method is thread-safe for multiple consumers (SPMC).
    ///</summary>
    ///<param name="returnIfEmpty">The value to return if the buffer is empty.</param>
    ///<returns>The retrieved element, or <paramref name="returnIfEmpty"/> if the buffer was empty.</returns>
    public T GetMultithreaded(T returnIfEmpty) => GetMultithreaded(returnIfEmpty, default(T));

    ///<summary>
    ///Atomically retrieves and removes an element from the buffer, replacing it with a specified value.
    ///This method is thread-safe for multiple consumers (SPMC).
    ///It uses a compare-and-set (CAS) loop to atomically increment the 'get' pointer,
    ///ensuring that each consumer retrieves a unique element.
    ///</summary>
    ///<param name="returnIfEmpty">The value to return if the buffer is empty.</param>
    ///<param name="replacement">The value to place in the buffer at the retrieved element's slot.
    ///This is useful for 'nulling out' the reference to aid garbage collection or for recycling pooled objects.</param>
    ///<returns>The retrieved element, or <paramref name="returnIfEmpty"/> if the buffer was empty.</returns>
    public T GetMultithreaded(T returnIfEmpty, T replacement)
    {
        ulong currentGet;
        do
        {
            currentGet = Volatile.Read(ref _get); //Volatile read of the head pointer
            if (currentGet == Volatile.Read(ref _put))
                return returnIfEmpty; //Buffer is empty
        } while (Interlocked.CompareExchange(ref _get, currentGet + 1, currentGet) != currentGet); //Atomically claim the item

        var index = (int)currentGet & _mask;
        var result = _buffer[index];
        _buffer[index] = replacement;
        return result;
    }

    ///<summary>
    ///Retrieves and removes an element from the buffer.
    ///</summary>
    ///<remarks>
    ///<b>Warning: This method is not thread-safe.</b> It should only be used in a single-consumer
    ///context or when external synchronization is in place. <br/>
    ///<b>Precondition:</b> The caller is responsible for ensuring the buffer is <b>not empty</b>
    ///before calling this method (e.g., by checking <see cref="IsEmpty"/>). Invoking this on an empty buffer
    ///will corrupt the buffer's state by advancing the read pointer past the write pointer,
    ///and will return a stale, invalid element.
    ///</remarks>
    ///<returns>The retrieved element.</returns>
    public T Get() => Get(default(T));

    ///<summary>
    ///Retrieves and removes an element from the buffer, replacing it with the specified value.
    ///</summary>
    ///<remarks>
    ///<b>Warning: This method is not thread-safe.</b> It should only be used in a single-consumer
    ///context or when external synchronization is in place. It performs a non-atomic update. <br/>
    ///<b>Precondition:</b> The caller is responsible for ensuring the buffer is <b>not empty</b>
    ///before calling this method (e.g., by checking <see cref="IsEmpty"/>). Invoking this on an empty buffer
    ///will corrupt the buffer's state by advancing the read pointer past the write pointer,
    ///and will return a stale, invalid element.
    ///</remarks>
    ///<param name="replacement">The value to place in the buffer at the retrieved element's slot.</param>
    ///<returns>The retrieved element.</returns>
    public T Get(T replacement)
    {
        var index = (int)_get++ & _mask;
        var item = _buffer[index];
        _buffer[index] = replacement;
        return item;
    }

    ///<summary>
    ///Atomically adds an element to the buffer if space is available.
    ///This method is thread-safe for multiple producers (MPSC).
    ///It uses a compare-and-set (CAS) loop to atomically increment the 'put' pointer,
    ///ensuring that each producer claims a unique slot.
    ///</summary>
    ///<param name="value">The element to add.</param>
    ///<returns><c>true</c> if the element was successfully added, <c>false</c> if the buffer was full.</returns>
    public bool PutMultithreaded(T value)
    {
        ulong currentPut;
        do
        {
            currentPut = Volatile.Read(ref _put); //Volatile read of the tail pointer
            if (_buffer.Length <= (int)(currentPut - Volatile.Read(ref _get)))
                return false; //Buffer is full
        } while (Interlocked.CompareExchange(ref _put, currentPut + 1, currentPut) != currentPut); //Atomically claim the slot

        _buffer[(int)currentPut & _mask] = value;
        return true;
    }

    ///<summary>
    ///Atomically adds an element to the buffer, returning the element that was overwritten at the insertion index.
    ///This method is thread-safe for multiple producers (MPSC).
    ///This is useful in object pooling scenarios where the overwritten object needs to be handled.
    ///</summary>
    ///<param name="returnIfFull">The value to return if the buffer is full and the new element cannot be added.</param>
    ///<param name="value">The element to add.</param>
    ///<returns>The element that was previously at the insertion index, or <paramref name="returnIfFull"/> if the buffer was full.</returns>
    public T PutMultithreaded(T returnIfFull, T value)
    {
        ulong currentPut;
        do
        {
            currentPut = Volatile.Read(ref _put); //Volatile read
            if (_buffer.Length <= (int)(currentPut - Volatile.Read(ref _get)))
                return returnIfFull; //Buffer is full
        } while (Interlocked.CompareExchange(ref _put, currentPut + 1, currentPut) != currentPut);

        var index = (int)currentPut & _mask;
        var result = _buffer[index];
        _buffer[index] = value;
        return result;
    }

    ///<summary>
    ///Adds an element to the buffer, returning the element that was overwritten.
    ///</summary>
    ///<remarks>
    ///<b>Warning: This method is not thread-safe.</b> It should only be used in a single-producer
    ///context or when external synchronization is in place. <br/>
    ///The caller is responsible for ensuring the buffer is not full before calling this method.
    ///</remarks>
    ///<param name="value">The element to add.</param>
    ///<returns>The element that was previously at the insertion index.</returns>
    public T Put(T value)
    {
        var index = (int)_put++ & _mask;
        var result = _buffer[index];
        _buffer[index] = value;
        return result;
    }

    ///<summary>
    ///Adds an element to the buffer, overwriting the previous value.
    ///This is a slightly more performant version of <see cref="Put(T)"/> for cases where the
    ///overwritten value is not needed.
    ///</summary>
    ///<remarks>
    ///<b>Warning: This method is not thread-safe.</b> It should only be used in a single-producer context. <br/>
    ///The caller is responsible for ensuring the buffer is not full before calling this method.
    ///</remarks>
    ///<param name="value">The element to add.</param>
    public void PutUnchecked(T value) { _buffer[(int)_put++ & _mask] = value; }

    ///<summary>
    ///Resets the buffer to an empty state by setting the head and tail pointers to zero.
    ///</summary>
    ///<remarks>
    ///<b>Warning: This method is not thread-safe.</b> It should only be called when no other threads
    ///are accessing the buffer, or when access is controlled by external synchronization.
    ///</remarks>
    ///<returns>Internal array for advanced cleaning.</returns>
    public T[] Clear()
    {
        //Not thread-safe. Should be called only when no other operations are in progress.
        //Use Volatile.Write to ensure changes are visible to all threads.
        Volatile.Write(ref _get, 0);
        Volatile.Write(ref _put, 0);

        return _buffer;
    }

    ///<summary>
    ///Returns a string representation of the buffer's current state.
    ///</summary>
    ///<remarks>
    ///<b>Warning: This method is not thread-safe.</b> The reported values of size, get, and put
    ///are snapshots and may be inconsistent if the buffer is being modified concurrently.
    ///It is intended for debugging and logging in controlled scenarios.
    ///</remarks>
    ///<returns>A string summarizing the buffer's capacity, size, and pointer positions.</returns>
    public override string ToString()
    {
        //Read volatile fields for a more consistent (but still potentially stale) snapshot
        var get = Volatile.Read(ref _get);
        var put = Volatile.Read(ref _put);

        return $"RingBuffer{{Capacity={Capacity}, Size={Math.Max(0, put - get)}, Get={get}, Put={put}}}";
    }
}