//+------------------------------------------------------------------+
//|                                        Exp_VininI_Trend_LRMA.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//|  CXMA class description                        |
//+------------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------------+
//|  Declaration of enumerations                   |
//+------------------------------------------------+
enum AlgMode
  {
   BREAKDOWN,  //breakthrough of UpLevel and DnLevel levels by indicator
   TWIST       //changing of the indicator movement direction
  };
//+------------------------------------------------+
//|  Declaration of enumerations                   |
//+------------------------------------------------+
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
//+------------------------------------------------+
//|  Declaration of enumerations                   |
//+------------------------------------------------+
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
//+------------------------------------------------+
//| Expert Advisor indicator input parameters      |
//+------------------------------------------------+
input double MM=-0.1;             //Share of a deposit in a deal, negative values - lot size
input int    StopLoss_=1000;      //Stop Loss in points
input int    TakeProfit_=2000;    // Take Profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     // Permission to buy
input bool   SellPosOpen=true;    // Permission to sell
input bool   BuyPosClose=true;    //Permission to exit long positions
input bool   SellPosClose=true;   //Permission to exit short positions
input AlgMode Mode=BREAKDOWN;     //algorithm to enter in the market
input uint SignalBar=1;           //bar index for getting an entry signal
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicators time frame
//+------------------------------------------------+
//| VininI_Trend_LRMA indicator input parameters   |
//+------------------------------------------------+
input int LRMAPeriod=13; //LRMA period
input Smooth_Method MA_Method1=MODE_SMA; //smoothing method of moving averages
input uint Length1=3; //start smoothing depth of moving averages                  
input int Phase1=15; //moving averages smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input uint MA_Step=10;  //step of depth changing of the moving averages smoothing
input uint MA_Count=10;  //number of smoothing steps

input Smooth_Method MA_Method2=MODE_JJMA; //indicator smoothing method
input uint Length2=20; //indicator smoothing depth
input int Phase2=100;  //indicator smoothing parameter,
                       // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period

input Applied_price_ IPC=PRICE_CLOSE;//price constant
input int UpLevel=+10; //Up level (range 0/+100)
input int DnLevel=-10; //Down level (range -100/0)
//+------------------------------------------------+
//| ChangeOfVolatility indicator input parameters  |
//+------------------------------------------------+
input uint MPeriod = 1;      // Momentum period
input uint Short=6;
input uint Long=100;
input uint MaxTrendLevel=60; // Trend level which enough to perform a deal (range 0/100)
//+------------------------------------------------+
//---- Declaration of integer variables for storing a chart period in seconds 
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
//---- getting the VininI_Trend_LRMA indicator handle
   InpInd_Handle1=iCustom(Symbol(),InpInd_Timeframe,"VininI_Trend_LRMA",LRMAPeriod,
                         MA_Method1,Length1,Phase1,MA_Step,MA_Count,MA_Method2,Length2,Phase2,IPC,UpLevel,DnLevel,0);
   if(InpInd_Handle1==INVALID_HANDLE) Print(" Failed to get handle of VininI_Trend_LRMA indicator");
   
//---- getting the ChangeOfVolatility indicator handle
   InpInd_Handle2=iCustom(Symbol(),InpInd_Timeframe,"ChangeOfVolatility",MPeriod,Short,Long,UpLevel,MaxTrendLevel,0,0);
   if(InpInd_Handle2==INVALID_HANDLE) Print(" Failed to get handle of ChangeOfVolatility indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;

//---- Initialization of variables of the start of data calculation
   int min_rates_1=int(LRMAPeriod); 
   min_rates_1+=XMA.GetStartBars(MA_Method1,Length1+MA_Step*(MA_Count-1),Phase1);
   min_rates_1+=XMA.GetStartBars(MA_Method2,Length2,Phase2);
//----  
   int min_rates_2=int(MPeriod+MathMax(Short,Long));
//----  
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
   if(BarsCalculated(InpInd_Handle1)<min_rates_total) return;
   if(BarsCalculated(InpInd_Handle2)<min_rates_total) return;

//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
   
   double Trend[1];

//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+------------------------------------------------+
//| Detecting market entry signals                 |
//+------------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      
      //--- copy newly appeared data in the array
            if(CopyBuffer(InpInd_Handle2,0,SignalBar,1,Trend)<=0) {Recount=true; return;}

      switch(Mode)
        {
         case BREAKDOWN:
           {
            double Value[2];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle1,0,SignalBar,2,Value)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(Value[1]>UpLevel)
              {
               if(BuyPosOpen && Value[0]<=UpLevel&&Trend[0]>MaxTrendLevel) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(Value[1]<DnLevel)
              {
               if(SellPosOpen && Value[0]>=DnLevel&&Trend[0]>MaxTrendLevel) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case TWIST:
           {
            double Value[3];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle1,0,SignalBar,3,Value)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(Value[1]<Value[2])
              {
               if(BuyPosOpen && Value[0]>Value[1]&&Trend[0]>MaxTrendLevel) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(Value[1]>Value[2])
              {
               if(SellPosOpen && Value[0]<Value[1]&&Trend[0]>MaxTrendLevel) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;
        }
     }

//+------------------------------------------------+
//| Performing deals                               |
//+------------------------------------------------+
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
