//+------------------------------------------------------------------+
//|                                           Catcher Profit 1.0.mq4 |
//|                                                    Dottor Market |
//|                                                             2013 |
//+------------------------------------------------------------------+
#property copyright "Dottor Market"
#property link      "www.facebook.com/ForexExchange"

extern string Condition          = "If total profit is higher";
extern double MaximumProfit      = 200.00;
extern bool   Percentage         = false;
extern double MaximumPercentage  = 2.00;
extern string Action             = "Close trades!";

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {

   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {

   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {

   StampaParametri();
   
   
   if(Percentage == false && OrdersTotal() > 0 && AccountEquity()-AccountBalance() > MaximumProfit)
   CloseAll();
   
   if(Percentage == true && OrdersTotal() > 0 && ((AccountEquity()-AccountBalance())/AccountBalance())*100 > MaximumPercentage)
   CloseAll();

   return(0);
  }
//+------------------------------------------------------------------+


void CloseAll() {
 
     int Ordini = OrdersTotal();
     int i = 0;
     for(i = Ordini; i >=0; i--)
     {
     OrderSelect(0,SELECT_BY_POS,MODE_TRADES);
     double bid = MarketInfo(OrderSymbol(),MODE_BID);
     double ask = MarketInfo(OrderSymbol(),MODE_ASK);
     
     if(OrderType()==OP_BUY)   OrderClose(OrderTicket(),OrderLots(),Bid,5,CLR_NONE);
     if(OrderType()==OP_SELL)  OrderClose(OrderTicket(),OrderLots(),Ask,5,CLR_NONE);
     }
}


void StampaParametri() {
   string cmt="";
      cmt = "\n";
      cmt = "========================";
      cmt =  cmt + "\nDottor Market - Catcher Profit 1.0";
      cmt =  cmt + "\nwww.facebook.com/ForexExchange";
      cmt =  cmt + "\n========================";
      if(!Percentage)
      cmt =  cmt + "\nMaximum Profit :           [ " + AccountCurrency() +" "+ DoubleToStr(MaximumProfit,2) + " ]";
      if(Percentage)
      {
      cmt =  cmt + "\nPercentage :                 [ " + "True" + " ]";
      cmt =  cmt + "\nMaximumPercentage :    [ " + DoubleToStr(MaximumPercentage,2) +" ]";
      }
      if(!Percentage)
      cmt =  cmt + "\nPercentage :                 [ " + "False" + " ]";
      cmt =  cmt + "\n========================" +"\n";
            
      Comment(cmt);
}

