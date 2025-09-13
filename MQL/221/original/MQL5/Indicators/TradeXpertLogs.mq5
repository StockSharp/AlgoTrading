//+------------------------------------------------------------------+
//|                                               TradeXpertLogs.mq5 |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"
#property version   "1.00"
#property indicator_separate_window
#property indicator_plots 0

#include <TheXpert/Commenter.mqh>

int OnInit()
{
   IndicatorSetString(INDICATOR_SHORTNAME, COMMENTS_INDIE_TRIGGER_NAME);
   
   return(0);
}

int OnCalculate(const int rates_total,
                const int prev_calculated,
                const datetime& time[],
                const double& open[],
                const double& high[],
                const double& low[],
                const double& close[],
                const long& tick_volume[],
                const long& volume[],
                const int& spread[])
{
   return(rates_total);
}
