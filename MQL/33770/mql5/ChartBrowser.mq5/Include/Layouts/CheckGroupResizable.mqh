#include <ControlsPlus/CheckGroup.mqh>
#include "GroupTemplate.mqh"

class CheckGroupResizable: public GroupTemplate<CCheckGroup>
{
  public:
    CheckGroupResizable()
    {
      RTTI;
      WIDTH_ADJUSTMENT = CONTROLS_BUTTON_SIZE;
    }

  protected:
    virtual bool isSelected(const int index) override
    {
      return m_states[index] != 0;
    }

    virtual bool createElement(const int index) override
    {
      return CreateButton(index);
    }
};