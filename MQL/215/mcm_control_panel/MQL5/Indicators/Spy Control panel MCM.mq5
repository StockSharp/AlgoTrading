//+------------------------------------------------------------------+
//|                                                   MultiTrend.mq5 |
//|                                            Copyright 2010, Lizar |
//|                            https://login.mql5.com/ru/users/Lizar |
//+------------------------------------------------------------------+
#define VERSION       "1.00 Build 2 (09 Dec 2010)"

#property copyright   "Copyright 2010, Lizar"
#property link        "https://login.mql5.com/ru/users/Lizar"
#property version     VERSION
#property description "This is the spy indicator of MCM Control Panel."
#property description "Being attached to chart, it generates the NewBar or Tick custom events for the MCM Control Panel."

#property indicator_chart_window
  
//+------------------------------------------------------------------+
//| Event enumeration                                                |
//| The events implemented as flags,                                 |
//| you can combine them using the "|" (OR)                          |
//+------------------------------------------------------------------+
  
enum ENUM_CHART_EVENT_SYMBOL
  {
   CHARTEVENT_NEWBAR_NO =0, // No events 
   
   CHARTEVENT_NEWBAR_M1 =0x00000001, // "New bar" event on M1 chart
   CHARTEVENT_NEWBAR_M2 =0x00000002, // "New bar" event on M2 chart
   CHARTEVENT_NEWBAR_M3 =0x00000004, // "New bar" event on M3 chart
   CHARTEVENT_NEWBAR_M4 =0x00000008, // "New bar" event on M4 chart
   
   CHARTEVENT_NEWBAR_M5 =0x00000010, // "New bar" event on M5 chart
   CHARTEVENT_NEWBAR_M6 =0x00000020, // "New bar" event on M6 chart
   CHARTEVENT_NEWBAR_M10=0x00000040, // "New bar" event on M10 chart
   CHARTEVENT_NEWBAR_M12=0x00000080, // "New bar" event on M12 chart
   
   CHARTEVENT_NEWBAR_M15=0x00000100, // "New bar" event on M15 chart
   CHARTEVENT_NEWBAR_M20=0x00000200, // "New bar" event on M20 chart
   CHARTEVENT_NEWBAR_M30=0x00000400, // "New bar" event on M30 chart
   CHARTEVENT_NEWBAR_H1 =0x00000800, // "New bar" event on H1 chart
   
   CHARTEVENT_NEWBAR_H2 =0x00001000, // "New bar" event on H2 chart
   CHARTEVENT_NEWBAR_H3 =0x00002000, // "New bar" event on H3 chart
   CHARTEVENT_NEWBAR_H4 =0x00004000, // "New bar" event on H4 chart
   CHARTEVENT_NEWBAR_H6 =0x00008000, // "New bar" event on H6 chart
   
   CHARTEVENT_NEWBAR_H8 =0x00010000, // "New bar" event on H8 chart
   CHARTEVENT_NEWBAR_H12=0x00020000, // "New bar" event on H12 chart
   CHARTEVENT_NEWBAR_D1 =0x00040000, // "New bar" event on daily chart
   CHARTEVENT_NEWBAR_W1 =0x00080000, // "New bar" event on weekly chart
     
   CHARTEVENT_NEWBAR_MN1=0x00100000, // "New bar" event on monthly chart
   CHARTEVENT_TICK      =0x00200000, // "New tick" event
   
   CHARTEVENT_ALL       =0xFFFFFFFF, // All events 
  };

input long                    chart_id;                         // chart id
input ushort                  custom_event_id;                  // custom event id
input ENUM_CHART_EVENT_SYMBOL flag_period=CHARTEVENT_NEWBAR_NO; // flag for periods

MqlDateTime time, prev_time;

