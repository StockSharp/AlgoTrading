//+------------------------------------------------------------------+
//|                                               Exp_3Parabolic.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Expert Advisor indicator input parameters    |
//+----------------------------------------------+
input double MM=-0.1;             // Share of a deposit in a deal, negative values - lot size
input int    StopLoss_=1000;      //stop loss in points
input int    TakeProfit_=2000;    //take profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     // Permission to buy
input bool   SellPosOpen=true;    // Permission to sell
input bool   BuyPosClose=true;     // Permission to exit long positions
input bool   SellPosClose=true;    // Permission to exit short positions
//+----------------------------------------------+
//| 3Parabolic indicator input parameters        |
//+----------------------------------------------+
input ENUM_TIMEFRAMES TimeFrame1=PERIOD_H6;  //1 Chart period for trend
input ENUM_TIMEFRAMES TimeFrame2=PERIOD_H3;  //2 Chart period for trend
input ENUM_TIMEFRAMES TimeFrame3=PERIOD_H1;  //3 Chart period for trend
input double SarStep=0.02;    //Step
input double SarMaximum=0.2;  //Maximum
input uint SignalBar=1;    //bar index for getting an entry signal
//+----------------------------------------------+

int TimeShiftSec;
//---- declaration of integer variables for the indicators handles
int InpInd_Handle_1,InpInd_Handle_2,InpInd_Handle_3;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//  Trading algorithms                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting handle of the iSAR indicator
   InpInd_Handle_1=iSAR(NULL,TimeFrame1,SarStep,SarMaximum);
   if(InpInd_Handle_1==INVALID_HANDLE) Print(" Failed to get handle of iSAR 1 indicator");

//---- getting handle of the iSAR indicator
   InpInd_Handle_2=iSAR(NULL,TimeFrame2,SarStep,SarMaximum);
   if(InpInd_Handle_2==INVALID_HANDLE) Print(" Failed to get handle of iSAR 2 indicator");

//---- getting handle of the iSAR indicator
   InpInd_Handle_3=iSAR(NULL,TimeFrame3,SarStep,SarMaximum);
   if(InpInd_Handle_3==INVALID_HANDLE) Print(" Failed to get handle of iSAR 3 indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(TimeFrame3);

//---- initialization of variables of the start of data calculation
   min_rates_total=int(2+SignalBar);
//----
   return(0);
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
   if(BarsCalculated(InpInd_Handle_1)<min_rates_total
      || BarsCalculated(InpInd_Handle_2)<min_rates_total
      || BarsCalculated(InpInd_Handle_3)<min_rates_total) return;

//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(TimeFrame1)-1,Symbol(),TimeFrame1);
   LoadHistory(TimeCurrent()-PeriodSeconds(TimeFrame2)-1,Symbol(),TimeFrame2);
   LoadHistory(TimeCurrent()-PeriodSeconds(TimeFrame3)-1,Symbol(),TimeFrame3);

//---- declaration of local variables
   double Sar_3[2],Close_3[2];
   static double Sar_1[1],Sar_2[1];
   static double Close_1[1],Close_2[1];
//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB1,NB2,NB3;

//---- copy newly appeared data into the arrays
   if(!SignalBar || NB1.IsNewBar(Symbol(),TimeFrame1) || Recount) // checking for a new bar
     {
      Recount=false;
      if(CopyBuffer(InpInd_Handle_1,0,SignalBar,1,Sar_1)<=0) {Recount=true; return;}
      if(CopyClose(Symbol(),TimeFrame1,SignalBar,1,Close_1)<=0) {Recount=true; return;}
      
      if(SellPosClose) if(Sar_1[0]<Close_1[0]) SELL_Close=true;
      if(BuyPosClose) if(Sar_1[0]>Close_1[0]) BUY_Close=true;
     }
     
//---- copy newly appeared data into the arrays
   if(!SignalBar || NB2.IsNewBar(Symbol(),TimeFrame2) || Recount) // checking for a new bar
     {
      Recount=false;
      if(CopyBuffer(InpInd_Handle_2,0,SignalBar,1,Sar_2)<=0) {Recount=true; return;}
      if(CopyClose(Symbol(),TimeFrame2,SignalBar,1,Close_2)<=0) {Recount=true; return;}
      
      if(SellPosClose) if(Sar_2[0]<Close_2[0]) SELL_Close=true;
      if(BuyPosClose) if(Sar_2[0]>Close_2[0]) BUY_Close=true;
     }

//+----------------------------------------------+
//| Detecting market entry signals               |
//+----------------------------------------------+
   if(!SignalBar || NB3.IsNewBar(Symbol(),TimeFrame3) || Recount) // checking for a new bar
     {
      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- copy newly appeared data into the arrays      
      if(CopyBuffer(InpInd_Handle_3,0,SignalBar,2,Sar_3)<=0) {Recount=true; return;}      
      if(CopyClose(Symbol(),TimeFrame3,SignalBar,2,Close_3)<=0) {Recount=true; return;}     

      if(SellPosClose) if(Sar_3[1]<Close_3[1]) SELL_Close=true;
      if(BuyPosClose) if(Sar_3[1]>Close_3[1]) BUY_Close=true;

      //---- Getting buy signals
      if(Sar_1[0]<Close_1[0] && Sar_2[0]<Close_2[0] && Sar_3[0]>Close_3[0] && Sar_3[1]<Close_3[1])
        {
         if(BuyPosOpen) BUY_Open=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),TimeFrame3,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(Sar_1[0]>Close_1[0] && Sar_2[0]>Close_2[0] && Sar_3[0]<Close_3[0] && Sar_3[1]>Close_3[1])
        {
         if(SellPosOpen) SELL_Open=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),TimeFrame3,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
//---- Closing a long position
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Closing a short position   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- Buying
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);

//---- Selling
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
