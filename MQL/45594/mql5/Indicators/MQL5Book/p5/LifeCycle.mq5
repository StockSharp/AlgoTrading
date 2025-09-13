//+------------------------------------------------------------------+
//|                                                    LifeCycle.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include <MQL5Book/Uninit.mqh>

input int Fake = 0;

//+------------------------------------------------------------------+
//| Global initialization/finalization trap                          |
//+------------------------------------------------------------------+
class Loader
{
   static Loader object;

   Loader()
   {
      Print(__FUNCSIG__);
   }
   ~Loader()
   {
      Print(__FUNCSIG__);
   }
};

static Loader Loader::object;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   Print(__FUNCSIG__, " ", Fake, " ",
      EnumToString((ENUM_DEINIT_REASON)_UninitReason));
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print(__FUNCSIG__, " ", EnumToString((ENUM_DEINIT_REASON)reason));
}
//+------------------------------------------------------------------+
