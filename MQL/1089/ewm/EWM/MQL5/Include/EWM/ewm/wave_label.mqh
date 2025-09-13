//+------------------------------------------------------------------+
//|                                                   wave_label.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsTxtControls.mqh>
#include <EWM\defines.mqh>
#include <EWM\functions.mqh>
#include <Charts\Chart.mqh>
#include <EWM\ewm\descrs.mqh>
#include "horizontal.mqh"
#include "vertical.mqh"
#include "price_label.mqh"

// the class of the wave label
class Wave_label : public CChartObjectText
{
  private:
    bool pos;
    double price;
    datetime time;
  public:
    Horizontal horizontal;
    Vertical vertical;
    Price_label price_label;
    // create the wave label
    void create(string name, int x, int y);
    void create(string name, string text, datetime time, double price);
    datetime get_time();          // get the wave label time on the current timeframe
    void set_time(datetime time); // set the wave label time on the current timeframe
    double get_price();           // get the wave label price
    void set_price(double price); // set the wave label price
    bool get_pos();               // get the wave label position (above/below the bar)
    void set_pos(bool pos);       // set the wave label position (above/under bar)
    int get_level();              // get the wave label level
    string get_group();           // get a group of wave label
    string get_unique_name();     // get the name of the label on which it is identified
    int get_time_create();        // get the wave label creation time
    int get_size_in_px();         // get the wave label height in pixels
    int get_font_size();          // get the wave label font size
    color get_color();            // get the color of the wave label
    string get_font();            // get the wave label font
    string get_text();            // get the wave label text
    string get(int param);        // select the required parts from the wave label name
    void move(const int every);   // move the wave label in a minute chart
    void up_level();              // increase the wave label level
    void down_level();            // decrease the wave label level
    int index();                  // find a wave label index in the wave_labels array
    void update();                // update time, position, price
    void copy(long id);
};

void Wave_label::update()
{
  Description(get_text());
  Descr *descr = descrs.At(get_level());
  Tooltip(Description() + ": " + descr.level);
  Font(get_font());
  FontSize(get_font_size());
  Color(get_color());
  Z_Order(100);
  double highs[], lows[];
  // set a label position (above/below the bar)
  while(CopyHigh(_Symbol, PERIOD_M1, Time(0), 1, highs) != 1 ||
        CopyLow(_Symbol, PERIOD_M1, Time(0), 1, lows) != 1)
  {
    Sleep(DELAY);
  }
  if(Price(0) >= highs[0] - (highs[0] - lows[0]) / 2)
  {
    set_pos(TOP);
  }
  else if(Price(0) < lows[0] + (highs[0] - lows[0]) / 2)
  {
    set_pos(BOTTOM);
  }
  // set the label price on the current timeframe
  while(CopyHigh(_Symbol, _Period, Time(0), 1, highs) != 1 ||
        CopyLow(_Symbol, _Period, Time(0), 1, lows) != 1)
  {
    Sleep(DELAY);
  }
  if(get_pos())
  {
    set_price(highs[0]);
  }
  else
  {
    set_price(lows[0]);
  }
  datetime times[];
  // set the label time on the current timeframe
  while(CopyTime(_Symbol, PERIOD_M1, Time(0), 1, times) != 1)
  {
    Sleep(DELAY);
  }
  if(Time(0) > times[0])
  {
    set_time(Time(0));
  }
  else
  {
    while(CopyTime(_Symbol, _Period, Time(0), 1, times) != 1)
    {
      Sleep(DELAY);
    }
    set_time(times[0]);
  }
  vertical.update(Name(), Time(0), get_pos(), Timeframes());
  horizontal.update(Name(), Time(0), get_price(), get_pos(), Timeframes());
  price_label.update(Name(), Time(0), get_price(), get_pos(), Timeframes());
}

