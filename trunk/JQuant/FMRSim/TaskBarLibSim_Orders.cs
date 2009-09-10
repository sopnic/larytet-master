
using System;

namespace TaskBarLibSim
{
    /// <summary>
    /// MOFINQType can be used to parse the records returned from User.GetOrdersMaof
    /// </summary>
    public struct MOFINQType
    {
        public string ERR;          //שגוי
        public string SYS_TYPE;     //סיסטם מקור הפקודה
        public string SND_RCV; 	    // -מטרת השדר
        public string ORDR_TYPE; 	//MANA/ORDR  שיטת הפקודה
        public string OPR_NAME; 	// סיסמאת המשתמש
        public string SUG_INQ; 	    //0=DETAIL     סוג פירוט
        public string SEQ_PIC; 	    // FMR  -    ספרור שוטף
        public string MANA_PIC; 	//מספר מנה/שעה אחרונה
        public string BNO_PIC; 	    //מספר נייר ערך
        public string BNO_NAME; 	//שם נייר
        public string OP;           //קוד קניה / מכירה
        public string BRANCH_PIC; 	//סניף
        public string TIK_PIC; 	    //תיק
        public string SUG_MAVR_PIC; // סוג  חשבון מפצל/מעבר
        public string ID_MAVR_PIC;  //מספר חשבון מפצל/מעבר
        public string SUG_PIC; 	    // סוג חשבון
        public string ID_PIC; 	    //חשבון
        public string ID_NAME; 	    //שם החשבון
        public string SUG_ID_PIC; 	//סוג חשבון
        public string NOSTRO; 	    //קוד נוסטרו
        public string ORDR_NV_PIC;  //כמות מבוקשת
        public string ORDR_SUG; 	//סוג פקודה
        public string ORDR_PRC_PIC; //מחיר הזמנה
        public string ORDR_TIME; 	//זמן קליטת ההזמנה
        public string DIL_NV_PIC; 	//כמות בעסקה
        public string DIL_PRC_PIC;  //מחיר עסקה
        public string DIL_TIME_PIC; //שעת העסקה
        public string MBR_SEQ_PIC;  //מספר הודעת חבר
        public string RZF_SEQ_PIC;  //מספר הודעת רצף
        public string RZF_ORD_PIC;  //מספר פקודה ברצף
        public string ORDER_NO_PIC; //HOST - ספרור מיוחד
        public string DIL_PIC; 	    //מספר אישור עסקה
        public string COD_UPD; 	    //סטטוס הפקודה
        public string STS; 	        //סטטוס הפקודה
        public string ERR_DATA; 	//נתונים חסרים/שגויים
        public string ERR_INQ; 	    //שגוי
        public string ERR_UPD; 	    //שגוי
        public string SUG_INFO; 	//0=ORDR  1=DIL  2=SUM  
        public string DSP_FMR; 	    //מצב הפקודה
    }

    /// <summary>
    /// RZFINQType can be used to parse the records returned from User.GetOrdersRezef
    /// </summary>
    public struct RZFINQType
    {
        public string ERR; 	        //שגוי
        public string SYS_TYPE; 	//סיסטם מקור הפקודה
        public string SND_RCV; 	    // -מטרת השדר
        public string ORDR_TYPE;    //MANA/ORDR  שיטת הפקודה
        public string OPR_NAME; 	// סיסמאת המשתמש
        public string SUG_INQ; 	    //0=DETAIL     סוג פירוט
        public string SEQ_N; 	    // FMR  -    ספרור שוטף
        public string MANA_N; 	    //מספר מנה/שעה אחרונה
        public string BNO_N; 	    //מספר נייר ערך
        public string BNO_NAME; 	//שם נייר
        public string OP; 	        //קוד קניה / מכירה
        public string BRANCH_N; 	//סניף
        public string TIK_N; 	    //תיק
        public string SUG_MAVR_N;   // סוג  חשבון מפצל/מעבר
        public string ID_MAVR_N;    //מספר חשבון מפצל/מעבר
        public string SUG_N; 	    // סוג חשבון
        public string ID_N; 	    //חשבון
        public string ID_NAME; 	    //שם החשבון
        public string SUG_ID_N; 	//סוג חשבון
        public string NOSTRO; 	    //קוד נוסטרו
        public string ORDR_NV_N;    //כמות מבוקשת
        public string ORDR_SUG; 	//סוג פקודה
        public string ORDR_PRC_N;   //מחיר הזמנה
        public string ORDR_TIME;    //זמן קליטת ההזמנה
        public string DIL_NV_N; 	//כמות בעסקה
        public string DIL_PRC_N;    //מחיר עסקה
        public string DIL_TIME_N;   //שעת העסקה
        public string MBR_SEQ_N;    //מספר הודעת חבר
        public string RZF_SEQ_N;    //מספר הודעת רצף
        public string RZF_ORD_N;    //מספר פקודה ברצף
        public string ORDER_NO_N;   //HOST - ספרור מיוחד
        public string DIL_N; 	    //מספר אישור עסקה
        public string COD_UPD; 	    //סטטוס הפקודה
        public string STS; 	        //סטטוס הפקודה
        public string ERR_DATA; 	//נתונים חסרים/שגויים
        public string ERR_INQ;  	//שגוי
        public string ERR_UPD; 	    //שגוי
        public string SUG_INFO; 	//0=ORDR  1=DIL  2=SUM  
        public string MSG1;	        // הודעה כללית
        public string STS_NAME; 	//מצב הפקודה
        public string DSP_FMR; 	    //מצב הפקודה                              
    }

