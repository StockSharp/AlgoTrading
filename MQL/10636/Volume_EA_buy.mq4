extern int MagicNumber=454;
extern double Lots=0.01;
extern double Factor=1.55;
extern double Trailing_Stop=350;
extern double CCILevel1=50;
extern double CCILevel2=190;
int start()
  {
//----
  int i,ordini;
  for (i=0;i<OrdersTotal();i++)
  {
      OrderSelect(i,SELECT_BY_POS);
      if ((OrderMagicNumber()==MagicNumber)&&(OrderSymbol()==Symbol()))
      {
         ordini++;
      }
  }
  if ((ordini==0)&&(TimeMinute(MarketInfo(Symbol(),MODE_TIME))==00))
  {
      if (Volume[1]>Volume[2]*Factor)
      if (iCCI(NULL, NULL,14,PRICE_TYPICAL,0)>CCILevel1)
      if (iCCI(NULL, NULL,14,PRICE_TYPICAL,0)<CCILevel2)
      if (Open[1]<Close[1])
      {
         {
            OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,NULL,MagicNumber,0,Blue);
         }
      }
  }
  else
  {
      if (TimeHour(MarketInfo(Symbol(),MODE_TIME))==23)
      {
         for (i=0;i<OrdersTotal();i++)
         {
            OrderSelect(i,SELECT_BY_POS);
            if ((OrderMagicNumber()==MagicNumber)&&(OrderSymbol()==Symbol()))
            {
               if (OrderType()==OP_BUY)
               {
                  OrderClose(OrderTicket(),OrderLots(),Bid,3,Green);
               }
               else
               {
                  OrderClose(OrderTicket(),OrderLots(),Ask,3,Green);
               }
            }
         }
      }
  }
//----
if(OrderType()==OP_BUY && Bid-OrderOpenPrice() > Trailing_Stop*Point && OrderMagicNumber()==MagicNumber && OrderStopLoss()< Bid - Trailing_Stop*Point )
            {
                    OrderModify(OrderTicket(),OrderOpenPrice(),Bid - Trailing_Stop*Point,OrderTakeProfit(),0,Blue);
                    Sleep(10000);
            }
      if(OrderType()==OP_SELL && OrderOpenPrice()-Ask > Trailing_Stop*Point && OrderMagicNumber()==MagicNumber && OrderStopLoss()> Ask + Trailing_Stop*Point)
            {
                    OrderModify(OrderTicket(),OrderOpenPrice(),Bid + Trailing_Stop*Point,OrderTakeProfit(),0,Blue);
                    Sleep(10000);
            }
   return(0);
}

