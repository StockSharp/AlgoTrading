//+------------------------------------------------------------------+
//|                                                 ChartBrowser.mqh |
//|                                    Copyright (c) 2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//|                              https://www.mql5.com/en/code/33770/ |
//|                                                                  |
//|                           https://www.mql5.com/en/articles/7734/ |
//|                           https://www.mql5.com/en/articles/7739/ |
//|                           https://www.mql5.com/ru/articles/7795/ |
//+------------------------------------------------------------------+

#include <ControlsPlus/Dialog.mqh>
#include <ControlsPlus/Button.mqh>
#include <ControlsPlus/Edit.mqh>
#include <ControlsPlus/CheckBox.mqh>
#include <Layouts/LayoutDefines.mqh>
#include <Layouts/Box.mqh>
#include <Layouts/ComboBoxResizable.mqh>
#include <Layouts/RadioGroupResizable.mqh>
#include <Layouts/CheckGroupResizable.mqh>
#include <Layouts/ListViewResizable.mqh>
#include <Layouts/AppDialogResizable.mqh>
#include <Layouts/LayoutMonitors.mqh>
#include <Layouts/LayoutStdLib.mqh>
#include <Layouts/SpinEditResizable.mqh>
#include <Layouts/Sort.mqh>


#define TAB_CURRENT   -1
#define TAB_CHARTS     0
#define TAB_INDICATORS 1
#define TAB_EXPERTS    2
#define TAB_SCRIPTS    3

const static string tabs[4] = {"Charts", "Indicators", "Experts", "Scripts"};

string period2string(const ENUM_TIMEFRAMES tf)
{
  const static int plen = StringLen("PERIOD_");
  return StringSubstr(EnumToString(tf), plen);
}

template<typename T>
int push(T &results[], const T &text)
{
  const int n = ArraySize(results);
  ArrayResize(results, n + 1);
  results[n] = text;
  return n;
}

struct Pair
{
  public:
    Pair() {}
    Pair(const string S, const long L): s(S), id(L) {}
    string s;
    long id;
};

class ComparePairs : public COMPARE<Pair>
{
  public:
    int Compare(const Pair &First, const Pair &Second) const
    {
      return Second.s < First.s ? +1 : (Second.s == First.s ? 0 : -1);
    }
};

int listCharts(const int type, string &results[], long &ids[])
{
  const string gap = "    ";
  const long me = ChartID();
  long id = ChartFirst();
  int count = 0, used = 0, temp, experts = 0, scripts = 0, indicators = 0, subs = 0;
  
  Pair sorter[];
  
  while(id != -1)
  {
    temp = 0;
    const int win = (int)ChartGetInteger(id, CHART_WINDOWS_TOTAL);

    // props: symbol, period, expert, script, main window indicators, subwindow indicators
    
    string header = StringFormat("%s %s %s %s", ChartSymbol(id), period2string(ChartPeriod(id)), (win > 1 ? (string)(win - 1) : ""), (id == me ? " *" : ""));
    
    string expert = ChartGetString(id, CHART_EXPERT_NAME);
    string script = ChartGetString(id, CHART_SCRIPT_NAME);
    if(expert != NULL || script != NULL)
    {
      if(expert != NULL)
      {
        experts++;
        if(type == 0) header += "\n" + gap + "[E] " + expert;
        else if(type == 2)
        {
          expert += "\n" + gap + header;
          push(sorter, Pair(expert, id));
        }
      }
      if(script != NULL)
      {
        scripts++;
        if(type == 0) header += "\n" + gap + "[S] " + script;
        else if(type == 3)
        {
          script += "\n" + gap + header;
          push(sorter, Pair(script, id));
        }
      }
      temp++;
    }
    
    for(int i = 0; i < win; i++)
    {
      const int n = ChartIndicatorsTotal(id, i);
      for(int k = 0; k < n; k++)
      {
        string ind = StringFormat("%s <%d;%d>", ChartIndicatorName(id, i, k), i, k);
        if(type == 0) header += "\n" + gap + "[I] " + ind;
        else if(type == 1)
        {
          ind += "\n" + gap + header;
          push(sorter, Pair(ind, id));
        }
        indicators++;
        if(i > 0) subs++;
        temp++;
      }
    }
    
    if(type == 0)
    {
      push(sorter, Pair(header, id));
    }
    
    count++;
    if(temp > 0)
    {
      used++;
    }
    id = ChartNext(id);
  }

  ComparePairs cmp;
  SORT::Sort(sorter, cmp);
  const int n = ArraySize(sorter);
  ArrayResize(results, n);
  ArrayResize(ids, n);
  
  for(int i = 0; i < n; i++)
  {
    results[i] = sorter[i].s;
    ids[i] = sorter[i].id;
  }

  //Print("Total charts number: ", count, ", with MQL-programs: ", used);
  //Print("Experts: ", experts, ", Scripts: ", scripts, ", Indicators: ", indicators, " (main: ", indicators - subs, " / sub: ", subs, ")");
  return n;
}

class TabButton: public CButton
{
  public:
    virtual bool OnEnable(void) override
    {
      m_button.Z_Order(0);
      ColorBackground(CONTROLS_BUTTON_COLOR_BG);
      return true;
    }

