//+------------------------------------------------------------------+
//|                                                   horizontal.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsLines.mqh>
#include <EWM\functions.mqh>
#include <EWM\defines.mqh>

class Horizontal : public CChartObjectTrend
{
  public:
    void create(string wave_label_name, datetime time, double price, bool pos, int tf);
    void update(string wave_label_name, datetime time, double price, bool pos, int tf);
    datetime get_time(datetime time1, double price1, bool pos);
    bool Time(int point, datetime time)
    {
      return(ObjectSetInteger(0, Name(), OBJPROP_TIME, point, time));
    }
    bool Price(int point, double price)
    {
      return(ObjectSetDouble(0, Name(), OBJPROP_PRICE, point, price));
    }
};

datetime Horizontal::get_time(datetime time1, double price1, bool pos)
{
  datetime times[];
  while(CopyTime(_Symbol, _Period, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  datetime time2 = times[0];
  MqlRates temp_rates[];
  int bars = Bars(_Symbol, _Period, time1, times[0]);
  while(CopyRates(_Symbol, _Period, time1, times[0], temp_rates) != bars)
  {
    Sleep(DELAY);
  }
  for(int i = 0; i < bars; i++)
  {
    if((pos && temp_rates[i].high >= price1) || (! pos && temp_rates[i].low <= price1))
    {
      time2 = temp_rates[i].time;
      break;
    }
  }
  return(time2);
}

void Horizontal::create(string wave_label_name, datetime time, double price, bool pos, int tf)
{
  Create(0, replace(wave_label_name, UNIQUE_NAME, NAME_HORIZONTAL), 0, time, price, get_time(time, price, pos), price);
  Color(pos == false ? clrRed : clrBlue);
  Style(STYLE_DOT);
  Timeframes(tf);
}

void Horizontal::update(string wave_label_name, datetime time, double price, bool pos, int tf)
{
  Name(replace(wave_label_name, UNIQUE_NAME, NAME_HORIZONTAL));
  Time(0, time);
  Price(0, price);
  Time(1, get_time(time, price, pos));
  Price(1, price);
  Color(pos == false ? clrRed : clrBlue);
  Style(STYLE_DOT);
  Timeframes(tf);
}
