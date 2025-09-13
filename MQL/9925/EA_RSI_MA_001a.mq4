/*------------------------------------------------------------------+
 |                                               EA_RSI_MA_001a.mq4 |
 |                                                 Copyright © 2010 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*/
#property copyright "Copyright © 2010, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//-----  
#define MagicNum 10001
//-----
extern int      RSIPeriod           = 14;
extern int      TrailingStop        = 299;//    1991.46 / 782.92/ 15
extern int      TP                  = 999;
extern int      SL                  = 399;
//-----
extern bool     UseMM               = false;
extern int      PercentMM           = 10;
extern double   Lots                = 0.1;
//+------------------------------------------------------------------+
double GetLots()
 { 
   if (UseMM)
    {
      double a, maxLot, minLot;
      a = (PercentMM * AccountFreeMargin() / 100000);
      double LotStep = MarketInfo(Symbol(), MODE_LOTSTEP);
      maxLot  = MarketInfo(Symbol(), MODE_MAXLOT );
      minLot  = MarketInfo(Symbol(), MODE_MINLOT );   
      a =  MathFloor(a / LotStep) * LotStep;
      if (a > maxLot) return(maxLot);
      else if (a < minLot) return(minLot);
      else return(a);
    }    
   else return(Lots);
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
int Get_Broker_Digit()
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
int start()
 {
   if (Symbol() != "EURUSD")
    {
		Comment("Not a right Symbol: ", Symbol(), " <>  EURUSD");
		return(0);
	 }  
   if (Period() != 1440)
    {
		Comment("Not a right Period!!! It should be D1");
		return(0);	
	 }             
   if (AccountFreeMargin() < 20)
    {
		Comment("AccountFreeMargin < 20");
		return(0);
	 }
   //=================================================================
   int cnt, ticket, total;
   int digitKoeff = Get_Broker_Digit();
   //-----
   if(CalculateCurrentOrders() == 0) 
    {      
      double rm0 = iCustom(NULL, 0, "RSI_MA", RSIPeriod, 0, 1);
      double rm1 = iCustom(NULL, 0, "RSI_MA", RSIPeriod, 0, 2);
      //-------------------------------------------------------  
      if(rm1 < 5 && rm0 > 20)
       {      
         ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * digitKoeff, Ask - SL * Point * digitKoeff, Ask + TP * Point * digitKoeff, "rsi-ma", MagicNum, 0, Green);
         if(ticket > 0)
          {            
             return(0);
          }
       }     
      if(rm1 > 95 && rm0 < 80)
       {
         ticket = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3 * digitKoeff, Bid + SL * Point * digitKoeff, Bid - TP * Point * digitKoeff, "rsi-ma", MagicNum, 0, Red);
         if(ticket > 0)
          {
             return(0); 
          }  
       }            
    }
   //==============================
   total = OrdersTotal();
   for(cnt = 0; cnt < total; cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() <= OP_SELL && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         double rmCl0 = iCustom(NULL, 0, "RSI_MA", RSIPeriod, 0, 1);
         double rmCl1 = iCustom(NULL, 0, "RSI_MA", RSIPeriod, 0, 2);
         //-----------------------         
         if(OrderType() == OP_BUY)
          {                
            if(rmCl1 > 95 && rmCl0 < 80)
             {
               OrderClose(OrderTicket(), OrderLots(), Bid, 3 * digitKoeff, Violet); 
               return(0); 
             }
            if(Bid - OrderOpenPrice() > Point * TrailingStop * digitKoeff)
             {
               if(OrderStopLoss() < Bid - Point * TrailingStop * digitKoeff)
                {
                   OrderModify(OrderTicket(), OrderOpenPrice(), Bid - Point * TrailingStop * digitKoeff, OrderTakeProfit(), 0, Green);
                   return(0);
                }
             }
          }
         else if(OrderType() == OP_SELL)
          {                
            if(rmCl1 < 5 && rmCl0 > 20)
             {
               OrderClose(OrderTicket(), OrderLots(), Ask, 3 * digitKoeff, Violet); 
               return(0); 
             }
            if((OrderOpenPrice() - Ask) > (Point * TrailingStop * digitKoeff))
             {
               if(OrderStopLoss() > (Ask + Point * TrailingStop * digitKoeff))
                {
                   OrderModify(OrderTicket(), OrderOpenPrice(), Ask + Point * TrailingStop * digitKoeff, OrderTakeProfit(), 0, Red);
                   return(0);
                }
             }
          }
       }
    }   
   return(0);
 }