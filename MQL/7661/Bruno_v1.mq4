//+------------------------------------------------------------------+
//|                                Bruno's.mq4                       |
//+------------------------------------------------------------------+
/*  as defined by BrunoFX??

BrunoFX Expert:

Intervals          - 30 minutes 
Indicators         - Moving Average  Exponential – Période: 8 (red line)
                   - Moving Average  Exponential – Période: 21 (blue line)
                   - Moving Average  Simple – Période: 4  (OliveDrab line)
                   - ADX ( Average Directionnal Mouvement Index) – Période: 13 – ADX color: green, +DI color: blue, -DI color: red
                   - Stochastic Oscillator - période %K: 21 - période %D: 3 – Slowing: 3 – Price field: Low/High – Méthode: exponential – Color: green
                   - MACD – Fast EMA: 13 –Slow EMA: 34 - MACD SMA: 8 - blue line
                   - Parabolic SAR – Step: 0.055 – Maximum: 0.2100 – Color: Deepink

Rules of entry:
1st condition      - Crossing of 2 moving average (with the fall or the rise)
2ème condition     - Reading ADX (green line) > 20 and going up and +DI > -DI when crossing of moving average to the rise or +DI < -DI when crossing moving average with the fall.
3ème condition     - When crossing of moving average to the rise: Stochastic %K (green line) >%D (dotted red line) and value of  %D < 80.
                   - When crossing of moving average to the fall: Stochastic %K (ligne verte) <%D (dotted red line) and Value %D value > 20.
4ème condition     - For a crossing of moving average to the rise: value MACD (bar histogram blue) > 0 et > line of signal MACD (dotted red line).
                   - For a crossing of moving average to the fall: value MACD (bar histogram blue) < 0 et < line of signal MACD (dotted red line).
5ème condition     - Parabolic SAR must point in the same direction as the crossing of the moving average

Rules of exit:
Crossing of the moving average 4 (OliveDrab line) on moving average  8 (red line)  : close the position
-  Stops LOSS to 30 pip  and  trailing stop to 20 points
*/
 extern double TakeProfit=50;
 extern double Lots=0.1;
 extern double InitialStop=30;
 extern double TrailingStop=20;
