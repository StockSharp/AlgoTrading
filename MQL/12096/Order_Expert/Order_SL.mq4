//+------------------------------------------------------------------+
//| Order_SL.mq4                                                     |
//|   Creates a short TrendLine to mark the price this script        |
//|   was dropped onto the chart.                                    |
//|   If the drop point is above the current price, 
//|      a Sell Stop Loss is created.
//|   else
//|      a Buy Stop Loss is created.
//+------------------------------------------------------------------+
#property strict
//--- description
#property description "Script draws \"Trend Line\" graphical object."
#property description "Dropped above current price is a Sell Stop Loss"
#property description "Dropped below current price is a Buy  Stop Loss"

//--- input parameters of the script
string          SLName  = "StopLoss";

int             LineWidth=1;          // Line width

// Find out where the cursor was dropped.
double SLPrice    = NormalizeDouble(WindowPriceOnDropped(),Digits);

//+------------------------------------------------------------------+
//| Create a trend line by the given coordinates                     |
//+------------------------------------------------------------------+
bool TrendCreate(const long            chart_ID=0,        // chart's ID
                 const string          name="Buy",        // line name
                 datetime              time1=0,           // first point time
                 datetime              time2=0,           // second point time
                 double                price=0,           // price for horizontal line
                 const color           clr=clrGreen,      // line color
                 const ENUM_LINE_STYLE style=STYLE_SOLID, // line style
                 const int             width=1)           // line width
  {
//--- reset the error value
   ResetLastError();
//--- create a trend line by the given coordinates
   if(!ObjectCreate(chart_ID,name,OBJ_TREND,0,time1,price,time2,price))
     {
      Print(__FUNCTION__,
            ": failed to create a trend line! Error code = ",GetLastError());
      return(false);
     }
//--- set line color
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
//--- set line display style
   ObjectSetInteger(chart_ID,name,OBJPROP_STYLE,style);
//--- set line width
   ObjectSetInteger(chart_ID,name,OBJPROP_WIDTH,width);
//--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,false);
//--- enable (true) or disable (false) the mode of moving the line by mouse
//--- when creating a graphical object using ObjectCreate function, the object cannot be
//--- highlighted and moved by default. Inside this method, selection parameter
//--- is true by default making it possible to highlight and move the object
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,true);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,true);
//--- enable (true) or disable (false) the mode of continuation of the line's display to the left
   ObjectSetInteger(chart_ID,name,OBJPROP_RAY_LEFT,false);
//--- enable (true) or disable (false) the mode of continuation of the line's display to the right
   ObjectSetInteger(chart_ID,name,OBJPROP_RAY_RIGHT,true);
//--- hide (true) or display (false) graphical object name in the object list
//   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,false);
//--- successful execution
   return(true);
  }

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
  {
//--- number of visible bars in the chart window
   int bars=(int)ChartGetInteger(0,CHART_VISIBLE_BARS);
   if (bars > 10) bars = 10;                 // Limit the horizontal line to 10 bars
   
//--- create Stop Loss line
   if(!TrendCreate(0,SLName,Time[bars],Time[0],SLPrice,clrOrange,STYLE_DASH,LineWidth)) return;

//--- redraw the chart
   ChartRedraw();
//---
  }
