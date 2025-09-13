//+------------------------------------------------------------------+
//|                                             Exp_Digital_MACD.mq5 |
//|                               Copyright © 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Trading algorithms                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| CXMA class description                       |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//| declaration of enumerations                  |
//+----------------------------------------------+
enum AlgMode
  {
   breakdown,      // break through zero
   MACDtwist,      // change of MACD direction
   SIGNALtwist,    // change of signal line direction
   MACDdisposition // MACD histogram breaks through signal line
  };
//+----------------------------------------------+
//| declaration of enumerations                  |
//+----------------------------------------------+
enum Applied_price_      // type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE_,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   // TrendFollow_2 Price 
  };
//+----------------------------------------------+
//| Expert Advisor input parameters              |
//+----------------------------------------------+
input double  MM=0.1;              // Share of a deposit in a deal
input MarginMode MMMode=LOT;       // Lot value calculation method
input uint    StopLoss_=1000;      // Stop Loss in points
input uint    TakeProfit_=2000;    // Take Profit in points
input uint    Deviation_=10;       // Max. price deviation in points
input bool    BuyPosOpen=true;     // Permission to enter a long position
input bool    SellPosOpen=true;    // Permission to enter a short position
input bool    BuyPosClose=true;    // Permission to exit long positions
input bool    SellPosClose=true;   // Permission to exit short positions
input AlgMode Mode=MACDtwist;      // Algorithm for market entering
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Indicator timeframe
input Smooth_Method Signal_Method=MODE_SMA;       // Signal line averaging method
input int Signal_XMA=5;                           // Period of the signal line 
input int Signal_Phase=100;                       // Parameter of the signal line
input Applied_price_ AppliedPrice=PRICE_CLOSE_;   // Price constant
input uint SignalBar=1;                           // Bar number for getting an entry signal
//+----------------------------------------------+
//--- Declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle;
//--- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- receiving the handle of Digital_MACD
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"Digital_MACD",Signal_Method,Signal_XMA,Signal_Phase,AppliedPrice);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of Digital_MACD");
      return(INIT_FAILED);
     }
//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- initialization of variables of the start of data calculation
   min_rates_total=66+GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;
   min_rates_total=int(min_rates_total+3+SignalBar);
//--- initialization end
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   GlobalVariableDel_(Symbol());
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- checking if the number of bars is enough for the calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//---- declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//--- determining signals for deals
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      switch(Mode)
        {
         case breakdown:
           {
            double Value[2];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Value)<=0) {Recount=true; return;}
            //---- getting buy signals
            if(Value[1]>0)
              {
               if(BuyPosOpen && Value[0]<=0) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //---- Getting sell signals
            if(Value[1]<0)
              {
               if(SellPosOpen && Value[0]>=0) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case MACDtwist:
           {
            double Value[3];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}
            //---- getting buy signals
            if(Value[1]<Value[2])
              {
               if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //---- Getting sell signals
            if(Value[1]>Value[2])
              {
               if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case SIGNALtwist:
           {
            double Value[3];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,1,SignalBar,3,Value)<=0) {Recount=true; return;}
            //---- getting buy signals
            if(Value[1]<Value[2])
              {
               if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //---- Getting sell signals
            if(Value[1]>Value[2])
              {
               if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case MACDdisposition:
           {
            double MACD[2],Signal[2];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,MACD)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Signal)<=0) {Recount=true; return;}
            //---- getting buy signals
            if(MACD[1]>Signal[1])
              {
               if(BuyPosOpen  &&  MACD[0]<=Signal[0]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //---- Getting sell signals
            if(MACD[1]<Signal[1])
              {
               if(SellPosOpen && MACD[0]>=Signal[0]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;
        }
     }
//--- trading
//---- Closing a long position
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//---- Closing a short position   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//--- Open a long position
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//--- Open a short position
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---
  }
//+------------------------------------------------------------------+
