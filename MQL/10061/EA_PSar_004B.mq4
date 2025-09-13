/*------------------------------------------------------------------+
 |                                                 EA_PSar_004B.mq4 |
 |                                                 Copyright © 2010 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*/
#property copyright "Copyright © 2010, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//-----  
#define MagicNum 101
//---
extern bool     sar4                = false;
extern bool     sar3                = true;
extern bool     sar2                = true;
//-----
extern string   _CloseModifyPeriod  = "1 or 5 or 15 or 30 or 60 or 240 or 1440";
extern int      CloseModifyPeriod   = 30;
extern int      PointDifferent      = 15;
extern double   step15              = 0.062;
extern double   step30              = 0.058;
extern double   step60              = 0.058;
extern double   step240             = 0.058;
extern double   stepClo             = 0.051;
extern double   max                 = 0.1;
//-----
extern int      TP                  = 888;
extern int      SL                  = 555;
//-----
extern bool     UseMM               = false;
extern int      PercentMM           = 10;
extern double   Lots                = 0.1;
int             Per;
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
   if(StringFind(Symbol(), "EURUSD", 0) != 0)
    {
		Comment("Not a right Symbol: ", Symbol(), " <>  EURUSD");
		return(0);
	 } 
   if(Period() != 1)
    {
		Comment("Not a right Period!!! It should be M1");
		return(0);	
	 }             
   if(AccountFreeMargin() < 20)
    {
		Comment("AccountFreeMargin < 20");
		return(0);
	 }
   //======================================================================
   double sa15_0, sa15_1, sa30_0, sa30_1, sa60_0, sa60_1, sa240_0, sa240_1;
   double saUp, saDn;
   int saDif;
   int cnt, ticket, total;
   int digitKoeff = Get_Broker_Digit();
   //-----
   if(CalculateCurrentOrders() == 0) 
    {//++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
      if(sar2 == true && sar3 == true && sar4 == true)
       {
         sa15_0  = iSAR(NULL, PERIOD_M15, step15, max, 0);
         sa15_1  = iSAR(NULL, PERIOD_M15, step15, max, 1);
         sa30_0  = iSAR(NULL, PERIOD_M30, step30, max, 0);
         sa30_1  = iSAR(NULL, PERIOD_M30, step30, max, 1);
         sa60_0  = iSAR(NULL, PERIOD_H1, step60, max, 0);
         sa60_1  = iSAR(NULL, PERIOD_H1, step60, max, 1);
         sa240_0 = iSAR(NULL, PERIOD_H4, step240, max, 0);
         sa240_1 = iSAR(NULL, PERIOD_H4, step240, max, 1);
         //--------------------------------------------  
         if(sa15_0 > sa30_0) saUp = sa15_0;
         else saUp = sa30_0;
         if(saUp > sa60_0) saUp = saUp;
         else saUp = sa60_0;
         if(saUp > sa240_0) saUp = saUp;
         else saUp = sa240_0;
         //-----
         if(sa15_0 < sa30_0) saDn = sa15_0;
         else saDn = sa30_0;
         if(saDn < sa60_0) saDn = saDn;
         else saDn = sa60_0;
         if(saDn < sa240_0) saDn = saDn;
         else saUp = sa240_0;
         //-----
         saDif = (saUp - saDn) / Point;
         //----------------------------
         if(saDif <= PointDifferent)
          {
            if((sa15_0 < iLow(NULL, PERIOD_M15, 0) && sa30_0 < iLow(NULL, PERIOD_M30, 0) && sa60_0 < iLow(NULL, PERIOD_H1, 0) && sa240_1 > iHigh(NULL, PERIOD_H4, 1) && sa240_0 < iLow(NULL, PERIOD_H4, 0)) || 
               (sa15_0 < iLow(NULL, PERIOD_M15, 0) && sa30_0 < iLow(NULL, PERIOD_M30, 0) && sa240_0 < iLow(NULL, PERIOD_H4, 0) && sa60_1 > iHigh(NULL, PERIOD_H1, 1) && sa60_0 < iLow(NULL, PERIOD_H1, 0)) ||
               (sa15_0 < iLow(NULL, PERIOD_M15, 0) && sa60_0 < iLow(NULL, PERIOD_H1, 0) && sa240_0 < iLow(NULL, PERIOD_H4, 0) && sa30_1 > iHigh(NULL, PERIOD_M30, 1) && sa30_0 < iLow(NULL, PERIOD_M30, 0)) ||
               (sa30_0 < iLow(NULL, PERIOD_M30, 0) && sa60_0 < iLow(NULL, PERIOD_H1, 0) && sa240_0 < iLow(NULL, PERIOD_H4, 0) && sa15_1 > iHigh(NULL, PERIOD_M15, 1) && sa15_0 < iLow(NULL, PERIOD_M15, 0)))
             {
               ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Green);
               if(ticket > 0)
                {
                  OrderModify(ticket, Ask, Ask - SL * Point * digitKoeff,  Ask + TP * Point * digitKoeff, 0, Green);         
                }  
             }
            if((sa15_0 > iHigh(NULL, PERIOD_M15, 0) && sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa60_0 > iHigh(NULL, PERIOD_H1, 0) && sa240_1 < iLow(NULL, PERIOD_H4, 1) && sa240_0 > iHigh(NULL, PERIOD_H4, 0)) || 
               (sa15_0 > iHigh(NULL, PERIOD_M15, 0) && sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa240_0 > iHigh(NULL, PERIOD_H4, 0) && sa60_1 < iLow(NULL, PERIOD_H1, 1) && sa60_0 > iHigh(NULL, PERIOD_H1, 0)) ||
               (sa15_0 > iHigh(NULL, PERIOD_M15, 0) && sa60_0 > iHigh(NULL, PERIOD_H1, 0) && sa240_0 > iHigh(NULL, PERIOD_H4, 0) && sa30_1 < iLow(NULL, PERIOD_M30, 1) && sa30_0 > iHigh(NULL, PERIOD_M30, 0)) ||
               (sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa60_0 > iHigh(NULL, PERIOD_H1, 0) && sa240_0 > iHigh(NULL, PERIOD_H4, 0) && sa15_1 < iLow(NULL, PERIOD_M15, 1) && sa15_0 > iHigh(NULL, PERIOD_M15, 0)))
             {
               ticket = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Red);
               if(ticket > 0)
                {
                  OrderModify(ticket, Bid, Bid + SL * Point * digitKoeff,  Bid - TP * Point * digitKoeff, 0, Red);    
                }  
             }
          }
       }
      //********************************************************************************************************************* 
      if(sar2 == true && sar3 == true && sar4 == false)
       {            
         sa15_0  = iSAR(NULL, PERIOD_M15, step15, max, 0);
         sa15_1  = iSAR(NULL, PERIOD_M15, step15, max, 1);
         sa30_0  = iSAR(NULL, PERIOD_M30, step30, max, 0);
         sa30_1  = iSAR(NULL, PERIOD_M30, step30, max, 1);
         sa60_0  = iSAR(NULL, PERIOD_H1, step60, max, 0);
         sa60_1  = iSAR(NULL, PERIOD_H1, step60, max, 1);
         //---------------------------------------------   
         if(sa15_0 > sa30_0) saUp = sa15_0;
         else saUp = sa30_0;
         if(saUp > sa60_0) saUp = saUp;
         else saUp = sa60_0;
         //---
         if(sa15_0 < sa30_0) saDn = sa15_0;
         else saDn = sa30_0;
         if(saDn < sa60_0) saDn = saDn;
         else saDn = sa60_0;
         //---
         saDif = (saUp - saDn) / Point;
         //-------------------------------         
         if(saDif <= PointDifferent)
          {
            if((sa15_0 < iLow(NULL, PERIOD_M15, 0) && sa30_0 < iLow(NULL, PERIOD_M30, 0) && sa60_1 > iHigh(NULL, PERIOD_H1, 1) && sa60_0 < iLow(NULL, PERIOD_H1, 0)) ||
               (sa15_0 < iLow(NULL, PERIOD_M15, 0) && sa60_0 < iLow(NULL, PERIOD_H1, 0) && sa30_1 > iHigh(NULL, PERIOD_M30, 1) && sa30_0 < iLow(NULL, PERIOD_M30, 0)) ||
               (sa30_0 < iLow(NULL, PERIOD_M30, 0) && sa60_0 < iLow(NULL, PERIOD_H1, 0) && sa15_1 > iHigh(NULL, PERIOD_M15, 1) && sa15_0 < iLow(NULL, PERIOD_M15, 0)))
             {
               ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Green);
               if(ticket > 0)
                {
                  OrderModify(ticket, Ask, Ask - SL * Point * digitKoeff,  Ask + TP * Point * digitKoeff, 0, Green);         
                }   
             }
            if((sa15_0 > iHigh(NULL, PERIOD_M15, 0) && sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa60_1 < iLow(NULL, PERIOD_H1, 1) && sa60_0 > iHigh(NULL, PERIOD_H1, 0)) ||
               (sa15_0 > iHigh(NULL, PERIOD_M15, 0) && sa60_0 > iHigh(NULL, PERIOD_H1, 0) && sa30_1 < iLow(NULL, PERIOD_M30, 1) && sa30_0 > iHigh(NULL, PERIOD_M30, 0)) ||
               (sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa60_0 > iHigh(NULL, PERIOD_H1, 0) && sa15_1 < iLow(NULL, PERIOD_M15, 1) && sa15_0 > iHigh(NULL, PERIOD_M15, 0)))
             {
               ticket = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Red);
               if(ticket > 0)
                {
                  OrderModify(ticket, Bid, Bid + SL * Point * digitKoeff,  Bid - TP * Point * digitKoeff, 0, Red);    
                }  
             }
          }   
       }
      //********************************************************************************************************************* 
      if(sar2 == true && sar3 == false && sar4 == false)
       {
         sa15_0  = iSAR(NULL, PERIOD_M15, step15, max, 0);
         sa15_1  = iSAR(NULL, PERIOD_M15, step15, max, 1);
         sa30_0  = iSAR(NULL, PERIOD_M30, step30, max, 0);
         sa30_1  = iSAR(NULL, PERIOD_M30, step30, max, 1);
         //---------------------------------------------   
         if(sa15_0 > sa30_0) saUp = sa15_0;
         else saUp = sa30_0;
         //---
         if(sa15_0 < sa30_0) saDn = sa15_0;
         else saDn = sa30_0;
         //---
         saDif = (saUp - saDn) / Point;
         //-------------------------------         
         if(saDif <= PointDifferent)
          {
            if((sa15_0 < iLow(NULL, PERIOD_M15, 0) && sa30_1 > iHigh(NULL, PERIOD_M30, 1) && sa30_0 < iLow(NULL, PERIOD_M30, 0)) ||
               (sa30_0 < iLow(NULL, PERIOD_M30, 0) && sa15_1 > iHigh(NULL, PERIOD_M15, 1) && sa15_0 < iLow(NULL, PERIOD_M15, 0)))
             {
               ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Green);
               if(ticket > 0)
                {
                  OrderModify(ticket, Ask, Ask - SL * Point * digitKoeff,  Ask + TP * Point * digitKoeff, 0, Green);         
                }    
             }
            if((sa15_0 > iHigh(NULL, PERIOD_M15, 0) && sa30_1 < iLow(NULL, PERIOD_M30, 1) && sa30_0 > iHigh(NULL, PERIOD_M30, 0)) ||
               (sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa15_1 < iLow(NULL, PERIOD_M15, 1) && sa15_0 > iHigh(NULL, PERIOD_M15, 0)))
             {
               ticket = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Red);
               if(ticket > 0)
                {
                  OrderModify(ticket, Bid, Bid + SL * Point * digitKoeff,  Bid - TP * Point * digitKoeff, 0, Red);    
                } 
             }
          }   
       }
      //*********************************************************************************************************************
      if(sar2 == false && sar3 == false && sar4 == false)
       {
         sa15_0  = iSAR(NULL, PERIOD_M15, step15, max, 0);
         sa15_1  = iSAR(NULL, PERIOD_M15, step15, max, 1);
         //---------------------------------------------   
         if(sa15_1 > iHigh(NULL, PERIOD_M15, 1) && sa15_0 < iLow(NULL, PERIOD_M15, 0))
          {
            ticket = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Green);
            if(ticket > 0)
             {
               OrderModify(ticket, Ask, Ask - SL * Point * digitKoeff,  Ask + TP * Point * digitKoeff, 0, Green);         
             }   
          }
         if(sa30_0 > iHigh(NULL, PERIOD_M30, 0) && sa15_1 < iLow(NULL, PERIOD_M15, 1) && sa15_0 > iHigh(NULL, PERIOD_M15, 0))
          {
            ticket = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 3 * digitKoeff, 0, 0, "+Psar", MagicNum, 0, Red);
            if(ticket > 0)
             {
               OrderModify(ticket, Bid, Bid + SL * Point * digitKoeff,  Bid - TP * Point * digitKoeff, 0, Red);    
             }  
          }
       }      
    }//************************************************************************************************************************************   
   //======================================================================================================================================  
   double saCloMod_1 = 0, saCloMod_0 = 0;
   total = OrdersTotal();
   for(cnt = 0; cnt < total; cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() <= OP_SELL && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         if(CloseModifyPeriod <= 1) Per = 1;
         else if(CloseModifyPeriod <= 5) Per = 5;
         else if(CloseModifyPeriod <= 15) Per = 15;
         else if(CloseModifyPeriod <= 30) Per = 30;
         else if(CloseModifyPeriod <= 60) Per = 60;
         else if(CloseModifyPeriod <= 240) Per = 240;
         else Per = 1440;   
         //-----   
         saCloMod_1 = iSAR(NULL, Per, stepClo, max, 1);
         saCloMod_0 = iSAR(NULL, Per, stepClo, max, 0);
         //-----------------------         
         if(OrderType() == OP_BUY)
          {                
            if(saCloMod_1 < Low[1] && saCloMod_0 > High[0])
             {
               OrderClose(OrderTicket(), OrderLots(), Bid, 3 * digitKoeff, Violet); 
               return(0); 
             }
            if(saCloMod_0 >= OrderOpenPrice() && saCloMod_0 > OrderStopLoss())
             {               
               OrderModify(OrderTicket(), OrderOpenPrice(), saCloMod_0, OrderTakeProfit(), 0, Green);
               return(0);
             }
          }
         else if(OrderType() == OP_SELL)
          {                
            if(saCloMod_1 > High[1] && saCloMod_0 < Low[0])
             {
               OrderClose(OrderTicket(), OrderLots(), Ask, 3  * digitKoeff, Violet); 
               return(0); 
             }
            if(saCloMod_0 <= OrderOpenPrice() && saCloMod_0 < OrderStopLoss())
             {               
               OrderModify(OrderTicket(), OrderOpenPrice(), saCloMod_0, OrderTakeProfit(), 0, Red);
               return(0);
             }
          }
       }
    }
   return(0);
 }