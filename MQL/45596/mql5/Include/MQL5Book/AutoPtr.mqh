//+------------------------------------------------------------------+
//|                                                      AutoPtr.mqh |
//|                               Copyright (c) 2019-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#define FREE(P) { if(CheckPointer(P) == POINTER_DYNAMIC) delete P; }

//+------------------------------------------------------------------+
//| Safe-pointer templated class                                     |
//+------------------------------------------------------------------+
template<typename T>
class AutoPtr
{
private:
   T *ptr;

public:
   AutoPtr() : ptr(NULL)
   {
      #ifdef DEBUG_PRINT
      Print(__FUNCSIG__, " ", &this, ": NULL");
      #endif
   }
   
   AutoPtr(T *p) : ptr(p)
   {
      #ifdef DEBUG_PRINT
      Print(__FUNCSIG__, " ", &this, ": ", ptr);
      #endif
   }
   
   AutoPtr(AutoPtr &p)
   {
      #ifdef DEBUG_PRINT
      Print(__FUNCSIG__, " ", &this, ": ", ptr, " -> ", p.ptr);
      #endif
      FREE(ptr);
      ptr = p.ptr;
      p.ptr = NULL;
   }

   ~AutoPtr()
   {
      #ifdef DEBUG_PRINT
      Print(__FUNCSIG__, " ", &this, ": ", ptr);
      #endif
      FREE(ptr);
   }

   AutoPtr *operator=(AutoPtr &ref)
   {
      #ifdef DEBUG_PRINT
      Print(__FUNCSIG__, " ", &this, ": ", ptr, " -> ", ref.ptr);
      #endif
      FREE(ptr);
      ptr = ref.ptr;
      ref.ptr = NULL;
      return &this;
   }

   T *operator=(const T *n)
   {
      #ifdef DEBUG_PRINT
      Print(__FUNCSIG__, " ", &this, ": ", ptr, " -> ", n);
      #endif
      FREE(ptr);
      ptr = (T *)n;
      return ptr;
   }

   T *operator[](int x = 0) const
   {
      return ptr;
   }
};
//+------------------------------------------------------------------+