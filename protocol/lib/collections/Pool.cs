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
using System.Collections.Generic;

namespace org.unirail.collections;

///<summary>
///<para>Implements a simple object pool for reusable instances of type <typeparamref name="T"/>.</para>
///<para>
///This pool helps to reduce garbage collection overhead and improve performance by reusing objects instead of constantly creating new ones,
///especially for frequently used objects that are expensive to create.
///</para>
///<para>
///The pool is backed by a <see cref="Stack{T}"/> to store and retrieve objects in a LIFO (Last-In, First-Out) manner.
///A <see cref="WeakReference{T}"/> is used to hold the stack, allowing the stack (and thus the pooled objects if they are only referenced from the stack)
///to be garbage collected if the <see cref="Pool{T}"/> itself is no longer strongly referenced and under memory pressure.
///</para>
///<para>
///**Important:** This implementation is **NOT thread-safe**. If you need to use a pool in a multi-threaded environment,
///you will need to add synchronization mechanisms (e.g., locks) to the <see cref="get"/> and <see cref="put"/> methods.
///</para>
///</summary>
///<typeparam name="T">The type of objects to pool. Must be a class with a parameterless constructor or a factory must be provided.</typeparam>
public class Pool<T>
{
    ///<summary>
    ///WeakReference to a Stack of pooled items of type T.
    ///Using WeakReference allows the Garbage Collector to reclaim the Stack (and the objects in it, if they are only referenced weakly)
    ///if the Pool itself is not strongly referenced and memory is needed. This can be beneficial in long-running applications to prevent unbounded memory usage
    ///if the pool is not actively used for extended periods.
    ///</summary>
    private WeakReference<Stack<T>> _items;

    ///<summary>
    ///Factory method to create new instances of type T when the pool is empty.
    ///This delegate is invoked by the <see cref="get"/> method when no objects are available in the pool.
    ///</summary>
    private readonly Func<T> _factory;

    ///<summary>
    ///Initializes a new instance of the <see cref="Pool{T}"/> class with the specified factory method.
    ///</summary>
    ///<param name="factory">The factory method used to create new instances of <typeparamref name="T"/> when the pool is empty.
    ///This method should encapsulate the logic for creating a new <typeparamref name="T"/> object.</param>
    ///<exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
    public Pool(Func<T> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _items = new WeakReference<Stack<T>>(new Stack<T>(5)); //Initialize WeakReference with a new Stack, setting initial capacity to 5.
    }

    ///<summary>
    ///Initializes a new instance of the <see cref="Pool{T}"/> class with the specified factory method and initial capacity of the internal stack.
    ///</summary>
    ///<param name="factory">The factory method used to create new instances of <typeparamref name="T"/> when the pool is empty.</param>
    ///<param name="initialCapacity">The initial capacity of the underlying <see cref="Stack{T}"/>. Use a value greater than 0 to pre-allocate stack capacity; otherwise, use the default capacity.</param>
    ///<exception cref="ArgumentNullException">Thrown if <paramref name="factory"/> is null.</exception>
    ///<exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="initialCapacity"/> is less than 0.</exception>
    public Pool(Func<T> factory, int initialCapacity)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        if (initialCapacity < 0)
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity cannot be negative.");
        _items = new WeakReference<Stack<T>>(new Stack<T>(initialCapacity > 0 ? initialCapacity : 5)); //Initialize WeakReference with a new Stack and specified initial capacity.
    }

    ///<summary>
    ///Retrieves an item from the pool. If the pool is not empty, an existing item is returned.
    ///Otherwise, a new item is created using the factory method.
    ///</summary>
    ///<returns>An instance of <typeparamref name="T"/>, either retrieved from the pool or newly created.</returns>
    public T get()
    {
        //Try to get the target Stack from the WeakReference.
        //If the WeakReference is still pointing to a Stack (i.e., the Stack has not been garbage collected yet), proceed.
        if (_items.TryGetTarget(out var stack))
        {
            //Try to pop an item from the Stack.
            //If the Stack is not empty (i.e., TryPop is successful), return the popped item.
            if (stack.TryPop(out var item))
                return item;
        }

        //If the WeakReference's target is lost (Stack was GC'd) or the Stack was empty,
        //create a new item using the factory method and return it.
        return _factory();
    }

    ///<summary>
    ///Returns an item to the pool, making it available for reuse.
    ///The item is pushed onto the stack for subsequent retrieval by the <see cref="get"/> method.
    ///</summary>
    ///<param name="item">The item to return to the pool. Must not be null.</param>
    ///<exception cref="ArgumentNullException">Thrown if <paramref name="item"/> is null.</exception>
    public void put(T item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item), "Item cannot be null.");

        //Try to get the target Stack from the WeakReference.
        if (!_items.TryGetTarget(out var stack))
        {
            //If the WeakReference's target is lost (Stack was GC'd), create a new Stack and update the WeakReference to point to it.
            _items = new WeakReference<Stack<T>>(stack = new Stack<T>(5)); //Re-initialize WeakReference with a new Stack.
        }

        //Push the item back into the Stack, making it available for the next get operation.
        stack.Push(item);
    }
}