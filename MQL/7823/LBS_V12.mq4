//+--------------------------------------------------------------------+
//|                                                      LBS V 1.0.mq4 |
//|                                                       Jacob Yego   |
//|                                                                    |
//+--------------------------------------------------------------------+
#property copyright "Jacob Yego"
#property link      "PointForex"
//----
extern int LotSize=10000; //Currency amount per Lot
extern int Leverage=0;    //Set to your Risk tolerance
extern int TrailingStop= 20;
extern int Slippage=5;
extern int TakeProfit=100;
extern int Triggertime=9;
extern int Lots=1;
// initialize internal variables
  double OpenBuy,SlBuy,OpenSell,SlSell,TpBuy,TpSell;
  //double SS1,SS2,SS3,S1,S2,S3;//for use to develop 3 wave system
  //double BS1,BS2,BS3,B1,B2,B3;// for use to develop 3 wave system
  double ATR,h,m,total,cnt,ticket,Spread;
  double time,NewPrice=0;
  double PriceOpen, Buy_Sl, Buy_Tp, LotMM;
  string symbol;
  //int gear1=2;//develop strategy to open 2nd order with take profit TP*gear1
  //int gear2=3; //develop strategy to open 3rd order with take profit TP*gear2
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   ATR=iATR(NULL,0,3,1);
//----
   //  SetLotsMM
   if(Leverage > 0)LotMM=MathMax(MathFloor(AccountBalance()*Leverage/LotSize),1)*LotSize/100000;
   else LotMM=Lots;
//----
   if(AccountFreeMargin() < LotSize/AccountLeverage()*LotMM)
     {
      Alert("Not enough money to open trade. Free Margin = ",
      AccountFreeMargin(),". Number of Lots in trade = ",LotMM);
      return(-1);
     }
   //+------------------------------------------------------------------+
   //|Place New Order                                                   |
   //+------------------------------------------------------------------+
   symbol=Symbol();
   Spread=MarketInfo (symbol, MODE_ASK) - MarketInfo (symbol, MODE_BID);
   time=TimeHour(CurTime());
   total=OrdersTotal();
//----
   if(total>0)
     {
      for(cnt=0;cnt<total;cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol())
//---
            return(0);
        }
     }
     if(total < 1)
     {
      ticket=0;
      if (time==Triggertime)
        {
         Print ("LBS BuyStop: ", Symbol(), " ", LotMM, " ", OpenBuy, " ", SlBuy, " ", TpBuy);
         OpenBuy=(iHigh(NULL, PERIOD_M15, 1) + 1*Point + Spread + ATR);
         SlBuy=(iLow (NULL,PERIOD_M15, 1) - 1*Point);
         TpBuy =(OpenBuy+30*Point);
         ticket=OrderSend (Symbol(), OP_BUYSTOP, Lots, OpenBuy, Slippage, SlBuy, TpBuy,
                             "LBS BuyStop", 0, 01969, Red);
        }
        if(total < 1)
        {
         if  (time==Triggertime)
           {
            Print ("LBS BuyStop: ", Symbol(), " ", LotMM, " ", OpenSell, " ", SlSell, " ", TpSell);
            OpenSell=(iLow(NULL, PERIOD_M15, 1) - 1*Point - Spread- ATR);
            SlSell=(iHigh (NULL,PERIOD_M15, 1) + 1*Point);
            TpSell= (OpenSell+30*Point);
            ticket=OrderSend (Symbol(), OP_SELLSTOP, Lots, OpenSell, Slippage, SlSell, TpSell,
                                   " LBS SellStop", 01969, 0, Green);
            ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+(30*Point),Bid-(TakeProfit*Point),"MaxMin Long",16384,0,Pink);
            //RUN TRAILING STOP: BUY
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
              {
               if(TrailingStop>0)
                 {
                  if(Bid-OrderOpenPrice()>Point*TrailingStop)
                    {
                     if(OrderStopLoss()<Bid-(Point*TrailingStop))
                       {
                        OrderModify(OrderTicket(),OrderOpenPrice(),Bid-(Point*TrailingStop),OrderTakeProfit(),0,Lime);
                        return(0);
                       }
                    }
                 }
              }
            //RUN TRAILING STOP: SELL
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
              {
               if(TrailingStop>0)
                 {
                  if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                    {
                     if(OrderStopLoss()>Ask+(Point*TrailingStop))
                       {
                        OrderModify(OrderTicket(),OrderOpenPrice(),Ask+(Point*TrailingStop),OrderTakeProfit(),0,Lime);
                        return(0);
                       }
                    }
                 }
              }
           }
        }
        //----
      return(0);
     }
  }
//+------------------------------------------------------------------+