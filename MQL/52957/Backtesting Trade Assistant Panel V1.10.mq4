//+------------------------------------------------------------------+
//|                            Backtesting Trade Assistant Panel.mq4 |
//|                                        Amirhossein Heydarijokani |
//|                                                             none |
//+------------------------------------------------------------------+
#property copyright "Amirhossein Heydarijokani"
#property link      "https://t.me/Axiom_Trader"
#property link      "https://www.mql5.com/en/users/heydariamir/seller"
#property version   "1.10"
#property strict
//--- Defaults parameters
input int StopLoss =50;    // StopLoss (Points)
input int TakeProfit =100; // TakeProfit (Points)
input int MagicNumber=99;





//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
   if(IsTesting())
   {
      RectLabelCreate(140,150);
      EditCreate("LT",80,100,60,20,DoubleToStr(MarketInfo(Symbol(),MODE_MINLOT),Digits));
      EditCreate("SL",80,125,60,20,IntegerToString(StopLoss));
      EditCreate("TP",80,150,60,20,IntegerToString(TakeProfit));
      LabelCreate("LT Label",10,100,"LotSize");
      LabelCreate("SL Label",10,125,"StopLoss");
      LabelCreate("TP Label",10,150,"TakeProfit");
      ButtonCreate("Buy",10,30,"BUY",clrGreen);
      ButtonCreate("Sell",80,30,"SELL",clrRed);
   }
   //---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+





//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
   //---
}
//+------------------------------------------------------------------+





//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
   if(IsTesting())
   {
      string name="Buy";
      if(ObjectGetInteger(0,name,OBJPROP_STATE))
      {
         ObjectSetInteger(0,name,OBJPROP_STATE,false);
         double Lot =StrToDouble(ObjectGetString(0,"LT",OBJPROP_TEXT));
         double sl=StrToDouble(ObjectGetString(0,"SL",OBJPROP_TEXT));
         double tp=StrToDouble(ObjectGetString(0,"TP",OBJPROP_TEXT));
         int ticket=OrderSend(Symbol(),OP_BUY,Lot,Ask,50,Bid-sl*Point,Ask+tp*Point,NULL,MagicNumber,0,clrNONE);
      }
      name="Sell";
      if(ObjectGetInteger(0,name,OBJPROP_STATE))
      {
         ObjectSetInteger(0,name,OBJPROP_STATE,false);
         double Lot =StrToDouble(ObjectGetString(0,"LT",OBJPROP_TEXT));
         int sl=StrToInteger(ObjectGetString(0,"SL",OBJPROP_TEXT));
         int tp=StrToInteger(ObjectGetString(0,"TP",OBJPROP_TEXT));
         int ticket=OrderSend(Symbol(),OP_SELL,Lot,Bid,50,Ask+sl*Point,Bid-tp*Point,NULL,MagicNumber,0,clrNONE);
      }
   }
   //---
}
//+------------------------------------------------------------------+







// docs.mql4.com/constants/objectconstants/enum_object/obj_rectangle_label
//+------------------------------------------------------------------+
//| Create rectangle label                                           |
//+------------------------------------------------------------------+
bool RectLabelCreate(const int width,const int height)
{
   const long             chart_ID=0;               // chart's ID
   const string           name="RectLabel";         // label name
   const int              sub_window=0;             // subwindow index
   const int              x=5;                      // X coordinate
   const int              y=25;                      // Y coordinate
   //const int              width=120;                 // width
   //const int              height=200;                // height
   const color            back_clr=clrAntiqueWhite;  // background color
   const ENUM_BORDER_TYPE border=BORDER_SUNKEN;     // border type
   const ENUM_BASE_CORNER corner=CORNER_LEFT_UPPER; // chart corner for anchoring
   const color            clr=clrRed;               // flat border color (Flat)
   const ENUM_LINE_STYLE  style=STYLE_SOLID;        // flat border style
   const int              line_width=1;             // flat border width
   const bool             back=false;               // in the background
   const bool             selection=false;          // highlight to move
   const bool             hidden=true;              // hidden in the object list
   const long             z_order=0;                // priority for mouse click

   //--- reset the error value
   ResetLastError();
   //--- create a rectangle label
   if(!ObjectCreate(chart_ID,name,OBJ_RECTANGLE_LABEL,sub_window,0,0))
   {
   Print(__FUNCTION__,
   ": failed to create a rectangle label! Error code = ",GetLastError());
   return(false);
   }
   //--- set label coordinates
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
   //--- set label size
   ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height);
   //--- set background color
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr);
   //--- set border type
   ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_TYPE,border);
   //--- set the chart's corner, relative to which point coordinates are defined
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
   //--- set flat border color (in Flat mode)
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
   //--- set flat border line style
   ObjectSetInteger(chart_ID,name,OBJPROP_STYLE,style);
   //--- set flat border width
   ObjectSetInteger(chart_ID,name,OBJPROP_WIDTH,line_width);
   //--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
   //--- enable (true) or disable (false) the mode of moving the label by mouse
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
   //--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
   //--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
   //--- successful execution
   return(true);
}
//+------------------------------------------------------------------+






