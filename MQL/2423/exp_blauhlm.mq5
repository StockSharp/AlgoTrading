//+------------------------------------------------------------------+
//|                                                  Exp_BlauHLM.mq5 |
//|                               Copyright © 2014, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2014, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//| Описание класса CXMA                         |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
enum AlgMode
  {
   breakdown,  // Пробой нуля гистограммой
   twist,      // Изменение направления гистограммы
   cloudtwist  // Изменение цвета сигнального облака
  };
//+----------------------------------------------+
//| объявление перечислений                      |
//+----------------------------------------------+
/*
enum Smooth_Method
  {
   MODE_SMA_,  // SMA
   MODE_EMA_,  // EMA
   MODE_SMMA_, // SMMA
   MODE_LWMA_, // LWMA
   MODE_JJMA,  // JJMA
   MODE_JurX,  // JurX
   MODE_ParMA, // ParMA
   MODE_T3,    // T3
   MODE_VIDYA, // VIDYA
   MODE_AMA,   // AMA
  };
*/
//+----------------------------------------------+
//| Торговые алгоритмы                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| Входные параметры эксперта                   |
//+----------------------------------------------+
input double MM=0.1;               // Доля финансовых ресурсов от депозита в сделке, отрицательные значения - размер лота
input MarginMode MMMode=LOT;       // Способ определения размера лота
input int     StopLoss_=1000;      // Stop Loss в пунктах
input int     TakeProfit_=2000;    // Take Profit в пунктах
input int     Deviation_=10;       // Макс. отклонение цены в пунктах
input bool    BuyPosOpen=true;     // Разрешение для входа в лонг
input bool    SellPosOpen=true;    // Разрешение для входа в шорт
input bool    BuyPosClose=true;    // Разрешение для выхода из лонгов
input bool    SellPosClose=true;   // Разрешение для выхода из шортов
input AlgMode Mode=twist;          // Алгоритм для входа в рынок
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; // Таймфрейм индикатора
input Smooth_Method XMA_Method=MODE_EMA;          // Метод усреднения
input uint XLength=2;                             // Период моментума
input uint XLength1=20;                           // Глубина первого усреднения
input uint XLength2=5;                            // Глубина второго усреднения
input uint XLength3=3;                            // Глубина третьего усреднения
input uint XLength4=3;                            // Глубина усреднения сигнальной линии
input int XPhase=15;                              // Параметр сглаживания
//---- для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
//---- Для VIDIA это период CMO, для AMA это период медленной скользящей
input uint SignalBar=1;                           // Номер бара для получения сигнала входа
//+----------------------------------------------+
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
//--- получение хендла индикатора BlauHLM
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"BlauHLM",XMA_Method,XLength,XLength1,XLength2,XLength3,XLength4,XPhase);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора BlauHLM");
      return(INIT_FAILED);
     }
//--- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- инициализация переменных начала отсчета данных
   min_rates_total=int(XLength);
   min_rates_total+=GetStartBars(XMA_Method,XLength1,XPhase);
   min_rates_total+=GetStartBars(XMA_Method,XLength2,XPhase);
   min_rates_total+=GetStartBars(XMA_Method,XLength3,XPhase);
   min_rates_total+=GetStartBars(XMA_Method,XLength4,XPhase);
   min_rates_total=int(min_rates_total+3+SignalBar);
//--- завершение инициализации
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
//--- проверка количества баров на достаточность для расчета
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//--- подгрузка истории для нормальной работы функций IsNewBar() и SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
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
      //---
      switch(Mode)
        {
         case breakdown: //пробой нуля гистограммой
           {
            double Hist[2];
            //--- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Hist)<=0) {Recount=true; return;}
            //--- получим сигналы для покупки
            if(Hist[1]>0)
              {
               if(BuyPosOpen  &&  Hist[0]<=0) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //--- получим сигналы для продажи
            if(Hist[1]<0)
              {
               if(SellPosOpen && Hist[0]>=0) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case twist://изменение направления
           {
            double Hist[3];
            //--- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,2,SignalBar,3,Hist)<=0) {Recount=true; return;}
            //--- получим сигналы для покупки
            if(Hist[1]<Hist[2])
              {
               if(BuyPosOpen && Hist[0]>Hist[1]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //--- получим сигналы для продажи
            if(Hist[1]>Hist[2])
              {
               if(SellPosOpen && Hist[0]<Hist[1]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case cloudtwist: //изменение цвета сигнального облака
           {
            double Up[2],Dn[2];
            //--- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Up)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,1,SignalBar,2,Dn)<=0) {Recount=true; return;}
            //--- получим сигналы для покупки
            if(Up[1]>Dn[1])
              {
               if(BuyPosOpen   &&   Up[0]<=Dn[0]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
            //--- получим сигналы для продажи
            if(Up[1]<Dn[1])
              {
               if(SellPosOpen && Up[0]>=Dn[0]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;
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
