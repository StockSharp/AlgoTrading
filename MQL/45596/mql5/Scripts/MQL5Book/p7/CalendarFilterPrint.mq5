//+------------------------------------------------------------------+
//|                                          CalendarFilterPrint.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright (c) 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Request economic calendar events according to specified filters."
#property script_show_inputs

#define LOGGING
#include <MQL5Book/CalendarFilter.mqh>
#include <MQL5Book/StringUtils.mqh>

//+------------------------------------------------------------------+
//| I N P U T S                                                      |
//+------------------------------------------------------------------+
input string Context; // Context (country - 2 chars, currency - 3 chars, empty - all)
input ENUM_CALENDAR_SCOPE Scope = SCOPE_MONTH;
input string Text = "farm";
input int Limit = -1;

//+------------------------------------------------------------------+
//| G L O B A L S                                                    |
//+------------------------------------------------------------------+
CalendarFilter f(Context, TimeCurrent() - Scope, TimeCurrent() + Scope);

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlCalendarValue records[];
   // setup appropriate filter conditions
   f.let(CALENDAR_IMPORTANCE_LOW, GREATER) // medium or high priority
      .let(LONG_MIN, CALENDAR_PROPERTY_RECORD_FORECAST, NOT_EQUAL) // forecast is available
      .let(Text); // full-text search with wildcard '*' support
      // NB: strings of 2 or 3 chars without wildcard
      // will be treated as a country or currency code respectively
   
   // apply the filters and get results
   if(f.select(records, true, Limit))
   {
      static const ENUM_CALENDAR_PROPERTY props[] =
      {
         CALENDAR_PROPERTY_RECORD_TIME,
         CALENDAR_PROPERTY_COUNTRY_CURRENCY,
         CALENDAR_PROPERTY_EVENT_NAME,
         CALENDAR_PROPERTY_EVENT_IMPORTANCE,
         CALENDAR_PROPERTY_RECORD_ACTUAL,
         CALENDAR_PROPERTY_RECORD_FORECAST,
         CALENDAR_PROPERTY_RECORD_PREVISED,
         CALENDAR_PROPERTY_RECORD_IMPACT,
         CALENDAR_PROPERTY_EVENT_SECTOR,
      };
      static const int p = ArraySize(props);

      // output formatted results
      string result[];
      if(f.format(records, props, result, true, true))
      {
         for(int i = 0; i < ArraySize(result) / p; ++i)
         {
            Print(SubArrayCombine(result, " | ", i * p, p));
         }
      }
   }
   else
   {
      Print("No calendar events for selected filters");
   }
}
//+------------------------------------------------------------------+
/*

example for default settings:
- Context = empty, meaning no specific conditions for countries or currencies
- Scope = SCOPE_MONTH
- Text = "farm" means context search within event names
- Limit = -1, no cut off: any number of matching events will be printed

Selecting calendar records...
country[i]= / ok
calendarValueHistory(temp,from,to,country[i],c)=2372 / ok
Filtering 2372 records
Got 9 records
            TIME | CUR⁞ |                          NAME | IMPORTAN⁞ | ACTU⁞ | FORE⁞ | PREV⁞ |   IMPACT | SECT⁞
2022.06.02 15:15 |  USD | ADP Nonfarm Employment Change |      HIGH |  +128 |  -225 |  +202 | POSITIVE |  JOBS
2022.06.02 15:30 |  USD |      Nonfarm Productivity q/q |  MODERATE |  -7.3 |  -7.5 |  -7.5 | POSITIVE |  JOBS
2022.06.03 15:30 |  USD |              Nonfarm Payrolls |      HIGH |  +390 |   -19 |  +436 | POSITIVE |  JOBS
2022.06.03 15:30 |  USD |      Private Nonfarm Payrolls |  MODERATE |  +333 |    +8 |  +405 | POSITIVE |  JOBS
2022.06.09 08:30 |  EUR |          Nonfarm Payrolls q/q |  MODERATE |  +0.3 |  +0.3 |  +0.3 |       NA |  JOBS
               — |    — |                             — |         — |     — |     — |     — |        — |     —
2022.07.07 15:15 |  USD | ADP Nonfarm Employment Change |      HIGH |  +nan |  -263 |  +128 |       NA |  JOBS
2022.07.08 15:30 |  USD |              Nonfarm Payrolls |      HIGH |  +nan |  -229 |  +390 |       NA |  JOBS
2022.07.08 15:30 |  USD |      Private Nonfarm Payrolls |  MODERATE |  +nan |   +51 |  +333 |       NA |  JOBS

*/
//+------------------------------------------------------------------+
