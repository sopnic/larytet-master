using System;
using System.Reflection;
using System.Threading;
using System.IO;


/// <summary>
/// I want to run TaskBarLib simulation. I am going to implement API using prerecorded
/// or generated in some other way log files as a data feed
/// Only small part of the API is implemented
/// This class is used by the FMRShell and allows to simulate different scenarios
/// and play back previously recorded log files
/// In the future this class will contain engines which simulate behaviour of the
/// real system
///
/// Side note. Another approach to the simulation is to install a real server running
/// simulator and let the rest of the application including 3rd party code (DLLs from
/// FMR) use the simulation server. At this point it looks like an overkill. Thanks to
/// the time stamps i can simulate the system behaviour fairly close.
/// </summary>
namespace TaskBarLibSim
{
    public enum MonthType
    {
        AllMonths = -1,
        April = 4,
        August = 8,
        December = 12,
        February = 2,
        January = 1,
        July = 7,
        June = 6,
        March = 3,
        May = 5,
        November = 11,
        October = 10,
        September = 9
    }

    public enum MadadTypes
    {
        AllMadad = -1,
        BANK = 4,
        BINUI = 6,
        CURRENCYLNKD = 0x17,
        DOLLAR = 5,
        GOV_CHG0 = 20,
        GOV_CHG5 = 0x15,
        GOV_FIXED0 = 0x11,
        GOV_FIXED2 = 0x12,
        GOV_FIXED5 = 0x13,
        MAALE = 14,
        MAKAM = 0x16,
        TELBOND = 11,
        TELBOND40 = 15,
        TELBOND60 = 0x10,
        TELDIV20 = 10,
        TLTK = 2,
        TLV100 = 3,
        TLV25 = 0,
        TLV75 = 1,
        TLVFIN = 7,
        TLVNADLAN15 = 8,
        YETER120 = 12,
        YETER30 = 9,
        YETER50 = 13
    }

    public enum K300StreamType
    {
        IndexStream = 0x34,
        MaofCNTStream = 50,  //out of date, not used anymore
        MaofStream = 0x30,
        RezefCNTStream = 0x33,  //out of date, not used anymore
        RezefStream = 0x31
    }

    public enum StockKind
    {
        StockKindAgach = 1,
        StockKindAll = -1,
        StockKindKeren = 3,
        StockKindMakam = 2,
        StockKindMenaya = 0
    }

    public enum LoginStatus
    {
        LoginSessionActive,
        LoginSessionInProgress,
        LoginSessionInactive,
        LoginSessionDBInitFailure,
        LoginSessionAS400Failure,
        LoginSessionPasswordExpired,
        LoginSessionPasswordChangeFailure,
        LoginSessionPasswordChangedToday,
        LoginSessionWrongUserOrPassword,
        LoginSessionMaxUsersLimit,
        LoginSessionReLogin,
        LoginSessionTaskBarBlocked
    }

    public enum OnlineSessionType
    {
        OnlineSessionTypeAccounts,
        OnlineSessionTypeAll,
        OnlineSessionTypeCustodian,
        OnlineSessionTypeKranot,
        OnlineSessionTypeKupa
    }

    public enum QueryType
    {
        qtDetailed = 0x30,
        qtSummary = 0x31
    }

    public enum YieldDataType
    {
        YieldData5YearsbyYear = 4,
        YieldDataMonthbyDay = 1,
        YieldDataTwelveMonths = 3,
        YieldDataYearbyMonth = 2,
        YieldDataYearbyQuater = 5
    }

    /// <summary>
    /// Query types enumaration
    /// </summary>
    public enum ConnectionState
    {
        csOpen,
        csProcessing,
        csClosed
    }

    public enum LoginLevel
    {
        LoginLevelAccounts = 3,
        LoginLevelMax = 50,
        LoginLevelOptionsStocks = 2,
        LoginLevelPermissions = 1
    }

    /// <summary>
    /// This type is represents the timestamps from AS400 servers
    /// </summary>
    public struct AS400DateTime
    {
        public int year;
        public int Month;
        public int day;
        public int hour;
        public int minute;
        public int second;
        public int ms;
    }

    public struct K300MaofType
    {
        public string SUG_REC;
        public string TRADE_METH;
        public string BNO_Num;
        public string LAST_REC;
        public string SIDURI_Num;
        public string SYMBOL_E;
        public string Symbol;
        public string BNO_NAME_E;
        public string BNO_NAME;
        public string BRANCH_NO;
        public string BRANCH_U;
        public string SUG_BNO;
        public string MIN_UNIT;
        public string HARIG_NV;
        public string MIN_PR;
        public string MAX_PR;
        public string BASIS_PRC;
        public string BASIS_COD;
        public string STATUS_COD;
        public string EX_DATE;
        public string EX_PRC;
        public string VL_MULT;
        public string VL_COD;
        public string ZERO_COD;
        public string shlav;
        public string STATUS;
        public string TRD_STP_CD;
        public string TRD_STP_N;
        public string STP_OPN_TM;
        public string LMT_BY1;
        public string LMT_BY2;
        public string LMT_BY3;
        public string LMY_BY1_NV;
        public string LMY_BY2_NV;
        public string LMY_BY3_NV;
        public string RWR_FE;
        public string LMT_SL1;
        public string LMT_SL2;
        public string LMT_SL3;
        public string LMY_SL1_NV;
        public string LMY_SL2_NV;
        public string LMY_SL3_NV;
        public string RWR_FF;
        public string PRC;
        public string COD_PRC;
        public string SUG_PRC;
        public string LST_DF_BS;
        public string RWR_FG;
        public string LST_DL_PR;
        public string LST_DL_TM;
        public string LST_DL_VL;
        public string DAY_VL;
        public string DAY_VL_NIS;
        public string DAY_DIL_NO;
        public string RWR_FH;
        public string DAY_MAX_PR;
        public string DAY_MIN_PR;
        public string POS_OPN;
        public string POS_OPN_DF;
        public string STS_NXT_DY;
        public string UPD_DAT;
        public string UPD_TIME;
        public string FILER;
    }

