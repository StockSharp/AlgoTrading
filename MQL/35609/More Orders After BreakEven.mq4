//+------------------------------------------------------------------+
//|                                  More Orders After BreakEven.mq4 |
//|                                     Copyright 2021, Omega Joctan |
//|                        https://www.mql5.com/en/users/omegajoctan |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, Omega Joctan"
#property link      "https://www.mql5.com/en/users/omegajoctan"
#property description "Wanna hire me ? omegajoctan@gmail.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
extern    int     MagicNumber = 11072021;
extern    int     MaximumOrders = 1;    //Maximum number of Orders Allowed to beopened by this EA
extern    int     TakeProfitPips  = 100;  //Take profit in Pips
extern    int     StopLossPips   = 200;
extern    int     BreakevenPips = 10;
extern    int     Slippage = 10;
extern    double  Lotsize = 0.01;   //Trading Volume
//---
int    points=1;
bool   debugMode=true;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
   if(Digits()%2==1) // Checking for digits brokers
     { points = 10; }   //for 1,3,5 Digits brokers Pip will be multiplied by 10
//---
   TakeProfitPips*=points;
   BreakevenPips*=points;
   Slippage*=points;
   StopLossPips*=points;

   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   double volume = Lotsize;
   double min = SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN); //Minimu Trading Volume
   if(Lotsize<min) // if User has given the lotsize smaller than that allowed by broker
     { 
       volume = min;
     }  /* minimum volume will be used */
   if(OrdersCounter()<MaximumOrders)
     {
      double TP = TakeProfitPips !=0 ? Ask+TakeProfitPips*_Point : 0; // if StopLoss TakeProfit iz equal to zero set to zero ...otherwise
      int buy =  OrderSend(Symbol(),OP_BUY,volume,Ask,Slippage,Ask-StopLossPips*_Point,TP,NULL,MagicNumber,0,clrNONE);
     }
     BreakEvenFunction();
//---
     if (debugMode)
       {
        Comment("OrdersCounter()==",OrdersCounter());
       }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int OrdersCounter()
  {
   int counter=0;
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS))
         if(OrderMagicNumber()==MagicNumber && OrderSymbol()==Symbol()) // if order has been opened by this EA
           {
//--- if break even has taken place 
   /* For buys Only when the StopLoss is equal or above the Open Price NOTE This is implementetion is not
   good if you are going to have Pending Orders Its only suitable for buy and sells only*/
            double XBreakeven = OrderType()==OP_BUY ? OrderStopLoss() >= OrderOpenPrice() : OrderStopLoss() <= OrderOpenPrice();
            if(!XBreakeven) //If only Break Even and trailing stop hasn't taken place'
              {
               counter++; //count the Position
              }
           }
   return counter;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void BreakEvenFunction()
  {
//---
   for(int i=OrdersTotal()-1; i>=0; i--)
      if(OrderSelect(i,SELECT_BY_POS))
         if(OrderMagicNumber()==MagicNumber && OrderSymbol()==Symbol())
           {
// for buy if Bid above Open Price + Breakeven pips Vice Versa for sells
            double xHybrid = OrderType()==OP_BUY ? (Bid>OrderOpenPrice()+BreakevenPips*_Point && OrderStopLoss()<OrderOpenPrice()) : (Ask<OrderOpenPrice()-BreakevenPips*_Point && OrderStopLoss()>OrderOpenPrice());
            /* For buys Only when the StopLoss is equal or above the Open Price Vice versa for sell */
            if(xHybrid)
              {
               bool modfy = OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,clrNONE);
              }
           }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
