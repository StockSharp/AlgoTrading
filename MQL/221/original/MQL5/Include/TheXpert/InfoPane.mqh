//+------------------------------------------------------------------+
//|                                                     InfoPane.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "CommonUID.mqh"
#include "Objects.mqh"
#include "Utils.mqh"
#include "Time.mqh"
#include "News.mqh"
#include "PaneSettings.mqh"

class InfoPane
{
public:
   InfoPane();
   ~InfoPane();
   
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   void OnTick();
   void OnTimer();
   
   void Show(bool needShow);
   void UseSliding(bool use);
   
private:
   void Draw();
   void Hide();
   void DrawBarArea();
   void DrawSpreadBar();
   void DrawDaily();
   void DrawOpen();
   void DrawSwap();
   void DrawTime();
   void DrawLevels();
   void DrawNews();
   
   void Slide(bool needShow);
   
   void OnSettingChanged(int id, string value);

private:
   string m_Border;
   string m_ShowBtn;
   string m_HideBtn;
   string m_Area;
   
   string m_SpreadUpBorder;
   string m_SpreadDnBorder;
   string m_SpreadArea;
   string m_SpreadProgress;
   string m_SpreadMax;
   string m_SpreadMin;
   string m_SpreadActual;
   
   string m_DailyOpen;
   string m_DailyDirection;
   string m_DailyPercent;
   
   string m_OpenLot;
   string m_OpenDirection;
   string m_OpenEquity;
   
   string m_TimeDay;
   string m_TimeClock;
   
   string m_Levels;
   
   string m_NewsName;
   string m_NewsTime;
   
   string m_Swap;
   string m_CopyRight;

   int m_UID;
   
   int m_Hidden;
   
   bool m_UseSliding;
   
   color m_BordersColor;
   color m_TextColor;
   color m_BuyColor;
   color m_SellColor;
   color m_UpColor;
   color m_DnColor;
   
   int m_BaseX;
   int m_BaseY;
   
   int m_SpreadInfoHandle;
   int m_SpreadInfoPeriod;
   
   int m_LotsDigits;
   
   ETimeKind m_TimeSettings;
};

InfoPane::InfoPane(void)
{
   m_UID = GetUID();
   string sUID = string(m_UID);
   
   m_Border = "Border" + sUID;
   m_ShowBtn = "ShowBtn" + sUID;
   m_HideBtn = "HideBtn" + sUID;
   m_Area = "Area" + sUID;
   
   m_SpreadUpBorder = "SpreadUpBorder" + sUID;
   m_SpreadDnBorder = "SpreadDnBorder" + sUID;
   m_SpreadArea = "SpreadArea" + sUID;
   m_SpreadProgress = "SpreadProgress" + sUID;
   m_SpreadMax = "SpreadMax" + sUID;
   m_SpreadMin = "SpreadMin" + sUID;
   m_SpreadActual = "SpreadActual" + sUID;
   
   m_DailyOpen = "DailyOpen" + sUID;
   m_DailyDirection = "DailyDirection" + sUID;
   m_DailyPercent = "DailyPercent" + sUID;
   
   m_OpenLot = "OpenLot" + sUID;
   m_OpenDirection = "OpenDirection" + sUID;
   m_OpenEquity = "OpenEquity" + sUID;
   
   m_TimeDay = "TimeDay" + sUID;
   m_TimeClock = "TimeClock" + sUID;
   m_Levels = "Levels" + sUID;
   
   m_Swap = "Swap" + sUID;
   
   m_NewsName = "NewsName" + sUID;
   m_NewsTime = "NewsTime" + sUID;
   
   m_CopyRight = "CopyRight" + sUID;
   
   m_UseSliding = true;
   m_Hidden = true;
   
   m_BordersColor = Black;
   m_TextColor = LightGray;
   m_BuyColor = DodgerBlue;
   m_SellColor = Tomato;
   m_UpColor = SeaGreen;
   m_DnColor = Tomato;
   
   m_BaseX = 0;
   m_BaseY = 0;
   
   m_SpreadInfoHandle = iCustom(_Symbol, PERIOD_CURRENT, "SpreadInfo");
   m_SpreadInfoPeriod = 5;
   
   m_LotsDigits = DoubleDigits(SymbolInfoDouble(_Symbol, SYMBOL_VOLUME_STEP));
   m_TimeSettings = ETIME_SERVER;
}

