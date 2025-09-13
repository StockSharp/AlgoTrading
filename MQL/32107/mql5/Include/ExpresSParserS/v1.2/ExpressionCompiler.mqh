//+------------------------------------------------------------------+
//|                                           ExpressionCompiler.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "ExpressionProcessor.mqh"
#include "ExpressionPromise.mqh"

class ExpressionHelperPromise: public ExpressionHelper<Promise *>
{
  public:
    ExpressionHelperPromise(IExpressionEnvironment *owner): ExpressionHelper(owner) { }

    virtual Promise *_negate(Promise *result) override
    {
      return new Promise(_owner, '!', result);
    }
    virtual Promise *_call(const int index, Promise *&params[]) override
    {
      return new Promise(_owner, index, params);
    }
    virtual Promise *_ternary(Promise *condition, Promise *truly, Promise *falsy) override
    {
      return new Promise(_owner, '?', condition, truly, falsy);
    }
    virtual Promise *_variable(const string &name) override
    {
      return Promise::lookUpVariable(name, _owner);
    }
    virtual Promise *_literal(const string &number) override
    {
      return new Promise(_owner, StringToDouble(number));
    }
    virtual Promise *_isEqual(Promise *result, Promise *next, const bool equality) override
    {
      return new Promise(_owner, (uchar)(equality ? '=' : '`'), result, next);
    }
};

class ExpressionCompiler: public ExpressionProcessor<Promise *>
{
  public:
    ExpressionCompiler(const string vars = NULL): ExpressionProcessor(vars) { helper = new ExpressionHelperPromise(&this); }
    ExpressionCompiler(VariableTable &vt): ExpressionProcessor(vt) { helper = new ExpressionHelperPromise(&this); }
    ~ExpressionCompiler()
    {
      CLEAR(root);
    }
    
    virtual Promise *evaluate(const string expression, const bool preprocess = false) override
    {
      CLEAR(root);
      Promise::environment(&this);
      root = ExpressionProcessor<Promise *>::evaluate(expression, preprocess);
      return root;
    }

  protected:
    virtual Promise *_fmod(Promise *v1, Promise *v2) override
    {
      return new Promise(this, '%', v1, v2);
    }
};
