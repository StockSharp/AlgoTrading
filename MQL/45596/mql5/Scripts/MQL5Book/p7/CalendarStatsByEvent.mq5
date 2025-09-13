//+------------------------------------------------------------------+
//|                                         CalendarStatsByEvent.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Output a table of calendar statistics by event kind for specific range of days and country or currency."
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>
#include <MQL5Book/Defines.mqh>
#include <MQL5Book/QuickSortStructT.mqh>

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

input string CountryOrCurrency = "EU";
input ENUM_CALENDAR_SCOPE Scope = SCOPE_YEAR;

//+------------------------------------------------------------------+
//| Struct for event statistics                                      |
//+------------------------------------------------------------------+
struct CalendarEventStats
{
   static const string importances[];

   ulong id;
   string name;
   string importance;
   int count;
};

static const string CalendarEventStats::importances[] = {"None", "Low", "Medium", "High"};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   MqlCalendarEvent events[];
   MqlCalendarValue values[];
   CalendarEventStats stats[];
   
   const datetime from = TimeCurrent() - Scope;
   const datetime to = TimeCurrent() + Scope;
   
   if(StringLen(CountryOrCurrency) == 2)
   {
      PRTF(CalendarEventByCountry(CountryOrCurrency, events));
   }
   else
   {
      PRTF(CalendarEventByCurrency(CountryOrCurrency, events));
   }
   
   for(int i = 0; i < ArraySize(events); ++i)
   {
      ResetLastError();
      if(CalendarValueHistoryByEvent(events[i].id, values, from, to))
      {
         CalendarEventStats event = {events[i].id, events[i].name,
            CalendarEventStats::importances[events[i].importance], ArraySize(values)};
         PUSH(stats, event);
      }
      else
      {
         if(_LastError != 0)
         {
            PrintFormat("Error %d for %lld", _LastError, events[i].id);
         }
      }
   }
   
   SORT_STRUCT(CalendarEventStats, stats, count);
   ArrayReverse(stats);
   ArrayPrint(stats);
}
//+------------------------------------------------------------------+
/*

CalendarEventByCountry(CountryOrCurrency,events)=82 / ok
          [id]                                                [name] [importance] [count]
[ 0] 999520001 "CFTC EUR Non-Commercial Net Positions"               "Low"             79
[ 1] 999010029 "ECB President Lagarde Speech"                        "High"            69
[ 2] 999010035 "ECB Executive Board Member Elderson Speech"          "Medium"          37
[ 3] 999030027 "Core CPI"                                            "Low"             36
[ 4] 999030026 "CPI"                                                 "Low"             36
[ 5] 999030025 "CPI excl. Energy and Unprocessed Food y/y"           "Low"             36
[ 6] 999030024 "CPI excl. Energy and Unprocessed Food m/m"           "Low"             36
[ 7] 999030010 "Core CPI m/m"                                        "Medium"          36
[ 8] 999030013 "CPI y/y"                                             "Low"             36
[ 9] 999030012 "Core CPI y/y"                                        "Low"             36
[10] 999040006 "Consumer Confidence Index"                           "Low"             36
[11] 999030011 "CPI m/m"                                             "Medium"          36
[12] 999010033 "ECB Executive Board Member Schnabel Speech"          "Medium"          35
[13] 999010014 "ECB Vice President de Guindos Speech"                "Medium"          34
[14] 999010020 "ECB Executive Board Member Lane Speech"              "Medium"          31
[15] 999010021 "ECB Supervisory Board Chair Enria Speech"            "Medium"          31
[16] 999010032 "ECB Executive Board Member Panetta Speech"           "Medium"          30
[17] 999500003 "S&P Global Composite PMI"                            "Medium"          26
[18] 999500002 "S&P Global Services PMI"                             "Medium"          26
[19] 999500001 "S&P Global Manufacturing PMI"                        "Medium"          26
[20] 999060001 "Sentix Investor Confidence"                          "Low"             24
[21] 999010031 "ECB Supervisory Board Member Fernandez-Bollo Speech" "Medium"          22
[22] 999010016 "Current Account"                                     "Low"             20
[23] 999010017 "Current Account n.s.a."                              "Low"             20
[24] 999050001 "ZEW Economic Sentiment Indicator"                    "Medium"          19
[25] 999010018 "ECB M3 Money Supply y/y"                             "Low"             19
[26] 999010026 "ECB Non-Financial Corporations Loans y/y"            "Low"             19
[27] 999010023 "Official Reserve Assets"                             "Low"             19
[28] 999010027 "ECB Private Sector Loans y/y"                        "Low"             19
[29] 999010034 "ECB Supervisory Board Member McCaul Speech"          "Medium"          19
[30] 999010019 "ECB Households Loans y/y"                            "Low"             19
[31] 999040008 "Industry Selling Price Expectations"                 "Low"             18
[32] 999040007 "Consumer Price Expectations"                         "Low"             18
[33] 999040005 "Economic Sentiment Indicator"                        "Low"             18
[34] 999040004 "Services Sentiment Indicator"                        "Low"             18
[35] 999040003 "Industrial Confidence Indicator"                     "Low"             18
[36] 999030022 "CPI excl. Tobacco y/y"                               "Low"             18
[37] 999030021 "CPI excl. Tobacco m/m"                               "Medium"          18
[38] 999030020 "Unemployment Rate"                                   "Medium"          18
[39] 999030019 "Trade Balance n.s.a."                                "Medium"          18
[40] 999030018 "Trade Balance"                                       "Medium"          18
[41] 999030017 "GDP y/y"                                             "Medium"          18
[42] 999030016 "GDP q/q"                                             "High"            18
[43] 999030015 "Construction Output y/y"                             "Low"             18
[44] 999030014 "Construction Output m/m"                             "Low"             18
[45] 999030008 "Industrial Production y/y"                           "Low"             18
[46] 999030007 "Industrial Production m/m"                           "Medium"          18
[47] 999030006 "PPI y/y"                                             "Low"             18
[48] 999030005 "PPI m/m"                                             "Medium"          18
[49] 999030004 "Retail Sales y/y"                                    "Medium"          18
[50] 999030003 "Retail Sales m/m"                                    "High"            18
[51] 999010001 "ECB Non-monetary Policy Meeting"                     "Medium"          18
[52] 999020001 "Economic and Financial Affairs Council Meeting"      "Medium"          18
[53] 999010015 "ECB Marginal Lending Facility Rate Decision"         "High"            16
[54] 999010024 "ECB Monetary Policy Statement"                       "Medium"          16
[55] 999010006 "ECB Deposit Facility Rate Decision"                  "High"            16
[56] 999010003 "ECB Monetary Policy Press Conference"                "High"            16
[57] 999010007 "ECB Interest Rate Decision"                          "High"            16
[58] 999020003 "EU Leaders Summit"                                   "High"            15
[59] 999020002 "Eurogroup Meeting"                                   "Medium"          15
[60] 999500004 "S&P Global Construction PMI"                         "Medium"          13
[61] 999030028 "Employment Level"                                    "Low"             12
[62] 999030001 "Employment Change q/q"                               "High"            12
[63] 999030002 "Employment Change y/y"                               "Medium"          12
[64] 999010002 "ECB Monetary Policy Meeting Accounts"                "Medium"          10
[65] 999010008 "ECB Economic Bulletin"                               "Medium"           8
[66] 999030023 "Wage Costs y/y"                                      "Medium"           6
[67] 999030009 "Labour Cost Index"                                   "Low"              6
[68] 999010025 "ECB Bank Lending Survey"                             "Low"              6
[69] 999010030 "ECB Supervisory Board Member af Jochnick Speech"     "Medium"           4
[70] 999010022 "ECB Supervisory Board Member Hakkarainen Speech"     "Medium"           3
[71] 999010028 "ECB Financial Stability Review"                      "Medium"           3
[72] 999010009 "ECB Targeted LTRO"                                   "Medium"           2
[73] 999010036 "ECB Supervisory Board Member Tuominen Speech"        "Medium"           1

*/
//+------------------------------------------------------------------+
