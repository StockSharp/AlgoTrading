//+------------------------------------------------------------------+
//|                                                  TDSGlobal 4.mq4 |
//|                           Copyright © 2005 Bob O'Brien / Barcode |
//|                                       http://mak.tradersmind.com |
//+------------------------------------------------------------------+

// Expert based on Alexander Elder's Triple Screen system. 
// To be run only on a Daily chart.
//---- External Variables
extern int Lots = 1;
extern int TakeProfit = 999;
extern int Stoploss = 0;
extern int TrailingStop = 10;
extern int Slippage = 5; // Slippage
extern int StopYear = 2005;
extern int MM = 0, Leverage = 1, AcctSize = 10000;
extern int WilliamsP = 24, WilliamsL = -75, WilliamsH = -25;
//----
int BuyEntryOrderTicket = 0, SellEntryOrderTicket = 0, cnt = 0, total = 0;
double MacdCurrent = 0, MacdPrevious = 0, MacdPrevious2 = 0, Direction = 0, 
       OsMAPrevious = 0, OsMAPrevious2 = 0, OsMADirection = 0;
double newbar = 0, PrevDay = 0, PrevMonth = 0, PrevYear = 0, PrevCurtime = 0;
double PriceOpen = 0; // Price Open
bool First = True;
double TradesThisSymbol = 0;
double WilliamsSell = 0, WilliamsBuy = 0, Williams = 0,NewPrice = 0;
double StartMinute1 = 0, EndMinute1 = 0, StartMinute2 = 0, EndMinute2 = 0, 
      StartMinute3 = 0, EndMinute3 = 0;
double StartMinute4 = 0, EndMinute4 = 0, StartMinute5 = 0, EndMinute5 = 0, 
       StartMinute6 = 0, EndMinute6 = 0;
double StartMinute7 = 0, EndMinute7 = 0, DummyField = 0;
double Lotsf = 0;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   Lotsf = MathRound(AccountBalance() / 10000);
/*   Comment("TSD for MT4 ver beta 0.2 - DO NOT USE WITH REAL MONEY YET",
           "\n",
           "\n","Weekly MacdPrevious = ", MacdPrevious, "    Weekly OsMAPrevious  = ",
           OsMAPrevious,
           "\n","Weekly MacdPrevious2 = ", MacdPrevious2, "    Weekly OsMAPrevious2 = ", 
           OsMAPrevious2,
           "\n","Weekly Direction = ", Direction, "    Weekly OsMADirection = ", OsMADirection,
           "\n",
           "\n","Lotsf = ", Lotsf,
           "\n",
           "\n","Daily Williams = ", Williams,
           "\n","Is Daily Williams Bullish = ", WilliamsSell,
           "\n","Is Daily Williams Bearish = ", WilliamsBuy,
           "\n",
           "\n","Total Orders = ", total,
           "\n","Trades this Symbol(",Symbol(), ") = ", TradesThisSymbol,
           "\n",
           "\n","New Bar Time is ", TimeToStr(newbar),
           "\n",
           "\n","Daily High[1] = ",High[1],
           "\n","Daily High[2] = ",High[2],
           "\n","Daily Low[1] = ",Low[1],
           "\n","Daily Low[2] = ",Low[2],
           "\n",
           "\n","Current Ask Price + 16 pips = ",Ask+(16*Point),
           "\n","Current Bid Price - 16 pips = ",Bid-(16*Point));*/
   total = OrdersTotal();
   TradesThisSymbol = 0;
   for(cnt = 0; cnt < total; cnt++)
     { 
       OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
       if(OrderSymbol() == Symbol())
         {
           TradesThisSymbol++;
         } // close for if(OrderSymbol()==Symbol())
     } // close for for(cnt=0;cnt<total;cnt++)         
   MacdPrevious  = iMACD(NULL, 1440, 12, 26, 9, PRICE_CLOSE, MODE_MAIN, 1);
   MacdPrevious2 = iMACD(NULL, 1440, 12, 26, 9, PRICE_CLOSE, MODE_MAIN, 2);
   OsMAPrevious  = iOsMA(NULL, 1440, 12, 26, 9, PRICE_CLOSE, 1);
   OsMAPrevious2 = iOsMA(NULL, 1440, 12, 26, 9, PRICE_CLOSE, 2);
   Williams = iWPR(NULL, 1440, WilliamsP, 1); 
   WilliamsSell = iWPR(NULL, 1440, 24, 1) > WilliamsL;
   WilliamsBuy  = iWPR(NULL, 1440, 24, 1) < WilliamsH;
   if(MacdPrevious > MacdPrevious2) 
       Direction = 1;
   if(MacdPrevious < MacdPrevious2) 
       Direction = -1;
   if(MacdPrevious == MacdPrevious2) 
       Direction = 0;
   if(OsMAPrevious > OsMAPrevious2) 
       OsMADirection = 1;
   if(OsMAPrevious < OsMAPrevious2) 
       OsMADirection = -1;
   if(OsMAPrevious == OsMAPrevious2) 
       OsMADirection = 0;
