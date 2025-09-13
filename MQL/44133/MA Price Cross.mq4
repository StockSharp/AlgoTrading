//+------------------------------------------------------------------+
//|                                               MA Price Cross.mq4 |
//|                                          Copyright 2023,JBlanked |
//|                                  https://www.github.com/jblanked |
//+------------------------------------------------------------------+
#property copyright "Copyright 2023,JBlanked"
#property link      "https://www.github.com/jblanked"
#property version   "1.00"
#property strict

input string first_ma = "=====MA SETTINGS=====";//-------------------->
input int ma_period = 160; // MA Period
input ENUM_MA_METHOD ma_method = MODE_SMA; // MA method
input ENUM_APPLIED_PRICE ma_applied = PRICE_CLOSE; // MA Applied Price

input string time_settings = "=====TIME SETTINGS=====";//-------------------->
input string start_time    = "01:00"; // Start time
input string stop_time     = "22:00"; // Stop time

input string order_settings = "=====ORDER SETTINGS=====";//-------------------->
input double stop_loss = 200; // Stop Loss (points)
input double take_profit = 600; // Take Profit (points)
input double lot_size = 0.10; // Lot Size
input int    magicnumber = 124458; // Magic Number
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
// notification to inform users where to contact me at
   SendNotification("Indicator started! GO to https://www.thehyenahut.com/request-custom-bots/ for more bots");
   Print("Indicator started! GO to https://www.thehyenahut.com/request-custom-bots/ for more bots");

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
   if(OrdersTotal() == 0) // if no orders are opened
     {

      if(TimeCurrent() >= StringToTimeFix(start_time) && TimeCurrent() < StringToTimeFix(stop_time)) // if current time is allowed
        {

         // continue

         double ma = iMA(_Symbol,_Period,ma_period,0,ma_method,ma_applied,1); // last ma price

         double past_ma = iMA(_Symbol,_Period,ma_period,0,ma_method,ma_applied,2); // candle before last ma price

         bool buy = past_ma < Ask && ma > Ask; // buy setup

         bool sell = past_ma > Bid && ma < Bid; // sell setup
         
         if(CheckVolumeValue(lot_size)) // if volume is allowed
         {

         // if buy setup, then buy
         if(buy)
            int buy_order = OrderSend(_Symbol,OP_BUY,lot_size,Ask,0,Ask - stop_loss * _Point,Ask + take_profit * _Point,"Drew123",magicnumber);

         // if sell setup, then sell
         if(sell)
            int sell_order = OrderSend(_Symbol,OP_SELL,lot_size,Bid,0,Bid + stop_loss * _Point,Bid - take_profit * _Point,"Drew123",magicnumber);
            
         } // end of if volume is allowed

        } // end of if current time is allowed

     } // end of if no orders are opened

  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//|      Function to convert time into string                        |
//+------------------------------------------------------------------+
datetime StringToTimeFix(string time) // replaces MQL5's StringToTime function'
  {
   MqlDateTime day; // define today as a datetime object
   TimeCurrent(day); // grab the current date's info

// Find the position of ":"
   int colon_position = StringFind(time, ":");
   if(colon_position < 0)  // if there are none in the input
     {
      Print("Error: Invalid time format"); // print an error
      return 0; // return 0
     }

// Extract hour and minute substrings
   string hourStr = StringSubstr(time, 0, colon_position); // find hour
   string minuteStr = StringSubstr(time, colon_position + 1); // find minute

   int hour = (int)StringToInteger(hourStr); // set hour as an integer
   int minute = (int)StringToInteger(minuteStr); // set minutes as an integer

   day.hour = hour; // set the hour to today's hours
   day.min = minute; // set the minutes to today's minutes
   day.sec = 0; // set seconds to 0

   datetime date = StructToTime(day); // convert MqlDateTime back to datetime

   return date; // return user input's time as the hour, minutes, and seconds set to 0
  }
//+------------------------------------------------------------------+
//+------------------------------------------------------------------+
//| Check the correctness of the order volume                        |
//+------------------------------------------------------------------+
bool CheckVolumeValue(double volume)
  {
   return volume < SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MIN) ? false : volume > SymbolInfoDouble(Symbol(),SYMBOL_VOLUME_MAX) ? false : true;
  }