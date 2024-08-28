//MIT License
//
//Copyright © 2020 Chikirev Sirguy, Unirail Group. All rights reserved.
//For inquiries, please contact:  al8v5C6HU4UtqE9@gmail.com
//GitHub Repository: https://github.com/AdHoc-Protocol
//
//Permission is hereby granted, free of charge, to any person obtaining a copy
//of this software and associated documentation files (the "Software"), to use,
//copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
//the Software, and to permit others to do so, under the following conditions:
//
//1. The above copyright notice and this permission notice must be included in all
//   copies or substantial portions of the Software.
//
//2. Users of the Software must provide a clear acknowledgment in their user
//   documentation or other materials that their solution includes or is based on
//   this Software. This acknowledgment should be prominent and easily visible,
//   and can be formatted as follows:
//   "This product includes software developed by Chikirev Sirguy and the Unirail Group
//   (https://github.com/AdHoc-Protocol)."
//
//3. If you modify the Software and distribute it, you must include a prominent notice
//   stating that you have changed the Software.
//
//THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//FITNESS FOR A PARTICULAR PURPOSE, AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
//AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES, OR OTHER
//LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT, OR OTHERWISE, ARISING FROM,
//OUT OF, OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
//SOFTWARE.

using System.Threading;

namespace org.unirail.collections;

public class RingBuffer<T>{
    //Buffer to hold the data
    private readonly T[] buffer;

    //Mask for wrapping around the buffer
    private readonly uint mask;

    //Constant for the delay in spin wait
    private const int SpinWaitDelay = 10;

    //Lock state for synchronization
    private volatile int lockState;

    //Read index for the buffer
    private volatile uint readIndex;

    //Write index for the buffer
    private volatile uint writeIndex;

    //Constructor that initializes the buffer and mask
    public RingBuffer(int powerOfTwo) => buffer = new T[(mask = (1U << powerOfTwo) - 1) + 1];

    //Property to get the length of the buffer
    public int Length => buffer.Length;

    //Property to get the size of the buffer
    public int Count => (int)(writeIndex - readIndex);

    //Method to get data from the buffer in a multithreaded environment
    public bool GetMultiThreaded(ref T value)
    {
        //Spin wait until the lock is free
        while( Interlocked.Exchange(ref lockState, 1) == 1 )
            Thread.SpinWait(SpinWaitDelay);
        //Get the data from the buffer
        var result = Get(ref value);
        //Release the lock
        lockState = 0;
        return result;
    }

    //Method to get data from the buffer
    public bool Get(ref T value)
    {
        //If the read and write indices are the same, the buffer is empty
        if( readIndex == writeIndex )
            return false;
        //Get the data from the buffer and increment the read index
        value = buffer[(int)readIndex++ & mask];
        return true;
    }

    //Method to put data into the buffer in a multithreaded environment
    public bool PutMultiThreaded(T value)
    {
        //If the buffer is full, return false
        if( buffer.Length <= Count )
            return false;
        //Spin wait until the lock is free
        while( Interlocked.Exchange(ref lockState, 1) == 1 )
            Thread.SpinWait(SpinWaitDelay);
        //Put the data into the buffer
        var result = Put(value);
        //Release the lock
        lockState = 0;
        return result;
    }

    //Method to put data into the buffer
    public bool Put(T value)
    {
        //If the buffer is full, return false
        if( buffer.Length <= Count )
            return false;
        //Put the data into the buffer and increment the write index
        buffer[(int)writeIndex++ & mask] = value;
        return true;
    }

    //Method to clear the buffer
    public void Clear()
    {
        //Reset the read and write indices
        readIndex  = 0;
        writeIndex = 0;
    }
}