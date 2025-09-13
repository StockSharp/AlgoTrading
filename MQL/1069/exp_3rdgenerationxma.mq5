//+------------------------------------------------------------------+
//|                                         Exp_3rdGenerationXMA.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//|  CXMA class description                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  declaration of enumerations                     |
//+----------------------------------------------+
enum Applied_price_ //Type of constant
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
//|  declaration of enumerations                     |
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
//| Expert Advisor indicator input parameters        |
//+----------------------------------------------+
input double MM=-0.1;             //Share of a deposit in a transaction, negative values - lot size
input int    StopLoss_=1000;      //Stop Loss in points
input int    TakeProfit_=2000;    //Take Profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     //Permission to enter long
input bool   SellPosOpen=true;    //Permission to enter short
input bool   BuyPosClose=true;     //Permission to exit long
input bool   SellPosClose=true;    //Permission to exit short
//+----------------------------------------------+
//| X2MA indicator input parameters            |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input Smooth_Method XMA_Method=MODE_EMA; //smoothing method
input int XLength=50; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     //for JJMA, it varies within the range -100 ... +100 and influences the quality of the transient process;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_TYPICAL;//price constant
/* , used for the indicator calculation ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in points
input uint SignalBar=1;//bar index for getting an entry signal
//+----------------------------------------------+
//---- Declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle;
//---- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//  Trade algorithms                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting the 3rdGenerationXMA indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"3rdGenXMA",XMA_Method,XLength,XPhase,IPC,0,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get the 3rdGenerationXMA indicator handle");

//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from SmoothAlgorithms.mqh
   CXMA XMA;

//---- Initialization of variables of data starting point
   //---- Initialization of variables of data starting point
   int min_rates_1=XMA.GetStartBars(XMA_Method,2*XLength,XPhase);
   int min_rates_2=min_rates_1+XMA.GetStartBars(XMA_Method,XLength,XPhase);
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
//---- checking for the sufficiency of the number of bars for the calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;

//---- uploading history for proper operation of the functions IsNewBar() and SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Declaration of local variables
   double Value[3];
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

      //---- copy new data into the arrays
      if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen&&Value[0]>Value[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(Value[1]>Value[2])
        {
         if(SellPosOpen&&Value[0]<Value[1]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//+----------------------------------------------+
//| Executing transactions                            |
//+----------------------------------------------+
//---- Closing long
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Closing short   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- Opening long
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);

//---- Opening short
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
