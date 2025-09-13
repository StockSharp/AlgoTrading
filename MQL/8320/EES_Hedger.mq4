//+------------------------------------------------------------------+
//|                                                   EES Hedger.mq4 |
//|                             Copyright © 2008, Elite E Services   |
//|  http://www.eliteeservices.net          http://www.getfxnow.com  |      
//|                Programmed by:               Mikhail Veneracion   |
//|                                                                  |
//|Support:  http://www.eesfx.com  Group:  http://www.forexcoding.com| 
//|Elite E Services (646)837-0059  2620 Regatta Dr. Suite 102 LV, NV |
//+------------------------------------------------------------------+
/*
Instructions:
Example you are trading SAR EA on GBPUSD and SAR EA uses magicnumber of 1234, 
so you want to use EES Hedger in conjunction with SAR EA in this example, so this is what you do.
Open another GBPUSD chart(same pair SAR EA is trading), does not matter what time frame, but it is 
ideal that you use the same timeframe as what the SAR EA is trading from, or a lower timeframe. Now 
on Advocates external var: Original_EA_Magic enter SAR EA's magicnumber which is in this example 1234. 
Now make sure that Advocate_EA_Magic is different from Original_EA_Magic.
Then that's it. Once SAR EA opens a trade, EES Hedger will open the opposite order.
Now if you want to use EES Hedger for Manual trades. Simply input Original_EA_Magic = 0;
*/

#property copyright "Copyright © 2008, Elite E Services"
#property link      "http://eliteeservices.net" 

extern int
   Original_EA_Magic       = 0,    //This is the magic number that your 1st EA uses where advocate EA will base its trades on
   Advocate_EA_Magic       = 2008, //This is the advocate EAs own magic number please keep it unique to avoid conflict
   Slippage                = 6,    //Amount of allowed slippage
   TakeProfit              = 50,   //Take profit amount, per trade
   StopLoss                = 50,   //Stop loss, per trade
   TS_MinProfit             = 0,   //minimum profit require before trailing stop starts
   TrailingStop            = 25,   //this is the trailing stop in pips
   BreakEvenAfterPips      = 25;   //when this amount in profit in pips is reached SL will be moved to breakeven
extern double 
   Lots                    = 0.1;
   
datetime TT;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   TT=0;
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
   //TRAILING STOP SECTION FOR TRADE 2
   if(TrailingStop>0 && subTotalTrade()>0)
   {
       int total = OrdersTotal();
       for(int cnt=0;cnt<total;cnt++)
       {
          OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);

          if(OrderType()<=OP_SELL &&
             OrderSymbol()==Symbol() &&
             OrderMagicNumber()==Advocate_EA_Magic)
          {
             subTrailingStop(OrderType());
          }
       }
   } 
   //----------------------- BREAK EVEN AFTER PIPS SECTION
   if(BreakEvenAfterPips >0 && subTotalTrade()>0)
   {  
      total = OrdersTotal();
      for(cnt=0;cnt<total;cnt++)
      {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);

         if(OrderType()<=OP_SELL &&
            OrderSymbol()==Symbol() &&
            OrderMagicNumber()==Advocate_EA_Magic)
         {
            subBreakEvenAfterPips (OrderType());
         }
      }
   }
   subTradeOpenBar(Original_EA_Magic);
//----
   return(0);
  }
//+------------------------------------------------------------------+

