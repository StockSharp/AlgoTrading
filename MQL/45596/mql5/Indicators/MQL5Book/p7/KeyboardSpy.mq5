//+------------------------------------------------------------------+
//|                                                  KeyboardSpy.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Intercepts interactive keyboard presses and sends notification about them into specified chart 'HostID' using 'EventID' event.\n"
#property description "Allows for control another MQL-programm running on an inactive chart (which are not receiving keyboard events)."
#property indicator_chart_window
#property indicator_plots 0

input long HostID;
input ushort EventID;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   Print("init ", ChartID());
   return INIT_SUCCEEDED;
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
//| Chart event handler                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   if(id == CHARTEVENT_KEYDOWN)
   {
      // NB: MT5 limitation: TerminalInfoInteger(TERMINAL_KEYSTATE_) does not work
      // in indicators created by iCustom/IndicatorCreate, that is
      // the function return 0 always for all keys, so we can't detect
      // Ctrl/Shift and other key states and use symbol alphanumeric keys
      EventChartCustom(HostID, EventID, lparam,
         (double)(ushort)TerminalInfoInteger(TERMINAL_KEYSTATE_CONTROL), // this is always 0 inside iCustom
         sparam);
   }
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   Print("deinit ", ChartID());
}
//+------------------------------------------------------------------+
