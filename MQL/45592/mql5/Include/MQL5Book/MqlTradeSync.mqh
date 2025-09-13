//+------------------------------------------------------------------+
//|                                                 MqlTradeSync.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/TradeRetcode.mqh>
#include <MQL5Book/TradeUtils.mqh>

// until SHOW_WARNINGS not defined in your code,
// the following default definition of WARNING will disable warnings;
// you can define SHOW_WARNINGS and also redefine WARNING
// in your code with special logging

#ifndef SHOW_WARNINGS
#ifndef WARNING
#define WARNING(X)    // this empty macro to disable warnings by default
#endif
#endif

#include <MQL5Book/Warnings.mqh>

#ifndef MAX_REQUOTES
#define MAX_REQUOTES 10
#endif

// RETURN macro can be predefined in your source code as empty thing
//    #define RETURN(X)
// to prevent early returns after failed checkups in MqlStructRequestSync methods,
// so incorrect requests will be sent and retcodes received from the server

#ifndef RETURN
#define RETURN(X) return(X)
#endif

//+------------------------------------------------------------------+
//| Data type for results of various trade requests,                 |
//| performed in a "synced" manner - with awaiting complete data.    |
//+------------------------------------------------------------------+
struct MqlTradeResultSync: public MqlTradeResult
{
   ulong position;
   double partial;

   MqlTradeResultSync()
   {
      ZeroMemory(this);
   }
   
   typedef bool (*condition)(MqlTradeResultSync &ref);
   
   bool wait(condition p, const ulong msc = 1000)
   {
      const ulong start = GetTickCount64();
      bool success;
      while(!(success = p(this)) && GetTickCount64() - start < msc);
      
      if(GetTickCount64() - start >= msc)
      {
         // on timeout update retcode if it was successful
         if(retcode >= TRADE_RETCODE_PLACED && retcode <= TRADE_RETCODE_DONE_PARTIAL)
         {
            WARNING("wait internal timeout " + EnumToString((TRADE_RETCODE)retcode));
            retcode = TRADE_RETCODE_TIMEOUT;
         }
      }
      return success;
   }

   // NB: use static because MQL5 does not support member methods in typedefs so far
   static bool orderExist(MqlTradeResultSync &ref)
   {
      return OrderSelect(ref.order) || HistoryOrderSelect(ref.order);
   }
   
   static bool positionExist(MqlTradeResultSync &ref)
   {
      ulong posid, ticket;
      if(HistoryOrderGetInteger(ref.order, ORDER_POSITION_ID, posid))
      {
         // there is no a built-in way to get ticket from id
         ticket = TU::PositionSelectById(posid);
         
         if(HistorySelectByPosition(posid))
         {
            if(ticket != posid)
            {
               WARNING("Position ticket <> id: " + (string)ticket + ", " + (string)posid);
            }
            ref.position = ticket; // posid = ticket in many cases, but not always
            for(int i = HistoryDealsTotal() - 1; i >= 0; i--)
            {
               const ulong d = HistoryDealGetTicket(i);
               if(HistoryDealGetInteger(d, DEAL_ORDER) == ref.order)
               {
                  ref.deal = d;
                  ref.price = HistoryDealGetDouble(d, DEAL_PRICE);
                  ref.volume = HistoryDealGetDouble(d, DEAL_VOLUME);
               }
            }
            return true;
         }
      }
      return false;
   }
   
   static bool checkSLTP(MqlTradeResultSync &ref)
   {
      if(PositionSelectByTicket(ref.position))
      {
         return TU::Equal(PositionGetDouble(POSITION_SL), ref.bid) // sl from request
            && TU::Equal(PositionGetDouble(POSITION_TP), ref.ask); // tp from request
      }
      else
      {
         WARNING("PositionSelectByTicket failed: P=" + (string)ref.position);
      }
      return false;
   }
   
   static bool positionRemoved(MqlTradeResultSync &ref)
   {
      if(ref.partial)
      {
         return PositionSelectByTicket(ref.position)
            && TU::Equal(PositionGetDouble(POSITION_VOLUME), ref.partial);
      }
      return !PositionSelectByTicket(ref.position);
   }

