//+------------------------------------------------------------------+
//|                                                      AutoPtr.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

template<typename T>
class AutoPtr
{
  private:
    T *ptr;
    
  public:
    AutoPtr(): ptr(NULL) {}
    AutoPtr(T *p): ptr(p) {}
    ~AutoPtr()
    {
      if(CheckPointer(ptr) == POINTER_DYNAMIC) delete ptr;
    }
    
    T *operator=(T *n)
    {
      if(CheckPointer(ptr) == POINTER_DYNAMIC) delete ptr;
      ptr = n;
      return ptr;
    }
    
    T* operator~()
    {
      return ptr;
    }
};
