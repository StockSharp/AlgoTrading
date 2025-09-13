//+------------------------------------------------------------------+
//|                                     EMA_CROSS_CONTEST_HEDGED.mq4 |
//|                                                      Coders Guru |
//|                                         http://www.forex-tsd.com |
//+------------------------------------------------------------------+
#property copyright "Coders Guru"
#property link      "http://www.forex-tsd.com"
//---- Trades limits
extern double    TakeProfit=150;
extern double    TrailingStop=40;
extern double    StopLoss=150;
extern int       HedgeLevel=6;
extern bool      UseClose=true;
extern bool      UseMACD=true;
extern double    Expiration=7200;
extern int       ShortEma=4;
extern int       LongEma=24;
extern int       CurrentBar=1;
extern double    Lots=0.10;
string           ExpertName="EMA_CROSS_CONTEST_HEDGED";
int              Magic=20060328;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {
   return(0);
  }
//+------------------------------------------------------------------
/*bool NewBar(int bar)
{
   static datetime lastbar=0;
   datetime curbar = Time[bar];
   if(lastbar!=curbar)
   {
      lastbar=curbar;
      return (true);
   }
   else
   {
      return(false);
   }
} */
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int Crossed()
  {
   double EmaLongPrevious=iMA(NULL,0,LongEma,0,MODE_EMA, PRICE_CLOSE, CurrentBar+1);
   double EmaLongCurrent=iMA(NULL,0,LongEma,0,MODE_EMA, PRICE_CLOSE, CurrentBar);
   double EmaShortPrevious=iMA(NULL,0,ShortEma,0,MODE_EMA, PRICE_CLOSE, CurrentBar+1);
   double EmaShortCurrent=iMA(NULL,0,ShortEma,0,MODE_EMA, PRICE_CLOSE, CurrentBar);
//----
   if (EmaShortPrevious<EmaLongPrevious && EmaShortCurrent>EmaLongCurrent)    return(1); //up trend
   if (EmaShortPrevious>EmaLongPrevious && EmaShortCurrent<EmaLongCurrent)    return(2); //down trend
//----
   return(0); //elsewhere
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool isNewSumbol(string current_symbol)
  {
   //loop through all the opened order and compare the symbols
   int total =OrdersTotal();
   for(int cnt=0;cnt < total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      string selected_symbol=OrderSymbol();
      if (current_symbol==selected_symbol)
         return(False);
     }
   return(True);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//---- 
   int cnt, ticket, total;
   double SEma, LEma;
   if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
   if(TakeProfit<10)
     {
      Print("TakeProfit less than 10");
      return(0);  // check TakeProfit
     }
   total =OrdersTotal();
//----
   double macd=iMACD(NULL,0,4,24,12,PRICE_CLOSE,MODE_SIGNAL,CurrentBar);
   if(UseMACD==false) macd=0;
   if(total < 1 || isNewSumbol(Symbol()))
     {
      if(Crossed()==1 &&  macd>=0 )
        {
         ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Ask-StopLoss*MarketInfo(Symbol(),MODE_POINT),Ask+TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + 3600,Green);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
           }
         else Print("Error opening BUY order : ",GetLastError());
//----
         OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+HedgeLevel*MarketInfo(Symbol(),MODE_POINT),3,(Ask+HedgeLevel*MarketInfo(Symbol(),MODE_POINT))-StopLoss*MarketInfo(Symbol(),MODE_POINT),(Ask+HedgeLevel*MarketInfo(Symbol(),MODE_POINT))+TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+(HedgeLevel+2)*MarketInfo(Symbol(),MODE_POINT),3,(Ask+(HedgeLevel+2)*MarketInfo(Symbol(),MODE_POINT))-StopLoss*MarketInfo(Symbol(),MODE_POINT),(Ask+(HedgeLevel+2)*MarketInfo(Symbol(),MODE_POINT))+TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+(HedgeLevel+3)*MarketInfo(Symbol(),MODE_POINT),3,(Ask+(HedgeLevel+3)*MarketInfo(Symbol(),MODE_POINT))-StopLoss*MarketInfo(Symbol(),MODE_POINT),(Ask+(HedgeLevel+3)*MarketInfo(Symbol(),MODE_POINT))+TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         OrderSend(Symbol(),OP_BUYSTOP,Lots,Ask+(HedgeLevel+4)*MarketInfo(Symbol(),MODE_POINT),3,(Ask+(HedgeLevel+4)*MarketInfo(Symbol(),MODE_POINT))-StopLoss*MarketInfo(Symbol(),MODE_POINT),(Ask+(HedgeLevel+4)*MarketInfo(Symbol(),MODE_POINT))+TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
//----
         return(0);
        }
      if(Crossed ()==2 &&  macd<=0  )
        {
         ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Bid+StopLoss*MarketInfo(Symbol(),MODE_POINT),Bid-TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,0,Red);
         if(ticket>0)
           {
            if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
           }
         else Print("Error opening SELL order : ",GetLastError());
//----
         OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-HedgeLevel*MarketInfo(Symbol(),MODE_POINT),3,(Bid-HedgeLevel*MarketInfo(Symbol(),MODE_POINT))+StopLoss*MarketInfo(Symbol(),MODE_POINT),(Bid-HedgeLevel*MarketInfo(Symbol(),MODE_POINT))-TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-(HedgeLevel+1)*MarketInfo(Symbol(),MODE_POINT),3,(Bid-(HedgeLevel+2)*MarketInfo(Symbol(),MODE_POINT))+StopLoss*MarketInfo(Symbol(),MODE_POINT),(Bid-(HedgeLevel+2)*MarketInfo(Symbol(),MODE_POINT))-TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-(HedgeLevel+2)*MarketInfo(Symbol(),MODE_POINT),3,(Bid-(HedgeLevel+3)*MarketInfo(Symbol(),MODE_POINT))+StopLoss*MarketInfo(Symbol(),MODE_POINT),(Bid-(HedgeLevel+3)*MarketInfo(Symbol(),MODE_POINT))-TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         OrderSend(Symbol(),OP_SELLSTOP,Lots,Bid-(HedgeLevel+3)*MarketInfo(Symbol(),MODE_POINT),3,(Bid-(HedgeLevel+4)*MarketInfo(Symbol(),MODE_POINT))+StopLoss*MarketInfo(Symbol(),MODE_POINT),(Bid-(HedgeLevel+4)*MarketInfo(Symbol(),MODE_POINT))-TakeProfit*MarketInfo(Symbol(),MODE_POINT),ExpertName,Magic,CurTime() + Expiration,Blue);
         return(0);
        }
      return(0);
     }
   for(cnt=0;cnt<total;cnt++)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)   // long position is opened
           {
            if(UseClose)
              {
               if(Crossed ()== 2)
                 {
                  OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close position
                  return(0); // exit
                 }
              }
            // check for trailing stop
            if(TrailingStop>0)
              {
               if(Bid-OrderOpenPrice()>MarketInfo(Symbol(),MODE_POINT)*TrailingStop)
                 {
                  if(OrderStopLoss()<Bid-MarketInfo(Symbol(),MODE_POINT)*TrailingStop)
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-MarketInfo(Symbol(),MODE_POINT)*TrailingStop,OrderTakeProfit(),0,Green);
                     return(0);
                    }
                 }
              }
           }
         else // go to short position
           {
            if(UseClose)
              {
               if(Crossed ()== 1)
                 {
                  OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close position
                  return(0); // exit
                 }
              }
            // check for trailing stop
            if(TrailingStop>0)
              {
               if((OrderOpenPrice()-Ask)>(MarketInfo(Symbol(),MODE_POINT)*TrailingStop))
                 {
                  if((OrderStopLoss()>(Ask+MarketInfo(Symbol(),MODE_POINT)*TrailingStop)) || (OrderStopLoss()==0))
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Ask+MarketInfo(Symbol(),MODE_POINT)*TrailingStop,OrderTakeProfit(),0,Red);
                     return(0);
                    }
                 }
              }
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+

