//+------------------------------------------------------------------+
//|                                           SupportResistTrade.mq4 |
//|                                 Copyright © 2008, Gryb Alexander |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2008, Gryb Alexander"
#property link      ""

extern int numBars = 55;

extern int maPeriod = 500;


double support;
double resist;
string trendType;
int timeFrame = 1;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
    ObjectCreate("lineSupport",OBJ_HLINE,0,0,0);
    ObjectSet("lineSupport",OBJPROP_COLOR,Blue);
    
    ObjectCreate("lineResist",OBJ_HLINE,0,0,0);
    ObjectSet("lineResist",OBJPROP_COLOR,Red);
    
    ObjectCreate("lblTrendType",OBJ_LABEL,0,0,0,0,0);
    ObjectSet("lblTrendType",OBJPROP_XDISTANCE,50);
    ObjectSet("lblTrendType",OBJPROP_YDISTANCE,50);
    ObjectSetText("lblTrendType","TrendType",14,"Tahoma",Red);

//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
    ObjectsDeleteAll();
    
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
  MarketAnalize();
  if(OrdersTotal()==0)
   CheckForOpen();
  else
   CheckForClose();
//----
   return(0);
  }

void MarketAnalize()
{
  // Определяем линии поддержки\сопротивления
  support = 10000;
  resist = 0;  
  for(int k = 1;k<=numBars;k++)
  {
   if(support>iLow(Symbol(),timeFrame,k))
     support = iLow(Symbol(),timeFrame,k);
   if(resist<iHigh(Symbol(),timeFrame,k))
     resist = iHigh(Symbol(),timeFrame,k);
  }   
  ObjectSet("lineSupport",OBJPROP_PRICE1,support);
  ObjectSet("lineResist",OBJPROP_PRICE1,resist);
  
  // Определяем состояние рынка: медведи или быки
  double ma = iMA(Symbol(),0,maPeriod,0,MODE_EMA,PRICE_OPEN,0);
  
  if(Open[0]>ma)
  {
    trendType = "bullish";
  }
  if(Open[0]<ma)
  {
    trendType = "bearish";
  }
  

  ObjectSetText("lblTrendType",trendType);
  
  //Итого: есть линии поддержки\сопротивления
  //       определено состояние рынка(бычий\медвежий)
}

void CheckForOpen()
{

  if(trendType=="bullish")
  {
    if(Ask>resist) OrderSend(Symbol(),OP_BUY,1,Ask,3,support,0);
  }
  if(trendType=="bearish")
  {
    if(Bid<support)  OrderSend(Symbol(),OP_SELL,1,Bid,3,resist,0);
  }
}
void CheckForClose()
{
 OrderSelect(0,SELECT_BY_POS);
 if(OrderProfit()>0)
 {
   if(OrderType()==OP_BUY)
   {
     if(Bid<support) OrderClose(OrderTicket(),OrderLots(),Bid,3);
   }
   if(OrderType()==OP_SELL)
   { 
      if(Ask>resist)  OrderClose(OrderTicket(),OrderLots(),Ask,3);
   }
 }
//Trailing
 if(OrderType()==OP_BUY)
 {
   if((Ask>OrderOpenPrice()+Point*20)&&(OrderStopLoss()<(OrderOpenPrice()+Point*10)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*10,OrderTakeProfit(),0,Blue);
   if((Ask>OrderOpenPrice()+Point*40)&&(OrderStopLoss()<(OrderOpenPrice()+Point*20)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*20,OrderTakeProfit(),0,Blue);    
   if((Ask>OrderOpenPrice()+Point*60)&&(OrderStopLoss()<(OrderOpenPrice()+Point*30)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()+Point*30,OrderTakeProfit(),0,Blue);
 }
 if(OrderType()==OP_SELL)
 {
   if((Bid<OrderOpenPrice()-Point*20)&&(OrderStopLoss()>(OrderOpenPrice()-Point*10)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*10,OrderTakeProfit(),0,Blue);     
   if((Bid<OrderOpenPrice()-Point*40)&&(OrderStopLoss()>(OrderOpenPrice()-Point*20)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*20,OrderTakeProfit(),0,Blue);     
   if((Bid<OrderOpenPrice()-Point*60)&&(OrderStopLoss()>(OrderOpenPrice()-Point*30)))
    OrderModify(OrderTicket(),OrderOpenPrice(),OrderOpenPrice()-Point*30,OrderTakeProfit(),0,Blue);     
 }
}
//+------------------------------------------------------------------+