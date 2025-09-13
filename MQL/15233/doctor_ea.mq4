//+------------------------------------------------------------------+
//|                                                       Doctor.mq4 |
//|                        Copyright 2016, MetaQuotes Software Corp. |
//|                                   https://M2P_Design@hotmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, MetaQuotes Software Corp."
#property link      "https://M2P_Design@hotmail.com"
#property version   "1.00"
#property strict

input double    LotSize = 0.01;
input int      StopLoss = 70;
input int    TakeProfit = 40;
input int         Magic = 280456;
input bool TrailingStop = true;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

//---
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
//---
   double MyPoint=Point;
   if(Digits==3 || Digits==5) MyPoint=Point*10;
   double    Lot=Lots();

//--------------------Trailing Stop
   if(TrailingStop) TrStop();
//--------------------Close Orders
   CloseTrd();

//--------------------Buying Signals
   if(Slope()==2 && MALinear()==2 && RSI5()==2 && CountB()==0)
     {
      double   Loss = Ask-(StopLoss*MyPoint);
      double Profit = Ask+(TakeProfit*MyPoint);
      int    Buy = OrderSend(Symbol(),OP_BUY,Lot,Ask,3,Loss,Profit,"Doctor Bola",Magic,0,clrAliceBlue);
     }

//--------------------Selling Signals
   else if(Slope()==1 && MALinear()==1 && RSI5()==1 && CountS()==0)
     {
      double   Loss = Bid+(StopLoss*MyPoint);
      double Profit = Bid-(TakeProfit*MyPoint);
      int   Sell = OrderSend(Symbol(),OP_SELL,Lot,Bid,3,Loss,Profit,"Doctor Bola",Magic,0,clrRed);
     }
  }
//+-------------------------- My Expert -----------------------------+
//+------------------------------------------------------------------+
//| Close Orders function                                            |
//+------------------------------------------------------------------+
int CloseTrd()
  {

   if(OrdersTotal()>0)
     {
      for(int i=0; i<=OrdersTotal(); i++)
        {
         bool OS=OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
         if(OS==true && OrderSymbol()==Symbol() && OrderMagicNumber()==280456)
           {
            if(OrderType()==OP_BUY && Slope()==1 && (MALinear()==1 || RSI5()==1 || PSAR()==2))
               bool OCB=OrderClose(OrderTicket(),OrderLots(),Bid,3,clrAzure);
            if(OrderType()==OP_SELL && Slope()==2 && (MALinear()==2 || RSI5()==2 || PSAR()==1))
               bool OCS=OrderClose(OrderTicket(),OrderLots(),Ask,3,clrAzure);
           }

        }
     }
   return(-1);
  }
//+------------------------------------------------------------------+
//| Lot Size function                                                |
//+------------------------------------------------------------------+
double Lots()
  {
   double Lot = LotSize;
   double Min = MarketInfo(Symbol(),MODE_MINLOT);
   double Max = MarketInfo(Symbol(),MODE_MAXLOT);
   if(LotSize<Min) Lot=Min;
   else if(LotSize>Max) Lot=Max;

   return (Lot);
  }
//+------------------------------------------------------------------+
//| Trailing Stop function                                           |
//+------------------------------------------------------------------+
int TrStop()
  {
   double MyPoint=Point;
   if(Digits==3|| Digits==5) MyPoint=Point*10;
   double TR= StopLoss*MyPoint;

   for(int cnt=0; cnt<=OrdersTotal(); cnt++)
     {
      bool OS=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OS==true && OrderType()==OP_BUY && Bid>(OrderOpenPrice()+TR/2) && OrderStopLoss()<Bid-TR)
         bool OrdModb=OrderModify(OrderTicket(),OrderOpenPrice(),Bid-TR,OrderTakeProfit(),0,clrNONE);
      else if(OS==true && OrderType()==OP_SELL && Ask<(OrderOpenPrice()-TR/2) && OrderStopLoss()>Ask+TR)
         bool OrdModb=OrderModify(OrderTicket(),OrderOpenPrice(),Ask+TR,OrderTakeProfit(),0,clrNONE);

      else return(-1);
     }

   return(0);
  }
//+------------------------------------------------------------------+
//| Count Buy Orders function                                        |
//+------------------------------------------------------------------+
int CountB()
  {
   int i=0;
   for(int cnt=0; cnt<=OrdersTotal(); cnt++)
     {
      bool OS=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OS==true && OrderSymbol()==Symbol() && OrderMagicNumber()==280456 && OrderType()==OP_BUY)
         i++;
     }
   return(i);
  }
//+------------------------------------------------------------------+
//| Count Sell Orders function                                       |
//+------------------------------------------------------------------+
int CountS()
  {
   int i=0;
   for(int cnt=0; cnt<=OrdersTotal(); cnt++)
     {
      bool OS=OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OS==true && OrderSymbol()==Symbol() && OrderMagicNumber()==280456 && OrderType()==OP_SELL)
         i++;
     }
   return(i);
  }
//--------------------------- Indicators -----------------------------
//+------------------------------------------------------------------+
//| Slope Direction Line function                                    |
//+------------------------------------------------------------------+
int Slope()
  {
   double Up = iCustom(Symbol(),30,"slope-direction-line",40,3,3,0,0);
   double Dn = iCustom(Symbol(),30,"slope-direction-line",40,3,3,1,0);

   if(Up<Dn) return(1);      // Slope Direction Blue
   if(Up>Dn) return(2);      // Slope Direction Red

   else return(-1);
  }
//+------------------------------------------------------------------+
//| Moving Average function                                          |
//+------------------------------------------------------------------+
int MALinear()
  {
   double MALW1 = iMA(Symbol(),30,400,0,MODE_LWMA,PRICE_CLOSE,1);
   double MALW2 = iMA(Symbol(),30,400,0,MODE_LWMA,PRICE_CLOSE,2);
   double MALW3 = iMA(Symbol(),30,400,0,MODE_LWMA,PRICE_CLOSE,3);

// Moving Average Below Bars
   if(MALW1<Low[1]  && MALW2<Low[2]  && MALW3<Low[3])   return(1);

// Moving Average Above Bars
   if(MALW1>High[1] && MALW2>High[2] && MALW3>High[3])  return(2);

   else return(-1);
  }
//+------------------------------------------------------------------+
//| RSI function                                                     |
//+------------------------------------------------------------------+
int RSI5()
  {
   double RSIndex1 = iRSI(Symbol(),30,14,PRICE_CLOSE,1);
   double RSIndex2 = iRSI(Symbol(),30,5,PRICE_CLOSE,1);

   if(RSIndex1<50 && RSIndex2>RSIndex1) return(1);
   if(RSIndex1>50 && RSIndex2<RSIndex1) return(2);

   else return(-1);
  }
//+------------------------------------------------------------------+
//| SAR function                                                     |
//+------------------------------------------------------------------+
int PSAR()
  {
   double ParSar=iSAR(Symbol(),30,0.02,0.2,0);

   if(ParSar<=Low[0])  return(1);
   if(ParSar>=High[0]) return(2);

   else return(-1);
  }
//+------------------------------------------------------------------+
