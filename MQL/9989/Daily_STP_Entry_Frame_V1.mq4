//+--------------------------------------------------------------------+
//|                      Daily STP Entry Frame                         |
//|                   Copyright © 2010, Cheftrader                     |
//|  If you use this EA to build your live account EA, please donate   |
//|   USD 9.99 or EUR 6.99 via www.paypal.com and send the money to    |
//|                     cheftrader@moneymail.de                        |
//|                      USE AT YOUR OWN RISK                          |
//+--------------------------------------------------------------------+

/*
This EA uses Stop-Orders to enter a position. The orders are placed at days opening
based on a function signal_STP_Entry(...) in a separate file. The standard file simply
sets the stoplevel at yesterdays high or low.
The pending stoporders are deleted, if they are not filled untill the evening.
Filled orders/positions are closed by either the stoploss or the takeprofit,
postions are not closed by any signal.
Hence it is possible that a position remains open overnight / or over the weekend.
Even if a postion is open, new pending stop orders are placed at the opening of the next day.  
Test/optimize separately for short and long system.
Trailing stops can be enabled [SLslope < 1.0] or disabled.
Test behaviore on dedicated days of the week with the dayfilter.
Test first without position management [MaxLot = MinLot] and determine later the optimal PercentofProfit parameter.

Orders are deleted (via init-function), if you change the period of the chart in which you have placed
the EA - this is done to prevent double pending orders, if you change the EA-Inputs or if the server reconnects and the EA re-inits.
*/

/// general parameter
#define MagicNumber  999                
#include <Signal YesterdaysHighLow V1.mqh> 
#define LONG     1
#define BOTH     0
#define SHORT    -1
#define allday   6

/// risk management - stoploss, takeprofit and trailing stop parameter  
extern double SL      = 8;   // StopLoss in Basepoints: 1/10000 or 100/10000 = 1/100 for JPY
extern double TP      = 20.5;// TakeProfit in Basepoints
extern double SLslope = 0.8; // Trailing stop uses only a part [e.g. 0.8] of the reached trade profit.
                             // If > 1.0 trailing stops are deactivated
                             
extern int    side = SHORT;  //LONG = 1, SHORT = -1, place orders in both directions: BOTH
/// money management - position size parameter  
extern int PercentOfProfit   = 30;  // Risk [in %] of already reached Profit in Account,
                                    // used to calculate position size
double MinLot = 0.1;                // minimal lot for trading
extern double MaxLot = 10.0;        // maximal lot for trading
int LotDIGIT = 1;                   // digits of lotsize 1: 0.1, 2: 0.01

/// money management - limit draw down 
double startequity;                 // init as accountbalance
double maxequity;                   // init as accountbalance
double maxDD = 50.0;                // max Drawdown: No new pending orders, if Equity sinks below 100%-maxDD% of maximum Equity
double sendmaillastequity, sendmailtreshold = 50;       // SendMail, if equitychange is greater than treshold;

/// time filter
int NoNewPendingOrders_Hour = 19;        // delete pending orders after this hour
int NoNewPendingOrders_Hour_Friday = 19; // special on friday: delete pending orders after this hour
extern int dayfilter = allday;           // place pending oders alldays = 6 or only on dayofweek 1 (monday)...5 (friday)
int hourfilter = 0;                      // don't place orders before hour
int closetimeperiod = 0;              // close open position after closetimeperiod [s]; if = 0 : don't close

//// symbol parameter
string tsymbol          = "";
int    tsymbolPERIOD    = PERIOD_D1;
double tsymbolSLIPPAGE  = 3;  // in Points
double tsymbolTICKVALUE, tsymbolPOINT, tsymbolDIGITS, tsymbolSTOPLEVEL, tsymbolLOTSIZE;

//// other variables
#property copyright "Copyright © 2010, Cheftrader"
#include <stdlib.mqh>                       // used to generate error messages
int lastplaceBUYorder  =0;                  // variable used to determine, if a an order has been placed today
int lastplaceSELLorder =0;
#define friday   5
#define saturday 6    
#define sunday   0



//+------------------------------------------------------------------+
int init()
  {
      
   if(tsymbol == "") tsymbol = Symbol();  
   
   startequity       = MathMin(AccountBalance(),AccountEquity());
   maxequity         = startequity;
   sendmaillastequity= startequity;
   
   tsymbolTICKVALUE  = MarketInfo(tsymbol,MODE_TICKVALUE);
   tsymbolDIGITS     = MarketInfo(tsymbol,MODE_DIGITS);
   tsymbolPOINT      = MarketInfo(tsymbol,MODE_POINT);  
   if(tsymbolDIGITS==3 || tsymbolDIGITS ==5) tsymbolPOINT=10*tsymbolPOINT; //tsymbolPOINT is valued as Basepoint = 1/10000 or 100/10000=1/100 for JPY
   tsymbolSTOPLEVEL  = MarketInfo(tsymbol,MODE_STOPLEVEL);
   tsymbolLOTSIZE    = MarketInfo(tsymbol,MODE_LOTSIZE);
    
   Print("Start ", systemname+ "_" + tsymbol);
   SendMail(systemname+MagicNumber, "Startequity: "+DoubleToStr(sendmaillastequity,0));
   for(int q=OrdersTotal()-1;q>=0;q--)
   {
    OrderSelect(q, SELECT_BY_POS, MODE_TRADES);
    Print("Delete Old Pending Trades");
    if(OrderSymbol() == tsymbol)
      if(OrderMagicNumber() == MagicNumber && (OrderType() == OP_BUYSTOP || OrderType() == OP_SELLSTOP || OrderType() == OP_BUYLIMIT || OrderType() == OP_SELLLIMIT))
         if(!OrderDelete(OrderTicket()))
            Print(ErrorDescription(GetLastError()));
    }  

   return(0);
  }

