//+------------------------------------------------------------------+
//|                                                    IndCommon.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#define ON_CALCULATE_STD_FULL_PARAM_LIST \
const int rates_total,     \
const int prev_calculated, \
const datetime &time[],    \
const double &open[],      \
const double &high[],      \
const double &low[],       \
const double &close[],     \
const long &tick_volume[], \
const long &volume[],      \
const int &spread[]

#define ON_CALCULATE_STD_SHORT_PARAM_LIST \
const int rates_total,     \
const int prev_calculated, \
const int begin,           \
const double &data[]
//+------------------------------------------------------------------+
