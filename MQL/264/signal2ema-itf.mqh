//+------------------------------------------------------------------+
//|                                               Signal2EMA-ITF.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.11.15 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
#include <Expert\Signal\SignalITF.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on crossover of two EMA                      |
//| with intraday time filter                                        |
//| Type=Signal                                                      |
//| Name=TwoEMAwithITF                                               |
//| Class=CSignal2EMA_ITF                                            |
//| Page=                                                            |
//| Parameter=PeriodFastEMA,int,21                                   |
//| Parameter=PeriodSlowEMA,int,34                                   |
//| Parameter=PeriodATR,int,24                                       |
//| Parameter=Limit,double,1.0                                       |
//| Parameter=StopLoss,double,2.0                                    |
//| Parameter=TakeProfit,double,1.0                                  |
//| Parameter=Expiration,int,4                                       |
//| Parameter=GoodMinuteOfHour,int,-1                                |
//| Parameter=BadMinutesOfHour,long,0                                |
//| Parameter=GoodHourOfDay,int,-1                                   |
//| Parameter=BadHoursOfDay,int,0                                    |
//| Parameter=GoodDayOfWeek,int,-1                                   |
//| Parameter=BadDaysOfWeek,int,0                                    |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignal2EMA_ITF.                                           |
//| Purpose: Class trading signals cross two EMA                     |
//|          with intraday time filter.                              |
//|          Derived from class CExpertSignal.                       |
//+------------------------------------------------------------------+
class CSignal2EMA_ITF : public CExpertSignal
  {
protected:
   CiMA             *m_fastEMA;
   CiMA             *m_slowEMA;
   CiATR            *m_ATR;
   CSignalITF        m_time_filter;
   //--- input parameters
   int               m_period_fast_EMA;
   int               m_period_slow_EMA;
   int               m_period_ATR;
   double            m_limit;
   double            m_stop_loss;
   double            m_take_profit;
   int               m_expiration;

public:
                     CSignal2EMA_ITF();
   //--- methods initialize protected data
   void              PeriodFastEMA(int period)                  { m_period_fast_EMA=period;                            }
   void              PeriodSlowEMA(int period)                  { m_period_slow_EMA=period;                            }
   void              PeriodATR(int period)                      { m_period_ATR=period;                                 }
   void              Limit(double limit)                        { m_limit=limit;                                       }
   void              StopLoss(double stop_loss)                 { m_stop_loss=stop_loss;                               }
   void              TakeProfit(double take_profit)             { m_take_profit=take_profit;                           }
   void              Expiration(int expiration)                 { m_expiration=expiration;                             }
   void              GoodMinuteOfHour(int good_minute_of_hour)  { m_time_filter.GoodMinuteOfHour(good_minute_of_hour); }
   void              BadMinutesOfHour(long bad_minutes_of_hour) { m_time_filter.BadMinutesOfHour(bad_minutes_of_hour); }
   void              GoodHourOfDay(int good_hour_of_day)        { m_time_filter.GoodHourOfDay(good_hour_of_day);       }
   void              BadHoursOfDay(int bad_hours_of_day)        { m_time_filter.BadHoursOfDay(bad_hours_of_day);       }
   void              GoodDayOfWeek(int good_day_of_week)        { m_time_filter.GoodDayOfWeek(good_day_of_week);       }
   void              BadDaysOfWeek(int bad_days_of_week)        { m_time_filter.BadDaysOfWeek(bad_days_of_week);       }
   virtual bool      ValidationSettings();
   virtual bool      InitIndicators(CIndicators *indicators);
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);
   //---
   virtual bool      CheckTrailingOrderLong(COrderInfo* order,double& price);
   virtual bool      CheckTrailingOrderShort(COrderInfo* order,double& price);

protected:
   bool              InitFastEMA(CIndicators* indicators);
   bool              InitSlowEMA(CIndicators* indicators);
   bool              InitATR(CIndicators* indicators);
   //---
   double            FastEMA(int ind)      { return(m_fastEMA.Main(ind));         }
   double            SlowEMA(int ind)      { return(m_slowEMA.Main(ind));         }
   double            StateFastEMA(int ind) { return(FastEMA(ind)-FastEMA(ind+1)); }
   double            StateSlowEMA(int ind) { return(SlowEMA(ind)-SlowEMA(ind+1)); }
   double            StateEMA(int ind)     { return(FastEMA(ind)-SlowEMA(ind));   }
   //---
   double            ATR(int ind)          { return(m_ATR.Main(ind));             }
  };
