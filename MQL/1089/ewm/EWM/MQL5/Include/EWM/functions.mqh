//+------------------------------------------------------------------+
//|                                                    functions.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <EWM\defines.mqh>
#include <Charts\Chart.mqh>

int get_current_tf()
{
  int i = 0;
  while(i < ArrayRange(periods, 0))
  {
    if(_Period == periods[i])
    {
      break;
    }
    i++;
  }
  return(tfs[i]);
}

datetime bar_to_time(int shift)
{
  int bars = Bars(_Symbol, _Period);
  datetime times[];
  while(CopyTime(_Symbol, _Period, 0, bars, times) != bars)
  {
    Sleep(DELAY);
  }
  datetime time;
  if(shift >= bars)
  {
    time = times[bars - 1] + (shift - bars + 1) * PeriodSeconds(_Period);
  }
  else
  {
    time = times[shift];
  }
  return(time);
}

void get_coordinates(int x, int y, double &price, datetime &time)
{
  CChart chart;
  chart.Attach(0);
  int chart_visible_bars = (int)ChartGetInteger(0, CHART_VISIBLE_BARS);
  if(y - SHIFT_Y < 0)
  {
    y = 0;
  }
  else if(y - SHIFT_Y >= chart.HeightInPixels(0))
  {
    y = chart.HeightInPixels(0) - 1;
  }
  else
  {
    y -= SHIFT_Y;
  }
  if(x - SHIFT_X < 0)
  {
    x = 0;
  }
  else if(x - SHIFT_X >= chart.WidthInPixels())
  {
    x = chart.WidthInPixels() - 1;
  }
  else
  {
    x -= SHIFT_X;
  }
  price = chart.PriceMax(0) - ((chart.PriceMax(0) - chart.PriceMin(0)) / chart.HeightInPixels(0)) * y;
  int pos = Bars(_Symbol, _Period) - 1 - chart.FirstVisibleBar() + (int)MathRound(x * chart.WidthInBars() / (double)chart.WidthInPixels());
  time = bar_to_time(pos);
  chart.Detach();
}

int get_level(string name)
{
  return((int)get(name, LEVEL));
}

string get_text(string name)
{
  return(get(name, TEXT));
}

string get_group(string name)
{
  return(get(name, GROUP));
}

string get_unique_name(string name)
{
  return(get(name, UNIQUE_NAME));
}

string get(string name, int type)
{
  string results[];
  StringSplit(name, StringGetCharacter(SEPARATOR, 0), results);
  return(results[type]);
}

string replace(string name, int type, string replacement)
{
  string results[];
  StringSplit(name, StringGetCharacter(SEPARATOR, 0), results);
  results[type] = replacement;
  name = results[0];
  for(int i = 1; i < ArrayRange(results, 0); i++)
  {
    name += SEPARATOR + results[i];
  }
  return(name);
}
