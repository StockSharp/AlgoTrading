//+------------------------------------------------------------------+
//| mo_bidir_v0_1.mq4                                                |
//|                                                                  |
//| - Works best in 5M timeframe                                     |
//| - Bug fix to stop_loss in line 22 2010.04.07                     |
//+------------------------------------------------------------------+
#property copyright "Copyright © 2010 - Monu Ogbe"

#define MAGIC  1234
#define IDENT  "mo_bidir_v0_1"

extern double  lots           = 1;
extern double  stop_loss      = 80;   // (8 pips) optimise 50-2000
extern double  take_profit    = 750;  // (75 pips) optimise 50-2000

int            last_bar       = 0;

int start(){
   if (last_bar == Bars) return(0);
   last_bar = Bars;
   if (OrdersTotal() == 0){
         OrderSend(Symbol(), OP_BUY, lots ,Ask, 3, Ask - stop_loss * Point, Bid + take_profit * Point, IDENT, MAGIC, 0, Blue);
         OrderSend(Symbol(), OP_SELL, lots ,Bid, 3, Bid + stop_loss * Point, Ask - take_profit * Point, IDENT, MAGIC, 0, Red);
   } 
   return(0);
}

