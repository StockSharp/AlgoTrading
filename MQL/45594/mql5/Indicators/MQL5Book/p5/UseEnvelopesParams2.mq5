//+------------------------------------------------------------------+
//|                                          UseEnvelopesParams2.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 2
#property indicator_plots   2

// drawing settings
#property indicator_type1   DRAW_LINE
#property indicator_color1  clrBlue
#property indicator_width1  1
#property indicator_label1  "Upper"
#property indicator_style1  STYLE_DOT

#property indicator_type2   DRAW_LINE
#property indicator_color2  clrRed
#property indicator_width2  1
#property indicator_label2  "Lower"
#property indicator_style2  STYLE_DOT

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/MqlParamBuilder.mqh>

input int WorkPeriod = 14;
input int Shift = 0;
input ENUM_MA_METHOD Method = MODE_EMA;
input ENUM_APPLIED_PRICE Price = PRICE_TYPICAL;
input double Deviation = 0.1;

// indicator buffer
double UpBuffer[];
double DownBuffer[];

// global variable for subordinate indicator
int Handle;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   IndicatorSetString(INDICATOR_SHORTNAME,
      StringFormat("Env(%d,%d,%.2f)", WorkPeriod, Shift, Deviation));
   
   // map arrays to indicator buffers
   SetIndexBuffer(0, UpBuffer);
   SetIndexBuffer(1, DownBuffer);
   
   // Simple way of creating "Envelopes" could be via
   // Handle = iEnvelopes(int period, int shift,
   // ENUM_MA_METHOD method, ENUM_APPLIED_PRICE price, double deviation)
   
   // But we'll use alternative way based on array of MqlParam
   /*
   // Here is an example of verbose code of filling out the array
   MqlParam params[5] = {};
   params[0].type = TYPE_INT;
   params[0].integer_value = WorkPeriod;
   params[1].type = TYPE_INT;
   params[1].integer_value = Shift;
   params[2].type = TYPE_INT;
   params[2].integer_value = Method;
   params[3].type = TYPE_INT;
   params[3].integer_value = Price;
   params[4].type = TYPE_DOUBLE;
   params[4].double_value = Deviation;
   */

   // Instead we use more handy equivalent provided by the builder:
   // data types are detected automatically, array size is adjusted internally
   MqlParam params[];
   MqlParamBuilder builder;
   // order is important, operators << and >> are executed from left to right
   builder << WorkPeriod << Shift << Method << Price << Deviation >> params;

   // Make sure everything is filled ok
   ArrayPrint(params);
   /*
       [type] [integer_value] [double_value] [string_value]
   [0]      7              14        0.00000 null            <- "INT" period
   [1]      7               0        0.00000 null            <- "INT" shift
   [2]      7               1        0.00000 null            <- "INT" EMA
   [3]      7               6        0.00000 null            <- "INT" TYPICAL
   [4]     13               0        0.10000 null            <- "DOUBLE" deviation
   */

   // First try to pass not the whole array, but only 3 elements.
   // This will generate an error, but it's intentional - just for demonstration purpose
   Handle = PRTF(IndicatorCreate(_Symbol, _Period, IND_ENVELOPES, 3, params));
   // EXAMPLE ERROR:
   // indicator Envelopes cannot load [4002]   
   // IndicatorCreate(_Symbol,_Period,IND_ENVELOPES,3,params)=-1 / WRONG_INTERNAL_PARAMETER(4002)
   
   // Second, call IndicatorCreate with complete set of parameters (5 required for Envelopes)
   Handle = PRTF(IndicatorCreate(_Symbol, _Period, IND_ENVELOPES, ArraySize(params), params));
   // SUCCESS:
   // IndicatorCreate(_Symbol,_Period,IND_ENVELOPES,ArraySize(params),params)=10 / ok
   
   return Handle == INVALID_HANDLE ? INIT_FAILED : INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   // wait until the subindicator is calculated for all bars
   if(BarsCalculated(Handle) != rates_total)
   {
      return prev_calculated;
   }
   
   // copy data from subordinate indicator into our buffers
   const int n = CopyBuffer(Handle, 0, 0, rates_total - prev_calculated + 1, UpBuffer);
   const int m = CopyBuffer(Handle, 1, 0, rates_total - prev_calculated + 1, DownBuffer);
   
   return n > -1 && m > -1 ? rates_total : 0;
}
//+------------------------------------------------------------------+
