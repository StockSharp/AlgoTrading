//+------------------------------------------------------------------+
//|                                              EA_TradingPanel.mq4 |
//|                                        Copyright 2021, FxWeirdos |
//|                                               info@fxweirdos.com |
//+------------------------------------------------------------------+

#property copyright "Copyright 2021, FxWeirdos. Mario Gharib. Forex Jarvis. info@fxweirdos.com"
#property link      "https://fxweirdos.com"
#property version   "1.01"
#property strict
//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+

// INPUT //
input int iNB=1;              //NB TRADES
input double dSL=2;           //NB PIPS SELL_SL
input double dVOL=0.01;       //VOLUME
input double dTP=10;          //NB PIPS SELL_TP

#include <Controls\Dialog.mqh>
#include <Controls\Button.mqh>
#include <Controls\Edit.mqh>
#include <Controls\Panel.mqh>
#include <Controls\Label.mqh>
#include <Controls\SpinEdit.mqh>
#include <Controls\Button.mqh>
#include <Controls\ComboBox.mqh>

// ********************************************************************************** //
// ****** THIS FUNCTIONS WILL RETURN THE PRICE OF SL BASED ON NUMBER OF PIPS ******** //
// ********************************************************************************** //

double dPriceSL(string sSymbol, double dPrice, double dnbPips) {
   
   double dp=0;

   if (SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==1 || SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==3 || SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==5)
      dp=10;
   else 
      dp=1;   
   
   // PIP POSITION 
   double pipPos = SymbolInfoDouble(sSymbol,SYMBOL_POINT)*dp;

	// TOTAL NUMBER OF RISKED PIPS
	return NormalizeDouble(MathAbs(dPrice-dnbPips*pipPos),5);
}

// ********************************************************************************** //
// ****** THIS FUNCTIONS WILL RETURN THE PRICE OF TP BASED ON NUMBER OF PIPS ******** //
// ********************************************************************************** //

double dPriceTP(string sSymbol, double dPrice, double dnbPips) {
   
   double dp=0;

   if (SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==1 || SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==3 || SymbolInfoInteger(sSymbol,SYMBOL_DIGITS)==5)
      dp=10;
   else 
      dp=1;   
   
   // PIP POSITION 
   double pipPos = SymbolInfoDouble(sSymbol,SYMBOL_POINT)*dp;
	// TOTAL NUMBER OF RISKED PIPS
	return NormalizeDouble(MathAbs(dPrice+dnbPips*pipPos),5);
}

//+------------------------------------------------------------------+
//| DEFINES                                                          |
//+------------------------------------------------------------------+
#define INDENT_LEFT                         (-60)     // left indent (including the border width)
#define INDENT_TOP                          (11)      // top indent (including the border width)
#define INDENT_RIGHT                        (11)      // right indent (including the border width)
#define INDENT_BOTTOM                       (11)      // bottom indent (including the border width)
#define CONTROLS_GAP_X                      (5)      // spacing along the X-axis
#define CONTROLS_GAP_Y                      (5)      // spacing along the Y-axis
#define LABEL_WIDTH                         (30)      // size along the X-axis
#define EDIT_WIDTH                          (55)      // size along the X-axis
#define EDIT_HEIGHT                         (20)      // size along the Y-axis
#define BUTTON_WIDTH                        (70)     // size by X coordinate
#define BUTTON_HEIGHT                       (20)      // size by Y coordinate

