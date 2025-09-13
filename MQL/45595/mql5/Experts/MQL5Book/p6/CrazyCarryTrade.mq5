//+------------------------------------------------------------------+
//|                                              CrazyCarryTrade.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Keep open positions in predefined direction, collect swaps and withdraw them."

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/MqlTradeSync.mqh>

enum ENUM_ORDER_TYPE_MARKET
{
   MARKET_BUY = ORDER_TYPE_BUY,
   MARKET_SELL = ORDER_TYPE_SELL
};

input ENUM_ORDER_TYPE_MARKET Type;
input double Volume;
input double MinProfitPerLot = 1000;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(!MQLInfoInteger(MQL_TESTER))
   {
      Alert("This is a test EA! Run it in the tester only!");
      return INIT_FAILED;
   }
   
   const double rate = SymbolInfoDouble(_Symbol,
      Type == MARKET_BUY ? SYMBOL_SWAP_LONG : SYMBOL_SWAP_SHORT);
   if(rate < 0)
   {
      Alert("Unprofitable symbol and direction specified!");
      return INIT_FAILED;
   }
   
   PRTF(TesterWithdrawal(AccountInfoDouble(ACCOUNT_BALANCE) * 2));
   /*
   example:
   not enough money for 20 000.00 withdrawal (free margin: 10 000.00)
   TesterWithdrawal(AccountInfoDouble(ACCOUNT_BALANCE)*2)=false / MQL_ERROR::10019(10019)
   */
   PRTF(TesterWithdrawal(100));
   /*
   example:
   deal #2 balance -100.00 [withdrawal] done
   TesterWithdrawal(100)=true / ok
   */
   PRTF(TesterDeposit(100)); // restore
   /*
   example:
   deal #3 balance 100.00 [deposit] done
   TesterDeposit(100)=true / ok
   */

   return INIT_SUCCEEDED;
}
//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   const double volume = Volume == 0 ? SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN) : Volume;
   ENUM_POSITION_PROPERTY_DOUBLE props[] = {POSITION_PROFIT, POSITION_SWAP};
   double values[][2];
   ulong tickets[];
   PositionFilter pf;
   pf.select(props, tickets, values, true);
   if(ArraySize(tickets) > 0)
   {
      double loss = 0, swaps = 0;
      for(int i = 0; i < ArraySize(tickets); ++i)
      {
         if(values[i][0] + values[i][1] * values[i][1] >= MinProfitPerLot * volume)
         {
            MqlTradeRequestSync request0;
            if(request0.close(tickets[i]) && request0.completed())
            {
               swaps += values[i][1]; // sum up swaps of positions being closed
            }
         }
         else
         {
            loss += values[i][0];
         }
      }
      
      if(loss / ArraySize(tickets) <= -MinProfitPerLot * volume * sqrt(ArraySize(tickets)))
      {
         MqlTradeRequestSync request1;
         (Type == MARKET_BUY ? request1.buy(volume) : request1.sell(volume));
      }

      if(swaps >= 0)
      {
         TesterWithdrawal(swaps); // withdraw collected swaps
      }
   }
   else
   {
      MqlTradeRequestSync request1;
      (Type == MARKET_BUY ? request1.buy(volume) : request1.sell(volume));
   }
}

//+------------------------------------------------------------------+
//| Finalization handler                                             |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   PrintFormat("Deposit: %.2f Withdrawals: %.2f",
      TesterStatistics(STAT_INITIAL_DEPOSIT),
      TesterStatistics(STAT_WITHDRAWAL));
   /*
   example:
   final balance 10091.19 USD
   Deposit: 10000.00 Withdrawals: 197.42
   */
}
//+------------------------------------------------------------------+
