//+------------------------------------------------------------------+
//|                                                SymbolMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/EnumToArray.mqh>

//+------------------------------------------------------------------+
//| SYMBOL_EXPIRATION options (bits)                                 |
//+------------------------------------------------------------------+
enum SYMBOL_EXPIRATION
{
   _SYMBOL_EXPIRATION_GTC = 1,
   _SYMBOL_EXPIRATION_DAY = 2,
   _SYMBOL_EXPIRATION_SPECIFIED = 4,
   _SYMBOL_EXPIRATION_SPECIFIED_DAY = 8,
};

//+------------------------------------------------------------------+
//| SYMBOL_FILLING options (bits)                                    |
//+------------------------------------------------------------------+
enum SYMBOL_FILLING
{
   _SYMBOL_FILLING_RETURN = 0,
   _SYMBOL_FILLING_FOK = 1,
   _SYMBOL_FILLING_IOC = 2,
};

//+------------------------------------------------------------------+
//| Allowed order types (bits)                                       |
//+------------------------------------------------------------------+
enum SYMBOL_ORDER
{
   _SYMBOL_ORDER_MARKET = 1,
   _SYMBOL_ORDER_LIMIT = 2,
   _SYMBOL_ORDER_STOP = 4,
   _SYMBOL_ORDER_STOP_LIMIT = 8,
   _SYMBOL_ORDER_SL = 16,
   _SYMBOL_ORDER_TP = 32,
   _SYMBOL_ORDER_CLOSEBY = 64,
};

//+------------------------------------------------------------------+
//| Main class for reading symbol properties                         |
//+------------------------------------------------------------------+
class SymbolMonitor
{
public:
   const string name;
   SymbolMonitor(): name(_Symbol) { }
   SymbolMonitor(const string s): name(s) { }

   long get(const ENUM_SYMBOL_INFO_INTEGER property) const
   {
      return SymbolInfoInteger(name, property);
   }

   double get(const ENUM_SYMBOL_INFO_DOUBLE property) const
   {
      return SymbolInfoDouble(name, property);
   }

   string get(const ENUM_SYMBOL_INFO_STRING property) const
   {
      return SymbolInfoString(name, property);
   }

   long get(const int property, const long) const
   {
      return SymbolInfoInteger(name, (ENUM_SYMBOL_INFO_INTEGER)property);
   }

   double get(const int property, const double) const
   {
      return SymbolInfoDouble(name, (ENUM_SYMBOL_INFO_DOUBLE)property);
   }

   string get(const int property, const string) const
   {
      return SymbolInfoString(name, (ENUM_SYMBOL_INFO_STRING)property);
   }
   
   static string boolean(const long v)
   {
      return v ? "true" : "false";
   }
   
   template<typename E>
   static string enumstr(const long v)
   {
      return EnumToString((E)v);
   }

   template<typename E>
   static string maskstr(const long v)
   {
      string text = "";
      if(v == 0)
      {
         ResetLastError();
         const string s = EnumToString((E)v);
         if(_LastError == 0) return "[(" + s + ")]";
      }
      for(int i = 0; ; ++i)
      {
         ResetLastError();
         const string s = EnumToString((E)(1 << i));
         if(_LastError != 0)
         {
            break;
         }
         if((v & (1 << i)) != 0)
         {
            text += s + " ";
         }
      }
      // square brackets used to distinguish single enum description
      // from bitmask
      return "[ " + text + "]";
   }

   // explain properties according to subtypes
   static string stringify(const long v, const ENUM_SYMBOL_INFO_INTEGER property)
   {
      switch(property)
      {
         case SYMBOL_SELECT:
         case SYMBOL_SPREAD_FLOAT:
         case SYMBOL_VISIBLE:
         case SYMBOL_CUSTOM:
         case SYMBOL_MARGIN_HEDGED_USE_LEG:
         case SYMBOL_EXIST:
            return boolean(v);
         case SYMBOL_TIME:
            return TimeToString(v, TIME_DATE|TIME_SECONDS);
         case SYMBOL_TRADE_CALC_MODE:   
            return enumstr<ENUM_SYMBOL_CALC_MODE>(v);
         case SYMBOL_TRADE_MODE:
            return enumstr<ENUM_SYMBOL_TRADE_MODE>(v);
         case SYMBOL_TRADE_EXEMODE:
            return enumstr<ENUM_SYMBOL_TRADE_EXECUTION>(v);
         case SYMBOL_SWAP_MODE:
            return enumstr<ENUM_SYMBOL_SWAP_MODE>(v);
         case SYMBOL_SWAP_ROLLOVER3DAYS:
            return enumstr<ENUM_DAY_OF_WEEK>(v);
         case SYMBOL_EXPIRATION_MODE:
            return maskstr<SYMBOL_EXPIRATION>(v);
         case SYMBOL_FILLING_MODE:
            return maskstr<SYMBOL_FILLING>(v);
         case SYMBOL_START_TIME:
         case SYMBOL_EXPIRATION_TIME:
            return TimeToString(v);
         case SYMBOL_ORDER_MODE:
            return maskstr<SYMBOL_ORDER>(v);
         case SYMBOL_OPTION_RIGHT:
            return enumstr<ENUM_SYMBOL_OPTION_RIGHT>(v);
         case SYMBOL_OPTION_MODE:
            return enumstr<ENUM_SYMBOL_OPTION_MODE>(v);
         case SYMBOL_CHART_MODE:
            return enumstr<ENUM_SYMBOL_CHART_MODE>(v);
         case SYMBOL_ORDER_GTC_MODE:
            return enumstr<ENUM_SYMBOL_ORDER_GTC_MODE>(v);
         case SYMBOL_SECTOR:
            return enumstr<ENUM_SYMBOL_SECTOR>(v);
         case SYMBOL_INDUSTRY:
            return enumstr<ENUM_SYMBOL_INDUSTRY>(v);
         case SYMBOL_BACKGROUND_COLOR: // Byte order: Transparency Blue Green Red
            return StringFormat("TBGR(0x%08X)", v);
      }
      
      return (string)v;
   }
   
   string stringify(const ENUM_SYMBOL_INFO_INTEGER property) const
   {
      return stringify(SymbolInfoInteger(name, property), property);
   }
   
   string stringify(const ENUM_SYMBOL_INFO_DOUBLE property, const string format = NULL) const
   {
      if(format == NULL) return (string)SymbolInfoDouble(name, property);
      return StringFormat(format, SymbolInfoDouble(name, property));
   }

   string stringify(const ENUM_SYMBOL_INFO_STRING property) const
   {
      return SymbolInfoString(name, property);
   }
   
   // all properties of type enum E
   template<typename E>
   void list2log() const
   {
      E e = (E)0; // disable warning 'possible use of uninitialized variable'
      int array[];
      const int n = EnumToArray(e, array, 0, USHORT_MAX);
      Print(typename(E), " Count=", n);
      for(int i = 0; i < n; ++i)
      {
         e = (E)array[i];
         PrintFormat("% 3d %s=%s", i, EnumToString(e), stringify(e));
      }
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
/* example
void OnStart()
{
   SymbolMonitor m;
   m.list2log<ENUM_SYMBOL_INFO_INTEGER>();
   m.list2log<ENUM_SYMBOL_INFO_DOUBLE>();
   m.list2log<ENUM_SYMBOL_INFO_STRING>();
}
*/
//+------------------------------------------------------------------+
