
using System;

namespace JQuant
{
    
    /// <summary>
    /// implements cyclic buffer
    /// inherit the class and add functionality and protection locks
    /// as required
    /// </summary>
    public class CyclicBuffer<DataType> :
        System.Collections.Generic.IEnumerable<DataType>
    {
        /// <summary>
        /// This class should be inherited before using
        /// </summary>
        /// <param name="size">
        /// A <see cref="System.Int32"/>
        /// Size of the buffer
        /// </param>
        protected CyclicBuffer(int size)
        {
            tail = 0;
            head = 0;
            Size = size;
            buffer = new DataType[size];
        }

        /// <summary>
        /// add object to the head
        /// </summary>
        public void Add(DataType o)
        {
            buffer[head] = o;
            
            head = IncIndex(head, Size);
            
            if (Count < Size)
            {
                Count++;
            }
        }

        /// <summary>
        /// remove object from the tail
        /// </summary>
        public DataType Remove()
        {
            DataType o = default(DataType);
            if (Count > 0)
            {
                Count--;
                o = buffer[tail];
                tail = IncIndex(tail, Size);
            }
            return o;
        }

        public bool Empty()
        {
            return (Count == 0);
        }
        
        public bool Full()
        {
            return (Count == Size);
        }
        
        public bool NotEmpty()
        {
            return (Count != 0);
        }
        
        public int Size
        {
            get;
            protected set;
        }

        public int Count
        {
            get;
            protected set;
        }
        
        protected static int IncIndex(int index, int size)
        {
            index++;
            if (index >= size)
            {
                index = 0;
            }
            return index;
        }
                
        protected static int DecIndex(int index, int size)
        {
            index--;
            if (index < 0)
            {
                index = size-1;
            }
            return index;
        }
                
        /// <value>
        /// currently does not follow the order. just return the entries in the buffer 
        /// </value>
        protected System.Collections.Generic.IEnumerator<DataType> Entry
        {
            get
            {
                for (int i = 0; i < Count; i++)
                {
                    yield return buffer[i];
                }
            }
        }
        
        public System.Collections.Generic.IEnumerator<DataType> GetEnumerator()
        {
            return Entry;
        }
        
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return Entry;
        }
        
        protected DataType[] buffer;
        protected int tail;
        protected int head;
    }

}
