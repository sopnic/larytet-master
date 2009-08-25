using System;

namespace JQuant
{
	/// <summary>
	/// sink is a data consumer. the interface assumes asynchronous processing
	/// usually Notify() will send add the data object to the internal queue for 
	/// the future processing and get out
	/// Sink registers itslef in the producer. Producer calls Notify()
	/// Different approach would be to use delegated method. I prefer interface - in 
	/// the future may be I will need more methods like real-time priority notification
	/// </summary>
    public interface ISink<DataType>
	{
		/// <summary>
		/// Producer calls the method to notfy consumer that there is new data available
		/// Notify() should be non-blocking - there are more than one sink to be served 
		/// by the producer
		/// </summary>
		/// <param name="count">
		/// A <see cref="System.Int32"/>
		/// Number of objects available
		/// </param>
		void Notify(int count, DataType data);
	}
	
    public interface IProducer<DataType>
    {
        bool AddSink(ISink<DataType> sink);
        bool RemoveSink(ISink<DataType> sink);
    }	
}
