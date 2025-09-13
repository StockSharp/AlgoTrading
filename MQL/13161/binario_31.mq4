//+------------------------------------------------------------------+
//|                           Copyright 2005, Gordago Software Corp. |
//|                                          http://www.gordago.com/ |
//+------------------------------------------------------------------+
  
// I want to thank Michal Rutka, michal1@zonnet.nl, for helping me correct
// the mistakes that I made... Good Job!!

/*
   ECN compatibility by Andriy Moraru, www.earnfore.com, 2012
*/

#property copyright "Provided by sencho, coded by don_forex"
#property link      "http://www.gordago.com"
#property version "1.0"
#property strict
extern int TakeProfit = 850;
extern int TrailingStop = 850;
extern int PipDifference = 25;
extern double Lots = 0.1;
extern double MaximumRisk = 10;
extern bool ECN_Mode = false; // In ECN mode, SL and TP aren't applied on OrderSend() but are added later with OrderModify()

double Poin;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
{
   //Checking for unconvetional Point digits number
   if (Point == 0.00001) Poin = 0.0001; //5 digits
   else if (Point == 0.001) Poin = 0.01; //3 digits
   else Poin = Point; //Normal
	return(0);
}

int start(){
   int cnt, ticket;
   if(Bars<100){
      Print("bars less than 100");
      return(0);
   }
   if(TakeProfit<10){
      Print("TakeProfit less than 10");
      return(0);
   }
   string TradeSymbol = Symbol();
   double MA144H = MathRound(iMA(NULL,0,144,0,MODE_EMA,PRICE_HIGH,0)/Poin)*Poin;
   double MA144L = MathRound(iMA(NULL,0,144,0,MODE_EMA,PRICE_LOW,0)/Poin)*Poin;
   double Spread = Ask-Bid; // MarketInfo(TradeSymbol,MODE_SPREAD);
   double BuyPrice      = MA144H + Spread+PipDifference*Poin;
   double BuyStopLoss   = MA144L - Poin;
   double BuyTakeProfit = MA144H +(PipDifference+TakeProfit)*Poin;
   double SellPrice     = MA144L -(PipDifference)*Poin;
   double SellStopLoss  = MA144H + Spread+Poin;
   double SellTakeProfit= MA144L - Spread-(PipDifference+TakeProfit)*Poin;
   double lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/50000,1);
   double close=iClose(NULL,0,0);
   int total=OrdersTotal();
   
   bool need_long  = true;
   bool need_short = true;
   // First update existing orders.
   for(cnt=0;cnt<total;cnt++) {
      if(OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)){};
      if(OrderSymbol()==Symbol() && OrderMagicNumber() == 16384){
         if(OrderType() == OP_BUYSTOP){
            need_long = false;
            if (OrderStopLoss()!=BuyStopLoss){
               Print(BuyStopLoss, " ",OrderStopLoss());
               if(OrderModify(OrderTicket(),BuyPrice,BuyStopLoss,BuyTakeProfit,0,Green)){};
            }
         }
         if(OrderType() == OP_SELLSTOP){
            need_short = false;
            if (OrderStopLoss()!=SellStopLoss){
               Print(SellStopLoss, " ",OrderStopLoss());
               if(OrderModify(OrderTicket(),SellPrice,SellStopLoss,SellTakeProfit,0,Green)){};
            }
         }
         if(OrderType()==OP_BUY){
            need_long = false;
            if (OrderStopLoss()<BuyStopLoss){
               Print(BuyStopLoss, " ",OrderStopLoss());
               if(OrderModify(OrderTicket(),OrderOpenPrice(),BuyStopLoss,BuyTakeProfit,0,Green)){};
               if(OrderDelete(OrderTicket())){};
            }
            if(TrailingStop>0) {
               if(Bid-OrderOpenPrice()>Poin*TrailingStop) {
                  if(OrderStopLoss()<Bid-Poin*TrailingStop) {
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Poin*TrailingStop,OrderTakeProfit(),0,Green)){};
                     return(0);
                  }
               }
            }
         }
         if(OrderType()==OP_SELL){
            need_short = false;
            if (OrderStopLoss()>SellStopLoss){
               Print(SellStopLoss, " ",OrderStopLoss());
               if(OrderModify(OrderTicket(),OrderOpenPrice(),SellStopLoss,SellTakeProfit,0,Green)){};
               if(OrderDelete(OrderTicket())){};
            }
            if(TrailingStop>0) {
               if((OrderOpenPrice()-Ask)>(Poin*TrailingStop)) {
                  if((OrderStopLoss()>(Ask+Poin*TrailingStop)) || (OrderStopLoss()==0)) {
                     if(OrderModify(OrderTicket(),OrderOpenPrice(),Ask+Poin*TrailingStop,OrderTakeProfit(),0,Red)){};
                     return(0);
                  }
               }
            }
         }
      }
   }
      
   if(AccountFreeMargin()<(1000*lot)){
      Print("We have no money. Free Margin = ", AccountFreeMargin());
      return(0);
   }
       
   if (close<MA144H && close>MA144L){
         double SL = 0, TP = 0, ecnSL = 0, ecnTP = 0;
      if(need_long)
      {
         SL = 0; TP = 0; ecnSL = 0; ecnTP = 0;

	      if (!ECN_Mode)
	      {
            SL = BuyStopLoss;
            TP = BuyTakeProfit;
         }
         else
         {
            ecnSL = BuyStopLoss;
            ecnTP = BuyTakeProfit;
         }
         ticket=OrderSend(Symbol(),OP_BUYSTOP,lot,BuyPrice,3,SL,TP,"Binario_v3",16384,0,Green);
         if (ticket > 0)
         {
           if (ECN_Mode)
           {
              if(OrderSelect(ticket, SELECT_BY_TICKET)){};
              if(OrderModify(ticket, OrderOpenPrice(), ecnSL, ecnTP, 0)){};
           }
         }
         
      }
      if(need_short)
      {
         SL = 0; TP = 0; ecnSL = 0; ecnTP = 0;

	      if (!ECN_Mode)
	      {
            SL = SellStopLoss;
            TP = SellTakeProfit;
         }
         else
         {
            ecnSL = SellStopLoss;
            ecnTP = SellTakeProfit;
         }
         ticket=OrderSend(Symbol(),OP_SELLSTOP,lot,SellPrice,3,SL,TP,"Binario_v3",16384,0,Red);
         if (ticket > 0)
         {
           if (ECN_Mode)
           {
              if(OrderSelect(ticket, SELECT_BY_TICKET)){};
              if(OrderModify(ticket, OrderOpenPrice(), ecnSL, ecnTP, 0)){};
           }
         }
      }
   }   
	return(0);
}

