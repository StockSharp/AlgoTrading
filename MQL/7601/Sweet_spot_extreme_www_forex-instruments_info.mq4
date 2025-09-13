//+-----------------------------------------------------------------------------------------------------------+
//|                                                                                    Sweet_Spot_Extreme.mq4 |
//|Copyright © 2006, Safari Traders, http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/ |
//+-----------------------------------------------------------------------------------------------------------+
#property copyright "Copyright © 2006, Safari Traders , chopped and optimized by transport_david "
#property link      "http://finance.groups.yahoo.com/group/MetaTrader_Experts_and_Indicators/"
//----
extern int    MAGIC_NUMBER     =613;
extern int    MaxTradesPerSymbol=   3;
extern int    Slippage       =2;
extern double Lots           =1.00;
extern double MaximumRisk    =0.05;
extern double DecreaseFactor =6;
extern double Stop            =10;
extern double MAPeriod        =85;
extern double CloseMa         =70;
extern double MinLot         =0.1;
extern double buyccilevel       =-200.00;
extern double sellccilevel     =200.00;
extern int price             =0;
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
  int start()
  {
   double Alpha=iCCI(NULL, 30, 12, price, 0);  // links to 30 minutes chart
   double MA=iMA(NULL,15,MAPeriod,0,MODE_EMA,PRICE_MEDIAN,1); // Links to 15 minutes chart for shorttearm accuracy
   double MAprevious=iMA(NULL,15,MAPeriod,0,MODE_EMA,PRICE_MEDIAN,2); // Links to 15 minutes chart for shorttearm accuracy
   double MAClose0=iMA(NULL,15,CloseMa,0,MODE_EMA,PRICE_MEDIAN,0); // Links to 15 minutes chart for shorttearm accuracy
   double MAClose1=iMA(NULL,15,CloseMa,0,MODE_EMA,PRICE_MEDIAN,1); // Links to 15 minutes chart for shorttearm accuracy
//----
   int trades=0;
     for(int cnt1=0; cnt1 < OrdersTotal(); cnt1++)
     {
      if(!OrderSelect(cnt1, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderMagicNumber()!=MAGIC_NUMBER) continue;
        if(StringFind(OrderSymbol(), Symbol(), 0)!=-1) 
        {
         //debugging
         Print("symbol "+Symbol());
         trades++;
         if(cnt1==OrdersTotal()-1) Print("Trades: "+trades);
        }
     }
   // no opened orders identified
     if(AccountFreeMargin()<(100*Lots))
     {
      Print("We have no money. Free Margin = ", AccountFreeMargin());
      return(0);
     }
     if(trades < MaxTradesPerSymbol)
     {
      // no opened orders identified
        if(AccountFreeMargin()<(100*Lots))
        {
         Print("We have no money. Free Margin = ", AccountFreeMargin());
         return(0);
        }
      int ticket=-10;
      // Bull trending
      if((MA>MAprevious) && (MAClose0 > MAClose1)&&(Alpha < buyccilevel))
         ticket=OrderSend(Symbol(),OP_BUY,LotsOptimized(),Ask, Slippage,0,0,"Sweet_Spot_Extreme", MAGIC_NUMBER,0,Aqua);
      // Bear trending
      if((MA<MAprevious) && (MAClose0 < MAClose1) && (Alpha > sellccilevel) )
         ticket=OrderSend(Symbol(),OP_SELL,LotsOptimized(),Bid, Slippage, 0,0,"Sweet_Spot_Extreme", MAGIC_NUMBER,0,Coral);
     }
   // EXIT POSITIONS
     for(int cnt=0; cnt<OrdersTotal(); cnt++)
     {
      if(!OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES)) continue;
      if(OrderMagicNumber()!=MAGIC_NUMBER) continue;
      if(StringFind(OrderSymbol(), Symbol(), 0)==-1) continue;
      if(OrderType()<=OP_SELL)
        {   // opened position_Sell??
         if(OrderType()==OP_BUY)
           { // position is opened_buy??   
            // early take profit giving this strategy a scalping capability?
            if((MA<=MAprevious)&&(OrderMagicNumber()==MAGIC_NUMBER))
              {
               OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(), 0,Aqua); // close position
               //return(0);
              }
            // check for stop
            if((Stop > 0)&&(OrderMagicNumber()==MAGIC_NUMBER))
              {
               if((OrderClosePrice()-OrderOpenPrice())>Stop*Point)
                 {
                  OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(), 0,Aqua); // close position
                  //return(0);
                 }
              }
           }
         else
           { // OrderType() == OP_SELL
            if(OrderType()==OP_SELL)
              {
               // should it be closed?
               if((MA>=MAprevious)&&(OrderMagicNumber()==MAGIC_NUMBER))
                 {
                  OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(), 0, Coral); // close position
                  //return(0); // exit
                 }
               // check for stop
               if((Stop > 0)&&(OrderMagicNumber()==MAGIC_NUMBER))
                 {
                  if((OrderOpenPrice()-OrderClosePrice())>Stop*Point)
                    {
                     OrderClose(OrderTicket(),OrderLots(),OrderClosePrice(), 0, Coral); // close position
                     //return(0);
                    }
                 }
              }
           }
        } //close -->  if(OrderType() <= OP_SELL){ 
     } //close -->  if(OrderType() == OP_BUY){
   //Comment("");   
   return(0);
  }
// Calculate optimal lot size                                       
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double LotsOptimized()
  {
   double lot=Lots;
   int    orders=HistoryTotal();     // history orders total
   int    losses=0;                  // number of losses orders without a break
//---- select lot size
   lot=NormalizeDouble(AccountFreeMargin()*MaximumRisk/500, 1);
//---- calcuulate number of losses orders without a break
   if(DecreaseFactor>0)
     {
      for(int i=orders-1;i>=0;i--)
        {
         if(OrderSelect(i,SELECT_BY_POS,MODE_HISTORY)==false) { Print("Error in history!"); break; }
         if(StringFind(OrderSymbol(), Symbol(), 0)==-1 || OrderType()>OP_SELL) continue;
         if(OrderMagicNumber()!=MAGIC_NUMBER) continue;
         if(OrderProfit()>0) break;
         if(OrderProfit()<0) losses++;
        }
      if(losses>1) lot=NormalizeDouble(lot-lot*losses/DecreaseFactor,1);
     }
//---- return lot size
   if(lot>50) lot=50;
   if(lot < MinLot) lot=MinLot;
   return(lot);
  }
//+------------------------------------------------------------------+