//+------------------------------------------------------------------+
//|                                                     Bago_EA.mq4  |
//|                                    Copyright @2006, Hua Ai (aha) |
//|                                                                  |
//+------------------------------------------------------------------+
#property copyright "Copyright @2006, Hua Ai (aha)"
#property link      ""

/*
Bago system can be categorized as a trend following system based on 
the cross of ema 5 and ema 12. When used properly on hourly chart it 
can capture daily swings of 100+ pips.

The use of small number emas gives Bago system the sensitivity to 
generate early signals following 10-20 minutes scale waves, but also 
produces a great deal of false signals that can quickly drain a trader
's account. So filters are extremely important for Bago system.

While Bago system is largely a discretionary system, the integration of 
two excellent filters may make it possible to use a computer program
generate signals with great high successful rate. This program is 
writtern to investigate this possiblity.

The mechanism to generate a raw Bago signal is simple: ema 5 crosses 
ema 12 in the same direction as RSI 21 crosses 50 level. To abstract 
real signals, we need to pay attention to context: where the price are,
and when the crosses happens.

The greatest meaning of integrating Vegas tunnel into Bago system is, 
the tunnel as well as its fibo lines changes the original plain 2-d
space into a twisted 2-d space. The twisted price trends now have the
coordinates. With this coordinates system we may see the entry and exit
with higher accuracy.

So, this program will first construct the simple rules upon which the 
the raw signals are generated, then will add rules to filter those 
signals. Those new rules are quantified as parameters so they can be 
easily changed and optimized based on the output results.

Enough talking, now come to business.
*/

