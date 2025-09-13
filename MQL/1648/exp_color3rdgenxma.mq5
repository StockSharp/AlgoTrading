//+------------------------------------------------------------------+
//|                                           Exp_Color3rdGenXMA.mq5 |
//|                               Copyright © 2013, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2013, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.01"
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Hour //Type of constant
  {
   H00=0,    //00
   H01,      //01
   H02,      //02
   H03,      //03
   H04,      //04
   H05,      //05
   H06,      //06
   H07,      //07
   H08,      //08
   H09,      //09
   H10,      //10
   H11,      //11
   H12,      //12
   H13,      //13
   H14,      //14
   H15,      //15
   H16,      //16
   H17,      //17
   H18,      //18
   H19,      //19
   H20,      //20
   H21,      //21
   H22,      //22
   H23,      //23
  };
//+----------------------------------------------+
//|  declaration of enumerations                 |
//+----------------------------------------------+
enum Min //Type of constant
  {
   M00=0,    //00
   M01,      //01
   M02,      //02
   M03,      //03
   M04,      //04
   M05,      //05
   M06,      //06
   M07,      //07
   M08,      //08
   M09,      //09
   M10,      //10
   M11,      //11
   M12,      //12
   M13,      //13
   M14,      //14
   M15,      //15
   M16,      //16
   M17,      //17
   M18,      //18
   M19,      //19
   M20,      //20
   M21,      //21
   M22,      //22
   M23,      //23
   M24,      //24
   M25,      //25
   M26,      //26
   M27,      //27
   M28,      //28
   M29,      //29
   M30,      //30
   M31,      //31
   M32,      //32
   M33,      //33
   M34,      //34
   M35,      //35
   M36,      //36
   M37,      //37
   M38,      //38
   M39,      //39
   M40,      //40
   M41,      //41
   M42,      //42
   M43,      //43
   M44,      //44
   M45,      //45
   M46,      //46
   M47,      //47
   M48,      //48
   M49,      //49
   M50,      //50
   M51,      //51
   M52,      //52
   M53,      //53
   M54,      //54
   M55,      //55
   M56,      //56
   M57,      //57
   M58,      //58
   M59       //59
  };
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
//|  CXMA class description                      |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  declaration of enumerations                 |
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
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
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
//+-----------------------------------------------+
//| Expert Advisor indicator input parameters     |
//+-----------------------------------------------+
input double MM=0.1;                //Share of a deposit in a deal
input         MarginMode MMMode=LOT; //lot value detection method
input uint    StopLoss_=1000;        //Stop Loss in points
input uint    TakeProfit_=2000;      //Take Profit in points
input uint    Deviation_=10;         //max. price deviation in points
input bool   BuyPosOpen=true;       // Permission to enter long position
input bool   SellPosOpen=true;      //Permission to enter short position
input bool       BuyPosClose=false;      //Permission to exit long positions by the indicator signals
input bool       SellPosClose=false;      //Permission to exit short positions by the indicator signals
//----
input Hour       StartHour=H08;         //Hour of position opening
input Min        StartMinute=M00;       //Minute of position opening
input uint       TimeMin=720;           //Position holding time in minutes
//+-----------------------------------------------+
//| Indicator input parameters                    |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //indicator timeframe
//----
input Smooth_Method XMA_Method=MODE_EMA; //averaging method
input uint XLength=50; //smoothing depth                    
input int XPhase=15; //smoothing parameter,
                     //for JJMA, it varies within the range -100 ... +100 and influences on the quality of the transient period;
// For VIDIA, it is a CMO period, for AMA, it is a slow moving average period
input Applied_price_ IPC=PRICE_TYPICAL;//price constant
input int Shift=0; // horizontal shift of the indicator in bars
input int PriceShift=0; // vertical shift of the indicator in points
//----
input uint SignalBar=1;//bar index for getting an entry signal
//+-----------------------------------------------+
//---- Declaration of integer variables for storing the chart period in seconds 
int TimeShiftSec;
//---- Declaration of integer variables for the indicator handles
int InpInd_Handle;
//---- declaration of the integer variables for the start of data calculation
int min_rates_total;
//+------------------------------------------------------------------+
//--- Check opened position on the holding time exploration
//+------------------------------------------------------------------+
bool CloseCheck(
                string symbol,      //Symbol
                int Time             //Position holding time in minutes
                )
  {
//---- Checking, if there is an open position
   if(!PositionSelect(symbol)) return(false);
   int OpenPosTime=int((TimeCurrent()-PositionGetInteger(POSITION_TIME))/60);
   if(OpenPosTime>=Time) return(true);
//----
   return(false);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- получение хендла индикатора Color3rdGenXMA
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"Color3rdGenXMA",XMA_Method,XLength,XPhase,IPC,0,0);
   if(InpInd_Handle==INVALID_HANDLE) Print("Failed to get handle with the Color3rdGenXMA indicator");

//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
   CXMA XMA;
//---- Initialization of variables of data calculation starting point
   min_rates_total=XMA.GetStartBars(XMA_Method,2*XLength,XPhase);
   min_rates_total+=XMA.GetStartBars(XMA_Method,XLength,XPhase)+2;
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

//---- Declaration of static variables
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Open1=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Open1=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+----------------------------------------------+
//| Determining transaction signals              |
//+----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // checking for a new bar
     {
      //---- zeroing out trading signals
      BUY_Open1=false;
      SELL_Open1=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- Declaration of local variables
      double IndSeries[1];

      //---- copy newly appeared data into the arrays
      if(CopyBuffer(InpInd_Handle,1,SignalBar,1,IndSeries)<=0) {Recount=true; return;}

      //---- Getting buy signals from the indicator
      if(IndSeries[0]==2)
        {
         if(BuyPosOpen) BUY_Open1=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals from the indicator
      if(IndSeries[0]==0)
        {
         if(SellPosOpen) SELL_Open1=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//---- Getting buy signals
   MqlDateTime tm;
   TimeToStruct(TimeCurrent(),tm);

   if(BUY_Open1 && tm.hour==StartHour && tm.min==StartMinute) BUY_Open=true;
   if(SELL_Open1 && tm.hour==StartHour && tm.min==StartMinute) SELL_Open=true;

//---- Getting signals to exit the positions by time ending   
   if((BuyPosOpen || SellPosOpen) && CloseCheck(Symbol(),TimeMin))
     {
      if(BuyPosOpen) BUY_Close=true;
      if(SellPosOpen) SELL_Close=true;
     }

//+----------------------------------------------+
//| Performing deals                             |
//+----------------------------------------------+
//---- Closing long
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Closing short   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- Opening long
   if(BUY_Open1) BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);

//---- Opening short
   if(SELL_Open1) SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
