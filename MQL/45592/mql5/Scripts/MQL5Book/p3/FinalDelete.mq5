//+------------------------------------------------------------------+
//|                                                  FinalDelete.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Base stub                                                        |
//+------------------------------------------------------------------+
class Base
{
public:
   void method() { Print(__FUNCSIG__); }
};

//+------------------------------------------------------------------+
//| Derived stub                                                     |
//+------------------------------------------------------------------+
class Derived final : public Base
{
public:
   void method() = delete;
   void derivedMethod() { Print(__FUNCSIG__); }
};

/*
// ERROR:
// cannot inherit from 'Derived' as it has been declared as 'final'
class Concrete : public Derived
{
};
*/

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Base *b;
   Derived d;
   
   b = &d;
   b.method();
   
   ((Derived *)b).derivedMethod(); // explicit typecasting in-place

   /*
   // ERROR:   
   // attempting to reference deleted function 'void Derived::method()'
   //    function 'void Derived::method()' was explicitly deleted
   ((Derived *)b).method();
   d.method();
   */
}

//+------------------------------------------------------------------+
