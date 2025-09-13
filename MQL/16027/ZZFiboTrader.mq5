//+------------------------------------------------------------------+
//|                                               ZZ Fibo Trader.mq5 |
//|                        Copyright 2016, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "(C) OSCHENKER, 2016"
#property link      "https://www.mql5.com"
#property version   "1.00"
#property icon "RedValue_70x70.ico"

input int       Breakout = 5;           // Level breakout depth
input int       StopLoss = 0;           // Additional SL buffer


input int       Retracement = 10;       // Typical retracement size
input int       MAPeriod    = 50;       // Moves averaging period
input int       Bound       = 70;       // Percentage difference
input color     LevelsColor = clrBlack; // Fibo levels color
input int       LevelsWidth = 1;        // Fibo levels width
input bool      Level23     = false;    // Show 23.6% level
input bool      Level38     = true;     // Show 38.2% level
input bool      Level50     = true;     // Show 50.0% level
input bool      Level61     = true;     // Show 61.8% level
input bool      Level76     = false;    // Show 76.4% level
input double    AFStep      = 0.01;     // SAR system step
input double    AFCap       = 0.4;      // SAR system cap

int             Fibo_Handle;
int             LevelIndex;
int             LevelsTotal;
int             TradeDirection;
int             RatesTotal;
int             PositionType;

long            PositionID;

double          EP;
double          AF;
double          StopLossLevel;
double          FiboPriceLeves[];
double          ZZLevel000[];
double          ZZLevel023[];
double          ZZLevel038[];
double          ZZLevel050[];
double          ZZLevel061[];
double          ZZLevel076[];
double          ZZLevel100[];
double          ZZLevel000_;

datetime        CrossBarTime;
datetime        CloseBarTime;
datetime        LastBarTime;

bool            LevelCross = false;

