//+------------------------------------------------------------------+
//|                                         Exp_TrendlessAG_Hist.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Описание классов усреднений                  |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//--- объявление переменных класса CXMA из файла SmoothAlgorithms.mqh
CXMA XMA1;
//+----------------------------------------------+
//| Торговые алгоритмы                           | 
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simpl Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price
   PRICE_DEMARK_         //Demark Price
  };
//+----------------------------------------------+
//| Входные параметры индикатора эксперта        |
//+----------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Stop Loss в пунктах
input int    TakeProfit_=2000;    // Take Profit в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в лонг
input bool   SellPosOpen=true;    // Разрешение для входа в шорт
input bool   BuyPosClose=true;    // Разрешение для выхода из лонгов
input bool   SellPosClose=true;   // Разрешение для выхода из шортов
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H12; // Таймфрейм индикатора
input Smooth_Method XMA_Method1=MODE_EMA;          // Метод усреднения  в формуле осциллятора
input int XLength1=7;                              // Период скользящей средней в формуле осциллятора                 
input int XPhase1=15;                              // Параметр скользящей средней в формуле осциллятора
//--- XPhase1: для JJMA изменяется в пределах -100 ... +100, влияет на качество переходного процесса в формуле осциллятора
//--- XPhase1: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;              // Входная цена скользящей средней в формуле осциллятора
input uint PointsCount=600;                        // Количество точек для расчета индикатора. Форекс-клуб рекомендует месяц для часовиков
input uint In100=90;                               // Сколько % точек индикатора должны входить в интервал +-100%
input Smooth_Method XMA_Method2=MODE_JJMA;         // Метод сглаживания индикатора
input int XLength2=5;                              // Глубина сглаживания
input int XPhase2=100;                             // Параметр сглаживания
//--- XPhase2: для JJMA изменяется в пределах -100 ... +100, влияет на качество переходного процесса;
//--- XPhase2: для VIDIA это период CMO, для AMA это период медленной скользящей
input uint SignalBar=1;                            // Номер бара для получения сигнала входа
//+----------------------------------------------+
//--- объявление целочисленных переменных для хранения периода графика в секундах 
int TimeShiftSec;
//--- объявление целочисленных переменных для хендлов индикаторов
int InpInd_Handle;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- получение хендла индикатора TrendlessAG_Hist
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"TrendlessAG_Hist",0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора TrendlessAG_Hist");
      return(INIT_FAILED);
     }
//--- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- инициализация переменных начала отсчёта данных
   int min_rates_1=XMA1.GetStartBars(XMA_Method1,XLength1,XPhase1);
   int min_rates_2=int(PointsCount+min_rates_1);
   min_rates_total=min_rates_2+XMA1.GetStartBars(XMA_Method2,XLength2,XPhase2)+2;
   min_rates_total+=int(3+SignalBar);
//---
   return(INIT_FAILED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   GlobalVariableDel_(Symbol());
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//--- подгрузка истории для нормальной работы функций IsNewBar() и SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//--- объявление локальных переменных
   double Value[3];
//--- объявление статических переменных
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//--- определение сигналов для сделок
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // проверка на появление нового бара
     {
      //--- обнулим торговые сигналы
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      //--- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}
      //--- получим сигналы для покупки
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- получим сигналы для продажи
      if(Value[1]>Value[2])
        {
         if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }
//--- совершение сделок
//--- закрываем лонг
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//--- закрываем шорт   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//--- открываем лонг
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//--- открываем шорт
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---
  }
//+------------------------------------------------------------------+