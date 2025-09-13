//+------------------------------------------------------------------+
//|                                                    TradePane.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Objects.mqh"
#include "CommonUID.mqh"
#include "TradeUtils.mqh"
#include "Utils.mqh"
#include "PaneSettings.mqh"
#include "Time.mqh"
#include "Comments.mqh"
#include "Trading.mqh"

#include "../Trade/SymbolInfo.mqh"

class TradePane
{
public:
   TradePane();
   ~TradePane();

   void Init();
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   void OnTick();
   void Show(bool needShow = true);
   
   bool Hidden() const;

private:
   void Draw();
   void Slide(bool needShow);
   void Hide();
   
   void DrawHideBorder();
   void DrawShowBtn();
   void DrawHideBtn();
   
   void DrawBuyWithTP();
   void DrawBuyWithSL();
   void DrawBuyStop();
   void DrawBuyLimit();

   void DrawSellWithTP();
   void DrawSellWithSL();
   void DrawSellStop();
   void DrawSellLimit();
   
   void DrawIncreaseLotBtn();
   void DrawDecreaseLotBtn();
   void DrawLots();
   
   void DrawAsk();
   void DrawBid();
   
   void DrawBuy();
   void DrawSell();
   void DrawClose();
   void DrawReverse();
   
   void DrawArea();

   void OnSettingChanged(int id, string value);
   
   void OnBuySL();
   void OnBuyTP();
   void OnBuyLimit();
   void OnBuyStop();
   void OnSellSL();
   void OnSellTP();
   void OnSellLimit();
   void OnSellStop();   

   void OnBuy();
   void OnSell();
   void OnClose();
   void OnReverse();
   
   bool CheckDropArea(string name);

private:
   bool m_UseSliding;
   bool m_Hidden;
   bool m_Inited;
   
   int m_UID;
   
   string m_HideBorder;

   string m_BuyBtn;
   string m_SellBtn;
   string m_ShowBtn;
   string m_HideBtn;

   string m_BuyWithTP;
   string m_BuyWithSL;
   string m_BuyStop;
   string m_BuyLimit;

   string m_SellWithTP;
   string m_SellWithSL;
   string m_SellStop;
   string m_SellLimit;
   
   string m_IncreaseLotBtn;
   string m_DecreaseLotBtn;
   string m_LotsEdit;
   
   string m_PreAsk;
   string m_LargeAsk;
   string m_PostAsk;
   string m_PreBid;
   string m_LargeBid;
   string m_PostBid;
   
   string m_CloseBtn;
   string m_ReverseBtn;
   string m_PaneArea;
   
   string m_BuyBtn_;
   string m_SellBtn_;
   string m_CloseBtn_;
   string m_ReverseBtn_;

   double m_LotsSize;
   int m_LotsDigits;
   
   color m_BordersColor;
   color m_TextColor;
   color m_BuyColor;
   color m_SellColor;
   color m_UpColor;
   color m_DnColor;
   
   CSymbolInfo m_SymbolInfo;
   
   PaneSettings m_Settings;
   
   string m_Comment;
   
   int m_SpreadInfoHandle;
   
   int m_BaseX;
   int m_BaseY;
   
   MyTrade m_Trade;
};

