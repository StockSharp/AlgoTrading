//+------------------------------------------------------------------+
//|                                     Check_Trade_Time_Func_EA.mq4 |
//|                        Copyright 2015, MetaQuotes Software Corp. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+
#property copyright "Copyright 2015, MetaQuotes Software Corp."
#property link      "https://www.mql5.com"
#property version   "1.00"
#property strict

input string TradeStartTime; //Trade Start Time (hh:mm)
input string TradeEndTime;   //Trade End Time (hh:mm)


//+------------------------------------------------------------------+
//| Expert initialization function                                   |
//+------------------------------------------------------------------+
int OnInit()
  {
//---
  if(!CheckTradeTime(TradeStartTime,TradeEndTime))
  {
     Alert("Invalid time entry. Please enter valid start and end times.");
     ExpertRemove();
  }   
     
    
//---
   return(INIT_SUCCEEDED);
  }
//+------------------------------------------------------------------+
//| Expert deinitialization function                                 |
//+------------------------------------------------------------------+
void OnDeinit(const int reason)
  {
//---
   
  }
//+------------------------------------------------------------------+
//| Expert tick function                                             |
//+------------------------------------------------------------------+
void OnTick()
  {
//---
   
  }
//+------------------------------------------------------------------+
bool CheckTradeTime(string st,string et)
{
 
   int Strlen1=StringLen(st);
   int Strlen2=StringLen(et);
   int st1=0,st2=0,et1=0,et2=0;
   
   if((Strlen1==5) && (StringGetChar(st,2)==':'))
   {
      st1=StringToInteger(StringSubstr(st,0,2));
      st2=StringToInteger(StringSubstr(st,3,2));
         
      if((Strlen2==5) && (StringGetChar(et,2)==':'))
      {
         et1=StringToInteger(StringSubstr(et,0,2));
         et2=StringToInteger(StringSubstr(et,3,2));
      }
      else if((Strlen2==4) && (StringGetChar(et,1)==':'))
      {
         et1=StringToInteger(StringSubstr(et,0,1));
         et2=StringToInteger(StringSubstr(et,2,2));
      }
      else
      return false;
   } 
   else if((Strlen1==4) && (StringGetChar(st,1)==':'))   
   {
      st1=StringToInteger(StringSubstr(st,0,1));
      st2=StringToInteger(StringSubstr(st,2,2));
         
      if((Strlen2==5) && (StringGetChar(et,2)==':'))
      {
         et1=StringToInteger(StringSubstr(et,0,2));
         et2=StringToInteger(StringSubstr(et,3,2));
      }
      else if((Strlen2==4) && (StringGetChar(et,1)==':'))
      {
         et1=StringToInteger(StringSubstr(et,0,1));
         et2=StringToInteger(StringSubstr(et,2,2));
      }
      else
      return false;
   }
   else 
   return false;
            
      
   if((st1<0) || (st2<0) || (st1>23) || (st2>59) || (et1<0) || (et2<0) || (et1>23) || (et2>59))
   return false;
   
   else
   {
      datetime StartTime=StrToTime(st);     
      datetime EndTime=StrToTime(et);  
      datetime curr_time=TimeCurrent();
   
      if(StartTime<EndTime && curr_time>=StartTime && curr_time<EndTime)  
      return true;
      else
      return false;
   }   
   
}   