//+------------------------------------------------------------------+
//|                                       RenkoLineBreakVsRSI_EA.mq5 |
//|                                            Copyright 2013, Rone. |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone."
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
#property description "RenkoLineBreak vs RSI expert"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
#include <Trade\Trade.mqh>
#include <Expert\Money\MoneyFixedLot.mqh>
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum MM_MODE {
   FIXED_LOT,     // Fixed lot
   FIXED_PERCENT  // Fixed percentage
};
//---
enum ENUM_TREND_MODE {
   DOWN = -2,
   TO_DOWN =-1,
   NO_TREND = 0,
   TO_UP = 1,
   UP = 2
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
input uint     InpMinBoxSize = 500;       // Min Box Size
input uint     InpRsiPeriod = 4;          // RSI Period
input uint     InpRsiVShift = 20;         // RSI Vertical Shift
input uint     InpTakeProfit = 1000;      // Take Profit
input uint     InpIndentFromHL = 50;      // Indent From High/Low
input double   InpVolume = 1.0;           // Volume
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double         box_size;
double         rsi_vshift;
double         take_profit;
double         indent;

int            rlb_handle;
int            rsi_handle;
int            rsi_period;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
//---
   box_size = InpMinBoxSize;
   indent = InpIndentFromHL * _Point;
   take_profit = InpTakeProfit * _Point;
   rsi_vshift = InpRsiVShift;
   rsi_period = (int)InpRsiPeriod;
//---
   ResetLastError();
   rlb_handle = iCustom(_Symbol, _Period, "RenkoLineBreak", box_size);
   if ( rlb_handle == INVALID_HANDLE ) {
      Print("Creating RenkoLineBreak failed. Error #", GetLastError());
      return(-1);
   }
   rsi_handle = iRSI(_Symbol, _Period, rsi_period, PRICE_CLOSE);
   if ( rsi_handle == INVALID_HANDLE ) {
      Print("Creating RSI failed. Error #", GetLastError());
      return(-1);
   }  
//---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
//---
   IndicatorRelease(rlb_handle);
   IndicatorRelease(rsi_handle);
//---  
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
datetime GetCurrentTime() {
//---
   datetime time[1];
   
   ResetLastError();
   if ( CopyTime(_Symbol, _Period, 0, 1, time) != 1 ) {
      Print(__FUNCTION__, ": getting time data failed. Error #", GetLastError());
      return((datetime)0);
   }
//---
   return(time[0]);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool GetTriggerPrices(ENUM_ORDER_TYPE type, double &open, double &stop) {
//---
   double high[], low[];
   
   ResetLastError();
   if ( CopyHigh(_Symbol, _Period, 1, 3, high) == 3
      && CopyLow(_Symbol, _Period, 1, 3, low) == 3 )
   {
      if ( type == ORDER_TYPE_BUY_STOP ) {
         open = high[2];
         stop = low[ArrayMinimum(low)];
         return(true);
      }
      if ( type == ORDER_TYPE_SELL_STOP ) {
         open = low[2];
         stop = high[ArrayMaximum(high)];
         return(true);
      }
   } else {
      Print(__FUNCTION__, ": getting high and/or low data failed. Error #", GetLastError());
   }
//---
   return(false);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool OpenPendingOrder(ENUM_ORDER_TYPE type, double open, double stop) {
//---
   MqlTick last_tick;
   
   ResetLastError();
   if ( !SymbolInfoTick(_Symbol, last_tick) ) {
      Print(__FUNCTION__, ": getting tick data failed. Error #", GetLastError());
      return(false);
   }
//---
   double target = 0.0;
   double spread = last_tick.ask - last_tick.bid;
   
   if ( type == ORDER_TYPE_BUY_STOP ) {
      open += indent + spread;
      stop -= indent;
      target = open + take_profit;
   } else if ( type == ORDER_TYPE_SELL_STOP ) {
      open -= indent;
      stop += indent + spread;
      target = open - take_profit;
   }
//---   
   CTrade trade;
   double lot = InpVolume;
   
   if ( !trade.OrderOpen(_Symbol, type, lot, open, open, stop, target) ) {
      Print(__FUNCTION__, ": opening order failed. Error #", GetLastError());
      return(false);
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool DeleteAllOrders() {
//---
   for ( int ord = 0; ord < OrdersTotal(); ord++ ) {
      ulong ticket;
      
      if ( (ticket = OrderGetTicket(ord)) > 0 ) {
         if ( OrderGetString(ORDER_SYMBOL) == _Symbol ) {
            CTrade trade;
            
            if ( !trade.OrderDelete(ticket) ) {
               Print(__FUNCTION__, ": deleting order failed. Error #", GetLastError());
               return(false);
            }
         }
      }
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double RsiValue() {
//---
   double rsi[2];
   
   ResetLastError();
   if ( CopyBuffer(rsi_handle, 0, 0, 2, rsi) == 2 ) {
      return(rsi[0]);
   } else {
      Print(__FUNCTION__, ": getting RSI data failed. Error #", GetLastError());
   }
//---
   return(-1.0);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int RlbTrend() {
//---
   double rlb[3];
   int trend = NO_TREND;
   
   ResetLastError();
   if ( CopyBuffer(rlb_handle, 2, 0, 3, rlb) == 3 ) {
      if ( rlb[1] > 0.0 ) {
         if ( rlb[0] < 0.0 ) {
            trend = TO_UP;
         } else {
            trend = UP;
         }
      } else if ( rlb[1] < 0.0 ) {
         if ( rlb[0] > 0.0 ) {
            trend = TO_DOWN;
         } else {
            trend = DOWN;
         }
      }
   } else {
      Print(__FUNCTION__, " getting RenloLineBreak data failed. Error #", GetLastError());
   }
   return(trend);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckOrder() {
//---
   datetime cur_time = GetCurrentTime();
   static datetime prev_time;
   
   if ( cur_time == prev_time ) {
      return;
   }
//---
   int trend = RlbTrend();
   double rsi_value = RsiValue();
   ENUM_ORDER_TYPE type = WRONG_VALUE;
   
   if ( trend == TO_UP || trend == TO_DOWN ) {
      if ( !DeleteAllOrders() ) {
         return;
      }
   }
   
   if ( trend == UP && rsi_value < 50.0 - rsi_vshift && rsi_value >= 0.0 ) {
      type = ORDER_TYPE_BUY_STOP;
   } else if ( trend == DOWN && rsi_value > 50.0 + rsi_vshift ) {
      type = ORDER_TYPE_SELL_STOP;
   }
   
   if ( type != WRONG_VALUE ) {
      if ( DeleteAllOrders() ) {
         double open_price, stop_price; 
         
         if ( GetTriggerPrices(type, open_price, stop_price) ) {
            if ( !OpenPendingOrder(type, open_price, stop_price) ) {
               Print(__FUNCTION__, ": opening order failed. Error #", GetLastError());
               return;
            }
         }
      }
   }
//---
   prev_time = cur_time;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckPosition() {
//---
   datetime cur_time = GetCurrentTime();
   static datetime prev_time;
   
   if ( cur_time == prev_time ) {
      return;
   }
//---
   long pos_type = PositionGetInteger(POSITION_TYPE);
   int trend = RlbTrend();
   double rsi_value = RsiValue();
   bool close_condition = false;
   
   if ( pos_type == POSITION_TYPE_BUY ) {
      if ( trend == TO_DOWN || rsi_value > 50.0 + rsi_vshift ) {
         close_condition = true;
      }
   } else if ( pos_type == POSITION_TYPE_SELL ){
      if ( trend == TO_UP || (rsi_value >= 0.0 && rsi_value < 50.0 - rsi_vshift) ) {
         close_condition = true;
      }
   }
//---
   if ( close_condition ) {
      CTrade trade;
      
      if ( !trade.PositionClose(_Symbol) ) {
         Print(__FUNCTION__, ": closing position failed. Error #", GetLastError());
         return;
      }
   }
//---
   prev_time = cur_time;
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick() {
//---
   if ( PositionSelect(_Symbol) ) {
      CheckPosition();
   } else {
      CheckOrder();
   }
//---   
}
//+------------------------------------------------------------------+
