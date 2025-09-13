//+------------------------------------------------------------------+
//|                                                   MultiTimer.mqh |
//|                                 Copyright © 2016-2021, Marketeer |
//|                          https://www.mql5.com/en/users/marketeer |
//| Multiple timers with publisher/subscriber design pattern         |
//+------------------------------------------------------------------+

//+------------------------------------------------------------------+
//| Base 'interface' for processing timer events                     |
//| "Subscriber"                                                     |
//| (abstract, can not be instantiated)                              |
//+------------------------------------------------------------------+
class TimerNotification
{
protected:
   int chronometer;

public:
   TimerNotification(): chronometer(0)
   {
   }
   
   // timer event
   // pure virtual method, must be implemented in descendants
   virtual void notify() = 0;

   // return current timer period (can be changed on the go)
   // pure virtual method, must be implemented in descendants
   virtual int getInterval() = 0;

   // check if timer should fire now
   virtual bool isTimeCome()
   {
      if(chronometer >= getInterval() - 1)
      {
         chronometer = 0;
         notify();
         return true;
      }
      
      ++chronometer;
      return false;
   }
};

//+------------------------------------------------------------------+
//| Main and single processor of timer events coming from terminal   |
//| "Publisher"                                                      |
//+------------------------------------------------------------------+
class MultiTimer
{
protected:
   TimerNotification *subscribers[];
   bool enabled;

   // single instance of main timer
   static MultiTimer _mainTimer;

   // protected constructor prevents creation of more instances than our static one
   MultiTimer(): enabled(true)
   {
   }

   // enable/disable all notifications of subscribers
   void setEnabled(const bool b)
   {
      enabled = b;
   }

   // method to be called from global OnTimer event handler
   void checkTimers()
   {
      if(!enabled) return;
      int n = ArraySize(subscribers);
      for(int i = 0; i < n; ++i)
      {
         if(CheckPointer(subscribers[i]) != POINTER_INVALID)
         {
            subscribers[i].isTimeCome();
         }
      }
   }

public:
   // attach new subscriber to get timer notifications
   void bind(TimerNotification &tn)
   {
      int i, n = ArraySize(subscribers);
      for(i = 0; i < n; ++i)
      {
         if(subscribers[i] == &tn) return;
         if(subscribers[i] == NULL) break;
      }

      if(i == n)
      {
         ArrayResize(subscribers, n + 1);
      }
      else
      {
         n = i;
      }
      subscribers[n] = &tn;
   }

   // remove subscriber from timer notifications list
   void unbind(TimerNotification &tn)
   {
      const int n = ArraySize(subscribers);
      for(int i = 0; i < n; ++i)
      {
         if(subscribers[i] == &tn)
         {
            subscribers[i] = NULL;
            return;
         }
      }
   }

   // method to call from the global OnTimer event handler
   static void onTimer()
   {
      _mainTimer.checkTimers();
   }
   
   // global enable/disable for all custom timers
   static void enable(const bool b)
   {
      _mainTimer.setEnabled(b);
   }
   
   //+------------------------------------------------------------------+
   //| Base timer class (still abstract cause notify is not implemented)|
   //+------------------------------------------------------------------+
   class SingleTimer: public TimerNotification
   {
   protected:
      int multiplier;
      MultiTimer *owner;
   
   public:
      // create a timer with given multiplier of base period, optionally paused,
      // register this object in MultiTimer
      SingleTimer(const int m, const bool paused = false): multiplier(m)
      {
         owner = &MultiTimer::_mainTimer;
         if(!paused) owner.bind(this);
      }
   
      // unregister this object in MultiTimer
      ~SingleTimer()
      {
         owner.unbind(this);
      }
   
      // specify timer period
      virtual int getInterval() override
      {
         return multiplier;
      }
   
      // pause this timer
      virtual void stop()
      {
         owner.unbind(this);
      }
   
      // resume this timer
      virtual void start()
      {
         owner.bind(this);
      }
   };
};

static MultiTimer MultiTimer::_mainTimer;

//+------------------------------------------------------------------+
//| Concrete worker class for timers with optional counter           |
//+------------------------------------------------------------------+
class CountableTimer: public MultiTimer::SingleTimer
{
protected:
   const uint repeat;
   uint count;
   
public:
   CountableTimer(const int m, const uint r = UINT_MAX, const bool paused = false):
      SingleTimer(m, paused), repeat(r), count(0) { }
   
   virtual bool isTimeCome() override
   {
      if(count >= repeat && repeat != UINT_MAX)
      {
         stop();
         return false;
      }
      // delegate timing checkup to the parent class,
      // increase own count only if timer is triggered right now (got true)
      return SingleTimer::isTimeCome() && (bool)++count;
   }

   // updated method is additionally dropping the count
   virtual void stop() override
   {
      SingleTimer::stop();
      count = 0;
   }

   uint getCount() const
   {
      return count;
   }
   
   uint getRepeat() const
   {
      return repeat;
   }
};

// pointer to a function of custom timer handler (return false to stop)
typedef bool (*TimerHandler)(void);

//+------------------------------------------------------------------+
//| Class for simple timers with pseudo-handler functions in defines |
//+------------------------------------------------------------------+
class FunctionalTimer: public MultiTimer::SingleTimer
{
   TimerHandler func;
public:
   FunctionalTimer(const int m, TimerHandler f):
      SingleTimer(m), func(f) { }
      
   virtual void notify() override
   {
      if(func != NULL)
      {
         if(!func())
         {
            stop();
         }
      }
   }
};

//+------------------------------------------------------------------+
//| Pseudo-handler function define based on FunctionalTimer class    |
//+------------------------------------------------------------------+
#define OnTimerCustom(P) OnTimer##P(); \
FunctionalTimer ft##P(P, OnTimer##P); \
bool OnTimer##P()

//+------------------------------------------------------------------+
