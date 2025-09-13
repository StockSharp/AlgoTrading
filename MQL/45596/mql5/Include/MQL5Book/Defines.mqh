//+------------------------------------------------------------------+
//|                                                      Defines.mqh |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define STR_TIME_MSC(T) (TimeToString((T) / 1000, TIME_DATE|TIME_SECONDS) + StringFormat("'%03d", (T) % 1000))
#define PUSH(A,V) (A[ArrayResize(A, ArrayRange(A, 0) + 1, ArrayRange(A, 0) * 2) - 1] = V)
#define EXPAND(A) (ArrayResize(A, ArrayRange(A, 0) + 1, ArrayRange(A, 0) * 2) - 1)

//+------------------------------------------------------------------+
