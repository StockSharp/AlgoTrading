//+------------------------------------------------------------------+
//|                                                   Signal3EMA.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.10.12 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on three EMA                                 |
//| Type=Signal                                                      |
//| Name=ThreeEMA                                                    |
//| Class=CSignal3EMA                                                |
//| Page=                                                            |
//| Parameter=FastPeriod,int,5                                       |
//| Parameter=MediumPeriod,int,12                                    |
//| Parameter=SlowPeriod,int,24                                      |
//| Parameter=StopLoss,int,20                                        |
//| Parameter=TakeProfit,int,50                                      |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignal3EMA.                                               |
//| Appointment: Class trading signals relative positions three MA.  |
//|              Derives from class CExpertSignal.                   |
//+------------------------------------------------------------------+
class CSignal3EMA : public CExpertSignal
  {
protected:
   CiMA             *m_FastEMA;
   CiMA             *m_MediumEMA;
   CiMA             *m_SlowEMA;
   //--- input parameters
   int               m_fast_period;
   int               m_medium_period;
   int               m_slow_period;
   int               m_stop_loss;
   int               m_take_profit;

public:
                     CSignal3EMA();
                    ~CSignal3EMA();
   //--- methods initialize protected data
   void              FastPeriod(int period)      { m_fast_period=period;          }
   void              MediumPeriod(int period)    { m_medium_period=period;        }
   void              SlowPeriod(int period)      { m_slow_period=period;          }
   void              StopLoss(int stop_loss)     { m_stop_loss=stop_loss;         }
   void              TakeProfit(int take_profit) { m_take_profit=take_profit;     }
   virtual bool      InitIndicators(CIndicators* indicators);
   virtual bool      ValidationSettings();
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);

protected:
   bool              InitFastEMA(CIndicators* indicators);
   bool              InitMediumEMA(CIndicators* indicators);
   bool              InitSlowEMA(CIndicators* indicators);
   //---
   double            FastEMA(int ind)            { return(m_FastEMA.Main(ind));   }
   double            MediumEMA(int ind)          { return(m_MediumEMA.Main(ind)); }
   double            SlowEMA(int ind)            { return(m_SlowEMA.Main(ind));   }
  };
//+------------------------------------------------------------------+
//| Constructor CSignal3EMA.                                         |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignal3EMA::CSignal3EMA()
  {
//--- initialize protected data
   m_FastEMA      =NULL;
   m_MediumEMA    =NULL;
   m_SlowEMA      =NULL;
//--- set default inputs
   m_fast_period  =5;
   m_medium_period=12;
   m_slow_period  =24;
   m_stop_loss    =20;
   m_take_profit  =50;
  }
//+------------------------------------------------------------------+
//| Destructor CSignal3EMA.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignal3EMA::~CSignal3EMA()
  {
//---
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal3EMA::ValidationSettings()
  {
   if(m_medium_period>=m_slow_period)
     {
      printf(__FUNCTION__+": period slow EMA must be greater than period medium EMA");
      return(false);
     }
   if(m_fast_period>=m_medium_period)
     {
      printf(__FUNCTION__+": period medium EMA must be greater than period fast EMA");
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
bool CSignal3EMA::InitIndicators(CIndicators* indicators)
  {
//--- check
   if(indicators==NULL)           return(false);
//--- create and initialize fast EMA indicator
   if(!InitFastEMA(indicators))   return(false);
//--- create and initialize medium EMA indicator
   if(!InitMediumEMA(indicators)) return(false);
//--- create and initialize slow EMA indicator
   if(!InitSlowEMA(indicators))   return(false);
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create fast EMA indicators.                                      |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal3EMA::InitFastEMA(CIndicators* indicators)
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
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Create medium EMA indicators.                                    |
//| INPUT:  indicators -pointer of indicator collection.             |
//| OUTPUT: true-if successful, false otherwise.                     |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal3EMA::InitMediumEMA(CIndicators* indicators)
  {
//--- create medium EMA indicator
   if(m_MediumEMA==NULL)
      if((m_MediumEMA=new CiMA)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add medium EMA indicator to collection
   if(!indicators.Add(m_MediumEMA))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_MediumEMA;
      return(false);
     }
//--- initialize medium EMA indicator
   if(!m_MediumEMA.Create(m_symbol.Name(),m_period,m_medium_period,0,MODE_EMA,PRICE_CLOSE))
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
bool CSignal3EMA::InitSlowEMA(CIndicators* indicators)
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
bool CSignal3EMA::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   double medium=MediumEMA(1);
//---
   price=0.0;
   sl   =m_symbol.Ask()-m_stop_loss*m_adjusted_point;
   tp   =m_symbol.Ask()+m_take_profit*m_adjusted_point;
//---
   return(FastEMA(1)>medium && medium>SlowEMA(1));
  }
//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal3EMA::CheckCloseLong(double& price)
  {
   double medium=MediumEMA(1);
//---
   price=0.0;
//---
   return(FastEMA(1)<medium && medium<SlowEMA(1));
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
bool CSignal3EMA::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   double medium=MediumEMA(1);
//---
   price=0.0;
   sl   =m_symbol.Bid()+m_stop_loss*m_adjusted_point;
   tp   =m_symbol.Bid()-m_take_profit*m_adjusted_point;
//---
   return(FastEMA(1)<medium && medium<SlowEMA(1));
  }
//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignal3EMA::CheckCloseShort(double& price)
  {
   double medium=MediumEMA(1);
//---
   price=0.0;
//---
   return(FastEMA(1)>medium && medium>SlowEMA(1));
  }
//+------------------------------------------------------------------+
