//+------------------------------------------------------------------+
//|                                                     Champion.mq5 |
//|                                             Copyright 2012, AM2. |
//|                                             www.forexsystems.biz |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, AM2."
#property link      "http://www.forexsystems.biz"
#property version   "1.07"

//--- external variables
input int TP        = 150;   // Take Profit
input int SL        = 50;    // Stop Loss
input int RSIPeriod = 14;    // RSI period
input int RSILevel  = 30;    // RSI level

//--- variables
MqlTradeRequest request;
MqlTradeResult result;
int rsiHandle;
double rsiVal[3];
double Ask,Bid,sl;
int i,Spread;
double Lots;
ulong StopLevel;
//+------------------------------------------------------------------+
//| volume                                                           |
//+------------------------------------------------------------------+
double volume()
  {
   Lots=AccountInfoDouble(ACCOUNT_FREEMARGIN)/2000;
   Lots=MathMin(15,MathMax(0.1,Lots));
   Lots=NormalizeDouble(Lots,2);
   return(Lots);
  }
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   rsiHandle=iRSI(NULL,0,RSIPeriod,PRICE_CLOSE);
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
//--- get data from indicators
   CopyBuffer(rsiHandle,0,0,3,rsiVal);
   ArraySetAsSeries(rsiVal,true);

//--- get price values
   Ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   Bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   Spread=int(SymbolInfoInteger(_Symbol,SYMBOL_SPREAD));
   StopLevel=SymbolInfoInteger(Symbol(),SYMBOL_TRADE_STOPS_LEVEL);

   Comment(StringFormat("\n\n\nAsk = %G\nBid = %G\nSpread = %G\nStopLevel = %G",Ask,Bid,Spread,StopLevel));

//--- set orders
   if(PositionsTotal()<1 && OrdersTotal()<1)
     {
      if(rsiVal[1]<RSILevel)
        {
         request.action = TRADE_ACTION_PENDING;
         request.symbol = _Symbol;
         request.volume = NormalizeDouble(volume()/3,2);
         request.price=NormalizeDouble(Ask+StopLevel*_Point,_Digits);
         request.sl = NormalizeDouble(request.price - SL*_Point,_Digits);
         request.tp = NormalizeDouble(request.price + TP*_Point,_Digits);
         request.deviation=0;
         request.type=ORDER_TYPE_BUY_STOP;
         request.type_filling=ORDER_FILLING_FOK;
         for(i=0;i<3;i++)
           {
            OrderSend(request,result);
            if(result.retcode==10009 || result.retcode==10008)
               Print("BuyStop order set");
            else
              {
               Print(ResultRetcodeDescription(result.retcode));
               return;
              }
           }
        }

      if(rsiVal[1]>100-RSILevel)
        {
         request.action = TRADE_ACTION_PENDING;
         request.symbol = _Symbol;
         request.volume = NormalizeDouble(volume()/3,2);
         request.price=NormalizeDouble(Bid-StopLevel*_Point,_Digits);
         request.sl = NormalizeDouble(request.price + SL*_Point,_Digits);
         request.tp = NormalizeDouble(request.price - TP*_Point,_Digits);
         request.deviation=0;
         request.type=ORDER_TYPE_SELL_STOP;
         request.type_filling=ORDER_FILLING_FOK;
         for(i=0;i<3;i++)
           {
            OrderSend(request,result);
            if(result.retcode==10009 || result.retcode==10008)
               Print("SellStop order set");
            else
              {
               Print(ResultRetcodeDescription(result.retcode));
               return;
              }
           }

        }
     }

//--- trailing position   
   for(i=0;i<PositionsTotal();i++)
     {
      if(Symbol()==PositionGetSymbol(i))
        {
         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_BUY)
           {
            sl=MathMax(PositionGetDouble(POSITION_PRICE_OPEN)+Spread*_Point,Bid-SL*_Point);

            if(sl>PositionGetDouble(POSITION_SL) && (Bid-StopLevel*_Point-Spread*_Point)>PositionGetDouble(POSITION_PRICE_OPEN))
              {
               request.action = TRADE_ACTION_SLTP;
               request.symbol = _Symbol;
               request.sl = NormalizeDouble(sl,_Digits);
               request.tp = PositionGetDouble(POSITION_TP);
               OrderSend(request,result);
               if(result.retcode==10009 || result.retcode==10008) // request executed
                  Print("Moving Stop Loss of Buy position #",request.order);
               else
                 {
                  Print(ResultRetcodeDescription(result.retcode));
                  return;
                 }
               return;
              }
           }

         if(PositionGetInteger(POSITION_TYPE)==POSITION_TYPE_SELL)
           {
            sl=MathMin(PositionGetDouble(POSITION_PRICE_OPEN)-Spread*_Point,Ask+SL*_Point);

            if(sl<PositionGetDouble(POSITION_SL) && (PositionGetDouble(POSITION_PRICE_OPEN)-StopLevel*_Point-Spread*_Point)>Ask)
              {
               request.action = TRADE_ACTION_SLTP;
               request.symbol = _Symbol;
               request.sl = NormalizeDouble(sl,_Digits);
               request.tp = PositionGetDouble(POSITION_TP);
               OrderSend(request,result);
               if(result.retcode==10009 || result.retcode==10008) // request executed
                  Print("Moving Stop Loss of Sell position #",request.order);
               else
                 {
                  Print(ResultRetcodeDescription(result.retcode));
                  return;
                 }
               return;
              }
           }
        }
     }
