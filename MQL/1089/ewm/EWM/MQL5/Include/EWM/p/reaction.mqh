//+------------------------------------------------------------------+
//|                                             c_reaction_lines.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsFibo.mqh>
#include <EWM\defines.mqh>
#include <EWM\ewm\wave_label.mqh>
#include <EWM\functions.mqh>

class Reaction : public CChartObjectFiboChannel
{
  public:
    bool inverse;
    double h;
    double dx;
    void attach(string name);
    bool Time(int point, datetime time)
    {
      return(ObjectSetInteger(0, Name(), OBJPROP_TIME, point, time));
    }
    bool Price(int point, double price)
    {
      return(ObjectSetDouble(0, Name(), OBJPROP_PRICE, point, price));
    }
    void create(Wave_label *&waves[]);
    void update(Wave_label *&waves[], int tf);
    void set_coordinates(Wave_label *&waves[]);
    void set_levels();
    string get(int param);
    int get_pos(int point);
    string get_name(string name);
    void copy(long id);
    double get_tn_line1(Coord &coord);
    double get_tn_line4(Coord &coord);
    double get_coord_on_line1(int pos, Coord &coord);
    double get_coord_on_line2(int pos, Coord &coord);
    double get_coord_on_line3(int pos, Coord &coord);
    double get_coord_on_line4(int pos, Coord &coord);
    void get_coord4(Coord &coord);
    void get_h(Coord &coord);
};

void Reaction::attach(string name)
{
  Attach(0, name, 0, 3);
}

string Reaction::get(int param)
{
  string results[];
  StringSplit(Name(), '_', results);
  return(results[param]);
}

void Reaction::create(Wave_label *&waves[])
{
  Create(0, get_name(waves[0].Name()) + SEPARATOR + get_name(waves[1].Name()) + SEPARATOR + get_name(waves[2].Name()) + SEPARATOR + NAME_REACTION, 0, 0, 0, 0, 0, 0, 0);
  update(waves, waves[2].Timeframes());
}

double Reaction::get_tn_line1(Coord &coord)
{
  return((coord.price4 - coord.price1) / (coord.pos4 - coord.pos1));
}

double Reaction::get_tn_line4(Coord &coord)
{
  return((coord.price3 - coord.price2) / (coord.pos3 - coord.pos2));
}

double Reaction::get_coord_on_line1(int pos, Coord &coord)
{
  double tn = get_tn_line1(coord);
  double price = coord.price1 + tn * (pos - coord.pos1);
  return(price);
}

double Reaction::get_coord_on_line2(int pos, Coord &coord)
{
  double tn = get_tn_line1(coord);
  double price = coord.price2 + tn * (pos - coord.pos2);
  return(price);
}

double Reaction::get_coord_on_line3(int pos, Coord &coord)
{
  double tn = get_tn_line1(coord);
  double price = coord.price3 + tn * (pos - coord.pos3);
  return(price);
}

double Reaction::get_coord_on_line4(int pos, Coord &coord)
{
  double tn = get_tn_line4(coord);
  double price = coord.price3 + tn * (coord.pos3 - pos);
  return(price);
}

void Reaction::get_coord4(Coord &coord)
{
  coord.pos4 = (coord.pos2 + coord.pos3) / 2.0;
  coord.price4 = (coord.price2 + coord.price3) / 2.0;
}

void Reaction::get_h(Coord &coord)
{
  if(coord.pos3 == coord.pos2 || coord.pos1 == coord.pos4)
  {
    h = 0;
  }
  else
  {
    MqlRates temp_rates[];
    int bars = Bars(_Symbol, _Period);
    while(CopyRates(_Symbol, _Period, 0, bars, temp_rates) != bars)
    {
      Sleep(DELAY);
    }
    double tn_line4 = get_tn_line4(coord);
    double tn_line1 = -get_tn_line1(coord);
    double dt;
    h = 0;
    if(coord.price2 < coord.price3)
    {
      for(int i = (int) coord.pos2; i < (int) coord.pos3; i++)
      {
        double delta = (coord.price2 + tn_line4 * (i - coord.pos2)) - temp_rates[i].low;
        if(delta > h)
        {
          h = delta;
          dt = delta / (tn_line4 + tn_line1);
        }
      }
    }
    else
    {
      for(int i = (int) coord.pos2; i < (int) coord.pos3; i++)
      {
        double delta = temp_rates[i].high - (coord.price2 + tn_line4 * (i - coord.pos2));
        if(delta > h)
        {
          h = delta;
          dt = delta / (tn_line4 + tn_line1);
        }
      }
    }
    h = MathAbs(dt) / (coord.pos4 - coord.pos1);
  }
}

