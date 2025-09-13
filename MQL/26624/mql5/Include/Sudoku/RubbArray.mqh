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
    
    virtual T operator[](uint i) const
    {
      return get(i);
    }

    T get(uint i) const
    {
      if(i >= (uint)ArraySize(data))
      {
        Print("Array size=", ArraySize(data), ", index=", i);
        return NULL;
      }
      return data[i];
    }

    virtual T top() const
    {
      if(ArraySize(data) == 0)
      {
        Print("Array size=0");
        return NULL;
      }
      return data[ArraySize(data) - 1];
    }
    
    virtual T peek() const
    {
      return top();
    }
    
    virtual BaseArray *add(T d)
    {
      uint n = ArraySize(data);
      ArrayResize(data, n + 1);
      data[n] = d;
      return &this;
    }

    virtual BaseArray *operator<<(T d)
    {
      return add(d);
    }

    virtual BaseArray *operator<<(const BaseArray<T> *x)
    {
      for(uint i = 0; i < x.size(); i++)
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
    
    virtual BaseArray *push(T d)
    {
      return add(d);
    }

    virtual void operator=(const BaseArray &d)
    {
      uint i, n = d.size();
      ArrayResize(data, n);
      for(i = 0; i < n; i++)
      {
        data[i] = d[i];
      }
    }

    virtual T operator>>(uint i)
    {
      T d = this[i];
      if(d == NULL) return NULL;
      uint n = ArraySize(data) - 1;
      if(i < n)
      {
        ArrayCopy(data, data, i, i + 1);
      }
      ArrayResize(data, n);
      return d;
    }
    
    virtual T pop()
    {
      if(ArraySize(data) == 0)
      {
        Print("Can't pop from empty array");
        return NULL;
      }
      uint _size = ArraySize(data) - 1;
      T d = this[_size];
      ArrayResize(data, _size);
      return d;
    }
    
    virtual uint size() const
    {
      return (uint)ArraySize(data);
    }
    
    
    virtual string toString() const
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
      
      uint i, n = size(); // ArraySize(data);
      string s;
      for(i = 0; i < n; i++)
      {
        s += StringFormat(fmt, this[i]) + ",";
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
      uint i, n = ArraySize(data);
      for(i = 0; i < n; i++)
      {
        if(CheckPointer(data[i]) == POINTER_DYNAMIC) delete data[i];
      }
      ArrayResize(data, 0);
    }
    
    T replace(const uint i, T v)
    {
      uint n = ArraySize(data);
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
