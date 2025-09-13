//+------------------------------------------------------------------+
//|                                        Exp_ColorZerolagX10MA.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.01"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
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
//---
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
   MODE_AMA,   //AMA
  }; */
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
//+------------------------------------------------+
//| Входные параметры индикатора ColorZerolagX10MA |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Таймфрейм индикатора
input Smooth_Method XMA_Method=MODE_JJMA; // Метод усреднения 
input uint Length1=3;   // Глубина усреднения 1
input double Factor1=0.1;
input uint Length2=5;   // Глубина усреднения 2
input double Factor2=0.1;
input uint Length3=7;   // Глубина усреднения 3
input double Factor3=0.1;
input uint Length4=9;   // Глубина усреднения 4 
input double Factor4=0.1;
input uint Length5=11;  // Глубина усреднения 5 
input double Factor5=0.1;
input uint Length6=13;  // Глубина усреднения 6
input double Factor6=0.1;
input uint Length7=15;  // Глубина усреднения 7 
input double Factor7=0.1;
input uint Length8=17;  // Глубина усреднения 8
input double Factor8=0.1;
input uint Length9=21;  // Глубина усреднения 9
input double Factor9=0.1;
input uint Length10=23; // Глубина усреднения 10 
input double Factor10=0.1;
input int XPhase=15; // Параметр усреднений
                     // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                     // для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method SmoothMethod=MODE_SMA; //Метод сглаживания 
input uint Smooth=3;      // Глубина сглаживания
input int SmoothPhase=15; // Параметр сглаживания
                          // для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса
                          // для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE; // Ценовая константа
input uint SignalBar=1; // Номер бара для получения сигнала входа
//+------------------------------------------------+
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
//---- получение хендла индикатора ColorZerolagX10MA
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorZerolagX10MA",
                         XMA_Method,Length1,Factor1,Length2,Factor2,Length3,Factor3,Length4,Factor4,
                         Length5,Factor5,Length6,Factor6,Length7,Factor7,Length8,Factor8,Length9,Factor9,
                         Length10,Factor10,XPhase,SmoothMethod,Smooth,SmoothPhase,IPC,0,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ColorZerolagX10MA");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   int min_rates[10];
   min_rates[0]=GetStartBars(XMA_Method,Length1,XPhase);
   min_rates[1]=GetStartBars(XMA_Method,Length2,XPhase);
   min_rates[2]=GetStartBars(XMA_Method,Length3,XPhase);
   min_rates[3]=GetStartBars(XMA_Method,Length4,XPhase);
   min_rates[4]=GetStartBars(XMA_Method,Length5,XPhase);
   min_rates[5]=GetStartBars(XMA_Method,Length6,XPhase);
   min_rates[6]=GetStartBars(XMA_Method,Length7,XPhase);
   min_rates[7]=GetStartBars(XMA_Method,Length8,XPhase);
   min_rates[8]=GetStartBars(XMA_Method,Length9,XPhase);
   min_rates[9]=GetStartBars(XMA_Method,Length10,XPhase);
   min_rates_total=min_rates[ArrayMaximum(min_rates)];
   min_rates_total+=GetStartBars(SmoothMethod,Smooth,SmoothPhase);
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
//---- объявление локальных переменных
   double Value[3];
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
      if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Value)<=0) {Recount=true; return;}
      //---- получим сигналы для покупки
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- получим сигналы для продажи
      if(Value[1]>Value[2])
        {
         if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
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
