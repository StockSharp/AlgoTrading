//+------------------------------------------------------------------+
//|                                                 LayoutStdLib.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                                  GUI Layout declarative language |
//|                         (MQL5 standard library controls support) |
//|                           https://www.mql5.com/ru/articles/7734/ |
//|                           https://www.mql5.com/ru/articles/7795/ |
//+------------------------------------------------------------------+

#include <ControlsPlus/Wnd.mqh>
#include <ControlsPlus/WndObj.mqh>
#include <ControlsPlus/WndClient.mqh>
#include <ControlsPlus/WndContainer.mqh>
#include <ControlsPlus/Dialog.mqh>
#include <ControlsPlus/Button.mqh>
#include <ControlsPlus/Edit.mqh>
#include <ControlsPlus/SpinEdit.mqh>
#include <ControlsPlus/DatePicker.mqh>
#include <ControlsPlus/CheckBox.mqh>
#include <ControlsPlus/RadioButton.mqh>

#include "Box.mqh"
#include "Grid.mqh"

#include "Layout.mqh"


template<typename T>
class StdControlProperties: public ControlProperties<T>
{
  public:
    StdControlProperties(): ControlProperties() {}
    StdControlProperties(T *ptr): ControlProperties(ptr) {}

    // we need dynamic_cast throughout below, because control classes
    // in the standard library does not provide a set of common virtual methods
    // to assign specific properties for all of them (for example, readonly
    // is available for edit field only)
    virtual T *operator<=(const bool b) override
    {
      if(StringFind(context, "enable") > -1)
      {
        if(b) object.Enable();
        else  object.Disable();
      }
      else
      if(StringFind(context, "visible") > -1)
      {
        object.Visible(b);
      }
      else
      {
        CEdit *edit = dynamic_cast<CEdit *>(object);
        if(edit != NULL) edit.ReadOnly(b);
        
        CButton *button = dynamic_cast<CButton *>(object);
        if(button != NULL) button.Locking(b);
      }
      
      return object;
    }

    virtual T *operator<=(const ENUM_ALIGN_MODE align) override
    {
      CEdit *edit = dynamic_cast<CEdit *>(object);
      if(edit != NULL) edit.TextAlign(align);
      return object;
    }
    
    virtual T *operator<=(const color c) override
    {
      CWndObj *ctrl = dynamic_cast<CWndObj *>(object);
      if(ctrl != NULL)
      {
        if(StringFind(context, "background") > -1)
        {
          ctrl.ColorBackground(c);
        }
        else if(StringFind(context, "border") > -1)
        {
          ctrl.ColorBorder(c);
        }
        else // default
        {
          ctrl.Color(c);
        }
      }
      else
      {
        CWndClient *client = dynamic_cast<CWndClient *>(object);
        if(client != NULL)
        {
          if(StringFind(context, "border") > -1)
          {
            client.ColorBorder(c);
          }
          else
          {
            client.ColorBackground(c);
          }
        }
      }
      return object;
    }

    virtual T *operator<=(const string s) override
    {
      CWndObj *ctrl = dynamic_cast<CWndObj *>(object);
      if(ctrl != NULL)
      {
        if(StringFind(context, "font") > -1)
        {
          ctrl.Font(s);
        }
        else // default
        {
          ctrl.Text(s);
        }
      }
      else
      {
        CCheckBox *check = dynamic_cast<CCheckBox *>(object);
        if(check != NULL) check.Text(s);
        else
        {
          CRadioButton *radio = dynamic_cast<CRadioButton *>(object);
          if(radio != NULL) radio.Text(s);
        }
      }
      return object;
    }

    virtual T *operator<=(const int i) override
    {
      if(StringFind(context, "width") > -1)
      {
        object.Width(i);
      }
      else
      if(StringFind(context, "height") > -1)
      {
        object.Height(i);
      }
      else
      if(StringFind(context, "margin") > -1)
      {
        object.Margins(i, i, i, i);
      }
      else
      if(StringFind(context, "left") > -1)
      {
        CRect r = object.Margins();
        object.Margins(i, r.top, r.right, r.bottom);
      }
      else
      if(StringFind(context, "top") > -1)
      {
        CRect r = object.Margins();
        object.Margins(r.left, i, r.right, r.bottom);
      }
      else
      if(StringFind(context, "right") > -1)
      {
        CRect r = object.Margins();
        object.Margins(r.left, r.top, i, r.bottom);
      }
      else
      if(StringFind(context, "bottom") > -1)
      {
        CRect r = object.Margins();
        object.Margins(r.left, r.top, r.right, i);
      }
      else
      if(StringFind(context, "align") > -1)
      {
        object.Alignment(i);
      }
      else
      if(StringFind(context, "fontsize") > -1)
      {
        CWndObj *ctrl = dynamic_cast<CWndObj *>(object);
        if(ctrl != NULL)
        {
          ctrl.FontSize(i);
        }
      }
      else // default
      {
        CSpinEdit *spin = dynamic_cast<CSpinEdit *>(object);
        if(spin != NULL)
        {
          if(StringFind(context, "min") > -1)
          {
            spin.MinValue(i);
          }
          else
          if(StringFind(context, "max") > -1)
          {
            spin.MaxValue(i);
          }
          else
          {
            spin.Value(i);
          }
        }
        else
        {
          CComboBox *combo = dynamic_cast<CComboBox *>(object);
          if(combo != NULL)
          {
            combo.Select(i);
          }
        }
      }
      return object;
    }
    
