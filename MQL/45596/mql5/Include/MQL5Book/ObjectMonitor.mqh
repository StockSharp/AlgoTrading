//+------------------------------------------------------------------+
//|                                                ObjectMonitor.mqh |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#include <MQL5Book/MapArray.mqh>
#include <MQL5Book/AutoPtr.mqh>
#include <MQL5Book/PRTF.mqh>

// macros for calling methods on array of objects
#define CALL_ALL(A,M) for(int i = 0, size = ArraySize(A); i < size; ++i) A[i][].M
#define CALL_PUT_ALL(A,P,M) for(int i = 0, size = ArraySize(A); i < size; ++i) P(A[i][].M)

// macros for properties with modifier
#define MOD_MAX 10
#define MOD_COMBINE(V,I) (V | (I << 24))
#define MOD_GET_NAME(V)  (V & 0xFFFFFF)
#define MOD_GET_INDEX(V) (V >> 24)

//+------------------------------------------------------------------+
//| Abstract class for reading/writing object properties             |
//+------------------------------------------------------------------+
class ObjectProxy
{
public:
   long get(const ENUM_OBJECT_PROPERTY_INTEGER property, const int modifier = 0)
   {
      return ObjectGetInteger(chart(), name(), property, modifier);
   }

   double get(const ENUM_OBJECT_PROPERTY_DOUBLE property, const int modifier = 0)
   {
      return ObjectGetDouble(chart(), name(), property, modifier);
   }

   string get(const ENUM_OBJECT_PROPERTY_STRING property, const int modifier = 0)
   {
      return ObjectGetString(chart(), name(), property, modifier);
   }

   ObjectProxy *set(const ENUM_OBJECT_PROPERTY_INTEGER property, const long value, const int modifier = 0)
   {
      ObjectSetInteger(chart(), name(), property, modifier, value);
      return &this;
   }

   ObjectProxy *set(const ENUM_OBJECT_PROPERTY_DOUBLE property, const double value, const int modifier = 0)
   {
      ObjectSetDouble(chart(), name(), property, modifier, value);
      return &this;
   }

   ObjectProxy *set(const ENUM_OBJECT_PROPERTY_STRING property, const string value, const int modifier = 0)
   {
      ObjectSetString(chart(), name(), property, modifier, value);
      return &this;
   }
   
   virtual string name() = 0;
   virtual void name(const string) { }
   virtual long chart() { return 0; }
   virtual void chart(const long) { }
};

//+------------------------------------------------------------------+
//| Simple class for reading/writing object properties               |
//+------------------------------------------------------------------+
class ObjectSelector: public ObjectProxy
{
protected:
   long host; // chart id
   string id; // object id on specified chart
public:
   ObjectSelector(const string _id, const long _chart = 0): id(_id), host(_chart) { }
   
   virtual string name() override
   {
      return id;
   }
   virtual void name(const string _id) override
   {
      id = _id;
   }
   virtual void chart(const long _chart) override
   {
      host = _chart;
   }
};

//+------------------------------------------------------------------+
//| Abstract class for taking snapshots of object properties         |
//+------------------------------------------------------------------+
class ObjectMonitorInterface: public ObjectSelector
{
public:
   ObjectMonitorInterface(const string _id, const long _chart = 0): ObjectSelector(_id, _chart) { }
   virtual int snapshot() = 0;
   virtual void print() { };
   virtual int backup() { return 0; }
   virtual void restore() { }
   virtual void applyChanges(ObjectMonitorInterface *reference) { }
};

//+------------------------------------------------------------------+
//| Worker class for reading a predefined set of object properties   |
//| of specific <E>num type and value <T>ype                         |
//+------------------------------------------------------------------+
template<typename T,typename E>
class ObjectMonitorBase: public ObjectMonitorInterface
{
protected:
   MapArray<E,T> data;  // array of [property,value] pairs
   MapArray<E,T> store; // backup (filled upon request)
   MapArray<E,T> change;
   
