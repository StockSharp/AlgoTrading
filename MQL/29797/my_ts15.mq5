//+------------------------------------------------------------------+
//|                                                      my_ts15.mq5 |
//|                                                             Scur |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Scur"
#property version   "1.50"

#include <Indicators\Trend.mqh>
#include <Trade\Trade.mqh>
//--- input parameters
input group                "MA parameters"
input int                  TS_ma_period=50;                 // period of MA
input int                  TS_ma_shift=0;                   // MA shift
input ENUM_MA_METHOD       TS_ma_method=MODE_LWMA;          // MA type of smoothing
input ENUM_APPLIED_PRICE   TS_applied_price=PRICE_WEIGHTED; // MA type of price
string                     TS_SL_symbol=_Symbol;            // working symbol
ENUM_TIMEFRAMES            TS_period=PERIOD_CURRENT;        // timeframe

input group                "Appearance settings"
input bool                 TS_show=true;                    // show indicator line


input group                "Stop loss parameters"
input group                "--- Stop Loss Enforcing ---"
input bool                 pre_init=false;                  // enforce max sl
input int                  max_sl=100;                      // max sl to enforce

input group                "--- Stop Loss Parameters"
input int                  MA_bars_trail=1;                 // distance in bars for the MA trailing signal
input int                  trail_behind_MA=5;               // points behind MA to set TS
input int                  trail_behind_price=30;           // points behind price to trail on positive
input int                  trail_behind_negative=60;        // points behind price to trail on negative
input double               trail_step=0;                    // trail step

CiMA                       TS_MA;                           // handler for MA properties
CTrade                     TS_trade;                        // handler for order modification

int                        TS_handle;                       // indicator handle
string                     TS_short_name;                   // indicator short name
bool                       TS_exists=false;                 // if chart line exists

MqlTradeRequest            TS_data;                         // MA working data values
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {

//---    sets the parameters of trailer MA
   MqlParam pars[4];
//--- period
   pars[0].type=TYPE_INT;
   pars[0].integer_value=TS_ma_period;
//--- shift
   pars[1].type=TYPE_INT;
   pars[1].integer_value=TS_ma_shift;
//--- type of smoothing
   pars[2].type=TYPE_INT;
   pars[2].integer_value=TS_ma_method;
//--- type of price
   pars[3].type=TYPE_INT;
   pars[3].integer_value=TS_applied_price;

//---    creates the indicator
   if(!((CIndicator)TS_MA).Create(TS_SL_symbol, TS_period, IND_MA, 4, pars))
      return(INIT_FAILED);

//---    creates the handle
   if(!(TS_handle = iMA(TS_SL_symbol, TS_period, TS_ma_period, TS_ma_shift, TS_ma_method, TS_applied_price)))
      return(INIT_FAILED);

//--- draws indicator
   if(TS_exists)
      ChartIndicatorDelete(0,0,TS_short_name);

   if(TS_show)
     {
      ChartIndicatorAdd(0,0,TS_handle);
      TS_short_name=ChartIndicatorName(0,0,ChartIndicatorsTotal(0,0)-1);
      IndicatorSetString(INDICATOR_SHORTNAME,TS_short_name);
      TS_exists=true;
     }
   else
      TS_exists=false;

//---
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   TrailDeInit();
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Trail();
  }