void Reaction::set_coordinates(Wave_label *&waves[])
{
  Time(0, waves[0].get_time());
  Price(0, waves[0].get_price());
  Time(1, waves[1].get_time());
  Price(1, waves[1].get_price());
  Time(2, waves[2].get_time());
  Price(2, waves[2].get_price());
  Coord coord;
  coord.pos1 = get_pos(0);
  coord.pos2 = get_pos(1);
  coord.pos3 = get_pos(2);
  coord.price1 = Price(0);
  coord.price2 = Price(1);
  coord.price3 = Price(2);
  get_coord4(coord);
  get_h(coord);
  double pos1, pos2, pos3;
  double price1, price2, price3;
  pos1 = coord.pos2 + (coord.pos1 - coord.pos4);
  pos2 = coord.pos3 + (coord.pos1 - coord.pos4);
  if(pos1 < 0 || pos2 < 0)
  {
    pos1 = coord.pos3;
    pos2 = coord.pos2;
    pos3 = coord.pos3 + (coord.pos1 - coord.pos4);
    price1 = coord.price3;
    price2 = coord.price2;
    price3 = coord.price3 - (coord.price4 - coord.price1);
    if(MathCeil(pos3) != MathFloor(pos3))
    {
      pos3 = MathFloor(pos3);
      price3 = get_coord_on_line3((int) pos3, coord);
      dx = -0.5 / (pos3 - pos1);
    }
    inverse = true;
  }
  else
  {
    price1 = coord.price2 - (coord.price4 - coord.price1);
    price2 = coord.price3 - (coord.price4 - coord.price1);
    price3 = coord.price2;
    pos3 = coord.pos2;
    dx = 0;
    if(MathFloor(pos1) != MathCeil(pos1))
    {
      pos1 = MathFloor(pos1);
      pos2 = MathFloor(pos2);
      price1 = get_coord_on_line2((int) pos1, coord);
      price2 = get_coord_on_line3((int) pos2, coord);
      dx = -0.5 / (pos3 - pos1);
    }
    inverse = false;
  }
  Time(0, bar_to_time((int) pos1));
  Time(1, bar_to_time((int) pos2));
  Time(2, bar_to_time((int) pos3));
  Price(0, price1);
  Price(1, price2);
  Price(2, price3);
}

void Reaction::update(Wave_label *&waves[], int tf)
{
  Name(get_name(waves[0].Name()) + SEPARATOR + get_name(waves[1].Name()) + SEPARATOR + get_name(waves[2].Name()) + SEPARATOR + NAME_REACTION);
  set_coordinates(waves);
  RayRight(false);
  RayLeft(false);
  Timeframes(tf);
  Z_Order(0);
  Color(clrNONE);
  set_levels(); 
}

void Reaction::set_levels()
{
  LevelsCount(reaction_levels.Total() + 1);
  int i = 0;
  while(i < reaction_levels.Total())
  {
    if(inverse)
    {
      LevelValue(i, -1 - (reaction_levels.At(i) + dx * reaction_levels.At(i)));
    }
    else
    {
      LevelValue(i, reaction_levels.At(i) + dx * reaction_levels.At(i));
    }
    LevelDescription(i, DoubleToString(reaction_levels.At(i) * 100, 1));
    Descr *descr = descrs.At((int) get(PITCHFORK_LEVEL3));
    LevelColor(i, descr.clr);
    LevelStyle(i, STYLE_DOT);
    LevelWidth(i, 1);
    i++;
  }
  if(inverse)
  {
    LevelValue(i, -1 - (h + dx * h));
  }
  else
  {
    LevelValue(i, h + dx * h);
  }
  LevelDescription(i, "");
  LevelColor(i, red_zone_color);
  LevelStyle(i, STYLE_SOLID);
  LevelWidth(i, 2);
}

int Reaction::get_pos(int point)
{
  datetime times[];
  while(CopyTime(_Symbol, _Period, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  int bars1 = Bars(_Symbol, _Period);
  int bars2 = Bars(_Symbol, _Period, Time(point), times[0]);
  return(bars1 - bars2);
}

string Reaction::get_name(string name)
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