//+------------------------------------------------------------------+
int deinit()
  {
   int q;
   
   for( q=OrdersTotal()-1;q>=0;q--)
   {
    OrderSelect(q, SELECT_BY_POS, MODE_TRADES);
    Print("Delete Pending Trades");
    if(OrderSymbol() == tsymbol)
      if(OrderMagicNumber() == MagicNumber && (OrderType() == OP_BUYSTOP || OrderType() == OP_SELLSTOP || OrderType() == OP_BUYLIMIT || OrderType() == OP_SELLLIMIT))
         if(!OrderDelete(OrderTicket()))
            Print(ErrorDescription(GetLastError()));
   }         
   Print("Deinit. Current spread: "+DoubleToStr(MarketInfo(tsymbol,MODE_SPREAD),0));   // Current live spread is used by tester - hence test results differ !
   
   return(0);
  }

//+------------------------------------------------------------------+
int start()
{
 //====================== check existing orders and positions
 double localSL, tsymbolBID, tsymbolASK;
 tsymbolBID         = MarketInfo(tsymbol,MODE_BID);
 tsymbolASK         = MarketInfo(tsymbol,MODE_ASK);
 
 for(int q=0;q<OrdersTotal();q++)
 {
  if (OrderSelect(q, SELECT_BY_POS, MODE_TRADES) && OrderSymbol()==tsymbol)
   {

// delete old pending orders
   if ( (OrderType()==OP_BUYSTOP  || OrderType()==OP_SELLSTOP)
          && OrderMagicNumber() == MagicNumber   )
    {
       // Delete PendingOrders in the evening
       if(   (TimeHour(TimeCurrent()) >= NoNewPendingOrders_Hour) ||
             ((TimeHour(TimeCurrent()) >=NoNewPendingOrders_Hour_Friday) && DayOfWeek() == friday) )
         {
            if(OrderDelete(OrderTicket()) < 0)
                 Print(ErrorDescription(GetLastError()));
         }
       // Redundant: PendingOrder nicht von heute, wird gelöscht
       if(TimeDayOfWeek(OrderOpenTime()) != DayOfWeek())
       {
          if(OrderDelete(OrderTicket()) < 0)
                 Print(ErrorDescription(GetLastError()));
       }   
    }
 /////////////////////////////////////////////////////////   
 // Close Order after timeperiod - close buy
   if ( OrderType()==OP_BUY && OrderMagicNumber() == MagicNumber && closetimeperiod != 0)
    {
       if(TimeCurrent() - OrderOpenTime() > closetimeperiod)
       {
           if(OrderClose(OrderTicket(),OrderLots(),tsymbolBID, tsymbolSLIPPAGE,Black) < 0)
                  Print(ErrorDescription(GetLastError()));
       }  
    }
  
 // Close Order after timeperiod - close sell
   if ( OrderType()==OP_SELL &&  OrderMagicNumber() == MagicNumber && closetimeperiod != 0)
    {
       if(TimeCurrent() - OrderOpenTime() > closetimeperiod)
       {
           if(OrderClose(OrderTicket(),OrderLots(),tsymbolASK, tsymbolSLIPPAGE,Black) < 0)
                  Print(ErrorDescription(GetLastError()));
       }  
 }
/////////////////////////////////////////////////////////   
// Trailing Stop - BUY
   if ( OrderType()==OP_BUY && OrderMagicNumber() == MagicNumber)
    {
       if(OrderProfit() > 0 && SLslope < 1.0 )
       {
         localSL = tsymbolBID - SL*tsymbolPOINT - SLslope* (tsymbolBID-OrderOpenPrice());
         if(localSL > (OrderStopLoss()+1.1*tsymbolPOINT))
            if((tsymbolBID-tsymbolSTOPLEVEL) > localSL)
            {
               if(OrderModify(OrderTicket(),tsymbolASK,NormalizeDouble(localSL,tsymbolDIGITS),OrderTakeProfit(),0,Red) < 0)
                  Print(ErrorDescription(GetLastError()));
            }   
       }  
    }
  
 // Trailing Stop - SELL
   if ( OrderType()==OP_SELL &&  OrderMagicNumber() == MagicNumber )
    {
       if(OrderProfit() > 0 && SLslope < 1.0)
       {
         localSL = tsymbolASK + SL*tsymbolPOINT + SLslope* (OrderOpenPrice() - tsymbolASK);
         if(localSL < (OrderStopLoss()-1.1*tsymbolPOINT))
            if((tsymbolASK+tsymbolSTOPLEVEL) < localSL)
            {
               if(OrderModify(OrderTicket(),tsymbolBID,NormalizeDouble(localSL,tsymbolDIGITS),OrderTakeProfit(),0,Green) < 0)
                     Print(ErrorDescription(GetLastError()));
            }   
       }  
    }
   }  
 }
 
 
//// Place Pending Orders 
// DrawDown-Filter
  double equity = AccountEquity();
  if(equity > sendmaillastequity + sendmailtreshold)
  {
       sendmaillastequity = equity;
       SendMail(systemname+MagicNumber, "Equity increased to: "+DoubleToStr(sendmaillastequity,0));
  }
  if(equity < sendmaillastequity + sendmailtreshold)
  {
       sendmaillastequity = equity;
       SendMail(systemname+MagicNumber, "Equity increased to: "+DoubleToStr(sendmaillastequity,0));
  }
  maxequity = MathMax(maxequity,equity);
  //Check #1: If maxDrawdown has occured: no new orders
  if(equity < (100-maxDD)/100*maxequity) return(0);

//// Timefilter
   //Check #2: If holiday: no new orders
   if(DayOfWeek()==sunday || DayOfWeek()==saturday) return(0);
   //Check #3: If dayfilter is set: no new orders
   if(dayfilter != DayOfWeek() && dayfilter != allday) return(0);
   //Check #4: If dayfilter is set: no new orders
   if(TimeHour(TimeCurrent()) < hourfilter) return(0);    

//Determine LotSize
double Profit     =  AccountBalance()-startequity;
double Risk       =  Profit*PercentOfProfit/100;
double Lot        =  NormalizeDouble(Risk/(tsymbolTICKVALUE*(SL+tsymbolSLIPPAGE)*tsymbolPOINT*tsymbolLOTSIZE),1);

if(Lot > 0) Lot   =  NormalizeDouble(MathSqrt(Lot)-0.1,LotDIGIT);
if ( Lot < MinLot )
 {
   Lot=MinLot;
 }
 if ( Lot > MaxLot )
 {
   Lot=MaxLot;
 }
//////////////////// 
 
//======================= condition for ORDER OPEN BUYSTOP ===============================

if(side == LONG  || side == BOTH)
//Check #5: If BUYSTOP orders have been placed today 
if(lastplaceBUYorder != DayOfYear())    
{
    double buystop = signal_STP_Entry(tsymbol, tsymbolPERIOD, LONG); //if the function returns a value <= 0, no entry order will be placed
    if(buystop <= 0) return(0);  //condition not met
    if(buystop <= tsymbolASK + tsymbolSTOPLEVEL) return(0);
    if(OrderSend(tsymbol,OP_BUYSTOP,Lot,NormalizeDouble(buystop,tsymbolDIGITS),tsymbolSLIPPAGE,NormalizeDouble(buystop-SL*tsymbolPOINT,tsymbolDIGITS),NormalizeDouble(buystop+TP*tsymbolPOINT,tsymbolDIGITS),systemname,MagicNumber,0,Green) < 0)
       Print(ErrorDescription(GetLastError()));
    else
       lastplaceBUYorder = DayOfYear();
}

//================================ condition for ORDER OPEN SELLSTOP ==================== 
if(side == SHORT  || side == BOTH)
if(lastplaceSELLorder != DayOfYear())  
 {   
    double sellstop = signal_STP_Entry(tsymbol, tsymbolPERIOD, SHORT); //if the function returns a value <= 0, no entry order will be placed
    if(sellstop <= 0) return(0);  //condition not met
    if(sellstop >= tsymbolBID - tsymbolSTOPLEVEL) return(0);
    if(OrderSend(tsymbol,OP_SELLSTOP,Lot,NormalizeDouble(sellstop,tsymbolDIGITS),tsymbolSLIPPAGE,NormalizeDouble(sellstop+SL*tsymbolPOINT,tsymbolDIGITS),NormalizeDouble(sellstop-TP*tsymbolPOINT,tsymbolDIGITS),systemname,MagicNumber,0,Red) < 0)
      Print(ErrorDescription(GetLastError()));
    else
      lastplaceSELLorder = DayOfYear();
  }
}
//+--------------------------------------------------------------------+
//|                      Daily STP Entry Frame                         |
//|                   Copyright © 2010, Cheftrader                     |
//|  If you use this EA to build your live account EA, please donate   |
//|   USD 9.99 or EUR 6.99 via www.paypal.com and send the money to    |
//|                     cheftrader@moneymail.de                        |
//|                      USE AT YOUR OWN RISK                          |
//+--------------------------------------------------------------------+



