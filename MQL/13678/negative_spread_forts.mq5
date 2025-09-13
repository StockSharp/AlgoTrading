//+------------------------------------------------------------------+
//|                                        negative_spread_FORTS.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "1987pavlov"
#property link      "https://www.mql5.com"
#property version   "2.0"
//+------------------------------------------------------------------+
//| Includes                                                         |
//+------------------------------------------------------------------+
#include <trade/trade.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input double Lots=1;       //Лот
input int TpPips = 5000;   //Take Profit
input int SlPips = 5000;   //Stop Loss
input ENUM_ORDER_TYPE_FILLING Filling=ORDER_FILLING_RETURN;  //Режим заполнения ордера
//+------------------------------------------------------------------+
//| Global variables                                                 |
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   if(!MarketBookAdd(_Symbol))
     {
      MessageBox("Не добавлен стакан по символу "+_Symbol);
      return( INIT_FAILED );
     }
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   MarketBookRelease(_Symbol);
  }
//+------------------------------------------------------------------+
//| Get Stakan values function                                       |
//+------------------------------------------------------------------+ 
bool GetStakanValues(const string aSymbol,double &ask,double &bid)
  {
   MqlBookInfo book_price[];
   bid = 0;
   ask = DBL_MAX;
//--- Get stakan
   if(MarketBookGet(aSymbol,book_price))
     {
      int size=ArraySize(book_price);
      //---    
      if(size>0)
        {
         for(int i=0; i<size; i++)
           {
            if(book_price[i].type==BOOK_TYPE_SELL)
              {
               if(book_price[i].price<ask)
                 {
                  ask=book_price[i].price;
                 }
              }
            else
            if(book_price[i].type==BOOK_TYPE_BUY)
              {
               bid=book_price[i].price;
               return( true );
              }
           }
        }
     }
   return( false);
  }
//+------------------------------------------------------------------+
//| Set order function                                               |
//+------------------------------------------------------------------+  
bool SetOrder(const double price,const bool buy_sell,ulong  &ticket)
  {
   MqlTradeRequest request={0};
   MqlTradeResult result={0};
//--- Fill structure
   request.action=TRADE_ACTION_PENDING;         // setting a pending order
   request.magic = 68975;                      // ORDER_MAGIC
   request.symbol = _Symbol;                   // symbol
   request.volume = double( Lots );
//---   
   if(buy_sell)
     {
      request.type=ORDER_TYPE_BUY_LIMIT;              // order type
      request.sl=price-SlPips;
     }
   else
     {
      request.type=ORDER_TYPE_SELL_LIMIT;
      request.sl = price + SlPips;
      request.tp = price - TpPips;
     }
   request.price=price;                        // open price
   request.type_filling=Filling;
   request.deviation=0;
   request.type_time=ORDER_TIME_DAY;
//---
   if(OrderSend(request,result))
     {
      if(( result.retcode==TRADE_RETCODE_PLACED) && (result.order>0))
        {
         ticket=result.order;
         return( true );
        }
     }
   else
     {
      Print(" Order not set!");
     }
   return( false );
  }
//+------------------------------------------------------------------+
//| BookEvent function                                               |
//+------------------------------------------------------------------+
void OnBookEvent(const string &symbol)
  {
   if(symbol==_Symbol)
     {
      double ask,bid,price=0,tradePrice;
      ulong ticket;
      //---            
      if(GetStakanValues(_Symbol,ask,bid)) //Берём здесь, потому что нужно для открытия позиции
        {
         //---      
         if(!PositionSelect(_Symbol))///No Positin:
           {
            //--- Делаем проверки какой ордер установить (BUY or SELL ) и берём цену
            //--- Устанавливаем ордер 
            int i=PositionsTotal();//Wait openedPosition 
            if(ask-bid<0) //Negative Spread
               if(SetOrder(bid,false,ticket)) // true ордер на покупку; false - ордер на продажу
                 {
                  //--- Ордер установлен
                  tradePrice=bid;
                  while(i==PositionsTotal())//Wait openedPosition
                    {
                     GetStakanValues(_Symbol,ask,bid);//Close freezy order
                     if(bid!=tradePrice && OrderSelect(ticket))
                       {
                        CTrade trade;
                        trade.SetTypeFilling(Filling);
                        if(trade.OrderDelete(ticket)) break;
                       }
                    }
                 }
           }
         if(PositionSelect(_Symbol)) //ClosePos
           {
            int i=PositionsTotal();//Wait openedPosition 
            if(GetStakanValues(_Symbol,ask,bid)) //Берём новые данные
               if(SetOrder(ask,true,ticket))
                 {
                  tradePrice=ask;
                  while(i==PositionsTotal())//Wait openedPosition
                    {
                     GetStakanValues(_Symbol,ask,bid);//Close freezy order
                     if(ask!=tradePrice && OrderSelect(ticket))
                       {
                        CTrade trade;
                        trade.SetTypeFilling(Filling);
                        if(trade.OrderDelete(ticket)) break;
                       }
                    }
                 }
           }
        }
     }
  }
//+------------------------------------------------------------------+
