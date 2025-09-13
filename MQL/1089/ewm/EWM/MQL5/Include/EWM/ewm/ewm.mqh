//+------------------------------------------------------------------+
//|                                                          ewm.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <EWM\defines.mqh>
#include "label.mqh"
#include "wave_label.mqh"
#include "panel.mqh"
#include "wave_labels.mqh"
#include <EWM\functions.mqh>
#include "vertical.mqh"

class Ewm
{
  public:
    bool analysis;
    Wave_labels wave_labels;
    Panel panel;
    Vertical vertical0;
    //методы
    void on_init();
    void on_deinit(int reason);
    void Ewm();                                                            // constructor
    void ~Ewm();                                                           // destructor
    void attach_labels();                                                    // attach chart labels to the objects
    // event handlers
    void on_label_drag(string name);                                         // handler of label dragging in the labels panel 
    void on_wave_drag(string name);                                    // handler of wave label dragging
    void on_label_click(string name);                                        // handler of label clicking in the labels panel
    void on_wave_click(string name);                                   // handler of wave label clicking 
    void on_chart_click(int x, int y);                                       // handler of clicking in the chart
    void on_remove_wave(string name);                                  // handler of wave label removing from the chart
    void on_remove_label(string name);                                       // handler of label removing on the label panel from the chart
    // keystroke handlers 
    void on_prev_level();                                                    // show the previous wave level on the labels panel
    void on_next_level();                                                    // show the next wave level in the labels panel
    void on_down_level();                                                    // decrease the wave level of the selected labels
    void on_up_level();                                                      // increase the wave level of the selected labels
    void on_start_marking();                                                 // start waves marking
    void on_stop_marking();                                                  // stop waves marking
    void on_select_group();                                                  // select a group of wave labels
    void on_hide_panel();                                                    // hide/show the labels panel
    void on_reduce_level();                                                  // reduce the number of wave levels displayed in the chart
    void on_increase_level();                                                // increase the number of wave levels displayed in the chart
    void clear();
    void on_show_price_label();
    void on_show_horizontal();
    void on_show_vertical();
    void on_hide_all_horizontal();
    void on_hide_all_price_label();
    void on_hide_all_vertical();
    void on_show_vertical0();
    void set_time_vertical0();
};

void Ewm::on_init()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    wave_label.update();
  }
  wave_labels.sort();
  wave_labels.adapt();
  wave_labels.correct();
}

void Ewm::on_deinit(int reason)
{

}

void Ewm::Ewm()
{
  attach_labels();
  wave_labels.sort();
}

void Ewm::~Ewm()
{
  wave_labels.FreeMode(true);
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    wave_label.Detach();
    wave_label.horizontal.Detach();
    wave_label.vertical.Detach();
    wave_label.price_label.Detach();
  }
  vertical0.Detach();
}

void Ewm::attach_labels()
{
  if(ObjectFind(0, NAME_VERTICAL0) >= 0)
  {
    vertical0.Attach(0, NAME_VERTICAL0, 0, 1);
  }
  for(int i = 0; i < ObjectsTotal(0, 0, OBJ_TEXT); i++)
  {
    string name = ObjectName(0, i, 0, OBJ_TEXT);
    if(StringFind(name, NAME_WAVE) >= 0)
    {
      Wave_label *wave_label = new Wave_label;
      wave_label.Attach(0, name, 0, 1);
      name = replace(name, UNIQUE_NAME, NAME_PRICE_LABEL);
      if(ObjectFind(0, name) >= 0)
      {
        wave_label.price_label.Attach(0, name, 0, 1);
      }
      name = replace(name, UNIQUE_NAME, NAME_HORIZONTAL);
      if(ObjectFind(0, name) >= 0)
      {
        wave_label.horizontal.Attach(0, name, 0, 2);
      }
      name = replace(name, UNIQUE_NAME, NAME_VERTICAL);
      if(ObjectFind(0, name) >= 0)
      {
        wave_label.vertical.Attach(0, name, 0, 1);
      }
      wave_label.update();
      wave_labels.Add(wave_label);
    }
  }
  wave_labels.set_max_group();
}

void Ewm::on_label_drag(string name)
{
  Label *label = panel.labels.get(name);
  name = replace(label.Name(), GROUP, wave_labels.group);
  if(ObjectFind(0, replace(name, UNIQUE_NAME, NAME_WAVE)) < 0)
  {
    // create the wave label in the chart
    Wave_label *wave_label = new Wave_label;
    wave_label.create(replace(name, UNIQUE_NAME, NAME_WAVE), label.X_Distance() + SHIFT_X, label.Y_Distance() + SHIFT_Y);
    wave_labels.Add(wave_label);
    if(wave_label.get_level() < wave_labels.min_level)
    {
      wave_labels.min_level = wave_label.get_level();
      wave_labels.hide(wave_labels.min_level);
    }
    // search the name of the wave label in a minute chart and move it
    wave_label.move();
    wave_labels.sort();
    wave_labels.correct();
  }
  // remove the labels panel
  panel.remove();
  // create the labels panel
  panel.create(); 
}