    public struct K300RzfType
    {
        public string SUG_REC;
        public string BNO_Num;
        public string BNO_NAME;
        public string Symbol;
        public string TRADE_METH;
        public string SIDURI_Num;
        public string RWR_VA;
        public string MIN_UNIT;
        public string HARIG_NV;
        public string MIN_PR_OPN;
        public string MAX_PR_OPN;
        public string MIN_PR_CNT;
        public string MAX_PR_CNT;
        public string BASIS_PRC;
        public string STATUS;
        public string EX_COD;
        public string EX_DETAIL;
        public string RWR_VB;
        public string shlav;
        public string LAST_PRC;
        public string TRD_STP_N;
        public string STP_OPN_TM;
        public string RWR_VD;
        public string LMT_BY1;
        public string LMT_BY2;
        public string LMT_BY3;
        public string LMY_BY1_NV;
        public string LMY_BY2_NV;
        public string LMY_BY3_NV;
        public string MKT_NV_BY;
        public string MKT_NV_BY_NUM;
        public string RWR_VE;
        public string LMT_SL1;
        public string LMT_SL2;
        public string LMT_SL3;
        public string LMY_SL1_NV;
        public string LMY_SL2_NV;
        public string LMY_SL3_NV;
        public string MKT_NV_SL;
        public string MKT_NV_SL_NUM;
        public string RWR_VF;
        public string THEOR_PR;
        public string THEOR_VL;
        public string RWR_VG;
        public string LST_DL_PR;
        public string LST_DL_TM;
        public string LST_DF_BS;
        public string LST_DF_OPN;
        public string LST_DL_VL;
        public string DAY_VL;
        public string DAY_VL_NIS;
        public string DAY_DIL_NO;
        public string DAY_MAX_PR;
        public string DAY_MIN_PR;
        public string BNO_NAME_E;
        public string SYMBOL_E;
        public string STP_COD;
        public string COD_SHAAR;
        public string UPD_DAT;
        public string UPD_TIME;
    }

    public struct K300MadadType
    {
        public string SUG_RC;
        public string BNO_N;
        public string FIL1_VK;
        public string MDD_COD;
        public string MDD_SUG;
        public string MDD_N;
        public string FIL2_VK;
        public string MDD_NAME;
        public string Madad;
        public string FIL3_VK;
        public string MDD_DF;
        public string CALC_TIME;
        public string FIL6_VK;
        public string UPD_DAT;
        public string UPD_TIME;
    }

    /// <summary>
    /// Defines securitys' weights in different TASE indices
    /// (the weights change daily)
    /// </summary>
    public struct SH161Type
    {
        public long BNO;               // îñôø ðééø òøê    Security's TASE Id
        public string BNO_NAME;        // ùí ðééø òøê      security's Hebrew name
        public double PRC;             // îçéø             Base price (usually it's previous close adjusted for splits and dividends
        public double HON_RASHUM;      // äåï øùåí         registered capital
        public double PCNT;            // àçåæ             security's weight in the index
        public long MIN_NV;            // ëîåú îéðéîìéú    
        public double BNO_IN_MDD;      // îñôø îðéåú áîãã = îîî
        public double PUBLIC_PRCNT;    // àçåæ àçæ÷åú áöéáåø
        public double NV_TZAFA;        // ëîåú öôä = îîî* àçåæ àçæ÷åú áöéáåø
    }

    public delegate void IK300Event_FireMaofCNTEventHandler(ref Array psaStrRecords, ref int nRecords);
    public delegate void IK300Event_FireMaofEventHandler(ref Array psaStrRecords, ref int nRecords);
    public delegate void IK300Event_FireRezefCNTEventHandler(ref Array psaStrRecords, ref int nRecords);
    public delegate void IK300Event_FireRezefEventHandler(ref Array psaStrRecords, ref int nRecords);

    public interface IK300
    {
        int GetMAOF(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad);
        int GetMAOFRaw(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad);
        void StopUpdate(int pnID);
        int StartStream(K300StreamType streamType, string strStockNumber, MadadTypes strMadad, int withEvents);
        int K300StartStream(K300StreamType streamType);
        int K300StopStream(K300StreamType streamType);
    }

    public interface K300 : IK300, IK300Event_Event
    {
    }

    public interface IK300Event_Event
    {
        // Events
        /*event IK300Event_FireMaofEventHandler FireMaof;
        event IK300Event_FireMaofCNTEventHandler FireMaofCNT;
        event IK300Event_FireRezefEventHandler FireRezef;
        event IK300Event_FireRezefCNTEventHandler FireRezefCNT;*/
    }


    public interface IK300Events
    {
    }

    public interface K300Events : IK300Events, _IK300EventsEvents_Event
    {
    }

    public delegate void _IK300EventsEvents_OnMaofEventHandler(ref K300MaofType data);
    public delegate void _IK300EventsEvents_OnRezefEventHandler(ref K300RzfType data);
    public delegate void _IK300EventsEvents_OnMadadEventHandler(ref K300MadadType data);

    public interface _IK300EventsEvents_Event
    {
        // Events
        event _IK300EventsEvents_OnMaofEventHandler OnMaof;
        event _IK300EventsEvents_OnRezefEventHandler OnRezef;
    }

    public class K300Class : IK300, K300, IK300Event_Event
    {
        // Events - even though they appear here,
        // these events are out of date, use K300EventsClass onMaof / on Rezef / onMadad instead
        // we'll need to clean up some time
        /*public event IK300Event_FireMaofEventHandler FireMaof;
        public event IK300Event_FireMaofCNTEventHandler FireMaofCNT;
        public event IK300Event_FireRezefEventHandler FireRezef;
        public event IK300Event_FireRezefCNTEventHandler FireRezefCNT;*/

        public K300Class()
        {
            maofStreamStarted = false;
            rezefStreamStarted = false;
            SimulationTop.k300Class = this;
        }

        // Methods
        public virtual int GetMAOF(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad)
        {
            return 0;
        }

