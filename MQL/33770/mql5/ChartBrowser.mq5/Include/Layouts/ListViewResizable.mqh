#include <ControlsPlus/ListView.mqh>
#include "GroupTemplate.mqh"

class ListViewResizable: public GroupTemplate<CListView>
{
  public:
    ListViewResizable()
    {
      RTTI;
    }
    
    void forceVScroll()
    {
      // make sure the scroll is shown
      // (it may remain hidden in some cases due to a bug in SCL)
      m_scroll_v.Show();
    }

    void adjustVSize()
    {
      OnResize(); // adjust number of rows (objects)
    }
    
  protected:
    virtual bool isSelected(const int index) override
    {
      return m_current == index;
    }

    virtual bool createElement(const int index) override
    {
      return CreateRow(index);
    }
};