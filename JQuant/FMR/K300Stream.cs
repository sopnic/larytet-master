using System;
using TaskBarLib;
namespace ConsoleApplication1
{
    public class Collector
    {
        //Methods 
        K300Class MyK300 = new K300Class();                         //Class starting and stopping stream
        K300EventsClass MyK300Event = new K300EventsClass();        //Event - updating data
        
        //variables to keep last record
        //probably convert it to private stuct?
        string Bno_num;         //TASE security's id number
        string Lmt_by1;         //limit order buy price - level 1
        string Lmt_by2;         //limit order buy price - level 2
        string Lmt_by3;         //limit order buy price - level 3
        string Lmy_by1_nv;      //limit order buy quantity - level 1
        string Lmy_by2_nv;      //limit order buy quantity - level 2
        string Lmy_by3_nv;      //limit order buy quantity - level 3
        string Lmt_sl1;         //limit order sell price - level 1
        string Lmt_sl2;         //limit order sell price - level 2
        string Lmt_sl3;         //limit order sell price - level 3
        string Lmy_sl1_nv;      //limit order sell quantity - level 1
        string Lmy_sl2_nv;      //limit order sell quantity - level 1
        string Lmy_sl3_nv;      //limit order sell quantity - level 1
        string Lst_dl_pr;       //last deal price
        string Lst_dl_tm;       //last deal time
        string Lst_dl_vl;       //last deal volume
        string Upd_time;        //time the record was updated.(there is also UPD_DAT for date, just in case...)

        public Collector()
        {
            MyK300Event.OnMaof += new _IK300EventsEvents_OnMaofEventHandler(GrabMF);
        }

        /// <summary>
        /// Collects data from the stream and saves it locally
        /// </summary>
        public void GrabData()
        {
            MyK300.K300StartStream(K300StreamType.MaofStream);
        }

        void GrabMF(ref K300MaofType data)
        {
            Bno_num = data.BNO_Num;
            Lmt_by1 = data.LMT_BY1;
            Lmt_by2 = data.LMT_BY2;
            Lmt_by3 = data.LMT_BY3;
            Lmy_by1_nv = data.LMY_BY1_NV;
            Lmy_by2_nv = data.LMY_BY2_NV;
            Lmy_by3_nv = data.LMY_BY3_NV;
            Lmt_sl1 = data.LMT_SL1;
            Lmt_sl2 = data.LMT_SL2;
            Lmt_sl3 = data.LMT_SL3;
            Lmy_sl1_nv = data.LMY_SL1_NV;
            Lmy_sl2_nv = data.LMY_SL2_NV;
            Lmy_sl3_nv = data.LMY_SL3_NV;
            Lst_dl_pr = data.LST_DL_PR;
            Lst_dl_tm = data.LST_DL_TM;
            Lst_dl_vl = data.LST_DL_VL;
            Upd_time = data.UPD_TIME;
            //next we may want to send the data to the database or write it to binary stream:
            Console.WriteLine("GrabMF is working...");  //instead of real code for now...
        }
    }
}//namespace
