#property copyright           "Mokara"
#property link                "https://www.mql5.com/en/users/mokara"
#property description         "Peter Panel"
#property version             "1.0"

#include <Controls/Dialog.mqh>
#include <Controls/Button.mqh>
#include <Controls/Panel.mqh>
#include <Controls/Label.mqh>
#include <Controls/Edit.mqh>
#include <Controls/ListView.mqh>
#include <Arrays/ArrayString.mqh>

#define SPAN 6
#define DIALOG_WIDTH 300
#define DIALOG_HEIGHT 350
#define UNIT_NUM 23
#define UNIT_WIDTH 44
#define UNIT_HEIGHT 30

enum DialogElements
{
   Dialog = 0,
   Panel_Lot,
   Panel_Order,
   Panel_Edit,
   Label_Lot,
   Label_Order,
   Label_Edit,
   Button_LotMax,
   Button_LotMin,
   Button_LotNormal,
   Button_LotUp,
   Button_LotDown,
   Button_Buy,
   Button_BuyStop,
   Button_BuyLimit,
   Button_Sell,
   Button_SellStop,
   Button_SellLimit,
   Button_Modify,
   Button_Close,
   Button_Reset,
   Edit_Lot,
   List_Order,
};

enum ORDER_TYPES
{
   BUY = 0,
   BUY_STOP,
   BUY_LIMIT,
   SELL,
   SELL_STOP,
   SELL_LIMIT,
};

enum Coordinates{X1, Y1, X2, Y2};
int XY[UNIT_NUM][4];
string N[UNIT_NUM];