// Select a range of minutes in the day to start trading based on the currency pair.
// This is to stop collisions occurring when 2 or more currencies set orders at the same time.
   if(Symbol() == "USDCHF")
     {
       StartMinute1 = 0;
       EndMinute1   = 1;
       StartMinute2 = 8;
       EndMinute2   = 9;
       StartMinute3 = 16;
       EndMinute3   = 17;
       StartMinute4 = 24;
       EndMinute4   = 25;
       StartMinute5 = 32;
       EndMinute5   = 33;
       StartMinute6 = 40;
       EndMinute6   = 41;
       StartMinute7 = 48;
       EndMinute7   = 49;
     } // close for if(Symbol() == "USDCHF")
   if(Symbol() == "GBPUSD")
     {  
       StartMinute1 = 2;
       EndMinute1   = 3;
       StartMinute2 = 10;
       EndMinute2   = 11;
       StartMinute3 = 18;
       EndMinute3   = 19;
       StartMinute4 = 26;
       EndMinute4   = 27;
       StartMinute5 = 34;
       EndMinute5   = 35;
       StartMinute6 = 42;
       EndMinute6   = 43;
       StartMinute7 = 50;
       EndMinute7   = 51;
     } // close for if(Symbol() == "GBPUSD")
   if(Symbol() == "USDJPY")
     {
       StartMinute1 = 4;
       EndMinute1   = 5;
       StartMinute2 = 12;
       EndMinute2   = 13;
       StartMinute3 = 20;
       EndMinute3   = 21;
       StartMinute4 = 28;
       EndMinute4   = 29;
       StartMinute5 = 36;
       EndMinute5   = 37;
       StartMinute6 = 44;
       EndMinute6   = 45;
       StartMinute7 = 52;
       EndMinute7   = 53;
     } //close for if(Symbol() == "USDJPY")
   if(Symbol() == "EURUSD")
     {
       StartMinute1 = 6;
       EndMinute1   = 7;
       StartMinute2 = 14;
       EndMinute2   = 15;
       StartMinute3 = 22;
       EndMinute3   = 23;
       StartMinute4 = 30;
       EndMinute4   = 31;
       StartMinute5 = 38;
       EndMinute5   = 39;
       StartMinute6 = 46;
       EndMinute6   = 47;
       StartMinute7 = 54;
       EndMinute7   = 59;
     } // close for if(Symbol() == "EURUSD")
   if((Minute() >= StartMinute1 && Minute() <= EndMinute1) ||
      (Minute() >= StartMinute2 && Minute() <= EndMinute2) ||
      (Minute() >= StartMinute3 && Minute() <= EndMinute3) ||
      (Minute() >= StartMinute4 && Minute() <= EndMinute4) ||
      (Minute() >= StartMinute5 && Minute() <= EndMinute5) ||
      (Minute() >= StartMinute6 && Minute() <= EndMinute6) ||
      (Minute() >= StartMinute7 && Minute() <= EndMinute7))
     {
       // dummy statement because MT will not allow me to use a continue statement
       DummyField = 0;
     } // close for LARGE if statement
   else 
       return(0);
