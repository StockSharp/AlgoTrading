//|$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
//  Multi Levels Trend Expert Advisor
//  hodhabi@gmail.com
//|$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$$
#define     NL    "\n" 

extern double Lots = 1;
extern double L1 = 1;
extern double L2 = 2;
extern double L3 = 4;
extern double L4 = 8;
extern double TP = 600;
extern double DP = 1440;
extern double MAP = 34;
extern double MaxTrade = 4;
extern int   TradeType      = 0;          // 0 to follow the trend, 1 to force buy, 2 to force sell
extern bool   CloseAllNow      = false;          // closes all orders now
extern bool   CloseProfitableTradesOnly = false; // closes only profitable trades
extern double ProftableTradeAmount      = 1;     // Only trades above this amount close out
extern bool   ClosePendingOnly = false;          // closes pending orders only
extern bool   UseAlerts        = false;

//+-------------+
//| Custom init |
//|-------------+
int init()
  {

  }

//+----------------+
//| Custom DE-init |
//+----------------+
int deinit()
  {

  }

void sendEmail()
{
  if (UseAlerts==true) SendMail("YTF Alert", "New order has been added  "+OrdersTotal()+"   Balance = " +AccountBalance() + " Equity = "+AccountEquity() +" Current Price: " + Close[0]);
  return;
}



//+------------------------------------------------------------------------+
//| Closes everything
//+------------------------------------------------------------------------+
void CloseAll()
{
  for(int i=OrdersTotal()-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
        if ( OrderType() == OP_BUY)  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
        if ( OrderType() == OP_SELL)  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );
        if ( OrderType()== OP_BUYSTOP)  result = OrderDelete( OrderTicket() );
        if ( OrderType()== OP_SELLSTOP)  result = OrderDelete( OrderTicket() );
        if (UseAlerts) PlaySound("alert.wav");
 }
  return; 
}


void CloseAllBuy()
{
  for(int i=OrdersTotal()-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
        if ( OrderType() == OP_BUY)  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
        if ( OrderType()== OP_BUYSTOP)  result = OrderDelete( OrderTicket() );
        if (UseAlerts) PlaySound("alert.wav");
 }
  return; 
}

void CloseAllSell()
{
  for(int i=OrdersTotal()-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
        if ( OrderType() == OP_SELL)  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );
        if ( OrderType()== OP_SELLSTOP)  result = OrderDelete( OrderTicket() );
        if (UseAlerts) PlaySound("alert.wav");
 }
  return; 
}
   
//+------------------------------------------------------------------------+
//| cancels all orders that are in profit
//+------------------------------------------------------------------------+
void CloseAllinProfit()
{
  for(int i=OrdersTotal()-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
        if ( OrderType() == OP_BUY && OrderProfit()+OrderSwap()>ProftableTradeAmount)  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_BID), 5, Red );
        if ( OrderType() == OP_SELL && OrderProfit()+OrderSwap()>ProftableTradeAmount)  result = OrderClose( OrderTicket(), OrderLots(), MarketInfo(OrderSymbol(), MODE_ASK), 5, Red );
        if (UseAlerts) PlaySound("alert.wav");
 }
  return; 
}







//+------------------------------------------------------------------------+
//| cancels all pending orders 
//+------------------------------------------------------------------------+
void ClosePendingOrdersOnly()
{
  for(int i=OrdersTotal()-1;i>=0;i--)
 {
    OrderSelect(i, SELECT_BY_POS);
    bool result = false;
        if ( OrderType()== OP_BUYSTOP)   result = OrderDelete( OrderTicket() );
        if ( OrderType()== OP_SELLSTOP)  result = OrderDelete( OrderTicket() );
  }
  return; 
  }

