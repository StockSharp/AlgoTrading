//+------------------------------------------------------------------+
//|                                             CalendarForDates.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Output a table of calendar records for specific range of days, with a filter for country and/or currency."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Defines.mqh>

#define DAY_LONG   (60 * 60 * 24)
#define WEEK_LONG  (DAY_LONG * 7)
#define MONTH_LONG (DAY_LONG * 30)
#define YEAR_LONG  (MONTH_LONG * 12)

enum ENUM_CALENDAR_SCOPE
{
   SCOPE_DAY = DAY_LONG,     // Day
   SCOPE_WEEK = WEEK_LONG,   // Week
   SCOPE_MONTH = MONTH_LONG, // Month
   SCOPE_YEAR = YEAR_LONG,   // Year
};

input string CountryCode = "EU";
input string Currency = "";
input ENUM_CALENDAR_SCOPE Scope = SCOPE_DAY;

//+------------------------------------------------------------------+
//| Extended struct with user-friendly data from MqlCalendarValue    |
//+------------------------------------------------------------------+
struct MqlCalendarRecord: public MqlCalendarValue
{
   static const string importances[];
   
   string importance;
   string name;
   string currency;
   string code;
   double actual, previous, revised, forecast;
   
   MqlCalendarRecord() { }
   
   MqlCalendarRecord(const MqlCalendarValue &value)
   {
      this = value;
      extend();
   }
   
   void extend()
   {
      MqlCalendarEvent event;
      CalendarEventById(event_id, event);
      
      importance = importances[event.importance];
      name = event.name;
      
      MqlCalendarCountry country;
      CalendarCountryById(event.country_id, country);
      
      currency = country.currency;
      code = country.code;
      
      MqlCalendarValue value = this;
      
      // Neither one of the following works:
      //   GetActualValue();
      //   this.GetActualValue();
      //   MqlCalendarValue::GetActualValue();
      
      actual = value.GetActualValue();
      previous = value.GetPreviousValue();
      revised = value.GetRevisedValue();
      forecast = value.GetForecastValue();
   }
};

static const string MqlCalendarRecord::importances[] = {"None", "Low", "Medium", "High"};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlCalendarValue values[];
   MqlCalendarRecord records[];
   datetime from = TimeCurrent() - Scope;
   datetime to = TimeCurrent() + Scope;
   if(PRTF(CalendarValueHistory(values, from, to, CountryCode, Currency)))
   {
      for(int i = 0; i < ArraySize(values); ++i)
      {
         PUSH(records, MqlCalendarRecord(values[i]));
      }
      Print("Near past and future calendar records (extended): ");
      ArrayPrint(records);
   }
}
//+------------------------------------------------------------------+
/*

CalendarValueHistory(values,from,to,CountryCode,Currency)=6 / ok
Near past and future calendar records (extended): 
      [id] [event_id]              [time]            [period] [revision]       [actual_value]         [prev_value] [revised_prev_value]     [forecast_value] [impact_type] [reserved] [importance]                                                [name] [currency] [code] [actual] [previous] [revised] [forecast]
[0] 162723  999020003 2022.06.23 03:00:00 1970.01.01 00:00:00          0 -9223372036854775808 -9223372036854775808 -9223372036854775808 -9223372036854775808             0        ... "High"       "EU Leaders Summit"                                   "EUR"      "EU"        nan        nan       nan        nan
[1] 162724  999020003 2022.06.24 03:00:00 1970.01.01 00:00:00          0 -9223372036854775808 -9223372036854775808 -9223372036854775808 -9223372036854775808             0        ... "High"       "EU Leaders Summit"                                   "EUR"      "EU"        nan        nan       nan        nan
[2] 168518  999010034 2022.06.24 11:00:00 1970.01.01 00:00:00          0 -9223372036854775808 -9223372036854775808 -9223372036854775808 -9223372036854775808             0        ... "Medium"     "ECB Supervisory Board Member McCaul Speech"          "EUR"      "EU"        nan        nan       nan        nan
[3] 168515  999010031 2022.06.24 13:10:00 1970.01.01 00:00:00          0 -9223372036854775808 -9223372036854775808 -9223372036854775808 -9223372036854775808             0        ... "Medium"     "ECB Supervisory Board Member Fernandez-Bollo Speech" "EUR"      "EU"        nan        nan       nan        nan
[4] 168509  999010014 2022.06.24 14:30:00 1970.01.01 00:00:00          0 -9223372036854775808 -9223372036854775808 -9223372036854775808 -9223372036854775808             0        ... "Medium"     "ECB Vice President de Guindos Speech"                "EUR"      "EU"        nan        nan       nan        nan
[5] 161014  999520001 2022.06.24 22:30:00 2022.06.21 00:00:00          0 -9223372036854775808             -6000000 -9223372036854775808 -9223372036854775808             0        ... "Low"        "CFTC EUR Non-Commercial Net Positions"               "EUR"      "EU"        nan   -6.00000       nan        nan

*/
//+------------------------------------------------------------------+