void SetCoordinates()
{
   XY[Dialog][X1] = 10;
   XY[Dialog][Y1] = 10;
   XY[Dialog][X2] = XY[Dialog][X1] + DIALOG_WIDTH;
   XY[Dialog][Y2] = XY[Dialog][Y1] + DIALOG_HEIGHT;
   
   //PANEL-1 VOLUME
   XY[Panel_Lot][X1] = SPAN;
   XY[Panel_Lot][Y1] = SPAN;
   XY[Panel_Lot][X2] = DIALOG_WIDTH - 2 * SPAN;
   XY[Panel_Lot][Y2] = UNIT_HEIGHT + 3 * SPAN;
      
   XY[Label_Lot][X1] = XY[Panel_Lot][X1] + SPAN;
   XY[Label_Lot][Y1] = XY[Panel_Lot][Y1] + SPAN;
   XY[Label_Lot][X2] = XY[Label_Lot][X1] + UNIT_WIDTH;
   XY[Label_Lot][Y2] = XY[Label_Lot][Y1] + UNIT_HEIGHT;
   
   XY[Edit_Lot][X1] = XY[Label_Lot][X2] + SPAN;
   XY[Edit_Lot][Y1] = XY[Label_Lot][Y1];
   XY[Edit_Lot][X2] = XY[Edit_Lot][X1] + UNIT_WIDTH;
   XY[Edit_Lot][Y2] = XY[Edit_Lot][Y1] + UNIT_HEIGHT;
   
   XY[Button_LotUp][X1] = XY[Edit_Lot][X2] + SPAN;
   XY[Button_LotUp][Y1] = XY[Edit_Lot][Y1];
   XY[Button_LotUp][X2] = XY[Button_LotUp][X1] + UNIT_WIDTH/2;
   XY[Button_LotUp][Y2] = XY[Button_LotUp][Y1] + UNIT_HEIGHT/2;
   
   XY[Button_LotDown][X1] = XY[Button_LotUp][X1];
   XY[Button_LotDown][Y1] = XY[Button_LotUp][Y2];
   XY[Button_LotDown][X2] = XY[Button_LotDown][X1] + UNIT_WIDTH/2;
   XY[Button_LotDown][Y2] = XY[Button_LotDown][Y1] + UNIT_HEIGHT/2;
   
   XY[Button_LotMax][X1] = XY[Button_LotDown][X2] + SPAN;
   XY[Button_LotMax][Y1] = XY[Button_LotUp][Y1];
   XY[Button_LotMax][X2] = XY[Button_LotMax][X1] + UNIT_WIDTH;
   XY[Button_LotMax][Y2] = XY[Button_LotMax][Y1] + UNIT_HEIGHT;
   
   XY[Button_LotMin][X1] = XY[Button_LotMax][X2] + SPAN;
   XY[Button_LotMin][Y1] = XY[Label_Lot][Y1];
   XY[Button_LotMin][X2] = XY[Button_LotMin][X1] + UNIT_WIDTH;
   XY[Button_LotMin][Y2] = XY[Button_LotMin][Y1] + UNIT_HEIGHT;
   
   XY[Button_LotNormal][X1] = XY[Button_LotMin][X2] + SPAN;
   XY[Button_LotNormal][Y1] = XY[Label_Lot][Y1];
   XY[Button_LotNormal][X2] = XY[Button_LotNormal][X1] + UNIT_WIDTH;
   XY[Button_LotNormal][Y2] = XY[Button_LotNormal][Y1] + UNIT_HEIGHT;
   
   //PANEL-2 TRADE
   XY[Panel_Order][X1] = SPAN;
   XY[Panel_Order][Y1] = XY[Panel_Lot][Y2] + SPAN;
   XY[Panel_Order][X2] = DIALOG_WIDTH - 2 * SPAN;
   XY[Panel_Order][Y2] = XY[Panel_Order][Y1] + 2 * UNIT_HEIGHT + 3 * SPAN;
   
   XY[Button_Buy][X1] = XY[Panel_Order][X1] + SPAN;
   XY[Button_Buy][Y1] = XY[Panel_Order][Y1] + SPAN;
   XY[Button_Buy][X2] = XY[Button_Buy][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_Buy][Y2] = XY[Button_Buy][Y1] + UNIT_HEIGHT;
   
   XY[Button_BuyStop][X1] = XY[Button_Buy][X2] + SPAN;
   XY[Button_BuyStop][Y1] = XY[Button_Buy][Y1];
   XY[Button_BuyStop][X2] = XY[Button_BuyStop][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_BuyStop][Y2] = XY[Button_BuyStop][Y1] + UNIT_HEIGHT;
   
   XY[Button_BuyLimit][X1] = XY[Button_BuyStop][X2] + SPAN;
   XY[Button_BuyLimit][Y1] = XY[Button_BuyStop][Y1];
   XY[Button_BuyLimit][X2] = XY[Button_BuyLimit][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_BuyLimit][Y2] = XY[Button_BuyLimit][Y1] + UNIT_HEIGHT;
   
   XY[Button_Sell][X1] = XY[Button_Buy][X1];
   XY[Button_Sell][Y1] = XY[Button_Buy][Y1] + UNIT_HEIGHT + SPAN;
   XY[Button_Sell][X2] = XY[Button_Sell][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_Sell][Y2] = XY[Button_Sell][Y1] + UNIT_HEIGHT;
   
   XY[Button_SellStop][X1] = XY[Button_Sell][X2] + SPAN;
   XY[Button_SellStop][Y1] = XY[Button_Sell][Y1];
   XY[Button_SellStop][X2] = XY[Button_SellStop][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_SellStop][Y2] = XY[Button_SellStop][Y1] + UNIT_HEIGHT;
   
   XY[Button_SellLimit][X1] = XY[Button_SellStop][X2] + SPAN;
   XY[Button_SellLimit][Y1] = XY[Button_SellStop][Y1];
   XY[Button_SellLimit][X2] = XY[Button_SellLimit][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_SellLimit][Y2] = XY[Button_SellLimit][Y1] + UNIT_HEIGHT;
   
   //PANEL-3 MODIFY  
   XY[Panel_Edit][X1] = SPAN;
   XY[Panel_Edit][Y1] = XY[Panel_Order][Y2] + SPAN;
   XY[Panel_Edit][X2] = DIALOG_WIDTH - 2 * SPAN;
   XY[Panel_Edit][Y2] = DIALOG_HEIGHT - 6 * SPAN;
   
   XY[List_Order][X1] = XY[Panel_Edit][X1] + SPAN;
   XY[List_Order][Y1] = XY[Panel_Edit][Y1] + SPAN;
   XY[List_Order][X2] = DIALOG_WIDTH - 3 * SPAN;
   XY[List_Order][Y2] = XY[Panel_Edit][Y2] - UNIT_HEIGHT - 2 * SPAN;
   
   XY[Button_Modify][X1] = XY[List_Order][X1];
   XY[Button_Modify][Y1] = XY[List_Order][Y2] + SPAN;
   XY[Button_Modify][X2] = XY[Button_Modify][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_Modify][Y2] = XY[Button_Modify][Y1] + UNIT_HEIGHT;
   
   XY[Button_Close][X1] = XY[Button_Modify][X2] + SPAN;
   XY[Button_Close][Y1] = XY[Button_Modify][Y1];
   XY[Button_Close][X2] = XY[Button_Close][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_Close][Y2] = XY[Button_Close][Y1] + UNIT_HEIGHT;
   
   XY[Button_Reset][X1] = XY[Button_Close][X2] + SPAN;
   XY[Button_Reset][Y1] = XY[Button_Close][Y1];
   XY[Button_Reset][X2] = XY[Button_Reset][X1] + 2 * UNIT_WIDTH - SPAN/3;
   XY[Button_Reset][Y2] = XY[Button_Reset][Y1] + UNIT_HEIGHT;
}

