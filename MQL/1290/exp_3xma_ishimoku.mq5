//+------------------------------------------------------------------+
//|                                            Exp_3XMA_Ishimoku.mq5 |
//|                               Copyright © 2012, Nikolay Kositsin | 
//|                                Khabarovsk, farria@mail.redcom.ru | 
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
enum MODE_PRICE //Type of constant
  {
   OPEN = 0,     //By open prices
   LOW,          //By lows
   HIGH,         //By highs
   CLOSE         //By close prices
  };
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
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
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame

input uint Up_period1=3; //high price calculation period 1
input uint Dn_period1=3; //low price calculation period 1
//----
input uint Up_period2=6; //high price calculation period 2
input uint Dn_period2=6; //low price calculation period 2
//----
input uint Up_period3=9; //high price calculation period 3
input uint Dn_period3=9; //low price calculation period 3
//---- 
input MODE_PRICE Up_mode1=HIGH;  //highs searching timeseries 1 
input MODE_PRICE Dn_mode1=LOW;   //lows searching timeseries 1 
//---- 
input MODE_PRICE Up_mode2=HIGH;  //highs searching timeseries 2 
input MODE_PRICE Dn_mode2=LOW;   //lows searching timeseries 2 
//---- 
input MODE_PRICE Up_mode3=HIGH;  //highs searching timeseries 3 
input MODE_PRICE Dn_mode3=LOW;   //lows searching timeseries 3 
//---- 
input Smooth_Method XMA1_Method=MODE_SMA; //smoothing method 1
input Smooth_Method XMA2_Method=MODE_SMA; //smoothing method 2
input Smooth_Method XMA3_Method=MODE_SMA; //smoothing method 3
//----
input int XLength1=8; //smoothing depth 1 
input int XLength2=25; //smoothing depth 2
input int XLength3=80; //smoothing depth 3
//----                  
input int XPhase=15; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
//---- 
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
//---- getting the 3XMA_Ishimoku indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"3XMA_Ishimoku",Up_period1,Dn_period1,
                         Up_period2,Dn_period2,Up_period3,Dn_period3,Up_mode1,Dn_mode1,Up_mode2,Dn_mode2,
                         Up_mode3,Dn_mode3,XMA1_Method,XMA2_Method,XMA3_Method,XLength1,XLength2,XLength3,XPhase,0,0,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of 3XMA_Ishimoku indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- initialization of variables of the start of data calculation
   CXMA XMA;
//---- Initialization of variables of the start of data calculation
   int min_rates_1=int(MathMax(Up_period1,Dn_period1));
   min_rates_1+=XMA.GetStartBars(XMA1_Method,XLength1,XPhase);
   int min_rates_2=int(MathMax(Up_period2,Dn_period2));
   min_rates_1+=XMA.GetStartBars(XMA2_Method,XLength2,XPhase);
   int min_rates_3=int(MathMax(Up_period3,Dn_period3));
   min_rates_3+=XMA.GetStartBars(XMA3_Method,XLength3,XPhase);
   min_rates_total=MathMax(min_rates_1,MathMax(min_rates_2,min_rates_3));
   min_rates_total+=int(2+SignalBar);
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
   double DnValue[2],UpValue[2],XmaValue[2];
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
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,XmaValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,2,SignalBar,2,DnValue)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(XmaValue[1]>MathMax(UpValue[1],DnValue[1]))
        {
         if(BuyPosOpen) if(XmaValue[0]<=MathMax(UpValue[0],DnValue[0])) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(XmaValue[1]<MathMin(UpValue[1],DnValue[1]))
        {
         if(SellPosOpen) if(XmaValue[0]>=MathMin(UpValue[0],DnValue[0])) SELL_Open=true;
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
