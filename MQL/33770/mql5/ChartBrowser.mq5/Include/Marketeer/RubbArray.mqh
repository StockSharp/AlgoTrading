//+------------------------------------------------------------------+
//|                                                    RubbArray.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                            https://www.mql5.com/ru/articles/5638 |
//+------------------------------------------------------------------+

template <typename T>
interface Clonable
{
  T clone();
};

template <typename T>
class BaseArray
{
  protected:
    T data[];
    
  public:
    virtual ~BaseArray()
    {
      clear();
    }
    
    virtual void clear()
    {
      ArrayResize(data, 0);
    }
    
    T operator[](int i) const
    {
      return get(i);
    }

    T get(int i) const
    {
      if(i < 0 || i >= ArraySize(data))
      {
        Print("Array size=", ArraySize(data), ", index=", i);
        return NULL;
      }
      return data[i];
    }

    T top() const
    {
      if(ArraySize(data) == 0)
      {
        Print("Array size=0");
        return NULL;
      }
      return data[ArraySize(data) - 1];
    }
    
    T peek() const
    {
      return top();
    }
    
    BaseArray *add(T d)
    {
      int n = ArraySize(data);
      ArrayResize(data, n + 1);
      data[n] = d;
      return &this;
    }

    BaseArray *operator<<(T d)
    {
      return add(d);
    }

    BaseArray *operator<<(const BaseArray<T> *x)
    {
      for(int i = 0; i < x.size(); i++)
      {
        Clonable<T> *clone = dynamic_cast<Clonable<T> *>(x[i]);
        if(clone != NULL)
        {
          add(clone.clone());
        }
        else
        {
          add(x[i]);
        }
      }
      return &this;
    }
    
    BaseArray *push(T d)
    {
      return add(d);
    }

    void operator=(const BaseArray &d)
    {
      int i, n = d.size();
      ArrayResize(data, n);
      for(i = 0; i < n; i++)
      {
        data[i] = d[i];
      }
    }

    T operator>>(int i)
    {
      T d = this[i];
      if(d == NULL) return NULL;
      int n = ArraySize(data) - 1;
      if(i < n)
      {
        ArrayCopy(data, data, i, i + 1);
      }
      ArrayResize(data, n);
      return d;
    }
    
    T pop()
    {
      int _size = ArraySize(data) - 1;
      T d = this[_size];
      ArrayResize(data, _size);
      return d;
    }
    
    int size() const
    {
      return ArraySize(data);
    }
    
    
    string toString() const
    {
      static string formats[4][2] = {{"double", "%f"}, {"long", "%i"}, {"string", "%s"}, {"int", "%i"}};
      string fmt = "%x";
      for(int k = 0; k < ArrayRange(formats, 0); k++)
      {
        if(typename(T) == formats[k][0])
        {
          fmt = formats[k][1];
          break;
        }
      }
      
      int i, n = ArraySize(data);
      string s;
      for(i = 0; i < n; i++)
      {
        s += StringFormat(fmt, data[i]) + ",";
      }
      return (s);
    }
    

};

template <typename T>
class RubbArray: public BaseArray<T>
{
  public:
    RubbArray()
    {
    }

    ~RubbArray()
    {
      clear();
    }
    
    virtual void clear() override
    {
      int i, n = ArraySize(data);
      for(i = 0; i < n; i++)
      {
        if(CheckPointer(data[i]) == POINTER_DYNAMIC) delete data[i];
      }
      ArrayResize(data, 0);
    }
    
    T replace(const int i, T v)
    {
      int n = ArraySize(data);
      if(i < n)
      {
        if(CheckPointer(data[i]) == POINTER_DYNAMIC) delete data[i];
        data[i] = v;
      }
      return v;
    }
    
};

#define List RubbArray
#define Stack RubbArray
