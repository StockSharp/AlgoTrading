//+------------------------------------------------------------------+
//|                                          ExpressionEvaluator.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "ExpressionProcessor.mqh"


class ExpressionHelperDouble: public ExpressionHelper<double>
{
  public:
    ExpressionHelperDouble(IExpressionEnvironment *owner): ExpressionHelper(owner) { }

    virtual double _variable(const string &name) override
    {
      if(!_variableTable.exists(name))
      {
        _owner.error("Variable undefined: " + name, __FUNCTION__);
        return nan;
      }
      return _variableTable.get(name);
    }
    virtual double _literal(const string &number) override
    {
      return StringToDouble(number);
    }
    virtual double _call(const int index, double &params[]) override
    {
      return _functionTable[index].execute(params);
    }
    virtual double _isEqual(double result, double next, const bool equality) override
    {
      const bool equal = fabs(result - next) <= _owner.getPrecision(); // _precision;
      return equality ? equal : !equal;
    }
    virtual double _negate(double result) override
    {
      return !result;
    }
    virtual double _ternary(double condition, double truly, double falsy) override
    {
      return condition ? truly : falsy;
    }
};

class ExpressionEvaluator: public ExpressionProcessor<double>
{
  public:
    ExpressionEvaluator(const string vars = NULL): ExpressionProcessor(vars) { helper = new ExpressionHelperDouble(&this); }
    ExpressionEvaluator(VariableTable &vt): ExpressionProcessor(vt) { helper = new ExpressionHelperDouble(&this); }
    
  protected:
    virtual double _fmod(double v1, double v2) override
    {
      return fmod(v1, v2);
    }
};
