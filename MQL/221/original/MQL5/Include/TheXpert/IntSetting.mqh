//+------------------------------------------------------------------+
//|                                                      Setting.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "Setting.mqh"

class IntSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, int value, int step = 1, int min = INT_MIN, int max = INT_MAX);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);
      void OnApply(string& value);

   private:
      int      m_IntValue;
      int      m_IntStep;
      int      m_IntMax;
      int      m_IntMin;
};

void IntSetting::Init(string settingName, int settingID, int UID, int value, int step, int min, int max)
{
   m_IntValue = value;
   m_IntMin = min;
   m_IntMax = max;
   m_IntStep = step;
   
   if (m_IntStep <= 0) m_IntStep = 1;
   
   string strValue = string(value);
   string strMax = string(max);
   string strMin = string(min);
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string IntSetting::Value() const
{
   return string(m_IntValue);
}

void IntSetting::OnBtnUp(string& value)
{
   m_IntValue += m_IntStep;
   if (m_IntValue > m_IntMax)
   {
      m_IntValue = m_IntMax;
   }

   value = string(m_IntValue);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void IntSetting::OnBtnDn(string& value)
{
   m_IntValue -= m_IntStep;
   if (m_IntValue < m_IntMin)
   {
      m_IntValue = m_IntMin;
   }

   value = string(m_IntValue);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void IntSetting::OnApply(string& value)
{
   string text = GetEditText();
   
   if (text != "")
   {
      long candidate = StringToInteger(text);
      string revert = string(candidate);
      
      if (revert == text)
      {
         m_IntValue = int(candidate);
         value = string(m_IntValue);
         EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
      }
   }
   value = string(m_IntValue);
}