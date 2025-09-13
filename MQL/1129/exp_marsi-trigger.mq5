//+------------------------------------------------------------------+
//|                                            Exp_MaRsi-Trigger.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Expert Advisor indicator input parameters    |
//+----------------------------------------------+
input double MM=-0.1;             // Share of a deposit in a deal, negative values - lot size
input int    StopLoss_=1000;      //stop loss in points
input int    TakeProfit_=2000;    //take profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     // Permission to buy
input bool   SellPosOpen=true;    // Permission to sell
input bool   BuyPosClose=true;     // Permission to exit long positions
input bool   SellPosClose=true;    // Permission to exit short positions
//+----------------------------------------------+
//| MaRsi-Trigger indicator input parameters     |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input uint nPeriodRsi=3;
input ENUM_APPLIED_PRICE nRSIPrice=PRICE_WEIGHTED;
input uint nPeriodRsiLong=13;
input ENUM_APPLIED_PRICE nRSIPriceLong=PRICE_MEDIAN;
input uint nPeriodMa=5;
input  ENUM_MA_METHOD nMAType=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPrice=PRICE_CLOSE;
input uint nPeriodMaLong=10;
input  ENUM_MA_METHOD nMATypeLong=MODE_EMA;
input ENUM_APPLIED_PRICE nMAPriceLong=PRICE_CLOSE;
input uint SignalBar=1; //bar index for getting an entry signal
//+----------------------------------------------+

int TimeShiftSec;
//---- declaration of integer variables for the indicators handles
int MA_Handle,RSI_Handle,MAl_Handle,RSIl_Handle,MaRsi_Handle;
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
//---- getting handle of the iRSI indicator
   RSI_Handle=iRSI(NULL,InpInd_Timeframe,nPeriodRsi,nRSIPrice);
   if(RSI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iRSI indicator");

//---- getting handle of the iRSIl indicator
   RSIl_Handle=iRSI(NULL,InpInd_Timeframe,nPeriodRsiLong,nRSIPriceLong);
   if(RSIl_Handle==INVALID_HANDLE) Print(" Failed to get handle of iRSIl indicator");

//---- getting handle of the iMA indicator
   MA_Handle=iMA(NULL,InpInd_Timeframe,nPeriodMa,0,nMAType,nMAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMA indicator");

//---- getting handle of the iMAl indicator
   MAl_Handle=iMA(NULL,InpInd_Timeframe,nPeriodMaLong,0,nMATypeLong,nMAPriceLong);
   if(MAl_Handle==INVALID_HANDLE) Print(" Failed to get handle of iMAl indicator");

//---- getting handle of the ColorMaRsi-Trigger indicator (indicator is displayed only with tests and is not involved in the trade!)
   if(MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION))
     {
      MaRsi_Handle=iCustom(NULL,InpInd_Timeframe,"ColorMaRsi-Trigger",nPeriodRsi,nRSIPrice,nPeriodRsiLong,nRSIPriceLong,
                           nPeriodMa,nMAType,nMAPrice,nPeriodMaLong,nMATypeLong,nMAPriceLong,0);

      if(MaRsi_Handle==INVALID_HANDLE) Print(" Failed to get handle of ColorMaRsi-Trigger indicator");
     }

//---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(MathMax(MathMax(nPeriodRsi,nPeriodRsiLong),nPeriodMa),nPeriodMaLong));

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

///---- Initialization of variables of the start of data calculation
   min_rates_total=int(MathMax(MathMax(MathMax(nPeriodRsi,nPeriodRsiLong),nPeriodMa),nPeriodMaLong));
   min_rates_total+=int(2+SignalBar);
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---- remove the result of unuseful technical indicators test from the chart
   if(!MQL5InfoInteger(MQL5_VISUAL_MODE) && !MQL5InfoInteger(MQL5_OPTIMIZATION) && MQL5InfoInteger(MQL5_TESTING))
     {
      bool hidden;
      ResetLastError();
      hidden=IndicatorRelease(RSI_Handle);
      if(!hidden) {Print("IndicatorRelease(RSI_Handle) return false. Error code ",GetLastError()); ResetLastError();}
      hidden=IndicatorRelease(RSIl_Handle);
      if(!hidden) {Print("IndicatorRelease(RSIl_Handle) return false. Error code ",GetLastError()); ResetLastError();}
      hidden=IndicatorRelease(MA_Handle);
      if(!hidden) {Print("IndicatorRelease(MA_Handle) return false. Error code ",GetLastError()); ResetLastError();}
      hidden=IndicatorRelease(MAl_Handle);
      if(!hidden) {Print("IndicatorRelease(MAl_Handle) return false. Error code ",GetLastError()); ResetLastError();}
     }
//----
   GlobalVariableDel_(Symbol());
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(MA_Handle)<min_rates_total
      || BarsCalculated(MAl_Handle)<min_rates_total
      || BarsCalculated(RSI_Handle)<min_rates_total
      || BarsCalculated(RSIl_Handle)<min_rates_total) return;

//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- declaration of local variables
   double MA_[1],MAl_[1],RSI_[1],RSIl_[1];

//---- Declaration of static variables
   int res,Trend,LastTrend;
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
      LastTrend=0;
      Recount=false;

      //---- Search of the last direction of trade
      int Bars_=Bars(Symbol(),InpInd_Timeframe);
      if(Bars_<min_rates_total) {Recount=true; return;}
      Bars_-=min_rates_total;

      //---- cycle of searching of the current and last directions of trade
      for(int bar=int(SignalBar); bar<Bars_ && !IsStopped(); bar++)
        {
         //---- copy newly appeared data into the arrays
         if(CopyBuffer(MA_Handle,0,bar,1,MA_)<=0) {Recount=true; return;}
         if(CopyBuffer(MAl_Handle,0,bar,1,MAl_)<=0) {Recount=true; return;}
         if(CopyBuffer(RSI_Handle,0,bar,1,RSI_)<=0) {Recount=true; return;}
         if(CopyBuffer(RSIl_Handle,0,bar,1,RSIl_)<=0) {Recount=true; return;}

         res=0;
         if(MA_[0]>MAl_[0]) res=+1;
         if(MA_[0]<MAl_[0]) res=-1;

         if(RSI_[0]>RSIl_[0]) res+=1;
         if(RSI_[0]<RSIl_[0]) res-=1;

         if(bar==SignalBar) {Trend=res; continue;}

         if(res) break;
         if(bar==Bars_-1) {Recount=true; return;}
        }

      LastTrend=res;

      //---- Getting buy signals
      if(Trend>0)
        {
         if(BuyPosOpen && LastTrend<0) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(Trend<0)
        {
         if(SellPosOpen && LastTrend>0) SELL_Open=true;
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
