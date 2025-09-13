//+------------------------------------------------------------------+
//|                                            GrailExpertMAV1.0.mq4 |
//|                                       Copyright © 2006, systrad5 |
//|                                                         17/09/06 |
//|               Feedback or comments welcome at systrad5@yahoo.com |
//+------------------------------------------------------------------+
//    You may get profitable experts in backtests if orders are 
//   filled and exited in the same bar. This is usually an error due 
//   to the limitations of backtesting on data with longer timeframes 
//   than the trading strategy.
//    Backtest results will always be unreliable if an order is 
//   filled and exited on the same bar unless the entry is on the 
//   open or the exit on the close. This is because it is impossible 
//   to tell the price action within the bar. A backtest will make an 
//   estimation of what happened during the bar. At times the 
//   estimation may result in a fill at a price that is estimated to 
//   occur before the exit but in realty occured after. This can 
//   result in fills at impossible prices, especially when the market 
//   moves quickly in one direction. Some strategies will 
//   inadvertently exploit these impossible prices to produce 
//   impossible results.
//    You can end up with an expert that appears to be extremely 
//   profitable in backtests but will lose a bundle in real trading 
//   (well I think it will), as the attached expert will. Try it on 
//   EURUSD 1H timeframe.
//    The only way to have reliable backtests off bar data is to 
//   either enter on the open or exit on the close. It is certain 
//   when these two points happened so the order of the price action 
//   will always be correct. It also makes it certain that the expert 
//   does not inadvertenlty open and close an order on the same bar 
//   (such as with a Stop Loss Exit).
//    As you can guess I wouldn't recommend this expert be used for 
//   real....

#define MAGICGRAIL  20050917
//---- input parameters
extern double TakeProfit = 20.0;
extern double StopLoss = 50.0;
extern double Lots = 0.1;
extern int    HLPeriod = 6;
extern int    MAPeriod = 14;
extern int    MASlope = 2;
extern int    TgtThreshold = 5;
//---- Globals & Buffers
double Threshold, HEntry = 0, LEntry = 0, PrevHigh, PrevLow, Spread;
double MA1,MA2;
int MADir;
datetime prevtime = 0; //Used to determine opening bar
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
void start()
  {
   int Dir;
   Threshold = TgtThreshold * Point;
//---- Calc Buffers
   if(prevtime == Time[0])
     {
       // IntraBar Tick
       // Determine if H[0] or L[0] over previous
       if(Low[0] < PrevLow)
         {
           LEntry = Low[0] + Threshold + (TakeProfit * Point);
           Spread = PrevHigh - Low[0];
           if (Spread < (2 * Threshold) + (TakeProfit * Point)) LEntry = 0;
         }
       //----
       if(High[0] > PrevHigh)
         {
           HEntry = High[0] - Threshold - (TakeProfit * Point);
           Spread = High[0] - PrevLow;
           if(Spread < (2 * Threshold) + (TakeProfit * Point)) 
               HEntry = 0;
         }
       // Check to see if price has broken entry barrier
       Dir = 0;
       //----
       if(Ask < HEntry && HEntry != 0 && MADir ==1) 
           Dir = 1;
       //----
       if(Bid > LEntry && LEntry != 0 && MADir ==-1) 
           Dir = -1;    
     }
   else
     {
       // Calc MA
       MA1 = iMA(NULL, 0, MAPeriod, 0, MODE_EMA, PRICE_TYPICAL, 1);
       MA2 = iMA(NULL, 0, MAPeriod, 0, MODE_EMA, PRICE_TYPICAL, 2);
       if(MA1 > MA2 + (MASlope*Point)) 
           MADir = 1;
       else 
         {
           if (MA1 < MA2 - (MASlope*Point)) 
               MADir = -1;
           else 
               MADir = 0;
         }
       // Find High & Low of previous x Bars
       PrevHigh = High[Highest(NULL, 0, MODE_HIGH, HLPeriod, 1)];
       PrevLow = Low[Lowest(NULL, 0, MODE_LOW, HLPeriod, 1)];
       if(HEntry + Threshold + (TakeProfit * Point) > PrevHigh) 
           HEntry = 0;      
       if(LEntry - Threshold - (TakeProfit * Point) < PrevLow) 
           LEntry = 0;
     }
   //---- calculate open orders by current symbol
   if(CalculateCurrentOrders(Symbol()) == 0) 
       CheckForOpen(Dir);
   else
     {
       HEntry = 0;
       LEntry = 0;
     }
   //----- reset time         
   prevtime = Time[0];
//----
  }
//+------------------------------------------------------------------+
//| Calculate open positions                                         |
//+------------------------------------------------------------------+
int CalculateCurrentOrders(string symbol)
  {
   int buys = 0, sells = 0;
//----
   for(int i = 0; i < OrdersTotal(); i++)
     {
       if(OrderSelect(i, SELECT_BY_POS, MODE_TRADES) == false) 
           break;
       if(OrderSymbol() == Symbol() && OrderMagicNumber() == MAGICGRAIL)
         {
           if(OrderType() == OP_BUY)  
               buys++;
           if(OrderType() == OP_SELL) 
               sells++;
         }
     }
//---- return orders volume
   if(buys > 0) 
       return(buys);
   else
       return(-sells);
  }
//+------------------------------------------------------------------+
//| Check for open order conditions                                  |
//+------------------------------------------------------------------+
void CheckForOpen(int Dir)
  {
   int ticket;

//---- sell conditions
   if(Dir == -1)
     {
       ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, 0, Bid + StopLoss*Point, 
                          Bid - TakeProfit*Point, "Grail 1.0: Sell", MAGICGRAIL, 0, Red);
       if(ticket < 1) 
           Print("Error opening SELL order : ", GetLastError()); 
       return;
     }
//---- buy conditions
   if(Dir == 1)
     {
       ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, 0, Ask - StopLoss*Point, 
                          Ask + TakeProfit*Point, "Grail 1.0: Buy", MAGICGRAIL, 0, Blue);
       if(ticket<1) Print("Error opening BUY order : ", GetLastError());
       return;
     }
//----
  }
//+------------------------------------------------------------------+