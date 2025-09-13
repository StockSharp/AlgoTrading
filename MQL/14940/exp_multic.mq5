//+------------------------------------------------------------------+
//|                                                   exp_multic.mq5 |
//|                                      Copyright 2012-2016, DC2008 |
//|                              http://www.mql5.com/ru/users/dc2008 |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012-2016, DC2008"
#property link      "http://www.mql5.com/ru/users/dc2008"
#property version   "1.00"
#property description   "Expert implements the strategy of multi-currency trading"
#property description   "without using technical analysis indicators"
//---
#include <Trade\Trade.mqh>
//---
#define EURUSD      "EURUSD"
#define GBPUSD      "GBPUSD"
#define USDJPY      "USDJPY"
#define USDCHF      "USDCHF"
#define USDCAD      "USDCAD"
#define AUDUSD      "AUDUSD"
#define EURGBP      "EURGBP"
#define EURJPY      "EURJPY"
#define EURAUD      "EURAUD"
#define GBPJPY      "GBPJPY"
//---
MqlTick last_tick;
CTrade  trade;
//---
input int      loss=1900;   // убыток в валюте депозита
input int      profit=4000; // прибыль в валюте депозита
input int      margin=5000; // минимальный размер депозита, при котором возможна торговля
input double   minLot=0.01; // минимальный объём позиции
input int      k_change=2100; // коэффициент для расчёта убытка при изменении позиции
input int      k_closse=4600; // коэффициент для расчёта прибыли при закрытии позиции
//---
string  valuta[10]=
  {
   EURUSD,
   GBPUSD,
   USDJPY,
   USDCHF,
   USDCAD,
   AUDUSD,
   EURGBP,
   EURJPY,
   EURAUD,
   GBPJPY
  };
int      N;          // количество валютных пар
bool     Type[10];   // направление торговли для каждой валютной пары: BUY или SELL
double   Lot[10];    // размер лота для каждой валютной пары
string   main_comment;  // строка комментария
bool     on_trade;      // разрешаем или запрещаем торговлю
double   realEquity;    // стартовый баланс
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   Comment("");
   realEquity=AccountInfoDouble(ACCOUNT_BALANCE);
   main_comment="exp_multic: ";
   N=ArraySize(valuta);
   on_trade=true;
   for(int i=0;i<N; i++)
     {
      Type[i]=true;
      Lot[i]=minLot;
     }
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment("");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   double Balans=AccountInfoDouble(ACCOUNT_BALANCE);
   double Equity=AccountInfoDouble(ACCOUNT_EQUITY);
   double FreeMargin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   Comment("Стартовый баланс: ",NormalizeDouble(realEquity,2),
           "\n","Текущий баланс: ",NormalizeDouble(Balans,2),
           "\n","Результат торговли робота: ",NormalizeDouble(Balans-realEquity,2));
//---
   if(Balans-Equity>loss) on_trade=false;    // при достижении заданного убытка все позиции закрываются
   if(Equity-Balans>profit) on_trade=false;  // при достижении заданной прибыли все позиции закрываются
//--- Закрытие всех позиций принудительно
   if(!on_trade)
     {
      for(int i=0;i<N; i++)
        {
         trade.PositionClose(valuta[i]);
         Lot[i]=minLot;
        }
      on_trade=true;
     }
//---
   if(on_trade)
     {
      for(int i=0;i<N; i++)
        {
         if(PositionSelect(valuta[i]))
           {
            //--- Если позиция прибыльная, то наращиваем объём
            if(PositionGetDouble(POSITION_PROFIT)>Lot[i]*k_change)
              {
               Lot[i]=Lot[i]+minLot;
               double lot=minLot;
               if(Type[i])
                 {
                  SymbolInfoTick(valuta[i],last_tick);
                  double price=last_tick.ask;
                  trade.PositionOpen(valuta[i],ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,main_comment+valuta[i]);
                 }
               else
                 {
                  SymbolInfoTick(valuta[i],last_tick);
                  double price=last_tick.bid;
                  trade.PositionOpen(valuta[i],ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,main_comment+valuta[i]);
                 }
              }
            //--- Если позиция убыточная, то закрываем её и даём команду на разворот
            if(PositionGetDouble(POSITION_PROFIT)<-Lot[i]*k_change)
              {
               Type[i]=!Type[i];
               trade.PositionClose(valuta[i]);
              }
            //--- Закрываем прибыльную позицию
            if(PositionGetDouble(POSITION_PROFIT)>minLot*k_closse)
               trade.PositionClose(valuta[i]);

           }
         //--- Открытие новых позиций
         if(!PositionSelect(valuta[i]) && FreeMargin>margin)
           {
            Lot[i]=minLot;
            double lot=minLot;
            if(Type[i])
              {
               SymbolInfoTick(valuta[i],last_tick);
               double price=last_tick.ask;
               trade.PositionOpen(valuta[i],ORDER_TYPE_BUY,NormalizeDouble(lot,2),price,0,0,main_comment+valuta[i]);
              }
            else
              {
               SymbolInfoTick(valuta[i],last_tick);
               double price=last_tick.bid;
               trade.PositionOpen(valuta[i],ORDER_TYPE_SELL,NormalizeDouble(lot,2),price,0,0,main_comment+valuta[i]);
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
