//+------------------------------------------------------------------+
//|                                                      Setting.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "BaseSettingLogic.mqh"
#include "Objects.mqh"
#include "../VirtualKeys.mqh"

enum CallType
{
   CALL_ON_UP, // call OnBtnUp
   CALL_ON_DN, // call OnBtnDn
   CALL_ON_APPLY // call OnApply
};

class DrawSetting
   : public ISettingEvents
{
   private:
      
      void ProcessCall(CallType type);
   
   public:
      DrawSetting();
      ~DrawSetting();
      
      virtual string Value() const
      {
         Assert(false, "Pure virtual function call at line "  + string(__LINE__) + " function "  + __FUNCTION__ + " file " + __FILE__);
         return "";
      }
      
      void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
      void SetColors(color clr, color bgClr);
      void Hide();
      void Draw(int x, int y, int chartID = 0, int subWnd = 0, int corner = 0);
   
   protected:
      void Init(string settingName, int settingID, int UID, const string& value, const string& max, const string& min);

      
      string GetEditText() const;
      
      virtual void DrawMinMax();
      virtual void DrawValue();
      virtual void DrawNextPrevious();
      virtual void DrawApply();

   private:
      void CreateObjects(int chartID, int subWnd);
      
   protected:
      string m_Name;

      int m_Chart;
      int m_SubWnd;

      int m_SettingID;

      string m_ValueEdit;
      string m_Title;
      string m_MaxCaption;
      string m_MaxValue;
      string m_MinCaption;
      string m_MinValue;
      string m_ApplyBtn;
      string m_UpBtn;
      string m_DnBtn;
      
      color m_BordersColor;
      color m_BGColor;

   private:
      int m_UID;
      string m_ObjID;
      
      string m_Value;
      string m_Max;
      string m_Min;
      
      int m_X;
      int m_Y;
      int m_Corner;
      
      bool m_Inited;
      bool m_Hidden;
};

DrawSetting::DrawSetting(void)
{
   m_Inited = false;
   m_Hidden = false;
}

DrawSetting::~DrawSetting(void)
{
   Hide();
}

DrawSetting::Init(string settingName, int settingID, int UID, const string& value, const string& max, const string& min)
{
   m_UID = UID;

   m_Name = settingName;
   m_SettingID = settingID;
   m_Value = value;
   m_Max = max;
   m_Min = min;

   m_ObjID = "Setting" + string(m_UID);

   m_Title      = m_ObjID + " Title";
   m_MaxCaption = m_ObjID + " MaxCaption";
   m_MaxValue   = m_ObjID + " MaxValue";
   m_MinCaption = m_ObjID + " MinCaption";
   m_MinValue   = m_ObjID + " MinValue";
   m_ValueEdit  = m_ObjID + " ValueEdit";
   m_ApplyBtn   = m_ObjID + " ApplyBtn";
   m_UpBtn      = m_ObjID + " UpBtn";
   m_DnBtn      = m_ObjID + " DnBtn";
   
   m_Inited = true;
}

void DrawSetting::Draw(int x, int y, int chartID, int subWnd, int corner)
{
   m_Hidden = false;
   m_X = x;
   m_Y = y;
   m_Chart = chartID;
   m_SubWnd = subWnd;
   m_Corner = corner;
   
   CreateObjects(chartID, subWnd);
   
   MoveObject(chartID, m_Title,     x,       y + 20,  corner);
   MoveObject(chartID, m_MaxCaption,x,       y,       corner);
   MoveObject(chartID, m_MaxValue,  x + 120, y,       corner);
   MoveObject(chartID, m_MinCaption,x,       y + 40,  corner);
   MoveObject(chartID, m_MinValue,  x + 120, y + 40,  corner);
   MoveObject(chartID, m_ValueEdit, x + 120, y + 20,  corner);
   MoveObject(chartID, m_ApplyBtn,  x + 247, y + 22,  corner);
   MoveObject(chartID, m_UpBtn,     x + 247, y + 2,   corner);
   MoveObject(chartID, m_DnBtn,     x + 247, y + 42,  corner);
}

void DrawSetting::CreateObjects(int chartID, int subWnd)
{
   m_Chart = chartID;
   m_SubWnd = subWnd;
   
   DrawValue();
   DrawApply();
   DrawMinMax();
   DrawNextPrevious();
}

void DrawSetting::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   switch (id)
   {
      case CHARTEVENT_OBJECT_DELETE:
         {
            if (m_Hidden) return; // no need to restore object in hidden mode
            if (sparam == m_Title)
            {
               DrawValue();
               MoveObject(m_Chart, m_Title,     m_X,       m_Y + 20,  m_Corner);
            }
            else if (sparam == m_MaxCaption)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MaxCaption,m_X,       m_Y,       m_Corner);
            }
            else if (sparam == m_MaxValue)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MaxValue,  m_X + 120, m_Y,       m_Corner);
            }
            else if (sparam == m_MinCaption)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MinCaption,m_X,       m_Y + 40,  m_Corner);
            }
            else if (sparam == m_MinValue)
            {
               DrawMinMax();
               MoveObject(m_Chart, m_MinValue,  m_X + 120, m_Y + 40,  m_Corner);
            }
            else if (sparam == m_ValueEdit)
            {
               DrawValue();
               MoveObject(m_Chart, m_ValueEdit, m_X + 120, m_Y + 20,  m_Corner);
            }
            else if (sparam == m_ApplyBtn)
            {
               DrawApply();
               MoveObject(m_Chart, m_ApplyBtn,  m_X + 247, m_Y + 22,  m_Corner);
            }
            else if (sparam == m_UpBtn)
            {
               DrawNextPrevious();
               MoveObject(m_Chart, m_UpBtn,     m_X + 247, m_Y + 2,   m_Corner);
            }
            else if (sparam == m_DnBtn)
            {
               DrawNextPrevious();
               MoveObject(m_Chart, m_DnBtn,     m_X + 247, m_Y + 42,  m_Corner);
            }
         }
      break;
         
      case CHARTEVENT_KEYDOWN:
         {
            if (lparam == VK_UP) ProcessCall(CALL_ON_UP);
            else if (lparam == VK_NUMPAD8) ProcessCall(CALL_ON_UP);
            else if (lparam == VK_DOWN) ProcessCall(CALL_ON_DN);
            else if (lparam == VK_NUMPAD2) ProcessCall(CALL_ON_DN);
         }
      break;
      
      case CHARTEVENT_OBJECT_CLICK:
         {
            if (sparam == m_UpBtn)
            {
               ProcessCall(CALL_ON_UP);
               ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_STATE, false);
            }
            else if (sparam == m_DnBtn)
            {
               ProcessCall(CALL_ON_DN);
               ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_STATE, false);
            }
            else if (sparam == m_ApplyBtn)
            {
               ProcessCall(CALL_ON_APPLY);
               ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_STATE, false);
            }
         }
      break;

      case CHARTEVENT_OBJECT_ENDEDIT:
         {
            if (sparam == m_ValueEdit)
            {
               ProcessCall(CALL_ON_APPLY);
               ChartRedraw(m_Chart);
            }
         }
      break;
   }
}

