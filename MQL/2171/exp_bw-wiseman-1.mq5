//+------------------------------------------------------------------+
//|                                             Exp_BW-wiseMan-1.mq5 |
//|                               Copyright © 2014, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2014, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Trading algorithms                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| Expert Advisor input parameters              |
//+----------------------------------------------+
input double MM=0.1;               // Share of a deposit in a deal
input MarginMode MMMode=LOT;       // Lot value calculation method
input int    StopLoss_=1000;       // Stop Loss in points
input int    TakeProfit_=2000;     // Take Profit in points
input int    Deviation_=10;        // Max. price deviation in points
input bool   BuyPosOpen=true;      // Permission to buy
input bool   SellPosOpen=true;     // Permission to sell
input bool   BuyPosClose=true;     // Permission to exit long positions
input bool   SellPosClose=true;    // Permission to exit short positions
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H1;         // Timeframe of BW-wiseMan-1
input bool                    retrogradely=true;          // Against trend
input uint                    back=2;                     // Number of bars for analysis
input uint                    jaw_period=13;              // Period for calculating jaws
input uint                    jaw_shift=8;                // Horizontal shift of jaws
input uint                    teeth_period=8;             // Period for calculating teeth
input uint                    teeth_shift=5;              // Horizontal shift of teeth
input uint                    lips_period=5;              // Period for lips calculation
input uint                    lips_shift=3;               // Horizontal shift of lips
input ENUM_MA_METHOD          ma_method=MODE_SMMA;        // Type of smoothing
input ENUM_APPLIED_PRICE      applied_price=PRICE_MEDIAN; // Price type or handle
input uint SignalBar=1;                                   // Bar number for getting the entry signal
//+----------------------------------------------+
int TimeShiftSec;
//--- declaration of integer variables for the indicators handles
int InpInd_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- getting the handle of the BW-wiseMan-1 indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BW-wiseMan-1",
                         back,jaw_period,jaw_shift,teeth_period,teeth_shift,lips_period,lips_shift,ma_method,applied_price);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of BW-wiseMan-1");
      return(INIT_FAILED);
     }
//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(lips_period,MathMax(jaw_period,teeth_period)));
   min_rates_total+=int(SignalBar);
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
//---- declaration of local variables
   double DnValue[1],UpValue[1];
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
      int N1,N2;
      //--- copy newly appeared data in the arrays
      if(!retrogradely)
        {
         N1=1;
         N2=0;
        }
      else
        {
         N1=0;
         N2=1;
        }
      //--- copy newly appeared data in the arrays
      if(CopyBuffer(InpInd_Handle,N1,SignalBar,1,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,N2,SignalBar,1,DnValue)<=0) {Recount=true; return;}
      //---- getting buy signals
      if(UpValue[0] && UpValue[0]!=EMPTY_VALUE)
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose)SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- Getting sell signals
      if(DnValue[0] && DnValue[0]!=EMPTY_VALUE)
        {
         if(SellPosOpen) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- Searching for the last trading direction for getting signals to close positions      
      if(((BuyPosOpen && BuyPosClose) || (SellPosOpen && SellPosClose)) && (!BUY_Close && !SELL_Close))
        {
         int Bars_=Bars(Symbol(),InpInd_Timeframe);

         for(int bar=int(SignalBar+1); bar<Bars_; bar++)
           {
            if(SellPosClose)
              {
               if(CopyBuffer(InpInd_Handle,N1,bar,1,UpValue)<=0) {Recount=true; return;}
               if(UpValue[0]!=0 && UpValue[0]!=EMPTY_VALUE)
                 {
                  SELL_Close=true;
                  break;
                 }
              }
            if(BuyPosClose)
              {
               if(CopyBuffer(InpInd_Handle,N2,bar,1,DnValue)<=0) {Recount=true; return;}
               if(DnValue[0]!=0 && DnValue[0]!=EMPTY_VALUE)
                 {
                  BUY_Close=true;
                  break;
                 }
              }
           }
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
