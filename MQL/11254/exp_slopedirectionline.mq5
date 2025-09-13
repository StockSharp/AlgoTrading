//+------------------------------------------------------------------+
//|                                       Exp_SlopeDirectionLine.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-------------------------------------------------+
//| Торговые алгоритмы                              |
//+-------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+-------------------------------------------------+
//| Перечисление для вариантов расчёта лота         |
//+-------------------------------------------------+
/*enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счёте
   BALANCE,          //MM от баланса средств на счёте
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счёте
   LOSSBALANCE,      //MM по убыткам от баланса средств на счёте
   LOT               //Лот без изменения
  }; */
//+-------------------------------------------------+
//| Описание класса CXMA                            |
//+-------------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+-------------------------------------------------+
//| объявление перечислений                         |
//+-------------------------------------------------+
enum Applied_price_ //Тип константы
  {
   PRICE_CLOSE_ = 1,     //Close
   PRICE_OPEN_,          //Open
   PRICE_HIGH_,          //High
   PRICE_LOW_,           //Low
   PRICE_MEDIAN_,        //Median Price (HL/2)
   PRICE_TYPICAL_,       //Typical Price (HLC/3)
   PRICE_WEIGHTED_,      //Weighted Close (HLCC/4)
   PRICE_SIMPL_,         //Simple Price (OC/2)
   PRICE_QUARTER_,       //Quarted Price (HLOC/4) 
   PRICE_TRENDFOLLOW0_,  //TrendFollow_1 Price 
   PRICE_TRENDFOLLOW1_,  //TrendFollow_2 Price 
   PRICE_DEMARK_         //Demark Price 
  };
//+-------------------------------------------------+
//| объявление перечислений                         |
//+-------------------------------------------------+
/*
enum Smooth_Method
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
  };
*/
//+-------------------------------------------------+
//| Входные параметры индикатора эксперта           |
//+-------------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Cпособ определения размера лота
input int    StopLoss_=1000;      // Stop Loss в пунктах
input int    TakeProfit_=2000;    // Take Profit в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в лонг
input bool   SellPosOpen=true;    // Разрешение для входа в шорт
input bool   BuyPosClose=true;    // Разрешение для выхода из лонгов
input bool   SellPosClose=true;   // Разрешение для выхода из шортов
//+-------------------------------------------------+
//| Входные параметры индикатора SlopeDirectionLine |
//+-------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4;     // Таймфрейм индикатора
input Smooth_Method MA_Method1=MODE_LWMA;             // Метод первого усреднения
input uint Length1=12;                                // Глубина первого усреднения
input int Phase1=15;                                  // Параметр первого усреднения
//--- Phase1: для JJMA изменяется в пределах -100..+100, влияет на качество переходного процесса;
//--- Phase1: для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method MA_Method2=MODE_SMA;              // Метод усреднения второго сглаживания 
input int Phase2=15;                                  // Параметр второго сглаживания
//--- Phase2: для JJMA изменяется в пределах -100..+100, влияет на качество переходного процесса;
//--- Phase2: для VIDIA это период CMO, для AMA это период медленной скользящей
input Applied_price_ IPC=PRICE_CLOSE;                 // Ценовая константа
input bool On_Push = false;                           // Разрешение на передачу push-сообщений
input bool On_Email = false;                          // Разрешение на отправку почты
input bool On_Alert = true;                           // Разрешение на подачу алерта
input bool On_Play_Sound = false;                     // Разрешение на подачу звукового сигнала
input string NameFileSound = "expert.wav";            // Имя для файла звукового сигнала
input string  CommentSirName="SlopeDirectionLine: ";  // Первая часть алерт-коммента
input uint SignalBar=1;                               // Номер бара для сигнала
//+-------------------------------------------------+
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
//--- получение хендла индикатора SlopeDirectionLine
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"SlopeDirectionLine",MA_Method1,Length1,Phase1,MA_Method2,Phase2,IPC,0,0,false,false,false,false,"","",0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора SlopeDirectionLine");
      return(INIT_FAILED);
     }
//--- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- инициализация переменных начала отсчёта данных
   int LengthR=int(MathMax(MathSqrt(Length1),1));
   min_rates_total+=GetStartBars(MA_Method1,Length1,Phase1);
   min_rates_total+=GetStartBars(MA_Method2,LengthR,Phase2);
   min_rates_total+=int(2+SignalBar);
//---
   return(INIT_SUCCEEDED);
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
   double Value[2];
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
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Value)<=0) {Recount=true; return;}
      //--- получим сигналы для покупки
      if(Value[1]==2)
        {
         if(BuyPosOpen && Value[0]!=2) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- получим сигналы для продажи
      if(Value[1]==0)
        {
         if(SellPosOpen&&Value[0]!=0) SELL_Open=true;
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
