//+------------------------------------------------------------------+
//|                                           AppDialogResizable.mqh |
//|                               Copyright (c) 2019-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include <ControlsPlus/Dialog.mqh>
#include <ControlsPlus/Button.mqh>

#resource "\\Include\\Layouts\\png\\Expand2.csv"
#resource "\\Include\\Layouts\\png\\size6.csv"
#resource "\\Include\\Layouts\\png\\size10.csv"

class AppDialogResizable: public CAppDialog
{
  protected:
    CBmpButton m_button_truemax;
    CBmpButton m_button_size;
    bool m_maximized;
    CRect m_max_rect;
    CSize m_size_limit;
    bool m_sizing;
    static int s_contained;
    static int s_index;
    int m_contained;
    int m_myid;

    // window maximization
    virtual bool CreateButtonMinMax(void) override;
    virtual void OnClickButtonMinMax(void) override;
    virtual void OnClickButtonTrueMax(void);
    virtual void OnClickButtonSizeFixMe(void);
    virtual void Expand(void);
    virtual void Restore(void);

    virtual void Minimize(void) override;
    virtual bool Save(const int handle) override;
    virtual bool Load(const int handle) override;

    // window resizing
    bool CreateButtonSize(void);
    bool OnDialogSizeStart(void);
    virtual bool OnDialogDragStart(void) override;
    virtual bool OnDialogDragProcess(void) override;
    virtual bool OnDialogDragEnd(void) override;

    virtual void SelfAdjustment(const bool restore = false) {};

  public:
    AppDialogResizable(): m_maximized(false), m_sizing(false), m_contained(0) { RTTI; }
    virtual bool Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2) override;
    virtual bool OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam) override;

    virtual bool OnChartChange(const long &lparam, const double &dparam, const string &sparam);

    void ChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam);
    
    void SetSizeLimit(const CSize &limit) { m_size_limit = limit; }
    CSize GetSizeLimit() { return m_size_limit; }
};

static int AppDialogResizable::s_contained = 0;
static int AppDialogResizable::s_index = 0;

void AppDialogResizable::ChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
  if(id == CHARTEVENT_CHART_CHANGE)
  {
    if(OnChartChange(lparam, dparam, sparam)) return;
  }
  else if(id == CHARTEVENT_MOUSE_MOVE)
  {
    const bool c = Contains((int)lparam, (int)dparam);
    const bool scroll = !c && (m_drag_object == NULL);
    if(!scroll) s_contained |= m_myid;
    else s_contained &= ~m_myid;

    //if((TerminalInfoInteger(TERMINAL_KEYSTATE_CAPSLOCK) & 1) != 0)
    //{
    //  printf("chscrl %s %s [%x] %x %x", this.Name(), (scroll ? "true" : "false"), m_myid, m_contained, s_contained);
    //}

    if(s_contained != m_contained)
    {
      ChartSetInteger(ChartID(), CHART_MOUSE_SCROLL, !s_contained);
      m_contained = s_contained;
    }
  }
  CAppDialog::ChartEvent(id, lparam, dparam, sparam);
}

EVENT_MAP_BEGIN(AppDialogResizable)
  ON_EVENT(ON_CLICK, m_button_truemax, OnClickButtonTrueMax)
  ON_EVENT(ON_CLICK, m_button_size, OnClickButtonSizeFixMe)
  ON_EVENT(ON_DRAG_START, m_button_size, OnDialogSizeStart)
  ON_EVENT_PTR(ON_DRAG_PROCESS, m_drag_object, OnDialogDragProcess)
  ON_EVENT_PTR(ON_DRAG_END, m_drag_object, OnDialogDragEnd)
EVENT_MAP_END(CAppDialog)

bool AppDialogResizable::Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2)
{
  // 1 * CONTROLS_BORDER_WIDTH - stays here, because the standard control library minimizes window
  // when it's height is 1 pixel smaller than the entire chart height
  m_max_rect.SetBound(0,
                      0,
                      (int)ChartGetInteger(ChartID(), CHART_WIDTH_IN_PIXELS) - 0 * CONTROLS_BORDER_WIDTH,
                      (int)ChartGetInteger(ChartID(), CHART_HEIGHT_IN_PIXELS) - 1 * CONTROLS_BORDER_WIDTH);
  if(!CAppDialog::Create(chart, name, subwin, x1, y1, x2, y2)) return false;
  if(!CreateButtonSize()) return false;
  m_size_limit.cx = x2 - x1;
  m_size_limit.cy = y2 - y1;
  if(m_size_limit.cx >= m_max_rect.Width() || m_size_limit.cy >= m_max_rect.Height())
  {
    m_size_limit.cx = m_min_rect.Width() * 3;
    m_size_limit.cy = m_min_rect.Height() * 7;
  }
  m_myid = 1 << s_index;
  s_index++;

  return true;
}