//+------------------------------------------------------------------+
//| CPanelDialog class                                               |
//| Function: main application dialog                                |
//+------------------------------------------------------------------+
class CControlsDialog : public CAppDialog
  {
   private:
   
      // ADDITIONAL CONTROLS //   
      CLabel   clLabel_NB;    // The "NB" of trades
      CLabel   clLabel_SL;    // The "SL" of trades
      CLabel   clLabel_VOL;   // The "VOL" of trades
      CLabel   clLabel_TP;    // The "TP" of trades
      CLabel   clLabel_PAIR;  // The "SYM" of trades
      
      CSpinEdit clSpinEdit_NB;// The "NB" value
      CEdit    clEdit_SL;     // The "SL" value
      CEdit    clEdit_VOL;    // The "VOL" value
      CEdit    clEdit_TP;     // The "TP" value
      
      CButton  clButton_SELL;             // The "SELL" button object
      CButton  clButton_BUY;              // The "SELL" button object

      // PARAMETER VALUES //
      int iNB;       // The "NB" value
      double dSL;       // The "SL" value
      double dVOL;   // The "VOL" value
      double dTP;       // The "TP" value
      int iPair;     //"Sym" value

   
   public:

      CComboBox   clComboBox_PAIR; // CComboBox object

      CControlsDialog(void);
      ~CControlsDialog(void);
      
      //--- creation
      virtual bool Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);
      
      //--- chart event handler
      virtual bool OnEvent(const int id,const long &lparam,const double &dparam,const string &sparam);
      
      //--- properties
      void vSetNB(const int value);
      void vSetSL(const double value);
      void vSetVOL(const double value);
      void vSetTP(const double value);
      void vSetPair(const int value);

   
   protected:
   
      // CREATING ADDITIONAL CONTROLS //

      bool bCreate_Label_NB(void);
      bool bCreate_Label_SL(void);
      bool bCreate_Label_VOL(void);
      bool bCreate_Label_TP(void);
      bool bCreate_Label_PAIR(void);
      
      bool bCreate_SpinEdit_NB (void);
      bool bCreate_Edit_SL(void);
      bool bCreate_Edit_VOL(void);
      bool bCreate_Edit_TP(void);
      
      bool bCreate_Button_SELL(void);
      bool bCreate_Button_BUY(void);
            
      bool bCreate_ComboBox_PAIR(void);

      //--- handlers of the dependent controls events
      void vOnClick_Button_SELL(void);
      void vOnClick_Button_BUY(void);

      //--- internal event handlers
      virtual bool      OnResize(void);

};

// ********************************************** //
// *************** EVENT HANDLING *************** //
// ********************************************** //
EVENT_MAP_BEGIN(CControlsDialog)
   ON_EVENT(ON_CLICK,clButton_SELL,       vOnClick_Button_SELL)
   ON_EVENT(ON_CLICK,clButton_BUY,        vOnClick_Button_BUY)
EVENT_MAP_END(CAppDialog)

// ********************************************** //
// *********** CONSTRUCTOR/DESTRUCTOR *********** //
// ********************************************** //
CControlsDialog::CControlsDialog(void) { }
CControlsDialog::~CControlsDialog(void) { }

// ********************************************** //
// ******************* CREATION ***************** //
// ********************************************** //
bool CControlsDialog::Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2) {

   //--- calling the parent class method
   if(!CAppDialog::Create(chart,name,subwin,x1,y1,x2,y2))
      return(false);
      
   // CREATING ADDITIONAL CONTROLS //

   if(!bCreate_Label_NB())                return(false);
   if(!bCreate_Label_SL())                return(false);
   if(!bCreate_Label_VOL())               return(false);
   if(!bCreate_Label_TP())                return(false);
   if(!bCreate_Label_PAIR())             return(false);
   
   if(!bCreate_SpinEdit_NB())             return(false);
   if(!bCreate_Edit_SL())                 return(false);
   if(!bCreate_Edit_VOL())                return(false);
   if(!bCreate_Edit_TP())                 return(false);
   
   if(!bCreate_Button_SELL())             return(false);
   if(!bCreate_Button_BUY())              return(false);

   if(!bCreate_ComboBox_PAIR())             return(false);

   return(true);
}


// ******************************************************************* //
// ***************** CREATING THE DISPLAY ELEMENT NB ***************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Label_NB(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+2*LABEL_WIDTH+2*CONTROLS_GAP_X;
   int y1=INDENT_TOP;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE LABEL NB//
   if(!clLabel_NB.Create(m_chart_id,m_name+"Label_NB",m_subwin,x1,y1+1,x2,y2))   return(false);
   if(!clLabel_NB.Text("NB"))                                                    return(false);
   if(!Add(clLabel_NB))                                                          return(false);

   return(true);
}

// ******************************************************************* //
// ******************* CREATING THE EDIT ELEMENT NB ****************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_SpinEdit_NB(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+3*LABEL_WIDTH+3*CONTROLS_GAP_X;
   int y1=INDENT_TOP;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

//--- create
   if(!clSpinEdit_NB.Create(m_chart_id,m_name+"SpinEdit_NB",m_subwin,x1,y1,x2,y2)) return(false);
   if(!Add(clSpinEdit_NB)) return(false);
   clSpinEdit_NB.MinValue(1);
   clSpinEdit_NB.MaxValue(20);
   clSpinEdit_NB.Value(1);

   return(true);
}


