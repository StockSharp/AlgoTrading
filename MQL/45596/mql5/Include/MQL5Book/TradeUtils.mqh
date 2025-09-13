//+------------------------------------------------------------------+
//|                                                   TradeUtils.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/TradeRetcode.mqh>

//+------------------------------------------------------------------+
//| Trade Utilities                                                  |
//+------------------------------------------------------------------+
namespace TU
{
   //+---------------------------------------------------------------+
   //| Helper class to normalize prices/volumes of specific symbol   |
   //+---------------------------------------------------------------+
   class SymbolMetrics
   {
   public:
      const string symbol;
      const double point;
      const int digits;
      const int lotDigits;
      
      SymbolMetrics(const string s): symbol(s),
         point(SymbolInfoDouble(s, SYMBOL_POINT)),
         digits((int)SymbolInfoInteger(s, SYMBOL_DIGITS)),
         lotDigits((int)MathLog10(1.0 / SymbolInfoDouble(s, SYMBOL_VOLUME_STEP))) { }
      
      SymbolMetrics(const int d, const int v): symbol(NULL), digits(d), lotDigits(v) { }
         
      double price(const double p)
      {
         return symbol != NULL ? TU::NormalizePrice(p, symbol) : NormalizeDouble(p, digits);
      }
      
      double volume(const double v)
      {
         return symbol != NULL ? TU::NormalizeLot(v, symbol) : NormalizeDouble(v, lotDigits);
      }
   
      string StringOf(const double v, const int d = INT_MAX)
      {
         return DoubleToString(v, d == INT_MAX ? digits : d);
      }
   };

   //+---------------------------------------------------------------+
   //| Unified estimation of 2 prices as profit or loss depending    |
   //| from trading direction buy or sell                            |
   //+---------------------------------------------------------------+
   struct TradeDirection
   {
      const bool direction; // 0/false - buy, 1/true - sell
      TradeDirection(const bool sell = false): direction(sell) { }
      TradeDirection(const ENUM_ORDER_TYPE type): direction(IsSellType(type)) { }
      
      // p1 is better than p2
      bool better(const double p1, const double p2) const
      {
         if(direction) return p1 < p2;
         return p1 > p2;
      }
   
      // p1 is worse than p2
      bool worse(const double p1, const double p2) const
      {
         if(direction) return p1 > p2;
         return p1 < p2;
      }
      
      int positive() const
      {
         return direction ? -1 : +1;
      }

      int negative() const
      {
         return direction ? +1 : -1;
      }
      
      double positive(const double p0, const double move) const
      {
         if(direction) return p0 - move;
         return p0 + move;
      }

      double negative(const double p0, const double move) const
      {
         if(direction) return p0 + move;
         return p0 - move;
      }
   };
   
   //+---------------------------------------------------------------+
   //| Quick and dirty beautifier of floating numbers, which produce |
   //| strings with endless fractional part with '0's or '9's        |
   //+---------------------------------------------------------------+
   string StringOf(const double number)
   {
      string str = (string)number;
      const int n = StringLen(str);
      if(n >= 16)
      {
         const int e = StringFind(str, "e");
         if(e > 0)
         {
            string sign = "";
            string num = StringSubstr(str, 0, e);
            if(num[0] == '-')
            {
               num = StringSubstr(num, 1);
               sign = "-";
            }
            StringReplace(num, ".", "");
            const int nexp = (int)StringToInteger(StringSubstr(str, e + 1));
            string zeros;
            StringInit(zeros, fabs(nexp) - 1, '0');
            if(nexp > 0)
            {
               str = sign + num + zeros+ ".0";
            }
            else
            {
               str = sign + "0." + zeros + num;
            }
         }
         
         int j = n - 2;
         string s2 = StringSubstr(str, 0, n - 1);
         const uchar c = (uchar)s2[j];
         if(c == '0' || c == '9')
         {
            while(--j >= 0 && s2[j] == c);
            
            if(j + 1 < n - 2)
            {
               StringSetLength(s2, j + 1);
               if(c == '9')
               {
                  const ushort dig = (ushort)s2[j];
                  if(dig == '.')
                  {
                     StringSetCharacter(s2, j - 1, (ushort)(s2[j - 1] + 1));
                     s2 += "0";
                  }
                  else
                  {
                     StringSetCharacter(s2, j, (ushort)(dig + 1));
                  }
               }
            }
            return s2;
         }
      }
      return str;
   }

