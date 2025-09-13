//+------------------------------------------------------------------+
//|                                                      Maximum Trade Volume |
//|                                      		Copyright 2024, phade |
//|                                       	https://www.fxcalculator.io |
//+------------------------------------------------------------------+
#include <Controls\Dialog.mqh>
#include <Controls\Label.mqh>  // Include the Label class

#define INDENT_LEFT                         (11)
#define INDENT_TOP                          (11)
#define INDENT_RIGHT                        (11)
#define INDENT_BOTTOM                       (11)
#define CONTROLS_GAP_X                      (5)
#define CONTROLS_GAP_Y                      (5)
#define BUTTON_WIDTH                        (100)
#define BUTTON_HEIGHT                       (20)
#define EDIT_HEIGHT                         (20)
#define GROUP_WIDTH                         (150)
#define LIST_HEIGHT                         (179)
#define RADIO_HEIGHT                        (56)
#define CHECK_HEIGHT                        (93)

//+------------------------------------------------------------------+
//| Class CControlsDialog                                            |
//+------------------------------------------------------------------+
class CControlsDialog : public CAppDialog
  {
public:
                     CControlsDialog(void);
                    ~CControlsDialog(void);
   //--- create
   virtual bool      Create(const long chart,const string name,const int subwin,const int x1,const int y1,const int x2,const int y2);

protected:
   //--- create dependent controls
   bool              CreatePanel(void);

private:
   CLabel            label_text;        // Label for the main title
   CLabel            label_sell;        // Label for "max lot for sell"
   CLabel            label_buy;         // Label for "max lot for buy"
   CLabel            label_pending_buy; // Label for "max lot for pending buy"
   CLabel            label_pending_sell;// Label for "max lot for pending sell"
  };

//+------------------------------------------------------------------+
//| Global Variables                                                 |
//+------------------------------------------------------------------+
CControlsDialog ExtDialog;
CPanel          my_white_border;  // object CPanel
bool            pause = true;     // true - pause

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
   if(!CreatePanel())
      return(false);
//--- succeed
   return(true);
  }

//+------------------------------------------------------------------+
//| Create the "CPanel"                                              |
//+------------------------------------------------------------------+
bool CControlsDialog::CreatePanel(void)
  {
//--- coordinates
   int x1 = 5;
   int y1 = 5;
   int x2 = 230;
   int y2 = 40;
//--- create panel
   if(!my_white_border.Create(0, ExtDialog.Name() + "MyWhiteBorder", m_subwin, x1, y1, x2, y2))
      return(false);
   if(!my_white_border.ColorBackground(CONTROLS_DIALOG_COLOR_BG))
      return(false);
   if(!my_white_border.ColorBorder(CONTROLS_DIALOG_COLOR_BORDER_DARK))
      return(false);
   if(!ExtDialog.Add(my_white_border))
      return(false);
   my_white_border.Alignment(WND_ALIGN_CLIENT, 0, 0, 0, 0);

//--- create the main label
   int label_x1 = INDENT_LEFT;
   int label_y1 = y1 + CONTROLS_GAP_Y - 3; // Positioning label below the panel
   int label_x2 = label_x1 + 200;  // Label width
   int label_y2 = label_y1 + 20;   // Label height

   if(!label_text.Create(0, "LabelText", m_subwin, label_x1, label_y1, label_x2, label_y2))
      return(false);

   label_text.Text("Max lot on " + _Symbol);  // Set text for the label
   label_text.Color(clrBlack);                // Set text color
   label_text.FontSize(18);                   // Set font size

   if(!ExtDialog.Add(label_text))
      return(false);

//--- create additional labels for "max lot for sell", "max lot for buy", etc.
   int text_y_gap = 25;  // Vertical gap between labels

// "max lot for sell"
   int sell_y1 = label_y2 + CONTROLS_GAP_Y + text_y_gap;
   int sell_y2 = sell_y1 + 20;

   if(!label_sell.Create(0, "SellLabel", m_subwin, label_x1, sell_y1, label_x2, sell_y2))
      return(false);

   label_sell.Text("Max lot for sell: " + DoubleToString(LotCheckSell(), 2));
   label_sell.Color(clrBlack);
   label_sell.FontSize(12);

   if(!ExtDialog.Add(label_sell))
      return(false);

// "max lot for buy"
   int buy_y1 = sell_y2 + CONTROLS_GAP_Y;
   int buy_y2 = buy_y1 + 20;

   if(!label_buy.Create(0, "BuyLabel", m_subwin, label_x1, buy_y1, label_x2, buy_y2))
      return(false);

   label_buy.Text("Max lot for buy: " + DoubleToString(LotCheckBuy(), 2));
   label_buy.Color(clrBlack);
   label_buy.FontSize(12);

   if(!ExtDialog.Add(label_buy))
      return(false);

// "max lot for pending buy"
   int pending_buy_y1 = buy_y2 + CONTROLS_GAP_Y;
   int pending_buy_y2 = pending_buy_y1 + 20;

   if(!label_pending_buy.Create(0, "PendingBuyLabel", m_subwin, label_x1, pending_buy_y1, label_x2, pending_buy_y2))
      return(false);

   label_pending_buy.Text("Max lot for pending buy: " + DoubleToString(LotCheckPendingBuy(), 2));
   label_pending_buy.Color(clrBlack);
   label_pending_buy.FontSize(12);

   if(!ExtDialog.Add(label_pending_buy))
      return(false);

// "max lot for pending sell"
   int pending_sell_y1 = pending_buy_y2 + CONTROLS_GAP_Y;
   int pending_sell_y2 = pending_sell_y1 + 20;

   if(!label_pending_sell.Create(0, "PendingSellLabel", m_subwin, label_x1, pending_sell_y1, label_x2, pending_sell_y2))
      return(false);

   label_pending_sell.Text("Max lot for pending sell: " + DoubleToString(LotCheckPendingSell(), 2));
   label_pending_sell.Color(clrBlack);
   label_pending_sell.FontSize(12);

   if(!ExtDialog.Add(label_pending_sell))
      return(false);

//--- succeed
   return(true);
  }