   static bool orderModified(MqlTradeResultSync &ref)
   {
      if(!(OrderSelect(ref.order) || HistoryOrderSelect(ref.order)))
      {
         WARNING("OrderSelect failed: #=" + (string)ref.order);
         return false;
      }
      return TU::Equal(ref.bid, OrderGetDouble(ORDER_SL))
         && TU::Equal(ref.ask, OrderGetDouble(ORDER_TP))
         && TU::Equal(ref.price, OrderGetDouble(ORDER_PRICE_OPEN))
         && TU::Equal(ref.volume, OrderGetDouble(ORDER_PRICE_STOPLIMIT));
   }

   static bool orderRemoved(MqlTradeResultSync &ref)
   {
      return !OrderSelect(ref.order) && HistoryOrderSelect(ref.order);
   }

   bool placed(const ulong msc = 1000)
   {
      if(retcode != TRADE_RETCODE_DONE
         && retcode != TRADE_RETCODE_DONE_PARTIAL)
      {
         return false;
      }
      
      if(!wait(orderExist, msc))
      {
         WARNING("Waiting for order: #" + (string)order);
         return false;
      }
      return true;
   }
   
   bool opened(const ulong msc = 1000)
   {
      if(retcode != TRADE_RETCODE_DONE
         && retcode != TRADE_RETCODE_DONE_PARTIAL)
      {
         return false;
      }
      
      if(!wait(orderExist, msc))
      {
         WARNING("Waiting for order: #" + (string)order);
      }
      
      if(deal != 0 && HistoryDealSelect(deal))
      {
         WARNING("Waiting for position for deal D=" + (string)deal);
         ulong posid;
         if(HistoryDealGetInteger(deal, DEAL_POSITION_ID, posid))
         {
            position = TU::PositionSelectById(posid);
            return true;
         }
      }
      
      if(!wait(positionExist, msc))
      {
         WARNING("Timeout");
         return false;
      }
      
      return true;
   }
   
   bool adjusted(const ulong msc = 1000)
   {
      if(retcode != TRADE_RETCODE_DONE && retcode != TRADE_RETCODE_PLACED)
      {
         return false;
      }
      
      if(!wait(checkSLTP, msc))
      {
         WARNING("SL/TP modification timeout: P=" + (string)position);
         return false;
      }
      
      return true;
   }
   
   bool closed(const ulong msc = 1000)
   {
      if(retcode != TRADE_RETCODE_DONE)
      {
         return false;
      }

      if(!wait(positionRemoved, msc))
      {
         WARNING("Position removal timeout: P=" + (string)position);
         return false;
      }
      
      return true;
   }

   bool modified(const ulong msc = 1000)
   {
      if(retcode != TRADE_RETCODE_DONE && retcode != TRADE_RETCODE_PLACED)
      {
         return false;
      }

      if(!wait(orderModified, msc))
      {
         WARNING("Order not found in environment: #" + (string)order);
         return false;
      }
      return true;
   }

   bool removed(const ulong msc = 1000)
   {
      if(retcode != TRADE_RETCODE_DONE)
      {
         return false;
      }

      if(!wait(orderRemoved, msc))
      {
         WARNING("Order removal timeout: #=" + (string)order);
         return false;
      }
      
      return true;
   }
};

//+------------------------------------------------------------------+
//| Data type for streamlined execution of various trade requests.   |
//| Provides proper preparation of input data for the fields.        |
//+------------------------------------------------------------------+
struct MqlTradeRequestSync: public MqlTradeRequest
{
   MqlTradeResultSync result;
   ulong timeout;
   double partial; // remaining volume after partial close

   static bool AsyncEnabled;
   
   MqlTradeRequestSync(const string s = NULL, const ulong t = 1000)
   {
      ZeroMemory(this);
      timeout = t;
      symbol = s == NULL ? _Symbol : s;
   }
   
   // Common methods
   
   bool completed()
   {
      if(action == TRADE_ACTION_DEAL)
      {
         if(position == 0)
         {
            const bool success = result.opened(timeout);
            if(success) position = result.position;
            return success;
         }
         else
         {
            result.position = position;
            result.partial = partial;
            return result.closed(timeout);
         }
      }
      else if(action == TRADE_ACTION_SLTP)
      {
         // pass original data from request to compare with online position,
         // this is required because result for TRADE_ACTION_SLTP contains retcode only
         result.position = position;
         result.bid = sl;
         result.ask = tp;
         return result.adjusted(timeout);
      }
      else if(action == TRADE_ACTION_CLOSE_BY)
      {
         return result.closed(timeout);
      }
      else if(action == TRADE_ACTION_PENDING)
      {
         return result.placed(timeout);
      }
      else if(action == TRADE_ACTION_MODIFY)
      {
         result.order = order;
         result.bid = sl;
         result.ask = tp;
         result.price = price;
         result.volume = stoplimit;
         return result.modified(timeout);
      }
      else if(action == TRADE_ACTION_REMOVE)
      {
         result.order = order;
         return result.removed(timeout);
      }
      return false;
   }

