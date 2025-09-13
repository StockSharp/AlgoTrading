//+------------------------------------------------------------------+
//|                     _HPCS_FastSlowMACrosssover_MT4_EA_V01_WE.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property script_show_inputs

input string is_start = "HH:MM" ;
input string is_stop = "HH:MM" ;

input int ii_takeprofit = 10;
input int ii_stoploss = 10;
input int ii_lots = 1;

input int ii_fastMAPeriod = 14;   // Fast Moving Average Period
input int ii_slowMAPeriod = 21;   // Slow Moving Average Period

datetime gdt_TimeCurrent = Time[1];
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---

//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---

  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---

   datetime ldt_StartTime = StringToTime(is_start);
   datetime ldt_StopTime = StringToTime(is_stop);

   if(TimeCurrent()>ldt_StartTime && TimeCurrent()<ldt_StopTime)
     {

      double ld_buysignal = iCustom(_Symbol,PERIOD_CURRENT,"_HPCS_FastSlowMACrossover_MT4_Indi_V01_WE",ii_fastMAPeriod,ii_slowMAPeriod,0,0);
      double ld_Sellsignal = iCustom(_Symbol,PERIOD_CURRENT,"_HPCS_FastSlowMACrossover_MT4_Indi_V01_WE",ii_fastMAPeriod,ii_slowMAPeriod,1,0);

      int li_Factor = 0;
      if(Digits == 5 || Digits == 3)
        { li_Factor = 10; }

      double ld_TakeProfitBuy = Ask + ii_takeprofit*Point()*li_Factor;
      double ld_StopLossBuy = Ask - ii_takeprofit*Point()*li_Factor;

      if(ld_StopLossBuy > (Bid - MarketInfo(_Symbol,MODE_STOPLEVEL)*Point()))
        {
         ld_StopLossBuy = Bid - (MarketInfo(_Symbol,MODE_STOPLEVEL)*Point());
        }

      if(ld_buysignal != EMPTY_VALUE)
        {
         if(gdt_TimeCurrent != Time[0])
           {
            int li_TicketBuy = OrderSend(_Symbol,OP_BUY,ii_lots,Ask,10,ld_StopLossBuy,ld_TakeProfitBuy,NULL,1212);
            if(li_TicketBuy < 0)
              {
               Print("Order Not Generated",GetLastError());
              }
            gdt_TimeCurrent = Time[0];

           }
        }

      double ld_TakeProfitSell = Bid - ii_takeprofit*Point()*li_Factor;
      double ld_StopLossSell = Bid + ii_takeprofit*Point()*li_Factor;

      if(ld_StopLossSell < (Ask + MarketInfo(_Symbol,MODE_STOPLEVEL)*Point()))
        {
         ld_StopLossSell = Ask + (MarketInfo(_Symbol,MODE_STOPLEVEL)*Point());
        }

      if(ld_Sellsignal != EMPTY_VALUE)
        {
         if(gdt_TimeCurrent != Time[0])
           {
            int li_TicketSell = OrderSend(_Symbol,OP_SELL,ii_lots,Bid,10,ld_StopLossSell,ld_TakeProfitSell,NULL,1122);
            if(li_TicketSell < 0)
              {
               Print("Order Not Generated",GetLastError());
              }
            gdt_TimeCurrent = Time[0];
           }
        }

     }
     else
     {
         Print("Market Not Operating");
     }
  }
//+------------------------------------------------------------------+
