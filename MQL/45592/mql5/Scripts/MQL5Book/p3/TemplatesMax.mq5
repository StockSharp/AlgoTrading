//+------------------------------------------------------------------+
//|                                                  TemplateMax.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

/*
  The following overloaded function template with 'const T &' parameters
  is required to accept classes/structs as its arguments.
  Otherwise we get multiple errors, such as:
  'objects are passed by reference only' and
  'ambiguous call to overloaded function with the same parameters'
*/

template<typename T>
T Max(T value1, T value2)
{
   Print(__FUNCSIG__, " T=", typename(T));
   return value1 > value2 ? value1 : value2;
}

template<typename T>
T Max(const T &value1, const T &value2, const bool ref = false)
{
   Print(__FUNCSIG__, " T=", typename(T));
   return value1 > value2 ? value1 : value2;
}

/*
// uncomment this for more specific version for pointers

template<typename T>
T *Max(T *value1, T *value2)
{
   Print(__FUNCSIG__, " T=", typename(T));
   return value1 > value2 ? value1 : value2;
}
*/

//+------------------------------------------------------------------+
//| Example of a struct                                              |
//+------------------------------------------------------------------+
struct Dummy
{
   int x;
   bool operator>(const Dummy &other) const
   {
      return x > other.x;
   }
};

//+------------------------------------------------------------------+
//| Example of a class                                               |
//+------------------------------------------------------------------+
class Data
{
public:
   int x;
   bool operator>(const Data &other) const
   {
      Print(__FUNCSIG__);
      return x > other.x;
   }
};

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void OnStart()
{
   double d1 = 0, d2 = 1;
   datetime t1 = D'2020.01.01', t2 = D'2021.10.10';
   Print(Max(d1, d2));
   Print(Max(t1, t2));
   // ERROR:
   //    template parameter ambiguous, could be 'double' or 'datetime'
   // Print(Max(d1, t1));
   
   Print(Max<ulong>(1000, 10000000));
   
   Dummy object1 = {}, object2 = {};
   Max(object1, object2);
   // ERRORs (before overloads are added):
   // if only the following function is presented
   // T Max(T value1, T value2)
   //    'object1' - objects are passed by reference only
   //    'Max' - cannot to apply template
   
   // Without overload T *Max(T *value1, T *value2)
   // the following code will call T Max(const T &value1, const T &value2)
   // with T=Data*
   Data *pointer1 = new Data();
   Data *pointer2 = new Data();
   Data *m = Max(pointer1, pointer2);
   delete pointer1;
   delete pointer2;
}
//+------------------------------------------------------------------+
