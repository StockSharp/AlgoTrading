//+------------------------------------------------------------------+
//|                                              ExpressionPratt.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "ExpressionPrecedence.mqh"


class ExpressionPratt: public ExpressionPrecedence
{
  public:
    ExpressionPratt(const string vars = NULL): ExpressionPrecedence(vars) { helper = new ExpressionHelperPromise(&this); }
    ExpressionPratt(VariableTable &vt): ExpressionPrecedence(vt) { helper = new ExpressionHelperPromise(&this); }

    virtual Promise *evaluate(const string expression, const bool preprocess = false) override
    {
      CLEAR(root);
      root = NULL;
      Promise::environment(&this);
      AbstractExpressionProcessor<Promise *>::evaluate(expression, preprocess);
      if(_length > 0)
      {
        root = parseExpression();
      }
      return root;
    }
  
  protected:  
    Promise *_parsePrefix()
    {
      Promise *result = NULL;
      switch(_token)
      {
        case '(':
          result = parseExpression();
          _match(')', ") expected!", __FUNCTION__);
          break;
        case '!':
          result = helper._negate(parseExpression(prefixes[_token]));
          break;
        case '+':
          result = parseExpression(prefixes[_token]);
          break;
        case '-':
          result = -parseExpression(prefixes[_token]);
          break;
        default:
          if(isalpha(_token))
          {
            string variable;
          
            while(isalnum(_token))
            {
              variable += ShortToString(_token);
              _nextToken();
            }
            
            if(_token == '(')
            {
              const string name = variable;
              const int index = _functionTable.index(name);
              if(index == -1)
              {
                error("Function undefined: " + name, __FUNCTION__);
                return __getNAN(root);
              }
              
              const int arity = _functionTable[index].arity();
              if(arity > 0 && _lookAhead() == ')')
              {
                error("Missing arguments for " + name + ", " + (string)arity + " required!", __FUNCTION__);
                return __getNAN(root);
              }
              
              Promise *params[];
              ArrayResize(params, arity);
              for(int i = 0; i < arity; i++)
              {
                params[i] = parseExpression(infixes[',']);
                if(i < arity - 1)
                {
                  if(_token != ',')
                  {
                    _match(',', ", expected (param-list)!", __FUNCTION__);
                    break;
                  }
                }
              }
            
              _match(')', ") expected after " + (string)arity + " arguments!", __FUNCTION__);
              
              result = helper._call(index, params);
            }
            else
            {
              return helper._variable(variable); // get index and if not found - optionally reserve the name with nan
            }
          }
          else
          {
            string number;
            if(_readNumber(number))
            {
              return helper._literal(number);
            }
          }
      }
      return result;
    }
    
    Promise *_parseInfix(Promise *left, const int precedence = 0)
    {
      Promise *result = NULL;
      const ushort _previous = _token;
      switch(_previous)
      {
        case '*':
        case '/':
        case '%':
        case '+':
        case '-':
          result = new Promise(this, (uchar)_previous, left, parseExpression(precedence));
          break;
        case '>':
        case '<':
          if(_lookAhead() == '=')
          {
            _nextToken();
            result = new Promise(this, (uchar)(_previous == '<' ? '{' : '}'), left, parseExpression(precedence));
          }
          else
          {
            result = new Promise(this, (uchar)_previous, left, parseExpression(precedence));
          }
          break;
        case '=':
        case '!':
          _matchNext('=', "= expected after " + ShortToString(_previous), __FUNCTION__);
          result = helper._isEqual(left, parseExpression(precedence), _previous == '=');
          break;
        case '&':
        case '|':
          _matchNext(_previous, ShortToString(_previous) + " expected after " + ShortToString(_previous), __FUNCTION__);
          result = new Promise(this, (uchar)_previous, left, parseExpression(precedence));
          break;
        case '?':
          {
            Promise *truly = parseExpression(infixes[':']);
            if(_token != ':')
            {
              _match(':', ": expected", __FUNCTION__);
            }
            else
            {
              Promise *falsy = parseExpression(infixes[':']);
              if(truly != NULL && falsy != NULL)
              {
                result = helper._ternary(left, truly, falsy);
              }
            }
          }
        case ':':
        case ',': // just skip
          break;
        default:
          error("Can't process infix token " + ShortToString(_previous));
        
      }
      return result;
    }

    virtual Promise *parseExpression(const int precedence = 0)
    {
      if(_failed) return NULL; // cut off subexpressions in case of errors
    
      _nextToken();
      if(prefixes[(uchar)_token] == 0)
      {
        this.error("Can't parse " + ShortToString(_token), __FUNCTION__);
        return NULL;
      }
      
      Promise *left = _parsePrefix();
      
      while((precedence < infixes[_token]) && !_failed)
      {
        left = _parseInfix(left, infixes[(uchar)_token]);
      }
      
      return left;
    }
};