//Adjustable parameters
/*
extern int     TrailingStop        =  30;
extern int     CrossEffectiveTime  =  2; 
extern int     TunnelBandWidth     =  0;
extern int     TunnelSafeZone      =  120;
extern int     HardStopLoss        =  30;
extern int     StopLossToFibo      =  20;

extern bool    LondonOpen          =  true;
extern bool    NewYorkOpen         =  true;
extern bool    TokyoOpen           =  true;
*/
extern int     FasterEMA         =5;
extern int     SlowerEMA         =12;
extern int     RSIPeriod         =21;
extern double  TotalLots         =3.0;
extern int     TPLevel1          =55;
extern double  TPLevel1Lots      =1.0;
extern int     TPLevel2          =89;
extern double  TPLevel2Lots      =1.0;
extern int     TPLevel3          =144;
//Parameters optimized for different pairs
int     TrailingStop      =30;
int     CrossEffectiveTime=2;
int     TunnelBandWidth   =0;
int     TunnelSafeZone    =120;
int     HardStopLoss      =30;
int     StopLossToFibo    =20;
//----
bool    LondonOpen        =true;
bool    NewYorkOpen       =true;
bool    TokyoOpen         =true;
// Indicator buffers to mark the signal
double         CrossUp[];
double         CrossDown[];
// State registers store cross up/down information 
bool           EMACrossedUp;
bool           EMACrossedDown;
bool           RSICrossedUp;
bool           RSICrossedDown;
bool           TunnelCrossedUp;
bool           TunnelCrossedDown;
// Cross up/down info should expire in a couple of bars.
// Timer registers to control the expiration.
int            EMACrossedUpTimer;
int            RSICrossedUpTimer;
int            EMACrossedDownTimer;
int            RSICrossedDownTimer;
//bool startup;
//int SignalLabeled; // 0: initial state; 1: up; 2: down.
//bool upalert,downalert;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init()
  {
   if (Symbol()=="EURUSD")
     {
      TrailingStop        =25;
      CrossEffectiveTime  =2;
      TunnelBandWidth     =5;
      TunnelSafeZone      =120;
      HardStopLoss        =30;
      StopLossToFibo      =50;
      LondonOpen          =true; // ok
      NewYorkOpen         =true; // the best
      TokyoOpen           =false;// disaster   
     }
   else if (Symbol()=="GBPUSD")
        {
         TrailingStop        =50;
         CrossEffectiveTime  =2;
         TunnelBandWidth     =8;
         TunnelSafeZone      =160;
         HardStopLoss        =30;
         StopLossToFibo      =15;
         LondonOpen          =true; // ok
         NewYorkOpen         =false;// so so
         TokyoOpen           =true; // the best
        }
      else if (Symbol()=="USDCHF")
           {
            TrailingStop        =70;
            CrossEffectiveTime  =2;
            TunnelBandWidth     =9;
            TunnelSafeZone      =160;
            HardStopLoss        =30;
            StopLossToFibo      =30;
            LondonOpen          =true; // Great
            NewYorkOpen         =true; // Ok
            TokyoOpen           =true; // so so   
           }
         else if (Symbol()=="AUDUSD")
              {
               TrailingStop        =60;
               CrossEffectiveTime  =2;
               TunnelBandWidth     =6;
               TunnelSafeZone      =70;
               HardStopLoss        =30;
               StopLossToFibo      =30;
               LondonOpen          =true; // Great
               NewYorkOpen         =true; // Ok
               TokyoOpen           =true; // so so
              }
   // No cross
   EMACrossedUp=false;
   RSICrossedUp=false;
   TunnelCrossedUp=false;
   EMACrossedDown=false;
   RSICrossedDown=false;
   TunnelCrossedDown=false;
   // Reset timers
   EMACrossedUpTimer=0;
   RSICrossedUpTimer=0;
   EMACrossedDownTimer=0;
   RSICrossedDownTimer=0;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit()
  {
//----
   EMACrossedUp=false;
   RSICrossedUp=false;
   TunnelCrossedUp=false;
   EMACrossedDown=false;
   RSICrossedDown=false;
   TunnelCrossedDown=false;
   //
   EMACrossedUpTimer=0;
   RSICrossedUpTimer=0;
   EMACrossedDownTimer=0;
   RSICrossedDownTimer=0;
//----
   return(0);
  }
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+
  int start() 
  {
   int i, cnt, total, ticket, orders;
   double fasterEMAnow, slowerEMAnow;
   double fasterEMAprevious, slowerEMAprevious;
   double RSInow, RSIprevious;
   double VegasTunnelFast, VegasTunnelSlow;
   double VegasPriceCrossedL, VegasPriceCrossedS;
   if (Minute()<2 && Seconds()<30)
     {
      fasterEMAnow     =iMA(NULL, 0, FasterEMA, 0, MODE_EMA, PRICE_CLOSE, 1);
      fasterEMAprevious=iMA(NULL, 0, FasterEMA, 0, MODE_EMA, PRICE_CLOSE, 2);
      //
      slowerEMAnow     =iMA(NULL, 0, SlowerEMA, 0, MODE_EMA, PRICE_CLOSE, 1);
      slowerEMAprevious=iMA(NULL, 0, SlowerEMA, 0, MODE_EMA, PRICE_CLOSE, 2);
      //
      RSInow           =iRSI(NULL,0,RSIPeriod,PRICE_CLOSE,1);
      RSIprevious      =iRSI(NULL,0,RSIPeriod,PRICE_CLOSE,2);
      //
      VegasTunnelFast  =iMA(NULL,0,144,0,1,PRICE_CLOSE,0);
      VegasTunnelSlow  =iMA(NULL,0,169,0,1,PRICE_CLOSE,0);
      // *********************************************************************
      // Based on the price and the calculated indicators determine the states
      // of the state machine.
      // *********************************************************************
      // Check if there is a RSI cross up
      if ((RSInow > 50) && (RSIprevious < 50) && (RSICrossedUp==false))
        {
         RSICrossedUp  =true;
         RSICrossedDown=false;
        }
      if (RSICrossedUp==true)
         RSICrossedUpTimer++;  // Record the number of bars the cross has happened
      else
         RSICrossedUpTimer=0;  // Reset the timer once the cross is reversed
      if (RSICrossedUpTimer>=CrossEffectiveTime)
         RSICrossedUp=false; // Reset state register when crossed 3 bars ago
      // Check if there is a RSI cross down
      if ((RSInow < 50) && (RSIprevious > 50) && (RSICrossedDown==false))
        {
         RSICrossedUp  =false;
         RSICrossedDown=true;
        }
      if (RSICrossedDown==true)
         RSICrossedDownTimer++;  // Record the number of bars the cross has happened
      else
         RSICrossedDownTimer=0;  // Reset the timer once the cross is reversed
      if (RSICrossedDownTimer>=CrossEffectiveTime)
         RSICrossedDown=false; // Reset register when crossed 3 bars ago
      // Check if there is a EMA cross up
      if((fasterEMAnow > slowerEMAnow) &&
           (fasterEMAprevious < slowerEMAprevious) &&
           (EMACrossedUp==false))
        {
         EMACrossedUp  =true;
         EMACrossedDown=false;
        }
      if (EMACrossedUp==true)
         EMACrossedUpTimer++;  // Record the number of bars the cross has happened
      else
         EMACrossedUpTimer=0;  // Reset the timer once the cross is reversed
      if (EMACrossedUpTimer>=CrossEffectiveTime)
         EMACrossedUp=false; // Reset register when crossed 3 bars ago
      // Check if there is a EMA cross down
      if((fasterEMAnow < slowerEMAnow) &&
           (fasterEMAprevious > slowerEMAprevious) &&
           (EMACrossedDown==false))
        {
         EMACrossedUp  =false;
         EMACrossedDown=true;
        }
      if (EMACrossedDown==true)
         EMACrossedDownTimer++;  // Record the number of bars the cross has happened
      else
         EMACrossedDownTimer=0;  // Reset the timer once the cross is reversed
      if (EMACrossedDownTimer>=CrossEffectiveTime)
         EMACrossedDown=false; // Reset register when crossed 3 bars ago
      if ((Close[1]>VegasTunnelFast && Close[1]>VegasTunnelSlow) &&
          (Close[2]<VegasTunnelFast || Close[2]<VegasTunnelSlow))
        {
         TunnelCrossedUp  =true;
         TunnelCrossedDown=false;
        }
      if ((Close[1]<VegasTunnelFast && Close[1]<VegasTunnelSlow) &&
          (Close[2]>VegasTunnelFast || Close[2]>VegasTunnelSlow))
        {
         TunnelCrossedUp  =false;
         TunnelCrossedDown=true;
        }
      // *********************************************************************
      // Based on states, determine exit situations.
      // *********************************************************************
      total=OrdersTotal();
//----
      for(cnt=0;cnt<total;cnt++)
        {
         OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
         if(OrderType()<=OP_SELL && OrderSymbol()==Symbol())
           {
            if(OrderType()==OP_BUY)
              {
               // Long Exits
               // When EMA or RSI crosses reverse, exit all lots
               if((EMACrossedDown==true) || (RSICrossedDown==true))
                 {
                  OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet);
                  continue;
                 }
               // Exits on fibo lines      
               if(TunnelCrossedUp==true)
                 {
                  if(Bid>=(VegasTunnelSlow+TPLevel3*Point))
                    {
                     if (OrderStopLoss()<Bid-TrailingStop*Point)
                        OrderModify(OrderTicket(),Ask,Bid-TrailingStop*Point,OrderTakeProfit(),0);
                     continue;
                    }
                  // Reach 2nd TP level, close the 2nd lot, move up remainder stop to TPLevel2-StopLossToFibo
                  // Or start trailing stop
                  else if(Bid>=(VegasTunnelSlow+TPLevel2*Point))
                       {
                        VegasPriceCrossedL=(MathRound(10000*VegasTunnelSlow)/10000);
                        if (OrderLots()>1 && OrderOpenPrice()<VegasTunnelSlow+(TPLevel2-12)*Point)
                           OrderClose(OrderTicket(),TPLevel2Lots,Bid,3,Violet);
                        //OrderModify(OrderTicket(),Ask,VegasPriceCrossedL+(TPLevel2-StopLossToFibo)*Point,OrderTakeProfit(),0);
                        if (OrderStopLoss()<Bid-TrailingStop*Point)
                           OrderModify(OrderTicket(),Ask,Bid-TrailingStop*Point,OrderTakeProfit(),0);
                        continue;
                       }
                  // Reach 1nd TP level, close the 1st lot, move up remainder stop to TPLevel1-StopLossToFibo
                     else if(Bid>=(VegasTunnelSlow+TPLevel1*Point))
                          {
                           VegasPriceCrossedL=(MathRound(10000*VegasTunnelSlow)/10000);
                           if (OrderLots()>2 && OrderOpenPrice()<VegasTunnelSlow+(TPLevel1-12)*Point)
                              OrderClose(OrderTicket(),TPLevel1Lots,Bid,3,Violet);
                           //OrderModify(OrderTicket(),Ask,VegasPriceCrossedL+(TPLevel1-StopLossToFibo)*Point,OrderTakeProfit(),0);
                           if (OrderStopLoss()<Bid-TrailingStop*Point)
                              OrderModify(OrderTicket(),Ask,Bid-TrailingStop*Point,OrderTakeProfit(),0);
                           continue;
                          }
                  //Tunnel crossed, move stop to below the tunnel; 
                  VegasPriceCrossedL=(MathRound(10000*VegasTunnelSlow)/10000);
                  if (OrderStopLoss()<VegasPriceCrossedL-(TunnelBandWidth+10)*Point)
                     OrderModify(OrderTicket(),Ask,VegasPriceCrossedL-(TunnelBandWidth+StopLossToFibo)*Point,OrderTakeProfit(),0);
                  continue;
                 }
               else
                 {
                  if(Bid>=(VegasTunnelSlow-TPLevel1*Point))
                    {
                     VegasPriceCrossedL=(MathRound(10000*VegasTunnelSlow)/10000);
                     if (OrderStopLoss()<VegasPriceCrossedL-(TPLevel1+StopLossToFibo)*Point)
                        OrderModify(OrderTicket(),Ask,VegasPriceCrossedL-(TPLevel1+StopLossToFibo)*Point,OrderTakeProfit(),0);
                     continue;
                    }
                  // Reach 2nd TP level, close the 2nd lot, move up remainder stop to TPLevel2-StopLossToFibo
                  // Or start trailing stop
                  else if(Bid>=(VegasTunnelSlow-TPLevel2*Point))
                       {
                        VegasPriceCrossedL=(MathRound(10000*VegasTunnelSlow)/10000);
                        if (OrderStopLoss()<VegasPriceCrossedL-(TPLevel2+StopLossToFibo)*Point)
                           OrderModify(OrderTicket(),Ask,VegasPriceCrossedL-(TPLevel2+StopLossToFibo)*Point,OrderTakeProfit(),0);
                        continue;
                       }
                  // Reach 1nd TP level, close the 1st lot, move up remainder stop to TPLevel1-StopLossToFibo
                     else if(Bid>=(VegasTunnelSlow-TPLevel3*Point))
                          {
                           VegasPriceCrossedL=(MathRound(10000*VegasTunnelSlow)/10000);
                           if (OrderStopLoss()<VegasPriceCrossedL-(TPLevel3+StopLossToFibo)*Point)
                              OrderModify(OrderTicket(),Ask,VegasPriceCrossedL-(TPLevel3+StopLossToFibo)*Point,OrderTakeProfit(),0);
                           continue;
                          }
                 }
              }
            else // Short Exits
              {
               // When EMA or RSI crosses reverse, exit all lots
               if((EMACrossedUp==true) || (RSICrossedUp==true))
                 {
                  OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet);
                  continue;
                 }
               if(TunnelCrossedDown==true)
                 {
                  if(Ask<=(VegasTunnelSlow-TPLevel3*Point))
                    {
                     if (OrderStopLoss()>Ask+TrailingStop*Point)
                        OrderModify(OrderTicket(),Bid,Ask+TrailingStop*Point,OrderTakeProfit(),0);
                     continue;
                    }
                  // Reach 2nd TP level, close the 2nd lot, move up remainder stop to TPLevel2-StopLossToFibo
                  // Or start trailing stop
                  else if(Ask<=(VegasTunnelSlow-TPLevel2*Point))
                       {
                        VegasPriceCrossedS=(MathRound(10000*VegasTunnelSlow)/10000);
                        if (OrderLots()>1 && OrderOpenPrice()>VegasTunnelSlow-(TPLevel2-12)*Point)
                           OrderClose(OrderTicket(),TPLevel2Lots,Ask,3,Violet);
                        //OrderModify(OrderTicket(),Bid,VegasPriceCrossedS-(TPLevel2+StopLossToFibo)*Point,OrderTakeProfit(),0);
                        if (OrderStopLoss()>Ask+TrailingStop*Point)
                           OrderModify(OrderTicket(),Bid,Ask+TrailingStop*Point,OrderTakeProfit(),0);
                        continue;
                       }
                  // Reach 2nd TP level, close the 2nd lot, move up remainder stop to TPLevel2-StopLossToFibo
                     else if(Ask<=(VegasTunnelSlow-TPLevel1*Point))
                          {
                           VegasPriceCrossedS=(MathRound(10000*VegasTunnelSlow)/10000);
                           if (OrderLots()>2 && OrderOpenPrice()>VegasTunnelSlow-(TPLevel1-12)*Point)
                              OrderClose(OrderTicket(),TPLevel1Lots,Ask,3,Violet);
                           //OrderModify(OrderTicket(),Bid,VegasPriceCrossedS-(TPLevel1+StopLossToFibo)*Point,OrderTakeProfit(),0);
                           if (OrderStopLoss()>Ask+TrailingStop*Point)
                              OrderModify(OrderTicket(),Bid,Ask+TrailingStop*Point,OrderTakeProfit(),0);
                           continue;
                          }
                  //Tunnel crossed, move stop to above the tunnel; 
                  VegasPriceCrossedS=(MathRound(10000*VegasTunnelSlow)/10000);
                  if (OrderStopLoss()>VegasPriceCrossedS+(TunnelBandWidth+StopLossToFibo)*Point)
                     OrderModify(OrderTicket(),Bid,VegasPriceCrossedS+(TunnelBandWidth+StopLossToFibo)*Point,OrderTakeProfit(),0);
                  continue;
                 }
               else
                 {
                  if(Ask<=(VegasTunnelSlow+TPLevel1*Point))
                    {
                     VegasPriceCrossedS=(MathRound(10000*VegasTunnelSlow)/10000);
                     if (OrderStopLoss()>VegasPriceCrossedS+(TPLevel1+StopLossToFibo)*Point)
                        OrderModify(OrderTicket(),Bid,VegasPriceCrossedS+(TPLevel1+StopLossToFibo)*Point,OrderTakeProfit(),0);
                     continue;
                    }
                  // Reach 2nd TP level, close the 2nd lot, move up remainder stop to TPLevel2-StopLossToFibo
                  // Or start trailing stop
                  else if(Ask<=(VegasTunnelSlow+TPLevel2*Point))
                       {
                        VegasPriceCrossedS=(MathRound(10000*VegasTunnelSlow)/10000);
                        if (OrderStopLoss()>VegasPriceCrossedS+(TPLevel2+StopLossToFibo)*Point)
                           OrderModify(OrderTicket(),Bid,VegasPriceCrossedS+(TPLevel2+StopLossToFibo)*Point,OrderTakeProfit(),0);
                        continue;
                       }
                  // Reach 1nd TP level, close the 1st lot, move up remainder stop to TPLevel1-StopLossToFibo
                     else if(Ask<=(VegasTunnelSlow+TPLevel3*Point))
                          {
                           VegasPriceCrossedS=(MathRound(10000*VegasTunnelSlow)/10000);
                           if (OrderStopLoss()>VegasPriceCrossedS+(TPLevel3+StopLossToFibo)*Point)
                              OrderModify(OrderTicket(),Bid,VegasPriceCrossedS+(TPLevel3+StopLossToFibo)*Point,OrderTakeProfit(),0);
                           continue;
                          }
                 }
              }
           }
        }
/*   }  
   if (Minute()==0 && Seconds()>30)
     { 
*/
      // *********************************************************************
      // Based on states, determine entry situations.
      // *********************************************************************
      //Check the trading time
      if(!((LondonOpen==true  && Hour()>=7 && Hour()<=16)  ||
             (NewYorkOpen==true && Hour()>=12 && Hour()<=21) ||
             (TokyoOpen==true   && ((Hour()>=0 && Hour()<=8) ||
                                    (Hour()>=23 && Hour()<=24)))
            )
         )
        {
         // Outside the trading hours
         return(0);
        }
      else
        {
         total=OrdersTotal();
         orders=0;
         for(cnt=0;cnt<total;cnt++)
           {
            OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
            if(OrderSymbol()==Symbol()) orders++;
           }
         //Print("orders = ",orders);
         //if(orders==0) 
           {
            //*************//
            // Long Entry  //
            //*************//
            if((EMACrossedUp==true) && (RSICrossedUp==true) && // Raw long signal generating rules
               // Experimenting new filters here
               // 1. For long trade, should only enter on the top of the tunnel to avoid tunnel resistance
               // 2. Shouldn't enter long on the high fibo lines above tunnel because the price tends to 
               //    go back to tunnel.
               // 3. Instead of push up from the tunnel, the price may drop from high fibo line area but with 
               //    EMA and RSI crossed, should avoid enter long at such situation.
               // 4. Around tunnel there ususually have a lot whipsaw, should avoid enter a trade in the band
               // 5. Should allow the price push from below the tunnel and enter long trade.
              ((Close[1]>=VegasTunnelSlow+TunnelBandWidth*Point && // Above whipsaw zone
                  Close[1]<=VegasTunnelSlow+TunnelSafeZone*Point &&  // Below high fibo line
                  Open[1]<Close[1]) ||                               // Price push up from a bull bar
                 (Close[1]<=VegasTunnelSlow-TunnelBandWidth*Point)   // Testing from below the tunnel
               ))
              {
/*               if (total==1)
               {
                  OrderSelect(0, SELECT_BY_POS, MODE_TRADES);
                  if(OrderType()==OP_SELL && OrderSymbol()==Symbol())
                  {
                     OrderClose(OrderTicket(),OrderLots(),Ask,3,Violet); 
                  }
                  else 
                     return(0); 
               }
*/
               ticket=OrderSend(Symbol(),OP_BUY,TotalLots,Ask,3,Ask-HardStopLoss*Point,0,"Bago",00011,0,Green);
               if(ticket>0)
                 {
                  if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("Long order opened : ",OrderOpenPrice());
                 }
               else Print("Error opening Long order : ",GetLastError());
               return(0);
              }
            //*************//
            // Short Entry //
            //*************//
            if((EMACrossedDown==true) && (RSICrossedDown==true) && // Raw short signal generating rules
               // Similar to long signal filtering rules.
              ((Close[1]<=VegasTunnelSlow-TunnelBandWidth*Point && // Below whipsaw zone
                  Close[1]>=VegasTunnelSlow-TunnelSafeZone*Point &&  // Above high fibo line
                  Open[1]>Close[1]) ||                               // Price down from a bear bar
                 (Close[1]>=VegasTunnelSlow+TunnelBandWidth*Point)   // Testing from above the tunnel
               ))
              {
/*               if (total==1)
               {
                  OrderSelect(0, SELECT_BY_POS, MODE_TRADES);
                  if(OrderType()==OP_BUY && OrderSymbol()==Symbol())
                  {
                     OrderClose(OrderTicket(),OrderLots(),Bid,3,Violet); 
                  }
                  else 
                     return(0); 
               }
*/
               ticket=OrderSend(Symbol(),OP_SELL,TotalLots,Bid,3,Bid+HardStopLoss*Point,0,"Sidus",00021,0,Red);
               if(ticket>0)
                 {
                  if(OrderSelect(ticket,SELECT_BY_TICKET,MODE_TRADES)) Print("Short order opened : ",OrderOpenPrice());
                 }
               else Print("Error opening Short order : ",GetLastError());

               return(0);
              }
            return(0);
           }
        }
     }
   return(0);
  }
//+------------------------------------------------------------------+