//+------------------------------------------------------------------+
//| Custom indicator iteration function                              |
//+------------------------------------------------------------------+
int OnCalculate (const int rates_total,      // size of price[] array
                 const int prev_calculated,  // number of bars, calculated at previous call
                 const int begin,            // data begin index
                 const double& price[]       // array for calculation
   )
  {  
   double price_current=price[rates_total-1];

   TimeCurrent(time);
   
   if(prev_calculated==0)
     {
//      EventChartCustom(chart_id,0,CHARTEVENT_NEWBAR_NO,price_current,_Symbol);   
      EventCustom(CHARTEVENT_NEWBAR_NO,price_current);
      prev_time=time; 
      return(rates_total);
     }
   
//--- new tick
   if((flag_period & CHARTEVENT_TICK)!=0) EventCustom(CHARTEVENT_TICK,price_current);       

//--- check change time
   if(time.min==prev_time.min && 
      time.hour==prev_time.hour && 
      time.day==prev_time.day &&
      time.mon==prev_time.mon) return(rates_total);

//--- new minute
   if((flag_period & CHARTEVENT_NEWBAR_M1)!=0) EventCustom(CHARTEVENT_NEWBAR_M1,price_current);     
   if(time.min%2 ==0 && (flag_period & CHARTEVENT_NEWBAR_M2)!=0)  EventCustom(CHARTEVENT_NEWBAR_M2,price_current);
   if(time.min%3 ==0 && (flag_period & CHARTEVENT_NEWBAR_M3)!=0)  EventCustom(CHARTEVENT_NEWBAR_M3,price_current); 
   if(time.min%4 ==0 && (flag_period & CHARTEVENT_NEWBAR_M4)!=0)  EventCustom(CHARTEVENT_NEWBAR_M4,price_current);      
   if(time.min%5 ==0 && (flag_period & CHARTEVENT_NEWBAR_M5)!=0)  EventCustom(CHARTEVENT_NEWBAR_M5,price_current);     
   if(time.min%6 ==0 && (flag_period & CHARTEVENT_NEWBAR_M6)!=0)  EventCustom(CHARTEVENT_NEWBAR_M6,price_current);     
   if(time.min%10==0 && (flag_period & CHARTEVENT_NEWBAR_M10)!=0) EventCustom(CHARTEVENT_NEWBAR_M10,price_current);      
   if(time.min%12==0 && (flag_period & CHARTEVENT_NEWBAR_M12)!=0) EventCustom(CHARTEVENT_NEWBAR_M12,price_current);      
   if(time.min%15==0 && (flag_period & CHARTEVENT_NEWBAR_M15)!=0) EventCustom(CHARTEVENT_NEWBAR_M15,price_current);      
   if(time.min%20==0 && (flag_period & CHARTEVENT_NEWBAR_M20)!=0) EventCustom(CHARTEVENT_NEWBAR_M20,price_current);      
   if(time.min%30==0 && (flag_period & CHARTEVENT_NEWBAR_M30)!=0) EventCustom(CHARTEVENT_NEWBAR_M30,price_current);      
   if(time.min!=0) {prev_time=time; return(rates_total);}
//--- new hour
   if((flag_period & CHARTEVENT_NEWBAR_H1)!=0) EventCustom(CHARTEVENT_NEWBAR_H1,price_current);
   if(time.hour%2 ==0 && (flag_period & CHARTEVENT_NEWBAR_H2)!=0)  EventCustom(CHARTEVENT_NEWBAR_H2,price_current);
   if(time.hour%3 ==0 && (flag_period & CHARTEVENT_NEWBAR_H3)!=0)  EventCustom(CHARTEVENT_NEWBAR_H3,price_current);      
   if(time.hour%4 ==0 && (flag_period & CHARTEVENT_NEWBAR_H4)!=0)  EventCustom(CHARTEVENT_NEWBAR_H4,price_current);      
   if(time.hour%6 ==0 && (flag_period & CHARTEVENT_NEWBAR_H6)!=0)  EventCustom(CHARTEVENT_NEWBAR_H6,price_current);      
   if(time.hour%8 ==0 && (flag_period & CHARTEVENT_NEWBAR_H8)!=0)  EventCustom(CHARTEVENT_NEWBAR_H8,price_current);      
   if(time.hour%12==0 && (flag_period & CHARTEVENT_NEWBAR_H12)!=0) EventCustom(CHARTEVENT_NEWBAR_H12,price_current);      
   if(time.hour!=0) {prev_time=time; return(rates_total);}
//--- new day
   if((flag_period & CHARTEVENT_NEWBAR_D1)!=0) EventCustom(CHARTEVENT_NEWBAR_D1,price_current);      
 //--- new week
   if(time.day_of_week==1 && (flag_period & CHARTEVENT_NEWBAR_W1)!=0) EventCustom(CHARTEVENT_NEWBAR_W1,price_current);      
 //--- new month
   if(time.day==1 && (flag_period & CHARTEVENT_NEWBAR_MN1)!=0) EventCustom(CHARTEVENT_NEWBAR_MN1,price_current);      
   prev_time=time;
//--- return value of prev_calculated for next call
   return(rates_total);
  }
//+------------------------------------------------------------------+

void EventCustom(ENUM_CHART_EVENT_SYMBOL event,double price)
  {
   EventChartCustom(chart_id,custom_event_id,(long)event,price,_Symbol);
   return;
  }