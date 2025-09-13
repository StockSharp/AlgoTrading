//+------------------------------------------------------------------+
//|                                               KositBablo10-1.mq5 |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "2.00"
//---external variables
input int TP        = 1400;   // Take Profit
input int SL        = 500;    // Stop Loss

input int TURBO = 0;      // TURBO = 0 normal mode!!!


//--- variables


MqlTradeRequest request;
MqlTradeResult result;
int rsiEUR_1, rsiEUR_2, maEUR_1, maEUR_2, rsiEUR_3, rsiEUR_4, maEUR_3, maEUR_4;
double rsiVal_1[3], rsiVal_2[3], maVal_1[3], maVal_2[3], rsiVal_3[3], rsiVal_4[3], maVal_3[3], maVal_4[3] ;
double Ask,Bid,sl;
int i,Spread;
double Lots;
double Poz, Ord;
ulong StopLevel;
//+------------------------------------------------------------------+
//| volume                                                           |
//+------------------------------------------------------------------+
double volume()
  {
   Lots=AccountInfoDouble(ACCOUNT_FREEMARGIN)/6500;
   Lots=MathMin(15,MathMax(0.1,Lots));
   Lots=NormalizeDouble(Lots,2);
   return(Lots);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
  // selling
   rsiEUR_1=iRSI("EURUSD", PERIOD_D1,12, PRICE_CLOSE);
   rsiEUR_2=iRSI("EURUSD", PERIOD_H1, 15, PRICE_CLOSE);
   maEUR_1=iMA("EURUSD",PERIOD_H1,23,11,1,PRICE_CLOSE);
   maEUR_2=iMA("EURUSD",PERIOD_H1,2,11,1,PRICE_CLOSE);
   
  // buying 
   rsiEUR_3=iRSI("EURUSD", PERIOD_D1,15, PRICE_CLOSE);
   rsiEUR_4=iRSI("EURUSD", PERIOD_H1, 5, PRICE_CLOSE);
   maEUR_3=iMA("EURUSD",PERIOD_H1,20,11,1,PRICE_CLOSE);
   maEUR_4=iMA("EURUSD",PERIOD_H1,2,11,1,PRICE_CLOSE);
   
   
   
   return(0);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//--- get the indicators data
   CopyBuffer(rsiEUR_1,0,0,3,rsiVal_1);
   ArraySetAsSeries(rsiVal_1,true);
   CopyBuffer(rsiEUR_2,0,0,3,rsiVal_2);
   ArraySetAsSeries(rsiVal_2,true);
   CopyBuffer(maEUR_1,0,0,3,maVal_1);
   ArraySetAsSeries(maVal_1,true);
   CopyBuffer(maEUR_2,0,0,3,maVal_2);
   ArraySetAsSeries(maVal_2,true);
   
   CopyBuffer(rsiEUR_3,0,0,3,rsiVal_3);
   ArraySetAsSeries(rsiVal_3,true);
   CopyBuffer(rsiEUR_4,0,0,3,rsiVal_4);
   ArraySetAsSeries(rsiVal_4,true);
   CopyBuffer(maEUR_3,0,0,3,maVal_3);
   ArraySetAsSeries(maVal_3,true);
   CopyBuffer(maEUR_4,0,0,3,maVal_4);
   ArraySetAsSeries(maVal_4,true);
   


//--- get prices values
   Ask = SymbolInfoDouble("EURUSD", SYMBOL_ASK);
   Bid = SymbolInfoDouble("EURUSD", SYMBOL_BID);
   Spread=int(SymbolInfoInteger("EURUSD",SYMBOL_SPREAD));
   StopLevel=SymbolInfoInteger("EURUSD",SYMBOL_TRADE_STOPS_LEVEL);

   Comment(StringFormat("\n\n\nAsk = %G\nBid = %G\nSpread = %G\nStopLevel = %G",Ask,Bid,Spread,StopLevel));
   


//--- set orders

 Poz = PositionsTotal();
 Ord = OrdersTotal();
if(TURBO > 0)
{Ord = 0;}
   if(Poz<1 && Ord<1)
     {
//+------------------------------------------------------------------+
//| delete all pending orders with the specified ORDER_MAGIC         |
//+------------------------------------------------------------------+
  if(rsiVal_3[1] < 60)
        {
        if(rsiVal_4[1] < 48 )
        {
        if((maVal_3[1]) > (maVal_4[1]))
        
   // main order +++++++++++++++++++++++++++++++++++++++      
        {
         request.action = TRADE_ACTION_PENDING;
         request.symbol = "EURUSD";
         request.volume = NormalizeDouble(volume()/3,2);
         request.price=NormalizeDouble(Ask+StopLevel*_Point,_Digits);
         request.sl = NormalizeDouble(request.price - SL*_Point,_Digits);
         request.tp = NormalizeDouble(request.price + TP*_Point,_Digits);
         request.deviation=0;
         request.type=ORDER_TYPE_BUY_STOP;
         request.type_filling=ORDER_FILLING_FOK;
         
           {
            OrderSend(request,result);
            if(result.retcode==10009 || result.retcode==10008)
               Print("The BuyStop order is set");
            else
              {
               Print(ResultRetcodeDescription(result.retcode));
                           
              }
           }       
        // turn over order +++++++++++++++++++++++++++++++++++++++
        
         request.action = TRADE_ACTION_PENDING;
         request.symbol = "EURUSD";
         request.volume = NormalizeDouble(volume()/2,2);
         request.price=NormalizeDouble(Bid-(SL-45) *_Point,_Digits);
         request.sl = NormalizeDouble(request.price + SL*_Point,_Digits);
         request.tp = NormalizeDouble(request.price - (TP-100)*_Point,_Digits);
         request.deviation=0;
         request.type=ORDER_TYPE_SELL_STOP;
         request.type_filling=ORDER_FILLING_FOK;
         request.type_time =ORDER_TIME_DAY;
           {
            OrderSend(request,result);
            if(result.retcode==10009 || result.retcode==10008)
               Print("The SellStop order is set");
            else
              {
               Print(ResultRetcodeDescription(result.retcode));
               return;
              }
          } } }          
  
 
        {
        if(rsiVal_2[1]> 55)
        {
        if((maVal_1[1])> (maVal_2[1]))
        {
        
        
        
         request.action = TRADE_ACTION_PENDING;
         request.symbol = "EURUSD";
         request.volume = NormalizeDouble(volume()/3,2);
         request.price=NormalizeDouble(Bid-StopLevel*_Point,_Digits);
         request.sl = NormalizeDouble(request.price + SL*_Point,_Digits);
         request.tp = NormalizeDouble(request.price - (TP-500)*_Point,_Digits);
         request.deviation=0;
         request.type=ORDER_TYPE_SELL_STOP;
         request.type_filling=ORDER_FILLING_FOK;
         for(i=0;i<3;i++)
           {
            OrderSend(request,result);
            if(result.retcode==10009 || result.retcode==10008)
               Print("The SellStop order is set");
            else
              {
               Print(ResultRetcodeDescription(result.retcode));
               return;
              }
           }
        // turn over order +++++++++++++++++++++++++++++++++++++++
        
         request.action = TRADE_ACTION_PENDING;
         request.symbol = "EURUSD";
         request.volume = NormalizeDouble(volume()/2,2);
         request.price=NormalizeDouble(Ask-(SL+50) *_Point,_Digits);
         request.sl = NormalizeDouble(request.price - SL*_Point,_Digits);
         request.tp = NormalizeDouble(request.price + (TP-600)*_Point,_Digits);
         request.deviation=0;
         request.type=ORDER_TYPE_BUY_STOP;
         request.type_filling=ORDER_FILLING_FOK;
         request.type_time =ORDER_TIME_DAY; 
           {
            OrderSend(request,result);
            if(result.retcode==10009 || result.retcode==10008)
               Print("The SellStop order is set");
            else
              {
               Print(ResultRetcodeDescription(result.retcode));
        }}
     
  
     
     
 
       } } }
}
      
//--- delete the order
   
}}
//+------------------------------------------------------------------+
//| ResultRetcodeDescription                                         |
//+------------------------------------------------------------------+
string ResultRetcodeDescription(int retcode)
  {
   string str;

   switch(retcode)
     {
      case TRADE_RETCODE_REQUOTE:
         str="Requote";
         break;
      case TRADE_RETCODE_REJECT:
         str="Request rejected";
         break;
      case TRADE_RETCODE_CANCEL:
         str="Request canceled by trader";
         break;
      case TRADE_RETCODE_PLACED:
         str="Order is placed";
         break;
      case TRADE_RETCODE_DONE:
         str="Request executed";
         break;
      case TRADE_RETCODE_DONE_PARTIAL:
         str="Request is executed partially";
         break;
      case TRADE_RETCODE_ERROR:
         str="Request processing error";
         break;
      case TRADE_RETCODE_TIMEOUT:
         str="Request timed out";
         break;
      case TRADE_RETCODE_INVALID:
         str="Invalid request";
         break;
      case TRADE_RETCODE_INVALID_VOLUME:
         str="Invalid request volume";
         break;
      case TRADE_RETCODE_INVALID_PRICE:
         str="Invalid request price";
         break;
      case TRADE_RETCODE_INVALID_STOPS:
         str="Invalid request stops";
         break;
      case TRADE_RETCODE_TRADE_DISABLED:
         str="Trade is not allowed";
         break;
      case TRADE_RETCODE_MARKET_CLOSED:
         str="Market is closed";
         break;
      case TRADE_RETCODE_NO_MONEY:
         str="Insufficient funds for request execution";
         break;
      case TRADE_RETCODE_PRICE_CHANGED:
         str="Prices have changed";
         break;
      case TRADE_RETCODE_PRICE_OFF:
         str="No quotes for request processing";
         break;
      case TRADE_RETCODE_INVALID_EXPIRATION:
         str="Invalid order expiration date in the request";
         break;
      case TRADE_RETCODE_ORDER_CHANGED:
         str="Order state has changed";
         break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS:
         str="Too many requests";
         break;
      case TRADE_RETCODE_NO_CHANGES:
         str="No changes in the request";
         break;
      case TRADE_RETCODE_SERVER_DISABLES_AT:
         str="Autotrading is disabled by the server";
         break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT:
         str="Autotrading is disabled by the client terminal";
         break;
      case TRADE_RETCODE_LOCKED:
         str="Request is blocked for processing";
         break;
      case TRADE_RETCODE_FROZEN:
         str="Order or position has been frozen";
         break;
      case TRADE_RETCODE_INVALID_FILL:
         str="Specified type of order execution for the balance is not supported";
         break;
      case TRADE_RETCODE_CONNECTION:
         str="No connection with trade server";
         break;
      case TRADE_RETCODE_ONLY_REAL:
         str="Operation is allowed only for real accounts";
         break;
      case TRADE_RETCODE_LIMIT_ORDERS:
         str="Pending orders have reached the limit";
         break;
      case TRADE_RETCODE_LIMIT_VOLUME:
         str="Volume of orders and positions for this symbol has reached the limit";
         break;

      default:
         str="Unknown result";
     }

   return(str);
  }
//+------------------------------------------------------------------+  