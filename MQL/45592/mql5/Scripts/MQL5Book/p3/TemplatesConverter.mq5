//+------------------------------------------------------------------+
//|                                           TemplatesConverter.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define MAX_LONG_IN_DOUBLE       9007199254740992
// FYI: ULONG_MAX            18446744073709551615

//+------------------------------------------------------------------+
//| 2-way bitwise exact conversion between T1 and T2                 |
//+------------------------------------------------------------------+
template<typename T1,typename T2>
class Converter
{
private:
   // nested template is used here for demo purpose:
   // more direct approach is to make it a simple union (not a template)
   // and declare L and D fields by T1 and T2 meta-types
   template<typename U1,typename U2>
   union DataOverlay
   {
      U1 L;
      U2 D;
   };

   DataOverlay<T1,T2> data;

public:
   T2 operator[](const T1 L)
   {
      data.L = L;
      return data.D;
   }

   T1 operator[](const T2 D)
   {
      data.D = D;
      return data.L;
   }
};


//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Converter<double,ulong> c;
   
   const ulong value = MAX_LONG_IN_DOUBLE + 1;

   double d = value; // possible loss of data due to type conversion
   ulong result = d; // possible loss of data due to type conversion
   
   Print(value == result); // false

   double z = c[value];
   ulong restored = c[z];
   
   Print(value == restored); // true
}
//+------------------------------------------------------------------+
