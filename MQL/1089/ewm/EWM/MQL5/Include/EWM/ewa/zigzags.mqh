//+------------------------------------------------------------------+
//|                                                      zigzags.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Object.mqh>
#include <Arrays\ArrayInt.mqh>
#include <Arrays\ArrayDouble.mqh>
#include <Arrays\ArrayObj.mqh>
#include <EWM\defines.mqh>

class Zigzag : public CObject
{
  CArrayInt index;
  CArrayDouble value;
  public:
    void Zigzag(int param, int index1, int index2, int type);
    int get_total();
    CArrayInt *get_index();
    CArrayDouble *get_value();
    int find_index(int key, bool less);
};

void Zigzag::Zigzag(int param, int index1, int index2, int type)
{
  bool up = true;
  double dparam;
  if(_Digits == 5 || _Digits == 3)
  {
    dparam = param * _Point * 10;
  }
  else
  {
    dparam = param * _Point;
  }
  int max_bar = index1;
  int min_bar = index1;
  double max = rates[max_bar].high;
  double min = rates[min_bar].low;
  bool is_first;
  if(type == TYPE1 || type == TYPE3)
  {
    is_first = false;
  }
  else
  {
    is_first = true;
  }
  for(int i = index1 + 1; i <= index2; i++)
  {
    if(up == true || ! is_first)
    {
      if(rates[i].high > max)
      {
        max = rates[i].high;
        max_bar = i;
      }
      else if(rates[i].low < max - dparam)
      {
        value.Add(max);
        index.Add(max_bar);
        up = false;
        min = rates[i].low;
        min_bar = i;
        is_first = true;
        continue;
      }
    }
    if( ! up || ! is_first)
    {
      if(rates[i].low < min)
      {
        min = rates[i].low;
        min_bar = i;
      }
      else if(rates[i].high > min + dparam)
      {
        value.Add(min);
        index.Add(min_bar);
        up = true;
        max = rates[i].high;
        max_bar = i;
        is_first = true;
      }
    }
  }
  if(type == TYPE1 || type == TYPE2)
  {
    if(up)
    {
      value.Add(max);
      index.Add(max_bar);
    }
    else
    {
      value.Add(min);
      index.Add(min_bar);
    }
  }
}

int Zigzag::get_total()
{
  return(index.Total());
}

CArrayInt *Zigzag::get_index()
{
  return(GetPointer(index));
}

CArrayDouble *Zigzag::get_value()
{
  return(GetPointer(value));
}

int Zigzag::find_index(int key, bool less)
{
  int left = 0;
  int right = index.Total() - 1;
  while(left <= right)
  {
    int middle = (left + right) / 2;
    if(key < index.At(middle))
    {
      right = middle - 1;
    }
    else if(key > index.At(middle))
    {
      left = middle + 1;
    }
    else
    {
      return(middle);
    }
  }
  if(less)
  {
    return(right);
  }
  else
  {
    return(left);
  }
}

class Points
{
  public:
    CArrayInt index;
    CArrayDouble value;
    int num_zigzag;
};

class Zigzags : public CArrayObj
{
  public:
    void create(int index1, int index2, int type);
    void remove();
    bool get_points(int num_points, int index1, int index2, double value1, double value2, Points *points, int num_zigzag);
    int find(int key, CArrayInt *index, bool b);
};

Zigzags zigzags;

void Zigzags::create(int index1, int index2, int type)
{
  int param = 1;
  Zigzag *zigzag = new Zigzag(param++, index1, index2, type);
  if(zigzag.get_total() >= 2)
  {
    Add(zigzag);
    while(true)
    {
      zigzag = new Zigzag(param++, index1, index2, type);
      if(zigzag.get_total() < 2)
      {
        delete zigzag;
        break;
      }
      Zigzag *temp_zigzag = At(Total() - 1);
      CArrayInt *index = zigzag.get_index();
      if(temp_zigzag.get_total() == zigzag.get_total() && index.CompareArray(temp_zigzag.get_index()))
      {
        delete zigzag;
      }
      else
      {
        Add(zigzag);
      }
    }
  }
  else
  {
    delete zigzag;
  }
}

void Zigzags::remove()
{
  FreeMode(true);
  Clear();
}

bool Zigzags::get_points(int num_points, int index1, int index2, double value1, double value2, Points *points, int num_zigzag)
{
  points.num_zigzag = -1;
  points.index.Clear();
  points.value.Clear();
  for(int i = num_zigzag; i >= 0; i--)
  {
    Zigzag *zigzag = At(i);
    CArrayInt *index = zigzag.get_index();
    int temp_index1 = zigzag.find_index(index1, false);
    int temp_index2 = zigzag.find_index(index2, true);
    int n = temp_index2 - temp_index1 + 1;
    if(n >= num_points)
    {
      CArrayDouble *value = zigzag.get_value();
      if((value1 > 0 && value.At(temp_index1) != value1 && index.At(temp_index1) != index1) || (value2 > 0 && value.At(temp_index2) != value2 && index.At(temp_index2) != index2))
      {
        continue;
      }
      points.num_zigzag = i;
      for(int j = temp_index1; j <= temp_index2; j++)
      {
        points.index.Add(index.At(j));
        points.value.Add(value.At(j));
      }
      return(true);
    }
  }
  return(false);
}
