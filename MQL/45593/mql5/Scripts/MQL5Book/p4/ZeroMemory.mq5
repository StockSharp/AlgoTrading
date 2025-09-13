//+------------------------------------------------------------------+
//|                                                   ZeroMemory.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define LIMIT 5

//+------------------------------------------------------------------+
//| Simple struct to be filled with zeroes                           |
//+------------------------------------------------------------------+
struct Simple
{
   MqlDateTime data[]; // dynamic array prevents initializer list,
   // string s;        // a string field would prevent it either,
   // ClassType *ptr;  // and a pointer would do also
   Simple()
   {
      // allocate memory, it may contain arbitrary data
      ArrayResize(data, LIMIT);
   }
};

//+------------------------------------------------------------------+
//| Base class to be filled with zeroes                              |
//+------------------------------------------------------------------+
class Base
{
public:
   // const on any field will prevent ZeroMemory use by compiler error:
   // "not allowed for objects with protected members or inheritance"
   /* const */ int x;
   Simple t; // use nested struct: it'll be deep-filled with zeros as well
   Base()
   {
      x = rand();
   }
   virtual void print() const
   {
      PrintFormat("%d %d", &this, x);
      ArrayPrint(t.data);
   }
};

//+------------------------------------------------------------------+
//| Derived class to be filled with zeroes                           |
//+------------------------------------------------------------------+
class Dummy : public Base
{
// member variables with any access other than public
// will prevent ZeroMemory use by compiler error:
// "not allowed for objects with protected members or inheritance"
/*
private:
protected:
*/
public:
   double data[]; // can be multidimensional array
   string s;
   Base *pointer;

public:
   Dummy()
   {
      ArrayResize(data, LIMIT);
      
      // because of ZeroMemory use, we'll loose this 'pointer'
      // and get the warnings after the script termination
      // about undeleted objects of type Base
      pointer = new Base();
   }
   
   ~Dummy()
   {
      // because of ZeroMemory use, the pointer will become invalid
      if(CheckPointer(pointer) != POINTER_INVALID) delete pointer;
   }
   
   virtual void print() const override
   {
      Base::print();
      ArrayPrint(data);
      Print(pointer);
      if(CheckPointer(pointer) != POINTER_INVALID) pointer.print();
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   // Simple simple = {}; // error: cannot be initialized with initializer list
   Simple simple;
   ZeroMemory(simple);
   
   /*
   // Don't use ZeroMemory to clear pointers:
   Base *base = new Base();
   ZeroMemory(base); // will set the poiter to NULL, but leave the object
   */
   
   string text[LIMIT] = {};
   // some algorithm fills and uses 'text'
   // ...
   // then we need to reuse it, but neither of Array... functions work
   // ArrayInitialize(text, NULL); // no one of the overloads can be applied to the function call
   // ArrayFill(text, 0, 10, NULL);// 'string' type cannot be used in ArrayFill function
   ZeroMemory(text);               // ok
   
   Print("Initial state");
   Dummy array[];
   ArrayResize(array, LIMIT);
   for(int i = 0; i < LIMIT; ++i)
   {
      array[i].print();
   }
   ZeroMemory(array);
   Print("ZeroMemory done");
   for(int i = 0; i < LIMIT; ++i)
   {
      array[i].print();
   }
   
   /*
      example output (will differ in initial state due to random memory allocation)
      
      Initial state
      1048576 31539
           [year]     [mon]    [day] [hour] [min] [sec] [day_of_week] [day_of_year]
      [0]       0     65665       32      0     0     0             0             0
      [1]       0         0        0      0     0     0         65624             8
      [2]       0         0        0      0     0     0             0             0
      [3]       0         0        0      0     0     0             0             0
      [4] 5242880 531430129 51557552      0     0 65665            32             0
      0.0 0.0 0.0 0.0 0.0
      ...
      ZeroMemory done
      1048576 0
          [year] [mon] [day] [hour] [min] [sec] [day_of_week] [day_of_year]
      [0]      0     0     0      0     0     0             0             0
      [1]      0     0     0      0     0     0             0             0
      [2]      0     0     0      0     0     0             0             0
      [3]      0     0     0      0     0     0             0             0
      [4]      0     0     0      0     0     0             0             0
      0.0 0.0 0.0 0.0 0.0
      ...
      5 undeleted objects left
      5 objects of type Base left
      3200 bytes of leaked memory
   */
}
//+------------------------------------------------------------------+
