//+------------------------------------------------------------------+
//|                                                 PaneSettings.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "CommonUID.mqh"

#include "IntSetting.mqh"
#include "DoubleSetting.mqh"
#include "StringSetting.mqh"
#include "SelectionSetting.mqh"
#include "BoolSetting.mqh"

#define SETTING_USE_SLIDING_ID     1
#define SETTING_COLOR_SCHEME_ID    2
#define SETTING_COMMENT_ID         3

#define SETTING_LOG_FONT_SIZE      4
#define SETTING_LOG_FONT_COLOR     5
#define SETTING_LOG_FONT_LINES     6
#define SETTING_LOG_FONT_LENGTH    7
#define SETTING_LOG_FONT_TABSIZE   8

#define SETTING_TIME               9
#define SETTING_SPREAD_BARS        10

#define SETTING_TRADE_SLIPPAGE     11
#define SETTING_TRADE_FILLING      12

class PaneSettings
{
public:
   PaneSettings();
   ~PaneSettings();
   
   void AddIntSetting(string settingName, int settingID, int value, int step = 1, int min = INT_MIN, int max = INT_MAX);
   void AddDoubleSetting(string settingName, int settingID, double value, double step = 1, double min = -DBL_MAX, double max = DBL_MAX);
   void AddStringSetting(string settingName, int settingID, string value);
   void AddSelectionSetting(string settingName, int settingID, int valueID, const SelectionTable& ids);
   void AddBoolSetting(string settingName, int settingID, bool value);
   
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   
   void Show(bool needShow);
   void SetColors(color clr, color bgClr);
   void UseSliding(bool use);
   
   void DrawBar();

private:
   void Hide();
   void DrawNextButton();
   void DrawPrevButton();
   void DrawBarArea();
   void DrawCurrentSetting();
   void HideCurrentSetting();
   void Slide(bool needShow);

private:
   DrawSetting* m_Settings[];
   
   string m_NextButton;
   string m_PrevButton;
   
   string m_Border;
   string m_ShowBtn;
   string m_HideBtn;
   
   string m_Area;

   int m_UID;
   
   int m_CurrentIndex;
   
   int m_Hidden;
   
   bool m_UseSliding;
   
   color m_BordersColor;
   color m_BGColor;
   
   int m_BaseX;
   int m_BaseY;
};

PaneSettings::PaneSettings(void)
{
   m_UID = GetUID();
   m_NextButton = "NextSetting" + string(m_UID);
   m_PrevButton = "PrevSetting" + string(m_UID);
   
   m_Border = "Border" + string(m_UID);
   m_ShowBtn = "ShowBtn" + string(m_UID);
   m_HideBtn = "HideBtn" + string(m_UID);
   
   m_Area = "Area" + string(m_UID);
   
   m_CurrentIndex = 0;

   m_UseSliding = true;
   m_Hidden = true;
   
   m_BordersColor = LightGray;
   m_BGColor = Black;
   
   m_BaseX = 0;
   m_BaseY = 0;
}

void PaneSettings::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   if (id == CHARTEVENT_OBJECT_DELETE)
   {
      if (sparam == m_NextButton && !m_Hidden)
      {
         DrawNextButton();
      }
      else if (sparam == m_PrevButton && !m_Hidden)
      {
         DrawPrevButton();
      }
      else if (
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
      if (sparam == m_NextButton)
      {
         HideCurrentSetting();
         
         m_CurrentIndex++;
         if (m_CurrentIndex >= ArraySize(m_Settings))
         {
            m_CurrentIndex = 0;
         }
         DrawBar();
      }
      else if (sparam == m_PrevButton)
      {
         HideCurrentSetting();

         m_CurrentIndex--;
         if (m_CurrentIndex < 0)
         {
            m_CurrentIndex = int(MathMax(0, ArraySize(m_Settings) - 1));
         }
         DrawBar();
      }
      else if (sparam == m_ShowBtn)
      {
         Show(true);
      }
      else if (sparam == m_HideBtn)
      {
         Show(false);
      }
   }
   
   int size = ArraySize(m_Settings);
   if (size > 0)
   {
      if (m_Settings[m_CurrentIndex] != NULL)
      {
         m_Settings[m_CurrentIndex].OnChartEvent(id, lparam, dparam, sparam);
      }
   }
}

