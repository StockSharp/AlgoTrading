//+------------------------------------------------------------------+
//|                                             Exp_ColorRsiMACD.mq5 |
//|                               Copyright © 2016, Nikolay Kositsin | 
//|                              Khabarovsk,   farria@mail.redcom.ru | 
//+------------------------------------------------------------------+
#property copyright "Copyright © 2016, Nikolay Kositsin"
#property link      "farria@mail.redcom.ru"
#property version   "1.00"
//+----------------------------------------------+
//|  Торговые алгоритмы                          | 
//+----------------------------------------------+
#include <TradeAlgorithms.mqh>
//+----------------------------------------------+
//|  Описание класса CXMA                        |
//+----------------------------------------------+
#include <SmoothAlgorithms.mqh> 
//+----------------------------------------------+
//|  объявление перечислений                     |
//+----------------------------------------------+
enum AlgMode
  {
   breakdown,  //пробой нуля
   MACDtwist,  //изменение направления MACD
   SIGNALtwist,//изменение направления сигнальной линии
   MACDdisposition //пробой гистограммой MACD сигнальной линии
  };
//+----------------------------------------------+
//|  объявление перечислений                     |
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
//|  объявление перечислений                     |
//+----------------------------------------------+
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
//+----------------------------------------------+
//| Входные параметры эксперта                   |
//+----------------------------------------------+
input double  MM=0.1;             //Доля финансовых ресурсов от депозита в сделке
input MarginMode MMMode=LOT;      //Способ определения размера лота
input int     StopLoss_=1000;      //стоплосс в пунктах
input int     TakeProfit_=2000;    //тейкпрофит в пунктах
input int     Deviation_=10;       //макс. отклонение цены в пунктах
input bool    BuyPosOpen=true;     //Разрешение для входа в лонг
input bool    SellPosOpen=true;    //Разрешение для входа в шорт
input bool    BuyPosClose=true;    //Разрешение для выхода из лонгов
input bool    SellPosClose=true;   //Разрешение для выхода из шортов
input AlgMode Mode=MACDdisposition;//алгоритм для входа в рынок
//+----------------------------------------------+
//| Входные параметры индикатора                 |
//+----------------------------------------------+
input ENUM_TIMEFRAMES InpInd_Timeframe=PERIOD_H4; //таймфрейм индикатора
input uint    RSIPeriod=14;
input ENUM_APPLIED_PRICE   RSIPrice=PRICE_CLOSE;
input Smooth_Method XMA_Method=MODE_T3; //метод усреднения гистограммы
input uint Fast_XMA = 12; //период быстрого мувинга
input uint Slow_XMA = 26; //период медленного мувинга
input int XPhase=100;  //параметр усреднения мувингов,
                       //для JJMA изменяющийся в пределах -100 ... +100, влияет на качество переходного процесса;
// Для VIDIA это период CMO, для AMA это период медленной скользящей
input Smooth_Method Signal_Method=MODE_JJMA; //метод усреднения сигнальной линии
input int Signal_XMA=9; //период сигнальной линии 
input int Signal_Phase=100; // параметр сигнальной линии,
                            //изменяющийся в пределах -100 ... +100,
//влияет на качество переходного процесса;
input Applied_price_ AppliedPrice=PRICE_CLOSE_;//ценовая константа
input uint SignalBar=1;//номер бара для получения сигнала входа
//+----------------------------------------------+
//---- Объявление целых переменных для хранения периода графика в секундах 
int TimeShiftSec;
//---- Объявление целых переменных для хендлов индикаторов
int InpInd_Handle;
//---- объявление целых переменных начала отсчета данных
int min_rates_total;
//+------------------------------------------------------------------+
//  Торговые алгоритмы                                               | 
//+------------------------------------------------------------------+
#include <TradeAlgorithms.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---- получение хендла индикатора ColorRsiMACD
   InpInd_Handle=iCustom(Symbol(),InpInd_Timeframe,"ColorRsiMACD",RSIPeriod,RSIPrice,
                         XMA_Method,Fast_XMA,Slow_XMA,XPhase,Signal_Method,Signal_XMA,Signal_Phase,AppliedPrice);
   if(InpInd_Handle==INVALID_HANDLE)
     {
      Print(" Не удалось получить хендл индикатора ColorRsiMACD");
      return(INIT_FAILED);
     }

//---- инициализация переменной для хранения периода графика в секундах  
   TimeShiftSec=PeriodSeconds(InpInd_Timeframe);

//---- Инициализация переменных начала отсчёта данных
   min_rates_total+=int(RSIPeriod);
   min_rates_total+=GetStartBars(XMA_Method,Fast_XMA,XPhase);
   min_rates_total+=GetStartBars(XMA_Method,Slow_XMA,XPhase);
   min_rates_total+=GetStartBars(Signal_Method,Signal_XMA,Signal_Phase)+2;
   min_rates_total=int(min_rates_total+3+SignalBar);
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

      switch(Mode)
        {
         case breakdown:
           {
            double Value[2];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,Value)<=0) {Recount=true; return;}

            //---- Получим сигналы для покупки
            if(Value[1]>0)
              {
               if(BuyPosOpen && Value[0]<=0) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Получим сигналы для продажи
            if(Value[1]<0)
              {
               if(SellPosOpen && Value[0]>=0) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;

         case MACDtwist:
           {
            double Value[3];
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
         break;

         case SIGNALtwist:
           {
            double Value[3];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,2,SignalBar,3,Value)<=0) {Recount=true; return;}

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
         break;

         case MACDdisposition:
           {
            double MACD[2],Signal[2];
            //---- копируем вновь появившиеся данные в массивы
            if(CopyBuffer(InpInd_Handle,0,SignalBar,2,MACD)<=0) {Recount=true; return;}
            if(CopyBuffer(InpInd_Handle,2,SignalBar,2,Signal)<=0) {Recount=true; return;}

            //---- Получим сигналы для покупки
            if(MACD[1]>Signal[1])
              {
               if(BuyPosOpen  &&  MACD[0]<=Signal[0]) BUY_Open=true;
               if(SellPosClose) SELL_Close=true;
               UpSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }

            //---- Получим сигналы для продажи
            if(MACD[1]<Signal[1])
              {
               if(SellPosOpen && MACD[0]>=Signal[0]) SELL_Open=true;
               if(BuyPosClose) BUY_Close=true;
               DnSignalTime=datetime(SeriesInfoInteger(Symbol(),InpInd_Timeframe,SERIES_LASTBAR_DATE))+TimeShiftSec;
              }
           }
         break;
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
