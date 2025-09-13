//+------------------------------------------------------------------+
//|                                                   MarketInfo.mqh |
//|                                    Copyright (c) 2010, Marketeer |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+

#include "MT4Mode.mqh"

double MarketInfo(const string symbol, const int type)
{
  switch(type)
  {
    case MODE_LOW:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_LASTLOW));
    case MODE_HIGH:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_LASTHIGH));
    case MODE_TIME:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_TIME));
    case MODE_BID:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_BID));
    case MODE_ASK:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_ASK));
    case MODE_POINT:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_POINT));
    case MODE_DIGITS:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_DIGITS));
    case MODE_SPREAD:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_SPREAD));
    case MODE_STOPLEVEL:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_TRADE_STOPS_LEVEL));
    case MODE_LOTSIZE:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_TRADE_CONTRACT_SIZE));
    case MODE_TICKVALUE:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_VALUE));
    case MODE_TICKSIZE:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_TRADE_TICK_SIZE));
    case MODE_SWAPLONG:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_SWAP_LONG));
    case MODE_SWAPSHORT:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_SWAP_SHORT));
    case MODE_STARTING:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_START_TIME));
    case MODE_EXPIRATION:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_EXPIRATION_TIME));
    case MODE_TRADEALLOWED:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_TRADE_MODE) != SYMBOL_TRADE_MODE_DISABLED);
    case MODE_MINLOT:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_VOLUME_MIN));
    case MODE_LOTSTEP:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_VOLUME_STEP));
    case MODE_MAXLOT:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_VOLUME_MAX));
    case MODE_SWAPTYPE:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_SWAP_MODE));
    case MODE_PROFITCALCMODE:
    case MODE_MARGINCALCMODE:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_TRADE_CALC_MODE));
    case MODE_MARGINREQUIRED:
    case MODE_MARGININIT: 
       return (double)(SymbolInfoDouble(symbol, SYMBOL_MARGIN_INITIAL));
    case MODE_MARGINMAINTENANCE:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_MARGIN_MAINTENANCE));
    case MODE_MARGINHEDGED:
       return (double)(SymbolInfoDouble(symbol, SYMBOL_MARGIN_HEDGED));
    case MODE_FREEZELEVEL:
       return (double)(SymbolInfoInteger(symbol, SYMBOL_TRADE_FREEZE_LEVEL));
  
    default: return(0);
  }
  return(0);
}

template<typename T>
T MarketInfo(const string symbol, const int type, const T return_type)
{
  return (T)MarketInfo(symbol, type);
}