void DrawSetting::Hide()
{
   m_Hidden = true;

   ObjectDelete(m_Chart, m_Title);
   ObjectDelete(m_Chart, m_MaxCaption);
   ObjectDelete(m_Chart, m_MaxValue);
   ObjectDelete(m_Chart, m_MinCaption);
   ObjectDelete(m_Chart, m_MinValue);
   ObjectDelete(m_Chart, m_ValueEdit);
   ObjectDelete(m_Chart, m_ApplyBtn);
   ObjectDelete(m_Chart, m_DnBtn);
   ObjectDelete(m_Chart, m_UpBtn);
}

string DrawSetting::GetEditText() const
{
   if (ObjectFind(m_Chart, m_ValueEdit) >= 0)
   {
      return ObjectGetString(m_Chart, m_ValueEdit, OBJPROP_TEXT);
   }
   return "";
}

void DrawSetting::ProcessCall(CallType type)
{
   string newValue;
   switch (type)
   {
      case CALL_ON_UP: OnBtnUp(newValue); break;
      case CALL_ON_DN: OnBtnDn(newValue); break;
      case CALL_ON_APPLY: OnApply(newValue); break;
   }
   
   m_Value = newValue;
   ObjectSetString(m_Chart, m_ValueEdit, OBJPROP_TEXT, m_Value);
}

void DrawSetting::DrawMinMax()
{
   CreateObject(m_Chart, m_SubWnd, m_MaxCaption, OBJ_LABEL, "Max Value");
   ObjectSetInteger(m_Chart, m_MaxCaption, OBJPROP_COLOR, m_BordersColor);
   
   CreateObject(m_Chart, m_SubWnd, m_MaxValue, OBJ_LABEL, m_Max);
   ObjectSetInteger(m_Chart, m_MaxValue, OBJPROP_COLOR, m_BordersColor);
   
   CreateObject(m_Chart, m_SubWnd, m_MinCaption, OBJ_LABEL, "Min Value");
   ObjectSetInteger(m_Chart, m_MinCaption, OBJPROP_COLOR, m_BordersColor);
   
   CreateObject(m_Chart, m_SubWnd, m_MinValue, OBJ_LABEL, m_Min);
   ObjectSetInteger(m_Chart, m_MinValue, OBJPROP_COLOR, m_BordersColor);
}

void DrawSetting::DrawValue()
{
   CreateObject(m_Chart, m_SubWnd, m_Title, OBJ_LABEL, m_Name);
   CreateObject(m_Chart, m_SubWnd, m_ValueEdit, OBJ_EDIT, m_Value);
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_XSIZE, 125);
   ObjectSetInteger(m_Chart, m_Title, OBJPROP_COLOR, m_BordersColor);
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_COLOR, m_BGColor);
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_BGCOLOR, m_BordersColor);
}

void DrawSetting::DrawNextPrevious()
{
   CreateObject(m_Chart, m_SubWnd, m_UpBtn, OBJ_BUTTON, CharToString(135));
   ObjectSetString(m_Chart, m_UpBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_XSIZE, 20);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_YSIZE, 15);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_STATE, false);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_COLOR, Black);
   ObjectSetInteger(m_Chart, m_UpBtn, OBJPROP_BGCOLOR, PaleTurquoise);
   
   CreateObject(m_Chart, m_SubWnd, m_DnBtn, OBJ_BUTTON, CharToString(136));
   ObjectSetString(m_Chart, m_DnBtn, OBJPROP_FONT, "Wingdings 3");
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_XSIZE, 20);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_YSIZE, 15);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_STATE, false);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_COLOR, Black);
   ObjectSetInteger(m_Chart, m_DnBtn, OBJPROP_BGCOLOR, PaleTurquoise);
}

void DrawSetting::DrawApply()
{
   CreateObject(m_Chart, m_SubWnd, m_ApplyBtn, OBJ_BUTTON, CharToString(195));
   ObjectSetString(m_Chart, m_ApplyBtn, OBJPROP_FONT, "Wingdings");
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_XSIZE, 20);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_YSIZE, 15);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_STATE, false);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_COLOR, Black);
   ObjectSetInteger(m_Chart, m_ApplyBtn, OBJPROP_BGCOLOR, PaleTurquoise);
}

void DrawSetting::SetColors(color clr, color bgClr)
{
   m_BordersColor = clr;
   m_BGColor = bgClr;
}
