//+------------------------------------------------------------------+
//|                                            ZigZagEvgeTrofi 1.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Trofimov Evgeniy"
#property link      "http://www.fracpar.narod.ru/"

#define MAGICMA  20080919
#define Slippage 5

//---- indicator parameters
extern int ExtDepth=17; //10-21 !15 !17
extern int ExtDeviation=7; //!7
extern int ExtBackstep=5;  //!5
extern double Lot=0.10;
//----
static int prevtime = 0;
int Urgency=2;
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   int i, candle;
   double ZigZag;
   int CurrentCondition, Signal;
   
   if (! IsTradeAllowed()) {
      prevtime = Time[1];
      MathSrand(TimeCurrent());
      Sleep(30000 + MathRand());
      return(0);
   }
   if (Time[0] == prevtime) return(0);
   prevtime = Time[0];  
   while(candle<100)
     {
      ZigZag=iCustom(NULL,0,"ZigZag",   ExtDepth,ExtDeviation,ExtBackstep,   0,candle);
      if(ZigZag!=0) break;
      candle++;
     }
   if(candle>99) return(0);
   if(ZigZag==High[candle])
      Signal=1;
   else if(ZigZag==Low[candle])
      Signal=2;
   int total = OrdersTotal();
   for (i = 0; i < total; i++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if (OrderSymbol() == Symbol() && OrderMagicNumber() == MAGICMA) {
         CurrentCondition=OrderType()+1;
         break;
      } 
   }
   
   if(Signal==CurrentCondition) return(0);
   if(Signal!=CurrentCondition && CurrentCondition>0)
     {
      if(CurrentCondition==1)
         OrderClose(OrderTicket(),OrderLots(),Bid,Slippage);
      else
         OrderClose(OrderTicket(),OrderLots(),Ask,Slippage);
      Sleep(20000);
      RefreshRates();
     }

   if(candle<=Urgency)
     {
      if(Signal==1)
         OrderSend(Symbol(), OP_BUY, Lot, Ask, Slippage, 0, 0, "ZigZag", MAGICMA, 0, Blue);
      else
         OrderSend(Symbol(), OP_SELL, Lot, Bid, Slippage, 0, 0, "ZigZag", MAGICMA, 0, Red);
      Sleep(20000);
      prevtime = Time[1];
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+