void SetNames()
{
   N[Dialog] = "Peter Panel 1.0";
   N[Panel_Lot] = "Lot Panel";
   N[Panel_Order] = "Order Panel";
   N[Panel_Edit] = "Edit Panel";
   N[Label_Lot] = "LOTS: ";
   N[Button_LotMax] = "Max";
   N[Button_LotMin] = "Min";
   N[Button_LotNormal] = "Norm";
   N[Button_LotUp] = "+";
   N[Button_LotDown] = "-";
   N[Button_Buy] = "Buy";
   N[Button_BuyStop] = "Buy Stop";
   N[Button_BuyLimit] = "Buy Limit";
   N[Button_Sell] = "Sell";
   N[Button_SellStop] = "Sell Stop";
   N[Button_SellLimit] = "Sell Limit";
   N[Button_Modify] = "Modify";
   N[Button_Close] = "Close";
   N[Button_Reset] = "Reset";
   N[Edit_Lot] = "Edit Lot";
   N[List_Order] = "List Orders";
}

class CControlsDialog : public CAppDialog
{
   private:
      CPanel panels[3];
      CButton buttons[14];
      CLabel labels[1];
      CEdit edits[1];
      CListView lists[1];
            
      double lotMin;
      double lotMax;
      double lotStep;
      double lotSet;
      double pAqua;
      double pGreen;
      double pRed;     
      
      CArrayString Positions;
      CArrayString Orders;
      
      long selTicket;
   
   public:
      CControlsDialog(void);
      ~CControlsDialog(void);
      virtual bool Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2);
      virtual bool OnEvent(const int id, const long &lparam, const double &dparam, const string &sparam);
      
      void GetLot();
      bool CheckLot();
      void SetLines();
      void SetLines(double a, double g, double r);
      bool GetLines();
      void ClearLines();
      bool Trade(int o);
      void GetTrades();
      void ListTrades();
      void RefreshTrades();
   
   protected:
      bool CreatePanel(void);
      bool CreateLabel(void);
      bool CreateButton(void);
      bool CreateEdit(void);
      bool CreateList(void);
      void OnClickButton_LotUp(void);
      void OnClickButton_LotDown(void);
      void OnClickButton_LotMax(void);
      void OnClickButton_LotMin(void);
      void OnClickButton_LotNormal(void);
      void OnClickButton_Buy(void);
      void OnClickButton_BuyStop(void);
      void OnClickButton_BuyLimit(void);
      void OnClickButton_Sell(void);
      void OnClickButton_SellStop(void);
      void OnClickButton_SellLimit(void);
      void OnClickButton_Modify(void);
      void OnClickButton_Close(void);
      void OnClickButton_Reset(void);
      void OnClickListItem(void);
};

EVENT_MAP_BEGIN(CControlsDialog)
ON_EVENT(ON_CLICK, buttons[Button_LotUp-Button_LotMax], OnClickButton_LotUp)
ON_EVENT(ON_CLICK, buttons[Button_LotDown-Button_LotMax], OnClickButton_LotDown)
ON_EVENT(ON_CLICK, buttons[Button_LotMax-Button_LotMax], OnClickButton_LotMax)
ON_EVENT(ON_CLICK, buttons[Button_LotMin-Button_LotMax], OnClickButton_LotMin)
ON_EVENT(ON_CLICK, buttons[Button_LotNormal-Button_LotMax], OnClickButton_LotNormal)
ON_EVENT(ON_CLICK, buttons[Button_Buy-Button_LotMax], OnClickButton_Buy)
ON_EVENT(ON_CLICK, buttons[Button_BuyStop-Button_LotMax], OnClickButton_BuyStop)
ON_EVENT(ON_CLICK, buttons[Button_BuyLimit-Button_LotMax], OnClickButton_BuyLimit)
ON_EVENT(ON_CLICK, buttons[Button_Sell-Button_LotMax], OnClickButton_Sell)
ON_EVENT(ON_CLICK, buttons[Button_SellStop-Button_LotMax], OnClickButton_SellStop)
ON_EVENT(ON_CLICK, buttons[Button_SellLimit-Button_LotMax], OnClickButton_SellLimit)
ON_EVENT(ON_CLICK, buttons[Button_Modify-Button_LotMax], OnClickButton_Modify)
ON_EVENT(ON_CLICK, buttons[Button_Close-Button_LotMax], OnClickButton_Close)
ON_EVENT(ON_CLICK, buttons[Button_Reset-Button_LotMax], OnClickButton_Reset)
ON_EVENT(ON_CHANGE, lists[0], OnClickListItem);
EVENT_MAP_END(CAppDialog)

