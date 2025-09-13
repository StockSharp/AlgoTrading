//+------------------------------------------------------------------+
//|                                            SpinEditResizable.mqh |
//|                                    Copyright (c) 2019, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

#include <ControlsPlus/SpinEdit.mqh>

class SpinEditResizable: public CSpinEdit
{
  public:
    SpinEditResizable()
    {
      RTTI;
    }
    virtual bool OnResize(void) override
    {
      m_edit.Width(Width());
      m_edit.Height(Height());
      
      int x1 = Width() - (CONTROLS_BUTTON_SIZE + CONTROLS_SPIN_BUTTON_X_OFF);
      int y1 = (Height() - 2 * CONTROLS_SPIN_BUTTON_SIZE) / 2;
      m_inc.Move(Left() + x1, Top() + y1);
      
      x1 = Width() - (CONTROLS_BUTTON_SIZE + CONTROLS_SPIN_BUTTON_X_OFF);
      y1 = (Height() - 2 * CONTROLS_SPIN_BUTTON_SIZE) / 2 + CONTROLS_SPIN_BUTTON_SIZE;
      m_dec.Move(Left() + x1, Top() + y1);

      return CWndContainer::OnResize();
    }
};