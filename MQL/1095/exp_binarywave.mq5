//+------------------------------------------------------------------+
//|                                               Exp_BinaryWave.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  Declaration of enumerations                 |
//+----------------------------------------------+
enum AlgMode
  {
   breakdown,  //breakthrough of zero
   twist       //changing direction
  };
//+----------------------------------------------+
//|  Declaration of enumerations                 |
//+----------------------------------------------+
/*
enum Smooth_Method
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  };
*/
//+----------------------------------------------+
//| Expert Advisor input parameters              |
//+----------------------------------------------+
input double MM=-0.1;             // Share of a deposit in a deal, negative values - lot size
input int     StopLoss_=1000;      //stop loss in points
input int     TakeProfit_=2000;    //take profit in points
input int     Deviation_=10;       //max. price deviation in points
input bool    BuyPosOpen=true;     //Permission to buy
input bool    SellPosOpen=true;    //Permission to sell
input bool    BuyPosClose=true;     //Permission to exit long positions
input bool    SellPosClose=true;    //Permission to exit short positions
input AlgMode Mode=breakdown;       //algorithm to enter in the market 
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
//---- indicators weight. The indicator does not take part in the wave calculation in case of a zero value
input double WeightMA    = 1.0;
input double WeightMACD  = 1.0;
input double WeightOsMA  = 1.0;
input double WeightCCI   = 1.0;
input double WeightMOM   = 1.0;
input double WeightRSI   = 1.0;
input double WeightADX   = 1.0;
//---- Moving Average parameters
input int   MAPeriod=13;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//---- MACD parameters
input int   FastMACD     = 12;
input int   SlowMACD     = 26;
input int   SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//---- OsMA parameters
input int   FastPeriod   = 12;
input int   SlowPeriod   = 26;
input int   SignalPeriod = 9;
input ENUM_APPLIED_PRICE   OsMAPrice=PRICE_CLOSE;
//---- CCI parameters
input int   CCIPeriod=14;
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_MEDIAN;
//---- Momentum parameters
input int   MOMPeriod=14;
input ENUM_APPLIED_PRICE   MOMPrice=PRICE_CLOSE;
//---- RSI parameters
input int   RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
//---- ADX parameters
input int   ADXPeriod=14;
//---- including wave smoothing
input Smooth_Method bMA_Method=MODE_JJMA; //smoothing method
input int bLength=5; //smoothing depth                    
input int bPhase=100; //smoothing parameter,
                      // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
input uint SignalBar=1;//bar index for getting an entry signal
//+----------------------------------------------+
//---- Declaration of integer variables for storing a chart period in seconds 
int TimeShiftSec;
//---- declaration of integer variables for the indicators handles
int InpInd_Handle;
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
//---- getting handle of the BinaryWave indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BinaryWave",WeightMA,WeightMACD,
                         WeightOsMA,WeightCCI,WeightMOM,WeightRSI,WeightADX,MAPeriod,MAType,MAPrice,
                         FastMACD,SlowMACD,SignalMACD,PriceMACD,FastPeriod,SlowPeriod,SignalPeriod,
                         OsMAPrice,CCIPeriod,CCIPrice,MOMPeriod,MOMPrice,RSIPeriod,RSIPrice,ADXPeriod,
                         bMA_Method,bLength,bPhase);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of BinaryWave indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;

//---- Initialization of variables of the start of data calculation
   int min_rates_1=MathMax(MAPeriod,MathMax(SlowPeriod,MathMax(CCIPeriod,MathMax(SlowMACD,MOMPeriod))))+1;
   int min_rates_2=min_rates_1+XMA.GetStartBars(bMA_Method,bLength,bPhase);
   min_rates_total=int(min_rates_2+3+SignalBar);
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

      if(Mode==breakdown)
        {
         double Value[2];
         //---- copy newly appeared data into the arrays
         if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Value)<=0) {Recount=true; return;}

         //---- Getting buy signals
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

      if(Mode==twist)
        {
         double Value[3];
         //---- copy newly appeared data into the arrays
         if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}

         //---- Getting buy signals
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
