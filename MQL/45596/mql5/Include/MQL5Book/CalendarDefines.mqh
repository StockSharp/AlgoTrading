//+------------------------------------------------------------------+
//|                                              CalendarDefines.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/IS.mqh>

#define DAY_LONG     (60 * 60 * 24)
#define WEEK_LONG    (DAY_LONG * 7)
#define MONTH_LONG   (DAY_LONG * 30)
#define QUARTER_LONG (MONTH_LONG * 3)
#define YEAR_LONG    (MONTH_LONG * 12)

enum ENUM_CALENDAR_SCOPE
{
   SCOPE_DAY = DAY_LONG,         // Day
   SCOPE_WEEK = WEEK_LONG,       // Week
   SCOPE_MONTH = MONTH_LONG,     // Month
   SCOPE_QUARTER = QUARTER_LONG, // Quarter
   SCOPE_YEAR = YEAR_LONG,       // Year
};

enum ENUM_CALENDAR_HAS_VALUE
{
   HAS_ANY,     // Any (unimportant)
   HAS_SET,     // Set
   HAS_NOT,     // Not Set
};

enum ENUM_CALENDAR_PROPERTY
{                                      // +/- means filtering support by corresponding field
   CALENDAR_PROPERTY_COUNTRY_ID,       // -ulong
   CALENDAR_PROPERTY_COUNTRY_NAME,     // -string
   CALENDAR_PROPERTY_COUNTRY_CODE,     // +string (2 chars)
   CALENDAR_PROPERTY_COUNTRY_CURRENCY, // +string (3 chars)
   CALENDAR_PROPERTY_COUNTRY_GLYPH,    // -string (1 char)
   CALENDAR_PROPERTY_COUNTRY_URL,      // -string

   CALENDAR_PROPERTY_EVENT_ID,         // +ulong (kind)
   CALENDAR_PROPERTY_EVENT_TYPE,       // +ENUM_CALENDAR_EVENT_TYPE
   CALENDAR_PROPERTY_EVENT_SECTOR,     // +ENUM_CALENDAR_EVENT_SECTOR
   CALENDAR_PROPERTY_EVENT_FREQUENCY,  // +ENUM_CALENDAR_EVENT_FREQUENCY
   CALENDAR_PROPERTY_EVENT_TIMEMODE,   // +ENUM_CALENDAR_EVENT_TIMEMODE
   CALENDAR_PROPERTY_EVENT_UNIT,       // +ENUM_CALENDAR_EVENT_UNIT
   CALENDAR_PROPERTY_EVENT_IMPORTANCE, // +ENUM_CALENDAR_EVENT_IMPORTANCE
   CALENDAR_PROPERTY_EVENT_MULTIPLIER, // +ENUM_CALENDAR_EVENT_MULTIPLIER
   CALENDAR_PROPERTY_EVENT_DIGITS,     // -uint
   CALENDAR_PROPERTY_EVENT_SOURCE,     // +string (URL)
   CALENDAR_PROPERTY_EVENT_CODE,       // -string
   CALENDAR_PROPERTY_EVENT_NAME,       // +string (4+ chars or wildcards '*')

   CALENDAR_PROPERTY_RECORD_ID,        // -ulong
   CALENDAR_PROPERTY_RECORD_TIME,      // +datetime
   CALENDAR_PROPERTY_RECORD_PERIOD,    // +datetime (as long)
   CALENDAR_PROPERTY_RECORD_REVISION,  // +int
   CALENDAR_PROPERTY_RECORD_ACTUAL,    // +long
   CALENDAR_PROPERTY_RECORD_PREVIOUS,  // +long
   CALENDAR_PROPERTY_RECORD_REVISED,   // +long
   CALENDAR_PROPERTY_RECORD_FORECAST,  // +long
   CALENDAR_PROPERTY_RECORD_IMPACT,    // +ENUM_CALENDAR_EVENT_IMPACT
   
   CALENDAR_PROPERTY_RECORD_PREVISED,  // non-standard (previous or revised if exist)
   
   CALENDAR_PROPERTY_CHANGE_ID,        // -ulong (reserved)
};

//+------------------------------------------------------------------+
/*
PRB: all standard ENUMs do not have an option for ALL/ANY element,
so choosing an element in UI is obligatory (which often contradicts to requirements),
but definition of equivalent custom ENUMs will require to typecast (may be unpractical)
*/

enum ENUM_CALENDAR_EVENT_TYPE_EXT
{
   TYPE_EVENT = CALENDAR_TYPE_EVENT,         // Event
   TYPE_INDICATOR = CALENDAR_TYPE_INDICATOR, // Indicator
   TYPE_HOLIDAY = CALENDAR_TYPE_HOLIDAY,     // Holiday
   TYPE_ANY                                  // Any (unimportant)
};

enum ENUM_CALENDAR_EVENT_SECTOR_EXT
{
   SECTOR_NONE = CALENDAR_SECTOR_NONE,     // None
   SECTOR_MARKET = CALENDAR_SECTOR_MARKET, // Market
   SECTOR_GDP = CALENDAR_SECTOR_GDP,       // GDP
   SECTOR_JOBS = CALENDAR_SECTOR_JOBS,     // Jobs
   SECTOR_PRICES = CALENDAR_SECTOR_PRICES, // Prices
   SECTOR_MONEY = CALENDAR_SECTOR_MONEY,   // Money
   SECTOR_TRADE = CALENDAR_SECTOR_TRADE,   // Trade
   SECTOR_GOVERNMENT = CALENDAR_SECTOR_GOVERNMENT, // Government
   SECTOR_BUSINESS = CALENDAR_SECTOR_BUSINESS, // Bussiness
   SECTOR_CONSUMER = CALENDAR_SECTOR_CONSUMER, // Consumer
   SECTOR_HOUSING = CALENDAR_SECTOR_HOUSING, // Housing
   SECTOR_TAXES = CALENDAR_SECTOR_TAXES,   // Taxes
   SECTOR_HOLIDAYS = CALENDAR_SECTOR_HOLIDAYS, // Holidays
   SECTOR_ANY                              // Any (unimportant)
};

enum ENUM_CALENDAR_EVENT_IMPORTANCE_EXT
{
   IMPORTANCE_NONE = CALENDAR_IMPORTANCE_NONE, // None
   IMPORTANCE_LOW = CALENDAR_IMPORTANCE_LOW,   // Low
   IMPORTANCE_MODERATE = CALENDAR_IMPORTANCE_MODERATE, // Moderate
   IMPORTANCE_HIGH = CALENDAR_IMPORTANCE_HIGH, // High
   IMPORTANCE_ANY                              // Any (unimportant)
};
//+------------------------------------------------------------------+
