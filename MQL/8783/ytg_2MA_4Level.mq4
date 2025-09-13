//+------------------------------------------------------------------+
//|                                               ytg_2MA_4Level.mq4 |
//|                                                     Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

/*
2 SMA первая с параметрами 14 вторая с 180
также есть параллельные линии:
SMA 180 + 250 пунктов по Y
SMA 180 + 500 пунктов по Y
SMA 180 - 250 пунктов по Y
SMA 180 - 500 пунктов по Y
Работает вот так: при пересечение MA14 любую линию происходит либо покупка, либо продажа*/

extern string _____1_____ = "Торговые настройки ";
extern int тейк_профит =   130; 
extern int стоп_лосс =    1000;
extern int лоты = 1;
extern string _____2_____ = "Настройки индикаторов ";
extern int расчётный_бар          = 1;
extern int период_быстрой_МА      = 14;
extern int метод_быстрой_МА       = 2;//0-3
extern int ценов_конст_быстрой_МА = 4;//0-6
extern int период_медлн_МА        =180;
extern int метод_медлн_МА         =2;//0-3
extern int ценов_конст_медлн_МА   =4;//0-6
extern string _____3_____ = "Настройки уровней ";
extern int верхний_1      = 500;
extern int верхний_2      = 250;
extern int нижний_1       = 500;
extern int нижний_2       = 250;

#include <stdlib.mqh>        // Стандартная библиотека МТ4

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   if(!IsDemo()) 
   {
    Comment("Версия только для тестов");
    return(0);
   }
   else Comment("Тестовая версия");

   if(!ExistPositions()) 
     {
     
      if( GetSignal()==1)OpenPosition("",OP_BUY,лоты,Bid-стоп_лосс*Point,Ask+тейк_профит*Point);

      if( GetSignal()==-1)OpenPosition("",OP_SELL,лоты,Ask+стоп_лосс*Point,Bid-тейк_профит*Point);
      
     }   
//----
   return(0);
  }
//+------------------------------------------------------------------+

 int GetSignal()
   {
    double MA_1_0=iMA(Symbol(),0,период_быстрой_МА,0,метод_быстрой_МА,ценов_конст_быстрой_МА,расчётный_бар);
    double MA_1_1=iMA(Symbol(),0,период_быстрой_МА,0,метод_быстрой_МА,ценов_конст_быстрой_МА,расчётный_бар+1);
    double MA_2_0=iMA(Symbol(),0,период_медлн_МА,0,метод_медлн_МА,ценов_конст_медлн_МА,расчётный_бар);
    double MA_2_1=iMA(Symbol(),0,период_медлн_МА,0,метод_медлн_МА,ценов_конст_медлн_МА,расчётный_бар+1);
   
    
    int vSignal = 0;
    if(MA_1_1<=MA_2_1&&MA_1_0>MA_2_0)vSignal = 1;//up
    else
    if(MA_1_1<=MA_2_1+верхний_1*Point&&MA_1_0>MA_2_0+верхний_1*Point)vSignal = 1;//up
    else
    if(MA_1_1<=MA_2_1+верхний_2*Point&&MA_1_0>MA_2_0+верхний_2*Point)vSignal = 1;//up    
    else
    if(MA_1_1<=MA_2_1-нижний_1*Point&&MA_1_0>MA_2_0-нижний_1*Point)vSignal = 1;//up
    else
    if(MA_1_1<=MA_2_1-нижний_2*Point&&MA_1_0>MA_2_0-нижний_2*Point)vSignal = 1;//up    
    
    
     
    else
    if(MA_1_1>=MA_2_1&&MA_1_0<MA_2_0) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1+верхний_1*Point&&MA_1_0<MA_2_0+верхний_1*Point) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1+верхний_2*Point&&MA_1_0<MA_2_0+верхний_2*Point) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1-нижний_1*Point&&MA_1_0<MA_2_0-нижний_1*Point) vSignal =-1;//down
    else
    if(MA_1_1>=MA_2_1-нижний_2*Point&&MA_1_0<MA_2_0-нижний_2*Point) vSignal =-1;//down    
    
        
    return (vSignal);
   }
   
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 06.03.2008                                                     |
//|  Описание : Возвращает флаг существования позиций                          |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//|    ot - время открытия             ( 0   - любое время открытия)           |
//+----------------------------------------------------------------------------+
bool ExistPositions(string sy="", int op=-1, int mn=-1, datetime ot=0) {
  int i, k=OrdersTotal();
 
  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (ot<=OrderOpenTime()) return(True);
            }
          }
        }
      }
    }
  }
  return(False);
}

//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия  : 13.06.2007                                                      |
//|  Описание : Открытие позиции. Версия функции для тестов на истории.        |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   ("" - текущий символ)                   |
//|    op - операция                                                           |
//|    ll - лот                                                                |
//|    sl - уровень стоп                                                       |
//|    tp - уровень тейк                                                       |
//|    mn - MagicNumber                                                        |
//+----------------------------------------------------------------------------+
void OpenPosition(string sy, int op, double ll, double sl=0, double tp=0, int mn=0) {
  color  clOpen;
  double pp;
  int    err, ticket;
 
  if (sy=="") sy=Symbol();
  if (op==OP_BUY) {
    pp=MarketInfo(sy, MODE_ASK); clOpen=Green;
  } else {
    pp=MarketInfo(sy, MODE_BID); clOpen=Red;
  }
  ticket=OrderSend(sy, op, ll, pp,5, sl, tp, "", mn, 0, clOpen);
  if (ticket<0) {
    err=GetLastError();
    Print("Error(",err,") open ",GetNameOP(op),": ",ErrorDescription(err));
    Print("Ask=",Ask," Bid=",Bid," sy=",sy," ll=",ll,
          " pp=",pp," sl=",sl," tp=",tp," mn=",mn);
  }
}

//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 01.09.2005                                                     |
//|  Описание : Возвращает наименование торговой операции                      |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    op - идентификатор торговой операции                                    |
//+----------------------------------------------------------------------------+
string GetNameOP(int op) {
  switch (op) {
    case OP_BUY      : return("Buy");
    case OP_SELL     : return("Sell");
    case OP_BUYLIMIT : return("Buy Limit");
    case OP_SELLLIMIT: return("Sell Limit");
    case OP_BUYSTOP  : return("Buy Stop");
    case OP_SELLSTOP : return("Sell Stop");
    default          : return("Unknown Operation");
  }
} 