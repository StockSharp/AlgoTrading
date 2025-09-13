//+------------------------------------------------------------------+
//|                   Strategy of Regularities of Exchange Rates.mq4 |
//|                       Copyright © 2008, Юрий, yuriy@fortrader.ru |
//|     http://www.ForTrader.ru, Аналитический журнал для трейдеров. |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Юрий, yuriy@fortrader.ru"
#property link      "http://www.ForTrader.ru, Аналитический журнал для трейдеров."

extern int optime=9; //время
extern int cltime=2; //время
extern int point=20;//расстояние
extern double Lots=0.1;//расстояние
extern int TakeProfit=20;//расстояние
extern int StopLoss=500;//расстояние

//Профитный советник для работы на часовиках

int bars;
int start()
  {
  Comment("FORTRADER.RU - версия для тестирования");
  if(IsDemo()==FALSE && IsTesting()==FALSE){Print("FORTRADER.RU -version only testing");return(0);}
 
 PosManager();
 
  //если период больше часовика то выходим
 if(Period()>60){Print("Period must be < hour");return(0);}
  
 if(bars!=Bars)
 {bars=Bars;
 
 TimePattern();
 }
   return(0);
  }


int TimePattern()
{
if(Hour() ==optime)
{
//если цена больше верхней линии то удаляем предыдущий ордер и ставим новый
OrderSend(Symbol(),OP_SELLSTOP,Lots,NormalizeDouble(Ask-point*Point,Digits),3,NormalizeDouble(Ask+StopLoss*Point,Digits),0,"FORTRADER.RU",0,0,Red);
OrderSend(Symbol(),OP_BUYSTOP,Lots,NormalizeDouble(Bid+point*Point,Digits),3,NormalizeDouble(Bid-StopLoss*Point,Digits),0,"FORTRADER.RU",0,0,Red);
}

return(0);
}

int deletebstop()
{
   for( int i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_BUYSTOP && OrderSymbol()==Symbol())
     {
      OrderDelete(OrderTicket()); 
     }//if
    }//if
   }
   return(0);
}

int deletesstop()
{
   for( int i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_SELLSTOP && OrderSymbol()==Symbol())
     {
      OrderDelete(OrderTicket()); 
     }//if
    }//if
   }
   return(0);
}

int PosManager()
{int i,z;

if(Hour() ==cltime){deletebstop();deletesstop();}

for(  i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                                       
     if(OrderType()==OP_SELL && ((OrderOpenPrice()-Ask)>=(TakeProfit)*Point || Hour()==cltime))
     {
     OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);   
     }//if
    }//if
   }
   
   
   for(i=1; i<=OrdersTotal(); i++)          
   {
    if(OrderSelect(i-1,SELECT_BY_POS)==true) 
    {                      
     if(OrderType()==OP_BUY && ((Bid-OrderOpenPrice())>=(TakeProfit)*Point || Hour()==cltime))
     {OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);         
     }//if
    }//if
   }


return(0);
}