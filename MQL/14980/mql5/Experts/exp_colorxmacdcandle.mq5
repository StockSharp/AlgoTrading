//+------------------------------------------------------------------+
//|                                         Exp_ColorXMACDCandle.mq5 |
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.10"
//+-------------------------------------------------+
//| Торговые алгоритмы                              |
//+-------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+-------------------------------------------------+
//| Перечисление для вариантов расчета лота         |
//+-------------------------------------------------+
/*enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счете
   BALANCE,          //MM от баланса средств на счете
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счете
   LOSSBALANCE,      //MM по убыткам от баланса средств на счете
   LOT               //Лот без изменения
  }; */
//+-------------------------------------------------+
//| Описание класса CXMA                            |
//+-------------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-------------------------------------------------+
//| Объявление перечислений                         |
//+-------------------------------------------------+
/*enum Smooth_Method - объявлено в файле SmoothAlgorithms.mqh
  {
   MODE_SMA_,  //SMA
   MODE_EMA_,  //EMA
   MODE_SMMA_, //SMMA
   MODE_LWMA_, //LWMA
   MODE_JJMA,  //JJMA
   MODE_JurX,  //JurX
   MODE_ParMA, //ParMA
   MODE_T3,    //T3
   MODE_VIDYA, //VIDYA
   MODE_AMA,   //AMA
  }; */
//+-------------------------------------------------+
//| Объявление перечислений                         |
//+-------------------------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_            //Low
  };
//+-------------------------------------------------+
//| Объявление перечислений                         |
//+-------------------------------------------------+
enum Mode //Тип константы
  {
   Histogram = 1,     //Изменение направления гистограммы
   SignalLine         //Пересечения гистограммы с сигнальной линией
  };
//+-------------------------------------------------+
//| Входные параметры эксперта                      |
//+-------------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Стоплосс в пунктах
input int    TakeProfit_=2000;    // Тейкпрофит в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в длинные позиции
input bool   SellPosOpen=true;    // Разрешение для входа в короткие позиции
input bool   BuyPosClose=true;    // Разрешение для выхода из длинных позиций
input bool   SellPosClose=true;   // Разрешение для выхода из коротких позиций
//+-------------------------------------------------+
//| Входные параметры индикатора ColorXMACDCandle   |
//+-------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Таймфрейм индикатора ColorXMACDCandle
input Mode SignalMode=Histogram; // Источник торгового сигнала
input Smooth_Method XMA_Method=MODE_T3; // Метод усреднения гистограммы
input int Fast_XMA = 12; // Период быстрого мувинга
input int Slow_XMA = 26; // Период медленного мувинга
input int XPhase = 100;  // Параметр усреднения мувингов
//--- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//--- для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method Signal_Method=MODE_JJMA; // Метод усреднения сигнальной линии
input int Signal_XMA=9; // Период сигнальной линии 
input int Signal_Phase=100; // Параметр сигнальной линии
//--- изменяющийся в пределах -100 ... +100,
//--- влияет на качество переходного процесса;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// Ценовая константа сигнальной линии
input uint SignalBar=1;                            // Номер бара для получения сигнала входа
//+-------------------------------------------------+
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
//---- получение хендла индикатора ColorXMACDCandle
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorXMACDCandle",XMA_Method,Fast_XMA,Slow_XMA,XPhase,Signal_Method,Signal_XMA,Signal_Phase,AppliedPrice);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ColorXMACDCandle");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   min_rates_total=MathMax(GetStartBars(XMA_Method,Fast_XMA,XPhase),GetStartBars(XMA_Method,Slow_XMA,XPhase));
   min_rates_total+=GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;
   min_rates_total+=int(3+SignalBar);
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
//---- объявление локальных переменных
   double Clr[2];
//---- объявление статических переменных
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//---- определение сигналов для сделок
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // проверка на появление нового бара
     {
      //---- обнулим торговые сигналы
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      //----
      if(SignalMode==Histogram)
        {
         //---- копируем вновь появившиеся данные в массивы
         if(CopyBuffer(InpInd_Handle,4,SignalBar,2,Clr)<=0) {Recount=true; return;}
         //---- получим сигналы для покупки
         if(Clr[1]==2)
           {
            if(BuyPosOpen && Clr[0]<2) BUY_Open=true;
            if(SellPosClose)SELL_Close=true;
            UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
         //---- получим сигналы для продажи
         if(Clr[1]==0)
           {
            if(SellPosOpen && Clr[0]>0) SELL_Open=true;
            if(BuyPosClose) BUY_Close=true;
            DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
        }
      //----
      if(SignalMode==SignalLine)
        {
         //---- копируем вновь появившиеся данные в массивы
         if(CopyBuffer(InpInd_Handle,7,SignalBar,2,Clr)<=0) {Recount=true; return;}
         //---- получим сигналы для покупки
         if(Clr[1]==1)
           {
            if(BuyPosOpen  &&  Clr[0]!=1) BUY_Open=true;
            if(SellPosClose)SELL_Close=true;
            UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
         //---- получим сигналы для продажи
         if(Clr[1]==2)
           {
            if(SellPosOpen && Clr[0]!=2) SELL_Open=true;
            if(BuyPosClose) BUY_Close=true;
            DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
           }
        }
     }
//---- совершение сделок
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
