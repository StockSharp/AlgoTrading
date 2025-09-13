#property description "The Expert Advisor demonstrates how to take a screenshot of the current chart using the ChartScreenShot() function when the 's' key is pressed."

#define WIDTH  1920    
#define HEIGHT 1080    
#define s_key  83       // ASCII code for 's' key  // https://www.w3.org/2002/09/tests/keys.html


input string screenshot_name = "Unnamed"; // Name of the screenshot

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
void OnInit()
  {
   ChartSetInteger(0, CHART_AUTOSCROLL, false); // Disable chart autoscroll
   ChartSetInteger(0, CHART_SHIFT, false);       // Set the shift of the right edge of the chart
   ChartSetInteger(0, CHART_MODE, CHART_CANDLES); // Show a candlestick chart
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{}

//+------------------------------------------------------------------+
//| ChartEvent function                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   // Handle the CHARTEVENT_KEYDOWN event ("A key press on the chart")
   if (id == CHARTEVENT_KEYDOWN && (int)lparam == s_key)
     {
         // Prepare a file name for the screenshot
         string name = screenshot_name + "_" + TimeToString(TimeCurrent(), TIME_DATE) + "_" + ".png";
         Comment(name); // Show the name on the chart as a comment

         // Save the chart screenshot in a file in the terminal_directory\MQL5\Files\
         if (ChartScreenShot(0, name, WIDTH, HEIGHT, ALIGN_CENTER))
           {
            Print("Saved the screenshot ", name, " to MQL5\\Files");
           }
     }
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment(""); // Clear the chart comment
  }