//+------------------------------------------------------------------+
//| Constructor CSignal2EMA_ITF.                                     |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignal2EMA_ITF::CSignal2EMA_ITF()
  {
//--- initialize protected data
   m_fastEMA        =NULL;
   m_slowEMA        =NULL;
   m_ATR            =NULL;
//--- set default inputs
   m_period_fast_EMA=21;
   m_period_slow_EMA=34;
   m_period_ATR     =24;
   m_limit          =1.0;
   m_stop_loss      =2.0;
   m_take_profit    =1.0;
   m_expiration     =4;
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::ValidationSettings()
  {
   if(m_period_fast_EMA>=m_period_slow_EMA)
     {
      printf(__FUNCTION__+": period slow EMA must be greater than period fast EMA");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create indicators.                                               |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::InitIndicators(CIndicators *indicators)
  {
//--- create and initialize fast EMA indicator
   if(!InitFastEMA(indicators)) return(false);
//--- create and initialize slow EMA indicator
   if(!InitSlowEMA(indicators)) return(false);
//--- create and initialize ATR indicator
   if(!InitATR(indicators))     return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create fast EMA indicators.                                      |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::InitFastEMA(CIndicators* indicators)
  {
//--- create fast EMA indicator and add it to collection
   if(m_fastEMA==NULL)
      if((m_fastEMA=new CiMA)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
   if(!indicators.Add(m_fastEMA))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_fastEMA;
      return(false);
     }
//--- initialize EMA indicator
   if(!m_fastEMA.Create(m_symbol.Name(),m_period,m_period_fast_EMA,0,MODE_EMA,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create slow EMA indicators.                                      |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::InitSlowEMA(CIndicators* indicators)
  {
//--- create slow EMA indicator and add it to collection
   if(m_slowEMA==NULL)
      if((m_slowEMA=new CiMA)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
   if(!indicators.Add(m_slowEMA))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_slowEMA;
      return(false);
     }
//--- initialize EMA indicator
   if(!m_slowEMA.Create(m_symbol.Name(),m_period,m_period_slow_EMA,0,MODE_EMA,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create ATR indicators.                                           |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::InitATR(CIndicators* indicators)
  {
//--- create ATR indicator and add it to collection
   if(m_ATR==NULL)
      if((m_ATR=new CiATR)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
   if(!indicators.Add(m_ATR))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_ATR;
      return(false);
     }
//--- initialize ATR indicator
   if(!m_ATR.Create(m_symbol.Name(),m_period,m_period_ATR))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position open.                         |
//| INPUT:  price      - refernce for price,                         |
//|         sl         - refernce for stop loss,                     |
//|         tp         - refernce for take profit,                   |
//|         expiration - refernce for expiration.                    |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(!(StateEMA(1)>0 && StateEMA(2)<0))                    return(false);
   if(!m_time_filter.CheckOpenLong(price,sl,tp,expiration)) return(false);
//---
   double ema=SlowEMA(1);
   double atr=ATR(1);
   double spread=m_symbol.Ask()-m_symbol.Bid();
//---
   price=m_symbol.NormalizePrice(ema-m_limit*atr+spread);
   sl   =m_symbol.NormalizePrice(price+m_stop_loss*atr);
   tp   =m_symbol.NormalizePrice(price-m_take_profit*atr);
   expiration+=m_expiration*PeriodSeconds(m_period);
//---
   return(true);
  }

//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::CheckCloseLong(double& price)
  {
   return(false);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position open.                        |
//| INPUT:  price      - refernce for price,                         |
//|         sl         - refernce for stop loss,                     |
//|         tp         - refernce for take profit,                   |
//|         expiration - refernce for expiration.                    |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(!(StateEMA(1)<0 && StateEMA(2)>0))                     return(false);
   if(!m_time_filter.CheckOpenShort(price,sl,tp,expiration)) return(false);
//---
   double ema=SlowEMA(1);
   double atr=ATR(1);
//---
   price      =m_symbol.NormalizePrice(ema+m_limit*atr);
   sl         =m_symbol.NormalizePrice(price+m_stop_loss*atr);
   tp         =m_symbol.NormalizePrice(price-m_take_profit*atr);
   expiration+=m_expiration*PeriodSeconds(m_period);
//---
   return(true);
  }

//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::CheckCloseShort(double& price)
  {
   return(false);
  }
//+------------------------------------------------------------------+
//| Check conditions for long order modify.                          |
//| INPUT:  order - pointer for order object,                        |
//|         price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::CheckTrailingOrderLong(COrderInfo* order,double& price)
  {
//--- check
   if(order==NULL) return(false);
//---
   double ema=SlowEMA(1);
   double atr=ATR(1);
   double spread=m_symbol.Ask()-m_symbol.Bid();
//---
   price=m_symbol.NormalizePrice(ema-m_limit*atr+spread);
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check conditions for short order modify.                         |
//| INPUT:  order - pointer for order object,                        |
//|         price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal2EMA_ITF::CheckTrailingOrderShort(COrderInfo* order,double& price)
  {
//--- check
   if(order==NULL) return(false);
//---
   double ema=SlowEMA(1);
   double atr=ATR(1);
//---
   price=m_symbol.NormalizePrice(ema+m_limit*atr);
//---
   return(true);
  }
//+------------------------------------------------------------------+
