//+------------------------------------------------------------------+
//|                                                 ExpMartin_v2.mq4 |
//|                        Copyright 2013, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, MetaQuotes Software Corp."
#property link      "http://www.metaquotes.net"
//----
extern double       Lots =0.1;  //start lot
extern double     Factor =2.0;  //the multiplier of the lot
extern int         Limit =5;    //limiting the number of multiplications lot
extern int      StopLoss =100;  //level limit losses
extern int    TakeProfit =100;  //the level of profit taking
extern int     StartType =0;    //Type of the starting order, 0-BUY, 1-SELL
extern int         Magic =1000; //individually number of expert
//----
double lots_step;
//----
int ticket_buy;
int ticket_sell;
int lots_digits;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   ticket_buy=-1;
   ticket_sell=-1;
//----
   lots_step=MarketInfo(Symbol(),MODE_LOTSTEP);
//----
   if(lots_step==0.01)
      lots_digits=2;
//----
   if(lots_step==0.1)
      lots_digits=1;
//----
   if(lots_step==1.0)
      lots_digits=0;
//----
   for(int pos=OrdersTotal()-1;pos>=0;pos--)
      if(OrderSelect(pos,SELECT_BY_POS)==true)
         if(OrderMagicNumber()==Magic)
            if(OrderSymbol()==Symbol())
              {
               if(OrderType()==OP_BUY)
                 {
                  ticket_buy=OrderTicket();
                  break;
                 }
               //----
               if(OrderType()==OP_SELL)
                 {
                  ticket_sell=OrderTicket();
                  break;
                 }
              }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {return(0);}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
