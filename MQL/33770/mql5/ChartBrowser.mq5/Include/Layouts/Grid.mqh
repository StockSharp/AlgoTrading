//+------------------------------------------------------------------+
//|                                                         Grid.mqh |
//|                                                   Enrico Lambino |
//|                                      www.mql5.com/en/users/iceron|
//+------------------------------------------------------------------+
#property copyright "Enrico Lambino"
#property link "http://www.mql5.com"
#property strict
#include "Box.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CGrid: public CBox
{
 protected:
  int m_cols;
  int m_rows;
  int m_hgap;
  int m_vgap;
  CSize m_cell_size;

 public:
  CGrid();
  CGrid(int rows, int cols, int hgap = 0, int vgap = 0);
  ~CGrid();
  //virtual int Type() const
  //{
  //  return CLASS_LAYOUT;
  //}
  virtual bool Init(int rows, int cols, int hgap = 0, int vgap = 0);
  virtual bool Create(const long chart, const string name, const int subwin,
                      const int x1, const int y1, const int x2, const int y2);
  virtual int Columns()
  {
    return (m_cols);
  }
  virtual void Columns(int cols)
  {
    m_cols = cols;
  }
  virtual int Rows()
  {
    return (m_rows);
  }
  virtual void Rows(int rows)
  {
    m_rows = rows;
  }
  virtual int HGap()
  {
    return (m_hgap);
  }
  virtual void HGap(int gap)
  {
    m_hgap = gap;
  }
  virtual int VGap()
  {
    return (m_vgap);
  }
  virtual void VGap(int gap)
  {
    m_vgap = gap;
  }
  virtual bool Pack();

 protected:
  virtual void CheckControlSize(CWnd *control);
};
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGrid::CGrid()
{
  RTTI;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGrid::CGrid(int rows, int cols, int hgap = 0, int vgap = 0)
{
  Init(rows, cols, hgap, vgap);
  RTTI;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CGrid::~CGrid()
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGrid::Init(int rows, int cols, int hgap = 0, int vgap = 0)
{
  Columns(cols);
  Rows(rows);
  HGap(hgap);
  VGap(vgap);
  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGrid::Create(const long chart, const string name, const int subwin,
                   const int x1, const int y1, const int x2, const int y2)
{
  return (CBox::Create(chart, name, subwin, x1, y1, x2, y2));
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CGrid::Pack()
{
  CSize size = Size();
  m_cell_size.cx = (size.cx - ((m_cols + 1) * m_hgap)) / m_cols;
  m_cell_size.cy = (size.cy - ((m_rows + 1) * m_vgap)) / m_rows;
  int x = Left(), y = Top();
  int cnt = 0;
  for(int i = 0; i < ControlsTotal(); i++)
  {
    CWnd *control = Control(i);
    if(control == NULL)
      continue;
    if(control == GetPointer(m_background))
      continue;
    if(cnt == 0 || Right() - (x + m_cell_size.cx) < m_cell_size.cx + m_hgap)
    {
      if(cnt == 0)
        y += m_vgap;
      else
        y += m_vgap + m_cell_size.cy;
      x = Left() + m_hgap;
    }
    else
      x += m_cell_size.cx + m_hgap;
    CheckControlSize(control);
    control.Move(x, y);
    CBox *container = dynamic_cast<CBox *>(control);
    //if(control.Type() == CLASS_LAYOUT)
    if(container)
    {
      container.Pack();
    }
    cnt++;
  }
  return (true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CGrid::CheckControlSize(CWnd *control)
{
  control.Size(m_cell_size.cx, m_cell_size.cy);
}
//+------------------------------------------------------------------+