CControlsDialog::CControlsDialog(void)
{
   GetLot();
   SetLines();
}

CControlsDialog::~CControlsDialog(void){}


void CControlsDialog::GetLot(void)
{
   lotMin = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MIN);
   lotMax = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_MAX);
   lotStep = SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP);
   lotSet = StringToDouble(edits[0].Text());
}

bool CControlsDialog::CheckLot(void)
{
   GetLot();
   if(lotSet > lotMax || lotSet < lotMin || lotSet == 0) return(false);
   return(true);
}

void CControlsDialog::SetLines()
{
   double y1 = ChartGetDouble(0, CHART_PRICE_MIN);
   double y2 = ChartGetDouble(0, CHART_PRICE_MAX);
   pAqua = (y1+y2)/2;
   pGreen = (y1+y2)/2 + (y2-y1)/5;
   pRed = (y1+y2)/2 - (y2-y1)/5;
   SetLines(pAqua, pGreen, pRed);   
}

void CControlsDialog::SetLines(double a, double g, double r)
{
   if(ObjectFind(0, "AQUA") == 0) ObjectDelete(0, "AQUA");
   ObjectCreate(0, "AQUA", OBJ_HLINE, 0, 0, a);
   ObjectSetInteger(0, "AQUA", OBJPROP_COLOR, clrAqua);
   ObjectSetInteger(0, "AQUA", OBJPROP_STYLE, STYLE_SOLID);
   ObjectSetInteger(0, "AQUA", OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, "AQUA", OBJPROP_BACK, true);
   
   if(ObjectFind(0, "GREEN") == 0) ObjectDelete(0, "GREEN");
   ObjectCreate(0, "GREEN", OBJ_HLINE, 0, 0, g);
   ObjectSetInteger(0, "GREEN", OBJPROP_COLOR, clrLimeGreen);
   ObjectSetInteger(0, "GREEN", OBJPROP_STYLE, STYLE_SOLID);
   ObjectSetInteger(0, "GREEN", OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, "GREEN", OBJPROP_BACK, true);
   
   if(ObjectFind(0, "RED") == 0) ObjectDelete(0, "RED");
   ObjectCreate(0, "RED", OBJ_HLINE, 0, 0, r);
   ObjectSetInteger(0, "RED", OBJPROP_COLOR, clrTomato);
   ObjectSetInteger(0, "RED", OBJPROP_STYLE, STYLE_SOLID);
   ObjectSetInteger(0, "RED", OBJPROP_SELECTABLE, true);
   ObjectSetInteger(0, "RED", OBJPROP_BACK, true);
}

bool CControlsDialog::GetLines(void)
{
   if(ObjectFind(0, "AQUA") == 0) {pAqua = ObjectGetDouble(0, "AQUA", OBJPROP_PRICE);}
   else {pAqua = 0; return(false);}
   
   if(ObjectFind(0, "GREEN") == 0) {pGreen = ObjectGetDouble(0, "GREEN", OBJPROP_PRICE);}
   else {pGreen = 0; return(false);}
   
   if(ObjectFind(0, "RED") == 0) {pRed = ObjectGetDouble(0, "RED", OBJPROP_PRICE);}
   else {pRed = 0; return(false);}
   
   return(true);
}

void CControlsDialog::ClearLines(void)
{
   if(ObjectFind(0, "AQUA") == 0) ObjectDelete(0, "AQUA");
   if(ObjectFind(0, "GREEN") == 0) ObjectDelete(0, "GREEN");
   if(ObjectFind(0, "RED") == 0) ObjectDelete(0, "RED");
}