    virtual bool OnDisable(void) override
    {
      m_button.Z_Order(-100);
      ColorBackground(CONTROLS_LISTITEM_COLOR_BG);
      return true;
    }
};


class ChartBrowserForm;

class DefaultLayoutStyleable: public StdLayoutStyleable
{
  public:
    virtual void apply(CWnd *control, const STYLER_PHASE phase) override
    {
      CButton *button = dynamic_cast<CButton *>(control);
      if(button != NULL)
      {
        button.ColorBorder(CONTROLS_LISTITEM_COLOR_BG);
      }
    }
};

class MyStdLayoutCache: public StdLayoutCache
{
  protected:
    ChartBrowserForm *parent;
    DefaultLayoutStyleable styler;
    TabButton *group[];
    ListViewResizable *list;
    int currentTab;

  public:
    MyStdLayoutCache(ChartBrowserForm *owner): parent(owner), list(NULL), currentTab(-1) {}

    virtual bool onEvent(const int event, CWnd *control) override
    {
      if(control != NULL)
      {
        // debug
        // Print(control._rtti, " / ", control.Name(), " / ", event);
        // CWndContainer *container = dynamic_cast<CWndContainer *>(findParent(control));
        // if(container != NULL)
        // Print(container._rtti, " / ", container.Name());
        TabButton *button = dynamic_cast<TabButton *>(control);
        if(button != NULL)
        {
          const int cmd = adjustGroup(button);
          fillList(cmd);
          return true;
        }
        else
        {
          CButton *go = dynamic_cast<CButton *>(control);
          if(go != NULL)
          {
            const long index = list.Value();
            // Print("Selected:", index);
            if(index == LONG_MAX)
            {
              if(list.Count() > 0)
              {
                MessageBox("Please, select an item in the list");
              }
              else
              {
                MessageBox("Nothing to select here");
              }
            }
            else
            {
              ChartSetInteger(list.Value(), CHART_BRING_TO_TOP, true);
              ChartGetInteger(list.Value(), CHART_WINDOW_HANDLE);
            }
          }
        }
      }
      return false; // not processed here, so give a chance to process in other handlers
    }

    virtual StdLayoutStyleable *getStyler() const override
    {
      return (StdLayoutStyleable *)&styler;
    }
    
    // custom stuff
    
    void registerList(ListViewResizable *ptr)
    {
      list = ptr;
    }
    
    void registerGroupButton(TabButton *ptr)
    {
      const int n = ArraySize(group);
      ArrayResize(group, n + 1);
      group[n] = ptr;
    }
  
  private:  
    int adjustGroup(TabButton *pressed)
    {
      const int n = ArraySize(group);
      int index = -1;
      for(int i = 0; i < n; i++)
      {
        if(group[i] == pressed)
        {
          pressed.Disable();
          index = i;
        }
        else group[i].Enable();
      }
      return index;
    }
    
    int expand(const string &a1[], const long &ids[], string &a2[], long &sdi[])
    {
      string lines[];
      const int n = ArraySize(a1);
      for(int i = 0; i < n; i++)
      {
        const int m = StringSplit(a1[i], '\n', lines);
        long temp[];
        ArrayResize(temp, m);
        ArrayFill(temp, 0, m, ids[i]);
        ArrayCopy(a2, lines, ArraySize(a2), 0, m);
        ArrayCopy(sdi, temp, ArraySize(sdi), 0, m);
      }
      return ArraySize(a2);
    }
  
  public:
    void fillList(const int type = TAB_CURRENT)
    {
      if(type != TAB_CURRENT)
      {
        currentTab = type;
      }
    
      string results[];
      long ids[];
      const int t = listCharts(currentTab, results, ids);
      
      parent.Caption(DIALOG_TITLE + ":" + (string)t + " " + tabs[currentTab]);
      
      string expanded[];
      long charts[];
      const int n = expand(results, ids, expanded, charts);
      list.ItemsClear();
      for(int i = 0; i < n; i++)
      {
        list.ItemAdd(expanded[i], charts[i]);
      }
      list.adjustVSize();
      //list.forceVScroll();
    }
};


//+-----------------------------------------------------------------------+
//| Main dialog window with controls                                      |
//+-----------------------------------------------------------------------+
class ChartBrowserForm: public AppDialogResizable
{
  private:
    MyStdLayoutCache *cache;
    CBox *pMain;
    
  public:
    ChartBrowserForm(void);
    ~ChartBrowserForm(void);

    bool CreateLayout(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2);
    
    // general handler for event map
    virtual bool OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam);

    MyStdLayoutCache *getCache(void) const
    {
      return cache;
    }
    
    virtual bool OnChartChange(const long &lparam, const double &dparam, const string &sparam) override
    {
      const bool result = AppDialogResizable::OnChartChange(lparam, dparam, sparam);
      if(this.m_minimized && ChartGetInteger(0, CHART_IS_MAXIMIZED))
      {
        this.Restore();
        cache.fillList();
      }
      return result;
    }

  protected:
    CBox *GetMainContainer(void);
    virtual void SelfAdjustment(const bool restore = false) override;
    bool OnRefresh();
};


