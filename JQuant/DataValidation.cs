
namespace FMRShell
{

    /// <summary>
    /// Data validation in the simplest case is synchronous action which takes place
    /// between data producer, like Collector and data handler (sink) like logger
    /// Data validation can contain a state machine which tracks previous events and
    /// flags the bad data based on the previous history
    /// </summary>
    public abstract class DataValidator : JQuant.INamedResource, JQuant.IResourceStatistics
    {
        // child will implment public constructor and set Name
        protected DataValidator()
        {
        }

        public string Name
        {
            get;
            set;
        }

        public abstract void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values);

    } // class DataValidator

    

    
} // namespace FMRShell