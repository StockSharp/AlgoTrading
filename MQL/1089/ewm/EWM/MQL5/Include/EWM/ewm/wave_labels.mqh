//+------------------------------------------------------------------+
//|                                                  wave_labels.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Arrays\ArrayObj.mqh>
#include <Charts\Chart.mqh>
#include <EWM\defines.mqh>
#include "wave_label.mqh"
#include <EWM\functions.mqh>
#include "label.mqh"

class Wave_labels : public CArrayObj
{
  public:
    int min_level;
    bool select;
    bool select_group;
    string group;
    int max_group;
    void Wave_labels()
    {
      min_level = -1;
      select = false;
      select_group = false;
      group = "-1";
    }
    void sort();
    void correct();
    void adapt();
    void hide(int level);
    void change_level(bool change);
    Wave_label *get(string name);
    int get_selected();
    void click(string name);
    void on_hide_trend_lines();
    void on_hide_price_labels();
    void on_hide_verticals();
    void set_max_group();
    int get_min_level();
    int get_max_level();
};

void Wave_labels::sort()
{
  FreeMode(false);
  if(Total() < 2)
  {
    return;
  }
  bool exchange = true;
  while(exchange)
  {
    exchange = false;
    for(int i = 0; i < Total() - 1; i++)
    {
      Wave_label *wave_label = At(i);
      Wave_label *temp_wave_label = At(i + 1);
      // sort labels by time, position and level
      if((wave_label.get_time() > temp_wave_label.get_time()) ||
         (wave_label.get_time() == temp_wave_label.get_time() && wave_label.get_pos() < temp_wave_label.get_pos()) ||
         (wave_label.get_time() == temp_wave_label.get_time() && wave_label.get_pos() == temp_wave_label.get_pos() && wave_label.get_level() > temp_wave_label.get_level()))
      {
        Update(i, temp_wave_label);
        Update(i + 1, wave_label);
        exchange = true;
      }
    }
  }
  FreeMode(true);
}

