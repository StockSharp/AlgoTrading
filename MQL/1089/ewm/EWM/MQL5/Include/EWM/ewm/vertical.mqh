//+------------------------------------------------------------------+
//|                                                     vertical.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsLines.mqh>
#include <EWM\functions.mqh>
#include <EWM\defines.mqh>

class Vertical : public CChartObjectVLine
{
  public:
    void create(string wave_label_name, datetime time, bool pos, int tf);
    void update(string wave_label_name, datetime time, bool pos, int tf);
};

void Vertical::create(string wave_label_name, datetime time, bool pos, int tf)
{
  Create(0, replace(wave_label_name, UNIQUE_NAME, NAME_VERTICAL), 0, time);
  Color(pos == false ? clrRed : clrBlue);
  Style(STYLE_DOT);
  Timeframes(tf);
}

void Vertical::update(string wave_label_name, datetime time, bool pos, int tf)
{
  Name(replace(wave_label_name, UNIQUE_NAME, NAME_VERTICAL));
  Time(0, time);
  Color(pos == false ? clrRed : clrBlue);
  Style(STYLE_DOT);
  Timeframes(tf);
}
