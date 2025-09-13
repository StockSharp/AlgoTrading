//+------------------------------------------------------------------+
//|                                               CalendarFilter.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

/*

   Filters:
   
    event ID
    country code
    currency code
    type
    sector
    importance
    date range (day/week/month/quarter in past/future)
    has (actual, forecast, previous, revised)
    event name full-text
    other properties
    
   May be applied multiple per property via OR/AND with EQUAL, NOT_EQUAL, GREATER conditions

   examples:
      CalendarFilter f;
      f.let(CALENDAR_IMPORTANCE_MODERATE, GREATER) // high priority news
       .let(CALENDAR_IMPACT_POSITIVE) // only with better actual value than expected
       .let(CALENDAR_TIMEMODE_DATETIME) // only events with exact timing
       .let("DE").let("FR") // collect 2 countries, OR...
       .let("USD").let("GBP") // ...collect 2 currencies (but do not combine these 2 conditions!)
       .let(TimeCurrent() - MONTH_LONG, TimeCurrent() + WEEK_LONG) // date range
       .let("farm"); // full-text search
      
      MqlCalendarValue records[];
      f.select(records);

*/

#include <MQL5Book/Defines.mqh>
#include <MQL5Book/IS.mqh>
#include <MQL5Book/CalendarDefines.mqh>
#include <MQL5Book/QuickSortStructT.mqh>

#ifdef LOGGING
#include <MQL5Book/PRTF.mqh>
#else
#define PRTF
#endif

//+------------------------------------------------------------------+
//| Calendar filter class                                            |
//+------------------------------------------------------------------+
class CalendarFilter
{
protected:
   // initial (optional) selectors passed via ctor, invariants
   string context;
   datetime from, to;
   bool fixedDates;       // true if 'from'/'to' assigned in ctor and can't be changed
   
   // common selectors for event fields, assigned via let-methods
   long selectors[][3];   // [0] - property, [1] - value, [2] - condition
   string stringCache[];  // cache of all strings in 'selectors'
   
   // specific selectors (country/currency/id/change)
   ulong change;          // for last changes requests
   string country[], currency[];
   ulong ids[];           // kinds of events
   
   //+------------------------------------------------------------------+
   //| Block of virtual methods for calendar access                     |
   //+------------------------------------------------------------------+
   
   virtual bool calendarCountryById(ulong country_id, MqlCalendarCountry &cnt)
   {
      return CalendarCountryById(country_id, cnt);
   }
   
   virtual bool calendarEventById(ulong event_id, MqlCalendarEvent &event)
   {
      return CalendarEventById(event_id, event);
   }

   virtual int calendarValueHistoryByEvent(ulong event_id, MqlCalendarValue &temp[],
      datetime _from, datetime _to = 0)
   {
      return CalendarValueHistoryByEvent(event_id, temp, _from, _to);
   }
   
   virtual int calendarValueHistory(MqlCalendarValue &temp[],
      datetime _from, datetime _to = 0,
      const string _code = NULL, const string _coin = NULL)
   {
      return CalendarValueHistory(temp, _from, _to, _code, _coin);
   }

   virtual int calendarValueLast(ulong &_change, MqlCalendarValue &result[],
      const string _code = NULL, const string _coin = NULL)
   {
      return CalendarValueLast(_change, result, _code, _coin);
   }

   virtual int calendarValueLastByEvent(ulong event_id, ulong &_change, MqlCalendarValue &result[])
   {
      return CalendarValueLastByEvent(event_id, _change, result);
   }
   
   virtual void init()
   {
      fixedDates = from != 0 || to != 0;
      change = 0;
      if(StringLen(context) == 3)
      {
         PUSH(currency, context);
      }
      else
      {
         // even if context is NULL, we save it to request entire calendar
         PUSH(country, context);
      }
   }