// ******************************************************************* //
// ******************** CREATING THE SELL BUTTON ********************* //
// ******************************************************************* //
bool CControlsDialog::bCreate_Button_SELL(void)
  {
//--- coordinates
   int x1=INDENT_LEFT+5*LABEL_WIDTH+5*CONTROLS_GAP_X;
   int y1=INDENT_TOP;
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!clButton_SELL.Create(m_chart_id,"ButtonSELL",m_subwin,x1,y1,x2,y2))   return(false);
   if(!clButton_SELL.Text("SELL"))     return(false);
   if(!Add(clButton_SELL))             return(false);

   return(true);
  }

// ******************************************************************* //
// ***************** CREATING THE DISPLAY ELEMENT SL ***************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Label_SL(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+2*LABEL_WIDTH+2*CONTROLS_GAP_X;
   int y1=INDENT_TOP+EDIT_HEIGHT+CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE LABEL NB//
   if(!clLabel_SL.Create(m_chart_id,m_name+"Label_SL",m_subwin,x1,y1+1,x2,y2))   return(false);
   if(!clLabel_SL.Text("SL"))                                                    return(false);
   if(!Add(clLabel_SL))                                                          return(false);

   return(true);
}

// ******************************************************************* //
// ******************* CREATING THE EDIT ELEMENT SL ****************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Edit_SL(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+3*LABEL_WIDTH+3*CONTROLS_GAP_X;
   int y1=INDENT_TOP+EDIT_HEIGHT+CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE EDIT NB //
   if(!clEdit_SL.Create(m_chart_id,m_name+"Edit_SL",m_subwin,x1,y1,x2,y2))  return(false);
   if(!clEdit_SL.Text(DoubleToString(dSL)))                           return(false);
   if(!clEdit_SL.ReadOnly(false))                                           return(false);
   if(!Add(clEdit_SL))                                                      return(false);

   return(true);
   }

// ******************************************************************* //
// ******************** CREATING THE BUY BUTTON ********************* //
// ******************************************************************* //
bool CControlsDialog::bCreate_Button_BUY(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+5*LABEL_WIDTH+5*CONTROLS_GAP_X;
   int y1=INDENT_TOP+EDIT_HEIGHT+CONTROLS_GAP_Y;
   int x2=x1+BUTTON_WIDTH;
   int y2=y1+BUTTON_HEIGHT;
//--- create
   if(!clButton_BUY.Create(m_chart_id,m_name+"ButtonBUY",m_subwin,x1,y1,x2,y2))  return(false);
   if(!clButton_BUY.Text("BUY")) return(false);
   if(!Add(clButton_BUY))        return(false);

   return(true);
  }

// ******************************************************************* //
// ***************** CREATING THE DISPLAY ELEMENT VOL **************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Label_VOL(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+2*LABEL_WIDTH+2*CONTROLS_GAP_X;
   int y1=INDENT_TOP+2*EDIT_HEIGHT+2*CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE LABEL NB//
   if(!clLabel_VOL.Create(m_chart_id,m_name+"Label_VOL",m_subwin,x1,y1+1,x2,y2))   return(false);
   if(!clLabel_VOL.Text("VOL"))                                                    return(false);
   if(!Add(clLabel_VOL))                                                          return(false);

   return(true);
}

// ******************************************************************* //
// ******************* CREATING THE EDIT ELEMENT VOL ***************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Edit_VOL(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+3*LABEL_WIDTH+3*CONTROLS_GAP_X;
   int y1=INDENT_TOP+2*EDIT_HEIGHT+2*CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE EDIT VOL //
   if(!clEdit_VOL.Create(m_chart_id,m_name+"Edit_VOL",m_subwin,x1,y1,x2,y2))  return(false);
   if(!clEdit_VOL.Text(DoubleToString(dVOL)))                           return(false);
   if(!clEdit_VOL.ReadOnly(false))                                           return(false);
   if(!Add(clEdit_VOL))                                                      return(false);

   return(true);
   }

// ******************************************************************* //
// ***************** CREATING THE DISPLAY ELEMENT TP ***************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Label_TP(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+2*LABEL_WIDTH+2*CONTROLS_GAP_X;
   int y1=INDENT_TOP+3*EDIT_HEIGHT+3*CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE LABEL NB//
   if(!clLabel_TP.Create(m_chart_id,m_name+"Label_TP",m_subwin,x1,y1+1,x2,y2))   return(false);
   if(!clLabel_TP.Text("TP"))                                                    return(false);
   if(!Add(clLabel_TP))                                                          return(false);

   return(true);
}

