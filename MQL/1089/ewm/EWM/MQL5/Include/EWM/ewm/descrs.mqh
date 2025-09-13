//+------------------------------------------------------------------+
//|                                                       Descrs.mqh |
//|                                  Copyright 2012, Roman Martynyuk |
//|                                           http://www.dml-ewa.ru/ |
//+------------------------------------------------------------------+
#property copyright "Copyright 2012, Roman Martynyuk"
#property link      "http://www.dml-ewa.ru/"

#include <Object.mqh>
#include <Arrays\ArrayObj.mqh>

class Descr : public CObject
{
  public:
    int num_level;
    string level;
    string labels[15];
    string font;
    int font_size;
    int size_in_px;
    color clr;
};

class Descrs : public CArrayObj
{

};

Descrs descrs;