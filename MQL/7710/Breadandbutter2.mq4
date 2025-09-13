//+------------------------------------------------------------------+
//|                 Breadandbutter2                                  |
//|                 Copyright © 1999-2007, MetaQuotes Software Corp. |
//|                 http://www.metaquotes.ru                         |
//+------------------------------------------------------------------+
#property copyright "Ron Thompson"
#property link      "http://www.lightpatch.com/forex"
// User Input
extern double        Lots=0.1 ;
extern int           TakeProfit=20   ;
extern int           StopLoss=20   ;
extern int           Interval=4   ;
extern double        crossfilter=1.1 ;
// Global scope
double      barmove0=0;
double      barmove1=0;
int         itv=0;
//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//|------------------------------------------------------------------|
int init()
  {
   itv=Interval;
   return(0);
  }
//+------------------------------------------------------------------+
//| Custor indicator deinitialization function                       |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int start()
  {
   bool      found=false;
   bool      rising=false;
   bool      falling=false;
   bool      cross=false;
//----
   double slA=0, slB=0, tpA=0, tpB=0;
   double p=Point;
   double wma5, wma10, wma15, adx14;
   double Pwma5, Pwma10, Pwma15, Padx14;
   int      cnt=0;
   // Error checking
   if(AccountFreeMargin()<(1000*Lots))        {Print("-----NO MONEY"); return(0);}
   if(Bars<100)                               {Print("-----NO BARS "); return(0);}
   // if(barmove0==Open[0] && barmove1==Open[1]) {                        return(0);}
   // bars moved, update current position
   // barmove0=Open[0];
   // barmove1=Open[1];
   // interval (bar) counter
   // used to pyramid orders during trend
   // itv++;
   // since the bar just moved
   // calculate TP and SL for (B)id and (A)sk
   tpA=Ask+(p*TakeProfit);
   slA=Ask-(p*StopLoss);
   tpB=Bid-(p*TakeProfit);
   slB=Bid+(p*StopLoss);
   //----
   if (TakeProfit<=0) {tpA=0; tpB=0;}
   if (StopLoss<=0)   {slA=0; slB=0;}
   // Hourly chart
   // 3 WMA's 5/10/15
   // ADX (14)
   wma5= iMA(Symbol(),PERIOD_H1, 5,0,PRICE_OPEN,MODE_LWMA,0);
   wma10=iMA(Symbol(),PERIOD_H1,10,0,PRICE_OPEN,MODE_LWMA,0);
   wma15=iMA(Symbol(),PERIOD_H1,15,0,PRICE_OPEN,MODE_LWMA,0);
   adx14=iADX(Symbol(),PERIOD_H1,14,PRICE_OPEN,0,0);
   // and then the Previous bar for movement calcs
   Pwma5= iMA(Symbol(),PERIOD_H1, 5,0,PRICE_OPEN,MODE_LWMA,1);
   Pwma10=iMA(Symbol(),PERIOD_H1,10,0,PRICE_OPEN,MODE_LWMA,1);
   Pwma15=iMA(Symbol(),PERIOD_H1,15,0,PRICE_OPEN,MODE_LWMA,1);
   Padx14=iADX(Symbol(),PERIOD_H1,14,PRICE_OPEN,0,1);
   // is it crossing zero up or down
   if (Pwma5<Pwma10<Pwma15 && wma5>wma10>wma15) { rising=true;  cross=true;}
   if (Pwma5>Pwma10>Pwma15 && wma5<wma10<wma15) { falling=true; cross=true;}
   //if (Pwma5<Pwma15 && wma5>wma15) { rising=true;  cross=true;}
   //if (Pwma5>Pwma15 && wma5<wma15) { falling=true; cross=true;}
   for(cnt=OrdersTotal();cnt>0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol())
        {
         return(0);
        }
     }
   // close then open orders based on cross
   // pyramid below based on itv
   if (cross)
     {
      // Close ALL the open orders 
      for(cnt=OrdersTotal();cnt>0;cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol())
           {
            if (OrderType()==0) {OrderClose(OrderTicket(),Lots,Bid,3,White);}
            if (OrderType()==1) {OrderClose(OrderTicket(),Lots,Ask,3,Red);}
            itv=0;
           }
        }
      // Open new order based on direction of cross
      if (rising)  OrderSend(Symbol(),OP_BUY,Lots,Ask,3,slA,tpA,"ZZZ100",11123,0,White);
      if (falling) OrderSend(Symbol(),OP_SELL,Lots,Bid,3,slB,tpB,"ZZZ100",11321,0,Red);
      // clear the interval counter
      itv=0;
      return(0);
     }
   // Only pyramid if order already open
   found=false;
   for(cnt=0;cnt<OrdersTotal();cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol()==Symbol())
        {
         if (OrderType()==0)  //BUY
           {
            if (itv>=Interval)
              {
               OrderSend(Symbol(),OP_BUY,Lots,Ask,3,slA,tpA,"ZZZ100",11123,0,White);
               itv=0;
              }
           }
         if (OrderType()==1)  //SELL
           {
            if (itv>=Interval)
              {
               OrderSend(Symbol(),OP_SELL,Lots,Bid,3,slB,tpB,"ZZZ100",11321,0,Red);
               itv=0;
              }
           }
         found=true;
         break;
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+