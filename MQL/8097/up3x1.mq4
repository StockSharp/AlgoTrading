//+------------------------------------------------------------------+
//|                                                        up3x1.mq4 |
//|                                Copyright © 2006, izhutov aKa PPP |
//|                                                izhutov@yandex.ru |
//+------------------------------------------------------------------+
#define MAGICMA  20050610
extern double Lots               = 0.1;
extern double TakeProfit         = 150;
extern double StopLoss           = 100;
extern double TrailingStop       = 100;
double Points;
int init ()
  {
   Points = MarketInfo (Symbol(), MODE_POINT);
   return(0);
  }
int CalculateCurrentOrders(string symbol)
  {
   int buys=0,sells=0;
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false) break;
      if(OrderSymbol()==Symbol() && OrderMagicNumber()==MAGICMA)
        {
         if(OrderType()==OP_BUY)  buys++;
         if(OrderType()==OP_SELL) sells++;
        }
     }
   if(buys>0) return(buys);
   else       return(-sells);
  }
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();  
   int    losses=0;                 
   lot=NormalizeDouble(AccountFreeMargin()*0.02/1000.0,1);
   if(3>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(OrderSymbol()!=Symbol() || OrderType()>OP_SELL) continue;
         //----
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/3,1);
     }
   if(lot<0.1) lot=0.1;
   return(lot);
  }
void CheckForOpen()
  {
   double ma1;
   double ma2;
   double ma3;
   double ma4;
   double ma5;
   double ma6;
   int    res;
   if(Volume[0]>1) return;
   ma1=iMA(NULL,0,24,6,0,PRICE_CLOSE,1);
   ma4=iMA(NULL,0,24,6,0,PRICE_CLOSE,0);
   ma2=iMA(NULL,0,60,6,0,PRICE_CLOSE,1);
   ma5=iMA(NULL,0,60,6,0,PRICE_CLOSE,0);
   ma3=iMA(NULL,0,120,6,0,PRICE_CLOSE,1);
   ma6=iMA(NULL,0,120,6,0,PRICE_CLOSE,0);
     if (ma1<ma2 && ma2<ma3 && ma5<ma4 && ma4<ma6)
     {
      res=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask,3,Ask-StopLoss*Points,Ask+TakeProfit*Points,"",MAGICMA,0,Blue);
      return;
     }
     if (ma1>ma2 && ma2>ma3 && ma5>ma4 && ma4>ma6)  
     {
      res=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid,3,Bid+StopLoss*Points,Bid-TakeProfit*Points,"",MAGICMA,0,Red);
      return;
     }
  }
void CheckForClose()
  {
   double ma1;
   double ma2;
   double ma3;
   double ma4;
   double ma5;
   double ma6;
   if(Volume[0]>1) return;
   ma1=iMA(NULL,0,24,6,0,PRICE_CLOSE,1);
   ma4=iMA(NULL,0,24,6,0,PRICE_CLOSE,0);
   ma2=iMA(NULL,0,60,6,0,PRICE_CLOSE,1);
   ma5=iMA(NULL,0,60,6,0,PRICE_CLOSE,0);
   ma3=iMA(NULL,0,120,6,0,PRICE_CLOSE,1);
   ma6=iMA(NULL,0,120,6,0,PRICE_CLOSE,0);
   for(int i=0;i<OrdersTotal();i++)
     {
      if(OrderSelect(i,SELECT_BY_POS,MODE_TRADES)==false)        break;
      if(OrderMagicNumber()!=MAGICMA || OrderSymbol()!=Symbol()) continue;
      if(OrderType()==OP_BUY)
        {
        if(ma1>ma2>ma3 && ma6<ma4<ma5) OrderClose(OrderTicket(),OrderLots(),Bid,3,White);
        break;
        }
if(TrailingStop>0)  
              {                
               if(Bid-OrderOpenPrice()>Points*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-Points*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Points*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }        
      if(OrderType()==OP_SELL)
        {
        if(ma1<ma2<ma3 && ma6>ma4>ma5) OrderClose(OrderTicket(),OrderLots(),Ask,3,White);
        break;
        }
if(TrailingStop>0)  
              {                 
               if((OrderOpenPrice()-Ask)>(Points*TrailingStop))
                 {
                  if(OrderStopLoss()==0.0 || 
                     OrderStopLoss()>(Ask+Points*TrailingStop))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Points*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }     
     }
  }
void start()
  {
   if(Bars<100 || IsTradeAllowed()==false) return;
   if(CalculateCurrentOrders(Symbol())==0) CheckForOpen();
   else                                    CheckForClose();
  }

