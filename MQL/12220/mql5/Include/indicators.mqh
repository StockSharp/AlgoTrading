//+------------------------------------------------------------------+
//| Indicators.mqh                                                   |
//| Copyright 2015, Vasiliy Sokolov, St. Petersburg, Russia.         |
//| https://login.mql5.com/ru/users/c-4                              |
//+------------------------------------------------------------------+
#property copyright "Copyright 2014, Vasiliy Sokolov."
#property link "http://www.mql5.com"

//+------------------------------------------------------------------+
//| Class provides a convenient interface to popular indicators.     |
//+------------------------------------------------------------------+
class CIndicators
{
public:
double iHighest(string symbol, ENUM_TIMEFRAMES tf, int count, int start_pos=1);
double iLowest(string symbol, ENUM_TIMEFRAMES tf, int count, int start_pos=1);
double iClose(string symbol, ENUM_TIMEFRAMES tf, int shift);
double iRSI(string symbol, ENUM_TIMEFRAMES tf, int period, ENUM_APPLIED_PRICE applied, int shift);
double iCCI(string symbol, ENUM_TIMEFRAMES tf, int period, ENUM_APPLIED_PRICE applied, int shift);
};

//+---------------------------------------------------------------------+
//| Returns the maximum price achieved in the past                      |
//| count bars starting from start_pos bar.                             |
//| from start_pos bar.                                                 |
//| INPUT PARAMETERS                                                    |
//| symbol - Symbol tool.                                               |
//| tf - timeframe of the instrument, is equal to NULL, if you use the  |
//| the current timeframe (see enumeration ENUM_TIMEFRAMES).            |
//| count - the number of bars from starting positions                  |
//| start_pos=1 - the number of the bar from which to start counting    |
//| the extremum.                                                       |
//| RESULT                                                              |
//| Maximum price per count bars. Null if the price get                 |
//| failed.                                                             | 
//+---------------------------------------------------------------------+
double CIndicators::iHighest(string symbol, ENUM_TIMEFRAMES tf, int count, int start_pos=1)
{
//---
double high[];
CopyHigh(symbol, tf, start_pos, count, high);
if(ArraySize(high) == 0)return 0.0;
double max = high[ArrayMaximum(high)];
return max;
//---
}
//+---------------------------------------------------------------------+
//| Returns the price for at least the last count bars                  |
//| from start_pos bar.                                                 |
//| INPUT PARAMETERS                                                    |
//| symbol - Symbol tool.                                               |
//| tf - timeframe of the instrument, is equal to NULL, if you use the  |
//| the current timeframe (see enumeration ENUM_TIMEFRAMES).            |
//| count - the number of bars from the starting position               |
//| start_pos=1 - the number of the bar from which to start counting    |
//| the extremum.                                                       |
//| RESULT                                                              |
//| Maximum price per count bars. Null if the price get                 |
//| failed.                                                             |   
//+---------------------------------------------------------------------+
double CIndicators::iLowest(string symbol, ENUM_TIMEFRAMES tf, int count, int start_pos=1)
{
//---
double low[];
CopyLow(symbol, tf, start_pos, count, low);
if(ArraySize(low) == 0)return 0.0;
double min = low[ArrayMinimum(low)];
return min;
//---
}
//+---------------------------------------------------------------------+
//| Returns the value of the closing price of the bar (specified by the |
//| shift) the corresponding graph.                                     |
//| INPUT PARAMETERS                                                    |
//| symbol - Symbol tool.                                               |
//| tf - timeframe of the instrument, is equal to NULL, if you use the  |
//| the current timeframe (see enumeration ENUM_TIMEFRAMES).            |
//| shift - the number of bars from the starting position.              |
//| RESULT                                                              |
//| The closing price of the bar. Null if the price get                 |
//| failed.                                                             | 
//+---------------------------------------------------------------------+
double CIndicators::iClose(string symbol, ENUM_TIMEFRAMES tf, int shift)
{
double close[];
CopyClose(symbol, tf, shift, 1, close);
if(ArraySize(close))
return close[0];
return 0.0;
}
//+---------------------------------------------------------------------+
//| Returns the value of the RSI indicator for a bar with offset shift. |
//| INPUT PARAMETERS                                                    |
//| symbol - Symbol tool.                                               |
//| tf - timeframe of the instrument, is equal to NULL, if you use the  |
//| the current timeframe (see enumeration ENUM_TIMEFRAMES).            |
//| shift - the number of bars from the starting position.              |
//| RESULT                                                              |
//| The value of the RSI indicator. Null if the price get               |
//| failed.                                                             | 
//+---------------------------------------------------------------------+
double CIndicators::iRSI(string symbol, ENUM_TIMEFRAMES tf, int period, ENUM_APPLIED_PRICE applied, int shift)
{
//--- 
int hRSI = iRSI(symbol, tf, period, applied);
if(hRSI == INVALID_HANDLE)
return 0.0;
double rsi[];
CopyBuffer(hRSI, 0, shift, 1, rsi);
if(ArraySize(rsi))
return rsi[0];
return 0.0;
//---
}
//+---------------------------------------------------------------------+
//| Returns the value of the CCI indicator for a bar with offset shift. |
//| INPUT PARAMETERS                                                    |
//| symbol - Symbol tool.                                               |
//| tf - timeframe of the instrument, is equal to NULL, if you use the  |
//| the current timeframe (see enumeration ENUM_TIMEFRAMES).            |
//| shift - the number of bars from the starting position.              |
//| RESULT                                                              |
//| The value of the CCI indicator. Null if the price get               |
//| failed.                                                             | 
//+---------------------------------------------------------------------+
double CIndicators::iCCI(string symbol, ENUM_TIMEFRAMES tf, int period, ENUM_APPLIED_PRICE applied, int shift)
{
int hCCI = iCCI(Symbol(), PERIOD_CURRENT, 55, PRICE_CLOSE);
if(hCCI == INVALID_HANDLE)return 0.0;
double cci[];
ArrayResize(cci, 1);
CopyBuffer(hCCI, 0, 0, 1, cci);
return cci[0];
}