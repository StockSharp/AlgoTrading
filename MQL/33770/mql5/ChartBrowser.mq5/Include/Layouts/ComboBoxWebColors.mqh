#include <Layouts/ComboBoxResizable.mqh>
#include "LayoutColors.mqh"

//+------------------------------------------------------------------+
//| Class ComboBoxWebColors                                                  |
//| Usage: drop-down list                                            |
//+------------------------------------------------------------------+
class ComboBoxWebColors : public ComboBoxResizable
{
  public:
                     ComboBoxWebColors(void);
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2) override;
   
  protected:
   virtual bool      OnChangeList(void) override;
   void  FillItems();
};

//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
ComboBoxWebColors::ComboBoxWebColors(void)
{
  RTTI;
  m_list.SetColorMode(true);
}

//+------------------------------------------------------------------+
//| Create a control                                                 |
//+------------------------------------------------------------------+
bool ComboBoxWebColors::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2)
{
  ComboBoxResizable::Create(chart, name, subwin, x1, y1, x2, y2);
  FillItems();
  return(true);
}

//+------------------------------------------------------------------+
//| Handler of click on drop-down list                               |
//+------------------------------------------------------------------+
bool ComboBoxWebColors::OnChangeList(void)
{
  ComboBoxResizable::OnChangeList();
  const color c = (color)Value();
  m_edit.ColorBackground(c == clrNONE ? CONTROLS_EDIT_COLOR_BG : c);
  return(true);
}

void ComboBoxWebColors::FillItems()
{
  for(int i = 0; i < LAYOUT_WEB_COLORS_COUNT; i++)
  {
    ItemAdd(LayoutColors::WebColorNames[i], LayoutColors::WebColorValues[i]);
  }
}