        public virtual int GetMAOFRaw(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad)
        {
            return 0;
        }

        public virtual int GetBaseAssets2(out System.Array psaRecords, int BaseAssetCode)
        {
            psaRecords = null;
            return -1;
        }

        public virtual int GetBaseAssets(out System.Array psaRecords, int BaseAssetCode)
        {
            psaRecords = null;
            return -1;
        }

        /// <summary>
        /// retreive weight of securities in indexes.
        /// </summary>
        /// <param name="vecRecords">Array in which <see cref="TaskBarLibSim.SH161Type"/>
        /// data is kept. Containing all the records for the index specified in parameter madadSymbol </param>
        /// <param name="madadSymbol">Only stocks included in this MadadType index will be retrieved. </param>
        /// <returns>Upon success the function returns the total 
        /// number of SH161Type records retrieved into the vecRecords array. 0 if no records were found. </returns>
        public virtual int GetSH161(ref System.Array vecRecords, MadadTypes madadSymbol)
        {
            vecRecords = null;
            return 0;
        }


        public virtual int K300StartStream(K300StreamType streamType)
        {
            switch (streamType)
            {
                case K300StreamType.MaofStream:
                    {
                        // start data generation thread
                        maofGenerator.Start();

                        // set flag to keep track of the started streams
                        maofStreamStarted = true;
                        break;
                    }

                case K300StreamType.RezefStream:
                    {
                        rezefGenerator.Start();
                        rezefStreamStarted = true;
                        break;
                    }

                case K300StreamType.IndexStream:
                    {
                        madadGenerator.Start();
                        madadStreamStarted = true;
                        break;
                    }
                
                default:
                    break;
            }
            return 0;
        }

        /// <summary>
        /// initializes the simulation
        /// actual simulation stream will start by the K300StartStream
        /// </summary>
        public static void InitStreamSimulation(ISimulationDataGenerator<K300MaofType> maofGenerator)
        {
            K300Class.maofGenerator = maofGenerator;
        }

        public static void InitStreamSimulation(ISimulationDataGenerator<K300RzfType> rzfGenerator)
        {
            K300Class.rezefGenerator = rzfGenerator;
        }

        public static void InitStreamSimulation(ISimulationDataGenerator<K300MadadType> mddGenerator)
        {
            K300Class.madadGenerator = mddGenerator;
        }

        public virtual int K300StopStream(K300StreamType streamType)
        {
            switch (streamType)
            {
                case K300StreamType.MaofStream:
                    maofGenerator.Stop();
                    break;

                case K300StreamType.RezefStream:
                    rezefGenerator.Stop();
                    break;

                case K300StreamType.IndexStream:
                    madadGenerator.Stop();
                    break;

                default:
                    break;
            }
            return 0;
        }

        public virtual int StartStream(K300StreamType streamType, string strStockNumber, MadadTypes strMadad, int withEvents)
        {
            maofGenerator.Start();
            return 0;
        }

        public virtual void StopUpdate(int pnID)
        {
        }

        public int K300SessionId
        {
            set;
            get;
        }

        protected bool maofStreamStarted;
        protected bool rezefStreamStarted;
        protected bool madadStreamStarted;
        protected static ISimulationDataGenerator<K300MaofType> maofGenerator;
        protected static ISimulationDataGenerator<K300RzfType> rezefGenerator;
        protected static ISimulationDataGenerator<K300MadadType> madadGenerator;
    }

    public class K300EventsClass : IK300Events, K300Events, _IK300EventsEvents_Event
    {

        public K300EventsClass()
        {
            SimulationTop.k300EventsClass = this;
        }

        // Events
        public event _IK300EventsEvents_OnMaofEventHandler OnMaof;
        public event _IK300EventsEvents_OnRezefEventHandler OnRezef;
        public event _IK300EventsEvents_OnMadadEventHandler OnMadad;

        /// <summary>
        /// part of the simulation
        /// send event to all registered users 
        /// </summary>
        public void SendEventMaof(ref K300MaofType data)
        {
            OnMaof(ref data);
        }

        /// <summary>
        /// Send simulated Rezef data to all registered users
        /// </summary>
        public void SendEventRzf(ref K300RzfType data)
        {
            OnRezef(ref data);
        }

        /// <summary>
        /// Send simulated Rezef data to all registered users
        /// </summary>
        public void SendEventMdd(ref K300MadadType data)
        {
            OnMadad(ref data);
        }

        // Properties - are used to filter the events data 
        public BaseAssetTypes EventsFilterBaseAsset { set; get; }
        public int EventsFilterBno { set; get; }
        public int EventsFilterMadad { set; get; }
        public int EventsFilterMaof { set; get; }
        public MonthType EventsFilterMonth { set; get; }
        public int EventsFilterRezef { set; get; }
        public StockKind EventsFilterStockKind { set; get; }
        public MadadTypes EventsFilterStockMadad { set; get; }
    }


    public partial class UserClass
    {
        public UserClass()
        {
            _loginProgress = 0;
            _loginStatus = LoginStatus.LoginSessionInactive;
        }

        public int Login(string username, string AS400Password, string AppPassword, out string message, out int sessionId)
        {
            message = "";
            _sessionId = 1;
            sessionId = _sessionId;
            _loginStatus = LoginStatus.LoginSessionInProgress;
            _loginStarted = DateTime.Now;

            return _sessionId;
        }

        public void GetLoginActivity(ref int sessionId, out int percent, out string description)
        {
            // simulation - if in progress move things                      
            switch (_loginStatus)
            {
                case LoginStatus.LoginSessionInactive:
                    // do nothing until Login() is not being called
                    break;
                case LoginStatus.LoginSessionActive:
                    // login done - nothing more is required
                    break;

                default:
                    TimeSpan ts = TimeSpan.FromSeconds(1);
                    DateTime current = DateTime.Now;
                    if ((_loginStarted + ts) <= current)
                    {
                        _loginProgress += 50;
                        _loginStarted = current;
                    }
                    if (_loginProgress >= 100)
                    {
                        _loginStatus = LoginStatus.LoginSessionActive;
                    }
                    break;
            }
            percent = _loginProgress;
            description = "";
            sessionId = _sessionId;
        }


