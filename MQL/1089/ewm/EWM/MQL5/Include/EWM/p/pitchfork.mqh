//+------------------------------------------------------------------+
//|                                                  c_pitchfork.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include "schiff.mqh"
#include "reaction.mqh"
#include <ChartObjects\ChartObjectsChannels.mqh>
#include <EWM\ewm\wave_label.mqh>
#include "warning.mqh"
#include <EWM\defines.mqh>

class Pitchfork : public CChartObjectPitchfork
{
  public:
    Wave_label *wave_label[3];
    Schiff schiff;
    Reaction reaction;
    Warning warning_up;
    Warning warning_down;
    void attach(string name);
    void create();
    void update();
    void set_coordinates();
    void set_levels(double &levels[]);
    string get_name(string name);
    string get(int param);
    bool Time(int point, datetime time)
    {
      return(ObjectSetInteger(0, Name(), OBJPROP_TIME, point, time));
    }
    bool Price(int point, double price)
    {
      return(ObjectSetDouble(0, Name(), OBJPROP_PRICE, point, price));
    }
    void set_visible();
    void set_hidden();
};

void Pitchfork::attach(string name)
{
  Attach(0, name, 0, 3);
}

void Pitchfork::create()
{
  Create(0, get_name(wave_label[0].Name()) + SEPARATOR + get_name(wave_label[1].Name()) + SEPARATOR + get_name(wave_label[2].Name()) + SEPARATOR + NAME_PITCHFORK, 0, 0, 0, 0, 0, 0, 0);
  schiff.create(wave_label);
  reaction.create(wave_label);
  warning_up.name = NAME_WARNING_UP;
  warning_down.name = NAME_WARNING_DOWN;
  update();
}

void Pitchfork::set_coordinates()
{
  Time(0, wave_label[0].get_time());
  Price(0, wave_label[0].get_price());
  Time(1, wave_label[1].get_time());
  Price(1, wave_label[1].get_price());
  Time(2, wave_label[2].get_time());
  Price(2, wave_label[2].get_price());
}

void Pitchfork::update()
{
  Name(get_name(wave_label[0].Name()) + SEPARATOR + get_name(wave_label[1].Name()) + SEPARATOR + get_name(wave_label[2].Name()) + SEPARATOR + NAME_PITCHFORK + ((get(PITCHFORK_VISIBLE) == "H") ? SEPARATOR + "H" : ""));
  string name = Name();
  set_coordinates();
  Selectable(false);
  RayLeft(false);
  RayRight(true);
  set_levels(pitchfork_levels);
  if(get(PITCHFORK_VISIBLE) == "")
  {
    Timeframes(wave_label[2].Timeframes());
  }
  Z_Order(0);
  Descr *descr = descrs.At((int) get(PITCHFORK_LEVEL3));
  Color(descr.clr);
  schiff.update(wave_label, Timeframes());
  reaction.update(wave_label, Timeframes());
  if(warning_up.Name() != NULL)
  {
    warning_up.update(wave_label, Timeframes());
  }
  if(warning_down.Name() != NULL)
  {
    warning_down.update(wave_label, Timeframes());
  }
}

void Pitchfork::set_levels(double &levels[])
{
  LevelsCount(ArrayRange(levels, 0));
  for(int i = 0; i < ArrayRange(levels, 0); i++)
  {
    LevelValue(i, levels[i]);
    LevelDescription(i, i > 0 ? DoubleToString(levels[i] * 100, 1) : "");
    Descr *descr = descrs.At((int) get(PITCHFORK_LEVEL3));
    LevelColor(i, descr.clr);
    LevelStyle(i, STYLE_DOT);
    LevelWidth(i, 1);
  }
}
string Pitchfork::get_name(string name)
{
  string results[];
  StringSplit(name, '_', results);
  name = results[0];
  for(int i = 1; i < 3; i++)
  {
    name += SEPARATOR + results[i];
  }
  return(name);
}
string Pitchfork::get(int param)
{
  string results[];
  StringSplit(Name(), '_', results);
  if(param > ArrayRange(results, 0) - 1)
  {
    return("");
  }
  return(results[param]);
}

void Pitchfork::set_visible()
{
  string results[];
  StringSplit(Name(), '_', results);
  string name = results[0];
  for(int i = 1; i < 10; i++)
  {
    name += SEPARATOR + results[i];
  }
  Name(name);
}
void Pitchfork::set_hidden()
{
  Name(Name() + SEPARATOR + "H");
}