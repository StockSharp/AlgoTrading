//+------------------------------------------------------------------+
//|   9-0-7-1-2-2-2-1                                        ttt.mq4 |
//|   9-0-2-1-2-1-2.5                                            Dan |
//|   9-0-9-2-1-1-1-1                                                |
//|   9-0-6-1-2-2-2-1(m15)                 http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Dan"
#property link      "http://www.metaquotes.net"
//---- input parameters
extern int MAGICMA =20050610;
extern double    lots=0.1;
extern int        StopLoss=200;
extern int        TakeProfit=200;
extern int       checkhour=8;
extern int       checkminute=0;
extern int       days2check=7;
extern int       checkmode=1;
extern double    profitK=2;
extern double    lossK=2;
extern double    offsetK=2;
extern int       closemode=1;
//+------------------------------------------------------------------+
int      latestopenhour=23;
int      tradesallowed=1;
double   sellprice;
double   buyprice;
int      profit;
int      loss;
int      offset;
int      avrange;
double   daj[30];
int      i;
int      dd;
int      bb;
int      d;
int      pp;
double   hh;
double   ll;
int      p;
double   totalrange;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void start() 
  {
   if (!IsTradeAllowed()) return;
   if (Bars==bb) return;
   bb=Bars;
//----
     if((Hour()==checkhour) && (Minute()==checkminute)) 
     {
      dd=Day();
      pp=((checkhour*60)/Period()+checkminute/Period());
      hh=High[Highest(NULL,0,MODE_HIGH,pp,1)];
      ll=Low[Lowest(NULL,0,MODE_LOW,pp,1)];
      p=((24*60)/Period());
      totalrange=0;
//----
        if(checkmode==1)   
        {
           for(i=1;i<=days2check;i++) 
           {
            daj[i]=(High[Highest(NULL,0,MODE_HIGH,p,p*i+1)]-Low[Lowest(NULL,0,MODE_LOW,p,p*i+1)]);
            totalrange=totalrange+daj[i];
            avrange=MathRound((totalrange/i)/Point);
           }
        }
        if(checkmode==2)  
        {
           for( i=1;i<=days2check;i++) 
           {
            daj[i]=MathAbs(Close[p*i+pp]-Close[p*(i-1)+pp]);
            totalrange=totalrange+daj[i];
            avrange=MathRound((totalrange/i)/Point);
           }
        }
      offset=MathRound((avrange)/offsetK);
      sellprice=ll-offset*Point;
      buyprice=hh+offset*Point;
        if   (CalculateCurrentOrders()>0)  
        {
           for(i=0;i<OrdersTotal();i++) 
           {
            if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)  break;
            if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
              if (OrderType()==OP_BUY) 
              {
               OrderClose(OrderTicket(),lots,Bid,3,White);
               break;
              }
              if (OrderType()==OP_SELL) 
              {
               OrderClose(OrderTicket(),lots,Ask,3,White);
               break;
              }
           }
        }
     }
     if (closemode==2  && Day()!=dd && (CalculateCurrentOrders()>0))    
     {
        for(i=0;i<OrdersTotal();i++) 
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)  break;
         if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
           if (OrderType()==OP_BUY) 
           {
            OrderClose(OrderTicket(),lots,Bid,3,White);
            break;
           }
           if (OrderType()==OP_SELL) 
           {
            OrderClose(OrderTicket(),lots,Ask,3,White);
            break;
           }
        }
     }
     for(i=0;i<OrdersTotal();i++) 
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)  break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
        if ((OrderType()==OP_BUY) && (((Close[1]-OrderOpenPrice())>=profit*Point) || ((OrderOpenPrice()-Close[1])>=loss*Point))) 
        {
         OrderClose(OrderTicket(),lots,Bid,3,White);
         break;
        }
        if ((OrderType()==OP_SELL) && (((Close[1]-OrderOpenPrice())>=loss*Point) || ((OrderOpenPrice()-Close[1])>=profit*Point)))  
        {
         OrderClose(OrderTicket(),lots,Ask,3,White);
         break;
        }
     }
   int lastopenhour=23;
//----
     if ((Hour()<=lastopenhour)  &&  (Day()==dd) &&  (Day()!=d) && (CalculateCurrentOrders()<tradesallowed))  
     {
        if (Close[1]>=buyprice) 
        {
         profit=MathRound((avrange)/profitK);
         loss=MathRound((avrange)/lossK);
         OrderSend(Symbol(),OP_BUY,lots,Ask,3,Ask-StopLoss*Point,Ask+TakeProfit*Point,"ttt",MAGICMA,0,Blue);
         dd=Day();
         if (tradesallowed==1)  {d=Day();}
         //      return(d);
        }
        if (Close[1]<=sellprice) 
        {
         profit=MathRound((avrange)/profitK);
         loss=MathRound((avrange)/lossK);
         OrderSend(Symbol(),OP_SELL,lots,Bid,3,Bid+StopLoss*Point,Bid-TakeProfit*Point,"ttt",MAGICMA,0,Red);
         dd=Day();
         if (tradesallowed==1)  {d=Day();}
        }
      return;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  int CalculateCurrentOrders()  
  {
   int ord=0;
   string symbol=Symbol();
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         ord++;
        }
     }
   return(ord);
  }
//+------------------------------------------------------------------+