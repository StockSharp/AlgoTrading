//+------------------------------------------------------------------+
//|                                                   SignalMACD.mqh |
//|                      Copyright © 2010, MetaQuotes Software Corp. |
//|                                        http://www.metaquotes.net |
//|                                              Revision 2010.10.12 |
//+------------------------------------------------------------------+
#include <Expert\ExpertSignal.mqh>
// wizard description start
//+------------------------------------------------------------------+
//| Description of the class                                         |
//| Title=Signals based on crossover of main and signal MACD lines   |
//| Type=Signal                                                      |
//| Name=MACD                                                        |
//| Class=CSignalMACD                                                |
//| Page=                                                            |
//| Parameter=PeriodFast,int,12                                      |
//| Parameter=PeriodSlow,int,24                                      |
//| Parameter=PeriodSignal,int,9                                     |
//| Parameter=StopLoss,int,20                                        |
//| Parameter=TakeProfit,int,50                                      |
//+------------------------------------------------------------------+
// wizard description end
//+------------------------------------------------------------------+
//| Class CSignalMACD.                                               |
//| Appointment: Class trading signals                               |
//|              cross of the main and signal lines MACD.            |
//|              Derives from class CExpertSignal.                   |
//+------------------------------------------------------------------+
class CSignalMACD : public CExpertSignal
  {
protected:
   CiMACD           *m_MACD;
   //--- input parameters
   int               m_period_fast;
   int               m_period_slow;
   int               m_period_signal;
   int               m_stop_loss;
   int               m_take_profit;

public:
                     CSignalMACD();
                    ~CSignalMACD();
   //--- methods initialize protected data
   void              PeriodFast(int period_fast)     { m_period_fast=period_fast;             }
   void              PeriodSlow(int period_slow)     { m_period_slow=period_slow;             }
   void              PeriodSignal(int period_signal) { m_period_signal=period_signal;         }
   void              StopLoss(int stop_loss)         { m_stop_loss=stop_loss;                 }
   void              TakeProfit(int take_profit)     { m_take_profit=take_profit;             }
   virtual bool      InitIndicators(CIndicators* indicators);
   virtual bool      ValidationSettings();
   //---
   virtual bool      CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseLong(double& price);
   virtual bool      CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration);
   virtual bool      CheckCloseShort(double& price);

protected:
   double            MainMACD(int ind)               { return(m_MACD.Main(ind));              }
   double            SignalMACD(int ind)             { return(m_MACD.Signal(ind));            }
   double            StateMACD(int ind)              { return(MainMACD(ind)-SignalMACD(ind)); }
   int               ExtStateMACD(int ind);
  };
//+------------------------------------------------------------------+
//| Constructor CSignalMACD.                                         |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalMACD::CSignalMACD()
  {
//--- initialize protected data
   m_MACD         =NULL;
//--- set default inputs
   m_period_fast  =12;
   m_period_slow  =24;
   m_period_signal=9;
   m_stop_loss    =20;
   m_take_profit  =50;
  }
//+------------------------------------------------------------------+
//| Destructor CSignalMACD.                                          |
//| INPUT:  no.                                                      |
//| OUTPUT: no.                                                      |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
void CSignalMACD::~CSignalMACD()
  {
//---
  }
//+------------------------------------------------------------------+
//| Validation settings protected data.                              |
//| INPUT:  no.                                                      |
//| OUTPUT: true-if settings are correct, false otherwise.           |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalMACD::ValidationSettings()
  {
   if(m_period_fast>=m_period_slow)
     {
      printf(__FUNCTION__+": slow period must be greater than fast period");
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
bool CSignalMACD::InitIndicators(CIndicators* indicators)
  {
//--- check
   if(indicators==NULL) return(false);
//--- create MACD indicator
   if(m_MACD==NULL)
      if((m_MACD=new CiMACD)==NULL)
        {
         printf(__FUNCTION__+": error creating object");
         return(false);
        }
//--- add MACD indicator to collection
   if(!indicators.Add(m_MACD))
     {
      printf(__FUNCTION__+": error adding object");
      delete m_MACD;
      return(false);
     }
//--- initialize MACD indicator
   if(!m_MACD.Create(m_symbol.Name(),m_period,m_period_fast,m_period_slow,m_period_signal,PRICE_CLOSE))
     {
      printf(__FUNCTION__+": error initializing object");
      return(false);
     }
//--- ok
   return(true);
  }
//+------------------------------------------------------------------+
//| Check relative positions of the main and signal lines MACD.      |
//| INPUT:  ind - start index for check.                             |
//| OUTPUT: absolute value - the number of intervals                 |
//|                     from cross of the main and signal lines MACD,|
//|         sign: minus - fast MA crosses slow MA down,              |
//|               plus - fast MA crosses slow MA upward.             |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
int CSignalMACD::ExtStateMACD(int ind)
  {
   int    res=0;
   double var;
//---
   for(int i=ind;i<5;i++)
     {
      if(MainMACD(i)==WRONG_VALUE || SignalMACD(i)==WRONG_VALUE) break;
      var=StateMACD(i);
      if(res>0)
        {
         if(var<0) break;
         res++;
         continue;
        }
      if(res<0)
        {
         if(var>0) break;
         res--;
         continue;
        }
      if(var>0) res++;
      if(var<0) res--;
     }
//---
   return(res);
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
bool CSignalMACD::CheckOpenLong(double& price,double& sl,double& tp,datetime& expiration)
  {
   price=0.0;
   sl   =m_symbol.Ask()-m_stop_loss*m_adjusted_point;
   tp   =m_symbol.Ask()+m_take_profit*m_adjusted_point;
//---
   return(ExtStateMACD(1)==1);
  }
//+------------------------------------------------------------------+
//| Check conditions for long position close.                        |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalMACD::CheckCloseLong(double& price)
  {
   price=0.0;
//---
   return(ExtStateMACD(1)==-1);
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
bool CSignalMACD::CheckOpenShort(double& price,double& sl,double& tp,datetime& expiration)
  {
   price=0.0;
   sl   =m_symbol.Bid()+m_stop_loss*m_adjusted_point;
   tp   =m_symbol.Bid()-m_take_profit*m_adjusted_point;
//---
   return(ExtStateMACD(1)==-1);
  }
//+------------------------------------------------------------------+
//| Check conditions for short position close.                       |
//| INPUT:  price - refernce for price.                              |
//| OUTPUT: true-if condition performed, false otherwise.            |
//| REMARK: no.                                                      |
//+------------------------------------------------------------------+
bool CSignalMACD::CheckCloseShort(double& price)
  {
   price=0.0;
//---
   return(ExtStateMACD(1)==1);
  }
//+------------------------------------------------------------------+
