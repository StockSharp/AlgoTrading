//+------------------------------------------------------------------+
//|                                                   DeltaPrice.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_separate_window
#property indicator_buffers 1
#property indicator_plots   1
#property indicator_applied_price PRICE_CLOSE

#property indicator_type1 DRAW_LINE
#property indicator_color1 clrDodgerBlue
#property indicator_width1 2
#property indicator_style1 STYLE_SOLID

#include <MQL5Book/AppliedTo.mqh>

input int Differencing = 1;

int handle = 0;

// indicator buffer
double Buffer[];

//+------------------------------------------------------------------+
//| Helper function to get relative location inside MQL5 folder      |
//+------------------------------------------------------------------+
string GetMQL5Path()
{
   static const string MQL5 = "\\MQL5\\";
   static const int length = StringLen(MQL5) - 1;
   static const string path = MQLInfoString(MQL_PROGRAM_PATH);
   const int start = StringFind(path, MQL5);
   if(start != -1)
   {
      return StringSubstr(path, start + length);
   }
   return path;
}

//+------------------------------------------------------------------+
//| Indicator initialization function                                |
//+------------------------------------------------------------------+
int OnInit()
{
   if(Differencing < 0) return INIT_PARAMETERS_INCORRECT;

   // NB! If you want to request MQLInfoString(MQL_PROGRAM_NAME)
   // to get indicator filename, you should do it before any call to 
   // IndicatorSetString(INDICATOR_SHORTNAME, "string"),
   // because the last will change MQL_PROGRAM_NAME to the specified "string"
   // instead of default "filename" (taken from the original 'filename.ex5')
   
   const string label = "DeltaPrice (" + (string)Differencing + "/" + APPLIED_TO_STR() + ")";
   IndicatorSetString(INDICATOR_SHORTNAME, label);
   PlotIndexSetString(0, PLOT_LABEL, label);
   IndicatorSetInteger(INDICATOR_DIGITS, _Digits);
   
   SetIndexBuffer(0, Buffer);
   if(Differencing > 1)
   {
      handle = iCustom(_Symbol, _Period, GetMQL5Path(), Differencing - 1);
      if(handle == INVALID_HANDLE)
      {
         return INIT_FAILED;
      }
   }
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Indicator calculation function                                   |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   for(int i = fmax(prev_calculated - 1, 1); i < rates_total; ++i)
   {
      if(Differencing > 1)
      {
         static double value[2];
         if(CopyBuffer(handle, 0, rates_total - i - 1, 2, value) == 2)
         {
            Buffer[i] = value[1] - value[0];
         }
      }
      else if(Differencing == 1)
      {
         Buffer[i] = price[i] - price[i - 1];
      }
      else
      {
         Buffer[i] = price[i];
      }
   }
   return rates_total;
}

//+------------------------------------------------------------------+
//| Indicator finalization function                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   IndicatorRelease(handle);
}
//+------------------------------------------------------------------+
