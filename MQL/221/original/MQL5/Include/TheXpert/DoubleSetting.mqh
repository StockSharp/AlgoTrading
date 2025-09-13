//+------------------------------------------------------------------+
//|                                                DoubleSetting.mqh |
//+------------------------------------------------------------------+

#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "Setting.mqh"

class DoubleSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, double value, double step = 1, double min = -DBL_MAX, double max = DBL_MAX);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);
      void OnApply(string& value);

   private:
      double   m_DoubleValue;
      double   m_DoubleStep;
      double   m_DoubleMax;
      double   m_DoubleMin;
};

void DoubleSetting::Init(string settingName, int settingID, int UID, double value, double step, double min, double max)
{
   m_DoubleValue = value;
   m_DoubleMin = min;
   m_DoubleMax = max;
   m_DoubleStep = step;
   
   if (m_DoubleStep <= 0) m_DoubleStep = 1;
   
   string strValue = string(value);
   string strMax = string(max);
   string strMin = string(min);
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string DoubleSetting::Value() const
{
   return string(m_DoubleValue);
}

void DoubleSetting::OnBtnUp(string& value)
{
   m_DoubleValue += m_DoubleStep;
   if (m_DoubleValue > m_DoubleMax)
   {
      m_DoubleValue = m_DoubleMax;
   }

   value = string(m_DoubleValue); 
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void DoubleSetting::OnBtnDn(string& value)
{
   m_DoubleValue -= m_DoubleStep;
   if (m_DoubleValue < m_DoubleMin)
   {
      m_DoubleValue = m_DoubleMin;
   }

   value = string(m_DoubleValue);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void DoubleSetting::OnApply(string& value)
{
   string text = GetEditText();
   
   if (text != "")
   {
      double candidate = StringToDouble(text);
      
      if (candidate != 0 || StringFind(text, "0") == 0)
      {
         m_DoubleValue = candidate;
         
         value = string(m_DoubleValue);
         EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
      }
   }
   value = string(m_DoubleValue);
}