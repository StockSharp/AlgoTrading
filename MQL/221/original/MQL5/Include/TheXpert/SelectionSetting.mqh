//+------------------------------------------------------------------+
//|                                                      Setting.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#include "Assert.mqh"
#include "Setting.mqh"
#include "SelectionTable.mqh"

class SelectionSetting
   : public DrawSetting
{
   public:
      void Init(string settingName, int settingID, int UID, int valueID, const SelectionTable& ids);
   
      string Value() const;
      
      void OnBtnUp(string& value);
      void OnBtnDn(string& value);

   private:
      virtual void DrawMinMax();
      virtual void DrawValue();
      virtual void DrawApply();

   private:
      int      m_ValueID;
      SelectionTable m_Table;
};

void SelectionSetting::Init(string settingName, int settingID, int UID, int valueID, const SelectionTable& ids)
{
   m_ValueID = valueID;
   m_Table.CopyFrom(ids);
   
   string strValue = Value();
   string strMax = "";
   string strMin = "";
   
   DrawSetting::Init(settingName, settingID, UID, strValue, strMax, strMin);
}

string SelectionSetting::Value() const
{
   return m_Table.GetNameByID(m_ValueID);
}

void SelectionSetting::OnBtnUp(string& value)
{
   m_ValueID = m_Table.Next(m_ValueID);

   value = m_Table.GetNameByID(m_ValueID);
   
   string strChanged = string(m_ValueID);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, strChanged);
}

void SelectionSetting::OnBtnDn(string& value)
{
   m_ValueID = m_Table.Previous(m_ValueID);

   value = m_Table.GetNameByID(m_ValueID);
   
   string strChanged = string(m_ValueID);
   EventChartCustom(m_Chart, EVENT_SETTING_CHANGED, m_SettingID, 0, strChanged);
}

void SelectionSetting::DrawMinMax()
{
}

void SelectionSetting::DrawValue()
{
   DrawSetting::DrawValue();
   ObjectSetInteger(m_Chart, m_ValueEdit, OBJPROP_READONLY, true);
}

void SelectionSetting::DrawApply()
{
}