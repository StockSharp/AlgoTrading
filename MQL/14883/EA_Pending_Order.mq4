//+------------------------------------------------------------------+
//|                                             EA Pending Order.mq4 |
//|                        Copyright 2016, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2016, WidiPramana."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

extern string  Name_EA                 = "PendingOrder";
extern int     Start_Hour              = 6;
extern int     End_Hour                = 20;
extern int     TP                      = 20;
extern int     SL                      = 100;
extern double  Lots                    = 0.01;
extern int     Distance                = 15;
extern int     Magic                   = 69;

double slb,tpb,sls,tps,pt;
int res,wt,wk,tiket,ticet;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   if(Digits==3 || Digits==5) pt=10*Point;   else   pt=Point;
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
label();
  
   if(Hour_trade()==1){
      if(totalorder(2)==0){res=OrderSend(Symbol(), OP_BUYLIMIT,NR(Lots), Ask-Distance*Point, 3, Ask-Distance*Point-SL*Point,Ask-Distance*Point+TP*Point, "", Magic, 0, Blue);}
      if(totalorder(3)==0){res=OrderSend(Symbol(), OP_SELLLIMIT,NR(Lots) , Bid+Distance*Point, 3, Bid+Distance*Point+SL*Point,Bid+Distance*Point-TP*Point, "", Magic, 0, Red);}
      if(totalorder(4)==0){res=OrderSend(Symbol(), OP_BUYSTOP,NR(Lots) , Ask+Distance*Point, 3, Ask+Distance*Point-SL*Point,Ask+Distance*Point+TP*Point, "", Magic, 0, Blue);}
      if(totalorder(5)==0){res=OrderSend(Symbol(), OP_SELLSTOP,NR(Lots) , Bid-Distance*Point, 3, Bid-Distance*Point+SL*Point,Bid-Distance*Point-TP*Point, "", Magic, 0, Red);}
     }
   return(0);
  }
//+------------------------------------------------------------------+

int Hour_trade()
{
   bool trade = false;
   if(Start_Hour > End_Hour){
     if (Hour() >= Start_Hour || Hour() < End_Hour) trade = true;
   } else
     if (Hour() >= Start_Hour && Hour() < End_Hour) trade = true;

   return (trade);
}

int totalorder( int tipe)
{
int total=0;
for(int i=0; i<OrdersTotal(); i++)
  {
      if(!OrderSelect(i,SELECT_BY_POS,MODE_TRADES)) continue;
      if(OrderSymbol()!=Symbol() || OrderMagicNumber()!=Magic || OrderType()!=tipe) continue;
     total++;
  }

return(total);
}
double NR(double thelot)
{
    double maxlots = MarketInfo(Symbol(), MODE_MAXLOT),
    minilot = MarketInfo(Symbol(), MODE_MINLOT),
    lstep = MarketInfo(Symbol(), MODE_LOTSTEP);
    double lots = lstep * NormalizeDouble(thelot / lstep, 0);
    lots = MathMax(MathMin(maxlots, lots), minilot);
    return (lots);
}

void label()
{
 Comment("\n ",
   "\n ",
   "\n ------------------------------------------------",
   "\n :: Pending+Order",
   "\n ------------------------------------------------",
   "\n :: Spread                 : ", MarketInfo(Symbol(), MODE_SPREAD),
   "\n :: Leverage               : 1 : ", AccountLeverage(),
   "\n :: Equity                 : ", AccountEquity(),
   "\n :: Hour Server             :", Hour(), ":", Minute(),
   "\n ------------------------------------------------");
}
