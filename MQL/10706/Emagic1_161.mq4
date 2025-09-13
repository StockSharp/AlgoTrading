#define SIGNAL_NONE 0
#define SIGNAL_BUY   1
#define SIGNAL_SELL  2
#define SIGNAL_CLOSEBUY 3
#define SIGNAL_CLOSESELL 4

#property copyright "Gunst"


extern int     MagicNumber = 12321;
extern bool    SignalMail = True;
extern string  MM  ="Auto money management";
extern bool    AutoMoneyManagement = true;
extern double  PercentToRisk = 3;
extern double  MaxLots = 999;
extern double  MinLots = 0.01;
extern string  NoMM  ="when AutoMM is off, use lotsize below.";
extern double  Lots = 0.01;
extern int     Slippage = 3;
extern bool    UseStopLoss = True;
extern int     StopLoss = 40;
extern bool    UseTakeProfit = False;
extern int     TakeProfit = 60;
extern bool    UseTrailingStop = True;
extern int     TrailingStop = 11;
extern string  Macd = "Quick, Slow, Signal";
extern int     Qema = 10;
extern int     Sema = 32;
extern int     Signalmacd = 4;
extern string  Ema = "fast close, slow open.";
extern int     Fastema = 8;
extern int     Slowema = 13;
double         point = 0;

int BarCount;
int Current;
bool TickCheck = False;

bool EachTickMode = false;
//+------------------------------------------------------------------+
//| expert initialization function                                   |
//+------------------------------------------------------------------+
int init() {
  
  
  if (Digits == 3 || Digits == 5)
{
point = Point*10; 
} 
else
{
point = Point;
} 
  
   BarCount = Bars;

   if (EachTickMode) Current = 0; else Current = 1;

   return(0);
}
//+------------------------------------------------------------------+
//| expert deinitialization function                                 |
//+------------------------------------------------------------------+
int deinit() {
   return(0);
}
//+------------------------------------------------------------------+
//| expert start function                                            |
//+------------------------------------------------------------------+

