//+------------------------------------------------------------------+
//|                              Demo_Create_OBJ_BITMAP_LABEL_EA.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

string label_name="currency_label";        // OBJ_BITMAP_LABEL object name 
string euro      ="\\Images\\euro.bmp";    // path: terminal_folder\MQL5\Images\euro.bmp
string dollar    ="\\Images\\dollar.bmp";  // path: terminal_folder\MQL5\Images\dollar.bmp
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create the button of OBJ_BITMAP_LABEL type, if not found
   if(ObjectFind(0,label_name)<0)
     {
      //--- try to create an object of OBJ_BITMAP_LABEL type
      bool created=ObjectCreate(0,label_name,OBJ_BITMAP_LABEL,0,0,0);
      if(created)
        {
         //--- base corner
         ObjectSetInteger(0,label_name,OBJPROP_CORNER,CORNER_RIGHT_UPPER);
         //--- set object properties
         ObjectSetInteger(0,label_name,OBJPROP_XDISTANCE,100);
         ObjectSetInteger(0,label_name,OBJPROP_YDISTANCE,50);
         //--- reset last error code
         ResetLastError();
         //--- load the image for "Pressed" state
         bool set=ObjectSetString(0,label_name,OBJPROP_BMPFILE,0,euro);
         //--- check result
         if(!set)
           {
            PrintFormat("Error loading image from file %s. Error code %d",euro,GetLastError());
           }
         ResetLastError();
         //--- load the image for "Unpressed" state
         set=ObjectSetString(0,label_name,OBJPROP_BMPFILE,1,dollar);

         if(!set)
           {
            PrintFormat("Error loading image from file %s. Error code %d",dollar,GetLastError());
           }
         //--- redraw the chart to draw the button immediately
         ChartRedraw(0);
        }
      else
        {
         //--- error creating object, print message
         PrintFormat("Error creating object of OBJ_BITMAP_LABEL type. Error code %d",GetLastError());
        }
     }
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//--- delete the object from the chart
   ObjectDelete(0,label_name);
  }
//+------------------------------------------------------------------+
