//+------------------------------------------------------------------+
//|                                                Exp_RSIOMA_V2.mq5 |
//|                                Copyright © 2013,Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+---------------------------------------------------+
//| declaration of enumerations                       |
//+---------------------------------------------------+
enum AlgMode
  {
   breakdown,  // Cloud breaks through levels
   twist,      // Change of histogram direction
   cloudtwist  // Change of color of the signal cloud
  };
//+---------------------------------------------------+
//| Description of class CXMA                         |
//+---------------------------------------------------+
#include <SmoothAlgorithms.mqh>
//+---------------------------------------------------+
//| Trading algorithms                                |
//+---------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+---------------------------------------------------+
//| declaration of enumerations                       |
//+---------------------------------------------------+
enum Applied_price_      // type of constant
  {
   PRICE_CLOSE_ = 1,     // Close
   PRICE_OPEN_,          // Open
   PRICE_HIGH_,          // High
   PRICE_LOW_,           // Low
   PRICE_MEDIAN_,        // Median Price (HL/2)
   PRICE_TYPICAL_,       // Typical Price (HLC/3)
   PRICE_WEIGHTED_,      // Weighted Close (HLCC/4)
   PRICE_SIMPL_,         // Simple Price (OC/2)
   PRICE_QUARTER_,       // Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  // TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  // TrendFollow_2 Price 
   PRICE_DEMARK_         // Demark Price 
  };
//+---------------------------------------------------+
//| declaration of enumerations                       |
//+---------------------------------------------------+
//+---------------------------------------------------+
//| Expert Advisor input parameters                   |   
//+---------------------------------------------------+
input double  MM=-0.1;             // Share of a deposit in a deal, negative values - lot size
input MarginMode MMMode=LOT;       // Lot value calculation method
input int     StopLoss_=1000;      // Stop Loss in points
input int     TakeProfit_=2000;    // Take Profit in points
input int     Deviation_=10;       // Max. price deviation in points
input bool    BuyPosOpen=true;     // Permission to enter a long position
input bool    SellPosOpen=true;    // Permission to enter a short position
input bool    BuyPosClose=true;    // Permission to exit long positions
input bool    SellPosClose=true;   // Permission to exit short positions
input AlgMode Mode=breakdown;      // An algorithm for market entering
//+---------------------------------------------------+
//| Input parameters of the RSIOMA_V2 indicator       |
//+---------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Indicator timeframe
input Smooth_Method RSIOMA_Method=MODE_EMA;       // Method of averaging of RSIOMA
input uint RSIOMA=14;                             // Depth of averaging of RSIOMA                    
input int RSIOMAPhase=15;                         // Parameter of averaging of RSIOMA
//--- RSIOMAPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- RSIOMAPhase: For VIDIA it is a CMO period, for AMA it is a slow average period
input Smooth_Method MARSIOMA_Method=MODE_EMA;     // Method of averaging of MARSIOMA
input uint MARSIOMA=21;                           // Depth of averaging of RSIOMA                    
input int MARSIOMAPhase=15;                       // Parameter of averaging of RSIOMA,
//--- MARSIOMAPhase: for JJMA it varies within the range -100 ... +100 and influences the quality of the transient period;
//--- MARSIOMAPhase: For VIDIA it is a CMO period, for AMA it is a slow average period
input uint MomPeriod=1;                           // Period of Momentum
input Applied_price_ IPC=PRICE_CLOSE;             // Price constant
input uint MainTrendLong=60;                      // Upper trigger level
input uint MainTrendShort=40;                     // Lower trigger level
input uint SignalBar=1;                           // Bar number for getting an entry signal
//+---------------------------------------------------+
//--- declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//--- declaration of integer variables for the indicators handles
int InpInd_Handle;
//--- declaration of integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- getting the handle of the RSIOMA_V2 indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"RSIOMA_V2",RSIOMA_Method,RSIOMA,RSIOMAPhase,MARSIOMA_Method,
                         MARSIOMA,MARSIOMAPhase,MomPeriod,IPC,100,0,clrNONE,clrNONE,MainTrendLong,MainTrendShort,clrGreen,clrRed,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Failed to get the handle of the RSIOMA_V2 indicator");
      return(INIT_FAILED);
     }
//--- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- initialization of variables of data calculation start
   min_rates_total+=GetStartBars(RSIOMA_Method,RSIOMA,RSIOMAPhase)+int(MomPeriod)+int(RSIOMA+1);
   min_rates_total+=int(3+SignalBar);
//--- initialization end
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   GlobalVariableDel_(Symbol());
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- checking if the number of bars is enough for the calculation
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//--- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//--- declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//--- determining signals for deals
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //--- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      switch(Mode)
        {
         case breakdown: //Cloud breaks through levels
           {
            double Hist[2];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,4,SignalBar,2,Hist)<=0) {Recount=true; return;}
            //--- getting buy signals
            if(Hist[1]>MainTrendLong)
              {
               if(BuyPosOpen  &&  Hist[0]<=MainTrendLong) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //--- Getting sell signals
            if(Hist[1]<MainTrendShort)
              {
               if(SellPosOpen && Hist[0]>=MainTrendShort) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case twist://direction change
           {
            double Hist[3];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,4,SignalBar,3,Hist)<=0) {Recount=true; return;}
            //--- getting buy signals
            if(Hist[1]<Hist[2])
              {
               if(BuyPosOpen && Hist[0]>Hist[1]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //--- Getting sell signals
            if(Hist[1]>Hist[2])
              {
               if(SellPosOpen && Hist[0]<Hist[1]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case cloudtwist: //Change of color of the signal cloud
           {
            double Up[2],Dn[2];
            //--- copy newly appeared data in the arrays
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Up)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Dn)<=0) {Recount=true; return;}
            //--- getting buy signals
            if(Up[1]>Dn[1])
              {
               if(BuyPosOpen   &&   Up[0]<=Dn[0]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //--- Getting sell signals
            if(Up[1]<Dn[1])
              {
               if(SellPosOpen && Up[0]>=Dn[0]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;
        }
     }
//--- trading
//--- closing a long position
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//--- closing a short position   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//--- open a long position
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//--- open a short position
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---
  }
//+------------------------------------------------------------------+
