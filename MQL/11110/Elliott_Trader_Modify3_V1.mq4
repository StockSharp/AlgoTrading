//+--------------------------------------------------------------------+
//|Date of Design 31.12.2013                    Elliott_Trader         |
//|                        Copyright © 2014, wagdytaher@hotmail.com    |
//|Author and Sole Owner of wagdytaher@hotmail.com                     |
//+--------------------------------------------------------------------+
#property copyright "Copyright © 2014, Elliott_Trader"
#property link      "https://www.facebook.com/elliott.trader"

#define MAGICMA  201203688

extern double Lots1               = 0.20;      // Value For Lot default.
extern double LotsMinimum        = 0.10;       // Describe Minimum Lot Size On Lose Factor.
extern double LotsMaximum        = 0.20;       // Describe Maximum Lot Size On Profit Factor.
extern double RiskFactor         = 1100;       // Describe Risk level For order.
extern double DecreaseFactor     = 30;          // Describe Lot Decrease Factor For order.
extern double Order2Pips         = 55;         // Distance with the First order. Order No 2. Type Limit.
extern double Order3Pips         = 89;         // Distance with the First order. Order No 3. Type Limit.
extern double Order4Pips         = 144;        // Distance with the First order. Order No 4. Type Limit.
extern double Order5Pips         = 210;        // Distance with the First order. Order No 5. Type Limit.
extern double Order6Pips         = 360;        // Distance with the First order. Order No 6. Type Limit.
extern double Order7Pips         = 450;        // Distance with the First order. Order No 7. Type Limit.
extern double Order8Pips         = 520;        // Distance with the First order. Order No 8. Type Limit.
extern double Order9Pips         = 630;        // Distance with the First order. Order No 9. Type Limit.
extern double Lots2;
extern double Lots3;
extern double Lots4;
extern double Lots5;
extern double Lots6;
extern double Lots7;
extern double Lots8;
extern double Lots9;
double Sto;

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
      double lot=Lots1;
      int    orders=HistoryTotal();                // history orders total
      int    losses=0;                             // number of losses orders without a break
   //---- select lot size
      lot=NormalizeDouble(AccountFreeMargin()*0.001/RiskFactor,1);
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
//+------------------------------------------------------------------+
//|  Lot Size Return                                   |
//+------------------------------------------------------------------+
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
  int    res;
//---- get Moving Average. 
   Sto= iStochastic(NULL,PERIOD_H4,21,3,3,MODE_SMA,30,MODE_MAIN,1);
   Lots2= (LotsOptimized()/100)*120;
   Lots3= (Lots2/100)*130;
   Lots4= (Lots3/100)*140;
   Lots5= (Lots4/100)*140;
   Lots6= (Lots5/100)*160;
   Lots7= (Lots6/100)*170;
   Lots8= (Lots7/100)*180;
   Lots9= (Lots8/100)*190;
   
//---- sell conditions.
   if(Sto >= 80)                         // Sell Order No 1. Type Instant.
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 80)                         // Sell Order No 2. Type Sell Limit.  
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots2,Bid+Order2Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 80)                         // Sell Order No 3. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots3,Bid+Order3Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 80)                         // Sell Order No 4. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots4,Bid+Order4Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 82)                         // Sell Order No 5. Type Sell Limit.  
     {    
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots5,Bid+Order5Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 84)                         // Sell Order No 6. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots6,Bid+Order6Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 86)                         // Sell Order No 7. Type Sell Limit.  
     {    
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots7,Bid+Order7Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 88)                      // Sell Order No 8. Type Sell Limit.
     {
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots8,Bid+Order8Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
   if(Sto >= 90)                        // Sell Order No 9. Type Sell Limit.  
     {    
      res=OrderSend(Symbol(),OP_SELLLIMIT,Lots9,Bid+Order9Pips*Point,3,0,0,"",MAGICMA,0,Red);
     }
//---- buy conditions.
   if(Sto <= 20)                        // Buy Order No 1. Type Instant. 
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 20)                           // Buy Order No 2. Type Buy Limit. 
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots2,Ask-Order2Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 20)                           // Buy Order No 3. Type Buy Limit. 
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots3,Ask-Order3Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 20)                      // Buy Order No 4. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots4,Ask-Order4Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 19)                          // Buy Order No 5. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots5,Ask-Order5Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 17)                         // Buy Order No 6. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots6,Ask-Order6Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 15)                         // Buy Order No 7. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots7,Ask-Order7Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 13)                       // Buy Order No 8. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots8,Ask-Order8Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
   if(Sto <= 10)                        // Buy Order No 9. Type Buy Limit.  
     {
      res=OrderSend(Symbol(),OP_BUYLIMIT,Lots9,Ask-Order9Pips*Point,3,0,0,"",MAGICMA,0,Blue);
     }
//----
  }
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose()
  {
double CloseBuy =iBands(NULL, PERIOD_M30, 20, 2, 0, PRICE_TYPICAL, MODE_LOWER,  + 2);
double CloseSell = iBands(NULL, PERIOD_M30, 20, 2, 0, PRICE_TYPICAL, MODE_UPPER,  + 2);
double Ma =iMA(NULL,0,1,4,MODE_SMA,PRICE_CLOSE,0);
double Ma1 =iMA(NULL,PERIOD_H4,200,4,MODE_SMA,PRICE_TYPICAL,0);
double Ma2 =iMA(NULL,PERIOD_H4,55,4,MODE_SMA,PRICE_TYPICAL,0);
double Sto_Op= iStochastic(NULL,PERIOD_H4,21,3,3,MODE_SMA,0,MODE_MAIN,1);

//---- Getting BB & MA & Stoch. 

   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
  
   //---- Check & Find Orders And Close If Account Total Profit Status In Positive.
      if(OrderType()==OP_BUY)
        {
   if(AccountProfit()>100 && Ma >= CloseBuy && Ma1 >= Ma2)  
          OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
         break;
        }
      if(OrderType()==OP_SELL)
        {
   if(AccountProfit()>100 && Ma <= CloseSell && Ma1 <= Ma2)  
          OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
         break;
        }

  //---- Check And Close If Order Type is Limit And Trend Is Changed.
     if(OrderType()==OP_BUYLIMIT)
   if(Sto_Op >=90 && Ma1 <= Ma2)
    {
     OrderDelete(OrderTicket());
     return(0);
    }
    if(OrderType()==OP_SELLLIMIT)
    if(Sto_Op <=10 && Ma1 >= Ma2)
    {
     OrderDelete(OrderTicket());
     return(0);
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
   else
//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
//----
  }
//+------------------------------------------------------------------+