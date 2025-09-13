//+------------------------------------------------------------------+
//|                                                 ThisCallback.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

class Manager; // forward declaration

//+------------------------------------------------------------------+
//| Class for manageable objects                                     |
//+------------------------------------------------------------------+
class Element
{
   Manager *owner;

public:
   Element(Manager &t): owner(&t) { }

   void doMath()
   {
      const int N = 1000000;
      for(int i = 0; i < N; ++i)
      {
         if(i % (N / 20) == 0)
         {
            // pass self into the owner's method
            owner.progressNotify(&this, i * 100.0f / N);
         }
         // ... lot of calculations
      }
      owner.progressNotify(&this, 100.0f); // complete
   }
   
   string getMyName() const
   {
      return typename(this);
   }
};

//+------------------------------------------------------------------+
//| Manager                                                          |
//+------------------------------------------------------------------+
class Manager
{
   Element *elements[1]; // should be dynamic array

public:
   ~Manager()
   {
      // loop through all elements
      // ... 
      if(CheckPointer(elements[0]) == POINTER_DYNAMIC)
      {
         delete elements[0];
      }
   }
   
   Element *addElement()
   {
      // find empty slot in the elements array
      // ...
      // pass self into the manageable object's consturctor
      elements[0] = new Element(this);
      return elements[0];
   }
   
   void deleteElement(Element *)
   {
      // ...
   }

   void progressNotify(Element *e, const float percent)
   {
      // Manager selects notification method:
      // display, print, send over Internet, etc
      Print(e.getMyName(), "=", percent);
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Manager manager;
   manager.addElement().doMath();
   // ...
}
//+------------------------------------------------------------------+
