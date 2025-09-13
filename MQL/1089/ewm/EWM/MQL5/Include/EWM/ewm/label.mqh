//+------------------------------------------------------------------+
//|                                                        label.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsTxtControls.mqh>
#include <EWM\defines.mqh>
#include <EWM\functions.mqh>
#include <EWM\ewm\descrs.mqh>

// the class of the wave label
class Label : public CChartObjectLabel
{
  public:
    void create(string name, int x, int y, bool selectable, ENUM_ANCHOR_POINT anchor, int timeframe, bool b = true);
    int get_font_size();      // get the font size of the label
    color get_color();        // get the label color
    string get_font();        // get the label font
};

void Label::create(string name, int x, int y, bool selectable, ENUM_ANCHOR_POINT anchor, int timeframe, bool b = true)
{
  Create(0, name, 0, x, y);
  Selectable(selectable);
  Timeframes(timeframe);
  Anchor(anchor);
  Z_Order(100);
  if(b)
  {
    Description(get_text(Name()));
    Font(get_font());
    FontSize(get_font_size());
    Color(get_color());
  }
  Descr *descr = descrs.At(get_level(Name()));
  Tooltip(Description() + ": " + descr.level);
}

int Label::get_font_size()
{
  Descr *descr = descrs.At(get_level(Name()));
  return(descr.font_size);
}

color Label::get_color()
{
  Descr *descr = descrs.At(get_level(Name()));
  return(descr.clr);
}

string Label::get_font()
{
  Descr *descr = descrs.At(get_level(Name()));
  return(descr.font);
}
