//+------------------------------------------------------------------+
//|                                                        Probe.mq4 |
//|                      Copyright © 2005, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
extern double Zazor=30; //Уроень отложеного ордера от текущей цены
extern double StopLoss=95;
extern double Kanal=120 ;
extern double periodinduka=73 ;
extern double UdalOrdotl=30 ;
extern double TrailingStop=110 ;
extern double UrBezubitka=60 ;
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   double vg=Kanal;
   double ng=-Kanal;
//----   
   double cci0=iCCI(NULL,PERIOD_H4,periodinduka,PRICE_TYPICAL,0);
   double cci1=iCCI(NULL,PERIOD_H4,periodinduka,PRICE_TYPICAL,1);
   //double cci2=iCCI(NULL,PERIOD_H4,periodinduka,PRICE_TYPICAL,2);
   double cnt,trade;
   trade=1;
     for(cnt=0;cnt<OrdersTotal ();cnt++) 
     {
      OrderSelect( cnt,SELECT_BY_POS,MODE_TRADES) ;
      if(OrderSymbol()==Symbol())  trade=0;
     }
   if(cci0 >ng &&  cci1<ng && trade==1   )
     {
      OrderSend(Symbol(),OP_BUYSTOP,0.1,Ask+Zazor*Point,3,Ask+Zazor*Point-StopLoss*Point,0,0,Green);
     }
   if(cci0<vg && cci1>vg  && trade==1       )
     {
      OrderSend(Symbol(),OP_SELLSTOP,0.1,Bid-Zazor*Point,3,Bid-Zazor*Point+StopLoss*Point,0,0,Green);
     }
     for(cnt=0;cnt<OrdersTotal ();cnt++) 
     {
        {
         OrderSelect(0, SELECT_BY_POS,MODE_TRADES) ;
           if (OrdersTotal () >0)
           {
              if (OrderType()==OP_BUYSTOP && OrderOpenPrice() -Ask  > UdalOrdotl* Point && OrderSymbol()==Symbol()) 
              {
               OrderDelete (OrderTicket () );
              }
               }
           {
            if (OrderType()==OP_SELLSTOP && Ask - OrderOpenPrice()   > UdalOrdotl* Point && OrderSymbol()==Symbol())
              {
               OrderDelete (OrderTicket () );
              }
           }
        }
      //-------
      if(OrdersTotal () > 0  )
        {
           for(cnt=0;cnt<OrdersTotal ();cnt++) 
           {
            OrderSelect(cnt, SELECT_BY_POS,MODE_TRADES);
              if(OrderType()==OP_BUY && cci0<vg && cci1>vg  && OrderSymbol()==Symbol() )
              {
               OrderClose(OrderTicket(),0.1,Bid,3,White);
               OrderSend(Symbol(),OP_SELLSTOP,0.1,Bid-Zazor*Point,3,Bid-Zazor*Point+StopLoss*Point,0,0,Green);
              }
              if(OrderType()==OP_SELL &&  cci0 >ng && cci1<ng  && OrderSymbol()==Symbol() )
              {
               OrderClose(OrderTicket(),0.1,Ask,3,White);
               OrderSend(Symbol(),OP_BUYSTOP,0.1,Ask+Zazor*Point,3,Ask+Zazor*Point-StopLoss*Point,0,0,Green);
              }                     
           }
        }
     }
   //Print(" CCI= ",cci0," Osma= ",osma," OsMA1= ",osma1," OsMA2= ",osma2," OsMA3= ",osma3,vniz,vverx);
     for(cnt=0;cnt<OrdersTotal ();cnt++)
     {
      if(TrailingStop>0 && OrderSymbol()==Symbol())
        {
         if(Bid-OrderOpenPrice()>Point*TrailingStop)
           {
            if(OrderStopLoss()<Bid-Point*TrailingStop)
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
               return(0);
              }
           }
        }
      if(TrailingStop>0 && OrderSymbol()==Symbol())
        {
         if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
           {
            if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
              {
               OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
               return(0);
              }
           }
        }
     }
     for(cnt=0;cnt<OrdersTotal ();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS) ;
        if (OrderType()==OP_BUY && Bid-OrderOpenPrice() > UrBezubitka*Point && OrderSymbol()==Symbol() && OrderOpenPrice()-OrderStopLoss() > 10*Point && UrBezubitka>0) 
        {
        OrderModify( OrderTicket (), 0, OrderOpenPrice(), 0, 0,0);}
        if (OrderType()==OP_SELL && OrderOpenPrice()-Bid > UrBezubitka*Point && OrderSymbol()==Symbol()&& OrderStopLoss()-OrderOpenPrice() > 10*Point&& UrBezubitka>0) 
        {
        OrderModify( OrderTicket (), 0, OrderOpenPrice(), 0, 0,0);}
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+