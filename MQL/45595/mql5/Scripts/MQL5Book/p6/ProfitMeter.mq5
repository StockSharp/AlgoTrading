//+------------------------------------------------------------------+
//|                                                  ProfitMeter.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//| Estimate profits/losses for symbols in market watch for          |
//| virtual trades in recent past.                                   |
//+------------------------------------------------------------------+
#property script_show_inputs

input ENUM_ORDER_TYPE Action = ORDER_TYPE_BUY; // Action (only Buy/Sell allowed)
input float Lot = 1;
input int Duration = 20; // Duration (bar number in past)

#include <MQL5Book/MarginProfitMeter.mqh>
#include <MQL5Book/Periods.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // make sure only buy/sell is in action
   ENUM_ORDER_TYPE type = (ENUM_ORDER_TYPE)(Action % 2);
   const string text[] = {"buying", "selling"};
   PrintFormat("Profits/Losses for %s %s lots"
      " of %d symbols in Market Watch on last %d bars %s",
      text[type], (string)Lot, SymbolsTotal(true),
      Duration, PeriodToString(_Period));
   
   for(int i = 0; i < SymbolsTotal(true); i++)
   {
      const string symbol = SymbolName(i, true);
      const double enter = iClose(symbol, _Period, Duration);
      const double exit = iClose(symbol, _Period, 0);
      
      double profit1, profit2; // 2 receiving variables
      
      // built-in variant
      if(!OrderCalcProfit(type, symbol, Lot, enter, exit, profit1))
      {
         PrintFormat("OrderCalcProfit(%s) failed: %d", symbol, _LastError);
         continue;
      }
      
      // custom variant
      const int points = (int)MathRound((exit - enter) / SymbolInfoDouble(symbol, SYMBOL_POINT));
      profit2 = Lot * points * MPM::PointValue(symbol);
      profit2 = NormalizeDouble(profit2, (int)AccountInfoInteger(ACCOUNT_CURRENCY_DIGITS));
      if(type == ORDER_TYPE_SELL) profit2 *= -1;
      
      // output both values to compare
      PrintFormat("%s: %f %f", symbol, profit1, profit2);
   }
}
//+------------------------------------------------------------------+
/*
   example output:

      Profits/Losses for buying 1.0 lots of 13 symbols in Market Watch on last 20 bars H1
      EURUSD: 390.000000 390.000000
      GBPUSD: 214.000000 214.000000
      USDCHF: -254.270000 -254.270000
      USDJPY: -57.930000 -57.930000
      USDCNH: -172.570000 -172.570000
      USDRUB: 493.360000 493.360000
      AUDUSD: 84.000000 84.000000
      NZDUSD: 13.000000 13.000000
      USDCAD: -97.480000 -97.480000
      USDSEK: -682.910000 -682.910000
      XAUUSD: -1706.000000 -1706.000000
      SP500m: 5300.000000 5300.000000
      XPDUSD: -84.030000 -84.030000

*/
//+------------------------------------------------------------------+
