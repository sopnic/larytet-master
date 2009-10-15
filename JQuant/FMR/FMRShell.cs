
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;

using System.Reflection;
using System.ComponentModel;
using JQuant;

using System.Windows.Forms;

/// <summary>
/// depending on the compilation flags i am going to use
/// simulation or real TaskBar namespace
/// the rest of the application is using FMRShell and is not aware if 
/// this is a simulation or a real server connected
/// Actually this is not precise - Command Line interface contains some internal test commands
/// which require simulation engines
/// </summary>
#if USEFMRSIM
using TaskBarLibSim;
#else
using TaskBarLib;
#endif

/// <summary>
/// thin shell around either TaskBarLib 
/// or around FMRSym if compiler directive USEFMRSIM is defined 
/// </summary>
namespace FMRShell
{

    #region Connection

    public enum ConnectionState
    {
        [Description("Idle")]
        Idle,                              // never opened yet
        [Description("Established")]
        Established,
        [Description("Trying")]
        Trying,
        [Description("Closed")]
        Closed,
        [Description("Disposed")]
        Disposed                           // Dispose() called
    }


    /// <summary>
    /// this guy logins to the remote server
    /// and eventually will run state machine keeping the connection alive and kicking 
    /// and will notify other subsystems if and when the connection goes down
    /// this little guy is one of the most important. there are two sources of information
    /// related to the connection status
    /// - periodic attempts to read data stream through TaskBarLib.K300Class
    /// - TaskBarLib.UserClass 
    /// This class handles all included in the TasBarkLib.UserClass and login related
    /// When there is no real TaskBarLib the class calls TasBarkLibSim
    /// 
    /// Normally application will do something like
    /// FMRShell.Connection connection = new FMRShell.Connection("xmlfilename")
    /// bool openResult = connection.Open(errCode)
    /// do work with connection.userClass  of type TaskBarLib.UserClass
    /// connection.Dispose();
    /// </summary>
    public class Connection : IDisposable
    {
        /// <summary>
        /// use default hard coded user name and password
        /// </summary>
        public Connection()
        {
            //  set defaults
            _parameters = new ConnectionParameters(
                "aryeh",    // user name
                "abc123",   // password
                "12345"     // account
                );
            Init();
        }

        /// <summary>
        /// create connection using provided by application connection parameters
        /// </summary>
        /// <param name="parameters">
        /// A <see cref="ConnectionParameters"/>
        /// </param>
        public Connection(ConnectionParameters parameters)
        {
            _parameters = parameters;
            Init();
        }

        /// <summary>
        /// open connection based on the login information stored in the specified XML file
        /// See example of the XML file in the ConnectionParameters.xml
        /// </summary>
        /// <param name="filename">
        /// A <see cref="System.String"/>
        /// Name of the XML file where the user login credentials can be found
        /// </param>
        public Connection(string filename)
        {
            xmlFileName = filename;
            useXmlFile = true;
            Init();
        }

        /// <summary>
        /// do some general initialization common for all constructors
        /// </summary>
        private void Init()
        {
            state = ConnectionState.Idle;
            userClass = new UserClass();
        }

        /// <summary>
        /// application have to call this method to get rid of the 
        /// connection (close sockets, etc.)
        /// </summary>
        public void Dispose()
        {
            // call userClass.Logout
            userClass.Logout(sessionId);

            // set userClass to null
            state = ConnectionState.Disposed;
        }

        ~Connection()
        {
        }

        public int GetSessionId()
        {
            return sessionId;
        }

        public string GetErrorMsg()
        {
            return errMsg;
        }

