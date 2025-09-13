//+------------------------------------------------------------------+
//|                                  Get Rich or Die Trying GBP.mq4 |
//|                                                     Carlos Gomes |
//|                                                                  |
//+------------------------------------------------------------------+
extern double TakeProfit=100;
extern double TakeProfit2=40;
extern double Stoploss=100;
extern double Lots=1.0;
extern double TrailingStop=30;
extern double per=18;
extern double per2=8;
extern double chas=2;
extern double totalt=1000;
extern double mm=0;
extern double risk=10;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int cnt, ticket, total;
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
   if(TakeProfit<0)
     {
      Print("TakeProfit less than 10");
      return(0);  // check TakeProfit
     }
   double Lotsi;
   if (mm!=0)
     {
      Lotsi=NormalizeDouble(AccountFreeMargin()*risk/100001,1);
      if (Lotsi<0.1)  Lotsi=0.1;
     }
   else Lotsi=Lots;
   //if ((CurTime()-OrderOpenTime())<61) return(0);
   int up, down; up=0; down=0;
   for(cnt=1;cnt<per;cnt++)
      if (Open[cnt]>Close[cnt]) up=up+1;
   for(cnt=1;cnt<per;cnt++)
      if (Open[cnt]<Close[cnt]) down=down+1;
//----
   int j;
   j=0;
   total=OrdersTotal();
   //
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())  // check for symbol
        {
         j=1;
         if(OrderType()==OP_BUY)
           {
            if(Bid-OrderOpenPrice()>=TakeProfit2*Point)
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,0,Violet); return(0);
              }
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green); return(0);
                    }
                 }
              }
           }
         else
           {
            if(OrderOpenPrice()-Ask>=TakeProfit2*Point)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,0,Violet); return(0);
              }
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red); return(0);
                    }
                 }
              }
           }
        }
     }
   if (total<totalt)
     {
       if(up>down && (22+(chas)==Hour()|| 19+(chas)==Hour()) &&Minute()<5)// && j==1)
        {
         OrderSend(Symbol(),OP_BUY,Lotsi,Ask,0,Bid-Stoploss*Point,Ask+TakeProfit*Point,"jk_prof",16384,0,Green); return(0);
        }
      if(up<down && (22+(chas)==Hour()|| 19+(chas)==Hour()) && Minute()<5)// && j==1)
        {
         OrderSend(Symbol(),OP_SELL,Lotsi,Bid,3,Ask+Stoploss*Point,Bid-TakeProfit*Point,"macd sample",16384,0,Red); return(0);
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+