MqlRates             Price[];
MqlTradeRequest      Request = {0};
MqlTradeResult       Results = {0};
MqlTradeCheckResult  Check   = {0};

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   Comment("FiboTrader, (C) O'Schenker. 2016");
   // get indicator handle
   Fibo_Handle = iCustom( _Symbol, PERIOD_CURRENT, "Market//SimpleZZFibo",
                          Retracement,
                          MAPeriod,
                          Bound,
                          LevelsColor,
                          LevelsWidth,
                          Level23,
                          Level38,
                          Level50,
                          Level61,
                          Level76);
   if(Fibo_Handle > 0) Print("CustomIndicatorHandle ",Fibo_Handle);
   else return(-1);
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   IndicatorRelease(Fibo_Handle);
   // remove comments, if any
   ChartSetString( 0, CHART_COMMENT, "");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   //--- copying rates 
   RatesTotal = CopyRates(Symbol(), 0, 0, 3, Price);
   if(RatesTotal > 0) /// Proceeds in case price data is available
      {
       // Copy indicator buffers
       if(CopyBuffer(Fibo_Handle, 2, 0, 2, ZZLevel000) <= 0) Print("No Indicator data 2");
       if(CopyBuffer(Fibo_Handle, 3, 0, 2, ZZLevel023) <= 0) Print("No Indicator data 3");
       if(CopyBuffer(Fibo_Handle, 4, 0, 2, ZZLevel038) <= 0) Print("No Indicator data 4");
       if(CopyBuffer(Fibo_Handle, 5, 0, 2, ZZLevel050) <= 0) Print("No Indicator data 5");
       if(CopyBuffer(Fibo_Handle, 6, 0, 2, ZZLevel061) <= 0) Print("No Indicator data 6");
       if(CopyBuffer(Fibo_Handle, 7, 0, 2, ZZLevel076) <= 0) Print("No Indicator data 7");
       if(CopyBuffer(Fibo_Handle, 8, 0, 2, ZZLevel100) <= 0) Print("No Indicator data 8");

       ArraySetAsSeries(Price, true);        // set array as series
       ArraySetAsSeries(ZZLevel000, true);   // set array as series
       ArraySetAsSeries(ZZLevel023, true);   // set array as series
       ArraySetAsSeries(ZZLevel038, true);   // set array as series
       ArraySetAsSeries(ZZLevel050, true);   // set array as series
       ArraySetAsSeries(ZZLevel061, true);   // set array as series
       ArraySetAsSeries(ZZLevel076, true);   // set array as series
       ArraySetAsSeries(ZZLevel100, true);   // set array as series

       if(ZZLevel000[1] != ZZLevel000_)
         {
          ZZLevel000_ = ZZLevel000[1];
          LevelCross = false;
          LevelIndex = 0;
         }          

       // map Fibo price levels
       int i = 0;
       ArrayResize(FiboPriceLeves, i + 1);
       FiboPriceLeves[i] = ZZLevel000[1];
       if(Level23) {i++; ArrayResize(FiboPriceLeves, i + 1); FiboPriceLeves[i] = ZZLevel023[1];}
       if(Level38) {i++; ArrayResize(FiboPriceLeves, i + 1); FiboPriceLeves[i] = ZZLevel038[1];}
       if(Level50) {i++; ArrayResize(FiboPriceLeves, i + 1); FiboPriceLeves[i] = ZZLevel050[1];}
       if(Level61) {i++; ArrayResize(FiboPriceLeves, i + 1); FiboPriceLeves[i] = ZZLevel061[1];}
       if(Level76) {i++; ArrayResize(FiboPriceLeves, i + 1); FiboPriceLeves[i] = ZZLevel076[1];}
       i++;
       ArrayResize(FiboPriceLeves, i + 1);
       FiboPriceLeves[i] = ZZLevel100[1];
       LevelsTotal = i;
       
       //   Current position management - check current position status                                                                           |
       if(PositionSelect(Symbol()))
         {
          PositionID = PositionGetInteger(POSITION_IDENTIFIER);

          // check if new bar is started to modify SL
          datetime BarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
          if(BarTime != LastBarTime)
            {
             LastBarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
             // move SL according to parabolic SAR system  
             switch(PositionType)
               {
                case 1: // PositionType is Buy
                  if(Price[0].high > EP)
                        {
                         AF = fmin(AFCap, AF + AFStep);
                         EP = Price[0].high;
                        }
                  StopLossLevel = StopLossLevel + AF * (EP - StopLossLevel);
                  break;

                case -1: // PositionType is Sell
                  if(Price[0].low < EP)
                        {
                         AF = fmin(AFCap, AF + AFStep);
                         EP = Price[0].low;
                        }
                  StopLossLevel = StopLossLevel - AF * (StopLossLevel - EP);
                  break;
               }
             Request.action = TRADE_ACTION_SLTP;
             Request.symbol = Symbol();
             Request.sl = StopLossLevel; //move SL as parabolic StopLossLevel;
             Request.position = PositionGetInteger(POSITION_TICKET);
             if(OrderCheck(Request, Check) && Check.retcode == 0)
               {
                if(!OrderSend(Request, Results) || Results.retcode != 10009) Print("Fail to modify SL");
               }
             else
               {
                if(Check.retcode == 10019) Print("There is not enough money to complete the request");
                if(Check.retcode == 10025) Print("No changes in request");
                if(Check.retcode == 10014) Print("Invalid volume in the request");
               }             
            }            
         }
       else
         {
          //check the direction of potential trade
          if(ZZLevel000[1] > ZZLevel100[1]) TradeDirection = 1;
          else TradeDirection = -1;
          
          // check if price cross any level
          for(int level = LevelsTotal - 1; level > LevelIndex; level--)
            {
             if((TradeDirection * (FiboPriceLeves[level] - Price[0].close)) > Point() * Breakout) // price is lower/higher then Fibo level depending on trade direction
                  {
                   LevelCross = true;
                   LevelIndex = level; // store the level number
                   CrossBarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
                   break;
                  }
            }
            
          // wait for another bar close
          if((datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE) != CrossBarTime)
            {
             CrossBarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
             // now it's time to check trade conditions (candle pattern - trend bar which close before the level)
             if(LevelCross) // if price crossed the level
               {
                
                //+------------------------------------------------+
                //| Trading conditions check                       |
                //+------------------------------------------------+
                if(  TradeDirection * (Price[1].close - Price[1].open)              > 0              && // last bar is in trade direction
                     TradeDirection * (FiboPriceLeves[LevelIndex] - Price[2].close) > 0              && // previous bar closed beyond the level
                     TradeDirection * (Price[1].close - FiboPriceLeves[LevelIndex]) > 0)                // last bar closed in a trade direction side of the level
                                {
                                 if((datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE) != CloseBarTime)
                                      {
                                       CloseBarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
                                       Print("Now trying to open position");
                                       ZeroMemory(Request);
                                       Request.action = TRADE_ACTION_DEAL;
                                       Request.symbol = Symbol();
                                       Request.volume = 1;
                                       Request.deviation = 3;
                                       if(TradeDirection == 1)
                                          {
                                           Request.sl = StopLossLevel = Price[1].low - Point() * StopLoss;
                                           Request.type = ORDER_TYPE_BUY;
                                           Request.price = SymbolInfoDouble(Symbol(), SYMBOL_ASK);
                                           PositionType = 1;
                                          }
                                       if(TradeDirection == -1)
                                          {
                                           Request.sl = StopLossLevel = Price[1].high + Point() * StopLoss;
                                           Request.type = ORDER_TYPE_SELL;
                                           Request.price = SymbolInfoDouble(Symbol(), SYMBOL_BID);
                                           PositionType = -1;
                                          }
                                       if(CheckMargin(Symbol(), Request.volume, Request.type, Request.price))
                                               {
                                                if(OrderSend(Request, Results))
                                                   {
                                                    Print("New position is open");
                                                    LevelCross = false;
                                                    LevelIndex = 0;
                                                    LastBarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
                                                    EP = StopLossLevel;
                                                    AF = AFStep;
                                                   }
                                               }
                                      }
                                }
               }
            }
                        
           
         }
      }
   else
      {
       Print("No price data available");
      }
  }
//+------------------------------------------------------------------+
//| Expert Check Margin function                                                   |
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
  int      DealsNum;
  ulong    DealTiket;
  if(HistorySelectByPosition(PositionID))
       {
       DealsNum = HistoryDealsTotal(); //--- total deals number in the list
       for( int i=0; i < DealsNum && !IsStopped(); i++)
         {
         DealTiket = HistoryDealGetTicket(i);
         if(StringFind(HistoryDealGetString(DealTiket, DEAL_COMMENT), "sl", 0) >= 0)
               {
                CloseBarTime = (datetime)SeriesInfoInteger(Symbol(), 0, SERIES_LASTBAR_DATE);
               }
         }
       }  
  } 
//+------------------------------------------------------------------+
