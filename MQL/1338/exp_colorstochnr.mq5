//+------------------------------------------------------------------+
//|                                             Exp_ColorStochNR.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum AlgMode
  {
   Breakdown,  //breakthrough of level by 50th oscillator
   OscTwist,  //changing the oscillator direction
   SignalTwist,//changing the direction of signal line
   OscDisposition, //breakthrough of signal line by the oscillator
   SignalBreakdown  //breakthrough of 50th level by signal line
  };
//+----------------------------------------------+
//|  Declaration of enumeration                  |
//+----------------------------------------------+
enum ENUM_MA_METHOD_
  {
   MODE_SMA_,       // Simple averaging
   MODE_EMA_        // Exponential averaging
  };
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
input AlgMode Mode=OscDisposition; //algorithm to enter in the market
//+----------------------------------------------+
//| Indicator input parameters                   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame

input uint Kperiod=5;  // K period
input uint Dperiod=3;  // D period
input uint Slowing=3;  // Slowing
input ENUM_MA_METHOD_ Dmethod=MODE_SMA_; // smoothing type
input ENUM_STO_PRICE PriceFild=STO_LOWHIGH; // stochastic calculation method
input uint Sens=0; // sensitivity in points

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
//---- getting the ColorStochNR indicator handle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorStochNR",
                         Kperiod,Dperiod,Slowing,Dmethod,PriceFild,Sens,0);
   if(InpInd_Handle==INVALID_HANDLE) Print(" Failed to get handle of ColorStochNR indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of the start of data calculation
   min_rates_total=int(Kperiod+Dperiod+Slowing+1);
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
         case Breakdown:
           {
            double UpValue[2],DnValue[2];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,UpValue)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,1,SignalBar,2,DnValue)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(UpValue[1]>50)
              {
               if(BuyPosOpen && DnValue[0]<=50) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(DnValue[1]<50)
              {
               if(SellPosOpen && UpValue[0]>=50) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case OscTwist:
           {
            double Value[2];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Value)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(Value[1]==1 && Value[0]>1 || Value[1]==2 && Value[0]>2 || Value[1]==3 && Value[0]==4)
              {
               if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(Value[1]==4 && (Value[0]<4 && Value[0]) || Value[1]==3 && (Value[0]<3 && Value[0]) || Value[1]==2 && Value[0]<=1)
              {
               if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case SignalTwist:
           {
            double Value[3];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,3,SignalBar,3,Value)<=0) {Recount=true; return;}

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
         
         case OscDisposition:
           {
            double Signal[2];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,4,SignalBar,2,Signal)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(Signal[1]==1)
              {
               if(BuyPosOpen) if(Signal[0]==2 || Signal[0]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(Signal[1]==2)
              {
               if(SellPosOpen) if(Signal[0]==1 || Signal[0]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;
         
         case SignalBreakdown:
           {
            double Signal[2];
            //---- copy newly appeared data into the arrays
            if(CopyBuffer(InpInd_Handle,4,SignalBar,2,Signal)<=0) {Recount=true; return;}

            //---- Getting buy signals
            if(Signal[1]>50)
              {
               if(BuyPosOpen && Signal[0]<=50) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Getting sell signals
            if(Signal[1]<50)
              {
               if(SellPosOpen && Signal[0]>=50) SELL_Open=true;
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
