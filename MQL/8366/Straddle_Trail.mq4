//+-----------------------------------------------------------------------------+
//|                                                          Straddle&Trail.mq4 |
//|                                                    Copyright © 2006, Yannis |
//|                                                            jsfero@otenet.gr |
//+-----------------------------------------------------------------------------+
//|                                       v2.40                                 |
//+-----------------------------------------------------------------------------+
// The ea can manage at the same time manually entered trades at any time (they have a magic number of 0)
// AND/OR a straddle (Long and Short pending positions - Stop orders) that is placed for a news event
// at a specific time of the day by this ea. 
//
// The manual trades are checked against Symbol() and MagicNumber=0 so if you have other experts running 
// on the same pair assuming they assign a magic number > 0 to the trades, you won't have any problems between 
// the manual trades and those from your expert. 
//
// The trades entered by the expert are checked against Symbol() Period() And an automatically given Magic Number
// so here also absolutely no problem if you have other experts running.
//
// The positions are tracked through the PosCounter() procedure
// which checks for :   b.ticket  / s.ticket    ==> Ticket # from Orders actually triggered BY THE EA, if any, otherwise = 0.
//                      b.ticketP / s.ticketP   ==> Ticket # from Pending Stop Orders NOT TRIGGERED (the straddle).
//                      b.ticketM / s.ticketM   ==> Ticket # from Orders entered Manually.
// The manually entered positions are tracked as far as SL, TP, BE and Trail are concerned, 
// and are totally unaffected by the straddle.
//
// The Stop Orders entered before the news release, either immediately if "Pre.Event.Entry.Minutes"=0 or
// xx minutes before the event and they are tracked and adjusted ONCE EVERY MINUTE, from the moment
// they are entered by the ea until a few minutes before the event (specified by "Stop.Adjusting.Min.Before.Event" parameter) 
// modifying their entry price, stop loss and take profit, according to current Bid and Ask, if "Adjust.Pending.Orders"=True.
// Once one of them is triggered, the opposite one is removed if "Remove.Opposite.Order" = True.
// 
// Another way of entering a straddle is by setting the "Place.Straddle.Immediately" to True. In that case,
// The time event settings will be ignored and the long and short pending orders that will be entered immediately
// will not be adjusted according to price like they are when this parameter is set to false and we use the ea for
// news release. This is to be used as a narrow range, low volume breakout strategy like for instance during a ranging 
// Asian period.
//
// The "ShutDown.NOW" parameter will shut down all the trades specified by "ShutDown.What" parameter.
// On the ea's input tab you can see the possible values but here they are again as a reference.
// ShutDown.What ==>    0=Everything, 1=All Triggered Positions,  2=Triggered Long
//                      3=Triggered Short, 4=All Pending Positions, 5=Pending Long, 6=Pending Short
// If "ShutDown.Current.Pair.Only" then the ea will close all trades for the pair on which this parameter was set to true
// otherwise it will close ALL the trades on ALL the pairs. 
//
// The ea will check the minimum distance allowed from the broker 
// against the trail, stop loss and take profit values specified by the user
// in the expert parameters tab. If these values are below the allowed distance
// they will be automatically adjusted to that minimum value
#include <stdlib.mqh>
//----
extern bool    ShutDown.NOW=False;                 // If true ALL POSITIONS (open and/or pending) will be closed/deleted
                                                   // based on "ShutDown.What" flag below
                                                   // This parameter is the first on the list so the user can access it
                                                   // as quickly as possible.
extern string  sStr00=" 0=Everything               ";
extern string  sStr01=" 1=All Triggered Positions  ";
extern string  sStr02=" 2=Triggered Long           ";
extern string  sStr03=" 3=Triggered Short          ";
extern string  sStr04=" 4=All Pending Positions    ";
extern string  sStr05=" 5=Pending Long             ";
extern string  sStr06=" 6=Pending Short            ";
extern int     ShutDown.What=0;
extern bool    ShutDown.Current.Pair.Only=True;    // If true, ALL trades for CURRENT pair will be shutdown (no matter what time frame).
                                                   // If False, ALL trades on ALL pairs will be shutdown.
extern string  sStr1="=== POSITION DETAILS ===";
extern double  Lots=1;
extern int     Slippage=10;
extern int     Distance.From.Price=30;             // Initial distance from price for the 2 pending orders.
extern int     StopLoss.Pips=30;                   // Initial stop loss. 
extern int     TakeProfit.Pips=60;                 // Initial take profit.
extern int     Trail.Pips=15;                      // Trail.
extern bool    Trail.Starts.After.BreakEven=true;  // if true trailing will start after a profit of "Move.To.BreakEven.at.pips" is made
extern int     Move.To.BreakEven.Lock.pips=1;      // Pips amount to lock once trade is in profit 
                                                   // by the number of pips specified with "Move.To.BreakEven.at.pips"
                                                   // Unused if Trail.Starts.After.BreakEven=False
extern int     Move.To.BreakEven.at.pips=5;        // trades in profit will move to entry price + Move.To.BreakEven.Lock.pips as soon as trade 
                                                   // is at entry price + Move.To.BreakEven.at.pips

                                                   // i.e. Entry price on a long order is @ 1.2100
                                                   // when price reaches 1.2110 (Entry price + "Move.To.BreakEven.at.pips")
                                                   // the ea will lock 1 pip moving sl 
                                                   // at 1.2101 (Entry price+ "Move.To.BreakEven.Lock.pips=1")