void Wave_labels::correct()
{
  CChart chart;
  chart.Attach(0);
  if(chart.HeightInPixels(0) == 0)
  {
    return;
  }
  double price_in_px = (chart.PriceMax(0) - chart.PriceMin(0)) / chart.HeightInPixels(0);
  chart.Detach();
  datetime times[];
  while(CopyTime(_Symbol, PERIOD_M1, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  for(int i = 0; i < Total(); i++)
  {
    Wave_label *wave_label = At(i), *temp_wave_label;
    if(wave_label.Timeframes() != get_current_tf() || wave_label.Time(0) > times[0])
    {
      continue;
    }
    double price = wave_label.get_price();
    if(wave_label.get_pos())
    {
      price += wave_label.get_size_in_px() / 2.0 * price_in_px;
    }
    else
    {
      price -= wave_label.get_size_in_px() / 2.0 * price_in_px; 
    }
    wave_label.Price(0, price);
    for(int j = i + 1; j < Total(); j++)
    {
      temp_wave_label = At(j);
      if(temp_wave_label.get_time() == wave_label.get_time() && temp_wave_label.get_pos() == wave_label.get_pos())
      {
        if(wave_label.Timeframes() != get_current_tf())
        {
          wave_label = temp_wave_label;
          i++;
          continue;
        }
        if(wave_label.get_pos())
        {
          price += (temp_wave_label.get_size_in_px() + wave_label.get_size_in_px()) / 2.0 * price_in_px;
        }
        else
        {
          price -= (temp_wave_label.get_size_in_px() + wave_label.get_size_in_px()) / 2.0 * price_in_px;
        }
        temp_wave_label.Price(0, price);
        wave_label = temp_wave_label;
        i++;
      }
    }
  }
}

void Wave_labels::adapt()
{
  min_level = get_min_level() - 1;
  for(int i = Total() - 1; i >= 0; i--)
  {
    Wave_label *wave_label = At(i), *temp_wave_label;
    for(int j = i - 1; j >= 0; j--)
    {
      temp_wave_label = At(j);
      if(wave_label.get_time() == temp_wave_label.get_time() && wave_label.get_pos() == temp_wave_label.get_pos())
      {
        if(wave_label.Time(0) != temp_wave_label.Time(0) && temp_wave_label.get_level() > min_level)
        {
          min_level = temp_wave_label.get_level();
        }
      }
    }
  }
  hide(++min_level);
}

void Wave_labels::hide(int level)
{
  for(int i = Total() - 1; i >= 0; i--)
  {
    Wave_label *wave_label = At(i);
    if(wave_label.get_level() < level)
    {
      wave_label.Timeframes(OBJ_NO_PERIODS);
      wave_label.update();
    }
    else
    {
      wave_label.Timeframes(get_current_tf());
      wave_label.update();
    }
  }
}

void Wave_labels::change_level(bool change)
{
  int selected = get_selected();
  int max_level = -1;
  int min_level = descrs.Total();
  bool b = false;
  for(int i = 0; i < Total(); i++)
  {
    Wave_label *wave_label = At(i);
    if(wave_label.Selected() || selected == -1)
    {
      int level = wave_label.get_level();
      if(level < min_level)
      {
        min_level = level;
      }
      if(level > max_level)
      {
        max_level = level;
      }
      b = true;
    }
  }
  if(b && ((descrs.Total() - 1 - max_level > 0 && change) || (min_level - 0 > 0 && ! change)))
  {
    for(int i = 0; i < Total(); i++)
    {
      Wave_label *wave_label = At(i);
      if(wave_label.Selected() || selected == -1)
      {
        if(change)
        {
          wave_label.up_level();
        }
        else if( ! change)
        {
          wave_label.down_level();
        }
      }
    }
  }
}

Wave_label *Wave_labels::get(string name)
{
  for(int i = 0; i < Total(); i++)
  {
    Wave_label *wave_label = At(i);
    if(wave_label.Name() == name)
    {
      return(wave_label);
    }
  }
  return(NULL);
}

int Wave_labels::get_selected()
{
  for(int i = 0; i < Total(); i++)
  {
    Wave_label *wave_label = At(i);
    if(wave_label.Selected())
    { 
      return(i);
    }
  }
  return(-1);
}

void Wave_labels::click(string name)
{
  select_group = false;
  for(int i = 0; i < Total(); i++)
  {
    Wave_label *wave_label = At(i);
    if(wave_label.Name() == name)
    {
      wave_label.Selected( ! wave_label.Selected());
      if(wave_label.Selected())
      {
        group = wave_label.get_group();
      }
    }
    else if( ! select && wave_label.Selected())
    {
      wave_label.Selected(false);
    }
  }
  if(get_selected() < 0)
  {
    group = (string) (max_group++ + 1);
  }
}
int Wave_labels::get_min_level()
{
  Wave_label *wave_label;
  int min = -1;
  if(Total() >= 1)
  {
    wave_label = At(0);
    min = wave_label.get_level();
  }
  for(int i = 1; i < Total(); i++)
  {
    wave_label = At(i);
    int level = wave_label.get_level();
    if(level < min)
    {
      min = level;
    }
  }
  return(min);
}

int Wave_labels::get_max_level()
{
  Wave_label *wave_label;
  int max = -1;
  if(Total() >= 1)
  {
    wave_label = At(0);
    max = wave_label.get_level();
  }
  for(int i = 1; i < Total(); i++)
  {
    wave_label = At(i);
    int level = wave_label.get_level();
    if(level > max)
    {
      max = level;
    }
  }
  return(max);
}

void Wave_labels::set_max_group()
{
  max_group = -1;
  for(int i = 0; i < Total(); i++)
  {
    Wave_label *wave_label = At(i);
    int group = (int) wave_label.get_group();
    if(group > max_group)
    {
      max_group = group;
    }
  }
}