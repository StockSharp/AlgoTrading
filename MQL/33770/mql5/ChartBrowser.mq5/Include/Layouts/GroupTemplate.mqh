//+------------------------------------------------------------------+
//|                                                GroupTemplate.mqh |
//|                                    Copyright (c) 2020, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| Core CheckGroupResizable, RadioGroupResizable, ListViewResizable |
//+------------------------------------------------------------------+

template<typename T>
class GroupTemplate: public T // T = CCheckGroup, CRadioGroup, CListView
{
  protected:
    int WIDTH_ADJUSTMENT;

  public:
    GroupTemplate(): WIDTH_ADJUSTMENT(0)
    {
      RTTI;
    }

  protected:
    virtual bool isSelected(const int index) = 0;
    virtual bool createElement(const int index) = 0;
    
    virtual bool Redraw(void) override
    {
      for(int i = 0; i < ArraySize(m_rows); i++)
      {
        if(i + m_offset < m_strings.Total() && i < m_total_view)
        {
          m_rows[i].Show();
          CRect r;
          r.left = Left() + CONTROLS_BORDER_WIDTH;
          r.top = Top() + CONTROLS_BORDER_WIDTH + m_item_height * i;
          r.right = Right() - 2*CONTROLS_BORDER_WIDTH - 1 /*- WIDTH_ADJUSTMENT*/ - (m_scroll_v.IsVisible() ? CONTROLS_SCROLL_SIZE : 0);
          r.bottom = r.top + m_item_height;
          
          m_rows[i].Move(r.left, r.top);
          m_rows[i].Size(r.right - r.left, r.bottom - r.top);
          
          m_rows[i].Text(m_strings[i + m_offset]);
          RowState(i, isSelected(i + m_offset));
        }
        else
        {
          m_rows[i].Hide();
        }
      }
      return true;
    }
  
    virtual bool OnResize(void) override
    {
      if(!IsVisible()) return true;

      int new_total_view = (Height() - 2 * CONTROLS_BORDER_WIDTH) / m_item_height;

      // if minimized/hidden
      if(new_total_view < 0) return T::OnResize();
      
      const int n = ArraySize(m_rows);
      
      if(new_total_view > n                     // first time expanding
      && n < m_strings.Total())
      {
        const int m = MathMin(m_strings.Total(), new_total_view);

        ArrayResize(m_rows, m);
        for(int i = n; i < m; i++)
        {
          if(!createElement(i)) return(false);
          m_rows[i].Show();
          m_rows[i].Id(rand() | (rand() << 32));
        }
        m_total_view = m;
        m_scroll_v.MaxPos(m_strings.Total() - m_total_view + m_offset);
      }
      else
      if(new_total_view < m_total_view)
      {
        for(int i = new_total_view; i < n; i++)
        {
          m_rows[i].Hide();
        }
        m_total_view = new_total_view;
        m_scroll_v.MaxPos(m_strings.Total() - m_total_view + m_offset);
      }
      else
      if(new_total_view > m_total_view
      && new_total_view <= n)                // second time expanding
      {
        for(int i = m_total_view; i < new_total_view; i++)
        {
          m_rows[i].Show();
        }
        m_total_view = new_total_view;
        m_scroll_v.MaxPos(m_strings.Total() - m_total_view + m_offset);
      }
      Redraw();
      
      return T::OnResize();
    }
};