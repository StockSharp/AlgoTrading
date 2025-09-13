//+------------------------------------------------------------------+
//|                                      Alexav_D1_Profit_GBPUSD.mq4 |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Alex Saveliev"
#property link      "asavelievca@yahoo.com"
//----
extern double Lots=1;
extern int MAPeriod=6;
extern int RSIPeriod=10;
extern int ATRPeriod=28;
extern double ism=1.6;
extern double tpm=1;
//----
extern double RSIUpperLevel=60;
extern double RSIUpperLimit=80;
extern double RSILowerLevel=39;
extern double RSILowerLimit=25;
//----
extern int FastMAPeriod=5;
extern int SlowMAPeriod=24;
extern int SignalMAPeriod=14;
extern double MacdDiffBuy=0.5;
extern double MacdDiffSell=0.15;
extern double slippage    =5;
extern int magicEA        =11911;
//----
int OpenOrdersBuy=0,OpenOrdersSell=0,oob=0,oos=0,PreviousOpenOrdersBuy=0,PreviousOpenOrdersSell=0,mode=0,cnt=0,k=0,DDay=8;
int OrderNumberBuy=0,OrderNumberSell=0;
double TakeProfit,InitialStop,StopLoss,dd;
//----
bool BlockBuy=False;
bool BlockSell=False;
bool Bopen1=false,Bopen2=false,Bopen3=false,Bopen4=false,Bopen5=false,Bopen6=false,Bopen7=false,Bopen8=false;
bool Bopen9=false,Bopen10=false,Bopen11=false,Bopen12=false;
bool Sopen1=false,Sopen2=false,Sopen3=false,Sopen4=false,Sopen5=false,Sopen6=false,Sopen7=false,Sopen8=false;
bool Sopen9=false,Sopen10=false,Sopen11=false,Sopen12=false;
string LastTradeB=" ",LastTradeS=" ";
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
   //  int totalOrders = OrdersTotal();
   OpenOrdersBuy=0;
   OpenOrdersSell=0;
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol()==Symbol())
        {
         mode=OrderType();
         if (mode==OP_BUY)  OpenOrdersBuy++;
         if (mode==OP_SELL) OpenOrdersSell++;
        }
     }
   if (OpenOrdersBuy==0) oob=0;
   if (OpenOrdersSell==0) oos=0;
//----
   Bopen1=False;
   Bopen2=False;
   Bopen3=False;
   Bopen4=False;
   Bopen5=False;
   Bopen6=False;
   Bopen7=False;
   Bopen8=False;
   Bopen9=False;
   Bopen10=False;
   Bopen11=False;
   Bopen12=False;