PaneSettings::DrawBar()
{
   DrawBarArea();

   if (!m_Hidden)
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 79, CORNER_LEFT_UPPER);

      DrawNextButton();
      DrawPrevButton();
      DrawCurrentSetting();      
   }
   else
   {
      MoveObject(0, m_Border, m_BaseX + 3, m_BaseY + 29, CORNER_LEFT_UPPER);

      Hide();   
   }
}

void PaneSettings::Hide()
{
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_NextButton);
   ObjectDelete(0, m_PrevButton);
   ObjectDelete(0, m_Area);

   HideCurrentSetting();
}

PaneSettings::~PaneSettings()
{
   ObjectDelete(0, m_NextButton);
   ObjectDelete(0, m_PrevButton);
   ObjectDelete(0, m_Border);
   ObjectDelete(0, m_ShowBtn);
   ObjectDelete(0, m_HideBtn);
   ObjectDelete(0, m_Area);
   
   int size = ArraySize(m_Settings);
   for (int i = 0; i < size; i++)
   {
      if (m_Settings[i] != NULL)
      {
         delete m_Settings[i];
      }
   }
}

void PaneSettings::DrawNextButton()
{
   CreateObject(0, 0, m_NextButton, OBJ_BUTTON, CharToString(135));
   MoveObject(0, m_NextButton, m_BaseX + 5, m_BaseY + 20, CORNER_LEFT_UPPER);
   ObjectSetString(0, m_NextButton, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_NextButton, OBJPROP_XSIZE, 20);
   ObjectSetInteger(0, m_NextButton, OBJPROP_YSIZE, 20);
   ObjectSetInteger(0, m_NextButton, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_NextButton, OBJPROP_COLOR, Black);
   ObjectSetInteger(0, m_NextButton, OBJPROP_BGCOLOR, PaleTurquoise);
   
}

void PaneSettings::DrawPrevButton()
{
   CreateObject(0, 0, m_PrevButton, OBJ_BUTTON, CharToString(136));
   MoveObject(0, m_PrevButton, m_BaseX + 5, m_BaseY + 50, CORNER_LEFT_UPPER);
   ObjectSetString(0, m_PrevButton, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(0, m_PrevButton, OBJPROP_XSIZE, 20);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_YSIZE, 20);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_STATE, false);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_COLOR, Black);
   ObjectSetInteger(0, m_PrevButton, OBJPROP_BGCOLOR, PaleTurquoise);
}

void PaneSettings::DrawBarArea()
{
   CreateObject(0, 0, m_Border, OBJ_EDIT, "");
   ObjectSetInteger(0, m_Border, OBJPROP_BGCOLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_COLOR, PaleTurquoise);
   ObjectSetInteger(0, m_Border, OBJPROP_READONLY, true);
   ObjectSetInteger(0, m_Border, OBJPROP_XSIZE, 297);
   ObjectSetInteger(0, m_Border, OBJPROP_YSIZE, 5);
   
   if (m_Hidden)
   {
      CreateObject(0, 0, m_ShowBtn, OBJ_BUTTON, CharToString(192));
      MoveObject(0, m_ShowBtn, m_BaseX + 120, m_BaseY + 39, CORNER_LEFT_UPPER);
      ObjectSetString(0, m_ShowBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_COLOR, m_BGColor);
      ObjectSetInteger(0, m_ShowBtn, OBJPROP_BGCOLOR, m_BordersColor);
   }
   else
   {
      CreateObject(0, 0, m_HideBtn, OBJ_BUTTON, CharToString(191));
      MoveObject(0, m_HideBtn, m_BaseX + 120, m_BaseY + 89, CORNER_LEFT_UPPER);
      ObjectSetString(0, m_HideBtn, OBJPROP_FONT, "Wingdings 3");
      ObjectSetInteger(0, m_HideBtn, OBJPROP_FONTSIZE, 20);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_XSIZE, 60);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_YSIZE, 15);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_COLOR, m_BGColor);
      ObjectSetInteger(0, m_HideBtn, OBJPROP_BGCOLOR, m_BordersColor);

      CreateObject(0, 0, m_Area, OBJ_EDIT, "");
      MoveObject(0, m_Area, m_BaseX + 4, m_BaseY + 15, CORNER_LEFT_UPPER);
      ObjectSetInteger(0, m_Area, OBJPROP_XSIZE, 295);
      ObjectSetInteger(0, m_Area, OBJPROP_YSIZE, 59);
      ObjectSetInteger(0, m_Area, OBJPROP_BGCOLOR, m_BordersColor);
      ObjectSetInteger(0, m_Area, OBJPROP_READONLY, true);
   }
}

