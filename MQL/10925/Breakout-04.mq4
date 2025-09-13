/*------------------------------------------------------------------+
 |                                                  Breakout-04.mq4 |
 |                                                 Copyright © 2013 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*/
#property copyright "Copyright © 2013, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//----------------------------------------
#define MagicNum  20123
//---------------------
extern  int       MondayHour         = 18;
extern  int       FridayHour         = 14; 
//---
extern  int       TrailingStop       = 21;
extern  int       TP                 = 550;
extern  int       SL                 = 124;
extern  bool      UseMM              = false;
extern  int       PercentMM          = 8;
extern  double    lots               = 0.1;
//---
double HiPrice, LoPrice, CloPrice, Range;
datetime StartTime;
int res;
//+------------------------------------------------------------------+
int GetDigits()
 {
   if(Digits == 5 || Digits == 3)
    {
      return(10);
    }
   else
    {
      return(1);
    } 
 }
//+------------------------------------------------------------------+ 
int CalculateCurrentOrders()
 {
   int orderT = OrdersTotal(), buys = 0, sells = 0;
   //----
   for(int i = 0; i < orderT; i++)
    {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) break;
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         if(OrderType() == OP_BUY)  buys++;
         if(OrderType() == OP_SELL) sells++;
       }
    }
   if(buys > 0) return(buys);
   else if(sells > 0) return(-sells);
   else return(0);
 }
//+------------------------------------------------------------------+ 
double GetLots()
 { 
   if(UseMM)
    {
      double a;
      a = NormalizeDouble((PercentMM * AccountFreeMargin() / 100000), 1);      
      if(a > 99.9) return(99.9);
      else if(a < 0.1)
       {
         Print("Lots < 0.1");
         return(0);
       }
      else return(a);
    }    
   else return(lots);
 }
//+------------------------------------------------------------------+
void CheckForModify()
 {
   int d = GetDigits(); 
   for(int cnt = 0; cnt < OrdersTotal(); cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() <= OP_SELL && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         if(OrderType() == OP_BUY)
          {
            if(TrailingStop>0)  
             {                 
               if(Bid-OrderOpenPrice()>Point*TrailingStop)
                {
                  if(OrderStopLoss()<Bid-Point*TrailingStop)
                   {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop*d,OrderTakeProfit(),0,Green);
                     return(0);
                   }
                }
             }  
          }
         else if(OrderType() == OP_SELL)
          {
            if(TrailingStop > 0)  
             {                 
               if((OrderOpenPrice()-Ask)>(Point*TrailingStop))
                {
                  if((OrderStopLoss()>(Ask+Point*TrailingStop)) || (OrderStopLoss()==0))
                   {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop*d,OrderTakeProfit(),0,Red);
                     return(0);
                   }
                }
             }               
          }
       }
    }
 }
//+------------------------------------------------------------------+  
void CheckForOpen()
 {
   if(TimeDayOfWeek(StartTime) == 5 && Hour() > FridayHour) return(0);	
	if(TimeDayOfWeek(StartTime) == 1 && Hour() < MondayHour) return(0);
	//---
   int shift	= iBarShift(NULL,PERIOD_D1,Time[0]) + 1;	// yesterday
	HiPrice		= iHigh(NULL,PERIOD_D1,shift);
	LoPrice		= iLow(NULL,PERIOD_D1,shift);
	CloPrice    = iClose(NULL,PERIOD_D1,shift);
	StartTime	= iTime(NULL,PERIOD_D1,shift - 1);
   //---
	if(TimeDayOfWeek(StartTime) == 1)/*Monday*/
	 {
		HiPrice = MathMax(HiPrice,iHigh(NULL,PERIOD_D1,shift+1));
		LoPrice = MathMin(LoPrice,iLow(NULL,PERIOD_D1,shift+1));
		CloPrice = iClose(NULL,PERIOD_D1,shift+1);
	 }     
   //----------------------------------------------------------------
   int d = GetDigits();
   if(Bid > HiPrice)  
    {
      res = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * d, 0, 0, "br", MagicNum, 0, Green);
      if(res > 0)
       {
         OrderModify(res, Ask, Ask - SL * Point * d, Ask + TP * Point * d, 0, Blue);
         PlaySound("wmpaud1.wav");
         return;
       }
    }
   //---------------------------- 
   if(Ask < LoPrice)  
    {
      res = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3 * d, 0, 0, "br", MagicNum, 0,Red);
      if(res > 0)
       {
         OrderModify(res, Bid, Bid + SL * Point * d, Bid - TP * Point * d, 0, Blue);
         PlaySound("wmpaud1.wav");
         return;
       }
    }  
 }
//+------------------------------------------------------------------+
void start()
 {
   if(CalculateCurrentOrders() != 0) CheckForModify();
   if(CalculateCurrentOrders() == 0) CheckForOpen();
 }
//+------------------------------------------------------------------+