void InfoPane::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   if (id == CHARTEVENT_OBJECT_DELETE)
   {
      if (!m_Hidden)
      {
         if (
               sparam == m_Border ||
               sparam == m_HideBtn ||
               sparam == m_Area ||

               sparam == m_SpreadUpBorder ||
               sparam == m_SpreadDnBorder ||
               sparam == m_SpreadArea ||
               sparam == m_SpreadProgress ||
               sparam == m_SpreadMax ||
               sparam == m_SpreadMin ||
               sparam == m_SpreadActual ||

               sparam == m_DailyOpen ||
               sparam == m_DailyDirection ||
               sparam == m_DailyPercent ||

               sparam == m_OpenLot ||
               sparam == m_OpenDirection ||
               sparam == m_OpenEquity ||

               sparam == m_TimeDay ||
               sparam == m_TimeClock ||

               sparam == m_Levels ||

               sparam == m_NewsName ||
               sparam == m_NewsTime ||
               sparam == m_Swap
            )
         {
            Draw();
         }       
      }
      
      if (
                  (sparam == m_Border) ||
                  (sparam == m_ShowBtn && m_Hidden) ||
                  (sparam == m_HideBtn && !m_Hidden)
              )
      {
         DrawBarArea();
      }
   }

   if (id == CHARTEVENT_OBJECT_CLICK)
   {
      if (sparam == m_ShowBtn)
      {
         Show(true);
      }
      else if (sparam == m_HideBtn)
      {
         Show(false);
      }
   }
   
   if (id == CHARTEVENT_CUSTOM + EVENT_SETTING_CHANGED)
   {
      OnSettingChanged(int(lparam), sparam);
   }

}

InfoPane::Draw()
{
   DrawBarArea();
   DrawSpreadBar();
   DrawDaily();
   DrawOpen();
   DrawSwap();
   DrawTime();
   DrawLevels();
   DrawNews();

   if (!m_Hidden)
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 161, CORNER_LEFT_LOWER);
   }
   else
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY +  11, CORNER_LEFT_LOWER);
      Hide();   
   }
}

void InfoPane::Hide()
{
   ObjectDelete(0, m_HideBtn);

   ObjectDelete(0, m_SpreadUpBorder);
   ObjectDelete(0, m_SpreadDnBorder);
   ObjectDelete(0, m_SpreadArea);
   ObjectDelete(0, m_SpreadProgress);
   ObjectDelete(0, m_SpreadMax);
   ObjectDelete(0, m_SpreadMin);
   ObjectDelete(0, m_SpreadActual);

   ObjectDelete(0, m_DailyOpen);
   ObjectDelete(0, m_DailyDirection);
   ObjectDelete(0, m_DailyPercent);
   
   ObjectDelete(0, m_OpenLot);
   ObjectDelete(0, m_OpenDirection);
   ObjectDelete(0, m_OpenEquity);
   
   ObjectDelete(0, m_Swap);
   ObjectDelete(0, m_TimeDay);
   ObjectDelete(0, m_TimeClock);
   ObjectDelete(0, m_Levels);
   
   ObjectDelete(0, m_NewsName);
   ObjectDelete(0, m_NewsTime);
   ObjectDelete(0, m_Area);

   ObjectDelete(0, m_CopyRight);
}

InfoPane::~InfoPane()
{
   Hide();
   ObjectDelete(0, m_Border);
   ObjectDelete(0, m_ShowBtn);
}

