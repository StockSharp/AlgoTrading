//+------------------------------------------------------------------+
//|                                             BreakoutStrategy.mq5 |
//|                                        Copyright 2024, QuanAlpha |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, QuanAlpha"
#property version   "1.00"

#include <Trade/Trade.mqh>

//+------------------------------------------------------------------+
//| INPUT PARAMETERS                                                 |
//+------------------------------------------------------------------+
string input aa = "------------------SETTINGS----------------------";
string input BOT_NAME = "BreakoutStrategy";
int input EXPERT_MAGIC = 1;
string input bb = "-------------------ENTRY------------------------";
int input ENTRY_PERIOD = 20;
int input ENTRY_SHIFT  = 1;
string input cc = "--------------------EXIT------------------------";
int input EXIT_PERIOD = 20;
int input EXIT_SHIFT  = 1;
bool input EXIT_MIDDLE_LINE = true; // Use middle line for exit?
string input gg = "----------------RISK PROFILE--------------------";
ENUM_TIMEFRAMES input TRADING_TIMEFRAME = PERIOD_H1;
double input RISK_PER_TRADE = 0.01;

//+------------------------------------------------------------------+
//| GLOBAL VARIABLES                                                 |
//+------------------------------------------------------------------+

// Trade Object
CTrade trade;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   trade.SetExpertMagicNumber(EXPERT_MAGIC);  
   trade.SetDeviationInPoints(10);
   
   printf(BOT_NAME + " initialized!");  
   return(INIT_SUCCEEDED);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   printf(BOT_NAME + " exited, exit code: %d", reason);
}

//+------------------------------------------------------------------+
//| Indicators                                                       |
//+------------------------------------------------------------------+

double HighestHigh(int period, int shift, ENUM_TIMEFRAMES timeframe)
{
    return iHigh(_Symbol, timeframe, iHighest(_Symbol, timeframe, MODE_HIGH, period, shift));
}

double LowestLow(int period, int shift, ENUM_TIMEFRAMES timeframe)
{
   return iLow(_Symbol, timeframe, iLowest(_Symbol, timeframe, MODE_LOW, period, shift));
}

double Middle(int period, int shift, ENUM_TIMEFRAMES timeframe)
{
   return (HighestHigh(period, shift, timeframe) + LowestLow(period, shift, timeframe)) / 2.;
}

double ATR(int period, int shift, ENUM_TIMEFRAMES timeframe)
{
   int handle = iATR(_Symbol, timeframe, period);
   double value[];
   CopyBuffer(handle, 0, shift, 1, value);
   return value[0];
}

//+------------------------------------------------------------------+
//| Manage existing positions                                        |
//+------------------------------------------------------------------+
int PositionManaging(ENUM_POSITION_TYPE position_type)
{
   int positions = 0;
   double trail_long, trail_short;
   double atr = ATR(20, 1, TRADING_TIMEFRAME);
   if (EXIT_MIDDLE_LINE)
   {  
      trail_long = Middle(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME);
      trail_short = trail_long;
   }
   else
   {
      trail_long = LowestLow(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME);
      trail_short = HighestHigh(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME);
   }
   
   for (int i = PositionsTotal() - 1; i >= 0; i--)
   {
      ulong posTicket = PositionGetTicket(i);
      if (PositionSelectByTicket(posTicket))
      {       
          if (PositionGetString(POSITION_SYMBOL) == _Symbol && 
              PositionGetInteger(POSITION_MAGIC) == EXPERT_MAGIC && 
              PositionGetInteger(POSITION_TYPE) == position_type)
          {
              positions = positions + 1;
              double sl = PositionGetDouble(POSITION_SL), tp = PositionGetDouble(POSITION_TP), cp = PositionGetDouble(POSITION_PRICE_CURRENT), op = PositionGetDouble(POSITION_PRICE_OPEN);
              if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
              {             
                  // Trailing stop
                  if (cp < trail_long)
                  {
                     trade.PositionClose(posTicket);
                     positions--;
                  }
                  else if (trail_long > sl + 0.1 * atr && cp - trail_long >= SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL))
                  {
                     trade.PositionModify(posTicket, trail_long, tp);
                  }                   
              }
              else if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
              {                  
                  // Trailing stop
                  if (cp > trail_short)
                  {
                     trade.PositionClose(posTicket);
                     positions--;
                  }
                  else if (trail_short < sl - 0.1 * atr && trail_short - cp >= SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL))
                  {
                     trade.PositionModify(posTicket, trail_short, tp);
                  }                     
              }
          }
      }
   }
   return MathMax(positions, 0);
}

