/*------------------------------------------------------------------+
 |                                             EA_Fibo_Avg_001a.mq4 |
 |                                                 Copyright © 2010 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*/
#property copyright "Copyright © 2010, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//-----  
#define MagicNum 10001
//-----
extern int      FiboNumPeriod  = 11;//  Numbers in the following integer sequence;
extern int      nAppliedPrice  = 0;//   PRICE_CLOSE=0; PRICE_OPEN=1; PRICE_HIGH=2; PRICE_LOW=3; PRICE_MEDIAN=4; PRICE_TYPICAL=5; PRICE_WEIGHTED=6;
extern int      maPeriod       = 21;//  Averaging period for calculation;
extern int      maMethod       = 2;//   MODE_SMA=0; MODE_EMA=1; MODE_SMMA=2; MODE_LWMA=3;
//-----
extern int      TrailingStop   = 140;
extern int      TP             = 999;
extern int      SL             = 399;
//-----
extern bool     UseMM          = false;
extern int      PercentMM      = 10;
extern double   Lots           = 0.1;
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
   int cnt, ticket, total;
   int digitKoeff = Get_Broker_Digit();
   //-----
   if(CalculateCurrentOrders() == 0) 
    {      
      double rm00 = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 0, 0);
      double rm01 = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 0, 1);
      double rm10 = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 1, 0);
      double rm11 = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 1, 1);
      //-------------------------------------------------------  
      if(rm01 < rm11 && rm00 > rm10)
       {      
         ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * digitKoeff, Ask - SL * Point * digitKoeff, Ask + TP * Point * digitKoeff, "rsi-ma", MagicNum, 0, Green);
         if(ticket > 0)
          {            
             return(0);
          }
       }     
      if(rm01 > rm11 && rm00 < rm10)
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
         double rm00c = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 0, 0);
         double rm01c = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 0, 1);
         double rm10c = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 1, 0);
         double rm11c = iCustom(NULL, 0, "Fibo-Average-2B", FiboNumPeriod, nAppliedPrice, maPeriod, maMethod, 1, 1);
         //-----------------------         
         if(OrderType() == OP_BUY)
          {                
            if(rm01c > rm11c && rm00c < rm10c)
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
            if(rm01c < rm11c && rm00c > rm10c)
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