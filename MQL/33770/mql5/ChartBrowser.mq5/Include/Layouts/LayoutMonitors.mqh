//+------------------------------------------------------------------+
//|                                               LayoutMonitors.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                                  GUI Layout declarative language |
//|                         (MQL5 standard library controls support) |
//|                           https://www.mql5.com/ru/articles/7795/ |
//+------------------------------------------------------------------+

class StateMonitor
{
  public:
    virtual void notify(void *sender) = 0;
};

class Publisher
{
  public:
    virtual void subscribe(StateMonitor *ptr) = 0;
    virtual void unsubscribe(StateMonitor *ptr) = 0;
};


template<typename V>
class ValuePublisher: public Publisher
{
  protected:
    string _rtti;
    V value;
    StateMonitor *dependencies[];

  public:
    ValuePublisher()
    {
      RTTI;
    }
    
    V operator~(void) const
    {
      return value;
    }

    void operator=(const V &v)
    {
      value = v;
      for(int i = 0; i < ArraySize(dependencies); i++)
      {
        dependencies[i].notify(&this);
      }
    }
    
    void operator=(V v)
    {
      value = v;
      for(int i = 0; i < ArraySize(dependencies); i++)
      {
        dependencies[i].notify(&this);
      }
    }

    virtual void subscribe(StateMonitor *ptr) override
    {
      const int n = ArraySize(dependencies);
      ArrayResize(dependencies, n + 1);
      dependencies[n] = ptr;
    }

    virtual void unsubscribe(StateMonitor *ptr) override
    {
      const int n = ArraySize(dependencies);
      bool found = false;
      for(int i = 0, j = 0; i < n; i++, j++)
      {
        if(i != j)
        {
          dependencies[j] = dependencies[i];
        }
        else
        if(dependencies[i] == ptr)
        {
          j--;
          found = true;
        }
      }
      if(found)
      {
        ArrayResize(dependencies, n - 1);
      }
    }
};


template<typename V>
class StdValue: public ValuePublisher<V>
{
  protected:
    CWnd *provider;

  public:
    StdValue()
    {
      RTTI;
    }
    
    void bind(CWnd *ptr)
    {
      provider = ptr;
    }
    
    CWnd *backlink() const
    {
      return provider;
    }
};

template<typename C>
class EnableStateMonitorBase: public StateMonitor
{
  protected:
    Publisher *sources[];
    C *control;

  public:
    EnableStateMonitorBase(): control(NULL) {}

    virtual void attach(C *c)
    {
      control = c;
      for(int i = 0; i < ArraySize(sources); i++)
      {
        if(control)
        {
          sources[i].subscribe(&this);
        }
        else
        {
          sources[i].unsubscribe(&this);
        }
      }
    }

    virtual bool isEnabled(void) = 0;
};

class EnableStateMonitor: public EnableStateMonitorBase<CWnd>
{
  public:
    EnableStateMonitor() {}

    void notify(void *sender) override
    {
      if(control)
      {
        if(isEnabled())
        {
          control.Enable();
        }
        else
        {
          control.Disable();
        }
      }
    }
};

template<typename C>
class Notifiable: public C
{
  public:
    virtual bool onEvent(const int event, void *parent) { return false; };
};

template<typename C,typename V>
class PlainTypeNotifiable: public Notifiable<C>
{
  public:
    virtual V value() = 0;
};

template<typename C, typename V>
class NotifiableProperty: public PlainTypeNotifiable<C,V>
{
  protected:
    StdValue<V> *property;

  public:
    NotifiableProperty()
    {
      RTTI;
    }

    void bind(StdValue<V> *prop)
    {
      property = prop;
      property.bind(&this);
      property = value();
    }
    
    virtual bool onEvent(const int event, void *parent) override
    {
      if(event == ON_CHANGE || event == ON_END_EDIT)
      {
        property = value();
        return true;
      }
      return false;
    };
};