//+------------------------------------------------------------------+
//| Manage orders                                                    |
//+------------------------------------------------------------------+
int OrderManaging()
{
   int orders = 0;
   for (int i = OrdersTotal() - 1; i >= 0; i--)
   {
      ulong orderTicket = OrderGetTicket(i);
      if (OrderSelect(orderTicket)) 
      {
          if (OrderGetString(ORDER_SYMBOL) == _Symbol && OrderGetInteger(ORDER_MAGIC) == EXPERT_MAGIC)
          {
               trade.OrderDelete(orderTicket);
          }
      }
   }
   return orders;
}

//+------------------------------------------------------------------+
//| Calculate lot_sizes based on risk % of equity per trade          |
//+------------------------------------------------------------------+

double CalculateLotSize(double sl_value, double price)
{     
   double lots = 0.0;
   double loss = 0.0;

   double balance=AccountInfoDouble(ACCOUNT_EQUITY);
   double point=SymbolInfoDouble(_Symbol,SYMBOL_POINT);
   double sl_price=price-sl_value;
      
   if (OrderCalcProfit(ORDER_TYPE_BUY,_Symbol,1.0,price,sl_price,loss))
   {
      double lotstep=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_STEP);
      double risk_money=RISK_PER_TRADE*balance;
      double margin = 0;
      lots=risk_money/MathAbs(loss);
      lots=MathFloor(lots/lotstep)*lotstep;
      //--- Adjust lots to broker limits.
      double minlot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
      double maxlot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
      if(lots < minlot)
      {
         lots=minlot;
      }
      if(OrderCalcMargin(ORDER_TYPE_BUY,_Symbol,lots,price,margin))
       {
         double free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
         if(free_margin<0)
           {
            lots=0;
           }
         else if(free_margin<margin)
           {
            lots=lots*free_margin/margin;
            lots=MathFloor(lots/lotstep-1)*lotstep;
           }
        }
   }
   return MathMax(lots, SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN));
}

//+------------------------------------------------------------------+
//| Get current server time function                                 |
//+------------------------------------------------------------------+
bool BarOpen()
{
   static datetime m_prev_bar = TimeCurrent();
   datetime bar_time = iTime(_Symbol, TRADING_TIMEFRAME, 0);
   if (bar_time == m_prev_bar)
   {
      return false;
   }
   
   m_prev_bar = bar_time;
   return true;
}

//+------------------------------------------------------------------+
//| TRADE EXECUTION                                                  |
//+------------------------------------------------------------------+
void ExecuteTrade()
{
   if (BarOpen()) 
   { 
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID), ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      
      double trading_hh = HighestHigh(ENTRY_PERIOD, ENTRY_SHIFT, TRADING_TIMEFRAME);
      double trading_ll = LowestLow(ENTRY_PERIOD, ENTRY_SHIFT, TRADING_TIMEFRAME);
      
      double trigger_long = trading_hh + SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_SIZE);
      double trigger_short = trading_ll - SymbolInfoDouble(_Symbol, SYMBOL_TRADE_TICK_SIZE);
      
      double exit_long = LowestLow(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME); 
      double exit_short = HighestHigh(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME);
      
      if (EXIT_MIDDLE_LINE)
      {
         exit_long = MathMax(Middle(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME), exit_long);
         exit_short = MathMin(Middle(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME), exit_short);
      }
   
      // Delete old orders
      int orders = OrderManaging();
      
      // Manage existing positions
      int long_positions = PositionManaging(POSITION_TYPE_BUY);
      int short_positions = PositionManaging(POSITION_TYPE_SELL);
      
      // Long order
      if (long_positions == 0 && trigger_long - ask >= SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL))
      {
         double sl = trigger_long - exit_long;
         double lot_size = CalculateLotSize(sl, trigger_long);
         double max_volume = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
         while (lot_size > max_volume)
         {
            trade.BuyStop(max_volume, trigger_long, _Symbol, exit_long, 0.0, ORDER_TIME_DAY);
            lot_size -= max_volume;
         }
         trade.BuyStop(lot_size, trigger_long, _Symbol, exit_long, 0.0, ORDER_TIME_DAY);
      }
      
      // Short order
      if (short_positions == 0 && bid - trigger_short >= SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL))
      {     
         double sl = exit_short - trigger_short;
         double lot_size = CalculateLotSize(sl, trigger_short);   
         double max_volume = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
         while (lot_size > max_volume)
         {
            trade.SellStop(max_volume, trigger_short, _Symbol, exit_short, 0.0, ORDER_TIME_DAY);
            lot_size -= max_volume;
         }
         trade.SellStop(lot_size, trigger_short, _Symbol, exit_short, 0.0, ORDER_TIME_DAY);
      }
   }
}

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+

void OnTick()
{  
   ExecuteTrade();
}