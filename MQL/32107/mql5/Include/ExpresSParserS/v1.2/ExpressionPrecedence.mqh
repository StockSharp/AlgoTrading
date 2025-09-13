//+------------------------------------------------------------------+
//|                                         ExpressionPrecedence.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "ExpressionProcessor.mqh"
#include "ExpressionPromise.mqh"


class ExpressionPrecedence: public AbstractExpressionProcessor<Promise *>
{
  protected:
    static uchar prefixes[128];
    static uchar infixes[128];
    
    // ()              8
    // !-+             7
    // *, /, %,        6
    // +, -,           5
    // >, <, >=, <=,   4
    // ==, !=,         3
    // &&, ||          2
    // ?:              1
    // ,
    
    static ExpressionPrecedence epinit;
    
    static void initPrecedence()
    {
      // grouping
      prefixes['('] = 9;

      // unary
      prefixes['+'] = 9;
      prefixes['-'] = 9;
      prefixes['!'] = 9;
      
      // identifiers
      prefixes['_'] = 9;
      for(uchar c = 'a'; c <= 'z'; c++)
      {
        prefixes[c] = 9;
      }
      
      // numbers
      prefixes['.'] = 9;
      for(uchar c = '0'; c <= '9'; c++)
      {
        prefixes[c] = 9;
      }
      
      // operators
      // infixes['('] = 9; // parenthesis is not used here as 'function call' operator
      infixes['*'] = 8;
      infixes['/'] = 8;
      infixes['%'] = 8;
      infixes['+'] = 7;
      infixes['-'] = 7;
      infixes['>'] = 6;
      infixes['<'] = 6;
      infixes['='] = 5;
      infixes['!'] = 5;
      infixes['&'] = 4;
      infixes['|'] = 4;
      infixes['?'] = 3;
      infixes[':'] = 2;
      infixes[','] = 1; // arg list delimiter
    }

    ExpressionPrecedence(const bool init)
    {
      initPrecedence();
    }

    virtual Promise *evaluate(const string expression, const bool preprocess = false) override { return NULL; }
    
    ushort _lookAhead()
    {
      int i = 1;
      while(_index + i < _length && isspace(_expression[_index + i])) i++;
      if(_index + i < _length)
      {
        return _expression[_index + i];
      }
      return 0;
    }
    
    void _matchNext(ushort c, string message, string context = NULL)
    {
      if(_lookAhead() == c)
      {
        _nextToken();
      }
      else if(!_failed) // prevent chained errors
      {
        error(message, context);
      }
    }

  public: // should be protected:, but left public to prevent the error (cannot access protected member function) until MQL fix in a stable build
    ExpressionPrecedence(const string vars = NULL): AbstractExpressionProcessor(vars) {}
    ExpressionPrecedence(VariableTable &vt): AbstractExpressionProcessor(vt) {}
    ~ExpressionPrecedence()
    {
      CLEAR(root);
    }

};

static uchar ExpressionPrecedence::prefixes[128] = {0};
static uchar ExpressionPrecedence::infixes[128] = {0};
static ExpressionPrecedence ExpressionPrecedence::epinit(true);
