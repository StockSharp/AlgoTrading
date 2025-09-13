//+------------------------------------------------------------------+
//|                                   _HPCS_Inter7_MT4_EA_V01_We.mq4 |
//|                        Copyright 2021, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict
#property script_show_inputs
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

input int ii_shift = 0;
input int ii_period = 20;
input double id_deviation = 2;
datetime gdt_Candle = Time[1];

//+------------------------------------------------------------------+
//|                                                                  |
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

   double ld_lowerEnvelope0 = iBands(_Symbol,PERIOD_CURRENT,ii_period,id_deviation,ii_shift,PRICE_CLOSE,MODE_LOWER,0);
   double ld_lowerEnvelope1 = iBands(_Symbol,PERIOD_CURRENT,ii_period,id_deviation,ii_shift,PRICE_CLOSE,MODE_LOWER,1);
   double ld_UpperEnvelope0 = iBands(_Symbol,PERIOD_CURRENT,ii_period,id_deviation,ii_shift,PRICE_CLOSE,MODE_UPPER,0);
   double ld_UpperEnvelope1 = iBands(_Symbol,PERIOD_CURRENT,ii_period,id_deviation,ii_shift,PRICE_CLOSE,MODE_UPPER,1);

   int li_Factor = 1;
   if(Digits == 5 || Digits == 3)
     {
      li_Factor = 10;
     }

   double ld_takeprofitSell = Bid - 10*Point()*li_Factor;
   double ld_stoplossSell = Bid + 10*Point()*li_Factor;

   if(ld_stoplossSell < Ask + (MarketInfo(_Symbol,MODE_STOPLEVEL)*Point))
   {
      ld_stoplossSell = Ask + (MarketInfo(_Symbol,MODE_STOPLEVEL)*Point);
   }

   if(Close[0] < ld_lowerEnvelope0 && Close[1] > ld_lowerEnvelope1)
     {
      if(gdt_Candle != Time[0])
        {
         int li_Ticket1 = OrderSend(_Symbol,OP_SELL,1,Bid,10,ld_stoplossSell,ld_takeprofitSell,NULL,1212);
         if(li_Ticket1 < 0)
           {
            Print("Order Not Generated",GetLastError());
           }

         //func_FileClose(0);
         gdt_Candle = Time[0];
        }
     }
     
   double ld_takeprofitBuy = Ask + 10*Point()*li_Factor;
   double ld_stoplossBuy = Ask - 10*Point()*li_Factor;

   if(ld_stoplossBuy > (Bid -MarketInfo(_Symbol,MODE_STOPLEVEL)*Point()))
     {
      ld_stoplossBuy = Bid -(MarketInfo(_Symbol,MODE_STOPLEVEL)*Point()) ;
     }

   if(Close[0] > ld_UpperEnvelope0 && Close[1] < ld_UpperEnvelope1)
     {
      if(gdt_Candle != Time[0])
        {
         int li_Ticket2 = OrderSend(_Symbol,OP_BUY,1,Ask,10,ld_stoplossBuy,ld_takeprofitBuy,NULL,1122);
         if(li_Ticket2 < 0)
           {
            Print("Order Not Generated",GetLastError());
           }
         //func_OrderClose(1);
         gdt_Candle = Time[0];
        }
     }
  }

/* void func_OrderClose(int ordertype)
{
 int li_totalOrder = OrdersTotal();
 for(int i=(li_totalOrder-1); i>=0; i--)
   {
    if(OrderSelect(i,SELECT_BY_POS))
      {
       if(OrderMagicNumber() == ii_magicNumber && OrderType() == ordertype)
         {
          if(OrderCloseTime() == 0)
            {
             if(!OrderClose(OrderTicket(),id_lots,OrderClosePrice(),ii_slipage))
               {
                Print("Order Noot Close with Error! ",GetLastError());
               }
            }

         }

      }
   }



}*/


//+------------------------------------------------------------------+
