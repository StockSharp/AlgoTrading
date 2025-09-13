/*
 * For the indicator to work, place the
 * SmoothAlgorithms.mqh
 * in the directory: MetaTrader\\MQL5\Include
 */
//+------------------------------------------------------------------+
//|                                                   BinaryWave.mq5 | 
//|                                          Copyright © 2009, LeMan |
//|                                                 b-market@mail.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, LeMan"
#property link      "b-market@mail.ru"
//---- indicator version number
#property version   "1.00"
//---- drawing indicator in a separate window
#property indicator_separate_window 
//---- number of indicator buffers
#property indicator_buffers 1 
//---- only one plot is used
#property indicator_plots   1
//+-----------------------------------+
//|  Parameters of indicator drawing  |
//+-----------------------------------+
//---- drawing the indicator as a line
#property indicator_type1   DRAW_LINE
//---- DodgerBlue color is used for indicator line
#property indicator_color1 DodgerBlue
//---- the indicator line is a continuous curve
#property indicator_style1  STYLE_SOLID
//---- Indicator line width is equal to 1
#property indicator_width1  1
//---- displaying the indicator label
#property indicator_label1  "BinaryWave"
//+----------------------------------------------+
//| Parameters of displaying horizontal levels   |
//+----------------------------------------------+
#property indicator_level1  0
#property indicator_levelcolor Red
#property indicator_levelstyle STYLE_SOLID
//+-----------------------------------+
//|  Declaration of constants         |
//+-----------------------------------+
#define RESET  0 // the constant for getting the command for the indicator recalculation back to the terminal
//+-----------------------------------+
//|  CXMA class description           |
//+-----------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-----------------------------------+

//---- declaration of the CXMA class variables from the SmoothAlgorithms.mqh file
CXMA XMA1;
//+-----------------------------------+
//|  Declaration of enumerations      |
//+-----------------------------------+
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
//+-----------------------------------+
//|  Input parameters of the indicator|
//+-----------------------------------+
//---- indicators weight. The indicator does not take part in the wave calculation in case of a zero value
input double WeightMA    = 1.0;
input double WeightMACD  = 1.0;
input double WeightOsMA  = 1.0;
input double WeightCCI   = 1.0;
input double WeightMOM   = 1.0;
input double WeightRSI   = 1.0;
input double WeightADX   = 1.0;
//---- Moving Average parameters
input int   MAPeriod=13;
input  ENUM_MA_METHOD   MAType=MODE_EMA;
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;
//---- MACD parameters
input int   FastMACD     = 12;
input int   SlowMACD     = 26;
input int   SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
//---- OsMA parameters
input int   FastPeriod   = 12;
input int   SlowPeriod   = 26;
input int   SignalPeriod = 9;
input ENUM_APPLIED_PRICE   OsMAPrice=PRICE_CLOSE;
//---- CCI parameters
input int   CCIPeriod=14;
input ENUM_APPLIED_PRICE   CCIPrice=PRICE_MEDIAN;
//---- Momentum parameters
input int   MOMPeriod=14;
input ENUM_APPLIED_PRICE   MOMPrice=PRICE_CLOSE;
//---- RSI parameters
input int   RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
//---- ADX parameters
input int   ADXPeriod=14;
//---- including wave smoothing
input Smooth_Method bMA_Method=MODE_JJMA; //smoothing method
input int bLength=5; //smoothing depth                    
input int bPhase=100; //smoothing parameter,
                     // for JJMA that can change withing the range -100 ... +100. It impacts the quality of the intermediate process of smoothing;
// for VIDIA it is a CMO period, for AMA it is a slow average period
//+-----------------------------------+