   static bool requote(const uint retcode)
   {
      switch(retcode)
      {
      case TRADE_RETCODE_REQUOTE:
      case TRADE_RETCODE_PRICE_CHANGED:
      case TRADE_RETCODE_PRICE_OFF:
         return true;
      }
      return false;
   }

   // Market (immediate) trades
   
   ulong buy(const double lot, const double p = 0,
      const double stop = 0, const double take = 0)
   {
      return buy(symbol, lot, p, stop, take);
   }

   ulong sell(const double lot, const double p = 0,
      const double stop = 0, const double take = 0)
   {
      return sell(symbol, lot, p, stop, take);
   }
   
   ulong buy(const string name, const double lot, const double p = 0,
      const double stop = 0, const double take = 0)
   {
      type = ORDER_TYPE_BUY;
      position = 0;
      return _market(name, lot, p, stop, take);
   }

   ulong sell(const string name, const double lot, const double p = 0,
      const double stop = 0, const double take = 0)
   {
      type = ORDER_TYPE_SELL;
      position = 0;
      return _market(name, lot, p, stop, take);
   }

   // Position modifications

   bool adjust(const ulong pos, const double stop = 0, const double take = 0)
   {
      if(!PositionSelectByTicket(pos))
      {
         WARNING("No position: P=" + (string)pos);
         result.retcode = TRADE_RETCODE_POSITION_CLOSED;
         RETURN(false);
      }
      return _adjust(pos, PositionGetString(POSITION_SYMBOL), stop, take);
   }

   bool adjust(const string name, const double stop = 0, const double take = 0)
   {
      if(!PositionSelect(name))
      {
         WARNING("No position: " + name);
         result.retcode = TRADE_RETCODE_POSITION_CLOSED;
         RETURN(false);
      }
      
      return _adjust(PositionGetInteger(POSITION_TICKET), name, stop, take);
   }
   
   bool adjust(const double stop = 0, const double take = 0)
   {
      if(position != 0)
      {
         if(!PositionSelectByTicket(position))
         {
            WARNING("No position with ticket P=" + (string)position);
            result.retcode = TRADE_RETCODE_POSITION_CLOSED;
            RETURN(false);
         }
         const string s = PositionGetString(POSITION_SYMBOL);
         if(symbol != NULL && symbol != s)
         {
            WARNING("Position symbol is adjusted from " + symbol + " to " + s);
         }
         symbol = s;
      }
      else if(AccountInfoInteger(ACCOUNT_MARGIN_MODE) != ACCOUNT_MARGIN_MODE_RETAIL_HEDGING
         && StringLen(symbol) > 0)
      {
         if(!PositionSelect(symbol))
         {
            WARNING("Can't select position for " + symbol);
            result.retcode = TRADE_RETCODE_POSITION_CLOSED;
            RETURN(false);
         }
         position = PositionGetInteger(POSITION_TICKET);
      }
      else
      {
         WARNING("Neither position ticket nor symbol was provided");
         result.retcode = TRADE_RETCODE_INVALID;
         RETURN(false);
      }
      return _adjust(position, symbol, stop, take);
   }

   // Position closing

   bool close(const string name, const double lot = 0)
   {
      if(!PositionSelect(name))
      {
         result.retcode = TRADE_RETCODE_POSITION_CLOSED;
         RETURN(false);
      }
      
      return close(PositionGetInteger(POSITION_TICKET), lot);
   }
   