/////////////////////////////////////////////////
//  Process the next bar details
/////////////////////////////////////////////////
   if(newbar != Time[0]) 
     {
       newbar = Time[0];
       if(TradesThisSymbol < 1) 
         {
           if(Direction == 1 && WilliamsBuy)
             {
               // Buy 1 point above high of previous candle
               PriceOpen = High[1] + 1 * Point; 
               // Check if buy price is a least 16 points > Ask
               if(PriceOpen > (Ask + 16 * Point))  
                 {
                   BuyEntryOrderTicket = OrderSend(Symbol(), OP_BUYSTOP, Lotsf, PriceOpen, 
                                                   Slippage, Low[1] - 1 * Point, 
                                                   PriceOpen + TakeProfit * Point, 
                                                   "Buy Entry Order placed at " + CurTime(), 
                                                   0, 0, Green);
                   return(0);

                 } // close for if(PriceOpen > (Ask + 16 * Point))
               else
                 {
                   NewPrice = Ask + 16 * Point;
                   BuyEntryOrderTicket = OrderSend(Symbol(), OP_BUYSTOP, Lotsf, NewPrice, 
                                                   Slippage, Low[1] - 1 * Point, 
                                                   NewPrice + TakeProfit * Point, 
                                                   "Buy Entry Order placed at " + CurTime(), 
                                                   0, 0, Green);
                   return(0);
                 } // close for else statement
             } // close for if(Direction == 1 && WilliamsBuy)
           if(Direction == -1 && WilliamsSell)
             {
               PriceOpen = Low[1] - 1 * Point;
               // Check if buy price is a least 16 points < Bid
               if(PriceOpen < (Bid - 16 * Point)) 
                 {
                   SellEntryOrderTicket = OrderSend(Symbol(), OP_SELLSTOP, Lotsf, PriceOpen, 
                                                    Slippage, High[1] + 1 * Point, 
                                                    PriceOpen - TakeProfit * Point, 
                                                    "Sell Entry Order placed at " + CurTime(), 
                                                    0, 0, Green);
                   return(0);
                 } // close for if(PriceOpen < (Bid - 16 * Point))
               else
                 {
                   NewPrice = Bid - 16 * Point;
                   SellEntryOrderTicket = OrderSend(Symbol(), OP_SELLSTOP, Lotsf, NewPrice, 
                                                    Slippage, High[1] + 1 * Point, 
                                                    NewPrice - TakeProfit * Point, 
                                                    "Sell Entry Order placed at " + CurTime(), 
                                                    0, 0, Green);
                   return(0);
                 } // close for else statement

             } // close for if(Direction == -1 && WilliamsSell)
         } //Close of if(TradesThisSymbol < 1)
/////////////////////////////////////////////////
//  Pending Order Management
/////////////////////////////////////////////////
       if(TradesThisSymbol > 0)
         {
           total = OrdersTotal();
           for(cnt = 0; cnt < total; cnt++)
             { 
               OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
               if(OrderSymbol() == Symbol() && OrderType() == OP_BUYSTOP)
                 {
                   if(Direction == -1)
                     { 
                       OrderDelete(OrderTicket());
                       return(0); 
                     } // close for if(Direction == -1)
                 } // close for if(OrderSymbol()==Symbol() && OrderType()==OP_BUYSTOP)
               if(OrderSymbol() == Symbol() && OrderType() == OP_SELLSTOP)
                 {
                   if(Direction == 1)
                     { 
                       OrderDelete(OrderTicket());
                       return(0); 
                     } //close for if(Direction == 1)
                 } //close for if(OrderSymbol()==Symbol() && OrderType()==OP_SELLSTOP)
               if(OrderSymbol() == Symbol() && OrderType() == OP_BUYSTOP)
                 {
                   if(High[1] < High[2])
                     { 
                       if(High[1] > (Ask + 16 * Point))
                         { 
                           OrderModify(OrderTicket(), High[1] + 1 * Point, Low[1] - 1 * Point,
                                       OrderTakeProfit(), 0, Cyan);
                           return(0);
                         } //close for if(High[1] > (Ask + 16 * Point))
                       else
                         {
                           OrderModify(OrderTicket(), Ask + 16 * Point, Low[1] - 1 * Point,
                                       OrderTakeProfit(), 0, Cyan);
                           return(0);
  
                         } //close for else statement
                     } //close for if(High[1] < High[2])
                 } //close for if(OrderSymbol()==Symbol() && OrderType()==OP_BUYSTOP)
               if(OrderSymbol() == Symbol() && OrderType() == OP_SELLSTOP)
                 {
                   if(Low[1] > Low[2])
                     { 
                       if(Low[1] < (Bid - 16 * Point))
                         {
                           OrderModify(OrderTicket(), Low[1] - 1 * Point, High[1] + 1 * Point,
                                       OrderTakeProfit(), 0, Cyan);
                           return(0);
                         } // close for if(Low[1] < (Bid - 16 * Point))
                       else
                         {
                           OrderModify(OrderTicket(), Bid - 16 * Point, High[1] + 1 * Point, 
                                       OrderTakeProfit(), 0, Cyan);
                           return(0);
      
                         } //close for else statement
                     } //close for if(Low[1] > Low[2])
                 } //close for if(OrderSymbol()==Symbol() && OrderType()==OP_SELLSTOP)
             } // close for for(cnt=0;cnt<total;cnt++)
         } // close for if(TradesThisSymbol > 0)
     } // close for if (newbar != Time[0]) 
