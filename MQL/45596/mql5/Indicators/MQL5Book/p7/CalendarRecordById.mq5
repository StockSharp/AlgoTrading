//+------------------------------------------------------------------+
//|                                           CalendarRecordById.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Finds nearest forthcoming news and monitors it for updates."
#property indicator_chart_window
#property indicator_plots 0

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/StructPrint.mqh>
#include <MQL5Book/Timing.mqh>

#define DAY_LONG   (60 * 60 * 24)
#define WEEK_LONG  (DAY_LONG * 7)
#define MONTH_LONG (DAY_LONG * 30)
#define YEAR_LONG  (MONTH_LONG * 12)

input uint TimerSeconds = 5;

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
   
   MqlCalendarRecord() { ZeroMemory(this); }
   
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

MqlCalendarValue track; 

//+------------------------------------------------------------------+
//| Compare plain structs                                            |
//+------------------------------------------------------------------+
template<typename S>
int StructCompare(const S &s1, const S &s2)
{
   uchar array1[], array2[];
   if(StructToCharArray(s1, array1) && StructToCharArray(s2, array2))
   {
      return ArrayCompare(array1, array2);
   }
   return -2;
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   EventSetTimer(TimerSeconds);
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   if(!track.id)
   {
      MqlCalendarValue values[];
      if(PRTF(CalendarValueHistory(values, TimeCurrent(), TimeCurrent() + DAY_LONG * 3)))
      {
         for(int i = 0; i < ArraySize(values); ++i)
         {
            MqlCalendarEvent event;
            CalendarEventById(values[i].event_id, event);
            if(event.type == CALENDAR_TYPE_INDICATOR && !values[i].HasActualValue())
            {
               track = values[i];
               PrintFormat("Started monitoring %lld", track.id);
               StructPrint(MqlCalendarRecord(track), ARRAYPRINT_HEADER);
               return;
            }
         }
      }
   }
   else
   {
      MqlCalendarValue update;
      if(CalendarValueById(track.id, update))
      {
         if(fabs(StructCompare(track, update)) == 1)
         {
            Alert(StringFormat("News %lld changed", track.id));
            PrintFormat("New state of %lld", track.id);
            StructPrint(MqlCalendarRecord(update), ARRAYPRINT_HEADER);
            if(update.HasActualValue())
            {
               Print("Timer stopped");
               EventKillTimer();
            }
            else
            {
               track = update;
            }
         }
      }
      
      if(TimeCurrent() <= track.time)
      {
         Comment("Forthcoming event time: ", track.time,
            ", remaining: ", Timing::stringify((uint)(track.time - TimeCurrent())));
      }
      else
      {
         Comment("Forthcoming event time: ", track.time,
            ", late for: ", Timing::stringify((uint)(TimeCurrent() - track.time)));
      }
   }
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function (dummy here)                 |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &price[])
{
   return rates_total;
}

//+------------------------------------------------------------------+
//| Finalization function                                            |
//+------------------------------------------------------------------+
void OnDeinit(const int)
{
   Comment("");
}
//+------------------------------------------------------------------+
/*

CalendarValueHistory(values,TimeCurrent(),TimeCurrent()+(60*60*24)*3)=186 / ok
Started monitoring 156045
  [id] [event_id]              [time]            [period] [revision]       [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved] [importance]                     [name] [currency] [code] [actual] [previous] [revised] [forecast]
156045  840020013 2022.06.27 15:30:00 2022.05.01 00:00:00          0 -9223372036854775808       400000 -9223372036854775808                0             0        ... "Medium"     "Durable Goods Orders m/m" "USD"      "US"        nan    0.40000       nan    0.00000
...
Alert: News 156045 changed
New state of 156045
  [id] [event_id]              [time]            [period] [revision] [actual_value] [prev_value] [revised_prev_value] [forecast_value] [impact_type] [reserved] [importance]                     [name] [currency] [code] [actual] [previous] [revised] [forecast]
156045  840020013 2022.06.27 15:30:00 2022.05.01 00:00:00          0         700000       400000 -9223372036854775808                0             1        ... "Medium"     "Durable Goods Orders m/m" "USD"      "US"    0.70000    0.40000       nan    0.00000
Timer stopped

*/
//+------------------------------------------------------------------+