//---- declaration of a dynamic array that further 
// will be used as an indicator buffer
double IndBuffer[];
//---- declaration of the integer variables for the start of data calculation
int min_rates_total,min_rates_total_1;
//---- declaration of integer variables for the indicators handles
int MA_Handle,MACD_Handle,OsMA_Handle,CCI_Handle,MOM_Handle,RSI_Handle,ADX_Handle;
//+------------------------------------------------------------------+
//| Determine the close price position relative to the Moving Average|
//+------------------------------------------------------------------+    
double MAClose(int bar,double &MaArray[],const double &Close[])
  {
//----
   if(WeightMA>0)
     {
      if(Close[bar]-MaArray[bar]>0) return(+WeightMA);
      if(Close[bar]-MaArray[bar]<0) return(-WeightMA);
      //if(Close[bar]-MaArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Determine MACD slope                                             |
//+------------------------------------------------------------------+    
double MACD(int bar,double &MacdArray[])
  {
//----
   if(WeightMACD>0)
     {
      if(MacdArray[bar]-MacdArray[bar+1]>0) return(+WeightMACD);
      if(MacdArray[bar]-MacdArray[bar+1]<0) return(-WeightMACD);
      //if(MacdArray[bar]-MacdArray[bar+1]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Determine OsMa position relative to zero                         |
//+------------------------------------------------------------------+    
double OsMA(int bar,double &OsMAArray[])
  {
//----
   if(WeightOsMA>0)
     {
      if(OsMAArray[bar]>0) return(+WeightOsMA);
      if(OsMAArray[bar]<0) return(-WeightOsMA);
      //if(OsMAArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Determine CCI position relative to zero                          |
//+------------------------------------------------------------------+    
double CCI(int bar,double &CCIArray[])
  {
//----
   if(WeightCCI>0)
     {
      if(CCIArray[bar]>0) return(+WeightCCI);
      if(CCIArray[bar]<0) return(-WeightCCI);
      //if(CCIArray[bar]==0) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Determine Momentum position relative to 100                      |
//+------------------------------------------------------------------+    
double MOM(int bar,double &MOMArray[])
  {
//----
   if(WeightMOM>0)
     {
      if(MOMArray[bar]>100) return(+WeightMOM);
      if(MOMArray[bar]<100) return(-WeightMOM);
      //if(MOMArray[bar]==100) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Determine RSI position relative to 50                            |
//+------------------------------------------------------------------+    
double RSI(int bar,double &RSIArray[])
  {
//----
   if(WeightRSI>0)
     {
      if(RSIArray[bar]>50) return(+WeightRSI);
      if(RSIArray[bar]<50) return(-WeightRSI);
      //if(RSIArray[bar]==100) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| Determine DMI position                                           |
//+------------------------------------------------------------------+    
double ADX(int bar,double &DMIPArray[],double &DMIMArray[])
  {
//----
   if(WeightADX>0)
     {
      if(DMIPArray[bar]>DMIMArray[bar]) return(+WeightADX);
      if(DMIPArray[bar]<DMIMArray[bar]) return(-WeightADX);
      //if(DMIPArray[bar]==DMIMArray[bar]) return(0);
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+   
//| BinaryWave indicator initialization function                     | 
//+------------------------------------------------------------------+ 
void OnInit()
  {
//---- Initialization of variables of the start of data calculation
   min_rates_total_1=MathMax(MAPeriod,MathMax(SlowPeriod,MathMax(CCIPeriod,MathMax(SlowMACD,MOMPeriod))))+1;
   min_rates_total=min_rates_total_1+XMA1.GetStartBars(bMA_Method,bLength,bPhase);
   
//---- setting up alerts for unacceptable values of external parameters
   XMA1.XMALengthCheck("bLength", bLength);
   XMA1.XMAPhaseCheck("bPhase", bPhase, bMA_Method);
   
//---- getting handle of the iMA indicator
   MA_Handle=iMA(NULL,0,MAPeriod,0,MAType,MAPrice);
   if(MA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMA indicator");
//---- getting handle of the iMACD indicator
   MACD_Handle=iMACD(NULL,0,FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(MACD_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMACD indicator");
//---- getting handle of the iOsMA indicator
   OsMA_Handle=iOsMA(NULL,0,FastPeriod,SlowPeriod,SignalPeriod,OsMAPrice);
   if(OsMA_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iOsMA indicator");
//---- getting handle of the iCCI indicator
   CCI_Handle=iCCI(NULL,0,CCIPeriod,CCIPrice);
   if(CCI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iCCI indicator");
//---- getting handle of the iMomentum indicator
   MOM_Handle=iMomentum(NULL,0,MOMPeriod,MOMPrice);
   if(MOM_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iMomentum indicator");
//---- getting handle of the iRSI indicator
   RSI_Handle=iRSI(NULL,0,RSIPeriod,RSIPrice);
   if(RSI_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iRSI indicator");
//---- getting handle of the iADX indicator
   ADX_Handle=iADX(NULL,0,ADXPeriod);
   if(ADX_Handle==INVALID_HANDLE) Print(" Failed to get handle of the iADX indicator");

//---- set dynamic array as an indicator buffer
   SetIndexBuffer(0,IndBuffer,INDICATOR_DATA);
//---- performing the shift of beginning of indicator drawing
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,min_rates_total);
//---- setting the indicator values that won't be visible on a chart
   PlotIndexSetDouble(0,PLOT_EMPTY_VALUE,EMPTY_VALUE);
//---- indexing elements in the buffer as in timeseries
   ArraySetAsSeries(IndBuffer,true);

//---- initializations of variable for indicator short name
   string shortname;
   string Smooth1=XMA1.GetString_MA_Method(bMA_Method);
   StringConcatenate(shortname,"BinaryWave(",bLength,", ",Smooth1,")");
//--- creation of the name to be displayed in a separate sub-window and in a pop up help
   IndicatorSetString(INDICATOR_SHORTNAME,shortname);

//---- determination of accuracy of displaying the indicator values
   IndicatorSetInteger(INDICATOR_DIGITS,_Digits+1);
//---- end of initialization
  }
//+------------------------------------------------------------------+ 
//| BinaryWave iteration function                                    | 
//+------------------------------------------------------------------+ 
int OnCalculate(
                const int rates_total,    // amount of history in bars at the current tick
                const int prev_calculated,// amount of history in bars at the previous tick
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[]
                )
  {
//---- checking the number of bars to be enough for calculation
   if(BarsCalculated(MA_Handle)<rates_total
      || BarsCalculated(MACD_Handle)<rates_total
      || BarsCalculated(OsMA_Handle)<rates_total
      || BarsCalculated(CCI_Handle)<rates_total
      || BarsCalculated(MOM_Handle)<rates_total
      || BarsCalculated(RSI_Handle)<rates_total
      || BarsCalculated(ADX_Handle)<rates_total
      || rates_total<min_rates_total)
      return(RESET);
      
//---- declaration of local variables 
   int to_copy,limit,bar,maxbar;
   double tmp,MA_[],MACD_[],OsMA_[],CCI_[],MOM_[],RSI_[],DMIP_[],DMIM_[];

//--- calculations of the necessary amount of data to be copied and
//----the limit starting number for loop of bars recalculation
   if(prev_calculated>rates_total || prev_calculated<=0)// checking for the first start of calculation of an indicator
     {
      to_copy=rates_total; // calculated number of all bars
      limit=rates_total-2; // starting index for calculation of all bars
     }
   else
     {
      to_copy=rates_total-prev_calculated+1; // calculated number of new bars
      limit=rates_total-prev_calculated; // starting number for calculation of new bars
     }

//---- copy newly appeared data into the arrays
   if(CopyBuffer(MA_Handle,0,0,to_copy,MA_)<=0) return(RESET);
   if(CopyBuffer(MACD_Handle,0,0,to_copy+1,MACD_)<=0) return(RESET);
   if(CopyBuffer(OsMA_Handle,0,0,to_copy,OsMA_)<=0) return(RESET);
   if(CopyBuffer(CCI_Handle,0,0,to_copy,CCI_)<=0) return(RESET);
   if(CopyBuffer(MOM_Handle,0,0,to_copy,MOM_)<=0) return(RESET);
   if(CopyBuffer(RSI_Handle,0,0,to_copy,RSI_)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,1,0,to_copy,DMIP_)<=0) return(RESET);
   if(CopyBuffer(ADX_Handle,2,0,to_copy,DMIM_)<=0) return(RESET);

//---- indexing elements in arrays as timeseries  
   ArraySetAsSeries(MA_,true);
   ArraySetAsSeries(MACD_,true);
   ArraySetAsSeries(OsMA_,true);
   ArraySetAsSeries(CCI_,true);
   ArraySetAsSeries(MOM_,true);
   ArraySetAsSeries(RSI_,true);
   ArraySetAsSeries(DMIP_,true);
   ArraySetAsSeries(DMIM_,true);
   ArraySetAsSeries(close,true);

//----   
   maxbar=rates_total-min_rates_total_1-1;

//---- main cycle of calculation of the indicator
   for(bar=limit; bar>=0 && !IsStopped(); bar--)
     {
      tmp=MAClose(bar,MA_,close)+MACD(bar,MACD_)+OsMA(bar,OsMA_)+CCI(bar,CCI_)+MOM(bar,MOM_)+RSI(bar,RSI_)+ADX(bar,DMIP_,DMIM_);
      IndBuffer[bar]=XMA1.XMASeries(maxbar,prev_calculated,rates_total,bMA_Method,bPhase,bLength,tmp,bar,true);
     }
//----     
   return(rates_total);
  }
//+------------------------------------------------------------------+
