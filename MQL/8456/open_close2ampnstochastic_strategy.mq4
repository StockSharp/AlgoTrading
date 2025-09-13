//+------------------------------------------------------------------+
//|                                      исправленная евродоллар.mq4 |
//|                      Copyright © 2008, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+

#property copyright "Copyright © 2008, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

extern double Lots               = 0.1;
extern double MaximumRisk        = 0.3;
extern double DecreaseFactor     = 100;
double  st1,st2;
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
      if(OrderSymbol()==Symbol())
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
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/1000.0,3);
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
   if(lot<0.1) lot=0.1;
   return(lot);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen()
  {
  int res;
 if(Volume[0]>1) return;

//---- покупаем -----------------------
  
  if ( (iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_MAIN,0)>iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_SIGNAL,0))) 
     if  ((Open[0]<Open[1])&&(Close[0]<Close[1])) 
    
  //  
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,15,0,"",0,0,Blue);
      return;
     }
//---- продаем ------------------------
if ((iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_MAIN,0)<iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_SIGNAL,0)))  
    if((Open[0]>Open[1])&&(Close[0]>Close[1]))
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,15,0,"",0,0,Red);
      return;
     }
     
  }
//------------Закрытие позиций----------------------------------------
void CheckForClose2()
{
 if(OrderType()==OP_BUY)  OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
 if(OrderType()==OP_SELL) OrderClose(OrderTicket(),OrderLots(),Ask,3,Blue);
//CheckForOpen();
}  

//
//+------------------------------------------------------------------+
//| Check for close order conditions                                 |
//+------------------------------------------------------------------+
void CheckForClose1()
  {
   if(Volume[0]>1) return;
//проверка на проигрыш   
if ((AccountProfit()<0)&&(MathAbs(AccountProfit())>=(AccountMargin()*MaximumRisk))) 
   { 
   CheckForClose2();
   Print(" убыток", AccountProfit());
   }/**/
      //---- check order type 
      if(OrderType()==OP_BUY)
        {
         if ((iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_MAIN,0)<iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_SIGNAL,0)))
         if ((Open[0]>Open[1])&&(Close[0]>Close[1]))
         CheckForClose2();  
        }
      if(OrderType()==OP_SELL)
        {
         if ((iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_MAIN,0)>iStochastic(NULL,0,9,3,3,MODE_SMA,0,MODE_SIGNAL,0)))
             if ((Open[0]<Open[1])&&(Close[0]<Close[1]))
          CheckForClose2();
        }

  }
  
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+

void start()
 {

//---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose1();
//---
  }
//+-----------------------------------------------+