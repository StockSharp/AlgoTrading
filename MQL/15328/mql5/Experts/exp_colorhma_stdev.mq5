//+------------------------------------------------------------------+
//|                                           Exp_ColorHMA_StDev.mq5 |
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                                Khabarovsk, farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Торговые алгоритмы                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//|  Перечисление для вариантов расчета лота     |
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
//|  Перечисление для вариантов выхода и входа   |
//+----------------------------------------------+
enum SignalMode
  {
   POINT=0,          //при появлении точечных сигналов (любая точка - сигнал)
   DIRECT,           //при изменении направления движения индикатора
   WITHOUT           //нет разрешения
  };
//+----------------------------------------------+
//| Входные параметры эксперта                   |
//+----------------------------------------------+
input double MM=0.1;                  // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;          // Способ определения размера лота
input int    StopLoss_=1000;          // Стоплосс в пунктах
input int    TakeProfit_=2000;        // Тейкпрофит в пунктах
input int    Deviation_=10;           // Макс. отклонение цены в пунктах
input SignalMode BuyPosOpen=POINT;    // Разрешение для входа в длинные позиции
input SignalMode SellPosOpen=POINT;   // Разрешение для входа в короткие позиции
input SignalMode BuyPosClose=DIRECT;  // Разрешение для выхода из длинных позиций
input SignalMode SellPosClose=DIRECT; // Разрешение для выхода из коротких позиций
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //таймфрейм индикатора
input uint HMA_Period=13;                         //Период мувинга
input double dK1=1.5;                             //коэффициент 1 для квадратичного фильтра
input double dK2=2.5;                             //коэффициент 2 для квадратичного фильтра
input uint std_period=9;                          //период квадратичного фильтра
input ENUM_APPLIED_PRICE IPC=PRICE_CLOSE;         //Ценовая константа
input uint SignalBar=1;                           //номер бара для получения сигнала входа
//+----------------------------------------------+
//---- Объявление целочисленных переменных для хранения периода графика в секундах 
int TimeShiftSec;
//---- Объявление целочисленных переменных для хендлов индикаторов
int InpInd_Handle;
//---- объявление целочисленных переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- получение хендла индикатора ColorHMA_StDev
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorHMA_StDev",HMA_Period,dK1,dK2,std_period,0,0,IPC);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ColorHMA_StDev");
      return(INIT_FAILED);
     }
//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//---- Инициализация переменных начала отсчета данных
   min_rates_total=int(MathFloor(MathSqrt(HMA_Period)))+int(HMA_Period)+int(std_period);
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
//---- проверка количества баров на достаточность для расчета
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
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      //----
      switch(BuyPosOpen)
        {
         case POINT:
           {
            //---- Объявление локальных переменных
            double Sign1[1],Sign2[1];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,3,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,5,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) BUY_Open=true;
            break;
           }
         case DIRECT:
           {
            //---- Объявление локальных переменных
            double Line[3];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Line)<=0) {Recount=true; return;}
            if(Line[0]>Line[1] &&  Line[1]<Line[2]) BUY_Open=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
      //----
      switch(SellPosOpen)
        {
         case POINT:
           {
            //---- Объявление локальных переменных
            double Sign1[1],Sign2[1];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,2,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,4,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) SELL_Open=true;
            break;
           }
         case DIRECT:
           {
            //---- Объявление локальных переменных
            double Line[3];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,3,Line)<=0) {Recount=true; return;}
            if(Line[0]<Line[1] && Line[1]>Line[2]) SELL_Open=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
      //----
      switch(BuyPosClose)
        {
         case POINT:
           {
            //---- Объявление локальных переменных
            double Sign1[1],Sign2[1];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,2,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,4,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) BUY_Close=true;
            break;
           }
         case DIRECT:
           {
            //---- Объявление локальных переменных
            double Line[2];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Line)<=0) {Recount=true; return;}
            if(Line[0]>Line[1]) BUY_Close=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
      //----
      switch(SellPosClose)
        {
         case POINT:
           {
            //---- Объявление локальных переменных
            double Sign1[1],Sign2[1];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,3,SignalBar,1,Sign1)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,5,SignalBar,1,Sign2)<=0) {Recount=true; return;}
            if((Sign1[0]!=EMPTY_VALUE) || (Sign2[0]!=EMPTY_VALUE)) SELL_Close=true;
            break;
           }
         case DIRECT:
           {
            //---- Объявление локальных переменных
            double Line[2];
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Line)<=0) {Recount=true; return;}
            if(Line[0]<Line[1]) SELL_Close=true;
            break;
           }
         case WITHOUT:
           {
            break;
           }
        }
     }
//+----------------------------------------------+
//| Совершение сделок                            |
//+----------------------------------------------+
//---- Закрываем длинную позицию
   BuyPositionClose(BUY_Close,Symbol(),Deviation_);
//---- Закрываем короткую позицию
   SellPositionClose(SELL_Close,Symbol(),Deviation_);
//---- Открываем длинную позицию
   BuyPositionOpen(BUY_Open,Symbol(),UpSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//---- Открываем короткую позицию
   SellPositionOpen(SELL_Open,Symbol(),DnSignalTime,MM,MMMode,Deviation_,StopLoss_,TakeProfit_);
//----
  }
//+------------------------------------------------------------------+
