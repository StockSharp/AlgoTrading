//+------------------------------------------------------------------+
//|                                                   Exp_KPrmSt.mq5 |
//|                             Copyright © 2012,   Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2012, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+-----------------------------------------------+
//| Торговые алгоритмы                            |
//+-----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+-----------------------------------------------+
//| Входные параметры индикатора эксперта         |
//+-----------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Стоплосс в пунктах
input int    TakeProfit_=2000;    // Тейкпрофит в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в длинные позиции
input bool   SellPosOpen=true;    // Разрешение для входа в короткие позиции
input bool   BuyPosClose=true;    // Разрешение для выхода из длинных позиций
input bool   SellPosClose=true;   // Разрешение для выхода из коротких позиций
//+-----------------------------------------------+
//| Входные параметры для индикатора KPrmSt       |
//+-----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H6; // Таймфрейм индикатора
input uint SignalBar=1; // Номер бара для получения сигнала входа
input uint  Per1=14;
input uint  Per2=3;
input uint  Per3=3;
input uint  Per4=5;
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
//---- получение хендла индикатора KPrmSt
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"KPrmSt",Per1,Per2,Per3,Per4,0);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print("Не удалось получить хендл индикатора KPrmSt");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- инициализация переменных начала отсчета данных
   min_rates_total=int(MathMax(Per1,Per4))+2;
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
   double Ma1[2],Ma2[2];
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
      if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Ma1)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Ma2)<=0) {Recount=true; return;}
      //---- получим сигналы для покупки
      if(Ma1[1]>Ma2[1])
        {
         if(BuyPosOpen && Ma1[0]<=Ma2[0]) BUY_Open=true;
         if(SellPosClose) SELL_Close=true; 
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //---- получим сигналы для продажи
      if(Ma1[1]<Ma2[1])
        {
         if(SellPosOpen && Ma1[0]>=Ma2[0]) SELL_Open=true;
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
