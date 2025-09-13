//+------------------------------------------------------------------+
//|                                            ComboBoxResizable.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include <ControlsPlus/ComboBox.mqh>

class ComboBoxResizable: public CComboBox
{
  public:
    ComboBoxResizable()
    {
      RTTI;
    }
    
    virtual bool OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam) override;

    virtual bool OnResize(void) override
    {
      m_edit.Width(Width());
      
      int x1 = Width() - (CONTROLS_BUTTON_SIZE + CONTROLS_COMBO_BUTTON_X_OFF);
      int y1 = (Height() - CONTROLS_BUTTON_SIZE) / 2;
      m_drop.Move(Left() + x1, Top() + y1);
      
      m_list.Width(Width());

      return CWndContainer::OnResize();
    }
    
    virtual bool OnClickButton(void) override
    {
      // this is a hack to trigger resizing of elements in the list
      // we need it because standard ListView is incorrectly coded in such a way
      // that elements are resized only if vscroll is present
      bool vs = m_list.VScrolled();
      if(m_drop.Pressed())
      {
        m_list.VScrolled(true);
      }
      bool b = CComboBox::OnClickButton();
      m_list.VScrolled(vs);
      return b;
    }
    /*
    virtual bool Enable(void) override
    {
      m_edit.Show();
      m_drop.Show();
      return CComboBox::Enable();
    }
    
    virtual bool Disable(void) override
    {
      m_edit.Hide();
      m_drop.Hide();
      return CComboBox::Disable();
    }*/
};

#define EXIT_ON_DISABLED \
      if(!IsEnabled())   \
      {                  \
        return false;    \
      }

EVENT_MAP_BEGIN(ComboBoxResizable)
  EXIT_ON_DISABLED
  ON_EVENT(ON_CLICK, m_drop, OnClickButton)
EVENT_MAP_END(CComboBox)
