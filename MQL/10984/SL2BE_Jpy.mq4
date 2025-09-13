//+------------------------------------------------------------------+
//|                                                        SL2BE.mq4 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"

extern string EnterBEPips = "Please Enter profit pips to move SL to BE.";
extern double BE_Pips = 30;
extern string PairNAme= "Please Enter Name of JPY Pair";
extern string JPYPairName = "USDJPY";

//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
//----
   if(Symbol()=="USDJPY" || Symbol()=="EURJPY" || Symbol()=="GBPJPY" || Symbol() == JPYPairName)
   {
      int type;
      int ticket;
      int total = OrdersTotal();
      double openPrice, stopPrice;
   
      for(int i=total-1;i>=0;i--)
      {
         for(int j=i;j>=0;j--)
            if(OrderSelect(i, SELECT_BY_POS))
            {
               ticket = OrderTicket();
               type = OrderType();
               openPrice = OrderOpenPrice();
               stopPrice = OrderStopLoss();
               if(type == OP_SELL && stopPrice > openPrice && Ask<= (openPrice-BE_Pips*0.01) && OrderSymbol()==Symbol())
                  OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Red);
               else if(type == OP_BUY && stopPrice < openPrice && Bid>= (openPrice+BE_Pips*0.01)&& OrderSymbol()==Symbol() )
                  OrderModify(ticket,OrderOpenPrice(),OrderOpenPrice(),OrderTakeProfit(),0,Blue);
            }
      }
   }
//----
   return(0);
  }
//+------------------------------------------------------------------+