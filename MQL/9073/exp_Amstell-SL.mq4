//+------------------------------------------------------------------+
//|                                                  exp_Amstell.mq4 |
//|                                   Copyright © 2009, Yuriy Tokman |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, Yuriy Tokman"
#property link      "yuriytokman@gmail.com"

//торговля без стоп лосов,период day,покупать ,если цена открытия последнего 
//открытого ордера выше текущей цены,и закрывать ордер по достижении 1000 пунктов,
//то же и с продажей.открывать ордер sell,если последний открытый sell ниже текущей 
//цены. открывать позиции по ценам открытия

extern int    TakeProfit       = 30;            // Размер тейка в пунктах
extern int    StopLoss         = 30;           // Размер стопа в пунктах
extern double Lots             = 0.01;          // Размер лота

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
  int Magic=0;
  
for(int cnt=0;cnt<OrdersTotal();cnt++)// Перебираем все ордера
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);//ордер выбирается среди открытых и отложенных ордеров
      if( OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)// Совпадает ли символ ордера( Здесь по надобности ещё магик можно проверить)
        {
         if(OrderType()==OP_BUY)//Отбираем позицию бай
           {
            if(Bid-OrderOpenPrice()>TakeProfit*Point || OrderOpenPrice()-Ask>StopLoss*Point)//
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); //закрываем ордер
               return(0); // выходим
              }//StopLoss
           }
          if(OrderType()==OP_SELL)//Отбираем позицию селл
           {
            if(OrderOpenPrice()-Ask>TakeProfit*Point || Bid-OrderOpenPrice()>StopLoss*Point)//
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); //закрываем ордер
               return(0); // выходим
              }
           }
         }
      }
//----
 int buy = 0, sell = 0;
//----
   if(!ExistPositions(NULL,OP_BUY))buy=1;
   else if(PriceOpenLastPos(NULL,OP_BUY)-Ask>10*Point)buy=1; 
   
   if(!ExistPositions(NULL,OP_SELL))sell=1;
   else if(Bid-PriceOpenLastPos(NULL,OP_SELL)>10*Point)sell=1;   
//----
    if(buy==1)
    OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"",0,0,Green);
    
    if(sell==1)
    OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"",0,0,Red);
//----
   return(0);
  }
//+------------------------------------------------------------------+
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
//|  Версия   : 19.02.2008                                                     |
//|  Описание : Возвращает цену открытия последней открытой позиций.           |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//+----------------------------------------------------------------------------+
double PriceOpenLastPos(string sy="", int op=-1, int mn=-1) {
  datetime t;
  double   r=0;
  int      i, k=OrdersTotal();

  if (sy=="0") sy=Symbol();
  for (i=0; i<k; i++) {
    if (OrderSelect(i, SELECT_BY_POS, MODE_TRADES)) {
      if (OrderSymbol()==sy || sy=="") {
        if (OrderType()==OP_BUY || OrderType()==OP_SELL) {
          if (op<0 || OrderType()==op) {
            if (mn<0 || OrderMagicNumber()==mn) {
              if (t<OrderOpenTime()) {
                t=OrderOpenTime();
                r=OrderOpenPrice();
              }
            }
          }
        }
      }
    }
  }
  return(r);
}