// ******************************************************************* //
// ******************* CREATING THE EDIT ELEMENT TP ****************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Edit_TP(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+3*LABEL_WIDTH+3*CONTROLS_GAP_X;
   int y1=INDENT_TOP+3*EDIT_HEIGHT+3*CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE EDIT NB //
   if(!clEdit_TP.Create(m_chart_id,m_name+"Edit_TP",m_subwin,x1,y1,x2,y2))  return(false);
   if(!clEdit_TP.Text(DoubleToString(dTP)))                           return(false);
   if(!clEdit_TP.ReadOnly(false))                                           return(false);
   if(!Add(clEdit_TP))                                                      return(false);

   return(true);
   }

// ******************************************************************* //
// **************** CREATING THE DISPLAY ELEMENT PRICE *************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_Label_PAIR(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+2*LABEL_WIDTH+2*CONTROLS_GAP_X;
   int y1=INDENT_TOP+4*EDIT_HEIGHT+4*CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE LABEL NB//
   if(!clLabel_PAIR.Create(m_chart_id,m_name+"clLabel_PAIR",m_subwin,x1,y1+1,x2,y2))   return(false);
   if(!clLabel_PAIR.Text("SYM"))                                                    return(false);
   if(!Add(clLabel_PAIR))                                                          return(false);

   return(true);
}

// ******************************************************************* //
// **************** CREATING THE COMBOBOX ELEMENT PAIR *************** //
// ******************************************************************* //
bool CControlsDialog::bCreate_ComboBox_PAIR(void)
  {
   // COORDINATES //
   int x1=INDENT_LEFT+3*LABEL_WIDTH+3*CONTROLS_GAP_X;
   int y1=INDENT_TOP+4*EDIT_HEIGHT+4*CONTROLS_GAP_Y;
   int x2=x1+EDIT_WIDTH*2.5;
   int y2=y1+EDIT_HEIGHT;

   // CREATING THE CAPTION //
   if(!clComboBox_PAIR.Create(m_chart_id,"COMBOBOX_PAIR",m_subwin,x1,y1,x2,y2))  return(false);
   if(!Add(clComboBox_PAIR))                                                      return(false);
   
   int HowManySymbols=SymbolsTotal(true);
   
   string array_string[], stemp;
   ArrayResize(array_string,HowManySymbols);

   for(int i=0;i<HowManySymbols;i++)
      array_string[i]=SymbolName(i,true);

   for(int i=0;i<HowManySymbols;i++) {
		for(int j=i+1;j<HowManySymbols;j++) {
			if(StringCompare(array_string[i],array_string[j],false)>0) {
				stemp = array_string[i];
				array_string[i]=array_string[j];
				array_string[j]=stemp;
			}
		}
	}

   for(int i=0;i<HowManySymbols;i++) {
      clComboBox_PAIR.ItemAdd(array_string[i]);
   }

   return(true);
}

//+------------------------------------------------------------------+
//| Setting the "NB" value                                          |
//+------------------------------------------------------------------+
void CControlsDialog::vSetNB(const int value) {

   iNB=value;
   clSpinEdit_NB.Value(value);
}

//+------------------------------------------------------------------+
//| Setting the "SL" value                                          |
//+------------------------------------------------------------------+
void CControlsDialog::vSetSL(const double value) {

   dSL=value;
   clEdit_SL.Text(DoubleToString(value,2));
}

//+------------------------------------------------------------------+
//| Setting the "VOL" value                                          |
//+------------------------------------------------------------------+
void CControlsDialog::vSetVOL(const double value) {

   dVOL=value;
   clEdit_VOL.Text(DoubleToString(value,2));
}
 
//+------------------------------------------------------------------+
//| Setting the "TP" value                                           |
//+------------------------------------------------------------------+
void CControlsDialog::vSetTP(const double value) {

   dTP=value;
   clEdit_TP.Text(DoubleToString(value,2));
}


//+------------------------------------------------------------------+
//| Setting the "SL PAIR" value                                      |
//+------------------------------------------------------------------+
void CControlsDialog::vSetPair(const int value){

   iPair=value;
   clComboBox_PAIR.Select(value);
}

//+------------------------------------------------------------------+
//| Resize handler                                                   |
//+------------------------------------------------------------------+
bool CControlsDialog::OnResize(void)
  {
//--- calling the parent class method
   if(!CAppDialog::OnResize()) return(false);

   return(true);
  }


