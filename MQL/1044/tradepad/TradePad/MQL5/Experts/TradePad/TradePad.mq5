//+------------------------------------------------------------------+
//|                                                     TradePad.mq5 |
//|                                                  2011, KTS Group |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "2011, KTS Group"
#property version   "1.00"

#include "TradePanel.mqh"
#include "Resources.mqh"
#include <Trade\SymbolInfo.mqh>

CTradePanel *UserPanel=NULL;
CSymbolInfo *Info=NULL;
CTradePadResources *rs_TradePad=NULL;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(UserPanel==NULL)
     {
      if((UserPanel=new CTradePanel)==NULL) return(-1);

      int  attempts=0;
      int  g_limit_attempts=10;
      bool g_timer=false;

      while(attempts++<g_limit_attempts)
        {
         if(ChartGetInteger(0,CHART_VISIBLE_BARS)>0)
           {
            g_timer=true;
            break;
           }
         Sleep(1000);
        }

      if(g_timer)
        {
         if((rs_TradePad=new CTradePadResources)==NULL)
           {
            Print("Create resource object error");
            return(-1);
           }
         if(!UserPanel.CreateForm("frmMain",rs_TradePad))
           {
            Print("Create form error");
            return(-1);
           }
         if(Info==NULL)
           {
            if((Info=new CSymbolInfo)==NULL) return(-1);
           }
         UserPanel.OnInit(Info);
         EventSetTimer(1);
        }
      else
        {
         Print("Timer not initialized.");
         return(-1);
        }
     }

   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   if(rs_TradePad!=NULL)
     {
      delete rs_TradePad;
      rs_TradePad=NULL;
     }

   if(UserPanel!=NULL)
     {
      delete UserPanel;
      UserPanel=NULL;
     }

   if(Info!=NULL)
     {
      delete Info;
      Info=NULL;
     }
   EventKillTimer();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   UserPanel.OnTick();
  }
//+------------------------------------------------------------------+
//| Expert events function                                           |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,const long &lparam,const double &dparam,const string &sparam)
  {
   UserPanel.OnChartEvent(id,lparam,dparam,sparam);
  }
//+------------------------------------------------------------------+
//| Expert timer function                                            |
//+------------------------------------------------------------------+
void OnTimer()
  {
   UserPanel.OnTimer();
  }
//+------------------------------------------------------------------+
//| Expert trade events function                                     |
//+------------------------------------------------------------------+
void OnTrade()
  {
   UserPanel.OnTrade();
  }
//+------------------------------------------------------------------+
