//+------------------------------------------------------------------+
//|                                              CalendarTrading.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Trade by calendar events in the tester or online."
#property tester_file "xyz.cal"

#define SHOW_WARNINGS  // output extended info into the log, with changes in data state
#define WARNING Print  // use simple Print for warnings (instead of a built-in format with line numbers etc.)
#define LOGGING        // calendar detailed logs
#include <MQL5Book/MqlTradeSync.mqh>
#include <MQL5Book/PositionFilter.mqh>
#include <MQL5Book/TrailingStop.mqh>
#include <MQL5Book/CalendarFilterCached.mqh>
#include <MQL5Book/StringUtils.mqh>
#include <MQL5Book/DealFilter.mqh>
#include <MQL5Book/Tuples.mqh>

input double Volume;               // Volume (0 = minimal lot)
input int Distance2SLTP = 500;     // Distance to SL/TP in points (0 = no)
input uint MultiplePositions = 25;
sinput ulong EventID;
sinput string Text;

AutoPtr<CalendarFilter> fptr;
AutoPtr<CalendarCache> cache;
AutoPtr<TrailingStop> trailing[];

double Lot;
bool Hedging;
string Base;
string Profit;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(AccountInfoInteger(ACCOUNT_TRADE_MODE) != ACCOUNT_TRADE_MODE_DEMO)
   {
      Alert("This is a test EA! Run it on a DEMO account only!");
      return INIT_FAILED;
   }

   Lot = Volume == 0 ? SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN) : Volume;
   Hedging = AccountInfoInteger(ACCOUNT_MARGIN_MODE) == ACCOUNT_MARGIN_MODE_RETAIL_HEDGING;
   Base = SymbolInfoString(_Symbol, SYMBOL_CURRENCY_BASE);
   Profit = SymbolInfoString(_Symbol, SYMBOL_CURRENCY_PROFIT);
   
   cache = new CalendarCache("xyz.cal", true);
   if(cache[].isLoaded())
   {
      fptr = new CalendarFilterCached(cache[]);
   }
   else
   {
      if(!MQLInfoInteger(MQL_TESTER))
      {
         Print("Calendar cache file not found, fall back to online mode");
         fptr = new CalendarFilter();
      }
      else
      {
         Print("Can't proceed in the tester without calendar cache file");
         return INIT_FAILED;
      }
   }
   CalendarFilter *f = fptr[];
   
   if(!f.isLoaded()) return INIT_FAILED;

   // if a specific event is given, use it
   if(EventID > 0) f.let(EventID);
   else
   {
      // otherwise track news for current chart currencies only
      f.let(Base);
      if(Base != Profit)
      {
         f.let(Profit);
      }
      
      // monitor high impact economic indicators with available forecasts
      f.let(CALENDAR_TYPE_INDICATOR);
      f.let(LONG_MIN, CALENDAR_PROPERTY_RECORD_FORECAST, NOT_EQUAL);
      f.let(CALENDAR_IMPORTANCE_HIGH);
   
      if(StringLen(Text)) f.let(Text);
   }
   
   f.describe();
   
   if(Distance2SLTP)
   {
      ArrayResize(trailing, Hedging && MultiplePositions ? MultiplePositions : 1);
   }
   // setup timer for periodic trade by news
   EventSetTimer(1);
   return INIT_SUCCEEDED;
}

//+------------------------------------------------------------------+
//| Custom tester event handler                                      |
//+------------------------------------------------------------------+
double OnTester()
{
   Print("Trade profits by calendar events:");
   HistorySelect(0, LONG_MAX);
   DealFilter filter;
   int props[] = {DEAL_PROFIT, DEAL_SWAP, DEAL_COMMISSION, DEAL_MAGIC, DEAL_TIME};
   filter.let(DEAL_TYPE, (1 << DEAL_TYPE_BUY) | (1 << DEAL_TYPE_SELL), IS::OR_BITWISE)
      .let(DEAL_ENTRY, (1 << DEAL_ENTRY_OUT) | (1 << DEAL_ENTRY_INOUT) | (1 << DEAL_ENTRY_OUT_BY), IS::OR_BITWISE);
   Tuple5<double, double, double, ulong, ulong> trades[];
   
   MapArray<ulong,double> profits;
   MapArray<ulong,double> losses;
   MapArray<ulong,int> counts;
   if(filter.select(props, trades))
   {
      for(int i = 0; i < ArraySize(trades); ++i)
      {
         counts.inc((ulong)trades[i]._4);
         const double payout = trades[i]._1 + trades[i]._2 + trades[i]._3;
         if(payout >= 0)
         {
            profits.inc((ulong)trades[i]._4, payout);
            losses.inc((ulong)trades[i]._4, 0);
         }
         else
         {
            profits.inc((ulong)trades[i]._4, 0);
            losses.inc((ulong)trades[i]._4, payout);
         }
      }
      
      for(int i = 0; i < profits.getSize(); ++i)
      {
         MqlCalendarEvent event;
         MqlCalendarCountry country;
         const ulong keyId = profits.getKey(i);
         if(cache[].calendarEventById(keyId, event)
            && cache[].calendarCountryById(event.country_id, country))
         {
            PrintFormat("%lld %s %s %+.2f [%d] (PF:%.2f) %s",
               event.id, country.code, country.currency,
               profits[keyId] + losses[keyId], counts[keyId],
               profits[keyId] / (losses[keyId] != 0 ? -losses[keyId] : DBL_MIN),
               event.name);
         }
         else
         {
            Print("undefined ", DoubleToString(profits.getValue(i), 2));
         }
      }
   }
   return 0;
}

