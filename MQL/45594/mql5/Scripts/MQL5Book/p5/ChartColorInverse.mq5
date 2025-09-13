//+------------------------------------------------------------------+
//|                                            ChartColorInverse.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define RGB_INVERSE(C) ((color)C ^ 0xFFFFFF)

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ENUM_CHART_PROPERTY_INTEGER colors[] =
   {
      CHART_COLOR_BACKGROUND,
      CHART_COLOR_FOREGROUND,
      CHART_COLOR_GRID,
      CHART_COLOR_VOLUME,
      CHART_COLOR_CHART_UP,
      CHART_COLOR_CHART_DOWN,
      CHART_COLOR_CHART_LINE,
      CHART_COLOR_CANDLE_BULL,
      CHART_COLOR_CANDLE_BEAR,
      CHART_COLOR_BID,
      CHART_COLOR_ASK,
      CHART_COLOR_LAST,
      CHART_COLOR_STOP_LEVEL
   };
   
   for(int i = 0; i < ArraySize(colors); ++i)
   {
      ChartSetInteger(0, colors[i], RGB_INVERSE(ChartGetInteger(0, colors[i])));
   }
}
//+------------------------------------------------------------------+
