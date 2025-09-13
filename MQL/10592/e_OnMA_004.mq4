/*------------------------------------------------------------------+
 |                                                   e_OnMA_004.mq4 |
 |                                                 Copyright © 2012 |
 |                                             basisforex@gmail.com |
 +------------------------------------------------------------------*///   01/21/12
#property copyright "Copyright © 2012, basisforex@gmail.com"
#property link      "basisforex@gmail.com"
//----------------------------------------
#include <stderror.mqh>
#include <stdlib.mqh>
//-------------------
#define MagicNum  101
//------
extern  int       maPeriod_1    =   7;
extern  int       maMethod_1    =   0;//1; //MODE_SMA=0; MODE_EMA=1; MODE_SMMA=2;MODE_LWMA=3;
extern  int       maAppPrice_1  =   2; //PRICE_CLOSE=0; PRICE_OPEN=1; PRICE_HIGH=2; PRICE_LOW=3; PRICE_MEDIAN=4; PRICE_TYPICAL=5; PRICE_WEIGHTED=6;
//----
extern  int       maPeriod_2    =   5;
extern  int       maMethod_2    =   0;//1; //MODE_SMA=0; MODE_EMA=1; MODE_SMMA=2;MODE_LWMA=3;
extern  int       maAppPrice_2  =   3; //PRICE_CLOSE=0; PRICE_OPEN=1; PRICE_HIGH=2; PRICE_LOW=3; PRICE_MEDIAN=4; PRICE_TYPICAL=5; PRICE_WEIGHTED=6;
//----
extern  double    Prof          =   25.0;
//----
extern  int       TP            =   50;
extern  int       SL            =   550;
extern  bool      UseMM         =   false;
extern  double    PercentMM     =   2;
extern  double    Lots          =   0.1;
//+-----------------------------------------------------------------+
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
int CalculateCurrentOrders()
 {
   int orderT = OrdersTotal(), buys = 0, sells = 0;
   for(int i = 0; i < orderT; i++)
    {
      if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) break;
      if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         if(OrderType() == OP_BUY)  buys++;
         if(OrderType() == OP_SELL) sells++;
       }
    }
   //----
   if(buys > 0) return(buys);
   else if(sells > 0) return(-sells);
   else return(0);
 }
//+------------------------------------------------------------------+ 
double GetLots()
 { 
   if (UseMM)
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
   else return(Lots);
 }
//+------------------------------------------------------------------+
void CheckForClose()
 {
   for(int cnt = 0; cnt < OrdersTotal(); cnt++)
    {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType() <= OP_SELL && OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNum)
       {
         if(OrderType() == OP_BUY && OrderProfit() + OrderSwap() >= Prof)
          {
            OrderClose(OrderTicket(), OrderLots(), Bid, 1, Violet);             
          }
         else if(OrderType() == OP_SELL && OrderProfit() + OrderSwap() >= Prof)
          {
            OrderClose(OrderTicket(), OrderLots(), Ask, 1, Violet);            
          }
       }
    }
 }
//+-------------------------------------------------------------------+
void CheckForOpen()
 {
   double maHigh0, maHigh1, maLow0, maLow1;
   int n, res, tot;
   int d = Get_Broker_Digit();
   tot = OrdersHistoryTotal();
   for(n = tot; n >= 0; n--)
    {
      OrderSelect(n, SELECT_BY_POS, MODE_HISTORY);
      if(OrderSymbol() == Symbol() && OrderType() <= OP_SELL && OrderMagicNumber() == MagicNum)
       {
         break;
       }
    }
   if(iBarShift(NULL, 0, OrderCloseTime()) > 0)
    {
     //----------------------------------------------------------------
      maHigh0 = iMA(NULL, 0, maPeriod_1, 0, maMethod_1, maAppPrice_1, 0);
      maHigh1 = iMA(NULL, 0, maPeriod_1, 0, maMethod_1, maAppPrice_1, 1);
      maLow0 =  iMA(NULL, 0, maPeriod_2, 0, maMethod_2, maAppPrice_2, 0); 
      maLow1 =  iMA(NULL, 0, maPeriod_2, 0, maMethod_2, maAppPrice_2, 1); 
      //----------------------------------------------------------------
      if(Close[1] < maLow1 && Bid < maLow0)  
       {
         res = OrderSend(Symbol(), OP_SELL, GetLots(), Bid, 2 * d, 0, 0, "onMA4", MagicNum, 0, Red); 
         if(res > 0)
          {
            OrderModify(res, Bid, Bid + SL * Point * d,  Bid - TP * Point * d, 0, Red);
          }   
       }  
      //----
      if(Close[1] > maHigh1 && Ask > maHigh0)  
       {
         res = OrderSend(Symbol(), OP_BUY, GetLots(), Ask, 2 * d, 0, 0, "onMA4", MagicNum, 0, Green); 
         if(res > 0)
          {
             OrderModify(res, Ask, Ask - SL * Point * d, Ask + TP * Point * d, 0, Green);
          }
       }      
    }
 }   
//+------------------------------------------------------------------+
void start()
 {                 
/*   if (Symbol() != "EURUSD")
    {
		Comment("Not a right Symbol: ", Symbol(), " <>  EURUSD");
		return(0);
	 }                           
   if (Period() != 15)
    {
		Comment("Not a right Period!!! It should be M15");
		return(0);	
	 }                */
   if(CalculateCurrentOrders() != 0) CheckForClose();
   if(CalculateCurrentOrders() == 0) CheckForOpen();
   int check;
   check = GetLastError();
   if(check != 0) Print("Error: ", ErrorDescription(check));
 }