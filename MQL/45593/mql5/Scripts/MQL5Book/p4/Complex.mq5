//+------------------------------------------------------------------+
//|                                                      Complex.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property script_show_inputs

#include <MQL5Book/PRTF.mqh>

input double r = 1;
input double i = 2;

complex c = {r, i};

complex mirror(const complex z)
{
   complex result = {z.imag, z.real}; // swap real and imaginary parts
   return result;
}

complex square(const complex z) 
{ 
   return (z * z);
}   

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   PRTF(c);
   PRTF(square(c));
   PRTF(square(mirror(c)));
}
//+------------------------------------------------------------------+
/*
   c=(1,2) / ok
   square(c)=(-3,4) / ok
   square(mirror(c))=(3,4) / ok
*/
//+------------------------------------------------------------------+
