//+------------------------------------------------------------------+
//|                                                       Timing.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Track elapsed and remaining time for lengthy operations          |
//+------------------------------------------------------------------+
class Timing
{
public:
   const uint start;
   
   Timing(): start(GetTickCount()) { }
   
   static string stringify(uint sec)
   {
      uint min = sec / 60;
      if(min > 0)
      {
         sec %= 60;
         uint hour = min / 60;
         if(hour > 0)
         {
            min %= 60;
            if(hour < 24)
            {
               return StringFormat("%02d:%02d:%02d", hour, min, sec);
            }
            
            uint days = hour / 24;
            hour %= 24;
            return StringFormat("%d days %02d:%02d:%02d", days, hour, min, sec);
         }
         return StringFormat("%02d:%02d", min, sec);
      }
      return StringFormat("00:%02d", sec);
   }
   
   string elapsed() const
   {
      const uint sec = (GetTickCount() - start) / 1000;
      return stringify(sec);
   }
   
   string remain(const float percent) const
   {
      if(percent == 0.0 || percent == 1.0) return "n/a";
      const uint sec = (GetTickCount() - start) / 1000;
      return stringify((uint)(sec / percent * (1 - percent)));
   }
};
//+------------------------------------------------------------------+
