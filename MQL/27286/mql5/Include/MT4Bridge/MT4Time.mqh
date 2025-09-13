class DateTime
{
  private:
    MqlDateTime mdtstruct;

  public:
    DateTime(){TimeToStruct(0, mdtstruct);}
    DateTime *assign(datetime dt) {TimeToStruct(dt, mdtstruct); return &this;}
    int __TimeDayOfWeek() {return mdtstruct.day_of_week;}
    int __TimeDayOfYear() {return mdtstruct.day_of_year;}
    int __TimeYear() {return mdtstruct.year;}
    int __TimeMonth() {return mdtstruct.mon;}
    int __TimeDay() {return mdtstruct.day;}
    int __TimeHour() {return mdtstruct.hour;}
    int __TimeMinute() {return mdtstruct.min;}
    int __TimeSeconds() {return mdtstruct.sec;}
};

DateTime _DateTime;

#define TimeDayOfWeek(T) _DateTime.assign(T).__TimeDayOfWeek()
#define TimeYear(T) _DateTime.assign(T).__TimeYear()
#define TimeMonth(T) _DateTime.assign(T).__TimeMonth()
#define TimeDay(T) _DateTime.assign(T).__TimeDay()
#define TimeHour(T) _DateTime.assign(T).__TimeHour()
#define TimeMinute(T) _DateTime.assign(T).__TimeMinute()
#define TimeSeconds(T) _DateTime.assign(T).__TimeSeconds()

#define _TimeYear _DateTime.__TimeYear
#define _TimeMonth _DateTime.__TimeMonth
#define _TimeDay _DateTime.__TimeDay
#define _TimeHour _DateTime.__TimeHour
#define _TimeMinute _DateTime.__TimeMinute
#define _TimeSeconds _DateTime.__TimeSeconds

#define Year _DateTime.assign(TimeCurrent()).__TimeYear
#define Month _DateTime.assign(TimeCurrent()).__TimeMonth
#define Day _DateTime.assign(TimeCurrent()).__TimeDay
#define Hour _DateTime.assign(TimeCurrent()).__TimeHour
#define Minute _DateTime.assign(TimeCurrent()).__TimeMinute
#define Seconds _DateTime.assign(TimeCurrent()).__TimeSeconds
