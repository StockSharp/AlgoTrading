//+------------------------------------------------------------------+
//|                                                        Genie.mq4 |
//|                                                   Genaro Gravoso |
//|                          http://www.oocities.org/ggravoso/b.html |
//+------------------------------------------------------------------+
#property copyright "Genaro Gravoso"
#property link      "http://www.oocities.org/ggravoso/b.html"
#property description "Parabolic SAR expert advisor"
#property version   "1.01"
#property strict

#define MAGICMA  20140730
//--- Inputs
input double TakeProfit    =500;
input double Lots          =0.01;
input double TrailingStop  =200;
input double MaximumRisk   =0.02;
input double DecreaseFactor=3;
input double Step          =0.02;  // Acceleration Factor
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
   double Previous,Current,PDI1,PDI,MDI1,MDI,ADX;
   int    ticket,total;
//--- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
//--- to simplify the coding and speed up access data are put into internal variables
   Current=iSAR(NULL,0,Step,0.2,0);
   Previous=iSAR(NULL,0,Step,0.2,1);
   ADX=iADX(NULL,0,14,PRICE_CLOSE,MODE_MAIN,0);
   PDI=iADX(NULL,0,14,PRICE_CLOSE,MODE_PLUSDI,0);
   PDI1=iADX(NULL,0,14,PRICE_CLOSE,MODE_PLUSDI,1);
   MDI=iADX(NULL,0,14,PRICE_CLOSE,MODE_MINUSDI,0);
   MDI1=iADX(NULL,0,14,PRICE_CLOSE,MODE_MINUSDI,1);
   
   total=OrdersTotal();
   if(total<3)
     {
      //--- no opened orders identified
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ",AccountFreeMargin());
         return;
        }
//--- sell conditions
      if(Previous<Close[1] && Current>Close[0] && PDI1>MDI1 && PDI<MDI && ADX>PDI && ADX>MDI)
        {
         ticket=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,Bid-TakeProfit*Point,"Genie",MAGICMA,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
               Print("SELL order opened: ",OrderOpenPrice());
           }
         else
            Print("Error opening SELL order: ",GetLastError());
         return;
        }
//--- buy conditions
      if(Previous>Close[1] && Current<Close[0] && PDI1<MDI1 && PDI>MDI && ADX>PDI && ADX>MDI)
        {
         ticket=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,Ask+TakeProfit*Point,"Genie",MAGICMA,0,Blue);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))
               Print("BUY order opened: ",OrderOpenPrice());
           }
         else
            Print("Error opening BUY order: ",GetLastError());
         return;
        }
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
         if(OrderStopLoss()<Bid-Point*TrailingStop || OrderStopLoss()==0)
           {
            //--- modify order and exit
            if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green))
               Print("OrderModify error ",GetLastError());
            return;
           }
         if(Bid-OrderOpenPrice()>Ask-Bid && Open[1]>Close[1])
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Bid,1,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if(OrderStopLoss()>Ask+Point*TrailingStop || OrderStopLoss()==0)
           {
            //--- modify order and exit
            if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red))
               Print("OrderModify error ",GetLastError());
            return;
           }
         if(OrderOpenPrice()-Ask>Ask-Bid && Open[1]<Close[1])
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Ask,1,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
     }
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