//----
   Sopen1=False;
   Sopen2=False;
   Sopen3=False;
   Sopen4=False;
   Sopen5=False;
   Sopen6=False;
   Sopen7=False;
   Sopen8=False;
   Sopen9=False;
   Sopen10=False;
   Sopen11=False;
   Sopen12=False;
   if (PreviousOpenOrdersBuy>OpenOrdersBuy) //If one of orders closed then close the rest of them
     {
      for(cnt=OrdersTotal();cnt>=0;cnt--)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         mode=OrderType();
         if (OrderSymbol()==Symbol() && OrderMagicNumber( )==magicEA)
           {
            if (mode==OP_BUY)
              {
               if (StrToInteger(OrderComment())==1) Bopen1=True;
               if (StrToInteger(OrderComment())==2) Bopen2=True;
               if (StrToInteger(OrderComment())==3) Bopen3=True;
               if (StrToInteger(OrderComment())==4) Bopen4=True;
               if (StrToInteger(OrderComment())==5) Bopen5=True;
               if (StrToInteger(OrderComment())==6) Bopen6=True;
               if (StrToInteger(OrderComment())==7) Bopen7=True;
               if (StrToInteger(OrderComment())==8) Bopen8=True;
               if (StrToInteger(OrderComment())==9) Bopen9=True;
               if (StrToInteger(OrderComment())==10) Bopen10=True;
               if (StrToInteger(OrderComment())==11) Bopen11=True;
               if (StrToInteger(OrderComment())==12) Bopen12=True;
              }
           }
        }
      for(cnt=OrdersTotal();cnt>=0;cnt--)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         mode=OrderType();
         if (OrderSymbol()==Symbol() && OrderMagicNumber( )==magicEA)
           {
            if (mode==OP_BUY)
              {
               //			OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(),slippage); 
               if (OrderNumberBuy!=OrderTicket())
                 {
                  Comment("Order was closed, exist OrderTicket()="+OrderTicket()+", OrderComment()="+OrderComment());
                  if (((StrToInteger(OrderComment())==2||StrToInteger(OrderComment())==3||StrToInteger(OrderComment())==4)&&!Bopen1&&Bopen2&&Bopen3&&Bopen4)||
                      ((StrToInteger(OrderComment())==6||StrToInteger(OrderComment())==7||StrToInteger(OrderComment())==8)&&!Bopen5&&Bopen6&&Bopen7&&Bopen8)||
                      ((StrToInteger(OrderComment())==10||StrToInteger(OrderComment())==11||StrToInteger(OrderComment())==12)&&!Bopen9&&Bopen10&&Bopen11&&Bopen12))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0);
                    }
                  if (((StrToInteger(OrderComment())==3||StrToInteger(OrderComment())==4)&&!Bopen1&&!Bopen2&&Bopen3&&Bopen4)||
                      ((StrToInteger(OrderComment())==7||StrToInteger(OrderComment())==8)&&!Bopen5&&!Bopen6&&Bopen7&&Bopen8)||
                      ((StrToInteger(OrderComment())==11||StrToInteger(OrderComment())==12)&&!Bopen9&&!Bopen10&&Bopen11&&Bopen12))
                    {
                     if (StrToInteger(OrderComment())==3||StrToInteger(OrderComment())==7||StrToInteger(OrderComment())==11)
                       {
                        OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(OrderTakeProfit()-OrderOpenPrice())/4,OrderTakeProfit(),0);
                       }
                     if (StrToInteger(OrderComment())==4||StrToInteger(OrderComment())==8||StrToInteger(OrderComment())==12)
                       {
                        OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(OrderTakeProfit()-OrderOpenPrice())/5,OrderTakeProfit(),0);
                       }
                    }
                  if ((StrToInteger(OrderComment())==4 && !Bopen1 && !Bopen2 && !Bopen3)||
                      (StrToInteger(OrderComment())==8 && !Bopen5 && !Bopen6 && !Bopen7)||
                      (StrToInteger(OrderComment())==12 && !Bopen9 && !Bopen10 && !Bopen11))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+(OrderTakeProfit()-OrderOpenPrice())/5*2,OrderTakeProfit(),0);
                    }
                 }
               OrderNumberBuy=OrderTicket();
              }
           }
        }
     }
   if (PreviousOpenOrdersSell>OpenOrdersSell)
     {
      for(cnt=OrdersTotal();cnt>=0;cnt--)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         mode=OrderType();
         if (OrderSymbol()==Symbol() && OrderMagicNumber( )==magicEA)
           {
            if (mode==OP_SELL)
              {
               if (StrToInteger(OrderComment())==1) Sopen1=True;
               if (StrToInteger(OrderComment())==2) Sopen2=True;
               if (StrToInteger(OrderComment())==3) Sopen3=True;
               if (StrToInteger(OrderComment())==4) Sopen4=True;
               if (StrToInteger(OrderComment())==5) Sopen5=True;
               if (StrToInteger(OrderComment())==6) Sopen6=True;
               if (StrToInteger(OrderComment())==7) Sopen7=True;
               if (StrToInteger(OrderComment())==8) Sopen8=True;
               if (StrToInteger(OrderComment())==9) Sopen9=True;
               if (StrToInteger(OrderComment())==10) Sopen10=True;
               if (StrToInteger(OrderComment())==11) Sopen11=True;
               if (StrToInteger(OrderComment())==12) Sopen12=True;
              }
           }
        }
      for(cnt=OrdersTotal();cnt>=0;cnt--)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         mode=OrderType();
         if (OrderSymbol()==Symbol() && OrderMagicNumber()==magicEA)
           {
            if (mode==OP_SELL)
              {
               if (OrderNumberSell!=OrderTicket())
                 {
                  if (((StrToInteger(OrderComment())==2||StrToInteger(OrderComment())==3||StrToInteger(OrderComment())==4)&&!Sopen1&&Sopen2&&Sopen3&&Sopen4)||
                      ((StrToInteger(OrderComment())==6||StrToInteger(OrderComment())==7||StrToInteger(OrderComment())==8)&&!Sopen5&&Sopen6&&Sopen7&&Sopen8)||
                      ((StrToInteger(OrderComment())==10||StrToInteger(OrderComment())==11||StrToInteger(OrderComment())==12)&&!Sopen9&&Sopen10&&Sopen11&&Sopen12))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0);
                    }
                  if (((StrToInteger(OrderComment())==3||StrToInteger(OrderComment())==4)&&!Sopen1&&!Sopen2&&Sopen3&&Sopen4)||
                      ((StrToInteger(OrderComment())==7||StrToInteger(OrderComment())==8)&&!Sopen5&&!Sopen6&&Sopen7&&Sopen8)||
                      ((StrToInteger(OrderComment())==11||StrToInteger(OrderComment())==12)&&!Sopen9&&!Sopen10&&Sopen11&&Sopen12))
                    {
                     if (StrToInteger(OrderComment())==3||StrToInteger(OrderComment())==7||StrToInteger(OrderComment())==11)
                       {
                        OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(OrderOpenPrice()-OrderTakeProfit())/4,OrderTakeProfit(),0);
                       }
                     if (StrToInteger(OrderComment())==4||StrToInteger(OrderComment())==8||StrToInteger(OrderComment())==12)
                       {
                        OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(OrderOpenPrice()-OrderTakeProfit())/5,OrderTakeProfit(),0);
                       }
                    }
                  if ((StrToInteger(OrderComment())==4 && !Sopen1 && !Sopen2 && !Sopen3)||
                      (StrToInteger(OrderComment())==8 && !Sopen5 && !Sopen6 && !Sopen7)||
                      (StrToInteger(OrderComment())==12 && !Sopen9 && !Sopen10 && !Sopen11))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-(OrderOpenPrice()-OrderTakeProfit())/5*2,OrderTakeProfit(),0);
                    }
                 }
               OrderNumberSell=OrderTicket();
              }
           }
        }
     }
   PreviousOpenOrdersBuy=OpenOrdersBuy;
   PreviousOpenOrdersSell=OpenOrdersSell;
   //
   if ((DayOfWeek()==2||DayOfWeek()==3||DayOfWeek()==4||DayOfWeek()==5)&& DDay!=DayOfWeek())
     {
      DDay=DayOfWeek();
//----
      if (iOpen(Symbol(),0,1) < iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_HIGH,2)) LastTradeB=" ";
      if (iRSI(Symbol(),0,RSIPeriod,0,1)>=RSIUpperLimit) BlockBuy=True;
      if ((iClose(Symbol(),0,1) > iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_HIGH,2) &&
          iOpen(Symbol(),0,1) < iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_HIGH,2) &&
          iRSI(Symbol(),0,RSIPeriod,0,1)>RSIUpperLevel) ||
         (iClose(Symbol(),0,1) > iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_HIGH,2) &&
          iOpen(Symbol(),0,1) > iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_HIGH,2) &&
          iRSI(Symbol(),0,RSIPeriod,0,1)>RSIUpperLevel) && !BlockBuy && LastTradeB!="BUY" &&
         (iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,2)<0||
         (iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,1)-iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,2))/iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,1)>MacdDiffBuy) )
        {
         BlockSell=False;
         LastTradeB="BUY";
         for(int i=0;i<4;i++)
           {
            dd=i;
            InitialStop=ism*iATR(Symbol(),0,ATRPeriod,1);
            TakeProfit=tpm*(dd/2+1)*iATR(Symbol(),0,ATRPeriod,1);
            OrderSend(Symbol(),OP_BUY,Lots,Ask,slippage,Ask-InitialStop,Ask+TakeProfit,DoubleToStr(oob+1+i,2),magicEA,0);
           }
         oob=oob+4;
        }
      if (iOpen(Symbol(),0,1) > iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_LOW,2)) LastTradeS=" ";
      if (iRSI(Symbol(),0,RSIPeriod,0,1)<=RSILowerLimit) BlockSell=True;
      if ((iClose(Symbol(),0,1) < iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_LOW,2) &&
          iOpen(Symbol(),0,1) > iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_LOW,2) &&
          iRSI(Symbol(),0,RSIPeriod,0,1)<RSILowerLevel) ||
         (iClose(Symbol(),0,1) < iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_LOW,2) &&
          iOpen(Symbol(),0,1) < iMA(Symbol(),0,MAPeriod,0,MODE_EMA,PRICE_LOW,2) &&
          iRSI(Symbol(),0,RSIPeriod,0,1)<RSILowerLevel) && !BlockSell && LastTradeS!="SELL"&&
         (iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,2)>0||
         (iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,1)-iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,2))/iMACD(NULL,0,FastMAPeriod,SlowMAPeriod,SignalMAPeriod,PRICE_CLOSE,MODE_EMA,1)>MacdDiffSell))
        {
         BlockBuy=False;
         LastTradeS="SELL";
         for(i=0;i<4;i++)
           {
            dd=i;
            InitialStop=ism*iATR(Symbol(),0,ATRPeriod,1);
            TakeProfit=tpm*(dd/2+1)*iATR(Symbol(),0,ATRPeriod,1);
            OrderSend(Symbol(),OP_SELL,Lots,Bid,slippage,Bid+InitialStop,Bid-TakeProfit,DoubleToStr(oos+1+i,2),magicEA,0);
           }
         oos=oos+4;
        }
     }
  }
//+------------------------------------------------------------------+