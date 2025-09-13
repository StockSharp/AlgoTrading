//+------------------------------------------------------------------+
//|                                                   SeriesBars.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const datetime target = PRTF(ChartTimeOnDropped());
   PRTF(iBarShift(NULL, 0, target));
   PRTF(iBarShift(NULL, 0, target, true));
   PRTF(iBarShift(NULL, 0, TimeCurrent()));
   PRTF(Bars(NULL, 0, target, TimeCurrent()));
   PRTF(Bars(NULL, 0, TimeCurrent(), target));
   PRTF(iBars(NULL, 0));
   PRTF(Bars(NULL, 0));
   PRTF(Bars(NULL, 0, 0, TimeCurrent()));
   PRTF(Bars(NULL, 0, TimeCurrent(), TimeCurrent()));
   
   PRTF(Bars("EURUSD", PERIOD_H1, D'2021.05.01', D'2021.09.01'));
   PRTF(Bars("XAUUSD", PERIOD_H1, D'2021.05.01', D'2021.09.01'));
   PRTF(Bars("USDRUB", PERIOD_H1, D'2021.05.01', D'2021.09.01'));
   PRTF(iBarShift("EURUSD", PERIOD_H1, D'2021.09.01'));
   PRTF(iBarShift("XAUUSD", PERIOD_H1, D'2021.09.01'));
   PRTF(iBarShift("USDRUB", PERIOD_H1, D'2021.09.01'));
   /*
      output example (dropped in past/on quotes)
      
      ChartTimeOnDropped()=2021.10.01 09:00:00 / ok
      iBarShift(NULL,0,target)=125 / ok
      iBarShift(NULL,0,target,true)=125 / ok
      iBarShift(NULL,0,TimeCurrent())=0 / ok
      Bars(NULL,0,target,TimeCurrent())=126 / ok
      Bars(NULL,0,TimeCurrent(),target)=126 / ok
      iBars(NULL,0)=10004 / ok
      Bars(NULL,0)=10004 / ok
      Bars(NULL,0,0,TimeCurrent())=10004 / ok
      Bars(NULL,0,TimeCurrent(),TimeCurrent())=0 / ok
      Bars(EURUSD,PERIOD_H1,D'2021.05.01',D'2021.09.01')=2087 / ok
      Bars(XAUUSD,PERIOD_H1,D'2021.05.01',D'2021.09.01')=1991 / ok
      Bars(USDRUB,PERIOD_H1,D'2021.05.01',D'2021.09.01')=694 / ok
      iBarShift(EURUSD,PERIOD_H1,D'2021.09.01')=671 / ok
      iBarShift(XAUUSD,PERIOD_H1,D'2021.09.01')=638 / ok
      iBarShift(USDRUB,PERIOD_H1,D'2021.09.01')=224 / ok
      
      output example (dropped in future/on empty margin on the right side)
      
      ChartTimeOnDropped()=2021.10.09 02:30:00 / ok
      iBarShift(NULL,0,target)=0 / ok
      iBarShift(NULL,0,target,true)=-1 / ok
      iBarShift(NULL,0,TimeCurrent())=0 / ok
      Bars(NULL,0,target,TimeCurrent())=0 / ok
      Bars(NULL,0,TimeCurrent(),target)=0 / ok
      iBars(NULL,0)=10004 / ok
      Bars(NULL,0)=10004 / ok
      Bars(NULL,0,0,TimeCurrent())=10004 / ok
      Bars(NULL,0,TimeCurrent(),TimeCurrent())=0 / ok
      ...
   */
}
//+------------------------------------------------------------------+
