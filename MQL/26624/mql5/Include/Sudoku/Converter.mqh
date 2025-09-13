//+------------------------------------------------------------------+
//|                                                    Converter.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

template<typename T1,typename T2>
union _L2D
{
  T1 L;
  T2 D;
};

template<typename T1,typename T2>
class Converter
{
  private:
    _L2D<T1,T2> L2D;
  
  public:
    T2 operator[](const T1 L)
    {
      L2D.L = L;
      return L2D.D;
    }

    T1 operator[](const T2 D)
    {
      L2D.D = D;
      return L2D.L;
    }
};