TradePane::TradePane(void)
{
   m_LotsSize = 0;
   m_LotsDigits = 0;
   m_BaseX = 0;
   m_BaseY = 0;
   
   m_UseSliding = true;
   m_Hidden = true;
   m_Inited = false;
   
   m_UID = GetUID();
   string sUID = string(m_UID);
   
   m_HideBorder = "HideBorder" + sUID;

   m_BuyBtn = "BuyBtn" + sUID;
   m_SellBtn = "SellBtn" + sUID;
   m_ShowBtn = "ShowBtn" + sUID;
   m_HideBtn = "HideBtn" + sUID;

   m_BuyWithTP = "BuyWithTP" + sUID;
   m_BuyWithSL = "BuyWithSL" + sUID;
   m_BuyStop = "BuyStop" + sUID;
   m_BuyLimit = "BuyLimit" + sUID;

   m_SellWithTP = "SellWithTP" + sUID;
   m_SellWithSL = "SellWithSL" + sUID;
   m_SellStop = "SellStop" + sUID;
   m_SellLimit = "SellLimit" + sUID;
   
   m_IncreaseLotBtn = "IncreaseLotBtn" + sUID;
   m_DecreaseLotBtn = "DecreaseLotBtn" + sUID;
   m_LotsEdit = "Lots" + sUID;
   
   m_PreAsk = "PreAsk" + sUID;
   m_LargeAsk = "LargeAsk" + sUID;
   m_PostAsk = "PostAsk" + sUID;
   
   m_PreBid = "PreBid" + sUID;
   m_LargeBid = "LargeBid" + sUID;
   m_PostBid = "PostBid" + sUID;
   
   m_CloseBtn = "CloseBtn" + sUID;
   m_ReverseBtn = "ReverseBtn" + sUID;
   
   m_PaneArea = "PaneArea" + sUID;
   
   m_BuyBtn_ = "BuyBtn_" + sUID;
   m_SellBtn_ = "SellBtn_" + sUID;
   m_CloseBtn_ = "CloseBtn_" + sUID;
   m_ReverseBtn_ = "ReverseBtn_" + sUID;
   
   m_BordersColor = Black;
   m_TextColor = LightGray;
   m_BuyColor = DodgerBlue;
   m_SellColor = Tomato;
   m_UpColor = SeaGreen;
   m_DnColor = Tomato;
   
   m_Comment = "Trade Xpert";
   
   m_Settings.AddBoolSetting("Use Sliding", SETTING_USE_SLIDING_ID, true);
   
   SelectionTable colors;
   colors.AddSelection(1, "Light\\Dark");
   colors.AddSelection(2, "Dark\\Light");
   
   m_Settings.AddSelectionSetting("Color scheme", SETTING_COLOR_SCHEME_ID, 2, colors);
   m_Settings.AddStringSetting("Orders Comment", SETTING_COMMENT_ID, m_Comment);
   
   m_Settings.AddIntSetting("Log Font Size", SETTING_LOG_FONT_SIZE,8, 1, 5, 20);
   
   SelectionTable logColors;
   logColors.AddSelection(LightGreen, "LightGreen");
   logColors.AddSelection(LightGray, "LightGray");
   logColors.AddSelection(White, "White");
   logColors.AddSelection(Black, "Black");
   logColors.AddSelection(Salmon, "Salmon");
   logColors.AddSelection(PowderBlue, "PowderBlue");
   logColors.AddSelection(DarkGray, "DarkGray");
   logColors.AddSelection(MidnightBlue, "MidnightBlue");

   m_Settings.AddSelectionSetting("Log Font Color", SETTING_LOG_FONT_COLOR, LightGray, logColors);

   m_Settings.AddIntSetting("Log Lines", SETTING_LOG_FONT_LINES, 10, 1, 1, 20);

   m_Settings.AddIntSetting("Log Line Length", SETTING_LOG_FONT_LENGTH, 60, 1, 20, 62);

   m_Settings.AddIntSetting("Log Tab Size", SETTING_LOG_FONT_TABSIZE, 8, 1, 2, 20);

   SelectionTable time;
   time.AddSelection(ETIME_CET, "CET");
   time.AddSelection(ETIME_EST, "EST");
   time.AddSelection(ETIME_GMT, "GMT");
   time.AddSelection(ETIME_LOCAL, "Local");
   time.AddSelection(ETIME_MOSCOW, "Moscow");
   time.AddSelection(ETIME_SERVER, "Server");

   m_Settings.AddSelectionSetting("Time to show", SETTING_TIME, ETIME_SERVER, time);
   
   m_Settings.AddIntSetting("Spread Bars", SETTING_SPREAD_BARS, 5, 1, 1, 50);

   m_Settings.AddIntSetting("Slippage", SETTING_TRADE_SLIPPAGE, 5, 1, 1, 1000);
   
   SelectionTable fillings;
   fillings.AddSelection(ORDER_FILLING_FOK, "Fill or Kill");
   fillings.AddSelection(ORDER_FILLING_IOC, "Available");
   fillings.AddSelection(ORDER_FILLING_RETURN, "Available+");

   m_Settings.AddSelectionSetting("Order filling", SETTING_TRADE_FILLING, ORDER_FILLING_FOK, fillings);

   m_Settings.SetColors(m_BordersColor, m_TextColor);
   
   m_SpreadInfoHandle = iCustom(_Symbol, PERIOD_CURRENT, "SpreadInfo");
   
}

