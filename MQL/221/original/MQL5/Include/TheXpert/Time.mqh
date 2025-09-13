//+------------------------------------------------------------------+
//|                                                         Time.mqh |
//+------------------------------------------------------------------+

#property copyright "TheXpert"
#property link      "theforexpert@gmail.com"

enum ETimeKind
{
   ETIME_LOCAL,
   ETIME_SERVER,
   ETIME_CET,
   ETIME_MOSCOW,
   ETIME_EST,
   ETIME_GMT
};

datetime FromGMT(int offset)
{
   static int hour = 60*60;
   return TimeGMT() + offset*hour;
}

datetime GetTime(ETimeKind time)
{
   switch (time)
   {
      case ETIME_LOCAL: return TimeLocal();
      case ETIME_SERVER: return TimeCurrent();
      case ETIME_CET: return FromGMT(1);
      case ETIME_MOSCOW: return FromGMT(3);
      case ETIME_EST: return FromGMT(-5);
      case ETIME_GMT: return TimeGMT();
   }
   return 0;
}

string GetTimeBase(ETimeKind time)
{
   switch (time)
   {
      case ETIME_LOCAL: return "Local";
      case ETIME_SERVER: return "Server";
      case ETIME_CET: return "CET";
      case ETIME_MOSCOW: return "Moscow";
      case ETIME_EST: return "EST";
      case ETIME_GMT: return "GMT";
   }
   return "N/A";
}

int TimeDay(datetime time)
{
   MqlDateTime info;
   TimeToStruct(time, info);
   
   return info.day_of_week;
}

string DayToString(int day)
{
   switch (day)
   {
      case 0: return "Sun";
      case 1: return "Mon";
      case 2: return "Tue";
      case 3: return "Wed";
      case 4: return "Thu";
      case 5: return "Fri";
      case 6: return "Sat";
   }
   return "N/A";
}