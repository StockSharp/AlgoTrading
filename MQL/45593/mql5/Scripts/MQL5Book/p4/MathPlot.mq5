//+------------------------------------------------------------------+
//|                                                     MathPlot.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <Graphics/Graphic.mqh>

//+------------------------------------------------------------------+
//| Unique IDs for supported math functions with single argument     |
//+------------------------------------------------------------------+
enum MATH_FUNC
{
   None, // None/Clean-Up
   Abs,
   Arccos,
   Arcsin,
   Arctan,
   Cos,
   Exp,
   Expm1,
   Log,
   Log10,
   Log1p,
   Sin,
   Sqrt,
   Tang,
   Arccosh,
   Arcsinh,
   Arctanh,
   Cosh,
   Sinh,
   Tanh,
};

//+------------------------------------------------------------------+
//| Inputs                                                           |
//+------------------------------------------------------------------+
input MATH_FUNC Function = Sin;
input double Xfrom = -3;  // X from
input double Xto = +3;    // X to
input double Xstep = 0.1; // X step
input ENUM_CURVE_TYPE CurveType = CURVE_LINES;

//+------------------------------------------------------------------+
//| Binding between function ID and pointer to calculation function  |
//+------------------------------------------------------------------+
struct MathDrawable
{
   MATH_FUNC type;
   CurveFunction pointer;
};

//+------------------------------------------------------------------+
//| Required because built-in functions doesn't allow pointers       |
//+------------------------------------------------------------------+
#define FUNC(F) \
double Wrapper##F(double x) \
{ \
   return F(x); \
}

//+------------------------------------------------------------------+
//| All wrappers for math functions                                  |
//+------------------------------------------------------------------+
FUNC(MathAbs);
FUNC(MathArccos);
FUNC(MathArcsin);
FUNC(MathArctan);
FUNC(MathCos);
FUNC(MathExp);
FUNC(MathExpm1);
FUNC(MathLog);
FUNC(MathLog10);
FUNC(MathLog1p);
FUNC(MathSin);
FUNC(MathSqrt);
FUNC(MathTan);
FUNC(MathArccosh);
FUNC(MathArcsinh);
FUNC(MathArctanh);
FUNC(MathCosh);
FUNC(MathSinh);
FUNC(MathTanh);

//+------------------------------------------------------------------+
//| Helper generator of MathDrawable items for setup array below     |
//+------------------------------------------------------------------+
#define FUNC_REG(N) {N, WrapperMath##N}

//+------------------------------------------------------------------+
//| Array of math functions with single argument, [i] = (ID, pointer)|
//+------------------------------------------------------------------+
MathDrawable setup[] =
{
   {0, NULL}, // None (choose to remove object with drawing)
   FUNC_REG(Abs),
   FUNC_REG(Arccos),
   FUNC_REG(Arcsin),
   FUNC_REG(Arctan),
   FUNC_REG(Cos),
   FUNC_REG(Exp),
   FUNC_REG(Expm1),
   FUNC_REG(Log),
   FUNC_REG(Log10),
   FUNC_REG(Log1p),
   FUNC_REG(Sin),
   FUNC_REG(Sqrt),
   {Tang, WrapperMathTan}, // Tan identifier is occupied somewhere
   FUNC_REG(Arccosh),
   FUNC_REG(Arcsinh),
   FUNC_REG(Arctanh),
   FUNC_REG(Cosh),
   FUNC_REG(Sinh),
   FUNC_REG(Tanh),
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ObjectDelete(0, "Graphic"); // cleans up the chart from previous drawing
   if(Function != None)
   {
      // create chart object with bitmap and draw selected function on it
      GraphPlot(setup[Function].pointer, Xfrom, Xto, Xstep, CurveType);
   }
}
//+------------------------------------------------------------------+
