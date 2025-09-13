//+------------------------------------------------------------------+
//|                                                    Exp_XMACD.mq5 |
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
   MACDtwist,  //changing the direction of MACD
   SIGNALtwist,//changing the direction of signal line
   MACDdisposition //MACD histogram breakthrough through the signal line
  };
//+----------------------------------------------+
//|  Declaration of enumerations                 |
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
input bool    BuyPosClose=true;    //Permission to exit long positions
input bool    SellPosClose=true;   //Permission to exit short positions
input AlgMode Mode=MACDdisposition;//algorithm to enter in the market
//+----------------------------------------------+
//|  Indicator input parameters                  |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input Smooth_Method XMA_Method=MODE_EMA; //histogram smoothing method
input int Fast_XMA = 12; ///Fast moving average period
input int Slow_XMA = 26; //Slow moving average period
input int XPhase= 100;  //moving averages smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
input Smooth_Method Signal_Method=MODE_SMA; //signal line smoothing method
input int Signal_XMA=9; //signal line period 
input int Signal_Phase=100; // signal line parameter
                            //that changes within the range -100 ... +100,
//impacts the transitional process quality;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//price constant
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
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"XMACD",
                         XMA_Method,Fast_XMA,Slow_XMA,XPhase,Signal_Method,Signal_XMA,Signal_Phase,AppliedPrice);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of BinaryWave indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;
//---- Initialization of variables of the start of data calculation
   min_rates_total=MathMax(XMA.GetStartBars(XMA_Method,Fast_XMA,XPhase),XMA.GetStartBars(XMA_Method,Slow_XMA,XPhase));
   min_rates_total+=XMA.GetStartBars(Signal_Method,Signal_XMA,Signal_Phase);
   min_rates_total=int(min_rates_total+3+SignalBar);
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

      switch(Mode)
        {
         case breakdown:
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
         break;

         case MACDtwist:
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
         break;

         case SIGNALtwist:
           {
            double Value[3];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,1,SignalBar,3,Value)<=0) {Recount=true; return;}

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
         break;
         
         case MACDdisposition:
           {
            double MACD[2],Signal[2];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,MACD)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Signal)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(MACD[1]>Signal[1])
              {
               if(BuyPosOpen && MACD[0]<=Signal[0]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(MACD[1]<Signal[1])
              {
               if(SellPosOpen && MACD[0]>=Signal[0]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
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
