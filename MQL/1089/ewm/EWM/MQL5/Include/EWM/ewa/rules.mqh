//+------------------------------------------------------------------+
//|                                                        rules.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Object.mqh>
#include <Arrays\ArrayObj.mqh>
#include <EWM\defines.mqh>
#include <EWM\ewa\wave.mqh>

class Rule;

class Logical : public CObject
{
  public:
    Rule *rule1;
    Rule *rule2;
    string rule_name1;
    string rule_name2;
};

class And : public Logical
{

};

class Or: public Logical
{

};

class Rule : public CObject
{
  public:
    string type;
    string name;
    CObject *descr;
    void set_descr(CObject *descr);
    int check(Wave *wave, int t);
};

void Rule::set_descr(CObject *descr)
{
  this.descr = descr;
}

int Rule::check(Wave *wave, int t)
{
  int result = -1;
  if(type == "Or")
  {
    Or *or = descr;
    if(or.rule1.check(wave, t) == 1 || or.rule2.check(wave, t) == 1)
    {
      result = 1;
    }
    else if(or.rule1.check(wave, t) != 0 || or.rule2.check(wave, t) != 0)
    {
      result = -1;
    }
    else
    {
      result = 0;
    }
  }
  else if(type == "And")
  {
    And *and = descr;
    if(and.rule1.check(wave, t) == 1 && and.rule2.check(wave, t) == 1)
    {
      result = 1;
    }
    else if(and.rule1.check(wave, t) != 0 && and.rule2.check(wave, t) != 0)
    {
      result = -1;
    }
    else
    {
      result = -1;
    }
  }
  else if(type == "RelativePosition")
  {
    Relative_position *relative_position = descr;
    result = relative_position.check(wave, t);
  }
  else if(type == "LengthRatio")
  {
    Fibonacci *fibonacci = descr;
    result = fibonacci.check(wave, t);
  }
  else if(type == "InternalRetrace")
  {
    Internal_retrace *internal_retrace = descr;
    result = internal_retrace.check(wave, t);
  }
  return(result);
}

class Internal_retrace : public CObject
{
  public:
    int num_wave;
    double ratio;
    int check(Wave *wave, int type);
};

int Internal_retrace::check(Wave *wave, int type)
{
  int result = -1;
  if(wave.fixed[num_wave] == 1)
  {
  if((wave.trend == TREND_UP && num_wave % 2 == 1) || (wave.trend == TREND_DOWN && num_wave % 2 == 0))
    {
     if(wave.maximum[num_wave] == wave.value[num_wave - 1] || wave.maximum[num_wave] == wave.minimum[num_wave])
     {
       result = (1 >= ratio) ? 1 : 0;
     }
     else if((wave.fixed[num_wave - 1] == 1 && (wave.value[num_wave] - wave.value[num_wave - 1]) / (wave.maximum[num_wave] - wave.value[num_wave - 1]) >= ratio) || (wave.fixed[num_wave - 1] == 0 && (wave.value[num_wave] - wave.minimum[num_wave]) / (wave.maximum[num_wave] - wave.minimum[num_wave]) >= ratio))
     {
       result = 1;
     }
     else
     {
       result = 0;
     }
    }
  else if((wave.trend == TREND_UP && num_wave % 2 == 0) || (wave.trend == TREND_DOWN && num_wave % 2 == 1))
    {
     if(wave.value[num_wave - 1] == wave.minimum[num_wave] || wave.maximum[num_wave] == wave.minimum[num_wave])
     {
       result = (1 >= ratio) ? 1 : 0;
     }
     else if((wave.fixed[num_wave - 1] == 1 && (wave.value[num_wave - 1] - wave.value[num_wave]) / (wave.value[num_wave - 1] - wave.minimum[num_wave]) >= ratio) || (wave.fixed[num_wave - 1] == 0 && (wave.maximum[num_wave] - wave.value[num_wave]) / (wave.maximum[num_wave] - wave.minimum[num_wave]) >= ratio))
     {
       result = 1;
     }
     else
     {
       result = 0;
     }
    }
  }
  return(result);
}

class Relative_position : public CObject
{
  public:
    string mod1;
    string mod2;
    int num_wave1;
    int num_wave2;
    string sign;
    int check(Wave *wave, int type);
    double get_value(int &num_wave, string mod, Wave *wave);
};

