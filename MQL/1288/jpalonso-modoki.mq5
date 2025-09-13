//+------------------------------------------------------------------+
//|                                              jpalonso-modoki.mq5 |
//|                                          Copyright 2012, alohafx |
//|                                   http://alohafx.blog36.fc2.com/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, alohafx"
#property link      "http://alohafx.blog36.fc2.com/"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Include                                                          |
//+------------------------------------------------------------------+
#include <Expert\Expert.mqh>
//--- available signals
#include <SignalEnvelopes-jpalonso.mqh>
//--- available trailing
#include <Expert\Trailing\TrailingNone.mqh>
//--- available money management
#include <Expert\Money\MoneyFixedLot.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
//--- inputs for expert
input string             Expert_Title              ="jpalonso-modoki"; // Document name
ulong                    Expert_MagicNumber        =4986;              // 
bool                     Expert_EveryTick          =false;             // 
//--- inputs for main signal
input int                Signal_ThresholdOpen      =10;                // Signal threshold value to open [0...100]
input int                Signal_ThresholdClose     =10;                // Signal threshold value to close [0...100]
input double             Signal_PriceLevel         =0.0;               // Price level to execute a deal
input double             Signal_StopLevel          =77.0;              // Stop Loss level (in points)
input double             Signal_TakeLevel          =127.0;              // Take Profit level (in points)
input int                Signal_Expiration         =4;                 // Expiration of pending orders (in bars)
input int                Signal_Envelopes_PeriodMA =200;               // Envelopes(200,0,MODE_SMA,...) Period of averaging
input int                Signal_Envelopes_Shift    =0;                 // Envelopes(200,0,MODE_SMA,...) Time shift
input ENUM_MA_METHOD     Signal_Envelopes_Method   =MODE_SMA;          // Envelopes(200,0,MODE_SMA,...) Method of averaging
input ENUM_APPLIED_PRICE Signal_Envelopes_Applied  =PRICE_CLOSE;       // Envelopes(200,0,MODE_SMA,...) Prices series
input double             Signal_Envelopes_Deviation=0.35;              // Envelopes(200,0,MODE_SMA,...) Deviation
input double             Signal_Envelopes_Weight   =1.0;               // Envelopes(200,0,MODE_SMA,...) Weight [0...1.0]
//--- inputs for money
input double             Money_FixLot_Percent      =10.0;              // Percent
input double             Money_FixLot_Lots         =5.0;               // Fixed volume
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
      return(-1);
     }
//--- Creating signal
   CExpertSignal *signal=new CExpertSignal;
   if(signal==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating signal");
      ExtExpert.Deinit();
      return(-2);
     }
//---
   ExtExpert.InitSignal(signal);
   signal.ThresholdOpen(Signal_ThresholdOpen);
   signal.ThresholdClose(Signal_ThresholdClose);
   signal.PriceLevel(Signal_PriceLevel);
   signal.StopLevel(Signal_StopLevel);
   signal.TakeLevel(Signal_TakeLevel);
   signal.Expiration(Signal_Expiration);
//--- Creating filter CSignalEnvelopes
   CSignalEnvelopes *filter0=new CSignalEnvelopes;
   if(filter0==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating filter0");
      ExtExpert.Deinit();
      return(-3);
     }
   signal.AddFilter(filter0);
//--- Set filter parameters
   filter0.PeriodMA(Signal_Envelopes_PeriodMA);
   filter0.Shift(Signal_Envelopes_Shift);
   filter0.Method(Signal_Envelopes_Method);
   filter0.Applied(Signal_Envelopes_Applied);
   filter0.Deviation(Signal_Envelopes_Deviation);
   filter0.Weight(Signal_Envelopes_Weight);
//--- Creation of trailing object
   CTrailingNone *trailing=new CTrailingNone;
   if(trailing==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating trailing");
      ExtExpert.Deinit();
      return(-4);
     }
//--- Add trailing to expert (will be deleted automatically))
   if(!ExtExpert.InitTrailing(trailing))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing trailing");
      ExtExpert.Deinit();
      return(-5);
     }
//--- Set trailing parameters
//--- Creation of money object
   CMoneyFixedLot *money=new CMoneyFixedLot;
   if(money==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating money");
      ExtExpert.Deinit();
      return(-6);
     }
//--- Add money to expert (will be deleted automatically))
   if(!ExtExpert.InitMoney(money))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing money");
      ExtExpert.Deinit();
      return(-7);
     }
//--- Set money parameters
   money.Percent(Money_FixLot_Percent);
   money.Lots(Money_FixLot_Lots);
//--- Check all trading objects parameters
   if(!ExtExpert.ValidationSettings())
     {
      //--- failed
      ExtExpert.Deinit();
      return(-8);
     }
//--- Tuning of all necessary indicators
   if(!ExtExpert.InitIndicators())
     {
      //--- failed
      printf(__FUNCTION__+": error initializing indicators");
      ExtExpert.Deinit();
      return(-9);
     }
//--- ok
   return(0);
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
   if(PositionSelect(Symbol())) return;
   if(TimeCurrent() < D'2012.10.08 10:55') return;
   
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