bool CControlsDialog::Trade(int o)
{   
   MqlTradeRequest tReq;
   MqlTradeResult tRes;
   ZeroMemory(tReq);
   ZeroMemory(tRes);
   
   if(!CheckLot()) return(false);
   if(!GetLines()) return(false);
   
   tReq.symbol = _Symbol;
   tReq.volume = lotSet;
   tReq.deviation = 10;
   
   switch(o)
   {
      case BUY: tReq.type = ORDER_TYPE_BUY; break;
      case BUY_STOP: tReq.type = ORDER_TYPE_BUY_STOP; break;
      case BUY_LIMIT: tReq.type = ORDER_TYPE_BUY_LIMIT; break;
      case SELL: tReq.type = ORDER_TYPE_SELL; break;
      case SELL_STOP: tReq.type = ORDER_TYPE_SELL_STOP; break;
      case SELL_LIMIT: tReq.type = ORDER_TYPE_SELL_LIMIT; break;
   }
   
   if(o == BUY) tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);   
   if(o == SELL) tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_BID);   
   if(o == BUY_STOP || o == BUY_LIMIT || o == SELL_STOP || o == SELL_LIMIT) tReq.price = pAqua;      
   if(o == BUY || o == BUY_STOP || o == BUY_LIMIT) {tReq.tp = pGreen; tReq.sl = pRed;}   
   if(o == SELL || o == SELL_STOP || o == SELL_LIMIT) {tReq.tp = pRed; tReq.sl = pGreen;}   
   if(o == BUY || o == SELL) tReq.action = TRADE_ACTION_DEAL; else tReq.action = TRADE_ACTION_PENDING;

   if(!OrderSend(tReq, tRes)) {Print("ERROR: invalid order. check levels and volume again. Error Code: " + tRes.retcode); return(false);}
  
   return(true);
}

void CControlsDialog::GetTrades(void)
{
   int ticket;
   string symbol, item, sType;
   double price, takeProfit, stopLoss, volume;
   ENUM_ORDER_TYPE type;  
   
   //OPENED POSITIONS
   for(int i = 0; i < PositionsTotal(); i++)
   {
      ticket = PositionGetTicket(i);
      symbol = PositionGetSymbol(i);
      if(PositionSelectByTicket(ticket) && symbol == _Symbol)
      {
         type = PositionGetInteger(POSITION_TYPE);
         switch(type)
         {
            case ORDER_TYPE_BUY: sType = "BUY"; break;
            case ORDER_TYPE_BUY_STOP: sType = "BUY_STOP"; break;
            case ORDER_TYPE_BUY_LIMIT: sType = "BUY_LIMIT"; break;
            case ORDER_TYPE_SELL: sType = "SELL"; break;
            case ORDER_TYPE_SELL_STOP: sType = "SELL_STOP"; break;
            case ORDER_TYPE_SELL_LIMIT: sType = "SELL_LIMIT"; break;
         }
         price = PositionGetDouble(POSITION_PRICE_OPEN);
         takeProfit = PositionGetDouble(POSITION_TP);
         stopLoss = PositionGetDouble(POSITION_SL);
         volume = PositionGetDouble(POSITION_VOLUME);
         item = ticket + "#" + sType + "#" + DoubleToString(price, _Digits) + "#" + DoubleToString(takeProfit, _Digits) + "#" + DoubleToString(stopLoss, _Digits) + "#" + volume;
         Positions.Add(item);
      }
   }
   
   //PENDING ORDERS
   for(int i = 0; i < OrdersTotal(); i++)
   {
      ticket = OrderGetTicket(i);
      if(OrderSelect(ticket))
      {
         symbol = OrderGetString(ORDER_SYMBOL);
         if(symbol == _Symbol)
         {
            type = OrderGetInteger(ORDER_TYPE);
            if(type == ORDER_TYPE_BUY_STOP || type == ORDER_TYPE_BUY_LIMIT || type == ORDER_TYPE_SELL_STOP || type == ORDER_TYPE_SELL_LIMIT)
            {
               switch(type)
               {
                  case ORDER_TYPE_BUY: sType = "BUY"; break;
                  case ORDER_TYPE_BUY_STOP: sType = "BUY_STOP"; break;
                  case ORDER_TYPE_BUY_LIMIT: sType = "BUY_LIMIT"; break;
                  case ORDER_TYPE_SELL: sType = "SELL"; break;
                  case ORDER_TYPE_SELL_STOP: sType = "SELL_STOP"; break;
                  case ORDER_TYPE_SELL_LIMIT: sType = "SELL_LIMIT"; break;
               }
               price = OrderGetDouble(ORDER_PRICE_OPEN);
               takeProfit = OrderGetDouble(ORDER_TP);
               stopLoss = OrderGetDouble(ORDER_SL);
               volume = OrderGetDouble(ORDER_VOLUME_CURRENT);
               item = ticket + "#" + sType + "#" + DoubleToString(price, _Digits) + "#" + DoubleToString(takeProfit, _Digits) + "#" + DoubleToString(stopLoss, _Digits) + "#" + volume;
               Orders.Add(item);
            }
         }
      }
   }
}

