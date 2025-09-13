//+------------------------------------------------------------------+
//|                                              Exp_EF_distance.mq5 |
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
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price 
  };
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Volatility //Type of constant
  {
   V1,     //1
   V2,     //2
   V3      //3
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
//----
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicators time frame
//----
input uint SignalBar=1;//bar index for getting an entry signal
//+----------------------------------------------+
//| EF_distance indicator input parameters       |
//+----------------------------------------------+
input int XLength=10;                       //smoothing depth                    
input uint Power=2.0;                       //averaging power
input Applied_price_ IPC=PRICE_CLOSE;       //price constant
//+----------------------------------------------+
//| Flat-Trend indicator input parameters        |
//+----------------------------------------------+
input uint StDevPeriod=20;                  //StDev period 
input Smooth_Method StDev_Method=MODE_LWMA; //StDev smoothing method
input uint StDevLength=5;                   //StDev smoothing depth                    
input int StDevPhase=15;                    //StDev smoothing parameter
input uint ATRPeriod=20;                    //StDev period   
input Smooth_Method ATR_Method=MODE_LWMA;   //ATR smoothing method
input uint ATRLength=5;                     //ATR smoothing depth
input int ATRPhase=15;                      //ATR smoothing parameter
input Volatility Volatil=V3;                //Volatility size to perform a deal
//+----------------------------------------------+
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
//---- getting the EF_distance indicator handle
   InpInd_Handle1=iCustom(Symbol(),InpInd_Timeframe,"EF_distance",XLength,Power,IPC,0,0);
   if(InpInd_Handle1==INVALID_HANDLE) Print(" Failed to get handle of EF_distance indicator");

//---- getting the Flat-Trend indicator handle
   InpInd_Handle2=iCustom(Symbol(),InpInd_Timeframe,"Flat-Trend",
                          StDevPeriod,StDev_Method,StDevLength,StDevPhase,ATRPeriod,ATR_Method,ATRLength,ATRPhase,0);
   if(InpInd_Handle2==INVALID_HANDLE) Print(" Failed to get handle of Flat-Trend indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of the start of data calculation
   min_rates_total=int(2*XLength);
   min_rates_total+=int(3+SignalBar);

   CXMA XMA;
   int min_rate_1=int(StDevPeriod)+XMA.GetStartBars(StDev_Method,StDevLength,StDevPhase);
   int min_rate_2=int(ATRPeriod)+XMA.GetStartBars(ATR_Method,ATRLength,ATRPhase);
   int min_rates_=MathMax(min_rate_1,min_rate_2)+1;

   min_rates_total=MathMax(min_rates_,min_rates_total);
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

//---- Declaration of local variables
   double Value[3],Vol[1];
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
      if(CopyBuffer(InpInd_Handle1,0,SignalBar,3,Value)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle2,0,SignalBar,1,Vol)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen && Value[0]>Value[1] && Vol[0]>=Volatil) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(Value[1]>Value[2])
        {
         if(SellPosOpen && Value[0]<Value[1] && Vol[0]>=Volatil) SELL_Open=true;
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
