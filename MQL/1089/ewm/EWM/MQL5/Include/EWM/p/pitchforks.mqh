//+------------------------------------------------------------------+
//|                                               c_l_pitchforks.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Arrays\ArrayObj.mqh>
#include <EWM\defines.mqh>
#include <EWM\ewm\wave_label.mqh>
#include "pitchfork.mqh"
#include <EWM\functions.mqh>

class Pitchforks : public CArrayObj
{
  public:
    void update();
    void remove(Wave_label *wave);
    Pitchfork *get(Wave_label *wave);
};

void Pitchforks::update()
{
  datetime times[];
  while(CopyTime(_Symbol, PERIOD_M1, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  for(int i = 0; i < Total(); i++)
  {
    Pitchfork *pitchfork = At(i);
    bool exchange = true;
    while(exchange)
    {
      exchange = false;
      for(int j = 0; j < 2; j++)
      {
        if(pitchfork.wave_label[j].Time(0) > pitchfork.wave_label[j + 1].Time(0))
        {
          Wave_label *wave = pitchfork.wave_label[j + 1];
          pitchfork.wave_label[j + 1] = pitchfork.wave_label[j];
          pitchfork.wave_label[j] = wave;
          exchange = true;
        }
      }
    }
    if(pitchfork.wave_label[2].Time(0) > times[0])
    {
      Delete(i);
      i--;
    }
    else
    {
      pitchfork.update();
    }
  }
}

void Pitchforks::remove(Wave_label *wave)
{
  for(int i = 0; i < Total(); i++)
  {
    Pitchfork *pitchfork = At(i);
    for(int j = 0; j < 3; j++)
    {
      if(pitchfork.wave_label[j] == wave)
      {
        Delete(i);
        i--;
        break;
      }
    }
  }
}

Pitchfork* Pitchforks::get(Wave_label *wave)
{
  for(int i = 0; i < Total(); i++)
  {
    Pitchfork *pitchfork = At(i);
    if(pitchfork.wave_label[2] == wave)
    {
      return(pitchfork);
    }
  }
  return(NULL);
}
