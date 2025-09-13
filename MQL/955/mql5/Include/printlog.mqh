#include <MyMQL_v2.1.mqh>
int LogHandle=0;
//+------------------------------------------------------------------+
void LogNewFile(){
   if(MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)){
      LogHandle=FileOpen("equity.csv",FILE_CSV|FILE_WRITE,';');
      FileWrite(LogHandle, "Date", "Value");
      FileClose(LogHandle);
   }   
}//LogNewFile()
//+------------------------------------------------------------------+
void LogUpdate(){
   static datetime LastTimeEquity=0;
   if(MQL5InfoInteger(MQL5_TESTING) && !MQL5InfoInteger(MQL5_OPTIMIZATION)){
      if(LastTimeEquity<iTime(Symbol(),PERIOD_H4,0)){
         LogHandle=FileOpen("equity.csv",FILE_CSV|FILE_READ|FILE_WRITE,';');
         LastTimeEquity=iTime(Symbol(),PERIOD_H4,0);
         FileSeek(LogHandle, 0, SEEK_END);
         FileWrite(LogHandle, TimeToString(LastTimeEquity), DoubleToString(AccountInfoDouble(ACCOUNT_EQUITY),2));
         FileClose(LogHandle);
      }
   }   
}//LogUpdate()
//+------------------------------------------------------------------+
