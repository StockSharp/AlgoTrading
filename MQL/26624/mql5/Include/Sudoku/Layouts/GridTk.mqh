//+------------------------------------------------------------------+
//|                                                       GridTk.mqh |
//|                                                   Enrico Lambino |
//|                                      www.mql5.com/en/users/iceron|
//+------------------------------------------------------------------+
#property copyright "Enrico Lambino"
#property link "http://www.mql5.com"
#property strict
#include "Grid.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CGridConstraints: public CObject
{
 protected:
  CWnd *m_control;
  int m_row;
  int m_col;
  int m_rowspan;
  int m_colspan;

 public:
  CGridConstraints(CWnd *control, int row, int column, int rowspan = 1, int colspan = 1);
  ~CGridConstraints();
  CWnd *Control()
  {
    return (m_control);
  }
  int Row()
  {
    return (m_row);
  }
  int Column()
  {
    return (m_col);
  }
  int RowSpan()
  {
    return (m_rowspan);
  }
  int ColSpan()
  {
    return (m_colspan);
  }
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGridConstraints::CGridConstraints(CWnd *control, int row, int column, int rowspan = 1, int colspan = 1)
{
  m_control = control;
  m_row = MathMax(0, row);
  m_col = MathMax(0, column);
  m_rowspan = MathMax(1, rowspan);
  m_colspan = MathMax(1, colspan);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGridConstraints::~CGridConstraints()
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CGridTk: public CGrid
{
 protected:
  CArrayObj m_constraints;

 public:
  CGridTk();
  ~CGridTk();
  bool Grid(CWnd *control, int row, int column, int rowspan, int colspan);
  bool Pack();
  CGridConstraints *GetGridConstraints(CWnd *control);
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGridTk::CGridTk(void)
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGridTk::~CGridTk(void)
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGridTk::Grid(CWnd *control, int row, int column, int rowspan = 1, int colspan = 1)
{
  CGridConstraints *constraints = new CGridConstraints(control, row, column, rowspan, colspan);
  if(!CheckPointer(constraints))
    return (false);
  if(!m_constraints.Add(constraints))
    return (false);
  return (Add(control));
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGridTk::Pack()
{
  CGrid::Pack();
  CSize size = Size();
  m_cell_size.cx = (size.cx - (m_cols + 1) * m_hgap) / m_cols;
  m_cell_size.cy = (size.cy - (m_rows + 1) * m_vgap) / m_rows;
  for(int i = 0; i < ControlsTotal(); i++)
  {
    int x = Left(), y = Top();
    CWnd *control = Control(i);
    if(control == NULL)
      continue;
    if(control == GetPointer(m_background))
      continue;
    CGridConstraints *constraints = GetGridConstraints(control);
    if(constraints == NULL)
      continue;
    int column = constraints.Column();
    int row = constraints.Row();
    x += (column * m_cell_size.cx) + ((column + 1) * m_hgap);
    y += (row * m_cell_size.cy) + ((row + 1) * m_vgap);
    int colspan = constraints.ColSpan();
    int rowspan = constraints.RowSpan();
    control.Size(colspan * m_cell_size.cx + ((colspan - 1) * m_hgap), rowspan * m_cell_size.cy + ((rowspan - 1) * m_vgap));
    control.Move(x, y);
    if(control.Type() == CLASS_LAYOUT)
    {
      CBox *container = control;
      container.Pack();
    }
  }
  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGridConstraints *CGridTk::GetGridConstraints(CWnd *control)
{
  for(int i = 0; i < m_constraints.Total(); i++)
  {
    CGridConstraints *constraints = m_constraints.At(i);
    CWnd *ctrl = constraints.Control();
    if(ctrl == NULL)
      continue;
    if(ctrl == control)
      return (constraints);
  }
  return (NULL);
}
//+------------------------------------------------------------------+
