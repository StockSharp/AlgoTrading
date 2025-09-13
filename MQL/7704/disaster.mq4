//+------------------------------------------------------------------+
//|                                                     disaster.mq4 |
//|                                                         Max Fade |
//|                                                          http:// |
//+------------------------------------------------------------------+
#property copyright "Max Fade"
#property link      "http://"
extern double StopLoss = 30;
extern double TakeProfit = 70;
extern int MAPER = 590;
int Magic = 54321;

datetime TradeTimeOut;

int init()
  {
   return(0);
  }
int deinit()
  {
   return(0);
  }
int start()
  {
// информация по предыдущему ордеру
int preOrderType=-1,preTicket=0;
double prePriceOpen=0,prePriceClose=0,preTP=0,preSL=0;
bool STOPLEVELCHECK;
int i,j,k,l,ticket,err,nBars=0,numOrdersCount=0,numSLCount=0;
int total=OrdersTotal();
double Lot=0.0,PriceStop=0.0,PriceLimit=0.0,Price=0.0;
   if (TradeTimeOut==0) TradeTimeOut=CurTime();
   if ( TradeTimeOut > CurTime() ) return(0);
   if ( Bars < 1000) return(0);
   // обработка истории - поиск ордеров
   total=OrdersHistoryTotal();
   // начинаем выборку с конца, после найденного последнего ордера прерываем дальнейший поиск
   for (i=total;i>=0;i--)
      {
      if (OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) continue;
      if (OrderMagicNumber() == Magic)
         {
         preOrderType=OrderType();
         preTicket=OrderTicket();
         prePriceOpen=OrderOpenPrice();
         preTP=OrderTakeProfit();
         preSL=OrderStopLoss();
         prePriceClose=OrderClosePrice();
         // последний ордер нашли, больше нам ничего не нужно
         break;
         }
      }
   // выборка текущих позиций
      total=OrdersTotal();
      for (i=0;i<total;i++)
         {
         if (OrderSelect(i,SELECT_BY_POS)==false) continue;
         if (OrderMagicNumber() == Magic)
            {
            numOrdersCount++;
            // если цена отложенного ордера отличается от необходимого - модифицируем
            STOPLEVELCHECK = true;
            if (OrderType()==OP_BUYSTOP || OrderType()==OP_SELLSTOP)
               {
               Price=NormalizeDouble(iMA(NULL,PERIOD_M1,MAPER,0,MODE_SMA,PRICE_CLOSE,0),Digits);
               if (OrderType()==OP_BUYSTOP)
                  {
                  PriceStop=Price - MarketInfo(Symbol(),MODE_SPREAD)*Point - StopLoss * Point;
                  PriceLimit=Price + TakeProfit * Point;
                  STOPLEVELCHECK = (Price-Ask) > MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;
                  if (preOrderType==OP_BUY && (prePriceOpen-prePriceClose)>0)
                     {
                     PriceLimit=Price + 0.5 * TakeProfit * Point;
                     }
                  }
               if (OrderType()==OP_SELLSTOP)
                  {
                  PriceStop=Price + StopLoss * Point;
                  PriceLimit=Price - MarketInfo(Symbol(),MODE_SPREAD)*Point - TakeProfit * Point;
                  STOPLEVELCHECK = (Bid-Price) > MarketInfo(Symbol(),MODE_STOPLEVEL)*Point;
                  if (preOrderType==OP_SELL && (prePriceOpen-prePriceClose)<0)
                     {
                     PriceLimit=Price - MarketInfo(Symbol(),MODE_SPREAD)*Point - 0.5*TakeProfit * Point;
                     }
                  }
               if (NormalizeDouble(OrderOpenPrice()-Price,Digits)!=0 
                     && NormalizeDouble(OrderStopLoss()-PriceStop,Digits)!=0
                     && NormalizeDouble(OrderTakeProfit()-PriceLimit,Digits)!=0 && STOPLEVELCHECK)
                  {
                  OrderModify(OrderTicket(),Price, PriceStop, PriceLimit ,0,CLR_NONE);
                  }
               }
            Price=NormalizeDouble(iMA(NULL,PERIOD_M1,MAPER,0,MODE_SMA,PRICE_CLOSE,0),Digits);
            if (OrderType()==OP_BUY && Minute()/5==NormalizeDouble(Minute()/5,0) )
               {
               if (NormalizeDouble(Bid-OrderStopLoss()-70*Point,Digits)>0)
                  {
                  //OrderModify(OrderTicket(),OrderOpenPrice(),,0,0,CLR_NONE);
                  }
               }
            if (OrderType()==OP_SELL && Minute()/5==NormalizeDouble(Minute()/5,0) )
               {
               if (NormalizeDouble(OrderStopLoss()-Ask-70*Point,Digits)>0)
                  {
                  //OrderModify(OrderTicket(),OrderOpenPrice(),, 0,0,CLR_NONE);
                  }
               }
            }
         }
   Lot=NormalizeDouble(0.4*AccountFreeMargin()/1000,1);
   if (Lot>MarketInfo(Symbol(),MODE_MAXLOT)) Lot=MarketInfo(Symbol(),MODE_MAXLOT);
   //if (Lot>5) Lot=5.0;
   if (Lot<MarketInfo(Symbol(),MODE_MINLOT)) return(0);
   if (numOrdersCount==0) // если открытых ордеров нет - ставим отложенные ордера
   {
      Price=NormalizeDouble(iMA(NULL,PERIOD_M1,MAPER,0,MODE_SMA,PRICE_CLOSE,0),Digits);
      if (Bid-Price > 20 * Point)
         {
         ticket=OrderSend(Symbol(), OP_SELLSTOP, Lot ,
            Price, 1, Price + StopLoss * Point, Price - TakeProfit * Point, "MACROS2", Magic, 0, Red);
            //TradeTimeOut=TimeCurrent()+60*60*10;
         }
      if (Price-Ask > 20 * Point)
         {
         ticket=OrderSend(Symbol(), OP_BUYSTOP, Lot ,
            Price+MarketInfo(Symbol(),MODE_SPREAD)*Point, 1,
            Price + MarketInfo(Symbol(),MODE_SPREAD)*Point - StopLoss * Point,
            Price + MarketInfo(Symbol(),MODE_SPREAD)*Point + TakeProfit * Point, "MACROS2", Magic, 0, Red);
            //TradeTimeOut=TimeCurrent()+60*60*10;
         }
   }
   return(0);
  }