   bool close(const ulong ticket, const double lot = 0)
   {
      if(!PositionSelectByTicket(ticket))
      {
         result.retcode = TRADE_RETCODE_POSITION_CLOSED;
         RETURN(false);
      }
      
      position = ticket;
      magic = PositionGetInteger(POSITION_MAGIC);
      symbol = PositionGetString(POSITION_SYMBOL);
      type = (ENUM_ORDER_TYPE)(PositionGetInteger(POSITION_TYPE) ^ 1);
      price = 0; // automatic setup for current price
      
      const double total = lot == 0 ? PositionGetDouble(POSITION_VOLUME) : TU::NormalizeLot(lot, symbol);
      double remaining = total;
      partial = PositionGetDouble(POSITION_VOLUME) - total;
      int k = 0;
      bool sent = false;
      
      do
      {
         if(total > SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX))
         {
            volume = SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX);
            remaining = total - ++k * volume; // keep precision high
         }
         else
         {
            volume = remaining;
            remaining = 0;
         }
         sent = _market(symbol, volume);
      }
      while(remaining > 0 && sent);
      return sent;
   }

   bool closeby(const ulong ticket1, const ulong ticket2)
   {
      if(!PositionSelectByTicket(ticket1))
      {
         result.retcode = TRADE_RETCODE_POSITION_CLOSED;
         RETURN(false);
      }
      const double volume1 = PositionGetDouble(POSITION_VOLUME);
      const string sym1 = PositionGetString(POSITION_SYMBOL);
      if(!PositionSelectByTicket(ticket2))
      {
         result.retcode = TRADE_RETCODE_POSITION_CLOSED;
         RETURN(false);
      }
      const double volume2 = PositionGetDouble(POSITION_VOLUME);
      const string sym2 = PositionGetString(POSITION_SYMBOL);
      if(sym1 != sym2)
      {
         WARNING(StringFormat("can't 'close by' positions of different symbols: %s and %s",
            sym1, sym2));
         result.retcode = TRADE_RETCODE_INVALID;
         RETURN(false);
      }
      if((SymbolInfoInteger(sym1, SYMBOL_ORDER_MODE) & SYMBOL_ORDER_CLOSEBY) == 0)
      {
         WARNING("'close by' not allowed for " + sym1);
         result.retcode = TRADE_RETCODE_INVALID_ORDER;
         RETURN(false);
      }

      action = TRADE_ACTION_CLOSE_BY;
      position = ticket1;
      position_by = ticket2;
      
      ZeroMemory(result);
      if(volume1 != volume2)
      {
         // remember which position should vanish
         if(volume1 < volume2)
            result.position = ticket1;
         else
            result.position = ticket2;
      }
      return orderSend(this, result);
   }

   // Pending orders placing

   ulong buyStop(const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      return buyStop(symbol, lot, p, stop, take, duration, until);
   }

   ulong sellStop(const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      return sellStop(symbol, lot, p, stop, take, duration, until);
   }
   
   ulong buyStop(const string name, const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_BUY_STOP;
      return _pending(name, lot, p, stop, take, duration, until);
   }

   ulong sellStop(const string name, const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_SELL_STOP;
      return _pending(name, lot, p, stop, take, duration, until);
   }

   ulong buyLimit(const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      return buyLimit(symbol, lot, p, stop, take, duration, until);
   }

   ulong sellLimit(const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      return sellLimit(symbol, lot, p, stop, take, duration, until);
   }

   ulong buyLimit(const string name, const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_BUY_LIMIT;
      return _pending(name, lot, p, stop, take, duration, until);
   }

   ulong sellLimit(const string name, const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_SELL_LIMIT;
      return _pending(name, lot, p, stop, take, duration, until);
   }

   ulong buyStopLimit(const double lot, const double p,
      const double origin,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_BUY_STOP_LIMIT;
      return _pending(symbol, lot, p, stop, take, duration, until, origin);
   }

   ulong sellStopLimit(const double lot, const double p,
      const double origin,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_SELL_STOP_LIMIT;
      return _pending(symbol, lot, p, stop, take, duration, until, origin);
   }

   ulong buyStopLimit(const string name, const double lot, const double p,
      const double origin,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_BUY_STOP_LIMIT;
      return _pending(name, lot, p, stop, take, duration, until, origin);
   }

   ulong sellStopLimit(const string name, const double lot, const double p,
      const double origin,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0)
   {
      type = ORDER_TYPE_SELL_STOP_LIMIT;
      return _pending(name, lot, p, stop, take, duration, until, origin);
   }

   // Pending order modification
   
   bool modify(const ulong ticket,
      const double p, const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0,
      const double origin = 0)
   {
      if(!OrderSelect(ticket))
      {
         result.retcode = TRADE_RETCODE_INVALID;
         RETURN(false);
      }
      
      action = TRADE_ACTION_MODIFY;
      order = ticket;
      
      // we need the following fields for check-ups inside subfunctions
      type = (ENUM_ORDER_TYPE)OrderGetInteger(ORDER_TYPE);
      symbol = OrderGetString(ORDER_SYMBOL);
      volume = OrderGetDouble(ORDER_VOLUME_CURRENT);
      
      if(!setVolumePrices(volume, p, stop, take, origin)) return false;
      if(!setExpiration(duration, until)) return false;
      ZeroMemory(result);
      return orderSend(this, result);
   }

   // Pending order removal
   
   bool remove(const ulong ticket)
   {
      action = TRADE_ACTION_REMOVE;
      order = ticket;
      ZeroMemory(result);

      if(!OrderSelect(ticket))
      {
         if(HistoryOrderSelect(ticket))
         {
            WARNING(StringFormat("Order %lld already removed", ticket));
            return true;
         }
         result.retcode = TRADE_RETCODE_INVALID;
         RETURN(false);
      }

      return orderSend(this, result);
   }

   // General purpose methods, name starts with underscore '_'
   // (made public for advanced use only, use with caution)
   
   // Prerequisite: field 'type' should be filled already with a pending order type
   ulong _pending(const string name, const double lot, const double p,
      const double stop = 0, const double take = 0,
      ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0,
      const double origin = 0)
   {
      if(!TU::IsPendingType(type))
      {
         result.retcode = TRADE_RETCODE_INVALID_ORDER;
         RETURN(0);
      }
      
      action = TRADE_ACTION_PENDING;
      if(!setSymbol(name)) return 0;
      if(!setVolumePrices(lot, p, stop, take, origin)) return 0;
      if(!setExpiration(duration, until)) return 0;

      /*
         Reference
         
         ENUM_ORDER_TYPE (only pending are used here):
            [ORDER_TYPE_BUY]           0     0
            [ORDER_TYPE_SELL]          1     0
            ORDER_TYPE_BUY_LIMIT       2     1
            ORDER_TYPE_SELL_LIMIT      3     1
            ORDER_TYPE_BUY_STOP        4     2
            ORDER_TYPE_SELL_STOP       5     2
            ORDER_TYPE_BUY_STOP_LIMIT  6     3
            ORDER_TYPE_SELL_STOP_LIMIT 7     3
            [ORDER_TYPE_CLOSE_BY]      8     4
            
         SYMBOL_ORDER_MODE bits (only pending are used here):
            [SYMBOL_ORDER_MARKET]    1
            SYMBOL_ORDER_LIMIT       2
            SYMBOL_ORDER_STOP        4
            SYMBOL_ORDER_STOP_LIMIT  8
            [SYMBOL_ORDER_SL]       16
            [SYMBOL_ORDER_TP]       32
            [SYMBOL_ORDER_CLOSEBY]  64
         
      */
      
      if((SymbolInfoInteger(name, SYMBOL_ORDER_MODE) & (1 << (type / 2))) == 0)
      {
         WARNING(StringFormat("pending orders %s not allowed for %s",
            EnumToString(type), name));
         result.retcode = TRADE_RETCODE_INVALID_ORDER;
         RETURN(0);
      }
      
      if(!checkMode())
      {
         WARNING(StringFormat("%s not allowed for %s trade mode", EnumToString(type), symbol));
         RETURN(0);
      }
      
      ZeroMemory(result);
      if(orderSend(this, result)) return result.order ? result.order :
         (result.retcode == TRADE_RETCODE_PLACED ? result.request_id : 0);
      return 0;
   }
   
   // Prerequisite: field 'type' should be filled already with a market order type
   ulong _market(const string name, const double lot, const double p = 0,
      const double stop = 0, const double take = 0)
   {
      if(!TU::IsMarketType(type))
      {
         result.retcode = TRADE_RETCODE_INVALID_ORDER;
         RETURN(0);
      }
      
      action = TRADE_ACTION_DEAL;
      if(!setSymbol(name)) return 0;
      if(!setVolumePrices(lot, p, stop, take)) return 0;

      if((SymbolInfoInteger(name, SYMBOL_ORDER_MODE) & SYMBOL_ORDER_MARKET) == 0)
      {
         WARNING("market orders not allowed for " + name);
         result.retcode = TRADE_RETCODE_INVALID_ORDER;
         RETURN(0);
      }
      
      if(!checkMode())
      {
         WARNING(StringFormat("%s not allowed for %s trade mode", EnumToString(type), symbol));
         RETURN(0);
      }
      
      int count = 0;
      do
      {
         ZeroMemory(result);
         if(orderSend(this, result)) return result.order ? result.order :
            (result.retcode == TRADE_RETCODE_PLACED ? result.request_id : 0);
         // automatic price means automatic handling of requotes
         if(requote(result.retcode))
         {
            WARNING("Requote N" + (string)++count);
            if(p == 0)
            {
               price = TU::GetCurrentPrice(type, symbol);
            }
         }
      }
      while(p == 0 && requote(result.retcode) && count < MAX_REQUOTES);
      
      return 0;
   }
   
