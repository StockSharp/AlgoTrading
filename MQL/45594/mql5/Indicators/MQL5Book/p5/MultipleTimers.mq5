//+------------------------------------------------------------------+
//|                                               MultipleTimers.mq5 |
//|                                 Copyright © 2016-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| Multiple timers with publisher/subscriber design pattern         |
//+------------------------------------------------------------------+
#property indicator_chart_window
#property indicator_buffers 0
#property indicator_plots   0

#include <MQL5Book/MultiTimer.mqh>

//+------------------------------------------------------------------+
//| Enum to choose base (system) timer granularity                   |
//+------------------------------------------------------------------+
enum TIMER_TYPE
{
   Seconds,
   Milliseconds
};

input TIMER_TYPE TimerType = Seconds;
input int BaseTimerPeriod = 1;

//+------------------------------------------------------------------+
//| Specific timer class                                             |
//+------------------------------------------------------------------+
class MyCountableTimer: public CountableTimer
{
public:
   MyCountableTimer(const int s, const uint r = UINT_MAX):
      CountableTimer(s, r) { }

   virtual void notify() override
   {
      Print(__FUNCSIG__, multiplier, " ", count);
   }
};

//+------------------------------------------------------------------+
//| Another specfic timer class (created in suspended state)         |
//+------------------------------------------------------------------+
class MySuspendedTimer: public CountableTimer
{
public:
   MySuspendedTimer(const int s, const uint r = UINT_MAX):
      CountableTimer(s, r, true) { }

   virtual void notify() override
   {
      Print(__FUNCSIG__, multiplier, " ", count);
      if(count == repeat - 1) // last time execution
      {
         Print("Forcing all timers to stop");
         EventKillTimer();
      }
   }
};

//+------------------------------------------------------------------+
//| Timer objects with different periods (bind during global init)   |
//+------------------------------------------------------------------+
MySuspendedTimer st(1, 5);
MyCountableTimer t1(2);
MyCountableTimer t2(4);

//+------------------------------------------------------------------+
//| Single-handler-function-alike timer define for 5 base periods    |
//+------------------------------------------------------------------+
bool OnTimerCustom(5)
{
   Print(__FUNCSIG__);
   st.start();         // resume suspended timer
   return false;       // stop this timer
}

//+------------------------------------------------------------------+
//| Single-handler-function-alike timer define for 3 base periods    |
//+------------------------------------------------------------------+
bool OnTimerCustom(3)
{
   Print(__FUNCSIG__);
   return true;        // keep this timer running
}

//+------------------------------------------------------------------+
//| Custom indicator initialization function                         |
//+------------------------------------------------------------------+
void OnInit()
{
   Print(__FUNCSIG__, " ", BaseTimerPeriod, " ", EnumToString(TimerType));
   if(TimerType == Seconds)
   {
      EventSetTimer(BaseTimerPeriod);
   }
   else
   {
      EventSetMillisecondTimer(BaseTimerPeriod);
   }
}

//+------------------------------------------------------------------+
//| Default timer handler                                            |
//+------------------------------------------------------------------+
void OnTimer()
{
   // invoke MultiTimer method to check and fire specific timers when appropriate
   MultiTimer::onTimer();
}

//+------------------------------------------------------------------+
//| Custom indicator iteration function (dummy)                      |
//+------------------------------------------------------------------+
int OnCalculate(const int rates_total,
                const int prev_calculated,
                const int begin,
                const double &data[])
{
   return 0;
}
//+------------------------------------------------------------------+
/*
   Example output (seconds):
   
   17:08:45.174	void OnInit() 1 Seconds
   17:08:47.202	void MyCountableTimer::notify()2 0
   17:08:48.216	bool OnTimer3()
   17:08:49.230	void MyCountableTimer::notify()2 1
   17:08:49.230	void MyCountableTimer::notify()4 0
   17:08:50.244	bool OnTimer5()
   17:08:51.258	void MyCountableTimer::notify()2 2
   17:08:51.258	bool OnTimer3()
   17:08:51.258	void MySuspendedTimer::notify()1 0
   17:08:52.272	void MySuspendedTimer::notify()1 1
   17:08:53.286	void MyCountableTimer::notify()2 3
   17:08:53.286	void MyCountableTimer::notify()4 1
   17:08:53.286	void MySuspendedTimer::notify()1 2
   17:08:54.300	bool OnTimer3()
   17:08:54.300	void MySuspendedTimer::notify()1 3
   17:08:55.314	void MyCountableTimer::notify()2 4
   17:08:55.314	void MySuspendedTimer::notify()1 4
   17:08:55.314	Forcing all timers to stop

   Example output (milliseconds):

   17:27:54.483	void OnInit() 1 Milliseconds
   17:27:54.514	void MyCountableTimer::notify()2 0
   17:27:54.545	bool OnTimer3()
   17:27:54.561	void MyCountableTimer::notify()2 1
   17:27:54.561	void MyCountableTimer::notify()4 0
   17:27:54.577	bool OnTimer5()
   17:27:54.608	void MyCountableTimer::notify()2 2
   17:27:54.608	bool OnTimer3()
   17:27:54.608	void MySuspendedTimer::notify()1 0
   17:27:54.623	void MySuspendedTimer::notify()1 1
   17:27:54.655	void MyCountableTimer::notify()2 3
   17:27:54.655	void MyCountableTimer::notify()4 1
   17:27:54.655	void MySuspendedTimer::notify()1 2
   17:27:54.670	bool OnTimer3()
   17:27:54.670	void MySuspendedTimer::notify()1 3
   17:27:54.686	void MyCountableTimer::notify()2 4
   17:27:54.686	void MySuspendedTimer::notify()1 4
   17:27:54.686	Forcing all timers to stop

*/
//+------------------------------------------------------------------+