        public LoginStatus get_LoginState(ref int sessionId)
        {
            return _loginStatus;
        }

        public string get_LoginErrorDesc(ref int sessionId)
        {
            return _loginErrorDesc;
        }

        /// <summary>
        /// Carries out the (simulated) AS400 logout process.
        /// </summary>
        /// <param name="SessionId">A <see cref="System.Int32"/>
        /// The logout process requires a 
        /// unique Session Identification number that identifies 
        /// the session to be closed.</param>
        /// <returns>A <see cref="System.Int32"/>
        /// In the event of failure -1 is returned from the function.
        /// In the event of success 0 is returned from the function.</returns>
        public int Logout(int SessionId)
        {
            bool success=true;
            if (success) return 0;
            else return -1;
        }

        //public properties:
        /// <summary>
        /// Relays the customer number for the current taskbar configuration.
        /// </summary>
        public virtual string Cust { get {return this._cust;}  }
        
        /// <summary>
        /// 
        /// </summary>
        public virtual ConnectionState State { get { return this._cs; } }

        public virtual string System { get {return this._system; }}

        public virtual string SystemName { get { return this._sysName;} }


        protected int _loginProgress;
        protected int _sessionId;
        protected string _loginErrorDesc;
        protected LoginStatus _loginStatus;
        protected DateTime _loginStarted;

        protected string _cust = "aryeh";
        protected ConnectionState _cs;
        protected string _system="TBSim";
        protected string _sysName="TaskBarLibSim";

    }

    public class ConfigClass
    {
        /// <summary>
        /// This function is used to get timestamp from AS400 server in order to compute
        /// roundrip times and latencies
        /// </summary>
        /// <param name="dt">A <see cref="TaskBarLibSim.AS400DateTime"/></param>
        /// <param name="latency">A <see cref="System.Int32"/></param>
        /// <returns>0 if success, -1 if failure</returns>
        public int GetAS400DateTime(out AS400DateTime dt, out int latency)
        {
            DateTime now = DateTime.Now;

            dt = new AS400DateTime();

            //fill the AS400DateTime struct with updated values
            dt.year = now.Year;
            dt.Month = now.Month;
            dt.day = now.Day;
            dt.hour = now.Hour;
            dt.minute = now.Minute;
            dt.second = now.Second;
            dt.ms = now.Millisecond;

            //An arbitrary value for latency
            latency = random.Next(15, 2*1000);

            countGetAS400DateTime++;
            
            // produce failure from time to time
            bool success = (random.Next(0,2) == 0);
            if (success) return 0;
            else return -1;
        }

        protected static int countGetAS400DateTime;
        protected static Random random = new Random();
    }



    // From this line down - simulation relates classes which are part of the TaskBarLib API

    /// <summary>
    /// this is not part of the FMR's TaskBarLib. Part of the simulation engine
    /// </summary>
    /// <param name="generator">
    /// A <see cref="ISimulationStreamGenerator"/>
    /// </param>
    public interface ISimulationDataGenerator<DataType>
    {
        void Start();

        void Stop();
    }

    /// <summary>
    /// a thread firing specified event
    /// </summary>
    public abstract class EventGenerator<DataType> : ISimulationDataGenerator<DataType>
    {
        public EventGenerator()
        {
            notStopped = true;
        }

        public virtual void Start()
        {
            notStopped = true;
            new Thread(Run).Start();
            return;
        }

        public virtual void Stop()
        {
            notStopped = false;
            return;
        }

        /// <summary>
        /// thread main loop 
        /// </summary>
        protected void Run()
        {
            while (notStopped)
            {
                DataType data;
                bool result = GetData(out data);

                if (!result)
                {
                    break;
                }
                SendEvents(ref data);
            }
        }


        /// <summary>
        /// Returns next chunk of data of type. If Ok return true 
        /// The method should block the calling thread until the next chunk
        /// of data is available or there is no more data will ever be available
        /// in the later case the method will return false
        /// </summary>
        /// <returns>
        /// A <see cref="System.Object"/>
        /// Data will be set to the next generated chunk
        /// </returns>
        protected abstract bool GetData(out DataType data);

        protected abstract void SendEvents(ref DataType data);

        private bool notStopped;
    }

    /// <summary>
    /// Use childs of this class, like MaofDataGeneratorLogFile
    /// This class provides OpenFile-ReadLinesFromFile-CallAbstractParser-SendData services
    /// </summary>
    public abstract class EventGeneratorPlayback<DataType> : EventGenerator<DataType>
    {
        protected EventGeneratorPlayback(string delimiter, string filename)
        {
            this.delimiter = delimiter;
            this.filename = filename;
            ReadyToGo = false;

            lastTimeSpan = default(TimeSpan);
            
            System.Console.WriteLine("Simulation playback data from "+filename);

            fileStream = default(FileStream);
            this.filename = filename;
            streamReader = default(StreamReader);

            Type t = typeof(DataType);
            fields = t.GetFields();
            
            do
            {
                try
                {
                    fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read);
                    streamReader = new StreamReader(fileStream);
                }
                catch (IOException e)
                {
                    System.Console.WriteLine("Failed to open file "+filename);
                    if (fileStream != default(FileStream))
                    {
                        fileStream.Close();
                        // help Garbage collector
                        streamReader = default(StreamReader);
                        fileStream = default(FileStream);
                        break;
                    }
                    System.Console.WriteLine(e.ToString());
                }
    
                bool res = CheckFile(fileStream);
                if (!res) break;
                
                ReadyToGo = true;
            }
            while (false);            
        }
        
        ~EventGeneratorPlayback()
        {
            // close the file I read from 
            if (fileStream != default(FileStream))
            {
                fileStream.Close();
                // help Garbage collector
                streamReader = default(StreamReader);
                fileStream = default(FileStream);
            }
        }

