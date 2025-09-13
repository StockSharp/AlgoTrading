//+--------------------------------------------------------------------+
//|Date of Design 11.11.11                      Marneni Money Tree.mq4 |
//|                        Copyright © 2011, MetaQuotes Software Corp. |
//|                               http://www.marnenimoneytree.webs.com |
//|Author and Sole Owner of Mareni Money Tree KONDALARAO MARNENI india.|
//+--------------------------------------------------------------------+
#property copyright "Copyright © 2011, Konddalarao Marneni."
#property link      "http://www.marnenimoneytree.webs.com"

#define MAGICMA  20111111

extern double AccountTarget      = 10000000.0; // To Set Target Level. 
extern double Lots               = 2.0;        // Value For Lot default.
extern double LotsMinimum        = 2.0;        // Describe Minimum Lot Size On Lose Factor.
extern double LotsMaximum        = 10.0;       // Describe Maximum Lot Size On Profit Factor.
extern double RiskFactor         = 110;       // Describe Risk level For order.
extern double DecreaseFactor     = 3;          // Describe Lot Decrease Factor For order.
extern double Order2Pips         = 4;          // Distance with the First order. Order No 2. Type Limit.
extern double Order3Pips         = 8;         // Distance with the First order. Order No 3. Type Limit.
extern double Order4Pips         = 12;         // Distance with the First order. Order No 4. Type Limit.
extern double Order5Pips         = 20;         // Distance with the First order. Order No 5. Type Limit.
extern double Order6Pips         = 30;         // Distance with the First order. Order No 6. Type Limit.
extern double Order7Pips         = 40;         // Distance with the First order. Order No 7. Type Limit.
extern double Order8Pips         = 50;         // Distance with the First order. Order No 8. Type Limit.
extern double Order9Pips         = 60;         // Distance with the First order. Order No 9. Type Limit.
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
         if(OrderType()==OP_BUY)  buys++;       // Find and Calculate Long Orders. 
         if(OrderType()==OP_SELL) sells++;      // Find and Calculate Short Orders.
         if(OrderType()==OP_BUYLIMIT)  buys++;  // Find and Calculate Long Limit Orders.
         if(OrderType()==OP_SELLLIMIT) sells++; // Find and Calculate Short Limit Orders.
        }
     }
//---- return orders volume
   if(buys>0) return(buys);                     // Number of Order Controler.
   else       return(-sells);
  }
//+------------------------------------------------------------------+
//| Calculate optimal lot size                                       |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();                // history orders total
   int    losses=0;                             // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*0.01/RiskFactor,1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
//---- return lot size
   if(lot<LotsMinimum) lot=LotsMinimum;        // Describe Minimum Lot Size On Lose Factor.
   return(lot);                                //  and Controlar.
   if(lot>LotsMaximum) lot=LotsMaximum;        // Describe Maximum Lot Size On Profit Factor And Controlar.
   return(LotsMaximum);                        //  and Controlar.
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
   double ma;     double ma1;   double ma2;
   double lots9;
   int    res;
//---- get Moving Average. 
   ma=iMA(NULL,0,40,4,MODE_SMA,PRICE_CLOSE,0);
   ma1=iMA(NULL,0,40,0,MODE_SMA,PRICE_CLOSE,0);
   ma2=iMA(NULL,0,40,0,MODE_SMA,PRICE_CLOSE,30);
   lots9=(LotsOptimized()/100)*150;
   
//---- sell conditions.
   if(ma>ma1 && ma<ma2)                         // Sell Order No 1. Type Instant.
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 2. Type Sell Limit.  
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order2Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 3. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order3Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 4. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order4Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 5. Type Sell Limit.  
     {    
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order5Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 6. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order6Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 7. Type Sell Limit.  
     {    
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order7Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 8. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,LotsOptimized(),Bid+Order8Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(ma>ma1 && ma<ma2)                         // Sell Order No 9. Type Sell Limit.  
     {    
      res=OrderSend(Symbol(),OP_SELLLIMIT,lots9,Bid+Order9Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
//---- buy conditions.
   if(ma<ma1 && ma>ma2)                         // Buy Order No 1. Type Instant. 
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 2. Type Buy Limit. 
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order2Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 3. Type Buy Limit. 
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order3Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 4. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order4Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 5. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order5Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 6. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order6Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 7. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order7Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 8. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,LotsOptimized(),Ask-Order8Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(ma<ma1 && ma>ma2)                         // Buy Order No 9. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,lots9,Ask-Order9Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
   double ma;     double ma1;

//---- get Moving Average. 
   ma=iMA(NULL,0,30,0,MODE_SMA,PRICE_CLOSE,1);
   ma1=iMA(NULL,0,30,3,MODE_SMA,PRICE_CLOSE,1);
   //----
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
  
   //---- Check & Find Orders And Close If Account Total Profit Status In Positive.
      if(OrderType()==OP_BUY)
        {
   if(AccountProfit()>0 && ma>ma1)  
          OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
   if(AccountProfit()>0 && ma<ma1)  
          OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
         break;
        }

   //---- Check & Find Orders And Close If Each Account Total profit in Lose & 
   //---- Some Orders Profit Status In Positive.
      if(OrderType()==OP_BUY)
        {
   if(OrderProfit()>0 && ma>ma1)  
          OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
   if(OrderProfit()>0 && ma<ma1)  
          OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
         break;
        }
   //---- Check And Close If Order Type is Limit And Trend Is Changed.
      if(OrderType()==OP_BUYLIMIT)
   if(ma>ma1)
    {
     OrderDelete(OrderTicket());
     return(0);
    }
    if(OrderType()==OP_SELLLIMIT)
    if(ma<ma1)
    {
     OrderDelete(OrderTicket());
     return(0);
    }
    
      if(OrderType()==OP_BUY)
        {
 
  //---- Check And Close Orders Immidiate, If AccountEquity Reached to Target Fixied Level.
     if(AccountEquity()>AccountTarget)  
          OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
        break;
        }
      if(OrderType()==OP_SELL)
        {
     if(AccountEquity()>AccountTarget)  
          OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
        break;
        }
     }
//----
  }
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
//---- check for history and trading
   if(Bars<100 || IsTradeAllowed()==false) return;       // Stop EA if Bars is Less then...
   else
   if(AccountNumber()>1741659   || IsTradeAllowed()==false) return;  // Stop EA if A/c No Wrong...
   else
   if(AccountNumber()<1741659   || IsTradeAllowed()==false) return;  // Stop EA if A/c No Wrong...
   else
   if(Year()>=2012   || IsTradeAllowed()==false) return; // Stop EA if Year is grater then...
   else
   if(AccountEquity()>AccountTarget && OrdersTotal()==0   || IsTradeAllowed()==false) return; //---
   // // Stop Trade if EA successed on Target and Orders Total is 0...
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+