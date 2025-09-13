//+------------------------------------------------------------------+
//| PZ_ReversalTrendFollowingEA.mq4
//| --
//| You can either be long or short at any given time. You buy if the market 
//| makes a new 100-day high and sell if the market makes a new 100-day low. 
//| You are always in the market and break-even all trades as soon as possible.
//+------------------------------------------------------------------+
#property copyright "Copyright © http://www.pointzero-trading.com"
#property link      "http://www.pointzero-trading.com"

//---- Dependencies
#import "stdlib.ex4"
   string ErrorDescription(int e);
#import

//---- Constants
#define  ShortName         "PZ Reversal Trend Following EA"
#define  Shift             1

//---- External variables
extern string TR_Ex                    = "------- Trade period";
extern int    TradingPeriod            = 100;
extern string MM_Ex                    = "------- Money management";
extern bool   MoneyManagement          = true;
extern double RiskPercent              = 2;
extern double LotSize                  = 0.1;
extern string EA_Ex                    = "------- EA Settings";
extern int    Slippage                 = 6;
extern int    MagicNumber              = 2000;

//---- Internal
double   DecimalPip;
double   PBuy = EMPTY_VALUE;
double   PSell = EMPTY_VALUE;

//+------------------------------------------------------------------+
//| Custom EA initialization function
//+------------------------------------------------------------------+
int init()
{
   DecimalPip = GetDecimalPip();
   Comment("Copyright © http://www.pointzero-trading.com");
   return(0);
}

//+------------------------------------------------------------------+
//| Custom EA deinit function
//+------------------------------------------------------------------+
int deinit()
{
   return(0);
}

//+------------------------------------------------------------------+
//| Custom EA start function
//+------------------------------------------------------------------+
int start()
{   
   //--
   //-- Trade if trigger price is breached
   //--
   
   // Buy
   if(PBuy != EMPTY_VALUE && Bid > PBuy)
   { 
      PlaceOrder(OP_BUY, GetLotSize()); 
      PBuy = EMPTY_VALUE;
   }
   
   // Sell
   if(PSell != EMPTY_VALUE && Ask < PSell) 
   { 
      PlaceOrder(OP_SELL, GetLotSize()); 
      PSell = EMPTY_VALUE; 
   }
   
   //--
   //-- Check entry
   //--
   
   // Do not continue unless the bar is closed
   if(!IsBarClosed(0, true)) return(0);
   
   // Trading thresholds
   double rhigh  = iHigh(Symbol(), Period(), iHighest(Symbol(), Period(), MODE_HIGH, TradingPeriod, Shift+1));
   double rlow   = iLow(Symbol(), Period(), iLowest(Symbol(), Period(), MODE_LOW, TradingPeriod, Shift+1));
                        
   // Trades opened
   int l_TotalTrades_buy = GetTotalTrades(OP_BUY, MagicNumber);
   int l_TotalTrades_sell = GetTotalTrades(OP_SELL, MagicNumber);
  
   // Bars
   double CLOSE = iClose(Symbol(),0, Shift);
   double HIGH = iHigh(Symbol(),0, Shift);
   double LOW = iLow(Symbol(),0, Shift);
   
   // Check if buy conditions apply
   if(CLOSE > rhigh && l_TotalTrades_buy == 0)
   {
      // Buy
      PBuy = HIGH;
      CloseOrder(OP_SELL);
   }
   
   // Check if sell conditions apply
   if(CLOSE < rlow && l_TotalTrades_sell == 0)
   {
      // Sell
      PSell = LOW;
      CloseOrder(OP_BUY);
   }
   
   return (0);
}

//+------------------------------------------------------------------+
//| My functions
//+------------------------------------------------------------------+

/**
* Returns total opened trades
* @param    int   Type
* @param    int   Magic
* @return   int
*/
int GetTotalTrades(int Type, int Magic)
{
   int counter = 0;
   for (int i = OrdersTotal() - 1; i >= 0; i--)
   {
      if (!OrderSelect(i, SELECT_BY_POS, MODE_TRADES))
      {
        Print(ShortName +" (OrderSelect Error) "+ ErrorDescription(GetLastError()));
      } else if(OrderSymbol() == Symbol() && (OrderType() == Type || Type == EMPTY_VALUE) && OrderMagicNumber() == Magic) {
            counter++;
      }
   }
   return(counter);
}

