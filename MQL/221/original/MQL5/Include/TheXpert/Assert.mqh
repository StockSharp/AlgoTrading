//+------------------------------------------------------------------+
//|                                                       Assert.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

class ReleaseTrigger
{
   public:
      ReleaseTrigger()
      {
         m_Release = false;
      }
   
      bool m_Release;
};

ReleaseTrigger ReleaseTriggerImpl;

void SetRelease(bool isRelease = true) export
{
   ReleaseTriggerImpl.m_Release = isRelease;
}

void Assert(bool condition, string message = "") export
{
   if (!condition && !ReleaseTriggerImpl.m_Release)
   {
      string msg = "Assertion failed";
      if (message != "")
      {
         msg = msg + ", assert message: " + message;
      }
      
      Alert(msg);
   }
}