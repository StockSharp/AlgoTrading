//+------------------------------------------------------------------+
//|                                                       labels.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Arrays\ArrayObj.mqh>
#include "label.mqh"

class Labels : public CArrayObj
{
  public:
    int current_label;
    bool click_label;
    void Labels()
    {
      current_label = 0;
      click_label = false;
    }
    Label *get(string name);
    int get_selected();
    void click(string name);
};

Label *Labels::get(string name)
{
  for(int i = 0; i < Total(); i++)
  {
    Label *label = At(i);
    if(label.Name() == name)
    {
      return(label);
    }
  }
  return(NULL);
}

int Labels::get_selected()
{
  for(int i = 0; i < Total(); i++)
  {
    Label *label = At(i);
    if(label.Selected())
    { 
      return(i);
    }
  }
  return(-1);
}

void Labels::click(string name)
{
  for(int i = 0; i < Total(); i++)
  {
    Label *label = At(i);
    // find the label in the list of labels on the labels panel
    if(label.Name() == name)
    {
      label.Selected( ! label.Selected());
      // if the label is selected
      if(label.Selected())
      {
        current_label = i;
        click_label = true;
      }
    }
    else if(label.Selected())
    {
      label.Selected(false);
    }
  }
}