extern string  sStr2="=== NEWS EVENT ===";
extern int     Event.Start.Hour=12;                // Event start time = Hour.      Broker's time.
extern int     Event.Start.Minutes=30;             // Event start time = Minutes.   Broker's time.
                                                   // IF YOU WANT TO DISABLE THE "NEWS" FEATURE (the straddle)
                                                   // SET BOTH PARAMETERS TO 0.
extern int     Pre.Event.Entry.Minutes=30;         // Number of minutes before event where the ea will place the straddle.
                                                   // If set to 0, the ea will place the straddle immediately when activated,
                                                   // otherwise xx minutes specified here before above Event start time.
extern int     Stop.Adjusting.Min.Before.Event=2;  // Minutes before the event where the EA will stop adjusting
                                                   // the pending orders. The smallest value is 1 min.
extern bool    Remove.Opposite.Order=True;         // if true, once the 1st of the 2 pending orders is triggered, 
                                                   // the opposite pending one is removed otherwise left as is.
extern bool    Adjust.Pending.Orders=True;         // if true, once the pending orders are placed at
                                                   // "Pre.Event.Start.Minutes" minutes before the event's time, 
                                                   // the ea will try to adjust the orders ONCE EVERY MINUTE until
                                                   // "Stop.Adjusting.Min.Before.Event" minutes before the release where
                                                   // it will stay put. 
extern bool    Place.Straddle.Immediately=False;   // if true, the straddle will be placed immediately once the 
                                                   // expert is activated. This overrides previous 'News Events' 
                                                   // settings for placing the long and short pending orders and 
                                                   // in that case, the positions WILL NOT BE ADJUSTED. 
                                                   // This is to be used as a "quiet" range breakout, for example if we 
                                                   // want to play a "regular" breakout during Asian Session for example
                                                   // or at any other time of the day where the market is rangebound
