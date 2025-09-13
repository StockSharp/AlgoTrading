//+------------------------------------------------------------------+
//|                                         Exp_MovingAverage_FN.mq5 |
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
enum filter_mode
  {
   N4=4,  //filter ¹4
   N5,    //filter ¹5
   N6,    //filter ¹6
   N7,    //filter ¹7
   N8,    //filter ¹8
   N9,    //filter ¹9
   N10,   //filter ¹10
   N11,   //filter ¹11
   N12,   //filter ¹12
   N13,   //filter ¹13
   N14,   //filter ¹14
   N15,   //filter ¹15
   N16,   //filter ¹16
   N17,   //filter ¹17
   N18,   //filter ¹18
   N19,   //filter ¹19
   N20,   //filter ¹20
   N21,   //filter ¹21
   N22,   //filter ¹22
   N23,   //filter ¹23
   N24,   //filter ¹24
   N25,   //filter ¹25
   N26,   //filter ¹26
   N27,   //filter ¹27
   N28,   //filter ¹28
   N29,   //filter ¹29
   N30,   //filter ¹30
   N31,   //filter ¹31
   N32,   //filter ¹32
   N33,   //filter ¹33
   N34,   //filter ¹34
   N35,   //filter ¹35
   N36,   //filter ¹36
   N37,   //filter ¹37
   N38,   //filter ¹38
   N39,   //filter ¹39
   N40,   //filter ¹40
   N41,   //filter ¹41
   N42,   //filter ¹42
   N43,   //filter ¹43
   N44,   //filter ¹44
   N45,   //filter ¹45
   N46,   //filter ¹46
   N47,   //filter ¹47
   N48,   //filter ¹48
   N49,   //filter ¹49
   N50,   //filter ¹50
   N51,   //filter ¹51
   N52,   //filter ¹52
   N53,   //filter ¹53
   N54,   //filter ¹54
   N55,   //filter ¹55
   N56,   //filter ¹56
   N57,   //filter ¹57
   N58,   //filter ¹58
   N59,   //filter ¹59
   N60    //filter ¹60
  };
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
//----
input filter_mode FilterNumber=N44; //fulter number for the calculation
input Smooth_Method XMA_Method=MODE_JJMA; //smoothing method
input int XLength=12; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
  // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
  // For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_CLOSE;//price constant
/* , used for calculation of the indicator ( 1-CLOSE, 2-OPEN, 3-HIGH, 4-LOW, 
  5-MEDIAN, 6-TYPICAL, 7-WEIGHTED, 8-SIMPL, 9-QUARTER, 10-TRENDFOLLOW, 11-0.5 * TRENDFOLLOW.) */
//----
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
//---- getting Moving Average_FN indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"MovingAverage_FN",FilterNumber,XMA_Method,XLength,XPhase,IPC,0,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of MovingAverage_FN indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;
//---- Initialization of variables of the start of data calculation
   min_rates_total=700+XMA.GetStartBars(XMA_Method, XLength, XPhase);
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
   double Value[3];
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