void InfoPane::DrawBarArea()
{
   CreateObject(0, 0, m_Border, OBJ_EDIT, "");
   ObjectSetInteger(0, m_Border, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_COLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_Border, OBJPROP_XSIZE, 297);
   ObjectSetInteger(0, m_Border, OBJPROP_YSIZE, 5);
   
   if (m_Hidden)
   {
      CreateObject(0, 0, m_ShowBtn, OBJ_BUTTON, CharToString(191));
      MoveObject(0, m_ShowBtn, m_BaseX + 120, m_BaseY + 32, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_ShowBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_BGCOLOR, m_BordersColor);
   }
   else
   {
      CreateObject(0, 0, m_HideBtn, OBJ_BUTTON, CharToString(192));
      MoveObject(0, m_HideBtn, m_BaseX + 120, m_BaseY + 182, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_HideBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_HideBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_BGCOLOR, m_BordersColor);
      
      CreateObject(0, 0, m_Area, OBJ_EDIT, "");
      MoveObject(0, m_Area, m_BaseX + 4, m_BaseY + 150, CORNER_LEFT_LOWER);
      ObjectSetInteger(0, m_Area, OBJPROP_XSIZE, 295);
      ObjectSetInteger(0, m_Area, OBJPROP_YSIZE, 145);
      ObjectSetInteger(0, m_Area, OBJPROP_BGCOLOR, m_BordersColor);
      ObjectSetInteger(0, m_Area, OBJPROP_READONLY, true);
      
      CreateObject(0, 0, m_CopyRight, OBJ_LABEL, "Copyright (c) 2010, TheXpert");
      MoveObject(0, m_CopyRight, m_BaseX + 60, m_BaseY + 135, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_CopyRight, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_CopyRight, OBJPROP_FONTSIZE, 8);
      ObjectSetInteger(0, m_CopyRight, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_CopyRight, OBJPROP_ANCHOR, ANCHOR_LEFT_LOWER);
   }
}

void InfoPane::Show(bool needShow)
{
   if (needShow)
   {
      ObjectDelete(0, m_ShowBtn);
      
      if (m_Hidden) Slide(true);
      m_Hidden = false;
   }
   else
   {
      Hide();
      
      if (!m_Hidden) Slide(false);
      m_Hidden = true;
   }
   Draw();
}

void InfoPane::Slide(bool needShow)
{
   if (!m_UseSliding) return;
   
   #define INFO_SLIDE_SHIFTS_SIZE 24
   static const int SlideShifts[INFO_SLIDE_SHIFTS_SIZE] = {0, 1, 3, 6, 10, 15, 21, 28, 36, 45, 56, 68, 82, 94, 105, 114, 122, 129, 135, 140, 144, 147, 149, 150};

   if (!needShow)
   {
      for (int i = 0; i < INFO_SLIDE_SHIFTS_SIZE; ++i)
      {
         MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 161 - SlideShifts[i], CORNER_LEFT_LOWER);
         ChartRedraw();
         Sleep(8);
      }
   }
   else
   {
      for (int i = INFO_SLIDE_SHIFTS_SIZE - 1; i >= 0; --i)
      {
         MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 161 - SlideShifts[i], CORNER_LEFT_LOWER);
         ChartRedraw();
         Sleep(8);
      }
   }
}

void InfoPane::UseSliding(bool use)
{
   m_UseSliding = use;
}

void InfoPane::OnTick()
{
   DrawSpreadBar();
   DrawDaily();
   DrawOpen();
   DrawLevels();
   DrawNews();
}