//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
   EventSetTimer(3);
   pause = true;

   if(!ExtDialog.Create(0, "Max lot checker", 0, 40, 40, 340, 250))
      Print("Could not create the dialog");

   ExtDialog.Run();
   return(INIT_SUCCEEDED);
  }

//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
  }

//+------------------------------------------------------------------+
//| Handle Chart Events                                              |
//+------------------------------------------------------------------+
void OnChartEvent(const int id,
                  const long &lparam,
                  const double &dparam,
                  const string &sparam)
  {
   ExtDialog.ChartEvent(id, lparam, dparam, sparam);
  }

//+------------------------------------------------------------------+
//| Calculate Lot Size                                               |
//+------------------------------------------------------------------+
double LotCheckBuy()
  {
   double margin_for_one_lot;
   static double lotSize;

   double currentMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);

   if(!OrderCalcMargin(ORDER_TYPE_BUY, _Symbol, 1.0, SymbolInfoDouble(_Symbol, SYMBOL_ASK), margin_for_one_lot))
      Print("Could not obtain the margin required to open 1 lot");

   lotSize = currentMargin / margin_for_one_lot;

   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);

   lotSize = MathMax(minLot, MathFloor(lotSize / lotStep) * lotStep);

   return lotSize;
  }
  
double LotCheckSell()
  {
   double margin_for_one_lot;
   static double lotSize;

   double currentMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);

   if(!OrderCalcMargin(ORDER_TYPE_SELL, _Symbol, 1.0, SymbolInfoDouble(_Symbol, SYMBOL_BID), margin_for_one_lot))
      Print("Could not obtain the margin required to open 1 lot");

   lotSize = currentMargin / margin_for_one_lot;

   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);

   lotSize = MathMax(minLot, MathFloor(lotSize / lotStep) * lotStep);

   return lotSize;
  }

double LotCheckPendingBuy()
  {
   double margin_for_one_lot;
   static double lotSize;

   double currentMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);

   if(!OrderCalcMargin(ORDER_TYPE_BUY_STOP || ORDER_TYPE_BUY_LIMIT, _Symbol, 1.0, SymbolInfoDouble(_Symbol, SYMBOL_ASK), margin_for_one_lot))
      Print("Could not obtain the margin required to open 1 lot");

   lotSize = currentMargin / margin_for_one_lot;

   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);

   lotSize = MathMax(minLot, MathFloor(lotSize / lotStep) * lotStep);

   return lotSize;
  }
  
double LotCheckPendingSell()
  {
   double margin_for_one_lot;
   static double lotSize;

   double currentMargin = AccountInfoDouble(ACCOUNT_MARGIN_FREE);

   if(!OrderCalcMargin(ORDER_TYPE_SELL_STOP || ORDER_TYPE_SELL_LIMIT, _Symbol, 1.0, SymbolInfoDouble(_Symbol, SYMBOL_BID), margin_for_one_lot))
      Print("Could not obtain the margin required to open 1 lot");

   lotSize = currentMargin / margin_for_one_lot;

   double minLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   double maxLot = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   double lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);

   lotSize = MathMax(minLot, MathFloor(lotSize / lotStep) * lotStep);

   return lotSize;
  }

//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
   Comment("");
   EventKillTimer();
   ExtDialog.Destroy(reason);
  }

//+------------------------------------------------------------------+
//| Timer Event                                                      |
//+------------------------------------------------------------------+
void OnTimer()
  {
   pause = !pause;
  }

//+------------------------------------------------------------------+