void TradePane::Init()
{
   m_SymbolInfo.Name(_Symbol);
   m_LotsDigits = DoubleDigits(m_SymbolInfo.LotsStep());
   
   Draw();
   m_Settings.Show(false);
}

void TradePane::Hide()
{
   ObjectDelete(0, m_BuyBtn);
   ObjectDelete(0, m_SellBtn);
   ObjectDelete(0, m_ShowBtn);
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_BuyWithTP);
   ObjectDelete(0, m_BuyWithSL);
   ObjectDelete(0, m_BuyStop);
   ObjectDelete(0, m_BuyLimit);
   ObjectDelete(0, m_SellWithTP);
   ObjectDelete(0, m_SellWithSL);
   ObjectDelete(0, m_SellStop);
   ObjectDelete(0, m_SellLimit);

   ObjectDelete(0, m_IncreaseLotBtn);
   ObjectDelete(0, m_DecreaseLotBtn);
   ObjectDelete(0, m_LotsEdit);
   
   ObjectDelete(0, m_PreAsk);
   ObjectDelete(0, m_LargeAsk);
   ObjectDelete(0, m_PostAsk);
   ObjectDelete(0, m_PreBid);
   ObjectDelete(0, m_LargeBid);
   ObjectDelete(0, m_PostBid);
   
   ObjectDelete(0, m_CloseBtn);
   ObjectDelete(0, m_ReverseBtn);

   ObjectDelete(0, m_PaneArea);
   
   ObjectDelete(0, m_BuyBtn_);
   ObjectDelete(0, m_SellBtn_);
   ObjectDelete(0, m_CloseBtn_);
   ObjectDelete(0, m_ReverseBtn_);
}

void TradePane::Show(bool needShow)
{
   if (needShow && m_Hidden)
   {
      m_Hidden = false;
      ObjectDelete(0, m_ShowBtn);
      Slide(true);
   }
   else if (!needShow && !m_Hidden)
   {
      m_Hidden = true;
      Hide();
      Slide(false);
   }
   
   Draw();
}

void TradePane::Draw()
{
   DrawHideBorder();

   // 210 is Y center point
   if (m_Hidden)
   {
      DrawShowBtn();
      
      MoveObject(0, m_HideBorder, m_BaseX + 13, m_BaseY + 35, CORNER_RIGHT_UPPER);
   }
   else
   {
      DrawArea();

      DrawHideBtn();
      
      DrawBuyWithTP();
      DrawBuyWithSL();
      DrawBuyLimit();
      DrawBuyStop();
      
      DrawSellWithTP();
      DrawSellWithSL();
      DrawSellLimit();
      DrawSellStop();
      
      DrawLots();
      DrawAsk();
      DrawBid();

      DrawIncreaseLotBtn();
      DrawDecreaseLotBtn();
      
      DrawBuy();
      DrawSell();
      DrawClose();
      DrawReverse();
      
      MoveObject(0, m_HideBorder, m_BaseX + 163, m_BaseY + 35, CORNER_RIGHT_UPPER);
   }
}

void TradePane::Slide(bool needShow)
{
   if (!m_UseSliding) return;
   
   #define PANE_SLIDE_SHIFTS_SIZE 24
   static const int SlideShifts[PANE_SLIDE_SHIFTS_SIZE] = {0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 56, 68, 82, 94, 105, 114, 122, 129, 135, 140, 144, 147, 149, 150};

   if (needShow)
   {
      for (int i = 0; i < PANE_SLIDE_SHIFTS_SIZE; ++i)
      {
         MoveObject(0, m_HideBorder, m_BaseX + SlideShifts[i] + 13, m_BaseY + 35, CORNER_RIGHT_UPPER);
         ChartRedraw();
         Sleep(10);
      }
   }
   else
   {
      for (int i = PANE_SLIDE_SHIFTS_SIZE - 1; i >= 0; --i)
      {
         MoveObject(0, m_HideBorder, m_BaseX + SlideShifts[i] + 13, m_BaseY + 35, CORNER_RIGHT_UPPER);
         ChartRedraw();
         Sleep(10);
      }
   }
}

void TradePane::DrawHideBorder()
{
   CreateObject(0, 0, m_HideBorder, OBJ_EDIT, "");
   ObjectSetInteger(0, m_HideBorder, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_COLOR, PaleTurquoise);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_XSIZE, 5);
   ObjectSetInteger(0, m_HideBorder, OBJPROP_YSIZE, 350);
}

