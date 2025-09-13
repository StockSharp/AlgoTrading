//+------------------------------------------------------------------+
//|                                                        Genie.mq4 |
//|                                                   Genaro Gravoso |
//|                          http://www.oocities.org/ggravoso/b.html |
//+------------------------------------------------------------------+
#property copyright "Genaro Gravoso"
#property link      "http://www.oocities.org/ggravoso/b.html"
#property description "Pivot Point Reversal scalping expert advisor"
#property version   "1.00"
#property strict

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
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==0)
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
   int    total,res;
//--- go trading only for first tiks of new bar
   if(Volume[0]>1) return;
   total=OrdersTotal();
   if(total<1)
     {
      //--- buy conditions
      if(Low[7]>Low[6] && Low[6]>Low[5] && Low[5]>Low[4] && Low[4]>Low[3] && Low[3]>Low[2] && 
         Low[2]>Low[1] && Low[1]<Low[0] && High[1]<Close[0])
        {
         res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,Ask+TakeProfit*Point,"Genie",0,0,Blue);
         return;
        }
      //--- sell conditions
      if(High[7]<High[6] && High[6]<High[5] && High[5]<High[4] && High[4]<High[3] && High[3]<High[2] && 
         High[2]<High[1] && High[1]>High[0] && Low[1]>Close[0])
        {
         res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,Bid-TakeProfit*Point,"Genie",0,0,Red);
         return;
        }
     }
//---
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   if(Volume[0]>1) return;
//---
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderMagicNumber()!=0 || OrderSymbol()!=Symbol()) continue;
      //--- check order type 
      if(OrderType()==OP_BUY)
        {
         if(Bid-OrderOpenPrice()>Point*TrailingStop)
           {
            if(OrderStopLoss()<Bid-Point*TrailingStop)
              {
               if(!OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green))
                  Print("OrderModify error ",GetLastError());
               return;
              }
           }
         if(Bid-OrderOpenPrice()>Ask-Bid && Open[0]>Close[0])
           {
            if(!OrderClose(OrderTicket(),OrderLots(),Bid,3,White))
               Print("OrderClose error ",GetLastError());
           }
         break;
        }
      if(OrderType()==OP_SELL)
        {
         if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
           {
            if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
              {
               if(!OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red))
                  Print("OrderModify error ",GetLastError());
               return;
              }
           }
         if(OrderOpenPrice()-Ask>Ask-Bid && Open[0]<Close[0])
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