        /// <summary>
        /// return false if the open connection fails 
        /// normally application will call Open() without arguments - blocking Open
        /// or Keep() - which runs a thread and attempts to keep the connection
        /// alive. 
        /// </summary>
        /// <param name="returnCode">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// True if Ok and returnCode is set to something meaningful
        /// Application will check that the method retruned true and only 
        /// after that analyze returnCode
        /// </returns>
        public bool Open(out int returnCode)
        {
            bool result = true;
            returnCode = 0;

            // should I read login credentials from XML file ?
            if (useXmlFile)
            {
                ReadConnectionParameters reader = new ReadConnectionParameters(xmlFileName);

                // let's try to read the file 
                result = reader.Parse(out _parameters);
            }

            if (result)
            {
                returnCode = userClass.Login(
                    _parameters.userName,
                    _parameters.userPassword,
                    _parameters.appPassword,
                    out  errMsg, out  sessionId);

                result = (returnCode >= 0);
                if (result)
                {
                    Console.WriteLine("SessionId is " + sessionId);
                    // returnCode == sessionId - check if not
                    if (sessionId != returnCode)
                    {
                        Console.WriteLine("Session Id=" + sessionId + ",returnCode=" + returnCode + " are not equal after Login");
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// the calling thread will be blocked by this method until Login to the
        /// remote server succeeds 
        /// </summary>
        /// <param name="printProgress">
        /// A <see cref="System.Boolean"/>
        /// if set to true will print dots as login makes progress
        /// </param>
        /// <returns>
        /// A <see cref="System.Boolean"/>
        /// True if Ok and returnCode is set to something meaningful
        /// Application will check that the method retruned true and only 
        /// after that analyze returnCode
        /// 
        /// TODO: Using of IWrite is an ugly patch. All prints should be done in
        /// some dedicated delagate
        /// </returns>
        public bool Open(IWrite iWrite, out int returnCode, bool printProgress)
        {
            state = ConnectionState.Trying;
            int percent = 0;

            bool openResult = Open(out returnCode);

            if (printProgress)
            {
                //Print title + progress bar
                iWrite.WriteLine(Environment.NewLine);
                iWrite.WriteLine("0    10   20   30   40   50   60   70   80   90  100");
                iWrite.WriteLine("|----+----+----+----+----+----+----+----+----+----|");
            }

            
            string desc_ = "";

            // loop until login succeeds
            while (openResult)
            {
                int percent_1;
                string description;

                // I need a short delay here before next attempt - 5 seconds
                // but do it only when already in the process - this saves me time when 
                // I have connection already established - don't wait on the first attempt.
                if (percent>0) Thread.Sleep(5 * 1000);

                userClass.GetLoginActivity(ref sessionId, out percent_1, out description);

                if (percent_1 != percent) 
                {
                    if (printProgress)
                    {
                        //scale the dots to the progress bar
                        while (percent < percent_1)
                        {
                            iWrite.Write(".");
                            percent += 2;
                        }
                    }
                }

                if (desc_ != description)
                {
                    MessageBox.Show(description);
                    desc_ = description;
                }


                loginStatus = userClass.get_LoginState(ref sessionId);

                if (loginStatus == LoginStatus.LoginSessionActive)
                {
                    openResult = true;
                    break;
                }
                else if ((loginStatus == LoginStatus.LoginSessionInProgress))
                {
                    //do nothing - just continue the loop
                }
                else    //any other loginStatus indicates login error
                {
                    openResult = false;
                    break;
                }
            }

            return openResult;
        }


        public string GetUserName()
        {
            return _parameters.userName;
        }

        public LoginStatus loginStatus
        {
            get;
            protected set;
        }


        public ConnectionState state
        {
            get;
            protected set;
        }

        public string LoginErrorDesc()
        {
            return this.userClass.get_LoginErrorDesc(ref this.sessionId);
        }

        /// <value>
        /// in the real code TaskBarLib instead of TaskBarLibSim will be used
        /// </value>
        public UserClass userClass
        {
            get;
            protected set;
        }

        public ConnectionParameters Parameters
        {
            get
            {
                return _parameters;
            }
        }

        protected int sessionId;
        protected string errMsg;
        protected string xmlFileName;
        protected bool useXmlFile;
        protected ConnectionParameters _parameters;

    }//class Connection

    #endregion;

    #region Collect Market Data

    public enum DataType
    {
        Maof,
        Rezef,
        Madad,

        // keep this entry last
        Last        
    }

    public struct MarketDataMadad: ICloneable
    {
        public K300MadadType k300MddType;

        public object Clone()
        {
            // create a new object
            MarketDataMadad md = new MarketDataMadad();

            // copy data
            md.k300MddType.SUG_RC = this.k300MddType.SUG_RC;
            md.k300MddType.BNO_N = this.k300MddType.BNO_N;
            md.k300MddType.FIL1_VK = this.k300MddType.FIL1_VK;
            md.k300MddType.MDD_COD = this.k300MddType.MDD_COD;
            md.k300MddType.MDD_SUG = this.k300MddType.MDD_SUG;
            md.k300MddType.MDD_N = this.k300MddType.MDD_N;
            md.k300MddType.FIL2_VK = this.k300MddType.FIL2_VK;
            md.k300MddType.MDD_NAME = this.k300MddType.MDD_NAME;
            md.k300MddType.Madad = this.k300MddType.Madad;
            md.k300MddType.FIL3_VK = this.k300MddType.FIL3_VK;
            md.k300MddType.MDD_DF = this.k300MddType.MDD_DF;
            md.k300MddType.CALC_TIME = this.k300MddType.CALC_TIME;
            md.k300MddType.FIL6_VK = this.k300MddType.FIL6_VK;
            md.k300MddType.UPD_DAT = this.k300MddType.UPD_DAT;
            md.k300MddType.UPD_TIME = this.k300MddType.UPD_TIME;

            return md;
        }


        public override string ToString()
        {
            return k300MddType.SUG_RC;
        }

        /// <summary>
        /// Prepares a single record to be written 
        /// to a CSV (comma-separated values) output file.
        /// </summary>
        /// <returns></returns>
        public string ToCSVString()
        {
            return "";  //it's unclear what's the data mean - no documentation either
            //will try to see what we understand from the logged data - Oct 08, 2009


                /*k300MddType.BNO_Num + "," +
                k300MddType.LMT_BY1 + "," +
                k300MddType.LMT_BY2 + "," +
                k300MddType.LMT_BY3 + "," +
                k300MddType.LMT_SL1 + "," +
                k300MddType.LMT_SL2 + "," +
                k300MddType.LMT_SL3 + "," +
                k300MddType.LMY_BY1_NV + "," +
                k300MddType.LMY_BY2_NV + "," +
                k300MddType.LMY_BY3_NV + "," +
                k300MddType.LMY_SL1_NV + "," +
                k300MddType.LMY_SL2_NV + "," +
                k300MddType.LMY_SL3_NV + "," +
                k300MddType.LST_DL_PR + "," +
                k300MddType.LST_DL_TM + "," +
                k300MddType.LST_DL_VL + "," +
                k300MddType.UPD_TIME + "\n";*/
        }
    }//struct MarketDataMadad


    /// <summary>
    /// generic class 
    /// data container for the trading/market data - Maof options
    /// can hold fields like time stamp, bid/ask, last price, etc.
    /// this class is not going to be used directly but inherited
    /// </summary>
    public struct MarketDataRezef : ICloneable
    {
        public K300RzfType k300RzfType;

        public object Clone()
        {
            // create a new object
            MarketDataRezef md = new MarketDataRezef();

            // copy data
            md.k300RzfType.SUG_REC = this.k300RzfType.SUG_REC;
            md.k300RzfType.BNO_Num = this.k300RzfType.BNO_Num;
            md.k300RzfType.BNO_NAME = this.k300RzfType.BNO_NAME;
            md.k300RzfType.Symbol = this.k300RzfType.Symbol;
            md.k300RzfType.TRADE_METH = this.k300RzfType.TRADE_METH;
            md.k300RzfType.SIDURI_Num = this.k300RzfType.SIDURI_Num;
            md.k300RzfType.RWR_VA = this.k300RzfType.RWR_VA;
            md.k300RzfType.MIN_UNIT = this.k300RzfType.MIN_UNIT;
            md.k300RzfType.HARIG_NV = this.k300RzfType.HARIG_NV;
            md.k300RzfType.MIN_PR_OPN = this.k300RzfType.MIN_PR_OPN;
            md.k300RzfType.MAX_PR_OPN = this.k300RzfType.MAX_PR_OPN;
            md.k300RzfType.MIN_PR_CNT = this.k300RzfType.MIN_PR_CNT;
            md.k300RzfType.MAX_PR_CNT = this.k300RzfType.MAX_PR_CNT;
            md.k300RzfType.BASIS_PRC = this.k300RzfType.BASIS_PRC;
            md.k300RzfType.STATUS = this.k300RzfType.STATUS;
            md.k300RzfType.EX_COD = this.k300RzfType.EX_COD;
            md.k300RzfType.EX_DETAIL = this.k300RzfType.EX_DETAIL;
            md.k300RzfType.RWR_VB = this.k300RzfType.RWR_VB;
            md.k300RzfType.shlav = this.k300RzfType.shlav;
            md.k300RzfType.LAST_PRC = this.k300RzfType.LAST_PRC;
            md.k300RzfType.TRD_STP_N = this.k300RzfType.TRD_STP_N;
            md.k300RzfType.STP_OPN_TM = this.k300RzfType.STP_OPN_TM;
            md.k300RzfType.RWR_VD = this.k300RzfType.RWR_VD;
            md.k300RzfType.LMT_BY1 = this.k300RzfType.LMT_BY1;
            md.k300RzfType.LMT_BY2 = this.k300RzfType.LMT_BY2;
            md.k300RzfType.LMT_BY3 = this.k300RzfType.LMT_BY3;
            md.k300RzfType.LMY_BY1_NV = this.k300RzfType.LMY_BY1_NV;
            md.k300RzfType.LMY_BY2_NV = this.k300RzfType.LMY_BY2_NV;
            md.k300RzfType.LMY_BY3_NV = this.k300RzfType.LMY_BY3_NV;
            md.k300RzfType.MKT_NV_BY = this.k300RzfType.MKT_NV_BY;
            md.k300RzfType.MKT_NV_BY_NUM = this.k300RzfType.MKT_NV_BY_NUM;
            md.k300RzfType.RWR_VE = this.k300RzfType.RWR_VE;
            md.k300RzfType.LMT_SL1 = this.k300RzfType.LMT_SL1;
            md.k300RzfType.LMT_SL2 = this.k300RzfType.LMT_SL2;
            md.k300RzfType.LMT_SL3 = this.k300RzfType.LMT_SL3;
            md.k300RzfType.LMY_SL1_NV = this.k300RzfType.LMY_SL1_NV;
            md.k300RzfType.LMY_SL2_NV = this.k300RzfType.LMY_SL2_NV;
            md.k300RzfType.LMY_SL3_NV = this.k300RzfType.LMY_SL3_NV;
            md.k300RzfType.MKT_NV_SL = this.k300RzfType.MKT_NV_SL;
            md.k300RzfType.MKT_NV_SL_NUM = this.k300RzfType.MKT_NV_SL_NUM;
            md.k300RzfType.RWR_VF = this.k300RzfType.RWR_VF;
            md.k300RzfType.THEOR_PR = this.k300RzfType.THEOR_PR;
            md.k300RzfType.THEOR_VL = this.k300RzfType.THEOR_VL;
            md.k300RzfType.RWR_VG = this.k300RzfType.RWR_VG;
            md.k300RzfType.LST_DL_PR = this.k300RzfType.LST_DL_PR;
            md.k300RzfType.LST_DL_TM = this.k300RzfType.LST_DL_TM;
            md.k300RzfType.LST_DF_BS = this.k300RzfType.LST_DF_BS;
            md.k300RzfType.LST_DF_OPN = this.k300RzfType.LST_DF_OPN;
            md.k300RzfType.LST_DL_VL = this.k300RzfType.LST_DL_VL;
            md.k300RzfType.DAY_VL = this.k300RzfType.DAY_VL;
            md.k300RzfType.DAY_VL_NIS = this.k300RzfType.DAY_VL_NIS;
            md.k300RzfType.DAY_DIL_NO = this.k300RzfType.DAY_DIL_NO;
            md.k300RzfType.DAY_MAX_PR = this.k300RzfType.DAY_MAX_PR;
            md.k300RzfType.DAY_MIN_PR = this.k300RzfType.DAY_MIN_PR;
            md.k300RzfType.BNO_NAME_E = this.k300RzfType.BNO_NAME_E;
            md.k300RzfType.SYMBOL_E = this.k300RzfType.SYMBOL_E;
            md.k300RzfType.STP_COD = this.k300RzfType.STP_COD;
            md.k300RzfType.COD_SHAAR = this.k300RzfType.COD_SHAAR;
            md.k300RzfType.UPD_DAT = this.k300RzfType.UPD_DAT;
            md.k300RzfType.UPD_TIME = this.k300RzfType.UPD_TIME;

            return md;
        }


        public override string ToString()
        {
            return k300RzfType.SUG_REC;
        }

        /// <summary>
        /// Prepares a single record to be written 
        /// to a CSV (comma-separated values) output file.
        /// </summary>
        /// <returns></returns>
        public string ToCSVString()
        {
            return
                k300RzfType.BNO_Num + "," +
                k300RzfType.LMT_BY1 + "," +
                k300RzfType.LMT_BY2 + "," +
                k300RzfType.LMT_BY3 + "," +
                k300RzfType.LMT_SL1 + "," +
                k300RzfType.LMT_SL2 + "," +
                k300RzfType.LMT_SL3 + "," +
                k300RzfType.LMY_BY1_NV + "," +
                k300RzfType.LMY_BY2_NV + "," +
                k300RzfType.LMY_BY3_NV + "," +
                k300RzfType.LMY_SL1_NV + "," +
                k300RzfType.LMY_SL2_NV + "," +
                k300RzfType.LMY_SL3_NV + "," +
                k300RzfType.LST_DL_PR + "," +
                k300RzfType.LST_DL_TM + "," +
                k300RzfType.LST_DL_VL + "," +
                k300RzfType.UPD_TIME + "\n";
        }
    }//struct MarketDataRezef


    /// <summary>
    /// generic class 
    /// data container for the trading/market data - Maof options
    /// can hold fields like time stamp, bid/ask, last price, etc.
    /// this class is not going to be used directly but inherited
    /// </summary>
    public struct MarketDataMaof : ICloneable
    {
        public K300MaofType k300MaofType;

        public object Clone()
        {
            // create a new object
            MarketDataMaof md = new MarketDataMaof();

            // copy data
            md.k300MaofType.SUG_REC = this.k300MaofType.SUG_REC;

            md.k300MaofType.TRADE_METH = this.k300MaofType.TRADE_METH;
            md.k300MaofType.BNO_Num = this.k300MaofType.BNO_Num;
            md.k300MaofType.LAST_REC = this.k300MaofType.LAST_REC;
            md.k300MaofType.SIDURI_Num = this.k300MaofType.SIDURI_Num;
            md.k300MaofType.SYMBOL_E = this.k300MaofType.SYMBOL_E;
            md.k300MaofType.Symbol = this.k300MaofType.Symbol;
            md.k300MaofType.BNO_NAME_E = this.k300MaofType.BNO_NAME_E;
            md.k300MaofType.BNO_NAME = this.k300MaofType.BNO_NAME;
            md.k300MaofType.BRANCH_NO = this.k300MaofType.BRANCH_NO;
            md.k300MaofType.BRANCH_U = this.k300MaofType.BRANCH_U;
            md.k300MaofType.SUG_BNO = this.k300MaofType.SUG_BNO;
            md.k300MaofType.MIN_UNIT = this.k300MaofType.MIN_UNIT;
            md.k300MaofType.HARIG_NV = this.k300MaofType.HARIG_NV;
            md.k300MaofType.MIN_PR = this.k300MaofType.MIN_PR;
            md.k300MaofType.MAX_PR = this.k300MaofType.MAX_PR;
            md.k300MaofType.BASIS_PRC = this.k300MaofType.BASIS_PRC;
            md.k300MaofType.BASIS_COD = this.k300MaofType.BASIS_COD;
            md.k300MaofType.STATUS_COD = this.k300MaofType.STATUS_COD;
            md.k300MaofType.EX_DATE = this.k300MaofType.EX_DATE;
            md.k300MaofType.EX_PRC = this.k300MaofType.EX_PRC;
            md.k300MaofType.VL_MULT = this.k300MaofType.VL_MULT;
            md.k300MaofType.VL_COD = this.k300MaofType.VL_COD;
            md.k300MaofType.ZERO_COD = this.k300MaofType.ZERO_COD;
            md.k300MaofType.shlav = this.k300MaofType.shlav;
            md.k300MaofType.STATUS = this.k300MaofType.STATUS;
            md.k300MaofType.TRD_STP_CD = this.k300MaofType.TRD_STP_CD;
            md.k300MaofType.TRD_STP_N = this.k300MaofType.TRD_STP_N;
            md.k300MaofType.STP_OPN_TM = this.k300MaofType.STP_OPN_TM;
            md.k300MaofType.LMT_BY1 = this.k300MaofType.LMT_BY1;
            md.k300MaofType.LMT_BY2 = this.k300MaofType.LMT_BY2;
            md.k300MaofType.LMT_BY3 = this.k300MaofType.LMT_BY3;
            md.k300MaofType.LMY_BY1_NV = this.k300MaofType.LMY_BY1_NV;
            md.k300MaofType.LMY_BY2_NV = this.k300MaofType.LMY_BY2_NV;
            md.k300MaofType.LMY_BY3_NV = this.k300MaofType.LMY_BY3_NV;
            md.k300MaofType.RWR_FE = this.k300MaofType.RWR_FE;
            md.k300MaofType.LMT_SL1 = this.k300MaofType.LMT_SL1;
            md.k300MaofType.LMT_SL2 = this.k300MaofType.LMT_SL2;
            md.k300MaofType.LMT_SL3 = this.k300MaofType.LMT_SL3;
            md.k300MaofType.LMY_SL1_NV = this.k300MaofType.LMY_SL1_NV;
            md.k300MaofType.LMY_SL2_NV = this.k300MaofType.LMY_SL2_NV;
            md.k300MaofType.LMY_SL3_NV = this.k300MaofType.LMY_SL3_NV;
            md.k300MaofType.RWR_FF = this.k300MaofType.RWR_FF;
            md.k300MaofType.PRC = this.k300MaofType.PRC;
            md.k300MaofType.COD_PRC = this.k300MaofType.COD_PRC;
            md.k300MaofType.SUG_PRC = this.k300MaofType.SUG_PRC;
            md.k300MaofType.LST_DF_BS = this.k300MaofType.LST_DF_BS;
            md.k300MaofType.RWR_FG = this.k300MaofType.RWR_FG;
            md.k300MaofType.LST_DL_PR = this.k300MaofType.LST_DL_PR;
            md.k300MaofType.LST_DL_TM = this.k300MaofType.LST_DL_TM;
            md.k300MaofType.LST_DL_VL = this.k300MaofType.LST_DL_VL;
            md.k300MaofType.DAY_VL = this.k300MaofType.DAY_VL;
            md.k300MaofType.DAY_VL_NIS = this.k300MaofType.DAY_VL_NIS;
            md.k300MaofType.DAY_DIL_NO = this.k300MaofType.DAY_DIL_NO;
            md.k300MaofType.RWR_FH = this.k300MaofType.RWR_FH;
            md.k300MaofType.DAY_MAX_PR = this.k300MaofType.DAY_MAX_PR;
            md.k300MaofType.DAY_MIN_PR = this.k300MaofType.DAY_MIN_PR;
            md.k300MaofType.POS_OPN = this.k300MaofType.POS_OPN;
            md.k300MaofType.POS_OPN_DF = this.k300MaofType.POS_OPN_DF;
            md.k300MaofType.STS_NXT_DY = this.k300MaofType.STS_NXT_DY;
            md.k300MaofType.UPD_DAT = this.k300MaofType.UPD_DAT;
            md.k300MaofType.UPD_TIME = this.k300MaofType.UPD_TIME;
            md.k300MaofType.FILER = this.k300MaofType.FILER;

            return md;
        }

        public override string ToString()
        {
            return k300MaofType.SUG_REC;
        }

        /// <summary>
        /// Prepares a single record to be written 
        /// to a CSV (comma-separated values) output file.
        /// </summary>
        /// <returns></returns>
        public string ToCSVString()
        {
            return
                k300MaofType.BNO_Num + "," +
                k300MaofType.LMT_BY1 + "," +
                k300MaofType.LMT_BY2 + "," +
                k300MaofType.LMT_BY3 + "," +
                k300MaofType.LMT_SL1 + "," +
                k300MaofType.LMT_SL2 + "," +
                k300MaofType.LMT_SL3 + "," +
                k300MaofType.LMY_BY1_NV + "," +
                k300MaofType.LMY_BY2_NV + "," +
                k300MaofType.LMY_BY3_NV + "," +
                k300MaofType.LMY_SL1_NV + "," +
                k300MaofType.LMY_SL2_NV + "," +
                k300MaofType.LMY_SL3_NV + "," +
                k300MaofType.LST_DL_PR + "," +
                k300MaofType.LST_DL_TM + "," +
                k300MaofType.LST_DL_VL + "," +
                k300MaofType.UPD_TIME + "\n";
        }
    }//struct MarketDataMaof


    public class K300MaofTypeToString : StructToString<K300MaofType>
    {
        public K300MaofTypeToString(string delimiter)
            : base(delimiter)
        {
        }
    }

    public class K300RzfTypeToString : StructToString<K300RzfType>
    {
        public K300RzfTypeToString(string delimiter)
            : base(delimiter)
        {
        }
    }

    public class K300MadadTypeToString : StructToString<K300MadadType>
    {
        public K300MadadTypeToString(string delimiter)
            : base(delimiter)
        {
        }
    }

    /// <summary>
    /// this class used by the RxDataValidator to let the application know that
    /// something wrong with the incoming data
    /// </summary>
    public class DataValidatorEvent
    {
        public MarketDataMaof sync
        {
            get;
            set;
        }
        public MarketDataMaof async
        {
            get;
            set;
        }
    }

    /// <summary>
    /// This is a producer (see IProducer) 
    /// Given Connection object will open data stream and notify registered 
    /// data sinks (ISink)
    /// The class operates simultaneously in asynchronous and synchronous fashion. 
    /// The class installs an event listener by calling K300Class.K300StartStream
    /// Additonally class spawns a thread which does polling of the remote server 
    /// in case notification does not work correctly and to ensure that the data is 
    /// consistent. 
    /// There is a tricky part. I want to make sure that the data which we get by 
    /// polling and via asynchronous API is the same. Collector uses for this purpose
    /// a dedicated sink - thread which polls the servers and compares the received 
    /// data with the one sent to it by the collector
    /// </summary>
    public class Collector
    {
        public class MadadProducer : ProducerBase<MarketDataMadad>
        {
            public MadadProducer(K300EventsClass k3)
            {
                MadadListeners = new List<JQuant.ISink<MarketDataMadad>>(5);
                mktDta = new MarketDataMadad();
                k3.OnMadad += new _IK300EventsEvents_OnMadadEventHandler(OnMadad);
                countOnMadad = 0;
                Name = "Madad";
            }

            /// <summary>
            /// Called by TaskBarLib. This method calls registered listeners and gets out 
            /// The idea behind it to be as fast as possible
            /// this is the point where some basic processing can be done like filter obvious
            /// errors
            /// </summary>
            /// <param name="data">
            /// A <see cref="K300MaofType"/>
            /// </param>
            protected void OnMadad(ref K300MadadType data)
            {
                //Console.Write(".");
                // no memory allocation here - I am using allready created object 
                mktDta.k300MddType = data;
                countEvents++;

                // sink should not modify the data. sink has two options:
                // 1) handle the data in the context of the Collector thead
                // 2) clone the data and and postopone the procesing (delegate to another thread)
                foreach (JQuant.ISink<MarketDataMadad> sink in MadadListeners)
                {
                    sink.Notify(countOnMadad, mktDta);
                }
            }

            public override bool AddSink(JQuant.ISink<MarketDataMadad> sink)
            {
                Console.WriteLine("MadadListeners.Add(sink)");
                MadadListeners.Add(sink);
                return true;
            }

            public override bool RemoveSink(JQuant.ISink<MarketDataMadad> sink)
            {
                MadadListeners.Remove(sink);
                return true;
            }

            public override void GetEventCounters(out System.Collections.ArrayList names, 
                                                 out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(4);
                values = new System.Collections.ArrayList(4);
    
                names.Add("Events");values.Add(GetEvents());
                names.Add("Sinks");values.Add(GetSinks());
            }

            protected int GetSinks()
            {
                return MadadListeners.Count;
            }


            protected int GetEvents()
            {
                return countEvents;
            }
            
            protected static List<JQuant.ISink<MarketDataMadad>> MadadListeners;
            MarketDataMadad mktDta;
            int countOnMadad;
            int countEvents;
        }//class MadadProducer

        public class MaofProducer : JQuant.IProducer<MarketDataMaof>
        {
            public MaofProducer(K300EventsClass k3)
            {
                MaofListeners = new List<JQuant.ISink<MarketDataMaof>>(5);
                mktDta = new MarketDataMaof();
                k3.OnMaof += new _IK300EventsEvents_OnMaofEventHandler(OnMaof);
                countOnMaof = 0;
                Name = "Maof";
            }

            /// <summary>
            /// Called by TaskBarLib. This method calls registered listeners and gets out 
            /// The idea behind it to be as fast as possible
            /// this is the point where some basic processing can be done like filter obvious
            /// errors
            /// </summary>
            /// <param name="data">
            /// A <see cref="K300MaofType"/>
            /// </param>
            protected void OnMaof(ref K300MaofType data)
            {
                //Console.Write(".");
                // no memory allocation here - I am using allready created object 
                mktDta.k300MaofType = data;
                countEvents++;

                // sink should not modify the data. sink has two options:
                // 1) handle the data in the context of the Collector thead
                // 2) clone the data and and postopone the procesing (delegate to another thread)
                foreach (JQuant.ISink<MarketDataMaof> sink in MaofListeners)
                {
                    sink.Notify(countOnMaof, mktDta);
                }
            }

            public bool AddSink(JQuant.ISink<MarketDataMaof> sink)
            {
                Console.WriteLine("MaofListeners.Add(sink)");
                MaofListeners.Add(sink);
                return true;
            }

            public bool RemoveSink(JQuant.ISink<MarketDataMaof> sink)
            {
                MaofListeners.Remove(sink);
                return true;
            }


            public void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(4);
                values = new System.Collections.ArrayList(4);
    
                names.Add("Events");values.Add(GetEvents());
                names.Add("Sinks");values.Add(GetSinks());
            }

            protected int GetSinks()
            {
                return MaofListeners.Count;
            }

            protected int GetEvents()
            {
                return countEvents;
            }
            
            public string Name
            {
                get;
                set;
            }
            
            protected static List<JQuant.ISink<MarketDataMaof>> MaofListeners;        
            MarketDataMaof mktDta;
            int countOnMaof;
            int countEvents;
        }//class MaofProducer

        public class RezefProducer : ProducerBase<MarketDataRezef>
        {
            public RezefProducer(K300EventsClass k3)
            {
                RezefListeners = new List<JQuant.ISink<MarketDataRezef>>(5);
                mktDta = new MarketDataRezef();
                k3.OnRezef += new _IK300EventsEvents_OnRezefEventHandler(OnRezef);
                Name = "Rezef";
                countOnRezef = 0;
            }

            protected void OnRezef(ref K300RzfType data)
            {
                mktDta.k300RzfType = data;
                countEvents++;
                
                foreach (JQuant.ISink<MarketDataRezef> sink in RezefListeners)
                {
                    sink.Notify(countOnRezef, mktDta);
                }
            }

            public override bool AddSink(JQuant.ISink<MarketDataRezef> sink)
            {
                Console.WriteLine("RezefListeners.Add(sink)");
                RezefListeners.Add(sink);
                return true;
            }

            public override bool RemoveSink(JQuant.ISink<MarketDataRezef> sink)
            {
                RezefListeners.Remove(sink);
                return true;
            }

            public override void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values)
            {
                names = new System.Collections.ArrayList(4);
                values = new System.Collections.ArrayList(4);
    
                names.Add("Events");values.Add(GetEvents());
                names.Add("Sinks");values.Add(GetSinks());
            }

            protected int GetSinks()
            {
                return RezefListeners.Count;
            }
            
            protected int GetEvents()
            {
                return countEvents;
            }
            
            
            protected static List<JQuant.ISink<MarketDataRezef>> RezefListeners;
            MarketDataRezef mktDta;
            int countOnRezef;
            int countEvents;
        }//class RezefProducer

        public Collector(int sessionId)
        {
            // create a couple of TaskBarLib objects required for access to the data stream 
            if (k300Class == null)
            {
                k300Class = new K300Class();
                k300Class.K300SessionId = sessionId;
            }

            if (k300EventsClass  == null) 
                k300EventsClass = new K300EventsClass();
            
            //set the filters:
            k300EventsClass.EventsFilterBaseAsset = BaseAssetTypes.BaseAssetMaof;
            //k300EventsClass.EventsFilterBno=??? //here we set a single security, if specified
            k300EventsClass.EventsFilterMadad = 1; //I want to receive also madad changes - no way to filter specific madad here, get them all
            k300EventsClass.EventsFilterMaof = 1;
            k300EventsClass.EventsFilterMonth = MonthType.October;
            k300EventsClass.EventsFilterRezef = 1;
            k300EventsClass.EventsFilterStockKind = StockKind.StockKindMenaya;
            k300EventsClass.EventsFilterStockMadad = MadadTypes.TLV25;

            //initialize inner producers:
            maofProducer = new MaofProducer(k300EventsClass);
            rezefProducer = new RezefProducer(k300EventsClass);
            madadProducer = new MadadProducer(k300EventsClass);
        }

        public void Start(DataType dt)
        {
            switch (dt)
            {
                case DataType.Maof:
                    int tries = 0;
                    int rc=-1;
                    while (rc != 0 && tries < 5)
                    {
                        rc = k300Class.K300StartStream(K300StreamType.MaofStream);
                        Console.WriteLine("MaofStream Started, rc=" + rc);
                        tries++; ;
                        if (rc != 0) Thread.Sleep(5 * 1000);
                    }
                    break;
                case DataType.Rezef:
                    rc = k300Class.K300StartStream(K300StreamType.RezefStream);
                    break;
                case DataType.Madad:
                    rc=k300Class.K300StartStream(K300StreamType.IndexStream);
                    //OR - try this instead:
                    //rc = k300Class.K300StartStream(K300StreamType.MaofStream);
                    Console.WriteLine("IndexStream Started, rc=" + rc);
                    break;
                default:
                    break;      //do nothing
            }
        }

        public void Stop(DataType dt)
        {
            int rc;
            switch (dt)
            {
                case DataType.Maof:
                    rc=k300Class.K300StopStream(K300StreamType.MaofStream);
                    Console.WriteLine("MaofStream stopped, rc= " + rc);
                    break;
                
                case DataType.Rezef:
                    rc = k300Class.K300StopStream(K300StreamType.RezefStream);
                    Console.WriteLine("RezefStream stopped, rc= " + rc);
                    break;
                
                case DataType.Madad:
                    // It is still not clear which stream to start here, 
                    // because Madad data is supported either in the Maof stream
                    // or in a special Index stream - both do the job
                    //so chose one of the following:
                    
                    // ** 1 ** - here you need to register yorself with the 
                    // index events with appropriate method 'OnMadad'
                    //k300Class.K300StopStream(K300StreamType.MaofStream);
                    //Console.WriteLine("MaofStream stopped, rc= " + rc);

                    // ** 2 **
                    // this one definitely does the job so far
                    rc = k300Class.K300StopStream(K300StreamType.IndexStream);
                    Console.WriteLine("IndexStream stopped, rc= " + rc);
                    break;
                
                default:
                    break;
            }
        }

        public MaofProducer maofProducer;
        public RezefProducer rezefProducer;
        public MadadProducer madadProducer;


        protected K300Class k300Class;
        protected K300EventsClass k300EventsClass;

        protected MarketDataMaof marketDataOnMaof;
        protected MarketDataRezef marketDataOnRezef;
        protected MarketDataMadad marketDataOnMadad;

    }   //class Collector

    /// <summary>
    /// The class also is a sink registered with the Collector. When Collector sends 
    /// a new notification validator pushes the data to FIFO 
    /// 
    /// this is also a thread which polls API and gets market data synchronously. 
    /// looks for the data in the FIFO and if found removes 
    /// the entry from the FIFO. Validator expects that order of the events is the same 
    /// and if there is no match (miss) notifies all registered listeners (sinks) about
    /// data mismatch
    /// </summary>
    public abstract class RxDataValidator : JQuant.IProducer<DataValidatorEvent>, JQuant.ISink<MarketDataMaof>
    {
        // child will implment public constructor and set Name
        protected RxDataValidator()
        {
        }
        
        public bool AddSink(JQuant.ISink<DataValidatorEvent> sink)
        {
            return true;
        }

        public bool RemoveSink(JQuant.ISink<DataValidatorEvent> sink)
        {
            return true;
        }

        public string Name
        {
            get;
            set;
        }

        public abstract void GetEventCounters(out System.Collections.ArrayList names, out System.Collections.ArrayList values);
        
        /// <summary>
        /// called by Collector to notify about incoming event. Add the event to the FIFO
        /// </summary>
        /// <param name="count">
        /// A <see cref="System.Int32"/>
        /// </param>
        /// <param name="data">
        /// A <see cref="MarketData"/>
        /// </param>
        public void Notify(int count, MarketDataMaof data)
        {
        }
    }

    #endregion


    #region Connection Params;
    /// <summary>
    /// handle XML file containing connection parameters
    /// </summary>
    class ReadConnectionParameters : XmlTextReader
    {
        public ReadConnectionParameters(string filename)
            : base(filename)
        {
        }

        private enum xmlState
        {
            [Description("BEGIN")]
            BEGIN,
            [Description("PARAMS")]
            PARAMS,
            [Description("USERNAME")]
            USERNAME,
            [Description("PASSWORD")]
            PASSWORD,
            [Description("ACCOUNT")]
            ACCOUNT
        }

        public bool Parse(out ConnectionParameters parameters)
        {
            xmlState state = xmlState.BEGIN;
            string username = "";
            string password = "";
            string account = "";

            bool result = true;
            string val;
            parameters = null;

            while (base.Read())
            {
                switch (base.NodeType)
                {
                    case XmlNodeType.Element:
                        val = base.Name;
                        if ((val.Equals("connectionparameters")) && (state == xmlState.BEGIN))
                        {
                            state = xmlState.PARAMS;
                        }
                        else if ((val.Equals("username")) && (state == xmlState.PARAMS))
                        {
                            state = xmlState.USERNAME;
                        }
                        else if ((val.Equals("password")) && (state == xmlState.USERNAME))
                        {
                            state = xmlState.PASSWORD;
                        }
                        else if ((val.Equals("account")) && (state == xmlState.PASSWORD))
                        {
                            state = xmlState.ACCOUNT;
                        }
                        else
                        {
                            Console.WriteLine("Failed at element " + val + " in state " + JQuant.EnumUtils.GetDescription(state));
                            result = false;
                        }
                        break;

                    case XmlNodeType.Text:
                        val = base.Value;
                        if (state == xmlState.USERNAME)
                        {
                            username = val;
                        }
                        else if (state == xmlState.PASSWORD)
                        {
                            password = val;
                        }
                        else if (state == xmlState.ACCOUNT)
                        {
                            account = val;
                        }
                        else
                        {
                            Console.WriteLine("Failed at text " + val + " in state " + JQuant.EnumUtils.GetDescription(state));
                            result = false;
                        }
                        break;

                    case XmlNodeType.EndElement:
                        // I will not check that endelement Name is Ok 
                        val = base.Name;
                        if ((val.Equals("connectionparameters")) && (state == xmlState.ACCOUNT))
                        {
                        }
                        else if ((val.Equals("username")) && (state == xmlState.USERNAME))
                        {
                        }
                        else if ((val.Equals("password")) && (state == xmlState.PASSWORD))
                        {
                        }
                        else if ((val.Equals("account")) && (state == xmlState.ACCOUNT))
                        {
                        }
                        else
                        {
                            Console.WriteLine("Failed at EndElement " + val + " in state " + JQuant.EnumUtils.GetDescription(state));
                            result = false;
                        }
                        break;
                }

                // something is broken in the XML file
                if (result)
                {
                    parameters = new ConnectionParameters(username, password, account);
                }
                else
                {
                    break;
                }
            }


            return result;
        }

    }

    /// <summary>
    /// User login credentials, IP address and everything else required
    /// to establish and keep connection. This is just a data holder
    /// </summary>
    /// <returns>
    /// A <see cref="System.Int32"/>
    /// </returns>
    public class ConnectionParameters
    {
        //use only one constructor
        public ConnectionParameters(string name, string password, string account)
        {
            userName = name;
            userPassword = password;
            Account = account;

            //these two aren't actually used, but some TaskBar functions require them.
            //so we set them to default values
            appPassword = "";
            Branch = "000";
        }

        public string userName
        {
            get;
            protected set;
        }

        public string userPassword
        {
            get;
            protected set;
        }

        public string appPassword
        {
            get;
            protected set;
        }

        public string Account
        {
            get;
            protected set;
        }

        public string Branch
        {
            get;
            protected set;
        }
    }

    #endregion;

    #region Config AS400DateTime;

    /// <summary>
    /// Used to get latecny and synchronize local machine vs. AS400
    /// </summary>
    /// 
    public class AS400Synch
    {
        /// <summary>
        /// Returns latency in case of successful call, -1 otherwise
        /// </summary>
        /// <returns>A <see cref="System.Int32"/></returns>
        public static int GetLatency()
        {
            ConfigClass cs = new ConfigClass();
            AS400DateTime dt;
            int ltncy;
            int ret = cs.GetAS400DateTime(out dt, out ltncy);
            if (ret == 0) return ltncy;
            else return ret;
        }

        public static DateTime GetAS400DateTime()
        {
            ConfigClass cs = new ConfigClass();
            AS400DateTime dt;
            int ltncy;
            int ret = cs.GetAS400DateTime(out dt, out ltncy);
            return ConvertToDateTime(dt);
        }

        /// <summary>
        /// Ping once
        /// </summary>
        /// <param name="dt">A <see cref="AS400DateTime"/></param>
        /// <param name="latency">A <see cref="System.Int32"/></param>
        /// <returns>A <see cref="System.Boolean"/> true in case of sucess, false otherwise</returns>
        public static bool Ping(out DateTime dt, out int latency)
        {
            ConfigClass cs = new ConfigClass();
            AS400DateTime AS400dt;
            int ret = cs.GetAS400DateTime(out AS400dt, out latency);
            dt = ConvertToDateTime(AS400dt);
            if (ret == 0) return true;
            else return false;
        }

        /// <summary>
        /// Converts AS400DateTime to the .net 
        /// </summary>
        /// <param name="dt"><see cref="TaskBar.AS400DateTime"/></param>
        /// <returns><see cref="System.DateTime"/></returns>
        public static DateTime ConvertToDateTime(AS400DateTime dt)
        {
            return new DateTime(dt.year, dt.Month, dt.day, dt.hour, dt.minute, dt.second, dt.ms);
        }

        public static string ToShortCSVString(DateTime dt, int latency)
        {
            return DateTime.Now.ToString("hh:mm:ss.fff") + ","
                + dt.ToString("hh:mm:ss.fff") + ","
                + latency.ToString() + "\n";
        }
    }

    #endregion;

    #region Orders FSM;

    public enum FMROrderState
    {
        IDLE,
        INITIALIZED,
        SENT,
        WaitingFMR,
        WaitingTASE,
        PASSED,
        UpdatingCanceling,
        EXECUTED,
        CANCELED,
        FATAL,
    }

    public enum FMROrderEvent
    {
        InitOrder,
        Send,
        GetOrderId,
        GetInternalError,
        ApproveFMR,
        ApproveTASE,
        ApproveCancelTASE,
        Execution,
    }

    public class FMROrder : IMaofOrder
    {
        protected FMROrder()
        {
        }

        public System.DateTime Created
        {
            get;
            set;
        }

        public TransactionType TransType
        {
            get;
            set;
        }

        public OrderType OrdrType
        {
            get;
            set;
        }

        /// <summary>
        /// call this method to send events to the Order Processor 
        /// </summary>
        /// <param name="orderEvent">
        /// A <see cref="FMROrderEvent"/>
        /// </param>
        public void SendEvent(FMROrderEvent orderEvent)
        {
            // call Order Processor
            newEvent(this, orderEvent);
        }

        public delegate void NewEvent(FMROrder order, FMROrderEvent orderEvent);

        /// <value>
        /// set by Order Processor when the order is created 
        /// </value>
        public NewEvent newEvent
        {
            get;
            set;
        }
    }


    /// <summary>
    /// Represents a single trading directive. Essentially is a data container.
    /// It's kept inside a list in the FSMClass, along with other outstanding orders.
    /// FSMClass takes care of its porcessing, logging and reporting.
    /// </summary>
    public class MaofOrder : LimitOrderBase, IMaofOrder
    {

        /// <summary>
        /// call this method to send events to the Order Processor 
        /// </summary>
        /// <param name="orderEvent">
        /// A <see cref="FMROrderEvent"/>
        /// </param>
        public void SendEvent(FMROrderEvent orderEvent)
        {
            // call Order Processor
            newEvent(this, orderEvent);
        }

        public delegate void NewEvent(MaofOrder order, FMROrderEvent orderEvent);

        /// <value>
        /// set by Order Processor when the order is created 
        /// </value>
        public NewEvent newEvent
        {
            get;
            set;
        }

        //ImplementIMaofOrder:
        public DateTime Created
        {
            get;
            protected set;
        }

        public new TransactionType TransType
        {
            get;
            set;
        }

        public OrderType OrdrType
        {
            get;
            set;
        }

        /// <summary>
        /// FSM state 
        /// </summary>
        public FMROrderState State
        {
            get;
            protected set;
        }

        /// <summary>
        /// TaskBar object used for sending the trading directive to the API
        /// </summary>
        MaofOrderType maofOrderType;

        //various reference IDs

        /// <summary>
        /// TASE reference no. of approval
        /// </summary>
        public string Asmachta
        {
            get;
            protected set;
        }

        /// <summary>
        /// Exchange member's internal ref. no. of approval
        /// </summary>
        public string AsmachtaFMR
        {
            get;
            protected set;
        }

        /// <summary>
        /// this one is only for taking care of internal errors, 
        /// not needed if one of the Asmachtas obtained
        /// </summary>
        public int OrderId
        {
            get;
            protected set;
        }


        //Special variables required by FMR to treat internal errors:

        /// <summary>
        /// TaskBar type
        /// </summary>
        public OrdersErrorTypes ErrorType
        {
            get;
            protected set;
        }

        /// <summary>
        /// Placeholder for the internal error ref. no.
        /// </summary>
        public int ErrNo
        {
            get;
            protected set;
        }

        /// <summary>
        /// Placeholder for the string message which accompanies internal error arrival
        /// </summary>
        public string VbMsg
        {
            get;
            protected set;
        }

        /// <summary>
        /// placeholder for data used for resubmitting orders in case of internal error 
        /// effectively contains only corrected price or quantity (in case of ReEnter error)
        /// or "YES" or "NO" strings in case of Confirmation error
        /// </summary>
        public string ReEnteredValue
        {
            get;
            protected set;
        }


        //Constructor
        /// <summary>
        /// Default constructor - usual case is a LMT order, initialize an Order object ready to further processing.
        /// </summary>
        /// <param name="_conn"><see cref="FMRShell.Connection"/> currently active connection
        /// which keeps all the needed FMR data - username, password, account no.
        /// as well as UserClass instance used to communicate with the part of API responsible for 
        /// processing of orders</param>
        /// <param name="_transactionType"><see cref="JQuant.TransactionType"/>  - BUY or SELL</param>
        /// <param name="_option"><see cref="JQuant.Option"/></param>
        /// <param name="_quantity"><see cref="System.Int32"/> quantity of options to trade</param>
        /// <param name="_limitPrice"><see cref="System.Double"/></param>
        public MaofOrder(
            FMRShell.Connection _conn,
            TransactionType _transactionType,
            Option _option,
            int _quantity,
            double _limitPrice  //probably int will match better here, because TaskBar uses it as a string
            )
        {
            //create an instance of TaskBar order object:
            maofOrderType = new MaofOrderType();

            //and assign to it all the needed parameters:
            maofOrderType.Account = _conn.Parameters.Account;
            maofOrderType.Branch = _conn.Parameters.Branch;
            maofOrderType.operation = "N";  //always create a new order here, update ("U") or delete ("D") will be applied to an existing object
            maofOrderType.Option = _option.IdNum.ToString();
            maofOrderType.Sug_Pkuda = "LMT";
            if (_transactionType == TransactionType.BUY)
                maofOrderType.ammount = _quantity.ToString();
            else maofOrderType.ammount = "-" + _quantity.ToString();
            maofOrderType.price = _limitPrice.ToString();
        }

        //overloaded constructor that addresses order types other than default LMT
        //- either IOC or FOK (although LMT is supported by this one as well)
        //IOC or FOK orders aren't passed to the limit order book, so they can 
        //be used in pinging the market or for exploiting immediate arbitrage opportunities.
        public MaofOrder(
            FMRShell.Connection _conn,
            TransactionType _transactionType,
            OrderType _orderType,
            Option _option,
            int _quantity,
            double _limitPrice  //probably int will match better here, because TaskBar uses it as a string
            )
        {
            maofOrderType = new MaofOrderType();

            maofOrderType.Account = _conn.Parameters.Account;
            maofOrderType.Branch = _conn.Parameters.Branch;
            maofOrderType.operation = "N";  //always create a new order here, update ("U") or delete ("D") will be applied to an existing object
            maofOrderType.Option = _option.IdNum.ToString();
            maofOrderType.Sug_Pkuda = EnumUtils.GetDescription(_orderType);
            if (_transactionType == TransactionType.BUY)
                maofOrderType.ammount = _quantity.ToString();
            else maofOrderType.ammount = "-" + _quantity.ToString();
            maofOrderType.price = _limitPrice.ToString();
        }

    }//class MaofOrder




    /// <summary>
    /// implements Maof order FSM
    /// </summary>
    public class MaofOrderFSM : MailboxThread<object>, IOrderProcessor
    {
        public class OrderSink : ISink<LimitOrderParameters>
        {
            /// <summary>
            /// Points to the <see cref="JQuant.Algorithm"/> class
            /// </summary>
            Algorithm algo
            {
                get;
                set;
            }

            public LimitOrderParameters OrderParams
            {
                get;
                protected set;
            }

            //implement ISink.Notify:
            public void Notify(int count, LimitOrderParameters LmtParms)
            {
                OrderParams = LmtParms;
            }

            //TODO - add it to Algo object (producer)

        }

        //A placeholder for Order parameters - only one order a time is processed by the FSM
        public LimitOrderParameters OrderParams
        {
            get;
            protected set;
        }


        /*protected override void HandleMessage(LimitOrderParameters OrdParams)
        {
            this.OrderParams = OrdParams;   //keep the data received by mail from the Algo machine
        }*/


        //points to an active connection to the TaskBar
        Connection conn
        {
            get;
            set;
        }

        public MaofOrderFSM() :
            base("MaofOrderFSM", 100)
        {
        }

        public bool Create(LimitOrderParameters OrdParams, out IMaofOrder order)
        {
            if (MFOrdersList == null) MFOrdersList = new List<MaofOrder>(20);   //enable initial capacity of 20 orders
            MaofOrder maofOrder = null;
            maofOrder = new MaofOrder(conn, OrdParams.TransType, OrdParams.Opt, OrdParams.Quantity, OrdParams.Price);

            if (maofOrder != null)
            {
                // set callback - i want to get events from other tasks
                maofOrder.newEvent = NewEvent;
                maofOrder.SendEvent(FMROrderEvent.InitOrder);
            }

            MFOrdersList.Add(maofOrder);

            order = maofOrder;

            return (order != null);
        }


        #region FSM Matrix

        /// <summary>
        /// called by another thread when FMR has something to say about
        /// the order, for example, Ok or rejected
        /// </summary>
        void NewEvent(MaofOrder order, FMROrderEvent orderEvent)
        {

            switch (orderEvent)
            {
                case FMROrderEvent.InitOrder:
                    processInitOrder(order);
                    break;
                case FMROrderEvent.Send:
                    processSend(order);
                    break;
                case FMROrderEvent.GetOrderId:
                    processGetOrderId(order);
                    break;
                case FMROrderEvent.GetInternalError:
                    processGetInternalError(order);
                    break;
                case FMROrderEvent.ApproveFMR:
                    processApproveFMR(order);
                    break;
                case FMROrderEvent.ApproveTASE:
                    processApproveTASE(order);
                    break;
                case FMROrderEvent.ApproveCancelTASE:
                    processApproveCancelTASE(order);
                    break;
                case FMROrderEvent.Execution:
                    processExecution(order);
                    break;
                default:
                    break;
            }

        }

        //Only appropriate states count, other cases yield error
        void processInitOrder(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.IDLE:
                    break;
                case FMROrderState.PASSED:
                    break;
                default:
                    break;
            }

        }

        void processSend(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.INITIALIZED:
                    break;
                case FMROrderState.UpdatingCanceling:
                    break;
                default:
                    break;
            }
        }

