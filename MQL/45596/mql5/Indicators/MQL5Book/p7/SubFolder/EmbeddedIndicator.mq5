//+------------------------------------------------------------------+
//|                                            EmbeddedIndicator.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

input int Reference = 0;

int handle = 0;

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
   Print(Reference);
   Print("Name: " + MQLInfoString(MQL_PROGRAM_NAME));
   Print("Full path: " + MQLInfoString(MQL_PROGRAM_PATH));
   
   const string location = GetMQL5Path();
   Print("Location in MQL5:" + location);
   
   if(Reference == 0)
   {
      handle = iCustom(_Symbol, _Period, location, 1);
      if(handle == INVALID_HANDLE)
      {
         return INIT_FAILED;
      }
   }
   Print("Success");
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Indicator calculation function (dummy here)                      |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Indicator finalization function                                  |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Print("Deinit ", Reference);
   IndicatorRelease(handle);
}
//+------------------------------------------------------------------+