int iMinimum,Last.Ticket,b.ticket,s.ticket,b.ticketP,s.ticketP,b.ticketM,s.ticketM,Magic,OpenTrades;
int Retry,HourNow,MinutesNow,LastMin,MyErr,OpenPositions,OpenLongM,OpenShortM,OpenLong,OpenShort,OpenLongP,OpenShortP;
string comment=" Straddle&Trail",ScreenComment="Straddle&Trail v2.40",NewsLabel="",sHour,sMin;
bool SupplyMagicNumber=True,IsOK=False;
double LongEntryLevel,ShortEntryLevel,SL,TP;
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int init()
  {  
   comments();
   if ((Event.Start.Hour>=0) && (Event.Start.Hour<=9)) sHour="0";
   else sHour="";
   if ((Event.Start.Minutes>=0) && (Event.Start.Minutes<=9)) sMin="0";
   else sMin="";
   if ((Event.Start.Hour==0) && (Event.Start.Minutes==0)) NewsLabel=ServerTime()+"  No News Event Scheduled";
   else NewsLabel=(ServerTime()+"    NEWS SCHEDULED FOR ("+sHour+Event.Start.Hour+":"+sMin+Event.Start.Minutes+")");
   ObjectDelete("Yannis");
   YannisCustomText( "Yannis", 15, 15,1);
   ObjectSetText( "Yannis", "jsfero@otenet.gr"  , 10, "Times New Roman", Yellow );
   ObjectDelete("Yannis2");
   YannisCustomText( "Yannis2", 15, 15,3);
   ObjectSetText( "Yannis2", NewsLabel , 10, "Tahoma", LightSkyBlue );
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int deinit()
  {  ObjectDelete("Yannis");
   ObjectDelete("Yannis2");
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int start()
  {
   iMinimum=MarketInfo(Symbol(),MODE_STOPLEVEL); // check the minimum pip distance allowed from broker for sl and tp
   int EventAboutToStart=IsItEventTime();
   if ((Event.Start.Hour>=0) && (Event.Start.Hour<=9)) sHour="0";
   else sHour="";
   if ((Event.Start.Minutes>=0) && (Event.Start.Minutes<=9)) sMin="0";
   else sMin="";
   if ((Event.Start.Hour==0) && (Event.Start.Minutes==0)) NewsLabel=ServerTime()+"  No News Event Scheduled";
   else NewsLabel=(ServerTime()+"    NEWS SCHEDULED FOR ("+sHour+Event.Start.Hour+":"+sMin+Event.Start.Minutes+")");
   Magic=CalcMagic  (Symbol(),Period());
   if (StopLoss.Pips  <iMinimum) StopLoss.Pips  =iMinimum;
   if (TakeProfit.Pips<iMinimum) TakeProfit.Pips=iMinimum;
   if (Trail.Pips     <iMinimum) Trail.Pips     =iMinimum;
//----
   PosCounter();  // check for open positions. Sets b.ticket, s.ticket, b.ticketP, s.ticketP, b.ticketM, s.ticketM
   // b.ticketM / s.ticketM   = Ticket Number for Manual Trades 
   //                           (b.ticketM=Long position s.ticketM=Short position, if any, otherwise = 0)
   // b.ticketP / s.ticketP   = Ticket Number for Pending Trades (Straddle) from EA 
   // b.ticket  / s.ticket    = Ticket Number for Triggered Trades (Straddle) from EA
   if ((ShutDown.NOW) && (OpenTrades>0)) ShutDownAllTrades(ShutDown.What);
//----
   PosCounter();
   CheckInitialSLTP(); // If a position is entered without initial SL / TP then place the initial stops
   //    If no open or pending positions, AND the event start hour and minute is not 0, place the 2 pending orders
   //    If Pre.Event.Entry.Minutes=0 (immediately) or
   //    else Pre event minutes before event time  
   if((IsTimeToPlaceEntries()) || (Place.Straddle.Immediately))
     {  
          if ((s.ticketP==0) &&
              (b.ticketP==0) &&
              (s.ticket ==0) &&
              (b.ticket ==0)) PlaceTheStraddle();
     }
//----
   PosCounter();
   // If both pending orders are placed and no trades actually opened 
   // and we are not into the specified minutes before the event, then we adjust our positions
   // according to current price automatically every new minute
   if ((EventAboutToStart==0) &&
       (s.ticketP >0)         &&
       (b.ticketP >0)         &&
       (s.ticket ==0)         &&
       (b.ticket ==0)         &&
       (MinutesNow!=LastMin)  &&
       (Adjust.Pending.Orders)&&
       (!Place.Straddle.Immediately)) AdjustPendingOrders();
   // If parameter "Remove.Opposite.Order" is set to True, remove non triggered opposite pending order
   PosCounter();
   if (Remove.Opposite.Order)
     {  if (((s.ticket>0) && (b.ticketP>0)) ||
              ((b.ticket>0) && (s.ticketP>0))) RemoveOppositePending();
     }
   // If a position is triggered, either manual or from ea, trail it.
   PosCounter();
   if ((s.ticket>0) || (b.ticket>0) || (b.ticketM>0) || (s.ticketM>0))
     {  if (Move.To.BreakEven.at.pips!=0) MoveToBreakEven(); // Check if must secure position
      Trail.Stop();                                        // Check trailing methods
     }
   comments();
   return(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool IsTimeToPlaceEntries()
  {
    return(((Pre.Event.Entry.Minutes==0) ||((HourNow==Event.Start.Hour)                                 &&
                                                  (MinutesNow>=(Event.Start.Minutes-Pre.Event.Entry.Minutes)) &&
                                                  (MinutesNow<=Event.Start.Minutes)
                                                )
              )&& (Event.Start.Hour>0) && (Event.Start.Minutes>0)
              );
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int IsItEventTime()
  {  HourNow   =TimeHour  (CurTime());
   MinutesNow=TimeMinute(CurTime());
//----
   if ((HourNow<Event.Start.Hour) || (HourNow>Event.Start.Hour))           return(0);
   if ((HourNow==Event.Start.Hour) && (MinutesNow>Event.Start.Minutes))    return(0);
   // Will return 0 if the event is not started or is over.
   if (Stop.Adjusting.Min.Before.Event<1)          Stop.Adjusting.Min.Before.Event=1;
   if (Stop.Adjusting.Min.Before.Event>Period())   Stop.Adjusting.Min.Before.Event=Period();
//----
   return((HourNow==Event.Start.Hour)                                          &&
            (MinutesNow>=(Event.Start.Minutes-Stop.Adjusting.Min.Before.Event))  &&
            (MinutesNow<=Event.Start.Minutes)
          );
   // Will return 0 hence allowing the ea to adjust its pending orders until minutes reaches 
   // range between (Event.Start.Minutes-Stop.Adjusting.Min.Before.Event) and Event.Start.Minutes range where the ea will return 1.
   // For example if the event is scheduled for 12.40, and we have set the 
   // Stop.Adjusting.Min.Before.Event parameter to 2, then until 12.27:59 the ea will adjust the pending orders.
   // From 12.28 and after it will stop adjusting and stay put.
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PlaceTheStraddle()
  {  
   int Retry=0;
   double ShortEntryLevel, LongEntryLevel, TP, SL;
//----
   if (StopLoss.Pips  ==0) StopLoss.Pips  =999;
   if (TakeProfit.Pips==0) TakeProfit.Pips=999;
   while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000);}
   RefreshRates();
   ShortEntryLevel=NormalizeDouble(Bid-(Distance.From.Price*Point),Digits);
   SL             =NormalizeDouble(ShortEntryLevel+(StopLoss.Pips*Point),Digits);
   TP             =NormalizeDouble(ShortEntryLevel-(TakeProfit.Pips*Point),Digits);
   Last.Ticket=OrderSend(  Symbol()          ,
                           OP_SELLSTOP       ,
                           Lots              ,
                           ShortEntryLevel   ,
                           Slippage          ,
                           SL                ,
                           TP                ,
                           "Straddle&Trail "+DoubleToStr(Period(),0)+"min ",
                           Magic             ,
                           0                 ,
                           OrangeRed);
   if (Last.Ticket<=0)
     {
       Print("Error opening SellStop  ",ErrorDescription(GetLastError()),"  Ask=",Ask,"   Bid=",Bid,"   Entry @ ",ShortEntryLevel,"   SL=",SL,"    TP=",TP);
     }
   Retry=0;
   while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
   RefreshRates();
   LongEntryLevel =NormalizeDouble(Ask+(Distance.From.Price*Point),Digits);
   SL             =NormalizeDouble(LongEntryLevel-(StopLoss.Pips*Point),Digits);
   TP             =NormalizeDouble(LongEntryLevel+(TakeProfit.Pips*Point),Digits);
   Last.Ticket=OrderSend(  Symbol()          ,
                           OP_BUYSTOP        ,
                           Lots              ,
                           LongEntryLevel    ,
                           Slippage          ,
                           SL                ,
                           TP                ,
                           "Straddle&Trail "+DoubleToStr(Period(),0)+"min ",
                           Magic             ,
                           0                 ,
                           RoyalBlue);
   if (Last.Ticket<=0)
     {
       Print("Error opening BuyStop  ",ErrorDescription(GetLastError()),"  Ask=",Ask,"   Bid=",Bid,"   Entry @ ",LongEntryLevel,"   SL=",SL,"    TP=",TP);
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void RemoveOppositePending()
  {
    if (b.ticketP>0)
     {
       OrderSelect(b.ticketP,SELECT_BY_TICKET);
      Retry=0; while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
      OrderDelete(b.ticketP);
      int MyErr=GetLastError();
      if (MyErr>0) Print("Error Removing Long Pending Order ticket=",b.ticketP,"  ",ErrorDescription(GetLastError()));
     }
   if (s.ticketP>0)
     {
      OrderSelect(s.ticketP,SELECT_BY_TICKET);
      Retry=0; while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
      OrderDelete(s.ticketP);
      MyErr=GetLastError();
      if (MyErr>0) Print("Error Removing Short Pending Order ticket=",s.ticketP,"  ",ErrorDescription(GetLastError()));
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void AdjustPendingOrders()
  {
   IsOK=False;
   // Adjust pending Long order
   while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
   RefreshRates();
   LongEntryLevel =NormalizeDouble(Ask+(Distance.From.Price*Point),Digits);
   SL             =NormalizeDouble(LongEntryLevel-(StopLoss.Pips*Point),Digits);
   TP             =NormalizeDouble(LongEntryLevel+(TakeProfit.Pips*Point),Digits);
   OrderModify(b.ticketP,LongEntryLevel,SL,TP,OrderExpiration(),MediumSpringGreen);
   MyErr=GetLastError();
   if (MyErr>0) Print("Error Adjusting Long Pending Order ticket=",b.ticketP,"  ",ErrorDescription(GetLastError()));
   else
     {
      Sleep(3000);
      // Adjust pending Short order
      while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
      RefreshRates();
      ShortEntryLevel=NormalizeDouble(Bid-(Distance.From.Price*Point),Digits);
      SL             =NormalizeDouble(ShortEntryLevel+(StopLoss.Pips*Point),Digits);
      TP             =NormalizeDouble(ShortEntryLevel-(TakeProfit.Pips*Point),Digits);
      OrderModify(s.ticketP,ShortEntryLevel,SL,TP,OrderExpiration(),MediumVioletRed);
      MyErr=GetLastError();
      if (MyErr>0) Print("Error Adjusting Short Pending Order ticket=",s.ticketP,"  ",ErrorDescription(GetLastError()));
      else
        {
         LastMin=MinutesNow; // Resetting the "counter" to current minute so the ea adjusts only every 1 minute
         Sleep(3000);
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void PosCounter()
  {
   b.ticket=0;s.ticket=0;b.ticketP=0;s.ticketP=0;OpenTrades=0;
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      // Trades entered by the EA
      if (OrderSymbol()==Symbol() && OrderMagicNumber()==Magic)
        {  
         if (OrderType()==OP_SELL)     {s.ticket =OrderTicket();}
         if (OrderType()==OP_SELLSTOP) {s.ticketP=OrderTicket();}
         if (OrderType()==OP_BUY)      {b.ticket =OrderTicket();}
         if (OrderType()==OP_BUYSTOP)  {b.ticketP=OrderTicket();}
         OpenTrades++;
        }
      // Manually Entered Trades (No Magic Number)
      else
         if (OrderSymbol()==Symbol() && OrderMagicNumber()==0)
           {
            if (OrderType()==OP_SELL)     {s.ticketM=OrderTicket();}
            if (OrderType()==OP_BUY)      {b.ticketM=OrderTicket();}
            OpenTrades++;
           }
     }
  }
//----  
void Trail.With.Standard.Trailing(int AfterBE)
  {
   double bsl, b.tsl, ssl, s.tsl;
   PosCounter();
   RefreshRates();
   if (AfterBE==0)
     {
       if (b.ticket>0)
        {
         bsl=Trail.Pips*Point;
         OrderSelect(b.ticket,SELECT_BY_TICKET);
         //determine if stoploss should be modified
         if(Bid>(OrderOpenPrice()+bsl) && OrderStopLoss()<(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl))))
           {
            b.tsl=NormalizeDouble(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl)),Digits);
            Print("b.tsl ",b.tsl);
            if (OrderStopLoss()<b.tsl)
              {
                while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
               OrderModify(b.ticket,OrderOpenPrice(),b.tsl,OrderTakeProfit(),OrderExpiration(),MediumSpringGreen);
               Sleep (2000);
              }
           }
        }
      if(s.ticket>0)
        {
         ssl=Trail.Pips*Point;
         //determine if stoploss should be modified
         OrderSelect(s.ticket,SELECT_BY_TICKET);
         if (Ask<(OrderOpenPrice()-ssl) && OrderStopLoss()>(OrderOpenPrice()-(OrderOpenPrice()-ssl)-Ask))
           {
            s.tsl=NormalizeDouble(OrderOpenPrice()-((OrderOpenPrice()-ssl)-Ask),Digits);
            Print("s.tsl ",s.tsl);
            if (OrderStopLoss()>s.tsl)
              {
               while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
               OrderModify(s.ticket,OrderOpenPrice(),s.tsl,OrderTakeProfit(),OrderExpiration(),MediumVioletRed);
               Sleep (2000);
              }
           }
        }
      if (b.ticketM>0)
        {
         bsl=Trail.Pips*Point;
         OrderSelect(b.ticketM,SELECT_BY_TICKET);
         //determine if stoploss should be modified
         if(Bid>(OrderOpenPrice()+bsl) && OrderStopLoss()<(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl))))
           {
            b.tsl=NormalizeDouble(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl)),Digits);
            Print("b.tsl ",b.tsl);
            if (OrderStopLoss()<b.tsl)
              {
               while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
               OrderModify(b.ticketM,OrderOpenPrice(),b.tsl,OrderTakeProfit(),OrderExpiration(),MediumSpringGreen);
               Sleep (2000);
              }
           }
        }
      if(s.ticketM>0)
        {
         ssl=Trail.Pips*Point;
         //determine if stoploss should be modified
         OrderSelect(s.ticketM,SELECT_BY_TICKET);
         if (Ask<(OrderOpenPrice()-ssl) && OrderStopLoss()>(OrderOpenPrice()-(OrderOpenPrice()-ssl)-Ask))
           {  
            s.tsl=NormalizeDouble(OrderOpenPrice()-((OrderOpenPrice()-ssl)-Ask),Digits);
            Print("s.tsl ",s.tsl);
            if (OrderStopLoss()>s.tsl)
              {
               while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
               OrderModify(s.ticketM,OrderOpenPrice(),s.tsl,OrderTakeProfit(),OrderExpiration(),MediumVioletRed);
               Sleep (2000);
              }
           }
        }
     }
   else // If Trail.Starts.After.BreakEven
     {
      if (b.ticket>0)
        {
         OrderSelect(b.ticket,SELECT_BY_TICKET);
         if (Bid>=(OrderOpenPrice()+(Move.To.BreakEven.at.pips*Point)))
           {
            bsl=Trail.Pips*Point;
            if (Bid>(OrderOpenPrice()+bsl) && OrderStopLoss()<(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl))))
              {  
               b.tsl=NormalizeDouble(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl)),Digits);
               Print("b.tsl ",b.tsl);
               if (OrderStopLoss()<b.tsl)
                 {  
                  while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
                  OrderModify(b.ticket,OrderOpenPrice(),b.tsl,OrderTakeProfit(),OrderExpiration(),MediumSpringGreen);
                  Sleep (2000);
                 }
              }
           }
        }
      if(s.ticket>0)
        {  
         OrderSelect(s.ticket,SELECT_BY_TICKET);
         if (Ask<=(OrderOpenPrice()-(Move.To.BreakEven.at.pips*Point)))
           {  
            ssl=Trail.Pips*Point;
            //determine if stoploss should be modified
            if(Ask<(OrderOpenPrice()-ssl) && OrderStopLoss()>(OrderOpenPrice()-(OrderOpenPrice()-ssl)-Ask))
              {  
               s.tsl=NormalizeDouble(OrderOpenPrice()-((OrderOpenPrice()-ssl)-Ask),Digits);
               Print("s.tsl ",s.tsl);
               if(OrderStopLoss()>s.tsl)
                 {  
                  while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
                  OrderModify(s.ticket,OrderOpenPrice(),s.tsl,OrderTakeProfit(),OrderExpiration(),MediumVioletRed);
                  Sleep (2000);
                 }
              }
           }
        }
      if (b.ticketM>0)
        {  
         OrderSelect(b.ticketM,SELECT_BY_TICKET);
         if (Bid>=(OrderOpenPrice()+(Move.To.BreakEven.at.pips*Point)))
           {  
            bsl=Trail.Pips*Point;
            if (Bid>(OrderOpenPrice()+bsl) && OrderStopLoss()<(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl))))
              {  
               b.tsl=NormalizeDouble(OrderOpenPrice()+(Bid-(OrderOpenPrice()+bsl)),Digits);
               Print("b.tsl ",b.tsl);
               if (OrderStopLoss()<b.tsl)
                 {  
                  while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
                  OrderModify(b.ticketM,OrderOpenPrice(),b.tsl,OrderTakeProfit(),OrderExpiration(),MediumSpringGreen);
                  Sleep (2000);
                 }
              }
           }
        }
      if(s.ticketM>0)
        {  
         OrderSelect(s.ticketM,SELECT_BY_TICKET);
         if (Ask<=(OrderOpenPrice()-(Move.To.BreakEven.at.pips*Point)))
           {  
            ssl=Trail.Pips*Point;
            //determine if stoploss should be modified
            if(Ask<(OrderOpenPrice()-ssl) && OrderStopLoss()>(OrderOpenPrice()-(OrderOpenPrice()-ssl)-Ask))
              {  
               s.tsl=NormalizeDouble(OrderOpenPrice()-((OrderOpenPrice()-ssl)-Ask),Digits);
               Print("s.tsl ",s.tsl);
               if(OrderStopLoss()>s.tsl)
                 {  
                  while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
                  OrderModify(s.ticketM,OrderOpenPrice(),s.tsl,OrderTakeProfit(),OrderExpiration(),MediumVioletRed);
                  Sleep (2000);
                 }
              }
           }
        }
     }
  }