void TradePane::DrawShowBtn()
{
   CreateObject(0, 0, m_ShowBtn, OBJ_BUTTON, CharToString(189));
   MoveObject(0, m_ShowBtn, m_BaseX + 35, m_BaseY + 180, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_ShowBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_XSIZE, 15);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_YSIZE, 60);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_ShowBtn, OBJPROP_COLOR, m_TextColor);
}

void TradePane::DrawHideBtn()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_HideBtn, OBJ_BUTTON, CharToString(190));
   MoveObject(0, m_HideBtn, m_BaseX + 185, m_BaseY + 180, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_HideBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_HideBtn, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_XSIZE, 15);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_YSIZE, 60);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_HideBtn, OBJPROP_COLOR, m_TextColor);
}

void TradePane::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   m_Settings.OnChartEvent(id, lparam, dparam, sparam);
   
   if (id == CHARTEVENT_OBJECT_CLICK)
   {
      if (sparam == m_ShowBtn)
      {
         Show(true);
         ObjectSetInteger(0, m_ShowBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_HideBtn)
      {
         Show(false);
         ObjectSetInteger(0, m_HideBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_DecreaseLotBtn)
      {
         m_LotsSize = 
            StringToDouble(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));

         m_LotsSize = CorrectLot(m_LotsSize - m_SymbolInfo.LotsStep());
         DrawLots();

         ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_IncreaseLotBtn)
      {
         m_LotsSize = 
            StringToDouble(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));

         if (m_LotsSize == 0) m_LotsSize = m_SymbolInfo.LotsMin();
         else
         {
            m_LotsSize = CorrectLot(m_LotsSize + m_SymbolInfo.LotsStep());
         }
         DrawLots();

         ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_STATE, false);
      }
      else if (sparam == m_BuyBtn || sparam == m_BuyBtn_)
      {
         OnBuy();
      }
      else if (sparam == m_SellBtn || sparam == m_SellBtn_)
      {
         OnSell();
      }
      else if (sparam == m_CloseBtn || sparam == m_CloseBtn_)
      {
         OnClose();
      }
      else if (sparam == m_ReverseBtn || sparam == m_ReverseBtn_)
      {
         OnReverse();
      }
   }

   if (id == CHARTEVENT_OBJECT_ENDEDIT)
   {
      if (sparam == m_LotsEdit)
      {
         m_LotsSize = CorrectLot
            (
               StringToDouble(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT))
            );
         DrawLots();
      }
   }
   
   if (id == CHARTEVENT_CUSTOM + EVENT_SETTING_CHANGED)
   {
      OnSettingChanged(int(lparam), sparam);
   }

   if (id == CHARTEVENT_OBJECT_DRAG)
   {
      if (sparam == m_BuyWithSL)
      {
         OnBuySL();
      }
      else if (sparam == m_BuyWithTP)
      {
         OnBuyTP();
      }
      else if (sparam == m_BuyLimit)
      {
         OnBuyLimit();
      }
      else if (sparam == m_BuyStop)
      {
         OnBuyStop();
      }
      else if (sparam == m_SellWithSL)
      {
         OnSellSL();
      }
      else if (sparam == m_SellWithTP)
      {
         OnSellTP();
      }
      else if (sparam == m_SellLimit)
      {
         OnSellLimit();
      }
      else if (sparam == m_SellStop)
      {
         OnSellStop();
      }
   }
}

TradePane::~TradePane(void)
{
   ObjectDelete(0, m_HideBorder);

   ObjectDelete(0, m_BuyBtn);
   ObjectDelete(0, m_SellBtn);
   ObjectDelete(0, m_ShowBtn);
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_BuyWithTP);
   ObjectDelete(0, m_BuyWithSL);
   ObjectDelete(0, m_BuyStop);
   ObjectDelete(0, m_BuyLimit);
   ObjectDelete(0, m_SellWithTP);
   ObjectDelete(0, m_SellWithSL);
   ObjectDelete(0, m_SellStop);
   ObjectDelete(0, m_SellLimit);
   
   ObjectDelete(0, m_IncreaseLotBtn);
   ObjectDelete(0, m_DecreaseLotBtn);
   ObjectDelete(0, m_LotsEdit);
   
   ObjectDelete(0, m_PreAsk);
   ObjectDelete(0, m_LargeAsk);
   ObjectDelete(0, m_PostAsk);
   ObjectDelete(0, m_PreBid);
   ObjectDelete(0, m_LargeBid);
   ObjectDelete(0, m_PostBid);
   
   ObjectDelete(0, m_CloseBtn);
   ObjectDelete(0, m_ReverseBtn);
   
   ObjectDelete(0, m_PaneArea);

   ObjectDelete(0, m_BuyBtn_);
   ObjectDelete(0, m_SellBtn_);
   ObjectDelete(0, m_CloseBtn_);
   ObjectDelete(0, m_ReverseBtn_);
}

