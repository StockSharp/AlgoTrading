//+------------------------------------------------------------------+
//|                                               Exp_GG-RSI-CCI.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------+
//|  Averaging classes description    |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
enum Signal_Mode
  {
   MODE_TREND,//exit by a trend signals
   MODE_FLAT  //exit by any signals
  };
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
/*enum Smooth_Method - enumeration is declared in SmoothAlgorithms.mqh
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
  }; */
//+----------------------------------------------+
//| Expert Advisor indicator input parameters    |
//+----------------------------------------------+
input double MM=-0.1;             //Share of a deposit in a deal, negative values - lot size
input int    StopLoss_=1000;      //Stop Loss in points
input int    TakeProfit_=2000;    // Take Profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     // Permission to buy
input bool   SellPosOpen=true;    // Permission to sell
input bool   BuyPosClose=true;     // Permission to exit long positions
input bool   SellPosClose=true;    // Permission to exit short positions
input Signal_Mode SignalMode=MODE_FLAT; // way to close position
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //timeframe of the GG-RSI-CCI indicator
input int IndPeriod=8; //RSI and CCI indicators period
input ENUM_APPLIED_PRICE Applied_Price=PRICE_CLOSE; //type of price
input Smooth_Method SmoothMethod=MODE_SMMA; //smoothing method
input int Period1=14; //rough smoothing of indicators
input int Period2=20; //specifying smoothing of indicators
input int Smooth_Phase=100;   //smoothing parameter,
                              // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input uint SignalBar=1; //bar index for getting an entry signal
//+----------------------------------------------+

int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
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
//---- getting the GG-RSI-CCI indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"GG-RSI-CCI",IndPeriod,Applied_Price,SmoothMethod,Period1,Period2,Smooth_Phase);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of GG-RSI-CCI indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- initialization of variables of the start of data calculation
   CXMA XMA;
   int min_rates_1=XMA.GetStartBars(SmoothMethod,Period1,Smooth_Phase);
   int min_rates_2=XMA.GetStartBars(SmoothMethod,Period2,Smooth_Phase);
   min_rates_total=IndPeriod;
   min_rates_total+=MathMax(min_rates_1,min_rates_2);
   min_rates_total+=int(3+SignalBar);
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

//---- Declaration of local variables
   double Value[2];
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
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Value)<=0) {Recount=true; return;}

      switch(int(Value[1]))
        {
         //---- Getting buy signals
         case 2:
           {
            if(BuyPosOpen) if(Value[0]!=2) BUY_Open=true;
            if(SellPosClose) SELL_Close=true;
            UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
            break;
           }

         //---- Getting sell signals
         case 0:
           {
            if(SellPosOpen) if(Value[0]!=0) SELL_Open=true;
            if(BuyPosClose) BUY_Close=true;
            DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
            break;
           }

         case 1:
            if(SignalMode==MODE_FLAT)
              {
               if(BuyPosClose) BUY_Close=true;
               if(SellPosClose) SELL_Close=true;
              }
            break;
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
