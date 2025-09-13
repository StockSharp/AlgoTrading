//+------------------------------------------------------------------+
//|                                             TemplatesAutoPtr.mq5 |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#define DEBUG_PRINT
#include <MQL5Book/AutoPtr.mqh>

//+------------------------------------------------------------------+
//| Dummy class                                                      |
//+------------------------------------------------------------------+
class Dummy
{
   int x;
public:
   Dummy(int i) : x(i)
   {
      Print(__FUNCSIG__, " ", &this);
   }
   ~Dummy()
   {
      Print(__FUNCSIG__, " ", &this);
   }
   int value() const
   {
      return x;
   }
};

//+------------------------------------------------------------------+
//| Derived class to handle by AutoPtr<Dummy> as well                |
//+------------------------------------------------------------------+
class Gummy : public Dummy
{
public:
   Gummy(int i) : Dummy(-i) { }
};

//+------------------------------------------------------------------+
//| Template specification/instantiation via inheritance             |
//+------------------------------------------------------------------+
class DummyPtr : AutoPtr<Dummy>
{
};

//+------------------------------------------------------------------+
//| Test struct                                                      |
//+------------------------------------------------------------------+
struct XYZ
{
   int x;
};

//+------------------------------------------------------------------+
//| Fabric function                                                  |
//+------------------------------------------------------------------+
AutoPtr<Dummy> generator()
{
   AutoPtr<Dummy> ptr(new Dummy(1));

   ptr = new Dummy(2);
   return ptr;
}


//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   AutoPtr<Dummy> ptr = generator();
   Print(ptr[].value());             // 2

   // OK: can manage derived objects
   // AutoPtr<Dummy> ptr2 = new Gummy(10);

   // ERRORS:
   // 'ptr' - parameter conversion not allowed
   // 'ptr' - object pointer expected
   // '*' - pointer cannot be used
   // AutoPtr<string> s;
}
//+------------------------------------------------------------------+