//docs.mql4.com/constants/objectconstants/enum_object/obj_edit
//+------------------------------------------------------------------+
//| Create Edit object                                               |
//+------------------------------------------------------------------+
bool EditCreate(const string name,const int x,const int y,const int width,const int height,const string text)
{
   const long             chart_ID=0;               // chart's ID
   //const string           name="Edit";              // object name
   const int              sub_window=0;             // subwindow index
   //const int              x=55;                      // X coordinate
   //const int              y=70;                      // Y coordinate
   //const int              width=60;                 // width
   //const int              height=20;                // height
   //const string           text=DoubleToStr(MarketInfo(Symbol(),MODE_MINLOT),Digits);              // text
   const string           font="Arial";             // font
   const int              font_size=10;             // font size
   const ENUM_ALIGN_MODE  align=ALIGN_CENTER;       // alignment type
   const bool             read_only=false;          // ability to edit
   const ENUM_BASE_CORNER corner=CORNER_LEFT_UPPER; // chart corner for anchoring
   const color            clr=clrBlack;             // text color
   const color            back_clr=clrWhite;        // background color
   const color            border_clr=clrNONE;       // border color
   const bool             back=false;               // in the background
   const bool             selection=false;          // highlight to move
   const bool             hidden=true;              // hidden in the object list
   const long             z_order=0;                // priority for mouse click


   //--- reset the error value
   ResetLastError();
   //--- create edit field
   if(!ObjectCreate(chart_ID,name,OBJ_EDIT,sub_window,0,0))
   {
   Print(__FUNCTION__,
   ": failed to create \"Edit\" object! Error code = ",GetLastError());
   return(false);
   }
   //--- set object coordinates
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
   //--- set object size
   ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height);
   //--- set the text
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
   //--- set text font
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
   //--- set font size
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
   //--- set the type of text alignment in the object
   ObjectSetInteger(chart_ID,name,OBJPROP_ALIGN,align);
   //--- enable (true) or cancel (false) read-only mode
   ObjectSetInteger(chart_ID,name,OBJPROP_READONLY,read_only);
   //--- set the chart's corner, relative to which object coordinates are defined
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
   //--- set text color
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
   //--- set background color
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr);
   //--- set border color
   ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_COLOR,border_clr);
   //--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
   //--- enable (true) or disable (false) the mode of moving the label by mouse
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
   //--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
   //--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
   //--- successful execution
   return(true);
}
//+------------------------------------------------------------------+





//docs.mql4.com/constants/objectconstants/enum_object/obj_label
//+------------------------------------------------------------------+
//| Create a text label                                              |
//+------------------------------------------------------------------+
bool LabelCreate(const string name,const int x,const int y,const string text)
{
   const long              chart_ID=0;               // chart's ID
   //const string            name="Label";             // label name
   const int               sub_window=0;             // subwindow index
   //const int               x=0;                      // X coordinate
   //const int               y=0;                      // Y coordinate
   const ENUM_BASE_CORNER  corner=CORNER_LEFT_UPPER; // chart corner for anchoring
   //const string            text="Label";             // text
   const string            font="Arial";             // font
   const int               font_size=10;             // font size
   const color             clr=clrBlack;               // color
   const double            angle=0.0;                // text slope
   const ENUM_ANCHOR_POINT anchor=ANCHOR_LEFT_UPPER; // anchor type
   const bool              back=false;               // in the background
   const bool              selection=false;          // highlight to move
   const bool              hidden=true;              // hidden in the object list
   const long              z_order=0;                // priority for mouse click

   //--- reset the error value
   ResetLastError();
   //--- create a text label
   if(!ObjectCreate(chart_ID,name,OBJ_LABEL,sub_window,0,0))
   {
   Print(__FUNCTION__,
   ": failed to create text label! Error code = ",GetLastError());
   return(false);
   }
   //--- set label coordinates
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
   //--- set the chart's corner, relative to which point coordinates are defined
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
   //--- set the text
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
   //--- set text font
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
   //--- set font size
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
   //--- set the slope angle of the text
   ObjectSetDouble(chart_ID,name,OBJPROP_ANGLE,angle);
   //--- set anchor type
   ObjectSetInteger(chart_ID,name,OBJPROP_ANCHOR,anchor);
   //--- set color
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
   //--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
   //--- enable (true) or disable (false) the mode of moving the label by mouse
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
   //--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
   //--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
   //--- successful execution
   return(true);
}




