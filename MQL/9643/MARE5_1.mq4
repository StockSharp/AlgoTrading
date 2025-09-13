//+------------------------------------------------------------------+
//|                                                      MARE5.1.mq4 |
//|                                        Author: Kvant & Reshetov  |
//|                                                                  |
//+------------------------------------------------------------------+
extern double Lots         = 7.8;
extern double TakeProfit   = 110;
extern double TrailingStop = 10;
extern double StopLoss     = 80;
extern int    MAFastPeriod = 13; 
extern int    MASlowPeriod = 55;
extern double MovingShift  = 2;
extern double cnt          = 0;
extern double TimeOpen     = 08;
extern double TimeClose    = 14;
//+------------------------------------------------------------------+
//| Start function                                                   |
//+------------------------------------------------------------------+
int start()
  {
   double FastMa = 0;
   double FastMa2 = 0;
   double FastMa5 = 0;
   double SlowMa = 0;
   double SlowMa2 = 0;
   double SlowMa5 = 0;
   FastMa = iMA(NULL, 1, MAFastPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 0);
   FastMa2 = iMA(NULL, 1, MAFastPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 2);
   FastMa5 = iMA(NULL, 1, MAFastPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 5);
   SlowMa = iMA(NULL, 1, MASlowPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 0);
   SlowMa2 = iMA(NULL, 1, MASlowPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 2); 
   SlowMa5 = iMA(NULL, 1, MASlowPeriod, MovingShift, MODE_SMA, PRICE_CLOSE, 5);
   if(Hour() >= TimeOpen && Hour() <= TimeClose) 
     {
       for(cnt = 0; cnt < OrdersTotal(); cnt++) 
         { 
           if(OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES) == false)
               break; 
           //if(CurTime()-OrderOpenTime()<300) return(0); 
         }
       if(OrdersTotal() < 1)
       //---- sell conditions
       if((SlowMa - FastMa) >= Point && (FastMa2 - SlowMa2) >= Point && 
          (FastMa5 - SlowMa5) >= Point && Close[1] < Open[1])  
         {
           OrderSend(Symbol(), OP_SELL, Lots, Bid, 3, Bid + StopLoss*Point, 
                     Bid - TakeProfit*Point, 0, 0, Red);
           return;
         }
       //---- buy conditions
       if((FastMa - SlowMa) >= Point && (SlowMa2 - FastMa2) >= Point && 
          (SlowMa5 - FastMa5) >= Point && Close[1] > Open[1])  
         {
           OrderSend(Symbol(), OP_BUY, Lots, Ask, 3, Ask - StopLoss*Point, 
                     Ask + TakeProfit*Point, 0, 0, Blue);
           return;
         }
     }
  }
//+------------------------------------------------------------------+