double Relative_position::get_value(int &num_wave, string mod, Wave *wave)
{
  double value;
  if(mod == MIN)
  {
    if(wave.trend == TREND_UP && num_wave % 2 == 0)
    {
      value = wave.minimum[num_wave];
    }
    else if(wave.trend == TREND_DOWN && num_wave % 2 == 0)
    {
      value = wave.maximum[num_wave];
    }
    else if(wave.trend == TREND_UP && num_wave % 2 == 1)
    {
      value = wave.minimum[num_wave];
      num_wave--;
    }
    else if(wave.trend == TREND_DOWN && num_wave % 2 == 1)
    {
      value = wave.maximum[num_wave];
      num_wave--;
    }
  }
  else if(mod == MAX)
  {
    if(wave.trend == TREND_UP && num_wave % 2 == 1)
    {
      value = wave.maximum[num_wave];
    }
    else if(wave.trend == TREND_DOWN && num_wave % 2 == 1)
    {
      value = wave.minimum[num_wave];
    }
    else if(wave.trend == TREND_UP && num_wave % 2 == 0)
    {
      value = wave.maximum[num_wave];
      num_wave--;
    }
    else if(wave.trend == TREND_DOWN && num_wave % 2 == 0)
    {
      value = wave.minimum[num_wave];
      num_wave--;
    }
  }
  return(value);
}

int Relative_position::check(Wave *wave, int type)
{
  int result = -1;
  double value1 = wave.value[num_wave1];
  int temp_num_wave1 = num_wave1;
  int temp_num_wave2 = num_wave2;
  if(mod1 != "val")
  {
    value1 = get_value(temp_num_wave1, mod1, wave);
  }
  double value2 = wave.value[num_wave2];
  if(mod2 != "val")
  {
    value2 = get_value(temp_num_wave2, mod2, wave);
  }
  if((wave.fixed[temp_num_wave1] == 1 && wave.fixed[temp_num_wave2] == 1) || (wave.fixed[temp_num_wave1] == 0 && temp_num_wave1 % 2 == 0 && wave.fixed[temp_num_wave2] == 1) || (wave.fixed[temp_num_wave1] == 1 && wave.fixed[temp_num_wave2] == 0 && temp_num_wave2 % 2 == 1) || (wave.fixed[temp_num_wave1] == 0 && temp_num_wave1 % 2 == 0 && wave.fixed[temp_num_wave2] == 0 && temp_num_wave2 % 2 == 1))
  {
    result = ((wave.trend == TREND_UP && value1 >= value2) || (wave.trend == TREND_DOWN && value1 <= value2)) ? 1 : 0;
  }
  return(result);
}

class Fibonacci : public CObject
{
  public:
    string mod;
    int num_wave1;
    int num_wave2;
    int num_wave3;
    int num_wave4;
    string sign;
    string type;
    double ratio;
    int check(Wave *wave, int type);
    double get_length(int num_wave1, int num_wave2, Wave *wave);
};

double Fibonacci::get_length(int num_wave1, int num_wave2, Wave *wave)
{
  double length = 0;
  if(wave.fixed[num_wave1] == 1 && wave.fixed[num_wave2] == 1)
  {
    if(type == LENGTH)
    {
      length = MathAbs(wave.value[num_wave1] - wave.value[num_wave2]);
    }
    else if(type == TIME)
    {
      length = MathAbs(wave.index[num_wave1] - wave.index[num_wave2]) + 1/2;
    }
  }
  else if(wave.fixed[num_wave1] == 1 && wave.fixed[num_wave2] != 1)
  {
    int i = num_wave2;
    while(wave.fixed[i] == -1)
    {
      i++;
    }
    if(type == LENGTH)
    {
      for(int j = num_wave1; j >= i + 1; j--)
      {
        if((wave.trend == TREND_UP && num_wave2 % 2 == 0) || (wave.trend == TREND_DOWN && num_wave2 % 2 == 1))
        {
          length = MathMax(length, MathAbs(wave.value[num_wave1] - wave.minimum[j]));
        }
        else if((wave.trend == TREND_UP && num_wave2 % 2 == 1) || (wave.trend == TREND_DOWN && num_wave2 % 2 == 0))
        {
          length = MathMax(length, MathAbs(wave.value[num_wave1] - wave.maximum[j]));
        }
      }
    }
    else
    {
      length = MathAbs(wave.index[num_wave1] - wave.index[i]);
    }
  }
  return(length);
}

