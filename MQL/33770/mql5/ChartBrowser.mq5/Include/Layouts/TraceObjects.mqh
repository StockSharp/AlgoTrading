template<typename V,typename E>
class GetObjectProperty
{
  protected:
    string name;

  public:
    GetObjectProperty(): name(NULL) {}
    GetObjectProperty(const string n): name(n) {}
    void operator=(const string n) { name = n; }
    virtual V operator[](E element) = 0;
};

class GetIntegerProperty: public GetObjectProperty<long,ENUM_OBJECT_PROPERTY_INTEGER>
{
  public:
    virtual long operator[](ENUM_OBJECT_PROPERTY_INTEGER element) override
    {
      return ObjectGetInteger(0, name, element);
    }
};

GetIntegerProperty gip;


void TraceObjects()
{
  for(int i = 0; i < ObjectsTotal(0); i++)
  {
    const string name = ObjectName(0, i, 0);
    gip = name;

    Print(name, " ", EnumToString((ENUM_OBJECT)gip[OBJPROP_TYPE]), " [", gip[OBJPROP_XDISTANCE], "@", gip[OBJPROP_YDISTANCE],
                 " ", gip[OBJPROP_XSIZE], "*", gip[OBJPROP_YSIZE], "] z=", gip[OBJPROP_ZORDER], " b=", gip[OBJPROP_BACK]);
  }
}


#ifdef INTERNAL_EVENT // if standard controls are included

void TraverseWindows(CWndContainer *container)
{
  static int level = 0;
  string margin = "";
  StringInit(margin, level * 2, ' ');
  Print("* ", margin, typename(container), " ", container._rtti, " ", container.Name(), " F:", container.StateFlags());
  level++;
  margin += "  ";
  for(int i = 0; i < container.ControlsTotal(); i++)
  {
    CWnd *wnd = container.Control(i);
    CWndContainer *child = dynamic_cast<CWndContainer *>(wnd);
    if(child != NULL)
    {
      TraverseWindows(child);
    }
    else
    {
      Print("+ ", margin, typename(wnd), " ", wnd._rtti, " ", wnd.Name(), " ", wnd.Id(), " F:", wnd.StateFlags());
    }
  }
  level--;
}

#endif