    /// <summary>
    /// Numerical definition of the error type returned by SendRezefOrder and SendMaofOrder processes.
    /// </summary>
    public enum OrdersErrorTypes 
    {
        Fatal,
        Confirmation,
        ReEnter,
        PasswordReq,
        Alert,
        NoError 
    }

    /// <summary>
    /// Numerical definition of the Trading type for which Holdings will be retrieved.
    /// </summary>
    public enum TradeType 
    { 
        ALLTradeType = -1,
        MF = 0,
        RF = 1 
    }

    /// <summary>
    /// Type of calculation type for calculating account securities (Margin requirements).
    /// It is ususally set to RM_Dill_ShortOrders. 
    /// </summary>
    public enum SecurityCalcType
    {
        RMOnly = 0,
        RM_Dill = 1,
        RM_Dill_Orders = 2,
        RM_Dill_ShortOrders = 3,
    }

    /// <summary>
    /// Maof Basic Order Structure
    /// </summary>
    public struct MaofOrderType
    {
        public string Branch;       //סניף
        public string Account;      //חשבון
        public string Option;       //מספר אופציה
        public string operation;    //פעולה
        public string ammount;      //כמות
        public string price;        //מחיר
        public string Sug_Pkuda;    //סוג פקודה
        public string Asmachta;     //אסמכתא
        public string AsmachtaFmr;  //אסמכתא פ.מ.ר
        public int Pass;            //נקלט
        public int OrderID;         //זיהוי פקודה פנימי
    }
    /// <summary>
    /// Basic Rezef Order Structure
    /// </summary>
    public struct RezefBasicOrder 
    {
        public string operation;
        public string asmachta_fmr;
        public string ammount;
        public string price;
        public string Stock_Number;
        public string OP;
        public string Branch;
        public string Account;
        public string order_type; 
        public string asmachta_rezef;
        public string price_percent;
        public string shlav; 
        public string Nv_Del;
        public string ORDR_TYPE;
        public string Mana;
        public string Zira;
        public string Nv_Min;
        public string Strat_Date; 
        public string end_date;
    }

    /// <summary>
    /// מבנה רשומת הוראה חדשה
    /// </summary>
    public struct RezefSimpleOrder 
    {
        public int BNO;     //מספר נייר ערך
        public int Amount;  //כמות
        public double price;//מחיר
        public int Branch;  //סניף
        public int Account; //חשבון
        public OrderOperation operation;    //סוג פעולה
        public RezefOrderKind OrderKind;    //סוג הוראה
        public long Query;  //הוראת שאילתא
    }

    /// <summary>
    /// פעולות קניה ומכירה
    /// </summary>
    public enum OrderOperation
    {
        OrderOperationNewBuy,
        OrderOperationNewSell,
        OrderOperationUpdBuy,
        OrderOperationUpdSell,
        OrderOperationDelete
    }

    /// <summary>
    /// סוגי פקודות ברצף
    /// </summary>
    public enum RezefOrderKind
    {
        RezefOrderKindLMT = 0,  //Lmt order - this one is probably we're going to use, if any
        RezefOrderKindMKT = 1,  //MKT
        RezefOrderKindATC = 2,  //At Close auction
        RezefOrderKindLMO = 3,  //Limit at opening auction
        RezefOrderKindKRN = 4,  //??? - need to chek it
        RezefOrderKindJMB = 5   //Jumbo - need special permissions (it's for instituational guys)
    }

    /// <summary>
    /// Base Asset Type Enumaration.
    /// (FMR mean 'underlying' by 'base')
    /// We're intersted exclusively in BaseAssetMaof(=1), at the moment
    /// </summary>
    public enum BaseAssetTypes
    {
        BaseAssetAll = -1,
        BaseAssetBanks = 4,
        BaseAssetDollar = 2,
        BaseAssetEuro = 5,
        BaseAssetInterest = 3,
        BaseAssetMaof = 1,          //this one we're going to use
        BaseAssetShacharAroch = 7,
        BaseAssetShacharBenoni = 6
    }