bool AppDialogResizable::CreateButtonMinMax(void) override
{
  if(!CAppDialog::CreateButtonMinMax()) return false;

  // add maximization button
  int off = (m_panel_flag) ? 0 : 2 * CONTROLS_BORDER_WIDTH;

  int x1 = Width() - off - 3 * (CONTROLS_BUTTON_SIZE + CONTROLS_DIALOG_BUTTON_OFF);
  int y1 = off + CONTROLS_DIALOG_BUTTON_OFF;
  int x2 = x1 + CONTROLS_BUTTON_SIZE;
  int y2 = y1 + CONTROLS_BUTTON_SIZE;

  if(!m_button_truemax.Create(m_chart_id, m_name + "TrueMax", m_subwin, x1, y1, x2, y2)) return false;
  if(!m_button_truemax.BmpNames("::Include\\Layouts\\png\\Expand2.csv", "::Include\\Controls\\res\\Restore.bmp")) return false;
  if(!CWndContainer::Add(m_button_truemax)) return false;
  
  m_button_truemax.Locking(true);
  m_button_truemax.Alignment(WND_ALIGN_RIGHT, 0, 0, off + 2 * CONTROLS_BUTTON_SIZE + 2 * CONTROLS_DIALOG_BUTTON_OFF, 0);

  CaptionAlignment(WND_ALIGN_WIDTH, off, 0, off + 3 * (CONTROLS_BUTTON_SIZE + CONTROLS_DIALOG_BUTTON_OFF), 0);

  return true;
}

bool AppDialogResizable::CreateButtonSize(void)
{
  int off = (m_panel_flag) ? 0 : 2 * CONTROLS_BORDER_WIDTH;

  int x1 = Width() - CONTROLS_BUTTON_SIZE + 1;
  int y1 = Height() - CONTROLS_BUTTON_SIZE + 1;
  int x2 = x1 + CONTROLS_BUTTON_SIZE - 1;
  int y2 = y1 + CONTROLS_BUTTON_SIZE - 1;

  if(!m_button_size.Create(m_chart_id, m_name + "Size", m_subwin, x1, y1, x2, y2)) return false;
  if(!m_button_size.BmpNames("::Include\\Layouts\\png\\size6.csv", "::Include\\Layouts\\png\\size10.csv")) return false;
  if(!CWndContainer::Add(m_button_size)) return false;
  m_button_size.Alignment(WND_ALIGN_RIGHT|WND_ALIGN_BOTTOM, 0, 0, 0, 0);
  m_button_size.PropFlagsSet(WND_PROP_FLAG_CAN_DRAG);

  return true;
}

void AppDialogResizable::OnClickButtonTrueMax(void)
{
  if(m_button_truemax.Pressed())
    Expand();
  else
    Restore();

  SubwinOff();
}

// This is a hack. It's required because in minimized state sizing button somehow "overlaps"
// the close button and intercepts clicks on it (which prevents exit from minimized app).
// This happens despite the fact that the sizing button is hidden, disabled and assigned
// with minimal Z-order (checked out, then removed).
// Looks like a bug in the standard control library, specifically:
// In CWnd::OnMouseEvent there must be a line:
//
//   if(!IS_ENABLED || !IS_VISIBLE) return false;
//
// but it's not there, so invisible, disabled and even background objects are processed
// in the same manner as all other objects. Specifically in CWndContainer::OnMouseEvent
// there is a reverse loop through all objects (it does _not_ respect Z-order anyhow).

void AppDialogResizable::OnClickButtonSizeFixMe(void)
{
  if(m_minimized)
  {
    Destroy();
  }
}

void AppDialogResizable::Expand(void)
{
  m_maximized = true;
  m_minimized = false;
  m_button_minmax.Pressed(false);
  Rebound(m_max_rect);
  m_button_size.Hide();
  m_button_size.StateFlagsReset(WND_STATE_FLAG_ENABLE);
  m_button_size.PropFlagsReset(WND_PROP_FLAG_CAN_DRAG);
  if(!m_panel_flag)
  {
    m_caption.PropFlagsReset(WND_PROP_FLAG_CAN_DRAG);
  }
  
  ClientAreaVisible(true);
  SelfAdjustment();
}

//+------------------------------------------------------------------+
//| Restore dialog window                                            |
//+------------------------------------------------------------------+
void AppDialogResizable::Restore(void)
{
  m_maximized = false;
  m_minimized = false;
  m_button_minmax.Pressed(false);
  m_button_size.Show();
  m_button_size.StateFlagsSet(WND_STATE_FLAG_ENABLE);
  m_button_size.PropFlagsSet(WND_PROP_FLAG_CAN_DRAG);
  CAppDialog::Maximize();
  if(!m_panel_flag)
  {
    m_caption.PropFlagsSet(WND_PROP_FLAG_CAN_DRAG);
  }
  SelfAdjustment(true);
}

void AppDialogResizable::Minimize()
{
  CAppDialog::Minimize();
  m_button_size.Hide();
  m_button_size.StateFlagsReset(WND_STATE_FLAG_ENABLE);
  m_button_size.PropFlagsReset(WND_PROP_FLAG_CAN_DRAG);
}

