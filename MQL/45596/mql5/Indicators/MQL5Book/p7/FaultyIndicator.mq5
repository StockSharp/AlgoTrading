//+------------------------------------------------------------------+
//|                                              FaultyIndicator.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#resource "SubFolder\\NonEmbeddedIndicator.ex5"

int handle;

//+------------------------------------------------------------------+
//| Indicator initialization function                                |
//+------------------------------------------------------------------+
int OnInit()
{
   handle = iCustom(_Symbol, _Period, "::SubFolder\\NonEmbeddedIndicator.ex5");
   if(handle == INVALID_HANDLE)
   {
      return INIT_FAILED;
   }
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
   IndicatorRelease(handle);
}
//+------------------------------------------------------------------+