/**
* Places an order
* @param    int      Type
* @param    double   Lotz
* @param    double   PendingPrice
*/
void PlaceOrder(int Type, double Lotz, double PendingPrice = 0)
{
   int err;
   color  l_color;
   double l_price, l_sprice = 0;
   RefreshRates();
   
   // Price and color for the trade type
   if(Type == OP_BUY){ l_price = Ask;  l_color = Blue; }
   if(Type == OP_SELL){ l_price = Bid; l_color = Red; } 
   
   // Avoid collusions
   while (IsTradeContextBusy()) Sleep(1000);
   int l_datetime = TimeCurrent();
   
   // Send order
   int l_ticket = OrderSend(Symbol(), Type, Lotz, l_price, Slippage, 0, 0, "", MagicNumber, 0, l_color);
   
   // Rety if failure
   if (l_ticket == -1)
   {
      while(l_ticket == -1 && TimeCurrent() - l_datetime < 60 && !IsTesting())
      {
         err = GetLastError();
         if (err == 148) return;
         Sleep(1000);
         while (IsTradeContextBusy()) Sleep(1000);
         RefreshRates();
         l_ticket = OrderSend(Symbol(), Type, Lotz, l_price, Slippage, 0, 0, "", MagicNumber, 0, l_color);
      }
      if (l_ticket == -1)
         Print(ShortName +" (OrderSend Error) "+ ErrorDescription(GetLastError()));
   }
}

/**
* Closes desired orders 
* @param    int   Type
*/
void CloseOrder(int Type)
{
   int l_type;
	for(int i = OrdersTotal()-1; i >= 0; i--)
	{
		OrderSelect(i, SELECT_BY_POS, MODE_TRADES); l_type = OrderType();
		if(OrderSymbol() == Symbol() && OrderMagicNumber() == MagicNumber && Type == l_type)
		{ 
	      if(Type == OP_BUY || Type == OP_SELL)  
	      {
            if(!OrderClose(OrderTicket(), OrderLots(), OrderClosePrice(), Slippage, Gold))
               Print(ShortName +" (OrderClose Error) "+ ErrorDescription(GetLastError()));
         } else {
            if(!OrderDelete(OrderTicket()))
               Print(ShortName +" (OrderDelete Error) "+ ErrorDescription(GetLastError()));
         }
      }
   }
}

/**
* Calculates lot size according to risk and the weight of this trade
* @return   double
*/
double GetLotSize()
{
   // Lots
   double l_lotz = LotSize;
   
   // Lotsize and restrictions 
   double l_minlot = MarketInfo(Symbol(), MODE_MINLOT);
   double l_maxlot = MarketInfo(Symbol(), MODE_MAXLOT);
   double l_lotstep = MarketInfo(Symbol(), MODE_LOTSTEP);
   int vp = 0; if(l_lotstep == 0.01) vp = 2; else vp = 1;
   
   // Apply money management
   if(MoneyManagement == true)
      l_lotz = MathFloor(AccountBalance() * RiskPercent / 100.0) / 1000.0;
  
   // Normalize to lotstep
   l_lotz = NormalizeDouble(l_lotz, vp);
   
   // Check max/minlot here
   if (l_lotz < l_minlot) l_lotz = l_minlot;
   if(l_lotz > l_maxlot) l_lotz = l_maxlot; 
   
   // Bye!
   return (l_lotz);
}

/**
* Returns decimal pip value
* @return   double
*/
double GetDecimalPip()
{
   switch(Digits)
   {
      case 5: return(0.0001);
      case 4: return(0.0001);
      case 3: return(0.001);
      default: return(0.01);
   }
}

/**
* Checks if the bar has closed
*/
bool IsBarClosed(int timeframe,bool reset)
{
    static datetime lastbartime;
    if(timeframe==-1)
    {
        if(reset)
            lastbartime=0;
        else
            lastbartime=iTime(NULL,timeframe,0);
        return(true);
    }
    if(iTime(NULL,timeframe,0)==lastbartime) // wait for new bar
        return(false);
    if(reset)
        lastbartime=iTime(NULL,timeframe,0);
    return(true);
}