/////////////////////////////////////////////////
//  Stop Loss Management
/////////////////////////////////////////////////
   if(TradesThisSymbol > 0)
     {
       total = OrdersTotal();
       for(cnt = 0; cnt < total; cnt++)
         { 
           OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
           if(OrderSymbol() == Symbol() && OrderType() == OP_BUY)
             {
               if(Ask - OrderOpenPrice() > (TrailingStop * Point))
                 { 
                   if(OrderStopLoss() < (Ask - TrailingStop * Point))
                     { 
                       OrderModify(OrderTicket(), OrderOpenPrice(), Ask - TrailingStop * Point,
                                   Ask + TakeProfit * Point, 0, Cyan);
                       return(0);

                     } // close for if(OrderStopLoss() < (Ask - TrailingStop * Point))
                 } // close for if(Ask-OrderOpenPrice() > (TrailingStop * Point))
             } // close for if(OrderSymbol()==Symbol() && OrderType()==OP_BUY)
           if(OrderSymbol() == Symbol() && OrderType() == OP_SELL)
             {
               if(OrderOpenPrice() - Bid > (TrailingStop * Point))
                 { 
                   if(OrderStopLoss() > (Bid + TrailingStop * Point))
                     { 
                       OrderModify(OrderTicket(), OrderOpenPrice(), Bid + TrailingStop * Point,
                                   Bid - TakeProfit * Point, 0, Cyan);
                       return(0);

                     } // close for if(OrderStopLoss() > (Bid + TrailingStop * Point))
                 } // close for if(OrderOpenPrice() - Bid > (TrailingStop * Point))
             } // close for if(OrderSymbol()==Symbol() && OrderType()==OP_SELL)
         } // close for for(cnt=0;cnt<total;cnt++)
     } // close for if(TradesThisSymbol > 0)
//----
   return(0);
  } // close for start
//+------------------------------------------------------------------+

