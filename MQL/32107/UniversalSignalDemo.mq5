//+------------------------------------------------------------------+
//|                                          UniversalSignalDemo.mq5 |
//|                        Copyright 2020, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2020, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.0"
#property tester_set "\\Files\\UniversalSignalDemo.set"
//+------------------------------------------------------------------+
//| Include                                                          |
//+------------------------------------------------------------------+
#include <Expert\Expert.mqh>
// available signals
#include <Expert\Signal\SignalUniversal.mqh>
// available trailing
#include <Expert\Trailing\TrailingUniversal.mqh>
// available money management
#include <Expert\Money\MoneyFixedLot.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
// inputs for expert
input string Expert_Title               = "UniversalTest"; // Document name
ulong        Expert_MagicNumber         = 32583;           //
bool         Expert_EveryTick           = false;           //
// inputs for main signal
input int    Signal_ThresholdOpen       = 10;            // Signal threshold value to open [0...100]
input int    Signal_ThresholdClose      = 10;            // Signal threshold value to close [0...100]
input double Signal_PriceLevel          = 0.0;           // Price level to execute a deal
input double Signal_StopLevel           = 50.0;          // Stop Loss level (in points)
input double Signal_TakeLevel           = 50.0;          // Take Profit level (in points)
input int    Signal_Expiration          = 4;             // Expiration of pending orders (in bars)
input int    Signal_Universal_Pattern_0 = 100;           // Universal Signal(100,100,...) Signal A (1-st model) weight
input int    Signal_Universal_Pattern_1 = 100;           // Universal Signal(100,100,...) Signal B (2-nd model) weight
input int    Signal_Universal_Pattern_2 = 100;           // Universal Signal(100,100,...) Signal C (3-rd model) weight
input int    Signal_Universal_Pattern_3 = 100;           // Universal Signal(100,100,...) Signal D (4-th model) weight
input int    Signal_Universal_Pattern_4 = 100;           // Universal Signal(100,100,...) Signal E (5-tt model) weight
input int    Signal_Universal_Pattern_5 = 100;           // Universal Signal(100,100,...) Signal F (6-th model) weight
input int    Signal_Universal_Pattern_6 = 100;           // Universal Signal(100,100,...) Signal G (7-th model) weight
input int    Signal_Universal_Pattern_7 = 100;           // Universal Signal(100,100,...) Signal H (8-th model) weight
input double Signal_Universal_Weight    = 1.0;           // Universal Signal(100,100,...) Weight [0...1.0]
// inputs for money
input double Money_FixLot_Percent       = 10.0;          // Percent
input double Money_FixLot_Lots          = 1;             // Fixed volume
//+------------------------------------------------------------------+
//| Global expert object                                             |
//+------------------------------------------------------------------+
CExpert ExtExpert;
//+------------------------------------------------------------------+
//| Initialization function of the expert                            |
//+------------------------------------------------------------------+
int OnInit()
{
  // Initializing expert
  if(!ExtExpert.Init(Symbol(), Period(), Expert_EveryTick, Expert_MagicNumber))
  {
    // failed
    printf(__FUNCTION__ + ": error initializing expert");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  // Creating signal
  CExpertSignal *signal = new CExpertSignal;
  if(signal == NULL)
  {
    // failed
    printf(__FUNCTION__ + ": error creating signal");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }

  ExtExpert.InitSignal(signal);
  signal.ThresholdOpen(Signal_ThresholdOpen);
  signal.ThresholdClose(Signal_ThresholdClose);
  signal.PriceLevel(Signal_PriceLevel);
  signal.StopLevel(Signal_StopLevel);
  signal.TakeLevel(Signal_TakeLevel);
  signal.Expiration(Signal_Expiration);
  // Creating filter CSignalUniversal
  CSignalUniversal *filter0 = new CSignalUniversal;
  if(filter0 == NULL)
  {
    // failed
    printf(__FUNCTION__ + ": error creating filter0");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  signal.AddFilter(filter0);
  // Set filter parameters
  filter0.Pattern_0(Signal_Universal_Pattern_0);
  filter0.Pattern_1(Signal_Universal_Pattern_1);
  filter0.Pattern_2(Signal_Universal_Pattern_2);
  filter0.Pattern_3(Signal_Universal_Pattern_3);
  filter0.Pattern_4(Signal_Universal_Pattern_4);
  filter0.Pattern_5(Signal_Universal_Pattern_5);
  filter0.Pattern_6(Signal_Universal_Pattern_6);
  filter0.Pattern_7(Signal_Universal_Pattern_7);
  filter0.Weight(Signal_Universal_Weight);
  // Creation of trailing object
  TrailingUniversal *trailing = new TrailingUniversal;
  if(trailing == NULL)
  {
    // failed
    printf(__FUNCTION__ + ": error creating trailing");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  // Add trailing to expert (will be deleted automatically))
  if(!ExtExpert.InitTrailing(trailing))
  {
    // failed
    printf(__FUNCTION__ + ": error initializing trailing");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  // Creation of money object
  CMoneyFixedLot *money = new CMoneyFixedLot;
  if(money == NULL)
  {
    // failed
    printf(__FUNCTION__ + ": error creating money");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  // Add money to expert (will be deleted automatically))
  if(!ExtExpert.InitMoney(money))
  {
    // failed
    printf(__FUNCTION__ + ": error initializing money");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  // Set money parameters
  money.Percent(Money_FixLot_Percent);
  money.Lots(Money_FixLot_Lots);
  // Check all trading objects parameters
  if(!ExtExpert.ValidationSettings())
  {
    // failed
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
  // Tuning of all necessary indicators
  if(!ExtExpert.InitIndicators())
  {
    // failed
    printf(__FUNCTION__ + ": error initializing indicators");
    ExtExpert.Deinit();
    return(INIT_FAILED);
  }
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
