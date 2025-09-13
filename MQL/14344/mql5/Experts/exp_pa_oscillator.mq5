//+------------------------------------------------------------------+
//|                                            Exp_PA_Oscillator.mq5 |
//|                               Copyright © 2015, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2015, Nikolay Kositsin"
#property link "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//| Объявление перечислений                      |
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
//| Объявление перечислений                      |
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
//+----------------------------------------------+
//| Входные параметры индикатора PA_Oscillator   |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H8; //таймфрейм индикатора
input Smooth_Method MA_Method1=MODE_EMA; // Метод усреднения быстрой скользящей средней
input uint Length1=12; // Глубина усреднения быстрой скользящей средней
input int Phase1=15;   // Параметр усреднения быстрой скользящей средней
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_EMA; // Метод усреднения медленной скользящей средней
input uint Length2=26; // Глубина усреднения медленной скользящей средней
input int Phase2=15;   // Параметр усреднения медленной скользящей средней
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;// Ценовая константа
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
//---- получение хендла индикатора PA_Oscillator
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"PA_Oscillator",MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,IPC,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора PA_Oscillator");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   int min_rates_1=GetStartBars(MA_Method1,Length1,Phase1);
   int min_rates_2=GetStartBars(MA_Method2,Length2,Phase2);
   min_rates_total=MathMax(min_rates_1,min_rates_2)+1; 
   min_rates_total+=int(2+SignalBar);
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
   double Col[2];
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
      //---- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Col)<=0) {Recount=true; return;}
      //---- получим сигналы для покупки
      if(Col[1]==0)
        {
         if(BuyPosOpen && Col[0]>0) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- получим сигналы для продажи
      if(Col[1]==2)
        {
         if(SellPosOpen && Col[0]<2) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
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