        /// <summary>
        /// Generic part of the log file parser - find next field in the line 
        /// </summary>
        /// <param name="src">
        /// A <see cref="System.String"/>
        /// Where to look for the field
        /// </param>
        /// <param name="from">
        /// A <see cref="System.Int32"/>
        /// Character to start to look from
        /// </param>
        /// <param name="field">
        /// A <see cref="System.String"/>
        /// Found field
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// True if a field was found
        /// </returns>
        protected bool getNextField(string src, ref int from, out string field)
        {
            
            bool res = false;
            field = "";

            do
            {
                if (from < 0)
                {
                    System.Console.WriteLine("EventGeneratorPlayback::getNextField from="+from);
                    System.Console.WriteLine("Failed to find delimiter " + delimiter + " position "+from + " line "+count);
                    System.Console.WriteLine("Line="+src);
                    break;
                }

                
                if (from > src.Length)
                {
                    System.Console.WriteLine("EventGeneratorPlayback::getNextField from="+from+" src.Length="+src.Length);
                    System.Console.WriteLine("Failed to find delimiter " + delimiter + " position "+from + " line "+count);
                    System.Console.WriteLine("Line="+src);
                    break;                    
                }
                
                if (from == src.Length)
                {
                    break;
                }
                
                int to = src.IndexOf(delimiter, from);
                
                if (to < 0) // may be end of line ?
                {
                    // fix the value - reference the char after end of line
                    to = src.Length;
                }
                
                if (((to-1) >= from) && (from >= 0))
                {
                    field = src.Substring(from, (to-from));
                }
                else
                {
                    System.Console.WriteLine("Failed to find delimiter " + delimiter + " position "+from + " line "+count);
                    System.Console.WriteLine("Line="+src);
                    break;
                }
                
                from = to;
                if (from >= src.Length) from = src.Length;

                res= true;
            }
            while (false);
                

            return res;
        }

        protected override bool GetData(out DataType data)
        {
            bool res;
            string str;
            data = default(DataType);
            TimeSpan timeSpan = lastTimeSpan;

            if (!ReadyToGo) return false;

            bool parseRes = false;
            
            do
            {
                res = false;
                
                if (streamReader.EndOfStream)
                {
                    res = false;
                    break;
                }
                
                // let's try to read
                try
                {
                    str = streamReader.ReadLine();
                    res = true;
                }
                catch (IOException e)
                {
                    System.Console.WriteLine(e.ToString());
                    res = false;
                    break;
                }                

                // parse the string
                // if i failed to parse read next line until eof or read error
                // i just skip the bad line
                parseRes = ParseLogString(str, out data, out timeSpan);

                res = true;
            }
            while (!parseRes);

            // i calculate the delay and wait
            if (res)
            {
                DoDelay(timeSpan);
            }



            count += 1;

            return res;

        }

        /// <summary>
        /// Look the two consecutive time stamps. Calculate time span. 
        /// If time spane is larger than MIN_DELAY call Thread.Sleep()
        /// else accumulate the difference in the field delay
        /// </summary>
        private void DoDelay(TimeSpan timeSpan)
        {
            // accumulate ellapsed time in the delay variable
            if (lastTimeSpan != default(TimeSpan))
            {
                delay += (timeSpan - lastTimeSpan).Milliseconds;
            }
            lastTimeSpan = timeSpan;


            // calculate next sleep taking into account that the shortest possible
            // sleep is MIN_DELAY
            int ticks = delay/MIN_DELAY;
            if (ticks > 0)
            {
                int sleep = ticks * MIN_DELAY;
                Thread.Sleep(sleep);
                delay -= sleep;
            }
        }

        protected void SetDelay(int delay)
        {
            this.delay = delay;
        }

        public int GetCount()
        {
            return count;
        }

        protected abstract bool ParseLogString(string str, out DataType data, out TimeSpan timeSpan);
        protected abstract bool CheckFile(FileStream fileStream);

        /// <summary>
        /// for example a comma
        /// used in the getNextField() 
        /// </summary>
        private string delimiter;
        
        protected FieldInfo[] fields;

        /// <summary>
        /// Delay in milliseconds
        /// Called in GetData() to slow the things down
        /// Child class will call SetDelay() depending on the time stamps in the log
        /// </summary>
        protected int delay;

        /// <summary>
        /// There is difference between Linux and Windows regarding the minimum possible
        /// delay in the call to Thread.Sleep()
        /// This is not relevant at this point. I use the maximum between two - 15ms
        /// </summary>
        private const int MIN_DELAY = 15; // ms
        
        protected int count;
        protected FileStream fileStream;
        protected StreamReader streamReader;
        protected string filename;
        private TimeSpan lastTimeSpan;

