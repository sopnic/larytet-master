using System;
using System.Collections.Generic;
using System.Collections.Specialized;


namespace JQuant
{
       
       
        /// <summary>
        /// Generic pool
        /// Pool is a stack of objects.
        /// The API is thread safe. Size of the pool is fixed. Initially the pool is empty.
        /// To fill the pool one of the "Fill" methods should be called
        /// </summary>
        public class Pool<ObjectType> : Stack<ObjectType>, IPool, IDisposable
        {
               
                /// <summary>
                /// Pool.Fill will call this method to create objects and fill the pool initially
                /// Argument firstCall will true in the first call and false in all consequent calls
                /// </summary>
                public delegate bool ObjectFactory(bool firstCall, out ObjectType obj);
               
               
                /// <summary>
                ///
                /// </summary>
                /// <param name="name">
                /// A <see cref="System.String"/>
                /// </param>
                /// <param name="capacity">
                /// A <see cref="System.Int32"/>
                /// </param>
                public Pool(string name, int capacity) : base(capacity)
                {
                        _capacity = capacity;
                        _name = name;

                        _minCount = _capacity;
                        _allocOk = 0;
                        _allocFailed = 0;
                        _freeOk = 0;
                       
                        // add myself to the list of created mailboxes
                        Resources.Pools.Add(this);
                       
                }

                public void Dispose()
                {
                        Clear();
                       
                        // remove myself from the list of created mailboxes
                        Resources.Pools.Remove(this);  
                }
               
                ~Pool()
                {
                        Clear();
                        Console.WriteLine("Pool "+GetName()+" destroyed");
                }
               
                public void Fill(ObjectType obj)
                {
                        lock (this) {
                                if (Count >= _capacity) {
                                        Console.WriteLine("Too many objects added to the pool "+_name);
                                }
                                else {
                                        Push(obj);      
                                }
                        }
                }
               
               
                public void Fill(ObjectFactory factory)
                {
                        ObjectType obj;
                        bool result = factory(true, out obj);
                        while (result)
                        {
                                lock (this) {
                                        if (Count >= _capacity) {
                                                Console.WriteLine("Too many objects added to the pool "+_name);
                                                break;
                                        }
                                        Push(obj);      
                                }
                                result = factory(true, out obj);
                        }
                }
               
                /// <summary>
                /// allocate object from the pool
                /// </summary>
                /// <param name="obj">
                /// A <see cref="ObjectType"/>
                /// </param>
                /// <returns>
                /// A <see cref="System.Boolean"/>
                /// True if an object is allocated from the pool
                /// </returns>
                public bool Get(out ObjectType obj)
                {
                        bool result = false;
                        lock (this) {
                                if (Count > 0) {
                                        obj = Pop();
                                        result = true;
                                        _allocOk++;
                                        if (_minCount < Count) {
                                                _minCount++;
                                        }
                                }
                                else {
                                        _allocFailed++;
                                }
                        }
                       
                        return result;
                }

                public bool Free(ObjectType obj)
                {
                        bool result = false;
                        lock (this) {
                                if (Count < _capacity) {
                                        Push(obj);
                                        result = true;
                                        _freeOk++;
                                }
                        }
                       
                        return result;
                }
               
                public string GetName()
                {
                        return _name;
                }
               
                public int GetCapacity()
                {
                        return _capacity;
                }
               
                public int GetCount()
                {
                        return Count;
                }
               
                public int GetMinCount()
                {
                        return _minCount;
                }
               
                public int GetAllocOk()
                {
                        return _allocOk;
                }
               
                public int GetAllocFailed()
                {
                        return _allocFailed;
                }
               
                public int GetFreeOk()
                {
                        return _freeOk;
                }
               

				// statistics 
                private int _capacity;
                private string _name;
                private int _minCount;
                private int _allocOk;
                private int _allocFailed;
                private int _freeOk;               
        }
}
