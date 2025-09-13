//+------------------------------------------------------------------+
//|                                                   Exp_F2a_AO.mq5 |
//|                             Copyright © 2013,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------------------+
//  Trading algorithms                            | 
//+-----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//|  Calculated lots variants enumeration        |
//+----------------------------------------------+
/*enum MarginMode  - enumeration is declared in TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM considering account free funds
   BALANCE,          //MM considering account balance
   LOSSFREEMARGIN,   //MM for losses share from an account free funds
   LOSSBALANCE,      //MM for losses share from an account balance
   LOT               //Lot should be unchanged
  }; */
//+----------------------------------------------+
//| Expert Advisor indicator input parameters    |
//+----------------------------------------------+
input double        MM=0.1;           //Share of a deposit in a deal
input  MarginMode MMMode=LOT;        //lot value detection method
input int    StopLoss_=1000;        //Stop Loss in points
input int    TakeProfit_=2000;      //Take Profit in points
input int    Deviation_=10;         //max. price deviation in points
input bool   BuyPosOpen=true;       // Permission to enter long position
input bool   SellPosOpen=true;      //Permission to enter short position
input bool   BuyPosClose=true;     //Permission to exit long positions
input bool   SellPosClose=true;    //Permission to exit short positions
//+----------------------------------------------+
//| Indicator input parameters for the trend     |
//| candlestick                                  |
//+----------------------------------------------+
input ENUM_TIMEFRAMES Inp_Timeframe=PERIOD_D1;     //the trend candlestick timeframe
input uint TrendBar=1;                             //the trend bar index
//+----------------------------------------------+
//| The F2a_AO indicator input parematers        |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H12; //the F2a_AO indicator timeframe

input uint  MA_Filtr=3;
input uint  MA_Fast=13;
input uint  MA_Slow=144;

input uint SignalBar=1;                               //bar index for getting an entry signal
//+----------------------------------------------+

int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total,min_rates_total1;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- getting handle of the F2a_AO indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"F2a_AO",MA_Filtr,MA_Fast,MA_Slow);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of F2a_AO indicator");

//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- initialization of variables of the start of data calculation
   int nshift=3;
   min_rates_total=int(MathMax(MathMax(MA_Filtr,MA_Fast),MA_Slow)+nshift+9+SignalBar);
   min_rates_total1=int(TrendBar);
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
   LoadHistory(TimeCurrent()-PeriodSeconds(Inp_Timeframe)-1,Symbol(),Inp_Timeframe);
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Declaration of local variables
   double DnValue[1],UpValue[1],iOpen[1],iClose[1];
//---- Declaration of static variables
   static double Trend=0;
   static bool Recount=true,Recount1=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB,NB1;
   
//+----------------------------------------------+
//| Determining transaction signals              |
//+----------------------------------------------+
   if(!TrendBar || NB1.IsNewBar(Symbol(),Inp_Timeframe) || Recount) // checking for a new bar
     {
       Recount1=false;
       //---- copy newly appeared data into the arrays
       if(CopyOpen(Symbol(),Inp_Timeframe,TrendBar,1,iOpen)<=0) {Recount=true; return;}
       if(CopyClose(Symbol(),Inp_Timeframe,TrendBar,1,iClose)<=0) {Recount=true; return;}
       Trend=iClose[0]-iOpen[0];
     }
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

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle,1,SignalBar,1,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,0,SignalBar,1,DnValue)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(UpValue[0])
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(DnValue[0])
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
               if(CopyBuffer(InpInd_Handle,1,bar,1,UpValue)<=0) {Recount=true; return;}
               if(UpValue[0]!=0)
                 {
                  SELL_Close=true;
                  break;
                 }
              }

            if(BuyPosClose)
              {
               if(CopyBuffer(InpInd_Handle,0,bar,1,DnValue)<=0) {Recount=true; return;}
               if(DnValue[0]!=0)
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
   if(Trend>0) BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);

//---- Opening short
   if(Trend<0) SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,0,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
