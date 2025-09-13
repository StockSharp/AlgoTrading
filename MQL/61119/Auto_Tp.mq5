//+------------------------------------------------------------------+
//|                                                 Auto_Tp_MQL5.mq5 |
//|                                                 Mir Mostofa Kamal|
//|                              https://www.mql5.com/en/users/bokul |
//+------------------------------------------------------------------+
#property copyright "Mir Mostofa Kamal"
#property link      "https://www.mql5.com/en/users/bokul"
#property description   "mkbokul@gmail.com"
#property version   "1.00"
#property description "Auto Take profit and stop loss set EA ."
#property description "This EA will work on all time frames. Good for Scalping. It also work on XAUUSD too."
#property description "WARNING: Use this software at your own risk."
#property description "The creator of this script cannot be held responsible for any damage or loss."
#property strict

#include <Trade\Trade.mqh>
CTrade trade;

// Inputs
input string Auto_Tp_Genaral_Settings        = "=== Auto Tp Genaral Settings ===";
input double TakeProfitPips     = 25;     // Take Profit in pips:
input bool   UseStopLoss        = false;  // Enable Stop Loss:
input double StopLossPips       = 12;     // Stop Loss in pips:

input string Auto_Tp_Advance_Settings        = "=== Auto Tp Advance Settings ===";
input bool   UseTrailingStop    = false;  // Enable Trailing Stop:
input double TrailingStopPips   = 15;     // Trailing Stop in pips:
input bool   UseEquityProtection = false; // Enable Equity Protection:
input double MinEquityPercent   = 20.0;   // Use Maximum Equity % Risk:
input int    Slippage           = 3;      //Slip:

int MagicNumber = 2025;                   // Only affect manual trades

//+---------------- On in It --------------------------------------------------+

int OnInit()
  {
   Print("Auto SL/TP Manager EA initialized.");
   return(INIT_SUCCEEDED);
  }
  
  
//+--------------- On Trick ---------------------------------------------------+

void OnTick()
  {
   ManageOpenPositions();
   CheckEquityLimit();
  }
  
//+--------------- Manage Open Positions ---------------------------------------------------+

void ManageOpenPositions()
  {
   double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
   double pip = point * 10;

   if(PositionSelect(_Symbol))
     {
      ulong ticket = PositionGetInteger(POSITION_TICKET);
      int type = (int)PositionGetInteger(POSITION_TYPE);
      double price = PositionGetDouble(POSITION_PRICE_OPEN);
      double sl = PositionGetDouble(POSITION_SL);
      double tp = PositionGetDouble(POSITION_TP);

      double newSL = 0.0, newTP = 0.0;

      if(type == POSITION_TYPE_BUY)
        {
         if(UseStopLoss)
            newSL = NormalizeDouble(price - StopLossPips * pip, _Digits);
         newTP = NormalizeDouble(price + TakeProfitPips * pip, _Digits);
        }
      else
         if(type == POSITION_TYPE_SELL)
           {
            if(UseStopLoss)
               newSL = NormalizeDouble(price + StopLossPips * pip, _Digits);
            newTP = NormalizeDouble(price - TakeProfitPips * pip, _Digits);
           }

      if((UseStopLoss && sl == 0.0) || tp == 0.0)
        {
         if(!UseStopLoss)
            newSL = 0.0;
         trade.PositionModify(_Symbol, newSL, newTP);
        }

      if(UseTrailingStop && UseStopLoss)
         ApplyTrailingStop(type, price, pip);
     }
  }
  
//+--------------- Apply Trailing Stop ---------------------------------------------------+

void ApplyTrailingStop(int type, double openPrice, double pip)
  {
   double sl = PositionGetDouble(POSITION_SL);
   double newSL;

   if(type == POSITION_TYPE_BUY)
     {
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      if(bid - openPrice > TrailingStopPips * pip)
        {
         newSL = NormalizeDouble(bid - TrailingStopPips * pip, _Digits);
         if(sl < newSL)
            trade.PositionModify(_Symbol, newSL, PositionGetDouble(POSITION_TP));
        }
     }
   else
      if(type == POSITION_TYPE_SELL)
        {
         double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
         if(openPrice - ask > TrailingStopPips * pip)
           {
            newSL = NormalizeDouble(ask + TrailingStopPips * pip, _Digits);
            if(sl > newSL || sl == 0.0)
               trade.PositionModify(_Symbol, newSL, PositionGetDouble(POSITION_TP));
           }
        }
  }
  
//+------------- Equity Limit -----------------------------------------------------+

void CheckEquityLimit()
  {
   if(!UseEquityProtection)
      return;

   double balance = AccountInfoDouble(ACCOUNT_BALANCE);
   double equity  = AccountInfoDouble(ACCOUNT_EQUITY);
   double minEquity = balance * MinEquityPercent / 100.0;

   if(equity <= minEquity)
     {
      if(PositionSelect(_Symbol) && PositionGetInteger(POSITION_MAGIC) == MagicNumber)
        {
         if(trade.PositionClose(_Symbol))
            Print("Closed trade due to low equity. Equity: ", equity);
        }
     }
  }
  
//+
//+--------------------- Finish codeing ---------------------------------------------+
