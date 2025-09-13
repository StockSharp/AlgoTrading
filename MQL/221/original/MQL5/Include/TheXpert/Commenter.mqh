//+------------------------------------------------------------------+
//|                                                    Commenter.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "CommonUID.mqh"
#include "Objects.mqh"
#include "Comments.mqh"

#include "PaneSettings.mqh"

#define COMMENTS_INDIE_TRIGGER_NAME "Logs for Trade Xpert"

class Commenter
{
public:
   Commenter();
   ~Commenter();
   
   int Window() const;
   void Draw();
   void Hide();
   
   void OnTick();
   void OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam);
   
private:
   void OnSettingChanged(int id, string value);

private:

   int m_UID;
   color m_TextColor;
   int m_FontSize;
};

Commenter::Commenter(void)
{
   m_UID = GetUID();
   m_TextColor = LightGray;
   m_FontSize = 8;
}

int Commenter::Window() const
{
   int windows = int(ChartGetInteger(0, CHART_WINDOWS_TOTAL));
   
   for (int i = 0; i < windows; i++)
   {
      int indies = ChartIndicatorsTotal(0, i);
      
      for (int j = 0; j < indies; j++)
      {
         if (ChartIndicatorName(0, i, j) == COMMENTS_INDIE_TRIGGER_NAME)
         {
            return i;
         }
      }
   }
   return -1;
}

void Commenter::Draw()
{
   int wnd = Window();
   if (wnd == -1) return;
   
   int lines = GetLines();
   
   for (int i = 0; i < lines; i++)
   {
      string pre = "Prefix" + string(m_UID)+ " " + string(i);
      string text = "Text" + string(m_UID)+ " " + string(i);
      
      string preValue;
      string textValue;
      
      if (GetLine(i, preValue, textValue))
      {
         if (StringLen(preValue) == 0) preValue = " ";
         if (StringLen(textValue) == 0) textValue= " ";
      
         CreateObject(0, wnd, pre, OBJ_LABEL, preValue);
         ObjectSetString(0, pre, OBJPROP_FONT, "Lucida Console");
         ObjectSetInteger(0, pre, OBJPROP_FONTSIZE, m_FontSize);
         ObjectSetInteger(0, pre, OBJPROP_XDISTANCE, 10);
         ObjectSetInteger(0, pre, OBJPROP_YDISTANCE, 20 + i*int(1.5*m_FontSize));
         ObjectSetInteger(0, pre, OBJPROP_COLOR, m_TextColor);
         
         CreateObject(0, wnd, text, OBJ_LABEL, textValue);
         ObjectSetString(0, text, OBJPROP_FONT, "Lucida Console");
         ObjectSetInteger(0, text, OBJPROP_FONTSIZE, m_FontSize);
         ObjectSetInteger(0, text, OBJPROP_XDISTANCE, 10 + 10*m_FontSize);
         ObjectSetInteger(0, text, OBJPROP_YDISTANCE, 20 + i*int(1.5*m_FontSize));
         ObjectSetInteger(0, text, OBJPROP_COLOR, m_TextColor);
      }
      else break;
   }
}

void Commenter::Hide()
{
   int wnd = Window();
   if (wnd == -1) return;

   int total = ObjectsTotal(0, wnd, OBJ_LABEL);
   
   for (int i = total - 1; i >= 0; i--)
   {
      string name = ObjectName(0, i, wnd, OBJ_LABEL);
      if (
            StringFind(name, "Prefix" + string(m_UID)+ " ") == 0 ||
            StringFind(name, "Text" + string(m_UID)+ " ") == 0
         )
      {
         ObjectDelete(0, name);
      }
   }
}

void Commenter::OnTick()
{
   Draw();
}

Commenter::~Commenter()
{
   Hide();
}


void Commenter::OnChartEvent(const int id, const long& lparam, const double& dparam, const string& sparam)
{
   if (id == CHARTEVENT_CUSTOM + EVENT_SETTING_CHANGED)
   {
      OnSettingChanged(int(lparam), sparam);
   }
   Draw();
}

void Commenter::OnSettingChanged(int id, string value)
{
   switch (id)
   {
      case SETTING_LOG_FONT_SIZE:
      {
         m_FontSize = int(value);
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_LINES:
      {
         SetLines(int(value));
         Hide();
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_LENGTH:
      {
         SetLength(int(value));
         Hide();
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_TABSIZE:
      {
         SetTabSize(int(value));
         Draw();
      }
      break;
      
      case SETTING_LOG_FONT_COLOR:
      {
         m_TextColor = color(value);
         Draw();
      }
      break;
      
   }
}