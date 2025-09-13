//+------------------------------------------------------------------+
#property copyright "Interbank FX, LLC" //( original template)

#property copyright "PicassoInActions , 123@donothing.us" //( original idea)


#include <stderror.mqh> 
//+------------------------------------------------------------------+
//| Global Variables / Includes                                      |
//+------------------------------------------------------------------+
datetime   CurrTime = 0;
datetime   PrevTime = 0;
  string        Sym = "";
     int  TimeFrame = 0;
     int      Shift = 1;
     int  SymDigits = 5;
  double  SymPoints = 0.0001;

//+------------------------------------------------------------------+
//| Expert User Inputs                                               |
//+------------------------------------------------------------------+
extern   bool   UseCompletedBars = true;
extern    int      Periods = 14;
extern double         Lots = 0.1;
extern    int  MagicNumberD = 1235;
extern    int  MagicNumberU = 1237;
extern    int ProfitTarget =  20; 
extern    int     StopLoss =  18; 
extern    int     Slippage =    3;


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
{
         Sym = Symbol();
   TimeFrame = Period();   
   SymPoints = MarketInfo( Sym, MODE_POINT  );
   SymDigits = MarketInfo( Sym, MODE_DIGITS );
   //---
        if( SymPoints == 0.001   ) { SymPoints = 0.01;   SymDigits = 3; }
   else if( SymPoints == 0.00001 ) { SymPoints = 0.0001; SymDigits = 5; }
  
   //----
   return(0);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit() { return(0); }

//+------------------------------------------------------------------+
//| Expert start function                                            |
//+------------------------------------------------------------------+
int start() 
{    
     
      if( CountAll( Sym, MagicNumberU) == 0)
       {
       //Print ("test");
        EnterLong(Sym, Lots, "");
       }
      else
       {
       Print (MagicNumberU);
       UpdateU ( Sym, MagicNumberU);
       }

      if( CountAll( Sym, MagicNumberD) == 0)
      {
        EnterShrt(Sym, Lots, "");
      }
      else
       {
        UpdateD ( Sym, MagicNumberD);
       }
    //----
   return(0); 
}

//+------------------------------------------------------------------+
//| Expert Custom Functions                                          |
//+------------------------------------------------------------------+
//| Update for Sell Stop()                                                       |
//+------------------------------------------------------------------+
int UpdateD( string Symbole, int Magic )
{
    //---- 
    
 int Ticket = -1; int err = 0; bool OrderLoop = False; int TryCount = 0; int tic =0; 
    
    
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        if ( OrderMagicNumber() != Magic   ) continue;
        if (      OrderSymbol() != Symbole ) continue;
 
                              
                if ((OrderOpenPrice()+Bid)>0.007  )
                {
                   EnterShrtUpdate (Symbole,Lots,"updated",OrderTicket( ));
                }
               else if ((OrderOpenPrice()+Bid)<0.0005 )
                {
                   EnterShrtUpdate (Symbole,Lots,"updated",OrderTicket( ));
                }
     }
    //----
    return(0);
}
//| Place Short Update Order                                                 |
//+------------------------------------------------------------------+
int EnterShrtUpdate( string FinalSymbol, double FinalLots, string EA_Comment, int tic )
{
   int Ticket = -1; int err = 0; bool OrderLoop = False; int TryCount = 0;
   
                     
   while( !OrderLoop )
   {
      while( IsTradeContextBusy() ) { Sleep( 10 ); }
                           
      RefreshRates();
      double SymAsk = NormalizeDouble( MarketInfo( FinalSymbol, MODE_ASK ), SymDigits );    
      double SymBid = NormalizeDouble( MarketInfo( FinalSymbol, MODE_BID ), SymDigits );
      double point=MarketInfo(Symbol(),MODE_POINT);
      
             if( OrderSelect( tic, SELECT_BY_TICKET ) )
              if (      OrderType()<2 ) continue;
             { OrderModify( tic, SymBid-100*point, StopShrt(SymAsk-100*point,StopLoss, SymPoints,SymDigits), TakeShrt(SymBid,ProfitTarget,SymPoints,SymDigits), 0, CLR_NONE ); } 
                                         
      int Err=GetLastError();
      
      switch (Err) 
      {
           //---- Success
           case               ERR_NO_ERROR: OrderLoop = true; 
                                       //     if( OrderSelect( Ticket, SELECT_BY_TICKET ) )
                                      //      { OrderModify( Ticket, OrderOpenPrice(), StopLong(SymBid,StopLoss, SymPoints,SymDigits), TakeLong(SymAsk,ProfitTarget,SymPoints,SymDigits), 0, CLR_NONE ); }
                                           break;
     
           //---- Retry Error     
           case            ERR_SERVER_BUSY:
           case          ERR_NO_CONNECTION:
           case          ERR_INVALID_PRICE:
           case             ERR_OFF_QUOTES:
           case            ERR_BROKER_BUSY:
           case     ERR_TRADE_CONTEXT_BUSY: TryCount++; break;
           case          ERR_PRICE_CHANGED:
           case                ERR_REQUOTE: continue;
     
           //---- Fatal known Error 
           case          ERR_INVALID_STOPS: OrderLoop = true; Print( "Invalid Stops"    ); break; 
           case   ERR_INVALID_TRADE_VOLUME: OrderLoop = true; Print( "Invalid Lots"     ); break; 
           case          ERR_MARKET_CLOSED: OrderLoop = true; Print( "Market Close"     ); break; 
           case         ERR_TRADE_DISABLED: OrderLoop = true; Print( "Trades Disabled"  ); break; 
           case       ERR_NOT_ENOUGH_MONEY: OrderLoop = true; Print( "Not Enough Money" ); break; 
           case  ERR_TRADE_TOO_MANY_ORDERS: OrderLoop = true; Print( "Too Many Orders"  ); break; 
              
           //---- Fatal Unknown Error
           case              ERR_NO_RESULT:
                                   default: OrderLoop = true; Print( "Unknown Error - " + Err ); break; 
           //----                         
       }  
       // end switch 
       if( TryCount > 10) { OrderLoop = true; }
   }
   //----               
   return(0);
}
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Update for Buy Stop()                                                       |
//+------------------------------------------------------------------+
int UpdateU( string Symbole, int Magic )
{
 int Ticket = -1; int err = 0; bool OrderLoop = False; int TryCount = 0; int tic =0; 
    
    
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        if ( OrderMagicNumber() != Magic   ) continue;
        if (      OrderSymbol() != Symbole ) continue;
        
               if ((OrderOpenPrice()-Ask)<0.0005 )
                {
                   EnterLongUpdate (Symbole,Lots,"updated",OrderTicket( ));
                }
               else if ((OrderOpenPrice()-Ask)>0.0007 )
                {
                   EnterLongUpdate (Symbole,Lots,"updated",OrderTicket( ));
                }
    }
    //----
    return(0);
}


