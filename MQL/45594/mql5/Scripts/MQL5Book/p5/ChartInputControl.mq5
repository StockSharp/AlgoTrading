//+------------------------------------------------------------------+
//|                                            ChartInputControl.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/ChartModeMonitor.mqh>

input bool ContextMenu = true;     // CHART_CONTEXT_MENU
input bool CrossHairTool = true;   // CHART_CROSSHAIR_TOOL
input bool MouseScroll = true;     // CHART_MOUSE_SCROLL
input bool KeyboardControl = true; // CHART_KEYBOARD_CONTROL
input bool QuickNavigation = true; // CHART_QUICK_NAVIGATION
input bool DragTradeLevels = true; // CHART_DRAG_TRADE_LEVELS

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const bool Inputs[] =
   {
      ContextMenu, CrossHairTool, MouseScroll,
      KeyboardControl, QuickNavigation, DragTradeLevels
   };
   const int flags[] =
   {
      CHART_CONTEXT_MENU, CHART_CROSSHAIR_TOOL, CHART_MOUSE_SCROLL,
      CHART_KEYBOARD_CONTROL, CHART_QUICK_NAVIGATION, CHART_DRAG_TRADE_LEVELS
   };

   ChartModeMonitor m(flags);
   Print("Initial state:");
   m.print();
   m.backup();

   // change chart controls according to user input
   for(int i = 0; i < ArraySize(flags); ++i)
   {
      ChartSetInteger(0, (ENUM_CHART_PROPERTY_INTEGER)flags[i], Inputs[i]);
   }

   while(!IsStopped())
   {
      m.snapshot(); // ??? can user change CHART_DRAG_TRADE_LEVELS?
      Sleep(500);   //     service desk #3292582
   }
   m.restore();
}
//+------------------------------------------------------------------+
/*

   Initial state:
       [key] [value]
   [0]    50       1
   [1]    49       1
   [2]    42       1
   [3]    47       1
   [4]    45       1
   [5]    43       1
   CHART_CONTEXT_MENU 1 -> 0
   CHART_CROSSHAIR_TOOL 1 -> 0
   CHART_MOUSE_SCROLL 1 -> 0
   CHART_KEYBOARD_CONTROL 1 -> 0

*/
//+------------------------------------------------------------------+
