//+------------------------------------------------------------------+
//|                                                  price_label.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsArrows.mqh>
#include <EWM\functions.mqh>
#include <EWM\defines.mqh>

class Price_label : public CChartObjectArrowLeftPrice
{
  public:
    void create(string wave_label_name, datetime time, double price, bool pos, int tf);
    void update(string wave_label_name, datetime time, double price, bool pos, int tf);
};

void Price_label::create(string wave_label_name, datetime time, double price, bool pos, int tf)
{
  Create(0, replace(wave_label_name, UNIQUE_NAME, NAME_PRICE_LABEL), 0, time, price);
  Timeframes(tf);
  Color(pos == false ? clrRed : clrBlue);
}

void Price_label::update(string wave_label_name, datetime time, double price, bool pos, int tf)
{
  Name(replace(wave_label_name, UNIQUE_NAME, NAME_PRICE_LABEL));
  Time(0, time);
  Price(0, price);
  Color(pos == false ? clrRed : clrBlue);
  Style(STYLE_SOLID);
  Timeframes(tf);
}