void TradePane::DrawBuyWithTP()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyWithTP, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyWithTP, m_BaseX + 130, m_BaseY + 50, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyWithTP, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyWithTP, OBJPROP_BMPFILE, 0, "BuyTP" + light + ".bmp");
   ObjectSetString(0, m_BuyWithTP, OBJPROP_BMPFILE, 1, "BuyTP" + light + ".bmp");
}

void TradePane::DrawBuyWithSL()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyWithSL, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyWithSL, m_BaseX + 70, m_BaseY + 285, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyWithSL, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyWithSL, OBJPROP_BMPFILE, 0, "BuySL" + light + ".bmp");
   ObjectSetString(0, m_BuyWithSL, OBJPROP_BMPFILE, 1, "BuySL" + light + ".bmp");
}

void TradePane::DrawBuyStop()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyStop, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyStop, m_BaseX + 70, m_BaseY + 50, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyStop, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyStop, OBJPROP_BMPFILE, 0, "BuyStop" + light + ".bmp");
   ObjectSetString(0, m_BuyStop, OBJPROP_BMPFILE, 1, "BuyStop" + light + ".bmp");
}

void TradePane::DrawBuyLimit()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyLimit, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_BuyLimit, m_BaseX + 130, m_BaseY + 285, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyLimit, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyLimit, OBJPROP_BMPFILE, 0, "BuyLimit" + light + ".bmp");
   ObjectSetString(0, m_BuyLimit, OBJPROP_BMPFILE, 1, "BuyLimit" + light + ".bmp");
}

void TradePane::DrawSellWithTP()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellWithTP, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellWithTP, m_BaseX + 130, m_BaseY + 335, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellWithTP, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellWithTP, OBJPROP_BMPFILE, 0, "SellTP" + light + ".bmp");
   ObjectSetString(0, m_SellWithTP, OBJPROP_BMPFILE, 1, "SellTP" + light + ".bmp");
}

void TradePane::DrawSellWithSL()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellWithSL, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellWithSL, m_BaseX + 70, m_BaseY + 100, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellWithSL, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellWithSL, OBJPROP_BMPFILE, 0, "SellSL" + light + ".bmp");
   ObjectSetString(0, m_SellWithSL, OBJPROP_BMPFILE, 1, "SellSL" + light + ".bmp");
}

void TradePane::DrawSellStop()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellStop, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellStop, m_BaseX + 70, m_BaseY + 335, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellStop, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellStop, OBJPROP_BMPFILE, 0, "SellStop" + light + ".bmp");
   ObjectSetString(0, m_SellStop, OBJPROP_BMPFILE, 1, "SellStop" + light + ".bmp");
}

void TradePane::DrawSellLimit()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellLimit, OBJ_BITMAP_LABEL, "", true);
   MoveObject(0, m_SellLimit, m_BaseX + 130, m_BaseY + 100, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellLimit, OBJPROP_SELECTED, true);

   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellLimit, OBJPROP_BMPFILE, 0, "SellLimit" + light + ".bmp");
   ObjectSetString(0, m_SellLimit, OBJPROP_BMPFILE, 1, "SellLimit" + light + ".bmp");
}

void TradePane::DrawIncreaseLotBtn()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_IncreaseLotBtn, OBJ_BUTTON, CharToString(129));
   MoveObject(0, m_IncreaseLotBtn, m_BaseX + 102, m_BaseY + 185, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_IncreaseLotBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_FONTSIZE, 6);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_XSIZE, 50);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_YSIZE, 10);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_IncreaseLotBtn, OBJPROP_COLOR, Black);
}