   //+------------------------------------------------------------------+
   //| Block of overloads to resolve specific ENUM into PROPERTY        |
   //+------------------------------------------------------------------+

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_TYPE e)
   {
      return CALENDAR_PROPERTY_EVENT_TYPE;
   }

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_SECTOR e)
   {
      return CALENDAR_PROPERTY_EVENT_SECTOR;
   }
   
   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_FREQUENCY e)
   {
      return CALENDAR_PROPERTY_EVENT_FREQUENCY;
   }

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_TIMEMODE e)
   {
      return CALENDAR_PROPERTY_EVENT_TIMEMODE;
   }

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_UNIT e)
   {
      return CALENDAR_PROPERTY_EVENT_UNIT;
   }

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_IMPORTANCE e)
   {
      return CALENDAR_PROPERTY_EVENT_IMPORTANCE;
   }

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_MULTIPLIER e)
   {
      return CALENDAR_PROPERTY_EVENT_MULTIPLIER;
   }

   ENUM_CALENDAR_PROPERTY resolve(const ENUM_CALENDAR_EVENT_IMPACT e)
   {
      return CALENDAR_PROPERTY_RECORD_IMPACT;
   }

   //+------------------------------------------------------------------+
   //| Comparison methods for scalars of different prop types           |
   //+------------------------------------------------------------------+

   template<typename V>
   static bool equal(const V v1, const V v2)
   {
      return v1 == v2;
   }

   template<typename V>
   static bool greater(const V v1, const V v2)
   {
      return v1 > v2;
   }

   static bool equal(const string v1, const string v2)
   {
      if(StringFind(v2, "*") > -1)
      {
         int previous = 0;
         string words[];
         const int n = StringSplit(v2, '*', words);
         for(int i = 0; i < n; ++i)
         {
            if(StringLen(words[i]) == 0) continue;
            int index = StringFind(v1, words[i], previous);
            if(index == -1)
            {
               return false;
            }
            previous = index + StringLen(words[i]);
         }
         return true;
      }
      else if(v2[0] == '\'' && v2[StringLen(v2) - 1] == '\'')
      {
         return v1 == v2;
      }
      return StringFind(v1, v2) > -1;
   }

   //+------------------------------------------------------------------+
   //| Internal filtering stuff (condition checks)                      |
   //+------------------------------------------------------------------+

   void filter(MqlCalendarValue &result[])
   {
      #ifdef LOGGING
      PrintFormat("Filtering %d records", ArraySize(result));
      #endif
      for(int i = ArraySize(result) - 1; i >= 0; --i)
      {
         if(!match(result[i]))
         {
            ArrayRemove(result, i, 1);
         }
      }
   }

   bool match(const MqlCalendarValue &v)
   {
      int or_totals = 0, or_matches = 0;

      MqlCalendarEvent event;
      if(!calendarEventById(v.event_id, event)) return false;
      
      for(int j = 0; j < ArrayRange(selectors, 0); ++j)
      {
         // retrieve selector value
         long field = 0;
         string text = NULL;
         
         switch((int)selectors[j][0])
         {
         // NB: multiple countries or currencies are supported by
         // multiple calls to Calendar-functions, so records presented to 'match'
         // are already selected according to the filter
         // CALENDAR_PROPERTY_COUNTRY_CODE,     // +string (2 chars)
         // CALENDAR_PROPERTY_COUNTRY_CURRENCY, // +string (3 chars)
         case CALENDAR_PROPERTY_EVENT_ID:
            field = (long)event.id;
            break;
         case CALENDAR_PROPERTY_EVENT_TYPE:
            field = event.type;
            break;
         case CALENDAR_PROPERTY_EVENT_SECTOR:
            field = event.sector;
            break;
         case CALENDAR_PROPERTY_EVENT_FREQUENCY:
            field = event.frequency;
            break;
         case CALENDAR_PROPERTY_EVENT_TIMEMODE:
            field = event.time_mode;
            break;
         case CALENDAR_PROPERTY_EVENT_UNIT:
            field = event.unit;
            break;
         case CALENDAR_PROPERTY_EVENT_IMPORTANCE:
            field = event.importance;
            break;
         case CALENDAR_PROPERTY_EVENT_MULTIPLIER:
            field = event.multiplier;
            break;
         case CALENDAR_PROPERTY_EVENT_SOURCE:
            text = event.source_url;
            break;
         case CALENDAR_PROPERTY_EVENT_NAME:
            text = event.name;
            break;
         case CALENDAR_PROPERTY_RECORD_REVISION:
            field = v.revision;
            break;
         case CALENDAR_PROPERTY_RECORD_IMPACT:
            field = v.impact_type;
            break;
         case CALENDAR_PROPERTY_RECORD_ACTUAL:
            field = v.actual_value;
            break;
         case CALENDAR_PROPERTY_RECORD_PREVIOUS:
            field = v.prev_value;
            break;
         case CALENDAR_PROPERTY_RECORD_REVISED:
            field = v.revised_prev_value;
            break;
         case CALENDAR_PROPERTY_RECORD_PREVISED:
            field = v.revised_prev_value != LONG_MIN ? v.revised_prev_value : v.prev_value;
            break;
         case CALENDAR_PROPERTY_RECORD_FORECAST:
            field = v.forecast_value;
            break;
         case CALENDAR_PROPERTY_RECORD_PERIOD:
            field = v.period;
            break;
         }
         
         // compare the value with the record
         if(text == NULL)
         {
            switch((IS)selectors[j][2])
            {
            case EQUAL:
               if(!equal(field, selectors[j][1])) return false;
               break;
            case NOT_EQUAL:
               if(equal(field, selectors[j][1])) return false;
               break;
            case GREATER:
               if(!greater(field, selectors[j][1])) return false;
               break;
            case LESS:
               if(greater(field, selectors[j][1])) return false;
               break;
            case OR_EQUAL:
               or_totals++;
               if(equal(field, selectors[j][1])) or_matches++;
               break;
            }
         }
         else
         {
            const string find = stringCache[(int)selectors[j][1]];
            switch((IS)selectors[j][2])
            {
            case EQUAL:
               if(!equal(text, find)) return false;
               break;
            case NOT_EQUAL:
               if(equal(text, find)) return false;
               break;
            case GREATER:
               if(!greater(text, find)) return false;
               break;
            case LESS:
               if(greater(text, find)) return false;
               break;
            case OR_EQUAL:
               or_totals++;
               if(equal(text, find)) or_matches++;
               break;
            }
         }
      }
      
      if(or_totals > 0) return or_matches > 0;
      
      return true;
   }

   int insertDelimiter(MqlCalendarValue &result[])
   {
      static const MqlCalendarValue empty[1] = {};
      for(int i = 1; i < ArraySize(result); ++i)
      {
         if(result[i].time > TimeTradeServer() && result[i - 1].time <= TimeTradeServer())
         {
            ArrayInsert(result, empty, i);
            return i;
         }
      }
      return -1;
   }

