//+------------------------------------------------------------------+
//|                                               SpinEditDouble.mqh |
//|                   Copyright 2009-2017, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2018, Amr Ali"
#property link      "https://www.mql5.com/en/users/amrali"
#property version   "1.00"
#property description "CSpinEditDouble class is intended for creation of a control, which allows editing a value of double type with a specified step and within specified limitations."
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
#include <Controls\WndContainer.mqh>
#include <Controls\Edit.mqh>
#include <Controls\BmpButton.mqh>
//+------------------------------------------------------------------+
//| Resources                                                        |
//+------------------------------------------------------------------+
#resource "\\Include\\Controls\\res\\SpinInc.bmp"
#resource "\\Include\\Controls\\res\\SpinDec.bmp"
//+------------------------------------------------------------------+
//| Class CSpinEditDouble                                            |
//| Usage: class that implements the "Up-Down" control               |
//+------------------------------------------------------------------+
class CSpinEditDouble : public CWndContainer
  {
private:
   //--- dependent controls
   CEdit             m_edit;                // the entry field object
   CBmpButton        m_inc;                 // the "Increment button" object
   CBmpButton        m_dec;                 // the "Decrement button" object
   //--- adjusted parameters
   double            m_min_value;           // minimum value
   double            m_max_value;           // maximum value
   double            m_step_value;          // stepping value
   int               m_digits;              // rounding digits
   //--- state
   double            m_value;               // current value

public:
                     CSpinEditDouble(void);
                    ~CSpinEditDouble(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   //--- pointer of the edit object
   CEdit            *GetEditPointer(void) { return(::GetPointer(m_edit)); }
   //--- set up
   void              SetParameters(double value,double min,double max,double step,int digits);
   double            MinValue(void) const { return(m_min_value); }
   void              MinValue(const double value);
   double            MaxValue(void) const { return(m_max_value); }
   void              MaxValue(const double value);
   double            StepValue(void) const { return(m_step_value); }
   void              StepValue(const double step) { m_step_value=step; }
   int               Digits(void) const { return(m_digits); }
   void              Digits(const int digits) { m_digits=digits; }
   //--- state
   double            Value(void) const { return(m_value); }
   bool              Value(double value);
   //--- methods for working with files
   virtual bool      Save(const int file_handle);
   virtual bool      Load(const int file_handle);

protected:
   //--- create dependent controls
   virtual bool      CreateEdit(void);
   virtual bool      CreateInc(void);
   virtual bool      CreateDec(void);
   //--- handlers of the dependent controls events
   virtual bool      OnClickInc(void);
   virtual bool      OnClickDec(void);
   virtual bool      OnChangeValue(void);
  };
//+------------------------------------------------------------------+
//| Common handler of chart events                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CSpinEditDouble)
   ON_EVENT(ON_CLICK,m_inc,OnClickInc)
   ON_EVENT(ON_CLICK,m_dec,OnClickDec)
   ON_EVENT(ON_END_EDIT,m_edit,OnChangeValue)
EVENT_MAP_END(CWndContainer)
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CSpinEditDouble::CSpinEditDouble(void) : m_min_value(-DBL_MAX),
                                         m_max_value(DBL_MAX),
                                         m_value(0.0),
                                         m_step_value(1.0),
                                         m_digits(0)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CSpinEditDouble::~CSpinEditDouble(void)
  {
  }
//+------------------------------------------------------------------+
//| Create a control                                                 |
//+------------------------------------------------------------------+
bool CSpinEditDouble::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2)
  {
//--- check height
   if(y2-y1<CONTROLS_SPIN_MIN_HEIGHT)
      return(false);
//--- call method of the parent class
   if(!CWndContainer::Create(chart,name,subwin,x1,y1,x2,y2))
      return(false);
//--- create dependent controls
   if(!CreateEdit())
      return(false);
   if(!CreateInc())
      return(false);
   if(!CreateDec())
      return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CSpinEditDouble::SetParameters(double value,double min,double max,double step,int digits)
  {
   Digits(digits);
   StepValue(step);
   MaxValue(max);
   MinValue(min);
   Value(value);
  }
//+------------------------------------------------------------------+
//| Set current value                                                |
//+------------------------------------------------------------------+
bool CSpinEditDouble::Value(double value)
  {
//--- check value
   if(value<m_min_value)
      value=m_min_value;
   if(value>m_max_value)
      value=m_max_value;
//--- change the current value
   m_value=value;
//--- copy text to the edit field edit
   m_edit.Text(DoubleToString(m_value,m_digits));
//--- send notification
   EventChartCustom(CONTROLS_SELF_MESSAGE,ON_CHANGE,m_id,0.0,m_name);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEditDouble::Save(const int file_handle)
  {
//--- check
   if(file_handle==INVALID_HANDLE)
      return(false);
//---
   FileWriteDouble(file_handle,m_value);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
bool CSpinEditDouble::Load(const int file_handle)
  {
//--- check
   if(file_handle==INVALID_HANDLE)
      return(false);
//---
   if(!FileIsEnding(file_handle))
      Value(FileReadDouble(file_handle));
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Set minimum value                                                |
//+------------------------------------------------------------------+
void CSpinEditDouble::MinValue(const double value)
  {
//--- if value was changed
   if(m_min_value!=value)
     {
      m_min_value=value;
      //--- adjust the edit value
      Value(m_value);
     }
  }
//+------------------------------------------------------------------+
//| Set maximum value                                                |
//+------------------------------------------------------------------+
void CSpinEditDouble::MaxValue(const double value)
  {
//--- if value was changed
   if(m_max_value!=value)
     {
      m_max_value=value;
      //--- adjust the edit value
      Value(m_value);
     }
  }
//+------------------------------------------------------------------+
//| Create the edit field                                            |
//+------------------------------------------------------------------+
bool CSpinEditDouble::CreateEdit(void)
  {
//--- create
   if(!m_edit.Create(m_chart_id,m_name+"Edit",m_subwin,0,0,Width(),Height()))
      return(false);
   if(!m_edit.Text(""))
      return(false);
   if(!m_edit.ReadOnly(false))
      return(false);
   if(!Add(m_edit))
      return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Increment" button                                    |
//+------------------------------------------------------------------+
bool CSpinEditDouble::CreateInc(void)
  {
//--- right align button (try to make equal offsets from top and bottom)
   int x1=Width()-(CONTROLS_BUTTON_SIZE+CONTROLS_SPIN_BUTTON_X_OFF);
   int y1=(Height()-2*CONTROLS_SPIN_BUTTON_SIZE)/2;
   int x2=x1+CONTROLS_BUTTON_SIZE;
   int y2=y1+CONTROLS_SPIN_BUTTON_SIZE;
//--- create
   if(!m_inc.Create(m_chart_id,m_name+"Inc",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_inc.BmpNames("::Include\\Controls\\res\\SpinInc.bmp"))
      return(false);
   if(!Add(m_inc))
      return(false);
//--- property
   m_inc.PropFlags(WND_PROP_FLAG_CLICKS_BY_PRESS);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Decrement" button                                    |
//+------------------------------------------------------------------+
bool CSpinEditDouble::CreateDec(void)
  {
//--- right align button (try to make equal offsets from top and bottom)
   int x1=Width()-(CONTROLS_BUTTON_SIZE+CONTROLS_SPIN_BUTTON_X_OFF);
   int y1=(Height()-2*CONTROLS_SPIN_BUTTON_SIZE)/2+CONTROLS_SPIN_BUTTON_SIZE;
   int x2=x1+CONTROLS_BUTTON_SIZE;
   int y2=y1+CONTROLS_SPIN_BUTTON_SIZE;
//--- create
   if(!m_dec.Create(m_chart_id,m_name+"Dec",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_dec.BmpNames("::Include\\Controls\\res\\SpinDec.bmp"))
      return(false);
   if(!Add(m_dec))
      return(false);
//--- property
   m_dec.PropFlags(WND_PROP_FLAG_CLICKS_BY_PRESS);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Handler of click on the "increment" button                       |
//+------------------------------------------------------------------+
bool CSpinEditDouble::OnClickInc(void)
  {
//--- try to increment current value
   return(Value(StringToDouble(DoubleToString(m_value+m_step_value,m_digits))));
  }
//+------------------------------------------------------------------+
//| Handler of click on the "decrement" button                       |
//+------------------------------------------------------------------+
bool CSpinEditDouble::OnClickDec(void)
  {
//--- try to decrement current value
   return(Value(StringToDouble(DoubleToString(m_value-m_step_value,m_digits))));
  }
//+------------------------------------------------------------------+
//| Handler of changing current state                                |
//+------------------------------------------------------------------+
bool CSpinEditDouble::OnChangeValue(void)
  {
//--- try to change current value
   return(Value(StringToDouble(m_edit.Text())));
  }
//+------------------------------------------------------------------+
