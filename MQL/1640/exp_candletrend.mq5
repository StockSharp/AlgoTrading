//+------------------------------------------------------------------+
//|                                              Exp_CandleTrend.mq5 |
//|                               Copyright © 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------------------+
//  Trading algorithms                            | 
//+-----------------------------------------------+
#include <TradeAlgorithms.mqh>

//+----------------------------------------------+
//|  Calculated lots variants enumeration        |
//+----------------------------------------------+
/*enum MarginMode  - enumeration is declared in TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM considering account free funds
   BALANCE,          //MM considering account balance
   LOSSFREEMARGIN,   //MM for losses share from an account free funds
   LOSSBALANCE,      //MM for losses share from an account balance
   LOT               //Lot should be unchanged
  }; */
//+-----------------------------------------------+
//| Expert Advisor indicator input parameters     |
//+-----------------------------------------------+
input double MM=0.1;                //Share of a deposit in a deal
input         MarginMode MMMode=LOT; //lot value detection method
input int    StopLoss_=1000;        //Stop Loss in points
input int    TakeProfit_=2000;      //Take Profit in points
input int    Deviation_=10;         //max. price deviation in points
input bool   BuyPosOpen=true;       // Permission to enter long position
input bool   SellPosOpen=true;      //Permission to enter short position
input bool       BuyPosClose=true;      //Permission to exit long positions
input bool   SellPosClose=true;     // Permission to exit short positions
//+-----------------------------------------------+
//| Indicator input parameters                    |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator timeframe

input uint CandleTrendTotal=3; //number of candlesticks in one direction

input uint SignalBar=1;//bar index for getting an entry signal
//+-----------------------------------------------+
//---- Declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- declaration of variables for timeseries
double iOpen[],iClose[];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- memory allocation for arrays of variables
   if(ArrayResize(iOpen,CandleTrendTotal)!=CandleTrendTotal) Print("Failed to distribute the memory for iOpen[] array");
   if(ArrayResize(iClose,CandleTrendTotal)!=CandleTrendTotal) Print("Failed to distribute the memory for iClose[] array");

//---- Initialization of variables of data calculation starting point
   min_rates_total=int(SignalBar+CandleTrendTotal);
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
//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

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

      //---- copy newly appeared data into the arrays
      if(CopyOpen(Symbol(),InpInd_Timeframe,SignalBar,CandleTrendTotal,iOpen)<=0) {Recount=true; return;}
      if(CopyClose(Symbol(),InpInd_Timeframe,SignalBar,CandleTrendTotal,iClose)<=0) {Recount=true; return;}

      int count=0;

      for(int iii=0; iii<int(CandleTrendTotal); iii++)
        {
         if(iOpen[iii]<iClose[iii]) count++;
         else if(iOpen[iii]>iClose[iii]) count--;
        }

      //---- Getting buy signals
      if(count==CandleTrendTotal)
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(count==-CandleTrendTotal)
        {
         if(SellPosOpen) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
//---- Closing long
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Closing short   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- Opening long
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);

//---- Opening short
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
