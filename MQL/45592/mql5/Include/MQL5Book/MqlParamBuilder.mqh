//+------------------------------------------------------------------+
//|                                              MqlParamBuilder.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/RTTI.mqh>

//+------------------------------------------------------------------+
//| Assemble values of built-in types in MqlParam array              |
//+------------------------------------------------------------------+
class MqlParamBuilder
{
protected:
   MqlParam array[];
   int n; // latest element in 'array', a reference for assignment
   
   void assign(const float v)
   {
      array[n].double_value = v;
   }

   void assign(const double v)
   {
      array[n].double_value = v;
   }

   void assign(const string v)
   {
      array[n].string_value = v;
   }

   // assume enums, colors, datetime etc (compatible with long ints),
   // also throw error or warning for unsupported types
   template<typename T>
   void assign(const T v)
   {
      array[n].integer_value = v;
   }
   
public:
   // append parameter value to internal array
   template<typename T>
   MqlParamBuilder *operator<<(T v)
   {
      // expand array
      n = ArraySize(array);
      ArrayResize(array, n + 1);
      ZeroMemory(array[n]);
      // detect appropriate type
      array[n].type = rtti(v);
      if(array[n].type == 0) array[n].type = TYPE_INT; // assume enum
      // store value in proper field
      assign(v);
      return &this;
   }

   // export internal array to output
   void operator>>(MqlParam &params[])
   {
      ArraySwap(array, params);
   }
   
   // import external array into this
   void operator<<(MqlParam &params[])
   {
      ArraySwap(array, params);
   }
   
   int size() const
   {
      return ArraySize(array);
   }
   
   ENUM_DATATYPE typeOf(int i) const
   {
      return i >= 0 && i < ArraySize(array) ? array[i].type : (ENUM_DATATYPE)0;
   }
};
//+------------------------------------------------------------------+
