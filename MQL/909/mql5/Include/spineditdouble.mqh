//+------------------------------------------------------------------+
//|                                               SpinEditDouble.mqh |
//|                        Copyright 2012, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#include "Controls\WndContainer.mqh"
#include "Controls\Edit.mqh"
#include "Controls\BmpButton.mqh"
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
   //--- state
   double            m_value;               // current value

public:
                     CSpinEditDouble(void);
                    ~CSpinEditDouble(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   //--- set up
   double            MinValue(void) const { return(m_min_value); }
   void              MinValue(const double value);
   double            MaxValue(void) const { return(m_min_value); }
   void              MaxValue(const double value);
   //--- state
   double            Value(void) const { return(m_value);     }
   bool              Value(double value);

protected:
   //--- create dependent controls
   virtual bool      CreateEdit(void);
   virtual bool      CreateInc(void);
   virtual bool      CreateDec(void);
   //--- handlers of the dependent controls events
   virtual bool      OnClickInc(void);
   virtual bool      OnClickDec(void);
   virtual bool      OnClickEdit(void);
   //--- internal event handlers
   virtual bool      OnChangeValue(void);
  };
//+------------------------------------------------------------------+
//| Common handler of chart events                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CSpinEditDouble)
ON_EVENT(ON_CLICK,m_inc,OnClickInc)
ON_EVENT(ON_CLICK,m_dec,OnClickDec)
ON_EVENT(ON_CLICK,m_edit,OnClickEdit)
EVENT_MAP_END(CWndContainer)
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CSpinEditDouble::CSpinEditDouble(void) : m_min_value(0),
                                         m_max_value(0),
                                         m_value(0)
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
   if(y2-y1<CONTROLS_SPIN_MIN_HEIGHT)                                           return(false);
//--- call method of the parent class
   if(!CWndContainer::Create(chart,name,subwin,x1,y1,x2,y2))                    return(false);
//--- create dependent controls
   if(!CreateEdit())                                                            return(false);
   if(!CreateInc())                                                             return(false);
   if(!CreateDec())                                                             return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Set current value                                                |
//+------------------------------------------------------------------+
bool CSpinEditDouble::Value(double value)
  {
//--- check value
   if(value<m_min_value) value=m_min_value;
   if(value>m_max_value) value=m_max_value;
//--- if value was changed
   if(m_value!=value)
     {
      m_value=value;
      //--- call virtual handler
      return(OnChangeValue());
     }
//--- value has not been changed
   return(false);
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
      Value((double)m_value);
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
   if(!m_edit.Create(m_chart_id,m_name+"Edit",m_subwin,0,0,Width(),Height()))   return(false);
   if(!m_edit.Text(""))                                                         return(false);
   if(!m_edit.ReadOnly(false))                                                  return(false);
   if(!Add(m_edit))                                                             return(false);
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
   if(!m_inc.Create(m_chart_id,m_name+"Inc",m_subwin,x1,y1,x2,y2))              return(false);
   if(!m_inc.BmpNames("::res\\SpinInc.bmp"))                                    return(false);
   if(!Add(m_inc))                                                              return(false);
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
   if(!m_dec.Create(m_chart_id,m_name+"Dec",m_subwin,x1,y1,x2,y2))              return(false);
   if(!m_dec.BmpNames("::res\\SpinDec.bmp"))                                    return(false);
   if(!Add(m_dec))                                                              return(false);
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
   if(!Value(m_value+0.01)) return(true);
//--- if value was changed, send notification
   return(EventChartCustom(m_chart_id,ON_CHANGE,m_id,0.0,m_name));
  }
//+------------------------------------------------------------------+
//| Handler of click on the "decrement" button                       |
//+------------------------------------------------------------------+
bool CSpinEditDouble::OnClickDec(void)
  {
//--- try to decrement current value
   if(!Value(m_value-0.01)) return(true);
//--- if value was changed, send notification
   return(EventChartCustom(m_chart_id,ON_CHANGE,m_id,0.0,m_name));
  }
//+------------------------------------------------------------------+
//| Handler of click on the "text" field                             |
//+------------------------------------------------------------------+
bool CSpinEditDouble::OnClickEdit(void)
  {
//--- try to decrement current value
   if(!Value(m_value)) return(true);
//--- if value was changed, send notification
   return(EventChartCustom(m_chart_id,ON_CHANGE,m_id,0.0,m_name));
  }
//+------------------------------------------------------------------+
//| Handler of changing current state                                |
//+------------------------------------------------------------------+
bool CSpinEditDouble::OnChangeValue(void)
  {
//--- copy text to the edit field edit
   return(m_edit.Text(DoubleToString(m_value,2)));
  }
//+------------------------------------------------------------------+
