//+------------------------------------------------------------------+
//|                                             Exp_i-BandsPrice.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.10"
//+----------------------------------------------+
//| Описание классов усреднений                  |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
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
//| Объявление перечислений                      |
//+----------------------------------------------+
enum Applied_price_ //тип константы
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
/*enum Smooth_Method - перечисление объявлено в файле SmoothAlgorithms.mqh
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
   MODE_AMA    //AMA
  }; */
//+----------------------------------------------+
//| Входные параметры эксперта                   |
//+----------------------------------------------+
input double MM=0.1;               // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;       // Способ определения размера лота
input int    StopLoss_=1000;       // Стоплосс в пунктах
input int    TakeProfit_=2000;     // Тейкпрофит в пунктах
input int    Deviation_=10;        // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;      // Разрешение для входа в длинные позиции
input bool   SellPosOpen=true;     // Разрешение для входа в короткие позиции
input bool   BuyPosClose=true;     // Разрешение для выхода из длинных позиций
input bool   SellPosClose=true;    // Разрешение для выхода из коротких позиций
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H1;  // Таймфрейм индикатора

input Smooth_Method XMA_Method=MODE_SMA;           // Метод первого усреднения
input uint XLength=100;                            // Глубина  первого сглаживания
input int XPhase=15;                               // Параметр первого усреднения
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input uint BandsPeriod=100;                        // Период усреднения BB
input double BandsDeviation=2.0;                   // Девиация
input uint Smooth=5;                               // Период сглаживания индикатора
input Applied_price_ IPC=PRICE_CLOSE;              // Ценовая константа
input int UpLevel=+25;                             // Верхний уровень
input int DnLevel=-25;                             // Нижний уровень
input uint SignalBar=1;                            // Номер бара для получения сигнала входа
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
//---- получение хендла индикатора i-BandsPrice
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"i-BandsPrice",XMA_Method,XLength,XPhase,BandsPeriod,BandsDeviation,Smooth,IPC,UpLevel,DnLevel,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора i-BandsPrice");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   min_rates_total=GetStartBars(XMA_Method,XLength,XPhase)+1;
   min_rates_total+=int(BandsPeriod);
   min_rates_total+=30;
   min_rates_total+=int(2+SignalBar);
//----
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
   double Value[2];
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
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Value)<=0) {Recount=true; return;}
      //---- получим сигналы для покупки
      if(BuyPosOpen && Value[1]==4 && Value[0]<4)
        {
         BUY_Open=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      if(SellPosClose && Value[1]>1) SELL_Close=true;
      //---- получим сигналы для продажи
      if(SellPosOpen && Value[1]==0 && Value[0]>0)
        {
         SELL_Open=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      if(BuyPosClose && Value[1]<2) BUY_Close=true;
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
