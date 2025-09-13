//+------------------------------------------------------------------+
//|                                                    suffic369.mq4 |
//|                                                                  |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "suffic369@yahoo.com, treberk"
#property link      "http://sufx.core.t3-ism.net/ExpertAdvisorBuilder/"
//----
extern int MagicNumber=0;
extern bool EachTickMode=True;
extern double Lots=0.1;
extern int Slippage=3;
extern bool StopLossMode=True;
extern int StopLoss=30;
extern bool TakeProfitMode=True;
extern int TakeProfit=60;
extern bool TrailingStopMode=True;
extern int TrailingStop=30;
//----
#define SIGNAL_NONE 0
//----
int BarCount;
int Current;
bool TickCheck=False;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
  int init() 
  {
   BarCount=Bars;
   if (EachTickMode) Current=0; else Current=1;
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
  int deinit() 
  {
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
  int start() 
  {
   int Order=SIGNAL_NONE;
   int Total, Ticket;
   double StopLossLevel, TakeProfitLevel;
//----
   if (EachTickMode && Bars!=BarCount) EachTickMode=False;
   Total=OrdersTotal();
   Order=SIGNAL_NONE;
   //+------------------------------------------------------------------+
   //| Signal Begin                                                     |
   //+------------------------------------------------------------------+
   double Buy1_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 1);
   double Buy1_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_HIGH, Current + 1);
   double Buy2_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 0);
   double Buy2_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_HIGH, Current + 0);
   double Buy3_1=iClose(NULL, 0, Current + 0);
   double Buy3_2=iBands(NULL, 0, 156, 1, 0, PRICE_CLOSE, MODE_LOWER, Current + 0);
   double Sell1_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 1);
   double Sell1_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_LOW, Current + 1);
   double Sell2_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 0);
   double Sell2_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_LOW, Current + 0);
   double Sell3_1=iClose(NULL, 0, Current + 0);
   double Sell3_2=iBands(NULL, 0, 156, 1, 0, PRICE_CLOSE, MODE_UPPER, Current + 0);
   double CloseBuy1_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 1);
   double CloseBuy1_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_LOW, Current + 1);
   double CloseBuy2_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 0);
   double CloseBuy2_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_LOW, Current + 0);
   double CloseBuy3_1=iClose(NULL, 0, Current + 0);
   double CloseBuy3_2=iBands(NULL, 0, 156, 1, 0, PRICE_CLOSE, MODE_UPPER, Current + 0);
   double CloseSell1_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 1);
   double CloseSell1_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_HIGH, Current + 1);
   double CloseSell2_1=iMA(NULL, 0, 3, 0, MODE_SMA, PRICE_CLOSE, Current + 0);
   double CloseSell2_2=iMA(NULL, 0, 5, 0, MODE_SMA, PRICE_HIGH, Current + 0);
   double CloseSell3_1=iClose(NULL, 0, Current + 0);
   double CloseSell3_2=iBands(NULL, 0, 156, 1, 0, PRICE_CLOSE, MODE_LOWER, Current + 0);
   //+------------------------------------------------------------------+
   //| Signal End                                                       |
   //+------------------------------------------------------------------+
   //Buy
   if(Total < 1)
     {
      //Check free margin
      if (AccountFreeMargin() < (1000 * Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);
        }
      if ((Buy1_1 < Buy1_2 && Buy2_1 > Buy2_2 && Buy3_1 < Buy3_2) && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars!=BarCount))))
        {
         if (StopLossMode) StopLossLevel=Ask - StopLoss * Point;
         else StopLossLevel=0.0;
         if (TakeProfitMode) TakeProfitLevel=Ask + TakeProfit * Point; else TakeProfitLevel=0.0;
//----
         Ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,Slippage,StopLossLevel,TakeProfitLevel,"",MagicNumber,0,DodgerBlue);
           if(Ticket > 0) 
           {
            if (OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
               Print("BUY order opened : ", OrderOpenPrice()); else Print("Error opening BUY order : ", GetLastError());
           }
         if (EachTickMode) TickCheck=True;
         if (!EachTickMode) BarCount=Bars;
         return(0);
        }
     }
   //Sell
   if ((Sell1_1 > Sell1_2 && Sell2_1 < Sell2_2 && Sell3_1 > Sell3_2) && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars!=BarCount))))
     {
        if(Total < 1) 
        {
         //Check free margin
         if (AccountFreeMargin() < (1000 * Lots))
           {
            Print("We have no money. Free Margin = ", AccountFreeMargin());
            return(0);
           }
         if (StopLossMode) StopLossLevel=Bid + StopLoss * Point;
         else StopLossLevel=0.0;
         if (TakeProfitMode) TakeProfitLevel=Bid - TakeProfit * Point; else TakeProfitLevel=0.0;
//----
         Ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,Slippage,StopLossLevel,TakeProfitLevel,"", MagicNumber,0,DeepPink);
         if(Ticket > 0)
           {
            if (OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES))
               Print("SELL order opened : ", OrderOpenPrice());
            else Print("Error opening SELL order : ", GetLastError());
           }
         if (EachTickMode) TickCheck=True;
         if (!EachTickMode) BarCount=Bars;
         return(0);
        }
     }
   //Check position
     for(int i=0; i < Total; i ++) 
     {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      if(OrderType()<=OP_SELL &&  OrderSymbol()==Symbol())
        {
         if(OrderType()==OP_BUY)
           {
            //Close
            if ((CloseBuy1_1 > CloseBuy1_2 && CloseBuy2_1 < CloseBuy2_2 && CloseBuy3_1 > CloseBuy3_2) && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars!=BarCount))))
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,MediumSeaGreen);
               if (EachTickMode) TickCheck=True;
               if (!EachTickMode) BarCount=Bars;
               return(0);
              }
            //Trailing stop
            if(TrailingStopMode && TrailingStop > 0)
              {
                 if(Bid - OrderOpenPrice() > Point * TrailingStop) 
                 {
                    if(OrderStopLoss() < Bid - Point * TrailingStop) 
                    {
                     OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,MediumSeaGreen);
                     if (!EachTickMode) BarCount=Bars;
                     return(0);
                    }
                 }
              }
            }
             else 
            {
            //Close
            if ((CloseSell1_1 < CloseSell1_2 && CloseSell2_1 > CloseSell2_2 && CloseSell3_1 < CloseSell3_2) && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars!=BarCount))))
              {
               OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, DarkOrange);
               if (EachTickMode) TickCheck=True;
               if (!EachTickMode) BarCount=Bars;
               return(0);
              }
            //Trailing stop
            if(TrailingStopMode && TrailingStop > 0)
              {
               if((OrderOpenPrice() - Ask) > (Point * TrailingStop))
                 {
                    if((OrderStopLoss() > (Ask + Point * TrailingStop)) || (OrderStopLoss()==0)) 
                    {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Ask + Point * TrailingStop, OrderTakeProfit(), 0, DarkOrange);
                     if (!EachTickMode) BarCount=Bars;
                     return(0);
                    }
                 }
              }
           }
        }
     }
   if (!EachTickMode) BarCount=Bars;
//----
   return(0);
  }
//+------------------------------------------------------------------+