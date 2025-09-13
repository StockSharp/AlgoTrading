//+------------------------------------------------------------------+
//|                                              Expert_2EMA_ITF.mq5 |
//|                        Copyright 2010, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2010, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Include                                                          |
//+------------------------------------------------------------------+
#include <Expert\Expert.mqh>
#include <Expert\Signal\Signal2EMA-ITF.mqh>
#include <Expert\Trailing\TrailingNone.mqh>
#include <Expert\Money\MoneyFixedLot.mqh>
//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
//--- inputs for expert
input string Inp_Expert_Title                         ="Expert_2EMA_ITF";
int          Expert_MagicNumber                       =27198;
bool         Expert_EveryTick                         =false;
//--- inputs for signal
input int    Inp_Signal_TwoEMAwithITF_PeriodFastEMA   =5;
input int    Inp_Signal_TwoEMAwithITF_PeriodSlowEMA   =30;
input int    Inp_Signal_TwoEMAwithITF_PeriodATR       =7;
input double Inp_Signal_TwoEMAwithITF_Limit           =1.2;
input double Inp_Signal_TwoEMAwithITF_StopLoss        =5.0;
input double Inp_Signal_TwoEMAwithITF_TakeProfit      =8.0;
input int    Inp_Signal_TwoEMAwithITF_Expiration      =4;
input int    Inp_Signal_TwoEMAwithITF_GoodMinuteOfHour=-1;
input long   Inp_Signal_TwoEMAwithITF_BadMinutesOfHour=0;
input int    Inp_Signal_TwoEMAwithITF_GoodHourOfDay   =-1;
input int    Inp_Signal_TwoEMAwithITF_BadHoursOfDay   =0;
input int    Inp_Signal_TwoEMAwithITF_GoodDayOfWeek   =-1;
input int    Inp_Signal_TwoEMAwithITF_BadDaysOfWeek   =0;
//--- inputs for money
input double Inp_Money_FixLot_Percent                 =10.0;
input double Inp_Money_FixLot_Lots                    =0.1;
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
//--- Creation of signal object
   CSignal2EMA_ITF *signal=new CSignal2EMA_ITF;
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
   signal.PeriodFastEMA(Inp_Signal_TwoEMAwithITF_PeriodFastEMA);
   signal.PeriodSlowEMA(Inp_Signal_TwoEMAwithITF_PeriodSlowEMA);
   signal.PeriodATR(Inp_Signal_TwoEMAwithITF_PeriodATR);
   signal.Limit(Inp_Signal_TwoEMAwithITF_Limit);
   signal.StopLoss(Inp_Signal_TwoEMAwithITF_StopLoss);
   signal.TakeProfit(Inp_Signal_TwoEMAwithITF_TakeProfit);
   signal.Expiration(Inp_Signal_TwoEMAwithITF_Expiration);
   signal.GoodMinuteOfHour(Inp_Signal_TwoEMAwithITF_GoodMinuteOfHour);
   signal.BadMinutesOfHour(Inp_Signal_TwoEMAwithITF_BadMinutesOfHour);
   signal.GoodHourOfDay(Inp_Signal_TwoEMAwithITF_GoodHourOfDay);
   signal.BadHoursOfDay(Inp_Signal_TwoEMAwithITF_BadHoursOfDay);
   signal.GoodDayOfWeek(Inp_Signal_TwoEMAwithITF_GoodDayOfWeek);
   signal.BadDaysOfWeek(Inp_Signal_TwoEMAwithITF_BadDaysOfWeek);
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
   CMoneyFixedLot *money=new CMoneyFixedLot;
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
   money.Percent(Inp_Money_FixLot_Percent);
   money.Lots(Inp_Money_FixLot_Lots);
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
//| Function-event handler "tick"                                    |
//+------------------------------------------------------------------+
void OnTick()
  {
   ExtExpert.OnTick();
  }
//+------------------------------------------------------------------+
//| Function-event handler "trade"                                   |
//+------------------------------------------------------------------+
void OnTrade()
  {
   ExtExpert.OnTrade();
  }
//+------------------------------------------------------------------+
//| Function-event handler "timer"                                   |
//+------------------------------------------------------------------+
void OnTimer()
  {
   ExtExpert.OnTimer();
  }
//+------------------------------------------------------------------+
