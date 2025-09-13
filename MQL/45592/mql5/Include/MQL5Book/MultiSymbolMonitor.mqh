//+------------------------------------------------------------------+
//|                                           MultiSymbolMonitor.mqh |
//|                               Copyright (c) 2019-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
#include <MQL5Book/MapArray.mqh>

//+------------------------------------------------------------------+
//| Detect opening time of latest bars for list of symbols           |
//+------------------------------------------------------------------+
class MultiSymbolMonitor
{
protected:
   ENUM_TIMEFRAMES period;
   MapArray<string,datetime> lastTime;

public:
   MultiSymbolMonitor(): period(_Period) {}
   MultiSymbolMonitor(const ENUM_TIMEFRAMES p): period(p) {}
   
   // add new symbol to monitor 
   bool attach(const string symbol)
   {
      if(lastTime.getSize() < sizeof(ulong) * 8) // 64
      {
         lastTime.put(symbol, NULL);
         return true;
      }
      return false;
   }
   
   // calculate new state of timestamps
   void refresh()
   {
      for(int i = 0; i < lastTime.getSize(); i++)
      {
         const string symbol = lastTime.getKey(i);
         const datetime dt = iTime(symbol, period, 0);
         lastTime.put(symbol, dt);
      }
   }
   
   // construct and return bitmask of changes
   ulong check(const bool refresh = false)
   {
      ulong flags = 0;
      for(int i = 0; i < lastTime.getSize(); i++)
      {
         const string symbol = lastTime.getKey(i);
         const datetime dt = iTime(symbol, period, 0);
        
         if(dt != lastTime[symbol])
         {
            flags |= 1 << i;
         }
         
         if(refresh) // update timestamp
         {
            lastTime.put(symbol, dt);
         }
      }
      return flags;
   }
   
   // return a list of symbol names in the bitmask of changes
   string describe(ulong flags = 0)
   {
      string message = "";
      if(flags == 0) flags = check();
      for(int i = 0; i < lastTime.getSize(); i++)
      {
         if((flags & (1 << i)) != 0)
         {
            message += lastTime.getKey(i) + "\t";
         }
      }
      return message;
   }
   
   // check if all bars have the same timestamp
   bool inSync() const
   {
      if(lastTime.getSize() == 0) return false;
      const datetime first = lastTime[0];
      for(int i = 1; i < lastTime.getSize(); i++)
      {
         if(first != lastTime[i]) return false;
      }
      return true;
   }
    
   void reset()
   {
      lastTime.reset();
   }
};
//+------------------------------------------------------------------+
