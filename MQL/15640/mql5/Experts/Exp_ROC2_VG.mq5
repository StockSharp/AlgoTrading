//+---------------------------------------------------------------------+
//|                                                     Exp_ROC2_VG.mq5 |
//|                                  Copyright © 2016, Nikolay Kositsin | 
//|                                 Khabarovsk,   farria@mail.redcom.ru | 
//+---------------------------------------------------------------------+
//| Для работы  индикатора  следует  положить файл SmoothAlgorithms.mqh |
//| в папку (директорию): каталог_данных_терминала\\MQL5\Include        |
//+---------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//  Торговые алгоритмы                           | 
//+----------------------------------------------+
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
input bool       Invert=false;          //Торговля против тренда
//+-----------------------------------------------+
//|  объявление перечислений                      |
//+-----------------------------------------------+
enum ENUM_TYPE
  {
   MOM=1,  //MOM
   ROC,    //ROC
   ROCP,   //ROCP
   ROCR,   //ROC
   ROCR100 //ROCR100
  };
//+-----------------------------------------------+
//| Входные параметры для индикатора              |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4;     // таймфрейм индикатора
//----
input uint ROCPeriod1=8;
input ENUM_TYPE ROCType1=MOM;
input uint ROCPeriod2=14;
input ENUM_TYPE ROCType2=MOM;
//----
input uint SignalBar=1;                               // номер бара для получения сигнала входа
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
//---- получение хендла индикатора ROC2_VG
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ROC2_VG",ROCPeriod1,ROCType1,ROCPeriod2,ROCType2,0,0,0,0,0,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("Не удалось получить хендл индикатора ROC2_VG");
      return(INIT_FAILED);
     }

//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(ROCPeriod1,ROCPeriod2));
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
      if(!Invert)
        {
         if(CopyBuffer(InpInd_Handle,0,SignalBar,2,UpIndSeries)<=0) {Recount=true; return;}
         if(CopyBuffer(InpInd_Handle,1,SignalBar,2,DnIndSeries)<=0) {Recount=true; return;}
        }
      else
        {
         if(CopyBuffer(InpInd_Handle,1,SignalBar,2,UpIndSeries)<=0) {Recount=true; return;}
         if(CopyBuffer(InpInd_Handle,0,SignalBar,2,DnIndSeries)<=0) {Recount=true; return;}
        }

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
