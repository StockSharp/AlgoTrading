//+------------------------------------------------------------------+
//|                                                  SymbolProps.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#ifndef EXTENDED_FUNCTORS
  #define EXTENDED_FUNCTORS
#endif

#include "../Functors.mqh"


enum SYMBOL_FUNC_TYPE
{
  ASK,
  BID,
  LAST,
  LASTVOLUME,
  POINT,
};

class SymbolContextFunc: public BaseFunc
{
  private:
    static BaseFuncFactory factory;
    
  protected:
    const SYMBOL_FUNC_TYPE type;

    static BaseFunc *legend(void)
    {
      string msg = "Attached SymbolProps funcs:";
      for(int i = SYMBOL_FUNC_TYPE::ASK; i <= SYMBOL_FUNC_TYPE::LASTVOLUME; i++)
      {
        msg += EnumToString((SYMBOL_FUNC_TYPE)i) + ";";
      }
      Print(msg);
      return NULL;
    }

  public:
    SymbolContextFunc(const string n, const int t): BaseFunc(n, 0), type((SYMBOL_FUNC_TYPE)t) {}

    static BaseFunc *create(const string name)
    {
      if(name == NULL)
      {
        return legend();
      }

      for(int i = SYMBOL_FUNC_TYPE::ASK; i <= SYMBOL_FUNC_TYPE::LASTVOLUME; i++)
      {
        if(name == EnumToString((SYMBOL_FUNC_TYPE)i))
        {
          return new SymbolContextFunc(name, i);
        }
      }
      return NULL;
    }

    double execute(const double &params[]) override
    {
      const string symbol = _commonFunctionTable.getSymbol();
      switch(type)
      {
        case ASK: return SymbolInfoDouble(symbol, SYMBOL_ASK);
        case BID: return SymbolInfoDouble(symbol, SYMBOL_BID);
        case LAST: return SymbolInfoDouble(symbol, SYMBOL_LAST);
        case LASTVOLUME: return (double)SymbolInfoInteger(symbol, SYMBOL_VOLUME);
        case POINT: return SymbolInfoDouble(symbol, SYMBOL_POINT);
      }
      return nan;
    }
};

static BaseFuncFactory SymbolContextFunc::factory(SymbolContextFunc::create);
