//+------------------------------------------------------------------+
//|                                                   SeriesInfo.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(SeriesInfoInteger(NULL, 0, SERIES_BARS_COUNT));
   PRTF((datetime)SeriesInfoInteger(NULL, 0, SERIES_FIRSTDATE));
   PRTF((datetime)SeriesInfoInteger(NULL, 0, SERIES_LASTBAR_DATE));
   PRTF((bool)SeriesInfoInteger(NULL, 0, SERIES_SYNCHRONIZED));
   PRTF((datetime)SeriesInfoInteger(NULL, 0, SERIES_SERVER_FIRSTDATE));
   PRTF((datetime)SeriesInfoInteger(NULL, 0, SERIES_TERMINAL_FIRSTDATE));
   PRTF(SeriesInfoInteger("ABRACADABRA", 0, SERIES_BARS_COUNT));
   /*
      output example
      
      SeriesInfoInteger(NULL,0,SERIES_BARS_COUNT)=10001 / ok
      (datetime)SeriesInfoInteger(NULL,0,SERIES_FIRSTDATE)=2020.03.02 10:00:00 / ok
      (datetime)SeriesInfoInteger(NULL,0,SERIES_LASTBAR_DATE)=2021.10.08 14:00:00 / ok
      (bool)SeriesInfoInteger(NULL,0,SERIES_SYNCHRONIZED)=false / ok
      (datetime)SeriesInfoInteger(NULL,0,SERIES_SERVER_FIRSTDATE)=1971.01.04 00:00:00 / ok
      (datetime)SeriesInfoInteger(NULL,0,SERIES_TERMINAL_FIRSTDATE)=2016.06.01 00:00:00 / ok
      SeriesInfoInteger(ABRACADABRA,0,SERIES_BARS_COUNT)=0 / MARKET_UNKNOWN_SYMBOL(4301)
   */
}
//+------------------------------------------------------------------+
