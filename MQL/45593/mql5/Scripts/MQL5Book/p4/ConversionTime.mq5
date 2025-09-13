//+------------------------------------------------------------------+
//|                                               ConversionTime.mq5 |
//|                                  Copyright 2021, MetaQuotes Ltd. |
//|                                             https://www.mql5.com |
//+------------------------------------------------------------------+

#define PRT(A) Print(#A, "=", (A))

//+------------------------------------------------------------------+
//| Script program start function                                    |
//+------------------------------------------------------------------+
void OnStart()
{
   datetime time = D'2021.01.21 23:00:15';
   PRT((string)time);
   PRT(TimeToString(time));
   PRT(TimeToString(time, TIME_DATE | TIME_MINUTES | TIME_SECONDS));
   PRT(TimeToString(time, TIME_MINUTES | TIME_SECONDS));
   PRT(TimeToString(time, TIME_DATE | TIME_SECONDS));
   PRT(TimeToString(time, TIME_DATE));
   PRT(TimeToString(time, TIME_MINUTES));
   PRT(TimeToString(time, TIME_SECONDS));
   
   /* will output:
   (string)time=2021.01.21 23:00:15
   TimeToString(time)=2021.01.21 23:00
   TimeToString(time,TIME_DATE|TIME_MINUTES|TIME_SECONDS)=2021.01.21 23:00:15
   TimeToString(time,TIME_MINUTES|TIME_SECONDS)=23:00:15
   TimeToString(time,TIME_DATE|TIME_SECONDS)=2021.01.21 23:00:15
   TimeToString(time,TIME_DATE)=2021.01.21
   TimeToString(time,TIME_MINUTES)=23:00
   TimeToString(time,TIME_SECONDS)=23:00:15
   */

   string timeonly = "21:01";
   PRT(timeonly);
   PRT((datetime)timeonly);
   PRT(StringToTime(timeonly));
   
   string date = "2000-10-10";
   PRT((datetime)date);
   PRT(StringToTime(date));
   PRT((long)(datetime)date);
   long seconds = 60;
   PRT((datetime)seconds); // 1 minute since 1970
   
   string ddmmyy = "15/01/2012 01:02:03";     // still ok
   PRT(StringToTime(ddmmyy));
   
   string wrong = "January 2-nd";
   PRT(StringToTime(wrong));
   PRT(GetLastError());
   
   /* will output: (####.##.## denotes your current date)
   timeonly=21:01
   (datetime)timeonly=####.##.## 21:01:00
   StringToTime(timeonly)=####.##.## 21:01:00
   (datetime)date=2000.10.10 00:00:00
   StringToTime(date)=2000.10.10 00:00:00
   (long)(datetime)date=971136000
   (datetime)((long)(datetime)date+1)=2000.10.10 00:00:01
   (datetime)seconds=1970.01.01 00:01:00
   StringToTime(ddmmyy)=2012.01.15 01:02:03
   (datetime)wrong=####.##.## 00:00:00
   GetLastError()=5031
   */
}
//+------------------------------------------------------------------+