void PaneSettings::AddIntSetting(string settingName, int settingID, int value, int step = 1, int min = INT_MIN, int max = INT_MAX)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   IntSetting* newSetting = new IntSetting;
   newSetting.Init(settingName, settingID, GetUID(), value, step, min, max);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;
   
   if (size == 0) DrawBar();
}

void PaneSettings::AddDoubleSetting(string settingName, int settingID, double value, double step = 1, double min = -DBL_MAX, double max = DBL_MAX)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   DoubleSetting* newSetting = new DoubleSetting;
   newSetting.Init(settingName, settingID, GetUID(), value, step, min, max);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::AddStringSetting(string settingName, int settingID, string value)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   StringSetting* newSetting = new StringSetting;
   newSetting.Init(settingName, settingID, GetUID(), value);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::AddSelectionSetting(string settingName, int settingID, int valueID, const SelectionTable& ids)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   SelectionSetting* newSetting = new SelectionSetting;
   newSetting.Init(settingName, settingID, GetUID(), valueID, ids);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::AddBoolSetting(string settingName, int settingID, bool value)
{
   int size = ArraySize(m_Settings);
   ArrayResize(m_Settings, size + 1);
   
   BoolSetting* newSetting = new BoolSetting;
   newSetting.Init(settingName, settingID, GetUID(), value);
   newSetting.SetColors(m_BordersColor, m_BGColor);
   
   m_Settings[size] = newSetting;

   if (size == 0) DrawBar();
}

void PaneSettings::DrawCurrentSetting(void)
{
   int size = ArraySize(m_Settings);
   if (size > 0)
   {
      if (m_Settings[m_CurrentIndex] != NULL)
      {
         m_Settings[m_CurrentIndex].Draw(30, 15, 0, 0);
      }
   }
}

void PaneSettings::HideCurrentSetting(void)
{
   int size = ArraySize(m_Settings);
   if (size > 0)
   {
      if (m_Settings[m_CurrentIndex] != NULL)
      {
         m_Settings[m_CurrentIndex].Hide();
      }
   }
}

void PaneSettings::Show(bool needShow)
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
   DrawBar();
}

void PaneSettings::Slide(bool needShow)
{
   if (!m_UseSliding) return;
   
   #define SETTINGS_SLIDE_SHIFTS_SIZE 14
   static const int SlideShifts[SETTINGS_SLIDE_SHIFTS_SIZE] = {0, 1, 3, 6, 10, 15, 21, 29, 35, 40, 44, 47, 49, 50};

   if (needShow)
   {
      for (int i = 0; i < SETTINGS_SLIDE_SHIFTS_SIZE; ++i)
      {
         MoveObject(0, m_Border, m_BaseX + 3, SlideShifts[i] + 29 + m_BaseY, CORNER_LEFT_UPPER);
         ChartRedraw();
         Sleep(8);
      }
   }
   else
   {
      for (int i = SETTINGS_SLIDE_SHIFTS_SIZE - 1; i >= 0; --i)
      {
         MoveObject(0, m_Border, m_BaseX + 3, SlideShifts[i] + 29 + m_BaseY, CORNER_LEFT_UPPER);
         ChartRedraw();
         Sleep(8);
      }
   }
}

void PaneSettings::SetColors(color clr, color bgClr)
{
   m_BordersColor = clr;
   m_BGColor = bgClr;
   
   int size = ArraySize(m_Settings);
   for (int i = 0; i < size; i++)
   {
      m_Settings[i].SetColors(m_BGColor, m_BordersColor);
   }
}

void PaneSettings::UseSliding(bool use)
{
   m_UseSliding = use;
}
