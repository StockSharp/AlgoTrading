//+------------------------------------------------------------------+
//|                                               DoubleSpinEdit.mqh |
//|                                             Copyright 2013, Rone |
//|                                            rone.sergey@gmail.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2013, Rone"
#property link      "rone.sergey@gmail.com"
#property version   "1.00"
//---
#include <Controls\WndContainer.mqh>
#include <Controls\Edit.mqh>
#include <Controls\BmpButton.mqh>
//+------------------------------------------------------------------+
//| Resources                                                        |
//+------------------------------------------------------------------+
//#resource "\\Include\\Controls\\res\\SpinInc.bmp"
//#resource "\\Include\\Controls\\res\\SpinDec.bmp"

#resource "res\\SpinInc.bmp"
#resource "res\\SpinDec.bmp"
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
class CDoubleSpinEdit : public CWndContainer {
private:
   CEdit             m_edit;
   CBmpButton        m_inc;
   CBmpButton        m_dec;
   //---
   double            m_min_value;
   double            m_max_value;
   double            m_step;
   int               m_digits;   
   double            m_value;
   
public:
                     CDoubleSpinEdit();
                    ~CDoubleSpinEdit();
   virtual bool      Create(const long chart, const string name, const int subwin,
                            const int x1, const int y1, const int x2, const int y2);
   virtual bool      OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam);
   //---
   void              SetParameters(double value, double min, double max, double step, int digits);
   double            MinValue(void) const { return(m_min_value); }
   void              MinValue(const double value);
   double            MaxValue(void) const { return(m_max_value); }
   void              MaxValue(const double value);
   double            Step(void) const { return(m_step); }
   void              Step(const double step);
   int               Digits(void) const { return(m_digits); }
   void              Digits(const int digits);
   double            Value(void) const { return(m_value); }
   bool              Value(double value);
   bool              ReadOnly(void) const { return(m_edit.ReadOnly()); }
   bool              ReadOnly(const bool flag) { return(m_edit.ReadOnly(flag)); }
   ENUM_ALIGN_MODE   TextAlign(void) const { return(m_edit.TextAlign()); }
   bool              TextAlign(const ENUM_ALIGN_MODE align) { return(m_edit.TextAlign(align)); }
   //---
   virtual bool      Save(const int file_handle);
   virtual bool      Load(const int file_handle);
   
protected:
   virtual bool      CreateEdit(void);
   virtual bool      CreateInc(void);
   virtual bool      CreateDec(void);
   virtual bool      OnEndEdit(void);
   virtual bool      OnClickInc(void);
   virtual bool      OnClickDec(void);
   virtual bool      OnChangeValue(void);
};
//+------------------------------------------------------------------+
//| Common handler of chart events                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CDoubleSpinEdit)
   ON_EVENT(ON_END_EDIT, m_edit, OnEndEdit)
   ON_EVENT(ON_CLICK,m_inc,OnClickInc)
   ON_EVENT(ON_CLICK,m_dec,OnClickDec)