void TradePane::DrawDecreaseLotBtn()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_DecreaseLotBtn, OBJ_BUTTON, CharToString(130));
   MoveObject(0, m_DecreaseLotBtn, m_BaseX + 102, m_BaseY + 225, CORNER_RIGHT_UPPER);
   ObjectSetString(0, m_DecreaseLotBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_FONTSIZE, 6);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_XSIZE, 50);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_YSIZE, 10);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_DecreaseLotBtn, OBJPROP_COLOR, Black);
}

void TradePane::DrawLots()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_LotsEdit, OBJ_EDIT, DoubleToString(m_LotsSize, m_LotsDigits));
   MoveObject(0, m_LotsEdit, m_BaseX + 102, m_BaseY + 195, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_FONTSIZE, 15);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_XSIZE, 50);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_YSIZE, 30);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_LotsEdit, OBJPROP_BGCOLOR, m_TextColor);
}

void TradePane::DrawAsk()
{
   if (m_Hidden) return;
   
   double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
   
   string pre, large, post;
   PreparePrice(_Symbol, ask, pre, large, post);
   
   int len = StringLen(large);
   for (int i = 0; i < len; i++)
   {
      pre = pre + "       ";
   }

   CreateObject(0, 0, m_PreAsk, OBJ_LABEL, string(pre));
   MoveObject(0, m_PreAsk, m_BaseX + 50, m_BaseY + 145, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PreAsk, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PreAsk, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_PreAsk, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PreAsk, OBJPROP_COLOR, m_TextColor);
   
   double values[1];
   CopyBuffer(m_SpreadInfoHandle, 2, 0, 1, values);
   
   double value = values[0];
   color clr = m_TextColor;
   
   if (value != EMPTY_VALUE)
   {
      if (value == 1) clr = m_UpColor;
      if (value == -1) clr = m_DnColor;
   }

   CreateObject(0, 0, m_LargeAsk, OBJ_LABEL, string(large));
   MoveObject(0, m_LargeAsk, m_BaseX + 50, m_BaseY + 135, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_LargeAsk, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_LargeAsk, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_LargeAsk, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_LargeAsk, OBJPROP_COLOR, clr);

   CreateObject(0, 0, m_PostAsk, OBJ_LABEL, string(post));
   MoveObject(0, m_PostAsk, m_BaseX + 50, m_BaseY + 145, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PostAsk, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PostAsk, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetString(0, m_PostAsk, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PostAsk, OBJPROP_COLOR, m_TextColor);
}

void TradePane::DrawBid()
{
   if (m_Hidden) return;
   
   double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
   
   string pre, large, post;
   PreparePrice(_Symbol, bid, pre, large, post);
   
   int len = StringLen(large);
   for (int i = 0; i < len; i++)
   {
      pre = pre + "       ";
   }

   CreateObject(0, 0, m_PreBid, OBJ_LABEL, string(pre));
   MoveObject(0, m_PreBid, m_BaseX + 50, m_BaseY + 255, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PreBid, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PreBid, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_PreBid, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PreBid, OBJPROP_COLOR, m_TextColor);

   double values[1];
   CopyBuffer(m_SpreadInfoHandle, 3, 0, 1, values);
   
   double value = values[0];
   color clr = m_TextColor;
   
   if (value != EMPTY_VALUE)
   {
      if (value == 1) clr = m_UpColor;
      if (value == -1) clr = m_DnColor;
   }

   CreateObject(0, 0, m_LargeBid, OBJ_LABEL, string(large));
   MoveObject(0, m_LargeBid, m_BaseX + 50, m_BaseY + 225, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_LargeBid, OBJPROP_FONTSIZE, 30);
   ObjectSetInteger(0, m_LargeBid, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
   ObjectSetString(0, m_LargeBid, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_LargeBid, OBJPROP_COLOR, clr);

   CreateObject(0, 0, m_PostBid, OBJ_LABEL, string(post));
   MoveObject(0, m_PostBid, m_BaseX + 50, m_BaseY + 255, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PostBid, OBJPROP_FONTSIZE, 10);
   ObjectSetInteger(0, m_PostBid, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetString(0, m_PostBid, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_PostBid, OBJPROP_COLOR, m_TextColor);
}

void TradePane::DrawBuy()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_BuyBtn_, OBJ_EDIT, "");
   MoveObject(0, m_BuyBtn_, m_BaseX + 140, m_BaseY + 175, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_BuyBtn_, OBJPROP_YSIZE, 33);
   
   CreateObject(0, 0, m_BuyBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_BuyBtn, m_BaseX + 140, m_BaseY + 175, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_BuyBtn, OBJPROP_BMPFILE, 0, "Buy" + light + ".bmp");
   ObjectSetString(0, m_BuyBtn, OBJPROP_BMPFILE, 1, "Buy" + light + ".bmp");
}

void TradePane::DrawSell()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_SellBtn_, OBJ_EDIT, "");
   MoveObject(0, m_SellBtn_, m_BaseX + 140, m_BaseY + 215, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_SellBtn_, OBJPROP_YSIZE, 33);

   CreateObject(0, 0, m_SellBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_SellBtn, m_BaseX + 140, m_BaseY + 215, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_SellBtn, OBJPROP_BMPFILE, 0, "Sell" + light + ".bmp");
   ObjectSetString(0, m_SellBtn, OBJPROP_BMPFILE, 1, "Sell" + light + ".bmp");
}

