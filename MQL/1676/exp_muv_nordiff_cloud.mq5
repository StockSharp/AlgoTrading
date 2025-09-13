//+------------------------------------------------------------------+
//|                                        Exp_MUV_NorDIFF_Cloud.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//  Trading algorithms                             | 
//+------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------+
//|  Calculated lot variants enumeration           |
//+------------------------------------------------+
/*enum MarginMode  - enumeration is declared in TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM considering account free funds
   BALANCE,          //MM considering account balance
   LOSSFREEMARGIN,   //MM for losses share from an account free funds                     
   LOSSBALANCE,      //MM for losses share from an account balance                       
   LOT               //Lot should be unchanged
  }; */
//+------------------------------------------------+
//| Expert Advisor indicator input parameters      |
//+------------------------------------------------+
input double     MM=0.1;           // A share of financial resources used in a deal
input MarginMode MMMode=LOT;       //lot value detection method

input int    StopLoss_=1000;       // Stop Loss in points.
input int    TakeProfit_=2000;     // Take Profit in points.
input int    Deviation_=10;        // maximum price deviation in points
input bool   BuyPosOpen=true;      // Permission to buy
input bool   SellPosOpen=true;     // Permission to sell
input bool   BuyPosClose=true;     // Permission to exit a long position
input bool   SellPosClose=true;    // Permission to exit a short position
//+------------------------------------------------+
//| MUV_NorDIFF_Cloud indicator input parameters   |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //| MUV_NorDIFF_Cloud indicator timeframe |

input uint MAPeriod=14;                           // XMUV indicator period
input uint Momentum=1;                            // Momentum period
input uint KPeriod=14;                            // extremums searching period

input uint SignalBar=1;                           // bar index for getting entry signal
//+------------------------------------------------+

int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting the MUV_NorDIFF_Cloud indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"MUV_NorDIFF_Cloud",MAPeriod,Momentum,KPeriod,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of the MUV_NorDIFF_Cloud indicator");

//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- initialization of variables of the start of data calculation
   min_rates_total=int(MAPeriod+Momentum+KPeriod);
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
//---- checking for the sufficiency of the number of bars for the calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;

//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Declaration of local variables
   double DnValue[1],UpValue[1];
//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+----------------------------------------------+
//| Determining transaction signals              |
//+----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // check for new bar appearance
     {
      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle,4,SignalBar,1,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,5,SignalBar,1,DnValue)<=0) {Recount=true; return;}

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

      //---- Searching for the last trading direction for getting positions closing signals
      //if(!MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)) //if execution is set to "Random delay" in the Strategy Tester 
      if((BuyPosOpen && BuyPosClose || SellPosOpen && SellPosClose) && (!BUY_Close && !SELL_Close))
        {
         int Bars_=Bars(Symbol(),InpInd_Timeframe);

         for(int bar=int(SignalBar+1); bar<Bars_; bar++)
           {
            if(SellPosClose)
              {
               if(CopyBuffer(InpInd_Handle,4,bar,1,UpValue)<=0) {Recount=true; return;}
               if(UpValue[0]!=0 && UpValue[0]!=EMPTY_VALUE)
                 {
                  SELL_Close=true;
                  break;
                 }
              }

            if(BuyPosClose)
              {
               if(CopyBuffer(InpInd_Handle,5,bar,1,DnValue)<=0) {Recount=true; return;}
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
