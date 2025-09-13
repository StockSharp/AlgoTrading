//+------------------------------------------------------------------+
//|                                            SARTrailingSystem.mq5 |
//|                                        Copyright 2016, Oschenker |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright  "Copyright 2016, Oschenker"
#property link       "https://www.mql5.com"
#property version    "1.00"

input int            TimerTime=300;        // Timer frequency (sec)
input int            StopLoss = 10;       // Initial Stop-loss (points)
input double         AFStep = 0.02;       // SAR acceleration factor increment step
input double         AFCap = 0.2;         // SAR acceleration factor max. value
input bool           RandomTrade = true;  // Random trade toggle

int                  TradeValue;
long                 StopLevel;
datetime             BarTime;
double               StopLossLevel;
double               EP;
double               AF;
double               delta;
ENUM_POSITION_TYPE   Trend;

bool                 ChangeFlag;
bool                 TimerFlag = false;

MqlTradeRequest      Request = {0};
MqlTradeResult       Results = {0};
MqlRates             Price[];

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   // check the minimum stop level
   StopLevel = SymbolInfoInteger(Symbol(), SYMBOL_TRADE_STOPS_LEVEL);
   // Initialize the generator of random numbers 
   MathSrand(int(TimeLocal())); 
   // Initialize timer
   if(EventSetTimer(TimerTime)) Print("Timer Setup to ", TimerTime, " sec.");
   else Print("Timer Error ", GetLastError());   
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   // Destroy timer
   EventKillTimer();
   // remove comments, if any
   ChartSetString( 0, CHART_COMMENT, "");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   Print("Новый тик");
   if(CopyRates(Symbol(), 0, 0, 1, Price) > 0)
     {
      //check if there is an open position
      if(PositionSelect(Symbol()))
         {
          // Check position SL
          if(PositionGetDouble(POSITION_SL, StopLossLevel))
            {
             // Initialize SAR variables
             if(EP == 0) EP = Price[0].close;
             if(AF == 0) AF = AFStep;
             // check if new bar is started to modify SL
             if(BarTime != (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE))
                 {
                  BarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
                  // Check the position direction 
                  Trend = (ENUM_POSITION_TYPE)PositionGetInteger(POSITION_TYPE);
                  switch(Trend)
                     {
                      case POSITION_TYPE_BUY:
                           if(Price[0].high > EP)
                              {
                               AF = fmin(AFCap, AF + AFStep);
                               EP = Price[0].high;
                              }
                           delta += AF * (EP - StopLossLevel);
                           ChangeFlag = false;
                           if(delta > _Point)
                              {
                               StopLossLevel += delta;
                               delta = 0;
                               ChangeFlag = (Price[0].close - StopLossLevel) > StopLevel * _Point;
                              }
                           break;
                      case POSITION_TYPE_SELL:
                           if(Price[0].low < EP)
                                 {
                                  AF = fmin(AFCap, AF + AFStep);
                                  EP = Price[0].low;
                                 }
                           delta += AF * (StopLossLevel - EP);
                           ChangeFlag = false;
                           if(delta >  _Point)
                              {
                               StopLossLevel -= delta;
                               delta = 0;
                               ChangeFlag = (StopLossLevel - Price[0].close) > StopLevel * _Point;
                              }
                           break;
                     }
                  // modifying SL according to previous calculations
                  if(ChangeFlag)
                    {
                     Request.action = TRADE_ACTION_SLTP;
                     Request.symbol = Symbol();
                     Request.sl = StopLossLevel;
                     Request.position = PositionGetInteger(POSITION_TICKET);
                     if(OrderSend(Request, Results)) Print("Stop Loss has changed", StopLossLevel);
                    }
                 }
            }
          else   Print("Position has no SL");
         }
      else
         {
          if(RandomTrade && TimerFlag)
           {
            ZeroMemory(Request);
            Request.action = TRADE_ACTION_DEAL;
            Request.symbol = Symbol();
            Request.volume = 1;
            Request.deviation = 3;
            if(rand() < 16384)
               {
                Request.price = SymbolInfoDouble(Symbol(), SYMBOL_ASK);
                Request.sl = SymbolInfoDouble(Symbol(), SYMBOL_BID) - _Point * StopLoss;
                Request.type = ORDER_TYPE_BUY;
                Trend = POSITION_TYPE_BUY;
               }
            else
               {
                Request.price = SymbolInfoDouble(Symbol(), SYMBOL_BID);
                Request.sl = SymbolInfoDouble(Symbol(), SYMBOL_ASK) + _Point * StopLoss;
                Request.type = ORDER_TYPE_SELL;
                Trend = POSITION_TYPE_SELL;
               }
            if(CheckMargin(Symbol(), Request.volume, Request.type, Request.price))
               {
                if(OrderSend(Request, Results))
                  {
                   Print("Trade is successful");
                   AF = AFStep;
                   EP = 0;
                   delta = 0;
                   TimerFlag = 0;
                  }
               }
           }
         }
     }
  }
//+------------------------------------------------------------------+
//| Expert Check Margin function                                     |
//+------------------------------------------------------------------+
bool CheckMargin(string          symb,
                 double          lots,
                 ENUM_ORDER_TYPE type,
                 double          price)
  {
   //check marging available and neccessary marging
   double margin,free_margin=AccountInfoDouble(ACCOUNT_MARGIN_FREE);
   if(!OrderCalcMargin(type, symb, lots, price, margin))
        {
         Print("Error in ",__FUNCTION__," code=",GetLastError()); 
         return(false);
        }
   // case of insufficient margin
   if(margin > free_margin)
        {
         Print("Not enough money for ",EnumToString(type)," ",lots," ",symb," Error code=",GetLastError());
         return(false);
        }
   return(true);
  }
  
//+------------------------------------------------------------------+
//| Trade function                                                   |
//+------------------------------------------------------------------+
void OnTrade()
  {
//---
   
  }
//+------------------------------------------------------------------+
//| TradeTransaction function                                        |
//+------------------------------------------------------------------+
void OnTradeTransaction(const MqlTradeTransaction& trans,
                        const MqlTradeRequest& request,
                        const MqlTradeResult& result)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Tester function                                                  |
//+------------------------------------------------------------------+
double OnTester()
  {
//---
   double ret=0.0;
//---

//---
   return(ret);
  }
//+------------------------------------------------------------------+
//| Timer function                                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   TimerFlag = true;
  }

//+------------------------------------------------------------------+