void TradePane::DrawClose()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_CloseBtn_, OBJ_EDIT, "");
   MoveObject(0, m_CloseBtn_, m_BaseX + 47, m_BaseY + 175, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_CloseBtn_, OBJPROP_YSIZE, 33);

   CreateObject(0, 0, m_CloseBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_CloseBtn, m_BaseX + 47, m_BaseY + 175, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_CloseBtn, OBJPROP_BMPFILE, 0, "Close" + light + ".bmp");
   ObjectSetString(0, m_CloseBtn, OBJPROP_BMPFILE, 1, "Close" + light + ".bmp");
}

void TradePane::DrawReverse()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_ReverseBtn_, OBJ_EDIT, "");
   MoveObject(0, m_ReverseBtn_, m_BaseX + 47, m_BaseY + 215, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_XSIZE, 33);
   ObjectSetInteger(0, m_ReverseBtn_, OBJPROP_YSIZE, 33);

   CreateObject(0, 0, m_ReverseBtn, OBJ_BITMAP_LABEL, "");
   MoveObject(0, m_ReverseBtn, m_BaseX + 47, m_BaseY + 215, CORNER_RIGHT_UPPER);
   
   string light = (m_TextColor == Black) ? "Dark" : "Light";
   ObjectSetString(0, m_ReverseBtn, OBJPROP_BMPFILE, 0, "Reverse" + light + ".bmp");
   ObjectSetString(0, m_ReverseBtn, OBJPROP_BMPFILE, 1, "Reverse" + light + ".bmp");
}

void TradePane::DrawArea()
{
   if (m_Hidden) return;
   
   CreateObject(0, 0, m_PaneArea, OBJ_EDIT, "");
   MoveObject(0, m_PaneArea, m_BaseX + 150, m_BaseY + 35, CORNER_RIGHT_UPPER);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_XSIZE, 140);
   ObjectSetInteger(0, m_PaneArea, OBJPROP_YSIZE, 350);
}

void TradePane::OnSettingChanged(int id, string value)
{
   switch (id)
   {
      case SETTING_USE_SLIDING_ID:
      {
         BoolTable table;
         m_UseSliding = table.GetValueByName(value);
         m_Settings.UseSliding(m_UseSliding);
      }
      break;
      
      case SETTING_COLOR_SCHEME_ID:
      {
         if (value == "1")
         {
            m_BordersColor = LightGray;
            m_TextColor = Black;
         }
         else if (value == "2")
         {
            m_BordersColor = Black;
            m_TextColor = LightGray;
         }
         
         Draw();

         m_Settings.SetColors(m_BordersColor, m_TextColor);
         m_Settings.DrawBar();
      }
      break;
      
      case SETTING_COMMENT_ID:
      {
         Comment_("Orders comment changed to: " + value);
         m_Comment = value;
      }
      break;
      
      case SETTING_TRADE_SLIPPAGE:
      {
         Comment_("Deviation changed to: " + value);
         m_Trade.SetSlippage(int(value));
      }
      break;

      case SETTING_TRADE_FILLING:
      {
         SelectionTable fillings;
         fillings.AddSelection(ORDER_FILLING_FOK, "Fill or Kill");
         fillings.AddSelection(ORDER_FILLING_IOC, "Available");
         fillings.AddSelection(ORDER_FILLING_RETURN, "Available+");
         
         ENUM_ORDER_TYPE_FILLING filling = ENUM_ORDER_TYPE_FILLING(value);
         
         Comment_("Orders filling changed to: " + fillings.GetNameByID(filling));

         m_Trade.SetFilling(filling);
      }
      break;
   }
}