void CControlsDialog::ListTrades(void)
{
   for(int i = 0; i < Positions.Total(); i++)
   {
      lists[0].AddItem(Positions.At(i));
   }
   
   for(int i = 0; i < Orders.Total(); i++)
   {
      lists[0].AddItem(Orders.At(i));
   }   
}

void CControlsDialog::RefreshTrades(void)
{
   Orders.DeleteRange(0, Orders.Total());
   Positions.DeleteRange(0, Positions.Total());
   lists[0].ItemsClear();
   GetTrades();
   ListTrades();
}

bool CControlsDialog::Create(const long chart, const string name, const int subwin, const int x1, const int y1, const int x2, const int y2)
{
   if(!CAppDialog::Create(chart, name, subwin, x1, y1, x2, y2)) return(false);
   if(!CreatePanel()) return(false);
   if(!CreateLabel()) return(false);
   if(!CreateButton()) return(false);
   if(!CreateEdit()) return(false);
   if(!CreateList()) return(false);
   return(true);
}

bool CControlsDialog::CreatePanel(void)
{
   for(int i = 0; i < 3; i++)
   {
      if(!panels[i].Create(m_chart_id, N[Panel_Lot+i], m_subwin, XY[Panel_Lot+i][X1], XY[Panel_Lot+i][Y1], XY[Panel_Lot+i][X2], XY[Panel_Lot+i][Y2])) return(false);
      if(!panels[i].ColorBackground(clrGainsboro)) return(false);
      if(!panels[i].ColorBorder(clrBlack)) return(false);
      if(!Add(panels[i])) return(false);
   }     
   return(true);
}

bool CControlsDialog::CreateLabel(void)
{
   for(int i = 0; i < 1; i++)
   {
      if(!labels[i].Create(m_chart_id, N[Label_Lot+i], m_subwin, XY[Label_Lot+i][X1], XY[Label_Lot+i][Y1]+SPAN, XY[Label_Lot+i][X2], XY[Label_Lot+i][Y2])) return(false);
      if(!labels[i].Text(N[Label_Lot+i])) return(false);
      if(!labels[i].Font("Arial Black")) return(false);
      if(!Add(labels[i])) return(false); 
   }
   return(true);
}

bool CControlsDialog::CreateButton(void)
{
   for(int i = 0; i < 14; i++)
   {
      if(!buttons[i].Create(m_chart_id, N[Button_LotMax+i], m_subwin, XY[Button_LotMax+i][X1], XY[Button_LotMax+i][Y1], XY[Button_LotMax+i][X2], XY[Button_LotMax+i][Y2])) return(false);
      if(!buttons[i].Text(N[Button_LotMax+i])) return(false);
      if(!buttons[i].ColorBackground(clrDarkGray)) return(false);
      if(!buttons[i].ColorBorder(clrBlack)) return(false);
      if(!buttons[i].Font("Arial Black")) return(false);
      if(!Add(buttons[i])) return(false);
   }   
   return(true);
}

bool CControlsDialog::CreateEdit(void)
{
   if(!edits[0].Create(m_chart_id, N[Edit_Lot], m_subwin, XY[Edit_Lot][X1], XY[Edit_Lot][Y1], XY[Edit_Lot][X2], XY[Edit_Lot][Y2])) return(false);
   if(!Add(edits[0])) return(false);
   return(true);
}

bool CControlsDialog::CreateList(void)
{
   if(!lists[0].Create(m_chart_id, N[List_Order], m_subwin, XY[List_Order][X1], XY[List_Order][Y1], XY[List_Order][X2], XY[List_Order][Y2])) return(false);
   if(!Add(lists[0])) return(false);
   return(true);
}

void CControlsDialog::OnClickButton_LotUp(void)
{   
   GetLot();
   if(lotSet < lotMin) {lotSet = lotMin; edits[0].Text(DoubleToString(lotSet, 2)); return;}
   if(lotSet > lotMax) {lotSet = lotMax; edits[0].Text(DoubleToString(lotSet, 2)); return;}
   if(lotSet >= lotMin && lotSet < lotMax) lotSet += lotStep;
   edits[0].Text(DoubleToString(lotSet, 2));   
}

