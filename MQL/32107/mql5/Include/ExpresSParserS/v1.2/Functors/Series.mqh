//+------------------------------------------------------------------+
//|                                                       Series.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#ifndef EXTENDED_FUNCTORS
  #define EXTENDED_FUNCTORS
#endif

#include "../Functors.mqh"


enum SERIES_FUNC_TYPE
{
  TIME,
  OPEN,
  HIGH,
  LOW,
  CLOSE,
  VOLUME,
  REALVOLUME,
  SPREAD,
};

class SeriesFunc: public BaseFunc
{
  private:
    static BaseFuncFactory factory;
    
  protected:
    const SERIES_FUNC_TYPE type;
    
    static BaseFunc *legend(void)
    {
      string msg = "Attached Series funcs:";
      for(int i = SERIES_FUNC_TYPE::TIME; i <= SERIES_FUNC_TYPE::SPREAD; i++)
      {
        msg += EnumToString((SERIES_FUNC_TYPE)i) + ";";
      }
      Print(msg);
      return NULL;
    }
    
  public:
    SeriesFunc(const string n, const int t): BaseFunc(n), type((SERIES_FUNC_TYPE)t)
    {
    }
    
    static BaseFunc *create(const string name)
    {
      if(name == NULL)
      {
        return legend();
      }

      for(int i = SERIES_FUNC_TYPE::TIME; i <= SERIES_FUNC_TYPE::SPREAD; i++)
      {
        if(name == EnumToString((SERIES_FUNC_TYPE)i))
        {
          return new SeriesFunc(name, i);
        }
      }
      return NULL;
    }
    
    double execute(const double &params[]) override
    {
      const string symbol = _commonFunctionTable.getSymbol();
      const ENUM_TIMEFRAMES period = _commonFunctionTable.getTimeframe();
      const int bar = (int)params[0];
      switch(type)
      {
        case TIME: return (double)iTime(symbol, period, bar);
        case OPEN: return iOpen(symbol, period, bar);
        case HIGH: return iHigh(symbol, period, bar);
        case LOW:  return iLow(symbol, period, bar);
        case CLOSE: return iClose(symbol, period, bar);
        case VOLUME: return (double)iVolume(symbol, period, bar);
        case REALVOLUME: return (double)iRealVolume(symbol, period, bar);
        case SPREAD: return (double)iSpread(symbol, period, bar);
      }
      return nan;
    }
};

static BaseFuncFactory SeriesFunc::factory(SeriesFunc::create);