public:
   CalendarFilter(const string _context = NULL,
      const datetime _from = 0, const datetime _to = 0):
      context(_context), from(_from), to(_to)
   {
      init();
   }

   virtual bool isLoaded() const
   {
      return true; // will be overriden in descendants
   }

   void describe() const
   {
      Print("-= Calendar filter description =-");
      Print("Dates: ", from, "-", to);
      Print("Countries:");
      ArrayPrint(country);
      Print("Currencies:");
      ArrayPrint(currency);
      Print("Event IDs:");
      ArrayPrint(ids);
      Print("Selectors:");
      ArrayPrint(selectors);
      Print("Strings:");
      ArrayPrint(stringCache);
   }

   template<typename E>
   static string stringify(const E e)
   {
      string result = EnumToString(e);
      string words[];
      if(StringSplit(result, '_', words) > 0)
      {
         return words[ArraySize(words) - 1];
      }
      return result;
   }

   //+------------------------------------------------------------------+
   //| Block of let-methods to setup conditions on event properties     |
   //+------------------------------------------------------------------+

   CalendarFilter *let(const int r, const IS c = EQUAL)
   {
      const int n = EXPAND(selectors);
      selectors[n][0] = CALENDAR_PROPERTY_RECORD_REVISION;
      selectors[n][1] = r;
      selectors[n][2] = c;
      return &this;
   }

   CalendarFilter *let(const ulong event)
   {
      PUSH(ids, event);
      return &this;
   }

   CalendarFilter *let(const long value, const ENUM_CALENDAR_PROPERTY property,
      const IS c = EQUAL)
   {
      /*
         fields covered by this overload:
            CALENDAR_PROPERTY_RECORD_PERIOD,
            CALENDAR_PROPERTY_RECORD_ACTUAL,
            CALENDAR_PROPERTY_RECORD_PREVIOUS,
            CALENDAR_PROPERTY_RECORD_REVISED,
            CALENDAR_PROPERTY_RECORD_FORECAST,
      */
      const int n = EXPAND(selectors);
      selectors[n][0] = property;
      selectors[n][1] = value;
      selectors[n][2] = c;
      return &this;
   }
   
   template<typename E>
   CalendarFilter *let(const E e, const IS c = EQUAL)
   {
      // enumerations are processed here
      const int n = EXPAND(selectors);
      selectors[n][0] = resolve(e);
      selectors[n][1] = e;
      selectors[n][2] = c;
      return &this;
   }

   CalendarFilter *let(const datetime _from, const datetime _to = 0)
   {
      if(!fixedDates)  // we can narrow down complete date range,
      {                // but do not change it if already specified in ctor
         from = _from;
         to = _to;
      }
      else
      {
         PrintFormat("A limited date range %s-%s was cached in filter constructor,"
            " can't change it ad hoc", (string)from, (string)to);
      }
      return &this;
   }
   
   CalendarFilter *let(const string find, const IS c = EQUAL)
   {
      const int wildcard = (StringFind(find, "*") + 1) * 10;
      switch(StringLen(find) + wildcard)
      {
      case 0:
      case 1:
         break;
      case 2:
         // if initial context was other than country, we can mix it with a country 
         if(StringLen(context) != 2)
         {
            if(ArraySize(country) == 1 && StringLen(country[0]) == 0) // narrowing all countries to specific one
            {
               country[0] = find;
            }
            else
            {
               PUSH(country, find);
            }
         }
         else
         {
            PrintFormat("Specific country '%s' was cached in filter constructor,"
               " can't change it ad hoc", context);
         }
         break;
      case 3:
         if(StringLen(context) != 3) // initially selected currency can not be changed
         {
            PUSH(currency, find);
         }
         else
         {
            PrintFormat("Specific currency '%s' was cached in filter constructor,"
               " can't change it ad hoc", context);
         }
         break;
      default:
         {
            const int n = EXPAND(selectors);
            PUSH(stringCache, find);
            if(StringFind(find, "http://") == 0 || StringFind(find, "https://") == 0)
            {
               selectors[n][0] = CALENDAR_PROPERTY_EVENT_SOURCE;
            }
            else
            {
               selectors[n][0] = CALENDAR_PROPERTY_EVENT_NAME;
            }
            selectors[n][1] = ArraySize(stringCache) - 1;
            selectors[n][2] = c;
            break;
         }
      }
      
      return &this;
   }
   
   void reset()
   {
      ArrayFree(selectors);
      ArrayFree(stringCache);
      ArrayFree(ids);
      ArrayFree(country);
      ArrayFree(currency);
      if(context != NULL)
      {
         if(StringLen(context) == 3) PUSH(currency, context);
         else PUSH(country, context);
      }
      if(!fixedDates)
      {
         from = to = 0;
      }
      change = 0;
   }
   
   //+------------------------------------------------------------------+
   //| Main public methods                                              |
   //+------------------------------------------------------------------+

   bool select(MqlCalendarValue &result[], const bool delimiter = false, const int limit = -1)
   {
      int count = 0;
      #ifdef LOGGING
      Print("Selecting calendar records... (now:", TimeCurrent(), ", server:", TimeTradeServer(), ")");
      #endif
      ArrayFree(result);
      if(ArraySize(ids)) // some kinds of events specified
      {
         for(int i = 0; i < ArraySize(ids); ++i)
         {
            #ifdef LOGGING
            PRTF(ids[i]);
            #endif
            MqlCalendarValue temp[];
            if(PRTF(calendarValueHistoryByEvent(ids[i], temp, from, to)))
            {
               ArrayCopy(result, temp, ArraySize(result));
               ++count;
            }
         }
      }
      else
      {
         // many countries or currencies
         if(ArraySize(country) > ArraySize(currency))
         {
            const string c = ArraySize(currency) > 0 ? currency[0] : NULL;
            for(int i = 0; i < ArraySize(country); ++i)
            {
               #ifdef LOGGING
               PRTF(country[i]);
               #endif
               MqlCalendarValue temp[];
               if(PRTF(calendarValueHistory(temp, from, to, country[i], c)))
               {
                  ArrayCopy(result, temp, ArraySize(result));
                  ++count;
               }
            }
         }
         else
         {
            const string c = ArraySize(country) > 0 ? country[0] : NULL;
            for(int i = 0; i < ArraySize(currency); ++i)
            {
               #ifdef LOGGING
               PRTF(currency[i]);
               #endif
               MqlCalendarValue temp[];
               if(PRTF(calendarValueHistory(temp, from, to, c, currency[i])))
               {
                  ArrayCopy(result, temp, ArraySize(result));
                  ++count;
               }
            }
         }
      }
      
      // get current change_id
      change = 0;
      MqlCalendarValue dummy[];
      calendarValueLast(change, dummy);

      if(ArraySize(result) > 0)
      {
         filter(result);
      }
      
      if(count > 1 && ArraySize(result) > 1)
      {
         #ifdef LOGGING
         PrintFormat("Sorting %d records by time", ArraySize(result));
         #endif
         SORT_STRUCT(MqlCalendarValue, result, time);
      }

      if(ArraySize(result) > 1)
      {
         int now = -1;
         if(delimiter)
         {
            now = insertDelimiter(result);
         }
         
         if(limit > 0 && limit < ArraySize(result))
         {
            #ifdef LOGGING
            PrintFormat("Limit %d is exceeded with %d records ('now' is at index %d)",
               limit, ArraySize(result), now);
            #endif
            if(now == -1)
            {
               ArrayResize(result, limit);
            }
            else
            {
               int start = fmax(now - limit, 0);
               int stop = fmin(now + limit, ArraySize(result) - 1);
               
               if(stop - start > limit)
               {
                  const int excess = (stop - start - limit) / 2;
                  const int odd = (stop - start - limit) % 2;
                  start += excess;
                  stop -= excess + odd;
               }

               #ifdef LOGGING
               PrintFormat("Removing records outside %d to %d", start, stop);
               #endif
               if(stop < ArraySize(result)) ArrayRemove(result, stop);
               if(start > 0) ArrayRemove(result, 0, start);
            }
         }
      }
      
      #ifdef LOGGING
      PrintFormat("Got %d records", ArraySize(result));
      #endif
      
      return ArraySize(result) > 0;
   }
   
   bool update(MqlCalendarValue &result[])
   {
      ArrayFree(result);

      // 'update' is only applicable after 'select' call or previous 'update'
      if(change == 0)
      {
         calendarValueLast(change, result);
         return false;
      }
      
      int count = 0;           // number of combined requests with results
      ulong _change = 0;       // change placeholder during partial requests
      ulong new_change = 0;    // final change id after all requests done
      if(ArraySize(ids))
      {
         for(int i = 0; i < ArraySize(ids); ++i)
         {
            MqlCalendarValue temp[];
            _change = change;
            if(calendarValueLastByEvent(ids[i], _change, temp))
            {
               ArrayCopy(result, temp, ArraySize(result));
               ++count;
            }
            new_change = fmax(new_change, _change);
         }
      }
      else
      {
         // many countries or currencies
         if(ArraySize(country) > ArraySize(currency))
         {
            const string c = ArraySize(currency) > 0 ? currency[0] : NULL;
            for(int i = 0; i < ArraySize(country); ++i)
            {
               MqlCalendarValue temp[];
               _change = change;
               if(calendarValueLast(_change, temp, country[i], c))
               {
                  ArrayCopy(result, temp, ArraySize(result));
                  ++count;
               }
               new_change = fmax(new_change, _change);
            }
         }
         else
         {
            const string c = ArraySize(country) > 0 ? country[0] : NULL;
            for(int i = 0; i < ArraySize(currency); ++i)
            {
               MqlCalendarValue temp[];
               _change = change;
               if(calendarValueLast(_change, temp, c, currency[i]))
               {
                  ArrayCopy(result, temp, ArraySize(result));
                  ++count;
               }
               new_change = fmax(new_change, _change);
            }
         }
      }
      change = new_change;

      if(ArraySize(result) > 0)
      {
         filter(result);
      }
      
      if(count > 1 && ArraySize(result) > 1)
      {
         #ifdef LOGGING
         PrintFormat("Sorting %d records by time", ArraySize(result));
         #endif
         SORT_STRUCT(MqlCalendarValue, result, time);
      }
      
      return ArraySize(result) > 0;
   }
   
   ulong getChangeID() const
   {
      return change;
   }
   
   bool format(const MqlCalendarValue &data[],
      const ENUM_CALENDAR_PROPERTY &props[], string &result[],
      const bool padding = false, const bool header = false)
   {
      const int cols = ArraySize(props);
      const int rows = ArraySize(data);
      if(!cols || !(rows + header)) return false;
      if(ArrayResize(result, cols * (rows + header)) <= 0) return false;
      
      int c = 0;
      int widths[];

      if(header)
      {
         for(int j = 0; j < cols; ++j)
         {
            result[c++] = stringify(props[j]);
         }
         ArrayResize(widths, cols);
         ArrayInitialize(widths, 0);
      }
      
      for(int i = 0; i < rows; ++i)
      {
         MqlCalendarValue v = data[i];
         MqlCalendarEvent event = {};
         MqlCalendarCountry cnt = {};
         if(!v.event_id || !calendarEventById(v.event_id, event))
         {
            for(int j = 0; j < cols; ++j) result[c++] = ShortToString(0x2014);
            continue;
         }
         if(!event.country_id || !calendarCountryById(event.country_id, cnt))
         {
            for(int j = 0; j < cols; ++j) result[c++] = ShortToString(0x2014);
            continue;
         }

         for(int j = 0; j < cols; ++j)
         {
            // retrieve field value
            switch((int)props[j])
            {
            case CALENDAR_PROPERTY_COUNTRY_CODE:
               result[c++] = cnt.code;
               break;
            case CALENDAR_PROPERTY_COUNTRY_CURRENCY:
               result[c++] = cnt.currency;
               break;
            case CALENDAR_PROPERTY_EVENT_TYPE:
               result[c++] = stringify(event.type);
               break;
            case CALENDAR_PROPERTY_EVENT_SECTOR:
               result[c++] = stringify(event.sector);
               break;
            case CALENDAR_PROPERTY_EVENT_FREQUENCY:
               result[c++] = stringify(event.frequency);
               break;
            case CALENDAR_PROPERTY_EVENT_TIMEMODE:
               result[c++] = stringify(event.time_mode);
               break;
            case CALENDAR_PROPERTY_EVENT_UNIT:
               result[c++] = stringify(event.unit);
               break;
            case CALENDAR_PROPERTY_EVENT_IMPORTANCE:
               result[c++] = stringify(event.importance);
               break;
            case CALENDAR_PROPERTY_EVENT_MULTIPLIER:
               result[c++] = stringify(event.multiplier);
               break;
            case CALENDAR_PROPERTY_EVENT_SOURCE:
               result[c++] = event.source_url;
               break;
            case CALENDAR_PROPERTY_EVENT_NAME:
               result[c++] = event.name;
               break;
            case CALENDAR_PROPERTY_EVENT_DIGITS:
               result[c++] = (string)event.digits;
               break;
            case CALENDAR_PROPERTY_RECORD_REVISION:
               result[c++] = (string)v.revision;
               break;
            case CALENDAR_PROPERTY_RECORD_IMPACT:
               result[c++] = stringify(v.impact_type);
               break;
            case CALENDAR_PROPERTY_RECORD_ACTUAL:
               result[c++] = StringFormat("%+.*f", event.digits, v.GetActualValue());
               break;
            case CALENDAR_PROPERTY_RECORD_PREVIOUS:
               result[c++] = StringFormat("%+.*f", event.digits, v.GetPreviousValue());
               break;
            case CALENDAR_PROPERTY_RECORD_REVISED:
               result[c++] = StringFormat("%+.*f", event.digits, v.GetRevisedValue());
               break;
            case CALENDAR_PROPERTY_RECORD_PREVISED:
               result[c++] = StringFormat("%+.*f", event.digits,
                  v.HasRevisedValue() ? v.GetRevisedValue() : v.GetPreviousValue());
               break;
            case CALENDAR_PROPERTY_RECORD_FORECAST:
               result[c++] = StringFormat("%+.*f", event.digits, v.GetForecastValue());
               break;
            case CALENDAR_PROPERTY_RECORD_TIME:
               result[c++] = TimeToString(v.time);
               break;
            case CALENDAR_PROPERTY_RECORD_PERIOD:
               result[c++] = TimeToString(v.period, TIME_DATE);
               break;
            }
            
            if(header)
            {
               widths[j] = fmax(widths[j], StringLen(result[c - 1]));
            }
         }
      }
      
      if(header && rows > 0)
      {
         for(int j = 0; j < cols; ++j)
         {
            if(widths[j] < StringLen(result[j]) - 1)
            {
               StringSetLength(result[j], widths[j]); // truncate
               result[j] += ShortToString(0x205E);    // add ellipsis
            }
         }
      }
      
      if(padding) // for table pretty-printing in monotype logs
      {
         int width[];
         ArrayResize(width, cols);
         ArrayInitialize(width, 0);
         for(int i = 0; i < rows + header; ++i)
         {
            for(int j = 0; j < cols; ++j)
            {
               width[j] = fmax(width[j], StringLen(result[i * cols + j]));
            }
         }

         for(int i = 0; i < rows + header; ++i)
         {
            for(int j = 0; j < cols; ++j)
            {
               const int pad = width[j] - StringLen(result[i * cols + j]);
               result[i * cols + j] = StringFormat("%*.*s%s", pad, pad, " ", result[i * cols + j]);
            }
         }
      }
      
      return true;
   }
};
//+------------------------------------------------------------------+
