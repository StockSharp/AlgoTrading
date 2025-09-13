//+------------------------------------------------------------------+
//|                                                Trading Panel.mq4 |
//|                                      Copyright 2021, SignalForex |
//|                                           https://signalforex.id |
//+------------------------------------------------------------------+
#property copyright "Copyright 2021, SignalForex"
#property link      "https://signalforex.id"
#property version   "1.00"
#property strict
/*
   Trading Panel ini dibuat untuk tujuan belajar
   Dengan memahami dasar dari pembuatan tombol, label dan comboBox
   serta events nya akan memudahkan kita dalam pengembangan lebih lanjut.
   
   Silahkan join group sharing dan edukasi code mql4 
   t.me/codeMQL
   
*/

//Control Radio Group=====================================================
#include <Controls\Dialog.mqh>
#include <Controls\Button.mqh>
#include <Controls\Label.mqh>
#include <Controls\ComboBox.mqh>
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
//--- indents and gaps
#define INDENT_LEFT                         (11)      // indent from left (with allowance for border width)
#define INDENT_TOP                          (11)      // indent from top (with allowance for border width)
#define INDENT_RIGHT                        (11)      // indent from right (with allowance for border width)
#define INDENT_BOTTOM                       (11)      // indent from bottom (with allowance for border width)
#define CONTROLS_GAP_X                      (5)       // gap by X coordinate
#define CONTROLS_GAP_Y                      (5)       // gap by Y coordinate
//--- for buttons
#define BUTTON_WIDTH                        (105)     // size by X coordinate
#define BUTTON_HEIGHT                       (25)      // size by Y coordinate
//--- for the indication area
#define EDIT_HEIGHT                         (20)      // size by Y coordinate
//--- for group controls
#define GROUP_WIDTH                         (180)     // size by X coordinate
#define LIST_HEIGHT                         (179)     // size by Y coordinate
#define RADIO_HEIGHT                        (56)      // size by Y coordinate
#define CHECK_HEIGHT                        (93)      // size by Y coordinate
//+------------------------------------------------------------------+
//| Class CControlsDialog                                            |
//| Usage: main dialog of the Controls application                   |
//+------------------------------------------------------------------+
class CControlsDialog : public CAppDialog
{
private:
   
   CButton           m_buttonM1, m_buttonM5, m_buttonM15, m_buttonM30, m_buttonH1, m_buttonH4, m_buttonD1, m_buttonW1;                       // the button object 
   CLabel            m_label, m_labelSFx;    //CLabel object
   CComboBox         m_comboPair;
   
public:
                     CControlsDialog(void);
                    ~CControlsDialog(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   

 
protected:
   //--- create dependent controls
   
   bool              CreateButtonTF(void);
   
   void              OnClickButtonM1(void);
   void              OnClickButtonM5(void);
   void              OnClickButtonM15(void);
   void              OnClickButtonM30(void);
   void              OnClickButtonH1(void);
   void              OnClickButtonH4(void);
   void              OnClickButtonD1(void);
   void              OnClickButtonW1(void);
   
   //--- create dependent controls
   bool              CreateLabel(void);
   //--- handlers of the dependent controls events
   void              OnClickLabel(void);
   
   bool              CreateCombo(void);
   void              OnChangeComboBox(void);
   
};
//+------------------------------------------------------------------+
//| Event Handling                                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CControlsDialog)

ON_EVENT(ON_CLICK,m_buttonM1,OnClickButtonM1)
ON_EVENT(ON_CLICK,m_buttonM5,OnClickButtonM5)
ON_EVENT(ON_CLICK,m_buttonM15,OnClickButtonM15)
ON_EVENT(ON_CLICK,m_buttonM30,OnClickButtonM30)
ON_EVENT(ON_CLICK,m_buttonH1,OnClickButtonH1)
ON_EVENT(ON_CLICK,m_buttonH4,OnClickButtonH4)
ON_EVENT(ON_CLICK,m_buttonD1,OnClickButtonD1)
ON_EVENT(ON_CLICK,m_buttonW1,OnClickButtonW1)
ON_EVENT(ON_CHANGE,m_comboPair,OnChangeComboBox)

EVENT_MAP_END(CAppDialog)
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CControlsDialog::CControlsDialog(void)
{
}
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CControlsDialog::~CControlsDialog(void)
{
}
//+------------------------------------------------------------------+
//| Create                                                           |
//+------------------------------------------------------------------+
bool CControlsDialog::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2)
{
   if(!CAppDialog::Create(chart,name,subwin,x1,y1,x2,y2))
      return(false);
//--- create dependent controls

   
   if(!CreateButtonTF())
      return(false);
   
   if(!CreateLabel())
      return(false);
   
   if (!CreateCombo())
      return(false);
      
//--- succeed
   return(true);
}


