//+------------------------------------------------------------------+
//|                                      SymbolSynthesizerDialog.mqh |
//|                        Copyright 2011, MetaQuotes Software Corp. |
//|                                              http://www.mql5.com |
//+------------------------------------------------------------------+
#include <Controls\Dialog.mqh>
#include <Controls\Button.mqh>
#include <Controls\Edit.mqh>
#include <Controls\Label.mqh>
#include <Controls\ListView.mqh>
#include <Controls\ComboBox.mqh>
#include <Controls\SpinEdit.mqh>
#include <Controls\RadioGroup.mqh>
#include <Controls\CheckGroup.mqh>
#include <SpinEditDouble.mqh>
#include <Trade\Trade.mqh>
//+------------------------------------------------------------------+
//| defines                                                          |
//+------------------------------------------------------------------+
//--- indents and gaps
#define INDENT_LEFT                         (11)      // indent from left (with allowance for border width)
#define INDENT_TOP                          (11)      // indent from top (with allowance for border width)
#define INDENT_RIGHT                        (11)      // indent from right (with allowance for border width)
#define INDENT_BOTTOM                       (11)      // indent from bottom (with allowance for border width)
#define CONTROLS_GAP_X                      (15)      // gap by X coordinate
#define CONTROLS_GAP_Y                      (5)       // gap by Y coordinate
//--- for labels
#define LABEL_WIDTH                         (100)     // size by X coordinate
//--- for buttons
#define BUTTON_WIDTH                        (125)     // size by X coordinate
#define BUTTON_HEIGHT                       (20)      // size by Y coordinate
//--- for the indication area
#define EDIT_HEIGHT                         (20)      // size by Y coordinate
//--- for group controls
#define GROUP_WIDTH                         (150)     // size by X coordinate
#define LIST_HEIGHT                         (204)     // size by Y coordinate
#define RADIO_HEIGHT                        (56)      // size by Y coordinate
#define CHECK_HEIGHT                        (93)      // size by Y coordinate
//--- 
int     SymNum;
double  vBID,vASK,vol,slippage;
//+------------------------------------------------------------------+
//| Class CSymbolSynthesizerDialog                                            |
//| Usage: main dialog of the SymbolSynthesizer application                   |
//+------------------------------------------------------------------+
class CSymbolSynthesizerDialog : public CAppDialog
  {
private:
   CLabel            m_label1;                         // the label1 object
   CLabel            m_label2;                         // the label2 object
   CLabel            m_label3;                         // the label3 object
   
   CComboBox         m_combo_box;                      // the dropdown list object
   
   CEdit             m_edit1;                          // the display field1 object
   CEdit             m_edit2;                          // the display field2 object
   CEdit             m_edit3;                          // the display field3 object
   
   CSpinEditDouble   m_spin_edit1;                     // the up-down1 object
   CSpinEdit         m_spin_edit2;                     // the up-down2 object
   
   CButton           m_button1;                        // the button sell object
   CButton           m_button2;                        // the button buy object
   
   CTrade            m_trade;                          // trading object

public:
                     CSymbolSynthesizerDialog(void);
                    ~CSymbolSynthesizerDialog(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
   //--- chart event handler
   virtual bool      OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
   //---
   void              TickChange(void);
   //---
   double            Call_Trade(string symb,int pos,string com);

protected:
   //--- create dependent controls
   bool              CreateLabel1(void);
   bool              CreateComboBox(void);
   bool              CreateEdit1(void);
   bool              CreateEdit2(void);
   bool              CreateEdit3(void);
   bool              CreateLabel2(void);
   bool              CreateSpinEdit1(void);
   bool              CreateLabel3(void);
   bool              CreateSpinEdit2(void);
   bool              CreateButton1(void);
   bool              CreateButton2(void);
   //--- handlers of the dependent controls events
   void              OnChangeComboBox(void);
   void              OnChangeSpinEdit1(void);
   void              OnChangeSpinEdit2(void);
   void              OnClickButton1(void);
   void              OnClickButton2(void);
  };
//+------------------------------------------------------------------+
//| Event Handling                                                   |
//+------------------------------------------------------------------+
EVENT_MAP_BEGIN(CSymbolSynthesizerDialog)
ON_EVENT(ON_CHANGE,m_combo_box,OnChangeComboBox)
ON_EVENT(ON_CHANGE,m_spin_edit1,OnChangeSpinEdit1)
ON_EVENT(ON_CHANGE,m_spin_edit2,OnChangeSpinEdit2)
ON_EVENT(ON_CLICK,m_button1,OnClickButton1)
ON_EVENT(ON_CLICK,m_button2,OnClickButton2)
EVENT_MAP_END(CAppDialog)
//+------------------------------------------------------------------+
//| Constructor                                                      |
//+------------------------------------------------------------------+
CSymbolSynthesizerDialog::CSymbolSynthesizerDialog(void)
  {
  }
//+------------------------------------------------------------------+
//| Destructor                                                       |
//+------------------------------------------------------------------+
CSymbolSynthesizerDialog::~CSymbolSynthesizerDialog(void)
  {
  }
//+------------------------------------------------------------------+
//| Create                                                           |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2)
  {
   if(!CAppDialog::Create(chart,name,subwin,x1,y1,x2,y2))                       return(false);
//--- create dependent controls
   if(!CreateLabel1())                                                          return(false);
   if(!CreateEdit1())                                                           return(false);
   if(!CreateEdit2())                                                           return(false);
   if(!CreateEdit3())                                                           return(false);
   if(!CreateLabel2())                                                          return(false);
   if(!CreateSpinEdit1())                                                       return(false);
   if(!CreateLabel3())                                                          return(false);
   if(!CreateSpinEdit2())                                                       return(false);
   if(!CreateButton1())                                                         return(false);
   if(!CreateButton2())                                                         return(false);
   if(!CreateComboBox())                                                        return(false);
//--- succeed
   OnChangeComboBox();
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Label1" element                                      |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateLabel1(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP;
   int x2=x1+GROUP_WIDTH;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_label1.Create(m_chart_id,m_name+"Label1",m_subwin,x1,y1,x2,y2))        return(false);
   if(!m_label1.Text("Symbol:"))                                                return(false);
   if(!Add(m_label1))                                                           return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "ComboBox" element                                    |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateComboBox(void)
  {
//--- coordinates
   int x1=INDENT_LEFT+(LABEL_WIDTH+CONTROLS_GAP_X);
   int y1=INDENT_TOP;
   int x2=x1+GROUP_WIDTH;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_combo_box.Create(m_chart_id,m_name+"ComboBox",m_subwin,x1,y1,x2,y2))   return(false);
   if(!Add(m_combo_box))                                                        return(false);
//--- fill out with strings
   for(int i=0;i<13;i++)
     {
      if(!m_combo_box.AddItem(Sym[i][0]))                                       return(false);
      if(Sym[i][0]==Symbol())
         if(!m_combo_box.Select(i))                                             return(false);
     }
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the display field1                                        |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateEdit1(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=ClientAreaWidth()-INDENT_RIGHT;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_edit1.Create(m_chart_id,m_name+"Edit1",m_subwin,x1,y1,x2,y2))          return(false);
   if(!m_edit1.ReadOnly(true))                                                  return(false);
   if(!Add(m_edit1))                                                            return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the display field2                                        |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateEdit2(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=ClientAreaWidth()-INDENT_RIGHT;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_edit2.Create(m_chart_id,m_name+"Edit2",m_subwin,x1,y1,x2,y2))          return(false);
   if(!m_edit2.ReadOnly(true))                                                  return(false);
   if(!Add(m_edit2))                                                            return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the display field3                                        |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateEdit3(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=ClientAreaWidth()-INDENT_RIGHT;
   int y2=y1+EDIT_HEIGHT+10;
//--- create
   if(!m_edit3.Create(m_chart_id,m_name+"Edit3",m_subwin,x1,y1,x2,y2))          return(false);
   if(!m_edit3.FontSize(14))                                                    return(false);
   if(!m_edit3.ReadOnly(true))                                                  return(false);
   if(!Add(m_edit3))                                                            return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Label2" element                                    |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateLabel2(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+10+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=x1+GROUP_WIDTH;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_label2.Create(m_chart_id,m_name+"Label2",m_subwin,x1,y1,x2,y2))        return(false);
   if(!m_label2.Text("Volume:")) return(false);
   if(!Add(m_label2)) return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "SpinEdit1" element                                   |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateSpinEdit1(void)
  {
//--- coordinates
   int x1=INDENT_LEFT+(LABEL_WIDTH+CONTROLS_GAP_X);
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+10+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=x1+GROUP_WIDTH;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_spin_edit1.Create(m_chart_id,m_name+"SpinEdit1",m_subwin,x1,y1,x2,y2)) return(false);
   if(!Add(m_spin_edit1)) return(false);
   m_spin_edit1.MinValue(0.01);
   m_spin_edit1.MaxValue(500);
   m_spin_edit1.Value(0.1);
//--- succeed
   vol=m_spin_edit1.Value();
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Label3" element                                      |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateLabel3(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+10+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=x1+GROUP_WIDTH;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_label3.Create(m_chart_id,m_name+"Label3",m_subwin,x1,y1,x2,y2))        return(false);
   if(!m_label3.Text("Slippage:"))                                              return(false);
   if(!Add(m_label3))                                                           return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "SpinEdit2" element                                    |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateSpinEdit2(void)
  {
//--- coordinates
   int x1=INDENT_LEFT+(LABEL_WIDTH+CONTROLS_GAP_X);
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+10+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=x1+GROUP_WIDTH;
   int y2=y1+EDIT_HEIGHT;
//--- create
   if(!m_spin_edit2.Create(m_chart_id,m_name+"SpinEdit2",m_subwin,x1,y1,x2,y2)) return(false);
   if(!Add(m_spin_edit2))                                                       return(false);
   m_spin_edit2.MinValue(0);
   m_spin_edit2.MaxValue(1000);
   m_spin_edit2.Value(30);
//--- succeed
   slippage=m_spin_edit2.Value();
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Button1" button                                      |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateButton1(void)
  {
//--- coordinates
   int x1=INDENT_LEFT;
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+10+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!m_button1.Create(m_chart_id,m_name+"Button1",m_subwin,x1,y1,x2,y2))      return(false);
   if(!m_button1.Text("Sell"))                                                  return(false);
   if(!m_button1.ColorBackground(clrRed))                                       return(false);
   if(!Add(m_button1))                                                          return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//| Create the "Button2" button                                      |
//+------------------------------------------------------------------+
bool CSymbolSynthesizerDialog::CreateButton2(void)
  {
//--- coordinates
   int x1=INDENT_LEFT+(BUTTON_WIDTH+CONTROLS_GAP_X);
   int y1=INDENT_TOP+(EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+
          (EDIT_HEIGHT+CONTROLS_GAP_Y)+10+
          (EDIT_HEIGHT+CONTROLS_GAP_Y);
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!m_button2.Create(m_chart_id,m_name+"Button2",m_subwin,x1,y1,x2,y2))      return(false);
   if(!m_button2.Text("Buy"))                                                   return(false);
   if(!m_button2.ColorBackground(clrBlue))                                      return(false);
   if(!Add(m_button2))                                                          return(false);
//--- succeed
   return(true);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
void CSymbolSynthesizerDialog::TickChange(void)
  {
   OnChangeComboBox();
   return;
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CSymbolSynthesizerDialog::OnChangeComboBox(void)
  {
   for(int i=0;i<13;i++)
     {
      if(Sym[i][0]==m_combo_box.Select())
        {
         SymNum=i;
         double bid1=SymbolInfoDouble(Sym[i][1],SYMBOL_BID);
         double ask1=SymbolInfoDouble(Sym[i][1],SYMBOL_ASK);
         double bid2=SymbolInfoDouble(Sym[i][2],SYMBOL_BID);
         double ask2=SymbolInfoDouble(Sym[i][2],SYMBOL_ASK);
         if(Sym[i][3]=="L")
           {
            vBID=bid1*bid2;
            vASK=ask1*ask2;
           }
         else
           {
            vBID=bid2/bid1;
            vASK=ask2/ask1;
           }
         m_edit1.Text(Sym[i][1]+": "+DoubleToString(bid1,(int)SymbolInfoInteger(Sym[i][1],SYMBOL_DIGITS))+" / "+DoubleToString(ask1,(int)SymbolInfoInteger(Sym[i][1],SYMBOL_DIGITS)));
         m_edit2.Text(Sym[i][2]+": "+DoubleToString(bid2,(int)SymbolInfoInteger(Sym[i][2],SYMBOL_DIGITS))+" / "+DoubleToString(ask2,(int)SymbolInfoInteger(Sym[i][2],SYMBOL_DIGITS)));
         m_edit3.Text("vPrice: "+DoubleToString(vBID,(int)SymbolInfoInteger(Sym[i][0],SYMBOL_DIGITS))+" / "+DoubleToString(vASK,(int)SymbolInfoInteger(Sym[i][0],SYMBOL_DIGITS)));
         break;
        }
     }
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CSymbolSynthesizerDialog::OnChangeSpinEdit1()
  {
   vol=m_spin_edit1.Value();
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CSymbolSynthesizerDialog::OnChangeSpinEdit2()
  {
   slippage=m_spin_edit2.Value();
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CSymbolSynthesizerDialog::OnClickButton1(void)
  {
   double hedge1,hedge2;
   vol=m_spin_edit1.Value();
   if(Sym[SymNum][3]=="L")
      hedge1=Call_Trade(Sym[SymNum][1],1,"HedgeSELL "+Sym[SymNum][0]+"1");
   else
      hedge1=Call_Trade(Sym[SymNum][1],0,"HedgeSELL "+Sym[SymNum][0]+"1");
   if(hedge1==-1)
     {
      Print("Hedge SELL failed: ",Sym[SymNum][0]);
      return;
     }
   vol=vol*vBID/SymbolInfoDouble(Sym[SymNum][1],SYMBOL_TRADE_TICK_VALUE)/SymbolInfoDouble(Sym[SymNum][2],SYMBOL_TRADE_TICK_VALUE)*(SymbolInfoDouble(Sym[SymNum][2],SYMBOL_POINT)/SymbolInfoDouble(Sym[SymNum][1],SYMBOL_POINT));
   hedge2=Call_Trade(Sym[SymNum][2],1,"HedgeSELL "+Sym[SymNum][0]+"2");
   if(hedge1>-1 && hedge2>-1)
     {
      if(Sym[SymNum][3]=="L")
         Print("Hedge SELL position: ",Sym[SymNum][0]," ",hedge1*hedge2);
      else
         Print("Hedge SELL position: ",Sym[SymNum][0]," ",hedge2/hedge1);
     }
   else
      Print("Hedge SELL failed: ",Sym[SymNum][0]);
  }
//+------------------------------------------------------------------+
//| Event handler                                                    |
//+------------------------------------------------------------------+
void CSymbolSynthesizerDialog::OnClickButton2(void)
  {
   double hedge1,hedge2;
   vol=m_spin_edit1.Value();
   if(Sym[SymNum][3]=="L")
      hedge1=Call_Trade(Sym[SymNum][1],0,"HedgeBUY "+Sym[SymNum][0]+"1");
   else
      hedge1=Call_Trade(Sym[SymNum][1],1,"HedgeBUY "+Sym[SymNum][0]+"1");
   if(hedge1==-1)
     {
      Print("Hedge BUY failed: ",Sym[SymNum][0]);
      return;
     }
   vol=vol*vASK/SymbolInfoDouble(Sym[SymNum][1],SYMBOL_TRADE_TICK_VALUE)/SymbolInfoDouble(Sym[SymNum][2],SYMBOL_TRADE_TICK_VALUE)*(SymbolInfoDouble(Sym[SymNum][2],SYMBOL_POINT)/SymbolInfoDouble(Sym[SymNum][1],SYMBOL_POINT));
   hedge2=Call_Trade(Sym[SymNum][2],0,"HedgeBUY "+Sym[SymNum][0]+"2");
   if(hedge1>-1 && hedge2>-1)
     {
      if(Sym[SymNum][3]=="L")
         Print("Hedge BUY position: ",Sym[SymNum][0]," ",hedge1*hedge2);
      else
         Print("Hedge BUY position: ",Sym[SymNum][0]," ",hedge2/hedge1);
     }
   else
      Print("Hedge BUY failed: ",Sym[SymNum][0]);
  }
//+------------------------------------------------------------------+
//|                                                                  |
//+------------------------------------------------------------------+
double CSymbolSynthesizerDialog::Call_Trade(string symb,int pos,string com)
  {
   bool     res=false;
   m_trade.SetDeviationInPoints((ulong)slippage);
   double   Lots=NormalizeDouble(vol,2);
   if(Lots<SymbolInfoDouble(symb,SYMBOL_VOLUME_MIN)) Lots=SymbolInfoDouble(symb,SYMBOL_VOLUME_MIN);

   if(pos==0)
     {
      int i=0;
      while(i<5)
        {
         double ASK=SymbolInfoDouble(symb,SYMBOL_ASK);
         res=m_trade.PositionOpen(symb,ORDER_TYPE_BUY,Lots,ASK,0,0,com);
         Sleep(5000);
         if(res) return(ASK);
         i++;
        }
      if(!res) Print("Error opening Buy order : ",GetLastError());
     }

   if(pos==1)
     {
      int i=0;
      while(i<5)
        {
         double BID=SymbolInfoDouble(symb,SYMBOL_BID);
         res=m_trade.PositionOpen(symb,ORDER_TYPE_SELL,Lots,BID,0,0,com);
         Sleep(5000);
         if(res) return(BID);
         i++;
        }
      if(!res) Print("Error opening Sell order : ",GetLastError());
     }

   return(-1);
  }
//+------------------------------------------------------------------+ 