int Fibonacci::check(Wave *wave, int type)
{
  int result = -1;
  if(num_wave1 < num_wave3)
  {
    int temp = num_wave1;
    num_wave1 = num_wave3;
    num_wave3 = temp;
    temp = num_wave2;
    num_wave2 = num_wave4;
    num_wave4 = temp;
    sign = ((sign == LESS) ? MORE : LESS);
    ratio = 1 / ratio;
  }
  if(((sign == MORE && num_wave1 % 2 == 1) || (sign == LESS && num_wave1 % 2 == 0)) && wave.fixed[num_wave1] == 1 && wave.fixed[num_wave2] == 1 && wave.fixed[num_wave3] == 1)
  {
    double length = get_length(num_wave3, num_wave4, wave);
    double critical_value = ((wave.trend == TREND_UP && sign == MORE) || (wave.trend == TREND_DOWN && sign == LESS)) ? wave.value[num_wave2] + ratio * length : wave.value[num_wave2] - ratio * length;
    double value = wave.value[num_wave1];
    if(mod != VAL)
    {
      if((mod == MIN && wave.trend == TREND_UP) || (mod == MAX && wave.trend==TREND_DOWN))
      {
        value = wave.minimum[num_wave1];
      }
      else if((mod == MAX && wave.trend == TREND_UP) || (mod == MIN && wave.trend==TREND_DOWN))
      {
        value = wave.maximum[num_wave1];
      }
    }
    result = ((((sign == MORE && wave.trend == TREND_UP) || (sign == LESS && wave.trend == TREND_DOWN)) && value >= critical_value) || (((sign == MORE && wave.trend == TREND_DOWN) || (sign == LESS && wave.trend == TREND_UP)) && value <= critical_value)) ? 1 : 0;
  }
  else if(((sign == LESS && num_wave1 % 2 == 1) || (sign == MORE && num_wave1 % 2 == 0)) && wave.fixed[num_wave2] == 1 && wave.fixed[num_wave3] == 1 && wave.fixed[num_wave4] == 1)
  {
    double length = get_length(num_wave3, num_wave4, wave);
    double critical_value = ((wave.trend == TREND_UP && sign == LESS) || (wave.trend == TREND_DOWN && sign == MORE)) ? wave.value[num_wave2] + ratio * length : wave.value[num_wave2] - ratio * length;
    double value = wave.value[num_wave1];
    if(mod != VAL)
    {
      if((mod == MIN && wave.trend == TREND_UP) || (mod == MAX && wave.trend==TREND_DOWN))
      {
        value = wave.minimum[num_wave1];
      }
      else if((mod == MAX && wave.trend == TREND_UP) || (mod == MIN && wave.trend==TREND_DOWN))
      {
        value = wave.maximum[num_wave1];
      }
    }
    result = ((((sign == LESS && wave.trend == TREND_UP) || (sign == MORE && wave.trend == TREND_DOWN)) && value <= critical_value) || (((sign == LESS && wave.trend == TREND_DOWN) || (sign == MORE && wave.trend == TREND_UP)) && value >= critical_value)) ? 1 : 0;
  
  }
  return(result);
}

class Rules : public CArrayObj
{
  public:
    Rule *find(string name);
    bool get_result(Wave *wave);
    void clear();
    int type;
};

Rules rules;

bool Rules::get_result(Wave *wave)
{
  for(int i = 0; i < Total(); i++)
  {
    Rule *rule = At(i);
    if(rule.check(wave, type) == 0)
    {
      return(false);
    }
  }
  return(true);
}

Rule *Rules::find(string name)
{
  for(int i = 0; i < Total(); i++)
  {
    Rule *rule = At(i);
    if(rule.name == name)
    {
      return(rule);
    }
  }
  return(NULL);
};

void Rules::clear()
{
  for(int i = 0; i < Total(); i++)
  {
    Rule *rule = At(i);
    delete rule.descr;
  }
}

class Guidelines : public Rules
{

};

class Entry_signals : public Rules
{

};

class Exit_signals : public Rules
{

};

class Stop_signals : public Rules
{

};

class Wave_signals : public Rules
{

};

class Confirm_signals : public Rules
{

};