//+------------------------------------------------------------------+
//| Create the button                                                |
//+------------------------------------------------------------------+
bool ButtonCreate(const string name,const int x,const int y,const string text,const color back_clr)
{
   const long              chart_ID=0;               // chart's ID
   //const string            name="Button";            // button name
   const int               sub_window=0;             // subwindow index
   //const int               x=0;                      // X coordinate
   //const int               y=0;                      // Y coordinate
   const int               width=60;                 // button width
   const int               height=30;                // button height
   const ENUM_BASE_CORNER  corner=CORNER_LEFT_UPPER; // chart corner for anchoring
   //const string            text="Button";            // text
   const string            font="Arial";             // font
   const int               font_size=10;             // font size
   const color             clr=clrWhite;             // text color
   //const color             back_clr=C'236,233,216';  // background color
   const color             border_clr=clrNONE;       // border color
   const bool              state=false;              // pressed/released
   const bool              back=false;               // in the background
   const bool              selection=false;          // highlight to move
   const bool              hidden=true;              // hidden in the object list
   const long              z_order=0;                // priority for mouse click

   //--- reset the error value
   ResetLastError();
   //--- create the button
   if(!ObjectCreate(chart_ID,name,OBJ_BUTTON,sub_window,0,0))
   {
      Print(__FUNCTION__,
      ": failed to create the button! Error code = ",GetLastError());
      return(false);
   }
   //--- set button coordinates
   ObjectSetInteger(chart_ID,name,OBJPROP_XDISTANCE,x);
   ObjectSetInteger(chart_ID,name,OBJPROP_YDISTANCE,y);
   //--- set button size
   ObjectSetInteger(chart_ID,name,OBJPROP_XSIZE,width);
   ObjectSetInteger(chart_ID,name,OBJPROP_YSIZE,height);
   //--- set the chart's corner, relative to which point coordinates are defined
   ObjectSetInteger(chart_ID,name,OBJPROP_CORNER,corner);
   //--- set the text
   ObjectSetString(chart_ID,name,OBJPROP_TEXT,text);
   //--- set text font
   ObjectSetString(chart_ID,name,OBJPROP_FONT,font);
   //--- set font size
   ObjectSetInteger(chart_ID,name,OBJPROP_FONTSIZE,font_size);
   //--- set text color
   ObjectSetInteger(chart_ID,name,OBJPROP_COLOR,clr);
   //--- set background color
   ObjectSetInteger(chart_ID,name,OBJPROP_BGCOLOR,back_clr);
   //--- set border color
   ObjectSetInteger(chart_ID,name,OBJPROP_BORDER_COLOR,border_clr);
   //--- display in the foreground (false) or background (true)
   ObjectSetInteger(chart_ID,name,OBJPROP_BACK,back);
   //--- set button state
   ObjectSetInteger(chart_ID,name,OBJPROP_STATE,state);
   //--- enable (true) or disable (false) the mode of moving the button by mouse
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTABLE,selection);
   ObjectSetInteger(chart_ID,name,OBJPROP_SELECTED,selection);
   //--- hide (true) or display (false) graphical object name in the object list
   ObjectSetInteger(chart_ID,name,OBJPROP_HIDDEN,hidden);
   //--- set the priority for receiving the event of a mouse click in the chart
   ObjectSetInteger(chart_ID,name,OBJPROP_ZORDER,z_order);
   //--- successful execution
   return(true);
}