void Wave_label::create(string name, int x, int y)
{
  double price;
  datetime time;
  get_coordinates(x, y, price, time);
  Create(0, name, 0, time, price);
  Description(get_text());
  Descr *descr = descrs.At(get_level());
  Tooltip(Description() + ": " + descr.level);
  Selectable(true);
  Timeframes(get_current_tf());
  Anchor(ANCHOR_CENTER);
  Font(get_font());
  FontSize(get_font_size());
  Color(get_color());
  Z_Order(100);
}

void Wave_label::create(string name, string text, datetime time, double price)
{
  Create(0, name, 0, time, price);
  Description(text);
  Selectable(true);
  Timeframes(get_current_tf());
  Descr *descr = descrs.At(get_level());
  Tooltip(Description() + ": " + descr.level);
  Anchor(ANCHOR_CENTER);
  Font(get_font());
  FontSize(get_font_size());
  Color(get_color());
}

datetime Wave_label::get_time()
{
  return(time);
}

void Wave_label::set_time(datetime time)
{
  this.time = time;
}

double Wave_label::get_price()
{
  return(price);
}

void Wave_label::set_price(double price)
{
  this.price = price;
}

bool Wave_label::get_pos()
{
  return(pos);
}

void Wave_label::set_pos(bool pos)
{
  this.pos = pos;
}

int Wave_label::get_level()
{
  return((int)get(LEVEL));
}

string Wave_label::get_group()
{
  return(get(GROUP));
}

string Wave_label::get_unique_name()
{
  return(get(UNIQUE_NAME));
}

int Wave_label::get_size_in_px()
{
  Descr *descr = descrs.At(get_level());
  return(descr.size_in_px);
}

int Wave_label::get_font_size()
{
  Descr *descr = descrs.At(get_level());
  return(descr.font_size);
}

color Wave_label::get_color()
{
  Descr *descr = descrs.At(get_level());
  return(descr.clr);
}

string Wave_label::get_font()
{
  Descr *descr = descrs.At(get_level());
  return(descr.font);
}

string Wave_label::get_text()
{
  return(get(TEXT));
}

string Wave_label::get(int param)
{
  string results[];
  StringSplit(Name(), '_', results);
  return(results[param]);
}

