//+------------------------------------------------------------------+
//|                                                   Exp_MACD-2.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Объявление перечислений                      |
//+----------------------------------------------+
enum TREND_MODE //Тип константы
  {
   HISTOGRAM = 1,     //Изменение направления движения гистограммы
   CLOUD,             //Изменение цвета облака
   ZERO               //Пробой гистограммой MACD нуля
  };
//+----------------------------------------------+
//| Торговые алгоритмы                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| Перечисление для вариантов расчета лота      |
//+----------------------------------------------+
/*enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счете
   BALANCE,          //MM от баланса средств на счете
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счете
   LOSSBALANCE,      //MM по убыткам от баланса средств на счете
   LOT               //Лот без изменения
  }; */
//+----------------------------------------------+
//| Входные параметры эксперта                   |
//+----------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Стоплосс в пунктах
input int    TakeProfit_=2000;    // Тейкпрофит в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в длинные позиции
input bool   SellPosOpen=true;    // Разрешение для входа в короткие позиции
input bool   BuyPosClose=true;    // Разрешение для выхода из длинных позиций
input bool   SellPosClose=true;   // Разрешение для выхода из коротких позиций
//+----------------------------------------------+
//| Входные параметры индикатора MACD-2          |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Таймфрейм индикатора
input TREND_MODE TrendMode=CLOUD; // Вариант определения тренда
input uint FastMACD     = 12;
input uint SlowMACD     = 26;
input uint SignalMACD   = 9;
input ENUM_APPLIED_PRICE   PriceMACD=PRICE_CLOSE;
input uint SignalBar=1; // Номер бара для получения сигнала входа
//+----------------------------------------------+
//---- объявление целочисленных переменных для хранения периода графика в секундах 
int TimeShiftSec;
//---- объявление целочисленных переменных для хендлов индикаторов
int InpInd_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- получение хендла индикатора MACD-2
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"MACD-2",FastMACD,SlowMACD,SignalMACD,PriceMACD);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора MACD-2");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(SignalMACD+MathMax(FastMACD,SlowMACD));
   min_rates_total+=int(3+SignalBar);
//---- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   GlobalVariableDel_(Symbol());
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---- проверка количества баров на достаточность для расчета
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//---- подгрузка истории для нормальной работы функций IsNewBar() и SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//---- объявление статических переменных
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//+----------------------------------------------+
//| Определение сигналов для сделок              |
//+----------------------------------------------+
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // проверка на появление нового бара
     {
      //---- обнулим торговые сигналы
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      if(TrendMode==HISTOGRAM)
        {
         //---- объявление локальных переменных
         double Value[3];
         //---- копируем вновь появившиеся данные в массивы
         if(CopyBuffer(InpInd_Handle,2,SignalBar,3,Value)<=0) {Recount=true; return;}
         //---- получим сигналы для покупки
         if(Value[1]<Value[2])
           {
            if(BuyPosOpen && Value[0]>Value[1])
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }
         //---- получим сигналы для продажи
         if(Value[1]>Value[2])
           {
            if(SellPosOpen && Value[0]<Value[1])
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
       if(TrendMode==CLOUD)
        {
         //---- объявление локальных переменных
         double Up[2],Dn[2];
         //---- копируем вновь появившиеся данные в массивы
         if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Up)<=0) {Recount=true; return;}
         if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Dn)<=0) {Recount=true; return;}
         //---- получим сигналы для покупки
         if(Up[1]>Dn[1])
           {
            if(BuyPosOpen && Up[0]<Dn[0])
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }
         //---- получим сигналы для продажи
         if(Up[1]<Dn[1])
           {
            if(SellPosOpen && Up[0]>Dn[0])
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
      if(TrendMode==ZERO)
        {
         //---- объявление локальных переменных
         double Value[2];
         //---- копируем вновь появившиеся данные в массивы
         if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Value)<=0) {Recount=true; return;}
         //---- получим сигналы для покупки
         if(Value[1]>0)
           {
            if(BuyPosOpen && Value[0]<=0)
              {
               BUY_Open=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(SellPosClose) SELL_Close=true;
           }
         //---- получим сигналы для продажи
         if(Value[1]<0)
           {
            if(SellPosOpen && Value[0]>=0)
              {
               SELL_Open=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            if(BuyPosClose) BUY_Close=true;
           }
        }
     }
//+----------------------------------------------+
//| Совершение сделок                            |
//+----------------------------------------------+
//---- закрываем длинную позицию
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//---- закрываем короткую позицию
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//---- открываем длинную позицию
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---- открываем короткую позицию
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
  }
//+------------------------------------------------------------------+
