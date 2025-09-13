//+------------------------------------------------------------------+
//| Order_Pending.mq4                                                |
//|   Creates a short TrendLine to mark the price this script        |
//|   was dropped onto the chart.                                    |
//|   If the drop point is above the current price, 
//|      a Buy trade is created.
//|   else
//|      a Sell trade is created.
//|   Stop Loss and Take Profit lines are also created.
//+------------------------------------------------------------------+
#property strict
//--- description
#property description "Script draws \"Trend Line\" graphical object."
#property description "Above current price is a Quick Buy"
#property description "Below current price is a Quick Sell"

double   InputAsk    = Ask;

// Find out where the cursor was dropped.
double   InputPrice  = NormalizeDouble(WindowPriceOnDropped(),Digits);
color    LineColor   = clrLime;        // Line color
double   Lots        = 0.01;           // Default lot size
double   Magic       = 3345.0;
int      iMagic;
double   MaxSlip     = 3;
int      iMaxSlip;
int      orderType   = OP_BUY;         // Default order type
string   SLName      = "StopLoss";
string   TPName      = "TakeProfit";
double   SLOffset    = 0.0060;         // 60 pip offset for 4/5 digit charts.
double   TPOffset    = 0.0060;         // 60 pip offset for 4/5 digit charts.
double   SLPrice;
double   TPPrice;

// Global Variables
string   GLots       = "GLots"+Symbol();
string   GTP         = "GTP"+Symbol();
string   GSL         = "GSL"+Symbol();
string   GMaxSlip    = "GMaxSlip"+Symbol();
string   GMagic      = "GMagic"+Symbol();
string   GStatus     = "GStatus"+Symbol();

//+------------------------------------------------------------------+
//| Create a trend line by the given coordinates                     |
//+------------------------------------------------------------------+
bool TrendCreate(const long            chart_ID=0,        // chart's ID
                 const string          name="Quick",      // line name
                 datetime              time1=0,           // first point time
                 datetime              time2=0,           // second point time
                 double                price=0,           // price for horizontal line
                 const color           clr=clrGreen,      // line color
                 const ENUM_LINE_STYLE style=STYLE_DASH,  // line style
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
//--- successful execution
   return(true);
  }

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
// Can we create an order?
   if (!IsTradeAllowed()) return; 

//--- number of visible bars in the chart window
   int bars=(int)ChartGetInteger(0,CHART_VISIBLE_BARS);
   if (bars > 10) bars = 10;
   
// Load GLOBAL variables
      Lots  = GlobalVariableGet(GLots);
      Magic = GlobalVariableGet(GMagic);
      MaxSlip = GlobalVariableGet(GMaxSlip);
      
      if (GlobalVariableCheck(GTP)) 
         TPOffset = GlobalVariableGet(GTP);
      else
         if (Digits < 4) TPOffset = TPOffset * 100.0;
      
      if (GlobalVariableCheck(GSL)) 
         SLOffset = GlobalVariableGet(GSL);
      else
         if (Digits < 4) SLOffset = SLOffset * 100.0;
         

// Name of line:  Buy or Sell
   if (InputPrice < InputAsk) 
      {
         LineColor = clrRed;     // Generate SELL
         InputAsk  = Bid;
         orderType = OP_SELL;
         SLPrice   = InputAsk + SLOffset;
         TPPrice   = InputAsk - TPOffset;
      } else
      {
         SLPrice   = InputAsk - SLOffset;
         TPPrice   = InputAsk + TPOffset;
      } 

//--- Create the order
   iMaxSlip = int(NormalizeDouble(MaxSlip,0));
   iMagic   = int(NormalizeDouble(Magic,0));
   int ticket=OrderSend(Symbol(), orderType, Lots, InputAsk, iMaxSlip, 0,0,"",iMagic,0,LineColor);
   if(ticket < 1)            // Did we create an order?
   {
      Print("Could not create the order: ",IntegerToString(GetLastError()));
      return;                // NO - abort
   }

   // Tell Order_EA that we tried to create One order
   datetime Temp = GlobalVariableSet(GStatus, 1.0);

//--- Create TP, & SL lines 
   if(!TrendCreate(0,SLName,Time[bars],Time[0],SLPrice,clrOrange)) return;
   if(!TrendCreate(0,TPName,Time[bars],Time[0],TPPrice,clrSeaGreen)) 
      {
         ObjectDelete(0,SLName);
         return;
      }   

//--- redraw the chart
   ChartRedraw();
//---
}
