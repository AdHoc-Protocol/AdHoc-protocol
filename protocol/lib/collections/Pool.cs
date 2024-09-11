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

using System;
using System.Collections.Generic;

namespace org.unirail.collections;

//Class to implement a pool of objects of type T
public class Pool<T>
{
    //WeakReference to a Stack of items of type T
    //WeakReference allows the Garbage Collector to collect the object if there are no strong references to it
    private WeakReference<Stack<T>> items = new(new Stack<T>(5));

    //Factory method to create new instances of type T
    private readonly Func<T> factory;

    //Constructor that takes a factory method as parameter
    public Pool(Func<T> factory) => this.factory = factory;

    //Method to get an item from the pool
    public T get()
    {
        //Try to get the target Stack from the WeakReference
        //If successful, try to pop an item from the Stack
        //If unsuccessful, create a new item using the factory method
        return items.TryGetTarget(out var i) && i.TryPop(out var item) ? item : factory();
    }

    //Method to put an item back into the pool
    public void put(T item)
    {
        //Try to get the target Stack from the WeakReference
        //If unsuccessful, create a new Stack and update the WeakReference
        if (!items.TryGetTarget(out var s))
            items = new WeakReference<Stack<T>>(s = new Stack<T>(5));

        //Push the item back into the Stack
        s.Push(item);
    }
}