bool AppDialogResizable::OnChartChange(const long &lparam, const double &dparam, const string &sparam)
{
  m_max_rect.SetBound(0, 0,
                      (int)ChartGetInteger(ChartID(), CHART_WIDTH_IN_PIXELS) - 0 * CONTROLS_BORDER_WIDTH,
                      (int)ChartGetInteger(ChartID(), CHART_HEIGHT_IN_PIXELS) - 1 * CONTROLS_BORDER_WIDTH);
  if(m_maximized)
  {
    if(m_rect.Width() != m_max_rect.Width() || m_rect.Height() != m_max_rect.Height())
    {
      Rebound(m_max_rect);
      SelfAdjustment();
      m_chart.Redraw();
    }
    return true;
  }
  return false;
}

void AppDialogResizable::OnClickButtonMinMax(void)
{
  CAppDialog::OnClickButtonMinMax();
  m_button_truemax.Pressed(false);
  m_maximized = false;
  if(m_minimized)
  {
    m_button_size.Hide();
    m_button_size.StateFlagsReset(WND_STATE_FLAG_ENABLE);
    m_button_size.PropFlagsReset(WND_PROP_FLAG_CAN_DRAG);
  }
  else
  {
    m_button_size.Show();
    m_button_size.StateFlagsSet(WND_STATE_FLAG_ENABLE);
    m_button_size.PropFlagsSet(WND_PROP_FLAG_CAN_DRAG);
  }
  if(!m_panel_flag)
  {
    m_caption.PropFlagsSet(WND_PROP_FLAG_CAN_DRAG);
  }
  if(!m_minimized)
  {
    SelfAdjustment();
  }
}

bool AppDialogResizable::OnDialogSizeStart(void)
{
  if(m_drag_object == NULL)
  {
    m_drag_object = new CDragWnd;
    if(m_drag_object == NULL) return false;
  }
  int x1 = m_button_size.Left() - CONTROLS_DRAG_SPACING;
  int y1 = m_button_size.Top() - CONTROLS_DRAG_SPACING;
  int x2 = m_button_size.Right() + CONTROLS_DRAG_SPACING;
  int y2 = m_button_size.Bottom() + CONTROLS_DRAG_SPACING;

  m_drag_object.Create(m_chart_id, "", m_subwin, x1, y1, x2, y2);
  m_drag_object.PropFlagsSet(WND_PROP_FLAG_CAN_DRAG);

  CChart chart;
  chart.Attach(m_chart_id);
  m_drag_object.Limits(-CONTROLS_DRAG_SPACING, -CONTROLS_DRAG_SPACING,
                       chart.WidthInPixels() + CONTROLS_DRAG_SPACING,
                       chart.HeightInPixels(m_subwin) + CONTROLS_DRAG_SPACING);
  chart.Detach();

  m_drag_object.MouseX(m_button_size.MouseX());
  m_drag_object.MouseY(m_button_size.MouseY());
  m_drag_object.MouseFlags(m_button_size.MouseFlags());
  
  m_sizing = true;

  return true;
}

bool AppDialogResizable::OnDialogDragStart(void)
{
  if(m_maximized) return false;
  
  return CAppDialog::OnDialogDragStart();
}
//+------------------------------------------------------------------+
//| Continue dragging the dialog box                                 |
//+------------------------------------------------------------------+
bool AppDialogResizable::OnDialogDragProcess(void)
{
  if(!m_sizing) return CDialog::OnDialogDragProcess();

  if(m_drag_object == NULL) return false;

  int x = m_drag_object.Right() - Right() - CONTROLS_DRAG_SPACING;
  int y = m_drag_object.Bottom() - Bottom() - CONTROLS_DRAG_SPACING;

  // resize dialog
  CRect r = Rect();
  r.right += x;
  r.bottom += y;
  
  if(r.Width() < m_size_limit.cx) r.right = r.left + m_size_limit.cx;
  if(r.Height() < m_size_limit.cy) r.bottom = r.top + m_size_limit.cy;
  
  Rebound(r);
  
  SelfAdjustment();

  return true;
}
//+------------------------------------------------------------------+
//| End dragging the dialog box                                      |
//+------------------------------------------------------------------+
bool AppDialogResizable::OnDialogDragEnd(void)
{
  m_contained = 0;

  if(!m_sizing) return CDialog::OnDialogDragEnd();

  if(m_drag_object != NULL)
  {
    m_button_size.MouseFlags(m_drag_object.MouseFlags());
    delete m_drag_object;
    m_drag_object = NULL;
  }

  m_norm_rect.SetBound(m_rect);
  m_sizing = false;
  
  SelfAdjustment();
  
  return true;
}

bool AppDialogResizable::Save(const int handle) override
{
  FileWriteInteger(handle, m_maximized);
  return CAppDialog::Save(handle);
}

bool AppDialogResizable::Load(const int handle) override
{
  m_maximized = (bool)FileReadInteger(handle);
  return CAppDialog::Load(handle);
}
