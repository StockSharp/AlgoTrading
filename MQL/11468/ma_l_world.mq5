//+------------------------------------------------------------------+
//|                                                   MA L WORLD.mq5 |
//|                                                Ronnie Mansolillo |
//|                        http://rmansolillo.wix.com/rosimantrading |
//+------------------------------------------------------------------+
#property copyright "Ronnie Mansolillo"
#property link      "http://rmansolillo.wix.com/rosimantrading"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Include                                                          |
//+------------------------------------------------------------------+
#include <Expert\Expert.mqh>
//--- available signals
#include <Expert\Signal\SignalMA_Simple_Crossover.mqh>
//--- available trailing
#include <Expert\Trailing\TrailingMA.mqh>
//--- available money management
#include <Expert\Money\MoneyFixedMargin.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
//--- inputs for expert
input string             Expert_Title                    ="MA L WORLD";   // Document name
ulong                    Expert_MagicNumber              =18884;          // 
bool                     Expert_EveryTick                =false;          // 
//--- inputs for main signal
input int                Signal_ThresholdOpen            =20;             // Signal threshold value to open [0...100]
input int                Signal_ThresholdClose           =20;             // Signal threshold value to close [0...100]
input double             Signal_PriceLevel               =2.0;            // Price level to execute a deal
input double             Signal_StopLevel                =95.0;           // Stop Loss level (in points)
input double             Signal_TakeLevel                =670.0;           // Take Profit level (in points)
input int                Signal_Expiration               =0;              // Expiration of pending orders (in bars)
input int                Signal_MACROSS_PeriodMA1        =12;             // Moving Average Crossover(12,...) Period of fast averaging
input int                Signal_MACROSS_PeriodMA2        =25;             // Moving Average Crossover(12,...) Period of slow averaging
input int                Signal_MACROSS_Shift            =0;              // Moving Average Crossover(12,...) Time shift
input ENUM_MA_METHOD     Signal_MACROSS_Method           =MODE_LWMA;      // Moving Average Crossover(12,...) Method of averaging
input ENUM_APPLIED_PRICE Signal_MACROSS_Applied          =PRICE_CLOSE;    // Moving Average Crossover(12,...) Prices series
input ENUM_TIMEFRAMES    Signal_MACROSS_Set_MAC_timeframe=PERIOD_CURRENT; // Moving Average Crossover(12,...) 
input double             Signal_MACROSS_Weight           =1.0;            // Moving Average Crossover(12,...) Weight [0...1.0]
//--- inputs for trailing
input int                Trailing_MA_Period              =92;             // Period of MA
input int                Trailing_MA_Shift               =0;              // Shift of MA
input ENUM_MA_METHOD     Trailing_MA_Method              =MODE_EMA;       // Method of averaging
input ENUM_APPLIED_PRICE Trailing_MA_Applied             =PRICE_CLOSE;    // Prices series
//--- inputs for money
input double             Money_FixMargin_Percent=1.0;            // Percentage of margin
//+------------------------------------------------------------------+
//| Global expert object                                             |
//+------------------------------------------------------------------+
CExpert ExtExpert;
//+------------------------------------------------------------------+
//| Initialization function of the expert                            |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- Initializing expert
   if(!ExtExpert.Init(Symbol(),Period(),Expert_EveryTick,Expert_MagicNumber))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing expert");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- Creating signal
   CExpertSignal *signal=new CExpertSignal;
   if(signal==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating signal");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//---
   ExtExpert.InitSignal(signal);
   signal.ThresholdOpen(Signal_ThresholdOpen);
   signal.ThresholdClose(Signal_ThresholdClose);
   signal.PriceLevel(Signal_PriceLevel);
   signal.StopLevel(Signal_StopLevel);
   signal.TakeLevel(Signal_TakeLevel);
   signal.Expiration(Signal_Expiration);
//--- Creating filter CSignalMAC
   CSignalMAC *filter0=new CSignalMAC;
   if(filter0==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating filter0");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
   signal.AddFilter(filter0);
//--- Set filter parameters
   filter0.PeriodMA1(Signal_MACROSS_PeriodMA1);
   filter0.PeriodMA2(Signal_MACROSS_PeriodMA2);
   filter0.Shift(Signal_MACROSS_Shift);
   filter0.Method(Signal_MACROSS_Method);
   filter0.Applied(Signal_MACROSS_Applied);
   filter0.Set_MAC_timeframe(Signal_MACROSS_Set_MAC_timeframe);
   filter0.Weight(Signal_MACROSS_Weight);
//--- Creation of trailing object
   CTrailingMA *trailing=new CTrailingMA;
   if(trailing==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating trailing");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- Add trailing to expert (will be deleted automatically))
   if(!ExtExpert.InitTrailing(trailing))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing trailing");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- Set trailing parameters
   trailing.Period(Trailing_MA_Period);
   trailing.Shift(Trailing_MA_Shift);
   trailing.Method(Trailing_MA_Method);
   trailing.Applied(Trailing_MA_Applied);
//--- Creation of money object
   CMoneyFixedMargin *money=new CMoneyFixedMargin;
   if(money==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating money");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- Add money to expert (will be deleted automatically))
   if(!ExtExpert.InitMoney(money))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing money");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- Set money parameters
   money.Percent(Money_FixMargin_Percent);
//--- Check all trading objects parameters
   if(!ExtExpert.ValidationSettings())
     {
      //--- failed
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- Tuning of all necessary indicators
   if(!ExtExpert.InitIndicators())
     {
      //--- failed
      printf(__FUNCTION__+": error initializing indicators");
      ExtExpert.Deinit();
      return(INIT_FAILED);
     }
//--- ok
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Deinitialization function of the expert                          |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   ExtExpert.Deinit();
  }
//+------------------------------------------------------------------+
//| "Tick" event handler function                                    |
//+------------------------------------------------------------------+
void OnTick()
  {
   ExtExpert.OnTick();
  }
//+------------------------------------------------------------------+
//| "Trade" event handler function                                   |
//+------------------------------------------------------------------+
void OnTrade()
  {
   ExtExpert.OnTrade();
  }
//+------------------------------------------------------------------+
//| "Timer" event handler function                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   ExtExpert.OnTimer();
  }
//+------------------------------------------------------------------+
