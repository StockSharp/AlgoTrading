//+------------------------------------------------------------------+
//|                                          ExpressionProcessor.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| The code is partially taken from                                 |
//| https://github.com/Ayan01/simple-expression-evaluator            |
//+------------------------------------------------------------------+

// Simple use-case:
//   ExpressionEvaluator ee;
//   double result = ee.evaluate("expression"); // get result
//   bool success = ee.success(); // check if expression has correct grammar

// To check for calculation errors analyze result for NaNs

// Customized use-case with variables:
//   ExpressionEvaluator ee("variable1=value1;variable2=value2;...");

// 25 built-in math functions

// Supported operators
// !-+
// ()
// *, /, %,
// +, -,
// >, <, >=, <=,
// ==, !=,
// &&, ||
// ?:

#ifdef INDICATOR_FUNCTORS
#include "Indicators.mqh"
#else
#include "Functors.mqh"
#endif


#define STACK_SIZE 100


interface IExpressionEnvironment
{
  VariableTable *variableTable();
  FunctionTable *functionTable();
  void error(string message, string context = NULL, const bool warning = false);
  double getPrecision(void) const;
};

class NullEnvironment: public IExpressionEnvironment
{
  private:
    const VariableTable _variableTable;
    const FunctionTable _functionTable;
  public:
    VariableTable *variableTable()
    {
      return (VariableTable *)&_variableTable;
    };
    FunctionTable *functionTable()
    {
      return (FunctionTable *)&_functionTable;
    };
    void error(string message, string context = NULL, const bool warning = false)
    {
      Print("NullEnvironment: ", message, " ", context);
    }
    double getPrecision(void) const
    {
      return 1.0e-8;
    }
};


template<typename T>
class ExpressionHelper
{
  protected:
    VariableTable *_variableTable;
    FunctionTable *_functionTable;
    IExpressionEnvironment *_owner;

  public:
    ExpressionHelper(IExpressionEnvironment *owner): _owner(owner), _variableTable(owner.variableTable()), _functionTable(owner.functionTable()) { }

    virtual T _variable(const string &name) = 0;
    virtual T _literal(const string &number) = 0;
    virtual T _negate(T result) = 0;
    virtual T _call(const int index, T &args[]) = 0;
    virtual T _ternary(T condition, T truly, T falsy) = 0;
    virtual T _isEqual(T result, T next, const bool equality) = 0;
};

template<typename T>
class AbstractExpressionProcessor: public IExpressionEnvironment
{
  protected:
    ExpressionHelper<T> *helper;

  protected:
    string _expression;
    ushort _token;
    uchar _tokenc; // used solely for debug purpose
    int _index;
    int _length;
    bool _failed;
    double _precision;

    VariableTable *_variableTable;
    FunctionTable *_functionTable;

    T root;

    bool _nextToken();
    void _match(ushort c, string message, string context = NULL);
    bool _readNumber(string &number);

    virtual void registerFunctions();
    virtual bool _preprocess();

  public:
    AbstractExpressionProcessor(const string vars = NULL);
    AbstractExpressionProcessor(VariableTable &vt);
    ~AbstractExpressionProcessor();

    virtual VariableTable *variableTable() override;
    virtual FunctionTable *functionTable() override;
    virtual void error(string message, string context = NULL, const bool warning = false) override;

    bool success();
    void setPrecision(const double p);
    virtual double getPrecision(void) const override { return _precision; }

    virtual T evaluate(const string expression, const bool preprocess = false);
    void detachResult() { root = NULL; }
    
    static bool isspace(ushort c);
    static bool isalpha(ushort c);
    static bool isalnum(ushort c);
    static bool isdigit(ushort c);
    
};

template<typename T>
AbstractExpressionProcessor::AbstractExpressionProcessor(const string vars = NULL): root(NULL)
{
  _variableTable = new VariableTable(vars);
  _index = -1;
  _failed = false;
  _precision = 1.0e-8;
  registerFunctions();
}

template<typename T>
AbstractExpressionProcessor::AbstractExpressionProcessor(VariableTable &vt): root(NULL)
{
  _variableTable = &vt;
  _index = -1;
  _failed = false;
  _precision = 1.0e-8;
  registerFunctions();
}

template<typename T>
AbstractExpressionProcessor::~AbstractExpressionProcessor()
{
  CLEAR(_variableTable);
  CLEAR(_functionTable);
  CLEAR(helper);
}

template<typename T>
VariableTable *AbstractExpressionProcessor::variableTable()
{
  return _variableTable;
}

template<typename T>
FunctionTable *AbstractExpressionProcessor::functionTable()
{
  return _functionTable;
}

template<typename T>
bool AbstractExpressionProcessor::success()
{
  return !_failed;
}

template<typename T>
void AbstractExpressionProcessor::setPrecision(const double p)
{
  _precision = p;
}

