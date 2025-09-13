//+------------------------------------------------------------------+
//|                                                            p.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include "pitchforks.mqh"
#include "pitchfork.mqh"
#include <EWM\ewm\wave_labels.mqh>
#include <EWM\ewm\wave_label.mqh>
#include <EWM\defines.mqh>

class P
{
  public:
    Pitchforks pitchforks;
    Wave_labels *wave_labels;
    void P(Wave_labels *wave_labels);
    void ~P();
    void on_init();
    void on_deinit(int reason);
    void on_remove_pitchfork(string name);
    void attach_pitchforks();
    void insert_pitchfork(int x, int y);
    void on_show_schiff();
    void on_show_warning_up();
    void on_show_warning_down();
    void copy_chart(long id);
    void on_hide_pitchfork();
};

void P::on_init()
{
  pitchforks.update();
}

void P::on_deinit(int reason)
{

}

void P::P(Wave_labels *wave_labels)
{
  this.wave_labels = wave_labels;
  attach_pitchforks();
}

void P::~P()
{
  for(int i = 0; i < pitchforks.Total(); i++)
  {
    Pitchfork *pitchfork = pitchforks.At(i);
    pitchfork.schiff.Detach();
    pitchfork.reaction.Detach();
    pitchfork.warning_up.Detach();
    pitchfork.warning_down.Detach();
    pitchfork.Detach();
  }
}

void P::on_remove_pitchfork(string name)
{
  for(int i = 0; i < pitchforks.Total(); i++)
  {
    Pitchfork *pitchfork = pitchforks.At(i);
    for(int j = 0; j < 3; j++)
    {
      if(pitchfork.wave_label[j].Name() == name)
      {
        pitchforks.Delete(i);
        i--;
        break;
      }
    }
  }
}

void P::attach_pitchforks()
{
  for(int i = 0; i < ObjectsTotal(0, 0, OBJ_PITCHFORK); i++)
  {
    string name = ObjectName(0, i, 0, OBJ_PITCHFORK);
    if(StringFind(name, NAME_PITCHFORK) >= 0)
    {
      Pitchfork *pitchfork = new Pitchfork;
      pitchfork.attach(name);
      name = get(pitchfork.Name(), PITCHFORK_LEVEL1) + SEPARATOR + get(pitchfork.Name(), PITCHFORK_TEXT1)  + SEPARATOR + get(pitchfork.Name(), PITCHFORK_GROUP1) + SEPARATOR + NAME_WAVE;
      Wave_label *wave_label = wave_labels.get(name);
      pitchfork.wave_label[0] = wave_label;
      name = get(pitchfork.Name(), PITCHFORK_LEVEL2) + SEPARATOR + get(pitchfork.Name(), PITCHFORK_TEXT2)  + SEPARATOR + get(pitchfork.Name(), PITCHFORK_GROUP2) + SEPARATOR + NAME_WAVE;
      wave_label = wave_labels.get(name);
      pitchfork.wave_label[1] = wave_label;
      name = get(pitchfork.Name(), PITCHFORK_LEVEL3) + SEPARATOR + get(pitchfork.Name(), PITCHFORK_TEXT3)  + SEPARATOR + get(pitchfork.Name(), PITCHFORK_GROUP3) + SEPARATOR + NAME_WAVE;
      wave_label = wave_labels.get(name);
      pitchfork.wave_label[2] = wave_label;
      if(CheckPointer(pitchfork.wave_label[0]) == POINTER_INVALID || CheckPointer(pitchfork.wave_label[1]) == POINTER_INVALID || CheckPointer(pitchfork.wave_label[2]) == POINTER_INVALID)
      {
        delete pitchfork;
        continue;
      }
      pitchforks.Add(pitchfork);
      name = replace(pitchfork.Name(), PITCHFORK_NAME, NAME_SCHIFF);
      if(ObjectFind(0, name) >= 0)
      {
        pitchfork.schiff.attach(name);
      }
      else
      {
        pitchfork.schiff.create(pitchfork.wave_label);
      }
      name = replace(pitchfork.Name(), PITCHFORK_NAME, NAME_REACTION);
      if(ObjectFind(0, name) >= 0)
      {
        pitchfork.reaction.attach(name);
      }
      else
      {
        pitchfork.reaction.create(pitchfork.wave_label);
      }
      name = replace(pitchfork.Name(), PITCHFORK_NAME, NAME_WARNING_UP);
      pitchfork.warning_up.name = NAME_WARNING_UP;
      if(ObjectFind(0, name) >= 0)
      {
        pitchfork.warning_up.attach(name);
      }
      name = replace(pitchfork.Name(), PITCHFORK_NAME, NAME_WARNING_DOWN);
      pitchfork.warning_down.name = NAME_WARNING_DOWN;
      if(ObjectFind(0, name) >= 0)
      {
        pitchfork.warning_down.attach(name);
      }
      pitchfork.update();
    }
  }
}