//+------------------------------------------------------------------+
//| close trail function                                                 |
//+------------------------------------------------------------------+
void TrailDeInit()
{
   ChartIndicatorDelete(0,0,TS_short_name);
   IndicatorRelease(TS_handle);
}
//+------------------------------------------------------------------+
//| trail function                                             |
//+------------------------------------------------------------------+
void Trail()
  {
//---
   int totalpos = PositionsTotal();
   double   TS_bid,
            TS_ask,
            TS_price,
            sl,
            tp,
            trail,
            TS_MA_val;
   int      TS_int_type;
   ulong    TS_ticket;
   string   TS_symbol;

//---    pre-calculates values based on inputs
   double   TS_maxsl = max_sl*_Point;
   double   TS_trailma = trail_behind_MA*_Point;
   double   TS_trailprice = trail_behind_price*_Point;
   double   TS_trailnegative = trail_behind_negative*_Point;
   double   TS_step = trail_step*_Point;

//---    refreshes the indicator information
   TS_MA.Refresh();
//---    get the MA value
   TS_MA_val=TS_MA.Main(MA_bars_trail);

   if(totalpos>0)
     {
      for(int cnt=0 ; cnt<totalpos ; cnt++)
        {
         TS_ticket=PositionGetTicket(cnt);
         if(PositionSelectByTicket(TS_ticket))
           {
            TS_symbol=PositionGetSymbol(cnt);

            //---    if position belongs to EA chart
            if(TS_symbol == _Symbol)
              {

               //---    sets working variables
               TS_trade.Request(TS_data);
               sl=PositionGetDouble(POSITION_SL);
               tp=PositionGetDouble(POSITION_TP);
               TS_bid=SymbolInfoDouble(TS_SL_symbol, SYMBOL_BID);
               TS_ask=SymbolInfoDouble(TS_SL_symbol, SYMBOL_ASK);
               TS_int_type=(int)PositionGetInteger(POSITION_TYPE);
               TS_price=PositionGetDouble(POSITION_PRICE_OPEN);
               trail=TS_price;

               //---    if position is long
               if(TS_int_type == 0)
                 {

                  //---    if position sl is below limit and can close
                  if((TS_bid <= TS_price - TS_maxsl) && pre_init)
                    {
                     //---    closes the order
                     TS_trade.PositionClose(TS_ticket,1000);
                     continue;
                    }

                  //---    if ma trailing is bigger or equal to bid trailing
                  if(TS_MA_val - TS_trailma >= TS_bid - TS_trailprice)
                     //---    sets trail to bid trailing
                     trail = TS_bid - TS_trailprice;
                  else
                     //---    set trail to ma trailing
                     trail = TS_MA_val - TS_trailma;

                  //---    if in positive ground
                  if(TS_price + TS_trailprice < TS_bid)
                    {
                     //---     if trailing too close to bid, sets trail to minimum distance to price
                     if(trail > TS_bid - TS_trailprice)
                        trail = TS_bid - TS_trailprice;
                    }
                  else
                     //---    if in negative ground
                    {
                     //---     if trailing too close to bid, sets trail to negative position value
                     if(trail > TS_bid - TS_trailnegative)
                        trail = TS_bid - TS_trailnegative;
                    }

                  //---    if pre_init close is true and new sl is inferior to max sl, sets sl to max allowed
                  if((trail <= TS_price - TS_maxsl) && pre_init)
                     trail = TS_price  - TS_maxsl;

                  //--- checks if new sl is lower than existing, if so replace it
                  if(((sl < trail) && ( trail-sl >= TS_step)) || sl == 0)
                    {
                     sl = NormalizeDouble(trail, _Digits);
                     TS_trade.PositionModify(TS_ticket, sl, tp);
                    }
                 }

               //---    if position is short
               if(TS_int_type == 1)
                 {
                  //---    if position sl is above limit and can close
                  if((TS_ask >= TS_price + TS_maxsl) && pre_init)
                    {
                     //---    closes the order
                     TS_trade.PositionClose(TS_ticket,1000);
                     continue;
                    }

                  //---    if ma trailing is lesser or equal to bid trailing
                  if(TS_MA_val + TS_trailma <= TS_ask + TS_trailprice)
                     //---    sets trail to ask trailing
                     trail = TS_ask + TS_trailprice;
                  else
                     //---    set trail to ma trailing
                     trail = TS_MA_val + TS_trailma;

                  //---    if in positive ground
                  if(TS_price - TS_trailprice > TS_ask)
                    {
                     //---     if trailing too close to ask, sets trail to minimum distance to price
                     if(trail < TS_ask + TS_trailprice)
                        trail = TS_ask + TS_trailprice;
                    }
                  else
                     //---    if in negative ground
                    {
                     //---     if trailing too close to ask, sets trail to negative position value
                     if(trail < TS_ask + TS_trailnegative)
                        trail = TS_ask + TS_trailnegative;
                    }
                  //---    if pre_init close is true and new sl exceed max sl, sets sl to max allowed
                  if((trail >= TS_price + TS_maxsl) && pre_init)
                     trail = TS_price  + TS_maxsl;

                  //---    checks if new sl is higher than existing, if so replace it
                  if( ((sl > trail) && (sl-trail>=TS_step)) || sl == 0)
                    {
                     sl = NormalizeDouble(trail, _Digits);
                     TS_trade.PositionModify(TS_ticket, sl, tp);
                    }
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+