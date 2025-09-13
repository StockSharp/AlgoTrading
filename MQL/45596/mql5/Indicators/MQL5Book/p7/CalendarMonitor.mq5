//+------------------------------------------------------------------+
//|                                              CalendarMonitor.mq5 |
//|                              Copyright (c) 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright (c) 2022, MetaQuotes Ltd."
#property description "Output a table with selected calendar events."
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

// #define LOGGING
#include <MQL5Book/CalendarFilter.mqh>
#include <MQL5Book/Tableau.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/StringUtils.mqh>

//+------------------------------------------------------------------+
//| I N P U T S                                                      |
//+------------------------------------------------------------------+
input group "General filters";
input string Context; // Context (country - 2 chars, currency - 3 chars, empty - all)
input ENUM_CALENDAR_SCOPE Scope = SCOPE_WEEK;
input bool UseChartCurrencies = true;

input group "Optional filters";
input ENUM_CALENDAR_EVENT_TYPE_EXT Type = TYPE_ANY;
input ENUM_CALENDAR_EVENT_SECTOR_EXT Sector = SECTOR_ANY;
input ENUM_CALENDAR_EVENT_IMPORTANCE_EXT Importance = IMPORTANCE_MODERATE; // Importance (at least)
input string Text;
input ENUM_CALENDAR_HAS_VALUE HasActual = HAS_ANY;
input ENUM_CALENDAR_HAS_VALUE HasForecast = HAS_ANY;
input ENUM_CALENDAR_HAS_VALUE HasPrevious = HAS_ANY;
input ENUM_CALENDAR_HAS_VALUE HasRevised = HAS_ANY;
input int Limit = 30;

input group "Rendering settings";
input ENUM_BASE_CORNER Corner = CORNER_RIGHT_LOWER;
input int Margins = 8;
input int FontSize = 8;
input string FontName = "Consolas";
input color BackgroundColor = clrSilver;
input uchar BackgroundTransparency = 128;    // BackgroundTransparency (255 - opaque, 0 - glassy)

//+------------------------------------------------------------------+
//| G L O B A L S                                                    |
//+------------------------------------------------------------------+
CalendarFilter f(Context);
AutoPtr<Tableau> t;

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
int OnInit()
{
   if(!f.isLoaded()) return INIT_FAILED;
   
   if(UseChartCurrencies)
   {
      const string base = SymbolInfoString(_Symbol, SYMBOL_CURRENCY_BASE);
      const string profit = SymbolInfoString(_Symbol, SYMBOL_CURRENCY_PROFIT);
      f.let(base);
      if(base != profit)
      {
         f.let(profit);
      }
   }
   
   if(Type != TYPE_ANY)
   {
      f.let((ENUM_CALENDAR_EVENT_TYPE)Type);
   }
   
   if(Sector != SECTOR_ANY)
   {
      f.let((ENUM_CALENDAR_EVENT_SECTOR)Sector);
   }
   
   if(Importance != IMPORTANCE_ANY)
   {
      f.let((ENUM_CALENDAR_EVENT_IMPORTANCE)(Importance - 1), GREATER);
   }

   if(StringLen(Text))
   {
      f.let(Text);
   }
   
   if(HasActual != HAS_ANY)
   {
      f.let(LONG_MIN, CALENDAR_PROPERTY_RECORD_ACTUAL, HasActual == HAS_SET ? NOT_EQUAL : EQUAL);
   }

   if(HasPrevious != HAS_ANY)
   {
      f.let(LONG_MIN, CALENDAR_PROPERTY_RECORD_PREVIOUS, HasPrevious == HAS_SET ? NOT_EQUAL : EQUAL);
   }
   
   if(HasRevised != HAS_ANY)
   {
      f.let(LONG_MIN, CALENDAR_PROPERTY_RECORD_REVISED, HasRevised == HAS_SET ? NOT_EQUAL : EQUAL);
   }
   
   if(HasForecast != HAS_ANY)
   {
      f.let(LONG_MIN, CALENDAR_PROPERTY_RECORD_FORECAST, HasForecast == HAS_SET ? NOT_EQUAL : EQUAL);
   }
   
   EventSetTimer(1);
   
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Timer event handler (main processing of calendar goes here)      |
//+------------------------------------------------------------------+
void OnTimer()
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

   MqlCalendarValue records[];
   
   f.let(TimeTradeServer() - Scope, TimeTradeServer() + Scope);
   
   const ulong trackID = f.getChangeID();
   if(trackID) // already has a state, try to detect changes
   {
      if(f.update(records)) // find changes that match filters
      {
         // notify user about new changes
         string result[];
         f.format(records, props, result);
         for(int i = 0; i < ArraySize(result) / p; ++i)
         {
            Alert(SubArrayCombine(result, " | ", i * p, p));
         }
         // fall through to the table redraw
      }
      else if(trackID == f.getChangeID())
      {
         return; // no changes in the calendar
      }
   }
   
   // request complete set of events according to filters
   f.select(records, true, Limit);

   // rebuild the table displayed on chart
   string result[];
   f.format(records, props, result, true, true);

   /*
   // on-chart table copy in the log
   for(int i = 0; i < ArraySize(result) / p; ++i)
   {
      Print(SubArrayCombine(result, " | ", i * p, p));
   }
   */

   if(t[] == NULL || t[].getRows() != ArraySize(records) + 1)
   {
      t = new Tableau("CALT", ArraySize(records) + 1, p,
         TBL_CELL_HEIGHT_AUTO, TBL_CELL_WIDTH_AUTO,
         Corner, Margins, FontSize, FontName, FontName + " Bold",
         TBL_FLAG_ROW_0_HEADER,
         BackgroundColor, BackgroundTransparency);
   }
   const string hints[] = {};
   t[].fill(result, hints);
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