        protected bool ReadyToGo;
    }

    /// <summary>
    /// this is a thread generating event based on the Maof log file
    /// </summary>
    public class MaofDataGeneratorLogFile : EventGeneratorPlayback<K300MaofType>, ISimulationDataGenerator<K300MaofType>, JQuant.IDataGenerator
    {
        /// Log file to read the data from
        /// <param name="filename">
        /// A <see cref="System.String"/>
        /// </param>
        /// <param name="speedup">
        /// Number of times to accelerate the time. For example, if speedup is 2
        /// then events which took 1s in the log file will be sent in 500ms
        /// if speedup is 0.1 the play back will be slower by 10 times 
        /// A <see cref="System.Double"/>
        /// </param>
        /// <param name="delay">
        /// A <see cref="System.Int32"/>
        /// Initial delay before I start to send events in milliseconds
        /// </param>
        public MaofDataGeneratorLogFile(string filename, double speedup, int delay)
            : base(",", filename)
        {
            // set initial delay
            // first event will be sent only after the delay expires
            base.SetDelay(delay);
        }

        ~MaofDataGeneratorLogFile()
        {
        }

        protected override bool CheckFile(FileStream fileStream)
        {
            const string HEADER = "SUG_REC,TRADE_METH,BNO_Num,LAST_REC,SIDURI_Num,SYMBOL_E,Symbol,BNO_NAME_E,BNO_NAME,BRANCH_NO,BRANCH_U,SUG_BNO,MIN_UNIT,HARIG_NV,MIN_PR,MAX_PR,BASIS_PRC,BASIS_COD,STATUS_COD,EX_DATE,EX_PRC,VL_MULT,VL_COD,ZERO_COD,shlav,STATUS,TRD_STP_CD,TRD_STP_N,STP_OPN_TM,LMT_BY1,LMT_BY2,LMT_BY3,LMY_BY1_NV,LMY_BY2_NV,LMY_BY3_NV,RWR_FE,LMT_SL1,LMT_SL2,LMT_SL3,LMY_SL1_NV,LMY_SL2_NV,LMY_SL3_NV,RWR_FF,PRC,COD_PRC,SUG_PRC,LST_DF_BS,RWR_FG,LST_DL_PR,LST_DL_TM,LST_DL_VL,DAY_VL,DAY_VL_NIS,DAY_DIL_NO,RWR_FH,DAY_MAX_PR,DAY_MIN_PR,POS_OPN,POS_OPN_DF,STS_NXT_DY,UPD_DAT,UPD_TIME,FILER,TimeStamp,Ticks";
            
            bool res = false;
            string str;
            do
            {
                
                // let's try to read
                try
                {
                    str = streamReader.ReadLine();
                }
                catch (IOException e)
                {
                    System.Console.WriteLine("Failed to read file "+filename);
                    System.Console.WriteLine(e.ToString());
                    break;
                }

                // first line is legend
                if (str.IndexOf(HEADER) != 0)
                {
                    System.Console.WriteLine("First line match failed in the file "+filename);
                    System.Console.WriteLine("Expected "+HEADER);
                    System.Console.WriteLine("Read "+str);
                    break;
                }

                res = true;
            }
            while (false);


            return res;
        }

        protected override bool ParseLogString(string str, out K300MaofType data, out TimeSpan timeSpan)
        {
            bool res = false;
            int commaIndex = 0;
            timeSpan = TimeSpan.FromTicks(0);

            // create a new object
            data = new K300MaofType();

            // boxing of the structure
            object o = (object)data;
            
            do
            {
                // set all fields in the object
                foreach (FieldInfo fi in fields)
                {
                    string fieldValue;

                    // getNextField() is in the parent class
                    // the method fetches next field in the line
                    // this is where delimiter (in our case a comma) is important
                    res = getNextField(str, ref commaIndex, out fieldValue);

                    if (!res) break;
                    
                    // commaIndex_1 points to comma
                    commaIndex++;
                    
                    fi.SetValue(o, fieldValue);
                }

                if (!res) break;

                // unboxing of the structure
                data = (K300MaofType)o;

                // the tricky part
                // last two fields in the record - TimeStamp and Ticks were not parsed
                // parse them now and convert to variable of type DateTime 
                string timeStampStr;
                res = getNextField(str, ref commaIndex, out timeStampStr);commaIndex++;
                if (!res) {System.Console.WriteLine("Failed to fectch time stamp from "+str);System.Console.WriteLine("Expected at "+commaIndex);}
                // string ticks = getNextField(str, ref commaIndex);commaIndex++;
                timeSpan = TimeSpan.Parse(timeStampStr);

                res = true;
            }
            while (false);

            return res;
        }

        protected override void SendEvents(ref K300MaofType data)
        {
            SimulationTop.k300EventsClass.SendEventMaof(ref data);
            
            // avoid tight loops in the system
            Thread.Sleep(0);
        }

        public string GetName()
        {
            return "Maof playback generator";
        }
    }

    
    /// <summary>
    /// this is a thread generating 
    /// very simple all fields are random Maof data generator
    /// objects of this type used as an argument to the InitStreamSimulation
    /// </summary>
    /// <param name="maofGenerator">
    /// A <see cref="ISimulationStreamGenerator"/>
    /// </param>
    public class MaofDataGeneratorRandom : EventGenerator<K300MaofType>, ISimulationDataGenerator<K300MaofType>, JQuant.IDataGenerator
    {
        public MaofDataGeneratorRandom()
        {
            randomString = new JQuant.RandomNumericalString(21, 80155);

            Type t = typeof(K300MaofType);
            fields = t.GetFields();
            count = 0;

            return;
        }

        protected override bool GetData(out K300MaofType data)
        {
            // delay - usually delay will be in the GetData
            // GetData reads log, pulls the time stamps and simulates
            // timing of the real data stream
            // Thread.Sleep(50);

            // create a new object
            data = new K300MaofType();

            // box the structure
            object o = (object)data;
            // set all fields in the object
            foreach (FieldInfo fi in fields)
            {
                string fieldValue = randomString.Next();
                fi.SetValue(o, fieldValue);
            }
            // unboxing of the structure
            data = (K300MaofType)o;
            // i want to mark first and last field
            data.SUG_REC = "SUG_REC";
            data.FILER = "FILER";
            data.BNO_NAME = "Maof";
            

            count += 1;


            return true;
        }

        protected override void SendEvents(ref K300MaofType data)
        {
            SimulationTop.k300EventsClass.SendEventMaof(ref data);
            // avoid tight loops in the system
            // Thread.Sleep(50);
        }

        public int GetCount()
        {
            return count;
        }

        public string GetName()
        {
            return "Maof data random generator";
        }


        JQuant.IRandomString randomString;
        protected FieldInfo[] fields;
        int count;
    }



    /// <summary>
    /// this is a thread generating 
    /// very siimple all fields are random Rezef data generator
    /// objects of this type used as an argument to the InitStreamSimulation
    /// </summary>
    /// <param name="maofGenerator">
    /// A <see cref="ISimulationStreamGenerator"/>
    /// </param>
    public class RezefDataGeneratorRandom : EventGenerator<K300RzfType>, ISimulationDataGenerator<K300RzfType>, JQuant.IDataGenerator
    {
        public RezefDataGeneratorRandom()
        {
            randomString = new JQuant.RandomNumericalString(21, 80155);

            Type t = typeof(K300RzfType);
            fields = t.GetFields();
            count = 0;

            return;
        }

        protected override bool GetData(out K300RzfType data)
        {
            // delay - usually delay will be in the GetData
            // GetData reads log, pulls the time stamps and simulates
            // timing of the real data stream
            Thread.Sleep(50);

            // create a new object
            data = new K300RzfType();

            // box the structure
            object o = (object)data;
            // set all fields in the object
            foreach (FieldInfo fi in fields)
            {
                string fieldValue = randomString.Next();
                fi.SetValue(o, fieldValue);
            }
            // unboxing of the structure
            data = (K300RzfType)o;
            data.BNO_NAME = "Rezef";


            count += 1;


            return true;
        }

        protected override void SendEvents(ref K300RzfType data)
        {
            SimulationTop.k300EventsClass.SendEventRzf(ref data);
            // avoid tight loops in the system
            Thread.Sleep(50);
        }

        public int GetCount()
        {
            return count;
        }

        public string GetName()
        {
            return "Rezef data random generator";
        }


        JQuant.IRandomString randomString;
        protected FieldInfo[] fields;
        int count;
    }


    /// <summary>
    /// this is a thread generating 
    /// very siimple all fields are random Madad data generator
    /// objects of this type used as an argument to the InitStreamSimulation
    /// </summary>
    /// <param name="maofGenerator">
    /// A <see cref="ISimulationStreamGenerator"/>
    /// </param>
    public class MadadDataGeneratorRandom : EventGenerator<K300MadadType>, ISimulationDataGenerator<K300MadadType>, JQuant.IDataGenerator
    {
        public MadadDataGeneratorRandom()
        {
            randomString = new JQuant.RandomNumericalString(21, 80155);

            Type t = typeof(K300MadadType);
            fields = t.GetFields();
            count = 0;

            return;
        }

        protected override bool GetData(out K300MadadType data)
        {
            // delay - usually delay will be in the GetData
            // GetData reads log, pulls the time stamps and simulates
            // timing of the real data stream
            Thread.Sleep(50);

            // create a new object
            data = new K300MadadType();

            // box the structure
            object o = (object)data;
            // set all fields in the object
            foreach (FieldInfo fi in fields)
            {
                string fieldValue = randomString.Next();
                fi.SetValue(o, fieldValue);
            }
            // unboxing of the structure
            data = (K300MadadType)o;
            data.BNO_N = "Madad";

            count += 1;
            
            return true;
        }

        protected override void SendEvents(ref K300MadadType data)
        {
            SimulationTop.k300EventsClass.SendEventMdd(ref data);
            // avoid tight loops in the system
            Thread.Sleep(50);
        }

        public int GetCount()
        {
            return count;
        }

        public string GetName()
        {
            return "Madad data random generator";
        }


        JQuant.IRandomString randomString;
        protected FieldInfo[] fields;
        int count;
    }


    /// <summary>
    /// Describes an asset, for example, TASE option
    /// this class is used in the MarketSimulation
    /// all prices are integers. If required in cents/agorots
    /// </summary>
    public class MarketData
    {
        public int BNO_Num;

        // three best asks and bids - price and size
        public int LMT_BY1;
        public int LMT_BY2;
        public int LMT_BY3;
        public int LMY_BY1_NV;
        public int LMY_BY2_NV;
        public int LMY_BY3_NV;
        public int LMT_SL1;
        public int LMT_SL2;
        public int LMT_SL3;
        public int LMY_SL1_NV;
        public int LMY_SL2_NV;
        public int LMY_SL3_NV;

        // last deal price and size
        public int LST_DL_PR;
        public int LST_DL_VL;
    }
    
    /// <summary>
    /// I work only with data containig BNO_Num field
    /// </summary>
    public class MarketSimulation<DataType> : JQuant.IConsumer<DataType>
    {
        protected class FSMState
        {
            DataType entry;
            
        }
        
        protected MarketSimulation(Type dataType, JQuant.IProducer<DataType> producer)
        {
            CheckDataType(dataType);
            producer.AddConsumer(this);

            field_BNO_Num = dataType.GetField("BNO_Num");
            field_LMT_BY1 = dataType.GetField("LMT_BY1");
            field_LMT_BY2 = dataType.GetField("LMT_BY2");
            field_LMT_BY3 = dataType.GetField("LMT_BY3");
            field_LMY_BY1_NV = dataType.GetField("LMY_BY1_NV");
            field_LMY_BY2_NV = dataType.GetField("LMY_BY3_NV");
            field_LMY_BY3_NV = dataType.GetField("LMY_BY3_NV");
            field_LMT_SL1 = dataType.GetField("LMT_SL1");
            field_LMT_SL2 = dataType.GetField("LMT_SL2");
            field_LMT_SL3 = dataType.GetField("LMT_SL3");
            field_LMY_SL1_NV = dataType.GetField("LMY_SL1_NV");
            field_LMY_SL2_NV = dataType.GetField("LMY_SL2_NV");
            field_LMY_SL3_NV = dataType.GetField("LMY_SL3_NV");
            field_LST_DL_PR = dataType.GetField("LST_DL_PR");
            field_LST_DL_VL = dataType.GetField("LST_DL_VL");


            entries = new System.Collections.Hashtable(200);
        }

        private bool CheckDataType(Type dataType)
        {
            FieldInfo[] fields = dataType.GetFields();
                
            System.Collections.Hashtable hashtable = new System.Collections.Hashtable(30);
            foreach (FieldInfo fi in fields)
            {
                string fieldName = fi.Name;
                hashtable.Add(fieldName, fieldName);
            }

            string[] MANDATORY_FIELDS = {"BNO_Num","LMT_BY1","LMT_BY2","LMT_BY3","LMY_BY1_NV",
                                        "LMY_BY2_NV","LMY_BY3_NV","LMT_SL1","LMT_SL2","LMT_SL3",
                                        "LMY_SL1_NV","LMY_SL2_NV","MY_SL3_NV","LST_DL_PR","LST_DL_VL"};

            bool res = true;
            foreach (string s in MANDATORY_FIELDS)
            {
                if (!hashtable.Contains(s))
                {
                    System.Console.WriteLine("No mandatory field "+s+" in the type "+dataType.Name);
                    res = false;
                    break;
                }
            }
            
            return res;
        }

        /// <summary>
        /// The method is being called by Event Generator
        /// </summary>
        public void Notify(int count, DataType data)
        {
            // do boxing - i want to be sure that this happens only once
            object o = (object)data;

            // create something better than a structure with strings
            // i need integers to work with
            MarketData marketData = RawDataToMarketData(o);
            
            // GetKey() will return (in the simplest case) BNO_number (boxed integer)
            object key = GetKey(o);

            // hopefully Item() will return null if there is no key in the hashtable
            object entry = entries[key];
            if (entry != null) // entry is in the table. this is most likely outcome 
            {
                UpdateEntry((MarketData)entry, marketData);
            }
            else // I see this entry (this BNO_number) very first time - add new entry to the hashtable
            {
                entries[key] = marketData;
            }
        }


        /// <summary>
        /// DataType is something like K300MaofType - lot of strings. The method will  convert
        /// this into something convenient to work with.
        /// </summary>
        /// <param name="dt">
        /// A <see cref="System.Object"/>
        /// Object of type DataType
        /// </param>
        /// <returns>
        /// A <see cref="MarketData"/>
        /// New object containing integers like Price, best bid/ask, etc.
        /// </returns>
        protected MarketData RawDataToMarketData(object dt)
        {
            MarketData md = new MarketData();
            
            md.BNO_Num       = Int32.Parse((string)field_BNO_Num   .GetValue(dt));
            md.LMT_BY1       = Int32.Parse((string)field_LMT_BY1   .GetValue(dt));
            md.LMT_BY2       = Int32.Parse((string)field_LMT_BY2   .GetValue(dt));
            md.LMT_BY3       = Int32.Parse((string)field_LMT_BY3   .GetValue(dt));
            md.LMY_BY1_NV    = Int32.Parse((string)field_LMY_BY1_NV.GetValue(dt));
            md.LMY_BY2_NV    = Int32.Parse((string)field_LMY_BY2_NV.GetValue(dt));
            md.LMY_BY3_NV    = Int32.Parse((string)field_LMY_BY3_NV.GetValue(dt));
            md.LMT_SL1       = Int32.Parse((string)field_LMT_SL1   .GetValue(dt));
            md.LMT_SL2       = Int32.Parse((string)field_LMT_SL2   .GetValue(dt));
            md.LMT_SL3       = Int32.Parse((string)field_LMT_SL3   .GetValue(dt));
            md.LMY_SL1_NV    = Int32.Parse((string)field_LMY_SL1_NV.GetValue(dt));
            md.LMY_SL2_NV    = Int32.Parse((string)field_LMY_SL2_NV.GetValue(dt));
            md.LMY_SL3_NV    = Int32.Parse((string)field_LMY_SL3_NV.GetValue(dt));
            md.LST_DL_PR     = Int32.Parse((string)field_LST_DL_PR .GetValue(dt));
            md.LST_DL_VL     = Int32.Parse((string)field_LST_DL_VL .GetValue(dt));

            return md;
        }

        /// <summary>
        /// Returns key for the hashtable
        /// The implemenation is trivial - return BNO_Num
        /// </summary>
        protected virtual object GetKey(object o)
        {
            // i know that data contains field "BNO_num" - no exceptions here
            object BNO_Num = Int32.Parse((string)field_BNO_Num.GetValue(o));
            return BNO_Num;
        }


        /// <summary>
        /// If there is any pending (waiting execution) orders check if I can execute any,
        /// shift the orders position in the queue, etc.
        /// In all cases replace the current value with new one.
        /// </summary>
        /// <param name="md0">
        /// A <see cref="MarketData"/>
        /// Currently stored data
        /// </param>
        /// <param name="md1">
        /// A <see cref="MarketData"/>
        /// New data
        /// </param>
        protected void UpdateEntry(MarketData md0, MarketData md1)
        {
        }


        /// <summary>
        /// Collection of all traded symbols (different BNO_num for TASE) 
        /// </summary>
        protected System.Collections.Hashtable entries;
        protected FieldInfo field_BNO_Num;
        protected FieldInfo field_LMT_BY1;
        protected FieldInfo field_LMT_BY2;
        protected FieldInfo field_LMT_BY3;
        protected FieldInfo field_LMY_BY1_NV;
        protected FieldInfo field_LMY_BY2_NV;
        protected FieldInfo field_LMY_BY3_NV;
        protected FieldInfo field_LMT_SL1;
        protected FieldInfo field_LMT_SL2;
        protected FieldInfo field_LMT_SL3;
        protected FieldInfo field_LMY_SL1_NV;
        protected FieldInfo field_LMY_SL2_NV;
        protected FieldInfo field_LMY_SL3_NV;
        protected FieldInfo field_LST_DL_PR;
        protected FieldInfo field_LST_DL_VL;
    }

    /// <summary>
    /// This is a simulation of the options Maof market. The class collects incoming events - events taken
    /// from the historical data, keeps track of all strikes.
    /// MarketSimulation compares pending orders with the market state and figures out if fill was possible
    /// To make the whole exercise practical for the current phase I assume that incoming orders do not influence
    /// the market. I assume that if buy order's bid is equal to the best Ask the probability of the fill is 1
    /// </summary>
    public class MarketSimulationMaof : MarketSimulation<K300MaofType>
    {
        public MarketSimulationMaof(JQuant.IProducer<K300MaofType> producer)
            : base(typeof(K300MaofType), producer)
        {
        }
    }


    /// <summary>
    /// I need some class where all apparently disconnected classes are connected
    /// For example K300Class and K300EventsClass
    /// User application registers event listeners in the EventClass and calls StartStream
    /// in the K300Class
    /// </summary>
    class SimulationTop
    {
        public static K300EventsClass k300EventsClass;
        public static K300Class k300Class;
    }

}
