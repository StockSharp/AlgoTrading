//+------------------------------------------------------------------+
//|                                                  SimpleArray.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

template<typename T>
class SimpleArray
{
protected:
   T data[];
   
   int expand()
   {
      const int n = ArraySize(data);
      ArrayResize(data, n + 1);
      return n;
   }
   
public:
   SimpleArray *operator<<(const T &r)
   {
      data[expand()] = (T)r; // (T) removes 'const'
      // otherwise operator= overload will not apply
      
      return &this;
   }

   template<typename U>
   SimpleArray *operator<<(U u)
   {
      data[expand()] = (T)u;
      return &this;
   }

   template<typename P>
   SimpleArray *operator<<(P *p)
   {
      data[expand()] = (T)*p;
      if(CheckPointer(p) == POINTER_DYNAMIC) delete p;
      return &this;
   }

   T operator[](int i) const
   {
      return data[i];
   }
   
   int size() const
   {
      return ArraySize(data);
   }
   
   void print() const
   {
      ArrayPrint(data);
   }
};
//+------------------------------------------------------------------+