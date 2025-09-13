//+---------------------------------------------------------------------+
//|                                           Exp_Directed_Movement.mq5 |
//|                                  Copyright © 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------------------+
//  Торговые алгоритмы                            | 
//+-----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//|  Перечисление для вариантов расчёта лота     |
//+----------------------------------------------+
/*enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счёте
   BALANCE,          //MM от баланса средств на счёте
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счёте
   LOSSBALANCE,      //MM по убыткам от баланса средств на счёте
   LOT               //Лот без изменения
  }; */
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  объявление перечислений                     |
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
//+-----------------------------------------------+
//| Входные параметры индикатора эксперта         |
//+-----------------------------------------------+
input double     MM=0.1;                //Доля финансовых ресурсов в сделке
input MarginMode MMMode=LOT;            //способ определения размера лота
input uint       StopLoss_=1000;        //стоплосс в пунктах
input uint       TakeProfit_=2000;      //тейкпрофит в пунктах
input uint       Deviation_=10;         //макс. отклонение цены в пунктах
input bool       BuyPosOpen=true;       //Разрешение для входа в лонг
input bool       SellPosOpen=true;      //Разрешение для входа в шорт
input bool       BuyPosClose=true;      //Разрешение для выхода из лонгов по сигналам индикаторов
input bool       SellPosClose=true;     //Разрешение для выхода из шортов по сигналам индикаторов
//+-----------------------------------------------+
//| Входные параметры для индикатора              |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4;  // таймфрейм индикатора
//----
input uint                 RSIPeriod=14;           // период индикатора
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;   // цена
input Smooth_Method MA_Method1=MODE_SMA_;          // метод усреднения первого сглаживания 
input int Length1=12;                              // глубина  первого сглаживания                    
input int Phase1=15;                               // параметр первого сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_JJMA;          // метод усреднения второго сглаживания 
input int Length2 = 5;                             // глубина  второго сглаживания 
input int Phase2=15;                               // параметр второго сглаживания,
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
//----
input uint SignalBar=1;                            // номер бара для получения сигнала входа
//+-----------------------------------------------+
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
//---- получение хендла индикатора Directed_Movement
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"Directed_Movement",RSIPeriod,RSIPrice,MA_Method1,Length1,Phase1,MA_Method2,Length2,Phase2,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("Не удалось получить хендл индикатора Directed_Movement");
      return(INIT_FAILED);
     }

//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(RSIPeriod);
   min_rates_total+=GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_total+=GetStartBars(MA_Method2,Length2,Phase2);
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
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;

      //---- Объявление локальных переменных
      double UpIndSeries[2],DnIndSeries[2];

      //---- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,UpIndSeries)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,DnIndSeries)<=0) {Recount=true; return;}

      //---- Получим сигналы для покупки с индикатора
      if(UpIndSeries[1]>DnIndSeries[1])
        {
         if(BuyPosOpen && UpIndSeries[0]<=DnIndSeries[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }

      //---- Получим сигналы для продажи с индикатора
      if(UpIndSeries[1]<DnIndSeries[1])
        {
         if(SellPosOpen && UpIndSeries[0]>=DnIndSeries[0]) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
     }
//+----------------------------------------------+
//| Совершение сделок                            |
//+----------------------------------------------+
//---- Закрываем лонг
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);

//---- Закрываем шорт   
   SellPositionClose(SELL_Close,Symbol(),Deviation_);

//---- Открываем лонг
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);

//---- Открываем шорт
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