private:
   // Prerequisite: Position 'pos' should be selected
   bool _adjust(const ulong pos, const string name,
      const double stop = 0, const double take = 0)
   {
      if(TU::Equal(stop, PositionGetDouble(POSITION_SL))
         && TU::Equal(take, PositionGetDouble(POSITION_TP)))
      {
         WARNING("SLTP already set as: " + TU::StringOf(stop) + ", " + TU::StringOf(take));
         result.retcode = TRADE_RETCODE_NO_CHANGES;
         RETURN(false);
      }

      action = TRADE_ACTION_SLTP;
      position = pos;
      type = (ENUM_ORDER_TYPE)PositionGetInteger(POSITION_TYPE); // only needed for our SLTP check
      if(!setSymbol(name)) return false;
      if(!setSLTP(stop, take)) return false;

      ZeroMemory(result);
      return orderSend(this, result);
   }

   void setFilling()
   {
      const int filling = (int)SymbolInfoInteger(symbol, SYMBOL_FILLING_MODE);
      const bool market = SymbolInfoInteger(symbol, SYMBOL_TRADE_EXEMODE) == SYMBOL_TRADE_EXECUTION_MARKET;
      
      // type_filling may be already assigned,
      // so matching bits mean that selected mode is allowed
      if(((type_filling + 1) & filling) != 0
         || (type_filling == ORDER_FILLING_RETURN && !market)) return; // ok
      
      if((filling & SYMBOL_FILLING_FOK) != 0)
      {
         type_filling = ORDER_FILLING_FOK;
      }
      else if((filling & SYMBOL_FILLING_IOC) != 0)
      {
         type_filling = ORDER_FILLING_IOC;
      }
      else
      {
         type_filling = ORDER_FILLING_RETURN;
      }
   }

   bool setSymbol(const string s)
   {
      if(s == NULL)
      {
         if(symbol == NULL)
         {
            WARNING("symbol is NULL, defaults to " + _Symbol);
            symbol = _Symbol;
            setFilling();
         }
         else
         {
            WARNING("new symbol is NULL, current used " + symbol);
         }
      }
      else
      {
         // check if provided symbol exists
         if(SymbolInfoDouble(s, SYMBOL_POINT) == 0)
         {
            WARNING("incorrect symbol " + s);
            result.retcode = TRADE_RETCODE_INVALID;
            RETURN(false);
         }
         if(symbol != s)
         {
            symbol = s;
            setFilling();
         }
      }
      return true;
   }
   
   bool setVolumePrices(const double v, const double p,
      const double stop, const double take,
      const double origin = 0)
   {
      TU::SymbolMetrics sm(symbol);

      volume = sm.volume(v); // NB! can become 0 if less then minimal
      if(volume == 0)
      {
         WARNING("volume is 0 after normalization");
         result.retcode = TRADE_RETCODE_INVALID_VOLUME;
         RETURN(false);
      }
      
      const double current = TU::GetCurrentPrice(type, symbol);
      if(p != 0)
      {
         price = sm.price(p);
         if(TU::IsPendingType(type) && !TU::Equal(price, current))
         {
            if(price > current)
            {
               if(type == ORDER_TYPE_BUY_LIMIT
               || type == ORDER_TYPE_SELL_STOP
               || type == ORDER_TYPE_SELL_STOP_LIMIT)
               {
                  WARNING(StringFormat("misplaced %s order: %s should be below current price %s",
                     EnumToString(type),
                     sm.StringOf(price), sm.StringOf(current)));
                  result.retcode = TRADE_RETCODE_INVALID_PRICE;
                  RETURN(false);
               }
            }
            else
            {
               if(type == ORDER_TYPE_SELL_LIMIT
               || type == ORDER_TYPE_BUY_STOP
               || type == ORDER_TYPE_BUY_STOP_LIMIT)
               {
                  WARNING(StringFormat("misplaced %s order: %s should be above current price %s",
                     EnumToString(type),
                     sm.StringOf(price), sm.StringOf(current)));
                  result.retcode = TRADE_RETCODE_INVALID_PRICE;
                  RETURN(false);
               }
            }
         }
      }
      else
      {
         price = sm.price(current);
      }

      stoplimit = sm.price(origin); // filled for *_STOP_LIMIT orders only
      
      return setSLTP(stop, take);
   }
   
   bool setSLTP(const double stop, const double take)
   {
      if((SymbolInfoInteger(symbol, SYMBOL_ORDER_MODE) & SYMBOL_ORDER_SL) == 0
         && stop != 0)
      {
         WARNING("Stop Loss not allowed for " + symbol);
         result.retcode = TRADE_RETCODE_INVALID_STOPS;
         RETURN(false);
      }
      if((SymbolInfoInteger(symbol, SYMBOL_ORDER_MODE) & SYMBOL_ORDER_TP) == 0
         && take != 0)
      {
         WARNING("Take Profit not allowed for " + symbol);
         result.retcode = TRADE_RETCODE_INVALID_STOPS;
         RETURN(false);
      }

      TU::SymbolMetrics sm(symbol);
      TU::TradeDirection dir(type);
      // for all orders except for *_STOP_LIMIT a nonzero specific price
      //    or current market price is used as reference point for sl/tp check-up;
      // for *_STOP_LIMIT-orders sl/tp is marked against stoplimit, which is nonzero
      //    only for these 2 types of orders 
      const double current = stoplimit == 0 ?
         (price == 0 ? TU::GetCurrentPrice((ENUM_ORDER_TYPE)(type ^ 1), symbol) : price) :
         stoplimit;
      const int level = (int)SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL);
      const int freeze = (int)SymbolInfoInteger(symbol, SYMBOL_TRADE_FREEZE_LEVEL);
      
      if(stop != 0)
      {
         sl = sm.price(stop);
         if(!dir.worse(sl, current))
         {
            WARNING(StringFormat("wrong SL (%s) against price (%s)",
               TU::StringOf(sl), TU::StringOf(current)));
            result.retcode = TRADE_RETCODE_INVALID_STOPS;
            RETURN(false);
         }
         
         if(level > 0 && fabs(current - sl) < level * sm.point)
         {
            WARNING(StringFormat("too close SL (%s) to current price (%s)",
               TU::StringOf(sl), TU::StringOf(current)));
            result.retcode = TRADE_RETCODE_INVALID_STOPS;
            RETURN(false);
         }
         
         if(action == TRADE_ACTION_SLTP
            && freeze > 0 && fabs(current - sl) < freeze * sm.point)
         {
            WARNING(StringFormat("frozen SL (%s) near current price (%s)",
               TU::StringOf(sl), TU::StringOf(current)));
            result.retcode = TRADE_RETCODE_INVALID_STOPS;
            RETURN(false);
         }
      }
      else
      {
         sl = 0; // no SL
      }
      
      if(take != 0)
      {
         tp = sm.price(take);
         if(!dir.better(tp, current))
         {
            WARNING(StringFormat("wrong TP (%s) against price (%s)",
               TU::StringOf(tp), TU::StringOf(current)));
            result.retcode = TRADE_RETCODE_INVALID_STOPS;
            RETURN(false);
         }

         if(level > 0 && fabs(current - tp) < level * sm.point)
         {
            WARNING(StringFormat("too close TP (%s) to current price (%s)",
               TU::StringOf(tp), TU::StringOf(current)));
            result.retcode = TRADE_RETCODE_INVALID_STOPS;
            RETURN(false);
         }

         if(action == TRADE_ACTION_SLTP
            && freeze > 0 && fabs(current - tp) < freeze * sm.point)
         {
            WARNING(StringFormat("forzen TP (%s) near current price (%s)",
               TU::StringOf(tp), TU::StringOf(current)));
            result.retcode = TRADE_RETCODE_INVALID_STOPS;
            RETURN(false);
         }
      }
      else
      {
         tp = 0; // no TP
      }
      return true;
   }
   
   bool setExpiration(ENUM_ORDER_TYPE_TIME duration = -1, datetime until = 0) // ORDER_TIME_GTC
   {
      // if something already assigned to the struct fields,
      // and parameters are default (unspecified), keep existing settings
      if(duration == -1)
      {
         duration = type_time != ORDER_TIME_GTC ? type_time : ORDER_TIME_GTC;
      }
      
      if(expiration != 0 && until == 0)
      {
         until = expiration;
      }
   
      /*
         Reference
         
         Available modes (bits) in SYMBOL_EXPIRATION_MODE:
            SYMBOL_EXPIRATION_GTC = 1
            SYMBOL_EXPIRATION_DAY = 2
            SYMBOL_EXPIRATION_SPECIFIED = 4
            SYMBOL_EXPIRATION_SPECIFIED_DAY = 8

         ENUM_ORDER_TYPE_TIME:
            ORDER_TIME_GTC = 0
            ORDER_TIME_DAY = 1
            ORDER_TIME_SPECIFIED = 2
            ORDER_TIME_SPECIFIED_DAY = 3
      */
      const int modes = (int)SymbolInfoInteger(symbol, SYMBOL_EXPIRATION_MODE);
      if(((1 << duration) & modes) != 0)
      {
         type_time = duration;
         if((duration == ORDER_TIME_SPECIFIED || duration == ORDER_TIME_SPECIFIED_DAY)
            && until == 0)
         {
            WARNING(StringFormat("datetime is 0, but it's required for order expiration mode %s",
               EnumToString(duration)));
            result.retcode = TRADE_RETCODE_INVALID_EXPIRATION;
            RETURN(false);
         }
         if(until > 0 && until <= TimeTradeServer())
         {
            WARNING(StringFormat("expiration datetime %s is in past, server time is %s",
               TimeToString(until), TimeToString(TimeTradeServer())));
            result.retcode = TRADE_RETCODE_INVALID_EXPIRATION;
            RETURN(false);
         }
         expiration = until;
      }
      else
      {
         WARNING(StringFormat("order expiration mode %s is not allowed for %s",
            EnumToString(duration), symbol));
         result.retcode = TRADE_RETCODE_INVALID_EXPIRATION;
         RETURN(false);
      }
      return true;
   }
   
   bool checkMode()
   {
      const ENUM_SYMBOL_TRADE_MODE mode =
         (ENUM_SYMBOL_TRADE_MODE)SymbolInfoInteger(symbol, SYMBOL_TRADE_MODE);
      if(mode == SYMBOL_TRADE_MODE_DISABLED)
      {
         result.retcode = TRADE_RETCODE_TRADE_DISABLED;
         return false;
      }
      const bool closure = position != 0;
      if(mode == SYMBOL_TRADE_MODE_CLOSEONLY && !closure)
      {
         result.retcode = TRADE_RETCODE_CLOSE_ONLY;
         return false;
      }
      if(mode == SYMBOL_TRADE_MODE_LONGONLY && !(TU::IsBuyType(type) ^ closure))
      {
         result.retcode = TRADE_RETCODE_LONG_ONLY;
         return false;
      }
      if(mode == SYMBOL_TRADE_MODE_SHORTONLY && !(TU::IsSellType(type) ^ closure))
      {
         result.retcode = TRADE_RETCODE_SHORT_ONLY;
         return false;
      }
      return true;
   }

   bool orderSend(const MqlTradeRequest &req, MqlTradeResult &res)
   {
      return AsyncEnabled ? OrderSendAsync(req, res) : OrderSend(req, res);
   }
};

static bool MqlTradeRequestSync::AsyncEnabled = false;

//+------------------------------------------------------------------+
//| Helper class for switching off async mode atomatically           |
//+------------------------------------------------------------------+
class AsyncSwitcher
{
public:
   AsyncSwitcher(const bool enabled = true)
   {
      MqlTradeRequestSync::AsyncEnabled = enabled;
   }
   ~AsyncSwitcher()
   {
      MqlTradeRequestSync::AsyncEnabled = false;
   }
};

//+------------------------------------------------------------------+
