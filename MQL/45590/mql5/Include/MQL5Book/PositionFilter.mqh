//+------------------------------------------------------------------+
//|                                               PositionFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PositionMonitor.mqh>
#include <MQL5Book/TradeFilter.mqh>

class PositionFilter: public TradeFilter<PositionMonitor,
   ENUM_POSITION_PROPERTY_INTEGER,
   ENUM_POSITION_PROPERTY_DOUBLE,
   ENUM_POSITION_PROPERTY_STRING>
{
protected:
   virtual int total() const override
   {
      return PositionsTotal();
   }
   virtual ulong get(const int i) const override
   {
      return PositionGetTicket(i);
   }
};
//+------------------------------------------------------------------+
/*
input ulong Magic;

void OnStart()
{
   PositionFilter filter;
   
   ENUM_POSITION_PROPERTY_DOUBLE properties[] =
      {POSITION_PROFIT, POSITION_VOLUME};
   
   double profits[][2];
   ulong tickets[];
   string symbols[];
   
   filter.let(POSITION_MAGIC, Magic).select(properties, tickets, profits);
   filter.select(POSITION_SYMBOL, tickets, symbols);

   ArrayPrint(profits);
   ArrayPrint(tickets);
   ArrayPrint(symbols);
}
*/
//+------------------------------------------------------------------+