   // check if the given value corresponds to an element in enumeration E,
   // and if so - add it to the data array
   bool detect(const int v, const int levels)
   {
      // some properties support modifiers which means multiple values
      static const int modifiables[] =
      {
         OBJPROP_TIME,
         OBJPROP_PRICE,
         OBJPROP_LEVELVALUE,
         OBJPROP_LEVELTEXT,

         // NB: these properties do not throw errors on level index overflow
         OBJPROP_LEVELCOLOR,
         OBJPROP_LEVELSTYLE,
         OBJPROP_LEVELWIDTH,
         OBJPROP_BMPFILE,
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
         
         int k = 1;
         // for modifiables set number of levels, if provided, or a suitable value
         if(modifiable)
         {
            if(levels > 0) k = levels;
            else if(v == OBJPROP_TIME || v == OBJPROP_PRICE) k = MOD_MAX;
            else if(v == OBJPROP_BMPFILE) k = 2;
         }
         
         // read the property value - a single one or many levels
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
   ObjectMonitorBase(const string _id, const int &flags[]): ObjectMonitorInterface(_id)
   {
      const int levels = (int)ObjectGetInteger(0, id, OBJPROP_LEVELS);
      for(int i = 0; i < ArraySize(flags); ++i)
      {
         detect(flags[i], levels);
      }
   }
   
   virtual int snapshot() override
   {
      MapArray<E,T> temp;
      change.reset();
      
      // collect new state
      for(int i = 0; i < data.getSize(); ++i)
      {
         const E e = (E)MOD_GET_NAME(data.getKey(i));
         const int m = MOD_GET_INDEX(data.getKey(i));
         temp.put((E)data.getKey(i), get(e, m));
      }
      
      int changes = 0;
      // compare previous and current state
      for(int i = 0; i < data.getSize(); ++i)
      {
         if(data[i] != temp[i])
         {
            if(changes == 0) Print(id);
            const E e = (E)MOD_GET_NAME(data.getKey(i));
            const int m = MOD_GET_INDEX(data.getKey(i));
            Print(EnumToString(e), (m > 0 ? (string)m : ""), " ", data[i], " -> ", temp[i]);
            change.put(data.getKey(i), temp[i]);
            changes++;
         }
      }
      
      // save new state
      data = temp;
      return changes;
   }
   
   MapArray<E,T> * const getChanges()
   {
      return &change;
   }
   
   virtual void applyChanges(ObjectMonitorInterface *intf) override
   {
      ObjectMonitorBase *reference = dynamic_cast<ObjectMonitorBase<T,E> *>(intf);
      if(reference)
      {
         MapArray<E,T> *event = reference.getChanges();
         if(event.getSize() > 0)
         {
            Print("Modifing ", id, " by ", event.getSize(), " changes");
            // event.print(); // uncomment to debug
            for(int i = 0; i < event.getSize(); ++i)
            {
               data.put(event.getKey(i), event[i]);
               const E e = (E)MOD_GET_NAME(event.getKey(i));
               const int m = MOD_GET_INDEX(event.getKey(i));
               Print(EnumToString(e), " ", m, " ", event[i]);
               set(e, event[i], m);
            }
         }
      }
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
//| Combined monitor for object properties of all types              |
//+------------------------------------------------------------------+
class ObjectMonitor: public ObjectMonitorInterface
{
protected:
   AutoPtr<ObjectMonitorInterface> m[3];
   
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
   
   ObjectMonitorInterface *getBase(const int i)
   {
      return m[i][];
   }
   
public:
   ObjectMonitor(const string objid, const int &flags[]): ObjectMonitorInterface(objid)
   {
      m[0] = new ObjectMonitorBase<long,ENUM_OBJECT_PROPERTY_INTEGER>(objid, flags);
      m[1] = new ObjectMonitorBase<double,ENUM_OBJECT_PROPERTY_DOUBLE>(objid, flags);
      m[2] = new ObjectMonitorBase<string,ENUM_OBJECT_PROPERTY_STRING>(objid, flags);
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
   
   virtual string name() override
   {
      return m[0][].name();
   }

   virtual void name(const string objid) override
   {
      m[0][].name(objid);
      m[1][].name(objid);
      m[2][].name(objid);
   }
   
   virtual void applyChanges(ObjectMonitorInterface *intf) override
   {
      ObjectMonitor *monitor = dynamic_cast<ObjectMonitor *>(intf);
      if(monitor)
      {
         m[0][].applyChanges(monitor.getBase(0));
         m[1][].applyChanges(monitor.getBase(1));
         m[2][].applyChanges(monitor.getBase(2));
      }
   }
};
//+------------------------------------------------------------------+
