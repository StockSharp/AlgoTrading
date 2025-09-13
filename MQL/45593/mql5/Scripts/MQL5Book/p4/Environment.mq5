//+------------------------------------------------------------------+
//|                                                  Environment.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MqlError.mqh>
#include <MQL5Book/EnumToArray.mqh>

//+------------------------------------------------------------------+
//| This block of wrappers is required to get function pointers      |
//| (which can not point to built-in functions, see NB comment below)|
//+------------------------------------------------------------------+

int _MQLInfoInteger(const ENUM_MQL_INFO_INTEGER p)
{
   return MQLInfoInteger(p);
}

int _TerminalInfoInteger(const ENUM_TERMINAL_INFO_INTEGER p)
{
   return TerminalInfoInteger(p);
}

double _TerminalInfoDouble(const ENUM_TERMINAL_INFO_DOUBLE p)
{
   return TerminalInfoDouble(p);
}

string _MQLInfoString(const ENUM_MQL_INFO_STRING p)
{
   return MQLInfoString(p);
}

string _TerminalInfoString(const ENUM_TERMINAL_INFO_STRING p)
{
   return TerminalInfoString(p);
}

/*
   NB: Function Pointer feature is limited in MQL5
   Built-in functions can not be used with pointers
   Only functions implemented in MQL5 are supported for pointers

   typedef int (*IntFuncPtr)(const ENUM_MQL_INFO_INTEGER property);
   IntFuncPtr ptr1 = _MQLInfoInteger;  // ok
   IntFuncPtr ptr2 = MQLInfoInteger;   // compile error
*/

//+------------------------------------------------------------------+
//| Generate combined type for property getter function based on     |
//| enum E and result values of type R                               |
//+------------------------------------------------------------------+
template<typename E,typename R>
struct Binding
{
public:
   typedef R (*FuncPtr)(const E property);
   const FuncPtr f;
   Binding(FuncPtr p): f(p) { }
};

//+------------------------------------------------------------------+
//| Helper function to list all properties of type R in enum E       |
//+------------------------------------------------------------------+
template<typename E,typename R>
void process(Binding<E,R> &b)
{
   E e = (E)0; // disable warning possible use of uninitialized variable
   int array[];
   int n = EnumToArray(e, array, 0, USHORT_MAX);
   Print(typename(E), " Count=", n);
   ResetLastError();
   for(int i = 0; i < n; ++i)
   {
      e = (E)array[i];
      R r = b.f(e); // make sure the function is called before _LastError access
      const int snapshot = _LastError; // keep the code before next calculations
      PrintFormat("% 3d %s=%s", i, EnumToString(e), (string)r +
         (snapshot != 0 ? " (" + E2S(snapshot) + "," + (string)snapshot + ")" : ""));
      ResetLastError();
   }
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   process(Binding<ENUM_MQL_INFO_INTEGER,int>(_MQLInfoInteger));
   process(Binding<ENUM_TERMINAL_INFO_INTEGER,int>(_TerminalInfoInteger));
   
   process(Binding<ENUM_TERMINAL_INFO_DOUBLE,double>(_TerminalInfoDouble));

   process(Binding<ENUM_MQL_INFO_STRING,string>(_MQLInfoString));
   process(Binding<ENUM_TERMINAL_INFO_STRING,string>(_TerminalInfoString));
}
//+------------------------------------------------------------------+
