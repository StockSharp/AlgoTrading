//+------------------------------------------------------------------+
//|                                              Converter(NaNs).mqh |
//|                               Copyright (c) 2019-2020, Marketeer |
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

static Converter<ulong,double> NaNs;
static double inf = NaNs[0x7FF0000000000000];    // +infinity
                      // 0xFFF0000000000000         -infinity
static double nan = NaNs[0x7FF8000000000000];    // quiet NaN
static double nanind = NaNs[0xFFF8000000000000]; // -nan(ind)

template<typename T>
T __getINF(T dummy)
{
  return new T(inf);
}

double __getINF(double dummy)
{
  return inf;
}

template<typename T>
T __getNAN(T dummy)
{
  return new T(nan);
}

double __getNAN(double dummy)
{
  return nan;
}
