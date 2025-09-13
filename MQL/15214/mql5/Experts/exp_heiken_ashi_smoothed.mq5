//+------------------------------------------------------------------+
//|                                     Exp_Heiken_Ashi_Smoothed.mq5 |
//|                             Copyright © 2011,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.10"
//+----------------------------------------------------+
//|  объявление перечислений                           |
//+----------------------------------------------------+
enum Smooth_Method
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  };
//+----------------------------------------------+
//| Торговые алгоритмы                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//|  Перечисление для вариантов расчета лота     |
//+----------------------------------------------+
/*enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счете
   BALANCE,          //MM от баланса средств на счете
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счете
   LOSSBALANCE,      //MM по убыткам от баланса средств на счете
   LOT               //Лот без изменения
  }; */
//+------------------------------------------------+
//| Входные параметры эксперта                     |
//+------------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Стоплосс в пунктах
input int    TakeProfit_=2000;    // Тейкпрофит в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в длинные позиции
input bool   SellPosOpen=true;    // Разрешение для входа в короткие позиции
input bool   BuyPosClose=true;    // Разрешение для выхода из длинных позиций
input bool   SellPosClose=true;   // Разрешение для выхода из коротких позиций
//+------------------------------------------------+
//| Входные параметры индикатора                   |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H1; //таймфрейм индикатора Heiken_Ashi_Smoothed
input Smooth_Method MA_Method=MODE_JJMA;          //Метод усреднения
input int Length=30;                              //Глубина  усреднения                    
input int Phase=100;                              //Параметр усреднения,
                                                  //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input uint SignalBar=1;                           //номер бара для получения сигнала входа
//+------------------------------------------------+
int TimeShiftSec;
//---- Объявление целочисленных переменных для хендлов индикаторов
int InpInd_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//  Торговые алгоритмы                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- получение хендла индикатора Heiken_Ashi_Smoothed
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"Heiken_Ashi_Smoothed",MA_Method,Length,Phase);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора Heiken_Ashi_Smoothed");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(Length,30)+SignalBar+1);
//--- завершение инициализации
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//----
   GlobalVariableDel_(Symbol());
//----
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
//---- Объявление локальных переменных
   double TrendValue[2];
//---- Объявление статических переменных
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
      //---- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,4,SignalBar,2,TrendValue)<=0) {Recount=true; return;}
      //---- Получим сигналы для покупки
      if(TrendValue[0]==1 && TrendValue[1]==0)
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose)SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- Получим сигналы для продажи
      if(TrendValue[0]==0 && TrendValue[1]==1)
        {
         if(SellPosOpen) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- Поиск последнего направления торговли для получения сигналов для закрывания позиций
      //if(!MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)) //если режим торговли в тестере "Произвольная задержка" 
        {
         if(SellPosOpen && SellPosClose  &&  TrendValue[1]==0) SELL_Close=true;
         if(BuyPosOpen  &&  BuyPosClose  &&  TrendValue[1]==1) BUY_Close=true;
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
//----
  }
//+------------------------------------------------------------------+