int subTradeOpenBar(int MagicNumber)
{
   int
      cnt, 
      total = 0;

   for(cnt=0;cnt<OrdersTotal();cnt++)
   {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol() &&
         OrderMagicNumber()==MagicNumber)
      {
         total = iBarShift(Symbol(), 0, OrderOpenTime(), false);   
         if((total<1)&&(TT!=iTime(Symbol(),0,0)))
         {
            if(OrderType()==OP_BUY)
            {
               //OPEN SELL ORDER
               subOpenOrder(OP_SELL, Lots, StopLoss, TakeProfit, Advocate_EA_Magic);
               TT=iTime(Symbol(),0,0);
               break;
            }
            
            if(OrderType()==OP_SELL)
            {
               //OPEN BUY ORDER
               subOpenOrder(OP_BUY, Lots, StopLoss, TakeProfit, Advocate_EA_Magic);
               TT=iTime(Symbol(),0,0);
               break;
            }
         }
      }
   }
   return(total);
}
int subOpenOrder(int type, double Lotz, int stoploss, int takeprofit,int MagicNumber)
{
   int NumberOfTries = 15;
   string TicketComment = "Advocate EA";
   int
         ticket      = 0,
         err         = 0,
         c           = 0;
         
   double         
         aStopLoss   = 0,
         aTakeProfit = 0,
         bStopLoss   = 0,
         bTakeProfit = 0;

   if(stoploss!=0)
   {
      aStopLoss   = NormalizeDouble(Ask-stoploss*Point,Digits);
      bStopLoss   = NormalizeDouble(Bid+stoploss*Point,Digits);
   }
   
   if(takeprofit!=0)
   {
      aTakeProfit = NormalizeDouble(Ask+takeprofit*Point,Digits);
      bTakeProfit = NormalizeDouble(Bid-takeprofit*Point,Digits);
   }
   
   if(type==OP_BUY)
   {
      for(c=0;c<NumberOfTries;c++)
      {
         ticket=OrderSend(Symbol(),OP_BUY,Lotz,Ask,Slippage,aStopLoss,aTakeProfit,TicketComment,MagicNumber,0,Green);
         err=GetLastError();
         if(err==0)
         { 
            if(ticket>0) break;
         }
         else
         {
            if(err==0 || err==4 || err==136 || err==137 || err==138 || err==146) //Busy errors
            {
               Sleep(5000);
               continue;
            }
            else //normal error
            {
               if(ticket>0) break;
            }  
         }
      }   
   }
   if(type==OP_SELL)
   {   
      for(c=0;c<NumberOfTries;c++)
      {
         ticket=OrderSend(Symbol(),OP_SELL,Lotz,Bid,Slippage,bStopLoss,bTakeProfit,TicketComment,MagicNumber,0,Red);
         err=GetLastError();
         if(err==0)
         { 
            if(ticket>0) break;
         }
         else
         {
            if(err==0 || err==4 || err==136 || err==137 || err==138 || err==146) //Busy errors
            {
               Sleep(5000);
               continue;
            }
            else //normal error
            {
               if(ticket>0) break;
            }  
         }
      }   
   }  
   return(ticket);
}
void subTrailingStop(int Type)
{
   if(Type==OP_BUY)   // buy position is opened   
   {
//----------------------- AFTER PROFIT TRAILING STOP      
        
            if(Bid-OrderOpenPrice()>Point*TS_MinProfit &&
              OrderStopLoss()<Bid-Point*TrailingStop)
            {
               OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,Green);
               return(0);
            }
          
   }         
   if(Type==OP_SELL)   // sell position is opened   
   {
//----------------------- AFTER PROFIT TRAILING STOP      
     
            if(OrderOpenPrice()-Ask>Point*TS_MinProfit)
            {
            if(OrderStopLoss()>Ask+Point*TrailingStop || OrderStopLoss()==0)
            {
               OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Point*TrailingStop,OrderTakeProfit(),0,Red);
               return(0);
            }
            }
           
           
        
   }
}
//---------------------- BREAK EVEN FUNCTION
void subBreakEvenAfterPips(int Type)
{
   if(Type==OP_BUY)
   {
      if(((Bid-OrderOpenPrice()) > (Point*BreakEvenAfterPips))&&(OrderStopLoss()<OrderOpenPrice()))
            OrderModify(
                        OrderTicket(),
                        OrderOpenPrice(),
                        NormalizeDouble(OrderOpenPrice()+(10*Point),Digits),
                        OrderTakeProfit(),
                        0,
                        GreenYellow);
   }
   if(Type==OP_SELL)
   {
    if(((OrderOpenPrice()-Ask) > (Point*BreakEvenAfterPips))&&(OrderStopLoss()>OrderOpenPrice()))
            OrderModify(
                        OrderTicket(),
                        OrderOpenPrice(),
                        NormalizeDouble(OrderOpenPrice()-(10*Point),Digits),
                        OrderTakeProfit(),
                        0,
                        Red);
   }
}
int subTotalTrade()
{
   int
      cnt, 
      total = 0;

   for(cnt=0;cnt<OrdersTotal();cnt++)
   {
      OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
      if(OrderSymbol()==Symbol() &&
         OrderMagicNumber()==Advocate_EA_Magic) total++;
   }
   return(total);
}