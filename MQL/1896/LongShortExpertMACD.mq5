//+------------------------------------------------------------------+
//|                                          LongShortExpertMACD.mq5 |
//| Copyright 2009-2013, MetaQuotes Software Corp. - 2013, jlwarrior |
//|                        https://login.mql5.com/en/users/jlwarrior |
//+------------------------------------------------------------------+
#property copyright "2009-2013, MetaQuotes Software Corp - 2013, jlwarrior"
#property link      "https://login.mql5.com/en/users/jlwarrior"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Include                                                          |
//+------------------------------------------------------------------+
#include <Expert\LongShortExpertModified.mqh>
#include <Expert\Signal\SignalMACD.mqh>
#include <Expert\Trailing\TrailingNone.mqh>
#include <Expert\Money\MoneyNone.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
//--- inputs for expert
input string Inp_Expert_Title            ="Long/Short ExpertMACD";
int          Expert_MagicNumber          =10981;
bool         Expert_EveryTick            =false;
input ENUM_AVAILABLE_POSITIONS Inp_Allowed_Positions=BOTH_POSITION; //short / long / both positions allowed

//--- default MACDExpert inputs
input int    Inp_Signal_MACD_PeriodFast  =12;                       // Fast MACD Period
input int    Inp_Signal_MACD_PeriodSlow  =24;                       // Slow MACD Period
input int    Inp_Signal_MACD_PeriodSignal=9;                        // Signal MACD Period
input int    Inp_Signal_MACD_TakeProfit  =50;                       // Take Profit Value
input int    Inp_Signal_MACD_StopLoss    =20;                       // Stop Loss Value
//+------------------------------------------------------------------+
//| Global expert object                                             |
//+------------------------------------------------------------------+
CLongShortExpertModified ExtExpert;  //specific designed CExpert Subclass
//+------------------------------------------------------------------+
//| Initialization function of the expert                            |
//+------------------------------------------------------------------+
int OnInit(void)
  {
//--- Initializing expert
   if(!ExtExpert.Init(Symbol(),Period(),Expert_EveryTick,Expert_MagicNumber))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing expert");
      ExtExpert.Deinit();
      return(-1);
     }
   
   // Specific parameter controlling which positions are allowed
   ExtExpert.SetAvailablePositions(Inp_Allowed_Positions);  

//--- Creation of signal object
   CSignalMACD *signal=new CSignalMACD;
   if(signal==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating signal");
      ExtExpert.Deinit();
      return(-2);
     }
//--- Add signal to expert (will be deleted automatically))
   if(!ExtExpert.InitSignal(signal))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing signal");
      ExtExpert.Deinit();
      return(-3);
     }
//--- Set signal parameters
   signal.PeriodFast(Inp_Signal_MACD_PeriodFast);
   signal.PeriodSlow(Inp_Signal_MACD_PeriodSlow);
   signal.PeriodSignal(Inp_Signal_MACD_PeriodSignal);
   signal.TakeLevel(Inp_Signal_MACD_TakeProfit);
   signal.StopLevel(Inp_Signal_MACD_StopLoss);
//--- Check signal parameters
   if(!signal.ValidationSettings())
     {
      //--- failed
      printf(__FUNCTION__+": error signal parameters");
      ExtExpert.Deinit();
      return(-4);
     }
//--- Creation of trailing object
   CTrailingNone *trailing=new CTrailingNone;
   if(trailing==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating trailing");
      ExtExpert.Deinit();
      return(-5);
     }
//--- Add trailing to expert (will be deleted automatically))
   if(!ExtExpert.InitTrailing(trailing))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing trailing");
      ExtExpert.Deinit();
      return(-6);
     }
//--- Set trailing parameters
//--- Check trailing parameters
   if(!trailing.ValidationSettings())
     {
      //--- failed
      printf(__FUNCTION__+": error trailing parameters");
      ExtExpert.Deinit();
      return(-7);
     }
//--- Creation of money object
   CMoneyNone *money=new CMoneyNone;
   if(money==NULL)
     {
      //--- failed
      printf(__FUNCTION__+": error creating money");
      ExtExpert.Deinit();
      return(-8);
     }
//--- Add money to expert (will be deleted automatically))
   if(!ExtExpert.InitMoney(money))
     {
      //--- failed
      printf(__FUNCTION__+": error initializing money");
      ExtExpert.Deinit();
      return(-9);
     }
//--- Set money parameters
//--- Check money parameters
   if(!money.ValidationSettings())
     {
      //--- failed
      printf(__FUNCTION__+": error money parameters");
      ExtExpert.Deinit();
      return(-10);
     }
//--- Tuning of all necessary indicators
   if(!ExtExpert.InitIndicators())
     {
      //--- failed
      printf(__FUNCTION__+": error initializing indicators");
      ExtExpert.Deinit();
      return(-11);
     }
//--- succeed
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
//| Function-event handler "tick"                                    |
//+------------------------------------------------------------------+
void OnTick(void)
  {
   ExtExpert.OnTick();
  }
//+------------------------------------------------------------------+
//| Function-event handler "trade"                                   |
//+------------------------------------------------------------------+
void OnTrade(void)
  {
   ExtExpert.OnTrade();
  }
//+------------------------------------------------------------------+
//| Function-event handler "timer"                                   |
//+------------------------------------------------------------------+
void OnTimer(void)
  {
   ExtExpert.OnTimer();
  }
//+------------------------------------------------------------------+