    virtual T *operator<=(const datetime d) override
    {
      CDatePicker *date = dynamic_cast<CDatePicker *>(object);
      if(date != NULL)
      {
        date.Value(d);
      }
      return object;
    }

    virtual T *operator<=(const long l) override
    {
      if(StringFind(context, "zorder") > -1)
      {
        CWndObj *ctrl = dynamic_cast<CWndObj *>(object);
        if(ctrl != NULL)
        {
          ctrl.ZOrder(l);
        }
      }
      else
      {
        object.Id(l);
      }
      return object;
    }

    virtual T *operator<=(const float f) override
    {
      const int margin = (int)f;
      
      CGrid *grid = dynamic_cast<CGrid *>(object);
      if(grid != NULL)
      {
        if(StringFind(context, "left") > -1
        || StringFind(context, "right") > -1)
        {
          grid.HGap(margin);
        }
        else
        if(StringFind(context, "top") > -1
        || StringFind(context, "bottom") > -1)
        {
          grid.VGap(margin);
        }
        else
        {
          grid.HGap(margin);
          grid.VGap(margin);
        }
      }
      else
      {
        CBox *box = dynamic_cast<CBox *>(object);
        if(box != NULL)
        {
          if(StringFind(context, "left") > -1)
          {
            box.PaddingLeft(margin);
          }
          else
          if(StringFind(context, "top") > -1)
          {
            box.PaddingTop(margin);
          }
          else
          if(StringFind(context, "right") > -1)
          {
            box.PaddingRight(margin);
          }
          else
          if(StringFind(context, "bottom") > -1)
          {
            box.PaddingBottom(margin);
          }
          else
          {
            // wrap this container's content with padding
            // (don't use it side by side with standard alignment!)
            box.Padding(margin, margin, margin, margin);
          }
        }
      }

      return object;
    }

    virtual T *operator<=(const double d) override
    {
      const int margin = (int)d;

      // align this control inside its container
      object.Margins(margin, margin, margin, margin);

      return object;
    }

    virtual T *operator<=(const PackedRect &r) override
    {
      object.Margins(r.parts[0], r.parts[1], r.parts[2], r.parts[3]);
      return object;
    }
    
    virtual T *operator<=(const ENUM_WND_ALIGN_FLAGS align)
    {
      object.Alignment(align);
      return object;
    }
    
    virtual T *operator<=(const LAYOUT_STYLE style)
    {
      CBox *box = dynamic_cast<CBox *>(object);
      if(box != NULL)
      {
        box.LayoutStyle(style);
      }
      return object;
    }
    
    virtual T *operator<=(const VERTICAL_ALIGN v)
    {
      CBox *box = dynamic_cast<CBox *>(object);
      if(box != NULL)
      {
        box.VerticalAlign(v);
      }
      return object;
    }
    
    virtual T *operator<=(const HORIZONTAL_ALIGN h)
    {
      CBox *box = dynamic_cast<CBox *>(object);
      if(box != NULL)
      {
        box.HorizontalAlign(h);
      }
      return object;
    }
};


class StdLayoutStyleable: public LayoutStyleable<CWnd>
{
  public:
    virtual void apply(CWnd *control, const STYLER_PHASE phase) = 0;
};


// CWnd implementation specific
class StdLayoutCache: public LayoutCache<CWnd>
{
  protected:
  public:
    static bool StringEndsWith(const string text, const string suffix)
    {
      if(StringLen(text) == 0) return StringLen(suffix) == 0;
      const int pos = StringFind(text, suffix);
      return pos == 5 && pos == StringLen(text) - StringLen(suffix); // this relies on 5-digit instance id!
    }

    virtual CWnd *get(const string name) override
    {
      const int n = ArraySize(cache);
      for(int i = 0; i < n; i++)
      {
        if(StringEndsWith(cache[i].Name(), name)) return cache[i];
      }
      return NULL;
    }

