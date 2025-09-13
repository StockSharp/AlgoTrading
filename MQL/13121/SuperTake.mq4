//+------------------------------------------------------------------+
//|                                                      ProjectName |
//|                                      Copyright 2012, CompanyName |
//|                                       http://www.companyname.net |
//+------------------------------------------------------------------+
#property copyright "genino.belaev@yandex.ru"
#property link      "https://www.mql5.com/ru/users/genino"
#property version   "1.00"
#property strict
extern double lots=0.1;// 
extern double STakeProfit=10;
extern double StopLoss=15;
extern int magic=123455;
extern double Martin=1.8;
int ticet=0;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(Digits==5 || Digits==3)
     {
      STakeProfit*=10;
      StopLoss*=10;
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Comment("LossPoslednei()= ",LossPoslednei());
     {
      if(OrdersTotal()==0 && OrdersHistoryTotal()==0)
        {
         ticet=OrderSend(Symbol(),OP_BUY,lots,Ask,3,Bid-StopLoss*Point,Ask+TP()*Point,"покупка",magic,0,clrGreen);
        }
      if(OrdersTotal()==0 && GetTypeLastClosePos()==0)
        {
         ticet=OrderSend(Symbol(),OP_SELL,lots,Bid,3,Ask+StopLoss*Point,Bid-TP()*Point,"продажа",magic,0,clrRed);
        }

      if(OrdersTotal()==0 && GetTypeLastClosePos()==1)
        {
         ticet=OrderSend(Symbol(),OP_BUY,lots,Ask,3,Bid-StopLoss*Point,Ask+TP()*Point,"покупка",magic,0,clrGreen);
        }
     }
  }
//+------------------------------------------------------------------+
//| Считаем последний ордер                                          |
//+------------------------------------------------------------------+
int GetTypeLastClosePos(string sy="",int mn=-1)
  {
   datetime t=0;
   int      i,k=OrdersHistoryTotal(),r=-1;
//---
   if(sy=="0") sy=Symbol();
   for(i=0; i<k; i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if((OrderSymbol()==sy || sy=="") && (mn<0 || OrderMagicNumber()==mn))
           {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               if(t<OrderCloseTime())
                 {
                  t=OrderCloseTime();
                  r=OrderType();
                 }
              }
           }
        }
     }
   return(r);
  }
//+----------------------------------------------------------------------------+
//|  Автор    : Ким Игорь В. aka KimIV,  http://www.kimiv.ru                   |
//+----------------------------------------------------------------------------+
//|  Версия   : 19.02.2008                                                     |
//|  Описание : Возвращает флаг убыточности последней позиции.                 |
//+----------------------------------------------------------------------------+
//|  Параметры:                                                                |
//|    sy - наименование инструмента   (""   - любой символ,                   |
//|                                     NULL - текущий символ)                 |
//|    op - операция                   (-1   - любая позиция)                  |
//|    mn - MagicNumber                (-1   - любой магик)                    |
//+----------------------------------------------------------------------------+
bool isLossLastPos(string sy="",int op=-1,int mn=-1)
  {
   datetime t=0;
   int      i,j=-1,k=OrdersHistoryTotal();
//---
   if(sy=="0") sy=Symbol();
   for(i=0; i<k; i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
        {
         if(OrderSymbol()==sy || sy=="")
           {
            if(OrderType()==OP_BUY || OrderType()==OP_SELL)
              {
               if(op<0 || OrderType()==op)
                 {
                  if(mn<0 || OrderMagicNumber()==mn)
                    {
                     if(t<OrderCloseTime())
                       {
                        t=OrderCloseTime();
                        j=i;
                       }
                    }
                 }
              }
           }
        }
     }
   if(OrderSelect(j,SELECT_BY_POS,MODE_HISTORY))
     {
      if(OrderProfit()<0) return(True);
     }
   return(False);
  }
//+------------------------------------------------------------------+
//| Возвращаем стоплосс последней убыточной сделки из истории        |
//| в пунктах                                                        |
//| В настройках стоп и тейк должны быть типа double                 |  
//+------------------------------------------------------------------+
double LossPoslednei()
  {
   int k=OrdersHistoryTotal();
   int i;
   double Delta=0;
   if(isLossLastPos()==true)
     {
      for(i=0; i<k; i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
           {
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_SELL)
              {
               Delta=(OrderOpenPrice()-OrderTakeProfit())/Point;
              }
           }
        }
      for(i=0; i<k; i++)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY))
           {
            if(OrderSymbol()==Symbol() && OrderMagicNumber()==magic && OrderType()==OP_BUY)
              {
               Delta=(OrderTakeProfit()-OrderOpenPrice())/Point;
              }
           }
        }
     }
   return(NormalizeDouble(Delta, Digits));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double TP()
  {
   double tec=0;
   if(isLossLastPos()==True)
     {
      tec=NormalizeDouble(LossPoslednei()*Martin,2);
     }
//---
   if(isLossLastPos()==False)
     {
      tec=STakeProfit;
     }
   return(tec);
  }
//+------------------------------------------------------------------+
