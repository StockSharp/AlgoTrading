//+------------------------------------------------------------------+
//|                                              CounterConstPtr.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Counter class                                                    |
//+------------------------------------------------------------------+
class Counter
{
public:
   int counter;

   Counter(const int n = 0) : counter(n) { }

   void increment() // non-const method
   {
      ++counter;
   }

   Counter *clone() const // const method
   {
      return new Counter(counter);
   }
};

//+------------------------------------------------------------------+
//| Function with non-const pointer                                  |
//+------------------------------------------------------------------+
void functionVolatile(Counter *ptr)
{
   // OK: all methods and properties are accessible through ptr
   ptr.increment();
   ptr.counter += 2;
   
   // delete right away to free up memory, because
   delete ptr.clone(); // we need the clone just to demo the method call
   ptr = NULL;
}

//+------------------------------------------------------------------+
//| Function with a pointer to const object                          |
//+------------------------------------------------------------------+
void functionConst(const Counter *ptr)
{
   /*
   // ERRORS:
   ptr.increment(); // call non-const method for constant object
   ptr.counter = 1; // constant cannot be modified
   */

   // OK: only const methods are accessible, fields are read-only
   Print(ptr.counter); // reading const object
   Counter *clone = ptr.clone(); // calling constant method
   ptr = clone;     // changing non-const ptr pointer
   delete ptr;      // cleanup
}

//+------------------------------------------------------------------+
//| Function with a const pointer to a const object                  |
//+------------------------------------------------------------------+
void functionConstConst(const Counter * const ptr)
{
   Counter local(0);
   /*
   // ERRORS:
   ptr.increment(); // call non-const method for constant object
   ptr.counter = 1; // constant cannot be modified
   ptr = &local;    // constant cannot be modified
   */
   
   // OK: only const methods are accessible, ptr can't be changed
   Print(ptr.counter); // reading const object
   delete ptr.clone(); // calling constant method
}

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Counter counter;
   const Counter constCounter;
   
   counter.increment();
   
   // ERROR:
   // constCounter.increment(); // call non-const method for constant object
   Counter *ptr = (Counter *)&constCounter; // typecasting workaround
   ptr.increment();
   
   // cannot convert from const pointer to nonconst pointer
   // Counter * const ptrc = &constCounter;
   // ptrc = &counter; // constant cannot be modified
   
   functionVolatile(&counter);
   
   // ERROR: cannot convert from const pointer...
   // functionVolatile(&constCounter); // to nonconst pointer
   
   functionVolatile((Counter *)&constCounter); // typecasting workaround
   
   functionConst(&counter);
   functionConst(&constCounter);
   
   functionConstConst(&counter);
   functionConstConst(&constCounter);
}
//+------------------------------------------------------------------+
