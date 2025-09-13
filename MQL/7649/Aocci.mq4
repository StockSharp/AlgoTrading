//+------------------------------------------------------------------+
//|                                       AOCCI                      |
//|                                                                  |
//+------------------------------------------------------------------+
extern int MinWidth=20;
extern int StopLoss=40;
extern int TrailingStop=0;
extern int ProfitTarget=50;
extern int Slippage=3;
//----
extern double FastMAPeriod=8;
extern double SlowMAPeriod= 17;
extern double SignalMAPeriod=9;
extern double chokevar=0.9;
extern double BIG_JUMP=1000; //30.0;       // Check for too-big candlesticks (avoid them)
extern double DOUBLE_JUMP=1000; //55.0;
extern int SignalCandle=0;                 //set to 1 if you want to get the cangle close 0 for current
//----
  double ao ;
  double ao1;
  double cci;
  double cci1;
//----
double   PrevPrice=0, PrevHigh=0, PrevLow=0, Pivot=0, Price=0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   ao  =iAO (NULL,0,0);
   ao1 =iAO (NULL,0,1);
//----
   cci =iCCI(NULL,0,55,PRICE_CLOSE,SignalCandle+0);
   cci1=iCCI(NULL,0,55,PRICE_CLOSE,SignalCandle+1);
//----
   PrevPrice=iClose(NULL,PERIOD_D1,SignalCandle+1);
   PrevHigh =iHigh(NULL,PERIOD_D1,SignalCandle+1);
   PrevLow  =iLow(NULL,PERIOD_D1,SignalCandle+1);
   Pivot=(PrevHigh + PrevLow + PrevPrice)/3;
   Price=iClose(NULL,PERIOD_H1,1); //gets close of last closed candle
   // Was there a sudden jump?  Ignore it...
     if((MathAbs(Open[1]-Open[0])/Point)>=BIG_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[2]-Open[1])/Point)>=BIG_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[3]-Open[2])/Point)>=BIG_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[4]-Open[3])/Point)>=BIG_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[5]-Open[4])/Point)>=BIG_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[2]-Open[0])/Point)>=DOUBLE_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[3]-Open[1])/Point)>=DOUBLE_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[4]-Open[2])/Point)>=DOUBLE_JUMP) 
     {
      return(0);
     }
     if((MathAbs(Open[5]-Open[3])/Point)>=DOUBLE_JUMP) 
     {
      return(0);
     }
   int NumTrades=0;
//----
   for(int i=0; i < OrdersTotal(); i++)
     {
      OrderSelect(i, SELECT_BY_POS);
      if (OrderSymbol()==Symbol())
        {
         if (OrderType()==OP_BUY )
           {
            if(Price >9999)       //never gonna happen 
               OrderClose(OrderTicket(), 1, Bid, Slippage);        return(0);
           }
         if (OrderType()==OP_SELL )
           {
            if(Price>9999)      //never gonna happen
               OrderClose(OrderTicket(), 1, Ask, Slippage);
           }
         NumTrades++;
        }
     }
   if (NumTrades==0)
     {
      if(ao>0 && cci>=0 && Ask>Pivot && (ao1<0 || cci1<=0 || Price<Pivot))
        {
         OrderSend(Symbol(), OP_BUY, 1, Ask, 2, Ask - StopLoss * Point, Ask + ProfitTarget * Point, 0);
         return(0);
        }
      if(ao>0 && cci>=0 && Ask>Pivot && (ao1<0 || cci1<=0 || Price<Pivot))
        {
         OrderSend(Symbol(), OP_SELL, 1, Bid, 2, Bid + StopLoss * Point, Bid - ProfitTarget * Point, 0);
         return(0);
        }
     }
   if (TrailingStop > 0)
     {
      for(i=0; i < OrdersTotal(); i++)
        {
         OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
         if ((OrderSymbol()==Symbol()) && (OrderType()==OP_BUY))
           {
            if (Ask - OrderOpenPrice() > TrailingStop * Point)
              {
               if (OrderStopLoss() < Ask - TrailingStop * Point)
                 {
                  OrderModify(OrderTicket(), OrderOpenPrice(), Ask - TrailingStop * Point, Ask + ProfitTarget * Point, 0);
                  return(0);
                 }
              }
           }
         if ((OrderSymbol()==Symbol()) && (OrderType()==OP_SELL))
           {
            if (OrderOpenPrice() - Bid > TrailingStop * Point)
              {
               if (OrderStopLoss() > Bid + TrailingStop * Point)
                 {
                  OrderModify(OrderTicket(), OrderOpenPrice(), Bid + TrailingStop * Point, Bid - ProfitTarget * Point, 0);
                  return(0);
                 }
              }
           }
        }
     }
//----
   return(0);
  }
//+------------------------------------------------------------------+