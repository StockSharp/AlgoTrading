//+------------------------------------------------------------------+
//|                                     Exp_ADX_Cross_Hull_Style.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//|  Averaging classes description               |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA;
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
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
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Applied_price_      // Type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPLE,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   // TrendFollow_2 Price 
  };
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
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicators time frame
//----
input uint ADXPeriod=14;//ADX indicator period
//----
input Smooth_Method W_Method=MODE_JJMA; //smoothing method
input int StartLength=3; //initial smoothing period                    
input int WPhase=100; //smoothing parameter,
                      // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
//----  
input uint nStep=5; //period change step
input uint nStepsTotal=10; //number of period changes
//----
input Smooth_Method SmoothMethod=MODE_JJMA; //smoothing method
input int SmoothLength=3; //smoothing depth                    
input int SmoothPhase=100; //smoothing parameter,
                           // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
input Applied_price_ IPC=PRICE_CLOSE;//price constant
//----
input uint SignalBar=1;    //bar index for getting an entry signal
//+----------------------------------------------+

int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle1,InpInd_Handle2;
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
//---- getting the ADX_Cross_Hull_Style indicator handle
   InpInd_Handle1=iCustom(Symbol(),InpInd_Timeframe,"ADX_Cross_Hull_Style",ADXPeriod,0);
   if(InpInd_Handle1==INVALID_HANDLE) Print(" Failed to get handle of ADX_Cross_Hull_Style indicator");

//---- getting the UltraXMA indicator handle
   InpInd_Handle2=iCustom(Symbol(),InpInd_Timeframe,"UltraXMA",W_Method,StartLength,WPhase,nStep,nStepsTotal,
                          SmoothMethod,SmoothLength,SmoothPhase,IPC,50,50,clrNONE,clrNONE,0,0);
   if(InpInd_Handle2==INVALID_HANDLE) Print(" Failed to get handle of UltraXMA indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- initialization of variables of the start of data calculation
   int AtrPeriod=10;
   int min_rates_1=int(MathMax(ADXPeriod+1,AtrPeriod));
   int min_rates_2=XMA.GetStartBars(W_Method,StartLength+nStep*nStepsTotal,WPhase)+1;
   min_rates_2+=XMA.GetStartBars(SmoothMethod,SmoothLength,SmoothPhase);
   min_rates_total=int(MathMax(min_rates_1,min_rates_2));
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
   if(BarsCalculated(InpInd_Handle1)<min_rates_total || BarsCalculated(InpInd_Handle2)<min_rates_total) return;

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
      //---- Declaration of local variables
      double DnValue[1],UpValue[1],UpUXMA[1],DnUXMA[1];

      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle1,0,SignalBar,1,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle1,1,SignalBar,1,DnValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle2,0,SignalBar,1,UpUXMA)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle2,1,SignalBar,1,DnUXMA)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(UpValue[0] && UpValue[0]!=EMPTY_VALUE)
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(DnValue[0] && DnValue[0]!=EMPTY_VALUE)
        {
         if(SellPosOpen) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
        
       if(UpUXMA[0]<DnUXMA[0]) {BUY_Open=false; BUY_Close=true;}
       if(UpUXMA[0]>DnUXMA[0]) {SELL_Open=false; SELL_Close=true;}

      //---- searching for the last trading direction for getting positions closing signals
      //if(!MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)) //if execution is set to "Random delay" in the Strategy Tester 
      if((BuyPosOpen && BuyPosClose || SellPosOpen && SellPosClose) && (!BUY_Close && !SELL_Close))
        {
         int Bars_=Bars(Symbol(),InpInd_Timeframe);

         for(int bar=int(SignalBar+1); bar<Bars_; bar++)
           {
            if(SellPosClose)
              {
               if(CopyBuffer(InpInd_Handle1,0,bar,1,UpValue)<=0) {Recount=true; return;}
               if(UpValue[0]!=0 && UpValue[0]!=EMPTY_VALUE)
                 {
                  SELL_Close=true;
                  break;
                 }
              }

            if(BuyPosClose)
              {
               if(CopyBuffer(InpInd_Handle1,1,bar,1,DnValue)<=0) {Recount=true; return;}
               if(DnValue[0]!=0 && DnValue[0]!=EMPTY_VALUE)
                 {
                  BUY_Close=true;
                  break;
                 }
              }
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
