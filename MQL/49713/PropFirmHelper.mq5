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
input string aa = "------------------SETTINGS----------------------";
input string BOT_NAME = "Breakout Strategy with Prop Firm Challenge Helper";
input int    EXPERT_MAGIC = 1;
input string bb = "-------------------ENTRY------------------------";
input int    ENTRY_PERIOD = 20;
input int    ENTRY_SHIFT  = 1;
input string cc = "--------------------EXIT------------------------";
input int    EXIT_PERIOD = 20;
input int    EXIT_SHIFT  = 1;
input string dd = "-------------PROP FIRM CHALLENGE-----------------";
input bool   isChallenge = false;
input double PASS_CRITERIA = 110100.;
input double DAILY_LOSS_LIMIT = 4500.;
input string gg = "----------------RISK PROFILE--------------------";
input double RISK_PER_TRADE = 1.0; // Percent of equity per trade
input ENUM_TIMEFRAMES TRADING_TIMEFRAME = PERIOD_H1;

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
//| Expert tick function                                             |
//+------------------------------------------------------------------+

void OnTick()
{  
   if (isChallenge)
   {
      // Check if we passed the challenge
      if (isPassed())
      {
         ClearAll("Prop Firm Challenge Passed!");
      }
      else if (isDailyLimit())
      {
         ClearAll("Daily loss limit exceeded!");
      }
      
   }

   ExecuteTrade();
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

   trail_long = LowestLow(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME);
   trail_short = HighestHigh(EXIT_PERIOD, EXIT_SHIFT, TRADING_TIMEFRAME);

   
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
//| Delete old order                                                 |
//+------------------------------------------------------------------+
int OrderManaging()
{
   int orders = 0;
   for (int i = OrdersTotal() - 1; i >= 0; i--)
   {
      ulong orderTicket = OrderGetTicket(i);
      if (OrderSelect(orderTicket)) 
      {
          if (OrderGetString(ORDER_SYMBOL) == _Symbol && 
              OrderGetInteger(ORDER_MAGIC) == EXPERT_MAGIC)
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
      double risk_money=RISK_PER_TRADE * balance / 100.;
      double margin = 0;
      lots=risk_money/MathAbs(loss);
      lots=MathFloor(lots/lotstep)*lotstep;
      
      // Adjust lots to broker limits.
      double minlot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
      double maxlot=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MAX);
      
      if(lots < minlot)
      {
         lots=minlot;
      }
      
      // Check if available margin is enough to enter a position
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
//| Check if new bar is created                                      |
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
//| Prop Firm Helper Functions                                       |
//+------------------------------------------------------------------+

// Delete all pending orders and exit all positions
void ClearAll(string message)
{
   Comment(message);
   for (int i = OrdersTotal() - 1; i >= 0; i--)
   {
      ulong orderTicket = OrderGetTicket(i);
      if (OrderSelect(orderTicket)) 
      {
         trade.OrderDelete(orderTicket);
      }
   }

   for (int i = PositionsTotal() - 1; i >= 0; i--)
   {
      ulong posTicket = PositionGetTicket(i);
      trade.PositionClose(posTicket);
   }
}

// Check if we have achieved profit target
bool isPassed()
{
   return AccountInfoDouble(ACCOUNT_EQUITY) > PASS_CRITERIA;
}

// Check if we are about to violate maximum daily loss
bool isDailyLimit()
{
   MqlDateTime date_time;
   TimeToStruct(TimeCurrent(), date_time);
   int current_day = date_time.day, current_month = date_time.mon, current_year = date_time.year;
   
   // Current balance
   double current_balance = AccountInfoDouble(ACCOUNT_BALANCE);
   
   // Get today's closed trades PL
   HistorySelect(0, TimeCurrent());
   int orders = HistoryDealsTotal();
   
   double PL = 0.0;
   for (int i = orders - 1; i >= 0; i--)
   {
      ulong ticket=HistoryDealGetTicket(i);
      if(ticket==0)
      {
         Print("HistoryDealGetTicket failed, no trade history");
         break;
      }
      double profit = HistoryDealGetDouble(ticket,DEAL_PROFIT);
      if (profit != 0)
      {
         // Get deal datetime
         MqlDateTime deal_time;
         TimeToStruct(HistoryDealGetInteger(ticket, DEAL_TIME), deal_time);
         // Check deal time
         if (deal_time.day == current_day && deal_time.mon == current_month && deal_time.year == current_year)
         {
            PL += profit;
         }
         else
            break;
      }
   }
   double starting_balance = current_balance - PL;
   double current_equity   = AccountInfoDouble(ACCOUNT_EQUITY);
   return current_equity < starting_balance - DAILY_LOSS_LIMIT;
}