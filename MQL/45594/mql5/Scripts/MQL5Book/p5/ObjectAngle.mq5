//+------------------------------------------------------------------+
//|                                                  ObjectAngle.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//|                                                                  |
//| The script creates and periodically updates a text object        |
//| rotating it by different angles.                                 |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // create and setup text object in the center of the window
   const string name = "ObjAngle";
   ObjectCreate(0, name, OBJ_LABEL, 0, 0, 0);
   const int centerX = (int)ChartGetInteger(0, CHART_WIDTH_IN_PIXELS) / 2;
   const int centerY = (int)ChartGetInteger(0, CHART_HEIGHT_IN_PIXELS) / 2;
   ObjectSetInteger(0, name, OBJPROP_XDISTANCE, centerX);
   ObjectSetInteger(0, name, OBJPROP_YDISTANCE, centerY);
   ObjectSetInteger(0, name, OBJPROP_ANCHOR, ANCHOR_CENTER);
   
   const int key = TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK);
   
   int angle = 0;
   
   while(!IsStopped())
   {
      // proceed if user did not pause animation by pressing ScrollLock
      if(TerminalInfoInteger(TERMINAL_KEYSTATE_SCRLOCK) == key)
      {
         // show angle in the text and rotate it accordingly
         ObjectSetString(0, name, OBJPROP_TEXT, StringFormat("Angle: %d°", angle));
         ObjectSetDouble(0, name, OBJPROP_ANGLE, angle);
         angle += 45;
       
         ChartRedraw();
      }
      Sleep(1000);
   }
   ObjectDelete(0, name);
}
//+------------------------------------------------------------------+
