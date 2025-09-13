//+------------------------------------------------------------------+
//|                                                        Genie.mq4 |
//|                                                   Genaro Gravoso |
//|                          http://www.oocities.org/ggravoso/b.html |
//+------------------------------------------------------------------+
#property copyright "Genaro Gravoso"
#property link      "http://www.oocities.org/ggravoso/b.html"
#property description "Relative Strength Index Stochastic expert advisor"
#property version   "1.00"
#property strict

#define MAGICMA  20140801
//--- Inputs
input double TakeProfit    =500;
input double Lots          =0.1;
input double TrailingStop  =200;
input double MaximumRisk   =0.02;
input double DecreaseFactor=3;
//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
//--- return orders volume
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
//--- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,1);
//--- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false)
           {
            Print("Error in history!");
            break;
           }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL)
            continue;
         //---
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1)
         lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
//--- return lot size
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double RSI,StochasticPrevious,Stochastic,SignalPrevious,Signal;
   int    res;
//--- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//--- to simplify the coding and speed up access data are put into internal variables
   RSI=iRSI(NULL,0,14,PRICE_CLOSE,0);
   Stochastic=iStochastic(NULL,0,5,3,3,MODE_SMA,STO_LOWHIGH,MODE_MAIN,0);
   StochasticPrevious=iStochastic(NULL,0,5,3,3,MODE_SMA,STO_LOWHIGH,MODE_MAIN,1);
   Signal=iStochastic(NULL,0,5,3,3,MODE_SMA,STO_LOWHIGH,MODE_SIGNAL,0);
   SignalPrevious=iStochastic(NULL,0,5,3,3,MODE_SMA,STO_LOWHIGH,MODE_SIGNAL,1);
//--- sell conditions
   if(RSI>70 && Stochastic>80 && StochasticPrevious>SignalPrevious && Stochastic<Signal)
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,Bid-TakeProfit*Point,"Genie",MAGICMA,0,Red);
      return;
     }
//--- buy conditions
   if(RSI<30 && Stochastic<20 && StochasticPrevious<SignalPrevious && Stochastic>Signal)
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,Ask+TakeProfit*Point,"Genie",MAGICMA,0,Blue);
      return;
     }
//---
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
//--- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      //--- check order type 
      if(OrderType()==OP_BUY)
        {
         if(OrderStopLoss()<Bid-Point*TrailingStop)
           {
            //--- modify order and exit
            if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green))
               Print("OrderModify error ",GetLastError());
            return;
           }
         if(Open[1]>Close[1] && Bid-OrderOpenPrice()>Ask-Bid)
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Bid,3,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
           {
            //--- modify order and exit
            if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red))
               Print("OrderModify error ",GetLastError());
            return;
           }
         if(Open[1]<Close[1] && OrderOpenPrice()-Ask>Ask-Bid)
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Ask,3,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
     }
//---
  }
//+------------------------------------------------------------------+
//| OnTick function                                                  |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false)
      return;
//--- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//---
  }
//+------------------------------------------------------------------+