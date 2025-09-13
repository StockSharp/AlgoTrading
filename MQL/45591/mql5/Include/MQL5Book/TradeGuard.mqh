//+------------------------------------------------------------------+
//|                                                   TradeGuard.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Ensure orders (especially pending) are transformed into positions|
//+------------------------------------------------------------------+
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/TradeUtils.mqh>

//+------------------------------------------------------------------+
//| Track given orders (tickets) until they're reflected in positions|
//+------------------------------------------------------------------+
class TradeGuard
{
   ulong tickets[];
public:
   void push(const ulong t)            // add new order to monitor
   {
      if(!check(t)) return;            // do not accept invalid ticket
      if(ArraySize(tickets) > 0)
      {
         const int i = ArrayBsearch(tickets, t);
         if(tickets[i] == t) return;
      }
      PUSH(tickets, t);
      ArraySort(tickets);
   }
   
   int check(const ulong ticket)       // order or position for given order must exist
   {
      static ulong prevmissing = 0;
      if(OrderSelect(ticket)) return 1;
      else if(HistoryOrderGetInteger(ticket, ORDER_TICKET) == ticket
         || HistoryOrderSelect(ticket))
      {
         const ulong id = HistoryOrderGetInteger(ticket, ORDER_POSITION_ID);
         if(id > 0)
         {
            if(TU::PositionSelectById(id))       // existing position
            {
               if(prevmissing == ticket)
               {
                  PrintFormat("Order found (position opened): %ld (%ld)", ticket, id);
                  prevmissing = 0;
               }
               return 2;
            }
            else if(HistorySelectByPosition(id)) // position already closed
            {
               for(int i = 0; i < HistoryDealsTotal(); ++i)
               {
                  const ulong deal = HistoryDealGetTicket(i);
                  if(HistoryDealGetInteger(deal, DEAL_ORDER) == ticket)
                  {
                     if(prevmissing == ticket)
                     {
                        PrintFormat("Order found (position closed): %ld (%ld)", ticket, id);
                        prevmissing = 0;
                     }
                     return 2;
                  }
               }
            }
         }
         else
         {
            const ENUM_ORDER_STATE state = (ENUM_ORDER_STATE)HistoryOrderGetInteger(ticket, ORDER_STATE);
            if(state == ORDER_STATE_CANCELED
               || state == ORDER_STATE_REJECTED
               || state == ORDER_STATE_EXPIRED)
            {
               PrintFormat("Order changed: %ld %s", ticket, EnumToString(state));
               return 2;
            }
         }
      }
      
      // neither order (active or historical), nor position found - usynced state
      if(prevmissing != ticket)
      {
         Print("Order is missing: ", ticket);
         prevmissing = ticket;
      }
      return 0;
   }
   
   ulong unsynced() // check all orders for existence, history record, or transformation into positions
   {
      for(int i = ArraySize(tickets) - 1; i >= 0; --i)
      {
         const int state = check(tickets[i]);
         if(!state) return tickets[i]; // a problem found, return missing order ticket
         else if(state == 2)           // order is in history and bound to position or cancelled
         {
            ArrayRemove(tickets, i, 1);
         }
      }
      return 0;                        // all is in sync
   }
   
   bool waitsync(const uint msc = 1000) // wait until all orders/positions found or timeout
   {
      const uint start = GetTickCount();
      bool result = false;
      while((result = unsynced()) && GetTickCount() - start < msc);
      return !result;
   }
   
   void pop(ulong t = 0) // remove problematic order from the guard
   {
      if(!t) t = unsynced();
      if(t)
      {
         const int i = ArrayBsearch(tickets, t);
         if(tickets[i] == t)
         {
            Print("Order dropped: ", t);
            ArrayRemove(tickets, i, 1);
            return;
         }
      }
   }
   
   void reset() // total clean up
   {
      ArrayResize(tickets, 0);
   }
};
//+------------------------------------------------------------------+
