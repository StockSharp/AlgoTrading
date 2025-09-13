//+------------------------------------------------------------------+
//|                                             Bullish Reversal.mq4 |
//|                                                               ar |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "ar"
#property link      "http://www.metaquotes.net"

extern double Lots=1;
extern double StopLoss=0;
extern double TakeProfit=0;
extern double TrailingStop=50;
extern int UseTrailingStop=1;
int ThisBarTrade=0;
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
//----
// int quick variables 
   int ticket,total;
// check sample size   
if(Bars<50)
   { 
    Print("Sample Size Too Small");
    return(0);
    } 
if(OrdersTotal()<1) // check if there are any postions open
   {
   if (Bars != ThisBarTrade ) 
    {
      ThisBarTrade = Bars;  // ensure only one trade opportunity per bar
   
     if(AccountFreeMargin()<1000*Lots) // check margin level 
        {
          Print("Free Margin = ",AccountFreeMargin());
         return(0);
         }
// seting up quick variables for candle stick parts 
double high1=iHigh(NULL,0,1);
double close1=iClose(NULL,0,1);
double open1=iOpen(NULL,0,1);
double low1=iLow(NULL,0,1);
double high2=iHigh(NULL,0,2);
double close2=iClose(NULL,0,2);
double open2=iOpen(NULL,01,2);
double low2=iLow(NULL,0,2);
double high3=iHigh(NULL,0,3);
double close3=iClose(NULL,0,3);
double open3=iOpen(NULL,0,3);
double low3=iLow(NULL,01,3);
double MA=iMA(NULL,0,50,0,MODE_SMA,PRICE_CLOSE,0);
// set up partern names
// strong Indicator
bool abandoned_baby=((open3>close3)&&(open2>close2)&&(low2<low3)&&(open1<close1)&&(low1>=low2)&&(close1>open3));
bool morning_doji_star=((open3>close3)&&((open2-close2)<=0)&&(open1<close3)&&(close1<open3));
bool three_inside_up=((open3>close3)&&(MathAbs(close2-open2))<=0.6*MathAbs(open3-close3)&&(close2>open2)&&(close1>open1)&&(close1>open3));
bool three_outside_up=((open3>close3)&&(1.1*MathAbs(open3-close3)<MathAbs(open2-close2))&&(open2<close2)&&(open1<close1));
bool three_white_soldiers=((open3<close3)&&(open2<close2)&&(open1<close1)&&(close3<close2<close1));
//----
bool hammer=((MathAbs(low1-MathMin(open1,close1))>=2*(MathAbs(open1-close1)))&&((MathAbs(high1-MathMax(open1,close1)))<=0.2*(MathAbs(close1-open1))));
// set up algo
if ((abandoned_baby==true||morning_doji_star==true||three_inside_up==true||three_outside_up==true||three_white_soldiers==true)&&open1<MA)
    {
       ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"BF",16384,0,Green);
       if(ticket>0) // if order can be filled 
           {
              if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES))// if order is executed 
               Print("BUY order opened : ",OrderOpenPrice());//     then notifiy the trader that it was executed.
            }
        return(0);
     }
     return(0);
    }
  return(0);
  }
//Trailing stop
if(TrailingStop>0)
  {
   if(Bid-OrderOpenPrice()> Point*TrailingStop) // if the is a chance of lossing more than trailing stop 
      {
        if(OrderStopLoss()< Bid-Point*TrailingStop) // amd if a trailing stop would work
        {
          OrderModify(OrderTicket(), OrderOpenPrice(), Bid-Point*TrailingStop, OrderTakeProfit(),0,Orange);
         }
       }
      return(0);
     }
  return(0);
}
//+------------------------------------------------------------------+