//+------------------------------------------------------------------+
//|                                          CustomSymbolMonitor.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/EnumToArray.mqh>
#include <MQL5Book/SymbolMonitor.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/MqlError.mqh>

//+------------------------------------------------------------------+
//| Main class for reading and writing custom symbol properties      |
//+------------------------------------------------------------------+
class CustomSymbolMonitor: public SymbolMonitor
{
protected:
   AutoPtr<const SymbolMonitor> origin;
   bool warningsOff;
   int errorCount;
   int mismatchCount;
   int fixes;
   
   template<typename E>
   static bool detect(const int v)
   {
      ResetLastError();
      const string s = EnumToString((E)v); // resulting string is not used
      if(_LastError == 0) // only error code is important
      {
         return true;
      }
      return false;
   }

   template<typename E>
   bool verify(const int i)
   {
      if(detect<E>(i))
      {
         if(get((E)i) != origin[].get((E)i))
         {
            if(!warningsOff)
            {
               Print("Fixing ", EnumToString((E)i), ": ", get((E)i), " <<< ", origin[].get((E)i));
            }
            set((E)i);
            fixes++;
         }
         return true;
      }
      return false;
   }

public:
   CustomSymbolMonitor(): SymbolMonitor(), origin(NULL), warningsOff(false) { }
   CustomSymbolMonitor(const string s, const SymbolMonitor *m = NULL): SymbolMonitor(s), origin(m), warningsOff(false) { }
   CustomSymbolMonitor(const string s, const string other): SymbolMonitor(s), origin(new SymbolMonitor(other)), warningsOff(false) { }
   
   void inherit(const SymbolMonitor &m)
   {
      origin = &m;
   }
   
   bool setAll(const bool reverseOrder = true, const int limit = UCHAR_MAX)
   {
      int properties[];
      ArrayResize(properties, limit);
      for(int i = 0; i < limit; ++i)
      {
         properties[i] = reverseOrder ? limit - i - 1 : i; // NB: number of errors differs in direct and reverse order
      }
      warningsOff = true;
      errorCount = 0;
      mismatchCount = 0;
      const bool success = set(properties);
      warningsOff = false;
      Print("Errors: ", errorCount);
      Print("Mismatches: ", mismatchCount);
      return success;
   }
   
   int verifyAll(const int limit = UCHAR_MAX)
   {
      int properties[];
      ArrayResize(properties, limit);
      for(int i = 0; i < limit; ++i)
      {
         properties[i] = i;
      }
      return verify(properties);
   }
   
   int verify(const int &properties[])
   {
      if(origin[] == NULL) return 0;
      fixes = 0;
      for(int i = 0; i < ArraySize(properties); ++i)
      {
         if(verify<ENUM_SYMBOL_INFO_INTEGER>(properties[i])
         || verify<ENUM_SYMBOL_INFO_DOUBLE>(properties[i])
         || verify<ENUM_SYMBOL_INFO_STRING>(properties[i]))
         {
            // ok
         }
         else
         {
            break; // unknown property: should not happen
         }
      }
      return fixes;
   }
   
   bool set(const int &properties[])
   {
      bool success = true;
      for(int i = 0; i < ArraySize(properties); ++i)
      {
         if(detect<ENUM_SYMBOL_INFO_INTEGER>(properties[i]))
         {
            success = set((ENUM_SYMBOL_INFO_INTEGER)properties[i]) && success;
         }
         else if(detect<ENUM_SYMBOL_INFO_DOUBLE>(properties[i]))
         {
            success = set((ENUM_SYMBOL_INFO_DOUBLE)properties[i]) && success;
         }
         else if(detect<ENUM_SYMBOL_INFO_STRING>(properties[i]))
         {
            success = set((ENUM_SYMBOL_INFO_STRING)properties[i]) && success;
         }
         else if(!warningsOff)
         {
            Print("Unresolved int value as enum: ", i, " for ", name);
         }
      }
      return success;
   }
   
   template<typename E>
   bool set(const E e)
   {
      if(origin[])
      {
         ResetLastError();
         const bool result = set(e, origin[].get(e));
         PrintFormat("%s %s %s -> %s (%d)", (result ? "true " : "false"), EnumToString(e), (string)origin[].get(e), E2S(_LastError), _LastError);
         if(result)
         {
            if(this.get(e) != origin[].get(e))
            {
               mismatchCount++;
               Print("!!!Mismatch!!! ", this.get(e), " <-> ", origin[].get(e));
            }
         }
         else
         {
            errorCount++;
         }
         return result;
      }
      return false;
   }

   bool set(const ENUM_SYMBOL_INFO_INTEGER property, const long value) const
   {
      return CustomSymbolSetInteger(name, property, value);
   }

   bool set(const ENUM_SYMBOL_INFO_DOUBLE property, const double value) const
   {
      return CustomSymbolSetDouble(name, property, value);
   }

   bool set(const ENUM_SYMBOL_INFO_STRING property, const string value) const
   {
      return CustomSymbolSetString(name, property, value);
   }
};

//+------------------------------------------------------------------+
//| Example                                                          |
//+------------------------------------------------------------------+
/* 
#include <MQL5Book/CustomSymbolMonitor.mqh>

void OnStart()
{
   const string custom = "ABCXYZ";
   if(CustomSymbolCreate(custom, "", _Symbol))
   {
      CustomSymbolMonitor cs(custom, _Symbol);
      int p[] = {SYMBOL_CURRENCY_BASE, SYMBOL_CURRENCY_PROFIT};
      Print("Try 1 (may fail if _Symbol has SYMBOL_CALC_MODE_FOREX)");
      cs.verify(p);
      Print("Try 2 (will succeed)");
      cs.set(SYMBOL_TRADE_CALC_MODE, SYMBOL_CALC_MODE_CFD);
      cs.set(p);
      cs.verify(p);
   }
   else
   {
      PrintFormat("Can't create symbol '%s': %s", custom, E2S(_LastError));
   }
}
*/
//+------------------------------------------------------------------+
