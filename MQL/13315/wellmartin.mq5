//+------------------------------------------------------------------+
//|                                                  Well Martin.mq5 |
//|                                              Copyright 2015, AM2 |
//|                                      http://www.forexsystems.biz |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, AM2"
#property link      "http://www.forexsystems.biz"
#property version   "1.00"
//---
#include <Trade\Trade.mqh>            // Подключаем торговый класс CTrade
//--- входные параметры индикатора Bollinger Bands
input int      BBPeriod  = 84;        // Период Bollinger Bands
input int      BBShift   = 0;         // Смещение относительно графика
input double   BBDev     = 1.8;       // Стандартное отклонение
//--- входные параметры индикатора ADX
input int      ADXPeriod = 40;        // Период ADX
input int      ADXLevel  = 45;        // Уровень ADX
//--- входные параметры эксперта
input int      TP        = 1200;      // Тейк-профит
input int      SL        = 1400;      // Стоп-лосс
input int      Slip      = 50;        // Проскальзывание
input int      Stelth    = 0;         // 1-Режим стопы видит только пользователь
input double   KLot      = 2;         // Коэффициент умножения лота
input double   MaxLot    = 5;         // Максимальный лот, после которого лот начальный
input double   Lot       = 0.1;       // Количество лотов для торговли 
input color    LableClr  = clrGreen;  // Цвет метки
//--- глобальные переменные
int BBHandle;                         // Хэндл индикатора Bolinger Bands
int ADXHandle;                        // Хэндл индикатора ADX
double BBUp[],BBLow[];                // Динамические массивы для хранения численных значений Bollinger Bands
double ADX[];                         // Динамические массивы для хранения численных значений ADX
CTrade trade;                         // Используем торговый класс CTrade
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Получаем хэндл индикаторов  Bollinger Bands и ADX
   BBHandle=iBands(_Symbol,0,BBPeriod,BBShift,BBDev,PRICE_CLOSE);
   ADXHandle=iADX(_Symbol,0,ADXPeriod);

