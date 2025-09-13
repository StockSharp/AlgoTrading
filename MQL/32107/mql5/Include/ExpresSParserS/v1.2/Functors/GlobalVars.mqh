//+------------------------------------------------------------------+
//|                                                   GlobalVars.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#ifndef EXTENDED_FUNCTORS
  #define EXTENDED_FUNCTORS
#endif

#include "../Functors.mqh"


class GlobalVariableFunc: public BaseFunc
{
  private:
    static BaseFuncFactory factory;

    static BaseFunc *legend(void)
    {
      const string msg = "Attached GlobalVars funcs:(look up the terminal variables)";
      Print(msg);
      return NULL;
    }

  public:
    GlobalVariableFunc(const string n): BaseFunc(n, 0) {}
    
    static BaseFunc *create(const string name)
    {
      if(name == NULL)
      {
        return legend();
      }

      return new GlobalVariableFunc(name);
    }
    
    double execute(const double &params[]) override
    {
      return GlobalVariableGet(name());
    }
};

static BaseFuncFactory GlobalVariableFunc::factory(GlobalVariableFunc::create);
