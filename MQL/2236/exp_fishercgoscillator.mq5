//+------------------------------------------------------------------+
//|                                       Exp_FisherCGOscillator.mq5 |
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
//| Enumeration for lot calculation options      |
//+----------------------------------------------+
/*enum MarginMode  - enumeration is declared in TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM considering account free funds
   BALANCE,          //MM considering account balance
   LOSSFREEMARGIN,   //MM for losses share from an account free funds
   LOSSBALANCE,      //MM for losses share from an account balance
   LOT               //Lot should be unchanged
  }; */
//+----------------------------------------------+
//| Expert Advisor input parameters               |
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
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H8; // Indicator timeframe
input int Length=10;                              // Indicator period 
input uint SignalBar=1;                           // Bar number for getting an entry signal
//+----------------------------------------------+
//---- Declaration of integer variables for storing the chart period in seconds 
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
//--- getting the handle of the FisherCGOscillator indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"FisherCGOscillator",Length,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of the FisherCGOscillator indicator");
//--- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- initialization of variables of the start of data calculation
   min_rates_total=int(Length);
   min_rates_total+=int(3+SignalBar);
//---
   return(0);
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
   double Ind[2],Sign[2];
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
      //--- copy newly appeared data in the arrays
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Ind)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Sign)<=0) {Recount=true; return;}
      //---- getting buy signals
      if(Ind[1]>Sign[1])
        {
         if(BuyPosOpen && Ind[0]<=Sign[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- Getting sell signals
      if(Ind[1]<Sign[1])
        {
         if(SellPosOpen && Ind[0]>=Sign[0]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
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