void Wave_label::move(const int every = NOT_EVERY_BAR)
{
  datetime times[];
  while(CopyTime(_Symbol, PERIOD_M1, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  if(Time(0) > times[0])
  {
    set_time(Time(0));
    set_price(Price(0));
    update();
    return;
  }
  MqlRates rts[], temp_rts[];
  CChart chart;
  chart.Attach(0);
  int visible_bars = (int) ChartGetInteger(0, CHART_VISIBLE_BARS);
  int first_visible_bars = chart.FirstVisibleBar();
  chart.Detach();
  int i, bars;
  if(every == NOT_EVERY_BAR)
  {
    while(CopyTime(_Symbol, _Period, first_visible_bars, 1, times) != 1)
    {
      Sleep(DELAY);
    }
    datetime time1 = times[0];
    while(CopyTime(_Symbol, _Period, first_visible_bars - visible_bars + 1, 1, times) != 1)
    {
      Sleep(DELAY);
    }
    datetime time2 = times[0];
    bars = Bars(_Symbol, _Period, time1, time2);
    i = bars - Bars(_Symbol, _Period, Time(0), time2);
    while(CopyRates(_Symbol, _Period, time1, time2, rts) != bars)
    {
      Sleep(DELAY);
    }
  }
  else
  {
    bars = Bars(_Symbol, _Period);
    i = bars - Bars(_Symbol, _Period, Time(0), times[0]);
    while(CopyRates(_Symbol, _Period, 0, bars, rts) != bars)
    {
      Sleep(DELAY);
    }
  }
  if(Price(0) >= rts[i].high - (rts[i].high - rts[i].low) / 2)
  {
    set_pos(TOP);
    set_price(rts[i].high);
  }
  else if(Price(0) <= rts[i].low + (rts[i].high - rts[i].low) / 2)
  {
    set_pos(BOTTOM);
    set_price(rts[i].low);
  }
  set_time(rts[i].time);
  if(every == NOT_EVERY_BAR)
  {
    for(int j = 0; j < MathMax(i, bars - i); j++)
    {
      if(get_pos() && ((i - j - 1 >= 0 && i - j + 1 <= bars - 1 &&
         rts[i - j].high >= rts[i - j - 1].high &&
         rts[i - j].high >= rts[i - j + 1].high) ||
         (i - j == 0 && i - j + 1 <= bars - 1 && rts[i - j].high >= rts[i - j + 1].high)))
      {
        set_time(rts[i - j].time);
        set_price(rts[i - j].high);
        i = i - j;
        break;
      }
      else if(get_pos() && ((i + j + 1 <= bars - 1 && i + j - 1 >= 0 &&
              rts[i + j].high >= rts[i + j - 1].high &&
              rts[i + j].high >= rts[i + j + 1].high) ||
              (i + j == bars - 1 && i + j - 1 >= 0 && rts[i + j].high >= rts[i + j - 1].high)))
      {
        set_time(rts[i + j].time);
        set_price(rts[i + j].high);
        i = i + j;
        break;
      }
      else if( ! get_pos() && ((i - j - 1 >= 0 && i - j + 1 <= bars - 1 &&
              rts[i - j].low <= rts[i - j - 1].low &&
              rts[i - j].low <= rts[i - j + 1].low) ||
              (i - j == 0 && i - j + 1 <= bars - 1 && rts[i - j].low <= rts[i - j + 1].low)))
      {
        set_time(rts[i - j].time);
        set_price(rts[i - j].low);
        i = i - j;
        break;
      }
      else if( ! get_pos() && ((i + j + 1 <= bars - 1 && i + j - 1 >= 0 &&
              rts[i + j].low <= rts[i + j - 1].low &&
              rts[i + j].low <= rts[i + j + 1].low) ||
              (i + j == bars - 1 && i + j - 1 >= 0 && rts[i + j].low <= rts[i + j - 1].low)))
      {
        set_time(rts[i + j].time);
        set_price(rts[i + j].low);
        i = i + j;
        break;
      }
    }
  }
  if(i + 1 == bars)
  {
    while(CopyTime(_Symbol, PERIOD_M1, 0, 1, times) != 1)
    {
      Sleep(DELAY);
    }
    bars = Bars(_Symbol, PERIOD_M1, rts[i].time, times[0]);
    while(CopyRates(_Symbol, PERIOD_M1, rts[i].time, times[0], temp_rts) != bars)
    {
      Sleep(DELAY);
    }
  }
  else
  {
    bars = Bars(_Symbol, PERIOD_M1, rts[i].time, rts[i + 1].time);
    while(CopyRates(_Symbol, PERIOD_M1, rts[i].time, rts[i + 1].time, temp_rts) != bars)
    {
      Sleep(DELAY);
    }
  }
  for(i = 0; i < bars; i++)
  {
    if(get_pos() && get_price() == temp_rts[i].high)
    {
      Time(0, temp_rts[i].time);
      break;
    }
    else if( ! get_pos() && get_price() == temp_rts[i].low)
    {
      Time(0, temp_rts[i].time);
      break;
    }
  }
  update();
}

void Wave_label::up_level()
{
  if(get_level() + 1 <= descrs.Total() - 1)
  { 
    Descr *descr = descrs.At(get_level() + 1);
    Name((string)(get_level() + 1) + "_" + descr.labels[index()] + "_" + get_group() + "_" + get_unique_name());
    update();
  }
}

void Wave_label::down_level()
{
  if(get_level() - 1 >= 0)
  {
    Descr *descr = descrs.At(get_level() - 1);
    Name((string)(get_level() - 1) + "_" + descr.labels[index()] + "_" + get_group() + "_" + get_unique_name());
    update();
  }
}

int Wave_label::index()
{
  int i = 0;
  Descr *descr = descrs.At(get_level());
  while(i < LABELS)
  {
    if(descr.labels[i] == get_text())
    {
      break;
    }
    i++;
  }
  return(i);
}
