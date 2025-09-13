//+------------------------------------------------------------------+
//|                                            TemplatesExtended.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| External declarations in templates                               |
//+------------------------------------------------------------------+
template<typename T>
class ClassType
{
   ClassType() // private ctor, used only once in the 'create' method
   {
      s = &this;
   }
   static ClassType *s; // pointer to existing object
public:
   static ClassType *create() // hard init (ensure object is created)
   {
      static ClassType single; // local static: singleton per T
      return &single;
   }
   
   static ClassType *check() // soft init (return existing pointer w/o creation)
   {
      return s;
   }
   
   template<typename U>
   void method(const U &u);
};

template<typename T>
template<typename U>
void ClassType::method(const U &u)
{
   Print(__FUNCSIG__, " ", typename(T), " ", typename(U));
}

template<typename T>
static ClassType<T> * ClassType::s = NULL;


//+------------------------------------------------------------------+
//| Inheritance with templates                                       |
//+------------------------------------------------------------------+

#define RTTI Print(typename(this))

template<typename T, typename B>
class DerivedFrom
{
   typedef void(*ppp)(T*);
   static void constraints(T* p) { B* pb = p; }
public:
   DerivedFrom() { ppp p = constraints; }
};

class Base
{
public:
   Base() { RTTI; }
};

template<typename T> 
class Derived : public T
{
public:
   Derived() { RTTI; }
}; 

template<typename T> 
class Base1
{
   Derived<T> object;
public:
   Base1() { RTTI; }
}; 

template<typename T>                // full "specialization"
class Derived1 : public Base1<Base> // (1 of 1 parameter specified)
{
   DerivedFrom<T,Base> df();        // constraint: want T based on Base
public:
   Derived1() { RTTI; }
}; 

template<typename T,typename E> 
class Base2 : public T
{
public:
   Base2() { RTTI; E e = (E)""; }       // constraint: stringifiable E expected
}; 

template<typename T>                    // partial "specialization"
class Derived2 : public Base2<T,string> // (1 of 2 parameters specified)
{
public:
   Derived2() { RTTI; }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   ClassType<string> *object = ClassType<string>::create();
   double d = 5.0;
   object.method(d);
   // OUTPUT:
   // void ClassType<string>::method<double>(const double&) string double
   
   Print(ClassType<string>::check()); // 1048576 (example of instance id)
   Print(ClassType<long>::check());   // 0 (no instance for T=long)
   
   Derived2<Derived1<Base>> derived2;
   // OUTPUT:
   // Base
   // Derived<Base>
   // Base1<Base>
   // Derived1<Base>
   // Base2<Derived1<Base>,string>
   // Derived2<Derived1<Base>>
}
//+------------------------------------------------------------------+