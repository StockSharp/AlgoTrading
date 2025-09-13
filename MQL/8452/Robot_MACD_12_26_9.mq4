//+------------------------------------------------------------------+
//|                                               Robot_MACD_12.26.9 |
//|                                                     Tokman Yuriy |
//|                                            yuriytokman@gmail.com |
//+------------------------------------------------------------------+
             //внешние переменные
extern double TakeProfit = 300;
extern double Lots = 0.1;

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   double MacdCurrent, MacdPrevious, SignalCurrent,SignalPrevious;
   int cnt, ticket, total;

   MacdCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,0);
   MacdPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_MAIN,1);
   SignalCurrent=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,0);
   SignalPrevious=iMACD(NULL,0,12,26,9,PRICE_CLOSE,MODE_SIGNAL,1);

   total=OrdersTotal();
   if(total<1)//проверка количества ордеров 
     {
      // проверка свободных средств
      if(AccountFreeMargin()<(1000*Lots))//количество свободных средств
        {
         Print("Недостаточно средств = ", AccountFreeMargin());
         return(0);  
        }
      // открытие длинной позиции
      if(MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious
         && MacdCurrent<0 && SignalCurrent<0 )
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,0,Ask+TakeProfit*Point,"-",0,0,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("открыта позиция BUY : ",OrderOpenPrice());
           }
         else Print("Ошибка при открытии BUY позиции : ",GetLastError()); 
         return(0);
        }
      // открытие короткой позиции
      if(MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious
         && MacdCurrent>0 && SignalCurrent>0)
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,0,Bid-TakeProfit*Point,"-",0,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("открыта позиция SELL : ",OrderOpenPrice());
           }
         else Print("Ошибка при открытии SELL позиции : ",GetLastError()); 
         return(0); 
        }
      return(0);
     }
   // условия закрытия ордеров   
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&   // наличие открытых ордеров 
         OrderSymbol()==Symbol())  // совпадают ли финансовые инструменты
        {
         if(OrderType()==OP_BUY)   // открыта длинная позиция
           {
            // условие закрытия
            if(MacdCurrent<SignalCurrent && MacdPrevious>SignalPrevious)
                {
                 OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
                 return(0);
                }
           }
         else // открыта короткая позиция
           {
            // условие закрытия
            if( MacdCurrent>SignalCurrent && MacdPrevious<SignalPrevious)
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
               return(0);
              }
           }
        }
     }
   return(0);
  }