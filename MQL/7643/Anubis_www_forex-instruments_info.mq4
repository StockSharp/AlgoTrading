//+------------------------------------------------------------------+
//|                       "Anubis".mq4                               | 
//|                       compiled for                               |
//|                                                                  | 
//|                        Automated                                 | 
//|                 Trading Championship                             |
//|                           2006                                   | 
//+------------------------------------------------------------------+ 
#property copyright "Copyright c 2006, Andrew Tikhonov"
#include <stdlib.mqh>
//----
#define          TRENDMAGIC   23061234
#define          FLATMAGIC    23065678
#define          UNDEF   0
#define          LONG    1
#define          SHORT  -1
//---- input parameters 
extern string    expName   ="Anubis";
extern double    Lots      =1;
extern double    CCIthres  =80; // 82
extern int       CCIPeriod =11; // 12
extern int       Stoploss  =100;
extern int       breakeven =65;
extern int       M_FastEMA =20; // 24
extern int       M_ShowEMA =50; // 26
extern int       M_Signal  =2;
extern double    RFactor   =0.6;
//----
int       shortOrders=2;
int       longOrders =2;
//----
extern double    closeK    =2; // 4.5 // 3.5
extern int       thres     =28; // 25
extern double    stdK      =2.9; // 3 // 6.2
//----
int openLongBar=0;
int openShortBar=0;
//----
double lastLongPrice;
double lastShortPrice;
//----
datetime expTime=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void init() 
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool numOrders(int & longs, int & shorts)
  {
   int i;
//----
   longs =0;
   shorts=0;
     for(i=OrdersTotal()-1; i>=0; i--) 
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        if(OrderMagicNumber()==FLATMAGIC) 
        {
         if (OrderType()==OP_BUY || OrderType()==OP_BUYLIMIT) { longs++; }
         if (OrderType()==OP_SELL || OrderType()==OP_SELLLIMIT) { shorts++; }
        }
     }
  } 
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void setFlatBreakeven() 
  {
   int i;
   if (OrdersTotal()==0) return;
   for(i=OrdersTotal()-1; i>=0; i--)
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()==OP_BUY && OrderMagicNumber()==FLATMAGIC)
        {
         if (Bid-breakeven*Point>OrderOpenPrice() && OrderOpenPrice()>OrderStopLoss())
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Aqua);
            return;
           }
        }
      if(OrderType()==OP_SELL && OrderMagicNumber()==FLATMAGIC)
        {
         if (Ask+breakeven*Point<OrderOpenPrice() && OrderOpenPrice()<OrderStopLoss())
           {
            OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Magenta);
            return;
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double calculateLoss()
  {
   if (OrderSelect(HistoryTotal()-1, SELECT_BY_POS, MODE_HISTORY))
     {
      if (OrderProfit() < 0) { return(RFactor); }
     }
   return(1.0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double getLots()
  {
   double lotsize =MarketInfo(Symbol(),MODE_LOTSIZE);
   double leverage=AccountLeverage();
   double minlot  =MarketInfo(Symbol(),MODE_MINLOT);
   double percent =0.15;
   double factor  =calculateLoss();
   double mylots  =Lots;
   double moneyAvailable=AccountBalance();
   double opt_lots=(percent * moneyAvailable)/(Ask * lotsize/leverage);
//----
   opt_lots=NormalizeDouble(opt_lots, 1);
   if (opt_lots < Lots){ Print (" --- OPTIMIZED LOTS --- "+opt_lots); mylots=opt_lots; }
   else if (AccountBalance() > 22000) { mylots=mylots * 3.2; }
      else if (AccountBalance() > 14000) { mylots=mylots * 2; }
//----
   return(NormalizeDouble(mylots * factor, 1));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool checkFreeMargin()
  {
   double lotsize =MarketInfo(Symbol(),MODE_LOTSIZE);
   double leverage=AccountLeverage();
   double minlot  =MarketInfo(Symbol(),MODE_MINLOT);
//----
     if (AccountFreeMargin() < (Ask * lotsize/leverage * minlot)) 
     {
      Print ("--- NOT ENOUGH MONEY ---");
      return(false);
     }
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start_flatSystem()
  {
   if (expTime!=Time[0]) { expTime=Time[0]; }
   else 
   {
    return; 
   }
   if (!checkFreeMargin()) 
   {
    return(0); 
   }
   if (!IsConnected()) { Print("not connected!"); }
   if (IsStopped()) { Print("stopped!"); }
   if (!IsTradeAllowed()) { Print("Trade NOT allowed!"); }
   if (IsTradeContextBusy()) { Print("Trade context busy!"); }
//----
   int i;
   double iCCI0;
   double iMACD1, iMACD2, iMACDs1, iMACDs2;
//----
   int openLong =0;
   int openShort=0;
   int openCmd=UNDEF;
   int numLongs;
   int numShorts;
//----
   numOrders(numLongs, numShorts);
   double stDev=iStdDev(Symbol(),PERIOD_H4,20,0,MODE_SMA,PRICE_CLOSE,1);
   iCCI0 =iCCI(Symbol(), PERIOD_H4, CCIPeriod, PRICE_CLOSE, 0);
   iMACD1 =iMACD(Symbol(),PERIOD_M15,M_FastEMA,M_ShowEMA,M_Signal,PRICE_CLOSE,MODE_MAIN,1);
   iMACD2 =iMACD(Symbol(),PERIOD_M15,M_FastEMA,M_ShowEMA,M_Signal,PRICE_CLOSE,MODE_MAIN,2);
   iMACDs1=iMACD(Symbol(),PERIOD_M15,M_FastEMA,M_ShowEMA,M_Signal,PRICE_CLOSE,MODE_SIGNAL,1);
   iMACDs2=iMACD(Symbol(),PERIOD_M15,M_FastEMA,M_ShowEMA,M_Signal,PRICE_CLOSE,MODE_SIGNAL,2);
   if (iCCI0 > CCIthres && IsTesting())
     {
      // ObjectCreate("UP"+Bars,OBJ_ARROW,0,Time[1],Low[1]-iATR(NULL,0,12,1));
      // ObjectSet("UP"+Bars,OBJPROP_ARROWCODE,159);
      // ObjectSet("UP"+Bars,OBJPROP_COLOR,Red);
     }
   if (iCCI0 < (-1)*CCIthres && IsTesting())
     {
      // ObjectCreate("DN"+Bars,OBJ_ARROW,0,Time[1],High[1]+iATR(NULL,0,12,1));
      // ObjectSet("DN"+Bars,OBJPROP_ARROWCODE,159);
      // ObjectSet("DN"+Bars,OBJPROP_COLOR,Blue);
     }
   double take;
   //double take= TakeProfit*Point;
   take= stdK * iStdDev(Symbol(),PERIOD_H4,30,MODE_EMA,0,PRICE_CLOSE,1);
//----
   if(iCCI0 > CCIthres && iMACD2>=iMACDs2 && iMACD1 < iMACDs1 && iMACD1 > 0)
   { openCmd=SHORT; }
   if (iCCI0 < (-1)*CCIthres && iMACD2<=iMACDs2 && iMACD1 > iMACDs1 && iMACD1 < 0)
   { openCmd=LONG; }
   if (numShorts==0) { lastShortPrice=0 ;}
   if (numLongs ==0) { lastLongPrice=0 ;}
   double myprice;
   if (openCmd==SHORT && openShortBar!=Bars && numShorts < shortOrders)
     {
      if (MathAbs(Ask-lastShortPrice) > 20*Point)
        {
         RefreshRates();
         OrderSend(Symbol(),OP_SELL,getLots(),Bid,3,Bid+Stoploss*Point,Bid-take,expName,FLATMAGIC,0,Blue);
         openShortBar=Bars;
         lastShortPrice=Ask;
        }
     }
   if (openCmd==LONG && openLongBar!=Bars && numLongs < longOrders)
     {
      if (MathAbs(Ask-lastLongPrice) > 20*Point)
        {
         RefreshRates();
//----
         OrderSend(Symbol(),OP_BUY,getLots(),Ask,3,Ask-Stoploss*Point,Ask+take,expName,FLATMAGIC,0,Red);
         openLongBar=Bars;
         lastLongPrice=Ask;
        }
     }
   setFlatBreakeven();
   if (OrdersTotal() > 0)
     {
      double iATR1=iATR(Symbol(),PERIOD_M15,12,1);
      double iClose1=iClose(Symbol(),PERIOD_M15,1);
      double iOpen1=iClose(Symbol(),PERIOD_M15,1);
      //
      for(i=OrdersTotal()-1; i>=0; i--)
        {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if(OrderMagicNumber()==FLATMAGIC)
           {
            if(OrderType()==OP_BUY)
              {
               if((iClose1 - iOpen1 > closeK * iATR1) ||
                  (iMACD1 < iMACD2 && Bid - OrderOpenPrice() > thres*Point))
                  OrderClose(OrderTicket(),OrderLots(),Bid,3,Green);
              }
            if(OrderType()==OP_SELL)
              {
               if((iOpen1 - iClose1 > closeK * iATR1) ||
                  (iMACD1 > iMACD2 && OrderOpenPrice() - Ask > thres*Point ))
                  OrderClose(OrderTicket(),OrderLots(),Ask,3,Green);
              }
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   start_flatSystem();
   return(0);
  }
//+------------------------------------------------------------------+