//+------------------------------------------------------------------+
//|                                                VariableTable.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "HashMapSimple.mqh"
#include "NaNs.mqh"

#define Map HashMapSimple
#define CLEAR(P) if(CheckPointer(P) == POINTER_DYNAMIC) delete P;

template<typename T>
class Table
{
  public:
    virtual T operator[](const int index) const
    {
      return _table[index];
    }
    virtual int index(const string variableName)
    {
      return _table.getIndex(variableName);
    }
    virtual bool exists(const string variableName) const
    {
      return _table.getIndex(variableName) != -1;
    }
    virtual T get(const string variableName) const
    {
      return _table[variableName];
    }
    virtual int add(const string variableName, T value) // blind add
    {
      return _table.add(variableName, value);
    }
    virtual int set(const string variableName, T value) // add or update
    {
      return _table.set(variableName, value);
    }
    virtual void update(const int index, T value)
    {
      _table.replace(index, value);
    }
    virtual int size() const
    {
      return _table.getSize();
    }
    
  protected:
    Map<string, T> _table;
};

class VariableTable: public Table<double>
{
  protected:
    bool implicitAllocation;
    
  public:
    VariableTable(const string pairs = NULL): implicitAllocation(false)
    {
      if(pairs != NULL) assign(pairs, true);
    }
    
    void assign(const string pairs, bool init = false)
    {
      if(init) _table.reset();
      else if(_table.getSize() == 0) init = true;
      
      string vararray[];
      const int n = StringSplit(pairs, ';', vararray);
      for(int i = 0; i < n; i++)
      {
        string pair[];
        if(StringSplit(vararray[i], '=', pair) == 2)
        {
          if(init)
          {
            _table.add(pair[0], StringToDouble(pair[1]));
          }
          else
          {
            _table.set(pair[0], StringToDouble(pair[1]));
          }
        }
        else if(StringLen(vararray[i]) > 0)
        {
          if(init)
          {
            _table.add(vararray[i], nan);
          }
          else
          {
            _table.set(vararray[i], nan);
          }
        }
      }
    }
    void adhocAllocation(const bool b) // set this option to accept and reserve all variable names
    {
      implicitAllocation = b;
    }
    bool adhocAllocation(void)
    {
      return implicitAllocation;
    }
};

VariableTable _commonVariableTable;

interface IFunctor
{
  string name(void) const;
  int arity(void) const;
  double execute(const double &params[]);
};

class BaseFunc;

typedef BaseFunc *(*CREATOR)(const string name);

class BaseFuncFactory
{
  private:
    static CREATOR classes[];
  
  public:
    BaseFuncFactory(CREATOR ptr)
    {
      int count = ArraySize(classes);
      ArrayResize(classes, count + 1);
      classes[count] = ptr;
      ptr(NULL); // print legend to log
    }
    
    static BaseFunc *dispatch(const string n)
    {
      BaseFunc *result = NULL;
      const int count = ArraySize(classes);
      for(int i = 0; i < count && result == NULL; i++)
      {
        CREATOR ptr = classes[i];
        if(ptr != NULL)
        {
          result = ptr(n);
        }
      }
      return result;
    }
};

static CREATOR BaseFuncFactory::classes[];

/*
  BaseFuncFactory provides support for different functor types, based on attached/included classes,
  dispatching calls by names. It replaces more straightforward approach with hardcoded classes,
  equivalent code with pre-selected choice by the lib could be:
  
static BaseFunc *BaseFunc::create(const string name)
{
  BaseFunc *result = NULL;
  
  const bool success = ((result = SeriesFunc::create(name)) != NULL)
                    || ((result = SymbolContextFunc::create(name)) != NULL)
                    || ((result = MAIndicatorFunc::create(name)) != NULL)
                    || ((result = MultiMAIndicatorFunc::create(name)) != NULL)
                    || ((result = GlobalVariableFunc::create(name)) != NULL);
                    
  return result;
}

  But this would be not adjustable by client code.
  
*/


class FunctionTable: public Table<IFunctor *>
{
    string symbol;
    ENUM_TIMEFRAMES tf;

  public:
    FunctionTable(): symbol(NULL), tf(0) {}
    void add(IFunctor *f)
    {
      Table<IFunctor *>::add(f.name(), f);
    }
    void add(IFunctor *&f[])
    {
      for(int i = 0; i < ArraySize(f); i++)
      {
        add(f[i]);
      }
      // Print("Built-in functions: ", _table.getSize());
    }
    
    void setSymbol(const string s)
    {
      symbol = s;
    }

    void setTimeframe(const ENUM_TIMEFRAMES t)
    {
      tf = t;
    }
    
    string getSymbol() const
    {
      return symbol;
    }

    ENUM_TIMEFRAMES getTimeframe() const
    {
      return tf;
    }
    
    #ifdef EXTENDED_FUNCTORS
    virtual int index(const string name) override
    {
      int i = Table<IFunctor *>::index(name);
      if(i == -1)
      {
        i = _table.getSize();
        IFunctor *f = BaseFuncFactory::dispatch(name);
        if(f)
        {
          Table<IFunctor *>::add(name, f);
          return i;
        }
        return -1;
      }
      return i;
    }
    #endif
};

FunctionTable _commonFunctionTable;