    virtual CWnd *get(const long m) override
    {
      if(m < 0)
      {
        for(int i = 0; i < ArraySize(cache); i++)
        {
          if(cache[i].Id() == -m) return cache[i];
          CWndContainer *container = dynamic_cast<CWndContainer *>(cache[i]);
          if(container != NULL)
          {
            for(int j = 0; j < container.ControlsTotal(); j++)
            {
              if(container.Control(j).Id() == -m) return container.Control(j);
            }
          }
        }
        return NULL;
      }
      else if(m >= ArraySize(cache)) return NULL;
      return cache[(int)m];
    }

    virtual CWnd *findParent(CWnd *control) const override
    {
      for(int i = 0; i < ArraySize(cache); i++)
      {
        CWndContainer *container = dynamic_cast<CWndContainer *>(cache[i]);
        if(container != NULL)
        {
          for(int j = 0; j < container.ControlsTotal(); j++)
          {
            if(container.Control(j) == control)
            {
              return container;
            }
          }
        }
      }
      return NULL;
    }
    
    virtual bool revoke(CWnd *control) override
    {
      static CWnd dummy;
      for(int i = 0; i < ArraySize(cache); i++)
      {
        if(cache[i] == control)
        {
          CWndContainer *container = dynamic_cast<CWndContainer *>(control);
          if(container != NULL)
          {
            for(int j = 0; j < container.ControlsTotal(); j++)
            {
              revoke(container.Control(j));
            }
          }
          // do not delete objects here, since they belong to their respective
          // parent controls/windows and are deleted from there
          // if(CheckPointer(control) == POINTER_DYNAMIC) delete control;
          cache[i] = &dummy;
          return true;
        }
      }
      return false;
    }
    
    void print()
    {
      for(int i = 0; i < ArraySize(cache); i++)
      {
        CWnd *wnd = cache[i];
        Print(wnd._rtti, " / ", wnd.Name(), " ", wnd.Id(), " F:", wnd.StateFlags());
      }
    }
};


class StdLayoutBase: public LayoutBase<CWndContainer,CWnd>
{
  public:
    virtual string getRootId(const string id) override
    {
      return StringSubstr(id, 0, 5);
    }

    virtual bool setContainer(CWnd *control) override
    {
      CDialog *dialog = dynamic_cast<CDialog *>(control);
      CBox *box = dynamic_cast<CBox *>(control);
      if(dialog != NULL)
      {
        container = dialog;
      }
      else if(box != NULL)
      {
        container = box;
      }
      return true;
    }

    virtual string create(CWnd *child, const string id = NULL) override
    {
      child.Create(ChartID(), id != NULL ? id : _id, 0, _x1, _y1, _x2, _y2);
      if(cacher != NULL)
      {
        child.Id(cacher.cacheSize() - 1);
      }
      return child.Name();
    }

    virtual void add(CWnd *child) override
    {
      CDialog *dlg = dynamic_cast<CDialog *>(container);
      if(dlg != NULL)
      {
        dlg.Add(child);
      }
      else
      {
        CWndContainer *ptr = dynamic_cast<CWndContainer *>(container);
        if(ptr != NULL)
        {
          ptr.Add(child);
        }
        else
        {
          Print("Can't add ", child.Name(), " to ", container.Name());
        }
      }
    }

    ~StdLayoutBase()
    {
    }
};

template<typename T>
class _layout: public StdLayoutBase
{
  protected:
    StdControlProperties<T> wrapper;

  public:
    _layout(const string id)
    {
      T *ptr = init<T>(id);
      wrapper.assign(ptr);
      wrapper <= id;
    }

    _layout(const string id, const int n)
    {
      T *ptr = init<T>(id, n, 0, 0, 0, 0);
    }

    _layout(const string id, const int dx, const int dy)
    {
      T *ptr = init<T>(id, 1, 0, 0, dx, dy);
      wrapper.assign(ptr);
      wrapper <= id;
    }

    _layout(const string id, const int x1, const int y1, const int x2, const int y2)
    {
      T *ptr = init<T>(id, 1, x1, y1, x2, y2);
      wrapper.assign(ptr);
      wrapper <= id;
    }

    template<typename V>
    _layout(const string id, const int dx, const int dy, const V value)
    {
      T *ptr = init<T>(id, 1, 0, 0, dx, dy);
      wrapper.assign(ptr);
      wrapper <= value;
    }
    
    _layout(T &ref, const string id = NULL)
    {
      init(&ref, id, 0, 0, 0, 0);
      wrapper.assign(&ref);
      if(id != NULL) wrapper <= id;
    }

    _layout(T *ptr, const string id = NULL)
    {
      init(ptr, id, 0, 0, 0, 0);
      wrapper.assign(ptr);
      if(id != NULL) wrapper <= id;
    }

    template<typename V>
    _layout(T &ref, const string id, const int dx, const int dy, const V value)
    {
      init(&ref, id, 0, 0, dx, dy);
      wrapper.assign(&ref);
      wrapper <= value;
    }

