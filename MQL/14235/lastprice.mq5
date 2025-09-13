//+------------------------------------------------------------------+
//|                                                    LastPrice.mq5 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "2.00"
input long Interval=400;                                       // Мин. отклонение цены сделки
input long minVolume=1;                                        // Мин. объем сделки
input long maxVolume=900000;                                   // Макс. объем сделки
input long Spread= 200;                                        // Макс. спред
input long Lots=1;                                             // Лот
input long SlPips=400;                                         // Stop Loss
input ENUM_ORDER_TYPE_FILLING Filling=ORDER_FILLING_RETURN;    // Режим заполнения ордера
double tradePrice= 0;
double LastPrice = 0;
double interval;
double pips;
#include <trade/trade.mqh>
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   interval=PointsToPrice(Interval); //Из пунктов в цену символa
   pips=PointsToPrice(SlPips);
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
//| Expert Points to price function                                  |
//+------------------------------------------------------------------+
double PointsToPrice(const long a_points)
  {
   double step_price=SymbolInfoDouble(_Symbol,SYMBOL_TRADE_TICK_SIZE);
   double a_price=(double(a_points)*_Point)/step_price;

   if(a_points<0)
     {
      a_price=MathFloor(a_price)*step_price;
     }
   else
     {
      a_price=MathCeil(a_price)*step_price;
     }

   return( NormalizeDouble( a_price, _Digits ) );
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
//---
   if(ask==DBL_MAX) ask=0;
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
   request.action=TRADE_ACTION_PENDING;        // setting a pending order
   request.magic = 68975;                      // ORDER_MAGIC
   request.symbol = _Symbol;                   // symbol
   request.volume = double( Lots );
//---   
   if(buy_sell)
     {
      request.type=ORDER_TYPE_BUY_LIMIT;       // order type
      request.sl=price-pips;
     }
   else
     {
      request.type=ORDER_TYPE_SELL_LIMIT;
      request.sl=price+pips;
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
//| Expert Check trading time function                               |
//+------------------------------------------------------------------+
bool CheckTradingTime()
  {
   MqlDateTime local_time;
   TimeLocal(local_time);
   MqlDateTime tick_time;
   TimeTradeServer(tick_time);
//---  
   if(( tick_time.day_of_week==0) || (tick_time.day_of_week==6))
     {
      return( false );
     }
//---
   if(( tick_time.hour>=0) && (tick_time.hour<10))
     {
      return( false );
     }
//---
   uint trade_time=tick_time.hour*3600+tick_time.min*60+tick_time.sec;
//---
   if(((trade_time>=(10*3600+10+5*60+30)) && (trade_time<(13*3600+54*60+30))) || 
      ( ( trade_time >= ( 14 * 3600 + 8 * 60 + 30 ) ) && ( trade_time < ( 15 * 3600 + 44 * 60 + 30 ) ) ) ||
      ( ( trade_time >= ( 16 * 3600 + 5 * 60 + 30 ) ) && ( trade_time < ( 18 * 3600 + 39 * 60 + 30 ) ) ) ||
      ( ( trade_time >= ( 19 * 3600 + 15 * 60 + 10 ) ) && ( trade_time < ( 23 * 3600 + 44 * 60  + 30 ) ) ) )
     {
      return( true );
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
      double ask,bid,last,price=0;
      ulong ticket;
      long lastvol;
      //---            
      if(GetStakanValues(_Symbol,ask,bid)) //Берем здесь, потому что нужно как и для открытия позиции, так и для закрытия
        {
         last=SymbolInfoDouble(_Symbol,SYMBOL_LAST);
         lastvol=SymbolInfoInteger(_Symbol,SYMBOL_VOLUME);
         //---      
         if(PositionSelect(_Symbol))
           {
            if(ENUM_POSITION_TYPE(PositionGetInteger(POSITION_TYPE))==POSITION_TYPE_BUY)///BUY
              {
               if((!CheckTradingTime()) || (((LastPrice<=bid || LastPrice<=ask) && (tradePrice<=bid)) || (tradePrice<bid)))
                 {
                  //--- Проверка закрытия, если да то
                  int i=PositionsTotal();//Wait openedPosition 
                  if(SetOrder(bid,false,ticket))
                    {
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
              }
            else///SELL
              {
               if((!CheckTradingTime()) || (((LastPrice>=ask || LastPrice>=bid) && (tradePrice>=ask)) || (tradePrice>ask)))
                 {
                  //--- Проверка закрытия, если да то
                  int i=PositionsTotal();//Wait openedPosition 
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
         else //No Position:
         if(CheckTradingTime() && (lastvol>=minVolume && lastvol<=maxVolume) && Spread>=(ask-bid))
           {
            //--- Делаем проверки, какой ордер установить (BUY or SELL), и берем цену
            if(last>=ask+interval)///BUY
              {
               //--- Устанавливаем ордер 
               int i=PositionsTotal();//Wait openedPosition 
               if(SetOrder(ask,true,ticket)) // true ордер на покупку; false - ордер на продажу
                 {
                  //--- Ордер установлен
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
                  LastPrice=last;
                 }
              }
            //--- Делаем проверки, какой ордер установить (BUY or SELL), и берем цену
            if(last<=bid-interval)///SELL
              {//--- Устанавливаем ордер 
               int i=PositionsTotal();//Wait openedPosition 
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
                  LastPrice=last;
                 }
              }
           }
        }
     }
  }
//+------------------------------------------------------------------+