    /// <summary>
    /// Underlying asset info
    /// *base assets info*
    /// </summary>
    public struct BaseAssetType
    {
        public int BaseAssetCode;   //base asset code
        public int BNO;             //base asset bno
        public int nCurr;           //currency code  
        public string BaseAssetKind;// base asset type 
        public string TradeMethod;  //trade method
        public double Value;        //morning value  
        public double Interest;     //interest
        public double Dividend;     //dividend
        public double StdIv;        //standard deviation   
        public double ValueChange;  //value change 
        public double StdIdChange;  //standard dev change
        public double Mult;         //value multiplier
        public int ExpiresToday;    //any derivative expiration
        public int ExpDate;         //nearest expiration date
        public int ExpDays;         //days to expiration date
        public string NameHeb;      //hebrew name
        public string NameEng;      //english name
    }

    /// <summary>
    /// drop here everything related to orders sending
    /// </summary>
    public partial class UserClass
    {
        /// <summary>
        /// Begins Maof trading session.
        /// </summary>
        /// <param name="sessionId">A <see cref="System.Int32"/>
        /// Unique Session Identification number that 
        /// identifies the session for which the function is called.</param>
        /// <param name="Account">A<see cref="System.String"/>
        /// The account for which the maof session will be initialized.</param>
        /// <param name="Branch">A<see cref="System.String"/>
        /// The branch for which the maof session will be initialized.</param>
        /// <param name="calcType">A<see cref="TaskBarLibSim.SecurityCalcType"/>
        /// Type of calculation type for calculating account securities.
        /// It is ususally set to RM_Dill_ShortOrders.</param>
        /// <returns>A <see cref="System.Int32"/>  
        /// 1 : Unable to begin Maof session.
        /// 0 : Maof session started successfully.
        /// -1 : General function failure.
        /// -2 : Account or branch parameters not relayed to function.
        /// -3 : DataBase failure.
        /// -4 : Account not found.
        /// -5 : Short Account not found.
        /// -6 : Inadequate "Generator" authorization.
        /// -7 : Inadequate User authorization.</returns>
        public virtual int StartMaofSession(int sessionId, string Account, string Branch, SecurityCalcType calcType)
        {
            return 0;
        }

        /// <summary>
        /// Terminates Maof trading session.
        /// </summary>
        /// <param name="sessionId"><param name="sessionId">A <see cref="System.Int32"/>
        /// Unique Session Identification number that 
        /// identifies the session for which the function is called.</param>
        /// <param name="Account">A<see cref="System.String"/>
        /// The account for which the maof session will be initialized.</param>
        /// <param name="Branch">A<see cref="System.String"/>
        /// The branch for which the maof session will be initialized.</param>
        /// <returns>A <see cref="System.Int32"/> 
        ///  1 : Unable to terminate Maof session.
        ///  0 : Maof session terminated successfully.
        ///  -1 : General function failure.
        ///  -5 : Inadequate user permissions.</returns>
        public virtual int StopMaofSession(int sessionId, string Account, string Branch)
        {
            return 0;
        }

        /// <summary>
        /// Begins Rezef trading session.
        /// </summary>
        /// <param name="sessionId">A <see cref="System.Int32"/>
        /// Unique Session Identification number that 
        /// identifies the session for which the function is called.</param>
        /// <param name="Account">A<see cref="System.String"/>
        /// The account for which the rezef session will be initialized.</param>
        /// <param name="Branch">A<see cref="System.String"/>
        /// The branch for which the rezef session will be initialized.</param>
        /// <param name="calcType">A<see cref="TaskBarLibSim.SecurityCalcType"/>
        /// Type of calculation type for calculating account securities.
        /// It is ususally set to RM_Dill_ShortOrders.</param>
        /// <returns>A <see cref="System.Int32"/>  
        /// 1 : Unable to begin Rezef session.
        /// 0 : Rezef session started successfully.
        /// -1 : General function failure.
        /// -2 : Account or branch parameters not relayed to function.
        /// -3 : DataBase failure.
        /// -4 : Account not found.
        /// -5 : Short Account not found.
        /// -6 : Inadequate User authorization.</returns>
        public virtual int StartRezefSession(int sessionId, string Account, string Branch)
        {
            return 0;
        }

        /// <summary>
        /// Terminates Rezef trading session
        /// </summary>
        /// <param name="sessionId">session id</param>
        /// <param name="Account">account no.</param>
        /// <param name="Branch">branch no.</param>
        /// <returns>A <see cref="System.Int32"/>  
        ///  0 : Rezef session terminated successfully.
        /// -1 : General function failure.
        /// -2 : Inadequate user permissions.</returns>
        public virtual int StopRezefSession(int sessionId, string Account, string Branch)
        {
            return 0;
        }

    }
    
}
