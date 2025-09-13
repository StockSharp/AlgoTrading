//+------------------------------------------------------------------+
//|                                                       schiff.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <ChartObjects\ChartObjectsChannels.mqh>
#include <EWM\defines.mqh>
#include <EWM\ewm\wave_label.mqh>
#include <EWM\functions.mqh>

class Schiff : public CChartObjectPitchfork
{
  public:
    bool schiff;
    void Schiff()
    {
      schiff = false;
    }
    void attach(string name);
    int get_level();
    color get_color();
    string get(int param);
    void create(Wave_label *&wave_label[]);
    void update(Wave_label *&wave_label[], int tf);
    void set_coordinates(Wave_label *&wave_labels[]);
    bool Time(int point, datetime time)
    {
      return(ObjectSetInteger(0, this.Name(), OBJPROP_TIME, point, time));
    }
    bool Price(int point, double price)
    {
      return(ObjectSetDouble(0, this.Name(), OBJPROP_PRICE, point, price));
    }
    int get_pos(int point);
    void set_levels(double &a_levels[]);
    string get_name(string name);
    void copy(long id);
    double get_tn(Coord &coord);
    double get_coord_on_line1(int pos, Coord &coord);
    double get_coord_on_line2(int pos, Coord &coord);
    double get_coord_on_line3(int pos, Coord &coord);
    double get_coord_on_line4(int pos, Coord &coord);
    void get_coord4(Coord &coord);
};

void Schiff::attach(string name)
{
  Attach(0, name, 0, 3);
  if(LevelsCount() == 2)
  {
    schiff = false;
  }
  else
  {
    schiff = true;
  }
}

string Schiff::get(int param)
{
  string results[];
  StringSplit(Name(), '_', results);
  return(results[param]);
}

void Schiff::create(Wave_label *&wave_label[])
{
  Create(0, get_name(wave_label[0].Name()) + SEPARATOR + get_name(wave_label[1].Name()) + SEPARATOR + get_name(wave_label[2].Name()) + SEPARATOR + NAME_SCHIFF, 0, 0, 0, 0, 0, 0, 0);
  update(wave_label, wave_label[2].Timeframes());
}

double Schiff::get_tn(Coord &coord)
{
  return((coord.price4 - coord.price1) / (coord.pos4 - coord.pos1));
}

double Schiff::get_coord_on_line1(int pos, Coord &coord)
{
  double tn = get_tn(coord);
  double price = coord.price1 + tn * (pos - coord.pos1);
  return(price);
}

double Schiff::get_coord_on_line2(int pos, Coord &coord)
{
  double tn = get_tn(coord);
  double price = coord.price2 + tn * (pos - coord.pos2);
  return(price);
}

double Schiff::get_coord_on_line3(int pos, Coord &coord)
{
  double tn = get_tn(coord);
  double price = coord.price3 + tn * (pos - coord.pos3);
  return(price);
}

double Schiff::get_coord_on_line4(int pos, Coord &coord)
{
  double tn = (coord.price2 - coord.price3) / (coord.pos3 - coord.pos2);
  double price = coord.price3 + tn * (coord.pos3 - pos);
  return(price);
}

void Schiff::get_coord4(Coord &coord)
{
  coord.pos4 = (coord.pos2 + coord.pos3) / 2;
  coord.price4 = (coord.price2 + coord.price3) / 2;
}

void Schiff::set_coordinates(Wave_label *&wave_label[])
{
  Time(0, wave_label[0].get_time());
  Price(0, wave_label[0].get_price());
  Time(1, wave_label[1].get_time());
  Price(1, wave_label[1].get_price());
  Time(2, wave_label[2].get_time());
  Price(2, wave_label[2].get_price());
  Coord coord;
  coord.pos1 = (get_pos(0) + get_pos(1)) / 2.0;
  coord.pos2 = get_pos(1);
  coord.pos3 = get_pos(2);
  coord.price1 = (Price(0) + Price(1)) / 2;
  coord.price2 = Price(1);
  coord.price3 = Price(2);
  get_coord4(coord);
  if(MathFloor(coord.pos1) != MathCeil(coord.pos1))
  {
    coord.pos1 = MathFloor(coord.pos1);
    coord.price1 = get_coord_on_line1((int) coord.pos1, coord);
  }
  Price(0, coord.price1);
  Price(1, coord.price2);
  Price(2, coord.price3);
  Time(0, bar_to_time((int) coord.pos1));
  Time(1, bar_to_time((int) coord.pos2));
  Time(2, bar_to_time((int) coord.pos3));
}

void Schiff::update(Wave_label *&wave_label[], int tf)
{
  Name(get_name(wave_label[0].Name()) + SEPARATOR + get_name(wave_label[1].Name()) + SEPARATOR + get_name(wave_label[2].Name()) + SEPARATOR + NAME_SCHIFF);
  set_coordinates(wave_label);
  Timeframes(tf);
  Color(clrNONE);
  RayLeft(false);
  RayRight(true);
  Width(1);
  Style(STYLE_DASH);
  if( ! schiff)
  {
    set_levels(schiff_levels1);
  }
  else
  {
    set_levels(schiff_levels2);
  }
  Z_Order(0);
}

void Schiff::set_levels(double &levels[])
{
  LevelsCount(ArrayRange(levels, 0));
  for(int i = 0; i < ArrayRange(levels, 0); i++)
  {
    LevelValue(i, levels[i]);
    LevelDescription(i, "");
    Descr *descr = descrs.At((int) get(PITCHFORK_LEVEL3));
    LevelColor(i, descr.clr);
    LevelStyle(i, STYLE_DASH);
    LevelWidth(i, 1);
  }
}

string Schiff::get_name(string name)
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

int Schiff::get_pos(int point)
{
  datetime times[], temp_times[];
  while(CopyTime(_Symbol, _Period, 0, 1, times) != 1)
  {
    Sleep(DELAY);
  }
  int bars1 = Bars(_Symbol, _Period);
  int bars2 = Bars(_Symbol, _Period, Time(point), times[0]);
  return(bars1 - bars2);
}
