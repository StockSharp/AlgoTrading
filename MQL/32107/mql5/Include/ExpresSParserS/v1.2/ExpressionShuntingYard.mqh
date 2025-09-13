//+------------------------------------------------------------------+
//|                                       ExpressionShuntingYard.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include "ExpressionPrecedence.mqh"


class ExpressionShuntingYard: public ExpressionPrecedence
{
  public:
    ExpressionShuntingYard(const string vars = NULL): ExpressionPrecedence(vars) { }
    ExpressionShuntingYard(VariableTable &vt): ExpressionPrecedence(vt) { }

    bool convertToByteCode(const string expression, ByteCode &codes[])
    {
      Promise::environment(&this);
      AbstractExpressionProcessor<Promise *>::evaluate(expression);
      if(_length > 0)
      {
        exportToByteCode(codes);
      }
      return !_failed;
    }

  protected:
    template<typename T>
    static void _push(T &stack[], T &value)
    {
      const int n = ArraySize(stack);
      ArrayResize(stack, n + 1, STACK_SIZE);
      stack[n] = value;
    }

    void exportToByteCode(ByteCode &output[])
    {
      ByteCode stack[];
      int ssize = 0;
      string number;
      uchar c;
      
      ArrayResize(stack, STACK_SIZE);
      
      const int previous = ArraySize(output);
      
      while(_nextToken() && !_failed)
      {
        if(_token == '+' || _token == '-' || _token == '!')
        {
          if(_token == '-')
          {
            _push(output, ByteCode(-1.0));
            push(stack, ByteCode('*'), ssize);
          }
          else if(_token == '!')
          {
            push(stack, ByteCode('!'), ssize);
          }
          continue;
        }
        
        number = "";
        if(_readNumber(number)) // if a number was read, _token has changed
        {
          _push(output, ByteCode(StringToDouble(number)));
        }
        
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
            push(stack, ByteCode('f', _functionTable.index(variable)), ssize);
          }
          else // variable name
          {
            int index = -1;
            if(CheckPointer(_variableTable) != POINTER_INVALID)
            {
              index = _variableTable.index(variable);
              if(index == -1)
              {
                if(_variableTable.adhocAllocation())
                {
                  index = _variableTable.add(variable, nan);
                  _push(output, ByteCode('v', index));
                  error("Unknown variable is NaN: " + variable, __FUNCTION__, true);
                }
                else
                {
                  error("Unknown variable : " + variable, __FUNCTION__);
                }
              }
              else
              {
                _push(output, ByteCode('v', index));
              }
            }
          }
        }
        
        if(infixes[_token] > 0) // operator, including least significant '?'
        {
          while(ssize > 0 && isTop2Pop(top(stack, ssize).code))
          {
            _push(output, pop(stack, ssize));
          }
          
          if(_token == '?' || _token == ':')
          {
            if(_token == '?')
            {
              const int start = ArraySize(output);
              _push(output, ByteCode((uchar)_token));
              exportToByteCode(output); // subexpression truly, _token has changed
              if(_token != ':')
              {
                error("Colon expected, given: " + ShortToString(_token), __FUNCTION__);
                break;
              }
              output[start].index = ArraySize(output);
              exportToByteCode(output); // subexpression falsy, _token has changed
              output[start].value = ArraySize(output);
              if(_token == ':')
              {
                break;
              }
            }
            else
            {
              break;
            }
          }
          else
          {
            if(_token == '>' || _token == '<')
            {
              if(_lookAhead() == '=')
              {
                push(stack, ByteCode((uchar)(_token == '<' ? '{' : '}')), ssize);
                _nextToken();
              }
              else
              {
                push(stack, ByteCode((uchar)_token), ssize);
              }
            }
            else if(_token == '=' || _token == '!')
            {
              if(_lookAhead() == '=')
              {
                push(stack, ByteCode((uchar)(_token == '!' ? '`' : '=')), ssize);
                _nextToken();
              }
            }
            else if(_token == '&' || _token == '|')
            {
              _matchNext(_token, ShortToString(_token) + " expected after " + ShortToString(_token), __FUNCTION__);
              push(stack, ByteCode((uchar)_token), ssize);
            }
            else if(_token != ',')
            {
              push(stack, ByteCode((uchar)_token), ssize);
            }
          }
        }
        
        if(_token == '(')
        {
          push(stack, ByteCode('('), ssize);
        }
        else if(_token == ')')
        {
          while(ssize > 0 && (c = top(stack, ssize).code) != '(')
          {
            _push(output, pop(stack, ssize));
          }
          if(c == '(') // must be true unless it's a subexpression (then 'c' can be 0)
          {
            ByteCode disable_warning = pop(stack, ssize);
          }
          else
          {
            if(previous == 0)
            {
              error("Closing parenthesis is missing", __FUNCTION__);
            }
            return;
          }
        }
      }
      
      while(ssize > 0)
      {
        _push(output, pop(stack, ssize));
      }
    }
    
    bool isTop2Pop(const uchar c)
    {
      return (c == 'f' || infixes[c] >= infixes[_token]) && c != '(' && c != ':';
    }
};