//+------------------------------------------------------------------+
//| Timer event handler                                              |
//+------------------------------------------------------------------+
void OnTimer()
{
   CalendarFilter *f = fptr[];
   MqlCalendarValue records[];
   
   f.let(TimeTradeServer() - SCOPE_DAY, TimeTradeServer() + SCOPE_DAY);

   if(f.update(records)) // find changes that match filters
   {
      // print changes to log
      static const ENUM_CALENDAR_PROPERTY props[] =
      {
         CALENDAR_PROPERTY_RECORD_TIME,
         CALENDAR_PROPERTY_COUNTRY_CURRENCY,
         CALENDAR_PROPERTY_COUNTRY_CODE,
         CALENDAR_PROPERTY_EVENT_NAME,
         CALENDAR_PROPERTY_EVENT_IMPORTANCE,
         CALENDAR_PROPERTY_RECORD_ACTUAL,
         CALENDAR_PROPERTY_RECORD_FORECAST,
         CALENDAR_PROPERTY_RECORD_PREVISED,
         CALENDAR_PROPERTY_RECORD_IMPACT,
      };
      static const int p = ArraySize(props);
      string result[];
      f.format(records, props, result);
      for(int i = 0; i < ArraySize(result) / p; ++i)
      {
         Print(SubArrayCombine(result, " | ", i * p, p));
      }

      // calculate news impact
      static const int impacts[3] = {0, +1, -1};
      int impact = 0;
      string about = "";
      ulong lasteventid = 0;
      for(int i = 0; i < ArraySize(records); ++i)
      {
         const int sign = result[i * p + 1] == Profit ? -1 : +1;
         impact += sign * impacts[records[i].impact_type];
         about += StringFormat("%+lld ", sign * (long)records[i].event_id);
         lasteventid = records[i].event_id;
      }
      
      if(impact == 0) return; // no signal

      // close existing positions if needed
      PositionFilter positions;
      ulong tickets[];
      positions.let(POSITION_SYMBOL, _Symbol).select(tickets);
      const int n = ArraySize(tickets);
      
      if(n >= (int)(Hedging ? MultiplePositions : 1))
      {
         MqlTradeRequestSync position;
         position.close(_Symbol) && position.completed();
      }
      
      // open new position according to the signal direction
      MqlTradeRequestSync request;
      request.magic = lasteventid;
      request.comment = about;
      const double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      const double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      const double point = SymbolInfoDouble(_Symbol, SYMBOL_POINT);
      ulong ticket = 0;

      if(impact > 0)
      {
         ticket = request.buy(Lot, 0,
            Distance2SLTP ? ask - point * Distance2SLTP : 0,
            Distance2SLTP ? ask + point * Distance2SLTP : 0);
      }
      else if(impact < 0)
      {
         ticket = request.sell(Lot, 0,
            Distance2SLTP ? bid + point * Distance2SLTP : 0,
            Distance2SLTP ? bid - point * Distance2SLTP : 0);
      }
      
      if(ticket && request.completed() && Distance2SLTP)
      {
         for(int i = 0; i < ArraySize(trailing); ++i)
         {
            if(trailing[i][] == NULL) // find free slot, create trailing object
            {
               trailing[i] = new TrailingStop(ticket, Distance2SLTP, Distance2SLTP / 50);
               break;
            }
         }
      }
   }
}

//+------------------------------------------------------------------+
//| Tick event handler                                               |
//+------------------------------------------------------------------+
void OnTick()
{
   for(int i = 0; i < ArraySize(trailing); ++i)
   {
      if(trailing[i][])
      {
         if(!trailing[i][].trail()) // position was closed
         {
            trailing[i] = NULL; // free the slot, delete object
         }
      }
   }
}

//+------------------------------------------------------------------+