   string StringOf(const MqlTradeRequest &r)
   {
      SymbolMetrics p(r.symbol);

      // main block: action, symbol, type      
      string text = EnumToString(r.action);
      if(StringLen(r.symbol) != 0) text += ", " + r.symbol;
      text += ", " + EnumToString(r.type);
      // volume block
      if(r.volume != 0) text += ", V=" + p.StringOf(r.volume, p.lotDigits);
      text += ", " + EnumToString(r.type_filling);
      // all prices block
      if(r.price != 0) text += ", @ " + p.StringOf(r.price);
      if(r.stoplimit != 0) text += ", X=" + p.StringOf(r.stoplimit);
      if(r.sl != 0) text += ", SL=" + p.StringOf(r.sl);
      if(r.tp != 0) text += ", TP=" + p.StringOf(r.tp);
      if(r.deviation != 0) text += ", D=" + (string)r.deviation;
      // pending block
      if(IsPendingType(r.type)) text += ", " + EnumToString(r.type_time);
      if(r.expiration != 0) text += ", " + TimeToString(r.expiration);
      // modification block
      if(r.order != 0) text += ", #=" + (string)r.order;
      if(r.position != 0) text += ", P=" + (string)r.position;
      if(r.position_by != 0) text += ", b=" + (string)r.position_by;
      // auxiliary block
      if(r.magic != 0) text += ", M=" + (string)r.magic;
      if(StringLen(r.comment)) text += ", " + r.comment;
      
      return text;
   }
   
   string StringOf(const MqlTradeResult &r)
   {
      // NB: we don't have a reference to symbol here,
      // so all numbers are formatted by unified StringOf
      
      string text = TRCSTR(r.retcode);
      if(r.deal != 0) text += ", D=" + (string)r.deal;
      if(r.order != 0) text += ", #=" + (string)r.order;
      if(r.volume != 0) text += ", V=" + StringOf(r.volume);
      if(r.price != 0) text += ", @ " + StringOf(r.price);
      if(r.bid != 0) text += ", Bid=" + StringOf(r.bid);
      if(r.ask != 0) text += ", Ask=" + StringOf(r.ask);
      if(StringLen(r.comment)) text += ", " + r.comment;
      if(r.request_id != 0) text += ", Req=" + (string)r.request_id;
      if(r.retcode_external != 0) text += ", Ext=" + (string)r.retcode_external;
      
      return text;
   }
   
   string StringOf(const MqlTradeTransaction &t)
   {
      SymbolMetrics p(t.symbol);
      
      string text = EnumToString(t.type);
      if(t.deal != 0)
      {
         text += ", D=" + (string)t.deal
            + "(" + EnumToString(t.deal_type) + ")";
      }
      
      if(t.order)
      {
         text += ", #=" + (string)t.order
            + "(" + EnumToString(t.order_type)
            + "/" + EnumToString(t.order_state) + ")";
      }
      
      if(IsPendingType(t.order_type)) text += ", " + EnumToString(t.time_type);
      if(t.time_expiration != 0) text += ", " + TimeToString(t.time_expiration);
      
      if(StringLen(t.symbol) != 0) text += ", " + t.symbol;
      
      if(t.price != 0) text += ", @ " + p.StringOf(t.price);
      if(t.price_sl != 0) text += ", SL=" + p.StringOf(t.price_sl);
      if(t.price_tp != 0) text += ", TP=" + p.StringOf(t.price_tp);
      if(t.price_trigger != 0) text += ", X=" + p.StringOf(t.price_trigger);
      
      if(t.volume != 0) text += ", V=" + p.StringOf(t.volume, p.lotDigits);
      
      if(t.position != 0) text += ", P=" + (string)t.position;
      if(t.position_by != 0) text += ", b=" + (string)t.position_by;
      
      return text;
   }
   
