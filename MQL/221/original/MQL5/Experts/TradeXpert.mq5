//+------------------------------------------------------------------+
//|                                                   TradeXpert.mq5 |
//+------------------------------------------------------------------+

#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"
#property version   "1.00"

#include <TheXpert/TradePane.mqh>
#include <TheXpert/PaneSettings.mqh>
#include <TheXpert/InfoPane.mqh>
#include <TheXpert/Commenter.mqh>

TradePane Pane;
InfoPane Info;
Commenter Comments;

int OnInit()
{
   Pane.Init();
   Info.Show(false);
   
   EventSetTimer(1);
   
   ChartRedraw();
   
   return(0);
}

void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   Pane.OnChartEvent(id, lparam, dparam, sparam);
   Info.OnChartEvent(id, lparam, dparam, sparam);
   Comments.OnChartEvent(id, lparam, dparam, sparam);
   
   ChartRedraw();
}

void OnDeinit(const int reason)
{
   EventKillTimer();
}

void OnTimer()
{
   Info.OnTimer();

   ChartRedraw();
}

void OnTick()
{
   Pane.OnTick();
   Info.OnTick();
   Comments.OnTick();

   ChartRedraw();
}