int start()
  {
   double price;
   double lots;
   double lots_test;
//----
   int slip;
   int ticket;
   int pos;
//----
//----open start BUY
   if(StartType==0)
      if(ticket_buy<0)
         if(ticket_sell<0)
           {
            ticket=OpenBuy(Lots);
            //----
            if(ticket>0)
               ticket_buy=ticket;
           }
//----
//----open the next order BUY with a positive profit order BUY
   if(ticket_buy>0)
      if(ticket_sell<0)
         if(OrderSelect(ticket_buy,SELECT_BY_TICKET)==true)
            if(OrderCloseTime()>0)
               if(OrderProfit()>0.0)
                 {
                  ticket=OpenBuy(Lots);
                  //----
                  if(ticket>0)
                     ticket_buy=ticket;
                 }
//----
//----open the next order at a negative profit SELL order BUY
   if(ticket_buy>0)
      if(ticket_sell<0)
         if(OrderSelect(ticket_buy,SELECT_BY_TICKET)==true)
            if(OrderCloseTime()>0)
               if(OrderProfit()<0.0)
                 {
                  lots=NormalizeDouble(MathCeil((OrderLots()*Factor)/lots_step)*lots_step,lots_digits);
                  lots_test=Lots;
                  //----
                  for(pos=0;pos<Limit;pos++)
                     lots_test=NormalizeDouble(MathCeil((lots_test*Factor)/lots_step)*lots_step,lots_digits);
                  //----
                  if(lots_test<lots)
                     lots=Lots;
                  //----
                  ticket=OpenSell(lots);
                  //----
                  if(ticket>0)
                    {
                     ticket_sell=ticket;
                     ticket_buy=-1;
                    }
                 }
//----
//----close BUY
   if(ticket_buy>0)
      if(OrderSelect(ticket_buy,SELECT_BY_TICKET)==true)
         if(OrderCloseTime()==0)
            if(OrderOpenPrice()+TakeProfit*Point<=MarketInfo(Symbol(),MODE_BID))
              {
               price=MarketInfo(Symbol(),MODE_BID);
               slip=MarketInfo(Symbol(),MODE_SPREAD)*2;
               return(OrderClose(ticket_buy,OrderLots(),price,slip,Blue));
              }
//----
   if(ticket_buy>0)
      if(OrderSelect(ticket_buy,SELECT_BY_TICKET)==true)
         if(OrderOpenPrice()-StopLoss*Point>=MarketInfo(Symbol(),MODE_BID))
            if(OrderCloseTime()==0)
              {
               price=MarketInfo(Symbol(),MODE_BID);
               slip=MarketInfo(Symbol(),MODE_SPREAD)*2;
               return(OrderClose(ticket_buy,OrderLots(),price,slip,Blue));
              }
//----
//----open start SELL
   if(StartType==1)
      if(ticket_buy<0)
         if(ticket_sell<0)
           {
            ticket=OpenSell(Lots);
            //----
            if(ticket>0)
               ticket_sell=ticket;
           }
//----
//----open the next order at the positive profit SELL SELL order
   if(ticket_buy<0)
      if(ticket_sell>0)
         if(OrderSelect(ticket_sell,SELECT_BY_TICKET)==true)
            if(OrderCloseTime()>0)
               if(OrderProfit()>0.0)
                 {
                  ticket=OpenSell(Lots);
                  //----
                  if(ticket>0)
                     ticket_sell=ticket;
                 }
//----
//----open the next order BUY with a negative profit orders SELL
   if(ticket_buy<0)
      if(ticket_sell>0)
         if(OrderSelect(ticket_sell,SELECT_BY_TICKET)==true)
            if(OrderCloseTime()>0)
               if(OrderProfit()<0.0)
                 {
                  lots=NormalizeDouble(MathCeil((OrderLots()*Factor)/lots_step)*lots_step,lots_digits);
                  lots_test=Lots;
                  //----
                  for(pos=0;pos<Limit;pos++)
                     lots_test=NormalizeDouble(MathCeil((lots_test*Factor)/lots_step)*lots_step,lots_digits);
                  //----
                  if(lots_test<lots)
                     lots=Lots;
                  //----
                  ticket=OpenBuy(lots);
                  //----
                  if(ticket>0)
                    {
                     ticket_buy=ticket;
                     ticket_sell=-1;
                    }
                 }
//----
//----close SELL
   if(ticket_sell>0)
      if(OrderSelect(ticket_sell,SELECT_BY_TICKET)==true)
         if(OrderCloseTime()==0)
            if(OrderOpenPrice()-TakeProfit*Point>=MarketInfo(Symbol(),MODE_ASK))
              {
               price=MarketInfo(Symbol(),MODE_ASK);
               slip=MarketInfo(Symbol(),MODE_SPREAD)*2;
               return(OrderClose(ticket_sell,OrderLots(),price,slip,Red));
              }
//----
   if(ticket_sell>0)
      if(OrderSelect(ticket_sell,SELECT_BY_TICKET)==true)
         if(OrderCloseTime()==0)
            if(OrderOpenPrice()+StopLoss*Point<=MarketInfo(Symbol(),MODE_ASK))
              {
               price=MarketInfo(Symbol(),MODE_ASK);
               slip=MarketInfo(Symbol(),MODE_SPREAD)*2;
               return(OrderClose(ticket_sell,OrderLots(),price,slip,Red));
              }
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| open order BUY                                                   |
//+------------------------------------------------------------------+
int OpenBuy(double lots)
  {
   double price;
//----
   int slip;
//----
   price=MarketInfo(Symbol(),MODE_ASK);
   slip=MarketInfo(Symbol(),MODE_SPREAD)*2;
//----
   return(OrderSend(Symbol(),OP_BUY,lots,price,slip,0.0,0.0,"",Magic,0,Blue));
  }
//+------------------------------------------------------------------+
//| open order SELL                                                  |
//+------------------------------------------------------------------+
int OpenSell(double lots)
  {
   double price;
//----
   int slip;
//----
   price=MarketInfo(Symbol(),MODE_BID);
   slip=MarketInfo(Symbol(),MODE_SPREAD)*2;
//----
   return(OrderSend(Symbol(),OP_SELL,lots,price,slip,0.0,0.0,"",Magic,0,Red));
  }
//+------------------------------------------------------------------+
