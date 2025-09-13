//+------------------------------------------------------------------+
//|                                         CalendarFilterCached.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/CalendarFilter.mqh>
#include <MQL5Book/CalendarCache.mqh>
#include <MQL5Book/AutoPtr.mqh>

//+------------------------------------------------------------------+
//| Calendar filter based on cached data                             |
//+------------------------------------------------------------------+
class CalendarFilterCached: public CalendarFilter
{
protected:
   AutoPtr<CalendarCache> cache;

   virtual bool calendarCountryById(ulong country_id, MqlCalendarCountry &cnt) override
   {
      return cache[].calendarCountryById(country_id, cnt);
   }
   
   virtual bool calendarEventById(ulong event_id, MqlCalendarEvent &event) override
   {
      return cache[].calendarEventById(event_id, event);
   }

   virtual int calendarValueHistoryByEvent(ulong event_id, MqlCalendarValue &temp[],
      datetime _from, datetime _to = 0) override
   {
      return cache[].calendarValueHistoryByEvent(event_id, temp, _from, _to);
   }
   
   virtual int calendarValueHistory(MqlCalendarValue &temp[],
      datetime _from, datetime _to = 0,
      const string _code = NULL, const string _coin = NULL) override
   {
      return cache[].calendarValueHistory(temp, _from, _to, _code, _coin);
   }

   virtual int calendarValueLast(ulong &_change, MqlCalendarValue &result[],
      const string _code = NULL, const string _coin = NULL) override
   {
      return cache[].calendarValueLast(_change, result, _code, _coin);
   }

   virtual int calendarValueLastByEvent(ulong event_id, ulong &_change, MqlCalendarValue &result[]) override
   {
      return cache[].calendarValueLastByEvent(event_id, _change, result);
   }

public:   
   CalendarFilterCached(CalendarCache *_cache): cache(_cache),
      CalendarFilter(_cache.getContext(), _cache.getFrom(), _cache.getTo())
   {
   }

   virtual bool isLoaded() const override
   {
      // if cache is involved, return it's state as true/false
      return cache[].isLoaded();
   }
};
//+------------------------------------------------------------------+
