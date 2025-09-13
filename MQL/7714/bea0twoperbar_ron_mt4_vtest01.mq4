//+------------------------------------------------------------------+
//|                 TwoPerBar                                        |
//|                 Copyright © 1999-2007, MetaQuotes Software Corp. |
//|                 http://www.metaquotes.ru                         |
//+------------------------------------------------------------------+
#property copyright "Ron Thompson"
#property link      "http://www.lightpatch.com/forex"
//---- user input
extern double Lots=0.1;               // how many lots to trade at a time 
       int    Slippage=5;             // how many pips of slippage can you tolorate
extern double ProfitMade=19;          // how much money do you expect to make
extern double LotLimit=12.8;
//---- naming and numbering
int      MagicNumber =200605242205;   // allows multiple experts to trade on same account
string   TradeComment="TPB_00_";      // comment so multiple EAs can be seen in Account History
//---- Bar handling
datetime bartime=0;                   // used to determine when a bar has moved
int      bartick=0;                   // number of times bars have moved
//---- Trade control
bool TradeAllowed=true;               // used to manage trades
double mylotsi;                       // used to manage lots
int sotn;                             // sell order ticket number
int botn;                             // buy order ticket number
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
   mylotsi=Lots;
  }
// Called EACH TICK and each Bar[]
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int      cnt=0;
   bool     ort;
   // bar counting
   if(bartime!=Time[0])
     {
      bartime=Time[0];
      bartick++;
//----
      if(OrdersTotal()>0){ort=true;}else{ort=false;}
      // close everything
      while(OrdersTotal()>0)
        {
         if(OrderSelect(botn, SELECT_BY_TICKET)) OrderClose(botn,mylotsi,Bid,Slippage,White);
         if(OrderSelect(sotn, SELECT_BY_TICKET)) OrderClose(sotn,mylotsi,Ask,Slippage,Red);
        } //while
      // adjust lot size based on loss, with hard upper limit
      if(ort) {mylotsi=mylotsi*2.0;} else {mylotsi=Lots;}
      if(mylotsi>LotLimit) mylotsi=LotLimit;
      // place orders
      botn=OrderSend(Symbol(),OP_BUY, mylotsi,Ask,Slippage,0,0,TradeComment,MagicNumber,White);
      sotn=OrderSend(Symbol(),OP_SELL,mylotsi,Bid,Slippage,0,0,TradeComment,MagicNumber,Red);
     }
   OrderSelect(botn, SELECT_BY_TICKET);
   if(Bid-OrderOpenPrice()>=(ProfitMade*Point))OrderClose(OrderTicket(),mylotsi,Bid,Slippage,White);
   OrderSelect(sotn, SELECT_BY_TICKET);
   if(OrderOpenPrice()-Ask>=(ProfitMade*Point))OrderClose(OrderTicket(),mylotsi,Ask,Slippage,Red);
  } // start()
//+------------------------------------------------------------------+