//+------------------------------------------------------------------+
//|                          Simple trade copier on the same account |
//|                                Copyright 2017, V                 |
//+------------------------------------------------------------------+
#property version "1.1"
#property copyright "Copyright © 2015, V"
#property description "Simple trade copier on the same account."
#property strict
//+------------------------------------------------------------------+
//| Enumerator of working mode                                       |
//+------------------------------------------------------------------+
input int slip=3;                // Slippage (in pips)
input double mult=1.0;           // Multiplier (for copied trade)
input int freq=1000;             // Check frequency (milliseconds)
input string prefix="VCPY_";     // Copied trade comment prefix
input int maxAge=30;             // Max age of the trade to be copied in sec
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class OrderContainer
  {
public:
   int               ticket;
   int               copiedFrom;
   string            symbol;
   int               type;
   double            price;
   double            lot;
   double            stoploss;
   double            takeprofit;
   datetime          opentime;
   string            comment;

                     OrderContainer(void);
                    ~OrderContainer(void)
     {
     }

                     OrderContainer(int pticket,
                                                      string psymbol,
                                                      int ptype,
                                                      double pprice,
                                                      double plot,
                                                      double pstoploss,
                                                      double ptakeprofit,
                                                      datetime popentime,
                                                      string pcomment)
     {
      ticket = pticket;
      symbol = psymbol;
      type=ptype;
      price=pprice;
      lot=plot;
      stoploss=pstoploss;
      takeprofit=takeprofit;
      opentime= popentime;
      comment = pcomment;

      if(StringSubstr(pcomment,0,StringLen(prefix))==prefix)
        {
         copiedFrom=StrToInteger(StringSubstr(pcomment,StringLen(prefix)));
        }
     }
  };
//+------------------------------------------------------------------+
//|Initialisation function                                           |
//+------------------------------------------------------------------+          
void init()
  {
   ObjectsDeleteAll();
   EventSetMillisecondTimer(freq);
   return;
  }
//+------------------------------------------------------------------+
//|Deinitialisation function                                         |
//+------------------------------------------------------------------+
void deinit()
  {
   ObjectsDeleteAll();
   EventKillTimer();
   return;
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnTimer()
  {
   OrderContainer *orders[100];
   int orderCount=OrdersTotal();

   if(orderCount==0)
      return;

//--- Saving information about all deals
   for(int i=0; i<orderCount; i++)
     {
      if(!OrderSelect(i,SELECT_BY_POS)) break;
      //if(!(OrderType()>1)) break;

      orders[i]=new OrderContainer(OrderTicket(),OrderSymbol(),OrderType(),OrderOpenPrice(),OrderLots(),OrderStopLoss(),OrderTakeProfit(),OrderOpenTime(),OrderComment());
     }

   OrderContainer *ordersToOpen[100];
   int toOpenIndex=0;

   OrderContainer *ordersToClose[100];
   int toCloseIndex=0;

   for(int i=0;i<orderCount;i++)
     {
      if(StringSubstr((orders[i]).comment,0,StringLen(prefix))==prefix)
        {
         bool toClose=true;

         for(int j=0;j<orderCount;j++)
           {
            if(orders[j].ticket==StrToInteger(StringSubstr(orders[i].comment,StringLen(prefix))))
              {
               //found the original trade, no need to close copied order
               toClose=false;
              }
           }

         if(toClose)
           {
            ordersToClose[toCloseIndex++]=orders[i];
           }
        }
      else
        {
         bool toOpen=true;

         for(int j=0;j<orderCount;j++)
           {
            if(orders[j].comment==(prefix+IntegerToString(orders[i].ticket)))
              {
               //found the copied trade, no need to open again
               toOpen=false;
              }
           }

         if(toOpen && (TimeCurrent()-orders[i].opentime)<=maxAge)
           {
            ordersToOpen[toOpenIndex++]=orders[i];
           }
        }
     }

//Open trades that are to be closed
   for(int i=0; i<toOpenIndex; i++)
     {
      OpenMarketOrder(ordersToOpen[i].ticket,ordersToOpen[i].symbol,ordersToOpen[i].type,ordersToOpen[i].price,ordersToOpen[i].lot*mult);
     }

   for(int i=0; i<toCloseIndex; i++)
     {
      int orderCloseReturnValue=-1;

      if(ordersToClose[i].type==0)
        {
         orderCloseReturnValue=OrderClose(ordersToClose[i].ticket,ordersToClose[i].lot,MarketInfo(ordersToClose[i].symbol,MODE_BID),slip);
        }
      else if(ordersToClose[i].type==1)
        {
         orderCloseReturnValue=OrderClose(ordersToClose[i].ticket,ordersToClose[i].lot,MarketInfo(ordersToClose[i].symbol,MODE_ASK),slip);
        }

      if(orderCloseReturnValue==-1)
        {
         Print("Error: ",GetLastError()," during closing the market order.");
        }
      else
        {
         Print("Market order ",IntegerToString(ordersToClose[i].ticket)," (",ordersToClose[i].comment,") closed as parent trade was also closed.");
        }

     }

   for(int i=0; i<toOpenIndex; i++)
     {
      delete(ordersToOpen[i]);
     }

   for(int i=0; i<toCloseIndex; i++)
     {
      delete(ordersToClose[i]);
     }

   for(int i=0; i<orderCount; i++)
     {
      delete(orders[i]);
     }

   ArrayFree(ordersToOpen);
   ArrayFree(ordersToClose);
   ArrayFree(orders);
  }
//+------------------------------------------------------------------+
//|Open market execution orders                                      |
//+------------------------------------------------------------------+
void OpenMarketOrder(int ticket_,string symbol_,int type_,double price_,double lot_)
  {
   double market_price=MarketInfo(symbol_,MODE_BID);
   if(type_==0) market_price=MarketInfo(symbol_,MODE_ASK);

   double delta;

   delta=MathAbs(market_price-price_)/MarketInfo(symbol_,MODE_POINT);

   if(delta>slip)
      return;

   int orderSendReturnValue=OrderSend(symbol_,type_,lot_,market_price,slip,0,0,prefix+IntegerToString(ticket_));

   if(orderSendReturnValue==-1)
     {
      Print("Error: ",GetLastError()," during opening the market order.");
     }
   else
     {
      Print("Market order ",IntegerToString(ticket_)," copied as ",prefix+IntegerToString(ticket_),".");
     }

   return;
  }
//+------------------------------------------------------------------+