void CControlsDialog::OnClickButton_LotDown(void)
{
   GetLot();
   if(lotSet < lotMin) {lotSet = lotMin; edits[0].Text(DoubleToString(lotSet, 2)); return;}
   if(lotSet > lotMax) {lotSet = lotMax; edits[0].Text(DoubleToString(lotSet, 2)); return;}
   if(lotSet > lotMin && lotSet <= lotMax) lotSet -= lotStep;
   edits[0].Text(DoubleToString(lotSet, 2));
}

void CControlsDialog::OnClickButton_LotMax(void)
{   
   double m1, m2, l;
   GetLot();
   if(!OrderCalcMargin(ORDER_TYPE_BUY, _Symbol, 1, SymbolInfoDouble(_Symbol, SYMBOL_ASK), m1)) return;
   if(!OrderCalcMargin(ORDER_TYPE_SELL, _Symbol, 1, SymbolInfoDouble(_Symbol, SYMBOL_BID), m2)) return;
   if(m2 < m1) m1 = m2;
   l = AccountInfoDouble(ACCOUNT_MARGIN_FREE)/m1 - lotMin;
   if(l < lotMin)
   {
      Alert("ERROR: not enough margin.");
      return;
   }
   lotSet = l;
   edits[0].Text(DoubleToString(lotSet, 2));
}

void CControlsDialog::OnClickButton_LotMin(void)
{
   GetLot();
   lotSet = lotMin;
   edits[0].Text(DoubleToString(lotSet, 2));
}

void CControlsDialog::OnClickButton_LotNormal(void)
{
   GetLot();
   int r = (int)MathRound(lotSet/lotStep);
   if(MathAbs(r * lotStep - lotSet) > 0.0000001) lotSet = r * lotStep;
   if(lotSet > lotMax) lotSet = lotMax;
   if(lotSet < lotMin) lotSet = lotMin;   
   edits[0].Text(DoubleToString(lotSet, 2));
}

void CControlsDialog::OnClickButton_Buy(void)
{   
   if(!Trade(BUY)){Alert("ERROR: buy order cannot be sent. check levels and volume again."); return;}   
}

void CControlsDialog::OnClickButton_BuyStop(void)
{
   if(!Trade(BUY_STOP)){Alert("ERROR: buy stop order cannot be sent. check levels and volume again."); return;}   
}

void CControlsDialog::OnClickButton_BuyLimit(void)
{
   if(!Trade(BUY_LIMIT)){Alert("ERROR: buy limit order cannot be sent. check levels and volume again."); return;}   
}

void CControlsDialog::OnClickButton_Sell(void)
{
   if(!Trade(SELL)){Alert("ERROR: sell order cannot be sent. check levels and volume again."); return;}   
}

void CControlsDialog::OnClickButton_SellStop(void)
{
   if(!Trade(SELL_STOP)){Alert("ERROR: sell stop order cannot be sent. check levels and volume again."); return;}   
}

void CControlsDialog::OnClickButton_SellLimit(void)
{
   if(!Trade(SELL_LIMIT)){Alert("ERROR: sell limit order cannot be sent. check levels and volume again."); return;}   
}

void CControlsDialog::OnClickListItem(void)
{
   string a[6];
   StringSplit(lists[0].Select(), '#', a);
   selTicket = StringToInteger(a[0]);
   double aqua = 0, green = 0, red = 0;
   
   if(selTicket == 0) return;
   
   if(PositionSelectByTicket(selTicket))
   {
      aqua = PositionGetDouble(POSITION_PRICE_OPEN);
      if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
      {
         green = PositionGetDouble(POSITION_TP);
         red = PositionGetDouble(POSITION_SL);
      }
      if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
      {
         green = PositionGetDouble(POSITION_SL);
         red = PositionGetDouble(POSITION_TP);
      }      
   }
   
   if(OrderSelect(selTicket))
   {
      aqua = OrderGetDouble(ORDER_PRICE_OPEN);
      if(OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_BUY_STOP || OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_BUY_LIMIT)
      {
         green = OrderGetDouble(ORDER_TP);
         red = OrderGetDouble(ORDER_SL);
      }
      if(OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_SELL_STOP || OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_SELL_LIMIT)
      {
         green = OrderGetDouble(ORDER_SL);
         red = OrderGetDouble(ORDER_TP);
      }   
   }
   
   SetLines(aqua, green, red);
}

