//+------------------------------------------------------------------+
//|                                                        vlado.mq4 |
//|                Copyright © 2005, Nick Bilak, beluck[AT]gmail.com |
//|                                    http://metatrader.50webs.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, Nick Bilak"
#property link      "http://metatrader.50webs.com/"
//----
#define MAGICMA  20050716
//----
extern int    slippage=5;      //slippage for market order processing
extern int    shift=0;         //shift to current bar, 
extern double Lots=0.1;
extern double MaximumRisk=10;
extern int mm=0;
extern int       WPRLen=100;
extern int       WPRLevel=-50;
//----
bool buysig,sellsig; int lastsig;
//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//---- return orders volume
   if(buys>0) return(buys);
   else       return(-sells);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   if (mm!=0)
      lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/100000.0,1);
   else
      lot=Lots;
//---- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
  void CheckForSignals() 
  {
   double wpr=iWPR(NULL,0,WPRLen,shift);
     if (wpr > WPRLevel) 
     {
      buysig=true;
      sellsig=false;
     }
     if (wpr < WPRLevel) 
     {
      sellsig=true;
      buysig=false;
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
  void CheckForOpen() 
  {
   int    res;
//---- sell conditions
     if(sellsig && lastsig!=-1)  
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,slippage,0,0,"vlado",MAGICMA,0,Red);
      lastsig=-1;
      return;
     }
//---- buy conditions
     if(buysig && lastsig!=1)  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,slippage,0,0,"vlado",MAGICMA,0,Blue);
      lastsig=1;
      return;
     }
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
  void CheckForClose()  
  {
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)  break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if (sellsig) OrderClose(OrderTicket(),OrderLots(),Bid,slippage,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if (buysig) OrderClose(OrderTicket(),OrderLots(),Ask,slippage,White);
         break;
        }
     }
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
  void start()  
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;
//---- check for signals
   CheckForSignals();
//---- calculate open orders by current symbol
   if (CalculateCurrentOrders(Symbol())==0)
      CheckForOpen();
   else
      CheckForClose();
  }
//+------------------------------------------------------------------+