void InfoPane::DrawSpreadBar()
{
      if (m_Hidden) return;
      
      double maxs[];
      ArrayResize(maxs, m_SpreadInfoPeriod);
      CopyBuffer(m_SpreadInfoHandle, 0, 0, m_SpreadInfoPeriod, maxs);
      int max = int(maxs[ArrayMaximum(maxs)]);
      
      double mins[];
      ArrayResize(mins, m_SpreadInfoPeriod);
      CopyBuffer(m_SpreadInfoHandle, 1, 0, m_SpreadInfoPeriod, mins);
      int min = int(mins[ArrayMinimum(mins)]);
      
      double ask = SymbolInfoDouble(_Symbol, SYMBOL_ASK);
      double bid = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      
      int current = int(MathRound((ask - bid)/_Point));
      
      int ratio = 50;
      if (max != min)
      {
         ratio = int(MathRound(100.0*(current - min)/(max - min)));
      }
      
      CreateObject(0, 0, m_SpreadUpBorder, OBJ_LABEL, CharToString(239));
      MoveObject(0, m_SpreadUpBorder, m_BaseX + 229, m_BaseY + 131, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadUpBorder, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_SpreadUpBorder, OBJPROP_FONTSIZE, 40);
      ObjectSetInteger(0, m_SpreadUpBorder, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadUpBorder, OBJPROP_ANCHOR, ANCHOR_LEFT_LOWER);

      CreateObject(0, 0, m_SpreadDnBorder, OBJ_LABEL, CharToString(240));
      MoveObject(0, m_SpreadDnBorder, m_BaseX + 229, m_BaseY + 32, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadDnBorder, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_SpreadDnBorder, OBJPROP_FONTSIZE, 40);
      ObjectSetInteger(0, m_SpreadDnBorder, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadDnBorder, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);

      CreateObject(0, 0, m_SpreadArea, OBJ_EDIT, "");
      MoveObject(0, m_SpreadArea, m_BaseX + 230, m_BaseY + 129, CORNER_LEFT_LOWER);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_READONLY, true);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_XSIZE, 30);
      ObjectSetInteger(0, m_SpreadArea, OBJPROP_YSIZE, 102);

      CreateObject(0, 0, m_SpreadProgress, OBJ_EDIT, "");
      MoveObject(0, m_SpreadProgress, m_BaseX + 231, m_BaseY + 28 + ratio, CORNER_LEFT_LOWER);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_COLOR, m_UpColor);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_BGCOLOR, m_UpColor);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_READONLY, true);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_XSIZE, 28);
      ObjectSetInteger(0, m_SpreadProgress, OBJPROP_YSIZE, ratio);

      CreateObject(0, 0, m_SpreadMax, OBJ_LABEL, string(max));
      MoveObject(0, m_SpreadMax, m_BaseX + 260, m_BaseY + 131, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadMax, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_SpreadMax, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadMax, OBJPROP_ANCHOR, ANCHOR_LEFT_LOWER);
      ObjectSetInteger(0, m_SpreadMax, OBJPROP_FONTSIZE, 10);

      CreateObject(0, 0, m_SpreadMin, OBJ_LABEL, string(min));
      MoveObject(0, m_SpreadMin, m_BaseX + 260, m_BaseY + 26, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadMin, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_SpreadMin, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_SpreadMin, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_SpreadMin, OBJPROP_FONTSIZE, 10);

      CreateObject(0, 0, m_SpreadActual, OBJ_LABEL, string(current));
      MoveObject(0, m_SpreadActual, m_BaseX + 260, m_BaseY + 26 + int(MathMax(ratio, 15)), CORNER_LEFT_LOWER);
      ObjectSetString(0, m_SpreadActual, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_SpreadActual, OBJPROP_COLOR, m_UpColor);
      ObjectSetInteger(0, m_SpreadActual, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_SpreadActual, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawDaily()
{
      if (m_Hidden) return;
      
      double data[1];
      CopyOpen(_Symbol, PERIOD_D1, 0, 1, data);
      double open = data[0];
      double actual = SymbolInfoDouble(_Symbol, SYMBOL_BID);
      double percent = 100.0*(actual - open)/actual;
      
      CreateObject(0, 0, m_DailyOpen, OBJ_LABEL, "Day Open: " + DoubleToString(open, _Digits));
      MoveObject(0, m_DailyOpen, m_BaseX + 150, m_BaseY + 115, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_DailyOpen, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_DailyOpen, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_DailyOpen, OBJPROP_ANCHOR, ANCHOR_RIGHT_UPPER);
      ObjectSetInteger(0, m_DailyOpen, OBJPROP_FONTSIZE, 10);

      char arrowID = (percent < 0) ? 113 : 112;
      color clr = (percent < 0) ? m_DnColor : m_UpColor;
      
      CreateObject(0, 0, m_DailyDirection, OBJ_LABEL, CharToString(arrowID));
      MoveObject(0, m_DailyDirection, m_BaseX + 150, m_BaseY + 115, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_DailyDirection, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_DailyDirection, OBJPROP_COLOR, clr);
      ObjectSetInteger(0, m_DailyDirection, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_DailyDirection, OBJPROP_FONTSIZE, 10);
      
      string strPercent = (percent < 0) ? " - " : " + ";
      strPercent = strPercent + DoubleToString(MathAbs(percent), 2) + "%";

      CreateObject(0, 0, m_DailyPercent, OBJ_LABEL, strPercent);
      MoveObject(0, m_DailyPercent, m_BaseX + 160, m_BaseY + 115, CORNER_LEFT_LOWER);
      ObjectSetString(0, m_DailyPercent, OBJPROP_FONT, "Arial Black");
      ObjectSetInteger(0, m_DailyPercent, OBJPROP_COLOR, m_TextColor);
      ObjectSetInteger(0, m_DailyPercent, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
      ObjectSetInteger(0, m_DailyPercent, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawOpen()
{
   if (m_Hidden) return;
   
   double lots = 0;
   double equity = 0;

   bool available = PositionSelect(_Symbol);
   if (available)
   {
      lots = PositionGetDouble(POSITION_VOLUME);
      
      if (lots > 0)
      {
         if (PositionGetInteger(POSITION_TYPE) == POSITION_TYPE_SELL) lots = -lots;
      }
      equity = 
         PositionGetDouble(POSITION_PROFIT) + 
         PositionGetDouble(POSITION_COMMISSION) + 
         PositionGetDouble(POSITION_SWAP);
   }
   
   string strLots = DoubleToString(MathAbs(lots), m_LotsDigits);
   if (lots == 0) strLots = "None";
   
   
   CreateObject(0, 0, m_OpenLot, OBJ_LABEL, strLots);
   MoveObject(0, m_OpenLot, m_BaseX + 15, m_BaseY + 95, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_OpenLot, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_OpenLot, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_OpenLot, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_OpenLot, OBJPROP_FONTSIZE, 10);

   string pos = " ";
   color clr = m_TextColor;
   
   if (lots > 0) 
   {
      pos = " Buy";
      clr = m_BuyColor;
   }
   else if (lots < 0)
   {
      pos = " Sell";
      clr = m_SellColor;
   }
   
   CreateObject(0, 0, m_OpenDirection, OBJ_LABEL, pos);
   MoveObject(0, m_OpenDirection, m_BaseX + 70, m_BaseY + 95, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_OpenDirection, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_OpenDirection, OBJPROP_COLOR, clr);
   ObjectSetInteger(0, m_OpenDirection, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_OpenDirection, OBJPROP_FONTSIZE, 10);
   
   string strEquity = (equity < 0) ? " - " : " + ";
   strEquity = strEquity + DoubleToString(MathAbs(equity), 2);

   clr = m_TextColor;
   
   if (equity > 0) 
   {
      clr = m_UpColor;
   }
   else if (equity < 0)
   {
      clr = m_DnColor;
   }

   CreateObject(0, 0, m_OpenEquity, OBJ_LABEL, strEquity);
   MoveObject(0, m_OpenEquity, m_BaseX + 110, m_BaseY + 95, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_OpenEquity, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_OpenEquity, OBJPROP_COLOR, clr);
   ObjectSetInteger(0, m_OpenEquity, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_OpenEquity, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawSwap()
{
   if (m_Hidden) return;
   
   double buySwap = SymbolInfoDouble(_Symbol, SYMBOL_SWAP_LONG);
   double sellSwap = SymbolInfoDouble(_Symbol, SYMBOL_SWAP_SHORT);
   
   string strSwap = "Swap long/short: " + DoubleToString(buySwap, 2) + "/" + DoubleToString(sellSwap, 2);
   
   CreateObject(0, 0, m_Swap, OBJ_LABEL, strSwap);
   MoveObject(0, m_Swap, m_BaseX + 15, m_BaseY + 75, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_Swap, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_Swap, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_Swap, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_Swap, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawTime()
{
   if (m_Hidden) return;

   datetime time = GetTime(m_TimeSettings);
   
   string day = DayToString(TimeDay(time));
   
   CreateObject(0, 0, m_TimeDay, OBJ_LABEL, day);
   MoveObject(0, m_TimeDay, m_BaseX + 15, m_BaseY + 145, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_TimeDay, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_TimeDay, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_TimeDay, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_TimeDay, OBJPROP_FONTSIZE, 15);

   CreateObject(0, 0, m_TimeClock, OBJ_LABEL, TimeToString(time, TIME_SECONDS) + " (" + GetTimeBase(m_TimeSettings) + ")");
   MoveObject(0, m_TimeClock, m_BaseX + 80, m_BaseY + 135, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_TimeClock, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_TimeClock, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_TimeClock, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_TimeClock, OBJPROP_FONTSIZE, 10);
}

void InfoPane::OnTimer()
{
   DrawTime();
}

void InfoPane::DrawLevels()
{
   if (m_Hidden) return;
   
   string levels = "Stop/Freeze Level: " +
      string(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_STOPS_LEVEL)) + "/" + 
      string(SymbolInfoInteger(_Symbol, SYMBOL_TRADE_FREEZE_LEVEL));

   CreateObject(0, 0, m_Levels, OBJ_LABEL, levels);
   MoveObject(0, m_Levels, m_BaseX + 15, m_BaseY + 55, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_Levels, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_Levels, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_Levels, OBJPROP_ANCHOR, ANCHOR_LEFT_UPPER);
   ObjectSetInteger(0, m_Levels, OBJPROP_FONTSIZE, 10);
}

void InfoPane::DrawNews()
{
   if (m_Hidden) return;
   
   string news = GetNearestNews();
   
   string newsHeader = "None";
   string newsTime = " ";
   
   if (ObjectFind(0, news) >= 0)
   {
      newsHeader = StringSubstr(ObjectGetString(0, news, OBJPROP_NAME), 17);
      datetime time = datetime(ObjectGetInteger(0, news, OBJPROP_TIME, 0));
      
      newsTime = TimeToString(time, TIME_MINUTES);
   }
   
   CreateObject(0, 0, m_NewsName, OBJ_EDIT, newsHeader);
   MoveObject(0, m_NewsName, m_BaseX + 65, m_BaseY + 35, CORNER_LEFT_LOWER);
   ObjectSetInteger(0, m_NewsName, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_NewsName, OBJPROP_BGCOLOR, m_BordersColor);
   ObjectSetInteger(0, m_NewsName, OBJPROP_FONTSIZE, 8);
   ObjectSetInteger(0, m_NewsName, OBJPROP_XSIZE, 160);
   ObjectSetInteger(0, m_NewsName, OBJPROP_YSIZE, 15);

   CreateObject(0, 0, m_NewsTime, OBJ_LABEL, newsTime);
   MoveObject(0, m_NewsTime, m_BaseX + 15, m_BaseY + 35, CORNER_LEFT_LOWER);
   ObjectSetString(0, m_NewsTime, OBJPROP_FONT, "Arial Black");
   ObjectSetInteger(0, m_NewsTime, OBJPROP_COLOR, m_TextColor);
   ObjectSetInteger(0, m_NewsTime, OBJPROP_FONTSIZE, 10);
}

void InfoPane::OnSettingChanged(int id, string value)
{
   switch (id)
   {
      case SETTING_USE_SLIDING_ID:
      {
         BoolTable table;
         m_UseSliding = table.GetValueByName(value);
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
      }
      break;
      
      case SETTING_SPREAD_BARS:
      {
         m_SpreadInfoPeriod = int(value);
      }
      break;

      case SETTING_TIME:
      {
         m_TimeSettings = ETimeKind(value);
         DrawTime();
      }
      break;
   }
}
