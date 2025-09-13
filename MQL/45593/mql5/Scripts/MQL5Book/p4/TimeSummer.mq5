//+------------------------------------------------------------------+
//|                                                   TimeSummer.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include "PRTF.mqh"
#include <MQL5Book/DateTime.mqh>

//+------------------------------------------------------------------+
//| Server time zone and DST information                             |
//+------------------------------------------------------------------+
struct ServerTime
{
   int offsetGMT;      // time zone offset in seconds against UTC/GMT
   int offsetDST;      // DST correction in seconds (included in offsetGMT)
   bool supportDST;    // DST changes are detected in the quotes
   string description; // textual explanation of result
};

//+------------------------------------------------------------------+
//| Estimate server time zone and DST mode from H1 quotes history    |
//+------------------------------------------------------------------+
ServerTime ServerTimeZone(const string symbol = NULL)
{
  const int year = 365 * 24 * 60 * 60;
  datetime array[];
  if(PRTF(CopyTime(symbol, PERIOD_H1, TimeCurrent() - year, TimeCurrent(), array)) > 0)
  {
     // approx. 6000 bars should be acquired here
     const int n = ArraySize(array);
     PrintFormat("Got %d H1 bars, ~%d days", n, n / 24);

     int hours[24] = {};
     int current = 0;
     for(int i = 0; i < n; ++i)
     {
        const ENUM_DAY_OF_WEEK weekday = TimeDayOfWeek(array[i]);
        // skip all days except Sunday and Monday
        if(weekday > MONDAY) continue;
        // lets analyze the first H1 bar of the trading week
        // find out an hour for the first bar after weekend
        current = _TimeHour();
        // collect stats for opening hours
        hours[current]++;
        
        // now skip 2 days to check next week
        i += 48;
     }
     
     Print("Week opening hours stats:");
     ArrayPrint(hours);
     
     if(hours[current] <= 52 / 4)
     {
        // TODO: refine results using previous weeks
        Print("Extraordinary week detected");
     }
     
     // find most often time shift
     int max = ArrayMaximum(hours);
     // now check if a secondary time shift was also happened
     hours[max] = 0;
     int sub = ArrayMaximum(hours);

     // result variable init
     ServerTime st = {};
     int DST = 0;

     // winter/summer periods are not equal!
     // summer time can be 8 months per year
     // 52 is the full year in weeks
     if(hours[sub] > 52 / 4) // DST is supported
     {
        if(current == max || current == sub)
        {
           if(current == MathMin(max, sub))
              DST = fabs(max - sub); // DST is enabled now
        }
        st.supportDST = true;
     }
     
     current += 2 + DST; // +2 to get UTC offset
     // NB. when DST is enabled, time is shifted forward in the time zone,
     // but for outside it is looked like the time is shifted back
     // (everything in this time zone is happened earlier for others)
     // so to get standard time of the zone we need to add DST
     current %= 24;
     
     // time zones are in the range [UTC-12,UTC+12]
     if(current > 12) current = current - 24;
     current += DST; // if DST is enabled it's shown in offsetGMT according to MT5 rules
     
     st.description = StringFormat("Server time offset: UTC%+d, including DST%+d", current, DST);
     st.offsetGMT = -current * 3600;
     st.offsetDST = -DST * 3600;

     return st;
  }
  ServerTime empty = {-INT_MAX, -INT_MAX, false};
  return empty;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(TimeLocal());
   PRTF(TimeCurrent());
   PRTF(TimeTradeServer());
   PRTF(TimeGMT());
   PRTF(TimeGMTOffset());
   PRTF(TimeDaylightSavings());
   ServerTime st = ServerTimeZone();
   Print(st.description);
   Print("ServerGMTOffset: ", st.offsetGMT);
   Print("ServerTimeDaylightSavings: ", st.offsetDST);
}
//+------------------------------------------------------------------+
/*
   example output MQ Demo
   (WARNING: TimeGMT != ServerGMT,
   because TimeGMT() - TimeCurrent() gives 3 hours difference,
   whereas ServerGMTOffset is 2 hours difference;
   probably DST is enabled all year round)
   
   TimeLocal()=2021.09.09 22:06:17 / ok
   TimeCurrent()=2021.09.09 22:06:10 / ok
   TimeTradeServer()=2021.09.09 22:06:17 / ok
   TimeGMT()=2021.09.09 19:06:17 / ok
   TimeGMTOffset()=-10800 / ok
   TimeDaylightSavings()=0 / ok
   CopyTime(symbol,PERIOD_H1,TimeCurrent()-year,TimeCurrent(),array)=6207 / ok
   Got 6207 H1 bars, ~258 days
   Week opening hours stats:
   52  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0  0
   Server time offset: UTC+2, including DST+0
   ServerGMTOffset: -7200
   ServerTimeDaylightSavings: 0
   
*/