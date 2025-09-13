//+------------------------------------------------------------------+
//|                                         ChartModeMonitorFull.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/AutoPtr.mqh>

#define CALL_ALL(A,M) for(int i = 0, size = ArraySize(A); i < size; ++i) A[i][].M
#define CALL_PUT_ALL(A,P,M) for(int i = 0, size = ArraySize(A); i < size; ++i) P(A[i][].M)

#define MOD_MAX 100      // subwindow number
#define MOD_COMBINE(V,I) (V | (I << 24))
#define MOD_GET_NAME(V)  (V & 0xFFFFFF)
#define MOD_GET_INDEX(V) (V >> 24)

//+------------------------------------------------------------------+
//| Abstract class for reading chart properties                      |
//+------------------------------------------------------------------+
class ChartModeMonitorInterface
{
public:
   long get(const ENUM_CHART_PROPERTY_INTEGER property, const int window = 0)
   {
      return ChartGetInteger(0, property, window);
   }

   double get(const ENUM_CHART_PROPERTY_DOUBLE property, const int window = 0)
   {
      return ChartGetDouble(0, property, window);
   }

   string get(const ENUM_CHART_PROPERTY_STRING property, const int window = 0)
   {
      return ChartGetString(0, property);
   }

   bool set(const ENUM_CHART_PROPERTY_INTEGER property, const long value, const int window = 0)
   {
      // this property should be protected, because it's handled by MQL5 non-gracefully:
      // it's used for actual window height while reading,
      // but sets window to fixed height mode while writing,
      // and there are no means to detect or switch off the fixed mode
      // except for writing/editing/applying tpl-file directly
      if(property == CHART_HEIGHT_IN_PIXELS)
      {
         return false;
      }
      return ChartSetInteger(0, property, window, value);
   }

   bool set(const ENUM_CHART_PROPERTY_DOUBLE property, const double value, const int window = 0)
   {
      return ChartSetDouble(0, property, value);
   }

   bool set(const ENUM_CHART_PROPERTY_STRING property, const string value, const int window = 0)
   {
      return ChartSetString(0, property, value);
   }
   
   virtual int snapshot() = 0;
   virtual void print() { };
   virtual int backup() { return 0; }
   virtual void restore() { }
};

//+------------------------------------------------------------------+
//| Worker class for reading a predefined set of chart properties of |
//| specific <E>num type and value <T>ype                            |
//+------------------------------------------------------------------+
template<typename T,typename E>
class ChartModeMonitorBase: public ChartModeMonitorInterface
{
protected:
   MapArray<E,T> data;  // array of [property,value] pairs
   MapArray<E,T> store; // backup (filled upon request)
   
   // check if the given value corresponds to an element in enumeration E,
   // and if so - add it to the data array
   bool detect(const int v, const int windows)
   {
      // some properties support modifiers which means multiple values,
      // but others do not throw errors on window index overflow
      static const int modifiables[] =
      {
         CHART_WINDOW_IS_VISIBLE,
         CHART_WINDOW_YDISTANCE,
         CHART_HEIGHT_IN_PIXELS,
         CHART_PRICE_MIN,
         CHART_PRICE_MAX,
      };

      bool result = false;
      ResetLastError();
      const string s = EnumToString((E)v); // resulting string is not used
      if(_LastError == 0) // only error code is important
      {
         bool modifiable = false;
         for(int i = 0; i < ArraySize(modifiables); ++i)
         {
            if(v == modifiables[i])
            {
               modifiable = true;
               break;
            }
         }
         
         int k = modifiable ? windows : 1;

         for(int i = 0; i < k; ++i)
         {
            ResetLastError();
            T temp = get((E)v, i);
            if(_LastError != 0) break;
            data.put((E)MOD_COMBINE(v, i), temp);
            result = true;
         }
      }
      return result;
   }
public:
   ChartModeMonitorBase(const int &flags[])
   {
      const int windows = (int)ChartGetInteger(0, CHART_WINDOWS_TOTAL);
      for(int i = 0; i < ArraySize(flags); ++i)
      {
         detect(flags[i], windows);
      }
   }
   
   virtual int snapshot() override
   {
      MapArray<E,T> temp;
      // collect new state
      for(int i = 0; i < data.getSize(); ++i)
      {
         const E e = (E)MOD_GET_NAME(data.getKey(i));
         const int m = MOD_GET_INDEX(data.getKey(i));
         temp.put(data.getKey(i), get(e, m));
      }
      
      int changes = 0;
      // compare previous and current state
      // NB: added or removed elements are not considered yet
      for(int i = 0; i < data.getSize(); ++i)
      {
         if(data[i] != temp[i])
         {
            const E e = (E)MOD_GET_NAME(data.getKey(i));
            const int m = MOD_GET_INDEX(data.getKey(i));
            Print(EnumToString(e), (m > 0 ? (string)m : ""), " ", data[i], " -> ", temp[i]);
            changes++;
         }
      }
      
      // save new state
      data = temp;
      return changes;
   }
   
   virtual void print() override
   {
      Print(typename(E));
      MapArray<string,T> logger;

      for(int i = 0; i < data.getSize(); ++i)
      {
         const E e = (E)MOD_GET_NAME(data.getKey(i));
         const int m = MOD_GET_INDEX(data.getKey(i));
         logger.put(EnumToString(e) + (m > 0 ? (string)m : ""), data[i]);
      }
      logger.print();
   }

   virtual int backup() override
   {
      store = data;
      return store.getSize();
   }
   
   virtual void restore() override
   {
      data = store;
      for(int i = 0; i < data.getSize(); ++i)
      {
         const E e = (E)MOD_GET_NAME(data.getKey(i));
         const int m = MOD_GET_INDEX(data.getKey(i));
         set(e, data[i], m);
      }
   }
};

//+------------------------------------------------------------------+
//| Combined monitor for chart properties of all types               |
//+------------------------------------------------------------------+
class ChartModeMonitor: public ChartModeMonitorInterface
{
   AutoPtr<ChartModeMonitorInterface> m[3];
   
   template<typename T>
   struct Sum
   {
      T result;
      Sum(): result(0) { }
      void add(const T v)
      {
         result += v;
      }
   };
   
public:
   ChartModeMonitor(const int &flags[])
   {
      m[0] = new ChartModeMonitorBase<long,ENUM_CHART_PROPERTY_INTEGER>(flags);
      m[1] = new ChartModeMonitorBase<double,ENUM_CHART_PROPERTY_DOUBLE>(flags);
      m[2] = new ChartModeMonitorBase<string,ENUM_CHART_PROPERTY_STRING>(flags);
   }
   
   virtual int snapshot() override
   {
      Sum<int> sum;
      CALL_PUT_ALL(m, sum.add, snapshot());
      return sum.result;
   }
   
   virtual void print() override
   {
      CALL_ALL(m, print());
   }
   
   virtual int backup() override
   {
      Sum<int> sum;
      CALL_PUT_ALL(m, sum.add, backup());
      return sum.result;
   }
   
   virtual void restore() override
   {
      CALL_ALL(m, restore());
   }
};
//+------------------------------------------------------------------+
