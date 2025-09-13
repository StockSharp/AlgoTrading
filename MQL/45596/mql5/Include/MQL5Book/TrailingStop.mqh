//+------------------------------------------------------------------+
//|                                                 TrailingStop.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MqlTradeSync.mqh>

//+------------------------------------------------------------------+
//| Base class for trailing on specific distance in points           |
//+------------------------------------------------------------------+
class TrailingStop
{
   const ulong ticket;  // position to manage
   const string symbol; // symbol of the position
   const double point;  // price point for the symbol
   const uint distance; // stop loss in points (for default trailing method)
   const uint step;     // sensitivity in points (1+)

protected:
   double level;
   bool ok;

   virtual double detectLevel() // override this method in descendants
   {
      return DBL_MAX;           // default distance trailing is enabled
   }

public:
   TrailingStop(const ulong t, const uint d, const uint s = 1) :
      ticket(t), distance(d), step(fmax(s, 1)),
      symbol(PositionSelectByTicket(t) ? PositionGetString(POSITION_SYMBOL) : NULL),
      point(SymbolInfoDouble(symbol, SYMBOL_POINT))
   {
      if(symbol == NULL)
      {
         WARNING("Position not found: " + (string)t);
         ok = false;
      }
      else
      {
         ok = true;
      }
   }
   
   bool isOK() const
   {
      return ok;
   }
      
   double getLevel() const
   {
      return level;
   }
   
   virtual bool trail()
   {
      if(!PositionSelectByTicket(ticket))
      {
         ok = false;
         return false;
      }

      // get prices required for calculations
      const double current = PositionGetDouble(POSITION_PRICE_CURRENT);
      const double sl = PositionGetDouble(POSITION_SL);
      const double tp = PositionGetDouble(POSITION_TP);

      // POSITION_TYPE_BUY  = 0 (false)
      // POSITION_TYPE_SELL = 1 (true)
      const bool sell = (bool)PositionGetInteger(POSITION_TYPE);
      TU::TradeDirection dir(sell);
      
      level = detectLevel();
      if(level == 0) return true; // can't trail - caller should remove SL if they want
      if(level == DBL_MAX) level = dir.negative(current, point * distance);
      level = TU::NormalizePrice(level, symbol);
      
      if(!dir.better(current, level))
      {
         return true; // can't change SL to profitable side
      }
      
      if(sl == 0)
      {
         PrintFormat("Initial SL: %f", level);
         move(level, tp);
      }
      else
      {
         if(dir.better(level, sl) && fabs(level - sl) >= point * step)
         {
            PrintFormat("SL: %f -> %f", sl, level);
            move(level, tp);
         }
      }
      
      return true; // has position
   }
   
   bool move(const double sl, const double tp)
   {
      MqlTradeRequestSync request;
      request.position = ticket;
      if(request.adjust(sl, tp) && request.completed())
      {
         Print("OK Trailing: ", TU::StringOf(sl));
         return true;
      }
      return false;
   }
};

//+------------------------------------------------------------------+
//| Derived class for trailing by Moving Average                     |
//+------------------------------------------------------------------+
class TrailingStopByMA: public TrailingStop
{
   int handle;
public:
   TrailingStopByMA(const ulong t, const int period,
      const int offset = 1,
      const ENUM_MA_METHOD method = MODE_SMA,
      const ENUM_APPLIED_PRICE type = PRICE_CLOSE): TrailingStop(t, 0, 1)
   {
      handle = iMA(_Symbol, PERIOD_CURRENT, period, offset, method, type);
   }
   virtual double detectLevel() override
   {
      double array[1];
      ResetLastError();
      if(CopyBuffer(handle, 0, 0, 1, array) != 1)
      {
         Print("CopyBuffer error: ", _LastError);
         return 0;
      }
      return array[0];
   }
};
//+------------------------------------------------------------------+
