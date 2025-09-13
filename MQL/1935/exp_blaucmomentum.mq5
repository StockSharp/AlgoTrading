//+------------------------------------------------------------------+
//|                                            Exp_BlauCMomentum.mq5 |
//|                                Copyright © 2013,Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum AlgMode
  {
   breakdown,  // Break through zero
   twist       // Direction change
  };
//+----------------------------------------------+
//|  declaration of enumerations                 |
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
   PRICE_SIMPL_,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price
  };
//+----------------------------------------------+
//|  Trading algorithms                          |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| Input parameters of the EA indicator         |
//+----------------------------------------------+
input double MM=0.1;              // Share of a deposit in a deal, negative values - lot size
input MarginMode MMMode=LOT;      // Lot value detection method
input int    StopLoss_=1000;      // Stop Loss in points
input int    TakeProfit_=2000;    // Take Profit in points
input int    Deviation_=10;       // Max. price deviation in points
input bool   BuyPosOpen=true;     // Permission to buy
input bool   SellPosOpen=true;    // Permission to sell
input bool   BuyPosClose=true;    // Permission to exit long positions
input bool   SellPosClose=true;   // Permission to exit short positions
input AlgMode Mode=twist;         // Algorithm for entering the market
//+----------------------------------------------+
//| Input parameters of BlauCMomentum            |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Indicator timeframe
input Smooth_Method XMA_Method=MODE_EMA;          // Averaging method
input uint XLength=1;                             // Momentum averaging depth
input uint XLength1=20;                           // Depth of the first averaging
input uint XLength2=5;                            // Depth of the second averaging
input uint XLength3=3;                            // Depth of the third averaging
input int XPhase=15;                              // Smoothing parameter
//--- XPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- XPhase: for VIDIA it is a CMO period, for AMA it is a slow average period
input Applied_price_ IPC1=PRICE_CLOSE;            // Price constant for closing
input Applied_price_ IPC2=PRICE_OPEN;             // Price constant for opening
input uint SignalBar=1;                           // Bar number for getting an entry signal
//---- Declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//---- Declaration of integer variables for indicators handles
int InpInd_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting the handle of the BlauCMomentum indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BlauCMomentum",XMA_Method,XLength,XLength1,XLength2,XLength3,XPhase,IPC1,IPC2,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of BlauCMomentum");
      return(INIT_FAILED);
     }
//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;
//---- initialization of variables of the start of data calculation
   min_rates_total=int(XLength);
   min_rates_total+=XMA.GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_total+=XMA.GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total+=XMA.GetStartBars(XMA_Method,XLength3,XPhase);
   min_rates_total+=int(3+SignalBar);
   if(IPC1==IPC2 && XLength==1)
     {
      Print("Invalid values of the price constants of the indicator!");
      return(INIT_FAILED);
     }
//---- initialization end
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   GlobalVariableDel_(Symbol());
//----
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//---- declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//+----------------------------------------------+
//| Determining signals for deals                |
//+----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      if(Mode==breakdown)
        {
         //---- Declaration of local variables
         double Value[2];

         //---- copy newly appeared data into the arrays
         if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Value)<=0) {Recount=true; return;}

         //---- getting buy signals
         if(Value[1]>0)
           {
            if(BuyPosOpen && Value[0]<=0)
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }

         //---- Getting sell signals
         if(Value[1]<0)
           {
            if(SellPosOpen && Value[0]>=0)
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }

      if(Mode==twist)
        {
         //---- Declaration of local variables
         double Value[3];

         //---- copy newly appeared data into the arrays
         if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}

         //---- getting buy signals
         if(Value[1]<Value[2])
           {
            if(BuyPosOpen && Value[0]>=Value[1])
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }

         //---- Getting sell signals
         if(Value[1]>Value[2])
           {
            if(SellPosOpen && Value[0]<=Value[1])
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
     }
//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
//---- Closing a long position
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//---- Closing a short position   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//---- Opening a long position
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---- Opening a short position
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+