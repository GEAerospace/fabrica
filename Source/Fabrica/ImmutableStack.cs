// GE Aviation Systems LLC licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GEAviation.Fabrica 
{
    /// <summary>
    /// This struct represents an Immuatable Struct. Each time the 
    /// stack is modified, a new <see cref="ImmutableStack{DataType}"/> is generated.
    /// This allows for scenarios where the state of a struct doesn't have to managed,
    /// such as during a tree traversal.
    /// </summary>
    /// <typeparam name="DataType">
    /// The item types.
    /// </typeparam>
    public struct ImmutableStack<DataType> : IEnumerable<DataType>
    {
        private DataType[] mStackStore;

        /// <summary>
        /// Creates a new stack.
        /// </summary>
        /// <param name="aItem">
        /// The first item for the stack.
        /// </param>
        public ImmutableStack( DataType aItem )
            : this( new DataType[1] {aItem})
        { }

        /// <summary>
        /// Used privately to create new copies of 
        /// the stack when it's operated on.
        /// </summary>
        /// <param name="aStack">
        /// The new stack.
        /// </param>
        private ImmutableStack(DataType[] aStack)
        {
            mStackStore = aStack;
        }

        /// <summary>
        /// Checks the stack to see if a particular item is within the stack.
        /// </summary>
        /// <param name="aItem">
        /// The item to look for.
        /// </param>
        /// <returns>
        /// True if the specified item is already in the stack, false otherwise.
        /// </returns>
        public bool containsItem( DataType aItem )
        {
            return mStackStore.Contains( aItem );
        }

        /// <summary>
        /// Provides a count of the number of items already in the stack.
        /// </summary>
        /// <returns></returns>
        public int count()
        {
            return mStackStore.Length;
        }

        /// <summary>
        /// Pushes a new item onto the stack, and returns the new stack.
        /// This does not modify the stack being operated on.
        /// </summary>
        /// <param name="aItem">
        /// The item to push onto the stack.
        /// </param>
        /// <returns>
        /// The new stack with the pushed item.
        /// </returns>
        public ImmutableStack<DataType> push( DataType aItem )
        {
            DataType[] lNewArray = new DataType[mStackStore.Length + 1];
            mStackStore.CopyTo( lNewArray, 0 );
            lNewArray[lNewArray.Length - 1] = aItem;
            return new ImmutableStack<DataType>( lNewArray );
        }

        /// <summary>
        /// Provides a look at the item currently on the top of the stack.
        /// </summary>
        /// <returns>
        /// The stack's top item.
        /// </returns>
        public DataType peek()
        {
            return mStackStore[mStackStore.Length - 1];
        }

        /// <summary>
        /// Pops an item off the top of the stack.
        /// This does not modify the stack being operated on.
        /// Cannot pop the last item off of the stack.
        /// </summary>
        /// <returns>
        /// The new stack with the remaining items.
        /// </returns>
        public ImmutableStack<DataType> pop()
        {
            if( mStackStore.Length >= 2 )
            {
                return new ImmutableStack<DataType>( mStackStore.Take( mStackStore.Length - 1 ).ToArray() );
            }

            throw new InvalidOperationException( "Cannot pop last element of the stack." );
        }

        /// <inheritdoc/>
        public IEnumerator<DataType> GetEnumerator()
        {
            return mStackStore.Reverse().GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return mStackStore.Reverse().GetEnumerator();
        }
    }
}
