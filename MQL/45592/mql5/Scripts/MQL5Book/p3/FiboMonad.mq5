//+------------------------------------------------------------------+
//|                                                    FiboMonad.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

class Fibo
{
   int previous;
   int current;
public:
   Fibo() : current(1), previous(0) { }
   Fibo(const Fibo &other) : current(other.current), previous(other.previous) { }
   
   Fibo *operator=(const Fibo &other)
   {
      current = other.current;
      previous = other.previous;
      return &this;
   }

   Fibo *operator=(const Fibo *other)
   {
      current = other.current;
      previous = other.previous;
      return &this;
   }
   
   Fibo *operator++() // prefix
   {
      int temp = current;
      current = current + previous;
      previous = temp;
      return &this;
   }

   Fibo operator++(int) // postfix
   {
      Fibo temp = this;
      ++this;
      return temp;
   }

   Fibo *operator--() // prefix
   {
      int diff = current - previous;
      current = previous;
      previous = diff;
      return &this;
   }

   Fibo operator--(int) // postfix
   {
      Fibo temp = this;
      --this;
      return temp;
   }
   
   Fibo *operator+=(int index)
   {
      for(int i = 0; i < index; ++i)
      {
         ++this;
      }
      return &this;
   }

   Fibo *operator[](int index)
   {
      current = 1;
      previous = 0;
      for(int i = 0; i < index; ++i)
      {
         ++this;
      }
      return &this;
   }
   
   int operator~() const
   {
      return current;
   }
};

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   Fibo f1, f2, f3, f4;
   for(int i = 0; i < 10; ++i, ++f1) // prefix increment
   {
      f4 = f3++; // postfix increment and overloaded assignment
   }

   // compare all increments with indexed access [10]
   Print(~f1, " ", ~f2[10], " ", ~f3, " ", ~f4); // 89 89 89 55

   // count down back to 0
   Fibo f0;
   Fibo f = f0[10]; // copy-constructor call (because of initialization)
   for(int i = 0; i < 10; ++i)
   {
      Print(~--f); // 55, 34, 21, 13, 8, 5, 3, 2, 1, 1
   }
   
   Fibo f5;
   Fibo *pf5 = &f5;
   
   f5 = f4;   // calls Fibo *operator=(const Fibo &other) 
   f5 = &f4;  // calls Fibo *operator=(const Fibo *other)
   pf5 = &f4; // calls nothing, assigns &f4 to pf5!
   
}
//+------------------------------------------------------------------+
