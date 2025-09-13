//+------------------------------------------------------------------+
//|                                                      nextbar.mq4 |
//|                                                              Dan |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "1-follow, 2-return"
#property link      "http://www.metaquotes.net"
//----
#define MAGICMA  20050610
//---- input parameters
extern int       bars2check=8;
extern int       bars2hold=10;
//extern int     filterday=0;
extern int       minbar=77;
//extern int     maxshadow=20;
//extern int     minshadow=20;
//extern int     tradesallowed=1;
extern int       profit=115;
extern int       loss=115;
//----
int       direction=1;
int bars;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void Check4Open()
  {
     {
      int Order;
        {
         if ((((Close[1]-Close[bars2check+1])>=minbar*Point)
         && (direction==1))
         || (((Close[bars2check+1]-Close[1])>=minbar*Point)
         && (direction==2)))
           {
            Order=OrderSend(Symbol(),OP_BUY,1,Ask,3,0,10,"",MAGICMA,0,Blue);
            bars=Bars;
            return;
           }
         if ((((Close[1]-Close[bars2check+1])>=minbar*Point)
         && (direction==2))
         || (((Close[bars2check+1]-Close[1])>=minbar*Point)
         && (direction==1)))
           {
            Order=OrderSend(Symbol(),OP_SELL,1,Bid,3,10,0,"",MAGICMA,0,Red);
            bars=Bars;
            return;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void Check4Close()
  {
   for(int i=0;i<OrdersTotal();i++)
     {
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)  break;
         if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
         if ((OrderType()==OP_BUY)
         && (((Close[1]-OrderOpenPrice())>=profit*Point)
         || ((OrderOpenPrice()-Close[1])>=loss*Point)))
           {
            OrderClose(OrderTicket(),1,Bid,3,White);
            break;
           }
         if ((OrderType()==OP_SELL)
         && (((Close[1]-OrderOpenPrice())>=loss*Point)
         || ((OrderOpenPrice()-Close[1])>=profit*Point)))
           {
            OrderClose(OrderTicket(),1,Ask,3,White);
            break;
           }
        }
      if (Bars==bars+bars2hold)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
         if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
         if
            (OrderType()==OP_BUY)
           {
            OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
            break;
           }
         if
            (OrderType()==OP_SELL)
           {
            OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
            break;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void start(int b)
  {
   if (Bars!=b)
     {
      b=Bars;
      if
         (OrdersTotal()<1) //tradesallowed
        {
         Check4Open();
        }
      if(OrdersTotal()>0)
        {
         Check4Close();
         b=Bars;
        }
     }
  }
//+------------------------------------------------------------------+