bool CControlsDialog::CreateButtonTF(void)
{
  //m_buttonM1, m_buttonM5, m_buttonM15, m_buttonM30, m_buttonMH1, m_buttonMH4, m_buttonMD1
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=10 + (2*CONTROLS_GAP_Y+BUTTON_HEIGHT);
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!m_buttonM1.Create(m_chart_id,m_name+"M1",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonM1.Text("M1"))
      return(false);
   if(!Add(m_buttonM1))
      return(false);
      
   x1=x2+CONTROLS_GAP_X;
   x2=x1+BUTTON_WIDTH;
   ////y1=y2+CONTROLS_GAP_Y;
   ////y2=y1+BUTTON_HEIGHT;
   if(!m_buttonM5.Create(m_chart_id,m_name+"M5",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonM5.Text("M5"))
      return(false);
   if(!Add(m_buttonM5))
      return(false);   
   
   x1=INDENT_LEFT;
   x2=x1+BUTTON_WIDTH;
   y1=y2+CONTROLS_GAP_Y;
   y2=y1+BUTTON_HEIGHT;
   if(!m_buttonM15.Create(m_chart_id,m_name+"M15",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonM15.Text("M15"))
      return(false);
   if(!Add(m_buttonM15))
      return(false);   
   
   x1=x2+CONTROLS_GAP_X;
   x2=x1+BUTTON_WIDTH;
   if(!m_buttonM30.Create(m_chart_id,m_name+"M30",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonM30.Text("M30"))
      return(false);
   if(!Add(m_buttonM30))
      return(false);   
   
   
   x1=INDENT_LEFT;
   x2=x1+BUTTON_WIDTH;
   y1=y2+CONTROLS_GAP_Y;
   y2=y1+BUTTON_HEIGHT;
   if(!m_buttonH1.Create(m_chart_id,m_name+"H1",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonH1.Text("H1"))
      return(false);
   if(!Add(m_buttonH1))
      return(false);   
   
   
   x1=x2+CONTROLS_GAP_X;
   x2=x1+BUTTON_WIDTH;
   if(!m_buttonH4.Create(m_chart_id,m_name+"H4",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonH4.Text("H4"))
      return(false);
   if(!Add(m_buttonH4))
      return(false);   
   
   x1=INDENT_LEFT;
   x2=x1+BUTTON_WIDTH;
   y1=y2+CONTROLS_GAP_Y;
   y2=y1+BUTTON_HEIGHT;
   if(!m_buttonD1.Create(m_chart_id,m_name+"D1",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonD1.Text("D1"))
      return(false);
   if(!Add(m_buttonD1))
      return(false);   
   
   
   x1=x2+CONTROLS_GAP_X;
   x2=x1+BUTTON_WIDTH;
   if(!m_buttonW1.Create(m_chart_id,m_name+"W1",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!m_buttonW1.Text("W1"))
      return(false);
   if(!Add(m_buttonW1))
      return(false);
      
         
//--- succeed
   return(true);
}

//+------------------------------------------------------------------+
//| Create the "CLabel"                                              |
//+------------------------------------------------------------------+
bool CControlsDialog::CreateLabel(void)
{
//--- coordinates
   int x1=INDENT_RIGHT;
   int y1=INDENT_TOP+CONTROLS_GAP_Y;
   int x2=x1+100;
   int y2=y1+20;
//--- create
   if(!m_label.Create(m_chart_id,m_name+"Label",m_subwin,x1,y1,x2,y2))
      return(false);
      
   if(!m_label.Text(" Pair "))
      return(false);
   if(!Add(m_label))
      return(false);
   
   
   y1 = 170;
   if(!m_labelSFx.Create(m_chart_id,m_name+"labelSFx",m_subwin,x1,y1,x2,y2))
      return(false);
      
   if(!m_labelSFx.Text("Join us: t.me/codeMQL"))
      return(false);
   if(!Add(m_labelSFx))
      return(false);
      
      
      
//--- succeed
   return(true);
}
  
  
bool CControlsDialog::CreateCombo(void)
{
//--- coordinates
   int x1=INDENT_LEFT + CONTROLS_GAP_X + BUTTON_WIDTH;
   int y1=INDENT_TOP+CONTROLS_GAP_Y;
   int x2=x1+100;
   int y2=y1+20;
//--- create
   
   if(!m_comboPair.Create(m_chart_id,m_name+"ComboPair",m_subwin,x1,y1,x2,y2))
      return(false);
   if(!Add(m_comboPair))
      return(false);
//--- fill out with strings
   int nPairs  = SymbolsTotal(true);
   for(int i=0;i<nPairs;i++)
      if(!m_comboPair.ItemAdd(SymbolName(i, true)))
         return(false);
   
   m_comboPair.SelectByText(Symbol());
//--- succeed
   return(true);
}
  
  

//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CControlsDialog::OnClickButtonM1(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_M1);
}
void CControlsDialog::OnClickButtonM5(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_M5);
}
void CControlsDialog::OnClickButtonM15(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_M15);
}
void CControlsDialog::OnClickButtonM30(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_M30);
}
void CControlsDialog::OnClickButtonH1(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_H1);
}
void CControlsDialog::OnClickButtonH4(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_H4);
}
void CControlsDialog::OnClickButtonD1(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_D1);
}
void CControlsDialog::OnClickButtonW1(void)
{
   ChartSetSymbolPeriod(0, Symbol(), PERIOD_W1);
}

void CControlsDialog::OnChangeComboBox(void)
{
   ChartSetSymbolPeriod(0, m_comboPair.Select(), PERIOD_CURRENT);
}
  
  
//+------------------------------------------------------------------+
//| Global Variables                                                 |
//+------------------------------------------------------------------+
CControlsDialog ExtDialog;

//================= end Control Radio Group ================================


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
{
//---

   //--- create application dialog
   if (ExtDialog.Name() == NULL){
      if(!ExtDialog.Create(0,"Trading Panel - SignalForex.id",0,40,40,285,270)){
         Print ("ERROR: GAGAL CREATE");
      }else{
         //--- run application
         ExtDialog.Run();
      }
   }
   
//---
   return(INIT_SUCCEEDED);
}
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
{
//---
   Print (__FUNCTION__, " Terima Kasih - SignalForex.id");
//--- destroy dialog
   //ExtDialog.Destroy(reason);
}
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
{
//---
   
}
//+------------------------------------------------------------------+


void OnChartEvent(const int id,         // event ID  
                  const long& lparam,   // event parameter of the long type
                  const double& dparam, // event parameter of the double type
                  const string& sparam) // event parameter of the string type
{
   ExtDialog.ChartEvent(id,lparam,dparam,sparam);
}