//+------------------------------------------------------------------+
//|                                                      Wipeout.mq4 |
//|                            Copyright © 2011, www.FxAutomated.com |
//|                                       http://www.FxAutomated.com |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2011, www.FxAutomated.com"
#property link      "http://www.FxAutomated.com"

//---- input parameters
extern string    Product="Wipeout v1";
extern double    CloseAllWhenProfitIs=300;
extern int       Slippage=5;
extern string    MoreAvailableAt="www.FxAutomated.com";
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+

int start()
  {
//----
int StopMultd;
 int digits=MarketInfo("EURUSD",MODE_DIGITS);
if(digits==5){StopMultd=10;} else{StopMultd=1;}
int Slip=Slippage*StopMultd;
double CurrentProfit=NormalizeDouble(AccountEquity()-AccountBalance(),Digits);
  
if(CurrentProfit>=CloseAllWhenProfitIs){ // start check
 if(OrdersTotal()>0){
  for(int i=1; i<=OrdersTotal(); i++)          // Cycle searching in orders
     {
      if (OrderSelect(i-1,SELECT_BY_POS)==true) // If the next is available
        {
           if(OrderType()==OP_BUY){OrderClose(OrderTicket(),OrderLots(),Bid,Slip,CLR_NONE);}
           if(OrderType()==OP_SELL){OrderClose(OrderTicket(),OrderLots(),Ask,Slip,CLR_NONE);}
           
           int Error=GetLastError();
           if(Error>0){
           if(Error==2){Alert("Common error.");}
           if(Error==146){Alert("Trading subsystem is busy. Retrying."); Sleep(500); RefreshRates();}

           }
        }
     }
 }
}// start check   
//----
   return(0);
  }
//+------------------------------------------------------------------+