//+------------------------------------------------------------------+
//| Event handler                                                    |
//| BUTTON SELL                                                      |
//+------------------------------------------------------------------+

void CControlsDialog::vOnClick_Button_SELL(void) {

   double dAsk = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
   double dBid = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);

   double dSL1=0.0;
   double dTP1=0.0;

   // NUMBER TRADES
   int iNB1 = clSpinEdit_NB.Value();

   // VOLUME
   double dVOL1 = (double)ObjectGetString(0,m_name+"Edit_VOL",OBJPROP_TEXT);   
   string sSym = clComboBox_PAIR.Select();

   dSL1 = dPriceTP (sSym, dBid, (double)ObjectGetString(0,m_name+"Edit_SL",OBJPROP_TEXT));
   dTP1 = dPriceSL(sSym, dBid, (double)ObjectGetString(0,m_name+"Edit_TP",OBJPROP_TEXT));
   
   for (int i=0;i<iNB1;i++) {
      int o=OrderSend(sSym,OP_SELL,dVOL1,dBid,5,dSL1,dTP1,"");    
   }
}


//+------------------------------------------------------------------+
//| Event handler                                                    |
//| BUTTON BUY                                                       |
//+------------------------------------------------------------------+
  
void CControlsDialog::vOnClick_Button_BUY(void)  {
   
   double dAsk = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_ASK),_Digits);
   double dBid = NormalizeDouble(SymbolInfoDouble(_Symbol,SYMBOL_BID),_Digits);
   
   double dSL1=0.0;
   double dTP1=0.0;

   // NUMBER TRADES
   int iNB1 = clSpinEdit_NB.Value();
   
   // VOLUME
   double dVOL1 = (double)ObjectGetString(0,m_name+"Edit_VOL",OBJPROP_TEXT);
   string sSym = clComboBox_PAIR.Select();

   dSL1 = dPriceSL(sSym, dAsk, (double)ObjectGetString(0,m_name+"Edit_SL",OBJPROP_TEXT));
   dTP1 = dPriceTP(sSym, dAsk, (double)ObjectGetString(0,m_name+"Edit_TP",OBJPROP_TEXT));

   for (int i=0;i<iNB1;i++) {
     int o=OrderSend(sSym,OP_BUY,dVOL1,dAsk,5,dSL1,dTP1,"");    
   }
}

// GLOBAL VARIABLES //
CControlsDialog ExtDialog1;
int counter=0;

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit() {
   
   if (counter++==0) {
      if(!ExtDialog1.Create(0,"Trading Panel",0,1,1,210,310))  // CREATING THE APPLICATION DIALOG //
         return(-1);
      if(!ExtDialog1.Run())                                  // STARTING THE APPLICATION //
         return(-2);
   }

   ExtDialog1.vSetNB(iNB);
   ExtDialog1.vSetSL(NormalizeDouble(dSL,2));
   ExtDialog1.vSetVOL(dVOL);
   ExtDialog1.vSetTP(dTP);

   ChartSetInteger(0,CHART_COLOR_BID,clrWhite);
   ChartSetInteger(0,CHART_COLOR_ASK,clrWhite);
   ChartSetInteger(0,CHART_COLOR_BACKGROUND,clrWhite);   
   ChartSetInteger(0,CHART_COLOR_FOREGROUND,clrWhite);
   ChartSetInteger(0,CHART_COLOR_CANDLE_BEAR,clrWhite);
   ChartSetInteger(0,CHART_COLOR_CANDLE_BULL,clrWhite);
   ChartSetInteger(0,CHART_COLOR_CHART_DOWN,clrWhite);
   ChartSetInteger(0,CHART_COLOR_CHART_UP,clrWhite);
   ChartSetInteger(0,CHART_COLOR_CHART_LINE,clrWhite);
   ChartSetInteger(0,CHART_COLOR_GRID,clrWhite);
   ChartSetInteger(0,CHART_SHOW_ONE_CLICK,false);
   
   return(0);
}

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason) {
   if (reason != REASON_CHARTCHANGE) {
      ExtDialog1.Destroy();                                  // DESTROYING THE DIALOG //
   }
}

// CHART EVENT HANDLER //
void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam) {
   ExtDialog1.ChartEvent(id,lparam,dparam,sparam);        // HANDLING THE EVENT // 

}