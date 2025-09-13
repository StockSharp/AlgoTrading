//+------------------------------------------------------------------+
//|                                                        panel.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Arrays\ArrayObj.mqh>
#include <EWM\defines.mqh>
#include "label.mqh"
#include "labels.mqh"

class Panel
{
  public:
    int current_level;
    int current_label;
    Labels labels;
    void create();
    void remove();
    void Panel();
    void ~Panel();
    void hide();
};

void Panel::Panel()
{
  current_level = 0;
  current_label = 0;
  labels.FreeMode(true);
  create();
}

void Panel::create()
{
  Label *label;
  string group = "0";
  Descr *descr = descrs.At(current_level);
  for(int i = 0; i < LABELS; i++)
  {
    label = new Label;
    label.create((string) current_level + SEPARATOR + descr.labels[i] + SEPARATOR + group + SEPARATOR + NAME_LABEL, x_distance + interval * i, y_distance, true, ANCHOR_CENTER, OBJ_ALL_PERIODS);
    labels.Add(label);
  }
  label = new Label;
  label.create((string) current_level + SEPARATOR + (string) descr.num_level + "-" + descr.level + SEPARATOR + group + SEPARATOR + NAME_LABEL, x_distance + interval * LABELS, y_distance, false, ANCHOR_LEFT, OBJ_ALL_PERIODS);
  labels.Add(label);
}

void Panel::remove()
{
  labels.Clear();
}

void Panel::~Panel()
{
  labels.Clear();
}

void Panel::hide()
{
  for(int i = 0; i < labels.Total(); i++)
  {
    Label *label = labels.At(i);
    if(label.Timeframes() == OBJ_ALL_PERIODS)
    {
      label.Timeframes(OBJ_NO_PERIODS);
    }
    else if(label.Timeframes() == OBJ_NO_PERIODS)
    {
      label.Timeframes(OBJ_ALL_PERIODS);
    }
  }
}