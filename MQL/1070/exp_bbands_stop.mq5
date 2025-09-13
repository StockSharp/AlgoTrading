//+------------------------------------------------------------------+
//|                                              Exp_BBands_Stop.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Expert Advisor indicator input parameters        |
//+----------------------------------------------+
input double MM=-0.1;             //Share of a deposit in a transaction, negative values - lot size
input int    StopLoss_=1000;      //Stop Loss in points
input int    TakeProfit_=2000;    //Take Profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     //Permission to enter long
input bool   SellPosOpen=true;    //Permission to enter short
input bool   BuyPosClose=true;     //Permission to exit long
input bool   SellPosClose=true;    //Permission to exit short
//+----------------------------------------------+
//| Exp_BBands_Stop indicator input parameters |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input int Length=20; //Bollinger period
input double Deviation=2; //Deviation
input double MoneyRisk=1.00; //Offset Factor
input uint SignalBar=1;//bar index for getting an entry signal
//+----------------------------------------------+
//---- Declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle;
//---- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//  Trade algorithms                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting the BBands_Stop_v1 indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BBands_Stop_v1",Length,Deviation,MoneyRisk,1,false);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get the BBands_Stop_v1 indicator handle");

//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of data starting point
   min_rates_total=int(Length+1+3+SignalBar);
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
//---- checking for the sufficiency of the number of bars for the calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;

//---- uploading history for proper operation of the functions IsNewBar() and SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Declaration of local variables
   double UpValue[2],DnValue[2];
//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+----------------------------------------------+
//| Determining transaction signals              |
//+----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- copy new data into the arrays
      if(CopyBuffer(InpInd_Handle,4,SignalBar,2,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,DnValue)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(UpValue[1])
        {
         if(BuyPosOpen&&DnValue[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(DnValue[1])
        {
         if(SellPosOpen&&UpValue[0]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//+----------------------------------------------+
//| Executing transactions                            |
//+----------------------------------------------+
//---- Closing long
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Closing short   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- Opening long
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);

//---- Opening short
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
