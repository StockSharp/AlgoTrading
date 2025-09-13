//+------------------------------------------------------------------+
//|                                                      warning.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsFibo.mqh>
#include <EWM\ewm\wave_label.mqh>

class Warning : public CChartObjectFiboChannel
{
  public:
    string name;
    bool is_warning;
    bool inverse;
    double h;
    double dx;
    void attach(string name);
    void Warning()
    {
      is_warning = false;
    }
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

void Warning::attach(string name)
{
  Attach(0, name, 0, 3);
}

string Warning::get(int param)
{
  string results[];
  StringSplit(Name(), '_', results);
  return(results[param]);
}

void Warning::create(Wave_label *&waves[])
{
  Create(0, get_name(waves[0].Name()) + SEPARATOR + get_name(waves[1].Name()) + SEPARATOR + get_name(waves[2].Name()) + SEPARATOR + name, 0, 0, 0, 0, 0, 0, 0);
  update(waves, waves[2].Timeframes());
}

double Warning::get_tn_line1(Coord &coord)
{
  return((coord.price4 - coord.price1) / (coord.pos4 - coord.pos1));
}

double Warning::get_tn_line4(Coord &coord)
{
  return((coord.price3 - coord.price2) / (coord.pos3 - coord.pos2));
}

double Warning::get_coord_on_line1(int pos, Coord &coord)
{
  double tn = get_tn_line1(coord);
  double price = coord.price1 + tn * (pos - coord.pos1);
  return(price);
}

double Warning::get_coord_on_line2(int pos, Coord &coord)
{
  double tn = get_tn_line1(coord);
  double price = coord.price2 + tn * (pos - coord.pos2);
  return(price);
}

double Warning::get_coord_on_line3(int pos, Coord &coord)
{
  double tn = get_tn_line1(coord);
  double price = coord.price3 + tn * (pos - coord.pos3);
  return(price);
}

double Warning::get_coord_on_line4(int pos, Coord &coord)
{
  double tn = get_tn_line4(coord);
  double price = coord.price3 + tn * (coord.pos3 - pos);
  return(price);
}

void Warning::get_coord4(Coord &coord)
{
  coord.pos4 = (coord.pos2 + coord.pos3) / 2.0;
  coord.price4 = (coord.price2 + coord.price3) / 2.0;
}

void Warning::set_coordinates(Wave_label *&waves[])
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
  double pos1 = coord.pos4;
  double pos2 = pos1 + (coord.pos4 - coord.pos1);
  double pos3;
  double price1 = coord.price4;
  double price2 = price1 + (coord.price4 - coord.price1);
  double price3;
  if(name == NAME_WARNING_UP)
  {
    if(coord.price3 >= coord.price2)
    {
      pos3 = coord.pos3;
      price3 = coord.price3;
    }
    else
    {
      pos3 = coord.pos2;
      price3 = coord.price2;
    }
  }
  else
  {
    if(coord.price3 >= coord.price2)
    {
      pos3 = coord.pos2;
      price3 = coord.price2;
    }
    else
    {
      pos3 = coord.pos3;
      price3 = coord.price3;
    }
  }
  if(MathFloor(pos1) != MathCeil(pos1))
  {
    pos1 = MathCeil(pos1);
    pos2 = MathCeil(pos2);
    price1 = get_coord_on_line1((int) pos1, coord);
    price2 = get_coord_on_line1((int) pos2, coord);
  }
  Time(0, bar_to_time((int) pos1));
  Time(1, bar_to_time((int) pos2));
  Time(2, bar_to_time((int) pos3));
  Price(0, price1);
  Price(1, price2);
  Price(2, price3);
}

void Warning::update(Wave_label *&waves[], int tf)
{
  Name(get_name(waves[0].Name()) + SEPARATOR + get_name(waves[1].Name()) + SEPARATOR + get_name(waves[2].Name()) + SEPARATOR + name);
  set_coordinates(waves);
  RayRight(true);
  RayLeft(false);
  Timeframes(tf);
  Z_Order(0);
  Color(clrNONE);
  set_levels();
}

void Warning::set_levels()
{
  LevelsCount(warning_levels.Total());
  int i = 0;
  while(i < warning_levels.Total())
  {
    LevelValue(i, warning_levels.At(i));
    LevelDescription(i, DoubleToString(warning_levels.At(i) * 100, 1));
    Descr *descr = descrs.At((int) get(PITCHFORK_LEVEL3));
    LevelColor(i, descr.clr);
    LevelStyle(i, STYLE_DOT);
    LevelWidth(i, 1);
    i++;
  }
}

int Warning::get_pos(int point)
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

string Warning::get_name(string name)
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