        void processGetOrderId(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.SENT:
                    break;
                default:
                    break;
            }
        }

        void processGetInternalError(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.SENT:
                    break;
                default:
                    break;
            }
        }

        void processApproveFMR(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.WaitingFMR:
                    break;
                default:
                    break;
            }
        }

        void processApproveTASE(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.WaitingFMR:
                    break;
                case FMROrderState.WaitingTASE:
                    break;
                default:
                    break;
            }
        }

        void processApproveCancelTASE(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.WaitingFMR:
                    break;
                case FMROrderState.WaitingTASE:
                    break;
                default:
                    break;
            }
        }

        void processExecution(MaofOrder order)
        {
            switch (order.State)
            {
                case FMROrderState.WaitingFMR:
                    break;
                case FMROrderState.WaitingTASE:
                    break;
                default:
                    break;
            }
        }

        #endregion;


        //Do the work

        void InitOrder_IDLE()
        {
        }

        // create a message

        // send message to the mailbox

        
        
        
        
        
        public bool Submit(IMaofOrder order)
        {
            return true;
        }

        public bool Cancel(IMaofOrder order)
        {
            return true;
        }

        
        
        
        /// <summary>
        /// A storage place where FSM keeps all the active orders.
        /// </summary>
        List<MaofOrder> MFOrdersList;

    }

    #endregion;


    /// <summary>
    /// FSM which handles ebvents Login, Logout, Timer and can be in states - Idle, LinkUp, LinkDown
    /// If timer expires in LinkDown state FSM produces audible signal (beep)
    /// 
    /// ------------- Usage -------------
    /// FMRPing fmrPing = FMRPing.GetInstance();
    /// fmrPing.Start();   // start the FSM
    /// fmrPing.SendLogin(); // notify the FSM that ping should work now
    /// </summary>
    public class FMRPing : MailboxThread<FMRPing.Events>, IDisposable
    {

        public static FMRPing GetInstance()
        {
            if (instance == null)
            {
                instance = new FMRPing();
            }
            return instance;
        }

        public void SendLogin()
        {
            this.Send(Events.Login);
        }

        public void SendLogout()
        {
            this.Send(Events.Logout);
        }

        protected static FMRPing instance = null;
        protected enum State
        {
            Idle,
            LinkUp,
            LinkDown
        }
        
        public enum Events
        {
            // start the ping
            Login,
    
            // stop the ping
            Logout,
    
            // timer expired
            Timer,
    
            // send Ping
            PingTimer,
    
            // Ping returned
            PingOk,
            
            // Ping failed
            PingFailed
        }

        
        protected State state;

        /// <summary>
        /// use GetInstance() to get reference to the instance of the FMRPing 
        /// </summary>
        protected FMRPing() : base("FMRPing", 10)
        {
            int pingPeriod  = 2;
            
            // i need a timer and a working thread
            timerTask = new TimerTask("FMRPngTmr");
            timers_5sec = new TimerList("FMRPng5", 5*1000, 2, this.TimerExpiredHandler, timerTask);
            timers_2sec = new TimerList("FMRPng2", pingPeriod*1000, 2, this.PingTimerExpiredHandler, timerTask);
            timerTask.Start();

            Statistics2min = new IntStatistics("1 min", 1*60/pingPeriod); // pings in 2 min
            Statistics10min = new IntStatistics("10 min", 10*60/pingPeriod); // pings in 10 min
            Statistics1hour = new IntStatistics("1 hour", 1*60*60/pingPeriod); // pings in 1 hour
            
            MaxMin2min = new IntMaxMin("1 min", 1*60/pingPeriod); // pings in 2 min
            MaxMin10min = new IntMaxMin("10 min", 10*60/pingPeriod); // pings in 10 min
            MaxMin1hour = new IntMaxMin("1 hour", 1*60*60/pingPeriod); // pings in 1 hour
            
            state = State.Idle;
            jobQueue = CreateJobQueue();
        }


        public void Dispose()
        {
            jobQueue.Dispose();
            base.Dispose();
        }

        protected override void HandleMessage(Events taskEvent)
        {
            switch (state)
            {
            case State.Idle:
                HandleIdle(taskEvent);
                break;
            case State.LinkUp:
                HandleLinkUp(taskEvent);
                break;
            case State.LinkDown:
                HandleLinkDown(taskEvent);
                break;
            }
        }
        
        protected void HandleIdle(Events taskEvent)
        {
            switch (taskEvent)
            {
            case Events.Login:
                countPings = 0;
                configClass = new ConfigClass();
                StartTimer();
                StartPingTimer();
                state = State.LinkUp; // assume link up state
                break;
            case Events.Logout:
                Console.WriteLine("FMRPing: logout in the idle state");
                break;
            case Events.Timer:
                // do not restart the timer
                break;
            case Events.PingTimer:
                // do not restart the timer
                break;
            case Events.PingOk:
                // ignore ping results 
                break;
            case Events.PingFailed:
                // ignore ping results 
                break;
            }
        }
        
        protected void HandleLinkUp(Events taskEvent)
        {
            switch (taskEvent)
            {
            case Events.Login:
                Console.WriteLine("FMRPing: Login in the linkup state");
                break;
            case Events.Logout:
                jobQueue.Stop();
                jobQueue.Dispose();
                jobQueue = CreateJobQueue();
                
                state = State.Idle;
                break;
            case Events.Timer:
                if (countPings == 0)
                {
                    Console.WriteLine("FMRPing: ping failed in linkup state, move to linkdown");
                    state = State.LinkDown;
                }
                countPings = 0;
                // restart expired timer
                StartTimer();
                break;
            case Events.PingTimer:
                SendPing();
                StartPingTimer();
                break;
            case Events.PingOk:
                countPings++;
                CountPingOk++;
                break;
            case Events.PingFailed:
                Console.WriteLine("FMRPing: ping failed in the linkup state");
                CountPingFailed++;
                break;
            }
        }
        
        protected void HandleLinkDown(Events taskEvent)
        {
            switch (taskEvent)
            {
            case Events.Login:
                Console.WriteLine("FMRPing: Login in the linkdown state");
                break;
            case Events.Logout:
                jobQueue.Stop();
                jobQueue.Dispose();
                jobQueue = CreateJobQueue();
                
                state = State.Idle;
                break;
            case Events.Timer:
                if (countPings != 0)
                {
                    Console.WriteLine("FMRPing: move to linkup state");
                    state = State.LinkUp;
                }
                countPings = 0;
                // restart expired timer
                StartTimer();
                break;
            case Events.PingTimer:
                SendPing();
                StartPingTimer();
                DoBeep();
                break;
            case Events.PingOk:
                Console.WriteLine("FMRPing: ping Ok, move to linkup state");
                state = State.LinkUp;
                countPings++;
                CountPingOk++;
                break;
            case Events.PingFailed:
                // nothing new here
                CountPingFailed++;
                break;
            }
        }

        /// <summary>
        /// send timer expired to the FSM 
        /// </summary>
        protected void TimerExpiredHandler(ITimer timer)
        {
            this.Send(Events.Timer);
        }

        protected void PingTimerExpiredHandler(ITimer timer)
        {
            this.Send(Events.PingTimer);
        }
        
        protected void DoBeep()
        {
            Console.Beep();
        }
        
        /// <summary>
        /// delegate called by working thread 
        /// </summary>
        protected void DoPing(ref object o)
        {
            int latency;
            AS400DateTime AS400dt;
            int ret = configClass.GetAS400DateTime(out AS400dt, out latency);
            bool b = (ret == 0);

            // update statistics
            if (b)
            {
                Statistics2min.Add(latency);
                Statistics10min.Add(latency);
                Statistics1hour.Add(latency);
                
                MaxMin2min.Add(latency);
                MaxMin10min.Add(latency);
                MaxMin1hour.Add(latency);
            }
            
            o = b;
        }

        /// <summary>
        /// delegate called by working thread 
        /// </summary>
        protected void PingDone(object o)
        {
            if ((bool)o)
            {
                this.Send(Events.PingOk);
            }
            else
            {
                this.Send(Events.PingFailed);
            }
        }
        
        protected void SendPing()
        {
            jobQueue.AddJob(DoPing, PingDone, null);            
        }

        protected void StartTimer()
        {
            timers_5sec.Start();
        }

        protected void StartPingTimer()
        {
            timers_2sec.Start();
        }

        public IntStatistics Statistics2min
        {
            get;
            protected set;
        }

        public IntStatistics Statistics10min
        {
            get;
            protected set;
        }

        public IntStatistics Statistics1hour
        {
            get;
            protected set;
        }

        public IntMaxMin MaxMin2min
        {
            get;
            protected set;
        }

        public IntMaxMin MaxMin10min
        {
            get;
            protected set;
        }

        public IntMaxMin MaxMin1hour
        {
            get;
            protected set;
        }

        
        public int CountPingFailed
        {
            get;
            protected set;
        }

        public int CountPingOk
        {
            get;
            protected set;
        }

        protected static JQuant.JobQueue CreateJobQueue()
        {
            JQuant.JobQueue jobQueue = new JQuant.JobQueue("FMRPngJQ", 3);
            jobQueue.Start();
            
            return jobQueue;
        }
        
        protected ConfigClass configClass;
        protected TimerTask timerTask;
        protected TimerList timers_5sec;
        protected TimerList timers_2sec;
        protected int countPings;
        JQuant.JobQueue jobQueue;
    }

}//namespace