//+------------------------------------------------------------------+
//| Event handling                                                   |
//+------------------------------------------------------------------+

EVENT_MAP_BEGIN(ChartBrowserForm)
  ON_EVENT_LAYOUT_ARRAY(ON_CLICK, cache)
  ON_NO_ID_EVENT(ON_LAYOUT_REFRESH, OnRefresh)
EVENT_MAP_END(AppDialogResizable)

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
ChartBrowserForm::ChartBrowserForm(void)
{
  RTTI;
  pMain = NULL;
  cache = new MyStdLayoutCache(&this);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
ChartBrowserForm::~ChartBrowserForm(void)
{
  delete cache;
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool ChartBrowserForm::CreateLayout(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2)
{
  StdLayoutBase::setCache(cache);
  {
    _layout<ChartBrowserForm> dialog(this, name, x1, y1, x2, y2);
    
    // ------------------
    // GUI Layout for MQL app (standard controls library)
    {
      _layout<CBoxV> main("main", ClientAreaWidth(), ClientAreaHeight(), WND_ALIGN_CLIENT);
      main <= PackedRect(0, 0, 0, 0);
      {
        {
          _layout<CBoxH> Controls("Controls", 192, 30, (ENUM_WND_ALIGN_FLAGS)(WND_ALIGN_CONTENT|(ENUM_WND_ALIGN_FLAGS)(WND_ALIGN_WIDTH|WND_ALIGN_TOP)));
          Controls <= PackedRect(5, 0, 5, 0) <= HORIZONTAL_ALIGN_STACK;
          {
            {
              _layout<TabButton> ChartsButton("ChartsButton", 64, 20, (ENUM_WND_ALIGN_FLAGS)WND_ALIGN_BOTTOM);
              ChartsButton <= tabs[TAB_CHARTS] <= PackedRect(0, 0, 0, 0);
              ChartsButton["enable"] <= false;
              cache.registerGroupButton(ChartsButton.get());
            }
            {
              _layout<TabButton> IndicatorsButton("IndicatorsButton", 64, 20, (ENUM_WND_ALIGN_FLAGS)WND_ALIGN_BOTTOM);
              IndicatorsButton <= tabs[TAB_INDICATORS] <= PackedRect(0, 0, 0, 0);
              cache.registerGroupButton(IndicatorsButton.get());
            }
            {
              _layout<TabButton> ExpertsButton("ExpertsButton", 64, 20, (ENUM_WND_ALIGN_FLAGS)WND_ALIGN_BOTTOM);
              ExpertsButton <= tabs[TAB_EXPERTS] <= PackedRect(0, 0, 0, 0);
              cache.registerGroupButton(ExpertsButton.get());
            }
            {
              _layout<TabButton> ScriptsButton("ScriptsButton", 64, 20, (ENUM_WND_ALIGN_FLAGS)WND_ALIGN_BOTTOM);
              ScriptsButton <= tabs[TAB_SCRIPTS] <= PackedRect(0, 0, 0, 0);
              cache.registerGroupButton(ScriptsButton.get());
            }

            {
              _layout<CButton> GoButton("GoButton", 50, 20, (ENUM_WND_ALIGN_FLAGS)WND_ALIGN_RIGHT);
              GoButton <= "Go" <= PackedRect(0, 0, 0, 0) <= clrWhite;
              GoButton["background"] <= clrDodgerBlue;
              GoButton["font"] <= "Arial Black";
            }
          }
          
        }
        {
          _layout<CBoxH> listContainer("listContainer", 192, 304, (ENUM_WND_ALIGN_FLAGS)(WND_ALIGN_CONTENT|WND_ALIGN_CLIENT));
          listContainer <= PackedRect(0, 30, 0, 0);
          {
            {
              _layout<ListViewResizable> list("list", 190, 304, WND_ALIGN_HEIGHT);
              list <= PackedRect(0, 0, 0, 0);
              cache.registerList(list.get());
              cache.fillList(TAB_CHARTS);
            }
          }
        }
      }
    }

    // ----------------------
  }

  SelfAdjustment();
  EventChartCustom(CONTROLS_SELF_MESSAGE, ON_LAYOUT_REFRESH, 0, 0.0, NULL);

  return true;
}

CBox *ChartBrowserForm::GetMainContainer(void)
{
  for(int i = 0; i < ControlsTotal(); i++)
  {
    CWndClient *client = dynamic_cast<CWndClient *>(Control(i));
    if(client != NULL)
    {
      for(int j = 0; j < client.ControlsTotal(); j++)
      {
        CBox *box = dynamic_cast<CBox *>(client.Control(j));
        if(box != NULL)
        {
          return box;
        }
      }
    }
  }
  return NULL;
}

void ChartBrowserForm::SelfAdjustment(const bool restore = false)
{
  if(pMain == NULL)
  {
    pMain = GetMainContainer();
  }
  
  if(pMain)
  {
    pMain.Show();
    pMain.Pack();
    Rebound(Rect());
  }
}

bool ChartBrowserForm::OnRefresh()
{
  SelfAdjustment();
  return true;
}