   bool Equal(const double v1, const double v2)
   {
      return v1 == v2 || fabs(v1 - v2) < DBL_EPSILON * fmax(fabs(v1), fabs(v2));
   }

   double NormalizeLot(const double lot, const string symbol = NULL)
   {
      const string s = symbol == NULL ? _Symbol : symbol;
      const double stepLot = SymbolInfoDouble(s, SYMBOL_VOLUME_STEP);
      if(stepLot <= 0)
      {
         Print("SYMBOL_VOLUME_STEP=0 for ", s);
         return 0;
      }
      
      const double nlot = NormalizeDouble(lot, -(int)MathLog10(stepLot));
      const double newLotsRounded = MathFloor(nlot / stepLot) * stepLot;
   
      const double minLot = SymbolInfoDouble(s, SYMBOL_VOLUME_MIN);
      if(minLot <= 0)
      {
         Print("SYMBOL_VOLUME_MIN=0 for ", s);
         return 0;
      }
   
      if(newLotsRounded < minLot)
      {
         return 0;
      }
     
      const double maxLot = SymbolInfoDouble(s, SYMBOL_VOLUME_MAX);
      if(newLotsRounded > maxLot) return maxLot;
     
      return newLotsRounded;
   }
   
   bool IsNormalizedLot(const double lot, const string symbol = NULL)
   {
      const double m = MathMin(SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN),
         SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP));
      if(m == 0) return false;
      const int d = (int)MathLog10(1.0 / m);
      return Equal(NormalizeDouble(lot, d), lot);
   }

   double NormalizePrice(const double price, const string symbol = NULL)
   {
      const double tick = SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE);
      if(tick == 0)
      {
         Print("SYMBOL_TRADE_TICK_SIZE=0 for ", symbol == NULL ? _Symbol : symbol);
         return price;
      }
      return MathRound(price / tick) * tick;
   }
   
   bool IsNormalizedPrice(const double price, const string symbol = NULL)
   {
      return Equal(NormalizePrice(price, symbol), price);
   }

   double GetCurrentPrice(const ENUM_ORDER_TYPE type, const string symbol = NULL)
   {
      if(type == ORDER_TYPE_CLOSE_BY) return 0;
      return SymbolInfoDouble(symbol, IsBuyType(type) ? SYMBOL_ASK : SYMBOL_BID);
   }
   
   bool IsBuyType(const ENUM_ORDER_TYPE type)
   {
      return (type & 1) == 0 && type < ORDER_TYPE_CLOSE_BY;
   }
   
   bool IsSellType(const ENUM_ORDER_TYPE type)
   {
      return (type & 1) == 1;
   }
   
   bool IsMarketType(const ENUM_ORDER_TYPE type)
   {
      return type == ORDER_TYPE_BUY || type == ORDER_TYPE_SELL;
   }

   bool IsPendingType(const ENUM_ORDER_TYPE type)
   {
      return type > ORDER_TYPE_SELL && type < ORDER_TYPE_CLOSE_BY;
   }
   
   bool IsSameType(const ENUM_ORDER_TYPE type1, const ENUM_ORDER_TYPE type2)
   {
      return (type1 & 1) == (type2 & 1);
   }

   ulong PositionSelectById(const ulong id)
   {
      for(int i = 0; i < PositionsTotal(); ++i)
      {
         const ulong t = PositionGetTicket(i); // selected
         if(PositionGetInteger(POSITION_IDENTIFIER) == id)
         {
            return t;
         }
      }
      return 0;
   }
};
//+------------------------------------------------------------------+
