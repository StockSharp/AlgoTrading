//+------------------------------------------------------------------+
//|                                                  FuncTypedef.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"

typedef double (*Calc)(double, double);

Calc calc;

//+------------------------------------------------------------------+
//| Summation                                                        |
//+------------------------------------------------------------------+
double plus(double v1, double v2)
{
   return v1 + v2;
}

//+------------------------------------------------------------------+
//| Subtraction                                                      |
//+------------------------------------------------------------------+
double minus(double v1, double v2)
{
   return v1 - v2;
}

//+------------------------------------------------------------------+
//| Multiplication                                                   |
//+------------------------------------------------------------------+
double mul(double v1, double v2)
{
   return v1 * v2;
}

//+------------------------------------------------------------------+
//| Calculation by arbitrary function pointer                        |
//+------------------------------------------------------------------+
double calculator(Calc ptr, double v1, double v2)
{
   if(ptr == NULL) return 0;
   return ptr(v1, v2);
}

//+------------------------------------------------------------------+
//| Function pointer selector                                        |
//+------------------------------------------------------------------+
Calc generator(ushort type)
{
   Calc local;
   switch(type)
   {
      case '+':
         return plus;
      case '-':
         return minus;
      case '*':
         return mul;
      case '!':
         return local; // warning: possible use of uninitialized variable
   }
   return NULL;
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print(calculator(plus, 1, 2));           //  3
   Print(calculator(minus, 1, 2));          // -1
   Print(calculator(calc, 1, 2));           //  0
   Print(calculator(generator('*'), 1, 2)); //  2
   Print(calculator(generator('!'), 1, 2)); //  0?
}
//+------------------------------------------------------------------+
