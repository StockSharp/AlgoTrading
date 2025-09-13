//+------------------------------------------------------------------+
//|                                                     DateTime.mqh |
//|                               Copyright (c) 2019-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Date and Time field extractor                                    |
//+------------------------------------------------------------------+
class DateTime
{
private:
   MqlDateTime mdtstruct;
   datetime origin;

   DateTime() : origin(0)
   {
      TimeToStruct(0, mdtstruct);
   }
   
   void convert(const datetime &dt)
   {
      if(origin != dt)
      {
         origin = dt;
         TimeToStruct(dt, mdtstruct);
      }
   }

public:
   static DateTime *assign(const datetime dt)
   {
      _DateTime.convert(dt);
      return &_DateTime;
   }
   ENUM_DAY_OF_WEEK timeDayOfWeek() const
   {
      return (ENUM_DAY_OF_WEEK)mdtstruct.day_of_week;
   }
   int timeDayOfYear() const
   {
      return mdtstruct.day_of_year;
   }
   int timeYear() const
   {
      return mdtstruct.year;
   }
   int timeMonth() const
   {
      return mdtstruct.mon;
   }
   int timeDay() const
   {
      return mdtstruct.day;
   }
   int timeHour() const
   {
      return mdtstruct.hour;
   }
   int timeMinute() const
   {
      return mdtstruct.min;
   }
   int timeSeconds() const
   {
      return mdtstruct.sec;
   }

   static DateTime _DateTime;
};

static DateTime DateTime::_DateTime;

#define TimeDayOfWeek(T) DateTime::assign(T).timeDayOfWeek()
#define TimeDayOfYear(T) DateTime::assign(T).timeDayOfYear()
#define TimeYear(T) DateTime::assign(T).timeYear()
#define TimeMonth(T) DateTime::assign(T).timeMonth()
#define TimeDay(T) DateTime::assign(T).timeDay()
#define TimeHour(T) DateTime::assign(T).timeHour()
#define TimeMinute(T) DateTime::assign(T).timeMinute()
#define TimeSeconds(T) DateTime::assign(T).timeSeconds()

#define _TimeDayOfWeek DateTime::_DateTime.timeDayOfWeek
#define _TimeDayOfYear DateTime::_DateTime.timeDayOfYear
#define _TimeYear DateTime::_DateTime.timeYear
#define _TimeMonth DateTime::_DateTime.timeMonth
#define _TimeDay DateTime::_DateTime.timeDay
#define _TimeHour DateTime::_DateTime.timeHour
#define _TimeMinute DateTime::_DateTime.timeMinute
#define _TimeSeconds DateTime::_DateTime.timeSeconds

//+------------------------------------------------------------------+
