//+------------------------------------------------------------------+
//|                                                StringSetting.mqh |
//+------------------------------------------------------------------+

#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "Setting.mqh"

class StringSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, string value);
   
      string Value() const;
      
      void OnApply(string& value);
      
   private:
      void DrawMinMax();
      void DrawNextPrevious();

   private:
      string m_StringValue;
};

void StringSetting::Init(string settingName, int settingID, int UID, string value)
{
   m_StringValue = value;
   
   string min = "";
   string max = "";
   
   DrawSetting::Init(settingName, settingID, UID, m_StringValue, min, max);
}

string StringSetting::Value() const
{
   return string(m_StringValue);
}

void StringSetting::OnApply(string& value)
{
   m_StringValue = GetEditText();
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, m_StringValue);
   value = string(m_StringValue);
}

void StringSetting::DrawMinMax()
{
}

void StringSetting::DrawNextPrevious()
{
}
