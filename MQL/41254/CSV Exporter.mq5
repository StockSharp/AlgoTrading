//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2020, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+


static datetime Switch;
int x;
input int    UpdateTime  =900; // after x seconds EA will run again.
input int    CandleCount  =133; // EA will export y candles only. It will rewrite on existing cvs file. 

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   Switch = TimeCurrent();
   x = 1;
//---
   return(INIT_SUCCEEDED);
  }


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
   MqlRates OHLC[];
   MqlDateTime mdt;
   TimeCurrent(mdt);

   string sSymbol=Symbol();
   string  sPeriod=EnumToString(Period());
   string  ExtFileName;
   int copiedOHLC=CopyRates(NULL,0,0,CandleCount,OHLC);  // 288 bars per day on M5
   MqlRates PriceInfo[];
   ArraySetAsSeries(PriceInfo,true);
   int PriceData = CopyRates(_Symbol,_Period,0,3,PriceInfo);
   if(TimeCurrent() > (Switch+x))
     {
      ExtFileName=sSymbol;
      StringConcatenate(ExtFileName,sSymbol,"_",sPeriod,".CSV");



      int mySpreadsheetHandle = FileOpen(ExtFileName,FILE_WRITE|FILE_CSV|FILE_ANSI);
      FileSeek(mySpreadsheetHandle,0,SEEK_END);

      for(int i=1; i<=CandleCount-1; i++)
        {

         FileWrite(mySpreadsheetHandle,OHLC[i].close);  //_Symbol,OHLC[i].time, OHLC[i].time, OHLC[i].open, OHLC[i].high, , OHLC[i].tick_volume , OHLC[i].time,
         FileSeek(mySpreadsheetHandle,0,SEEK_END);
        }
      FileClose(mySpreadsheetHandle);
      Switch = TimeCurrent();
      x = UpdateTime;
     }
   Comment(" CSV Exporter ae running ... ");
  }
//+------------------------------------------------------------------+
