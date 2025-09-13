//+------------------------------------------------------------------+
//|                                                          Box.mqh |
//|                                                   Enrico Lambino |
//|                                      www.mql5.com/en/users/iceron|
//+------------------------------------------------------------------+
#property copyright "Enrico Lambino"
#property link "www.mql5.com/en/users/iceron"
#include <Controls\WndClient.mqh>
#define CLASS_LAYOUT 999

#ifdef LAYOUT_BOX_DEBUG
#define COLOR_BOX_BORDER clrRed
#else
#define COLOR_BOX_BORDER clrNONE
#endif
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum LAYOUT_STYLE
{
  LAYOUT_STYLE_VERTICAL,
  LAYOUT_STYLE_HORIZONTAL
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum VERTICAL_ALIGN
{
  VERTICAL_ALIGN_CENTER,
  VERTICAL_ALIGN_CENTER_NOSIDES,
  VERTICAL_ALIGN_TOP,
  VERTICAL_ALIGN_BOTTOM
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
enum HORIZONTAL_ALIGN
{
  HORIZONTAL_ALIGN_CENTER,
  HORIZONTAL_ALIGN_CENTER_NOSIDES,
  HORIZONTAL_ALIGN_LEFT,
  HORIZONTAL_ALIGN_RIGHT
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CBox: public CWndClient
{
 protected:
  LAYOUT_STYLE m_layout_style;
  VERTICAL_ALIGN m_vertical_align;
  HORIZONTAL_ALIGN m_horizontal_align;
  CSize m_min_size;
  int m_controls_total;
  int m_padding_top;
  int m_padding_bottom;
  int m_padding_left;
  int m_padding_right;
  int m_total_x;
  int m_total_y;

 public:
  CBox();
  ~CBox();
  virtual int Type() const
  {
    return CLASS_LAYOUT;
  }
  virtual bool Create(const long chart, const string name, const int subwin,
                      const int x1, const int y1, const int x2, const int y2);
  virtual bool Pack();
  void LayoutStyle(LAYOUT_STYLE style)
  {
    m_layout_style = style;
  }
  LAYOUT_STYLE LayoutStyle() const
  {
    return (m_layout_style);
  }
  void HorizontalAlign(const HORIZONTAL_ALIGN align)
  {
    m_horizontal_align = align;
  }
  HORIZONTAL_ALIGN HorizontalAlign() const
  {
    return (m_horizontal_align);
  }
  void VerticalAlign(const VERTICAL_ALIGN align)
  {
    m_vertical_align = align;
  }
  VERTICAL_ALIGN VerticalAlign() const
  {
    return (m_vertical_align);
  }
  void Padding(const int top, const int bottom, const int left, const int right);
  void Padding(const int padding);
  void PaddingTop(const int padding)
  {
    m_padding_top = padding;
  }
  int PaddingTop() const
  {
    return (m_padding_top);
  }
  void PaddingRight(const int padding)
  {
    m_padding_right = padding;
  }
  int PaddingRight() const
  {
    return (m_padding_right);
  }
  void PaddingBottom(const int padding)
  {
    m_padding_bottom = padding;
  }
  int PaddingBottom() const
  {
    return (m_padding_bottom);
  }
  void PaddingLeft(const int padding)
  {
    m_padding_left = padding;
  }
  int PaddingLeft() const
  {
    return (m_padding_left);
  }
  CSize GetMinSize() const
  {
    CSize sz;
    sz.cx = m_min_size.cx + m_padding_left + m_padding_right;
    sz.cy = m_min_size.cy + m_padding_top + m_padding_bottom;
    return sz;
  }

 protected:
  virtual void CheckControlSize(CWnd *control);
  virtual void GetTotalControlsSize(void);
  virtual bool GetSpace(int &x_space, int &y_space);
  virtual bool Render(void);
  virtual void Shift(CWnd *control, int &x, int &y, const int x_space, const int y_space);
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CBox::CBox():
    m_layout_style(LAYOUT_STYLE_HORIZONTAL),
    m_vertical_align(VERTICAL_ALIGN_CENTER),
    m_horizontal_align(HORIZONTAL_ALIGN_CENTER),
    m_controls_total(0),
    m_padding_top(2),
    m_padding_bottom(2),
    m_padding_left(2),
    m_padding_right(2),
    m_total_x(0),
    m_total_y(0)

{
  m_min_size.cx = 0;
  m_min_size.cy = 0;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CBox::~CBox()
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CBox::Create(const long chart, const string name, const int subwin,
                  const int x1, const int y1, const int x2, const int y2)
{
  if(!CWndContainer::Create(chart, name, subwin, x1, y1, x2, y2))
    return (false);
  if(!CreateBack())
    return (false);
  if(!ColorBackground(CONTROLS_DIALOG_COLOR_CLIENT_BG))
    return (false);
  if(!ColorBorder(COLOR_BOX_BORDER))
    return (false);
  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CBox::Pack(void)
{
  GetTotalControlsSize();
  return (Render());
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CBox::CheckControlSize(CWnd *control)
{
  bool adjust = false;
  CSize size = Size();
  CSize control_size = control.Size();
  if(control_size.cx > size.cx - (m_padding_left + m_padding_right))
  {
    control_size.cx = size.cx - (m_padding_left + m_padding_right);
    adjust = true;
  }
  if(control_size.cy > size.cy - (m_padding_top + m_padding_bottom))
  {
    control_size.cy = size.cy - (m_padding_top + m_padding_bottom);
    adjust = true;
  }
  if(adjust)
    control.Size(control_size.cx, control_size.cy);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CBox::GetTotalControlsSize(void)
{
  m_total_x = 0;
  m_total_y = 0;
  m_controls_total = 0;
  m_min_size.cx = 0;
  m_min_size.cy = 0;
  int total = ControlsTotal();
  
  
  for(int i = 0; i < total; i++)
  {
    CWnd *control = Control(i);
    if(control == NULL) continue;
    if(control == &m_background) continue;
    CheckControlSize(control);
    if(control.Type() == CLASS_LAYOUT)
    {
      ((CBox *)control).GetTotalControlsSize();
    }
    
    CSize control_size = control.Size();
    if(m_min_size.cx < control_size.cx)
      m_min_size.cx = control_size.cx;
    if(m_min_size.cy < control_size.cy)
      m_min_size.cy = control_size.cy;
    if(m_layout_style == LAYOUT_STYLE_HORIZONTAL) m_total_x += control_size.cx;
    else m_total_x = MathMax(m_min_size.cx, m_total_x);
    if(m_layout_style == LAYOUT_STYLE_VERTICAL) m_total_y += control_size.cy;
    else m_total_y = MathMax(m_min_size.cy, m_total_y);
    m_controls_total++;
  }
  
  CSize size = Size();
  
  if(m_total_x > size.cx && m_layout_style == LAYOUT_STYLE_HORIZONTAL)
  {
    size.cx = m_total_x;
  }
  if(m_total_y > size.cy && m_layout_style == LAYOUT_STYLE_VERTICAL) // shrink
  {
    size.cy = m_total_y;
  }
    
  Size(size);
}

//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CBox::GetSpace(int &x_space, int &y_space)
{
  if(m_controls_total == 0)
    return (true);
  if(m_controls_total == 1)
  {
    if(m_horizontal_align == HORIZONTAL_ALIGN_CENTER_NOSIDES)
      m_horizontal_align = HORIZONTAL_ALIGN_CENTER;
    if(m_vertical_align == VERTICAL_ALIGN_CENTER_NOSIDES)
      m_vertical_align = VERTICAL_ALIGN_CENTER;
  }
  CSize size = Size();

  int x_space_total = 0;
  int y_space_total = 0;
  if(m_layout_style == LAYOUT_STYLE_HORIZONTAL)
  {
    x_space_total = size.cx - (m_total_x + m_padding_left + m_padding_right);
    y_space_total = size.cy - (m_min_size.cy + m_padding_top + m_padding_bottom);

    if(m_horizontal_align == HORIZONTAL_ALIGN_CENTER_NOSIDES)
      x_space = x_space_total / (m_controls_total - 1);
    else if(m_horizontal_align == HORIZONTAL_ALIGN_CENTER)
      x_space = x_space_total / (m_controls_total + 1);
    else
      x_space = x_space_total / m_controls_total;

    if(m_vertical_align == VERTICAL_ALIGN_CENTER || m_vertical_align == VERTICAL_ALIGN_CENTER_NOSIDES)
      y_space = y_space_total / 2;
    else
      y_space = y_space_total;
  }
  else if(m_layout_style == LAYOUT_STYLE_VERTICAL)
  {
    x_space_total = size.cx - (m_min_size.cx + m_padding_left + m_padding_right);
    y_space_total = size.cy - (m_total_y + m_padding_top + m_padding_bottom);

    if(m_horizontal_align == HORIZONTAL_ALIGN_CENTER || m_horizontal_align == HORIZONTAL_ALIGN_CENTER_NOSIDES)
      x_space = x_space_total / 2;
    else
      x_space = x_space_total;

    if(m_vertical_align == VERTICAL_ALIGN_CENTER_NOSIDES)
      y_space = y_space_total / (m_controls_total - 1);
    else if(m_vertical_align == VERTICAL_ALIGN_CENTER)
      y_space = y_space_total / (m_controls_total + 1);
    else
      y_space = y_space_total / m_controls_total;
  }
  else
    return (false);

  if(x_space < 0) x_space = 0;
  if(y_space < 0) y_space = 0;

  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CBox::Shift(CWnd *control, int &x, int &y, const int x_space, const int y_space)
{
  if(m_layout_style == LAYOUT_STYLE_HORIZONTAL)
    x += x_space + control.Width();
  else if(m_layout_style == LAYOUT_STYLE_VERTICAL)
    y += y_space + control.Height();
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CBox::Render(void)
{
  int x_space = 0, y_space = 0;
  if(!GetSpace(x_space, y_space))
    return (false);
  int x = Left() + m_padding_left +
          ((m_horizontal_align == HORIZONTAL_ALIGN_LEFT || m_horizontal_align == HORIZONTAL_ALIGN_CENTER_NOSIDES) ? 0 : x_space);
  int y = Top() + m_padding_top +
          ((m_vertical_align == VERTICAL_ALIGN_TOP || m_vertical_align == VERTICAL_ALIGN_CENTER_NOSIDES) ? 0 : y_space);
  for(int j = 0; j < ControlsTotal(); j++)
  {
    CWnd *control = Control(j);
    if(control == NULL)
      continue;
    if(control == GetPointer(m_background))
      continue;
    control.Move(x, y);
    if(control.Type() == CLASS_LAYOUT)
    {
      CBox *container = control;
      container.Pack();
    }
    if(j < ControlsTotal() - 1)
      Shift(GetPointer(control), x, y, x_space, y_space);
  }
  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CBox::Padding(const int top, const int right, const int bottom, const int left)
{
  m_padding_top = top;
  m_padding_right = right;
  m_padding_bottom = bottom;
  m_padding_left = left;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CBox::Padding(const int padding)
{
  m_padding_top = padding;
  m_padding_right = padding;
  m_padding_bottom = padding;
  m_padding_left = padding;
}
//+------------------------------------------------------------------+
