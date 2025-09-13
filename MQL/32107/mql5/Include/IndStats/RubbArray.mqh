
template <typename T>
class RubbArray
{
  private:
    T *data[];
    
  protected:
    void add(T *d)
    {
      int n = ArraySize(data);
      ArrayResize(data, n + 1);
      data[n] = d;
    }

    void arrayShrink(const int dst, const int src)
    {
      for(int i = src; i < ArraySize(data); ++i)
      {
        data[dst + i - src] = data[i];
      }
    }
    
  public:
    virtual ~RubbArray()
    {
      clear();
    }
    
    void clear()
    {
      int i, n = ArraySize(data);
      for(i = 0; i < n; i++)
      {
        if(CheckPointer(data[i]) == POINTER_DYNAMIC) delete data[i];
      }
      ArrayResize(data, 0);
    }
    
    T *operator[](int i) const
    {
      if(i >= ArraySize(data))
      {
        Print("Array size=", ArraySize(data), ", index=", i);
        return NULL;
      }
      return data[i];
    }

    T *top() const
    {
      if(ArraySize(data) == 0)
      {
        Print("Array size=0");
        return NULL;
      }
      return data[ArraySize(data) - 1];
    }
    
    RubbArray *operator<<(T *d)
    {
      add(d);
      return GetPointer(this);
    }

    /*
    T operator=(T *d)
    {
      add(d);
      return d;
    }
    */

    void operator=(const RubbArray &d)
    {
      int i, n = d.size();
      ArrayResize(data, n);
      for(i = 0; i < n; i++)
      {
        data[i] = d[i];
      }
    }

    T *operator>>(int i)
    {
      T *d = this[i];
      if(d == NULL) return NULL;
      #ifdef __MQL5__
      ArrayCopy(data, data, i, i + 1);
      #else
      arrayShrink(i, i + 1);
      #endif
      ArrayResize(data, ArraySize(data) - 1);
      return d;
    }
    
    T *pop()
    {
      int _size = ArraySize(data) - 1;
      T *d = this[_size];
      ArrayResize(data, _size);
      return d;
    }
    
    int size() const
    {
      return ArraySize(data);
    }
    
    /*
    void print() const
    {
      int i, n = ArraySize(data);
      string s;
      for(i = 0; i < n; i++)
      {
        s += (string)data[i] + ",";
      }
      Print(s);
    }
    */
};

/* usage
void OnStart()
{
  RubbArray<TestEnum> d, d2;
  //RubbArray<double> d, d2;
  d << 5 << 7;
  d = 10;
  d << 15;
  d.print();
  Print(d[1]);
  double x = d >> 1;
  d2 = d;
  d2.print();
}
*/