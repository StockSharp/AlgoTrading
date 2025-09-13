//+------------------------------------------------------------------+
//|                                                 ZeroLag_MACD.mq5 |
//|                                                        avoitenko |
//|                        https://login.mql5.com/en/users/avoitenko |
//+------------------------------------------------------------------+
#property copyright "avoitenko"
#property link      "https://login.mql5.com/en/users/avoitenko"
#property version   "3.00"

//---
#include <MovingAverages.mqh>
//---
#property indicator_separate_window
#property indicator_buffers 8
#property indicator_plots   2

//--- plot MACDBuffer
#property indicator_type1  DRAW_HISTOGRAM
#property indicator_color1 clrBlue
#property indicator_style1 STYLE_SOLID
#property indicator_width1 2

//--- plot SignalBuffer
#property indicator_type2  DRAW_LINE
#property indicator_color2 clrRed
#property indicator_style2 STYLE_SOLID
#property indicator_width2 1

//---- input parameters
input uint  InpFastPeriod     =  12;//Fast Period
input uint  InpSlowPeriod     =  26;//Slow Period
input uint  InpSignalPeriod   =  9; //Signal Period
input ENUM_APPLIED_PRICE InpAppliedPrice=PRICE_CLOSE;//Applied Price

//---- buffers
double MainBuffer[];
double SignalBuffer[];
double FastBuffer[];
double SlowBuffer[];
double val[];
double val1[];
double val2[];
double val3[];

//---indicator handles
int ma_fast_handle;
int ma_slow_handle;
int max_period;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
  {
//---   
   max_period=(int)fmax(InpFastPeriod,InpSlowPeriod);
//--- indicator buffers mapping
   SetIndexBuffer(0,MainBuffer,INDICATOR_DATA);
   SetIndexBuffer(1,SignalBuffer,INDICATOR_DATA);
   SetIndexBuffer(2,FastBuffer,INDICATOR_CALCULATIONS);
   SetIndexBuffer(3,SlowBuffer,INDICATOR_CALCULATIONS);
   SetIndexBuffer(4,val,INDICATOR_CALCULATIONS);
   SetIndexBuffer(5,val1,INDICATOR_CALCULATIONS);
   SetIndexBuffer(6,val2,INDICATOR_CALCULATIONS);
   SetIndexBuffer(7,val3,INDICATOR_CALCULATIONS);
//---
   ArraySetAsSeries(MainBuffer,true);
   ArraySetAsSeries(SignalBuffer,true);
   ArraySetAsSeries(FastBuffer,true);
   ArraySetAsSeries(SlowBuffer,true);
   ArraySetAsSeries(val,true);
   ArraySetAsSeries(val1,true);
   ArraySetAsSeries(val2,true);
   ArraySetAsSeries(val3,true);
//---
   PlotIndexSetInteger(0,PLOT_DRAW_BEGIN,max_period);
   PlotIndexSetInteger(1,PLOT_DRAW_BEGIN,max_period+InpSignalPeriod);
//---   
   string name=StringFormat("ZeroLag MACD(%d,%d,%d)",InpFastPeriod,InpSlowPeriod,InpSignalPeriod);
   IndicatorSetString(INDICATOR_SHORTNAME,name);
//---
   string label=StringFormat("ZL MACD(%d,%d,%d)",InpFastPeriod,InpSlowPeriod,InpSignalPeriod);
   PlotIndexSetString(0,PLOT_LABEL,label);
   PlotIndexSetString(1,PLOT_LABEL,"Signal");
//--- handles
   ma_fast_handle=iMA(NULL,0,fmax(InpFastPeriod,1),0,MODE_EMA,InpAppliedPrice);
   if(ma_fast_handle==INVALID_HANDLE) {Print("Error initialize iMA handle #1");return(INIT_FAILED);}
//---
   ma_slow_handle=iMA(NULL,0,fmax(InpSlowPeriod,1),0,MODE_EMA,InpAppliedPrice);
   if(ma_slow_handle==INVALID_HANDLE) {Print("Error initialize iMA handle #2");return(INIT_FAILED);}
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime &time[],
                const double &open[],
                const double &high[],
                const double &low[],
                const double &close[],
                const long &tick_volume[],
                const long &volume[],
                const int &spread[])
  {

   int limit;
   int limit2;

   if(prev_calculated>rates_total || prev_calculated<=0)
     {
      limit  = rates_total-max_period-(int)fmax(InpSignalPeriod,1);
      limit2 = rates_total-1;

      ArrayInitialize(MainBuffer,0);
      ArrayInitialize(SignalBuffer,0);
      ArrayInitialize(FastBuffer,0);
      ArrayInitialize(SlowBuffer,0);
     }
   else
     {
      limit = rates_total-prev_calculated;
      limit2= rates_total-prev_calculated;
     }

//---
   if(CopyBuffer(ma_fast_handle, 0, 0, limit2+1, FastBuffer) != limit2+1) return(0);
   if(CopyBuffer(ma_slow_handle, 0, 0, limit2+1, SlowBuffer) != limit2+1) return(0);

//---
   ExponentialMAOnBuffer(rates_total,prev_calculated,0,fmax(InpFastPeriod,1),FastBuffer,val);
   ExponentialMAOnBuffer(rates_total,prev_calculated,0,fmax(InpSlowPeriod,1),SlowBuffer,val1);

//---
   for(int i=limit2; i>=0 && !_StopFlag; i--)
     {
      double ZeroLagEMAp = FastBuffer[i] + FastBuffer[i] - val[i];
      double ZeroLagEMAq = SlowBuffer[i] + SlowBuffer[i] - val1[i];
      MainBuffer[i]=ZeroLagEMAp-ZeroLagEMAq;
     }

//---
   ExponentialMAOnBuffer(rates_total,prev_calculated,0,fmax(InpSignalPeriod,1),MainBuffer,val2);
   ExponentialMAOnBuffer(rates_total,prev_calculated,0,fmax(InpSignalPeriod,1),val2,val3);

//---
   for(int i=limit; i>=0 && !_StopFlag; i--)
      SignalBuffer[i]=val2[i]+val2[i]-val3[i];

//---
   return(rates_total);
  }
//+------------------------------------------------------------------+