template<typename T>
void AbstractExpressionProcessor::registerFunctions()
{
  _functionTable = &_commonFunctionTable;
  AbstractFunc::fill(_functionTable);
}

template<typename T>
T AbstractExpressionProcessor::evaluate(const string expression, const bool preprocess = false)
{
  _expression = expression;
  if(preprocess) _preprocess();
  _length = StringLen(_expression);
  _index = -1;
  _failed = false;
  return NULL;
}

template<typename T>
void AbstractExpressionProcessor::error(string message, string stack = NULL, const bool warning = false)
{
  const string context = StringSubstr(_expression, 0, _index) + "^" + StringSubstr(_expression, _index);
  if(stack != NULL)
  {
    string array[];
    const int n = StringSplit(stack, ':', array);
    if(n == 3) stack = array[2];
  }
  else
  {
    stack = "";
  }
  
  Print(stack, (warning ? " warning: " : " error: "), message, " @ ", _index, ": ", context);
  _failed = !warning;
}

template<typename T>
static bool AbstractExpressionProcessor::isspace(ushort c)
{
  if(c == ' ' || c == '\t' || c == '\r' || c == '\n') return true;
  return false;
}

template<typename T>
static bool AbstractExpressionProcessor::isalpha(ushort c)
{
  if((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_') return true;
  return false;
}

template<typename T>
static bool AbstractExpressionProcessor::isdigit(ushort c)
{
  return c >= '0' && c <= '9';
}

template<typename T>
static bool AbstractExpressionProcessor::isalnum(ushort c)
{
  return isalpha(c) || isdigit(c);
}

template<typename T>
bool AbstractExpressionProcessor::_nextToken()
{
  _index++;
  while(_index < _length && isspace(_expression[_index])) _index++;
  if(_index < _length)
  {
    _token = _expression[_index];
    _tokenc = (uchar)_token;
    return true;
  }
  else
  {
    _token = 0;
    _tokenc = 0;
  }
  return false;
}

template<typename T>
void AbstractExpressionProcessor::_match(ushort c, string message, string context = NULL)
{
  if(_token == c)
  {
    _nextToken();
  }
  else if(!_failed) // prevent chained errors
  {
    error(message, context);
  }
}

template<typename T>
bool AbstractExpressionProcessor::_readNumber(string &number)
{
  bool point = false;
  while(isdigit(_token) || _token == '.') // NB: exponents are not supported!
  {
    if(_token == '.' && point)
    {
      error("Too many floating points", __FUNCTION__);
      return false;
    }
    number += ShortToString(_token);
    if(_token == '.') point = true;
    _nextToken();
  }
  return StringLen(number) > 0;
}

template<typename T>
bool AbstractExpressionProcessor::_preprocess()
{
  bool replaced = false;
  if(_variableTable == NULL) return false;
  int p = StringFind(_expression, "{");
  while(p >= 0)
  {
    int s = StringFind(_expression, "}", p);
    if(s == -1) break;
    string name = StringSubstr(_expression, p + 1, s - p - 1);
    int i = _variableTable.index(name);
    if(i != -1)
    {
      double value = _variableTable[i];
      replaced |= StringReplace(_expression, "{" + name + "}", ((long)value != value) ? (string)(float)value : (string)(long)value) > 0;
    }
    p = StringFind(_expression, "{", p);
  }
  return replaced;
}


template<typename T>
class ExpressionProcessor: public AbstractExpressionProcessor<T>
{
  public:
    ExpressionProcessor(const string vars = NULL): AbstractExpressionProcessor(vars) { }
    ExpressionProcessor(VariableTable &vt): AbstractExpressionProcessor(vt) { }
    T evaluate(const string expression, const bool preprocess = false) override;

  protected:
    virtual T _fmod(T v1, T v2) = 0;
  
    T _parse();
    T _if();
    T _logic();
    T _eq();
    T _compare();
    T _expr();
    T _term();
    T _factor();
    T _unary();
    T _identifier();
    T _number();
    T _function(const string &name);
};

template<typename T>
T ExpressionProcessor::evaluate(const string expression, const bool preprocess = false)
{
  AbstractExpressionProcessor<T>::evaluate(expression, preprocess);
  if(_length > 0)
  {
    _nextToken();
    return _parse();
  }
  return NULL;
}

template<typename T>
T ExpressionProcessor::_parse(void)
{
  T result = _if();
  if(_token != '\0')
  {
    error("Tokens after end of expression.", __FUNCTION__);
  }
  return result;
}

template<typename T>
T ExpressionProcessor::_if() // ternary if w?t:f
{
  T result = _logic();
  if(_token == '?')
  {
    _nextToken();
    T truly = _if();
    if(_token == ':')
    {
      _nextToken();
      T falsy = _if();
      return helper._ternary(result, truly, falsy);
    }
    else
    {
      error("Incomplete ternary if-condition", __FUNCTION__);
    }
  }
  return result;
}

template<typename T>
T ExpressionProcessor::_logic() // || / && - equal priorities! use parenthesis
{
  T result = _eq();
  while(_token == '&' || _token == '|')
  {
    ushort previous = _token;
    _nextToken();
    if(previous == '&' && _token == '&')
    {
      _nextToken();
      result = _eq() && result;
    }
    else
    if(previous == '|' && _token == '|')
    {
      _nextToken();
      result = _eq() || result;
    }
    else
    {
      error("Unexpected tokens " + ShortToString(previous) + " and " + ShortToString(_token), __FUNCTION__);
    }
  }
  
  return result;
}

template<typename T>
T ExpressionProcessor::_eq() // == !=
{
  T result = _compare();
  if(_token == '!' || _token == '=')
  {
    const bool equality = _token == '=';
    _nextToken();
    if(_token == '=')
    {
      _nextToken();
      return helper._isEqual(result, _compare(), equality);
    }
    else
    {
      error("Unexpected token " + ShortToString(_token), __FUNCTION__);
    }
  }
  
  return result;
}

template<typename T>
T ExpressionProcessor::_compare() // < > <= >=
{
  T result = _expr();
  if(_token == '<')
  {
    _nextToken();
    if(_token == '=')
    {
      _nextToken();
      return result <= _expr();
    }
    else
    {
      return result < _expr();
    }
  }
  else if(_token == '>')
  {
    _nextToken();
    if(_token == '=')
    {
      _nextToken();
      return result >= _expr();
    }
    else
    {
      return result > _expr();
    }
  }
  return result;
}

template<typename T>
T ExpressionProcessor::_expr()
{
  /* expr -> term  { '+' term | '-' term } */

  T result = _term();
  while((_token == '+') || (_token == '-'))
  {
    if(_token == '+')
    {
      _nextToken();
      result = result + _term();
    }
    else
    {
      _nextToken();
      result = result - _term();
    }
  }
  return result;
}

template<typename T>
T ExpressionProcessor::_term()
{
  /* term -> unary { '*' unary | '/' unary | '%' unary }*/

  T result = _unary();
  while((_token == '*') || (_token == '/') || (_token == '%'))
  {
    if(_token == '*')
    {
      _nextToken();
      result = result * _unary();
    }
    else if(_token == '%')
    {
      _nextToken();
      T n = _unary();
      result = _fmod(result, n);
    }
    else
    {
      _nextToken();
      T n = _unary();
      if(n != NULL)
      {
        result = result / n;
      }
      else
      {
        error("Error : Division by 0!", __FUNCTION__);
        result = __getINF(root);
      }
    }
  }
  return result;
}

template<typename T>
T ExpressionProcessor::_unary()
{
  /* unary -> {'!' | '-' | '+'} unary | factor */
  
  if(_token == '!')
  {
    _nextToken();
    return helper._negate(_unary());
  }
  else
  if(_token == '+' || _token == '-') // leading sign
  {
    const ushort _prev = _token;
    _nextToken();
    return _prev == '-' ? -_unary() : _unary();
  }
  
  return _factor();
}

template<typename T>
T ExpressionProcessor::_factor()
{
  /* factor -> '(' if ')' | number | variable */

  T result;
  
  if(_token == '(')
  {
    _nextToken();
    result = _if();
    _match(')', ") expected!", __FUNCTION__);
  }
  else if(isalpha(_token))
  {
    result = _identifier();
  }
  else
  {
    result = _number();
  }
  
  return result;
}

template<typename T>
T ExpressionProcessor::_identifier()
{
  /* identifier -> alphabet { alphabet | digit } {'(' -> function } */

  string variable;

  while(isalnum(_token))
  {
    variable += ShortToString(_token);
    _nextToken();
  }
  
  if(_token == '(')
  {
    _nextToken();
    return _function(variable);
  }
  
  return helper._variable(variable);
}

template<typename T>
T ExpressionProcessor::_function(const string &name)
{
  const int index = _functionTable.index(name);
  if(index == -1)
  {
    error("Function undefined: " + name, __FUNCTION__);
    return __getNAN(root);
  }
  
  const int arity = _functionTable[index].arity();
  if(arity > 0 && _token == ')')
  {
    error("Missing arguments for " + name + ", " + (string)arity + " required!", __FUNCTION__);
    return __getNAN(root);
  }
  
  T params[];
  ArrayResize(params, arity);
  for(int i = 0; i < arity; i++)
  {
    params[i] = _if();
    if(i < arity - 1)
    {
      _match(',', ", expected (param-list)!", __FUNCTION__);
    }
  }

  _match(')', ") expected after " + (string)arity + " arguments!", __FUNCTION__);
  
  return helper._call(index, params);
}

template<typename T>
T ExpressionProcessor::_number()
{
  /* number -> digit { digit } */

  string number;
  
  if(!_readNumber(number))
  {
    error("Number expected", __FUNCTION__);
  }
  return helper._literal(number);
}