void Ewm::on_wave_drag(string name)
{
  Wave_label *wave_label = wave_labels.get(name);
  // search the name of the wave label in a minute chart and move it
  wave_label.move();
  wave_labels.sort();
  // connect label with the price chart
  wave_labels.correct();
}

void Ewm::on_label_click(string name)
{
  panel.labels.click(name);
  if(panel.labels.get_selected() >= 0 && wave_labels.get_selected() < 0)
  {
    wave_labels.group = (string) (wave_labels.max_group++ + 1);
  }
}

void Ewm::on_wave_click(string name)
{
  int selected_label = panel.labels.get_selected();
  // if the label is selected in the labels panel
  if(selected_label >= 0)
  {
    // deselect the label
    Label *label = panel.labels.At(selected_label);
    label.Selected(false);
  }
  wave_labels.click(name);
}

void Ewm::on_chart_click(int x, int y)
{
  int selected_label = panel.labels.get_selected();
  // if the label is selected in the labels panel
  if(selected_label >= 0)
  {
    if(panel.labels.click_label)
    {
      panel.labels.click_label = false;
    }
    else
    {
      Label *label = panel.labels.At(selected_label);
      string name = replace(label.Name(), GROUP, wave_labels.group);
      if(ObjectFind(0, replace(name, UNIQUE_NAME, NAME_WAVE)) < 0)
      {
        Wave_label *wave_label = new Wave_label;
        wave_label.create(replace(name, UNIQUE_NAME, NAME_WAVE), x, y);
        wave_labels.Add(wave_label);
        if(wave_label.get_level() < wave_labels.min_level)
        {
          wave_labels.min_level = wave_label.get_level();
          wave_labels.hide(wave_labels.min_level);
        }
        // search the name of the wave label in a minute chart and move it
        wave_label.move();
        wave_labels.sort();
        wave_labels.correct();
      }
      label.Selected(false);
      if(selected_label + 1 < panel.labels.Total() - 1)
      {
        label = panel.labels.At(selected_label + 1);
        panel.current_label = selected_label + 1;
        label.Selected(true);
      }
    }
  }
}

void Ewm::on_remove_wave(string name)
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Name() == name)
    {
      wave_labels.Delete(i);
      break;
    }
  }
}

void Ewm::on_prev_level()
{
  if(panel.current_level - 1 >= 0)
  {
    panel.current_level--;
    panel.remove();
    panel.create();
  }
}

void Ewm::on_next_level()
{
  if(panel.current_level + 1 < descrs.Total())
  {
    panel.current_level++;
    panel.remove();
    panel.create();
  }
}

void Ewm::on_down_level()
{
  wave_labels.change_level(DOWN);
  wave_labels.correct();
}

void Ewm::on_up_level()
{
  wave_labels.change_level(UP);
  wave_labels.correct();
}

void Ewm::on_start_marking()
{
  int selected_label = panel.labels.get_selected();
  if(selected_label == -1)
  {
    Label *label = panel.labels.At(panel.current_label);
    label.Selected(true);
    if(wave_labels.get_selected() < 0)
    {
      wave_labels.group = (string) (wave_labels.max_group++ + 1);
    }
  }
  else
  {
    Label *label = panel.labels.At(selected_label);
    label.Selected(false);
    if(selected_label + 1 >= panel.labels.Total() - 1)
    {
      label = panel.labels.At(0);
      panel.current_label = 0;
      label.Selected(true);
    }
    else
    {
      label = panel.labels.At(selected_label + 1);
      panel.current_label = selected_label + 1;
      label.Selected(true);
    }
  }
}

void Ewm::on_stop_marking()
{
  int index = panel.labels.get_selected();
  if(index >= 0)
  {
    Label *label = panel.labels.At(index);
    label.Selected(false);
  }
  wave_labels.group = (string) (wave_labels.max_group++ + 1);
}

void Ewm::on_select_group()
{
  int selected_wave = wave_labels.get_selected();
  if(selected_wave >= 0)
  {
    Wave_label *wave_label = wave_labels.At(selected_wave);
    string group = wave_label.get_group();
    wave_labels.select_group = ! wave_labels.select_group;
    for(int i = 0; i < wave_labels.Total(); i++)
    {
      wave_label = wave_labels.At(i);
      if(wave_labels.select_group && group == wave_label.get_group())
      {
        wave_label.Selected(true);
      }
      else
      {
        wave_label.Selected(false);
      }
    }
  }
}

