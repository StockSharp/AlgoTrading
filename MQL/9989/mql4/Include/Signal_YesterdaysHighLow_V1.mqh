//+--------------------------------------------------------------------+
//|               Signalgeneration for Daily STP Entry Frame           |
//|                      Copyright © 2010, Cheftrader                  |
//|     If you want to donate, visit www.paypal.com and send money to  |
//|                     cheftrader@moneymail.de                        |
//|                      USE AT YOUR OWN RISK                          |
//+--------------------------------------------------------------------+
//|                                                                    |
//| Having a better approach for signalgeneration ?  Please tell me:   |
//|                     cheftrader@moneymail.de                        |
//+--------------------------------------------------------------------+

// the return value is a signal and a stp-entry value at the same time:
// return <=0: no signal,
// else the return value is the stp-entry value

// THE STP-ENTRY VALUE IS YESTERDAYS LOW - HALF OF SPREAD


#property copyright "Copyright © 2010, Cheftrader"
#property link      ""

string systemname = "BreakoutHighLow";
double Threshold = 5; // Threshold for the pending order sending
double SPREAD    = 3; // approximate Spread in Points, considered in entry stop calculation


double signal_STP_Entry(string Tsymbol, int TsymbolPERIOD, int side)
{
   // int side: -1=long/buy, -1=short/sell
   double TsymbolBID, TsymbolASK,TsymbolPOINT,TsymbolDIGITS;
   int shift = 1;
  
   if (TimeDayOfWeek(iTime(Tsymbol, TsymbolPERIOD, shift))== 0)
   {
     shift = shift+1; //if bar n-1 = sunday, shift the idata() functions by an additional bar
   }  

   double L =  iLow(Tsymbol,TsymbolPERIOD,shift);
   double H =  iHigh(Tsymbol,TsymbolPERIOD,shift);
   double C =  iClose(Tsymbol,TsymbolPERIOD,0);
   double O =  iOpen(Tsymbol,TsymbolPERIOD,0);
   TsymbolBID        = MarketInfo(Tsymbol,MODE_BID);
   TsymbolASK        = MarketInfo(Tsymbol,MODE_ASK);
   TsymbolDIGITS     = MarketInfo(Tsymbol,MODE_DIGITS);
   TsymbolPOINT      = MarketInfo(Tsymbol,MODE_POINT);  
   if(TsymbolDIGITS==3 || TsymbolDIGITS ==5) TsymbolPOINT=10*TsymbolPOINT; //TsymbolPOINT is valued as Basepoint = 1/10000 or 100/10000=1/100 for JPY
     
   /// buy condition
   if(H-C >= Threshold*TsymbolPOINT  && //last quote differs from yesterdays high by Treshold
      O < H   &&                        //day open is below yesterdays high
      side==1)                          //buyfilter
     return(H+0.5*SPREAD*TsymbolPOINT);
   if(C-L >= Threshold*TsymbolPOINT  && //last quote differs from yesterdays low by Treshold
    O >= L   &&                         //day open is above yesterdays low
    side==-1)                           //sell
     return(L-0.5*SPREAD*TsymbolPOINT);    
return(-1);   //if the function returns a value <= 0, no entry order will be placed
}
//+--------------------------------------------------------------------+
//|                      Daily STP Entry Frame                         |
//|                      Copyright © 2010, Cheftrader                  |
//|     If you want to donate, visit www.paypal.com and send money to  |
//|                     cheftrader@moneymail.de                        |
//|                      USE AT YOUR OWN RISK                          |
//+--------------------------------------------------------------------+