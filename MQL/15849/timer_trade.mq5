//+------------------------------------------------------------------+
//|                                                  Timer Trade.mq5 |
//|                                                        Oschenker |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Oschenker"
#property link      "https://www.mql5.com"
#property version   "1.00"
//--- Входные параметры
input int      TimerTime=30; //Задержка таймера (сек).
input int      TradeValue = 1; // Рабочий лот
input int      StopLossLevel = 10; // Stop Loss (points)
input int      TakeProfitLevel = 50; //Величина Take Profit (points)

//--- Включение стандартной торговой библиотеки
#include <Trade\Trade.mqh>;

CTrade  trade;

int Trigger = 0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create timer
   if(EventSetTimer(TimerTime * 100)) Print("Timer Setup to ", TimerTime, " sec.");
   else Print("Timer Error ", GetLastError());   
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- destroy timer
   EventKillTimer();

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- Событие не используется
   
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
//--- Поступил сигнал от таймера - отметим в логе
  Print("Time to Deal");
  
//--- Заключим сделку покупки или продажи в зависимости от положения треггера
   if(Trigger == 0)
   {
      if(!trade.Buy( TradeValue, NULL, 0, SymbolInfoDouble(Symbol(), SYMBOL_BID) - Point() * StopLossLevel, SymbolInfoDouble(Symbol(), SYMBOL_ASK) + Point() * TakeProfitLevel)) 
         {
               
//--- Покупка неудалась               
         Print("Метод Buy() потерпел неудачу. Код возврата=",trade.ResultRetcode(),". Описание кода: ",trade.ResultRetcodeDescription());
         }
      else
         {
///--- В случае удачной покупки переключим триггер
         Trigger = 1;
         }
     }
    if(Trigger == 1)
         {
         if(!trade.Sell( TradeValue, NULL, 0, SymbolInfoDouble(Symbol(), SYMBOL_ASK) + Point() * StopLossLevel, SymbolInfoDouble(Symbol(), SYMBOL_BID) - Point() * TakeProfitLevel)) 
               {
//--- Продажа неудалась
               Print("Метод Sell() потерпел неудачу. Код возврата=",trade.ResultRetcode(),". Описание кода: ",trade.ResultRetcodeDescription());
               }
             else
               {
///--- В случае успешной продажи переключим триггер
         Trigger = 0;
               }
         }
    }
//+------------------------------------------------------------------+