//--- Нужно проверить, не были ли возвращены значения Invalid Handle
   if(BBHandle==INVALID_HANDLE || ADXHandle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикаторов");
      return(INIT_FAILED);
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- освобождаем хэндлы индикаторов
   IndicatorRelease(BBHandle);
   IndicatorRelease(ADXHandle);
//--- удалим созданные метки
   ObjectsDeleteAll(0,0,OBJ_ARROW_LEFT_PRICE);
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- Будет содержать цены, объемы и спред для каждого бара
   MqlRates mrate[];
//--- Установим индексацию в массивах котировок и индикаторов  как в таймсериях
//--- массив котировок
   ArraySetAsSeries(mrate,true);
//--- массив значений индикаторов
   ArraySetAsSeries(BBUp,true);
   ArraySetAsSeries(BBLow,true);
   ArraySetAsSeries(ADX,true);
//--- Получим исторические данные последних 3-х баров
   if(CopyRates(_Symbol,_Period,0,3,mrate)<0)
     {
      Alert("Ошибка копирования исторических данных - ошибка:",GetLastError(),"!!");
      return;
     }
//--- Копируем значения индикатора Bolinger Bands используя хэндлы
   if(CopyBuffer(BBHandle,1,0,3,BBUp)<0 || CopyBuffer(BBHandle,2,0,3,BBLow)<0)
     {
      Alert("Ошибка копирования буферов индикатора Bollinger Bands - номер ошибки:",GetLastError(),"!");
      return;
     }
//--- Копируем значения индикатора ADX используя хэндлы
   if(CopyBuffer(ADXHandle,0,0,3,ADX)<0)
     {
      Alert("Ошибка копирования буферов индикатора ADX - номер ошибки:",GetLastError(),"!");
      return;
     }
//--- Лучшее предложение на покупку
   double Ask=SymbolInfoDouble(_Symbol,SYMBOL_ASK);
//--- Лучшее предложение на продажу                           
   double Bid=SymbolInfoDouble(_Symbol,SYMBOL_BID);
//--- Профит
   double pr=0;
//--- Стопы
   double stop=0,take=0;
//--- объявляем переменные типа boolean, они будут использоваться при проверке условий для покупки и продажи
//--- Пробой верхней границы Bolinger Bands и противоположная сделка
   bool Buy=Ask<BBLow[1] && ADX[1]<ADXLevel && (LastDealType()==0 || LastDealType()==2);
//--- Пробой нижней границы Bolinger Bands и противоположная сделка                             
   bool Sell=Bid>BBUp[1] && ADX[1]<ADXLevel && (LastDealType()==0 || LastDealType()==1);
//--- Проверка на новый бар  
   if(IsNewBar(_Symbol,0))
     {
      //--- Нет позиции и сигнал на покупку
      if(PositionsTotal()<1 && Buy)
        {
         //--- Вычисляем стопы
         if(SL==0)stop=0; else stop=NormalizeDouble(Ask-SL*_Point,_Digits);
         if(TP==0)take=0; else take=NormalizeDouble(Ask+TP*_Point,_Digits);
         //--- Стопы виртуальные 
         if(Stelth==1) {stop=0;take=0;}
         //--- Открываем ордер на покупку
         trade.PositionOpen(_Symbol,ORDER_TYPE_BUY,Volume(),Ask,stop,take);
         //--- Ставим виртуальные стопы
         if(Stelth==1) PutLable("SL"+DoubleToString(Ask,_Digits),TimeCurrent(),NormalizeDouble(Ask-SL*_Point,_Digits),LableClr);
         if(Stelth==1) PutLable("TP"+DoubleToString(Ask,_Digits),TimeCurrent(),NormalizeDouble(Ask+TP*_Point,_Digits),LableClr);
        }
      //--- Нет позиции и сигнал на продажу
      if(PositionsTotal()<1 && Sell)
        {
         //--- Вычисляем стопы
         if(SL==0)stop=0; else stop=NormalizeDouble(Bid+SL*_Point,_Digits);
         if(TP==0)take=0; else take=NormalizeDouble(Bid-TP*_Point,_Digits);
         //--- Стопы виртуальные 
         if(Stelth==1) {stop=0;take=0;}
         //--- Открываем ордер на продажу
         trade.PositionOpen(_Symbol,ORDER_TYPE_SELL,Volume(),Bid,stop,take);
         if(Stelth==1) PutLable("TP"+DoubleToString(Bid,_Digits),TimeCurrent(),NormalizeDouble(Bid-TP*_Point,_Digits),LableClr);
         if(Stelth==1) PutLable("SL"+DoubleToString(Bid,_Digits),TimeCurrent(),NormalizeDouble(Bid+SL*_Point,_Digits),LableClr);
        }
     }
//--- Закрытие по профиту
//--- Открыта позиция и режим Стелс
   if(PositionSelect(_Symbol) && Stelth==1)
     {
      //--- Открыта покупка
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
        {
         //--- Считаем профит
         pr=(Bid-PositionGetDouble(POSITION_PRICE_OPEN))/_Point;
         if(pr>=TP)
           {
            //--- Закрываем позицию
            trade.PositionClose(_Symbol);
           }
         if(pr<=-SL)
           {
            //--- Закрываем позицию
            trade.PositionClose(_Symbol);
           }
        }
      //--- Открыта продажа
      if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
        {
         //--- Считаем профит
         pr=(PositionGetDouble(POSITION_PRICE_OPEN)-Bid)/_Point;
         if(pr>=TP)
           {
            //--- Закрываем позицию
            trade.PositionClose(_Symbol);
           }
         if(pr<=-SL)
           {
            //--- Закрываем позицию
            trade.PositionClose(_Symbol);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//| Виртуальный стоп                                                 |
//+------------------------------------------------------------------+
void PutLable(const string name="",datetime time=0,double price=0,const color clr=clrGreen)
  {
//--- сбросим значение ошибки
   ResetLastError();
//--- Создаем метку
   if(!ObjectCreate(0,name,OBJ_ARROW_LEFT_PRICE,0,time,price))
     {
      Print(__FUNCTION__,
            ": не удалось создать левую ценовую метку! Код ошибки = ",GetLastError());
      return;
      //--- установим цвет метки
      ObjectSetInteger(0,name,OBJPROP_COLOR,clr);
      //--- установим стиль окаймляющей линии
      ObjectSetInteger(0,name,OBJPROP_STYLE,STYLE_SOLID);
      //--- установим размер метки
      ObjectSetInteger(0,name,OBJPROP_WIDTH,2);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsNewBar(string symbol,ENUM_TIMEFRAMES timeframe)
  {
//---- получим время появления текущего бара
   datetime TNew=datetime(SeriesInfoInteger(symbol,timeframe,SERIES_LASTBAR_DATE));
   datetime m_TOld=0;
//--- проверка на появление нового бара
   if(TNew!=m_TOld && TNew)
     {
      m_TOld=TNew;
      //--- появился новый бар!
      return(true);
      Print("Новый бар!");
     }
//--- новых баров пока нет!
   return(false);
  }
//+------------------------------------------------------------------+
//| Считаем лот в зависимости от полученного профита                 |
//+------------------------------------------------------------------+
double Volume(void)
  {
   double lot=Lot;
//--- Получим доступ к истории
   HistorySelect(0,TimeCurrent());
//--- Сделки в истории
   int orders=HistoryDealsTotal();
//--- Тикет последней сделки  
   ulong ticket=HistoryDealGetTicket(orders-1);
   if(ticket==0)
     {
      Print("Нет сделок в истории! ");
      lot=Lot;
     }
//--- Профит сделки
   double profit=HistoryDealGetDouble(ticket,DEAL_PROFIT);
//--- Лот сделки
   double lastlot=HistoryDealGetDouble(ticket,DEAL_VOLUME);
//--- Профит отрицательный
   if(profit<0.0)
     {
      //--- Увеличиваем следующий лот
      lot=lastlot*KLot;
      Print(" Cделка закрыта по стопу! ");
     }
//--- Приводим лот к минимальному
   double minvol=SymbolInfoDouble(_Symbol,SYMBOL_VOLUME_MIN);
   if(lot<minvol)
      lot=minvol;
//--- Если лот больше максимального то начальный лот
   if(lot>MaxLot)
      lot=Lot;
//--- Возвращаем торговый объем
   return(lot);
  }
//+------------------------------------------------------------------+
//| Смотрим тип последней закрытой сделки                            |
//+------------------------------------------------------------------+
int LastDealType(void)
  {
   int type=0;
//--- Получим доступ к истории
   HistorySelect(0,TimeCurrent());
//--- Сделки в истории
   int orders=HistoryDealsTotal();
//--- Тикет последней сделки  
   ulong ticket=HistoryDealGetTicket(orders-1);
//--- Нет сделок в истории
   if(ticket==0)
     {
      Print("Нет сделок в истории! ");
      type=0;
     }
   if(ticket>0)
     {
      //--- Последняя сделка BUY 
      if(HistoryDealGetInteger(ticket,DEAL_TYPE)==DEAL_TYPE_BUY)
        {
         type=2;
        }
      //--- Последняя сделка SELL
      if(HistoryDealGetInteger(ticket,DEAL_TYPE)==DEAL_TYPE_SELL)
        {
         type=1;
        }
     }
//---
   return(type);
  }
//+------------------------------------------------------------------+
