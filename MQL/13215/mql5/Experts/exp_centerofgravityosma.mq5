//+------------------------------------------------------------------+
//|                                      Exp_CenterOfGravityOSMA.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+---------------------------------------------------+
//| Торговые алгоритмы                                |
//+---------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+---------------------------------------------------+
//| Объявление перечислений                           |
//+---------------------------------------------------+
enum Applied_price_ //тип константы
  {
   PRICE_CLOSE_ = 1,     //PRICE_CLOSE
   PRICE_OPEN_,          //PRICE_OPEN
   PRICE_HIGH_,          //PRICE_HIGH
   PRICE_LOW_,           //PRICE_LOW
   PRICE_MEDIAN_,        //PRICE_MEDIAN
   PRICE_TYPICAL_,       //PRICE_TYPICAL
   PRICE_WEIGHTED_,      //PRICE_WEIGHTED
   PRICE_SIMPL_,         //PRICE_SIMPL_
   PRICE_QUARTER_,       //PRICE_QUARTER_
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price
  };
//+---------------------------------------------------+
//| Входные параметры эксперта                        |
//+---------------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Стоплосс в пунктах
input int    TakeProfit_=2000;    // Тейкпрофит в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в длинную позицию
input bool   SellPosOpen=true;    // Разрешение для входа в короткую позицию
input bool   BuyPosClose=true;    // Разрешение для выхода из длинных позиций
input bool   SellPosClose=true;   // Разрешение для выхода из коротких позиций
//+-----------------------------------------------+
//| Входные параметры для индикаторов 2pbIdealMA  |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Таймфрейм индикатора
input uint SignalBar=1; // Номер бара для получения сигнала входа
//+-----------------------------------------------+
//| Входные параметры для индикатора              |
//+-----------------------------------------------+
input uint Period_=10; // Период усреднения индикатора
input uint SmoothPeriod1=3; // Период сглаживания сигнальной линии
input ENUM_MA_METHOD MA_Method_1=MODE_SMA; // Метод усреднения сигнальной линии
input uint SmoothPeriod2=3; // Период сглаживания сигнальной линии
input ENUM_MA_METHOD MA_Method_2=MODE_SMA; // Метод усреднения сигнальной линии
input Applied_price_ AppliedPrice=PRICE_CLOSE_;// Ценовая константа
//+-----------------------------------------------+
//---- объявление целочисленных переменных для хранения периода графика в секундах 
int TimeShiftSec;
//---- объявление целочисленных переменных для хендлов индикаторов
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
//---- получение хендла индикатора CenterOfGravityOSMA
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"CenterOfGravityOSMA",
                         Period_,SmoothPeriod1,MA_Method_1,SmoothPeriod2,MA_Method_2,AppliedPrice);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("Не удалось получить хендл индикатора CenterOfGravityOSMA");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(Period_+1+SmoothPeriod1+SmoothPeriod2+2);
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
   double macd[3];
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
      //---- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,0,SignalBar,3,macd)<=0) {Recount=true; return;}
      //---- получим сигналы для покупки
      if(macd[1]<macd[2])
        {
         if(BuyPosOpen && macd[0]>macd[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- получим сигналы для продажи
      if(macd[1]>macd[2])
        {
         if(SellPosOpen && macd[0]<macd[1]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
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
  }
//+------------------------------------------------------------------+
