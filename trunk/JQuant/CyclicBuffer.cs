
using System;

namespace JQuant
{
    
    /// <summary>
    /// implements cyclic buffer
    /// </summary>
    public class CyclicBuffer
    {        
        public CyclicBuffer(int size)
        {
            tail = 0;
            head = 0;
            Size = size;
            buffer = new object[size];
        }

        /// <summary>
        /// add object to the head
        /// </summary>
        public void Add(object o)
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
        public object Remove()
        {
            object o = null;
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
                
        
        protected object[] buffer;
        protected int tail;
        protected int head;
    }

    /// <summary>
    /// Cyclic buffers of integers
    /// Can be used to calculate average over last X minutes
    /// </summary>
    public class IntStatistics: JQuant.CyclicBuffer
    {
        public IntStatistics(string name, int size)
            : base(size)
        {
            summ = 0;
            Max = System.Int32.MinValue;
            Min = System.Int32.MaxValue;
            Name = name;
        }

        public void Add(int val)
        {
            if (Count < Size)
            {
                Count++;
            }
            else
            {
                summ -= (int)(buffer[head]);
                
            }
            summ += val;                    
            buffer[head] = val;
            
            head = IncIndex(head, Size);
            
            if (val > Max)
            {
                Max = val;
            }
            
            if (val < Min)
            {
                Min = val;
            }
        }

        protected void Add(object o)
        {
            base.Add(o);
        }

        protected object Remove()
        {
            object o = base.Remove();
            return o;
        }


        public int Min
        {
            get;
            protected set;
        }
        
        public int Max
        {
            get;
            protected set;
        }
        
        public double Mean
        {
            get
            {
                double mean = summ/Count;
                return mean;
            }
            protected set
            {
            }
        }

        public string Name
        {
            get;
            protected set;
        }

        int summ;
    }
    
}
