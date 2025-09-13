//+------------------------------------------------------------------+
//|                                                   ConverterT.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| 2-way bitwise exact conversion between T1 and T2                 |
//+------------------------------------------------------------------+
template<typename T1,typename T2>
class Converter
{
private:
   union DataOverlay
   {
      T1 L;
      T2 D;
   };

   DataOverlay data;

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