EVENT_MAP_END(CWndContainer)
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CDoubleSpinEdit::CDoubleSpinEdit() {
   m_min_value = 0.0;
   m_max_value = 0.0;
   m_step = 0.0;
   m_value = 0.0;
   m_digits = 0;
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
CDoubleSpinEdit::~CDoubleSpinEdit()
  {
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::Create(const long chart, const string name, const int subwin,
                             const int x1,const int y1,const int x2,const int y2)
{
//---
   if ( y2 - y1 < CONTROLS_SPIN_MIN_HEIGHT ) {
      return(false);
   }
//---
   if ( !CWndContainer::Create(chart, name, subwin, x1, y1, x2, y2) ) {
      return(false);
   }
//---
   if ( !CreateEdit() ) {
      return(false);
   }
   if ( !CreateInc() ) {
      return(false);
   }
   if ( !CreateDec() ) {
      return(false);
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CDoubleSpinEdit::SetParameters(double value, double min, double max, double step, int digits) {
   Digits(digits);
   MinValue(min);
   MaxValue(max);
   Value(value);
   Step(step);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::Value(double value) {
//---
   if ( value < m_min_value ) {
      value = m_min_value;
   }
   if ( value > m_max_value ) {
      value = m_max_value;
   }
   m_value = value;
//---   
   return(OnChangeValue());
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::Save(const int file_handle) {
//---
   if ( file_handle == INVALID_HANDLE ) {
      return(false);
   }
   FileWriteDouble(file_handle, m_value);
   return(true);
//---
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::Load(const int file_handle) {
//---
   if ( file_handle == INVALID_HANDLE ) {
      return(false);
   }
   
   if ( !FileIsEnding(file_handle) ) {
      Value(FileReadDouble(file_handle));
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CDoubleSpinEdit::MinValue(const double value) {
//---
   if ( m_min_value != value ) {
      m_min_value = value;
      Value(m_value);
   }
//---
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CDoubleSpinEdit::MaxValue(const double value) {
//---
   if ( m_max_value != value ) {
      m_max_value = value;
      Value(m_value);
   }
//---
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CDoubleSpinEdit::Step(const double step) {
   if ( m_step != step ) {
      m_step = step;
   }
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CDoubleSpinEdit::Digits(const int digits) {
   if ( m_digits != digits ) {
      m_digits = digits;
   }
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::CreateEdit(void) {
//---
   if ( !m_edit.Create(m_chart_id, m_name+"Edit", m_subwin, 0, 0, Width(), Height()) ) {
      return(false);
   }
   if ( !m_edit.Text("") ) {
      return(false);
   }
   if ( !m_edit.ReadOnly(true) ) {
      return(false);
   }
   if ( !Add(m_edit) ) {
      return(false);
   }
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::CreateInc(void) {
//---
   int x1 = Width() - (CONTROLS_BUTTON_SIZE + CONTROLS_SPIN_BUTTON_X_OFF);
   int y1 = (Height() - 2 * CONTROLS_SPIN_BUTTON_SIZE) / 2;
   int x2 = x1 + CONTROLS_BUTTON_SIZE;
   int y2 = y1 + CONTROLS_SPIN_BUTTON_SIZE;
//---
   if ( !m_inc.Create(m_chart_id, m_name+"Inc", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !m_inc.BmpNames("::res\\SpinInc.bmp") ) {
      return(false);
   }
   if ( !Add(m_inc) ) {
      return(false);
   }
   
   m_inc.PropFlags(WND_PROP_FLAG_CLICKS_BY_PRESS);
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::CreateDec(void) {
//---
   int x1 = Width() - (CONTROLS_BUTTON_SIZE + CONTROLS_SPIN_BUTTON_X_OFF);
   int y1 = (Height() - 2 * CONTROLS_SPIN_BUTTON_SIZE) / 2 + CONTROLS_SPIN_BUTTON_SIZE;
   int x2 = x1 + CONTROLS_BUTTON_SIZE;
   int y2 = y1 + CONTROLS_SPIN_BUTTON_SIZE;
//---
   if ( !m_dec.Create(m_chart_id, m_name+"Dec", m_subwin, x1, y1, x2, y2) ) {
      return(false);
   }
   if ( !m_dec.BmpNames("::res\\SpinDec.bmp") ) {
      return(false);
   }
   if ( !Add(m_dec) ) {
      return(false);
   }
   
   m_dec.PropFlags(WND_PROP_FLAG_CLICKS_BY_PRESS);
//---
   return(true);
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::OnEndEdit(void) {
   return(Value(StringToDouble(m_edit.Text())));
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::OnClickInc(void) {
   return(Value(m_value+m_step));
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::OnClickDec(void) {
   return(Value(m_value-m_step));
}
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CDoubleSpinEdit::OnChangeValue(void) {
//---
   m_edit.Text(DoubleToString(m_value, m_digits));
   EventChartCustom(m_chart_id, ON_CHANGE, m_id, 0.0, m_name);
//---
   return(true);
}
//+------------------------------------------------------------------+
