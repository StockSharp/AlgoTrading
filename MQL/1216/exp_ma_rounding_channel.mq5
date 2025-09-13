//+------------------------------------------------------------------+
//|                                      Exp_MA_Rounding_Channel.mq5 |
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
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Applied_price_ //Type od constant
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_   //TrendFollow_2 Price 
  };
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
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input Smooth_Method XMA_Method=MODE_SMA; //smoothing method
input int XLength=12; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input uint MaRound=500; //rounding ratio 
input int    ATRPeriod=12; //ATR period for the channel width
input double ATR_Factor=1.0; //channel deviation ratio
input bool  ChanContinuity=false; //channel continuity
input uint SignalBar=1; // bar index for getting an entry signal
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
//---- getting the MA_Rounding_Channel indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"MA_Rounding_Channel",
                         XMA_Method,XLength,XPhase,IPC,MaRound,ATRPeriod,ATR_Factor,ChanContinuity,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of MA_Rounding_Channel indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of the start of data calculation
   CXMA XMA;
   min_rates_total=XMA.GetStartBars(XMA_Method,XLength,XPhase)+3;
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

//---- Declaration of static variables
   int LastTrend;
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
      double DnValue[2],UpValue[2],Close[2];
      ArrayInitialize(UpValue,0);
      ArrayInitialize(DnValue,0);


      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      LastTrend=0;
      Recount=false;

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,2,SignalBar,2,DnValue)<=0) {Recount=true; return;}
      if(CopyClose(Symbol(),InpInd_Timeframe,SignalBar,2,Close)<=0) {Recount=true; return;}

      //---- there is a channel on the closed bar!
      if(UpValue[1] && DnValue[1])
        {
         //---- Getting buy signals
         if(Close[1]>UpValue[1])
           {
            if(BuyPosOpen && !UpValue[0] || Close[0]<=UpValue[0]) BUY_Open=true;
            if(SellPosClose) SELL_Close=true;
            UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }

         //---- Getting sell signals
         if(Close[1]<DnValue[1])
           {
            if(SellPosOpen && !DnValue[0] || Close[0]>=DnValue[0]) SELL_Open=true;
            if(BuyPosClose) BUY_Close=true;
            DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
        }

      //---- There is not a channel on the closed bar! Find the latest trading signal
      if(!UpValue[1] || !DnValue[1])
        {
         if(BuyPosClose || SellPosClose)
           {
            double DnValue_[1],UpValue_[1];
            ArrayInitialize(UpValue_,0);
            ArrayInitialize(DnValue_,0);

            int Bars_=Bars(Symbol(),InpInd_Timeframe);
            int end=0;

            for(int bar=int(SignalBar+1); bar<Bars_; bar++)
              {
               if(CopyBuffer(InpInd_Handle,1,bar,1,UpValue_)<=0) {Recount=true; return;}
               if(UpValue_[0]) {end=bar; break;}
              }

            if(SellPosClose && end) if(Close[1]>UpValue_[0]) SELL_Close=true;

            if(BuyPosClose && end)
              {
               if(CopyBuffer(InpInd_Handle,2,end,1,DnValue_)<=0) {Recount=true; return;}
               if(Close[1]<DnValue_[0]) BUY_Close=true;
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
