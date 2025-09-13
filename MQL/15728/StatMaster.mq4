//+------------------------------------------------------------------+
//|                                                       StatMaster |
//|                                       Copyright 2016, Il Anokhin |
//|                           http://www.mql5.com/en/users/ilanokhin |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Il Anokhin"
#property link "http://www.mql5.com/en/users/ilanokhin"
#property description ""
#property strict
//-------------------------------------------------------------------------
// Inputs
//-------------------------------------------------------------------------
enum mmperiod {Every_Minute,Every_Hour,Every_Day};
//-------------------------------------------------------------------------
input mmperiod SavePeriod=0;            //Save in CSV File Period
//-------------------------------------------------------------------------
// Variables
//-------------------------------------------------------------------------
int i,fn,ts,lm,lh,ld;

double lbid;

string tlog="DATE;TIME;ASK;BID;SPREAD\n";
//-------------------------------------------------------------------------
// 1. Main function
//-------------------------------------------------------------------------
void OnTick(void)
  {

   Comment("Copyright © 2016, Il Anokhin\nTicks saved: "+IntegerToString(ts));

//--- 1.1. Saving data in string variable ---------------------------------

   if(Bid!=lbid)
     {

      tlog=tlog+TimeToStr(TimeCurrent(),TIME_DATE)+";"+TimeToStr(TimeCurrent(),TIME_SECONDS)+";"+DoubleToStr(Ask,Digits)+";"+DoubleToStr(Bid,Digits)+";"+DoubleToStr(MarketInfo(Symbol(),MODE_SPREAD),0)+"\n";

      ts++;

     }

//--- 1.2. Writing data in csv file every selected period -----------------

   if(SavePeriod==0 && Minute()!=lm) {fn=FileOpen("StatMaster_"+Symbol()+".csv",FILE_WRITE|FILE_CSV); FileWrite(fn,tlog); FileClose(fn);}

   if(SavePeriod==1 && Hour()!=lh) {fn=FileOpen("StatMaster_"+Symbol()+".csv",FILE_WRITE|FILE_CSV); FileWrite(fn,tlog); FileClose(fn);}

   if(SavePeriod==2 && Day()!=ld) {fn=FileOpen("StatMaster_"+Symbol()+".csv",FILE_WRITE|FILE_CSV); FileWrite(fn,tlog); FileClose(fn);}

//--- 1.3. Getting last values of munute, hour, day and bid price ---------

   lm=Minute();

   lh=Hour();

   ld=Day();

   lbid=Bid;

//--- 1.4. End of main function -------------------------------------------

   return;

  }
//-------------------------------------------------------------------------
// 2. Deinitialization and writing data in csv file
//-------------------------------------------------------------------------
int deinit()
  {

   Alert("Data has been saved in MQL4/Files/StatMaster_"+Symbol()+".csv");

   fn=FileOpen("StatMaster_"+Symbol()+".csv",FILE_WRITE|FILE_CSV);

   FileWrite(fn,tlog);

   FileClose(fn);

   return(0);

  }
//-------------------------------------------------------------------------
