//+------------------------------------------------------------------+
//|                                          Exp_FigurelliSeries.mq5 |
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
enum HourX //Type of constant
  {
   HXNONE=-1, //None
   HX00,      //00
   HX01,      //01
   HX02,      //02
   HX03,      //03
   HX04,      //04
   HX05,      //05
   HX06,      //06
   HX07,      //07
   HX08,      //08
   HX09,      //09
   HX10,      //10
   HX11,      //11
   HX12,      //12
   HX13,      //13
   HX14,      //14
   HX15,      //15
   HX16,      //16
   HX17,      //17
   HX18,      //18
   HX19,      //19
   HX20,      //20
   HX21,      //21
   HX22,      //22
   HX23,      //23
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
input bool       BuyPosClose=true;      //Permission to exit long positions by the indicator signals
input bool       SellPosClose=true;      //Permission to exit short positions by the indicator signals
//----
input Hour       StartHour=H08;         //Hour of position opening
input Min        StartMinute=M00;       //Minute of position opening
input HourX      StopHour=HX23;         //Hour of position closing
input Min        StopMinute=M59;        //Minute of position closing
//+-----------------------------------------------+
//| Indicator input parameters                    |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_M30; //indicator timeframe
//----
input uint iStartPeriod=6;  // initial period
input uint iStep=6;         // periods calculation step
input uint iTotal=36;       // number of Moving Averages
input  ENUM_MA_METHOD   iMAType=MODE_EMA; // Moving Averages smoothing type
input ENUM_APPLIED_PRICE   iMAPrice=PRICE_CLOSE; // price timeseries of Moving Averages
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
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- Checking time interval for correctness
    if(int(StopHour)<int(StartHour))
      {
       Print(__FUNCTION__," Incorrect input parameter values for opening and closing of the position!");
       return(-1);
      }
      
//---- getting handle of the FigurelliSeries indicator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"FigurelliSeries",iStartPeriod,iStep,iTotal,iMAType,iMAPrice,0);
   if(InpInd_Handle==INVALID_HANDLE) Print("Failed to get handle of the FigurelliSeries indicator");

//---- initialization of a variable for storing the chart period in seconds  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Initialization of variables of data calculation starting point
   min_rates_total=int(iStartPeriod+iStep*(iTotal-1));
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
      if(CopyBuffer(InpInd_Handle,0,SignalBar,1,IndSeries)<=0) {Recount=true; return;}

      //---- Getting buy signals from the indicator
      if(IndSeries[0]>0)
        {
         if(BuyPosOpen) BUY_Open1=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Getting sell signals from the indicator
      if(IndSeries[0]<0)
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

//---- fetting signals to exit the positions by time ending   
   if(tm.hour==StopHour && tm.min>=StopMinute || tm.hour>StopHour || tm.hour<StartHour)
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
