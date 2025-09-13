//+------------------------------------------------------------------+
//|                                               AccountMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/EnumToArray.mqh>

//+------------------------------------------------------------------+
//| Main class for reading symbol properties                         |
//+------------------------------------------------------------------+
class AccountMonitor
{
public:
   long get(const ENUM_ACCOUNT_INFO_INTEGER property) const
   {
      return AccountInfoInteger(property);
   }

   double get(const ENUM_ACCOUNT_INFO_DOUBLE property) const
   {
      return AccountInfoDouble(property);
   }

   string get(const ENUM_ACCOUNT_INFO_STRING property) const
   {
      return AccountInfoString(property);
   }

   long get(const int property, const long) const
   {
      return AccountInfoInteger((ENUM_ACCOUNT_INFO_INTEGER)property);
   }

   double get(const int property, const double) const
   {
      return AccountInfoDouble((ENUM_ACCOUNT_INFO_DOUBLE)property);
   }

   string get(const int property, const string) const
   {
      return AccountInfoString((ENUM_ACCOUNT_INFO_STRING)property);
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

   // explain properties according to subtypes
   static string stringify(const long v, const ENUM_ACCOUNT_INFO_INTEGER property)
   {
      switch(property)
      {
         case ACCOUNT_TRADE_ALLOWED:
         case ACCOUNT_TRADE_EXPERT:
         case ACCOUNT_FIFO_CLOSE:
            return boolean(v);
         case ACCOUNT_TRADE_MODE:
            return enumstr<ENUM_ACCOUNT_TRADE_MODE>(v);
         case ACCOUNT_MARGIN_MODE:
            return enumstr<ENUM_ACCOUNT_MARGIN_MODE>(v);
         case ACCOUNT_MARGIN_SO_MODE:
            return enumstr<ENUM_ACCOUNT_STOPOUT_MODE>(v);
      }
      
      return (string)v;
   }
   
   string stringify(const ENUM_ACCOUNT_INFO_INTEGER property) const
   {
      return stringify(AccountInfoInteger(property), property);
   }
   
   string stringify(const ENUM_ACCOUNT_INFO_DOUBLE property, const string format = NULL) const
   {
      if(format == NULL) return DoubleToString(AccountInfoDouble(property),
         (int)get(ACCOUNT_CURRENCY_DIGITS));
      return StringFormat(format, AccountInfoDouble(property));
   }

   string stringify(const ENUM_ACCOUNT_INFO_STRING property) const
   {
      return AccountInfoString(property);
   }
   
   // all properties of type enum E
   template<typename E>
   void list2log()
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
   AccountMonitor m;
   m.list2log<ENUM_ACCOUNT_INFO_INTEGER>();
   m.list2log<ENUM_ACCOUNT_INFO_DOUBLE>();
   m.list2log<ENUM_ACCOUNT_INFO_STRING>();
}
*/
//+------------------------------------------------------------------+
