//+------------------------------------------------------------------+
//|                                                       Layout.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                                  GUI Layout declarative language |
//|                           https://www.mql5.com/ru/articles/7734/ |
//|                           https://www.mql5.com/ru/articles/7795/ |
//+------------------------------------------------------------------+

#include <Marketeer/RubbArray.mqh>
#include "Generators.mqh"

union PackedRect
{
  const ulong compound;
  const ushort parts[4];
  PackedRect(const ulong value): compound(value) {}
  PackedRect(const ushort left, const ushort top, const ushort right, const ushort bottom): compound(left | ((ulong)top << 16) | ((ulong)right << 32) | ((ulong)bottom << 48)) {}
  PackedRect(const CRect &r): compound(r.left | ((ulong)r.top << 16) | ((ulong)r.right << 32) | ((ulong)r.bottom << 48)) {}
};

template<typename T>
class ControlProperties
{
  protected:
    T *object;
    string context;

  public:
    ControlProperties(): object(NULL), context(NULL) {}
    ControlProperties(T *ptr): object(ptr), context(NULL) {}
    void assign(T *ptr) { object = ptr; }
    T *get(void) { return object; }
    virtual ControlProperties<T> *operator[](const string property) { context = property; StringToLower(context); return &this; };
    virtual T *operator<=(const bool b) = 0;
    virtual T *operator<=(const ENUM_ALIGN_MODE align) = 0;
    virtual T *operator<=(const color c) = 0;
    virtual T *operator<=(const string s) = 0;
    virtual T *operator<=(const int i) = 0;
    virtual T *operator<=(const long l) = 0;
    virtual T *operator<=(const double d) = 0;
    virtual T *operator<=(const float f) = 0;
    virtual T *operator<=(const datetime d) = 0;
    virtual T *operator<=(const PackedRect &r) = 0;
    // TODO: ushort for margins?
};


class LayoutData
{
  protected:
    static RubbArray<LayoutData *> stack;
    static string rootId;
    int _x1, _y1, _x2, _y2;
    string _id;
  
  public:
    LayoutData()
    {
      _x1 = _y1 = _x2 = _y2 = 0;
      _id = NULL;
    }
};

enum STYLER_PHASE
{
  STYLE_PHASE_BEFORE_INIT,
  STYLE_PHASE_AFTER_INIT
};


template<typename C>
class LayoutStyleable
{
  public:
    virtual void apply(C *control, const STYLER_PHASE phase) {};
};


template<typename C>
class LayoutCache
{
  protected:
    C *cache[];   // autocreated controls and boxes

  public:
    ~LayoutCache()
    {
      for(int i = 0; i < ArraySize(cache); i++)
      {
        if(CheckPointer(cache[i]) == POINTER_DYNAMIC) delete cache[i];
      }
    }

    virtual LayoutStyleable<C> *getStyler() const
    {
      return NULL;
    }

    virtual void save(C *control)
    {
      const int n = ArraySize(cache);
      ArrayResize(cache, n + 1);
      cache[n] = control;
    }

    virtual C *get(const string name) = 0;

    virtual C *get(const long m)
    {
      if(m < 0 || m >= ArraySize(cache)) return NULL;
      return cache[(int)m];
    }

    virtual bool find(C *control, const int excludeLast = 0) const
    {
      for(int i = 0; i < ArraySize(cache) - excludeLast; i++) // excluding just added (latest element)
      {
        if(cache[i] == control)
        {
          return true;
        }
      }
      return false;
    }

    virtual int indexOf(C *control)
    {
      for(int i = 0; i < ArraySize(cache); i++)
      {
        if(cache[i] == control)
        {
          return i;
        }
      }
      return -1;
    }

    virtual C *findParent(C *control) const = 0;

    virtual bool revoke(C *control) = 0;
    
    virtual int cacheSize()
    {
      return ArraySize(cache);
    }
    
    virtual bool onEvent(const int event, C *control)
    {
      return false;
    }
};


template<typename P,typename C>
class LayoutBase: public LayoutData
{
  protected:
    P *container; // not null if container (can be used as flag)
    C *object;
    C *array[];
    int shadow;   // set to 1 during creation (when last cached object not yet registered)
    
    static LayoutCache<C> *cacher;

  public:
    LayoutBase(): container(NULL), object(NULL), shadow(0) {}

    C *get()
    {
      return object;
    }

    C *operator[](const int i = 0) const
    {
      if(object != NULL) return object;
      if(i < 0 || i >= ArraySize(array)) return NULL;
      return array[i];
    }

    int size() const
    {
      return ArraySize(array);
    }

    static void setCache(LayoutCache<C> *c)
    {
      cacher = c;
    }
  
  protected:
    virtual bool setContainer(C *control) = 0;

    virtual string create(C *object, const string id = NULL) = 0;
    virtual void add(C *object) = 0;
    virtual string getRootId(const string id) = 0;

    virtual bool save(C *control)
    {
      if(cacher == NULL)
      {
        Print("Before first implicit layout object created please assign a LayoutCache storage");
        return false;
      }
      cacher.save(control);
      return true;
    }
    
    // nonbound layout, control T is implicitly stored in internal cache
    template<typename T>
    T *init(const string name, const int m = 1, const int x1 = 0, const int y1 = 0, const int x2 = 0, const int y2 = 0)
    {
      if(m > 1)
      {
        ArrayResize(array, m);
        object = NULL;
        container = NULL;
      }
      T *temp = NULL;
      for(int i = 0; i < m; i++)
      {
        temp = new T();
        if(save(temp))
        {
          if(m > 1) array[i] = temp;
          shadow = 1;
          init(temp, name + (m > 1 ? (string)(i + 1) : ""), x1, y1, x2, y2);
          shadow = 0;
        }
        else return NULL;
      }
      return temp;
    }
    