void CControlsDialog::OnClickButton_Modify(void)
{
   string a[6];
   StringSplit(lists[0].Select(), '#', a);
   selTicket = StringToInteger(a[0]);
   MqlTradeRequest tReq;
   MqlTradeResult tRes;
   ZeroMemory(tReq);
   ZeroMemory(tRes);
   
   if(PositionSelectByTicket(selTicket))
   {
      tReq.symbol = PositionGetString(POSITION_SYMBOL);
      tReq.action = TRADE_ACTION_SLTP;
      tReq.position = selTicket;
      if(!GetLines())
      {
         Alert("ERROR: one or more lines are missing.");
         return;
      }
      if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_BUY)
      {
         tReq.tp = pGreen;
         tReq.sl = pRed;
      }
      if(PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL)
      {
         tReq.tp = pRed;
         tReq.sl = pGreen;         
      }
      if(!OrderSend(tReq, tRes))
      {
         Alert("ERROR: position modify error.");
         return;
      }      
   }
   
   if(OrderSelect(selTicket))
   {
      tReq.symbol = OrderGetString(ORDER_SYMBOL);
      tReq.action = TRADE_ACTION_MODIFY;
      tReq.order = selTicket;            
      if(!GetLines())
      {
         Alert("ERROR: one or more lines are missing.");
         return;
      }
      if(OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_BUY_STOP || OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_BUY_LIMIT)
      {
         tReq.price = pAqua;
         tReq.tp = pGreen;
         tReq.sl = pRed;
      }
      if(OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_SELL_STOP || OrderGetInteger(ORDER_TYPE) == ORDER_TYPE_SELL_LIMIT)
      {
         tReq.price = pAqua;
         tReq.tp = pRed;
         tReq.sl = pGreen;
      }
      if(!OrderSend(tReq, tRes))
      {
         Alert("ERROR: order modify error.");
         return;
      }
   }
   
}

void CControlsDialog::OnClickButton_Close(void)
{
   string symbol;
   ENUM_POSITION_TYPE pType;
   MqlTradeRequest tReq = {0};
   MqlTradeResult tRes = {0};
   
   //OPENED POSITIONS
   if(PositionSelectByTicket(selTicket))
   {
      symbol = PositionGetString(POSITION_SYMBOL);
      if(symbol == _Symbol)
      {
         pType = PositionGetInteger(POSITION_TYPE);
         
         if(pType == POSITION_TYPE_BUY)
         {
            tReq.type = ORDER_TYPE_SELL;
            tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_BID);
         }
         
         if(pType == POSITION_TYPE_SELL)
         {
            tReq.type = ORDER_TYPE_BUY;
            tReq.price = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
         }
         
         tReq.volume = PositionGetDouble(POSITION_VOLUME);
         tReq.position = selTicket;
         tReq.tp = 0;
         tReq.sl = 0;
         tReq.action = TRADE_ACTION_DEAL;
         tReq.deviation = 10;
         tReq.symbol = _Symbol;         
         
         if(!OrderSend(tReq, tRes) || tRes.retcode != TRADE_RETCODE_DONE)
         {
            Alert("ERROR: opened position close. " + tRes.retcode);
            return;
         }
      }
   }
   
   //PENDING ORDERS
   if(OrderSelect(selTicket))
   {
      ZeroMemory(tReq);
      ZeroMemory(tRes);
      tReq.action = TRADE_ACTION_REMOVE;
      tReq.order = selTicket;
      
      if(!OrderSend(tReq, tRes) || tRes.retcode != TRADE_RETCODE_DONE)
      {
         Alert("ERROR: pending order close. " + tRes.retcode);
         return;
      }
   }
}

void CControlsDialog::OnClickButton_Reset(void)
{
   SetLines();
}

CControlsDialog ExtDialog;
int OnInit()
{
   SetCoordinates();
   SetNames();
   if(!ExtDialog.Create(0, N[Dialog], 0, XY[Dialog][X1], XY[Dialog][Y1], XY[Dialog][X2], XY[Dialog][Y2]))
      return(INIT_FAILED);
   ExtDialog.Run();
   ExtDialog.GetTrades();
   ExtDialog.ListTrades();
   return(INIT_SUCCEEDED);
}

void OnDeinit(const int reason)
{
   ExtDialog.ClearLines();
   ExtDialog.Destroy(reason);
}

void OnChartEvent(const int id, const long &lparam, const double &dparam, const string &sparam)
{
   ExtDialog.ChartEvent(id, lparam, dparam, sparam);
   ExtDialog.GetLines();
}

void OnTradeTransaction(const MqlTradeTransaction &trans, const MqlTradeRequest &request, const MqlTradeResult &result) 
{
   ExtDialog.RefreshTrades();
}