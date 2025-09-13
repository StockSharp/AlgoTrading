//+------------------------------------------------------------------+
//|                                             Demo_resource_EA.mq5 |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2011, MetaQuotes Software Corp."
#property link      "http://www.mql5.com"
#property version   "1.00"

#resource "\\Images\\euro.bmp";    // euro.bmp located in client_terminal_data_folder\MQL5\Images\
#resource "\\Images\\dollar.bmp";  // dollar.bmp located in client_terminal_data_folder\MQL5\Images\

string label_name="currency_label";        // OBJ_BITMAP_LABEL object name
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//--- create a button of OBJ_BITMAP_LABEL type, if it not exist
   if(ObjectFind(0,label_name)<0)
     {
      //--- try to create an object of OBJ_BITMAP_LABEL type
      bool created=ObjectCreate(0,label_name,OBJ_BITMAP_LABEL,0,0,0);
      if(created)
        {
         //--- bind it to upper right corner of the chart
         ObjectSetInteger(0,label_name,OBJPROP_CORNER,CORNER_RIGHT_UPPER);
         //--- define object properties
         ObjectSetInteger(0,label_name,OBJPROP_XDISTANCE,100);
         ObjectSetInteger(0,label_name,OBJPROP_YDISTANCE,50);
         //--- reset last error
         ResetLastError();
         //--- load image for "Pressed" state of the button
         bool set=ObjectSetString(0,label_name,OBJPROP_BMPFILE,0,"::Images\\euro.bmp");
         //--- check result
         if(!set)
           {
            PrintFormat("Error in loading resource %s. Error code %d","Images\\euro.bmp",GetLastError());
           }
         ResetLastError();
         //--- load image for "Released" state of the button
         set=ObjectSetString(0,label_name,OBJPROP_BMPFILE,1,"::Images\\dollar.bmp");

         if(!set)
           {
            PrintFormat("Error in loading resource %s. Error code %d","Images\\dollar.bmp",GetLastError());
           }
         //--- chart redraw (for not waiting for a new tick)
         ChartRedraw(0);
        }
      else
        {
         //--- object is not created, show error
         PrintFormat("The object of OBJ_BITMAP_LABEL is not created. Error code %d",GetLastError());
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
//--- delete object from chart
   ObjectDelete(0,label_name);
  }
//+------------------------------------------------------------------+

/*
//--- correct resource definition
#resource "\\Images\\euro.bmp" // if the euro.bmp is located in client_terminal_data_folder\MQL5\Images\
#resource "picture.bmp"        // if the picture.bmp is located in the same folder, as the program

//--- incorrect resource definition
#resource ":picture_2.bmp"     // The ":" cannot be used
#resource "..\\picture_3.bmp"  // The ".." cannot be used
#resource "\\Files\\Images\\Folder_First\\My_panel\\Labels\\too_long_path.bmp"

*/
//+------------------------------------------------------------------+
