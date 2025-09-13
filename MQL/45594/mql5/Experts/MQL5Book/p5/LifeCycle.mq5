//+------------------------------------------------------------------+
//|                                                    LifeCycle.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

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
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print(__FUNCSIG__, " ", EnumToString((ENUM_DEINIT_REASON)reason));
}
//+------------------------------------------------------------------+
