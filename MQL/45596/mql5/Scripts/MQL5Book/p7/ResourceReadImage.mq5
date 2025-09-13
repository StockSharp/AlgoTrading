//+------------------------------------------------------------------+
//|                                            ResourceReadImage.mq5 |
//|                                  Copyright 2022, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright   "Copyright 2022, MetaQuotes Ltd."
#property link        "https://www.mql5.com"
#property description "Runs the script 3 times to consequentially go through 3 steps/states:"
"1. prepare original and modified bitmaps for On/Off states;"
"2. create an object on a chart to draw the bitmaps (clickable to switch between On/Off);"
"3. delete the objects and resources;"
// NB: descriptions are not visible in UI for scripts unless
// the following directive is specified: #property script_show_inputs

#include <MQL5Book/PRTF.mqh>

//+------------------------------------------------------------------+
//| Helper function to setup an object for bitmaps drawing           |
//+------------------------------------------------------------------+
void ShowBitmap(const string name, const string resourceOn, const string resourceOff = NULL)
{
   ObjectCreate(0, name, OBJ_BITMAP_LABEL, 0, 0, 0);
   
   ObjectSetString(0, name, OBJPROP_BMPFILE, 0, resourceOn);
   if(resourceOff != NULL) ObjectSetString(0, name, OBJPROP_BMPFILE, 1, resourceOff);
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, 50);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, 50);
   ObjectSetInteger(0, name, OBJPROP_CORNER, CORNER_RIGHT_LOWER);
   ObjectSetInteger(0, name, OBJPROP_ANCHOR, ANCHOR_RIGHT_LOWER);
}

//+------------------------------------------------------------------+
//| Helper function to create an image with inverted colors          |
//+------------------------------------------------------------------+
bool ResourceCreateInverted(const string resource, const string inverted)
{
   uint data[], width, height;
   PRTF(ResourceReadImage(resource, data, width, height));
   for(int i = 0; i < ArraySize(data); ++i)
   {
      data[i] = data[i] ^ 0x00FFFFFF;
   }
   return PRTF(ResourceCreate(inverted, data, width, height, 0, 0, 0,
      COLOR_FORMAT_ARGB_NORMALIZE));
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const static string resource = "::Images\\pseudo.bmp";
   const static string inverted = resource + "_inv";
   const static string object = "object";
   
   Print(""); // use empty line just as logging delimiter
   
   uint data[], width, height;
   // check for resource existence
   if(!PRTF(ResourceReadImage(resource, data, width, height)))
   {
      Print("Initial state: Creating 2 bitmaps");
      PRTF(ResourceCreate(resource, "\\Images\\dollar.bmp")); // try local "argb.bmp" as well
      ResourceCreateInverted(resource, inverted);
   }
   else
   {
      Print("Resources (bitmaps) are detected");
      if(PRTF(ObjectFind(0, object) < 0))
      {
         Print("Active state: Creating object to draw 2 bitmaps");
         ShowBitmap(object, resource, inverted);
      }
      else
      {
         Print("Cleanup state: Removing object and resources");
         PRTF(ObjectDelete(0, object));
         PRTF(ResourceFree(resource));
         PRTF(ResourceFree(inverted));
      }
   }
}
//+------------------------------------------------------------------+
/*
   1-st run output
   
   ResourceReadImage(resource,data,width,height)=false / RESOURCE_NOT_FOUND(4016)
   Initial state: Creating 2 bitmaps
   ResourceCreate(resource,\Images\dollar.bmp)=true / ok
   ResourceReadImage(resource,data,width,height)=true / ok
   ResourceCreate(inverted,data,width,height,0,0,0,COLOR_FORMAT_XRGB_NOALPHA)=true / ok

   2-nd run output
   
   ResourceReadImage(resource,data,width,height)=true / ok
   Resources (bitmaps) are detected
   ObjectFind(0,object)<0=true / OBJECT_NOT_FOUND(4202)
   Active state: Creating object to draw 2 bitmaps

   3-rd run output

   ResourceReadImage(resource,data,width,height)=true / ok
   Resources (bitmaps) are detected
   ObjectFind(0,object)<0=false / ok
   Cleanup state: Removing object and resources
   ObjectDelete(0,object)=true / ok
   ResourceFree(resource)=true / ok
   ResourceFree(inverted)=true / ok

*/
//+------------------------------------------------------------------+