    _layout(T &ref, const string id, const int dx, const int dy)
    {
      init(&ref, id, 0, 0, dx, dy);
      wrapper.assign(&ref);
      wrapper <= id;
    }

    _layout(T *ptr, const string id, const int dx, const int dy)
    {
      init(ptr, id, 0, 0, dx, dy);
      wrapper.assign(ptr);
      wrapper <= id;
    }

    _layout(T &ref, const string id, const int x1, const int y1, const int x2, const int y2)
    {
      init(&ref, id, x1, y1, x2, y2);
      wrapper.assign(&ref);
      wrapper <= id;
    }
    
    _layout(T *ptr, const string id, const int x1, const int y1, const int x2, const int y2)
    {
      init(ptr, id, x1, y1, x2, y2);
      wrapper.assign(ptr);
      wrapper <= id;
    }

    _layout(T &refs[], const string id, const int x1, const int y1, const int x2, const int y2)
    {
      init(refs, id, x1, y1, x2, y2);
    }
      
    _layout(T &refs[], const string id = NULL)
    {
      init(refs, id, 0, 0, 0, 0);
    }

    template<typename V>
    _layout<T> *operator<=(const V value) // overrides base class method
    {
      if(object != NULL)
      {
        wrapper <= value;
      }
      else
      {
        for(int i = 0; i < ArraySize(array); i++)
        {
          wrapper.assign(array[i]);
          wrapper <= value;
        }
      }
      return &this;
    }

    virtual _layout<T> *operator[](const string prop) override
    {
      wrapper[prop];
      return &this;
    }

    virtual _layout<T> *operator<=(const PackedRect &r) override
    {
      if(object != NULL)
      {
        wrapper <= r;
      }
      else
      {
        for(int i = 0; i < ArraySize(array); i++)
        {
          wrapper.assign(array[i]);
          wrapper <= r;
        }
      }
      return &this;
    }

    // the following methods are specific to StdLayoutBase

    template<typename V>
    _layout<T> *operator<=(SequenceGenerator<V> &gen)
    {
      if(object == NULL)
      {
        for(int i = 0; i < ArraySize(array); i++)
        {
          wrapper.assign(array[i]);
          wrapper <= ++gen;
        }
      }
      return &this;
    }

    _layout<T> *operator<(ItemGenerator<T> *gen)
    {
      while(gen.addItemTo(object));
      if(CheckPointer(gen) == POINTER_DYNAMIC) delete gen;
      return &this;
    }

    _layout<T> *operator<=(ItemGenerator<T> &gen)
    {
      while(gen.addItemTo(object));
      return &this;
    }

    template<typename V>
    void attach(StdValue<V> *v)
    {
      ((T *)object).bind(v);
    }

    template<typename V>
    void attach(StdValue<V> &a[])
    {
      if(ArraySize(array) == ArraySize(a))
      {
        for(int i = 0; i < ArraySize(a); i++)
        {
          ((T *)array[i]).bind(&a[i]);
        }
      }
      else
      {
        Print("Can't bind arrays in ", typename(this));
      }
    }
};


template<typename T>
class StdItemGenerator: public ItemGenerator<T>
{
  protected:
    long maximum;
    long index;
    string prefix;

  public:
    StdItemGenerator(const long max, const string customText = NULL): index(0), maximum(max), prefix(customText) {}
    
    virtual long index2value()
    {
      return index;
    }

    virtual bool addItemTo(T *object) override
    {
      object.AddItem((prefix != NULL ? prefix : typename(T)) + (string)index, index2value());
      index++;
      return index < maximum;
    }
};

template<typename T>
class StdGroupItemGenerator: public StdItemGenerator<T>
{
  public:
    StdGroupItemGenerator(const long max, const string customText = NULL): StdItemGenerator(max, customText) {}

    virtual long index2value() override
    {
      return 1 << index;
    }
};

template<typename T>
class SymbolsItemGenerator: public ItemGenerator<T>
{
  protected:
    long index;

  public:
    SymbolsItemGenerator(): index(0) {}

    virtual bool addItemTo(T *object) override
    {
      object.AddItem(SymbolName((int)index, true), index);
      index++;
      return index < SymbolsTotal(true);
    }
};

template<typename T,typename V>
class ArrayItemGenerator: public ItemGenerator<T>
{
  protected:
    V data[];
    int index;
    bool bitmask;

  public:
    ArrayItemGenerator(const V &array[], const bool group = false): index(0), bitmask(group)
    {
      ArrayCopy(data, array);
    }

    virtual bool addItemTo(T *object)
    {
      if(index < ArraySize(data))
      {
        object.AddItem((string)data[index], (bitmask ? 1 << index : index));
        index++;
        return true;
      }
      return false;
    }
};
