//+------------------------------------------------------------------+
//|                                                     patterns.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Object.mqh>
#include <Arrays\ArrayObj.mqh>
#include <Arrays\ArrayDouble.mqh>
#include <Arrays\ArrayString.mqh>
#include <EWM\defines.mqh>
#include "wave.mqh"
#include "rules.mqh"

class Target : public CObject
{
  public:
    string type;
    int num_wave1;
    int num_wave2;
    int num_wave3;
    int num_wave4;
    double ratio;
};

class Targets : public CArrayObj
{

};

class Time_targets : public Targets
{

};

class Fibo : public CObject
{
  public:
    int num_wave1;
    int num_wave2;
    double score;
    double low;
    double middle;
    double high;
};

class Value_fibos : public CArrayObj
{
  public:
    double get_score(int num_wave1, int num_wave2, double ratio);
};

double Value_fibos::get_score(int num_wave1, int num_wave2, double ratio)
{
  double score = 0;
  for(int i = 0; i < Total(); i++)
  {
    Fibo *fibo = At(i);
    if(fibo.num_wave1 == num_wave1 && fibo.num_wave2 == num_wave2)
    {
      if(ratio > fibo.low && ratio < fibo.high)
      {
        if(ratio < fibo.middle)
        {
          score += fibo.score * (1 - (ratio - fibo.low) / (fibo.middle - fibo.low)) * 100;
        }
        else if(ratio > fibo.middle)
        {
          score += fibo.score * (1 - (fibo.high - ratio) / (fibo.high - fibo.middle)) * 100;
        }
        else
        {
          score += fibo.score*100;
        }
      }
    }
  }
  return(score);
}

class Time_fibos : public Value_fibos
{

};

class Proportion_fibos : public Value_fibos
{

};

class Proportion_fibos_required : public Value_fibos
{

};

class Subwaves_descr : public CObject
{
  public:
    string wave_label;
    double ratio1;
    double ratio2;
    int num_wave1;
    int num_wave2;
    CArrayDouble probability;
    CArrayString name_wave;
};

class Subwaves : public CArrayObj
{

};

class Pattern : public CObject
{
  public:
    string name;
    string type;
    double probability;
    string descr;
    int num_wave;
    Rules rules;
    Guidelines guidelines;
    Subwaves subwaves;
    Rules entry_signals;
    Rules exit_signals;
    Rules stop_signals;
    Rules wave_signals;
    Rules confirm_signals;
    Time_fibos time_fibos;
    Value_fibos value_fibos;
    Proportion_fibos proportion_fibos;
    Proportion_fibos_required proportion_fibos_required;
    Targets targets;
    Time_targets time_targets;
    bool check(Wave *wave);
};

bool Pattern::check(Wave *wave)
{
  bool b = true;
  int j = -1;
  int k = -1;
  for(int i = 0; i <= 5; i++)
  {
    if(wave.fixed[i] == 1 && b)
    {
      j = i;
      b = false;
    }
    else if(wave.fixed[i] == 1)
    {
      k = i;
    }
  }
  int trend1 = 0;
  int trend2 = 0;
  for(int i = j; i < k; i++)
  {
    if((i % 2 == 0 && wave.value[i + 1] > wave.value[i]) || (i % 2==1 && wave.value[i] > wave.value[i + 1]))
    {
      trend1++;
    }
    else if((i % 2 == 0 && wave.value[i] > wave.value[i + 1]) || (i % 2 == 1 && wave.value[i + 1] > wave.value[i]))
    {
      trend2++;
    }
  }
  if((trend1 == 0 && trend2 == 0) || (trend1 > 0 && trend2 > 0))
  {
    return(false);
  }
  else if(trend1 == k - j)
  {
    wave.trend = TREND_UP;
  }
  else if(trend2 == k - j)
  {
    wave.trend = TREND_DOWN;
  }
  for(int i = 1; i <= 5; i++)
  {
    if((wave.fixed[i - 1]==0 && wave.fixed[i]==1) || (wave.fixed[i - 1]==1 && wave.fixed[i]==1) || (wave.fixed[i - 1] == 1 && wave.fixed[i] == 0))
    {
      wave.maximum[i] = rates[wave.index[i]].high;
      wave.minimum[i] = rates[wave.index[i]].low;
      for(j = wave.index[i - 1]; j <= wave.index[i]; j++)
      {
        if(rates[j].high > wave.maximum[i])
        {
          wave.maximum[i] = rates[j].high;
        }
        if(rates[j].low < wave.minimum[i])
        {
          wave.minimum[i] = rates[j].low;
        }
      }
      if(wave.fixed[i - 1] == 0 && wave.fixed[i] == 1)
      {
        if((i % 2 == 0 && wave.trend == TREND_UP) || (i % 2 == 1 && wave.trend == TREND_DOWN))
        {
          wave.value[i - 1] = wave.maximum[i];
        }
        else
        {
          wave.value[i - 1] = wave.minimum[i];
        }
      }
      else if(wave.fixed[i - 1] == 1 && wave.fixed[i] == 0)
      {
        if((i % 2 == 0 && wave.trend == TREND_UP) || (i % 2 == 1 && wave.trend == TREND_DOWN))
        {
          wave.value[i] = wave.minimum[i];
        }
        else
        {
          wave.value[i] = wave.maximum[i];
        }
      }
      wave.length[i] = MathAbs(wave.value[i] - wave.value[i - 1]);
      wave.time[i] = wave.index[i] - wave.index[i - 1] + 1/2;
    }
  }
  if( ! rules.get_result(wave))
  {
    return(false);
  }
  for(int i = 1; i <= 5; i++)
  {
    for(j = 1; j <= 5; j++)
    {
      wave.length_ratio[i][j] = -1;
      wave.time_ratio[i][j] = -1;
      if(wave.fixed[i] == 1 && wave.fixed[i - 1] == 1 && wave.fixed[j]==1 && wave.fixed[j - 1] == 1)
      {
        wave.length_ratio[i][j] = wave.length[i] / wave.length[j];
        wave.time_ratio[i][j] = wave.time[i] / wave.time[j];
        wave.value_fibo_score += value_fibos.get_score(i, j, wave.length_ratio[i][j]);
        wave.time_fibo_score += time_fibos.get_score(i, j, wave.time_ratio[i][j]);
        wave.proportion_fibo_score += proportion_fibos.get_score(i, j, wave.length_ratio[i][j]);
      } 
    }
  }
  wave.fibo_score = 0.25 * wave.value_fibo_score + 0.25 * wave.time_fibo_score + 0.5 * wave.proportion_fibo_score;
  wave.pattern_score = 0.11 * probability + 0.3 * wave.fibo_score;
  return(true);
}

class Patterns : public CArrayObj
{
  public:
    Pattern * find(string name);
    void clear();
};

Patterns patterns;

Pattern *Patterns::find(string name)
{
  for(int i = 0; i < Total(); i++)
  {
    Pattern *pattern = At(i);
    if(pattern.name == name)
    {
      return(pattern);
    }
  }
  return(NULL);
}