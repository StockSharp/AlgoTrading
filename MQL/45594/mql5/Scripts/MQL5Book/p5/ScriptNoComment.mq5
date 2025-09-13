//+------------------------------------------------------------------+
//|                                              ScriptNoComment.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property description "Clean up comments in the upper left corner of the current chart"
/*
   Uncomment one of the next lines to see
   how it enforces confirmation message box or
   properties dialog during start-up
*/
//#property script_show_confirm
//#property script_show_inputs

input string Text = "";

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Comment(Text);
}
//+------------------------------------------------------------------+
