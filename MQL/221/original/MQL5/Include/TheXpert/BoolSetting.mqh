//+------------------------------------------------------------------+
//|                                                  BoolSetting.mqh |
//+------------------------------------------------------------------+

#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "Setting.mqh"
#include "BoolTable.mqh"

class BoolSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, bool value);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);

   private:
      virtual void DrawMinMax();
      virtual void DrawValue();
      virtual void DrawApply();

   private:
      int m_BoolValue;
      BoolTable m_Table;
};

void BoolSetting::Init(string settingName,int settingID,int UID,bool value)
{
   m_BoolValue = value;
   
   string strValue = Value();
   string strMax = "";
   string strMin = "";
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string BoolSetting::Value() const
{
   return m_Table.GetNameByValue(m_BoolValue);
}

void BoolSetting::OnBtnUp(string& value)
{
   m_BoolValue = !m_BoolValue;

   value = Value();
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void BoolSetting::OnBtnDn(string& value)
{
   m_BoolValue = !m_BoolValue;

   value = Value();
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, value);
}

void BoolSetting::DrawMinMax()
{
}

void BoolSetting::DrawValue()
{
   DrawSetting::DrawValue();
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_READONLY, true);
}

void BoolSetting::DrawApply()
{
}