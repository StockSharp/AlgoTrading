//+------------------------------------------------------------------+
//|                                                    Exp_RMACD.mq5 |
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
enum AlgMode
  {
   breakdown,  //breakthrough of zero
   MACDtwist,  //changing the MACD direction
   SIGNALtwist,//changing the direction of signal line
   MACDdisposition //breakthrough of signal line by the MACD histogram
  };
//+----------------------------------------------+
//|  declaration of enumerations                 |
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
input double  MM=-0.1;             //Share of a deposit in a deal, negative values - lot size
input int     StopLoss_=1000;      //Stop Loss in points
input int     TakeProfit_=2000;    //Take Profit in points
input int     Deviation_=10;       //max. price deviation in points
input bool    BuyPosOpen=true;     //Permission to buy
input bool    SellPosOpen=true;    //Permission to sell
input bool    BuyPosClose=true;    //Permission to exit long positions
input bool    SellPosClose=true;   //Permission to exit short positionsâ
input AlgMode Mode=MACDdisposition;//algorithm to enter in the market
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input uint Fast_RVI=12; //fast moving average period
input uint Slow_TRVI=26; //slow moving average period
input ENUM_APPLIED_VOLUME VolumeType=VOLUME_TICK;  //volume
input Smooth_Method Signal_Method=MODE_SMA; //signal line smoothing method
input uint Signal_XMA=9; //signal line period 
input int Signal_Phase=100; // signal line parameter,
                            //that changes within the range -100 ... +100,
//depends of the quality of the transitional prices;
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input uint SignalBar=1;//bar index for getting an entry signal
//+----------------------------------------------+
//---- Declaration of integer variables for storing a chart period in seconds 
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
//---- getting the ColorRMACD indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorRMACD",Fast_RVI,Slow_TRVI,VolumeType,Signal_Method,Signal_XMA,Signal_Phase,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of ColorRMACD indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;
//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(Fast_RVI,Slow_TRVI+8));
   min_rates_total+=XMA.GetStartBars(Signal_Method,Signal_XMA,Signal_Phase);
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
            if(CopyBuffer(InpInd_Handle,2,SignalBar,3,Value)<=0) {Recount=true; return;}

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
            if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Signal)<=0) {Recount=true; return;}

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
