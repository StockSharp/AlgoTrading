//+------------------------------------------------------------------+
//|                                                  DealDataBox.mqh |
//|                                            Copyright 2013, Rone. |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone."
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
#property description "Deal Data Box class"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
#include <Controls\ComboBox.mqh>
#include <Controls\RadioGroup.mqh>
#include <Trade\SymbolInfo.mqh>

#include "HedgePanelDefines.mqh"
#include "DoubleSpinEdit.mqh"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CDealDataBox : CWndContainer {
private:
   CPanel            m_background;
   CComboBox         m_symbols;
   CDoubleSpinEdit   m_lot;
   CRadioGroup       m_types;
   
public:
                     CDealDataBox();
                    ~CDealDataBox();
   //--- create
   virtual bool      Create(const long chart, const string name, const int subwin,
                            const int x1, const int y1, const int x2, const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam);
   //---
   string            SelectedSymbol() { return(m_symbols.Select()); }
   double            Lot() { return(m_lot.Value()); }
   long              OrderType();
   
protected:
   bool              CreateBack();
   bool              CreateSymbols();
   bool              CreateLot();
   bool              CreateTypes();
   
   bool              FillSymbols();
   
   void              OnChangeSymbols();
};
//+------------------------------------------------------------------+
//| Common handler of chart events                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CDealDataBox)
   ON_EVENT(ON_CHANGE, m_symbols, OnChangeSymbols)
EVENT_MAP_END(CWndContainer)
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CDealDataBox::CDealDataBox()
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CDealDataBox::~CDealDataBox()
{
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDealDataBox::Create(const long chart, const string name, const int subwin,
                          const int x1, const int y1, const int x2, const int y2)
{
   if ( !CWndContainer::Create(chart, name, subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !CreateBack() ) {
      return(false);
   }
   if ( !CreateLot() ) {
      return(false);
   }
   if ( !CreateTypes() ) {
      return(false);
   }
   if ( !CreateSymbols() ) {
      return(false);
   }   
   
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDealDataBox::CreateBack(void) {
   if( !m_background.Create(m_chart_id, m_name+"Back", m_subwin, 0, 0, Width(), Height()) ) {
      return(false);
   }
   if( !m_background.ColorBorder(DEALBOXES_BG_COLOR_BORDER) ) {
      return(false);
   }
   if( !m_background.ColorBackground(DEALBOXES_COLOR_BG) ) {
      return(false);
   }
   if( !Add(m_background) ) {
      return(false);
   }

   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDealDataBox::CreateSymbols(void) {
   int x1 = CONTROLS_GAP_X;
   int y1 = CONTROLS_GAP_Y;
   int x2 = x1 + CONTROLS_WIDTH;
   int y2 = y1 + CONTROLS_HEIGHT;
   
   if ( !m_symbols.Create(m_chart_id, m_name+"Symbols", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !Add(m_symbols) ) {
      return(false);
   }
   if ( !FillSymbols() ) {
      return(false);
   }
   m_symbols.SelectByValue(0);

   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDealDataBox::CreateLot(void) {
   int x1 = CONTROLS_GAP_X;
   int y1 = 2 * CONTROLS_GAP_Y + CONTROLS_HEIGHT;
   int x2 = x1 + CONTROLS_WIDTH;
   int y2 = y1 + CONTROLS_HEIGHT;
   
   if ( !m_lot.Create(m_chart_id, m_name+"Lot", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !m_lot.ReadOnly(false) ) {
      return(false);
   }
   if ( !Add(m_lot) ) {
      return(false);
   }
   m_lot.SetParameters(0.0, 0.0, 0.0, 0.0, 2);
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDealDataBox::CreateTypes(void) {
   int x1 = CONTROLS_GAP_X;
   int y1 = 3 * CONTROLS_GAP_Y + 2 * CONTROLS_HEIGHT;
   int x2 = x1 + CONTROLS_WIDTH;
   int y2 = y1 + RADIO_HEIGHT;
   
   if ( !m_types.Create(m_chart_id, m_name+"RadioTypes", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !Add(m_types) ) {
      return(false);
   }
   
   if ( !m_types.AddItem("Buy", 0) ) {
      return(false);
   }
   if ( !m_types.AddItem("Sell", 1) ) {
      return(false);
   }
   if ( !m_types.Value(0) ) {
      return(false);
   }
   
   return(true);   
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDealDataBox::FillSymbols(void) {
   int total = SymbolsTotal(true);
   
   if ( !m_symbols.ItemAdd(NO_SYMBOL_SELECTED) ) {
      return(false);
   }
   for ( int i = 0; i < total; i++ ) {
      if ( !m_symbols.ItemAdd(SymbolName(i, true)) ) {
         return(false);
      }
   }
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CDealDataBox::OnChangeSymbols(void) {
//---
   string cur_sym = m_symbols.Select();
   
   if ( cur_sym == NO_SYMBOL_SELECTED ) {
      m_lot.SetParameters(0.0, 0.0, 0.0, 0.0, 2);
   } else {
      CSymbolInfo sym;
      sym.Name(cur_sym);
      m_lot.SetParameters(10*sym.LotsMin(), sym.LotsMin(), sym.LotsMax(), 10*sym.LotsStep(),
                           (int)NormalizeDouble(MathCeil(MathLog10(1/sym.LotsStep())), 0));
   }
//---   
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
long CDealDataBox::OrderType() {
   long type = m_types.Value();
   
   if ( type == 0 ) {
      return(ORDER_TYPE_BUY);
   }
   if ( type == 1 ) {
      return(ORDER_TYPE_SELL);
   }
   return(WRONG_VALUE);
}
//+------------------------------------------------------------------+
