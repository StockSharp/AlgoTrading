//+------------------------------------------------------------------+
//|                                     SpaceX_EA_Delete_StopLoss.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright SpaceX 2021, MetaQuotes Ltd."
#property link      "https://www.mql5.com"
#property version   "1.00"
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

//---SpaceX---//
#include <Trade\PositionInfo.mqh>
#include <Trade\Trade.mqh>
CPositionInfo  m_position;                   // trade position object
CTrade         m_trade;                      // trading object
#include <Controls\Dialog.mqh>
#include <Controls\Button.mqh>
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
//--- indents and gaps
#define INDENT_LEFT                         (11)      // indent from left (with allowance for border width)
#define INDENT_TOP                          (11)      // indent from top (with allowance for border width)
//--- for buttons
#define BUTTON_WIDTH                        (70)      // size by X coordinate
#define BUTTON_HEIGHT                       (20)      // size by Y coordinate
//+------------------------------------------------------------------+
//| Class CControlsDialog                                            |
//| Usage: main dialog of the Controls application                   |
//+------------------------------------------------------------------+
class CControlsDialog : public CAppDialog
  {
private:
   CButton           m_button_close_all;              // the button object

public:
                     CControlsDialog(void);
                    ~CControlsDialog(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);

protected:
   //--- create dependent controls
   bool              CreateButtonCloseAll(void);
   //--- handlers of the dependent controls events
   void              OnClickButtonCloseAll(void);
   //--- close positions
   void              ClosePositions(void);
  };
//+------------------------------------------------------------------+
//| Event Handling                                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CControlsDialog)
ON_EVENT(ON_CLICK,m_button_close_all,OnClickButtonCloseAll)
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
   if(!CreateButtonCloseAll())
      return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Close all" button                                    |
//+------------------------------------------------------------------+
bool CControlsDialog::CreateButtonCloseAll(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP;
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!m_button_close_all.Create(m_chart_id,m_name+"ButtonCloseAll",m_subwin,x1,y1,x2+x2,y2))
      return(false);
   if(!m_button_close_all.Text("DELETE SL_TP"))
      return(false);
   if(!Add(m_button_close_all))
      return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CControlsDialog::OnClickButtonCloseAll(void)
  {
   ClosePositions();
  }
//+------------------------------------------------------------------+
//| Close positions                                                  |
//+------------------------------------------------------------------+
void CControlsDialog::ClosePositions(void)
  {  
  
           bool errors=false;
   for(int i=PositionsTotal()-1; i>=0; i--) // returns the number of open positions
      if(m_position.SelectByIndex(i))
         if(!m_trade.PositionModify(m_position.Ticket(),0.0,0.0))
            errors=true;

  
  }
//+------------------------------------------------------------------+
//| Global Variables                                                 |
//+------------------------------------------------------------------+
CControlsDialog ExtDialog;
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   m_trade.SetMarginMode();
//--- create application dialog
   if(!ExtDialog.Create(0,"DELETE BUTTON",0,20,40,200,111))
      return(INIT_FAILED);
//--- run application
   ExtDialog.Run();
//--- succeed

//---




   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   Comment("");
//--- destroy dialog
   ExtDialog.Destroy(reason);
  }
//+------------------------------------------------------------------+
//| Expert chart event function                                      |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,         // event ID  
                  const long& lparam,   // event parameter of the long type
                  const double& dparam, // event parameter of the double type
                  const string& sparam) // event parameter of the string type
  {
   ExtDialog.ChartEvent(id,lparam,dparam,sparam);
  }
//+------------------------------------------------------------------+



//----------------------------------------------------------------------------------------------------------------------
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
//---

  }
//+------------------------------------------------------------------+
