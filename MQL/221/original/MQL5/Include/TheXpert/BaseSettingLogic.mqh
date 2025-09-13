//+------------------------------------------------------------------+
//|                                             BaseSettingLogic.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#define EVENT_SETTING_CHANGED 4456

class ISettingEvents
{
   public:
      virtual void OnBtnUp(string& value)
      {}
      
      virtual void OnBtnDn(string& value)
      {}
      
      virtual void OnApply(string& value)
      {}

      virtual void Draw(int x, int y, int chartID = 0, int subWnd = 0, int corner = 0)
      {}
      
      virtual void Hide()
      {}
};