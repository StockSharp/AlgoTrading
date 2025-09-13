//+------------------------------------------------------------------+
//|                                                 QuoteRefresh.mqh |
//|                                    Copyright (c) 2021, Marketeer |
//+------------------------------------------------------------------+
#ifndef PRTF
#define PRTF
#endif
//+----------------------------------------------------------------------+
//| Helper function to ensure data is downloaded and timeseries is built |
//+----------------------------------------------------------------------+
bool QuoteRefresh(const string asset, const ENUM_TIMEFRAMES period, const datetime start)
{
   if(MQL5InfoInteger(MQL5_PROGRAM_TYPE) == PROGRAM_INDICATOR
      && _Symbol == asset && _Period == period)
   {
      return (bool)SeriesInfoInteger(asset, period, SERIES_SYNCHRONIZED);
   }

   if(Bars(asset, period) >= TerminalInfoInteger(TERMINAL_MAXBARS))
   {
      return (bool)SeriesInfoInteger(asset, period, SERIES_SYNCHRONIZED);
   }
   
   datetime times[1];
   datetime first = 0, server = 0;
   if(PRTF(SeriesInfoInteger(asset, period, SERIES_FIRSTDATE, first)))
   {
      if(first > 0 && first <= start)
      {
         // applied data is present, it's already or going to be synchronized
         return (bool)SeriesInfoInteger(asset, period, SERIES_SYNCHRONIZED);
      }
      else
      if(PRTF(SeriesInfoInteger(asset, period, SERIES_TERMINAL_FIRSTDATE, first)))
      {
         if(first > 0 && first <= start)
         {
            // raw data is present in the terminal base, need to build timeseries
            return PRTF(CopyTime(asset, period, first, 1, times)) == 1;
         }
         else
         {
            if(PRTF(SeriesInfoInteger(asset, period, SERIES_SERVER_FIRSTDATE, server)))
            {
               // raw data is present on the server, need to request it
               if(first > 0 && first < server)
                  PrintFormat("Warning: %s first date %s on server is less than on terminal ",
                     asset, TimeToString(server), TimeToString(first));
               // can't request more than the server has
               return PRTF(CopyTime(asset, period, fmax(start, server), 1, times)) == 1;
            }
         }
      }
   }
   
   return false;
}
//+----------------------------------------------------------------------+
