//+------------------------------------------------------------------+
//|                                                       DojiEN.mq4 |
//|                                  Copyright 2024, MetaQuotes Ltd. |
//|                                                          mql.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2024, MetaQuotes Ltd."
#property link      "mql.com"
#property version   "1.00"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   // Enable alerts when EA is initialized
   Print("EA for detecting classic Doji pattern initialized.");
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   // When EA is deinitialized, objects can be removed (if you want to keep them, you can disable this)
   Print("EA for detecting Doji pattern deinitialized.");
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
   // Check if a classic Doji pattern appeared on the previous closed candle
   if(IsDoji())
     {
      // Mark the Doji pattern on the chart
      MarkDoji();
      
      // Display alert and log message
      string symbol = Symbol();
      int timeframe = Period();
      Alert("Classic Doji pattern detected on symbol: ", symbol, " on timeframe: ", timeframe);
      Print("Classic Doji pattern detected on symbol: ", symbol, " on timeframe: ", timeframe);
     }
  }
//+------------------------------------------------------------------+
//| Function to check for a classic Doji pattern                     |
//+------------------------------------------------------------------+
bool IsDoji()
  {
   // Define tolerance for the difference between open and close prices, and for open/close being near the candle's middle
   double middle_tolerance = 0.1;  // 10% tolerance for proximity of open/close to the middle of the candle
   double open_close_tolerance = 3 * Point;  // 3 points tolerance between open and close

   // Get data for the previous closed candle (index 1)
   double open = iOpen(NULL, 0, 1);  // Open price of the candle
   double close = iClose(NULL, 0, 1); // Close price of the candle
   double high = iHigh(NULL, 0, 1);  // High price of the candle
   double low = iLow(NULL, 0, 1);    // Low price of the candle

   // Calculate the middle point of the candle
   double middle = (high + low) / 2.0;

   // Check if the difference between open and close is less than 3 points
   bool is_open_close_near = MathAbs(open - close) <= open_close_tolerance;

   // Check if the open and close prices are near the middle of the candle
   bool is_near_middle = MathAbs(open - middle) <= (high - low) * middle_tolerance &&
                         MathAbs(close - middle) <= (high - low) * middle_tolerance;

   // If both conditions are met, we consider it a classic Doji
   return is_open_close_near && is_near_middle;
  }
//+------------------------------------------------------------------+
//| Function to mark the Doji pattern                                |
//+------------------------------------------------------------------+
void MarkDoji()
  {
   // Define parameters for drawing the arrow
   int shift = 1; // Index of the previously closed candle (index 1)
   string dojiArrowName = "DojiArrow_" + TimeToStr(Time[shift], TIME_DATE|TIME_MINUTES);

   // Check if the arrow already exists on the chart
   if (ObjectFind(0, dojiArrowName) < 0)
     {
      // Draw the arrow 5 points below the low of the candle
      double arrowPosition = Low[shift] - 5 * Point;  // Position 5 points below the candle's low
      ObjectCreate(0, dojiArrowName, OBJ_ARROW, 0, Time[shift], arrowPosition);
      ObjectSetInteger(0, dojiArrowName, OBJPROP_ARROWCODE, 241);  // Arrow code (horizontal line)
      ObjectSetInteger(0, dojiArrowName, OBJPROP_COLOR, clrRed);   // Color of the arrow (red)
      ObjectSetInteger(0, dojiArrowName, OBJPROP_WIDTH, 2);        // Arrow thickness
     }

   // Add the "Doji" label 3 points below the arrow
   string dojiTextName = "DojiText_" + TimeToStr(Time[shift], TIME_DATE|TIME_MINUTES);
   if (ObjectFind(0, dojiTextName) < 0)
     {
      double textPosition = Low[shift] - 8 * Point - 14 * Point;  // Position of the text 14 points below the arrow
      ObjectCreate(0, dojiTextName, OBJ_TEXT, 0, Time[shift], textPosition);
      ObjectSetString(0, dojiTextName, OBJPROP_TEXT, "Doji");    // Text "Doji"
      ObjectSetInteger(0, dojiTextName, OBJPROP_COLOR, clrRed);  // Text color (red)
      ObjectSetInteger(0, dojiTextName, OBJPROP_FONTSIZE, 10);   // Font size
     }
  }
//+------------------------------------------------------------------+
