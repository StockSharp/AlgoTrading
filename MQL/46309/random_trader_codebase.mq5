//+------------------------------------------------------------------+
//|                                      random_trader.mq5             |
//|                                                 SalmanSoltaniyan   |
//|                                             https://www.mql5.com   |
//+------------------------------------------------------------------+
#property copyright "SalmanSoltaniyan"
#property link      "https://www.mql5.com/en/users/salmansoltaniyan"
#property version   "1.01"
#property description "Need more help? Contact me on https://www.mql5.com/en/users/salmansoltaniyan"

//--- Include required files
#include <Trade\Trade.mqh>

//+------------------------------------------------------------------+
//| Validation class for trading operations                           |
//+------------------------------------------------------------------+
class CTradeValidation
  {
private:
   string            symbol;
   double            point;
  // double            min_stop_level;
   double            pip_point;
   double            stop_level;    // Added stop level
   double            freeze_level;  // Added freeze level

public:
   //--- Constructor
                     CTradeValidation(string _symbol, bool use_max_level = false)
     {
      symbol = _symbol;
      point = SymbolInfoDouble(symbol, SYMBOL_POINT);
      
      //--- Initialize pip value based on symbol digits
      int digits = (int)SymbolInfoInteger(symbol, SYMBOL_DIGITS);
      if(digits == 2 || digits == 3) // JPY pairs or crypto
         pip_point = point;
      else
         if(digits == 4 || digits == 5) // Standard Forex pairs
            pip_point = point * 10;
            
      //--- Initialize stop level and freeze level from symbol
      freeze_level = SymbolInfoInteger(symbol, SYMBOL_TRADE_FREEZE_LEVEL) * point+ (10 * point);;
      stop_level = SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL) * point+ (10 * point);;
   
      //--- If use_max_level is true, set freeze level to max of freeze level and stop level
      if(use_max_level)
      {
         freeze_level = MathMax(freeze_level, stop_level);
      stop_level = freeze_level;
      }
      
     }

   //--- Get stop level
   double            GetStopLevel() { return stop_level; }
   
   //--- Get freeze level
   double            GetFreezeLevel() { return freeze_level; }


   //--- Calculate and validate lot size
   double            CalculateValidLot(double sl_price, double entry_price, double risk_percent)
     {
      double sl_points = MathAbs(sl_price - entry_price) / point;
      double tick_value = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE);
      double min_lot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
      //double max_lot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
      double max_lot=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_LIMIT);
      if(max_lot==0)
         max_lot=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
      double lot_step = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);

      //--- Calculate lot size based on risk
      double lot_size = (AccountInfoDouble(ACCOUNT_BALANCE) * risk_percent / 100) / (sl_points * tick_value);

      //--- Round lot size to symbol lot step
      lot_size = MathFloor(lot_size / lot_step) * lot_step;

      //--- Validate lot size
      if(lot_size < min_lot)
         lot_size = min_lot;
      if(lot_size > max_lot)
         lot_size = max_lot;

      return lot_size;
     }

   //--- Check if there is enough margin for the position
   bool              CheckMargin(double &lot_size, ENUM_ORDER_TYPE order_type, bool use_max_margin0 = true)
     {
      //--- Get required margin for the position
      double margin_required;
      double price = (order_type == ORDER_TYPE_BUY) ?
                     SymbolInfoDouble(symbol, SYMBOL_ASK) :
                     SymbolInfoDouble(symbol, SYMBOL_BID);

      if(!OrderCalcMargin(order_type, symbol, lot_size, price, margin_required))
        {
         return false;
        }

      //--- Get available margin
      double free_margin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);

      //--- Add 10% buffer to required margin for safety
      margin_required *= 1.1;

      //--- Check if we have enough margin
      if(free_margin < margin_required)
        {
         if(use_max_margin0)
           {
            //--- Calculate maximum possible lot size based on available margin
            double max_lot = CalculateMaxLot(order_type, free_margin);
            if(max_lot > 0)
              {
               lot_size = max_lot;
               return true;
              }
           }
         return false;
        }

      return true;
     }

   //--- Calculate maximum possible lot size based on available margin
   double            CalculateMaxLot(ENUM_ORDER_TYPE order_type, double available_margin)
     {
      double price = (order_type == ORDER_TYPE_BUY) ?
                     SymbolInfoDouble(symbol, SYMBOL_ASK) :
                     SymbolInfoDouble(symbol, SYMBOL_BID);

      double min_lot = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN);
      double max_lot=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_LIMIT);
      if(max_lot==0)
         max_lot=SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX);
      double lot_step = SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP);

      //--- Calculate maximum possible lot size
      double margin_per_lot;
      if(!OrderCalcMargin(order_type, symbol, 1.0, price, margin_per_lot))
        {
         return 0;
        }

      //--- Calculate maximum lot size (with 10% buffer)
      double max_possible_lot = (available_margin / 1.1) / margin_per_lot;

      //--- Round to lot step
      max_possible_lot = MathFloor(max_possible_lot / lot_step) * lot_step;

      //--- Validate against symbol limits
      if(max_possible_lot < min_lot)
         return 0;
      if(max_possible_lot > max_lot)
         max_possible_lot = max_lot;

      return max_possible_lot;
     }

   //--- Get pip point value
   double            GetPipPoint() { return pip_point; }

 

   //--- Normalize price to tick size
   double            Round2Ticksize(double price)
     {
      double tick_size = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
      return (MathRound(price / tick_size) * tick_size);
     }
  //--- Validate placing a position
   bool              ValidateOrderPlacement(ENUM_ORDER_TYPE order_type, double sl_price, double tp_price, double open_price=0)
     {
      //--- Normalize prices
      open_price = Round2Ticksize(open_price);
      sl_price = Round2Ticksize(sl_price);
      tp_price = Round2Ticksize(tp_price);
      
      //--- Get current market prices
      double bid = SymbolInfoDouble(symbol, SYMBOL_BID);
      double ask = SymbolInfoDouble(symbol, SYMBOL_ASK);
      
      //--- Validate based on order type
      switch(order_type) {
         case ORDER_TYPE_BUY:
            // For market orders, use bid/ask instead of open price
            return (bid - sl_price >= stop_level && tp_price - bid >= stop_level);
            
         case ORDER_TYPE_SELL:
            // For market orders, use bid/ask instead of open price
            return (sl_price - ask >= stop_level && ask - tp_price >= stop_level);
            
         case ORDER_TYPE_BUY_LIMIT:
            return (ask - open_price >= stop_level && 
                   open_price - sl_price >= stop_level && 
                   tp_price - open_price >= stop_level);
            
         case ORDER_TYPE_SELL_LIMIT:
            return (open_price - bid >= stop_level && 
                   sl_price - open_price >= stop_level && 
                   open_price - tp_price >= stop_level);
            
         case ORDER_TYPE_BUY_STOP:
            return (open_price - ask >= stop_level && 
                   open_price - sl_price >= stop_level && 
                   tp_price - open_price >= stop_level);
            
         case ORDER_TYPE_SELL_STOP:
            return (bid - open_price >= stop_level && 
                   sl_price - open_price >= stop_level && 
                   open_price - tp_price >= stop_level);
      }
      
      return false;
     }
   //--- Validate SL modification
   bool              ValidateSLModify(ENUM_ORDER_TYPE order_type, double old_sl, double new_sl, double open_price = 0)
     {
         /*
      stopLoss/TakeProfit of a pending order cannot be placed closer to the requested order open price than at the minimum distance StopLevel.
      The positions of StopLoss and TakeProfit of pending orders are not limited by the freeze distance FreezeLevel.
      Market Orders StopLoss and TakeProfit cannot be placed closer to the market price than at the minimum distance.
      Market  order cannot be modified, if the execution price of its StopLoss or TakeProfit ranges within the freeze distance from the market price.
      
      */ 
      //--- Normalize prices
      old_sl = Round2Ticksize(old_sl);
      new_sl = Round2Ticksize(new_sl);
      open_price = Round2Ticksize(open_price);
      
      //--- Get current market prices
      double bid = SymbolInfoDouble(symbol, SYMBOL_BID);
      double ask = SymbolInfoDouble(symbol, SYMBOL_ASK);
      
      //--- First validate old SL (only for market orders)
      if(order_type == ORDER_TYPE_BUY || order_type == ORDER_TYPE_SELL) {
         if(order_type == ORDER_TYPE_BUY) {
            if(bid - old_sl <= freeze_level) return false;
         }
         else if(order_type == ORDER_TYPE_SELL) {
            if(old_sl - ask <= freeze_level) return false;
         }
      }
      
      //--- Then validate new SL based on order type
      switch(order_type) {
         case ORDER_TYPE_BUY:
            return (bid - new_sl >= stop_level);
            
         case ORDER_TYPE_SELL:
            return (new_sl - ask >= stop_level);
            
         case ORDER_TYPE_BUY_LIMIT:
         case ORDER_TYPE_BUY_STOP:
            return (open_price - new_sl >= stop_level);
            
         case ORDER_TYPE_SELL_LIMIT:
         case ORDER_TYPE_SELL_STOP:
            return (new_sl - open_price >= stop_level);
      }
      
      return false;
     }

  };
