//+------------------------------------------------------------------+
//|                                               SignalCrossEMA.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.10.12 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on crossover of two EMA                      |
//| Type=Signal                                                      |
//| Name=CrossEMA                                                    |
//| Class=CSignalCrossEMA                                            |
//| Page=                                                            |
//| Parameter=FastPeriod,int,12                                      |
//| Parameter=SlowPeriod,int,24                                      |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalCrossEMA.                                           |
//| Appointment: Class trading signals cross two EMA.                |
//|              Derives from class CExpertSignal.                   |
//+------------------------------------------------------------------+
class CSignalCrossEMA : public CExpertSignal
  {
protected:
   CiMA             *m_FastEMA;
   CiMA             *m_SlowEMA;
   //--- input parameters
   int               m_fast_period;
   int               m_slow_period;

public:
                     CSignalCrossEMA();
                    ~CSignalCrossEMA();
   //--- methods initialize protected data
   void              FastPeriod(int period) { m_fast_period=period;                }
   void              SlowPeriod(int period) { m_slow_period=period;                }
   virtual bool      InitIndicators(CIndicators* indicators);
   virtual bool      ValidationSettings();
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);

protected:
   bool              InitFastEMA(CIndicators* indicators);
   bool              InitSlowEMA(CIndicators* indicators);
   //---
   double            FastEMA(int ind)       { return(m_FastEMA.Main(ind));         }
   double            SlowEMA(int ind)       { return(m_SlowEMA.Main(ind));         }
   double            StateFastEMA(int ind)  { return(FastEMA(ind)-FastEMA(ind+1)); }
   double            StateSlowEMA(int ind)  { return(SlowEMA(ind)-SlowEMA(ind+1)); }
   double            StateEMA(int ind)      { return(FastEMA(ind)-SlowEMA(ind));   }
  };
//+------------------------------------------------------------------+
//| Constructor CSignalCrossEMA.                                     |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalCrossEMA::CSignalCrossEMA()
  {
//--- initialize protected data
   m_FastEMA     =NULL;
   m_SlowEMA     =NULL;
//--- set default inputs
   m_fast_period =12;
   m_slow_period =24;
  }
//+------------------------------------------------------------------+
//| Destructor CSignalCrossEMA.                                      |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalCrossEMA::~CSignalCrossEMA()
  {
//---
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCrossEMA::ValidationSettings()
  {
   if(m_fast_period>=m_slow_period)
     {
      printf(__FUNCTION__+": period of slow EMA must be greater than period of fast EMA");
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
bool CSignalCrossEMA::InitIndicators(CIndicators* indicators)
  {
//--- check
   if(indicators==NULL)         return(false);
//--- create and initialize fast EMA indicator
   if(!InitFastEMA(indicators)) return(false);
//--- create and initialize slow EMA indicator
   if(!InitSlowEMA(indicators)) return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create fast EMA indicators.                                      |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCrossEMA::InitFastEMA(CIndicators* indicators)
  {
//--- create fast EMA indicator
   if(m_FastEMA==NULL)
      if((m_FastEMA=new CiMA)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add fast EMA indicator to collection
   if(!indicators.Add(m_FastEMA))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_FastEMA;
      return(false);
     }
//--- initialize fast EMA indicator
   if(!m_FastEMA.Create(m_symbol.Name(),m_period,m_fast_period,0,MODE_EMA,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
   m_FastEMA.BufferResize(1000);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create slow EMA indicators.                                      |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCrossEMA::InitSlowEMA(CIndicators* indicators)
  {
//--- create slow EMA indicator
   if(m_SlowEMA==NULL)
      if((m_SlowEMA=new CiMA)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add slow EMA indicator to collection
   if(!indicators.Add(m_SlowEMA))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_SlowEMA;
      return(false);
     }
//--- initialize slow EMA indicator
   if(!m_SlowEMA.Create(m_symbol.Name(),m_period,m_slow_period,0,MODE_EMA,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
   m_SlowEMA.BufferResize(1000);
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
bool CSignalCrossEMA::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(!(StateEMA(2)<0 && StateEMA(1)>0)) return(false);
//---
   price=0.0;
   sl   =0.0;
   tp   =0.0;
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCrossEMA::CheckCloseLong(double& price)
  {
   if(!(StateEMA(2)>0 && StateEMA(1)<0)) return(false);
//---
   price=0.0;
//---
   return(true);
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
bool CSignalCrossEMA::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   if(!(StateEMA(2)>0 && StateEMA(1)<0)) return(false);
//---
   price=0.0;
   sl   =0.0;
   tp   =0.0;
//---
   return(true);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalCrossEMA::CheckCloseShort(double& price)
  {
   if(!(StateEMA(2)<0 && StateEMA(1)>0)) return(false);
//---
   price=0.0;
//---
   return(true);
  }
//+------------------------------------------------------------------+
