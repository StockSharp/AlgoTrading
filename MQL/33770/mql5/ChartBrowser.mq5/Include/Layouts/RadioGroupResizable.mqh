#include <ControlsPlus/RadioGroup.mqh>
#include "GroupTemplate.mqh"

class RadioGroupResizable: public GroupTemplate<CRadioGroup>
{
  public:
    RadioGroupResizable()
    {
      RTTI;
      WIDTH_ADJUSTMENT = CONTROLS_BUTTON_SIZE;
    }

  protected:
    virtual bool isSelected(const int index) override
    {
      return m_current == index;
    }

    virtual bool createElement(const int index) override
    {
      return CreateButton(index);
    }
};