void P::insert_pitchfork(int x, int y)
{
  double price;
  datetime time;
  get_coordinates(x, y, price, time);
  Pitchfork *pitchfork = new Pitchfork;
  int j = 2;
  for(int i = wave_labels.Total() - 1; i >= 0 && j >= 0; i--)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected() && wave_label.get_time() <= time)
    {
      pitchfork.wave_label[j--] = wave_label;
    }
  }
  if(j == -1)
  {
    for(int i = 0; i < pitchforks.Total(); i++)
    {
      Pitchfork *temp_pitchfork = pitchforks.At(i);
      int k = 0;
      for(j = 0; j < 3; j++)
      {
        if(temp_pitchfork.wave_label[j] == pitchfork.wave_label[j])
        {
          k++;
        }
        if(k == 3)
        {
          delete pitchfork;
          if(temp_pitchfork.Timeframes() != OBJ_NO_PERIODS)
          {
            pitchforks.Delete(i);
          }
          else
          {
            temp_pitchfork.Timeframes(get_current_tf());
            temp_pitchfork.schiff.Timeframes(get_current_tf());
            temp_pitchfork.reaction.Timeframes(get_current_tf());
            temp_pitchfork.warning_up.Timeframes(get_current_tf());
            temp_pitchfork.warning_down.Timeframes(get_current_tf());
          }
          return;
        }
      }
    }
    pitchfork.create();
    pitchforks.Add(pitchfork);
  }
  else
  {
    delete pitchfork;
  }
}

void P::on_show_schiff()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      for(int j = 0; j < pitchforks.Total(); j++)
      {
        Pitchfork *pitchfork = pitchforks.At(j);
        if(wave_label == pitchfork.wave_label[2])
        {
          pitchfork.schiff.schiff = ! pitchfork.schiff.schiff;
          pitchfork.schiff.update(pitchfork.wave_label, pitchfork.Timeframes());
          break;
        }
      }
    }
  }
}

void P::on_show_warning_up()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      for(int j = 0; j < pitchforks.Total(); j++)
      {
        Pitchfork *pitchfork = pitchforks.At(j);
        if(wave_label == pitchfork.wave_label[2])
        {
          if(pitchfork.warning_up.Name() == NULL)
          {
            pitchfork.warning_up.create(pitchfork.wave_label);
          }
          else
          {
            if(pitchfork.warning_up.Timeframes() == OBJ_NO_PERIODS)
            {
              pitchfork.warning_up.Timeframes(pitchfork.Timeframes());
            }
            else
            {
              pitchfork.warning_up.Delete();
            }
          }
          pitchfork.update();
          break;
        }
      }
    }
  }
}

void P::on_show_warning_down()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      for(int j = 0; j < pitchforks.Total(); j++)
      {
        Pitchfork *pitchfork = pitchforks.At(j);
        if(wave_label == pitchfork.wave_label[2])
        {
          if(pitchfork.warning_down.Name() == NULL)
          {
            pitchfork.warning_down.create(pitchfork.wave_label);
          }
          else
          {
            if(pitchfork.warning_down.Timeframes() == OBJ_NO_PERIODS)
            {
              pitchfork.warning_down.Timeframes(pitchfork.Timeframes());
            }
            else
            {
              pitchfork.warning_down.Delete();
            }
          }
          pitchfork.update();
          break;
        }
      }
    }
  }
}

void P::on_hide_pitchfork()
{
  for(int i = 0; i < pitchforks.Total(); i++)
  {
    Pitchfork *pitchfork = pitchforks.At(i);
    if(pitchfork.wave_label[2].Timeframes() != OBJ_NO_PERIODS)
    {
      if(pitchfork.Timeframes() == OBJ_NO_PERIODS)
      {
        pitchfork.Timeframes(pitchfork.wave_label[2].Timeframes());
        pitchfork.set_visible();
      }
      else
      {
        pitchfork.Timeframes(OBJ_NO_PERIODS);
        pitchfork.set_hidden();
      }
    }
  }
  pitchforks.update();
}