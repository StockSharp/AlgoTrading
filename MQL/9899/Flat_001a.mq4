/*------------------------------------------------------------------+
 |                                                    Flat_001a.mq4 |
 |                                                 Copyright © 2010 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*/
#property copyright "Copyright © 2010, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//----------------------------------------  
#define MagNum 10101 
//------------------ 
extern int     TrailingStop_1    = 6;
extern int     DiffMin           = 18;
extern int     DiffMax           = 28;
//------------------------------------
extern bool    OpenTime_1        = true;
extern int     OpenHour_1        = 0;
//------------------------------------
extern int     TakeProfit        = 8;
extern bool    UseMM             = false;//true;
extern double  PercentMM         = 10;
extern double  lots              = 0.1;
//------------------------------------
double         H, L;
int            D;
//+------------------------------------------------------------------+
int Calculate_BUY()
 {
   int orderT = OrdersTotal(), buys = 0;
   //----
   for(int i = 0; i < orderT; i++)
    {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) break;
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagNum)
       {
         if(OrderType() == OP_BUY)  buys++;
       }
    }
   if(buys > 0) return(buys);
   else return(0);
 }
//+------------------------------------------------------------------+
int Calculate_SELL()
 {
   int orderT = OrdersTotal(), sells = 0;
   //----
   for(int i = 0; i < orderT; i++)
    {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) break;
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagNum)
       {
         if(OrderType() == OP_SELL)  sells++;
       }
    }
   if(sells > 0) return(-sells);
   else return(0);
 }
//+------------------------------------------------------------------+ 
double GetLots()
 { 
   if (UseMM)
    {
      double a;
      a = NormalizeDouble((PercentMM * AccountFreeMargin() / 100000), 1);      
      if(a > 49.9) return(49.9);
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
int start()
 {
   if (Symbol() != "EURUSD")
    {
		Comment("Not a right Symbol: ", Symbol(), " <>  EURUSD");
		return(0);
	 }           
   if (Period() != 60)
    {
		Comment("Not a right Period!!! It should be H1");
		return(0);	
	 }     
   if (AccountFreeMargin() < 100)
    {
		Comment("AccountFreeMargin < 100");
		return(0);
	 }
	//--------------------   
   int total, ticket, cnt;      
   //----------------------------------------------------
   if(OpenTime_1 == true)
    {
      if(Hour() >= OpenHour_1 && Hour() <= OpenHour_1 + 1)
       {
         H = High[iHighest(NULL, 0, MODE_HIGH, 3, 0)];
         L = Low[iLowest(NULL, 0, MODE_LOW, 3, 0)];
         D = (H - L) / Point;
         if((D > DiffMin && D < DiffMax) && Calculate_SELL() == 0 && Calculate_BUY() == 0 && Bid > L && Bid <= L + D / 4 * Point)
          { 
             ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3, L - D / 3 * Point, Ask + TakeProfit * Point, "ch1", MagNum, 0, Green);
          }
         if((D > DiffMin && D < DiffMax) && Calculate_BUY() == 0 && Calculate_SELL() == 0 && Bid < H && Bid >= H - D / 4 * Point)
          {
             ticket = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3, H + D / 3 * Point, Bid - TakeProfit * Point, "ch1", MagNum, 0, Red);
          }
       }
    }   
//======================================================================================================================================== 
   total = OrdersTotal();
   for(cnt = 0; cnt < total; cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagNum)
       {
         if(OrderComment() == "ch1")
          { 
            if(OrderType() == OP_BUY)
             {
               if(Bid - OrderOpenPrice() > Point * TrailingStop_1)
                {
                  if(OrderStopLoss() < Bid - Point * TrailingStop_1)
                   {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Bid - Point * TrailingStop_1, OrderTakeProfit(), 0, Yellow);
                     return(0);
                   }
                }       
             }
            else if(OrderType() == OP_SELL)
             {
               if((OrderOpenPrice() - Ask) > (Point * TrailingStop_1))
                {
                  if((OrderStopLoss() > (Ask + Point * TrailingStop_1)) || (OrderStopLoss() == 0))
                   {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Ask + Point * TrailingStop_1, OrderTakeProfit(), 0, Yellow);
                     return(0);
                   }
                }  
             }
          }   
       }
    }
   return(0);          
 }