//----
datetime BarTime;
bool Signal1L;
bool Signal2L;
bool Signal3L;
bool Signal4L;
bool Signal5L;
bool Signal1S;
bool Signal2S;
bool Signal3S;
bool Signal4S;
bool Signal5S;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {
//----
   Signal1L=false;
   Signal2L=false;
   Signal3L=false;
   Signal4L=false;
   Signal5L=false;
   //
   Signal1S=false;
   Signal2S=false;
   Signal3S=false;
   Signal4S=false;
   Signal5S=false;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   int cnt,total,ticket,MinDist,tmp;
   double Spread;
   //----
     if(Bars<100)
     {
      Print("bars less than 100");
      return(0);
     }
   //exit if not new bar
   if(BarTime==Time[0]) {return(0);}
   //new bar, update bartime
   BarTime=Time[0];
   //--------------- Miscellaneous setup stuff -------------------------
   MinDist=MarketInfo(Symbol(),MODE_STOPLEVEL);
   Spread=(Ask-Bid);
   // use an indicator for data values
   double SARP1= iSAR(NULL,0,0.055,0.21,1);
   double SARP2 =iSAR(NULL,0,0.055,0.21,2);
   double DMIPP1=iADX(NULL,0,13,PRICE_CLOSE,MODE_PLUSDI,1);
   double DMIPP2=iADX(NULL,0,13,PRICE_CLOSE,MODE_PLUSDI,2);
   double DMIMP1=iADX(NULL,0,13,PRICE_CLOSE,MODE_MINUSDI,1);
   double DMIMP2=iADX(NULL,0,13,PRICE_CLOSE,MODE_MINUSDI,2);
   double StochMP1=iStochastic(NULL,0,21,3,3,MODE_EMA,0,MODE_MAIN,1);
   double StochMP2=iStochastic(NULL,0,21,3,3,MODE_EMA,0,MODE_MAIN,2);
   double StochSP1=iStochastic(NULL,0,21,3,3,MODE_EMA,0,MODE_SIGNAL,1);
   double StochSP2=iStochastic(NULL,0,21,3,3,MODE_EMA,0,MODE_SIGNAL,2);
   double MACDHP1=iMACD(NULL,0,13,34,8,PRICE_CLOSE,MODE_MAIN,1);
   double MACDHP2=iMACD(NULL,0,13,34,8,PRICE_CLOSE,MODE_MAIN,2);
   double MACDSP1=iMACD(NULL,0,13,34,8,PRICE_CLOSE,MODE_SIGNAL,1);
   double MACDSP2=iMACD(NULL,0,13,34,8,PRICE_CLOSE,MODE_SIGNAL,2);
   double MA1P1=iMA(NULL,0,4,0,PRICE_CLOSE,MODE_SMA,1);
   double MA1P2=iMA(NULL,0,4,0,PRICE_CLOSE,MODE_SMA,1);
   double MA2P1=iMA(NULL,0,8,0,PRICE_CLOSE,MODE_EMA,1);
   double MA2P2=iMA(NULL,0,8,0,PRICE_CLOSE,MODE_EMA,1);
   double MA3P1=iMA(NULL,0,21,0,PRICE_CLOSE,MODE_EMA,1);
   double MA3P2=iMA(NULL,0,21,0,PRICE_CLOSE,MODE_EMA,1);
   //Condition 2:
   if (DMIPP1 > DMIMP1 && DMIPP1 > 20) Signal2L=true;
   else Signal2L=false;
   //Condition 3:
   if (MA2P1 >MA3P1 && StochMP1 > StochSP1 && StochMP1 < 80) Signal3L=true;
   else Signal3L=false;
   //Condition 4:
   if (MACDHP1 > 0 && MACDHP1 > MACDSP1) Signal4L=true;
   else Signal4L=false;
   //Condition 5:
   if (MA2P1 > MA3P1 && SARP1 > SARP2) Signal5L=true;
   else Signal5L=false;
   // -------------------- ORDER CLOSURE ----------------------------------
   // If Orders are in force then check for closure against Technicals LONG & SHORT
   // CLOSE LONG Entries
   total=OrdersTotal();
   if(total>0)
     {
      for(cnt=0;cnt<total;cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
           {
            if(Close[1] < SARP1)
              {
               OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); // close LONG position
              }
           }
         //CLOSE SHORT ENTRIES: 
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderType()==OP_SELL && OrderSymbol()==Symbol())       // check for symbol
           {
            if(Close[1] > Close[10])
              {
               OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); // close SHORT position
              }
           }
        }  // for loop return
     }   // close 1st if 
   // -------------------- ORDER TRAILING STOP Adjustment -------------------
   // TRAILING STOP: LONG
   if(0==1)  //This is used to turn the trailing stop on & off
     {
      total=OrdersTotal();
      if(total>0)
        {
         for(cnt=0;cnt<total;cnt++)
           {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_BUY && OrderSymbol()==Symbol()
            &&
            Bid-OrderOpenPrice()> (Point*TrailingStop)
            &&
            OrderStopLoss()<Bid-(Point*TrailingStop)
            )
              {OrderModify(OrderTicket(),OrderOpenPrice(),Bid-Point*TrailingStop,OrderTakeProfit(),0,White);
              return(0);}
           }
        }
      //TRAILING STOP: SHORT
      total=OrdersTotal();
      if(total>0)
        {
         for(cnt=0;cnt<total;cnt++)
           {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderType()==OP_SELL && OrderSymbol()==Symbol()
            &&
            OrderOpenPrice()-Ask > (Point*TrailingStop)
            &&
            OrderStopLoss() > Ask+(Point*TrailingStop)
            )
              {OrderModify(OrderTicket(),OrderOpenPrice(),Ask+(Point*TrailingStop),OrderTakeProfit(),0,Yellow);
              return(0);}
           }}
     }  // end bracket for on/off switch
   // --------------------------- END OF ORDER Closure routines & Stoploss changes -------------
   // --------------------------- NEW POSITIONS ? ----------------------------------------------
   // Possibly add in timer to stop multiple entries within Period
   // Check Margin available
   // ONLY ONE ORDER per SYMBOL
   // Loop around orders to check symbol doesn't appear more than once
   // Check for elapsed time from last entry to stop multiple entries on same bar
   if (0==1) // switch to turn ON/OFF history check
     {
      total=HistoryTotal();
      if(total>0)
        {
         for(cnt=0;cnt<total;cnt++)
           {
            OrderSelect(cnt, SELECT_BY_POS, MODE_HISTORY);            //Needs to be next day not as below
            if(OrderSymbol()==Symbol()&& CurTime()- OrderCloseTime() < (Period() * 60 )
               )
              {
               return(0);
              }
           }
        }
     }
   //-----------------
   total=OrdersTotal();
   if(total>0)
     {
      for(cnt=0;cnt<total;cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderSymbol()==Symbol()) return(0);
        }
     }
   //-----------------
   if(AccountFreeMargin()<(1000*Lots))
     {Print("We have no money. Free Margin = ", AccountFreeMargin());
     return(0);}
   //ENTRY RULES: LONG 
   if(Signal2L==true &&
      Signal3L==true &&
      Signal4L==true &&
      Signal5L==true
        )
     {
      ticket=OrderSend(Symbol(),OP_BUY,Lots,Ask,3,Bid-(InitialStop*Point),0,"MaxMin Long",16384,0,Orange); //Bid-(Point*(MinDist+2))
      if(ticket>0)
        {
         if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("BUY order opened : ",OrderOpenPrice());
        }
      else Print("Error opening BUY order : ",GetLastError());
      return(0);
     }
   //ENTRY RULES: SHORT
   if(Close[1] < Close[20]&& 0==1)
     {
      ticket=OrderSend(Symbol(),OP_SELL,Lots,Bid,3,Ask+(InitialStop*Point),0,"MaxMin Short",16384,0,Red);
      if(ticket>0)
        {
         if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("SELL order opened : ",OrderOpenPrice());
        }
      else Print("Error opening SELL order : ",GetLastError());
      return(0);
     }
   //---------------------- End of PROGRAM -------------------- 
   return(0);
  }
//+------------------------------------------------------------------+