//+------------------------------------------------------------------+
//|                                                  DailyProfit.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2022, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

void DayProfit()
  {
   double dayprof = 0.0;
   datetime end = TimeCurrent();
   string sdate = TimeToString (TimeCurrent(), TIME_DATE);
   datetime start = StringToTime(sdate);

   HistorySelect(start,end);
   int TotalDeals = HistoryDealsTotal();

   for(int i = 0; i < TotalDeals; i++)
     {
      ulong Ticket = HistoryDealGetTicket(i);

      if(HistoryDealGetInteger(Ticket,DEAL_ENTRY) == DEAL_ENTRY_OUT)
        {
         double LatestProfit = HistoryDealGetDouble(Ticket, DEAL_PROFIT);
         dayprof += LatestProfit;
        }
     }
   Print("DAY PROFIT: ", dayprof);
  }


int OnInit()
  {

   DayProfit();      
      
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+
