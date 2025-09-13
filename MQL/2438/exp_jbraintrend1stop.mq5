//+------------------------------------------------------------------+
//|                                         Exp_JBrainTrend1Stop.mq5 |
//|                               Copyright © 2014, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2014, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+------------------------------------------------+
//| Торговые алгоритмы                             |
//+------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------+
//| Перечисление для вариантов расчёта лота        |
//+------------------------------------------------+
/*enum MarginMode  - перечисление объявлено в файле TradeAlgorithms.mqh
  {
   FREEMARGIN=0,     //MM от свободных средств на счёте
   BALANCE,          //MM от баланса средств на счёте
   LOSSFREEMARGIN,   //MM по убыткам от свободных средств на счёте
   LOSSBALANCE,      //MM по убыткам от баланса средств на счёте
   LOT               //Лот без изменения
  }; */
//+------------------------------------------------+
//| Входные параметры индикатора эксперта          |
//+------------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Stop Loss в пунктах
input int    TakeProfit_=2000;    // Take Profit в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в лонг
input bool   SellPosOpen=true;    // Разрешение для входа в шорт
input bool   BuyPosClose=true;    // Разрешение для выхода из лонгов
input bool   SellPosClose=true;   // Разрешение для выхода из шортов
//+------------------------------------------------+
//| Входные параметры индикатора JBrainTrend1Stop  |
//+------------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Таймфрейм индикатора
input int ATR_Period=7;                           // Период ATR 
input int STO_Period=9;                           // Период стохастика
input ENUM_MA_METHOD MA_Method=MODE_SMA;          // Метод усреднения
input ENUM_STO_PRICE STO_Price=STO_LOWHIGH;       // Метод расчёта цен 
input int Stop_dPeriod=3;                         // Приращение периода для стопа
input int Length_=7;                              // Глубина JMA сглаживания                   
input int Phase_=100;                             // Параметр JMA сглаживания
input uint SignalBar=1;                           // Номер бара для получения сигнала входа
//+------------------------------------------------+
int TimeShiftSec;
//--- объявление целочисленных переменных для хендлов индикаторов
int InpInd_Handle;
//--- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Торговые алгоритмы                                               |
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- получение хендла индикатора JBrainTrend1Stop
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"JBrainTrend1Stop",ATR_Period,STO_Period,MA_Method,STO_Price,Stop_dPeriod,Length_,Phase_);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора JBrainTrend1Stop");
      return(INIT_FAILED);
     }
//--- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- инициализация переменных начала отсчёта данных
   min_rates_total=int(MathMax(MathMax(MathMax(ATR_Period,STO_Period),ATR_Period+Stop_dPeriod),30)+2);
   min_rates_total+=int(3+SignalBar);
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
//--- объявление статических переменных
   int LastTrend;
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
      LastTrend=0;
      Recount=false;
      //--- поиск последнего направления торговли
      int Bars_=Bars(Symbol(),InpInd_Timeframe);
      if(Bars_<min_rates_total) {Recount=true; return;}
      Bars_-=min_rates_total+3;
      //--- объявление локальных переменных
      double DnTrend[2],UpTrend[2];
      //--- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,UpTrend)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,DnTrend)<=0) {Recount=true; return;}
      //--- получим сигналы для покупки
      if(UpTrend[1])
        {
         if(BuyPosOpen && DnTrend[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- получим сигналы для продажи
      if(DnTrend[1])
        {
         if(SellPosOpen && UpTrend[0]) SELL_Open=true;
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
