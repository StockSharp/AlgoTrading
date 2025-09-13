//+------------------------------------------------------------------+
//|                                                         wave.mqh |
//|                                                  Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Object.mqh>

class Wave : public CObject
{
  public:
    string name;
    int level;
    int num_zigzag;
    string trend;
    //Wave *parent_wave;
    double value[6];
    int index[6];
    int fixed[6];
    double maximum[6];
    double minimum[6];
    double length_ratio[6][6];
    double time_ratio[6][6];
    double length[6];
    double time[6];
    double value_fibo_score;
    double time_fibo_score;
    double proportion_fibo_score;
    double fibo_score;
    double pattern_score;
};
