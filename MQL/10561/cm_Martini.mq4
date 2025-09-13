//|                           Copyright © 2011, Skype:  mqlcmillion  |
//|                                         http://cmillion.narod.ru |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, Skype:  mqlcmillion "
#property link      "http://cmillion.narod.ru"
//-------------------------------------------------------------------- 
extern int     Step           = 10;
extern double  ProfitClose    = 1.0,
               lot            = 0.01;
extern int     slippage       = 3,     //The maximum permissible deviation of the price for market orders (orders to buy or sell).
               magic          = 0;     //The magic number order. Can be used as a user-defined identifier.
//-------------------------------------------------------------------- 
int init() 
{ 
   Comment("Start EA ",TimeToStr(TimeCurrent(),TIME_DATE|TIME_SECONDS));
} 
//-------------------------------------------------------------------- 
int start() 
{
   double OOP,Price,Lot,Profit,MaxLot;
   int OT,n,oo,Order;
   for (int i=0; i<OrdersTotal(); i++)
   {    
      if (OrderSelect(i,SELECT_BY_POS,MODE_TRADES))
      { 
         if (OrderSymbol()==Symbol() && OrderMagicNumber()==magic)
         { 
            OT = OrderType(); 
            OOP = NormalizeDouble(OrderOpenPrice(),Digits);
            if (OT==OP_BUY)             
            {  
               Price = OOP;
               Lot = OrderLots();
               Profit+=OrderProfit()+OrderSwap()+OrderCommission();
               if (MaxLot<Lot) {MaxLot=Lot; Order= 1;}
               n++;
            }                                         
            if (OT==OP_SELL)        
            {
               Price = OOP;
               Lot = OrderLots();
               Profit+=OrderProfit()+OrderSwap()+OrderCommission();
               if (MaxLot<Lot) {MaxLot=Lot; Order=-1;}
               n++;
            } 
            if (OT>1) oo++;
         }
      }
   } 
   Comment("Profit ",DoubleToStr(Profit,2));
   if (Profit>ProfitClose)
   {
      CloseAllOrders(OP_BUY);
      CloseAllOrders(OP_SELL);
   }
   if (Order==1 && Bid+n*Step*Point<Price)
   {
      if(OrderSend(Symbol(),OP_SELL,Lot*2,NormalizeDouble(Bid,Digits),slippage,0,0,"",magic,0,Red)!=-1) return;
   }
   
   if (Order==-1 && Ask-n*Step*Point>Price) 
   {
      if(OrderSend(Symbol(),OP_BUY,Lot*2,NormalizeDouble(Ask,Digits),slippage,0,0,"",magic,0,Blue)!=-1) return;
   }
   
   if (n==0 && oo==0)
   {
      OrderSend(Symbol(),OP_SELLSTOP,lot,NormalizeDouble(Bid-10*Point,Digits),slippage,0,0,"",magic,0,Red); 
      OrderSend(Symbol(),OP_BUYSTOP ,lot,NormalizeDouble(Ask+10*Point,Digits),slippage,0,0,"",magic,0,Blue); 
   }
   if (n!=0) DeleteAll(0);
   return(0); 
} 
//----------------------------------------------------------------- 
bool CloseAllOrders(int tip)
{
   bool error=true;
   int err,nn,OT,OMN;
   while(true)
   {
      for (int j = OrdersTotal()-1; j >= 0; j--)
      {
         if (OrderSelect(j, SELECT_BY_POS))
         {
            OMN = OrderMagicNumber();
            if (OrderSymbol() == Symbol() && OMN == magic)
            {
               OT = OrderType();
               if (OT != tip) continue;
               if (OT==OP_BUY) 
               {
                  error=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Bid,Digits),slippage,Blue);
                  if (error) Comment("Close order N ",OrderTicket(),"  Profit ",OrderProfit(),
                                     "     ",TimeToStr(TimeCurrent(),TIME_SECONDS));
               }
               if (OT==OP_SELL) 
               {
                  error=OrderClose(OrderTicket(),OrderLots(),NormalizeDouble(Ask,Digits),slippage,Red);
                  if (error) Comment("Close order N ",OrderTicket(),"  Profit ",OrderProfit(),
                                     "     ",TimeToStr(TimeCurrent(),TIME_SECONDS));
               }
               if (!error) 
               {
                  err = GetLastError();
                  if (err<2) continue;
                  if (err==129) 
                  {  Comment("Wrong price ",TimeToStr(TimeCurrent(),TIME_SECONDS));
                     RefreshRates();
                     continue;
                  }
                  if (err==146) 
                  {
                     if (IsTradeContextBusy()) Sleep(2000);
                     continue;
                  }
                  Comment("Error ",err," close order N ",OrderTicket(),
                          "     ",TimeToStr(TimeCurrent(),TIME_SECONDS));
               }
            }
         }
      }
      int n=0;
      for (j = 0; j < OrdersTotal(); j++)
      {
         if (OrderSelect(j, SELECT_BY_POS))
         {
            OMN = OrderMagicNumber();
            if (OrderSymbol() == Symbol() && OMN == magic)
            {
               OT = OrderType();
               if (OT != tip) continue;
               if (OT==OP_BUY || OT==OP_SELL) n++;
            }
         }  
      }
      if (n==0) break;
      nn++;
      if (nn>10) {Alert(Symbol()," Failed to close all trades, there are still ",n);return(0);}
      Sleep(1000);
      RefreshRates();
   }
   return(1);
}
//--------------------------------------------------------------------
bool DeleteAll(int tip)
{
   bool error;
   int err,n,OMN,OT;
   while(true)
   {
      error=true;
      for (int j = OrdersTotal()-1; j >= 0; j--)
      {
         if (OrderSelect(j, SELECT_BY_POS))
         {
            OMN = OrderMagicNumber();
            if (OrderSymbol() == Symbol() && OMN == magic)
            {
               OT = OrderType();
               if (OT>1 && (tip==0 || OT==tip)) error=OrderDelete(OrderTicket());
            }
         }
      }
      if (error) break;
      n++;
      if (n>10) break;
      Sleep(1000);
   }
   return(1);
}
//--------------------------------------------------------------------