//----
void Trail.Stop()
  {  
   if (Trail.Starts.After.BreakEven)   Trail.With.Standard.Trailing(1);
   else                                Trail.With.Standard.Trailing(0);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void MoveToBreakEven()
  {  
   PosCounter();
   RefreshRates();
   if (b.ticket > 0)
     {  
      OrderSelect(b.ticket,SELECT_BY_TICKET);
      if (OrderStopLoss()<OrderOpenPrice())
        {  
         if (Bid >((Move.To.BreakEven.at.pips*Point) +OrderOpenPrice()))
           {  
            while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
            OrderModify(b.ticket, OrderOpenPrice(), (OrderOpenPrice()+(Move.To.BreakEven.Lock.pips*Point)),OrderTakeProfit(),OrderExpiration(),MediumSpringGreen);
            if (OrderSelect(b.ticket,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Long StopLoss Moved to BE at : ",OrderStopLoss());
               Sleep(2000);
              }
            else Print("Error moving Long StopLoss to BE: ",GetLastError());
           }
        }
     }
   if (s.ticket > 0)
     {  
      OrderSelect(s.ticket,SELECT_BY_TICKET);
      if (OrderStopLoss()>OrderOpenPrice())
        {  
         if(Ask < (OrderOpenPrice()-(Move.To.BreakEven.at.pips*Point)))
           {  
            while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
            OrderModify(OrderTicket(), OrderOpenPrice(), (OrderOpenPrice()-(Move.To.BreakEven.Lock.pips*Point)),OrderTakeProfit(),OrderExpiration(),MediumVioletRed);
            if(OrderSelect(s.ticket,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Short StopLoss Moved to BE at : ",OrderStopLoss());
               Sleep(2000);
              }
            else Print("Error moving Short StopLoss to BE: ",GetLastError());
           }
        }
     }
   if (b.ticketM > 0)
     {  
      OrderSelect(b.ticketM,SELECT_BY_TICKET);
      if (OrderStopLoss()<OrderOpenPrice())
        {  
         if (Bid >((Move.To.BreakEven.at.pips*Point) +OrderOpenPrice()))
           {  
            while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
            OrderModify(b.ticketM, OrderOpenPrice(), (OrderOpenPrice()+(Move.To.BreakEven.Lock.pips*Point)),OrderTakeProfit(),OrderExpiration(),MediumSpringGreen);
            if (OrderSelect(b.ticketM,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Long StopLoss Moved to BE at : ",OrderStopLoss());
               Sleep(2000);
              }
            else Print("Error moving Long StopLoss to BE: ",GetLastError());
           }
        }
     }
   if (s.ticketM > 0)
     {  
      OrderSelect(s.ticketM,SELECT_BY_TICKET);
      if (OrderStopLoss()>OrderOpenPrice())
        {  
         if(Ask < (OrderOpenPrice()-(Move.To.BreakEven.at.pips*Point)))
           {  
            while(Retry<5 && !IsTradeAllowed()) {Retry++; Sleep(2000); }
            OrderModify(OrderTicket(), OrderOpenPrice(), (OrderOpenPrice()-(Move.To.BreakEven.Lock.pips*Point)),OrderTakeProfit(),OrderExpiration(),MediumVioletRed);
            if(OrderSelect(s.ticketM,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Short StopLoss Moved to BE at : ",OrderStopLoss());
               Sleep(2000);
              }
            else Print("Error moving Short StopLoss to BE: ",GetLastError());
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CheckInitialSLTP()
  {
   int sl,tp;
   RefreshRates();
   if (b.ticket>0)
     {  
      OrderSelect(b.ticket,SELECT_BY_TICKET);
      if (OrderStopLoss()==0 || OrderTakeProfit()==0)
        {  
         if (OrderStopLoss  ()==0)  {sl=StopLoss.Pips;}
         if (OrderTakeProfit()==0)  {tp=TakeProfit.Pips;}
         if ((sl>0 && OrderStopLoss()==0) || (tp>0 && OrderTakeProfit()==0))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(2000); }
            OrderModify(b.ticket, OrderOpenPrice(), OrderOpenPrice()-sl*Point,OrderOpenPrice()+tp*Point,OrderExpiration(),MediumSpringGreen);
            if (OrderSelect(b.ticket,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Initial SL or TP is Set for Long Entry");
               Sleep(2000);
              }
            else Print("Error setting initial SL or TP for Long Entry");
           }
        }
     }
   if (s.ticket > 0)
     {  
      OrderSelect(s.ticket,SELECT_BY_TICKET);
      if (OrderStopLoss()==0 || OrderTakeProfit()==0)
        {  
         if (OrderStopLoss  ()==0)  {sl=StopLoss.Pips;}
         if (OrderTakeProfit()==0)  {tp=TakeProfit.Pips;}
         if ((sl>0 && OrderStopLoss()==0) || (tp>0 && OrderTakeProfit()==0))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(2000); }
            OrderModify(s.ticket, OrderOpenPrice(), OrderOpenPrice()+sl*Point,OrderOpenPrice()-tp*Point,OrderExpiration(),MediumVioletRed);
            if (OrderSelect(s.ticket,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Initial SL or TP is Set for Short Entry");
               Sleep(2000);
              }
            else Print("Error setting initial SL or TP for Short Entry");
           }
        }
     }
   if (b.ticketM>0)
     {  
      OrderSelect(b.ticketM,SELECT_BY_TICKET);
      if (OrderStopLoss()==0 || OrderTakeProfit()==0)
        {  
         if (OrderStopLoss  ()==0)  {sl=StopLoss.Pips;}
         if (OrderTakeProfit()==0)  {tp=TakeProfit.Pips;}
         if ((sl>0 && OrderStopLoss()==0) || (tp>0 && OrderTakeProfit()==0))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(2000); }
            OrderModify(b.ticketM, OrderOpenPrice(), OrderOpenPrice()-sl*Point,OrderOpenPrice()+tp*Point,OrderExpiration(),MediumSpringGreen);
            if (OrderSelect(b.ticketM,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Initial SL or TP is Set for Long Entry");
               Sleep(2000);
              }
            else Print("Error setting initial SL or TP for Long Entry");
           }
        }
     }
   if (s.ticketM > 0)
     {  
      OrderSelect(s.ticketM,SELECT_BY_TICKET);
      if (OrderStopLoss()==0 || OrderTakeProfit()==0)
        {  
         if (OrderStopLoss  ()==0)  {sl=StopLoss.Pips;}
         if (OrderTakeProfit()==0)  {tp=TakeProfit.Pips;}
         if ((sl>0 && OrderStopLoss()==0) || (tp>0 && OrderTakeProfit()==0))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(2000); }
            OrderModify(s.ticketM, OrderOpenPrice(), OrderOpenPrice()+sl*Point,OrderOpenPrice()-tp*Point,OrderExpiration(),MediumVioletRed);
            if (OrderSelect(s.ticketM,SELECT_BY_TICKET,MODE_TRADES))
              {  
               Print("Initial SL or TP is Set for Short Entry");
               Sleep(2000);
              }
            else Print("Error setting initial SL or TP for Short Entry");
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void ShutDownAllTrades(int aiWhat)
  {  
   int Retry;
   for(int cnt=OrdersTotal()-1;cnt>=0;cnt--)
     {  
      OrderSelect(cnt, SELECT_BY_POS, MODE_TRADES);
      if(((ShutDown.Current.Pair.Only) && (OrderSymbol()==Symbol())) || (!ShutDown.Current.Pair.Only))
        {  
         if ((OrderType()==OP_BUY) && ((aiWhat==0) || (aiWhat==1) || (aiWhat==2)))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(1000); }
            OrderClose(OrderTicket(),OrderLots(),Bid,Slippage,Yellow);
            Sleep(1000);
           }
         if ((OrderType()==OP_SELL) && ((aiWhat==0) || (aiWhat==1) || (aiWhat==3)))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(1000); }
            OrderClose(OrderTicket(),OrderLots(),Ask,Slippage,Yellow);
            Sleep(1000);
           }
         if ((OrderType()==OP_BUYSTOP) && ((aiWhat==0) || (aiWhat==4) || (aiWhat==5)))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(1000); }
            OrderDelete(OrderTicket());
            Sleep(1000);
           }
         if ((OrderType()==OP_SELLSTOP) && ((aiWhat==0) || (aiWhat==4) || (aiWhat==6)))
           {  
            Retry=0; while(Retry<5 && !IsTradeAllowed()) { Retry++; Sleep(1000); }
            OrderDelete(OrderTicket());
            Sleep(1000);
           }
        }
     }
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void comments()
  {  
   string s0="", s1="", s2="", s3="", swap="", sCombo="", sStr ;
   int PipsProfit;
   double AmountProfit;
   PipsProfit=0; AmountProfit=0;
   ObjectSetText( "Yannis", "jsfero@otenet.gr"  , 10, "Times New Roman", Yellow );
   ObjectSetText( "Yannis2", NewsLabel , 10, "Tahoma", LightSkyBlue );
   PosCounter();
   RefreshRates();
   if (b.ticket>0)
     {  
      OrderSelect(b.ticket,SELECT_BY_TICKET);
      PipsProfit=NormalizeDouble(((Bid - OrderOpenPrice())/Point),Digits);
      AmountProfit=OrderProfit();
     }
   else if (s.ticket>0)
        {  
         OrderSelect(s.ticket,SELECT_BY_TICKET);
         PipsProfit=NormalizeDouble(((OrderOpenPrice()-Ask)/Point),Digits);
         AmountProfit=OrderProfit();
        }
      else if (b.ticketM>0)
           {  
            OrderSelect(b.ticketM,SELECT_BY_TICKET);
            PipsProfit=NormalizeDouble(((Bid - OrderOpenPrice())/Point),Digits);
            AmountProfit=OrderProfit();
           }
         else if (s.ticketM>0)
              {  
               OrderSelect(s.ticketM,SELECT_BY_TICKET);
               PipsProfit=NormalizeDouble(((OrderOpenPrice()-Ask)/Point),Digits);
               AmountProfit=OrderProfit();
              }
   if (Move.To.BreakEven.at.pips>0) s1="S/L will move to B/E after: "+Move.To.BreakEven.at.pips+" pips   and lock: "+Move.To.BreakEven.Lock.pips+" pips"+"\n\n";
   else                             s1="";
   if ((!Place.Straddle.Immediately) && (Event.Start.Hour!=0) && (Event.Start.Minutes!=0))
     {  
      if (Adjust.Pending.Orders) s2=StringConcatenate("A straddle will be placed ",DoubleToStr(Pre.Event.Entry.Minutes,0),
                                                          " Minutes before news \nat ",DoubleToStr(Distance.From.Price,0),
                                                          " pips above and below price \nAdjusting every minute until ",
                                                          DoubleToStr(Stop.Adjusting.Min.Before.Event,0),
                                                          " minutes before event time");
     }
   else s2="";
//----
   Comment( "\n",ScreenComment,"\n\n",
            "SL: ",StopLoss.Pips,"  TP:",TakeProfit.Pips,"  Trail:",Trail.Pips,"\n",
            s1,"\n",
            "Minimum allowed for SL & TP is ",iMinimum," pips\n\n",
            s2
          );
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
string ServerTime()
  {  
   string strHourPad,strMinutePad;
   if (TimeHour(iTime(NULL,PERIOD_M1,0))>=0 && TimeHour(iTime(NULL,PERIOD_M1,0))<=9) strHourPad="0";
   else strHourPad="";
   if (TimeMinute(iTime(NULL,PERIOD_M1,0))>=0 && TimeMinute(iTime(NULL,PERIOD_M1,0))<=9) strMinutePad="0";
   else strMinutePad="";
   return(StringConcatenate("Broker\'s Current Time is (",strHourPad,TimeHour(iTime(NULL,PERIOD_M1,0)),":",strMinutePad,TimeMinute(iTime(NULL,PERIOD_M1,0)),")"));
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int CalcMagic(string CurrPair, int CurrPeriod)
  {       
   if (CurrPair=="EURUSD" || CurrPair=="EURUSDm") {return(1000+CurrPeriod);}
   else if (CurrPair=="GBPUSD" || CurrPair=="GBPUSDm") {return(2000+CurrPeriod);}
      else if (CurrPair=="USDCHF" || CurrPair=="USDCHFm") {return(3000+CurrPeriod);}
         else if (CurrPair=="USDJPY" || CurrPair=="USDJPYm") {return(4000+CurrPeriod);}
            else if (CurrPair=="EURJPY" || CurrPair=="EURJPYm") {return(5000+CurrPeriod);}
               else if (CurrPair=="EURCHF" || CurrPair=="EURCHFm") {return(6000+CurrPeriod);}
                  else if (CurrPair=="EURGBP" || CurrPair=="EURGBPm") {return(7000+CurrPeriod);}
                     else if (CurrPair=="USDCAD" || CurrPair=="USDCADm") {return(8000+CurrPeriod);}
                        else if (CurrPair=="AUDUSD" || CurrPair=="AUDUSDm") {return(9000+CurrPeriod);}
                           else if (CurrPair=="GBPCHF" || CurrPair=="GBPCHFm") {return(10000+CurrPeriod);}
                              else if (CurrPair=="GBPJPY" || CurrPair=="GBPJPYm") {return(11000+CurrPeriod);}
                                 else if (CurrPair=="CHFJPY" || CurrPair=="CHFJPYm") {return(12000+CurrPeriod);}
                                    else if (CurrPair=="NZDUSD" || CurrPair=="NZDUSDm") {return(13000+CurrPeriod);}
                                       else if (CurrPair=="EURCAD" || CurrPair=="EURCADm") {return(14000+CurrPeriod);}
                                          else if (CurrPair=="AUDJPY" || CurrPair=="AUDJPYm") {return(15000+CurrPeriod);}
                                             else if (CurrPair=="EURAUD" || CurrPair=="EURAUDm") {return(16000+CurrPeriod);}
                                                else if (CurrPair=="AUDCAD" || CurrPair=="AUDCADm") {return(17000+CurrPeriod);}
                                                   else if (CurrPair=="AUDNZD" || CurrPair=="AUDNZDm") {return(18000+CurrPeriod);}
                                                      else if (CurrPair=="NZDJPY" || CurrPair=="NZDJPYm") {return(19000+CurrPeriod);}
                                                         else if (CurrPair=="CADJPY" || CurrPair=="CADJPYm") {return(20000+CurrPeriod);}
                                                            else if (CurrPair=="XAUUSD" || CurrPair=="XAUUSDm") {return(21000+CurrPeriod);}
                                                               else if (CurrPair=="XAGUSD" || CurrPair=="XAGUSDm") {return(22000+CurrPeriod);}
                                                                  else if (CurrPair=="GBPAUD" || CurrPair=="GBPAUDm") {return(23000+CurrPeriod);}
                                                                     else if (CurrPair=="GBPCAD" || CurrPair=="GBPCADm") {return(24000+CurrPeriod);}
                                                                        else if (CurrPair=="AUFCHF" || CurrPair=="AUFCHFm") {return(25000+CurrPeriod);}
                                                                           else if (CurrPair=="CADCHF" || CurrPair=="CADCHFm") {return(26000+CurrPeriod);}
                                                                              else if (CurrPair=="NZDCHF" || CurrPair=="NZDCHFm") {return(27000+CurrPeriod);}
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
int YannisCustomText( string Text, int xOffset, int yOffset,int iCorner)
  {  
   ObjectCreate(Text,OBJ_LABEL         , 0, 0, 0 );
   ObjectSet   (Text,OBJPROP_CORNER    , iCorner);
   ObjectSet   (Text,OBJPROP_XDISTANCE , xOffset );
   ObjectSet   (Text,OBJPROP_YDISTANCE , yOffset );
   ObjectSet   (Text,OBJPROP_BACK      , True );
  }
//+------------------------------------------------------------------+