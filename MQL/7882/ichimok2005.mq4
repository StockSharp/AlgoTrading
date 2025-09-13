//+------------------------------------------------------------------+
//|                                                                  |
//|                 Copyright © 1999-2007, MetaQuotes Software Corp. |
//|                                         http://www.metaquotes.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2005, ForexProf"
//----
#define MAGICNUM  112116
//----
extern double    StopLoss=30;
extern double    TakeProfit=60;
extern int       slippage=0;
extern int       shift=1;
extern double    Lots=0.1;
extern double    MaximumRisk=10;
extern int       mm=0;
extern int       Tenkan=9;
extern int       Kijun=26;
extern int       Senkou=52;
//----
bool buysig,sellsig; int lastsig;
//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//----
   int total=OrdersTotal();
   for(int i=0;i<total;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICNUM)
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
   int i=1;
     while(i<=Bars-1) 
     {
      double chinkou1=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_CHINKOUSPAN,i);
      double chinkou2=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_CHINKOUSPAN,i+1);
      if ((chinkou1>=Open[1] && chinkou2<=Open[1]) || (chinkou1<=Open[1] && chinkou2>=Open[1]))
         break;
      i++;
     }
   double senkouAi=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_SENKOUSPANA,i);
   double senkouBi=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_SENKOUSPANA,i);
   double senkouB1=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_SENKOUSPANB,shift);
   double senkouB2=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_SENKOUSPANB,shift+1);
   double senkouB3=iIchimoku(NULL,0,Tenkan,Kijun,Senkou,MODE_SENKOUSPANB,shift+2);
//----
   if(Open[shift+2]<senkouB3 && Open[shift+1] > senkouB2 && Close[shift+1] > senkouB2 &&
        Open[shift] > senkouB1 && Close[shift] > senkouB1 &&
        Close[shift+1]>Open[shift+1] && Close[shift]>Open[shift] &&
            !(chinkou1<=senkouBi && chinkou1>=senkouAi && senkouAi<senkouBi))
            {
      buysig=true;
      sellsig=false;
     }
   if(Open[shift+2]>senkouB3 && Open[shift+1] < senkouB2 && Close[shift+1] < senkouB2 &&
        Open[shift] < senkouB1 && Close[shift] < senkouB1 &&
        Close[shift+1]<Open[shift+1] && Close[shift]<Open[shift] &&
            !(chinkou1<=senkouBi && chinkou1>=senkouAi && senkouAi<senkouBi))
            {
      buysig=false;
      sellsig=true;
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
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,slippage,Bid+StopLoss*Point,Bid-TakeProfit*Point,"ichim",MAGICNUM,0,Red);
      lastsig=-1;
      return;
     }
//---- buy conditions
     if(buysig && lastsig!=1)  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,slippage,Ask-StopLoss*Point,Ask+TakeProfit*Point,"ichim",MAGICNUM,0,Blue);
      lastsig=1;
      return;
     }
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
  void CheckForClose()  
  {
   int total=OrdersTotal();
   for(int i=0;i<total;i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)  break;
      if(OrderMagicNumber()!=MAGICNUM || OrderSymbol()!=Symbol()) continue;
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