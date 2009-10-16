
using System;

namespace JQuant
{
    /// <summary>
    /// implements cyclic buffer
    /// </summary>
    public class CyclicBuffer<DataType> :
        System.Collections.Generic.IEnumerable<DataType>
    {
        /// <summary>
        /// Syncronized CyclicBuffer - upper layer provides mutual exclusion API 
        /// </summary>
        /// <param name="size">
        /// A <see cref="System.Int32"/>
        /// Size of the buffer
        /// </param>
        public CyclicBuffer(int size, ICriticalSection sectionlock)
        {
            this.sectionlock = sectionlock;
            Init(size);
        }

        /// <summary>
        /// Not syncronized CyclicBuffer - upper layer takes care of locks 
        /// </summary>
        /// <param name="size">
        /// A <see cref="System.Int32"/>
        /// Size of the buffer
        /// </param>
        public CyclicBuffer(int size)
        {
            this.sectionlock = dummyCriticalSection;
            Init(size);
        }

        protected void Init(int size)
        {
            Size = size;
            buffer = new DataType[size];
            Reset();
        }
        
        public void Reset()
        {
            sectionlock.Enter();
            
            tail = 0;
            head = 0;
            Count = 0;
            
            sectionlock.Exit();
        }

        /// <summary>
        /// add object to the head
        /// </summary>
        public void Add(DataType o)
        {
            sectionlock.Enter();
            
            buffer[head] = o;
            
            head = IncIndex(head, Size);
            
            if (Count < Size)
            {
                Count++;
            }
            
            sectionlock.Exit();
        }

        /// <summary>
        /// remove object from the tail
        /// </summary>
        public DataType Remove()
        {
            sectionlock.Enter();
            
            DataType o = default(DataType);
            if (Count > 0)
            {
                Count--;
                o = buffer[tail];
                tail = IncIndex(tail, Size);
            }
            
            sectionlock.Exit();
            
            
            return o;
        }

        /// <summary>
        /// returns true if CyclicBuffer is empty
        /// </summary>
        public bool Empty()
        {
            return (Count == 0);
        }
        
        /// <summary>
        /// returns true if CyclicBuffer is full - number of stored elements
        /// is equal to size
        /// </summary>
        public bool Full()
        {
            return (Count == Size);
        }
        
        /// <summary>
        /// returns true if CyclicBuffer contains at least one element
        /// </summary>
        public bool NotEmpty()
        {
            return (Count != 0);
        }
        
        /// <summary>
        /// size of the cyclic buffer
        /// </summary>
        public int Size
        {
            get;
            protected set;
        }

        /// <summary>
        /// number of elements in the cyclic buffer
        /// </summary>
        public int Count
        {
            get;
            protected set;
        }
        
        /// <summary>
        /// increment index and take care of wrap around
        /// </summary>
        protected static int IncIndex(int index, int size)
        {
            index++;
            if (index >= size)
            {
                index = 0;
            }
            return index;
        }
                
        /// <summary>
        /// decrement index and take care of wrap around
        /// </summary>
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
        /// When in foreach Reset is called once, followed by MoveNext-Current sequence
        /// second call is MoveNext
        /// 
        /// Upper layer should take care of proper synchronization between Add/Remove/foreach
        /// </value>
        protected class Enumerator : System.Collections.Generic.IEnumerator<DataType>
        {
            public Enumerator(CyclicBuffer<DataType> cb)
            {
                this.cb = cb;
                Reset();
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                // MoveNext will return false if there is nothing more
                // in the buffer to return - all elements handled
                bool result = (count < cb.Count);
                
                if (result)
                {
                    index = IncIndex(index, cb.Size);

                    // number of returned elements so far
                    count++;
                }

                return result;
            }

            public void Reset()
            {
                count = 0;
                
                // in the full cyclic buffer next element after head is oldest
                // if not full - oldest element is at zero
                if (cb.Full()) 
                {
                    index = cb.head;

                    // I want MoveNext to do the same thing - increment index
                    // I have to start from one element less (from -1)
                    index = DecIndex(index, cb.Size);
                }
                else
                {
                    index = 0;
                }
            }

            public DataType Current
            {
                get
                {
                    return cb.buffer[index];
                }                    
            }

            /// <value>
            /// this is explicit property required by System.Collections.IEnumerable
            /// will not be called 
            /// </value>
            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return Current;
                }                    
            }

            /// <summary>
            /// reference to the cyclicbuffer
            /// </summary>
            protected CyclicBuffer<DataType> cb;

            /// <summary>
            /// keeps where I am now
            /// </summary>
            protected int index;

            /// <summary>
            /// keeps how many elements were returned so far
            /// </summary>
            protected int count;
        }

        /// <summary>
        /// Calling thread should take care of proper synchronization 
        /// between Add/Remove/foreach
        /// This method is part of IEnumerable interface
        /// </summary>
        public System.Collections.Generic.IEnumerator<DataType> GetEnumerator()
        {
            return new Enumerator(this);
        }
        
        /// <value>
        /// this method is required by System.Collections.IEnumerable
        /// will not be called 
        /// </value>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }
        
        protected DataType[] buffer;
        protected int tail;
        protected int head;
        protected ICriticalSection sectionlock;
        protected static DummyCriticalSection dummyCriticalSection = new DummyCriticalSection();
    }


    public class CyclicBufferSynchronized<DataType> : CyclicBuffer<DataType>
    {
        public CyclicBufferSynchronized(string name, int size)
            : base(size)
        {
            sectionlock = new LockCriticalSection(name, this);
        }
        
    }

}
