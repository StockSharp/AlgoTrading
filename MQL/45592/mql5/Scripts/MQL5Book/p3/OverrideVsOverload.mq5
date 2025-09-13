//+------------------------------------------------------------------+
//|                                           OverrideVsOverload.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Base                                                             |
//+------------------------------------------------------------------+
class Base
{
public:
   void nonvirtual(float v)
   {
      Print(__FUNCSIG__, " ", v);
   }
   virtual void process(float v)
   {
      Print(__FUNCSIG__, " ", v);
   }
};

//+------------------------------------------------------------------+
//| Derived                                                          |
//+------------------------------------------------------------------+
class Derived : public Base
{
public:
   void nonvirtual(int v)
   {
      Print(__FUNCSIG__, " ", v);
   }
   /*
   // this Derived::nonvirtual(float v) redeclaration
   // will remove "deprecated behavior, hidden method calling" warning
   void nonvirtual(float v)
   {
      Base::nonvirtual(v);
      Print(__FUNCSIG__, " ", v);
   }
   */
   virtual void process(int v) // override
   // error: 'Derived::process' method is declared with 'override' specifier,
   // but does not override any base class method
   {
      Print(__FUNCSIG__, " ", v);
   }
};

//+------------------------------------------------------------------+
//| Concrete, 2nd level derived                                      |
//+------------------------------------------------------------------+
class Concrete : public Derived
{
};

//+------------------------------------------------------------------+
//| Special, 3rd level derived                                       |
//+------------------------------------------------------------------+
class Special : public Concrete
{
public:
   virtual void process(int v) override
   {
      Print(__FUNCSIG__, " ", v);
   }
   virtual void process(float v) override
   {
      Print(__FUNCSIG__, " ", v);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Print("");

   float f = 2.0;
   int i = 1;

   Concrete c;
   Base *ptr = &c;
   
   // Static binding tests

   ptr.nonvirtual(i); // Base::nonvirtual(float), int -> float conversion
   c.nonvirtual(i);   // Derived::nonvirtual(int)

   // warning: deprecated behavior, hidden method calling
   c.nonvirtual(f);   // Base::nonvirtual(float), because
                      // static lookup in Base,
                      // Derived::nonvirtual(int) does not fit

   // Dynamic binding tests

   // note: no Base::process(int) and
   // no overrides for process(float) in classes up to Concrete (including)
   ptr.process(i);    // Base::process(float), int -> float conversion
   c.process(i);      // Derived::process(int), because
                      // Concrete doesn't have an override,
                      // override in Special doesn't count

   Special s;
   ptr = &s;
   // note: no Base::process(int) in ptr
   ptr.process(i);    // Special::process(float), int -> float conversion
   ptr.process(f);    // Special::process(float)

   Derived *d = &s;
   d.process(i);      // Special::process(int)

   // warning: deprecated behavior, hidden method calling
   d.process(f);      // Special::process(float)
}
//+------------------------------------------------------------------+