//--- move the order
   for(int pos=0; pos<OrdersTotal(); pos++)
     {
      ulong order_ticket=OrderGetTicket(pos);
      if(OrderGetString(ORDER_SYMBOL)==Symbol())
        {
         if(OrderGetInteger(ORDER_TYPE)==ORDER_TYPE_BUY_STOP)
           {
            if(OrderGetDouble(ORDER_PRICE_OPEN)-Ask>20*_Point)
              {
               request.action= TRADE_ACTION_MODIFY;
               request.order = order_ticket;
               request.price = NormalizeDouble(Ask+20*_Point,_Digits);
               request.sl = NormalizeDouble(request.price - SL*_Point,_Digits);
               request.tp = NormalizeDouble(request.price + TP*_Point,_Digits);
               OrderSend(request,result);
               if(result.retcode==10009 || result.retcode==10008)
                  Print("BuyStop order modified");
               else
                 {
                  Print(ResultRetcodeDescription(result.retcode));
                  return;
                 }
              }
           }

         if(OrderGetInteger(ORDER_TYPE)==ORDER_TYPE_SELL_STOP)
           {
            if(Bid-OrderGetDouble(ORDER_PRICE_OPEN)>20*_Point)
              {
               request.action= TRADE_ACTION_MODIFY;
               request.order = order_ticket;
               request.price = NormalizeDouble(Bid - 20 * _Point,_Digits);
               request.sl = NormalizeDouble(request.price + SL * _Point,_Digits);
               request.tp = NormalizeDouble(request.price - TP * _Point,_Digits);
               OrderSend(request,result);
               if(result.retcode==10009 || result.retcode==10008)
                  Print("SellStop order modified");
               if(!OrderSend(request,result) || result.deal==0)
                 {
                  Print(ResultRetcodeDescription(result.retcode));
                  return;
                 }
              }
           }
        }
     }
  }
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
         str="Order placed";
         break;
      case TRADE_RETCODE_DONE:
         str="Request executed";
         break;
      case TRADE_RETCODE_DONE_PARTIAL:
         str="Request partially executed";
         break;
      case TRADE_RETCODE_ERROR:
         str="Request processing error";
         break;
      case TRADE_RETCODE_TIMEOUT:
         str="Request canceled because of time out";
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
         str="Trade disabled";
         break;
      case TRADE_RETCODE_MARKET_CLOSED:
         str="Market is closed";
         break;
      case TRADE_RETCODE_NO_MONEY:
         str="Insufficient funds for request execution";
         break;
      case TRADE_RETCODE_PRICE_CHANGED:
         str="Prices changed";
         break;
      case TRADE_RETCODE_PRICE_OFF:
         str="No quotes for request processing";
         break;
      case TRADE_RETCODE_INVALID_EXPIRATION:
         str="Invalid order expiration date in request";
         break;
      case TRADE_RETCODE_ORDER_CHANGED:
         str="Order state changed";
         break;
      case TRADE_RETCODE_TOO_MANY_REQUESTS:
         str="Too many requests";
         break;
      case TRADE_RETCODE_NO_CHANGES:
         str="No changes in request";
         break;
      case TRADE_RETCODE_SERVER_DISABLES_AT:
         str="Autotrading disabled by server";
         break;
      case TRADE_RETCODE_CLIENT_DISABLES_AT:
         str="Autotrading disabled by client terminal";
         break;
      case TRADE_RETCODE_LOCKED:
         str="Request blocked for processing";
         break;
      case TRADE_RETCODE_FROZEN:
         str="Order or position frozen";
         break;
      case TRADE_RETCODE_INVALID_FILL:
         str="Unsupported type of order execution for balance is specified";
         break;
      case TRADE_RETCODE_CONNECTION:
         str="No connection to the trading server";
         break;
      case TRADE_RETCODE_ONLY_REAL:
         str="Operation is allowed only for real accounts";
         break;
      case TRADE_RETCODE_LIMIT_ORDERS:
         str="Number of pending orders reached the limit";
         break;
      case TRADE_RETCODE_LIMIT_VOLUME:
         str="Volume of orders and positions for this symbol reached the limit";
         break;

      default:
         str="Unknown result";
     }

   return(str);
  }
//+------------------------------------------------------------------+  