int start() {



   int Order = SIGNAL_NONE;
   int Total, Ticket;
   double StopLossLevel, TakeProfitLevel;

//*******************************************************************************************************    
   double Risk = PercentToRisk / 100;
        if (AutoMoneyManagement)
        Lots = NormalizeDouble(AccountBalance()*Risk/StopLoss/(MarketInfo(Symbol(), MODE_TICKVALUE)),2);
        if (Lots >= MaxLots)
        Lots = (MaxLots);
        if (Lots <= MinLots)
        Lots = (MinLots);
//*******************************************************************************************************

  
      



   if (EachTickMode && Bars != BarCount) TickCheck = False;
   Total = OrdersTotal();
   Order = SIGNAL_NONE;

   //+------------------------------------------------------------------+
   //| Variable Begin                                                   |
   //+------------------------------------------------------------------+


double Buy1_1 = iMA(NULL, 0, Fastema, 0, MODE_EMA, PRICE_CLOSE, Current + 0);
double Buy1_2 = iMA(NULL, 0, Slowema, 0, MODE_EMA, PRICE_OPEN, Current + 0);
double Buy2_1 = iMACD(NULL, 0, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 1);
double Buy2_2 = iMACD(NULL, 0, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 0);

double Sell1_1 = iMA(NULL, 0, Fastema, 0, MODE_EMA, PRICE_CLOSE, Current + 0);
double Sell1_2 = iMA(NULL, 0, Slowema, 0, MODE_EMA, PRICE_OPEN, Current + 0);
double Sell2_1 = iMACD(NULL, 0, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 0);
double Sell2_2 = iMACD(NULL, 0, Qema, Sema, Signalmacd, PRICE_CLOSE, MODE_SIGNAL, Current + 1);




   
   //+------------------------------------------------------------------+
   //| Variable End                                                     |
   //+------------------------------------------------------------------+

   //Check position
   bool IsTrade = False;

   for (int i = 0; i < Total; i ++) {
      OrderSelect(i, SELECT_BY_POS, MODE_TRADES);
      
      if ( OrderMagicNumber() != MagicNumber ) continue;
      
      if(OrderType() <= OP_SELL &&  OrderSymbol() == Symbol()) {
         IsTrade = True;
         if(OrderType() == OP_BUY) {
            //Close

            //+------------------------------------------------------------------+
            //| Signal Begin(Exit Buy)                                           |
            //+------------------------------------------------------------------+

            

            //+------------------------------------------------------------------+
            //| Signal End(Exit Buy)                                             |
            //+------------------------------------------------------------------+

            if (Order == SIGNAL_CLOSEBUY && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars != BarCount)))) {
               OrderClose(OrderTicket(), OrderLots(), Bid, Slippage, MediumSeaGreen);
               if (SignalMail) SendMail("[Signal Alert]", "[" + Symbol() + "] " + DoubleToStr(Bid, Digits) + " Close Buy");
               if (!EachTickMode) BarCount = Bars;
               IsTrade = False;
               continue;
            }
            //Trailing stop
            if(UseTrailingStop && TrailingStop > 0) {                 
               if(Bid - OrderOpenPrice() > point * TrailingStop) {
                  if(OrderStopLoss() < Bid - point * TrailingStop) {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Bid - point * TrailingStop, OrderTakeProfit(), 0, MediumSeaGreen);
                     if (!EachTickMode) BarCount = Bars;
                     continue;
                  }
               }
            }
         } else {
            //Close

            //+------------------------------------------------------------------+
            //| Signal Begin(Exit Sell)                                          |
            //+------------------------------------------------------------------+

            

            //+------------------------------------------------------------------+
            //| Signal End(Exit Sell)                                            |
            //+------------------------------------------------------------------+

            if (Order == SIGNAL_CLOSESELL && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars != BarCount)))) {
               OrderClose(OrderTicket(), OrderLots(), Ask, Slippage, DarkOrange);
               if (SignalMail) SendMail("[Signal Alert]", "[" + Symbol() + "] " + DoubleToStr(Ask, Digits) + " Close Sell");
               if (!EachTickMode) BarCount = Bars;
               IsTrade = False;
               continue;
            }
            //Trailing stop
            if(UseTrailingStop && TrailingStop > 0) {                 
               if((OrderOpenPrice() - Ask) > (point * TrailingStop)) {
                  if((OrderStopLoss() > (Ask + point * TrailingStop)) || (OrderStopLoss() == 0)) {
                     OrderModify(OrderTicket(), OrderOpenPrice(), Ask + point * TrailingStop, OrderTakeProfit(), 0, DarkOrange);
                     if (!EachTickMode) BarCount = Bars;
                     continue;
                  }
               }
            }
         }
      }
   }

   //+------------------------------------------------------------------+
   //| Signal Begin(Entry)                                              |
   //+------------------------------------------------------------------+

   if (Buy1_1 > Buy1_2 && Buy2_1 < Buy2_2) Order = SIGNAL_BUY;

   if (Sell1_1 < Sell1_2 && Sell2_1 < Sell2_2) Order = SIGNAL_SELL;


   //+------------------------------------------------------------------+
   //| Signal End                                                       |
   //+------------------------------------------------------------------+

   //Buy

   if (Order == SIGNAL_BUY && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars != BarCount)))) {
      if(!IsTrade) {
         //Check free margin
         if (AccountFreeMargin() < (1000 * Lots)) {
            Print("We have no money. Free Margin = ", AccountFreeMargin());
            return(0);
         }

         if (UseStopLoss) StopLossLevel = Ask - StopLoss * point; else StopLossLevel = 0.0;
         if (UseTakeProfit) TakeProfitLevel = Ask + TakeProfit * point; else TakeProfitLevel = 0.0;

         Ticket = OrderSend(Symbol(), OP_BUY, Lots, Ask, Slippage, StopLossLevel, TakeProfitLevel, "EmagicBuy" + MagicNumber, MagicNumber, 0, DodgerBlue);
         if(Ticket > 0) {
            if (OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES)) {
				Print("BUY order opened : ", OrderOpenPrice());
                if (SignalMail) SendMail("[Signal Alert]", "[" + Symbol() + "] " + DoubleToStr(Ask, Digits) + " Open Buy");
			} else {
				Print("Error opening BUY order : ", GetLastError());
			}
         }
         if (EachTickMode) TickCheck = True;
         if (!EachTickMode) BarCount = Bars;
         return(0);
      }
   }

   //Sell
   if (Order == SIGNAL_SELL && ((EachTickMode && !TickCheck) || (!EachTickMode && (Bars != BarCount)))) {
      if(!IsTrade) {
         //Check free margin
         if (AccountFreeMargin() < (1000 * Lots)) {
            Print("We have no money. Free Margin = ", AccountFreeMargin());
            return(0);
         }

         if (UseStopLoss) StopLossLevel = Bid + StopLoss * point; else StopLossLevel = 0.0;
         if (UseTakeProfit) TakeProfitLevel = Bid - TakeProfit * point; else TakeProfitLevel = 0.0;

         Ticket = OrderSend(Symbol(), OP_SELL, Lots, Bid, Slippage, StopLossLevel, TakeProfitLevel, "EmagicSell" + MagicNumber, MagicNumber, 0, DeepPink);
         if(Ticket > 0) {
            if (OrderSelect(Ticket, SELECT_BY_TICKET, MODE_TRADES)) {
				Print("SELL order opened : ", OrderOpenPrice());
                if (SignalMail) SendMail("[Signal Alert]", "[" + Symbol() + "] " + DoubleToStr(Bid, Digits) + " Open Sell");
			} else {
				Print("Error opening SELL order : ", GetLastError());
			}
         }
         if (EachTickMode) TickCheck = True;
         if (!EachTickMode) BarCount = Bars;
         return(0);
      }
   }

   if (!EachTickMode) BarCount = Bars;

   return(0);
}
//+------------------------------------------------------------------+