//+------------------------------------------------------------------+
//|                                                 ArrayDynamic.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))
#define FORALL(A) for(int _iterator_ = 0; _iterator_ < ArraySize(A); ++_iterator_)
#define FREE(P) { if(CheckPointer(P) == POINTER_DYNAMIC) delete (P); }
#define CALLALL(A, CALL) FORALL(A) { CALL(A[_iterator_]) }

//+------------------------------------------------------------------+
//| Extend the array by 1 element and put the value into it          |
//+------------------------------------------------------------------+
template<typename T>
void ArrayExtend(T &array[], const T value)
{
   if(ArrayIsDynamic(array))
   {
      const int n = ArraySize(array);
      ArrayResize(array, n + 1);
      array[n] = (T)value;
   }
}

//+------------------------------------------------------------------+
//| Dummy class for objects to place in array                        |
//+------------------------------------------------------------------+
class Dummy
{
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   int dynamic[];
   int fixed[10] = {}; // all zeros
   
   PRT(ArrayResize(fixed, 0)); // warning: cannot be used for static allocated array
   
   for(int i = 0; i < 10; ++i)
   {
      ArrayExtend(dynamic, (i + 1) * (i + 1));
      ArrayExtend(fixed, (i + 1) * (i + 1));
   }

   Print("Filled");
   ArrayPrint(dynamic);
   ArrayPrint(fixed);
   
   ArrayFree(dynamic);
   ArrayFree(fixed); // warning: cannot be used for static allocated array

   Print("Free Up");
   ArrayPrint(dynamic); // produces no output
   ArrayPrint(fixed);
   
   /*
   
      output
   
   ArrayResize(fixed,0)=0
   Filled   
     1   4   9  16  25  36  49  64  81 100
   0 0 0 0 0 0 0 0 0 0
   Free Up
   0 0 0 0 0 0 0 0 0 0
   
   */
   
   Dummy *dummies[] = {};
   ArrayExtend(dummies, new Dummy());
   ArrayFree(dummies);

   /*
      output
      
   1 undeleted objects left
   1 object of type Dummy left
   24 bytes of leaked memory

   */

   /*
      // example of array cleanup to prevent memory loss
      // if array contains dynamic pointers
      
      FORALL(dummies)
      {
         FREE(dummies[_iterator_]);
      }
      
      // OR (shorthand form)

      CALLALL(dummies, FREE);
      
      // the cleanup above should be executed before ArrayFree
      ArrayFree(dummies);
   */
}
//+------------------------------------------------------------------+
