//+------------------------------------------------------------------+
//|       Arthur Matteson's Trading Machine, version 1.5             |
//| EA written in MQL4 for MetaTrader4 on 9/13/2006                  |
//| Meant for EUR/USD on the hourly timeframe (H1)                   |
//+------------------------------------------------------------------+
//  These parameters give a 43.5% profit on "Every tick" for EUR/USD
//  from 7/13/2006 to 9/12/2006 (MT4 release 197, H1 timeframe):
//
//#define NUM_LOTS 1.0
//#define EMA_SPEED 11.0
//#define BIG_JUMP 30.0
//#define DOUBLE_JUMP 55.0
//#define STOP_LOSS 20.0
//#define EMERGENCY_LOSS 50.0
//#define TAKE_PROFIT 25.0
//#define SLOPE_SMALL 5.0
//#define SLOPE_LARGE 8.0
//#define MINUTES_BEGIN 25.0
//#define MINUTES_END 25.0
//#define SLIP_BEGIN 0.0
//#define SLIP_END 0.0
//#define MIN_VOLUME 0.0
//#define SLIPPAGE 3.0
//#define ADJUST 1.0

// Variables that can be changed outside the program code, even optimized
extern double NUM_LOTS=1.0;        // How many lots to deal with (may be less than one)
extern double EMA_SPEED=11.0;      // The period for the averager
extern double BIG_JUMP=30.0;       // Check for too-big candlesticks (avoid them)
extern double DOUBLE_JUMP=55.0;    // Check for pairs of big candlesticks
extern double STOP_LOSS=20.0;      // A smart stop-loss
extern double EMERGENCY_LOSS=50.0; // The trade's stop loss in case of program error
extern double TAKE_PROFIT=25.0;    // The trade's take profit
extern double SLOPE_SMALL=5.0;     // The minimum EMA slope to enter a trade
extern double SLOPE_LARGE=8.0;     // The maximum EMA slope to enter a trade
extern double MINUTES_BEGIN=25.0;  // Wait this long to determine candlestick lows/highs
extern double MINUTES_END=25.0;    // Wait this long to determine candlestick lows/highs
extern double SLIP_BEGIN=0.0;      // An allowance between the close and low/high price
extern double SLIP_END=0.0;        // An allowance between the close and low/high price
extern double MIN_VOLUME=0.0;      // If the previous volume is not above this, exit the trade
extern double SLIPPAGE=3.0;        // This is for the OrderSend command
extern double ADJUST=1.0;          // A strange but functional imaginary spread adjustment
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int i, ticket;
   double ema_old, ema_new, ema_slope;
   double begin_buy_chance, begin_sell_chance;
   double end_buy_chance, end_sell_chance;
   static double open_price;
//----
   begin_buy_chance=0; begin_sell_chance=0;
   end_buy_chance=0; end_sell_chance=0; ticket=0;
   // Find the existing order, if there is one
     if(OrdersTotal()>0) 
     {
        if(OrderType()==OP_BUY) 
        {
         // If ticket=+1, this means a long order is in progress
         ticket=1;
        }
        if(OrderType()==OP_SELL) 
        {
         // If ticket=-1, this means a short order is in progress
         ticket=-1;
        }
     }
   // Find the exponentially-weighted average, and its derivative
   ema_old=iMA(NULL, PERIOD_H1, EMA_SPEED, 0, MODE_EMA, PRICE_OPEN, 1);
   ema_new=iMA(NULL, PERIOD_H1, EMA_SPEED, 0, MODE_EMA, PRICE_OPEN, 0);
   ema_slope=(ema_new-ema_old)/Point;
   // Are conditions correct to go long?
     if(ema_slope>=SLOPE_SMALL) 
     {
        if(ema_slope<=SLOPE_LARGE) 
        {
           if((Minute()>MINUTES_BEGIN)&&(Close[0]<=Open[0])&&(Close[0]<=Low[0]+(SLIP_BEGIN*Point))) 
           {
            begin_buy_chance=1;
           }
        }
     }
   // Are conditions correct to go short?
     if(ema_slope<=-SLOPE_SMALL) 
     {
        if(ema_slope>=-SLOPE_LARGE) 
        {
           if((Minute()>MINUTES_BEGIN)&&(Close[0]>=Open[0])&&(Close[0]>=High[0]-(SLIP_BEGIN*Point))) 
           {
            begin_sell_chance=1;
           }
        }
     }
   // Was there a sudden jump?  Ignore it...
     if((MathAbs(Open[1]-Open[0])/Point)>=BIG_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[2]-Open[1])/Point)>=BIG_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[3]-Open[2])/Point)>=BIG_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[4]-Open[3])/Point)>=BIG_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[5]-Open[4])/Point)>=BIG_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[2]-Open[0])/Point)>=DOUBLE_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[3]-Open[1])/Point)>=DOUBLE_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[4]-Open[2])/Point)>=DOUBLE_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
     if((MathAbs(Open[5]-Open[3])/Point)>=DOUBLE_JUMP) 
     {
      begin_buy_chance=0;
      begin_sell_chance=0;
     }
   // Implement a stop-loss
     if(ticket>0) 
     {
        if(((Close[0]-open_price)/Point)<=-STOP_LOSS) 
        {
           if((Minute()>MINUTES_END)&&(Close[0]>=Open[0])&&(Close[0]>=High[0]-(SLIP_END*Point))) 
           {
            end_buy_chance=1;
           }
        }
     }
   // Implement a stop-loss
     if(ticket<0) 
     {
        if(((open_price-Close[0])/Point)<=-STOP_LOSS) 
        {
           if((Minute()>MINUTES_END)&&(Close[0]<=Open[0])&&(Close[0]<=Low[0]+(SLIP_END*Point))) 
           {
            end_sell_chance=1;
           }
        }
     }
   // Prevent duplicate orders
   if(ticket>0) begin_buy_chance=0;
   if(ticket<0) begin_sell_chance=0;
   // Was there no volume last time?  If so, end the trade...
     if(Volume[1]<=MIN_VOLUME) 
     {
      if(ticket>0) end_buy_chance=1;
      if(ticket<0) end_sell_chance=1;
     }
   // Was a long-exit signaled?
     if(end_buy_chance>=1) 
     {
      ticket=0;
      OrderClose(OrderTicket(), NUM_LOTS, Bid, SLIPPAGE, Teal);
     }
   // Was a short-exit signaled?
     if(end_sell_chance>=1)
     {
      ticket=0;
      OrderClose(OrderTicket(), NUM_LOTS, Ask, SLIPPAGE, BlueViolet);
     }
   // Was a long-enter signaled?
     if(begin_buy_chance>=1) 
     {
      ticket=OrderSend(Symbol(), OP_BUY, NUM_LOTS, Ask, SLIPPAGE, Ask-EMERGENCY_LOSS*Point, Ask+TAKE_PROFIT*Point, "Long", 16384, 0, Lime);
      OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES);
      open_price=Open[0]-(ADJUST*Point)+(Bid-Ask);
     }
   // Was a short-enter signaled?
     if(begin_sell_chance>=1) 
     {
      ticket=OrderSend(Symbol(), OP_SELL, NUM_LOTS, Bid, SLIPPAGE, Bid+EMERGENCY_LOSS*Point, Bid-TAKE_PROFIT*Point, "Short", 16384, 0, Red);
      OrderSelect(ticket, SELECT_BY_TICKET, MODE_TRADES);
      open_price=Open[0]+(ADJUST*Point)+(Ask-Bid);
     }
   return(0);
  }
//+------------------------------------------------------------------+