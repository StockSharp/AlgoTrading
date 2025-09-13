//+------------------------------------------------------------------+
//|                                                    CommonUID.mqh |
//+------------------------------------------------------------------+
#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

#define UID_GLOBAL "Impl_CommonUIDIdentifier"
#define TIMEOUT_SECONDS 10

class CommonUID
{
   public:
      CommonUID();
      
      int ID() const {return m_ID;}
      
   private:
      int m_ID;
};

CommonUID::CommonUID()
{
   uint startTicks = GetTickCount();
   
   int hFile = FileOpen(UID_GLOBAL, FILE_READ | FILE_WRITE);
   while (hFile == INVALID_HANDLE)
   {
      uint now = GetTickCount();
      if (now < startTicks) startTicks = now;
      
      if (now - startTicks > TIMEOUT_SECONDS*1000)
      {
         Alert("CommonUID: unexpected situation -- trying to lock finished by timeout");
         return;
      }
      
      while (GetTickCount() - now < 100 || GetTickCount() < now){}
      hFile = FileOpen(UID_GLOBAL, FILE_READ | FILE_WRITE);
   }
   
   if (GlobalVariableCheck(UID_GLOBAL) == 0)
   {
      m_ID = 1;
   }
   else
   {
      m_ID = int(GlobalVariableGet(UID_GLOBAL) + 1);
   }
   
   GlobalVariableSet(UID_GLOBAL, m_ID);
   FileClose(hFile);
}

int GetUID() export
{
   CommonUID id;
   return id.ID();
}