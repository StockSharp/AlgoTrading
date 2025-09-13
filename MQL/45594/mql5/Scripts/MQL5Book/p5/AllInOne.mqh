//+------------------------------------------------------------------+
//|                                                     AllInOne.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Uninit.mqh>

// detect and output actual program type
const string type = PRTF(EnumToString((ENUM_PROGRAM_TYPE)MQLInfoInteger(MQL_PROGRAM_TYPE)));

//+------------------------------------------------------------------+
//| Simple class to trap uninitialization reason                     |
//+------------------------------------------------------------------+
class Finalizer
{
   static const Finalizer f;
public:
   ~Finalizer()
   {
      PRTF(EnumToString((ENUM_DEINIT_REASON)UninitializeReason()));
   }
};

static const Finalizer Finalizer::f;

//+------------------------------------------------------------------+
//| Event handler for indicator/expert start-up                      |
//+------------------------------------------------------------------+
int OnInit()
{
   Print(__FUNCTION__);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Stub of event handler for script/service start-up                |
//| NB: enable '#define _OnStart OnStart' to use as event handler    |
//+------------------------------------------------------------------+
void _OnStart()
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Event handler for indicator/expert finalization                  |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print(__FUNCTION__, " ", EnumToString((ENUM_DEINIT_REASON)reason));
}

//+------------------------------------------------------------------+
//| Event handler for new tick in an expert adviser                  |
//+------------------------------------------------------------------+
void OnTick()
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Handler of timer events in indicator/expert                      |
//+------------------------------------------------------------------+
void OnTimer()
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Handler for trading events in expert adviser                     |
//+------------------------------------------------------------------+
void OnTrade()
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Handler for trading events (with details) in expert adviser      |
//+------------------------------------------------------------------+
void OnTradeTransaction( 
   const MqlTradeTransaction &trans,
   const MqlTradeRequest &request,
   const MqlTradeResult &result)
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Handler of market book events in indicator/expert                |
//+------------------------------------------------------------------+
void OnBookEvent(const string& symbol)
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Handler of chart events in indicator/expert                      |
//+------------------------------------------------------------------+
void OnChartEvent(
   const int id,
   const long &lparam,
   const double &dparam,
   const string &sparam)
{
   Print(__FUNCTION__);
}

//+------------------------------------------------------------------+
//| Stub of event handler for [re]calculation of indicator (form #1) |
//| NB: enable '#define _OnCalculate1 OnCalculate' to use as handler |
//+------------------------------------------------------------------+
int _OnCalculate1(
   const int rates_total,
   const int prev_calculated,
   const int begin,
   const double &price[])
{
   Print(__FUNCTION__);
   return 0;
}

//+------------------------------------------------------------------+
//| Stub of event handler for [re]calculation of indicator (form #2) |
//| NB: enable '#define _OnCalculate1 OnCalculate' to use as handler |
//+------------------------------------------------------------------+
int _OnCalculate2(
   const int rates_total,
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
   Print(__FUNCTION__);
   return 0;
}

//+------------------------------------------------------------------+
//| Handler of after-testing event for expert adviser                |
//| (another tester-related events exist but ommitted here)          |
//+------------------------------------------------------------------+
double OnTester()
{
   Print(__FUNCTION__);
   return 0;
}

//+------------------------------------------------------------------+
