//+------------------------------------------------------------------+
//|                                 Exp_BalanceOfPower_Histogram.mq5 |
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------+
//  Торговые алгоритмы                       | 
//+------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------+
//|  Перечисление для вариантов расчёта лота |
//+------------------------------------------+
/*
enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счёте
   BALANCE,          //MM от баланса средств на счёте
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счёте
   LOSSBALANCE,      //MM по убыткам от баланса средств на счёте
   LOT               //Лот без изменения
  }; 
*/
//+------------------------------------------+
//|  Описание класса CXMA                    |
//+------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+------------------------------------------+
//|  объявление перечислений                 |
//+------------------------------------------+
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
//+------------------------------------------+
//|  объявление перечислений                 |
//+------------------------------------------+
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
//+------------------------------------------+
//| Входные параметры индикатора эксперта    |
//+------------------------------------------+
input double MM=0.1;              //Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      //способ определения размера лота
input int    StopLoss_=1000;      //стоплосс в пунктах
input int    TakeProfit_=2000;    //тейкпрофит в пунктах
input int    Deviation_=10;       //макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     //Разрешение для входа в лонг
input bool   SellPosOpen=true;    //Разрешение для входа в шорт
input bool   BuyPosClose=true;     //Разрешение для выхода из лонгов
input bool   SellPosClose=true;    //Разрешение для выхода из шортов
//+------------------------------------------+
//| Входные параметры индикатора JJRSX       |
//+------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //таймфрейм индикатора
//----
input Smooth_Method XMethod=MODE_T3;              // метод усреднения
input uint XLength=15;                            // глубина усреднения          
input int XPhase=15;                              // параметр усреднения,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
//----
input uint SignalBar=1;//номер бара для получения сигнала входа
//+------------------------------------------+
//---- Объявление целых переменных для хранения периода графика в секундах 
int TimeShiftSec;
//---- Объявление целых переменных для хендлов индикаторов
int InpInd_Handle;
//---- объявление целых переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- получение хендла индикатора BalanceOfPower_Histogram
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BalanceOfPower_Histogram",XMethod,XLength,XPhase,0,0,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора BalanceOfPower_Histogram");
      return(INIT_FAILED);
     }

//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Инициализация переменных начала отсчёта данных
   min_rates_total=GetStartBars(XMethod,XLength,XPhase)+1;
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
//---- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;

//---- подгрузка истории для нормальной работы функций IsNewBar() и SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);

//---- Объявление локальных переменных
   double Value[3];
//---- Объявление статических переменных
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;

//+------------------------------------------+
//| Определение сигналов для сделок          |
//+------------------------------------------+
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

      //---- Получим сигналы для покупки
      if(Value[1]<Value[2])
        {
         if(BuyPosOpen && Value[0]>Value[1]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Получим сигналы для продажи
      if(Value[1]>Value[2])
        {
         if(SellPosOpen && Value[0]<Value[1]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }

//+------------------------------------------+
//| Совершение сделок                        |
//+------------------------------------------+
//---- Закрываем лонг
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Закрываем шорт   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
   //Print(LOT);
//---- Открываем лонг
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);

//---- Открываем шорт
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
