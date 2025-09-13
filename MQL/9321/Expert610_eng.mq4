//+------------------------------------------------------------------+
//|                                                Haiken Ashi-2.mq4 |
//|                      Copyright © 2009, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2009, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
 
 
extern int Digits2Round = 2;  // Floating point rounding
 
extern int PercentOfFreeDepo = 1; // Risk Percent of depo
 
extern double MinLot = 0.10; // minimal lot for trading, if calculated lot is less than minimal (it depends on the equity)
 
extern int MagicNumber = 888; // This is magic number for the expert,
                              // It opens, modify and deletes orders with this MagicNumber
                              
extern double Threshold = 0.00050;  // Threshold for the pending order sending
 
extern double SL = 0.00050; // StopLoss
extern double TP = 0.00100; // TakeProfit
extern double P  = 0.00020; // Breakout points
 
 
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
  int ticket;
  int err;
  int q=0;
 
double L=iLow(NULL,NULL,1);
double H=iHigh(NULL,NULL,1);
double C=iClose(NULL,NULL,0);
double O=iOpen(NULL,NULL,0);
double Spread=MarketInfo(Symbol(),MODE_SPREAD)*Point;
double FreeDepo=NormalizeDouble(AccountBalance()-AccountMargin(),Digits2Round);
double Risk=NormalizeDouble((FreeDepo*PercentOfFreeDepo/100),Digits2Round);
double Lot=NormalizeDouble(Risk/(SL/0.0001)*0.1,Digits2Round);
double Check1=H-C;
double Check2=C-L;
 
//===================== Lets determine lot size and risk ===================================
 
if ( Lot < MinLot )
 {
   Lot=MinLot;
 }
Comment( "\n","Acceptable risk is ",PercentOfFreeDepo, "% = ",Risk," of the free money ",FreeDepo," in lots = ",Lot);
 
//====================== checking for the orders opening
 for( q=0;q<OrdersTotal();q++)
 {
  if (OrderSelect(q, SELECT_BY_POS, MODE_TRADES) && OrderSymbol()==Symbol())
   {
// checking positions, if there are some opended orders, lets check them with the indicator
   if (OrderType()==OP_BUYSTOP)
     {
       return(0); 
     }
   if (OrderType()==OP_SELLSTOP)
     {
       return(0); 
     }
   }  
 }
 
 
//======================= condition for ORDER BUY ===============================
 
if  (Check1 >= Threshold && Check2 >= Threshold && O<H)     
  {
    ticket=OrderSend(Symbol(),OP_BUYSTOP,Lot,H+P+Spread,0,H+P-SL+Spread,H+P+TP+Spread,NULL,0,iTime( Symbol(), PERIOD_D1, 0 ) + 86400);
    if (ticket==-1)
      {
        err=GetLastError();
        Print("error(",err,")");
      }
  }
else 
  {
    Comment("\n","Cannot set OP_BUYSTOP",
            "\n","The price is not satisfied to the market entry condition");
  }     
  
   //================================ condition for ORDER SELL ==================== 
if  (Check1 >= Threshold && Check2 >= Threshold && O>L)  
  {   
    ticket=OrderSend(Symbol(),OP_SELLSTOP,Lot,L-P,0,L-P+SL,L-P-TP,NULL,0,iTime( Symbol(), PERIOD_D1, 0 ) + 86400);
    if (ticket==-1)
      {
         err=GetLastError();
         Print("error(",err,")");
      }
  }
else 
  {
    Comment("\n","Cannot set OP_SELLSTOP",
            "\n","The price is not satisfied to the market entry condition");
  }     
}
//+------------------------------------------------------------------+