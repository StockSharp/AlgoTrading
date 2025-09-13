//+------------------------------------------------------------------+
//|                                         TemplatesSimpleArray.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/SimpleArray.mqh>

//+------------------------------------------------------------------+
//| Dummy class                                                      |
//+------------------------------------------------------------------+
class Dummy
{
   int x;
public:
   Dummy(int i) : x(i) { }
   /*
   // Copying of objects can be resource-consuming, especially
   // if many temporary objects are passed back and forth.
   // Copy-constructor does not exist by default,
   // so don't add it, unless overheads are managed somehow,
   // implemenation below is shown just for reference
   Dummy(const Dummy &d)
   {
      x = d.x;
   }
   */
   int value() const
   {
      return x;
   }
};

//+------------------------------------------------------------------+
//| Test struct                                                      |
//+------------------------------------------------------------------+
struct Properties
{
   int x;
   string s;
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // storage for class objects
   SimpleArray<AutoPtr<Dummy>> arrayObjects;
   AutoPtr<Dummy> ptr = new Dummy(20);
   arrayObjects << ptr;
   arrayObjects << new AutoPtr<Dummy>(new Dummy(30));
   Print("Size: ", arrayObjects.size());
   arrayObjects.print(); // prints nothing for classes/pointers
   Print(arrayObjects[0][].value());
   Print(arrayObjects[1][].value());

   // storage for numbers
   SimpleArray<double> arrayNumbers;
   arrayNumbers << 1.0 << 2.0 << "3.0";
   arrayNumbers.print();
   
   // storage for struct objects
   SimpleArray<Properties> arrayStructs;
   Properties prop = {12345, "abc"};
   arrayStructs << prop;
   arrayStructs.print();

   /*
   // will work if Dummy has a copy constructor
   SimpleArray<Dummy> good;
   good << new Dummy(0);
   */
   
   // NB: SimpleArray is intended for objects, not pointers!
   // It's supposed to use AutoPtr object as a wrapper for pointers.
   
   // ERRORs below (all break in SimpleArray.mqh)
   
   // SimpleArray<Dummy> array;
   // object of 'Dummy' cannot be returned,
   //    copy constructor 'Dummy::Dummy(const Dummy &)' not found
   
   // SimpleArray<Dummy*> array;
   // '*' - object pointer expected
}
//+------------------------------------------------------------------+
