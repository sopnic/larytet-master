
using System;


/// <summary>
/// I want to run TaskLib simulation. I am going to implement API using prerecorded 
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
        public virtual extern int GetMAOF(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad);
        public virtual extern int GetMAOFRaw(ref Array vecRecords, ref string strLastTime, string strOptionNumber, MadadTypes strMadad);
        public virtual extern int K300StartStream(K300StreamType streamType);
        public virtual extern int K300StopStream(K300StreamType streamType);
        public virtual extern int StartStream(K300StreamType streamType, string strStockNumber, MadadTypes strMadad, int withEvents);
        public virtual extern void StopUpdate(int pnID);
    }
}
