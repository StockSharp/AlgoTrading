//+------------------------------------------------------------------+
//|                                                   Indicators.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+
//| Fallback compatibility file for legacy projects.                 |
//| In new projects include specific classes from /Functors directly.|
//+------------------------------------------------------------------+

#ifdef SERIES_FUNCTORS
  #ifndef EXTENDED_FUNCTORS
    #define EXTENDED_FUNCTORS
  #endif
#endif

#ifdef SYMBOL_FUNCTORS
  #ifndef EXTENDED_FUNCTORS
    #define EXTENDED_FUNCTORS
  #endif
#endif

#ifdef GLOBAL_VARS_FUNCTORS
  #ifndef EXTENDED_FUNCTORS
    #define EXTENDED_FUNCTORS
  #endif
#endif

#ifdef INDICATOR_FUNCTORS
  #ifndef EXTENDED_FUNCTORS
    #define EXTENDED_FUNCTORS
  #endif
#endif


#include "Functors.mqh"


#ifdef SERIES_FUNCTORS
#include "Functors/Series.mqh"
#endif


#ifdef SYMBOL_FUNCTORS
#include "Functors/SymbolProps.mqh"
#endif


#ifdef GLOBAL_VARS_FUNCTORS
#include "Functors/GlobalVars.mqh"
#endif


// TODO: OrdersTotal(), Order(index, property)
// TODO: PositionsTotal, Position(index, property)
// TODO: Account


#ifdef INDICATOR_FUNCTORS
#include "Functors/Indicators.mqh"
#endif
