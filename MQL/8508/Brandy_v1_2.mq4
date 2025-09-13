//+------------------------------------------------------------------+
//|                                                  Brandy v1.1.mq4 |
//|                                       Copyright © 2008, Virtuoso |
//|                                  mailto: virtuoso2008@rambler.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Virtuoso"
#property link      "mailto: virtuoso2008@rambler.ru"


//---- input parameters

extern string param1 = "Параметры оптимизации";
extern int          p1   =  70;
extern int          s1   =   5;
extern int          p2   =  20;
extern int          s2   =   5;
extern double       sl   =  50;
extern double       ts   = 150;

extern string param2 = "Неоптимизируемые";
extern double       lots = 0.1;
extern int          mn   = 784;

static int prevtime;


//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   prevtime=Time[0];
  }


//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   if(Time[0]==prevtime) return(0);
   prevtime=Time[0];
   
   if(!IsTradeAllowed())
     {
      povtor();
      return(0);
     }

//----

   double ma11=iMA(NULL,0,p1,0,MODE_SMA,PRICE_CLOSE,1);
   double ma21=iMA(NULL,0,p1,0,MODE_SMA,PRICE_CLOSE,s1);
   double ma12=iMA(NULL,0,p2,0,MODE_SMA,PRICE_CLOSE,1);
   double ma22=iMA(NULL,0,p2,0,MODE_SMA,PRICE_CLOSE,s2);

//------проверяем открытые ордера и при необходимости тралим либо закрываем позицию------

   int k=0;
   if(OrdersTotal()>0)
     {
      bool zm=true;
      int total=OrdersTotal();
      RefreshRates();
      for(int i=0;i<total;i++)
        {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if(OrderSymbol()==Symbol() && OrderMagicNumber()==mn)
           {
            k++;
            if(OrderType()==OP_BUY)
              {
               if(ma11<ma21)
                 {
                  zm=OrderClose(OrderTicket(),OrderLots(),Bid,3,Red);
                  k=0;
                 }
               else if(ts>=100 && Bid-OrderOpenPrice()>ts*Point && Bid-OrderStopLoss()>ts*Point)
                 {
                  zm=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-ts*Point,0,0,CLR_NONE);
                 }
               if(!zm)
                 {
                  povtor();
                  return(0);
                 }
              }
            else if(OrderType()==OP_SELL)
              {
               if(ma11>ma21)
                 {
                  zm=OrderClose(OrderTicket(),OrderLots(),Ask,3,Red);
                  k=0;
                 }
               else if(ts>=100 && OrderOpenPrice()-Ask>ts*Point && OrderStopLoss()-Ask>ts*Point)
                 {
                  zm=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+ts*Point,0,0,CLR_NONE);
                 }
               if(!zm)
                 {
                  povtor();
                  return(0);
                 }
              }
           } 
        }
     }

//------открываем позицию------

   int ticket;
   RefreshRates();

   if(ma11>ma21 && ma12>ma22 && k==0)
     {   
      ticket=OrderSend(Symbol(),OP_BUY,lots,Ask,3,Ask-sl*Point,0,WindowExpertName(),mn,0,Blue); 
     }

   else if (ma11<ma21 && ma12<ma22 && k==0)
     {
      ticket=OrderSend(Symbol(),OP_SELL,lots,Bid,3,Bid+sl*Point,0,WindowExpertName(),mn,0,Red); 
     }

   if(ticket<0) povtor();

//-- Exit --
   return(0);
  }


void povtor()
  {
   Sleep(30000);
   prevtime=Time[1];
  }