//+------------------------------------------------------------------+

//--- Create trade object
CTrade mytrade;

//--- Create validation object
CTradeValidation *validation;

//--- Enums
enum ENUM_LOSS {
   ATR,    // ATR-based stop loss
   PIP     // Fixed pip-based stop loss
};

//--- Input parameters
input double reward_risk_ratio = 2;           // Reward/Risk ratio
input ENUM_LOSS loss = PIP;                   // Loss based on distance(PIP) or ATR
input double loss_atr = 5;                    // ATR multiplier for stop loss
input double loss_pip = 20;                   // Fixed pip distance for stop loss
input double risk_percent_per_trade = 1;      // Risk percent per trade
input bool use_breakeven = true;              // Use breakeven mechanism
input double breakeven_distance = 10;         // Distance in pips to activate breakeven
input bool use_max_margin = true;             // Use maximum available margin if required margin is not enough

//--- Global variables
ulong tiket = 0;                              // Position ticket
double sl_distance;                           // Stop loss distance
double tp_distance;                           // Take profit distance
int atr_handle;                               // ATR indicator handle
double atr_array[];                           // ATR values array
bool breakeven_activated = false;             // Breakeven status flag

//+------------------------------------------------------------------+
//| Expert initialization function                                     |
//+------------------------------------------------------------------+
int OnInit() {
   //--- Validate input parameters
   if(loss_atr <= 0) {
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   if(loss_pip <= 0) {
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   if(reward_risk_ratio <= 0) {
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   if(risk_percent_per_trade <= 0) {
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   if(use_breakeven && breakeven_distance <= 0) {
      return(INIT_PARAMETERS_INCORRECT);
   }
   
   //--- Create validation object
   validation = new CTradeValidation(_Symbol);
   
   //--- Initialize stop loss and take profit distances based on selected mode
   if(loss == PIP) {
      sl_distance = loss_pip * validation.GetPipPoint();
      tp_distance = sl_distance * reward_risk_ratio;
   }
   if(loss == ATR) {
      atr_handle = iATR(_Symbol, PERIOD_CURRENT, 10);
      ArraySetAsSeries(atr_array, true);
   }
   
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                   |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
   //--- Clean up ATR handle if it was created
   if(atr_handle != INVALID_HANDLE) IndicatorRelease(atr_handle);
   
   //--- Delete validation object
   if(validation != NULL) delete validation;
}

//+------------------------------------------------------------------+
//| Expert tick function                                               |
//+------------------------------------------------------------------+
void OnTick() {
   //--- Check for breakeven if position exists and breakeven is enabled
   if(PositionsTotal() == 1 && use_breakeven) {
      CheckBreakeven();
   }
   
   //--- Open new position if none exists
   if(PositionsTotal() == 0) {
      OpenNewPosition();
   }
}

//+------------------------------------------------------------------+
//| Check and apply breakeven if conditions are met                    |
//+------------------------------------------------------------------+
void CheckBreakeven() {
   if(!PositionSelectByTicket(tiket)) return;
   
   ENUM_POSITION_TYPE current_position_type = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
   double open_price = PositionGetDouble(POSITION_PRICE_OPEN);
   double current_sl = PositionGetDouble(POSITION_SL);
   double current_tp = PositionGetDouble(POSITION_TP);
   double current_price = (current_position_type == POSITION_TYPE_BUY) ? 
                         SymbolInfoDouble(_Symbol, SYMBOL_BID) : 
                         SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   
   //--- Calculate breakeven activation distance in points using correct pip value
   double breakeven_activation_points = breakeven_distance * validation.GetPipPoint();
   
   //--- Check if breakeven should be activated
   if(!breakeven_activated) {
      if(current_position_type == POSITION_TYPE_BUY) {
         if(current_price - open_price >= breakeven_activation_points) {
            //--- Validate new stop loss distance
            double new_sl = NormalizeDouble(open_price, _Digits);
            ENUM_ORDER_TYPE order_type = (current_position_type == POSITION_TYPE_BUY) ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
            if(validation.ValidateSLModify(order_type, current_sl, new_sl)) {
               mytrade.PositionModify(tiket, new_sl, current_tp);
               breakeven_activated = true;
            }
         }
      }
      else if(current_position_type == POSITION_TYPE_SELL) {
         if(open_price - current_price >= breakeven_activation_points) {
            //--- Validate new stop loss distance
            double new_sl = NormalizeDouble(open_price, _Digits);
            ENUM_ORDER_TYPE order_type = (current_position_type == POSITION_TYPE_BUY) ? ORDER_TYPE_BUY : ORDER_TYPE_SELL;
            if(validation.ValidateSLModify(order_type, current_sl, new_sl)) {
               mytrade.PositionModify(tiket, new_sl, current_tp);
               breakeven_activated = true;
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Open new position                                                  |
//+------------------------------------------------------------------+
void OpenNewPosition() {
   //--- Reset breakeven flag for new position
   breakeven_activated = false;
   
   //--- Update ATR values if using ATR-based stops
   if(loss == ATR) {
      CopyBuffer(atr_handle, 0, 0, 5, atr_array);
      sl_distance = loss_atr * atr_array[0];
      tp_distance = sl_distance * reward_risk_ratio;
   }

   //--- Randomly choose position type
   double rand_buy_sell = MathMod(MathRand(), 2);
   
   if(rand_buy_sell == 0) {  // BUY
      double entry_price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      double sl = entry_price - sl_distance;
      double tp = entry_price + tp_distance;
      
      //--- Validate SL/TP distances
      if(!validation.ValidateOrderPlacement(ORDER_TYPE_BUY, sl, tp)) return;
      
      sl = NormalizeDouble(sl, _Digits);
      tp = NormalizeDouble(tp, _Digits);
      
      //--- Calculate and validate lot size
      double lot0 = validation.CalculateValidLot(sl, entry_price, risk_percent_per_trade);
      if(lot0 > 0) {
         //--- Check if we have enough margin for this position
         if(validation.CheckMargin(lot0, ORDER_TYPE_BUY, use_max_margin)) {
            mytrade.Buy(lot0, _Symbol, entry_price, sl, tp);
            tiket = PositionGetTicket(0);
         }
      }
   }
   else {  // SELL
      double entry_price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      double sl = entry_price + sl_distance;
      double tp = entry_price - tp_distance;
      
      //--- Validate SL/TP distances
      if(!validation.ValidateOrderPlacement(ORDER_TYPE_SELL, sl, tp)) return;
      
      sl = NormalizeDouble(sl, _Digits);
      tp = NormalizeDouble(tp, _Digits);
      
      //--- Calculate and validate lot size
      double lot0 = validation.CalculateValidLot(sl, entry_price, risk_percent_per_trade);
      if(lot0 > 0) {
         //--- Check if we have enough margin for this position
         if(validation.CheckMargin(lot0, ORDER_TYPE_SELL, use_max_margin)) {
            mytrade.Sell(lot0, _Symbol, entry_price, sl, tp);
            tiket = PositionGetTicket(0);
         }
      }
   }
}

//+------------------------------------------------------------------+
//| MIT License                                                       |
//|                                                                  |
//| Copyright (c) 2025 SalmanSoltaniyan                              |
//|                                                                  |
//| Permission is hereby granted, free of charge, to any person       |
//| obtaining a copy of this software and associated documentation    |
//| files (the "Software"), to deal in the Software without          |
//| restriction, including without limitation the rights to use,      |
//| copy, modify, merge, publish, distribute, sublicense, and/or sell |
//| copies of the Software, and to permit persons to whom the        |
//| Software is furnished to do so, subject to the following         |
//| conditions:                                                      |
//|                                                                  |
//| The above copyright notice and this permission notice shall be   |
//| included in all copies or substantial portions of the Software.  |
//|                                                                  |
//| THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,  |
//| EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES  |
//| OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND         |
//| NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT     |
//| HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,    |
//| WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING    |
//| FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR   |
//| OTHER DEALINGS IN THE SOFTWARE.                                 |
//+------------------------------------------------------------------+