    // bound layout, with explicit control object passed as reference/pointer
    template<typename T>
    void init(T *ref, const string id = NULL, const int x1 = 0, const int y1 = 0, const int x2 = 0, const int y2 = 0)
    {
      if(ArraySize(array) == 0)
      {
        setContainer(ref);
        object = ref;
      }
      _x1 = x1;
      _y1 = y1;
      _x2 = x2;
      _y2 = y2;
      if(stack.size() > 0)
      {
        if(_x1 == 0 && _y1 == 0 && _x2 == 0 && _y2 == 0)
        {
          _x1 = stack.top()._x1;
          _y1 = stack.top()._y1;
          _x2 = stack.top()._x2;
          _y2 = stack.top()._y2;
        }

        _id = rootId + (id == NULL ? typename(T) + StringFormat("%d", ref) : id);
      }
      else
      {
        _id = (id == NULL ? typename(T) + StringFormat("%d", ref) : id);
      }
      
      bool existing = false;
      
      if(cacher != NULL && cacher.find(ref, shadow))
      {
        // this object exists in the cache, no need to create again
        // can be a dynamic modification of the dialog
        existing = true;
      }
      
      // FIXME: this is a hack because rootId behaviour is implementation specific for standard library
      
      // this 'if' is used for the case, when a blank form was created on start
      // (it's not in the cache since it's automatic (it's a rule so far)), and then
      // new elements are added ad-hoc, so the form re-creation should be skipped,
      // but it's needed on the stack
      else if(rootId == _id) // the dialog already created
      {
        existing = true;
      }
      else // normal workflow branch
      {
        _id = create(ref);
      }
      
      if(stack.size() == 0)
      {
        rootId = getRootId(_id);
      }
      if(container)
      {
        stack << &this;
      }

      if(cacher != NULL && !existing)
      {
        LayoutStyleable<C> *styler = cacher.getStyler();
        if(styler != NULL)
        {
          styler.apply(ref, STYLE_PHASE_BEFORE_INIT);
        }
      }
      
      if(ArraySize(array) > 0)
      {
        LayoutBase *up = stack.size() > 0 ? stack.top() : NULL;
        if(up != NULL && up.container != NULL)
        {
          up.add(ref);
        }
      }
    }
    
    // array of explicitly defined controls of type T, bound to the single layout object
    // only simple controls (not containers) are allowed to init via arrays
    template<typename T>
    void init(T &refs[], const string id = NULL, const int x1 = 0, const int y1 = 0, const int x2 = 0, const int y2 = 0)
    {
      object = NULL;
      container = NULL;
      _x1 = x1;
      _y1 = y1;
      _x2 = x2;
      _y2 = y2;
      if(stack.size() > 0)
      {
        if(_x1 == 0 && _y1 == 0 && _x2 == 0 && _y2 == 0)
        {
          _x1 = stack.top()._x1;
          _y1 = stack.top()._y1;
          _x2 = stack.top()._x2;
          _y2 = stack.top()._y2;
        }
        _id = rootId + (id == NULL ? typename(T) + StringFormat("%d", &refs[0]) : id) + "_";
      }
      else
      {
        _id = (id == NULL ? typename(T) + StringFormat("%d", &refs[0]) : id) + "_";
      }
      
      LayoutStyleable<C> *styler = cacher != NULL ? cacher.getStyler() : NULL;
      
      int size = ArraySize(refs);
      ArrayResize(array, size);
      LayoutBase *up = stack.size() > 0 ? stack.top() : NULL;
      for(int i = 0; i < size; i++)
      {
        create(&refs[i], _id + (string)(i + 1));

        if(styler != NULL)
        {
          styler.apply(&refs[i], STYLE_PHASE_BEFORE_INIT);
        }

        if(up != NULL && up.container != NULL)
        {
          up.add(&refs[i]);
        }

        array[i] = &refs[i];
      }
    }
    
    ~LayoutBase()
    {
      if(container)
      {
        stack.pop();
      }
      
      if(object)
      {
        // FIXME: should not call styler for "old" objects
        if(cacher != NULL)
        {
          LayoutStyleable<C> *styler = cacher.getStyler();
          if(styler != NULL)
          {
            styler.apply(object, STYLE_PHASE_AFTER_INIT);
          }
        }

        LayoutBase *up = stack.size() > 0 ? stack.top() : NULL;
        if(up != NULL && up.container != NULL)
        {
          up.add(object);
        }
      }
      
      if(ArraySize(array) > 0 && cacher != NULL)
      {
        LayoutStyleable<C> *styler = cacher.getStyler();
        for(int i = 0; i < ArraySize(array); i++)
        {
          styler.apply(array[i], STYLE_PHASE_AFTER_INIT);
        }
      }
      
      if(stack.size() == 0 && cacher != NULL)
      {
        cacher = NULL;
      }
    }

    template<typename V>
    LayoutBase<P,C> *operator<=(const V value) // template function cannot be virtual
    {
      Print("Please, override " , __FUNCSIG__, " in your concrete Layout class");
      return &this;
    }
    
    virtual LayoutBase<P,C> *operator<=(const PackedRect &r) // optional
    {
      Print("Please, override " , __FUNCSIG__, " in your concrete Layout class");
      return &this;
    }

    virtual LayoutBase<P,C> *operator[](const string prop) // optional
    {
      Print("Please, override " , __FUNCSIG__, " in your concrete Layout class");
      return &this;
    }
};

template<typename P,typename C>
static LayoutCache<C> *LayoutBase::cacher = NULL;


static RubbArray<LayoutData *> LayoutData::stack;
static string LayoutData::rootId;

