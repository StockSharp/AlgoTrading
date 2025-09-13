//+------------------------------------------------------------------+
//|                                      Exp_JBrainSig1_UltraRSI.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------------------+
//|  CXMA class description                       |
//+-----------------------------------------------+
#include <SmoothAlgorithms.mqh>
//+-----------------------------------------------+
//|  Declaration of enumerations                  |
//+-----------------------------------------------+
enum AlgMode
  {
   JBrainSig1Filter,//filter - JBrainSig1, signal UltraRSI
   UltraRSIFilter,  //filter - UltraRSI, signal - JBrainSig1
   Composition      //simultaneous filtration
  };
//+-----------------------------------------------+
//|  Declaration of enumerations                  |
//+-----------------------------------------------+
/*enum Smooth_Method - the enumeration is declared in the SmoothAlgorithms.mqh file
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
//+-----------------------------------------------+
//| Expert Advisor indicator input parameters     |
//+-----------------------------------------------+
input double MM=-0.1;             //Share of a deposit in a deal, negative values - lot size
input int    StopLoss_=1000;      //stop loss in points
input int    TakeProfit_=2000;    //take profit in points
input int    Deviation_=10;       //max. price deviation in points
input bool   BuyPosOpen=true;     //Permission to buy
input bool   SellPosOpen=true;    //Permission to sell
input bool   BuyPosClose=true;     //Permission to exit long positions
input bool   SellPosClose=true;    //Permission to exit short positions
input AlgMode Mode=Composition;    //algorithm to enter in the market
//+-----------------------------------------------+
//| Indicators input parameters                   |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator time frame
input uint SignalBar=1;//bar index for getting an entry signal

//+-----------------------------------------------+
//| JBrainTrendSig1 indicator input parameters    |
//+-----------------------------------------------+
input int ATR_Period=7; //ATR period 
input int STO_Period=9; //stochastic period
input ENUM_MA_METHOD XMA_Method=MODE_SMA; //smoothing method
input ENUM_STO_PRICE STO_Price=STO_LOWHIGH; //method of prices calculation
input int XLength=7; // depth of the JMA smoothing                   
input int XPhase=100; // parameter of the JMA smoothing,
                      //that changes within the range -100 ... +100,
//impacts the transitional process quality;
//+-----------------------------------------------+
//| UltraRSI indicator input parameters           |
//+-----------------------------------------------+
input int RSI_Period=13; //RSI indicator period
input ENUM_APPLIED_PRICE Applied_price=PRICE_CLOSE; //applied price
//----
input Smooth_Method W_Method=MODE_JJMA; //smoothing method
input int StartLength=3; //initial smoothing period                    
input int WPhase=100; //smoothing parameter,
                      // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
//----  
input uint nStep=5; //period change stepà
input uint nStepsTotal=10; //number of period changes
//----
input Smooth_Method SmoothMethod=MODE_JJMA; //smoothing method
input int SmoothLength=3; //smoothing depth                    
input int SmoothPhase=100; //smoothing parameter,
                           // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing; 
//+-----------------------------------------------+
//---- Declaration of integer variables for storing a chart period in seconds 
int TimeShiftSec;
//---- declaration of integer variables for the indicators handles
int InpInd1_Handle,InpInd2_Handle;
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
//---- getting handle of the JBrainTrend1Sig indicator
   InpInd1_Handle=iCustom(Symbol(),InpInd_Timeframe,"JBrainTrend1Sig",ATR_Period,STO_Period,XMA_Method,STO_Price,XLength,XPhase);
   if(InpInd1_Handle==INVALID_HANDLE) Print("Failed to get handle of the JBrainTrend1Sig indicator");

//---- getting handle of the UltraRSI indicator
   InpInd2_Handle=iCustom(Symbol(),InpInd_Timeframe,"UltraRSI",RSI_Period,Applied_price,W_Method,
                          StartLength,WPhase,nStep,nStepsTotal,SmoothMethod,SmoothLength,SmoothPhase,50,50,clrBlue,clrBlue,1,1);
   if(InpInd2_Handle==INVALID_HANDLE) Print("Failed to get handle of the UltraRSI indicator");

//---- initialization of a variable for storing a chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of the start of data calculation
   int min_rates_1=int(MathMax(MathMax(ATR_Period,STO_Period),30)+2);

//---- Initialization of variables of the start of data calculation
   CXMA XMA;
   int min_rates_2=RSI_Period;
   min_rates_2+=XMA.GetStartBars(W_Method,StartLength+nStep*nStepsTotal,WPhase)+1;
   min_rates_2+=XMA.GetStartBars(SmoothMethod,SmoothLength,SmoothPhase);
   min_rates_total=int(MathMax(min_rates_1,min_rates_2)+3+SignalBar);
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
   if(BarsCalculated(InpInd1_Handle)<min_rates_total || BarsCalculated(InpInd2_Handle)<min_rates_total) return;

//---- uploading history for IsNewBar() and SeriesInfoInteger() functions normal operation  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+-----------------------------------------------+
//| Detecting market entry signals                |
//+-----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //---- declaration of local variables
      double UpVal1[1],DnVal1[1];
      double UpVal2[2],DnVal2[2];
      bool BUY_Open1,SELL_Open1,BUY_Open2,SELL_Open2;
      bool BUY_Close1,SELL_Close1,BUY_Close2,SELL_Close2;

      //---- zeroing out trading signals
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;

      BUY_Open1=false;
      SELL_Open1=false;
      BUY_Close1=false;
      SELL_Close1=false;

      BUY_Open2=false;
      SELL_Open2=false;
      BUY_Close2=false;
      SELL_Close2=false;

      Recount=false;

      //---- Search of the last direction of trade by JBrainTrendSig1
      int Bars_=Bars(Symbol(),InpInd_Timeframe);

      for(int bar=int(SignalBar); bar<Bars_; bar++)
        {
         if(CopyBuffer(InpInd1_Handle,0,bar,1,DnVal1)<=0) {Recount=true; return;}
         if(CopyBuffer(InpInd1_Handle,1,bar,1,UpVal1)<=0) {Recount=true; return;}

         if(DnVal1[0])
           {
            BUY_Close1=true;
            if(bar==int(SignalBar)) SELL_Open1=true;
            break;
           }

         if(UpVal1[0])
           {
            SELL_Close1=true;
            if(bar==int(SignalBar)) BUY_Open1=true;
            break;
           }
        }

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd2_Handle,0,SignalBar,2,UpVal2)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd2_Handle,1,SignalBar,2,DnVal2)<=0) {Recount=true; return;}

      //---- Getting buy signals
      if(UpVal2[1]>DnVal2[1])
        {
         if(UpVal2[0]<=DnVal2[0]) BUY_Open2=true;
         SELL_Close2=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals
      if(DnVal2[1]>UpVal2[1])
        {
         if(DnVal2[0]<=UpVal2[0]) SELL_Open2=true;
         BUY_Close2=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      switch(Mode)
        {
         case JBrainSig1Filter:
            if(BuyPosOpen&&BUY_Open2&&SELL_Close1) BUY_Open=true;
            if(SellPosOpen&&SELL_Open2&&BUY_Close1) SELL_Open=true;
            break;

         case UltraRSIFilter:
            if(BuyPosOpen&&BUY_Open1&&SELL_Close2) BUY_Open=true;
            if(SellPosOpen&&SELL_Open1&&BUY_Close2) SELL_Open=true;
            break;

         case Composition:
            if(BuyPosOpen&&(BUY_Open1&&SELL_Close2 || BUY_Open2&&SELL_Close1)) BUY_Open=true;
            if(SellPosOpen&&(SELL_Open1&&BUY_Close2||SELL_Open2&&BUY_Close1)) SELL_Open=true;
            break;
        }

      if(SellPosClose && SELL_Close1 && SELL_Close2) SELL_Close=true;
      if(BuyPosClose  &&  BUY_Close1  &&  BUY_Close2) BUY_Close=true;
     }

//+-----------------------------------------------+
//| Performing deals                              |
//+-----------------------------------------------+
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
