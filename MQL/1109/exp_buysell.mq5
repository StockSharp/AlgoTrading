//+------------------------------------------------------------------+
//|                                                  Exp_BuySell.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
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
//| BuySell indicator input parameters           |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input uint MA_Period=14;                          //indicator period
input ENUM_MA_METHOD MA_Method=MODE_SMA;          //smoothing type
input ENUM_APPLIED_PRICE MA_Price=PRICE_CLOSE;    //price
input uint ATR_Period=60;                         //ATR period
input uint SignalBar=1;                           //Bar index for getting an entry signal
//+----------------------------------------------+
//---- Declaration of integer variables for storing a chart period in seconds 
int TimeShiftSec;
//---- declaration of integer variables for the indicators handles
int InpInd_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//---- Declaration of integer variables for the indicator trading signals
double UpTrend[],DnTrend[];
//+------------------------------------------------------------------+
//  Trading algorithms                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting handle of the BuySell indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BuySell",MA_Period,MA_Method,MA_Price,ATR_Period);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of BullsBears indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MA_Period+ATR_Period);
   min_rates_total+=int(1+3+SignalBar);

//---- memory distribution for variables' arrays   
   int size=2;
   if(ArrayResize(UpTrend,size)<size) Print("Failed to distribute the memory for UpTrend[] array");
   if(ArrayResize(DnTrend,size)<size) Print("Failed to distribute the memory for DnTrend[] array");
//---- indexing elements in arrays as timeseries
   ArraySetAsSeries(UpTrend,true);
   ArraySetAsSeries(DnTrend,true);
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
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;

//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+----------------------------------------------+
//| Detecting market entry signals               |
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
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,UpTrend)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,DnTrend)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(UpTrend[0])
        {
         if(BuyPosOpen && DnTrend[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(DnTrend[0])
        {
         if(SellPosOpen && UpTrend[1]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
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
