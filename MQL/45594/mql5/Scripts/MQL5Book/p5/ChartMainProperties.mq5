//+------------------------------------------------------------------+
//|                                          ChartMainProperties.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(_Symbol);
   PRTF(Symbol());
   PRTF(_Period);
   PRTF(Period());
   PRTF(_Point);
   PRTF(Point());
   PRTF(_Digits);
   PRTF(Digits());
   PRTF(DoubleToString(_Point, _Digits));
   PRTF(EnumToString(_Period));
}
//+------------------------------------------------------------------+
/*
   example output:
   
   _Symbol=EURUSD / ok
   Symbol()=EURUSD / ok
   _Period=16385 / ok
   Period()=16385 / ok
   _Point=1e-05 / ok
   Point()=1e-05 / ok
   _Digits=5 / ok
   Digits()=5 / ok
   DoubleToString(_Point,_Digits)=0.00001 / ok
   EnumToString(_Period)=PERIOD_H1 / ok
*/
//+------------------------------------------------------------------+
