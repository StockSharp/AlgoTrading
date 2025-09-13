//+------------------------------------------------------------------+
//|                                             Exp_KaufWMAcross.mq5 |
//|                               Copyright © 2014, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2014, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.10"
//+----------------------------------------------+
//| Торговые алгоритмы                           |
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//| Перечисление для вариантов расчёта лота      |
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
//| Входные параметры индикатора эксперта        |
//+----------------------------------------------+
input double MM=0.1;              // Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      // Способ определения размера лота
input int    StopLoss_=1000;      // Stop Loss в пунктах
input int    TakeProfit_=2000;    // Take Profit в пунктах
input int    Deviation_=10;       // Макс. отклонение цены в пунктах
input bool   BuyPosOpen=true;     // Разрешение для входа в лонг
input bool   SellPosOpen=true;    // Разрешение для входа в шорт
input bool   BuyPosClose=true;    // Разрешение для выхода из лонгов
input bool   SellPosClose=true;   // Разрешение для выхода из шортов
//+----------------------------------------------+
//| Входные параметры индикатора KaufWMAcross    |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H6; //таймфрейм индикатора KaufWMAcross
//--- параметры AMA
input uint ama_period=9;                            // Период AMA
input uint fast_ma_period=2;                        // Период быстрой скользящей
input uint slow_ma_period=30;                       // Период медленной скользящей
input ENUM_APPLIED_PRICE  AMAPrice=PRICE_CLOSE;     // Цена AMA
//--- параметры скользящего среднего
input uint  MAPeriod=13;                            // Период MA
input  ENUM_MA_METHOD   MAType=MODE_LWMA;           // Метод усреднения MA
input ENUM_APPLIED_PRICE   MAPrice=PRICE_CLOSE;     // Цена MA
input uint SignalBar=1;                             // Номер бара для получения сигнала входа
//+----------------------------------------------+
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
//--- получение хендла индикатора KaufWMAcross
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"KaufWMAcross",ama_period,fast_ma_period,slow_ma_period,AMAPrice,MAPeriod,MAType,MAPrice);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора KaufWMAcross");
      return(INIT_FAILED);
     }
//--- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);
//--- инициализация переменных начала отсчёта данных
   int ATR_Period=15;
   min_rates_total=int(MathMax(MathMax(ATR_Period,ama_period+1),MAPeriod+1));
   min_rates_total+=int(3+SignalBar);
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
//--- проверка количества баров на достаточность для расчёта
   if(BarsCalculated(InpInd_Handle)<min_rates_total) return;
//--- подгрузка истории для нормальной работы функций IsNewBar() и SeriesInfoInteger()  
   LoadHistory(TimeCurrent()-PeriodSeconds(InpInd_Timeframe)-1,Symbol(),InpInd_Timeframe);
//--- объявление локальных переменных
   double DnValue[1],UpValue[1];
//--- объявление статических переменных
   static bool Recount=true;
   static bool BUY_Open=false,BUY_Close=false;
   static bool SELL_Open=false,SELL_Close=false;
   static datetime UpSignalTime,DnSignalTime;
   static CIsNewBar NB;
//--- Определение сигналов для сделок
   if(!SignalBar || NB.IsNewBar(Symbol(),InpInd_Timeframe) || Recount) // проверка на появление нового бара
     {
      //--- обнулим торговые сигналы
      BUY_Open=false;
      SELL_Open=false;
      BUY_Close=false;
      SELL_Close=false;
      Recount=false;
      //--- копируем вновь появившиеся данные в массивы
      if(CopyBuffer(InpInd_Handle,1,SignalBar,1,UpValue)<=0) {Recount=true; return;}
      if(CopyBuffer(InpInd_Handle,0,SignalBar,1,DnValue)<=0) {Recount=true; return;}
      //--- получим сигналы для покупки
      if(UpValue[0] && UpValue[0]!=EMPTY_VALUE)
        {
         if(BuyPosOpen) BUY_Open=true;
         if(SellPosClose)SELL_Close=true;
         UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- получим сигналы для продажи
      if(DnValue[0] && DnValue[0]!=EMPTY_VALUE)
        {
         if(SellPosOpen) SELL_Open=true;
         if(BuyPosClose) BUY_Close=true;
         DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
        }
      //--- поиск последнего направления торговли для получения сигналов для закрывания позиций
      //if(!MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)) //если режим торговли в тестере "Произвольная задержка" 
      if(((BuyPosOpen && BuyPosClose) || (SellPosOpen && SellPosClose)) && (!BUY_Close && !SELL_Close))
        {
         int Bars_=Bars(Symbol(),InpInd_Timeframe);

         for(int bar=int(SignalBar+1); bar<Bars_; bar++)
           {
            if(SellPosClose)
              {
               if(CopyBuffer(InpInd_Handle,1,bar,1,UpValue)<=0) {Recount=true; return;}
               if(UpValue[0]!=0 && UpValue[0]!=EMPTY_VALUE)
                 {
                  SELL_Close=true;
                  break;
                 }
              }

            if(BuyPosClose)
              {
               if(CopyBuffer(InpInd_Handle,0,bar,1,DnValue)<=0) {Recount=true; return;}
               if(DnValue[0]!=0 && DnValue[0]!=EMPTY_VALUE)
                 {
                  BUY_Close=true;
                  break;
                 }
              }
           }
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