//+-----------+
//| Main      |
//+-----------+
int start()
  {
   int      OrdersBUY, ticket;
   int      OrdersSELL;
   double   BuyLots, SellLots, BuyProfit, SellProfit;

//+------------------------------------------------------------------+
//  Determine last order price                                       |
//-------------------------------------------------------------------+


double Trend = iMA(NULL,DP,MAP,0,MODE_LWMA,PRICE_CLOSE,1);

      if(Close[0] > Trend && OrdersTotal()==0 && TradeType ==0 )
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }

      if(Close[0] < Trend && OrdersTotal()==0 && TradeType ==0)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }


      if(OrdersTotal()==0 && TradeType ==1 )
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }

      if(OrdersTotal()==0 && TradeType ==2)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }




  if (OrdersTotal()==1 && (MaxTrade-1)>=OrdersTotal())
  {
  if(OrderSelect((OrdersTotal()-1), SELECT_BY_POS)==true)
  {
   if ((OrderType()==OP_BUY) && ((OrderOpenPrice()-OrderClosePrice())/Point)>= TP)
   {
          ticket=OrderSend(Symbol(),OP_BUY,Lots*L1,Ask,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 

         return(0); 
 
   } 
   if ((OrderType()==OP_SELL) && ((OrderClosePrice()- OrderOpenPrice())/Point)>= TP)
   {
            ticket=OrderSend(Symbol(),OP_SELL,Lots*L1,Bid,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 


   } 
  }
  }

  if (OrdersTotal()==2 && (MaxTrade-1)>=OrdersTotal())
  {
  if(OrderSelect((OrdersTotal()-1), SELECT_BY_POS)==true)
  {
   if ((OrderType()==OP_BUY) && ((OrderOpenPrice()-OrderClosePrice())/Point)>= TP)
   {
          ticket=OrderSend(Symbol(),OP_BUY,Lots*L2,Ask,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 

         return(0); 
 
   } 
   if ((OrderType()==OP_SELL) && ((OrderClosePrice()- OrderOpenPrice())/Point)>= TP)
   {
            ticket=OrderSend(Symbol(),OP_SELL,Lots*L2,Bid,3,0,0,"MLTrendETF",16384,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) sendEmail();
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 


   } 
  }
  }


//Close all

  for(int j=1;j<50;j++)
 {

  {
  if(OrderSelect((j-1), SELECT_BY_POS)==true)
  {
   if ((OrderType()==OP_BUY) && ((OrderClosePrice()-OrderOpenPrice())/Point)>= TP)
   {
    
         CloseAllBuy();
         return(0); 
 
   } 
   if ((OrderType()==OP_SELL) && ((OrderOpenPrice()-OrderClosePrice())/Point)>= TP)
   {
         CloseAllSell();
         return(0); 


   } 
  }
  }
  }









      for(int i=0;i<OrdersTotal();i++)
      {
         OrderSelect(i,SELECT_BY_POS,MODE_TRADES);
           if(OrderType()==OP_BUY)  OrdersBUY++;
           if(OrderType()==OP_SELL) OrdersSELL++;
           if(OrderType()==OP_BUY)  BuyLots += OrderLots();
           if(OrderType()==OP_SELL) SellLots += OrderLots();
           if(OrderType() == OP_BUY)  BuyProfit += OrderProfit() + OrderCommission() + OrderSwap();
           if(OrderType() == OP_SELL)  SellProfit += OrderProfit() + OrderCommission() + OrderSwap();
      }               
   



    if(CloseAllNow == true){
     CloseAll();
     CloseAllNow = false;
     }
    
    if(CloseProfitableTradesOnly) CloseAllinProfit();
    
//    if(BuyProfit+SellProfit >= ProfitTarget) CloseAll(); 

    if(ClosePendingOnly) ClosePendingOrdersOnly();

   
   Comment("                            Comments Last Update 09-May-2011 4:45pm", NL,
           "                            Developed by: Dr. Hamad Odhabi", NL,             
           "                            Buys    ", OrdersBUY, NL,
           "                            BuyLots        ", BuyLots, NL,
           "                            Sells    ", OrdersSELL, NL,
           "                            SellLots        ", SellLots, NL,
           "                            Balance ", AccountBalance(), NL,
           "                            Equity        ", AccountEquity(), NL,
           "                            Margin              ", AccountMargin(), NL,
           "                            MarginPercent        ", MathRound((AccountEquity()/AccountMargin())*100), NL,
           "                            Current Time is  ",TimeHour(CurTime()),":",TimeMinute(CurTime()),".",TimeSeconds(CurTime()));
 } // start()

 




