//+------------------------------------------------------------------+
//|                                                   AutoTrader.mq5 |
//|                                              Copyright 2016, AM2 |
//|                                      http://www.forexsystems.biz |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, AM2"
#property link      "http://www.forexsystems.biz"
#property version   "1.00"

#define VK_CONTROL 0x11 //CTRL key
#define KEY_CODE   'E'

// Подключаем библиотеку Windows для распознавания нажатия клавиш
#import "user32.dll"
void  keybd_event(int bVk,int bScan,int dwFlags,int dwExtraInfo);
#import

// Используем класс CTrade
#include<Trade\Trade.mqh>

//Объект класса CTrade
CTrade trade;

// Объект класса CPositionInfo
CPositionInfo position;

// Входные переменные
input int StartHour   = 9;   // Час начала торговли
input int StartMinute = 30;  // Минута начала торговли
input int StopHour    = 23;  // Час окончания торговли
input int StopMinute  = 30;  // Минута окончания торговли
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//|  Нажимаем клавиши Ctrl+E                                         |
//+------------------------------------------------------------------+
void Key()
  {
   keybd_event(VK_CONTROL,0,0,0);
   Sleep(10);
   keybd_event(KEY_CODE,0,0,0);
   Sleep(10);
   keybd_event(KEY_CODE,0,2,0);
   Sleep(10);
   keybd_event(VK_CONTROL,0,2,0);
  }
//+------------------------------------------------------------------+
//|   Проверка времени торговли                                      |
//+------------------------------------------------------------------+
bool TimeSession(int aStartHour,int aStartMinute,int aStopHour,int aStopMinute,datetime aTimeCur)
  {
//--- время начала сессии
   int StartTime=3600*aStartHour+60*aStartMinute;
//--- время окончания сессии
   int StopTime=3600*aStopHour+60*aStopMinute;
//--- текущее время в секундах от начала дня
   aTimeCur=aTimeCur%86400;
   if(StopTime<StartTime)
     {
      //--- переход через полночь
      if(aTimeCur>=StartTime || aTimeCur<StopTime)
        {
         return(true);
        }
     }
   else
     {
      //--- внутри одного дня
      if(aTimeCur>=StartTime && aTimeCur<StopTime)
        {
         return(true);
        }
     }
   return(false);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
// закрываем все позиции по времени
   if(!TimeSession(StartHour,StartMinute,StopHour,StopMinute,TimeCurrent()))
     {
      for(int i=0; i<PositionsTotal(); i++)
        {
         if(position.Select(PositionGetSymbol(i)))
           {
            // закрыть открытую позицию по этому символу
            trade.PositionClose(PositionGetSymbol(i));
           }
        }
     }

// включаем по времени
   if(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)==0 && TimeSession(StartHour,StartMinute,StopHour,StopMinute,TimeCurrent())) Key();
// выключаем по времени
   if(TerminalInfoInteger(TERMINAL_TRADE_ALLOWED)==1 && !TimeSession(StartHour,StartMinute,StopHour,StopMinute,TimeCurrent())) Key();

   Comment("\n Enable: ",TerminalInfoInteger(TERMINAL_TRADE_ALLOWED),
           "\n Time: ",TimeCurrent());
  }
//+------------------------------------------------------------------+
