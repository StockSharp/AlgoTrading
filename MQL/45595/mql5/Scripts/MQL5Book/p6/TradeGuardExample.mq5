//+------------------------------------------------------------------+
//|                                            TradeGuardExample.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Open pending order if not exists. As soon as it's triggered into |
//| position, close the position. Normally, order or position        |
//| should exist, no more than one of either kind. Yet if trade      |
//| environment is not synced, OrdersTotal() may report 0 (order is  |
//| already filled), but position is not yet listed in               |
//| PositionsTotal(). This is why we need the guard.                 |
//+------------------------------------------------------------------+
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/TradeGuard.mqh>

// uncomment this define to run without the guard: duplicated positions are possible
// #define SKIP_TRADE_GUARD

#ifndef SKIP_TRADE_GUARD
TradeGuard guard;
#endif

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE) != ACCOUNT_TRADE_MODE_DEMO)
   { 
      Alert("This is a test script! Run it on a DEMO account only!");
      return;
   }

   // 2 positions, 2 orders, or order and position is a broken state: stop the script
   while(!IsStopped() && (OrdersTotal() + PositionsTotal() < 2))
   {
      #ifndef SKIP_TRADE_GUARD
      if(guard.waitsync()) // active orders, history and positions are consistent (in sync)
      #endif
      {
         MqlTradeRequestSync trade;
         if(PositionsTotal() == 1)
         {
            trade.close(PositionGetTicket(0)); // close position if it exists
         }
         else if(!OrdersTotal()) // neither position nor order does exist
         {
            // create a pending order
            const ulong ticket = trade.buyLimit(0.01, SymbolInfoDouble(_Symbol, SYMBOL_ASK));
            #ifndef SKIP_TRADE_GUARD
            if(ticket) guard.push(ticket); // add the order into environment to control
            #endif
         }
         Sleep(100); // adjust for acceptable CPU load
      }
      #ifndef SKIP_TRADE_GUARD
      else // on timeout some error handling is required
      {
         guard.pop(); // here we just drop problematic order from the guard
      }
      #endif
   }
}
//+------------------------------------------------------------------+
/*
   The guard sporadically outputs:
   
   2022.08.24 14:03:25.552  Order is missing: 1437363765
   2022.08.24 14:03:25.661  Order found (position opened): 1437363765 (1437363765)
   
*/