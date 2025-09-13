//+------------------------------------------------------------------+
//|                                     Informative Dashboard EA.mq5 |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, MetaQuotes Ltd."
#property link      "https://www.udemy.com/course/create-design-stunning-dashboards-and-panels-for-metatrader-5"
#property description "Learn how to create complex panels with buttons, input fiels and much more using the link provided"
#property version   "1.00"

#include <Informative Dashboard.mqh>

CInformativeDashboard dashboard;

input int x1_ = 20;
input int y1_ = 20;
input int x2_ = 300;
input int y2_ = 200;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   
   dashboard.CreateDashboard("Informative Dashboard",x1_, y1_, x2_, y2_);
   
   dashboard.Run();
   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
     dashboard.Destroy(reason);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
      
    dashboard.RefreshValues();  
   
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,const long& lparam,const double& dparam,const string& sparam)
  {
    dashboard.ChartEvent(id, lparam, dparam, sparam);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