//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Place Long Order                                                 |
//+------------------------------------------------------------------+
int EnterLongUpdate( string FinalSymbol, double FinalLots, string EA_Comment, int tic )
{
   int Ticket = -1; int err = 0; bool OrderLoop = False; int TryCount = 0;
   
   while( !OrderLoop )
   {
      while( IsTradeContextBusy() ) { Sleep( 10 ); }
                           
      RefreshRates();
      double SymAsk = NormalizeDouble( MarketInfo( FinalSymbol, MODE_ASK ), SymDigits );    
      double SymBid = NormalizeDouble( MarketInfo( FinalSymbol, MODE_BID ), SymDigits );
      double point=MarketInfo(Symbol(),MODE_POINT);
      
                                       if( OrderSelect( tic, SELECT_BY_TICKET ) )
                                       
                                      
                                       
                                       if (      OrderType()<2 ) continue;
                                       
                                        { OrderModify( tic, SymBid+100*point, StopLong(SymBid+80*point,StopLoss, SymPoints,SymDigits), TakeLong(SymBid,ProfitTarget,SymPoints,SymDigits), 0, CLR_NONE ); } 
                                         
      int Err=GetLastError();
      
      switch (Err) 
      {
           //---- Success
           case               ERR_NO_ERROR: OrderLoop = true; 
                                                       break;
     
           //---- Retry Error     
           case            ERR_SERVER_BUSY:
           case          ERR_NO_CONNECTION:
           case          ERR_INVALID_PRICE:
           case             ERR_OFF_QUOTES:
           case            ERR_BROKER_BUSY:
           case     ERR_TRADE_CONTEXT_BUSY: TryCount++; break;
           case          ERR_PRICE_CHANGED:
           case                ERR_REQUOTE: continue;
     
           //---- Fatal known Error 
           case          ERR_INVALID_STOPS: OrderLoop = true; Print( "Invalid Stops"    ); break; 
           case   ERR_INVALID_TRADE_VOLUME: OrderLoop = true; Print( "Invalid Lots"     ); break; 
           case          ERR_MARKET_CLOSED: OrderLoop = true; Print( "Market Close"     ); break; 
           case         ERR_TRADE_DISABLED: OrderLoop = true; Print( "Trades Disabled"  ); break; 
           case       ERR_NOT_ENOUGH_MONEY: OrderLoop = true; Print( "Not Enough Money" ); break; 
           case  ERR_TRADE_TOO_MANY_ORDERS: OrderLoop = true; Print( "Too Many Orders"  ); break; 
              
           //---- Fatal Unknown Error
           case              ERR_NO_RESULT:
                                   default: OrderLoop = true; Print( "Unknown Error - " + Err ); break; 
           //----                         
       }  
       // end switch 
       if( TryCount > 10) { OrderLoop = true; }
   }
   //----               
   return(0);
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| CountAll()                                                       |
//+------------------------------------------------------------------+
int CountAll( string Symbole, int Magic )
{
    //---- 
    int count = 0;
    if (OrdersTotal()<1)
    {
    return(count);
    }
    
    
    for (int i = OrdersTotal() - 1; i >= 0; i--)
    {
        OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
        if ( OrderMagicNumber() != Magic   ) continue;
        if (      OrderSymbol() != Symbole ) continue;
        
             if ( OrderType() == OP_BUYSTOP  ) { count++; }
        else if ( OrderType() == OP_SELLSTOP ) { count++; }
    }
    //----
    return(count);
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Calculate Stop Long                                              |
//+------------------------------------------------------------------+
double StopLong(double price,double stop,double point,double SymDgts )
{
if(stop==0) { return(0); }
else        { return(NormalizeDouble( price-(stop*point),SymDgts)); }
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Calculate Stop Short                                             |
//+------------------------------------------------------------------+
double StopShrt(double price,double stop,double point,double SymDgts )
{
if(stop==0) { return(0); }
else        { return(NormalizeDouble( price+(stop*point),SymDgts)); }
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Calculate Profit Target Long                                     |
//+------------------------------------------------------------------+
double TakeLong(double price,double take,double point,double SymDgts )
{
if(take==0) {  return(0);}
else        {  return(NormalizeDouble( price+(take*point),SymDgts));}
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Calculate Profit Target Long                                     |
//+------------------------------------------------------------------+
double TakeShrt(double price,double take,double point,double SymDgts )
{
if(take==0) {  return(0);}
else        {  return(NormalizeDouble( price-(take*point),SymDgts));}
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Place Long Order                                                 |
//+------------------------------------------------------------------+
int EnterLong( string FinalSymbol, double FinalLots, string EA_Comment )
{
   int Ticket = -1; int err = 0; bool OrderLoop = False; int TryCount = 0;
                     
   while( !OrderLoop )
   {
      while( IsTradeContextBusy() ) { Sleep( 10 ); }
                           
      RefreshRates();
      double SymAsk = NormalizeDouble( MarketInfo( FinalSymbol, MODE_ASK ), SymDigits );    
      double SymBid = NormalizeDouble( MarketInfo( FinalSymbol, MODE_BID ), SymDigits );
      double point=MarketInfo(Symbol(),MODE_POINT);
                           
 Ticket=OrderSend(FinalSymbol,OP_BUYSTOP,FinalLots,SymBid+100*point,0,StopLong(SymAsk+100*point,StopLoss, SymPoints,SymDigits),TakeLong(SymBid,ProfitTarget, SymPoints,SymDigits),"some comment",MagicNumberU,0,CLR_NONE);
    

      int Err=GetLastError();
      
      switch (Err) 
      {
           //---- Success
          // case               ERR_NO_ERROR: OrderLoop = true; 
                                       //     if( OrderSelect( Ticket, SELECT_BY_TICKET ) )
                                      //      { OrderModify( Ticket, OrderOpenPrice(), StopLong(SymBid,StopLoss, SymPoints,SymDigits), TakeLong(SymAsk,ProfitTarget,SymPoints,SymDigits), 0, CLR_NONE ); }
                                      //      break;
     
           //---- Retry Error     
           case            ERR_SERVER_BUSY:
           case          ERR_NO_CONNECTION:
           case          ERR_INVALID_PRICE:
           case             ERR_OFF_QUOTES:
           case            ERR_BROKER_BUSY:
           case     ERR_TRADE_CONTEXT_BUSY: TryCount++; break;
           case          ERR_PRICE_CHANGED:
           case                ERR_REQUOTE: continue;
     
           //---- Fatal known Error 
           case          ERR_INVALID_STOPS: OrderLoop = true; Print( "Invalid Stops"    ); break; 
           case   ERR_INVALID_TRADE_VOLUME: OrderLoop = true; Print( "Invalid Lots"     ); break; 
           case          ERR_MARKET_CLOSED: OrderLoop = true; Print( "Market Close"     ); break; 
           case         ERR_TRADE_DISABLED: OrderLoop = true; Print( "Trades Disabled"  ); break; 
           case       ERR_NOT_ENOUGH_MONEY: OrderLoop = true; Print( "Not Enough Money" ); break; 
           case  ERR_TRADE_TOO_MANY_ORDERS: OrderLoop = true; Print( "Too Many Orders"  ); break; 
              
           //---- Fatal Unknown Error
           case              ERR_NO_RESULT:
                                   default: OrderLoop = true; Print( "Unknown Error - " + Err ); break; 
           //----                         
       }  
       // end switch 
       if( TryCount > 10) { OrderLoop = true; }
   }
   //----               
   return(Ticket);
}
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Place Shrt Order                                                 |
//+------------------------------------------------------------------+
int EnterShrt( string FinalSymbol, double FinalLots, string EA_Comment )
{
   int Ticket = -1; int err = 0; bool OrderLoop = False; int TryCount = 0;
                     
   while( !OrderLoop )
   {
      while( IsTradeContextBusy() ) { Sleep( 10 ); }
                           
      RefreshRates();
      double SymAsk = NormalizeDouble( MarketInfo( FinalSymbol, MODE_ASK ), SymDigits );    
      double SymBid = NormalizeDouble( MarketInfo( FinalSymbol, MODE_BID ), SymDigits );
      double point=MarketInfo(Symbol(),MODE_POINT);
                               
     // Ticket = OrderSend( FinalSymbol, OP_SELL, FinalLots, SymBid, 0,  0.0,0.0, EA_Comment, MagicNumber, 0, CLR_NONE ); 
      Ticket=OrderSend(FinalSymbol,OP_SELLSTOP,FinalLots,SymBid-100*point,0,StopShrt(SymAsk-100*point,StopLoss, SymPoints,SymDigits),TakeShrt(SymBid,ProfitTarget, SymPoints,SymDigits),"some comment",MagicNumberD,0,CLR_NONE);
     // ticket=OrderSend(Symbol(),OP_SELLSTOP,0.1,price-70*point,0,price+100*point,price-200*point,"some comment",mgnD,0,CLR_NONE);
                           
      int Err=GetLastError();
      
      switch (Err) 
      {
           //---- Success
             //    case               ERR_NO_ERROR: OrderLoop = true;
                                                 // if( OrderSelect( Ticket, SELECT_BY_TICKET ) )
                                                 // { OrderModify( Ticket, OrderOpenPrice(), StopShrt(SymAsk,StopLoss, SymPoints,SymDigits), TakeShrt(SymBid,ProfitTarget, SymPoints,SymDigits), 0, CLR_NONE ); }
                                                 // break;
     
           //---- Retry Error     
           case            ERR_SERVER_BUSY:
           case          ERR_NO_CONNECTION:
           case          ERR_INVALID_PRICE:
           case             ERR_OFF_QUOTES:
           case            ERR_BROKER_BUSY:
           case     ERR_TRADE_CONTEXT_BUSY: TryCount++; break;
           case          ERR_PRICE_CHANGED:
           case                ERR_REQUOTE: continue;
     
           //---- Fatal known Error 
           case          ERR_INVALID_STOPS: OrderLoop = true; Print( "Invalid Stops"    ); break; 
           case   ERR_INVALID_TRADE_VOLUME: OrderLoop = true; Print( "Invalid Lots"     ); break; 
           case          ERR_MARKET_CLOSED: OrderLoop = true; Print( "Market Close"     ); break; 
           case         ERR_TRADE_DISABLED: OrderLoop = true; Print( "Trades Disabled"  ); break; 
           case       ERR_NOT_ENOUGH_MONEY: OrderLoop = true; Print( "Not Enough Money" ); break; 
           case  ERR_TRADE_TOO_MANY_ORDERS: OrderLoop = true; Print( "Too Many Orders"  ); break; 
              
           //---- Fatal Unknown Error
           case              ERR_NO_RESULT:
                                   default: OrderLoop = true; Print( "Unknown Error - " + Err ); break; 
           //----                         
       }  
       // end switch 
       if( TryCount > 10) { OrderLoop = true; }
   }
   //----               
   return(Ticket);
}
//+------------------------------------------------------------------+
