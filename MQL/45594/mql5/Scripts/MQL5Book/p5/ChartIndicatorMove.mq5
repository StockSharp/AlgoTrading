//+------------------------------------------------------------------+
//|                                           ChartIndicatorMove.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

//+------------------------------------------------------------------+
//| Move direction                                                   |
//+------------------------------------------------------------------+
enum DIRECTION
{
   Up = -1,
   Down = +1,
};

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input DIRECTION MoveDirection = Up;
input bool JumpOver = true;

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   const int w = ChartWindowOnDropped();
   if(w == 0 && MoveDirection == Up)
   {
      Alert("Can't move up from window at index 0");
      return;
   }
   const int n = ChartIndicatorsTotal(0, w);
   for(int i = 0; i < n; ++i)
   {
      const string name = ChartIndicatorName(0, w, i);
      const string caption = EnumToString(MoveDirection);
      const int button = MessageBox("Move '" + name + "' " + caption + "?",
         caption, MB_YESNOCANCEL);
      if(button == IDCANCEL) break;
      if(button == IDYES)
      {
         const int h = ChartIndicatorGet(0, w, name);
         ChartIndicatorAdd(0, w + MoveDirection * (JumpOver + 1), h);
         ChartIndicatorDelete(0, w, name);
         IndicatorRelease(h);
         break;
      }
   }
}
//+------------------------------------------------------------------+
