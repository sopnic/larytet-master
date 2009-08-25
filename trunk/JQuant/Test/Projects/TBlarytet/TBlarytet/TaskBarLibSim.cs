using System;

/// <summary>
/// I want to run TaskBarLib simulation. I am going to implement API using prerecorded 
/// or generated in some other way log files as a data feed
/// Only small part of the API is implemented 
/// </summary>
namespace TaskBarLibSim
{
    public enum BaseAssetTypes
    {
        BaseAssetAll = -1,
        BaseAssetBanks = 4,
        BaseAssetDollar = 2,
        BaseAssetEuro = 5,
        BaseAssetInterest = 3,
        BaseAssetMaof = 1,
        BaseAssetShacharAroch = 7,
        BaseAssetShacharBenoni = 6
    }
	
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
        MaofCNTStream = 50,
        MaofStream = 0x30,
        RezefCNTStream = 0x33,
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
	
    public enum OrderOperation
    {
        OrderOperationNewBuy,
        OrderOperationNewSell,
        OrderOperationUpdBuy,
        OrderOperationUpdSell,
        OrderOperationDelete
    }
	
    public enum OrdersErrorTypes
    {
        Alert = 0x34,
        Confirmation = 0x31,
        Fatal = 0x30,
        NoError = 0x35,
        PasswordReq = 0x33,
        ReEnter = 50
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
        event IK300Event_FireMaofEventHandler FireMaof;
        event IK300Event_FireMaofCNTEventHandler FireMaofCNT;
        event IK300Event_FireRezefEventHandler FireRezef;
        event IK300Event_FireRezefCNTEventHandler FireRezefCNT;
    }
	
	public class K300Class : IK300, K300, IK300Event_Event
    {
        // Events
        public event IK300Event_FireMaofEventHandler FireMaof;
        public event IK300Event_FireMaofCNTEventHandler FireMaofCNT;
        public event IK300Event_FireRezefEventHandler FireRezef;
        public event IK300Event_FireRezefCNTEventHandler FireRezefCNT;

        // Methods
        public virtual int GetMAOF(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad)
		{
			return 0;
		}
		
        public virtual int GetMAOFRaw(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad)
		{
			return 0;
		}
		
        public virtual int K300StartStream(K300StreamType streamType)
		{
			return 0;
		}
		
        public virtual int K300StopStream(K300StreamType streamType)
		{
			return 0;
		}
		
        public virtual int StartStream(K300StreamType streamType, string strStockNumber, MadadTypes strMadad, int withEvents)
		{
			return 0;
		}
		
        public virtual void StopUpdate(int pnID)
		{
		}
		
    }

    public class UserClass
	{
		public int Login(string username, string AS400Password, string AppPassword, 
            out string message, out int sessionId)
		{
			message = "";
			sessionId = 0;
			
			return 0;
		}
	}
		
}
