//+------------------------------------------------------------------+
//|                                                     Functors.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "VariableTable.mqh"

class AbstractFuncStorage
{
  protected:
    IFunctor *funcs[];
    int total;
    
  public:
    ~AbstractFuncStorage()
    {
      for(int i = 0; i < total; i++)
      {
        CLEAR(funcs[i]);
      }
    }
    void add(IFunctor *f)
    {
      ArrayResize(funcs, total + 1);
      funcs[total++] = f;
    }
    void fill(FunctionTable &table)
    {
      table.add(funcs);
    }
};

class AbstractFunc: public IFunctor
{
  private:
    const string _name;
    const int _arity;
    static AbstractFuncStorage storage;

  public:
    AbstractFunc(const string n, const int a): _name(n), _arity(a)
    {
      storage.add(&this);
    }
    string name(void) const override
    {
      return _name;
    }
    int arity(void) const override
    {
      return _arity;
    }
    static void fill(FunctionTable &table)
    {
      if(table.size() == 0)
      {
        storage.fill(table);
      }
    }
};

static AbstractFuncStorage AbstractFunc::storage;

template<typename T>
class FuncN: public AbstractFunc
{
  public:
    FuncN(const string n): AbstractFunc(n, sizeof(T) % 4) {}
};

#define _ARITY(N)   struct arity##N { char x[N]; };
struct arity0 { char x[4]; };

_ARITY(1);
_ARITY(2);
_ARITY(3);

#define PARAMS0 
#define PARAMS1 params[0]
#define PARAMS2 params[0],params[1]
#define PARAMS3 params[0],params[1],params[2]


#define FUNCTOR(CLAZZ,NAME,ARITY) \
class Func_##CLAZZ: public FuncN<arity##ARITY> \
{ \
  public: \
    Func_##CLAZZ(): FuncN(NAME) {} \
    double execute(const double &params[]) override \
    { \
      return (double)CLAZZ(PARAMS##ARITY); \
    } \
}; \
Func_##CLAZZ __##CLAZZ;

FUNCTOR(fabs, "abs", 1);
FUNCTOR(acos, "acos", 1);
FUNCTOR(acosh, "acosh", 1);
FUNCTOR(asin, "asin", 1);
FUNCTOR(asinh, "asinh", 1);
FUNCTOR(atan, "atan", 1);
FUNCTOR(atanh, "atanh", 1);
FUNCTOR(ceil, "ceil", 1);
FUNCTOR(cos, "cos", 1);
FUNCTOR(cosh, "cosh", 1);
FUNCTOR(exp, "exp", 1);
FUNCTOR(floor, "floor", 1);
FUNCTOR(log, "log", 1);
FUNCTOR(log10, "log10", 1);
FUNCTOR(fmax, "max", 2);
FUNCTOR(fmin, "min", 2);
FUNCTOR(fmod, "mod", 2);
FUNCTOR(pow, "pow", 2);
FUNCTOR(rand, "rand", 0);
FUNCTOR(round, "round", 1);
FUNCTOR(sin, "sin", 1);
FUNCTOR(sinh, "sinh", 1);
FUNCTOR(sqrt, "sqrt", 1);
FUNCTOR(tan, "tan", 1);
FUNCTOR(tanh, "tanh", 1);

FUNCTOR(TimeCurrent, "now", 0);

class Func_CopyBuffer: public FuncN<arity2>
{
    const int handle;
    
  public:
    Func_CopyBuffer(const string name, const int h): FuncN(name), handle(h) {}
    double execute(const double &params[]) override
    {
      const int bar = (int)params[0];
      const int buf = (int)params[1];
      double result[1];
      if(CopyBuffer(handle, buf, bar, 1, result) == 1)
      {
        return result[0];
      }
      return nan;
    }
};

class BaseFunc: public AbstractFunc
{
  public:
    BaseFunc(const string name, const int arity = 1): AbstractFunc(name, arity)
    {
      // for indicators the following convention is used
      // if arity = 1, the single argument is the bar number,
      // when 2 arguments, they are bar number and buffer index
      
      // NB. indicator parameters, such as period, price type etc. should be specified in its name,
      // because MT creates indicator handle per parameter set,
      // so the identifier is the name plus the parameter set;
      // here indicators are created during parsing, then called during calculation
    }
    static BaseFunc *create(const string name)
    {
      return NULL;
    }
};
