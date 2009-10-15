
using System;

namespace JQuant
{
    
    /// <summary>
    /// implements cyclic buffer
    /// </summary>
    public class CyclicBuffer<DataType>
    {        
        public CyclicBuffer(int size)
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
                
        
        protected DataType[] buffer;
        protected int tail;
        protected int head;
    }

}