void TradePane::OnTick()
{
   DrawAsk();
   DrawBid();
}

bool TradePane::CheckDropArea(string name)
{
   int x = int(ObjectGetInteger(0, name, OBJPROP_XDISTANCE));
   
   if (x < m_BaseX + 170)
   {
      Comment_("To perform the action please drop the pointer outside the Pane area");
      return false;
   }
   return true;
}


void TradePane::OnBuySL()
{
   if (!CheckDropArea(m_BuyWithSL))
   {
      DrawBuyWithSL();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyWithSL, OBJPROP_YDISTANCE));
   DrawBuyWithSL();
   ChartRedraw();
   
   Comment_("Trying to Buy with SL");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.BuySL(lot, price);
}

void TradePane::OnBuyTP()
{
   if (!CheckDropArea(m_BuyWithTP))
   {
      DrawBuyWithTP();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyWithTP, OBJPROP_YDISTANCE));
   DrawBuyWithTP();
   ChartRedraw();
   
   Comment_("Trying to Buy with TP");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.BuyTP(lot, price);
}

void TradePane::OnBuyLimit()
{
   if (!CheckDropArea(m_BuyLimit))
   {
      DrawBuyLimit();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyLimit, OBJPROP_YDISTANCE));
   DrawBuyLimit();
   ChartRedraw();

   Comment_("Trying to place Buy Limit");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.OpenBuyLimit(lot, price);
}

void TradePane::OnBuyStop()
{
   if (!CheckDropArea(m_BuyStop))
   {
      DrawBuyStop();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_BuyStop, OBJPROP_YDISTANCE));
   DrawBuyStop();
   ChartRedraw();

   Comment_("Trying to place Buy Stop");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }

   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.OpenBuyStop(lot, price);
}

void TradePane::OnSellSL()
{
   if (!CheckDropArea(m_SellWithSL))
   {
      DrawSellWithSL();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellWithSL, OBJPROP_YDISTANCE));
   DrawSellWithSL();
   ChartRedraw();
   
   Comment_("Trying to Sell with SL");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.SellSL(lot, price);
}

void TradePane::OnSellTP()
{
   if (!CheckDropArea(m_SellWithTP))
   {
      DrawSellWithTP();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellWithTP, OBJPROP_YDISTANCE));
   DrawSellWithTP();
   ChartRedraw();
   
   Comment_("Trying to Sell with TP");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.SellTP(lot, price);
}

void TradePane::OnSellLimit()
{
   if (!CheckDropArea(m_SellLimit))
   {
      DrawSellLimit();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellLimit, OBJPROP_YDISTANCE));
   DrawSellLimit();
   ChartRedraw();
   
   Comment_("Trying to place Sell Limit");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }

   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   Comment_("Sell Limit price = " + DoubleToString(price, _Digits));
   m_Trade.OpenSellLimit(lot, price);
}

void TradePane::OnSellStop()   
{
   if (!CheckDropArea(m_SellStop))
   {
      DrawSellStop();
      ChartRedraw();
      return;
   }

   int pos = int(ObjectGetInteger(0, m_SellStop, OBJPROP_YDISTANCE));
   DrawSellStop();
   ChartRedraw();
   
   Comment_("Trying to place Sell Stop");
   
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }

   double price = NormalizeDouble(PriceOnDropped(pos), _Digits);
   m_Trade.OpenSellStop(lot, price);
}

void TradePane::OnBuy()
{
   Comment_("Trying to Buy");
   ChartRedraw();
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }
   m_Trade.Buy(lot);
}

void TradePane::OnSell()
{
   Comment_("Trying to Sell");
   ChartRedraw();
   double lot = double(ObjectGetString(0, m_LotsEdit, OBJPROP_TEXT));
   
   if (lot == 0)
   {
      Comment_("Please set the lot size");
      return;
   }
   m_Trade.Sell(lot);
}

void TradePane::OnClose()
{
   Comment_("Trying to Close position");
   ChartRedraw();

   m_Trade.Close();
}

void TradePane::OnReverse()
{
   Comment_("Trying to Reverse position");
   ChartRedraw();

   m_Trade.Reverse();
}