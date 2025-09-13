//+------------------------------------------------------------------+
//|                                                   Indicators.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#ifndef EXTENDED_FUNCTORS
  #define EXTENDED_FUNCTORS
#endif

#include "../Functors.mqh"


class MAIndicatorFunc: public BaseFunc
{
  private:
    static BaseFuncFactory factory;

  protected:
    const int handle;

    static BaseFunc *legend(void)
    {
      const string msg = "Attached Indicators funcs:(MAs and multi-MAs, e.g. SMA_OPEN_10, M_SMA_OPEN_5_2_11)";
      Print(msg);
      return NULL;
    }
    
  public:
    MAIndicatorFunc(const string n, const int h): BaseFunc(n), handle(h) {}
    
    ~MAIndicatorFunc()
    {
      IndicatorRelease(handle);
    }
    
    static BaseFunc *create(const string name) // SMA_OPEN_10(0)
    {
      if(name == NULL)
      {
        return legend();
      }

      string parts[];
      if(StringSplit(name, '_', parts) != 3) return NULL;

      ENUM_MA_METHOD m = -1;
      ENUM_APPLIED_PRICE t = -1;
      
      static string methods[] = {"SMA", "EMA", "SMMA", "LWMA"};
      for(int i = 0; i < ArraySize(methods); i++)
      {
        if(parts[0] == methods[i])
        {
          m = (ENUM_MA_METHOD)i;
          break;
        }
      }

      if(m == -1) return NULL;

      static string types[] = {"NULL", "CLOSE", "OPEN", "HIGH", "LOW", "MEDIAN", "TYPICAL", "WEIGHTED"};
      for(int i = 1; i < ArraySize(types); i++)
      {
        if(parts[1] == types[i])
        {
          t = (ENUM_APPLIED_PRICE)i;
          break;
        }
      }
      
      if(t == -1) return NULL;
      
      const string symbol = _commonFunctionTable.getSymbol();
      const ENUM_TIMEFRAMES period = _commonFunctionTable.getTimeframe();
      int h = iMA(symbol, period, (int)StringToInteger(parts[2]), 0, m, t);
      if(h == INVALID_HANDLE) return NULL;
      
      return new MAIndicatorFunc(name, h);
    }
    
    double execute(const double &params[]) override
    {
      const int bar = (int)params[0];
      double result[1] = {0};
      if(CopyBuffer(handle, 0, bar, 1, result) != 1)
      {
        Print("CopyBuffer error: ", GetLastError());
      }
      return result[0];
    }
};

static BaseFuncFactory MAIndicatorFunc::factory(MAIndicatorFunc::create);


class MultiMAIndicatorFunc: public BaseFunc
{
  private:
    static BaseFuncFactory factory;

  protected:
    int handle[];
    
  public:
    MultiMAIndicatorFunc(const string n, const int &h[]): BaseFunc(n, 2)
    {
      ArrayCopy(handle, h);
    }
    
    ~MultiMAIndicatorFunc()
    {
      for(int i = 0; i < ArraySize(handle); i++)
      {
        IndicatorRelease(handle[i]);
      }
    }
    
    static BaseFunc *create(const string name) // M_SMA_OPEN_5_2_11(period, bar), periods: 5,7,9,11
    {
      string parts[];
      if(StringSplit(name, '_', parts) != 6) return NULL;
      
      if(parts[0] != "M") return NULL;

      ENUM_MA_METHOD m = -1;
      ENUM_APPLIED_PRICE t = -1;
      
      static string methods[] = {"SMA", "EMA", "SMMA", "LWMA"};
      for(int i = 0; i < ArraySize(methods); i++)
      {
        if(parts[1] == methods[i])
        {
          m = (ENUM_MA_METHOD)i;
          break;
        }
      }

      if(m == -1) return NULL;

      static string types[] = {"NULL", "CLOSE", "OPEN", "HIGH", "LOW", "MEDIAN", "TYPICAL", "WEIGHTED"};
      for(int i = 1; i < ArraySize(types); i++)
      {
        if(parts[2] == types[i])
        {
          t = (ENUM_APPLIED_PRICE)i;
          break;
        }
      }
      
      if(t == -1) return NULL;
      
      const int start = (int)StringToInteger(parts[3]);
      const int step = (int)StringToInteger(parts[4]);
      const int stop = (int)StringToInteger(parts[5]);
      
      int h[];
      ArrayResize(h, stop + 1);
      ArrayInitialize(h, INVALID_HANDLE);
      
      const string symbol = _commonFunctionTable.getSymbol();
      const ENUM_TIMEFRAMES period = _commonFunctionTable.getTimeframe();
      for(int i = start; i <= stop; i += MathMax(step, 1))
      {
        h[i] = iMA(symbol, period, i, 0, m, t);
        if(h[i] == INVALID_HANDLE) return NULL;
      }
      
      return new MultiMAIndicatorFunc(name, h);
    }
    
    double execute(const double &params[]) override
    {
      const int h = (int)params[0];
      if(h < 0 || h >= ArraySize(handle))
      {
        Print("Wrong multiperiod: ", h);
        return 0;
      }
      if(handle[h] == INVALID_HANDLE)
      {
        Print("Invalid multihandle: ", h);
        return 0;
      }
      const int bar = (int)params[1];
      double result[1] = {0};
      if(CopyBuffer(handle[h], 0, bar, 1, result) != 1)
      {
        Print("CopyBuffer error: ", GetLastError());
      }
      return result[0];
    }
};

static BaseFuncFactory MultiMAIndicatorFunc::factory(MultiMAIndicatorFunc::create);
