//+------------------------------------------------------------------+
//|                                             Profit Pandermic.mq4 |
//|                              Copyright © 2009, TradingSytemForex |
//|                                               braytone@gmail.com |
//+------------------------------------------------------------------+

#property copyright "Copyright © 2009, TradingSytemForex"

#define EAName "Profit Pandermic"

//---- input parameters

extern string S1="SlopeDirectioLine Config.";

extern bool      TradeAtSignal=true;
extern int       period=80; 
extern int       method=3;                         // mode_sma 
extern int       price=0;                          // price_close


extern string S2="Risk Config.";

extern double    Lots=0.1;                         // lots size
extern int       TakeProfit=0;                     // desired profit


extern string S3="Order Config.";



/*
extern bool MAFilter=false;                        // moving average filter
extern int MAPeriod=20;                            // ma filter period
extern int MAMethod=0;                             // ma filter method
extern int MAPrice=0;                              // ma filter price
*/


//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+

int start()
  {
  
   // signal conditions

   int limit=1;
   for(int i=1;i<=limit;i++)
   {
   

/*
   // moving average filter

      double MAF=iMA(Symbol(),0,MAPeriod,0,MAMethod,MAPrice,i);

      string MABUY="false";string MASELL="false";

      if((MAFilter==false)||(MAFilter&&Bid>MAF))MABUY="true";
      if((MAFilter==false)||(MAFilter&&Ask<MAF))MASELL="true";
      
*/      

   // main signal
      
      double SDL3=iCustom(Symbol(),0,"Slope Direction Line",period,method,price,2,i+2);
      double SDL4=iCustom(Symbol(),0,"Slope Direction Line",period,method,price,2,i+1);
      double SDL5=iCustom(Symbol(),0,"Slope Direction Line",period,method,price,2,i);
      int cnt, ticket, total;

      string BUY="false";
      string SELL="false";

      if((TradeAtSignal&&SDL5>SDL4&&SDL4<SDL3)||(TradeAtSignal==false&&SDL4<SDL3))BUY="true";
      if((TradeAtSignal&&SDL5<SDL4&&SDL4>SDL3)||(TradeAtSignal==false&&SDL4>SDL3))SELL="true";
      
      string SignalBUY="false";
      string SignalSELL="false";
      
   }
   
//----------------------

if(Bars<100)
     {
      Print("bars less than 100");
      return(0);  
     }
   if(TakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);  // check TakeProfit
     }

//----------------------
  
   total=OrdersTotal();
   if(total<1) 
     {
      
      if(AccountFreeMargin()<(1000*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);  
        }
       
        
//---------------------check for order entry possisbilty

if(SignalBUY=="true"&&NewBarBuy())
      {
        ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,Ask+TakeProfit*Point,EAName,0,Blue);
        if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
           }
         else Print("Error opening BUY order : ",GetLastError()); 
         return(0); 
        }
        
        
if(SignalSELL=="true"&&NewBarSell())
      {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,Bid-TakeProfit*Point,EAName,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
           }
         else Print("Error opening SELL order : ",GetLastError()); 
         return(0); 
        }
      return(0);
     }

//------------------------check for orders exit possibility

  for (cnt=total-1;cnt>=0;cnt--)
  {
   {
    OrderSelect(cnt,SELECT_BY_POS,MODE_TRADES);
    if(OrderType()<=OP_SELL&&OrderSymbol()==Symbol())
    {
      if(OrderType()==OP_BUY)
      {
       if (SignalSELL=="true"&&NewBarSell())
       {
        OrderClose(OrderTicket(),OrderLots(),Bid,3);
        switch(OrderType())
        
   
 
  
  return(0);
  
  }

  {
  
   if(SignalSELL=="true"&&NewBarBuy())
    {
     OrderClose(OrderTicket(),OrderLots(),Ask,3);
        switch(OrderType())
       
      }
    }
  }
  
   return(0);
   
          
        


