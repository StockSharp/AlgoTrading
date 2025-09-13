//+------------------------------------------------------------------+
//|                                          CalendarChangeSaver.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Periodically request and save calendar change IDs and their timestamps."
#property service

#define PACK_DATETIME_COUNTER(D,C) (D | (((ulong)(C)) << 48))
#define DATETIME(A) ((datetime)((A) & 0x7FFFFFFFF))
#define COUNTER(A)  ((ushort)((A) >> 48))
#define BULK_LIMIT  100

input string Filename = "calendar.chn";
input int PeriodMsc = 1000;

//+------------------------------------------------------------------+
//| Generate user-friendly short description of the event            |
//+------------------------------------------------------------------+
string Description(const MqlCalendarValue &value)
{
   MqlCalendarEvent event;
   MqlCalendarCountry country;
   CalendarEventById(value.event_id, event);
   CalendarCountryById(event.country_id, country);
   return StringFormat("%lld (%s/%s @ %s)",
      value.id, country.code, event.name, TimeToString(value.time));
}

//+------------------------------------------------------------------+
//| Service program start function                                   |
//+------------------------------------------------------------------+
void OnStart()
{
   bool online = true;
   ulong change = 0, last = 0;
   int count = 0;
   int handle = FileOpen(Filename,
      FILE_WRITE | FILE_READ | FILE_SHARE_WRITE | FILE_SHARE_READ | FILE_BIN);
   if(handle == INVALID_HANDLE)
   {
      PrintFormat("Can't open file '%s' for writing", Filename);
      return;
   }
   
   const ulong p = FileSize(handle);
   if(p > 0)
   {
      PrintFormat("Resuming file %lld bytes", p);
      FileSeek(handle, 0, SEEK_END);
   }

   Print("Requesting start ID...");
   
   while(!IsStopped())
   {
      if(!TerminalInfoInteger(TERMINAL_CONNECTED))
      {
         if(online)
         {
            Print("Waiting for connection...");
            online = false;
         }
         Sleep(PeriodMsc);
         continue;
      }
      else if(!online)
      {
         Print("Connected");
         online = true;
      }
      
      MqlCalendarValue values[];
      const int n = CalendarValueLast(change, values);
      if(n > 0)
      {
         // check for unreliable responce from terminal,
         // when outdated irrelevant changes are returned,
         // which usually happens just after terminal start-up
         if(n >= BULK_LIMIT)
         {
            Print("New change ID: ", change);
            PrintFormat("Too many records (%d), malfunction assumed, skipping", n);
         }
         else
         {
            string records = "[" + Description(values[0]);
            for(int i = 1; i < n; ++i)
            {
               records += "," + Description(values[i]);
            }
            records += "]";
            Print("New change ID: ", change, " ",
               TimeToString(TimeTradeServer(), TIME_DATE | TIME_SECONDS), "\n", records);
            FileWriteLong(handle, PACK_DATETIME_COUNTER(TimeTradeServer(), n));
            for(int i = 0; i < n; ++i)
            {
               FileWriteLong(handle, values[i].id);
            }
            FileFlush(handle);
            ++count;
         }
      }
      else if(_LastError == 0)
      {
         if(!last && change)
         {
            Print("Start change ID obtained: ", change);
         }
      }
      
      last = change;
      Sleep(PeriodMsc);
   }
   PrintFormat("%d records added", count);
   FileClose(handle);
}
//+------------------------------------------------------------------+
