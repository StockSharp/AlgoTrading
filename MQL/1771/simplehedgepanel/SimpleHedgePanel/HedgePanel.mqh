//+------------------------------------------------------------------+
//|                                                   HedgePanel.mqh |
//|                                            Copyright 2013, Rone. |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone."
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
#include <Controls\Dialog.mqh>
#include <Controls\ComboBox.mqh>
#include <Controls\Label.mqh>
#include <Controls\Button.mqh>
#include <Trade\Trade.mqh>

#include "HedgePanelDefines.mqh"
#include "DoubleSpinEdit.mqh"
#include "DealDataBox.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CHedgePanel : public CAppDialog {
private:
   int               m_total;
   CDealDataBox      m_dealboxes[];
   CButton           m_pos_open;
   CButton           m_pos_close;
   
public:
                     CHedgePanel();
                    ~CHedgePanel();
                    
   bool              Init(const int boxes, const int x1, const int y1);
   //--- create
   virtual bool      Create(const long chart, const string name, const int subwin,
                            const int x1, const int y1, const int x2, const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   
protected:
   bool              CreateDealboxes();
   bool              CreateButtonOpenPositions();
   bool              CreateButtonClosePositions();
   //---
   bool              OnClickPositionsOpen();
   bool              OnClickPositionsClose();
};
//+------------------------------------------------------------------+
//| Event Handling                                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CHedgePanel)
   ON_EVENT(ON_CLICK, m_pos_open, OnClickPositionsOpen)
   ON_EVENT(ON_CLICK, m_pos_close, OnClickPositionsClose)
EVENT_MAP_END(CAppDialog)
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CHedgePanel::CHedgePanel() {
   m_total = 0;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CHedgePanel::~CHedgePanel() {
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CHedgePanel::Init(const int boxes, const int x1, const int y1) {
   if ( boxes < 1 || boxes > 5) {
      m_total = 2;
      Print(__FUNCTION__, ": incorrect pairs value. EA will use value 2.");
   } else {
      m_total = boxes;
   }
   ArrayResize(m_dealboxes, m_total);
   
   int x2 = x1 + INDENT_LEFT + INDENT_RIGHT + m_total * (CONTROLS_WIDTH + 2 * CONTROLS_GAP_X)
            + (m_total - 1) * CONTROLS_GAP_X + 8;
   int y2 = y1 + INDENT_TOP + CONTROLS_GAP_Y + 2 * (CONTROLS_HEIGHT + CONTROLS_GAP_Y)
            + RADIO_HEIGHT + CONTROLS_GAP_Y + CONTROLS_HEIGHT + CONTROLS_GAP_Y + CONTROLS_GAP_Y
            + CONTROLS_HEIGHT + 2 * CONTROLS_GAP_Y + INDENT_BOTTOM + 8;
   string name = (m_total == 1) ? "Simple Panel" : "Simple Hedge Panel";
   
   return(Create(0, name, 0, x1, y1, x2, y2));
}
//+------------------------------------------------------------------+
//| Create                                                           |
//+------------------------------------------------------------------+
bool CHedgePanel::Create(const long chart, const string name, const int subwin,
                         const int x1, const int y1, const int x2, const int y2)
{
   if ( !CAppDialog::Create(chart, name, subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !CreateButtonOpenPositions() ) {
      return(false);
   }
   if ( !CreateButtonClosePositions() ) {
      return(false);
   }
   if ( !CreateDealboxes() ) {
      return(false);
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CHedgePanel::CreateDealboxes(void) {
   int x1 = INDENT_LEFT;
   int y1 = INDENT_TOP;
   int x2 = x1 + CONTROLS_WIDTH + 2 * CONTROLS_GAP_X;
   int y2 = y1 + CONTROLS_GAP_Y + 2 * (CONTROLS_HEIGHT + CONTROLS_GAP_Y)
            + RADIO_HEIGHT + CONTROLS_GAP_Y;
   
   for ( int i = 0; i < m_total; i++ ) {
      if ( !m_dealboxes[i].Create(m_chart_id, m_name+(string)i, m_subwin, x1, y1, x2, y2) ) {
         return(false);
      }
      if ( !Add(m_dealboxes[i]) ) {
         return(false);
      }
      x1 = x2 + CONTROLS_GAP_X;
      x2 = x1 + CONTROLS_WIDTH + 2 * CONTROLS_GAP_X;
   }
   
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CHedgePanel::CreateButtonOpenPositions(void) {
   int x1 = INDENT_LEFT;
   int y1 = INDENT_TOP + CONTROLS_GAP_Y + 2 * (CONTROLS_HEIGHT + CONTROLS_GAP_Y)
            + RADIO_HEIGHT + CONTROLS_GAP_Y + CONTROLS_GAP_Y;
   int x2 = ClientAreaWidth() - INDENT_RIGHT;
   int y2 = y1 + CONTROLS_HEIGHT; 
   
   if ( !m_pos_open.Create(m_chart_id, m_name+"ButtonOpen", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   string text = (m_total == 1) ? "Open Position" : "Open Hedge Positions"; 
   if ( !m_pos_open.Text(text) ) {
      return(false);
   }
   if ( !m_pos_open.ColorBackground(clrLightGreen) ) {
      return(false);
   }
   if ( !Add(m_pos_open) ) {
      return(false);
   }
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CHedgePanel::CreateButtonClosePositions(void) {
   int x1 = INDENT_LEFT;
   int y1 = INDENT_TOP + CONTROLS_GAP_Y + 2 * (CONTROLS_HEIGHT + CONTROLS_GAP_Y)
            + RADIO_HEIGHT + CONTROLS_GAP_Y + CONTROLS_HEIGHT + CONTROLS_GAP_Y + CONTROLS_GAP_Y;
   int x2 = ClientAreaWidth() - INDENT_RIGHT;
   int y2 = y1 + CONTROLS_HEIGHT;
   
   if ( !m_pos_close.Create(m_chart_id, m_name+"ButtonClosePos", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   string text = (m_total == 1) ? "Close Position" : "Close Hedge Positions"; 
   if ( !m_pos_close.Text(text) ) {
      return(false);
   }
   if ( !m_pos_close.ColorBackground(clrKhaki) ) {
      return(false);
   }
   if ( !Add(m_pos_close) ) {
      return(false);
   }
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CHedgePanel::OnClickPositionsOpen(void) {
//---
   for ( int i = 0; i < m_total; i++ ) {
      string cur_sym = m_dealboxes[i].SelectedSymbol();
      
      if ( cur_sym != NO_SYMBOL_SELECTED ) {
         CTrade trade;
         double price;
         ENUM_ORDER_TYPE type = (ENUM_ORDER_TYPE)m_dealboxes[i].OrderType();
         
         price = (type == ORDER_TYPE_BUY) ? SymbolInfoDouble(cur_sym, SYMBOL_ASK) 
                                          : SymbolInfoDouble(cur_sym, SYMBOL_BID);
                  
         if ( !trade.PositionOpen(cur_sym, type, m_dealboxes[i].Lot(), price, 0.0, 0.0) ) {
            Print(__FUNCTION__, ": opening "+cur_sym+" position failed. Error #", GetLastError());
         }
      }
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CHedgePanel::OnClickPositionsClose(void) {
//---
   for ( int i = 0; i < m_total; i++ ) {
      string sym = m_dealboxes[i].SelectedSymbol();
      if ( PositionSelect(sym) ) {
         CTrade trade;
         
         if ( !trade.PositionClose(sym) ) {
            Print(__FUNCTION__, ": closing "+sym+"failed. Error #", GetLastError());
         }
      }
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