void Ewm::on_hide_panel()
{
  panel.hide();
}

void Ewm::on_reduce_level()
{
  int min = wave_labels.get_min_level();
  if(wave_labels.min_level - 1 >= min)
  {
    wave_labels.min_level--;
    wave_labels.hide(wave_labels.min_level);
    wave_labels.correct();
  }
}

void Ewm::on_increase_level()
{
  int max = wave_labels.get_max_level();
  if(wave_labels.min_level + 1 <= max)
  {
    wave_labels.min_level++;
    wave_labels.hide(wave_labels.min_level);
    wave_labels.correct();
  }
}

void Ewm::clear()
{
  if(wave_labels.Total() > 0)
  {
    if(MessageBox(MSG_CLEAR, NULL, MB_YESNO | MB_ICONQUESTION) == IDNO)
    {
      return;
    }
  }
  wave_labels.FreeMode(true);
  wave_labels.Clear();
}

void Ewm::on_show_horizontal()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      if(wave_label.horizontal.Name() == NULL)
      {
        wave_label.horizontal.create(wave_label.Name(), wave_label.Time(0), wave_label.get_price(), wave_label.get_pos(), wave_label.Timeframes());
      }
      else
      {
        if(wave_label.horizontal.Timeframes() == OBJ_NO_PERIODS)
        {
          wave_label.horizontal.Timeframes(wave_label.Timeframes());
        }
        else
        {
          wave_label.horizontal.Delete();
        }
      }
    }
    
  }
}

void Ewm::on_hide_all_horizontal()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.horizontal.Name() != NULL)
    {
      if(wave_label.horizontal.Timeframes() != OBJ_NO_PERIODS)
      {
        wave_label.horizontal.Timeframes(OBJ_NO_PERIODS);
      }
      else
      {
        wave_label.horizontal.Timeframes(wave_label.Timeframes());
      }
    }
  }
}

void Ewm::on_show_price_label()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      if(wave_label.price_label.Name() == NULL)
      {
        wave_label.price_label.create(wave_label.Name(), wave_label.Time(0), wave_label.get_price(), wave_label.get_pos(), wave_label.Timeframes());
      }
      else
      {
        if(wave_label.price_label.Timeframes() == OBJ_NO_PERIODS)
        {
          wave_label.price_label.Timeframes(wave_label.Timeframes());
        }
        else
        {
          wave_label.price_label.Delete();
        }
      }
    }
  }
}

void Ewm::on_hide_all_price_label()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.price_label.Name() != NULL)
    {
      if(wave_label.price_label.Timeframes() != OBJ_NO_PERIODS)
      {
        wave_label.price_label.Timeframes(OBJ_NO_PERIODS);
      }
      else
      {
        wave_label.price_label.Timeframes(wave_label.Timeframes());
      }
    }
  }
}

void Ewm::on_show_vertical()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.Selected())
    {
      if(wave_label.vertical.Name() == NULL)
      {
        wave_label.vertical.create(wave_label.Name(), wave_label.Time(0), wave_label.get_pos(), wave_label.Timeframes());
      }
      else
      {
        if(wave_label.vertical.Timeframes() == OBJ_NO_PERIODS)
        {
          wave_label.vertical.Timeframes(wave_label.Timeframes());
        }
        else
        {
          wave_label.vertical.Delete();
        }
      }
    }
  }
}

void Ewm::on_hide_all_vertical()
{
  for(int i = 0; i < wave_labels.Total(); i++)
  {
    Wave_label *wave_label = wave_labels.At(i);
    if(wave_label.vertical.Name() != NULL)
    {
      if(wave_label.vertical.Timeframes() != OBJ_NO_PERIODS)
      {
        wave_label.vertical.Timeframes(OBJ_NO_PERIODS);
      }
      else
      {
        wave_label.vertical.Timeframes(wave_label.Timeframes());
      }
    }
  }
}

void Ewm::on_show_vertical0()
{
  if(vertical0.Name() == NULL)
  {
    vertical0.Create(0, NAME_VERTICAL0, 0, 0);
    vertical0.Color(verical0_color);
    set_time_vertical0();
  }
  else
  {
    if(vertical0.Timeframes() == OBJ_NO_PERIODS)
    {
      vertical0.Timeframes(OBJ_ALL_PERIODS);
    }
    else
    {
      vertical0.Delete();
    }
  }
}

void Ewm::set_time_vertical0()
{
  datetime times[];
  while(CopyTime(_Symbol, PERIOD_M1, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  vertical0.Time(0, times[0]);
}