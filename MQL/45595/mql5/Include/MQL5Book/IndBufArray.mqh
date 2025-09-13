//+------------------------------------------------------------------+
//|                                                  IndBufArray.mqh |
//|                               Copyright (c) 2016-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

// helper defines which allows to not use read-accessors
// by selecting proper operator[] overloads via SETTER/GETTER type
#define SETTER int
#define GETTER uint

class BufferGetter;            // forward declaration, helper class

//+------------------------------------------------------------------+
//| Indicator buffer holder                                          |
//+------------------------------------------------------------------+
class Buffer                   // main indicator buffer class
{
protected:
   double buffer[];           // indicator buffer
    
   int cursor;                // position in the buffer
    
   BufferGetter *instance;    // helper object for reading values (optional)
    
public:
   Buffer(int i,
      const double empty = EMPTY_VALUE,
      const ENUM_INDEXBUFFER_TYPE type = INDICATOR_DATA,
      const bool asSeries = false)
   {
      SetIndexBuffer(i, buffer, type);
      ArraySetAsSeries(buffer, asSeries);
      ArrayInitialize(buffer, empty);
      instance = NULL;
   }
    
   virtual ~Buffer()
   {
      if(CheckPointer(instance) == POINTER_DYNAMIC) delete instance;
   }
    
   void series(const bool s)
   {
      ArraySetAsSeries(buffer, s);
   }
    
   void empty(const double empty = EMPTY_VALUE, const int offset = 0, const int count = 0)
   {
      if(offset == 0 && count == 0)
      {
         ArrayInitialize(buffer, empty);
      }
      else
      {
         ArrayFill(buffer, offset, count, empty);
      }
   }

   double operator[](GETTER b)
   {
      return buffer[b];
   }
    
   Buffer *operator[](SETTER b)
   {
      cursor = (int)b;
      return &this;
   }
   
   // writes data from the 'source' into this buffer
   int write(const double &source[], const int from = 0, const int to = 0, const int count = WHOLE_ARRAY)
   {
      ResetLastError();
      return ArrayCopy(buffer, source, to, from, count);
   }
   
   // reads data from this buffer to the given 'target'
   int read(double &target[], const int from = 0, const int to = 0, const int count = WHOLE_ARRAY)
   {
      ResetLastError();
      return ArrayCopy(target, buffer, to, from, count);
   }
   
   int copy(const int handle, const int bus, const int from, const int count)
   {
      ResetLastError();
      return CopyBuffer(handle, bus, from, count, buffer);
   }
    
   double operator=(double x)
   {
      buffer[cursor] = x;
      return x;
   }
    
   void set(const int b, const double v)
   {
      buffer[b] = v;
   }
    
   void set(const int b, const double &array[])
   {
      for(int i = 0; i < ArraySize(array); i++)
      {
         buffer[b + i] = array[i];
      }
   }
    
   BufferGetter *edit()
   {
      if(instance == NULL) instance = new BufferGetter(this);
      return instance;
   }
    
   double operator+(double x) const
   {
      return buffer[cursor] + x;
   }
    
   double operator-(double x) const
   {
      return buffer[cursor] - x;
   }
    
   double operator*(double x) const
   {
      return buffer[cursor] * x;
   }
    
   double operator/(double x) const
   {
      return buffer[cursor] / x;
   }

   double operator+=(double x)
   {
      buffer[cursor] += x;
      return buffer[cursor];
   }
    
   double operator-=(double x)
   {
      buffer[cursor] -= x;
      return buffer[cursor];
   }
    
   double operator*=(double x)
   {
      buffer[cursor] *= x;
      return buffer[cursor];
   }
    
   double operator/=(double x)
   {
      buffer[cursor] /= x;
      return buffer[cursor];
   }
};

//+------------------------------------------------------------------+
//| Indicator buffer read-accessor via operator[](int) overload      |
//+------------------------------------------------------------------+
class BufferGetter // helper class to access buffer values 'directly'
{
private:
   Buffer *owner;
   int cursor;
    
public:
   BufferGetter(Buffer &o)
   {
      owner = &o;
   }
    
   double operator[](int b)
   {
      return owner[(GETTER)b];
   }
};

//+------------------------------------------------------------------+
//| Manager class for array of indicator buffer holders              |
//+------------------------------------------------------------------+
class BufferArray
{
protected:
   Buffer *array[];
    
public:
   BufferArray() { }
    
   void add(const int m = 1,
      const ENUM_INDEXBUFFER_TYPE type = INDICATOR_DATA,
      const double empty = EMPTY_VALUE,
      const bool asSeries = false)
   {
      const int n = ArraySize(array);
      ArrayResize(array, n + m);
      for(int i = 0; i < m; ++i)
      {
         array[n + i] = new Buffer(n + i, empty, type, asSeries);
      }
   }
    
   BufferArray(const int n, const bool asSeries = false, const double empty = EMPTY_VALUE)
   {
      ArrayResize(array, n);
      for(int i = 0; i < n; ++i)
      {
         array[i] = new Buffer(i, empty, INDICATOR_DATA, asSeries);
      }
   }

   BufferArray(const int n,
      const ENUM_INDEXBUFFER_TYPE &types[],
      const double empty = EMPTY_VALUE,
      const bool asSeries = false)
   {
      ArrayResize(array, n);
      for(int i = 0; i < n; ++i)
      {
         array[i] = new Buffer(i, empty, types[i], asSeries);
      }
   }
    
   virtual ~BufferArray()
   {
      const int n = ArraySize(array);
      for(int i = 0; i < n; ++i)
      {
         if(CheckPointer(array[i]) == POINTER_DYNAMIC)
         {
            delete array[i];
         }
      }
      ArrayResize(array, 0);
   }
    
   Buffer *operator[](int n) const
   {
      if(n >= ArraySize(array))
      {
         Print("OOB:", n , " >= ", ArraySize(array));
      }
      return array[n];
   }
    
   int size() const
   {
      return ArraySize(array);
   }
    
   void empty(const double empty = EMPTY_VALUE)
   {
      const int n = ArraySize(array);
      for(int i = 0; i < n; ++i)
      {
         array[i].empty(empty);
      }
   }
};

//+------------------------------------------------------------------+
//| Read-accessor for array of indicator buffer holders/accessors    |
//+------------------------------------------------------------------+
class BufferArrayGetter
{
private:
   BufferGetter *array[];
    
public:
   BufferArrayGetter(){};
    
   BufferArrayGetter(const BufferArray &a)
   {
      bind(a);
   }
    
   void bind(const BufferArray &a)
   {
      int n = a.size();
      ArrayResize(array, n);
      for(int i = 0; i < n; ++i)
      {
         array[i] = a[i].edit();
      }
   }
    
   BufferGetter *operator[](int n) const
   {
      return array[n];
   }
    
   virtual ~BufferArrayGetter()
   {
      ArrayResize(array, 0);
   }
};
//+------------------------------------------------------------------+
